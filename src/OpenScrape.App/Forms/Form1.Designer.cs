﻿namespace OpenScrape.App
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
            TreeNode treeNode9 = new TreeNode("Regions");
            TreeNode treeNode10 = new TreeNode("Board");
            TreeNode treeNode11 = new TreeNode("Images");
            TreeNode treeNode12 = new TreeNode("Fonts");
            twRegions = new TreeView();
            btnNew = new Button();
            btnPlusWidth = new Button();
            btnMinusWidth = new Button();
            btnPlusHeight = new Button();
            btnMinusHeight = new Button();
            cbSpeed = new ComboBox();
            btnSaveMap = new Button();
            btnLoadMap = new Button();
            groupBox1 = new GroupBox();
            lbXY = new Label();
            tbHeight = new TextBox();
            label2 = new Label();
            label1 = new Label();
            tbWidth = new TextBox();
            groupBox2 = new GroupBox();
            label7 = new Label();
            label3 = new Label();
            tbY = new TextBox();
            tbX = new TextBox();
            btnUpRight = new Button();
            btnDownRight = new Button();
            btnDownLeft = new Button();
            btnUpLeft = new Button();
            btnUp = new Button();
            btnDown = new Button();
            btnLeft = new Button();
            btnRigth = new Button();
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            btnCapture = new Button();
            btnDelete = new Button();
            btnCreateImage = new Button();
            btnWindow = new Button();
            ckColor = new CheckBox();
            tbR = new TextBox();
            label8 = new Label();
            cbTest = new CheckBox();
            ckBoard = new CheckBox();
            btnCreateFont = new Button();
            lbAction = new Label();
            pbCard1 = new PictureBox();
            pbCard0 = new PictureBox();
            tbResumen = new TextBox();
            btnCapture3bet = new Button();
            btnCapture4Bet = new Button();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbCard1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbCard0).BeginInit();
            SuspendLayout();
            // 
            // twRegions
            // 
            twRegions.Location = new Point(12, 48);
            twRegions.Name = "twRegions";
            treeNode9.Name = "Nodo0";
            treeNode9.NodeFont = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            treeNode9.Text = "Regions";
            treeNode10.Name = "Nodo1";
            treeNode10.NodeFont = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            treeNode10.Text = "Board";
            treeNode11.Name = "Nodo2";
            treeNode11.NodeFont = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            treeNode11.Text = "Images";
            treeNode12.Name = "Nodo3";
            treeNode12.NodeFont = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            treeNode12.Text = "Fonts";
            twRegions.Nodes.AddRange(new TreeNode[] { treeNode9, treeNode10, treeNode11, treeNode12 });
            twRegions.Size = new Size(159, 342);
            twRegions.TabIndex = 0;
            twRegions.AfterSelect += twRegions_AfterSelect;
            twRegions.DoubleClick += twRegions_DoubleClick;
            // 
            // btnNew
            // 
            btnNew.Location = new Point(12, 19);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(48, 23);
            btnNew.TabIndex = 1;
            btnNew.Text = "New";
            btnNew.UseVisualStyleBackColor = true;
            btnNew.Click += btnNew_Click;
            // 
            // btnPlusWidth
            // 
            btnPlusWidth.Enabled = false;
            btnPlusWidth.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnPlusWidth.ImageAlign = ContentAlignment.TopCenter;
            btnPlusWidth.Location = new Point(10, 69);
            btnPlusWidth.Name = "btnPlusWidth";
            btnPlusWidth.Size = new Size(25, 25);
            btnPlusWidth.TabIndex = 3;
            btnPlusWidth.Text = "+";
            btnPlusWidth.TextAlign = ContentAlignment.TopCenter;
            btnPlusWidth.UseVisualStyleBackColor = true;
            btnPlusWidth.Click += btnPlusWidth_Click;
            // 
            // btnMinusWidth
            // 
            btnMinusWidth.Enabled = false;
            btnMinusWidth.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnMinusWidth.Location = new Point(34, 69);
            btnMinusWidth.Name = "btnMinusWidth";
            btnMinusWidth.Size = new Size(25, 25);
            btnMinusWidth.TabIndex = 4;
            btnMinusWidth.Text = "-";
            btnMinusWidth.UseVisualStyleBackColor = true;
            btnMinusWidth.Click += btnMinusWidth_Click;
            // 
            // btnPlusHeight
            // 
            btnPlusHeight.Enabled = false;
            btnPlusHeight.Location = new Point(73, 68);
            btnPlusHeight.Name = "btnPlusHeight";
            btnPlusHeight.Size = new Size(25, 25);
            btnPlusHeight.TabIndex = 5;
            btnPlusHeight.Text = "+";
            btnPlusHeight.TextAlign = ContentAlignment.MiddleRight;
            btnPlusHeight.UseVisualStyleBackColor = true;
            btnPlusHeight.Click += btnPlusHeight_Click;
            // 
            // btnMinusHeight
            // 
            btnMinusHeight.Enabled = false;
            btnMinusHeight.Location = new Point(97, 68);
            btnMinusHeight.Name = "btnMinusHeight";
            btnMinusHeight.Size = new Size(25, 25);
            btnMinusHeight.TabIndex = 6;
            btnMinusHeight.Text = "-";
            btnMinusHeight.TextAlign = ContentAlignment.TopCenter;
            btnMinusHeight.UseVisualStyleBackColor = true;
            btnMinusHeight.Click += btnMinusHeight_Click;
            // 
            // cbSpeed
            // 
            cbSpeed.FormattingEnabled = true;
            cbSpeed.Items.AddRange(new object[] { "1", "2", "5", "10", "20", "30", "40", "50" });
            cbSpeed.Location = new Point(386, 58);
            cbSpeed.Name = "cbSpeed";
            cbSpeed.Size = new Size(42, 23);
            cbSpeed.TabIndex = 12;
            cbSpeed.SelectedIndexChanged += cbSpeed_SelectedIndexChanged;
            // 
            // btnSaveMap
            // 
            btnSaveMap.Location = new Point(516, 367);
            btnSaveMap.Name = "btnSaveMap";
            btnSaveMap.Size = new Size(75, 23);
            btnSaveMap.TabIndex = 13;
            btnSaveMap.Text = "Save Map";
            btnSaveMap.UseVisualStyleBackColor = true;
            btnSaveMap.Click += btnSaveMap_Click;
            // 
            // btnLoadMap
            // 
            btnLoadMap.Location = new Point(434, 367);
            btnLoadMap.Name = "btnLoadMap";
            btnLoadMap.Size = new Size(75, 23);
            btnLoadMap.TabIndex = 14;
            btnLoadMap.Text = "Load Map";
            btnLoadMap.UseVisualStyleBackColor = true;
            btnLoadMap.Click += btnLoadMap_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(lbXY);
            groupBox1.Controls.Add(tbHeight);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(tbWidth);
            groupBox1.Controls.Add(btnPlusWidth);
            groupBox1.Controls.Add(btnMinusWidth);
            groupBox1.Controls.Add(btnPlusHeight);
            groupBox1.Controls.Add(btnMinusHeight);
            groupBox1.Location = new Point(177, 48);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(203, 100);
            groupBox1.TabIndex = 15;
            groupBox1.TabStop = false;
            groupBox1.Text = "Rectangle";
            // 
            // lbXY
            // 
            lbXY.AutoSize = true;
            lbXY.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
            lbXY.Location = new Point(130, 77);
            lbXY.Name = "lbXY";
            lbXY.Size = new Size(0, 13);
            lbXY.TabIndex = 8;
            // 
            // tbHeight
            // 
            tbHeight.Location = new Point(73, 42);
            tbHeight.Name = "tbHeight";
            tbHeight.Size = new Size(49, 23);
            tbHeight.TabIndex = 7;
            tbHeight.Text = "0";
            tbHeight.TextAlign = HorizontalAlignment.Right;
            tbHeight.Leave += tbHeight_Leave;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(73, 24);
            label2.Name = "label2";
            label2.Size = new Size(43, 15);
            label2.TabIndex = 6;
            label2.Text = "Height";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 24);
            label1.Name = "label1";
            label1.Size = new Size(39, 15);
            label1.TabIndex = 5;
            label1.Text = "Width";
            // 
            // tbWidth
            // 
            tbWidth.Location = new Point(10, 42);
            tbWidth.Name = "tbWidth";
            tbWidth.Size = new Size(49, 23);
            tbWidth.TabIndex = 0;
            tbWidth.Text = "0";
            tbWidth.TextAlign = HorizontalAlignment.Right;
            tbWidth.Leave += tbWidth_Leave;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label7);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(tbY);
            groupBox2.Controls.Add(tbX);
            groupBox2.Controls.Add(btnUpRight);
            groupBox2.Controls.Add(btnDownRight);
            groupBox2.Controls.Add(btnDownLeft);
            groupBox2.Controls.Add(btnUpLeft);
            groupBox2.Controls.Add(btnUp);
            groupBox2.Controls.Add(btnDown);
            groupBox2.Controls.Add(btnLeft);
            groupBox2.Controls.Add(btnRigth);
            groupBox2.Location = new Point(464, 48);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(107, 143);
            groupBox2.TabIndex = 16;
            groupBox2.TabStop = false;
            groupBox2.Text = "Nudge";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(57, 96);
            label7.Name = "label7";
            label7.Size = new Size(14, 15);
            label7.TabIndex = 32;
            label7.Text = "Y";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(5, 96);
            label3.Name = "label3";
            label3.Size = new Size(14, 15);
            label3.TabIndex = 31;
            label3.Text = "X";
            // 
            // tbY
            // 
            tbY.Location = new Point(58, 114);
            tbY.Name = "tbY";
            tbY.Size = new Size(43, 23);
            tbY.TabIndex = 30;
            tbY.Text = "0";
            tbY.TextAlign = HorizontalAlignment.Right;
            tbY.Leave += tbY_Leave;
            // 
            // tbX
            // 
            tbX.Location = new Point(6, 114);
            tbX.Name = "tbX";
            tbX.Size = new Size(43, 23);
            tbX.TabIndex = 29;
            tbX.Text = "0";
            tbX.TextAlign = HorizontalAlignment.Right;
            tbX.Leave += tbX_Leave;
            // 
            // btnUpRight
            // 
            btnUpRight.Enabled = false;
            btnUpRight.Location = new Point(66, 20);
            btnUpRight.Name = "btnUpRight";
            btnUpRight.RightToLeft = RightToLeft.No;
            btnUpRight.Size = new Size(25, 25);
            btnUpRight.TabIndex = 28;
            btnUpRight.Text = "↗";
            btnUpRight.TextAlign = ContentAlignment.MiddleRight;
            btnUpRight.UseVisualStyleBackColor = true;
            btnUpRight.Click += btnUpRight_Click;
            // 
            // btnDownRight
            // 
            btnDownRight.Enabled = false;
            btnDownRight.Location = new Point(66, 68);
            btnDownRight.Name = "btnDownRight";
            btnDownRight.RightToLeft = RightToLeft.No;
            btnDownRight.Size = new Size(25, 25);
            btnDownRight.TabIndex = 27;
            btnDownRight.Text = "↘";
            btnDownRight.TextAlign = ContentAlignment.MiddleRight;
            btnDownRight.UseVisualStyleBackColor = true;
            btnDownRight.Click += btnDownRight_Click;
            // 
            // btnDownLeft
            // 
            btnDownLeft.Enabled = false;
            btnDownLeft.Location = new Point(18, 68);
            btnDownLeft.Name = "btnDownLeft";
            btnDownLeft.RightToLeft = RightToLeft.No;
            btnDownLeft.Size = new Size(25, 25);
            btnDownLeft.TabIndex = 26;
            btnDownLeft.Text = "↙";
            btnDownLeft.TextAlign = ContentAlignment.MiddleRight;
            btnDownLeft.UseVisualStyleBackColor = true;
            btnDownLeft.Click += btnDownLeft_Click;
            // 
            // btnUpLeft
            // 
            btnUpLeft.Enabled = false;
            btnUpLeft.Location = new Point(18, 20);
            btnUpLeft.Name = "btnUpLeft";
            btnUpLeft.RightToLeft = RightToLeft.No;
            btnUpLeft.Size = new Size(25, 25);
            btnUpLeft.TabIndex = 25;
            btnUpLeft.Text = "↖";
            btnUpLeft.TextAlign = ContentAlignment.MiddleRight;
            btnUpLeft.UseVisualStyleBackColor = true;
            btnUpLeft.Click += btnUpLeft_Click;
            // 
            // btnUp
            // 
            btnUp.Enabled = false;
            btnUp.Location = new Point(42, 20);
            btnUp.Name = "btnUp";
            btnUp.RightToLeft = RightToLeft.No;
            btnUp.Size = new Size(25, 25);
            btnUp.TabIndex = 24;
            btnUp.Text = "↑";
            btnUp.TextAlign = ContentAlignment.BottomCenter;
            btnUp.UseVisualStyleBackColor = true;
            btnUp.Click += btnUp_Click;
            // 
            // btnDown
            // 
            btnDown.Enabled = false;
            btnDown.Location = new Point(42, 68);
            btnDown.Name = "btnDown";
            btnDown.RightToLeft = RightToLeft.No;
            btnDown.Size = new Size(25, 25);
            btnDown.TabIndex = 23;
            btnDown.Text = "↓";
            btnDown.TextAlign = ContentAlignment.TopCenter;
            btnDown.UseVisualStyleBackColor = true;
            btnDown.Click += btnDown_Click;
            // 
            // btnLeft
            // 
            btnLeft.Enabled = false;
            btnLeft.Location = new Point(18, 44);
            btnLeft.Name = "btnLeft";
            btnLeft.Size = new Size(25, 25);
            btnLeft.TabIndex = 22;
            btnLeft.Text = "←";
            btnLeft.TextAlign = ContentAlignment.TopCenter;
            btnLeft.UseVisualStyleBackColor = true;
            btnLeft.Click += btnLeft_Click;
            // 
            // btnRigth
            // 
            btnRigth.Enabled = false;
            btnRigth.Location = new Point(66, 44);
            btnRigth.Name = "btnRigth";
            btnRigth.Size = new Size(25, 25);
            btnRigth.TabIndex = 21;
            btnRigth.Text = "→";
            btnRigth.TextAlign = ContentAlignment.TopCenter;
            btnRigth.UseVisualStyleBackColor = true;
            btnRigth.Click += btnRigth_Click;
            // 
            // backgroundWorker1
            // 
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            // 
            // btnCapture
            // 
            btnCapture.Location = new Point(12, 513);
            btnCapture.Name = "btnCapture";
            btnCapture.Size = new Size(80, 60);
            btnCapture.TabIndex = 20;
            btnCapture.Text = "Capture";
            btnCapture.UseVisualStyleBackColor = true;
            btnCapture.Click += btnCapture_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(66, 19);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(49, 23);
            btnDelete.TabIndex = 21;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnCreateImage
            // 
            btnCreateImage.Enabled = false;
            btnCreateImage.Location = new Point(464, 206);
            btnCreateImage.Name = "btnCreateImage";
            btnCreateImage.Size = new Size(91, 23);
            btnCreateImage.TabIndex = 22;
            btnCreateImage.Text = "Create Image";
            btnCreateImage.UseVisualStyleBackColor = true;
            btnCreateImage.Click += btnCreateImage_Click;
            // 
            // btnWindow
            // 
            btnWindow.Location = new Point(489, 460);
            btnWindow.Name = "btnWindow";
            btnWindow.Size = new Size(102, 66);
            btnWindow.TabIndex = 23;
            btnWindow.Text = "Window";
            btnWindow.UseVisualStyleBackColor = true;
            btnWindow.Click += btnWindow_Click;
            // 
            // ckColor
            // 
            ckColor.AutoSize = true;
            ckColor.Enabled = false;
            ckColor.Location = new Point(386, 119);
            ckColor.Name = "ckColor";
            ckColor.Size = new Size(55, 19);
            ckColor.TabIndex = 29;
            ckColor.Text = "Color";
            ckColor.UseVisualStyleBackColor = true;
            ckColor.CheckedChanged += ckColor_CheckedChanged;
            // 
            // tbR
            // 
            tbR.Enabled = false;
            tbR.Location = new Point(222, 154);
            tbR.Name = "tbR";
            tbR.Size = new Size(85, 23);
            tbR.TabIndex = 32;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(177, 157);
            label8.Name = "label8";
            label8.Size = new Size(36, 15);
            label8.TabIndex = 39;
            label8.Text = "Color";
            // 
            // cbTest
            // 
            cbTest.AutoSize = true;
            cbTest.Location = new Point(125, 23);
            cbTest.Name = "cbTest";
            cbTest.Size = new Size(46, 19);
            cbTest.TabIndex = 44;
            cbTest.Text = "Test";
            cbTest.UseVisualStyleBackColor = true;
            // 
            // ckBoard
            // 
            ckBoard.AutoSize = true;
            ckBoard.Enabled = false;
            ckBoard.Location = new Point(386, 94);
            ckBoard.Name = "ckBoard";
            ckBoard.Size = new Size(57, 19);
            ckBoard.TabIndex = 45;
            ckBoard.Text = "Board";
            ckBoard.UseVisualStyleBackColor = true;
            ckBoard.CheckedChanged += ckBoard_CheckedChanged;
            // 
            // btnCreateFont
            // 
            btnCreateFont.Location = new Point(464, 235);
            btnCreateFont.Name = "btnCreateFont";
            btnCreateFont.Size = new Size(91, 23);
            btnCreateFont.TabIndex = 47;
            btnCreateFont.Text = "Create Font";
            btnCreateFont.UseVisualStyleBackColor = true;
            btnCreateFont.Click += btnCreateFont_Click;
            // 
            // lbAction
            // 
            lbAction.AutoSize = true;
            lbAction.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            lbAction.Location = new Point(257, 457);
            lbAction.Name = "lbAction";
            lbAction.Size = new Size(57, 21);
            lbAction.TabIndex = 55;
            lbAction.Text = "label9";
            // 
            // pbCard1
            // 
            pbCard1.Location = new Point(308, 400);
            pbCard1.Name = "pbCard1";
            pbCard1.Size = new Size(20, 35);
            pbCard1.TabIndex = 57;
            pbCard1.TabStop = false;
            // 
            // pbCard0
            // 
            pbCard0.Location = new Point(277, 400);
            pbCard0.Name = "pbCard0";
            pbCard0.Size = new Size(20, 35);
            pbCard0.TabIndex = 56;
            pbCard0.TabStop = false;
            // 
            // tbResumen
            // 
            tbResumen.Location = new Point(177, 183);
            tbResumen.Multiline = true;
            tbResumen.Name = "tbResumen";
            tbResumen.Size = new Size(251, 207);
            tbResumen.TabIndex = 60;
            // 
            // btnCapture3bet
            // 
            btnCapture3bet.Location = new Point(98, 513);
            btnCapture3bet.Name = "btnCapture3bet";
            btnCapture3bet.Size = new Size(80, 60);
            btnCapture3bet.TabIndex = 61;
            btnCapture3bet.Text = "vs 3Bet";
            btnCapture3bet.UseVisualStyleBackColor = true;
            btnCapture3bet.Click += btnCapture3bet_Click;
            // 
            // btnCapture4Bet
            // 
            btnCapture4Bet.Location = new Point(184, 513);
            btnCapture4Bet.Name = "btnCapture4Bet";
            btnCapture4Bet.Size = new Size(80, 60);
            btnCapture4Bet.TabIndex = 62;
            btnCapture4Bet.Text = "vs 4Bet";
            btnCapture4Bet.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(593, 585);
            Controls.Add(btnCapture4Bet);
            Controls.Add(btnCapture3bet);
            Controls.Add(tbResumen);
            Controls.Add(pbCard1);
            Controls.Add(pbCard0);
            Controls.Add(lbAction);
            Controls.Add(btnCreateFont);
            Controls.Add(ckBoard);
            Controls.Add(cbTest);
            Controls.Add(label8);
            Controls.Add(btnCapture);
            Controls.Add(btnWindow);
            Controls.Add(tbR);
            Controls.Add(ckColor);
            Controls.Add(btnCreateImage);
            Controls.Add(btnDelete);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(btnLoadMap);
            Controls.Add(btnSaveMap);
            Controls.Add(cbSpeed);
            Controls.Add(btnNew);
            Controls.Add(twRegions);
            Name = "Form1";
            StartPosition = FormStartPosition.Manual;
            Text = "Form1";
            Load += Form1_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pbCard1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbCard0).EndInit();
            ResumeLayout(false);
            PerformLayout();
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
        private Button btnCapture;
        private Button btnDelete;
        private Button btnCreateImage;
        private Button btnWindow;
        private Label lbXY;
        private CheckBox ckColor;
        private TextBox tbR;
        private Label label8;
        private CheckBox cbTest;
        private CheckBox ckBoard;
        private Button btnCreateFont;
        private Label label7;
        private Label label3;
        private TextBox tbY;
        private TextBox tbX;
        private Label lbAction;
        private PictureBox pbCard1;
        private PictureBox pbCard0;
        private Label label6;
        private Label label9;
        private TextBox tbResumen;
        private Button btnCapture3bet;
        private Button btnCapture4Bet;
    }
}