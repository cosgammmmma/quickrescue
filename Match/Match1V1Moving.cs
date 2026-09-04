//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Match1V1Moving.cs
// Date: 20150503  Author: HuZhe Version: 1
// Description: 水中搬运相关信息
// Histroy:
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
    /// 对抗比赛水中搬运仿真使命特有的仿真机器鱼类
    /// </summary>
    [Serializable]
    public class Fish1V1Moving : RoboFish
    {
        // 在这里定义水中搬运仿真使命特有的仿真机器鱼的特性和行为
    }

    /// <summary>
    /// 水中搬运仿真使命特有的仿真环境类
    /// </summary>
    [Serializable]
    public class Environment1V1Moving : SimEnvironment
    {
        // 在这里定义水中搬运真使命特有的仿真环境的特性和行为
    }

    /// <summary>
    /// 水中搬运仿真使命类
    /// </summary>
    [Serializable]
    public partial class Match1V1Moving : Mission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static Match1V1Moving instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Match1V1Moving Instance()
        {
            if (instance == null)
            {
                instance = new Match1V1Moving();
            }
            return instance;
        }

        /// <summary>
        /// 私有构造函数 在Instance()中调用 进程生命周期内只会被调用一次
        /// </summary>
        private Match1V1Moving()
        {
            CommonPara.MsPerCycle = 100;
            CommonPara.TeamCount = 1;
            CommonPara.FishCntPerTeam = 2;
            CommonPara.Name = "1V1";
            InitTeamsAndFishes();
            InitEnvironment();
            InitDecision();
            InitHtMissionVariables();
        }
        #endregion

        #region public fields
        /// <summary>
        ///  仿真使命（比赛或实验项目）具体参与队伍列表及其具体仿真机器鱼
        /// </summary>
        public List<Team<Fish1V1Moving>> Teams = new List<Team<Fish1V1Moving>>();

        /// <summary>
        ///  仿真使命（比赛或实验项目）所使用的具体仿真环境
        /// </summary>
        public Environment1V1Moving Env = new Environment1V1Moving();
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
                Teams.Add(new Team<Fish1V1Moving>());


                // 给通用仿真机器鱼队伍列表添加新建的通用仿真机器鱼队伍
                TeamsRef.Add(new Team<RoboFish>());

                // 给具体仿真机器鱼队伍设置队员数量
                Teams[i].Para.FishCount = CommonPara.FishCntPerTeam;

                // 给具体仿真机器鱼队伍设置所在半场
                if (i == 0)
                {// 第0支队伍默认在左半场
                    Teams[i].Para.MyHalfCourt = HalfCourt.LEFT;
                }
               

                // 给通用仿真机器鱼队伍设置公共参数
                TeamsRef[i].Para = Teams[i].Para;

                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    // 给具体仿真机器鱼队伍添加新建的具体仿真机器鱼
                    Teams[i].Fishes.Add(new Fish1V1Moving());

                    // 给通用仿真机器鱼队伍添加新建的通用仿真机器鱼
                    TeamsRef[i].Fishes.Add((RoboFish)Teams[i].Fishes[j]);
                }
            }

            // 设置仿真机器鱼鱼体和编号默认颜色
            ResetColorFishAndId();
        }


        private xna.Vector3[] hole = new xna.Vector3[10];
        private int MovingCircleRadius = 80;
        private void CalHol()
        {
            for (int i = 1; i <= 3; i++)
            {
                for (int j = 1; j <= i; j++)
                {
                    int l = i * (i - 1) / 2 + j - 1;
                    hole[l].Y = 0;
                    hole[l].X = - (i * 3 - 3) * MovingCircleRadius;
                    hole[l].Z = - (i - 1) * 2 * MovingCircleRadius + 4 * (j - 1) * MovingCircleRadius;
                }
            }
        }

        private bool IsBallInHole(int x)
        {
            float dist = (Env.Balls[x].PositionMm.X - hole[x].X) * (Env.Balls[x].PositionMm.X - hole[x].X) + (Env.Balls[x].PositionMm.Z - hole[x].Z) * (Env.Balls[x].PositionMm.Z - hole[x].Z);
            if (dist >= 484.00)
                return false;
            else
                return true;
        }

        /// <summary>
        /// 初始化当前使命的仿真环境，在当前使命类构造函数中调用
        /// </summary>
        private void InitEnvironment()
        {
            CalHol();
            EnvRef = (SimEnvironment)Env;
            for (int i = 0; i < 6; i++)
            {// 添加10个仿真水球
                Env.Balls.Add(new Ball());
            }

            int r = Env.Balls[0].RadiusMm;
            xna.Vector3 positionMm = new xna.Vector3(0, 0, 0);
        }
  
        /// <summary>
        /// 初始化当前仿真使命类特有的需要传递给策略的变量名和变量值，在当前使命类构造函数中调用
        /// 添加到仿真使命基类的哈希表Hashtable成员中
        /// </summary>
        private void InitHtMissionVariables()
        {
            // 初始化当前已经完成的单程数量
            HtMissionVariables.Add("Ball0InHole",scores[0]);
            HtMissionVariables.Add("Ball1InHole",scores[1]);
            HtMissionVariables.Add("Ball2InHole",scores[2]);
            HtMissionVariables.Add("Ball3InHole",scores[3]);
            HtMissionVariables.Add("Ball4InHole",scores[4]);
            HtMissionVariables.Add("Ball5InHole",scores[5]);
   
        }

        /// <summary>
        /// 更新当前使命类特有的需要传递给策略的变量名和变量值，在ProcessControlRules中调用
        /// </summary>
        private void UpdateHtMissionVariables()
        {
            // 更新当前已经完成的单程数量

            HtMissionVariables["Ball0InHole"]=scores[0];
            HtMissionVariables["Ball1InHole"]=scores[1];
            HtMissionVariables["Ball2InHole"]=scores[2];
            HtMissionVariables["Ball3InHole"]=scores[3];
            HtMissionVariables["Ball4InHole"]=scores[4];
            HtMissionVariables["Ball5InHole"]=scores[5];
   
            
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
                    myMission.TeamsRef[i].Para.MyHalfCourt = (HalfCourt)i;
                    for (int j = 0; j < Teams[i].Fishes.Count; j++)
                    {
                        Teams[i].Fishes[j].CountDrawing = 0;
                        Teams[i].Fishes[j].InitPhase = i * 5 + j * 5;
                    }
                }

                myMission.TeamsRef[0].Fishes[0].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[0].BodyLength * 3, 
                    0, Teams[0].Fishes[0].BodyWidth * 3);
                myMission.TeamsRef[0].Fishes[1].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[1].BodyLength * 3, 
                    0, -Teams[0].Fishes[1].BodyWidth * 3);

                 for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                
                    // 左半场队伍的仿真机器鱼朝右
                    myMission.TeamsRef[0].Fishes[j].BodyDirectionRad = 0;
                   
               
            }
        }

        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真水球恢复默认位置
        /// </summary>
        public override void ResetBalls()
        {

            int r = Env.Balls[0].RadiusMm;
            // 将9个仿真水球恢复默认位置
            for (int i = 1; i < 4; i++)
            {
                for (int j = 1; j <= i; j++)
                {
                    Env.Balls[(i - 1) * i / 2 + j - 1].PositionMm = new xna.Vector3(160f * (i + 3), 0, -r * (i - 1) * 2 + (j - 1) * r * 4);
                }
            }
            for (int i = 0; i < 6; i++)
            {
                scores[i] = 0;
            }
        }

        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真障碍物恢复默认位置
        /// </summary>
        public override void ResetObstacles()
        {
            Field f = Field.Instance();
            int r = Env.Balls[0].RadiusMm;
            
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
                Teams[0].Fishes[j].ColorFish = Color.Red;
              Teams[0].Fishes[j].ColorId = Color.Black;
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

            // 将当前仿真使命涉及的全部仿真障碍物恢复默认位置
            ResetObstacles();

            // 将当前仿真使命涉及的全部动态仿真障碍物恢复默认位置 
            //added by chenxiao
            //ResetDynamicObstacles();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命特有的内部变量设置为默认值
        /// </summary>
        private void ResetInternalVariables()
        {
            HtMissionVariables["Ball0InHole"] = scores[0];
            HtMissionVariables["Ball1InHole"] = scores[1];
            HtMissionVariables["Ball2InHole"] = scores[2];
            HtMissionVariables["Ball3InHole"] = scores[3];
            HtMissionVariables["Ball4InHole"] = scores[4];
            HtMissionVariables["Ball5InHole"] = scores[5];
      
        }
        #endregion

        #region public methods that implement IMission interface
        /// <summary>
        /// 实现IMission中的接口用于 设置当前使命类型中各对象的初始值
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
        /// 处理对抗比赛2VS2争球具体仿真使命的控制规则
        /// </summary>
        public override void ProcessControlRules()
        {
            // 处理得分即每个球门里仿真水球个数统计任务
            ScoreCalculationHandler();

            // 处理倒计时递减到0比赛结束判断提示和响应任务
            GameOverHandler();

    
            // 更新当前使命类特有的需要传递给策略的变量名和变量值
            UpdateHtMissionVariables();
        }

                   

        #region 对抗赛2VS2争球仿真使命控制规则所需私有变量
        // added by renjing 20110310
        /// <summary>
        /// 比赛阶段
        /// </summary>

        private int[] scores = { 0, 0, 0, 0, 0, 0 };
        private int[] ustime = { 0, 0, 0, 0, 0, 0,};
        private int lasttime;
        
        #endregion

        #region 对抗赛3VS3仿真使命控制规则具体处理过程
        #region 得分实时统计
        // Added by LiYoubing 20110518
        /// <summary>
        /// 处理得分即每个球门里仿真水球个数统计任务
        /// </summary>
        
        private void ScoreCalculationHandler()
        {
            MyMission myMission = MyMission.Instance();

            for (int i = 0; i < 6; i++)
            {
                if (IsBallInHole(i) && scores[i] == 0)
                {
                    scores[i] = 1;
                    ustime[i] = CommonPara.RemainingCycles;
                    string strMsg = "";
                    int Shootouttime = CommonPara.TotalSeconds * 1000 - CommonPara.RemainingCycles * myMission.ParasRef.MsPerCycle;
                    int ShootouttimeSec = Shootouttime / 1000;
                   
                    strMsg += string.Format("Congradulations!Goal time is: {1:00}:{2:00}:{3:00}.",
                          Teams[0].Para.Score, ShootouttimeSec / 60, ShootouttimeSec % 60, Shootouttime % 1000);
                    MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, strMsg,
                           "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    lasttime = ustime[i];
                }
            }
            int count = 0;
            for (int i=0;i<6;i++)
            {
                if (scores[i] == 1)
                    count++;
            }
            for (int i = 0; i < myMission.TeamsRef.Count; i++)
            {// 更新左/右半场队伍的得分
                myMission.TeamsRef[i].Para.Score = count;
            }
         
        }

        // Added by LiYoubing 20110518
        /// <summary>
        /// 判断仿真水球是否完全进入上/下障碍物围成的特殊球门区域
        /// </summary>
        /// <param name="ball">仿真水球</param>
        /// <param name="bIsLeftHalfCourt">待判断的是否上球门true判断上球门false判断下球门</param>
        /// <returns>仿真水球位于障碍物围成的特殊球门内返回true否则返回false</returns>
      
        #endregion

        #region 比赛结束判断及处理
        // Added by renjing 20110310 Modified by LiYoubing 20110515
        /// <summary>
        /// 处理倒计时递减到0比赛结束判断提示和响应任务
        /// </summary>
        private void GameOverHandler()
        {
            MyMission myMission = MyMission.Instance();

            if (CommonPara.RemainingCycles == 0)
            {
                string strMsg = "";

                int Shootouttime = CommonPara.TotalSeconds * 1000 - lasttime * myMission.ParasRef.MsPerCycle;
                int ShootouttimeSec = Shootouttime / 1000;

                strMsg += string.Format("Time Over!Last Goal time is: {1:00}:{2:00}:{3:00}.",
                      Teams[0].Para.Score, ShootouttimeSec / 60, ShootouttimeSec % 60, Shootouttime % 1000);
                MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, strMsg,
                       "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (Teams[0].Para.Score == 6)
            {
                string strMsg = "";

                int Shootouttime = CommonPara.TotalSeconds * 1000 - lasttime * myMission.ParasRef.MsPerCycle;
                int ShootouttimeSec = Shootouttime / 1000;

                strMsg += string.Format("Congratulations!Last Goal time is: {1:00}:{2:00}:{3:00}.",
                      Teams[0].Para.Score, ShootouttimeSec / 60, ShootouttimeSec % 60, Shootouttime % 1000);
                MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, strMsg,
                       "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                CommonPara.RemainingCycles = 0;
            }
            if (CommonPara.RemainingCycles > 0) return;
            // 比赛时间耗完或才接着处理
           
         
       }

    
        #endregion

        #endregion
        #endregion
    }
}