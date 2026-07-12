using Microsoft.Extensions.Logging;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Features.RegionsTableMap;
using OpenScrape.Features.RegionsTableMap.Update;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;

namespace OpenScrape.App.Forms.Controls;

/// <summary>
/// UserControl que encapsula el editor de regiones de la pestaña de configuración.
/// Gestiona TreeView, PictureBox con rectángulos, test de color/OCR/carta,
/// movimiento y redimensionado de regiones.
/// </summary>
public sealed class RegionEditorControl : UserControl
{
    #region Servicios

    private readonly ICaptureService _captureService;
    private readonly IScreenReaderService _screenReader;
    private readonly OcrService? _ocrService;
    private readonly ImageCropperService _imageCropper;
    private readonly CardCacheService _cardCache;
    private readonly ColorDetectionService _colorDetection;
    private readonly IRegionLookupCache _regionLookup;
    private readonly RegionTableMapUseCases _regionUseCases;
    private readonly ILogger<RegionEditorControl> _logger;

    #endregion

    #region Controles recibidos via Initialize

    private TreeView? _twRegions;
    private PictureBox? _pbImage;
    private TextBox? _tbX, _tbY, _tbWidth, _tbHeight, _tbColor, _tbRegionName;
    private TextBox? _tbRegionUmbral, _tbRegionInactUmbral;
    private Button? _btnSaveMap, _btnTestColor, _btnTestTexto, _btnTestCarta;
    private GroupBox? _rgRegion;
    private CheckBox? _cbRegionColor, _cbRegionHash, _cbRegionBoard, _cbRegionNumber;
    private Button? _btnUp, _btnDown, _btnLeft, _btnRight;
    private Button? _btnUpLeft, _btnUpRight, _btnDownLeft, _btnDownRight;
    private Button? _btnPlusWidth, _btnMinusWidth, _btnPlusHeight, _btnMinusHeight;
    private ComboBox? _cbSpeed;
    private PictureBox? _pbColorDebug, _pbTestCarta, _pictureBox1, _pbRegionColor;
    private TextBox? _tbTestColor, _tbTestTexto;

    #endregion

    #region Estado

    private IReadOnlyList<RegionTableMap>? _categories;
    private DomainRegion? _selectedRegion;
    private List<CardDTO>? _cardsImages;
    private Image? _img;
    private int _speed = 1;

    #endregion

    #region Eventos

    /// <summary>
    /// Se dispara cuando el usuario guarda cambios en una región.
    /// FrmMain debe suscribirse para recargar los datos desde la base de datos.
    /// </summary>
    public event EventHandler? RegionsSaved;

    #endregion

    public RegionEditorControl(
        ICaptureService captureService,
        IScreenReaderService screenReader,
        OcrService? ocrService,
        ImageCropperService imageCropper,
        CardCacheService cardCache,
        ColorDetectionService colorDetection,
        IRegionLookupCache regionLookup,
        RegionTableMapUseCases regionUseCases,
        ILogger<RegionEditorControl> logger)
    {
        _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        _screenReader = screenReader ?? throw new ArgumentNullException(nameof(screenReader));
        _ocrService = ocrService;
        _imageCropper = imageCropper ?? throw new ArgumentNullException(nameof(imageCropper));
        _cardCache = cardCache ?? throw new ArgumentNullException(nameof(cardCache));
        _colorDetection = colorDetection ?? throw new ArgumentNullException(nameof(colorDetection));
        _regionLookup = regionLookup ?? throw new ArgumentNullException(nameof(regionLookup));
        _regionUseCases = regionUseCases ?? throw new ArgumentNullException(nameof(regionUseCases));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Inicializa el control con referencias a los controles de FrmMain.
    /// Debe llamarse una vez después de que los controles estén creados.
    /// </summary>
    public void Initialize(
        TreeView twRegions,
        PictureBox pbImage,
        TextBox tbX, TextBox tbY, TextBox tbWidth, TextBox tbHeight,
        TextBox tbColor, TextBox tbRegionName,
        TextBox tbRegionUmbral, TextBox tbRegionInactUmbral,
        Button btnSaveMap,
        Button btnTestColor, Button btnTestTexto, Button btnTestCarta,
        GroupBox rgRegion,
        CheckBox cbRegionColor, CheckBox cbRegionHash,
        CheckBox cbRegionBoard, CheckBox cbRegionNumber,
        Button btnUp, Button btnDown, Button btnLeft, Button btnRight,
        Button btnUpLeft, Button btnUpRight, Button btnDownLeft, Button btnDownRight,
        Button btnPlusWidth, Button btnMinusWidth, Button btnPlusHeight, Button btnMinusHeight,
        ComboBox cbSpeed,
        PictureBox pbColorDebug, TextBox tbTestColor,
        TextBox tbTestTexto, PictureBox pbTestCarta,
        PictureBox pictureBox1, PictureBox pbRegionColor,
        IReadOnlyList<RegionTableMap>? categories)
    {
        _twRegions = twRegions ?? throw new ArgumentNullException(nameof(twRegions));
        _pbImage = pbImage ?? throw new ArgumentNullException(nameof(pbImage));
        _tbX = tbX ?? throw new ArgumentNullException(nameof(tbX));
        _tbY = tbY ?? throw new ArgumentNullException(nameof(tbY));
        _tbWidth = tbWidth ?? throw new ArgumentNullException(nameof(tbWidth));
        _tbHeight = tbHeight ?? throw new ArgumentNullException(nameof(tbHeight));
        _tbColor = tbColor ?? throw new ArgumentNullException(nameof(tbColor));
        _tbRegionName = tbRegionName ?? throw new ArgumentNullException(nameof(tbRegionName));
        _tbRegionUmbral = tbRegionUmbral ?? throw new ArgumentNullException(nameof(tbRegionUmbral));
        _tbRegionInactUmbral = tbRegionInactUmbral ?? throw new ArgumentNullException(nameof(tbRegionInactUmbral));
        _btnSaveMap = btnSaveMap ?? throw new ArgumentNullException(nameof(btnSaveMap));
        _btnTestColor = btnTestColor ?? throw new ArgumentNullException(nameof(btnTestColor));
        _btnTestTexto = btnTestTexto ?? throw new ArgumentNullException(nameof(btnTestTexto));
        _btnTestCarta = btnTestCarta ?? throw new ArgumentNullException(nameof(btnTestCarta));
        _rgRegion = rgRegion ?? throw new ArgumentNullException(nameof(rgRegion));
        _cbRegionColor = cbRegionColor ?? throw new ArgumentNullException(nameof(cbRegionColor));
        _cbRegionHash = cbRegionHash ?? throw new ArgumentNullException(nameof(cbRegionHash));
        _cbRegionBoard = cbRegionBoard ?? throw new ArgumentNullException(nameof(cbRegionBoard));
        _cbRegionNumber = cbRegionNumber ?? throw new ArgumentNullException(nameof(cbRegionNumber));
        _btnUp = btnUp ?? throw new ArgumentNullException(nameof(btnUp));
        _btnDown = btnDown ?? throw new ArgumentNullException(nameof(btnDown));
        _btnLeft = btnLeft ?? throw new ArgumentNullException(nameof(btnLeft));
        _btnRight = btnRight ?? throw new ArgumentNullException(nameof(btnRight));
        _btnUpLeft = btnUpLeft ?? throw new ArgumentNullException(nameof(btnUpLeft));
        _btnUpRight = btnUpRight ?? throw new ArgumentNullException(nameof(btnUpRight));
        _btnDownLeft = btnDownLeft ?? throw new ArgumentNullException(nameof(btnDownLeft));
        _btnDownRight = btnDownRight ?? throw new ArgumentNullException(nameof(btnDownRight));
        _btnPlusWidth = btnPlusWidth ?? throw new ArgumentNullException(nameof(btnPlusWidth));
        _btnMinusWidth = btnMinusWidth ?? throw new ArgumentNullException(nameof(btnMinusWidth));
        _btnPlusHeight = btnPlusHeight ?? throw new ArgumentNullException(nameof(btnPlusHeight));
        _btnMinusHeight = btnMinusHeight ?? throw new ArgumentNullException(nameof(btnMinusHeight));
        _cbSpeed = cbSpeed ?? throw new ArgumentNullException(nameof(cbSpeed));
        _pbColorDebug = pbColorDebug ?? throw new ArgumentNullException(nameof(pbColorDebug));
        _tbTestColor = tbTestColor ?? throw new ArgumentNullException(nameof(tbTestColor));
        _tbTestTexto = tbTestTexto ?? throw new ArgumentNullException(nameof(tbTestTexto));
        _pbTestCarta = pbTestCarta ?? throw new ArgumentNullException(nameof(pbTestCarta));
        _pictureBox1 = pictureBox1 ?? throw new ArgumentNullException(nameof(pictureBox1));
        _pbRegionColor = pbRegionColor ?? throw new ArgumentNullException(nameof(pbRegionColor));

        SubscribeEvents();
        LoadTreeViewRegions(categories);
    }

    /// <summary>
    /// Carga o recarga el TreeView con las categorías y regiones.
    /// Puede llamarse múltiples veces para refrescar después de guardar.
    /// </summary>
    public void LoadTreeViewRegions(IReadOnlyList<RegionTableMap>? categories)
    {
        _categories = categories;
        _twRegions!.Nodes.Clear();

        if (categories == null)
            return;

        foreach (var category in categories)
        {
            TreeNode regionsConfigNode = _twRegions.Nodes.Add(category.Id);

            if (category.Regions != null)
            {
                foreach (var region in category.Regions)
                {
                    regionsConfigNode.Nodes.Add(region.Name);
                }
            }
        }
    }

    #region Suscripción a eventos

    private void SubscribeEvents()
    {
        _twRegions!.DoubleClick += TwRegions_DoubleClick;
        _btnSaveMap!.Click += BtnSaveMap_Click;
        _btnTestColor!.Click += BtnTestColor_Click;
        _btnTestTexto!.Click += BtnTestTexto_Click;
        _btnTestCarta!.Click += BtnTestCarta_Click;

        _btnPlusWidth!.Click += BtnPlusWidth_Click;
        _btnMinusWidth!.Click += BtnMinusWidth_Click;
        _btnPlusHeight!.Click += BtnPlusHeight_Click;
        _btnMinusHeight!.Click += BtnMinusHeight_Click;

        _btnRight!.Click += BtnRight_Click;
        _btnLeft!.Click += BtnLeft_Click;
        _btnDown!.Click += BtnDown_Click;
        _btnUp!.Click += BtnUp_Click;
        _btnUpLeft!.Click += BtnUpLeft_Click;
        _btnUpRight!.Click += BtnUpRight_Click;
        _btnDownLeft!.Click += BtnDownLeft_Click;
        _btnDownRight!.Click += BtnDownRight_Click;

        _tbWidth!.Leave += TbWidth_Leave;
        _tbHeight!.Leave += TbHeight_Leave;
        _tbX!.Leave += TbX_Leave;
        _tbY!.Leave += TbY_Leave;

        _cbSpeed!.SelectedIndexChanged += CbSpeed_SelectedIndexChanged;
    }

    #endregion

    #region TreeView handlers

    private void TwRegions_DoubleClick(object? sender, EventArgs e)
    {
        try
        {
            EnableButtons();
            _pbImage!.Refresh();

            if (_twRegions?.SelectedNode?.Parent != null)
            {
                foreach (TreeNode rootNode in _twRegions.Nodes)
                {
                    if (rootNode != _twRegions.SelectedNode.Parent)
                    {
                        rootNode.Collapse();
                    }
                }
            }

            if (_twRegions?.SelectedNode != null)
            {
                _selectedRegion = GetRegionFromNode(_twRegions.SelectedNode);
                if (_selectedRegion != null && _pbImage!.Image != null)
                {
                    UpdateRegionDisplay();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting region");
        }
    }

    #endregion

    #region Display

    /// <summary>
    /// Actualiza la visualización de la región seleccionada en el PictureBox y los controles.
    /// </summary>
    private void UpdateRegionDisplay()
    {
        using var lapiz = new Pen(Color.Red);
        using var papel = _pbImage!.CreateGraphics();
        papel.DrawRectangle(lapiz, _selectedRegion!.PosX, _selectedRegion.PosY,
            _selectedRegion.Width, _selectedRegion.Height);

        _tbX!.Text = _selectedRegion.PosX.ToString();
        _tbY!.Text = _selectedRegion.PosY.ToString();
        _tbWidth!.Text = _selectedRegion.Width.ToString();
        _tbHeight!.Text = _selectedRegion.Height.ToString();
        _tbRegionName!.Text = _selectedRegion.Name;
        _tbColor!.Text = _selectedRegion.Color != null ? _selectedRegion.Color.ToUpper() : string.Empty;
        _tbRegionUmbral!.Text = _selectedRegion.Umbral.ToString();
        _tbRegionInactUmbral!.Text = _selectedRegion.InactiveUmbral.ToString();

        _cbRegionColor!.Checked = _selectedRegion.IsColor.GetValueOrDefault();
        _cbRegionHash!.Checked = _selectedRegion.IsHash.GetValueOrDefault();
        _cbRegionBoard!.Checked = _selectedRegion.IsBoard.GetValueOrDefault();
        _cbRegionNumber!.Checked = _selectedRegion.IsOnlyNumber.GetValueOrDefault();

        SetPictureBoxColor(_selectedRegion.Color != null ? _selectedRegion.Color.ToUpper() : string.Empty);

        _btnTestColor!.Enabled = _selectedRegion.IsColor.GetValueOrDefault();
        _btnTestTexto!.Enabled = _selectedRegion.Umbral != null;
        _btnTestCarta!.Enabled = _selectedRegion.IsHash.GetValueOrDefault();
    }

    /// <summary>
    /// Obtiene la región correspondiente al nodo seleccionado en el TreeView.
    /// </summary>
    private DomainRegion? GetRegionFromNode(TreeNode node)
    {
        return _categories?
            .SelectMany(c => c.Regions ?? new List<DomainRegion>())
            .FirstOrDefault(r => r.Name == node.Text);
    }

    /// <summary>
    /// Establece el color de vista previa en el PictureBox de color.
    /// </summary>
    private void SetPictureBoxColor(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor))
            return;

        hexColor = hexColor.Replace("#", "");

        try
        {
            var color = ColorTranslator.FromHtml("#" + hexColor);
            using var bmp = new Bitmap(_pbRegionColor!.Width, _pbRegionColor.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(color);
            }
            _pbRegionColor.Image?.Dispose();
            _pbRegionColor.Image = new Bitmap(bmp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al establecer color");
        }
    }

    #endregion

    #region Botones de prueba

    private void BtnTestColor_Click(object? sender, EventArgs e)
    {
        if (_selectedRegion == null || _pbImage?.Image == null)
            return;

        var color = _colorDetection.GetPixelColor(
            _pbImage.Image,
            _selectedRegion.PosX,
            _selectedRegion.PosY);

        _pbColorDebug!.BackColor = Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
        _tbTestColor!.Text = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
    }

    private void BtnTestTexto_Click(object? sender, EventArgs e)
    {
        if (_selectedRegion == null || _pbImage?.Image == null)
            return;

        var firstOcr = _ocrService.ExtractTextFromRegionAndDebug(
            _pbImage.Image,
            _selectedRegion.PosX, _selectedRegion.PosY,
            _selectedRegion.Width, _selectedRegion.Height,
            _selectedRegion.Umbral ?? 0,
            _selectedRegion.IsOnlyNumber ?? false);

        using var secondOcr = _ocrService.ExtractTextFromRegionAndDebug(
            _pbImage.Image,
            _selectedRegion.PosX, _selectedRegion.PosY,
            _selectedRegion.Width, _selectedRegion.Height,
            _selectedRegion.InactiveUmbral ?? 0,
            _selectedRegion.IsOnlyNumber ?? false);

        var result = string.Empty;

        if (_selectedRegion.IsOnlyNumber == true)
        {
            if (string.IsNullOrEmpty(firstOcr.Text))
                firstOcr.Text = "0";

            if (string.IsNullOrEmpty(secondOcr.Text))
                secondOcr.Text = "0";

            var ocr1 = decimal.TryParse(firstOcr.Text, out var v1) ? v1 : 0m;
            var ocr2 = decimal.TryParse(secondOcr.Text, out var v2) ? v2 : 0m;

            result = ocr2 >= ocr1 ? ocr2.ToString() : ocr1.ToString();
        }

        _tbTestTexto!.Text = !string.IsNullOrEmpty(result) ? result : "Sin resultado";

        _pictureBox1!.Image?.Dispose();
        _pictureBox1.Image = firstOcr.Image;
        firstOcr.Image = null;
        firstOcr.Dispose();
    }

    private async void BtnTestCarta_Click(object? sender, EventArgs e)
    {
        if (_selectedRegion == null || _pbImage?.Image == null)
        {
            _logger.LogError("No se ha seleccionado una región o la imagen es nula.");
            return;
        }

        try
        {
            var imageToBase64 = _imageCropper.CropImageToBase64(
                _pbImage.Image,
                _selectedRegion.PosX,
                _selectedRegion.PosY,
                _selectedRegion.Width,
                _selectedRegion.Height);

            _cardsImages ??= await _cardCache.GetCardsAsync();

            if (_cardsImages?.Any() == true)
            {
                var bestMatch = _cardsImages
                    .Where(item => !string.IsNullOrEmpty(item.ImageBase64))
                    .Select(item => new
                    {
                        Card = item,
                        Percentage = _imageCropper.CompareCardsBase64(item.ImageBase64, imageToBase64)
                    })
                    .OrderByDescending(x => x.Percentage)
                    .FirstOrDefault();

                if (bestMatch != null)
                {
                    _pbTestCarta!.Image?.Dispose();
                    _pbTestCarta.Image = _imageCropper.Base64ToImage(bestMatch.Card.ImageBase64);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al probar carta");
        }
    }

    #endregion

    #region Botón guardar mapa

    private async void BtnSaveMap_Click(object? sender, EventArgs e)
    {
        try
        {
            var umbral = string.IsNullOrEmpty(_tbRegionUmbral!.Text) ? "0" : _tbRegionUmbral.Text;
            var inactUmbral = string.IsNullOrEmpty(_tbRegionInactUmbral!.Text) ? "0" : _tbRegionInactUmbral.Text;

            if (!double.TryParse(umbral, out double regionUmbral) ||
                !double.TryParse(inactUmbral, out double regionInactUmbral) ||
                !int.TryParse(_tbX!.Text, out int x) ||
                !int.TryParse(_tbY!.Text, out int y) ||
                !int.TryParse(_tbWidth!.Text, out int width) ||
                !int.TryParse(_tbHeight!.Text, out int height) ||
                _selectedRegion == null)
            {
                _logger.LogInformation("Error de validación: umbral={Umbral}, inact={Inact}, X={X}, Y={Y}, W={W}, H={H}",
                    umbral, inactUmbral, _tbX.Text, _tbY.Text, _tbWidth.Text, _tbHeight.Text);
                return;
            }

            await _regionUseCases.UpdateRegionTableMap.ExecuteAsync(
                new UpdateRegionTableMapRequest(
                    _selectedRegion.Category,
                    _selectedRegion.Name,
                    x, y, width, height,
                    regionUmbral,
                    regionInactUmbral,
                    _tbColor!.Text,
                    _cbRegionColor!.Checked,
                    _cbRegionHash!.Checked,
                    _cbRegionNumber!.Checked,
                    _cbRegionBoard!.Checked
                )
            );

            _logger.LogInformation("Región actualizada: {Region}", _selectedRegion.Name);

            // Notificar a FrmMain para que recargue los datos
            RegionsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar la región");
        }
    }

    #endregion

    #region Movimiento de región

    private void EnableButtons()
    {
        var buttons = new[]
        {
            _btnPlusHeight, _btnPlusWidth, _btnMinusHeight, _btnMinusWidth,
            _btnUp, _btnUpRight, _btnRight, _btnDownRight, _btnDown,
            _btnDownLeft, _btnLeft, _btnUpLeft
        };

        foreach (var button in buttons)
        {
            if (button != null)
                button.Enabled = true;
        }
    }

    private void BtnPlusWidth_Click(object? sender, EventArgs e) => ResizeRegion(widthDelta: _speed, heightDelta: 0);
    private void BtnMinusWidth_Click(object? sender, EventArgs e) => ResizeRegion(widthDelta: -_speed, heightDelta: 0);
    private void BtnPlusHeight_Click(object? sender, EventArgs e) => ResizeRegion(widthDelta: 0, heightDelta: _speed);
    private void BtnMinusHeight_Click(object? sender, EventArgs e) => ResizeRegion(widthDelta: 0, heightDelta: -_speed);

    private void BtnRight_Click(object? sender, EventArgs e) => MoveRegion(offsetX: _speed, offsetY: 0);
    private void BtnLeft_Click(object? sender, EventArgs e) => MoveRegion(offsetX: -_speed, offsetY: 0);
    private void BtnDown_Click(object? sender, EventArgs e) => MoveRegion(offsetX: 0, offsetY: _speed);
    private void BtnUp_Click(object? sender, EventArgs e) => MoveRegion(offsetX: 0, offsetY: -_speed);
    private void BtnUpLeft_Click(object? sender, EventArgs e) => MoveRegion(offsetX: -_speed, offsetY: -_speed);
    private void BtnUpRight_Click(object? sender, EventArgs e) => MoveRegion(offsetX: _speed, offsetY: -_speed);
    private void BtnDownLeft_Click(object? sender, EventArgs e) => MoveRegion(offsetX: -_speed, offsetY: _speed);
    private void BtnDownRight_Click(object? sender, EventArgs e) => MoveRegion(offsetX: _speed, offsetY: _speed);

    private void ResizeRegion(int widthDelta, int heightDelta)
    {
        if (_selectedRegion == null)
            return;

        _pbImage!.Refresh();

        using var papel = _pbImage.CreateGraphics();
        using var lapiz = new Pen(Color.Red);

        int newWidth = Math.Max(1, _selectedRegion.Width + widthDelta);
        int newHeight = Math.Max(1, _selectedRegion.Height + heightDelta);

        _selectedRegion = _selectedRegion with { Width = newWidth, Height = newHeight };

        if (widthDelta != 0)
            _tbWidth!.Text = _selectedRegion.Width.ToString();
        if (heightDelta != 0)
            _tbHeight!.Text = _selectedRegion.Height.ToString();

        papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY,
            _selectedRegion.Width, _selectedRegion.Height);

        _img = _pbImage.Image;
    }

    private void MoveRegion(int offsetX, int offsetY)
    {
        if (_selectedRegion == null || _pbImage?.Image == null)
            return;

        _pbImage.Refresh();

        using var papel = _pbImage.CreateGraphics();
        using var lapiz = new Pen(Color.Red);

        var rgbResponse = GetColorResponse();

        var updatedRegion = _selectedRegion with
        {
            PosX = _selectedRegion.PosX + offsetX,
            PosY = _selectedRegion.PosY + offsetY
        };

        _tbX!.Text = updatedRegion.PosX.ToString();
        _tbY!.Text = updatedRegion.PosY.ToString();
        papel.DrawRectangle(lapiz, updatedRegion.PosX, updatedRegion.PosY,
            updatedRegion.Width, updatedRegion.Height);

        if (updatedRegion.IsColor.GetValueOrDefault())
        {
            updatedRegion = updatedRegion with
            {
                Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}"
            };
        }

        _selectedRegion = updatedRegion;
        _img = _pbImage.Image;
    }

    private GetRGBColorResponse GetColorResponse()
    {
        if (_selectedRegion == null || _pbImage?.Image == null)
            return new GetRGBColorResponse();

        var rgbRequest = new GetRGBColorRequest
        {
            Image = (Bitmap)_pbImage.Image,
            X = _selectedRegion.PosX,
            Y = _selectedRegion.PosY,
            IsColor = _selectedRegion.IsColor.GetValueOrDefault()
        };

        var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

        if (_selectedRegion.IsColor.GetValueOrDefault())
        {
            _tbColor!.Text = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}";
        }

        return rgbResponse;
    }

    #endregion

    #region TextBox Leave handlers

    private void TbWidth_Leave(object? sender, EventArgs e)
    {
        if (_selectedRegion == null)
            return;

        if (!int.TryParse(_tbWidth!.Text, out int width))
        {
            _tbWidth.Text = _selectedRegion.Width.ToString();
            return;
        }

        _pbImage!.Refresh();
        using var papel = _pbImage.CreateGraphics();
        using var lapiz = new Pen(Color.Red);

        _selectedRegion = _selectedRegion with { Width = Math.Max(1, width) };
        papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY,
            _selectedRegion.Width, _selectedRegion.Height);

        _img = _pbImage.Image;
    }

    private void TbHeight_Leave(object? sender, EventArgs e)
    {
        if (_selectedRegion == null)
            return;

        if (!int.TryParse(_tbHeight!.Text, out int height))
        {
            _tbHeight.Text = _selectedRegion.Height.ToString();
            return;
        }

        _pbImage!.Refresh();
        using var papel = _pbImage.CreateGraphics();
        using var lapiz = new Pen(Color.Red);

        _selectedRegion = _selectedRegion with { Height = Math.Max(1, height) };
        papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY,
            _selectedRegion.Width, _selectedRegion.Height);

        _img = _pbImage.Image;
    }

    private void TbX_Leave(object? sender, EventArgs e)
    {
        if (_selectedRegion == null)
            return;

        if (!int.TryParse(_tbX!.Text, out int posX))
        {
            _tbX.Text = _selectedRegion.PosX.ToString();
            return;
        }

        _pbImage!.Refresh();
        using var papel = _pbImage.CreateGraphics();
        using var lapiz = new Pen(Color.Red);

        _selectedRegion = _selectedRegion with { PosX = posX };
        papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY,
            _selectedRegion.Width, _selectedRegion.Height);

        _img = _pbImage.Image;
    }

    private void TbY_Leave(object? sender, EventArgs e)
    {
        if (_selectedRegion == null)
            return;

        if (!int.TryParse(_tbY!.Text, out int posY))
        {
            _tbY.Text = _selectedRegion.PosY.ToString();
            return;
        }

        _pbImage!.Refresh();
        using var papel = _pbImage.CreateGraphics();
        using var lapiz = new Pen(Color.Red);

        _selectedRegion = _selectedRegion with { PosY = posY };
        papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY,
            _selectedRegion.Width, _selectedRegion.Height);

        _img = _pbImage.Image;
    }

    #endregion

    #region Velocidad

    private void CbSpeed_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (int.TryParse(_cbSpeed!.Text, out var speed))
        {
            _speed = speed;
        }
    }

    #endregion
}
