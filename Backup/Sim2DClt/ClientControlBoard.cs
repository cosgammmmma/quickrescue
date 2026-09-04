using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics; // for Process

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;

using URWPGSim2D.Common;
using URWPGSim2D.StrategyLoader;

namespace URWPGSim2D.Sim2DClt
{
    public partial class ClientControlBoard : Form
    {

        public ClientControlBoard(FromClientUiEvents port)
        {
            InitializeComponent();

            // 传入客户端服务实例中创建的FromWinformEvents端口的引用
            _fromClientUiPort = port;
        }

        /// <summary>
        /// 界面给客户端服务实例发送消息的端口
        /// </summary>
        FromClientUiEvents _fromClientUiPort;

        /// <summary>
        /// 策略dll文件完整文件名
        /// </summary>
        string _strStrategyFullName;

        /// <summary>
        /// 用于加载策略组件dll的应用程序域
        /// </summary>
        AppDomain _appDomainForStrategy = null;

        IStrategy _strategyInterface = null;
        /// <summary>
        /// 策略组件dll中Strategy类使用IStrategy接口加载后的实例
        /// </summary>
        public IStrategy StragegyInterface
        {
            get { return _strategyInterface; }
        }

        //added by LiuShu 20101108
        /// <summary>
        /// 点击btnStrategyBrowse按钮加载策略
        /// </summary>
        /// <remarks>加载策略按钮按下时将用户选择的策略保存，同时提取队伍名称，将队伍名称发送给服务端</remarks>
        private void btnStrategyBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "dll files (*.dll)|*.dll|All files (*.*)|*.*";  // 过滤文件类型
            openFileDialog.ShowReadOnly = true; //设定文件是否只读

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtStrategy.Text = openFileDialog.SafeFileName; // 获取文件名
                _strStrategyFullName = openFileDialog.FileName; // 获取包含完整路径的文件名
                string strStrategyCachePath = Application.StartupPath + "\\StrategyCache\\"; // StrategyCache目录
                string strCachedStrategyFullName = strStrategyCachePath + openFileDialog.SafeFileName; // Cache的dll文件
                try
                {
                    if (!System.IO.Directory.Exists(strStrategyCachePath))
                    {
                        // 策略缓存目录不存在则新建
                        System.IO.Directory.CreateDirectory(strStrategyCachePath);
                    }

                    // 如果已经加载过策略dll则先卸载前一次加载策略所使用的应用程序域即可卸载已加载的dll文件
                    if (_appDomainForStrategy != null)
                    {
                        AppDomain.Unload(_appDomainForStrategy);
                        _appDomainForStrategy = null;
                        _strategyInterface = null;
                    }
                    System.IO.File.Copy(_strStrategyFullName, strCachedStrategyFullName, true);
                }
                catch (System.IO.IOException)
                {//当有两支队伍加载同一个策略文件时会出现异常现象，这时
                    strCachedStrategyFullName = strCachedStrategyFullName.Replace(".dll", string.Format(" {0:0000}{1:00}{2:00} {3:00}{4:00}{5:00}.dll",
                        DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                        DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second));

                    System.IO.File.Copy(_strStrategyFullName, strCachedStrategyFullName, true);
                }

                #region 使用AppDomain加载策略组件dll
                _appDomainForStrategy = AppDomain.CreateDomain("Client AppDomain For Strategy");

                // 使用AppDomain创建策略接口工厂类（StrategyInterfaceFactory）实例
                StrategyInterfaceFactory factory = (StrategyInterfaceFactory)_appDomainForStrategy.CreateInstance(
                    "URWPGSim2D.StrategyLoader", typeof(StrategyInterfaceFactory).FullName).Unwrap();

                // 使用策略接口工厂类实例创建策略接口实例
                _strategyInterface = factory.Create(strCachedStrategyFullName, "URWPGSim2D.Strategy.Strategy", null);
                #endregion

                // 将队伍名字显示到lblTeamName上
                lblTeamName.Text = _strategyInterface.GetTeamName();
                btnReady.Enabled = true;

                // 将TeamName传送给Client服务实例使其Announce给Server服务实例
                _fromClientUiPort.Post(new FromClientUiMsg(FromClientUiMsg.MsgEnum.STRATEGY_LOADED, 
                    new string[] {lblTeamName.Text}));
            }
        }

        private void txtStrategy_MouseHover(object sender, EventArgs e)
        {
            // added by LiuShu 20101108
            // 在鼠标滑上txtStrategy时，将存储的策略文件完整路径显示出来
            ToolTip tooltipStrategy = new ToolTip();
            tooltipStrategy.SetToolTip(txtStrategy, _strStrategyFullName); 
        }

        private void ClientControlBoard_Load(object sender, EventArgs e)
        {
            // 界面加载完成时给Client服务实例发送消息使其向Server服务实例发送通知告知Client服务的Uri
            _fromClientUiPort.Post(new FromClientUiMsg(FromClientUiMsg.MsgEnum.UI_LOADED, null));
        }

        private void btnReady_Click(object sender, EventArgs e)
        {
            // added by LiuShu 20101116    
            btnReady.Enabled = false;           // 此时Ready按钮不能再用
            btnStrategyBrowse.Enabled = false;  // 此时加载策略按钮不能再用

            // 当用户按下ready键时，将消息传入_fromClientUiPort，并向Server服务实例发送
            _fromClientUiPort.Post(new FromClientUiMsg(FromClientUiMsg.MsgEnum.READY, null));
        }

        /// <summary>
        /// 响应客户端主界面FormClosed事件 向Client服务发送关闭消息以便其Stop Service并Shutdown Dss Node
        /// </summary>
        private void ClientControlBoard_FormClosed(object sender, FormClosedEventArgs e)
        {
            //_fromClientUiPort.Post(new FromClientUiMsg(FromClientUiMsg.MsgEnum.CLOSED, null));
            Process.GetCurrentProcess().Kill(); // 没有数据需要保存直接结束当前进程
        }

        public void OnCompetitionTypeChanged(string strCompetitionType)
        {
            lblCompetitionType.Text = strCompetitionType;
            txtStrategy.Text = "";
            lblTeamName.Text = "";
            btnStrategyBrowse.Enabled = true;
        }

        public void SetBtnReadyEnabledState(bool state)
        {
            btnReady.Enabled = state;
        }

        public void SetBtnStrategyBrowseEnabledState(bool state)
        {
            btnStrategyBrowse.Enabled = state;
        }

        public void SetRadioTeamState(bool stateA, bool stateB)
        {
            rdoLeft.Checked = stateA;
            rdoRight.Checked = stateB;
        }

        /// <summary>
        /// 设置队伍方位控件状态（仅对两支队伍参与的仿真使命有效） LiYoubing 20120520
        /// </summary>
        /// <param name="stateA">当前客户端代表的仿真机器人队伍是否在场地左边</param>
        /// <param name="stateB">当前客户端代表的仿真机器人队伍是否在场地右边</param>
        /// <param name="color">当前客户端代表的仿真机器人队伍队员鱼体颜色用于设置单选按钮前景色</param>
        public void SetRadioTeamState(bool stateA, bool stateB, Color color)
        {
            SetRadioTeamState(stateA, stateB);
            RadioButton rb = stateA ? rdoLeft : rdoRight;
            rb.ForeColor = color; // 因为rb.Enabled属性为false设置的这个颜色无效 LiYoubing 20120520
        }

        /// <summary>
        /// 收到Server服务发来的RESTART消息后清空队伍名称和策略文件名显示控件 LiYoubing 20120520
        /// </summary>
        public void ClearTeamNameAndStrategyFileName()
        {
            lblTeamName.Text = "";
            txtStrategy.Text = "";
        }

        public void SetTeamId(int teamId)
        {
            lblTeamId.Text = (teamId + 1).ToString();
            btnStrategyBrowse.Enabled = true;   //modified 20101208
        }

        //deleted 20101214
        public void SetCompetitionType(string strCompetitionType)
        {
            lblCompetitionType.Text = strCompetitionType;
        }

        public void DisplayCompetitionState(string strCompetitionState)
        {
            string competitionState = string.Format(
                "{0:0000}-{1:00}-{2:00} {3:00}:{4:00}:{5:00}  {6}",
                DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, strCompetitionState);

            if (txtCompetitionState.Text == "")
            {
                txtCompetitionState.Text = competitionState;
            }
            else
            {
                txtCompetitionState.Text = competitionState + "\r\n" + txtCompetitionState.Text;
            }
        }

        private void txtCompetitionState_MouseHover(object sender, EventArgs e)
        {
            ToolTip toolTip = new ToolTip();
            string strTmp = txtCompetitionState.Text;
            //int i = strTmp.IndexOf('\r');
            //toolTip.SetToolTip(txtCompetitionState, (i > 0) ? strTmp.Substring(0, i + 1) : strTmp);
            toolTip.SetToolTip(txtCompetitionState, strTmp);
        }
    }
}