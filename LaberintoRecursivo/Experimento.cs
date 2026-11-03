namespace LaberintoRecursivo;

/// <summary>Una fila de resultados del experimento de profundidad.</summary>
public record FilaExperimento(
    int CeldasPorLado, int Cuadricula, int CeldasLibres, int LongitudCamino,
    long Llamadas, int ProfundidadMaxima, int Retrocesos,
    double MsRecursivo, double MsIterativo, string Estado);

/// <summary>
/// Resuelve laberintos de distintos tamaños para observar cómo crecen la profundidad
/// de la recursión, el número de llamadas y el tiempo, con un tamaño de pila elegido.
/// </summary>
public static class Experimento
{
    public static readonly int[] TamanosPredeterminados = { 10, 20, 40, 80, 160, 320 };

    /// <summary>
    /// Ejecuta un trabajo en un hilo nuevo con el tamaño de pila indicado (en bytes).
    /// Así se puede controlar cuánta recursión admite la búsqueda.
    /// </summary>
    public static Task<T> EjecutarConPila<T>(Func<T> trabajo, int bytesDePila)
    {
        var tarea = new TaskCompletionSource<T>();
        var hilo = new Thread(() =>
        {
            try { tarea.SetResult(trabajo()); }
            catch (Exception ex) { tarea.SetException(ex); }
        }, bytesDePila)
        { IsBackground = true };

        hilo.Start();
        return tarea.Task;
    }

    /// <summary>Resuelve cada tamaño con la versión recursiva y con la iterativa.</summary>
    public static List<FilaExperimento> Ejecutar(int[] tamanos, int semilla)
    {
        // Calentamiento: la primera ejecución incluye la compilación JIT.
        var pequeno = Laberinto.Generar(5, semilla);
        Resolutores.ResolverRecursivo(pequeno, false);
        Resolutores.ResolverIterativo(pequeno, false);

        var filas = new List<FilaExperimento>();
        foreach (int n in tamanos)
        {
            var lab = Laberinto.Generar(n, semilla);
            var rec = Resolutores.ResolverRecursivo(lab, registrarPasos: false);
            var ite = Resolutores.ResolverIterativo(lab, registrarPasos: false);

            string estado = rec.PilaInsuficiente
                ? $"Pila insuficiente (se detuvo en profundidad {rec.ProfundidadMaxima:N0})"
                : (rec.Encontrado ? "Resuelto" : "Sin solución");

            filas.Add(new FilaExperimento(n, lab.Filas, lab.CeldasLibres, ite.Camino.Count,
                rec.Llamadas, rec.ProfundidadMaxima, rec.Retrocesos,
                rec.Milisegundos, ite.Milisegundos, estado));
        }
        return filas;
    }
}