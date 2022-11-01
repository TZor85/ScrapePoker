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
            this.ckHash = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbResult = new System.Windows.Forms.TextBox();
            this.ckColor = new System.Windows.Forms.CheckBox();
            this.tbR = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.tbG = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tbB = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.lbAction = new System.Windows.Forms.Label();
            this.lbP1Bet = new System.Windows.Forms.Label();
            this.lbP2Bet = new System.Windows.Forms.Label();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.pbCard1 = new System.Windows.Forms.PictureBox();
            this.pbCard0 = new System.Windows.Forms.PictureBox();
            this.lbDealer0 = new System.Windows.Forms.Label();
            this.lbP0Chips = new System.Windows.Forms.Label();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.lbDealer2 = new System.Windows.Forms.Label();
            this.lbP2Chips = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.lbDealer1 = new System.Windows.Forms.Label();
            this.lbP1Chips = new System.Windows.Forms.Label();
            this.lbEfective = new System.Windows.Forms.Label();
            this.cbTest = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbZoom)).BeginInit();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbImageRegion)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.groupBox7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCard1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCard0)).BeginInit();
            this.groupBox6.SuspendLayout();
            this.groupBox5.SuspendLayout();
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
            this.twRegions.Size = new System.Drawing.Size(159, 342);
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
            this.btnPlusHeight.Location = new System.Drawing.Point(73, 68);
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
            this.btnMinusHeight.Location = new System.Drawing.Point(97, 68);
            this.btnMinusHeight.Name = "btnMinusHeight";
            this.btnMinusHeight.Size = new System.Drawing.Size(25, 25);
            this.btnMinusHeight.TabIndex = 6;
            this.btnMinusHeight.Text = "-";
            this.btnMinusHeight.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnMinusHeight.UseVisualStyleBackColor = true;
            this.btnMinusHeight.Click += new System.EventHandler(this.btnMinusHeight_Click);
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
            this.cbSpeed.Location = new System.Drawing.Point(386, 58);
            this.cbSpeed.Name = "cbSpeed";
            this.cbSpeed.Size = new System.Drawing.Size(42, 23);
            this.cbSpeed.TabIndex = 12;
            this.cbSpeed.SelectedIndexChanged += new System.EventHandler(this.cbSpeed_SelectedIndexChanged);
            // 
            // btnSaveMap
            // 
            this.btnSaveMap.Location = new System.Drawing.Point(260, 367);
            this.btnSaveMap.Name = "btnSaveMap";
            this.btnSaveMap.Size = new System.Drawing.Size(75, 23);
            this.btnSaveMap.TabIndex = 13;
            this.btnSaveMap.Text = "Save Map";
            this.btnSaveMap.UseVisualStyleBackColor = true;
            this.btnSaveMap.Click += new System.EventHandler(this.btnSaveMap_Click);
            // 
            // btnLoadMap
            // 
            this.btnLoadMap.Location = new System.Drawing.Point(176, 367);
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
            this.groupBox1.Size = new System.Drawing.Size(203, 100);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Rectangle";
            // 
            // lbXY
            // 
            this.lbXY.AutoSize = true;
            this.lbXY.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lbXY.Location = new System.Drawing.Point(130, 77);
            this.lbXY.Name = "lbXY";
            this.lbXY.Size = new System.Drawing.Size(0, 13);
            this.lbXY.TabIndex = 8;
            // 
            // tbHeight
            // 
            this.tbHeight.Location = new System.Drawing.Point(73, 42);
            this.tbHeight.Name = "tbHeight";
            this.tbHeight.Size = new System.Drawing.Size(49, 23);
            this.tbHeight.TabIndex = 7;
            this.tbHeight.Text = "0";
            this.tbHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(73, 24);
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
            this.pbZoom.Location = new System.Drawing.Point(74, 22);
            this.pbZoom.Name = "pbZoom";
            this.pbZoom.Size = new System.Drawing.Size(92, 62);
            this.pbZoom.TabIndex = 17;
            this.pbZoom.TabStop = false;
            // 
            // btnZoom
            // 
            this.btnZoom.Enabled = false;
            this.btnZoom.Location = new System.Drawing.Point(6, 61);
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
            this.groupBox3.Location = new System.Drawing.Point(376, 264);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(172, 97);
            this.groupBox3.TabIndex = 19;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Zoom";
            // 
            // btnCapture
            // 
            this.btnCapture.Location = new System.Drawing.Point(446, 748);
            this.btnCapture.Name = "btnCapture";
            this.btnCapture.Size = new System.Drawing.Size(104, 76);
            this.btnCapture.TabIndex = 20;
            this.btnCapture.Text = "Capture";
            this.btnCapture.UseVisualStyleBackColor = true;
            this.btnCapture.Click += new System.EventHandler(this.btnCapture_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(66, 19);
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
            this.btnWindow.Location = new System.Drawing.Point(337, 748);
            this.btnWindow.Name = "btnWindow";
            this.btnWindow.Size = new System.Drawing.Size(102, 76);
            this.btnWindow.TabIndex = 23;
            this.btnWindow.Text = "Window";
            this.btnWindow.UseVisualStyleBackColor = true;
            this.btnWindow.Click += new System.EventHandler(this.btnWindow_Click);
            // 
            // pbImageRegion
            // 
            this.pbImageRegion.Location = new System.Drawing.Point(177, 264);
            this.pbImageRegion.Name = "pbImageRegion";
            this.pbImageRegion.Size = new System.Drawing.Size(159, 97);
            this.pbImageRegion.TabIndex = 24;
            this.pbImageRegion.TabStop = false;
            // 
            // ckHash
            // 
            this.ckHash.AutoSize = true;
            this.ckHash.Enabled = false;
            this.ckHash.Location = new System.Drawing.Point(386, 98);
            this.ckHash.Name = "ckHash";
            this.ckHash.Size = new System.Drawing.Size(53, 19);
            this.ckHash.TabIndex = 26;
            this.ckHash.Text = "Hash";
            this.ckHash.UseVisualStyleBackColor = true;
            this.ckHash.CheckedChanged += new System.EventHandler(this.ckHash_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(177, 166);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(39, 15);
            this.label3.TabIndex = 27;
            this.label3.Text = "Result";
            // 
            // tbResult
            // 
            this.tbResult.Enabled = false;
            this.tbResult.Location = new System.Drawing.Point(222, 163);
            this.tbResult.Name = "tbResult";
            this.tbResult.Size = new System.Drawing.Size(114, 23);
            this.tbResult.TabIndex = 28;
            // 
            // ckColor
            // 
            this.ckColor.AutoSize = true;
            this.ckColor.Enabled = false;
            this.ckColor.Location = new System.Drawing.Point(386, 119);
            this.ckColor.Name = "ckColor";
            this.ckColor.Size = new System.Drawing.Size(55, 19);
            this.ckColor.TabIndex = 29;
            this.ckColor.Text = "Color";
            this.ckColor.UseVisualStyleBackColor = true;
            this.ckColor.CheckedChanged += new System.EventHandler(this.ckColor_CheckedChanged);
            // 
            // tbR
            // 
            this.tbR.Enabled = false;
            this.tbR.Location = new System.Drawing.Point(222, 220);
            this.tbR.Name = "tbR";
            this.tbR.Size = new System.Drawing.Size(29, 23);
            this.tbR.TabIndex = 32;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(229, 202);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(14, 15);
            this.label4.TabIndex = 33;
            this.label4.Text = "R";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(264, 202);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(15, 15);
            this.label5.TabIndex = 35;
            this.label5.Text = "G";
            // 
            // tbG
            // 
            this.tbG.Enabled = false;
            this.tbG.Location = new System.Drawing.Point(257, 220);
            this.tbG.Name = "tbG";
            this.tbG.Size = new System.Drawing.Size(29, 23);
            this.tbG.TabIndex = 34;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(299, 202);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(14, 15);
            this.label6.TabIndex = 37;
            this.label6.Text = "B";
            // 
            // tbB
            // 
            this.tbB.Enabled = false;
            this.tbB.Location = new System.Drawing.Point(292, 220);
            this.tbB.Name = "tbB";
            this.tbB.Size = new System.Drawing.Size(29, 23);
            this.tbB.TabIndex = 36;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(177, 223);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(36, 15);
            this.label8.TabIndex = 39;
            this.label8.Text = "Color";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.lbAction);
            this.groupBox4.Controls.Add(this.lbP1Bet);
            this.groupBox4.Controls.Add(this.lbP2Bet);
            this.groupBox4.Controls.Add(this.groupBox7);
            this.groupBox4.Controls.Add(this.groupBox6);
            this.groupBox4.Controls.Add(this.groupBox5);
            this.groupBox4.Controls.Add(this.lbEfective);
            this.groupBox4.Location = new System.Drawing.Point(17, 396);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(531, 346);
            this.groupBox4.TabIndex = 43;
            this.groupBox4.TabStop = false;
            // 
            // lbAction
            // 
            this.lbAction.AutoSize = true;
            this.lbAction.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lbAction.Location = new System.Drawing.Point(365, 264);
            this.lbAction.Name = "lbAction";
            this.lbAction.Size = new System.Drawing.Size(119, 37);
            this.lbAction.TabIndex = 11;
            this.lbAction.Text = "ACTION";
            // 
            // lbP1Bet
            // 
            this.lbP1Bet.AutoSize = true;
            this.lbP1Bet.Location = new System.Drawing.Point(157, 72);
            this.lbP1Bet.Name = "lbP1Bet";
            this.lbP1Bet.Size = new System.Drawing.Size(0, 15);
            this.lbP1Bet.TabIndex = 10;
            // 
            // lbP2Bet
            // 
            this.lbP2Bet.AutoSize = true;
            this.lbP2Bet.Location = new System.Drawing.Point(334, 72);
            this.lbP2Bet.Name = "lbP2Bet";
            this.lbP2Bet.Size = new System.Drawing.Size(0, 15);
            this.lbP2Bet.TabIndex = 9;
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.pbCard1);
            this.groupBox7.Controls.Add(this.pbCard0);
            this.groupBox7.Controls.Add(this.lbDealer0);
            this.groupBox7.Controls.Add(this.lbP0Chips);
            this.groupBox7.Location = new System.Drawing.Point(174, 220);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(145, 113);
            this.groupBox7.TabIndex = 8;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Hero";
            // 
            // pbCard1
            // 
            this.pbCard1.Location = new System.Drawing.Point(41, 26);
            this.pbCard1.Name = "pbCard1";
            this.pbCard1.Size = new System.Drawing.Size(25, 55);
            this.pbCard1.TabIndex = 8;
            this.pbCard1.TabStop = false;
            // 
            // pbCard0
            // 
            this.pbCard0.Location = new System.Drawing.Point(10, 26);
            this.pbCard0.Name = "pbCard0";
            this.pbCard0.Size = new System.Drawing.Size(25, 55);
            this.pbCard0.TabIndex = 7;
            this.pbCard0.TabStop = false;
            // 
            // lbDealer0
            // 
            this.lbDealer0.AutoSize = true;
            this.lbDealer0.Location = new System.Drawing.Point(99, 19);
            this.lbDealer0.Name = "lbDealer0";
            this.lbDealer0.Size = new System.Drawing.Size(40, 15);
            this.lbDealer0.TabIndex = 6;
            this.lbDealer0.Text = "Dealer";
            // 
            // lbP0Chips
            // 
            this.lbP0Chips.AutoSize = true;
            this.lbP0Chips.Location = new System.Drawing.Point(6, 86);
            this.lbP0Chips.Name = "lbP0Chips";
            this.lbP0Chips.Size = new System.Drawing.Size(43, 15);
            this.lbP0Chips.TabIndex = 3;
            this.lbP0Chips.Text = "Chips: ";
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.lbDealer2);
            this.groupBox6.Controls.Add(this.lbP2Chips);
            this.groupBox6.Location = new System.Drawing.Point(380, 22);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(145, 113);
            this.groupBox6.TabIndex = 7;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Player 2";
            // 
            // lbDealer2
            // 
            this.lbDealer2.AutoSize = true;
            this.lbDealer2.Location = new System.Drawing.Point(99, 19);
            this.lbDealer2.Name = "lbDealer2";
            this.lbDealer2.Size = new System.Drawing.Size(40, 15);
            this.lbDealer2.TabIndex = 7;
            this.lbDealer2.Text = "Dealer";
            // 
            // lbP2Chips
            // 
            this.lbP2Chips.AutoSize = true;
            this.lbP2Chips.Location = new System.Drawing.Point(6, 86);
            this.lbP2Chips.Name = "lbP2Chips";
            this.lbP2Chips.Size = new System.Drawing.Size(43, 15);
            this.lbP2Chips.TabIndex = 2;
            this.lbP2Chips.Text = "Chips: ";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.lbDealer1);
            this.groupBox5.Controls.Add(this.lbP1Chips);
            this.groupBox5.Location = new System.Drawing.Point(6, 22);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(145, 113);
            this.groupBox5.TabIndex = 6;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Player 1";
            // 
            // lbDealer1
            // 
            this.lbDealer1.AutoSize = true;
            this.lbDealer1.Location = new System.Drawing.Point(101, 19);
            this.lbDealer1.Name = "lbDealer1";
            this.lbDealer1.Size = new System.Drawing.Size(40, 15);
            this.lbDealer1.TabIndex = 2;
            this.lbDealer1.Text = "Dealer";
            // 
            // lbP1Chips
            // 
            this.lbP1Chips.AutoSize = true;
            this.lbP1Chips.Location = new System.Drawing.Point(6, 86);
            this.lbP1Chips.Name = "lbP1Chips";
            this.lbP1Chips.Size = new System.Drawing.Size(43, 15);
            this.lbP1Chips.TabIndex = 1;
            this.lbP1Chips.Text = "Chips: ";
            // 
            // lbEfective
            // 
            this.lbEfective.AutoSize = true;
            this.lbEfective.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lbEfective.Location = new System.Drawing.Point(6, 306);
            this.lbEfective.Name = "lbEfective";
            this.lbEfective.Size = new System.Drawing.Size(57, 21);
            this.lbEfective.TabIndex = 3;
            this.lbEfective.Text = "Ef BB: ";
            // 
            // cbTest
            // 
            this.cbTest.AutoSize = true;
            this.cbTest.Location = new System.Drawing.Point(125, 23);
            this.cbTest.Name = "cbTest";
            this.cbTest.Size = new System.Drawing.Size(46, 19);
            this.cbTest.TabIndex = 44;
            this.cbTest.Text = "Test";
            this.cbTest.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(555, 836);
            this.Controls.Add(this.cbTest);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.btnCapture);
            this.Controls.Add(this.btnWindow);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.tbB);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tbG);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tbR);
            this.Controls.Add(this.ckColor);
            this.Controls.Add(this.tbResult);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.ckHash);
            this.Controls.Add(this.pbImageRegion);
            this.Controls.Add(this.btnCreateImage);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnLoadMap);
            this.Controls.Add(this.btnSaveMap);
            this.Controls.Add(this.cbSpeed);
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
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCard1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCard0)).EndInit();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
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
        private CheckBox ckHash;
        private Label lbXY;
        private Label label3;
        private TextBox tbResult;
        private CheckBox ckColor;
        private TextBox tbR;
        private Label label4;
        private Label label5;
        private TextBox tbG;
        private Label label6;
        private TextBox tbB;
        private Label label8;
        private GroupBox groupBox4;
        private Label lbEfective;
        private GroupBox groupBox5;
        private Label lbP1Chips;
        private GroupBox groupBox7;
        private Label lbDealer0;
        private Label lbP0Chips;
        private GroupBox groupBox6;
        private Label lbDealer2;
        private Label lbP2Chips;
        private Label lbDealer1;
        private Label lbP1Bet;
        private Label lbP2Bet;
        private Label lbAction;
        private CheckBox cbTest;
        private PictureBox pbCard1;
        private PictureBox pbCard0;
    }
}