## :target: Misión

Hasta este punto cada vista recibía una entidad sin transformar (`@model List<Vuelo>`, `@model Vuelo`). Eso funciona para
pantallas simples, pero AeroTaller ya requiere datos que **ninguna entidad contiene**:

- El listado de vuelos requiere la ruta compuesta (`TUC → AEP`), la duración en horas y minutos, el precio con formato y
  **cuántos asientos quedan libres** (un dato que surge de otra consulta).
- El detalle del vuelo requiere un **mapa de asientos**: 20 filas de 6, con el pasillo entre la C y la D, los ocupados
  en gris y los libres como enlaces para reservar.
- El formulario de reserva requiere **las listas de vuelos y pasajeros** para los desplegables.

Al terminar este desafío:

1. Hay una carpeta `ViewModels/` y cada pantalla recibe **exactamente** los datos que muestra, ya formateados.
2. `/Vuelos` permite buscar por destino y fecha.
3. `/Vuelos/Detalle/{id}` muestra el **mapa de asientos**.
4. `/Reservas` lista las reservas y `/Reservas/Crear` tiene el formulario (con el vuelo y el asiento precargados cuando
   se llega desde el mapa).

:::nota
Todavía **no se valida** nada: si se carga el asiento `99Z`, se guarda. Eso corresponde al desafío 5. En este desafío
el foco está en que los datos lleguen a cada vista con la forma adecuada.
:::

## :book-open-text: Repaso

Un **ViewModel** es el objeto que se le pasa a una vista, con solo los datos que esa vista utiliza, listos para mostrar.
En el tema 12 se presentaron tres escenarios, y en AeroTaller aparecen los tres:

| Escenario del tema 12 | En la clínica | En AeroTaller |
|---|---|---|
| 1 · Simplificar un modelo | `TurnoListadoViewModel` (fecha formateada, médico con especialidad) | `VueloListadoViewModel`, `ReservaListadoViewModel` |
| 2 · Dos modelos, una vista | `PacienteDetalleViewModel` (paciente + turnos) | `PasajeroDetalleViewModel`, `VueloDetalleViewModel` (vuelo + asientos ocupados) |
| 3 · La vista necesita más | `TurnoCrearViewModel` (campos + listas para los `<select>`) | `ReservaCrearViewModel`, `VueloIndexViewModel` |

Tres ideas que **se evalúan en el parcial**:

- **Las listas no vuelven en el POST.** Un `<select>` envía solo el valor elegido. Si es necesario volver a mostrar
  el formulario, las listas se cargan de nuevo: por eso existe `CargarListas(modelo)`.
- **Overposting.** Si la acción recibe la entidad, el binding completa cualquier propiedad incluida en el request,
  aunque no esté en el formulario. Con un ViewModel, lo que no está declarado no tiene dónde asignarse.
- **El repositorio no depende de los ViewModels.** El controlador realiza la conversión: ViewModel → entidad → repositorio.

```text
Formulario (POST)  →  ReservaCrearViewModel  →  Reserva (entidad)  →  ReservaRepository.Insertar
```

Lo visto en clase: [Tema 12 — ViewModels](https://taller-de-lenguajes-2.github.io/clases/tema-12-viewmodels/index.html).

### :lightbulb: Tips y recursos

**¿Dónde se construye el mapa?**

| Forma | Cómo | Ventaja | Desventaja | Cuándo |
|---|---|---|---|---|
| Matriz en el ViewModel | `List<List<AsientoViewModel>>` construida en C# | se depura con breakpoints; la vista solo dibuja | un poco más de código en el ViewModel | **la recomendada** (y la que exige el TP) |
| Lista plana | `List<AsientoViewModel>` de 120 y la vista la divide cada 6 (`j % 6 == 0`) | el ViewModel es más simple | la vista hace cálculos; un error de índice desordena toda la grilla | si la grilla no tiene filas "lógicas" |
| Todo en la vista | `@model Vuelo` + los ocupados en `ViewData`, y los `for` en Razor | rápido de escribir | lógica en la vista, `ViewData` sin tipos (errores recién en ejecución) | prototipos; en el parcial resta puntos |

**¿Entidad o ViewModel en la vista?**

| Forma | Ventaja | Desventaja | Cuándo |
|---|---|---|---|
| `@model List<Pasajero>` (la entidad) | ninguna clase adicional | la vista formatea; si cambia la pantalla, es necesario modificar la entidad o la vista | pantallas que muestran la entidad sin cambios (`/Pasajeros` puede quedar así) |
| `@model PasajeroDetalleViewModel` | la vista recibe los datos ya preparados; reúne varios modelos | una clase más por pantalla | cuando la pantalla requiere datos que la entidad no contiene |
| ViewModel para un **formulario** | evita overposting; incluye las listas de los `<select>` | requiere convertir ViewModel → entidad | **siempre** que haya un POST |

:::catedra
La diapositiva lo plantea así: "Para pantallas simples alcanza [con la entidad]; cuando la pantalla pide algo distinto del
modelo, va un ViewModel". Para formularios, el tema 12 muestra el overposting con `Crear(Paciente paciente)`.
:::

## :code-xml: Paso a paso

### Paso 1 · La carpeta y el primer ViewModel

Crear la carpeta `AeroTaller/ViewModels/` (junto a `Models/`). El primero es una **fila del listado de vuelos**
(escenario 1): contiene textos ya formateados, sin lógica de negocio.

```csharp
// ViewModels/VueloListadoViewModel.cs
public class VueloListadoViewModel(Vuelo vuelo, int ocupados)
{
    public int Id { get; } = vuelo.Id;
    public string Numero { get; } = vuelo.Numero;
    public string Ruta { get; } = $"{vuelo.Origen.Codigo} → {vuelo.Destino.Codigo}";
    public string Ciudades { get; } = $"{vuelo.Origen.Ciudad} → {vuelo.Destino.Ciudad}";
    public string Salida { get; } = vuelo.Salida.ToString("ddd dd/MM HH:mm");          // "lun 23/11 07:30"
    public string Llegada { get; } = vuelo.Llegada.ToString("HH:mm") + (vuelo.LlegaAlOtroDia ? " (+1)" : "");
    public string Duracion { get; } = $"{vuelo.DuracionMinutos / 60} h {vuelo.DuracionMinutos % 60:00} min";
    public string Precio { get; } = vuelo.Precio.ToString("C0");                       // "$ 98.000"
    public int Libres { get; } = vuelo.Capacidad - ocupados;
    public bool YaSalio { get; } = vuelo.YaSalio;
}
```

¿Por qué recibe `ocupados` como un parámetro aparte? Porque un `Vuelo` no contiene la cantidad de reservas: ese dato
surge de otra consulta
(`ListarOcupacion`, la del `LEFT JOIN + GROUP BY` escrita en el desafío 2 y usada en el tablero del 3). El ViewModel
**reúne** los dos datos.

:::catedra
En la diapositiva, `TurnoListadoViewModel` asigna las propiedades **en un constructor común**
(`public TurnoListadoViewModel(Turno turno) { Dia = …; }`). Aquí se usa un **constructor primario** (C# 12) y cada
propiedad se inicializa con `= vuelo.X`. Es equivalente y más breve. Cualquiera de las dos formas es válida en el parcial.
:::

:::nota
¿Un archivo por clase o uno por entidad (`VueloViewModels.cs` con todos los de vuelos)? Ambas opciones sirven. En la
clínica hay uno por clase; si se agrupan, el nombre del archivo debe indicarlo.
:::

### Paso 2 · Buscar vuelos: filtro + resultados

`/Vuelos` pasa a tener un buscador por destino y fecha. La pantalla requiere **lo que envía el usuario** (el filtro),
**la lista de destinos** para el `<select>` y **los resultados**: todo en un solo objeto.

```csharp
// ViewModels/VueloIndexViewModel.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;   // SelectListItem

public class VueloIndexViewModel
{
    // Lo que viene en el query string: /Vuelos?DestinoId=2&Desde=2026-12-01
    [Display(Name = "Destino")]
    public int? DestinoId { get; set; }

    [Display(Name = "Desde")]
    [DataType(DataType.Date)]                 // asp-for genera <input type="date">
    public DateTime? Desde { get; set; }

    // Datos que usa la vista para dibujarse
    public List<SelectListItem> Destinos { get; set; } = [];
    public List<VueloListadoViewModel> Resultados { get; set; } = [];
}
```

En el controlador, el **mismo** ViewModel se recibe como parámetro (el model binding lo completa desde el query string)
y se envía a la vista con los resultados:

```csharp
// Controllers/VuelosController.cs
public class VuelosController(
    VueloRepository vuelos,
    AeropuertoRepository aeropuertos,
    ReservaRepository reservas) : Controller
{
    public IActionResult Index(VueloIndexViewModel filtro)
    {
        List<Vuelo> encontrados = vuelos.Buscar(filtro.DestinoId, filtro.Desde);

        // Una sola consulta con GROUP BY para todos (no una consulta por vuelo)
        Dictionary<int, int> ocupados = vuelos.ListarOcupacion()
            .ToDictionary(o => o.VueloId, o => o.Ocupados);

        filtro.Resultados = encontrados
            .Select(v => new VueloListadoViewModel(v, ocupados.GetValueOrDefault(v.Id)))
            .ToList();
        filtro.Destinos = aeropuertos.Listar()
            .Select(a => new SelectListItem($"{a.Ciudad} ({a.Codigo})", a.Id.ToString()))
            .ToList();
        return View(filtro);
    }
    // … Detalle, en el paso 3
}
```

:::nota
El constructor de `VuelosController` ahora declara tres repositorios como dependencias. Los cuatro ya están registrados en `Program.cs`
desde el desafío 3 (`AddScoped(_ => new AeropuertoRepository(cadenaConexion))`, etc.), de modo que ASP.NET los crea y
se los pasa. Si alguno faltara, al abrir `/Vuelos` aparecería *Unable to resolve service for type 'AeropuertoRepository'
while attempting to activate 'VuelosController'*.
:::

En la vista cambia el `@model` y aparece un formulario **GET** (el filtro se envía en la URL, por lo que el enlace
se puede compartir):

```cshtml
@model VueloIndexViewModel
@{
    ViewData["Title"] = "Vuelos";
}

<h1>Vuelos</h1>

<form asp-action="Index" method="get" class="buscador">
    <label asp-for="DestinoId"></label>
    <select asp-for="DestinoId" asp-items="Model.Destinos">
        <option value="">Todos</option>
    </select>

    <label asp-for="Desde"></label>
    <input asp-for="Desde" />

    <button type="submit">Buscar</button>
    <a asp-action="Index">Limpiar</a>
</form>

<table>
    <thead>
        <tr><th>Vuelo</th><th>Ruta</th><th>Sale</th><th>Llega</th><th>Duración</th><th>Precio</th><th>Libres</th></tr>
    </thead>
    <tbody>
        @foreach (VueloListadoViewModel vuelo in Model.Resultados)
        {
            <tr class="@(vuelo.YaSalio ? "pasado" : "")">
                <td><a asp-action="Detalle" asp-route-id="@vuelo.Id">@vuelo.Numero</a></td>
                <td title="@vuelo.Ciudades">@vuelo.Ruta</td>
                <td>@vuelo.Salida</td>
                <td>@vuelo.Llegada</td>
                <td>@vuelo.Duracion</td>
                <td>@vuelo.Precio</td>
                <td>@(vuelo.YaSalio ? "salió" : vuelo.Libres.ToString())</td>
            </tr>
        }
    </tbody>
</table>
```

Notar que la vista **no formatea nada**: solo muestra los datos. Si en el futuro el precio debe llevar decimales, se cambia en un
único lugar.

### Paso 3 · El mapa de asientos (la matriz)

Esta es la parte central del desafío. La vista debe dibujar una **grilla**: una fila del avión por renglón, seis
asientos por fila. La forma más simple de dibujarla es que el ViewModel la provea ya construida como una **lista de filas**,
donde cada fila es una **lista de asientos**: `List<List<AsientoViewModel>>`.

```text
Mapa[0]  →  [1A] [1B] [1C] | [1D] [1E] [1F]
Mapa[1]  →  [2A] [2B] [2C] | [2D] [2E] [2F]
Mapa[2]  →  [3A]█ [3B]█ [3C] | [3D] [3E] [3F]      █ = ocupado
…
Mapa[19] →  [20A] …                      [20F]
```

Primero, un asiento. Es un dato pequeño e inmutable: un `record` resulta adecuado.

```csharp
// ViewModels/AsientoViewModel.cs
public record AsientoViewModel(string Codigo, bool Ocupado);
```

Después, el detalle del vuelo. Es el escenario 2 (dos modelos, una vista): el `Vuelo` y la lista de asientos ocupados
que devuelve `ReservaRepository.AsientosOcupados(vueloId)`.

Se recomienda intentarlo antes de consultar la solución: un `for` de filas (de 1 a `vuelo.Filas`) y, dentro, un
`foreach` por cada letra de `Vuelo.LetrasAsiento` (`"ABCDEF"`). El código del asiento es `$"{fila}{letra}"`.

:::solucion Ver VueloDetalleViewModel completo
```csharp
// ViewModels/VueloDetalleViewModel.cs
public class VueloDetalleViewModel
{
    public int Id { get; }
    public string Numero { get; }
    public string Ruta { get; }
    public string Salida { get; }
    public string Llegada { get; }
    public string Precio { get; }
    public int Capacidad { get; }
    public int Ocupados { get; }
    public bool PuedeReservar { get; }

    // Cada elemento es una FILA del avión; cada fila tiene 6 asientos (A–F)
    public List<List<AsientoViewModel>> Mapa { get; } = [];

    public VueloDetalleViewModel(Vuelo vuelo, List<string> asientosOcupados)
    {
        Id = vuelo.Id;
        Numero = vuelo.Numero;
        Ruta = $"{vuelo.Origen} → {vuelo.Destino}";        // usa el ToString() de Aeropuerto
        Salida = vuelo.Salida.ToString("dddd dd/MM/yyyy HH:mm");
        Llegada = vuelo.Llegada.ToString("dddd dd/MM/yyyy HH:mm");
        Precio = vuelo.Precio.ToString("C0");
        Capacidad = vuelo.Capacidad;
        Ocupados = asientosOcupados.Count;
        PuedeReservar = !vuelo.YaSalio && Ocupados < Capacidad;

        var ocupados = asientosOcupados.ToHashSet();   // HashSet: "¿está?" sin recorrer toda la lista
        for (int fila = 1; fila <= vuelo.Filas; fila++)
        {
            var asientos = new List<AsientoViewModel>();
            foreach (char letra in Vuelo.LetrasAsiento)
            {
                string codigo = $"{fila}{letra}";
                asientos.Add(new AsientoViewModel(codigo, ocupados.Contains(codigo)));
            }
            Mapa.Add(asientos);
        }
    }
}
```
:::

Y en `VuelosController`:

```csharp
// GET /Vuelos/Detalle/2
public IActionResult Detalle(int id)
{
    Vuelo? vuelo = vuelos.Obtener(id);
    if (vuelo is null) return NotFound();          // sigue devolviendo 404, como en el desafío 3

    var modelo = new VueloDetalleViewModel(vuelo, reservas.AsientosOcupados(id));
    return View(modelo);
}
```

¿Por qué construir la matriz en C# y no en la vista? Porque en C# se puede **depurar** (con un breakpoint se inspecciona
`Mapa`), y la vista queda con una sola tarea: dibujar. `PuedeReservar` es un dato **para mostrar** (si se ofrecen
enlaces o no): la regla de negocio ("no se reserva un vuelo que ya salió") se controla en el servidor en el desafío 5.

### Paso 4 · Dibujar el mapa

En `Views/Vuelos/Detalle.cshtml`, cambiar el `@model` a `VueloDetalleViewModel`. El CSS ya está en `sitio.css`:
`.avion`, `.fila`, `.numero-fila`, `.pasillo`, `.asiento`, `.asiento.libre` y `.asiento.ocupado`.

Dos `for` anidados: el exterior recorre filas (`Model.Mapa[i]`), el interior los asientos (`Model.Mapa[i][j]`).
Antes del cuarto asiento (`j == 3`, la D) se ubica el pasillo.

```cshtml
@model VueloDetalleViewModel
@{
    ViewData["Title"] = $"Vuelo {Model.Numero}";
}

<h1>Vuelo @Model.Numero</h1>

<dl class="ficha">
    <dt>Ruta</dt>      <dd>@Model.Ruta</dd>
    <dt>Sale</dt>      <dd>@Model.Salida</dd>
    <dt>Llega</dt>     <dd>@Model.Llegada</dd>
    <dt>Precio</dt>    <dd>@Model.Precio</dd>
    <dt>Ocupación</dt> <dd>@Model.Ocupados de @Model.Capacidad asientos</dd>
</dl>

<h2>Mapa de asientos</h2>

<div class="avion">
    @for (int i = 0; i < Model.Mapa.Count; i++)
    {
        <div class="fila">
            <span class="numero-fila">@(i + 1)</span>
            @for (int j = 0; j < Model.Mapa[i].Count; j++)
            {
                AsientoViewModel asiento = Model.Mapa[i][j];
                if (j == 3)
                {
                    <span class="pasillo"></span>
                }
                if (asiento.Ocupado || !Model.PuedeReservar)
                {
                    <span class="asiento @(asiento.Ocupado ? "ocupado" : "")" title="@asiento.Codigo">@asiento.Codigo[^1]</span>
                }
                else
                {
                    <a class="asiento libre" title="Reservar @asiento.Codigo"
                       asp-controller="Reservas" asp-action="Crear"
                       asp-route-vueloId="@Model.Id" asp-route-asiento="@asiento.Codigo">@asiento.Codigo[^1]</a>
                }
            }
        </div>
    }
</div>

<p><a asp-action="Index">← Volver a los vuelos</a></p>
```

`asp-route-vueloId` y `asp-route-asiento` no son parte de la ruta (`{controller}/{action}/{id?}`), así que el tag
helper los agrega al query string: `/Reservas/Crear?vueloId=2&asiento=5C`. Ese enlace es parte del **contrato**: el
verificador lo busca con ese formato exacto.

:::nota
`asiento.Codigo[^1]` es el último carácter del código (la letra): en el cuadrado se muestra "A", y el número de
fila queda a la izquierda. Al pasar el puntero se muestra el código completo (`title`).
:::

### Paso 5 · Reservas y pasajeros con su ViewModel

Una fila del listado de reservas reúne datos de tres entidades (reserva, vuelo con sus aeropuertos, pasajero):

```csharp
// ViewModels/ReservaListadoViewModel.cs
public class ReservaListadoViewModel(Reserva reserva)
{
    public int Id { get; } = reserva.Id;
    public int VueloId { get; } = reserva.Vuelo.Id;
    public string Vuelo { get; } = reserva.Vuelo.Numero;
    public string Ruta { get; } = $"{reserva.Vuelo.Origen.Codigo} → {reserva.Vuelo.Destino.Codigo}";
    public string Salida { get; } = reserva.Vuelo.Salida.ToString("dd/MM HH:mm");
    public int PasajeroId { get; } = reserva.Pasajero.Id;
    public string Pasajero { get; } = reserva.Pasajero.Nombre;
    public string Asiento { get; } = reserva.Asiento;
    public string Estado { get; } = reserva.Estado.ToString();       // enum → texto
    public bool Cancelada { get; } = reserva.Estado == EstadoReserva.Cancelada;
}

// ViewModels/ReservaIndexViewModel.cs — el listado con un filtro por estado (/Reservas?estado=Cancelada)
public class ReservaIndexViewModel
{
    public EstadoReserva? Estado { get; set; }
    public List<ReservaListadoViewModel> Reservas { get; set; } = [];
}
```

Crear `Controllers/ReservasController.cs` con el `Index`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

public class ReservasController(
    ReservaRepository reservas,
    VueloRepository vuelos,
    PasajeroRepository pasajeros) : Controller
{
    // GET /Reservas  ·  GET /Reservas?estado=Confirmada
    public IActionResult Index(EstadoReserva? estado)
    {
        var modelo = new ReservaIndexViewModel
        {
            Estado = estado,
            Reservas = reservas.Listar(estado).Select(r => new ReservaListadoViewModel(r)).ToList()
        };
        return View(modelo);
    }
    // … Crear, en el paso 6
}
```

La vista `Views/Reservas/Index.cshtml` es una tabla con `@model ReservaIndexViewModel` y un `@foreach` sobre
`Model.Reservas` (vuelo, ruta, salida, pasajero, asiento, estado). Las canceladas, con `class="pasado"`. Para los
filtros: `<a asp-action="Index" asp-route-estado="Cancelada">Canceladas</a>`.

En el detalle del pasajero se aplica el escenario 2 de la diapositiva, de forma casi idéntica:

```csharp
// ViewModels/PasajeroDetalleViewModel.cs
public class PasajeroDetalleViewModel(Pasajero pasajero, List<Reserva> reservas)
{
    public int Id { get; } = pasajero.Id;
    public string Nombre { get; } = pasajero.Nombre;
    public string Dni { get; } = pasajero.Dni;
    public string Email { get; } = pasajero.Email ?? "—";
    public List<ReservaListadoViewModel> Reservas { get; } =
        reservas.Select(r => new ReservaListadoViewModel(r)).ToList();
}
```

En el desafío 3 las reservas del pasajero se enviaban por separado, en `ViewData["Reservas"]`, y la vista debía
convertirlas (`as List<Reserva>`). Ahora se envían juntas, con tipo, en un solo objeto:

```csharp
// PasajerosController.Detalle
Pasajero? pasajero = pasajeros.Obtener(id);
if (pasajero is null) return NotFound();

var modelo = new PasajeroDetalleViewModel(pasajero, reservas.ListarPorPasajero(id));
return View(modelo);              // ViewData["Reservas"] ya no es necesario
```

Actualizar `Views/Pasajeros/Detalle.cshtml` a `@model PasajeroDetalleViewModel`, eliminar la conversión de `ViewData`
y recorrer `Model.Reservas`. Notar que se **reutiliza** `ReservaListadoViewModel` para esa tabla: ya incluye la ruta y
la salida formateadas.

### Paso 6 · El formulario de reserva (GET)

Escenario 3: el formulario requiere los campos que carga el usuario **y** las listas para los desplegables.

```csharp
// ViewModels/ReservaCrearViewModel.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class ReservaCrearViewModel
{
    // Lo que carga el usuario (vuelve en el POST)
    [Display(Name = "Vuelo")]
    public int? VueloId { get; set; }

    [Display(Name = "Pasajero")]
    public int? PasajeroId { get; set; }

    [Display(Name = "Asiento")]
    public string Asiento { get; set; } = "";

    // Datos que usa la vista para dibujarse (NO vuelven en el POST)
    public List<SelectListItem> Vuelos { get; set; } = [];
    public List<SelectListItem> Pasajeros { get; set; } = [];
}
```

:::catedra
En la diapositiva, `TurnoCrearViewModel` usa `int PacienteId`. Aquí ya se usa **`int?`**: así, cuando el usuario no
selecciona ninguna opción, el valor es `null` (y no un `0`, que podría confundirse con un Id). En el desafío 5 se agrega
`[Required]` y allí se explica su importancia: es lo que muestra la diapositiva "Validar los desplegables" del tema 13.
:::

El GET recibe `vueloId` y `asiento` del query string (provienen de los enlaces del mapa) y los **precarga**:

```csharp
// GET /Reservas/Crear  ·  GET /Reservas/Crear?vueloId=2&asiento=5C
[HttpGet]
public IActionResult Crear(int? vueloId, string? asiento)
{
    var modelo = new ReservaCrearViewModel { VueloId = vueloId, Asiento = asiento ?? "" };
    CargarListas(modelo);
    return View(modelo);
}

private void CargarListas(ReservaCrearViewModel modelo)
{
    modelo.Vuelos = vuelos.Listar()
        .Where(v => !v.YaSalio)       // en el desplegable, solo los que todavía no salieron
        .Select(v => new SelectListItem($"{v.Numero} · {v.Origen.Codigo} → {v.Destino.Codigo} · {v.Salida:dd/MM HH:mm}", v.Id.ToString()))
        .ToList();
    modelo.Pasajeros = pasajeros.Listar()
        .Select(p => new SelectListItem($"{p.Nombre} ({p.Dni})", p.Id.ToString()))
        .ToList();
}
```

La vista `Views/Reservas/Crear.cshtml`:

```cshtml
@model ReservaCrearViewModel
@{
    ViewData["Title"] = "Nueva reserva";
}

<h1>Nueva reserva</h1>

<form asp-action="Crear" method="post" class="formulario">
    <label asp-for="VueloId"></label>
    <select asp-for="VueloId" asp-items="Model.Vuelos">
        <option value="">— Elegir —</option>
    </select>

    <label asp-for="PasajeroId"></label>
    <select asp-for="PasajeroId" asp-items="Model.Pasajeros">
        <option value="">— Elegir —</option>
    </select>

    <label asp-for="Asiento"></label>
    <input asp-for="Asiento" placeholder="12C" />

    <button type="submit">Reservar</button>
</form>
```

`asp-for="VueloId"` cumple dos funciones: asigna `name="VueloId"` al `<select>` (así se llama el campo en el POST) y
marca como seleccionada la opción cuyo `value` coincide con `Model.VueloId`. Por eso, cuando se llega desde el mapa, el vuelo
aparece seleccionado automáticamente.

### Paso 7 · El POST (simple) y por qué un ViewModel

Por ahora el POST implementa lo mínimo, igual que el `TurnosController` del tema 12: busca el vuelo y el pasajero, y si
algún dato no es válido vuelve a mostrar el formulario **recargando las listas**.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Crear(ReservaCrearViewModel modelo)
{
    Vuelo? vuelo = modelo.VueloId is null ? null : vuelos.Obtener(modelo.VueloId.Value);
    Pasajero? pasajero = modelo.PasajeroId is null ? null : pasajeros.Obtener(modelo.PasajeroId.Value);

    // La validación completa corresponde al desafío 5
    if (vuelo is null || pasajero is null || string.IsNullOrWhiteSpace(modelo.Asiento))
    {
        CargarListas(modelo);      // ← las listas no vuelven en el POST
        return View(modelo);
    }

    // ViewModel → entidad: la Fecha y el Estado los asigna el servidor, no el formulario
    var reserva = new Reserva
    {
        Vuelo = vuelo,
        Pasajero = pasajero,
        Asiento = modelo.Asiento.Trim().ToUpperInvariant(),   // "5c " → "5C"
        Fecha = DateTime.Now,
        Estado = EstadoReserva.Confirmada
    };
    reservas.Insertar(reserva);

    // POST → Redirect → GET: se vuelve al mapa y el asiento reservado aparece ocupado
    return RedirectToAction("Detalle", "Vuelos", new { id = vuelo.Id });
}
```

¿Por qué no recibir directamente `Crear(Reserva reserva)`? Porque `Reserva` tiene `Estado` y `Fecha`. Con las
herramientas del navegador (F12) cualquier usuario puede agregar `<input name="Estado" value="Cancelada">` al formulario
y el binding asigna ese valor. Eso es **overposting**. `ReservaCrearViewModel` solo tiene `VueloId`, `PasajeroId` y
`Asiento`: lo demás no tiene dónde asignarse. Además, `Reserva` tiene un `Vuelo` completo, no un `VueloId`: la
estructura del formulario ni siquiera coincide con la de la entidad.

Probar el flujo completo: `/Vuelos` → AT1001 → clic en el 5C → el formulario aparece con el vuelo y el asiento
cargados → seleccionar un pasajero → **Reservar** → se vuelve al mapa con el 5C en gris. Verificar la fila nueva en DB
Browser.
