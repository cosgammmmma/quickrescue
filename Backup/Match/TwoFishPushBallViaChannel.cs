//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: TwoFishPushBallViaChannel.cs
// Date: 20101209  Author: ChenWei  Version: 1
// Description: 双鱼协作过孔推球比赛项目相关的仿真机器鱼，仿真环境和仿真使命定义文件
// Histroy:
// Date: 20110512 Author: LiYoubing
// Modification: 
// 1.实现2个协作任务完成状态的独立判断
// 2.修正比赛结束判断
// 3.增加三个传递给策略的标志量
// 4.整理代码
// Date: 20111113 Author: ZhangBo
// Modification: 
// 1.更改障碍物位置
// 2.更改水球初始位置
// 3.更改仿真鱼初始位置
// 4.更改得分计算方法
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Text;

using System.Runtime.CompilerServices; 
using System.Drawing;
using xna = Microsoft.Xna.Framework;
using URWPGSim2D.Common;

namespace URWPGSim2D.Match
{
    /// <summary>
    /// 双鱼协作过孔推球仿真使命特有的仿真机器鱼类
    /// </summary>
    [Serializable]
    public class FishTwoFishPushBallViaChannel : RoboFish
    {
        // 在这里定义双鱼协作过孔推球仿真使命特有的仿真机器鱼的特性和行为
    }

    /// <summary>
    /// 双鱼协作过孔推球仿真使命特有的仿真环境类
    /// </summary>
    [Serializable]
    public class EnvironmentTwoFishPushBallViaChannel : SimEnvironment
    {
        // 在这里定义双鱼协作过孔推球仿真使命特有的仿真环境的特性和行为
    }

    /// <summary>
    /// 双鱼协作过孔推球仿真使命类
    /// </summary>
    [Serializable]
    public partial class TwoFishPushBallViaChannel : Mission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static TwoFishPushBallViaChannel instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)] //使全局只有一个实例
        public static TwoFishPushBallViaChannel Instance()
        {
            if (instance == null)
            {
                instance = new TwoFishPushBallViaChannel();
            }
            return instance;
        }

        /// <summary>
        /// 私有构造函数 在Instance()中调用 进程生命周期内只会被调用一次
        /// </summary>
        private TwoFishPushBallViaChannel()
        {
            CommonPara.MsPerCycle = 100;
            CommonPara.TeamCount = 1;
            CommonPara.FishCntPerTeam = 2;
            CommonPara.Name = "双鱼协作过孔推球";
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
        public List<Team<FishTwoFishPushBallViaChannel>> Teams = new List<Team<FishTwoFishPushBallViaChannel>>(); //学习一下list

        /// <summary>
        ///  仿真使命（比赛或实验项目）所使用的具体仿真环境
        /// </summary>
        public EnvironmentTwoFishPushBallViaChannel Env = new EnvironmentTwoFishPushBallViaChannel();//实例化继承的类,构建类的对象.
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
                Teams.Add(new Team<FishTwoFishPushBallViaChannel>());  //在建立的列表对象里面添加具体对象。

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
                    Teams[i].Para.MyHalfCourt = HalfCourt.LEFT;
                }

                // 给通用仿真机器鱼队伍设置公共参数
                TeamsRef[i].Para = Teams[i].Para;

                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    // 给具体仿真机器鱼队伍添加新建的具体仿真机器鱼
                    Teams[i].Fishes.Add(new FishTwoFishPushBallViaChannel());

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
            Env.ObstaclesRect.Add(new RectangularObstacle("obs1", tmpPositionMm, Color.Black, Color.DarkGray,
                f.GoalDepthMm, f.FieldLengthZMm - f.GoalWidthMm, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs2", tmpPositionMm, Color.Black, Color.DarkGray,
                f.GoalDepthMm, f.FieldLengthZMm - f.GoalWidthMm, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs3", tmpPositionMm, Color.Black, Color.DarkGray,
                f.GoalDepthMm, f.FieldLengthZMm - f.GoalWidthMm, 0));
        }

        /// <summary>
        /// 初始化当前仿真使命类特有的需要传递给策略的变量名和变量值，在当前使命类构造函数中调用
        /// 添加到仿真使命基类的哈希表Hashtable成员中
        /// </summary>
        private void InitHtMissionVariables()
        {
            // 初始化在执行任务1时仿真机器鱼0碰过仿真水球标志
            HtMissionVariables.Add("IsCollidedBallAndFish0AtStage1", "false");
            // 初始化在执行任务1时仿真机器鱼1碰过仿真水球标志
            HtMissionVariables.Add("IsCollidedBallAndFish1AtStage1", "false");
            // 初始化在执行任务2时仿真机器鱼0碰过仿真水球标志
            HtMissionVariables.Add("IsCollidedBallAndFish0AtStage2", "false");
            // 初始化在执行任务2时仿真机器鱼1碰过仿真水球标志
            HtMissionVariables.Add("IsCollidedBallAndFish1AtStage2", "false");
            // 初始化在执行任务3时仿真机器鱼0碰过仿真水球标志
            HtMissionVariables.Add("IsCollidedBallAndFish0AtStage3", "false");
            // 初始化在执行任务3时仿真机器鱼1碰过仿真水球标志
            HtMissionVariables.Add("IsCollidedBallAndFish1AtStage3", "false");
            // 初始化在执行任务4时仿真机器鱼0碰过仿真水球标志
            HtMissionVariables.Add("IsCollidedBallAndFish0AtStage4", "false");
            // 初始化在执行任务4时仿真机器鱼1碰过仿真水球标志
            HtMissionVariables.Add("IsCollidedBallAndFish1AtStage4", "false");
            // 初始化正在执行的协作任务编号
            HtMissionVariables.Add("ExecutingTaskNo", "1");
        }

        /// <summary>
        /// 更新当前使命类特有的需要传递给策略的变量名和变量值，在ProcessControlRules中调用
        /// </summary>
        private void UpdateHtMissionVariables()
        {
            // 更新在执行任务1时仿真机器鱼0碰过仿真水球标志
            HtMissionVariables["IsCollidedBallAndFish0AtStage1"] = IsCollidedBallAndFish0AtStage1.ToString();
            // 更新在执行任务1时仿真机器鱼1碰过仿真水球标志
            HtMissionVariables["IsCollidedBallAndFish1AtStage1"] = IsCollidedBallAndFish1AtStage1.ToString();
            // 更新在执行任务2时仿真机器鱼0碰过仿真水球标志
            HtMissionVariables["IsCollidedBallAndFish0AtStage2"] = IsCollidedBallAndFish0AtStage2.ToString();
            // 更新在执行任务2时仿真机器鱼1碰过仿真水球标志
            HtMissionVariables["IsCollidedBallAndFish1AtStage2"] = IsCollidedBallAndFish1AtStage2.ToString();
            // 更新在执行任务3时仿真机器鱼0碰过仿真水球标志
            HtMissionVariables["IsCollidedBallAndFish0AtStage3"] = IsCollidedBallAndFish0AtStage3.ToString();
            // 更新在执行任务3时仿真机器鱼1碰过仿真水球标志
            HtMissionVariables["IsCollidedBallAndFish1AtStage3"] = IsCollidedBallAndFish1AtStage3.ToString();
            // 更新在执行任务4时仿真机器鱼0碰过仿真水球标志
            HtMissionVariables["IsCollidedBallAndFish0AtStage4"] = IsCollidedBallAndFish0AtStage4.ToString();
            // 更新在执行任务4时仿真机器鱼1碰过仿真水球标志
            HtMissionVariables["IsCollidedBallAndFish1AtStage4"] = IsCollidedBallAndFish1AtStage4.ToString();
            // 更新正在执行的协作任务编号
            HtMissionVariables["ExecutingTaskNo"] = ExecutingTaskNo.ToString();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值
        /// </summary>
        public override void ResetTeamsAndFishes()
        {
            Field f = Field.Instance();
            Random random = new Random();
            if (Teams != null)
            {
                //Teams[0].Fishes[0].PositionMm = new xna.Vector3(f.LeftMm + Teams[0].Fishes[0].BodyLength * 2
                //    + f.GoalDepthMm, 0, f.TopMm + Teams[0].Fishes[0].BodyWidth * 2); //鱼体的位置
                //Teams[0].Fishes[1].PositionMm = new xna.Vector3(f.LeftMm + Teams[0].Fishes[1].BodyLength * 2
                //    + f.GoalDepthMm, 0, f.BottomMm - Teams[0].Fishes[0].BodyWidth * 2);

                Teams[0].Fishes[0].PositionMm = new xna.Vector3(f.LeftMm + Teams[0].Fishes[0].BodyLength * 2
                    + f.GoalDepthMm, 0, f.BottomMm - Teams[0].Fishes[0].BodyWidth * 16); //鱼体的位置
                Teams[0].Fishes[1].PositionMm = new xna.Vector3(f.LeftMm + Teams[0].Fishes[1].BodyLength * 2
                    + f.GoalDepthMm, 0, f.BottomMm - Teams[0].Fishes[0].BodyWidth * 6);

                Teams[0].Fishes[0].BodyDirectionRad = xna.MathHelper.ToRadians(random.Next(0, 360));
                Teams[0].Fishes[1].BodyDirectionRad = xna.MathHelper.ToRadians(random.Next(0, 360));

                Teams[0].Fishes[0].CountDrawing = 0;
                Teams[0].Fishes[1].CountDrawing = 0;
                Teams[0].Fishes[0].InitPhase =  5;
                Teams[0].Fishes[1].InitPhase = 10;
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
            // 每次调用时都将唯一的仿真水球放到右半场中间
            //Env.Balls[0].PositionMm = new xna.Vector3(f.RightMm - f.GoalDepthMm - 4 * f.ForbiddenZoneLengthXMm / 5, 0, 0);
            //初始时刻仿真水球在场地右侧球门
            //Env.Balls[0].PositionMm = new xna.Vector3(f.RightMm - f.GoalDepthMm - b.RadiusMm, 0, f.BottomMm - b.RadiusMm);
            Env.Balls[0].PositionMm = new xna.Vector3(f.RightMm - b.RadiusMm, 0, 0);
        }

        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真障碍物恢复默认位置
        /// </summary>
        public override void ResetObstacles()
        {
            Field f = Field.Instance();
            
            // 将三个方形障碍物恢复默认位置
            Env.ObstaclesRect[0].PositionMm = new xna.Vector3((f.FieldLengthXMm - 2 * f.GoalDepthMm) / 4, 0,
                f.BottomMm - (f.FieldLengthZMm - f.GoalWidthMm) / 2);
            Env.ObstaclesRect[1].PositionMm = new xna.Vector3(0, 0,
                f.TopMm + (f.FieldLengthZMm - f.GoalWidthMm) / 2);
            Env.ObstaclesRect[2].PositionMm = new xna.Vector3(-(f.FieldLengthXMm - 2 * f.GoalDepthMm) / 4, 0,
                f.BottomMm - (f.FieldLengthZMm - f.GoalWidthMm) / 2);

            // 提供从界面修改仿真场地尺寸及障碍物各项参数的功能后恢复默认就不仅仅是恢复默认位置了
            for (int i = 0; i < 3; i++)
            {// 所有仿真方形障碍物长度/宽度相同
                Env.ObstaclesRect[i].IsDeletionAllowed = false; // 不允许删除
                Env.ObstaclesRect[i].LengthMm = f.GoalDepthMm;
                Env.ObstaclesRect[i].WidthMm = f.FieldLengthZMm - f.GoalWidthMm;
                Env.ObstaclesRect[i].ColorBorder = Color.Green;
            }
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

            // 将当前仿真使命涉及的全部仿真障碍物恢复默认位置
            ResetObstacles();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命特有的内部变量设置为默认值
        /// </summary>
        private void ResetInternalVariables()
        {
            IsCollidedBallAndFish0AtStage1 = false;
            IsCollidedBallAndFish1AtStage1 = false;
            IsCollidedBallAndFish0AtStage2 = false;
            IsCollidedBallAndFish1AtStage2 = false;
            IsCollidedBallAndFish0AtStage3 = false;
            IsCollidedBallAndFish1AtStage3 = false;
            IsCollidedBallAndFish0AtStage4 = false;
            IsCollidedBallAndFish1AtStage4 = false;
            IsCompleted = false;
            ExecutingTaskNo = 1;
            RemainingMsWhenScored1 = -1;
            RemainingMsWhenScored2 = -1;
            RemainingMsWhenScored3 = -1;
            RemainingMsWhenScored4 = -1;
            HtMissionVariables["IsCollidedBallAndFish0AtStage1"] = "false";
            HtMissionVariables["IsCollidedBallAndFish1AtStage1"] = "false";
            HtMissionVariables["IsCollidedBallAndFish0AtStage2"] = "false";
            HtMissionVariables["IsCollidedBallAndFish1AtStage2"] = "false";
            HtMissionVariables["IsCollidedBallAndFish0AtStage3"] = "false";
            HtMissionVariables["IsCollidedBallAndFish1AtStage3"] = "false";
            HtMissionVariables["IsCollidedBallAndFish0AtStage4"] = "false";
            HtMissionVariables["IsCollidedBallAndFish1AtStage4"] = "false";
            HtMissionVariables["ExecutingTaskNo"] = "1";
            for (int j = 0; j < 4; j++)
            {
                stageScore[j] = 0;
            }
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

        // Add by caiqiong 2011-3-11
        // Modified by LiYoubing 20110512
        /// <summary>
        /// 处理双鱼协作过孔推球具体仿真使命的控制规则
        /// </summary>
        public override void ProcessControlRules()
        {
            // 处理协作任务完成状态检测和切换工作
            CooperationHandler();

            // 处理比赛结束的判断和响应任务
            GameOverHandler();

            // 更新当前使命类特有的需要传递给策略的变量名和变量值
            UpdateHtMissionVariables();
        }

        #region 协作过孔仿真使命控制规则所需私有变量
        /// <summary>
        /// 在右半场执行任务1时仿真机器鱼0碰过仿真水球标志
        /// </summary>
        public bool IsCollidedBallAndFish0AtStage1 = false;

        /// <summary>
        /// 在右半场执行任务1时仿真机器鱼1碰过仿真水球标志
        /// </summary>
        public bool IsCollidedBallAndFish1AtStage1 = false;

        /// <summary>
        /// 在左半场执行任务2时仿真机器鱼0碰过仿真水球标志
        /// </summary>
        public bool IsCollidedBallAndFish0AtStage2 = false;

        /// <summary>
        /// 在左半场执行任务2时仿真机器鱼1碰过仿真水球标志
        /// </summary>
        public bool IsCollidedBallAndFish1AtStage2 = false;

        /// <summary>
        /// 在右半场执行任务1时仿真机器鱼0碰过仿真水球标志
        /// </summary>
        public bool IsCollidedBallAndFish0AtStage3 = false;

        /// <summary>
        /// 在右半场执行任务1时仿真机器鱼1碰过仿真水球标志
        /// </summary>
        public bool IsCollidedBallAndFish1AtStage3 = false;

        /// <summary>
        /// 在右半场执行任务1时仿真机器鱼0碰过仿真水球标志
        /// </summary>
        public bool IsCollidedBallAndFish0AtStage4 = false;

        /// <summary>
        /// 在右半场执行任务1时仿真机器鱼1碰过仿真水球标志
        /// </summary>
        public bool IsCollidedBallAndFish1AtStage4 = false;

        /// <summary>
        /// 正在执行的协作任务编号 取值范围{1,2,3,4}
        /// 1表示正在执行协作任务1
        /// 2表示正在执行协作任务2
        /// 3表示正在执行协作任务1
        /// 4表示正在执行协作任务2
        /// </summary>
        public int ExecutingTaskNo = 1;

        /// <summary>
        /// 使命结束标志 时间耗完或仿真水球进入左球门
        /// </summary>
        private bool IsCompleted = false;

        /// <summary>
        ///得到1分时剩余的仿真周期数转换得到的毫秒数
        /// </summary>
        private int RemainingMsWhenScored1 = -1;

        /// <summary>
        /// /得到2分时剩余的仿真周期数转换得到的毫秒数
        /// </summary>
        private int RemainingMsWhenScored2 = -1;
        /// <summary>
        /// /得到3分时剩余的仿真周期数转换得到的毫秒数
        /// </summary>
        private int RemainingMsWhenScored3 = -1;
        /// <summary>
        /// /得到4分时剩余的仿真周期数转换得到的毫秒数
        /// </summary>
        private int RemainingMsWhenScored4= -1;

        /// <summary>
        /// 标识每个阶段的得分情况的数组
        /// </summary>
        int[] stageScore = new int[] { 0, 0, 0, 0 };
        #endregion

        #region 协作过孔仿真使命控制规则具体处理过程
        /// <summary>
        /// 处理协作任务完成状态检测和切换工作
        /// </summary>
        private void CooperationHandler()
        {
            if (Teams[0].Fishes[0].Collision.Contains(CollisionType.FISH_BALL))
            {
                if (ExecutingTaskNo == 1)
                {// 设置在执行任务1时仿真机器鱼0碰过仿真水球标志
                    IsCollidedBallAndFish0AtStage1 = true;
                }
                else if (ExecutingTaskNo == 2)
                {// 设置在执行任务2时仿真机器鱼0碰过仿真水球标志
                    IsCollidedBallAndFish0AtStage2 = true;
                }
                else if (ExecutingTaskNo == 3)
                {// 设置在执行任务3时仿真机器鱼0碰过仿真水球标志
                    IsCollidedBallAndFish0AtStage3 = true;
                }
                else if (ExecutingTaskNo == 4)
                {// 设置在执行任务4时仿真机器鱼0碰过仿真水球标志
                    IsCollidedBallAndFish0AtStage4 = true;
                }
            }
            if (Teams[0].Fishes[1].Collision.Contains(CollisionType.FISH_BALL))
            {// 设置在执行任务1时仿真机器鱼1碰过仿真水球标志
                if (ExecutingTaskNo == 1)
                {// 设置在执行任务1时仿真机器鱼1碰过仿真水球标志
                    IsCollidedBallAndFish1AtStage1 = true;
                }
                else if (ExecutingTaskNo == 2)
                {// 设置在执行任务2时仿真机器鱼1碰过仿真水球标志
                    IsCollidedBallAndFish1AtStage2 = true;
                }
                else if (ExecutingTaskNo == 3)
                {// 设置在执行任务3时仿真机器鱼1碰过仿真水球标志
                    IsCollidedBallAndFish1AtStage3 = true;
                }
                else if (ExecutingTaskNo == 4)
                {// 设置在执行任务4时仿真机器鱼1碰过仿真水球标志
                    IsCollidedBallAndFish1AtStage4 = true;
                }
            }

            // LiYoubing 20110515 
            // 计分方法
            //if ((Env.Balls[0].PositionMm.X + Env.Balls[0].RadiusMm < Env.ObstaclesRect[0].PolygonVertices[0].X)
            //    && (Env.Balls[0].PrePositionMm.X + Env.Balls[0].RadiusMm >= Env.ObstaclesRect[0].PolygonVertices[0].X)
            //    && (Env.Balls[0].PositionMm.Z >= Env.ObstaclesRect[0].PolygonVertices[2].Z + Env.Balls[0].RadiusMm)
            //    && (Env.Balls[0].PositionMm.Z <= Env.ObstaclesRect[1].PolygonVertices[0].Z - Env.Balls[0].RadiusMm))
            //{// 判断仿真水球从右往左过孔 用当前周期和前一周期仿真水球实体上X坐标的最大值与上边障碍物左上角X坐标比较
            //    if ((IsCollidedBallAndFish0AtStage1 && IsCollidedBallAndFish1AtStage1) == true
            //        && Teams[0].Para.Score == 0)
            //    {// 完成协作目标1，得1分，要排除将球推到左边然后推回右边再推过孔重复得分
            //        Teams[0].Para.Score += 1;
            //        RemainingMsOnTask1Completed = CommonPara.RemainingCycles * CommonPara.MsPerCycle;
            //    }
            //    IsCollidedBallAndFish0AtStage1 = false;  // 清除在右半场执行任务1时仿真机器鱼0碰过仿真水球标志
            //    IsCollidedBallAndFish1AtStage1 = false;  // 清除在右半场执行任务1时仿真机器鱼1碰过仿真水球标志
            //    ExecutingTaskNo = 2;
            //}

            //// 从左往右过孔清除标志确保左右两边协作任务独立判断
            //// 能确保如下情况不算协作：一条鱼过孔把球推到孔左边给另一条鱼碰一下推回右边再推到左边
            //if ((Env.Balls[0].PositionMm.X - Env.Balls[0].RadiusMm > Env.ObstaclesRect[0].PolygonVertices[1].X)
            //    && (Env.Balls[0].PrePositionMm.X - Env.Balls[0].RadiusMm <= Env.ObstaclesRect[0].PolygonVertices[1].X)
            //    && (Env.Balls[0].PositionMm.Z >= Env.ObstaclesRect[0].PolygonVertices[2].Z + Env.Balls[0].RadiusMm)
            //    && (Env.Balls[0].PositionMm.Z <= Env.ObstaclesRect[1].PolygonVertices[0].Z - Env.Balls[0].RadiusMm))
            //{// 判断仿真水球从左往右过孔 用当前周期和前一周期仿真水球实体上X坐标的最小值与上边障碍物右上角X坐标比较
            //    IsCollidedBallAndFish0AtStage2 = false;     // 清除在左半场执行任务2时仿真机器鱼0碰过仿真水球标志
            //    IsCollidedBallAndFish1AtStage2 = false;     // 清除在左半场执行任务2时仿真机器鱼1碰过仿真水球标志
            //    ExecutingTaskNo = 1;
            //}

            //if ((Env.Balls[0].PositionMm.X + Env.Balls[0].RadiusMm < Env.FieldInfo.LeftMm + Env.FieldInfo.GoalDepthMm)
            //    && (Env.Balls[0].PrePositionMm.X + Env.Balls[0].RadiusMm > Env.FieldInfo.LeftMm + Env.FieldInfo.GoalDepthMm)
            //    && (Env.Balls[0].PositionMm.Z >= -Env.FieldInfo.GoalWidthMm / 2 + Env.Balls[0].RadiusMm)
            //    && (Env.Balls[0].PositionMm.Z <= Env.FieldInfo.GoalWidthMm / 2 - Env.Balls[0].RadiusMm))
            //{// 判断仿真水球进左边球门 用当前周期和前一周期仿真水球实体上X坐标的最大值与左球门右竖线X坐标比较
            //    if ((IsCollidedBallAndFish0AtStage2 && IsCollidedBallAndFish1AtStage2) == true
            //        && Teams[0].Para.Score < 2)
            //    {// 完成协作目标2，得1分，排除重复得分，最多只能得2分
            //        Teams[0].Para.Score += 1;
            //        RemainingMsOnTask2Completed = CommonPara.RemainingCycles * CommonPara.MsPerCycle;
            //    }
            //    IsCompleted = true;                 // 设置使命结束标志
            //}

            // added By ZhangBo 20111113
            // 修改后的计分逻辑
            if (Env.Balls[0].PositionMm.X + Env.Balls[0].RadiusMm < Env.ObstaclesRect[0].PolygonVertices[0].X)
            {// 判断仿真水球从右往左过孔 用当前周期和前一周期仿真水球实体上X坐标的最大值与上边障碍物1左上角X坐标比较
                if ((IsCollidedBallAndFish0AtStage1 && IsCollidedBallAndFish1AtStage1) == true )
                {// 完成协作目标1，得1分
                    stageScore[0] = 1;
                }
                ExecutingTaskNo = 2;
            }
            if (Env.Balls[0].PositionMm.X + Env.Balls[0].RadiusMm < Env.ObstaclesRect[1].PolygonVertices[0].X)
            {// 判断仿真水球从右往左过孔 用当前周期和前一周期仿真水球实体上X坐标的最大值与上边障碍物2左上角X坐标比较
                if ((IsCollidedBallAndFish0AtStage2 && IsCollidedBallAndFish1AtStage2) == true)
                {// 完成协作目标2，得1分
                    stageScore[1] = 1;
                }
                ExecutingTaskNo = 3;
            }
            if (Env.Balls[0].PositionMm.X + Env.Balls[0].RadiusMm < Env.ObstaclesRect[2].PolygonVertices[0].X)
            {// 判断仿真水球从右往左过孔 用当前周期和前一周期仿真水球实体上X坐标的最大值与上边障碍物3左上角X坐标比较
                if ((IsCollidedBallAndFish0AtStage3 && IsCollidedBallAndFish1AtStage3) == true)
                {// 完成协作目标3，得1分
                    stageScore[2] = 1;
                }
                ExecutingTaskNo = 4;
            }
            if (Env.Balls[0].PositionMm.X + Env.Balls[0].RadiusMm < Env.FieldInfo.LeftMm + Env.FieldInfo.GoalDepthMm)
            {// 判断仿真水球进左边球门 用当前周期和前一周期仿真水球实体上X坐标的最大值与左球门右竖线X坐标比较
                if ((IsCollidedBallAndFish0AtStage4 && IsCollidedBallAndFish1AtStage4) == true)
                {// 完成协作目标4，得1分
                    stageScore[3] = 1;
                }
                IsCompleted = true;                 // 设置使命结束标志
            }
            
            Teams[0].Para.Score = 0;
            for (int j = 0; j < 4; j++)
            {// 实时计算当前得分情况
                Teams[0].Para.Score += stageScore[j];
            }
            if (Teams[0].Para.Score == 1)
            {// 记录得到1分的时刻
                RemainingMsWhenScored1 = CommonPara.RemainingCycles * CommonPara.MsPerCycle;
            }
            else if (Teams[0].Para.Score == 2)
            {// 记录得到2分的时刻
                RemainingMsWhenScored2 = CommonPara.RemainingCycles * CommonPara.MsPerCycle;
            }
            else if (Teams[0].Para.Score == 3)
            {// 记录得到3分的时刻
                RemainingMsWhenScored3 = CommonPara.RemainingCycles * CommonPara.MsPerCycle;
            }
            else if (Teams[0].Para.Score == 4)
            {// 记录得到4分的时刻
                RemainingMsWhenScored4 = CommonPara.RemainingCycles * CommonPara.MsPerCycle;
            }
        }

        
        // modified by zhangbo20111113
        /// <summary>
        /// 处理比赛结束的判断和响应任务 包括仿真水球被推进左球门和时间耗完两种情况
        /// </summary>
        void GameOverHandler()
        {
            string strMsg = "";
            if (CommonPara.RemainingCycles <= 0)
            {// 时间耗完 比赛结束标志在仿真主循环里设置此处不管
                IsCompleted = true;                         // 设置使命结束标志
                strMsg += "Competition time is over!\n";    // 时间耗完提示
            }
            if (IsCompleted == true)
            {// 使命结束 弹出提示
                CommonPara.IsRunning = false;               // 置比赛结束标志使仿真循环中断
                int iRemainingMs = -1;
                //strMsg += string.Format("Completed task count and score: {0}.\n", Teams[0].Para.Score);
                //if (RemainingMsOnTask2Completed != -1)
                //{// 协作任务2完成了则记录该任务完成时剩余的时间
                //    iRemainingMs =RemainingMsWhenScored2;
                //    strMsg += string.Format("Remaining time when the 2th Cooperation Task is completed: ");
                //}
                //else if (RemainingMsOnTask1Completed != -1)
                //{// 否则协作任务1完成了则记录该任务完成时剩余的时间
                //    iRemainingMs = RemainingMsWhenScored1;
                //    strMsg += string.Format("Remaining time when the 1th Cooperation Task is completed: ");
                //}
                //else
                //{// 否则记录比赛结束时剩余的时间
                //    iRemainingMs = CommonPara.RemainingCycles * CommonPara.MsPerCycle;
                //    strMsg += string.Format("Remaining time: ");
                //}
                strMsg += string.Format("Completed task count and score: {0}.\n", Teams[0].Para.Score);
                if (Teams[0].Para.Score == 4)
                {// 得到4分，记录该任务完成时剩余的时间
                    iRemainingMs = RemainingMsWhenScored4;
                    strMsg += string.Format("Remaining time when scored 4: ");
                }
                else if (Teams[0].Para.Score == 3)
                {// 得到3分，记录该任务完成时剩余的时间
                    iRemainingMs = RemainingMsWhenScored3;
                    strMsg += string.Format("Remaining time when scored 3: ");
                }
                else if (Teams[0].Para.Score == 2)
                {// 得到2分，记录该任务完成时剩余的时间
                    iRemainingMs = RemainingMsWhenScored2;
                    strMsg += string.Format("Remaining time when scored 2: ");
                }
                else if (Teams[0].Para.Score == 1)
                {// 得到1分，记录该任务完成时剩余的时间
                    iRemainingMs = RemainingMsWhenScored1;
                    strMsg += string.Format("Remaining time when scored 1: ");
                }
                else
                {// 否则记录比赛结束时剩余的时间
                    iRemainingMs = CommonPara.RemainingCycles * CommonPara.MsPerCycle;
                    strMsg += string.Format("Remaining time: ");
                }
                int iRemainingSec = iRemainingMs / 1000;
                // 按"MM:SS:MsMsMs"格式显示剩余时间
                strMsg += string.Format("{0:00}:{1:00}:{2:000}.", iRemainingSec / 60, iRemainingSec % 60, iRemainingMs % 1000);
                MessageBox.Show(strMsg, "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion
        #endregion
    }
}
