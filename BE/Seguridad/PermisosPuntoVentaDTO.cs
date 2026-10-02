using System.Collections.Generic;

namespace BE.Seguridad
{
    public class PermisosPuntoVentaDTO
    {
        public int IdRolPv { get; set; }
        public string CodigoRolPv { get; set; } = RolesPvConstantes.Vendedor;
        public string NombreRolPv { get; set; } = "Vendedor Estándar";

        public List<string> Acciones { get; set; } = new List<string>();

        // Helpers de Acceso Rápido
        public bool PuedeVer => TieneAccion(AccionesPvConstantes.VentaVer);
        public bool PuedeCrear => TieneAccion(AccionesPvConstantes.VentaCrear);
        public bool PuedeGuardarBorrador => TieneAccion(AccionesPvConstantes.VentaGuardarBorrador);
        public bool PuedeAnular => TieneAccion(AccionesPvConstantes.VentaAnular);
        public bool PuedeAnularEnviadoWms => TieneAccion(AccionesPvConstantes.VentaAnularEnviadoWms);
        public bool PuedeEnviarWms => TieneAccion(AccionesPvConstantes.VentaEnviarWms);
        public bool PuedeReabrir => TieneAccion(AccionesPvConstantes.VentaReabrir);
        public bool PuedeModificarCondicionPago => TieneAccion(AccionesPvConstantes.VentaModificarCondPago) || TieneAccion(AccionesPvConstantes.VentaModificarCondPagoCobranza);
        public bool PuedeModificarCondicionPagoCobranza => TieneAccion(AccionesPvConstantes.VentaModificarCondPagoCobranza);
        public bool MantenerSesion => TieneAccion(AccionesPvConstantes.VentaMantenerSesion);
        public bool PuedeImprimir => TieneAccion(AccionesPvConstantes.VentaImprimir);
        public bool PuedeExportarExcel => TieneAccion(AccionesPvConstantes.VentaExportarExcel);

        public bool PuedeClienteBloqueadoVer => TieneAccion(AccionesPvConstantes.ClienteBloqueadoVer);
        public bool PuedeClienteBloqueadoGestionar => TieneAccion(AccionesPvConstantes.ClienteBloqueadoGestionar);

        public bool PuedeArticuloFraccionadoVer => TieneAccion(AccionesPvConstantes.ArticuloFraccionadoVer);
        public bool PuedeArticuloFraccionadoGestionar => TieneAccion(AccionesPvConstantes.ArticuloFraccionadoGestionar);

        public bool PuedeStockAlmacenVer => TieneAccion(AccionesPvConstantes.StockAlmacenVer);
        public bool PuedeStockAlmacenExportar => TieneAccion(AccionesPvConstantes.StockAlmacenExportar);

        public bool EsSoloLectura => !PuedeCrear && !PuedeAnular && !PuedeEnviarWms && !PuedeReabrir;
        public bool EsAdministrador => CodigoRolPv == RolesPvConstantes.Administrador;

        public bool TieneAccion(string codigoAccion)
        {
            if (string.IsNullOrWhiteSpace(codigoAccion) || Acciones == null) return false;
            return Acciones.Contains(codigoAccion.Trim().ToUpperInvariant());
        }
    }
}
