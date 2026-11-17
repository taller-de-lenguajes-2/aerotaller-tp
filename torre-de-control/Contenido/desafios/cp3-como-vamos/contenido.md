## :plane-landing: ¿Cómo vamos?

Última escala antes del tramo final. Desde el checkpoint anterior, AeroTaller **ordenó la cabina**: las reglas se trasladaron
a una capa de servicios (desafío 6), cada pieza depende de contratos y no de clases concretas (desafío 7), y ahora hay
**caja negra**: lo inesperado no llega sin control al usuario y todo queda registrado (desafío 8). La organización actual es la siguiente:

```text
 Navegador
    │  POST /Reservas/Crear (VueloId, PasajeroId, Asiento + token)
    ▼
 Pipeline ─────────────── UseSerilogRequestLogging → UseExceptionHandler("/Home/Error") → UseStaticFiles → ruteo
    ▼
 Controllers ──────────── "delgados": reciben el ViewModel, llaman al servicio, seleccionan la vista
    │                      catch (ReglaDeNegocioException) → ModelState.AddModelError(ex.Campo, ex.Message)
    ▼
 Servicios ────────────── las REGLAS: vuelo que ya salió, asiento libre, overbooking, DNI único…
    │  (IReservaService)   lanzan ReglaDeNegocioException · registran con ILogger<T>
    │                      traducen SqliteException 19 (el choque de asientos) a ReglaDeNegocioException
    ▼
 Interfaces ───────────── IVueloRepository, IReservaRepository…  ← aquí se conecta la implementación
    │                                │
    ▼                                ▼
 Repositorios (SQLite)        Repositorios/Falsos (memoria)   ← lo selecciona Program.cs según OrigenDeDatos
    ▼                                ▼
 aerotaller.db                 BaseFalsa (Singleton)
 (índice único parcial)
```

| Pieza | Qué debe estar implementado | Desafío |
|---|---|---|
| Servicios | `ReservaService`, `PasajeroService`, `VueloService` con las reglas; `ReglaDeNegocioException` con el campo | 6 |
| Cancelar y editar | `POST /Reservas/Cancelar/{id}` (cambia el estado), editar y eliminar pasajeros, mensajes con `TempData` | 6 |
| Interfaces | `Repositorios/Interfaces.cs`; controladores y servicios declaran `I…Repository` como dependencia | 7 |
| Contenedor | `ConexionDb` Singleton, repositorios y servicios Scoped, todo registrado en `Program.cs` | 7 |
| Datos falsos | `BaseFalsa` + un `…RepositoryFalso` por interfaz; `OrigenDeDatos=Falso` sin acceder a la base | 7 |
| Base | índice único parcial `(VueloId, Asiento) WHERE Estado = 'Confirmada'` | 8 |
| Excepciones | el servicio atrapa solo las excepciones que traduce (`catch … when`); el resto las atrapa `UseExceptionHandler` | 8 |
| Logs | Serilog a consola y `logs/aerotaller-AAAAMMDD.txt`; plantillas, sin datos personales | 8 |

Notar la línea de las interfaces: **las capas superiores no dependen de la implementación concreta de las inferiores**. Si
la base de datos cambiara, bastaría con escribir otra implementación y modificar `Program.cs`. Esa es la idea más importante de esta mitad del TP.

:::cuidado
Antes de seguir: **commit, push y verificación en GitHub**. Abrir el repositorio en el navegador y confirmar que están
`Repositorios/Interfaces.cs`, la carpeta `Repositorios/Falsos/`, `Views/Home/Error.cshtml`, el paquete de Serilog en el `.csproj`
y el último commit del desafío 8. Confirmar también lo que **no** debe estar: ni `aerotaller.db` ni la carpeta `logs/`. Solo se
corrige lo que está en GitHub.
:::

## :circle-check: Lo que ya se sabe hacer

Responder con honestidad, sin consultar el código. Anotar en la bitácora los puntos que resulten difíciles, para la consulta.
Todo esto **se evalúa en el parcial**.

**Capas y servicios**

- ¿Se puede indicar, para una regla nueva («no se reserva a menos de una hora de la salida»), en qué capa va y por qué no en el
  controlador ni en el repositorio?
- ¿Se puede escribir de memoria un controlador "delgado": `ModelState.IsValid` → servicio en un `try` → `RedirectToAction` /
  `catch (ReglaDeNegocioException ex)` → `AddModelError(ex.Campo, ex.Message)` → recargar listas → misma vista?
- ¿Se puede explicar por qué cancelar una reserva es un `UPDATE` del estado y no un `DELETE`, y por qué es un `POST` y no un enlace?

**Inyección de dependencias**

- ¿Se puede escribir una interfaz a partir de un repositorio existente, y hacer que la clase la implemente?
- ¿Se pueden escribir de memoria los registros de `Program.cs`: `AddSingleton(new ConexionDb(cadena))` y
  `AddScoped<IVueloRepository, VueloRepository>()`, y explicar qué significa cada parte?
- ¿Se puede explicar Transient, Scoped y Singleton con un ejemplo de AeroTaller cada uno, y por qué `BaseFalsa` **debe**
  ser Singleton?
- ¿Se puede leer `Unable to resolve service for type 'X' while attempting to activate 'Y'` e indicar exactamente qué falta y dónde?
- ¿Se puede explicar qué es una dependencia cautiva y por qué la aplicación no se inicia en Development?
- ¿Se puede agregar una implementación nueva de una interfaz y usarla **cambiando solo `Program.cs`**?

**Excepciones**

- ¿Se puede decidir, capa por capa, dónde va un `try/catch` y dónde no?
- ¿Se puede escribir un `catch` con filtro `when` y explicar qué ocurre cuando la condición da `false`?
- ¿Se puede crear una excepción propia con un dato extra (como `Campo`) y lanzarla envolviendo la original?
- ¿Se puede explicar la diferencia entre `throw;` y `throw ex;`, y por qué un catch vacío es peor que no tener catch?
- ¿Se puede explicar por qué la regla «asiento libre» del servicio no es suficiente y se requiere el índice único?

**Logs**

- ¿Se puede configurar Serilog desde cero: paquete, sección en `appsettings.json`, `AddSerilog`, y el orden del pipeline?
- ¿Se puede escribir un log con plantilla (`{ReservaId}`) y explicar qué se pierde con `$"…"`?
- ¿Se puede elegir el nivel adecuado (`Information`, `Warning`, `Error`) para tres eventos distintos?
- ¿Se puede encontrar en `logs/` el error que vio un usuario, a partir del código que le mostró la página?

**Depurador**

- ¿Se puede usar la **Pila de llamadas** para mostrar qué código invoca al constructor de un repositorio, y por qué no es código propio?
- ¿Se puede activar «All Exceptions» y encontrar en qué línea se origina una excepción que termina en la página de error?

**Contenidos anteriores, que siguen siendo evaluados**

- ¿Se puede escribir un `SELECT` con JOIN doble (origen y destino) y un `LEFT JOIN` + `GROUP BY`?
- ¿Se puede construir un ViewModel de formulario con `int?` + `[Required]` en los combos, y su `CargarListas(modelo)`?

Si la respuesta fue "no" en más de cuatro preguntas, conviene hacer el repaso de abajo antes de seguir. Los desafíos siguientes
(login y simulacro) integran **todos** estos contenidos.

## :compass: Lo que viene

| Desafío | Qué cambia | La idea |
|---|---|---|
| 9 · Autenticación | tabla `Usuario`, login con sesión, `[RequiereLogin]` y `[RequiereRol]`, vista `SinPermiso` | autenticación (¿quién es el usuario?) y autorización (¿qué puede hacer?). Es el tema 15: **se desarrolla en clase esta semana** |
| 10 · Simulacro de parcial | agregar **Equipaje** en todas las capas en **120 minutos**, sin paso a paso | práctica en condiciones de parcial: base, repositorio, servicio, formulario, vistas y logs |

Para el simulacro, una recomendación anticipada: el tiempo se consume en lo que todavía no se domina. Si todavía hay dudas
sobre cómo se escribe un repositorio con `AddWithValue`, un ViewModel con validaciones o un POST con PRG, **conviene practicarlo
antes**: en el simulacro no hay paso a paso.

## :search: Repaso sugerido

| Si resultó difícil… | Releer | Y probar |
|---|---|---|
| tiempos de vida, dependencia cautiva | [tema 14](https://taller-de-lenguajes-2.github.io/clases/tema-14-inyeccion-de-dependencias/index.html), diapositivas de Transient, Scoped y Singleton | ejecutar `/TiemposDeVida` del proyecto de ejemplo del tema 14 y recargar |
| interfaces, por qué depender de una abstracción | [tema 04](https://taller-de-lenguajes-2.github.io/clases/tema-04-interfaces-y-clases-abstractas/index.html) y la impresora del tema 14 | arrancar con «AeroTaller (datos falsos)» y detenerse en un controlador: ¿qué clase recibió? |
| dónde va cada `try/catch`, `when`, `throw;` | [tema 16](https://taller-de-lenguajes-2.github.io/clases/tema-16-excepciones/index.html), «Reglas de oro» | base en solo lectura + «All Exceptions», y seguir la excepción por la pila |
| Serilog, plantillas, qué no registrar | [tema 17](https://taller-de-lenguajes-2.github.io/clases/tema-17-logs/index.html) | hacer una reserva y buscar la línea en `logs/`; buscar un `$"` en las llamadas `Log…` del proyecto |
| reglas en el servicio, `ReglaDeNegocioException` | el desafío 6 de este TP y la diapositiva de capas del [tema 09](https://taller-de-lenguajes-2.github.io/clases/tema-09-repositorios/index.html) | breakpoint en el controlador y F11 hasta el repositorio |

Un ejercicio de repaso muy similar a un punto de parcial: **sin consultar el código**, escribir en un archivo aparte
el bloque de registros de `Program.cs` para las dos ramas (`Sqlite` y `Falso`) y `ReservaService.Reservar` con el `try/catch`
del choque. Después, compararlo con lo implementado.

:::nota
Abajo está la bitácora: **qué se hizo / qué falta / dudas**. Conviene registrar dudas concretas ("por qué el repositorio
falso puede ser Scoped si la base es Singleton", "si el `LogWarning` del choque lleva la excepción") y llevarlas a la
consulta. Antes del simulacro es el mejor momento para resolverlas.
:::
