namespace URWPGSim2D.Gadget
{
    partial class DlgTrajectory
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
            this.picTrajectory = new System.Windows.Forms.PictureBox();
            this.pnl = new System.Windows.Forms.Panel();
            this.grp = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.picTrajectory)).BeginInit();
            this.grp.SuspendLayout();
            this.SuspendLayout();
            // 
            // picTrajectory
            // 
            this.picTrajectory.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.picTrajectory.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picTrajectory.Location = new System.Drawing.Point(10, 10);
            this.picTrajectory.Name = "picTrajectory";
            this.picTrajectory.Size = new System.Drawing.Size(750, 500);
            this.picTrajectory.TabIndex = 9;
            this.picTrajectory.TabStop = false;
            // 
            // pnl
            // 
            this.pnl.AutoScroll = true;
            this.pnl.Location = new System.Drawing.Point(5, 15);
            this.pnl.Name = "pnl";
            this.pnl.Size = new System.Drawing.Size(130, 480);
            this.pnl.TabIndex = 10;
            // 
            // grp
            // 
            this.grp.Controls.Add(this.pnl);
            this.grp.Location = new System.Drawing.Point(769, 10);
            this.grp.Name = "grp";
            this.grp.Size = new System.Drawing.Size(140, 500);
            this.grp.TabIndex = 0;
            this.grp.TabStop = false;
            this.grp.Text = "Please Select";
            // 
            // DlgTrajectory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(921, 521);
            this.Controls.Add(this.grp);
            this.Controls.Add(this.picTrajectory);
            this.Name = "DlgTrajectory";
            this.Text = "DlgTrajectory";
            this.Load += new System.EventHandler(this.DlgTrajectory_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.DlgTrajectory_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.picTrajectory)).EndInit();
            this.grp.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox picTrajectory;
        private System.Windows.Forms.Panel pnl;
        private System.Windows.Forms.GroupBox grp;
    }
}