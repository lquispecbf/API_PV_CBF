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
        private Timer? _renewalTimer;

        public SapWarmupService(ISapServiceClient sapClient)
        {
            _sapClient = sapClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await FileLogger.Info("🚀 [Warm-up] Iniciando pre-calentamiento de sesión SAP...", "WARMUP");

            bool exito = false;
            try
            {
                // Consulta un endpoint OData liviano de solo 1 registro para forzar el login y mantener la conexión caliente.
                var (ok, _) = await _sapClient.SendAsync(HttpMethod.Get, "Users?$top=1&$select=UserCode",
                    payload: null,
                    cancellationToken: cancellationToken);

                exito = ok;
                if (exito)
                {
                    await FileLogger.Info("✅ [Warm-up] Sesión SAP pre-calentada. El primer usuario no esperará.", "WARMUP");
                }
                else
                {
                    await FileLogger.Warning("⚠️ [Warm-up] Intento inicial no devolvió éxito. Se reintentará en 30s.", "WARMUP");
                }
            }
            catch (Exception ex)
            {
                // El warm-up falla silenciosamente — la app sigue funcionando.
                await FileLogger.Warning($"⚠️ [Warm-up] No se pudo pre-calentar sesión SAP: {ex.Message}. " +
                    "Se reintentará en 30s.", "WARMUP");
            }

            // Si tuvo éxito, programar renovación regular cada 20 min. Si falló, reintentar calentamiento rápido en 30s.
            var tiempoInicio = exito ? TimeSpan.FromMinutes(20) : TimeSpan.FromSeconds(30);
            _renewalTimer = new Timer(
                callback: RenovarSesionSilenciosamente,
                state: null,
                dueTime: tiempoInicio,
                period: TimeSpan.FromMinutes(20));

            await FileLogger.Info($"⏰ [Warm-up] Timer de renovación de sesión activo (primer chequeo en {(int)tiempoInicio.TotalSeconds}s, periodo: 20 min).", "WARMUP");
        }

        private async void RenovarSesionSilenciosamente(object? _)
        {
            try
            {
                await FileLogger.Info("🔄 [Warm-up] Renovando sesión SAP proactivamente...", "WARMUP");
                var (ok, _) = await _sapClient.SendAsync(HttpMethod.Get, "Users?$top=1&$select=UserCode");
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
            _renewalTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _renewalTimer?.Dispose();
        }
    }
}
