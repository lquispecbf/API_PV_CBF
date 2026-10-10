using System;

namespace BE.PuntoVenta
{
    public class VentaDigemidDTO
    {
        public int ID_DIGEMID_PV { get; set; }
        public int DOCENTRY { get; set; }
        public int? DOCENTRY_SAP { get; set; }
        public string CARDCODE { get; set; } = string.Empty;
        public string? CARDNAME { get; set; }
        public string LICTRADNUM { get; set; } = string.Empty;
        public DateTime FECHA_CONSULTA { get; set; }
        public DateTime FECHA_CAPTURA { get; set; }
        public string NOMBRE_ARCHIVO { get; set; } = string.Empty;
        public string? NOMBRE_ORIGINAL { get; set; }
        public string ESTADO_SERVICIO_DIGEMID { get; set; } = "EXITOSO";
        public bool TIENE_DATA_DIGEMID { get; set; } = true;
        public string? ESTABLECIMIENTO_JSON { get; set; }
        public bool REQUIERE_REGULARIZACION { get; set; } = false;
        public DateTime? FECHA_REGULARIZACION { get; set; }
        public string? USUARIO_REGULARIZACION { get; set; }
        public string? ESTADO { get; set; } = "A";
        public DateTime? FECHA_CREACION { get; set; }
        public string? USUARIO_CREACION { get; set; }
        public DateTime? FECHA_MODIFICACION { get; set; }
        public string? USUARIO_MODIFICACION { get; set; }

        // Imagen en Base64 enviada desde el frontend para almacenamiento seguro en disco
        public string? IMAGEN_BASE64 { get; set; }
    }

    public class VentaDigemidRegularizarRequestDTO
    {
        public int DOCENTRY { get; set; }
        public int? DOCENTRY_SAP { get; set; }
        public string? CARDCODE { get; set; }
        public string? CARDNAME { get; set; }
        public string LICTRADNUM { get; set; } = string.Empty;
        public DateTime FECHA_CONSULTA { get; set; }
        public DateTime FECHA_CAPTURA { get; set; }
        public string ESTADO_SERVICIO_DIGEMID { get; set; } = "EXITOSO";
        public bool TIENE_DATA_DIGEMID { get; set; } = true;
        public string? ESTABLECIMIENTO_JSON { get; set; }
        public string? IMAGEN_BASE64 { get; set; }
        public string? USUARIO { get; set; }
    }
}
