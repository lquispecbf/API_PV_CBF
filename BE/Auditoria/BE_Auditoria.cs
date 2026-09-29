namespace BE.Auditoria;

public class BE_Auditoria
{
    public long ID_AUDITORIA { get; set; }

    public DateTime FECHA_HORA { get; set; }

    public string USUARIO { get; set; } = string.Empty;

    public int? USUARIO_ID { get; set; }

    public string? IP { get; set; }

    public string? USER_AGENT { get; set; }

    public string? SISTEMA_OPERATIVO { get; set; }

    public string? DISPOSITIVO { get; set; }

    public string? NAVEGADOR { get; set; }

    public string? CONTROLLER { get; set; }

    public string? ACTION { get; set; }

    public string? URL { get; set; }

    public string? QUERY_STRING { get; set; }

    public string? METODO_HTTP { get; set; }

    public string? TIPO_ACCION { get; set; }

    public int CODIGO_RESPUESTA { get; set; }

    public int TIEMPO_MS { get; set; }

    public string? VISTA { get; set; }

    public string? NOMBRE_ARCHIVO { get; set; }

    public string? PARAMETROS { get; set; }

    public string? EXCEPCION { get; set; }

    public int TIENE_EXCEPCION { get; set; }

    public string? FECHA_INICIO { get; set; }

    public string? FECHA_FIN { get; set; }

    public string? BUSCAR_TEXTO { get; set; }

    public string? ORIGEN { get; set; }

    public decimal? LATITUD { get; set; }

    public decimal? LONGITUD { get; set; }
}
