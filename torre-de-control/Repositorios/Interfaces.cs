public record ConexionDb(string Cadena);

public interface IContenidoRepository
{
    List<Desafio> ListarDesafios();
    Desafio? ObtenerDesafio(string id);
    List<Insignia> ListarInsignias();
    string? ObtenerPagina(string nombre);
}

public interface IAlumnoRepository
{
    List<Alumno> Listar();
    Alumno? Obtener(int id);
    int Insertar(Alumno alumno);
    bool Actualizar(Alumno alumno);
}

public interface IProgresoRepository
{
    Dictionary<string, EstadoDesafio> Estados(int alumnoId);
    void Iniciar(int alumnoId, string desafioId);
    void Completar(int alumnoId, string desafioId);

    List<string> TareasMarcadas(int alumnoId, string desafioId);
    Dictionary<string, int> ContarTareasPorDesafio(int alumnoId);
    void MarcarTarea(int alumnoId, string desafioId, string tareaId, bool marcada);

    List<string> LecturasAbiertas(int alumnoId, string desafioId);
    int ContarLecturas(int alumnoId);
    void AbrirLectura(int alumnoId, string desafioId, string lecturaId);

    void GuardarEstadoRepo(int alumnoId, string desafioId, int sinCommitear, int sinPushear);
    int ContarDesafiosConRepoAlDia(int alumnoId);

    void GuardarVerificacion(int alumnoId, string desafioId, bool aprobado, int chequeos, int fallidos);
    Dictionary<string, UltimaVerificacion> UltimasVerificaciones(int alumnoId);
    int ContarDesafiosVerificados(int alumnoId);
}

public interface IIntentoQuizRepository
{
    List<IntentoQuiz> Listar(int alumnoId);
    List<IntentoQuiz> Listar(int alumnoId, string desafioId);
    int Insertar(IntentoQuiz intento);
}

public interface IBitacoraRepository
{
    List<NotaBitacora> Listar(int alumnoId);
    int Insertar(NotaBitacora nota);
    bool CambiarResuelta(int alumnoId, int id, bool resuelta);
    bool Eliminar(int alumnoId, int id);
}

public interface IInsigniaRepository
{
    List<InsigniaObtenida> Listar(int alumnoId);
    void Otorgar(int alumnoId, string insigniaId);
}
