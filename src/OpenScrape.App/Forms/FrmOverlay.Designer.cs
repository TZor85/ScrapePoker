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
            this.SuspendLayout();
            // 
            // FrmOverlay
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.Magenta;
            MinimumSize = new Size(200, 100);
            FormBorderStyle = FormBorderStyle.None;
            Name = "FrmOverlay";
            Opacity = 0.8D;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            TransparencyKey = Color.Magenta;
            MouseDown += FrmOverlay_MouseDown;
            this.ResumeLayout(false);
        }

        #endregion
    }
}