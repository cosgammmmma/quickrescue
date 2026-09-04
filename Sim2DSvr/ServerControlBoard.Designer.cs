namespace URWPGSim2D.Sim2DSvr
{
    partial class ServerControlBoard
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.GroupBox grpBackgroundMusic;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerControlBoard));
            System.Windows.Forms.GroupBox grpStrategy;
            System.Windows.Forms.GroupBox grpMatchSetting;
            System.Windows.Forms.Label label12;
            System.Windows.Forms.Label LB_Time_Choose;
            System.Windows.Forms.Label LB_Type;
            System.Windows.Forms.GroupBox grpCommands;
            System.Windows.Forms.GroupBox grpVelocityTest;
            System.Windows.Forms.Label label16;
            System.Windows.Forms.Label label15;
            System.Windows.Forms.GroupBox grpBallSetting;
            System.Windows.Forms.Label label84;
            System.Windows.Forms.Label label85;
            System.Windows.Forms.Label label86;
            System.Windows.Forms.Label label87;
            System.Windows.Forms.GroupBox grpFishSetting;
            System.Windows.Forms.Label label60;
            System.Windows.Forms.Label label59;
            System.Windows.Forms.Label label58;
            System.Windows.Forms.Label label57;
            System.Windows.Forms.Label label56;
            System.Windows.Forms.Label label55;
            System.Windows.Forms.Label label54;
            System.Windows.Forms.GroupBox grpFishDefaultSetting;
            System.Windows.Forms.GroupBox grpObstacleSetting;
            System.Windows.Forms.GroupBox grpObstacleColor;
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.GroupBox grpObstacleDirection;
            System.Windows.Forms.Label label3;
            System.Windows.Forms.Label label4;
            System.Windows.Forms.Label label28;
            System.Windows.Forms.Label label27;
            System.Windows.Forms.GroupBox grpObstacleSize;
            System.Windows.Forms.Label label6;
            System.Windows.Forms.Label label7;
            System.Windows.Forms.GroupBox grpObstaclAndChannel;
            System.Windows.Forms.GroupBox grpObstacleType;
            System.Windows.Forms.Label label32;
            System.Windows.Forms.Label label31;
            System.Windows.Forms.Label label30;
            System.Windows.Forms.Label label19;
            System.Windows.Forms.Label label13;
            System.Windows.Forms.Label label10;
            System.Windows.Forms.Label label9;
            System.Windows.Forms.TabPage tabPage1;
            System.Windows.Forms.TabPage tabPage2;
            System.Windows.Forms.TabPage tabPage3;
            System.Windows.Forms.Label label8;
            System.Windows.Forms.TabPage tabPage4;
            System.Windows.Forms.Label label47;
            System.Windows.Forms.Label label46;
            System.Windows.Forms.Label label45;
            System.Windows.Forms.Label label44;
            System.Windows.Forms.Label label43;
            System.Windows.Forms.Label label42;
            System.Windows.Forms.Label label41;
            System.Windows.Forms.Label label39;
            System.Windows.Forms.Label label38;
            System.Windows.Forms.Panel panel5;
            this.btnMusicStop = new System.Windows.Forms.Button();
            this.btnMusicPause = new System.Windows.Forms.Button();
            this.btnMusicPlay = new System.Windows.Forms.Button();
            this.btnMusicBrowse = new System.Windows.Forms.Button();
            this.txtMusicName = new System.Windows.Forms.TextBox();
            this.pnlStrategy = new System.Windows.Forms.Panel();
            this.rdoRemote = new System.Windows.Forms.RadioButton();
            this.rdoLocal = new System.Windows.Forms.RadioButton();
            this.cmbCompetitionItem = new System.Windows.Forms.ComboBox();
            this.cmbCompetitionTime = new System.Windows.Forms.ComboBox();
            this.btnDisplayFishInfo = new System.Windows.Forms.Button();
            this.btnCapture = new System.Windows.Forms.Button();
            this.btnDrawTrajectory = new System.Windows.Forms.Button();
            this.btnReplay = new System.Windows.Forms.Button();
            this.btnRestart = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnRecordVideo = new System.Windows.Forms.Button();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnTestVelocity = new System.Windows.Forms.Button();
            this.trbVelocityCode = new System.Windows.Forms.TrackBar();
            this.trbSwervingCode = new System.Windows.Forms.TrackBar();
            this.pnlBallSetting = new System.Windows.Forms.Panel();
            this.pnlFishSetting = new System.Windows.Forms.Panel();
            this.btnFishDefaultSetting = new System.Windows.Forms.Button();
            this.btnObstacleColorFilled = new System.Windows.Forms.Button();
            this.btnObstacleColorBorder = new System.Windows.Forms.Button();
            this.cmbObstacleDirection = new URWPGSim2D.Common.NumberComboBox();
            this.grpObstaclePosition = new System.Windows.Forms.GroupBox();
            this.txtObstaclePosZ = new URWPGSim2D.Common.NumberTextBox();
            this.txtObstaclePosX = new URWPGSim2D.Common.NumberTextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.lblObstacleLength = new System.Windows.Forms.Label();
            this.txtObstacleWidth = new URWPGSim2D.Common.NumberTextBox();
            this.txtObstacleLength = new URWPGSim2D.Common.NumberTextBox();
            this.btnObstacleModify = new System.Windows.Forms.Button();
            this.btnObstacleDelete = new System.Windows.Forms.Button();
            this.btnObstacleAdd = new System.Windows.Forms.Button();
            this.lstObstacle = new System.Windows.Forms.ListBox();
            this.cmbObastacleType = new System.Windows.Forms.ComboBox();
            this.grpFieldSetting = new System.Windows.Forms.GroupBox();
            this.chkWaveEffect = new System.Windows.Forms.CheckBox();
            this.cmbFieldWidth = new System.Windows.Forms.ComboBox();
            this.cmbFieldLength = new URWPGSim2D.Common.NumberComboBox();
            this.grpFieldDefaultSetting = new System.Windows.Forms.GroupBox();
            this.btnFieldDefaultSetting = new System.Windows.Forms.Button();
            this.picMatch = new System.Windows.Forms.PictureBox();
            this.tabServerControlBoard = new System.Windows.Forms.TabControl();
            this.lblFishOrBallPosition = new System.Windows.Forms.Label();
            this.lblFishOrBallSelected = new System.Windows.Forms.Label();
            this.lblTmp = new System.Windows.Forms.Label();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.picLoader = new System.Windows.Forms.PictureBox();
            this.wave_timer = new System.Windows.Forms.Timer(this.components);
            this.fish_timer = new System.Windows.Forms.Timer(this.components);
            grpBackgroundMusic = new System.Windows.Forms.GroupBox();
            grpStrategy = new System.Windows.Forms.GroupBox();
            grpMatchSetting = new System.Windows.Forms.GroupBox();
            label12 = new System.Windows.Forms.Label();
            LB_Time_Choose = new System.Windows.Forms.Label();
            LB_Type = new System.Windows.Forms.Label();
            grpCommands = new System.Windows.Forms.GroupBox();
            grpVelocityTest = new System.Windows.Forms.GroupBox();
            label16 = new System.Windows.Forms.Label();
            label15 = new System.Windows.Forms.Label();
            grpBallSetting = new System.Windows.Forms.GroupBox();
            label84 = new System.Windows.Forms.Label();
            label85 = new System.Windows.Forms.Label();
            label86 = new System.Windows.Forms.Label();
            label87 = new System.Windows.Forms.Label();
            grpFishSetting = new System.Windows.Forms.GroupBox();
            label60 = new System.Windows.Forms.Label();
            label59 = new System.Windows.Forms.Label();
            label58 = new System.Windows.Forms.Label();
            label57 = new System.Windows.Forms.Label();
            label56 = new System.Windows.Forms.Label();
            label55 = new System.Windows.Forms.Label();
            label54 = new System.Windows.Forms.Label();
            grpFishDefaultSetting = new System.Windows.Forms.GroupBox();
            grpObstacleSetting = new System.Windows.Forms.GroupBox();
            grpObstacleColor = new System.Windows.Forms.GroupBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            grpObstacleDirection = new System.Windows.Forms.GroupBox();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label28 = new System.Windows.Forms.Label();
            label27 = new System.Windows.Forms.Label();
            grpObstacleSize = new System.Windows.Forms.GroupBox();
            label6 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            grpObstaclAndChannel = new System.Windows.Forms.GroupBox();
            grpObstacleType = new System.Windows.Forms.GroupBox();
            label32 = new System.Windows.Forms.Label();
            label31 = new System.Windows.Forms.Label();
            label30 = new System.Windows.Forms.Label();
            label19 = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            tabPage1 = new System.Windows.Forms.TabPage();
            tabPage2 = new System.Windows.Forms.TabPage();
            tabPage3 = new System.Windows.Forms.TabPage();
            label8 = new System.Windows.Forms.Label();
            tabPage4 = new System.Windows.Forms.TabPage();
            label47 = new System.Windows.Forms.Label();
            label46 = new System.Windows.Forms.Label();
            label45 = new System.Windows.Forms.Label();
            label44 = new System.Windows.Forms.Label();
            label43 = new System.Windows.Forms.Label();
            label42 = new System.Windows.Forms.Label();
            label41 = new System.Windows.Forms.Label();
            label39 = new System.Windows.Forms.Label();
            label38 = new System.Windows.Forms.Label();
            panel5 = new System.Windows.Forms.Panel();
            grpBackgroundMusic.SuspendLayout();
            grpStrategy.SuspendLayout();
            grpMatchSetting.SuspendLayout();
            grpCommands.SuspendLayout();
            grpVelocityTest.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trbVelocityCode)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trbSwervingCode)).BeginInit();
            grpBallSetting.SuspendLayout();
            grpFishSetting.SuspendLayout();
            grpFishDefaultSetting.SuspendLayout();
            grpObstacleSetting.SuspendLayout();
            grpObstacleColor.SuspendLayout();
            grpObstacleDirection.SuspendLayout();
            this.grpObstaclePosition.SuspendLayout();
            grpObstacleSize.SuspendLayout();
            grpObstaclAndChannel.SuspendLayout();
            grpObstacleType.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            tabPage3.SuspendLayout();
            this.grpFieldSetting.SuspendLayout();
            this.grpFieldDefaultSetting.SuspendLayout();
            tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMatch)).BeginInit();
            this.tabServerControlBoard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLoader)).BeginInit();
            this.SuspendLayout();
            // 
            // grpBackgroundMusic
            // 
            grpBackgroundMusic.Controls.Add(this.btnMusicStop);
            grpBackgroundMusic.Controls.Add(this.btnMusicPause);
            grpBackgroundMusic.Controls.Add(this.btnMusicPlay);
            grpBackgroundMusic.Controls.Add(this.btnMusicBrowse);
            grpBackgroundMusic.Controls.Add(this.txtMusicName);
            resources.ApplyResources(grpBackgroundMusic, "grpBackgroundMusic");
            grpBackgroundMusic.Name = "grpBackgroundMusic";
            grpBackgroundMusic.TabStop = false;
            // 
            // btnMusicStop
            // 
            resources.ApplyResources(this.btnMusicStop, "btnMusicStop");
            this.btnMusicStop.Name = "btnMusicStop";
            this.toolTip.SetToolTip(this.btnMusicStop, resources.GetString("btnMusicStop.ToolTip"));
            this.btnMusicStop.UseVisualStyleBackColor = true;
            this.btnMusicStop.Click += new System.EventHandler(this.btnMusicStop_Click);
            // 
            // btnMusicPause
            // 
            resources.ApplyResources(this.btnMusicPause, "btnMusicPause");
            this.btnMusicPause.Name = "btnMusicPause";
            this.toolTip.SetToolTip(this.btnMusicPause, resources.GetString("btnMusicPause.ToolTip"));
            this.btnMusicPause.UseVisualStyleBackColor = true;
            this.btnMusicPause.Click += new System.EventHandler(this.btnMusicPause_Click);
            // 
            // btnMusicPlay
            // 
            resources.ApplyResources(this.btnMusicPlay, "btnMusicPlay");
            this.btnMusicPlay.Name = "btnMusicPlay";
            this.toolTip.SetToolTip(this.btnMusicPlay, resources.GetString("btnMusicPlay.ToolTip"));
            this.btnMusicPlay.UseVisualStyleBackColor = true;
            this.btnMusicPlay.Click += new System.EventHandler(this.btnMusicPlay_Click);
            // 
            // btnMusicBrowse
            // 
            resources.ApplyResources(this.btnMusicBrowse, "btnMusicBrowse");
            this.btnMusicBrowse.Name = "btnMusicBrowse";
            this.btnMusicBrowse.UseVisualStyleBackColor = true;
            this.btnMusicBrowse.Click += new System.EventHandler(this.btnMusicBrowse_Click);
            // 
            // txtMusicName
            // 
            resources.ApplyResources(this.txtMusicName, "txtMusicName");
            this.txtMusicName.Name = "txtMusicName";
            this.txtMusicName.MouseHover += new System.EventHandler(this.txtMusicName_MouseHover);
            // 
            // grpStrategy
            // 
            grpStrategy.Controls.Add(this.pnlStrategy);
            grpStrategy.Controls.Add(this.rdoRemote);
            grpStrategy.Controls.Add(this.rdoLocal);
            resources.ApplyResources(grpStrategy, "grpStrategy");
            grpStrategy.Name = "grpStrategy";
            grpStrategy.TabStop = false;
            // 
            // pnlStrategy
            // 
            resources.ApplyResources(this.pnlStrategy, "pnlStrategy");
            this.pnlStrategy.Name = "pnlStrategy";
            // 
            // rdoRemote
            // 
            resources.ApplyResources(this.rdoRemote, "rdoRemote");
            this.rdoRemote.Checked = true;
            this.rdoRemote.Name = "rdoRemote";
            this.rdoRemote.TabStop = true;
            this.rdoRemote.UseVisualStyleBackColor = true;
            this.rdoRemote.Click += new System.EventHandler(this.rdoRemote_Click);
            // 
            // rdoLocal
            // 
            resources.ApplyResources(this.rdoLocal, "rdoLocal");
            this.rdoLocal.Name = "rdoLocal";
            this.rdoLocal.UseVisualStyleBackColor = true;
            this.rdoLocal.Click += new System.EventHandler(this.rdoLocal_Click);
            // 
            // grpMatchSetting
            // 
            grpMatchSetting.Controls.Add(label12);
            grpMatchSetting.Controls.Add(LB_Time_Choose);
            grpMatchSetting.Controls.Add(this.cmbCompetitionItem);
            grpMatchSetting.Controls.Add(this.cmbCompetitionTime);
            grpMatchSetting.Controls.Add(LB_Type);
            grpMatchSetting.Cursor = System.Windows.Forms.Cursors.Arrow;
            resources.ApplyResources(grpMatchSetting, "grpMatchSetting");
            grpMatchSetting.Name = "grpMatchSetting";
            grpMatchSetting.TabStop = false;
            // 
            // label12
            // 
            resources.ApplyResources(label12, "label12");
            label12.Name = "label12";
            // 
            // LB_Time_Choose
            // 
            resources.ApplyResources(LB_Time_Choose, "LB_Time_Choose");
            LB_Time_Choose.Name = "LB_Time_Choose";
            // 
            // cmbCompetitionItem
            // 
            this.cmbCompetitionItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.cmbCompetitionItem, "cmbCompetitionItem");
            this.cmbCompetitionItem.FormattingEnabled = true;
            this.cmbCompetitionItem.Items.AddRange(new object[] {
         //   resources.GetString("cmbCompetitionItem.Items"),
           // resources.GetString("cmbCompetitionItem.Items1"),
           // resources.GetString("cmbCompetitionItem.Items2"),
            //resources.GetString("cmbCompetitionItem.Items3"),
           // resources.GetString("cmbCompetitionItem.Items4"),
            resources.GetString("cmbCompetitionItem.Items5"),
           // resources.GetString("cmbCompetitionItem.Items6"),
            //resources.GetString("cmbCompetitionItem.Items7"),
            resources.GetString("cmbCompetitionItem.Items8"),
            resources.GetString("cmbCompetitionItem.Items9"),
            resources.GetString("cmbCompetitionItem.Items10"),
            resources.GetString("cmbCompetitionItem.Items11"),
            resources.GetString("cmbCompetitionItem.Items12")
            });
            this.cmbCompetitionItem.Name = "cmbCompetitionItem";
            this.cmbCompetitionItem.SelectedIndexChanged += new System.EventHandler(this.cmbCompetitionItem_SelectedIndexChanged);
            // 
            // cmbCompetitionTime
            // 
            this.cmbCompetitionTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.cmbCompetitionTime, "cmbCompetitionTime");
            this.cmbCompetitionTime.FormattingEnabled = true;
            this.cmbCompetitionTime.Items.AddRange(new object[] {
            resources.GetString("cmbCompetitionTime.Items"),
            resources.GetString("cmbCompetitionTime.Items1"),
            resources.GetString("cmbCompetitionTime.Items2"),
            resources.GetString("cmbCompetitionTime.Items3")});
            this.cmbCompetitionTime.Name = "cmbCompetitionTime";
            this.cmbCompetitionTime.SelectedIndexChanged += new System.EventHandler(this.cmbCompetitionTime_SelectedIndexChanged);
            // 
            // LB_Type
            // 
            resources.ApplyResources(LB_Type, "LB_Type");
            LB_Type.Name = "LB_Type";
            // 
            // grpCommands
            // 
            grpCommands.Controls.Add(this.btnDisplayFishInfo);
            grpCommands.Controls.Add(this.btnCapture);
            grpCommands.Controls.Add(this.btnDrawTrajectory);
            grpCommands.Controls.Add(this.btnReplay);
            grpCommands.Controls.Add(this.btnRestart);
            grpCommands.Controls.Add(this.btnStart);
            grpCommands.Controls.Add(this.btnRecordVideo);
            grpCommands.Controls.Add(this.btnPause);
            resources.ApplyResources(grpCommands, "grpCommands");
            grpCommands.Name = "grpCommands";
            grpCommands.TabStop = false;
            // 
            // btnDisplayFishInfo
            // 
            resources.ApplyResources(this.btnDisplayFishInfo, "btnDisplayFishInfo");
            this.btnDisplayFishInfo.Name = "btnDisplayFishInfo";
            this.toolTip.SetToolTip(this.btnDisplayFishInfo, resources.GetString("btnDisplayFishInfo.ToolTip"));
            this.btnDisplayFishInfo.UseVisualStyleBackColor = true;
            this.btnDisplayFishInfo.Click += new System.EventHandler(this.btnDisplayFishInfo_Click);
            // 
            // btnCapture
            // 
            resources.ApplyResources(this.btnCapture, "btnCapture");
            this.btnCapture.Name = "btnCapture";
            this.toolTip.SetToolTip(this.btnCapture, resources.GetString("btnCapture.ToolTip"));
            this.btnCapture.UseVisualStyleBackColor = true;
            this.btnCapture.Click += new System.EventHandler(this.btnCapture_Click);
            // 
            // btnDrawTrajectory
            // 
            resources.ApplyResources(this.btnDrawTrajectory, "btnDrawTrajectory");
            this.btnDrawTrajectory.Name = "btnDrawTrajectory";
            this.toolTip.SetToolTip(this.btnDrawTrajectory, resources.GetString("btnDrawTrajectory.ToolTip"));
            this.btnDrawTrajectory.UseVisualStyleBackColor = true;
            this.btnDrawTrajectory.Click += new System.EventHandler(this.btnDrawTrajectory_Click);
            // 
            // btnReplay
            // 
            resources.ApplyResources(this.btnReplay, "btnReplay");
            this.btnReplay.Name = "btnReplay";
            this.toolTip.SetToolTip(this.btnReplay, resources.GetString("btnReplay.ToolTip"));
            this.btnReplay.UseVisualStyleBackColor = true;
            this.btnReplay.Click += new System.EventHandler(this.btnReplay_Click);
            // 
            // btnRestart
            // 
            resources.ApplyResources(this.btnRestart, "btnRestart");
            this.btnRestart.Name = "btnRestart";
            this.toolTip.SetToolTip(this.btnRestart, resources.GetString("btnRestart.ToolTip"));
            this.btnRestart.UseVisualStyleBackColor = true;
            this.btnRestart.Click += new System.EventHandler(this.btnRestart_Click);
            // 
            // btnStart
            // 
            resources.ApplyResources(this.btnStart, "btnStart");
            this.btnStart.Name = "btnStart";
            this.toolTip.SetToolTip(this.btnStart, resources.GetString("btnStart.ToolTip"));
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnRecordVideo
            // 
            resources.ApplyResources(this.btnRecordVideo, "btnRecordVideo");
            this.btnRecordVideo.Name = "btnRecordVideo";
            this.toolTip.SetToolTip(this.btnRecordVideo, resources.GetString("btnRecordVideo.ToolTip"));
            this.btnRecordVideo.UseVisualStyleBackColor = true;
            this.btnRecordVideo.Click += new System.EventHandler(this.btnRecordVideo_Click);
            // 
            // btnPause
            // 
            resources.ApplyResources(this.btnPause, "btnPause");
            this.btnPause.Name = "btnPause";
            this.toolTip.SetToolTip(this.btnPause, resources.GetString("btnPause.ToolTip"));
            this.btnPause.UseVisualStyleBackColor = true;
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
            // 
            // grpVelocityTest
            // 
            grpVelocityTest.Controls.Add(label16);
            grpVelocityTest.Controls.Add(label15);
            grpVelocityTest.Controls.Add(this.btnTestVelocity);
            grpVelocityTest.Controls.Add(this.trbVelocityCode);
            grpVelocityTest.Controls.Add(this.trbSwervingCode);
            resources.ApplyResources(grpVelocityTest, "grpVelocityTest");
            grpVelocityTest.Name = "grpVelocityTest";
            grpVelocityTest.TabStop = false;
            // 
            // label16
            // 
            resources.ApplyResources(label16, "label16");
            label16.Name = "label16";
            // 
            // label15
            // 
            resources.ApplyResources(label15, "label15");
            label15.Name = "label15";
            // 
            // btnTestVelocity
            // 
            this.btnTestVelocity.BackColor = System.Drawing.Color.White;
            this.btnTestVelocity.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            resources.ApplyResources(this.btnTestVelocity, "btnTestVelocity");
            this.btnTestVelocity.Name = "btnTestVelocity";
            this.btnTestVelocity.UseVisualStyleBackColor = true;
            this.btnTestVelocity.Click += new System.EventHandler(this.btnTestVelocity_Click);
            // 
            // trbVelocityCode
            // 
            resources.ApplyResources(this.trbVelocityCode, "trbVelocityCode");
            this.trbVelocityCode.Maximum = 15;
            this.trbVelocityCode.Name = "trbVelocityCode";
            this.trbVelocityCode.Scroll += new System.EventHandler(this.trbVelocityCode_Scroll);
            // 
            // trbSwervingCode
            // 
            resources.ApplyResources(this.trbSwervingCode, "trbSwervingCode");
            this.trbSwervingCode.Maximum = 15;
            this.trbSwervingCode.Name = "trbSwervingCode";
            this.trbSwervingCode.Value = 7;
            this.trbSwervingCode.Scroll += new System.EventHandler(this.trbSwervingCode_Scroll);
            // 
            // grpBallSetting
            // 
            grpBallSetting.Controls.Add(label84);
            grpBallSetting.Controls.Add(label85);
            grpBallSetting.Controls.Add(label86);
            grpBallSetting.Controls.Add(label87);
            grpBallSetting.Controls.Add(this.pnlBallSetting);
            resources.ApplyResources(grpBallSetting, "grpBallSetting");
            grpBallSetting.Name = "grpBallSetting";
            grpBallSetting.TabStop = false;
            // 
            // label84
            // 
            resources.ApplyResources(label84, "label84");
            label84.Name = "label84";
            // 
            // label85
            // 
            resources.ApplyResources(label85, "label85");
            label85.Name = "label85";
            // 
            // label86
            // 
            resources.ApplyResources(label86, "label86");
            label86.Name = "label86";
            // 
            // label87
            // 
            resources.ApplyResources(label87, "label87");
            label87.Name = "label87";
            // 
            // pnlBallSetting
            // 
            resources.ApplyResources(this.pnlBallSetting, "pnlBallSetting");
            this.pnlBallSetting.Name = "pnlBallSetting";
            // 
            // grpFishSetting
            // 
            grpFishSetting.Controls.Add(label60);
            grpFishSetting.Controls.Add(label59);
            grpFishSetting.Controls.Add(label58);
            grpFishSetting.Controls.Add(label57);
            grpFishSetting.Controls.Add(label56);
            grpFishSetting.Controls.Add(label55);
            grpFishSetting.Controls.Add(label54);
            grpFishSetting.Controls.Add(this.pnlFishSetting);
            resources.ApplyResources(grpFishSetting, "grpFishSetting");
            grpFishSetting.Name = "grpFishSetting";
            grpFishSetting.TabStop = false;
            // 
            // label60
            // 
            resources.ApplyResources(label60, "label60");
            label60.Name = "label60";
            // 
            // label59
            // 
            resources.ApplyResources(label59, "label59");
            label59.Name = "label59";
            // 
            // label58
            // 
            resources.ApplyResources(label58, "label58");
            label58.Name = "label58";
            // 
            // label57
            // 
            resources.ApplyResources(label57, "label57");
            label57.Name = "label57";
            // 
            // label56
            // 
            resources.ApplyResources(label56, "label56");
            label56.Name = "label56";
            // 
            // label55
            // 
            resources.ApplyResources(label55, "label55");
            label55.Name = "label55";
            // 
            // label54
            // 
            resources.ApplyResources(label54, "label54");
            label54.Name = "label54";
            // 
            // pnlFishSetting
            // 
            resources.ApplyResources(this.pnlFishSetting, "pnlFishSetting");
            this.pnlFishSetting.Name = "pnlFishSetting";
            // 
            // grpFishDefaultSetting
            // 
            grpFishDefaultSetting.Controls.Add(this.btnFishDefaultSetting);
            resources.ApplyResources(grpFishDefaultSetting, "grpFishDefaultSetting");
            grpFishDefaultSetting.Name = "grpFishDefaultSetting";
            grpFishDefaultSetting.TabStop = false;
            // 
            // btnFishDefaultSetting
            // 
            this.btnFishDefaultSetting.BackColor = System.Drawing.Color.White;
            this.btnFishDefaultSetting.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            resources.ApplyResources(this.btnFishDefaultSetting, "btnFishDefaultSetting");
            this.btnFishDefaultSetting.Name = "btnFishDefaultSetting";
            this.btnFishDefaultSetting.UseVisualStyleBackColor = true;
            this.btnFishDefaultSetting.Click += new System.EventHandler(this.btnFishDefaultSetting_Click);
            // 
            // grpObstacleSetting
            // 
            grpObstacleSetting.BackColor = System.Drawing.Color.WhiteSmoke;
            grpObstacleSetting.Controls.Add(grpObstacleColor);
            grpObstacleSetting.Controls.Add(grpObstacleDirection);
            grpObstacleSetting.Controls.Add(this.grpObstaclePosition);
            grpObstacleSetting.Controls.Add(grpObstacleSize);
            grpObstacleSetting.Controls.Add(this.btnObstacleModify);
            grpObstacleSetting.Controls.Add(this.btnObstacleDelete);
            grpObstacleSetting.Controls.Add(this.btnObstacleAdd);
            grpObstacleSetting.Controls.Add(grpObstaclAndChannel);
            grpObstacleSetting.Controls.Add(grpObstacleType);
            resources.ApplyResources(grpObstacleSetting, "grpObstacleSetting");
            grpObstacleSetting.Name = "grpObstacleSetting";
            grpObstacleSetting.TabStop = false;
            // 
            // grpObstacleColor
            // 
            grpObstacleColor.Controls.Add(this.btnObstacleColorFilled);
            grpObstacleColor.Controls.Add(this.btnObstacleColorBorder);
            grpObstacleColor.Controls.Add(label1);
            grpObstacleColor.Controls.Add(label2);
            resources.ApplyResources(grpObstacleColor, "grpObstacleColor");
            grpObstacleColor.Name = "grpObstacleColor";
            grpObstacleColor.TabStop = false;
            // 
            // btnObstacleColorFilled
            // 
            resources.ApplyResources(this.btnObstacleColorFilled, "btnObstacleColorFilled");
            this.btnObstacleColorFilled.Name = "btnObstacleColorFilled";
            this.btnObstacleColorFilled.UseVisualStyleBackColor = true;
            this.btnObstacleColorFilled.Click += new System.EventHandler(this.btnObstacleColor_Click);
            // 
            // btnObstacleColorBorder
            // 
            resources.ApplyResources(this.btnObstacleColorBorder, "btnObstacleColorBorder");
            this.btnObstacleColorBorder.Name = "btnObstacleColorBorder";
            this.btnObstacleColorBorder.UseVisualStyleBackColor = true;
            this.btnObstacleColorBorder.Click += new System.EventHandler(this.btnObstacleColor_Click);
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            // 
            // grpObstacleDirection
            // 
            grpObstacleDirection.Controls.Add(this.cmbObstacleDirection);
            resources.ApplyResources(grpObstacleDirection, "grpObstacleDirection");
            grpObstacleDirection.Name = "grpObstacleDirection";
            grpObstacleDirection.TabStop = false;
            // 
            // cmbObstacleDirection
            // 
            // TODO: “this.cmbObstacleDirection.EditHandle”的代码生成失败，原因是出现异常“无效的基元类型: System.IntPtr。请考虑使用 CodeObjectCreateExpression。”。
            this.cmbObstacleDirection.FormattingEnabled = true;
            this.cmbObstacleDirection.Items.AddRange(new object[] {
            resources.GetString("cmbObstacleDirection.Items"),
            resources.GetString("cmbObstacleDirection.Items1"),
            resources.GetString("cmbObstacleDirection.Items2"),
            resources.GetString("cmbObstacleDirection.Items3"),
            resources.GetString("cmbObstacleDirection.Items4"),
            resources.GetString("cmbObstacleDirection.Items5")});
            resources.ApplyResources(this.cmbObstacleDirection, "cmbObstacleDirection");
            this.cmbObstacleDirection.Name = "cmbObstacleDirection";
            // 
            // grpObstaclePosition
            // 
            this.grpObstaclePosition.Controls.Add(this.txtObstaclePosZ);
            this.grpObstaclePosition.Controls.Add(this.txtObstaclePosX);
            this.grpObstaclePosition.Controls.Add(label3);
            this.grpObstaclePosition.Controls.Add(label4);
            this.grpObstaclePosition.Controls.Add(label28);
            this.grpObstaclePosition.Controls.Add(label27);
            resources.ApplyResources(this.grpObstaclePosition, "grpObstaclePosition");
            this.grpObstaclePosition.Name = "grpObstaclePosition";
            this.grpObstaclePosition.TabStop = false;
            // 
            // txtObstaclePosZ
            // 
            resources.ApplyResources(this.txtObstaclePosZ, "txtObstaclePosZ");
            this.txtObstaclePosZ.Name = "txtObstaclePosZ";
            // 
            // txtObstaclePosX
            // 
            resources.ApplyResources(this.txtObstaclePosX, "txtObstaclePosX");
            this.txtObstaclePosX.Name = "txtObstaclePosX";
            // 
            // label3
            // 
            resources.ApplyResources(label3, "label3");
            label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(label4, "label4");
            label4.Name = "label4";
            // 
            // label28
            // 
            resources.ApplyResources(label28, "label28");
            label28.Name = "label28";
            // 
            // label27
            // 
            resources.ApplyResources(label27, "label27");
            label27.Name = "label27";
            // 
            // grpObstacleSize
            // 
            grpObstacleSize.Controls.Add(label6);
            grpObstacleSize.Controls.Add(label7);
            grpObstacleSize.Controls.Add(this.label5);
            grpObstacleSize.Controls.Add(this.lblObstacleLength);
            grpObstacleSize.Controls.Add(this.txtObstacleWidth);
            grpObstacleSize.Controls.Add(this.txtObstacleLength);
            resources.ApplyResources(grpObstacleSize, "grpObstacleSize");
            grpObstacleSize.Name = "grpObstacleSize";
            grpObstacleSize.TabStop = false;
            // 
            // label6
            // 
            resources.ApplyResources(label6, "label6");
            label6.Name = "label6";
            // 
            // label7
            // 
            resources.ApplyResources(label7, "label7");
            label7.Name = "label7";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // lblObstacleLength
            // 
            resources.ApplyResources(this.lblObstacleLength, "lblObstacleLength");
            this.lblObstacleLength.Name = "lblObstacleLength";
            // 
            // txtObstacleWidth
            // 
            resources.ApplyResources(this.txtObstacleWidth, "txtObstacleWidth");
            this.txtObstacleWidth.Name = "txtObstacleWidth";
            // 
            // txtObstacleLength
            // 
            resources.ApplyResources(this.txtObstacleLength, "txtObstacleLength");
            this.txtObstacleLength.Name = "txtObstacleLength";
            // 
            // btnObstacleModify
            // 
            resources.ApplyResources(this.btnObstacleModify, "btnObstacleModify");
            this.btnObstacleModify.Name = "btnObstacleModify";
            this.btnObstacleModify.UseVisualStyleBackColor = true;
            this.btnObstacleModify.Click += new System.EventHandler(this.btnObstacleModify_Click);
            // 
            // btnObstacleDelete
            // 
            resources.ApplyResources(this.btnObstacleDelete, "btnObstacleDelete");
            this.btnObstacleDelete.Name = "btnObstacleDelete";
            this.btnObstacleDelete.UseVisualStyleBackColor = true;
            this.btnObstacleDelete.Click += new System.EventHandler(this.btnObstacleDelete_Click);
            // 
            // btnObstacleAdd
            // 
            resources.ApplyResources(this.btnObstacleAdd, "btnObstacleAdd");
            this.btnObstacleAdd.Name = "btnObstacleAdd";
            this.btnObstacleAdd.UseVisualStyleBackColor = true;
            this.btnObstacleAdd.Click += new System.EventHandler(this.btnObstacleAdd_Click);
            // 
            // grpObstaclAndChannel
            // 
            grpObstaclAndChannel.Controls.Add(this.lstObstacle);
            resources.ApplyResources(grpObstaclAndChannel, "grpObstaclAndChannel");
            grpObstaclAndChannel.Name = "grpObstaclAndChannel";
            grpObstaclAndChannel.TabStop = false;
            // 
            // lstObstacle
            // 
            this.lstObstacle.FormattingEnabled = true;
            resources.ApplyResources(this.lstObstacle, "lstObstacle");
            this.lstObstacle.Name = "lstObstacle";
            this.lstObstacle.SelectedIndexChanged += new System.EventHandler(this.lstObstacle_SelectedIndexChanged);
            // 
            // grpObstacleType
            // 
            grpObstacleType.Controls.Add(this.cmbObastacleType);
            resources.ApplyResources(grpObstacleType, "grpObstacleType");
            grpObstacleType.Name = "grpObstacleType";
            grpObstacleType.TabStop = false;
            // 
            // cmbObastacleType
            // 
            this.cmbObastacleType.BackColor = System.Drawing.SystemColors.Window;
            this.cmbObastacleType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.cmbObastacleType, "cmbObastacleType");
            this.cmbObastacleType.FormattingEnabled = true;
            this.cmbObastacleType.Items.AddRange(new object[] {
            resources.GetString("cmbObastacleType.Items"),
            resources.GetString("cmbObastacleType.Items1")});
            this.cmbObastacleType.Name = "cmbObastacleType";
            this.cmbObastacleType.SelectedIndexChanged += new System.EventHandler(this.cmbObastacleType_SelectedIndexChanged);
            // 
            // label32
            // 
            resources.ApplyResources(label32, "label32");
            label32.Name = "label32";
            // 
            // label31
            // 
            resources.ApplyResources(label31, "label31");
            label31.Name = "label31";
            // 
            // label30
            // 
            resources.ApplyResources(label30, "label30");
            label30.Name = "label30";
            // 
            // label19
            // 
            resources.ApplyResources(label19, "label19");
            label19.Name = "label19";
            // 
            // label13
            // 
            resources.ApplyResources(label13, "label13");
            label13.Name = "label13";
            // 
            // label10
            // 
            resources.ApplyResources(label10, "label10");
            label10.Name = "label10";
            // 
            // label9
            // 
            resources.ApplyResources(label9, "label9");
            label9.Name = "label9";
            // 
            // tabPage1
            // 
            tabPage1.BackColor = System.Drawing.Color.WhiteSmoke;
            tabPage1.Controls.Add(grpBackgroundMusic);
            tabPage1.Controls.Add(grpStrategy);
            tabPage1.Controls.Add(grpMatchSetting);
            tabPage1.Controls.Add(grpCommands);
            resources.ApplyResources(tabPage1, "tabPage1");
            tabPage1.Name = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.BackColor = System.Drawing.Color.WhiteSmoke;
            tabPage2.Controls.Add(grpVelocityTest);
            tabPage2.Controls.Add(grpBallSetting);
            tabPage2.Controls.Add(grpFishSetting);
            tabPage2.Controls.Add(grpFishDefaultSetting);
            resources.ApplyResources(tabPage2, "tabPage2");
            tabPage2.Name = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            tabPage3.BackColor = System.Drawing.Color.WhiteSmoke;
            tabPage3.Controls.Add(this.grpFieldSetting);
            tabPage3.Controls.Add(grpObstacleSetting);
            tabPage3.Controls.Add(this.grpFieldDefaultSetting);
            resources.ApplyResources(tabPage3, "tabPage3");
            tabPage3.Name = "tabPage3";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // grpFieldSetting
            // 
            this.grpFieldSetting.Controls.Add(this.chkWaveEffect);
            this.grpFieldSetting.Controls.Add(label8);
            this.grpFieldSetting.Controls.Add(label19);
            this.grpFieldSetting.Controls.Add(label13);
            this.grpFieldSetting.Controls.Add(label10);
            this.grpFieldSetting.Controls.Add(label9);
            this.grpFieldSetting.Controls.Add(this.cmbFieldWidth);
            this.grpFieldSetting.Controls.Add(this.cmbFieldLength);
            resources.ApplyResources(this.grpFieldSetting, "grpFieldSetting");
            this.grpFieldSetting.Name = "grpFieldSetting";
            this.grpFieldSetting.TabStop = false;
            // 
            // chkWaveEffect
            // 
            resources.ApplyResources(this.chkWaveEffect, "chkWaveEffect");
            this.chkWaveEffect.Name = "chkWaveEffect";
            this.chkWaveEffect.UseVisualStyleBackColor = true;
            this.chkWaveEffect.CheckedChanged += new System.EventHandler(this.chkWaveEffect_CheckedChanged);
            // 
            // label8
            // 
            resources.ApplyResources(label8, "label8");
            label8.Name = "label8";
            // 
            // cmbFieldWidth
            // 
            resources.ApplyResources(this.cmbFieldWidth, "cmbFieldWidth");
            this.cmbFieldWidth.FormattingEnabled = true;
            this.cmbFieldWidth.Items.AddRange(new object[] {
            resources.GetString("cmbFieldWidth.Items"),
            resources.GetString("cmbFieldWidth.Items1"),
            resources.GetString("cmbFieldWidth.Items2")});
            this.cmbFieldWidth.Name = "cmbFieldWidth";
            // 
            // cmbFieldLength
            // 
            // TODO: “this.cmbFieldLength.EditHandle”的代码生成失败，原因是出现异常“无效的基元类型: System.IntPtr。请考虑使用 CodeObjectCreateExpression。”。
            this.cmbFieldLength.FormattingEnabled = true;
            this.cmbFieldLength.Items.AddRange(new object[] {
            resources.GetString("cmbFieldLength.Items"),
            resources.GetString("cmbFieldLength.Items1"),
            resources.GetString("cmbFieldLength.Items2")});
            resources.ApplyResources(this.cmbFieldLength, "cmbFieldLength");
            this.cmbFieldLength.Name = "cmbFieldLength";
            this.cmbFieldLength.SelectedIndexChanged += new System.EventHandler(this.cmbFieldLength_SelectedIndexChanged);
            this.cmbFieldLength.Validated += new System.EventHandler(this.cmbFieldLength_Validated);
            // 
            // grpFieldDefaultSetting
            // 
            this.grpFieldDefaultSetting.Controls.Add(this.btnFieldDefaultSetting);
            resources.ApplyResources(this.grpFieldDefaultSetting, "grpFieldDefaultSetting");
            this.grpFieldDefaultSetting.Name = "grpFieldDefaultSetting";
            this.grpFieldDefaultSetting.TabStop = false;
            // 
            // btnFieldDefaultSetting
            // 
            this.btnFieldDefaultSetting.BackColor = System.Drawing.Color.White;
            this.btnFieldDefaultSetting.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            resources.ApplyResources(this.btnFieldDefaultSetting, "btnFieldDefaultSetting");
            this.btnFieldDefaultSetting.Name = "btnFieldDefaultSetting";
            this.btnFieldDefaultSetting.UseVisualStyleBackColor = false;
            this.btnFieldDefaultSetting.Click += new System.EventHandler(this.btnFieldDefaultSetting_Click);
            // 
            // tabPage4
            // 
            tabPage4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            tabPage4.Controls.Add(label47);
            tabPage4.Controls.Add(label46);
            tabPage4.Controls.Add(label45);
            tabPage4.Controls.Add(label44);
            tabPage4.Controls.Add(label43);
            tabPage4.Controls.Add(label42);
            tabPage4.Controls.Add(label41);
            tabPage4.Controls.Add(label39);
            tabPage4.Controls.Add(label38);
            tabPage4.Controls.Add(panel5);
            resources.ApplyResources(tabPage4, "tabPage4");
            tabPage4.Name = "tabPage4";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // label47
            // 
            resources.ApplyResources(label47, "label47");
            label47.Name = "label47";
            // 
            // label46
            // 
            resources.ApplyResources(label46, "label46");
            label46.Name = "label46";
            // 
            // label45
            // 
            resources.ApplyResources(label45, "label45");
            label45.Name = "label45";
            // 
            // label44
            // 
            resources.ApplyResources(label44, "label44");
            label44.ForeColor = System.Drawing.Color.Maroon;
            label44.Name = "label44";
            // 
            // label43
            // 
            resources.ApplyResources(label43, "label43");
            label43.Name = "label43";
            // 
            // label42
            // 
            resources.ApplyResources(label42, "label42");
            label42.ForeColor = System.Drawing.Color.Maroon;
            label42.Name = "label42";
            // 
            // label41
            // 
            resources.ApplyResources(label41, "label41");
            label41.Name = "label41";
            // 
            // label39
            // 
            resources.ApplyResources(label39, "label39");
            label39.Name = "label39";
            // 
            // label38
            // 
            resources.ApplyResources(label38, "label38");
            label38.Name = "label38";
            // 
            // panel5
            // 
            resources.ApplyResources(panel5, "panel5");
            panel5.Name = "panel5";
            // 
            // picMatch
            // 
            resources.ApplyResources(this.picMatch, "picMatch");
            this.picMatch.BackColor = System.Drawing.Color.Transparent;
            this.picMatch.Name = "picMatch";
            this.picMatch.TabStop = false;
            this.picMatch.DoubleClick += new System.EventHandler(this.picMatch_DoubleClick);
            this.picMatch.MouseLeave += new System.EventHandler(this.picMatch_MouseLeave);
            this.picMatch.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picMatch_MouseMove);
            this.picMatch.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picMatch_MouseDown);
            this.picMatch.Paint += new System.Windows.Forms.PaintEventHandler(this.picMatch_Paint);
            this.picMatch.MouseUp += new System.Windows.Forms.MouseEventHandler(this.picMatch_MouseUp);
            // 
            // tabServerControlBoard
            // 
            resources.ApplyResources(this.tabServerControlBoard, "tabServerControlBoard");
            this.tabServerControlBoard.Controls.Add(tabPage1);
            this.tabServerControlBoard.Controls.Add(tabPage2);
            this.tabServerControlBoard.Controls.Add(tabPage3);
            this.tabServerControlBoard.Controls.Add(tabPage4);
            this.tabServerControlBoard.Name = "tabServerControlBoard";
            this.tabServerControlBoard.SelectedIndex = 0;
            this.tabServerControlBoard.SelectedIndexChanged += new System.EventHandler(this.tabServerControlBoard_SelectedIndexChanged);
            // 
            // lblFishOrBallPosition
            // 
            this.lblFishOrBallPosition.BackColor = System.Drawing.Color.Transparent;
            this.lblFishOrBallPosition.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            resources.ApplyResources(this.lblFishOrBallPosition, "lblFishOrBallPosition");
            this.lblFishOrBallPosition.ForeColor = System.Drawing.Color.White;
            this.lblFishOrBallPosition.Name = "lblFishOrBallPosition";
            // 
            // lblFishOrBallSelected
            // 
            this.lblFishOrBallSelected.BackColor = System.Drawing.Color.Transparent;
            this.lblFishOrBallSelected.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            resources.ApplyResources(this.lblFishOrBallSelected, "lblFishOrBallSelected");
            this.lblFishOrBallSelected.ForeColor = System.Drawing.Color.White;
            this.lblFishOrBallSelected.Name = "lblFishOrBallSelected";
            // 
            // lblTmp
            // 
            resources.ApplyResources(this.lblTmp, "lblTmp");
            this.lblTmp.BackColor = System.Drawing.Color.Transparent;
            this.lblTmp.ForeColor = System.Drawing.Color.White;
            this.lblTmp.Name = "lblTmp";
            // 
            // picLoader
            // 
            resources.ApplyResources(this.picLoader, "picLoader");
            this.picLoader.Name = "picLoader";
            this.picLoader.TabStop = false;
            // 
            // wave_timer
            // 
            this.wave_timer.Enabled = true;
            this.wave_timer.Interval = 10;
            this.wave_timer.Tick += new System.EventHandler(this.wave_timer_Tick);
            // 
            // fish_timer
            // 
            this.fish_timer.Enabled = true;
            this.fish_timer.Interval = 1000;
            this.fish_timer.Tick += new System.EventHandler(this.fish_timer_Tick);
            // 
            // ServerControlBoard
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.picLoader);
            this.Controls.Add(this.lblTmp);
            this.Controls.Add(this.lblFishOrBallSelected);
            this.Controls.Add(this.lblFishOrBallPosition);
            this.Controls.Add(this.tabServerControlBoard);
            this.Controls.Add(this.picMatch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "ServerControlBoard";
            this.Load += new System.EventHandler(this.ServerControlBoard_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ServerControlBoard_FormClosed);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ServerControlBoard_KeyDown);
            grpBackgroundMusic.ResumeLayout(false);
            grpBackgroundMusic.PerformLayout();
            grpStrategy.ResumeLayout(false);
            grpStrategy.PerformLayout();
            grpMatchSetting.ResumeLayout(false);
            grpMatchSetting.PerformLayout();
            grpCommands.ResumeLayout(false);
            grpVelocityTest.ResumeLayout(false);
            grpVelocityTest.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trbVelocityCode)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trbSwervingCode)).EndInit();
            grpBallSetting.ResumeLayout(false);
            grpBallSetting.PerformLayout();
            grpFishSetting.ResumeLayout(false);
            grpFishSetting.PerformLayout();
            grpFishDefaultSetting.ResumeLayout(false);
            grpObstacleSetting.ResumeLayout(false);
            grpObstacleColor.ResumeLayout(false);
            grpObstacleColor.PerformLayout();
            grpObstacleDirection.ResumeLayout(false);
            this.grpObstaclePosition.ResumeLayout(false);
            this.grpObstaclePosition.PerformLayout();
            grpObstacleSize.ResumeLayout(false);
            grpObstacleSize.PerformLayout();
            grpObstaclAndChannel.ResumeLayout(false);
            grpObstacleType.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            this.grpFieldSetting.ResumeLayout(false);
            this.grpFieldSetting.PerformLayout();
            this.grpFieldDefaultSetting.ResumeLayout(false);
            tabPage4.ResumeLayout(false);
            tabPage4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMatch)).EndInit();
            this.tabServerControlBoard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picLoader)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picMatch;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.ComboBox cmbCompetitionItem;
        private System.Windows.Forms.ComboBox cmbCompetitionTime;
        private System.Windows.Forms.Button btnReplay;
        private System.Windows.Forms.Button btnRestart;
        private System.Windows.Forms.Button btnObstacleModify;
        private System.Windows.Forms.Button btnObstacleDelete;
        private System.Windows.Forms.Button btnObstacleAdd;
        private System.Windows.Forms.GroupBox grpObstaclePosition;
        private System.Windows.Forms.RadioButton rdoRemote;
        private System.Windows.Forms.RadioButton rdoLocal;
        private System.Windows.Forms.ComboBox cmbObastacleType;
        //private System.Windows.Forms.Label LB_Radius;
        private System.Windows.Forms.GroupBox grpFieldSetting;
        private System.Windows.Forms.ComboBox cmbFieldWidth;
        private System.Windows.Forms.Button btnFieldDefaultSetting;
        private System.Windows.Forms.GroupBox grpFieldDefaultSetting;
        private System.Windows.Forms.Button btnFishDefaultSetting;
        private System.Windows.Forms.Label lblFishOrBallPosition;
        //private System.Windows.Forms.Label label48;
        //private System.Windows.Forms.RichTextBox richTextBox4;
        //private System.Windows.Forms.RichTextBox richTextBox5;
        //private System.Windows.Forms.RichTextBox richTextBox6;
        //private System.Windows.Forms.GroupBox groupBox26;
        //private System.Windows.Forms.Button button3;
        //private System.Windows.Forms.Button button10;
        //private System.Windows.Forms.Label label61;
        //private System.Windows.Forms.Label label62;
        //private System.Windows.Forms.Label label63;
        //private System.Windows.Forms.Label label64;
        //private System.Windows.Forms.Label label65;
        //private System.Windows.Forms.Label label66;
        //private System.Windows.Forms.RichTextBox richTextBox7;
        //private System.Windows.Forms.RichTextBox richTextBox8;
        //private System.Windows.Forms.RichTextBox richTextBox9;
        //private System.Windows.Forms.GroupBox groupBox27;
        //private System.Windows.Forms.Button button11;
        //private System.Windows.Forms.Button button12;
        //private System.Windows.Forms.Label label67;
        //private System.Windows.Forms.Label label68;
        //private System.Windows.Forms.Label label69;
        //private System.Windows.Forms.Label label70;
        //private System.Windows.Forms.Label label71;
        //private System.Windows.Forms.Label label72;
        private System.Windows.Forms.Button btnTestVelocity;
        private System.Windows.Forms.Button btnCapture;
        private System.Windows.Forms.Button btnRecordVideo;
        private System.Windows.Forms.Label lblFishOrBallSelected;
        private System.Windows.Forms.Label lblTmp;
        private System.Windows.Forms.Panel pnlStrategy;
        private System.Windows.Forms.Button btnDisplayFishInfo;
        private System.Windows.Forms.Button btnDrawTrajectory;
        private System.Windows.Forms.Panel pnlFishSetting;
        //private System.Windows.Forms.Button BT_FishSettingConfirm_TeamB;
        //private System.Windows.Forms.TextBox TB_FishDirection_TeamB;
        //private System.Windows.Forms.TextBox TB_FishPositionY_TeamB;
        //private System.Windows.Forms.ComboBox CB_FishPlayer_TeamB;
        //private System.Windows.Forms.Button BT_FrontColor_TeamB;
        //private System.Windows.Forms.TextBox TB_FishPositionX_TeamB;
        //private System.Windows.Forms.Button BT_BackColor_TeamB;
        //private System.Windows.Forms.Button BT_FishSettingConfirm_TeamA;
        //private System.Windows.Forms.TextBox TB_FishDirection_TeamA;
        //private System.Windows.Forms.TextBox TB_FishPositionY_TeamA;
        //private System.Windows.Forms.ComboBox CB_FishPlayer_TeamA;
        //private System.Windows.Forms.Button BT_FrontColor_TeamA;
        //private System.Windows.Forms.TextBox TB_FishPositionX_TeamA;
        //private System.Windows.Forms.Button BT_BackColor_TeamA;
        private System.Windows.Forms.Panel pnlBallSetting;
        private System.Windows.Forms.TrackBar trbVelocityCode;
        private System.Windows.Forms.TrackBar trbSwervingCode;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Button btnObstacleColorBorder;
        private System.Windows.Forms.Button btnObstacleColorFilled;
        private System.Windows.Forms.ListBox lstObstacle;
        private URWPGSim2D.Common.NumberTextBox txtObstaclePosX;
        private URWPGSim2D.Common.NumberTextBox txtObstacleLength;
        private URWPGSim2D.Common.NumberTextBox txtObstaclePosZ;
        private URWPGSim2D.Common.NumberTextBox txtObstacleWidth;
        private URWPGSim2D.Common.NumberComboBox cmbObstacleDirection;
        private URWPGSim2D.Common.NumberComboBox cmbFieldLength;
        private System.Windows.Forms.TabControl tabServerControlBoard;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblObstacleLength;
        private System.Windows.Forms.Button btnMusicBrowse;
        private System.Windows.Forms.TextBox txtMusicName;
        private System.Windows.Forms.Button btnMusicStop;
        private System.Windows.Forms.Button btnMusicPause;
        private System.Windows.Forms.Button btnMusicPlay;
        private System.Windows.Forms.PictureBox picLoader;
        private System.Windows.Forms.Timer wave_timer;
        private System.Windows.Forms.Timer fish_timer;
        private System.Windows.Forms.CheckBox chkWaveEffect;

    }
}