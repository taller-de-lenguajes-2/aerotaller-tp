using System.Text.Json;
using Microsoft.Data.Sqlite;

public class IntentoQuizRepository(ConexionDb conexionDb) : IIntentoQuizRepository
{
    private readonly string cadenaConexion = conexionDb.Cadena;

    private const string Select = "SELECT Id, AlumnoId, DesafioId, Correctas, Total, Aprobado, Respuestas, Fecha FROM IntentoQuiz";

    public List<IntentoQuiz> Listar(int alumnoId)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();
        using var comando = new SqliteCommand(Select + " WHERE AlumnoId = @alumno ORDER BY Id", conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        return LeerIntentos(comando);
    }

    public List<IntentoQuiz> Listar(int alumnoId, string desafioId)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();
        using var comando = new SqliteCommand(Select + " WHERE AlumnoId = @alumno AND DesafioId = @desafio ORDER BY Id", conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        comando.Parameters.AddWithValue("@desafio", desafioId);
        return LeerIntentos(comando);
    }

    public int Insertar(IntentoQuiz intento)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();
        using var comando = new SqliteCommand("""
            INSERT INTO IntentoQuiz (AlumnoId, DesafioId, Correctas, Total, Aprobado, Respuestas, Fecha)
            VALUES (@alumno, @desafio, @correctas, @total, @aprobado, @respuestas, @fecha)
            RETURNING Id
            """, conexion);
        comando.Parameters.AddWithValue("@alumno", intento.AlumnoId);
        comando.Parameters.AddWithValue("@desafio", intento.DesafioId);
        comando.Parameters.AddWithValue("@correctas", intento.Correctas);
        comando.Parameters.AddWithValue("@total", intento.Total);
        comando.Parameters.AddWithValue("@aprobado", intento.Aprobado ? 1 : 0);
        comando.Parameters.AddWithValue("@respuestas", JsonSerializer.Serialize(intento.Respuestas));
        comando.Parameters.AddWithValue("@fecha", Fechas.ATexto(intento.Fecha));
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    private static List<IntentoQuiz> LeerIntentos(SqliteCommand comando)
    {
        var intentos = new List<IntentoQuiz>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            intentos.Add(new IntentoQuiz
            {
                Id = lector.GetInt32(0),
                AlumnoId = lector.GetInt32(1),
                DesafioId = lector.GetString(2),
                Correctas = lector.GetInt32(3),
                Total = lector.GetInt32(4),
                Aprobado = lector.GetInt32(5) == 1,
                Respuestas = JsonSerializer.Deserialize<List<RespuestaQuiz>>(lector.GetString(6)) ?? [],
                Fecha = Fechas.DeTexto(lector.GetString(7))
            });
        }
        return intentos;
    }
}

public class BitacoraRepository(ConexionDb conexionDb) : IBitacoraRepository
{
    private readonly string cadenaConexion = conexionDb.Cadena;

    public List<NotaBitacora> Listar(int alumnoId)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();
        using var comando = new SqliteCommand("""
            SELECT Id, AlumnoId, DesafioId, Tipo, Texto, Resuelta, Fecha
            FROM NotaBitacora WHERE AlumnoId = @alumno ORDER BY Fecha DESC, Id DESC
            """, conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);

        var notas = new List<NotaBitacora>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            notas.Add(new NotaBitacora
            {
                Id = lector.GetInt32(0),
                AlumnoId = lector.GetInt32(1),
                DesafioId = lector.IsDBNull(2) ? null : lector.GetString(2),
                Tipo = lector.GetString(3),
                Texto = lector.GetString(4),
                Resuelta = lector.GetInt32(5) == 1,
                Fecha = Fechas.DeTexto(lector.GetString(6))
            });
        }
        return notas;
    }

    public int Insertar(NotaBitacora nota)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();
        using var comando = new SqliteCommand("""
            INSERT INTO NotaBitacora (AlumnoId, DesafioId, Tipo, Texto, Fecha)
            VALUES (@alumno, @desafio, @tipo, @texto, @fecha)
            RETURNING Id
            """, conexion);
        comando.Parameters.AddWithValue("@alumno", nota.AlumnoId);
        comando.Parameters.AddWithValue("@desafio", (object?)nota.DesafioId ?? DBNull.Value);
        comando.Parameters.AddWithValue("@tipo", nota.Tipo);
        comando.Parameters.AddWithValue("@texto", nota.Texto);
        comando.Parameters.AddWithValue("@fecha", Fechas.ATexto(nota.Fecha));
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public bool CambiarResuelta(int alumnoId, int id, bool resuelta)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();
        using var comando = new SqliteCommand(
            "UPDATE NotaBitacora SET Resuelta = @resuelta WHERE Id = @id AND AlumnoId = @alumno", conexion);
        comando.Parameters.AddWithValue("@resuelta", resuelta ? 1 : 0);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        return comando.ExecuteNonQuery() > 0;
    }

    public bool Eliminar(int alumnoId, int id)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();
        using var comando = new SqliteCommand("DELETE FROM NotaBitacora WHERE Id = @id AND AlumnoId = @alumno", conexion);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        return comando.ExecuteNonQuery() > 0;
    }
}

public class InsigniaRepository(ConexionDb conexionDb) : IInsigniaRepository
{
    private readonly string cadenaConexion = conexionDb.Cadena;

    public List<InsigniaObtenida> Listar(int alumnoId)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();
        using var comando = new SqliteCommand(
            "SELECT InsigniaId, Fecha FROM InsigniaObtenida WHERE AlumnoId = @alumno ORDER BY Fecha", conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);

        var lista = new List<InsigniaObtenida>();
        using var lector = comando.ExecuteReader();
        while (lector.Read()) lista.Add(new InsigniaObtenida(lector.GetString(0), Fechas.DeTexto(lector.GetString(1))));
        return lista;
    }

    public void Otorgar(int alumnoId, string insigniaId)
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();
        using var comando = new SqliteCommand(
            "INSERT OR IGNORE INTO InsigniaObtenida (AlumnoId, InsigniaId, Fecha) VALUES (@alumno, @insignia, @ahora)", conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        comando.Parameters.AddWithValue("@insignia", insigniaId);
        comando.Parameters.AddWithValue("@ahora", Fechas.Ahora());
        comando.ExecuteNonQuery();
    }
}
