using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

// El botón "Verificar" de la Torre corre el MISMO verificador que usa GitHub Classroom
// (la carpeta verificador/ del repo) sobre el proyecto del alumno, y además mira su git.
public class VerificadorService(
    IContenidoRepository contenido,
    IProgresoService progresoService,
    IProgresoRepository progreso,
    IInsigniaService insignias,
    IWebHostEnvironment entorno,
    ILogger<VerificadorService> logger) : IVerificadorService
{
    // Una verificación a la vez por carpeta: compilar dos veces el mismo proyecto en paralelo falla
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Candados = new();

    private static readonly JsonSerializerOptions OpcionesJson = new() { PropertyNameCaseInsensitive = true };

    // La carpeta del repo: la que tiene AeroTaller/ y verificador/ (por defecto, la que contiene a torre-de-control/)
    public string RutaPorDefecto => Path.GetFullPath(Path.Combine(entorno.ContentRootPath, ".."));

    public ResultadoVerificacion Verificar(int alumnoId, string desafioId)
    {
        Alumno alumno = progresoService.ObtenerAlumno(alumnoId);
        Desafio desafio = contenido.ObtenerDesafio(desafioId)
            ?? throw new KeyNotFoundException($"No existe el desafío {desafioId}");
        if (desafio.Numero is null)
            throw new SolicitudInvalidaException("Este paso no tiene verificación automática.");

        string repo = RutaRepo(alumno);
        string carpetaAeroTaller = Path.Combine(repo, "AeroTaller");
        if (!File.Exists(Path.Combine(carpetaAeroTaller, "AeroTaller.csproj")))
            return new ResultadoVerificacion(false, false,
                $"No se encontró AeroTaller/AeroTaller.csproj en {repo}. Revisar la carpeta del repositorio en la tarjeta de embarque.", [], null, []);

        string verificador = Path.Combine(RutaPorDefecto, "verificador");
        if (!File.Exists(Path.Combine(verificador, "Verificador.csproj")))
            return new ResultadoVerificacion(false, false, $"No se encontró el verificador en {verificador}.", [], null, []);

        SemaphoreSlim candado = Candados.GetOrAdd(carpetaAeroTaller, _ => new SemaphoreSlim(1, 1));
        if (!candado.Wait(TimeSpan.FromSeconds(1)))
            throw new SolicitudInvalidaException("Ya hay una verificación en curso sobre este proyecto. Debe esperarse a que termine.");

        try
        {
            (bool ok, string salida) = Correr("dotnet", RutaPorDefecto, TimeSpan.FromMinutes(5),
                "run", "--project", verificador, "--", desafio.Numero.Value.ToString(), "--json", "--proyecto", carpetaAeroTaller);

            // La salida de dotnet run puede traer líneas antes del JSON: tomamos la última que empieza con {
            string? json = salida.Split('\n').LastOrDefault(l => l.TrimStart().StartsWith('{'));
            if (json is null)
            {
                logger.LogWarning("El verificador no devolvió JSON: {Salida}", salida);
                return new ResultadoVerificacion(false, false, "El verificador no pudo ejecutarse:\n" + Ultimas(salida), [], EstadoRepo(repo), []);
            }

            var resultado = JsonSerializer.Deserialize<SalidaVerificador>(json, OpcionesJson)!;
            if (resultado.Error is not null)
                return new ResultadoVerificacion(false, false, resultado.Error, [], EstadoRepo(repo), []);

            var chequeos = resultado.Desafios.SelectMany(d => d.Chequeos).Select(c => new ResultadoCheck(c.Nombre, c.Ok, c.Detalle)).ToList();
            bool aprobado = resultado.Desafios.Count > 0 && resultado.Desafios.All(d => d.Ok);
            progreso.GuardarVerificacion(alumnoId, desafioId, aprobado, chequeos.Count, chequeos.Count(c => !c.Ok));

            EstadoRepoGit estadoRepo = EstadoRepo(repo);
            if (estadoRepo.EsRepo) progreso.GuardarEstadoRepo(alumnoId, desafioId, estadoRepo.SinCommitear, estadoRepo.SinPushear);

            return new ResultadoVerificacion(true, aprobado, null, chequeos, estadoRepo, insignias.Evaluar(alumnoId));
        }
        finally
        {
            candado.Release();
        }
    }

    public EstadoRepoGit EstadoRepo(int alumnoId) => EstadoRepo(RutaRepo(progresoService.ObtenerAlumno(alumnoId)));

    private string RutaRepo(Alumno alumno) =>
        string.IsNullOrWhiteSpace(alumno.RutaProyecto) ? RutaPorDefecto : alumno.RutaProyecto;

    private static EstadoRepoGit EstadoRepo(string ruta)
    {
        if (!Directory.Exists(ruta))
            return new EstadoRepoGit(false, null, false, 0, 0, [], $"No existe la carpeta {ruta}.");

        (bool ok, string salida) = Correr("git", ruta, TimeSpan.FromSeconds(10), "-C", ruta, "status", "--porcelain=v1", "--branch");
        if (!ok)
            return new EstadoRepoGit(false, null, false, 0, 0, [], salida.Contains("not a git repository")
                ? "La carpeta no es un repositorio git (el repositorio de Classroom debe clonarse)."
                : salida.Trim());

        string[] lineas = salida.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Primera línea: "## main...origin/main [ahead 2]"  ·  "## main" (sin remoto)  ·  "## No commits yet on main"
        string cabecera = lineas.FirstOrDefault(l => l.StartsWith("## ")) ?? "## ?";
        int cambios = lineas.Count(l => !l.StartsWith("## "));
        bool tieneRemoto = cabecera.Contains("...");
        string rama = cabecera[3..].Split("...")[0].Replace("No commits yet on ", "").Trim();
        Match adelante = Regex.Match(cabecera, @"ahead (\d+)");
        int sinPushear = adelante.Success ? int.Parse(adelante.Groups[1].Value) : 0;

        (bool hayLog, string log) = Correr("git", ruta, TimeSpan.FromSeconds(10), "-C", ruta, "log", "--oneline", "-5");
        List<string> commits = hayLog ? log.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList() : [];

        string mensaje = (cambios, sinPushear, tieneRemoto) switch
        {
            (_, _, false) when commits.Count == 0 => "Todavía no hay commits.",
            (_, _, false) => $"La rama no tiene remoto: git push -u origin {rama}",
            (0, 0, _) => "Todo registrado en commits y subido con push.",
            ( > 0, _, _) => $"Hay {cambios} archivo(s) con cambios sin commit.",
            _ => $"Hay {sinPushear} commit(s) sin push."
        };
        if (!tieneRemoto && commits.Count > 0) sinPushear = Math.Max(sinPushear, 1);

        return new EstadoRepoGit(true, rama, tieneRemoto, cambios, sinPushear, commits, mensaje);
    }

    // Corre un programa con los argumentos separados (ArgumentList): nada de armar un string de comando a mano
    private static (bool Ok, string Salida) Correr(string programa, string carpeta, TimeSpan limite, params string[] argumentos)
    {
        var inicio = new ProcessStartInfo(programa)
        {
            WorkingDirectory = carpeta,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (string argumento in argumentos) inicio.ArgumentList.Add(argumento);
        inicio.Environment["DOTNET_NOLOGO"] = "1";

        try
        {
            using Process proceso = Process.Start(inicio)!;
            Task<string> salida = proceso.StandardOutput.ReadToEndAsync();
            Task<string> error = proceso.StandardError.ReadToEndAsync();
            if (!proceso.WaitForExit(limite))
            {
                proceso.Kill(entireProcessTree: true);
                return (false, $"{programa} tardó más de {limite.TotalMinutes:0.#} minutos.");
            }
            // El verificador devuelve 1 si algún chequeo falla: su salida se usa igual
            return (programa == "dotnet" || proceso.ExitCode == 0, salida.Result + error.Result);
        }
        catch (Win32Exception)
        {
            return (false, $"No se encontró el comando {programa} en esta computadora.");
        }
    }

    private static string Ultimas(string texto) =>
        string.Join("\n", texto.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).TakeLast(10));

    private record SalidaVerificador(string? Error, List<DesafioVerificado> Desafios)
    {
        public List<DesafioVerificado> Desafios { get; init; } = Desafios ?? [];
    }

    private record DesafioVerificado(int Numero, string Titulo, bool Ok, List<ChequeoVerificado> Chequeos);

    private record ChequeoVerificado(string Nombre, bool Ok, string Detalle);
}
