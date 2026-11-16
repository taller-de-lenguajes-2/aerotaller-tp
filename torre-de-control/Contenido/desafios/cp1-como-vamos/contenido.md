## 🛬 ¿Cómo vamos?

Primera escala. Antes de seguir, frená cinco minutos y mirá lo que ya está construido. Hasta acá armaste **la mitad de
abajo** de la aplicación: la que guarda y trae los datos. Todavía no hay ni una pantalla nueva, pero todo lo que viene
se apoya en esto.

| Capa | Qué hay | Estado |
|---|---|---|
| Vistas (`Views/`) | layout y página de inicio de la plantilla | ⏳ desafío 3 |
| Controladores (`Controllers/`) | solo `HomeController` de la plantilla | ⏳ desafío 3 |
| ViewModels (`ViewModels/`) | — | ⏳ desafío 4 |
| Servicios (`Servicios/`) | — | ⏳ desafío 6 |
| **Repositorios** (`Repositorios/`) | `AeropuertoRepository`, `VueloRepository`, `PasajeroRepository`, `ReservaRepository` | ✅ desafío 2 |
| **Modelo** (`Models/Modelos.cs`) | `Aeropuerto`, `Vuelo`, `Pasajero`, `EstadoReserva`, `Reserva`, `OcupacionVuelo` | ✅ desafío 2 |
| **Base** (`aerotaller.sql` → `aerotaller.db`) | 4 tablas con claves foráneas, `UNIQUE`, `CHECK` y datos | ✅ desafío 1 |

Y el camino que ya recorre un dato, de abajo hacia arriba:

| Paso | Dónde | Qué pasa |
|---|---|---|
| 1 | `Program.cs` | lee `ConnectionStrings:AeroTaller` y, si no hay base, ejecuta `aerotaller.sql` |
| 2 | `VueloRepository.Listar()` | abre una conexión, ejecuta el `SELECT` con el JOIN doble |
| 3 | `LeerVuelo(lector, 0)` | cada fila se convierte en un `Vuelo` con su `Origen` y su `Destino` |
| 4 | quien llamó | recibe una `List<Vuelo>`: nunca vio una conexión ni una línea de SQL |

En el desafío 3, "quien llamó" va a ser un controlador.

:::cuidado
Antes de seguir, el ritual: **commit, push y mirar GitHub**. Entrá a tu repo en el navegador y confirmá que están
`aerotaller.sql`, `Models/` y `Repositorios/`, y que `Program.cs` **no** tiene el código de prueba del desafío 2.
Y que `aerotaller.db` **no** está (la base no se sube: se genera).
:::

## ✅ Lo que ya sabés hacer

Respondete con sinceridad, sin mirar el código. Todo esto cae en el parcial:

- ¿Podrías escribir un `CREATE TABLE` con clave primaria autoincremental, una clave foránea, un `UNIQUE` y un campo opcional?
- ¿Podrías explicar por qué el script borra las tablas **de las hijas a las madres**?
- ¿Podrías escribir un JOIN entre dos tablas, y uno con la **misma** tabla dos veces (alias `o` y `d`)?
- ¿Podrías escribir un `LEFT JOIN` con `GROUP BY` que cuente también los ceros, y explicar por qué la condición va en el `ON`?
- ¿Sabés por qué las fechas van como `'AAAA-MM-DD HH:MM'` y qué pasa si las guardás con otro formato?
- ¿Podrías explicar de dónde sale `aerotaller.db` cuando arrancás la app, y qué hacer si cambiaste el script?
- ¿Podrías escribir de memoria un método de repositorio con `using var conexion`, un parámetro y un `while (lector.Read())`?
- ¿Sabés cuándo usar `ExecuteReader`, `ExecuteNonQuery` y `ExecuteScalar`, y qué devuelve cada uno?
- ¿Sabés leer una columna que puede ser `NULL` y mandar un `null` de C# como parámetro?
- ¿Podrías explicar, con un ejemplo, por qué `$"… WHERE Dni = '{dni}'"` es peligroso y `AddWithValue("@texto", $"%{texto}%")` no?
- ¿Podrías obtener el Id de una fila recién insertada?
- ¿Sabés pasar un `enum` a texto para guardarlo y de texto a `enum` al leerlo?
- ¿Podrías decir qué **no** va en un repositorio?
- ¿Sabés poner un breakpoint, mirar una lista en Variables y seguir una llamada con el Call Stack?

Si dudaste en más de dos, mirá el repaso sugerido de abajo antes de seguir: el desafío 3 da todo esto por sabido.

## 🧭 Lo que viene

La segunda mitad del vuelo: **lo que ve el usuario**.

| Desafío | Qué construís | Temas |
|---|---|---|
| 3 · Controladores y vistas | `/Vuelos`, `/Vuelos/Detalle/{id}`, `/Pasajeros` con buscador, el detalle de cada pasajero y el tablero de ocupación en la página de inicio. Los repositorios se registran en `Program.cs` y los controladores los reciben por el constructor | 10 · 11 |
| 4 · ViewModels | el **mapa de asientos**, el listado de reservas y el formulario para reservar con combos de vuelos y pasajeros | 12 |
| 5 · Validación | que nadie reserve el asiento 99Z, un vuelo que ya salió o un DNI repetido | 13 |

Después del desafío 5 hay otra escala como esta.

## 🔎 Repaso sugerido

| Si te costó… | Releé | Y practicá |
|---|---|---|
| claves foráneas, `UNIQUE`, `NULL` | tema 07: "Clave foránea", "Integridad referencial", "Campos vacíos: NULL" | borrar en DB Browser un pasajero con reservas y leer el error |
| `JOIN`, alias, `GROUP BY` | tema 07: "Unir tablas: JOIN" y "Contar y agrupar" | las consultas 3 y 4 del desafío 1, sin mirar la solución |
| `LEFT JOIN` | la consulta 4 del desafío 1 y la lectura de W3Schools | cambiar el `AND r.Estado …` del `ON` al `WHERE` y ver qué vuelos desaparecen |
| la receta de ADO.NET | tema 08: "Todo junto: listar pacientes" y "La receta" | escribir `AeropuertoRepository.Listar()` de memoria en un archivo aparte |
| parámetros e inyección | tema 08: "Nunca concatenar: inyección SQL" | la prueba `Buscar("' OR '1'='1")` del desafío 2 |
| qué va en un repositorio | tema 09: "Qué va en un repositorio y qué no" | revisar que tus repositorios no tengan reglas del negocio ni `Console.WriteLine` |
| composición y agregación | tema 02: "La diferencia clave: el ciclo de vida" | decidir qué relación es `Reserva` → `Pasajero`, y justificarlo |
| el depurador | desafío 0, paso 5 | el breakpoint de `LeerVuelo` del desafío 2, siguiendo el Call Stack |

:::nota
En la **bitácora** de este checkpoint anotá qué hiciste, qué te falta y, sobre todo, **listá tus dudas para la consulta**. Una duda
escrita hoy es una pregunta concreta en la consulta y un error menos en el parcial.
:::
