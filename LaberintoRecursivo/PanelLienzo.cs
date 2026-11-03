using System.Drawing.Drawing2D;

namespace LaberintoRecursivo;

/// <summary>
/// Panel que muestra un mapa de bits escalado sin suavizado, para que cada celda
/// del laberinto se vea como un cuadro nítido. Usa doble búfer para evitar parpadeo.
/// </summary>
public class PanelLienzo : Panel
{
    public Bitmap? Imagen { get; set; }

    public PanelLienzo()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.WhiteSmoke;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Imagen is null) return;

        // Escala que conserva la proporción y centra la imagen.
        float escala = Math.Min((float)ClientSize.Width / Imagen.Width, (float)ClientSize.Height / Imagen.Height);
        int ancho = (int)(Imagen.Width * escala), alto = (int)(Imagen.Height * escala);
        int x = (ClientSize.Width - ancho) / 2, y = (ClientSize.Height - alto) / 2;

        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.DrawImage(Imagen, new Rectangle(x, y, ancho, alto));
    }
}