using OpenScrape.App.Helpers;
using OpenScrape.Domain.ValueObjects;
using System.Drawing;
using System.Drawing.Imaging;
using Region = OpenScrape.Domain.ValueObjects.Region;
using Timer = System.Windows.Forms.Timer;

namespace OpenScrape.App.Forms;

/// <summary>
/// Formulario de debug para calibración de detección de turnos del Hero
/// </summary>
public partial class FrmDetectionDebug : Form
{
    private IntPtr _pokerWindowHandle;
    private Region? _currentRegion;
    private Timer _refreshTimer;
    private bool _isCapturing = false;
    private List<Color> _capturedColors = new();
    private Point _currentCoordinates;
    private const int ZOOM_SIZE = 100; // Tamaño del área ampliada
    private const int ZOOM_FACTOR = 10; // Factor de ampliación

    public FrmDetectionDebug()
    {
        InitializeComponent();
        InitializeTimer();
        SetupEventHandlers();
    }

    public FrmDetectionDebug(IntPtr pokerWindowHandle, Region region) : this()
    {
        _pokerWindowHandle = pokerWindowHandle;
        _currentRegion = region;
        _currentCoordinates = new Point(region.PosX, region.PosY);

        // Configurar la ventana
        this.Text = $"Debug de Detección - Región: {region.Name}";
        lblRegionName.Text = $"Región: {region.Name}";
        lblCoordinates.Text = $"Coordenadas: ({_currentCoordinates.X}, {_currentCoordinates.Y})";
    }

    private void InitializeTimer()
    {
        _refreshTimer = new Timer();
        _refreshTimer.Interval = 100; // Actualizar cada 100ms
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    private void SetupEventHandlers()
    {
        this.KeyPreview = true;
        this.KeyDown += FrmDetectionDebug_KeyDown;

        btnStartCapture.Click += BtnStartCapture_Click;
        btnStopCapture.Click += BtnStopCapture_Click;
        btnSaveColor.Click += BtnSaveColor_Click;
        btnClearColors.Click += BtnClearColors_Click;
        btnSaveCoordinates.Click += BtnSaveCoordinates_Click;

        // Configurar controles numéricos para coordenadas
        numX.ValueChanged += NumCoordinates_ValueChanged;
        numY.ValueChanged += NumCoordinates_ValueChanged;

        // Configurar valores iniciales
        if (_currentRegion != null)
        {
            numX.Value = _currentRegion.PosX;
            numY.Value = _currentRegion.PosY;
        }
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (_pokerWindowHandle == IntPtr.Zero || !_isCapturing)
            return;

        try
        {
            UpdateDetectionInfo();
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Error: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
        }
    }

    private void UpdateDetectionInfo()
    {
        if (_pokerWindowHandle == IntPtr.Zero)
        {
            lblStatus.Text = "Handle de ventana inválido";
            lblStatus.ForeColor = Color.Red;
            return;
        }

        try
        {
            // Capturar imagen de la ventana del poker
            var img = CaptureWindowsHelper.CaptureWindow(_pokerWindowHandle);

            if (img == null)
            {
                lblStatus.Text = "Error al capturar ventana";
                lblStatus.ForeColor = Color.Red;
                return;
            }

            using (img)
            using (var bitmap = new Bitmap(img))
            {
                // Validar coordenadas
                if (_currentCoordinates.X >= bitmap.Width || _currentCoordinates.Y >= bitmap.Height ||
                    _currentCoordinates.X < 0 || _currentCoordinates.Y < 0)
                {
                    lblStatus.Text = "Coordenadas fuera de rango";
                    lblStatus.ForeColor = Color.Red;
                    return;
                }

                // Obtener color del píxel actual
                Color currentColor = bitmap.GetPixel(_currentCoordinates.X, _currentCoordinates.Y);

                // Actualizar información de color
                lblCurrentColor.Text = $"Color Actual: R={currentColor.R}, G={currentColor.G}, B={currentColor.B}";
                lblCurrentColorHex.Text = $"Hex: #{currentColor.R:X2}{currentColor.G:X2}{currentColor.B:X2}";

                // Mostrar el color actual
                pnlCurrentColor.BackColor = currentColor;

                // Verificar si coincide con el valor esperado (B=24)
                bool isExpectedColor = currentColor.B == 24;
                lblDetectionStatus.Text = isExpectedColor ? "[SI] Color Detectado (B=24)" : "[NO] Color No Detectado";
                lblDetectionStatus.ForeColor = isExpectedColor ? Color.Green : Color.Red;

                // Crear imagen ampliada del área alrededor del píxel
                CreateZoomedImage(bitmap);

                // Actualizar estadísticas
                UpdateColorStatistics(currentColor);
            }

            lblStatus.Text = "Capturando...";
            lblStatus.ForeColor = Color.Green;
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Error: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
            System.Diagnostics.Debug.WriteLine($"Error en UpdateDetectionInfo: {ex}");
        }
    }

    private void CreateZoomedImage(Bitmap sourceBitmap)
    {
        // Calcular área a ampliar
        int halfZoom = ZOOM_SIZE / (2 * ZOOM_FACTOR);
        int startX = Math.Max(0, _currentCoordinates.X - halfZoom);
        int startY = Math.Max(0, _currentCoordinates.Y - halfZoom);
        int endX = Math.Min(sourceBitmap.Width, _currentCoordinates.X + halfZoom);
        int endY = Math.Min(sourceBitmap.Height, _currentCoordinates.Y + halfZoom);

        // Crear imagen ampliada
        var zoomedBitmap = new Bitmap(ZOOM_SIZE, ZOOM_SIZE);
        using (var g = Graphics.FromImage(zoomedBitmap))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            var sourceRect = new Rectangle(startX, startY, endX - startX, endY - startY);
            var destRect = new Rectangle(0, 0, ZOOM_SIZE, ZOOM_SIZE);

            g.DrawImage(sourceBitmap, destRect, sourceRect, GraphicsUnit.Pixel);

            // Dibujar cruz en el centro para marcar el píxel exacto
            using (var pen = new Pen(Color.Red, 2))
            {
                int centerX = ZOOM_SIZE / 2;
                int centerY = ZOOM_SIZE / 2;
                g.DrawLine(pen, centerX - 5, centerY, centerX + 5, centerY);
                g.DrawLine(pen, centerX, centerY - 5, centerX, centerY + 5);
            }
        }

        // Actualizar PictureBox
        if (pbZoomedArea.Image != null)
            pbZoomedArea.Image.Dispose();

        pbZoomedArea.Image = zoomedBitmap;
    }

    private void UpdateColorStatistics(Color color)
    {
        // Agregar color a la lista de colores capturados
        _capturedColors.Add(color);

        // Mantener solo los últimos 100 colores para estadísticas
        if (_capturedColors.Count > 100)
            _capturedColors.RemoveAt(0);

        // Calcular estadísticas
        if (_capturedColors.Count > 0)
        {
            var avgR = _capturedColors.Average(c => c.R);
            var avgG = _capturedColors.Average(c => c.G);
            var avgB = _capturedColors.Average(c => c.B);

            var minB = _capturedColors.Min(c => c.B);
            var maxB = _capturedColors.Max(c => c.B);

            lblColorStats.Text = $"Promedio: R={avgR:F1}, G={avgG:F1}, B={avgB:F1}\n" +
                               $"Rango B: {minB}-{maxB}\n" +
                               $"Muestras: {_capturedColors.Count}";
        }
    }

    private void FrmDetectionDebug_KeyDown(object? sender, KeyEventArgs e)
    {
        int step = e.Control ? 10 : 1; // Paso más grande con Ctrl

        switch (e.KeyCode)
        {
            case Keys.Left:
                _currentCoordinates.X = Math.Max(0, _currentCoordinates.X - step);
                break;
            case Keys.Right:
                _currentCoordinates.X += step;
                break;
            case Keys.Up:
                _currentCoordinates.Y = Math.Max(0, _currentCoordinates.Y - step);
                break;
            case Keys.Down:
                _currentCoordinates.Y += step;
                break;
            case Keys.Space:
                BtnSaveColor_Click(sender, e);
                break;
            case Keys.Enter:
                BtnSaveCoordinates_Click(sender, e);
                break;
        }

        // Actualizar controles numéricos
        numX.Value = _currentCoordinates.X;
        numY.Value = _currentCoordinates.Y;
        lblCoordinates.Text = $"Coordenadas: ({_currentCoordinates.X}, {_currentCoordinates.Y})";
    }

    private void NumCoordinates_ValueChanged(object? sender, EventArgs e)
    {
        _currentCoordinates.X = (int)numX.Value;
        _currentCoordinates.Y = (int)numY.Value;
        lblCoordinates.Text = $"Coordenadas: ({_currentCoordinates.X}, {_currentCoordinates.Y})";
    }

    private void BtnStartCapture_Click(object? sender, EventArgs e)
    {
        _isCapturing = true;
        _refreshTimer.Start();
        btnStartCapture.Enabled = false;
        btnStopCapture.Enabled = true;
        lblStatus.Text = "Iniciando captura...";
        lblStatus.ForeColor = Color.Blue;
    }

    private void BtnStopCapture_Click(object? sender, EventArgs e)
    {
        _isCapturing = false;
        _refreshTimer.Stop();
        btnStartCapture.Enabled = true;
        btnStopCapture.Enabled = false;
        lblStatus.Text = "Captura detenida";
        lblStatus.ForeColor = Color.Gray;
    }

    private void BtnSaveColor_Click(object? sender, EventArgs e)
    {
        if (_capturedColors.Count == 0)
        {
            MessageBox.Show("No hay colores capturados para guardar.", "Advertencia",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var lastColor = _capturedColors.Last();
        var result = MessageBox.Show(
            $"¿Guardar este color como el color de detección?\n\n" +
            $"R={lastColor.R}, G={lastColor.G}, B={lastColor.B}\n" +
            $"Hex: #{lastColor.R:X2}{lastColor.G:X2}{lastColor.B:X2}",
            "Confirmar Color",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            // Aquí se implementará la lógica para guardar el color en la configuración
            SaveColorToConfiguration(lastColor);
        }
    }

    private void BtnClearColors_Click(object? sender, EventArgs e)
    {
        _capturedColors.Clear();
        lblColorStats.Text = "Estadísticas limpias";
    }

    private void BtnSaveCoordinates_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            $"¿Guardar estas coordenadas?\n\n" +
            $"X: {_currentCoordinates.X}\n" +
            $"Y: {_currentCoordinates.Y}",
            "Confirmar Coordenadas",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            // Aquí se implementará la lógica para guardar las coordenadas
            SaveCoordinatesToConfiguration(_currentCoordinates);
        }
    }

    private void SaveColorToConfiguration(Color color)
    {
        try
        {
            // TODO: Implementar guardado en base de datos
            var colorHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            // Por ahora, mostrar en un archivo de log
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Color guardado: {colorHex} (R={color.R}, G={color.G}, B={color.B})";
            File.AppendAllText("detection_calibration.log", logEntry + Environment.NewLine);

            MessageBox.Show($"Color guardado: {colorHex}", "Éxito",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar color: {ex.Message}", "Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveCoordinatesToConfiguration(Point coordinates)
    {
        try
        {
            // TODO: Implementar guardado en base de datos

            // Por ahora, mostrar en un archivo de log
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Coordenadas guardadas: X={coordinates.X}, Y={coordinates.Y}";
            File.AppendAllText("detection_calibration.log", logEntry + Environment.NewLine);

            MessageBox.Show($"Coordenadas guardadas: ({coordinates.X}, {coordinates.Y})", "Éxito",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar coordenadas: {ex.Message}", "Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


}