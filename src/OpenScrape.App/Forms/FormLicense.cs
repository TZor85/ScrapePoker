using OpenScrape.App.Services.License;

namespace OpenScrape.App.Forms;

/// <summary>
/// Activación de la licencia. La key se valida contra la API y se guarda cifrada;
/// nunca se escribe en el log.
/// </summary>
public sealed class FormLicense : Form
{
    private readonly LicenseClient _client;
    private readonly LicenseTokenStore _store;

    private readonly TextBox _keyBox = new()
    {
        Dock = DockStyle.Top,
        PlaceholderText = "OSPK-XXXX-XXXX-XXXX",
        MaxLength = 19
    };

    private readonly Button _validateButton = new()
    {
        Text = "Validar",
        Dock = DockStyle.Top,
        Height = 32
    };

    private readonly Label _status = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.DimGray
    };

    public FormLicense(LicenseClient client, LicenseTokenStore store)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _store = store ?? throw new ArgumentNullException(nameof(store));

        Text = "Licencia de OpenScrape";
        Width = 420;
        Height = 190;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label { Text = "Clave de licencia", Dock = DockStyle.Fill });
        layout.Controls.Add(_keyBox);
        layout.Controls.Add(_validateButton);
        layout.Controls.Add(_status);

        Controls.Add(layout);

        var stored = _store.Load();
        if (stored is not null)
        {
            _keyBox.Text = stored.Key;
            _status.Text = $"Plan {stored.Plan}, vence {stored.ExpiresAt:yyyy-MM-dd HH:mm}.";
        }

        _validateButton.Click += ValidateAsync;
    }

    private async void ValidateAsync(object? sender, EventArgs e)
    {
        _validateButton.Enabled = false;
        _status.Text = "Validando...";

        try
        {
            var stored = await _client.ActivateAsync(_keyBox.Text);
            _store.Save(stored);

            _status.Text = $"Licencia {stored.Plan} válida hasta {stored.ExpiresAt:yyyy-MM-dd HH:mm}.";
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (LicenseRequiredException ex)
        {
            _status.Text = ex.Message;
        }
        catch (Exception ex)
        {
            _status.Text = $"No se pudo validar: {ex.Message}";
        }
        finally
        {
            _validateButton.Enabled = true;
        }
    }
}
