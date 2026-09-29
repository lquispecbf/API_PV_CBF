namespace BE.PuntoVenta;

public class LotesBatchRequestDTO
{
    public List<string> Items { get; set; } = new();
    public string? CodigoAlmacen { get; set; }
}
