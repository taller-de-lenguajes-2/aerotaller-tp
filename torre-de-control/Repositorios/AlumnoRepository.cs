using Microsoft.Data.Sqlite;

public class AlumnoRepository(ConexionDb conexionDb) : IAlumnoRepository
{
    private readonly string cadenaConexion = conexionDb.Cadena;

    private const string Select = "SELECT Id, Nombre, RutaProyecto, ModoLibre, Creado FROM Alumno";

    public List<Alumno> Listar()
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(Select + " ORDER BY Nombre", conexion);
        using var lector = comando.ExecuteReader();

        var alumnos = new List<Alumno>();
        while (lector.Read()) alumnos.Add(LeerAlumno(lector));
        return alumnos;
    }

    public Alumno? Obtener(int id)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand(Select + " WHERE Id = @id", conexion);
        comando.Parameters.AddWithValue("@id", id);
        using var lector = comando.ExecuteReader();
        return lector.Read() ? LeerAlumno(lector) : null;
    }

    public int Insertar(Alumno alumno)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand("""
            INSERT INTO Alumno (Nombre, RutaProyecto, ModoLibre, Creado)
            VALUES (@nombre, @ruta, @libre, @creado)
            RETURNING Id
            """, conexion);
        comando.Parameters.AddWithValue("@nombre", alumno.Nombre);
        comando.Parameters.AddWithValue("@ruta", (object?)alumno.RutaProyecto ?? DBNull.Value);
        comando.Parameters.AddWithValue("@libre", alumno.ModoLibre ? 1 : 0);
        comando.Parameters.AddWithValue("@creado", Fechas.ATexto(alumno.Creado));
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public bool Actualizar(Alumno alumno)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();

        using var comando = new SqliteCommand("""
            UPDATE Alumno SET Nombre = @nombre, RutaProyecto = @ruta, ModoLibre = @libre
            WHERE Id = @id
            """, conexion);
        comando.Parameters.AddWithValue("@nombre", alumno.Nombre);
        comando.Parameters.AddWithValue("@ruta", (object?)alumno.RutaProyecto ?? DBNull.Value);
        comando.Parameters.AddWithValue("@libre", alumno.ModoLibre ? 1 : 0);
        comando.Parameters.AddWithValue("@id", alumno.Id);
        return comando.ExecuteNonQuery() > 0;
    }

    private static Alumno LeerAlumno(SqliteDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        Nombre = lector.GetString(1),
        RutaProyecto = lector.IsDBNull(2) ? null : lector.GetString(2),
        ModoLibre = lector.GetInt32(3) == 1,
        Creado = Fechas.DeTexto(lector.GetString(4))
    };
}
