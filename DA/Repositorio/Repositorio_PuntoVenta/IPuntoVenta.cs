using BE.PuntoVenta;
using System.Data;

namespace DA.Repositorio.Repositorio_PuntoVenta;

public interface IPuntoVenta
{
    Task<List<ClienteBusquedaSapDTO>> BuscarCliente(string term);
    Task<List<ListaPrecioSapDTO>> BuscarListaPrecios(string? nombreBusqueda);
    Task<List<VendedorSapDTO>> BuscarVendedores(int codigoVendedorSap);
    Task<List<AlmacenSapDTO>> BuscarAlmacenes(string? nombreBusqueda);
    Task<List<StockPorAlmacenDTO>> BuscarStockPorAlmacen(StockPorAlmacenFiltroDTO filtro);
    Task<List<DireccionClienteSapDTO>> BuscarDireccionesCliente(string codigoCliente);
    Task<List<TipoEmbalajeSapDTO>> BuscarTiposEmbalaje();
    Task<List<LugarEntregaSapDTO>> BuscarLugaresEntrega();
    Task<List<HoraEntregaSapDTO>> BuscarHorasEntrega();
    Task<List<ModoEnvioSapDTO>> BuscarModosEnvio();
    Task<List<FormaPagoSapDTO>> BuscarFormasPago(string? condicionPago);
    Task<List<TipoComprobanteSapDTO>> BuscarTiposComprobante(string? nombreBusqueda);
    Task<List<NotaCreditoClienteSapDTO>> BuscarNotasCreditoCliente(string codigoCliente);
    Task<List<ClienteDesgloseCreditoSapDTO>> BuscarDesgloseCreditoCliente(string codigoCliente, int? docEntrySap = null);
    Task<bool> EsFormaPagoCredito(string? payForm);
    Task<(bool Valido, decimal Disponible, string? Mensaje)> ValidarLimiteCredito(string cardCode, string? payForm, decimal montoNeto, int? docEntrySap = null);
    Task<(bool Valido, string? Mensaje)> ValidarPreciosYPromociones(VentaGuardarRequestDTO request);
    Task<List<ArticuloAutocompleteDTO>> BuscarArticulosAutocomplete(string? textoBusqueda, int codigoListaPrecio, string codigoAlmacen);
    Task<List<ArticuloBusquedaSapDTO>> BuscarArticulosDescripcion(string? descripcion, string? codigo, string? laboratorio, string? principioActivo, int codigoListaPrecio, string codigoAlmacen, string? titularRs = null);
    Task<List<ArticuloBusquedaSapDTO>> BuscarArticulosPorCodigo(string codigoArticulo, int codigoListaPrecio, string codigoAlmacen);
    Task<ArticuloDetalleVentaDTO> BuscarDetalleArticuloVenta(string codigoArticulo, int codigoListaPrecio, string codigoAlmacen, string codigoCliente, int? codigoUmd = null);
    Task<Dictionary<string, ArticuloDetalleVentaDTO>> BuscarDetalleArticulosVentaBatch(List<ArticuloDetalleVentaItemRequestDTO> items, int codigoListaPrecio, string codigoAlmacen, string codigoCliente);
    Task<Dictionary<string, List<PromoArticuloSapDTO>>> BuscarPromosArticulosBatch(List<string> codigos, string codigoCliente, int codigoListaPrecio);
    Task<List<UnidadMedidaArticuloSapDTO>> BuscarUmdArticulo(string codigoArticulo);
    Task<List<PromoArticuloSapDTO>> BuscarPromoArticulo(string codigoArticulo, string codigoCliente, int codigoListaPrecio, int codigoUmd);
    Task<List<ArticuloPrecioDTO>> ObtenerPrecios(List<string> codigosArticulos, int codigoListaPrecio, string codigoAlmacen, string codigoCliente);
    Task<List<LoteDisponibleSapDTO>> BuscarLotesArticulo(string codigoArticulo, string codigoAlmacen);
    Task<Dictionary<string, List<LoteDisponibleSapDTO>>> BuscarLotesArticulosBatch(List<string> codigosArticulos, string codigoAlmacen);
    Task<VentaGuardarResponseDTO> GuardarVentaCompleta(VentaGuardarRequestDTO request);
    Task<List<VentaListaDTO>> ListarVentas(VentaBusquedaFiltroDTO filtro);
    Task<int?> ObtenerVendedorPorDocEntry(int docEntry, int docEntrySap = 0);
    Task<List<LogImportadorDTO>> ListarLogImportador(int docEntry);
    Task<VentaCargarResponseDTO?> CargarVenta(int docEntry);
    Task<VentaCargarResponseDTO?> VerVenta(int docEntry);
    Task<VentaCargarResponseDTO?> PrepararReaperturaVenta(int docEntry, string? docStatusEsperado = null);
    Task AnularVenta(int docEntry, bool puedeAnularEnviadoWms = true, string? docStatusEsperado = null);
    Task TrasladarVenta(int docEntry, string? docStatusEsperado = null);
    Task EnviarWMS(int docEntry, string? docStatusEsperado = null);
    Task MarcarImpreso(int docEntry);
    Task<DataTable?> ObtenerReporteTicket(int docEntrySap);
    Task<DataTable?> ObtenerReportePreliminarSap(int docEntrySap, int docEntryOwtr);
    Task<DataTable?> ObtenerReportePreliminarPv(int docEntry);
    Task<List<string>> ListarTitularesRs();
    Task<List<ListaPrecioClienteItemDTO>> ObtenerListaPreciosClienteAsync(string? itemCode = null);

    // Clientes Bloqueados
    Task<List<ClienteBloqueadoDTO>> BuscarClientesBloqueados(ClienteBloqueadoFiltroDTO filtro);
    Task<ClienteBloqueadoDTO?> ObtenerClienteBloqueado(int id);
    Task<int> InsertarClienteBloqueado(ClienteBloqueadoGuardarDTO dto);
    Task<int> ActualizarClienteBloqueado(ClienteBloqueadoGuardarDTO dto);
    Task<int> EliminarClienteBloqueado(int id, int usuarioModificacion);
    Task<ClienteBloqueadoDTO?> ValidarClienteBloqueado(string carcode);
    Task<List<ClienteBusquedaSapDTO>> ObtenerDetalleClientesSap(List<string> codigos);
    Task<List<ClienteBusquedaSapDTO>> ValidarRucsBatch(List<string> rucs);

    // Artículos Fraccionados
    Task<List<ArticuloFraccionadoDTO>> BuscarArticulosFraccionados(ArticuloFraccionadoFiltroDTO filtro);
    Task<ArticuloFraccionadoDTO?> ObtenerArticuloFraccionado(int id);
    Task<int> InsertarArticuloFraccionado(ArticuloFraccionadoGuardarDTO dto);
    Task<int> ActualizarArticuloFraccionado(ArticuloFraccionadoGuardarDTO dto);
    Task<int> EliminarArticuloFraccionado(int id, int usuarioModificacion);
    Task<int> EliminarTodosArticulosFraccionados(int usuarioModificacion);
    Task<ArticuloFraccionadoDTO?> ValidarArticuloFraccionado(string itemcode);
    Task<List<ArticuloBusquedaSapDTO>> ObtenerDetalleArticulosSap(List<string> codigos);
    Task<List<ArticuloBusquedaSapDTO>> ValidarItemcodesBatch(List<string> itemcodes);
    Task<List<ArticuloFraccionadoDTO>> ValidarArticulosFraccionadosBatch(List<string> itemcodes);

    // Modificar Condición de Pago
    Task<OrdenCondicionPagoSapDTO?> ObtenerOrdenCondicionPagoSap(int docEntrySap);
    Task<object> ActualizarCondicionPago_ServiceLayer(int docEntryPv, int docEntrySap, int groupNumAnterior, string? condicionAnterior, int groupNumNuevo, string condicionNuevo, string usuario, int? docNumSap = null, string? motivo = null);
    Task<int> ActualizarCondicionPagoPuntoVenta(int docEntryPv, int docEntrySap, int groupNumNuevo, string? condicionNuevo = null);
    Task<int> RegistrarHistorialCondicionPago(BE_HistorialCondicionPago historial);
    Task<List<BE_HistorialCondicionPago>> ListarHistorialCondicionPago(int docEntryPv, int docEntrySap);
}
