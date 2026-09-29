using BE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA.Repositorio
{
    public interface ILogin
    {
        Task<List<BE_Usuario>> Validar_Login (BE_Usuario obj_Usuario);
        Task<BE_Usuario> ControlIntentosLogin(int id, bool loginCorrecto);
    }
}
