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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnAccept = new System.Windows.Forms.Button();
            this.lbApps = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // btnCancelar
            // 
            this.btnCancel.Location = new System.Drawing.Point(222, 112);
            this.btnCancel.Name = "btnCancelar";
            this.btnCancel.Size = new System.Drawing.Size(80, 31);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancelar";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancelar_Click);
            // 
            // btnAceptar
            // 
            this.btnAccept.Enabled = false;
            this.btnAccept.Location = new System.Drawing.Point(136, 112);
            this.btnAccept.Name = "btnAceptar";
            this.btnAccept.Size = new System.Drawing.Size(80, 31);
            this.btnAccept.TabIndex = 1;
            this.btnAccept.Text = "Aceptar";
            this.btnAccept.UseVisualStyleBackColor = true;
            this.btnAccept.Click += new System.EventHandler(this.btnAceptar_Click);
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
            this.Controls.Add(this.btnAccept);
            this.Controls.Add(this.btnCancel);
            this.Name = "FormListApps";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "FormListApps";
            this.Load += new System.EventHandler(this.FormListApps_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Button btnCancel;
        private Button btnAccept;
        private ListBox lbApps;
    }
}