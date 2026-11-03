using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LaberintoRecursivo;

public enum TipoPaso : byte { Entrar, Retroceder, Camino }

/// <summary>Un paso de la búsqueda, usado para animarla.</summary>
public readonly record struct Paso(int Fila, int Columna, TipoPaso Tipo);

/// <summary>Resultado de resolver un laberinto.</summary>
public class ResultadoBusqueda
{
    public bool Encontrado { get; init; }
    public bool PilaInsuficiente { get; init; }
    public List<(int Fila, int Columna)> Camino { get; init; } = new();
    public long Llamadas { get; init; }
    public int ProfundidadMaxima { get; init; }
    public int Retrocesos { get; init; }
    public double Milisegundos { get; init; }
    public List<Paso> Pasos { get; init; } = new();
}

/// <summary>
/// Búsqueda de la salida del laberinto por retroceso (backtracking):
/// versión recursiva (Tarea 2.3) y versión iterativa con pila explícita (para comparar).
/// </summary>
public static class Resolutores
{
    // Orden fijo de exploración: abajo, derecha, arriba, izquierda.
    private static readonly (int DF, int DC)[] Direcciones = { (1, 0), (0, 1), (-1, 0), (0, -1) };

    // =====================================================================
    //  Versión recursiva
    // =====================================================================

    public static ResultadoBusqueda ResolverRecursivo(Laberinto lab, bool registrarPasos)
    {
        var busqueda = new BusquedaRecursiva(lab, registrarPasos);
        var cronometro = Stopwatch.StartNew();
        bool encontrado = false, pilaInsuficiente = false;

        try
        {
            encontrado = busqueda.Explorar(lab.Inicio.Fila, lab.Inicio.Columna, 1);
        }
        catch (InsufficientExecutionStackException)
        {
            pilaInsuficiente = true;   // la pila del hilo se iba a agotar: se detuvo a tiempo
        }
        cronometro.Stop();

        busqueda.Camino.Reverse();     // el camino se armó desde la salida hacia el inicio
        if (registrarPasos)
        {
            foreach (var (f, c) in busqueda.Camino) busqueda.Pasos.Add(new Paso(f, c, TipoPaso.Camino));
        }

        return new ResultadoBusqueda
        {
            Encontrado = encontrado,
            PilaInsuficiente = pilaInsuficiente,
            Camino = busqueda.Camino,
            Llamadas = busqueda.Llamadas,
            ProfundidadMaxima = busqueda.ProfundidadMaxima,
            Retrocesos = busqueda.Retrocesos,
            Milisegundos = cronometro.Elapsed.TotalMilliseconds,
            Pasos = busqueda.Pasos
        };
    }

    /// <summary>Guarda el estado compartido por todas las llamadas recursivas.</summary>
    private sealed class BusquedaRecursiva
    {
        private readonly Laberinto lab;
        private readonly bool[,] visitado;
        private readonly bool registrar;

        public List<(int, int)> Camino { get; } = new();
        public List<Paso> Pasos { get; } = new();
        public long Llamadas { get; private set; }
        public int ProfundidadMaxima { get; private set; }
        public int Retrocesos { get; private set; }

        public BusquedaRecursiva(Laberinto laberinto, bool registrarPasos)
        {
            lab = laberinto;
            visitado = new bool[lab.Filas, lab.Columnas];
            registrar = registrarPasos;
        }

        /// <summary>Devuelve true si desde (fila, columna) se puede llegar a la salida.</summary>
        public bool Explorar(int fila, int columna, int profundidad)
        {
            Llamadas++;
            if (profundidad > ProfundidadMaxima) ProfundidadMaxima = profundidad;

            // CASO BASE 1: pared o celda ya visitada -> por aquí no hay camino
            if (!lab.EsLibre(fila, columna) || visitado[fila, columna])
                return false;

            // Protección: si la pila del hilo está por agotarse, se detiene de forma controlada
            // en lugar de provocar un StackOverflowException (que no se puede capturar).
            if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
                throw new InsufficientExecutionStackException();

            visitado[fila, columna] = true;
            if (registrar) Pasos.Add(new Paso(fila, columna, TipoPaso.Entrar));

            // CASO BASE 2: se llegó a la salida
            if ((fila, columna) == lab.Fin)
            {
                Camino.Add((fila, columna));
                return true;
            }

            // CASO RECURSIVO: probar cada dirección; si alguna llega a la salida, esta celda es parte del camino
            foreach (var (df, dc) in Direcciones)
            {
                if (Explorar(fila + df, columna + dc, profundidad + 1))
                {
                    Camino.Add((fila, columna));
                    return true;
                }
            }

            // Ninguna dirección funcionó: se retrocede (backtracking)
            Retrocesos++;
            if (registrar) Pasos.Add(new Paso(fila, columna, TipoPaso.Retroceder));
            return false;
        }
    }

    // =====================================================================
    //  Versión iterativa con pila explícita (para comparar)
    // =====================================================================

    public static ResultadoBusqueda ResolverIterativo(Laberinto lab, bool registrarPasos)
    {
        var visitado = new bool[lab.Filas, lab.Columnas];
        var pasos = new List<Paso>();
        var camino = new List<(int, int)>();
        // Cada elemento recuerda la celda y la siguiente dirección por probar (lo que haría el marco de pila).
        var pila = new Stack<(int F, int C, int Dir)>();
        int profundidadMaxima = 1, retrocesos = 0;
        long iteraciones = 0;
        bool encontrado = false;

        var cronometro = Stopwatch.StartNew();
        visitado[lab.Inicio.Fila, lab.Inicio.Columna] = true;
        if (registrarPasos) pasos.Add(new Paso(lab.Inicio.Fila, lab.Inicio.Columna, TipoPaso.Entrar));
        pila.Push((lab.Inicio.Fila, lab.Inicio.Columna, 0));

        while (pila.Count > 0)
        {
            iteraciones++;
            var (f, c, dir) = pila.Pop();

            if ((f, c) == lab.Fin)
            {
                camino.AddRange(pila.Reverse().Select(e => (e.F, e.C)));
                camino.Add((f, c));
                encontrado = true;
                break;
            }

            if (dir == Direcciones.Length)
            {
                retrocesos++;   // se probaron todas las direcciones: la celda se descarta
                if (registrarPasos) pasos.Add(new Paso(f, c, TipoPaso.Retroceder));
                continue;
            }

            pila.Push((f, c, dir + 1));   // la próxima vez se probará la siguiente dirección
            int nf = f + Direcciones[dir].DF, nc = c + Direcciones[dir].DC;
            if (lab.EsLibre(nf, nc) && !visitado[nf, nc])
            {
                visitado[nf, nc] = true;
                if (registrarPasos) pasos.Add(new Paso(nf, nc, TipoPaso.Entrar));
                pila.Push((nf, nc, 0));
                if (pila.Count > profundidadMaxima) profundidadMaxima = pila.Count;
            }
        }
        cronometro.Stop();

        if (registrarPasos)
        {
            foreach (var (f, c) in camino) pasos.Add(new Paso(f, c, TipoPaso.Camino));
        }

        return new ResultadoBusqueda
        {
            Encontrado = encontrado,
            Camino = camino,
            Llamadas = iteraciones,
            ProfundidadMaxima = profundidadMaxima,
            Retrocesos = retrocesos,
            Milisegundos = cronometro.Elapsed.TotalMilliseconds,
            Pasos = pasos
        };
    }
}