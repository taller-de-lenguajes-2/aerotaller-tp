// El CONTENIDO del tutorial: se lee de Contenido/desafios/<id>/desafio.json (+ contenido.md).
// No está en la base: así el profe lo edita con cualquier editor y recarga la página.

public class Desafio
{
    public string Id { get; set; } = "";            // nombre de la carpeta: "02-repositorios"
    public int Orden { get; set; }
    public string Codigo { get; set; } = "";        // tres letras, como un aeropuerto: "REP"
    public int? Numero { get; set; }                 // número de desafío para el verificador (null en checkpoints)
    public string Titulo { get; set; } = "";
    public string Subtitulo { get; set; } = "";
    public string Tipo { get; set; } = TiposDesafio.Desafio;
    public int MinutosEstimados { get; set; }
    public int? MinutosLimite { get; set; }          // solo el simulacro
    public List<TemaRef> Temas { get; set; } = [];
    public string? Insignia { get; set; }            // la insignia que se gana al completarlo
    public List<Tarea> Tareas { get; set; } = [];
    public List<Pregunta> Quiz { get; set; } = [];
    public List<Lectura> Lecturas { get; set; } = [];
    public Commit? Commit { get; set; }
    public List<string> Herramientas { get; set; } = [];   // paneles extra de la página: "base" (crear aerotaller.db)

    // contenido.md, tal cual (lo dibuja el navegador)
    public string Markdown { get; set; } = "";
}

public static class TiposDesafio
{
    public const string Desafio = "desafio";
    public const string Checkpoint = "checkpoint";
    public const string Simulacro = "simulacro";
}

public record TemaRef(string Numero, string Titulo, string Url);

public class Tarea
{
    public string Id { get; set; } = "";
    public string Texto { get; set; } = "";
    public string? Ayuda { get; set; }
}

public class Pregunta
{
    public string Id { get; set; } = "";
    public string Texto { get; set; } = "";
    public string? Codigo { get; set; }              // un fragmento de código para la pregunta (opcional)
    public List<Opcion> Opciones { get; set; } = [];
    public string Correcta { get; set; } = "";       // Id de la opción correcta (NUNCA se manda al navegador)
    public string Pista { get; set; } = "";          // se muestra si contesta mal
    public string Explicacion { get; set; } = "";    // se muestra si contesta bien
    public List<string> Etiquetas { get; set; } = []; // "depurador" = sale de mirar el debugger
}

public record Opcion(string Id, string Texto);

public class Lectura
{
    public string Id { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Url { get; set; } = "";
    public string Fuente { get; set; } = "";
    public string Pregunta { get; set; } = "";       // qué buscar en la lectura
}

public class Commit
{
    public string Mensaje { get; set; } = "";
    public string? Detalle { get; set; }
}

public class Insignia
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Icono { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public ReglaInsignia Regla { get; set; } = new();
}

// Cuándo se gana una insignia. Los tipos los evalúa InsigniaService.
public class ReglaInsignia
{
    public string Tipo { get; set; } = "";
    public string? DesafioId { get; set; }
    public int Cantidad { get; set; } = 1;
}
