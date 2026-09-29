namespace BE.PuntoVenta;

public class DocumentoEstadoInfo
{
    public int DocEntry { get; set; }
    public string DocStatus { get; set; } = "";
    public string SendStatus { get; set; } = "";
    public int DocEntrySap { get; set; }
    public string Almacen { get; set; } = "";
    public bool Existe { get; set; }
}
