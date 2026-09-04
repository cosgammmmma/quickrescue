namespace URWPGSim2D.Sim2DClt
{
    partial class ClientControlBoard
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClientControlBoard));
            this.lblType = new System.Windows.Forms.Label();
            this.grpCompetition = new System.Windows.Forms.GroupBox();
            this.lblCompetitionType = new System.Windows.Forms.Label();
            this.grpTeamInfo = new System.Windows.Forms.GroupBox();
            this.lblTeamId = new System.Windows.Forms.Label();
            this.lblId = new System.Windows.Forms.Label();
            this.rdoRight = new System.Windows.Forms.RadioButton();
            this.rdoLeft = new System.Windows.Forms.RadioButton();
            this.lblHalfCourt = new System.Windows.Forms.Label();
            this.lblTeamName = new System.Windows.Forms.Label();
            this.lblName = new System.Windows.Forms.Label();
            this.grpStrategy = new System.Windows.Forms.GroupBox();
            this.txtStrategy = new System.Windows.Forms.TextBox();
            this.btnReady = new System.Windows.Forms.Button();
            this.lblLocation = new System.Windows.Forms.Label();
            this.btnStrategyBrowse = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtCompetitionState = new System.Windows.Forms.TextBox();
            this.grpCompetition.SuspendLayout();
            this.grpTeamInfo.SuspendLayout();
            this.grpStrategy.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblType
            // 
            this.lblType.Font = new System.Drawing.Font("SimSun", 9F);
            this.lblType.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblType.Location = new System.Drawing.Point(11, 26);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(76, 24);
            this.lblType.TabIndex = 15;
            this.lblType.Text = "Type:";
            this.lblType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // grpCompetition
            // 
            this.grpCompetition.Controls.Add(this.lblCompetitionType);
            this.grpCompetition.Controls.Add(this.lblType);
            this.grpCompetition.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold);
            this.grpCompetition.Location = new System.Drawing.Point(10, 121);
            this.grpCompetition.Name = "grpCompetition";
            this.grpCompetition.Size = new System.Drawing.Size(477, 72);
            this.grpCompetition.TabIndex = 17;
            this.grpCompetition.TabStop = false;
            this.grpCompetition.Text = "Competition";
            // 
            // lblCompetitionType
            // 
            this.lblCompetitionType.BackColor = System.Drawing.SystemColors.Window;
            this.lblCompetitionType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblCompetitionType.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Bold);
            this.lblCompetitionType.ForeColor = System.Drawing.Color.MediumSlateBlue;
            this.lblCompetitionType.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblCompetitionType.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblCompetitionType.Location = new System.Drawing.Point(90, 26);
            this.lblCompetitionType.Name = "lblCompetitionType";
            this.lblCompetitionType.Padding = new System.Windows.Forms.Padding(2, 5, 2, 2);
            this.lblCompetitionType.Size = new System.Drawing.Size(346, 24);
            this.lblCompetitionType.TabIndex = 2;
            this.lblCompetitionType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpTeamInfo
            // 
            this.grpTeamInfo.Controls.Add(this.lblTeamId);
            this.grpTeamInfo.Controls.Add(this.lblId);
            this.grpTeamInfo.Controls.Add(this.rdoRight);
            this.grpTeamInfo.Controls.Add(this.rdoLeft);
            this.grpTeamInfo.Controls.Add(this.lblHalfCourt);
            this.grpTeamInfo.Controls.Add(this.lblTeamName);
            this.grpTeamInfo.Controls.Add(this.lblName);
            this.grpTeamInfo.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold);
            this.grpTeamInfo.Location = new System.Drawing.Point(10, 12);
            this.grpTeamInfo.Name = "grpTeamInfo";
            this.grpTeamInfo.Size = new System.Drawing.Size(477, 94);
            this.grpTeamInfo.TabIndex = 18;
            this.grpTeamInfo.TabStop = false;
            this.grpTeamInfo.Text = "Team";
            // 
            // lblTeamId
            // 
            this.lblTeamId.BackColor = System.Drawing.SystemColors.Window;
            this.lblTeamId.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblTeamId.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Bold);
            this.lblTeamId.ForeColor = System.Drawing.Color.MediumSlateBlue;
            this.lblTeamId.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTeamId.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblTeamId.Location = new System.Drawing.Point(360, 25);
            this.lblTeamId.Name = "lblTeamId";
            this.lblTeamId.Padding = new System.Windows.Forms.Padding(2, 5, 2, 2);
            this.lblTeamId.Size = new System.Drawing.Size(76, 24);
            this.lblTeamId.TabIndex = 21;
            this.lblTeamId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblId
            // 
            this.lblId.Font = new System.Drawing.Font("SimSun", 9F);
            this.lblId.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblId.Location = new System.Drawing.Point(309, 25);
            this.lblId.Name = "lblId";
            this.lblId.Size = new System.Drawing.Size(48, 24);
            this.lblId.TabIndex = 22;
            this.lblId.Text = "ID:";
            this.lblId.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // rdoRight
            // 
            this.rdoRight.AutoCheck = false;
            this.rdoRight.Enabled = false;
            this.rdoRight.ForeColor = System.Drawing.Color.Yellow;
            this.rdoRight.Location = new System.Drawing.Point(221, 56);
            this.rdoRight.Name = "rdoRight";
            this.rdoRight.Size = new System.Drawing.Size(89, 24);
            this.rdoRight.TabIndex = 20;
            this.rdoRight.Text = "B (Right)";
            this.rdoRight.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.rdoRight.UseVisualStyleBackColor = true;
            // 
            // rdoLeft
            // 
            this.rdoLeft.AutoCheck = false;
            this.rdoLeft.Enabled = false;
            this.rdoLeft.ForeColor = System.Drawing.Color.Red;
            this.rdoLeft.Location = new System.Drawing.Point(93, 56);
            this.rdoLeft.Name = "rdoLeft";
            this.rdoLeft.Size = new System.Drawing.Size(80, 24);
            this.rdoLeft.TabIndex = 19;
            this.rdoLeft.Text = "A (Left)";
            this.rdoLeft.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.rdoLeft.UseVisualStyleBackColor = true;
            // 
            // lblHalfCourt
            // 
            this.lblHalfCourt.Font = new System.Drawing.Font("SimSun", 9F);
            this.lblHalfCourt.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblHalfCourt.Location = new System.Drawing.Point(11, 56);
            this.lblHalfCourt.Name = "lblHalfCourt";
            this.lblHalfCourt.Size = new System.Drawing.Size(76, 24);
            this.lblHalfCourt.TabIndex = 18;
            this.lblHalfCourt.Text = "Half Court:";
            this.lblHalfCourt.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTeamName
            // 
            this.lblTeamName.BackColor = System.Drawing.SystemColors.Window;
            this.lblTeamName.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblTeamName.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Bold);
            this.lblTeamName.ForeColor = System.Drawing.Color.MediumSlateBlue;
            this.lblTeamName.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTeamName.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblTeamName.Location = new System.Drawing.Point(90, 25);
            this.lblTeamName.Name = "lblTeamName";
            this.lblTeamName.Padding = new System.Windows.Forms.Padding(2, 5, 2, 2);
            this.lblTeamName.Size = new System.Drawing.Size(220, 24);
            this.lblTeamName.TabIndex = 1;
            this.lblTeamName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblName
            // 
            this.lblName.Font = new System.Drawing.Font("SimSun", 9F);
            this.lblName.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblName.Location = new System.Drawing.Point(11, 25);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(76, 24);
            this.lblName.TabIndex = 15;
            this.lblName.Text = "Name:";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // grpStrategy
            // 
            this.grpStrategy.Controls.Add(this.txtStrategy);
            this.grpStrategy.Controls.Add(this.btnReady);
            this.grpStrategy.Controls.Add(this.lblLocation);
            this.grpStrategy.Controls.Add(this.btnStrategyBrowse);
            this.grpStrategy.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold);
            this.grpStrategy.Location = new System.Drawing.Point(10, 208);
            this.grpStrategy.Name = "grpStrategy";
            this.grpStrategy.Size = new System.Drawing.Size(477, 100);
            this.grpStrategy.TabIndex = 19;
            this.grpStrategy.TabStop = false;
            this.grpStrategy.Text = "Strategy";
            // 
            // txtStrategy
            // 
            this.txtStrategy.AllowDrop = true;
            this.txtStrategy.Font = new System.Drawing.Font("SimSun", 10.5F);
            this.txtStrategy.Location = new System.Drawing.Point(90, 26);
            this.txtStrategy.Multiline = true;
            this.txtStrategy.Name = "txtStrategy";
            this.txtStrategy.Size = new System.Drawing.Size(280, 24);
            this.txtStrategy.TabIndex = 3;
            this.txtStrategy.MouseHover += new System.EventHandler(this.txtStrategy_MouseHover);
            // 
            // btnReady
            // 
            this.btnReady.Enabled = false;
            this.btnReady.Font = new System.Drawing.Font("SimSun", 9F);
            this.btnReady.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnReady.Location = new System.Drawing.Point(208, 63);
            this.btnReady.Name = "btnReady";
            this.btnReady.Size = new System.Drawing.Size(60, 24);
            this.btnReady.TabIndex = 2;
            this.btnReady.Text = "Ready";
            this.btnReady.UseVisualStyleBackColor = true;
            this.btnReady.Click += new System.EventHandler(this.btnReady_Click);
            // 
            // lblLocation
            // 
            this.lblLocation.Font = new System.Drawing.Font("SimSun", 9F);
            this.lblLocation.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblLocation.Location = new System.Drawing.Point(11, 26);
            this.lblLocation.Name = "lblLocation";
            this.lblLocation.Size = new System.Drawing.Size(76, 24);
            this.lblLocation.TabIndex = 15;
            this.lblLocation.Text = "Location:";
            this.lblLocation.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnStrategyBrowse
            // 
            this.btnStrategyBrowse.Enabled = false;
            this.btnStrategyBrowse.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnStrategyBrowse.Location = new System.Drawing.Point(376, 26);
            this.btnStrategyBrowse.Name = "btnStrategyBrowse";
            this.btnStrategyBrowse.Size = new System.Drawing.Size(60, 24);
            this.btnStrategyBrowse.TabIndex = 1;
            this.btnStrategyBrowse.Text = "...";
            this.btnStrategyBrowse.UseVisualStyleBackColor = true;
            this.btnStrategyBrowse.Click += new System.EventHandler(this.btnStrategyBrowse_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtCompetitionState);
            this.groupBox1.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold);
            this.groupBox1.Location = new System.Drawing.Point(10, 324);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(477, 65);
            this.groupBox1.TabIndex = 20;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "State";
            // 
            // txtCompetitionState
            // 
            this.txtCompetitionState.AcceptsReturn = true;
            this.txtCompetitionState.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.txtCompetitionState.Font = new System.Drawing.Font("SimSun", 10.5F);
            this.txtCompetitionState.Location = new System.Drawing.Point(90, 20);
            this.txtCompetitionState.Multiline = true;
            this.txtCompetitionState.Name = "txtCompetitionState";
            this.txtCompetitionState.ReadOnly = true;
            this.txtCompetitionState.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtCompetitionState.Size = new System.Drawing.Size(346, 24);
            this.txtCompetitionState.TabIndex = 0;
            this.txtCompetitionState.MouseHover += new System.EventHandler(this.txtCompetitionState_MouseHover);
            // 
            // ClientControlBoard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(496, 401);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.grpStrategy);
            this.Controls.Add(this.grpTeamInfo);
            this.Controls.Add(this.grpCompetition);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "ClientControlBoard";
            this.Text = "Client of Underwater Robot Water Polo Game Sim 2D";
            this.Load += new System.EventHandler(this.ClientControlBoard_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ClientControlBoard_FormClosed);
            this.grpCompetition.ResumeLayout(false);
            this.grpTeamInfo.ResumeLayout(false);
            this.grpStrategy.ResumeLayout(false);
            this.grpStrategy.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.GroupBox grpCompetition;
        private System.Windows.Forms.GroupBox grpTeamInfo;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblTeamName;
        private System.Windows.Forms.Label lblHalfCourt;
        private System.Windows.Forms.RadioButton rdoLeft;
        private System.Windows.Forms.RadioButton rdoRight;
        private System.Windows.Forms.GroupBox grpStrategy;
        private System.Windows.Forms.TextBox txtStrategy;
        private System.Windows.Forms.Button btnReady;
        private System.Windows.Forms.Label lblLocation;
        private System.Windows.Forms.Button btnStrategyBrowse;
        private System.Windows.Forms.Label lblCompetitionType;
        private System.Windows.Forms.Label lblTeamId;
        private System.Windows.Forms.Label lblId;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtCompetitionState;

    }
}