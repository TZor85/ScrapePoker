namespace OpenScrape.App.Forms
{
    partial class FormPlaying
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
            this.btnRiver = new System.Windows.Forms.Button();
            this.btnTurn = new System.Windows.Forms.Button();
            this.btnFlop = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.lbBoard = new System.Windows.Forms.Label();
            this.lbPosition = new System.Windows.Forms.Label();
            this.lbPair = new System.Windows.Forms.Label();
            this.pbRiver = new System.Windows.Forms.PictureBox();
            this.pbTurn = new System.Windows.Forms.PictureBox();
            this.pbFlop3 = new System.Windows.Forms.PictureBox();
            this.pbFlop2 = new System.Windows.Forms.PictureBox();
            this.pbFlop1 = new System.Windows.Forms.PictureBox();
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
            this.btnCapture = new System.Windows.Forms.Button();
            this.btnWindow = new System.Windows.Forms.Button();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.btnNew = new System.Windows.Forms.Button();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbRiver)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbTurn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbFlop3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbFlop2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbFlop1)).BeginInit();
            this.groupBox7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCard1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCard0)).BeginInit();
            this.groupBox6.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnRiver
            // 
            this.btnRiver.Location = new System.Drawing.Point(875, 375);
            this.btnRiver.Name = "btnRiver";
            this.btnRiver.Size = new System.Drawing.Size(74, 46);
            this.btnRiver.TabIndex = 58;
            this.btnRiver.Text = "River";
            this.btnRiver.UseVisualStyleBackColor = true;
            this.btnRiver.Visible = false;
            // 
            // btnTurn
            // 
            this.btnTurn.Location = new System.Drawing.Point(795, 375);
            this.btnTurn.Name = "btnTurn";
            this.btnTurn.Size = new System.Drawing.Size(74, 46);
            this.btnTurn.TabIndex = 57;
            this.btnTurn.Text = "Turn";
            this.btnTurn.UseVisualStyleBackColor = true;
            this.btnTurn.Visible = false;
            this.btnTurn.Click += new System.EventHandler(this.btnTurn_Click);
            // 
            // btnFlop
            // 
            this.btnFlop.Location = new System.Drawing.Point(715, 375);
            this.btnFlop.Name = "btnFlop";
            this.btnFlop.Size = new System.Drawing.Size(74, 46);
            this.btnFlop.TabIndex = 56;
            this.btnFlop.Text = "Flop";
            this.btnFlop.UseVisualStyleBackColor = true;
            this.btnFlop.Visible = false;
            this.btnFlop.Click += new System.EventHandler(this.btnFlop_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.lbPair);
            this.groupBox4.Controls.Add(this.lbAction);
            this.groupBox4.Controls.Add(this.lbP1Bet);
            this.groupBox4.Controls.Add(this.lbP2Bet);
            this.groupBox4.Controls.Add(this.groupBox7);
            this.groupBox4.Controls.Add(this.groupBox6);
            this.groupBox4.Controls.Add(this.groupBox5);
            this.groupBox4.Controls.Add(this.lbEfective);
            this.groupBox4.Location = new System.Drawing.Point(10, 8);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(385, 173);
            this.groupBox4.TabIndex = 55;
            this.groupBox4.TabStop = false;
            // 
            // lbBoard
            // 
            this.lbBoard.AutoSize = true;
            this.lbBoard.Location = new System.Drawing.Point(751, 215);
            this.lbBoard.Name = "lbBoard";
            this.lbBoard.Size = new System.Drawing.Size(38, 15);
            this.lbBoard.TabIndex = 19;
            this.lbBoard.Text = "Board";
            // 
            // lbPosition
            // 
            this.lbPosition.AutoSize = true;
            this.lbPosition.Location = new System.Drawing.Point(751, 242);
            this.lbPosition.Name = "lbPosition";
            this.lbPosition.Size = new System.Drawing.Size(50, 15);
            this.lbPosition.TabIndex = 18;
            this.lbPosition.Text = "Position";
            // 
            // lbPair
            // 
            this.lbPair.AutoSize = true;
            this.lbPair.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lbPair.Location = new System.Drawing.Point(6, 246);
            this.lbPair.Name = "lbPair";
            this.lbPair.Size = new System.Drawing.Size(0, 21);
            this.lbPair.TabIndex = 17;
            // 
            // pbRiver
            // 
            this.pbRiver.Location = new System.Drawing.Point(875, 122);
            this.pbRiver.Name = "pbRiver";
            this.pbRiver.Size = new System.Drawing.Size(25, 55);
            this.pbRiver.TabIndex = 16;
            this.pbRiver.TabStop = false;
            // 
            // pbTurn
            // 
            this.pbTurn.Location = new System.Drawing.Point(844, 122);
            this.pbTurn.Name = "pbTurn";
            this.pbTurn.Size = new System.Drawing.Size(25, 55);
            this.pbTurn.TabIndex = 15;
            this.pbTurn.TabStop = false;
            // 
            // pbFlop3
            // 
            this.pbFlop3.Location = new System.Drawing.Point(813, 122);
            this.pbFlop3.Name = "pbFlop3";
            this.pbFlop3.Size = new System.Drawing.Size(25, 55);
            this.pbFlop3.TabIndex = 14;
            this.pbFlop3.TabStop = false;
            // 
            // pbFlop2
            // 
            this.pbFlop2.Location = new System.Drawing.Point(782, 122);
            this.pbFlop2.Name = "pbFlop2";
            this.pbFlop2.Size = new System.Drawing.Size(25, 55);
            this.pbFlop2.TabIndex = 13;
            this.pbFlop2.TabStop = false;
            // 
            // pbFlop1
            // 
            this.pbFlop1.Location = new System.Drawing.Point(751, 122);
            this.pbFlop1.Name = "pbFlop1";
            this.pbFlop1.Size = new System.Drawing.Size(25, 55);
            this.pbFlop1.TabIndex = 12;
            this.pbFlop1.TabStop = false;
            // 
            // lbAction
            // 
            this.lbAction.AutoSize = true;
            this.lbAction.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lbAction.Location = new System.Drawing.Point(129, 15);
            this.lbAction.Name = "lbAction";
            this.lbAction.Size = new System.Drawing.Size(131, 30);
            this.lbAction.TabIndex = 11;
            this.lbAction.Text = "3BB / 4B / C";
            // 
            // lbP1Bet
            // 
            this.lbP1Bet.AutoSize = true;
            this.lbP1Bet.Location = new System.Drawing.Point(11, 114);
            this.lbP1Bet.Name = "lbP1Bet";
            this.lbP1Bet.Size = new System.Drawing.Size(0, 15);
            this.lbP1Bet.TabIndex = 10;
            // 
            // lbP2Bet
            // 
            this.lbP2Bet.AutoSize = true;
            this.lbP2Bet.Location = new System.Drawing.Point(277, 114);
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
            this.groupBox7.Location = new System.Drawing.Point(123, 48);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(129, 113);
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
            this.lbDealer0.Location = new System.Drawing.Point(83, 19);
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
            this.groupBox6.Location = new System.Drawing.Point(272, 48);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(99, 63);
            this.groupBox6.TabIndex = 7;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Player 2";
            // 
            // lbDealer2
            // 
            this.lbDealer2.AutoSize = true;
            this.lbDealer2.Location = new System.Drawing.Point(9, 20);
            this.lbDealer2.Name = "lbDealer2";
            this.lbDealer2.Size = new System.Drawing.Size(40, 15);
            this.lbDealer2.TabIndex = 7;
            this.lbDealer2.Text = "Dealer";
            // 
            // lbP2Chips
            // 
            this.lbP2Chips.AutoSize = true;
            this.lbP2Chips.Location = new System.Drawing.Point(6, 44);
            this.lbP2Chips.Name = "lbP2Chips";
            this.lbP2Chips.Size = new System.Drawing.Size(43, 15);
            this.lbP2Chips.TabIndex = 2;
            this.lbP2Chips.Text = "Chips: ";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.lbDealer1);
            this.groupBox5.Controls.Add(this.lbP1Chips);
            this.groupBox5.Location = new System.Drawing.Point(2, 48);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(101, 63);
            this.groupBox5.TabIndex = 6;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Player 1";
            // 
            // lbDealer1
            // 
            this.lbDealer1.AutoSize = true;
            this.lbDealer1.Location = new System.Drawing.Point(9, 19);
            this.lbDealer1.Name = "lbDealer1";
            this.lbDealer1.Size = new System.Drawing.Size(40, 15);
            this.lbDealer1.TabIndex = 2;
            this.lbDealer1.Text = "Dealer";
            // 
            // lbP1Chips
            // 
            this.lbP1Chips.AutoSize = true;
            this.lbP1Chips.Location = new System.Drawing.Point(6, 43);
            this.lbP1Chips.Name = "lbP1Chips";
            this.lbP1Chips.Size = new System.Drawing.Size(43, 15);
            this.lbP1Chips.TabIndex = 1;
            this.lbP1Chips.Text = "Chips: ";
            // 
            // lbEfective
            // 
            this.lbEfective.AutoSize = true;
            this.lbEfective.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lbEfective.Location = new System.Drawing.Point(0, 147);
            this.lbEfective.Name = "lbEfective";
            this.lbEfective.Size = new System.Drawing.Size(48, 17);
            this.lbEfective.TabIndex = 3;
            this.lbEfective.Text = "Ef BB: ";
            // 
            // btnCapture
            // 
            this.btnCapture.Location = new System.Drawing.Point(19, 187);
            this.btnCapture.Name = "btnCapture";
            this.btnCapture.Size = new System.Drawing.Size(81, 46);
            this.btnCapture.TabIndex = 53;
            this.btnCapture.Text = "Capture";
            this.btnCapture.UseVisualStyleBackColor = true;
            this.btnCapture.Click += new System.EventHandler(this.btnCapture_Click);
            // 
            // btnWindow
            // 
            this.btnWindow.Location = new System.Drawing.Point(296, 187);
            this.btnWindow.Name = "btnWindow";
            this.btnWindow.Size = new System.Drawing.Size(85, 46);
            this.btnWindow.TabIndex = 54;
            this.btnWindow.Text = "Window";
            this.btnWindow.UseVisualStyleBackColor = true;
            this.btnWindow.Click += new System.EventHandler(this.btnWindow_Click);
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            // 
            // btnNew
            // 
            this.btnNew.Location = new System.Drawing.Point(242, 209);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(49, 24);
            this.btnNew.TabIndex = 59;
            this.btnNew.Text = "New";
            this.btnNew.UseVisualStyleBackColor = true;
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            // 
            // FormPlaying
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(404, 242);
            this.Controls.Add(this.lbBoard);
            this.Controls.Add(this.lbPosition);
            this.Controls.Add(this.btnNew);
            this.Controls.Add(this.btnRiver);
            this.Controls.Add(this.btnTurn);
            this.Controls.Add(this.pbRiver);
            this.Controls.Add(this.btnFlop);
            this.Controls.Add(this.pbTurn);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.pbFlop3);
            this.Controls.Add(this.pbFlop2);
            this.Controls.Add(this.btnCapture);
            this.Controls.Add(this.pbFlop1);
            this.Controls.Add(this.btnWindow);
            this.Name = "FormPlaying";
            this.Text = "FormPlaying";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormPlaying_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormPlaying_FormClosed);
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbRiver)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbTurn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbFlop3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbFlop2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbFlop1)).EndInit();
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

        private Button btnRiver;
        private Button btnTurn;
        private Button btnFlop;
        private GroupBox groupBox4;
        private Label lbBoard;
        private Label lbPosition;
        private Label lbPair;
        private PictureBox pbRiver;
        private PictureBox pbTurn;
        private PictureBox pbFlop3;
        private PictureBox pbFlop2;
        private PictureBox pbFlop1;
        private Label lbAction;
        private Label lbP1Bet;
        private Label lbP2Bet;
        private GroupBox groupBox7;
        private PictureBox pbCard1;
        private PictureBox pbCard0;
        private Label lbDealer0;
        private Label lbP0Chips;
        private GroupBox groupBox6;
        private Label lbDealer2;
        private Label lbP2Chips;
        private GroupBox groupBox5;
        private Label lbDealer1;
        private Label lbP1Chips;
        private Label lbEfective;
        private Button btnCapture;
        private Button btnWindow;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private Button btnNew;
    }
}