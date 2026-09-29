using BE.Auditoria;

namespace DA.Repositorio.Repositorio_Auditoria;

public interface IAuditoria
{
    Task RegistrarAuditoria(
        BE_Auditoria auditoria);

    Task<List<BE_Auditoria>> BuscarAuditoria(
        BE_Auditoria filtro);

    Task<BE_Auditoria?> ObtenerAuditoria(
        long idAuditoria);
}
