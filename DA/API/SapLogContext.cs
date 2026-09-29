namespace DA.API
{
    /// <summary>
    /// Contexto de auditoría y metadatos para las operaciones y llamadas hacia SAP Service Layer.
    /// Permite correlacionar cada línea de log con el módulo, usuario, documento o artículo involucrado.
    /// </summary>
    public class SapLogContext
    {
        public string? Modulo { get; set; }
        public string? Usuario { get; set; }
        public string? DocEntry { get; set; }
        public string? DocNum { get; set; }
        public string? ItemCode { get; set; }
        public string? CustomPath { get; set; }

        public SapLogContext() { }

        public SapLogContext(string? modulo, string? usuario = null, string? docEntry = null, string? docNum = null, string? itemCode = null)
        {
            Modulo = modulo;
            Usuario = usuario;
            DocEntry = docEntry;
            DocNum = docNum;
            ItemCode = itemCode;
        }

        public static SapLogContext ParaDocumento(string modulo, string? usuario, string? docEntry, string? docNum = null)
        {
            return new SapLogContext(modulo, usuario, docEntry, docNum, null);
        }

        public static SapLogContext ParaArticulo(string modulo, string? usuario, string itemCode)
        {
            return new SapLogContext(modulo, usuario, null, null, itemCode);
        }

        /// <summary>
        /// Conversión implícita desde string para permitir pasar directamente el nombre del módulo como contexto.
        /// </summary>
        public static implicit operator SapLogContext?(string? modulo)
        {
            return string.IsNullOrWhiteSpace(modulo) ? null : new SapLogContext(modulo.Trim());
        }
    }
}
