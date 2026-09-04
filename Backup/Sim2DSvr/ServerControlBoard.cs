//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: ServerControlBoard.cs
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真平台服务端Sim2DSvr Dss Service实现文件之界面ServerControlBoard实现文件
// Histroy:
// Date: 20110512  Author: LiYoubing
// Modification: 
// 1.UnloadStrategy中增加null检查
// Date: 20110518  Author: LiYoubing
// Modification: 
// 1.picMatch_Paint中增加动态绘制半场标志（L/R和正方形色块）的功能
// 2.ServerControlBoard_KeyDown中给Start/Pause/Restart/Replay等按钮增加热键响应
// 3.btnRestart_Click中增加对半场交换标志量的检测和处理
// Date: 20110617  Author: LiYoubing
// Modification: 
// 1.btnFishDefaultSetting_Click中恢复仿真机器鱼和仿真水球颜色及位姿默认值
// Date: 20110710  Author: LiYoubing
// Modification: 
// 1.picMatch_Paint中修正仿真使命参与队伍名称和得分显示方式，根据Socre值的正负确定是否显示得分
// 2.添加背景音乐功能
// Date: 20110726  Author: LiYoubing
// Modification: 
// 1.cmbFieldLength_SelectedIndexChanged中解决运行时切换到Field选项卡时会把仿真机器鱼位置复位的bug
// 2.StateStart和StateRestart中加入对Field选项卡上场地尺寸参数设置控件Enabled状态的约束
// 3.Field选项卡上添加水波组件开关
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Drawing.Drawing2D;
using System.Diagnostics; // for Process
using System.Threading;
using xna = Microsoft.Xna.Framework;

using URWPGSim2D.Common;
using URWPGSim2D.Gadget;
using URWPGSim2D.StrategyLoader;

namespace URWPGSim2D.Sim2DSvr
{
    public partial class ServerControlBoard : Form
    {
        public ServerControlBoard()
        {
            InitializeComponent();

            try
            {
                SysConfig myConfig = SysConfig.Instance();
                _maxCachedCycles = Convert.ToInt32(myConfig.MyXmlReader("MaxCachedCycles"));
            }
            catch
            {
                Console.WriteLine("从配置文件读取参数出错...");
            }

            _bmpCachedImages = new Bitmap[_maxCachedCycles];
        }

        /// <summary>
        /// 动态对象（仿真机器鱼和仿真水球）实时信息显示对话框
        /// </summary>
        public DlgFishInfo dlgFishInfo = null;

        /// <summary>
        /// 动态对象（仿真机器鱼和仿真水球）运动轨迹绘制对话框
        /// </summary>
        public DlgTrajectory dlgTrajectory = null;

        /// <summary>
        /// 重绘场地绘图对象PictureBox控件的内容，每个仿真周期由SimulationLoop调用
        /// </summary>
        /// <param name="bmp">绘制好静态和动态图形对象如仿真机器鱼的Bitmap对象引用</param>
        public void DrawMatch(Bitmap bmp)
        {
            #region 存储当前画面，用于画面回放
            if (_curCachingIndex == _maxCachedCycles)
            {
                _curCachingIndex = 0;
            }

            // 此处要将需要保存的Bitmap对象克隆保存否则会被销毁而不能用
            if (_bmpCachedImages[_curCachingIndex] != null)
            {
                _bmpCachedImages[_curCachingIndex].Dispose();
            }
            _bmpCachedImages[_curCachingIndex] = (Bitmap)bmp.Clone();
            _curCachingIndex++;
            #endregion

            //picMatch.Image = bmp;
            SetMatchBmp(bmp);
        }

        /// <summary>
        /// 将新的Bitmap对象传给PictureBox控件的Image属性
        /// 并将PictureBox.Image属性的原值Bitmap对象销毁
        /// </summary>
        /// <param name="bmp">待设置为PictureBox.Image属性值的Bitmap对象引用</param>
        /// <remarks>
        /// 发现严重的内存泄漏问题 LiYoubing 20110222
        /// Server运行过程中，占用内存由几十M一直增长到1.3G左右再下降到一百多M，然后又上升，如此往复
        /// 这是因为每次Mission.Draw都创建新的Bitmap对象，CLR回收内存有滞后
        /// 多番比较最终确定如下解决方案并编写本方法：
        /// 在给picMatch.Image赋新值前先显式销毁老值对应的Bitmap对象
        /// </remarks>
        public void SetMatchBmp(Bitmap bmp)
        {
            if (picMatch.Image != null)
            {// 销毁已经存在的Bitmap对象
                picMatch.Image.Dispose();
            }

            // 将新的Bitmap对象传入
            picMatch.Image = bmp;
        }

        /// <summary>
        /// 将新的Bitmap对象传给PictureBox控件的BackgroundImage属性
        /// 并将PictureBox.BackgroundImage属性的原值Bitmap对象销毁
        /// 用于重绘仿真场地
        /// </summary>
        /// <param name="bmp">待设置为PictureBox.BackgroundImage属性值的Bitmap对象引用</param>
        public void SetFieldBmp(Bitmap bmp)
        {
            // 重绘仿真场地
            if (picMatch.BackgroundImage != null)
            {// 销毁已经存在的Bitmap对象
                picMatch.BackgroundImage.Dispose();
            }

            // 将新的Bitmap对象传入
            picMatch.BackgroundImage = bmp;
        }

        #region 主界面Referee面板开始/暂停/重启/回放按钮处理
        /// <summary>
        /// 开始事件响应，向Server服务实例发送START消息
        /// </summary>
        private void btnStart_Click(object sender, EventArgs e)
        {
            StateStart();

            FromServerUiEvents.Instance().Post(new FromServerUiMsg(FromServerUiMsg.MsgEnum.START,
                new string[] { FromServerUiMsg.MsgEnum.START.ToString() }));
        }

        /// <summary>
        /// 暂停事件响应，向Server服务实例发送PAUSE消息 
        /// 使其将当前使命的IsRunning参数取反
        /// </summary>
        private void btnPause_Click(object sender, EventArgs e)
        {
            // 暂停与继续按钮复用
            string strMsg = btnPause.Text.Remove(btnPause.Text.IndexOf(Convert.ToChar("(")));
            MyMission myMission = MyMission.Instance();
            //if (btnPause.Text.Equals("Pause(P)"))
            if (myMission.ParasRef.IsRunning == true && myMission.ParasRef.IsPaused == false)
            {
                btnPause.Text = "Continue(C)";
                //btnReplay.Enabled = true;

                if (MyMission.Instance().IsRomteMode == false)
                {//如果当前是local模式，则暂停模式下的用户界面上的ready按钮可用，允许用户加载策略
                    for (int i = 0;i < _listTeamStrategyControls.Count;i++)
                    {
                        _listTeamStrategyControls[i].BtnBrowse.Enabled = true;
                        _listTeamStrategyControls[i].BtnReady.Enabled = true;
                    }
                    
                }
            }
            //else if (btnPause.Text.Equals("Continue(C)"))
            else if (myMission.ParasRef.IsRunning == true && myMission.ParasRef.IsPaused == true)
            {
                btnPause.Text = "Pause(P)";
                //btnReplay.Enabled = false;

                if (MyMission.Instance().IsRomteMode == false)
                {//如果当前是local模式，则继续模式下的用户界面上的ready按钮不可用，不允许用户加载策略
                    for (int i = 0; i < _listTeamStrategyControls.Count; i++)
                    {
                        _listTeamStrategyControls[i].BtnBrowse.Enabled = false;
                        _listTeamStrategyControls[i].BtnReady.Enabled = false;
                    }
                }
            }

            StatePaused();

            FromServerUiEvents.Instance().Post(new FromServerUiMsg(FromServerUiMsg.MsgEnum.PAUSE,
                new string[] { strMsg }));
        }

        /// <summary>
        /// 模拟点击“暂停”按钮，供Sim2DSvr.ProcessUiUpdating方法调用
        /// 发生于对抗性比赛进球或交换半场时需要由程序设定暂停状态时
        /// </summary>
        /// <remarks>added by LiYoubing 20110309</remarks>
        public void ClickPauseBtn()
        {
            btnPause_Click(null, null);
        }

        /// <summary>
        /// 重启事件响应，向Server服务实例发送COMPETITION_ITEM_CHANGED消息 
        /// 使其停止当前使命运行并重新初始化当前选中的使命类型
        /// </summary>
        private void btnRestart_Click(object sender, EventArgs e)
        {
            // 水波组件初始化
            InitWaveEffect();

            DialogResult result = MessageBox.Show("Restart Game?", "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.OK)
            {
                // 恢复总时间为界面选中的值 当被重启的使命刚经历过制胜球阶段（程序中指定过总时间）时需要
                MyMission.Instance().ParasRef.TotalSeconds = Convert.ToInt32(cmbCompetitionTime.Text) * 60;
                // 发送比赛类型改变事件消息，以当前选中比赛类型名称和比赛时间分钟数作为参数  modified 20110117
                FromServerUiEvents.Instance().Post(new FromServerUiMsg(FromServerUiMsg.MsgEnum.RESTART,
                    new string[] { FromServerUiMsg.MsgEnum.RESTART.ToString() }));

                // 这句只能放在这里，不能放在StateRestart方法里，因为只有在点击Restart键时才将交换半场的全部设置还原
                // 放在StateRestart方法中的话，在很多情况都会引用 Liushu 20110513
                MyMission myMission = MyMission.Instance();
                if (myMission.ParasRef.IsExchangedHalfCourt == true
                    && myMission.ParasRef.TeamCount == 2)
                {// 如果是2支队伍的使命且交换了半场则交换回来
                    // 交换半场时调用TeamsRef.Reverse将TeamsRef[0]/TeamsRef[1]指向了Teams[1]/Teams[0]
                    myMission.TeamsRef.Reverse();

                    // 交换了半场的标志复位
                    myMission.ParasRef.IsExchangedHalfCourt = false;
                }

                StateRestart();

                //added by liushu 20110222
                if (MyMission.Instance().IsRomteMode == false)
                {//如果是local模式，点击Restart之后清空已加载了的策略
                    UnloadStrategy();
                }
                //更改了事件响应的顺序,应该先post消息,然后改变按钮状态,然后卸载策略.by chenwei
            }
        }

        #region 使命执行过程中局部画面回放功能
        /// <summary>
        /// 画面回放使用，当前缓存画面在缓存数组中的索引
        /// </summary>
        private int _curCachingIndex = 0;

        /// <summary>
        /// 画面回放使用，当前回放画面在缓存数组中的索引
        /// </summary>
        private int _curReplayingIndex = 0;

        /// <summary>
        /// 画面回放使用，缓存数组大小即最大缓存画面数量
        /// </summary>
        private int _maxCachedCycles = 50;

        /// <summary>
        /// 画面回放使用，缓存回放画面的数组
        /// </summary>
        private Bitmap[] _bmpCachedImages = null;

        private bool _isReplaying = false;

        /// <summary>
        /// 回放事件响应，向Server服务实例发送REPLAY消息 
        /// </summary>
        private void btnReplay_Click(object sender, EventArgs e)
        {
            //if (btnReplay.Text == "Replay(Y)")
            if (_isReplaying == false)
            {// 不在回放 点击的是回放按钮
                _curReplayingIndex = _curCachingIndex;
                //_isReplaying = true;
                btnReplay.Text = "End(E)";
                //btnPause.Enabled = false;
            }
            else
            {// 正在回放 点击的是结束按钮
                //_isReplaying = false;
                btnReplay.Text = "Replay(Y)";
                //btnPause.Enabled = true;
            }

            StateReplay();

            FromServerUiEvents.Instance().Post(new FromServerUiMsg(FromServerUiMsg.MsgEnum.REPLAY,
                new string[] { FromServerUiMsg.MsgEnum.REPLAY.ToString() }));
        }

        /// <summary>
        /// 使命运行过程中局部画面回放 
        /// 由响应REPLAY消息时激活的TimeoutPort Receiver按照仿真周期间隔调用
        /// </summary>
        public void Replay()
        {
            if (_curReplayingIndex == _maxCachedCycles)
            {
                _curReplayingIndex = 0;
            }
            picMatch.Image = _bmpCachedImages[_curReplayingIndex];
            _curReplayingIndex++;

            if (_isReplaying == true)
            {
                FromServerUiEvents.Instance().Post(new FromServerUiMsg(FromServerUiMsg.MsgEnum.REPLAY,
                    new string[] { FromServerUiMsg.MsgEnum.REPLAY.ToString() }));
            }
        }
        #endregion

        /// <summary>
        /// 比赛Restart或者结束之后清空内存中已经加载过的策略。 added by liushu 20110307
        /// </summary>
        public void UnloadStrategy()
        {          
            for (int i = 0; i < _listTeamStrategyControls.Count; i++)
            {
                if (_listTeamStrategyControls[i]._appDomainForStrategy != null)
                {// 加上null检查确保程序不会异常 LiYoubing 20110511
                    AppDomain.Unload(_listTeamStrategyControls[i]._appDomainForStrategy);
                }
                _listTeamStrategyControls[i]._appDomainForStrategy = null;
                _listTeamStrategyControls[i].StrategyInterface = null;
            }
            _listTeamStrategyControls.Clear();
        }

        #region 主界面开始/暂停/重启/回放四个按钮间及其和其他控件间的约束关系处理
        /// <summary>
        /// 设置平台开始后（非暂停状态）相关控件的状态  weiqingdan20101211
        /// </summary>
        private void StateStart()
        {
            // Referee选项卡
            cmbCompetitionItem.Enabled = false;
            cmbCompetitionTime.Enabled = false;

            if (MyMission.Instance().IsRomteMode == false)
            {// 如果当前是local模式，则比赛开始之后，加载策略按钮不可用
                for (int i = 0; i < _listTeamStrategyControls.Count; i++)
                {
                    _listTeamStrategyControls[i].BtnBrowse.Enabled = false;
                }
            }

            // 设置各按钮Enabled状态
            SetBtnEnabledState(false, true, true, false, true, true, true, false, false, false);

            // 点击开始按钮后仿真机器鱼和仿真水球参数显示设置控件设置只读状态
            SetFishAndBallCtrlsReadOnly(true);

            // Field选项卡场地尺寸设置控件禁用 LiYoubing 20110726
            cmbFieldLength.Enabled = false;
        }

        /// <summary>
        /// 设置平台 暂停/继续 状态切换时相关控件的状态    weiqingdan20101211
        /// </summary>
        private void StatePaused()
        {
            if (MyMission.Instance().ParasRef.IsPaused == true)
            {// 当前是暂停状态 点击的继续按钮 离开暂停状态
                // 设置各按钮Enabled状态
                SetBtnEnabledState(false, true, true, false, true, true, true, false, false, false);

                // 点击继续按钮后仿真机器鱼和仿真水球参数显示设置控件设置只读状态
                SetFishAndBallCtrlsReadOnly(true);
            }
            else
            {// 当前尚未暂停 点击的暂停按钮 进入暂停状态
                // 设置各按钮Enabled状态
                SetBtnEnabledState(false, true, true, true, true, true, true, false, true, true);

                // 点击暂停按钮后仿真机器鱼和仿真水球参数显示设置控件解除只读状态
                SetFishAndBallCtrlsReadOnly(false);
            }
        }

        /// <summary>
        /// 设置平台 回放/停止回放 状态切换时相关控件的状态    weiqingdan20101211
        /// </summary>
        private void StateReplay()
        {
            // 是否正在回放标志量取反
            _isReplaying = !_isReplaying;
            if (_isReplaying == true)
            {// 点击的回放按钮 进入回放状态
                // 设置各按钮Enabled状态
                SetBtnEnabledState(false, false, true, true, true, true, true, false, false, false);

                // 正在回放仿真机器鱼和仿真水球参数显示设置控件设置只读状态
                SetFishAndBallCtrlsReadOnly(true);
            }
            else
            {// 点击的结束按钮 退出回放状态 回到暂停状态
                // 设置各按钮Enabled状态
                SetBtnEnabledState(false, true, true, true, true, true, true, false, true, true);

                // 结束回放一定进入暂停状态仿真机器鱼和仿真水球参数显示设置控件解除只读状态
                SetFishAndBallCtrlsReadOnly(false);
            }
        }

        /// <summary>
        /// 设置平台重置时相关控件的状态    weiqingdan20101211
        /// </summary>
        private void StateRestart()
        {
            // Referee选项卡
            cmbCompetitionItem.Enabled = true;
            cmbCompetitionTime.Enabled = true;

            // 设置各按钮Enabled状态
            SetBtnEnabledState(false, false, false, false, true, true, true, false, true, true);

            // 点击重启按钮后仿真机器鱼和仿真水球参数显示设置控件解除只读状态
            SetFishAndBallCtrlsReadOnly(false);

            // Field选项卡场地尺寸设置控件启用 LiYoubing 20110726
            cmbFieldLength.Enabled = true;

            btnTestVelocity.Text = "Start";
            _isTestMode = false;
        }

        /// <summary>
        /// 设置全部仿真机器鱼和仿真水球的参数设置和显示控件是否只读
        /// </summary>
        /// <param name="bReadOnly">是否只读true是false否</param>
        void SetFishAndBallCtrlsReadOnly(bool bReadOnly)
        {
            for (int i = 0; i < MyMission.Instance().ParasRef.TeamCount; i++)
            {// 设置每条仿真机器鱼的参数设置和显示控件是否只读
                //_listFishSettingControls[i].CmbPlayers.Enabled = true;
                _listFishSettingControls[i].TxtPositionMmX.ReadOnly = bReadOnly;
                _listFishSettingControls[i].TxtPositionMmZ.ReadOnly = bReadOnly;
                _listFishSettingControls[i].TxtDirectionDeg.ReadOnly = bReadOnly;
                _listFishSettingControls[i].BtnConfirm.Enabled = !bReadOnly;
            }

            for (int i = 0; i < MyMission.Instance().EnvRef.Balls.Count; i++)
            {// 设置每个仿真水球的参数设置和显示控件是否只读
                _listBallSettingControls[i].TxtPositionMmX.ReadOnly = bReadOnly;
                _listBallSettingControls[i].TxtPositionMmZ.ReadOnly = bReadOnly;
                _listBallSettingControls[i].TxtRadiusMm.ReadOnly = bReadOnly;
            }
        }

        /// <summary>
        /// 设置主控界面开始/暂停(继续)/重启/回放(结束)/录像/绘制轨迹/显示信息/
        /// 测速/Fish面板DefaultSetting/Field面板DefaultSetting等几个按钮的Enabled属性
        /// </summary>
        private void SetBtnEnabledState(bool bStart, bool bPause, bool bRestart, bool bReplay,
            bool bRecordVideo, bool bDrawTrajectory, bool bDisplayFishInfo,
            bool bTestVelocity, bool bFishDefaultSetting, bool bFieldDefaultSetting)
        {
            btnStart.Enabled = bStart;
            btnPause.Enabled = bPause;
            btnRestart.Enabled = bRestart;
            btnReplay.Enabled = bReplay;
            btnRecordVideo.Enabled = bRecordVideo;
            btnDrawTrajectory.Enabled = bDrawTrajectory;
            btnDisplayFishInfo.Enabled = bDisplayFishInfo;
            btnTestVelocity.Enabled = bTestVelocity;
            btnFishDefaultSetting.Enabled = bFishDefaultSetting;
            btnFieldDefaultSetting.Enabled = bFieldDefaultSetting;
        }
        #endregion
        #endregion

        /// <summary>
        /// 改变比赛类型事件响应，重新初始化选中的使命
        /// </summary>
        private void cmbCompetitionItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 水波组件初始化
            InitWaveEffect();

            // 发送COMPETITION_ITEM_CHANGED消息，以当前选中比赛类型名称和比赛时间分钟数作为参数
            // 使Server服务实例调用InitMission方法重新初始化当前选中的使命类型
            FromServerUiEvents.Instance().Post(new FromServerUiMsg(FromServerUiMsg.MsgEnum.COMPETITION_ITEM_CHANGED,
                new string[] { ((ComboBox)sender).SelectedItem.ToString(), cmbCompetitionTime.SelectedItem.ToString() }));
        }

        /// <summary>
        /// 改变比赛时间事件响应，根据界面选择更新当前仿真使命的时间相关参数
        /// </summary>
        private void cmbCompetitionTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            int minutes = Int32.Parse(((ComboBox)sender).SelectedItem.ToString());
            if (MyMission.Instance().ParasRef != null)
            {
                // 更新完成使命所需总秒数
                MyMission.Instance().ParasRef.TotalSeconds = minutes * 60;

                // 重新初始化剩余周期数
                MyMission.Instance().ParasRef.RemainingCycles = minutes * 60 * 1000 
                    / MyMission.Instance().ParasRef.MsPerCycle;

                //picMatch.Image = MyMission.Instance().IMissionRef.Draw();
                SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
            }
        }

        #region 主界面Referee面板Strategy Setting处理 Caiqiong
        int _iConnetedClientsCount;
        /// <summary>
        /// 当前已经连接上的客户端数量，该值为0时，才允许由Remote模式往Local模式切换
        /// </summary>
        public int ConnetedClientsCount
        {
            set { _iConnetedClientsCount = value; }
        }

        /// <summary>
        /// Referee面板Strategy区动态生成的自定义组合控件列表
        /// </summary>
        List<TeamStrategyComboControls> _listTeamStrategyControls = new List<TeamStrategyComboControls>();
        public List<TeamStrategyComboControls> TeamStrategyControls
        {
            get { return _listTeamStrategyControls; } 
        }

        /// <summary>
        /// 是否有队伍已经准备好并按下服务端Ready按钮，是则不允许从Local往Remote模式切换
        /// </summary>
        bool _isThereTeamReady = false;

        /// <summary>
        /// 为当前选中的使命类型初始化相应的Strategy区动态控件
        /// </summary>
        public void InitTeamStrategyControls()
        {
            pnlStrategy.Controls.Clear();       // 先清空外层Panel容器
            _listTeamStrategyControls.Clear();  // 再清空组合控件列表  
            for (int i = 0; i < MyMission.Instance().ParasRef.TeamCount; i++)
            {
                InitTeamStrategyControls(i);
            }
        }

        /// <summary>
        /// 为编号为teamId的队伍创建自定义组合控件对象
        /// </summary>
        /// <param name="teamId">队伍的编号（0,1...）</param>
        private void InitTeamStrategyControls(int teamId)
        {
            // 创建第teamId支队伍的设置组合控件
            _listTeamStrategyControls.Add(new TeamStrategyComboControls(teamId));

            // 把刚创建的组合控件中的GroupBox容器加入整体Panel容器
            pnlStrategy.Controls.Add(_listTeamStrategyControls[teamId].GrpContainer);

            // 设置刚加入Panel容器的GroupBox容器的位置
            _listTeamStrategyControls[teamId].GrpContainer.Location = new Point(0, teamId * 50);

            // 为当前组合控件中的Browse按钮添加Click事件，处理重绘动作以更新画面显示
            // 详细的加载策略操作在TeamStrategyComboControls类的BtnBrowse_Click方法中完成
            _listTeamStrategyControls[teamId].BtnBrowse.Click += new EventHandler(BtnBrowse_Click);

            // 为当前组合控件中的Ready按钮添加Click事件，处理所有Ready按钮均按下后的操作
            _listTeamStrategyControls[teamId].BtnReady.Click += new EventHandler(BtnReady_Click);
        }

        ///added by caiqiong 20101204
        /// <summary>
        /// 选择本地添加策略模式时候的界面切换
        /// </summary>
        private void rdoLocal_Click(object sender, EventArgs e)
        {
            MyMission mission = MyMission.Instance();
            if ((mission.IsRomteMode == true) && (_iConnetedClientsCount == 0))
            {// 当前运行于Remote模式且无客户端连接上，可以切换为Local模式
                mission.IsRomteMode = false;    // 切换为Local模式
                _isThereTeamReady = false;
                for (int i = 0; i < _listTeamStrategyControls.Count; i++)
                {
                    _listTeamStrategyControls[i].BtnBrowse.Enabled = true;
                    _listTeamStrategyControls[i].BtnReady.Enabled = false;
                }
            }
            btnTestVelocity.Enabled = true;
            if (mission.IsRomteMode == false) return;
            rdoLocal.Checked = false;
            rdoRemote.Checked = true;
        }

        private void rdoRemote_Click(object sender, EventArgs e)
        {
            MyMission mission = MyMission.Instance();
            if ((mission.IsRomteMode == false) && (_isThereTeamReady == false))
            {// 当前运行于Local模式且无队伍Ready，可以切换为Remote模式
                mission.IsRomteMode = true; // 切换为Remote模式
                for (int i = 0; i < _listTeamStrategyControls.Count; i++)
                {
                    _listTeamStrategyControls[i].BtnBrowse.Enabled = false;
                    _listTeamStrategyControls[i].BtnReady.Enabled = true;
                    _listTeamStrategyControls[i].TxtStrategyFileName.Text = "";
                    _listTeamStrategyControls[i].ClearStrategy();
                    //_listTeamStrategyControls[i].ClearStrategy();
                }
            }
            btnTestVelocity.Enabled = false;
            if (mission.IsRomteMode == true) return;
            rdoLocal.Checked = true;
            rdoRemote.Checked = false;
        }
        
        ///add by caiqiong 20101203
        /// <summary>
        /// 仿真使命参与队伍Ready按钮响应，处理与其他按钮的约束
        /// </summary>
        public void BtnReady_Click(object sender, EventArgs e)
        {
            // 从类似"Team1"的Browse按钮Name字符串中取出数字1
            //int teamId = Convert.ToInt32(((Button)sender).Name.Substring(4));

            if (MyMission.Instance().IsRomteMode == true) return;
            _isThereTeamReady = true;

            //bool flag = false;
            //for (int i = 0; i < _listTeamStrategyControls.Count; i++)
            //{// 只要还有Ready按钮尚未按下则flag为true
            //    flag = flag || _listTeamStrategyControls[i].BtnReady.Enabled;
            //}
            //if (flag == false)
            //{// 所有Ready按钮都是不可用的状态，则判断是否所有参赛队伍都已加载完策略  added by liushu 20110307
            //    int i;
            //    for (i = 0; i < _listTeamStrategyControls.Count; i++)
            //    {
            //        if (_listTeamStrategyControls[i]._appDomainForStrategy == null)
            //        {
            //            break;
            //        }
            //    }

            //    if (i == _listTeamStrategyControls.Count)       //如果所有的参赛队伍都已经加载完策略则Start按钮可用
            //    btnStart.Enabled = true;
            //}

            if (btnPause.Enabled == false)  //local模式时，由于比赛过程中点击pause是可以重新加载策略重新点击ready的，但是Start仍然不可用。
            {//若果pause按钮是不可用的，说明没有进行比赛，则判断Start按钮是否可用
                bool flag = false;
                for (int i = 0; i < _listTeamStrategyControls.Count; i++)
                {// 只要还有Ready按钮尚未按下则flag为true
                    flag = flag || _listTeamStrategyControls[i].BtnReady.Enabled;
                }
                if (flag == false)
                {// 所有Ready按钮都是不可用的状态，则判断是否所有参赛队伍都已加载完策略  added by liushu 20110307
                    int i;
                    for (i = 0; i < _listTeamStrategyControls.Count; i++)
                    {
                        if (_listTeamStrategyControls[i]._appDomainForStrategy == null)
                        {
                            break;
                        }
                    }

                    if (i == _listTeamStrategyControls.Count)       //如果所有的参赛队伍都已经加载完策略则Start按钮可用
                        btnStart.Enabled = true;
                }
            }
            else
            {//如果是比赛状态，Start按钮不可用
                btnStart.Enabled = false;
            }

            btnRestart.Enabled = true;
        }

        /// <summary>
        /// 仿真使命参与队伍策略dll文件Browse按钮响应，处理与其他按钮的约束并重绘更新显示
        /// 浏览策略dll文件并将其加载到IStrategy对象的具体操作在TeamStrategyComboControls类的BtnBrowse_Click方法中完成
        /// </summary>
        public void BtnBrowse_Click(object sender, EventArgs e)
        {
            //// 从类似"Team1"的Browse按钮Name字符串中取出数字1
            //int teamId = Convert.ToInt32(((Button)sender).Name.Substring(4));

            //bool flag = false;
            //for (int i = 0; i < _listTeamStrategyControls.Count; i++)
            //{// 只要还有Ready按钮尚未按下则flag为true
            //    flag = flag || _listTeamStrategyControls[i].BtnReady.Enabled;
            //}
            //btnStart.Enabled = !flag;

            //picMatch.Image = MyMission.Instance().IMissionRef.Draw();
            SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
        }
        #endregion

        #region 主界面Fish面板Fish Setting & Ball Setting处理 Liufei
        /// <summary>
        /// Fish面板FishSetting区动态生成的自定义组合控件列表
        /// </summary>
        private List<FishSettingComboControls> _listFishSettingControls = new List<FishSettingComboControls>();

        /// <summary>
        /// Fish面板BallSetting区动态生成的自定义组合控件列表
        /// </summary>
        private List<BallSettingComboControls> _listBallSettingControls = new List<BallSettingComboControls>();

        /// <summary>
        /// 为当前选中的使命类型初始化相应的FishSetting区和BallSetting区动态控件
        /// </summary>
        public void InitFishAndBallSettingControls()
        {
            pnlFishSetting.Controls.Clear();    // 先清空外层Panel容器
            _listFishSettingControls.Clear();   // 再清空组合控件列表
            for (int i = 0; i < MyMission.Instance().ParasRef.TeamCount; i++)
            {// 为每支队伍创建组合控件
                InitFishSettingControls(i);
            }

            pnlBallSetting.Controls.Clear();    // 先清空外层Panel容器
            _listBallSettingControls.Clear();   // 再清空组合控件列表
            for (int i = 0; i < MyMission.Instance().EnvRef.Balls.Count; i++)
            {// 为每个仿真水球创建组合控件
                InitBallSettingControls(i);
            }
        }

        /// <summary>
        /// 为编号为teamId的队伍创建自定义组合控件对象
        /// </summary>
        /// <param name="teamId">队伍的编号（0,1...）</param>
        private void InitFishSettingControls(int teamId)
        {
            // 创建第teamId支队伍的设置组合控件，传入的参数用于初始化队员选择ComboBox的Items
            _listFishSettingControls.Add(new FishSettingComboControls(teamId, 
                MyMission.Instance().TeamsRef[teamId].Fishes.Count));

            // 把刚创建的组合控件中的GroupBox容器加入整体Panel容器
            pnlFishSetting.Controls.Add(_listFishSettingControls[teamId].GrpContainer);

            // 设置刚加入Panel容器的GroupBox容器的位置
            _listFishSettingControls[teamId].GrpContainer.Location = new Point(teamId * 100, 5);
            
            // 为当前组合控件中的Confirm按钮添加Click事件，处理重绘动作以更新画面显示
            // 将界面数据设置到仿真机器鱼对象的操作在FishSettingComboControls类中的Confirm按钮Click事件处理方法中
            _listFishSettingControls[teamId].BtnConfirm.Click += new EventHandler(BtnConfirm_Click);
        }

        /// <summary>
        /// 仿真机器鱼色标/位姿等设置Confirm按钮响应，处理重绘操作以更新显示
        /// 将界面设置的参数往仿真机器鱼对象存储的具体操作在FishSettingComboControls类的BtnConfirm_Click方法中完成
        /// </summary>
        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            //picMatch.Image = MyMission.Instance().IMissionRef.Draw();
            SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
        }

        /// <summary>
        /// 为编号为teamId的队伍创建自定义组合控件对象
        /// </summary>
        /// <param name="teamId">队伍的编号（0,1...）</param>
        private void InitBallSettingControls(int ballId)
        {
            // 创建第ballId号仿真水球的设置组合控件
            _listBallSettingControls.Add(new BallSettingComboControls(ballId, MyMission.Instance().EnvRef.Balls.Count));

            // 把刚创建的组合控件中的GroupBox容器加入整体Panel容器
            pnlBallSetting.Controls.Add(_listBallSettingControls[ballId].GrpContainer);

            // 设置刚加入Panel容器的GroupBox容器的位置
            _listBallSettingControls[ballId].GrpContainer.Location = new Point(0, ballId * 55);

            // 为当前组合控件中的TxtPositionMmX/TxtPositionMmZ输入框添加Validated事件，处理重绘动作以更新画面显示
            // 将界面数据设置到仿真水球对象的操作在BallSettingComboControls类中的相关方法中完成
            _listBallSettingControls[ballId].TxtPositionMmX.Validated += new EventHandler(TxtPositionMm_Validated);
            _listBallSettingControls[ballId].TxtPositionMmZ.Validated += new EventHandler(TxtPositionMm_Validated);
        }

        /// <summary>
        /// 仿真水球坐标输入框Validated事件响应，处理重绘动作以更新画面显示
        /// 将界面数据设置到仿真水球对象的操作在BallSettingComboControls类中的相关方法中完成
        /// </summary>
        void TxtPositionMm_Validated(object sender, EventArgs e)
        {
            //picMatch.Image = MyMission.Instance().IMissionRef.Draw();
            SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
        }

        /// <summary>
        /// 处理Fish面板FishSetting区和BallSetting区当前选中的仿真机器鱼和仿真水球信息更新任务
        /// </summary>
        public void ProcessFishAndBallInfoUpdating()
        {
            if (tabServerControlBoard.SelectedIndex == 1)
            {// 当主界面TabControl选中的是Fish面板时才需要处理
                for (int i = 0; i < _listFishSettingControls.Count; i++)
                {
                    _listFishSettingControls[i].CmbPlayers_SelectedIndexChanged(
                        _listFishSettingControls[i].CmbPlayers, new EventArgs());
                }
                for (int i = 0; i < _listBallSettingControls.Count; i++)
                {
                    _listBallSettingControls[i].SetBallInfoToUi();
                }
            }
        }

        /// <summary>
        /// 处理Field面板ObstacleSetting区信息更新任务
        /// </summary>
        private void ProcessFieldInfoUpdating()
        {
            if (tabServerControlBoard.SelectedIndex == 2)
            {// 当主界面TabControl选中的是Field面板时才需要处理
                cmbObastacleType.SelectedIndex = 1; // 障碍物类型设为方形
                lstObstacle.Items.Clear();         // 清除列表
                MyMission myMission = MyMission.Instance();
                for (int i = 0; i < myMission.EnvRef.ObstaclesRect.Count; i++)
                {// 将当前仿真使命的全部方形障碍物名称显示在列表中
                    lstObstacle.Items.Add(myMission.EnvRef.ObstaclesRect[i].Name);
                }
                if (myMission.EnvRef.ObstaclesRect.Count > 0)
                {// 当前仿真使命方形障碍物数量不为0则选中第0个
                    lstObstacle.SelectedIndex = 0;
                }
                cmbFieldLength.Text = Field.Instance().FieldLengthXMm.ToString();
                if (cmbObstacleDirection.Text == "")
                {// 障碍物方向若为空则置为默认值0
                    cmbObstacleDirection.Text = "0";
                }
            }
        }

        private void tabServerControlBoard_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabServerControlBoard.SelectedIndex)
            {
                case 1: // Fish选项卡
                    // 处理Fish面板FishSetting区当前选中的仿真机器鱼和仿真水球信息更新任务
                    ProcessFishAndBallInfoUpdating();
                    break;
                case 2: // Field选项卡
                    ProcessFieldInfoUpdating();
                    break;
            }
        }

        private void btnFishDefaultSetting_Click(object sender, EventArgs e)
        {// 重新初始化当前选中的使命即可恢复仿真场地上仿真机器鱼和仿真水球默认设置
            // LiYoubing 20110617
            IMission iMission = MyMission.Instance().IMissionRef;
            // 仿真机器鱼各项设置主要是位姿设置恢复默认值
            iMission.ResetTeamsAndFishes();
            // 仿真机器鱼鱼体颜色和编号颜色及仿真水球颜色恢复默认值
            iMission.ResetColorFishAndId();
            // 仿真水球恢复默认位置
            iMission.ResetBalls();
            // 仿真水球填充和边框颜色恢复默认值
            iMission.ResetColorBall();
            // 重绘仿真场地上的全部动态对象
            SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
            // 模拟切换选项卡以更新Fish面板显示
            tabServerControlBoard_SelectedIndexChanged(null, null);
            //// 发送COMPETITION_ITEM_CHANGED消息，以当前选中比赛类型名称和比赛时间分钟数作为参数
            //// 使Server服务实例调用InitMission方法重新初始化当前选中的使命类型
            //FromServerUiEvents.Instance().Post(new FromServerUiMsg(FromServerUiMsg.MsgEnum.COMPETITION_ITEM_CHANGED,
            //    new string[] { ((ComboBox)sender).SelectedItem.ToString(), cmbCompetitionTime.SelectedItem.ToString() }));
        }
        #endregion

        #region 主界面Fish面板Velocity Test处理
        private bool _isTestMode = false;
        /// <summary>
        /// 指示当前处于测速模式即已经按下Velocity Test下的Start按钮
        /// </summary>
        public bool IsTestMode
        {
            get { return _isTestMode; }
        }

        public Decision GetTestDecision()
        {
            Decision decision = new Decision();
            decision.VCode = trbVelocityCode.Value;
            decision.TCode = trbSwervingCode.Value;
            return decision;
        }

        private void trbVelocityCode_Scroll(object sender, EventArgs e)
        {
            toolTip.SetToolTip(trbVelocityCode, trbVelocityCode.Value.ToString());
        }

        private void trbSwervingCode_Scroll(object sender, EventArgs e)
        {
            toolTip.SetToolTip(trbSwervingCode, trbSwervingCode.Value.ToString());
        }

        private void btnTestVelocity_Click(object sender, EventArgs e)
        {
            if (btnTestVelocity.Text.Equals("Start"))
            {
                btnTestVelocity.Text = "Stop";
                StateStart();
                btnTestVelocity.Enabled = true;
                _isTestMode = true;

                FromServerUiEvents.Instance().Post(new FromServerUiMsg(FromServerUiMsg.MsgEnum.START,
                    new string[] { FromServerUiMsg.MsgEnum.START.ToString() }));
            }
            else
            {
                btnTestVelocity.Text = "Start";
                btnRestart_Click(btnRestart, new EventArgs());
            }
        }
        #endregion

        #region 主界面Field面板Obstacle Setting处理 Added By Caiqiong
        /// <summary>
        /// 添加障碍物 add by caiqiong 2010-11-26
        /// </summary>
        private void btnObstacleAdd_Click(object sender, EventArgs e)
        {
            MyMission myMission = MyMission.Instance();
            xna.Vector3 positionMm = new xna.Vector3(Convert.ToSingle(txtObstaclePosX.Text),
                0, Convert.ToSingle(txtObstaclePosZ.Text));
            int count = 0;
            if (cmbObastacleType.Text.Equals("CIRCLE"))
            {// 添加圆形障碍物
                count = myMission.EnvRef.ObstaclesRound.Count;
                // 往当前仿真使命的圆形障碍物列表添加元素
                myMission.EnvRef.ObstaclesRound.Add(new RoundedObstacle("CircleObs" + (count + 1), 
                    positionMm, btnObstacleColorBorder.BackColor, btnObstacleColorFilled.BackColor,
                    (int)Convert.ToSingle(txtObstacleLength.Text)));
                // 检查圆形障碍物的尺寸和位置参数合法性 若超出场地则调整到场地内
                RoundedObstacle obj =  myMission.EnvRef.ObstaclesRound[count];
                UrwpgSimHelper.ParametersCheckingObstacle(ref obj);
                // 往界面列表控件添加元素
                lstObstacle.Items.Add(myMission.EnvRef.ObstaclesRound[count].Name);
            }
            else if (cmbObastacleType.Text.Equals("RECTANGLE"))
            {// 添加方形障碍物
                count = myMission.EnvRef.ObstaclesRect.Count;
                // 往当前仿真使命的方形障碍物列表添加元素
                myMission.EnvRef.ObstaclesRect.Add(new RectangularObstacle("RectObs" + (count + 1),
                    positionMm, btnObstacleColorBorder.BackColor, btnObstacleColorFilled.BackColor,
                    (int)Convert.ToSingle(txtObstacleLength.Text), (int)Convert.ToSingle(txtObstacleWidth.Text), 
                    Convert.ToSingle(cmbObstacleDirection.Text)));
                // 检查方形障碍物的尺寸和位置参数合法性 若超出场地则调整到场地内
                RectangularObstacle obj = myMission.EnvRef.ObstaclesRect[count];
                UrwpgSimHelper.ParametersCheckingObstacle(ref obj);
                // 往界面列表控件添加元素
                lstObstacle.Items.Add(myMission.EnvRef.ObstaclesRect[count].Name);
            }
            // 设置选中对象执行选中索引改变事件响应函数刷新选中对象参数显示
            lstObstacle.SelectedIndex = count;

            //picMatch.Image = MyMission.Instance().IMissionRef.Draw();
            SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
        }

        /// <summary>
        /// 删除障碍物 add by caiqiong 2010-11-28
        /// </summary>
        private void btnObstacleDelete_Click(object sender, EventArgs e)
        {
            if (lstObstacle.SelectedIndex == -1) return;
            MyMission myMission = MyMission.Instance();
            if (cmbObastacleType.Text.Equals("CIRCLE"))
            {// 删除圆形障碍物
                // 从当前仿真使命的圆形障碍物列表删除元素
                myMission.EnvRef.ObstaclesRound.RemoveAt(lstObstacle.SelectedIndex);
                // 从界面列表控件删除元素
                lstObstacle.Items.RemoveAt(lstObstacle.SelectedIndex);
            }
            else if (cmbObastacleType.Text.Equals("RECTANGLE"))
            {// 删除方形障碍物
                int count = myMission.EnvRef.ObstaclesRect.Count;
                // 从当前仿真使命的方形障碍物列表删除元素
                myMission.EnvRef.ObstaclesRect.RemoveAt(lstObstacle.SelectedIndex);
                // 从界面列表控件删除元素
                lstObstacle.Items.RemoveAt(lstObstacle.SelectedIndex);
            }
            // 设置选中对象执行选中索引改变事件响应函数刷新选中对象参数显示
            lstObstacle.SelectedIndex = (lstObstacle.Items.Count > 0) ? 0 : -1;
            #region
            //xna.Vector3 positionMm = new xna.Vector3();
            //positionMm.X = Convert.ToInt16(TB_ObstacleOrChannel_X.Text);
            //positionMm.Z = Convert.ToInt16(TB_ObstacleOrChannel_Y.Text);

            //if (RB_Obstacle.Checked == true)
            //{
            //    if (Convert.ToString(cmbObastacleType.SelectedItem) == "CIRCLE")
            //    {
            //        if (lstObstacle.SelectedIndex != -1)
            //        {
            //            int i = Convert.ToInt16(Convert.ToString(lstObstacle.SelectedItem).Trim("CIRCLE".ToCharArray()));
            //            int count = MyMission.Instance().EnvRef.ObstaclesRound.Count;

            //            for (int j = i - 1; j < count - 1; j++)
            //            {
            //                MyMission.Instance().EnvRef.ObstaclesRound[j] = MyMission.Instance().EnvRef.ObstaclesRound[j + 1];
            //            }

            //            lstObstacle.Items.Clear();
            //            for (int j = 1; j < MyMission.Instance().EnvRef.ObstaclesRound.Count; j++)
            //            {
            //                lstObstacle.Items.Add("CIRCLE" + Convert.ToString(j));
            //            }
            //            for (int j = 1; j < MyMission.Instance().EnvRef.ObstaclesRect.Count + 1; j++)
            //            {
            //                lstObstacle.Items.Add("RECTANGLE" + Convert.ToString(j));
            //            }

            //            MyMission.Instance().EnvRef.ObstaclesRound.Remove(MyMission.Instance().EnvRef.ObstaclesRound[count - 1]);
            //        }
            //        else
            //        {
            //            MessageBox.Show("No Obstaclecircle is choosed");
            //        }
            //    }
            //    else if (Convert.ToString(cmbObastacleType.SelectedItem) == "RECTANGLE")
            //    {
            //        if (lstObstacle.SelectedIndex != -1)
            //        {
            //            int i = Convert.ToInt16(Convert.ToString(lstObstacle.SelectedItem).Trim("RECTANGLE".ToCharArray()));
            //            int count = MyMission.Instance().EnvRef.ObstaclesRect.Count;

            //            for (int j = i - 1; j < count - 1; j++)
            //            {
            //                MyMission.Instance().EnvRef.ObstaclesRect[j] = MyMission.Instance().EnvRef.ObstaclesRect[j + 1];
            //            }
            //            //show
            //            lstObstacle.Items.Clear();
            //            for (int j = 1; j < MyMission.Instance().EnvRef.ObstaclesRound.Count + 1; j++)
            //            {
            //                lstObstacle.Items.Add("CIRCLE" + Convert.ToString(j));
            //            }
            //            for (int j = 1; j < MyMission.Instance().EnvRef.ObstaclesRect.Count; j++)
            //            {
            //                lstObstacle.Items.Add("RECTANGLE" + Convert.ToString(j));
            //            }

            //            MyMission.Instance().EnvRef.ObstaclesRect.Remove(MyMission.Instance().EnvRef.ObstaclesRect[count - 1]);
            //        }
            //        else
            //        {
            //            MessageBox.Show("No ObstacleRectangle is choosed");
            //        }
            //    }

            //    //picMatch.Image = MyMission.Instance().IMissionRef.Draw();
            //    SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
            //}
            //else if (RB_Channel.Checked == true)
            //{
            //    int i = Convert.ToInt16(Convert.ToString(List_Channel.SelectedItem).Trim("Channel".ToCharArray()));
            //    int count = MyMission.Instance().EnvRef.Channels.Count;
            //    for (int j = i - 1; j < count - 1; j++)
            //    {
            //        MyMission.Instance().EnvRef.Channels[j] = MyMission.Instance().EnvRef.Channels[j + 1];
            //    }
            //    List_Channel.Items.Clear();
            //    for (int j = 1; j < MyMission.Instance().EnvRef.Channels.Count; j++)
            //    {
            //        List_Channel.Items.Add("Channel" + Convert.ToString(j));
            //    }
            //    MyMission.Instance().EnvRef.Channels.Remove(MyMission.Instance().EnvRef.Channels[count - 1]);
            //}
            #endregion
            //picMatch.Image = MyMission.Instance().IMissionRef.Draw();
            SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
        }

        /// <summary>
        /// 修改障碍物 add by caiqiong 2010-11-28
        /// </summary>
        private void btnObstacleModify_Click(object sender, EventArgs e)
        {
            if (lstObstacle.SelectedIndex == -1) return;
            MyMission myMission = MyMission.Instance();
            xna.Vector3 positionMm = new xna.Vector3(Convert.ToSingle(txtObstaclePosX.Text),
                0, Convert.ToSingle(txtObstaclePosZ.Text));
            int index = lstObstacle.SelectedIndex;
            if (cmbObastacleType.Text.Equals("CIRCLE"))
            {// 修改当前仿真使命的圆形障碍物列表中对应被选中元素的参数
                myMission.EnvRef.ObstaclesRound[index].PositionMm = positionMm;
                myMission.EnvRef.ObstaclesRound[index].RadiusMm = (int)Convert.ToSingle(txtObstacleLength.Text);
                myMission.EnvRef.ObstaclesRound[index].ColorBorder = btnObstacleColorBorder.BackColor;
                myMission.EnvRef.ObstaclesRound[index].ColorFilled = btnObstacleColorFilled.BackColor;
                // 检查圆形障碍物的尺寸和位置参数合法性 若超出场地则调整到场地内
                RoundedObstacle obj = myMission.EnvRef.ObstaclesRound[index];
                UrwpgSimHelper.ParametersCheckingObstacle(ref obj);
            }
            else if (cmbObastacleType.Text.Equals("RECTANGLE"))
            {// 修改当前仿真使命的方形障碍物列表中对应被选中元素的参数
                myMission.EnvRef.ObstaclesRect[index].PositionMm = positionMm;
                myMission.EnvRef.ObstaclesRect[index].LengthMm = (int)Convert.ToSingle(txtObstacleLength.Text);
                myMission.EnvRef.ObstaclesRect[index].WidthMm = (int)Convert.ToSingle(txtObstacleWidth.Text);
                myMission.EnvRef.ObstaclesRect[index].DirectionRad = xna.MathHelper.ToRadians(Convert.ToSingle(cmbObstacleDirection.Text));
                myMission.EnvRef.ObstaclesRect[index].ColorBorder = btnObstacleColorBorder.BackColor;
                myMission.EnvRef.ObstaclesRect[index].ColorFilled = btnObstacleColorFilled.BackColor;
                // 检查方形障碍物的尺寸和位置参数合法性 若超出场地则调整到场地内
                RectangularObstacle obj = myMission.EnvRef.ObstaclesRect[index];
                UrwpgSimHelper.ParametersCheckingObstacle(ref obj);
            }
            // 设置选中对象执行选中索引改变事件响应函数刷新选中对象参数显示
            lstObstacle.SelectedIndex = -1;
            lstObstacle.SelectedIndex = index;

            //picMatch.Image = MyMission.Instance().IMissionRef.Draw();
            SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
        }

        /// <summary>
        /// add by caiqiong 2010-11-28
        /// </summary>
        private void cmbObastacleType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 输入控件置默认值
            txtObstaclePosX.Text = "0";
            txtObstaclePosZ.Text = "0";
            txtObstacleLength.Text = "100";
            txtObstacleWidth.Text = "50";
            lstObstacle.Items.Clear();         // 清除列表
            MyMission myMission = MyMission.Instance();
            if (cmbObastacleType.Text.Equals("CIRCLE"))
            {
                txtObstacleWidth.Enabled = false;
                cmbObstacleDirection.Enabled = false;
                lblObstacleLength.Text = "Radius:";
                for (int i = 0; i < myMission.EnvRef.ObstaclesRound.Count; i++)
                {// 将当前仿真使命的全部圆形障碍物名称显示在列表中
                    lstObstacle.Items.Add(myMission.EnvRef.ObstaclesRound[i].Name);
                }
                if (myMission.EnvRef.ObstaclesRound.Count > 0)
                {// 当前仿真使命圆形障碍物数量不为0则选中第0个
                    lstObstacle.SelectedIndex = 0;
                }
            }
            else if (cmbObastacleType.Text.Equals("RECTANGLE"))
            {
                txtObstacleWidth.Enabled = true;
                cmbObstacleDirection.Enabled = true;
                lblObstacleLength.Text = "Length:";
                for (int i = 0; i < myMission.EnvRef.ObstaclesRect.Count; i++)
                {// 将当前仿真使命的全部方形障碍物名称显示在列表中
                    lstObstacle.Items.Add(myMission.EnvRef.ObstaclesRect[i].Name);
                }
                if (myMission.EnvRef.ObstaclesRect.Count > 0)
                {// 当前仿真使命方形障碍物数量不为0则选中第0个
                    lstObstacle.SelectedIndex = 0;
                }
            }
        }

        // LiYoubing 20110629
        /// <summary>
        /// 将方形障碍物的信息显示到界面控件 add by caiqiong 2010-11-28
        /// </summary>
        /// <param name="obj"></param>
        private void SetRectangularInfoToControls(RectangularObstacle obj)
        {
            txtObstaclePosX.Text = obj.PositionMm.X.ToString();
            txtObstaclePosZ.Text = obj.PositionMm.Z.ToString();
            txtObstacleLength.Text = obj.LengthMm.ToString();
            txtObstacleWidth.Text = obj.WidthMm.ToString();
            cmbObstacleDirection.SelectedItem = xna.MathHelper.ToDegrees(obj.DirectionRad).ToString();
            btnObstacleColorBorder.BackColor = obj.ColorBorder;
            btnObstacleColorFilled.BackColor = obj.ColorFilled;
            btnObstacleDelete.Enabled = obj.IsDeletionAllowed;
            btnObstacleModify.Enabled = obj.IsDeletionAllowed;
        }

        /// <summary>
        /// 将圆形障碍物的信息显示到界面控件 add by caiqiong 2010-11-28
        /// </summary>
        /// <param name="obj"></param>
        private void SetCircleInfoToControls(RoundedObstacle obj)
        {
            txtObstaclePosX.Text = obj.PositionMm.X.ToString();
            txtObstaclePosZ.Text = obj.PositionMm.Z.ToString();
            txtObstacleLength.Text = obj.RadiusMm.ToString();
            btnObstacleColorBorder.BackColor = obj.ColorBorder;
            btnObstacleColorFilled.BackColor = obj.ColorFilled;
            btnObstacleDelete.Enabled = obj.IsDeletionAllowed;
            btnObstacleModify.Enabled = obj.IsDeletionAllowed;
        }

        /// <summary>
        /// 响应障碍物列表选中项改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lstObstacle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstObstacle.SelectedIndex == -1) return;
            if (cmbObastacleType.Text.Equals("CIRCLE"))
            {// 当前选中的是圆形障碍物
                SetCircleInfoToControls(MyMission.Instance().EnvRef.ObstaclesRound[lstObstacle.SelectedIndex]);
            }
            else if (cmbObastacleType.Text.Equals("RECTANGLE"))
            {// 当前选中的是方形障碍物
                SetRectangularInfoToControls(MyMission.Instance().EnvRef.ObstaclesRect[lstObstacle.SelectedIndex]);
            }
        }

        /// <summary>
        /// 响应障碍物边框和填充颜色设置按钮点击事件 调用调色板设置颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnObstacleColor_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;

            ColorDialog colorDlg = new ColorDialog();
            colorDlg.ShowHelp = true;
            colorDlg.Color = btn.BackColor;

            if (colorDlg.ShowDialog() == DialogResult.OK)
            {// 确认选择则更新按钮上显示的颜色和相应仿真障碍物的边框颜色
                btn.BackColor = colorDlg.Color;
                // 没有选中任何障碍物修改颜色按钮的背景色后即返回
                if (lstObstacle.SelectedIndex == -1) return;
                if (cmbObastacleType.SelectedItem.ToString().Equals("CIRCLE"))
                {// 当前正在操作的是圆形障碍物
                    if (btn.Name.Equals("btnObstacleColorBorder"))
                    {// 点击的是障碍物边框颜色设置按钮
                        MyMission.Instance().EnvRef.ObstaclesRound[lstObstacle.SelectedIndex].ColorBorder
                            = btn.BackColor;
                    }
                    else
                    {// 点击的是障碍物填充颜色设置按钮
                        MyMission.Instance().EnvRef.ObstaclesRound[lstObstacle.SelectedIndex].ColorFilled
                           = btn.BackColor;
                    }
                }
                else if (cmbObastacleType.SelectedItem.ToString().Equals("RECTANGLE"))
                {// 当前正在操作的是方形障碍物
                    if (btn.Name.Equals("btnObstacleColorBorder"))
                    {// 点击的是障碍物边框颜色设置按钮
                        MyMission.Instance().EnvRef.ObstaclesRect[lstObstacle.SelectedIndex].ColorBorder
                            = btn.BackColor;
                    }
                    else
                    {// 点击的是障碍物填充颜色设置按钮
                        MyMission.Instance().EnvRef.ObstaclesRect[lstObstacle.SelectedIndex].ColorFilled
                            = btn.BackColor;
                    }
                }
            }
        }
        #endregion

        #region 主界面Field面板Field Setting处理
        private void btnFieldDefaultSetting_Click(object sender, EventArgs e)
        {// 恢复到默认场地必须同时重置仿真机器鱼/水球/障碍物等的参数否则可能出现对象超出场地边界的问题
            MyMission.Instance().IMissionRef.ResetField();
            MyMission.Instance().IMissionRef.ResetTeamsAndFishes();
            MyMission.Instance().IMissionRef.ResetBalls();
            MyMission.Instance().IMissionRef.ResetObstacles();
            Bitmap bmp = URWPGSim2D.Gadget.WaveEffect.Instance().GetBitmap();
            SetFieldBmp(Field.Instance().Draw(bmp,MyMission.Instance().ParasRef.IsGoalBlockNeeded,MyMission.Instance().ParasRef.IsFieldInnerLinesNeeded));
            SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
            ProcessFieldInfoUpdating();
            // 默认开启水波效果 LiYoubing 20110726 默认关闭 20110823
            chkWaveEffect.Checked = false;
        }
        #endregion

        #region 仿真场地绘制控件PictureBox的MouseMove/MouseLeave/MouseDown/MouseUp等事件 Weiqingdan 20101130
        /// <summary>
        /// picMatch_MouseMove和MouseDown的公共调用方法
        /// 判断当前是否选中场上的仿真机器鱼或仿真水球，如果选中了则记录其编号
        /// </summary>
        /// <param name="ptRealField">当前鼠标位置在场上的以毫米为单位的实际坐标</param>
        /// <param name="isForDragging">true为MouseDown调用表示需要选中供拖动否则为MouseMove调用</param>
        /// <param name="isBallSelected">输出 当前是否选中仿真水球</param>
        /// <param name="isFishSelected">输出 当前是否选中仿真机器鱼</param>
        private void DetectRoboFishAndBall(Point ptRealField, bool isForDragging,
            ref bool isBallSelected, ref bool isFishSelected)
        {
            MyMission missionref = MyMission.Instance();
            isBallSelected = false;
            isFishSelected = false;

            // 处理仿真水球选中/拖动
            for (int i = 0; i < missionref.EnvRef.Balls.Count; i++)
            {
                // 鼠标当前位置到第i个水球中心点的距离
                float deltaX = ptRealField.X - missionref.EnvRef.Balls[i].PositionMm.X;
                float deltaZ = ptRealField.Y - missionref.EnvRef.Balls[i].PositionMm.Z;
                double disToBall = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
                if (disToBall < missionref.EnvRef.Balls[i].RadiusMm)
                {
                    if (!_isDragging && isForDragging) _isDragging = true;
                    _selectedBallId = i;//isForDragging ? i : -1;  // 选中第i个仿真水球
                    _selectedTeamId = -1;
                    _selectedFishId = -1;
                    _selectedRoundObstacleId = -1;
                    _selectedRectObstacleId = -1;
                    _selectedChannelId = -1;
                    isBallSelected = true;
                    break;  // 有仿真水球被选中了则不可能再有其他对象被选中
                }
            }

            // 没有仿真水球被选中，才需要检测是否有仿真机器鱼被选中/拖动
            if (isBallSelected == false)
            {
                for (int i = 0; i < missionref.TeamsRef.Count; i++)
                {
                    for (int j = 0; j < missionref.TeamsRef[i].Fishes.Count; j++)
                    {
                        float deltaX = ptRealField.X - missionref.TeamsRef[i].Fishes[j].PositionMm.X;
                        float deltaZ = ptRealField.Y - missionref.TeamsRef[i].Fishes[j].PositionMm.Z;
                        double disToFish = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);

                        // 仅检测鼠标当前位置是否位于仿真机器鱼前端矩形区域的外接圆内
                        if (disToFish < missionref.TeamsRef[i].Fishes[j].FishBodyRadiusMm)
                        {
                            if (!_isDragging && isForDragging) _isDragging = true;
                            _selectedTeamId = i;//isForDragging ? i : -1;  // 第i支队伍
                            _selectedFishId = j;//isForDragging ? j : -1;  // 第j条仿真机器鱼被选中
                            _selectedBallId = -1;
                            _selectedRoundObstacleId = -1;
                            _selectedRectObstacleId = -1;
                            _selectedChannelId = -1;
                            isFishSelected = true;
                            break;  // 有仿真机器鱼被选中则不可能再有其他对象被选中
                        }
                    }// end for (int j = 0...
                }// end for (int i = 0...
            }// end if (isThereBallSelected...
        }

        /// <summary>
        /// picMatch_MouseMove和MouseDown的公共调用方法
        /// 判断当前是否选中场上的障碍物或通道，如果选中了则记录其编号  added by liushu 20110224
        /// </summary>
        /// <param name="ptRealField">当前鼠标位置在场上的以毫米为单位的实际坐标</param>
        /// <param name="isForDragging">true为MouseDown调用表示需要选中供拖动否则为MouseMove调用</param>
        /// <param name="isRoundObstaclesSelected">输出 当前是否选中仿真圆形障碍物</param>
        /// <param name="isRectObstaclesSelected">输出 当前是否选中仿真矩形障碍物</param>
        /// <param name="isFishSelected">输出 当前是否选中仿真通道</param>
        private void DetectObstaclesAndChannels(Point ptRealField, bool isForDragging,
            ref bool isRoundObstaclesSelected, ref bool isRectObstaclesSelected, ref bool isChannelsSelected)
        {
            MyMission missionref = MyMission.Instance();
            isRoundObstaclesSelected = false;
            isRectObstaclesSelected = false;
            isChannelsSelected = false;

            // 处理仿真圆形障碍物选中/拖动
            for (int i = 0; i < missionref.EnvRef.ObstaclesRound.Count; i++)
            {
                // 鼠标当前位置到第i个圆形障碍物中心点的距离
                float deltaX = ptRealField.X - missionref.EnvRef.ObstaclesRound[i].PositionMm.X;
                float deltaZ = ptRealField.Y - missionref.EnvRef.ObstaclesRound[i].PositionMm.Z;
                double disToRoundObstacle = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);

                if (disToRoundObstacle < missionref.EnvRef.ObstaclesRound[i].RadiusMm)
                {
                    if (!_isDragging && isForDragging) _isDragging = true;
                    _selectedRoundObstacleId = i;    //isForDragging ? i : -1;  // 选中第i个仿真圆形障碍物
                    _selectedRectObstacleId = -1;
                    _selectedChannelId = -1;
                    _selectedBallId = -1;
                    _selectedTeamId = -1;
                    _selectedFishId = -1;
                    isRoundObstaclesSelected = true;
                    return; // 有仿真圆形障碍物被选中了则不可能再有其他对象被选中
                }
            }

            // 没有仿真圆形障碍物被选中，检测是否有仿真矩形障碍物被选中/拖动
            if (isRoundObstaclesSelected == false)
            {
                for (int i = 0; i < missionref.EnvRef.ObstaclesRect.Count; i++)
                {
                    if (missionref.EnvRef.ObstaclesRect[i].DirectionRad == 0)
                    {//如果矩形障碍物的方向角为0
                        float X1 = missionref.EnvRef.ObstaclesRect[i].PositionMm.X - missionref.EnvRef.ObstaclesRect[i].LengthMm / 2;
                        float X2 = missionref.EnvRef.ObstaclesRect[i].PositionMm.X + missionref.EnvRef.ObstaclesRect[i].LengthMm / 2;
                        float Z1 = missionref.EnvRef.ObstaclesRect[i].PositionMm.Z - missionref.EnvRef.ObstaclesRect[i].WidthMm / 2;
                        float Z2 = missionref.EnvRef.ObstaclesRect[i].PositionMm.Z + missionref.EnvRef.ObstaclesRect[i].WidthMm / 2;

                        // 检测鼠标当前位置是否位于障碍物矩形区域中
                        if ((ptRealField.X > X1) && (ptRealField.X < X2) && (ptRealField.Y > Z1) && (ptRealField.Y < Z2))
                        {
                            if (!_isDragging && isForDragging) _isDragging = true;
                            _selectedRectObstacleId = i;    //isForDragging ? i : -1;  // 第i个矩形障碍物
                            _selectedRoundObstacleId = -1;
                            _selectedChannelId = -1;
                            _selectedBallId = -1;
                            _selectedTeamId = -1;
                            _selectedFishId = -1;
                            isRectObstaclesSelected = true;
                            return; // 有仿真矩形障碍物被选中则不可能再有其他对象被选中
                        }
                    }
                    else
                    {//如果矩形障碍物的方向角不为0
                        float deltaX = ptRealField.X - missionref.EnvRef.ObstaclesRect[i].PositionMm.X;
                        float deltaZ = ptRealField.Y - missionref.EnvRef.ObstaclesRect[i].PositionMm.Z;
                        double disToRectObstacle = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
                        float rad = Math.Min(missionref.EnvRef.ObstaclesRect[i].WidthMm, missionref.EnvRef.ObstaclesRect[i].LengthMm);

                        //检测鼠标当前位置是否位于障碍物矩形区域的内切圆中
                        if (disToRectObstacle < rad)
                        {
                            if (!_isDragging && isForDragging) _isDragging = true;
                            _selectedRectObstacleId = i;    //isForDragging ? i : -1;  // 选中第i个仿真矩形障碍物
                            _selectedRoundObstacleId = -1;    
                            _selectedChannelId = -1;
                            _selectedBallId = -1;
                            _selectedTeamId = -1;
                            _selectedFishId = -1;
                            isRectObstaclesSelected = true;
                            return; // 有仿真矩形障碍物被选中了则不可能再有其他对象被选中
                        }
                    }//end else
                }//end  for (int i = 0...               
            }//end if (isRoundObstaclesSelected...

            #region
            ////没有仿真障碍物被选中，检测是否有仿真通道被选中
            //if (isRoundObstaclesSelected == false && isRectObstaclesSelected == false)
            //{
            //    for (int i = 0; i < missionref.EnvRef.Channels.Count; i++)
            //    {
            //        if (missionref.EnvRef.Channels[i].DirectionRad == 0)
            //        {//如果矩形障碍物的方向角为0
            //            float X1 = missionref.EnvRef.Channels[i].PositionMm.X - missionref.EnvRef.Channels[i].LengthMm / 2;
            //            float X2 = missionref.EnvRef.Channels[i].PositionMm.X + missionref.EnvRef.Channels[i].LengthMm / 2;
            //            float Z1 = missionref.EnvRef.Channels[i].PositionMm.Z - missionref.EnvRef.Channels[i].WidthMm / 2;
            //            float Z2 = missionref.EnvRef.Channels[i].PositionMm.Z + missionref.EnvRef.Channels[i].WidthMm / 2;

            //            // 检测鼠标当前位置是否位于障碍物矩形区域中
            //            if ((ptRealField.X > X1) && (ptRealField.X < X2) && (ptRealField.Y > Z1) && (ptRealField.Y < Z2))
            //            {
            //                if (!_isDragging && isForDragging) _isDragging = true;
            //                _selectedChannelId = i;    //isForDragging ? i : -1;  // 第i个通道
            //                _selectedRoundObstacleId = -1;
            //                _selectedRectObstacleId = -1;
            //                _selectedBallId = -1;
            //                _selectedTeamId = -1;
            //                _selectedFishId = -1;
            //                isChannelsSelected = true;
            //                return;   // 有仿真通道被选中则不可能再有其他对象被选中
            //            }
            //        }//end if (missionref.EnvRef.Channels...
            //        else
            //        {//如果矩形障碍物的方向角不为0
            //            float deltaX = ptRealField.X - missionref.EnvRef.Channels[i].PositionMm.X;
            //            float deltaZ = ptRealField.Y - missionref.EnvRef.Channels[i].PositionMm.Z;
            //            double disToRectChannel = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
            //            float rad = Math.Min(missionref.EnvRef.Channels[i].WidthMm, missionref.EnvRef.Channels[i].LengthMm);

            //            //检测鼠标当前位置是否位于障碍物矩形区域的内切圆中
            //            if (disToRectChannel < rad)
            //            {
            //                if (!_isDragging && isForDragging) _isDragging = true;
            //                _selectedChannelId = i;    //isForDragging ? i : -1;  // 选中第i个仿真通道
            //                _selectedRoundObstacleId = -1;
            //                _selectedRectObstacleId = -1;
            //                _selectedBallId = -1;
            //                _selectedTeamId = -1;
            //                _selectedFishId = -1;
            //                isChannelsSelected = true;
            //                return;   // 有仿真通道被选中了则不可能再有其他对象被选中
            //            }
            //        }//end else
            //    }// end for (missionref...
            //}//end if (isRoundObstaclesSelected...
            #endregion
        }

        /// <summary>
        /// 鼠标移动时显示场地实际坐标，如果在仿真机器鱼或者仿真水球上，则显示其相应的位置
        /// </summary>
        private void picMatch_MouseMove(object sender, MouseEventArgs e)
        {
            // 处理程序刚启动时，默认选中仿真使命尚未初始化完成时的情况
            if (MyMission.Instance().TeamsRef == null) return;

            MyMission missionref = MyMission.Instance();

            // 仿真使命不在运行时才处理鼠标移动显示实际场地坐标功能
            if (!missionref.ParasRef.IsRunning)
            {
                Point ptRealField = Field.Instance().PixToMm(new Point(e.X, e.Y));
                bool isBallSelected = false;
                bool isFishSelected = false;
                bool isRoundObstaclesSelected = false;
                bool isRectObstaclesSelected = false;
                bool isChannelsSelected = false;
                DetectRoboFishAndBall(ptRealField, false, ref isBallSelected, ref isFishSelected);
                DetectObstaclesAndChannels(ptRealField, false, ref isRoundObstaclesSelected, ref isRectObstaclesSelected, ref isChannelsSelected);
                
                if (isBallSelected == true)         // 鼠标滑过仿真水球
                {
                    // 当前仿真使命只使用了一个仿真水球时，选中后只显示Ball，否则显示Ball1,Ball2...
                    lblFishOrBallSelected.Text = "Selected: ";
                    lblFishOrBallSelected.Text += 
                        (missionref.EnvRef.Balls.Count > 1) ? "Ball" + (_selectedBallId + 1) : "Ball";
                }
                else if (isFishSelected == true)    // 鼠标滑过仿真机器鱼
                {
                    lblFishOrBallSelected.Text =
                        String.Format("Selected: Team{0} Fish{1}", _selectedTeamId + 1, _selectedFishId + 1);
                }
                else if (isRoundObstaclesSelected == true)  //鼠标滑过仿真圆形障碍物
                {
                    // 当前仿真使命只使用了一个仿真圆形障碍物时，选中后只显示RoundObstacle，否则显示RoundObstacle1,RoundObstacle2...
                    lblFishOrBallSelected.Text = "Selected: ";
                    lblFishOrBallSelected.Text +=
                        (missionref.EnvRef.ObstaclesRound.Count > 1) ? "RoundObstacle" + (_selectedRoundObstacleId + 1) : "RoundObstacle";
                }
                else if (isRectObstaclesSelected == true)  //鼠标滑过仿真矩形障碍物
                {
                    // 当前仿真使命只使用了一个仿真矩形障碍物时，选中后只显示RectObstacle，否则显示RectObstacle1,RectObstacle2...
                    lblFishOrBallSelected.Text = "Selected: ";
                    lblFishOrBallSelected.Text +=
                        (missionref.EnvRef.ObstaclesRect.Count > 1) ? "RectObstacle" + (_selectedRectObstacleId + 1) : "RectObstacle";
                }
                else if (isChannelsSelected == true)  //鼠标滑过仿真通道
                {
                    // 当前仿真使命只使用了一个仿真矩形障碍物时，选中后只显示Channel，否则显示Channel1,Channel2...
                    lblFishOrBallSelected.Text = "Selected: ";
                    lblFishOrBallSelected.Text +=
                        (missionref.EnvRef.Channels.Count > 1) ? "Channel" + (_selectedChannelId + 1) : "Channel";
                }
                else
                {
                    lblFishOrBallSelected.Text = "";
                }

                lblFishOrBallPosition.Text = "X: " + ptRealField.X + " Z: " + ptRealField.Y;
                this.Cursor = (isBallSelected || isFishSelected || isRoundObstaclesSelected || isRectObstaclesSelected || isChannelsSelected) ? Cursors.NoMove2D : Cursors.Default;

                //picMatch.Image = MyMission.Instance().IMissionRef.Draw();
                SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
            }
        }

        /// <summary>
        /// 鼠标离开场地绘图容器PictureBox时，清空显示的鼠标坐标信息
        /// </summary>
        private void picMatch_MouseLeave(object sender, EventArgs e)
        {
            lblFishOrBallPosition.Text = "";
            //picMatch.Image = MyMission.Instance().IMissionRef.Draw();
            SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
        }

        #region 鼠标拖动仿真场地上的动态对象（仿真机器鱼/水球/障碍物）
        /// <summary>
        /// 当前界面场地中选中的仿真机器鱼所在队伍编号（如3V3中的0,1）
        /// </summary>
        int _selectedTeamId = -1;

        /// <summary>
        /// 当前界面场地中选中的仿真机器鱼在其所在队伍的内的编号（如3V3中的0,1,2）
        /// </summary>
        int _selectedFishId = -1;

        /// <summary>
        /// 当前界面场地中选中的仿真水球在Env.Balls列表中的编号（如3V3中的0）
        /// </summary>
        int _selectedBallId = -1;

        /// <summary>
        /// 当前界面场地中选中的仿真圆形障碍物在Env.ObstaclesRounds列表中的编号
        /// </summary>
        int _selectedRoundObstacleId = -1;

        /// <summary>
        /// 当前界面场地中选中的仿真方形障碍物在Env.ObstaclesRects列表中的编号
        /// </summary>
        int _selectedRectObstacleId = -1;

        /// <summary>
        /// 当前界面场地中选中的仿真通道在Env.Channels列表中的编号
        /// </summary>
        int _selectedChannelId = -1;

        /// <summary>
        /// 指示当前是否正在执行拖动操作
        /// </summary>
        bool _isDragging = false;

        /// <summary>
        /// 鼠标按下时准备拖动仿真机器鱼或者仿真水球
        /// </summary>
        private void picMatch_MouseDown(object sender, MouseEventArgs e)
        {
            // 处理程序刚启动时，默认选中仿真使命尚未初始化完成时的情况
            if (MyMission.Instance().TeamsRef == null) return;
            // 仿真使命不在运行时才允许拖动场上的动态对象（仿真机器鱼和仿真水球）
            //if (!MyMission.Instance().ParasRef.IsRunning)
            //{
            Point ptRealField = Field.Instance().PixToMm(new Point(e.X, e.Y));
            bool isBallSelected = false;
            bool isFishSelected = false;
            bool isRoundObstaclesSelected = false;
            bool isRectObstaclesSelected = false;
            bool isChannelsSelected = false;

            DetectRoboFishAndBall(ptRealField, true, ref isBallSelected, ref isFishSelected);
            DetectObstaclesAndChannels(ptRealField, true, ref isRoundObstaclesSelected, ref isRectObstaclesSelected, ref isChannelsSelected);    

            this.Cursor = (isBallSelected || isFishSelected || isRoundObstaclesSelected || isRectObstaclesSelected || isChannelsSelected) ? Cursors.NoMove2D : Cursors.Default;
            //}
        }

        /// <summary>
        /// 鼠标释放时将仿真机器鱼/水球/障碍物放到光标的当前位置
        /// </summary>
        private void picMatch_MouseUp(object sender, MouseEventArgs e)
        {
            // 仿真使命不在运行时才允许拖动场上的动态对象（仿真机器鱼和仿真水球）
            //if (!MyMission.Instance().ParasRef.IsRunning)
            //{
            // 不在拖动或什么都没选中则直接返回
            if ((_isDragging == false) || 
                ((_selectedBallId == -1) && (_selectedTeamId == -1) && (_selectedFishId == -1) 
                && (_selectedRectObstacleId == -1) && (_selectedRoundObstacleId == -1))) return;

            MyMission myMission = MyMission.Instance();
            Field f = Field.Instance();
            Point ptRealField = Field.Instance().PixToMm(new Point(e.X, e.Y));

            if (myMission.ParasRef.IsGoalBlockNeeded)//需要绘制球门块时的场地左右边界检测方法 added by zhangbo 20111110
            {
                if ((ptRealField.X < f.LeftMm) && (ptRealField.Y > -f.GoalWidthMm / 2)
                    && (ptRealField.Y < f.GoalWidthMm / 2))
                {
                    ptRealField.X = f.LeftMm;
                }
                else if ((ptRealField.X < f.LeftMm + f.GoalDepthMm)
                    && ((ptRealField.Y < -f.GoalWidthMm / 2) || (ptRealField.Y > f.GoalWidthMm / 2)))
                {
                    ptRealField.X = f.LeftMm + f.GoalDepthMm;
                }
                else if ((ptRealField.X > f.RightMm) && (ptRealField.Y > -f.GoalWidthMm / 2)
                    && (ptRealField.Y < f.GoalWidthMm / 2))
                {
                    ptRealField.X = f.RightMm;
                }
                else if ((ptRealField.X > f.RightMm - f.GoalDepthMm)
                    && ((ptRealField.Y < -f.GoalWidthMm / 2) || (ptRealField.Y > f.GoalWidthMm / 2)))
                {
                    ptRealField.X = f.RightMm - f.GoalDepthMm;
                }
            }
            else//此时场地上没有球门块 added by zhangbo 20111110
            {
                if (ptRealField.X < f.LeftMm)
                {
                    ptRealField.X = f.LeftMm;
                }
                else if (ptRealField.X > f.RightMm)
                {
                    ptRealField.X = f.RightMm;
                }
            }

            if (ptRealField.Y < f.TopMm)
            {
                ptRealField.Y = f.TopMm;
            }
            else if (ptRealField.Y > f.BottomMm)
            {
                ptRealField.Y = f.BottomMm;
            }

            if (_isDragging && (_selectedBallId > -1))
            {// 选中的是仿真水球
                myMission.EnvRef.Balls[_selectedBallId].PositionMm.X = ptRealField.X;
                myMission.EnvRef.Balls[_selectedBallId].PositionMm.Z = ptRealField.Y;

                // 参数合法性检查及调整
                Ball obj = myMission.EnvRef.Balls[_selectedBallId];
                UrwpgSimHelper.ParametersCheckingBall(ref obj);

                // 更新显示
                _listBallSettingControls[_selectedBallId].TxtPositionMmX.Text = obj.PositionMm.X.ToString();
                _listBallSettingControls[_selectedBallId].TxtPositionMmZ.Text = obj.PositionMm.Z.ToString();

                _selectedBallId = -1;
            }
            else if (_isDragging && (_selectedTeamId > -1) && (_selectedFishId > -1))
            { // 选中的是仿真机器鱼
                myMission.TeamsRef[_selectedTeamId].Fishes[_selectedFishId].PositionMm.X = ptRealField.X;
                myMission.TeamsRef[_selectedTeamId].Fishes[_selectedFishId].PositionMm.Z = ptRealField.Y;

                // 参数合法性检查及调整
                RoboFish obj = myMission.TeamsRef[_selectedTeamId].Fishes[_selectedFishId];
                UrwpgSimHelper.ParametersCheckingRoboFish(ref obj);

                // 更新显示
                _listFishSettingControls[_selectedTeamId].CmbPlayers.SelectedIndex = -1;
                _listFishSettingControls[_selectedTeamId].CmbPlayers.SelectedIndex = _selectedFishId;

                _selectedTeamId = -1;
                _selectedFishId = -1;
            }
            else if (_isDragging && (_selectedRoundObstacleId > -1))
            {//选中的是仿真圆形障碍物
                if (myMission.ParasRef.IsRunning == false)
                {// 仿真使命不在运行才允许拖动仿真障碍物
                    myMission.EnvRef.ObstaclesRound[_selectedRoundObstacleId].PositionMm.X = ptRealField.X;
                    myMission.EnvRef.ObstaclesRound[_selectedRoundObstacleId].PositionMm.Z = ptRealField.Y;

                    // 参数合法性检查及调整
                    RoundedObstacle obj = myMission.EnvRef.ObstaclesRound[_selectedRoundObstacleId];
                    UrwpgSimHelper.ParametersCheckingObstacle(ref obj);

                    // 更新显示                    
                    cmbObastacleType.SelectedIndex = 0; // 选中“CIRCLE”
                    lstObstacle.SelectedIndex = -1;
                    lstObstacle.SelectedIndex = _selectedRoundObstacleId;

                    _selectedRoundObstacleId = -1;
                }
            }
            else if (_isDragging && (_selectedRectObstacleId > -1))
            {//选中的是仿真矩形障碍物
                if (myMission.ParasRef.IsRunning == false)
                {// 仿真使命不在运行才允许拖动仿真障碍物
                    myMission.EnvRef.ObstaclesRect[_selectedRectObstacleId].PositionMm.X = ptRealField.X;
                    myMission.EnvRef.ObstaclesRect[_selectedRectObstacleId].PositionMm.Z = ptRealField.Y;

                    // 参数合法性检查及调整
                    RectangularObstacle obj = myMission.EnvRef.ObstaclesRect[_selectedRectObstacleId];
                    UrwpgSimHelper.ParametersCheckingObstacle(ref obj);

                    // 更新显示
                    cmbObastacleType.SelectedIndex = 1; // 选中“RECTANGLE”
                    lstObstacle.SelectedIndex = -1;
                    lstObstacle.SelectedIndex = _selectedRectObstacleId;

                    _selectedRectObstacleId = -1;
                }
            }

            //picMatch.Image = myMission.IMissionRef.Draw();
            SetMatchBmp(myMission.IMissionRef.Draw());

            _isDragging = false;
            this.Cursor = Cursors.Default;
        }
        #endregion
        #endregion

        #region 仿真场地绘制控件PictureBox的Paint/DoubleClick事件及主界面Form的KeyDown事件 Weiqingdan
        /// <summary>
        /// 响应绘图控件PictureBox的Paint事件 实现水池区域各Label的透明显示
        /// </summary>
        private void picMatch_Paint(object sender, PaintEventArgs e)
        {
            Field f = Field.Instance();
            MyMission myMission = MyMission.Instance();
            if (myMission.TeamsRef == null) return;
            SolidBrush brush = new SolidBrush(lblTmp.ForeColor);    // 使命名称/队名颜色
            SolidBrush brushEmphasized = new SolidBrush(Color.Red); // 倒计时/比分颜色

            // 使命名称和倒计时显示行距离左边球门线10像素距离场地下边距80像素
            lblTmp.Location = new Point(f.FieldOuterBorderPix + f.FieldInnerBorderPix + f.GoalDepthPix + 10,
                2 * f.FieldOuterBorderPix + 2 * f.FieldInnerBorderPix + f.FieldLengthZPix - 80);

            int startX = lblTmp.Location.X;         // 使命名称和倒计时显示行X坐标
            lblTmp.Text = myMission.ParasRef.Name;  // 显示使命名称
            e.Graphics.DrawString(lblTmp.Text, lblTmp.Font, brush, startX, lblTmp.Location.Y);

            startX += lblTmp.Width + 10;            // X坐标往右偏移使命名称所占像素值
            int seconds = myMission.ParasRef.RemainingCycles * myMission.ParasRef.MsPerCycle / 1000;
            lblTmp.Text = string.Format("{0:00} : {1:00}", seconds / 60, seconds % 60); // 显示使命倒计时
            e.Graphics.DrawString(lblTmp.Text, lblTmp.Font, brushEmphasized, startX, lblTmp.Location.Y);

            lblTmp.Location = new Point(lblTmp.Location.X, lblTmp.Location.Y + 30);
            startX = lblTmp.Location.X;         // 队伍名称和计分显示行X坐标

            // 2支队伍的对抗性比赛以“队名1 得分1 : 得分2 队名2”格式显示
            if (myMission.TeamsRef.Count == 2)
            {
                lblTmp.Text = myMission.TeamsRef[0].Para.Name; // 显示队名1
                e.Graphics.DrawString(lblTmp.Text, lblTmp.Font, brush, startX, lblTmp.Location.Y);

                startX += lblTmp.Width + 10;
                lblTmp.Text = string.Format("{0:00} : {1:00}", myMission.TeamsRef[0].Para.Score,
                    myMission.TeamsRef[1].Para.Score); // 以比分形式显示2支队伍的得分
                e.Graphics.DrawString(lblTmp.Text, lblTmp.Font, brushEmphasized, startX, lblTmp.Location.Y);

                startX += lblTmp.Width + 10;
                lblTmp.Text = myMission.TeamsRef[1].Para.Name; // 显示队名2
                e.Graphics.DrawString(lblTmp.Text, lblTmp.Font, brush, startX, lblTmp.Location.Y);
            }
            //else if (myMission.TeamsRef.Count == 1)    // 1支队伍只显示队名
            //{
            //    lblTmp.Text = myMission.TeamsRef[0].Para.Name;
            //    e.Graphics.DrawString(lblTmp.Text, lblTmp.Font, brush, startX, lblTmp.Location.Y);
            //}
            else // 3支及以上的队伍按组显示“队名 得分”
            {
                startX -= 10;                           // 抵消使第1支队伍名称前不必要的偏移

                // 按组显示“队名i 得分i”信息
                for (int i = 0; i < myMission.TeamsRef.Count; i++)
                {
                    startX += 10;                       // 相对上一组“队名 得分”偏移10像素
                    lblTmp.Text = myMission.TeamsRef[i].Para.Name;
                    e.Graphics.DrawString(lblTmp.Text, lblTmp.Font, brush, startX, lblTmp.Location.Y);

                    if (myMission.TeamsRef[i].Para.Score >= 0)
                    {// 得分为负值则不显示 所以可以给不需要用到得分值的仿真使命的队伍得分值置为负值
                        startX += lblTmp.Width + 10;        // 得分相对队名偏移10像素
                        lblTmp.Text = string.Format("{0:00}", myMission.TeamsRef[0].Para.Score);
                        e.Graphics.DrawString(lblTmp.Text, lblTmp.Font, brushEmphasized, startX, lblTmp.Location.Y);
                        startX += lblTmp.Width;
                    }
                }
            }

            // 绘制底部状态栏中的当前焦点坐标
            Label LB_S = (Label)lblFishOrBallPosition;
            LB_S.Visible = false;
            e.Graphics.DrawString(LB_S.Text, LB_S.Font, new SolidBrush(LB_S.ForeColor),
                f.FieldOuterBorderPix + f.FieldInnerBorderPix + f.GoalDepthPix + 10,
                2 * f.FieldOuterBorderPix + 2 * f.FieldInnerBorderPix + f.FieldLengthZPix - 18);

            // 绘制底部状态栏中的当前选中对象（仿真水球/仿真机器鱼/仿真障碍物等）名称
            Label lbl_S = (Label)lblFishOrBallSelected;
            lbl_S.Visible = false;
            e.Graphics.DrawString(lbl_S.Text, lbl_S.Font, new SolidBrush(lbl_S.ForeColor),
                f.FieldOuterBorderPix + f.FieldInnerBorderPix + f.GoalDepthPix + 10 + LB_S.Size.Width,
                2 * f.FieldOuterBorderPix + 2 * f.FieldInnerBorderPix + f.FieldLengthZPix - 18);

            SolidBrush drawBrushA = new SolidBrush(Color.Red);
            SolidBrush drawBrushB = new SolidBrush(Color.Yellow);
            if (myMission.ParasRef.TeamCount == 2)
            {// 对于2支队伍参与的仿真使命绘制左右半场标志颜色与各队伍仿真机器鱼前端色标颜色一致
                drawBrushA = new SolidBrush(myMission.TeamsRef[0].Fishes[0].ColorFish);
                drawBrushB = new SolidBrush(myMission.TeamsRef[1].Fishes[0].ColorFish);
            }
            // 绘制左半场标志
            String drawStringA = "L";
            Font drawFontA = new Font("黑体", 20);
            PointF drawPointA = new PointF(0, (f.FieldLengthZPix - f.GoalWidthPix) / 2);
            e.Graphics.DrawString(drawStringA, drawFontA, drawBrushA, drawPointA);
            e.Graphics.FillRectangle(drawBrushA,
                new Rectangle(new Point(3, (f.FieldLengthZPix + f.GoalWidthPix) / 2), new Size(14, 14)));

            // 绘制右半场标志
            String drawStringB = "R";
            Font drawFontB = new Font("黑体", 20);
            PointF drawPointB = new PointF(f.FieldLengthXPix + 2 * f.FieldInnerBorderPix + 20,
                (f.FieldLengthZPix - f.GoalWidthPix) / 2);
            e.Graphics.DrawString(drawStringB, drawFontB, drawBrushB, drawPointB);
            e.Graphics.FillRectangle(drawBrushB,
                new Rectangle(new Point(f.FieldLengthXPix + 2 * f.FieldInnerBorderPix + 2 * f.FieldOuterBorderPix - 17,
                    (f.FieldLengthZPix + f.GoalWidthPix) / 2), new Size(14, 14)));
        }

        // Weiqingdan
        /// <summary>
        /// 响应绘图控件PictureBox的DoubleClick事件 进入或退出全屏模式
        /// </summary>
        private void picMatch_DoubleClick(object sender, EventArgs e)
        {
            #region
            // 水波组件初始化
            InitWaveEffect();
            Field f = Field.Instance();

            // 全屏操作适应多显示器环境 LiYoubing 20110803
            // 窗口当前所在显示器
            Screen currentScreen = Screen.FromHandle(this.Handle);
            int ScreenWidth = currentScreen.Bounds.Width;    // 显示器宽度
            int ScreenHeight = currentScreen.Bounds.Height;  // 显示器高度
            if (this.WindowState == FormWindowState.Normal)     // 当前是正常窗口状态则放大到全屏
            {
                this.FormBorderStyle = FormBorderStyle.None;    // 此时this.FormBorderStyle为None 不会显示窗体标题栏等相关
                this.WindowState = FormWindowState.Maximized;
                //this.TopMost = true;

                //int ScreenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;    // 显示器宽度
                //int ScreenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;  // 显示器高度
                double dRatioScreenWidthAndHeight = (double)ScreenWidth / (double)ScreenHeight;

                double dRatioFieldXMmAndZMm = (double)(f.FieldLengthXMm) / (double)(f.FieldLengthZMm);
                if (dRatioFieldXMmAndZMm <= dRatioScreenWidthAndHeight)
                {
                    f.FieldLengthZPix = ScreenHeight - 2 * f.FieldInnerBorderPix - 2 * f.FieldOuterBorderPix;
                    f.ScaleMmToPix = (double)f.FieldLengthZMm / (double)f.FieldLengthZPix;
                    f.FieldLengthXPix = (int)(f.FieldLengthXMm / f.ScaleMmToPix + 0.5);
                    f.GoalDepthPix = (int)(f.GoalDepthMm / f.ScaleMmToPix + 0.5);
                    f.GoalWidthPix = (int)(f.GoalWidthMm / f.ScaleMmToPix + 0.5);
                    f.GoalBlockWidthPix = (int)((f.FieldLengthZPix - f.GoalWidthPix) / 2 + 0.5);
                    f.GoalBlockDepthPix = f.GoalDepthPix;

                    f.PictureBoxXPix = f.FieldLengthXPix + 2 * f.FieldInnerBorderPix + 2 * f.FieldOuterBorderPix;
                    f.PictureBoxZPix = f.FieldLengthZPix + 2 * f.FieldInnerBorderPix + 2 * f.FieldOuterBorderPix;
                    f.ForbiddenZoneLengthXPix = (int)(f.ForbiddenZoneLengthXMm / f.ScaleMmToPix + 0.5);
                    f.ForbiddenZoneLengthZPix = (int)(f.ForbiddenZoneLengthZMm / f.ScaleMmToPix + 0.5);
                    f.CenterCircleRadiusPix = (int)(f.CenterCircleRadiusMm / f.ScaleMmToPix + 0.5);

                    int iSubtractScreenWidthAndPictureBoxX = ScreenWidth - f.PictureBoxXPix;
                    picMatch.Location = new Point(iSubtractScreenWidthAndPictureBoxX / 2, 0);
                    picMatch.Width = f.PictureBoxXPix;
                    picMatch.Height = f.PictureBoxZPix;

                    tabServerControlBoard.Visible = false;
                }
                else
                {
                    f.PictureBoxXPix = ScreenWidth;
                    f.FieldLengthXPix = f.PictureBoxXPix - 2 * f.FieldOuterBorderPix - 2 * f.FieldInnerBorderPix;
                    f.ScaleMmToPix = (double)(f.FieldLengthXMm) / (double)(f.FieldLengthXPix);
                    f.FieldLengthZPix = (int)(f.FieldLengthZMm / f.ScaleMmToPix + 0.5);
                    f.GoalDepthPix = (int)(f.GoalDepthMm / f.ScaleMmToPix + 0.5);
                    f.GoalWidthPix = (int)(f.GoalWidthMm / f.ScaleMmToPix + 0.5);
                    f.GoalBlockWidthPix = (int)((f.FieldLengthZPix - f.GoalWidthPix) / 2 + 0.5);
                    f.GoalBlockDepthPix = f.GoalDepthPix;
                    f.PictureBoxZPix = f.FieldLengthZPix + 2 * f.FieldInnerBorderPix + 2 * f.FieldOuterBorderPix;

                    f.ForbiddenZoneLengthXPix = (int)(f.ForbiddenZoneLengthXMm / f.ScaleMmToPix + 0.5);
                    f.ForbiddenZoneLengthZPix = (int)(f.ForbiddenZoneLengthZMm / f.ScaleMmToPix + 0.5);
                    f.CenterCircleRadiusPix = (int)(f.CenterCircleRadiusMm / f.ScaleMmToPix + 0.5);

                    int iSubtractScreenHeightAndPictureBoxZ = ScreenHeight - f.PictureBoxZPix;
                    picMatch.Location = new Point(0, iSubtractScreenHeightAndPictureBoxZ / 2);
                    picMatch.Width = f.PictureBoxXPix;
                    picMatch.Height = f.PictureBoxZPix;

                    tabServerControlBoard.Visible = false;
                }
            }
            else // 当前是全屏状态则恢复到正常窗口状态
            {
                //int ScreenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;    // 显示器宽度
                //int ScreenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;  // 显示器高度 
                double dRatioPictureBoxWidthAndHeight = (double)(ScreenWidth - f.TabControlLengthXPix - 2 * f.FormPaddingPix
                    - f.SpanBetweenFieldAndTabControlPix - 10) / (double)(f.TabControlLengthZPix);
                double dRatioFieldXMmAndZMm = (double)(f.FieldLengthXMm) / (double)(f.FieldLengthZMm);
                if (dRatioFieldXMmAndZMm <= dRatioPictureBoxWidthAndHeight)
                {
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.WindowState = FormWindowState.Normal;

                    f.FieldLengthZPix = f.TabControlLengthZPix - 2 * f.FieldInnerBorderPix - 2 * f.FieldOuterBorderPix;
                    f.ScaleMmToPix = (double)f.FieldLengthZMm / (double)f.FieldLengthZPix;
                    f.FieldLengthXPix = (int)(f.FieldLengthXMm / f.ScaleMmToPix + 0.5);
                    f.GoalDepthPix = (int)(f.GoalDepthMm / f.ScaleMmToPix + 0.5);
                    f.GoalWidthPix = (int)(f.GoalWidthMm / f.ScaleMmToPix + 0.5);
                    f.GoalBlockWidthPix = (int)((f.FieldLengthZPix - f.GoalWidthPix) / 2 + 0.5);
                    f.GoalBlockDepthPix = f.GoalDepthPix;

                    f.PictureBoxXPix = f.FieldLengthXPix + 2 * f.FieldInnerBorderPix + 2 * f.FieldOuterBorderPix;
                    f.PictureBoxZPix = f.FieldLengthZPix + 2 * f.FieldInnerBorderPix + 2 * f.FieldOuterBorderPix;
                    f.FormLengthXPix = f.TabControlLengthXPix + f.FieldLengthXPix + f.SpanBetweenFieldAndTabControlPix
                        + 2 * f.FieldInnerBorderPix + 2 * f.FieldOuterBorderPix + 2 * f.FormPaddingPix;
                    f.FormLengthZPix = f.TabControlLengthZPix + 2 * f.FormPaddingPix;
                    f.ForbiddenZoneLengthXPix = (int)(f.ForbiddenZoneLengthXMm / f.ScaleMmToPix + 0.5);
                    f.ForbiddenZoneLengthZPix = (int)(f.ForbiddenZoneLengthZMm / f.ScaleMmToPix + 0.5);
                    f.CenterCircleRadiusPix = (int)(f.CenterCircleRadiusMm / f.ScaleMmToPix + 0.5);

                    picMatch.Location = new Point(10, 10);
                }
                else
                {
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.WindowState = FormWindowState.Normal;

                    f.PictureBoxXPix = ScreenWidth - f.TabControlLengthXPix - 2 * f.FormPaddingPix - f.SpanBetweenFieldAndTabControlPix - 10;
                    f.FieldLengthXPix = f.PictureBoxXPix - 2 * f.FieldOuterBorderPix - 2 * f.FieldInnerBorderPix;
                    f.ScaleMmToPix = (double)(f.FieldLengthXMm) / (double)(f.FieldLengthXPix);
                    f.FieldLengthZPix = (int)(f.FieldLengthZMm / f.ScaleMmToPix + 0.5);
                    f.GoalDepthPix = (int)(f.GoalDepthMm / f.ScaleMmToPix + 0.5);
                    f.GoalWidthPix = (int)(f.GoalWidthMm / f.ScaleMmToPix + 0.5);
                    f.GoalBlockWidthPix = (int)((f.FieldLengthZPix - f.GoalWidthPix) / 2 + 0.5);
                    f.GoalBlockDepthPix = f.GoalDepthPix;
                    f.PictureBoxZPix = f.FieldLengthZPix + 2 * f.FieldInnerBorderPix + 2 * f.FieldOuterBorderPix;

                    f.FormLengthXPix = f.TabControlLengthXPix + f.FieldLengthXPix + f.SpanBetweenFieldAndTabControlPix
                        + 2 * f.FieldInnerBorderPix + 2 * f.FieldOuterBorderPix + 2 * f.FormPaddingPix;
                    f.FormLengthZPix = f.TabControlLengthZPix + 2 * f.FormPaddingPix;
                    f.ForbiddenZoneLengthXPix = (int)(f.ForbiddenZoneLengthXMm / f.ScaleMmToPix + 0.5);
                    f.ForbiddenZoneLengthZPix = (int)(f.ForbiddenZoneLengthZMm / f.ScaleMmToPix + 0.5);
                    f.CenterCircleRadiusPix = (int)(f.CenterCircleRadiusMm / f.ScaleMmToPix + 0.5);

                    int iSubtractFormLengthZPixAndPictureBoxZPix = f.FormLengthZPix - f.PictureBoxZPix;
                    picMatch.Location = new Point(10, iSubtractFormLengthZPixAndPictureBoxZPix / 2);
                }
                // 重置主窗口尺寸 LiYoubing 20110810
                this.ClientSize = new Size(f.FormLengthXPix, f.FormLengthZPix);
                tabServerControlBoard.Visible = true;
                // 重置控制面板位置 LiYoubing 20110810
                tabServerControlBoard.Location = new Point(f.FormPaddingPix + 2 * f.FieldOuterBorderPix 
                    + 2 * f.FieldInnerBorderPix + f.FieldLengthXPix + f.SpanBetweenFieldAndTabControlPix, 
                    f.FormPaddingPix);
                //this.TopMost = false;
            }

            f.ScaleMmToPixX = f.ScaleMmToPix;
            f.ScaleMmToPixZ = f.ScaleMmToPix;
            f.FieldCenterXPix = f.FieldOuterBorderPix + f.FieldInnerBorderPix + f.FieldLengthXPix / 2;
            f.FieldCenterZPix = f.FieldOuterBorderPix + f.FieldInnerBorderPix + f.FieldLengthZPix / 2;

            picMatch.Width = f.PictureBoxXPix;
            picMatch.Height = f.PictureBoxZPix;

            if (picMatch.BackgroundImage != null)
            {
                picMatch.BackgroundImage.Dispose();
            }
            Bitmap bmp = URWPGSim2D.Gadget.WaveEffect.Instance().GetBitmap();
            picMatch.BackgroundImage = f.Draw(bmp, MyMission.Instance().ParasRef.IsGoalBlockNeeded, MyMission.Instance().ParasRef.IsFieldInnerLinesNeeded);
            //picMatch.Image = MyMission.Instance().IMissionRef.Draw();
            SetMatchBmp(MyMission.Instance().IMissionRef.Draw());
            #endregion
        }

        // Weiqingdan
        /// <summary>
        /// 响应界面操作热键ESC/Alt+P/Alt+C/AltS/Alt+R/Alt+Y/Alt+E/Alt+I/Alt+V
        /// </summary>
        private void ServerControlBoard_KeyDown(object sender, KeyEventArgs e)
        {
            MyMission myMission = MyMission.Instance();
            if (this.WindowState == FormWindowState.Maximized)
            {// 全屏模式
                if (e.KeyCode == Keys.Escape)
                {// 响应ESC键退出全屏模式
                    picMatch_DoubleClick(this, null);
                }
            }
            if (e.KeyCode == Keys.P && e.Modifiers == Keys.Alt 
                && btnPause.Enabled == true && myMission.ParasRef.IsRunning == true
                && myMission.ParasRef.IsPaused == false)
            {// Pause/Continue按钮可用时响应Alt+P热键
                btnPause_Click(null, null);
            }
            else if (e.KeyCode == Keys.C && e.Modifiers == Keys.Alt
                && btnPause.Enabled == true && myMission.ParasRef.IsRunning == true
                && myMission.ParasRef.IsPaused == true)
            {// Pause/Continue按钮可用时响应Alt+C热键
                btnPause_Click(null, null);
            }
            else if (e.KeyCode == Keys.S && e.Modifiers == Keys.Alt && btnStart.Enabled == true)
            {// Start按钮可用时响应Alt+S热键
                btnStart_Click(null, null);
            }
            else if (e.KeyCode == Keys.R && e.Modifiers == Keys.Alt && btnRestart.Enabled == true)
            {// Restart按钮可用时响应Alt+R热键
                btnRestart_Click(null, null);
            }
            else if (e.KeyCode == Keys.Y && e.Modifiers == Keys.Alt 
                && btnReplay.Enabled == true && _isReplaying == false)
            {// Replay/End按钮可用时响应Alt+Y热键
                btnReplay_Click(null, null);
            }
            else if (e.KeyCode == Keys.E && e.Modifiers == Keys.Alt 
                && btnReplay.Enabled == true && _isReplaying == true)
            {// Replay/End按钮可用时响应Alt+E热键
                btnReplay_Click(null, null);
            }
            else if (e.KeyCode == Keys.I && e.Modifiers == Keys.Alt
                && btnCapture.Enabled == true)
            {// Image按钮可用时响应Alt+I热键
                btnCapture_Click(null, null);
            }
            else if (e.KeyCode == Keys.V && e.Modifiers == Keys.Alt
                && btnRecordVideo.Enabled == true)
            {// Video按钮可用时响应Alt+V热键
                btnRecordVideo_Click(null, null);
            }
            //else if (e.KeyCode == Keys.R && e.Alt && e.Control
            //    && btnReplay.Enabled == true && btnReplay.Text == "Replay(R)")
            //{// Replay/End按钮可用时响应Ctrl+Alt+R热键
            //    btnReplay_Click(null, null);
            //}
            //else if (e.KeyCode == Keys.E && e.Alt && e.Control
            //    && btnReplay.Enabled == true && btnReplay.Text == "End(E)")
            //{// Replay/End按钮可用时响应Ctrl+Alt+E热键
            //    btnReplay_Click(null, null);
            //}
        }
        #endregion

        #region 截图功能
        /// <summary>
        /// 截图功能：声明API函数
        /// </summary>
        /// <param name="hdcDest">目标设备的句柄</param>
        /// <param name="nXDest">目标对象的左上角的X坐标</param>
        /// <param name="nYDest">目标对象的左上角的Y坐标</param>
        /// <param name="nWidth">目标对象的矩形的宽度</param>
        /// <param name="nHeight">目标对象的矩形的长度(高度)</param>
        /// <param name="hdcSrc">源设备的句柄</param>
        /// <param name="nXSrc">源对象的左上角的X坐标</param>
        /// <param name="nYSrc">源对象的左上角的Y坐标</param>
        /// <param name="dwRop">光栅的操作值</param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, System.Int32 dwRop);

        /// <summary>
        /// 截图功能：声明API函数
        /// </summary>
        /// <param name="lpszDriver">驱动名称</param>
        /// <param name="lpszDevice">设备名称</param>
        /// <param name="lpszOutput">无用，可以设为"NULL"</param>
        /// <param name="lpInitData">任意的打印机数据</param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        private static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        /// <summary>
        /// 截图功能：截图按钮点击事件
        /// </summary>
        private void btnCapture_Click(object sender, EventArgs e)
        {
            try
            {
                //IntPtr dc1 = ServerControlBoard.CreateDC("DISPLAY", null, null, (IntPtr)null);
                //Graphics g1 = Graphics.FromHdc(dc1); //由一个指定设备的句柄创建一个新的Graphics对象
                //Bitmap MyImage = new Bitmap(this.Width, this.Height, g1); //根据屏幕大小创建一个与之相同大小的Bitmap对象
                //Graphics g2 = Graphics.FromImage(MyImage); //获得屏幕的句柄
                //IntPtr dc3 = g1.GetHdc(); //获得位图的句柄
                //IntPtr dc2 = g2.GetHdc(); //把当前屏幕捕获到位图对象中
                //BitBlt(dc2, 0, 0, this.Width, this.Height, dc3, this.Location.X, this.Location.Y, 13369376); //把当前屏幕拷贝到位图中
                //g1.ReleaseHdc(dc3); //释放屏幕句柄
                //g2.ReleaseHdc(dc2); //释放位图句柄
                
                // 获取PictrueBox画笔
                Graphics g1 = this.picMatch.CreateGraphics();
                // 创建一个新图片
                Bitmap MyImage = new Bitmap(this.picMatch.Width, this.picMatch.Height);
                // 获取新图片的画笔
                Graphics g2 = Graphics.FromImage(MyImage);
                // 获取它们的句柄
                IntPtr dc1 = g1.GetHdc();
                IntPtr dc2 = g2.GetHdc();
                // 句柄复制
                BitBlt(dc2, 0, 0, picMatch.Width, picMatch.Height, dc1, 0, 0, 13369376);
                // 释放句柄
                g1.ReleaseHdc(dc1);
                g2.ReleaseHdc(dc2);

                string ThePath = Application.StartupPath + "\\Image\\"; // Image目录 
                string FileName = DateTime.Now.Date.ToString("yyyyMMdd") + DateTime.Now.ToString("HHmmss") + ".JPG";
                if (!System.IO.Directory.Exists(ThePath))
                {
                    // 保存截图目录不存在则新建
                    System.IO.Directory.CreateDirectory(ThePath);
                }
                MyImage.Save(ThePath + "\\" + FileName, System.Drawing.Imaging.ImageFormat.Jpeg);

                ////用户自己可以选择图片类型，保存位置，保存名称
                //SaveFileDialog saveFileDialog = new SaveFileDialog();
                //saveFileDialog.RestoreDirectory = true;
                //saveFileDialog.FileName = DateTime.Now.Date.ToString("yyyyMMdd") + DateTime.Now.ToString("HHmmss") + ".JPG";
                //if (saveFileDialog.ShowDialog() == DialogResult.OK)
                //{
                //    //获得文件路径
                //    string localFilePath = saveFileDialog.FileName.ToString();
                //    //获取文件名，不带路径
                //    string filename = localFilePath.Substring(0,localFilePath.LastIndexOf("\\"));
                    
                //    MyImage.Save(localFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                //}
            }
            catch
            {
            }
        }
        #endregion

        #region 屏幕录像功能 LiYoubing 20110806
        /// <summary>
        /// WM_COPYDATA类型的消息所传递的数据类型
        /// </summary>
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
            public string lpData;
            //public IntPtr lpData;
        }

        /// <summary>
        /// API: Sends the specified message to a window or windows
        /// </summary>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message</param>
        /// <param name="Msg">The message to be sent</param>
        /// <param name="wParam">first message parameter: handle to destination window</param>
        /// <param name="lParam">second message parameter: user defined</param>
        /// <returns>Specifies the result of the message processing which depends on the message sent</returns>
        [System.Runtime.InteropServices.DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(int hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);

        /// <summary>
        /// API: Sends the specified message to a window or windows
        /// </summary>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message</param>
        /// <param name="Msg">The message to be sent</param>
        /// <param name="wParam">first message parameter: handle to destination window</param>
        /// <param name="lParam">second message parameter: user defined</param>
        /// <returns>Specifies the result of the message processing which depends on the message sent</returns>
        [System.Runtime.InteropServices.DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(int hWnd, int Msg, int wParam, string lParam);

        /// <summary>
        /// API: Retrieves a handle to the top-level window whose class name and window name 
        /// match the specified strings
        /// </summary>
        /// <param name="lpClassName">The class name or a class atom</param>
        /// <param name="lpWindowName">The window name (the window's title)</param>
        /// <returns>A handle to the window that has the specified class name and window name or NULL</returns>
        [System.Runtime.InteropServices.DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// API: Retrieves a handle to a window whose class name and window name match the specified strings
        /// </summary>
        /// <param name="hWndParent">A handle to the parent window whose child windows are to be searched</param>
        /// <param name="hWndChildAfter">A handle to a child window. The search begins with the next 
        /// child window in the Z order. The child window must be a direct child window of hwndParent, 
        /// not just a descendant window</param>
        /// <param name="lpClass">The class name or a class atom</param>
        /// <param name="lpWindow">The window name (the window's title)</param>
        /// <returns>A handle to the window that has the specified class name and window name or NULL</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        private static extern int FindWindowEx(int hWndParent, int hWndChildAfter, string lpClass, string lpWindow);

        /// <summary>
        /// 屏幕录像功能Video/End按钮响应
        /// </summary>
        private void btnRecordVideo_Click(object sender, EventArgs e)
        {
            // 按下的是Video按钮标志量为true是End按钮则为false
            bool flag = true;
            if (btnRecordVideo.Text.Equals("Video(V)"))
            {
                btnRecordVideo.Text = "End(V)";
            }
            else
            {
                btnRecordVideo.Text = "Video(V)";
                flag = false;
            }

            // 屏幕录像程序URWPGSim2D.Screencast主窗口句柄
            int hWnd = 0;

            #region 处理URWPGSim2D.Screencast程序启动事宜
            // 查找屏幕录像程序URWPGSim2D.Screencast主窗口获取窗口句柄
            hWnd = FindWindow(null, "URWPGSim2D.Screencast");
            if (hWnd == 0)
            {// Screencast主窗口句柄没有获取到说明Screencast尚未启动
                if (flag == false)
                {// 按下的是End按钮则提示当前不在录像
                    MessageBox.Show("URWPGSim2D.Screencast is not running", 
                        "Confirm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = Application.StartupPath + "\\URWPGSim2D.Screencast.exe";
                try
                {
                    process.Start();
                    // 暂停1秒使得URWPGSim2D.Screencast启动完准备好接收WM_COPYDATA消息
                    System.Threading.Thread.Sleep(1000);
                }
                catch
                {
                    //MessageBox.Show(process.StartInfo.FileName + "\ncan not be booted.\nPlease check it.", 
                    //    "Confirm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                hWnd = FindWindow(null, "URWPGSim2D.Screencast");
                if (hWnd == 0)
                {
                    MessageBox.Show(process.StartInfo.FileName + "\ncan not be booted.\nPlease check it.", 
                        "Confirm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            #endregion

            #region 构造屏幕录像默认文件名
            // 屏幕录像默认保存目录
            string strFilePath = Application.StartupPath + "\\Video";
            if (!System.IO.Directory.Exists(strFilePath))
            {
                // 屏幕录像默认保存目录不存在则新建
                System.IO.Directory.CreateDirectory(strFilePath);
            }
            string strFileName = "";
            MyMission myMission = MyMission.Instance();
            for (int i = 0; i < myMission.ParasRef.TeamCount; i++)
            {// 使用队伍名称拼接屏幕录像文件名
                strFileName += myMission.TeamsRef[i].Para.Name;
                if (i < myMission.ParasRef.TeamCount - 1)
                {
                    strFileName += ".";
                }
            }
            // 若队伍名称为空则使用默认名称test
            strFileName = string.IsNullOrEmpty(strFileName) ? "test." : strFileName + ".";
            strFileName = myMission.ParasRef.Name + "." + strFileName;
            DateTime now = DateTime.Now;
            strFileName += string.Format("{0:0000}{1:00}{2:00}{3:00}{4:00}{5:00}.wmv",
                now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            #endregion

            #region 设置通过WM_COPYDATA消息发送的string数据
            COPYDATASTRUCT cds = new COPYDATASTRUCT();
            if (flag == true)
            {// 按下的是Video按钮标志量为1
                cds.dwData = (IntPtr)1;
                // 屏幕录像区域坐标和尺寸取picMatch在整个屏幕上的绝对坐标和尺寸
                // 两个坐标值和两个尺寸值共4个整数均格式化成5位十进制数字字符串
                // 当前URWPGSim2D程序主窗口句柄格式化成10位十进制数字字符串
                // 传给Screencast用于获取URWPGSim2D主窗口所在的显示器屏幕
                Point startPoint = picMatch.PointToScreen(new Point(0, 0));
                cds.lpData = "";
                // 起始位置坐标在多屏幕条件下可能为负
                // 格式化成1个符号位（正号或符号）加5个数字位
                //if (startPoint.X >= 0)
                //{ cds.lpData += string.Format("{0:+00000}", startPoint.X); }
                //else
                //{ cds.lpData += string.Format("{0:00000}", startPoint.X); }
                //if (startPoint.Y >= 0)
                //{ cds.lpData += string.Format("{0:+00000}", startPoint.Y); }
                //else
                //{ cds.lpData += string.Format("{0:00000}", startPoint.Y); }
                cds.lpData += string.Format("{0:+00000;-00000}{1:+00000;-00000}{2:000000}{3:000000}{4:0000000000}{5}", 
                    startPoint.X, startPoint.Y, picMatch.Width, picMatch.Height, (int)(this.Handle), 
                    strFilePath + "\\" + strFileName);
                cds.cbData = System.Text.Encoding.Default.GetBytes(cds.lpData).Length + 1;
            }
            else
            {// 按下的是End按钮标志量为0
                cds.dwData = IntPtr.Zero;
            }
            #endregion

            //const int WM_GETTEXT = 0x000D;
            //const int WM_CLICK = 0x00F5;
            //const int WM_SETTEXT = 0x000C;
            //int hWndEdit = 0;
            //hWndEdit = FindWindowEx(hWnd, 0, "TextBox", null);
            //if (hWndEdit != 0)
            //{
            //    SendMessage(hWndEdit, WM_SETTEXT, 0, startPoint.X.ToString());
            //}

            // WM_COPYDATA类型的消息在系统里的代码为0x004A
            const int WM_COPYDATA = 0x004A;
            SendMessage(hWnd, WM_COPYDATA, 0, ref cds);
            if (flag == false)
            {// 按下的是End按钮则等待URWPGSim2D.Screencast退出再提示
                // 暂停1秒使得URWPGSim2D.Screencast接收WM_COPYDATA消息后关闭完毕
                System.Threading.Thread.Sleep(1000);
                MessageBox.Show("Screencasting ended\nURWPGSim2D.Screencast exited successfully", 
                    "Confirm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion

        #region 仿真机器鱼和仿真水球等对象的动态信息显示功能 Liushu
        // added by Liushu 20101128
        /// <summary>
        /// 动态信息显示保存功能：实时显示和保存仿真场地上各动态对象（仿真机器鱼和仿真水球）运动学参数
        /// </summary>
        private void btnDisplayFishInfo_Click(object sender, EventArgs e)
        {
            if (dlgFishInfo == null || dlgFishInfo.IsDisposed)
            {
                dlgFishInfo = new DlgFishInfo();
                dlgFishInfo.SetFishInfo();
            }

            dlgFishInfo.Show();
            dlgFishInfo.Focus();
        }

        // Liushu 20101128
        /// <summary>
        /// 处理当前运行着的使命中动态对象（仿真机器鱼和仿真水球）参数集中显示任务
        /// </summary>
        /// <param name="displayFishInfo"></param>
        public void ProcessFishInfoDisplaying()
        {
            if ((dlgFishInfo != null) && (dlgFishInfo.IsDisposed == false))
            {
                MyMission.Instance().ParasRef.DisplayingCycles++;
                if ((MyMission.Instance().ParasRef.DisplayingCycles % dlgFishInfo.step) == 0)
                {
                    //如果DisplayingCycles增长到与用户界面所选择的更新步长step相等时，令其为0。20101129
                    MyMission.Instance().ParasRef.DisplayingCycles = 0;
                    dlgFishInfo.SetFishInfo();
                    if (dlgFishInfo.writeExcelDemo != null)
                    {
                        dlgFishInfo.writeExcelDemo.FishInfoWriteToExcel();
                    }
                }
            }
        }
        #endregion

        #region 仿真机器鱼和仿真水球等对象的轨迹绘制功能 Weiqingdan
        // Weiqingdan 20101127
        /// <summary>
        /// 绘制轨迹功能：绘制当前使命仿真机器鱼和仿真水球的轨迹
        /// </summary>
        private void btnDrawTrajectory_Click(object sender, EventArgs e)
        {
            if (dlgTrajectory == null || dlgTrajectory.IsDisposed)
            {
                dlgTrajectory = new DlgTrajectory();
            }
            dlgTrajectory.Show();
            dlgTrajectory.Focus();
        }

        /// <summary>
        /// 处理当前运行着的仿真使命中动态对象（仿真机器鱼和仿真水球）轨迹绘制任务
        /// </summary>
        public void ProcessTrajectoryDrawing()
        {
            if ((dlgTrajectory != null) && (dlgTrajectory.IsDisposed == false))
            {
                MyMission myMission = MyMission.Instance();
                for (int i = 0; i < myMission.TeamsRef.Count; i++)
                {
                    for (int j = 0; j < myMission.TeamsRef[i].Fishes.Count; j++)
                    {
                        RoboFish f = MyMission.Instance().TeamsRef[i].Fishes[j];
                        myMission.TeamsRef[i].Fishes[j].TrajectoryPoints.Add(
                            Field.Instance().MmToPix(new Point((int)f.PositionMm.X, (int)f.PositionMm.Z)));
                    }
                }
                for (int i = 0; i < myMission.EnvRef.Balls.Count; i++)
                {// LiYoubing 20110104 修正null reference bug
                    myMission.EnvRef.Balls[i].TrajectoryPoints.Add(
                        Field.Instance().MmToPix(new Point((int)myMission.EnvRef.Balls[i].PositionMm.X,
                            (int)myMission.EnvRef.Balls[i].PositionMm.Z)));
                }
                dlgTrajectory.DrawPictureBox();
            }
        }
        #endregion

        private void ServerControlBoard_Load(object sender, EventArgs e)
        {
            // 动态生成窗体和picMatch的长宽，计算的顺序不可改变（可能与属性设置有关） weiqingdan20101105
            Field f = Field.Instance();
            this.Width = f.FormLengthXPix + 10;     // 10为窗体两侧边框的像素数
            this.Height = f.FormLengthZPix + 32;    // 42为窗体上下边框的像素数
            int iSubtractFormLengthZPixAndPictureBoxZPix = f.FormLengthZPix - f.PictureBoxZPix;
            picMatch.Location = new Point(f.FormPaddingPix, iSubtractFormLengthZPixAndPictureBoxZPix / 2);
            picMatch.Width = f.PictureBoxXPix;
            picMatch.Height = f.PictureBoxZPix;

            // 水波组件初始化
            InitWaveEffect();
            // 启动水波组件 LiYoubing 20110726
            chkWaveEffect.Checked = false; // 默认关闭 20110823
            // 通过复选按钮的选中和取消状态控制水波组件的启用和禁用
            wave_timer.Enabled = chkWaveEffect.Checked;
            fish_timer.Enabled = chkWaveEffect.Checked;

            Bitmap bmp = URWPGSim2D.Gadget.WaveEffect.Instance().GetBitmap();
            // 将比赛场地作为绘图控件PictureBox的背景图片
            MissionCommonPara para = MyMission.Instance().ParasRef;
            if (para == null)
            {
                picMatch.BackgroundImage = Field.Instance().Draw(bmp, false, false);
            }
            else
            {
                picMatch.BackgroundImage = Field.Instance().Draw(bmp, para.IsGoalBlockNeeded, para.IsFieldInnerLinesNeeded);
            }
            //picMatch.BackgroundImage = f.Draw(bmp, MyMission.Instance().ParasRef.IsGoalBlockNeeded, MyMission.Instance().ParasRef.IsFieldInnerLinesNeeded);

            cmbCompetitionTime.SelectedIndex = 0;
            cmbCompetitionItem.SelectedIndex = 0;
        }

        #region Field选项卡中Field Setting组仿真场地长度输入框自定义组合框控件的相关事件响应 LiYoubing 20110627
        // LiYoubing 20110627
        void cmbFieldLength_Validated(object sender, EventArgs e)
        {
            int length = (int)Convert.ToSingle(cmbFieldLength.Text);
            int min = (int)Convert.ToSingle(cmbFieldLength.Items[0].ToString());
            if (length < min)
            {// 输入的场地长度小于规定的最小长度（列表的第0个选项值）
                MessageBox.Show(string.Format("Given length {0} mm is less than minimum length {1} mm.\n Canceled.", length, min), 
                    "URWPGSim2DServer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbFieldLength.SelectedIndex = 0;
                return;
            }

            cmbFieldLength_SelectedIndexChanged(null, null);
        }

        // LiYoubing 20110627
        private void cmbFieldLength_SelectedIndexChanged(object sender, EventArgs e)
        {
            Field f = Field.Instance();

            int length = (int)Convert.ToSingle(cmbFieldLength.Text);
            // 可以设置的场地长度没有变化则不必执行后续一系列与场地尺寸有关的更新操作 LiYoubing 20110726
            if (length == f.FieldLengthXMm) { return; }

            cmbFieldWidth.Text = (length * f.FieldLengthZOriMm / f.FieldLengthXOriMm).ToString();
            f.FieldLengthXMm = (int)Convert.ToSingle(cmbFieldLength.Text);
            f.FieldLengthZMm = (int)Convert.ToSingle(cmbFieldWidth.Text);
            
            // 根据设定的场地长度和宽度重新计算其他需要计算的场地参数
            f.FieldCalculation();
            MyMission.Instance().IMissionRef.ResetTeamsAndFishes();
            MyMission.Instance().IMissionRef.ResetBalls();
            MyMission.Instance().IMissionRef.ResetObstacles();

            Bitmap bmp = URWPGSim2D.Gadget.WaveEffect.Instance().GetBitmap();
            // 重绘仿真场地
            SetFieldBmp(f.Draw(bmp, MyMission.Instance().ParasRef.IsGoalBlockNeeded, MyMission.Instance().ParasRef.IsFieldInnerLinesNeeded));

            // 重绘仿真使命场景
            SetMatchBmp(MyMission.Instance().IMissionRef.Draw());

            //// 模拟切换一次TabControl的选项卡解决改变场地大小后第一次双击场地不能全屏问题
            //// 闪动厉害，不采用
            //tabServerControlBoard.SelectedIndex = -1;
            //tabServerControlBoard.SelectedIndex = 2;
        }
        #endregion

        //added by Liushu 20101121
        /// <summary>
        /// Server收到Ready消息后，处理并将结果显示到用户界面。
        /// </summary>
        /// <param name="teamId">当前消息来源客户端代表的队伍在服务端TeamsRef列表中的编号</param>
        public void SetTeamReadyState(int teamId)
        {
            _listTeamStrategyControls[teamId].BtnReady.Enabled = false;

            if (btnPause.Enabled == false)  //modified by liushu 20110308
            {
                bool flag = false;
                for (int i = 0; i < _listTeamStrategyControls.Count; i++)
                {// 只要还有Ready按钮尚未按下则flag为true
                    flag = flag || _listTeamStrategyControls[i].BtnReady.Enabled;
                }
                if (flag == false)
                {// 所有Ready按钮均已按下则可以Start
                    btnStart.Enabled = true;
                }
            }
            else 
            {
                btnStart.Enabled = false;
            }

            btnRestart.Enabled = true;
        }

        /// <summary>
        /// 重置随使命运行动态变化的私有变量值和控件状态
        /// </summary>
        public void ResetPrivateVarsAndControls()
        {
            rdoRemote.Focus();  // 让焦点停留到rdoRemote控件上

            StateRestart();

            // 重置回放相关变量
            _curCachingIndex = 0;
            _curReplayingIndex = 0;
            _isReplaying = false;
            
            // 重置可能处于不同状态的按钮
            btnPause.Text = "Pause(P)";
            btnReplay.Text = "Replay(Y)";

            // 重置运行模式为Remote模式
            MyMission.Instance().IsRomteMode = true;
            rdoRemote.Checked = true;
            _isThereTeamReady = false;

            if ((dlgFishInfo != null) && (dlgFishInfo.IsDisposed == false))
            {// 如果动态对象运动学参数实时显示窗口是打开的则重启它
                dlgFishInfo.Dispose();
                btnDisplayFishInfo_Click(btnDisplayFishInfo, new EventArgs());
            }

            if ((dlgTrajectory != null) && (dlgTrajectory.IsDisposed == false))
            {// 如果动态对象轨迹绘制窗口是打开的则重启它
                dlgTrajectory.Dispose();
                btnDrawTrajectory_Click(btnDrawTrajectory, new EventArgs());
            }
        }

        /// <summary>
        /// 向Server服务发送CLOSED消息以使其Stop Service并Shutdown Dss Node
        /// </summary>
        private void ServerControlBoard_FormClosed(object sender, FormClosedEventArgs e)
        {
            //FromServerUiEvents.Instance().Post(new FromServerUiMsg(FromServerUiMsg.MsgEnum.CLOSED, new string[] { }));
            // 如果正在使用Excel保存报表数据则先关闭之
            if ((dlgFishInfo != null) && (dlgFishInfo.writeExcelDemo != null))
            {
                dlgFishInfo.writeExcelDemo.CloseExcel();
            }
            Process.GetCurrentProcess().Kill(); // 没有数据需要保存直接结束当前进程
        }

        /// <summary>
        /// 向用户端显示的对话框提示信息  20101215
        /// </summary>
        /// <param name="dialogInfo"></param>
        public void ShowDialogInfo(string dialogInfo)
        {
            MessageBox.Show(string.Format("{0}", dialogInfo), "Confirm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        #region 水波组件 Solesschong 20110712
         /*=======================================水波组件原理============================================*
        / * FastBitmap是水波组件内部的数据结构，具体作用不详。。。 
          * 水波效果源程序被封装在WaveEffect类中
          *      对外接口为 LoadBmp, DropWater, PaintWater, GetBitmap. 
          *      LoadBmp是初始化方法，
          *      GetBitmap是组件对外的输出。 
          * 组件工作流程
          *      首先用一张图片初始化，之后根据鱼的位置用DropWater方法不断为水面添加水波源，
          *      再以一定频率调用PaintWater，刷新水面图片，
          *      并把GetBitmap返回的图片交付Field.Draw()处理，
          *      返回值赋给picMatch.BackgroundImage。
          * /=============================================================================================*
          */

        /// <summary>
        /// 水波效果开关 LiYoubing 20110726
        /// </summary>
        private void chkWaveEffect_CheckedChanged(object sender, EventArgs e)
        {
            // 通过复选按钮的选中和取消状态控制水波组件的启用和禁用
            wave_timer.Enabled = chkWaveEffect.Checked;
            fish_timer.Enabled = chkWaveEffect.Checked;

            // 水波组件初始化
            InitWaveEffect();
            // 重绘背景
            Bitmap bmp = URWPGSim2D.Gadget.WaveEffect.Instance().GetBitmap();
            MissionCommonPara para = MyMission.Instance().ParasRef;
            if (para == null)
            {
                picMatch.BackgroundImage = Field.Instance().Draw(bmp, false, false);
            }
            else
            {
                picMatch.BackgroundImage = Field.Instance().Draw(bmp, para.IsGoalBlockNeeded, para.IsFieldInnerLinesNeeded);
            }
            picMatch.Invalidate();
        }

        /// <summary>
        /// 水波组件初始化
        /// </summary>
        private void InitWaveEffect()
        {
            if (picLoader.Image != null) { picLoader.Image.Dispose(); }

            picLoader.Image = new Bitmap(Application.StartupPath + "\\URWPGSim2D.FieldBackground.bmp");

            WaveEffect.Instance().LoadBmp((Bitmap)picLoader.Image);
        }
        /// <summary>
        /// 封装的产生水波过程
        /// bool标示是否为概率模式
        /// </summary>
        /// <param name="IsPossibility"></param>
        private void DropAllFish(bool IsPossibility)
        {
            int p = 0;
            double size = 4.0 / Field.Instance().ScaleMmToPix;
            foreach (Ball ball in MyMission.Instance().EnvRef.Balls)
            {
                int x, z;
                x = (int)ball.PositionMm.X;
                z = (int)ball.PositionMm.Z;
                x = Field.Instance().MmToPixX((int)x);
                z = Field.Instance().MmToPixZ((int)z);
                WaveEffect w = WaveEffect.Instance();
                // 产生水波的概率
                double v = Math.Sqrt((ball.PositionMm.X - ball.PrePositionMm.X) * (ball.PositionMm.X - ball.PrePositionMm.X) 
                    + (ball.PositionMm.Z - ball.PrePositionMm.Z) * (ball.PositionMm.Z - ball.PrePositionMm.Z));
                if (IsPossibility)
                {
                    if (v > 9 && v < 320)
                    {
                        p = (int)(WaveEffect.Instance().GetRandom((int)v * 400));
                    }
                    else
                    {
                        p = w.GetRandom(1000);
                    }
                }
                else  // (!IsPossibility) 非概率模式
                {
                    p = 1000;
                }
                if (p > 900)
                {
                    w.DropWater(x, z, (int)(size * (25 + w.GetRandom(5))), 10 + w.GetRandom(50));
                }
            }

            foreach (Team<RoboFish> team in MyMission.Instance().TeamsRef)
            {
                foreach (RoboFish fish in team.Fishes)
                {
                    // 获得鱼位置
                    int x, z, len; float dir;
                    x = (int)fish.PositionMm.X;
                    z = (int)fish.PositionMm.Z;
                    len = (int)(fish.BodyLength / Field.Instance().ScaleMmToPix);
                    dir = fish.BodyDirectionRad;
                    x = Field.Instance().MmToPixX((int)x);
                    z = Field.Instance().MmToPixZ((int)z);
                    WaveEffect w = WaveEffect.Instance();

                    // 产生水波的概率
                    double v = Math.Sqrt((fish.PositionMm.X - fish.PrePositionMm.X) * (fish.PositionMm.X - fish.PrePositionMm.X) 
                        + (fish.PositionMm.Z - fish.PrePositionMm.Z) * (fish.PositionMm.Z - fish.PrePositionMm.Z));
                    int unitlen = 10;
                    if (IsPossibility)
                    {
                        if (v > 9 && v < 320)
                        {
                            p = (int)(WaveEffect.Instance().GetRandom((int)v * 400));
                        }
                        else
                        {
                            p = w.GetRandom(1000);
                        }
                    }
                    else  // (!IsPossibility) 非概率模式
                    {
                        p = 1000;
                    }
                    if (p > 900)
                    {
                        for (int i = -len / 2; i < len / 2; i += unitlen)
                        {
                            if (WaveEffect.Instance().GetRandom(2) < 1)
                            {
                                w.DropWater(x - (int)(Math.Cos(dir) * i), z - (int)(Math.Sin(dir) * i),
                                    (int)(size * (10 + w.GetRandom(5))), 10 + w.GetRandom(400));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 定时刷新水波图片,集成水波扩散 
        /// 速度有待提高
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wave_timer_Tick(object sender, EventArgs e)
        {
            // 暂停之后一般有鱼体位置突变，需要初始化组件
            if (MyMission.Instance().ParasRef.IsPaused)
                InitWaveEffect();

            WaveEffect.Instance().PaintWater();
            Bitmap bmp = URWPGSim2D.Gadget.WaveEffect.Instance().GetBitmap();
            picMatch.BackgroundImage = Field.Instance().Draw(bmp, MyMission.Instance().ParasRef.IsGoalBlockNeeded, MyMission.Instance().ParasRef.IsFieldInnerLinesNeeded);
            picMatch.Invalidate();
        }

        /// <summary>
        /// 定时产生水波源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fish_timer_Tick(object sender, EventArgs e)
        {
            DropAllFish(true);
            // 让产生水波的时间不确定，效果更真实
            fish_timer.Interval = 500 + WaveEffect.Instance().GetRandom(300);
        }
        #endregion

        #region 背景音乐功能 LiYoubing 20110711
        /// <summary>
        /// 播放背景音乐用的播放器对象
        /// </summary>
        AudioPlayer _player = new AudioPlayer();

        /// <summary>
        /// 当前已经选择的背景音乐完整文件名(含路径)
        /// </summary>
        string _strMusicFullName = "";

        /// <summary>
        /// 背景音乐浏览按钮点击事件响应 弹出文件选择对话框 选择背景音乐文件
        /// </summary>
        private void btnMusicBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "audio files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*";  // 过滤文件类型
            openFileDialog.ShowReadOnly = true; // 设定文件是否只读
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtMusicName.Text = openFileDialog.SafeFileName;    // 获取文件名
                _strMusicFullName = openFileDialog.FileName;        // 获取包含完整路径的文件名
            }
        }

        /// <summary>
        /// 背景音乐播放按钮点击事件响应
        /// 开始循环播放背景音乐
        /// </summary>
        private void btnMusicPlay_Click(object sender, EventArgs e)
        {
            _player.FileName = _strMusicFullName;
            _player.Play();
            btnMusicPause.Enabled = true;
            btnMusicStop.Enabled = true;
        }

        /// <summary>
        /// 背景音乐暂停/继续按钮点击事件响应
        /// 暂停或继续播放背景音乐
        /// </summary>
        private void btnMusicPause_Click(object sender, EventArgs e)
        {
            if (btnMusicPause.Text.Equals("Pause"))
            {
                btnMusicPause.Text = "Resume";
                _player.Pause();
            }
            else
            {
                btnMusicPause.Text = "Pause";
                _player.Resume();
            }
        }

        /// <summary>
        /// 背景音乐停止按钮点击事件响应
        /// 停止播放背景音乐
        /// </summary>
        private void btnMusicStop_Click(object sender, EventArgs e)
        {
            _player.Stop();
            btnMusicPause.Enabled = false;
            btnMusicStop.Enabled = false;
            btnMusicPause.Text = "Pause";
        }

        /// <summary>
        /// 鼠标悬停在背景音乐名称显示框上时显示背景音乐完整文件名（含路径）
        /// </summary>
        private void txtMusicName_MouseHover(object sender, EventArgs e)
        {
            // 在鼠标滑上txtMusicNam时，将存储的背景音乐文件完整路径显示出来
            ToolTip tip = new ToolTip();
            tip.SetToolTip(txtMusicName, _strMusicFullName);
        }

        #endregion
    }
}