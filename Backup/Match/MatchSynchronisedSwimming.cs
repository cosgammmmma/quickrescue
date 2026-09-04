//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: MatchSynchronisedSwimming.cs
// Date: 20110603  Author: BaoHua  Version: 1
// Description: 花样游泳比赛项目相关的仿真机器鱼，仿真环境和仿真使命定义文件
// Histroy:
// Date: 20110710  Author: LiYoubing
// Modification: 
// 1.仿真机器鱼初始位置和鱼体方向随机设置
// Date: 20111106  Author: ZhangBo
// Modification: 
// 1.项目仿真机器鱼项目由8条增为10条。
// ……
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
    /// 花样游泳仿真使命特有的仿真机器鱼类
    /// </summary>
    [Serializable]
    public class FishSynchronisedSwimming : RoboFish
    {
        // 在这里定义花样游泳仿真使命特有的仿真机器鱼的特性和行为
    }

    /// <summary>
    /// 花样游泳仿真使命特有的仿真环境类
    /// </summary>
    [Serializable]
    public class EnvironmentSynchronisedSwimming : SimEnvironment
    {
        // 在这里定义花样游泳仿真使命特有的仿真环境的特性和行为
    }

    /// <summary>
    /// 花样游泳使命类
    /// </summary>
    [Serializable]
    public partial class MatchSynchronisedSwimming : Mission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static MatchSynchronisedSwimming instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static MatchSynchronisedSwimming Instance()
        {
            if (instance == null)
            {
                instance = new MatchSynchronisedSwimming();
            }
            return instance;
        }

        /// <summary>
        /// 私有构造函数 在Instance()中调用 进程生命周期内只会被调用一次
        /// </summary>
        private MatchSynchronisedSwimming()
        {
            CommonPara.MsPerCycle = 100;
            CommonPara.TeamCount = 1;
            CommonPara.FishCntPerTeam = 10;
            CommonPara.Name = "花样游泳";
            InitTeamsAndFishes();
            InitEnvironment();
            InitDecision();
        }
        #endregion

        #region public fields
        /// <summary>
        /// 仿真使命（比赛或实验项目）具体参与队伍列表及其具体仿真机器鱼
        /// </summary>
        public List<Team<FishSynchronisedSwimming>> Teams = new List<Team<FishSynchronisedSwimming>>();

        /// <summary>
        /// 仿真使命（比赛或实验项目）所使用的具体仿真环境
        /// </summary>
        public EnvironmentSynchronisedSwimming Env = new EnvironmentSynchronisedSwimming();
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
                Teams.Add(new Team<FishSynchronisedSwimming>());

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
                    Teams[i].Fishes.Add(new FishSynchronisedSwimming());

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
                    }
                }

                int fishRadius = Teams[0].Fishes[0].CollisionModelRadiusMm;
                Random random = new Random();
                int minX = f.LeftMm + f.GoalDepthMm + fishRadius;
                int maxX = f.RightMm - f.GoalDepthMm - fishRadius;
                int minZ = f.TopMm + fishRadius;
                int maxZ = f.BottomMm - fishRadius;
                foreach (Team<RoboFish> team in TeamsRef)
                {
                    foreach (RoboFish fish in team.Fishes)
                    {// 随机设置仿真机器鱼位置和鱼体方向
                        fish.PositionMm = new xna.Vector3(random.Next(minX, maxX), 0, random.Next(minZ, maxZ));
                        fish.BodyDirectionRad = xna.MathHelper.ToRadians(random.Next(0, 360));
                    }
                }

                #region
                // 设置仿真机器鱼位置
                //int fishRadius = Teams[0].Fishes[0].CollisionModelRadiusMm;
                //int fishWidth = 2 * Teams[0].Fishes[0].BodyWidth;
                //Teams[0].Fishes[0].PositionMm = new xna.Vector3(f.LeftMm + f.GoalDepthMm
                //    + f.ForbiddenZoneLengthXMm + 4 * fishRadius, 0, -f.ForbiddenZoneLengthZMm / 2);
                //Teams[0].Fishes[1].PositionMm = new xna.Vector3(f.LeftMm + f.GoalDepthMm
                //    + f.ForbiddenZoneLengthXMm + 4 * fishRadius, 0, f.ForbiddenZoneLengthZMm / 2);
                //Teams[0].Fishes[2].PositionMm = new xna.Vector3(f.RightMm - f.GoalDepthMm
                //    - f.ForbiddenZoneLengthXMm - 4 * fishRadius, 0, -f.ForbiddenZoneLengthZMm / 2);
                //Teams[0].Fishes[3].PositionMm = new xna.Vector3(f.RightMm - f.GoalDepthMm
                //    - f.ForbiddenZoneLengthXMm - 4 * fishRadius, 0, f.ForbiddenZoneLengthZMm / 2);
                //Teams[0].Fishes[4].PositionMm = new xna.Vector3(f.LeftMm + f.GoalDepthMm
                //    + 4 * fishRadius, 0, -f.ForbiddenZoneLengthZMm / 2 - 4 * fishWidth);
                //Teams[0].Fishes[5].PositionMm = new xna.Vector3(f.LeftMm + f.GoalDepthMm
                //    + 4 * fishRadius, 0, f.ForbiddenZoneLengthZMm / 2 + 4 * fishWidth);
                //Teams[0].Fishes[6].PositionMm = new xna.Vector3(f.RightMm - f.GoalDepthMm
                //    - 4 * fishRadius, 0, -f.ForbiddenZoneLengthZMm / 2 - 4 * fishWidth);
                //Teams[0].Fishes[7].PositionMm = new xna.Vector3(f.RightMm - f.GoalDepthMm
                //    - 4 * fishRadius, 0, f.ForbiddenZoneLengthZMm / 2 + 4 * fishWidth);

                // 设置仿真机器鱼鱼体方向
                //Teams[0].Fishes[0].BodyDirectionRad = 0;
                //Teams[0].Fishes[1].BodyDirectionRad = 0;
                //Teams[0].Fishes[4].BodyDirectionRad = 0;
                //Teams[0].Fishes[5].BodyDirectionRad = 0;
                //Teams[0].Fishes[2].BodyDirectionRad = xna.MathHelper.Pi;
                //Teams[0].Fishes[3].BodyDirectionRad = xna.MathHelper.Pi;
                //Teams[0].Fishes[6].BodyDirectionRad = xna.MathHelper.Pi;
                //Teams[0].Fishes[7].BodyDirectionRad = xna.MathHelper.Pi;
                #endregion

                #region
                //for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                //{// 设置仿真机器鱼位置
                //    // 编号（0,1,...,7）为偶数的放左半场奇数的放右半场
                //    int lr = ((j % 2) == 0) ? -1 : 1;
                //    // 0,1,2,3在上，4,5,6,7在下
                //    int ud = (j < 4) ? -1 : 1;
                //    // 0,1,4,5在里层左上/右上/左下/右下，2,3,6,7在外层左上/右上/左下/右下
                //    int factor = ((j % 4) < 2) ? 1 : 2;
                //    // 统一设定鱼体绘图中心点位置
                //    myMission.TeamsRef[0].Fishes[j].PositionMm = new xna.Vector3(
                //        lr * factor * 2 * myMission.TeamsRef[0].Fishes[j].CollisionModelRadiusMm, 0,
                //        ud * myMission.TeamsRef[0].Fishes[j].CollisionModelRadiusMm);
                //    // 统一设定鱼体方向
                //    myMission.TeamsRef[0].Fishes[j].BodyDirectionRad = -1 * ud * xna.MathHelper.PiOver2;
                //}
                #endregion
            }
        }

        // LiYoubing 20110701
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命使用的仿真场地尺寸恢复默认值
        /// </summary>
        public override void ResetField()
        {
            Field f = Field.Instance();
            f.FieldLengthXMm = 4500;
            f.FieldLengthZMm = 3000;
            f.FieldCalculation();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命相应的仿真环境各参数设置为默认值  liufei
        /// </summary>
        public override void ResetEnvironment()
        {
            // 将当前仿真使命涉及的仿真场地尺寸恢复默认值
            ResetField();
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
            //AnYongyue 20120819
            //设置1号机器鱼的鱼体颜色为黑色
            Teams[0].Fishes[0].ColorFish = Color.Yellow;
            //设置1号机器鱼的编号颜色为红色
            Teams[0].Fishes[0].ColorId = Color.Black;
            for (int j = 1; j < CommonPara.FishCntPerTeam; j++)
            {
                // 第0支队伍的仿真机器鱼前端色标即鱼体颜色为红色
                Teams[0].Fishes[j].ColorFish = Color.Red;
                // 第0支队伍的仿真机器鱼后端色标即编号颜色为黑色
                Teams[0].Fishes[j].ColorId = Color.Black;
            }
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
                Teams[i].Para.Score = -1;   // 得分值为负界面不显示
                Teams[i].Para.Name = "Team" + (i + 1).ToString();
            }

            // 重启或改变仿真使命类型时将当前选中使命的动态对象（仿真机器鱼和仿真水球）的部分运动学设置为默认值
            ResetSomeLocomotionPara();

            // 重启或改变仿真使命类型时将当前选中使命的决策数组各元素设置为默认值
            ResetDecision();
        }

        /// <summary>
        /// 处理对抗赛1VS1具体仿真使命的控制规则
        /// </summary>
        public override void ProcessControlRules()
        {
            // 处理倒计时递减到0比赛结束判断提示和响应任务
            GameOverHandler();
        }

        #region 花样游泳仿真使命控制规则具体处理过程
        #region 比赛结束判断及处理
        /// <summary>
        /// 处理倒计时递减到0比赛结束判断提示和响应任务
        /// </summary>
        private void GameOverHandler()
        {
            if (CommonPara.RemainingCycles > 0) return;

            // 比赛时间耗完才接着处理
            string strMsg = "Time Over!";

            //MessageBox.Show(strMsg, "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, strMsg,
                "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        #endregion
        #endregion
        #endregion
    }
}