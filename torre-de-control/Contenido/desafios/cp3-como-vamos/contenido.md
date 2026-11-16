## 🛬 ¿Cómo vamos?

Última escala antes del tramo final. Desde el checkpoint anterior, AeroTaller **ordenó la cabina**: las reglas se mudaron
a una capa de servicios (desafío 6), cada pieza depende de contratos y no de clases concretas (desafío 7), y ahora hay
**caja negra**: lo inesperado no le explota al usuario en la cara y todo queda registrado (desafío 8). Así está armado hoy:

```text
 Navegador
    │  POST /Reservas/Crear (VueloId, PasajeroId, Asiento + token)
    ▼
 Pipeline ─────────────── UseSerilogRequestLogging → UseExceptionHandler("/Home/Error") → UseStaticFiles → ruteo
    ▼
 Controllers ──────────── "flacos": reciben el ViewModel, llaman al servicio, eligen la vista
    │                      catch (ReglaDeNegocioException) → ModelState.AddModelError(ex.Campo, ex.Message)
    ▼
 Servicios ────────────── las REGLAS: vuelo que ya salió, asiento libre, overbooking, DNI único…
    │  (IReservaService)   lanzan ReglaDeNegocioException · registran con ILogger<T>
    │                      traducen SqliteException 19 (el choque de asientos) a ReglaDeNegocioException
    ▼
 Interfaces ───────────── IVueloRepository, IReservaRepository…  ← acá se "enchufa" la implementación
    │                                │
    ▼                                ▼
 Repositorios (SQLite)        Repositorios/Falsos (memoria)   ← lo elige Program.cs según OrigenDeDatos
    ▼                                ▼
 aerotaller.db                 BaseFalsa (Singleton)
 (índice único parcial)
```

| Pieza | Qué tenés | Desafío |
|---|---|---|
| Servicios | `ReservaService`, `PasajeroService`, `VueloService` con las reglas; `ReglaDeNegocioException` con el campo | 6 |
| Cancelar y editar | `POST /Reservas/Cancelar/{id}` (cambia el estado), editar y eliminar pasajeros, mensajes con `TempData` | 6 |
| Interfaces | `Repositorios/Interfaces.cs`; controladores y servicios piden `I…Repository` | 7 |
| Contenedor | `ConexionDb` Singleton, repositorios y servicios Scoped, todo registrado en `Program.cs` | 7 |
| Datos falsos | `BaseFalsa` + un `…RepositoryFalso` por interfaz; `OrigenDeDatos=Falso` sin tocar la base | 7 |
| Base | índice único parcial `(VueloId, Asiento) WHERE Estado = 'Confirmada'` | 8 |
| Excepciones | el servicio atrapa solo lo que sabe traducir (`catch … when`); el resto lo agarra `UseExceptionHandler` | 8 |
| Logs | Serilog a consola y `logs/aerotaller-AAAAMMDD.txt`; plantillas, sin datos personales | 8 |

Fijate en la línea de las interfaces: **todo lo de arriba no sabe qué hay abajo**. Si mañana la base fuera otra, se
escribe otra implementación y se cambia `Program.cs`. Esa es la idea más importante de esta mitad del TP.

:::cuidado
Antes de seguir: **commit, push y mirar GitHub**. Abrí tu repo en el navegador y confirmá que ves `Repositorios/Interfaces.cs`,
la carpeta `Repositorios/Falsos/`, `Views/Home/Error.cshtml`, el paquete de Serilog en el `.csproj` y el último commit del
desafío 8. Confirmá también lo que **no** tiene que estar: ni `aerotaller.db` ni la carpeta `logs/`. Lo que no está en GitHub,
para la corrección no existe.
:::

## ✅ Lo que ya sabés hacer

Respondé con honestidad, sin mirar tu código. Lo que te cueste, anotalo en la bitácora para la consulta.
Todo esto **cae en el parcial**.

**Capas y servicios**

- ¿Podrías decir, para una regla nueva («no se reserva a menos de una hora de la salida»), en qué capa va y por qué no en el
  controlador ni en el repositorio?
- ¿Podrías escribir de memoria un controlador "flaco": `ModelState.IsValid` → servicio en un `try` → `RedirectToAction` /
  `catch (ReglaDeNegocioException ex)` → `AddModelError(ex.Campo, ex.Message)` → recargar listas → misma vista?
- ¿Podrías explicar por qué cancelar una reserva es un `UPDATE` del estado y no un `DELETE`, y por qué es un `POST` y no un link?

**Inyección de dependencias**

- ¿Podrías escribir una interfaz a partir de un repositorio que ya existe, y hacer que la clase la implemente?
- ¿Podrías escribir de memoria los registros de `Program.cs`: `AddSingleton(new ConexionDb(cadena))` y
  `AddScoped<IVueloRepository, VueloRepository>()`, y explicar qué significa cada parte?
- ¿Podrías explicar Transient, Scoped y Singleton con un ejemplo de AeroTaller cada uno, y por qué `BaseFalsa` **tiene** que
  ser Singleton?
- ¿Podrías leer `Unable to resolve service for type 'X' while attempting to activate 'Y'` y decir exactamente qué falta y dónde?
- ¿Podrías explicar qué es una dependencia cautiva y por qué la app no arranca en Development?
- ¿Podrías agregar una implementación nueva de una interfaz y usarla **cambiando solo `Program.cs`**?

**Excepciones**

- ¿Podrías decidir, capa por capa, dónde va un `try/catch` y dónde no?
- ¿Podrías escribir un `catch` con filtro `when` y explicar qué pasa cuando la condición da `false`?
- ¿Podrías crear una excepción propia con un dato extra (como `Campo`) y lanzarla envolviendo la original?
- ¿Podrías explicar la diferencia entre `throw;` y `throw ex;`, y por qué un catch vacío es peor que no tener catch?
- ¿Podrías explicar por qué la regla «asiento libre» del servicio no alcanza y hace falta el índice único?

**Logs**

- ¿Podrías configurar Serilog desde cero: paquete, sección en `appsettings.json`, `AddSerilog`, y el orden del pipeline?
- ¿Podrías escribir un log con plantilla (`{ReservaId}`) y explicar qué se pierde con `$"…"`?
- ¿Podrías elegir el nivel justo (`Information`, `Warning`, `Error`) para tres eventos distintos?
- ¿Podrías encontrar en `logs/` el error que vio un usuario, a partir del código que le mostró la página?

**Depurador**

- ¿Podrías usar la **Pila de llamadas** para mostrar quién llama al constructor de un repositorio, y por qué no sos vos?
- ¿Podrías activar «All Exceptions» y encontrar en qué línea nace una excepción que termina en la página de error?

**Lo de antes, que sigue cayendo**

- ¿Podrías escribir un `SELECT` con JOIN doble (origen y destino) y un `LEFT JOIN` + `GROUP BY`?
- ¿Podrías armar un ViewModel de formulario con `int?` + `[Required]` en los combos, y su `CargarListas(modelo)`?

Si contestaste "no" a más de cuatro, antes de seguir hacé el repaso de abajo. Lo que viene (login y simulacro) usa **todo**
esto a la vez.

## 🧭 Lo que viene

| Desafío | Qué cambia | La idea |
|---|---|---|
| 9 · Autenticación | tabla `Usuario`, login con sesión, `[RequiereLogin]` y `[RequiereRol]`, vista `SinPermiso` | autenticación (¿quién sos?) y autorización (¿qué podés hacer?). Es el tema 15: **lo ven esta semana** |
| 10 · Simulacro de parcial | agregar **Equipaje** de punta a punta en **120 minutos**, sin paso a paso | probarte en condiciones de parcial: base, repositorio, servicio, formulario, vistas y logs |

Para el simulacro, un consejo desde ahora: el tiempo se va en lo que no tenés automatizado. Si todavía dudás cómo se escribe
un repositorio con `AddWithValue`, un ViewModel con validaciones o un POST con PRG, **practicalo antes**: en el simulacro no
hay paso a paso.

## 🔎 Repaso sugerido

| Si te costó… | Releé | Y probá |
|---|---|---|
| tiempos de vida, dependencia cautiva | [tema 14](https://taller-de-lenguajes-2.github.io/clases/tema-14-inyeccion-de-dependencias/index.html), diapositivas de Transient, Scoped y Singleton | corré `/TiemposDeVida` del proyecto de ejemplo del tema 14 y recargá |
| interfaces, por qué depender de una abstracción | [tema 04](https://taller-de-lenguajes-2.github.io/clases/tema-04-interfaces-y-clases-abstractas/index.html) y la impresora del tema 14 | arrancá con «AeroTaller (datos falsos)» y frená en un controlador: ¿qué clase recibió? |
| dónde va cada `try/catch`, `when`, `throw;` | [tema 16](https://taller-de-lenguajes-2.github.io/clases/tema-16-excepciones/index.html), «Reglas de oro» | base en solo lectura + «All Exceptions» y seguí la excepción por la pila |
| Serilog, plantillas, qué no loguear | [tema 17](https://taller-de-lenguajes-2.github.io/clases/tema-17-logs/index.html) | hacé una reserva y buscá la línea en `logs/`; buscá un `$"` en tus `Log…` |
| reglas en el servicio, `ReglaDeNegocioException` | el desafío 6 de este TP y la diapositiva de capas del [tema 09](https://taller-de-lenguajes-2.github.io/clases/tema-09-repositorios/index.html) | breakpoint en el controlador y F11 hasta el repositorio |

Un ejercicio de repaso que se parece mucho a un punto de parcial: **sin mirar tu código**, escribí en un archivo aparte
el bloque de registros de `Program.cs` para las dos ramas (`Sqlite` y `Falso`) y `ReservaService.Reservar` con el `try/catch`
del choque. Después comparalo con lo que tenés.

:::nota
Abajo está la bitácora: **qué hice / qué me falta / dudas**. Listá tus dudas concretas ("no entiendo por qué el repositorio
falso puede ser Scoped si la base es Singleton", "no sé si el `LogWarning` del choque lleva la excepción") y llevalas a la
consulta. Antes del simulacro es el mejor momento para sacarse las dudas.
:::
