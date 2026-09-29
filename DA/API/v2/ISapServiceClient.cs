using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DA.API.v2
{
    /// <summary>
    /// Contrato del cliente moderno para SAP B1 Service Layer.
    /// Coexiste con API_ServiceLayer (legacy) sin reemplazarlo.
    /// </summary>
    public interface ISapServiceClient
    {
        /// <summary>
        /// Envía una operación individual (GET, POST, PATCH, DELETE) al Service Layer
        /// con reintentos automáticos, gestión de sesión y contexto de auditoría estructurado.
        /// </summary>
        /// <param name="method">Método HTTP (HttpMethod.Patch, HttpMethod.Post, etc.).</param>
        /// <param name="endpoint">Endpoint relativo. Ej: "Items('ART001')".</param>
        /// <param name="payload">Objeto a serializar como JSON body. Null para requests sin body.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <param name="logContext">Contexto opcional con módulo, usuario, DocEntry, DocNum o ItemCode (o string de módulo).</param>
        /// <returns>Tupla (success, response) donde response es el body de la respuesta.</returns>
        Task<(bool success, string response)> SendAsync(
            HttpMethod method,
            string endpoint,
            object? payload = null,
            CancellationToken cancellationToken = default,
            SapLogContext? logContext = null);

        /// <summary>
        /// Envía múltiples operaciones en un solo request OData $batch con contexto de auditoría.
        /// Reduce N round-trips HTTP a 1, ideal para actualizaciones masivas.
        /// </summary>
        Task<SapBatchResult> SendBatchAsync(
            IEnumerable<SapBatchRequest> requests,
            CancellationToken cancellationToken = default,
            SapLogContext? logContext = null);
    }
}
