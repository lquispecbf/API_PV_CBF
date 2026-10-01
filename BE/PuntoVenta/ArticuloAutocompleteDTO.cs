namespace BE.PuntoVenta;

public class ArticuloAutocompleteDTO
{
    public string? CODIGO { get; set; }
    public string? DESCRIPCION { get; set; }
    public string TIPO_CONTROLADO { get; set; } = "01";
}
