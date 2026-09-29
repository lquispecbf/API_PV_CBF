using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA.Configuracion
{
    public class ConfiguracionConexion
    {
        public string CadenaSQL { get; set; }
        public string CadenaSQLExt { get; set; }
        public string CadenaSAP { get; set; }
        public string CadenaSAP_ODBC { get; set; }
        public string CadenaSQLPOS { get; set; }

        public string ServiceLayerBase { get; set; }
        public string CompanyDB { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IgnorarErroresCertificadoServiceLayer { get; set; }

        /// <summary>
        /// Timeout en segundos para llamadas al SAP Service Layer.
        /// Usado por SapServiceClient (cliente moderno). Default: 45 segundos.
        /// </summary>
        public int ServiceLayerTimeoutSeconds { get; set; } = 45;

        /// <summary>
        /// Ruta base general para logs de Service Layer y servicios por defecto. Default: D:\LogServiceLayerPagado
        /// </summary>
        public string ServiceLayerLogPath { get; set; } = @"D:\LogServiceLayerPagado";

        /// <summary>
        /// Ruta base específica para logs del módulo Mantenimiento de Artículos SAP.
        /// Si no se especifica, utiliza ServiceLayerLogPath.
        /// </summary>
        public string? LogPathArticulosSap { get; set; }

        /// <summary>
        /// Ruta base específica para logs del módulo Modificar Condición de Pago en Punto de Venta.
        /// Si no se especifica, utiliza ServiceLayerLogPath.
        /// </summary>
        public string? LogPathPuntoVentaCondicionPago { get; set; }

        /// <summary>
        /// Opciones del pipeline de resiliencia Polly para SapServiceClient.
        /// Si no se configura en appsettings.json se usan los valores por defecto.
        /// </summary>
        public ServiceLayerResilienciaOptions ServiceLayerResiliencia { get; set; } = new();
    }

    /// <summary>
    /// Parámetros configurables del pipeline de resiliencia Polly (Retry + CircuitBreaker + Timeout).
    /// Se leen desde appsettings.json → ConfiguracionConexion → ServiceLayerResiliencia.
    /// Todos los valores tienen defaults razonables — no es obligatorio definirlos en appsettings.
    /// </summary>
    public class ServiceLayerResilienciaOptions
    {
        /// <summary>Número máximo de reintentos automáticos ante fallos transitorios. Default: 3.</summary>
        public int MaxReintentos { get; set; } = 3;

        /// <summary>
        /// Timeout en segundos por cada intento individual (no el total).
        /// Si un PATCH tarda más de esto, se cancela y Polly reintenta.
        /// Default: 30s.
        /// </summary>
        public int TimeoutPorIntentoSegundos { get; set; } = 30;

        /// <summary>
        /// Mínimo de requests evaluados antes de que el Circuit Breaker pueda abrirse.
        /// Evita falsos positivos con poco tráfico. Default: 5.
        /// </summary>
        public int CircuitBreaker_MinRequests { get; set; } = 5;

        /// <summary>
        /// Ventana de tiempo (segundos) en la que se cuentan los fallos para el Circuit Breaker.
        /// Default: 30s.
        /// </summary>
        public int CircuitBreaker_VentanaSegundos { get; set; } = 30;

        /// <summary>
        /// Segundos que el Circuit Breaker permanece ABIERTO antes de pasar a semi-abierto.
        /// Durante este tiempo los requests fallan inmediatamente sin tocar SAP. Default: 60s.
        /// </summary>
        public int CircuitBreaker_DuracionAbiertaSegundos { get; set; } = 60;
    }
}
