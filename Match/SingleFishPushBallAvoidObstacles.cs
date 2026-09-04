//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: SingleFishPushBallAvoidObstacles.cs
// Date: 20110310  Author: chen penghui  Version: 1
// Description: 单鱼推球避障比赛项目相关的仿真机器鱼，仿真环境和仿真使命定义文件
// Histroy:
// Date: 20110310  Author: chen penghui
// Modification: 修改内容简述
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Runtime.CompilerServices; 
using System.Drawing;
using xna = Microsoft.Xna.Framework;
using URWPGSim2D.Common;

namespace URWPGSim2D.Match
{
    [Serializable]
    public class FishSingleFishPushBallAvoidObstacles : RoboFish
    {
        public int fishSingleFishPushBallAvoidObstacles;
    }

    [Serializable]
    public class EnvironmentSingleFishPushBallAvoidObstacles : SimEnvironment
    {
        public int envSingleFishPushBallAvoidObstacles;
    }

    [Serializable]
    public partial class SingleFishPushBallAvoidObstacles : Mission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static SingleFishPushBallAvoidObstacles instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SingleFishPushBallAvoidObstacles Instance()
        {
            if (instance == null)
            {
                instance = new SingleFishPushBallAvoidObstacles();
            }
            return instance;
        }

        private SingleFishPushBallAvoidObstacles()
        {
            CommonPara.MsPerCycle = 100;
            CommonPara.TotalSeconds = 180;//chenpenghui 这个时间如何确定
            CommonPara.TeamCount = 1;
            CommonPara.FishCntPerTeam = 1;
            CommonPara.Name = "单鱼推球避障";
            InitTeamsAndFishes();
            InitEnvironment();
            InitDecision();
        }
        #endregion

        #region public fields
        /// <summary>
        ///  仿真使命（比赛或实验项目）具体参与队伍列表及其具体仿真机器鱼
        /// </summary>
        public List<Team<FishSingleFishPushBallAvoidObstacles>> Teams = new List<Team<FishSingleFishPushBallAvoidObstacles>>();

        /// <summary>
        ///  仿真使命（比赛或实验项目）所使用的具体仿真环境
        /// </summary>
        public EnvironmentSingleFishPushBallAvoidObstacles Env = new EnvironmentSingleFishPushBallAvoidObstacles();

        /// <summary>
        /// 单鱼推球，球要越过的黑线的X轴值
        /// </summary>
        public int blackLineX = -500;

      
        #endregion

        /// <summary>
        /// 记录犯规类型，值为1时：属于犯规情况1，机器鱼碰到障碍物；值为2时：属于犯规情况2，球碰到障碍物；为0时，没有犯规！
        /// </summary>
        public int FoulFlag = 0;

        /// <summary>
        /// 记录fish游四个单程，各个单程第一次完成的时间值
        /// </summary>
        public int[] finshedTimeOneWay = { 0, 0, 0, 0 };

        /// <summary>
        /// 记录fish完成的单程数目
        /// </summary>
        public int finishedCountOneWay = 0;

       
        int presentCollisionCycle = 0;//记录碰撞周期
        int escapeCollisionTime = 1;//碰撞后下次检测为三秒后
        bool collisionFlag = false;//过了三秒后再产生碰撞的标志量
        bool collisionFishAndObstacles = false;// 机器鱼与障碍物的碰撞标志
        bool collisionBallAndObstacles = false;// 球与障碍物的碰撞标志


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
                Teams.Add(new Team<FishSingleFishPushBallAvoidObstacles>());

                // 给通用仿真机器鱼队伍列表添加新建的通用仿真机器鱼队伍
                TeamsRef.Add(new Team<RoboFish>());

                // 给具体仿真机器鱼队伍设置队员数量
                Teams[i].Para.FishCount = CommonPara.FishCntPerTeam;

                // 给具体仿真机器鱼队伍设置所在半场
                if (i == 0) // 第0支队伍默认在左半场
                {
                    Teams[i].Para.MyHalfCourt = HalfCourt.LEFT;
                }
                else if (i == 1)    // 第1支队伍默认在右半场
                {
                    Teams[i].Para.MyHalfCourt = HalfCourt.RIGHT;
                }

                // 给通用仿真机器鱼队伍设置公共参数
                TeamsRef[i].Para = Teams[i].Para;

                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    // 给具体仿真机器鱼队伍添加新建的具体仿真机器鱼
                    Teams[i].Fishes.Add(new FishSingleFishPushBallAvoidObstacles());

                    // 给通用仿真机器鱼队伍添加新建的通用仿真机器鱼
                    TeamsRef[i].Fishes.Add((RoboFish)Teams[i].Fishes[j]);
                }
            }
        }

        /// <summary>
        /// 设置当前使命的仿真环境，在当前使命类构造函数中调用
        /// </summary>
        private void InitEnvironment()
        {
            Env.envSingleFishPushBallAvoidObstacles = 999;//chen penghui 这个值如何确定
            EnvRef = (SimEnvironment)Env;

            #region 初始化障碍物       
            //左半场的障碍物 最下端的底线依次向上添加障碍物
            xna.Vector3 Obs1PositionMm = new xna.Vector3(-480, 0, 650);
            xna.Vector3 Obs2PositionMm = new xna.Vector3(-550, 0, 270);

            xna.Vector3 Obs3PositionMm = new xna.Vector3(-550, 0, 180);
            xna.Vector3 Obs4PositionMm = new xna.Vector3(-480, 0, -160);

            xna.Vector3 Obs5PositionMm = new xna.Vector3(-480, 0, -510);


            Env.ObstaclesRect.Add(new RectangularObstacle("obs1", Obs1PositionMm, Color.Blue, Color.Blue, 10, 700, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs2", Obs2PositionMm, Color.Blue, Color.Blue, 150, 60, 0));

            Env.ObstaclesRect.Add(new RectangularObstacle("obs3", Obs3PositionMm, Color.Blue, Color.Blue, 150, 60, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs4", Obs4PositionMm, Color.Blue, Color.Blue, 10, 700, 0));

            Env.ObstaclesRect.Add(new RectangularObstacle("obs5", Obs5PositionMm, Color.Blue, Color.Blue, 150, 3, 0));

            //右半场的障碍物 从最上端的底线依次向下添加障碍物

            xna.Vector3 Obs6PositionMm = new xna.Vector3(480, 0, -650);
            xna.Vector3 Obs7PositionMm = new xna.Vector3(550, 0, -270);

            xna.Vector3 Obs8PositionMm = new xna.Vector3(550, 0, -180);
            xna.Vector3 Obs9PositionMm = new xna.Vector3(480, 0, 160);

            xna.Vector3 Obs10PositionMm = new xna.Vector3(480, 0, 510);


            Env.ObstaclesRect.Add(new RectangularObstacle("obs6", Obs6PositionMm, Color.Blue, Color.Blue, 10, 700, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs7", Obs7PositionMm, Color.Blue, Color.Blue, 150, 60, 0));

            Env.ObstaclesRect.Add(new RectangularObstacle("obs8", Obs8PositionMm, Color.Blue, Color.Blue, 150, 60, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs9", Obs9PositionMm, Color.Blue, Color.Blue, 10, 700, 0));

            Env.ObstaclesRect.Add(new RectangularObstacle("obs10", Obs10PositionMm, Color.Blue, Color.Blue, 150, 3, 0));
            #endregion
        }

        private void SetTeamsAndFishesSingleFishPushBallAvoidObstacles()
        {
            Field f = Field.Instance();
            if (Teams != null)
            {
                Teams[0].Para.Name = "Team1";
                Teams[0].Fishes[0].PositionMm = new xna.Vector3(f.LeftMm + Teams[0].Fishes[0].BodyLength * 2, 0, 0);
                Teams[0].Fishes[0].BodyDirectionRad = (float)(0);
                Teams[0].Fishes[0].VelocityDirectionRad = (float)(0);
                Teams[0].Fishes[0].ColorFish = Color.Red;


                for (int j = 0; j < CommonPara.TeamCount; j++)
                {
                    Teams[j].Fishes[0].CountDrawing = 0;
                }
            }

        }

        private void SetEnvironmentSingleFishPushBallAvoidObstacles()
        {
            // 只有第一次调用时需要添加一个仿真水球
            if (Env.Balls.Count == 0)
            {
                Env.Balls.Add(new Ball());
            }

            // 每次调用时都将唯一的仿真水球放到场地中心点
            Env.Balls[0].PositionMm = new xna.Vector3(1030, 0, 0);
            Env.Balls[0].VelocityMmPs = 0;
            Env.Balls[0].VelocityDirectionRad = 0;
        }
        #endregion

        //#region public methods that implement IMission interface
        /// <summary>
        /// 实现IMission中的接口用于 设置当前使命类型中各对象的初始值
        /// </summary>
        public override void SetMission()
        {
            for (int i = 0; i < 10; i++)
            {
                Env.ObstaclesRect.Remove(Env.ObstaclesRect[0]);
            }

            #region 重置障碍物
            //左半场的障碍物 最下端的底线依次向上添加障碍物
            xna.Vector3 Obs1PositionMm = new xna.Vector3(-480, 0, 650);
            xna.Vector3 Obs2PositionMm = new xna.Vector3(-550, 0, 270);

            xna.Vector3 Obs3PositionMm = new xna.Vector3(-550, 0, 180);
            xna.Vector3 Obs4PositionMm = new xna.Vector3(-480, 0, -160);

            xna.Vector3 Obs5PositionMm = new xna.Vector3(-480, 0, -510);


            Env.ObstaclesRect.Add(new RectangularObstacle("obs1", Obs1PositionMm, Color.Blue, Color.Blue, 10, 700, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs2", Obs2PositionMm, Color.Blue, Color.Blue, 150, 60, 0));

            Env.ObstaclesRect.Add(new RectangularObstacle("obs3", Obs3PositionMm, Color.Blue, Color.Blue, 150, 60, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs4", Obs4PositionMm, Color.Blue, Color.Blue, 10, 700, 0));

            Env.ObstaclesRect.Add(new RectangularObstacle("obs5", Obs5PositionMm, Color.Blue, Color.Blue, 150, 3, 0));

            //右半场的障碍物 从最上端的底线依次向下添加障碍物

            xna.Vector3 Obs6PositionMm = new xna.Vector3(480, 0, -650);
            xna.Vector3 Obs7PositionMm = new xna.Vector3(550, 0, -270);

            xna.Vector3 Obs8PositionMm = new xna.Vector3(550, 0, -180);
            xna.Vector3 Obs9PositionMm = new xna.Vector3(480, 0, 160);

            xna.Vector3 Obs10PositionMm = new xna.Vector3(480, 0, 510);


            Env.ObstaclesRect.Add(new RectangularObstacle("obs6", Obs6PositionMm, Color.Blue, Color.Blue, 10, 700, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs7", Obs7PositionMm, Color.Blue, Color.Blue, 150, 60, 0));

            Env.ObstaclesRect.Add(new RectangularObstacle("obs8", Obs8PositionMm, Color.Blue, Color.Blue, 150, 60, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs9", Obs9PositionMm, Color.Blue, Color.Blue, 10, 700, 0));

            Env.ObstaclesRect.Add(new RectangularObstacle("obs10", Obs10PositionMm, Color.Blue, Color.Blue, 150, 3, 0));
            #endregion

            //presentCollisionCycle = 0;//记录碰撞周期
            //escapeCollisionTime = 1;//碰撞后下次检测为三秒后
            //collisionFlag = false;//过了三秒后再产生碰撞的标志量
            //collisionFishAndObstacles = false;// 机器鱼与障碍物的碰撞标志
            //collisionBallAndObstacles = false;// 球与障碍物的碰撞标志

            ResetDecision();
            ResetSomeLocomotionPara();
            SetEnvironmentSingleFishPushBallAvoidObstacles();
            SetTeamsAndFishesSingleFishPushBallAvoidObstacles();

        }

        public override void ProcessControlRules()
        {
            DataProcessingOnPushingBall();
            JudgeFoulingOnPushingBall();
            HandleWhileFoul();
            HandleWhileBallGoOverBlackLine();
            GameOverHandler();

        }


        /// <summary>
        /// 球越过黑线的处理函数
        /// </summary>
        private void HandleWhileBallGoOverBlackLine()
        {
            MyMission mymission = MyMission.Instance();
            //球完全越过黑线
            if ((mymission.EnvRef.Balls[0].PositionMm.X + mymission.EnvRef.Balls[0].RadiusMm) <= blackLineX)
            {
                mymission.ParasRef.IsRunning = false;//比赛停止    
                System.Windows.Forms.MessageBox.Show("Congradulations! ","Confirming",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            }

        }

        /// <summary>
        /// 比赛时间结束的处理
        /// </summary>
        private void GameOverHandler()
        {
            MyMission mymission = MyMission.Instance();
            //比赛时间为0
            
            if (mymission.ParasRef.RemainingCycles <= 0)
            {
                mymission.ParasRef.IsRunning = false;//比赛停止
                System.Windows.Forms.MessageBox.Show("Sorry !You had only finished" + finishedCountOneWay+"oneway,cost seconds!", "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
               
            }
        }

         /// <summary>
        /// 单鱼推球避障犯规判断
        /// </summary>
        /// <returns></returns>
        private void JudgeFoulingOnPushingBall()
        {
            MyMission myMission = MyMission.Instance();//当前仿真使命的信息
           
            CollisionType ballCollisionType = myMission.EnvRef.Balls[0].Collision;
            CollisionType fishCollisionType = myMission.TeamsRef[0].Fishes[0].Collision[0];

            if (collisionFlag == false)//3秒之内没有碰撞产生
            {
                //机器鱼与障碍物之间的碰撞
                if (fishCollisionType == CollisionType.FISH_OBSTACLE)
                {
                    collisionFishAndObstacles = true;
                    collisionFlag = true;
                    
                }
                //球与障碍物之间的碰撞
                else if (ballCollisionType == CollisionType.BALL_OBSTACLE)
                {
                    collisionBallAndObstacles = true;
                    collisionFlag = true;
                }

            }
            else
            {
                if (CommonPara.RemainingCycles <= (presentCollisionCycle - escapeCollisionTime * 1000 / CommonPara.MsPerCycle))
                {
                    collisionFlag = false;
                }
             }
        }

       /// <summary>
       /// 单鱼推球避障，机器鱼或者球碰到障碍物的处理
       /// </summary>
        private void HandleWhileFoul()
        { 
            MyMission myMission = MyMission.Instance();//当前仿真使命的信息
            FishSingleFishPushBallAvoidObstacles fish = Teams[0].Fishes[0];
            Ball ball = myMission.EnvRef.Balls[0];
            
            //myMission.ParasRef.IsPauseNeeded = true;//这里还待考虑
        
            if (collisionFishAndObstacles)
            {
                
                System.Windows.Forms.MessageBox.Show(" Collision between Fish and Obstacles", "Time - 5s");
                 
                CommonPara.RemainingCycles -= 5000 / CommonPara.MsPerCycle;
                
                // 有鱼碰到障碍物的犯规情况出现时，将鱼移到已完成单程，进入下一个单程的初始位置；
                if (finishedCountOneWay == 0)
                {
                    fish.PositionMm.X =myMission.EnvRef.ObstaclesRect[1].PositionMm.X-myMission.EnvRef.ObstaclesRect[1].LengthMm-fish.BodyLength;
                    fish.PositionMm.Z =myMission.EnvRef.ObstaclesRect[4].PositionMm.Z-myMission.EnvRef.ObstaclesRect[4].WidthMm+fish.BodyWidth ;
                    fish.BodyDirectionRad = 0;
                    fish.VelocityDirectionRad = 0 ;
      
                }
                else if (finishedCountOneWay == 1)
                { 
                    fish.PositionMm.X = myMission.EnvRef.ObstaclesRect[4].PositionMm.X+myMission.EnvRef.ObstaclesRect[4].LengthMm+fish.BodyLength ;
                    fish.PositionMm.Z = myMission.EnvRef.ObstaclesRect[4].PositionMm.Z+myMission.EnvRef.ObstaclesRect[4].WidthMm+fish.BodyWidth  ;
                    fish.BodyDirectionRad =0 ;
                    fish.VelocityDirectionRad = 0 ;
                }
                else if(finishedCountOneWay == 2)
                {
                    fish.PositionMm.X = myMission.EnvRef.ObstaclesRect[9].PositionMm.X+myMission.EnvRef.ObstaclesRect[9].LengthMm+fish.BodyLength;
                    fish.PositionMm.Z = myMission.EnvRef.ObstaclesRect[9].PositionMm.Z+myMission.EnvRef.ObstaclesRect[9].WidthMm+fish.BodyWidth;
                    fish.BodyDirectionRad = (float) Math.PI;
                    fish.VelocityDirectionRad =0;
                }
                else if (finishedCountOneWay == 3)
                {
                    fish.PositionMm.X = myMission.EnvRef.ObstaclesRect[4].PositionMm.X-myMission.EnvRef.ObstaclesRect[4].LengthMm-fish.BodyLength;
                    fish.PositionMm.Z =myMission.EnvRef.ObstaclesRect[4].PositionMm.Z+myMission.EnvRef.ObstaclesRect[4].WidthMm+fish.BodyWidth ;
                    fish.BodyDirectionRad =(float) Math.PI ;
                    fish.VelocityDirectionRad =0 ;
                }
                collisionFishAndObstacles = false;
                presentCollisionCycle = CommonPara.RemainingCycles;
                collisionFlag = true;
            }
            if (collisionBallAndObstacles)
            {
                System.Windows.Forms.MessageBox.Show("Collision between Ball and Obstacles", "Time - 3s"); 
                CommonPara.RemainingCycles -= 3000 / CommonPara.MsPerCycle;
                
                // 有球撞到障碍物的犯规情况出现时，球也移到相应的位置
                if ((finishedCountOneWay == 0)||(finishedCountOneWay == 1)||(finishedCountOneWay == 2))//完成2个单程之前，球都应该保持在初始位置附近
                {
                   //球的初始位置
                    ball.PositionMm.X = 1030;
                    ball.PositionMm.Z = 0;
                    ball.VelocityMmPs = 0;
                    ball.VelocityDirectionRad = 0;

                }
                else if (finishedCountOneWay == 3)//已经只差最后一个单程了，此时球在碰到障碍物设置球在原点
                {
                    ball.PositionMm.X=0;
                    ball.PositionMm.Z=0;
                    ball.VelocityMmPs = 0;
                    ball.VelocityDirectionRad = 0;

                }              
                collisionBallAndObstacles = false;
                presentCollisionCycle = CommonPara.RemainingCycles;
                collisionFlag = true;
            }
            
  
        }

       /// <summary>
       /// 单鱼推球过程中的数据记录，记录完成第一次完成各个单程的时间，及共完成单程的个数
       /// </summary>
        private void DataProcessingOnPushingBall()
        {
            MyMission myMission = MyMission.Instance();//当前仿真使命的信息
            FishSingleFishPushBallAvoidObstacles fish = Teams[0].Fishes[0];
            
            Ball ball=myMission.EnvRef.Balls[0];
            //记录完成每个单程的时间和完成的单程数目
            if ((fish.PositionMm.X > (myMission.EnvRef.ObstaclesRect[0].PositionMm.X+fish.BodyLength)&&(finishedCountOneWay==0)))
            {//判断完成第一次完成第一个单程
                
                finshedTimeOneWay[0] = myMission.ParasRef.TotalSeconds - myMission.ParasRef.RemainingCycles/myMission.ParasRef.MsPerCycle;
                finishedCountOneWay=1;

            }
            else if((fish.PositionMm.X > (myMission.EnvRef.ObstaclesRect[9].PositionMm.X+fish.BodyLength)&&(finishedCountOneWay==1)))
            {//判断完成第一次完成第二个单程
               
                finshedTimeOneWay[1]=myMission.ParasRef.TotalSeconds - myMission.ParasRef.RemainingCycles/myMission.ParasRef.MsPerCycle;;
                finishedCountOneWay=2;
            }
            else if((fish.PositionMm.X < (myMission.EnvRef.ObstaclesRect[9].PositionMm.X-fish.BodyLength)&&(finishedCountOneWay==2))
                &&(ball.PositionMm.X<(myMission.EnvRef.ObstaclesRect[9].PositionMm.X-ball.RadiusMm*2)))
            {//判断完成第一次完成第三个单程
                
                finshedTimeOneWay[2]=myMission.ParasRef.TotalSeconds - myMission.ParasRef.RemainingCycles/myMission.ParasRef.MsPerCycle;;
                finishedCountOneWay=3;
            }
            else if((fish.PositionMm.X < (myMission.EnvRef.ObstaclesRect[0].PositionMm.X-fish.BodyLength)&&(finishedCountOneWay==3))
                &&(ball.PositionMm.X<(myMission.EnvRef.ObstaclesRect[0].PositionMm.X-ball.RadiusMm*2)))
            {//判断完成第一次完成第四个单程
                
                finshedTimeOneWay[3]=myMission.ParasRef.TotalSeconds - myMission.ParasRef.RemainingCycles/myMission.ParasRef.MsPerCycle;;
                finishedCountOneWay=4;
            }
        }
    }
}