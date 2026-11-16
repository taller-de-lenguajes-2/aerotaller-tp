// Corrige el cuestionario EN EL SERVIDOR: el navegador nunca recibe la respuesta correcta.
// Ante una respuesta incorrecta devuelve una pista (no la respuesta). Se aprueba con todo correcto, sin límite de intentos.
public class QuizService(
    IContenidoRepository contenido,
    IProgresoService progreso,
    IIntentoQuizRepository intentos,
    IInsigniaService insignias) : IQuizService
{
    public ResultadoQuiz Corregir(int alumnoId, string desafioId, Dictionary<string, string> respuestas)
    {
        progreso.ObtenerAlumno(alumnoId);
        // Detalle valida que exista y que no esté bloqueado
        progreso.Detalle(alumnoId, desafioId);
        Desafio desafio = contenido.ObtenerDesafio(desafioId)!;

        if (desafio.Quiz.Count == 0)
            throw new SolicitudInvalidaException("Este desafío no tiene cuestionario.");

        var detalle = new List<CorreccionPregunta>();
        var guardadas = new List<RespuestaQuiz>();

        foreach (Pregunta pregunta in desafio.Quiz)
        {
            string? elegida = respuestas.GetValueOrDefault(pregunta.Id);
            bool correcta = elegida == pregunta.Correcta;

            string mensaje = correcta ? pregunta.Explicacion
                : elegida is null ? "Pregunta sin responder."
                : pregunta.Pista;

            detalle.Add(new CorreccionPregunta(pregunta.Id, correcta, mensaje));
            guardadas.Add(new RespuestaQuiz(pregunta.Id, elegida ?? "", correcta));
        }

        int correctas = detalle.Count(d => d.Correcta);
        var intento = new IntentoQuiz
        {
            AlumnoId = alumnoId,
            DesafioId = desafioId,
            Correctas = correctas,
            Total = desafio.Quiz.Count,
            Aprobado = correctas == desafio.Quiz.Count,
            Respuestas = guardadas,
            Fecha = DateTime.Now
        };
        intentos.Insertar(intento);
        int numero = intentos.Listar(alumnoId, desafioId).Count;

        return new ResultadoQuiz(intento.Aprobado, correctas, intento.Total, numero, detalle, insignias.Evaluar(alumnoId));
    }
}
