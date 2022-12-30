namespace OpenScrape.App
{
    partial class FormCreateFont
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
            this.btnAdd = new System.Windows.Forms.Button();
            this.tbFont = new System.Windows.Forms.TextBox();
            this.rtDraw = new System.Windows.Forms.RichTextBox();
            this.lbFonts = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(88, 11);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 0;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // tbFont
            // 
            this.tbFont.Location = new System.Drawing.Point(12, 12);
            this.tbFont.Name = "tbFont";
            this.tbFont.Size = new System.Drawing.Size(70, 23);
            this.tbFont.TabIndex = 2;
            // 
            // rtDraw
            // 
            this.rtDraw.Location = new System.Drawing.Point(88, 45);
            this.rtDraw.Name = "rtDraw";
            this.rtDraw.Size = new System.Drawing.Size(242, 246);
            this.rtDraw.TabIndex = 3;
            this.rtDraw.Text = "";
            // 
            // lbFonts
            // 
            this.lbFonts.FormattingEnabled = true;
            this.lbFonts.ItemHeight = 15;
            this.lbFonts.Location = new System.Drawing.Point(12, 45);
            this.lbFonts.Name = "lbFonts";
            this.lbFonts.Size = new System.Drawing.Size(70, 244);
            this.lbFonts.TabIndex = 5;
            this.lbFonts.DoubleClick += new System.EventHandler(this.lbFonts_DoubleClick);
            // 
            // FormCreateFont
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(337, 299);
            this.Controls.Add(this.lbFonts);
            this.Controls.Add(this.rtDraw);
            this.Controls.Add(this.tbFont);
            this.Controls.Add(this.btnAdd);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormCreateFont";
            this.Text = "CreateFont";
            this.Load += new System.EventHandler(this.FormCreateFont_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button btnAdd;
        private TextBox tbFont;
        private RichTextBox rtDraw;
        private ListBox lbFonts;
    }
}