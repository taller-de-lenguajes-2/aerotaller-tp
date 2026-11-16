using System.Text.Json;

// Lee los desafíos desde archivos (no desde la base).
// Cada desafío es una carpeta en Contenido/desafios/ con desafio.json y contenido.md.
// Se lee en cada pedido: al editar un .md, basta con recargar la página.
public class ContenidoRepository(IWebHostEnvironment entorno, ILogger<ContenidoRepository> logger) : IContenidoRepository
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private string Carpeta => Path.Combine(entorno.ContentRootPath, "Contenido");

    public List<Desafio> ListarDesafios()
    {
        var desafios = new List<Desafio>();
        string carpetaDesafios = Path.Combine(Carpeta, "desafios");
        if (!Directory.Exists(carpetaDesafios)) return desafios;

        foreach (string carpeta in Directory.GetDirectories(carpetaDesafios))
        {
            Desafio? desafio = LeerDesafio(carpeta);
            if (desafio is not null) desafios.Add(desafio);
        }
        return desafios.OrderBy(d => d.Orden).ToList();
    }

    public Desafio? ObtenerDesafio(string id)
    {
        // El id es el nombre de la carpeta: se valida para no salir de Contenido/ con "../"
        if (id.Contains("..") || id.Contains('/') || id.Contains('\\')) return null;

        string carpeta = Path.Combine(Carpeta, "desafios", id);
        return Directory.Exists(carpeta) ? LeerDesafio(carpeta) : null;
    }

    public List<Insignia> ListarInsignias()
    {
        string archivo = Path.Combine(Carpeta, "insignias.json");
        if (!File.Exists(archivo)) return [];
        return JsonSerializer.Deserialize<List<Insignia>>(File.ReadAllText(archivo), OpcionesJson) ?? [];
    }

    public string? ObtenerPagina(string nombre)
    {
        if (!nombre.All(c => char.IsLetterOrDigit(c) || c == '-')) return null;
        string archivo = Path.Combine(Carpeta, nombre + ".md");
        return File.Exists(archivo) ? File.ReadAllText(archivo) : null;
    }

    private Desafio? LeerDesafio(string carpeta)
    {
        string json = Path.Combine(carpeta, "desafio.json");
        if (!File.Exists(json)) return null;

        try
        {
            var desafio = JsonSerializer.Deserialize<Desafio>(File.ReadAllText(json), OpcionesJson);
            if (desafio is null) return null;

            desafio.Id = Path.GetFileName(carpeta);
            string md = Path.Combine(carpeta, "contenido.md");
            desafio.Markdown = File.Exists(md) ? File.ReadAllText(md) : "";
            return desafio;
        }
        catch (JsonException ex)
        {
            // Un JSON mal escrito no tumba todo el tutorial: se avisa en el log y se saltea ese desafío
            logger.LogError(ex, "El archivo {Archivo} tiene un error de JSON", json);
            return null;
        }
    }
}
