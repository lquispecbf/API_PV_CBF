namespace BE.Seguridad
{
    public static class AccionesPvConstantes
    {
        // Módulo Ventas
        public const string VentaVer = "VENTA.VER";
        public const string VentaCrear = "VENTA.CREAR";
        public const string VentaGuardarBorrador = "VENTA.GUARDAR_BORRADOR";
        public const string VentaAnular = "VENTA.ANULAR";
        public const string VentaAnularEnviadoWms = "VENTA.ANULAR_ENVIADO_WMS";
        public const string VentaEnviarWms = "VENTA.ENVIAR_WMS";
        public const string VentaReabrir = "VENTA.REABRIR";
        public const string VentaModificarCondPago = "VENTA.MODIFICAR_COND_PAGO";
        public const string VentaModificarCondPagoCobranza = "VENTA.MODIFICAR_COND_PAGO_COBRANZA";
        public const string VentaMantenerSesion = "VENTA.MANTENER_SESION";
        public const string VentaImprimir = "VENTA.IMPRIMIR";
        public const string VentaExportarExcel = "VENTA.EXPORTAR_EXCEL";

        // Módulo Clientes Bloqueados
        public const string ClienteBloqueadoVer = "CLIENTE_BLOQUEADO.VER";
        public const string ClienteBloqueadoGestionar = "CLIENTE_BLOQUEADO.GESTIONAR";

        // Módulo Artículos Fraccionados
        public const string ArticuloFraccionadoVer = "ARTICULO_FRACCIONADO.VER";
        public const string ArticuloFraccionadoGestionar = "ARTICULO_FRACCIONADO.GESTIONAR";

        // Módulo Stock por Almacén / Reportes
        public const string StockAlmacenVer = "STOCK_ALMACEN.VER";
        public const string StockAlmacenExportar = "STOCK_ALMACEN.EXPORTAR";
    }

    public static class RolesPvConstantes
    {
        public const string Administrador = "ADMINISTRADOR_PV";
        public const string Supervisor = "SUPERVISOR_PV";
        public const string Vendedor = "VENDEDOR_PV";
        public const string SoloConsulta = "SOLO_CONSULTA_PV";
        public const string Personalizado = "PERSONALIZADO_PV";
    }
}
