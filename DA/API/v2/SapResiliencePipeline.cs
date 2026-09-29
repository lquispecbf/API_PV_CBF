using System;
using System.Net;
using System.Net.Http;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using DA.Configuracion;
using DA.API;

namespace DA.API.v2
{
    /// <summary>
    /// Excepción lanzada cuando SAP B1 Service Layer devuelve el error de transacción
    /// "Could not commit transaction" en el body de la respuesta.
    /// Polly la capta para reintentar la operación.
    /// </summary>
    public class SapCommitException : Exception
    {
        public SapCommitException(string sapBody)
            : base($"SAP no pudo confirmar la transacción. Body: {sapBody}") { }
    }

    /// <summary>
    /// Construye y cachea el ResiliencePipeline de Polly 8.x para SAP B1 Service Layer.
    ///
    /// El pipeline aplica tres capas en orden (de afuera hacia adentro):
    ///   1. Timeout por intento  →  si un PATCH individual supera N segundos, se cancela
    ///   2. Retry con jitter     →  reintenta con backoff exponencial aleatorio
    ///   3. Circuit Breaker      →  si hay demasiados fallos, para de tocar SAP
    ///
    /// Es un singleton estático para que el Circuit Breaker tenga estado global compartido
    /// entre todos los usuarios de la aplicación.
    /// </summary>
    internal static class SapResiliencePipeline
    {
        private static ResiliencePipeline<HttpResponseMessage>? _pipeline;
        private static readonly object _buildLock = new();

        /// <summary>
        /// Obtiene el pipeline construido. La primera llamada lo instancia; las posteriores
        /// devuelven la instancia cacheada ignorando los parámetros (el pipeline ya está fijo).
        /// </summary>
        public static ResiliencePipeline<HttpResponseMessage> Obtener(ServiceLayerResilienciaOptions opts)
        {
            if (_pipeline is not null) return _pipeline;
            lock (_buildLock)
            {
                if (_pipeline is not null) return _pipeline;
                _pipeline = Construir(opts);
            }
            return _pipeline;
        }

        private static ResiliencePipeline<HttpResponseMessage> Construir(ServiceLayerResilienciaOptions opts)
        {
            return new ResiliencePipelineBuilder<HttpResponseMessage>()

                // ── CAPA 1: Timeout por intento individual ──────────────────────────────
                // Si un PATCH individual tarda más de TimeoutPorIntentoSegundos, se cancela
                // y Polly pasa al retry. El timeout global del HttpClient (45s) sigue como
                // última red de seguridad.
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(opts.TimeoutPorIntentoSegundos),
                    OnTimeout = args =>
                    {
                        FileLogger.Info(
                            $"⏱ [Polly Timeout] Intento superó {opts.TimeoutPorIntentoSegundos}s. " +
                            "Cancelando y reintentando...", "POLLY")
                            .GetAwaiter().GetResult();
                        return default;
                    }
                })

                // ── CAPA 2: Retry con Backoff Exponencial + Jitter ──────────────────────
                // Reintentos: 1→~2s, 2→~4s, 3→~8s (con variación aleatoria ±30%)
                //
                // El JITTER es clave: si 10 usuarios reintentan al mismo tiempo exacto,
                // SAP se satura (thundering herd). Con jitter, cada usuario espera un tiempo
                // ligeramente distinto → SAP recibe los reintentos escalonados.
                //
                // Qué se reintenta:
                //   - Excepciones de red (HttpRequestException)
                //   - Timeout del intento (TimeoutRejectedException)
                //   - Error de commit SAP (SapCommitException — body "Could not commit transaction")
                //   - HTTP 401 Unauthorized (sesión expirada — se limpia antes de reintentar)
                //   - HTTP 429 Too Many Requests
                //   - HTTP 502/503/504 (SAP o su gateway caídos)
                //   - HTTP 408 Request Timeout
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = opts.MaxReintentos,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromSeconds(2),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>()
                        .Handle<SapCommitException>()
                        .HandleResult(r =>
                            r.StatusCode == HttpStatusCode.Unauthorized ||
                            r.StatusCode == HttpStatusCode.RequestTimeout ||
                            r.StatusCode == (HttpStatusCode)429 ||
                            r.StatusCode == HttpStatusCode.BadGateway ||
                            r.StatusCode == HttpStatusCode.ServiceUnavailable ||
                            r.StatusCode == HttpStatusCode.GatewayTimeout),
                    OnRetry = args =>
                    {
                        string causa = args.Outcome.Exception?.Message
                            ?? $"HTTP {(int)(args.Outcome.Result?.StatusCode ?? 0)} {args.Outcome.Result?.StatusCode}";

                        FileLogger.Warning(
                            $"🔁 [Polly Retry] Intento #{args.AttemptNumber + 1} de {opts.MaxReintentos}. " +
                            $"Causa: {causa}. Espera: {args.RetryDelay.TotalMilliseconds:F0} ms.", "POLLY")
                            .GetAwaiter().GetResult();
                        return default;
                    }
                })

                // ── CAPA 3: Circuit Breaker ─────────────────────────────────────────────
                // Si en una ventana de CircuitBreaker_VentanaSegundos hay al menos
                // CircuitBreaker_MinRequests requests y más del 50% son fallos de servidor
                // (excepciones, timeout, 5xx), el circuito se ABRE durante
                // CircuitBreaker_DuracionAbiertaSegundos.
                //
                // Mientras está ABIERTO: cualquier llamada lanza BrokenCircuitException
                // inmediatamente, sin tocar SAP. El usuario recibe un mensaje claro en ~0ms.
                //
                // Después de la pausa → SEMI-ABIERTO: deja pasar 1 request de prueba.
                //   - Si funciona → CERRADO (funcionamiento normal)
                //   - Si falla → ABIERTO otra vez
                //
                // IMPORTANTE: No cuenta errores de cliente (400 Bad Request, 401) —
                // solo fallos de infraestructura/servidor donde realmente SAP no puede responder.
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = 0.5,           // 50% de fallos en la ventana → abrir
                    MinimumThroughput = opts.CircuitBreaker_MinRequests,
                    SamplingDuration = TimeSpan.FromSeconds(opts.CircuitBreaker_VentanaSegundos),
                    BreakDuration = TimeSpan.FromSeconds(opts.CircuitBreaker_DuracionAbiertaSegundos),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>()
                        .Handle<SapCommitException>()
                        // Solo fallos de servidor — NO 400 ni 401 (esos son recuperables solos)
                        .HandleResult(r =>
                            r.StatusCode == HttpStatusCode.BadGateway ||
                            r.StatusCode == HttpStatusCode.ServiceUnavailable ||
                            r.StatusCode == HttpStatusCode.GatewayTimeout ||
                            r.StatusCode == HttpStatusCode.InternalServerError),
                    OnOpened = args =>
                    {
                        FileLogger.Error(
                            $"🔴 [Circuit Breaker ABIERTO] SAP Service Layer no responde correctamente. " +
                            $"Pausando llamadas durante {opts.CircuitBreaker_DuracionAbiertaSegundos}s. " +
                            $"Causa: {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}",
                            "POLLY").GetAwaiter().GetResult();
                        return default;
                    },
                    OnClosed = args =>
                    {
                        FileLogger.Info(
                            "🟢 [Circuit Breaker CERRADO] SAP Service Layer disponible nuevamente. " +
                            "Reanudando operaciones normales.", "POLLY")
                            .GetAwaiter().GetResult();
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        FileLogger.Warning(
                            "🟡 [Circuit Breaker SEMI-ABIERTO] Enviando request de prueba a SAP Service Layer...",
                            "POLLY").GetAwaiter().GetResult();
                        return default;
                    }
                })

                .Build();
        }
    }
}
