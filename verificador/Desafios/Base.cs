using System.Text.RegularExpressions;

// Datos y servicios que requiere la verificación de un desafío
public class Contexto(Proyecto proyecto, string carpetaTemporal, bool revisarGit)
{
    public Proyecto Proyecto { get; } = proyecto;
    public bool RevisarGit { get; } = revisarGit;

    // Cada servidor usa su propia subcarpeta temporal (de este modo, cada desafío comienza con una base nueva)
    public string NuevaCarpeta()
    {
        string carpeta = Path.Combine(carpetaTemporal, Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(carpeta);
        return carpeta;
    }

    public Servidor Arrancar(Dictionary<string, string>? extra = null) => Servidor.Arrancar(Proyecto, NuevaCarpeta(), extra: extra);
}

public abstract class Desafio
{
    public abstract int Numero { get; }
    public abstract string Titulo { get; }
    public abstract void Verificar(Contexto ctx, Informe informe);

    // Datos de ejemplo de aerotaller.sql que usan las pruebas
    protected const string DniAna = "30111222";      // reservas en AT0999, AT1001 (3A), AT1002
    protected const string DniBruno = "28999000";    // AT1001 3B
    protected const string DniDiego = "33777888";
    protected const string DniElena = "40123123";    // AT2010 7D, AT0500 1D
    protected const string DniFacundo = "41222333";
    protected const string DniGabriela = "37555666"; // AT2020 12A
    protected const string DniHernan = "29888111";   // AT3005 2F

    protected static Dictionary<string, string> Reserva(int vueloId, int pasajeroId, string asiento) => new()
    {
        ["VueloId"] = vueloId.ToString(),
        ["PasajeroId"] = pasajeroId.ToString(),
        ["Asiento"] = asiento
    };

    protected static bool Contiene(Respuesta r, string texto) => r.Texto.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected static string Recorte(Respuesta r)
    {
        string texto = Regex.Replace(Regex.Replace(r.Texto, "<script[\\s\\S]*?</script>|<style[\\s\\S]*?</style>", ""), "<[^>]+>", " ");
        texto = Regex.Replace(texto, @"\s+", " ").Trim();
        return texto.Length > 160 ? texto[..160] + "…" : texto;
    }

    protected static string Describir(Respuesta r) =>
        r.EsRedireccion ? $"respondió {r.Codigo} → {r.Ubicacion}" : $"respondió {r.Codigo}: «{Recorte(r)}»";

    // Login automático para las páginas protegidas (desde el desafío 9). Antes del desafío 9 no realiza ninguna acción.
    protected static Navegador Mostrador(Servidor s, string paginaProtegida = "/Reservas")
    {
        var nav = s.NuevoNavegador();
        nav.IniciarSesionSiHaceFalta(paginaProtegida, "mostrador", "vuelos2026");
        return nav;
    }

    protected static Navegador Admin(Servidor s, string paginaProtegida = "/Vuelos/Crear")
    {
        var nav = s.NuevoNavegador();
        nav.IniciarSesionSiHaceFalta(paginaProtegida, "admin", "aero2026");
        return nav;
    }

    // Detecta strings interpolados que construyen SQL: $"SELECT … {x}" o $"""… WHERE Id = {id} """
    protected static List<string> SqlInterpolado(Proyecto p)
    {
        var hallazgos = new List<string>();
        var literales = new Regex(@"\$+@?""""""[\s\S]*?""""""|\$@?""(?:[^""\\\n]|\\.)*""|@\$""(?:[^""]|"""")*""");
        var sql = new Regex(@"\b(SELECT|INSERT\s+INTO|UPDATE|DELETE\s+FROM)\b", RegexOptions.IgnoreCase);
        foreach ((string ruta, string texto) in p.Cs)
        {
            foreach (Match m in literales.Matches(Proyecto.SinComentarios(texto)))
            {
                if (sql.IsMatch(m.Value) && Regex.IsMatch(m.Value, @"(?<!\{)\{[^{}]+\}"))
                    hallazgos.Add($"{ruta}: {Regex.Replace(m.Value, @"\s+", " ")[..Math.Min(70, Regex.Replace(m.Value, @"\s+", " ").Length)]}…");
            }
        }
        return hallazgos;
    }

    protected static (bool, string) Resultado(bool ok, string siFalla, string siPasa = "") => (ok, ok ? siPasa : siFalla);
}
