//-----------------------------------------------------------------------
// Copyright (C), 2011, PKU&HNIU
// File Name: MatchSnooker.cs
// Date: 20110605  Author: HNIU  Version: 1
// Description: 非对抗性的相关机器鱼，仿真环境和仿真使命定义文件
// Histroy:
// Date: 20110621  Author: HNIU
// Modification: 
// 1.受RoboFish的碰撞状态标志改成标志列表的影响 标志值的比较改用List.Contains方法进行
// Date: 20110710  Author: LiYoubing
// Modification: 
// 1.全面整理代码
// Date: 20111103  Author: ZhangBo
// Modification: 
// 1.去掉4个红球
// 2.修改彩球得分逻辑
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
    /// 非对抗比赛水球斯诺克仿真使命特有的仿真机器鱼类
    /// </summary>
    [Serializable]
    public class FishSnooker : RoboFish
    {
        // 在这里定义水球斯诺克仿真使命特有的仿真机器鱼的特性和行为
    }

    /// <summary>
    /// 非对抗比赛水球斯诺克仿真使命特有的仿真环境类
    /// </summary>
    [Serializable]
    public class EnvironmentSnooker : SimEnvironment
    {
        // 在这里定义水球斯诺克仿真使命特有的仿真环境的特性和行为
    }
  
    /// <summary>
    /// 非对抗比赛水球斯诺克使命类
    /// </summary>
    [Serializable]
    public partial class MatchSnooker : Mission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static MatchSnooker instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static MatchSnooker Instance()
        {
            if (instance == null)
            {
                instance = new MatchSnooker();
            }
            return instance;
        }

        /// <summary>
        /// 私有构造函数 在Instance()中调用 进程生命周期内只会被调用一次
        /// </summary>
        private MatchSnooker() 
        {
            CommonPara.MsPerCycle = 100;
            CommonPara.TeamCount = 1;
            CommonPara.FishCntPerTeam = 1;
            CommonPara.Name = "水球斯诺克";
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
        public List<Team<FishSnooker>> Teams = new List<Team<FishSnooker>>();

        /// <summary>
        /// 仿真使命（比赛或实验项目）所使用的具体仿真环境
        /// </summary>
        public EnvironmentSnooker Env = new EnvironmentSnooker();  
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
                Teams.Add(new Team<FishSnooker>());

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
                    Teams[i].Fishes.Add(new FishSnooker());

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
            // 在整个比赛场地上面添加10个水球，其中包括6个红球和4个彩球
            for (int i = 0; i < 10; i++)
            {
                Env.Balls.Add(new Ball());
            }

            // 设置10个仿真水球默认颜色
            ResetColorBall();

            // 添加4个方形障碍物，其中有2个放置在场地上方，2个放置在场地下方，构成六个球洞
            Field f = Field.Instance();
            // 用于搭建球洞的4个方形障碍物X方向长度为场地X方向总长度减去2个球门块和3球洞X方向长度之和即5个球门深度
            int obsLengthXMm = (f.FieldLengthXMm - 3 * f.GoalDepthMm) / 2;
            xna.Vector3 positionMm = new xna.Vector3(0, 0, 0);
            Env.ObstaclesRect.Add(new RectangularObstacle("obs1", positionMm, Color.Black, Color.Green, obsLengthXMm, f.GoalDepthMm, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs2", positionMm, Color.Black, Color.Green, obsLengthXMm, f.GoalDepthMm, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs3", positionMm, Color.Black, Color.Green, obsLengthXMm, f.GoalDepthMm, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs4", positionMm, Color.Black, Color.Green, obsLengthXMm, f.GoalDepthMm, 0));

            // 设置4个方形障碍物的默认位置
            ResetObstacles();
        }

        /// <summary>
        /// 初始化当前仿真使命类特有的需要传递给策略的变量名和变量值，在当前使命类构造函数中调用
        /// 添加到仿真使命基类的哈希表Hashtable成员中
        /// </summary>
        private void InitHtMissionVariables()
        {
            // 初始化比赛阶段为CompetitionPeriod.NormalTime（0）
            HtMissionVariables.Add("BallsInFieldFlag", BallsInFieldFlag.ToString());
        }

        /// <summary>
        /// 更新当前使命类特有的需要传递给策略的变量名和变量值，在ProcessControlRules中调用
        /// </summary>
        private void UpdateHtMissionVariables()
        {
            // 更新14个仿真水球是否在场上标志量
            HtMissionVariables["BallsInFieldFlag"] = BallsInFieldFlag.ToString();
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
                // 设置比赛前机器鱼的初始的位姿信息（初始坐标，左半场的鱼体方向）
                myMission.TeamsRef[0].Fishes[0].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0, 0);
                // 左半场队伍的仿真机器鱼朝右
                myMission.TeamsRef[0].Fishes[0].BodyDirectionRad = 0;

                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    Teams[0].Fishes[j].CountDrawing = 0;        // 仿真机器鱼绘制次数计数
                    Teams[0].Fishes[j].InitPhase = j * 5;       // 尾部三个关节正弦摆动公式的初始调节相位值
                }
            }
        }

        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真水球恢复默认位置
        /// </summary>
        public override void ResetBalls()
        {
            // 将10个仿真水球放置在初始的位姿处
            // 首先设置前6个红色水球的初始的位姿信息
            Env.Balls[0].PositionMm = new xna.Vector3(Teams[0].Fishes[0].CollisionModelRadiusMm * 2, 0, 0);
            Env.Balls[1].PositionMm = new xna.Vector3(Teams[0].Fishes[0].CollisionModelRadiusMm * 2 
                + Env.Balls[0].RadiusMm * (float)Math.Sqrt(3), 0, Env.Balls[1].RadiusMm);
            Env.Balls[2].PositionMm = new xna.Vector3(Teams[0].Fishes[0].CollisionModelRadiusMm * 2 
                + Env.Balls[0].RadiusMm * (float)Math.Sqrt(3), 0, -Env.Balls[2].RadiusMm);
            Env.Balls[3].PositionMm = new xna.Vector3(Teams[0].Fishes[0].CollisionModelRadiusMm * 2 
                + Env.Balls[0].RadiusMm * 2 * (float)Math.Sqrt(3), 0, 2 * Env.Balls[3].RadiusMm);
            Env.Balls[4].PositionMm = new xna.Vector3(Teams[0].Fishes[0].CollisionModelRadiusMm * 2 
                + Env.Balls[0].RadiusMm * 2 * (float)Math.Sqrt(3), 0, 0);
            Env.Balls[5].PositionMm = new xna.Vector3(Teams[0].Fishes[0].CollisionModelRadiusMm * 2 
                + Env.Balls[0].RadiusMm * 2 * (float)Math.Sqrt(3), 0, -2 * Env.Balls[5].RadiusMm);
            //Env.Balls[6].PositionMm = new xna.Vector3(Teams[0].Fishes[0].CollisionModelRadiusMm * 2 
            //    + Env.Balls[0].RadiusMm * 3 * (float)Math.Sqrt(3), 0, -3 * Env.Balls[6].RadiusMm);
            //Env.Balls[7].PositionMm = new xna.Vector3(Teams[0].Fishes[0].CollisionModelRadiusMm * 2 
            //    + Env.Balls[0].RadiusMm * 3 * (float)Math.Sqrt(3), 0, -Env.Balls[7].RadiusMm);
            //Env.Balls[8].PositionMm = new xna.Vector3(Teams[0].Fishes[0].CollisionModelRadiusMm * 2 
            //    + Env.Balls[0].RadiusMm * 3 * (float)Math.Sqrt(3), 0, Env.Balls[8].RadiusMm);
            //Env.Balls[9].PositionMm = new xna.Vector3(Teams[0].Fishes[0].CollisionModelRadiusMm * 2 
            //    + Env.Balls[0].RadiusMm * 3 * (float)Math.Sqrt(3), 0, 3 * Env.Balls[9].RadiusMm);

            //设置四个彩球的初始的位姿信息
            Env.Balls[6].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 2, 0, 0);
            Env.Balls[7].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 4, 0, 0);
            Env.Balls[8].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0,
                -Teams[0].Fishes[0].CollisionModelRadiusMm * 2);
            Env.Balls[9].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0,
                Teams[0].Fishes[0].CollisionModelRadiusMm * 2);
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
                //第0支队伍的仿真机器鱼后端色标即编号颜色为黑色
                Teams[0].Fishes[j].ColorId = Color.Black;
            }
        }

        // LiYoubing 20110624
        /// <summary>
        /// 设置仿真水球填充和边框默认颜色
        /// </summary>
        public override void ResetColorBall()
        {
            for (int i = 0; i < 6; i++)
            {// 把第0-9号仿真水球设为红色
                Env.Balls[i].ColorBorder = Color.Red;
                Env.Balls[i].ColorFilled = Color.Black;
            }

            // 把第10-13号水球设为红色以外的颜色
            Env.Balls[6].ColorBorder = Color.Yellow;
            Env.Balls[6].ColorFilled = Color.Black;
            Env.Balls[7].ColorBorder = Color.Green;
            Env.Balls[7].ColorFilled = Color.Black;
            Env.Balls[8].ColorBorder = Color.Brown;
            Env.Balls[8].ColorFilled = Color.Black;
            Env.Balls[9].ColorBorder = Color.Pink;
            Env.Balls[9].ColorFilled = Color.Black;
        }

        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真障碍物恢复默认位置
        /// </summary>
        public override void ResetObstacles()
        {
            // 将4个仿真方形障碍物放到默认位置
            Field f = Field.Instance();
            // 用于搭建球洞的四个方形障碍物X方向长度为场地X方向总长度减去2个球门块和3球洞X方向长度之和即5个球门深度
            float obsLengthXMm = (float)(f.FieldLengthXMm - 3 * f.GoalDepthMm) / 2;
            // 左上障碍物位置
            Env.ObstaclesRect[0].PositionMm = new xna.Vector3(-(obsLengthXMm + f.GoalDepthMm) / 2, 0, f.TopMm + f.GoalDepthMm / 2);
            // 右上障碍物位置
            Env.ObstaclesRect[1].PositionMm = new xna.Vector3((obsLengthXMm + f.GoalDepthMm) / 2, 0, f.TopMm + f.GoalDepthMm / 2);
            // 左下障碍物位置
            Env.ObstaclesRect[2].PositionMm = new xna.Vector3(-(obsLengthXMm + f.GoalDepthMm) / 2, 0, f.BottomMm - f.GoalDepthMm / 2);
            // 右下障碍物位置
            Env.ObstaclesRect[3].PositionMm = new xna.Vector3((obsLengthXMm + f.GoalDepthMm) / 2, 0, f.BottomMm - f.GoalDepthMm / 2);
            // 提供从界面修改仿真场地尺寸及障碍物各项参数的功能后恢复默认就不仅仅是恢复默认位置了
            for (int i = 0; i < 4; i++)
            {// 所有仿真方形障碍物长度/宽度/边框颜色/填充颜色相同
                Env.ObstaclesRect[i].IsDeletionAllowed = false; // 不允许删除
                Env.ObstaclesRect[i].LengthMm = (int)obsLengthXMm;
                Env.ObstaclesRect[i].WidthMm = f.GoalDepthMm;
                Env.ObstaclesRect[i].ColorBorder = Color.Green;
                Env.ObstaclesRect[i].ColorFilled = Color.Green;
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
            ResetCoordinates();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命相应的仿真环境各参数设置为默认值  liufei
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
            BallsInFieldFlag = 0x03FF;  // 10个仿真水球都在场上低10位全为1
            RemainingCycles = 0;
            LastGoaledBallId = -1;
            RedBallsGoaledTopHolls = 0;
            RedBallsGoaledBottomHolls = 0;
            ColorBallsGoaledTopHolls = 0;
            ColorBallsGoaledBottomHolls = 0;
            HtMissionVariables["BallsInFieldFlag"] = BallsInFieldFlag.ToString();

            //UpTheHoll = 0;              // 添加于20110621
            //BelowTheHoll = 0;           // 添加于20110621
            //IsDisallowed = false;       // 添加于20110622
        }

        /// <summary>
        /// 改变场地时将与场地有关的坐标参数（用于判断仿真水球进球洞）重新计算
        /// </summary>
        //private void ResetCoordinates()
        //{
        //    Field f = Field.Instance();
        //    int r = Env.Balls[0].RadiusMm;                      // 默认58
        //    _TopHollZ = f.TopMm + f.GoalDepthMm - r;            // 默认-1408
        //    _BottomHollZ = f.BottomMm - f.GoalDepthMm + r;      // 默认1408
        //    _LeftHollXl = f.LeftMm + f.GoalDepthMm + r;         // 默认-2042
        //    _LeftHollXr = f.LeftMm + 2 * f.GoalDepthMm - r;     // 默认-2008
        //    _MiddleHollXl = -f.GoalDepthMm / 2 + r;             // 默认-17
        //    _MiddleHollXr = f.GoalDepthMm / 2 - r;              // 默认17
        //    _RightHollXl = f.RightMm - 2 * f.GoalDepthMm + r;   // 默认2008
        //    _RightHollXr = f.RightMm - f.GoalDepthMm - r;       // 默认2042
        //}

        // Modified By ZhangBo
        // Date:20111108
        private void ResetCoordinates()
        {
            Field f = Field.Instance();
            int r = Env.Balls[0].RadiusMm;                     
            _TopHollZ = f.TopMm + f.GoalDepthMm - r;           
            _BottomHollZ = f.BottomMm - f.GoalDepthMm + r;     
            _LeftHollXl = f.LeftMm + r;       
            _LeftHollXr = f.LeftMm + f.GoalDepthMm - r;    
            _MiddleHollXl = -f.GoalDepthMm / 2 + r;             
            _MiddleHollXr = f.GoalDepthMm / 2 - r;              
            _RightHollXl = f.RightMm -  f.GoalDepthMm + r;   
            _RightHollXr = f.RightMm - r;       
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
        /// 处理水球斯诺克具体仿真使命的控制规则
        /// </summary>
        public override void ProcessControlRules()
        {
            #region 老版本实现
            //// 计算比赛的相关得分
            //TheCalculateOfScore();
            
            //// 球进洞的相关处理
            //BallInHoleHandle();
            #endregion
            // 处理斯诺克规则
            SnookerHandler();

            // 处理倒计时递减到0比赛结束判断提示和响应任务以及任务提前完成的时候的相关处理
            GameOverHandler();

            // 更新当前使命类特有的需要传递给策略的变量名和变量值
            UpdateHtMissionVariables();
        }

        #region 水球斯诺克仿真使命控制规则所需私有变量
        /// <summary>
        /// 仿真水球刚好完全进入上方3个球洞时中心点的Z坐标值
        /// </summary>
        int _TopHollZ;
        /// <summary>
        /// 仿真水球刚好完全进入下方3个球洞时中心点的Z坐标值
        /// </summary>
        int _BottomHollZ;
        /// <summary>
        /// 仿真水球紧靠左上球洞左壁时中心点的X坐标值
        /// </summary>
        int _LeftHollXl;
        /// <summary>
        /// 仿真水球紧靠左上/下球洞右壁时中心点的X坐标值
        /// </summary>
        int _LeftHollXr;
        /// <summary>
        /// 仿真水球紧靠正上/下球洞左壁时中心点的X坐标值
        /// </summary>
        int _MiddleHollXl;
        /// <summary>
        /// 仿真水球紧靠正上/下球洞右壁时中心点的X坐标值
        /// </summary>
        int _MiddleHollXr;
        /// <summary>
        /// 仿真水球紧靠右上/下球洞左壁时中心点的X坐标值
        /// </summary>
        int _RightHollXl;
        /// <summary>
        /// 仿真水球紧靠右上/下球洞右壁时中心点的X坐标值
        /// </summary>
        int _RightHollXr;

        /// <summary>
        /// 10个仿真水球是否还在场上的标志量，该标志量用一个32位整数表示
        /// 低10位即0至9位分别表示编号为0至9的仿真水球是否还在场上，1表示在场上，0表示不在场上
        /// </summary>
        int BallsInFieldFlag;

        /// <summary>
        /// 最近一个有效进球发生时剩余的周期数
        /// </summary>
        int RemainingCycles = 0;

        /// <summary>
        /// 上一个有效进球的id（[0,9]）
        /// </summary>
        int LastGoaledBallId = -1;

        /// <summary>
        /// 进入场地上方球洞的红球数量
        /// </summary>
        int RedBallsGoaledTopHolls = 0;

        /// <summary>
        /// 进入场地下方球洞的红球数量
        /// </summary>
        int RedBallsGoaledBottomHolls = 0;

        /// <summary>
        /// 进入场地上方球洞的彩球数量
        /// </summary>
        int ColorBallsGoaledTopHolls = 0;

        /// <summary>
        /// 进入场地下方球洞的彩球数量
        /// </summary>
        int ColorBallsGoaledBottomHolls = 0;
        #endregion

        #region 水球斯诺克仿真使命控制规则具体处理过程
        /// <summary>
        /// 仿真水球是否在场地上方三个球洞的某一个里面
        /// </summary>
        /// <param name="ball">待判断的仿真水球对象</param>
        /// <returns>true表示仿真水球在场地上方三个球洞的某一个里面false表示不在</returns>
        private bool BallGoaledTopHoll(Ball ball)
        {
            return ball.PositionMm.Z <= _TopHollZ && 
                ((ball.PositionMm.X < _LeftHollXr && ball.PositionMm.X > _LeftHollXl) || 
                (Math.Abs(ball.PositionMm.X) <= _MiddleHollXr) || 
                (ball.PositionMm.X >= _RightHollXl && ball.PositionMm.X <= _RightHollXr));
        }

        /// <summary>
        /// 仿真水球是否在场地下方三个球洞的某一个里面
        /// </summary>
        /// <param name="ball">待判断的仿真水球对象</param>
        /// <returns>true表示仿真水球在场地下方三个球洞的某一个里面false表示不在</returns>
        private bool BallGoaledBottomHoll(Ball ball)
        {
            return ball.PositionMm.Z >= _BottomHollZ && 
                ((ball.PositionMm.X < _LeftHollXr && ball.PositionMm.X > _LeftHollXl) || 
                (Math.Abs(ball.PositionMm.X) <= _MiddleHollXr) || 
                (ball.PositionMm.X >= _RightHollXl && ball.PositionMm.X <= _RightHollXr));
        }

        /// <summary>
        /// 处理斯诺克规则
        /// </summary>
        private void SnookerHandler()
        {
            for (int i = 0; i < Env.Balls.Count; i++)
            {// 循环检测每个仿真水球
                // 判断第i个仿真水球是否在场地上方三个球洞的某一个里面 true是false否
                bool bIsGoaledTopHoll = BallGoaledTopHoll(Env.Balls[i]);
                // 如果第i个仿真水球不在上方球洞里面再判断是否在下方三个球洞的某一个里面 true是false否
                bool bIsGoaledBottomHoll = (bIsGoaledTopHoll == true) ? false : BallGoaledBottomHoll(Env.Balls[i]);

                if (bIsGoaledTopHoll || bIsGoaledBottomHoll)
                {// 第i个仿真水球在六个球洞的某一个里面
                    GoaledHollHandler(i, bIsGoaledTopHoll);
                    // 任一仿真周期不可能有多个仿真水球进洞因此只要检测到某个仿真水球进洞了即可结束循环
                    break;
                }
            }
        }

        /// <summary>
        /// 进球处理
        /// </summary>
        /// <param name="goaledBallId">当前进球的id（[0,9]）</param>
        /// <param name="bIsGoaledTopHoll">球进入的是否为场地上方球洞true是false否</param>
        private void GoaledHollHandler(int goaledBallId, bool bIsGoaledTopHoll)
        {
            Field f = Field.Instance();
            int r = Env.Balls[goaledBallId].RadiusMm;
            if (IsGoaledNotDisallowed(goaledBallId, ref LastGoaledBallId, ref BallsInFieldFlag))
            {// 进球有效
                RemainingCycles = CommonPara.RemainingCycles;   // 记录剩余周期数
                if (goaledBallId >= 0 && goaledBallId <= 5)
                {// 进的是红球
                    if (bIsGoaledTopHoll == true)
                    {// 进的是场地上方球洞
                        Env.Balls[goaledBallId].PositionMm = new xna.Vector3(
                            _LeftHollXr + (5 + 2 * RedBallsGoaledTopHolls) * r, 0, f.TopMm - (int)(1.5 * r));
                        RedBallsGoaledTopHolls++;       // 进入场地上方球洞的红球数增1
                    }
                    else
                    {// 进的是场地下方球洞
                        Env.Balls[goaledBallId].PositionMm = new xna.Vector3(
                            _LeftHollXr + (5 + 2 * RedBallsGoaledBottomHolls) * r, 0, f.BottomMm + (int)(1.5 * r));
                        RedBallsGoaledBottomHolls++;    // 进入场地下方球洞的红球数增1
                    }
                }
                else
                {// 进的是彩球
                    if ((BallsInFieldFlag & 0x003F) > 0)
                    {// 场上还有红球即标志量低6位（第0-5位）还有1
                        ResetColorBallPosition(goaledBallId);
                    }
                    else
                    {// 场上已无红球
                        if (bIsGoaledTopHoll == true)
                        {// 进的是场地上方球洞
                            Env.Balls[goaledBallId].PositionMm = new xna.Vector3(
                                _RightHollXl - (5 + 2 * ColorBallsGoaledTopHolls) * r, 0, f.TopMm - (int)(1.5 * r));
                            ColorBallsGoaledTopHolls++;     // 进入场地上方球洞的彩球数增1
                        }
                        else
                        {// 进的是场地下方球洞
                            Env.Balls[goaledBallId].PositionMm = new xna.Vector3(
                                _RightHollXl - (5 + 2 * ColorBallsGoaledBottomHolls) * r, 0, f.BottomMm + (int)(1.5 * r));
                            ColorBallsGoaledBottomHolls++;  // 进入场地下方球洞的彩球数增1
                        }
                    }
                }
            }
            else
            {// 无效进球
                if (goaledBallId >= 0 && goaledBallId <= 5)
                {// 进的是红球
                    MessageBox.Show("The ball is disallowed! ", "Confirming", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    if (bIsGoaledTopHoll == true)
                    {// 进的是场地上方球洞
                        Env.Balls[goaledBallId].PositionMm = new xna.Vector3(f.RightMm - r, 0, 0);
                    }
                    else
                    {// 进的是场地下方球洞
                        Env.Balls[goaledBallId].PositionMm = new xna.Vector3(f.LeftMm + r, 0, 0);
                    };
                }
                else
                {// 进的是彩球
                    ResetColorBallPosition(goaledBallId);
                }
            }
        }

        /// <summary>
        /// 判断进球是否有效
        /// </summary>
        /// <param name="goaledBallId">当前进球的id（[0,9]）</param>
        /// <param name="lastGoaledBallId">前一个进球的id（[0,9]），ref型，会被修改</param>
        /// <param name="ballsInFieldFlag">所有仿真水球是否在场上的标志量，ref型，可能被修改</param>
        /// <returns>进球有效返回true否则返回false</returns>
        private bool IsGoaledNotDisallowed(int goaledBallId, ref int lastGoaledBallId, ref int ballsInFieldFlag)
        {
            bool result = false;
            if (lastGoaledBallId == -1)
            {// 是第一次进球
                // 进的是红球（id在[0,5]之间）则有效 否则无效
                result = goaledBallId >= 0 && goaledBallId <= 5;
            }
            else
            {
                if ((ballsInFieldFlag & 0x003F) > 0)
                {// 场上还有红球即标志量低6位（第0-5位）还有1
                    // 判断当前进球id和上一个进球id是否分布在5.5不同侧
                    // 是则表示红球彩球交替，有效；否则，同为红球或同为彩球，无效
                    result = (goaledBallId - 5.5) * (lastGoaledBallId - 5.5) < 0;
                }
                else
                {// 场上已无红球
                    switch (ballsInFieldFlag)
                    {
                        case 0x03C0:   // id为6的彩球尚未入洞
                            result = (goaledBallId == 6);
                            break;
                        case 0x0380:   // id为7的彩球尚未入洞
                            result = (goaledBallId == 7);
                            break;
                        case 0x0300:   // id为8的彩球尚未入洞
                            result = (goaledBallId == 8);
                            break;
                        case 0x0200:   // id为9的彩球尚未入洞
                            result = (goaledBallId == 9);
                            break;
                    }
                }
            }
            lastGoaledBallId = goaledBallId;
            if (result == true)
            {// 进球有效
                if ((goaledBallId >= 0 && goaledBallId <= 5) ||
                    (ballsInFieldFlag & 0x003F) == 0)
                {// 有效进球为红球 或者 有效进球为彩球但场上已无红球
                    // 则将当前进球对应的标志位置0
                    // 以goaledBallId=3为例，将32位全1左移31-goalBallId=28位得到11110...0
                    // 再右移31位得到0...01，最后左移goalBallId=3位得到0...01000
                    int goaledBallFlag = (int)((0xFFFFFFFF << (31 - goaledBallId)) >> 31) << goaledBallId;
                    // 再按位取反得到1...10111
                    goaledBallFlag = ~goaledBallFlag;
                    // 最后修改ballsInFieldFlag，按位与使得所进球id对应的位变为0，其余位不变
                    ballsInFieldFlag &= goaledBallFlag;
                }

                // 计分处理
                if (goaledBallId >= 0 && goaledBallId <= 5)
                {// 有效进球为红球
                    Teams[0].Para.Score++;
                }
                else
                {// 有效进球为彩球
                    if ((ballsInFieldFlag & 0x003F) > 0)
                    {// 场上还有红球即标志量低6位（第0-5位）还有1
                        //Teams[0].Para.Score++;
                        Teams[0].Para.Score += (goaledBallId - 4);
                    }
                    else
                    {// 场上已无红球
                        Teams[0].Para.Score += (goaledBallId - 4);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 恢复彩球初始位置
        /// </summary>
        /// <param name="goaledBallId">待恢复到初始位置的彩球id（范围[6,9]）</param>
        private void ResetColorBallPosition(int goaledBallId)
        {
            switch (goaledBallId)
            {
                case 6:    // id为10的彩球恢复到初始位置
                    Env.Balls[goaledBallId].PositionMm = new xna.Vector3(
                        -Teams[0].Fishes[0].CollisionModelRadiusMm * 2, 0, 0);
                    break;
                case 7:    // id为11的彩球恢复到初始位置
                    Env.Balls[goaledBallId].PositionMm = new xna.Vector3(
                        -Teams[0].Fishes[0].CollisionModelRadiusMm * 4, 0, 0);
                    break;
                case 8:    // id为12的彩球恢复到初始位置
                    Env.Balls[goaledBallId].PositionMm = new xna.Vector3(
                        -Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0,
                        -Teams[0].Fishes[0].CollisionModelRadiusMm * 2);
                    break;
                case 9:    // id为13的彩球恢复到初始位置
                    Env.Balls[goaledBallId].PositionMm = new xna.Vector3(
                        -Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0,
                        Teams[0].Fishes[0].CollisionModelRadiusMm * 2);
                    break;
            }
        }

        // Added by liufei 20110614
        /// <summary>
        /// 处理倒计时递减到0比赛结束判断提示和响应任务
        /// </summary>
        private void GameOverHandler()
        {
            #region
            MyMission mymission = MyMission.Instance();
            string strMsg = "";

            // 使命结束标志 时间耗完或得分满33分
            bool bIsCompleted = false;  // 用ParasRef.IsRunning会导致Restart时也弹出对话框

            if (BallsInFieldFlag  == 0)
            {// 将所有的水球均顶进了球洞，得满分，标志量变为0
                mymission.ParasRef.IsRunning = false;           // 置比赛结束标志使仿真循环中断
                bIsCompleted = true;                            // 比赛提前完成，此时任务已经完成
                strMsg = "Competition task is completed!\n";    // 任务完成提示
            }
            if (mymission.ParasRef.RemainingCycles <= 0)
            {// 时间耗完 比赛结束标志在仿真主循环里设置此处不管
                bIsCompleted = true;
                strMsg = "Competition time is over!\n";        // 时间耗完提示
            }
            if (bIsCompleted == true)
            {// 使命结束 弹出提示 记录得分
                strMsg += string.Format("The score is : {0}.\n", Teams[0].Para.Score);
                if (Teams[0].Para.Score > 0)
                {// 完成至少1个进球则记录最后一个进球的时候所剩余的时间
                    int iRemainingMs = RemainingCycles * mymission.ParasRef.MsPerCycle;
                    int iRemainingSec = iRemainingMs / 1000;
                    // 按"MM:SS:MsMsMs"格式显示剩余时间
                    strMsg += string.Format("Remaining time when the {0}th Score is gotten: {1:00}:{2:00}:{3:00}.",
                        Teams[0].Para.Score, iRemainingSec / 60, iRemainingSec % 60, iRemainingMs % 1000);
                }
                MatchHelper.MissionMessageBox(ref MyMission.Instance().MissionRef, strMsg,
                    "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            #endregion
        }        
        #endregion

        #region 规则实现老版本 by Liufei
        ///// <summary>
        ///// 进入下面三个球洞的球的个数
        ///// </summary>
        //int BelowTheHoll = 0;
        ///// <summary>
        ///// 进入上面的三个球洞的球的个数
        ///// </summary>9
        //int UpTheHoll = 0;
        ///// <summary>
        ///// 进这16个有效球的时候（进每一个有效球时的剩余的周期数）
        ///// 在比赛时间用完而没有完成任务的情况下，显示最后一个有效进球的时候的剩余的周期数，用来记录每一个进球的时候的剩余的周期数
        ///// </summary>
        //int[] BallInHoleTime = new int[15];

        ///// <summary>
        ///// 添加一个标志量，用来确定红球进洞的相关处理add by liufei于20110622
        ///// </summary>
        //bool IsDisallowed = false;

        //// add by liufei20110610
        ///// <summary>
        ///// 处理15次有效进球的判定及计分
        ///// </summary>
        //private void TheCalculateOfScore()
        //{
        //    Field f = Field.Instance();

        //    for (int j = 0; j < Env.Balls.Count; j++)
        //    {
        //        if ((Env.Balls[j].PositionMm.Z <= -1408 &&
        //                         ((Env.Balls[j].PositionMm.X < -2008 && Env.Balls[j].PositionMm.X > -2042) ||
        //                         (Math.Abs(Env.Balls[j].PositionMm.X) <= 17) ||
        //                         (Env.Balls[j].PositionMm.X >= 2008 && Env.Balls[j].PositionMm.X <= 2042))) ||
        //                          (Env.Balls[j].PositionMm.Z >= 1408 &&
        //           ((Env.Balls[j].PositionMm.X < -2008 && Env.Balls[j].PositionMm.X > -2042) ||
        //           (Math.Abs(Env.Balls[j].PositionMm.X) <= 17) ||
        //           (Env.Balls[j].PositionMm.X >= 2008 && Env.Balls[j].PositionMm.X <= 2042))))
        //        {// 第j个仿真水球在6个球洞的某一个里面
        //            if (Teams[0].Para.Score == 0)
        //            {// 得第一分时的相关处理
        //                #region
        //                //此时说明有一个球进洞了
        //                if (Env.Balls[j].ColorFilled == Color.Red)
        //                {// 此时进球有效
        //                    Teams[0].Para.Score++;
        //                    BallInHoleTime[0] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 1)//进了第二个球
        //            {
        //                #region//此时说明有一个球进洞了
        //                if (Env.Balls[j].ColorFilled == Color.Red)
        //                {// 此时进球无效，对话框提示无效进球
        //                    System.Windows.Forms.MessageBox.Show("The Ball is disallow! ", "Confirming",
        //                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //                    IsDisallowed = true;
        //                }
        //                else
        //                {// 此时进球有效
        //                    Teams[0].Para.Score++;
        //                    BallInHoleTime[1] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 2)//进了第三个球
        //            {
        //                #region//第三个球应该进红球
        //                if (Env.Balls[j].ColorFilled == Color.Red)
        //                {// 此时进球有效
        //                    Teams[0].Para.Score++;
        //                    BallInHoleTime[2] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 3)//进了第四个球
        //            {
        //                #region//此时说明有一个球进洞了
        //                if (Env.Balls[j].ColorFilled == Color.Red)
        //                {// 此时进球无效，对话框提示无效进球
        //                    System.Windows.Forms.MessageBox.Show("The Ball is disallow! ", "Confirming",
        //                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //                    IsDisallowed = true;
        //                }
        //                else
        //                {// 此时进球有效
        //                    Teams[0].Para.Score++;
        //                    BallInHoleTime[3] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 4)//此时已经进了第5个球
        //            {
        //                #region//对第5个进球的相关处理
        //                if (Env.Balls[j].ColorFilled == Color.Red)
        //                {// 此时进球有效
        //                    Teams[0].Para.Score = 5;
        //                    BallInHoleTime[4] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 5)
        //            {
        //                #region//此时说明有一个球进洞了
        //                if (Env.Balls[j].ColorFilled == Color.Red)
        //                {// 此时进球无效，对话框提示无效进球
        //                    System.Windows.Forms.MessageBox.Show("The Ball is disallow! ", "Confirming",
        //                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //                    IsDisallowed = true;
        //                }
        //                else
        //                {// 此时进球有效
        //                    Teams[0].Para.Score = 6;
        //                    BallInHoleTime[5] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 6)
        //            {
        //                #region//对第7个进球的相关处理
        //                if (Env.Balls[j].ColorFilled == Color.Red)
        //                {// 此时进球有效
        //                    Teams[0].Para.Score = 7;
        //                    BallInHoleTime[6] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 7)
        //            {
        //                #region//此时说明有一个球进洞了
        //                if (Env.Balls[j].ColorFilled == Color.Red)
        //                {// 此时进球无效，对话框提示无效进球
        //                    System.Windows.Forms.MessageBox.Show("The Ball is disallow! ", "Confirming",
        //                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //                    IsDisallowed = true;
        //                }
        //                else
        //                {// 此时进球有效
        //                    Teams[0].Para.Score = 8;
        //                    BallInHoleTime[7] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 8)
        //            {
        //                #region//对第9个进球的相关处理
        //                if (Env.Balls[j].ColorFilled == Color.Red)
        //                {// 此时进球有效
        //                    Teams[0].Para.Score = 9;
        //                    BallInHoleTime[8] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 9)
        //            {
        //                #region//此时说明有一个球进洞了
        //                if (Env.Balls[j].ColorFilled == Color.Red)
        //                {// 此时进球无效，对话框提示无效进球
        //                    System.Windows.Forms.MessageBox.Show("The Ball is disallow! ", "Confirming",
        //                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //                    IsDisallowed = true;
        //                }
        //                else
        //                {// 此时进球有效
        //                    Teams[0].Para.Score = 10;
        //                    BallInHoleTime[9] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 10)
        //            {
        //                #region//对第11个进球的相关处理
        //                if (Env.Balls[j].ColorFilled == Color.Red)
        //                {// 此时进球有效
        //                    Teams[0].Para.Score = 11;
        //                    BallInHoleTime[10] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            //else if (Teams[0].Para.Score == 11)
        //            //{
        //            //    #region//此时说明有一个球进洞了
        //            //    if (Env.Balls[j].ColorFilled == Color.Red)
        //            //    {// 此时进球无效，对话框提示无效进球
        //            //        System.Windows.Forms.MessageBox.Show("The Ball is disallow! ", "Confirming",
        //            //            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //            //        IsDisallowed = true;
        //            //    }
        //            //    else
        //            //    {// 此时进球有效
        //            //        Teams[0].Para.Score = 12;
        //            //        BallInHoleTime[11] = MyMission.Instance().ParasRef.RemainingCycles;
        //            //        RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //            //    }
        //            //    #endregion
        //            //}
        //            //else if (Teams[0].Para.Score == 12)
        //            //{
        //            //    #region//对第13个进球的相关处理
        //            //    if (Env.Balls[j].ColorFilled == Color.Red)
        //            //    {// 此时进球有效
        //            //        Teams[0].Para.Score = 13;
        //            //        BallInHoleTime[12] = MyMission.Instance().ParasRef.RemainingCycles;
        //            //        RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //            //    }
        //            //    #endregion
        //            //}
        //            //else if (Teams[0].Para.Score == 13)
        //            //{
        //            //    #region//此时说明有一个球进洞了
        //            //    if (Env.Balls[j].ColorFilled == Color.Red)
        //            //    {// 此时进球无效，对话框提示无效进球
        //            //        System.Windows.Forms.MessageBox.Show("The Ball is disallow! ", "Confirming",
        //            //            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //            //        IsDisallowed = true;
        //            //    }
        //            //    else
        //            //    {// 此时进球有效
        //            //        Teams[0].Para.Score = 14;
        //            //        BallInHoleTime[13] = MyMission.Instance().ParasRef.RemainingCycles;
        //            //        RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //            //    }
        //            //    #endregion
        //            //}
        //            //else if (Teams[0].Para.Score == 14)
        //            //{
        //            //    #region//对第15个进球的相关处理
        //            //    if (Env.Balls[j].ColorFilled == Color.Red)
        //            //    {// 此时进球有效
        //            //        Teams[0].Para.Score = 15;
        //            //        BallInHoleTime[14] = MyMission.Instance().ParasRef.RemainingCycles;
        //            //        RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //            //    }
        //            //    #endregion
        //            //}
        //            //else if (Teams[0].Para.Score == 15)
        //            //{
        //            //    #region//此时说明有一个球进洞了
        //            //    if (Env.Balls[j].ColorFilled == Color.Red)
        //            //    {// 此时进球无效，对话框提示无效进球
        //            //        System.Windows.Forms.MessageBox.Show("The Ball is disallow! ", "Confirming",
        //            //            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //            //        IsDisallowed = true;
        //            //    }
        //            //    else
        //            //    {// 此时进球有效
        //            //        Teams[0].Para.Score = 16;
        //            //        BallInHoleTime[15] = MyMission.Instance().ParasRef.RemainingCycles;
        //            //        RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //            //    }
        //            //    #endregion
        //            //}
        //            //else if (Teams[0].Para.Score == 16)
        //            //{
        //            //    #region//对第17个进球的相关处理
        //            //    if (Env.Balls[j].ColorFilled == Color.Red)
        //            //    {// 此时进球有效
        //            //        Teams[0].Para.Score = 17;
        //            //        BallInHoleTime[16] = MyMission.Instance().ParasRef.RemainingCycles;
        //            //        RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //            //    }
        //            //    #endregion
        //            //}
        //            //else if (Teams[0].Para.Score == 17)
        //            //{
        //            //    #region//此时说明有一个球进洞了
        //            //    if (Env.Balls[j].ColorFilled == Color.Red)
        //            //    {// 此时进球无效，对话框提示无效进球
        //            //        System.Windows.Forms.MessageBox.Show("The Ball is disallow! ", "Confirming",
        //            //            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //            //        IsDisallowed = true;
        //            //    }
        //            //    else
        //            //    {// 此时进球有效
        //            //        Teams[0].Para.Score = 18;
        //            //        BallInHoleTime[17] = MyMission.Instance().ParasRef.RemainingCycles;
        //            //        RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //            //    }
        //            //    #endregion
        //            //}
        //            //else if (Teams[0].Para.Score == 18)
        //            //{
        //            //    #region//对第19个进球的相关处理
        //            //    if (Env.Balls[j].ColorFilled == Color.Red)
        //            //    {// 此时进球有效
        //            //        Teams[0].Para.Score = 19;
        //            //        BallInHoleTime[18] = MyMission.Instance().ParasRef.RemainingCycles;
        //            //        RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //            //    }
        //            //    #endregion
        //            //}
        //            else if (Teams[0].Para.Score == 11)
        //            {
        //                #region//此时场地的上面原来有四个彩球，进了一个
        //                if (Env.Balls[j].ColorFilled == Color.Yellow)
        //                {
        //                    Env.Balls[6].PositionMm = new xna.Vector3(1810, 0, -1558);
        //                    Teams[0].Para.Score = 13;
        //                    BallInHoleTime[11] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 12)
        //            {
        //                #region//此时场地的上面原来有三个彩球，进了一个
        //                if (Env.Balls[j].ColorFilled == Color.Green)
        //                {
        //                    Env.Balls[7].PositionMm = new xna.Vector3(1694, 0, -1558);
        //                    Teams[0].Para.Score = 16;
        //                    BallInHoleTime[12] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 13)
        //            {
        //                #region//此时场地的上面原来有两个彩球，进了一个彩球
        //                if (Env.Balls[j].ColorFilled == Color.Brown)
        //                {
        //                    Env.Balls[8].PositionMm = new xna.Vector3(1578, 0, -1558);
        //                    Teams[0].Para.Score = 20;
        //                    BallInHoleTime[13] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //            else if (Teams[0].Para.Score == 14)
        //            {
        //                #region//此时场地的上面没有水球了
        //                if (Env.Balls[j].ColorFilled == Color.Pink)
        //                {
        //                    Env.Balls[9].PositionMm = new xna.Vector3(1462, 0, -1558);//由于在处理进球里面没有将彩球进球
        //                    //进行具体的处理，因此在计分里面进行了简单的分配位置
        //                    Teams[0].Para.Score = 25;
        //                    BallInHoleTime[14] = MyMission.Instance().ParasRef.RemainingCycles;
        //                    RemainingCycles = MyMission.Instance().ParasRef.RemainingCycles;
        //                }
        //                #endregion
        //            }
        //        }
        //    }
        //}

        ///// <summary>
        ///// 水球进入球洞的相关的处理 add by liufei20110610
        ///// </summary>
        //private void BallInHoleHandle()
        //{
        //    #region//当红球进入下方球洞的时候的处理
        //    //int BelowTheHoll = 0;
        //    for (int i = 0; i < 6; i++)
        //    {
        //        //当球进入下面的三个球洞的时候
        //        if (Env.Balls[i].PositionMm.Z >= 1408 &&
        //            ((Env.Balls[i].PositionMm.X < -2008 && Env.Balls[i].PositionMm.X > -2042) ||
        //            (Math.Abs(Env.Balls[i].PositionMm.X) <= 17) ||
        //            (Env.Balls[i].PositionMm.X >= 2008 && Env.Balls[i].PositionMm.X <= 2042)))
        //        {
        //            //此时表示水球进入第一个球洞
        //            Color TheBallColor = Env.Balls[i].ColorFilled;//将第i个球的颜色赋给TheBallColor
        //            if (IsDisallowed == true)
        //            {
        //                Env.Balls[i].PositionMm = new xna.Vector3(-2192, 0, 0);
        //            }
        //            else
        //            {
        //                Env.Balls[i].PositionMm = new xna.Vector3(-1810 + 116 * BelowTheHoll, 0, 1558);//2042_1810
        //                BelowTheHoll++;
        //            }
        //        }
        //    }
        //    #endregion

        //    #region//当红球进入上面的三个球洞的时候的相关处理

        //    for (int i = 0; i < 6; i++)
        //    {
        //        //当球进入上面的三个球洞的时候
        //        if (Env.Balls[i].PositionMm.Z <= -1408 &&
        //            ((Env.Balls[i].PositionMm.X < -2008 && Env.Balls[i].PositionMm.X > -2042) ||
        //            (Math.Abs(Env.Balls[i].PositionMm.X) <= 17) ||
        //            (Env.Balls[i].PositionMm.X >= 2008 && Env.Balls[i].PositionMm.X <= 2042)))
        //        {
        //            //此时表示水球进入第一个球洞
        //            Color TheBallColor = Env.Balls[i].ColorFilled;//将第i个球的颜色赋给TheBallColor
        //            if (IsDisallowed == true)
        //            {
        //                Env.Balls[i].PositionMm = new xna.Vector3(2192, 0, 0);
        //            }
        //            else
        //            {
        //                Env.Balls[i].PositionMm = new xna.Vector3(-1810 + 116 * UpTheHoll, 0, -1558);
        //                UpTheHoll++;
        //            }
        //        }
        //    }
        //    #endregion

        //    #region//处理彩球的进球
        //    //首先，当场地上面还有红球的时候，对彩球的处理，进一个彩球之后，将相对应的彩球放置到彩球的初始
        //    //位姿处
        //    for (int i = 0; i < 10; i++)
        //    {
        //        if (Math.Abs(Env.Balls[i].PositionMm.X) < 2250 &&
        //            Math.Abs(Env.Balls[i].PositionMm.Z) < 1500)
        //        {
        //            Color TheBallColor = Env.Balls[i].ColorFilled;
        //            int number = i;
        //            if (number < 6)//此时说明有红球在场地上面,即此时红球和彩球均在场地上面
        //            {
        //                for (int j = 6; j < 10; j++)
        //                {
        //                    //进入上面的球洞的状态
        //                    if ((Env.Balls[j].PositionMm.Z <= -1408 &&
        //                       ((Env.Balls[j].PositionMm.X < -2008 && Env.Balls[j].PositionMm.X > -2042) ||
        //                       (Math.Abs(Env.Balls[j].PositionMm.X) <= 17) ||
        //                       (Env.Balls[j].PositionMm.X >= 2008 && Env.Balls[j].PositionMm.X <= 2042))) ||
        //                        (Env.Balls[j].PositionMm.Z >= 1408 &&
        //         ((Env.Balls[j].PositionMm.X < -2008 && Env.Balls[j].PositionMm.X > -2042) ||
        //         (Math.Abs(Env.Balls[j].PositionMm.X) <= 17) ||
        //         (Env.Balls[j].PositionMm.X >= 2008 && Env.Balls[j].PositionMm.X <= 2042))))
        //                    {
        //                        if (j == 6)
        //                        {
        //                            Env.Balls[j].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 2, 0, 0);

        //                        }
        //                        else if (j == 7)
        //                        {
        //                            Env.Balls[j].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 4, 0, 0);
        //                        }
        //                        else if (j == 8)
        //                        {
        //                            Env.Balls[j].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0,
        //     -Teams[0].Fishes[0].CollisionModelRadiusMm * 2);
        //                        }
        //                        else
        //                        {
        //                            Env.Balls[j].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0,
        //     Teams[0].Fishes[0].CollisionModelRadiusMm * 2);
        //                        }

        //                    }//彩球进入球洞的状态
        //                }

        //            }
        //            else//当红球不在场地上面的时候,即整个场地上面就仅仅剩下彩球
        //            {
        //                #region//当6号球在场地上面的时候
        //                if (i == 6)//此时表明第6个球在场地上面，此时机器鱼本来应该顶进10号球的
        //                {

        //                    for (int m = 7; m < 10; m++)
        //                        if ((Env.Balls[m].PositionMm.Z <= -1408 &&
        //                      ((Env.Balls[m].PositionMm.X < -2008 && Env.Balls[m].PositionMm.X > -2042) ||
        //                      (Math.Abs(Env.Balls[m].PositionMm.X) <= 17) ||
        //                      (Env.Balls[m].PositionMm.X >= 2008 && Env.Balls[m].PositionMm.X <= 2042))) ||
        //                       (Env.Balls[m].PositionMm.Z >= 1408 &&
        //        ((Env.Balls[m].PositionMm.X < -2008 && Env.Balls[m].PositionMm.X > -2042) ||
        //        (Math.Abs(Env.Balls[m].PositionMm.X) <= 17) ||
        //        (Env.Balls[m].PositionMm.X >= 2008 && Env.Balls[m].PositionMm.X <= 2042))))
        //                        {

        //                            if (m == 7)
        //                            {
        //                                Env.Balls[m].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 4, 0, 0);
        //                            }
        //                            else if (m == 8)
        //                            {
        //                                Env.Balls[m].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0,
        //         -Teams[0].Fishes[0].CollisionModelRadiusMm * 2);
        //                            }
        //                            else
        //                            {
        //                                Env.Balls[m].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0,
        //         Teams[0].Fishes[0].CollisionModelRadiusMm * 2);
        //                            }


        //                        }
        //                }
        //                #endregion
        //                #region//当7号球在场地上面的时候
        //                if (i == 7)//此时表明第7个球在场地上面，此时机器鱼本来应该顶进11号球的
        //                {
        //                    // Env.Balls[10].PositionMm = new xna.Vector3(2042,0,-1558);//将10号球放置在场地的右上方

        //                    for (int m = 8; m < 10; m++)
        //                        if ((Env.Balls[m].PositionMm.Z <= -1408 &&
        //                      ((Env.Balls[m].PositionMm.X < -2008 && Env.Balls[m].PositionMm.X > -2042) ||
        //                      (Math.Abs(Env.Balls[m].PositionMm.X) <= 17) ||
        //                      (Env.Balls[m].PositionMm.X >= 2008 && Env.Balls[m].PositionMm.X <= 2042))) ||
        //                       (Env.Balls[m].PositionMm.Z >= 1408 &&
        //        ((Env.Balls[m].PositionMm.X < -2008 && Env.Balls[m].PositionMm.X > -2042) ||
        //        (Math.Abs(Env.Balls[m].PositionMm.X) <= 17) ||
        //        (Env.Balls[m].PositionMm.X >= 2008 && Env.Balls[m].PositionMm.X <= 2042))))
        //                        {


        //                            if (m == 8)
        //                            {
        //                                Env.Balls[m].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0,
        //         -Teams[0].Fishes[0].CollisionModelRadiusMm * 2);
        //                            }
        //                            else
        //                            {
        //                                Env.Balls[m].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0,
        //         Teams[0].Fishes[0].CollisionModelRadiusMm * 2);
        //                            }
        //                        }
        //                }
        //                #endregion
        //                #region//当8号球在场地上面的时候
        //                if (i == 8)//此时表明第8个球在场地上面，此时机器鱼本来应该顶进12号球的
        //                {
        //                    //Env.Balls[10].PositionMm = new xna.Vector3(2042, 0, -1558);//令10号球位于一个固定的位姿
        //                    //Env.Balls[11].PositionMm = new xna.Vector3(1926, 0, -1558);//令11号球位于一个
        //                    for (int m = 9; m < 10; m++)
        //                        if ((Env.Balls[m].PositionMm.Z <= -1408 &&
        //                      ((Env.Balls[m].PositionMm.X < -2008 && Env.Balls[m].PositionMm.X > -2042) ||
        //                      (Math.Abs(Env.Balls[m].PositionMm.X) <= 17) ||
        //                      (Env.Balls[m].PositionMm.X >= 2008 && Env.Balls[m].PositionMm.X <= 2042))) ||
        //                       (Env.Balls[m].PositionMm.Z >= 1408 &&
        //        ((Env.Balls[m].PositionMm.X < -2008 && Env.Balls[m].PositionMm.X > -2042) ||
        //        (Math.Abs(Env.Balls[m].PositionMm.X) <= 17) ||
        //        (Env.Balls[m].PositionMm.X >= 2008 && Env.Balls[m].PositionMm.X <= 2042))))
        //                        {

        //                            if (m == 9)
        //                            {
        //                                Env.Balls[m].PositionMm = new xna.Vector3(-Teams[0].Fishes[0].CollisionModelRadiusMm * 6, 0,
        //         Teams[0].Fishes[0].CollisionModelRadiusMm * 2);
        //                            }


        //                        }
        //                }
        //                #endregion
        //            }
        //        }
        //    }
        //    #endregion
        //}

        //// Added by liufei 20110614 
        ///// <summary>
        ///// 处理倒计时递减到0比赛结束判断提示和响应任务
        ///// </summary>
        //private void GameOverHandler1()
        //{
        //    #region
        //    MyMission mymission = MyMission.Instance();
        //    string strMsg = "";

        //    // 使命结束标志 时间耗完或得分满25分
        //    bool bIsCompleted = false;  // 用ParasRef.IsRunning会导致Restart时也弹出对话框

        //    if (Teams[0].Para.Score == 25)
        //    {// 将所有的水球均顶进了球洞，得满分
        //        mymission.ParasRef.IsRunning = false;           // 置比赛结束标志使仿真循环中断
        //        bIsCompleted = true;                            // 比赛提前完成，此时任务已经完成
        //        strMsg = "Competition task is completed!\n";    // 任务完成提示
        //    }
        //    if (mymission.ParasRef.RemainingCycles <= 0)
        //    {// 时间耗完 比赛结束标志在仿真主循环里设置此处不管
        //        bIsCompleted = true;
        //        strMsg = "Competition time is over!\n";        // 时间耗完提示
        //    }
        //    if (bIsCompleted == true)
        //    {// 使命结束 弹出提示 记录得分
        //        strMsg += string.Format("The Score is : {0}.\n", Teams[0].Para.Score);
        //        if (Teams[0].Para.Score > 0)//刘飞更新于20110621晚上
        //        {// 完成至少1个进球则记录最后一个进球的时候所剩余的时间
        //            int iRemainingMs;
        //            if (Teams[0].Para.Score <= 15)
        //            {
        //                iRemainingMs = BallInHoleTime[Teams[0].Para.Score - 1] * mymission.ParasRef.MsPerCycle;
        //            }
        //            else if (Teams[0].Para.Score == 17)
        //            {
        //                iRemainingMs = BallInHoleTime[11] * mymission.ParasRef.MsPerCycle;
        //            }
        //            else if (Teams[0].Para.Score == 20)
        //            {
        //                iRemainingMs = BallInHoleTime[12] * mymission.ParasRef.MsPerCycle;
        //            }
        //            else if (Teams[0].Para.Score == 24)
        //            {
        //                iRemainingMs = BallInHoleTime[13] * mymission.ParasRef.MsPerCycle;
        //            }
        //            else
        //            {
        //                iRemainingMs = BallInHoleTime[14] * mymission.ParasRef.MsPerCycle;
        //            }
        //            int iRemainingSec = iRemainingMs / 1000;
        //            // 按"MM:SS:MsMsMs"格式显示剩余时间
        //            strMsg += string.Format("Remaining time when the {0}th Score is gotten: {1:00}:{2:00}:{3:00}.",
        //                Teams[0].Para.Score, iRemainingSec / 60, iRemainingSec % 60, iRemainingMs % 1000);
        //        }
        //        MessageBox.Show(strMsg, "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    }
        //    #endregion
        //}
        #endregion
        #endregion
    }
}