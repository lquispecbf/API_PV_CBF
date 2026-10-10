namespace BE.Seguridad
{
    public class RenovarTokenSesionRequestDTO
    {
        public int IdUsuario { get; set; }
        public string? Usuario { get; set; }
    }

    public class RenovarTokenSesionResponseDTO
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public System.DateTime Expiration { get; set; }
        public string? Mensaje { get; set; }
        public int? CodigoVendedorSap { get; set; }
    }
}
