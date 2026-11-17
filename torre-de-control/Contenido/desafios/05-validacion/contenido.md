## :target: Misión

En su estado actual, AeroTaller acepta cualquier dato: el asiento `99Z`, un asiento que ya está ocupado, una reserva en
un vuelo que salió ayer, un pasajero sin nombre, un DNI `hola`. Este desafío incorpora la validación, que cumple en la aplicación
el papel de un control migratorio: **ningún dato inválido debe ser aceptado**.

Al terminar este desafío:

1. Los tres formularios (`Reservas/Crear`, `Pasajeros/Crear` y `Vuelos/Crear`) validan **la forma** de cada campo con
   atributos, en el servidor **y** en el navegador.
2. Las reglas que requieren consultar la base (asiento libre, vuelo lleno, DNI repetido…) se controlan en el servidor y
   el error aparece **al lado del campo** que corresponde.
3. Un atributo propio, `[FechaFutura]`, impide crear vuelos en el pasado.
4. Si alguna validación falla, **no se modifica la base** y el formulario se vuelve a mostrar con los datos que el
   usuario había cargado.

Las reglas son las del **contrato** (`CONTRATO.md`, "Reglas del negocio"):

| Regla | Error en el campo |
|---|---|
| El asiento es fila + letra A–F (`12C`) | `Asiento` |
| El asiento existe en ese avión (fila ≤ `Filas`) | `Asiento` |
| El asiento está libre | `Asiento` |
| No se reserva un vuelo que ya salió | `VueloId` |
| No hay overbooking (confirmadas < capacidad) | `VueloId` |
| Un pasajero no tiene dos reservas confirmadas en el mismo vuelo | `PasajeroId` |
| DNI: 7 u 8 dígitos y único | `Dni` |
| Vuelo: salida futura, origen ≠ destino, número `AT` + 4 dígitos y único | el campo correspondiente |

## :book-open-text: Repaso

Lo visto en el tema 13, en cuatro ideas:

- **Nunca confiar en los datos que llegan del cliente.** La validación del navegador es una comodidad; la del servidor
  es la que protege la aplicación. La base (`NOT NULL`, `UNIQUE`, `FOREIGN KEY`) es la última línea de defensa.
- **DRY:** la regla se declara **una vez**, como atributo en el ViewModel, y ASP.NET la aplica en los dos lados:
  en el servidor (`ModelState`) y en el navegador (`data-val-*` + jQuery Validation).
- **`ModelState.IsValid`** no detiene nada por sí solo: los atributos registran los errores y **la acción determina cómo proceder**.
- **Las reglas que consultan datos** ("¿ese DNI ya existe?") no pueden ser atributos: se agregan manualmente con
  `ModelState.AddModelError(nameof(modelo.Campo), "mensaje")`.

| Qué | Dónde | En AeroTaller |
|---|---|---|
| Forma de cada campo | atributos en el ViewModel | asiento `12C`, DNI de 7 u 8 dígitos, número `AT1234` |
| Reglas que consultan datos | `AddModelError` en el controlador (en el desafío 6, en un servicio) | asiento libre, DNI repetido, vuelo lleno |
| Integridad | la base | `UNIQUE` en `Dni` y `Numero`, `FOREIGN KEY`, `CHECK (OrigenId <> DestinoId)` |
| Comodidad | el navegador, generado a partir de los atributos | aviso antes de enviar |

Lo visto en clase: [Tema 13 — Validación](https://taller-de-lenguajes-2.github.io/clases/tema-13-validacion/index.html).

### :lightbulb: Tips y recursos

**¿Dónde se ubica una regla que evalúa dos campos (origen ≠ destino)?**

| Forma | Cómo | Ventaja | Desventaja | Cuándo |
|---|---|---|---|---|
| `AddModelError` en el controlador | un `if` en el POST | simple, igual que en el tema 13 | el controlador aumenta de tamaño | **lo visto en clase**; en el parcial es suficiente |
| `IValidatableObject` en el ViewModel | el ViewModel implementa `Validate(...)` y devuelve los errores | la regla se define junto con el formulario | no accede a la base; se ejecuta solo si las propiedades superaron sus atributos | reglas entre campos del mismo formulario (no se vio en clase) |
| Restricción en la base | `CHECK (OrigenId <> DestinoId)` (ya está en `aerotaller.sql`) | no puede omitirse, ni siquiera con SQL ejecutado manualmente | el error es una excepción de SQLite, no un mensaje legible | **además** de las otras: es la última línea de defensa |

**¿Conviene escribir manualmente el JavaScript de la validación?**

| Forma | Ventaja | Desventaja | Cuándo |
|---|---|---|---|
| Atributos + `_ValidationScriptsPartial` | una sola regla para servidor y navegador (DRY) | solo cubre los atributos que admite jQuery Validation | **siempre** como base |
| JavaScript propio en la vista | control total del mensaje y del momento | la regla queda escrita dos veces, en dos lenguajes | casi nunca |
| Solo servidor (sin scripts) | menos componentes | el usuario debe esperar una solicitud al servidor para ver el error | si el formulario es pequeño; **la del servidor nunca se elimina** |

:::catedra
El tema 13 implementa las reglas con datos en el controlador (`PacientesController` y `TurnosController`) y menciona la
alternativa: "en el controlador (o en un servicio)". En el desafío 6 se aplica precisamente esa alternativa: se trasladan a
`ReservaService`. `IValidatableObject` no se vio en clase: es una alternativa válida, pero en el parcial corresponde usar
lo visto en la materia.
:::

## :code-xml: Paso a paso

### Paso 1 · Atributos en el formulario de reserva

Agregar a `ReservaCrearViewModel` los atributos que definen la forma de cada campo. Se necesitan
`using System.ComponentModel.DataAnnotations;` y `using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;`
(para `[ValidateNever]`).

```csharp
public class ReservaCrearViewModel
{
    // int? para que la ausencia de selección llegue como null y la detecte [Required]
    [Display(Name = "Vuelo")]
    [Required(ErrorMessage = "El vuelo es obligatorio.")]
    public int? VueloId { get; set; }

    [Display(Name = "Pasajero")]
    [Required(ErrorMessage = "El pasajero es obligatorio.")]
    public int? PasajeroId { get; set; }

    // Una o dos cifras y una letra de la A a la F; se aceptan minúsculas y espacios ("12c ")
    [Display(Name = "Asiento")]
    [Required(ErrorMessage = "El asiento es obligatorio.")]
    [RegularExpression(@"^\s*\d{1,2}[A-Fa-f]\s*$", ErrorMessage = "El asiento es la fila y una letra de la A a la F (ej.: 12C).")]
    public string Asiento { get; set; } = "";

    // Las listas no las envía el usuario: no se validan (y deben recargarse si el POST falla)
    [ValidateNever] public List<SelectListItem> Vuelos { get; set; } = [];
    [ValidateNever] public List<SelectListItem> Pasajeros { get; set; } = [];
}
```

El `@` delante del patrón indica un *verbatim string*: así `\d` y `\s` llegan sin modificaciones al motor de
expresiones regulares. El patrón rechaza `99Z`, porque la `Z` no está en `[A-Fa-f]`. En cambio, acepta `25A` en un avión
de 20 filas: el atributo no tiene acceso a la cantidad de filas de ese avión. Esa regla corresponde al paso 3.

Agregar también `[ValidateNever]` en las listas de `VueloIndexViewModel` (`Destinos` y `Resultados`).

### Paso 2 · `ModelState` en el servidor y los errores en la vista

En el POST, lo primero es consultar el estado de la validación. Si alguna validación falló, se vuelve a mostrar
**el mismo formulario** con los datos que cargó el usuario (y las listas recargadas):

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Crear(ReservaCrearViewModel modelo)
{
    // 1 · Los atributos ya se ejecutaron durante el binding: si algo falló, ModelState lo registra
    if (!ModelState.IsValid)
    {
        CargarListas(modelo);
        return View(modelo);
    }

    // 2 · Reglas que requieren consultar la base (paso 3)
    // 3 · Guardar y redirigir
}
```

En la vista se necesita un `<span asp-validation-for>` por campo, el resumen para los errores "del formulario entero" y
los scripts de validación del navegador:

```cshtml
<form asp-action="Crear" method="post" class="formulario">
    <div asp-validation-summary="ModelOnly"></div>

    <label asp-for="VueloId"></label>
    <select asp-for="VueloId" asp-items="Model.Vuelos">
        <option value="">— Elegir —</option>
    </select>
    <span asp-validation-for="VueloId"></span>

    <label asp-for="PasajeroId"></label>
    <select asp-for="PasajeroId" asp-items="Model.Pasajeros">
        <option value="">— Elegir —</option>
    </select>
    <span asp-validation-for="PasajeroId"></span>

    <label asp-for="Asiento"></label>
    <input asp-for="Asiento" placeholder="12C" />
    <span asp-validation-for="Asiento"></span>

    <button type="submit">Reservar</button>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

`_ValidationScriptsPartial.cshtml` ya está en `Views/Shared/` (carga jQuery, jQuery Validation y Unobtrusive desde un
CDN, igual que en la clínica). El layout contiene el `RenderSectionAsync("Scripts", …)` que lo incluye.

Abrir el formulario, presionar F12 e inspeccionar el HTML del `<input>` del asiento: aparece `data-val-regex-pattern`
con **el mismo** patrón declarado en el ViewModel. Es la regla escrita una sola vez (DRY), reflejada en el navegador.

:::nota
La clase `field-validation-error` (el texto rojo) y `validation-summary-errors` ya tienen estilo en `sitio.css`. El
verificador busca precisamente `field-validation-error` para determinar si el formulario mostró el error.
:::

### Paso 3 · Las reglas que requieren consultar la base

Un atributo evalúa un valor aislado. Para determinar si el 3A está ocupado o si el AT0500 está lleno es necesario
consultar los repositorios. Por ahora esas reglas se ubican **en el controlador**, después del primer `if` (en el desafío 6 se trasladan a
un servicio).

El orden importa: primero se normaliza el asiento (`"12c "` → `"12C"`) y después se controla, en este orden: vuelo que
ya salió → asiento que no existe en ese avión → vuelo lleno → asiento ocupado → pasajero repetido. Cada error se
registra con `nameof` para evitar errores de escritura en el nombre del campo.

Se recomienda intentarlo con los miembros que ya exponen el modelo y los repositorios: `vuelo.YaSalio`, `vuelo.TieneAsiento(asiento)`,
`vuelo.Capacidad`, `reservas.ContarConfirmadas(vueloId)`, `reservas.AsientosOcupados(vueloId)` y
`reservas.PasajeroTieneReserva(vueloId, pasajeroId)`.

:::solucion Ver el POST completo de Reservas/Crear
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Crear(ReservaCrearViewModel modelo)
{
    // 1 · Forma de los campos (atributos)
    if (!ModelState.IsValid)
    {
        CargarListas(modelo);
        return View(modelo);
    }

    // Superada la validación de [Required]: en este punto los int? ya tienen valor
    string asiento = modelo.Asiento.Trim().ToUpperInvariant();      // "12c " → "12C"
    Vuelo? vuelo = vuelos.Obtener(modelo.VueloId!.Value);
    Pasajero? pasajero = pasajeros.Obtener(modelo.PasajeroId!.Value);

    // 2 · Reglas que requieren consultar la base: el error se asigna al campo que corresponde
    if (vuelo is null)
        ModelState.AddModelError(nameof(modelo.VueloId), "Ese vuelo no existe.");
    else if (vuelo.YaSalio)
        ModelState.AddModelError(nameof(modelo.VueloId), $"El vuelo {vuelo.Numero} ya salió.");
    else if (!vuelo.TieneAsiento(asiento))
        ModelState.AddModelError(nameof(modelo.Asiento),
            $"El asiento {asiento} no existe en este avión (filas 1 a {vuelo.Filas}, letras A a F).");
    else if (reservas.ContarConfirmadas(vuelo.Id) >= vuelo.Capacidad)
        ModelState.AddModelError(nameof(modelo.VueloId), $"El vuelo {vuelo.Numero} está completo.");
    else if (reservas.AsientosOcupados(vuelo.Id).Contains(asiento))
        ModelState.AddModelError(nameof(modelo.Asiento), $"El asiento {asiento} ya está ocupado.");

    if (pasajero is null)
        ModelState.AddModelError(nameof(modelo.PasajeroId), "Ese pasajero no existe.");
    else if (vuelo is not null && reservas.PasajeroTieneReserva(vuelo.Id, pasajero.Id))
        ModelState.AddModelError(nameof(modelo.PasajeroId), $"{pasajero.Nombre} ya tiene una reserva en este vuelo.");

    if (!ModelState.IsValid)
    {
        CargarListas(modelo);
        return View(modelo);
    }

    // 3 · Todo en orden: ViewModel → entidad → repositorio
    var reserva = new Reserva
    {
        Vuelo = vuelo!,
        Pasajero = pasajero!,
        Asiento = asiento,
        Fecha = DateTime.Now,
        Estado = EstadoReserva.Confirmada
    };
    reservas.Insertar(reserva);

    return RedirectToAction("Detalle", "Vuelos", new { id = vuelo!.Id });   // POST → Redirect → GET
}
```
:::

¿Por qué el `else if`? Para mostrar **un** error de vuelo/asiento por vez, el más importante primero: si el vuelo ya
salió, no tiene sentido informar además que el asiento está ocupado. El error del pasajero se evalúa por separado porque
corresponde a otro campo.

¿Por qué controlar `vuelo is null` si el desplegable solo muestra vuelos existentes? Porque **el POST puede construirse
manualmente**: el verificador (y cualquier usuario con un archivo `.http` o con F12) envía `VueloId=999` o el Id del
AT0999 aunque no esté en la lista.

### Paso 4 · Alta de pasajeros

Nuevo formulario: `GET/POST /Pasajeros/Crear` con los campos `Nombre`, `Dni` y `Email` (contrato). El ViewModel:

```csharp
// ViewModels/PasajeroFormViewModel.cs
using System.ComponentModel.DataAnnotations;

public class PasajeroFormViewModel
{
    [Display(Name = "Nombre y apellido")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre tiene que tener entre {2} y {1} caracteres.")]
    public string Nombre { get; set; } = "";

    [Display(Name = "DNI")]
    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI son 7 u 8 números, sin puntos.")]
    public string Dni { get; set; } = "";

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Ese email no es válido.")]
    public string? Email { get; set; }                       // string? = opcional

    // ViewModel → entidad: el repositorio nunca recibe un ViewModel
    public Pasajero APasajero() => new()
    {
        Nombre = Nombre.Trim(),
        Dni = Dni,
        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim()
    };
}
```

En el mensaje, `{0}` es el nombre del campo y `{1}`, `{2}`… son los parámetros del atributo: en `[StringLength(100,
MinimumLength = 3)]`, `{1}` es 100 y `{2}` es 3.

El controlador sigue el modelo del `PacientesController` del tema 13: la regla del DNI repetido requiere consultar la base.

```csharp
[HttpGet]
public IActionResult Crear() => View(new PasajeroFormViewModel());

[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Crear(PasajeroFormViewModel modelo)
{
    // Solo se consulta la base si el DNI tiene forma de DNI
    if (ModelState.IsValid && pasajeros.ExisteDni(modelo.Dni))
        ModelState.AddModelError(nameof(modelo.Dni), $"Ya hay un pasajero con el DNI {modelo.Dni}.");

    if (!ModelState.IsValid) return View(modelo);

    int id = pasajeros.Insertar(modelo.APasajero());
    return RedirectToAction(nameof(Detalle), new { id });
}
```

La vista `Views/Pasajeros/Crear.cshtml` es igual a la de reservas: `asp-validation-summary="ModelOnly"`, y por cada
campo `<label asp-for>`, `<input asp-for>` y `<span asp-validation-for>`; al final, la sección `Scripts`. Agregar un
enlace "+ Nuevo pasajero" en `/Pasajeros` (`<a class="boton secundario" asp-action="Crear">`).

### Paso 5 · Un atributo propio: `[FechaFutura]`

Ningún atributo de .NET expresa "esta fecha debe ser posterior al momento actual". Se escribe una vez, como en el tema
13, en una carpeta nueva `Validaciones/`:

```csharp
// Validaciones/FechaFuturaAttribute.cs
using System.ComponentModel.DataAnnotations;

public class FechaFuturaAttribute : ValidationAttribute
{
    public FechaFuturaAttribute()
        : base("{0}: tiene que ser posterior a este momento.") { }

    public override bool IsValid(object? value)
    {
        // El caso null lo resuelve [Required]: aquí solo se evalúa la fecha si existe
        return value is not DateTime fecha || fecha > DateTime.Now;
    }
}
```

- Hereda de `ValidationAttribute` y **sobrescribe** `IsValid`: herencia y polimorfismo (tema 03).
- El nombre termina en `Attribute`; al usarlo se escribe `[FechaFutura]`.
- Se ejecuta **solo en el servidor**: no se implementa la parte de JavaScript.

### Paso 6 · Alta de vuelos

`GET/POST /Vuelos/Crear` con `Numero`, `OrigenId`, `DestinoId`, `Salida`, `DuracionMinutos`, `Filas` y `Precio`.
Combina todo lo anterior: desplegables con `int?`, `[FechaFutura]`, `[Range]` y dos reglas con datos.

:::solucion Ver VueloCrearViewModel
```csharp
// ViewModels/VueloCrearViewModel.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

public class VueloCrearViewModel
{
    [Display(Name = "Número de vuelo")]
    [Required(ErrorMessage = "El número es obligatorio.")]
    [RegularExpression(@"^[Aa][Tt]\d{4}$", ErrorMessage = "El número es AT seguido de 4 dígitos (ej.: AT1234).")]
    public string Numero { get; set; } = "";

    [Display(Name = "Origen")]
    [Required(ErrorMessage = "El origen es obligatorio.")]
    public int? OrigenId { get; set; }

    [Display(Name = "Destino")]
    [Required(ErrorMessage = "El destino es obligatorio.")]
    public int? DestinoId { get; set; }

    [Display(Name = "Salida")]
    [Required(ErrorMessage = "La fecha de salida es obligatoria.")]
    [FechaFutura]
    public DateTime? Salida { get; set; } = DateTime.Today.AddDays(7).AddHours(8);   // valor propuesto: en una semana, 08:00

    [Display(Name = "Duración (minutos)")]
    [Required(ErrorMessage = "La duración es obligatoria.")]
    [Range(20, 1200, ErrorMessage = "La duración va de {1} a {2} minutos.")]
    public int? DuracionMinutos { get; set; }

    [Display(Name = "Filas de asientos")]
    [Required(ErrorMessage = "La cantidad de filas es obligatoria.")]
    [Range(1, 40, ErrorMessage = "El avión tiene entre {1} y {2} filas.")]
    public int? Filas { get; set; }

    [Display(Name = "Precio")]
    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(1000, 5000000, ErrorMessage = "El precio va de {1} a {2}.")]
    public decimal? Precio { get; set; }

    [ValidateNever] public List<SelectListItem> Aeropuertos { get; set; } = [];
}
```
:::

Las dos reglas con datos se implementan en el POST de `VuelosController`. Origen ≠ destino no requiere consultar la
base, pero sí evaluar **dos** campos a la vez: un atributo sobre una propiedad no tiene acceso a la otra.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Crear(VueloCrearViewModel modelo)
{
    string numero = modelo.Numero.Trim().ToUpperInvariant();

    if (modelo.OrigenId is not null && modelo.OrigenId == modelo.DestinoId)
        ModelState.AddModelError(nameof(modelo.DestinoId), "El destino tiene que ser distinto del origen.");
    if (ModelState.IsValid && vuelos.ExisteNumero(numero))
        ModelState.AddModelError(nameof(modelo.Numero), $"Ya existe un vuelo {numero}.");

    if (!ModelState.IsValid)
    {
        modelo.Aeropuertos = ListaDeAeropuertos();      // las listas no vuelven en el POST
        return View(modelo);
    }

    // Si llega un Id de aeropuerto inexistente, Obtener devuelve null: en el desafío 6 lo controla el servicio
    var vuelo = new Vuelo
    {
        Numero = numero,
        Origen = aeropuertos.Obtener(modelo.OrigenId!.Value)!,
        Destino = aeropuertos.Obtener(modelo.DestinoId!.Value)!,
        Salida = modelo.Salida!.Value,
        DuracionMinutos = modelo.DuracionMinutos!.Value,
        Filas = modelo.Filas!.Value,
        Precio = modelo.Precio!.Value
    };
    int id = vuelos.Insertar(vuelo);
    return RedirectToAction(nameof(Detalle), new { id });
}
```

`ListaDeAeropuertos()` es un método privado que construye los `SelectListItem` (lo usan el GET de `Crear`, el POST y el
`Index`; conviene extraerlo del `Index` del desafío 4 para no repetirlo). En la vista, `<input asp-for="Salida" />` con
un `DateTime?` genera un `<input type="datetime-local">`.

:::nota
En el desafío 9, `Vuelos/Crear` quedará restringido al rol Admin. Por ahora cualquier usuario puede usarlo.
:::

### Paso 7 · Probar el servidor sin la ayuda del navegador

Con los scripts de validación, el navegador bloquea el envío del formulario vacío: en ese caso no se está probando el
servidor. Para probarlo efectivamente:

1. **Desactivar JavaScript** (Chrome: F12 → `Ctrl/Cmd + Shift + P` → "Disable JavaScript") y enviar el formulario
   vacío. Los errores deben aparecer de todos modos: en ese caso los genera el **servidor**.
2. **Antiforgery:** con F12, en la pestaña Elementos, eliminar el `<input type="hidden" name="__RequestVerificationToken">`
   que el tag helper `<form>` agregó automáticamente, y enviar. Respuesta: **400 Bad Request**, y la acción ni siquiera
   se ejecuta (con un breakpoint se comprueba que no se detiene). Ese es el efecto de `[ValidateAntiForgeryToken]`.
3. **Las reglas con datos**, con la base recién creada:

| Prueba | Esperado |
|---|---|
| AT1001, Bruno Díaz, `25A` | "El asiento 25A no existe en este avión…" |
| AT1001, Bruno Díaz, `3A` | "El asiento 3A ya está ocupado." |
| AT1001, Ana Gómez, `5C` | "Ana Gómez ya tiene una reserva en este vuelo." |
| AT0500, Gabriela Sosa, `1F` | :circle-check: se guarda (el avión queda lleno: 6 de 6) |
| AT0500, Hernán Molina, `1A` | "El vuelo AT0500 está completo." |
| AT0999 (enviado con F12: cambiar el `value` de una opción a `1`) | "El vuelo AT0999 ya salió." |

Después de cada error, verificar en DB Browser que **no** apareció ninguna fila nueva en `Reserva`.
