using System.Globalization;
using Microsoft.Data.Sqlite;

// Torre de Control: el tutorial interactivo del TP AeroTaller.
// Backend: Web API (temas 06, 08, 09, 14, 16, 17) · Frontend: Vue 3 en wwwroot/ (sin compilar nada).
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("es-AR");

var builder = WebApplication.CreateBuilder(args);

string cadenaConexion = builder.Configuration.GetConnectionString("Progreso")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Progreso' en appsettings.json");
AsegurarBase(cadenaConexion);

builder.Services.AddSingleton(new ConexionDb(cadenaConexion));

// Repositorios
builder.Services.AddScoped<IContenidoRepository, ContenidoRepository>();
builder.Services.AddScoped<IAlumnoRepository, AlumnoRepository>();
builder.Services.AddScoped<IProgresoRepository, ProgresoRepository>();
builder.Services.AddScoped<IIntentoQuizRepository, IntentoQuizRepository>();
builder.Services.AddScoped<IBitacoraRepository, BitacoraRepository>();
builder.Services.AddScoped<IInsigniaRepository, InsigniaRepository>();

// Servicios
builder.Services.AddScoped<IInsigniaService, InsigniaService>();
builder.Services.AddScoped<IProgresoService, ProgresoService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IVerificadorService, VerificadorService>();
builder.Services.AddScoped<IBaseAeroTallerService, BaseAeroTallerService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorGlobal>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseDefaultFiles();          // "/" → wwwroot/index.html
// el frontend (Vue) y sus librerías; no-cache = el navegador revalida siempre (si se edita un .js o .css, se ve al recargar)
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = archivo => archivo.Context.Response.Headers.CacheControl = "no-cache"
});
app.MapControllers();           // la API: /api/...

app.Logger.LogInformation("Torre de Control lista. Progreso en {Archivo}",
    Path.GetFullPath(new SqliteConnectionStringBuilder(cadenaConexion).DataSource));
app.Run();

// progreso.sql usa CREATE TABLE IF NOT EXISTS: se puede correr en cada arranque sin perder nada
static void AsegurarBase(string cadenaConexion)
{
    using var conexion = new SqliteConnection(cadenaConexion);
    conexion.Open();
    using var comando = conexion.CreateCommand();
    comando.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "progreso.sql"));
    comando.ExecuteNonQuery();
}
