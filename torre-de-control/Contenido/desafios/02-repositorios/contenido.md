## :target: Misión

La base ya existe, pero todavía no hay código C# que acceda a ella. En este desafío se construyen las dos piezas que conectan el mundo de las
**filas** con el mundo de los **objetos**:

1. **El modelo** (`Models/Modelos.cs`): las clases de AeroTaller (`Vuelo`, `Pasajero`, `Reserva`…), que no dependen
   de SQL ni de HTML.
2. **Los repositorios** (`Repositorios/`): una clase por tabla con **todo** el SQL, siempre con parámetros. Hacia afuera
   exponen `Listar()` u `Obtener(id)`; internamente quedan las conexiones, los comandos y los lectores.

Además, se prueban **sin pantallas**, mediante código temporal en `Program.cs` y un breakpoint para inspeccionar los objetos
que devuelven. En el desafío 3 los controladores los usan sin modificaciones.

:::nota
Es el desafío más largo del TP, y el que más se parece al parcial: en casi todos los parciales hay una tabla nueva con su
repositorio. Es razonable que requiera dos sesiones. Conviene hacer un commit al terminar cada repositorio.
:::

## :book-open-text: Repaso

Cuatro temas confluyen en este desafío:

| Tema | La idea | Dónde aparece en este desafío |
|---|---|---|
| 01 · Abstracción | de la realidad se conserva lo que necesita **el** sistema | `Vuelo` tiene `Filas`, no la marca del avión |
| 01 · Ocultamiento | los detalles internos se declaran `private` | `LeerAeropuerto(lector)` no es accesible desde afuera |
| 02 · Asociación | una clase tiene como miembro a otra | `Reserva` tiene un `Vuelo` y un `Pasajero` (objetos, no Ids) |
| 08 · ADO.NET | `SqliteConnection` → `SqliteCommand` → `SqliteDataReader`, parámetros | el cuerpo de cada método |
| 09 · Repositorio | una clase por entidad, una sola razón para cambiar | `VueloRepository`, `PasajeroRepository`… |

La **receta** del tema 08, que se repite en cada método:

1. `using var conexion = new SqliteConnection(cadenaConexion);` y `conexion.Open();`
2. El SQL, con `@parámetros` para todo valor que provenga del exterior.
3. `using var comando = new SqliteCommand(sql, conexion);` + `Parameters.AddWithValue(…)`.
4. Ejecutar: `ExecuteReader` (filas), `ExecuteNonQuery` (filas afectadas) o `ExecuteScalar` (un valor).
5. Si hay filas: `while (lector.Read())` → objetos.

La regla del tema 09 para decidir qué va en el repositorio:

| Sí va | No va |
|---|---|
| el SQL, siempre con parámetros | `Console.WriteLine` o HTML |
| abrir y cerrar conexiones | reglas del negocio ("no se reserva un vuelo que ya salió") |
| pasar de filas a objetos, y de objetos a parámetros | devolver un `SqliteDataReader` |
| devolver `null` o `false` si no existe | SQL de otra entidad |

:::catedra
En el [tema 09](https://taller-de-lenguajes-2.github.io/clases/tema-09-repositorios/index.html) el repositorio venía
con su interfaz (`IPacienteRepository`). En AeroTaller **la interfaz se incorpora en el desafío 7**: por ahora se escribe la clase
concreta, que recibe la cadena por el constructor igual que `PacienteRepository(string cadenaConexion)` de los temas 09
y 10. Cuando en el desafío 7 se reemplace SQLite por datos en memoria, quedará claro para qué sirve la interfaz.
:::

### :lightbulb: Tips y recursos

**¿Cómo se lee una columna?**

| Forma | Ejemplo | A favor | En contra | Cuándo |
|---|---|---|---|---|
| Por posición | `lector.GetInt32(0)` | rápido, tipado, permite la técnica de `desde` para reutilizar bloques | si cambia el orden del `SELECT`, se leen datos cruzados | la que se usa en AeroTaller (tema 08) |
| Por nombre | `lector["Nombre"]` | autoexplicativo; el orden no importa | devuelve `object` (requiere conversión); en un JOIN dos columnas se llaman `Id`: hacen falta alias con `AS` | consultas cortas, sin JOIN |
| Posición a partir del nombre | `lector.GetString(lector.GetOrdinal("Nombre"))` | tipado y sin depender del orden | más extenso; mismo problema de nombres repetidos | cuando el `SELECT` cambia con frecuencia |

**¿Cómo se guarda una relación en el objeto?**

| Forma | Ejemplo | A favor | En contra |
|---|---|---|---|
| Objeto completo | `Vuelo.Origen` es un `Aeropuerto` | en la vista se escribe `vuelo.Origen.Ciudad` sin otra consulta; es orientado a objetos | cada lectura requiere el JOIN; para insertar es necesario construir el objeto (o al menos su `Id`) |
| Solo el Id | `Vuelo.OrigenId` es un `int` | igual a la tabla, simple de insertar | para mostrar la ciudad hay que hacer otra consulta por cada vuelo (el problema "N+1") |

En AeroTaller se usa el objeto completo en el modelo. En el desafío 4 se verá que los **formularios** sí trabajan con
Ids (`OrigenId`, `VueloId`): en ese punto se incorporan los ViewModels.

:::catedra
En el [tema 08](https://taller-de-lenguajes-2.github.io/clases/tema-08-ado-net/index.html) el JOIN de turnos se leía **por
nombre**, con alias (`p.Nombre AS PacienteNombre`, `lector["PacienteNombre"]`). En AeroTaller se lee **por posición**, con un
`desde`, para reutilizar `LeerVuelo` y `LeerPasajero` desde `ReservaRepository`. Las dos formas son correctas: en el
parcial, lo importante es elegir una y ser consistente. Para el Id generado, la diapositiva también menciona `SELECT last_insert_rowid()`
después del `INSERT`; `RETURNING Id` hace lo mismo en una sola instrucción.
:::

## :code-xml: Paso a paso

### Paso 1 · El modelo: `Models/Modelos.cs`

Crear la carpeta `Models/` y dentro `Modelos.cs`. Sin `namespace` (como en los ejemplos de la cátedra): así las clases
son accesibles desde las vistas y los controladores sin `using`.

Las decisiones de abstracción importantes:

- **`Vuelo.Origen` y `Vuelo.Destino` son objetos `Aeropuerto`**, no `int OrigenId`. En la base la relación es una clave
  foránea; en C#, un objeto dentro de otro. Así, en una vista se escribe `vuelo.Origen.Ciudad` sin consultas adicionales.
- **Lo que se deduce, se calcula**: la llegada, la capacidad, si ya salió. No son columnas de la tabla: son propiedades
  de solo lectura (`=>`). Si cambia `Filas`, `Capacidad` nunca queda desactualizada.
- **El estado es un `enum`**, no un `string`: el compilador no permite escribir `"Confirmda"`.

```csharp
public class Vuelo
{
    // Cada fila del avión tiene 6 asientos: A B C | pasillo | D E F
    public const string LetrasAsiento = "ABCDEF";

    public int Id { get; set; }
    public string Numero { get; set; } = "";       // "AT1001"

    // El vuelo "tiene" un origen y un destino: objetos, no sus Id sueltos
    public Aeropuerto Origen { get; set; } = null!;
    public Aeropuerto Destino { get; set; } = null!;

    public DateTime Salida { get; set; }
    public int DuracionMinutos { get; set; }
    public int Filas { get; set; }
    public decimal Precio { get; set; }

    // Propiedades calculadas: no se guardan en la base, se deducen de las otras
    public DateTime Llegada => Salida.AddMinutes(DuracionMinutos);
    public int Capacidad => Filas * LetrasAsiento.Length;
    public bool YaSalio => Salida <= DateTime.Now;
    public bool LlegaAlOtroDia => Llegada.Date > Salida.Date;

    // "12A" es un asiento de este avión si la fila existe y la letra es A–F
    public bool TieneAsiento(string asiento) { … }
}
```

`= null!` le indica al compilador que el repositorio siempre completa ese valor. Con `Nullable` activado, sin eso el compilador
advierte que la propiedad podría quedar en `null`.

:::pista TieneAsiento, sin consultar la solución
`asiento[^1]` es el último carácter (la letra) y `asiento[..^1]` es todo menos el último (la fila).
Usar `int.TryParse` para la fila y `LetrasAsiento.Contains(letra)` para la letra. Contemplar los casos `""` y `"A"`.
:::

:::solucion Ver Modelos.cs completo
```csharp
// El dominio de AeroTaller (desafío 2).
// Son clases "de negocio": no dependen de SQL, de HTML ni de formularios.

public class Aeropuerto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";      // IATA: "TUC"
    public string Ciudad { get; set; } = "";
    public string Nombre { get; set; } = "";

    public override string ToString() => $"{Codigo} · {Ciudad}";
}

public class Vuelo
{
    // Cada fila del avión tiene 6 asientos: A B C | pasillo | D E F
    public const string LetrasAsiento = "ABCDEF";

    public int Id { get; set; }
    public string Numero { get; set; } = "";       // "AT1001"

    // El vuelo "tiene" un origen y un destino: objetos, no sus Id sueltos
    public Aeropuerto Origen { get; set; } = null!;
    public Aeropuerto Destino { get; set; } = null!;

    public DateTime Salida { get; set; }
    public int DuracionMinutos { get; set; }
    public int Filas { get; set; }
    public decimal Precio { get; set; }

    // Propiedades calculadas: no se guardan en la base, se deducen de las otras
    public DateTime Llegada => Salida.AddMinutes(DuracionMinutos);
    public int Capacidad => Filas * LetrasAsiento.Length;
    public bool YaSalio => Salida <= DateTime.Now;
    public bool LlegaAlOtroDia => Llegada.Date > Salida.Date;

    // "12A" es un asiento de este avión si la fila existe y la letra es A–F
    public bool TieneAsiento(string asiento)
    {
        if (string.IsNullOrWhiteSpace(asiento) || asiento.Length < 2) return false;

        char letra = char.ToUpperInvariant(asiento[^1]);
        bool filaValida = int.TryParse(asiento[..^1], out int fila) && fila >= 1 && fila <= Filas;
        return filaValida && LetrasAsiento.Contains(letra);
    }
}

public class Pasajero
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Dni { get; set; } = "";
    public string? Email { get; set; }             // opcional, como en la tabla
}

// Un enum en vez de un string suelto: el compilador no permite escribir "Confirmda"
public enum EstadoReserva
{
    Confirmada,
    Cancelada
}

public class Reserva
{
    public int Id { get; set; }
    public Vuelo Vuelo { get; set; } = null!;
    public Pasajero Pasajero { get; set; } = null!;
    public string Asiento { get; set; } = "";
    public DateTime Fecha { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.Confirmada;
}

// El resultado de una consulta con GROUP BY: no es una tabla, es un "reporte"
public record OcupacionVuelo(int VueloId, string Numero, string Ruta, DateTime Salida, int Capacidad, int Ocupados)
{
    public int Porcentaje => Capacidad == 0 ? 0 : Ocupados * 100 / Capacidad;
}
```
:::

:::catedra
¿`Vuelo` → `Aeropuerto` es composición o agregación? En el [tema 08](https://taller-de-lenguajes-2.github.io/clases/tema-08-ado-net/index.html)
la diapositiva "Una clase por tabla" dice que `Turno` tiene un objeto `Paciente` y lo llama **composición**, en el sentido
amplio de "un objeto armado con otros". Si se aplica la pregunta del [tema 02](https://taller-de-lenguajes-2.github.io/clases/tema-02-composicion-y-agregacion/index.html)
("si destruyo el contenedor, ¿el contenido tiene sentido por sí solo?"), el aeropuerto de Tucumán sigue existiendo aunque
se cancele el vuelo: con ese criterio es **agregación** (◇). Si se pregunta en el parcial, la respuesta adecuada es
la que **se justifica con el ciclo de vida**.
:::

### Paso 2 · El más simple: `AeropuertoRepository`

Crear la carpeta `Repositorios/` y empezar por el más corto, para fijar la receta. Dos métodos: `Listar()` (ordenado por
ciudad) y `Obtener(int id)`.

```csharp
using Microsoft.Data.Sqlite;

// Todo el SQL de la tabla Aeropuerto se concentra aquí
public class AeropuertoRepository(string cadenaConexion)
{
    public Aeropuerto? Obtener(int id)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand("SELECT Id, Codigo, Ciudad, Nombre FROM Aeropuerto WHERE Id = @id", conexion);
        comando.Parameters.AddWithValue("@id", id);

        using var lector = comando.ExecuteReader();
        return lector.Read() ? LeerAeropuerto(lector) : null;   // no existe → null (el 404 se resuelve en el controlador)
    }

    // Detalle interno: de una fila a un objeto. No se usa desde afuera → private
    private static Aeropuerto LeerAeropuerto(SqliteDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        Codigo = lector.GetString(1),
        Ciudad = lector.GetString(2),
        Nombre = lector.GetString(3)
    };
}
```

`(string cadenaConexion)` después del nombre de la clase es un **primary constructor**: el parámetro se puede usar en todos
los métodos. El repositorio **recibe** la cadena; no la define ni la lee de `appsettings.json` (esa responsabilidad corresponde a `Program.cs`).

Escribir `Listar()` sin ayuda: mismo esquema, `ORDER BY Ciudad`, y un `while (lector.Read())` que agrega a una lista.

### Paso 3 · El CRUD completo: `PasajeroRepository`

Es el repositorio típico del tema 09: `Listar`, `Buscar`, `Obtener`, `Insertar`, `Actualizar`, `Eliminar`, más
`ExisteDni` (se necesita en el desafío 5 para no repetir DNIs). Tres elementos nuevos respecto del aeropuerto:

**El `NULL` de la base.** `Email` es opcional: un `NULL` llega como `DBNull` y `GetString` lanza una excepción. Se verifica antes:

```csharp
Email = lector.IsDBNull(3) ? null : lector.GetString(3)
```

A la inversa, al insertar: un `null` de C# se envía como `DBNull.Value`:

```csharp
comando.Parameters.AddWithValue("@email", (object?)pasajero.Email ?? DBNull.Value);   // null de C# → NULL de SQL
```

**Los `%` del LIKE van en el valor, no en el SQL:**

```csharp
// Busca por nombre o por DNI
using var comando = new SqliteCommand(Select + " WHERE Nombre LIKE @texto OR Dni LIKE @texto ORDER BY Nombre", conexion);
comando.Parameters.AddWithValue("@texto", $"%{texto}%");
```

Ese `$"%{texto}%"` es una interpolación, pero construye el **valor del parámetro**: se envía por separado y el motor nunca lo
interpreta como SQL. Lo que no debe hacerse es interpolar **dentro del SQL**.

**Insertar y devolver el Id**, con `RETURNING Id` y `ExecuteScalar` (tema 08):

```csharp
var sql = """
    INSERT INTO Pasajero (Nombre, Dni, Email)
    VALUES (@nombre, @dni, @email)
    RETURNING Id
    """;
```

:::pista Un lector reutilizable con "desde"
Declarar `LeerPasajero(SqliteDataReader lector, int desde)` como **public static**: lee `Id` en `desde`, `Nombre` en `desde + 1`…
Aquí se la llama con `0`; en el paso 5, `ReservaRepository` la llama con otra posición, porque en su JOIN las columnas
del pasajero vienen al final.
:::

:::solucion Ver PasajeroRepository.cs
```csharp
using Microsoft.Data.Sqlite;

// El CRUD completo de una tabla: Listar, Buscar, Obtener, Insertar, Actualizar, Eliminar.
public class PasajeroRepository(string cadenaConexion)
{
    private const string Select = "SELECT Id, Nombre, Dni, Email FROM Pasajero";

    public List<Pasajero> Listar()
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(Select + " ORDER BY Nombre", conexion);
        return LeerPasajeros(comando);
    }

    // Los % van en el VALOR del parámetro, no en el SQL
    public List<Pasajero> Buscar(string texto)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(Select + " WHERE Nombre LIKE @texto OR Dni LIKE @texto ORDER BY Nombre", conexion);
        comando.Parameters.AddWithValue("@texto", $"%{texto}%");
        return LeerPasajeros(comando);
    }

    public Pasajero? Obtener(int id)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(Select + " WHERE Id = @id", conexion);
        comando.Parameters.AddWithValue("@id", id);
        return LeerPasajeros(comando).FirstOrDefault();
    }

    // "excepto": al editar, el propio pasajero no cuenta como duplicado
    public bool ExisteDni(string dni, int? excepto = null)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand("SELECT COUNT(*) FROM Pasajero WHERE Dni = @dni AND Id <> @excepto", conexion);
        comando.Parameters.AddWithValue("@dni", dni);
        comando.Parameters.AddWithValue("@excepto", excepto ?? 0);
        return Convert.ToInt32(comando.ExecuteScalar()) > 0;
    }

    public int Insertar(Pasajero pasajero)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        var sql = """
            INSERT INTO Pasajero (Nombre, Dni, Email)
            VALUES (@nombre, @dni, @email)
            RETURNING Id
            """;
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@nombre", pasajero.Nombre);
        comando.Parameters.AddWithValue("@dni", pasajero.Dni);
        comando.Parameters.AddWithValue("@email", (object?)pasajero.Email ?? DBNull.Value);   // null de C# → NULL de SQL

        return Convert.ToInt32(comando.ExecuteScalar());
    }

    // ExecuteNonQuery devuelve cuántas filas cambiaron: 0 = no existía ese Id
    public bool Actualizar(Pasajero pasajero)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        var sql = "UPDATE Pasajero SET Nombre = @nombre, Dni = @dni, Email = @email WHERE Id = @id";
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@nombre", pasajero.Nombre);
        comando.Parameters.AddWithValue("@dni", pasajero.Dni);
        comando.Parameters.AddWithValue("@email", (object?)pasajero.Email ?? DBNull.Value);
        comando.Parameters.AddWithValue("@id", pasajero.Id);

        return comando.ExecuteNonQuery() > 0;
    }

    // Si el pasajero tiene reservas, la clave foránea impide borrarlo: SQLite lanza SqliteException
    // y el repositorio NO la captura (su manejo no le corresponde). Eso se resuelve en una capa superior (desafío 8).
    public bool Eliminar(int id)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand("DELETE FROM Pasajero WHERE Id = @id", conexion);
        comando.Parameters.AddWithValue("@id", id);
        return comando.ExecuteNonQuery() > 0;
    }

    private static List<Pasajero> LeerPasajeros(SqliteCommand comando)
    {
        var pasajeros = new List<Pasajero>();

        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            pasajeros.Add(LeerPasajero(lector, 0));
        }
        return pasajeros;
    }

    // public static: ReservaRepository la reutiliza para las columnas del pasajero en su JOIN
    public static Pasajero LeerPasajero(SqliteDataReader lector, int desde) => new()
    {
        Id = lector.GetInt32(desde),
        Nombre = lector.GetString(desde + 1),
        Dni = lector.GetString(desde + 2),
        Email = lector.IsDBNull(desde + 3) ? null : lector.GetString(desde + 3)
    };
}
```
:::

### Paso 4 · El JOIN doble: `VueloRepository`

Para construir un `Vuelo` completo se necesitan sus **dos** aeropuertos: es la consulta 3 del desafío 1, ahora desde C#.
Como todos los métodos de lectura usan el mismo `SELECT … FROM … JOIN`, se escribe una sola vez en una constante:

```csharp
private const string SelectConJoin = """
    SELECT v.Id, v.Numero, v.Salida, v.DuracionMinutos, v.Filas, v.Precio,
           o.Id, o.Codigo, o.Ciudad, o.Nombre,
           d.Id, d.Codigo, d.Ciudad, d.Nombre
    FROM Vuelo v
    INNER JOIN Aeropuerto o ON v.OrigenId  = o.Id
    INNER JOIN Aeropuerto d ON v.DestinoId = d.Id
    """;
```

Y un único método que convierte ese bloque de 14 columnas en un `Vuelo` con sus dos `Aeropuerto` adentro:

```csharp
// Lee un vuelo (con sus dos aeropuertos) a partir de la columna "desde".
// public static: ReservaRepository reutiliza el mismo bloque de columnas en su JOIN.
public static Vuelo LeerVuelo(SqliteDataReader lector, int desde) => new()
{
    Id = lector.GetInt32(desde),
    Numero = lector.GetString(desde + 1),
    Salida = LeerFecha(lector, desde + 2),
    DuracionMinutos = lector.GetInt32(desde + 3),
    Filas = lector.GetInt32(desde + 4),
    Precio = lector.GetDecimal(desde + 5),
    Origen = new Aeropuerto { Id = lector.GetInt32(desde + 6), Codigo = lector.GetString(desde + 7), … },
    Destino = new Aeropuerto { Id = lector.GetInt32(desde + 10), Codigo = lector.GetString(desde + 11), … }
};
```

**Las fechas.** En la base son texto `'AAAA-MM-DD HH:MM'`; en C# son `DateTime`. La conversión va siempre con el mismo
formato y con `CultureInfo.InvariantCulture` (la cultura `es-AR` de `Program.cs` es para **mostrar**, no para guardar):

```csharp
public const string FormatoFecha = "yyyy-MM-dd HH:mm";

public static DateTime LeerFecha(SqliteDataReader lector, int columna) =>
    DateTime.ParseExact(lector.GetString(columna), FormatoFecha, CultureInfo.InvariantCulture);

// y al guardar:
comando.Parameters.AddWithValue("@salida", vuelo.Salida.ToString(FormatoFecha, CultureInfo.InvariantCulture));
```

Los métodos del repositorio, y cuándo se usan:

| Método | Qué hace | Se usa en |
|---|---|---|
| `Listar()` | todos, `ORDER BY v.Salida` | desafío 3 |
| `Obtener(int id)` | uno o `null` | desafío 3 |
| `ListarOcupacion()` | la consulta 4 del desafío 1 → `List<OcupacionVuelo>`, solo vuelos futuros | desafío 3 (el tablero) |
| `Buscar(int? destinoId, DateTime? desde)` | filtros opcionales | desafío 4 |
| `ExisteNumero(string numero)` | indica si ya existe un vuelo con ese número | desafío 5 |
| `Insertar(Vuelo vuelo)` | `INSERT … RETURNING Id` con `vuelo.Origen.Id` y `vuelo.Destino.Id` | desafío 5 |

Pueden escribirse ahora los seis o dejar los últimos tres para cuando se necesiten. El verificador de este desafío comprueba que
existan los cuatro repositorios y que usen parámetros.

:::nota
En `Buscar` el `WHERE` se construye por partes según los filtros recibidos (`condiciones.Add("v.DestinoId = @destinoId")`).
Eso **no** es inyección: las partes son texto fijo escrito por quien programa y los valores van como parámetros. El riesgo está en
insertar en el SQL un texto ingresado por el usuario.
:::

:::solucion Ver VueloRepository.cs
```csharp
using System.Globalization;
using Microsoft.Data.Sqlite;

// La tabla Vuelo tiene DOS claves foráneas a Aeropuerto (origen y destino):
// para armar el objeto completo hace falta un JOIN doble, con un alias para cada aeropuerto.
public class VueloRepository(string cadenaConexion)
{
    // Las fechas se guardan como texto: 'AAAA-MM-DD HH:MM'
    public const string FormatoFecha = "yyyy-MM-dd HH:mm";

    private const string SelectConJoin = """
        SELECT v.Id, v.Numero, v.Salida, v.DuracionMinutos, v.Filas, v.Precio,
               o.Id, o.Codigo, o.Ciudad, o.Nombre,
               d.Id, d.Codigo, d.Ciudad, d.Nombre
        FROM Vuelo v
        INNER JOIN Aeropuerto o ON v.OrigenId  = o.Id
        INNER JOIN Aeropuerto d ON v.DestinoId = d.Id
        """;

    public List<Vuelo> Listar()
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(SelectConJoin + " ORDER BY v.Salida", conexion);
        return LeerVuelos(comando);
    }

    // Filtros opcionales: el WHERE se construye solo con los filtros recibidos, pero SIEMPRE con parámetros
    public List<Vuelo> Buscar(int? destinoId, DateTime? desde)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        var condiciones = new List<string>();
        using var comando = new SqliteCommand { Connection = conexion };

        if (destinoId is not null)
        {
            condiciones.Add("v.DestinoId = @destinoId");
            comando.Parameters.AddWithValue("@destinoId", destinoId);
        }
        if (desde is not null)
        {
            // Como la fecha es texto con formato AAAA-MM-DD, comparar textos = comparar fechas
            condiciones.Add("v.Salida >= @desde");
            comando.Parameters.AddWithValue("@desde", desde.Value.ToString(FormatoFecha, CultureInfo.InvariantCulture));
        }

        string where = condiciones.Count > 0 ? " WHERE " + string.Join(" AND ", condiciones) : "";
        comando.CommandText = SelectConJoin + where + " ORDER BY v.Salida";
        return LeerVuelos(comando);
    }

    public Vuelo? Obtener(int id)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(SelectConJoin + " WHERE v.Id = @id", conexion);
        comando.Parameters.AddWithValue("@id", id);
        return LeerVuelos(comando).FirstOrDefault();
    }

    public bool ExisteNumero(string numero)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand("SELECT COUNT(*) FROM Vuelo WHERE Numero = @numero", conexion);
        comando.Parameters.AddWithValue("@numero", numero);
        return Convert.ToInt32(comando.ExecuteScalar()) > 0;
    }

    // INSERT … RETURNING Id: ExecuteScalar devuelve el Id que generó la base
    public int Insertar(Vuelo vuelo)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        var sql = """
            INSERT INTO Vuelo (Numero, OrigenId, DestinoId, Salida, DuracionMinutos, Filas, Precio)
            VALUES (@numero, @origenId, @destinoId, @salida, @duracion, @filas, @precio)
            RETURNING Id
            """;
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@numero", vuelo.Numero);
        comando.Parameters.AddWithValue("@origenId", vuelo.Origen.Id);
        comando.Parameters.AddWithValue("@destinoId", vuelo.Destino.Id);
        comando.Parameters.AddWithValue("@salida", vuelo.Salida.ToString(FormatoFecha, CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("@duracion", vuelo.DuracionMinutos);
        comando.Parameters.AddWithValue("@filas", vuelo.Filas);
        comando.Parameters.AddWithValue("@precio", vuelo.Precio);

        return Convert.ToInt32(comando.ExecuteScalar());
    }

    // LEFT JOIN + GROUP BY: cuántas reservas confirmadas tiene cada vuelo futuro.
    // LEFT para que también aparezcan los vuelos SIN reservas (con 0).
    // La condición del estado va en el ON, no en el WHERE: en el WHERE descartaría esos vuelos.
    public List<OcupacionVuelo> ListarOcupacion()
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        var sql = """
            SELECT v.Id, v.Numero, o.Codigo || ' → ' || d.Codigo AS Ruta, v.Salida,
                   v.Filas * 6 AS Capacidad, COUNT(r.Id) AS Ocupados
            FROM Vuelo v
            INNER JOIN Aeropuerto o ON v.OrigenId  = o.Id
            INNER JOIN Aeropuerto d ON v.DestinoId = d.Id
            LEFT JOIN Reserva r ON r.VueloId = v.Id AND r.Estado = 'Confirmada'
            WHERE v.Salida >= @ahora
            GROUP BY v.Id, v.Numero, Ruta, v.Salida, v.Filas
            ORDER BY v.Salida
            """;
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@ahora", DateTime.Now.ToString(FormatoFecha, CultureInfo.InvariantCulture));

        var resultado = new List<OcupacionVuelo>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            resultado.Add(new OcupacionVuelo(
                lector.GetInt32(0),
                lector.GetString(1),
                lector.GetString(2),
                LeerFecha(lector, 3),
                lector.GetInt32(4),
                lector.GetInt32(5)));
        }
        return resultado;
    }

    private static List<Vuelo> LeerVuelos(SqliteCommand comando)
    {
        var vuelos = new List<Vuelo>();

        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            vuelos.Add(LeerVuelo(lector, 0));
        }
        return vuelos;
    }

    // Lee un vuelo (con sus dos aeropuertos) a partir de la columna "desde".
    // Es public static porque ReservaRepository reutiliza el mismo bloque de columnas en su JOIN.
    public static Vuelo LeerVuelo(SqliteDataReader lector, int desde) => new()
    {
        Id = lector.GetInt32(desde),
        Numero = lector.GetString(desde + 1),
        Salida = LeerFecha(lector, desde + 2),
        DuracionMinutos = lector.GetInt32(desde + 3),
        Filas = lector.GetInt32(desde + 4),
        Precio = lector.GetDecimal(desde + 5),
        Origen = new Aeropuerto
        {
            Id = lector.GetInt32(desde + 6),
            Codigo = lector.GetString(desde + 7),
            Ciudad = lector.GetString(desde + 8),
            Nombre = lector.GetString(desde + 9)
        },
        Destino = new Aeropuerto
        {
            Id = lector.GetInt32(desde + 10),
            Codigo = lector.GetString(desde + 11),
            Ciudad = lector.GetString(desde + 12),
            Nombre = lector.GetString(desde + 13)
        }
    };

    public static DateTime LeerFecha(SqliteDataReader lector, int columna) =>
        DateTime.ParseExact(lector.GetString(columna), FormatoFecha, CultureInfo.InvariantCulture);
}
```
:::

### Paso 5 · Juntar todo: `ReservaRepository`

La reserva es la entidad que involucra más tablas: la reserva, su vuelo **con los dos aeropuertos** y el pasajero. Son cuatro tablas,
pero `Aeropuerto` aparece dos veces: cinco alias. La técnica consiste en ordenar las columnas **en bloques** y reutilizar los
lectores de los otros repositorios:

| Posiciones | Bloque | Se lee con |
|---|---|---|
| 0 – 3 | `r.Id, r.Asiento, r.Fecha, r.Estado` | `LeerReservas` |
| 4 – 17 | el vuelo y sus aeropuertos, **en el mismo orden** que `VueloRepository` | `VueloRepository.LeerVuelo(lector, 4)` |
| 18 – 21 | `p.Id, p.Nombre, p.Dni, p.Email` | `PasajeroRepository.LeerPasajero(lector, 18)` |

**El enum, ida y vuelta.** En la base el estado es texto; en C#, `EstadoReserva`:

```csharp
Estado = Enum.Parse<EstadoReserva>(lector.GetString(3)),        // "Confirmada" → EstadoReserva.Confirmada
comando.Parameters.AddWithValue("@estado", reserva.Estado.ToString());   // y al revés, para guardar
```

Los métodos y su uso en los desafíos siguientes:

| Método | Para |
|---|---|
| `Listar(EstadoReserva? estado)` | el listado de reservas, con filtro opcional por estado (desafío 4) |
| `ListarPorPasajero(int pasajeroId)` | el detalle del pasajero (desafío 3) |
| `Obtener(int id)` | una reserva (desafío 6) |
| `AsientosOcupados(int vueloId)` | `List<string>` con los asientos confirmados: el mapa de asientos (desafío 4) |
| `ContarConfirmadas(int vueloId)` | `COUNT(*)` para no hacer overbooking (desafío 5) |
| `PasajeroTieneReserva(int vueloId, int pasajeroId)` | un pasajero no reserva dos veces el mismo vuelo (desafío 5) |
| `Insertar(Reserva reserva)` | `INSERT … RETURNING Id`, con `reserva.Vuelo.Id` y `reserva.Pasajero.Id` (desafío 4) |
| `CambiarEstado(int id, EstadoReserva estado)` | cancelar = `UPDATE`, no `DELETE` (desafío 6) |

:::cuidado
Las consultas que cuentan o buscan asientos (`AsientosOcupados`, `ContarConfirmadas`, `PasajeroTieneReserva`) filtran
`Estado = 'Confirmada'`. Una reserva cancelada **libera** el asiento: si se omite el filtro, el 1C del AT1001 (cancelado
en los datos de ejemplo) figura como ocupado de forma permanente.
:::

:::solucion Ver ReservaRepository.cs
```csharp
using System.Globalization;
using Microsoft.Data.Sqlite;

// La reserva reúne todo: vuelo (con sus dos aeropuertos) + pasajero → JOIN de 4 tablas (5 alias).
public class ReservaRepository(string cadenaConexion)
{
    // Columnas 0–3: reserva · 4–17: vuelo y aeropuertos (mismo orden que VueloRepository) · 18–21: pasajero
    private const string SelectConJoin = """
        SELECT r.Id, r.Asiento, r.Fecha, r.Estado,
               v.Id, v.Numero, v.Salida, v.DuracionMinutos, v.Filas, v.Precio,
               o.Id, o.Codigo, o.Ciudad, o.Nombre,
               d.Id, d.Codigo, d.Ciudad, d.Nombre,
               p.Id, p.Nombre, p.Dni, p.Email
        FROM Reserva r
        INNER JOIN Vuelo v      ON r.VueloId    = v.Id
        INNER JOIN Aeropuerto o ON v.OrigenId   = o.Id
        INNER JOIN Aeropuerto d ON v.DestinoId  = d.Id
        INNER JOIN Pasajero p   ON r.PasajeroId = p.Id
        """;

    public List<Reserva> Listar(EstadoReserva? estado)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand { Connection = conexion };
        comando.CommandText = SelectConJoin;
        if (estado is not null)
        {
            comando.CommandText += " WHERE r.Estado = @estado";
            comando.Parameters.AddWithValue("@estado", estado.ToString());   // el enum se guarda como texto
        }
        comando.CommandText += " ORDER BY v.Salida, r.Asiento";
        return LeerReservas(comando);
    }

    public List<Reserva> ListarPorPasajero(int pasajeroId)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(SelectConJoin + " WHERE r.PasajeroId = @pasajeroId ORDER BY v.Salida", conexion);
        comando.Parameters.AddWithValue("@pasajeroId", pasajeroId);
        return LeerReservas(comando);
    }

    public Reserva? Obtener(int id)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(SelectConJoin + " WHERE r.Id = @id", conexion);
        comando.Parameters.AddWithValue("@id", id);
        return LeerReservas(comando).FirstOrDefault();
    }

    public List<string> AsientosOcupados(int vueloId)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(
            "SELECT Asiento FROM Reserva WHERE VueloId = @vueloId AND Estado = 'Confirmada'", conexion);
        comando.Parameters.AddWithValue("@vueloId", vueloId);

        var asientos = new List<string>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            asientos.Add(lector.GetString(0));
        }
        return asientos;
    }

    public bool PasajeroTieneReserva(int vueloId, int pasajeroId)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand("""
            SELECT COUNT(*) FROM Reserva
            WHERE VueloId = @vueloId AND PasajeroId = @pasajeroId AND Estado = 'Confirmada'
            """, conexion);
        comando.Parameters.AddWithValue("@vueloId", vueloId);
        comando.Parameters.AddWithValue("@pasajeroId", pasajeroId);
        return Convert.ToInt32(comando.ExecuteScalar()) > 0;
    }

    public int ContarConfirmadas(int vueloId)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(
            "SELECT COUNT(*) FROM Reserva WHERE VueloId = @vueloId AND Estado = 'Confirmada'", conexion);
        comando.Parameters.AddWithValue("@vueloId", vueloId);
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public int Insertar(Reserva reserva)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        var sql = """
            INSERT INTO Reserva (VueloId, PasajeroId, Asiento, Fecha, Estado)
            VALUES (@vueloId, @pasajeroId, @asiento, @fecha, @estado)
            RETURNING Id
            """;
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@vueloId", reserva.Vuelo.Id);
        comando.Parameters.AddWithValue("@pasajeroId", reserva.Pasajero.Id);
        comando.Parameters.AddWithValue("@asiento", reserva.Asiento);
        comando.Parameters.AddWithValue("@fecha", reserva.Fecha.ToString(VueloRepository.FormatoFecha, CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("@estado", reserva.Estado.ToString());

        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public bool CambiarEstado(int id, EstadoReserva estado)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand("UPDATE Reserva SET Estado = @estado WHERE Id = @id", conexion);
        comando.Parameters.AddWithValue("@estado", estado.ToString());
        comando.Parameters.AddWithValue("@id", id);
        return comando.ExecuteNonQuery() > 0;
    }

    private static List<Reserva> LeerReservas(SqliteCommand comando)
    {
        var reservas = new List<Reserva>();

        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            reservas.Add(new Reserva
            {
                Id = lector.GetInt32(0),
                Asiento = lector.GetString(1),
                Fecha = VueloRepository.LeerFecha(lector, 2),
                Estado = Enum.Parse<EstadoReserva>(lector.GetString(3)),   // texto → enum
                Vuelo = VueloRepository.LeerVuelo(lector, 4),
                Pasajero = PasajeroRepository.LeerPasajero(lector, 18)
            });
        }
        return reservas;
    }
}
```
:::

### Paso 6 · Probarlos sin pantallas

Todavía no hay controladores que usen los repositorios, pero ya pueden probarse. En `Program.cs`, **justo después** de
`CrearBaseSiNoExiste(cadenaConexion);`, agregar este bloque temporal:

```csharp
// ── PRUEBA TEMPORAL (desafío 2): eliminar al terminar ──
var prueba = new VueloRepository(cadenaConexion).Listar();
var reservasDeAna = new ReservaRepository(cadenaConexion).ListarPorPasajero(1);
var terminanEnEz = new PasajeroRepository(cadenaConexion).Buscar("ez");
var ocupacion = new VueloRepository(cadenaConexion).ListarOcupacion();
Console.WriteLine($"Vuelos: {prueba.Count} · reservas de Ana: {reservasDeAna.Count}");   // ← breakpoint aquí
```

Colocar el breakpoint en el `Console.WriteLine` y arrancar con **F5**. La ejecución se detiene antes de que la app comience a atender solicitudes.
En el panel **Variables**, desplegar cada lista y comprobar:

| Variable | Qué debe verse |
|---|---|
| `prueba` | 9 vuelos ordenados por salida; cada uno con `Origen` y `Destino` desplegables |
| `reservasDeAna` | 3 reservas, todas en el 3A, cada una con su `Vuelo` (con aeropuertos) y su `Pasajero` |
| `terminanEnEz` | Ana Gómez y Facundo López |
| `ocupacion` | 8 vuelos (el que ya salió no está) con `Ocupados` y `Porcentaje` |

Si algo falla, el mensaje de la excepción indica qué columna se leyó mal.

:::cuidado
Cuando todo funcione, **eliminar el bloque de prueba**. Es código de exploración, no para el repositorio: la app no debe hacer
consultas de prueba cada vez que arranca. En el desafío 3, los controladores obtienen los repositorios sin usar `new`.
:::

### Paso 7 · Commit

Como se indicó en la misión: un commit por repositorio es una buena práctica. Al final, el commit del desafío con
`Program.cs` limpio.
