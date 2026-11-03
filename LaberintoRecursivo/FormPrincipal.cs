using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LaberintoRecursivo
{
    public partial class FormPrincipal : Form
    {
        private const int PilaParaResolver = 64 * 1024 * 1024;   // 64 MB para el hilo de búsqueda

        private static readonly Color ColorPared = Color.FromArgb(40, 44, 52);
        private static readonly Color ColorLibre = Color.White;
        private static readonly Color ColorEnPila = Color.FromArgb(120, 180, 255);   // llamada activa
        private static readonly Color ColorDescartada = Color.FromArgb(255, 205, 150); // se retrocedió
        private static readonly Color ColorCamino = Color.FromArgb(40, 170, 90);
        private static readonly Color ColorInicio = Color.Gold;
        private static readonly Color ColorFin = Color.Crimson;

        private Laberinto? laberinto;
        private Bitmap? lienzo;
        private int pixelesPorCelda = 1;
        private List<Paso> pasosPendientes = new();
        private int indicePaso;

        // ---------- Pestaña 1 ----------
        private readonly NumericUpDown nudTamano = new() { Minimum = 5, Maximum = 320, Value = 20, Width = 90 };
        private readonly NumericUpDown nudSemilla = new() { Minimum = 0, Maximum = 999999, Value = 2026, Width = 90 };
        private readonly Button btnGenerar = new() { Text = "Generar laberinto", Width = 220, Height = 34 };
        private readonly CheckBox chkAnimar = new() { Text = "Animar la búsqueda", Checked = true, AutoSize = true };
        private readonly TrackBar trkVelocidad = new() { Minimum = 1, Maximum = 200, Value = 5, Width = 220, TickStyle = TickStyle.None };
        private readonly Button btnRecursivo = new() { Text = "Resolver (recursivo)", Width = 220, Height = 34 };
        private readonly Button btnIterativo = new() { Text = "Resolver (iterativo, pila explícita)", Width = 220, Height = 34 };
        private readonly Label lblEstadisticas = new() { AutoSize = true, Font = new Font("Consolas", 9.5F), Margin = new Padding(3, 12, 3, 3) };
        private readonly PanelLienzo panelLienzo = new() { Dock = DockStyle.Fill };
        private readonly System.Windows.Forms.Timer temporizador = new() { Interval = 15 };

        // ---------- Pestaña 2 ----------
        private readonly ComboBox cmbPila = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        private readonly NumericUpDown nudSemillaExperimento = new() { Minimum = 0, Maximum = 999999, Value = 2026, Width = 90 };
        private readonly Button btnExperimento = new() { Text = "Ejecutar experimento", AutoSize = true };
        private readonly DataGridView dgvExperimento = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = SystemColors.Window
        };

        public FormPrincipal()
        {
            Text = "Tarea 2.3 - Laberinto resuelto con recursividad (backtracking)";
            Size = new Size(1150, 760);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F);

            var pestanas = new TabControl { Dock = DockStyle.Fill };
            pestanas.TabPages.Add(CrearPestanaLaberinto());
            pestanas.TabPages.Add(CrearPestanaExperimento());
            Controls.Add(pestanas);

            btnGenerar.Click += (s, e) => GenerarLaberinto();
            btnRecursivo.Click += async (s, e) => await ResolverAsync(recursivo: true);
            btnIterativo.Click += async (s, e) => await ResolverAsync(recursivo: false);
            temporizador.Tick += Temporizador_Tick;
            btnExperimento.Click += BtnExperimento_Click;

            GenerarLaberinto();
        }

        // =====================================================================
        //  Construcción de la interfaz
        // =====================================================================

        private TabPage CrearPestanaLaberinto()
        {
            var controles = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            controles.Controls.AddRange(new Control[]
            {
            Etiqueta("Celdas por lado:"), nudTamano,
            Etiqueta("Semilla (mismo número = mismo laberinto):"), nudSemilla,
            btnGenerar,
            chkAnimar, Etiqueta("Velocidad de la animación:"), trkVelocidad,
            btnRecursivo, btnIterativo,
            Leyenda(), lblEstadisticas
            });

            var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tabla.Controls.Add(controles, 0, 0);
            tabla.Controls.Add(panelLienzo, 1, 0);

            var pagina = new TabPage("1. Laberinto") { Padding = new Padding(6) };
            pagina.Controls.Add(tabla);
            return pagina;
        }

        private TabPage CrearPestanaExperimento()
        {
            cmbPila.Items.AddRange(new object[] { "256 KB", "1 MB", "16 MB", "64 MB" });
            cmbPila.SelectedIndex = 1;

            foreach (string columna in new[] { "Celdas por lado", "Cuadrícula", "Celdas libres", "Longitud del camino",
                                            "Llamadas recursivas", "Profundidad máxima", "Retrocesos",
                                            "Tiempo recursivo (ms)", "Tiempo iterativo (ms)", "Estado" })
            {
                dgvExperimento.Columns.Add(columna, columna);
            }
            dgvExperimento.Columns[9].FillWeight = 260;

            var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            barra.Controls.AddRange(new Control[]
            {
            Etiqueta("Tamaño de pila del hilo:"), cmbPila, Etiqueta("Semilla:"), nudSemillaExperimento, btnExperimento
            });

            var nota = Etiqueta("Se resuelven laberintos de 10, 20, 40, 80, 160 y 320 celdas por lado. " +
                                "Si la pila no alcanza, la búsqueda se detiene de forma controlada y se informa la profundidad alcanzada.");
            nota.ForeColor = Color.DimGray;
            nota.MaximumSize = new Size(1050, 0);

            var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tabla.Controls.Add(barra, 0, 0);
            tabla.Controls.Add(nota, 0, 1);
            tabla.Controls.Add(dgvExperimento, 0, 2);

            var pagina = new TabPage("2. Experimento de profundidad") { Padding = new Padding(6) };
            pagina.Controls.Add(tabla);
            return pagina;
        }

        // =====================================================================
        //  Pestaña 1: generar, resolver y animar
        // =====================================================================

        private void GenerarLaberinto()
        {
            temporizador.Stop();
            laberinto = Laberinto.Generar((int)nudTamano.Value, (int)nudSemilla.Value);
            DibujarLaberinto();
            lblEstadisticas.Text = $"Cuadrícula: {laberinto.Filas} × {laberinto.Columnas}{Environment.NewLine}" +
                                   $"Celdas libres: {laberinto.CeldasLibres:N0}";
        }

        private async Task ResolverAsync(bool recursivo)
        {
            if (laberinto is null) return;
            temporizador.Stop();
            DibujarLaberinto();   // se limpia una búsqueda anterior
            HabilitarBotones(false);

            Laberinto lab = laberinto;
            // La búsqueda corre en un hilo con pila amplia para no congelar la ventana ni agotar la pila.
            ResultadoBusqueda r = await Experimento.EjecutarConPila(
                () => recursivo ? Resolutores.ResolverRecursivo(lab, registrarPasos: true)
                                : Resolutores.ResolverIterativo(lab, registrarPasos: true),
                PilaParaResolver);

            string tipo = recursivo ? "Recursivo" : "Iterativo";
            string llamadas = recursivo ? "Llamadas recursivas" : "Iteraciones";
            string profundidad = recursivo ? "Profundidad máxima" : "Tamaño máximo de la pila";
            lblEstadisticas.Text =
                $"Método: {tipo}{Environment.NewLine}" +
                $"Cuadrícula: {lab.Filas} × {lab.Columnas}{Environment.NewLine}" +
                $"{llamadas}: {r.Llamadas:N0}{Environment.NewLine}" +
                $"{profundidad}: {r.ProfundidadMaxima:N0}{Environment.NewLine}" +
                $"Retrocesos: {r.Retrocesos:N0}{Environment.NewLine}" +
                $"Longitud del camino: {r.Camino.Count:N0}{Environment.NewLine}" +
                $"Tiempo (con registro de pasos): {r.Milisegundos:F2} ms{Environment.NewLine}" +
                (r.PilaInsuficiente ? "¡Pila insuficiente!" : (r.Encontrado ? "Salida encontrada" : "Sin solución"));

            pasosPendientes = r.Pasos;
            indicePaso = 0;

            if (chkAnimar.Checked)
            {
                temporizador.Start();   // HabilitarBotones(true) se llama al terminar la animación
            }
            else
            {
                AplicarPasos(pasosPendientes.Count);
                HabilitarBotones(true);
            }
        }

        private void Temporizador_Tick(object? sender, EventArgs e)
        {
            AplicarPasos(trkVelocidad.Value);
            if (indicePaso >= pasosPendientes.Count)
            {
                temporizador.Stop();
                HabilitarBotones(true);
            }
        }

        /// <summary>Pinta los siguientes pasos de la búsqueda sobre el lienzo.</summary>
        private void AplicarPasos(int cantidad)
        {
            if (lienzo is null || laberinto is null) return;

            using (var g = Graphics.FromImage(lienzo))
            using (var enPila = new SolidBrush(ColorEnPila))
            using (var descartada = new SolidBrush(ColorDescartada))
            using (var camino = new SolidBrush(ColorCamino))
            {
                int limite = Math.Min(pasosPendientes.Count, indicePaso + cantidad);
                for (; indicePaso < limite; indicePaso++)
                {
                    Paso p = pasosPendientes[indicePaso];
                    Brush pincel = p.Tipo switch
                    {
                        TipoPaso.Entrar => enPila,
                        TipoPaso.Retroceder => descartada,
                        _ => camino
                    };
                    g.FillRectangle(pincel, p.Columna * pixelesPorCelda, p.Fila * pixelesPorCelda, pixelesPorCelda, pixelesPorCelda);
                }
                PintarExtremos(g);
            }
            panelLienzo.Invalidate();
        }

        /// <summary>Dibuja el laberinto vacío en un mapa de bits.</summary>
        private void DibujarLaberinto()
        {
            if (laberinto is null) return;

            pixelesPorCelda = Math.Max(1, 700 / laberinto.Columnas);
            lienzo?.Dispose();
            lienzo = new Bitmap(laberinto.Columnas * pixelesPorCelda, laberinto.Filas * pixelesPorCelda);

            using (var g = Graphics.FromImage(lienzo))
            using (var libre = new SolidBrush(ColorLibre))
            {
                g.Clear(ColorPared);
                for (int f = 0; f < laberinto.Filas; f++)
                {
                    for (int c = 0; c < laberinto.Columnas; c++)
                    {
                        if (laberinto.EsLibre(f, c))
                            g.FillRectangle(libre, c * pixelesPorCelda, f * pixelesPorCelda, pixelesPorCelda, pixelesPorCelda);
                    }
                }
                PintarExtremos(g);
            }

            panelLienzo.Imagen = lienzo;
            panelLienzo.Invalidate();
        }

        private void PintarExtremos(Graphics g)
        {
            if (laberinto is null) return;
            using var inicio = new SolidBrush(ColorInicio);
            using var fin = new SolidBrush(ColorFin);
            g.FillRectangle(inicio, laberinto.Inicio.Columna * pixelesPorCelda, laberinto.Inicio.Fila * pixelesPorCelda, pixelesPorCelda, pixelesPorCelda);
            g.FillRectangle(fin, laberinto.Fin.Columna * pixelesPorCelda, laberinto.Fin.Fila * pixelesPorCelda, pixelesPorCelda, pixelesPorCelda);
        }

        private void HabilitarBotones(bool habilitar)
        {
            btnGenerar.Enabled = btnRecursivo.Enabled = btnIterativo.Enabled = habilitar;
            nudTamano.Enabled = nudSemilla.Enabled = habilitar;
        }

        // =====================================================================
        //  Pestaña 2: experimento
        // =====================================================================

        private async void BtnExperimento_Click(object? sender, EventArgs e)
        {
            int[] opcionesKB = { 256, 1024, 16 * 1024, 64 * 1024 };
            int bytesPila = opcionesKB[cmbPila.SelectedIndex] * 1024;
            int semilla = (int)nudSemillaExperimento.Value;

            btnExperimento.Enabled = false;
            UseWaitCursor = true;
            dgvExperimento.Rows.Clear();

            try
            {
                List<FilaExperimento> filas = await Experimento.EjecutarConPila(
                    () => Experimento.Ejecutar(Experimento.TamanosPredeterminados, semilla), bytesPila);

                foreach (FilaExperimento f in filas)
                {
                    int indice = dgvExperimento.Rows.Add(f.CeldasPorLado, $"{f.Cuadricula} × {f.Cuadricula}", f.CeldasLibres.ToString("N0"),
                        f.LongitudCamino.ToString("N0"), f.Llamadas.ToString("N0"), f.ProfundidadMaxima.ToString("N0"),
                        f.Retrocesos.ToString("N0"), f.MsRecursivo.ToString("F2"), f.MsIterativo.ToString("F2"), f.Estado);

                    if (f.Estado.StartsWith("Pila"))
                        dgvExperimento.Rows[indice].DefaultCellStyle.BackColor = Color.MistyRose;
                }
            }
            finally
            {
                btnExperimento.Enabled = true;
                UseWaitCursor = false;
            }
        }

        // =====================================================================
        //  Auxiliares
        // =====================================================================

        private static Label Etiqueta(string texto) =>
            new() { Text = texto, AutoSize = true, Margin = new Padding(3, 9, 3, 3) };

        /// <summary>Explica el significado de cada color del laberinto.</summary>
        private static Control Leyenda()
        {
            var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(3, 12, 3, 3) };
            foreach (var (color, texto) in new[]
            {
            (ColorEnPila, "En la pila de llamadas"), (ColorDescartada, "Descartada (retroceso)"),
            (ColorCamino, "Camino a la salida"), (ColorInicio, "Inicio"), (ColorFin, "Salida")
        })
            {
                panel.Controls.Add(new Label
                {
                    Text = "   " + texto,
                    AutoSize = true,
                    Margin = new Padding(0, 2, 0, 2),
                    Image = Muestra(color),
                    ImageAlign = ContentAlignment.MiddleLeft
                });
            }
            return panel;
        }

        private static Bitmap Muestra(Color color)
        {
            var bmp = new Bitmap(14, 14);
            using var g = Graphics.FromImage(bmp);
            g.Clear(color);
            return bmp;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            temporizador.Stop();
            lienzo?.Dispose();
            base.OnFormClosed(e);
        }
    }
}