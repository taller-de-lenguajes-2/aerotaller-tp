using Microsoft.Data.Sqlite;

// El botón "Crear base de datos" de la Torre: ejecuta AeroTaller/aerotaller.sql (el script que viene con la plantilla)
// y genera aerotaller.db en la carpeta AeroTaller/ del repositorio del alumno, ubicación desde la que la aplicación la abre al iniciarse.
public class BaseAeroTallerService(
    IProgresoService progreso,
    IVerificadorService verificador,
    ILogger<BaseAeroTallerService> logger) : IBaseAeroTallerService
{
    private const string NombreBase = "aerotaller.db";
    private const string NombreScript = "aerotaller.sql";

    public EstadoBaseAeroTaller Estado(int alumnoId) => Estado(CarpetaAeroTaller(alumnoId));

    public ResultadoCrearBase Crear(int alumnoId)
    {
        string carpeta = CarpetaAeroTaller(alumnoId);
        string script = Path.Combine(carpeta, NombreScript);
        if (!Directory.Exists(carpeta))
            return new ResultadoCrearBase(false,
                $"No existe la carpeta {carpeta}. Revisar la carpeta del repositorio en la tarjeta de embarque.", Estado(carpeta));
        if (!File.Exists(script))
            return new ResultadoCrearBase(false, $"No se encontró el script {script}.", Estado(carpeta));

        string archivo = Path.Combine(carpeta, NombreBase);
        bool existia = File.Exists(archivo);

        // Pooling = false: al terminar no queda ninguna conexión abierta que retenga el archivo
        var cadena = new SqliteConnectionStringBuilder { DataSource = archivo, Pooling = false, DefaultTimeout = 5 }.ToString();
        try
        {
            using var conexion = new SqliteConnection(cadena);
            conexion.Open();

            // Las claves foráneas se desactivan solo para vaciar la base en cualquier orden (fuera de la transacción: así lo exige SQLite)
            Ejecutar(conexion, null, "PRAGMA foreign_keys = OFF;");
            using var transaccion = conexion.BeginTransaction();

            // Se eliminan todos los objetos existentes (incluidas las tablas que el script ya no define) y se ejecuta el script completo.
            // Si se produce un error, el Rollback deja la base exactamente en su estado anterior.
            foreach ((string tipo, string nombre) in Objetos(conexion, transaccion))
                Ejecutar(conexion, transaccion, $"DROP {tipo} IF EXISTS \"{nombre.Replace("\"", "\"\"")}\";");
            Ejecutar(conexion, transaccion, File.ReadAllText(script));

            // Con las claves foráneas desactivadas, un INSERT inválido no fallaría: se verifica al final
            List<string> violaciones = ViolacionesDeClaves(conexion, transaccion);
            if (violaciones.Count > 0)
            {
                transaccion.Rollback();
                return new ResultadoCrearBase(false,
                    "El script carga filas que no respetan las claves foráneas: " + string.Join("; ", violaciones) + ". La base no se modificó.",
                    Estado(carpeta));
            }

            transaccion.Commit();
        }
        catch (SqliteException ex)
        {
            logger.LogWarning(ex, "No se pudo crear {Archivo}", archivo);
            string mensaje = ex.SqliteErrorCode switch
            {
                5 or 6 => "La base está bloqueada por otro programa. En DB Browser, usar «Escribir cambios» o cerrar la base, y volver a intentar.",
                _ => $"El script tiene un error: {ex.Message}"
            };
            return new ResultadoCrearBase(false, mensaje + (existia ? " La base anterior no se modificó." : ""), Estado(carpeta));
        }

        logger.LogInformation("Base de AeroTaller {Accion} en {Archivo}", existia ? "recreada" : "creada", archivo);
        return new ResultadoCrearBase(true,
            existia ? "Base recreada: se restauraron los datos del script, con fechas a partir de hoy." : "Base creada a partir de aerotaller.sql.",
            Estado(carpeta));
    }

    private string CarpetaAeroTaller(int alumnoId)
    {
        Alumno alumno = progreso.ObtenerAlumno(alumnoId);
        string repo = string.IsNullOrWhiteSpace(alumno.RutaProyecto) ? verificador.RutaPorDefecto : alumno.RutaProyecto;
        return Path.Combine(repo, "AeroTaller");
    }

    private static EstadoBaseAeroTaller Estado(string carpeta)
    {
        string archivo = Path.Combine(carpeta, NombreBase);
        bool hayScript = File.Exists(Path.Combine(carpeta, NombreScript));
        var info = new FileInfo(archivo);
        if (!info.Exists)
            return new EstadoBaseAeroTaller(carpeta, archivo, hayScript, false, 0, null, [], null);

        var tablas = new List<TablaBase>();
        string? problema = null;
        try
        {
            // Solo lectura: consultar el estado de la base nunca la modifica ni la crea
            var cadena = new SqliteConnectionStringBuilder { DataSource = archivo, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString();
            using var conexion = new SqliteConnection(cadena);
            conexion.Open();
            foreach ((string tipo, string nombre) in Objetos(conexion, null).Where(o => o.Tipo == "TABLE"))
            {
                using var contar = conexion.CreateCommand();
                contar.CommandText = $"SELECT COUNT(*) FROM \"{nombre.Replace("\"", "\"\"")}\"";
                tablas.Add(new TablaBase(nombre, Convert.ToInt64(contar.ExecuteScalar())));
            }
            if (tablas.Count == 0) problema = "La base existe pero no tiene tablas: el script no se ejecutó.";
        }
        catch (SqliteException ex)
        {
            problema = $"No se pudo leer la base: {ex.Message}";
        }

        return new EstadoBaseAeroTaller(carpeta, archivo, hayScript, true, info.Length, info.LastWriteTime, tablas, problema);
    }

    // Tablas, vistas y disparadores creados por el usuario (las tablas internas de SQLite empiezan con sqlite_)
    private static List<(string Tipo, string Nombre)> Objetos(SqliteConnection conexion, SqliteTransaction? transaccion)
    {
        using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion;
        comando.CommandText = """
            SELECT UPPER(type), name FROM sqlite_master
            WHERE type IN ('table', 'view', 'trigger') AND name NOT LIKE 'sqlite_%'
            ORDER BY CASE type WHEN 'trigger' THEN 0 WHEN 'view' THEN 1 ELSE 2 END, name
            """;
        var objetos = new List<(string, string)>();
        using var lector = comando.ExecuteReader();
        while (lector.Read()) objetos.Add((lector.GetString(0), lector.GetString(1)));
        return objetos;
    }

    private static List<string> ViolacionesDeClaves(SqliteConnection conexion, SqliteTransaction transaccion)
    {
        using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion;
        comando.CommandText = "PRAGMA foreign_key_check;";
        var violaciones = new List<string>();
        using var lector = comando.ExecuteReader();
        while (lector.Read() && violaciones.Count < 5)
            violaciones.Add($"{lector.GetString(0)} fila {lector.GetValue(1)} → {lector.GetString(2)}");
        return violaciones;
    }

    private static void Ejecutar(SqliteConnection conexion, SqliteTransaction? transaccion, string sql)
    {
        using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion;
        comando.CommandText = sql;
        comando.ExecuteNonQuery();
    }
}
