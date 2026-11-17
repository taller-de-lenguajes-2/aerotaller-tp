using System.Diagnostics;
using System.Text.RegularExpressions;

// El proyecto AeroTaller del alumno: dónde está, si compila, y búsquedas en su código fuente.
public class Proyecto
{
    public string Carpeta { get; }
    public string Csproj => Path.Combine(Carpeta, "AeroTaller.csproj");
    public string Dll => Path.Combine(Carpeta, "bin", "Debug", "net10.0", "AeroTaller.dll");
    public string CarpetaRepo => Path.GetDirectoryName(Carpeta)!;

    private List<(string Ruta, string Texto)>? fuentes;

    public Proyecto(string carpeta) => Carpeta = Path.GetFullPath(carpeta);

    // Busca AeroTaller/AeroTaller.csproj subiendo desde la carpeta actual
    public static string? Buscar(string desde)
    {
        var carpeta = new DirectoryInfo(desde);
        while (carpeta is not null)
        {
            string candidato = Path.Combine(carpeta.FullName, "AeroTaller", "AeroTaller.csproj");
            if (File.Exists(candidato)) return Path.GetDirectoryName(candidato);
            carpeta = carpeta.Parent;
        }
        return null;
    }

    public (bool Ok, string Salida) Compilar()
    {
        (int codigo, string salida) = Comando.Correr("dotnet", Carpeta, TimeSpan.FromMinutes(4),
            "build", Csproj, "-c", "Debug", "-v", "q", "-nologo", "-clp:NoSummary");
        if (codigo == 0) return (true, "");

        // Solo las líneas de error, sin repetir
        var errores = salida.Split('\n')
            .Where(l => l.Contains(": error "))
            .Select(l => Regex.Replace(l.Trim(), @"\s*\[[^\]]*\.csproj\]$", ""))
            .Select(l => l.Replace(Carpeta + Path.DirectorySeparatorChar, ""))
            .Distinct()
            .Take(6);
        string resumen = string.Join("\n", errores);
        return (false, resumen.Length > 0 ? resumen : salida.Trim().Split('\n').LastOrDefault() ?? "");
    }

    // Todo el código C# y Razor del alumno (sin bin/ ni obj/)
    public List<(string Ruta, string Texto)> Fuentes => fuentes ??= Directory
        .EnumerateFiles(Carpeta, "*.*", SearchOption.AllDirectories)
        .Where(r => r.EndsWith(".cs") || r.EndsWith(".cshtml"))
        .Where(r => !EnCarpeta(r, "bin") && !EnCarpeta(r, "obj"))
        .Select(r => (Path.GetRelativePath(Carpeta, r).Replace('\\', '/'), File.ReadAllText(r)))
        .ToList();

    public IEnumerable<(string Ruta, string Texto)> Cs => Fuentes.Where(f => f.Ruta.EndsWith(".cs"));

    public string Leer(string relativo)
    {
        string ruta = Path.Combine(Carpeta, relativo);
        return File.Exists(ruta) ? File.ReadAllText(ruta) : "";
    }

    public bool Existe(string relativo) => File.Exists(Path.Combine(Carpeta, relativo));

    // ¿Algún .cs declara este tipo? (class / record / interface / enum)
    public bool DeclaraTipo(string nombre, string tipo = @"(class|record|interface|enum)") =>
        Cs.Any(f => Regex.IsMatch(f.Texto, $@"\b{tipo}\s+{nombre}\b"));

    public (string Ruta, string Texto)? DondeSeDeclara(string nombre) =>
        Cs.Where(f => Regex.IsMatch(f.Texto, $@"\b(class|record|interface)\s+{nombre}\b")).Select(f => ((string, string)?)f).FirstOrDefault();

    public List<string> Coincidencias(string patron, Func<string, bool>? filtroRuta = null) =>
        Cs.Where(f => filtroRuta is null || filtroRuta(f.Ruta))
          .Where(f => Regex.IsMatch(SinComentarios(f.Texto), patron))
          .Select(f => f.Ruta)
          .ToList();

    // Para que un "new VueloRepository(" en un comentario no se contabilice
    public static string SinComentarios(string codigo) =>
        Regex.Replace(Regex.Replace(codigo, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");

    private bool EnCarpeta(string ruta, string carpeta) =>
        Path.GetRelativePath(Carpeta, ruta).Split(Path.DirectorySeparatorChar, '/').Contains(carpeta);
}

public static class Comando
{
    public static (int Codigo, string Salida) Correr(string programa, string carpeta, TimeSpan limite, params string[] argumentos)
    {
        var inicio = new ProcessStartInfo(programa)
        {
            WorkingDirectory = carpeta,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string a in argumentos) inicio.ArgumentList.Add(a);

        using Process proceso = Process.Start(inicio)!;
        Task<string> salida = proceso.StandardOutput.ReadToEndAsync();
        Task<string> error = proceso.StandardError.ReadToEndAsync();
        if (!proceso.WaitForExit(limite))
        {
            proceso.Kill(entireProcessTree: true);
            return (-1, $"{programa} tardó más de {limite.TotalSeconds} s");
        }
        return (proceso.ExitCode, salida.Result + error.Result);
    }
}
