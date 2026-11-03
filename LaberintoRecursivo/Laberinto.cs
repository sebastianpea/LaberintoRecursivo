namespace LaberintoRecursivo;

/// <summary>
/// Laberinto perfecto (un único camino entre dos celdas) representado como una cuadrícula.
/// Las celdas en coordenadas impares son habitaciones; las demás son paredes o pasillos.
/// </summary>
public class Laberinto
{
    private readonly bool[,] libre;

    public int Filas { get; }
    public int Columnas { get; }
    public int CeldasLibres { get; private set; }
    public (int Fila, int Columna) Inicio => (1, 1);
    public (int Fila, int Columna) Fin => (Filas - 2, Columnas - 2);

    private Laberinto(int celdasPorLado)
    {
        Filas = Columnas = 2 * celdasPorLado + 1;
        libre = new bool[Filas, Columnas];
    }

    public bool EsLibre(int fila, int columna) => libre[fila, columna];

    /// <summary>
    /// Genera el laberinto con búsqueda en profundidad y una pila explícita.
    /// La generación es iterativa a propósito: así el experimento mide solo la recursión de la búsqueda.
    /// </summary>
    public static Laberinto Generar(int celdasPorLado, int semilla)
    {
        var lab = new Laberinto(celdasPorLado);
        var aleatorio = new Random(semilla);
        var pila = new Stack<(int F, int C)>();
        var vecinos = new List<(int DF, int DC)>(4);
        (int DF, int DC)[] saltos = { (-2, 0), (2, 0), (0, -2), (0, 2) };

        lab.Abrir(1, 1);
        pila.Push((1, 1));

        while (pila.Count > 0)
        {
            var (f, c) = pila.Peek();

            vecinos.Clear();
            foreach (var (df, dc) in saltos)
            {
                int nf = f + df, nc = c + dc;
                if (nf > 0 && nf < lab.Filas - 1 && nc > 0 && nc < lab.Columnas - 1 && !lab.libre[nf, nc])
                    vecinos.Add((df, dc));
            }

            if (vecinos.Count == 0)
            {
                pila.Pop();   // sin vecinos nuevos: se regresa a la celda anterior
                continue;
            }

            var (sf, sc) = vecinos[aleatorio.Next(vecinos.Count)];
            lab.Abrir(f + sf / 2, c + sc / 2);   // se derriba la pared intermedia
            lab.Abrir(f + sf, c + sc);
            pila.Push((f + sf, c + sc));
        }
        return lab;
    }

    private void Abrir(int fila, int columna)
    {
        libre[fila, columna] = true;
        CeldasLibres++;
    }
}