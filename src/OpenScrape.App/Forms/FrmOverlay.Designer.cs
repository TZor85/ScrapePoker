namespace OpenScrape.App.Forms
{
    partial class FrmOverlay
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lbAction = new Label();
            lbPotOdds = new Label();
            lbEquity = new Label();
            lbShouldCall = new Label();
            lbSituacion = new Label();
            SuspendLayout();
            // 
            // lbAction
            // 
            lbAction.AutoSize = true;
            lbAction.BackColor = Color.Transparent;
            lbAction.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lbAction.ForeColor = Color.Red;
            lbAction.Location = new Point(3, 75);
            lbAction.Name = "lbAction";
            lbAction.Size = new Size(70, 25);
            lbAction.TabIndex = 0;
            lbAction.Text = "Action";
            // 
            // lbPotOdds
            // 
            lbPotOdds.AutoSize = true;
            lbPotOdds.BackColor = Color.Transparent;
            lbPotOdds.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lbPotOdds.ForeColor = SystemColors.MenuHighlight;
            lbPotOdds.Location = new Point(3, 3);
            lbPotOdds.Name = "lbPotOdds";
            lbPotOdds.Size = new Size(50, 19);
            lbPotOdds.TabIndex = 1;
            lbPotOdds.Text = "label1";
            // 
            // lbEquity
            // 
            lbEquity.AutoSize = true;
            lbEquity.BackColor = Color.Transparent;
            lbEquity.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lbEquity.ForeColor = SystemColors.MenuHighlight;
            lbEquity.Location = new Point(3, 26);
            lbEquity.Name = "lbEquity";
            lbEquity.Size = new Size(50, 19);
            lbEquity.TabIndex = 2;
            lbEquity.Text = "label2";
            // 
            // lbShouldCall
            // 
            lbShouldCall.AutoSize = true;
            lbShouldCall.BackColor = Color.Transparent;
            lbShouldCall.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lbShouldCall.ForeColor = SystemColors.MenuHighlight;
            lbShouldCall.Location = new Point(120, 3);
            lbShouldCall.Name = "lbShouldCall";
            lbShouldCall.Size = new Size(50, 19);
            lbShouldCall.TabIndex = 3;
            lbShouldCall.Text = "label1";
            // 
            // lbSituacion
            // 
            lbSituacion.AutoSize = true;
            lbSituacion.BackColor = Color.Transparent;
            lbSituacion.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lbSituacion.ForeColor = Color.DarkOrchid;
            lbSituacion.Location = new Point(4, 50);
            lbSituacion.Name = "lbSituacion";
            lbSituacion.Size = new Size(95, 25);
            lbSituacion.TabIndex = 4;
            lbSituacion.Text = "Situacion";
            // 
            // FrmOverlay
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.LightGray;
            ClientSize = new Size(214, 99);
            Controls.Add(lbSituacion);
            Controls.Add(lbShouldCall);
            Controls.Add(lbEquity);
            Controls.Add(lbPotOdds);
            Controls.Add(lbAction);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "FrmOverlay";
            Opacity = 0.7D;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            TransparencyKey = Color.Transparent;
            MouseDown += FrmOverlay_MouseDown;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lbAction;
        private Label lbPotOdds;
        private Label lbEquity;
        private Label lbShouldCall;
        private Label lbSituacion;
    }
}