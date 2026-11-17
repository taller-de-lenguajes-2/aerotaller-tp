// Un chequeo: qué se revisó, si se aprobó y una explicación para el alumno (sobre todo cuando falla)
public record Chequeo(string Nombre, bool Ok, string Detalle = "");

public record ResultadoDesafio(int Numero, string Titulo, List<Chequeo> Chequeos)
{
    public bool Ok => Chequeos.Count > 0 && Chequeos.All(c => c.Ok);
}

// Interrumpe la verificación de un desafío cuando falla un requisito básico (por ejemplo, la aplicación no inicia)
public class VerificacionAbortada(string mensaje) : Exception(mensaje);

// Acumula los chequeos de un desafío. Cada chequeo se ejecuta de forma aislada: si lanza una excepción, se marca
// como no aprobado con el error y la verificación continúa.
public class Informe
{
    public List<Chequeo> Chequeos { get; } = [];

    public void Agregar(string nombre, bool ok, string detalleSiFalla = "", string detalleSiPasa = "") =>
        Chequeos.Add(new Chequeo(nombre, ok, ok ? detalleSiPasa : detalleSiFalla));

    public void Probar(string nombre, Func<(bool Ok, string Detalle)> chequeo)
    {
        try
        {
            (bool ok, string detalle) = chequeo();
            Chequeos.Add(new Chequeo(nombre, ok, detalle));
        }
        catch (VerificacionAbortada)
        {
            throw;
        }
        catch (Exception ex)
        {
            Chequeos.Add(new Chequeo(nombre, false, $"{ex.GetType().Name}: {ex.Message}"));
        }
    }
}
