using Microsoft.Data.Sqlite;

// Lee la base creada por la aplicación del alumno (la temporal), sin modificarla.
public class BaseDatos(string ruta)
{
    public string Ruta { get; } = ruta;
    public bool Existe => File.Exists(Ruta);

    private SqliteConnection Abrir()
    {
        if (!File.Exists(Ruta))
            throw new VerificacionAbortada("La aplicación inició pero no creó la base de datos: primero debe completarse el desafío 1 (CrearBaseSiNoExiste con la cadena de la configuración).");
        // Pooling=False: no dejar el archivo abierto (la aplicación del alumno también lo usa)
        var conexion = new SqliteConnection($"Data Source={Ruta};Mode=ReadOnly;Pooling=False");
        conexion.Open();
        return conexion;
    }

    public List<string> Tablas()
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand("SELECT name FROM sqlite_master WHERE type = 'table'", conexion);
        return Leer(comando, l => l.GetString(0));
    }

    public bool TieneTabla(string tabla) => Tablas().Contains(tabla, StringComparer.OrdinalIgnoreCase);

    public List<string> Columnas(string tabla)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand($"SELECT name FROM pragma_table_info(@tabla)", conexion);
        comando.Parameters.AddWithValue("@tabla", tabla);
        return Leer(comando, l => l.GetString(0));
    }

    // (columna local, tabla referenciada)
    public List<(string Desde, string Tabla)> ClavesForaneas(string tabla)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand("SELECT \"from\", \"table\" FROM pragma_foreign_key_list(@tabla)", conexion);
        comando.Parameters.AddWithValue("@tabla", tabla);
        return Leer(comando, l => (l.GetString(0), l.GetString(1)));
    }

    // ¿La columna tiene una restricción UNIQUE (o un índice único sobre ella sola)?
    public bool EsUnica(string tabla, string columna)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand("""
            SELECT COUNT(*) FROM pragma_index_list(@tabla) i
            WHERE i."unique" = 1 AND i.partial = 0
              AND (SELECT COUNT(*) FROM pragma_index_info(i.name)) = 1
              AND (SELECT name FROM pragma_index_info(i.name)) = @columna COLLATE NOCASE
            """, conexion);
        comando.Parameters.AddWithValue("@tabla", tabla);
        comando.Parameters.AddWithValue("@columna", columna);
        return Convert.ToInt32(comando.ExecuteScalar()) > 0;
    }

    // Índices únicos parciales (… WHERE …) de una tabla, con su SQL
    public List<string> IndicesUnicosParciales(string tabla)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand("""
            SELECT m.sql FROM pragma_index_list(@tabla) i
            JOIN sqlite_master m ON m.name = i.name
            WHERE i."unique" = 1 AND i.partial = 1
            """, conexion);
        comando.Parameters.AddWithValue("@tabla", tabla);
        return Leer(comando, l => l.IsDBNull(0) ? "" : l.GetString(0));
    }

    public long Contar(string sql, params (string Nombre, object Valor)[] parametros) => Convert.ToInt64(Escalar(sql, parametros));

    public object? Escalar(string sql, params (string Nombre, object Valor)[] parametros)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand(sql, conexion);
        foreach ((string nombre, object valor) in parametros) comando.Parameters.AddWithValue(nombre, valor);
        return comando.ExecuteScalar();
    }

    public int IdVuelo(string numero) =>
        Convert.ToInt32(Escalar("SELECT Id FROM Vuelo WHERE Numero = @n", ("@n", numero)) ?? throw new VerificacionAbortada($"No se encontró el vuelo {numero} de los datos de aerotaller.sql"));

    public int IdPasajero(string dni) =>
        Convert.ToInt32(Escalar("SELECT Id FROM Pasajero WHERE Dni = @d", ("@d", dni)) ?? throw new VerificacionAbortada($"No se encontró el pasajero con DNI {dni} de los datos de aerotaller.sql"));

    public long ReservasConfirmadas(int vueloId, string asiento) =>
        Contar("SELECT COUNT(*) FROM Reserva WHERE VueloId = @v AND Asiento = @a AND Estado = 'Confirmada'", ("@v", vueloId), ("@a", asiento));

    public long TotalReservas() => Contar("SELECT COUNT(*) FROM Reserva");

    private static List<T> Leer<T>(SqliteCommand comando, Func<SqliteDataReader, T> leer)
    {
        var lista = new List<T>();
        using var lector = comando.ExecuteReader();
        while (lector.Read()) lista.Add(leer(lector));
        return lista;
    }
}
