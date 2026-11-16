## 🎯 Misión

Hoy AeroTaller acepta cualquier cosa: el asiento `99Z`, un asiento que ya está ocupado, una reserva en un vuelo que
salió ayer, un pasajero sin nombre, un DNI `hola`. Es el control migratorio de la aplicación: **nadie pasa con datos
inválidos**.

Al terminar este desafío:

1. Los tres formularios (`Reservas/Crear`, `Pasajeros/Crear` y `Vuelos/Crear`) validan **la forma** de cada campo con
   atributos, en el servidor **y** en el navegador.
2. Las reglas que necesitan la base (asiento libre, vuelo lleno, DNI repetido…) se controlan en el servidor y el error
   aparece **al lado del campo** que corresponde.
3. Un atributo propio, `[FechaFutura]`, impide crear vuelos en el pasado.
4. Si algo falla, **no se toca la base** y el formulario vuelve con lo que el usuario había cargado.

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

## 📚 Repaso

Lo que vimos en el tema 13, en cuatro ideas:

- **Nunca confiar en lo que llega del cliente.** La validación del navegador es comodidad; la del servidor es la que
  protege. Y la base (`NOT NULL`, `UNIQUE`, `FOREIGN KEY`) es la última red.
- **DRY:** la regla se declara **una vez**, como atributo en el ViewModel, y ASP.NET la aplica en los dos lados:
  en el servidor (`ModelState`) y en el navegador (`data-val-*` + jQuery Validation).
- **`ModelState.IsValid`** no frena nada solo: los atributos anotan los errores, y **la acción decide**.
- **Las reglas que consultan datos** ("¿ese DNI ya existe?") no pueden ser atributos: se agregan a mano con
  `ModelState.AddModelError(nameof(modelo.Campo), "mensaje")`.

| Qué | Dónde | En AeroTaller |
|---|---|---|
| Forma de cada campo | atributos en el ViewModel | asiento `12C`, DNI de 7 u 8 dígitos, número `AT1234` |
| Reglas que consultan datos | `AddModelError` en el controlador (en el desafío 6, en un servicio) | asiento libre, DNI repetido, vuelo lleno |
| Integridad | la base | `UNIQUE` en `Dni` y `Numero`, `FOREIGN KEY`, `CHECK (OrigenId <> DestinoId)` |
| Comodidad | el navegador, generado de los atributos | aviso antes de enviar |

Lo que vimos en clase: [Tema 13 — Validación](https://taller-de-lenguajes-2.github.io/clases/tema-13-validacion/index.html).

## 💻 Paso a paso

### Paso 1 · Atributos en el formulario de reserva

Sumale a `ReservaCrearViewModel` los atributos de la forma de cada campo. Necesitás
`using System.ComponentModel.DataAnnotations;` y `using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;`
(para `[ValidateNever]`).

```csharp
public class ReservaCrearViewModel
{
    // int? para que "no eligió nada" llegue como null y lo agarre [Required]
    [Display(Name = "Vuelo")]
    [Required(ErrorMessage = "Elegí un vuelo.")]
    public int? VueloId { get; set; }

    [Display(Name = "Pasajero")]
    [Required(ErrorMessage = "Elegí un pasajero.")]
    public int? PasajeroId { get; set; }

    // Una o dos cifras y una letra de la A a la F; se aceptan minúsculas y espacios ("12c ")
    [Display(Name = "Asiento")]
    [Required(ErrorMessage = "Indicá el asiento.")]
    [RegularExpression(@"^\s*\d{1,2}[A-Fa-f]\s*$", ErrorMessage = "El asiento es la fila y una letra de la A a la F (ej.: 12C).")]
    public string Asiento { get; set; } = "";

    // Las listas no las manda el usuario: no se validan (y hay que recargarlas si el POST falla)
    [ValidateNever] public List<SelectListItem> Vuelos { get; set; } = [];
    [ValidateNever] public List<SelectListItem> Pasajeros { get; set; } = [];
}
```

El `@` delante del patrón es un *verbatim string*: así `\d` y `\s` llegan tal cual al motor de expresiones regulares.
¿El patrón acepta `99Z`? No: la `Z` no está en `[A-Fa-f]`. ¿Y `25A` en un avión de 20 filas? Sí: el atributo no
sabe cuántas filas tiene ese avión. Eso va en el paso 3.

Aprovechá y poné `[ValidateNever]` también en las listas de `VueloIndexViewModel` (`Destinos` y `Resultados`).

### Paso 2 · `ModelState` en el servidor y los errores en la vista

En el POST, lo primero es preguntar. Si algo falló, se vuelve a mostrar **el mismo formulario** con lo que cargó el
usuario (y las listas recargadas):

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Crear(ReservaCrearViewModel modelo)
{
    // 1 · Los atributos ya corrieron durante el binding: si algo falló, ModelState lo tiene
    if (!ModelState.IsValid)
    {
        CargarListas(modelo);
        return View(modelo);
    }

    // 2 · Reglas que necesitan la base (paso 3)
    // 3 · Guardar y redirigir
}
```

En la vista, un `<span asp-validation-for>` por campo, el resumen para los errores "del formulario entero" y los
scripts de validación del navegador:

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
CDN, igual que la clínica). El layout tiene el `RenderSectionAsync("Scripts", …)` que lo recibe.

Abrí el formulario, apretá F12 y mirá el HTML del `<input>` del asiento: vas a ver `data-val-regex-pattern` con **tu**
patrón. Esa es la regla escrita una sola vez (DRY), reflejada en el navegador.

:::nota
La clase `field-validation-error` (el texto rojo) y `validation-summary-errors` ya tienen estilo en `sitio.css`. El
verificador busca justamente `field-validation-error` para saber si tu formulario mostró el error.
:::

### Paso 3 · Las reglas que necesitan la base

Un atributo mira un valor suelto. Para saber si el 3A está ocupado o si el AT0500 está lleno hay que consultar los
repositorios. Por ahora esas reglas van **en el controlador**, después del primer `if` (en el desafío 6 se mudan a un
servicio).

El orden importa: primero normalizá el asiento (`"12c "` → `"12C"`), y después controlá en este orden: vuelo que ya
salió → asiento que no existe en ese avión → vuelo lleno → asiento ocupado → pasajero repetido. Cada error, con
`nameof` para no escribir mal el nombre del campo.

Intentalo con lo que ya tenés en los repositorios: `vuelo.YaSalio`, `vuelo.TieneAsiento(asiento)`,
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

    // Pasó [Required]: acá los int? ya tienen valor
    string asiento = modelo.Asiento.Trim().ToUpperInvariant();      // "12c " → "12C"
    Vuelo? vuelo = vuelos.Obtener(modelo.VueloId!.Value);
    Pasajero? pasajero = pasajeros.Obtener(modelo.PasajeroId!.Value);

    // 2 · Reglas que necesitan la base: el error va al campo que corresponde
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

¿Por qué el `else if`? Para mostrar **un** error de vuelo/asiento por vez y el más importante primero: si el vuelo ya
salió, no tiene sentido decir además que el asiento está ocupado. El del pasajero va aparte porque es otro campo.

¿Por qué controlar `vuelo is null` si el desplegable solo muestra vuelos que existen? Porque **el POST se puede armar a
mano**: el verificador (y cualquiera con un `.http` o con F12) manda `VueloId=999` o el Id del AT0999 aunque no esté
en la lista.

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

    // ViewModel → entidad: el repositorio nunca ve un ViewModel
    public Pasajero APasajero() => new()
    {
        Nombre = Nombre.Trim(),
        Dni = Dni,
        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim()
    };
}
```

En el mensaje, `{0}` es el nombre del campo y `{1}`, `{2}`… los parámetros del atributo: en `[StringLength(100,
MinimumLength = 3)]`, `{1}` es 100 y `{2}` es 3.

El controlador, como el `PacientesController` del tema 13: la regla del DNI repetido necesita la base.

```csharp
[HttpGet]
public IActionResult Crear() => View(new PasajeroFormViewModel());

[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Crear(PasajeroFormViewModel modelo)
{
    // Solo consulto la base si el DNI tiene forma de DNI
    if (ModelState.IsValid && pasajeros.ExisteDni(modelo.Dni))
        ModelState.AddModelError(nameof(modelo.Dni), $"Ya hay un pasajero con el DNI {modelo.Dni}.");

    if (!ModelState.IsValid) return View(modelo);

    int id = pasajeros.Insertar(modelo.APasajero());
    return RedirectToAction(nameof(Detalle), new { id });
}
```

La vista `Views/Pasajeros/Crear.cshtml` es igual a la de reservas: `asp-validation-summary="ModelOnly"`, y por cada
campo `<label asp-for>`, `<input asp-for>` y `<span asp-validation-for>`; al final, la sección `Scripts`. Sumá un link
"+ Nuevo pasajero" en `/Pasajeros` (`<a class="boton secundario" asp-action="Crear">`).

### Paso 5 · Un atributo propio: `[FechaFutura]`

Ningún atributo de .NET dice "esta fecha tiene que ser posterior a ahora". Lo escribimos una vez, como en el tema 13,
en una carpeta nueva `Validaciones/`:

```csharp
// Validaciones/FechaFuturaAttribute.cs
using System.ComponentModel.DataAnnotations;

public class FechaFuturaAttribute : ValidationAttribute
{
    public FechaFuturaAttribute()
        : base("{0}: tiene que ser posterior a este momento.") { }

    public override bool IsValid(object? value)
    {
        // null lo resuelve [Required]: acá solo se mira la fecha si hay una
        return value is not DateTime fecha || fecha > DateTime.Now;
    }
}
```

- Hereda de `ValidationAttribute` y **sobrescribe** `IsValid`: herencia y polimorfismo (tema 03).
- El nombre termina en `Attribute`; al usarlo se escribe `[FechaFutura]`.
- Corre **solo en el servidor**: no le escribimos la parte de JavaScript.

### Paso 6 · Alta de vuelos

`GET/POST /Vuelos/Crear` con `Numero`, `OrigenId`, `DestinoId`, `Salida`, `DuracionMinutos`, `Filas` y `Precio`.
Combina todo lo anterior: combos con `int?`, `[FechaFutura]`, `[Range]` y dos reglas con datos.

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
    [Required(ErrorMessage = "Elegí el origen.")]
    public int? OrigenId { get; set; }

    [Display(Name = "Destino")]
    [Required(ErrorMessage = "Elegí el destino.")]
    public int? DestinoId { get; set; }

    [Display(Name = "Salida")]
    [Required(ErrorMessage = "La fecha de salida es obligatoria.")]
    [FechaFutura]
    public DateTime? Salida { get; set; } = DateTime.Today.AddDays(7).AddHours(8);   // propone: en una semana, 08:00

    [Display(Name = "Duración (minutos)")]
    [Required(ErrorMessage = "La duración es obligatoria.")]
    [Range(20, 1200, ErrorMessage = "La duración va de {1} a {2} minutos.")]
    public int? DuracionMinutos { get; set; }

    [Display(Name = "Filas de asientos")]
    [Required(ErrorMessage = "Indicá cuántas filas tiene el avión.")]
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

Las dos reglas con datos van en el POST de `VuelosController`. Origen ≠ destino no necesita la base, pero sí mirar
**dos** campos a la vez: un atributo sobre una propiedad no ve la otra.

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

    // Si alguien manda un Id de aeropuerto inventado, Obtener devuelve null: en el desafío 6 lo controla el servicio
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

`ListaDeAeropuertos()` es un método privado que arma los `SelectListItem` (lo usan el GET de `Crear`, el POST y el
`Index`: sacalo del `Index` del desafío 4 para no repetirlo). En la vista, `<input asp-for="Salida" />` con un
`DateTime?` genera un `<input type="datetime-local">`.

:::nota
En el desafío 9, `Vuelos/Crear` va a quedar solo para el rol Admin. Por ahora lo puede usar cualquiera.
:::

### Paso 7 · Probar al servidor sin la ayuda del navegador

Con los scripts de validación, el navegador frena el formulario vacío antes de enviarlo: no estás probando el
servidor. Para probarlo de verdad:

1. **Desactivá JavaScript** (Chrome: F12 → `Ctrl/Cmd + Shift + P` → "Disable JavaScript") y enviá el formulario
   vacío. Tienen que aparecer los errores igual: esos los puso el **servidor**.
2. **Antiforgery:** con F12, en la pestaña Elementos, borrá el `<input type="hidden" name="__RequestVerificationToken">`
   que el tag helper `<form>` agregó solo, y enviá. Respuesta: **400 Bad Request**, y tu acción ni se ejecuta (poné un
   breakpoint y fijate que no frena). Eso hace `[ValidateAntiForgeryToken]`.
3. **Las reglas con datos**, con la base recién creada:

| Probá | Esperado |
|---|---|
| AT1001, Bruno Díaz, `25A` | "El asiento 25A no existe en este avión…" |
| AT1001, Bruno Díaz, `3A` | "El asiento 3A ya está ocupado." |
| AT1001, Ana Gómez, `5C` | "Ana Gómez ya tiene una reserva en este vuelo." |
| AT0500, Gabriela Sosa, `1F` | ✅ se guarda (el avión queda lleno: 6 de 6) |
| AT0500, Hernán Molina, `1A` | "El vuelo AT0500 está completo." |
| AT0999 (mandalo con F12: cambiá el `value` de una opción a `1`) | "El vuelo AT0999 ya salió." |

Después de cada error, mirá en DB Browser que **no** apareció ninguna fila nueva en `Reserva`.

## 🔴 Breakpoints

:::breakpoint ReservasController.cs · Crear (POST) · if (!ModelState.IsValid)
Con JavaScript desactivado, enviá el formulario vacío. En **Inspección** agregá `ModelState.IsValid` y
`ModelState.ErrorCount`. Desplegá `ModelState["Asiento"].Errors[0].ErrorMessage`: ¿es tu mensaje? Mirá también
`modelo.VueloId`: ¿es `null` o `0`?
:::

:::breakpoint FechaFuturaAttribute.cs · IsValid
Creá un vuelo con salida de ayer. Mirá `value` en **Variables**: ¿de qué tipo es? Ahora borrá la fecha y enviá
otra vez: ¿frena? Si frena, ¿qué vale `value` y qué devuelve el método? ¿Quién termina mostrando el error en ese caso?
:::

:::breakpoint ReservasController.cs · Crear (POST) · el segundo if (!ModelState.IsValid)
Reservá el `25A` en el AT1001. Con F10 recorré las reglas: ¿en qué `else if` entra? Antes y después del
`AddModelError`, mirá `ModelState.IsValid` en **Inspección**: así ves cómo una regla "a mano" se suma a las de los
atributos.
:::

Preguntate:

- Si el usuario carga `12c` (minúscula), ¿qué se guarda en la base? ¿Dónde se convirtió?
- ¿Por qué en `Pasajeros/Crear` solo se llama a `ExisteDni` si `ModelState.IsValid` ya es `true`?

## 🔀 Alternativas

**¿Dónde va una regla que mira dos campos (origen ≠ destino)?**

| Forma | Cómo | Ventaja | Desventaja | Cuándo |
|---|---|---|---|---|
| `AddModelError` en el controlador | un `if` en el POST | simple, igual que el tema 13 | el controlador engorda | **lo de la clase**; en el parcial alcanza |
| `IValidatableObject` en el ViewModel | el ViewModel implementa `Validate(...)` y devuelve los errores | la regla viaja con el formulario | no ve la base; corre solo si los atributos de las propiedades pasaron | reglas entre campos del mismo formulario (no lo vimos en clase) |
| Restricción en la base | `CHECK (OrigenId <> DestinoId)` (ya está en tu `aerotaller.sql`) | nadie la saltea, ni con SQL a mano | el error es una excepción de SQLite, no un mensaje lindo | **además** de las otras: es la última red |

**¿Escribo yo el JavaScript de la validación?**

| Forma | Ventaja | Desventaja | Cuándo |
|---|---|---|---|
| Atributos + `_ValidationScriptsPartial` | una sola regla para servidor y navegador (DRY) | solo cubre los atributos que jQuery Validation conoce | **siempre** como base |
| JavaScript a mano en la vista | control total del mensaje y del momento | la regla queda escrita dos veces, en dos lenguajes | casi nunca |
| Solo servidor (sin scripts) | menos piezas | el usuario espera un viaje al servidor para ver el error | si el formulario es chico; **la del servidor nunca se saca** |

:::catedra
El tema 13 hace las reglas con datos en el controlador (`PacientesController` y `TurnosController`) y nombra la
alternativa: "en el controlador (o en un servicio)". En el desafío 6 hacemos justamente eso: las mudamos a
`ReservaService`. `IValidatableObject` no se vio en clase: es una alternativa válida, pero en el parcial usá lo que
conocés.
:::

## ⚠️ Errores típicos

- **Atributos sin `if (!ModelState.IsValid)`**: los errores se anotan pero la acción sigue y guarda (o explota). En MVC
  el `if` lo escribís vos; el 400 automático es de la Web API con `[ApiController]`.
- **`int VueloId` sin `?`**: si el campo no llega queda en 0 y `[Required]` no lo ve; si llega vacío, el error es uno
  genérico en inglés (*The value '' is invalid.*). Con `int?` aparece tu mensaje.
- **El nombre del campo mal escrito en `AddModelError("Vuelo", …)`**: `ModelState.IsValid` da `false`, pero ningún
  `asp-validation-for` se llama así y el resumen `ModelOnly` solo muestra errores con nombre vacío: **el error no se ve
  en ningún lado**. Usá `nameof(modelo.VueloId)`.
- **Mensajes en inglés** (*The Nombre field is required.*): con `<Nullable>enable</Nullable>`, un `string` sin `?` ya
  es obligatorio aunque no tenga `[Required]`, y el mensaje es el de .NET. Poné el `[Required(ErrorMessage = …)]`.
- **Sin la sección `Scripts`**: no hay validación en el navegador (el servidor sigue validando, pero el usuario espera
  el viaje). Y al revés: **solo** validar en el navegador es no validar.
- **"Mis vuelos de ejemplo ya salieron"**: las fechas de `datos.sql` se calculan desde el día en que se **creó** la base.
  Si la creaste hace una semana, el AT1001 ya salió. Borrá `aerotaller.db` y arrancá de nuevo.
- **Olvidarse `CargarListas` en el camino del segundo `if`**: el formulario vuelve con los errores… y los desplegables
  vacíos.
