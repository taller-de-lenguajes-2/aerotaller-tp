// Un chequeo: qué se revisó, si pasó y una explicación para el alumno (sobre todo cuando falla)
public record Chequeo(string Nombre, bool Ok, string Detalle = "");

public record ResultadoDesafio(int Numero, string Titulo, List<Chequeo> Chequeos)
{
    public bool Ok => Chequeos.Count > 0 && Chequeos.All(c => c.Ok);
}

// Para cortar un desafío cuando algo básico falla (por ejemplo, la app no arranca)
public class VerificacionAbortada(string mensaje) : Exception(mensaje);

// Acumula los chequeos de un desafío. Cada chequeo corre aislado: si explota, queda en rojo con el error y se sigue.
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
