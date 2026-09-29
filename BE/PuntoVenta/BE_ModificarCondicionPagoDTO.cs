using System.Collections.Generic;

namespace BE.PuntoVenta;

/// <summary>
/// Mapeo directo de la consulta a SAP HANA (CBF_SP_PV_OBTENER_ORDEN_CONDICION_PAGO)
/// </summary>
public class OrdenCondicionPagoSapDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public string? DocStatus { get; set; }
    public string? Canceled { get; set; }
    public string? USophEnwms { get; set; }
    public string? CardCode { get; set; }
    public string? CardName { get; set; }
    public string? LicTradNum { get; set; }
    public string? EstadoSap { get; set; }
    public int GroupNum { get; set; }
    public string? PymntGroup { get; set; }
}

/// <summary>
/// DTO con datos consolidados para renderizar el modal de Modificar Condición de Pago
/// </summary>
public class ModalCondicionPagoDataDTO
{
    public int DocEntry { get; set; }
    public int DocEntrySap { get; set; }
    public int DocNum { get; set; }
    public string? CardCode { get; set; }
    public string? CardName { get; set; }
    public string? Ruc { get; set; }
    public string? EstadoSap { get; set; }
    public int CondicionPagoActualCodigo { get; set; }
    public string? CondicionPagoActualNombre { get; set; }
    public string? RolUsuario { get; set; }
    public bool PuedeModificar { get; set; } = true;
    public string? MotivoBloqueo { get; set; }
    public List<FormaPagoSapDTO> OpcionesPermitidas { get; set; } = new();
    public List<BE_HistorialCondicionPago> Historial { get; set; } = new();
}

/// <summary>
/// Solicitud enviada desde el frontend para actualizar la condición de pago
/// </summary>
public class ActualizarCondicionPagoRequestDTO
{
    public int DocEntry { get; set; }
    public int DocEntrySap { get; set; }
    public int NuevoGroupNum { get; set; }
    public int ActualGroupNum { get; set; }
    public string? CondicionPagoActualNombre { get; set; }
    public string? CondicionPagoNuevoNombre { get; set; }
    public string? Motivo { get; set; }
}
