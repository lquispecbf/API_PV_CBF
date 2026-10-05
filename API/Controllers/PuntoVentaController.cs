using BE;
using BE.PuntoVenta;
using BE.Seguridad;
using ClosedXML.Excel;
using DA.Repositorio.Repositorio_Errores;
using DA.Repositorio.Repositorio_PuntoVenta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Serilog;
using API.Infrastructure.Filters;

namespace API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class PuntoVentaController : ControllerBase
    {
        private readonly IPuntoVenta _puntoVenta;
        private readonly IErrores _errores;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly BL.Servicios.IDigemidService _digemidService;
        private readonly string? _rutaCrystal_API_PV;
        private readonly string? _rutaCrystal_PDF;

        public PuntoVentaController(
            IErrores errores,
            IPuntoVenta puntoVenta,
            IConfiguration configuration,
            IWebHostEnvironment env,
            BL.Servicios.IDigemidService digemidService)
        {
            _errores = errores;
            _puntoVenta = puntoVenta;
            _configuration = configuration;
            _env = env;
            _digemidService = digemidService;
            _rutaCrystal_API_PV = configuration["RutaCrystal_API_PV"];
            _rutaCrystal_PDF = configuration["RutaCrystal_PDF"];
        }

        private string ObtenerUsuarioActual()
        {
            return User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("usuario") ?? "";
        }

        private string ObtenerIdUsuarioActual()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id_usuario") ?? "0";
        }

        private int ObtenerCodigoVendedorSap()
        {
            var val = User.FindFirstValue("id_vendedor");
            if (int.TryParse(val, out int id)) return id;
            return 0;
        }

        private async Task<bool> ValidarAccesoVendedor(int docEntry, int docEntrySap = 0)
        {
            int codigoVendedorSap = ObtenerCodigoVendedorSap();
            // Si es Supervisor / Administrador (codigoVendedorSap <= 0), tiene acceso irrestricto
            if (codigoVendedorSap <= 0) return true;

            // Si es Vendedor, consultamos el dueño de la orden
            int? slpCodeDoc = await _puntoVenta.ObtenerVendedorPorDocEntry(docEntry, docEntrySap);
            if (!slpCodeDoc.HasValue) return false;

            return slpCodeDoc.Value == codigoVendedorSap;
        }

        private bool EsUsuarioMantenerSesion()
        {
            return User.Claims.Any(c => c.Type == "pv_action" && c.Value == AccionesPvConstantes.VentaMantenerSesion);
        }

        private bool EsUsuarioAutorizadoAnularEnviadoWms()
        {
            return User.Claims.Any(c => c.Type == "pv_action" && c.Value == AccionesPvConstantes.VentaAnularEnviadoWms);
        }

        [HttpGet]
        public IActionResult KeepAlive()
        {
            var idUsuario = ObtenerIdUsuarioActual();
            if (string.IsNullOrEmpty(idUsuario) || idUsuario == "0")
            {
                return Unauthorized();
            }

            return Ok(new { success = true, keepAlive = EsUsuarioMantenerSesion() });
        }

        private static string? TruncarTexto(string? texto, int max)
        {
            if (string.IsNullOrEmpty(texto) || texto.Length <= max)
                return texto;
            return texto.Substring(0, max);
        }

        [HttpGet]
        public IActionResult VentaInfo()
        {
            return Ok(new
            {
                UsuarioSapCode = ObtenerCodigoVendedorSap(),
                MantenerSesionPuntoVenta = EsUsuarioMantenerSesion(),
                PuedeAnularEnviadoWms = EsUsuarioAutorizadoAnularEnviadoWms(),
                PuedeModificarCondicionPago = !string.IsNullOrEmpty(ObtenerRolCondicionPagoUsuario())
            });
        }

        [HttpGet]
        public IActionResult StockPorAlmacenInfo()
        {
            return Ok(new { MantenerSesionPuntoVenta = EsUsuarioMantenerSesion() });
        }

        [RequierePermisoModulo("PuntoVenta:StockPorAlmacen")]
        [RequiereAccionPv(AccionesPvConstantes.StockAlmacenVer)]
        [HttpPost]
        public async Task<IActionResult> Buscar_StockPorAlmacen([FromBody] StockPorAlmacenFiltroDTO filtro)
        {
            try
            {
                var resultado = await _puntoVenta.BuscarStockPorAlmacen(filtro);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al buscar stock por almacén: {ex.Message}" });
            }
        }

        [RequierePermisoModulo("PuntoVenta:StockPorAlmacen")]
        [RequiereAccionPv(AccionesPvConstantes.StockAlmacenExportar)]
        [HttpPost]
        public async Task<IActionResult> ExportarExcel_StockPorAlmacen([FromBody] StockPorAlmacenFiltroDTO filtro)
        {
            try
            {
                var resultados = await _puntoVenta.BuscarStockPorAlmacen(filtro);
                if (resultados == null || resultados.Count == 0)
                    return NotFound("No se encontraron datos para exportar.");

                var fileContent = GenerarExcelStockPorAlmacen(resultados);
                return File(
                    fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Reporte Stock por Almacen.xlsx"
                );
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, "Error al exportar el reporte.");
            }
        }

        private static byte[] GenerarExcelStockPorAlmacen(List<StockPorAlmacenDTO> stock)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Stock");

            ws.Cell(1, 1).Value = "CODIGO";
            ws.Cell(1, 2).Value = "DESCRIPCION";
            ws.Cell(1, 3).Value = "UMD";
            ws.Cell(1, 4).Value = "LABORATORIO";
            ws.Cell(1, 5).Value = "PRECIO";
            ws.Cell(1, 6).Value = "PRECIO CAJA";
            ws.Cell(1, 7).Value = "STOCK";
            ws.Cell(1, 8).Value = "FECHA VENCIMIENTO";
            ws.Cell(1, 9).Value = "PRINCIPIO ACTIVO";
            ws.Cell(1, 10).Value = "ESTADO SKU";
            ws.Cell(1, 11).Value = "OBSERVACION";

            var headerRange = ws.Range("A1:K1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int fila = 2;
            foreach (var s in stock)
            {
                ws.Cell(fila, 1).Value = s.CODIGO ?? "";
                ws.Cell(fila, 2).Value = s.DESCRIPCION ?? "";
                ws.Cell(fila, 3).Value = s.UMD ?? "";
                ws.Cell(fila, 4).Value = s.LABORATORIO ?? "";
                ws.Cell(fila, 5).Value = s.PRECIO;
                ws.Cell(fila, 5).Style.NumberFormat.Format = "0.0000";
                ws.Cell(fila, 6).Value = s.PRECIO_CAJA;
                ws.Cell(fila, 6).Style.NumberFormat.Format = "0.0000";
                ws.Cell(fila, 7).Value = s.STOCK;
                ws.Cell(fila, 8).Value = s.FECHA_VENCIMIENTO ?? "";
                ws.Cell(fila, 9).Value = s.PRINCIPIO_ACTIVO ?? "";
                ws.Cell(fila, 10).Value = s.ESTADO_SKU ?? "";
                ws.Cell(fila, 11).Value = s.OBSERVACION ?? "";
                fila++;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_Cliente(string criterioBusqueda)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(criterioBusqueda) || criterioBusqueda.Length < 3)
                    return Ok(new List<ClienteBusquedaSapDTO>());

                var lista = await _puntoVenta.BuscarCliente(criterioBusqueda);

                return Ok(lista.Take(30));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_ListaPrecios(string? nombreBusqueda)
        {
            try
            {
                var lista = await _puntoVenta.BuscarListaPrecios(nombreBusqueda);

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new { error = "Error al buscar listas de precio." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_Vendedores()
        {
            try
            {
                int codigoVendedorSap = ObtenerCodigoVendedorSap();
                var lista = await _puntoVenta.BuscarVendedores(codigoVendedorSap);
                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar vendedores: " + ex.Message
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_Almacenes(string? nombreBusqueda)
        {
            try
            {
                var lista = await _puntoVenta.BuscarAlmacenes(nombreBusqueda);

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar almacenes."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_DireccionesCliente(string codigoCliente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoCliente))
                    return Ok(new List<DireccionClienteSapDTO>());

                var lista = await _puntoVenta.BuscarDireccionesCliente(codigoCliente);

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar direcciones del cliente."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_TiposEmbalaje()
        {
            try
            {
                var lista = await _puntoVenta.BuscarTiposEmbalaje();

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar tipos de embalaje."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_LugaresEntrega()
        {
            try
            {
                var lista = await _puntoVenta.BuscarLugaresEntrega();

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar lugares de entrega."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_HorasEntrega()
        {
            try
            {
                var lista = await _puntoVenta.BuscarHorasEntrega();

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar horas de entrega."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_ModosEnvio()
        {
            try
            {
                var lista = await _puntoVenta.BuscarModosEnvio();

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar modos de envío."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_FormasPago(string? condicionPago)
        {
            try
            {
                var lista = await _puntoVenta.BuscarFormasPago(condicionPago);

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar formas de pago."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_TiposComprobante(string? nombreBusqueda)
        {
            try
            {
                var lista = await _puntoVenta.BuscarTiposComprobante(nombreBusqueda);
                var filtrados = lista.Where(x => x.CODIGO == "01" || x.CODIGO == "03").ToList();

                return Ok(filtrados);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar tipos de comprobante."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_NotasCreditoCliente(string codigoCliente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoCliente))
                    return Ok(new List<NotaCreditoClienteSapDTO>());

                var lista = await _puntoVenta.BuscarNotasCreditoCliente(codigoCliente);

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar notas de crédito del cliente."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_DesgloseCreditoCliente(string codigoCliente, int? docEntrySap = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoCliente))
                    return Ok(new List<ClienteDesgloseCreditoSapDTO>());

                var lista = await _puntoVenta.BuscarDesgloseCreditoCliente(codigoCliente, docEntrySap);

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar el desglose de crédito del cliente."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_ArticulosPorCodigo(string codigoArticulo, int codigoListaPrecio, string codigoAlmacen)
        {
            try
            {
                var lista = await _puntoVenta.BuscarArticulosPorCodigo(
                    TruncarTexto(codigoArticulo, 50),
                    codigoListaPrecio,
                    codigoAlmacen
                );

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar detalle del artículo."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_DetalleArticuloVenta(string codigoArticulo, int codigoListaPrecio, string codigoAlmacen, string? codigoCliente, int? codigoUmd)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoArticulo))
                    return Ok(new ArticuloDetalleVentaDTO());

                var detalle = await _puntoVenta.BuscarDetalleArticuloVenta(
                    TruncarTexto(codigoArticulo, 50),
                    codigoListaPrecio,
                    codigoAlmacen,
                    codigoCliente ?? "",
                    codigoUmd
                );

                return Ok(detalle);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar detalle optimizado del artículo."
                });
            }
        }
        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpPost]
        public async Task<IActionResult> Buscar_DetalleArticulosVentaBatch([FromBody] ArticuloDetalleVentaBatchRequestDTO request)
        {
            try
            {
                if (request is null || request.Items is null || request.Items.Count == 0)
                    return Ok(new Dictionary<string, ArticuloDetalleVentaDTO>());

                var dict = await _puntoVenta.BuscarDetalleArticulosVentaBatch(
                    request.Items,
                    request.CodigoListaPrecio,
                    request.CodigoAlmacen ?? "",
                    request.CodigoCliente ?? ""
                );

                return Ok(dict);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar detalle de artículos en lote."
                });
            }
        }
        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpPost]
        public async Task<IActionResult> Calcular_PreciosBatch([FromBody] PreciosBatchRequestDTO request)
        {
            try
            {
                if (request is null || request.Items is null || request.Items.Count == 0)
                    return Ok(new List<ArticuloPrecioDTO>());

                var lista = await _puntoVenta.ObtenerPrecios(
                    request.Items,
                    request.CodigoListaPrecio,
                    request.CodigoAlmacen ?? "",
                    request.CodigoCliente ?? ""
                );

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al calcular precios."
                });
            }
        }
        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_ArticulosAutocomplete(string? textoBusqueda, int codigoListaPrecio, string codigoAlmacen)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textoBusqueda))
                    return Ok(new List<ArticuloAutocompleteDTO>());

                var lista = await _puntoVenta.BuscarArticulosAutocomplete(
                    TruncarTexto(textoBusqueda, 200),
                    codigoListaPrecio,
                    codigoAlmacen
                );

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar artículos para autocomplete."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_ArticulosDescripcion(string? textoBusqueda, int codigoListaPrecio, string codigoAlmacen)
        {
            try
            {
                var lista = await _puntoVenta.BuscarArticulosDescripcion(
                    TruncarTexto(textoBusqueda, 200),
                    null,
                    null,
                    null,
                    codigoListaPrecio,
                    codigoAlmacen
                );

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar artículos."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta", "PuntoVenta:ArticuloFraccionado")]
        [HttpGet]
        public async Task<IActionResult> Buscar_UmdArticulo(string codigoArticulo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoArticulo))
                    return Ok(new List<UnidadMedidaArticuloSapDTO>());

                var lista = await _puntoVenta.BuscarUmdArticulo(codigoArticulo);

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar unidades de medida."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_PromoArticulo(string codigoArticulo, string codigoCliente, int codigoListaPrecio, int codigoUmd)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoArticulo))
                    return Ok(new List<PromoArticuloSapDTO>());

                var lista = await _puntoVenta.BuscarPromoArticulo(
                    codigoArticulo,
                    codigoCliente ?? "",
                    codigoListaPrecio,
                    codigoUmd
                );

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al consultar promociones."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_LotesArticulo(string codigoArticulo, string codigoAlmacen)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoArticulo))
                    return Ok(new List<LoteDisponibleSapDTO>());

                var lista = await _puntoVenta.BuscarLotesArticulo(codigoArticulo, codigoAlmacen);

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar lotes del artículo."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpPost]
        public async Task<IActionResult> Buscar_LotesArticulosBatch([FromBody] LotesBatchRequestDTO request)
        {
            try
            {
                if (request is null || request.Items is null || request.Items.Count == 0)
                    return Ok(new Dictionary<string, List<LoteDisponibleSapDTO>>());

                var dict = await _puntoVenta.BuscarLotesArticulosBatch(request.Items, request.CodigoAlmacen ?? "");

                return Ok(dict);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar lotes de artículos en lote."
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_ArticulosAvanzado(
            string? descripcion,
            string? codigo,
            string? laboratorio,
            string? principioActivo,
            string? titularRs,
            int codigoListaPrecio,
            string codigoAlmacen,
            string? textoBusqueda = null,
            string? rubro = null)
        {
            try
            {
                var busquedaDesc = !string.IsNullOrWhiteSpace(descripcion) ? descripcion : textoBusqueda;
                if (string.IsNullOrWhiteSpace(busquedaDesc)
                    && string.IsNullOrWhiteSpace(codigo)
                    && string.IsNullOrWhiteSpace(laboratorio)
                    && string.IsNullOrWhiteSpace(principioActivo)
                    && string.IsNullOrWhiteSpace(titularRs)
                    && string.IsNullOrWhiteSpace(rubro))
                    return Ok(new List<ArticuloBusquedaSapDTO>());

                var lista = await _puntoVenta.BuscarArticulosDescripcion(
                    TruncarTexto(busquedaDesc, 200),
                    TruncarTexto(codigo, 200),
                    TruncarTexto(laboratorio, 200),
                    TruncarTexto(principioActivo, 200),
                    codigoListaPrecio,
                    codigoAlmacen,
                    TruncarTexto(titularRs, 200)
                );

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return StatusCode(500, new
                {
                    error = "Error al buscar artículos: " + ex.Message
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpGet]
        public async Task<IActionResult> Buscar_TitularesRs()
        {
            try
            {
                var lista = await _puntoVenta.ListarTitularesRs();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al buscar titulares RS." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [RequiereAccionPv(AccionesPvConstantes.VentaCrear, AccionesPvConstantes.VentaGuardarBorrador)]
        [HttpPost]
        public async Task<IActionResult> Guardar_Venta([FromBody] VentaGuardarRequestDTO request)
        {
            try
            {
                if (request is null)
                {
                    var errors = string.Join(" | ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                    string rawPreview = "(no disponible)";
                    try
                    {
                        Request.Body.Position = 0;
                        using var reader = new StreamReader(Request.Body, leaveOpen: true);
                        var body = await reader.ReadToEndAsync();
                        rawPreview = body.Length > 500 ? body.Substring(0, 500) + "..." : body;
                    }
                    catch { rawPreview = $"(no se pudo re-leer: ContentLength={Request.ContentLength}, ContentType={Request.ContentType})"; }

                    return StatusCode(400, new
                    {
                        error = "El cuerpo de la solicitud no se pudo deserializar.",
                        modelStateErrors = errors,
                        diagnostic = new
                        {
                            contentType = Request.ContentType?.ToString(),
                            contentLength = Request.ContentLength,
                            rawPreview
                        }
                    });
                }

                var esBorrador = (request.DOCSTATUS ?? "").Trim().ToUpper() == "E";
                if (esBorrador)
                {
                    if (!User.Claims.Any(c => c.Type == "pv_action" && (c.Value == AccionesPvConstantes.VentaGuardarBorrador || c.Value == AccionesPvConstantes.VentaCrear)))
                    {
                        return StatusCode(403, new { error = "No cuenta con permisos para guardar borradores de venta." });
                    }
                }
                else
                {
                    if (!User.Claims.Any(c => c.Type == "pv_action" && c.Value == AccionesPvConstantes.VentaCrear))
                    {
                        return StatusCode(403, new { error = "No cuenta con permisos para emitir y registrar ventas." });
                    }
                }

                int codigoVendedorSap = ObtenerCodigoVendedorSap();
                if (codigoVendedorSap > 0)
                {
                    request.SLPCODE = codigoVendedorSap;

                    if (request.DOCENTRY > 0)
                    {
                        if (!await ValidarAccesoVendedor(request.DOCENTRY))
                        {
                            return StatusCode(403, new { error = "No tiene autorización para modificar esta orden de venta." });
                        }
                    }
                }

                var clienteBloqueado = await _puntoVenta.ValidarClienteBloqueado(request.CARDCODE);
                if (clienteBloqueado != null)
                {
                    return BadRequest(new
                    {
                        error = "El cliente \"" + request.CARDCODE + "\" se encuentra bloqueado. Motivo: " + (clienteBloqueado.MOTIVO_BLOQUEO ?? "")
                    });
                }

                // Validación obligatoria de Línea de Crédito en Servidor para órdenes definitivas
                if (request.DOCSTATUS != "E")
                {
                    var validacionCredito = await _puntoVenta.ValidarLimiteCredito(
                        request.CARDCODE ?? "",
                        request.PAYFORM,
                        request.IMP_NET,
                        request.DOCENTRY_SAP.HasValue && request.DOCENTRY_SAP.Value > 0 ? request.DOCENTRY_SAP.Value : (int?)null
                    );

                    if (!validacionCredito.Valido)
                    {
                        return BadRequest(new
                        {
                            error = validacionCredito.Mensaje
                        });
                    }
                }

                var lugarEntrega = (request.DELIVERY_POINT ?? "").Trim().ToUpperInvariant();
                var payForm = (request.PAYFORM ?? "").Trim().ToUpperInvariant();

                // Validación Comercial: Si el lugar de entrega es AGENCIA, la forma de pago NO puede ser CONTRA ENTREGA
                if (lugarEntrega == "AGENCIA" && (payForm.Contains("CONTRA ENTREGA") || payForm.Contains("CONTRAENTREGA")))
                {
                    return BadRequest(new
                    {
                        error = "Incompatibilidad comercial: Cuando el Lugar de Entrega es AGENCIA, no se permite la forma de pago CONTRA ENTREGA. Por favor seleccione otra condición de pago o cambie el lugar de entrega."
                    });
                }

                var lugaresRestringidos = new[] { "AGENCIA", "CENTRO", "DOMICILIO" };
                int horaEntrega = ObtenerHoraMilitar(request.DELIVERY_TIME);

                if (lugaresRestringidos.Contains(lugarEntrega))
                {
                    if (horaEntrega != 8 && horaEntrega != 13)
                    {
                        return BadRequest(new
                        {
                            error = "Para el lugar de entrega seleccionado (Agencia, Centro o Domicilio), solo se permiten las horas de entrega de 8:00 a.m. y 1:00 p.m."
                        });
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.DOCDATE) && !string.IsNullOrWhiteSpace(request.DELIVERY_DATE))
                {
                    var formatosFecha = new[] { "dd/MM/yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "d/M/yyyy" };
                    bool atencionOk = DateTime.TryParseExact(request.DOCDATE.Trim(), formatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtAtencion)
                        || DateTime.TryParse(request.DOCDATE.Trim(), out dtAtencion);
                    bool entregaOk = DateTime.TryParseExact(request.DELIVERY_DATE.Trim(), formatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtEntrega)
                        || DateTime.TryParse(request.DELIVERY_DATE.Trim(), out dtEntrega);

                    if (atencionOk && entregaOk && dtEntrega.Date < dtAtencion.Date)
                    {
                        return BadRequest(new
                        {
                            error = "La fecha de entrega no puede ser menor a la fecha de atención."
                        });
                    }

                    if (entregaOk && dtEntrega.Date == DateTime.Today)
                    {
                        if (horaEntrega != -1 && horaEntrega <= DateTime.Now.Hour)
                        {
                            return BadRequest(new
                            {
                                error = "La hora de entrega seleccionada ya no se encuentra disponible para la fecha de hoy."
                            });
                        }
                    }
                }

                var erroresFraccionados = new List<string>();
                var itemcodesFraccion = request.DETALLE
                    .Where(d => d.QUANTITY > 0)
                    .Select(d => d.ITEMCODE ?? "")
                    .Distinct()
                    .ToList();

                var mapaFracc = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (itemcodesFraccion.Count > 0)
                {
                    var fraccionados = await _puntoVenta.ValidarArticulosFraccionadosBatch(itemcodesFraccion);
                    mapaFracc = fraccionados.ToDictionary(
                        x => x.ITEMCODE ?? "", x => x.FRACCIONADO, StringComparer.OrdinalIgnoreCase);
                }

                foreach (var d in request.DETALLE)
                {
                    if (d.QUANTITY <= 0) continue;

                    var umdNombre = (d.UMD_NOMBRE ?? "").Trim().ToUpperInvariant();
                    var esPza = umdNombre == "PZA" || umdNombre == "PIEZA";
                    if (!esPza) continue;

                    if (!mapaFracc.TryGetValue(d.ITEMCODE ?? "", out var multiplo))
                    {
                        erroresFraccionados.Add(
                            $"El artículo \"{d.ITEMCODE}\" no permite venta fraccionada y su UMD seleccionada es {d.UMD_NOMBRE}.");
                        continue;
                    }

                    if (d.QUANTITY % multiplo != 0)
                    {
                        erroresFraccionados.Add(
                            $"El artículo \"{d.ITEMCODE}\" cantidad {d.QUANTITY} no es múltiplo de {multiplo}.");
                    }
                }

                if (erroresFraccionados.Count > 0)
                {
                    return BadRequest(new { error = string.Join(" ", erroresFraccionados) });
                }

                // Validación obligatoria de Precios y Promociones en Servidor contra SAP HANA
                if (request.DOCSTATUS != "E")
                {
                    var validacionPrecios = await _puntoVenta.ValidarPreciosYPromociones(request);
                    if (!validacionPrecios.Valido)
                    {
                        return BadRequest(new
                        {
                            error = validacionPrecios.Mensaje
                        });
                    }
                }

                // Validación de Productos Controlados según Dirección de Envío (OWASP A04 - Server Side Validation)
                // Se aplica cuando no es guardado en borrador (DOCSTATUS != "E")
                if (!string.Equals(request.DOCSTATUS, "E", StringComparison.OrdinalIgnoreCase))
                {
                    var itemcodesActivos = request.DETALLE
                        .Where(d => d.QUANTITY > 0 && !string.IsNullOrWhiteSpace(d.ITEMCODE))
                        .Select(d => d.ITEMCODE!.Trim().ToUpperInvariant())
                        .Distinct()
                        .ToList();

                    if (itemcodesActivos.Count > 0 && !string.IsNullOrWhiteSpace(request.CARDCODE))
                    {
                        int codListaPrecio = 1;
                        if (!string.IsNullOrWhiteSpace(request.PRICE_LIST) && int.TryParse(request.PRICE_LIST, out var parsedLp))
                        {
                            codListaPrecio = parsedLp;
                        }

                        var itemsBatchReq = itemcodesActivos.Select(c => new ArticuloDetalleVentaItemRequestDTO
                        {
                            CodigoArticulo = c,
                            CodigoUmd = null
                        }).ToList();

                        var detalleArticulosMap = await _puntoVenta.BuscarDetalleArticulosVentaBatch(
                            itemsBatchReq,
                            codListaPrecio,
                            request.WHSCODE ?? "",
                            request.CARDCODE ?? ""
                        );

                        var direccionesCliente = await _puntoVenta.BuscarDireccionesCliente(request.CARDCODE);
                        var dirEnvio = direccionesCliente.FirstOrDefault(d =>
                            string.Equals(d.CODIGO_DIRECCION, request.DELIVERY_PLACE, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(d.TIPO_DIRECCION, "S", StringComparison.OrdinalIgnoreCase))
                            ?? direccionesCliente.FirstOrDefault(d =>
                            string.Equals(d.CODIGO_DIRECCION, request.DELIVERY_PLACE, StringComparison.OrdinalIgnoreCase));

                        var erroresControlados = new List<string>();

                        foreach (var det in request.DETALLE.Where(d => d.QUANTITY > 0 && !string.IsNullOrWhiteSpace(d.ITEMCODE)))
                        {
                            var codeUpper = det.ITEMCODE!.Trim().ToUpperInvariant();
                            if (detalleArticulosMap.TryGetValue(codeUpper, out var artInfo) && artInfo?.ARTICULO != null)
                            {
                                var tipo = (artInfo.ARTICULO.TIPO_CONTROLADO ?? "01").Trim();
                                if (tipo != "01" && tipo != "1")
                                {
                                    bool permitido = false;
                                    string tipoNombre = "Controlado";
                                    if (tipo == "02" || tipo == "2")
                                    {
                                        tipoNombre = "Precursores";
                                        permitido = dirEnvio != null && string.Equals(dirEnvio.U_CBF_PREC, "SI", StringComparison.OrdinalIgnoreCase);
                                    }
                                    else if (tipo == "03" || tipo == "3")
                                    {
                                        tipoNombre = "Psicotrópicos";
                                        permitido = dirEnvio != null && string.Equals(dirEnvio.U_CBF_PSI, "SI", StringComparison.OrdinalIgnoreCase);
                                    }
                                    else if (tipo == "04" || tipo == "4")
                                    {
                                        tipoNombre = "Estupefacientes";
                                        permitido = dirEnvio != null && string.Equals(dirEnvio.U_CBF_ESTU, "SI", StringComparison.OrdinalIgnoreCase);
                                    }
                                    else if (tipo == "05" || tipo == "5")
                                    {
                                        tipoNombre = "Psicotrópicos IV B";
                                        permitido = dirEnvio != null && string.Equals(dirEnvio.U_CBF_PSI_IV, "SI", StringComparison.OrdinalIgnoreCase);
                                    }

                                    if (!permitido)
                                    {
                                        var desc = !string.IsNullOrWhiteSpace(det.ITEMNAME) ? det.ITEMNAME : (artInfo.ARTICULO.DESCRIPCION ?? det.ITEMCODE);
                                        erroresControlados.Add($"\"{det.ITEMCODE} - {desc}\" ({tipoNombre})");
                                    }
                                }
                            }
                        }

                        if (erroresControlados.Count > 0)
                        {
                            var nombreDir = !string.IsNullOrWhiteSpace(request.DELIVERY_PLACE) ? request.DELIVERY_PLACE : "(Sin dirección seleccionada)";
                            return BadRequest(new
                            {
                                error = $"No se puede guardar la venta porque la dirección de envío \"{nombreDir}\" no está autorizada para comercializar los siguientes productos controlados: {string.Join(", ", erroresControlados.Distinct())}."
                            });
                        }
                    }
                }

                var resultado = await _puntoVenta.GuardarVentaCompleta(request);

                if (resultado.EXITO)
                    return Ok(resultado);

                return BadRequest(new { error = resultado.MENSAJE });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);

                return BadRequest(new
                {
                    error = $"Error al guardar la venta: {ex.Message}"
                });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [RequiereAccionPv(AccionesPvConstantes.VentaExportarExcel)]
        [HttpPost]
        public async Task<IActionResult> ExportarExcel_Ventas([FromBody] VentaBusquedaFiltroDTO filtro)
        {
            try
            {
                int codigoVendedorSap = ObtenerCodigoVendedorSap();
                if (codigoVendedorSap > 0)
                {
                    filtro.VENDEDOR = codigoVendedorSap.ToString();
                }

                // Validación de rango máximo de fechas para proteger recursos del servidor (OWASP A01 / A04)
                if (!string.IsNullOrWhiteSpace(filtro.FECHA_INICIO) && !string.IsNullOrWhiteSpace(filtro.FECHA_FIN))
                {
                    var formatos = new[] { "dd/MM/yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "d/M/yyyy" };
                    if ((DateTime.TryParseExact(filtro.FECHA_INICIO.Trim(), formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtInicio) || DateTime.TryParse(filtro.FECHA_INICIO.Trim(), out dtInicio)) &&
                        (DateTime.TryParseExact(filtro.FECHA_FIN.Trim(), formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtFin) || DateTime.TryParse(filtro.FECHA_FIN.Trim(), out dtFin)))
                    {
                        int maxDias = codigoVendedorSap > 0 ? 90 : 180;
                        if ((dtFin.Date - dtInicio.Date).TotalDays > maxDias)
                        {
                            return BadRequest(new
                            {
                                error = $"El rango de fechas no puede superar los {maxDias} días para la exportación de reportes."
                            });
                        }
                    }
                }

                var resultados = await _puntoVenta.ListarVentas(filtro);
                if (resultados == null || resultados.Count == 0)
                    return NotFound("No se encontraron datos para exportar.");

                var fileContent = GenerarExcelVentas(resultados);
                return File(
                    fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Reporte Ventas.xlsx"
                );
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, "Error al exportar el reporte.");
            }
        }

        private static byte[] GenerarExcelVentas(List<VentaListaDTO> ventas)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Ventas");

            ws.Cell(1, 1).Value = "CLIENTE";
            ws.Cell(1, 2).Value = "RUC/DNI";
            ws.Cell(1, 3).Value = "FECHA";
            ws.Cell(1, 4).Value = "VENDEDOR";
            ws.Cell(1, 5).Value = "ALMACEN";
            ws.Cell(1, 6).Value = "COMENTARIO";
            ws.Cell(1, 7).Value = "LUGAR ENTREGA";
            ws.Cell(1, 8).Value = "TOTAL";
            ws.Cell(1, 9).Value = "NROSAP";
            ws.Cell(1, 10).Value = "ESTADO ENVIO";

            var headerRange = ws.Range("A1:J1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int fila = 2;
            foreach (var v in ventas)
            {
                ws.Cell(fila, 1).Value = v.CLIENTE ?? "";
                ws.Cell(fila, 2).Value = v.RUC_DNI ?? "";
                ws.Cell(fila, 3).Value = v.FECHA?.Length >= 10 ? v.FECHA[..10] : (v.FECHA ?? "");
                ws.Cell(fila, 4).Value = v.VENDEDOR ?? "";
                ws.Cell(fila, 5).Value = v.ALMACEN ?? "";
                ws.Cell(fila, 6).Value = v.COMENTARIO ?? "";
                ws.Cell(fila, 7).Value = v.LUGAR_ENTREGA ?? "";
                ws.Cell(fila, 8).Value = v.TOTAL;
                ws.Cell(fila, 9).Value = v.NROSAP ?? "";
                ws.Cell(fila, 10).Value = v.ESTADO_ENVIO ?? "";
                fila++;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [RequiereAccionPv(AccionesPvConstantes.VentaVer)]
        [HttpPost]
        public async Task<IActionResult> Buscar_Ventas([FromBody] VentaBusquedaFiltroDTO filtro)
        {
            try
            {
                int codigoVendedorSap = ObtenerCodigoVendedorSap();
                if (codigoVendedorSap > 0)
                {
                    filtro.VENDEDOR = codigoVendedorSap.ToString();
                }

                var resultado = await _puntoVenta.ListarVentas(filtro);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al buscar ventas: {ex.Message}" });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpPost]
        public async Task<IActionResult> Buscar_LogImportador([FromBody] CargarVentaRequestDTO data)
        {
            try
            {
                if (data == null || data.DocEntry <= 0)
                    return BadRequest(new { error = "El DocEntry es requerido." });

                if (!await ValidarAccesoVendedor(data.DocEntry))
                    return StatusCode(403, new { error = "No tiene autorización para consultar los logs de esta orden de venta." });

                var lista = await _puntoVenta.ListarLogImportador(data.DocEntry);
                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al consultar el log del importador: {ex.Message}" });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpPost]
        public async Task<IActionResult> Cargar_Venta([FromBody] CargarVentaRequestDTO data)
        {
            try
            {
                if (data == null || data.DocEntry <= 0)
                    return BadRequest(new { error = "El DocEntry es requerido." });

                if (!await ValidarAccesoVendedor(data.DocEntry))
                    return StatusCode(403, new { error = "No tiene autorización para consultar o editar esta orden de venta." });

                var resultado = await _puntoVenta.CargarVenta(data.DocEntry);
                if (resultado is null)
                    return NotFound(new { error = "Venta no encontrada." });
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al cargar venta: {ex.Message}" });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [HttpPost]
        public async Task<IActionResult> Ver_Venta([FromBody] CargarVentaRequestDTO data)
        {
            try
            {
                if (data == null || data.DocEntry <= 0)
                    return BadRequest(new { error = "El DocEntry es requerido." });

                if (!await ValidarAccesoVendedor(data.DocEntry))
                    return StatusCode(403, new { error = "No tiene autorización para ver esta orden de venta." });

                var resultado = await _puntoVenta.VerVenta(data.DocEntry);
                if (resultado is null)
                    return NotFound(new { error = "Venta no encontrada." });
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al cargar venta: {ex.Message}" });
            }
        }

        private string? BuscarRutaFisicaImagen(string? codigoArticulo)
        {
            if (string.IsNullOrWhiteSpace(codigoArticulo)) return null;

            // Si vino con query string residual (ej. ?t=...), limpiar
            int idxQuery = codigoArticulo.IndexOfAny(new[] { '?', '&' });
            if (idxQuery >= 0)
            {
                codigoArticulo = codigoArticulo.Substring(0, idxQuery);
            }

            // Sanitizar nombre de archivo para evitar Directory/Path Traversal
            string nombreLimpio = Path.GetFileName(codigoArticulo.Trim());
            if (string.IsNullOrWhiteSpace(nombreLimpio)) return null;

            // Colección de rutas base candidatas a inspeccionar
            var rutasCandidatas = new List<string>();

            string? rutaConfig = _configuration["RutaImagenesCatalogo"];
            if (!string.IsNullOrWhiteSpace(rutaConfig))
            {
                rutasCandidatas.Add(rutaConfig);
                if (!Path.IsPathRooted(rutaConfig))
                {
                    if (!string.IsNullOrWhiteSpace(_env.WebRootPath))
                        rutasCandidatas.Add(Path.Combine(_env.WebRootPath, rutaConfig.TrimStart('~', '/', '\\')));
                    if (!string.IsNullOrWhiteSpace(_env.ContentRootPath))
                        rutasCandidatas.Add(Path.Combine(_env.ContentRootPath, rutaConfig.TrimStart('~', '/', '\\')));
                }
            }

            if (!string.IsNullOrWhiteSpace(_env.WebRootPath))
            {
                rutasCandidatas.Add(Path.Combine(_env.WebRootPath, "img", "Img_Catalogo_Articulos"));
                rutasCandidatas.Add(Path.Combine(_env.WebRootPath, "Catalogo", "Articulos"));
            }

            if (!string.IsNullOrWhiteSpace(_env.ContentRootPath))
            {
                rutasCandidatas.Add(Path.Combine(_env.ContentRootPath, "wwwroot", "img", "Img_Catalogo_Articulos"));
                rutasCandidatas.Add(Path.Combine(_env.ContentRootPath, "wwwroot", "Catalogo", "Articulos"));
            }

            string[] extensiones = { "", ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".JPG", ".JPEG", ".PNG", ".WEBP", ".BMP", ".GIF" };

            foreach (var rutaBase in rutasCandidatas.Distinct())
            {
                if (string.IsNullOrWhiteSpace(rutaBase) || !Directory.Exists(rutaBase)) continue;

                foreach (var ext in extensiones)
                {
                    string rutaCompleta = Path.Combine(rutaBase, nombreLimpio + ext);
                    if (System.IO.File.Exists(rutaCompleta))
                    {
                        Log.Information("[BuscarRutaFisicaImagen] Archivo encontrado: {Ruta}", rutaCompleta);
                        return rutaCompleta;
                    }
                }
            }

            Log.Warning("[BuscarRutaFisicaImagen] No se encontró imagen para el artículo {Codigo} en ninguna de las {Count} rutas examinadas.", nombreLimpio, rutasCandidatas.Count);
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Obtener_ImagenArticulo(string codigoArticulo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoArticulo))
                    return Ok(new { imgRuta = "" });

                string? rutaFisica = BuscarRutaFisicaImagen(codigoArticulo);
                if (string.IsNullOrEmpty(rutaFisica))
                {
                    return Ok(new { imgRuta = "" });
                }

                // Servir vía endpoint seguro directamente desde la ruta física configurada en appsettings
                string urlVirtual = $"/PuntoVenta/Ver_ImagenArticulo?codigoArticulo={Uri.EscapeDataString(codigoArticulo.Trim())}";
                return Ok(new { imgRuta = urlVirtual });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Obtener_ImagenArticulo] Error al resolver imagen para {Codigo}", codigoArticulo);
                await _errores.Insertar_Exception(ex);
                return Ok(new { imgRuta = "" });
            }
        }

        [HttpGet]
        public IActionResult Ver_ImagenArticulo(string codigoArticulo)
        {
            try
            {
                string? rutaFisica = BuscarRutaFisicaImagen(codigoArticulo);
                if (string.IsNullOrEmpty(rutaFisica) || !System.IO.File.Exists(rutaFisica))
                {
                    return NotFound();
                }

                string ext = Path.GetExtension(rutaFisica).ToLowerInvariant();
                string contentType = ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    ".gif" => "image/gif",
                    ".bmp" => "image/bmp",
                    _ => "application/octet-stream"
                };

                return PhysicalFile(rutaFisica, contentType);
            }
            catch
            {
                return NotFound();
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [RequiereAccionPv(AccionesPvConstantes.VentaAnular)]
        [HttpPost]
        public async Task<IActionResult> Anular_Venta([FromBody] CargarVentaRequestDTO data)
        {
            try
            {
                if (data == null || data.DocEntry <= 0)
                {
                    return BadRequest(new { error = "El número de documento no es válido." });
                }

                if (!await ValidarAccesoVendedor(data.DocEntry))
                    return StatusCode(403, new { error = "No tiene autorización para anular esta orden de venta." });

                bool puedeAnularWms = EsUsuarioAutorizadoAnularEnviadoWms();
                await _puntoVenta.AnularVenta(data.DocEntry, puedeAnularWms, data.DOCSTATUS);
                return Ok(new { success = true, message = "Documento anulado correctamente." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al anular la venta: {ex.Message}" });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [RequiereAccionPv(AccionesPvConstantes.VentaCrear)]
        [RequiereAccionPv(AccionesPvConstantes.VentaReabrir)]
        [HttpPost]
        public async Task<IActionResult> Reabrir_Venta([FromBody] ReabrirVentaRequestDTO data)
        {
            try
            {
                if (data == null || data.DocEntry <= 0)
                    return BadRequest(new { error = "El DocEntry es requerido." });

                if (!await ValidarAccesoVendedor(data.DocEntry))
                    return StatusCode(403, new { error = "No tiene autorización para reabrir esta orden de venta." });

                var resultado = await _puntoVenta.PrepararReaperturaVenta(data.DocEntry, data.DOCSTATUS);
                if (resultado is null)
                    return NotFound(new { error = "Venta no encontrada." });

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al reabrir la venta: {ex.Message}" });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [RequiereAccionPv(AccionesPvConstantes.VentaCrear)]
        [HttpPost]
        public async Task<IActionResult> Trasladar_Venta([FromBody] CargarVentaRequestDTO data)
        {
            try
            {
                if (data == null || data.DocEntry <= 0)
                    return BadRequest(new { error = "El DocEntry es requerido." });

                if (!await ValidarAccesoVendedor(data.DocEntry))
                    return StatusCode(403, new { error = "No tiene autorización para trasladar esta orden de venta." });

                await _puntoVenta.TrasladarVenta(data.DocEntry, data.DOCSTATUS);
                return Ok(new { success = true, message = "BORRADOR TRASLADADO CORRECTAMENTE" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al trasladar la venta: {ex.Message}" });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [RequiereAccionPv(AccionesPvConstantes.VentaEnviarWms)]
        [HttpPost]
        public async Task<IActionResult> EnviarWMS_Venta([FromBody] CargarVentaRequestDTO data)
        {
            try
            {
                if (data == null || data.DocEntry <= 0)
                    return BadRequest(new { error = "El DocEntry es requerido." });

                if (!await ValidarAccesoVendedor(data.DocEntry))
                    return StatusCode(403, new { error = "No tiene autorización para enviar a WMS esta orden de venta." });

                await _puntoVenta.EnviarWMS(data.DocEntry, data.DOCSTATUS);
                return Ok(new { success = true, message = "DOCUMENTO ENVIADO A WMS CORRECTAMENTE" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al enviar a WMS la venta: {ex.Message}" });
            }
        }

        private static string ObtenerNumeroSapDeTabla(DataTable? tabla, int fallbackDocEntry)
        {
            if (tabla != null && tabla.Rows.Count > 0)
            {
                foreach (DataColumn col in tabla.Columns)
                {
                    if (col.ColumnName.Equals("DocNum", StringComparison.OrdinalIgnoreCase) ||
                        col.ColumnName.Equals("DOCNUM", StringComparison.OrdinalIgnoreCase) ||
                        col.ColumnName.Equals("DOCNUM_SAP", StringComparison.OrdinalIgnoreCase) ||
                        col.ColumnName.Equals("NROSAP", StringComparison.OrdinalIgnoreCase) ||
                        col.ColumnName.Equals("NRO_SAP", StringComparison.OrdinalIgnoreCase) ||
                        col.ColumnName.Equals("DocNumSap", StringComparison.OrdinalIgnoreCase))
                    {
                        var val = tabla.Rows[0][col]?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(val))
                            return val;
                    }
                }
            }
            return fallbackDocEntry > 0 ? fallbackDocEntry.ToString() : "0";
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [RequiereAccionPv(AccionesPvConstantes.VentaImprimir)]
        [HttpGet]
        public async Task<IActionResult> Imprimir_Venta([FromQuery] VentaReporteParamDTO dto)
        {
            try
            {
                if (dto.DocEntrySap <= 0)
                    return BadRequest(new { error = "REFRESQUE LA PANTALLA PARA TRAER EL NRO SAP" });

                if (!await ValidarAccesoVendedor(dto.DocEntry, dto.DocEntrySap))
                    return StatusCode(403, new { error = "No tiene autorización para imprimir esta orden de venta." });

                var tabla = await _puntoVenta.ObtenerReportePreliminarSap(dto.DocEntrySap, dto.DocEntryOwtr);

                if (tabla is null)
                    return NotFound(new { error = "No se encontró la venta para generar el reporte." });

                var sb = new StringBuilder();

                using (var sw = new StringWriter(sb))
                {
                    tabla.WriteXml(sw, XmlWriteMode.WriteSchema);
                }

                var reportPath = Path.Combine(_rutaCrystal_PDF, "RPT_ORDEN_V.rpt");

                var requestBody = JsonConvert.SerializeObject(new
                {
                    reportPath,
                    dto.Whs,
                    xml = sb.ToString()
                });

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(90);

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    _rutaCrystal_API_PV)
                {
                    Content = new StringContent(
                        requestBody,
                        Encoding.UTF8,
                        "application/json")
                };

                using var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode(500, new { error = $"Error al generar el reporte: {error}" });
                }

                var pdf = await response.Content.ReadAsByteArrayAsync();

                if (pdf.Length == 0)
                    return StatusCode(500, new { error = "No se obtuvo PDF del reporte." });

                await _puntoVenta.MarcarImpreso(dto.DocEntry);

                var docNum = !string.IsNullOrWhiteSpace(dto.NroSap) ? dto.NroSap.Trim() : ObtenerNumeroSapDeTabla(tabla, dto.DocEntrySap);
                var nombreArchivo = $"Impreso_{docNum}.pdf";

                Response.Headers["Content-Disposition"] = $"inline; filename=\"{nombreArchivo}\"";
                Response.Headers["Access-Control-Expose-Headers"] = "Content-Disposition";
                return File(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al generar el reporte: {ex.Message}" });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [RequiereAccionPv(AccionesPvConstantes.VentaImprimir)]
        [HttpGet]
        public async Task<IActionResult> Ticket_Venta([FromQuery] VentaReporteParamDTO dto)
        {
            try
            {
                if (dto.DocEntrySap <= 0)
                    return BadRequest(new { error = "DocEntry inválido." });

                if (!await ValidarAccesoVendedor(dto.DocEntry, dto.DocEntrySap))
                    return StatusCode(403, new { error = "No tiene autorización para generar el ticket de esta orden de venta." });

                var tabla = await _puntoVenta.ObtenerReporteTicket(dto.DocEntrySap);

                if (tabla is null)
                    return NotFound(new { error = "No se encontró la venta para generar el ticket." });

                var sb = new StringBuilder();

                using (var sw = new StringWriter(sb))
                {
                    tabla.WriteXml(sw, XmlWriteMode.WriteSchema);
                }

                var reportPath = Path.Combine(_rutaCrystal_PDF, "RPT_ORDEN_T.rpt");

                var requestBody = JsonConvert.SerializeObject(new
                {
                    reportPath,
                    whs = (string?)null,
                    xml = sb.ToString()
                });

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(90);

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    _rutaCrystal_API_PV)
                {
                    Content = new StringContent(
                        requestBody,
                        Encoding.UTF8,
                        "application/json")
                };

                using var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode(500, new { error = $"Error al generar el ticket: {error}" });
                }

                var pdf = await response.Content.ReadAsByteArrayAsync();

                if (pdf.Length == 0)
                    return StatusCode(500, new { error = "No se obtuvo PDF del ticket." });

                var docNum = !string.IsNullOrWhiteSpace(dto.NroSap) ? dto.NroSap.Trim() : ObtenerNumeroSapDeTabla(tabla, dto.DocEntrySap);
                var nombreArchivo = $"Ticket_{docNum}.pdf";

                Response.Headers["Content-Disposition"] = $"inline; filename=\"{nombreArchivo}\"";
                Response.Headers["Access-Control-Expose-Headers"] = "Content-Disposition";
                return File(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al generar el ticket: {ex.Message}" });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [RequiereAccionPv(AccionesPvConstantes.VentaImprimir)]
        [HttpGet]
        public async Task<IActionResult> PreliminarSap_Venta([FromQuery] VentaReporteParamDTO dto)
        {
            try
            {
                if (dto.DocEntrySap <= 0)
                    return BadRequest(new { error = "REFRESQUE LA PANTALLA PARA TRAER EL NRO SAP" });

                if (!await ValidarAccesoVendedor(dto.DocEntry, dto.DocEntrySap))
                    return StatusCode(403, new { error = "No tiene autorización para generar el preliminar SAP de esta orden de venta." });

                var tabla = await _puntoVenta.ObtenerReportePreliminarSap(dto.DocEntrySap, dto.DocEntryOwtr);

                if (tabla is null)
                    return NotFound(new { error = "No se encontró la venta para generar el preliminar SAP." });

                var sb = new StringBuilder();

                using (var sw = new StringWriter(sb))
                {
                    tabla.WriteXml(sw, XmlWriteMode.WriteSchema);
                }

                var reportPath = Path.Combine(_rutaCrystal_PDF, "RPT_ORDEN_V.rpt");

                var requestBody = JsonConvert.SerializeObject(new
                {
                    reportPath,
                    dto.Whs,
                    xml = sb.ToString()
                });

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(90);

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    _rutaCrystal_API_PV)
                {
                    Content = new StringContent(
                        requestBody,
                        Encoding.UTF8,
                        "application/json")
                };

                using var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode(500, new { error = $"Error al generar el preliminar SAP: {error}" });
                }

                var pdf = await response.Content.ReadAsByteArrayAsync();

                if (pdf.Length == 0)
                    return StatusCode(500, new { error = "No se obtuvo PDF del preliminar SAP." });

                var docNum = !string.IsNullOrWhiteSpace(dto.NroSap) ? dto.NroSap.Trim() : ObtenerNumeroSapDeTabla(tabla, dto.DocEntrySap);
                var nombreArchivo = $"PreliminarSap_{docNum}.pdf";

                Response.Headers["Content-Disposition"] = $"inline; filename=\"{nombreArchivo}\"";
                Response.Headers["Access-Control-Expose-Headers"] = "Content-Disposition";
                return File(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al generar el preliminar SAP: {ex.Message}" });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta")]
        [RequiereAccionPv(AccionesPvConstantes.VentaImprimir)]
        [HttpGet]
        public async Task<IActionResult> PreliminarPv_Venta([FromQuery] VentaReporteParamDTO dto)
        {
            try
            {
                if (dto.DocEntry <= 0)
                    return BadRequest(new { error = "DocEntry inválido." });

                if (!await ValidarAccesoVendedor(dto.DocEntry, dto.DocEntrySap))
                    return StatusCode(403, new { error = "No tiene autorización para generar el preliminar PV de esta orden de venta." });

                var tabla = await _puntoVenta.ObtenerReportePreliminarPv(dto.DocEntry);

                if (tabla is null)
                    return NotFound(new { error = "No se encontró la venta para generar el preliminar PV." });

                var sb = new StringBuilder();

                using (var sw = new StringWriter(sb))
                {
                    tabla.WriteXml(sw, XmlWriteMode.WriteSchema);
                }

                var reportPath = Path.Combine(_rutaCrystal_PDF, "RPT_ORDEN_P.rpt");

                var requestBody = JsonConvert.SerializeObject(new
                {
                    reportPath,
                    dto.Whs,
                    xml = sb.ToString()
                });

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(90);

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    _rutaCrystal_API_PV)
                {
                    Content = new StringContent(
                        requestBody,
                        Encoding.UTF8,
                        "application/json")
                };

                using var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode(500, new { error = $"Error al generar el preliminar PV: {error}" });
                }

                var pdf = await response.Content.ReadAsByteArrayAsync();

                if (pdf.Length == 0)
                    return StatusCode(500, new { error = "No se obtuvo PDF del preliminar PV." });

                var nombreArchivo = $"PreliminarPv_{dto.DocEntry}.pdf";

                Response.Headers["Content-Disposition"] = $"inline; filename=\"{nombreArchivo}\"";
                Response.Headers["Access-Control-Expose-Headers"] = "Content-Disposition";
                return File(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = $"Error al generar el preliminar PV: {ex.Message}" });
            }
        }

        // ========== CLIENTES BLOQUEADOS ==========

        [HttpGet]
        public IActionResult ClienteBloqueadoInfo()
        {
            return Ok(new { MantenerSesionPuntoVenta = EsUsuarioMantenerSesion() });
        }

        [RequierePermisoModulo("PuntoVenta:ClienteBloqueado")]
        [RequiereAccionPv(AccionesPvConstantes.ClienteBloqueadoVer)]
        [HttpPost]
        public async Task<IActionResult> Buscar_ClientesBloqueados([FromBody] ClienteBloqueadoFiltroDTO filtro)
        {
            try
            {
                var lista = await _puntoVenta.BuscarClientesBloqueados(filtro);

                if (!string.IsNullOrWhiteSpace(filtro.CARCODE) && lista.Count == 0)
                {
                    var todosBloqueados = await _puntoVenta.BuscarClientesBloqueados(
                        new ClienteBloqueadoFiltroDTO { ESTADO = filtro.ESTADO });

                    if (todosBloqueados.Count > 0)
                    {
                        var codigosSql = todosBloqueados.Select(x => x.CARCODE ?? "").Distinct().ToList();
                        var detallesSap = await _puntoVenta.ObtenerDetalleClientesSap(codigosSql);
                        var mapaSap = detallesSap.ToDictionary(x => x.CODIGO_CLIENTE ?? "", x => x, StringComparer.OrdinalIgnoreCase);

                        lista = todosBloqueados.Where(x =>
                        {
                            if (string.IsNullOrEmpty(x.CARCODE)) return false;
                            if (!mapaSap.TryGetValue(x.CARCODE, out var sap)) return false;
                            var termino = filtro.CARCODE!.Trim().ToUpperInvariant();
                            return (sap.CLIENTE ?? "").ToUpperInvariant().Contains(termino)
                                || (sap.RUC ?? "").ToUpperInvariant().Contains(termino)
                                || (x.CARCODE ?? "").ToUpperInvariant().Contains(termino);
                        }).ToList();
                    }
                }

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al buscar clientes bloqueados." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ClienteBloqueado")]
        [RequiereAccionPv(AccionesPvConstantes.ClienteBloqueadoVer)]
        [HttpPost]
        public async Task<IActionResult> Obtener_ClienteBloqueado([FromBody] ClienteBloqueadoIdDTO dto)
        {
            try
            {
                var resultado = await _puntoVenta.ObtenerClienteBloqueado(dto.ID_CLIENTE_BLOQUEADO);
                if (resultado == null)
                    return NotFound(new { error = "Registro no encontrado." });
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al obtener el registro." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ClienteBloqueado")]
        [RequiereAccionPv(AccionesPvConstantes.ClienteBloqueadoGestionar)]
        [HttpPost]
        public async Task<IActionResult> Insertar_ClienteBloqueado([FromBody] ClienteBloqueadoGuardarDTO dto)
        {
            try
            {
                var usuarioStr = ObtenerIdUsuarioActual();
                dto.USUARIO = int.TryParse(usuarioStr, out var u) ? u : 0;

                if (string.IsNullOrWhiteSpace(dto.CARCODE))
                    return BadRequest(new { error = "Debe seleccionar un cliente." });

                var clientesSap = await _puntoVenta.BuscarCliente(dto.CARCODE.Trim());
                if (!clientesSap.Any())
                    return BadRequest(new { error = "El código de cliente \"" + dto.CARCODE + "\" no existe en SAP." });

                var existentes = await _puntoVenta.BuscarClientesBloqueados(new ClienteBloqueadoFiltroDTO { CARCODE = dto.CARCODE });
                if (existentes.Any(x => x.ESTADO))
                    return BadRequest(new { error = "El cliente con código \"" + dto.CARCODE + "\" ya se encuentra bloqueado activo." });

                var id = await _puntoVenta.InsertarClienteBloqueado(dto);
                return Ok(new { success = true, message = "Cliente bloqueado registrado correctamente.", id });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al insertar el registro." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ClienteBloqueado")]
        [RequiereAccionPv(AccionesPvConstantes.ClienteBloqueadoGestionar)]
        [HttpPost]
        public async Task<IActionResult> Actualizar_ClienteBloqueado([FromBody] ClienteBloqueadoGuardarDTO dto)
        {
            try
            {
                var existente = await _puntoVenta.ObtenerClienteBloqueado(dto.ID_CLIENTE_BLOQUEADO);
                if (existente == null)
                    return NotFound(new { error = "El registro no fue encontrado." });
                if (!existente.ESTADO)
                    return BadRequest(new { error = "No se puede modificar un registro inactivo." });

                var usuarioStr = ObtenerIdUsuarioActual();
                dto.USUARIO = int.TryParse(usuarioStr, out var u) ? u : 0;

                var filas = await _puntoVenta.ActualizarClienteBloqueado(dto);
                return Ok(new { success = true, message = "Registro actualizado correctamente." });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al actualizar el registro." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ClienteBloqueado")]
        [RequiereAccionPv(AccionesPvConstantes.ClienteBloqueadoGestionar)]
        [HttpPost]
        public async Task<IActionResult> Eliminar_ClienteBloqueado([FromBody] ClienteBloqueadoIdDTO dto)
        {
            try
            {
                var existente = await _puntoVenta.ObtenerClienteBloqueado(dto.ID_CLIENTE_BLOQUEADO);
                if (existente == null)
                    return NotFound(new { error = "El registro no fue encontrado." });
                if (!existente.ESTADO)
                    return BadRequest(new { error = "No se puede eliminar un registro inactivo." });

                var usuarioStr = ObtenerIdUsuarioActual();
                int usuario = int.TryParse(usuarioStr, out var u) ? u : 0;

                var filas = await _puntoVenta.EliminarClienteBloqueado(dto.ID_CLIENTE_BLOQUEADO, usuario);
                return Ok(new { success = true, message = "Registro eliminado correctamente." });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al eliminar el registro." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta", "PuntoVenta:ClienteBloqueado")]
        [HttpGet]
        public async Task<IActionResult> Validar_ClienteBloqueado(string carcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(carcode))
                    return Ok(new { bloqueado = false });

                var resultado = await _puntoVenta.ValidarClienteBloqueado(carcode);
                if (resultado != null)
                    return Ok(new { bloqueado = true, motivo = resultado.MOTIVO_BLOQUEO });

                return Ok(new { bloqueado = false });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al validar el cliente." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta", "PuntoVenta:ClienteBloqueado")]
        [HttpGet]
        public async Task<IActionResult> Buscar_ClienteSap(string? criterioBusqueda)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(criterioBusqueda) || criterioBusqueda.Length < 3)
                    return Ok(new List<ClienteBusquedaSapDTO>());

                var lista = await _puntoVenta.BuscarCliente(criterioBusqueda);
                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al buscar clientes en SAP." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ClienteBloqueado")]
        [RequiereAccionPv(AccionesPvConstantes.ClienteBloqueadoVer)]
        [HttpPost]
        public async Task<IActionResult> Exportar_ClientesBloqueados([FromBody] ClienteBloqueadoFiltroDTO filtro)
        {
            try
            {
                var lista = await _puntoVenta.BuscarClientesBloqueados(filtro);
                var fileContent = GenerarExcel(lista);
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Clientes_Bloqueados.xlsx");
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al exportar." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ClienteBloqueado")]
        [RequiereAccionPv(AccionesPvConstantes.ClienteBloqueadoGestionar)]
        [HttpPost]
        public async Task<IActionResult> Importar_ClientesBloqueados([FromBody] List<ClienteBloqueadoImportarDTO> registros)
        {
            try
            {
                var resultado = new ClienteBloqueadoImportarResultadoDTO();

                if (registros == null || registros.Count == 0)
                    return Ok(resultado);

                var usuarioStr = ObtenerIdUsuarioActual();
                int usuario = int.TryParse(usuarioStr, out var u) ? u : 0;

                var rucsUnicos = registros
                    .Select(r => r.RUC.Trim().ToUpperInvariant())
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Distinct()
                    .ToList();

                var clientesSap = await _puntoVenta.ValidarRucsBatch(rucsUnicos);
                var sapPorRuc = clientesSap
                    .GroupBy(c => (c.RUC ?? "").Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First());

                var carcodesExistentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var buscarTodos = new ClienteBloqueadoFiltroDTO { CARCODE = null, ESTADO = null };
                var existentes = await _puntoVenta.BuscarClientesBloqueados(buscarTodos);
                foreach (var ex in existentes)
                {
                    if (ex.ESTADO && !string.IsNullOrEmpty(ex.CARCODE))
                        carcodesExistentes.Add(ex.CARCODE.Trim().ToUpperInvariant());
                }

                foreach (var registro in registros)
                {
                    var ruc = (registro.RUC ?? "").Trim().ToUpperInvariant();
                    var motivo = (registro.MOTIVO ?? "").Trim();

                    if (string.IsNullOrEmpty(ruc))
                    {
                        resultado.Errores.Add(new ClienteBloqueadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Ruc = registro.RUC ?? "",
                            Motivo = motivo,
                            Error = "RUC vacío."
                        });
                        continue;
                    }

                    if (string.IsNullOrEmpty(motivo))
                    {
                        resultado.Errores.Add(new ClienteBloqueadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Ruc = registro.RUC ?? "",
                            Motivo = "",
                            Error = "El motivo no puede estar en blanco."
                        });
                        continue;
                    }

                    if (!sapPorRuc.TryGetValue(ruc, out var clienteSap))
                    {
                        resultado.Errores.Add(new ClienteBloqueadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Ruc = registro.RUC ?? "",
                            Motivo = motivo,
                            Error = "RUC no válido en SAP."
                        });
                        continue;
                    }

                    var carcode = (clienteSap.CODIGO_CLIENTE ?? "").Trim().ToUpperInvariant();

                    if (carcodesExistentes.Contains(carcode))
                    {
                        resultado.Errores.Add(new ClienteBloqueadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Ruc = registro.RUC ?? "",
                            Motivo = motivo,
                            Error = "Ya se encuentra bloqueado."
                        });
                        continue;
                    }

                    try
                    {
                        var dto = new ClienteBloqueadoGuardarDTO
                        {
                            CARCODE = clienteSap.CODIGO_CLIENTE ?? "",
                            MOTIVO_BLOQUEO = motivo,
                            ESTADO = true,
                            USUARIO = usuario
                        };

                        await _puntoVenta.InsertarClienteBloqueado(dto);
                        carcodesExistentes.Add(carcode);
                        resultado.Exitosos++;
                    }
                    catch
                    {
                        resultado.Errores.Add(new ClienteBloqueadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Ruc = registro.RUC ?? "",
                            Motivo = motivo,
                            Error = "Error al insertar el registro."
                        });
                    }
                }

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al importar clientes bloqueados." });
            }
        }

        private static byte[] GenerarExcel(List<ClienteBloqueadoDTO> datos)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Clientes Bloqueados");

            string[] headers = { "ID", "CLIENTE", "RUC", "CÓDIGO", "MOTIVO", "ESTADO", "FECHA CREACIÓN" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var headerRange = ws.Range("A1:G1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int fila = 2;
            foreach (var item in datos)
            {
                ws.Cell(fila, 1).Value = item.ID_CLIENTE_BLOQUEADO;
                ws.Cell(fila, 2).Value = item.CLIENTE ?? "";
                ws.Cell(fila, 3).Value = item.RUC ?? "";
                ws.Cell(fila, 4).Value = item.CARCODE ?? "";
                ws.Cell(fila, 5).Value = item.MOTIVO_BLOQUEO ?? "";
                ws.Cell(fila, 6).Value = item.ESTADO ? "ACTIVO" : "INACTIVO";
                ws.Cell(fila, 7).Value = item.FECHAHORA_CREACION ?? "";
                fila++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ========== ARTICULOS FRACCIONADOS ==========

        [HttpGet]
        public IActionResult ArticuloFraccionadoInfo()
        {
            return Ok(new { MantenerSesionPuntoVenta = EsUsuarioMantenerSesion() });
        }

        [RequierePermisoModulo("PuntoVenta:ArticuloFraccionado")]
        [RequiereAccionPv(AccionesPvConstantes.ArticuloFraccionadoVer)]
        [HttpPost]
        public async Task<IActionResult> Buscar_ArticulosFraccionados([FromBody] ArticuloFraccionadoFiltroDTO filtro)
        {
            try
            {
                var lista = await _puntoVenta.BuscarArticulosFraccionados(filtro);

                if (!string.IsNullOrWhiteSpace(filtro.ITEMCODE) && lista.Count == 0)
                {
                    var todos = await _puntoVenta.BuscarArticulosFraccionados(
                        new ArticuloFraccionadoFiltroDTO { ESTADO = filtro.ESTADO });

                    if (todos.Count > 0)
                    {
                        var codigosSql = todos.Select(x => x.ITEMCODE ?? "").Distinct().ToList();
                        var detallesSap = await _puntoVenta.ObtenerDetalleArticulosSap(codigosSql);
                        var mapaSap = detallesSap.ToDictionary(x => x.CODIGO ?? "", x => x, StringComparer.OrdinalIgnoreCase);

                        lista = todos.Where(x =>
                        {
                            if (string.IsNullOrEmpty(x.ITEMCODE)) return false;
                            if (!mapaSap.TryGetValue(x.ITEMCODE, out var sap)) return false;
                            var termino = filtro.ITEMCODE!.Trim().ToUpperInvariant();
                            return (sap.DESCRIPCION ?? "").ToUpperInvariant().Contains(termino)
                                || (x.ITEMCODE ?? "").ToUpperInvariant().Contains(termino);
                        }).ToList();
                    }
                }

                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al buscar artículos fraccionados." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ArticuloFraccionado")]
        [RequiereAccionPv(AccionesPvConstantes.ArticuloFraccionadoVer)]
        [HttpPost]
        public async Task<IActionResult> Obtener_ArticuloFraccionado([FromBody] ArticuloFraccionadoIdDTO dto)
        {
            try
            {
                var resultado = await _puntoVenta.ObtenerArticuloFraccionado(dto.ID_ARTICULO_FRACCIONADO);
                if (resultado == null)
                    return NotFound(new { error = "Registro no encontrado." });
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al obtener el registro." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ArticuloFraccionado")]
        [RequiereAccionPv(AccionesPvConstantes.ArticuloFraccionadoGestionar)]
        [HttpPost]
        public async Task<IActionResult> Insertar_ArticuloFraccionado([FromBody] ArticuloFraccionadoGuardarDTO dto)
        {
            try
            {
                var usuarioStr = ObtenerIdUsuarioActual();
                dto.USUARIO = int.TryParse(usuarioStr, out var u) ? u : 0;

                if (string.IsNullOrWhiteSpace(dto.ITEMCODE))
                    return BadRequest(new { error = "Debe seleccionar un artículo." });

                if (dto.FRACCIONADO <= 0)
                    return BadRequest(new { error = "El valor de fraccionado debe ser mayor a 0." });

                var articulosSap = await _puntoVenta.BuscarArticulosPorCodigo(dto.ITEMCODE.Trim(), 0, "");
                if (!articulosSap.Any())
                    return BadRequest(new { error = "El código de artículo \"" + dto.ITEMCODE + "\" no existe en SAP." });

                var umds = await _puntoVenta.BuscarUmdArticulo(dto.ITEMCODE.Trim());
                var predeterminada = umds.FirstOrDefault(u => (u.ES_PREDETERMINADA ?? "").Trim().ToUpper() == "Y") ?? umds.FirstOrDefault();
                var factorPred = predeterminada != null ? predeterminada.FACTOR : 1;

                if (factorPred <= 1)
                {
                    return BadRequest(new { error = "El artículo no se puede registrar como fraccionado porque su unidad de medida predeterminada tiene factor 1." });
                }

                if (dto.FRACCIONADO >= factorPred)
                {
                    return BadRequest(new { error = $"El valor de fraccionado ({dto.FRACCIONADO}) debe ser menor al factor de la unidad predeterminada ({factorPred:0.##})." });
                }

                if (dto.MONTO_MINIMO <= 0)
                {
                    return BadRequest(new { error = "Debe seleccionar un monto mínimo mayor a 0." });
                }

                if (dto.MONTO_MINIMO < dto.FRACCIONADO)
                {
                    return BadRequest(new { error = "El monto mínimo no puede ser menor al valor fraccionado." });
                }

                if (dto.MONTO_MINIMO % dto.FRACCIONADO != 0)
                {
                    return BadRequest(new { error = "El monto mínimo debe ser un múltiplo exacto del valor fraccionado." });
                }

                if (dto.MONTO_MINIMO >= factorPred)
                {
                    return BadRequest(new { error = $"El monto mínimo ({dto.MONTO_MINIMO}) debe ser menor al factor de la unidad predeterminada ({factorPred:0.##})." });
                }

                var existentes = await _puntoVenta.BuscarArticulosFraccionados(new ArticuloFraccionadoFiltroDTO { ITEMCODE = dto.ITEMCODE });
                if (existentes.Any(x => x.ESTADO))
                    return BadRequest(new { error = "El artículo con código \"" + dto.ITEMCODE + "\" ya se encuentra registrado como fraccionado activo." });

                var id = await _puntoVenta.InsertarArticuloFraccionado(dto);
                return Ok(new { success = true, message = "Artículo fraccionado registrado correctamente.", id });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al insertar el registro." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ArticuloFraccionado")]
        [RequiereAccionPv(AccionesPvConstantes.ArticuloFraccionadoGestionar)]
        [HttpPost]
        public async Task<IActionResult> Actualizar_ArticuloFraccionado([FromBody] ArticuloFraccionadoGuardarDTO dto)
        {
            try
            {
                if (dto.ID_ARTICULO_FRACCIONADO <= 0)
                    return BadRequest(new { error = "ID de artículo fraccionado no válido." });

                if (dto.FRACCIONADO <= 0)
                    return BadRequest(new { error = "El valor de fraccionado debe ser mayor a 0." });

                var existente = await _puntoVenta.ObtenerArticuloFraccionado(dto.ID_ARTICULO_FRACCIONADO);
                if (existente == null)
                    return NotFound(new { error = "El registro no fue encontrado." });
                if (!existente.ESTADO)
                    return BadRequest(new { error = "No se puede modificar un registro inactivo." });

                var itemcode = existente.ITEMCODE ?? dto.ITEMCODE;
                if (!string.IsNullOrWhiteSpace(itemcode))
                {
                    var umds = await _puntoVenta.BuscarUmdArticulo(itemcode.Trim());
                    var predeterminada = umds.FirstOrDefault(u => (u.ES_PREDETERMINADA ?? "").Trim().ToUpper() == "Y") ?? umds.FirstOrDefault();
                    var factorPred = predeterminada != null ? predeterminada.FACTOR : 1;

                    if (factorPred <= 1)
                    {
                        return BadRequest(new { error = "El artículo no se puede registrar como fraccionado porque su unidad de medida predeterminada tiene factor 1." });
                    }

                    if (dto.FRACCIONADO >= factorPred)
                    {
                        return BadRequest(new { error = $"El valor de fraccionado ({dto.FRACCIONADO}) debe ser menor al factor de la unidad predeterminada ({factorPred:0.##})." });
                    }

                    if (dto.MONTO_MINIMO <= 0)
                    {
                        return BadRequest(new { error = "Debe seleccionar un monto mínimo mayor a 0." });
                    }

                    if (dto.MONTO_MINIMO < dto.FRACCIONADO)
                    {
                        return BadRequest(new { error = "El monto mínimo no puede ser menor al valor fraccionado." });
                    }

                    if (dto.MONTO_MINIMO % dto.FRACCIONADO != 0)
                    {
                        return BadRequest(new { error = "El monto mínimo debe ser un múltiplo exacto del valor fraccionado." });
                    }

                    if (dto.MONTO_MINIMO >= factorPred)
                    {
                        return BadRequest(new { error = $"El monto mínimo ({dto.MONTO_MINIMO}) debe ser menor al factor de la unidad predeterminada ({factorPred:0.##})." });
                    }
                }

                var usuarioStr = ObtenerIdUsuarioActual();
                dto.USUARIO = int.TryParse(usuarioStr, out var u) ? u : 0;

                var filas = await _puntoVenta.ActualizarArticuloFraccionado(dto);
                return Ok(new { success = true, message = "Registro actualizado correctamente." });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al actualizar el registro." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ArticuloFraccionado")]
        [RequiereAccionPv(AccionesPvConstantes.ArticuloFraccionadoGestionar)]
        [HttpPost]
        public async Task<IActionResult> Eliminar_ArticuloFraccionado([FromBody] ArticuloFraccionadoIdDTO dto)
        {
            try
            {
                var existente = await _puntoVenta.ObtenerArticuloFraccionado(dto.ID_ARTICULO_FRACCIONADO);
                if (existente == null)
                    return NotFound(new { error = "El registro no fue encontrado." });
                if (!existente.ESTADO)
                    return BadRequest(new { error = "No se puede eliminar un registro inactivo." });

                var usuarioStr = ObtenerIdUsuarioActual();
                int usuario = int.TryParse(usuarioStr, out var u) ? u : 0;

                var filas = await _puntoVenta.EliminarArticuloFraccionado(dto.ID_ARTICULO_FRACCIONADO, usuario);
                return Ok(new { success = true, message = "Registro eliminado correctamente." });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al eliminar el registro." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ArticuloFraccionado")]
        [RequiereAccionPv(AccionesPvConstantes.ArticuloFraccionadoGestionar)]
        [HttpPost]
        public async Task<IActionResult> EliminarTodos_ArticulosFraccionados()
        {
            try
            {
                var usuarioStr = ObtenerIdUsuarioActual();
                int usuario = int.TryParse(usuarioStr, out var u) ? u : 0;

                var filas = await _puntoVenta.EliminarTodosArticulosFraccionados(usuario);
                return Ok(new { success = true, filas = filas, message = $"Se inactivaron {filas} artículos fraccionados correctamente." });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al inactivar los artículos fraccionados." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:Venta", "PuntoVenta:ArticuloFraccionado")]
        [HttpGet]
        public async Task<IActionResult> Validar_ArticuloFraccionado(string itemcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(itemcode))
                    return Ok(new { fraccionado = false });

                var resultado = await _puntoVenta.ValidarArticuloFraccionado(itemcode);
                if (resultado != null)
                    return Ok(new { fraccionado = true, multiplo = resultado.FRACCIONADO, montoMinimo = resultado.MONTO_MINIMO });

                return Ok(new { fraccionado = false });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al validar el artículo." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ArticuloFraccionado")]
        [RequiereAccionPv(AccionesPvConstantes.ArticuloFraccionadoGestionar, AccionesPvConstantes.ArticuloFraccionadoVer)]
        [HttpGet]
        public async Task<IActionResult> Buscar_ArticuloSap(string? criterioBusqueda)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(criterioBusqueda) || criterioBusqueda.Length < 3)
                    return Ok(new List<ArticuloAutocompleteDTO>());

                var lista = await _puntoVenta.BuscarArticulosAutocomplete(
                    TruncarTexto(criterioBusqueda, 200),
                    0,
                    ""
                );
                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al buscar artículos en SAP." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ArticuloFraccionado")]
        [RequiereAccionPv(AccionesPvConstantes.ArticuloFraccionadoVer)]
        [HttpPost]
        public async Task<IActionResult> Exportar_ArticulosFraccionados([FromBody] ArticuloFraccionadoFiltroDTO filtro)
        {
            try
            {
                var lista = await _puntoVenta.BuscarArticulosFraccionados(filtro);
                var fileContent = GenerarExcelFraccionados(lista);
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Articulos_Fraccionados.xlsx");
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al exportar." });
            }
        }

        [RequierePermisoModulo("PuntoVenta:ArticuloFraccionado")]
        [RequiereAccionPv(AccionesPvConstantes.ArticuloFraccionadoGestionar)]
        [HttpPost]
        public async Task<IActionResult> Importar_ArticulosFraccionados([FromBody] List<ArticuloFraccionadoImportarDTO> registros)
        {
            try
            {
                var resultado = new ArticuloFraccionadoImportarResultadoDTO();

                if (registros == null || registros.Count == 0)
                    return Ok(resultado);

                var usuarioStr = ObtenerIdUsuarioActual();
                int usuario = int.TryParse(usuarioStr, out var u) ? u : 0;

                var itemcodesUnicos = registros
                    .Select(r => r.ITEMCODE.Trim().ToUpperInvariant())
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Distinct()
                    .ToList();

                var articulosSap = await _puntoVenta.ValidarItemcodesBatch(itemcodesUnicos);
                var sapPorCodigo = articulosSap
                    .GroupBy(a => (a.CODIGO ?? "").Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First());

                var mapaUmds = new Dictionary<string, List<UnidadMedidaArticuloSapDTO>>(StringComparer.OrdinalIgnoreCase);
                foreach (var code in itemcodesUnicos)
                {
                    try
                    {
                        var uList = await _puntoVenta.BuscarUmdArticulo(code);
                        mapaUmds[code] = uList;
                    }
                    catch
                    {
                        mapaUmds[code] = new List<UnidadMedidaArticuloSapDTO>();
                    }
                }

                var mapaExistentes = new Dictionary<string, ArticuloFraccionadoDTO>(StringComparer.OrdinalIgnoreCase);
                var buscarTodos = new ArticuloFraccionadoFiltroDTO { ITEMCODE = null, ESTADO = null };
                var existentes = await _puntoVenta.BuscarArticulosFraccionados(buscarTodos);
                foreach (var ex in existentes)
                {
                    if (!string.IsNullOrEmpty(ex.ITEMCODE))
                    {
                        var key = ex.ITEMCODE.Trim().ToUpperInvariant();
                        if (!mapaExistentes.ContainsKey(key))
                            mapaExistentes[key] = ex;
                    }
                }

                foreach (var registro in registros)
                {
                    var itemcode = (registro.ITEMCODE ?? "").Trim().ToUpperInvariant();
                    var fraccionado = registro.FRACCIONADO;
                    var montoMinimo = registro.MONTO_MINIMO > 0 ? registro.MONTO_MINIMO : fraccionado;

                    if (string.IsNullOrEmpty(itemcode))
                    {
                        resultado.Errores.Add(new ArticuloFraccionadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Itemcode = registro.ITEMCODE ?? "",
                            Fraccionado = fraccionado,
                            MontoMinimo = montoMinimo,
                            Error = "Código de artículo vacío."
                        });
                        continue;
                    }

                    if (fraccionado <= 0)
                    {
                        resultado.Errores.Add(new ArticuloFraccionadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Itemcode = itemcode,
                            Fraccionado = fraccionado,
                            MontoMinimo = montoMinimo,
                            Error = "El valor de fraccionado debe ser mayor a 0."
                        });
                        continue;
                    }

                    if (!sapPorCodigo.TryGetValue(itemcode, out var articuloSap))
                    {
                        resultado.Errores.Add(new ArticuloFraccionadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Itemcode = itemcode,
                            Fraccionado = fraccionado,
                            MontoMinimo = montoMinimo,
                            Error = "Código no válido en SAP."
                        });
                        continue;
                    }

                    if (!mapaUmds.TryGetValue(itemcode, out var umdsItem) || umdsItem.Count == 0)
                    {
                        try
                        {
                            umdsItem = await _puntoVenta.BuscarUmdArticulo(itemcode);
                            mapaUmds[itemcode] = umdsItem;
                        }
                        catch
                        {
                            umdsItem = new List<UnidadMedidaArticuloSapDTO>();
                        }
                    }

                    var predeterminada = umdsItem.FirstOrDefault(u => (u.ES_PREDETERMINADA ?? "").Trim().ToUpper() == "Y") ?? umdsItem.FirstOrDefault();
                    var factorPred = predeterminada != null ? predeterminada.FACTOR : 1;

                    if (factorPred <= 1)
                    {
                        resultado.Errores.Add(new ArticuloFraccionadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Itemcode = itemcode,
                            Fraccionado = fraccionado,
                            MontoMinimo = montoMinimo,
                            Error = "No es fraccionable: la unidad de medida predeterminada tiene factor 1."
                        });
                        continue;
                    }

                    if (fraccionado >= factorPred)
                    {
                        resultado.Errores.Add(new ArticuloFraccionadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Itemcode = itemcode,
                            Fraccionado = fraccionado,
                            MontoMinimo = montoMinimo,
                            Error = $"El valor de fraccionado ({fraccionado}) debe ser menor al factor de la unidad predeterminada ({factorPred:0.##})."
                        });
                        continue;
                    }

                    if (montoMinimo <= 0)
                    {
                        resultado.Errores.Add(new ArticuloFraccionadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Itemcode = itemcode,
                            Fraccionado = fraccionado,
                            MontoMinimo = montoMinimo,
                            Error = "El monto mínimo debe ser mayor a 0."
                        });
                        continue;
                    }

                    if (montoMinimo < fraccionado || montoMinimo % fraccionado != 0)
                    {
                        resultado.Errores.Add(new ArticuloFraccionadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Itemcode = itemcode,
                            Fraccionado = fraccionado,
                            MontoMinimo = montoMinimo,
                            Error = $"El monto mínimo ({montoMinimo}) no es múltiplo del valor fraccionado ({fraccionado}). No se importará."
                        });
                        continue;
                    }

                    if (montoMinimo >= factorPred)
                    {
                        resultado.Errores.Add(new ArticuloFraccionadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Itemcode = itemcode,
                            Fraccionado = fraccionado,
                            MontoMinimo = montoMinimo,
                            Error = $"El monto mínimo ({montoMinimo}) debe ser menor al factor de la unidad predeterminada ({factorPred:0.##}). No se importará."
                        });
                        continue;
                    }

                    try
                    {
                        if (mapaExistentes.TryGetValue(itemcode, out var itemExistente))
                        {
                            var dto = new ArticuloFraccionadoGuardarDTO
                            {
                                ID_ARTICULO_FRACCIONADO = itemExistente.ID_ARTICULO_FRACCIONADO,
                                ITEMCODE = itemcode,
                                FRACCIONADO = fraccionado,
                                MONTO_MINIMO = montoMinimo,
                                ESTADO = true,
                                USUARIO = usuario
                            };

                            await _puntoVenta.ActualizarArticuloFraccionado(dto);
                            itemExistente.FRACCIONADO = fraccionado;
                            itemExistente.MONTO_MINIMO = montoMinimo;
                            itemExistente.ESTADO = true;
                            resultado.Actualizados++;
                        }
                        else
                        {
                            var dto = new ArticuloFraccionadoGuardarDTO
                            {
                                ITEMCODE = articuloSap.CODIGO ?? "",
                                FRACCIONADO = fraccionado,
                                MONTO_MINIMO = montoMinimo,
                                ESTADO = true,
                                USUARIO = usuario
                            };

                            var nuevoId = await _puntoVenta.InsertarArticuloFraccionado(dto);
                            mapaExistentes[itemcode] = new ArticuloFraccionadoDTO
                            {
                                ID_ARTICULO_FRACCIONADO = nuevoId,
                                ITEMCODE = itemcode,
                                FRACCIONADO = fraccionado,
                                MONTO_MINIMO = montoMinimo,
                                ESTADO = true
                            };
                            resultado.Nuevos++;
                        }

                        resultado.Exitosos++;
                    }
                    catch (Exception ex)
                    {
                        resultado.Errores.Add(new ArticuloFraccionadoImportarErrorDTO
                        {
                            Fila = registro.FILA,
                            Itemcode = itemcode,
                            Fraccionado = fraccionado,
                            MontoMinimo = montoMinimo,
                            Error = $"Error al guardar el registro: {ex.Message}"
                        });
                    }
                }

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al importar artículos fraccionados." });
            }
        }

        private static byte[] GenerarExcelFraccionados(List<ArticuloFraccionadoDTO> datos)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Artículos Fraccionados");

            string[] headers = { "ID", "ARTÍCULO", "CÓDIGO", "FRACCIONADO", "MONTO MÍNIMO", "ESTADO", "FECHA CREACIÓN" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var headerRange = ws.Range("A1:G1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int fila = 2;
            foreach (var item in datos)
            {
                ws.Cell(fila, 1).Value = item.ID_ARTICULO_FRACCIONADO;
                ws.Cell(fila, 2).Value = item.ARTICULO ?? "";
                ws.Cell(fila, 3).Value = item.ITEMCODE ?? "";
                ws.Cell(fila, 4).Value = item.FRACCIONADO;
                ws.Cell(fila, 5).Value = item.MONTO_MINIMO;
                ws.Cell(fila, 6).Value = item.ESTADO ? "ACTIVO" : "INACTIVO";
                ws.Cell(fila, 7).Value = item.FECHAHORA_CREACION ?? "";
                fila++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static int ObtenerHoraMilitar(string? deliveryTime)
        {
            if (string.IsNullOrWhiteSpace(deliveryTime)) return -1;
            var trimmed = deliveryTime.Trim();

            if (int.TryParse(trimmed, out int codigo) && codigo >= 1 && codigo <= 14)
            {
                return codigo + 6; // "01" -> 7, "06" -> 12, "07" -> 13, "14" -> 20
            }

            var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"(\d{1,2})(?::(\d{2}))?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int h))
            {
                var lower = trimmed.ToLowerInvariant();
                if (lower.Contains("p. m.") || lower.Contains("pm") || lower.Contains("p.m."))
                {
                    if (h < 12) h += 12;
                }
                else if (lower.Contains("a. m.") || lower.Contains("am") || lower.Contains("a.m."))
                {
                    if (h == 12) h = 0;
                }
                return h;
            }

            return -1;
        }

        #region Modificar Condición de Pago

        private string? ObtenerRolCondicionPagoUsuario()
        {
            if (User.Claims.Any(c => c.Type == "pv_action" && c.Value == AccionesPvConstantes.VentaModificarCondPagoCobranza))
            {
                return "COBRANZA";
            }

            if (User.Claims.Any(c => c.Type == "pv_action" && c.Value == AccionesPvConstantes.VentaModificarCondPago))
            {
                return "VENTA";
            }

            return null;
        }

        [RequiereAccionPv(AccionesPvConstantes.VentaModificarCondPago, AccionesPvConstantes.VentaModificarCondPagoCobranza)]
        [HttpGet]
        public async Task<IActionResult> ObtenerDatosModificarCondicionPago(int docEntry, int docEntrySap)
        {
            try
            {
                var rol = ObtenerRolCondicionPagoUsuario();
                if (string.IsNullOrEmpty(rol))
                {
                    return Ok(new { success = false, message = "No tiene permisos para consultar o modificar la condición de pago." });
                }

                if (!await ValidarAccesoVendedor(docEntry, docEntrySap))
                {
                    return Ok(new { success = false, message = "No tiene autorización para consultar o modificar la condición de pago de esta orden de venta." });
                }

                if (docEntrySap <= 0)
                {
                    return Ok(new { success = false, message = "La orden no cuenta con un DocEntry de SAP válido." });
                }

                var ordenSap = await _puntoVenta.ObtenerOrdenCondicionPagoSap(docEntrySap);
                if (ordenSap == null)
                {
                    return Ok(new { success = false, message = $"No se encontró la orden con DocEntry SAP {docEntrySap} en SAP." });
                }

                bool puedeModificar = true;
                string? motivoBloqueo = null;

                if (ordenSap.DocStatus == "C")
                {
                    puedeModificar = false;
                    motivoBloqueo = $"La orden SAP N° {ordenSap.DocNum} se encuentra CERRADA o CANCELADA en SAP.";
                }
                else if (ordenSap.Canceled != "N")
                {
                    puedeModificar = false;
                    motivoBloqueo = $"La orden SAP N° {ordenSap.DocNum} se encuentra CANCELADA en SAP.";
                }
                else
                {
                    bool esEnviadoWms = ordenSap.USophEnwms == "Y" || ordenSap.USophEnwms == "1" || ordenSap.USophEnwms == "S";
                    if (!esEnviadoWms)
                    {
                        puedeModificar = false;
                        motivoBloqueo = $"La orden SAP N° {ordenSap.DocNum} no tiene estado 'ENVIADO WMS' en SAP.";
                    }
                }

                // Obtener lugar de entrega de la orden local
                string lugarEntregaOrden = "";
                if (docEntry > 0)
                {
                    var ventaLocal = await _puntoVenta.VerVenta(docEntry);
                    lugarEntregaOrden = (ventaLocal?.DELIVERY_POINT ?? "").Trim().ToUpperInvariant();
                }

                var todasFormasPago = await _puntoVenta.BuscarFormasPago(null);
                var listaFiltrada = new List<FormaPagoSapDTO>();
                string condActual = (ordenSap.PymntGroup ?? "").ToUpperInvariant();

                if (puedeModificar && !string.IsNullOrEmpty(rol))
                {
                    if (rol == "COBRANZA")
                    {
                        listaFiltrada = (todasFormasPago ?? new List<FormaPagoSapDTO>())
                            .Where(fp => !string.IsNullOrWhiteSpace(fp.NOMBRE) && fp.NOMBRE.Trim() != "*")
                            .OrderBy(fp => fp.NOMBRE)
                            .ToList();
                    }
                    else if (rol == "VENTA")
                    {
                        bool actualEsContraEntrega = condActual.Contains("CONTRA ENTREGA") || condActual.Contains("CONTRAENTREGA");
                        bool actualEsCredito = condActual.Contains("CREDITO") || condActual.Contains("CRÉDITO");
                        bool actualEsContado = !actualEsContraEntrega && condActual.Contains("CONTADO");

                        if (todasFormasPago != null)
                        {
                            foreach (var fp in todasFormasPago)
                            {
                                string nombreFp = (fp.NOMBRE ?? "").Trim().ToUpperInvariant();
                                bool fpEsContraEntrega = nombreFp == "CONTRA ENTREGA *" || nombreFp == "CONTRAENTREGA *" || nombreFp.StartsWith("CONTRA ENTREGA *") || nombreFp.StartsWith("CONTRAENTREGA *");
                                bool fpEsContado = nombreFp == "CONTADO *" || nombreFp.StartsWith("CONTADO *");

                                if (actualEsContraEntrega)
                                {
                                    // Si la orden actual es CONTRA ENTREGA -> solo "CONTADO *"
                                    if (fpEsContado) listaFiltrada.Add(fp);
                                }
                                else if (actualEsContado)
                                {
                                    // Si la orden actual es CONTADO -> solo "CONTRA ENTREGA *"
                                    if (fpEsContraEntrega) listaFiltrada.Add(fp);
                                }
                                else if (actualEsCredito)
                                {
                                    // Si la orden actual contiene CREDITO -> literal solamente "CONTADO *" y "CONTRA ENTREGA *"
                                    if (fpEsContado || fpEsContraEntrega) listaFiltrada.Add(fp);
                                }
                            }
                        }
                    }

                    // Regla Comercial: Si el lugar de entrega es AGENCIA, no se permite ninguna opción CONTRA ENTREGA (salvo autorización de Cobranza)
                    if (rol != "COBRANZA" && lugarEntregaOrden == "AGENCIA")
                    {
                        listaFiltrada = listaFiltrada
                            .Where(fp => {
                                var nom = (fp.NOMBRE ?? "").Trim().ToUpperInvariant();
                                return !nom.Contains("CONTRA ENTREGA") && !nom.Contains("CONTRAENTREGA");
                            })
                            .ToList();
                    }
                }

                var historial = await _puntoVenta.ListarHistorialCondicionPago(docEntry, docEntrySap);

                var resultado = new ModalCondicionPagoDataDTO
                {
                    DocEntry = docEntry,
                    DocEntrySap = docEntrySap,
                    DocNum = ordenSap.DocNum,
                    CardCode = ordenSap.CardCode,
                    CardName = ordenSap.CardName,
                    Ruc = ordenSap.LicTradNum,
                    EstadoSap = ordenSap.EstadoSap,
                    CondicionPagoActualCodigo = ordenSap.GroupNum,
                    CondicionPagoActualNombre = ordenSap.PymntGroup,
                    RolUsuario = rol,
                    PuedeModificar = puedeModificar,
                    MotivoBloqueo = motivoBloqueo,
                    OpcionesPermitidas = listaFiltrada,
                    Historial = historial ?? new List<BE_HistorialCondicionPago>()
                };

                return Ok(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Error al obtener datos: " + ex.Message });
            }
        }

        [RequiereAccionPv(AccionesPvConstantes.VentaModificarCondPago, AccionesPvConstantes.VentaModificarCondPagoCobranza)]
        [HttpPost]
        public async Task<IActionResult> ActualizarCondicionPago([FromBody] ActualizarCondicionPagoRequestDTO request)
        {
            if (request == null || request.DocEntrySap <= 0)
            {
                return Ok(new { success = false, message = "Datos de solicitud inválidos." });
            }

            var rol = ObtenerRolCondicionPagoUsuario();
            if (string.IsNullOrEmpty(rol))
            {
                return Ok(new { success = false, message = "No tiene permisos para modificar la condición de pago." });
            }

            if (!await ValidarAccesoVendedor(request.DocEntry, request.DocEntrySap))
            {
                return Ok(new { success = false, message = "No tiene autorización para modificar la condición de pago de esta orden de venta." });
            }

            if (request.NuevoGroupNum <= 0)
            {
                return Ok(new { success = false, message = "Debe seleccionar una condición de pago válida." });
            }

            if (request.NuevoGroupNum == request.ActualGroupNum)
            {
                return Ok(new { success = false, message = "La condición de pago seleccionada es igual a la actual. No hay cambios por realizar." });
            }

            // Revalidación atómica en SAP HANA antes de modificar
            var ordenSap = await _puntoVenta.ObtenerOrdenCondicionPagoSap(request.DocEntrySap);
            if (ordenSap == null)
            {
                return Ok(new { success = false, message = $"No se encontró la orden con DocEntry SAP {request.DocEntrySap} en SAP." });
            }

            if (ordenSap.DocStatus == "C")
            {
                return Ok(new { success = false, message = $"La orden SAP N° {ordenSap.DocNum} se encuentra CERRADA o CANCELADA en SAP. No se puede modificar." });
            }

            if (ordenSap.Canceled != "N")
            {
                return Ok(new { success = false, message = $"La orden SAP N° {ordenSap.DocNum} se encuentra CANCELADA en SAP. No se puede modificar." });
            }

            bool esEnviadoWms = ordenSap.USophEnwms == "Y" || ordenSap.USophEnwms == "1" || ordenSap.USophEnwms == "S";
            if (!esEnviadoWms)
            {
                return Ok(new { success = false, message = $"La orden SAP N° {ordenSap.DocNum} no tiene estado 'ENVIADO WMS' en SAP. No se puede modificar." });
            }

            if (ordenSap.GroupNum == request.NuevoGroupNum)
            {
                return Ok(new { success = false, message = "La orden en SAP ya tiene configurada la condición de pago seleccionada." });
            }

            // Obtener lugar de entrega de la orden local
            string lugarEntregaOrden = "";
            if (request.DocEntry > 0)
            {
                var ventaLocal = await _puntoVenta.VerVenta(request.DocEntry);
                lugarEntregaOrden = (ventaLocal?.DELIVERY_POINT ?? "").Trim().ToUpperInvariant();
            }

            // Obtener nombre de la nueva condición de pago si no viene en el request
            string condicionNuevoNombre = request.CondicionPagoNuevoNombre?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(condicionNuevoNombre))
            {
                var formasPago = await _puntoVenta.BuscarFormasPago(null);
                var fpEncontrada = formasPago?.FirstOrDefault(x => x.CODIGO == request.NuevoGroupNum);
                condicionNuevoNombre = fpEncontrada?.NOMBRE?.Trim() ?? $"GroupNum: {request.NuevoGroupNum}";
            }

            // Regla Comercial: Si el lugar de entrega es AGENCIA, no se permite cambiar a CONTRA ENTREGA (salvo autorización de Cobranza)
            var condNuevoUpper = condicionNuevoNombre.ToUpperInvariant();
            if (rol != "COBRANZA" && lugarEntregaOrden == "AGENCIA" && (condNuevoUpper.Contains("CONTRA ENTREGA") || condNuevoUpper.Contains("CONTRAENTREGA")))
            {
                return Ok(new { success = false, message = "Incompatibilidad comercial: Cuando el Lugar de Entrega es AGENCIA, no se permite cambiar a la condición de pago CONTRA ENTREGA." });
            }

            var usuarioActual = string.IsNullOrWhiteSpace(ObtenerUsuarioActual()) ? "SISTEMA" : ObtenerUsuarioActual().Trim();

            var resultado = await _puntoVenta.ActualizarCondicionPago_ServiceLayer(
                request.DocEntry,
                request.DocEntrySap,
                ordenSap.GroupNum,
                ordenSap.PymntGroup,
                request.NuevoGroupNum,
                condicionNuevoNombre,
                usuarioActual,
                ordenSap.DocNum,
                request.Motivo
            );

            return Ok(resultado);
        }

        #region Scraping y Consulta Oficial DIGEMID

        [HttpGet]
        public async Task<IActionResult> ConsultarDigemid([FromQuery] string ruc)
        {
            var resultado = await _digemidService.ConsultarEstablecimientosPorRucAsync(ruc);
            if (!resultado.Success && !string.IsNullOrWhiteSpace(resultado.Error))
            {
                // Si el RUC es inválido, devolver 400; en otros casos devolver Ok con Success=false para que el frontend maneje la alerta
                if (resultado.Error.Contains("11 dígitos"))
                {
                    return BadRequest(resultado);
                }
            }

            return Ok(resultado);
        }

        #endregion

        #endregion
    }
}

