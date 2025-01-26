using Emgu.CV.Aruco;
using OpenScrape.App.Models;

namespace OpenScrape.App
{
    partial class FrmMain
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
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            tbControl = new TabControl();
            tbJuego = new TabPage();
            tbConfig = new TabPage();
            groupBox1 = new GroupBox();
            tbTestTexto = new TextBox();
            btnTestTexto = new Button();
            pbColorDebug = new PictureBox();
            tbTestColor = new TextBox();
            btnTestColor = new Button();
            rgRegion = new GroupBox();
            pbRegionColor = new PictureBox();
            tbRegionInactUmbral = new TextBox();
            tbRegionUmbral = new TextBox();
            cbRegionNumber = new CheckBox();
            cbSpeed = new ComboBox();
            btnUpRight = new Button();
            btnDownRight = new Button();
            btnDownLeft = new Button();
            btnUpLeft = new Button();
            btnUp = new Button();
            btnDown = new Button();
            btnLeft = new Button();
            btnRigth = new Button();
            btnPlusWidth = new Button();
            btnMinusWidth = new Button();
            btnPlusHeight = new Button();
            btnMinusHeight = new Button();
            tbRegionName = new TextBox();
            tbColor = new TextBox();
            cbRegionBoard = new CheckBox();
            cbRegionHash = new CheckBox();
            cbRegionColor = new CheckBox();
            label7 = new Label();
            tbHeight = new TextBox();
            label2 = new Label();
            tbWidth = new TextBox();
            label1 = new Label();
            tbY = new TextBox();
            label3 = new Label();
            tbX = new TextBox();
            btnCreateFont = new Button();
            btnCreateImage = new Button();
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
            twRegionsConfig = new TreeView();
            tbTables = new TabPage();
            dgvHands = new DataGridView();
            twTables = new TreeView();
            tbLogs = new TabPage();
            tbResume = new TextBox();
            pictureBox1 = new PictureBox();
            label4 = new Label();
            btnTestCarta = new Button();
            pbTestCarta = new PictureBox();
            tbControl.SuspendLayout();
            tbConfig.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbColorDebug).BeginInit();
            rgRegion.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbRegionColor).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbCard1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbCard0).BeginInit();
            gbTest.SuspendLayout();
            tbTables.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHands).BeginInit();
            tbLogs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbTestCarta).BeginInit();
            SuspendLayout();
            // 
            // backgroundWorker1
            // 
            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            // 
            // tbControl
            // 
            tbControl.Controls.Add(tbJuego);
            tbControl.Controls.Add(tbConfig);
            tbControl.Controls.Add(tbTables);
            tbControl.Controls.Add(tbLogs);
            tbControl.Dock = DockStyle.Fill;
            tbControl.Location = new Point(0, 0);
            tbControl.Name = "tbControl";
            tbControl.SelectedIndex = 0;
            tbControl.Size = new Size(679, 635);
            tbControl.TabIndex = 65;
            // 
            // tbJuego
            // 
            tbJuego.Location = new Point(4, 24);
            tbJuego.Name = "tbJuego";
            tbJuego.Size = new Size(671, 607);
            tbJuego.TabIndex = 2;
            tbJuego.Text = "Juego";
            tbJuego.UseVisualStyleBackColor = true;
            // 
            // tbConfig
            // 
            tbConfig.Controls.Add(groupBox1);
            tbConfig.Controls.Add(rgRegion);
            tbConfig.Controls.Add(btnCreateFont);
            tbConfig.Controls.Add(btnCreateImage);
            tbConfig.Controls.Add(btnLoadMap);
            tbConfig.Controls.Add(btnSaveMap);
            tbConfig.Controls.Add(cbMark);
            tbConfig.Controls.Add(btnWindow);
            tbConfig.Controls.Add(btnCapture4Bet);
            tbConfig.Controls.Add(btnCapture3bet);
            tbConfig.Controls.Add(pbCard1);
            tbConfig.Controls.Add(pbCard0);
            tbConfig.Controls.Add(lbAction);
            tbConfig.Controls.Add(btnCapture);
            tbConfig.Controls.Add(gbTest);
            tbConfig.Controls.Add(cbTest);
            tbConfig.Controls.Add(btnDelete);
            tbConfig.Controls.Add(btnNew);
            tbConfig.Controls.Add(twRegionsConfig);
            tbConfig.Location = new Point(4, 24);
            tbConfig.Name = "tbConfig";
            tbConfig.Padding = new Padding(3);
            tbConfig.Size = new Size(671, 607);
            tbConfig.TabIndex = 0;
            tbConfig.Text = "Configurar";
            tbConfig.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(pbTestCarta);
            groupBox1.Controls.Add(btnTestCarta);
            groupBox1.Controls.Add(tbTestTexto);
            groupBox1.Controls.Add(btnTestTexto);
            groupBox1.Controls.Add(pbColorDebug);
            groupBox1.Controls.Add(tbTestColor);
            groupBox1.Controls.Add(btnTestColor);
            groupBox1.Location = new Point(384, 35);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(200, 214);
            groupBox1.TabIndex = 85;
            groupBox1.TabStop = false;
            groupBox1.Text = "Test Region";
            // 
            // tbTestTexto
            // 
            tbTestTexto.Location = new Point(68, 51);
            tbTestTexto.Name = "tbTestTexto";
            tbTestTexto.Size = new Size(126, 23);
            tbTestTexto.TabIndex = 88;
            // 
            // btnTestTexto
            // 
            btnTestTexto.Enabled = false;
            btnTestTexto.Location = new Point(6, 51);
            btnTestTexto.Name = "btnTestTexto";
            btnTestTexto.Size = new Size(56, 23);
            btnTestTexto.TabIndex = 87;
            btnTestTexto.Text = "Texto";
            btnTestTexto.UseVisualStyleBackColor = true;
            btnTestTexto.Click += btnTestTexto_Click;
            // 
            // pbColorDebug
            // 
            pbColorDebug.Location = new Point(174, 25);
            pbColorDebug.Name = "pbColorDebug";
            pbColorDebug.Size = new Size(20, 20);
            pbColorDebug.TabIndex = 86;
            pbColorDebug.TabStop = false;
            // 
            // tbTestColor
            // 
            tbTestColor.Location = new Point(68, 22);
            tbTestColor.Name = "tbTestColor";
            tbTestColor.Size = new Size(56, 23);
            tbTestColor.TabIndex = 85;
            // 
            // btnTestColor
            // 
            btnTestColor.Enabled = false;
            btnTestColor.Location = new Point(6, 21);
            btnTestColor.Name = "btnTestColor";
            btnTestColor.Size = new Size(56, 23);
            btnTestColor.TabIndex = 0;
            btnTestColor.Text = "Color";
            btnTestColor.UseVisualStyleBackColor = true;
            btnTestColor.Click += btnTestColor_Click;
            // 
            // rgRegion
            // 
            rgRegion.Controls.Add(pbRegionColor);
            rgRegion.Controls.Add(tbRegionInactUmbral);
            rgRegion.Controls.Add(tbRegionUmbral);
            rgRegion.Controls.Add(cbRegionNumber);
            rgRegion.Controls.Add(cbSpeed);
            rgRegion.Controls.Add(btnUpRight);
            rgRegion.Controls.Add(btnDownRight);
            rgRegion.Controls.Add(btnDownLeft);
            rgRegion.Controls.Add(btnUpLeft);
            rgRegion.Controls.Add(btnUp);
            rgRegion.Controls.Add(btnDown);
            rgRegion.Controls.Add(btnLeft);
            rgRegion.Controls.Add(btnRigth);
            rgRegion.Controls.Add(btnPlusWidth);
            rgRegion.Controls.Add(btnMinusWidth);
            rgRegion.Controls.Add(btnPlusHeight);
            rgRegion.Controls.Add(btnMinusHeight);
            rgRegion.Controls.Add(tbRegionName);
            rgRegion.Controls.Add(tbColor);
            rgRegion.Controls.Add(cbRegionBoard);
            rgRegion.Controls.Add(cbRegionHash);
            rgRegion.Controls.Add(cbRegionColor);
            rgRegion.Controls.Add(label7);
            rgRegion.Controls.Add(tbHeight);
            rgRegion.Controls.Add(label2);
            rgRegion.Controls.Add(tbWidth);
            rgRegion.Controls.Add(label1);
            rgRegion.Controls.Add(tbY);
            rgRegion.Controls.Add(label3);
            rgRegion.Controls.Add(tbX);
            rgRegion.Location = new Point(176, 35);
            rgRegion.Name = "rgRegion";
            rgRegion.Size = new Size(200, 342);
            rgRegion.TabIndex = 84;
            rgRegion.TabStop = false;
            rgRegion.Text = "Region";
            // 
            // pbRegionColor
            // 
            pbRegionColor.Location = new Point(95, 270);
            pbRegionColor.Name = "pbRegionColor";
            pbRegionColor.Size = new Size(20, 20);
            pbRegionColor.TabIndex = 102;
            pbRegionColor.TabStop = false;
            // 
            // tbRegionInactUmbral
            // 
            tbRegionInactUmbral.Location = new Point(117, 306);
            tbRegionInactUmbral.Name = "tbRegionInactUmbral";
            tbRegionInactUmbral.PlaceholderText = "I. Umbral";
            tbRegionInactUmbral.Size = new Size(71, 23);
            tbRegionInactUmbral.TabIndex = 101;
            // 
            // tbRegionUmbral
            // 
            tbRegionUmbral.Location = new Point(5, 306);
            tbRegionUmbral.Name = "tbRegionUmbral";
            tbRegionUmbral.PlaceholderText = "Umbral";
            tbRegionUmbral.Size = new Size(71, 23);
            tbRegionUmbral.TabIndex = 100;
            // 
            // cbRegionNumber
            // 
            cbRegionNumber.AutoSize = true;
            cbRegionNumber.CheckAlign = ContentAlignment.MiddleRight;
            cbRegionNumber.FlatStyle = FlatStyle.System;
            cbRegionNumber.Location = new Point(101, 243);
            cbRegionNumber.Name = "cbRegionNumber";
            cbRegionNumber.RightToLeft = RightToLeft.No;
            cbRegionNumber.Size = new Size(87, 20);
            cbRegionNumber.TabIndex = 99;
            cbRegionNumber.Text = "Is Number";
            cbRegionNumber.UseVisualStyleBackColor = true;
            // 
            // cbSpeed
            // 
            cbSpeed.FormattingEnabled = true;
            cbSpeed.Items.AddRange(new object[] { "1", "2", "5", "10", "20", "30", "40", "50" });
            cbSpeed.Location = new Point(146, 141);
            cbSpeed.Name = "cbSpeed";
            cbSpeed.Size = new Size(42, 23);
            cbSpeed.TabIndex = 98;
            // 
            // btnUpRight
            // 
            btnUpRight.Enabled = false;
            btnUpRight.Location = new Point(90, 141);
            btnUpRight.Name = "btnUpRight";
            btnUpRight.RightToLeft = RightToLeft.No;
            btnUpRight.Size = new Size(25, 25);
            btnUpRight.TabIndex = 97;
            btnUpRight.Text = "↗";
            btnUpRight.TextAlign = ContentAlignment.MiddleRight;
            btnUpRight.UseVisualStyleBackColor = true;
            // 
            // btnDownRight
            // 
            btnDownRight.Enabled = false;
            btnDownRight.Location = new Point(90, 189);
            btnDownRight.Name = "btnDownRight";
            btnDownRight.RightToLeft = RightToLeft.No;
            btnDownRight.Size = new Size(25, 25);
            btnDownRight.TabIndex = 96;
            btnDownRight.Text = "↘";
            btnDownRight.TextAlign = ContentAlignment.MiddleRight;
            btnDownRight.UseVisualStyleBackColor = true;
            // 
            // btnDownLeft
            // 
            btnDownLeft.Enabled = false;
            btnDownLeft.Location = new Point(42, 189);
            btnDownLeft.Name = "btnDownLeft";
            btnDownLeft.RightToLeft = RightToLeft.No;
            btnDownLeft.Size = new Size(25, 25);
            btnDownLeft.TabIndex = 95;
            btnDownLeft.Text = "↙";
            btnDownLeft.TextAlign = ContentAlignment.MiddleRight;
            btnDownLeft.UseVisualStyleBackColor = true;
            // 
            // btnUpLeft
            // 
            btnUpLeft.Enabled = false;
            btnUpLeft.Location = new Point(42, 141);
            btnUpLeft.Name = "btnUpLeft";
            btnUpLeft.RightToLeft = RightToLeft.No;
            btnUpLeft.Size = new Size(25, 25);
            btnUpLeft.TabIndex = 94;
            btnUpLeft.Text = "↖";
            btnUpLeft.TextAlign = ContentAlignment.MiddleRight;
            btnUpLeft.UseVisualStyleBackColor = true;
            // 
            // btnUp
            // 
            btnUp.Enabled = false;
            btnUp.Location = new Point(66, 141);
            btnUp.Name = "btnUp";
            btnUp.RightToLeft = RightToLeft.No;
            btnUp.Size = new Size(25, 25);
            btnUp.TabIndex = 93;
            btnUp.Text = "↑";
            btnUp.TextAlign = ContentAlignment.BottomCenter;
            btnUp.UseVisualStyleBackColor = true;
            // 
            // btnDown
            // 
            btnDown.Enabled = false;
            btnDown.Location = new Point(66, 189);
            btnDown.Name = "btnDown";
            btnDown.RightToLeft = RightToLeft.No;
            btnDown.Size = new Size(25, 25);
            btnDown.TabIndex = 92;
            btnDown.Text = "↓";
            btnDown.TextAlign = ContentAlignment.TopCenter;
            btnDown.UseVisualStyleBackColor = true;
            // 
            // btnLeft
            // 
            btnLeft.Enabled = false;
            btnLeft.Location = new Point(42, 165);
            btnLeft.Name = "btnLeft";
            btnLeft.Size = new Size(25, 25);
            btnLeft.TabIndex = 91;
            btnLeft.Text = "←";
            btnLeft.TextAlign = ContentAlignment.TopCenter;
            btnLeft.UseVisualStyleBackColor = true;
            // 
            // btnRigth
            // 
            btnRigth.Enabled = false;
            btnRigth.Location = new Point(90, 165);
            btnRigth.Name = "btnRigth";
            btnRigth.Size = new Size(25, 25);
            btnRigth.TabIndex = 90;
            btnRigth.Text = "→";
            btnRigth.TextAlign = ContentAlignment.TopCenter;
            btnRigth.UseVisualStyleBackColor = true;
            // 
            // btnPlusWidth
            // 
            btnPlusWidth.Enabled = false;
            btnPlusWidth.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnPlusWidth.ImageAlign = ContentAlignment.TopCenter;
            btnPlusWidth.Location = new Point(42, 72);
            btnPlusWidth.Name = "btnPlusWidth";
            btnPlusWidth.Size = new Size(25, 25);
            btnPlusWidth.TabIndex = 86;
            btnPlusWidth.Text = "+";
            btnPlusWidth.TextAlign = ContentAlignment.TopCenter;
            btnPlusWidth.UseVisualStyleBackColor = true;
            // 
            // btnMinusWidth
            // 
            btnMinusWidth.Enabled = false;
            btnMinusWidth.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnMinusWidth.Location = new Point(66, 72);
            btnMinusWidth.Name = "btnMinusWidth";
            btnMinusWidth.Size = new Size(25, 25);
            btnMinusWidth.TabIndex = 87;
            btnMinusWidth.Text = "-";
            btnMinusWidth.UseVisualStyleBackColor = true;
            // 
            // btnPlusHeight
            // 
            btnPlusHeight.Enabled = false;
            btnPlusHeight.Location = new Point(146, 71);
            btnPlusHeight.Name = "btnPlusHeight";
            btnPlusHeight.Size = new Size(25, 25);
            btnPlusHeight.TabIndex = 88;
            btnPlusHeight.Text = "+";
            btnPlusHeight.TextAlign = ContentAlignment.MiddleRight;
            btnPlusHeight.UseVisualStyleBackColor = true;
            // 
            // btnMinusHeight
            // 
            btnMinusHeight.Enabled = false;
            btnMinusHeight.Location = new Point(170, 71);
            btnMinusHeight.Name = "btnMinusHeight";
            btnMinusHeight.Size = new Size(25, 25);
            btnMinusHeight.TabIndex = 89;
            btnMinusHeight.Text = "-";
            btnMinusHeight.TextAlign = ContentAlignment.TopCenter;
            btnMinusHeight.UseVisualStyleBackColor = true;
            // 
            // tbRegionName
            // 
            tbRegionName.BorderStyle = BorderStyle.None;
            tbRegionName.Location = new Point(6, 21);
            tbRegionName.Name = "tbRegionName";
            tbRegionName.ReadOnly = true;
            tbRegionName.Size = new Size(187, 16);
            tbRegionName.TabIndex = 85;
            tbRegionName.TextAlign = HorizontalAlignment.Center;
            // 
            // tbColor
            // 
            tbColor.Location = new Point(5, 269);
            tbColor.Name = "tbColor";
            tbColor.PlaceholderText = "Color";
            tbColor.Size = new Size(71, 23);
            tbColor.TabIndex = 84;
            // 
            // cbRegionBoard
            // 
            cbRegionBoard.AutoSize = true;
            cbRegionBoard.CheckAlign = ContentAlignment.MiddleRight;
            cbRegionBoard.FlatStyle = FlatStyle.System;
            cbRegionBoard.Location = new Point(5, 243);
            cbRegionBoard.Name = "cbRegionBoard";
            cbRegionBoard.Padding = new Padding(0, 0, 13, 0);
            cbRegionBoard.RightToLeft = RightToLeft.No;
            cbRegionBoard.Size = new Size(87, 20);
            cbRegionBoard.TabIndex = 42;
            cbRegionBoard.Text = "Is Board";
            cbRegionBoard.UseVisualStyleBackColor = true;
            // 
            // cbRegionHash
            // 
            cbRegionHash.AutoSize = true;
            cbRegionHash.CheckAlign = ContentAlignment.MiddleRight;
            cbRegionHash.FlatStyle = FlatStyle.System;
            cbRegionHash.Location = new Point(101, 223);
            cbRegionHash.Name = "cbRegionHash";
            cbRegionHash.Padding = new Padding(0, 0, 17, 0);
            cbRegionHash.RightToLeft = RightToLeft.No;
            cbRegionHash.Size = new Size(87, 20);
            cbRegionHash.TabIndex = 41;
            cbRegionHash.Text = "Is Hash";
            cbRegionHash.UseVisualStyleBackColor = true;
            // 
            // cbRegionColor
            // 
            cbRegionColor.AutoSize = true;
            cbRegionColor.CheckAlign = ContentAlignment.MiddleRight;
            cbRegionColor.FlatStyle = FlatStyle.System;
            cbRegionColor.Location = new Point(5, 223);
            cbRegionColor.Name = "cbRegionColor";
            cbRegionColor.Padding = new Padding(0, 0, 15, 0);
            cbRegionColor.RightToLeft = RightToLeft.No;
            cbRegionColor.Size = new Size(87, 20);
            cbRegionColor.TabIndex = 40;
            cbRegionColor.Text = "Is Color";
            cbRegionColor.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 9F);
            label7.Location = new Point(126, 46);
            label7.Name = "label7";
            label7.Size = new Size(14, 15);
            label7.TabIndex = 39;
            label7.Text = "Y";
            // 
            // tbHeight
            // 
            tbHeight.Location = new Point(146, 106);
            tbHeight.Name = "tbHeight";
            tbHeight.Size = new Size(49, 23);
            tbHeight.TabIndex = 38;
            tbHeight.Text = "0";
            tbHeight.TextAlign = HorizontalAlignment.Right;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(100, 111);
            label2.Name = "label2";
            label2.Size = new Size(43, 15);
            label2.TabIndex = 37;
            label2.Text = "Height";
            // 
            // tbWidth
            // 
            tbWidth.Location = new Point(42, 106);
            tbWidth.Name = "tbWidth";
            tbWidth.Size = new Size(49, 23);
            tbWidth.TabIndex = 36;
            tbWidth.Text = "0";
            tbWidth.TextAlign = HorizontalAlignment.Right;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(1, 111);
            label1.Name = "label1";
            label1.Size = new Size(39, 15);
            label1.TabIndex = 35;
            label1.Text = "Width";
            // 
            // tbY
            // 
            tbY.Location = new Point(146, 43);
            tbY.Name = "tbY";
            tbY.Size = new Size(49, 23);
            tbY.TabIndex = 33;
            tbY.Text = "0";
            tbY.TextAlign = HorizontalAlignment.Right;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9F);
            label3.Location = new Point(19, 46);
            label3.Name = "label3";
            label3.Size = new Size(14, 15);
            label3.TabIndex = 32;
            label3.Text = "X";
            // 
            // tbX
            // 
            tbX.Location = new Point(42, 43);
            tbX.Name = "tbX";
            tbX.Size = new Size(49, 23);
            tbX.TabIndex = 30;
            tbX.Text = "0";
            tbX.TextAlign = HorizontalAlignment.Right;
            // 
            // btnCreateFont
            // 
            btnCreateFont.Location = new Point(384, 440);
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
            btnCreateImage.Location = new Point(384, 411);
            btnCreateImage.Name = "btnCreateImage";
            btnCreateImage.Size = new Size(91, 23);
            btnCreateImage.TabIndex = 77;
            btnCreateImage.Text = "Create Image";
            btnCreateImage.UseVisualStyleBackColor = true;
            btnCreateImage.Click += btnCreateImage_Click;
            // 
            // btnLoadMap
            // 
            btnLoadMap.Location = new Point(505, 411);
            btnLoadMap.Name = "btnLoadMap";
            btnLoadMap.Size = new Size(75, 23);
            btnLoadMap.TabIndex = 75;
            btnLoadMap.Text = "Load Map";
            btnLoadMap.UseVisualStyleBackColor = true;
            btnLoadMap.Click += btnLoadMap_Click;
            // 
            // btnSaveMap
            // 
            btnSaveMap.Location = new Point(590, 411);
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
            cbMark.Location = new Point(443, 548);
            cbMark.Name = "cbMark";
            cbMark.Size = new Size(97, 19);
            cbMark.TabIndex = 73;
            cbMark.Text = "Marcar Mano";
            cbMark.UseVisualStyleBackColor = true;
            // 
            // btnWindow
            // 
            btnWindow.Location = new Point(565, 501);
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
            lbAction.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
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
            // twRegionsConfig
            // 
            twRegionsConfig.Location = new Point(6, 35);
            twRegionsConfig.Name = "twRegionsConfig";
            twRegionsConfig.Size = new Size(159, 342);
            twRegionsConfig.TabIndex = 45;
            twRegionsConfig.DoubleClick += twRegions_DoubleClick;
            // 
            // tbTables
            // 
            tbTables.Controls.Add(dgvHands);
            tbTables.Controls.Add(twTables);
            tbTables.Location = new Point(4, 24);
            tbTables.Name = "tbTables";
            tbTables.Size = new Size(671, 607);
            tbTables.TabIndex = 3;
            tbTables.Text = "Tablas";
            tbTables.UseVisualStyleBackColor = true;
            // 
            // dgvHands
            // 
            dgvHands.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvHands.Dock = DockStyle.Fill;
            dgvHands.Location = new Point(205, 0);
            dgvHands.Name = "dgvHands";
            dgvHands.Size = new Size(466, 607);
            dgvHands.TabIndex = 1;
            // 
            // twTables
            // 
            twTables.Dock = DockStyle.Left;
            twTables.Location = new Point(0, 0);
            twTables.Name = "twTables";
            twTables.Size = new Size(205, 607);
            twTables.TabIndex = 0;
            twTables.BeforeExpand += twTables_BeforeExpand;
            twTables.DoubleClick += twTables_DoubleClick;
            // 
            // tbLogs
            // 
            tbLogs.Controls.Add(tbResume);
            tbLogs.Location = new Point(4, 24);
            tbLogs.Name = "tbLogs";
            tbLogs.Padding = new Padding(3);
            tbLogs.Size = new Size(671, 607);
            tbLogs.TabIndex = 1;
            tbLogs.Text = "Logs";
            tbLogs.UseVisualStyleBackColor = true;
            // 
            // tbResume
            // 
            tbResume.Dock = DockStyle.Fill;
            tbResume.Enabled = false;
            tbResume.Location = new Point(3, 3);
            tbResume.Multiline = true;
            tbResume.Name = "tbResume";
            tbResume.ReadOnly = true;
            tbResume.Size = new Size(665, 601);
            tbResume.TabIndex = 1;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(782, 193);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(302, 192);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 66;
            pictureBox1.TabStop = false;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 16F);
            label4.Location = new Point(777, 435);
            label4.Name = "label4";
            label4.Size = new Size(71, 30);
            label4.TabIndex = 67;
            label4.Text = "label4";
            // 
            // btnTestCarta
            // 
            btnTestCarta.Enabled = false;
            btnTestCarta.Location = new Point(6, 92);
            btnTestCarta.Name = "btnTestCarta";
            btnTestCarta.Size = new Size(56, 23);
            btnTestCarta.TabIndex = 89;
            btnTestCarta.Text = "Carta";
            btnTestCarta.UseVisualStyleBackColor = true;
            btnTestCarta.Click += btnTestCarta_Click;
            // 
            // pbTestCarta
            // 
            pbTestCarta.Location = new Point(71, 80);
            pbTestCarta.Name = "pbTestCarta";
            pbTestCarta.Size = new Size(20, 35);
            pbTestCarta.TabIndex = 90;
            pbTestCarta.TabStop = false;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(679, 635);
            Controls.Add(label4);
            Controls.Add(pictureBox1);
            Controls.Add(tbControl);
            Name = "FrmMain";
            StartPosition = FormStartPosition.Manual;
            Text = "Dealytics";
            Load += FrmMain_Load;
            tbControl.ResumeLayout(false);
            tbConfig.ResumeLayout(false);
            tbConfig.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pbColorDebug).EndInit();
            rgRegion.ResumeLayout(false);
            rgRegion.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pbRegionColor).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbCard1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbCard0).EndInit();
            gbTest.ResumeLayout(false);
            gbTest.PerformLayout();
            tbTables.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvHands).EndInit();
            tbLogs.ResumeLayout(false);
            tbLogs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbTestCarta).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private Label label6;
        private Label label9;
        private TabControl tbControl;
        private TabPage tbConfig;
        private TabPage tbLogs;
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
        private TreeView twRegionsConfig;
        private Button btnCreateFont;
        private Button btnCreateImage;
        private Button btnLoadMap;
        private Button btnSaveMap;
        private TextBox tbResume;
        private PictureBox pictureBox1;
        private Label label4;
        private TabPage tbJuego;
        private TabPage tbTables;
        private TreeView twTables;
        private DataGridView dgvHands;
        private GroupBox rgRegion;
        private TextBox tbHeight;
        private Label label2;
        private TextBox tbWidth;
        private Label label1;
        private TextBox tbY;
        private Label label3;
        private TextBox tbX;
        private Label label7;
        private CheckBox cbRegionColor;
        private CheckBox cbRegionHash;
        private CheckBox cbRegionBoard;
        private TextBox tbRegionName;
        private TextBox tbColor;
        private GroupBox groupBox1;
        private TextBox tbTestColor;
        private Button btnTestColor;
        private ComboBox cbSpeed;
        private Button btnUpRight;
        private Button btnDownRight;
        private Button btnDownLeft;
        private Button btnUpLeft;
        private Button btnUp;
        private Button btnDown;
        private Button btnLeft;
        private Button btnRigth;
        private Button btnPlusWidth;
        private Button btnMinusWidth;
        private Button btnPlusHeight;
        private Button btnMinusHeight;
        private TextBox tbRegionUmbral;
        private CheckBox cbRegionNumber;
        private TextBox tbRegionInactUmbral;
        private PictureBox pbColorDebug;
        private PictureBox pbRegionColor;
        private TextBox tbTestTexto;
        private Button btnTestTexto;
        private PictureBox pbTestCarta;
        private Button btnTestCarta;
    }
}