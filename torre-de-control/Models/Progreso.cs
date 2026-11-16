// El PROGRESO de cada alumno: esto sí se guarda en progreso.db.

public class Alumno
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? RutaProyecto { get; set; }
    public bool ModoLibre { get; set; }
    public DateTime Creado { get; set; }
}

public record EstadoDesafio(string DesafioId, DateTime Iniciado, DateTime? Completado);

public record RespuestaQuiz(string PreguntaId, string OpcionId, bool Correcta);

public class IntentoQuiz
{
    public int Id { get; set; }
    public int AlumnoId { get; set; }
    public string DesafioId { get; set; } = "";
    public int Correctas { get; set; }
    public int Total { get; set; }
    public bool Aprobado { get; set; }
    public List<RespuestaQuiz> Respuestas { get; set; } = [];
    public DateTime Fecha { get; set; }
}

public static class TiposNota
{
    public const string Hice = "Hice";
    public const string Falta = "Falta";
    public const string Duda = "Duda";
    public const string Nota = "Nota";
    public static readonly string[] Todos = [Hice, Falta, Duda, Nota];
}

public class NotaBitacora
{
    public int Id { get; set; }
    public int AlumnoId { get; set; }
    public string? DesafioId { get; set; }
    public string Tipo { get; set; } = TiposNota.Nota;
    public string Texto { get; set; } = "";
    public bool Resuelta { get; set; }
    public DateTime Fecha { get; set; }
}

public record InsigniaObtenida(string InsigniaId, DateTime Fecha);
