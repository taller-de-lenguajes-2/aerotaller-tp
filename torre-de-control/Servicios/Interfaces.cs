public interface IProgresoService
{
    Alumno ObtenerAlumno(int alumnoId);
    List<EtapaMapa> Mapa(int alumnoId);
    ResumenAlumno Resumen(int alumnoId);
    DesafioVista Detalle(int alumnoId, string desafioId);
    void MarcarTarea(int alumnoId, string desafioId, string tareaId, bool marcada);
    ResultadoAccion Completar(int alumnoId, string desafioId);
    ResultadoAccion CompletarCheckpoint(int alumnoId, string desafioId, CheckpointEntrada entrada);
    ResultadoAccion AbrirLectura(int alumnoId, string desafioId, string lecturaId);
}

public interface IQuizService
{
    ResultadoQuiz Corregir(int alumnoId, string desafioId, Dictionary<string, string> respuestas);
}

public interface IInsigniaService
{
    List<InsigniaVista> Listar(int alumnoId);
    List<Insignia> Evaluar(int alumnoId);
}

public interface IVerificadorService
{
    string RutaPorDefecto { get; }
    ResultadoVerificacion Verificar(int alumnoId, string desafioId);
    EstadoRepoGit EstadoRepo(int alumnoId);
}

public interface IBaseAeroTallerService
{
    EstadoBaseAeroTaller Estado(int alumnoId);
    ResultadoCrearBase Crear(int alumnoId);
}
