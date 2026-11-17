using Microsoft.Data.Sqlite;

// Estados de los desafíos, tareas marcadas, lecturas abiertas y estado del repo git.
public class ProgresoRepository(ConexionDb conexionDb) : IProgresoRepository
{
    private readonly string cadenaConexion = conexionDb.Cadena;

    private SqliteConnection Abrir()
    {
        var conexion = new SqliteConnection(cadenaConexion);
        conexion.Open();
        return conexion;
    }

    public Dictionary<string, EstadoDesafio> Estados(int alumnoId)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand(
            "SELECT DesafioId, Iniciado, Completado FROM EstadoDesafio WHERE AlumnoId = @alumno", conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);

        var estados = new Dictionary<string, EstadoDesafio>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            string id = lector.GetString(0);
            estados[id] = new EstadoDesafio(id, Fechas.DeTexto(lector.GetString(1)),
                lector.IsDBNull(2) ? null : Fechas.DeTexto(lector.GetString(2)));
        }
        return estados;
    }

    // INSERT OR IGNORE: si ya estaba iniciado, no cambia nada (la clave primaria lo impide)
    public void Iniciar(int alumnoId, string desafioId)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand(
            "INSERT OR IGNORE INTO EstadoDesafio (AlumnoId, DesafioId, Iniciado) VALUES (@alumno, @desafio, @ahora)", conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        comando.Parameters.AddWithValue("@desafio", desafioId);
        comando.Parameters.AddWithValue("@ahora", Fechas.Ahora());
        comando.ExecuteNonQuery();
    }

    // UPSERT: inserta o actualiza en una sola sentencia
    public void Completar(int alumnoId, string desafioId)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand("""
            INSERT INTO EstadoDesafio (AlumnoId, DesafioId, Iniciado, Completado)
            VALUES (@alumno, @desafio, @ahora, @ahora)
            ON CONFLICT (AlumnoId, DesafioId) DO UPDATE SET Completado = COALESCE(Completado, @ahora)
            """, conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        comando.Parameters.AddWithValue("@desafio", desafioId);
        comando.Parameters.AddWithValue("@ahora", Fechas.Ahora());
        comando.ExecuteNonQuery();
    }

    public List<string> TareasMarcadas(int alumnoId, string desafioId)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand(
            "SELECT TareaId FROM TareaMarcada WHERE AlumnoId = @alumno AND DesafioId = @desafio", conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        comando.Parameters.AddWithValue("@desafio", desafioId);
        return LeerTextos(comando);
    }

    // GROUP BY: cuántas tareas marcó en cada desafío (para el mapa, en una sola consulta)
    public Dictionary<string, int> ContarTareasPorDesafio(int alumnoId)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand(
            "SELECT DesafioId, COUNT(*) FROM TareaMarcada WHERE AlumnoId = @alumno GROUP BY DesafioId", conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);

        var cuentas = new Dictionary<string, int>();
        using var lector = comando.ExecuteReader();
        while (lector.Read()) cuentas[lector.GetString(0)] = lector.GetInt32(1);
        return cuentas;
    }

    public void MarcarTarea(int alumnoId, string desafioId, string tareaId, bool marcada)
    {
        using var conexion = Abrir();
        var sql = marcada
            ? "INSERT OR IGNORE INTO TareaMarcada (AlumnoId, DesafioId, TareaId, Fecha) VALUES (@alumno, @desafio, @tarea, @ahora)"
            : "DELETE FROM TareaMarcada WHERE AlumnoId = @alumno AND DesafioId = @desafio AND TareaId = @tarea";
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        comando.Parameters.AddWithValue("@desafio", desafioId);
        comando.Parameters.AddWithValue("@tarea", tareaId);
        comando.Parameters.AddWithValue("@ahora", Fechas.Ahora());
        comando.ExecuteNonQuery();
    }

    public List<string> LecturasAbiertas(int alumnoId, string desafioId)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand(
            "SELECT LecturaId FROM LecturaAbierta WHERE AlumnoId = @alumno AND DesafioId = @desafio", conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        comando.Parameters.AddWithValue("@desafio", desafioId);
        return LeerTextos(comando);
    }

    public int ContarLecturas(int alumnoId)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand("SELECT COUNT(*) FROM LecturaAbierta WHERE AlumnoId = @alumno", conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public void AbrirLectura(int alumnoId, string desafioId, string lecturaId)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand("""
            INSERT OR IGNORE INTO LecturaAbierta (AlumnoId, DesafioId, LecturaId, Fecha)
            VALUES (@alumno, @desafio, @lectura, @ahora)
            """, conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        comando.Parameters.AddWithValue("@desafio", desafioId);
        comando.Parameters.AddWithValue("@lectura", lecturaId);
        comando.Parameters.AddWithValue("@ahora", Fechas.Ahora());
        comando.ExecuteNonQuery();
    }

    public void GuardarEstadoRepo(int alumnoId, string desafioId, int sinCommitear, int sinPushear)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand("""
            INSERT INTO EstadoRepo (AlumnoId, DesafioId, SinCommitear, SinPushear, Fecha)
            VALUES (@alumno, @desafio, @sinCommitear, @sinPushear, @ahora)
            """, conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        comando.Parameters.AddWithValue("@desafio", desafioId);
        comando.Parameters.AddWithValue("@sinCommitear", sinCommitear);
        comando.Parameters.AddWithValue("@sinPushear", sinPushear);
        comando.Parameters.AddWithValue("@ahora", Fechas.Ahora());
        comando.ExecuteNonQuery();
    }

    // En cuántos desafíos distintos verificó con el repo "al día" (todo commiteado y pusheado)
    public int ContarDesafiosConRepoAlDia(int alumnoId)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand("""
            SELECT COUNT(DISTINCT DesafioId) FROM EstadoRepo
            WHERE AlumnoId = @alumno AND SinCommitear = 0 AND SinPushear = 0
            """, conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public void GuardarVerificacion(int alumnoId, string desafioId, bool aprobado, int chequeos, int fallidos)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand("""
            INSERT INTO Verificacion (AlumnoId, DesafioId, Aprobado, Chequeos, Fallidos, Fecha)
            VALUES (@alumno, @desafio, @aprobado, @chequeos, @fallidos, @ahora)
            """, conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        comando.Parameters.AddWithValue("@desafio", desafioId);
        comando.Parameters.AddWithValue("@aprobado", aprobado ? 1 : 0);
        comando.Parameters.AddWithValue("@chequeos", chequeos);
        comando.Parameters.AddWithValue("@fallidos", fallidos);
        comando.Parameters.AddWithValue("@ahora", Fechas.Ahora());
        comando.ExecuteNonQuery();
    }

    // La última verificación de cada desafío (subconsulta con MAX por grupo)
    public Dictionary<string, UltimaVerificacion> UltimasVerificaciones(int alumnoId)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand("""
            SELECT v.DesafioId, v.Aprobado, v.Fecha FROM Verificacion v
            WHERE v.AlumnoId = @alumno
              AND v.Id = (SELECT MAX(Id) FROM Verificacion WHERE AlumnoId = v.AlumnoId AND DesafioId = v.DesafioId)
            """, conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);

        var ultimas = new Dictionary<string, UltimaVerificacion>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
            ultimas[lector.GetString(0)] = new UltimaVerificacion(lector.GetInt32(1) == 1, Fechas.DeTexto(lector.GetString(2)));
        return ultimas;
    }

    // En cuántos desafíos distintos el verificador aprobó todos los chequeos alguna vez
    public int ContarDesafiosVerificados(int alumnoId)
    {
        using var conexion = Abrir();
        using var comando = new SqliteCommand(
            "SELECT COUNT(DISTINCT DesafioId) FROM Verificacion WHERE AlumnoId = @alumno AND Aprobado = 1", conexion);
        comando.Parameters.AddWithValue("@alumno", alumnoId);
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    private static List<string> LeerTextos(SqliteCommand comando)
    {
        var textos = new List<string>();
        using var lector = comando.ExecuteReader();
        while (lector.Read()) textos.Add(lector.GetString(0));
        return textos;
    }
}
