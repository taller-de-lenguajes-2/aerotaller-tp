// Algo que el alumno pidió y no se puede hacer todavía ("te falta aprobar el cuestionario").
// ManejadorGlobal la convierte en un 400 con el detalle.
public class SolicitudInvalidaException(string mensaje, List<string>? faltantes = null) : Exception(mensaje)
{
    public List<string> Faltantes { get; } = faltantes ?? [];
}
