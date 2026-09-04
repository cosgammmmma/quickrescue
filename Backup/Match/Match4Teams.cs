//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Match4Teams.cs
// Date: 20101119  Author: LiYoubing  Version: 1
// Description: 4支队伍测试使命相关的仿真机器鱼，仿真环境和仿真使命定义文件
// Histroy:
// Date: 20101119  Author: LiYoubing
// Modification: 修改内容简述
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices; // for MethodImpl
using System.Drawing;
using xna = Microsoft.Xna.Framework;

using Microsoft.Dss.Core;

using URWPGSim2D.Common;

namespace URWPGSim2D.Match
{
    [Serializable]
    public class Fish4Teams : RoboFish
    {
        public int fish4TeamsTest;
    }

    [Serializable]
    public class Environment4Teams : SimEnvironment
    {
        public int env4TeamsTest;
    }

    /// <summary>
    /// 对抗比赛4Teams使命类
    /// </summary>
    [Serializable]
    public partial class Match4Teams : Mission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static Match4Teams instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Match4Teams Instance()
        {
            if (instance == null)
            {
                instance = new Match4Teams();
            }
            return instance;
        }

        private Match4Teams() 
        {
            CommonPara.MsPerCycle = 100;
            CommonPara.TeamCount = 4;
            CommonPara.FishCntPerTeam = 3;
            CommonPara.Name = "4支队伍测试";
            InitTeamsAndFishes();
            InitEnvironment();
            InitDecision();
        }
        #endregion

        #region public fields
        /// <summary>
        ///  仿真使命（比赛或实验项目）具体参与队伍列表及其具体仿真机器鱼
        /// </summary>
        public List<Team<Fish4Teams>> Teams = new List<Team<Fish4Teams>>();

        /// <summary>
        ///  仿真使命（比赛或实验项目）所使用的具体仿真环境
        /// </summary>
        public Environment4Teams Env = new Environment4Teams();
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
                Teams.Add(new Team<Fish4Teams>());

                // 给通用仿真机器鱼队伍列表添加新建的通用仿真机器鱼队伍
                TeamsRef.Add(new Team<RoboFish>());

                // 给具体仿真机器鱼队伍设置队员数量
                Teams[i].Para.FishCount = CommonPara.FishCntPerTeam;

                // 给具体仿真机器鱼队伍设置所在半场
                if (i % 2 == 0)
                {// 第0支队伍默认在左半场
                    Teams[i].Para.MyHalfCourt = HalfCourt.LEFT;
                }
                else if (i % 2 == 1)
                {// 第1支队伍默认在右半场
                    Teams[i].Para.MyHalfCourt = HalfCourt.RIGHT;
                }

                // 给通用仿真机器鱼队伍设置公共参数
                TeamsRef[i].Para = Teams[i].Para;

                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    // 给具体仿真机器鱼队伍添加新建的具体仿真机器鱼
                    Teams[i].Fishes.Add(new Fish4Teams());

                    // 给通用仿真机器鱼队伍添加新建的通用仿真机器鱼
                    TeamsRef[i].Fishes.Add((RoboFish)Teams[i].Fishes[j]);
                }
            }
        }

        /// <summary>
        /// 初始化当前使命的仿真环境，在当前使命类构造函数中调用
        /// </summary>
        private void InitEnvironment()
        {
            Env.env4TeamsTest = 4;
            EnvRef = (SimEnvironment)Env;
        }

        private void SetTeamsAndFishes4Teams()
        {
            Field f = Field.Instance();
            if (Teams != null)
            {
                Teams[0].Para.MyHalfCourt = HalfCourt.LEFT;
                Teams[1].Para.MyHalfCourt = HalfCourt.RIGHT;

                for (int i = 0; i < Teams.Count; i++)
                {
                    Teams[i].Para.Score = 0;
                    Teams[i].Para.Name = "Team" + (i + 1).ToString();
                    for (int j = 0; j < Teams[i].Fishes.Count; j++)
                    {
                        Teams[i].Fishes[j].CountDrawing = 0;
                        Teams[i].Fishes[j].InitPhase = i * 5 + j * 5;
                        Teams[i].Fishes[j].ColorId = Color.Black;
                    }
                }

                Teams[0].Fishes[0].PositionMm = new xna.Vector3(f.LeftMm + Teams[0].Fishes[0].BodyLength * 3, 0, 0);
                Teams[0].Fishes[1].PositionMm = new xna.Vector3(f.LeftMm + Teams[0].Fishes[1].BodyLength * 3,
                    0, Teams[0].Fishes[1].BodyWidth * 3);
                Teams[0].Fishes[2].PositionMm = new xna.Vector3(f.LeftMm + Teams[0].Fishes[2].BodyLength * 3,
                    0, -Teams[0].Fishes[2].BodyWidth * 3);

                Teams[1].Fishes[0].PositionMm = new xna.Vector3(f.RightMm - Teams[0].Fishes[0].BodyLength * 3, 0, 0);
                Teams[1].Fishes[1].PositionMm = new xna.Vector3(f.RightMm - Teams[0].Fishes[1].BodyLength * 3,
                    0, Teams[1].Fishes[1].BodyWidth * 3);
                Teams[1].Fishes[2].PositionMm = new xna.Vector3(f.RightMm - Teams[0].Fishes[2].BodyLength * 3,
                    0, -Teams[1].Fishes[2].BodyWidth * 3);

                Teams[2].Fishes[0].PositionMm = new xna.Vector3(0, 0, f.TopMm + Teams[2].Fishes[0].BodyLength * 3);
                Teams[2].Fishes[1].PositionMm = new xna.Vector3(Teams[0].Fishes[1].BodyWidth * 3,
                    0, f.TopMm + Teams[2].Fishes[1].BodyLength * 3);
                Teams[2].Fishes[2].PositionMm = new xna.Vector3(-Teams[0].Fishes[2].BodyWidth * 3,
                    0, f.TopMm + Teams[2].Fishes[2].BodyLength * 3);

                Teams[3].Fishes[0].PositionMm = new xna.Vector3(0, 0, f.BottomMm - Teams[3].Fishes[0].BodyLength * 3);
                Teams[3].Fishes[1].PositionMm = new xna.Vector3(Teams[1].Fishes[1].BodyWidth * 3,
                    0, f.BottomMm - Teams[3].Fishes[1].BodyLength * 3);
                Teams[3].Fishes[2].PositionMm = new xna.Vector3(-Teams[1].Fishes[2].BodyWidth * 3,
                    0, f.BottomMm - Teams[3].Fishes[2].BodyLength * 3);

                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    Teams[0].Fishes[j].BodyDirectionRad = 0;
                    Teams[0].Fishes[j].VelocityDirectionRad = Teams[0].Fishes[j].BodyDirectionRad;
                    Teams[1].Fishes[j].BodyDirectionRad = xna.MathHelper.Pi;
                    Teams[1].Fishes[j].VelocityDirectionRad = Teams[1].Fishes[j].BodyDirectionRad;
                    Teams[2].Fishes[j].BodyDirectionRad = xna.MathHelper.PiOver2;
                    Teams[2].Fishes[j].VelocityDirectionRad = Teams[2].Fishes[j].BodyDirectionRad;
                    Teams[3].Fishes[j].BodyDirectionRad = -xna.MathHelper.PiOver2;
                    Teams[3].Fishes[j].VelocityDirectionRad = Teams[3].Fishes[j].BodyDirectionRad;

                    Teams[0].Fishes[j].ColorFish = Color.Red;
                    Teams[1].Fishes[j].ColorFish = Color.Yellow;
                    Teams[2].Fishes[j].ColorFish = Color.Coral;
                    Teams[3].Fishes[j].ColorFish = Color.Aqua;
                }
            }
        }

        private void SetEnvironment4Teams()
        {
            // 只有第一次调用时需要添加一个仿真水球
            if (Env.Balls.Count == 0)
            {
                Env.Balls.Add(new Ball());
            }

            // 每次调用时都将唯一的仿真水球放到场地中心点
            Env.Balls[0].PositionMm = new xna.Vector3(0, 0, 0);
        }
        #endregion

        #region public methods that implement IMission interface
        /// <summary>
        /// 实现IMission中的接口用于 设置当前使命中各对象的初始值
        /// </summary>
        public override void SetMission()
        {
            ResetDecision();
            ResetSomeLocomotionPara();
            SetTeamsAndFishes4Teams();
            SetEnvironment4Teams();
        }

        public override void ProcessControlRules()
        {
 
        }
        #endregion
    }
}