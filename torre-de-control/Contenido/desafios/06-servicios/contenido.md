## 🎯 Misión

Mirá el POST de `Reservas/Crear` que escribiste en el desafío 5: recibe el formulario, consulta tres repositorios,
decide cinco reglas del negocio, traduce el ViewModel a una entidad y elige la vista. Es el piloto atendiendo el
check-in, cargando valijas y sirviendo café. Funciona, pero:

- Si mañana hay una Web API para reservar desde el celular, ¿copiás las cinco reglas?
- Si cambia una regla, ¿en cuántos controladores la buscás?

La solución es una **capa de servicios**: una clase cuyo trabajo es **decidir si algo se puede hacer**. El controlador
queda "flaco" (recibe, llama, elige la vista) y el repositorio sigue hablando SQL.

Al terminar este desafío:

1. `ReservaService`, `PasajeroService` y `VueloService` tienen **todas** las reglas del negocio, detrás de sus
   interfaces (`IReservaService`…).
2. Cuando una regla no se cumple, el servicio lanza `ReglaDeNegocioException` con el **campo** y el **mensaje**; el
   controlador la atrapa y la muestra al lado del input.
3. Se puede **cancelar** una reserva (`POST /Reservas/Cancelar/{id}`: cambia el estado, no borra).
4. Los pasajeros se pueden **editar** y **eliminar** (si no tienen reservas).
5. Los mensajes "Reserva cancelada." o "No se puede eliminar…" aparecen arriba de la página, después del redirect.

:::nota
Todo lo que funcionaba en el desafío 5 tiene que **seguir funcionando igual** después de mudar las reglas. Es un
*refactor*: el comportamiento no cambia, cambia dónde vive el código. Tus pruebas del desafío 5 (25A, 3A, Ana Gómez
repetida, AT0500 lleno, AT0999) son tu red: corrélas de nuevo al terminar el paso 4.
:::

## 📚 Repaso

Tres temas se juntan en este desafío:

**Tema 09 · Arquitectura en capas.** La diapositiva dibuja Controladores → **Servicios** → Repositorios → Datos, con dos
reglas: cada capa usa solo a la de abajo, y ninguna se saltea una capa. También dice que "en proyectos chicos, la capa de
servicios puede no estar" (es lo que hiciste hasta ahora). La `Recepcion` del ejemplo del tema 09 **es** un servicio:
recibe el repositorio por el constructor y decide la regla "no se da de alta un DNI que ya está cargado".

| Capa | Su trabajo | En AeroTaller | Lo que NO hace |
|---|---|---|---|
| Controlador | HTTP: recibir, validar la forma, elegir la vista | `ReservasController` | reglas del negocio, SQL |
| Servicio | decidir si se puede, coordinar repositorios | `ReservaService` | HTML, `ModelState`, SQL |
| Repositorio | guardar y traer, con SQL parametrizado | `ReservaRepository` | reglas, mensajes al usuario |

**Tema 04 · Interfaces.** Una interfaz es un **contrato**: dice *qué* se puede hacer, no *cómo*. El controlador va a
depender de `IReservaService`, no de `ReservaService`. Hoy hay una sola implementación; en el desafío 7 vas a ver
para qué sirve tener el contrato separado (y lo mismo con los repositorios).

**Tema 16 · Excepciones propias.** Una excepción propia hereda de `Exception`, su nombre termina en `Exception`, puede
llevar datos propios (como `Faltante` en `SaldoInsuficienteException`) y "tiene sentido cuando el error es del dominio y
alguien lo va a capturar por su tipo". Es exactamente nuestro caso: "ese asiento ya está ocupado" es un error del
dominio, y el controlador la va a atrapar por su tipo para mostrarla en el formulario.

Lo que vimos en clase: [tema 04](https://taller-de-lenguajes-2.github.io/clases/tema-04-interfaces-y-clases-abstractas/index.html),
[tema 09](https://taller-de-lenguajes-2.github.io/clases/tema-09-repositorios/index.html) y
[tema 16](https://taller-de-lenguajes-2.github.io/clases/tema-16-excepciones/index.html).

## 💻 Paso a paso

### Paso 1 · La excepción del negocio

Carpeta nueva `Excepciones/`. La excepción lleva, además del mensaje, **el nombre del campo** del formulario donde hay
que mostrar el error:

```csharp
// Excepciones/ReglaDeNegocioException.cs
// La lanza un SERVICIO cuando una regla del negocio no se cumple ("ese asiento ya está ocupado").
public class ReglaDeNegocioException(string campo, string mensaje) : Exception(mensaje)
{
    // "VueloId", "Asiento", "Dni"… o "" si es un error general (va al asp-validation-summary)
    public string Campo { get; } = campo;
}
```

Fijate el constructor primario: `: Exception(mensaje)` le pasa el mensaje a la clase base, y así `ex.Message` funciona
como en cualquier excepción. Es el mismo molde que `SaldoInsuficienteException` del tema 16.

:::nota
En el desafío 8 le vas a agregar un tercer parámetro opcional (la excepción original que la causó). Por ahora alcanza
con dos.
:::

### Paso 2 · El contrato: las interfaces de los servicios

Carpeta nueva `Servicios/`. Primero el **qué** (los contratos). Podés ponerlas juntas en `Servicios/Interfaces.cs` o
una por archivo:

```csharp
// Servicios/Interfaces.cs
// Si una regla no se cumple, el servicio lanza ReglaDeNegocioException.

public interface IReservaService
{
    Reserva Reservar(int vueloId, int pasajeroId, string asiento);
    void Cancelar(int reservaId);
}

public interface IPasajeroService
{
    int Registrar(Pasajero pasajero);
    void Actualizar(Pasajero pasajero);
    void Eliminar(int id);
}

public interface IVueloService
{
    int Crear(string numero, int origenId, int destinoId, DateTime salida, int duracionMinutos, int filas, decimal precio);
}
```

Mirá lo que **no** aparece en las firmas: ningún `ReservaCrearViewModel`, ningún `ModelStateDictionary`, ningún
`IActionResult`. El servicio recibe datos del dominio (ids, textos, entidades) y no sabe si lo llama un controlador MVC,
una Web API o un programa de consola.

### Paso 3 · `ReservaService`: mudar las reglas

Ahora el **cómo**. Las cinco reglas que tenías en el controlador se mudan acá, en el mismo orden. La diferencia: en vez
de `ModelState.AddModelError(nameof(modelo.Asiento), "…")`, el servicio **lanza**
`new ReglaDeNegocioException("Asiento", "…")`. La primera regla que falla corta todo: las siguientes ni se evalúan.

Por ahora el servicio recibe los repositorios **concretos** (como los controladores): las interfaces de los
repositorios llegan en el desafío 7.

Intentalo: abrí tu controlador del desafío 5 al lado y mudá regla por regla.

:::solucion Ver ReservaService completo
```csharp
// Servicios/ReservaService.cs
public class ReservaService(
    ReservaRepository reservas,
    VueloRepository vuelos,
    PasajeroRepository pasajeros) : IReservaService
{
    public Reserva Reservar(int vueloId, int pasajeroId, string asiento)
    {
        asiento = asiento.Trim().ToUpperInvariant();          // "12a " → "12A"

        Vuelo vuelo = vuelos.Obtener(vueloId)
            ?? throw new ReglaDeNegocioException("VueloId", "Ese vuelo no existe.");
        Pasajero pasajero = pasajeros.Obtener(pasajeroId)
            ?? throw new ReglaDeNegocioException("PasajeroId", "Ese pasajero no existe.");

        // Regla 1: no se reserva un vuelo que ya salió
        if (vuelo.YaSalio)
            throw new ReglaDeNegocioException("VueloId", $"El vuelo {vuelo.Numero} ya salió.");

        // Regla 2: el asiento tiene que existir en ESTE avión
        if (!vuelo.TieneAsiento(asiento))
            throw new ReglaDeNegocioException("Asiento",
                $"El asiento {asiento} no existe en este avión (filas 1 a {vuelo.Filas}, letras A a F).");

        // Regla 3: sin overbooking
        if (reservas.ContarConfirmadas(vueloId) >= vuelo.Capacidad)
            throw new ReglaDeNegocioException("VueloId", $"El vuelo {vuelo.Numero} está completo.");

        // Regla 4: el asiento tiene que estar libre
        if (reservas.AsientosOcupados(vueloId).Contains(asiento))
            throw new ReglaDeNegocioException("Asiento", $"El asiento {asiento} ya está ocupado.");

        // Regla 5: un pasajero no reserva dos veces el mismo vuelo
        if (reservas.PasajeroTieneReserva(vueloId, pasajeroId))
            throw new ReglaDeNegocioException("PasajeroId", $"{pasajero.Nombre} ya tiene una reserva en este vuelo.");

        var reserva = new Reserva
        {
            Vuelo = vuelo,
            Pasajero = pasajero,
            Asiento = asiento,
            Fecha = DateTime.Now,
            Estado = EstadoReserva.Confirmada
        };
        reserva.Id = reservas.Insertar(reserva);
        return reserva;
    }

    public void Cancelar(int reservaId)
    {
        Reserva reserva = reservas.Obtener(reservaId)
            ?? throw new ReglaDeNegocioException("", "Esa reserva no existe.");

        if (reserva.Estado == EstadoReserva.Cancelada)
            throw new ReglaDeNegocioException("", "La reserva ya estaba cancelada.");
        if (reserva.Vuelo.YaSalio)
            throw new ReglaDeNegocioException("", $"El vuelo {reserva.Vuelo.Numero} ya salió: no se puede cancelar.");

        reservas.CambiarEstado(reservaId, EstadoReserva.Cancelada);   // UPDATE, no DELETE
    }
}
```
:::

`?? throw` es una **cláusula de guarda** en una línea: si `Obtener` devuelve `null`, se lanza la excepción ahí mismo
("lanzar temprano", tema 16). Y como el campo viaja como texto (`"VueloId"`), tiene que coincidir **letra por letra**
con la propiedad del ViewModel: el servicio no conoce `ReservaCrearViewModel`, así que no puede usar `nameof`.

### Paso 4 · Registrar el servicio y adelgazar el controlador

En `Program.cs`, después de los repositorios:

```csharp
// Servicios: las reglas del negocio. "Cuando alguien pida IReservaService, creá un ReservaService"
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IPasajeroService, PasajeroService>();
builder.Services.AddScoped<IVueloService, VueloService>();
```

¿Cómo arma el contenedor un `ReservaService` si necesita tres repositorios? Mira su constructor, ve que pide
`ReservaRepository`, `VueloRepository` y `PasajeroRepository`, y como esos ya están registrados (con tus
`AddScoped(_ => new …Repository(cadenaConexion))`), los crea y se los pasa. Nadie escribe `new ReservaService(…)`.

El controlador pide el **contrato** y queda así:

```csharp
public class ReservasController(
    ReservaRepository reservas,
    VueloRepository vuelos,
    PasajeroRepository pasajeros,
    IReservaService reservaService) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Crear(ReservaCrearViewModel modelo)
    {
        // 1 · La forma de los campos la siguen controlando los atributos del ViewModel
        if (!ModelState.IsValid)
        {
            CargarListas(modelo);
            return View(modelo);
        }

        // 2 · Las reglas las decide el servicio
        try
        {
            Reserva reserva = reservaService.Reservar(modelo.VueloId!.Value, modelo.PasajeroId!.Value, modelo.Asiento);
            TempData["Mensaje"] = $"Reserva confirmada: {reserva.Vuelo.Numero}, asiento {reserva.Asiento}.";
            return RedirectToAction("Detalle", "Vuelos", new { id = reserva.Vuelo.Id });
        }
        catch (ReglaDeNegocioException ex)
        {
            // El servicio dice en qué campo está el problema: el error aparece al lado de ese input
            ModelState.AddModelError(ex.Campo, ex.Message);
            CargarListas(modelo);
            return View(modelo);
        }
    }
    // … Index, Crear (GET) y CargarListas quedan como estaban
}
```

¿Y los repositorios que quedaron en el constructor? Los usan `Index` y `CargarListas` para **leer** (listar vuelos y
pasajeros). Leer para mostrar no tiene reglas; lo que **cambia datos** pasa por el servicio.

Borrá del controlador las reglas que mudaste: si queda una copia en cada lado, el día que la regla cambie vas a
cambiar una sola. Una regla, un lugar (DRY, tema 13).

Corré tus pruebas del desafío 5: mismo error, mismo campo. Si alguna dejó de mostrarse, revisá el nombre del campo.

### Paso 5 · `PasajeroService` y `VueloService`

Mismo movimiento con los otros dos formularios:

- `PasajeroService.Registrar`: la regla del DNI repetido (`ExisteDni`) que tenías en `PasajerosController.Crear`.
- `PasajeroService.Actualizar` (para el paso 7): el DNI no puede ser de **otro** pasajero (`ExisteDni(dni, excepto: id)`).
- `PasajeroService.Eliminar` (para el paso 7): no se borra un pasajero que tiene reservas.
- `VueloService.Crear`: origen ≠ destino, que los aeropuertos existan y número único.

:::solucion Ver PasajeroService y VueloService
```csharp
// Servicios/PasajeroService.cs
public class PasajeroService(
    PasajeroRepository pasajeros,
    ReservaRepository reservas) : IPasajeroService
{
    public int Registrar(Pasajero pasajero)
    {
        if (pasajeros.ExisteDni(pasajero.Dni))
            throw new ReglaDeNegocioException("Dni", $"Ya hay un pasajero con el DNI {pasajero.Dni}.");

        pasajero.Id = pasajeros.Insertar(pasajero);
        return pasajero.Id;
    }

    public void Actualizar(Pasajero pasajero)
    {
        // "excepto": al editar, el propio pasajero no cuenta como duplicado
        if (pasajeros.ExisteDni(pasajero.Dni, excepto: pasajero.Id))
            throw new ReglaDeNegocioException("Dni", $"Ya hay otro pasajero con el DNI {pasajero.Dni}.");

        if (!pasajeros.Actualizar(pasajero))
            throw new ReglaDeNegocioException("", "Ese pasajero no existe.");
    }

    public void Eliminar(int id)
    {
        // La regla se controla ANTES de ir a la base. Si se escapara, la clave foránea haría de red de seguridad.
        int cantidad = reservas.ListarPorPasajero(id).Count;
        if (cantidad > 0)
            throw new ReglaDeNegocioException("", $"No se puede eliminar: el pasajero tiene {cantidad} reserva(s).");

        if (!pasajeros.Eliminar(id))
            throw new ReglaDeNegocioException("", "Ese pasajero no existe.");
    }
}

// Servicios/VueloService.cs
public class VueloService(
    VueloRepository vuelos,
    AeropuertoRepository aeropuertos) : IVueloService
{
    public int Crear(string numero, int origenId, int destinoId, DateTime salida, int duracionMinutos, int filas, decimal precio)
    {
        numero = numero.Trim().ToUpperInvariant();

        if (origenId == destinoId)
            throw new ReglaDeNegocioException("DestinoId", "El destino tiene que ser distinto del origen.");

        Aeropuerto origen = aeropuertos.Obtener(origenId)
            ?? throw new ReglaDeNegocioException("OrigenId", "Ese aeropuerto no existe.");
        Aeropuerto destino = aeropuertos.Obtener(destinoId)
            ?? throw new ReglaDeNegocioException("DestinoId", "Ese aeropuerto no existe.");

        if (vuelos.ExisteNumero(numero))
            throw new ReglaDeNegocioException("Numero", $"Ya existe un vuelo {numero}.");

        var vuelo = new Vuelo
        {
            Numero = numero,
            Origen = origen,
            Destino = destino,
            Salida = salida,
            DuracionMinutos = duracionMinutos,
            Filas = filas,
            Precio = precio
        };
        return vuelos.Insertar(vuelo);
    }
}
```
:::

Los controladores quedan con el mismo molde que `Reservas/Crear`: `if (!ModelState.IsValid)` → `try` { servicio,
`TempData`, redirect } → `catch (ReglaDeNegocioException ex)` { `AddModelError`, recargar listas, misma vista }.
Por ejemplo, en `VuelosController.Crear` (POST):

```csharp
try
{
    // Pasó [Required]: acá los int? ya tienen valor
    int id = vueloService.Crear(modelo.Numero, modelo.OrigenId!.Value, modelo.DestinoId!.Value,
        modelo.Salida!.Value, modelo.DuracionMinutos!.Value, modelo.Filas!.Value, modelo.Precio!.Value);

    TempData["Mensaje"] = $"Vuelo {modelo.Numero.ToUpperInvariant()} creado.";
    return RedirectToAction(nameof(Detalle), new { id });
}
catch (ReglaDeNegocioException ex)
{
    ModelState.AddModelError(ex.Campo, ex.Message);
}

modelo.Aeropuertos = ListaDeAeropuertos();      // algo falló: las listas no vuelven en el POST
return View(modelo);
```

### Paso 6 · Cancelar una reserva

El contrato pide `POST /Reservas/Cancelar/{id}`, que **no borra**: cambia `Estado` a `'Cancelada'`. Así queda la
historia (quién tuvo ese asiento) y el asiento vuelve a estar libre, porque `AsientosOcupados` solo cuenta las
confirmadas.

Primero, el ViewModel del listado sabe si una reserva se puede cancelar (para mostrar o no el botón):

```csharp
// en ReservaListadoViewModel
public bool PuedeCancelarse { get; } = reserva.Estado == EstadoReserva.Confirmada && !reserva.Vuelo.YaSalio;
```

La acción:

```csharp
// POST /Reservas/Cancelar/7
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Cancelar(int id)
{
    try
    {
        reservaService.Cancelar(id);
        TempData["Mensaje"] = "Reserva cancelada.";
    }
    catch (ReglaDeNegocioException ex)
    {
        // No hay formulario donde mostrarlo: el mensaje viaja en TempData hasta el próximo request
        TempData["Error"] = ex.Message;
    }
    return RedirectToAction(nameof(Index));
}
```

En `Views/Reservas/Index.cshtml`, una columna más con un **formulario** (no un link: un link es GET, y un GET nunca
cambia datos):

```cshtml
<td>
    @if (reserva.PuedeCancelarse)
    {
        <form asp-action="Cancelar" asp-route-id="@reserva.Id" method="post"
              onsubmit="return confirm('¿Cancelar la reserva?');">
            <button type="submit" class="peligro">Cancelar</button>
        </form>
    }
</td>
```

El tag helper `<form>` con `method="post"` agrega solo el campo oculto del token antiforgery: por eso
`[ValidateAntiForgeryToken]` no rechaza este POST.

**¿Y dónde se ve el mensaje?** Después del redirect hay **otro** request (el GET del `Index`). `ViewData` no
sobrevive al cambio de request; `TempData` sí: dura hasta que alguien lo lee. Lo lee el layout, así sirve para
cualquier pantalla. En `Views/Shared/_Layout.cshtml`, adentro del `<main>` y antes de `@RenderBody()`:

```cshtml
@* Mensajes que vienen de un Redirect (TempData sobrevive exactamente un request más) *@
@if (TempData["Mensaje"] is string mensaje)
{
    <p class="mensaje ok">@mensaje</p>
}
@if (TempData["Error"] is string error)
{
    <p class="mensaje error">@error</p>
}
```

Las clases `mensaje ok` y `mensaje error` ya tienen estilo en `sitio.css`.

:::catedra
`TempData` no está en las diapositivas. Es parte del mismo tema que la sesión (tema 15, "estado entre requests"): por
defecto MVC lo guarda en una **cookie** que se borra al leerla. La alternativa sin `TempData` sería pasar el mensaje en
la URL (`/Reservas?mensaje=…`), pero queda feo, se puede falsificar y se repite si recargás.
:::

### Paso 7 · Editar y eliminar pasajeros

**Editar** usa el mismo formulario que crear. Sumale a `PasajeroFormViewModel` un `Id` opcional (null = nuevo) y un
constructor para cargarlo desde la entidad:

```csharp
public class PasajeroFormViewModel
{
    public int? Id { get; set; }                       // null = alta; con valor = edición
    public bool EsNuevo => Id is null;

    // … Nombre, Dni y Email con sus atributos, como en el desafío 5

    public PasajeroFormViewModel() { }                 // el model binding necesita este

    // Entidad → ViewModel (para mostrar el formulario de edición lleno)
    public PasajeroFormViewModel(Pasajero pasajero)
    {
        Id = pasajero.Id;
        Nombre = pasajero.Nombre;
        Dni = pasajero.Dni;
        Email = pasajero.Email;
    }

    // ViewModel → entidad
    public Pasajero APasajero() => new()
    {
        Id = Id ?? 0,
        Nombre = Nombre.Trim(),
        Dni = Dni,
        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim()
    };
}
```

Renombrá `Views/Pasajeros/Crear.cshtml` a `Formulario.cshtml` y hacé que el `<form>` apunte a una acción u otra:

```cshtml
@model PasajeroFormViewModel
@{
    ViewData["Title"] = Model.EsNuevo ? "Nuevo pasajero" : "Editar pasajero";
}

<h1>@ViewData["Title"]</h1>

<form asp-action="@(Model.EsNuevo ? "Crear" : "Editar")" asp-route-id="@Model.Id" method="post" class="formulario">
    @* … los mismos campos del desafío 5 … *@
</form>
```

El controlador (`PasajerosController` ahora pide también `IPasajeroService pasajeroService`):

```csharp
[HttpGet]
public IActionResult Crear() => View("Formulario", new PasajeroFormViewModel());

[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Crear(PasajeroFormViewModel modelo)
{
    if (!ModelState.IsValid) return View("Formulario", modelo);

    try
    {
        int id = pasajeroService.Registrar(modelo.APasajero());
        TempData["Mensaje"] = "Pasajero registrado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }
    catch (ReglaDeNegocioException ex)
    {
        ModelState.AddModelError(ex.Campo, ex.Message);
        return View("Formulario", modelo);
    }
}

[HttpGet]
public IActionResult Editar(int id)
{
    Pasajero? pasajero = pasajeros.Obtener(id);
    if (pasajero is null) return NotFound();

    return View("Formulario", new PasajeroFormViewModel(pasajero));
}

[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Editar(int id, PasajeroFormViewModel modelo)
{
    modelo.Id = id;     // el Id lo manda la ruta, no un input que se pueda tocar
    if (!ModelState.IsValid) return View("Formulario", modelo);

    try
    {
        pasajeroService.Actualizar(modelo.APasajero());
        TempData["Mensaje"] = "Cambios guardados.";
        return RedirectToAction(nameof(Detalle), new { id });
    }
    catch (ReglaDeNegocioException ex)
    {
        ModelState.AddModelError(ex.Campo, ex.Message);
        return View("Formulario", modelo);
    }
}
```

**Eliminar** es un POST (como cancelar) que no tiene formulario donde mostrar el error: usa `TempData["Error"]`.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Eliminar(int id)
{
    try
    {
        pasajeroService.Eliminar(id);
        TempData["Mensaje"] = "Pasajero eliminado.";
        return RedirectToAction(nameof(Index));
    }
    catch (ReglaDeNegocioException ex)
    {
        TempData["Error"] = ex.Message;
        return RedirectToAction(nameof(Detalle), new { id });
    }
}
```

En `Views/Pasajeros/Detalle.cshtml`, un link "Editar" (`<a class="boton" asp-action="Editar" asp-route-id="@Model.Id">`)
y un `<form asp-action="Eliminar" asp-route-id="@Model.Id" method="post">` con su botón, igual que el de cancelar.
Probá eliminar a Ana Gómez (tiene reservas: error en rojo) y a un pasajero que hayas creado vos sin reservas (se borra).

:::nota
En el desafío 9, eliminar pasajeros va a quedar solo para el rol Admin. Por ahora el botón lo ve cualquiera.
:::

## 🔴 Breakpoints

:::breakpoint ReservasController.cs · Crear (POST) · la línea de reservaService.Reservar(…)
Cuando frene, en **Inspección** agregá `reservaService.GetType().Name`: la variable es de tipo `IReservaService`, pero
¿qué clase es el objeto de verdad? Apretá **F11** (entrar): ¿a qué archivo saltaste? Mirá la **Pila de llamadas**:
`ReservaService.Reservar` arriba, `ReservasController.Crear` justo abajo. Es el viaje Controlador → Servicio.
:::

:::breakpoint ReservaService.cs · Reservar · la primera línea
Reservá un asiento en el AT0500. En **Inspección** agregá `reservas.ContarConfirmadas(vueloId)` y `vuelo.Capacidad`
(el panel puede ejecutar métodos: esa línea hace una consulta a la base). Seguí con **F10**: ¿en qué `throw` sale?
Cuando lanza, apretá F10 otra vez: ¿a qué línea del controlador vuelve?
:::

:::breakpoint ReservasController.cs · Cancelar
Cancelá una reserva. Cuando frene, abrí F12 en el navegador, pestaña **Red**, y apretá F5 en VS Code. Vas a ver dos
requests: un `POST /Reservas/Cancelar/…` que responde **302** y un `GET /Reservas` que responde 200. El mensaje
verde viajó del primero al segundo en una cookie (`.AspNetCore.Mvc.CookieTempDataProvider`): buscala en la pestaña
**Aplicación → Cookies**. Recargá la página: ¿el mensaje sigue?
:::

Preguntate:

- Si sacás `IReservaService` del constructor del controlador y escribís `new ReservaService(…)` adentro de la acción,
  ¿qué le tendrías que pasar? ¿De dónde lo sacarías?
- ¿Qué pasa en la pantalla si el servicio lanza `new ReglaDeNegocioException("Vuelo", "…")` (sin el `Id`)?

## 🔀 Alternativas

**¿Cómo avisa el servicio que una regla no se cumple?**

| Forma | Cómo se ve | Ventaja | Desventaja | Cuándo |
|---|---|---|---|---|
| Excepción propia | `throw new ReglaDeNegocioException("Asiento", "…")` | el camino feliz queda limpio; nadie puede "olvidarse" de mirar el error | un error por vez; lanzar es más lento que un `if` | **la del TP**; reglas del dominio que alguien atrapa por su tipo |
| Objeto resultado | `ResultadoReserva { Ok, Campo, Mensaje, Reserva }` | sin excepciones; puede juntar varios errores | cada llamada tiene que acordarse de preguntar `if (!r.Ok)` | errores muy frecuentes o cuando querés mostrar todos juntos |
| Patrón Try | `bool TryReservar(…, out string error)` | igual que `int.TryParse` (tema 16) | con varios datos de salida se vuelve incómodo | operaciones chicas con un solo error posible |
| Sin servicio | `AddModelError` en el controlador (desafío 5) | menos clases | reglas atadas a MVC; no se reusan | proyectos muy chicos (el tema 09 lo admite) |

:::catedra
El tema 16 dice dos cosas que parecen chocar: "las excepciones son para lo excepcional" (por eso `TryParse` en vez de
atrapar `FormatException`) y que una excepción propia "tiene sentido cuando el error es del dominio y alguien lo va a
capturar por su tipo". Una regla del negocio violada entra en la segunda: no es un error de tipeo que se chequea antes,
es el dominio diciendo "no". La validación de **forma** (el usuario tipeó mal) sigue sin excepciones: atributos y
`ModelState`.
:::

**¿Quién trae los datos para mostrar?**

| Forma | Ventaja | Desventaja |
|---|---|---|
| El controlador lee del repositorio y escribe a través del servicio (lo de este desafío) | sin métodos de "pasamanos" en el servicio | el controlador depende de repositorios y de servicios |
| Todo pasa por el servicio (`reservaService.Listar()`) | el controlador solo conoce servicios; capas estrictas | muchos métodos que solo llaman al repositorio |

## ⚠️ Errores típicos

- **"Unable to resolve service for type 'IReservaService' while attempting to activate 'ReservasController'"**:
  falta `AddScoped<IReservaService, ReservaService>()`, o registraste `AddScoped<ReservaService>()` (la clase) y el
  controlador pide la interfaz. Lo que se pide tiene que ser lo que se registró.
- **`catch (Exception ex)` en el controlador**: también atrapa tus bugs (`NullReferenceException`, un SQL mal escrito) y
  se los muestra al usuario como si fueran una regla. Atrapá **solo** `ReglaDeNegocioException`. Y nunca un `catch` vacío.
- **El campo de la excepción no coincide con el ViewModel** (`"Vuelo"` en vez de `"VueloId"`): `ModelState` queda
  inválido, pero el error no aparece en ningún lado. Con `""` va al resumen (`ModelOnly`).
- **El servicio conoce MVC**: recibe un `ReservaCrearViewModel`, el `ModelState` o devuelve `IActionResult`. Si en
  `Servicios/` aparece un `using Microsoft.AspNetCore.Mvc`, algo se coló.
- **Cancelar o eliminar con un link** (`<a href="/Reservas/Cancelar/7">`): la acción es `[HttpPost]`, así que el link
  responde 405 (*Method Not Allowed*). Y está bien que así sea: un GET nunca cambia datos.
- **"Could not create an instance of type 'PasajeroFormViewModel'. Model bound complex types must … have a parameterless
  constructor"**: le agregaste el constructor que recibe un `Pasajero` y te olvidaste del vacío. El binding lo necesita.
- **Eliminar sin la regla**: SQLite te frena con *SQLite Error 19: 'FOREIGN KEY constraint failed'*, como una
  excepción sin atrapar. La clave foránea es la última red; el mensaje amable lo da el servicio.
