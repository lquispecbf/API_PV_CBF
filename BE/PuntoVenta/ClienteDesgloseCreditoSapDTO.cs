namespace BE.PuntoVenta;

public class ClienteDesgloseCreditoSapDTO
{
    public decimal LIMITE { get; set; }
    public decimal BALANCE { get; set; }
    public decimal SALDO_ORDENES { get; set; }
    public decimal SALDO_NOTAS_DEBITO { get; set; }
    public decimal DISPONIBLE { get; set; }
    public decimal MONTO_ORDEN_RETENIDA_SAP { get; set; }
    public decimal DISPONIBLE_EFECTIVO { get; set; }
}
