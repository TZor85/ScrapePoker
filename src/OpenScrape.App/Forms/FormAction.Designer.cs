namespace OpenScrape.App.Forms
{
    partial class FormAction
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
            button1 = new Button();
            SuspendLayout();
            // 
            // lbAction
            // 
            lbAction.AutoSize = true;
            lbAction.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            lbAction.ForeColor = Color.Red;
            lbAction.Location = new Point(3, 9);
            lbAction.Name = "lbAction";
            lbAction.Size = new Size(65, 25);
            lbAction.TabIndex = 0;
            lbAction.Text = "label1";
            // 
            // button1
            // 
            button1.Location = new Point(3, 37);
            button1.Name = "button1";
            button1.Size = new Size(233, 10);
            button1.TabIndex = 1;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // FormAction
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(235, 50);
            Controls.Add(button1);
            Controls.Add(lbAction);
            FormBorderStyle = FormBorderStyle.None;
            Name = "FormAction";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "FormAction";
            TopMost = true;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lbAction;
        private Button button1;
    }
}