namespace BE.PuntoVenta;

public class UnidadMedidaArticuloSapDTO
{
    public int CODIGO_UMD { get; set; }

    public string? CODIGO { get; set; }

    public string? NOMBRE { get; set; }

    public decimal FACTOR { get; set; }

    public string? ES_PREDETERMINADA { get; set; }
}
