using System.Drawing;
using OpenScrape.App.Helpers;

namespace OpenScrape.App.Services;

/// <summary>
/// Implementación de <see cref="IThemeService"/> que aplica estilos visuales
/// modernos a los controles WinForms del formulario principal. Cada método
/// recibe los controles que necesita por parámetro y opera exclusivamente
/// sobre ellos, sin referencias al estado de FrmMain.
/// </summary>
public class ThemeService : IThemeService
{
    public void InitializeVisualStyles(Form form)
    {
        form.SuspendLayout();

        form.BackColor = AppThemeHelper.BackgroundMain;
        form.Font = new Font("Segoe UI", 9F);

        // NOTA: SetStyle (DoubleBuffer) es protected y debe llamarse
        // desde FrmMain directamente, no desde este servicio.

        form.ResumeLayout(true);
    }

    public void ApplyModernTabStyle(TabControl tabControl)
    {
        tabControl.SuspendLayout();

        tabControl.Appearance = TabAppearance.FlatButtons;
        tabControl.BackColor = AppThemeHelper.BackgroundMain;
        tabControl.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        foreach (TabPage tab in tabControl.TabPages)
        {
            tab.BackColor = AppThemeHelper.BackgroundMain;
            tab.Padding = new Padding(10);

            switch (tab.Name)
            {
                case "tbJuego":
                    ApplyGameTabStyle(tab);
                    break;
                case "tbConfig":
                    ApplyConfigTabStyle(tab);
                    break;
                case "tbTables":
                    ApplyTablesTabStyle(tab);
                    break;
                case "tbLogs":
                    ApplyLogsTabStyle(tab);
                    break;
                case "tpHistorial":
                    ApplyHistorialTabStyle(tab);
                    break;
                case "tpBankroll":
                    ApplyBankrollTabStyle(tab);
                    break;
            }
        }

        tabControl.ResumeLayout(true);
    }

    public void ApplyGameTabStyle(TabPage gameTab)
    {
        gameTab.SuspendLayout();

        // Mejorar visualización de cartas
        var cardNames = new[] { "pbHeroCard0", "pbHeroCard1", "pbBoard1", "pbBoard2", "pbBoard3", "pbBoard4", "pbBoard5" };
        foreach (var name in cardNames)
        {
            var pb = gameTab.Controls.Find(name, true).FirstOrDefault() as PictureBox;
            if (pb == null) continue;

            pb.BackColor = AppThemeHelper.BackgroundCard;
            pb.BorderStyle = BorderStyle.None;
            pb.SizeMode = PictureBoxSizeMode.StretchImage;

            // Agregar borde redondeado visual
            pb.Paint += (s, e) =>
            {
                using var pen = new Pen(AppThemeHelper.BorderLight, 2f);
                e.Graphics.DrawRectangle(pen, 0, 0, pb.Width - 1, pb.Height - 1);
            };
        }

        // Mejorar paneles de información
        var panelNames = new[]
        {
            "pnNamePlayerFive", "panel2", "panel3", "panel4", "panel5",
            "panel6", "panel7", "panel8", "panel9", "panel10",
            "pnNameHero", "pnNamePlayerOne", "pnNamePlayerTwo",
            "pnNamePlayerThree", "pnNamePlayerFour"
        };
        foreach (var name in panelNames)
        {
            var panel = gameTab.Controls.Find(name, true).FirstOrDefault() as Panel;
            if (panel == null) continue;

            panel.BackColor = AppThemeHelper.BackgroundCard;
            panel.BorderStyle = BorderStyle.None;
            panel.Padding = new Padding(5);

            // Sombra sutil
            panel.Paint += (s, e) =>
            {
                using var brush = new SolidBrush(AppThemeHelper.BorderLight);
                e.Graphics.FillRectangle(brush, 0, panel.Height - 1, panel.Width, 1);
                e.Graphics.FillRectangle(brush, panel.Width - 1, 0, 1, panel.Height);
            };
        }

        // Mejorar botones de acción
        var buttonNames = new[] { "btnCapture", "btnWindow" };
        foreach (var name in buttonNames)
        {
            var btn = gameTab.Controls.Find(name, true).FirstOrDefault() as Button;
            if (btn == null) continue;

            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = AppThemeHelper.Accent;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            btn.Cursor = Cursors.Hand;

            // Efecto hover
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(0, 100, 160);
            btn.MouseLeave += (s, e) => btn.BackColor = AppThemeHelper.Accent;
        }

        // Mejorar label de acción principal
        var lbAction = gameTab.Controls.Find("lbAction", true).FirstOrDefault() as Label;
        if (lbAction != null)
        {
            lbAction.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lbAction.ForeColor = AppThemeHelper.PrimaryDark;
            lbAction.BackColor = Color.Transparent;
        }

        gameTab.ResumeLayout(true);
    }

    public void ApplyConfigTabStyle(TabPage configTab)
    {
        configTab.SuspendLayout();

        // Mejorar TreeView de regiones
        var twRegionsConfig = configTab.Controls.Find("twRegionsConfig", true).FirstOrDefault() as TreeView;
        if (twRegionsConfig != null)
        {
            twRegionsConfig.BackColor = AppThemeHelper.BackgroundCard;
            twRegionsConfig.BorderStyle = BorderStyle.None;
            twRegionsConfig.Font = new Font("Segoe UI", 9F);
            twRegionsConfig.ForeColor = AppThemeHelper.PrimaryDark;
            twRegionsConfig.LineColor = AppThemeHelper.BorderLight;
        }

        // Mejorar GroupBox de región
        var rgRegion = configTab.Controls.Find("rgRegion", true).FirstOrDefault() as GroupBox;
        if (rgRegion != null)
        {
            rgRegion.BackColor = AppThemeHelper.BackgroundCard;
            rgRegion.ForeColor = AppThemeHelper.PrimaryDark;
            rgRegion.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        }

        // Mejorar controles de movimiento
        var movementNames = new[] { "btnUp", "btnDown", "btnLeft", "btnRight", "btnUpLeft", "btnUpRight", "btnDownLeft", "btnDownRight" };
        foreach (var name in movementNames)
        {
            var btn = configTab.Controls.Find(name, true).FirstOrDefault() as Button;
            if (btn == null) continue;

            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = AppThemeHelper.BorderLight;
            btn.BackColor = AppThemeHelper.BackgroundCard;
            btn.ForeColor = AppThemeHelper.PrimaryDark;
            btn.Font = new Font("Segoe UI", 10F);
            btn.Cursor = Cursors.Hand;

            // Efecto hover
            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = AppThemeHelper.PrimaryLight;
                btn.ForeColor = Color.White;
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = AppThemeHelper.BackgroundCard;
                btn.ForeColor = AppThemeHelper.PrimaryDark;
            };
        }

        // Mejorar botones de tamaño
        var sizeNames = new[] { "btnPlusWidth", "btnMinusWidth", "btnPlusHeight", "btnMinusHeight" };
        foreach (var name in sizeNames)
        {
            var btn = configTab.Controls.Find(name, true).FirstOrDefault() as Button;
            if (btn == null) continue;

            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = AppThemeHelper.Accent;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
        }

        // Mejorar TextBoxes
        var textBoxNames = new[] { "tbX", "tbY", "tbWidth", "tbHeight", "tbColor", "tbRegionUmbral", "tbRegionInactUmbral" };
        foreach (var name in textBoxNames)
        {
            var tb = configTab.Controls.Find(name, true).FirstOrDefault() as TextBox;
            if (tb == null) continue;

            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.BackColor = AppThemeHelper.BackgroundCard;
            tb.ForeColor = AppThemeHelper.PrimaryDark;
            tb.Font = new Font("Segoe UI", 9F);
        }

        // Mejorar CheckBoxes
        var checkBoxNames = new[] { "cbRegionColor", "cbRegionHash", "cbRegionBoard", "cbRegionNumber" };
        foreach (var name in checkBoxNames)
        {
            var cb = configTab.Controls.Find(name, true).FirstOrDefault() as CheckBox;
            if (cb == null) continue;

            cb.FlatStyle = FlatStyle.System;
            cb.BackColor = Color.Transparent;
            cb.ForeColor = AppThemeHelper.PrimaryDark;
            cb.Font = new Font("Segoe UI", 9F);
        }

        configTab.ResumeLayout(true);
    }

    public void ApplyTablesTabStyle(TabPage tablesTab)
    {
        tablesTab.SuspendLayout();

        // Mejorar TreeView de tablas
        var twTables = tablesTab.Controls.Find("twTables", true).FirstOrDefault() as TreeView;
        if (twTables != null)
        {
            twTables.BackColor = AppThemeHelper.BackgroundCard;
            twTables.BorderStyle = BorderStyle.None;
            twTables.Font = new Font("Segoe UI", 9F);
            twTables.ForeColor = AppThemeHelper.PrimaryDark;
        }

        // Mejorar DataGridView
        var dgvHands = tablesTab.Controls.Find("dgvHands", true).FirstOrDefault() as DataGridView;
        if (dgvHands != null)
        {
            dgvHands.EnableHeadersVisualStyles = false;
            dgvHands.BackgroundColor = AppThemeHelper.BackgroundMain;
            dgvHands.BorderStyle = BorderStyle.None;
            dgvHands.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvHands.GridColor = AppThemeHelper.BorderLight;
            dgvHands.Font = new Font("Segoe UI", 9F);

            // Estilo de headers
            dgvHands.ColumnHeadersDefaultCellStyle.BackColor = AppThemeHelper.PrimaryDark;
            dgvHands.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHands.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            dgvHands.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryDark;
            dgvHands.ColumnHeadersHeight = 35;

            // Estilo de celdas
            dgvHands.DefaultCellStyle.BackColor = AppThemeHelper.BackgroundCard;
            dgvHands.DefaultCellStyle.ForeColor = AppThemeHelper.PrimaryDark;
            dgvHands.DefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryLight;
            dgvHands.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvHands.RowTemplate.Height = 28;

            // Estilo alternado
            dgvHands.AlternatingRowsDefaultCellStyle.BackColor = AppThemeHelper.BackgroundMain;
        }

        tablesTab.ResumeLayout(true);
    }

    public void ApplyLogsTabStyle(TabPage logsTab)
    {
        logsTab.SuspendLayout();

        // Mejorar TextBox de logs
        var tbResume = logsTab.Controls.Find("tbResume", true).FirstOrDefault() as TextBox;
        if (tbResume != null)
        {
            tbResume.BackColor = AppThemeHelper.PrimaryDark;
            tbResume.ForeColor = Color.FromArgb(220, 220, 220);
            tbResume.Font = new Font("Consolas", 9F);
            tbResume.BorderStyle = BorderStyle.None;
        }

        logsTab.ResumeLayout(true);
    }

    public void ApplyHistorialTabStyle(TabPage historialTab)
    {
        historialTab.SuspendLayout();

        // Labels título
        var titleNames = new[] { "lblSesionesTitle", "lblManosTitle" };
        foreach (var name in titleNames)
        {
            var lbl = historialTab.Controls.Find(name, true).FirstOrDefault() as Label;
            if (lbl == null) continue;

            lbl.BackColor = AppThemeHelper.PrimaryDark;
            lbl.ForeColor = Color.White;
            lbl.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lbl.Padding = new Padding(6, 0, 0, 0);
        }

        // Estilizar ambas grillas
        var dgvNames = new[] { "dgvSessions", "dgvSessionHands" };
        foreach (var name in dgvNames)
        {
            var dgv = historialTab.Controls.Find(name, true).FirstOrDefault() as DataGridView;
            if (dgv == null) continue;

            dgv.EnableHeadersVisualStyles = false;
            dgv.BackgroundColor = AppThemeHelper.BackgroundMain;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = AppThemeHelper.BorderLight;
            dgv.Font = new Font("Segoe UI", 9F);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = AppThemeHelper.PrimaryDark;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryDark;
            dgv.ColumnHeadersHeight = 35;
            dgv.DefaultCellStyle.BackColor = AppThemeHelper.BackgroundCard;
            dgv.DefaultCellStyle.ForeColor = AppThemeHelper.PrimaryDark;
            dgv.DefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryLight;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.RowTemplate.Height = 28;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = AppThemeHelper.BackgroundMain;
        }

        // Label de stats
        var lblSessionStats = historialTab.Controls.Find("lblSessionStats", true).FirstOrDefault() as Label;
        if (lblSessionStats != null)
        {
            lblSessionStats.BackColor = AppThemeHelper.BackgroundMain;
            lblSessionStats.ForeColor = AppThemeHelper.PrimaryDark;
            lblSessionStats.Font = new Font("Segoe UI", 9F);
        }

        historialTab.ResumeLayout(true);
    }

    public void ApplyBankrollTabStyle(TabPage bankrollTab)
    {
        bankrollTab.SuspendLayout();

        // Paneles con fondo blanco y borde sutil
        var panelNames = new[] { "pnlBankrollInfo", "pnlRiskInfo", "pnlPerformance" };
        foreach (var name in panelNames)
        {
            var panel = bankrollTab.Controls.Find(name, true).FirstOrDefault() as Panel;
            if (panel == null) continue;

            panel.BackColor = AppThemeHelper.BackgroundCard;
            panel.BorderStyle = BorderStyle.None;
            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(AppThemeHelper.BorderLight, 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, ((Panel)s!).Width - 1, ((Panel)s!).Height - 1);
            };
        }

        // DataGridView con mismo estilo que historial
        var dgvBankrollHistory = bankrollTab.Controls.Find("dgvBankrollHistory", true).FirstOrDefault() as DataGridView;
        if (dgvBankrollHistory != null)
        {
            dgvBankrollHistory.EnableHeadersVisualStyles = false;
            dgvBankrollHistory.BackgroundColor = AppThemeHelper.BackgroundMain;
            dgvBankrollHistory.BorderStyle = BorderStyle.None;
            dgvBankrollHistory.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvBankrollHistory.GridColor = AppThemeHelper.BorderLight;
            dgvBankrollHistory.Font = new Font("Segoe UI", 9F);
            dgvBankrollHistory.ColumnHeadersDefaultCellStyle.BackColor = AppThemeHelper.PrimaryDark;
            dgvBankrollHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvBankrollHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            dgvBankrollHistory.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryDark;
            dgvBankrollHistory.ColumnHeadersHeight = 32;
            dgvBankrollHistory.DefaultCellStyle.BackColor = AppThemeHelper.BackgroundCard;
            dgvBankrollHistory.DefaultCellStyle.ForeColor = AppThemeHelper.PrimaryDark;
            dgvBankrollHistory.DefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryLight;
            dgvBankrollHistory.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvBankrollHistory.RowTemplate.Height = 26;
            dgvBankrollHistory.AlternatingRowsDefaultCellStyle.BackColor = AppThemeHelper.BackgroundMain;
        }

        bankrollTab.ResumeLayout(true);
    }
}
