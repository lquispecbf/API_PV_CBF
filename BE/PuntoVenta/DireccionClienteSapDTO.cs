namespace BE.PuntoVenta;

public class DireccionClienteSapDTO
{
    public string? CODIGO_CLIENTE { get; set; }
    public string? CODIGO_DIRECCION { get; set; }
    public string? TIPO_DIRECCION { get; set; }

    public string? DIRECCION { get; set; }
    public string? BARRIO { get; set; }
    public string? CIUDAD { get; set; }
    public string? PROVINCIA { get; set; }
    public string? DEPARTAMENTO { get; set; }
    public string? CODIGO_POSTAL { get; set; }
    public string? PAIS { get; set; }
    public string? ES_PREDETERMINADA { get; set; }
}
