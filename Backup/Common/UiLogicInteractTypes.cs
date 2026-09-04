//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: SysConfig.cs
// Date: 20101120  Author: LiYoubing  Version: 1
// Description: 界面和处理逻辑交互类定义文件
// Histroy:
// Date: 20110628  Author: LiYoubing
// Modification:
// 1.增加只允许输入数字的文本框和组合下拉框控件定义
// 2.仿真机器鱼/水球信息显示和设置组合控件中的TextBox全部换成NumberTextBox
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using xna = Microsoft.Xna.Framework;

namespace URWPGSim2D.Common
{
    #region 只允许输入数字的文本框/组合下拉框控件
    // LiYoubing 20110627
    /// <summary>
    /// 只允许输入数字的文本框控件
    /// </summary>
    public class NumberTextBox : TextBox
    {
        private const int WM_CHAR = 0x0102;          // 输入字符消息（键盘输入的，输入法输入的好像不是这个消息）
        private const int WM_PASTE = 0x0302;         // 程序发送此消息给editcontrol或combobox从剪贴板中得到数据

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m) 
        { 
            switch(m.Msg)
            { 
                case WM_CHAR:
                    System.Console.WriteLine(m.WParam);
                    bool isSign = ((int)m.WParam == 45);
                    bool isNum = ((int)m.WParam >= 48) && ((int)m.WParam <= 57);
                    bool isBack = (int)m.WParam == (int)Keys.Back;
                    bool isDelete = (int)m.WParam == (int)Keys.Delete; //实际上这是一个"."键
                    bool isCtr = ((int)m.WParam == 24) || ((int)m.WParam == 22) || ((int)m.WParam == 26) ||((int)m.WParam == 3);

                    if( isNum || isBack || isCtr)
                    {
                        base.WndProc (ref m);
                    }
                    if (isSign)
                    {
                        if (this.SelectionStart!=0)
                        {
                            break;
                        }
                        base.WndProc (ref m);
                        break;
                    }
                    if (isDelete)
                    {
                        if (this.Text.IndexOf(".")<0)
                        {
                            base.WndProc (ref m);
                        }
                    }
                    if ((int)m.WParam == 1)
                    {
                        this.SelectAll();
                    }
                    break;

                case WM_PASTE:
                    IDataObject iData = Clipboard.GetDataObject();  // 取剪贴板对象

                    if(iData.GetDataPresent(DataFormats.Text))      // 判断是否是Text
                    {
                        string str = (string)iData.GetData(DataFormats.Text);   // 取数据
                        if (MatchNumber(str)) 
                        {
                            base.WndProc (ref m);
                            break;
                        }
                    }
                    m.Result = (IntPtr)0;   // 不可以粘贴
                    break;

                default:
                    base.WndProc (ref m);
                    break;
            }
        }

        private bool MatchNumber(string ClipboardText)
        {
            int index=0;
            string strNum = "-0.123456789";

            index = ClipboardText.IndexOf(strNum[0]);
            if (index>=0)
            {
                if (index>0)
                {
                    return false;
                }
                index = this.SelectionStart;
                if (index>0)
                {
                    return false;
                }
            }

            index = ClipboardText.IndexOf(strNum[2]);
            if (index!=-1)
            {
                index = this.Text.IndexOf(strNum[2]);
                if (index!=-1)
                {
                    return false;
                }
            }

            for(int i=0; i<ClipboardText.Length; i++)
            {
                index = strNum.IndexOf(ClipboardText[i]);
                if (index <0)
                {
                    return false;
                }
            }
            return true;
        }
    }

    // LiYoubing 20110627
    /// <summary>
    /// 只允许输入数字的组合框控件
    /// </summary>
    public class NumberComboBox : ComboBox
    {
        private IntPtr _editHandle = IntPtr.Zero;
        /// <summary>
        /// Editor窗口句柄
        /// </summary>
        public IntPtr EditHandle
        {
            get { return _editHandle; }
            set { _editHandle = value; }
        }

        private EditNativeWindow _editNativeWindow = null;
        /// <summary>
        /// Editor窗口的NativeWindow对象
        /// </summary>
        public EditNativeWindow EditNativeWindow
        {
            get { return _editNativeWindow; }
            set { _editNativeWindow = value; }
        }

        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr GetWindow(IntPtr hwnd, int wFlag);

        private const int GW_CHILD = 5;

        /// <summary>
        /// 重载句柄创建完成事件 保存窗体句柄和窗体对象的引用
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            _editHandle = GetWindow(Handle, GW_CHILD);
            if (_editHandle != IntPtr.Zero)
            {
                _editNativeWindow = new EditNativeWindow(this);
            }
        }

        /// <summary>
        /// 重载句柄销毁完成事件 销毁窗体对象释放资源
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            if (_editNativeWindow != null)
            {
                _editNativeWindow.Dispose();
                _editNativeWindow = null;
            }
        }
    }

    // LiYoubing 20110627
    /// <summary>
    /// Editor窗口的NativeWindow类
    /// </summary>
    public class EditNativeWindow : NativeWindow, IDisposable
    {
        private NumberComboBox _owner;

        /// <summary>
        /// 构造函数，给传入的NumberComboBox对象构造一个Native窗口
        /// </summary>
        /// <param name="owner"></param>
        public EditNativeWindow(NumberComboBox owner)
            : base()
        {
            _owner = owner;
            base.AssignHandle(owner.EditHandle);
        }

        private const int WM_PASTE = 0x0302;

        private const int WM_CHAR = 0x0102;

        /// <summary>
        /// 拦截NativeWindow的WM_PASTE和WM_CHAR消息
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_PASTE:
                    IDataObject iData = Clipboard.GetDataObject();
                    if (iData.GetDataPresent(DataFormats.Text))//粘贴的内容是否是文本
                    {
                        string str;
                        str = (String)iData.GetData(DataFormats.Text);
                        if (System.Text.RegularExpressions.Regex.IsMatch(str, @"^(\d{1,})$")) //文本内容是不是数字
                        {
                            break;
                        }
                    }
                    m.Result = IntPtr.Zero;
                    return;
                case WM_CHAR:
                    int keyChar = m.WParam.ToInt32();
                    bool charOk = (keyChar > 47 && keyChar < 58) ||   //数字
                         keyChar == 8 ||                              //退格
                         keyChar == 3 || keyChar == 22 || keyChar == 24;//拷贝,粘贴,剪切
                    if (!charOk)
                    {
                        m.WParam = IntPtr.Zero;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        #region IDisposable 成员
        /// <summary>
        /// 销毁对象
        /// </summary>
        public void Dispose()
        {
            base.ReleaseHandle();
            _owner = null;
        }
        #endregion
    }
    #endregion

    #region 仿真机器鱼/水球信息显示和设置自定义组合控件
    /// <summary>
    /// 服务端Fish设置界面FishSetting块控件组合类
    /// </summary>
    public class FishSettingComboControls
    {
        /// <summary>
        /// 仿真机器鱼选择下拉框控件
        /// </summary>
        public ComboBox CmbPlayers = new ComboBox();

        /// <summary>
        /// 仿真机器鱼前部色标选择按钮控件
        /// </summary>
        public Button BtnFrontColor = new Button();

        /// <summary>
        /// 仿真机器鱼后部色标选择按钮控件
        /// </summary>
        public Button BtnBackColor = new Button();

        /// <summary>
        /// 仿真机器鱼位置坐标X分量显示控件
        /// </summary>
        public TextBox TxtPositionMmX = new NumberTextBox();

        /// <summary>
        /// 仿真机器鱼位置坐标Z分量显示控件
        /// </summary>
        public TextBox TxtPositionMmZ = new NumberTextBox();

        /// <summary>
        /// 仿真机器鱼鱼体方向显示控件（弧度转成角度显示）
        /// </summary>
        public TextBox TxtDirectionDeg = new NumberTextBox();

        /// <summary>
        /// 仿真机器鱼设置确认按钮控件
        /// </summary>
        public Button BtnConfirm = new Button();

        /// <summary>
        /// 分组容器控件
        /// </summary>
        public GroupBox GrpContainer = new GroupBox();

        /// <summary>
        /// 当前组合控件所代表队伍的编号（0,1...)
        /// </summary>
        public int TeamId = 0;

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="teamId">当前对象所代表的仿真使命参与队伍编号（从0开始）</param>
        /// <param name="Fishes">当前对象所代表的仿真使命参与队伍仿真机器鱼数量</param>
        public FishSettingComboControls(int teamId, int Fishes)
        {
            TeamId = teamId;  // 保存当前组合控件所代表队伍的编号
            GrpContainer.Text = "Team" + (teamId + 1).ToString();
            CmbPlayers.SelectedIndexChanged += new EventHandler(CmbPlayers_SelectedIndexChanged);
            BtnConfirm.Click += new EventHandler(BtnConfirm_Click);
            BtnFrontColor.Click += new EventHandler(BtnFrontColor_Click);
            BtnBackColor.Click += new EventHandler(BtnBackColor_Click);

            GrpContainer.Controls.Add(CmbPlayers);
            GrpContainer.Controls.Add(BtnFrontColor);
            GrpContainer.Controls.Add(BtnBackColor);
            GrpContainer.Controls.Add(TxtPositionMmX);
            GrpContainer.Controls.Add(TxtPositionMmZ);
            GrpContainer.Controls.Add(TxtDirectionDeg);
            GrpContainer.Controls.Add(BtnConfirm);

            SetControls();

            for (int i = 0; i < Fishes; i++)
            {
                CmbPlayers.Items.Add("F" + (i + 1).ToString());
            }
            if (CmbPlayers.Items.Count > 0)
            {
                CmbPlayers.SelectedIndex = 0;
            }
        }

        private void SetControls()
        {
            Size size = new Size(70, 20);
            GrpContainer.Size = new Size(90, 220);
            CmbPlayers.Size = size;
            BtnFrontColor.Size = size;
            BtnBackColor.Size = size;
            TxtPositionMmX.Size = size;
            TxtPositionMmZ.Size = size;
            TxtDirectionDeg.Size = size;
            BtnConfirm.Size = size;

            CmbPlayers.Location = new Point(10, 15);
            BtnFrontColor.Location = new Point(10, 45);
            BtnBackColor.Location = new Point(10, 75);
            TxtPositionMmX.Location = new Point(10, 105);
            TxtPositionMmZ.Location = new Point(10, 135);
            TxtDirectionDeg.Location = new Point(10, 165);
            BtnConfirm.Location = new Point(10, 195);

            BtnConfirm.Text = "Confirm";
            BtnFrontColor.UseVisualStyleBackColor = true;
            BtnBackColor.UseVisualStyleBackColor = true;
            BtnConfirm.UseVisualStyleBackColor = true;
            CmbPlayers.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            RoboFish f = MyMission.Instance().TeamsRef[TeamId].Fishes[CmbPlayers.SelectedIndex];
            //int flag = (MyMission.Instance().TeamsRef[TeamId].Para.MyHalfCourt == HalfCourt.RIGHT) ? -1 : 1;

            f.ColorFish = BtnFrontColor.BackColor;
            f.ColorId = BtnBackColor.BackColor;

            f.PrePositionMm = f.PositionMm; // 保存移动前的坐标
            f.PositionMm.X = (int)Convert.ToSingle(TxtPositionMmX.Text);
            f.PositionMm.Z = (int)Convert.ToSingle(TxtPositionMmZ.Text);

            f.BodyDirectionRad = xna.MathHelper.ToRadians(Convert.ToSingle(TxtDirectionDeg.Text));
            f.VelocityDirectionRad = f.BodyDirectionRad;

            #region 设置的坐标数据合法性检查
            UrwpgSimHelper.ParametersCheckingRoboFish(ref f);

            // 无论是否进行过数据修正都将保存的值往界面控件上重写一次
            TxtPositionMmX.Text = f.PositionMm.X.ToString();
            TxtPositionMmZ.Text = f.PositionMm.Z.ToString();
            #endregion
        }

        /// <summary>
        /// 更换队伍队员时点击按钮触发事件
        /// </summary>
        /// <param name="sender">事件对象</param>
        /// <param name="e">事件参数</param>
        public void CmbPlayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CmbPlayers.SelectedIndex == -1) return;
            RoboFish f = MyMission.Instance().TeamsRef[TeamId].Fishes[CmbPlayers.SelectedIndex];
            //int flag = (MyMission.Instance().TeamsRef[TeamId].Para.MyHalfCourt == HalfCourt.RIGHT) ? -1 : 1;
            BtnFrontColor.BackColor = f.ColorFish;
            BtnBackColor.BackColor = f.ColorId;
            TxtPositionMmX.Text = ((int)f.PositionMm.X).ToString();
            TxtPositionMmZ.Text = ((int)f.PositionMm.Z).ToString();
            TxtDirectionDeg.Text = ((int)xna.MathHelper.ToDegrees(f.BodyDirectionRad)).ToString();
        }

        private void BtnFrontColor_Click(object sender, EventArgs e)
        {// 前端色标颜色修改为仿真机器鱼鱼体颜色
            ColorDialog colorDlg = new ColorDialog();

            colorDlg.ShowHelp = true;

            colorDlg.Color = BtnFrontColor.BackColor;

            // 确认选择则更新按钮上显示的颜色和相应仿真机器鱼的鱼体颜色
            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                BtnFrontColor.BackColor = colorDlg.Color;
                RoboFish f = MyMission.Instance().TeamsRef[TeamId].Fishes[CmbPlayers.SelectedIndex];
                f.ColorFish = BtnFrontColor.BackColor;
            }
        }

        private void BtnBackColor_Click(object sender, EventArgs e)
        {// 后端色标颜色修改为仿真机器鱼编号颜色
            ColorDialog colorDlg = new ColorDialog();

            colorDlg.ShowHelp = true;

            colorDlg.Color = BtnBackColor.BackColor;

            // 确认选择则更新按钮上显示的颜色和相应仿真机器鱼的编号颜色
            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                BtnBackColor.BackColor = colorDlg.Color;
                RoboFish f = MyMission.Instance().TeamsRef[TeamId].Fishes[CmbPlayers.SelectedIndex];
                f.ColorId = BtnBackColor.BackColor;
            }
        }
    }

    /// <summary>
    /// 服务端Fish设置界面BallSetting块控件组合类
    /// </summary>
    public class BallSettingComboControls
    {
        /// <summary>
        /// 仿真水球填充色选择按钮控件
        /// </summary>
        public Button BtnColor = new Button();

        /// <summary>
        /// 仿真水球位置坐标X分量显示控件
        /// </summary>
        public TextBox TxtPositionMmX = new NumberTextBox();

        /// <summary>
        /// 仿真水球位置坐标Z分量显示控件
        /// </summary>
        public TextBox TxtPositionMmZ = new NumberTextBox();

        /// <summary>
        /// 仿真水球半径值显示控件
        /// </summary>
        public TextBox TxtRadiusMm = new NumberTextBox();

        /// <summary>
        /// 分组容器控件
        /// </summary>
        public GroupBox GrpContainer = new GroupBox();

        /// <summary>
        /// 当前组合控件所代表仿真水球的编号（0,1...)
        /// </summary>
        public int BallId = 0;

        /// <summary>
        /// 带参数构造函数
        /// 根据仿真水球编号（从0开始）和当前仿真使命中仿真水球数量构造仿真水球设置对象
        /// </summary>
        /// <param name="ballId">仿真水球编号（从0开始）</param>
        /// <param name="balls">当前仿真使命中仿真水球数量</param>
        public BallSettingComboControls(int ballId, int balls)
        {
            BallId = ballId;

            GrpContainer.Controls.Add(BtnColor);
            GrpContainer.Controls.Add(TxtPositionMmX);
            GrpContainer.Controls.Add(TxtPositionMmZ);
            GrpContainer.Controls.Add(TxtRadiusMm);

            TxtPositionMmX.Validated += new EventHandler(TxtPositionMm_Validated);
            TxtPositionMmZ.Validated += new EventHandler(TxtPositionMm_Validated);
            BtnColor.Click += new EventHandler(BtnColor_Click);

            GrpContainer.Text = (balls > 1) ? string.Format("Ball{0}", ballId) : "Ball";
            GrpContainer.Size = new Size(255, 45);
            BtnColor.Size = new Size(65, 20);
            TxtPositionMmX.Size = new Size(50, 20);
            TxtPositionMmZ.Size = new Size(50, 20);
            TxtRadiusMm.Size = new Size(50, 20);

            BtnColor.Location = new Point(5, 15);
            TxtPositionMmX.Location = new Point(80, 15);
            TxtPositionMmZ.Location = new Point(140, 15);
            TxtRadiusMm.Location = new Point(200, 15);

            BtnColor.UseVisualStyleBackColor = true;
            TxtRadiusMm.ReadOnly = true;    // 半径只显示不可修改
        }

        void BtnColor_Click(object sender, EventArgs e)
        {// 设置仿真水球填充颜色和边框颜色
            ColorDialog colorDlg = new ColorDialog();

            colorDlg.ShowHelp = true;

            colorDlg.Color = BtnColor.ForeColor;

            // 确认选择则更新按钮上显示的颜色和相应仿真水球的填充颜色和边框颜色
            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                BtnColor.BackColor = colorDlg.Color;
                Ball b = MyMission.Instance().EnvRef.Balls[BallId];
                b.ColorFilled = BtnColor.BackColor; // 设置填充颜色
                b.ColorBorder = BtnColor.BackColor; // 设置边框颜色
            }
        }

        private void TxtPositionMm_Validated(object sender, EventArgs e)
        {
            Ball b = MyMission.Instance().EnvRef.Balls[BallId];

            b.PositionMm.X = (int)Convert.ToSingle(TxtPositionMmX.Text);
            b.PositionMm.Z = (int)Convert.ToSingle(TxtPositionMmZ.Text);

            #region 设置的坐标数据合法性检查
            UrwpgSimHelper.ParametersCheckingBall(ref b);

            // 无论是否进行过数据修正都将保存的值往界面控件上重写一次
            TxtPositionMmX.Text = b.PositionMm.X.ToString();
            TxtPositionMmZ.Text = b.PositionMm.Z.ToString();
            #endregion
        }

        /// <summary>
        /// 将当前仿真水球对象详细信息设置到相应界面控件中
        /// </summary>
        public void SetBallInfoToUi()
        {
            Ball ball = MyMission.Instance().EnvRef.Balls[BallId];
            BtnColor.BackColor = ball.ColorFilled;
            TxtPositionMmX.Text = ((int)ball.PositionMm.X).ToString();
            TxtPositionMmZ.Text = ((int)ball.PositionMm.Z).ToString();
            TxtRadiusMm.Text = ball.RadiusMm.ToString();
        }
    }
    #endregion
}