## :target: Misión

En el estado actual, cualquiera que abra AeroTaller puede reservar, cancelar, borrar pasajeros y crear vuelos: un
aeropuerto sin control de acceso. En este desafío se construye la **torre de control**: quién accede y con qué alcance.

| Pantalla | Sin sesión | Mostrador | Admin |
|---|---|---|---|
| `/` (tablero), `/Vuelos`, `/Vuelos/Detalle/{id}` | :check: | :check: | :check: |
| `/Pasajeros…`, `/Reservas…` | → al login | :check: | :check: |
| `/Vuelos/Crear`, `POST /Pasajeros/Eliminar/{id}` | → al login | **Sin permiso** | :check: |

Lo que debe quedar funcionando:

1. El modelo `Usuario`, a partir de la tabla que ya trae `aerotaller.sql` con dos usuarios de prueba (`admin` / `aero2026` y `mostrador` / `vuelos2026`).
2. `UsuarioRepository` (y su versión falsa, para que el desafío 7 siga funcionando).
3. `AutenticacionService`: comprueba la clave y guarda en la **sesión** la identidad del usuario.
4. `LoginController` con el formulario, `Salir`, y el regreso a la dirección solicitada originalmente (`returnUrl`).
5. Dos **filtros**: `[RequiereLogin]` y `[RequiereRol(...)]`, y la vista `SinPermiso`.
6. El layout que muestra el usuario conectado y solo los enlaces que corresponden a su rol.

Un detalle a destacar: el mapa de asientos (`/Vuelos/Detalle/{id}`) es público, pero cada asiento libre lleva a
`/Reservas/Crear`, que requiere sesión. Al hacer clic sin haber iniciado sesión se llega al login… y al ingresar se vuelve
**al formulario con el vuelo y el asiento ya elegidos**. Eso es el `returnUrl`.

## :book-open-text: Repaso

Dos preguntas distintas, en este orden:

| | Pregunta | Si falla |
|---|---|---|
| **Autenticación** | ¿El usuario es quien dice ser? (usuario y clave) | 401 Unauthorized, o se lo redirige al login |
| **Autorización** | ¿Qué puede hacer? (su rol) | 403 Forbidden, o una página «sin permiso» |

El problema: **HTTP no tiene estado**. Cada request llega sin información del anterior: el servidor no conserva el dato de que,
un minuto antes, el usuario inició sesión. Para conservar información entre requests se necesitan cookies, y para que el
usuario no pueda modificar lo que se conserva, la **sesión**:

| | Cookie propia | Sesión |
|---|---|---|
| Dónde están los datos | en el navegador | en el servidor (el navegador tiene solo una cookie con el Id) |
| ¿El usuario los puede cambiar? | sí | no |
| Para | preferencias inofensivas | quién inició sesión y su rol |

Y para no repetir el mismo `if (no hay sesión) return RedirectToAction("Index", "Login");` en cada acción: un **filtro**,
código que se ejecuta **antes** de la acción sin estar escrito dentro de ella. Si el filtro asigna un `context.Result`, la acción
no se ejecuta. Ya se usaba uno: `[ValidateAntiForgeryToken]`.

:::catedra
Esto corresponde al [tema 15](https://taller-de-lenguajes-2.github.io/clases/tema-15-cookies-y-sesion/index.html), con el
login de la clínica. Las diferencias con lo visto en clase:

- En clase los usuarios estaban **en una lista en memoria** (`UsuarioRepository` Singleton). En el TP están en la **tabla
  `Usuario`**, por lo que el repositorio sigue el mismo patrón que los demás: recibe `ConexionDb` y es Scoped.
- En clase `LoginController` recibía `AutenticacionService` (la clase). En el TP recibe `IAutenticacionService`: es el
  desafío 7 aplicado.
- En clase `[RequiereRol("Admin")]` recibía **un** rol y no verificaba si había sesión. En el TP recibe **varios**
  (`params string[] roles`) y, si no hay sesión, redirige al login (no tiene sentido responder «sin permiso» a alguien
  cuya identidad se desconoce).
- Las claves siguen en **texto plano**, como en clase: solo para el TP. La forma correcta se presenta en «Para investigar».
:::

### :lightbulb: Tips y recursos

**¿Qué mecanismo gestiona el login?**

| Forma | Cómo | A favor | En contra | Cuándo |
|---|---|---|---|---|
| **Sesión + filtros propios** (la del TP, tema 15) | `AutenticacionService` + `[RequiereLogin]` + `[RequiereRol]` | se entiende cada pieza; es lo que se evalúa | se escribe manualmente (con riesgo de errores) | el TP y el parcial |
| Autenticación por cookie de ASP.NET Core | `AddAuthentication().AddCookie()`, `HttpContext.SignInAsync(...)` con *claims*, `[Authorize]`, `[Authorize(Roles = "Admin")]` | el framework resuelve el proceso; responde 401/403 según corresponde | más conceptos nuevos (claims, esquemas) | un proyecto real pequeño o mediano |
| ASP.NET Core Identity | registro, hash de claves, recuperación de contraseña, bloqueo por intentos… | todo resuelto y probado | pesado; usa Entity Framework | un proyecto real con usuarios que se registran por su cuenta |

**¿Cómo se guarda la clave?**

| Forma | A favor | En contra |
|---|---|---|
| **Texto plano** (la del TP, solo para el TP) | se ve y se prueba fácilmente | quien obtenga una copia de la base obtiene todas las claves |
| Hash rápido (SHA-256) implementado manualmente | no se guarda la clave | se puede atacar probando millones de claves por segundo; sin sal, dos claves iguales producen el mismo hash |
| `PasswordHasher<T>` de ASP.NET Core | hash lento, con sal, y con versión (se puede mejorar después) | una línea más al registrar y al comprobar |

Para investigar, un ejemplo con `PasswordHasher<T>` (definido en `Microsoft.AspNetCore.Identity`, que ya se incluye en ASP.NET Core):

```csharp
var hasher = new PasswordHasher<Usuario>();

// al dar de alta: en la columna Clave se guarda el hash, nunca la clave
usuario.Clave = hasher.HashPassword(usuario, "aero2026");

// al hacer login: se compara el hash guardado con la clave ingresada
bool ok = hasher.VerifyHashedPassword(usuario, usuario.Clave, claveIngresada) != PasswordVerificationResult.Failed;
```

## :code-xml: Paso a paso

### Paso 1 · La tabla Usuario

La tabla ya está en `aerotaller.sql` desde el desafío 1, con dos usuarios de ejemplo. Se recomienda releerla junto con el `CONTRATO.md`:

```sql
-- Usuarios del sistema (no confundir con Pasajero: los pasajeros no inician sesión).
-- Atención: clave en texto plano SOLO para el TP (igual que en el tema 15).
CREATE TABLE Usuario (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    NombreUsuario TEXT NOT NULL UNIQUE,
    Nombre        TEXT NOT NULL,
    Clave         TEXT NOT NULL,
    Rol           TEXT NOT NULL CHECK (Rol IN ('Admin', 'Mostrador'))
);

INSERT INTO Usuario (NombreUsuario, Nombre, Clave, Rol) VALUES
    ('admin',     'Administración', 'aero2026',   'Admin'),
    ('mostrador', 'Marta',          'vuelos2026', 'Mostrador');
```

`NombreUsuario` es `UNIQUE` porque es lo que se escribe en el login: dos usuarios con el mismo nombre harían ambiguo el
ingreso. El `CHECK` limita los roles a los dos que existen. Comprobarlo en DB Browser: `SELECT * FROM Usuario;` devuelve
`admin` y `mostrador`.

### Paso 2 · El modelo y su repositorio

```csharp
// Models/Usuario.cs
// Quien usa el sistema. No confundir con Pasajero: los pasajeros no inician sesión.
public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Clave { get; set; } = "";     // atención: en texto plano solo para el TP
    public string Rol { get; set; } = "";       // Roles.Admin o Roles.Mostrador
}

// Los nombres de los roles en un solo lugar: evita inconsistencias como "admin" en un lugar y "Admin" en otro
public static class Roles
{
    public const string Admin = "Admin";
    public const string Mostrador = "Mostrador";
}
```

En `Repositorios/Interfaces.cs`:

```csharp
public interface IUsuarioRepository
{
    Usuario? Obtener(string nombreUsuario);
}
```

`UsuarioRepository` se escribe sin ayuda: es un `SELECT … WHERE NombreUsuario = @usuario` con `AddWithValue`, igual que
cualquier `Obtener` de los desafíos anteriores. Si no encuentra al usuario, devuelve `null`.

:::solucion Ver UsuarioRepository y su versión falsa
```csharp
using Microsoft.Data.Sqlite;

public class UsuarioRepository(ConexionDb conexionDb) : IUsuarioRepository
{
    private readonly string cadenaConexion = conexionDb.Cadena;

    public Usuario? Obtener(string nombreUsuario)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(
            "SELECT Id, NombreUsuario, Nombre, Clave, Rol FROM Usuario WHERE NombreUsuario = @usuario", conexion);
        comando.Parameters.AddWithValue("@usuario", nombreUsuario);

        using var lector = comando.ExecuteReader();
        if (!lector.Read()) return null;

        return new Usuario
        {
            Id = lector.GetInt32(0),
            NombreUsuario = lector.GetString(1),
            Nombre = lector.GetString(2),
            Clave = lector.GetString(3),
            Rol = lector.GetString(4)
        };
    }
}

// Repositorios/Falsos/RepositoriosFalsos.cs
public class UsuarioRepositoryFalso(BaseFalsa db) : IUsuarioRepository
{
    public Usuario? Obtener(string nombreUsuario)
    {
        lock (db.Candado) return db.Usuarios.FirstOrDefault(u => u.NombreUsuario == nombreUsuario);
    }
}

// Y en BaseFalsa: la propiedad y, en el constructor, los dos usuarios
public List<Usuario> Usuarios { get; }
// …
Usuarios =
[
    new Usuario { Id = 1, NombreUsuario = "admin", Nombre = "Admin (memoria)", Clave = "aero2026", Rol = Roles.Admin },
    new Usuario { Id = 2, NombreUsuario = "mostrador", Nombre = "Mostrador (memoria)", Clave = "vuelos2026", Rol = Roles.Mostrador }
];
```
:::

Registrarlo en **las dos ramas** del `if` de `Program.cs`:

```csharp
// rama Falso
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepositoryFalso>();
// rama Sqlite
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
```

### Paso 3 · Habilitar la sesión

En `Program.cs`, con los demás servicios:

```csharp
// Sesión: los datos quedan en el servidor (en memoria); el navegador guarda solo una cookie con el Id
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);   // sin actividad por 20 minutos, la sesión expira
    options.Cookie.HttpOnly = true;                   // JavaScript no puede leer la cookie de sesión
    options.Cookie.IsEssential = true;                // se envía aunque no se acepten cookies opcionales
});
builder.Services.AddHttpContextAccessor();            // para que un SERVICIO acceda a la sesión
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();
```

Y en el pipeline, **antes** del ruteo:

```csharp
app.UseSerilogRequestLogging();
app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseSession();          // ← antes de MapControllerRoute: así las acciones (y los filtros) pueden usar la sesión

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

### Paso 4 · El servicio de autenticación

Un servicio no es un controlador: no tiene `HttpContext` disponible. `IHttpContextAccessor` le provee el del request actual.

```csharp
// Servicios/Interfaces.cs
public interface IAutenticacionService
{
    bool Login(string nombreUsuario, string clave);
    void Logout();
    bool EstaAutenticado();
    bool TieneRol(string rol);
}
```

```csharp
// Servicios/AutenticacionService.cs
// Guarda los datos del usuario en la SESIÓN (en el servidor); el navegador solo tiene la cookie con el Id de sesión.
public class AutenticacionService(
    IUsuarioRepository usuarios,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AutenticacionService> logger) : IAutenticacionService
{
    // Las claves de la sesión, en un solo lugar (las usan también los filtros y el layout)
    public const string ClaveUsuario = "Usuario";
    public const string ClaveNombre = "Nombre";
    public const string ClaveRol = "Rol";

    private ISession Sesion => httpContextAccessor.HttpContext!.Session;

    // Autenticación: ¿es quien dice ser?
    public bool Login(string nombreUsuario, string clave)
    {
        Usuario? usuario = usuarios.Obtener(nombreUsuario);
        if (usuario is null || usuario.Clave != clave)
        {
            // Se registra el intento (desafío 8), NUNCA la clave ingresada
            logger.LogWarning("Login fallido para el usuario {NombreUsuario}", nombreUsuario);
            return false;
        }

        Sesion.SetString(ClaveUsuario, usuario.NombreUsuario);
        Sesion.SetString(ClaveNombre, usuario.Nombre);
        Sesion.SetString(ClaveRol, usuario.Rol);

        logger.LogInformation("{NombreUsuario} inició sesión ({Rol})", usuario.NombreUsuario, usuario.Rol);
        return true;
    }

    public void Logout() => Sesion.Clear();

    public bool EstaAutenticado() => Sesion.GetString(ClaveUsuario) is not null;

    // Autorización: ¿qué puede hacer?
    public bool TieneRol(string rol) => Sesion.GetString(ClaveRol) == rol;
}
```

¿Por qué Scoped y no Singleton? Porque recibe `IUsuarioRepository`, que es Scoped: un Singleton que lo reciba sería una
dependencia cautiva (desafío 7) y la aplicación no se iniciaría.

### Paso 5 · El login

```csharp
// ViewModels/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

public class LoginViewModel
{
    [Display(Name = "Usuario")]
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string NombreUsuario { get; set; } = "";

    [Display(Name = "Contraseña")]
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]                       // asp-for genera type="password"
    public string Clave { get; set; } = "";

    // Dirección a la que se vuelve después de iniciar sesión
    public string? ReturnUrl { get; set; }
}
```

```csharp
// Controllers/LoginController.cs
using Microsoft.AspNetCore.Mvc;

public class LoginController(IAutenticacionService autenticacion) : Controller
{
    // GET /Login?returnUrl=/Reservas
    [HttpGet]
    public IActionResult Index(string? returnUrl)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(LoginViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        if (!autenticacion.Login(modelo.NombreUsuario, modelo.Clave))
        {
            // No indicar cuál de los dos es incorrecto: no se dan pistas a quien prueba usuarios
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
            return View(modelo);
        }

        // Solo se vuelve a una dirección del PROPIO sitio (evita redirecciones a otro sitio)
        if (Url.IsLocalUrl(modelo.ReturnUrl)) return Redirect(modelo.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    // Cerrar sesión cambia el estado: es POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Salir()
    {
        autenticacion.Logout();
        return RedirectToAction("Index", "Home");
    }
}
```

La vista `Views/Login/Index.cshtml` es un formulario como los del desafío 5: `asp-validation-summary="ModelOnly"` (allí
aparece el error de clave, que se agrega con clave `""`), los dos campos con su `asp-validation-for`, y el `ReturnUrl`
**oculto** para que se reenvíe en el POST:

```cshtml
<form asp-action="Index" method="post" class="formulario">
    <div asp-validation-summary="ModelOnly"></div>
    <input asp-for="ReturnUrl" type="hidden" />
    @* … NombreUsuario y Clave con label, input y span de validación … *@
    <button type="submit">Ingresar</button>
</form>
```

### Paso 6 · Los filtros: el control de pasaportes

Crear `Filtros/RequiereLoginAttribute.cs` con los dos filtros:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

// Se ejecuta ANTES de la acción. Si no hay sesión, interrumpe el request y redirige al login, conservando la dirección solicitada.
public class RequiereLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var sesion = context.HttpContext.Session;
        if (sesion.GetString(AutenticacionService.ClaveUsuario) is null)
        {
            var pedido = context.HttpContext.Request;
            context.Result = new RedirectToActionResult("Index", "Login",
                new { returnUrl = pedido.Path + pedido.QueryString });
        }
    }
}

// Autorización: además de haber iniciado sesión, se requiere uno de estos roles.
// Uso: [RequiereRol(Roles.Admin)]  o  [RequiereRol(Roles.Admin, Roles.Mostrador)]
public class RequiereRolAttribute(params string[] roles) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var sesion = context.HttpContext.Session;
        if (sesion.GetString(AutenticacionService.ClaveUsuario) is null)
        {
            var pedido = context.HttpContext.Request;
            context.Result = new RedirectToActionResult("Index", "Login",
                new { returnUrl = pedido.Path + pedido.QueryString });
        }
        else if (!roles.Contains(sesion.GetString(AutenticacionService.ClaveRol)))
        {
            context.Result = new ViewResult { ViewName = "SinPermiso" };
        }
    }
}
```

La vista `Views/Shared/SinPermiso.cshtml` (en `Shared` para que se encuentre desde cualquier controlador):

```cshtml
@{
    ViewData["Title"] = "Sin permiso";
}

<h1>Sin permiso</h1>
<p>El usuario actual no tiene permiso para acceder a esta sección.</p>
<p><a asp-controller="Home" asp-action="Index">← Volver al inicio</a></p>
```

A continuación, **aplicarlos**:

```csharp
[RequiereLogin]   // todas las acciones requieren sesión iniciada
public class ReservasController(...) : Controller { … }

[RequiereLogin]
public class PasajerosController(...) : Controller
{
    // … y Eliminar, además, solo el Admin
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiereRol(Roles.Admin)]
    public IActionResult Eliminar(int id) { … }
}

// VuelosController NO lleva [RequiereLogin]: el listado y el detalle son públicos.
// Solo Crear, en el GET y en el POST:
[HttpGet]
[RequiereRol(Roles.Admin)]
public IActionResult Crear() { … }

[HttpPost]
[ValidateAntiForgeryToken]
[RequiereRol(Roles.Admin)]
public IActionResult Crear(VueloCrearViewModel modelo) { … }
```

:::cuidado
Proteger solo el GET de `Crear` no es suficiente: cualquiera puede enviar el POST directamente (con Postman, con `curl`, o
copiando el formulario). **Cada acción que modifica datos requiere su filtro.**
:::

### Paso 7 · El layout muestra el usuario conectado

La vista también puede leer la sesión: `Context` es el `HttpContext` del request. En `_Layout.cshtml`:

```cshtml
@inject IConfiguration Configuracion
@{
    string? rol = Context.Session.GetString(AutenticacionService.ClaveRol);
    bool datosFalsos = Configuracion["OrigenDeDatos"] == "Falso";
}
...
<nav>
    <a asp-controller="Vuelos" asp-action="Index">Vuelos</a>
    @if (rol is not null)
    {
        <a asp-controller="Reservas" asp-action="Index">Reservas</a>
        <a asp-controller="Pasajeros" asp-action="Index">Pasajeros</a>
    }
    @if (rol == Roles.Admin)
    {
        <a asp-controller="Vuelos" asp-action="Crear">Nuevo vuelo</a>
    }
</nav>
<div class="usuario">
    @if (rol is not null)
    {
        <span>@Context.Session.GetString(AutenticacionService.ClaveNombre) <small>(@rol)</small></span>
        <form asp-controller="Login" asp-action="Salir" method="post">
            <button type="submit" class="enlace">Salir</button>
        </form>
    }
    else
    {
        <a asp-controller="Login" asp-action="Index">Ingresar</a>
    }
</div>
```

`Salir` es un **formulario POST** (el tag helper agrega el token antiforgery automáticamente), no un enlace: cerrar sesión
cambia el estado. La clase `enlace` (ya incluida en `sitio.css`) da al botón la apariencia de un enlace.

:::nota
Ocultar «Nuevo vuelo» al mostrador es una cuestión de **usabilidad**, no de seguridad. La protección está en el filtro del servidor:
al escribir `/Vuelos/Crear` manualmente con la sesión de `mostrador`, debe verse «Sin permiso».
:::

### Paso 8 · Probar todo, con las mismas pruebas del verificador

| Prueba | Resultado esperado |
|---|---|
| Sin sesión, `/Reservas` | redirige a `/Login?returnUrl=%2FReservas` |
| Ingresar como `mostrador` desde esa pantalla | se vuelve a `/Reservas` |
| Sin sesión, clic en un asiento libre del mapa, e ingresar | se vuelve a `/Reservas/Crear?vueloId=…&asiento=…` con todo precargado |
| Clave incorrecta | «Usuario o contraseña incorrectos.» y se permanece en el login |
| `mostrador` en `/Vuelos/Crear` | vista «Sin permiso» |
| `admin` en `/Vuelos/Crear` | el formulario |
| Salir, y después `/Reservas` | redirige nuevamente al login |
| `/Login?returnUrl=https://otro-sitio.com` e ingresar | redirige al inicio de AeroTaller, no al otro sitio |
| «AeroTaller (datos falsos)» e ingresar como `admin` | en la parte superior aparece «Admin (memoria)» |

En las herramientas del navegador (**F12 → Aplicación → Cookies**) aparece `.AspNetCore.Session` con un valor ilegible.
Es el único dato del usuario que almacena el navegador; el rol está en el servidor. Revisar también `logs/`: los inicios de
sesión (y los fallidos) quedaron registrados, sin ninguna clave.
