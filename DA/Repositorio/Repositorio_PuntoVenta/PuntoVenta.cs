using BE.PuntoVenta;
using DA.AccesoDatos;
using DA.API;
using DA.API.v2;
using DA.Configuracion;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;

namespace DA.Repositorio.Repositorio_PuntoVenta;

public class PuntoVenta : IPuntoVenta
{
    private readonly IHanaExecutor _hana;
    private readonly ISqlExecutor _sql;
    private readonly ISapServiceClient _sapClient;
    private readonly string _cadenaSQLPOS;

    public PuntoVenta(IHanaExecutor hana, ISqlExecutor sql, IOptions<ConfiguracionConexion> config, ISapServiceClient sapClient)
    {
        _hana = hana;
        _sql = sql;
        _sapClient = sapClient ?? throw new ArgumentNullException(nameof(sapClient));
        _cadenaSQLPOS = config.Value.CadenaSQLPOS
            ?? throw new ArgumentNullException(nameof(config.Value.CadenaSQLPOS));
    }
    public async Task<List<ClienteBusquedaSapDTO>> BuscarCliente(string term)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_CLIENTE", 2);

        return await _hana.QueryListAsync(
            commandText,
            MapClienteBusquedaSap,
            CommandType.Text,
            _hana.CreateParameter("p1", string.IsNullOrWhiteSpace(term) ? DBNull.Value : term.Trim()),
            _hana.CreateParameter("p2", "ACTIVO")
        );
    }
    private static ClienteBusquedaSapDTO MapClienteBusquedaSap(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordRuc = reader.GetOrdinal("RUC");
        int ordEstado = reader.GetOrdinal("ESTADO");
        int ordCondicionPago = reader.GetOrdinal("CONDICION_PAGO");
        int ordLimiteCredito = reader.GetOrdinal("LIMITE_CREDITO");
        int ordSlpCode = reader.GetOrdinal("SLPCODE");
        int ordSlpName = reader.GetOrdinal("SLPNAME");

        return new ClienteBusquedaSapDTO
        {
            CODIGO_CLIENTE = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            CLIENTE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            RUC = reader.IsDBNull(ordRuc) ? null : reader.GetString(ordRuc),
            ESTADO_CLIENTE = reader.IsDBNull(ordEstado) ? null : reader.GetString(ordEstado),
            CONDICION_PAGO = reader.IsDBNull(ordCondicionPago) ? null : reader.GetString(ordCondicionPago),
            LIMITE_CREDITO = reader.IsDBNull(ordLimiteCredito) ? 0 : Convert.ToDecimal(reader.GetValue(ordLimiteCredito)),
            SLPCODE = reader.IsDBNull(ordSlpCode) ? 0 : reader.GetInt32(ordSlpCode),
            SLPNAME = reader.IsDBNull(ordSlpName) ? null : reader.GetString(ordSlpName),
            LISTA_PRECIO = TryGetString(reader, "LISTA_PRECIO")
        };
    }
    public async Task<List<ListaPrecioSapDTO>> BuscarListaPrecios(string? nombreBusqueda)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_LISTA_PRECIOS", 2);

        return await _hana.QueryListAsync(
            commandText,
            MapListaPrecioSap,
            CommandType.Text,
            _hana.CreateParameter("p1", string.IsNullOrWhiteSpace(nombreBusqueda) ? DBNull.Value : nombreBusqueda.Trim()),
            _hana.CreateParameter("p2", "ACTIVO")
        );
    }
    private static ListaPrecioSapDTO MapListaPrecioSap(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordEstado = reader.GetOrdinal("ESTADO");

        return new ListaPrecioSapDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? 0 : Convert.ToInt32(reader.GetValue(ordCodigo)),
            NOMBRE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            ESTADO = reader.IsDBNull(ordEstado) ? null : reader.GetString(ordEstado)
        };
    }
    public async Task<List<VendedorSapDTO>> BuscarVendedores(int codigoVendedorSap)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_VENDEDORES", 2);

        return await _hana.QueryListAsync(
            commandText,
            MapVendedorSap,
            CommandType.Text,
            _hana.CreateParameter("p1", codigoVendedorSap),
            _hana.CreateParameter("p2", "ACTIVO")
        );
    }
    private static VendedorSapDTO MapVendedorSap(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordEstado = reader.GetOrdinal("ESTADO");

        return new VendedorSapDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? 0 : Convert.ToInt32(reader.GetValue(ordCodigo)),
            NOMBRE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            ESTADO = reader.IsDBNull(ordEstado) ? null : reader.GetString(ordEstado)
        };
    }
    public async Task<List<AlmacenSapDTO>> BuscarAlmacenes(string? nombreBusqueda)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_ALMACENES", 2);

        return await _hana.QueryListAsync(
            commandText,
            MapAlmacenSap,
            CommandType.Text,
            _hana.CreateParameter("p1", string.IsNullOrWhiteSpace(nombreBusqueda) ? DBNull.Value : nombreBusqueda.Trim()),
            _hana.CreateParameter("p2", "ACTIVO")
        );
    }
    private static AlmacenSapDTO MapAlmacenSap(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordEstado = reader.GetOrdinal("ESTADO");

        return new AlmacenSapDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            NOMBRE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            ESTADO = reader.IsDBNull(ordEstado) ? null : reader.GetString(ordEstado)
        };
    }
    public async Task<List<StockPorAlmacenDTO>> BuscarStockPorAlmacen(StockPorAlmacenFiltroDTO filtro)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_STOCK_ALMACEN", 3);

        return await _hana.QueryListAsync(
            commandText,
            MapStockPorAlmacen,
            CommandType.Text,
            _hana.CreateParameter("p1", string.IsNullOrWhiteSpace(filtro.TEXTO_BUSQUEDA) ? DBNull.Value : filtro.TEXTO_BUSQUEDA.Trim()),
            _hana.CreateParameter("p2", filtro.CODIGO_LISTA_PRECIO),
            _hana.CreateParameter("p3", string.IsNullOrWhiteSpace(filtro.CODIGO_ALMACEN) ? DBNull.Value : filtro.CODIGO_ALMACEN.Trim())
        );
    }
    private static StockPorAlmacenDTO MapStockPorAlmacen(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordDescripcion = reader.GetOrdinal("DESCRIPCION");
        int ordUmd = reader.GetOrdinal("UMD");
        int ordLaboratorio = reader.GetOrdinal("LABORATORIO");
        int ordPrecio = reader.GetOrdinal("PRECIO");
        int ordPrecioCaja = reader.GetOrdinal("PRECIO_CAJA");
        int ordStock = reader.GetOrdinal("STOCK");
        int ordFechaVencimiento = reader.GetOrdinal("FECHA_VENCIMIENTO");
        int ordPrincipioActivo = reader.GetOrdinal("PRINCIPIO_ACTIVO");
        int ordEstadoSku = reader.GetOrdinal("ESTADO_SKU");
        int ordObservacion = reader.GetOrdinal("OBSERVACION");

        return new StockPorAlmacenDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            DESCRIPCION = reader.IsDBNull(ordDescripcion) ? null : reader.GetString(ordDescripcion),
            UMD = reader.IsDBNull(ordUmd) ? null : reader.GetString(ordUmd),
            LABORATORIO = reader.IsDBNull(ordLaboratorio) ? null : reader.GetString(ordLaboratorio),
            PRECIO = reader.IsDBNull(ordPrecio) ? 0 : reader.GetDecimal(ordPrecio),
            PRECIO_CAJA = reader.IsDBNull(ordPrecioCaja) ? 0 : reader.GetDecimal(ordPrecioCaja),
            STOCK = reader.IsDBNull(ordStock) ? 0 : reader.GetDecimal(ordStock),
            FECHA_VENCIMIENTO = reader.IsDBNull(ordFechaVencimiento) ? null : reader.GetValue(ordFechaVencimiento).ToString(),
            PRINCIPIO_ACTIVO = reader.IsDBNull(ordPrincipioActivo) ? null : reader.GetString(ordPrincipioActivo),
            ESTADO_SKU = reader.IsDBNull(ordEstadoSku) ? null : reader.GetString(ordEstadoSku),
            OBSERVACION = reader.IsDBNull(ordObservacion) ? null : reader.GetString(ordObservacion)
        };
    }
    public async Task<List<DireccionClienteSapDTO>> BuscarDireccionesCliente(string codigoCliente)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_DIRECCIONES_CLIENTE", 1);

        return await _hana.QueryListAsync(
            commandText,
            MapDireccionClienteSap,
            CommandType.Text,
            _hana.CreateParameter("p1", codigoCliente)
        );
    }
    private static DireccionClienteSapDTO MapDireccionClienteSap(DbDataReader reader)
    {
        int ordCliente = reader.GetOrdinal("CODIGO_CLIENTE");
        int ordDireccion = reader.GetOrdinal("CODIGO_DIRECCION");
        int ordTipo = reader.GetOrdinal("TIPO_DIRECCION");
        int ordDir = reader.GetOrdinal("DIRECCION");
        int ordBarrio = reader.GetOrdinal("BARRIO");
        int ordCiudad = reader.GetOrdinal("CIUDAD");
        int ordProvincia = reader.GetOrdinal("PROVINCIA");
        int ordDepartamento = reader.GetOrdinal("DEPARTAMENTO");
        int ordCodigoPostal = reader.GetOrdinal("CODIGO_POSTAL");
        int ordPais = reader.GetOrdinal("PAIS");

        int ordPredeterminada = -1;
        try { ordPredeterminada = reader.GetOrdinal("ES_PREDETERMINADA"); } catch { }

        return new DireccionClienteSapDTO
        {
            CODIGO_CLIENTE = reader.IsDBNull(ordCliente) ? null : reader.GetString(ordCliente),
            CODIGO_DIRECCION = reader.IsDBNull(ordDireccion) ? null : reader.GetString(ordDireccion),
            TIPO_DIRECCION = reader.IsDBNull(ordTipo) ? null : reader.GetString(ordTipo),
            DIRECCION = reader.IsDBNull(ordDir) ? null : reader.GetString(ordDir),
            BARRIO = reader.IsDBNull(ordBarrio) ? null : reader.GetString(ordBarrio),
            CIUDAD = reader.IsDBNull(ordCiudad) ? null : reader.GetString(ordCiudad),
            PROVINCIA = reader.IsDBNull(ordProvincia) ? null : reader.GetString(ordProvincia),
            DEPARTAMENTO = reader.IsDBNull(ordDepartamento) ? null : reader.GetString(ordDepartamento),
            CODIGO_POSTAL = reader.IsDBNull(ordCodigoPostal) ? null : reader.GetString(ordCodigoPostal),
            PAIS = reader.IsDBNull(ordPais) ? null : reader.GetString(ordPais),
            ES_PREDETERMINADA = (ordPredeterminada >= 0 && !reader.IsDBNull(ordPredeterminada))
                ? reader.GetString(ordPredeterminada)
                : "N"
        };
    }
    public async Task<List<TipoEmbalajeSapDTO>> BuscarTiposEmbalaje()
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_TIPO_EMBALAJE", 0);

        return await _hana.QueryListAsync(
            commandText,
            MapTipoEmbalajeSap,
            CommandType.Text
        );
    }
    private static TipoEmbalajeSapDTO MapTipoEmbalajeSap(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordEstado = reader.GetOrdinal("ESTADO");

        return new TipoEmbalajeSapDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            NOMBRE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            ESTADO = reader.IsDBNull(ordEstado) ? null : reader.GetString(ordEstado)
        };
    }
    public async Task<List<LugarEntregaSapDTO>> BuscarLugaresEntrega()
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_LUGAR_ENTREGA", 0);

        return await _hana.QueryListAsync(
            commandText,
            MapLugarEntregaSap,
            CommandType.Text
        );
    }
    private static LugarEntregaSapDTO MapLugarEntregaSap(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordEstado = reader.GetOrdinal("ESTADO");

        return new LugarEntregaSapDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            NOMBRE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            ESTADO = reader.IsDBNull(ordEstado) ? null : reader.GetString(ordEstado)
        };
    }
    public async Task<List<HoraEntregaSapDTO>> BuscarHorasEntrega()
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_HORAS_ENTREGA", 0);

        return await _hana.QueryListAsync(
            commandText,
            MapHoraEntregaSap,
            CommandType.Text
        );
    }
    private static HoraEntregaSapDTO MapHoraEntregaSap(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordEstado = reader.GetOrdinal("ESTADO");

        return new HoraEntregaSapDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            NOMBRE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            ESTADO = reader.IsDBNull(ordEstado) ? null : reader.GetString(ordEstado)
        };
    }
    public async Task<List<ModoEnvioSapDTO>> BuscarModosEnvio()
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_MODOS_ENVIO", 0);

        return await _hana.QueryListAsync(
            commandText,
            MapModoEnvioSap,
            CommandType.Text
        );
    }
    private static ModoEnvioSapDTO MapModoEnvioSap(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordEstado = reader.GetOrdinal("ESTADO");

        return new ModoEnvioSapDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            NOMBRE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            ESTADO = reader.IsDBNull(ordEstado) ? null : reader.GetString(ordEstado)
        };
    }
    public async Task<List<FormaPagoSapDTO>> BuscarFormasPago(string? condicionPago)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_FORMAS_PAGO", 1);

        return await _hana.QueryListAsync(
            commandText,
            MapFormaPagoSap,
            CommandType.Text,
            _hana.CreateParameter("p1", string.IsNullOrWhiteSpace(condicionPago) ? DBNull.Value : condicionPago.Trim())
        );
    }
    private static FormaPagoSapDTO MapFormaPagoSap(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordEstado = reader.GetOrdinal("ESTADO");

        return new FormaPagoSapDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? 0 : (int)reader.GetInt16(ordCodigo),
            NOMBRE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            ESTADO = reader.IsDBNull(ordEstado) ? null : reader.GetString(ordEstado)
        };
    }
    public async Task<List<TipoComprobanteSapDTO>> BuscarTiposComprobante(string? nombreBusqueda)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_TIPOS_COMPROBANTE", 2);

        return await _hana.QueryListAsync(
            commandText,
            MapTipoComprobanteSap,
            CommandType.Text,
            _hana.CreateParameter("p1", string.IsNullOrWhiteSpace(nombreBusqueda) ? DBNull.Value : nombreBusqueda.Trim()),
            _hana.CreateParameter("p2", "ACTIVO")
        );
    }
    private static TipoComprobanteSapDTO MapTipoComprobanteSap(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordEstado = reader.GetOrdinal("ESTADO");

        return new TipoComprobanteSapDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            NOMBRE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            ESTADO = reader.IsDBNull(ordEstado) ? null : reader.GetString(ordEstado)
        };
    }
    public async Task<List<NotaCreditoClienteSapDTO>> BuscarNotasCreditoCliente(string codigoCliente)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_NOTAS_CREDITO_CLIENTE", 1);

        return await _hana.QueryListAsync(
            commandText,
            MapNotaCreditoClienteSap,
            CommandType.Text,
            _hana.CreateParameter("p1", codigoCliente)
        );
    }

    private static NotaCreditoClienteSapDTO MapNotaCreditoClienteSap(DbDataReader reader)
    {
        int ordDocEntry = reader.GetOrdinal("DOCENTRY");
        int ordDocNum = reader.GetOrdinal("DOCNUM");
        int ordCodigoCliente = reader.GetOrdinal("CODIGO_CLIENTE");
        int ordFecha = reader.GetOrdinal("FECHA");
        int ordTotal = reader.GetOrdinal("TOTAL");
        int ordMoneda = reader.GetOrdinal("MONEDA");

        return new NotaCreditoClienteSapDTO
        {
            DOCENTRY = reader.IsDBNull(ordDocEntry) ? 0 : Convert.ToInt32(reader[ordDocEntry]),
            DOCNUM = reader.IsDBNull(ordDocNum) ? 0 : Convert.ToInt32(reader[ordDocNum]),
            CODIGO_CLIENTE = reader.IsDBNull(ordCodigoCliente) ? null : Convert.ToString(reader[ordCodigoCliente]),
            FECHA = reader.IsDBNull(ordFecha) ? null : Convert.ToString(reader[ordFecha]),
            TOTAL = reader.IsDBNull(ordTotal) ? 0 : Convert.ToDecimal(reader[ordTotal]),
            MONEDA = reader.IsDBNull(ordMoneda) ? null : Convert.ToString(reader[ordMoneda])
        };
    }
    public async Task<List<ClienteDesgloseCreditoSapDTO>> BuscarDesgloseCreditoCliente(string codigoCliente, int? docEntrySap = null)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_DESGLOSE_CREDITO_CLIENTE", 1);

        var lista = await _hana.QueryListAsync(
            commandText,
            MapClienteDesgloseCreditoSap,
            CommandType.Text,
            _hana.CreateParameter("p1", codigoCliente)
        );

        if (lista.Count > 0)
        {
            var item = lista[0];
            decimal montoRetenido = 0;

            if (docEntrySap.HasValue && docEntrySap.Value > 0)
            {
                try
                {
                    const string sqlCheckOrdr = @"
                        SELECT IFNULL(""DocTotal"", 0) AS ""DocTotal""
                        FROM ORDR 
                        WHERE ""DocEntry"" = :pDocEntrySap 
                          AND ""DocStatus"" = 'O' 
                          AND IFNULL(""CANCELED"", 'N') = 'N'";

                    var montos = await _hana.QueryListAsync<decimal>(
                        sqlCheckOrdr,
                        r => r.IsDBNull(0) ? 0 : Convert.ToDecimal(r.GetValue(0)),
                        CommandType.Text,
                        _hana.CreateParameter("pDocEntrySap", docEntrySap.Value)
                    );

                    if (montos.Count > 0 && montos[0] > 0)
                    {
                        montoRetenido = montos[0];
                    }
                }
                catch
                {
                    montoRetenido = 0;
                }
            }

            item.MONTO_ORDEN_RETENIDA_SAP = montoRetenido;
            item.DISPONIBLE_EFECTIVO = item.DISPONIBLE + montoRetenido;
        }

        return lista;
    }
    private static ClienteDesgloseCreditoSapDTO MapClienteDesgloseCreditoSap(DbDataReader reader)
    {
        int ordLimite = reader.GetOrdinal("LIMITE");
        int ordBalance = reader.GetOrdinal("BALANCE");
        int ordSaldoOrdenes = reader.GetOrdinal("SALDO_ORDENES");
        int ordSaldoNotasDebito = reader.GetOrdinal("SALDO_NOTAS_DEBITO");
        int ordDisponible = reader.GetOrdinal("DISPONIBLE");

        var disp = reader.IsDBNull(ordDisponible) ? 0 : Convert.ToDecimal(reader[ordDisponible]);

        return new ClienteDesgloseCreditoSapDTO
        {
            LIMITE = reader.IsDBNull(ordLimite) ? 0 : Convert.ToDecimal(reader[ordLimite]),
            BALANCE = reader.IsDBNull(ordBalance) ? 0 : Convert.ToDecimal(reader[ordBalance]),
            SALDO_ORDENES = reader.IsDBNull(ordSaldoOrdenes) ? 0 : Convert.ToDecimal(reader[ordSaldoOrdenes]),
            SALDO_NOTAS_DEBITO = reader.IsDBNull(ordSaldoNotasDebito) ? 0 : Convert.ToDecimal(reader[ordSaldoNotasDebito]),
            DISPONIBLE = disp,
            MONTO_ORDEN_RETENIDA_SAP = 0,
            DISPONIBLE_EFECTIVO = disp
        };
    }
    public async Task<List<ArticuloAutocompleteDTO>> BuscarArticulosAutocomplete(string? textoBusqueda, int codigoListaPrecio, string codigoAlmacen)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_ARTICULOS_AUTOCOMPLETE", 3);

        return await _hana.QueryListAsync(
            commandText,
            MapArticuloAutocomplete,
            CommandType.Text,
            _hana.CreateParameter("p1", string.IsNullOrWhiteSpace(textoBusqueda) ? DBNull.Value : textoBusqueda.Trim()),
            _hana.CreateParameter("p2", codigoListaPrecio),
            _hana.CreateParameter("p3", codigoAlmacen)
        );
    }
    private static ArticuloAutocompleteDTO MapArticuloAutocomplete(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordDescripcion = reader.GetOrdinal("DESCRIPCION");

        return new ArticuloAutocompleteDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            DESCRIPCION = reader.IsDBNull(ordDescripcion) ? null : reader.GetString(ordDescripcion)
        };
    }
    public async Task<List<ArticuloBusquedaSapDTO>> BuscarArticulosDescripcion(string? descripcion, string? codigo, string? laboratorio, string? principioActivo, int codigoListaPrecio, string codigoAlmacen, string? titularRs = null)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_ARTICULOS_DESCRIPCION", 7);

        return await _hana.QueryListAsync(
            commandText,
            MapArticuloBusquedaSap,
            CommandType.Text,
            _hana.CreateParameter("p1", string.IsNullOrWhiteSpace(descripcion) ? DBNull.Value : descripcion.Trim()),
            _hana.CreateParameter("p2", string.IsNullOrWhiteSpace(codigo) ? DBNull.Value : codigo.Trim()),
            _hana.CreateParameter("p3", string.IsNullOrWhiteSpace(laboratorio) ? DBNull.Value : laboratorio.Trim()),
            _hana.CreateParameter("p4", string.IsNullOrWhiteSpace(principioActivo) ? DBNull.Value : principioActivo.Trim()),
            _hana.CreateParameter("p5", codigoListaPrecio),
            _hana.CreateParameter("p6", codigoAlmacen),
            _hana.CreateParameter("p7", string.IsNullOrWhiteSpace(titularRs) ? DBNull.Value : titularRs.Trim())
        );
    }

    public async Task<List<string>> ListarTitularesRs()
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_LISTAR_TITULARES_RS", 0);

        return await _hana.QueryListAsync(
            commandText,
            reader => reader.IsDBNull(0) ? string.Empty : (reader.GetValue(0)?.ToString() ?? string.Empty),
            CommandType.Text
        );
    }

    public async Task<List<ArticuloBusquedaSapDTO>> BuscarArticulosPorCodigo(string codigoArticulo, int codigoListaPrecio, string codigoAlmacen)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_ARTICULO_POR_CODIGO", 3);

        return await _hana.QueryListAsync(
            commandText,
            MapArticuloBusquedaSap,
            CommandType.Text,
            _hana.CreateParameter("p1", codigoArticulo),
            _hana.CreateParameter("p2", codigoListaPrecio),
            _hana.CreateParameter("p3", codigoAlmacen)
        );
    }
    public async Task<ArticuloDetalleVentaDTO> BuscarDetalleArticuloVenta(string codigoArticulo, int codigoListaPrecio, string codigoAlmacen, string codigoCliente, int? codigoUmd = null)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_ARTICULO_VENTA_RAPIDO", 3);

        var articulo = await _hana.QuerySingleOrDefaultAsync(
            commandText,
            MapArticuloBusquedaSap,
            CommandType.Text,
            _hana.CreateParameter("p1", codigoArticulo.Trim().ToUpperInvariant()),
            _hana.CreateParameter("p2", codigoListaPrecio),
            _hana.CreateParameter("p3", codigoAlmacen)
        );

        if (articulo is null)
            return new ArticuloDetalleVentaDTO();

        var umds = await BuscarUmdArticuloConFallback(codigoArticulo);
        var umdPromo = codigoUmd.GetValueOrDefault();

        if (umdPromo <= 0)
        {
            umdPromo = umds.FirstOrDefault(u => string.Equals(u.ES_PREDETERMINADA, "Y", StringComparison.OrdinalIgnoreCase))?.CODIGO_UMD
                ?? umds.FirstOrDefault()?.CODIGO_UMD
                ?? 0;
        }

        var promos = umdPromo > 0
            ? await BuscarPromoArticuloConFallback(codigoArticulo, codigoCliente, codigoListaPrecio, umdPromo)
            : new List<PromoArticuloSapDTO>();

        var fraccionado = await ValidarArticuloFraccionado(codigoArticulo);

        return new ArticuloDetalleVentaDTO
        {
            ARTICULO = articulo,
            UMDS = umds,
            PROMOS = promos,
            FRACCIONADO = fraccionado
        };
    }
    public async Task<Dictionary<string, ArticuloDetalleVentaDTO>> BuscarDetalleArticulosVentaBatch(
        List<ArticuloDetalleVentaItemRequestDTO> items,
        int codigoListaPrecio,
        string codigoAlmacen,
        string codigoCliente)
    {
        var resultado = new Dictionary<string, ArticuloDetalleVentaDTO>(StringComparer.OrdinalIgnoreCase);
        if (items is null || items.Count == 0)
            return resultado;

        var articulosUnicos = items
            .Where(i => !string.IsNullOrWhiteSpace(i.CodigoArticulo))
            .GroupBy(i => i.CodigoArticulo.Trim().ToUpperInvariant())
            .Select(g => g.First())
            .ToList();

        if (articulosUnicos.Count == 0)
            return resultado;

        try
        {
            var listaCodigos = articulosUnicos.Select(a => a.CodigoArticulo.Trim().ToUpperInvariant()).ToList();
            var articulosHana = new List<ArticuloBusquedaSapDTO>();

            var chunks = listaCodigos.Chunk(500).ToList();
            var tasksHana = chunks.Select(async chunk =>
            {
                string strCodigos = string.Join(",", chunk);
                var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_ARTICULOS_VENTA_BATCH", 3);
                return await _hana.QueryListAsync(
                    commandText,
                    MapArticuloBusquedaSap,
                    CommandType.Text,
                    _hana.CreateParameter("p1", strCodigos),
                    _hana.CreateParameter("p2", codigoListaPrecio),
                    _hana.CreateParameter("p3", codigoAlmacen)
                );
            });

            var chunkResults = await Task.WhenAll(tasksHana);
            foreach (var chunkResult in chunkResults)
            {
                if (chunkResult != null)
                {
                    articulosHana.AddRange(chunkResult);
                }
            }

            if (articulosHana.Count > 0)
            {
                var dictUmds = await BuscarUmdsArticulosBatch(listaCodigos);
                var dictPromos = await BuscarPromosArticulosBatch(listaCodigos, codigoCliente, codigoListaPrecio);
                var listaFraccionados = await ValidarArticulosFraccionadosBatch(listaCodigos);
                var dictFraccionados = listaFraccionados
                    .Where(f => !string.IsNullOrWhiteSpace(f.ITEMCODE))
                    .ToDictionary(f => f.ITEMCODE!.Trim().ToUpperInvariant(), f => f, StringComparer.OrdinalIgnoreCase);

                foreach (var art in articulosHana)
                {
                    if (string.IsNullOrWhiteSpace(art.CODIGO)) continue;
                    string code = art.CODIGO.Trim().ToUpperInvariant();

                    dictUmds.TryGetValue(code, out var umds);
                    umds ??= new List<UnidadMedidaArticuloSapDTO>();

                    dictPromos.TryGetValue(code, out var promos);
                    promos ??= new List<PromoArticuloSapDTO>();

                    dictFraccionados.TryGetValue(code, out var fraccionado);

                    resultado[code] = new ArticuloDetalleVentaDTO
                    {
                        ARTICULO = art,
                        UMDS = umds,
                        PROMOS = promos,
                        FRACCIONADO = fraccionado
                    };
                }

                return resultado;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PuntoVenta] Fallback en BuscarDetalleArticulosVentaBatch: {ex.Message}");
        }

        using (var semaphore = new SemaphoreSlim(15))
        {
            var tasks = articulosUnicos.Select(async item =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var detalle = await BuscarDetalleArticuloVenta(
                        item.CodigoArticulo,
                        codigoListaPrecio,
                        codigoAlmacen,
                        codigoCliente,
                        item.CodigoUmd
                    );
                    return (Codigo: item.CodigoArticulo.Trim().ToUpperInvariant(), Detalle: detalle);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var resArray = await Task.WhenAll(tasks);
            foreach (var r in resArray)
            {
                if (r.Detalle != null && r.Detalle.ARTICULO != null)
                {
                    resultado[r.Codigo] = r.Detalle;
                }
            }
        }

        return resultado;
    }
    private async Task<List<UnidadMedidaArticuloSapDTO>> BuscarUmdArticuloConFallback(string codigoArticulo)
    {
        try
        {
            return await BuscarUmdArticulo(codigoArticulo);
        }
        catch
        {
            return new List<UnidadMedidaArticuloSapDTO>();
        }
    }

    private async Task<Dictionary<string, List<UnidadMedidaArticuloSapDTO>>> BuscarUmdsArticulosBatch(List<string> codigosArticulos)
    {
        var dict = new Dictionary<string, List<UnidadMedidaArticuloSapDTO>>(StringComparer.OrdinalIgnoreCase);
        if (codigosArticulos == null || codigosArticulos.Count == 0) return dict;

        var codigosUnicos = codigosArticulos
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        try
        {
            var chunks = codigosUnicos.Chunk(500).ToList();
            var tasksUmd = chunks.Select(async chunk =>
            {
                string strCodigos = string.Join(",", chunk);
                var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_UMDS_BATCH", 1);
                return await _hana.QueryListAsync(
                    commandText,
                    reader =>
                    {
                        string itemcode = reader.GetString(reader.GetOrdinal("ITEMCODE")).Trim().ToUpperInvariant();
                        var dto = new UnidadMedidaArticuloSapDTO
                        {
                            CODIGO_UMD = reader.GetInt32(reader.GetOrdinal("CODIGO_UMD")),
                            CODIGO = reader.IsDBNull(reader.GetOrdinal("CODIGO")) ? null : reader.GetString(reader.GetOrdinal("CODIGO")),
                            NOMBRE = reader.IsDBNull(reader.GetOrdinal("NOMBRE")) ? null : reader.GetString(reader.GetOrdinal("NOMBRE")),
                            FACTOR = reader.IsDBNull(reader.GetOrdinal("FACTOR")) ? 1 : reader.GetDecimal(reader.GetOrdinal("FACTOR")),
                            ES_PREDETERMINADA = reader.IsDBNull(reader.GetOrdinal("ES_PREDETERMINADA")) ? "N" : reader.GetString(reader.GetOrdinal("ES_PREDETERMINADA"))
                        };
                        return (ItemCode: itemcode, UMD: dto);
                    },
                    CommandType.Text,
                    _hana.CreateParameter("p1", strCodigos)
                );
            });

            var chunkResults = await Task.WhenAll(tasksUmd);
            foreach (var lista in chunkResults)
            {
                if (lista != null)
                {
                    foreach (var item in lista)
                    {
                        if (!dict.TryGetValue(item.ItemCode, out var listUmds))
                        {
                            listUmds = new List<UnidadMedidaArticuloSapDTO>();
                            dict[item.ItemCode] = listUmds;
                        }
                        listUmds.Add(item.UMD);
                    }
                }
            }

            if (dict.Count > 0) return dict;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PuntoVenta] Fallback en BuscarUmdsArticulosBatch: {ex.Message}");
        }

        using var sem = new SemaphoreSlim(15);
        var tasks = codigosUnicos.Select(async code =>
        {
            await sem.WaitAsync();
            try
            {
                var umds = await BuscarUmdArticuloConFallback(code);
                lock (dict) { dict[code] = umds; }
            }
            finally
            {
                sem.Release();
            }
        });
        await Task.WhenAll(tasks);
        return dict;
    }
    public async Task<Dictionary<string, List<PromoArticuloSapDTO>>> BuscarPromosArticulosBatch(List<string> codigos, string codigoCliente, int codigoListaPrecio)
    {
        var dict = new Dictionary<string, List<PromoArticuloSapDTO>>(StringComparer.OrdinalIgnoreCase);
        if (codigos is null || codigos.Count == 0) return dict;

        var codigosUnicos = codigos.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim().ToUpperInvariant()).Distinct().ToList();
        if (codigosUnicos.Count == 0) return dict;

        try
        {
            var chunks = codigosUnicos.Chunk(500).ToList();
            var tasksPromo = chunks.Select(async chunk =>
            {
                string strCodigos = string.Join(",", chunk);
                var commandText = _hana.BuildProcedureCall("CBF_SP_PV_GET_PROMO_BATCH", 3);
                return await _hana.QueryListAsync(
                    commandText,
                    MapPromoArticuloSap,
                    CommandType.Text,
                    _hana.CreateParameter("p1", strCodigos),
                    _hana.CreateParameter("p2", codigoCliente),
                    _hana.CreateParameter("p3", codigoListaPrecio)
                );
            });

            var chunkResults = await Task.WhenAll(tasksPromo);
            foreach (var chunkResult in chunkResults)
            {
                if (chunkResult != null)
                {
                    foreach (var promo in chunkResult)
                    {
                        if (string.IsNullOrWhiteSpace(promo.CODIGO_ARTICULO)) continue;
                        string cod = promo.CODIGO_ARTICULO.Trim().ToUpperInvariant();
                        if (!dict.TryGetValue(cod, out var list))
                        {
                            list = new List<PromoArticuloSapDTO>();
                            dict[cod] = list;
                        }
                        list.Add(promo);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PuntoVenta] Fallback en BuscarPromosArticulosBatch: {ex.Message}");
        }

        return dict;
    }

    private async Task<List<PromoArticuloSapDTO>> BuscarPromoArticuloConFallback(string codigoArticulo, string codigoCliente, int codigoListaPrecio, int codigoUmd)
    {
        try
        {
            return await BuscarPromoArticulo(codigoArticulo, codigoCliente, codigoListaPrecio, codigoUmd);
        }
        catch
        {
            return new List<PromoArticuloSapDTO>();
        }
    }
    private static ArticuloBusquedaSapDTO MapArticuloBusquedaSap(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordDescripcion = reader.GetOrdinal("DESCRIPCION");
        int ordUmd = reader.GetOrdinal("UMD");
        int ordPrecio = reader.GetOrdinal("PRECIO");
        int ordPrecioCaja = reader.GetOrdinal("PRECIO_CAJA");
        int ordStock = reader.GetOrdinal("STOCK");
        int ordStockCajas = reader.GetOrdinal("STOCK_CAJAS");
        int ordGestionaLote = reader.GetOrdinal("GESTIONA_LOTE");
        int ordLoteProximo = reader.GetOrdinal("LOTE_PROXIMO");
        int ordFechaVencimiento = reader.GetOrdinal("FECHA_VENCIMIENTO");
        int ordPrincipioActivo = reader.GetOrdinal("PRINCIPIO_ACTIVO");
        int ordEstadoSku = reader.GetOrdinal("ESTADO_SKU");
        int ordObservacion = reader.GetOrdinal("OBSERVACION");
        int ordCajonM = reader.GetOrdinal("CAJON_M");
        int ordEspecificacion = reader.GetOrdinal("ESPECIFICACION");
        int ordProtocolos = reader.GetOrdinal("PROTOCOLOS");
        int ordRegistroSanitario = reader.GetOrdinal("REGISTRO_SANITARIO");
        int ordLaboratorio = reader.GetOrdinal("LABORATORIO");
        int ordCodebars = reader.GetOrdinal("CODEBARS");
        int ordIgvAfect = reader.GetOrdinal("IGV_AFECT");
        int ordUomentry = reader.GetOrdinal("UOMENTRY");
        int ordPriceBef = reader.GetOrdinal("PRICE_BEF");

        return new ArticuloBusquedaSapDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            DESCRIPCION = reader.IsDBNull(ordDescripcion) ? null : reader.GetString(ordDescripcion),
            UMD = reader.IsDBNull(ordUmd) ? null : reader.GetString(ordUmd),
            PRECIO = reader.IsDBNull(ordPrecio) ? 0 : Convert.ToDecimal(reader[ordPrecio]),
            PRECIO_CAJA = reader.IsDBNull(ordPrecioCaja) ? 0 : Convert.ToDecimal(reader[ordPrecioCaja]),
            STOCK = reader.IsDBNull(ordStock) ? 0 : Convert.ToDecimal(reader[ordStock]),
            STOCK_CAJAS = reader.IsDBNull(ordStockCajas) ? 0 : Convert.ToDecimal(reader[ordStockCajas]),
            GESTIONA_LOTE = reader.IsDBNull(ordGestionaLote) ? null : reader.GetString(ordGestionaLote),
            LOTE_PROXIMO = reader.IsDBNull(ordLoteProximo) ? null : reader.GetString(ordLoteProximo),
            FECHA_VENCIMIENTO = reader.IsDBNull(ordFechaVencimiento) ? null : reader.GetValue(ordFechaVencimiento).ToString(),
            PRINCIPIO_ACTIVO = reader.IsDBNull(ordPrincipioActivo) ? null : reader.GetString(ordPrincipioActivo),
            ESTADO_SKU = reader.IsDBNull(ordEstadoSku) ? null : reader.GetString(ordEstadoSku),
            OBSERVACION = reader.IsDBNull(ordObservacion) ? null : reader.GetString(ordObservacion),
            CAJON_M = reader.IsDBNull(ordCajonM) ? null : reader.GetString(ordCajonM),
            ESPECIFICACION = reader.IsDBNull(ordEspecificacion) ? null : reader.GetString(ordEspecificacion),
            PROTOCOLOS = reader.IsDBNull(ordProtocolos) ? null : reader.GetString(ordProtocolos),
            REGISTRO_SANITARIO = reader.IsDBNull(ordRegistroSanitario) ? null : reader.GetString(ordRegistroSanitario),
            LABORATORIO = reader.IsDBNull(ordLaboratorio) ? null : reader.GetString(ordLaboratorio),
            CODEBARS = reader.IsDBNull(ordCodebars) ? null : reader.GetString(ordCodebars),
            IGV_AFECT = reader.IsDBNull(ordIgvAfect) ? null : reader.GetString(ordIgvAfect),
            UOMENTRY = reader.IsDBNull(ordUomentry) ? 0 : Convert.ToInt32(reader[ordUomentry]),
            PRICE_BEF = reader.IsDBNull(ordPriceBef) ? 0 : Convert.ToDecimal(reader[ordPriceBef])
        };
    }
    public async Task<List<UnidadMedidaArticuloSapDTO>> BuscarUmdArticulo(string codigoArticulo)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_UMD_ARTICULO", 1);

        return await _hana.QueryListAsync(
            commandText,
            MapUnidadMedidaArticuloSap,
            CommandType.Text,
            _hana.CreateParameter("p1", codigoArticulo.Trim().ToUpperInvariant())
        );
    }
    private static UnidadMedidaArticuloSapDTO MapUnidadMedidaArticuloSap(DbDataReader reader)
    {
        int ordCodigoUmd = reader.GetOrdinal("CODIGO_UMD");
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordFactor = reader.GetOrdinal("FACTOR");
        int ordEsPredeterminada = reader.GetOrdinal("ES_PREDETERMINADA");

        return new UnidadMedidaArticuloSapDTO
        {
            CODIGO_UMD = reader.IsDBNull(ordCodigoUmd) ? 0 : reader.GetInt32(ordCodigoUmd),
            CODIGO = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            NOMBRE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            FACTOR = reader.IsDBNull(ordFactor) ? 0 : reader.GetDecimal(ordFactor),
            ES_PREDETERMINADA = reader.IsDBNull(ordEsPredeterminada) ? null : reader.GetString(ordEsPredeterminada)
        };
    }

    public async Task<List<PromoArticuloSapDTO>> BuscarPromoArticulo(string codigoArticulo, string codigoCliente, int codigoListaPrecio, int codigoUmd)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_GET_PROMO", 4);

        return await _hana.QueryListAsync(
            commandText,
            MapPromoArticuloSap,
            CommandType.Text,
            _hana.CreateParameter("p1", codigoArticulo.Trim().ToUpperInvariant()),
            _hana.CreateParameter("p2", codigoCliente),
            _hana.CreateParameter("p3", codigoListaPrecio),
            _hana.CreateParameter("p4", codigoUmd)
        );
    }
    private static PromoArticuloSapDTO MapPromoArticuloSap(DbDataReader reader)
    {
        int ordCodigoArticulo = reader.GetOrdinal("CODIGO_ARTICULO");
        int ordPrecio = reader.GetOrdinal("PRECIO");
        int ordCantidad = reader.GetOrdinal("CANTIDAD");
        int ordDescuento = reader.GetOrdinal("DESCUENTO");
        int ordPromoType = reader.GetOrdinal("PROMO_TYPE");
        int ordWmsGif = reader.GetOrdinal("WMS_GIF");
        int ordUmd = reader.GetOrdinal("UMD");
        int ordCodigoListaPrecio = reader.GetOrdinal("CODIGO_LISTA_PRECIO");

        return new PromoArticuloSapDTO
        {
            CODIGO_ARTICULO = reader.IsDBNull(ordCodigoArticulo) ? null : reader.GetString(ordCodigoArticulo),
            PRECIO = reader.IsDBNull(ordPrecio) ? 0 : reader.GetDecimal(ordPrecio),
            CANTIDAD = reader.IsDBNull(ordCantidad) ? 0 : reader.GetDecimal(ordCantidad),
            DESCUENTO = reader.IsDBNull(ordDescuento) ? 0 : reader.GetDecimal(ordDescuento),
            PROMO_TYPE = reader.IsDBNull(ordPromoType) ? null : reader.GetString(ordPromoType),
            WMS_GIF = reader.IsDBNull(ordWmsGif) ? null : reader.GetString(ordWmsGif),
            UMD = reader.IsDBNull(ordUmd) ? null : reader.GetString(ordUmd),
            CODIGO_LISTA_PRECIO = reader.IsDBNull(ordCodigoListaPrecio) ? null : reader.GetString(ordCodigoListaPrecio)
        };
    }

    public async Task<List<ArticuloPrecioDTO>> ObtenerPrecios(List<string> codigosArticulos, int codigoListaPrecio, string codigoAlmacen, string codigoCliente)
    {
        if (codigosArticulos is null || codigosArticulos.Count == 0)
            return new List<ArticuloPrecioDTO>();

        var codigosUnicos = codigosArticulos
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (codigosUnicos.Count == 0)
            return new List<ArticuloPrecioDTO>();

        var chunks = codigosUnicos.Chunk(500).ToList();
        var tasks = chunks.Select(async chunk =>
        {
            var codigosConcatenados = string.Join(",", chunk);
            var commandText = _hana.BuildProcedureCall("CBF_SP_PV_CALCULAR_PRECIOS_BATCH", 4);

            return await _hana.QueryListAsync(
                commandText,
                MapArticuloPrecio,
                CommandType.Text,
                _hana.CreateParameter("p1", codigosConcatenados),
                _hana.CreateParameter("p2", codigoListaPrecio),
                _hana.CreateParameter("p3", codigoAlmacen),
                _hana.CreateParameter("p4", codigoCliente)
            );
        });

        var results = await Task.WhenAll(tasks);
        var listaCompleta = new List<ArticuloPrecioDTO>();
        foreach (var r in results)
        {
            if (r != null) listaCompleta.AddRange(r);
        }
        return listaCompleta;
    }
    private static ArticuloPrecioDTO MapArticuloPrecio(DbDataReader reader)
    {
        int ordCodigoArticulo = reader.GetOrdinal("CODIGO_ARTICULO");
        int ordPrecio = reader.GetOrdinal("PRECIO");
        int ordPromoType = reader.GetOrdinal("PROMO_TYPE");
        int ordPrecioPromo = reader.GetOrdinal("PRECIO_PROMO");
        int ordCantidadPromo = reader.GetOrdinal("CANTIDAD_PROMO");
        int ordDescuentoPromo = reader.GetOrdinal("DESCUENTO_PROMO");
        int ordUmdPromo = reader.GetOrdinal("UMD_PROMO");

        return new ArticuloPrecioDTO
        {
            CODIGO_ARTICULO = reader.IsDBNull(ordCodigoArticulo) ? null : reader.GetString(ordCodigoArticulo),
            PRECIO = reader.IsDBNull(ordPrecio) ? 0 : reader.GetDecimal(ordPrecio),
            PROMO_TYPE = reader.IsDBNull(ordPromoType) ? null : reader.GetString(ordPromoType),
            PRECIO_PROMO = reader.IsDBNull(ordPrecioPromo) ? null : reader.GetDecimal(ordPrecioPromo),
            CANTIDAD_PROMO = reader.IsDBNull(ordCantidadPromo) ? null : reader.GetDecimal(ordCantidadPromo),
            DESCUENTO_PROMO = reader.IsDBNull(ordDescuentoPromo) ? null : reader.GetDecimal(ordDescuentoPromo),
            UMD_PROMO = reader.IsDBNull(ordUmdPromo) ? null : reader.GetString(ordUmdPromo)
        };
    }

    public async Task<List<LoteDisponibleSapDTO>> BuscarLotesArticulo(string codigoArticulo, string codigoAlmacen)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_LOTES_ARTICULO", 2);

        return await _hana.QueryListAsync(
            commandText,
            MapLoteDisponibleSap,
            CommandType.Text,
            _hana.CreateParameter("p1", codigoArticulo.Trim().ToUpperInvariant()),
            _hana.CreateParameter("p2", codigoAlmacen)
        );
    }

    public async Task<Dictionary<string, List<LoteDisponibleSapDTO>>> BuscarLotesArticulosBatch(List<string> codigosArticulos, string codigoAlmacen)
    {
        var resultado = new Dictionary<string, List<LoteDisponibleSapDTO>>(StringComparer.OrdinalIgnoreCase);
        if (codigosArticulos is null || codigosArticulos.Count == 0)
            return resultado;

        var codigosUnicos = codigosArticulos
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (codigosUnicos.Count == 0)
            return resultado;

        foreach (var cod in codigosUnicos)
        {
            resultado[cod] = new List<LoteDisponibleSapDTO>();
        }

        var chunks = codigosUnicos.Chunk(500).ToList();
        var tasksLotes = chunks.Select(async chunk =>
        {
            var codigosConcatenados = string.Join(",", chunk);
            var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_LOTES_BATCH", 2);

            return await _hana.QueryListAsync(
                commandText,
                reader =>
                {
                    int ordItemCode = reader.GetOrdinal("ITEMCODE");
                    int ordSysnumber = reader.GetOrdinal("SYSNUMBER");
                    int ordDistnumber = reader.GetOrdinal("DISTNUMBER");
                    int ordQuantity = reader.GetOrdinal("QUANTITY");
                    int ordFechaVencimiento = reader.GetOrdinal("FECHA_VENCIMIENTO");
                    int ordUbicacion = reader.GetOrdinal("UBICACION");
                    int ordFechaIngreso = reader.GetOrdinal("FECHA_INGRESO");
                    int ordProtocolo = reader.GetOrdinal("PROTOCOLO");

                    var itemCode = reader.IsDBNull(ordItemCode) ? "" : reader.GetString(ordItemCode).Trim().ToUpperInvariant();
                    var lote = new LoteDisponibleSapDTO
                    {
                        SYSNUMBER = reader.IsDBNull(ordSysnumber) ? null : reader.GetString(ordSysnumber),
                        DISTNUMBER = reader.IsDBNull(ordDistnumber) ? null : reader.GetString(ordDistnumber),
                        QUANTITY = reader.IsDBNull(ordQuantity) ? 0 : reader.GetDecimal(ordQuantity),
                        FECHA_VENCIMIENTO = reader.IsDBNull(ordFechaVencimiento) ? null : reader.GetValue(ordFechaVencimiento).ToString(),
                        UBICACION = reader.IsDBNull(ordUbicacion) ? null : reader.GetString(ordUbicacion),
                        FECHA_INGRESO = reader.IsDBNull(ordFechaIngreso) ? null : reader.GetValue(ordFechaIngreso).ToString(),
                        PROTOCOLO = reader.IsDBNull(ordProtocolo) ? null : reader.GetString(ordProtocolo)
                    };

                    return (ItemCode: itemCode, Lote: lote);
                },
                CommandType.Text,
                _hana.CreateParameter("p1", codigosConcatenados),
                _hana.CreateParameter("p2", codigoAlmacen)
            );
        });

        var chunkResults = await Task.WhenAll(tasksLotes);
        foreach (var lotes in chunkResults)
        {
            if (lotes == null) continue;
            foreach (var (itemCode, lote) in lotes)
            {
                if (!string.IsNullOrEmpty(itemCode))
                {
                    if (!resultado.TryGetValue(itemCode, out var lista))
                    {
                        lista = new List<LoteDisponibleSapDTO>();
                        resultado[itemCode] = lista;
                    }
                    lista.Add(lote);
                }
            }
        }

        return resultado;
    }

    private static LoteDisponibleSapDTO MapLoteDisponibleSap(DbDataReader reader)
    {
        int ordSysnumber = reader.GetOrdinal("SYSNUMBER");
        int ordDistnumber = reader.GetOrdinal("DISTNUMBER");
        int ordQuantity = reader.GetOrdinal("QUANTITY");
        int ordFechaVencimiento = reader.GetOrdinal("FECHA_VENCIMIENTO");
        int ordUbicacion = reader.GetOrdinal("UBICACION");
        int ordFechaIngreso = reader.GetOrdinal("FECHA_INGRESO");
        int ordProtocolo = reader.GetOrdinal("PROTOCOLO");

        return new LoteDisponibleSapDTO
        {
            SYSNUMBER = reader.IsDBNull(ordSysnumber) ? null : reader.GetString(ordSysnumber),
            DISTNUMBER = reader.IsDBNull(ordDistnumber) ? null : reader.GetString(ordDistnumber),
            QUANTITY = reader.IsDBNull(ordQuantity) ? 0 : reader.GetDecimal(ordQuantity),
            FECHA_VENCIMIENTO = reader.IsDBNull(ordFechaVencimiento) ? null : reader.GetValue(ordFechaVencimiento).ToString(),
            UBICACION = reader.IsDBNull(ordUbicacion) ? null : reader.GetString(ordUbicacion),
            FECHA_INGRESO = reader.IsDBNull(ordFechaIngreso) ? null : reader.GetValue(ordFechaIngreso).ToString(),
            PROTOCOLO = reader.IsDBNull(ordProtocolo) ? null : reader.GetString(ordProtocolo)
        };
    }

    private async Task EsperarAnulacionEnSap(SqlConnection con, int docEntry, int docEntrySap)
    {
        // Espera activa asíncrona de hasta 6 segundos (15 intentos x 400ms)
        for (int intento = 0; intento < 15; intento++)
        {
            await Task.Delay(400);

            const string sql = @"
                SELECT DOCSTATUS, SENDSTATUS, ISNULL(DOCENTRY_SAP, 0) AS DOCENTRY_SAP
                FROM T_SK_ODOCS WITH (NOLOCK)
                WHERE DOCENTRY = @DOCENTRY";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@DOCENTRY", docEntry);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                string docStatus = (reader.IsDBNull(0) ? "" : reader.GetString(0)).Trim();
                string sendStatus = (reader.IsDBNull(1) ? "" : reader.GetString(1)).Trim();

                // Si el importador ya procesó y confirmó la anulación en SAP (SENDSTATUS == 'S' y DOCSTATUS == 'C'):
                if (docStatus == "C" && sendStatus == "S")
                {
                    return; // Confirmada anulación en SAP
                }
            }
        }

        throw new InvalidOperationException(
            $"El sistema está terminando de anular la orden previa en SAP (DocEntry SAP: {docEntrySap}). Por favor presione Guardar nuevamente en unos segundos para asegurar que no quede stock comprometido.");
    }

    public async Task<VentaGuardarResponseDTO> GuardarVentaCompleta(VentaGuardarRequestDTO request)
    {
        var response = new VentaGuardarResponseDTO();
        var swTotal = Stopwatch.StartNew();
        var swStep = new Stopwatch();

        Console.WriteLine($"[TELEMETRIA-PV] =============================================================");
        Console.WriteLine($"[TELEMETRIA-PV] INICIO GUARDAR ORDEN (DocEntry: {request.DOCENTRY}, Líneas: {request.DETALLE?.Count ?? 0}, DocStatus: {request.DOCSTATUS})");

        swStep.Restart();
        using var con = new SqlConnection(_cadenaSQLPOS);
        await con.OpenAsync();

        // 1. Verificación previa y aseguramiento de anulación en SAP antes de iniciar la transacción de guardado
        if (request.DOCENTRY > 0)
        {
            var estadoInicial = await ObtenerEstadoDocumentoAtómico(con, null, request.DOCENTRY);
            if (!estadoInicial.Existe)
            {
                throw new InvalidOperationException(
                    $"La orden N° {request.DOCENTRY} ya no existe en la base de datos.");
            }

            if (estadoInicial.DocStatus is "W")
            {
                throw new InvalidOperationException(
                    $"No se pueden guardar los cambios en la orden N° {request.DOCENTRY} porque ya fue enviada a WMS en segundo plano. Los cambios NO han sido guardados. Por favor refresque la pestaña Búsqueda.");
            }

            if (estadoInicial.DocStatus is "U")
            {
                throw new InvalidOperationException(
                    $"No se pueden guardar los cambios en la orden N° {request.DOCENTRY} porque ya fue procesada en SAP en segundo plano. Los cambios NO han sido guardados. Por favor refresque la pestaña Búsqueda.");
            }

            // Validación de concurrencia para órdenes reabiertas y borradores de BD:
            if (!string.IsNullOrWhiteSpace(request.DOCSTATUS_ORIGINAL))
            {
                var docStatusOrig = request.DOCSTATUS_ORIGINAL.Trim().ToUpper();
                if (docStatusOrig is "C" or "CANCELADO" or "CANCELADO SAP")
                {
                    if (estadoInicial.DocStatus is not ("C" or "Z"))
                    {
                        string descActual = ObtenerDescripcionEstado(estadoInicial);
                        throw new InvalidOperationException(
                            $"La orden N° {request.DOCENTRY} ya fue modificada o procesada en otra instancia (Estado actual en base de datos: '{descActual}'). Los datos NO se pueden sobreescribir sobre esta orden.");
                    }
                }
                else if (docStatusOrig is "E" or "BORRADOR")
                {
                    if (estadoInicial.DocStatus != "E" && estadoInicial.DocStatus != "C")
                    {
                        string descActual = ObtenerDescripcionEstado(estadoInicial);
                        throw new InvalidOperationException(
                            $"El borrador N° {request.DOCENTRY} ya fue procesado o modificado en otra instancia (Estado actual en base de datos: '{descActual}'). Los datos NO se pueden sobreescribir.");
                    }
                }
            }

            // Si la orden tiene (o adquirió por carrera durante la edición) un número SAP previo:
            if (estadoInicial.DocEntrySap > 0)
            {
                // Si aún no estaba en cola de anulación, la ponemos en cola con SOPH_SP_CAN_SKODOCS
                if (estadoInicial.DocStatus != "C" || estadoInicial.SendStatus != "N")
                {
                    using var cmdCan = new SqlCommand("SOPH_SP_CAN_SKODOCS", con);
                    cmdCan.CommandType = CommandType.StoredProcedure;
                    var paramCan = new SqlParameter("@DOCENTRY", SqlDbType.Int) { Direction = ParameterDirection.InputOutput, Value = request.DOCENTRY };
                    cmdCan.Parameters.Add(paramCan);
                    await cmdCan.ExecuteNonQueryAsync();
                }

                // Esperar a que el Importador complete la anulación en SAP antes de sobreescribir la fila
                await EsperarAnulacionEnSap(con, request.DOCENTRY, estadoInicial.DocEntrySap);
            }
        }

        using var tran = con.BeginTransaction();
        Console.WriteLine($"[TELEMETRIA-PV] 1. Abrir Transacción SQL: {swStep.ElapsedMilliseconds} ms");

        try
        {
            if (request.DOCENTRY > 0)
            {
                swStep.Restart();
                var estadoActual = await ObtenerEstadoDocumentoAtómico(con, tran, request.DOCENTRY);
                if (!estadoActual.Existe)
                {
                    throw new InvalidOperationException(
                        $"La orden N° {request.DOCENTRY} ya no existe en la base de datos.");
                }

                if (estadoActual.DocStatus is "W")
                {
                    throw new InvalidOperationException(
                        $"No se pueden guardar los cambios en la orden N° {request.DOCENTRY} porque ya fue enviada a WMS en segundo plano.");
                }

                if (estadoActual.DocStatus is "U")
                {
                    throw new InvalidOperationException(
                        $"No se pueden guardar los cambios en la orden N° {request.DOCENTRY} porque ya fue procesada en SAP en segundo plano.");
                }

                using var cmdDel2 = new SqlCommand("DELETE FROM T_SK_DOCS2 WHERE DOCENTRY = @DOCENTRY", con, tran);
                cmdDel2.Parameters.AddWithValue("@DOCENTRY", request.DOCENTRY);
                await cmdDel2.ExecuteNonQueryAsync();

                using var cmdDel1 = new SqlCommand("DELETE FROM T_SK_DOCS1 WHERE DOCENTRY = @DOCENTRY", con, tran);
                cmdDel1.Parameters.AddWithValue("@DOCENTRY", request.DOCENTRY);
                await cmdDel1.ExecuteNonQueryAsync();
                Console.WriteLine($"[TELEMETRIA-PV] 2. Validación y Limpieza Previa: {swStep.ElapsedMilliseconds} ms");
            }

            swStep.Restart();
            int docEntry = await InsertarCabecera(con, tran, request);
            Console.WriteLine($"[TELEMETRIA-PV] 3. Insertar Cabecera (SOPH_SP_INS_UPD_SKODOCS): {swStep.ElapsedMilliseconds} ms -> DocEntry: {docEntry}");

            int lineId = 1;
            foreach (var detalle in request.DETALLE)
            {
                detalle.LINE_ID = lineId++;
            }

            swStep.Restart();
            await InsertarDetallesYLotesBatch(con, tran, docEntry, request.DETALLE);
            Console.WriteLine($"[TELEMETRIA-PV] 4. Inserción de Detalles y Lotes (Batch): {swStep.ElapsedMilliseconds} ms");

            if (request.DOCSTATUS != "E")
            {
                if (request.DOCENTRY > 0)
                {
                    using var cmdClearSap = new SqlCommand("UPDATE T_SK_ODOCS SET DOCENTRY_SAP = NULL WHERE DOCENTRY = @DOCENTRY", con, tran);
                    cmdClearSap.Parameters.AddWithValue("@DOCENTRY", request.DOCENTRY);
                    await cmdClearSap.ExecuteNonQueryAsync();
                }

                swStep.Restart();
                await FinalizarDocumento(con, tran, docEntry);
                Console.WriteLine($"[TELEMETRIA-PV] 5. Finalizar/Cálculo Totales (SOPH_SP_DC_SKODOCS): {swStep.ElapsedMilliseconds} ms");
            }

            swStep.Restart();
            tran.Commit();
            Console.WriteLine($"[TELEMETRIA-PV] 6. Commit de Transacción SQL: {swStep.ElapsedMilliseconds} ms");

            swTotal.Stop();
            Console.WriteLine($"[TELEMETRIA-PV] === TIEMPO TOTAL EN REPOSITORIO SQL: {swTotal.ElapsedMilliseconds} ms ===");
            Console.WriteLine($"[TELEMETRIA-PV] =============================================================");

            response.EXITO = true;
            response.DOCENTRY = docEntry;
            response.MENSAJE = request.DOCSTATUS == "E"
                ? "Borrador guardado correctamente."
                : "Venta registrada correctamente.";
        }
        catch (Exception ex)
        {
            tran.Rollback();
            response.EXITO = false;
            response.MENSAJE = $"Error al guardar la venta: {ex.Message}";
            Console.WriteLine($"[TELEMETRIA-PV] ERROR: {ex.Message} (Tiempo hasta fallo: {swTotal.ElapsedMilliseconds} ms)");
        }

        return response;
    }

    private static string? ConvertirFecha(string? fecha)
    {
        if (string.IsNullOrWhiteSpace(fecha)) return null;
        if (DateTime.TryParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.ToString("yyyy-MM-dd");
        return fecha;
    }

    private static string LimpiarSoloNumeros(string? valor, int maxLength)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;
        var soloNumeros = System.Text.RegularExpressions.Regex.Replace(valor, @"\D", "");
        return soloNumeros.Length <= maxLength ? soloNumeros : soloNumeros.Substring(0, maxLength);
    }

    private async Task<int> InsertarCabecera(SqlConnection con, SqlTransaction tran, VentaGuardarRequestDTO request)
    {
        using var cmd = new SqlCommand("SOPH_SP_INS_UPD_SKODOCS", con, tran);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@OBJTYPE", request.OBJTYPE ?? "17");
        cmd.Parameters.AddWithValue("@WHSCODE", (object?)request.WHSCODE ?? string.Empty);
        cmd.Parameters.AddWithValue("@BINCODE", request.BINCODE);
        cmd.Parameters.AddWithValue("@DOCDATE", (object?)(request.DOCDATE) ?? string.Empty);
        cmd.Parameters.AddWithValue("@CARDCODE", (object?)request.CARDCODE ?? string.Empty);
        cmd.Parameters.AddWithValue("@CARDNAME", (object?)request.CARDNAME ?? string.Empty);
        cmd.Parameters.AddWithValue("@SLPCODE", request.SLPCODE);
        cmd.Parameters.AddWithValue("@SLPNAME", (object?)request.SLPNAME ?? string.Empty);
        cmd.Parameters.AddWithValue("@LICTRADNUM", (object?)request.LICTRADNUM ?? string.Empty);
        cmd.Parameters.AddWithValue("@COMMENTS", TruncarTexto(request.COMMENTS, 254));
        cmd.Parameters.AddWithValue("@ORIN_RECORD", (object?)request.ORIN_RECORD ?? string.Empty);
        cmd.Parameters.AddWithValue("@ORIN_TOTAL", request.ORIN_TOTAL);
        cmd.Parameters.AddWithValue("@IMP_DELIVERY", request.IMP_DELIVERY);
        cmd.Parameters.AddWithValue("@IMP_DSCTO", request.IMP_DSCTO);
        cmd.Parameters.AddWithValue("@IMP_NET", request.IMP_NET);
        cmd.Parameters.AddWithValue("@DOCSTATUS", (object?)request.DOCSTATUS ?? "Z");
        cmd.Parameters.AddWithValue("@SENDSTATUS", "N");
        cmd.Parameters.AddWithValue("@DELIVERY_DATE", (object?)(request.DELIVERY_DATE) ?? string.Empty);
        cmd.Parameters.AddWithValue("@DELIVERY_TIME", (object?)request.DELIVERY_TIME ?? string.Empty);
        cmd.Parameters.AddWithValue("@DELIVERY_PLACE", (object?)request.DELIVERY_PLACE ?? string.Empty);
        cmd.Parameters.AddWithValue("@DELIVERY_ADDRESS", (object?)request.DELIVERY_ADDRESS ?? string.Empty);
        cmd.Parameters.AddWithValue("@DELIVERY_ADDRESS2", (object?)request.DELIVERY_ADDRESS2 ?? string.Empty);
        cmd.Parameters.AddWithValue("@DELIVERY_POINT", (object?)request.DELIVERY_POINT ?? string.Empty);
        cmd.Parameters.AddWithValue("@INVOICE_PLACE", (object?)request.INVOICE_PLACE ?? string.Empty);
        cmd.Parameters.AddWithValue("@INVOICE_ADDRESS", (object?)request.INVOICE_ADDRESS ?? string.Empty);
        cmd.Parameters.AddWithValue("@PRICE_LIST", (object?)request.PRICE_LIST?.Trim() ?? string.Empty);
        cmd.Parameters.AddWithValue("@DOCTYPE", (object?)request.DOCTYPE ?? string.Empty);
        cmd.Parameters.AddWithValue("@PAYFORM", (object?)request.PAYFORM ?? string.Empty);
        cmd.Parameters.AddWithValue("@AGENCY_DATA", TruncarTexto(request.AGENCY_DATA, 150));
        cmd.Parameters.AddWithValue("@PACKING_TYPE", (object?)request.PACKING_TYPE ?? string.Empty);
        cmd.Parameters.AddWithValue("@SEND_MODE", (object?)request.SEND_MODE ?? string.Empty);
        cmd.Parameters.AddWithValue("@SEND_NDOC", LimpiarSoloNumeros(request.SEND_NDOC, 25));
        cmd.Parameters.AddWithValue("@SEND_NAME", TruncarTexto(request.SEND_NAME, 50));
        cmd.Parameters.AddWithValue("@SEND_PHONE", LimpiarSoloNumeros(request.SEND_PHONE, 20));
        cmd.Parameters.AddWithValue("@SEND_PLACE", TruncarTexto(request.SEND_PLACE, 250));
        bool esActualizacion = request.DOCENTRY > 0;
        cmd.Parameters.AddWithValue("@PROCEED", esActualizacion ? "U" : "I");

        if (esActualizacion)
        {
            cmd.Parameters.AddWithValue("@DOCENTRY", request.DOCENTRY);
            await cmd.ExecuteNonQueryAsync();
            return request.DOCENTRY;
        }
        else
        {
            var docEntryParam = new SqlParameter("@DOCENTRY", SqlDbType.Int);
            docEntryParam.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(docEntryParam);

            await cmd.ExecuteNonQueryAsync();

            return Convert.ToInt32(docEntryParam.Value);
        }
    }

    private async Task InsertarDetallesYLotesBatch(SqlConnection con, SqlTransaction tran, int docEntry, List<VentaDetalleDTO> detalles)
    {
        if (detalles == null || detalles.Count == 0) return;

        // Si la orden tuviera una cantidad muy alta de ítems (> 40), procesamos en bloques
        // para no exceder el límite seguro de parámetros de SQL Server (2,100 parámetros por lote).
        const int batchSize = 40;
        for (int b = 0; b < detalles.Count; b += batchSize)
        {
            var chunk = detalles.Skip(b).Take(batchSize).ToList();
            using var cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.Transaction = tran;
            cmd.CommandType = CommandType.Text;

            var sbSql = new StringBuilder();
            int pIndex = 0;

            foreach (var detalle in chunk)
            {
                int lineId = detalle.LINE_ID;
                string pDoc = $"@d_doc_{pIndex}";
                string pLine = $"@d_line_{pIndex}";
                string pItem = $"@d_item_{pIndex}";
                string pName = $"@d_name_{pIndex}";
                string pCodeBars = $"@d_cb_{pIndex}";
                string pQty = $"@d_qty_{pIndex}";
                string pPrice = $"@d_price_{pIndex}";
                string pDsct = $"@d_dsct_{pIndex}";
                string pIgv = $"@d_igv_{pIndex}";
                string pTotal = $"@d_tot_{pIndex}";
                string pForSale = $"@d_sale_{pIndex}";
                string pUom = $"@d_uom_{pIndex}";
                string pQtyUom = $"@d_qtyu_{pIndex}";
                string pTotUom = $"@d_totu_{pIndex}";
                string pPrcUom = $"@d_prcu_{pIndex}";
                string pUgp = $"@d_ugp_{pIndex}";
                string pPrcBef = $"@d_prcb_{pIndex}";
                string pList = $"@d_list_{pIndex}";
                string pGif = $"@d_gif_{pIndex}";
                string pDsc = $"@d_dsc_{pIndex}";

                sbSql.AppendLine($"EXEC SOPH_SP_INS_SKDOCS1 @DOCENTRY={pDoc}, @LINE_ID={pLine}, @ITEMCODE={pItem}, @ITEMNAME={pName}, @CODEBARS={pCodeBars}, @QUANTITY={pQty}, @PRICE={pPrice}, @DSCT_PERCENT={pDsct}, @IGV_AFECT={pIgv}, @LINE_TOTAL={pTotal}, @FORSALE={pForSale}, @UOMENTRY={pUom}, @QUANTITY_UOM={pQtyUom}, @TOTAL_UOM={pTotUom}, @PRICE_UOM={pPrcUom}, @UGPENTRY={pUgp}, @PRICE_BEF={pPrcBef}, @PRICE_LIST={pList}, @WMS_GIF={pGif}, @WMS_DSC={pDsc};");

                var esAfecto = detalle.IGV_AFECT?.Trim().ToUpperInvariant() switch
                {
                    "S" or "Y" or "IGV" => true,
                    "N" or "IGV_EX" or "EX" or "INA" or "NO" => false,
                    _ => true
                };
                var precioSinIgv = esAfecto
                    ? Math.Round(detalle.PRICE / 1.18m, 10)
                    : detalle.PRICE;

                var igvAfect = detalle.IGV_AFECT?.Trim().ToUpperInvariant() switch
                {
                    "S" or "Y" or "IGV" => "IGV",
                    "N" or "IGV_EX" or "EX" or "INA" or "NO" => "IGV_EX",
                    _ => !string.IsNullOrWhiteSpace(detalle.IGV_AFECT) ? detalle.IGV_AFECT.Trim() : "IGV"
                };

                cmd.Parameters.AddWithValue(pDoc, docEntry);
                cmd.Parameters.AddWithValue(pLine, lineId);
                cmd.Parameters.AddWithValue(pItem, (object?)detalle.ITEMCODE ?? string.Empty);
                cmd.Parameters.AddWithValue(pName, (object?)detalle.ITEMNAME ?? string.Empty);
                cmd.Parameters.AddWithValue(pCodeBars, (object?)detalle.CODEBARS ?? string.Empty);
                cmd.Parameters.AddWithValue(pQty, detalle.QUANTITY);
                cmd.Parameters.AddWithValue(pPrice, precioSinIgv);
                cmd.Parameters.AddWithValue(pDsct, detalle.DSCT_PERCENT);
                cmd.Parameters.AddWithValue(pIgv, igvAfect);
                cmd.Parameters.AddWithValue(pTotal, detalle.LINE_TOTAL);
                cmd.Parameters.AddWithValue(pForSale, detalle.FORSALE);
                cmd.Parameters.AddWithValue(pUom, detalle.UOMENTRY);
                cmd.Parameters.AddWithValue(pQtyUom, detalle.QUANTITY_UOM > 0 ? detalle.QUANTITY_UOM : detalle.QUANTITY);
                cmd.Parameters.AddWithValue(pTotUom, detalle.TOTAL_UOM > 0 ? detalle.TOTAL_UOM : detalle.QUANTITY);
                cmd.Parameters.AddWithValue(pPrcUom, detalle.PRICE_UOM > 0 ? detalle.PRICE_UOM : precioSinIgv);
                cmd.Parameters.AddWithValue(pUgp, detalle.UGPENTRY > 0 ? detalle.UGPENTRY : detalle.UOMENTRY);
                cmd.Parameters.AddWithValue(pPrcBef, detalle.PRICE_BEF > 0 ? detalle.PRICE_BEF : precioSinIgv);
                cmd.Parameters.AddWithValue(pList, (object?)detalle.PRICE_LIST?.Trim() ?? string.Empty);
                cmd.Parameters.AddWithValue(pGif, (object?)(detalle.WMS_GIF ?? "NO") ?? string.Empty);
                cmd.Parameters.AddWithValue(pDsc, detalle.WMS_DSC);

                int lIndex = 0;
                foreach (var lote in detalle.LOTES)
                {
                    string pLDoc = $"@l_doc_{pIndex}_{lIndex}";
                    string pLLineH = $"@l_lh_{pIndex}_{lIndex}";
                    string pLLineId = $"@l_lid_{pIndex}_{lIndex}";
                    string pLItem = $"@l_item_{pIndex}_{lIndex}";
                    string pLQty = $"@l_qty_{pIndex}_{lIndex}";
                    string pLSys = $"@l_sys_{pIndex}_{lIndex}";
                    string pLDist = $"@l_dist_{pIndex}_{lIndex}";

                    sbSql.AppendLine($"EXEC SOPH_SP_INS_SKDOCS2 @DOCENTRY={pLDoc}, @LINE_HEADER={pLLineH}, @LINE_ID={pLLineId}, @ITEMCODE={pLItem}, @QUANTITY={pLQty}, @SYSNUMBER={pLSys}, @DISTNUMBER={pLDist};");

                    cmd.Parameters.AddWithValue(pLDoc, docEntry);
                    cmd.Parameters.AddWithValue(pLLineH, lineId);
                    cmd.Parameters.AddWithValue(pLLineId, 0);
                    cmd.Parameters.AddWithValue(pLItem, (object?)lote.ITEMCODE ?? string.Empty);
                    cmd.Parameters.AddWithValue(pLQty, lote.QUANTITY);
                    cmd.Parameters.AddWithValue(pLSys, (object?)lote.SYSNUMBER ?? string.Empty);
                    cmd.Parameters.AddWithValue(pLDist, (object?)lote.DISTNUMBER ?? string.Empty);

                    lIndex++;
                }

                pIndex++;
            }

            cmd.CommandText = sbSql.ToString();
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private async Task InsertarDetalle(SqlConnection con, SqlTransaction tran, int docEntry, VentaDetalleDTO detalle)
    {
        using var cmd = new SqlCommand("SOPH_SP_INS_SKDOCS1", con, tran);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@DOCENTRY", docEntry);
        cmd.Parameters.AddWithValue("@LINE_ID", detalle.LINE_ID);
        cmd.Parameters.AddWithValue("@ITEMCODE", (object?)detalle.ITEMCODE ?? string.Empty);
        cmd.Parameters.AddWithValue("@ITEMNAME", (object?)detalle.ITEMNAME ?? string.Empty);
        cmd.Parameters.AddWithValue("@CODEBARS", (object?)detalle.CODEBARS ?? string.Empty);
        cmd.Parameters.AddWithValue("@QUANTITY", detalle.QUANTITY);
        var esAfecto = detalle.IGV_AFECT?.Trim().ToUpperInvariant() switch
        {
            "S" or "Y" or "IGV" => true,
            "N" or "IGV_EX" or "EX" or "INA" or "NO" => false,
            _ => true
        };
        var precioSinIgv = esAfecto
            ? Math.Round(detalle.PRICE / 1.18m, 10)
            : detalle.PRICE;
        cmd.Parameters.AddWithValue("@PRICE", precioSinIgv);
        cmd.Parameters.AddWithValue("@DSCT_PERCENT", detalle.DSCT_PERCENT);
        var igvAfect = detalle.IGV_AFECT?.Trim().ToUpperInvariant() switch
        {
            "S" or "Y" or "IGV" => "IGV",
            "N" or "IGV_EX" or "EX" or "INA" or "NO" => "IGV_EX",
            _ => !string.IsNullOrWhiteSpace(detalle.IGV_AFECT) ? detalle.IGV_AFECT.Trim() : "IGV"
        };
        cmd.Parameters.AddWithValue("@IGV_AFECT", igvAfect);
        cmd.Parameters.AddWithValue("@LINE_TOTAL", detalle.LINE_TOTAL);
        cmd.Parameters.AddWithValue("@FORSALE", detalle.FORSALE);
        cmd.Parameters.AddWithValue("@UOMENTRY", detalle.UOMENTRY);
        cmd.Parameters.AddWithValue("@QUANTITY_UOM", detalle.QUANTITY_UOM > 0 ? detalle.QUANTITY_UOM : detalle.QUANTITY);
        cmd.Parameters.AddWithValue("@TOTAL_UOM", detalle.TOTAL_UOM > 0 ? detalle.TOTAL_UOM : detalle.QUANTITY);
        cmd.Parameters.AddWithValue("@PRICE_UOM",
            detalle.PRICE_UOM > 0 ? detalle.PRICE_UOM : precioSinIgv);
        cmd.Parameters.AddWithValue("@UGPENTRY", detalle.UGPENTRY > 0 ? detalle.UGPENTRY : detalle.UOMENTRY);
        cmd.Parameters.AddWithValue("@PRICE_BEF",
            detalle.PRICE_BEF > 0 ? detalle.PRICE_BEF : precioSinIgv);
        cmd.Parameters.AddWithValue("@PRICE_LIST", (object?)detalle.PRICE_LIST?.Trim() ?? string.Empty);
        cmd.Parameters.AddWithValue("@WMS_GIF", (object?)(detalle.WMS_GIF ?? "NO") ?? string.Empty);
        cmd.Parameters.AddWithValue("@WMS_DSC", detalle.WMS_DSC);

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task InsertarLote(SqlConnection con, SqlTransaction tran, int docEntry, int lineHeader, VentaLoteDTO lote)
    {
        using var cmd = new SqlCommand("SOPH_SP_INS_SKDOCS2", con, tran);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@DOCENTRY", docEntry);
        cmd.Parameters.AddWithValue("@LINE_HEADER", lineHeader);
        cmd.Parameters.AddWithValue("@LINE_ID", 0);
        cmd.Parameters.AddWithValue("@ITEMCODE", (object?)lote.ITEMCODE ?? string.Empty);
        cmd.Parameters.AddWithValue("@QUANTITY", lote.QUANTITY);
        cmd.Parameters.AddWithValue("@SYSNUMBER", (object?)lote.SYSNUMBER ?? string.Empty);
        cmd.Parameters.AddWithValue("@DISTNUMBER", (object?)lote.DISTNUMBER ?? string.Empty);

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<int> FinalizarDocumento(SqlConnection con, SqlTransaction tran, int docEntry)
    {
        using var cmd = new SqlCommand("SOPH_SP_DC_SKODOCS", con, tran);
        cmd.CommandType = CommandType.StoredProcedure;

        var docEntryParam = new SqlParameter("@DOCENTRY", Convert.ToInt32(docEntry));
        docEntryParam.Direction = ParameterDirection.InputOutput;
        cmd.Parameters.Add(docEntryParam);

        await cmd.ExecuteNonQueryAsync();

        return Convert.ToInt32(docEntryParam.Value);
    }

    public async Task<List<VentaListaDTO>> ListarVentas(VentaBusquedaFiltroDTO filtro)
    {
        var condiciones = new List<string>();
        condiciones.Add("T0.OBJTYPE = 17");

        var termCliente = (filtro.CLIENTE ?? "").Replace("'", "''");
        condiciones.Add($"(T0.CARDNAME LIKE '%{termCliente}%' OR T0.LICTRADNUM LIKE '%{termCliente}%')");

        if (!string.IsNullOrWhiteSpace(filtro.VENDEDOR) && !filtro.VENDEDOR.Equals("-1"))
        {
            var term = filtro.VENDEDOR.Replace("'", "''");
            condiciones.Add($"T0.SLPCODE='{term}'");
        }

        return await _sql.QueryListAsync(
            "SOPH_SP_LST_SKDOCS",
            MapVentaLista,
            TipoConexionSql.POS,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@DOCENTRY", ""),
            _sql.CreateParameter("@DOCDATE", (object?)ConvertirFechaADateTime(filtro.FECHA_INICIO) ?? DBNull.Value),
            _sql.CreateParameter("@DOCDATEE", (object?)ConvertirFechaADateTime(filtro.FECHA_FIN) ?? DBNull.Value),
            _sql.CreateParameter("@CONDITION", string.Join(" AND ", condiciones)),
            _sql.CreateParameter("@PROCEED", "B")
        );
    }

    private static VentaListaDTO MapVentaLista(DbDataReader reader)
    {
        int ordCardName = reader.GetOrdinal("CARDNAME");
        int ordLicTradNum = reader.GetOrdinal("LICTRADNUM");
        int ordDocDate = reader.GetOrdinal("DOCDATE");
        int ordSlpName = reader.GetOrdinal("SLPNAME");
        int ordWhsCode = reader.GetOrdinal("WHSCODE");
        int ordComments = reader.GetOrdinal("COMMENTS");
        int ordDeliveryPoint = reader.GetOrdinal("DELIVERY_POINT");
        int ordImpNet = reader.GetOrdinal("IMP_NET");
        int ordDocNumSap = reader.GetOrdinal("DOCNUM_SAP");
        int ordStat = reader.GetOrdinal("STAT");
        int ordDocEntry = reader.GetOrdinal("DOCENTRY");
        int ordDocStatus = reader.GetOrdinal("DOCSTATUS");
        int ordDocEntrySap = reader.GetOrdinal("DOCENTRY_SAP");
        int ordDocEntryOwtr = reader.GetOrdinal("DOCENTRY_OWTR");

        return new VentaListaDTO
        {
            CLIENTE = reader.IsDBNull(ordCardName) ? null : reader.GetString(ordCardName),
            RUC_DNI = reader.IsDBNull(ordLicTradNum) ? null : reader.GetString(ordLicTradNum),
            FECHA = reader.IsDBNull(ordDocDate) ? null : FormatearFechaDocDate(reader.GetValue(ordDocDate)),
            VENDEDOR = reader.IsDBNull(ordSlpName) ? null : reader.GetString(ordSlpName),
            ALMACEN = reader.IsDBNull(ordWhsCode) ? null : reader.GetString(ordWhsCode),
            COMENTARIO = reader.IsDBNull(ordComments) ? null : reader.GetString(ordComments),
            LUGAR_ENTREGA = reader.IsDBNull(ordDeliveryPoint) ? null : reader.GetString(ordDeliveryPoint),
            TOTAL = reader.IsDBNull(ordImpNet) ? 0 : reader.GetDecimal(ordImpNet),
            NROSAP = reader.IsDBNull(ordDocNumSap) ? null : reader.GetString(ordDocNumSap),
            ESTADO_ENVIO = reader.IsDBNull(ordStat) ? null : reader.GetString(ordStat),
            DOCENTRY = reader.IsDBNull(ordDocEntry) ? 0 : reader.GetInt32(ordDocEntry),
            DOCSTATUS = reader.IsDBNull(ordDocStatus) ? null : reader.GetString(ordDocStatus),
            DOCENTRY_SAP = reader.IsDBNull(ordDocEntrySap) ? 0 : reader.GetInt32(ordDocEntrySap),
            DOCENTRY_OWTR = reader.IsDBNull(ordDocEntryOwtr) ? 0 : reader.GetInt32(ordDocEntryOwtr)
        };
    }

    private static string? FormatearFechaDocDate(object? valor)
    {
        if (valor == null || valor == DBNull.Value) return null;
        if (valor is DateTime dt)
            return dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        if (DateTime.TryParse(valor.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtParsed))
            return dtParsed.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        if (DateTime.TryParse(valor.ToString(), out var dtDefault))
            return dtDefault.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        return valor.ToString();
    }

    private static DateTime? ConvertirFechaADateTime(string? fecha)
    {
        if (string.IsNullOrWhiteSpace(fecha)) return null;
        if (DateTime.TryParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        return null;
    }

    public async Task<VentaCargarResponseDTO?> CargarVenta(int docEntry)
    {
        var header = await _sql.QuerySingleOrDefaultAsync(
            "SOPH_SP_LST_SKDOCS",
            MapVentaCargarHeader,
            TipoConexionSql.POS,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@DOCENTRY", docEntry),
            _sql.CreateParameter("@DOCDATE", DBNull.Value),
            _sql.CreateParameter("@DOCDATEE", DBNull.Value),
            _sql.CreateParameter("@CONDITION", ""),
            _sql.CreateParameter("@PROCEED", "F")
        );

        if (header is null) return null;

        var detalle = await _sql.QueryListAsync(
            "SOPH_SP_LST_SKDOCS",
            MapVentaDetalleCargado,
            TipoConexionSql.POS,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@DOCENTRY", docEntry),
            _sql.CreateParameter("@DOCDATE", DBNull.Value),
            _sql.CreateParameter("@DOCDATEE", DBNull.Value),
            _sql.CreateParameter("@CONDITION", ""),
            _sql.CreateParameter("@PROCEED", "D")
        );

        var lotes = await _sql.QueryListAsync(
            "SOPH_SP_LST_SKDOCS",
            MapVentaLoteCargado,
            TipoConexionSql.POS,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@DOCENTRY", docEntry),
            _sql.CreateParameter("@DOCDATE", DBNull.Value),
            _sql.CreateParameter("@DOCDATEE", DBNull.Value),
            _sql.CreateParameter("@CONDITION", ""),
            _sql.CreateParameter("@PROCEED", "L")
        );

        foreach (var d in detalle)
        {
            d.LOTES = lotes
                .Where(l => l.LINE_HEADER == d.LINE_ID && l.ITEMCODE == d.ITEMCODE)
                .ToList();
        }

        header.DETALLE = detalle;
        return header;
    }

    public async Task<VentaCargarResponseDTO?> VerVenta(int docEntry)
    {
        var venta = await CargarVenta(docEntry);
        if (venta == null || venta.DETALLE == null || venta.DETALLE.Count == 0)
            return venta;

        try
        {
            var codigos = venta.DETALLE
                .Where(d => !string.IsNullOrWhiteSpace(d.ITEMCODE))
                .Select(d => d.ITEMCODE!.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            var whsCode = venta.WHSCODE ?? "";

            var taskUmds = BuscarUmdsArticulosBatch(codigos);
            var tasksLotes = codigos.Select(async cod =>
            {
                try
                {
                    var lotes = await BuscarLotesArticulo(cod, whsCode);
                    return (ItemCode: cod, Lotes: lotes ?? new List<LoteDisponibleSapDTO>());
                }
                catch
                {
                    return (ItemCode: cod, Lotes: new List<LoteDisponibleSapDTO>());
                }
            }).ToList();

            await Task.WhenAll(taskUmds, Task.WhenAll(tasksLotes));

            var dictUmds = await taskUmds;
            var chunkLotes = await Task.WhenAll(tasksLotes);
            var dictLotes = chunkLotes.ToDictionary(x => x.ItemCode, x => x.Lotes, StringComparer.OrdinalIgnoreCase);

            foreach (var d in venta.DETALLE)
            {
                var itemCodeUpper = (d.ITEMCODE ?? "").Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(itemCodeUpper) && dictUmds.TryGetValue(itemCodeUpper, out var listUmds))
                {
                    var umd = listUmds.FirstOrDefault(u => u.CODIGO_UMD == d.UOMENTRY) ?? listUmds.FirstOrDefault();
                    if (umd != null)
                    {
                        d.UMD_NOMBRE = umd.NOMBRE;
                        d.UMD_FACTOR = umd.FACTOR;
                    }
                }
                if (string.IsNullOrWhiteSpace(d.UMD_NOMBRE))
                {
                    d.UMD_NOMBRE = "PZA";
                }

                if (dictLotes.TryGetValue(itemCodeUpper, out var listLotes) && listLotes.Count > 0)
                {
                    if (d.LOTES != null && d.LOTES.Count > 0)
                    {
                        var lotesCoincidentes = listLotes
                            .Where(l => d.LOTES.Any(dl =>
                                (!string.IsNullOrWhiteSpace(dl.DISTNUMBER) && string.Equals(dl.DISTNUMBER.Trim(), (l.DISTNUMBER ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrWhiteSpace(dl.SYSNUMBER) && string.Equals(dl.SYSNUMBER.Trim(), (l.SYSNUMBER ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                            ))
                            .Where(l => !string.IsNullOrWhiteSpace(l.FECHA_VENCIMIENTO))
                            .ToList();

                        if (lotesCoincidentes.Count > 0)
                        {
                            d.FECHA_VENCIMIENTO = lotesCoincidentes
                                .Select(l => l.FECHA_VENCIMIENTO)
                                .OrderBy(f => ConvertirFechaADateTime(f) ?? DateTime.MaxValue)
                                .FirstOrDefault();
                        }
                    }

                    if (string.IsNullOrWhiteSpace(d.FECHA_VENCIMIENTO))
                    {
                        d.FECHA_VENCIMIENTO = listLotes
                            .Where(l => !string.IsNullOrWhiteSpace(l.FECHA_VENCIMIENTO))
                            .Select(l => l.FECHA_VENCIMIENTO)
                            .OrderBy(f => ConvertirFechaADateTime(f) ?? DateTime.MaxValue)
                            .FirstOrDefault();
                    }
                }
            }

            var lineasSinFecha = venta.DETALLE
                .Where(d => string.IsNullOrWhiteSpace(d.FECHA_VENCIMIENTO) && !string.IsNullOrWhiteSpace(d.ITEMCODE))
                .ToList();

            if (lineasSinFecha.Count > 0)
            {
                var codsSinFecha = lineasSinFecha
                    .Select(d => d.ITEMCODE!.Trim().ToUpperInvariant())
                    .Distinct()
                    .ToList();

                var dictObtn = await ObtenerFechasLotesObtn(codsSinFecha);
                foreach (var d in lineasSinFecha)
                {
                    var itemCodeUpper = (d.ITEMCODE ?? "").Trim().ToUpperInvariant();
                    if (dictObtn.TryGetValue(itemCodeUpper, out var listObtn) && listObtn.Count > 0)
                    {
                        if (d.LOTES != null && d.LOTES.Count > 0)
                        {
                            var match = listObtn.FirstOrDefault(lo => d.LOTES.Any(dl =>
                                (!string.IsNullOrWhiteSpace(dl.DISTNUMBER) && string.Equals(dl.DISTNUMBER.Trim(), (lo.DISTNUMBER ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrWhiteSpace(dl.SYSNUMBER) && string.Equals(dl.SYSNUMBER.Trim(), (lo.SYSNUMBER ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                            ));

                            if (match != null && !string.IsNullOrWhiteSpace(match.FECHA_VENCIMIENTO))
                            {
                                d.FECHA_VENCIMIENTO = match.FECHA_VENCIMIENTO;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(d.FECHA_VENCIMIENTO))
                        {
                            d.FECHA_VENCIMIENTO = listObtn
                                .Where(lo => !string.IsNullOrWhiteSpace(lo.FECHA_VENCIMIENTO))
                                .Select(lo => lo.FECHA_VENCIMIENTO)
                                .OrderBy(f => ConvertirFechaADateTime(f) ?? DateTime.MaxValue)
                                .FirstOrDefault();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PuntoVenta] Advertencia al enriquecer lotes/UMDs en VerVenta: {ex.Message}");
        }

        return venta;
    }

    private async Task<Dictionary<string, List<LoteDisponibleSapDTO>>> ObtenerFechasLotesObtn(List<string> codigos)
    {
        var resultado = new Dictionary<string, List<LoteDisponibleSapDTO>>(StringComparer.OrdinalIgnoreCase);
        if (codigos == null || codigos.Count == 0) return resultado;

        try
        {
            var chunks = codigos.Chunk(500).ToList();
            foreach (var chunk in chunks)
            {
                var cods = string.Join("','", chunk.Select(c => c.Replace("'", "''")));
                var sql = $@"
                    SELECT 
                        T0.""ItemCode"" AS ""ITEMCODE"",
                        CAST(T0.""AbsEntry"" AS VARCHAR(50)) AS ""SYSNUMBER"",
                        T0.""DistNumber"" AS ""DISTNUMBER"",
                        TO_VARCHAR(T0.""ExpDate"", 'DD/MM/YYYY') AS ""FECHA_VENCIMIENTO""
                    FROM OBTN T0
                    WHERE T0.""ItemCode"" IN ('{cods}')
                      AND T0.""ExpDate"" IS NOT NULL
                    ORDER BY T0.""ExpDate"" ASC";

                var lista = await _hana.QueryListAsync(
                    sql,
                    reader =>
                    {
                        int ordItem = reader.GetOrdinal("ITEMCODE");
                        int ordSys = reader.GetOrdinal("SYSNUMBER");
                        int ordDist = reader.GetOrdinal("DISTNUMBER");
                        int ordExp = reader.GetOrdinal("FECHA_VENCIMIENTO");

                        var itemCode = reader.IsDBNull(ordItem) ? "" : reader.GetString(ordItem).Trim().ToUpperInvariant();
                        var lote = new LoteDisponibleSapDTO
                        {
                            SYSNUMBER = reader.IsDBNull(ordSys) ? null : reader.GetValue(ordSys).ToString(),
                            DISTNUMBER = reader.IsDBNull(ordDist) ? null : reader.GetString(ordDist),
                            FECHA_VENCIMIENTO = reader.IsDBNull(ordExp) ? null : reader.GetValue(ordExp).ToString()
                        };

                        return (ItemCode: itemCode, Lote: lote);
                    },
                    CommandType.Text
                );

                foreach (var (itemCode, lote) in lista)
                {
                    if (!string.IsNullOrEmpty(itemCode))
                    {
                        if (!resultado.TryGetValue(itemCode, out var list))
                        {
                            list = new List<LoteDisponibleSapDTO>();
                            resultado[itemCode] = list;
                        }
                        list.Add(lote);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PuntoVenta] Fallback en ObtenerFechasLotesObtn: {ex.Message}");
        }

        return resultado;
    }

    public async Task<List<LogImportadorDTO>> ListarLogImportador(int docEntry)
    {
        return await _sql.QueryListAsync(
            "SOPH_SP_LST_LOG_IMPORTADOR",
            MapLogImportador,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@DOCENTRY", docEntry)
        );
    }

    private static LogImportadorDTO MapLogImportador(DbDataReader reader)
    {
        int ordId = reader.GetOrdinal("ID");
        int ordOv = reader.GetOrdinal("OV");
        int ordDocEntry = reader.GetOrdinal("DOC_ENTRY");
        int ordFechaHora = reader.GetOrdinal("FECHA_HORA");
        int ordDescripcion = reader.GetOrdinal("DESCRIPCION");
        int ordTipoError = reader.GetOrdinal("TIPO_ERROR");
        int ordRucCliente = reader.GetOrdinal("RUC_CLIENTE");
        int ordNombreCliente = reader.GetOrdinal("NOMBRE_CLIENTE");
        int ordUsuarioCreacion = reader.GetOrdinal("USUARIO_CREACION");
        int ordMontoTotal = reader.GetOrdinal("MONTO_TOTAL");

        return new LogImportadorDTO
        {
            ID = reader.IsDBNull(ordId) ? null : reader.GetInt32(ordId),
            OV = reader.IsDBNull(ordOv) ? null : reader.GetString(ordOv),
            DOC_ENTRY = reader.IsDBNull(ordDocEntry) ? null : reader.GetInt32(ordDocEntry),
            FECHA_HORA = reader.IsDBNull(ordFechaHora) ? null : FormatearFechaHoraDocDate(reader.GetValue(ordFechaHora)),
            DESCRIPCION = reader.IsDBNull(ordDescripcion) ? null : reader.GetString(ordDescripcion),
            TIPO_ERROR = reader.IsDBNull(ordTipoError) ? null : reader.GetString(ordTipoError),
            RUC_CLIENTE = reader.IsDBNull(ordRucCliente) ? null : reader.GetString(ordRucCliente),
            NOMBRE_CLIENTE = reader.IsDBNull(ordNombreCliente) ? null : reader.GetString(ordNombreCliente),
            USUARIO_CREACION = reader.IsDBNull(ordUsuarioCreacion) ? null : reader.GetString(ordUsuarioCreacion),
            MONTO_TOTAL = reader.IsDBNull(ordMontoTotal) ? null : reader.GetDecimal(ordMontoTotal)
        };
    }

    private static string? FormatearFechaHoraDocDate(object? valor)
    {
        if (valor == null || valor == DBNull.Value) return null;
        if (valor is DateTime dt)
            return dt.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        if (DateTime.TryParse(valor.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtParsed))
            return dtParsed.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        if (DateTime.TryParse(valor.ToString(), out var dtDefault))
            return dtDefault.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        return valor.ToString();
    }

    private static VentaCargarResponseDTO MapVentaCargarHeader(DbDataReader reader)
    {
        int ordDocEntry = reader.GetOrdinal("DOCENTRY");
        int ordCardCode = reader.GetOrdinal("CARDCODE");
        int ordCardName = reader.GetOrdinal("CARDNAME");
        int ordLicTradNum = reader.GetOrdinal("LICTRADNUM");
        int ordSlpCode = reader.GetOrdinal("SLPCODE");
        int ordSlpName = reader.GetOrdinal("SLPNAME");
        int ordWhsCode = reader.GetOrdinal("WHSCODE");
        int ordDocDate = reader.GetOrdinal("DOCDATE");
        int ordComments = reader.GetOrdinal("COMMENTS");
        int ordImpDelivery = reader.GetOrdinal("IMP_DELIVERY");
        int ordImpDscto = reader.GetOrdinal("IMP_DSCTO");
        int ordImpNet = reader.GetOrdinal("IMP_NET");
        int ordPriceList = reader.GetOrdinal("PRICE_LIST");
        int ordDocType = reader.GetOrdinal("DOCTYPE");
        int ordPayForm = reader.GetOrdinal("PAYFORM");
        int ordPackingType = reader.GetOrdinal("PACKING_TYPE");
        int ordSendMode = reader.GetOrdinal("SEND_MODE");
        int ordDeliveryDate = reader.GetOrdinal("DELIVERY_DATE");
        int ordDeliveryTime = reader.GetOrdinal("DELIVERY_TIME");
        int ordDeliveryPoint = reader.GetOrdinal("DELIVERY_POINT");
        int ordDeliveryAddress = reader.GetOrdinal("DELIVERY_ADDRESS");
        int ordInvoicePlace = reader.GetOrdinal("INVOICE_PLACE");
        int ordInvoiceAddress = reader.GetOrdinal("INVOICE_ADDRESS");
        int ordDeliveryPlace = reader.GetOrdinal("DELIVERY_PLACE");
        int ordAgencyData = reader.GetOrdinal("AGENCY_DATA");
        int ordSendNdoc = reader.GetOrdinal("SEND_NDOC");
        int ordSendName = reader.GetOrdinal("SEND_NAME");
        int ordSendPhone = reader.GetOrdinal("SEND_PHONE");
        int ordSendPlace = reader.GetOrdinal("SEND_PLACE");
        int ordOrinTotal = reader.GetOrdinal("ORIN_TOTAL");
        int ordOrinRecord = reader.GetOrdinal("ORIN_RECORD");

        return new VentaCargarResponseDTO
        {
            DOCENTRY = reader.IsDBNull(ordDocEntry) ? 0 : reader.GetInt32(ordDocEntry),
            CARDCODE = reader.IsDBNull(ordCardCode) ? null : reader.GetString(ordCardCode),
            CARDNAME = reader.IsDBNull(ordCardName) ? null : reader.GetString(ordCardName),
            LICTRADNUM = reader.IsDBNull(ordLicTradNum) ? null : reader.GetString(ordLicTradNum),
            SLPCODE = reader.IsDBNull(ordSlpCode) ? 0 : reader.GetInt32(ordSlpCode),
            SLPNAME = reader.IsDBNull(ordSlpName) ? null : reader.GetString(ordSlpName),
            WHSCODE = reader.IsDBNull(ordWhsCode) ? null : reader.GetString(ordWhsCode),
            DOCDATE = reader.IsDBNull(ordDocDate) ? null : reader.GetValue(ordDocDate).ToString(),
            COMMENTS = reader.IsDBNull(ordComments) ? null : reader.GetString(ordComments),
            IMP_DELIVERY = reader.IsDBNull(ordImpDelivery) ? 0 : reader.GetDecimal(ordImpDelivery),
            IMP_DSCTO = reader.IsDBNull(ordImpDscto) ? 0 : reader.GetDecimal(ordImpDscto),
            IMP_NET = reader.IsDBNull(ordImpNet) ? 0 : reader.GetDecimal(ordImpNet),
            PRICE_LIST = TryGetString(reader, "PRICE_LIST"),
            DOCTYPE = TryGetString(reader, "DOCTYPE"),
            PAYFORM = TryGetString(reader, "PAYFORM"),
            PACKING_TYPE = TryGetString(reader, "PACKING_TYPE"),
            SEND_MODE = reader.IsDBNull(ordSendMode) ? null : reader.GetString(ordSendMode),
            DELIVERY_DATE = reader.IsDBNull(ordDeliveryDate) ? null : reader.GetValue(ordDeliveryDate).ToString(),
            DELIVERY_TIME = reader.IsDBNull(ordDeliveryTime) ? null : reader.GetString(ordDeliveryTime),
            DELIVERY_PLACE = reader.IsDBNull(ordDeliveryPlace) ? null : reader.GetString(ordDeliveryPlace),
            DELIVERY_ADDRESS = reader.IsDBNull(ordDeliveryAddress) ? null : reader.GetString(ordDeliveryAddress),
            INVOICE_PLACE = reader.IsDBNull(ordInvoicePlace) ? null : reader.GetString(ordInvoicePlace),
            INVOICE_ADDRESS = reader.IsDBNull(ordInvoiceAddress) ? null : reader.GetString(ordInvoiceAddress),
            DELIVERY_POINT = reader.IsDBNull(ordDeliveryPoint) ? null : reader.GetString(ordDeliveryPoint),
            AGENCY_DATA = reader.IsDBNull(ordAgencyData) ? null : reader.GetString(ordAgencyData),
            SEND_NDOC = reader.IsDBNull(ordSendNdoc) ? null : reader.GetString(ordSendNdoc),
            SEND_NAME = reader.IsDBNull(ordSendName) ? null : reader.GetString(ordSendName),
            SEND_PHONE = reader.IsDBNull(ordSendPhone) ? null : reader.GetString(ordSendPhone),
            SEND_PLACE = reader.IsDBNull(ordSendPlace) ? null : reader.GetString(ordSendPlace),
            ORIN_TOTAL = reader.IsDBNull(ordOrinTotal) ? 0 : reader.GetDecimal(ordOrinTotal),
            ORIN_RECORD = reader.IsDBNull(ordOrinRecord) ? null : reader.GetString(ordOrinRecord),
            DOCSTATUS = TryGetString(reader, "DOCSTATUS"),
            SENDSTATUS = TryGetString(reader, "SENDSTATUS"),
            DOCENTRY_SAP = TryGetInt32(reader, "DOCENTRY_SAP")
        };
    }

    private static string? TryGetString(DbDataReader reader, string columnName)
    {
        try
        {
            int ord = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ord) ? null : reader.GetString(ord)?.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static int? TryGetInt32(DbDataReader reader, string columnName)
    {
        try
        {
            int ord = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ord) ? null : reader.GetInt32(ord);
        }
        catch
        {
            return null;
        }
    }

    private static VentaDetalleCargadoDTO MapVentaDetalleCargado(DbDataReader reader)
    {
        int ordLineId = reader.GetOrdinal("LINE_ID");
        int ordItemCode = reader.GetOrdinal("ITEMCODE");
        int ordItemName = reader.GetOrdinal("ITEMNAME");
        int ordCodebars = reader.GetOrdinal("CODEBARS");
        int ordQuantity = reader.GetOrdinal("QUANTITY");
        int ordPrice = reader.GetOrdinal("PRICE");
        int ordDsctPercent = reader.GetOrdinal("DSCT_PERCENT");
        int ordIgvAfect = reader.GetOrdinal("IGV_AFECT");
        int ordLineTotal = reader.GetOrdinal("LINE_TOTAL");
        int ordUomentry = reader.GetOrdinal("UOMENTRY");
        int ordUgpentry = reader.GetOrdinal("UGPENTRY");
        int ordPriceUom = reader.GetOrdinal("PRICE_UOM");
        int ordPriceBef = reader.GetOrdinal("PRICE_BEF");
        int ordPriceList = reader.GetOrdinal("PRICE_LIST");
        int ordWmsGif = reader.GetOrdinal("WMS_GIF");
        int ordWmsDsc = reader.GetOrdinal("WMS_DSC");
        int ordForSale = reader.GetOrdinal("FORSALE");

        return new VentaDetalleCargadoDTO
        {
            LINE_ID = reader.IsDBNull(ordLineId) ? 0 : reader.GetInt32(ordLineId),
            ITEMCODE = reader.IsDBNull(ordItemCode) ? null : reader.GetString(ordItemCode),
            ITEMNAME = reader.IsDBNull(ordItemName) ? null : reader.GetString(ordItemName),
            CODEBARS = reader.IsDBNull(ordCodebars) ? null : reader.GetString(ordCodebars),
            QUANTITY = reader.IsDBNull(ordQuantity) ? 0 : reader.GetInt32(ordQuantity),
            PRICE = reader.IsDBNull(ordPrice) ? 0 : reader.GetDecimal(ordPrice),
            DSCT_PERCENT = reader.IsDBNull(ordDsctPercent) ? 0 : reader.GetDecimal(ordDsctPercent),
            IGV_AFECT = TryGetString(reader, "IGV_AFECT") ?? TryGetString(reader, "IGV_AFEC"),
            LINE_TOTAL = reader.IsDBNull(ordLineTotal) ? 0 : reader.GetDecimal(ordLineTotal),
            UOMENTRY = reader.IsDBNull(ordUomentry) ? 0 : reader.GetInt32(ordUomentry),
            UGPENTRY = reader.IsDBNull(ordUgpentry) ? 0 : reader.GetInt32(ordUgpentry),
            PRICE_UOM = reader.IsDBNull(ordPriceUom) ? 0 : reader.GetDecimal(ordPriceUom),
            PRICE_BEF = reader.IsDBNull(ordPriceBef) ? 0 : reader.GetDecimal(ordPriceBef),
            PRICE_LIST = TryGetString(reader, "PRICE_LIST"),
            WMS_GIF = reader.IsDBNull(ordWmsGif) ? null : reader.GetString(ordWmsGif),
            WMS_DSC = reader.IsDBNull(ordWmsDsc) ? 0 : reader.GetDecimal(ordWmsDsc),
            FORSALE = reader.IsDBNull(ordForSale) ? 0 : reader.GetDecimal(ordForSale)
        };
    }

    private static VentaLoteCargadoDTO MapVentaLoteCargado(DbDataReader reader)
    {
        int ordLineHeader = reader.GetOrdinal("LINE_HEADER");
        int ordItemCode = reader.GetOrdinal("ITEMCODE");
        int ordQuantity = reader.GetOrdinal("QUANTITY");
        int ordSysnumber = reader.GetOrdinal("SYSNUMBER");
        int ordDistnumber = reader.GetOrdinal("DISTNUMBER");

        return new VentaLoteCargadoDTO
        {
            LINE_HEADER = reader.IsDBNull(ordLineHeader) ? 0 : reader.GetInt32(ordLineHeader),
            ITEMCODE = reader.IsDBNull(ordItemCode) ? null : reader.GetString(ordItemCode),
            QUANTITY = reader.IsDBNull(ordQuantity) ? 0 : reader.GetDecimal(ordQuantity),
            SYSNUMBER = reader.IsDBNull(ordSysnumber) ? null : reader.GetString(ordSysnumber),
            DISTNUMBER = reader.IsDBNull(ordDistnumber) ? null : reader.GetString(ordDistnumber)
        };
    }

    private async Task<DocumentoEstadoInfo> ObtenerEstadoDocumentoAtómico(
        SqlConnection con, SqlTransaction? tran, int docEntry)
    {
        const string sql = @"
            SELECT DOCENTRY, DOCSTATUS, SENDSTATUS, ISNULL(DOCENTRY_SAP, 0) AS DOCENTRY_SAP, ISNULL(WHSCODE, '') AS WHSCODE
            FROM T_SK_ODOCS WITH (UPDLOCK, ROWLOCK)
            WHERE DOCENTRY = @DOCENTRY";

        using var cmd = new SqlCommand(sql, con, tran);
        cmd.Parameters.AddWithValue("@DOCENTRY", docEntry);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new DocumentoEstadoInfo
            {
                DocEntry = reader.GetInt32(0),
                DocStatus = (reader.IsDBNull(1) ? "" : reader.GetString(1)).Trim(),
                SendStatus = (reader.IsDBNull(2) ? "" : reader.GetString(2)).Trim(),
                DocEntrySap = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3)),
                Almacen = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Existe = true
            };
        }
        return new DocumentoEstadoInfo { Existe = false };
    }

    private static string ObtenerDescripcionEstado(DocumentoEstadoInfo estado)
    {
        if (estado.DocStatus == "U") return "PROCESADO/SAP";
        if (estado.DocStatus == "W") return "WMS/SAP";
        if (estado.DocStatus == "P") return "IMPRESO/SAP";
        if (estado.DocStatus == "C") return estado.SendStatus == "S" ? "CANCELADO/SAP" : "CANCELADO";
        if (estado.DocStatus == "L") return "ERROR/LOTE";
        if (estado.DocStatus == "Z") return "ANULADO/INTERMEDIA";
        if (estado.DocEntrySap > 0 || estado.SendStatus == "S") return "CREADO/SAP";
        if (estado.DocStatus == "E") return "BORRADOR";
        if (estado.DocStatus == "O") return "CREADO";
        return !string.IsNullOrWhiteSpace(estado.DocStatus) ? estado.DocStatus : "DESCONOCIDO";
    }

    private static string TruncarTexto(string? valor, int maxLength)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;
        var trimmed = valor.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }

    public async Task<VentaCargarResponseDTO?> PrepararReaperturaVenta(int docEntry, string? docStatusEsperado = null)
    {
        using var con = new SqlConnection(_cadenaSQLPOS);
        await con.OpenAsync();

        var estado = await ObtenerEstadoDocumentoAtómico(con, null, docEntry);
        if (!estado.Existe)
            return null;

        if (estado.DocStatus is "U")
        {
            throw new InvalidOperationException("EL DOCUMENTO EN ESTADO PROCESADO SAP NO SE PUEDE MODIFICAR.");
        }

        if (estado.DocStatus is "W")
        {
            throw new InvalidOperationException("EL DOCUMENTO SE ENCUENTRA EN ESTADO ENVIADO A WMS Y NO SE PUEDE MODIFICAR.");
        }

        if (estado.DocStatus is "C" && estado.SendStatus == "S")
        {
            throw new InvalidOperationException($"La orden N° {docEntry} ya se encuentra anulada.");
        }

        // Validación de concurrencia: si el estado en pantalla difiere del estado real en BD
        if (!string.IsNullOrWhiteSpace(docStatusEsperado) && !string.Equals(estado.DocStatus, docStatusEsperado.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            string descActual = ObtenerDescripcionEstado(estado);
            throw new InvalidOperationException(
                $"El estado de la orden N° {docEntry} ha cambiado en segundo plano (Estado en pantalla: '{docStatusEsperado}', Estado actual en base de datos: '{descActual}'). Por favor actualice la pestaña Búsqueda para consultar el estado actual antes de modificar.");
        }

        // Manejo de sincronización con Importador de SAP:
        // Caso 1: Orden en cola de importador pero aún no creada en SAP (SENDSTATUS = 'N' y DOCENTRY_SAP = 0).
        // Se neutraliza de inmediato a Borrador ('E') para que el importador NO la inserte en SAP mientras se edita.
        if (estado.SendStatus == "N" && estado.DocEntrySap <= 0)
        {
            using var cmdFrenar = new SqlCommand(
                "UPDATE T_SK_ODOCS WITH (UPDLOCK, ROWLOCK) SET DOCSTATUS = 'E', SENDSTATUS = 'S' WHERE DOCENTRY = @DOCENTRY AND SENDSTATUS = 'N'", con);
            cmdFrenar.Parameters.AddWithValue("@DOCENTRY", docEntry);
            await cmdFrenar.ExecuteNonQueryAsync();
        }
        // Caso 2: Orden en tránsito de actualización/WMS/anulación con el importador (SENDSTATUS = 'N' y DOCENTRY_SAP > 0).
        else if (estado.SendStatus == "N" && estado.DocEntrySap > 0)
        {
            throw new InvalidOperationException(
                $"La orden N° {docEntry} está finalizando una sincronización con SAP en este momento. Por favor espere unos segundos e intente reabrirla nuevamente.");
        }
        // Caso 3: Orden ya sincronizada en SAP (DOCENTRY_SAP > 0 y SENDSTATUS == 'S' o estados O/P).
        // Se ejecuta SOPH_SP_CAN_SKODOCS para instruir al importador a que anule la orden previa en SAP.
        else if (estado.DocStatus is "O" or "P" || estado.SendStatus == "S" || estado.DocEntrySap > 0)
        {
            using var cmdCan = new SqlCommand("SOPH_SP_CAN_SKODOCS", con);
            cmdCan.CommandType = CommandType.StoredProcedure;
            var param = new SqlParameter("@DOCENTRY", SqlDbType.Int) { Direction = ParameterDirection.InputOutput, Value = docEntry };
            cmdCan.Parameters.Add(param);
            await cmdCan.ExecuteNonQueryAsync();
        }

        var venta = await CargarVenta(docEntry);
        return venta;
    }

    public async Task AnularVenta(int docEntry, bool puedeAnularEnviadoWms = true, string? docStatusEsperado = null)
    {
        using var con = new SqlConnection(_cadenaSQLPOS);
        await con.OpenAsync();

        var estado = await ObtenerEstadoDocumentoAtómico(con, null, docEntry);
        if (!estado.Existe)
        {
            throw new InvalidOperationException($"La orden N° {docEntry} no existe.");
        }

        if (estado.DocStatus is "W" && !puedeAnularEnviadoWms)
        {
            throw new InvalidOperationException("No tiene permisos para anular órdenes en estado ENVIADO WMS.");
        }

        if (estado.DocStatus is "U")
        {
            throw new InvalidOperationException(
                "EL DOCUMENTO EN ESTADO PROCESADO SAP NO SE PUEDE ANULAR");
        }

        if (estado.DocStatus is "C" or "Z")
        {
            throw new InvalidOperationException(
                $"El documento N° {docEntry} ya se encuentra anulado.");
        }

        if (!string.IsNullOrWhiteSpace(docStatusEsperado) && !string.Equals(estado.DocStatus, docStatusEsperado.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            string descActual = ObtenerDescripcionEstado(estado);
            throw new InvalidOperationException(
                $"El estado de la orden N° {docEntry} ha cambiado en segundo plano (Estado actual: '{descActual}'). Por favor actualice la pestaña Búsqueda.");
        }

        using var cmd = new SqlCommand("SOPH_SP_CAN_SKODOCS", con);
        cmd.CommandType = CommandType.StoredProcedure;
        var param = new SqlParameter("@DOCENTRY", SqlDbType.Int);
        param.Direction = ParameterDirection.InputOutput;
        param.Value = docEntry;
        cmd.Parameters.Add(param);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task TrasladarVenta(int docEntry, string? docStatusEsperado = null)
    {
        using var con = new SqlConnection(_cadenaSQLPOS);
        await con.OpenAsync();

        var estado = await ObtenerEstadoDocumentoAtómico(con, null, docEntry);
        if (!estado.Existe)
        {
            throw new InvalidOperationException($"La orden N° {docEntry} no existe.");
        }

        if (estado.SendStatus == "S" || estado.DocEntrySap > 0)
        {
            throw new InvalidOperationException(
                $"El documento N° {docEntry} ya fue trasladado a SAP previamente (DocEntry SAP: {estado.DocEntrySap}).");
        }

        if (estado.DocStatus is "E")
        {
            throw new InvalidOperationException(
                $"El documento N° {docEntry} está en estado BORRADOR. Primero debe completarlo y guardarlo como venta antes de trasladarlo.");
        }

        if (estado.DocStatus is "W")
        {
            throw new InvalidOperationException(
                $"El documento N° {docEntry} ya se encuentra en WMS/Almacén.");
        }

        if (estado.DocStatus is "C" or "U")
        {
            throw new InvalidOperationException(
                $"El documento N° {docEntry} se encuentra en estado '{estado.DocStatus}' y no se puede trasladar.");
        }

        if (!string.IsNullOrWhiteSpace(docStatusEsperado) && !string.Equals(estado.DocStatus, docStatusEsperado.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            string descActual = ObtenerDescripcionEstado(estado);
            throw new InvalidOperationException(
                $"El estado de la orden N° {docEntry} ha cambiado en segundo plano (Estado actual: '{descActual}'). Por favor actualice la pestaña Búsqueda.");
        }

        using var cmd = new SqlCommand("SOPH_SP_DC_SKODOCS", con);
        cmd.CommandType = CommandType.StoredProcedure;
        var param = new SqlParameter("@DOCENTRY", SqlDbType.Int);
        param.Direction = ParameterDirection.InputOutput;
        param.Value = docEntry;
        cmd.Parameters.Add(param);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EnviarWMS(int docEntry, string? docStatusEsperado = null)
    {
        using var con = new SqlConnection(_cadenaSQLPOS);
        await con.OpenAsync();

        var estado = await ObtenerEstadoDocumentoAtómico(con, null, docEntry);
        if (!estado.Existe)
        {
            throw new InvalidOperationException($"La orden N° {docEntry} no existe.");
        }

        if (estado.DocStatus is "W")
        {
            throw new InvalidOperationException(
                $"El documento N° {docEntry} ya se encuentra enviado a WMS.");
        }

        if (estado.DocStatus is "U")
        {
            throw new InvalidOperationException(
                $"El documento N° {docEntry} ya fue procesado en SAP.");
        }

        if (estado.DocStatus is "C" or "Z")
        {
            throw new InvalidOperationException(
                $"El documento N° {docEntry} se encuentra anulado.");
        }

        if (estado.DocStatus is "E")
        {
            throw new InvalidOperationException(
                $"El documento N° {docEntry} es un borrador y no puede enviarse a WMS.");
        }

        if (estado.SendStatus == "E")
        {
            throw new InvalidOperationException(
                $"La orden N° {docEntry} presentó un error al sincronizarse con SAP. Revise el log del importador para más detalles.");
        }

        // Si la orden aún no ha terminado de sincronizarse en SAP (SENDSTATUS == 'N' o DOCENTRY_SAP <= 0):
        if (estado.SendStatus == "N" || estado.DocEntrySap <= 0)
        {
            throw new InvalidOperationException(
                $"La orden N° {docEntry} se está sincronizando con SAP en este momento. Por favor espere unos segundos e intente nuevamente.");
        }

        if (!string.IsNullOrWhiteSpace(docStatusEsperado) && !string.Equals(estado.DocStatus, docStatusEsperado.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            string descActual = ObtenerDescripcionEstado(estado);
            throw new InvalidOperationException(
                $"El estado de la orden N° {docEntry} ha cambiado en segundo plano (Estado actual: '{descActual}'). Por favor actualice la pestaña Búsqueda.");
        }

        using var cmd = new SqlCommand("SOPH_SP_WMS_SKODOCS", con);
        cmd.CommandType = CommandType.StoredProcedure;
        var param = new SqlParameter("@DOCENTRY", SqlDbType.Int);
        param.Direction = ParameterDirection.InputOutput;
        param.Value = docEntry;
        cmd.Parameters.Add(param);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarcarImpreso(int docEntry)
    {
        using var con = new SqlConnection(_cadenaSQLPOS);
        await con.OpenAsync();
        using var cmd = new SqlCommand("SOPH_SP_PR_SKODOCS", con);
        cmd.CommandType = CommandType.StoredProcedure;
        var param = new SqlParameter("@DOCENTRY", SqlDbType.Int);
        param.Direction = ParameterDirection.InputOutput;
        param.Value = docEntry;
        cmd.Parameters.Add(param);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<DataTable?> ObtenerReporteTicket(int docEntrySap)
    {
        var commandText = _hana.BuildProcedureCall("SOPH_SP_RPT_TICKET", 1);

        var resultado = await _hana.QueryDataTableAsync(
            commandText,
            CommandType.Text,
            _hana.CreateParameter("p1", docEntrySap)
        );

        if (resultado.Rows.Count == 0)
            return null;

        resultado.TableName = "DT_TICKET";

        return resultado;
    }

    public async Task<DataTable?> ObtenerReportePreliminarSap(int docEntrySap, int docEntryOwtr)
    {
        var commandText = _hana.BuildProcedureCall("SOPHOS_LYT_OV", 2);

        var resultado = await _hana.QueryDataTableAsync(
            commandText,
            CommandType.Text,
            _hana.CreateParameter("p1", docEntrySap),
            _hana.CreateParameter("p2", docEntryOwtr)
        );

        if (resultado.Rows.Count == 0)
            return null;

        resultado.TableName = "DT_OV";

        return resultado;
    }

    public async Task<DataTable?> ObtenerReportePreliminarPv(int docEntry)
    {
        var resultado = await _sql.QueryDataTableAsync(
            "SOPHOS_LYT_OV",
            TipoConexionSql.POS,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@DocEntry", docEntry),
            _sql.CreateParameter("@company", _hana.CompanyDB)
        );

        if (resultado.Rows.Count == 0)
            return null;

        resultado.TableName = "DT_PV";

        return resultado;
    }

    // ========== CLIENTES BLOQUEADOS ==========

    public async Task<List<ClienteBloqueadoDTO>> BuscarClientesBloqueados(ClienteBloqueadoFiltroDTO filtro)
    {
        var commandText = "SP_BUSCAR_CLIENTES_BLOQUEADOS";

        var lista = await _sql.QueryListAsync(
            commandText,
            MapClienteBloqueado,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@CARCODE", (object?)filtro.CARCODE ?? DBNull.Value),
            _sql.CreateParameter("@ESTADO", (object?)filtro.ESTADO ?? DBNull.Value)
        );

        // Enriquecer con datos del cliente desde SAP
        if (lista.Count > 0)
        {
            var codigos = lista.Select(x => x.CARCODE).Distinct().ToList();
            var clientesSap = new Dictionary<string, ClienteBusquedaSapDTO>();

            foreach (var codigo in codigos)
            {
                try
                {
                    var clientes = await BuscarCliente(codigo);
                    var match = clientes.FirstOrDefault(c =>
                        string.Equals(c.CODIGO_CLIENTE, codigo, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        clientesSap[codigo] = match;
                }
                catch { /* Si falla SAP, deja vacío */ }
            }

            foreach (var item in lista)
            {
                if (clientesSap.TryGetValue(item.CARCODE ?? "", out var sap))
                {
                    item.CLIENTE = sap.CLIENTE;
                    item.RUC = sap.RUC;
                }
            }
        }

        return lista;
    }

    private static ClienteBloqueadoDTO MapClienteBloqueado(DbDataReader reader)
    {
        int ordId = reader.GetOrdinal("ID_CLIENTE_BLOQUEADO");
        int ordCarcode = reader.GetOrdinal("CARCODE");
        int ordMotivo = reader.GetOrdinal("MOTIVO_BLOQUEO");
        int ordEstado = reader.GetOrdinal("ESTADO");
        int ordUsuCre = reader.GetOrdinal("USUARIO_CREACION");
        int ordFechaCre = reader.GetOrdinal("FECHAHORA_CREACION");
        int ordUsuMod = reader.GetOrdinal("USUARIO_MODIFICACION");
        int ordFechaMod = reader.GetOrdinal("FECHAHORA_MODIFICACION");

        return new ClienteBloqueadoDTO
        {
            ID_CLIENTE_BLOQUEADO = reader.GetInt32(ordId),
            CARCODE = reader.IsDBNull(ordCarcode) ? null : reader.GetString(ordCarcode),
            MOTIVO_BLOQUEO = reader.IsDBNull(ordMotivo) ? null : reader.GetString(ordMotivo),
            ESTADO = !reader.IsDBNull(ordEstado) && reader.GetBoolean(ordEstado),
            USUARIO_CREACION = reader.IsDBNull(ordUsuCre) ? null : reader.GetInt32(ordUsuCre),
            FECHAHORA_CREACION = reader.IsDBNull(ordFechaCre) ? null : reader.GetString(ordFechaCre),
            USUARIO_MODIFICACION = reader.IsDBNull(ordUsuMod) ? null : reader.GetInt32(ordUsuMod),
            FECHAHORA_MODIFICACION = reader.IsDBNull(ordFechaMod) ? null : reader.GetString(ordFechaMod)
        };
    }

    public async Task<ClienteBloqueadoDTO?> ObtenerClienteBloqueado(int id)
    {
        var commandText = "SP_BUSCAR_CLIENTES_BLOQUEADOS";

        var lista = await _sql.QueryListAsync(
            commandText,
            MapClienteBloqueado,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@CARCODE", DBNull.Value),
            _sql.CreateParameter("@ESTADO", DBNull.Value)
        );

        var item = lista.FirstOrDefault(x => x.ID_CLIENTE_BLOQUEADO == id);
        if (item == null) return null;

        // Enriquecer con datos SAP
        try
        {
            var clientes = await BuscarCliente(item.CARCODE ?? "");
            var match = clientes.FirstOrDefault(c =>
                string.Equals(c.CODIGO_CLIENTE, item.CARCODE, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                item.CLIENTE = match.CLIENTE;
                item.RUC = match.RUC;
            }
        }
        catch { /* Si falla SAP, deja vacío */ }

        return item;
    }

    public async Task<int> InsertarClienteBloqueado(ClienteBloqueadoGuardarDTO dto)
    {
        var commandText = "SP_INSERTAR_CLIENTE_BLOQUEADO";

        var resultado = await _sql.QuerySingleAsync(
            commandText,
            reader => reader.GetInt32(0),
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@CARCODE", dto.CARCODE),
            _sql.CreateParameter("@MOTIVO_BLOQUEO", dto.MOTIVO_BLOQUEO),
            _sql.CreateParameter("@USUARIO_CREACION", dto.USUARIO)
        );

        return resultado;
    }

    public async Task<int> ActualizarClienteBloqueado(ClienteBloqueadoGuardarDTO dto)
    {
        var commandText = "SP_ACTUALIZAR_CLIENTE_BLOQUEADO";

        return await _sql.ExecuteNonQueryAsync(
            commandText,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@ID_CLIENTE_BLOQUEADO", dto.ID_CLIENTE_BLOQUEADO),
            _sql.CreateParameter("@MOTIVO_BLOQUEO", dto.MOTIVO_BLOQUEO),
            _sql.CreateParameter("@ESTADO", dto.ESTADO),
            _sql.CreateParameter("@USUARIO_MODIFICACION", dto.USUARIO)
        );
    }

    public async Task<int> EliminarClienteBloqueado(int id, int usuarioModificacion)
    {
        var commandText = "SP_ELIMINAR_CLIENTE_BLOQUEADO";

        return await _sql.ExecuteNonQueryAsync(
            commandText,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@ID_CLIENTE_BLOQUEADO", id),
            _sql.CreateParameter("@USUARIO_MODIFICACION", usuarioModificacion)
        );
    }

    public async Task<ClienteBloqueadoDTO?> ValidarClienteBloqueado(string carcode)
    {
        var lista = await BuscarClientesBloqueados(new ClienteBloqueadoFiltroDTO { CARCODE = carcode, ESTADO = true });
        return lista.FirstOrDefault();
    }

    public async Task<List<ClienteBusquedaSapDTO>> ObtenerDetalleClientesSap(List<string> codigos)
    {
        if (codigos is null || codigos.Count == 0)
            return new List<ClienteBusquedaSapDTO>();

        var distinctCodes = codigos.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        var chunks = distinctCodes.Chunk(500).ToList();

        var tasks = chunks.Select(async chunk =>
        {
            var codigosConcatenados = string.Join(",", chunk);
            var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_CLIENTES_BLOQUEADOS_DETALLE", 1);

            return await _hana.QueryListAsync(
                commandText,
                MapClienteSapDetalle,
                CommandType.Text,
                _hana.CreateParameter("p1", codigosConcatenados)
            );
        });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private static ClienteBusquedaSapDTO MapClienteSapDetalle(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");
        int ordRuc = reader.GetOrdinal("RUC");

        return new ClienteBusquedaSapDTO
        {
            CODIGO_CLIENTE = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            CLIENTE = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre),
            RUC = reader.IsDBNull(ordRuc) ? null : reader.GetString(ordRuc)
        };
    }

    public async Task<List<ClienteBusquedaSapDTO>> ValidarRucsBatch(List<string> rucs)
    {
        if (rucs is null || rucs.Count == 0)
            return new List<ClienteBusquedaSapDTO>();

        var distinctRucs = rucs.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();
        var chunks = distinctRucs.Chunk(500).ToList();

        var tasks = chunks.Select(async chunk =>
        {
            var rucsConcatenados = string.Join(",", chunk);
            var commandText = _hana.BuildProcedureCall("CBF_SP_PV_VALIDAR_RUCS_CLIENTES", 1);

            return await _hana.QueryListAsync(
                commandText,
                MapClienteSapDetalle,
                CommandType.Text,
                _hana.CreateParameter("p1", rucsConcatenados)
            );
        });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    // ========== ARTICULOS FRACCIONADOS ==========

    public async Task<List<ArticuloFraccionadoDTO>> BuscarArticulosFraccionados(ArticuloFraccionadoFiltroDTO filtro)
    {
        var commandText = "SP_BUSCAR_ARTICULOS_FRACCIONADOS";

        var lista = await _sql.QueryListAsync(
            commandText,
            MapArticuloFraccionado,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@ITEMCODE", (object?)filtro.ITEMCODE ?? DBNull.Value),
            _sql.CreateParameter("@ESTADO", (object?)filtro.ESTADO ?? DBNull.Value)
        );

        if (lista.Count > 0)
        {
            var codigos = lista.Select(x => x.ITEMCODE ?? "").Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            var articulosSap = new Dictionary<string, ArticuloBusquedaSapDTO>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var sapList = await ObtenerDetalleArticulosSap(codigos);
                foreach (var sap in sapList)
                {
                    if (!string.IsNullOrWhiteSpace(sap.CODIGO))
                        articulosSap[sap.CODIGO] = sap;
                }
            }
            catch { /* Si falla SAP, deja vacío */ }

            foreach (var item in lista)
            {
                if (item.ITEMCODE != null && articulosSap.TryGetValue(item.ITEMCODE, out var sap))
                {
                    item.ARTICULO = sap.DESCRIPCION;
                }
            }
        }

        return lista;
    }

    private static ArticuloFraccionadoDTO MapArticuloFraccionado(DbDataReader reader)
    {
        int ordId = reader.GetOrdinal("ID_ARTICULO_FRACCIONADO");
        int ordItemcode = reader.GetOrdinal("ITEMCODE");
        int ordFraccionado = reader.GetOrdinal("FRACCIONADO");
        int ordMontoMinimo = -1;
        try { ordMontoMinimo = reader.GetOrdinal("MONTO_MINIMO"); } catch { }
        int ordEstado = reader.GetOrdinal("ESTADO");
        int ordUsuCre = reader.GetOrdinal("USUARIO_CREACION");
        int ordFechaCre = reader.GetOrdinal("FECHAHORA_CREACION");
        int ordUsuMod = reader.GetOrdinal("USUARIO_MODIFICACION");
        int ordFechaMod = reader.GetOrdinal("FECHAHORA_MODIFICACION");

        int fraccionadoVal = reader.IsDBNull(ordFraccionado) ? 0 : reader.GetInt32(ordFraccionado);
        int montoMinimoVal = (ordMontoMinimo != -1 && !reader.IsDBNull(ordMontoMinimo))
            ? reader.GetInt32(ordMontoMinimo)
            : fraccionadoVal;

        return new ArticuloFraccionadoDTO
        {
            ID_ARTICULO_FRACCIONADO = reader.GetInt32(ordId),
            ITEMCODE = reader.IsDBNull(ordItemcode) ? null : reader.GetString(ordItemcode),
            FRACCIONADO = fraccionadoVal,
            MONTO_MINIMO = montoMinimoVal,
            ESTADO = !reader.IsDBNull(ordEstado) && reader.GetBoolean(ordEstado),
            USUARIO_CREACION = reader.IsDBNull(ordUsuCre) ? null : reader.GetInt32(ordUsuCre),
            FECHAHORA_CREACION = reader.IsDBNull(ordFechaCre) ? null : reader.GetString(ordFechaCre),
            USUARIO_MODIFICACION = reader.IsDBNull(ordUsuMod) ? null : reader.GetInt32(ordUsuMod),
            FECHAHORA_MODIFICACION = reader.IsDBNull(ordFechaMod) ? null : reader.GetString(ordFechaMod)
        };
    }

    public async Task<ArticuloFraccionadoDTO?> ObtenerArticuloFraccionado(int id)
    {
        var commandText = "SP_BUSCAR_ARTICULOS_FRACCIONADOS";

        var lista = await _sql.QueryListAsync(
            commandText,
            MapArticuloFraccionado,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@ITEMCODE", DBNull.Value),
            _sql.CreateParameter("@ESTADO", DBNull.Value)
        );

        var item = lista.FirstOrDefault(x => x.ID_ARTICULO_FRACCIONADO == id);
        if (item == null) return null;

        try
        {
            var articulos = await BuscarArticulosPorCodigo(item.ITEMCODE ?? "", 0, "");
            var match = articulos.FirstOrDefault(a =>
                string.Equals(a.CODIGO, item.ITEMCODE, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                item.ARTICULO = match.DESCRIPCION;
            }
        }
        catch { /* Si falla SAP, deja vacío */ }

        return item;
    }

    public async Task<int> InsertarArticuloFraccionado(ArticuloFraccionadoGuardarDTO dto)
    {
        var commandText = "SP_INSERTAR_ARTICULO_FRACCIONADO";

        var resultado = await _sql.QuerySingleAsync(
            commandText,
            reader => reader.GetInt32(0),
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@ITEMCODE", dto.ITEMCODE),
            _sql.CreateParameter("@FRACCIONADO", dto.FRACCIONADO),
            _sql.CreateParameter("@MONTO_MINIMO", dto.MONTO_MINIMO <= 0 ? dto.FRACCIONADO : dto.MONTO_MINIMO),
            _sql.CreateParameter("@USUARIO_CREACION", dto.USUARIO)
        );

        return resultado;
    }

    public async Task<int> ActualizarArticuloFraccionado(ArticuloFraccionadoGuardarDTO dto)
    {
        var commandText = "SP_ACTUALIZAR_ARTICULO_FRACCIONADO";

        return await _sql.ExecuteNonQueryAsync(
            commandText,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@ID_ARTICULO_FRACCIONADO", dto.ID_ARTICULO_FRACCIONADO),
            _sql.CreateParameter("@FRACCIONADO", dto.FRACCIONADO),
            _sql.CreateParameter("@MONTO_MINIMO", dto.MONTO_MINIMO <= 0 ? dto.FRACCIONADO : dto.MONTO_MINIMO),
            _sql.CreateParameter("@ESTADO", dto.ESTADO),
            _sql.CreateParameter("@USUARIO_MODIFICACION", dto.USUARIO)
        );
    }

    public async Task<int> EliminarArticuloFraccionado(int id, int usuarioModificacion)
    {
        var commandText = "SP_ELIMINAR_ARTICULO_FRACCIONADO";

        return await _sql.ExecuteNonQueryAsync(
            commandText,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@ID_ARTICULO_FRACCIONADO", id),
            _sql.CreateParameter("@USUARIO_MODIFICACION", usuarioModificacion)
        );
    }

    public async Task<int> EliminarTodosArticulosFraccionados(int usuarioModificacion)
    {
        var commandText = "SP_ELIMINAR_TODOS_ARTICULOS_FRACCIONADOS";

        return await _sql.ExecuteNonQueryAsync(
            commandText,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@USUARIO_MODIFICACION", usuarioModificacion)
        );
    }

    public async Task<ArticuloFraccionadoDTO?> ValidarArticuloFraccionado(string itemcode)
    {
        var lista = await BuscarArticulosFraccionados(new ArticuloFraccionadoFiltroDTO { ITEMCODE = itemcode, ESTADO = true });
        return lista.FirstOrDefault();
    }

    public async Task<List<ArticuloBusquedaSapDTO>> ObtenerDetalleArticulosSap(List<string> codigos)
    {
        if (codigos is null || codigos.Count == 0)
            return new List<ArticuloBusquedaSapDTO>();

        var distinctCodes = codigos.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        var chunks = distinctCodes.Chunk(500).ToList();

        var tasks = chunks.Select(async chunk =>
        {
            var codigosConcatenados = string.Join(",", chunk);
            var commandText = _hana.BuildProcedureCall("CBF_SP_PV_BUSCAR_ARTICULOS_FRACCIONADOS_DETALLE", 1);

            return await _hana.QueryListAsync(
                commandText,
                MapArticuloFraccionadoDetalle,
                CommandType.Text,
                _hana.CreateParameter("p1", codigosConcatenados)
            );
        });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private static ArticuloBusquedaSapDTO MapArticuloFraccionadoDetalle(DbDataReader reader)
    {
        int ordCodigo = reader.GetOrdinal("CODIGO");
        int ordNombre = reader.GetOrdinal("NOMBRE");

        return new ArticuloBusquedaSapDTO
        {
            CODIGO = reader.IsDBNull(ordCodigo) ? null : reader.GetString(ordCodigo),
            DESCRIPCION = reader.IsDBNull(ordNombre) ? null : reader.GetString(ordNombre)
        };
    }

    public async Task<List<ArticuloBusquedaSapDTO>> ValidarItemcodesBatch(List<string> itemcodes)
    {
        return await ObtenerDetalleArticulosSap(itemcodes);
    }

    public async Task<List<ArticuloFraccionadoDTO>> ValidarArticulosFraccionadosBatch(List<string> itemcodes)
    {
        if (itemcodes is null || itemcodes.Count == 0)
            return new List<ArticuloFraccionadoDTO>();

        var commandText = "SP_BUSCAR_ARTICULOS_FRACCIONADOS";

        var lista = await _sql.QueryListAsync(
            commandText,
            MapArticuloFraccionado,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@ITEMCODE", DBNull.Value),
            _sql.CreateParameter("@ESTADO", true)
        );

        return lista.Where(x => itemcodes.Contains(x.ITEMCODE ?? "", StringComparer.OrdinalIgnoreCase)).ToList();
    }

    public async Task<OrdenCondicionPagoSapDTO?> ObtenerOrdenCondicionPagoSap(int docEntrySap)
    {
        var commandText = _hana.BuildProcedureCall("CBF_SP_PV_OBTENER_ORDEN_CONDICION_PAGO", 1);
        var lista = await _hana.QueryListAsync(
            commandText,
            MapOrdenCondicionPagoSap,
            CommandType.Text,
            _hana.CreateParameter("p1", docEntrySap)
        );
        return lista.FirstOrDefault();
    }

    private static OrdenCondicionPagoSapDTO MapOrdenCondicionPagoSap(DbDataReader reader)
    {
        int ordDocEntry = reader.GetOrdinal("DocEntry");
        int ordDocNum = reader.GetOrdinal("DocNum");
        int ordDocStatus = reader.GetOrdinal("DocStatus");
        int ordCanceled = reader.GetOrdinal("CANCELED");
        int ordUSophEnwms = reader.GetOrdinal("U_SOPH_ENWMS");
        int ordCardCode = reader.GetOrdinal("CardCode");
        int ordCardName = reader.GetOrdinal("CardName");
        int ordLicTradNum = reader.GetOrdinal("LicTradNum");
        int ordEstadoSap = reader.GetOrdinal("ESTADO_SAP");
        int ordGroupNum = reader.GetOrdinal("GroupNum");
        int ordPymntGroup = reader.GetOrdinal("PymntGroup");

        return new OrdenCondicionPagoSapDTO
        {
            DocEntry = reader.IsDBNull(ordDocEntry) ? 0 : reader.GetInt32(ordDocEntry),
            DocNum = reader.IsDBNull(ordDocNum) ? 0 : reader.GetInt32(ordDocNum),
            DocStatus = reader.IsDBNull(ordDocStatus) ? null : reader.GetString(ordDocStatus),
            Canceled = reader.IsDBNull(ordCanceled) ? null : reader.GetString(ordCanceled),
            USophEnwms = reader.IsDBNull(ordUSophEnwms) ? null : reader.GetString(ordUSophEnwms),
            CardCode = reader.IsDBNull(ordCardCode) ? null : reader.GetString(ordCardCode),
            CardName = reader.IsDBNull(ordCardName) ? null : reader.GetString(ordCardName),
            LicTradNum = reader.IsDBNull(ordLicTradNum) ? null : reader.GetString(ordLicTradNum),
            EstadoSap = reader.IsDBNull(ordEstadoSap) ? null : reader.GetString(ordEstadoSap),
            GroupNum = reader.IsDBNull(ordGroupNum) ? 0 : Convert.ToInt32(reader.GetValue(ordGroupNum)),
            PymntGroup = reader.IsDBNull(ordPymntGroup) ? null : reader.GetString(ordPymntGroup)
        };
    }

    public async Task<object> ActualizarCondicionPago_ServiceLayer(int docEntryPv, int docEntrySap, int groupNumAnterior, string? condicionAnterior, int groupNumNuevo, string condicionNuevo, string usuario, int? docNumSap = null, string? motivo = null)
    {
        var logContext = new SapLogContext(
            modulo: "PUNTO_VENTA_COND_PAGO",
            usuario: string.IsNullOrWhiteSpace(usuario) ? "SISTEMA" : usuario.Trim(),
            docEntry: docEntrySap > 0 ? docEntrySap.ToString() : null,
            docNum: docNumSap?.ToString()
        );

        if (docEntrySap <= 0)
        {
            await FileLogger.Warning("Intento de actualización con DocEntry SAP inválido.", logContext);
            return new { success = false, message = "El DocEntry de la orden SAP es inválido." };
        }

        if (groupNumNuevo <= 0)
        {
            await FileLogger.Warning("Intento de actualización con GroupNum inválido.", logContext);
            return new { success = false, message = "Debe seleccionar una condición de pago válida." };
        }

        await FileLogger.Info($"[INICIO] Actualizar Condición Pago SAP -> DocEntry PV: {docEntryPv} | DocEntry SAP: {docEntrySap} | DocNum: {docNumSap} | Ant: {condicionAnterior} ({groupNumAnterior}) -> Nue: {condicionNuevo} ({groupNumNuevo}) | Usuario: {usuario}", logContext);

        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["PaymentGroupCode"] = groupNumNuevo
            };

            string endpoint = $"Orders({docEntrySap})";

            var (success, response) = await _sapClient.SendAsync(HttpMethod.Patch, endpoint, payload, logContext: logContext);

            if (success)
            {
                await FileLogger.Info($"[OK] Orden '{docEntrySap}' (DocNum: {docNumSap}) actualizada exitosamente en SAP con PaymentGroupCode={groupNumNuevo}.", logContext);

                // 1. Actualizar EXCLUSIVAMENTE el campo de condición de pago en Punto de Venta (T_SK_ODOCS)
                try
                {
                    int filasAfectadas = await ActualizarCondicionPagoPuntoVenta(docEntryPv, docEntrySap, groupNumNuevo, condicionNuevo);
                    await FileLogger.Info($"[OK-PV] PAYFORM actualizado en T_SK_ODOCS para DocEntry PV: {docEntryPv} / SAP: {docEntrySap}. Filas afectadas: {filasAfectadas}", logContext);
                }
                catch (Exception exPv)
                {
                    await FileLogger.Error($"Error al actualizar PAYFORM en T_SK_ODOCS para orden SAP '{docEntrySap}': {exPv.Message}", logContext);
                }

                // 2. Registrar historial de auditoría
                try
                {
                    await RegistrarHistorialCondicionPago(new BE_HistorialCondicionPago
                    {
                        DOCENTRY_PV = docEntryPv,
                        DOCENTRY_SAP = docEntrySap,
                        DOCNUM_SAP = docNumSap,
                        GROUPNUM_ANTERIOR = groupNumAnterior,
                        CONDICION_PAGO_ANTERIOR = condicionAnterior,
                        GROUPNUM_NUEVO = groupNumNuevo,
                        CONDICION_PAGO_NUEVO = condicionNuevo,
                        USUARIO = string.IsNullOrWhiteSpace(usuario) ? "SISTEMA" : usuario.Trim(),
                        MOTIVO = motivo
                    });
                }
                catch (Exception exHist)
                {
                    await FileLogger.Error($"Error al registrar historial en SQL Server para orden SAP '{docEntrySap}': {exHist.Message}", logContext);
                }

                return new
                {
                    success = true,
                    message = "Condición de pago actualizada correctamente en SAP y Punto de Venta."
                };
            }

            var (sapCode, sapMensaje) = SapResponseParser.ExtraerDetalleError(response);
            string detalleErrorLog = sapCode != 0 ? $"[ERROR SAP {sapCode}] {sapMensaje}" : $"[ERROR SAP] {sapMensaje}";

            await FileLogger.Error($"No se pudo actualizar orden '{docEntrySap}'. Detalle: {detalleErrorLog}", logContext);

            return new
            {
                success = false,
                message = SapResponseParser.FormatearMensajeError(sapCode, sapMensaje)
            };
        }
        catch (Exception ex)
        {
            await FileLogger.Exception(ex, logContext);
            return new
            {
                success = false,
                message = $"Excepción al comunicarse con Service Layer: {ex.Message}"
            };
        }
    }

    public async Task<int> RegistrarHistorialCondicionPago(BE_HistorialCondicionPago historial)
    {
        var commandText = "SP_INSERTAR_HISTORIAL_CAMBIO_CONDICION_PAGO";

        return await _sql.QuerySingleAsync(
            commandText,
            reader => reader.GetInt32(0),
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@DOCENTRY_PV", historial.DOCENTRY_PV),
            _sql.CreateParameter("@DOCENTRY_SAP", historial.DOCENTRY_SAP),
            _sql.CreateParameter("@DOCNUM_SAP", (object?)historial.DOCNUM_SAP ?? DBNull.Value),
            _sql.CreateParameter("@GROUPNUM_ANTERIOR", historial.GROUPNUM_ANTERIOR),
            _sql.CreateParameter("@CONDICION_PAGO_ANTERIOR", (object?)historial.CONDICION_PAGO_ANTERIOR ?? DBNull.Value),
            _sql.CreateParameter("@GROUPNUM_NUEVO", historial.GROUPNUM_NUEVO),
            _sql.CreateParameter("@CONDICION_PAGO_NUEVO", historial.CONDICION_PAGO_NUEVO),
            _sql.CreateParameter("@USUARIO", historial.USUARIO),
            _sql.CreateParameter("@MOTIVO", (object?)historial.MOTIVO ?? DBNull.Value)
        );
    }

    public async Task<List<BE_HistorialCondicionPago>> ListarHistorialCondicionPago(int docEntryPv, int docEntrySap)
    {
        var commandText = "SP_LISTAR_HISTORIAL_CAMBIO_CONDICION_PAGO";

        return await _sql.QueryListAsync(
            commandText,
            MapHistorialCondicionPago,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            _sql.CreateParameter("@DOCENTRY_PV", docEntryPv > 0 ? (object)docEntryPv : DBNull.Value),
            _sql.CreateParameter("@DOCENTRY_SAP", docEntrySap > 0 ? (object)docEntrySap : DBNull.Value)
        );
    }

    private static BE_HistorialCondicionPago MapHistorialCondicionPago(DbDataReader reader)
    {
        int ordId = reader.GetOrdinal("ID_HISTORIAL");
        int ordDocEntryPv = reader.GetOrdinal("DOCENTRY_PV");
        int ordDocEntrySap = reader.GetOrdinal("DOCENTRY_SAP");
        int ordDocNumSap = reader.GetOrdinal("DOCNUM_SAP");
        int ordGroupNumAnt = reader.GetOrdinal("GROUPNUM_ANTERIOR");
        int ordCondAnt = reader.GetOrdinal("CONDICION_PAGO_ANTERIOR");
        int ordGroupNumNue = reader.GetOrdinal("GROUPNUM_NUEVO");
        int ordCondNue = reader.GetOrdinal("CONDICION_PAGO_NUEVO");
        int ordUsuario = reader.GetOrdinal("USUARIO");
        int ordFechaReg = reader.GetOrdinal("FECHA_REGISTRO");
        int ordFechaRegTxt = reader.GetOrdinal("FECHA_REGISTRO_TEXTO");
        int ordMotivo = reader.GetOrdinal("MOTIVO");

        return new BE_HistorialCondicionPago
        {
            ID_HISTORIAL = reader.IsDBNull(ordId) ? 0 : reader.GetInt32(ordId),
            DOCENTRY_PV = reader.IsDBNull(ordDocEntryPv) ? 0 : reader.GetInt32(ordDocEntryPv),
            DOCENTRY_SAP = reader.IsDBNull(ordDocEntrySap) ? 0 : reader.GetInt32(ordDocEntrySap),
            DOCNUM_SAP = reader.IsDBNull(ordDocNumSap) ? null : reader.GetInt32(ordDocNumSap),
            GROUPNUM_ANTERIOR = reader.IsDBNull(ordGroupNumAnt) ? 0 : reader.GetInt32(ordGroupNumAnt),
            CONDICION_PAGO_ANTERIOR = reader.IsDBNull(ordCondAnt) ? null : reader.GetString(ordCondAnt),
            GROUPNUM_NUEVO = reader.IsDBNull(ordGroupNumNue) ? 0 : reader.GetInt32(ordGroupNumNue),
            CONDICION_PAGO_NUEVO = reader.IsDBNull(ordCondNue) ? null : reader.GetString(ordCondNue),
            USUARIO = reader.IsDBNull(ordUsuario) ? null : reader.GetString(ordUsuario),
            FECHA_REGISTRO = reader.IsDBNull(ordFechaReg) ? null : reader.GetDateTime(ordFechaReg),
            FECHA_REGISTRO_TEXTO = reader.IsDBNull(ordFechaRegTxt) ? null : reader.GetString(ordFechaRegTxt),
            MOTIVO = reader.IsDBNull(ordMotivo) ? null : reader.GetString(ordMotivo)
        };
    }

    public async Task<int> ActualizarCondicionPagoPuntoVenta(int docEntryPv, int docEntrySap, int groupNumNuevo, string? condicionNuevo = null)
    {
        if (docEntryPv <= 0 && docEntrySap <= 0) return 0;

        using var con = new SqlConnection(_cadenaSQLPOS);
        await con.OpenAsync();

        string valorPayform = groupNumNuevo > 0 ? groupNumNuevo.ToString() : (condicionNuevo?.Trim() ?? "");

        const string sql = @"
            UPDATE T_SK_ODOCS
            SET PAYFORM = @PAYFORM
            WHERE (DOCENTRY = @DOCENTRY_PV)";

        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@PAYFORM", string.IsNullOrWhiteSpace(valorPayform) ? (object)DBNull.Value : valorPayform);
        cmd.Parameters.AddWithValue("@DOCENTRY_PV", docEntryPv);

        return await cmd.ExecuteNonQueryAsync();
    }
}
