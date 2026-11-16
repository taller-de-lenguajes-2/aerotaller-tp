## 🎯 Misión

Hoy cualquiera que abra AeroTaller puede reservar, cancelar, borrar pasajeros y crear vuelos. Un aeropuerto sin control
de acceso. En este desafío construimos la **torre de control**: quién entra y hasta dónde.

| Pantalla | Sin sesión | Mostrador | Admin |
|---|---|---|---|
| `/` (tablero), `/Vuelos`, `/Vuelos/Detalle/{id}` | ✔ | ✔ | ✔ |
| `/Pasajeros…`, `/Reservas…` | → al login | ✔ | ✔ |
| `/Vuelos/Crear`, `POST /Pasajeros/Eliminar/{id}` | → al login | **Sin permiso** | ✔ |

Lo que tiene que quedar andando:

1. La tabla `Usuario` con los dos usuarios de prueba (`admin` / `aero2026` y `mostrador` / `vuelos2026`).
2. `UsuarioRepository` (y su falso, para que el desafío 7 siga andando).
3. `AutenticacionService`: comprueba la clave y guarda quién es en la **sesión**.
4. `LoginController` con el formulario, `Salir`, y la vuelta a donde querías ir (`returnUrl`).
5. Dos **filtros**: `[RequiereLogin]` y `[RequiereRol(...)]`, y la vista `SinPermiso`.
6. El layout que muestra quién está conectado y solo los links que le corresponden.

Un detalle lindo: el mapa de asientos (`/Vuelos/Detalle/{id}`) es público, pero cada asiento libre lleva a `/Reservas/Crear`,
que pide sesión. Si hacés clic sin estar logueado, vas al login… y al ingresar volvés **al formulario con el vuelo y el
asiento ya elegidos**. Eso es el `returnUrl`.

## 📚 Repaso

Dos preguntas distintas, en este orden:

| | Pregunta | Si falla |
|---|---|---|
| **Autenticación** | ¿Sos quien decís ser? (usuario y clave) | 401 Unauthorized, o se lo manda al login |
| **Autorización** | ¿Qué podés hacer? (tu rol) | 403 Forbidden, o una página «sin permiso» |

El problema: **HTTP no tiene estado**. Cada request llega sin saber nada del anterior: el servidor no recuerda que hace un
minuto te logueaste. Para recordar algo entre requests hacen falta cookies, y para que el usuario no pueda tocar lo que
recordamos, la **sesión**:

| | Cookie propia | Sesión |
|---|---|---|
| Dónde están los datos | en el navegador | en el servidor (el navegador tiene solo una cookie con el Id) |
| ¿El usuario los puede cambiar? | sí | no |
| Para | preferencias inofensivas | quién inició sesión y su rol |

Y para no repetir el mismo `if (no hay sesión) return RedirectToAction("Index", "Login");` en cada acción: un **filtro**,
código que corre **antes** de la acción sin estar escrito adentro. Si el filtro pone un `context.Result`, la acción no se
ejecuta. Ya venías usando uno: `[ValidateAntiForgeryToken]`.

:::catedra
Esto es el [tema 15](https://taller-de-lenguajes-2.github.io/clases/tema-15-cookies-y-sesion/index.html), con el login de la
clínica. Las diferencias con lo que vimos en clase:

- En clase los usuarios estaban **en una lista en memoria** (`UsuarioRepository` Singleton). Acá están en la **tabla `Usuario`**,
  así que el repositorio es como los otros: recibe `ConexionDb` y es Scoped.
- En clase `LoginController` recibía `AutenticacionService` (la clase). Acá recibe `IAutenticacionService`: es el desafío 7 aplicado.
- En clase `[RequiereRol("Admin")]` recibía **un** rol y no miraba si había sesión. Acá recibe **varios** (`params string[] roles`)
  y, si no hay sesión, manda al login (no tiene sentido decir «sin permiso» a alguien que no sabemos quién es).
- Las claves siguen en **texto plano**, como en clase: solo para el TP. Cómo se hace bien queda en «Para investigar».
:::

## 💻 Paso a paso

### Paso 1 · La tabla Usuario

En `aerotaller.sql` agregá `DROP TABLE IF EXISTS Usuario;` junto a los otros `DROP` y la tabla (respetá el contrato):

```sql
-- Usuarios del sistema (no confundir con Pasajero: los pasajeros no inician sesión).
-- ⚠ Clave en texto plano SOLO para el TP (igual que en el tema 15).
CREATE TABLE Usuario (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    NombreUsuario TEXT NOT NULL UNIQUE,
    Nombre        TEXT NOT NULL,
    Clave         TEXT NOT NULL,
    Rol           TEXT NOT NULL CHECK (Rol IN ('Admin', 'Mostrador'))
);
```

Pegá al final del script el contenido de `material/datos-usuarios.sql` (los dos `INSERT`). Apagá la app, **borrá
`AeroTaller/aerotaller.db`** y arrancá: se crea de nuevo, con la tabla nueva. Comprobalo en DB Browser:
`SELECT * FROM Usuario;`.

### Paso 2 · El modelo y su repositorio

```csharp
// Models/Usuario.cs
// Quien usa el sistema. No confundir con Pasajero: los pasajeros no inician sesión.
public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Clave { get; set; } = "";     // ⚠ en texto plano solo para el TP
    public string Rol { get; set; } = "";       // Roles.Admin o Roles.Mostrador
}

// Los nombres de los roles en un solo lugar: así no hay "admin" en un lado y "Admin" en otro
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

Escribí `UsuarioRepository` vos: es un `SELECT … WHERE NombreUsuario = @usuario` con `AddWithValue`, igual que cualquier
`Obtener` de los desafíos anteriores. Si no lo encuentra, devuelve `null`.

:::solucion Ver UsuarioRepository y el falso
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

Registralo en **las dos ramas** del `if` de `Program.cs`:

```csharp
// rama Falso
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepositoryFalso>();
// rama Sqlite
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
```

### Paso 3 · Encender la sesión

En `Program.cs`, con los demás servicios:

```csharp
// Sesión: los datos quedan en el servidor (en memoria); el navegador guarda solo una cookie con el Id
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);   // sin actividad por 20 minutos, la sesión se pierde
    options.Cookie.HttpOnly = true;                   // JavaScript no puede leer la cookie de sesión
    options.Cookie.IsEssential = true;                // se manda aunque no se acepten cookies opcionales
});
builder.Services.AddHttpContextAccessor();            // para que un SERVICIO llegue a la sesión
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

Un servicio no es un controlador: no tiene `HttpContext` a mano. `IHttpContextAccessor` le da el del request actual.

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
            // Se registra el intento (desafío 8), NUNCA la clave que escribió
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
dependencia cautiva (desafío 7) y la app no arrancaría.

### Paso 5 · El login

```csharp
// ViewModels/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

public class LoginViewModel
{
    [Display(Name = "Usuario")]
    [Required(ErrorMessage = "Ingresá tu usuario.")]
    public string NombreUsuario { get; set; } = "";

    [Display(Name = "Contraseña")]
    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    [DataType(DataType.Password)]                       // asp-for genera type="password"
    public string Clave { get; set; } = "";

    // A dónde volver después de iniciar sesión
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
            // No decir cuál de los dos está mal: no le damos pistas a quien prueba usuarios
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
            return View(modelo);
        }

        // Solo se vuelve a una dirección de NUESTRO sitio (evita redirecciones a otro sitio)
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

La vista `Views/Login/Index.cshtml` es un formulario como los del desafío 5: `asp-validation-summary="ModelOnly"` (ahí
aparece el error de clave, que va con clave `""`), los dos campos con su `asp-validation-for`, y el `ReturnUrl` **oculto**
para que vuelva en el POST:

```cshtml
<form asp-action="Index" method="post" class="formulario">
    <div asp-validation-summary="ModelOnly"></div>
    <input asp-for="ReturnUrl" type="hidden" />
    @* … NombreUsuario y Clave con label, input y span de validación … *@
    <button type="submit">Ingresar</button>
</form>
```

### Paso 6 · Los filtros: el control de pasaportes

Creá `Filtros/RequiereLoginAttribute.cs` con los dos filtros:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

// Corre ANTES de la acción. Si no hay sesión, no deja pasar y manda al login, recordando a dónde quería ir.
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

// Autorización: además de haber iniciado sesión, hace falta uno de estos roles.
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

La vista `Views/Shared/SinPermiso.cshtml` (en `Shared` para que la encuentre desde cualquier controlador):

```cshtml
@{
    ViewData["Title"] = "Sin permiso";
}

<h1>Sin permiso</h1>
<p>Iniciaste sesión, pero tu usuario no tiene permiso para entrar acá.</p>
<p><a asp-controller="Home" asp-action="Index">← Volver al inicio</a></p>
```

Y ahora, a **ponerlos**:

```csharp
[RequiereLogin]   // todas las acciones piden sesión iniciada
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
Proteger solo el GET de `Crear` no sirve: cualquiera puede mandar el POST directo (con Postman, con `curl`, o copiando el
formulario). **Cada acción que cambia datos lleva su filtro.**
:::

### Paso 7 · El layout sabe quién está

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

`Salir` es un **formulario POST** (el tag helper agrega el token antiforgery solo), no un link: cerrar sesión cambia el estado.
La clase `enlace` (ya está en `sitio.css`) hace que el botón parezca un link.

:::nota
Ocultar «Nuevo vuelo» al mostrador es **comodidad**, no seguridad. Lo que protege es el filtro en el servidor: probá escribir
`/Vuelos/Crear` a mano con `mostrador` y tenés que ver «Sin permiso».
:::

### Paso 8 · Probar todo, como lo va a probar el verificador

| Prueba | Resultado esperado |
|---|---|
| Sin sesión, `/Reservas` | redirige a `/Login?returnUrl=%2FReservas` |
| Ingresar como `mostrador` desde ahí | volvés a `/Reservas` |
| Sin sesión, clic en un asiento libre del mapa, e ingresar | volvés a `/Reservas/Crear?vueloId=…&asiento=…` con todo precargado |
| Clave mala | «Usuario o contraseña incorrectos.» y seguís en el login |
| `mostrador` en `/Vuelos/Crear` | vista «Sin permiso» |
| `admin` en `/Vuelos/Crear` | el formulario |
| Salir, y después `/Reservas` | de nuevo al login |
| `/Login?returnUrl=https://otro-sitio.com` e ingresar | terminás en el inicio de AeroTaller, no en el otro sitio |
| «AeroTaller (datos falsos)» e ingresar como `admin` | arriba dice «Admin (memoria)» |

Abrí las herramientas del navegador (**F12 → Aplicación → Cookies**): vas a ver `.AspNetCore.Session` con un valor ilegible.
Ese es todo lo que el navegador sabe de vos. El rol está en el servidor. Mirá también `logs/`: los logins (y los fallidos)
quedaron registrados, sin ninguna clave.

## 🔴 Breakpoints

:::breakpoint RequiereLoginAttribute.cs · OnActionExecuting, primera línea
Sin sesión, abrí `/Reservas?estado=Cancelada`. Con F10 mirá `pedido.Path` y `pedido.QueryString` en Variables. Poné otro
breakpoint en `ReservasController.Index` y apretá F5: **nunca frena ahí**, porque el filtro puso un `Result` y cortó.
Mirá la **Pila de llamadas** del filtro: quien lo llama es `ControllerActionInvoker`, la misma pieza que después llama a tu acción.
:::

:::breakpoint LoginController.cs · Index (POST), en el if (Url.IsLocalUrl(…))
Llegá al login desde `/Reservas` y mirá `modelo.ReturnUrl` (`/Reservas`): `IsLocalUrl` da `true`. Ahora abrí a mano
`/Login?returnUrl=https://otro-sitio.com` e ingresá: `IsLocalUrl` da `false` y terminás en el inicio. Sin ese `if`, un link
trucho mandado por mail podría llevar al usuario, recién logueado, a un sitio que imita a AeroTaller.
:::

:::breakpoint AutenticacionService.cs · Login, en la línea del primer SetString
Ingresá con `mostrador` desde una ventana normal y con `admin` desde una de incógnito. En Inspección (Watch) agregá
`Sesion.Id`: es distinto en cada navegador. El mismo `AutenticacionService`, el mismo código, dos sesiones separadas. Probá
también con una clave mala: ¿qué vale `usuario`? ¿Y si el usuario no existe?
:::

Preguntate:

- Si `app.UseSession()` va **después** de `MapControllerRoute`, ¿qué pasa en el primer request?
- Si reiniciás la app con alguien logueado, ¿sigue logueado? ¿Por qué? (Pista: `AddDistributedMemoryCache`.)
- ¿Por qué el error del login no dice si falló el usuario o la clave?

## 🔀 Alternativas

**¿Quién se encarga del login?**

| Forma | Cómo | A favor | En contra | Cuándo |
|---|---|---|---|---|
| **Sesión + filtros propios** (lo nuestro, tema 15) | `AutenticacionService` + `[RequiereLogin]` + `[RequiereRol]` | se entiende cada pieza; es lo que se evalúa | lo escribimos a mano (y lo podemos escribir mal) | el TP y el parcial |
| Autenticación por cookie de ASP.NET Core | `AddAuthentication().AddCookie()`, `HttpContext.SignInAsync(...)` con *claims*, `[Authorize]`, `[Authorize(Roles = "Admin")]` | el framework hace el trabajo; responde 401/403 como corresponde | más conceptos nuevos (claims, esquemas) | un proyecto real chico o mediano |
| ASP.NET Core Identity | registro, hash de claves, recuperar contraseña, bloqueo por intentos… | todo resuelto y probado | pesado; usa Entity Framework | un proyecto real con usuarios que se registran solos |

**¿Cómo se guarda la clave?**

| Forma | A favor | En contra |
|---|---|---|
| **Texto plano** (lo nuestro, solo para el TP) | se ve y se prueba fácil | si alguien se lleva la base, se lleva todas las claves |
| Hash rápido (SHA-256) hecho a mano | no se guarda la clave | se puede atacar probando millones de claves por segundo; sin sal, dos claves iguales dan el mismo hash |
| `PasswordHasher<T>` de ASP.NET Core | hash lento, con sal, y con versión (se puede mejorar después) | una línea más al registrar y al comprobar |

Para investigar, así se vería con `PasswordHasher<T>` (está en `Microsoft.AspNetCore.Identity`, que ya viene con ASP.NET Core):

```csharp
var hasher = new PasswordHasher<Usuario>();

// al dar de alta: en la columna Clave se guarda el hash, nunca la clave
usuario.Clave = hasher.HashPassword(usuario, "aero2026");

// al hacer login: se compara el hash guardado con lo que escribió
bool ok = hasher.VerifyHashedPassword(usuario, usuario.Clave, claveIngresada) != PasswordVerificationResult.Failed;
```

## ⚠️ Errores típicos

- **`InvalidOperationException: Session has not been configured for this application or request.`**: falta `app.UseSession()`,
  o está después de `MapControllerRoute`. Como el layout lee la sesión, explota **cualquier** página.
- **Falta `AddDistributedMemoryCache()` o `AddHttpContextAccessor()`**: `Unable to resolve service for type
  '…IDistributedCache' while attempting to activate '…DistributedSessionStore'` (la sesión no tiene dónde guardar) o
  `'…IHttpContextAccessor' while attempting to activate 'AutenticacionService'`. En Development, la app ni arranca.
- **`SQLite Error 1: 'no such table: Usuario'`**: cambiaste `aerotaller.sql` pero no borraste `aerotaller.db`.
- **`IUsuarioRepository` registrado solo en una rama**: con datos falsos (o con SQLite) la app no arranca. Las dos ramas
  del `if` tienen que registrar las mismas interfaces.
- **`"admin"` en un lado y `"Admin"` en otro**: el filtro compara textos exactos. Usá `Roles.Admin` siempre (y la tabla
  tiene un `CHECK` que solo acepta `'Admin'` y `'Mostrador'`).
- **Proteger solo el GET** (o solo esconder el link): el POST queda abierto para cualquiera.
- **`return Redirect(modelo.ReturnUrl);` sin `Url.IsLocalUrl`**: redirección abierta. Y **Salir como link** (`<a href="/Login/Salir">`):
  la acción es `[HttpPost]`, así que da 405, y aunque anduviera, cerrar sesión cambia el estado.
