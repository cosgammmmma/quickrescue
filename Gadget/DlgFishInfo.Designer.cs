namespace URWPGSim2D.Gadget
{
    partial class DlgFishInfo
    { /// <summary>
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.LB_Step = new System.Windows.Forms.Label();
            this.numericUpDownStep = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStep)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Location = new System.Drawing.Point(10, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(730, 260);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Real-time Value of Locomotion and Tatatic Parameters";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(634, 281);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(81, 23);
            this.btnSave.TabIndex = 10;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // LB_Step
            // 
            this.LB_Step.AutoSize = true;
            this.LB_Step.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.LB_Step.Location = new System.Drawing.Point(35, 286);
            this.LB_Step.Name = "LB_Step";
            this.LB_Step.Size = new System.Drawing.Size(35, 12);
            this.LB_Step.TabIndex = 9;
            this.LB_Step.Text = "Step:";
            // 
            // numericUpDownStep
            // 
            this.numericUpDownStep.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.numericUpDownStep.Location = new System.Drawing.Point(76, 284);
            this.numericUpDownStep.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownStep.Name = "numericUpDownStep";
            this.numericUpDownStep.Size = new System.Drawing.Size(59, 21);
            this.numericUpDownStep.TabIndex = 8;
            this.numericUpDownStep.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDownStep.ValueChanged += new System.EventHandler(this.numericUpDownStep_ValueChanged);
            // 
            // DlgFishInfo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(750, 317);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.LB_Step);
            this.Controls.Add(this.numericUpDownStep);
            this.Controls.Add(this.groupBox1);
            this.MaximizeBox = false;
            this.Name = "DlgFishInfo";
            this.Text = "Real-time Value of Locomotion and Tatatic Parameters";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.DlgFishInfo_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStep)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label LB_Step;
        private System.Windows.Forms.NumericUpDown numericUpDownStep;
    }
}