using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

// Levanta la app del alumno (el .dll ya compilado) en un puerto libre, con la configuración que le pasemos.
// La base va a una carpeta temporal: nunca toca el aerotaller.db del alumno.
public sealed class Servidor : IDisposable
{
    private readonly Process proceso;
    private readonly ConcurrentQueue<string> salida = new();

    public string Url { get; }
    public string RutaBase { get; }
    public DateTime Inicio { get; } = DateTime.Now;

    public string Salida => string.Join("\n", salida);

    private Servidor(Proyecto proyecto, string rutaBase, string cadena, Dictionary<string, string> extra)
    {
        RutaBase = rutaBase;
        int puerto = PuertoLibre();
        Url = $"http://127.0.0.1:{puerto}";

        var inicio = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = proyecto.Carpeta,      // logs/ y aerotaller.db relativos quedan en la carpeta del proyecto
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        inicio.ArgumentList.Add(proyecto.Dll);
        inicio.ArgumentList.Add($"--urls={Url}");
        inicio.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        inicio.Environment["ConnectionStrings__AeroTaller"] = cadena;
        inicio.Environment["DOTNET_NOLOGO"] = "1";
        foreach ((string clave, string valor) in extra)
            inicio.Environment[clave.Replace(":", "__")] = valor;

        proceso = Process.Start(inicio)!;
        proceso.OutputDataReceived += (_, e) => { if (e.Data is not null) salida.Enqueue(e.Data); };
        proceso.ErrorDataReceived += (_, e) => { if (e.Data is not null) salida.Enqueue(e.Data); };
        proceso.BeginOutputReadLine();
        proceso.BeginErrorReadLine();
    }

    // Arranca y espera a que responda. Si no arranca, aborta el desafío con lo que imprimió la app.
    public static Servidor Arrancar(Proyecto proyecto, string carpetaTemporal, string? cadena = null,
        Dictionary<string, string>? extra = null, string nombreBase = "aerotaller.db")
    {
        string rutaBase = Path.Combine(carpetaTemporal, nombreBase);
        cadena ??= $"Data Source={rutaBase};Foreign Keys=True";
        var servidor = new Servidor(proyecto, rutaBase, cadena, extra ?? []);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var reloj = Stopwatch.StartNew();
        while (reloj.Elapsed < TimeSpan.FromSeconds(40))
        {
            if (servidor.proceso.HasExited)
            {
                Thread.Sleep(300);
                throw new VerificacionAbortada("La app se cerró al arrancar:\n" + Ultimas(servidor.Salida, 12));
            }
            try
            {
                http.GetAsync(servidor.Url + "/").GetAwaiter().GetResult();
                return servidor;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                Thread.Sleep(250);
            }
        }
        servidor.Dispose();
        throw new VerificacionAbortada("La app no respondió en 40 segundos:\n" + Ultimas(servidor.Salida, 12));
    }

    public Navegador NuevoNavegador() => new(Url);

    public void Dispose()
    {
        try
        {
            if (!proceso.HasExited)
            {
                proceso.Kill(entireProcessTree: true);
                proceso.WaitForExit(5000);
            }
        }
        catch (InvalidOperationException) { }
        proceso.Dispose();
        // Microsoft.Data.Sqlite deja conexiones en el pool del proceso del alumno; al matarlo, se liberan
    }

    public static string Ultimas(string texto, int cantidad) =>
        string.Join("\n", texto.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).TakeLast(cantidad));

    private static int PuertoLibre()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int puerto = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return puerto;
    }
}
