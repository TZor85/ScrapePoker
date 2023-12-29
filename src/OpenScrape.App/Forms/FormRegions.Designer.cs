namespace OpenScrape.App
{
    partial class FormRegions
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
            label1 = new Label();
            cbRegions = new ComboBox();
            btnAdd = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(25, 9);
            label1.Name = "label1";
            label1.Size = new Size(80, 15);
            label1.TabIndex = 0;
            label1.Text = "Region name:";
            // 
            // cbRegions
            // 
            cbRegions.FormattingEnabled = true;
            cbRegions.Location = new Point(25, 27);
            cbRegions.Name = "cbRegions";
            cbRegions.Size = new Size(121, 23);
            cbRegions.TabIndex = 1;
            cbRegions.SelectedIndexChanged += cbRegions_SelectedIndexChanged;
            // 
            // btnAdd
            // 
            btnAdd.Enabled = false;
            btnAdd.Location = new Point(25, 69);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(56, 23);
            btnAdd.TabIndex = 2;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(87, 69);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(59, 23);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // FormRegions
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(175, 104);
            Controls.Add(btnCancel);
            Controls.Add(btnAdd);
            Controls.Add(cbRegions);
            Controls.Add(label1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormRegions";
            Text = "New Region Record";
            TopMost = true;
            Load += FormRegions_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox cbRegions;
        private Button btnAdd;
        private Button btnCancel;
    }
}