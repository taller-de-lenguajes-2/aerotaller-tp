using System.Globalization;

// AeroTaller — proyecto base del TP integrador.
// Se completa desafío por desafío siguiendo la Torre de Control (dotnet run --project torre-de-control).

// Fechas y moneda en castellano, en cualquier computadora.
// Decimales: un <input type="number"> SIEMPRE envía "18.5" (con punto), y con es-AR sin modificar
// el model binding lo leería como 185. Por eso los números (no la moneda) usan punto decimal.
var cultura = (CultureInfo)CultureInfo.GetCultureInfo("es-AR").Clone();
cultura.NumberFormat.NumberDecimalSeparator = ".";
cultura.NumberFormat.NumberGroupSeparator = "";
CultureInfo.DefaultThreadCurrentCulture = cultura;

var builder = WebApplication.CreateBuilder(args);

// Desafío 1: leer la cadena de conexión y crear la base si no existe
// Desafío 3 y 7: registrar repositorios (y después servicios) en el contenedor
// Desafío 8: Serilog
// Desafío 9: sesión

// MVC: controladores que devuelven vistas
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();   // sirve lo que está en wwwroot (el CSS)

// Ruteo convencional: /Vuelos/Detalle/3 → VuelosController.Detalle(3)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
