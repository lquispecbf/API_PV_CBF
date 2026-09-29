namespace BE.Auditoria;

public class BE_EndpointAuditoria
{
    public long ID_ENDPOINT { get; set; }

    public string CONTROLLER { get; set; } = string.Empty;

    public string ACTION { get; set; } = string.Empty;

    public int ID_TIPO_ACCION { get; set; }

    public string? TIPO_ACCION { get; set; } = string.Empty;

    public bool? AUDITAR { get; set; }

    public bool? ACTIVO { get; set; }

    public string? ORIGEN { get; set; }
}
