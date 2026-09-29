namespace BE.PuntoVenta;

public class PreciosBatchRequestDTO
{
    public List<string> Items { get; set; } = new();
    public int CodigoListaPrecio { get; set; }
    public string? CodigoAlmacen { get; set; }
    public string? CodigoCliente { get; set; }
}
