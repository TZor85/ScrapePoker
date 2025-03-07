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
            SuspendLayout();
            // 
            // lbAction
            // 
            lbAction.AutoSize = true;
            lbAction.BackColor = Color.Transparent;
            lbAction.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lbAction.ForeColor = Color.DeepSkyBlue;
            lbAction.Location = new Point(9, 117);
            lbAction.Name = "lbAction";
            lbAction.Size = new Size(0, 25);
            lbAction.TabIndex = 0;
            // 
            // lbPotOdds
            // 
            lbPotOdds.AutoSize = true;
            lbPotOdds.Location = new Point(6, 8);
            lbPotOdds.Name = "lbPotOdds";
            lbPotOdds.Size = new Size(38, 15);
            lbPotOdds.TabIndex = 1;
            lbPotOdds.Text = "label1";
            // 
            // lbEquity
            // 
            lbEquity.AutoSize = true;
            lbEquity.Location = new Point(83, 8);
            lbEquity.Name = "lbEquity";
            lbEquity.Size = new Size(38, 15);
            lbEquity.TabIndex = 2;
            lbEquity.Text = "label2";
            // 
            // lbShouldCall
            // 
            lbShouldCall.AutoSize = true;
            lbShouldCall.Location = new Point(162, 9);
            lbShouldCall.Name = "lbShouldCall";
            lbShouldCall.Size = new Size(38, 15);
            lbShouldCall.TabIndex = 3;
            lbShouldCall.Text = "label1";
            // 
            // FrmOverlay
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(250, 150);
            Controls.Add(lbShouldCall);
            Controls.Add(lbEquity);
            Controls.Add(lbPotOdds);
            Controls.Add(lbAction);
            FormBorderStyle = FormBorderStyle.None;
            Name = "FrmOverlay";
            Opacity = 0.7D;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "FrmOverlay";
            TopMost = true;
            TransparencyKey = Color.White;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lbAction;
        private Label lbPotOdds;
        private Label lbEquity;
        private Label lbShouldCall;
    }
}