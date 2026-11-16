## 🎯 Misión

Dos cosas pueden salir mal en AeroTaller, y hoy ninguna está bien resuelta.

**1 · El choque de asientos.** Dos personas en dos mostradores eligen el 20C del mismo vuelo **en el mismo instante**.
Las dos pasan la regla «el asiento está libre» de `ReservaService` (en ese momento lo estaba), y las dos insertan.
Resultado: dos pasajeros con la misma tarjeta de embarque. La regla del servicio no alcanza, porque entre que pregunta
y que inserta pasan unos milisegundos en los que otro puede meterse.

**2 · El error inesperado.** La base queda en solo lectura, el disco se llena, alguien borró una tabla. Hoy el usuario ve
una página con el `StackTrace` (en Development) o una pantalla en blanco (en producción), y **nadie se entera**: no queda
registro de qué pasó ni cuándo.

Los aviones tienen una **caja negra**: registra todo, siempre, para que después se pueda reconstruir qué pasó.
Eso vamos a construir:

1. Un **índice único parcial** en la base: la última palabra la tiene SQLite.
2. Un `catch` **con filtro** en `ReservaService` que convierte el choque en un error amable al lado del campo `Asiento`.
3. **Serilog** escribiendo en la consola y en `logs/aerotaller-AAAAMMDD.txt`.
4. `ILogger<T>` en los servicios, con **plantillas** (log estructurado).
5. `UseExceptionHandler("/Home/Error")` + una vista de error digna, con un código para buscar el problema en el log.
6. Una prueba de verdad: la base en solo lectura.

## 📚 Repaso

Una excepción que nadie atrapa **sube por la pila de llamadas** (tema 16). En AeroTaller la pila de una reserva es:

```text
ReservasController.Crear → ReservaService.Reservar → ReservaRepository.Insertar → SqliteCommand.ExecuteScalar 💥
```

La pregunta de este desafío no es «cómo se escribe un try/catch» (eso ya lo sabés) sino **en qué escalón** va cada uno.
Regla: se atrapa donde se puede hacer algo útil con el error. Si no hay nada útil para hacer, que suba.

| Capa | ¿Atrapa? | Qué hace |
|---|---|---|
| Repositorio | **no** | deja subir todo. El `using var conexion` cierra la conexión igual (es un `finally` que escribe el compilador) |
| Servicio | **solo lo que sabe traducir** | `SqliteException` con código 19 (restricción) → `ReglaDeNegocioException` en el campo `Asiento`. Cualquier otra, que siga |
| Controlador | `ReglaDeNegocioException` | la pasa a `ModelState` (ya lo hacés desde el desafío 6) |
| `UseExceptionHandler` | **todo lo demás** | lo registra en el log y muestra una página de error sin detalles técnicos |

Las herramientas del tema 16 que vas a usar:

- **Filtro `when`**: `catch (SqliteException ex) when (ex.SqliteErrorCode == 19)`. Si la condición da `false`, ese catch se
  saltea y la excepción sigue subiendo, **intacta**.
- **Envolver con contexto**: `throw new ReglaDeNegocioException("Asiento", "…", ex)`. La original viaja adentro, como `InnerException`.
- **`throw;` y nunca `throw ex;`** (el segundo pisa el `StackTrace`).
- **Nunca un catch vacío** ni un `catch (Exception)` que devuelve un valor de mentira.

Y las del tema 17:

- `ILogger<T>` llega por el constructor (inyección de dependencias, desafío 7). La `T` es la **categoría**: cada línea dice quién la escribió.
- Niveles: `Information` para el flujo normal (una reserva), `Warning` para lo raro que no rompe (un choque de asientos),
  `Error` para lo que falló (eso lo registra `UseExceptionHandler` por vos).
- **Plantillas, no strings armados**: `LogInformation("Reserva {ReservaId} confirmada", reserva.Id)`. Sin `$`, sin `+`.
- **Nunca** contraseñas, cadenas de conexión ni datos personales que no hacen falta (el DNI, por ejemplo).
- Serilog se enchufa **detrás** de `ILogger`: tu código no cambia, cambia adónde va cada línea.

:::catedra
En los temas [16](https://taller-de-lenguajes-2.github.io/clases/tema-16-excepciones/index.html) y
[17](https://taller-de-lenguajes-2.github.io/clases/tema-17-logs/index.html) el manejador global es para una **Web API**:
una clase `ManejadorGlobal : IExceptionHandler` que responde `ProblemDetails` (JSON) y registra el error con `LogError(ex, …)`.
En **MVC** el usuario espera una **página**, no un JSON, así que adaptamos: `app.UseExceptionHandler("/Home/Error")` vuelve a
ejecutar el request contra la acción `HomeController.Error`, que dibuja una vista. Y no hace falta loguear a mano: con la forma
de ruta (`"/Home/Error"`), el propio middleware registra la excepción completa como `[ERR]`, con categoría
`Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware`. (La novedad de .NET 10 que cuenta la diapositiva —que la
excepción no se registra— aplica cuando un `IExceptionHandler` propio devuelve `true`.)
:::

## 💻 Paso a paso

### Paso 1 · El índice único parcial: la base tiene la última palabra

En `aerotaller.sql`, después del `CREATE TABLE Reserva`:

```sql
-- Defensa en profundidad: aunque el servicio ya lo controla, la BASE no deja
-- dos reservas confirmadas en el mismo asiento del mismo vuelo.
CREATE UNIQUE INDEX UX_Reserva_AsientoConfirmado
    ON Reserva (VueloId, Asiento) WHERE Estado = 'Confirmada';
```

¿Por qué **parcial** (con `WHERE`)? Porque una reserva **cancelada** libera el asiento: tiene que poder haber una
cancelada y una confirmada en el mismo 1C. Un índice único común (sin `WHERE`) lo impediría.

Apagá la app, **borrá `AeroTaller/aerotaller.db`** y arrancá: se crea de nuevo, ya con el índice.

Probalo en DB Browser for SQLite (pestaña **Ejecutar SQL**). El 3A del AT1001 (vuelo 2) ya está ocupado en los datos de ejemplo:

```sql
INSERT INTO Reserva (VueloId, PasajeroId, Asiento, Fecha, Estado)
VALUES (2, 5, '3A', '2026-01-01 10:00', 'Confirmada');
-- UNIQUE constraint failed: Reserva.VueloId, Reserva.Asiento

INSERT INTO Reserva (VueloId, PasajeroId, Asiento, Fecha, Estado)
VALUES (2, 5, '3A', '2026-01-01 10:00', 'Cancelada');
-- ✔ anda: el índice solo mira las confirmadas
```

Después tocá **Revertir cambios** para no dejar esa fila de prueba.

### Paso 2 · Atrapar el choque en el servicio

Primero, que `ReglaDeNegocioException` pueda llevar la excepción original adentro. Agregale un tercer parámetro opcional:

```csharp
// Excepciones/ReglaDeNegocioException.cs
public class ReglaDeNegocioException(string campo, string mensaje, Exception? causa = null)
    : Exception(mensaje, causa)        // causa → InnerException
{
    // "" = error general del formulario (se muestra en el asp-validation-summary)
    public string Campo { get; } = campo;
}
```

En `ReservaService.Reservar`, el `try` envuelve **solo** la línea que puede chocar, y el `catch` atrapa **solo** el código
de restricción:

```csharp
using Microsoft.Data.Sqlite;

public class ReservaService(...) : IReservaService
{
    // SQLITE_CONSTRAINT: se violó una restricción de la base (UNIQUE, FOREIGN KEY, CHECK…)
    private const int ErrorDeRestriccion = 19;

    public Reserva Reservar(int vueloId, int pasajeroId, string asiento)
    {
        // … las reglas del desafío 6 y el new Reserva { … }, sin cambios …

        try
        {
            reserva.Id = reservas.Insertar(reserva);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == ErrorDeRestriccion)
        {
            // Las dos pasaron la regla "asiento libre", pero el índice único solo deja entrar a la primera
            throw new ReglaDeNegocioException("Asiento", $"Alguien acaba de reservar el {asiento}. Elegí otro asiento.", ex);
        }

        return reserva;
    }
}
```

El controlador **no cambia**: ya atrapa `ReglaDeNegocioException` y la pasa a `ModelState`. El usuario ve el error al lado
del campo `Asiento`, como cualquier otra regla.

:::nota
¿Por qué no atrapar en el repositorio? Porque el repositorio no sabe **qué significa** el error para el usuario ni en qué
campo mostrarlo. ¿Y en el controlador? Porque tendría que conocer `SqliteException`, y con `OrigenDeDatos=Falso` (desafío 7)
esa excepción no existe. El servicio es el que sabe que «restricción violada al insertar una reserva» = «el asiento se ocupó recién».
:::

**Provocá el choque.** A mano es casi imposible acertarle a los milisegundos justos, así que vamos a usar el depurador para
«congelar» un mostrador y DB Browser como el **otro** mostrador:

1. F5 con **AeroTaller**. Breakpoint en la línea `reserva.Id = reservas.Insertar(reserva);`.
2. Abrí `http://localhost:5210/Reservas/Crear?vueloId=5&asiento=20C` (el AT2020), elegí un pasajero y enviá.
   Frena en el breakpoint: **ya pasó todas las reglas**, el 20C estaba libre.
3. Con la app frenada, en DB Browser abrí `AeroTaller/aerotaller.db` y ejecutá (es «el otro mostrador» ganando de mano):

   ```sql
   INSERT INTO Reserva (VueloId, PasajeroId, Asiento, Fecha, Estado)
   VALUES (5, 8, '20C', '2026-01-01 10:00', 'Confirmada');
   ```

   Tocá **Escribir cambios** (`Ctrl/Cmd + S`).
4. Volvé a VS Code y apretá F10: el `INSERT` choca contra el índice y cae en el `catch`. F5, y en el navegador aparece
   el error al lado del asiento. En la base hay **una sola** reserva confirmada del 20C.

:::nota
¿Por qué no con dos navegadores y el breakpoint? Porque cuando el depurador frena, frena **todo el proceso**: el segundo
request no avanza hasta que soltás el primero, y para entonces el 20C ya está ocupado (lo agarra la regla del servicio, no el
índice). El verificador sí lo prueba de verdad: manda 8 reservas del mismo asiento a la vez, sin depurador, y cuenta cuántas
quedaron en la base.
:::

### Paso 3 · Serilog: que el log sobreviva

Los proveedores de log que trae .NET no escriben a archivo. Instalá Serilog:

```bash
cd AeroTaller
dotnet add package Serilog.AspNetCore
cd ..
```

Trae Serilog, los *sinks* (destinos) de consola y de archivo, y la lectura de la configuración desde `appsettings.json`.

En `appsettings.json`, **reemplazá** la sección `"Logging"` por la de Serilog (Serilog no lee `"Logging"`):

```json
{
  "ConnectionStrings": {
    "AeroTaller": "Data Source=aerotaller.db;Foreign Keys=True"
  },
  "OrigenDeDatos": "Sqlite",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/aerotaller-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext}: {Message:lj} (trace {TraceId}){NewLine}{Exception}"
        }
      }
    ]
  },
  "AllowedHosts": "*"
}
```

- `rollingInterval: Day` → un archivo por día: `logs/aerotaller-20261116.txt`. La ruta es relativa a la carpeta desde donde
  corre la app (`AeroTaller/`).
- `(trace {TraceId})` agrega a cada línea el identificador del request: con eso vas a encontrar el error que vio un usuario (paso 6).

En `Program.cs`, apenas después de crear el `builder`:

```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog reemplaza al logger por defecto y lee su configuración de appsettings.json
builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));
```

Arrancá y fijate que aparezca `AeroTaller/logs/`. `logs/` ya está en el `.gitignore`: los logs no se suben al repositorio.

### Paso 4 · ILogger<T> en los servicios: contar lo que pasa

Pedí el logger por el constructor (el contenedor ya sabe darlo):

```csharp
public class ReservaService(
    IReservaRepository reservas,
    IVueloRepository vuelos,
    IPasajeroRepository pasajeros,
    ILogger<ReservaService> logger) : IReservaService
```

Y registrá los eventos del negocio, con **plantillas**:

```csharp
// Al final de Reservar: una reserva nueva es el flujo normal → Information
logger.LogInformation("Reserva {ReservaId} confirmada: vuelo {Numero}, asiento {Asiento}, pasajero {PasajeroId}",
    reserva.Id, vuelo.Numero, asiento, pasajero.Id);

// Adentro del catch del choque: raro, pero no es un error nuestro → Warning (y SIN pasar ex: no es un bug)
logger.LogWarning("Choque de reservas: el asiento {Asiento} del vuelo {Numero} se ocupó recién", asiento, vuelo.Numero);

// En Cancelar
logger.LogInformation("Reserva {ReservaId} cancelada (vuelo {Numero}, asiento {Asiento})",
    reservaId, reserva.Vuelo.Numero, reserva.Asiento);
```

Hacé lo mismo en `PasajeroService` (registrado, actualizado, eliminado) y en `VueloService` (vuelo creado). En pasajeros,
**solo el Id**: el DNI y el email son datos personales y no hacen falta para reconstruir qué pasó.

```csharp
logger.LogInformation("Pasajero {PasajeroId} registrado", pasajero.Id);   // ✔
logger.LogInformation("Pasajero {Dni} registrado", pasajero.Dni);         // ✘ dato personal
```

Un detalle lindo para cerrar el desafío 7: al final de `Program.cs`, antes de `app.Run()`, registrá con qué datos arrancó.

```csharp
app.Logger.LogInformation("AeroTaller arrancó con datos de origen {OrigenDeDatos}", origenDeDatos);
```

Hacé una reserva y abrí el archivo del día. Tenés que ver algo así:

```text
2026-11-16 12:37:59 [INF] ReservaService: Reserva 16 confirmada: vuelo AT1001, asiento 4C, pasajero 5 (trace 9f2c…)
2026-11-16 12:37:59 [INF] Serilog.AspNetCore.RequestLoggingMiddleware: HTTP POST /Reservas/Crear responded 302 in 9.5723 ms (trace 9f2c…)
```

(La segunda línea aparece cuando hagas el paso 5.)

### Paso 5 · El pipeline y la página de error

En `Program.cs`, después de `builder.Build()`, el orden **importa**:

```csharp
var app = builder.Build();

app.UseSerilogRequestLogging();           // 1 · una línea de log por request (envuelve a todo lo demás)
app.UseExceptionHandler("/Home/Error");   // 2 · si algo explota más adentro: se loguea y se muestra la página de error
app.UseStaticFiles();
// … MapControllerRoute igual que antes …
```

`UseSerilogRequestLogging` va **antes** para que registre la respuesta que realmente salió (el 500 final), y no la excepción
a mitad de camino.

:::cuidado
El proyecto que crea `dotnet new mvc` pone `UseExceptionHandler` adentro de `if (!app.Environment.IsDevelopment())`.
Nosotros lo llamamos **siempre**: así lo ves funcionar mientras desarrollás (y el verificador lo prueba en Development).
:::

La acción y su ViewModel:

```csharp
// ViewModels/: la pantalla de error solo recibe el código para buscar el request en el log
public record ErrorViewModel(string? RequestId);
```

```csharp
using System.Diagnostics;

// HomeController: a esta acción llega UseExceptionHandler cuando algo explota.
// El error ya quedó en el log; acá solo se muestra un mensaje digno y el código para buscarlo.
// OJO: sin [HttpGet]. Si el request que falló era un POST, se re-ejecuta como POST.
public IActionResult Error()
{
    return View(new ErrorViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
}
```

```cshtml
@* Views/Home/Error.cshtml *@
@model ErrorViewModel
@{
    ViewData["Title"] = "Algo salió mal";
}

<h1>Algo salió mal</h1>
<p>No pudimos completar lo que pediste. El error quedó registrado y lo vamos a revisar.</p>
@* Nunca el detalle técnico (ex.Message, StackTrace): solo un código para buscarlo en logs/ *@
<p class="nota">Código para soporte: <code>@Model.RequestId</code></p>
<p><a asp-controller="Home" asp-action="Index">← Volver al inicio</a></p>
```

:::nota
Extra opcional: `app.UseStatusCodePagesWithReExecute("/Home/NoEncontrado", "?codigo={0}");` (después de `UseExceptionHandler`)
hace lo mismo con los 404: en vez de la página en blanco del navegador, una vista tuya con el layout. Si lo hacés, la acción
tiene que devolver la vista **y** dejar el código: `Response.StatusCode = codigo;`.
:::

### Paso 6 · Probarlo de verdad: la base en solo lectura

Romper la base a propósito es la mejor forma de ver el manejador en acción. Arrancá así (las comillas son necesarias por el `;`):

```bash
dotnet run --project AeroTaller -- "--ConnectionStrings:AeroTaller=Data Source=aerotaller.db;Mode=ReadOnly"
```

La línea de comandos **pisa** la cadena de `appsettings.json` (igual que hace el verificador con su base temporal).
Ahora la app puede leer pero no escribir:

1. `/Vuelos` y el tablero andan (solo leen).
2. Intentá reservar un asiento libre: las reglas pasan (leen), pero el `INSERT` falla → **«Algo salió mal»**, con un código.
3. Abrí `logs/aerotaller-AAAAMMDD.txt` y buscá el error:

```text
[ERR] Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware: An unhandled exception has occurred while executing the request. (trace 4bf92f35…)
Microsoft.Data.Sqlite.SqliteException (0x80004005): SQLite Error 8: 'attempt to write a readonly database'.
   at …
   at ReservaRepository.Insertar(Reserva reserva) in …/Repositorios/ReservaRepository.cs:line …
   at ReservaService.Reservar(…) in …/Servicios/ReservaService.cs:line …
   at ReservasController.Crear(…) in …/Controllers/ReservasController.cs:line …
[ERR] Serilog.AspNetCore.RequestLoggingMiddleware: HTTP POST /Reservas/Crear responded 500 in 27.8260 ms (trace 4bf92f35…)
```

4. **Del código al log.** El código de la página tiene la forma `00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00`.
   El **segundo** tramo (el de 32 caracteres) es el `TraceId`: buscalo en el archivo y aparecen todas las líneas de ese request.
   Así se trabaja en la vida real: el usuario te pasa el código, vos encontrás el `StackTrace` que a él no le mostraste.

Fijate: el **código 8** no es el 19 del `catch` del paso 2, así que ese catch **no lo atrapa** (el `when` da `false`). Es
exactamente lo que queremos: el servicio traduce lo que entiende y deja pasar lo que no.

Cortá con `Ctrl + C` y volvé a correr normal.

## 🔴 Breakpoints

:::breakpoint ReservaService.cs · Reservar, adentro del catch (SqliteException ex) when (…)
Hacé el choque del paso 2 (depurador + DB Browser). Cuando frene adentro del catch, en Variables mirá `ex.SqliteErrorCode` (19),
`ex.Message` (`UNIQUE constraint failed: Reserva.VueloId, Reserva.Asiento`). Seguí con F10 hasta el `throw` y después poné otro
breakpoint en el `catch (ReglaDeNegocioException ex)` del controlador: ahí mirá `ex.Campo` y `ex.InnerException`: la
`SqliteException` original viajó adentro.
:::

:::breakpoint ReservaRepository.cs · Insertar (con «All Exceptions» activado)
En el panel **Puntos de interrupción** de VS Code tildá **All Exceptions**, arrancá con la base en solo lectura y reservá.
El depurador frena donde **nace** la excepción. Mirá `$exception` en Variables (`SqliteErrorCode` vale 8) y la **Pila de
llamadas**: Repositorio ← Servicio ← Controlador. Con F5 vas a ver que no entra a ningún catch tuyo. Destildá «All Exceptions»
al terminar: frena hasta en las excepciones que .NET maneja solo.
:::

:::breakpoint HomeController.cs · Error
Con la misma prueba, frená en la acción `Error`. En Inspección (Watch) agregá `Request.Method`: es **POST**, porque se re-ejecuta
el request original. Y agregá
`HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>().Path`: es `/Reservas/Crear`, la ruta
donde explotó. Si quisieras mostrar algo distinto según dónde falló, de ahí lo sacarías.
:::

Preguntate:

- Si el `try` del paso 2 envolviera **todo** el método `Reservar`, ¿qué otras excepciones podría atrapar sin querer?
- Si sacás el `when`, ¿qué vería el usuario con la base en solo lectura? ¿Sería verdad?
- ¿Cuántas líneas `[ERR]` deja un error inesperado en el log? ¿Quién escribe cada una?

## 🔀 Alternativas

**¿Dónde se traduce el error de la base?**

| Opción | A favor | En contra | Cuándo |
|---|---|---|---|
| **El servicio atrapa `SqliteException` con `when`** (lo nuestro) | simple; el servicio sabe qué significa el error | el servicio conoce un detalle de SQLite (`using Microsoft.Data.Sqlite`) | proyectos con una sola base, como el parcial |
| El repositorio traduce a una excepción propia (`AsientoOcupadoException`) y el servicio atrapa esa | el servicio no sabe nada de SQLite; el repositorio falso puede lanzar la misma | una clase más; el repositorio tiene que distinguir qué restricción falló | cuando hay varias implementaciones de verdad (SQLite, SQL Server…) |
| Sin índice: solo la regla del servicio (`AsientosOcupados`) | nada que atrapar | **no resuelve el choque**: dos requests simultáneos pasan los dos | nunca, si el dato importa |

**¿Quién atrapa lo inesperado?**

| Opción | A favor | En contra |
|---|---|---|
| **`UseExceptionHandler("/Home/Error")`** (lo nuestro, MVC) | un solo lugar; página con el layout; el middleware ya registra el error | la vista no sabe qué tipo de error fue (salvo que lo busques en `IExceptionHandlerPathFeature`) |
| `IExceptionHandler` propio (`ManejadorGlobal`, temas 16–17) | elegís el código HTTP según el tipo de excepción; ideal para JSON (`ProblemDetails`) | pensado para Web API; si devuelve `true`, el log lo tenés que escribir vos |
| `try { … } catch (Exception ex) { … }` en cada acción | — | código repetido; una acción nueva sin el try queda desprotegida; tienta a tragarse errores |

## ⚠️ Errores típicos

- **Catch vacío o con valor de mentira** (`catch (Exception) { return 0; }`): el error desaparece, el usuario ve «Reserva
  confirmada» y no queda nada en el log. El repositorio **no atrapa**.
- **`throw ex;`** en vez de `throw;`: el `StackTrace` dice que el error nació en el catch. El analizador lo marca (CA2200).
- **`$"Reserva {reserva.Id} confirmada"` en el log**: se ve igual en pantalla, pero se pierde el dato estructurado (y el string
  se arma aunque el nivel esté filtrado). Plantilla con los valores aparte.
- **Loguear y relanzar** (`logger.LogError(ex, …); throw;`): el mismo error queda dos veces (tuyo y del manejador global).
  O se resuelve y se loguea, o se deja subir.
- **`[HttpGet]` en la acción `Error`**: los errores de un POST (como reservar) no llegan a la página de error. La acción va sin
  atributo de verbo.
- **Índice sin `WHERE Estado = 'Confirmada'`**: el 1C del AT1001 tiene una reserva **cancelada** en los datos de ejemplo.
  El mapa lo muestra libre, pero al reservarlo el índice salta y el usuario lee «Alguien acaba de reservar el 1C»… que es mentira.
- **Cambiar `aerotaller.sql` y no borrar `aerotaller.db`**: `CrearBaseSiNoExiste` ve que el archivo existe y no ejecuta el
  script. El índice no está, y el verificador (8 reservas simultáneas del mismo asiento) encuentra más de una.
