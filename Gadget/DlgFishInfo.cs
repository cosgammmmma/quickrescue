using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using xna = Microsoft.Xna.Framework;

using URWPGSim2D.Common;

namespace URWPGSim2D.Gadget
{
    public partial class DlgFishInfo : Form
    {
        // 20101128
        /// <summary>
        /// 定义信息采样步长，默认为10
        /// </summary>
        public int step = 10;

        // 20101201
        /// <summary>
        /// Excel数据记录对象
        /// </summary>
        public ExcelHelper writeExcelDemo = null;

        public DlgFishInfo()
        {
            InitializeComponent();
            InitializeFishInfo();
        }

        DataGridView dgv = new DataGridView();

        private void InitializeFishInfo()
        {
            //列标题居中
            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
            headerStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersDefaultCellStyle = headerStyle;
            dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            dgv.RowHeadersWidth = 90;

            DataGridViewTextBoxColumn txColumn1 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn txColumn2 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn txColumn3 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn txColumn4 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn txColumn5 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn txColumn6 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn txColumn7 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn txColumn8 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn txColumn9 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn txColumn10 = new DataGridViewTextBoxColumn();

            txColumn1.HeaderText = "X";
            txColumn1.Width = 60;
            txColumn1.DefaultCellStyle = headerStyle;
            //txColumn1.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            txColumn1.ReadOnly = true;
            dgv.Columns.Add(txColumn1);

            txColumn2.HeaderText = "Z";
            txColumn2.Width = 60;
            txColumn2.DefaultCellStyle = headerStyle;
            txColumn2.ReadOnly = true;
            dgv.Columns.Add(txColumn2);

            txColumn3.HeaderText = "Fish Direction";
            txColumn3.Width = 60;
            txColumn3.DefaultCellStyle = headerStyle;
            txColumn3.ReadOnly = true;
            dgv.Columns.Add(txColumn3);

            txColumn4.HeaderText = "Velocity";
            txColumn4.Width = 60;
            txColumn4.DefaultCellStyle = headerStyle;
            txColumn3.ReadOnly = true;
            dgv.Columns.Add(txColumn4);

            txColumn5.HeaderText = "Velocity Direction";
            txColumn5.Width = 60;
            txColumn5.DefaultCellStyle = headerStyle;
            txColumn5.ReadOnly = true;
            dgv.Columns.Add(txColumn5);

            txColumn6.HeaderText = "Angular Velocity";
            txColumn6.Width = 60;
            txColumn6.DefaultCellStyle = headerStyle;
            txColumn6.ReadOnly = true;
            dgv.Columns.Add(txColumn6);

            txColumn7.HeaderText = "Velocity Tactic";
            txColumn7.Width = 60;
            txColumn7.DefaultCellStyle = headerStyle;
            txColumn7.ReadOnly = true;
            dgv.Columns.Add(txColumn7);

            txColumn8.HeaderText = "Swerving Tactic";
            txColumn8.Width = 60;
            txColumn8.DefaultCellStyle = headerStyle;
            txColumn8.ReadOnly = true;
            dgv.Columns.Add(txColumn8);

            txColumn9.HeaderText = "Target Velocity Tactic";
            txColumn9.Width = 60;
            txColumn9.DefaultCellStyle = headerStyle;
            txColumn9.ReadOnly = true;
            dgv.Columns.Add(txColumn9);

            txColumn10.HeaderText = "Target Swerving Tactic";
            txColumn10.Width = 60;
            txColumn10.DefaultCellStyle = headerStyle;
            txColumn10.ReadOnly = true;
            dgv.Columns.Add(txColumn10);

            MyMission missionref = MyMission.Instance();
            int rowsId = 0;
            for (int i = 0; i < missionref.TeamsRef.Count; i++)
            {
                for (int j = 0; j < missionref.TeamsRef[i].Fishes.Count; j++)
                {
                    dgv.Rows.Add(new object[] { });
                    dgv.ReadOnly = true;
                    dgv.Rows[rowsId + j].HeaderCell.Value = String.Format("Team{0} F{1}", i + 1, j + 1);
                }
                rowsId += missionref.TeamsRef[i].Fishes.Count;

            }
            dgv.AllowUserToAddRows = false;
        }

        public void SetFishInfo()
        {
            MyMission missionref = MyMission.Instance();

            dgv.Location = new Point(10, 18);
            dgv.Size = new Size(710, 230);
            dgv.BackgroundColor = Color.White;
            groupBox1.Controls.Add(dgv);

            int fishes = 0;
            for (int i = 0; i < missionref.TeamsRef.Count; i++)
            {
                for (int j = 0; j < missionref.TeamsRef[i].Fishes.Count; j++)
                {
                    RoboFish f = missionref.TeamsRef[i].Fishes[j];

                    // 第1列第fishes + j个单元格的值为仿真机器鱼的X坐标值
                    dgv[0, fishes + j].Value = (int)(f.PositionMm.X);

                    // 第2列第fishes + j个单元格的值为仿真机器鱼的Z坐标值
                    dgv[1, fishes + j].Value = (int)(f.PositionMm.Z);

                    // 第3列第fishes + j个单元格的值为仿真机器鱼的鱼体方向
                    dgv[2, fishes + j].Value = string.Format("{0:0.0000}", f.BodyDirectionRad);

                    // 第4列第fishes + j个单元格的值为仿真机器鱼的运动速度
                    dgv[3, fishes + j].Value = string.Format("{0:0.0000}", f.VelocityMmPs);

                    // 第5列第fishes + j个单元格的值为仿真机器鱼运动的速度方向
                    dgv[4, fishes + j].Value = string.Format("{0:0.0000}", f.VelocityDirectionRad);

                    // 第6列第fishes + j个单元格的值为机器鱼的角速度
                    dgv[5, fishes + j].Value = string.Format("{0:0.0000}", f.AngularVelocityRadPs);    

                    // 第7列第fishes + j个单元格的值为当前时刻机器鱼运动的速度档位
                    dgv[6, fishes + j].Value = f.Tactic.VCode;

                    // 第8列第fishes + j个单元格的值为当前时刻机器鱼运动的方向档位
                    dgv[7, fishes + j].Value = f.Tactic.TCode;

                    // 第9列第fishes + j个单元格的值为前一时刻机器鱼运动的目标决策速度档位
                    dgv[8, fishes + j].Value = f.TargetTactic.VCode;

                    // 第10列第fishes + j个单元格的值为前一时刻机器鱼运动的目标决策方向档位
                    dgv[9, fishes + j].Value = f.TargetTactic.TCode;

                }
                fishes += missionref.TeamsRef[i].Fishes.Count;
            }
        }

        private void DlgFishInfo_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 如果正在使用Excel保存报表数据则先关闭之
            if (writeExcelDemo != null)
            {
                writeExcelDemo.CloseExcel();
            }
        }

        private void numericUpDownStep_ValueChanged(object sender, EventArgs e)
        {
            step = Int32.Parse(((NumericUpDown)sender).Value.ToString());
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (btnSave.Text == "Save")
            {
                btnSave.Text = "Stop Saving";

                writeExcelDemo = new ExcelHelper();
            }
            else if (btnSave.Text == "Stop Saving")
            {
                btnSave.Text = "Save";
                writeExcelDemo.CloseExcel();
            }
        }
    }
}