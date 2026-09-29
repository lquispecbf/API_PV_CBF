namespace API.Infrastructure.Auditoria;

public static class AnalizadorUserAgent
{
    public static (string navegador, string version, string so) Analizar(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return ("Desconocido", "", "Desconocido");

        string navegador = "Desconocido";
        string version = "";
        string so = "Desconocido";

        // Sistema Operativo
        if (userAgent.Contains("Windows NT 10.0")) so = "Windows 10/11";
        else if (userAgent.Contains("Windows NT 6.3")) so = "Windows 8.1";
        else if (userAgent.Contains("Windows NT 6.2")) so = "Windows 8";
        else if (userAgent.Contains("Windows NT 6.1")) so = "Windows 7";
        else if (userAgent.Contains("Windows")) so = "Windows";
        else if (userAgent.Contains("Android")) so = "Android";
        else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) so = "iOS";
        else if (userAgent.Contains("Mac OS X")) so = "macOS";
        else if (userAgent.Contains("Linux")) so = "Linux";

        // Navegador
        if (userAgent.Contains("Edg/"))
        {
            navegador = "Edge";
            version = ExtraerVersion(userAgent, "Edg/");
        }
        else if (userAgent.Contains("Chrome/") && !userAgent.Contains("Edg/"))
        {
            navegador = "Chrome";
            version = ExtraerVersion(userAgent, "Chrome/");
        }
        else if (userAgent.Contains("Firefox/"))
        {
            navegador = "Firefox";
            version = ExtraerVersion(userAgent, "Firefox/");
        }
        else if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome/"))
        {
            navegador = "Safari";
            version = ExtraerVersion(userAgent, "Version/");
        }

        return (navegador, version, so);
    }

    private static string ExtraerVersion(string userAgent, string prefijo)
    {
        try
        {
            int inicio = userAgent.IndexOf(prefijo, StringComparison.Ordinal) + prefijo.Length;
            int fin = userAgent.IndexOf(' ', inicio);
            if (fin == -1) fin = userAgent.Length;
            return userAgent.Substring(inicio, fin - inicio);
        }
        catch
        {
            return "";
        }
    }
}
