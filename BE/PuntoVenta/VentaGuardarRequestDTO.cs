namespace BE.PuntoVenta;

public class VentaGuardarRequestDTO
{
    public string OBJTYPE { get; set; } = "OV";
    public string? WHSCODE { get; set; }
    public int BINCODE { get; set; }
    public string? DOCDATE { get; set; }
    public string? CARDCODE { get; set; }
    public string? CARDNAME { get; set; }
    public int SLPCODE { get; set; }
    public string? SLPNAME { get; set; }
    public string? LICTRADNUM { get; set; }
    public string? COMMENTS { get; set; }
    public string? ORIN_RECORD { get; set; } = string.Empty;
    public decimal ORIN_TOTAL { get; set; }
    public decimal IMP_DELIVERY { get; set; }
    public decimal IMP_DSCTO { get; set; }
    public decimal IMP_NET { get; set; }
    public string? DELIVERY_DATE { get; set; }
    public string? DELIVERY_TIME { get; set; }
    public string? DELIVERY_PLACE { get; set; }
    public string? DELIVERY_ADDRESS { get; set; }
    public string? DELIVERY_ADDRESS2 { get; set; }
    public string? DELIVERY_POINT { get; set; }
    public string? INVOICE_PLACE { get; set; }
    public string? INVOICE_ADDRESS { get; set; }
    public string? PRICE_LIST { get; set; }
    public string? DOCTYPE { get; set; }
    public string? PAYFORM { get; set; }
    public string? AGENCY_DATA { get; set; }
    public string? PACKING_TYPE { get; set; }
    public string? SEND_MODE { get; set; }
    public string? SEND_NDOC { get; set; }
    public string? SEND_NAME { get; set; }
    public string? SEND_PHONE { get; set; }
    public string? SEND_PLACE { get; set; }
    public string? DOCSTATUS { get; set; } = "Z";
    public string? DOCSTATUS_ORIGINAL { get; set; }
    public int DOCENTRY { get; set; }

    public List<VentaDetalleDTO> DETALLE { get; set; } = new();
    public List<int> NOTAS_CREDITO { get; set; } = new();
}
