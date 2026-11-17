## :target: Misión

Considerar el POST de `Reservas/Crear` escrito en el desafío 5: recibe el formulario, consulta tres repositorios,
evalúa cinco reglas del negocio, convierte el ViewModel en una entidad y selecciona la vista. Equivale a un piloto que,
además de volar, atiende el check-in, carga el equipaje y sirve el café. Funciona, pero tiene dos limitaciones:

- Si se agregara una Web API para reservar desde el celular, habría que copiar las cinco reglas.
- Si cambiara una regla, habría que buscarla en cada controlador que la aplica.

La solución es una **capa de servicios**: una clase cuya responsabilidad es **determinar si una operación está permitida**.
El controlador queda "delgado" (recibe la solicitud, invoca al servicio y selecciona la vista) y el repositorio sigue a
cargo del SQL.

Al terminar este desafío:

1. `ReservaService`, `PasajeroService` y `VueloService` contienen **todas** las reglas del negocio, detrás de sus
   interfaces (`IReservaService`…).
2. Cuando una regla no se cumple, el servicio lanza `ReglaDeNegocioException` con el **campo** y el **mensaje**; el
   controlador la captura y la muestra al lado del input.
3. Se puede **cancelar** una reserva (`POST /Reservas/Cancelar/{id}`: cambia el estado, no borra).
4. Los pasajeros se pueden **editar** y **eliminar** (si no tienen reservas).
5. Los mensajes "Reserva cancelada." o "No se puede eliminar…" aparecen arriba de la página, después del redirect.

:::nota
Todo lo que funcionaba en el desafío 5 debe **seguir funcionando igual** después de trasladar las reglas. Es un
*refactor*: el comportamiento no cambia, cambia la ubicación del código. Las pruebas del desafío 5 (25A, 3A, Ana Gómez
repetida, AT0500 lleno, AT0999) funcionan como red de seguridad: deben ejecutarse de nuevo al terminar el paso 4.
:::

## :book-open-text: Repaso

Tres temas confluyen en este desafío:

**Tema 09 · Arquitectura en capas.** La diapositiva muestra Controladores → **Servicios** → Repositorios → Datos, con dos
reglas: cada capa usa solo a la de abajo, y ninguna omite una capa. También indica que "en proyectos chicos, la capa
de servicios puede no estar" (es lo que se hizo hasta ahora). La `Recepcion` del ejemplo del tema 09 **es** un
servicio: recibe el repositorio por el constructor y aplica la regla "no se da de alta un DNI que ya está cargado".

| Capa | Su trabajo | En AeroTaller | Lo que NO hace |
|---|---|---|---|
| Controlador | HTTP: recibir, validar la forma, elegir la vista | `ReservasController` | reglas del negocio, SQL |
| Servicio | validar si la operación está permitida, coordinar repositorios | `ReservaService` | HTML, `ModelState`, SQL |
| Repositorio | guardar y obtener datos, con SQL parametrizado | `ReservaRepository` | reglas, mensajes al usuario |

**Tema 04 · Interfaces.** Una interfaz es un **contrato**: indica *qué* se puede hacer, no *cómo*. El controlador
dependerá de `IReservaService`, no de `ReservaService`. Por ahora hay una sola implementación; en el desafío 7 se verá
la utilidad de tener el contrato separado (y lo mismo con los repositorios).

**Tema 16 · Excepciones propias.** Una excepción propia hereda de `Exception`, su nombre termina en `Exception`, puede
llevar datos propios (como `Faltante` en `SaldoInsuficienteException`) y "tiene sentido cuando el error es del dominio y
alguien lo va a capturar por su tipo". Es exactamente este caso: "ese asiento ya está ocupado" es un error del dominio,
y el controlador la captura por su tipo para mostrarla en el formulario.

Material de clase: [tema 04](https://taller-de-lenguajes-2.github.io/clases/tema-04-interfaces-y-clases-abstractas/index.html),
[tema 09](https://taller-de-lenguajes-2.github.io/clases/tema-09-repositorios/index.html) y
[tema 16](https://taller-de-lenguajes-2.github.io/clases/tema-16-excepciones/index.html).

### :lightbulb: Tips y recursos

**¿Cómo informa el servicio que una regla no se cumple?**

| Forma | Cómo se ve | Ventaja | Desventaja | Cuándo |
|---|---|---|---|---|
| Excepción propia | `throw new ReglaDeNegocioException("Asiento", "…")` | el camino feliz queda limpio; el error no puede pasarse por alto inadvertidamente | un error por vez; lanzar es más lento que un `if` | **la del TP**; reglas del dominio que se capturan por su tipo |
| Objeto resultado | `ResultadoReserva { Ok, Campo, Mensaje, Reserva }` | sin excepciones; puede reunir varios errores | cada llamada debe verificar `if (!r.Ok)` | errores muy frecuentes o cuando se quieren mostrar todos juntos |
| Patrón Try | `bool TryReservar(…, out string error)` | igual que `int.TryParse` (tema 16) | con varios datos de salida se vuelve incómodo | operaciones pequeñas con un solo error posible |
| Sin servicio | `AddModelError` en el controlador (desafío 5) | menos clases | reglas acopladas a MVC; no se reutilizan | proyectos muy pequeños (el tema 09 lo admite) |

:::catedra
El tema 16 plantea dos ideas que parecen contradictorias: "las excepciones son para lo excepcional" (por eso `TryParse` en
lugar de capturar `FormatException`) y que una excepción propia "tiene sentido cuando el error es del dominio y alguien
lo va a capturar por su tipo". Una regla del negocio violada corresponde al segundo caso: no es un error de formato que se
verifica de antemano, sino un rechazo propio del dominio. La validación de **forma** (el usuario escribió mal un dato) sigue sin
excepciones: atributos y `ModelState`.
:::

**¿Qué capa obtiene los datos para mostrar?**

| Forma | Ventaja | Desventaja |
|---|---|---|
| El controlador lee del repositorio y escribe a través del servicio (lo de este desafío) | sin métodos en el servicio que solo delegan en el repositorio | el controlador depende de repositorios y de servicios |
| Todo pasa por el servicio (`reservaService.Listar()`) | el controlador solo depende de servicios; capas estrictas | muchos métodos que solo llaman al repositorio |

## :code-xml: Paso a paso

### Paso 1 · La excepción del negocio

Carpeta nueva `Excepciones/`. La excepción lleva, además del mensaje, **el nombre del campo** del formulario donde debe
mostrarse el error:

```csharp
// Excepciones/ReglaDeNegocioException.cs
// La lanza un SERVICIO cuando una regla del negocio no se cumple ("ese asiento ya está ocupado").
public class ReglaDeNegocioException(string campo, string mensaje) : Exception(mensaje)
{
    // "VueloId", "Asiento", "Dni"… o "" si es un error general (va al asp-validation-summary)
    public string Campo { get; } = campo;
}
```

Notar el constructor primario: `: Exception(mensaje)` pasa el mensaje a la clase base, y así `ex.Message` funciona
como en cualquier excepción. Es el mismo patrón que `SaldoInsuficienteException` del tema 16.

:::nota
En el desafío 8 se le agrega un tercer parámetro opcional (la excepción original que la causó). Por ahora, dos
parámetros son suficientes.
:::

### Paso 2 · El contrato: las interfaces de los servicios

Carpeta nueva `Servicios/`. Primero el **qué** (los contratos). Pueden ubicarse juntas en `Servicios/Interfaces.cs` o
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

Notar lo que **no** aparece en las firmas: ningún `ReservaCrearViewModel`, ningún `ModelStateDictionary`, ningún
`IActionResult`. El servicio recibe datos del dominio (ids, textos, entidades) y es independiente de quién lo invoque: un
controlador MVC, una Web API o un programa de consola.

### Paso 3 · `ReservaService`: trasladar las reglas

Ahora el **cómo**. Las cinco reglas que estaban en el controlador se trasladan aquí, en el mismo orden. La diferencia:
en lugar de `ModelState.AddModelError(nameof(modelo.Asiento), "…")`, el servicio **lanza**
`new ReglaDeNegocioException("Asiento", "…")`. La primera regla que falla interrumpe la ejecución: las siguientes no se evalúan.

Por ahora el servicio recibe los repositorios **concretos** (como los controladores): las interfaces de los
repositorios llegan en el desafío 7.

Se recomienda intentarlo con el controlador del desafío 5 abierto al lado, trasladando regla por regla.

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

`?? throw` es una **cláusula de guarda** en una línea: si `Obtener` devuelve `null`, se lanza la excepción en ese mismo
punto ("lanzar temprano", tema 16). Y como el campo viaja como texto (`"VueloId"`), debe coincidir **letra por letra**
con la propiedad del ViewModel: el servicio no depende de `ReservaCrearViewModel`, así que no puede usar `nameof`.

### Paso 4 · Registrar el servicio y simplificar el controlador

En `Program.cs`, después de los repositorios:

```csharp
// Servicios: las reglas del negocio. "Cuando se pida IReservaService, crear un ReservaService"
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IPasajeroService, PasajeroService>();
builder.Services.AddScoped<IVueloService, VueloService>();
```

Para construir un `ReservaService`, que requiere tres repositorios, el contenedor examina su constructor, identifica los
parámetros `ReservaRepository`, `VueloRepository` y `PasajeroRepository` y, como esos tipos ya están registrados (con los
`AddScoped(_ => new …Repository(cadenaConexion))` del proyecto), los crea y se los pasa. En ningún lugar del código se
escribe `new ReservaService(…)`.

El controlador declara el **contrato** como dependencia y queda así:

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

        // 2 · Las reglas las evalúa el servicio
        try
        {
            Reserva reserva = reservaService.Reservar(modelo.VueloId!.Value, modelo.PasajeroId!.Value, modelo.Asiento);
            TempData["Mensaje"] = $"Reserva confirmada: {reserva.Vuelo.Numero}, asiento {reserva.Asiento}.";
            return RedirectToAction("Detalle", "Vuelos", new { id = reserva.Vuelo.Id });
        }
        catch (ReglaDeNegocioException ex)
        {
            // El servicio indica en qué campo está el problema: el error aparece al lado de ese input
            ModelState.AddModelError(ex.Campo, ex.Message);
            CargarListas(modelo);
            return View(modelo);
        }
    }
    // … Index, Crear (GET) y CargarListas quedan como estaban
}
```

Los repositorios que permanecen en el constructor los usan `Index` y `CargarListas` para **leer** (listar vuelos y
pasajeros). La lectura para mostrar no involucra reglas; lo que **cambia datos** pasa por el servicio.

Eliminar del controlador las reglas trasladadas: si se conserva una copia en cada lugar, cuando la regla cambie es
probable que se modifique solo una. Una regla, un lugar (DRY, tema 13).

Ejecutar las pruebas del desafío 5: mismo error, mismo campo. Si alguna dejó de mostrarse, revisar el nombre del campo.

### Paso 5 · `PasajeroService` y `VueloService`

El mismo procedimiento con los otros dos formularios:

- `PasajeroService.Registrar`: la regla del DNI repetido (`ExisteDni`) que estaba en `PasajerosController.Crear`.
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

Los controladores siguen el mismo molde que `Reservas/Crear`: `if (!ModelState.IsValid)` → `try` { servicio,
`TempData`, redirect } → `catch (ReglaDeNegocioException ex)` { `AddModelError`, recargar listas, misma vista }.
Por ejemplo, en `VuelosController.Crear` (POST):

```csharp
try
{
    // Pasó [Required]: en este punto los int? ya tienen valor
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

El contrato exige `POST /Reservas/Cancelar/{id}`, que **no borra**: cambia `Estado` a `'Cancelada'`. Así se conserva el
historial (quién tuvo ese asiento) y el asiento vuelve a estar libre, porque `AsientosOcupados` solo cuenta las
confirmadas.

Primero, el ViewModel del listado indica si una reserva se puede cancelar (para mostrar o no el botón):

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

En `Views/Reservas/Index.cshtml`, se agrega una columna con un **formulario** (no un enlace: un enlace es GET, y un GET
no debe modificar datos):

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

El tag helper `<form>` con `method="post"` agrega automáticamente el campo oculto del token antiforgery: por eso
`[ValidateAntiForgeryToken]` no rechaza este POST.

**¿Dónde se muestra el mensaje?** Después del redirect hay **otro** request (el GET del `Index`). `ViewData` no
sobrevive al cambio de request; `TempData` sí: dura hasta que se lee. Como lo lee el layout, funciona en cualquier
pantalla. En `Views/Shared/_Layout.cshtml`, dentro del `<main>` y antes de `@RenderBody()`:

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
`TempData` no está en las diapositivas. Pertenece al mismo tema que la sesión (tema 15, "estado entre requests"): por
defecto MVC lo guarda en una **cookie** que se borra al leerla. La alternativa sin `TempData` sería pasar el mensaje en
la URL (`/Reservas?mensaje=…`), pero es menos claro, se puede falsificar y se repite al recargar la página.
:::

### Paso 7 · Editar y eliminar pasajeros

**Editar** usa el mismo formulario que crear. Agregar a `PasajeroFormViewModel` un `Id` opcional (null = nuevo) y un
constructor para cargarlo desde la entidad:

```csharp
public class PasajeroFormViewModel
{
    public int? Id { get; set; }                       // null = alta; con valor = edición
    public bool EsNuevo => Id is null;

    // … Nombre, Dni y Email con sus atributos, como en el desafío 5

    public PasajeroFormViewModel() { }                 // el model binding requiere este constructor

    // Entidad → ViewModel (para mostrar el formulario de edición completo)
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

Renombrar `Views/Pasajeros/Crear.cshtml` a `Formulario.cshtml` y hacer que el `<form>` apunte a una acción u otra:

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

El controlador (`PasajerosController` ahora declara también la dependencia `IPasajeroService pasajeroService`):

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
    modelo.Id = id;     // el Id lo aporta la ruta, no un input que pueda modificarse
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

En `Views/Pasajeros/Detalle.cshtml`, un enlace "Editar" (`<a class="boton" asp-action="Editar" asp-route-id="@Model.Id">`)
y un `<form asp-action="Eliminar" asp-route-id="@Model.Id" method="post">` con su botón, igual que el de cancelar.
Probar eliminar a Ana Gómez (tiene reservas: error en rojo) y a un pasajero creado durante las pruebas, sin reservas (se
borra).

:::nota
En el desafío 9, eliminar pasajeros quedará restringido al rol Admin. Por ahora el botón es visible para cualquier
usuario.
:::
