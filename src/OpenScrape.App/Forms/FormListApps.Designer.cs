namespace OpenScrape.App
{
    partial class FormListApps
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
            this.btnCancelar = new System.Windows.Forms.Button();
            this.btnAceptar = new System.Windows.Forms.Button();
            this.lbApps = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // btnCancelar
            // 
            this.btnCancelar.Location = new System.Drawing.Point(222, 112);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new System.Drawing.Size(80, 31);
            this.btnCancelar.TabIndex = 0;
            this.btnCancelar.Text = "Cancelar";
            this.btnCancelar.UseVisualStyleBackColor = true;
            this.btnCancelar.Click += new System.EventHandler(this.btnCancelar_Click);
            // 
            // btnAceptar
            // 
            this.btnAceptar.Enabled = false;
            this.btnAceptar.Location = new System.Drawing.Point(136, 112);
            this.btnAceptar.Name = "btnAceptar";
            this.btnAceptar.Size = new System.Drawing.Size(80, 31);
            this.btnAceptar.TabIndex = 1;
            this.btnAceptar.Text = "Aceptar";
            this.btnAceptar.UseVisualStyleBackColor = true;
            this.btnAceptar.Click += new System.EventHandler(this.btnAceptar_Click);
            // 
            // lbApps
            // 
            this.lbApps.FormattingEnabled = true;
            this.lbApps.ItemHeight = 15;
            this.lbApps.Location = new System.Drawing.Point(12, 12);
            this.lbApps.Name = "lbApps";
            this.lbApps.Size = new System.Drawing.Size(412, 94);
            this.lbApps.TabIndex = 2;
            this.lbApps.SelectedIndexChanged += new System.EventHandler(this.lbApps_SelectedIndexChanged);
            // 
            // FormListApps
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(436, 152);
            this.Controls.Add(this.lbApps);
            this.Controls.Add(this.btnAceptar);
            this.Controls.Add(this.btnCancelar);
            this.Name = "FormListApps";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "FormListApps";
            this.Load += new System.EventHandler(this.FormListApps_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Button btnCancelar;
        private Button btnAceptar;
        private ListBox lbApps;
    }
}