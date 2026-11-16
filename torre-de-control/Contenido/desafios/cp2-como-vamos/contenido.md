## 🛬 ¿Cómo vamos?

Aterrizaje técnico a mitad de camino. Desde el checkpoint anterior, AeroTaller incorporó **la interfaz** (controladores
y vistas), **la forma justa de los datos para cada pantalla** (ViewModels, con el mapa de asientos) y **el control
migratorio** (validación). Así está armado en este punto:

```text
 Navegador
    │  GET /Vuelos/Detalle/2          POST /Reservas/Crear (VueloId, PasajeroId, Asiento + token)
    ▼
 Controllers ──────────── reciben el request, arman ViewModels, validan y eligen la vista
    │     ▲                      ⚠ las reglas con datos (asiento libre, vuelo lleno…) por ahora viven AQUÍ
    │     │ ViewModels
    │     └──────────────── Views (Razor): solo dibujan lo que reciben
    ▼
 Repositorios ─────────── todo el SQL, con parámetros (VueloRepository, ReservaRepository…)
    ▼
 SQLite (aerotaller.db) ── tablas, claves foráneas, UNIQUE, CHECK
```

| Capa | Qué hay | Desafío |
|---|---|---|
| Base | `aerotaller.sql` con 4 tablas, claves foráneas, datos de ejemplo; se crea sola al arrancar | 1 |
| Modelos | `Aeropuerto`, `Vuelo` (con `Origen`/`Destino`, `Capacidad`, `YaSalio`, `TieneAsiento`), `Pasajero`, `Reserva`, `EstadoReserva` | 2 |
| Repositorios | uno por entidad, reciben la cadena por el constructor, siempre con `AddWithValue` | 2 |
| Controladores | `Home` (tablero), `Vuelos`, `Pasajeros`, `Reservas` | 3 · 4 · 5 |
| ViewModels | listados, detalles (mapa de asientos), formularios con listas | 4 |
| Validación | atributos + `ModelState` + `[FechaFutura]` + reglas con `AddModelError` | 5 |

Notar la marca ⚠: el controlador de reservas creció considerablemente. Recibe el formulario, consulta tres
repositorios, decide cinco reglas del negocio, traduce el ViewModel y elige la vista. Son **demasiadas
responsabilidades** para una sola clase. De eso se ocupa lo que viene.

:::cuidado
Antes de seguir: **commit, push y verificación en GitHub**. Abrir el repositorio en el navegador y confirmar que están
la carpeta `ViewModels/`, `Validaciones/FechaFuturaAttribute.cs` y el último commit del desafío 5. Lo que no está en
GitHub no existe para la corrección.
:::

## ✅ Lo que ya se sabe hacer

Responder con sinceridad, sin consultar el código. Lo que resulte difícil conviene anotarlo en la bitácora para la
consulta. Todo esto **se evalúa en el parcial**.

**Controladores y vistas**

- ¿Es posible explicar qué recorrido hace `GET /Vuelos/Detalle/2` desde que llega hasta que se devuelve HTML, nombrando
  cada pieza (ruteo, controlador, acción, repositorio, ViewModel, vista, layout)?
- ¿Es posible devolver un 404 cuando el id no existe, sin que falle la vista?
- ¿Es posible armar un enlace con `asp-action`, `asp-controller` y `asp-route-…`, e indicar qué URL genera?

**ViewModels**

- Para una pantalla nueva, ¿es posible decidir si alcanza con la entidad o hace falta un ViewModel, y justificarlo?
- ¿Es posible escribir un ViewModel que reúna **dos** modelos (como `PasajeroDetalleViewModel`)?
- ¿Es posible armar una **matriz** (`List<List<…>>`) en un ViewModel y recorrerla con dos `for` en Razor?
- ¿Es posible explicar por qué las listas de un `<select>` no vuelven en el POST y qué hace `CargarListas(modelo)`?
- ¿Es posible explicar qué es el **overposting** y cómo lo evita un ViewModel?

**Validación**

- ¿Es posible elegir el atributo adecuado para cada caso: `[Required]`, `[StringLength]`, `[Range]`,
  `[RegularExpression]`, `[EmailAddress]`, `[Display]`, `[ValidateNever]`?
- ¿Es posible explicar por qué un combo es `int?` con `[Required]` y no `int`?
- ¿Es posible escribir un atributo propio heredando de `ValidationAttribute`?
- ¿Es posible agregar una regla que consulte la base con `ModelState.AddModelError(nameof(…), …)` y que el error
  aparezca junto al campo correcto?
- ¿Es posible demostrar que el servidor valida aunque el navegador no lo haga (JavaScript desactivado), y explicar qué
  hace `[ValidateAntiForgeryToken]` si falta el token?
- ¿Es posible explicar por qué después de un POST exitoso se redirige (POST → Redirect → GET)?

**Depurador**

- ¿Es posible detener la ejecución en un POST y encontrar en **Variables** qué valor llegó en cada campo del ViewModel y
  qué errores tiene `ModelState`?

Si la respuesta fue "no" en más de tres, conviene hacer el repaso sugerido más abajo antes de seguir. El desafío 6
**mueve** código que ya existe: si ese código no se entiende del todo, trasladarlo resulta el doble de difícil.

## 🧭 Lo que viene

La segunda mitad del vuelo consiste en ordenar la cabina:

| Desafío | Qué cambia | La idea |
|---|---|---|
| 6 · Servicios | las reglas de reserva, pasajeros y vuelos salen del controlador y pasan a `ReservaService`, `PasajeroService` y `VueloService`; aparecen `ReglaDeNegocioException`, la cancelación de reservas y la edición/eliminación de pasajeros | cada capa con **una** responsabilidad: el controlador queda "delgado" |
| 7 · Inyección de dependencias | interfaces para los repositorios, `ConexionDb`, y un interruptor para cambiar SQLite por datos en memoria **modificando solo `Program.cs`** | depender de contratos, no de clases concretas |
| 8 · Excepciones y logs | qué hacer cuando ocurre un error real, Serilog a archivo y una página de error adecuada | que el usuario vea un mensaje claro y el programador, el detalle |

Después vienen el checkpoint 3, la autenticación (desafío 9) y el **simulacro de parcial**.

## 🔎 Repaso sugerido

| Si resultó difícil… | Releer | Y practicar |
|---|---|---|
| el recorrido del request, el ruteo, `asp-route-…` | [tema 10](https://taller-de-lenguajes-2.github.io/clases/tema-10-mvc/index.html) y [tema 11](https://taller-de-lenguajes-2.github.io/clases/tema-11-layout-y-vistas/index.html) | breakpoint en `VuelosController.Detalle` y observar `id` con distintas URLs |
| cuándo usar un ViewModel, overposting, `CargarListas` | [tema 12](https://taller-de-lenguajes-2.github.io/clases/tema-12-viewmodels/index.html), escenarios 1 a 3 | breakpoint en el POST de `Reservas/Crear` y observar `modelo.Vuelos.Count` |
| atributos, `ModelState`, `AddModelError` | [tema 13](https://taller-de-lenguajes-2.github.io/clases/tema-13-validacion/index.html) | JavaScript desactivado, formulario vacío, y observar `ModelState.ErrorCount` |
| el SQL de los repositorios (JOIN doble, `GROUP BY`) | [tema 07](https://taller-de-lenguajes-2.github.io/clases/tema-07-bases-de-datos/index.html) y [tema 08](https://taller-de-lenguajes-2.github.io/clases/tema-08-ado-net/index.html) | ejecutar la consulta en DB Browser antes de pasarla a C# |

Un ejercicio de repaso muy similar a un punto de parcial: **sin consultar el código**, escribir en un archivo aparte el
`ReservaCrearViewModel` completo (con atributos) y el POST de `Reservas/Crear`. Después, compararlo con lo implementado.

:::nota
Más abajo está la bitácora: **lo realizado / lo pendiente / dudas**. Conviene listar dudas concretas ("no queda claro
por qué el `select` vuelve vacío", "no queda claro cuándo va `nameof`") y llevarlas a la consulta. Una duda bien
formulada ya está a mitad de camino de resolverse.
:::
