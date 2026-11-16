using System.Globalization;

// Las fechas se guardan como texto 'AAAA-MM-DD HH:MM:SS' (igual criterio que en el TP)
public static class Fechas
{
    public const string Formato = "yyyy-MM-dd HH:mm:ss";

    public static string ATexto(DateTime fecha) => fecha.ToString(Formato, CultureInfo.InvariantCulture);

    public static DateTime DeTexto(string texto) => DateTime.ParseExact(texto, Formato, CultureInfo.InvariantCulture);

    public static string Ahora() => ATexto(DateTime.Now);
}
