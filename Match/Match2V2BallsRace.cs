//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Match2V2BallsRace.cs
// Date: 20110303  Author: RenJing Version: 1
// Description: 两队争球对抗赛2VS2争球相关的仿真机器鱼，仿真环境和仿真使命定义文件
// Histroy:
// Date: 20110512 Author: LiYoubing
// Modification: 
// 1.障碍物围成的上下球门被拖动后重新选择该项目恢复初始位置
// 2.整理代码
// Date: 20110726 Author: LiYoubing
// Modification: 
// 1.进入制胜球阶段时修改比赛阶段标志
// 2.交换半场时交换构成上下球门的障碍物边框颜色
// ……
//
// Date: 20120408 Author: ChenXiao
// Modification: 
// 1.InitEnvironment（）添加2个动态障碍物
// 2.添加RestDyns（）函数，用来初始化动态障碍物
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
    /// 对抗比赛2V2争球仿真使命特有的仿真机器鱼类
    /// </summary>
    [Serializable]
    public class Fish2V2BallRace : RoboFish
    {
        // 在这里定义对抗比赛2V2争球仿真使命特有的仿真机器鱼的特性和行为
    }

    /// <summary>
    /// 对抗比赛2V2争球仿真使命特有的仿真环境类
    /// </summary>
    [Serializable]
    public class Environment2V2BallRace : SimEnvironment
    {
        // 在这里定义对抗比赛2V2争球仿真使命特有的仿真环境的特性和行为
    }

    /// <summary>
    /// 对抗比赛2V2争球仿真使命类
    /// </summary>
    [Serializable]
    public partial class Match2V2BallRace : Mission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static Match2V2BallRace instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Match2V2BallRace Instance()
        {
            if (instance == null)
            {
                instance = new Match2V2BallRace();
            }
            return instance;
        }

        /// <summary>
        /// 私有构造函数 在Instance()中调用 进程生命周期内只会被调用一次
        /// </summary>
        private Match2V2BallRace()
        {
            CommonPara.MsPerCycle = 100;
            CommonPara.TeamCount = 2;
            CommonPara.FishCntPerTeam = 2;
            CommonPara.Name = "2V2争球";
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
        public List<Team<Fish2V2BallRace>> Teams = new List<Team<Fish2V2BallRace>>();

        /// <summary>
        ///  仿真使命（比赛或实验项目）所使用的具体仿真环境
        /// </summary>
        public Environment2V2BallRace Env = new Environment2V2BallRace();
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
                Teams.Add(new Team<Fish2V2BallRace>());


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
                    Teams[i].Fishes.Add(new Fish2V2BallRace());

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
            for (int i = 0; i < 9; i++)
            {// 添加9个仿真水球
                Env.Balls.Add(new Ball());
            }

            int r = Env.Balls[0].RadiusMm;
            xna.Vector3 positionMm = new xna.Vector3(0, 0, 0);

            Env.ObstaclesRect.Add(new RectangularObstacle("obs1", positionMm, Color.Red, Color.Gray, r, 5 * r, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs2", positionMm, Color.Red, Color.Gray, r, 5 * r, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs3", positionMm, Color.Yellow, Color.Gray, r, 5 * r, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs4", positionMm, Color.Yellow, Color.Gray, r, 5 * r, 0));

            ////added by chenxiao
            //Env.DynamicRect.Add(new RectangularDynamic("dyn1", positionMm, Color.Yellow, Color.Red,r/2 ,6*r, r, 0, 80, -(float)Math.PI,0));
            //Env.DynamicRect.Add(new RectangularDynamic("dyn2", positionMm, Color.Yellow, Color.Red, r / 2, 6*r, r, 0, 80, (float)Math.PI, 0));


        }

        /// <summary>
        /// 初始化当前仿真使命类特有的需要传递给策略的变量名和变量值，在当前使命类构造函数中调用
        /// 添加到仿真使命基类的哈希表Hashtable成员中
        /// </summary>
        private void InitHtMissionVariables()
        {
            // 初始化当前已经完成的单程数量
            HtMissionVariables.Add("CompetitionPeriod", ((int)MatchHelper.CompetitionPeriod.NormalTime).ToString());
        }

        /// <summary>
        /// 更新当前使命类特有的需要传递给策略的变量名和变量值，在ProcessControlRules中调用
        /// </summary>
        private void UpdateHtMissionVariables()
        {
            // 更新当前已经完成的单程数量
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
                    }
                }

                // 左半场队伍的仿真机器鱼位置
                myMission.TeamsRef[0].Fishes[0].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[0].BodyLength * 3, 
                    0, Teams[0].Fishes[0].BodyWidth * 3);
                myMission.TeamsRef[0].Fishes[1].PositionMm = new xna.Vector3(
                    f.LeftMm + myMission.TeamsRef[0].Fishes[1].BodyLength * 3, 
                    0, -Teams[0].Fishes[1].BodyWidth * 3);

                // 右半场队伍的仿真机器鱼位置
                myMission.TeamsRef[1].Fishes[0].PositionMm = new xna.Vector3(
                    f.RightMm - myMission.TeamsRef[1].Fishes[0].BodyLength * 3, 
                    0, Teams[1].Fishes[0].BodyWidth * 3);
                myMission.TeamsRef[1].Fishes[1].PositionMm = new xna.Vector3(
                    f.RightMm - myMission.TeamsRef[1].Fishes[1].BodyLength * 3, 
                    0, -Teams[1].Fishes[1].BodyWidth * 3);

                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    // 左半场队伍的仿真机器鱼朝右
                    myMission.TeamsRef[0].Fishes[j].BodyDirectionRad = 0;
                    // 右半场队伍的仿真机器鱼朝左
                    myMission.TeamsRef[1].Fishes[j].BodyDirectionRad = xna.MathHelper.Pi;
                }
            }
        }

        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真水球恢复默认位置
        /// </summary>
        public override void ResetBalls()
        {
            // 将9个仿真水球恢复默认位置
            for (int i = 0; i < 7; i += 3)
            {// 仿真水球分3行陈列
                for (int j = 0; j < 3; j++)
                {// 仿真水球分3列陈列
                    Env.Balls[i + j].PositionMm = new xna.Vector3(40f * (i - 3), 0, 120f * (j - 1));
                }
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
            // 将4个仿真方形障碍物恢复默认位置
            Env.ObstaclesRect[0].PositionMm = new xna.Vector3(-4 * r - f.GoalDepthMm / 2, 0, f.TopMm + 5 * r / 2);
            Env.ObstaclesRect[1].PositionMm = new xna.Vector3(4 * r + f.GoalDepthMm / 2, 0, f.TopMm + 5 * r / 2);
            Env.ObstaclesRect[2].PositionMm = new xna.Vector3(-4 * r - f.GoalDepthMm / 2, 0, f.BottomMm - 5 * r / 2);
            Env.ObstaclesRect[3].PositionMm = new xna.Vector3(4 * r + f.GoalDepthMm / 2, 0, f.BottomMm - 5 * r / 2);
            //Env.ObstaclesRect[0].PositionMm = new xna.Vector3(-5 * r, 0, f.TopMm + 5 * r / 2);
            //Env.ObstaclesRect[1].PositionMm = new xna.Vector3(5 * r, 0, f.TopMm + 5 * r / 2);
            //Env.ObstaclesRect[2].PositionMm = new xna.Vector3(-5 * r, 0, f.BottomMm - 5 * r / 2);
            //Env.ObstaclesRect[3].PositionMm = new xna.Vector3(5 * r, 0, f.BottomMm - 5 * r / 2);
            // 提供从界面修改仿真场地尺寸及障碍物各项参数的功能后恢复默认就不仅仅是恢复默认位置了
            for (int i = 0; i < 4; i++)
            {// 所有仿真方形障碍物长度/宽度相同
                Env.ObstaclesRect[i].IsDeletionAllowed = false; // 不允许删除
                Env.ObstaclesRect[i].LengthMm = f.GoalDepthMm;
                //Env.ObstaclesRect[i].LengthMm = r;
                Env.ObstaclesRect[i].WidthMm = 5 * r;

                // LiYoubing 20110726
                if (i < 2)
                {// 上方球门边框颜色和左半场(TeamsRef[0])仿真机器鱼颜色一致
                    Env.ObstaclesRect[i].ColorBorder = TeamsRef[0].Fishes[0].ColorFish;
                }
                else
                {// 下方球门边框颜色和右半场(TeamsRef[1])仿真机器鱼颜色一致
                    Env.ObstaclesRect[i].ColorBorder = TeamsRef[1].Fishes[0].ColorFish;
                }
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
            CompetitionPeriod = (int)MatchHelper.CompetitionPeriod.NormalTime;
            HtMissionVariables["CompetitionPeriod"] = ((int)MatchHelper.CompetitionPeriod.NormalTime).ToString();
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

            // 处理半场交换任务
            HalfCourtExchangeHandler();

            // 更新当前使命类特有的需要传递给策略的变量名和变量值
            UpdateHtMissionVariables();
        }

        #region 对抗赛2VS2争球仿真使命控制规则所需私有变量
        // added by renjing 20110310
        /// <summary>
        /// 比赛阶段
        /// </summary>
        private int CompetitionPeriod;
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

            // 当前仿真周期上下球门里仿真水球数量
            int[] ballsCount = { 0, 0 };
            for (int i = 0; i < Env.Balls.Count; i++)
            {
                if (IsBallInSpecialGoal(Env.Balls[i], true) == true)
                {// 仿真水球在上方球门内
                    ballsCount[0]++;
                }
                else if (IsBallInSpecialGoal(Env.Balls[i], false) == true)
                {// 仿真水球在下方球门内
                    ballsCount[1]++;
                }
            }
            for (int i = 0; i < myMission.TeamsRef.Count; i++)
            {// 更新左/右半场队伍的得分
                myMission.TeamsRef[i].Para.Score = ballsCount[i];
            }
            if (CompetitionPeriod == (int)MatchHelper.CompetitionPeriod.ClutchShotTime)
            {// 制胜球阶段
                if (Teams[0].Para.Score != Teams[1].Para.Score)
                {// 双方得分不同，决出胜负
                    CommonPara.IsRunning = false;//比赛停止
                    int winner = (Teams[0].Para.Score > Teams[1].Para.Score) ? 0 : 1;
                    MessageBox.Show(string.Format("Congradulations!\nCompetition is completed.\nWinner:【{0}】 in 【{1}】\n", 
                        Teams[winner].Para.Name, Teams[winner].Para.MyHalfCourt), 
                        "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // Added by LiYoubing 20110518
        /// <summary>
        /// 判断仿真水球是否完全进入上/下障碍物围成的特殊球门区域
        /// </summary>
        /// <param name="ball">仿真水球</param>
        /// <param name="bIsLeftHalfCourt">待判断的是否上球门true判断上球门false判断下球门</param>
        /// <returns>仿真水球位于障碍物围成的特殊球门内返回true否则返回false</returns>
        private bool IsBallInSpecialGoal(Ball ball, bool bIsTopGoal)
        {
            int tmpIndex = (bIsTopGoal == true) ? 0 : 2;
            bool bIsZInGoal = (bIsTopGoal == true)
                ? (ball.PositionMm.Z + ball.RadiusMm 
                <= Env.ObstaclesRect[tmpIndex].PositionMm.Z + Env.ObstaclesRect[tmpIndex].WidthMm / 2)
                : (ball.PositionMm.Z - ball.RadiusMm 
                >= Env.ObstaclesRect[tmpIndex].PositionMm.Z - Env.ObstaclesRect[tmpIndex].WidthMm / 2);

            return bIsZInGoal 
                && (ball.PositionMm.X - ball.RadiusMm >= Env.ObstaclesRect[tmpIndex].PositionMm.X 
                + Env.ObstaclesRect[tmpIndex].LengthMm / 2)
                && (ball.PositionMm.X + ball.RadiusMm <= Env.ObstaclesRect[tmpIndex + 1].PositionMm.X 
                - Env.ObstaclesRect[tmpIndex + 1].LengthMm / 2);
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

            // 比赛时间耗完或才接着处理
            string strMsg = "";
            if (Teams[0].Para.Score != Teams[1].Para.Score)
            {//双方得分不同，决出胜负
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

                    CompetitionPeriod = (int)MatchHelper.CompetitionPeriod.ClutchShotTime;//开始制胜球
                    HandleClutchShotTime();
                }
                else
                {// 制胜球阶段结束
                    CommonPara.IsRunning = false;
                    strMsg = "Competition is tied.\n";
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
            ResetSomeLocomotionPara();
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
        /// 5.交换半场前Mission.TeamsRef[0]/[1]分别指向Match3V3.Teams[0]/[1]
        /// 6.交换半场后Mission.TeamsRef[0]/[1]分别指向Match3V3.Teams[1]/[0]
        /// 7.ResetTeamsAndFishes方法中对半场标志及各队伍仿真机器鱼初始位姿的设置
        ///   必须针对Mission.TeamsRef[0]/[1]而非Match3V3.Teams[0]/[1]
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

                // 重置仿真障碍物状态实现构成上下球门的障碍物边框颜色交换 LiYoubing 20110726
                ResetObstacles();
                ResetTeamsAndFishes();
                ResetSomeLocomotionPara();

                for (int i = 0; i < Env.Balls.Count; i++)
                {// 把所有仿真水球的位置沿X轴翻转
                    Env.Balls[i].PositionMm.Z = -Env.Balls[i].PositionMm.Z;
                }
            }
        }
        #endregion
        #endregion
        #endregion
    }
}