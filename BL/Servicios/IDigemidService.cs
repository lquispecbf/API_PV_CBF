using BE.PuntoVenta;

namespace BL.Servicios
{
    public interface IDigemidService
    {
        Task<DigemidConsultaResponseDTO> ConsultarEstablecimientosPorRucAsync(string ruc);
    }
}
