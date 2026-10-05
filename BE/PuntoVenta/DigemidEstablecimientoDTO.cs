using System;
using System.Collections.Generic;

namespace BE.PuntoVenta
{
    public class DigemidEstablecimientoDTO
    {
        public string Item { get; set; } = "";
        public string NumeroRegistro { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string NombreComercial { get; set; } = "";
        public string RazonSocial { get; set; } = "";
        public string Ruc { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Ubigeo { get; set; } = "";
        public string Situacion { get; set; } = "";
        public string Empadronado { get; set; } = "";
        public bool EsActivo => string.Equals(Situacion?.Trim(), "ACTIVO", StringComparison.OrdinalIgnoreCase);
    }

    public class DigemidConsultaResponseDTO
    {
        public bool Success { get; set; }
        public string Ruc { get; set; } = "";
        public int TotalRegistros { get; set; }
        public bool TieneActivos { get; set; }
        public string MensajeResumen { get; set; } = "";
        public string Error { get; set; } = "";
        public string Message => string.IsNullOrWhiteSpace(Error) ? MensajeResumen : Error;
        public List<DigemidEstablecimientoDTO> Establecimientos { get; set; } = new();
        public List<DigemidEstablecimientoDTO> Data => Establecimientos;
    }
}
