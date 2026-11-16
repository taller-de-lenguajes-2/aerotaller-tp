## 🛬 ¿Cómo vamos?

Aterrizaje técnico a mitad de camino. Desde el checkpoint anterior le sumaste a AeroTaller **la cara** (controladores
y vistas), **la forma justa de los datos para cada pantalla** (ViewModels, con el mapa de asientos) y **el control
migratorio** (validación). Así está armado hoy:

```text
 Navegador
    │  GET /Vuelos/Detalle/2          POST /Reservas/Crear (VueloId, PasajeroId, Asiento + token)
    ▼
 Controllers ──────────── reciben el request, arman ViewModels, validan y eligen la vista
    │     ▲                      ⚠ las reglas con datos (asiento libre, vuelo lleno…) hoy viven ACÁ
    │     │ ViewModels
    │     └──────────────── Views (Razor): solo dibujan lo que les llega
    ▼
 Repositorios ─────────── todo el SQL, con parámetros (VueloRepository, ReservaRepository…)
    ▼
 SQLite (aerotaller.db) ── tablas, claves foráneas, UNIQUE, CHECK
```

| Capa | Qué tenés | Desafío |
|---|---|---|
| Base | `aerotaller.sql` con 4 tablas, claves foráneas, datos de ejemplo; se crea sola al arrancar | 1 |
| Modelos | `Aeropuerto`, `Vuelo` (con `Origen`/`Destino`, `Capacidad`, `YaSalio`, `TieneAsiento`), `Pasajero`, `Reserva`, `EstadoReserva` | 2 |
| Repositorios | uno por entidad, reciben la cadena por el constructor, siempre con `AddWithValue` | 2 |
| Controladores | `Home` (tablero), `Vuelos`, `Pasajeros`, `Reservas` | 3 · 4 · 5 |
| ViewModels | listados, detalles (mapa de asientos), formularios con listas | 4 |
| Validación | atributos + `ModelState` + `[FechaFutura]` + reglas con `AddModelError` | 5 |

Fijate en la marca ⚠: el controlador de reservas creció un montón. Recibe el formulario, consulta tres repositorios,
decide cinco reglas del negocio, traduce el ViewModel y elige la vista. Son **demasiadas responsabilidades** para una
sola clase. De eso se trata lo que viene.

:::cuidado
Antes de seguir: **commit, push y mirar GitHub**. Abrí tu repo en el navegador y confirmá que ves la carpeta
`ViewModels/`, `Validaciones/FechaFuturaAttribute.cs` y el último commit del desafío 5. Lo que no está en GitHub, para
la corrección no existe.
:::

## ✅ Lo que ya sabés hacer

Respondé con honestidad, sin mirar tu código. Lo que te cueste, anotalo en la bitácora para la consulta.
Todo esto **cae en el parcial**.

**Controladores y vistas**

- ¿Podrías explicar qué recorrido hace `GET /Vuelos/Detalle/2` desde que llega hasta que vuelve HTML, nombrando cada
  pieza (ruteo, controlador, acción, repositorio, ViewModel, vista, layout)?
- ¿Podrías devolver un 404 cuando el id no existe, sin que explote la vista?
- ¿Podrías armar un link con `asp-action`, `asp-controller` y `asp-route-…`, y decir qué URL genera?

**ViewModels**

- ¿Podrías decir, para una pantalla nueva, si alcanza con la entidad o hace falta un ViewModel, y por qué?
- ¿Podrías escribir un ViewModel que junte **dos** modelos (como `PasajeroDetalleViewModel`)?
- ¿Podrías armar una **matriz** (`List<List<…>>`) en un ViewModel y recorrerla con dos `for` en Razor?
- ¿Podrías explicar por qué las listas de un `<select>` no vuelven en el POST y qué hace `CargarListas(modelo)`?
- ¿Podrías explicar qué es el **overposting** y cómo lo evita un ViewModel?

**Validación**

- ¿Podrías elegir el atributo justo para cada caso: `[Required]`, `[StringLength]`, `[Range]`,
  `[RegularExpression]`, `[EmailAddress]`, `[Display]`, `[ValidateNever]`?
- ¿Podrías explicar por qué un combo es `int?` con `[Required]` y no `int`?
- ¿Podrías escribir un atributo propio heredando de `ValidationAttribute`?
- ¿Podrías agregar una regla que consulte la base con `ModelState.AddModelError(nameof(…), …)` y que el error aparezca
  al lado del campo correcto?
- ¿Podrías demostrar que tu servidor valida aunque el navegador no (JavaScript desactivado) y qué hace
  `[ValidateAntiForgeryToken]` si falta el token?
- ¿Podrías explicar por qué después de un POST exitoso se redirige (POST → Redirect → GET)?

**Depurador**

- ¿Podrías frenar en un POST y encontrar en **Variables** qué valor llegó en cada campo del ViewModel y qué errores tiene
  `ModelState`?

Si contestaste "no" a más de tres, antes de seguir hacé el repaso de abajo. El desafío 6 **mueve** código que
ya tenés: si ese código no lo entendés del todo, mudarlo va a doler el doble.

## 🧭 Lo que viene

La segunda mitad del vuelo es ordenar la cabina:

| Desafío | Qué cambia | La idea |
|---|---|---|
| 6 · Servicios | las reglas de reserva, pasajeros y vuelos salen del controlador y van a `ReservaService`, `PasajeroService` y `VueloService`; aparecen `ReglaDeNegocioException`, cancelar reservas y editar/eliminar pasajeros | cada capa con **una** responsabilidad: el controlador queda "flaco" |
| 7 · Inyección de dependencias | interfaces para los repositorios, `ConexionDb`, y un interruptor para cambiar SQLite por datos en memoria **tocando solo `Program.cs`** | depender de contratos, no de clases concretas |
| 8 · Excepciones y logs | qué hacer cuando algo explota de verdad, Serilog a archivo y una página de error digna | que el usuario vea un mensaje amable y el programador, el detalle |

Después viene el checkpoint 3, la autenticación (desafío 9) y el **simulacro de parcial**.

## 🔎 Repaso sugerido

| Si te costó… | Releé | Y probá |
|---|---|---|
| el viaje del request, el ruteo, `asp-route-…` | [tema 10](https://taller-de-lenguajes-2.github.io/clases/tema-10-mvc/index.html) y [tema 11](https://taller-de-lenguajes-2.github.io/clases/tema-11-layout-y-vistas/index.html) | breakpoint en `VuelosController.Detalle` y mirá `id` con distintas URLs |
| cuándo usar un ViewModel, overposting, `CargarListas` | [tema 12](https://taller-de-lenguajes-2.github.io/clases/tema-12-viewmodels/index.html), escenarios 1 a 3 | breakpoint en el POST de `Reservas/Crear` y mirá `modelo.Vuelos.Count` |
| atributos, `ModelState`, `AddModelError` | [tema 13](https://taller-de-lenguajes-2.github.io/clases/tema-13-validacion/index.html) | JavaScript desactivado, formulario vacío, y mirá `ModelState.ErrorCount` |
| el SQL de los repositorios (JOIN doble, `GROUP BY`) | [tema 07](https://taller-de-lenguajes-2.github.io/clases/tema-07-bases-de-datos/index.html) y [tema 08](https://taller-de-lenguajes-2.github.io/clases/tema-08-ado-net/index.html) | corré la consulta en DB Browser antes de pasarla a C# |

Un ejercicio de repaso que se parece mucho a un punto de parcial: **sin mirar tu código**, escribí en un archivo aparte
el `ReservaCrearViewModel` completo (con atributos) y el POST de `Reservas/Crear`. Después comparalo con lo que tenés.

:::nota
Abajo está la bitácora: **qué hice / qué me falta / dudas**. Listá tus dudas concretas ("no entiendo por qué el
`select` vuelve vacío", "no sé cuándo va `nameof`") y llevalas a la consulta. Una duda bien escrita ya está a mitad de
camino de resolverse.
:::
