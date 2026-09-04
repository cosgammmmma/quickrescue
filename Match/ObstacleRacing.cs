//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: MatchObstacleRacing.cs
// Date: 20150503  Author: HuZhe Version: 1
// Description: 水中搬运相关信息
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
    /// 对抗比赛ObstacleRacing仿真使命特有的仿真机器鱼类
    /// </summary>
    [Serializable]
    public class FishObstacleRacing : RoboFish
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

    }

    /// <summary>
    /// 对抗比赛ObstacleRacing仿真使命特有的仿真环境类
    /// </summary>
    [Serializable]
    public class EnvironmentObstacleRacing : SimEnvironment
    {
        // 在这里定义对抗比赛ObstacleRacing仿真使命特有的仿真环境的特性和行为
    }

    /// <summary>
    /// 对抗比赛ObstacleRacing使命类
    /// </summary>
    [Serializable]
    public partial class MatchObstacleRacing : Mission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static MatchObstacleRacing instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static MatchObstacleRacing Instance()
        {
            if (instance == null)
            {
                instance = new MatchObstacleRacing();
            }
            return instance;
        }

        /// <summary>
        /// 私有构造函数 在Instance()中调用 进程生命周期内只会被调用一次
        /// </summary>
        private MatchObstacleRacing()
        {
            CommonPara.MsPerCycle = 100;
            CommonPara.TeamCount = 1;
            CommonPara.FishCntPerTeam = 1;
            CommonPara.Name = "障碍越野";
            initScore();
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
        public List<Team<FishObstacleRacing>> Teams = new List<Team<FishObstacleRacing>>();

        /// <summary>
        /// 仿真使命（比赛或实验项目）所使用的具体仿真环境
        /// </summary>
        public EnvironmentObstacleRacing Env = new EnvironmentObstacleRacing();
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
                Teams.Add(new Team<FishObstacleRacing>());

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
                Teams[i].Fishes.Add(new FishObstacleRacing());
     
                TeamsRef[i].Fishes.Add((RoboFish)Teams[i].Fishes[0]);

                
            }
            TeamsRef[0].Fishes[0].PositionMm = new xna.Vector3(1600, 0, 00);
            TeamsRef[0].Fishes[0].BodyDirectionRad = xna.MathHelper.Pi;


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

            for (int i = 0; i < 2; i++)
            {// 添加9个仿真水球
                Env.Balls.Add(new Ball());
            }
 
            xna.Vector3 positionMm = new xna.Vector3(0, 0, 0);
            xna.Vector3 positionMm11 = new xna.Vector3(-1000, 0, 0);
            xna.Vector3 positionMm22 = new xna.Vector3(1000, 0, 0);
            xna.Vector3 positionMm1 = new xna.Vector3(0, 0, -1350);
            xna.Vector3 positionMm2 = new xna.Vector3(0, 0, -1050);
            xna.Vector3 positionMm3 = new xna.Vector3(0, 0, 1350);
            xna.Vector3 positionMm4 = new xna.Vector3(0, 0, 1050);

            Env.ObstaclesRound.Add(new RoundedObstacle("obs1",positionMm,Color.White,Color.Green,80));
            Env.ObstaclesRound.Add(new RoundedObstacle("obs2", positionMm11, Color.White, Color.Green, 80));
            Env.ObstaclesRound.Add(new RoundedObstacle("obs3", positionMm22, Color.White,Color.Green, 80));

            Env.ObstaclesRect.Add(new RectangularObstacle("obs4", positionMm3, Color.Green, 200, 100, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs7", positionMm4, Color.Green, 200, 100, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs5", positionMm1, Color.Green, 200,100, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs6", positionMm2, Color.Green, 200, 100, 0));
       
        }

        private int[] touch = { 0, 0 };
        private int[] goal = { 0, 0 };
        private int[] touchtime = { 0, 0 };
        private int[] goaltime = { 0, 0 };


        /// <summary>
        /// 初始化当前仿真使命类特有的需要传递给策略的变量名和变量值，在当前使命类构造函数中调用
        /// 添加到仿真使命基类的哈希表Hashtable成员中
        /// </summary>
        private void InitHtMissionVariables()
        {
           

        }

        /// <summary>
        /// 更新当前使命类特有的需要传递给策略的变量名和变量值，在ProcessControlRules中调用
        /// </summary>
        private void UpdateHtMissionVariables()
        {
            // 更新比赛阶段值
            
           
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
                myMission.TeamsRef[0].Fishes[0].PositionMm = new xna.Vector3(1600, 0, 0);
                myMission.TeamsRef[0].Fishes[0].BodyDirectionRad = xna.MathHelper.Pi;

               /* for (int i = 0; i < Teams.Count; i++)
                {
                    // 左/右半场队伍的半场标志分别为LEFT(0)/RIGHT(1)
                    myMission.TeamsRef[i].Para.MyHalfCourt = (HalfCourt)i;
                    for (int j = 0; j < Teams[i].Fishes.Count; j++)
                    {
                        Teams[i].Fishes[j].CountDrawing = 0;
                        Teams[i].Fishes[j].InitPhase = i * 5 + j * 5;

                        // 清除犯规标志 LiYoubing 20110726

                    }
                }
             //   if (CommonPara.IsExchangedHalfCourt == false)
                {
                    // 左半场队伍的仿真机器鱼位置
                    myMission.TeamsRef[0].Fishes[0].PositionMm = new xna.Vector3(
                        f.LeftMm + myMission.TeamsRef[0].Fishes[0].BodyLength * 3, 0, 0);
                    myMission.TeamsRef[0].Fishes[1].PositionMm = new xna.Vector3(
                        -2115, 0, 1591);
                    myMission.TeamsRef[0].Fishes[2].PositionMm = new xna.Vector3(-1715, 0, 1591);
                    myMission.TeamsRef[0].Fishes[3].PositionMm = new xna.Vector3(-1315, 0, 1591);
       
                    myMission.TeamsRef[1].Fishes[0].PositionMm = new xna.Vector3(
                        450, 0, 0);
                    myMission.TeamsRef[1].Fishes[1].PositionMm = new xna.Vector3(1600, 0, -800);
                    myMission.TeamsRef[1].Fishes[2].PositionMm = new xna.Vector3(1600, 0, 0);
                    myMission.TeamsRef[1].Fishes[3].PositionMm = new xna.Vector3(1600, 0, 800);


                    for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                    {
                        // 左半场队伍的仿真机器鱼朝右
                        myMission.TeamsRef[0].Fishes[j].BodyDirectionRad = 0;
                        // 右半场队伍的仿真机器鱼朝左
                        myMission.TeamsRef[1].Fishes[j].BodyDirectionRad = xna.MathHelper.Pi;
                    }
                }*/
         
            }
        }

    

        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真水球恢复默认位置
        /// </summary>
      
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
            Teams[0].Fishes[0].ColorFish = Color.Cyan;
            Teams[0].Fishes[0].ColorId = Color.Black;
           
        }
        public override void ResetObstacles()
        {
            Field f = Field.Instance();



            xna.Vector3 positionMm = new xna.Vector3(0, 0, 0);
            xna.Vector3 positionMm11 = new xna.Vector3(-1000, 0, 0);
            xna.Vector3 positionMm22 = new xna.Vector3(1000, 0, 0);
            xna.Vector3 positionMm1 = new xna.Vector3(0, 0, -1350);
            xna.Vector3 positionMm2 = new xna.Vector3(0, 0, -1050);
            xna.Vector3 positionMm3 = new xna.Vector3(0, 0, 1350);
            xna.Vector3 positionMm4 = new xna.Vector3(0, 0, 1050);

            Env.ObstaclesRound[0].PositionMm = positionMm;
            Env.ObstaclesRound[1].PositionMm = positionMm11;
            Env.ObstaclesRound[2].PositionMm = positionMm22;

            Env.ObstaclesRect[0].PositionMm = positionMm3;
            Env.ObstaclesRect[1].PositionMm = positionMm4;
            Env.ObstaclesRect[2].PositionMm = positionMm1;
            Env.ObstaclesRect[3].PositionMm = positionMm2;

        

            
        }
        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命相应的仿真环境各参数设置为默认值
        /// </summary>
        public override void ResetEnvironment()
        {
            initScore();
            // 将当前仿真使命涉及的仿真场地尺寸恢复默认值
            ResetField();
            ResetBalls();
            // 将当前仿真使命涉及的全部仿真水球恢复默认位置
            ResetObstacles();
        }

        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命使用的仿真场地尺寸恢复默认值
        /// ObstacleRacing场地改为默认值的1.5倍
        /// </summary>
        public override void ResetField()
        {
            Field f = Field.Instance();
            f.FieldLengthXMm = 4500;
            f.FieldLengthZMm = 3000;
            f.FieldCalculation();
        }

        public override void ResetBalls()
        {
            // 每次调用时都将唯一的仿真水球放到场地中心点
            Env.Balls[0].PositionMm = new xna.Vector3(-1600, 0, -1200);
            Env.Balls[1].PositionMm = new xna.Vector3(-1600, 0, 1200);
        }

  
        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命特有的内部变量设置为默认值
        /// </summary>
        private void ResetInternalVariables()
        {
            
        }

        /// <summary>
        /// 重启或改变仿真使命类型时设置关于犯规的时间（周期数）参数为默认值
        /// </summary>
      

        /// <summary>
        /// 重启或改变仿真使命类型时设置关于死球和比赛类型的参数为默认值
        /// </summary>

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
          

            // 处理进球判断提示和响应任务 added by renjing 20110310
           GoalHandler();

            // 处理倒计时递减到0比赛结束判断提示和响应任务 added by renjing 20110310
            GameOverHandler();

            // 处理半场交换任务 added by liushu 20110314
     //       HalfCourtExchangeHandler();

            // 更新当前使命类特有的需要传递给策略的变量名和变量值
            UpdateHtMissionVariables();
         
        }

        #region 对抗赛5VS5仿真使命控制规则所需私有变量
        /// <summary>
        /// 防守机器鱼在禁区内犯规时最大停留时间（单位：毫秒）
        /// </summary>
        #endregion

        #region 对抗赛5VS5仿真使命控制规则具体处理过程      

        #region 进球判断及处理


        // Added by renjing 20110310 Modified by LiYoubing 20110515
        /// <summary>
        /// 进球处理
        /// </summary>
        private void GoalHandler()
        {
            MyMission mymission = MyMission.Instance();

            String strMsg="";
            bool pauseFlag = false;
            if (touch[0] == 0)
            {
                RoboFish fish = Teams[0].Fishes[0];
                Ball thisball = Env.Balls[0];
                CollisionDetectionResult result = CollisionDetection.DetectCollisionBetweenFishAndBall(ref fish,ref  thisball);
                if (result.Intersect == true)
                {
                    pauseFlag = true;
                    touch[0] = 1;
                    
                    int Shootouttime = CommonPara.TotalSeconds * 1000 - CommonPara.RemainingCycles * mymission.ParasRef.MsPerCycle;
                    int ShootouttimeSec = Shootouttime / 1000;
                    strMsg += string.Format("Congradulations!The first touching time is: {1:00}:{2:00}:{3:00}.",
                  Teams[0].Para.Score, ShootouttimeSec / 60, ShootouttimeSec % 60, Shootouttime % 1000);

                }
               
            }
            else if (touch[0] == 1 && goal[0] == 0) 
            {
                Ball thisball = Env.Balls[0];
                double distance = (Env.Balls[0].PositionMm.X - 1900) * (Env.Balls[0].PositionMm.X - 1900) + (Env.Balls[0].PositionMm.Z + 1200) * (Env.Balls[0].PositionMm.Z + 1200);
                if (distance <= 1024.00)
                {
                    TeamsRef[0].Fishes[0].PositionMm = new xna.Vector3(1600, 0, 00);
                    TeamsRef[0].Fishes[0].BodyDirectionRad = xna.MathHelper.Pi;

                    pauseFlag = true;
                    goal[0] = 1;
                    Teams[0].Para.Score = 1;
                    int Shootouttime = CommonPara.TotalSeconds * 1000 - CommonPara.RemainingCycles * mymission.ParasRef.MsPerCycle;
                    int ShootouttimeSec = Shootouttime / 1000;
                    strMsg += string.Format("Congradulations!The first goal time is: {1:00}:{2:00}:{3:00}.",
                  Teams[0].Para.Score, ShootouttimeSec / 60, ShootouttimeSec % 60, Shootouttime % 1000);
                }

            }
            else if (goal[0] == 1 && touch[1] == 0)
            {
                RoboFish fish = Teams[0].Fishes[0];
                Ball thisball = Env.Balls[1];

                CollisionDetectionResult result = CollisionDetection.DetectCollisionBetweenFishAndBall(ref fish, ref  thisball);
                if (result.Intersect == true)
                {
                    pauseFlag = true;
                    touch[1] = 1;
                    
                    int Shootouttime = CommonPara.TotalSeconds * 1000 - CommonPara.RemainingCycles * mymission.ParasRef.MsPerCycle;
                    int ShootouttimeSec = Shootouttime / 1000;
                    strMsg += string.Format("Congradulations!The first touching time is: {1:00}:{2:00}:{3:00}.",
                  Teams[0].Para.Score, ShootouttimeSec / 60, ShootouttimeSec % 60, Shootouttime % 1000);
                }
            }
            else if (touch[1] == 1 && goal[1] == 0)
            {
                Ball thisball = Env.Balls[1];
                double distance = (Env.Balls[1].PositionMm.X - 1900) * (Env.Balls[1].PositionMm.X - 1900) + (Env.Balls[1].PositionMm.Z - 1200) * (Env.Balls[1].PositionMm.Z - 1200);
                if (distance <= 1024.00)
                {
                    pauseFlag = true;
                    goal[1] = 1;
                    Teams[0].Para.Score = 2;
                    int Shootouttime = CommonPara.TotalSeconds * 1000 - CommonPara.RemainingCycles * mymission.ParasRef.MsPerCycle;
                    int ShootouttimeSec = Shootouttime / 1000;
                    strMsg += string.Format("Congradulations!The first goal time is: {1:00}:{2:00}:{3:00}.",
                  Teams[0].Para.Score, ShootouttimeSec / 60, ShootouttimeSec % 60, Shootouttime % 1000);
                }
            }

            if (pauseFlag)
            {
                MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, strMsg,
            "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (goal[1] == 1)
            {

                CommonPara.RemainingCycles = 0;
            }



                

            
    
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


            CommonPara.IsRunning = false;
            string strMsg = string.Format("Competition finished.\n");
            MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, strMsg,
               "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            
            /*
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
            {
                CommonPara.IsRunning = false;
                if (ltime < rtime)
                {
                    int winner = 0;
                    strMsg = string.Format("Congradulations!\nCompetition is completed.\nWinner:【{0}】 in 【{1}】\n",
    Teams[winner].Para.Name, Teams[winner].Para.MyHalfCourt);
                }
                else if (ltime > rtime)
                {
                    int winner = 1;
                    strMsg = string.Format("Congradulations!\nCompetition is completed.\nWinner:【{0}】 in 【{1}】\n",
    Teams[winner].Para.Name, Teams[winner].Para.MyHalfCourt);
                }
                else
                {
                    strMsg = string.Format("Competition is tied.\n");
                }
            }
            
            MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, strMsg,
                "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
 */

        
        }





        #region 交换半场
        // added by liushu 20110314 Modified by LiYoubing 20110516
        /// <summary>
        /// 处理半场交换任务
        /// 交换半场的基础及思想：
        /// 1.仿真使命基类对象的通用队伍列表成员实际指向具体仿真使命类对象的具体队伍列表成员
        /// 2.策略调用及与客户端通信所用的temId为Mission.TeamsRef各成员的索引
        /// 3.策略调用及与客户端通信时左/右半场队伍分别为Mission.TeamsRef[0]/[1]
        /// 4.交换半场前后TeamsRef[0]/[1]始终代表左/右半场队伍
        /// 5.交换半场前Mission.TeamsRef[0]/[1]分别指向MatchObstacleRacing.Teams[0]/[1]
        /// 6.交换半场后Mission.TeamsRef[0]/[1]分别指向MatchObstacleRacing.Teams[1]/[0]
        /// 7.ResetTeamsAndFishes方法中对半场标志及各队伍仿真机器鱼初始位姿的设置
        ///   必须针对Mission.TeamsRef[0]/[1]而非MatchObstacleRacing.Teams[0]/[1]
        /// 8.Mission.CommonPara中设置一个标志量IsExchangedHalfCourt表示是否交换过半场
        /// 9.处理进球/犯规等情况必须根据半场交换标志量来确定目标队伍
        /// 10.在界面Restart按钮的响应中对标志量进行复位
        /// </summary>
        /// 
        private void initScore()
        {
            for (int i = 0; i < 2; i++)
            {
                touch[i] = 0; goal[i] = 0;
            }
        }
        private void exchScore()
        {
           
        }
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
            //    exchScore();

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
        #endregion
    }
}