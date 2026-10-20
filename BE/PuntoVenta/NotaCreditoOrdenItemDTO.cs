namespace BE.PuntoVenta;

public class NotaCreditoOrdenItemDTO
{
    public int Index { get; set; }
    public string Estado { get; set; } = "1";
    public int DocEntrySap { get; set; }
    public int DocNum { get; set; }
    public string NumAtCard { get; set; } = "";
    public decimal Monto { get; set; }
    public bool EsCerradaSap { get; set; }
    public bool EsFormatoAntiguo { get; set; }
    public bool PermiteModificar { get; set; }
}
