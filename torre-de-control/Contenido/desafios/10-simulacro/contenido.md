## ✈️ Consigna

**AeroTaller empieza a despachar equipaje.** A partir del lunes, el personal de mostrador registra las valijas de cada
pasajero al hacer el check-in, y la aerolínea quiere controlar dos cosas: que nadie lleve más de lo permitido y cuántos
kilos van en la bodega de cada avión.

Se debe agregar el **equipaje** a AeroTaller **de punta a punta**: base, modelo, repositorio, servicio con las reglas,
formulario, vistas y log. Como en el parcial: con la consigna, sin paso a paso.

**Las reglas del negocio:**

| Tipo | Peso máximo por pieza | Piezas por reserva |
|---|---|---|
| `Mano` | 10 kg | 1 |
| `Bodega` | 23 kg | 2 |

- Solo se agrega equipaje a una reserva **confirmada** (no cancelada) de un vuelo que **todavía no salió**.
- El peso es mayor que cero.
- Cada pieza que se agrega queda registrada en el log.
- Los kilos en bodega de un vuelo cuentan solo las piezas de tipo `Bodega` de sus reservas **confirmadas**.

**Los nombres obligatorios** (están en `CONTRATO.md`; el verificador los busca tal cual):

| Qué | Nombre |
|---|---|
| Tabla | `Equipaje` (`Id`, `ReservaId`, `Tipo`, `PesoKg`) · FK `ReservaId` → `Reserva` · `Tipo` es `'Mano'` o `'Bodega'` |
| Interfaz del repositorio | `IEquipajeRepository` |
| Repositorio falso | una clase con `Falso` en el nombre (`EquipajeRepositoryFalso`) |
| Pantalla | `GET/POST /Equipaje/Crear?reservaId={id}` con los campos `Tipo` y `PesoKg` |

Todo lo demás (nombres de métodos, de ViewModels, la resolución interna) queda a criterio de quien resuelve, pero se
espera que respete la arquitectura del TP: SQL solo en repositorios, reglas solo en servicios, controladores flacos,
ViewModels para las pantallas.

## 📋 Requisitos

### R1 · La base

- La tabla `Equipaje` en `aerotaller.sql`, con su clave foránea y `CHECK` para `Tipo` (`'Mano'`, `'Bodega'`) y `PesoKg` (> 0).
- Los datos de `material/datos-equipaje.sql` al final del script.
- `aerotaller.db` recreada (y que todo lo anterior siga funcionando).

:::pista El orden de los DROP
Con `PRAGMA foreign_keys = ON`, borrar `Reserva` antes que `Equipaje` puede fallar (hay filas que la referencian).
El `DROP TABLE IF EXISTS Equipaje;` va **primero**, y el `CREATE TABLE Equipaje` **después** del de `Reserva`.
:::

:::pista Un paso que suele olvidarse
Cambiar el script no cambia la base: `CrearBaseSiNoExiste` solo lo ejecuta si el archivo no existe. App detenida, borrar
`aerotaller.db`, volver a iniciar.
:::

### R2 · Modelo y repositorio

- Un `enum TipoEquipaje { Mano, Bodega }` y una clase `Equipaje` (`Id`, `ReservaId`, `Tipo`, `PesoKg` decimal).
- `IEquipajeRepository` con lo que sea necesario: como mínimo, listar las piezas de una reserva, insertar una pieza
  (devolviendo el Id) y calcular los kilos en bodega de un vuelo.
- `EquipajeRepository` con ADO.NET, **siempre con parámetros**.
- `EquipajeRepositoryFalso` sobre la `BaseFalsa`, y los registros en **las dos ramas** de `Program.cs`.

:::pista El enum y el texto de la base
Es el mismo problema que `EstadoReserva` en el desafío 2: en la base hay texto (`'Bodega'`), en C# un enum. Para guardar,
`.ToString()`; para leer, `Enum.Parse<TipoEquipaje>(...)`.
:::

:::pista ¿Dónde guarda el falso?
La `BaseFalsa` necesita una lista nueva (`public List<Equipaje> Equipajes { get; } = [];`). Si se omite el registro en la
rama `Falso`, en Development la app no arranca con datos falsos: conviene probarlo antes de entregar.
:::

### R3 · Las reglas en el servicio

- Un servicio de equipaje (por ejemplo `IEquipajeService` / `EquipajeService`) que recibe la reserva, el tipo y el peso,
  aplica **todas** las reglas de la consigna y, si alguna no se cumple, lanza `ReglaDeNegocioException` con el campo que corresponde:

| Regla rota | Campo del error |
|---|---|
| la reserva no existe, está cancelada o el vuelo ya salió | `""` (error general del formulario) |
| el peso supera el máximo de su tipo | `PesoKg` |
| ya tiene las piezas permitidas de ese tipo | `Tipo` |

- Si todo está bien, inserta la pieza y la registra en el log con una **plantilla** (sin `$`, sin datos personales).
- El controlador **no** decide ninguna de estas reglas.

:::pista ¿Cómo saber si el vuelo ya salió?
El servicio puede recibir también `IReservaRepository`: `Obtener(reservaId)` devuelve la reserva con su `Vuelo`, y el
vuelo ya sabe si `YaSalio`. Para contar piezas, listar las de la reserva y contarlas por tipo (`Count(p => p.Tipo == …)`).
:::

:::pista Los números mágicos
`23`, `10` y `2` escritos sueltos en tres lugares son tres lugares para equivocarse. Unas constantes en la clase `Equipaje`
(`PesoMaximoBodega`…) se pueden usar en el servicio **y** en la vista, para mostrar las reglas junto al formulario.
:::

### R4 · El formulario

- `GET /Equipaje/Crear?reservaId={id}`: el formulario, con los datos de la reserva arriba (pasajero, vuelo). Si la reserva
  no existe, 404.
- `POST /Equipaje/Crear`: con un ViewModel propio (`Tipo` y `PesoKg` obligatorios, peso con rango razonable), `[ValidateAntiForgeryToken]`,
  `ModelState`, el servicio en un `try`, y **PRG**: si la operación es exitosa, redirige al detalle de la reserva; si falla,
  la misma vista con el error junto al campo.
- Solo con sesión iniciada (`[RequiereLogin]`).

:::pista El combo del tipo
`Html.GetEnumSelectList<TipoEquipaje>()` arma las opciones de un `<select>` a partir del enum. Para que «sin selección»
llegue como `null` y lo detecte `[Required]`, la propiedad del ViewModel es `TipoEquipaje?`, igual que los `int?` de los combos.
:::

:::pista El reservaId debe volver en el POST
Viene en el query string del GET, pero el POST es otro request: guardarlo en un `<input asp-for="ReservaId" type="hidden" />`.
Y si el POST falla, los datos de la reserva que se mostraban arriba también deben volver a cargarse (como `CargarListas`).
:::

### R5 · El detalle de la reserva

- `GET /Reservas/Detalle/{id}`: los datos de la reserva (vuelo, pasajero, asiento, estado), la lista de piezas
  (tipo y peso) y el **total de kilos**. Si esta pantalla todavía no existe, crearla; si la reserva no existe, 404.
- Un enlace «Agregar equipaje» que lleve a `/Equipaje/Crear?reservaId={id}` (solo si la reserva admite equipaje).
- Desde el listado de `/Reservas`, cada reserva lleva a su detalle.

:::pista El total
No hace falta otra consulta: si el ViewModel recibe la lista de piezas, el total es un `Sum(p => p.PesoKg)` en el constructor.
:::

### R6 · Kilos en bodega por vuelo

- En `/Vuelos/Detalle/{id}`, una línea más: cuántos kilos despachados en bodega lleva ese vuelo.
- Calculado **en la base**, con una sola consulta: `SUM` + `JOIN` con `Reserva` + `COALESCE`.

:::pista La consulta
Partir de esto y completar el `WHERE` (solo ese vuelo, solo reservas confirmadas, solo bodega):

```sql
SELECT COALESCE(SUM(e.PesoKg), 0)
FROM Equipaje e
INNER JOIN Reserva r ON e.ReservaId = r.Id
WHERE …
```

`SUM` de cero filas da `NULL`, no `0`: `COALESCE` devuelve el primer valor que no sea `NULL`. Con `ExecuteScalar` y
`Convert.ToDecimal` se obtiene el valor en C#.
:::

:::nota
Los decimales van con **punto** (`18.5`): un `<input type="number">` siempre envía punto, y por eso el `Program.cs` del
proyecto base configura la cultura `es-AR` con punto decimal para los números. Si en `Program.cs` esas líneas se
reemplazaron por `new CultureInfo("es-AR")` sin más, el servidor interpreta `18.5` como 185.
:::

## ⏱️ Reglas del simulacro

**El tiempo disponible es de 2 horas y 30 minutos (150 minutos).** El reloj de la Torre arrancó la primera vez que se abrió esta página. Si todas
las tareas se marcan dentro del tiempo, además de «Aterrizaje perfecto» se obtiene la insignia **A horario**.

Para que sirva como entrenamiento, se recomienda resolverlo **en condiciones de parcial**:

- En forma individual: sin compañeros y sin asistentes de IA.
- Se puede usar el código propio del TP, las diapositivas de la cátedra y la documentación oficial.
- Las pistas están plegadas: abrir una no tiene costo aquí, pero conviene anotar cuáles se abrieron. En el parcial no hay
  pistas, y esa lista indica exactamente qué temas repasar.

**Un plan de vuelo sugerido** (el orden importa: cada paso se apoya en el anterior):

| Minutos | Qué | Commit obligatorio |
|---|---|---|
| 0–10 | leer **toda** la consigna; revisar `material/datos-equipaje.sql` | — |
| 10–30 | R1 · base | minuto 20 |
| 30–60 | R2 · modelo, repositorio, falso, registros (compila) | minutos 40 y 60 |
| 60–90 | R3 · servicio con las reglas y el log | minuto 80 |
| 90–120 | R4 · controlador, ViewModel y vista; probados 25 kg y una pieza válida | minutos 100 y 120 |
| 120–140 | R5 y R6 · detalle de la reserva y kilos en bodega | minuto 140 |
| 140–150 | verificador, push final, revisar GitHub | minuto 150 |

Tres reglas de supervivencia:

1. **Compilar seguido** (cada 10–15 minutos). Un error de compilación a 5 minutos del final es el peor escenario posible.
2. **Commit y push cada 20 minutos**, como mínimo (además de uno por requisito terminado). La Torre muestra una cuenta
   regresiva y un aviso cada 20 minutos. Si se termina el tiempo, lo que está en GitHub es lo que cuenta, y el historial
   de commits muestra cómo se avanzó.
3. **No dedicar más de 15 minutos** a un solo problema: dejarlo, seguir con el requisito siguiente y retomarlo al final.

Al cumplirse los 150 minutos: commit y push de lo que haya, y marcar con honestidad las tareas que **sí** están resueltas.
Después, terminar el resto sin reloj: el simulacro sirve para aprender, no solo para medir.

## 🧪 Cómo se corrige

El simulacro **no lleva puntaje**. El resultado es el **porcentaje de completitud**: la proporción de tareas de la
checklist cumplidas, que la Torre muestra junto al cronómetro. Cada requisito equivale a una tarea.

| Requisito | Qué se evalúa |
|---|---|
| R1 · Base | la tabla con FK y `CHECK`; los datos de ejemplo cargados; la base se crea automáticamente |
| R2 · Modelo y repositorio | enum ↔ texto; parámetros en **todo** el SQL; `IEquipajeRepository`; el falso y los dos registros |
| R3 · Servicio | las cuatro reglas, cada error en su campo; nada de reglas en el controlador; log con plantilla |
| R4 · Formulario | ViewModel con validaciones; antiforgery; PRG; error junto al campo; `[RequiereLogin]`; 404 si la reserva no existe |
| R5 · Detalle de la reserva | piezas y total; enlace para agregar; 404 si no existe |
| R6 · Kilos en bodega | una sola consulta con `SUM` + `JOIN` + `COALESCE`; solo bodega y solo confirmadas; 0 si no hay equipaje |

**La parte automática** (el verificador, `dotnet run --project verificador -- 10`) prueba lo que se puede probar desde
afuera, con su propia base:

- existe la tabla `Equipaje` con las columnas del contrato;
- una pieza de bodega de **25 kg** → error en el formulario y nada nuevo en la base;
- una pieza válida → redirect y una fila nueva en `Equipaje`;
- una **tercera** pieza de bodega en la misma reserva → error;
- el detalle de la reserva muestra el total de kilos.

**La parte que se revisa en el código** (lo que el verificador no ve): que cada cosa esté en su capa, que no haya SQL
armado con `$"…"`, que no haya `catch` vacíos, que el log use plantillas y no registre datos personales, y que la app siga
funcionando con `OrigenDeDatos=Falso`.

Lo que más resta en un parcial, y conviene revisar antes de entregar:

- **No compila**: no se puede probar nada. Es preferible entregar menos, pero que compile.
- **Reglas en el controlador** «porque era más rápido»: funciona, pero es justamente lo que se está evaluando.
- **SQL con interpolación** (`$"… WHERE ReservaId = {id}"`): inyección SQL, aunque funcione.
- **Sin PRG**: después de agregar una pieza, F5 en el navegador la agrega de nuevo.
- **Formulario sin `[ValidateAntiForgeryToken]`** o pantalla sin `[RequiereLogin]`.

Al terminar, se recomienda comparar la solución con la de un compañero (una vez que ambos hayan terminado) y llevar a la
consulta lo que se resolvió de forma distinta: casi siempre hay más de una forma correcta, y discutirlas es la mejor
preparación para el parcial.
