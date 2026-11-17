## :plane-landing: ¿Cómo vamos?

Primera escala. Antes de seguir, conviene detenerse cinco minutos y revisar lo que ya está construido. Hasta aquí se construyó **la mitad
inferior** de la aplicación: la que almacena y recupera los datos. Todavía no hay pantallas nuevas, pero los desafíos siguientes
se apoyan en esta base.

| Capa | Qué hay | Estado |
|---|---|---|
| Vistas (`Views/`) | layout y página de inicio de la plantilla | :hourglass: desafío 3 |
| Controladores (`Controllers/`) | solo `HomeController` de la plantilla | :hourglass: desafío 3 |
| ViewModels (`ViewModels/`) | — | :hourglass: desafío 4 |
| Servicios (`Servicios/`) | — | :hourglass: desafío 6 |
| **Repositorios** (`Repositorios/`) | `AeropuertoRepository`, `VueloRepository`, `PasajeroRepository`, `ReservaRepository` | :circle-check: desafío 2 |
| **Modelo** (`Models/Modelos.cs`) | `Aeropuerto`, `Vuelo`, `Pasajero`, `EstadoReserva`, `Reserva`, `OcupacionVuelo` | :circle-check: desafío 2 |
| **Base** (`aerotaller.sql` → `aerotaller.db`) | el script de la plantilla: 5 tablas con claves foráneas, `UNIQUE`, `CHECK`, un índice parcial y datos; la aplicación la genera si no existe | :circle-check: desafío 1 |

El recorrido actual de un dato, de abajo hacia arriba:

| Paso | Dónde | Qué ocurre |
|---|---|---|
| 1 | `Program.cs` | lee `ConnectionStrings:AeroTaller` y, si no hay base, ejecuta `aerotaller.sql` |
| 2 | `VueloRepository.Listar()` | abre una conexión, ejecuta el `SELECT` con el JOIN doble |
| 3 | `LeerVuelo(lector, 0)` | cada fila se convierte en un `Vuelo` con su `Origen` y su `Destino` |
| 4 | método invocante | recibe una `List<Vuelo>`, sin acceso a conexiones ni a SQL |

En el desafío 3, el método invocante pertenece a un controlador.

:::cuidado
Antes de seguir, el procedimiento habitual: **commit, push y verificación en GitHub**. Ingresar al repositorio en el navegador y confirmar que están
`Models/` y `Repositorios/`, y que `Program.cs` **no** tiene el código de prueba del desafío 2.
Confirmar también que `aerotaller.db` **no** está (la base no se sube: se genera).
:::

## :circle-check: Lo que ya se sabe hacer

Responder con sinceridad, sin consultar el código. Todo esto se evalúa en el parcial:

- ¿Es posible escribir un `CREATE TABLE` con clave primaria autoincremental, una clave foránea, un `UNIQUE` y un campo opcional?
- ¿Es posible explicar por qué el script borra las tablas **de las hijas a las madres**?
- ¿Es posible escribir un JOIN entre dos tablas, y uno con la **misma** tabla dos veces (alias `o` y `d`)?
- ¿Es posible escribir un `LEFT JOIN` con `GROUP BY` que cuente también los ceros, y explicar por qué la condición va en el `ON`?
- ¿Por qué las fechas van como `'AAAA-MM-DD HH:MM'` y qué ocurre si se guardan con otro formato?
- ¿Cómo se genera `aerotaller.db` al iniciar la aplicación, y qué hacer si se modificó el script?
- ¿Es posible escribir de memoria un método de repositorio con `using var conexion`, un parámetro y un `while (lector.Read())`?
- ¿Cuándo usar `ExecuteReader`, `ExecuteNonQuery` y `ExecuteScalar`, y qué devuelve cada uno?
- ¿Cómo leer una columna que puede ser `NULL` y enviar un `null` de C# como parámetro?
- ¿Es posible explicar, con un ejemplo, por qué `$"… WHERE Dni = '{dni}'"` es peligroso y `AddWithValue("@texto", $"%{texto}%")` no?
- ¿Cómo se obtiene el Id de una fila recién insertada?
- ¿Cómo se convierte un `enum` a texto para guardarlo y de texto a `enum` al leerlo?
- ¿Qué **no** va en un repositorio?
- ¿Es posible colocar un breakpoint, inspeccionar una lista en Variables y seguir una llamada con el Call Stack?

Si hubo dudas en más de dos, revisar el repaso sugerido más abajo antes de seguir: el desafío 3 presupone estos conocimientos.

## :compass: Lo que viene

La segunda mitad del vuelo: **lo que ve el usuario**.

| Desafío | Qué se construye | Temas |
|---|---|---|
| 3 · Controladores y vistas | `/Vuelos`, `/Vuelos/Detalle/{id}`, `/Pasajeros` con buscador, el detalle de cada pasajero y el tablero de ocupación en la página de inicio. Los repositorios se registran en `Program.cs` y los controladores los reciben por el constructor | 10 · 11 |
| 4 · ViewModels | el **mapa de asientos**, el listado de reservas y el formulario para reservar con combos de vuelos y pasajeros | 12 |
| 5 · Validación | el rechazo del asiento 99Z, de un vuelo que ya partió y de un DNI repetido | 13 |

Después del desafío 5 hay otra escala como esta.

## :search: Repaso sugerido

| Si resultó difícil… | Releer | Y practicar |
|---|---|---|
| claves foráneas, `UNIQUE`, `NULL` | tema 07: "Clave foránea", "Integridad referencial", "Campos vacíos: NULL" | borrar en DB Browser un pasajero con reservas y leer el error |
| `JOIN`, alias, `GROUP BY` | tema 07: "Unir tablas: JOIN" y "Contar y agrupar" | las consultas 3 y 4 del desafío 1, sin consultar la solución |
| `LEFT JOIN` | la consulta 4 del desafío 1 y la lectura de W3Schools | cambiar el `AND r.Estado …` del `ON` al `WHERE` y ver qué vuelos desaparecen |
| la receta de ADO.NET | tema 08: "Todo junto: listar pacientes" y "La receta" | escribir `AeropuertoRepository.Listar()` de memoria en un archivo aparte |
| parámetros e inyección | tema 08: "Nunca concatenar: inyección SQL" | la prueba `Buscar("' OR '1'='1")` del desafío 2 |
| qué va en un repositorio | tema 09: "Qué va en un repositorio y qué no" | revisar que los repositorios propios no contengan reglas del negocio ni `Console.WriteLine` |
| composición y agregación | tema 02: "La diferencia clave: el ciclo de vida" | decidir qué relación es `Reserva` → `Pasajero`, y justificarlo |
| el depurador | desafío 0, paso 5 | el breakpoint de `LeerVuelo` del desafío 2, siguiendo el Call Stack |

:::nota
En la **bitácora** de este checkpoint, anotar qué se hizo, qué falta y, sobre todo, **listar las dudas para la consulta**. Registrar
las dudas por escrito permite plantear preguntas concretas en la consulta y prevenir errores en el parcial.
:::
