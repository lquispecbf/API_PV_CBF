namespace BE.PuntoVenta
{
    public class ListaPrecioClienteItemDTO
    {
        public string? CODIGO { get; set; }
        public string? DESCRIPCION { get; set; }
        public string? LABORATORIO { get; set; }
        public string? PRINCIPIO_ACTIVO { get; set; }
        public string? FECHA_VENCIMIENTO { get; set; }
        public decimal PRECIO_CAJA { get; set; }
        public decimal STOCK { get; set; }
        public decimal CANTIDAD_ESCALA { get; set; }
        public decimal PRECIO_ESCALA { get; set; }
    }
}
