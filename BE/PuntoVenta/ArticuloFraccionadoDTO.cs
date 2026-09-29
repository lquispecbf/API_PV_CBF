namespace BE.PuntoVenta;

public class ArticuloFraccionadoDTO
{
    public int ID_ARTICULO_FRACCIONADO { get; set; }
    public string? ITEMCODE { get; set; }
    public string? ARTICULO { get; set; }
    public int FRACCIONADO { get; set; }
    public int MONTO_MINIMO { get; set; }
    public bool ESTADO { get; set; }
    public int? USUARIO_CREACION { get; set; }
    public string? FECHAHORA_CREACION { get; set; }
    public int? USUARIO_MODIFICACION { get; set; }
    public string? FECHAHORA_MODIFICACION { get; set; }
}

public class ArticuloFraccionadoFiltroDTO
{
    public string? ITEMCODE { get; set; }
    public bool? ESTADO { get; set; }
}

public class ArticuloFraccionadoIdDTO
{
    public int ID_ARTICULO_FRACCIONADO { get; set; }
}

public class ArticuloFraccionadoGuardarDTO
{
    public int ID_ARTICULO_FRACCIONADO { get; set; }
    public string ITEMCODE { get; set; } = string.Empty;
    public int FRACCIONADO { get; set; }
    public int MONTO_MINIMO { get; set; }
    public bool ESTADO { get; set; }
    public int USUARIO { get; set; }
}

public class ArticuloFraccionadoImportarDTO
{
    public string ITEMCODE { get; set; } = string.Empty;
    public int FRACCIONADO { get; set; }
    public int MONTO_MINIMO { get; set; }
    public int FILA { get; set; }
}

public class ArticuloFraccionadoImportarResultadoDTO
{
    public int Exitosos { get; set; }
    public int Nuevos { get; set; }
    public int Actualizados { get; set; }
    public List<ArticuloFraccionadoImportarErrorDTO> Errores { get; set; } = new();
}

public class ArticuloFraccionadoImportarErrorDTO
{
    public int Fila { get; set; }
    public string Itemcode { get; set; } = string.Empty;
    public int Fraccionado { get; set; }
    public int MontoMinimo { get; set; }
    public string Error { get; set; } = string.Empty;
}
