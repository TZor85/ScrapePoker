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
            groupBox4 = new GroupBox();
            lbAction = new Label();
            lbP1Bet = new Label();
            lbP2Bet = new Label();
            groupBox7 = new GroupBox();
            pbCard1 = new PictureBox();
            pbCard0 = new PictureBox();
            lbDealer0 = new Label();
            lbP0Chips = new Label();
            btnCapture = new Button();
            btnWindow = new Button();
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            groupBox4.SuspendLayout();
            groupBox7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbCard1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbCard0).BeginInit();
            SuspendLayout();
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(lbAction);
            groupBox4.Controls.Add(lbP1Bet);
            groupBox4.Controls.Add(lbP2Bet);
            groupBox4.Controls.Add(groupBox7);
            groupBox4.Location = new Point(10, 7);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(285, 131);
            groupBox4.TabIndex = 55;
            groupBox4.TabStop = false;
            // 
            // lbAction
            // 
            lbAction.AutoSize = true;
            lbAction.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point);
            lbAction.Location = new Point(141, 63);
            lbAction.Name = "lbAction";
            lbAction.Size = new Size(131, 30);
            lbAction.TabIndex = 11;
            lbAction.Text = "3BB / 4B / C";
            // 
            // lbP1Bet
            // 
            lbP1Bet.AutoSize = true;
            lbP1Bet.Location = new Point(11, 110);
            lbP1Bet.Name = "lbP1Bet";
            lbP1Bet.Size = new Size(0, 15);
            lbP1Bet.TabIndex = 10;
            // 
            // lbP2Bet
            // 
            lbP2Bet.AutoSize = true;
            lbP2Bet.Location = new Point(277, 110);
            lbP2Bet.Name = "lbP2Bet";
            lbP2Bet.Size = new Size(0, 15);
            lbP2Bet.TabIndex = 9;
            // 
            // groupBox7
            // 
            groupBox7.Controls.Add(pbCard1);
            groupBox7.Controls.Add(pbCard0);
            groupBox7.Controls.Add(lbDealer0);
            groupBox7.Controls.Add(lbP0Chips);
            groupBox7.Location = new Point(6, 12);
            groupBox7.Name = "groupBox7";
            groupBox7.Size = new Size(129, 113);
            groupBox7.TabIndex = 8;
            groupBox7.TabStop = false;
            groupBox7.Text = "Hero";
            // 
            // pbCard1
            // 
            pbCard1.Location = new Point(41, 26);
            pbCard1.Name = "pbCard1";
            pbCard1.Size = new Size(25, 55);
            pbCard1.TabIndex = 8;
            pbCard1.TabStop = false;
            // 
            // pbCard0
            // 
            pbCard0.Location = new Point(10, 26);
            pbCard0.Name = "pbCard0";
            pbCard0.Size = new Size(25, 55);
            pbCard0.TabIndex = 7;
            pbCard0.TabStop = false;
            // 
            // lbDealer0
            // 
            lbDealer0.AutoSize = true;
            lbDealer0.Location = new Point(83, 19);
            lbDealer0.Name = "lbDealer0";
            lbDealer0.Size = new Size(40, 15);
            lbDealer0.TabIndex = 6;
            lbDealer0.Text = "Dealer";
            // 
            // lbP0Chips
            // 
            lbP0Chips.AutoSize = true;
            lbP0Chips.Location = new Point(6, 86);
            lbP0Chips.Name = "lbP0Chips";
            lbP0Chips.Size = new Size(43, 15);
            lbP0Chips.TabIndex = 3;
            lbP0Chips.Text = "Chips: ";
            // 
            // btnCapture
            // 
            btnCapture.Location = new Point(11, 142);
            btnCapture.Name = "btnCapture";
            btnCapture.Size = new Size(81, 33);
            btnCapture.TabIndex = 53;
            btnCapture.Text = "Capture";
            btnCapture.UseVisualStyleBackColor = true;
            btnCapture.Click += btnCapture_Click;
            // 
            // btnWindow
            // 
            btnWindow.Location = new Point(202, 144);
            btnWindow.Name = "btnWindow";
            btnWindow.Size = new Size(85, 32);
            btnWindow.TabIndex = 54;
            btnWindow.Text = "Window";
            btnWindow.UseVisualStyleBackColor = true;
            btnWindow.Click += btnWindow_Click;
            // 
            // backgroundWorker1
            // 
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            // 
            // FormPlaying
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(304, 181);
            Controls.Add(groupBox4);
            Controls.Add(btnCapture);
            Controls.Add(btnWindow);
            Name = "FormPlaying";
            Text = "FormPlaying";
            FormClosing += FormPlaying_FormClosing;
            FormClosed += FormPlaying_FormClosed;
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            groupBox7.ResumeLayout(false);
            groupBox7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pbCard1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbCard0).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private GroupBox groupBox4;
        private Label lbAction;
        private Label lbP1Bet;
        private Label lbP2Bet;
        private GroupBox groupBox7;
        private PictureBox pbCard1;
        private PictureBox pbCard0;
        private Label lbDealer0;
        private Label lbP0Chips;
        private Button btnCapture;
        private Button btnWindow;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
    }
}