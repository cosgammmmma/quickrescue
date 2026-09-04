//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Match1V1.cs
// Date: 20101119  Author: LiYoubing  Version: 1
// Description: 具体仿真使命（比赛或实验项目）所用到的Helper类定义文件
// Histroy:
// Date: 20110516  Author: LiYoubing
// Modification: 
// 1.用MissionMessageBox封装使命运行过程中弹出提示对话框前后的处理
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using URWPGSim2D.Common;

namespace URWPGSim2D.Match
{
    public static class MatchHelper
    {
        /// <summary>
        /// 3VS3及1VS1等对抗赛比赛阶段
        /// </summary>
        public enum CompetitionPeriod
        {
            /// <summary>
            /// 正常比赛阶段
            /// </summary>
            NormalTime = 0,

            /// <summary>
            /// 加时赛阶段
            /// </summary>
            ExtraTime = 1,

            /// <summary>
            /// 点球大战阶段
            /// </summary>
            PenaltyKickTime = 2,

            /// <summary>
            /// 制胜球阶段
            /// </summary>
            ClutchShotTime = 3,

              //longhainan
            /// <summary>
            /// 点球阶段
            /// </summary>
            ShootoutTime = 4,
        }

        /// <summary>
        /// 处理具体仿真使命运行过程中弹出提示对话框的任务
        /// </summary>
        /// <param name="mission">运行的具体仿真使命对象相应的Mission对象</param>
        /// <param name="strMsg">需要显示的消息字符串</param>
        /// <param name="strCaption">需要显示的对话框标题</param>
        /// <param name="btn">消息对话框按钮类型</param>
        /// <param name="icon">消息对话框图标类型</param>
        public static void MissionMessageBox(ref Mission mission, string strMsg, string strCaption, 
            MessageBoxButtons btn, MessageBoxIcon icon)
        {
            mission.CommonPara.IsPaused = true;
            MessageBox.Show(strMsg, strCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            mission.CommonPara.IsPaused = false;
            mission.CommonPara.IsPauseNeeded = true;
        }
    }
}