using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using URWPGSim2D.Common;

namespace URWPGSim2D.Gadget
{
    public partial class DlgTrajectory : Form
    {
        private Pen myPen = new Pen(Color.Red);
        Graphics myGraphic;

        public DlgTrajectory()
        {
            InitializeComponent();

            InitDynamicControls();
        }

        /// <summary>
        /// 当前使命类型中各队伍的各仿真机器鱼对应的复选框二维列表
        /// listCkb[i][j]对应第i支队伍第j个仿真机器鱼
        /// </summary>
        List<List<CheckBox>> listCkb = new List<List<CheckBox>>();

        /// <summary>
        /// 仿真水球对应的复选框
        /// </summary>
        CheckBox ckb = new CheckBox();

        List<SelectionComboControls> _listComboControls = new List<SelectionComboControls>();

        SelectionComboControls _ballComboControl = null;

        /// <summary>
        /// 初始化仿真机器鱼和仿真水球对应的动态控件
        /// </summary>
        private void InitDynamicControls()
        {
            List<Team<RoboFish>> TeamsRef = MyMission.Instance().TeamsRef;
            Field f = Field.Instance();

            int offSetPix = 10;
            for (int i = 0; i < TeamsRef.Count; i++)
            {
                _listComboControls.Add(new SelectionComboControls(i, TeamsRef[i].Fishes.Count, false));
                pnl.Controls.Add(_listComboControls[i].GrpContainer);
                _listComboControls[i].GrpContainer.Location = new Point(10, 10 + i * 110);
                offSetPix += 110;
            }

            if (MyMission.Instance().EnvRef.Balls.Count > 0)
            {
                _ballComboControl = new SelectionComboControls(0, MyMission.Instance().EnvRef.Balls.Count, true);
                pnl.Controls.Add(_ballComboControl.GrpContainer);
                _ballComboControl.GrpContainer.Location = new Point(10, offSetPix);
            }
        }

        /// <summary>
        /// 绘制仿真机器鱼和仿真水球轨迹
        /// </summary>
        public void DrawPictureBox()
        {
            MyMission myMission = MyMission.Instance();
            myPen.Width = 2;
            Bitmap bmp = new Bitmap(myMission.EnvRef.FieldInfo.PictureBoxXPix, myMission.EnvRef.FieldInfo.PictureBoxZPix);
            myGraphic = Graphics.FromImage(bmp);

            for (int i = 0; i < myMission.TeamsRef.Count; i++)
            {// 绘制仿真机器鱼轨迹
                for (int j = 0; j < myMission.TeamsRef[i].Fishes.Count; j++)
                {
                    if ((_listComboControls[i].ListChkFishes[j].Checked == true) 
                        && (myMission.TeamsRef[i].Fishes[j].TrajectoryPoints.Count > 1))
                    {
                        myPen.Color = myMission.TeamsRef[i].Fishes[j].ColorFish;
                        int m = 0;
                        for (m = 0; m < myMission.TeamsRef[i].Fishes[j].TrajectoryPoints.Count - 1; m++)
                        {
                            myGraphic.DrawLine(myPen, myMission.TeamsRef[i].Fishes[j].TrajectoryPoints[m],
                                myMission.TeamsRef[i].Fishes[j].TrajectoryPoints[m + 1]);
                        }
                        myGraphic.DrawString(String.Format("F{0}", j), new Font(Font, FontStyle.Bold),
                            new SolidBrush(myPen.Color), (PointF)(myMission.TeamsRef[i].Fishes[j].TrajectoryPoints[m]));
                    }
                }
            }

            if (_ballComboControl != null)
            {// 绘制仿真水球的轨迹
                for (int i = 0; i < _ballComboControl.ListChkFishes.Count; i++)
                {
                    if ((_ballComboControl.ListChkFishes[i].Checked == true) 
                        && (myMission.EnvRef.Balls[i].TrajectoryPoints.Count > 1))
                    {
                        myPen.Color = myMission.EnvRef.Balls[i].ColorFilled;
                        int n;
                        for (n = 0; n < myMission.EnvRef.Balls[i].TrajectoryPoints.Count - 1; n++)
                        {
                            myGraphic.DrawLine(myPen, myMission.EnvRef.Balls[i].TrajectoryPoints[n],
                                myMission.EnvRef.Balls[i].TrajectoryPoints[n + 1]);
                        }
                        string str = (_ballComboControl.ListChkFishes.Count > 1) ? String.Format("Ball{0}", i + 1) : "Ball";
                        myGraphic.DrawString(str, new Font(Font, FontStyle.Bold),
                            new SolidBrush(myPen.Color), (PointF)(myMission.EnvRef.Balls[0].TrajectoryPoints[n]));
                    }
                }
            }

            picTrajectory.Image = bmp;
        }

        /// <summary>
        /// 仿真机器鱼和仿真水球对应的复选框点击事件响应
        /// </summary>
        private void CheckBox_Click(object sender, EventArgs e)
        {
            DrawPictureBox();
        }

        private void DlgTrajectory_Load(object sender, EventArgs e)
        {
            // 动态生成窗体和picMatch的长宽，计算的顺序不可改变（可能与属性设置有关） weiqingdan20101105
            Field f = Field.Instance();
            this.Width = f.PictureBoxXPix - f.FieldInnerBorderPix - f.FieldOuterBorderPix + grp.Width + 3 * 10 + 10;
            this.Height = f.PictureBoxZPix - f.FieldInnerBorderPix - f.FieldOuterBorderPix + 2 * 10 + 42;

            picTrajectory.Width = f.PictureBoxXPix - f.FieldInnerBorderPix - f.FieldOuterBorderPix;
            picTrajectory.Height = f.PictureBoxZPix - f.FieldInnerBorderPix - f.FieldOuterBorderPix;
            picTrajectory.Location = new Point(10, 10);
            grp.Location = new Point(20 + picTrajectory.Width, 10);
            grp.Height = picTrajectory.Height;
        }

        private void DlgTrajectory_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 清除当前仿真使命每支队伍的每条仿真机器鱼保存的轨迹点
            MyMission myMission = MyMission.Instance();
            for (int i = 0; i < myMission.TeamsRef.Count; i++)
            {
                for (int j = 0; j < myMission.TeamsRef[i].Fishes.Count; j++)
                {
                    myMission.TeamsRef[i].Fishes[j].TrajectoryPoints.Clear();
                }
            }
            if (myMission.EnvRef.Balls.Count > 0)
            {
                for (int i = 0; i < myMission.EnvRef.Balls.Count; i++)
                {
                    myMission.EnvRef.Balls[i].TrajectoryPoints.Clear();
                }
            }
        }
    }

    /// <summary>
    /// 动态对象轨迹绘制界面仿真机器鱼或仿真水球选择控件组合类
    /// </summary>
    public class SelectionComboControls
    {
        public SelectionComboControls(int teamId, int count, bool isBall)
        {
            TeamId = teamId;

            GrpContainer.Text = (isBall == true) ? "Ball" : "Team" + (teamId + 1).ToString();
            GrpContainer.Size = new Size(100, 100);
            GrpContainer.Controls.Add(PnlContainer);
            PnlContainer.AutoScroll = true;
            PnlContainer.Size = new Size(80, 50);
            PnlContainer.Location = new Point(5, 15);
            
            for (int i = 0; i < count; i++)
            {
                ListChkFishes.Add(new CheckBox());
                PnlContainer.Controls.Add(ListChkFishes[i]);
                ListChkFishes[i].Text = (isBall == true) ? String.Format("B{0}", i + 1) : String.Format("F{0}", i + 1);
                ListChkFishes[i].Size = new Size(50, 16);
                ListChkFishes[i].Location = new Point(10, 10 + i * 20);
            }
        }

        /// <summary>
        /// 当前队伍所有仿真机器鱼对应的复选框列表
        /// </summary>
        public List<CheckBox> ListChkFishes = new List<CheckBox>();

        /// <summary>
        /// 分组容器控件
        /// </summary>
        public GroupBox GrpContainer = new GroupBox();

        /// <summary>
        /// 嵌于GroupBox之内的Panel，用于容纳CheckBox，提供滚动条
        /// </summary>
        public Panel PnlContainer = new Panel();

        /// <summary>
        /// 当前组合控件所代表队伍的编号（0,1...)
        /// </summary>
        public int TeamId = 0;
    }
}
