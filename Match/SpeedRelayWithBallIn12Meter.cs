//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Match4TeamsTypes.cs
// Date: 20101209  Author: Weiqingdan  Version: 1
// Description: 12米带球接力竞速比赛项目相关的仿真机器鱼，仿真环境和仿真使命定义文件
// Histroy:
// Date: 20110511 Author: LiYoubing
// Modification: 
// 1.增加一个传递给策略的标志量
// 2.整理代码
// Date: 20111113 Author: ZhangBo
// Modification: 
// 1.添加障碍物
// 2.整理代码
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;//add by liufei
using System.Runtime.CompilerServices; // for MethodImpl
using System.Drawing;
using xna = Microsoft.Xna.Framework;
using System.Diagnostics;

using URWPGSim2D.Common;

namespace URWPGSim2D.Match
{
    /// <summary>
    /// 12米带球接力竞速仿真使命特有的仿真机器鱼类
    /// </summary>
    [Serializable]
    public class FishSpeedRelayWithBallIn12Meter : RoboFish
    {
        // 在这里定义12米带球接力竞速仿真使命特有的仿真机器鱼的特性和行为
    }

    /// <summary>
    /// 12米带球接力竞速仿真使命特有的仿真环境类
    /// </summary>
    [Serializable]
    public class EnvironmentSpeedRelayWithBallIn12Meter : SimEnvironment
    {
        // 在这里定义12米带球接力竞速仿真使命特有的仿真环境的特性和行为
    }

    /// <summary>
    /// 12米带球接力竞速具体仿真使命类
    /// </summary>
    [Serializable]
    public partial class SpeedRelayWithBallIn12Meter : Mission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static SpeedRelayWithBallIn12Meter instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SpeedRelayWithBallIn12Meter Instance()
        {
            if (instance == null)
            {
                instance = new SpeedRelayWithBallIn12Meter();
            }
            return instance;
        }

        private SpeedRelayWithBallIn12Meter()
        {
            CommonPara.MsPerCycle = 100;
            CommonPara.TotalSeconds = 300;
            CommonPara.TeamCount = 1;
            CommonPara.FishCntPerTeam = 2;
            CommonPara.Name = "12米带球接力竞速";
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
        public List<Team<FishSpeedRelayWithBallIn12Meter>> Teams = new List<Team<FishSpeedRelayWithBallIn12Meter>>();

        /// <summary>
        ///  仿真使命（比赛或实验项目）所使用的具体仿真环境
        /// </summary>
        public EnvironmentSpeedRelayWithBallIn12Meter Env = new EnvironmentSpeedRelayWithBallIn12Meter();
        #endregion

        #region private and protected methods
        /// <summary>
        /// 初始化当前使命参与队伍列表及每支队伍的仿真机器鱼对象，在当前使命类构造函数中调用
        /// 该方法要在调用SetMissionCommonPara设置好仿真使命公共参数（如每队队员数量）之后调用
        /// </summary>
        private void InitTeamsAndFishes()
        {
            for (int i = 0; i < CommonPara.TeamCount; i++)
            {
                // 给具体仿真机器鱼队伍列表添加新建的具体仿真机器鱼队伍
                Teams.Add(new Team<FishSpeedRelayWithBallIn12Meter>());

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
                    Teams[i].Fishes.Add(new FishSpeedRelayWithBallIn12Meter());

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
            // 第一次调用的时候，添加仿真水球
            if ((Env.Balls != null) && (Env.Balls.Count == 0))
            {
                Env.Balls.Add(new Ball());
            }

            Field f = Field.Instance();

            // 初始化时可以置于任何位置 ResetEnvironment中会具体设置
            xna.Vector3 tmpPositionMm = new xna.Vector3(0, 0, 0);

            //Env.ObstaclesRect.Add(new RectangularObstacle("obs1", tmpPositionMm, Color.Black, Color.DarkGray, 
            //    f.GoalDepthMm, f.BottomMm  - f.GoalWidthMm / 2, 0));
            //Env.ObstaclesRect.Add(new RectangularObstacle("obs2", tmpPositionMm, Color.Black, Color.DarkGray, 
            //    f.GoalDepthMm, f.BottomMm  - f.GoalWidthMm / 2, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs1", tmpPositionMm, Color.Black, Color.Green,
                Convert.ToInt32( f.FieldLengthXMm - f.GoalDepthMm * 2 - f.GoalWidthMm * 1.5 * 2), Convert.ToInt32( f.GoalDepthMm * 1.5), 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs2", tmpPositionMm, Color.Black, Color.Green,
                Convert.ToInt32(f.GoalWidthMm * 2), Convert.ToInt32(f.FieldLengthZMm - f.GoalDepthMm * 1.5 * 2 - f.GoalWidthMm * 2), 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs3", tmpPositionMm, Color.Black, Color.Green,
                Convert.ToInt32( f.FieldLengthXMm - f.GoalDepthMm * 2 - f.GoalWidthMm * 1.5 * 2), Convert.ToInt32( f.GoalDepthMm * 1.5), 0));
        }

        // Zhangbo 20111113
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真障碍物恢复默认位置
        /// </summary>
        public override void ResetObstacles()
        {
            Field f = Field.Instance();

            // 将三个方形障碍物恢复默认位置
            Env.ObstaclesRect[0].PositionMm = new xna.Vector3(0, 0,
               Convert.ToSingle(f.TopMm + f.GoalWidthMm + f.GoalDepthMm * 1.5 / 2));
            Env.ObstaclesRect[1].PositionMm = new xna.Vector3(0, 0, 0);
            Env.ObstaclesRect[2].PositionMm = new xna.Vector3(0, 0,
                Convert.ToSingle(f.BottomMm - f.GoalWidthMm - f.GoalDepthMm * 1.5 / 2));

            // 提供从界面修改仿真场地尺寸及障碍物各项参数的功能后恢复默认就不仅仅是恢复默认位置了
            for (int i = 0; i < 3; i++)
            {
                Env.ObstaclesRect[i].IsDeletionAllowed = false; // 不允许删除
                Env.ObstaclesRect[i].ColorBorder = Color.Green;
            }
            Env.ObstaclesRect[0].LengthMm = Convert.ToInt32(f.FieldLengthXMm - f.GoalDepthMm * 2 - f.GoalWidthMm * 1.5 * 2);
            Env.ObstaclesRect[0].WidthMm = Convert.ToInt32(f.GoalDepthMm * 1.5);
            Env.ObstaclesRect[1].LengthMm = Convert.ToInt32(f.GoalWidthMm);
            Env.ObstaclesRect[1].WidthMm = Convert.ToInt32(f.FieldLengthZMm - f.GoalDepthMm * 1.5 * 2 - f.GoalWidthMm * 2);
            Env.ObstaclesRect[2].LengthMm = Convert.ToInt32(f.FieldLengthXMm - f.GoalDepthMm * 2 - f.GoalWidthMm * 1.5 * 2);
            Env.ObstaclesRect[2].WidthMm = Convert.ToInt32(f.GoalDepthMm * 1.5);
        }

        /// <summary>
        /// 初始化当前仿真使命类特有的需要传递给策略的变量名和变量值，在当前使命类构造函数中调用
        /// 添加到仿真使命基类的哈希表Hashtable成员中
        /// </summary>
        private void InitHtMissionVariables()
        {
            // 初始化当前已经完成的单程数量
            HtMissionVariables.Add("FinishedSingleTripCount", "0");
        }

        /// <summary>
        /// 更新当前使命类特有的需要传递给策略的变量名和变量值，在ProcessControlRules中调用
        /// </summary>
        private void UpdateHtMissionVariables()
        {
            // 更新当前已经完成的单程数量
            HtMissionVariables["FinishedSingleTripCount"] = FinishedSingleTripCount.ToString();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值
        /// </summary>
        public override void ResetTeamsAndFishes()
        {
            Field f = Field.Instance();
            if (Teams != null)
            {
                //设置比赛前机器鱼的初始的位姿信息（初始坐标，左半场的鱼体方向，右半场的鱼体的方向，右半场的速度方向）
                Teams[0].Fishes[0].PositionMm = new xna.Vector3(f.LeftMm + Teams[0].Fishes[0].BodyLength * 2 + Teams[0].Fishes[0].BodyWidth, 0, 0);
                Teams[0].Fishes[1].PositionMm = new xna.Vector3(f.RightMm - Teams[0].Fishes[1].BodyLength * 2 - Teams[0].Fishes[1].BodyWidth, 0, 0);

                Teams[0].Fishes[0].BodyDirectionRad = 0;
               
                Teams[0].Fishes[1].BodyDirectionRad = (float)(-Math.PI);
                Teams[0].Fishes[1].VelocityDirectionRad = (float)(-Math.PI);
                
                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    Teams[0].Fishes[j].CountDrawing = 0;        //仿真机器鱼绘制次数计数
                    Teams[0].Fishes[j].InitPhase = j * 5;       //尾部三个关节正弦摆动公式的初始调节相位值
                } 
            }
        }

        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真水球恢复默认位置
        /// </summary>
        public override void ResetBalls()
        {
            Field f = Field.Instance();
            Ball b = Env.Balls[0];
            // 设置唯一的仿真水球位置
            //Env.Balls[0].PositionMm = new xna.Vector3(Field.Instance().LeftMm
            //    + Teams[0].Fishes[0].BodyLength * 3 + Env.Balls[0].RadiusMm, 0, 0);
            Env.Balls[0].PositionMm = new xna.Vector3(-f.GoalWidthMm / 2 - b.RadiusMm-10.0f, 0,
                Convert.ToSingle(f.TopMm + f.GoalWidthMm + f.GoalDepthMm * 1.5 + b.RadiusMm));
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
                // 第0支队伍仿真机器鱼后端色标即编号颜色为黑色
                Teams[0].Fishes[j].ColorId = Color.Black;
            }

            for (int i = 0; i < Env.Balls.Count; i++)
            {
                // 所有仿真水球默认填充颜色和边框颜色均为粉色
                Env.Balls[i].ColorFilled = Color.Pink;
                Env.Balls[i].ColorBorder = Color.Pink;
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

            // 将当前仿真使命涉及的全部障碍物恢复默认位置
            ResetObstacles();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命特有的内部变量设置为默认值
        /// </summary>
        private void ResetInternalVariables()
        {
            FinishedSingleTripCount = 0;
            HtMissionVariables["FinishedSingleTripCount"] = "0";
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
        /// 当前已经完成的单程数量 取值范围{0,1,2,3,4}
        /// </summary>
        private int FinishedSingleTripCount = 0;

        /// <summary>
        /// 第1/2/3/4个单程完成时剩余的周期数
        /// 在比赛时间用完而没有完成任务的情况下，显示完成的单程数和最后一个完成的单程完成时剩余的时间
        /// </summary>
        private int[] FinishedSingleTripTime = new int[4];

        /// <summary>
        /// 处理12米接力竞速具体仿真使命的控制规则
        /// </summary>
        public override void ProcessControlRules()
        {
            // 处理4个单程运动状态的判定和切换任务
            HandleSingleTripTask();

            //处理犯规1 added by zhangbo 20111220
            FoulType1Handler();

            //处理犯规2 added by zhangbo 20111227
            FoulType2Handler();

            // 处理比赛结束的判断和响应任务
            GameOverHandler();

            // 更新当前使命类特有的需要传递给策略的变量名和变量值
            UpdateHtMissionVariables();
        }

        // add by liufei20110320
        /// <summary>
        /// 处理4个单程运动状态的判定和切换任务
        /// </summary>
        private void HandleSingleTripTask()
        {
            Field f = Field.Instance();

            if (FinishedSingleTripCount == 0)
            {// 第一个单程运动的处理
                //if ((MyMission.Instance().DecisionRef[0, 1].VCode != 0) || (MyMission.Instance().DecisionRef[0, 1].TCode != 7))
                //{// 第一个单程令第1条机器鱼静止
                //    Teams[0].Fishes[1].AngularVelocityRadPs = 0;
                //    Teams[0].Fishes[1].VelocityMmPs = 0;
                //}
                //if ((Env.Balls[0].PositionMm.X > (f.RightMm - f.GoalDepthMm - f.ForbiddenZoneLengthXMm + Env.Balls[0].RadiusMm)) &&
                //    (Math.Abs(Env.Balls[0].PositionMm.Z) < (f.ForbiddenZoneLengthZMm / 2.0) - Env.Balls[0].RadiusMm))
                //if ((Env.Balls[0].PositionMm.X > (f.RightMm - f.GoalDepthMm + Env.Balls[0].RadiusMm)) &&
                //    (Math.Abs(Env.Balls[0].PositionMm.Z) < (f.GoalWidthMm / 2.0) - Env.Balls[0].RadiusMm))
                if (Env.Balls[0].PositionMm.X > 0 && (Env.Balls[0].PositionMm.X < (f.RightMm - f.GoalDepthMm - 1.5 * f.GoalWidthMm - Env.Balls[0].RadiusMm)) &&
                    (Math.Abs(Env.Balls[0].PositionMm.Z) < Math.Abs(f.BottomMm - f.GoalWidthMm - f.GoalDepthMm * 1.5 - Env.Balls[0].RadiusMm)))
                {// 判定第一个单程的结束
                    FinishedSingleTripCount = 1;
                    Teams[0].Para.Score = 1;
                    FinishedSingleTripTime[0] = MyMission.Instance().ParasRef.RemainingCycles;
                }
            }
            else if (FinishedSingleTripCount == 1)
            {// 第二个单程运动的处理
                //if ((MyMission.Instance().DecisionRef[0, 0].VCode != 0) || (MyMission.Instance().DecisionRef[0, 0].TCode != 7))
                //{// 第二个单程令第0条机器鱼静止
                //    Teams[0].Fishes[0].AngularVelocityRadPs = 0;
                //    Teams[0].Fishes[0].VelocityMmPs = 0;
                //}
                //if ((Env.Balls[0].PositionMm.X < (f.LeftMm + f.GoalDepthMm - Env.Balls[0].RadiusMm)) &&
                //    (Math.Abs(Env.Balls[0].PositionMm.Z) < (f.GoalWidthMm / 2.0 - Env.Balls[0].RadiusMm)))
                if (Env.Balls[0].PositionMm.X < 0 && (Env.Balls[0].PositionMm.X > (f.LeftMm + f.GoalDepthMm + 1.5 * f.GoalWidthMm + Env.Balls[0].RadiusMm)) &&
                    (Math.Abs(Env.Balls[0].PositionMm.Z) < Math.Abs(f.BottomMm - f.GoalWidthMm - f.GoalDepthMm * 1.5 - Env.Balls[0].RadiusMm)))
                {// 判定第二个单程的结束
                    FinishedSingleTripCount = 2;
                    Teams[0].Para.Score = 2;
                    FinishedSingleTripTime[1] = MyMission.Instance().ParasRef.RemainingCycles;
                }
            }
            else if (FinishedSingleTripCount == 2)
            {// 第三个单程运动的处理
                //if ((MyMission.Instance().DecisionRef[0, 0].VCode != 0) || (MyMission.Instance().DecisionRef[0, 0].TCode != 7))
                //{// 第三个单程令第0条机器鱼静止
                //    Teams[0].Fishes[0].AngularVelocityRadPs = 0;
                //    Teams[0].Fishes[0].VelocityMmPs = 0;
                //}
                if (Env.Balls[0].PositionMm.X > 0 && (Env.Balls[0].PositionMm.X < (f.RightMm - f.GoalDepthMm - 1.5 * f.GoalWidthMm - Env.Balls[0].RadiusMm)) &&
                    (Math.Abs(Env.Balls[0].PositionMm.Z) < Math.Abs(f.BottomMm - f.GoalWidthMm - f.GoalDepthMm * 1.5 - Env.Balls[0].RadiusMm)))
                {// 判定第三个单程的结束
                    FinishedSingleTripCount = 3;
                    Teams[0].Para.Score = 3;
                    FinishedSingleTripTime[2] = MyMission.Instance().ParasRef.RemainingCycles;
                }
            }
            else if (FinishedSingleTripCount == 3)
            {// 第四个单程运动的处理
                //if ((MyMission.Instance().DecisionRef[0, 1].VCode != 0) || (MyMission.Instance().DecisionRef[0, 1].TCode != 7))
                //{// 第四个单程令第1条机器鱼静止
                //    Teams[0].Fishes[1].AngularVelocityRadPs = 0;
                //    Teams[0].Fishes[1].VelocityMmPs = 0;
                //}
                if (Env.Balls[0].PositionMm.X < 0 && (Env.Balls[0].PositionMm.X > (f.LeftMm + f.GoalDepthMm + 1.5 * f.GoalWidthMm + Env.Balls[0].RadiusMm)) &&
                    (Math.Abs(Env.Balls[0].PositionMm.Z) < Math.Abs(f.BottomMm - f.GoalWidthMm - f.GoalDepthMm * 1.5 - Env.Balls[0].RadiusMm)))
                {// 判定第四个单程的结束
                    FinishedSingleTripCount = 4;
                    Teams[0].Para.Score = 4;
                    FinishedSingleTripTime[3] = MyMission.Instance().ParasRef.RemainingCycles;
                }
            }
        }

        /// <summary>
        /// 犯规处理函数1 added by zhangbo 20111220
        /// 规定1号机器鱼只能通过上面通道，机器鱼2只能通过下面通道。
        /// </summary>
        private void FoulType1Handler()
        {
            Field f = Field.Instance();
            //1号鱼进入下方通道，则犯规。将1号机器鱼置于初始位置。
            if (Math.Abs(Teams[0].Fishes[0].PositionMm.X) < (f.RightMm - f.GoalDepthMm - 1.5 * f.GoalWidthMm - Env.Balls[0].RadiusMm) &&
                Teams[0].Fishes[0].PositionMm.Z > f.BottomMm - f.GoalWidthMm+Env.Balls[0].RadiusMm)
            {
                Teams[0].Fishes[0].PositionMm = new xna.Vector3(f.LeftMm + Teams[0].Fishes[0].BodyLength * 2, 0, 0);
                Teams[0].Fishes[0].BodyDirectionRad = 0;
                MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef,
                string.Format("Enter the Channel Not Allowed:\n【{0}】Fish {1} Fouled!\n",
                Teams[0].Para.Name, 1),
                "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            //2号鱼进入上方通道，则犯规。将2号机器鱼置于初始位置。
            if (Math.Abs(Teams[0].Fishes[1].PositionMm.X) < (f.RightMm - f.GoalDepthMm - 1.5 * f.GoalWidthMm - Env.Balls[0].RadiusMm) &&
                Teams[0].Fishes[1].PositionMm.Z < f.TopMm + f.GoalWidthMm-Env.Balls[0].RadiusMm)
            {
                Teams[0].Fishes[1].PositionMm = new xna.Vector3(f.RightMm - Teams[0].Fishes[1].BodyLength * 2, 0, 0);
                Teams[0].Fishes[1].BodyDirectionRad = (float)Math.PI;
                MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef,
                string.Format("Enter the Channel Not Allowed:\n【{0}】Fish {1} Fouled!\n",
                Teams[0].Para.Name, 2),
                "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 犯规处理函数2 added by zhangbo 20111227
        /// 各个单程内，水球只能通过指定通道。
        /// </summary>
        private void FoulType2Handler()
        {
            Field f = Field.Instance();
            Ball b = Env.Balls[0];
            if (FinishedSingleTripCount == 1 || FinishedSingleTripCount == 3)
            {//第二、四个单程，球进入上方通道，则犯规。
                if (Math.Abs(Env.Balls[0].PositionMm.X) < (f.RightMm - f.GoalDepthMm - 1.5 * f.GoalWidthMm - Env.Balls[0].RadiusMm) &&
                    Env.Balls[0].PositionMm.Z < f.TopMm + f.GoalWidthMm - Env.Balls[0].RadiusMm)
                {
                    Env.Balls[0].PositionMm.X = f.GoalWidthMm / 2 + b.RadiusMm + 10.0f;
                    Env.Balls[0].PositionMm.Z = Convert.ToSingle(f.TopMm + f.GoalWidthMm + f.GoalDepthMm * 1.5 + b.RadiusMm);
                    MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef,
                    string.Format("Ball should be pushed through the other channel:\n【{0}】Fish {1} Fouled!\n",
                    Teams[0].Para.Name, 1),
                    "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            if (FinishedSingleTripCount == 2)
            {
                //第三个单程，球进入下方通道，则犯规。
                if (Math.Abs(Env.Balls[0].PositionMm.X) < (f.RightMm - f.GoalDepthMm - 1.5 * f.GoalWidthMm - Env.Balls[0].RadiusMm) &&
                    Env.Balls[0].PositionMm.Z > f.BottomMm - f.GoalWidthMm + Env.Balls[0].RadiusMm)
                {
                    Env.Balls[0].PositionMm.X = -f.GoalWidthMm / 2 - b.RadiusMm - 10.0f;
                    Env.Balls[0].PositionMm.Z = Convert.ToSingle(f.TopMm + f.GoalWidthMm + f.GoalDepthMm * 1.5 + b.RadiusMm);
                    MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef,
                    string.Format("Ball should be pushed through the other channel:\n【{0}】Fish {1} Fouled!\n",
                    Teams[0].Para.Name, 2),
                    "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        //add by liufei 2011.03.17
        /// <summary>
        /// 处理比赛结束的判断和响应任务 包括4个单程任务完成和时间耗完两种情况
        /// </summary>
        private void GameOverHandler()
        {
            MyMission mymission = MyMission.Instance();
            string strMsg = "";

            // 使命结束标志 时间耗完或仿真水球进入左球门
            bool bIsCompleted = false;  // 用ParasRef.IsRunning会导致Restart时也弹出对话框

            if (FinishedSingleTripCount == 4)
            {// 4个单程任务完成
                mymission.ParasRef.IsRunning = false;           // 置比赛结束标志使仿真循环中断
                bIsCompleted = true;
                strMsg = "Competition task is completed!\n";    // 任务完成提示
            }
            if (mymission.ParasRef.RemainingCycles <= 0)
            {// 时间耗完 比赛结束标志在仿真主循环里设置此处不管
                bIsCompleted = true;
                strMsg += "Competition time is over!\n";        // 时间耗完提示
            }
            if (bIsCompleted == true)
            {// 使命结束 弹出提示 记录完成的单程数
                strMsg += string.Format("The SingleTrip count finished: {0}.\n", FinishedSingleTripCount);
                if (FinishedSingleTripCount > 0)
                {// 完成至少1个单程则记录最后一个单程完成时所剩余的时间
                    int iRemainingMs = FinishedSingleTripTime[FinishedSingleTripCount - 1] * mymission.ParasRef.MsPerCycle;
                    int iRemainingSec = iRemainingMs / 1000;
                    // 按"MM:SS:MsMsMs"格式显示剩余时间
                    strMsg += string.Format("Remaining time when the {0}th SingleTrip is completed: {1:00}:{2:00}:{3:00}.", 
                        FinishedSingleTripCount, iRemainingSec / 60, iRemainingSec % 60, iRemainingMs % 1000);
                }
                MessageBox.Show(strMsg, "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion
    }
}