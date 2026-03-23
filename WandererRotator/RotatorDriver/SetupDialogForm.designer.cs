namespace ASCOM.photonWanderer.Rotator
{
    partial class SetupDialogForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupDialogForm));
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.picASCOM = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chkTrace = new System.Windows.Forms.CheckBox();
            this.comboBoxComPort = new System.Windows.Forms.ComboBox();
            this.chkReverse = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.numericBacklash = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.numericCompletionCorrectionThreshold = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.numericDefaultMotionRate = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.numericVirtualMechanicalPosition = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.textMeasuredDegreesPerSecond = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericBacklash)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericCompletionCorrectionThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericDefaultMotionRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericVirtualMechanicalPosition)).BeginInit();
            this.SuspendLayout();
            // 
            // cmdOK
            // 
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.Location = new System.Drawing.Point(281, 279);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(59, 24);
            this.cmdOK.TabIndex = 0;
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            this.cmdOK.Click += new System.EventHandler(this.CmdOK_Click);
            // 
            // cmdCancel
            // 
            this.cmdCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(281, 309);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(59, 25);
            this.cmdCancel.TabIndex = 1;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.CmdCancel_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(245, 31);
            this.label1.TabIndex = 2;
            this.label1.Text = "Rotator setup";
            // 
            // picASCOM
            // 
            this.picASCOM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.picASCOM.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picASCOM.Image = ((System.Drawing.Image)(resources.GetObject("picASCOM.Image")));
            this.picASCOM.Location = new System.Drawing.Point(292, 9);
            this.picASCOM.Name = "picASCOM";
            this.picASCOM.Size = new System.Drawing.Size(48, 56);
            this.picASCOM.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picASCOM.TabIndex = 3;
            this.picASCOM.TabStop = false;
            this.picASCOM.Click += new System.EventHandler(this.BrowseToAscom);
            this.picASCOM.DoubleClick += new System.EventHandler(this.BrowseToAscom);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Comm Port";
            // 
            // chkTrace
            // 
            this.chkTrace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkTrace.AutoSize = true;
            this.chkTrace.Location = new System.Drawing.Point(16, 328);
            this.chkTrace.Name = "chkTrace";
            this.chkTrace.Size = new System.Drawing.Size(69, 17);
            this.chkTrace.TabIndex = 6;
            this.chkTrace.Text = "Trace on";
            this.chkTrace.UseVisualStyleBackColor = true;
            // 
            // comboBoxComPort
            // 
            this.comboBoxComPort.FormattingEnabled = true;
            this.comboBoxComPort.Location = new System.Drawing.Point(77, 71);
            this.comboBoxComPort.Name = "comboBoxComPort";
            this.comboBoxComPort.Size = new System.Drawing.Size(133, 21);
            this.comboBoxComPort.TabIndex = 7;
            // 
            // chkReverse
            // 
            this.chkReverse.AutoSize = true;
            this.chkReverse.Location = new System.Drawing.Point(16, 107);
            this.chkReverse.Name = "chkReverse";
            this.chkReverse.Size = new System.Drawing.Size(125, 17);
            this.chkReverse.TabIndex = 8;
            this.chkReverse.Text = "Reverse rotation axis";
            this.chkReverse.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 132);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(51, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Backlash";
            // 
            // numericBacklash
            // 
            this.numericBacklash.DecimalPlaces = 1;
            this.numericBacklash.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericBacklash.Location = new System.Drawing.Point(136, 130);
            this.numericBacklash.Maximum = new decimal(new int[] {
            36,
            0,
            0,
            0});
            this.numericBacklash.Name = "numericBacklash";
            this.numericBacklash.Size = new System.Drawing.Size(71, 20);
            this.numericBacklash.TabIndex = 10;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 158);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(101, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Correction threshold";
            // 
            // numericCompletionCorrectionThreshold
            // 
            this.numericCompletionCorrectionThreshold.DecimalPlaces = 3;
            this.numericCompletionCorrectionThreshold.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numericCompletionCorrectionThreshold.Location = new System.Drawing.Point(136, 156);
            this.numericCompletionCorrectionThreshold.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericCompletionCorrectionThreshold.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numericCompletionCorrectionThreshold.Name = "numericCompletionCorrectionThreshold";
            this.numericCompletionCorrectionThreshold.Size = new System.Drawing.Size(71, 20);
            this.numericCompletionCorrectionThreshold.TabIndex = 12;
            this.numericCompletionCorrectionThreshold.Value = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 184);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(90, 13);
            this.label5.TabIndex = 13;
            this.label5.Text = "Default deg / sec";
            // 
            // numericDefaultMotionRate
            // 
            this.numericDefaultMotionRate.DecimalPlaces = 2;
            this.numericDefaultMotionRate.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericDefaultMotionRate.Location = new System.Drawing.Point(136, 182);
            this.numericDefaultMotionRate.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numericDefaultMotionRate.Name = "numericDefaultMotionRate";
            this.numericDefaultMotionRate.Size = new System.Drawing.Size(71, 20);
            this.numericDefaultMotionRate.TabIndex = 14;
            this.numericDefaultMotionRate.Value = new decimal(new int[] {
            35,
            0,
            0,
            65536});
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 210);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(117, 13);
            this.label7.TabIndex = 15;
            this.label7.Text = "Virtual mechanical pos";
            // 
            // numericVirtualMechanicalPosition
            // 
            this.numericVirtualMechanicalPosition.DecimalPlaces = 3;
            this.numericVirtualMechanicalPosition.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericVirtualMechanicalPosition.Location = new System.Drawing.Point(136, 208);
            this.numericVirtualMechanicalPosition.Maximum = new decimal(new int[] {
            359999,
            0,
            0,
            196608});
            this.numericVirtualMechanicalPosition.Name = "numericVirtualMechanicalPosition";
            this.numericVirtualMechanicalPosition.Size = new System.Drawing.Size(71, 20);
            this.numericVirtualMechanicalPosition.TabIndex = 16;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 237);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(103, 13);
            this.label6.TabIndex = 17;
            this.label6.Text = "Measured deg / sec";
            // 
            // textMeasuredDegreesPerSecond
            // 
            this.textMeasuredDegreesPerSecond.Location = new System.Drawing.Point(118, 234);
            this.textMeasuredDegreesPerSecond.Name = "textMeasuredDegreesPerSecond";
            this.textMeasuredDegreesPerSecond.ReadOnly = true;
            this.textMeasuredDegreesPerSecond.Size = new System.Drawing.Size(89, 20);
            this.textMeasuredDegreesPerSecond.TabIndex = 18;
            this.textMeasuredDegreesPerSecond.TabStop = false;
            // 
            // SetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(350, 352);
            this.Controls.Add(this.textMeasuredDegreesPerSecond);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.numericVirtualMechanicalPosition);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.numericDefaultMotionRate);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.numericCompletionCorrectionThreshold);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.numericBacklash);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.chkReverse);
            this.Controls.Add(this.comboBoxComPort);
            this.Controls.Add(this.chkTrace);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.picASCOM);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupDialogForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wanderer Rotator (by photon) Setup";
            this.Load += new System.EventHandler(this.SetupDialogForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericBacklash)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericCompletionCorrectionThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericDefaultMotionRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericVirtualMechanicalPosition)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox picASCOM;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkTrace;
        private System.Windows.Forms.ComboBox comboBoxComPort;
        private System.Windows.Forms.CheckBox chkReverse;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numericBacklash;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numericCompletionCorrectionThreshold;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown numericDefaultMotionRate;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numericVirtualMechanicalPosition;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textMeasuredDegreesPerSecond;
    }
}