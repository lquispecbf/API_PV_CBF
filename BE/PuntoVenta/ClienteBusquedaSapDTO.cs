namespace BE.PuntoVenta;

public class ClienteBusquedaSapDTO
{
    public string? CODIGO_CLIENTE { get; set; }
    public string? CLIENTE { get; set; }
    public string? RUC { get; set; }
    public string? ESTADO_CLIENTE { get; set; }

    public decimal LIMITE_CREDITO { get; set; }
    public string? LISTA_PRECIO { get; set; }
    public string? FORMA_PAGO { get; set; }
    public string? CONDICION_PAGO { get; set; }
    public int SLPCODE { get; set; }
    public string? SLPNAME { get; set; }
    public int? GROUP_CODE { get; set; }
}
