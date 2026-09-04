namespace URWPGSim2D.Screencast
{
    partial class Screencast
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label label3;
            System.Windows.Forms.Label label4;
            System.Windows.Forms.Label label5;
            System.Windows.Forms.Label label6;
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.txtFileName = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.cmbEncoder = new System.Windows.Forms.ComboBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.txtX = new URWPGSim2D.Screencast.NumberTextBox();
            this.txtY = new URWPGSim2D.Screencast.NumberTextBox();
            this.txtWidth = new URWPGSim2D.Screencast.NumberTextBox();
            this.txtHeight = new URWPGSim2D.Screencast.NumberTextBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(9, 13);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(47, 12);
            label1.TabIndex = 100;
            label1.Text = "StartX:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(9, 43);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(47, 12);
            label2.TabIndex = 101;
            label2.Text = "StartY:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(172, 13);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(41, 12);
            label3.TabIndex = 102;
            label3.Text = "Width:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(174, 43);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(47, 12);
            label4.TabIndex = 103;
            label4.Text = "Height:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(9, 77);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(35, 12);
            label5.TabIndex = 104;
            label5.Text = "File:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(9, 111);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(53, 12);
            label6.TabIndex = 105;
            label6.Text = "Encoder:";
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(47, 145);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(209, 145);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 2;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // txtFileName
            // 
            this.txtFileName.Location = new System.Drawing.Point(62, 74);
            this.txtFileName.Name = "txtFileName";
            this.txtFileName.Size = new System.Drawing.Size(210, 21);
            this.txtFileName.TabIndex = 4;
            this.txtFileName.MouseHover += new System.EventHandler(this.txtFileName_MouseHover);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(284, 72);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(36, 23);
            this.btnBrowse.TabIndex = 3;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // cmbEncoder
            // 
            this.cmbEncoder.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEncoder.FormattingEnabled = true;
            this.cmbEncoder.Location = new System.Drawing.Point(62, 108);
            this.cmbEncoder.Name = "cmbEncoder";
            this.cmbEncoder.Size = new System.Drawing.Size(258, 20);
            this.cmbEncoder.TabIndex = 15;
            this.cmbEncoder.SelectedIndexChanged += new System.EventHandler(this.cmbEncoder_SelectedIndexChanged);
            // 
            // txtX
            // 
            this.txtX.Location = new System.Drawing.Point(62, 9);
            this.txtX.Name = "txtX";
            this.txtX.Size = new System.Drawing.Size(100, 21);
            this.txtX.TabIndex = 11;
            this.txtX.Text = "0";
            // 
            // txtY
            // 
            this.txtY.Location = new System.Drawing.Point(62, 40);
            this.txtY.Name = "txtY";
            this.txtY.Size = new System.Drawing.Size(100, 21);
            this.txtY.TabIndex = 12;
            this.txtY.Text = "0";
            // 
            // txtWidth
            // 
            this.txtWidth.Location = new System.Drawing.Point(220, 9);
            this.txtWidth.Name = "txtWidth";
            this.txtWidth.Size = new System.Drawing.Size(100, 21);
            this.txtWidth.TabIndex = 13;
            this.txtWidth.Text = "800";
            // 
            // txtHeight
            // 
            this.txtHeight.Location = new System.Drawing.Point(220, 40);
            this.txtHeight.Name = "txtHeight";
            this.txtHeight.Size = new System.Drawing.Size(100, 21);
            this.txtHeight.TabIndex = 14;
            this.txtHeight.Text = "550";
            // 
            // Screencast
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(331, 177);
            this.Controls.Add(this.txtHeight);
            this.Controls.Add(this.txtWidth);
            this.Controls.Add(this.txtY);
            this.Controls.Add(this.txtX);
            this.Controls.Add(label6);
            this.Controls.Add(this.cmbEncoder);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtFileName);
            this.Controls.Add(label5);
            this.Controls.Add(label4);
            this.Controls.Add(label3);
            this.Controls.Add(label2);
            this.Controls.Add(label1);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "Screencast";
            this.Text = "URWPGSim2D.Screencast";
            this.Load += new System.EventHandler(this.Screencast_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.TextBox txtFileName;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.ComboBox cmbEncoder;
        private System.Windows.Forms.ToolTip toolTip;
        private NumberTextBox txtX;
        private NumberTextBox txtY;
        private NumberTextBox txtWidth;
        private NumberTextBox txtHeight;
    }
}

