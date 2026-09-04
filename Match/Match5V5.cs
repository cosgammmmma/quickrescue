//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Match5V5.cs
// Date: 20101119  Author: LiYoubing  Version: 1
// Description: 两队对抗赛3VS3相关的仿真机器鱼，仿真环境和仿真使命定义文件
// Histroy:
// Date: 20110512  Author: LiYoubing
// Modification: 
// 1.受RoboFish的碰撞状态标志改成标志列表的影响 标志值的比较改用List.Contains方法进行
// Date: 20110516  Author: LiYoubing
// Modification: 
// 1.全面整理代码
// 2.完全实现半场交换
// 3.各种提示信息加入真实队名
// Date: 20110726 Author: LiYoubing
// Modification: 
// 1.进入制胜球阶段时修改比赛阶段标志
// 2.ResetTeamsAndFishes中清除犯规标志
// Date:20111101 Author:ZhangBo
// Modification:
// 1.将3V3代码改为5V5。
// 2.限定仿真鱼的角色，活动范围不能超过其允许的区域，添加相应的犯规处理。
// 3.改动犯规处理代码，实现以场地中轴为中心交替放置犯规机器鱼等功能。
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices; // for MethodImpl
using System.Drawing;
using System.Windows.Forms;
using xna = Microsoft.Xna.Framework;

using Microsoft.Dss.Core;

using URWPGSim2D.Common;

namespace URWPGSim2D.Match
{
    /// <summary>
    ///5V5对抗赛中仿真机器鱼犯规类型 added by zhangbo 20111211
    /// </summary>
    public enum FoulType5V5
    {
        /// <summary>
        /// 没有犯规
        /// </summary>
        NONE = 0,

        /// <summary>
        /// 犯规情况1（防守方两条仿真机器鱼位于己方禁区一定时间）
        /// </summary>
        FOUl1,

        /// <summary>
        /// 犯规情况2（防守方机器鱼进入己方球门一定时间）
        /// </summary>
        FOUl2,

        /// <summary>
        /// 犯规情况3（机器鱼游出其允许活动范围）
        /// </summary>
        FOUl3
    }

    /// <summary>
    /// 对抗比赛5V5仿真使命特有的仿真机器鱼类
    /// </summary>
    [Serializable]
    public class Fish5V5 : RoboFish
    {
        /// <summary>
        /// 记录仿真机器鱼在己方禁区的次数
        /// </summary>
        public int CountInOwnForbiddenZone = 0;

        /// <summary>
        /// 记录仿真机器鱼在己方球门区的次数
        /// </summary>
        public int CountInOwnGoalArea = 0;

        /// <summary>
        /// 记录犯规的仿真机器鱼一次犯规后已经下场的周期数
        /// </summary>
        public int CountCyclesFouled = 0;

        /// <summary>
        /// 仿真机器鱼犯规状态 
        /// </summary>
        public FoulType5V5 FoulFlag;
    }

    /// <summary>
    /// 对抗比赛5V5仿真使命特有的仿真环境类
    /// </summary>
    [Serializable]
    public class Environment5V5 : SimEnvironment
    {
        // 在这里定义对抗比赛5V5仿真使命特有的仿真环境的特性和行为
    }

    /// <summary>
    /// 对抗比赛5V5使命类
    /// </summary>
    [Serializable]
    public partial class Match5V5 : Mission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static Match5V5 instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Match5V5 Instance()
        {
            if (instance == null)
            {
                instance = new Match5V5();
            }
            return instance;
        }

        /// <summary>
        /// 私有构造函数 在Instance()中调用 进程生命周期内只会被调用一次
        /// </summary>
        private Match5V5()
        {
            CommonPara.MsPerCycle = 100;
            CommonPara.TeamCount = 2;
            CommonPara.FishCntPerTeam = 5;
            CommonPara.Name = "5VS5";
            InitTeamsAndFishes();
            InitEnvironment();
            InitDecision();
            InitHtMissionVariables();
        }
        #endregion

        #region public fields
        /// <summary>
        /// 仿真使命（比赛或实验项目）具体参与队伍列表及其具体仿真机器鱼
        /// </summary>
        public List<Team<Fish5V5>> Teams = new List<Team<Fish5V5>>();

        /// <summary>
        /// 仿真使命（比赛或实验项目）所使用的具体仿真环境
        /// </summary>
        public Environment5V5 Env = new Environment5V5();
        #endregion

        #region private and protected methods
        /// <summary>
        /// 新建当前使命参与队伍列表及每支队伍的仿真机器鱼对象，在当前使命类构造函数中调用
        /// 该方法要在调用SetMissionCommonPara设置好仿真使命公共参数（如每队队员数量）之后调用
        /// </summary>
        private void InitTeamsAndFishes()
        {
            for (int i = 0; i < CommonPara.TeamCount; i++)
            {
                // 给具体仿真机器鱼队伍列表添加新建的具体仿真机器鱼队伍
                Teams.Add(new Team<Fish5V5>());

                // 给通用仿真机器鱼队伍列表添加新建的通用仿真机器鱼队伍
                TeamsRef.Add(new Team<RoboFish>());

                // 给具体仿真机器鱼队伍设置队员数量
                Teams[i].Para.FishCount = CommonPara.FishCntPerTeam;

                // 给具体仿真机器鱼队伍设置所在半场
                if (i == 0)
                {// 第0支队伍默认在左半场
                    Teams[i].Para.MyHalfCourt = HalfCourt.LEFT;
                }
                else if (i == 1)
                {// 第1支队伍默认在右半场
                    Teams[i].Para.MyHalfCourt = HalfCourt.RIGHT;
                }

                // 给通用仿真机器鱼队伍设置公共参数
                TeamsRef[i].Para = Teams[i].Para;

                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    // 给具体仿真机器鱼队伍添加新建的具体仿真机器鱼
                    Teams[i].Fishes.Add(new Fish5V5());

                    // 给通用仿真机器鱼队伍添加新建的通用仿真机器鱼
                    TeamsRef[i].Fishes.Add((RoboFish)Teams[i].Fishes[j]);
                }
            }

            // 设置仿真机器鱼鱼体和编号默认颜色
            ResetColorFishAndId();
        }

        /// <summary>
        /// 初始化当前使命的仿真环境，在当前使命类构造函数中调用
        /// </summary>
        private void InitEnvironment()
        {
            EnvRef = (SimEnvironment)Env;
            // 只有第一次调用时需要添加一个仿真水球
            if (Env.Balls.Count == 0)
            {
                Env.Balls.Add(new Ball());
            }
        }

        /// <summary>
        /// 初始化当前仿真使命类特有的需要传递给策略的变量名和变量值，在当前使命类构造函数中调用
        /// 添加到仿真使命基类的哈希表Hashtable成员中
        /// </summary>
        private void InitHtMissionVariables()
        {
            // 初始化比赛阶段为CompetitionPeriod.NormalTime（0）
            HtMissionVariables.Add("CompetitionPeriod", ((int)MatchHelper.CompetitionPeriod.NormalTime).ToString());
        }

        /// <summary>
        /// 更新当前使命类特有的需要传递给策略的变量名和变量值，在ProcessControlRules中调用
        /// </summary>
        private void UpdateHtMissionVariables()
        {
            // 更新比赛阶段值
            HtMissionVariables["CompetitionPeriod"] = CompetitionPeriod.ToString();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值
        /// </summary>
        public override void ResetTeamsAndFishes()
        {
            Field f = Field.Instance();
            MyMission myMission = MyMission.Instance();
            if (Teams != null)
            {
                for (int i = 0; i < Teams.Count; i++)
                {
                    // 左/右半场队伍的半场标志分别为LEFT(0)/RIGHT(1)
                    myMission.TeamsRef[i].Para.MyHalfCourt = (HalfCourt)i;
                    for (int j = 0; j < Teams[i].Fishes.Count; j++)
                    {
                        Teams[i].Fishes[j].CountDrawing = 0;
                        Teams[i].Fishes[j].InitPhase = i * 5 + j * 5;

                        // 清除犯规标志 LiYoubing 20110726
                        Teams[i].Fishes[j].FoulFlag = FoulType5V5.NONE;
                        Teams[i].Fishes[j].CountInOwnForbiddenZone = 0;
                        Teams[i].Fishes[j].CountInOwnGoalArea = 0;
                        Teams[i].Fishes[j].CountCyclesFouled = 0;
                    }
                }

                // 左半场队伍的仿真机器鱼位置
                myMission.TeamsRef[0].Fishes[0].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[0].BodyLength * 3, 0, 0);
                myMission.TeamsRef[0].Fishes[1].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[1].BodyLength * 3, 
                    0, myMission.TeamsRef[0].Fishes[1].BodyWidth * 3);
                myMission.TeamsRef[0].Fishes[2].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[2].BodyLength * 3, 
                    0, -myMission.TeamsRef[0].Fishes[2].BodyWidth * 3);
                //myMission.TeamsRef[0].Fishes[3].PositionMm = new xna.Vector3(
                //    f.LeftMm + myMission.TeamsRef[0].Fishes[3].BodyLength * 3,
                //    0, myMission.TeamsRef[0].Fishes[3].BodyWidth * 6);
                //myMission.TeamsRef[0].Fishes[4].PositionMm = new xna.Vector3(
                //    f.LeftMm + myMission.TeamsRef[0].Fishes[4].BodyLength * 3,
                //    0, -myMission.TeamsRef[0].Fishes[4].BodyWidth * 6);
                myMission.TeamsRef[0].Fishes[3].PositionMm = new xna.Vector3(
                   -myMission.TeamsRef[0].Fishes[3].BodyLength * 2,
                    0, myMission.TeamsRef[0].Fishes[3].BodyWidth * 2);
                myMission.TeamsRef[0].Fishes[4].PositionMm = new xna.Vector3(
                   -myMission.TeamsRef[0].Fishes[4].BodyLength * 2,
                    0, -myMission.TeamsRef[0].Fishes[4].BodyWidth * 2);
               
              //  右半场队伍的仿真机器鱼位置
                myMission.TeamsRef[1].Fishes[0].PositionMm = new xna.Vector3(
                    f.RightMm - myMission.TeamsRef[1].Fishes[0].BodyLength * 3, 0, 0);
                myMission.TeamsRef[1].Fishes[1].PositionMm = new xna.Vector3(
                    f.RightMm - myMission.TeamsRef[1].Fishes[1].BodyLength * 3,
                    0, myMission.TeamsRef[1].Fishes[1].BodyWidth * 3);
                myMission.TeamsRef[1].Fishes[2].PositionMm = new xna.Vector3(
                    f.RightMm - myMission.TeamsRef[1].Fishes[2].BodyLength * 3,
                    0, -myMission.TeamsRef[1].Fishes[2].BodyWidth * 3);
                //myMission.TeamsRef[1].Fishes[3].PositionMm = new xna.Vector3(
                //    f.RightMm - myMission.TeamsRef[1].Fishes[3].BodyLength * 3,
                //    0, myMission.TeamsRef[1].Fishes[3].BodyWidth * 6);
                //myMission.TeamsRef[1].Fishes[4].PositionMm = new xna.Vector3(
                //    f.RightMm - myMission.TeamsRef[1].Fishes[4].BodyLength * 3,
                //    0, -myMission.TeamsRef[1].Fishes[4].BodyWidth * 6);
                myMission.TeamsRef[1].Fishes[3].PositionMm = new xna.Vector3(
                   myMission.TeamsRef[1].Fishes[3].BodyLength * 2,
                    0, myMission.TeamsRef[1].Fishes[3].BodyWidth * 10);
                myMission.TeamsRef[1].Fishes[4].PositionMm = new xna.Vector3(
                   myMission.TeamsRef[1].Fishes[4].BodyLength * 2,
                    0, -myMission.TeamsRef[1].Fishes[4].BodyWidth * 10);

                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    // 左半场队伍的仿真机器鱼朝右
                    myMission.TeamsRef[0].Fishes[j].BodyDirectionRad = 0;
                    // 右半场队伍的仿真机器鱼朝左
                    myMission.TeamsRef[1].Fishes[j].BodyDirectionRad = xna.MathHelper.Pi;
                }
            }
        }

        //modified by longhainan 20120801
        public override void ResetGoalHandler()
        {
            Field f = Field.Instance();
            MyMission myMission = MyMission.Instance();
            if (Teams != null)
            {
                for (int i = 0; i < Teams.Count; i++)
                {
                    // 左/右半场队伍的半场标志分别为LEFT(0)/RIGHT(1)
                    myMission.TeamsRef[i].Para.MyHalfCourt = (HalfCourt)i;
                    for (int j = 0; j < Teams[i].Fishes.Count; j++)
                    {
                        Teams[i].Fishes[j].CountDrawing = 0;
                        Teams[i].Fishes[j].InitPhase = i * 5 + j * 5;

                        // 清除犯规标志 LiYoubing 20110726
                        Teams[i].Fishes[j].FoulFlag = FoulType5V5.NONE;
                        Teams[i].Fishes[j].CountInOwnForbiddenZone = 0;
                        Teams[i].Fishes[j].CountInOwnGoalArea = 0;
                        Teams[i].Fishes[j].CountCyclesFouled = 0;
                    }
                }

                // 左半场队伍的仿真机器鱼位置
                myMission.TeamsRef[0].Fishes[0].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[0].BodyLength * 3, 0, 0);
                myMission.TeamsRef[0].Fishes[1].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[1].BodyLength * 3,
                    0, myMission.TeamsRef[0].Fishes[1].BodyWidth * 3);
                myMission.TeamsRef[0].Fishes[2].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[2].BodyLength * 3,
                    0, -myMission.TeamsRef[0].Fishes[2].BodyWidth * 3);
                //myMission.TeamsRef[0].Fishes[3].PositionMm = new xna.Vector3(
                //    f.LeftMm + myMission.TeamsRef[0].Fishes[3].BodyLength * 3,
                //    0, myMission.TeamsRef[0].Fishes[3].BodyWidth * 6);
                //myMission.TeamsRef[0].Fishes[4].PositionMm = new xna.Vector3(
                //    f.LeftMm + myMission.TeamsRef[0].Fishes[4].BodyLength * 3,
                //    0, -myMission.TeamsRef[0].Fishes[4].BodyWidth * 6);
                myMission.TeamsRef[0].Fishes[3].PositionMm = new xna.Vector3(
                   -myMission.TeamsRef[0].Fishes[3].BodyLength * 2,
                    0, myMission.TeamsRef[0].Fishes[3].BodyWidth * 10);
                myMission.TeamsRef[0].Fishes[4].PositionMm = new xna.Vector3(
                   -myMission.TeamsRef[0].Fishes[4].BodyLength * 2,
                    0, -myMission.TeamsRef[0].Fishes[4].BodyWidth * 10);

                // 右半场队伍的仿真机器鱼位置
                myMission.TeamsRef[1].Fishes[0].PositionMm = new xna.Vector3(
                    f.RightMm - myMission.TeamsRef[1].Fishes[0].BodyLength * 3, 0, 0);
                myMission.TeamsRef[1].Fishes[1].PositionMm = new xna.Vector3(
                    f.RightMm - myMission.TeamsRef[1].Fishes[1].BodyLength * 3,
                    0, myMission.TeamsRef[1].Fishes[1].BodyWidth * 3);
                myMission.TeamsRef[1].Fishes[2].PositionMm = new xna.Vector3(
                    f.RightMm - myMission.TeamsRef[1].Fishes[2].BodyLength * 3,
                    0, -myMission.TeamsRef[1].Fishes[2].BodyWidth * 3);
                //myMission.TeamsRef[1].Fishes[3].PositionMm = new xna.Vector3(
                //    f.RightMm - myMission.TeamsRef[1].Fishes[3].BodyLength * 3,
                //    0, myMission.TeamsRef[1].Fishes[3].BodyWidth * 6);
                //myMission.TeamsRef[1].Fishes[4].PositionMm = new xna.Vector3(
                //    f.RightMm - myMission.TeamsRef[1].Fishes[4].BodyLength * 3,
                //    0, -myMission.TeamsRef[1].Fishes[4].BodyWidth * 6);
                myMission.TeamsRef[1].Fishes[3].PositionMm = new xna.Vector3(
                   myMission.TeamsRef[1].Fishes[3].BodyLength * 2,
                    0, myMission.TeamsRef[1].Fishes[3].BodyWidth * 2);
                myMission.TeamsRef[1].Fishes[4].PositionMm = new xna.Vector3(
                   myMission.TeamsRef[1].Fishes[4].BodyLength * 2,
                    0, -myMission.TeamsRef[1].Fishes[4].BodyWidth * 2);

                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    // 左半场队伍的仿真机器鱼朝右
                    myMission.TeamsRef[0].Fishes[j].BodyDirectionRad = 0;
                    // 右半场队伍的仿真机器鱼朝左
                    myMission.TeamsRef[1].Fishes[j].BodyDirectionRad = xna.MathHelper.Pi;
                }
            }
        }

        // modified by longhainan 20120801
        public override void ResetShootout()
        {
            Field f = Field.Instance();
            MyMission myMission = MyMission.Instance();
            if (Teams != null)
            {
                for (int i = 0; i < Teams.Count; i++)
                {
                    // 左/右半场队伍的半场标志分别为LEFT(0)/RIGHT(1)
                    myMission.TeamsRef[i].Para.MyHalfCourt = (HalfCourt)i;
                    for (int j = 0; j < Teams[i].Fishes.Count; j++)
                    {
                        Teams[i].Fishes[j].CountDrawing = 0;
                        Teams[i].Fishes[j].InitPhase = i * 5 + j * 5;

                        // 清除犯规标志 LiYoubing 20110726
                        Teams[i].Fishes[j].FoulFlag = FoulType5V5.NONE;
                        Teams[i].Fishes[j].CountInOwnForbiddenZone = 0;
                        Teams[i].Fishes[j].CountInOwnGoalArea = 0;
                        Teams[i].Fishes[j].CountCyclesFouled = 0;
                    }
                }

                // 左半场队伍的仿真机器鱼位置
                myMission.TeamsRef[0].Fishes[0].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[0].BodyLength * 3, 0, 0);
                myMission.TeamsRef[0].Fishes[1].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[1].BodyLength * 3,
                    0, myMission.TeamsRef[0].Fishes[1].BodyWidth * 3);
                myMission.TeamsRef[0].Fishes[2].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[2].BodyLength * 3,
                    0, -myMission.TeamsRef[0].Fishes[2].BodyWidth * 3);
                //myMission.TeamsRef[0].Fishes[3].PositionMm = new xna.Vector3(
                //    f.LeftMm + myMission.TeamsRef[0].Fishes[3].BodyLength * 3,
                //    0, myMission.TeamsRef[0].Fishes[3].BodyWidth * 6);
                //myMission.TeamsRef[0].Fishes[4].PositionMm = new xna.Vector3(
                //    f.LeftMm + myMission.TeamsRef[0].Fishes[4].BodyLength * 3,
                //    0, -myMission.TeamsRef[0].Fishes[4].BodyWidth * 6);
                myMission.TeamsRef[0].Fishes[3].PositionMm = new xna.Vector3(
                   -myMission.TeamsRef[0].Fishes[3].BodyLength * 2,
                    0, myMission.TeamsRef[0].Fishes[3].BodyWidth * 2);
                myMission.TeamsRef[0].Fishes[4].PositionMm = new xna.Vector3(
                   -myMission.TeamsRef[0].Fishes[4].BodyLength * 2,
                    0, -myMission.TeamsRef[0].Fishes[4].BodyWidth * 2);
           
                // 右半场队伍的仿真机器鱼位置
                //myMission.TeamsRef[1].Fishes[0].PositionMm = new xna.Vector3(
                //    f.RightMm - myMission.TeamsRef[1].Fishes[0].BodyLength * 3, 0, 0)  
                myMission.TeamsRef[1].Fishes[0].PositionMm = new xna.Vector3(
                    f.RightMm - myMission.TeamsRef[1].Fishes[0].BodyLength * 3, 
                    0, myMission.TeamsRef[1].Fishes[0].BodyWidth * 32);
                myMission.TeamsRef[1].Fishes[1].PositionMm = new xna.Vector3(
                    f.RightMm - myMission.TeamsRef[1].Fishes[1].BodyLength * 3, 
                    0, myMission.TeamsRef[1].Fishes[1].BodyWidth * 29);
                myMission.TeamsRef[1].Fishes[2].PositionMm = new xna.Vector3(
                 f.RightMm - myMission.TeamsRef[1].Fishes[2].BodyLength * 3,
                 0, myMission.TeamsRef[1].Fishes[2].BodyWidth * 26);
                //myMission.TeamsRef[1].Fishes[2].PositionMm = new xna.Vector3(
                //    f.RightMm - myMission.TeamsRef[1].Fishes[2].BodyLength * 3, 
                //    0, -myMission.TeamsRef[1].Fishes[2].BodyWidth * 30);

                //myMission.TeamsRef[1].Fishes[3].PositionMm = new xna.Vector3(
                //    f.RightMm - myMission.TeamsRef[1].Fishes[3].BodyLength * 3,
                //    0, myMission.TeamsRef[1].Fishes[3].BodyWidth * 6);
                //myMission.TeamsRef[1].Fishes[4].PositionMm = new xna.Vector3(
                //    f.RightMm - myMission.TeamsRef[1].Fishes[4].BodyLength * 3,
                //    0, -myMission.TeamsRef[1].Fishes[4].BodyWidth * 6);

                myMission.TeamsRef[1].Fishes[3].PositionMm = new xna.Vector3(
                   myMission.TeamsRef[1].Fishes[3].BodyLength * 2,
                    0, myMission.TeamsRef[1].Fishes[3].BodyWidth * 32);
                myMission.TeamsRef[1].Fishes[4].PositionMm = new xna.Vector3(
                   myMission.TeamsRef[1].Fishes[4].BodyLength * 2,
                    0, -myMission.TeamsRef[1].Fishes[4].BodyWidth * 32);
                //myMission.TeamsRef[1].Fishes[3].PositionMm = new xna.Vector3(
                //   f.RightMm - myMission.TeamsRef[1].Fishes[3].BodyLength * 6,
                //   0, myMission.TeamsRef[1].Fishes[3].BodyWidth * 30);
                //myMission.TeamsRef[1].Fishes[4].PositionMm = new xna.Vector3(
                // f.RightMm - myMission.TeamsRef[1].Fishes[4].BodyLength * 6,
                // 0, myMission.TeamsRef[1].Fishes[4].BodyWidth * 27);

                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    // 左半场队伍的仿真机器鱼朝右
                    myMission.TeamsRef[0].Fishes[j].BodyDirectionRad = 0;
                    // 右半场队伍的仿真机器鱼朝左
                    myMission.TeamsRef[1].Fishes[j].BodyDirectionRad = xna.MathHelper.Pi;
                }
            }
            CommonPara.TotalSeconds = 3 * 60;
            CommonPara.RemainingCycles = CommonPara.TotalSeconds * 1000 / CommonPara.MsPerCycle;

        }

        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真水球恢复默认位置
        /// </summary>
        public override void ResetBalls()
        {
            // 每次调用时都将唯一的仿真水球放到场地中心点
            Env.Balls[0].PositionMm = new xna.Vector3(0, 0, 0);
        }

        // LiYoubing 20110616
        /// <summary>
        /// 设置当前仿真使命所有参与队伍的所有仿真机器鱼鱼体和编号默认颜色的过程独立出来
        /// 在InitTeamsAndFishes调用一次用于初始化
        /// 在ResetTeamsAndFishes中则不调用以使得用户
        /// 可以在界面上修改仿真机器鱼鱼体和编号颜色并保持
        /// 为此还需要提供一个恢复默认颜色的功能
        /// 该功能需要调用此过程
        /// </summary>
        public override void ResetColorFishAndId()
        {
            for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
            {
                // 第0支队伍的仿真机器鱼前端色标即鱼体颜色为红色
                Teams[0].Fishes[j].ColorFish = Color.Red;
                // 第1支队伍的仿真机器鱼前端色标即鱼体颜色为黄色
                Teams[1].Fishes[j].ColorFish = Color.Yellow;
                // 两支队伍仿真机器鱼后端色标即编号颜色均为黑色
                Teams[0].Fishes[j].ColorId = Color.Black;
                Teams[1].Fishes[j].ColorId = Color.Black;
            }
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命相应的仿真环境各参数设置为默认值
        /// </summary>
        public override void ResetEnvironment()
        {
            // 将当前仿真使命涉及的仿真场地尺寸恢复默认值
            ResetField();

            // 将当前仿真使命涉及的全部仿真水球恢复默认位置
            ResetBalls();
        }

        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命使用的仿真场地尺寸恢复默认值
        /// 5V5场地改为默认值的1.5倍
        /// </summary>
        public override void ResetField()
        {
            Field f = Field.Instance();
            f.FieldLengthXMm = 4500;
            f.FieldLengthZMm = 3000;
            f.FieldCalculation();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命特有的内部变量设置为默认值
        /// </summary>
        private void ResetInternalVariables()
        {
            // 重启或改变仿真使命类型时设置关于犯规的时间（周期数）参数为默认值
            ResetCyclesAboutFouled();

            // 重启或改变仿真使命类型时关于死球和比赛类型的参数为默认值
            ResetDeadBallAndMatchType();

            HtMissionVariables["CompetitionPeriod"] = ((int)MatchHelper.CompetitionPeriod.NormalTime).ToString();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时设置关于犯规的时间（周期数）参数为默认值
        /// </summary>
        private void ResetCyclesAboutFouled()
        {
            TimesFouled = 10000;
            CyclesFouled = TimesFouled / CommonPara.MsPerCycle;
            TimesInForbiddenzoneWhileFouled = 5000;
            CyclesInForbiddenzoneWhileFouled = TimesInForbiddenzoneWhileFouled / CommonPara.MsPerCycle;
            TimesInGoalWhileFouled = 5000;
            CyclesInGoalWhileFouled = TimesInGoalWhileFouled / CommonPara.MsPerCycle;
        }

        /// <summary>
        /// 重启或改变仿真使命类型时设置关于死球和比赛类型的参数为默认值
        /// </summary>
        private void ResetDeadBallAndMatchType()
        {
            // 判断死球状态持续时间设定为10秒
            MaxDurationTimeDeadBall = 10 * 1000 / CommonPara.MsPerCycle;
            PenaltyKickCount = 0;
            DurationTimeDeadBall = 0;
            CompetitionPeriod = (int)MatchHelper.CompetitionPeriod.NormalTime;
            DeadBallTeamACount = 0;
            DeadBallTeamBCount = 0;
        }
        #endregion

        #region public methods that implement IMission interface
        /// <summary>
        /// 实现IMission中的接口用于 设置当前使命中各对象的初始值
        /// </summary>
        public override void SetMission()
        {
            // 重启或改变仿真使命类型时将该仿真使命相应的仿真环境各参数设置为默认值
            ResetEnvironment();

            // 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值
            ResetTeamsAndFishes();
            for (int i = 0; i < Teams.Count; i++)
            {// 恢复各支队伍得分和队名为默认值
                // 这个操作在需要频繁恢复默认场景的仿真使命中不能放ResetTeamsAndFishes里做
                // 因为ResetTeamsAndFishes在恢复默认场景时需要调用而此时不能改变得分和队伍名称
                Teams[i].Para.Score = 0;
                Teams[i].Para.Name = "Team" + (i + 1).ToString();
            }
            
            // 重启或改变仿真使命类型时将当前选中使命的动态对象（仿真机器鱼和仿真水球）的部分运动学设置为默认值
            ResetSomeLocomotionPara();

            // 重启或改变仿真使命类型时将当前选中使命的决策数组各元素设置为默认值
            ResetDecision();

            // 重启或改变仿真使命类型时将该仿真使命特有的内部变量设置为默认值
            ResetInternalVariables();
        }

        /// <summary>
        /// 处理对抗赛5VS5具体仿真使命的控制规则
        /// </summary>
        public override void ProcessControlRules()
        {
            if (CompetitionPeriod == (int)MatchHelper.CompetitionPeriod.PenaltyKickTime)
            {// 点球阶段每支队伍只有第0条仿真机器鱼可以活动
                for (int i = 0; i < MyMission.Instance().ParasRef.TeamCount; i++)
                {
                    for (int j = 1; j < MyMission.Instance().ParasRef.FishCntPerTeam; j++)
                    {// 留下第0条仿真机器鱼不强制静止
                        Teams[i].Fishes[j].VelocityMmPs = 0;
                        Teams[i].Fishes[j].AngularVelocityRadPs = 0;
                    }
                }
            }

            //清空犯规次数计数    added by zhangbo 20111214
            countTeam1FishFouled = 0;
            countTeam2FishFouled = 0;

            // 处理死球判断提示和响应任务 added by renjing 20110310
            DeadBallHandler();

            // 处理犯规情况1的判断提示和响应任务 
            FoulType1Handler();

            // 处理犯规情况2的判断提示和响应任务
            FoulType2Handler();

            //处理犯规情况3的判断提示和响应任务 added by zhangbo 20111213
            FoulType3Handler();
            
            // 处理进球判断提示和响应任务 added by renjing 20110310
            GoalHandler();

            // 处理倒计时递减到0比赛结束判断提示和响应任务 added by renjing 20110310
            GameOverHandler();

            // 处理半场交换任务 added by liushu 20110314
            HalfCourtExchangeHandler();

            // 更新当前使命类特有的需要传递给策略的变量名和变量值
            UpdateHtMissionVariables();
        }

        #region 对抗赛5VS5仿真使命控制规则所需私有变量
        /// <summary>
        /// 防守机器鱼在禁区内犯规时最大停留时间（单位：毫秒）
        /// </summary>
        private int TimesInForbiddenzoneWhileFouled;

        /// <summary>
        /// 防守机器鱼在禁区内犯规时最大停留周期数（犯规1）
        /// </summary>
        private int CyclesInForbiddenzoneWhileFouled;

        /// <summary>
        /// 防守机器鱼在球门内犯规时最大停留时间（单位：毫秒）
        /// </summary>
        private int TimesInGoalWhileFouled;

        /// <summary>
        /// 防守机器鱼在球门内犯规时最大停留周期数（犯规2）
        /// </summary>
        private int CyclesInGoalWhileFouled;

        /// <summary>
        /// 机器鱼一次犯规需被罚下场时间（单位：毫秒）
        /// </summary>
        private int TimesFouled;

        /// <summary>
        /// 机器鱼一次犯规需被罚下场周期数
        /// </summary>
        private int CyclesFouled;

        /// <summary>
        /// 已经进入死球状态的周期数
        /// </summary>
        private int DurationTimeDeadBall;

        /// <summary>
        /// 提示死球状态前进入准死球状态需要持续的周期数
        /// </summary>
        private int MaxDurationTimeDeadBall;

        /// <summary>
        /// 已经进入死球状态的A队鱼的数目
        /// </summary>
        private int DeadBallTeamACount;

        /// <summary>
        /// 已经进入死球状态的B队鱼的数目
        /// </summary>
        private int DeadBallTeamBCount;

        /// <summary>
        /// 比赛阶段 added by renjing 20110310
        /// </summary>
        private int CompetitionPeriod;

        /// <summary>
        /// Team1犯规机器鱼数量 added by zhangbo 20111213
        /// </summary>
        int countTeam1FishFouled;

        /// <summary>
        /// Team2犯规机器鱼数量 added by zhangbo 20111213
        /// </summary>
        int countTeam2FishFouled;

        /// <summary>
        /// 点球进行的次数
        /// </summary>
        private int PenaltyKickCount;
        #endregion

        #region 对抗赛5VS5仿真使命控制规则具体处理过程      
        #region 死球判断及处理
        // Added by renjing 20110310 Modified by LiYoubing 20110515
        /// <summary>
        /// 死球状态后，场上机器鱼的处理
        /// </summary>
        private void DeadBallHandler()
        {
            if (IsDeadBall())
            {
                MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef,
                    "DeadBall Now!", "Comfirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                //左上争球点
                Point pointFreeKickoff1 = new Point(Env.FieldInfo.LeftMm / 2, Env.FieldInfo.TopMm / 2);
                //左下争球点
                Point pointFreeKickoff2 = new Point(Env.FieldInfo.LeftMm / 2, Env.FieldInfo.BottomMm / 2);
                //右上争球点
                Point pointFreeKickoff3 = new Point(Env.FieldInfo.RightMm / 2, Env.FieldInfo.TopMm / 2);
                //右下争球点
                Point pointFreeKickoff4 = new Point(Env.FieldInfo.RightMm / 2, Env.FieldInfo.BottomMm / 2);
                //临时变量保存争球点
                Point pointFreeKickoff = pointFreeKickoff1;

                double pointFreeKickoffToBall;
                pointFreeKickoffToBall = Math.Sqrt((pointFreeKickoff.X - Env.Balls[0].PositionMm.X)
                    * (pointFreeKickoff.X - Env.Balls[0].PositionMm.X)
                    + (pointFreeKickoff.Y - Env.Balls[0].PositionMm.Z)
                    * (pointFreeKickoff.Y - Env.Balls[0].PositionMm.Z));
                //左上争球点到球的距离
                double pointFreeKickoff1ToBall;
                pointFreeKickoff1ToBall = Math.Sqrt((pointFreeKickoff1.X - Env.Balls[0].PositionMm.X)
                    * (pointFreeKickoff1.X - Env.Balls[0].PositionMm.X)
                    + (pointFreeKickoff1.Y - Env.Balls[0].PositionMm.Z)
                    * (pointFreeKickoff1.Y - Env.Balls[0].PositionMm.Z));
                //左下争球点到球的距离
                double pointFreeKickoff2ToBall;
                pointFreeKickoff2ToBall = Math.Sqrt((pointFreeKickoff2.X - Env.Balls[0].PositionMm.X)
                    * (pointFreeKickoff2.X - Env.Balls[0].PositionMm.X)
                    + (pointFreeKickoff2.Y - Env.Balls[0].PositionMm.Z)
                    * (pointFreeKickoff2.Y - Env.Balls[0].PositionMm.Z));
                //右上争球点到球的距离
                double pointFreeKickoff3ToBall;
                pointFreeKickoff3ToBall = Math.Sqrt((pointFreeKickoff3.X - Env.Balls[0].PositionMm.X)
                    * (pointFreeKickoff3.X - Env.Balls[0].PositionMm.X)
                    + (pointFreeKickoff3.Y - Env.Balls[0].PositionMm.Z)
                    * (pointFreeKickoff3.Y - Env.Balls[0].PositionMm.Z));
                //右下争球点到球的距离
                double pointFreeKickoff4ToBall;
                pointFreeKickoff4ToBall = Math.Sqrt((pointFreeKickoff4.X - Env.Balls[0].PositionMm.X)
                    * (pointFreeKickoff4.X - Env.Balls[0].PositionMm.X)
                    + (pointFreeKickoff4.Y - Env.Balls[0].PositionMm.Z)
                    * (pointFreeKickoff4.Y - Env.Balls[0].PositionMm.Z));

                if (pointFreeKickoff2ToBall < pointFreeKickoffToBall)
                {
                    pointFreeKickoff = pointFreeKickoff2;
                    pointFreeKickoffToBall = pointFreeKickoff2ToBall;
                }
                if (pointFreeKickoff3ToBall < pointFreeKickoffToBall)
                {
                    pointFreeKickoff = pointFreeKickoff3;
                    pointFreeKickoffToBall = pointFreeKickoff3ToBall;
                }
                if (pointFreeKickoff4ToBall < pointFreeKickoffToBall)
                {
                    pointFreeKickoff = pointFreeKickoff4;
                }

                // 仿真水球速度置为0
                Env.Balls[0].VelocityMmPs = 0;
                Env.Balls[0].VelocityDirectionRad = 0;
                // 将仿真水球的中心坐标置成所选出的争球点的坐标
                Env.Balls[0].PositionMm.X = pointFreeKickoff.X;
                Env.Balls[0].PositionMm.Z = pointFreeKickoff.Y;

                #region 仿真机器鱼的位姿设置
                // 交换半场前/后左半场的队伍分别为第0/1支队伍
                Team<Fish5V5> teamLeft = (CommonPara.IsExchangedHalfCourt == true) ? Teams[1] : Teams[0];
                // 交换半场前/后右半场的队伍分别为第1/0支队伍
                Team<Fish5V5> teamRight = (CommonPara.IsExchangedHalfCourt == true) ? Teams[0] : Teams[1];

                //Modified By Zhangbo 20111214
                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {// 将两支队伍的仿真机器鱼对称置于争球点左右 用相对距离 不用绝对数字
                    if (pointFreeKickoff.X < 0)
                    {
                        if (teamLeft.Fishes[j].PositionMm.X < -Env.FieldInfo.CenterCircleRadiusMm)
                        {
                            teamLeft.Fishes[j].PositionMm.X = pointFreeKickoff.X
                                - teamLeft.Fishes[j].BodyLength - Env.Balls[0].RadiusMm;
                            teamLeft.Fishes[j].PositionMm.Z = pointFreeKickoff.Y + 3 * teamLeft.Fishes[j].BodyWidth * (j - 1);
                            teamLeft.Fishes[j].BodyDirectionRad = 0;
                        }
                        if (teamRight.Fishes[j].PositionMm.X < -Env.FieldInfo.CenterCircleRadiusMm)
                        {
                            teamRight.Fishes[j].PositionMm.X = pointFreeKickoff.X
                                + teamRight.Fishes[j].BodyLength + Env.Balls[0].RadiusMm;
                            teamRight.Fishes[j].BodyDirectionRad = (float)Math.PI;
                            if (j == 0)
                            {
                                teamRight.Fishes[j].PositionMm.Z = pointFreeKickoff.Y - 3 * teamRight.Fishes[j].BodyWidth;
                            }
                            else
                            {
                                teamRight.Fishes[j].PositionMm.Z = pointFreeKickoff.Y + 3 * teamRight.Fishes[j].BodyWidth * (j - 3);
                            }
                        }
                    }
                    else
                    {
                        if (teamRight.Fishes[j].PositionMm.X > Env.FieldInfo.CenterCircleRadiusMm)
                        {
                            teamRight.Fishes[j].PositionMm.X = pointFreeKickoff.X
                                + teamRight.Fishes[j].BodyLength + Env.Balls[0].RadiusMm;
                            teamRight.Fishes[j].PositionMm.Z = pointFreeKickoff.Y + 3 * teamRight.Fishes[j].BodyWidth * (j - 1);
                            teamRight.Fishes[j].BodyDirectionRad = (float)Math.PI;
                        }
                        if (teamLeft.Fishes[j].PositionMm.X > Env.FieldInfo.CenterCircleRadiusMm)
                        {
                            teamLeft.Fishes[j].PositionMm.X = pointFreeKickoff.X
                                - teamLeft.Fishes[j].BodyLength - Env.Balls[0].RadiusMm;
                            teamLeft.Fishes[j].BodyDirectionRad = 0;
                            if (j == 0)
                            {
                                teamLeft.Fishes[j].PositionMm.Z = pointFreeKickoff.Y - 3 * teamLeft.Fishes[j].BodyWidth;
                            }
                            else
                            {
                                teamLeft.Fishes[j].PositionMm.Z = pointFreeKickoff.Y + 3 * teamLeft.Fishes[j].BodyWidth * (j - 3);
                            }
                        }
                    }
                    //teamLeft.Fishes[j].BodyDirectionRad = 0;
                    //teamRight.Fishes[j].BodyDirectionRad = (float)Math.PI;
                }
                
                #endregion
            }
        }

        // Added by renjing 20110310 Modified by LiYoubing 20110515
        /// <summary>
        /// 死球判断函数
        /// </summary>
        /// <returns>true：死球 false：没有死球</returns>
        private bool IsDeadBall()
        {
            if (Env.Balls[0].PositionMm.X <= (Env.FieldInfo.GoalDepthMm + Env.FieldInfo.LeftMm + Env.Balls[0].RadiusMm + 50)
                && Env.Balls[0].PositionMm.Z <= Env.FieldInfo.TopMm + Env.Balls[0].RadiusMm + 50)
            {// 仿真水球在左上角落
                return IsDeadBall(Env.Balls[0]);
            }
            else if (Env.Balls[0].PositionMm.X <= (Env.FieldInfo.GoalDepthMm + Env.FieldInfo.LeftMm + Env.Balls[0].RadiusMm + 50)
                && Env.Balls[0].PositionMm.Z >= Env.FieldInfo.BottomMm - Env.Balls[0].RadiusMm - 50)
            {// 仿真水球在左下角落
                return IsDeadBall(Env.Balls[0]);
            }
            else if (Env.Balls[0].PositionMm.X >= (Env.FieldInfo.RightMm - Env.FieldInfo.GoalDepthMm - Env.Balls[0].RadiusMm - 50)
                && Env.Balls[0].PositionMm.Z <= Env.FieldInfo.TopMm + Env.Balls[0].RadiusMm + 50)
            {// 仿真水球在右上角落
                return IsDeadBall(Env.Balls[0]);
            }
            else if (Env.Balls[0].PositionMm.X >= (Env.FieldInfo.RightMm - Env.FieldInfo.GoalDepthMm - Env.Balls[0].RadiusMm - 50)
                && Env.Balls[0].PositionMm.Z >= Env.FieldInfo.BottomMm - Env.Balls[0].RadiusMm - 50)
            {// 仿真水球在右下角落
                return IsDeadBall(Env.Balls[0]);
            }
            else
            {// 仿真水球不在死球区域
                DurationTimeDeadBall = 0;
                DeadBallTeamACount = 0;
                DeadBallTeamBCount = 0;
            }
            return false;
        }

        // Added by LiYoubing 20110515
        /// <summary>
        /// 判断仿真水球和所有仿真机器鱼之间是否满足死球判断条件
        /// </summary>
        /// <param name="ball">待判断的仿真水球对象</param>
        /// <returns>仿真水球和仿真机器鱼之间是否满足死球条件true满足false不满足</returns>
        private bool IsDeadBall(Ball ball)
        {
            for (int i = 0; i < CommonPara.TeamCount; i++)
            {
                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    if ((Teams[i].Fishes[j].PositionMm - Env.Balls[0].PositionMm).Length() <= 120
                        || Teams[i].Fishes[j].Collision.Contains(CollisionType.FISH_BALL))
                    {// 仿真机器鱼前端中心和仿真水球中心之间向量长度小于等于120毫米或仿真水球和仿真机器鱼发生了碰撞
                        if (i == 0)
                        {
                            DeadBallTeamACount++;
                        }
                        else
                        {
                            DeadBallTeamBCount++;
                        }
                    }
                    if (DeadBallTeamACount >= 1 && DeadBallTeamBCount >= 1)
                    {// 2支队伍均至少有一条仿真机器鱼与仿真水球间满足死球判断条件
                        DurationTimeDeadBall++;
                        if (DurationTimeDeadBall >= MaxDurationTimeDeadBall)
                        {// 满足死球判断条件达到设定的周期数则判断为死球
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        #endregion

        #region 犯规情况1判断及处理
        // Add By ChenPenghui 2010-04-01 Modified by renjing 20110310 Modified by zhangbo 20111214 
        /// <summary>
        /// 处理犯规情况1：仿真水球在己方禁区时，己方仿真机器鱼超过三条在己方禁区内，并且持续时间超过一定周期数
        /// </summary>
        private void FoulType1Handler()
        {
            #region 犯规判断及提示
            if (IsBallInForbiddenZone(Env.Balls[0], true))
            {// 仿真水球位于左边禁区
                // 交换了半场则第1支队伍位于左半场否则第0支队伍位于左半场
                Team<Fish5V5> team = (CommonPara.IsExchangedHalfCourt == true) ? Teams[1] : Teams[0];

                // 处理左半场队伍（交换半场前为第0支交换半场后为第1支）在左禁区的犯规情况1
                //FoulType1Handler(ref team, ref arrFishIndexFouled, true);
                FoulType1Handler(ref team, true);
            }
            else if (IsBallInForbiddenZone(Env.Balls[0], false))
            {// 仿真水球位于右边禁区
                // 交换了半场则第0支队伍位于左半场否则第1支队伍位于左半场
                Team<Fish5V5> team = (CommonPara.IsExchangedHalfCourt == true) ? Teams[0] : Teams[1];

                // 处理右半场队伍（交换半场前为第1支交换半场后为第0支）在右禁区的犯规情况1
                //FoulType1Handler(ref team, ref arrFishIndexFouled, false);
                FoulType1Handler(ref team, false);

            }
            else
            {// 仿真水球不在禁区 
                for (int i = 0; i < CommonPara.TeamCount; i++)
                {
                    for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                    {
                        if (Teams[i].Fishes[j].FoulFlag == FoulType5V5.NONE)
                        {// 且犯规标志为NONE则恢复仿真机器鱼犯规处理相关变量为默认值
                            Teams[i].Fishes[j].CountInOwnForbiddenZone = 0;
                            Teams[i].Fishes[j].CountCyclesFouled = 0;
                            Teams[i].Fishes[j].CountInOwnGoalArea = 0;
                        }
                    }
                }
            }
            #endregion
            #region 犯规处理
            for (int i = 0; i < CommonPara.TeamCount; i++)
            {// 犯规处理
                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {// 使用countFishFouled记录单支队伍罚下仿真机器鱼的条数用于确定罚下场后的摆放位置
                    Fish5V5 fish = Teams[i].Fishes[j];
                    if (fish.FoulFlag == FoulType5V5.FOUl1)
                    {
                        if (i == 0)
                        {
                            countTeam1FishFouled++;
                        }
                        else
                        {
                            countTeam2FishFouled++;
                        }
                        fish.CountCyclesFouled++;
                        if (fish.CountCyclesFouled < CyclesFouled)
                        {// 罚下场的周期数尚未达到设定的值
                            int flag = (Teams[i].Para.MyHalfCourt == HalfCourt.LEFT) ? -1 : 1;
                            //fish.PositionMm.X = flag * countFishFouled * (2 * fish.BodyWidth);

                            int countFishFouled = (i == 0) ? countTeam1FishFouled : countTeam2FishFouled;
                            //实现犯规机器鱼以x=0为中心交替放置added by zhangbo 20111214
                            fish.PositionMm.X = (float)(0.25f + 0.5f * (countFishFouled - 0.5f) * Math.Pow(-1, countFishFouled)) * (2 * fish.BodyWidth);
                            if ((CommonPara.IsExchangedHalfCourt == false && i == 0) || (CommonPara.IsExchangedHalfCourt == true && i == 1))
                            {
                                fish.PositionMm.Z = Env.FieldInfo.TopMm - fish.BodyLength / 2;
                                fish.BodyDirectionRad = (float)Math.PI / 2;
                            }
                            else
                            {
                                fish.PositionMm.Z = -(Env.FieldInfo.TopMm - fish.BodyLength / 2);
                                fish.BodyDirectionRad = -(float)Math.PI / 2;
                            }
                        }
                        else
                        {// 处理完毕恢复各相关变量的默认值
                            fish.FoulFlag = FoulType5V5.NONE;
                            fish.CountInOwnForbiddenZone = 0;
                            fish.CountCyclesFouled = 0;
                        }
                    }
                }
            }
            #endregion
        }

        // Added by renjing 20110310 Modified by LiYoubing 20110515
        /// <summary>
        /// 处理某支队伍在己方半场的犯规情况1
        /// </summary>
        /// <param name="team">待处理的仿真使命参与队伍（仿真机器鱼为具体使命的仿真机器鱼）</param>
        /// <param name="bIsLeftHalfCourt">待判断的是否左禁区true判断左禁区false判断右禁区</param>
        //private void FoulType1Handler(ref Team<Fish5V5> team, ref List<int> arrFishIndexFouled, bool bIsLeftHalfCourt)
        private void FoulType1Handler(ref Team<Fish5V5> team, bool bIsLeftHalfCourt)
        {
            // 犯规的仿真机器鱼在队伍中的索引
            List<int> arrFishIndexFouled = new List<int>();

            for (int j = 0; j < team.Fishes.Count; j++)
            {
                if (IsRoboFishInForbiddenZone(team.Fishes[j], bIsLeftHalfCourt))
                {// 仿真机器鱼在己方禁区内
                    team.Fishes[j].CountInOwnForbiddenZone++;
                }
                else
                {// 仿真机器鱼不在己方禁区内
                    team.Fishes[j].CountInOwnForbiddenZone = 0;
                }
                if (team.Fishes[j].CountInOwnForbiddenZone > CyclesInForbiddenzoneWhileFouled)
                {// 仿真机器鱼在己方禁区停留一定周期数以上
                    arrFishIndexFouled.Add(j);
                }
            }
            if (arrFishIndexFouled.Count >= 3)
            {//允许两条鱼在己方禁区内的代码，有犯规1情况出现时的处理
                #region 允许两条鱼在己方禁区内的代码
                int theLastFishEnterIndex = arrFishIndexFouled[0];
                int minTimeEnterInForbiddenZone = team.Fishes[arrFishIndexFouled[0]].CountInOwnForbiddenZone;
                for (int i = 1; i < arrFishIndexFouled.Count; i++)
                {
                    if (team.Fishes[arrFishIndexFouled[i]].CountInOwnForbiddenZone < minTimeEnterInForbiddenZone)
                    {
                        theLastFishEnterIndex = arrFishIndexFouled[i];
                    }
                }
                team.Fishes[theLastFishEnterIndex].FoulFlag = FoulType5V5.FOUl1;
                team.Fishes[theLastFishEnterIndex].CountCyclesFouled = 0;
                team.Fishes[theLastFishEnterIndex].CountInOwnForbiddenZone = 0;
                //indexFouledFish += Convert.ToString(theLastFishEnterIndex + 1) + " ";
                #endregion
                MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, 
                    string.Format("FoulType1:\n【{0}】Fish {1} Fouled!\n", 
                    team.Para.Name, theLastFishEnterIndex + 1), 
                    "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Added by LiYoubing 20110515
        /// <summary>
        /// 判断仿真水球是否完全进入左/右禁区（包括球门）区域
        /// </summary>
        /// <param name="ball">仿真水球</param>
        /// <param name="bIsLeftHalfCourt">待判断的是否左禁区true判断左禁区false判断右禁区</param>
        /// <returns>仿真水球位于禁区内返回true否则返回false</returns>
        private bool IsBallInForbiddenZone(Ball ball, bool bIsLeftHalfCourt)
        {
            int tmpMm = (bIsLeftHalfCourt == true) ? Env.FieldInfo.LeftMm : Env.FieldInfo.RightMm;
            int flag = (bIsLeftHalfCourt == true) ? 1 : -1;
            bool bIsXInForbiddenZone = (bIsLeftHalfCourt == true) 
                ? (ball.PositionMm.X <= tmpMm + flag * (Env.FieldInfo.GoalDepthMm 
                + Env.FieldInfo.ForbiddenZoneLengthXMm - ball.RadiusMm)) 
                : (ball.PositionMm.X >= tmpMm + flag * (Env.FieldInfo.GoalDepthMm 
                + Env.FieldInfo.ForbiddenZoneLengthXMm - ball.RadiusMm));

            return bIsXInForbiddenZone 
                && (ball.PositionMm.Z >= ball.RadiusMm - Env.FieldInfo.ForbiddenZoneLengthZMm / 2) 
                && (ball.PositionMm.Z <= Env.FieldInfo.ForbiddenZoneLengthZMm / 2 - ball.RadiusMm);
        }

        // Added by LiYoubing 20110515
        /// <summary>
        /// 判断仿真机器鱼鱼体前方矩形中心点（PositionMm）是否位于左/右禁区（包括球门）区域
        /// </summary>
        /// <param name="fish">仿真机器鱼</param>
        /// <param name="bIsLeftHalfCourt">待判断的是否左禁区true判断左禁区false判断右禁区</param>
        /// <returns>仿真机器鱼鱼体前方矩形中心点（PositionMm）位于禁区内返回true否则返回false</returns>
        private bool IsRoboFishInForbiddenZone(RoboFish fish, bool bIsLeftHalfCourt)
        {
            int tmpMm = (bIsLeftHalfCourt == true) ? Env.FieldInfo.LeftMm : Env.FieldInfo.RightMm;
            int flag = (bIsLeftHalfCourt == true) ? 1 : -1;
            bool bIsXInForbiddenZone = (bIsLeftHalfCourt == true) 
                ? (fish.PositionMm.X <= tmpMm + flag * (Env.FieldInfo.GoalDepthMm 
                + Env.FieldInfo.ForbiddenZoneLengthXMm)) 
                : (fish.PositionMm.X >= tmpMm + flag * (Env.FieldInfo.GoalDepthMm 
                + Env.FieldInfo.ForbiddenZoneLengthXMm));

            return bIsXInForbiddenZone 
                && (fish.PositionMm.Z >= -Env.FieldInfo.ForbiddenZoneLengthZMm / 2) 
                && (fish.PositionMm.Z <= Env.FieldInfo.ForbiddenZoneLengthZMm / 2);
        }
        #endregion

        #region 犯规情况2判断及处理
        // Added by renjing 20110310 Modified by LiYoubing 20110515
        /// <summary>
        /// 处理犯规情况2：仿真水球在己方禁区时，己方仿真机器鱼进入己方球门内，并且持续时间超过一定周期数
        /// </summary>
        private void FoulType2Handler()
        {
            #region 犯规判断及提示
            if (IsBallInForbiddenZone(Env.Balls[0], true))
            {// 仿真水球位于左边禁区
                // 交换了半场则第1支队伍位于左半场否则第0支队伍位于左半场
                Team<Fish5V5> team = (CommonPara.IsExchangedHalfCourt == true) ? Teams[1] : Teams[0];

                // 处理左半场队伍（交换半场前为第0支交换半场后为第1支）在左禁区的犯规情况1
                //FoulType2Handler(ref team,ref arrFishIndexFouled, true);
                FoulType2Handler(ref team, true);
            }
            else if (IsBallInForbiddenZone(Env.Balls[0], false))
            {// 仿真水球位于右边禁区
                // 交换了半场则第0支队伍位于左半场否则第1支队伍位于左半场
                Team<Fish5V5> team = (CommonPara.IsExchangedHalfCourt == true) ? Teams[0] : Teams[1];

                // 处理右半场队伍（交换半场前为第1支交换半场后为第0支）在右禁区的犯规情况1
                //FoulType2Handler(ref team, ref arrFishIndexFouled, false);
                FoulType2Handler(ref team, false);
            }
            else
            {// 仿真水球不在禁区 
                for (int i = 0; i < CommonPara.TeamCount; i++)
                {
                    for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                    {
                        if (Teams[i].Fishes[j].FoulFlag == FoulType5V5.NONE)
                        {// 且犯规标志为NONE则恢复仿真机器鱼犯规处理相关变量为默认值
                            Teams[i].Fishes[j].CountInOwnForbiddenZone = 0;
                            Teams[i].Fishes[j].CountCyclesFouled = 0;
                            Teams[i].Fishes[j].CountInOwnGoalArea = 0;
                        }
                    }
                }
            }
            #endregion
            #region 犯规处理 
            for (int i = 0; i < CommonPara.TeamCount; i++)
            {// 犯规处理 Modified by zhangbo 20111214
                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {// 使用countFishFouled记录单支队伍罚下仿真机器鱼的条数用于确定罚下场后的摆放位置
                    Fish5V5 fish = Teams[i].Fishes[j];
                    if (fish.FoulFlag == FoulType5V5.FOUl2)
                    {
                        if (i == 0)
                        {
                            countTeam1FishFouled++;
                        }
                        else
                        {
                            countTeam2FishFouled++;
                        }
                        fish.CountCyclesFouled++;
                        if (fish.CountCyclesFouled < CyclesFouled)
                        {// 罚下场的周期数尚未达到设定的值
                            int flag = (Teams[i].Para.MyHalfCourt == HalfCourt.LEFT) ? -1 : 1;
                            //fish.PositionMm.X = flag * countFishFouled * (2 * fish.BodyWidth);

                            int countFishFouled = (i == 0) ? countTeam1FishFouled : countTeam2FishFouled;
                            //实现犯规机器鱼以x=0为中心交替放置added by zhangbo 20111214
                            fish.PositionMm.X = (float)(0.25f + 0.5f * (countFishFouled - 0.5f) * Math.Pow(-1, countFishFouled)) * (2 * fish.BodyWidth);
                            if ((CommonPara.IsExchangedHalfCourt == false && i == 0) || (CommonPara.IsExchangedHalfCourt == true && i == 1))
                            {
                                fish.PositionMm.Z = Env.FieldInfo.TopMm - fish.BodyLength / 2;
                                fish.BodyDirectionRad = (float)Math.PI / 2;
                            }
                            else
                            {
                                fish.PositionMm.Z = -(Env.FieldInfo.TopMm - fish.BodyLength / 2);
                                fish.BodyDirectionRad = -(float)Math.PI / 2;
                            }
                        }
                        else
                        {// 处理完毕恢复各相关变量的默认值
                            fish.FoulFlag = FoulType5V5.NONE;
                            fish.CountCyclesFouled = 0;
                            fish.CountInOwnGoalArea = 0;
                        }
                    }
                }
            }
            #endregion
        }

        // Added by LiYoubing 20110515
        /// <summary>
        /// 处理某支队伍在己方半场的犯规情况2
        /// </summary>
        /// <param name="team">待处理的仿真使命参与队伍（仿真机器鱼为具体使命的仿真机器鱼）</param>
        /// <param name="bIsLeftHalfCourt">待判断的是否左球门true判断左球门false判断右球门</param>
        //private void FoulType2Handler(ref Team<Fish5V5> team,ref List<int> arrFishIndexFouled, bool bIsLeftHalfCourt)
        private void FoulType2Handler(ref Team<Fish5V5> team, bool bIsLeftHalfCourt)
        {
            // 犯规的仿真机器鱼在队伍中的索引
            List<int> arrFishIndexFouled = new List<int>();

            for (int j = 0; j < team.Fishes.Count; j++)
            {
                if (IsRoboFishInGoal(team.Fishes[j], bIsLeftHalfCourt))
                {// 仿真机器鱼在己方球门内
                    team.Fishes[j].CountInOwnGoalArea++;
                }
                else
                {// 仿真机器鱼不在己方球门内
                    team.Fishes[j].CountInOwnGoalArea = 0;
                }
                if (team.Fishes[j].CountInOwnGoalArea > CyclesInGoalWhileFouled)
                {// 仿真机器鱼在己方球门停留一定周期数以上
                    arrFishIndexFouled.Add(j);
                }
            }
            if (arrFishIndexFouled.Count >= 1)
            {// 犯规情况2提示
                for (int i = 0; i < arrFishIndexFouled.Count; i++)
                {
                    team.Fishes[arrFishIndexFouled[i]].FoulFlag = FoulType5V5.FOUl2;
                    team.Fishes[arrFishIndexFouled[i]].CountCyclesFouled = 0;
                    team.Fishes[arrFishIndexFouled[i]].CountInOwnGoalArea = 0;
                    MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, 
                        string.Format("FoulType2:\n【{0}】Fish {1} Fouled!\n",
                        team.Para.Name, arrFishIndexFouled[i] + 1),
                        "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // Added by LiYoubing 20110515
        /// <summary>
        /// 判断仿真机器鱼鱼体前方矩形中心点（PositionMm）是否位于左/右球门区域
        /// </summary>
        /// <param name="fish">仿真机器鱼</param>
        /// <param name="bIsLeftHalfCourt">待判断的是否左球门true判断左球门false判断右球门</param>
        /// <returns>仿真机器鱼鱼体前方矩形中心点（PositionMm）位于球门内返回true否则返回false</returns>
        private bool IsRoboFishInGoal(RoboFish fish, bool bIsLeftHalfCourt)
        {
            int tmpMm = (bIsLeftHalfCourt == true) ? Env.FieldInfo.LeftMm : Env.FieldInfo.RightMm;
            int flag = (bIsLeftHalfCourt == true) ? 1 : -1;
            bool bIsXInGoal = (bIsLeftHalfCourt == true) 
                ? (fish.PositionMm.X <= tmpMm + flag * Env.FieldInfo.GoalDepthMm)
                : (fish.PositionMm.X >= tmpMm + flag * Env.FieldInfo.GoalDepthMm);

            return bIsXInGoal && (fish.PositionMm.Z >= -Env.FieldInfo.GoalWidthMm / 2)
                    && (fish.PositionMm.Z <= Env.FieldInfo.GoalWidthMm / 2);
        }
        #endregion

        #region 犯规情况3判断及处理
        // Added by zhangbo20111211
        /// <summary>
        /// 处理犯规情况3：不同角色的仿真机器鱼活动范围固定，超出范围则罚到场外
        /// </summary>
        private void FoulType3Handler()
        {
            #region 犯规判断及提示
            //Team<Fish5V5> team = (CommonPara.IsExchangedHalfCourt == true) ? Teams[1] : Teams[0];
            //FoulType3Handler1(ref team);
            //FoulType3Handler2(ref team);
            if (CommonPara.IsExchangedHalfCourt == false)
            {
                Team<Fish5V5> LeftTeam = Teams[0];
                Team<Fish5V5> RightTeam = Teams[1];
                FoulType3Handler1(ref LeftTeam);
                FoulType3Handler2(ref RightTeam);
            }
            else
            {
                Team<Fish5V5> LeftTeam = Teams[1];
                Team<Fish5V5> RightTeam = Teams[0];
                FoulType3Handler1(ref LeftTeam);
                FoulType3Handler2(ref RightTeam);
            }
            #endregion

            #region 犯规处理
            for (int i = 0; i < CommonPara.TeamCount; i++)
            {// 犯规处理
                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {// 使用countFishFouled记录单支队伍罚下仿真机器鱼的条数用于确定罚下场后的摆放位置
                    Fish5V5 fish = Teams[i].Fishes[j];
                    if (fish.FoulFlag == FoulType5V5.FOUl3)
                    {
                        if (i == 0)
                        {
                            countTeam1FishFouled++;
                        }
                        else
                        {
                            countTeam2FishFouled++;
                        }
                        fish.CountCyclesFouled++;
                        if (fish.CountCyclesFouled < CyclesFouled)
                        {// 罚下场的周期数尚未达到设定的值
                            int flag = (Teams[i].Para.MyHalfCourt == HalfCourt.LEFT) ? -1 : 1;
                            //fish.PositionMm.X = flag * countFishFouled * (2 * fish.BodyWidth);
                          
                            int countFishFouled = (i == 0) ? countTeam1FishFouled : countTeam2FishFouled;
                            //实现犯规机器鱼以x=0为中心交替放置added by zhangbo 20111214
                            fish.PositionMm.X = (float)(0.25f + 0.5f * (countFishFouled - 0.5f) * Math.Pow(-1, countFishFouled)) * (2 * fish.BodyWidth);
                            if ((CommonPara.IsExchangedHalfCourt == false && i == 0) || (CommonPara.IsExchangedHalfCourt == true && i == 1))
                            {
                                fish.PositionMm.Z = Env.FieldInfo.TopMm - fish.BodyLength / 2;
                                fish.BodyDirectionRad = (float)Math.PI / 2;
                            }
                            else
                            {
                                fish.PositionMm.Z = -(Env.FieldInfo.TopMm - fish.BodyLength / 2);
                                fish.BodyDirectionRad = -(float)Math.PI / 2;
                            }
                        }
                        else
                        {// 处理完毕恢复各相关变量的默认值
                            fish.FoulFlag = FoulType5V5.NONE;
                            fish.CountCyclesFouled = 0;
                            fish.CountInOwnGoalArea = 0;
                        }
                    }
                }
            }
            #endregion
        }

        // Added by Zhangbo 20111213
        /// <summary>
        /// 处理犯规情况3,左半场队伍犯规
        /// </summary>
        /// <param name="team">待处理的仿真使命参与队伍（仿真机器鱼为具体使命的仿真机器鱼）</param>
        private void FoulType3Handler1(ref Team<Fish5V5> LeftTeam)
        {
            // 犯规的仿真机器鱼在队伍中的索引
            List<int> arrFishIndexFouled = new List<int>();

            for (int j = 1; j < 3; j++)
            {
                if (LeftTeam.Fishes[j].PositionMm.X > Env.FieldInfo.CenterCircleRadiusMm)
                arrFishIndexFouled.Add(j);
            }
            for (int j = 3; j < 5; j++)
            {
                if (LeftTeam.Fishes[j].PositionMm.X < -Env.FieldInfo.CenterCircleRadiusMm)
                    arrFishIndexFouled.Add(j);
            }

            if (arrFishIndexFouled.Count >= 1)
            {// 犯规情况3提示
                for (int i = 0; i < arrFishIndexFouled.Count; i++)
                {
                    LeftTeam.Fishes[arrFishIndexFouled[i]].FoulFlag = FoulType5V5.FOUl3;
                    LeftTeam.Fishes[arrFishIndexFouled[i]].CountCyclesFouled = 0;
                    LeftTeam.Fishes[arrFishIndexFouled[i]].CountInOwnGoalArea = 0;
                    MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef,
                        string.Format("FoulType3:\n【{0}】Fish {1} Fouled!\n",
                        LeftTeam.Para.Name, arrFishIndexFouled[i] + 1),
                        "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // Added by Zhangbo 20111213
        /// <summary>
        /// 处理犯规情况3,右半场队伍犯规
        /// </summary>
        /// <param name="team">待处理的仿真使命参与队伍（仿真机器鱼为具体使命的仿真机器鱼）</param>
        private void FoulType3Handler2(ref Team<Fish5V5> RightTeam)
        {
            // 犯规的仿真机器鱼在队伍中的索引
            List<int> arrFishIndexFouled = new List<int>();

            for (int j = 1; j < 3; j++)
            {
                if (RightTeam.Fishes[j].PositionMm.X < -Env.FieldInfo.CenterCircleRadiusMm)
                    arrFishIndexFouled.Add(j);
            }
            for (int j = 3; j < 5; j++)
            {
                if (RightTeam.Fishes[j].PositionMm.X > Env.FieldInfo.CenterCircleRadiusMm)
                    arrFishIndexFouled.Add(j);
            }

            if (arrFishIndexFouled.Count >= 1)
            {// 犯规情况3提示
                for (int i = 0; i < arrFishIndexFouled.Count; i++)
                {
                    RightTeam.Fishes[arrFishIndexFouled[i]].FoulFlag = FoulType5V5.FOUl3;
                    RightTeam.Fishes[arrFishIndexFouled[i]].CountCyclesFouled = 0;
                    RightTeam.Fishes[arrFishIndexFouled[i]].CountInOwnGoalArea = 0;
                    MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef,
                        string.Format("FoulType3:\n【{0}】Fish {1} Fouled!\n",
                        RightTeam.Para.Name, arrFishIndexFouled[i] + 1),
                        "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        #endregion

        #region 进球判断及处理
        // Added by renjing 20110310 Modified by LiYoubing 20110515
        /// <summary>
        /// 进球处理
        /// </summary>
        private void GoalHandler()
        {
            MyMission mymission = MyMission.Instance();
            if (IsBallInGoal(Env.Balls[0], true))
            {// 仿真水球进入左边球门
             //  交换半场前/后左球门的进球得分分别属于第1/0支队伍
                Team<Fish5V5> team = (CommonPara.IsExchangedHalfCourt == true) ? Teams[0] : Teams[1];

             // 处理左球门进球提示和得分等事项
                GoalHandler(ref team, true);
          //modified by longhainan 20120801
                ResetTeamsAndFishes();
                ResetBalls();
                ResetSomeLocomotionPara();
            }

            if (IsBallInGoal(Env.Balls[0], false))
            {// 仿真水球进入右边球门

             //modified by longhainan 20120815
                if (CompetitionPeriod == (int)MatchHelper.CompetitionPeriod.ShootoutTime)
                { 
                    //交换半场前/后右球门的进球得分分别属于第0/1支队伍
                    Team<Fish5V5> team = (CommonPara.IsExchangedHalfCourt == true) ? Teams[1] : Teams[0];
                    //处理右球门进球提示和得分等事项
                    GoalHandler(ref team, false);

                    ResetShootout();
                    ResetBalls();
                    ResetSomeLocomotionPara();
                    //CommonPara.TotalSeconds = 3 * 60;
                }
                else
                {  
                    //交换半场前/后右球门的进球得分分别属于第0/1支队伍
                    Team<Fish5V5> team = (CommonPara.IsExchangedHalfCourt == true) ? Teams[1] : Teams[0];

                    //处理右球门进球提示和得分等事项
                    GoalHandler(ref team, false);
                    //modified by longhainan 20120801

                    ResetGoalHandler();
                    ResetBalls();
                    ResetSomeLocomotionPara();
                }
      
            }

        }
        
        // Added by LiYoubing 20110515
        /// <summary>
        /// 处理某支队伍进球的情况
        /// </summary>
        /// <param name="team">待处理的仿真使命参与队伍（仿真机器鱼为具体使命的仿真机器鱼）</param>
        /// <param name="bIsLeftHalfCourt">待判断的是否左球门true判断左球门false判断右球门</param>
        private void GoalHandler(ref Team<Fish5V5> team, bool bIsLeftHalfCourt)
        {           
       //modified by longhainan,anyongyue 20120815
            if (CompetitionPeriod == (int)MatchHelper.CompetitionPeriod.ShootoutTime)
            {
                string strMsg = "";
                MyMission mymission = MyMission.Instance();
                if (IsBallInGoal(Env.Balls[0], false))
                {                 
                    
                    int Shootouttime = CommonPara.TotalSeconds*1000 - CommonPara.RemainingCycles * mymission.ParasRef.MsPerCycle;
                    int ShootouttimeSec = Shootouttime / 1000;
                    strMsg += string.Format("Congradulations!shootouttime is: {1:00}:{2:00}:{3:00}.",
                        Teams[0].Para.Score, ShootouttimeSec / 60, ShootouttimeSec % 60, Shootouttime % 1000);
                   // strMsg += string.Format("Congradulations!\nshootouttime is: {1:00}:{2:00}.",ShootouttimeSec / 60, ShootouttimeSec % 60);
                    //strMsg += string.Format("Congradulations!\nshootouttime is: {0}s", ShootouttimeSec);
                            
                }
                MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, strMsg,
                   "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            //else if (CompetitionPeriod == (int)MatchHelper.CompetitionPeriod.ShootoutTimes)
            //{
            //   string strMsg = "";
            //    MyMission mymission = MyMission.Instance();
            //    if (IsBallInGoal(Env.Balls[0], false))
            //    {                 
                    
            //        int Shootouttime = CommonPara.TotalSeconds*1000 - CommonPara.RemainingCycles * mymission.ParasRef.MsPerCycle;
            //        int ShootouttimeSec = Shootouttime / 1000;
            //        //strMsg += string.Format("Congradulations!shootout time is: {1:00}:{2:00}.",ShootouttimeSec / 60, ShootouttimeSec % 60);
            //        strMsg += string.Format("Congradulations!shootout time is: {0}s", ShootouttimeSec);
                            
            //    }
            //    MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, strMsg,
            //       "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //}
              
            else 
            {
                MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef,
               string.Format("Congradulations!\nGoaled:【{0}】 in 【{1}】\n",
               team.Para.Name, team.Para.MyHalfCourt),
               "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            for (int i = 0; i < CommonPara.TeamCount; i++)
            {// 解除犯规状态
                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    team.Fishes[j].FoulFlag = FoulType5V5.NONE;
                    team.Fishes[j].CountCyclesFouled = 0;
                    team.Fishes[j].CountInOwnGoalArea = 0;
                    team.Fishes[j].CountInOwnForbiddenZone = 0;
                }
            }

            if (CommonPara.IsRunning == true)
            {// 仿真使命运行完毕后Restart之前可以重复按Pause/Continue按钮弹出提示对话框但不必重复计分
                team.Para.Score++;  // 累积得分
            }
            if (CompetitionPeriod == (int)MatchHelper.CompetitionPeriod.ClutchShotTime
                || CompetitionPeriod == (int)MatchHelper.CompetitionPeriod.PenaltyKickTime)
            {// 在点球大战和制胜球阶段进球，比赛结束
                CommonPara.IsRunning = false;
                MessageBox.Show(string.Format("Congradulations!\nCompetition is completed.\nWinner:【{0}】 in 【{1}】\n",
                    team.Para.Name, team.Para.MyHalfCourt),
                    "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            { // 恢复默认场景
               
                ResetTeamsAndFishes();
                ResetBalls();
                ResetSomeLocomotionPara();

            }
        }




        // Added by LiYoubing 20110515
        /// <summary>
        /// 判断仿真水球是否完全进入左/右球门区域
        /// </summary>
        /// <param name="ball">仿真水球</param>
        /// <param name="bIsLeftHalfCourt">待判断的是否左球门true判断左球门false判断右球门</param>
        /// <returns>仿真水球位于球门内返回true否则返回false</returns>
        private bool IsBallInGoal(Ball ball, bool bIsLeftHalfCourt)
        {
            int tmpMm = (bIsLeftHalfCourt == true) ? Env.FieldInfo.LeftMm : Env.FieldInfo.RightMm;
            int flag = (bIsLeftHalfCourt == true) ? 1 : -1;
            bool bIsXInGoal = (bIsLeftHalfCourt == true)
                ? (ball.PositionMm.X <= tmpMm + flag * (Env.FieldInfo.GoalDepthMm - ball.RadiusMm))
                : ball.PositionMm.X >= tmpMm + flag * (Env.FieldInfo.GoalDepthMm - ball.RadiusMm);

            return bIsXInGoal && (ball.PositionMm.Z >= ball.RadiusMm - Env.FieldInfo.GoalWidthMm / 2)
                    && (ball.PositionMm.Z <= Env.FieldInfo.GoalWidthMm / 2 - ball.RadiusMm);
        }
        #endregion

        #region 比赛结束判断及处理
        // Added by renjing 20110310 Modified by LiYoubing 20110515
        /// <summary>
        /// 处理倒计时递减到0比赛结束判断提示和响应任务
        /// </summary>
        private void GameOverHandler()
        {
            if (CommonPara.RemainingCycles > 0) return;
            
            // 比赛时间耗完才接着处理
            string strMsg = "";
            if (Teams[0].Para.Score != Teams[1].Para.Score)
            {// 双方得分不同，决出胜负
                CommonPara.IsRunning = false;//比赛停止
                int winner = (Teams[0].Para.Score > Teams[1].Para.Score) ? 0 : 1;
                strMsg = string.Format("Congradulations!\nCompetition is completed.\nWinner:【{0}】 in 【{1}】\n",
                    Teams[winner].Para.Name, Teams[winner].Para.MyHalfCourt);
            }
            else
            {// 双方得分相同
                if (CompetitionPeriod == (int)MatchHelper.CompetitionPeriod.NormalTime)
                {// 开始制胜球
                    strMsg = "Normal time competition is over.\nClutch shot time followed.\n";
                    for (int i = 0; i < CommonPara.TeamCount; i++)
                    {// 解除犯规状态
                        for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                        {
                            Teams[i].Fishes[j].FoulFlag = FoulType5V5.NONE;
                            Teams[i].Fishes[j].CountCyclesFouled = 0;
                            Teams[i].Fishes[j].CountInOwnGoalArea = 0;
                        }
                    }
                    CompetitionPeriod = (int)MatchHelper.CompetitionPeriod.ClutchShotTime;//开始制胜球
                    HandleClutchShotTime();
                }
                else
                {   //modified by longhainan 20120801
                    //比分再次平局开始点球
                    //CommonPara.IsRunning = false;
                    //strMsg = "Competition is tied.\n";
                    strMsg = "Clutch shot time competition is over.\nshootout time followed.\n";
    
                   { // 开始点球
                    for (int i = 0; i < CommonPara.TeamCount; i++)
                    {// 解除犯规状态
                        for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                        {
                            Teams[i].Fishes[j].FoulFlag = FoulType5V5.NONE;
                            Teams[i].Fishes[j].CountCyclesFouled = 0;
                            Teams[i].Fishes[j].CountInOwnGoalArea = 0;
                        }
                    }
                    CompetitionPeriod = (int)MatchHelper.CompetitionPeriod.ShootoutTime;//开始点球
                    HandleShootoutTime();
                  }
                }
               }
          
            //MessageBox.Show(strMsg, "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, strMsg,
                "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        
        }

        /// <summary>
        /// 处理制胜球
        /// 制胜球限时5分钟，发生进球即结束使命，得分的队伍胜
        /// </summary>
        private void HandleClutchShotTime()
        {
            // 设置比赛阶段标志为“制胜球阶段” LiYoubing 20110726
            CompetitionPeriod = (int)MatchHelper.CompetitionPeriod.ClutchShotTime;
            // 制胜球限时5分钟
            CommonPara.TotalSeconds = 5 * 60;
            CommonPara.RemainingCycles = CommonPara.TotalSeconds * 1000 / CommonPara.MsPerCycle;

            ResetTeamsAndFishes();
            ResetBalls();
            ResetSomeLocomotionPara();
        }

        //modified by longhainan 20120801
        /// <summary>
        /// 处理点球
        /// </summary>
        private void HandleShootoutTime()
        {
            // 设置比赛阶段标志为“点球阶段” 
            CompetitionPeriod = (int)MatchHelper.CompetitionPeriod.ShootoutTime;

            // 点球限时3分钟
            CommonPara.TotalSeconds =3 * 60;
            CommonPara.RemainingCycles = CommonPara.TotalSeconds * 1000 / CommonPara.MsPerCycle;

            ResetShootout();
            ResetBalls();
            ResetSomeLocomotionPara();
            
        }
        ////处理点球1
        //private void HandleShootoutTimes()
        //{
        //    // 设置比赛阶段标志为“点球阶段1” 
        //    CompetitionPeriod = (int)MatchHelper.CompetitionPeriod.ShootoutTimes;

        //    // 点球限时3分钟
        //    CommonPara.TotalSeconds = 3 * 60;
        //    CommonPara.RemainingCycles = CommonPara.TotalSeconds * 1000 / CommonPara.MsPerCycle;

        //    ResetShootout();
        //    ResetBalls();
        //    ResetSomeLocomotionPara();

        //}
        #endregion


        #region 处理加时赛和点球等情况的方法 20110516 已经作废
        // LiYoubing 20110516 加时赛取消 此方法作废
        /// <summary>
        /// 处理加时赛
        /// </summary>
        private void HandleExtraTimeKick()
        {
            CommonPara.RemainingCycles = 3 * 60 * 1000 / CommonPara.MsPerCycle; // 加时赛3分钟
            ResetTeamsAndFishes();
            ResetBalls();
            ResetSomeLocomotionPara();
        }

        // LiYoubing 20110516 点球取消 此方法作废
        /// <summary>
        /// 点球大战之前环境信息的设置
        /// </summary>
        /// <param name="teamId">开球的队伍编号</param>
        private void HandleBeforePenaltyKick(int teamId)
        {
            Env.Balls[0].VelocityMmPs = 0;
            Env.Balls[0].VelocityDirectionRad = 0;

            Teams[0].Fishes[0].BodyDirectionRad = 0;
            Teams[0].Fishes[0].VelocityDirectionRad = Teams[0].Fishes[0].BodyDirectionRad;
            Teams[1].Fishes[0].BodyDirectionRad = xna.MathHelper.Pi;
            Teams[1].Fishes[0].VelocityDirectionRad = Teams[1].Fishes[0].BodyDirectionRad;

            Teams[0].Fishes[0].ColorFish = Color.Red;
            Teams[1].Fishes[0].ColorFish = Color.Yellow;
            //A队
            if (teamId % 2 == 0)
            {
                Env.Balls[0].PositionMm = new xna.Vector3((Env.FieldInfo.RightMm 
                    - Env.FieldInfo.GoalDepthMm - Env.FieldInfo.ForbiddenZoneLengthXMm), 0, 0);
                Teams[0].Fishes[0].PositionMm = new xna.Vector3(Env.Balls[0].PositionMm.X 
                    - Teams[0].Fishes[0].BodyLength * (float)1.1, 0, 0);
                Teams[0].Fishes[1].PositionMm = new xna.Vector3(0, 0, 
                    MyMission.Instance().TeamsRef[0].Fishes[1].BodyWidth * 3);
                Teams[0].Fishes[2].PositionMm = new xna.Vector3(0, 0, 
                    -MyMission.Instance().TeamsRef[0].Fishes[2].BodyWidth * 3);

                Teams[1].Fishes[0].PositionMm = new xna.Vector3(Env.FieldInfo.RightMm 
                    - Teams[1].Fishes[0].BodyLength * (float)1.8, 0, 0);
                Teams[1].Fishes[1].PositionMm = new xna.Vector3(0, 0, 
                    MyMission.Instance().TeamsRef[1].Fishes[1].BodyWidth);
                Teams[1].Fishes[2].PositionMm = new xna.Vector3(0, 0, 
                    -MyMission.Instance().TeamsRef[1].Fishes[2].BodyWidth);

                for (int i = 0; i < MyMission.Instance().ParasRef.TeamCount; i++)
                {
                    for (int j = 1; j < MyMission.Instance().ParasRef.FishCntPerTeam; j++)
                    {
                        Teams[i].Fishes[j].BodyDirectionRad = 0;
                    }
                }
            }
            //B队
            else
            {
                Env.Balls[0].PositionMm = new xna.Vector3((Env.FieldInfo.LeftMm 
                    + Env.FieldInfo.GoalDepthMm + Env.FieldInfo.ForbiddenZoneLengthXMm), 0, 0);
                Teams[0].Fishes[0].PositionMm = new xna.Vector3(Env.FieldInfo.LeftMm 
                    + Teams[0].Fishes[0].BodyLength * (float)1.8, 0, 0);
                Teams[0].Fishes[1].PositionMm = new xna.Vector3(0, 0, 
                    MyMission.Instance().TeamsRef[0].Fishes[1].BodyWidth);
                Teams[0].Fishes[2].PositionMm = new xna.Vector3(0, 0, 
                    -MyMission.Instance().TeamsRef[0].Fishes[2].BodyWidth);

                Teams[1].Fishes[0].PositionMm = new xna.Vector3(Env.Balls[0].PositionMm.X 
                    + Teams[1].Fishes[0].BodyLength * (float)1.1, 0, 0);
                Teams[1].Fishes[1].PositionMm = new xna.Vector3(0, 0, 
                    MyMission.Instance().TeamsRef[1].Fishes[1].BodyWidth * 3);
                Teams[1].Fishes[2].PositionMm = new xna.Vector3(0, 0, 
                    -MyMission.Instance().TeamsRef[1].Fishes[2].BodyWidth * 3);
                for (int i = 0; i < MyMission.Instance().ParasRef.TeamCount; i++)
                {
                    for (int j = 1; j < MyMission.Instance().ParasRef.FishCntPerTeam; j++)
                    {
                        Teams[i].Fishes[j].BodyDirectionRad =(float) Math.PI;
                    }
                }
            }
        }

        // LiYoubing 20110516 取消点球 此方法作废
        /// <summary>
        /// 处理点球 
        /// </summary>
        private void HandlePenaltyKick()
        {
            CommonPara.RemainingCycles = 20 * 1000 / CommonPara.MsPerCycle; // 点球20毫秒
            ResetTeamsAndFishes();
            ResetBalls();
            ResetSomeLocomotionPara();
            HandleBeforePenaltyKick(PenaltyKickCount);
        }
        #endregion

        #region 交换半场
        // added by liushu 20110314 Modified by LiYoubing 20110516
        /// <summary>
        /// 处理半场交换任务
        /// 交换半场的基础及思想：
        /// 1.仿真使命基类对象的通用队伍列表成员实际指向具体仿真使命类对象的具体队伍列表成员
        /// 2.策略调用及与客户端通信所用的temId为Mission.TeamsRef各成员的索引
        /// 3.策略调用及与客户端通信时左/右半场队伍分别为Mission.TeamsRef[0]/[1]
        /// 4.交换半场前后TeamsRef[0]/[1]始终代表左/右半场队伍
        /// 5.交换半场前Mission.TeamsRef[0]/[1]分别指向Match5v5.Teams[0]/[1]
        /// 6.交换半场后Mission.TeamsRef[0]/[1]分别指向Match5v5.Teams[1]/[0]
        /// 7.ResetTeamsAndFishes方法中对半场标志及各队伍仿真机器鱼初始位姿的设置
        ///   必须针对Mission.TeamsRef[0]/[1]而非Match5v5.Teams[0]/[1]
        /// 8.Mission.CommonPara中设置一个标志量IsExchangedHalfCourt表示是否交换过半场
        /// 9.处理进球/犯规等情况必须根据半场交换标志量来确定目标队伍
        /// 10.在界面Restart按钮的响应中对标志量进行复位
        /// </summary>
        private void HalfCourtExchangeHandler()
        {
            MyMission myMission = MyMission.Instance();

            if (CommonPara.RemainingCycles == CommonPara.TotalSeconds * 1000 / CommonPara.MsPerCycle / 2)
            {
                //CommonPara.IsExchangedHalfCourt = true;
                // LiYoubing 20110702
                // 制胜球阶段也有可能需要交换半场 对交换半场的标志量取反而不是直接赋值可以满足这个要求
                CommonPara.IsExchangedHalfCourt = !CommonPara.IsExchangedHalfCourt;
                MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, 
                    "Time to Exchange Half-Court.", "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // 交换半场前TeamsRef[0]/[1]分别指向Teams[0]/[1]
                // 交换半场使用List<Team<RoboFish>>的Reverse方法将TeamsRef[0]/[1]的指向对调
                // 交换半场前后TeamsRef[0]/[1]始终代表左/右半场队伍
                myMission.TeamsRef.Reverse();

                ResetTeamsAndFishes();
                ResetBalls();
                ResetSomeLocomotionPara();
            }
        }
        #endregion
        #endregion
        #endregion
    }
}