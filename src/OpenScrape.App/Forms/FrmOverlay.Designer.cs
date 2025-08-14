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
            panel1 = new Panel();
            lbPotOdds = new Label();
            panel2 = new Panel();
            lbShouldCall = new Label();
            panel3 = new Panel();
            lbEquity = new Label();
            panel4 = new Panel();
            lbSituacion = new Label();
            panel5 = new Panel();
            lbAction = new Label();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            panel4.SuspendLayout();
            panel5.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.AutoSize = true;
            panel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel1.BackColor = Color.Black;
            panel1.Controls.Add(lbPotOdds);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(53, 19);
            panel1.TabIndex = 5;
            // 
            // lbPotOdds
            // 
            lbPotOdds.AutoSize = true;
            lbPotOdds.BackColor = Color.Transparent;
            lbPotOdds.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lbPotOdds.ForeColor = SystemColors.MenuHighlight;
            lbPotOdds.Location = new Point(0, 0);
            lbPotOdds.Name = "lbPotOdds";
            lbPotOdds.Size = new Size(50, 19);
            lbPotOdds.TabIndex = 2;
            lbPotOdds.Text = "label1";
            // 
            // panel2
            // 
            panel2.AutoSize = true;
            panel2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel2.BackColor = Color.Black;
            panel2.Controls.Add(lbShouldCall);
            panel2.Location = new Point(120, 0);
            panel2.Margin = new Padding(0);
            panel2.Name = "panel2";
            panel2.Size = new Size(53, 19);
            panel2.TabIndex = 7;
            // 
            // lbShouldCall
            // 
            lbShouldCall.AutoSize = true;
            lbShouldCall.BackColor = Color.Transparent;
            lbShouldCall.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lbShouldCall.ForeColor = SystemColors.MenuHighlight;
            lbShouldCall.Location = new Point(0, 0);
            lbShouldCall.Name = "lbShouldCall";
            lbShouldCall.Size = new Size(50, 19);
            lbShouldCall.TabIndex = 7;
            lbShouldCall.Text = "label1";
            // 
            // panel3
            // 
            panel3.AutoSize = true;
            panel3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel3.BackColor = Color.Black;
            panel3.Controls.Add(lbEquity);
            panel3.Location = new Point(0, 20);
            panel3.Name = "panel3";
            panel3.Size = new Size(53, 19);
            panel3.TabIndex = 8;
            // 
            // lbEquity
            // 
            lbEquity.AutoSize = true;
            lbEquity.BackColor = Color.Transparent;
            lbEquity.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lbEquity.ForeColor = SystemColors.MenuHighlight;
            lbEquity.Location = new Point(0, 0);
            lbEquity.Name = "lbEquity";
            lbEquity.Size = new Size(50, 19);
            lbEquity.TabIndex = 3;
            lbEquity.Text = "label2";
            // 
            // panel4
            // 
            panel4.AutoSize = true;
            panel4.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel4.BackColor = Color.Black;
            panel4.Controls.Add(lbSituacion);
            panel4.Location = new Point(0, 40);
            panel4.Margin = new Padding(0);
            panel4.Name = "panel4";
            panel4.Size = new Size(95, 25);
            panel4.TabIndex = 9;
            // 
            // lbSituacion
            // 
            lbSituacion.AutoSize = true;
            lbSituacion.BackColor = Color.Transparent;
            lbSituacion.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lbSituacion.ForeColor = Color.DarkOrchid;
            lbSituacion.Location = new Point(0, 0);
            lbSituacion.Margin = new Padding(0);
            lbSituacion.Name = "lbSituacion";
            lbSituacion.Size = new Size(95, 25);
            lbSituacion.TabIndex = 5;
            lbSituacion.Text = "Situacion";
            // 
            // panel5
            // 
            panel5.AutoSize = true;
            panel5.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel5.BackColor = Color.Black;
            panel5.Controls.Add(lbAction);
            panel5.Location = new Point(0, 66);
            panel5.Margin = new Padding(0);
            panel5.Name = "panel5";
            panel5.Size = new Size(73, 25);
            panel5.TabIndex = 10;
            // 
            // lbAction
            // 
            lbAction.AutoSize = true;
            lbAction.BackColor = Color.Transparent;
            lbAction.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lbAction.ForeColor = Color.Red;
            lbAction.Location = new Point(0, 0);
            lbAction.Name = "lbAction";
            lbAction.Size = new Size(70, 25);
            lbAction.TabIndex = 1;
            lbAction.Text = "Action";
            // 
            // FrmOverlay
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.LightGray;
            ClientSize = new Size(214, 92);
            Controls.Add(panel5);
            Controls.Add(panel4);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "FrmOverlay";
            Opacity = 0.7D;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            TransparencyKey = Color.LightGray;
            MouseDown += FrmOverlay_MouseDown;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Panel panel1;
        private Label lbPotOdds;
        private Panel panel2;
        private Label lbShouldCall;
        private Panel panel3;
        private Label lbEquity;
        private Panel panel4;
        private Label lbSituacion;
        private Panel panel5;
        private Label lbAction;
    }
}