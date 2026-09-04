//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: SysConfig.cs
// Date: 20101120  Author: LiYoubing  Version: 1
// Description: 界面和处理逻辑交互类及其他杂项类定义文件
// Histroy:
// Date: 20101120  Author: LiYoubing
// Modification: 修改内容简述
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Runtime.CompilerServices; // for MethodImpl
using System.Drawing;
using System.Windows.Forms;

using URWPGSim2D.Common;

namespace URWPGSim2D.StrategyLoader
{
    /// <summary>
    /// 服务端Referee设置界面Strategy Setting块控件组合类
    /// </summary>
    public class TeamStrategyComboControls
    {
        /// <summary>
        /// 策略组件dll文件名显示控件
        /// </summary>
        public TextBox TxtStrategyFileName = new TextBox();

        /// <summary>
        /// 加载策略按钮控件
        /// </summary>
        public Button BtnBrowse = new Button();

        /// <summary>
        /// 确认策略加载完成按钮控件
        /// </summary>
        public Button BtnReady = new Button();

        /// <summary>
        /// 分组容器控件
        /// </summary>
        public GroupBox GrpContainer = new GroupBox();

        /// <summary>
        /// 当前组合控件所代表队伍的编号（0,1...)
        /// </summary>
        public int TeamId = 0;

        /// <summary>
        /// 策略组件dll中Strategy类使用IStrategy接口加载后的实例
        /// </summary>
        public IStrategy StrategyInterface = null;

        public TeamStrategyComboControls(int teamId)
        {
            TeamId = teamId;  // 保存当前组合控件所代表队伍的编号
            GrpContainer.Text = "Team" + (teamId + 1).ToString();
            TxtStrategyFileName.MouseHover += new EventHandler(TxtStrategyFileName_MouseHover);
            BtnBrowse.Click += new EventHandler(BtnBrowse_Click);
            BtnReady.Click += new EventHandler(BtnReady_Click);

            GrpContainer.Controls.Add(TxtStrategyFileName);
            GrpContainer.Controls.Add(BtnBrowse);
            GrpContainer.Controls.Add(BtnReady);

            SetControls();
        }

        private void SetControls()
        {
            GrpContainer.Size = new Size(250, 40);
            TxtStrategyFileName.Size = new Size(110, 20);
            BtnBrowse.Size = new Size(50, 20);
            BtnReady.Size = new Size(50, 20);

            TxtStrategyFileName.Location = new Point(10, 14);
            BtnBrowse.Location = new Point(130, 14);
            BtnReady.Location = new Point(190, 14);

            BtnBrowse.Text = "…";
            BtnBrowse.Name = "Team" + TeamId;   // 用于判断Click事件的来源按钮的id
            BtnReady.Text = "Ready";
            BtnBrowse.UseVisualStyleBackColor = true;
            BtnReady.UseVisualStyleBackColor = true;
            BtnBrowse.Enabled = false;          // 默认Remote模式Browse按钮不可用
            TxtStrategyFileName.ReadOnly = true;
        }

        private void TxtStrategyFileName_MouseHover(object sender, EventArgs e)
        {
            // 在鼠标滑上txtStrategyFileName时，将存储的策略文件完整路径显示出来
            ToolTip tipStrategyFileName = new ToolTip();
            tipStrategyFileName.SetToolTip(TxtStrategyFileName, _strStrategyFullName);
        }

        private void BtnReady_Click(object sender, EventArgs e)
        {
            // 服务端运行于Remote模式时Ready按钮点击无效
            if (MyMission.Instance().IsRomteMode == true) return;
            BtnReady.Enabled = false;
        }

        ///add by caiqiong 20101204
        /// <summary>
        /// 用于加载策略组件dll的应用程序域
        /// </summary>
        public AppDomain _appDomainForStrategy = null;

        /// <summary>
        /// 策略dll文件完整文件名
        /// </summary>
        private string _strStrategyFullName;

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "dll files (*.dll)|*.dll|All files (*.*)|*.*";  // 过滤文件类型
            openFileDialog.ShowReadOnly = true; // 设定文件是否只读
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                TxtStrategyFileName.Text = openFileDialog.SafeFileName; // 获取文件名
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
                    //// 如果已经加载过策略dll则先卸载前一次加载策略所使用的应用程序域即可卸载已加载的dll文件
                    //if (_appDomainForStrategy != null)
                    //{
                    //    AppDomain.Unload(_appDomainForStrategy);
                    //    _appDomainForStrategy = null;
                    //    StrategyInterface = null;
                    //}
                    //System.IO.File.Copy(_strStrategyFullName, strCachedStrategyFullName, true);

                    //added by liushu 20110307
                    if (string.Compare(_strStrategyFullName, strCachedStrategyFullName) != 0)
                    {//如果加载策略时不是直接在StrategyCache目录中添加的
                        if (_appDomainForStrategy != null)
                        {//如果此队伍已经加载过dll，则先卸载前一次加载策略所使用的应用程序域即可卸载之前加载的dll策略文件
                            AppDomain.Unload(_appDomainForStrategy);
                            _appDomainForStrategy = null;
                            StrategyInterface = null;
                        }
                        System.IO.File.Copy(_strStrategyFullName, strCachedStrategyFullName, true);
                    }
                }
                catch (System.IO.IOException)
                {//当有两支队伍加载同一个策略文件时会出现异常现象，这时
                    strCachedStrategyFullName = strCachedStrategyFullName.Replace(".dll", string.Format(" {0:0000}{1:00}{2:00} {3:00}{4:00}{5:00}.dll",
                        DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                        DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second));

                    System.IO.File.Copy(_strStrategyFullName, strCachedStrategyFullName, true);  
                }
                catch
                {
                    MessageBox.Show("策略加载异常");    
                }

                #region 使用AppDomain加载策略组件dll
                _appDomainForStrategy = AppDomain.CreateDomain("Server AppDomain For Strategy" + TeamId);

                // 使用AppDomain创建策略接口工厂类（StrategyInterfaceFactory）实例
                StrategyInterfaceFactory factory = (StrategyInterfaceFactory)_appDomainForStrategy.CreateInstance(
                   "URWPGSim2D.StrategyLoader", typeof(StrategyInterfaceFactory).FullName).Unwrap();

                // 使用策略接口工厂类实例创建策略接口实例
                StrategyInterface = factory.Create(strCachedStrategyFullName, "URWPGSim2D.Strategy.Strategy", null);
                #endregion

                BtnReady.Enabled = true;

                // 半场交换队伍名称显示错误修正 LiYoubing 20110520
                int teamId = TeamId;
                if (MyMission.Instance().ParasRef.IsExchangedHalfCourt == true
                    && MyMission.Instance().ParasRef.TeamCount == 2)
                {
                    teamId = (TeamId + 1) % 2;
                }
                MyMission.Instance().TeamsRef[teamId].Para.Name = StrategyInterface.GetTeamName();
            }
        }

        public void ClearStrategy()
        {
            if (_appDomainForStrategy != null)
            {// 如果已经加载过策略dll则先卸载前一次加载策略所使用的应用程序域即可卸载已加载的dll文件
                AppDomain.Unload(_appDomainForStrategy);
                _appDomainForStrategy = null;
                StrategyInterface = null;
            }
        }
    }
}