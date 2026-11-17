using System.Net;
using System.Text.RegularExpressions;

public record Respuesta(int Codigo, string Html, string? Ubicacion, HttpResponseMessage Mensaje)
{
    // El HTML con las entidades decodificadas (&#xF3; → ó) para poder buscar texto en castellano
    public string Texto { get; } = WebUtility.HtmlDecode(Html);

    public bool EsRedireccion => Codigo is 301 or 302 or 303 or 307 or 308;

    public int CantidadErroresDeCampo => Regex.Matches(Html, @"class=""[^""]*\bfield-validation-error\b").Count;

    public bool TieneErrorEn(string campo) =>
        Regex.IsMatch(Html, $@"class=""[^""]*\bfield-validation-error\b[^""]*""[^>]*data-valmsg-for=""{campo}""")
        || Regex.IsMatch(Html, $@"data-valmsg-for=""{campo}""[^>]*class=""[^""]*\bfield-validation-error\b");

    public bool TieneResumenDeErrores => Html.Contains("validation-summary-errors");

    public string Token() =>
        Regex.Match(Html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
}

// Un "navegador" mínimo: guarda cookies (sesión, antiforgery) y no sigue redirecciones automáticamente (para poder inspeccionarlas).
public sealed class Navegador : IDisposable
{
    private readonly HttpClient http;
    public CookieContainer Cookies { get; } = new();
    public string Url { get; }

    public Navegador(string url)
    {
        Url = url;
        http = new HttpClient(new HttpClientHandler { CookieContainer = Cookies, AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(url),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public Respuesta Get(string ruta) => Enviar(new HttpRequestMessage(HttpMethod.Get, ruta));

    public Respuesta Post(string ruta, Dictionary<string, string> campos, string? token)
    {
        var datos = new Dictionary<string, string>(campos);
        if (token is not null) datos["__RequestVerificationToken"] = token;
        return Enviar(new HttpRequestMessage(HttpMethod.Post, ruta) { Content = new FormUrlEncodedContent(datos) });
    }

    // Solicita la página del formulario (para obtener el token antiforgery) y envía el POST
    public Respuesta EnviarFormulario(string paginaFormulario, string accion, Dictionary<string, string> campos)
    {
        Respuesta formulario = Get(paginaFormulario);
        if (formulario.EsRedireccion)
            throw new VerificacionAbortada($"GET {paginaFormulario} redirige a {formulario.Ubicacion}: posiblemente falta iniciar sesión.");
        return Post(accion, campos, formulario.Token());
    }

    // Antes del desafío 9 no hay login; después, las páginas protegidas redirigen a /Login.
    // Si la página redirige al login, inicia sesión con el usuario indicado.
    public void IniciarSesionSiHaceFalta(string paginaProtegida, string usuario, string clave)
    {
        Respuesta r = Get(paginaProtegida);
        if (!r.EsRedireccion || r.Ubicacion is null || !r.Ubicacion.Contains("/Login", StringComparison.OrdinalIgnoreCase)) return;
        IniciarSesion(usuario, clave);
    }

    public Respuesta IniciarSesion(string usuario, string clave)
    {
        Respuesta login = Get("/Login");
        return Post("/Login", new() { ["NombreUsuario"] = usuario, ["Clave"] = clave }, login.Token());
    }

    public string? Cookie(string nombre) => Cookies.GetCookies(new Uri(Url)).FirstOrDefault(c => c.Name == nombre)?.Value;

    private Respuesta Enviar(HttpRequestMessage pedido)
    {
        HttpResponseMessage respuesta = http.Send(pedido);
        string html = respuesta.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        string? ubicacion = respuesta.Headers.Location?.ToString();
        return new Respuesta((int)respuesta.StatusCode, html, ubicacion, respuesta);
    }

    public void Dispose() => http.Dispose();
}
