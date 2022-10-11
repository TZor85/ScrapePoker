namespace OpenScrape.App
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Regions");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Hashes");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Images");
            this.twRegions = new System.Windows.Forms.TreeView();
            this.btnNew = new System.Windows.Forms.Button();
            this.btnPlusWidth = new System.Windows.Forms.Button();
            this.btnMinusWidth = new System.Windows.Forms.Button();
            this.btnPlusHeight = new System.Windows.Forms.Button();
            this.btnMinusHeight = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.cbSpeed = new System.Windows.Forms.ComboBox();
            this.btnSaveMap = new System.Windows.Forms.Button();
            this.btnLoadMap = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lbXY = new System.Windows.Forms.Label();
            this.tbHeight = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tbWidth = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnUpRight = new System.Windows.Forms.Button();
            this.btnDownRight = new System.Windows.Forms.Button();
            this.btnDownLeft = new System.Windows.Forms.Button();
            this.btnUpLeft = new System.Windows.Forms.Button();
            this.btnUp = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.btnLeft = new System.Windows.Forms.Button();
            this.btnRigth = new System.Windows.Forms.Button();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.pbZoom = new System.Windows.Forms.PictureBox();
            this.btnZoom = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnCapture = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnCreateImage = new System.Windows.Forms.Button();
            this.btnWindow = new System.Windows.Forms.Button();
            this.pbImageRegion = new System.Windows.Forms.PictureBox();
            this.btnCreateHash = new System.Windows.Forms.Button();
            this.ckHash = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbZoom)).BeginInit();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbImageRegion)).BeginInit();
            this.SuspendLayout();
            // 
            // twRegions
            // 
            this.twRegions.Location = new System.Drawing.Point(12, 48);
            this.twRegions.Name = "twRegions";
            treeNode1.Name = "Nodo0";
            treeNode1.Text = "Regions";
            treeNode2.Name = "Nodo1";
            treeNode2.Text = "Hashes";
            treeNode3.Name = "Nodo2";
            treeNode3.Text = "Images";
            this.twRegions.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2,
            treeNode3});
            this.twRegions.Size = new System.Drawing.Size(159, 390);
            this.twRegions.TabIndex = 0;
            this.twRegions.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.twRegions_AfterSelect);
            this.twRegions.DoubleClick += new System.EventHandler(this.twRegions_DoubleClick);
            // 
            // btnNew
            // 
            this.btnNew.Location = new System.Drawing.Point(12, 19);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(48, 23);
            this.btnNew.TabIndex = 1;
            this.btnNew.Text = "New";
            this.btnNew.UseVisualStyleBackColor = true;
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            // 
            // btnPlusWidth
            // 
            this.btnPlusWidth.Enabled = false;
            this.btnPlusWidth.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnPlusWidth.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnPlusWidth.Location = new System.Drawing.Point(10, 69);
            this.btnPlusWidth.Name = "btnPlusWidth";
            this.btnPlusWidth.Size = new System.Drawing.Size(25, 25);
            this.btnPlusWidth.TabIndex = 3;
            this.btnPlusWidth.Text = "+";
            this.btnPlusWidth.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnPlusWidth.UseVisualStyleBackColor = true;
            this.btnPlusWidth.Click += new System.EventHandler(this.btnPlusWidth_Click);
            // 
            // btnMinusWidth
            // 
            this.btnMinusWidth.Enabled = false;
            this.btnMinusWidth.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnMinusWidth.Location = new System.Drawing.Point(34, 69);
            this.btnMinusWidth.Name = "btnMinusWidth";
            this.btnMinusWidth.Size = new System.Drawing.Size(25, 25);
            this.btnMinusWidth.TabIndex = 4;
            this.btnMinusWidth.Text = "-";
            this.btnMinusWidth.UseVisualStyleBackColor = true;
            this.btnMinusWidth.Click += new System.EventHandler(this.btnMinusWidth_Click);
            // 
            // btnPlusHeight
            // 
            this.btnPlusHeight.Enabled = false;
            this.btnPlusHeight.Location = new System.Drawing.Point(79, 68);
            this.btnPlusHeight.Name = "btnPlusHeight";
            this.btnPlusHeight.Size = new System.Drawing.Size(25, 25);
            this.btnPlusHeight.TabIndex = 5;
            this.btnPlusHeight.Text = "+";
            this.btnPlusHeight.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnPlusHeight.UseVisualStyleBackColor = true;
            this.btnPlusHeight.Click += new System.EventHandler(this.btnPlusHeight_Click);
            // 
            // btnMinusHeight
            // 
            this.btnMinusHeight.Enabled = false;
            this.btnMinusHeight.Location = new System.Drawing.Point(103, 68);
            this.btnMinusHeight.Name = "btnMinusHeight";
            this.btnMinusHeight.Size = new System.Drawing.Size(25, 25);
            this.btnMinusHeight.TabIndex = 6;
            this.btnMinusHeight.Text = "-";
            this.btnMinusHeight.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnMinusHeight.UseVisualStyleBackColor = true;
            this.btnMinusHeight.Click += new System.EventHandler(this.btnMinusHeight_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(12, 460);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(50, 23);
            this.btnSave.TabIndex = 7;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // cbSpeed
            // 
            this.cbSpeed.FormattingEnabled = true;
            this.cbSpeed.Items.AddRange(new object[] {
            "1",
            "2",
            "5",
            "10",
            "20",
            "30",
            "40",
            "50"});
            this.cbSpeed.Location = new System.Drawing.Point(379, 58);
            this.cbSpeed.Name = "cbSpeed";
            this.cbSpeed.Size = new System.Drawing.Size(49, 23);
            this.cbSpeed.TabIndex = 12;
            this.cbSpeed.SelectedIndexChanged += new System.EventHandler(this.cbSpeed_SelectedIndexChanged);
            // 
            // btnSaveMap
            // 
            this.btnSaveMap.Location = new System.Drawing.Point(96, 513);
            this.btnSaveMap.Name = "btnSaveMap";
            this.btnSaveMap.Size = new System.Drawing.Size(75, 23);
            this.btnSaveMap.TabIndex = 13;
            this.btnSaveMap.Text = "Save Map";
            this.btnSaveMap.UseVisualStyleBackColor = true;
            this.btnSaveMap.Click += new System.EventHandler(this.btnSaveMap_Click);
            // 
            // btnLoadMap
            // 
            this.btnLoadMap.Location = new System.Drawing.Point(12, 513);
            this.btnLoadMap.Name = "btnLoadMap";
            this.btnLoadMap.Size = new System.Drawing.Size(75, 23);
            this.btnLoadMap.TabIndex = 14;
            this.btnLoadMap.Text = "Load Map";
            this.btnLoadMap.UseVisualStyleBackColor = true;
            this.btnLoadMap.Click += new System.EventHandler(this.btnLoadMap_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lbXY);
            this.groupBox1.Controls.Add(this.tbHeight);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.tbWidth);
            this.groupBox1.Controls.Add(this.btnPlusWidth);
            this.groupBox1.Controls.Add(this.btnMinusWidth);
            this.groupBox1.Controls.Add(this.btnPlusHeight);
            this.groupBox1.Controls.Add(this.btnMinusHeight);
            this.groupBox1.Location = new System.Drawing.Point(177, 48);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(195, 100);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Rectangle";
            // 
            // lbXY
            // 
            this.lbXY.AutoSize = true;
            this.lbXY.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lbXY.Location = new System.Drawing.Point(138, 77);
            this.lbXY.Name = "lbXY";
            this.lbXY.Size = new System.Drawing.Size(0, 13);
            this.lbXY.TabIndex = 8;
            // 
            // tbHeight
            // 
            this.tbHeight.Location = new System.Drawing.Point(79, 42);
            this.tbHeight.Name = "tbHeight";
            this.tbHeight.Size = new System.Drawing.Size(49, 23);
            this.tbHeight.TabIndex = 7;
            this.tbHeight.Text = "0";
            this.tbHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(79, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(43, 15);
            this.label2.TabIndex = 6;
            this.label2.Text = "Height";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 15);
            this.label1.TabIndex = 5;
            this.label1.Text = "Width";
            // 
            // tbWidth
            // 
            this.tbWidth.Location = new System.Drawing.Point(10, 42);
            this.tbWidth.Name = "tbWidth";
            this.tbWidth.Size = new System.Drawing.Size(49, 23);
            this.tbWidth.TabIndex = 0;
            this.tbWidth.Text = "0";
            this.tbWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnUpRight);
            this.groupBox2.Controls.Add(this.btnDownRight);
            this.groupBox2.Controls.Add(this.btnDownLeft);
            this.groupBox2.Controls.Add(this.btnUpLeft);
            this.groupBox2.Controls.Add(this.btnUp);
            this.groupBox2.Controls.Add(this.btnDown);
            this.groupBox2.Controls.Add(this.btnLeft);
            this.groupBox2.Controls.Add(this.btnRigth);
            this.groupBox2.Location = new System.Drawing.Point(441, 48);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(107, 100);
            this.groupBox2.TabIndex = 16;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Nudge";
            // 
            // btnUpRight
            // 
            this.btnUpRight.Enabled = false;
            this.btnUpRight.Location = new System.Drawing.Point(66, 20);
            this.btnUpRight.Name = "btnUpRight";
            this.btnUpRight.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnUpRight.Size = new System.Drawing.Size(25, 25);
            this.btnUpRight.TabIndex = 28;
            this.btnUpRight.Text = "↗";
            this.btnUpRight.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnUpRight.UseVisualStyleBackColor = true;
            this.btnUpRight.Click += new System.EventHandler(this.btnUpRight_Click);
            // 
            // btnDownRight
            // 
            this.btnDownRight.Enabled = false;
            this.btnDownRight.Location = new System.Drawing.Point(66, 69);
            this.btnDownRight.Name = "btnDownRight";
            this.btnDownRight.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnDownRight.Size = new System.Drawing.Size(25, 25);
            this.btnDownRight.TabIndex = 27;
            this.btnDownRight.Text = "↘";
            this.btnDownRight.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnDownRight.UseVisualStyleBackColor = true;
            this.btnDownRight.Click += new System.EventHandler(this.btnDownRight_Click);
            // 
            // btnDownLeft
            // 
            this.btnDownLeft.Enabled = false;
            this.btnDownLeft.Location = new System.Drawing.Point(18, 68);
            this.btnDownLeft.Name = "btnDownLeft";
            this.btnDownLeft.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnDownLeft.Size = new System.Drawing.Size(25, 25);
            this.btnDownLeft.TabIndex = 26;
            this.btnDownLeft.Text = "↙";
            this.btnDownLeft.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnDownLeft.UseVisualStyleBackColor = true;
            this.btnDownLeft.Click += new System.EventHandler(this.btnDownLeft_Click);
            // 
            // btnUpLeft
            // 
            this.btnUpLeft.Enabled = false;
            this.btnUpLeft.Location = new System.Drawing.Point(18, 20);
            this.btnUpLeft.Name = "btnUpLeft";
            this.btnUpLeft.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnUpLeft.Size = new System.Drawing.Size(25, 25);
            this.btnUpLeft.TabIndex = 25;
            this.btnUpLeft.Text = "↖";
            this.btnUpLeft.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnUpLeft.UseVisualStyleBackColor = true;
            this.btnUpLeft.Click += new System.EventHandler(this.btnUpLeft_Click);
            // 
            // btnUp
            // 
            this.btnUp.Enabled = false;
            this.btnUp.Location = new System.Drawing.Point(42, 20);
            this.btnUp.Name = "btnUp";
            this.btnUp.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnUp.Size = new System.Drawing.Size(25, 25);
            this.btnUp.TabIndex = 24;
            this.btnUp.Text = "↑";
            this.btnUp.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnUp.UseVisualStyleBackColor = true;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            // 
            // btnDown
            // 
            this.btnDown.Enabled = false;
            this.btnDown.Location = new System.Drawing.Point(42, 69);
            this.btnDown.Name = "btnDown";
            this.btnDown.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnDown.Size = new System.Drawing.Size(25, 25);
            this.btnDown.TabIndex = 23;
            this.btnDown.Text = "↓";
            this.btnDown.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnDown.UseVisualStyleBackColor = true;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // btnLeft
            // 
            this.btnLeft.Enabled = false;
            this.btnLeft.Location = new System.Drawing.Point(18, 44);
            this.btnLeft.Name = "btnLeft";
            this.btnLeft.Size = new System.Drawing.Size(25, 25);
            this.btnLeft.TabIndex = 22;
            this.btnLeft.Text = "←";
            this.btnLeft.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnLeft.UseVisualStyleBackColor = true;
            this.btnLeft.Click += new System.EventHandler(this.btnLeft_Click);
            // 
            // btnRigth
            // 
            this.btnRigth.Enabled = false;
            this.btnRigth.Location = new System.Drawing.Point(66, 44);
            this.btnRigth.Name = "btnRigth";
            this.btnRigth.Size = new System.Drawing.Size(25, 25);
            this.btnRigth.TabIndex = 21;
            this.btnRigth.Text = "→";
            this.btnRigth.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnRigth.UseVisualStyleBackColor = true;
            this.btnRigth.Click += new System.EventHandler(this.btnRigth_Click);
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            // 
            // pbZoom
            // 
            this.pbZoom.Location = new System.Drawing.Point(100, 15);
            this.pbZoom.Name = "pbZoom";
            this.pbZoom.Size = new System.Drawing.Size(216, 114);
            this.pbZoom.TabIndex = 17;
            this.pbZoom.TabStop = false;
            // 
            // btnZoom
            // 
            this.btnZoom.Enabled = false;
            this.btnZoom.Location = new System.Drawing.Point(10, 59);
            this.btnZoom.Name = "btnZoom";
            this.btnZoom.Size = new System.Drawing.Size(60, 23);
            this.btnZoom.TabIndex = 18;
            this.btnZoom.Text = "Zoom";
            this.btnZoom.UseVisualStyleBackColor = true;
            this.btnZoom.Click += new System.EventHandler(this.btnZoom_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.pbZoom);
            this.groupBox3.Controls.Add(this.btnZoom);
            this.groupBox3.Location = new System.Drawing.Point(177, 303);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(325, 135);
            this.groupBox3.TabIndex = 19;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Zoom";
            // 
            // btnCapture
            // 
            this.btnCapture.Location = new System.Drawing.Point(435, 460);
            this.btnCapture.Name = "btnCapture";
            this.btnCapture.Size = new System.Drawing.Size(104, 76);
            this.btnCapture.TabIndex = 20;
            this.btnCapture.Text = "Capture";
            this.btnCapture.UseVisualStyleBackColor = true;
            this.btnCapture.Click += new System.EventHandler(this.btnCapture_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(122, 19);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(49, 23);
            this.btnDelete.TabIndex = 21;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnCreateImage
            // 
            this.btnCreateImage.Enabled = false;
            this.btnCreateImage.Location = new System.Drawing.Point(441, 166);
            this.btnCreateImage.Name = "btnCreateImage";
            this.btnCreateImage.Size = new System.Drawing.Size(91, 23);
            this.btnCreateImage.TabIndex = 22;
            this.btnCreateImage.Text = "Create Image";
            this.btnCreateImage.UseVisualStyleBackColor = true;
            this.btnCreateImage.Click += new System.EventHandler(this.btnCreateImage_Click);
            // 
            // btnWindow
            // 
            this.btnWindow.Location = new System.Drawing.Point(326, 460);
            this.btnWindow.Name = "btnWindow";
            this.btnWindow.Size = new System.Drawing.Size(102, 76);
            this.btnWindow.TabIndex = 23;
            this.btnWindow.Text = "Window";
            this.btnWindow.UseVisualStyleBackColor = true;
            this.btnWindow.Click += new System.EventHandler(this.btnWindow_Click);
            // 
            // pbImageRegion
            // 
            this.pbImageRegion.Location = new System.Drawing.Point(177, 154);
            this.pbImageRegion.Name = "pbImageRegion";
            this.pbImageRegion.Size = new System.Drawing.Size(65, 85);
            this.pbImageRegion.TabIndex = 24;
            this.pbImageRegion.TabStop = false;
            // 
            // btnCreateHash
            // 
            this.btnCreateHash.Enabled = false;
            this.btnCreateHash.Location = new System.Drawing.Point(441, 204);
            this.btnCreateHash.Name = "btnCreateHash";
            this.btnCreateHash.Size = new System.Drawing.Size(91, 23);
            this.btnCreateHash.TabIndex = 25;
            this.btnCreateHash.Text = "Create Hash";
            this.btnCreateHash.UseVisualStyleBackColor = true;
            this.btnCreateHash.Click += new System.EventHandler(this.btnCrateHash_Click);
            // 
            // ckHash
            // 
            this.ckHash.AutoSize = true;
            this.ckHash.Enabled = false;
            this.ckHash.Location = new System.Drawing.Point(378, 98);
            this.ckHash.Name = "ckHash";
            this.ckHash.Size = new System.Drawing.Size(53, 19);
            this.ckHash.TabIndex = 26;
            this.ckHash.Text = "Hash";
            this.ckHash.UseVisualStyleBackColor = true;
            this.ckHash.CheckedChanged += new System.EventHandler(this.ckHash_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(555, 548);
            this.Controls.Add(this.ckHash);
            this.Controls.Add(this.btnCreateHash);
            this.Controls.Add(this.pbImageRegion);
            this.Controls.Add(this.btnWindow);
            this.Controls.Add(this.btnCreateImage);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnCapture);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnLoadMap);
            this.Controls.Add(this.btnSaveMap);
            this.Controls.Add(this.cbSpeed);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnNew);
            this.Controls.Add(this.twRegions);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbZoom)).EndInit();
            this.groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbImageRegion)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TreeView twRegions;
        private Button btnNew;
        private Button btnPlusWidth;
        private Button btnMinusWidth;
        private Button btnPlusHeight;
        private Button btnMinusHeight;
        private Button btnSave;
        private ComboBox cbSpeed;
        private Button btnSaveMap;
        private Button btnLoadMap;
        private GroupBox groupBox1;
        private TextBox tbHeight;
        private Label label2;
        private Label label1;
        private TextBox tbWidth;
        private GroupBox groupBox2;
        private Button btnUpRight;
        private Button btnDownRight;
        private Button btnDownLeft;
        private Button btnUpLeft;
        private Button btnUp;
        private Button btnDown;
        private Button btnLeft;
        private Button btnRigth;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private PictureBox pbZoom;
        private Button btnZoom;
        private GroupBox groupBox3;
        private Button btnCapture;
        private Button btnDelete;
        private Button btnCreateImage;
        private Button btnWindow;
        private PictureBox pbImageRegion;
        private Button btnCreateHash;
        private CheckBox ckHash;
        private Label lbXY;
    }
}