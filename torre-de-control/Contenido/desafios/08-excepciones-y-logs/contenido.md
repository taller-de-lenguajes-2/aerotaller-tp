## :target: Misión

Hay dos situaciones que pueden fallar en AeroTaller, y actualmente ninguna está bien resuelta.

**1 · El choque de asientos.** Dos personas en dos mostradores eligen el 20C del mismo vuelo **en el mismo instante**.
Ambas operaciones superan la regla «el asiento está libre» de `ReservaService` (en ese momento lo estaba), y ambas insertan.
Resultado: dos pasajeros con la misma tarjeta de embarque. La regla del servicio no es suficiente, porque entre la consulta
y la inserción transcurren unos milisegundos en los que otra operación puede intercalarse.

**2 · El error inesperado.** La base queda en solo lectura, el disco se llena, alguien borró una tabla. Actualmente el usuario ve
una página con el `StackTrace` (en Development) o una pantalla en blanco (en producción), y **el error pasa inadvertido**: no queda
registro de qué ocurrió ni cuándo.

Los aviones tienen una **caja negra**: registra todo, siempre, para que después se pueda reconstruir qué ocurrió.
Eso es lo que se construye en este desafío:

1. Un **índice único parcial** en la base: la validación definitiva queda a cargo de SQLite.
2. Un `catch` **con filtro** en `ReservaService` que convierte el choque en un mensaje de error claro junto al campo `Asiento`.
3. **Serilog** escribiendo en la consola y en `logs/aerotaller-AAAAMMDD.txt`.
4. `ILogger<T>` en los servicios, con **plantillas** (log estructurado).
5. `UseExceptionHandler("/Home/Error")` + una vista de error adecuada, con un código para buscar el problema en el log.
6. Una prueba concreta: la base en solo lectura.

## :book-open-text: Repaso

Una excepción que no se atrapa **sube por la pila de llamadas** (tema 16). En AeroTaller la pila de una reserva es:

```text
ReservasController.Crear → ReservaService.Reservar → ReservaRepository.Insertar → SqliteCommand.ExecuteScalar   ← excepción
```

La pregunta de este desafío no es «cómo se escribe un try/catch» (eso ya se conoce) sino **en qué nivel** va cada uno.
Regla: se atrapa donde se puede hacer algo útil con el error. Si no hay nada útil para hacer, se deja subir.

| Capa | ¿Atrapa? | Qué hace |
|---|---|---|
| Repositorio | **no** | propaga todas las excepciones. El `using var conexion` cierra la conexión de todos modos (equivale a un `finally` generado por el compilador) |
| Servicio | **solo lo que puede traducir** | `SqliteException` con código 19 (restricción) → `ReglaDeNegocioException` en el campo `Asiento`. Cualquier otra se propaga |
| Controlador | `ReglaDeNegocioException` | la agrega a `ModelState` (como ya se hace desde el desafío 6) |
| `UseExceptionHandler` | **todo lo demás** | lo registra en el log y muestra una página de error sin detalles técnicos |

Las herramientas del tema 16 que se usan:

- **Filtro `when`**: `catch (SqliteException ex) when (ex.SqliteErrorCode == 19)`. Si la condición da `false`, ese catch se
  omite y la excepción se propaga **intacta**.
- **Envolver con contexto**: `throw new ReglaDeNegocioException("Asiento", "…", ex)`. La original viaja dentro, como `InnerException`.
- **`throw;` y nunca `throw ex;`** (el segundo sobrescribe el `StackTrace`).
- **Nunca un catch vacío** ni un `catch (Exception)` que devuelve un valor falso.

Y las del tema 17:

- `ILogger<T>` llega por el constructor (inyección de dependencias, desafío 7). La `T` es la **categoría**: cada línea indica qué clase la escribió.
- Niveles: `Information` para el flujo normal (una reserva), `Warning` para lo inusual que no constituye un error (un choque de asientos),
  `Error` para lo que falló (eso lo registra `UseExceptionHandler` automáticamente).
- **Plantillas, no cadenas ya construidas**: `LogInformation("Reserva {ReservaId} confirmada", reserva.Id)`. Sin `$`, sin `+`.
- **Nunca** contraseñas, cadenas de conexión ni datos personales innecesarios (el DNI, por ejemplo).
- Serilog se conecta **detrás** de `ILogger`: el código de la aplicación no cambia, cambia el destino de cada línea.

:::catedra
En los temas [16](https://taller-de-lenguajes-2.github.io/clases/tema-16-excepciones/index.html) y
[17](https://taller-de-lenguajes-2.github.io/clases/tema-17-logs/index.html) el manejador global es para una **Web API**:
una clase `ManejadorGlobal : IExceptionHandler` que responde `ProblemDetails` (JSON) y registra el error con `LogError(ex, …)`.
En **MVC** el usuario espera una **página**, no un JSON, así que se adapta: `app.UseExceptionHandler("/Home/Error")` vuelve a
ejecutar el request contra la acción `HomeController.Error`, que muestra una vista. Además, no es necesario registrar el error manualmente:
con la forma de ruta (`"/Home/Error"`), el propio middleware registra la excepción completa como `[ERR]`, con categoría
`Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware`. (La novedad de .NET 10 que presenta la diapositiva —que la
excepción no se registra— aplica cuando un `IExceptionHandler` propio devuelve `true`.)
:::

### :lightbulb: Tips y recursos

**¿Dónde se traduce el error de la base?**

| Opción | A favor | En contra | Cuándo |
|---|---|---|---|
| **El servicio atrapa `SqliteException` con `when`** (la opción de este TP) | simple; la interpretación del error queda en el servicio | el servicio queda acoplado a un detalle de SQLite (`using Microsoft.Data.Sqlite`) | proyectos con una sola base, como el parcial |
| El repositorio traduce a una excepción propia (`AsientoOcupadoException`) y el servicio atrapa esa | el servicio no depende de SQLite; el repositorio falso puede lanzar la misma | una clase más; el repositorio debe distinguir qué restricción falló | cuando hay varias implementaciones reales (SQLite, SQL Server…) |
| Sin índice: solo la regla del servicio (`AsientosOcupados`) | nada que atrapar | **no resuelve el choque**: dos requests simultáneos superan la regla | nunca, si el dato importa |

**¿Dónde se atrapa lo inesperado?**

| Opción | A favor | En contra |
|---|---|---|
| **`UseExceptionHandler("/Home/Error")`** (la opción de este TP, MVC) | un solo lugar; página con el layout; el middleware ya registra el error | la vista no dispone del tipo de error (salvo que se consulte `IExceptionHandlerPathFeature`) |
| `IExceptionHandler` propio (`ManejadorGlobal`, temas 16–17) | permite elegir el código HTTP según el tipo de excepción; ideal para JSON (`ProblemDetails`) | pensado para Web API; si devuelve `true`, el log debe escribirse manualmente |
| `try { … } catch (Exception ex) { … }` en cada acción | — | código repetido; una acción nueva sin el try queda desprotegida; induce a ocultar errores |

## :code-xml: Paso a paso

### Paso 1 · El índice único parcial: la validación definitiva en la base

El script de la plantilla ya trae, después del `CREATE TABLE Reserva`, este índice:

```sql
-- Defensa en profundidad: aunque el servicio ya lo controla, la BASE no admite
-- dos reservas confirmadas en el mismo asiento del mismo vuelo.
CREATE UNIQUE INDEX UX_Reserva_AsientoConfirmado
    ON Reserva (VueloId, Asiento) WHERE Estado = 'Confirmada';
```

Hasta ahora no hacía falta: el servicio del desafío 6 ya rechaza un asiento ocupado. En este desafío se lo aprovecha para el
caso que el servicio **no puede** cubrir: dos personas que reservan el mismo asiento en el mismo instante.

¿Por qué **parcial** (con `WHERE`)? Porque una reserva **cancelada** libera el asiento: debe poder existir una
cancelada y una confirmada en el mismo 1C. Un índice único común (sin `WHERE`) lo impediría.

Probarlo en DB Browser for SQLite (pestaña **Ejecutar SQL**). El 3A del AT1001 (vuelo 2) ya está ocupado en los datos de ejemplo:

```sql
INSERT INTO Reserva (VueloId, PasajeroId, Asiento, Fecha, Estado)
VALUES (2, 5, '3A', '2026-01-01 10:00', 'Confirmada');
-- UNIQUE constraint failed: Reserva.VueloId, Reserva.Asiento

INSERT INTO Reserva (VueloId, PasajeroId, Asiento, Fecha, Estado)
VALUES (2, 5, '3A', '2026-01-01 10:00', 'Cancelada');
-- funciona: el índice solo considera las confirmadas
```

Después, presionar **Revertir cambios** para no dejar esa fila de prueba.

### Paso 2 · Atrapar el choque en el servicio

Primero, `ReglaDeNegocioException` debe poder llevar la excepción original dentro. Agregarle un tercer parámetro opcional:

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
            // Ambas operaciones superaron la regla "asiento libre", pero el índice único solo admite la primera
            throw new ReglaDeNegocioException("Asiento", $"El asiento {asiento} acaba de reservarse en otra operación. Debe elegirse otro asiento.", ex);
        }

        return reserva;
    }
}
```

El controlador **no cambia**: ya atrapa `ReglaDeNegocioException` y la agrega a `ModelState`. El usuario ve el error junto al
campo `Asiento`, como cualquier otra regla.

:::nota
¿Por qué no atrapar en el repositorio? Porque el repositorio no dispone de la información sobre **qué significa** el error para el usuario ni
en qué campo mostrarlo. ¿Y en el controlador? Porque quedaría acoplado a `SqliteException`, y con `OrigenDeDatos=Falso` (desafío 7)
esa excepción no existe. El servicio es la capa que puede interpretar que «restricción violada al insertar una reserva» = «el asiento acaba de ocuparse».
:::

**Provocar el choque.** Manualmente es casi imposible lograr la coincidencia en los milisegundos exactos; por eso se usa el depurador para
«congelar» un mostrador y DB Browser como el **otro** mostrador:

1. F5 con **AeroTaller**. Breakpoint en la línea `reserva.Id = reservas.Insertar(reserva);`.
2. Abrir `http://localhost:5210/Reservas/Crear?vueloId=5&asiento=20C` (el AT2020), elegir un pasajero y enviar.
   La ejecución se detiene en el breakpoint: la reserva **ya superó todas las reglas** y el 20C estaba libre.
3. Con la aplicación detenida, en DB Browser abrir `AeroTaller/aerotaller.db` y ejecutar (representa «el otro mostrador», que se adelanta):

   ```sql
   INSERT INTO Reserva (VueloId, PasajeroId, Asiento, Fecha, Estado)
   VALUES (5, 8, '20C', '2026-01-01 10:00', 'Confirmada');
   ```

   Presionar **Escribir cambios** (`Ctrl/Cmd + S`).
4. Volver a VS Code y presionar F10: el `INSERT` choca contra el índice y entra en el `catch`. Con F5, en el navegador aparece
   el error junto al asiento. En la base hay **una sola** reserva confirmada del 20C.

:::nota
¿Por qué no se usan dos navegadores y el breakpoint? Porque, cuando el depurador detiene la ejecución, se detiene **todo el proceso**: el segundo
request no avanza hasta que se libera el primero, y para entonces el 20C ya está ocupado (lo detecta la regla del servicio, no el
índice). El verificador sí lo prueba en condiciones reales: envía 8 reservas del mismo asiento a la vez, sin depurador, y cuenta
cuántas quedaron en la base.
:::

### Paso 3 · Serilog: que el log persista

Los proveedores de log que incluye .NET no escriben en archivos. Instalar Serilog:

```bash
cd AeroTaller
dotnet add package Serilog.AspNetCore
cd ..
```

El paquete incluye Serilog, los *sinks* (destinos) de consola y de archivo, y la lectura de la configuración desde `appsettings.json`.

En `appsettings.json`, **reemplazar** la sección `"Logging"` por la de Serilog (Serilog no lee `"Logging"`):

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
  se ejecuta la aplicación (`AeroTaller/`).
- `(trace {TraceId})` agrega a cada línea el identificador del request: con ese dato se localiza el error que vio un usuario (paso 6).

En `Program.cs`, inmediatamente después de crear el `builder`:

```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog reemplaza al logger por defecto y lee su configuración de appsettings.json
builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));
```

Ejecutar la aplicación y verificar que aparezca `AeroTaller/logs/`. `logs/` ya está en el `.gitignore`: los logs no se suben al repositorio.

### Paso 4 · ILogger<T> en los servicios: registrar lo que ocurre

Recibir el logger por el constructor (el contenedor lo resuelve sin registro adicional):

```csharp
public class ReservaService(
    IReservaRepository reservas,
    IVueloRepository vuelos,
    IPasajeroRepository pasajeros,
    ILogger<ReservaService> logger) : IReservaService
```

Y registrar los eventos del negocio, con **plantillas**:

```csharp
// Al final de Reservar: una reserva nueva es el flujo normal → Information
logger.LogInformation("Reserva {ReservaId} confirmada: vuelo {Numero}, asiento {Asiento}, pasajero {PasajeroId}",
    reserva.Id, vuelo.Numero, asiento, pasajero.Id);

// Dentro del catch del choque: inusual, pero no es un error de la aplicación → Warning (y SIN pasar ex: no es un bug)
logger.LogWarning("Choque de reservas: el asiento {Asiento} del vuelo {Numero} se ocupó recién", asiento, vuelo.Numero);

// En Cancelar
logger.LogInformation("Reserva {ReservaId} cancelada (vuelo {Numero}, asiento {Asiento})",
    reservaId, reserva.Vuelo.Numero, reserva.Asiento);
```

Hacer lo mismo en `PasajeroService` (registrado, actualizado, eliminado) y en `VueloService` (vuelo creado). En pasajeros,
**solo el Id**: el DNI y el email son datos personales y no son necesarios para reconstruir qué ocurrió.

```csharp
logger.LogInformation("Pasajero {PasajeroId} registrado", pasajero.Id);   // correcto
logger.LogInformation("Pasajero {Dni} registrado", pasajero.Dni);         // incorrecto: dato personal
```

Un detalle que completa el desafío 7: al final de `Program.cs`, antes de `app.Run()`, registrar el origen de datos con el que se inició la aplicación.

```csharp
app.Logger.LogInformation("AeroTaller arrancó con datos de origen {OrigenDeDatos}", origenDeDatos);
```

Hacer una reserva y abrir el archivo del día. Debe verse algo así:

```text
2026-11-16 12:37:59 [INF] ReservaService: Reserva 16 confirmada: vuelo AT1001, asiento 4C, pasajero 5 (trace 9f2c…)
2026-11-16 12:37:59 [INF] Serilog.AspNetCore.RequestLoggingMiddleware: HTTP POST /Reservas/Crear responded 302 in 9.5723 ms (trace 9f2c…)
```

(La segunda línea aparece después de completar el paso 5.)

### Paso 5 · El pipeline y la página de error

En `Program.cs`, después de `builder.Build()`, el orden **importa**:

```csharp
var app = builder.Build();

app.UseSerilogRequestLogging();           // 1 · una línea de log por request (envuelve a todo lo demás)
app.UseExceptionHandler("/Home/Error");   // 2 · si algo falla más adentro: se registra y se muestra la página de error
app.UseStaticFiles();
// … MapControllerRoute igual que antes …
```

`UseSerilogRequestLogging` va **antes** para que registre la respuesta que efectivamente se envió (el 500 final), y no la
excepción intermedia.

:::cuidado
La plantilla de `dotnet new mvc` coloca `UseExceptionHandler` dentro de `if (!app.Environment.IsDevelopment())`.
En AeroTaller se llama **siempre**: así puede verificarse su funcionamiento durante el desarrollo (y el verificador lo prueba en Development).
:::

La acción y su ViewModel:

```csharp
// ViewModels/: la pantalla de error solo recibe el código para buscar el request en el log
public record ErrorViewModel(string? RequestId);
```

```csharp
using System.Diagnostics;

// HomeController: UseExceptionHandler re-ejecuta el request contra esta acción cuando ocurre un error.
// El error ya quedó en el log; aquí solo se muestra un mensaje adecuado y el código para buscarlo.
// IMPORTANTE: sin [HttpGet]. Si el request que falló era un POST, se re-ejecuta como POST.
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
<p>No fue posible completar la operación. El error quedó registrado.</p>
@* Nunca el detalle técnico (ex.Message, StackTrace): solo un código para buscarlo en logs/ *@
<p class="nota">Código para soporte: <code>@Model.RequestId</code></p>
<p><a asp-controller="Home" asp-action="Index">← Volver al inicio</a></p>
```

:::nota
Extra opcional: `app.UseStatusCodePagesWithReExecute("/Home/NoEncontrado", "?codigo={0}");` (después de `UseExceptionHandler`)
hace lo mismo con los 404: en lugar de la página en blanco del navegador, una vista propia con el layout. Si se implementa, la
acción debe devolver la vista **y** conservar el código: `Response.StatusCode = codigo;`.
:::

### Paso 6 · Prueba concreta: la base en solo lectura

Provocar deliberadamente una falla en la base es la mejor forma de observar el funcionamiento del manejador. Ejecutar la
aplicación de esta forma (las comillas son necesarias por el `;`):

```bash
dotnet run --project AeroTaller -- "--ConnectionStrings:AeroTaller=Data Source=aerotaller.db;Mode=ReadOnly"
```

La línea de comandos **sobrescribe** la cadena de `appsettings.json` (del mismo modo que lo hace el verificador con su base temporal).
En este modo la aplicación puede leer pero no escribir:

1. `/Vuelos` y el tablero funcionan (solo leen).
2. Intentar reservar un asiento libre: las reglas se cumplen (solo leen), pero el `INSERT` falla → **«Algo salió mal»**, con un código.
3. Abrir `logs/aerotaller-AAAAMMDD.txt` y buscar el error:

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
   El **segundo** tramo (el de 32 caracteres) es el `TraceId`: al buscarlo en el archivo aparecen todas las líneas de ese request.
   Así se trabaja en un entorno real: el usuario informa el código, y con él se encuentra el `StackTrace` que no se le mostró.

Notar que el **código 8** no es el 19 del `catch` del paso 2, así que ese catch **no lo atrapa** (el `when` da `false`). Es
exactamente lo buscado: el servicio traduce los errores que corresponden a su regla de negocio y propaga el resto.

Detener con `Ctrl + C` y volver a ejecutar normalmente.
