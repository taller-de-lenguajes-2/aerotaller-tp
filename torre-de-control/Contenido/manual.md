## :route: El viaje de un request (de memoria)

```text
Navegador ──GET /Reservas/Crear──▶ Ruteo {controller}/{action}/{id?}
   ▲                                   │
   │                         [filtros: RequiereLogin…]
   │                                   ▼
   │                       ReservasController.Crear()        ← recibe el request y selecciona la vista
   │                                   │ llama
   │                                   ▼
   │                         IReservaService.Reservar()       ← reglas del negocio
   │                                   │ usa
   │                                   ▼
   │                      IReservaRepository.Insertar()       ← SQL con parámetros
   │                                   │
   │                                   ▼
   │                               SQLite (.db)
   │
   └──── HTML ◀── Razor (Views/Reservas/Crear.cshtml + _Layout) ◀── View(viewModel)
```

| Capa | Responsabilidad | Fuera de su responsabilidad |
|---|---|---|
| Vista | presentación del ViewModel | origen de los datos |
| Controlador | HTTP: formularios, redirecciones, selección de la vista | SQL, reglas del negocio |
| Servicio | reglas del negocio ("¿está libre el asiento?") | HTML, `ModelState` |
| Repositorio | SQL de **una** tabla/entidad | reglas, HTML |

## :layers: Program.cs (el orden importa)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(c => c.ReadFrom.Configuration(builder.Configuration));

string cadena = builder.Configuration.GetConnectionString("AeroTaller")
    ?? throw new InvalidOperationException("Falta la cadena 'AeroTaller'");
CrearBaseSiNoExiste(cadena);

builder.Services.AddSingleton(new ConexionDb(cadena));                     // dato que no cambia
builder.Services.AddScoped<IVueloRepository, VueloRepository>();           // uno por request
builder.Services.AddScoped<IReservaService, ReservaService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromMinutes(20); o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; });
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseSerilogRequestLogging();          // antes del manejador de errores
app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseSession();                        // antes del ruteo
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
```

| Tiempo de vida | Se crea | Ejemplo |
|---|---|---|
| `AddSingleton` | una vez para toda la aplicación | `ConexionDb`, `BaseFalsa` (datos en memoria) |
| `AddScoped` | una por request | repositorios, servicios |
| `AddTransient` | cada vez que se resuelve | objetos livianos sin estado |

:triangle-alert: Un Singleton **no** puede recibir un Scoped (*captive dependency*). Las entidades y los ViewModels **no** se inyectan.

## :database: Repositorio (ADO.NET)

```csharp
public class VueloRepository(ConexionDb conexionDb) : IVueloRepository
{
    private readonly string cadenaConexion = conexionDb.Cadena;

    public Vuelo? Obtener(int id)
    {
        using var conexion = new SqliteConnection(cadenaConexion);   // abrir tarde, cerrar temprano
        conexion.Open();
        using var comando = new SqliteCommand("SELECT Id, Numero FROM Vuelo WHERE Id = @id", conexion);
        comando.Parameters.AddWithValue("@id", id);                    // SIEMPRE parámetros
        using var lector = comando.ExecuteReader();
        return lector.Read() ? new Vuelo { Id = lector.GetInt32(0), Numero = lector.GetString(1) } : null;
    }

    public int Insertar(Vuelo v)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();
        using var comando = new SqliteCommand("INSERT INTO Vuelo (Numero) VALUES (@numero) RETURNING Id", conexion);
        comando.Parameters.AddWithValue("@numero", v.Numero);
        return Convert.ToInt32(comando.ExecuteScalar());
    }
}
```

| Método | Devuelve | Para |
|---|---|---|
| `ExecuteReader()` | un lector fila por fila | `SELECT` de varias columnas |
| `ExecuteScalar()` | el primer valor | `COUNT(*)`, `INSERT … RETURNING Id` |
| `ExecuteNonQuery()` | filas afectadas | `UPDATE`, `DELETE` (`> 0` = existía) |

- `NULL` de la base → `lector.IsDBNull(i) ? null : lector.GetString(i)`
- `null` de C# → `(object?)valor ?? DBNull.Value`
- `LIKE`: los `%` van en el **valor**: `AddWithValue("@texto", $"%{texto}%")`

## :database: SQL de uso frecuente

```sql
SELECT v.Numero, o.Codigo AS Origen, d.Codigo AS Destino          -- JOIN doble con alias
FROM Vuelo v
INNER JOIN Aeropuerto o ON v.OrigenId  = o.Id
INNER JOIN Aeropuerto d ON v.DestinoId = d.Id;

SELECT v.Numero, COUNT(r.Id) AS Ocupados                           -- LEFT JOIN + GROUP BY
FROM Vuelo v
LEFT JOIN Reserva r ON r.VueloId = v.Id AND r.Estado = 'Confirmada' -- el filtro va en el ON
GROUP BY v.Id, v.Numero;

UPDATE Reserva SET Estado = 'Cancelada' WHERE Id = 7;               -- siempre con WHERE
SELECT * FROM Pasajero WHERE Email IS NULL;                         -- IS NULL, no = NULL
```

## :layout-template: Controlador + ViewModel + vista

```csharp
[HttpGet]
public IActionResult Crear()
{
    var modelo = new ReservaCrearViewModel();
    CargarListas(modelo);
    return View(modelo);
}

[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Crear(ReservaCrearViewModel modelo)
{
    if (!ModelState.IsValid) { CargarListas(modelo); return View(modelo); }   // las listas no se reciben en el POST
    try
    {
        Reserva r = reservaService.Reservar(modelo.VueloId!.Value, modelo.PasajeroId!.Value, modelo.Asiento);
        return RedirectToAction(nameof(Detalle), new { id = r.Id });            // POST → Redirect → GET
    }
    catch (ReglaDeNegocioException ex)
    {
        ModelState.AddModelError(ex.Campo, ex.Message);
        CargarListas(modelo);
        return View(modelo);
    }
}
```

```cshtml
@model ReservaCrearViewModel
<form asp-action="Crear" method="post">
    <div asp-validation-summary="ModelOnly"></div>
    <label asp-for="VueloId"></label>
    <select asp-for="VueloId" asp-items="Model.Vuelos"><option value="">— Elegir —</option></select>
    <span asp-validation-for="VueloId"></span>
    <button type="submit">Reservar</button>
</form>
@section Scripts { <partial name="_ValidationScriptsPartial" /> }
```

| Atributo | Ejemplo |
|---|---|
| `[Required(ErrorMessage = "…")]` | en combos, con `int?` |
| `[StringLength(100, MinimumLength = 3)]` | nombres |
| `[RegularExpression(@"^\d{7,8}$")]` | DNI |
| `[Range(1, 40)]` · `[EmailAddress]` · `[Display(Name = "…")]` | |
| `[ValidateNever]` | listas `SelectListItem` del ViewModel |
| atributo propio `: ValidationAttribute` | `[FechaFutura]` |

## :fire-extinguisher: Excepciones y logs

```csharp
try
{
    reserva.Id = reservas.Insertar(reserva);
}
catch (SqliteException ex) when (ex.SqliteErrorCode == 19)    // SQLITE_CONSTRAINT
{
    logger.LogWarning("Choque de reservas en {Asiento}", asiento);
    throw new ReglaDeNegocioException("Asiento", $"El asiento {asiento} acaba de reservarse en otra operación. Debe elegirse otro asiento.", ex);
}
logger.LogInformation("Reserva {ReservaId} confirmada", reserva.Id);   // plantilla, no $"…"
```

- Nunca `catch { }` vacío. Atrapar solo excepciones **específicas**, y únicamente donde pueden manejarse.
- `throw;` conserva el stack trace; `throw ex;` lo reemplaza.
- Registrar cada error **una** sola vez. No registrar claves, cadenas de conexión ni datos personales.
- El usuario ve un mensaje adecuado + un código; el detalle técnico se registra en el log.

## :lock-keyhole: Login con sesión y filtros

```csharp
public class RequiereLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Session.GetString("Usuario") is null)
            context.Result = new RedirectToActionResult("Index", "Login",
                new { returnUrl = context.HttpContext.Request.Path });
    }
}

[RequiereLogin] public class ReservasController(…) : Controller { … }
[RequiereRol(Roles.Admin)] public IActionResult Crear() { … }
```

| | |
|---|---|
| Autenticación | ¿quién es el usuario? → login |
| Autorización | ¿qué puede hacer? → roles |
| Cookie | se almacena en el navegador (no debe contener datos sensibles) |
| Sesión | se almacena en el servidor; el navegador solo conserva el Id |

## :circle-dot: Depurador

| Tecla | |
|---|---|
| F5 | continuar |
| F10 | paso a paso (sin entrar) |
| F11 | entrar al método |
| Shift+F11 | salir del método |
| Shift+F5 | detener |

Paneles: **Variables**, **Inspección (Watch)**, **Pila de llamadas (Call Stack)**. Breakpoint condicional: clic derecho sobre el punto rojo.

## :git-branch: Ritual de git

```bash
git status
git add -A
git commit -m "Desafío N: descripción del cambio"
git push
```

Un commit por paso lógico. Después del push, **revisar GitHub**.

## :list-checks: Checklist antes de entregar el parcial

- [ ] Compila sin errores (`dotnet build`).
- [ ] La base se genera automáticamente a partir del script.
- [ ] Cada formulario: `[ValidateAntiForgeryToken]`, `ModelState.IsValid`, recarga de listas, PRG.
- [ ] Ningún SQL construido con `+` o `$"…{x}"` a partir de datos del usuario.
- [ ] Controladores delgados: las reglas en servicios, el SQL en repositorios.
- [ ] Todo registrado en `Program.cs`; ningún `new XRepository(` en controladores.
- [ ] Errores: mensaje para el usuario + log.
- [ ] Commit y push. Revisar GitHub.
