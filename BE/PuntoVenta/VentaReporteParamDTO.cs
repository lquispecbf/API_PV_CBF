namespace BE.PuntoVenta
{
    public class VentaReporteParamDTO
    {
        public int DocEntrySap { get; set; }
        public int DocEntryOwtr { get; set; }
        public string? Whs { get; set; }
        public int DocEntry { get; set; }
        public string? NroSap { get; set; }
    }
}
