## :target: Misión

Hasta este punto AeroTaller contenía todos los datos, pero no los mostraba. En este desafío se incorporan las pantallas
de la aplicación:

1. La **página de inicio** pasa a ser un tablero con la ocupación de las próximas salidas.
2. **`/Vuelos`**: el listado de vuelos, con un enlace al detalle de cada uno.
3. **`/Vuelos/Detalle/{id}`**: la ficha del vuelo, o un **404** si no existe.
4. **`/Pasajeros`**: el listado con un **buscador** (`?buscar=ana`), y **`/Pasajeros/Detalle/{id}`** con las reservas de esa persona.

Los repositorios del desafío 2 se usan sin cambios. Los controladores **no escriben SQL ni abren conexiones**: solo obtienen
datos de los repositorios y determinan qué vista los muestra. Todavía no hay formularios que guarden datos (eso empieza en el desafío 4).

:::nota
El enlace **Reservas** del menú sigue devolviendo 404 hasta el desafío 4, y el mapa de asientos del detalle del vuelo
también se incorpora allí. Es intencional: en este desafío el foco está en el recorrido del request, de la URL a la vista.
:::

## :book-open-text: Repaso

El recorrido de un request en MVC, como en el tema 10, aplicado a AeroTaller:

| Paso | Qué ocurre | En AeroTaller |
|---|---|---|
| 1 | el navegador envía la solicitud | `GET /Vuelos/Detalle/3` |
| 2 | el **ruteo** convencional `{controller=Home}/{action=Index}/{id?}` | controlador `Vuelos`, acción `Detalle`, `id = 3` |
| 3 | ASP.NET **crea el controlador** y le pasa las dependencias que declara su constructor | `new VuelosController(un VueloRepository)` |
| 4 | la **acción** usa el modelo | `vuelos.Obtener(3)` → un `Vuelo` (o `null` → `NotFound()`) |
| 5 | `View(vuelo)` selecciona la **vista** y le pasa el modelo | `Views/Vuelos/Detalle.cshtml` con `@model Vuelo` |
| 6 | Razor genera el HTML dentro del `_Layout` | se devuelve una página HTML común, sin código C# |

Lo necesario de Razor (tema 11):

| Sintaxis | Uso |
|---|---|
| `@model List<Vuelo>` | declarar qué tipo de objeto recibe la vista (al comienzo del archivo, con minúscula) |
| `Model` | ese objeto, con IntelliSense (con mayúscula) |
| `@vuelo.Numero` / `@(vuelo.Precio * 2)` | mostrar un valor; con paréntesis si hay espacios u operadores |
| `@foreach`, `@if` | recorrer y decidir, dentro del HTML |
| `<a asp-action="Detalle" asp-route-id="@vuelo.Id">` | un enlace generado a partir del ruteo: `/Vuelos/Detalle/3` |
| `ViewData["Title"] = "Vuelos";` | el título que usa el layout en `<title>` |

:::catedra
Todo esto está en las diapositivas del [tema 10](https://taller-de-lenguajes-2.github.io/clases/tema-10-mvc/index.html)
("El controlador", "La vista: HTML con C#", "Program.cs: de Web API a MVC") y del
[tema 11](https://taller-de-lenguajes-2.github.io/clases/tema-11-layout-y-vistas/index.html) ("GET con query string",
"Links con asp-route", "El título de cada página"). El proyecto de ejemplo de la clínica sigue el mismo esquema: ante una
duda, conviene abrir `tema-11-layout-y-vistas/proyecto-ejemplo/Controllers/PacientesController.cs` y compararlo con el
propio.
:::

### :lightbulb: Tips y recursos

**¿Cómo llega el repositorio al controlador?**

| Forma | Código | A favor | En contra | Cuándo |
|---|---|---|---|---|
| Registrado con una lambda y recibido por el constructor | `AddScoped(_ => new VueloRepository(cadenaConexion))` | la cadena se define en un solo lugar (`Program.cs`); el controlador no depende de su origen | el controlador depende de la clase concreta | **en este desafío** (tema 10) |
| `new` dentro de la acción | `var vuelos = new VueloRepository("Data Source=aerotaller.db");` | no requiere ningún registro | cadena repetida en cada controlador; el verificador no puede cambiar la base; no es posible cambiar de repositorio sin modificar todos los controladores | nunca |
| Interfaz + registro por tipo | `AddScoped<IVueloRepository, VueloRepository>()` + `ConexionDb` | el controlador depende de un contrato: SQLite puede reemplazarse por datos en memoria modificando solo `Program.cs` | más componentes (interfaz, `ConexionDb`) | desafío 7 (tema 14) |

**¿Cómo enviar a una vista más de un dato?**

| Forma | En el controlador | En la vista | A favor | En contra |
|---|---|---|---|---|
| `ViewData` | `ViewData["Reservas"] = lista;` | `ViewData["Reservas"] as List<Reserva>` | ya es conocido (el `Title`) | sin tipo: un error de tipeo en la clave devuelve `null`, sin ninguna advertencia |
| `ViewBag` | `ViewBag.Reservas = lista;` | `ViewBag.Reservas` | más corto | `dynamic`: sin IntelliSense y los errores aparecen recién al abrir la página |
| Tupla como modelo | `View((pasajero, lista))` | `@model (Pasajero Pasajero, List<Reserva> Reservas)` | con tipo | pierde legibilidad con tres o cuatro datos |
| Un ViewModel | `View(new PasajeroDetalleViewModel(…))` | `@model PasajeroDetalleViewModel` | con tipo, con nombre, diseñado para esa pantalla | una clase más (se incorpora en el **desafío 4**) |

:::catedra
En el [tema 11](https://taller-de-lenguajes-2.github.io/clases/tema-11-layout-y-vistas/index.html) la diapositiva
"El título de cada página" lo anticipa: `ViewData` sirve para datos sueltos como el título; para los datos de la página
conviene un modelo con tipo, que es el tema 12. Por eso en este desafío `ViewData` se usa para las reservas **de forma
provisoria**. Otra diferencia menor: en el ejemplo de la clínica el buscador usa `?texto=ana`; en AeroTaller el contrato
establece `?buscar=ana`.
:::

## :code-xml: Paso a paso

### Paso 1 · Registrar los repositorios en `Program.cs`

Un controlador no ejecuta `new VueloRepository(…)`: lo **declara** como parámetro de su constructor y ASP.NET se lo
provee. Para eso, el repositorio debe estar registrado en el **contenedor de servicios**. En `Program.cs`, después de `CrearBaseSiNoExiste`
y antes de `AddControllersWithViews`:

```csharp
// Desafío 3: los repositorios, en el contenedor. Cada vez que se resuelve uno para un controlador, se ejecuta la lambda.
builder.Services.AddScoped(_ => new AeropuertoRepository(cadenaConexion));
builder.Services.AddScoped(_ => new VueloRepository(cadenaConexion));
builder.Services.AddScoped(_ => new PasajeroRepository(cadenaConexion));
builder.Services.AddScoped(_ => new ReservaRepository(cadenaConexion));
```

- **`AddScoped`**: un objeto nuevo **por request** (si en el mismo request dos clases declaran un `VueloRepository` como dependencia, reciben la misma instancia).
- **`_ => new …`**: una función que construye el objeto. El `_` es un parámetro que no se usa (el propio contenedor).
  La cadena solo se utiliza en `Program.cs`, el único lugar donde se lee de la configuración.

Es la misma línea del tema 10. Los tiempos de vida (`Scoped`, `Singleton`, `Transient`) y la variante "con
interfaces" se estudian a fondo en el desafío 7.

### Paso 2 · El tablero en la página de inicio

`HomeController` pasa a recibir un `VueloRepository` y a enviarle a la vista la ocupación (la consulta con `LEFT JOIN` y
`GROUP BY` escrita en el desafío 2):

```csharp
using Microsoft.AspNetCore.Mvc;

public class HomeController(VueloRepository vuelos) : Controller
{
    // GET /  →  el tablero: ocupación de los próximos vuelos
    public IActionResult Index()
    {
        List<OcupacionVuelo> ocupacion = vuelos.ListarOcupacion();
        return View(ocupacion);      // Views/Home/Index.cshtml recibe la lista
    }
}
```

Reemplazar el contenido de `Views/Home/Index.cshtml` por una tabla con número (enlace al detalle), ruta, salida y
ocupación. Para la barra de ocupación, el CSS de la plantilla ya incluye las clases `barra`:

```cshtml
<div class="barra" title="@vuelo.Ocupados de @vuelo.Capacidad">
    <span style="width: @(vuelo.Porcentaje)%"></span>
</div>
@vuelo.Ocupados / @vuelo.Capacidad
```

:::solucion Ver Views/Home/Index.cshtml
```cshtml
@model List<OcupacionVuelo>
@{
    ViewData["Title"] = "Tablero";
}

<h1>Próximas salidas</h1>
<p class="nota">Ocupación de cada vuelo: reservas confirmadas sobre la capacidad del avión.</p>

<table>
    <thead>
        <tr><th>Vuelo</th><th>Ruta</th><th>Salida</th><th>Ocupación</th></tr>
    </thead>
    <tbody>
        @foreach (OcupacionVuelo vuelo in Model)
        {
            <tr>
                <td><a asp-controller="Vuelos" asp-action="Detalle" asp-route-id="@vuelo.VueloId">@vuelo.Numero</a></td>
                <td>@vuelo.Ruta</td>
                <td>@vuelo.Salida.ToString("ddd dd/MM HH:mm")</td>
                <td>
                    <div class="barra" title="@vuelo.Ocupados de @vuelo.Capacidad">
                        <span style="width: @(vuelo.Porcentaje)%"></span>
                    </div>
                    @vuelo.Ocupados / @vuelo.Capacidad
                </td>
            </tr>
        }
    </tbody>
</table>

@if (Model.Count == 0)
{
    <p>No hay vuelos programados.</p>
}
```
:::

`asp-controller="Vuelos"` es necesario porque el enlace parte de una vista de **Home** y apunta a otro controlador. Sin él,
el tag helper genera la URL con el controlador actual.

### Paso 3 · El listado de vuelos

Crear `Controllers/VuelosController.cs` con la acción `Index` y la vista `Views/Vuelos/Index.cshtml`. La carpeta se llama
como el controlador **sin** el sufijo `Controller`, y el archivo como la acción.

```csharp
using Microsoft.AspNetCore.Mvc;

// El controlador recibe sus dependencias por el constructor: nunca instancia un repositorio con "new"
public class VuelosController(VueloRepository vuelos) : Controller
{
    // GET /Vuelos
    public IActionResult Index()
    {
        List<Vuelo> lista = vuelos.Listar();
        return View(lista);
    }
}
```

En la vista conviene aprovechar las propiedades que ya expone el modelo: `vuelo.Origen.Codigo`, `vuelo.Llegada`,
`vuelo.LlegaAlOtroDia`, `vuelo.YaSalio`. Ninguno de esos valores se calcula en la vista: el modelo los provee calculados.

| Columna | Cómo |
|---|---|
| Vuelo | enlace al detalle: `<a asp-action="Detalle" asp-route-id="@vuelo.Id">@vuelo.Numero</a>` |
| Ruta | `@vuelo.Origen.Codigo → @vuelo.Destino.Codigo` (las ciudades, en un `title`) |
| Sale | `@vuelo.Salida.ToString("ddd dd/MM HH:mm")` → "mié 18/11 07:30" (con la cultura es-AR configurada en `Program.cs`, el día se muestra en castellano) |
| Llega | la hora, y un "(+1)" si `LlegaAlOtroDia` |
| Precio | `@vuelo.Precio.ToString("C0")` → "$ 98.000" |

Los vuelos que ya salieron se muestran en gris: el CSS incluye la clase `pasado` para las filas (`<tr class="@(vuelo.YaSalio ? "pasado" : "")">`).

:::solucion Ver Views/Vuelos/Index.cshtml
```cshtml
@model List<Vuelo>
@{
    ViewData["Title"] = "Vuelos";
}

<h1>Vuelos</h1>

<table>
    <thead>
        <tr><th>Vuelo</th><th>Ruta</th><th>Sale</th><th>Llega</th><th>Precio</th></tr>
    </thead>
    <tbody>
        @foreach (Vuelo vuelo in Model)
        {
            <tr class="@(vuelo.YaSalio ? "pasado" : "")">
                <td><a asp-action="Detalle" asp-route-id="@vuelo.Id">@vuelo.Numero</a></td>
                <td title="@vuelo.Origen.Ciudad → @vuelo.Destino.Ciudad">@vuelo.Origen.Codigo → @vuelo.Destino.Codigo</td>
                <td>@vuelo.Salida.ToString("ddd dd/MM HH:mm")</td>
                <td>@vuelo.Llegada.ToString("HH:mm")@(vuelo.LlegaAlOtroDia ? " (+1)" : "")</td>
                <td>@vuelo.Precio.ToString("C0")</td>
            </tr>
        }
    </tbody>
</table>

@if (Model.Count == 0)
{
    <p>No hay vuelos cargados.</p>
}
```
:::

### Paso 4 · El detalle del vuelo (y el 404)

Agregar a `VuelosController` la acción `Detalle`. El parámetro **debe llamarse `id`**, igual que en la plantilla de la
ruta, para que el ruteo le asigne el valor de `/Vuelos/Detalle/3`:

```csharp
// GET /Vuelos/Detalle/3  →  id = 3 (ruteo convencional)
public IActionResult Detalle(int id)
{
    Vuelo? vuelo = vuelos.Obtener(id);
    if (vuelo is null) return NotFound();   // 404: el vuelo no existe

    return View(vuelo);                     // Views/Vuelos/Detalle.cshtml con @model Vuelo
}
```

El repositorio devuelve `null` cuando el vuelo no existe; el **controlador** traduce ese resultado a HTTP (404). Cada
componente cumple su propia responsabilidad, como en el tema 09.

La vista es una ficha con `<dl class="ficha">` (la plantilla ya incluye el estilo): ruta con ciudades y códigos, salida,
llegada, duración, capacidad y precio. Si el vuelo ya salió, debe indicarse.

:::pista La duración en horas y minutos
`DuracionMinutos` es un `int`: `@(Model.DuracionMinutos / 60) h @(Model.DuracionMinutos % 60) min`. Sin los paréntesis,
Razor mostraría el número y después el texto "/ 60".
:::

:::solucion Ver Views/Vuelos/Detalle.cshtml
```cshtml
@model Vuelo
@{
    ViewData["Title"] = $"Vuelo {Model.Numero}";
}

<h1>Vuelo @Model.Numero</h1>

<dl class="ficha">
    <dt>Ruta</dt>      <dd>@Model.Origen.Ciudad (@Model.Origen.Codigo) → @Model.Destino.Ciudad (@Model.Destino.Codigo)</dd>
    <dt>Sale</dt>      <dd>@Model.Salida.ToString("dddd dd/MM/yyyy HH:mm")</dd>
    <dt>Llega</dt>     <dd>@Model.Llegada.ToString("dddd dd/MM/yyyy HH:mm")</dd>
    <dt>Duración</dt>  <dd>@(Model.DuracionMinutos / 60) h @(Model.DuracionMinutos % 60) min</dd>
    <dt>Capacidad</dt> <dd>@Model.Capacidad asientos (@Model.Filas filas de 6)</dd>
    <dt>Precio</dt>    <dd>@Model.Precio.ToString("C0")</dd>
</dl>

@if (Model.YaSalio)
{
    <p class="nota">Este vuelo ya salió: no admite reservas.</p>
}

<p><a asp-action="Index">← Volver a los vuelos</a></p>
```
:::

Probar `/Vuelos/Detalle/2` (el AT1001) y después `/Vuelos/Detalle/999`. El navegador muestra su propia página de error
404: es lo esperado (el verificador controla el **código** de respuesta, no el diseño).

### Paso 5 · Pasajeros, con buscador

`PasajerosController.Index` recibe el texto a buscar por **query string**: `/Pasajeros?buscar=ana`. El parámetro tiene
el mismo nombre que la clave de la URL y es `string?`, porque cuando la clave no está presente su valor es `null`:

```csharp
using Microsoft.AspNetCore.Mvc;

public class PasajerosController(PasajeroRepository pasajeros, ReservaRepository reservas) : Controller
{
    // GET /Pasajeros             → todos
    // GET /Pasajeros?buscar=ana  → filtrados por nombre o DNI
    public IActionResult Index(string? buscar)
    {
        List<Pasajero> lista = string.IsNullOrWhiteSpace(buscar)
            ? pasajeros.Listar()
            : pasajeros.Buscar(buscar);

        ViewData["Buscar"] = buscar;    // para que el buscador muestre lo que se buscó
        return View(lista);
    }
}
```

En la vista, el buscador es un formulario **GET**: sus campos se envían en la URL, de modo que no se requiere ninguna
acción POST. El `name` del input debe ser `buscar`, igual que el parámetro:

```cshtml
@* method="get": el texto viaja en la URL → /Pasajeros?buscar=ana *@
<form asp-action="Index" method="get" class="buscador">
    <input type="search" name="buscar" value="@ViewData["Buscar"]" placeholder="Nombre o DNI" />
    <button type="submit">Buscar</button>
</form>
```

Debajo, una tabla con nombre (enlace al detalle), DNI y email (`@(pasajero.Email ?? "—")`), o un mensaje si no hay resultados.

:::cuidado
Probar el buscador con `ana`, con `ez` o con un fragmento de DNI (`301`). **No** conviene probar con `gom` esperando
encontrar a Gómez: el `LIKE` de SQLite no iguala `o` con `ó` (como se observó en la consulta 2 del desafío 1). No es un
error de la implementación.
:::

:::solucion Ver Views/Pasajeros/Index.cshtml
```cshtml
@model List<Pasajero>
@{
    ViewData["Title"] = "Pasajeros";
}

<h1>Pasajeros</h1>

@* method="get": el texto viaja en la URL → /Pasajeros?buscar=ana *@
<form asp-action="Index" method="get" class="buscador">
    <input type="search" name="buscar" value="@ViewData["Buscar"]" placeholder="Nombre o DNI" />
    <button type="submit">Buscar</button>
</form>

@if (Model.Count == 0)
{
    <p>No hay pasajeros que coincidan con «@ViewData["Buscar"]».</p>
}
else
{
    <table>
        <thead>
            <tr><th>Nombre</th><th>DNI</th><th>Email</th></tr>
        </thead>
        <tbody>
            @foreach (Pasajero pasajero in Model)
            {
                <tr>
                    <td><a asp-action="Detalle" asp-route-id="@pasajero.Id">@pasajero.Nombre</a></td>
                    <td>@pasajero.Dni</td>
                    <td>@(pasajero.Email ?? "—")</td>
                </tr>
            }
        </tbody>
    </table>
}
```
:::

### Paso 6 · El detalle del pasajero, con sus reservas

Aquí aparece un problema nuevo: la vista necesita **dos datos** (el pasajero y sus reservas), pero `View(…)` recibe **un
solo** modelo. Por ahora, el pasajero se pasa como modelo y las reservas se envían por separado, en `ViewData`:

```csharp
// GET /Pasajeros/Detalle/1
public IActionResult Detalle(int id)
{
    Pasajero? pasajero = pasajeros.Obtener(id);
    if (pasajero is null) return NotFound();

    // Provisorio: View() recibe UN modelo. Las reservas viajan aparte, en el diccionario ViewData.
    // En el desafío 4 esto se reemplaza por un PasajeroDetalleViewModel.
    ViewData["Reservas"] = reservas.ListarPorPasajero(id);
    return View(pasajero);
}
```

`ViewData` almacena todo como `object`: en la vista es necesario convertirlo nuevamente al tipo real.

```cshtml
@model Pasajero
@{
    ViewData["Title"] = Model.Nombre;
    // ViewData almacena object: se convierte al tipo real. Si la clave no existe, se usa una lista vacía (así el foreach no falla)
    var reservasDelPasajero = ViewData["Reservas"] as List<Reserva> ?? new List<Reserva>();
}
```

Después, una ficha con DNI y email, y una tabla con las reservas: vuelo (enlace a `/Vuelos/Detalle/{id}`), ruta, salida,
asiento y estado. Las canceladas, con la clase `pasado`.

:::solucion Ver Views/Pasajeros/Detalle.cshtml
```cshtml
@model Pasajero
@{
    ViewData["Title"] = Model.Nombre;
    // ViewData almacena object: se convierte al tipo real. Si la clave no existe, se usa una lista vacía (así el foreach no falla)
    var reservasDelPasajero = ViewData["Reservas"] as List<Reserva> ?? new List<Reserva>();
}

<h1>@Model.Nombre</h1>

<dl class="ficha">
    <dt>DNI</dt>   <dd>@Model.Dni</dd>
    <dt>Email</dt> <dd>@(Model.Email ?? "—")</dd>
</dl>

<h2>Reservas</h2>
@if (reservasDelPasajero.Count == 0)
{
    <p>Todavía no tiene reservas.</p>
}
else
{
    <table>
        <thead>
            <tr><th>Vuelo</th><th>Ruta</th><th>Salida</th><th>Asiento</th><th>Estado</th></tr>
        </thead>
        <tbody>
            @foreach (Reserva reserva in reservasDelPasajero)
            {
                <tr class="@(reserva.Estado == EstadoReserva.Cancelada ? "pasado" : "")">
                    <td><a asp-controller="Vuelos" asp-action="Detalle" asp-route-id="@reserva.Vuelo.Id">@reserva.Vuelo.Numero</a></td>
                    <td>@reserva.Vuelo.Origen.Codigo → @reserva.Vuelo.Destino.Codigo</td>
                    <td>@reserva.Vuelo.Salida.ToString("dd/MM HH:mm")</td>
                    <td>@reserva.Asiento</td>
                    <td>@reserva.Estado</td>
                </tr>
            }
        </tbody>
    </table>
}

<p><a asp-action="Index">← Volver a los pasajeros</a></p>
```
:::

Abrir `/Pasajeros/Detalle/1` (Ana Gómez): tres reservas en el 3A, una de ellas de un vuelo que ya salió. Luego,
abrir `/Pasajeros/Detalle/4` (Diego Herrera): una reserva cancelada, en gris.

### Paso 7 · Recorrer la app y revisar

Navegar **solo con clics**, sin escribir URLs: inicio → un vuelo del tablero → volver → Pasajeros → buscar → un pasajero →
uno de sus vuelos. Si algún enlace conduce a una página inesperada, revisar su tag helper.

Antes del commit, tres controles rápidos:

- En `Controllers/` no hay ningún `using Microsoft.Data.Sqlite` ni ningún `new …Repository(`.
- Cada vista tiene su `ViewData["Title"]` (se comprueba en la pestaña del navegador).
- Ningún enlace está escrito manualmente con `href="/Vuelos/…"`: todos usan `asp-action` / `asp-controller` / `asp-route-…`.
