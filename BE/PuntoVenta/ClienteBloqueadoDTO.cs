namespace BE.PuntoVenta;

public class ClienteBloqueadoDTO
{
    public int ID_CLIENTE_BLOQUEADO { get; set; }
    public string? CARCODE { get; set; }
    public string? CLIENTE { get; set; }
    public string? RUC { get; set; }
    public string? MOTIVO_BLOQUEO { get; set; }
    public bool ESTADO { get; set; }
    public int? USUARIO_CREACION { get; set; }
    public string? FECHAHORA_CREACION { get; set; }
    public int? USUARIO_MODIFICACION { get; set; }
    public string? FECHAHORA_MODIFICACION { get; set; }
}

public class ClienteBloqueadoFiltroDTO
{
    public string? CARCODE { get; set; }
    public bool? ESTADO { get; set; }
}

public class ClienteBloqueadoIdDTO
{
    public int ID_CLIENTE_BLOQUEADO { get; set; }
}

public class ClienteBloqueadoGuardarDTO
{
    public int ID_CLIENTE_BLOQUEADO { get; set; }
    public string CARCODE { get; set; } = string.Empty;
    public string MOTIVO_BLOQUEO { get; set; } = string.Empty;
    public bool ESTADO { get; set; }
    public int USUARIO { get; set; }
}

public class ClienteBloqueadoImportarDTO
{
    public string RUC { get; set; } = string.Empty;
    public string MOTIVO { get; set; } = string.Empty;
    public int FILA { get; set; }
}

public class ClienteBloqueadoImportarResultadoDTO
{
    public int Exitosos { get; set; }
    public List<ClienteBloqueadoImportarErrorDTO> Errores { get; set; } = new();
}

public class ClienteBloqueadoImportarErrorDTO
{
    public int Fila { get; set; }
    public string Ruc { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
