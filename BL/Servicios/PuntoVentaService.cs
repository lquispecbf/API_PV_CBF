using BE.PuntoVenta;
using DA.Repositorio.Repositorio_PuntoVenta;

namespace BL.Servicios
{
    public interface IPuntoVentaService
    {
        Task<VentaGuardarResponseDTO> ProcesarVentaAsync(VentaGuardarRequestDTO request);
    }

    public class PuntoVentaService : IPuntoVentaService
    {
        private readonly IPuntoVenta _puntoVentaRepo;

        public PuntoVentaService(IPuntoVenta puntoVentaRepo)
        {
            _puntoVentaRepo = puntoVentaRepo;
        }

        public async Task<VentaGuardarResponseDTO> ProcesarVentaAsync(VentaGuardarRequestDTO request)
        {
            return await _puntoVentaRepo.GuardarVentaCompleta(request);
        }
    }
}
