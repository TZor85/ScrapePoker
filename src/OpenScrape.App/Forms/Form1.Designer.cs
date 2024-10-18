using Emgu.CV.Aruco;
using OpenScrape.App.Models;

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
            TreeNode treeNode1 = new TreeNode("Regions");
            TreeNode treeNode2 = new TreeNode("Board");
            TreeNode treeNode3 = new TreeNode("Images");
            TreeNode treeNode4 = new TreeNode("Fonts");
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            tbControl = new TabControl();
            tabPage1 = new TabPage();
            tbResumen = new TextBox();
            ckBoard = new CheckBox();
            label8 = new Label();
            tbR = new TextBox();
            ckColor = new CheckBox();
            groupBox1 = new GroupBox();
            lbXY = new Label();
            tbHeight = new TextBox();
            label2 = new Label();
            label1 = new Label();
            tbWidth = new TextBox();
            btnPlusWidth = new Button();
            btnMinusWidth = new Button();
            btnPlusHeight = new Button();
            btnMinusHeight = new Button();
            cbSpeed = new ComboBox();
            btnCreateFont = new Button();
            btnCreateImage = new Button();
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
            btnLoadMap = new Button();
            btnSaveMap = new Button();
            cbMark = new CheckBox();
            btnWindow = new Button();
            btnCapture4Bet = new Button();
            btnCapture3bet = new Button();
            pbCard1 = new PictureBox();
            pbCard0 = new PictureBox();
            lbAction = new Label();
            btnCapture = new Button();
            gbTest = new GroupBox();
            cbRiver = new CheckBox();
            cbTurn = new CheckBox();
            cbFlop = new CheckBox();
            cbTest = new CheckBox();
            btnDelete = new Button();
            btnNew = new Button();
            twRegions = new TreeView();
            tabPage2 = new TabPage();
            tbResume = new TextBox();
            pictureBox1 = new PictureBox();
            label4 = new Label();
            tbControl.SuspendLayout();
            tabPage1.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbCard1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbCard0).BeginInit();
            gbTest.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // backgroundWorker1
            // 
            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            // 
            // tbControl
            // 
            tbControl.Controls.Add(tabPage1);
            tbControl.Controls.Add(tabPage2);
            tbControl.Location = new Point(12, 12);
            tbControl.Name = "tbControl";
            tbControl.SelectedIndex = 0;
            tbControl.Size = new Size(616, 621);
            tbControl.TabIndex = 65;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(tbResumen);
            tabPage1.Controls.Add(ckBoard);
            tabPage1.Controls.Add(label8);
            tabPage1.Controls.Add(tbR);
            tabPage1.Controls.Add(ckColor);
            tabPage1.Controls.Add(groupBox1);
            tabPage1.Controls.Add(cbSpeed);
            tabPage1.Controls.Add(btnCreateFont);
            tabPage1.Controls.Add(btnCreateImage);
            tabPage1.Controls.Add(groupBox2);
            tabPage1.Controls.Add(btnLoadMap);
            tabPage1.Controls.Add(btnSaveMap);
            tabPage1.Controls.Add(cbMark);
            tabPage1.Controls.Add(btnWindow);
            tabPage1.Controls.Add(btnCapture4Bet);
            tabPage1.Controls.Add(btnCapture3bet);
            tabPage1.Controls.Add(pbCard1);
            tabPage1.Controls.Add(pbCard0);
            tabPage1.Controls.Add(lbAction);
            tabPage1.Controls.Add(btnCapture);
            tabPage1.Controls.Add(gbTest);
            tabPage1.Controls.Add(cbTest);
            tabPage1.Controls.Add(btnDelete);
            tabPage1.Controls.Add(btnNew);
            tabPage1.Controls.Add(twRegions);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(608, 593);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Configurar";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tbResumen
            // 
            tbResumen.Location = new Point(186, 147);
            tbResumen.Multiline = true;
            tbResumen.Name = "tbResumen";
            tbResumen.ReadOnly = true;
            tbResumen.Size = new Size(251, 242);
            tbResumen.TabIndex = 85;
            // 
            // ckBoard
            // 
            ckBoard.AutoSize = true;
            ckBoard.Enabled = false;
            ckBoard.Location = new Point(395, 53);
            ckBoard.Name = "ckBoard";
            ckBoard.Size = new Size(57, 19);
            ckBoard.TabIndex = 84;
            ckBoard.Text = "Board";
            ckBoard.UseVisualStyleBackColor = true;
            ckBoard.CheckedChanged += ckBoard_CheckedChanged;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(186, 115);
            label8.Name = "label8";
            label8.Size = new Size(36, 15);
            label8.TabIndex = 83;
            label8.Text = "Color";
            // 
            // tbR
            // 
            tbR.Enabled = false;
            tbR.Location = new Point(231, 112);
            tbR.Name = "tbR";
            tbR.Size = new Size(71, 23);
            tbR.TabIndex = 82;
            // 
            // ckColor
            // 
            ckColor.AutoSize = true;
            ckColor.Enabled = false;
            ckColor.Location = new Point(395, 78);
            ckColor.Name = "ckColor";
            ckColor.Size = new Size(55, 19);
            ckColor.TabIndex = 81;
            ckColor.Text = "Color";
            ckColor.UseVisualStyleBackColor = true;
            ckColor.CheckedChanged += ckColor_CheckedChanged;
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
            groupBox1.Location = new Point(186, 8);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(203, 100);
            groupBox1.TabIndex = 80;
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
            cbSpeed.Location = new Point(395, 17);
            cbSpeed.Name = "cbSpeed";
            cbSpeed.Size = new Size(42, 23);
            cbSpeed.TabIndex = 79;
            cbSpeed.SelectedIndexChanged += cbSpeed_SelectedIndexChanged;
            // 
            // btnCreateFont
            // 
            btnCreateFont.Location = new Point(473, 186);
            btnCreateFont.Name = "btnCreateFont";
            btnCreateFont.Size = new Size(91, 23);
            btnCreateFont.TabIndex = 78;
            btnCreateFont.Text = "Create Font";
            btnCreateFont.UseVisualStyleBackColor = true;
            btnCreateFont.Click += btnCreateFont_Click;
            // 
            // btnCreateImage
            // 
            btnCreateImage.Enabled = false;
            btnCreateImage.Location = new Point(473, 157);
            btnCreateImage.Name = "btnCreateImage";
            btnCreateImage.Size = new Size(91, 23);
            btnCreateImage.TabIndex = 77;
            btnCreateImage.Text = "Create Image";
            btnCreateImage.UseVisualStyleBackColor = true;
            btnCreateImage.Click += btnCreateImage_Click;
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
            groupBox2.Location = new Point(473, 6);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(107, 143);
            groupBox2.TabIndex = 76;
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
            // btnLoadMap
            // 
            btnLoadMap.Location = new Point(443, 364);
            btnLoadMap.Name = "btnLoadMap";
            btnLoadMap.Size = new Size(75, 23);
            btnLoadMap.TabIndex = 75;
            btnLoadMap.Text = "Load Map";
            btnLoadMap.UseVisualStyleBackColor = true;
            btnLoadMap.Click += btnLoadMap_Click;
            // 
            // btnSaveMap
            // 
            btnSaveMap.Location = new Point(525, 364);
            btnSaveMap.Name = "btnSaveMap";
            btnSaveMap.Size = new Size(75, 23);
            btnSaveMap.TabIndex = 74;
            btnSaveMap.Text = "Save Map";
            btnSaveMap.UseVisualStyleBackColor = true;
            btnSaveMap.Click += btnSaveMap_Click;
            // 
            // cbMark
            // 
            cbMark.AutoSize = true;
            cbMark.Location = new Point(384, 548);
            cbMark.Name = "cbMark";
            cbMark.Size = new Size(97, 19);
            cbMark.TabIndex = 73;
            cbMark.Text = "Marcar Mano";
            cbMark.UseVisualStyleBackColor = true;
            // 
            // btnWindow
            // 
            btnWindow.Location = new Point(500, 501);
            btnWindow.Name = "btnWindow";
            btnWindow.Size = new Size(102, 66);
            btnWindow.TabIndex = 72;
            btnWindow.Text = "Window";
            btnWindow.UseVisualStyleBackColor = true;
            btnWindow.Click += btnWindow_Click;
            // 
            // btnCapture4Bet
            // 
            btnCapture4Bet.Location = new Point(183, 512);
            btnCapture4Bet.Name = "btnCapture4Bet";
            btnCapture4Bet.Size = new Size(80, 60);
            btnCapture4Bet.TabIndex = 71;
            btnCapture4Bet.Text = "vs 4Bet";
            btnCapture4Bet.UseVisualStyleBackColor = true;
            // 
            // btnCapture3bet
            // 
            btnCapture3bet.Location = new Point(97, 512);
            btnCapture3bet.Name = "btnCapture3bet";
            btnCapture3bet.Size = new Size(80, 60);
            btnCapture3bet.TabIndex = 70;
            btnCapture3bet.Text = "vs 3Bet";
            btnCapture3bet.UseVisualStyleBackColor = true;
            // 
            // pbCard1
            // 
            pbCard1.Location = new Point(307, 399);
            pbCard1.Name = "pbCard1";
            pbCard1.Size = new Size(20, 35);
            pbCard1.TabIndex = 69;
            pbCard1.TabStop = false;
            // 
            // pbCard0
            // 
            pbCard0.Location = new Point(276, 399);
            pbCard0.Name = "pbCard0";
            pbCard0.Size = new Size(20, 35);
            pbCard0.TabIndex = 68;
            pbCard0.TabStop = false;
            // 
            // lbAction
            // 
            lbAction.AutoSize = true;
            lbAction.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            lbAction.Location = new Point(241, 459);
            lbAction.Name = "lbAction";
            lbAction.Size = new Size(57, 21);
            lbAction.TabIndex = 67;
            lbAction.Text = "label9";
            // 
            // btnCapture
            // 
            btnCapture.Location = new Point(11, 512);
            btnCapture.Name = "btnCapture";
            btnCapture.Size = new Size(80, 60);
            btnCapture.TabIndex = 66;
            btnCapture.Text = "Capture";
            btnCapture.UseVisualStyleBackColor = true;
            btnCapture.Click += btnCapture_Click;
            // 
            // gbTest
            // 
            gbTest.Controls.Add(cbRiver);
            gbTest.Controls.Add(cbTurn);
            gbTest.Controls.Add(cbFlop);
            gbTest.Enabled = false;
            gbTest.Location = new Point(6, 383);
            gbTest.Name = "gbTest";
            gbTest.Size = new Size(103, 99);
            gbTest.TabIndex = 65;
            gbTest.TabStop = false;
            gbTest.Text = "Test";
            // 
            // cbRiver
            // 
            cbRiver.AutoSize = true;
            cbRiver.Location = new Point(6, 72);
            cbRiver.Name = "cbRiver";
            cbRiver.Size = new Size(52, 19);
            cbRiver.TabIndex = 47;
            cbRiver.Text = "River";
            cbRiver.UseVisualStyleBackColor = true;
            // 
            // cbTurn
            // 
            cbTurn.AutoSize = true;
            cbTurn.Location = new Point(6, 47);
            cbTurn.Name = "cbTurn";
            cbTurn.Size = new Size(50, 19);
            cbTurn.TabIndex = 46;
            cbTurn.Text = "Turn";
            cbTurn.UseVisualStyleBackColor = true;
            // 
            // cbFlop
            // 
            cbFlop.AutoSize = true;
            cbFlop.Location = new Point(6, 22);
            cbFlop.Name = "cbFlop";
            cbFlop.Size = new Size(49, 19);
            cbFlop.TabIndex = 45;
            cbFlop.Text = "Flop";
            cbFlop.UseVisualStyleBackColor = true;
            cbFlop.CheckedChanged += cbFlop_CheckedChanged;
            // 
            // cbTest
            // 
            cbTest.AutoSize = true;
            cbTest.Location = new Point(119, 10);
            cbTest.Name = "cbTest";
            cbTest.Size = new Size(46, 19);
            cbTest.TabIndex = 48;
            cbTest.Text = "Test";
            cbTest.UseVisualStyleBackColor = true;
            cbTest.CheckedChanged += cbTest_CheckedChanged;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(60, 6);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(49, 23);
            btnDelete.TabIndex = 47;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnNew
            // 
            btnNew.Location = new Point(6, 6);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(48, 23);
            btnNew.TabIndex = 46;
            btnNew.Text = "New";
            btnNew.UseVisualStyleBackColor = true;
            btnNew.Click += btnNew_Click;
            // 
            // twRegions
            // 
            twRegions.Location = new Point(6, 35);
            twRegions.Name = "twRegions";
            treeNode1.Name = "Nodo0";
            treeNode1.NodeFont = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            treeNode1.Text = "Regions";
            treeNode2.Name = "Nodo1";
            treeNode2.NodeFont = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            treeNode2.Text = "Board";
            treeNode3.Name = "Nodo2";
            treeNode3.NodeFont = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            treeNode3.Text = "Images";
            treeNode4.Name = "Nodo3";
            treeNode4.NodeFont = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            treeNode4.Text = "Fonts";
            twRegions.Nodes.AddRange(new TreeNode[] { treeNode1, treeNode2, treeNode3, treeNode4 });
            twRegions.Size = new Size(159, 342);
            twRegions.TabIndex = 45;
            twRegions.AfterSelect += twRegions_AfterSelect;
            twRegions.DoubleClick += twRegions_DoubleClick;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(tbResume);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(608, 593);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Logs";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // tbResume
            // 
            tbResume.Dock = DockStyle.Fill;
            tbResume.Enabled = false;
            tbResume.Location = new Point(3, 3);
            tbResume.Multiline = true;
            tbResume.Name = "tbResume";
            tbResume.ReadOnly = true;
            tbResume.Size = new Size(602, 587);
            tbResume.TabIndex = 1;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(634, 193);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(302, 192);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 66;
            pictureBox1.TabStop = false;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 16F, FontStyle.Regular, GraphicsUnit.Point);
            label4.Location = new Point(648, 435);
            label4.Name = "label4";
            label4.Size = new Size(71, 30);
            label4.TabIndex = 67;
            label4.Text = "label4";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(630, 635);
            Controls.Add(label4);
            Controls.Add(pictureBox1);
            Controls.Add(tbControl);
            Name = "Form1";
            StartPosition = FormStartPosition.Manual;
            Text = "Form1";
            Load += Form1_Load;
            tbControl.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pbCard1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbCard0).EndInit();
            gbTest.ResumeLayout(false);
            gbTest.PerformLayout();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private Label label6;
        private Label label9;
        private TabControl tbControl;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private CheckBox cbMark;
        private Button btnWindow;
        private Button btnCapture4Bet;
        private Button btnCapture3bet;
        private PictureBox pbCard1;
        private PictureBox pbCard0;
        private Label lbAction;
        private Button btnCapture;
        private GroupBox gbTest;
        private CheckBox cbRiver;
        private CheckBox cbTurn;
        private CheckBox cbFlop;
        private CheckBox cbTest;
        private Button btnDelete;
        private Button btnNew;
        private TreeView twRegions;
        private TextBox tbResumen;
        private CheckBox ckBoard;
        private Label label8;
        private TextBox tbR;
        private CheckBox ckColor;
        private GroupBox groupBox1;
        private Label lbXY;
        private TextBox tbHeight;
        private Label label2;
        private Label label1;
        private TextBox tbWidth;
        private Button btnPlusWidth;
        private Button btnMinusWidth;
        private Button btnPlusHeight;
        private Button btnMinusHeight;
        private ComboBox cbSpeed;
        private Button btnCreateFont;
        private Button btnCreateImage;
        private GroupBox groupBox2;
        private Label label7;
        private Label label3;
        private TextBox tbY;
        private TextBox tbX;
        private Button btnUpRight;
        private Button btnDownRight;
        private Button btnDownLeft;
        private Button btnUpLeft;
        private Button btnUp;
        private Button btnDown;
        private Button btnLeft;
        private Button btnRigth;
        private Button btnLoadMap;
        private Button btnSaveMap;
        private TextBox tbResume;
        private PictureBox pictureBox1;
        private Label label4;
    }
}