namespace OpenScrape.App.Forms
{
    partial class FrmDetectionDebug
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
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                // Cleanup custom resources
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                pbZoomedArea.Image?.Dispose();
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
            this.lblRegionName = new System.Windows.Forms.Label();
            this.lblCoordinates = new System.Windows.Forms.Label();
            this.lblCurrentColor = new System.Windows.Forms.Label();
            this.lblCurrentColorHex = new System.Windows.Forms.Label();
            this.lblDetectionStatus = new System.Windows.Forms.Label();
            this.lblColorStats = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnlCurrentColor = new System.Windows.Forms.Panel();
            this.pbZoomedArea = new System.Windows.Forms.PictureBox();
            this.btnStartCapture = new System.Windows.Forms.Button();
            this.btnStopCapture = new System.Windows.Forms.Button();
            this.btnSaveColor = new System.Windows.Forms.Button();
            this.btnClearColors = new System.Windows.Forms.Button();
            this.btnSaveCoordinates = new System.Windows.Forms.Button();
            this.numX = new System.Windows.Forms.NumericUpDown();
            this.numY = new System.Windows.Forms.NumericUpDown();
            this.lblX = new System.Windows.Forms.Label();
            this.lblY = new System.Windows.Forms.Label();
            this.grpCoordinates = new System.Windows.Forms.GroupBox();
            this.grpColorInfo = new System.Windows.Forms.GroupBox();
            this.grpZoomedView = new System.Windows.Forms.GroupBox();
            this.grpControls = new System.Windows.Forms.GroupBox();
            this.lblInstructions = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbZoomedArea)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numY)).BeginInit();
            this.grpCoordinates.SuspendLayout();
            this.grpColorInfo.SuspendLayout();
            this.grpZoomedView.SuspendLayout();
            this.grpControls.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblRegionName
            // 
            this.lblRegionName.AutoSize = true;
            this.lblRegionName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRegionName.Location = new System.Drawing.Point(12, 9);
            this.lblRegionName.Name = "lblRegionName";
            this.lblRegionName.Size = new System.Drawing.Size(54, 15);
            this.lblRegionName.TabIndex = 0;
            this.lblRegionName.Text = "Región:";
            // 
            // lblCoordinates
            // 
            this.lblCoordinates.AutoSize = true;
            this.lblCoordinates.Location = new System.Drawing.Point(15, 25);
            this.lblCoordinates.Name = "lblCoordinates";
            this.lblCoordinates.Size = new System.Drawing.Size(82, 13);
            this.lblCoordinates.TabIndex = 1;
            this.lblCoordinates.Text = "Coordenadas: ()";
            // 
            // lblCurrentColor
            // 
            this.lblCurrentColor.AutoSize = true;
            this.lblCurrentColor.Location = new System.Drawing.Point(15, 25);
            this.lblCurrentColor.Name = "lblCurrentColor";
            this.lblCurrentColor.Size = new System.Drawing.Size(67, 13);
            this.lblCurrentColor.TabIndex = 2;
            this.lblCurrentColor.Text = "Color Actual:";
            // 
            // lblCurrentColorHex
            // 
            this.lblCurrentColorHex.AutoSize = true;
            this.lblCurrentColorHex.Location = new System.Drawing.Point(15, 45);
            this.lblCurrentColorHex.Name = "lblCurrentColorHex";
            this.lblCurrentColorHex.Size = new System.Drawing.Size(29, 13);
            this.lblCurrentColorHex.TabIndex = 3;
            this.lblCurrentColorHex.Text = "Hex:";
            // 
            // lblDetectionStatus
            // 
            this.lblDetectionStatus.AutoSize = true;
            this.lblDetectionStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDetectionStatus.Location = new System.Drawing.Point(15, 65);
            this.lblDetectionStatus.Name = "lblDetectionStatus";
            this.lblDetectionStatus.Size = new System.Drawing.Size(108, 13);
            this.lblDetectionStatus.TabIndex = 4;
            this.lblDetectionStatus.Text = "Estado Detección:";
            // 
            // lblColorStats
            // 
            this.lblColorStats.AutoSize = true;
            this.lblColorStats.Location = new System.Drawing.Point(15, 115);
            this.lblColorStats.Name = "lblColorStats";
            this.lblColorStats.Size = new System.Drawing.Size(70, 13);
            this.lblColorStats.TabIndex = 5;
            this.lblColorStats.Text = "Estadísticas:";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 550);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(43, 13);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "Estado:";
            // 
            // pnlCurrentColor
            // 
            this.pnlCurrentColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlCurrentColor.Location = new System.Drawing.Point(150, 25);
            this.pnlCurrentColor.Name = "pnlCurrentColor";
            this.pnlCurrentColor.Size = new System.Drawing.Size(50, 50);
            this.pnlCurrentColor.TabIndex = 7;
            // 
            // pbZoomedArea
            // 
            this.pbZoomedArea.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbZoomedArea.Location = new System.Drawing.Point(15, 25);
            this.pbZoomedArea.Name = "pbZoomedArea";
            this.pbZoomedArea.Size = new System.Drawing.Size(200, 200);
            this.pbZoomedArea.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbZoomedArea.TabIndex = 8;
            this.pbZoomedArea.TabStop = false;
            // 
            // btnStartCapture
            // 
            this.btnStartCapture.Location = new System.Drawing.Point(15, 25);
            this.btnStartCapture.Name = "btnStartCapture";
            this.btnStartCapture.Size = new System.Drawing.Size(100, 30);
            this.btnStartCapture.TabIndex = 9;
            this.btnStartCapture.Text = "Iniciar Captura";
            this.btnStartCapture.UseVisualStyleBackColor = true;
            // 
            // btnStopCapture
            // 
            this.btnStopCapture.Enabled = false;
            this.btnStopCapture.Location = new System.Drawing.Point(125, 25);
            this.btnStopCapture.Name = "btnStopCapture";
            this.btnStopCapture.Size = new System.Drawing.Size(100, 30);
            this.btnStopCapture.TabIndex = 10;
            this.btnStopCapture.Text = "Detener Captura";
            this.btnStopCapture.UseVisualStyleBackColor = true;
            // 
            // btnSaveColor
            // 
            this.btnSaveColor.Location = new System.Drawing.Point(15, 65);
            this.btnSaveColor.Name = "btnSaveColor";
            this.btnSaveColor.Size = new System.Drawing.Size(100, 30);
            this.btnSaveColor.TabIndex = 11;
            this.btnSaveColor.Text = "Guardar Color";
            this.btnSaveColor.UseVisualStyleBackColor = true;
            // 
            // btnClearColors
            // 
            this.btnClearColors.Location = new System.Drawing.Point(125, 65);
            this.btnClearColors.Name = "btnClearColors";
            this.btnClearColors.Size = new System.Drawing.Size(100, 30);
            this.btnClearColors.TabIndex = 12;
            this.btnClearColors.Text = "Limpiar Colores";
            this.btnClearColors.UseVisualStyleBackColor = true;
            // 
            // btnSaveCoordinates
            // 
            this.btnSaveCoordinates.Location = new System.Drawing.Point(15, 105);
            this.btnSaveCoordinates.Name = "btnSaveCoordinates";
            this.btnSaveCoordinates.Size = new System.Drawing.Size(120, 30);
            this.btnSaveCoordinates.TabIndex = 13;
            this.btnSaveCoordinates.Text = "Guardar Coordenadas";
            this.btnSaveCoordinates.UseVisualStyleBackColor = true;
            // 
            // numX
            // 
            this.numX.Location = new System.Drawing.Point(40, 50);
            this.numX.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numX.Name = "numX";
            this.numX.Size = new System.Drawing.Size(60, 20);
            this.numX.TabIndex = 14;
            // 
            // numY
            // 
            this.numY.Location = new System.Drawing.Point(130, 50);
            this.numY.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numY.Name = "numY";
            this.numY.Size = new System.Drawing.Size(60, 20);
            this.numY.TabIndex = 15;
            // 
            // lblX
            // 
            this.lblX.AutoSize = true;
            this.lblX.Location = new System.Drawing.Point(15, 52);
            this.lblX.Name = "lblX";
            this.lblX.Size = new System.Drawing.Size(17, 13);
            this.lblX.TabIndex = 16;
            this.lblX.Text = "X:";
            // 
            // lblY
            // 
            this.lblY.AutoSize = true;
            this.lblY.Location = new System.Drawing.Point(110, 52);
            this.lblY.Name = "lblY";
            this.lblY.Size = new System.Drawing.Size(17, 13);
            this.lblY.TabIndex = 17;
            this.lblY.Text = "Y:";
            // 
            // grpCoordinates
            // 
            this.grpCoordinates.Controls.Add(this.lblCoordinates);
            this.grpCoordinates.Controls.Add(this.lblY);
            this.grpCoordinates.Controls.Add(this.lblX);
            this.grpCoordinates.Controls.Add(this.numY);
            this.grpCoordinates.Controls.Add(this.numX);
            this.grpCoordinates.Location = new System.Drawing.Point(12, 35);
            this.grpCoordinates.Name = "grpCoordinates";
            this.grpCoordinates.Size = new System.Drawing.Size(220, 85);
            this.grpCoordinates.TabIndex = 18;
            this.grpCoordinates.TabStop = false;
            this.grpCoordinates.Text = "Coordenadas";
            // 
            // grpColorInfo
            // 
            this.grpColorInfo.Controls.Add(this.lblCurrentColor);
            this.grpColorInfo.Controls.Add(this.lblCurrentColorHex);
            this.grpColorInfo.Controls.Add(this.lblDetectionStatus);
            this.grpColorInfo.Controls.Add(this.pnlCurrentColor);
            this.grpColorInfo.Controls.Add(this.lblColorStats);
            this.grpColorInfo.Location = new System.Drawing.Point(250, 35);
            this.grpColorInfo.Name = "grpColorInfo";
            this.grpColorInfo.Size = new System.Drawing.Size(220, 180);
            this.grpColorInfo.TabIndex = 19;
            this.grpColorInfo.TabStop = false;
            this.grpColorInfo.Text = "Información de Color";
            // 
            // grpZoomedView
            // 
            this.grpZoomedView.Controls.Add(this.pbZoomedArea);
            this.grpZoomedView.Location = new System.Drawing.Point(12, 130);
            this.grpZoomedView.Name = "grpZoomedView";
            this.grpZoomedView.Size = new System.Drawing.Size(230, 240);
            this.grpZoomedView.TabIndex = 20;
            this.grpZoomedView.TabStop = false;
            this.grpZoomedView.Text = "Vista Ampliada (10x)";
            // 
            // grpControls
            // 
            this.grpControls.Controls.Add(this.btnStartCapture);
            this.grpControls.Controls.Add(this.btnStopCapture);
            this.grpControls.Controls.Add(this.btnSaveColor);
            this.grpControls.Controls.Add(this.btnClearColors);
            this.grpControls.Controls.Add(this.btnSaveCoordinates);
            this.grpControls.Location = new System.Drawing.Point(250, 230);
            this.grpControls.Name = "grpControls";
            this.grpControls.Size = new System.Drawing.Size(240, 150);
            this.grpControls.TabIndex = 21;
            this.grpControls.TabStop = false;
            this.grpControls.Text = "Controles";
            // 
            // lblInstructions
            // 
            this.lblInstructions.AutoSize = true;
            this.lblInstructions.Location = new System.Drawing.Point(12, 385);
            this.lblInstructions.Name = "lblInstructions";
            this.lblInstructions.Size = new System.Drawing.Size(458, 156);
            this.lblInstructions.TabIndex = 22;
            this.lblInstructions.Text = "Instrucciones:\r\n\r\n1. Haz clic en \"Iniciar Captura\" para comenzar el monitoreo en " +
    "tiempo real\r\n2. Usa las flechas del teclado para mover las coordenadas (Ctrl + f" +
    "lecha = paso grande)\r\n3. Ajusta las coordenadas hasta que el píxel esté en el ár" +
    "ea correcta\r\n4. Cuando sea tu turno en el poker, presiona ESPACIO para guardar e" +
    "l color\r\n5. Presiona ENTER para guardar las coordenadas actuales\r\n6. La cruz roj" +
    "a en la vista ampliada marca el píxel exacto siendo monitoreado\r\n\r\nLa detección " +
    "actual busca un valor B=24 en el color del píxel.\r\nUsa esta herramienta para en" +
    "contrar el color y coordenadas correctas.";
            // 
            // FrmDetectionDebug
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 580);
            this.Controls.Add(this.lblInstructions);
            this.Controls.Add(this.grpControls);
            this.Controls.Add(this.grpZoomedView);
            this.Controls.Add(this.grpColorInfo);
            this.Controls.Add(this.grpCoordinates);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblRegionName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmDetectionDebug";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Debug de Detección de Turnos";
            ((System.ComponentModel.ISupportInitialize)(this.pbZoomedArea)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numY)).EndInit();
            this.grpCoordinates.ResumeLayout(false);
            this.grpCoordinates.PerformLayout();
            this.grpColorInfo.ResumeLayout(false);
            this.grpColorInfo.PerformLayout();
            this.grpZoomedView.ResumeLayout(false);
            this.grpControls.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblRegionName;
        private System.Windows.Forms.Label lblCoordinates;
        private System.Windows.Forms.Label lblCurrentColor;
        private System.Windows.Forms.Label lblCurrentColorHex;
        private System.Windows.Forms.Label lblDetectionStatus;
        private System.Windows.Forms.Label lblColorStats;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel pnlCurrentColor;
        private System.Windows.Forms.PictureBox pbZoomedArea;
        private System.Windows.Forms.Button btnStartCapture;
        private System.Windows.Forms.Button btnStopCapture;
        private System.Windows.Forms.Button btnSaveColor;
        private System.Windows.Forms.Button btnClearColors;
        private System.Windows.Forms.Button btnSaveCoordinates;
        private System.Windows.Forms.NumericUpDown numX;
        private System.Windows.Forms.NumericUpDown numY;
        private System.Windows.Forms.Label lblX;
        private System.Windows.Forms.Label lblY;
        private System.Windows.Forms.GroupBox grpCoordinates;
        private System.Windows.Forms.GroupBox grpColorInfo;
        private System.Windows.Forms.GroupBox grpZoomedView;
        private System.Windows.Forms.GroupBox grpControls;
        private System.Windows.Forms.Label lblInstructions;
    }
}