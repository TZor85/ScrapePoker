namespace OpenScrape.App
{
    partial class FormImage
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
            pbImagen = new PictureBox();
            btnLoad = new Button();
            lbPath = new Label();
            btnMinus = new Button();
            btnPlus = new Button();
            lbTotal = new Label();
            ((System.ComponentModel.ISupportInitialize)pbImagen).BeginInit();
            SuspendLayout();
            // 
            // pbImagen
            // 
            pbImagen.Location = new Point(12, 30);
            pbImagen.Name = "pbImagen";
            pbImagen.Size = new Size(422, 246);
            pbImagen.TabIndex = 1;
            pbImagen.TabStop = false;
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(2, 1);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(46, 23);
            btnLoad.TabIndex = 2;
            btnLoad.Text = "Load";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // lbPath
            // 
            lbPath.AutoSize = true;
            lbPath.Location = new Point(127, 5);
            lbPath.Name = "lbPath";
            lbPath.Size = new Size(0, 15);
            lbPath.TabIndex = 3;
            // 
            // btnMinus
            // 
            btnMinus.Location = new Point(54, 1);
            btnMinus.Name = "btnMinus";
            btnMinus.Size = new Size(30, 23);
            btnMinus.TabIndex = 4;
            btnMinus.Text = "<";
            btnMinus.UseVisualStyleBackColor = true;
            btnMinus.Click += btnMinus_Click;
            // 
            // btnPlus
            // 
            btnPlus.Location = new Point(87, 1);
            btnPlus.Name = "btnPlus";
            btnPlus.Size = new Size(30, 23);
            btnPlus.TabIndex = 5;
            btnPlus.Text = ">";
            btnPlus.UseVisualStyleBackColor = true;
            btnPlus.Click += btnPlus_Click;
            // 
            // lbTotal
            // 
            lbTotal.AutoSize = true;
            lbTotal.Location = new Point(406, 9);
            lbTotal.Name = "lbTotal";
            lbTotal.Size = new Size(0, 15);
            lbTotal.TabIndex = 6;
            // 
            // FormImage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(445, 288);
            Controls.Add(lbTotal);
            Controls.Add(btnPlus);
            Controls.Add(btnMinus);
            Controls.Add(lbPath);
            Controls.Add(btnLoad);
            Controls.Add(pbImagen);
            Location = new Point(150, 0);
            Name = "FormImage";
            StartPosition = FormStartPosition.Manual;
            Text = "FormImage";
            ((System.ComponentModel.ISupportInitialize)pbImagen).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        public PictureBox pbImagen;
        private Button btnLoad;
        private Label lbPath;
        private Button btnMinus;
        private Button btnPlus;
        private Label lbTotal;
    }
}