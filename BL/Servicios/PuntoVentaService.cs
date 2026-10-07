using BE.PuntoVenta;
using DA.Repositorio.Repositorio_PuntoVenta;

namespace BL.Servicios
{
    public interface IPuntoVentaService
    {
        Task<VentaGuardarResponseDTO> ProcesarVentaAsync(VentaGuardarRequestDTO request);
        Task<VentaDigemidDTO?> ObtenerDigemidPorDocEntryAsync(int docEntry);
        Task<(bool Exito, string Mensaje)> RegularizarDigemidAsync(VentaDigemidRegularizarRequestDTO request);
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

        public async Task<VentaDigemidDTO?> ObtenerDigemidPorDocEntryAsync(int docEntry)
        {
            return await _puntoVentaRepo.ObtenerImagenDigemidPorDocEntry(docEntry);
        }

        public async Task<(bool Exito, string Mensaje)> RegularizarDigemidAsync(VentaDigemidRegularizarRequestDTO request)
        {
            return await _puntoVentaRepo.RegularizarImagenDigemid(request);
        }
    }
}
