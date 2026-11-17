using System.Text.Json;

// Verificador del TP AeroTaller
//
//   dotnet run --project verificador -- 3          revisa el desafío 3
//   dotnet run --project verificador -- 0-5        revisa del 0 al 5
//   dotnet run --project verificador -- todos      revisa todo
//   opciones: --json (salida para la Torre de Control)  --proyecto <carpeta AeroTaller>  --sin-git
//
// Compila el proyecto, lo ejecuta con una base TEMPORAL (el aerotaller.db del proyecto no se modifica) y prueba cada desafío
// del mismo modo que una persona: navega, envía formularios y consulta qué se registró en la base y en el log.

Desafio[] todos =
[
    new Desafio0(), new Desafio1(), new Desafio2(), new Desafio3(), new Desafio4(), new Desafio5(),
    new Desafio6(), new Desafio7(), new Desafio8(), new Desafio9(), new Desafio10()
];

bool json = args.Contains("--json");
bool revisarGit = !args.Contains("--sin-git");
string? carpetaProyecto = Opcion(args, "--proyecto") ?? Proyecto.Buscar(Directory.GetCurrentDirectory());
string? seleccion = args.FirstOrDefault(a => !a.StartsWith("--") && a != Opcion(args, "--proyecto"));

List<Desafio> elegidos;
try
{
    elegidos = Elegir(seleccion, todos);
}
catch (FormatException)
{
    Console.Error.WriteLine("Uso: dotnet run --project verificador -- <desafío | desde-hasta | todos> [--json] [--proyecto <carpeta>]");
    return 2;
}

if (carpetaProyecto is null || !File.Exists(Path.Combine(carpetaProyecto, "AeroTaller.csproj")))
{
    Salir(json, "No se encontró AeroTaller/AeroTaller.csproj. El verificador debe ejecutarse desde la carpeta del repositorio.");
    return 2;
}

var proyecto = new Proyecto(carpetaProyecto);
string temporal = Path.Combine(Path.GetTempPath(), "aerotaller-verificador-" + Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(temporal);
var resultados = new List<ResultadoDesafio>();

try
{
    if (!json) Console.WriteLine($"Verificador AeroTaller · {proyecto.Carpeta}\n  Compilando…");
    (bool compila, string errores) = proyecto.Compilar();

    foreach (Desafio desafio in elegidos)
    {
        var informe = new Informe();
        if (!compila)
        {
            informe.Agregar("El proyecto compila", false, "dotnet build falla:\n" + errores);
        }
        else
        {
            if (!json) Console.WriteLine($"  Desafío {desafio.Numero} · {desafio.Titulo}…");
            try
            {
                desafio.Verificar(new Contexto(proyecto, temporal, revisarGit), informe);
            }
            catch (VerificacionAbortada ex)
            {
                informe.Agregar("La aplicación se pudo iniciar para las pruebas", false, ex.Message);
            }
            catch (Exception ex)
            {
                informe.Agregar("Verificación interrumpida", false, $"{ex.GetType().Name}: {ex.Message}");
            }
        }
        resultados.Add(new ResultadoDesafio(desafio.Numero, desafio.Titulo, informe.Chequeos));
    }
}
finally
{
    try { Directory.Delete(temporal, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
}

if (json)
{
    var opciones = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        proyecto = proyecto.Carpeta,
        desafios = resultados.Select(r => new { r.Numero, r.Titulo, r.Ok, chequeos = r.Chequeos })
    }, opciones));
}
else
{
    Imprimir(resultados);
}
return resultados.All(r => r.Ok) ? 0 : 1;


static void Imprimir(List<ResultadoDesafio> resultados)
{
    Console.WriteLine();
    foreach (ResultadoDesafio r in resultados)
    {
        Color(r.Ok ? ConsoleColor.Green : ConsoleColor.Red, $"{(r.Ok ? "✔" : "✘")} Desafío {r.Numero} · {r.Titulo}");
        Console.WriteLine($"  {r.Chequeos.Count(c => c.Ok)}/{r.Chequeos.Count}");
        foreach (Chequeo c in r.Chequeos)
        {
            Color(c.Ok ? ConsoleColor.DarkGreen : ConsoleColor.Red, $"   {(c.Ok ? "✔" : "✘")} {c.Nombre}");
            if (!c.Ok && c.Detalle.Length > 0)
                foreach (string linea in c.Detalle.Split('\n'))
                    Color(ConsoleColor.DarkGray, "       " + linea);
        }
        Console.WriteLine();
    }
    int ok = resultados.Count(r => r.Ok);
    Color(ok == resultados.Count ? ConsoleColor.Green : ConsoleColor.Yellow, $"Resultado: {ok}/{resultados.Count} desafíos aprobados.");
}

static void Color(ConsoleColor color, string texto)
{
    var anterior = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(texto);
    Console.ForegroundColor = anterior;
}

static List<Desafio> Elegir(string? seleccion, Desafio[] todos)
{
    if (seleccion is null || seleccion == "todos") return todos.ToList();
    if (seleccion.Contains('-'))
    {
        string[] partes = seleccion.Split('-');
        int desde = int.Parse(partes[0]), hasta = int.Parse(partes[1]);
        return todos.Where(d => d.Numero >= desde && d.Numero <= hasta).ToList();
    }
    int numero = int.Parse(seleccion);
    var elegido = todos.FirstOrDefault(d => d.Numero == numero) ?? throw new FormatException();
    return [elegido];
}

static string? Opcion(string[] args, string nombre)
{
    int i = Array.IndexOf(args, nombre);
    return i >= 0 && i < args.Length - 1 ? args[i + 1] : null;
}

static void Salir(bool json, string mensaje)
{
    if (json) Console.WriteLine(JsonSerializer.Serialize(new { error = mensaje }));
    else Console.Error.WriteLine(mensaje);
}
