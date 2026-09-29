using System.Collections.Generic;
using System.Net.Http;

namespace DA.API.v2
{
    /// <summary>
    /// Representa una operación individual dentro de un request OData $batch.
    /// </summary>
    public class SapBatchRequest
    {
        /// <summary>Método HTTP de la operación. Por defecto PATCH.</summary>
        public HttpMethod Method { get; init; } = HttpMethod.Patch;

        /// <summary>
        /// Endpoint relativo de la operación.
        /// Ejemplos: "Items('ART001')", "Orders(12345)", "BusinessPartners('C001')".
        /// </summary>
        public string Endpoint { get; init; } = string.Empty;

        /// <summary>
        /// Objeto a serializar como JSON body de la operación.
        /// Null para operaciones sin body (ej: DELETE, GET).
        /// </summary>
        public object? Payload { get; init; }
    }

    /// <summary>
    /// Resultado agregado de un request OData $batch.
    /// </summary>
    public class SapBatchResult
    {
        /// <summary>True si todas las operaciones del batch tuvieron éxito.</summary>
        public bool AllSucceeded { get; init; }

        /// <summary>Cantidad total de operaciones enviadas.</summary>
        public int TotalRequests { get; init; }

        /// <summary>Cantidad de operaciones exitosas.</summary>
        public int SucceededCount { get; init; }

        /// <summary>Cantidad de operaciones fallidas.</summary>
        public int FailedCount { get; init; }

        /// <summary>Resultados individuales de cada operación, en el mismo orden que el request.</summary>
        public List<SapBatchPartResult> Results { get; init; } = [];
    }

    /// <summary>
    /// Resultado de una operación individual dentro de un $batch.
    /// </summary>
    public class SapBatchPartResult
    {
        /// <summary>Índice de la operación (base 0) dentro del batch original.</summary>
        public int Index { get; init; }

        /// <summary>True si el código HTTP de la respuesta indica éxito (2xx).</summary>
        public bool Success { get; init; }

        /// <summary>Código HTTP retornado para esta operación.</summary>
        public int StatusCode { get; init; }

        /// <summary>Body de la respuesta para esta operación.</summary>
        public string Body { get; init; } = string.Empty;
    }
}
