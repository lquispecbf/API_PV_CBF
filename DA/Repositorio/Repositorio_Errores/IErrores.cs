using BE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA.Repositorio.Repositorio_Errores
{
    public interface IErrores
    {
        Task Insertar_Exception(Exception ex);
    }
}
