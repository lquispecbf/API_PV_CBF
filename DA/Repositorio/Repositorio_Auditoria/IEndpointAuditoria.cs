using BE;
using BE.Auditoria;

namespace DA.Repositorio.Repositorio_Auditoria;

public interface IEndpointAuditoria
{
    Task<BE_EndpointAuditoria?> ObtenerEndpoint(
        string controller,
        string action,
        string? origen = "INTRANET");

    Task<List<BE_EndpointAuditoria>> Buscar_EndpointAuditoria(
        BE_EndpointAuditoria filtro);

    Task<BE_EndpointAuditoria?> Obtener_EndpointAuditoria(
        long idEndpoint);

    Task<BE_RespuestaBD> Insertar_EndpointAuditoria(
        BE_EndpointAuditoria obj);

    Task<BE_RespuestaBD> Actualizar_EndpointAuditoria(
        BE_EndpointAuditoria obj);

    Task<BE_RespuestaBD> Eliminar_EndpointAuditoria(
        BE_EndpointAuditoria obj);

    Task<List<BE_TipoAccion>> Obtener_Combo_TipoAccion();
}
