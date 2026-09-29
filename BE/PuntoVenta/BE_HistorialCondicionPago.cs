using System;

namespace BE.PuntoVenta;

/// <summary>
/// Entidad para el historial de cambios de condición de pago en SAP (Tabla HISTORIAL_CAMBIO_CONDICION_PAGO)
/// </summary>
public class BE_HistorialCondicionPago
{
    public int ID_HISTORIAL { get; set; }
    public int DOCENTRY_PV { get; set; }
    public int DOCENTRY_SAP { get; set; }
    public int? DOCNUM_SAP { get; set; }
    public int GROUPNUM_ANTERIOR { get; set; }
    public string? CONDICION_PAGO_ANTERIOR { get; set; }
    public int GROUPNUM_NUEVO { get; set; }
    public string? CONDICION_PAGO_NUEVO { get; set; }
    public string? USUARIO { get; set; }
    public DateTime? FECHA_REGISTRO { get; set; }
    public string? FECHA_REGISTRO_TEXTO { get; set; }
    public string? MOTIVO { get; set; }
}
