using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using DA.API;

namespace DA.API.v2
{
    /// <summary>
    /// Background service que pre-calienta la sesión SAP al arrancar la aplicación.
    /// Elimina el "primer request lento" haciendo login SAP antes de que llegue el primer usuario.
    /// También renueva la sesión cada 20 minutos para que nunca expire en producción.
    /// </summary>
    public class SapWarmupService : IHostedService, IDisposable
    {
        private readonly ISapServiceClient _sapClient;
        private readonly CancellationTokenSource _cts = new();
        private Timer? _renewalTimer;

        public SapWarmupService(ISapServiceClient sapClient)
        {
            _sapClient = sapClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // 🚀 IMPORTANTE PARA IIS:
            // No bloquear StartAsync. Si SAP está frío o apagado, el pipeline de reintentos
            // demoraría hasta 140s, provocando que IIS marque w3wp.exe como colgado.
            // Se desacopla a una tarea en background.
            _ = Task.Run(async () => await EjecutarWarmupInicialAsync(_cts.Token), CancellationToken.None);

            return Task.CompletedTask;
        }

        private async Task EjecutarWarmupInicialAsync(CancellationToken ct)
        {
            await FileLogger.Info("🚀 [Warm-up] Iniciando pre-calentamiento en background...", "WARMUP");

            bool exito = false;
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

                var (ok, _) = await _sapClient.SendAsync(
                    HttpMethod.Get,
                    "Users?$top=1&$select=UserCode",
                    payload: null,
                    cancellationToken: timeoutCts.Token);

                exito = ok;
                if (exito)
                {
                    await FileLogger.Info("✅ [Warm-up] Sesión SAP pre-calentada exitosamente.", "WARMUP");
                }
                else
                {
                    await FileLogger.Warning("⚠️ [Warm-up] Intento inicial no devolvió éxito. Se reintentará en 60s.", "WARMUP");
                }
            }
            catch (Exception ex)
            {
                await FileLogger.Warning($"⚠️ [Warm-up] No se pudo pre-calentar sesión SAP: {ex.Message}. Se reintentará en 60s.", "WARMUP");
            }

            if (ct.IsCancellationRequested) return;

            var tiempoInicio = exito ? TimeSpan.FromMinutes(20) : TimeSpan.FromSeconds(60);
            _renewalTimer = new Timer(
                callback: _ => _ = RenovarSesionSeguraAsync(),
                state: null,
                dueTime: tiempoInicio,
                period: TimeSpan.FromMinutes(20));

            await FileLogger.Info($"⏰ [Warm-up] Timer activo (primer chequeo en {(int)tiempoInicio.TotalSeconds}s, periodo: 20 min).", "WARMUP");
        }

        private async Task RenovarSesionSeguraAsync()
        {
            if (_cts.IsCancellationRequested) return;

            try
            {
                await FileLogger.Info("🔄 [Warm-up] Renovando sesión SAP proactivamente...", "WARMUP");

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

                var (ok, _) = await _sapClient.SendAsync(
                    HttpMethod.Get,
                    "Users?$top=1&$select=UserCode",
                    cancellationToken: timeoutCts.Token);

                if (ok)
                {
                    await FileLogger.Info("✅ [Warm-up] Sesión SAP renovada exitosamente.", "WARMUP");
                }
                else
                {
                    await FileLogger.Warning("⚠️ [Warm-up] Intento de renovación no exitoso.", "WARMUP");
                }
            }
            catch (Exception ex)
            {
                await FileLogger.Warning($"⚠️ [Warm-up] Error en renovación proactiva: {ex.Message}", "WARMUP");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            _renewalTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _renewalTimer?.Dispose();
            _cts.Dispose();
        }
    }
}
