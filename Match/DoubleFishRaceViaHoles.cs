//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: DoubleFishRaceViaHoles.cs
// Date: 20101228  Author: renjing  Version: 1
// Description: 双鱼过孔竞速比赛项目相关的仿真机器鱼，仿真环境和仿真使命定义文件
// Histroy:
// Date: 20110514  Author: LiYoubing
// Modification: 
// 1.重整代码
// 2.修正犯规判断和处理
// 3.修正成绩显示
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Runtime.CompilerServices; // for MethodImpl
using System.Drawing;
using xna = Microsoft.Xna.Framework;
using URWPGSim2D.Common;

namespace URWPGSim2D.Match
{
    /// <summary>
    /// 双鱼过孔竞速仿真使命特有的仿真机器鱼类
    /// </summary>
    [Serializable]
    public class FishDoubleFishRacingViaHoles : RoboFish
    {
        // 在这里定义双鱼过孔竞速仿真使命特有的仿真机器鱼的特性和行为
    }

    /// <summary>
    /// 双鱼过孔竞速仿真使命特有的仿真环境类
    /// </summary>
    [Serializable]
    public class EnvironmentDoubleFishRacingViaHoles : SimEnvironment
    {
        // 在这里定义双鱼过孔竞速仿真使命特有的仿真环境的特性和行为
    }

    /// <summary>
    /// 双鱼过孔竞速仿真使命类
    /// </summary>
    [Serializable]
    public partial class DoubleFishRaceViaHoles : Mission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static DoubleFishRaceViaHoles instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static DoubleFishRaceViaHoles Instance()
        {
            if (instance == null)
            {
                instance = new DoubleFishRaceViaHoles();
            }
            return instance;
        }

        /// <summary>
        /// 私有构造函数 在Instance()中调用 进程生命周期内只会被调用一次
        /// </summary>
        private DoubleFishRaceViaHoles()
        {
            CommonPara.MsPerCycle = 100;
            CommonPara.TeamCount = 2;
            CommonPara.FishCntPerTeam = 1;
            CommonPara.Name = "双鱼过孔竞速";
            InitTeamsAndFishes();
            InitEnvironment();
            InitDecision();
        }
        #endregion

        #region public fields
        /// <summary>
        ///  仿真使命（比赛或实验项目）具体参与队伍列表及其具体仿真机器鱼
        /// </summary>
        public List<Team<FishDoubleFishRacingViaHoles>> Teams = new List<Team<FishDoubleFishRacingViaHoles>>();

        /// <summary>
        ///  仿真使命（比赛或实验项目）所使用的具体仿真环境
        /// </summary>
        public EnvironmentDoubleFishRacingViaHoles Env = new EnvironmentDoubleFishRacingViaHoles();
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
                Teams.Add(new Team<FishDoubleFishRacingViaHoles>());

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
                    Teams[i].Fishes.Add(new FishDoubleFishRacingViaHoles());

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

            // 初始化时可以置于任何位置 ResetEnvironment中会具体设置
            xna.Vector3 positionMm = new xna.Vector3(0, 0, 0);

            Field f = Field.Instance();
            int h = f.FieldLengthZMm - 6 * Teams[0].Fishes[0].BodyWidth;
            Env.ObstaclesRect.Add(new RectangularObstacle("obs1", positionMm, Color.Green, Color.Green, f.GoalDepthMm, h / 2, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs2", positionMm, Color.Green, Color.Green, f.GoalDepthMm, h, 0));
            Env.ObstaclesRect.Add(new RectangularObstacle("obs3", positionMm, Color.Green, Color.Green, f.GoalDepthMm, h / 2, 0));
        }

        /// <summary>
        /// 初始化当前仿真使命类特有的需要传递给策略的变量名和变量值，在当前使命类构造函数中调用
        /// 添加到仿真使命基类的哈希表Hashtable成员中
        /// </summary>
        private void InitHtMissionVariables()
        {
            // 初始化需要传给策略的变量“VariableName”默认值为“VariableDefaultValue”
            // 同时定义一个具体仿真使命类的私有成员变量，名称为“VariableName”
            //HtMissionVariables.Add("VarialbeName", "VariableDefaultValue");
        }

        /// <summary>
        /// 更新当前使命类特有的需要传递给策略的变量名和变量值，在ProcessControlRules中调用
        /// </summary>
        private void UpdateHtMissionVariables()
        {
            // 更新需要传给策略的变量“VariableName”其值为私有成员变量VariableName的值转换成字符串的结果
            //HtMissionVariables["IsCollidedBallAndFish0"] = VariableName.ToString();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值
        /// </summary>
        public override void ResetTeamsAndFishes()
        {
            Field f = Field.Instance();
            if (Teams != null)
            {
                Teams[0].Para.Name = "Team1";
                Teams[0].Fishes[0].PositionMm = new xna.Vector3(f.LeftMm + Teams[0].Fishes[0].BodyLength * 2, 0, 0);
                Teams[0].Fishes[0].BodyDirectionRad = (float)(-Math.PI/2);
                Teams[0].Fishes[0].VelocityDirectionRad = (float)(-Math.PI / 2);

                Teams[1].Para.Name = "Team2";
                Teams[1].Fishes[0].PositionMm = new xna.Vector3(f.RightMm - Teams[1].Fishes[0].BodyLength * 2, 0, 0);
                Teams[1].Fishes[0].BodyDirectionRad = (float)(Math.PI/2);
                Teams[1].Fishes[0].VelocityDirectionRad = (float)(Math.PI / 2);

                for (int j = 0; j < CommonPara.TeamCount; j++)
                {
                    Teams[j].Fishes[0].CountDrawing = 0;
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
            int h = f.FieldLengthZMm - 6 * Teams[0].Fishes[0].BodyWidth;
            // 将3个方形障碍物恢复默认位置
            Env.ObstaclesRect[0].PositionMm = new xna.Vector3(f.LeftMm + 3 * f.GoalDepthMm / 2 + f.ForbiddenZoneLengthXMm, 0, 0);
            Env.ObstaclesRect[1].PositionMm = new xna.Vector3(0, 0, 0);
            Env.ObstaclesRect[2].PositionMm = new xna.Vector3(f.RightMm - 3 * f.GoalDepthMm / 2 - f.ForbiddenZoneLengthXMm, 0, 0);
            // 提供从界面修改仿真场地尺寸及障碍物各项参数的功能后恢复默认就不仅仅是恢复默认位置了
            for (int i = 0; i < 3; i++)
            {// 所有仿真方形障碍物长度/宽度相同
                Env.ObstaclesRect[i].IsDeletionAllowed = false; // 不允许删除
                Env.ObstaclesRect[i].LengthMm = f.GoalDepthMm;
                Env.ObstaclesRect[i].WidthMm = h / 2;
            }
            Env.ObstaclesRect[1].WidthMm = h;
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

            // 将当前仿真使命涉及的全部仿真障碍物恢复默认位置
            ResetObstacles();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命特有的内部变量设置为默认值
        /// </summary>
        private void ResetInternalVariables()
        {
            for (int i = 0; i < 2; i++)
            {// 四个坐标半轴分别属于对应的顺时针相邻象限
                previousRadian[i] = (float)(Math.PI - i * Math.PI);     // π和0
                totalRadian[i] = 0;                                     // 0和0
                currentQuadrant[i] = 3 - 2 * i;                         // 3和1
                previousQuadrant[i] = currentQuadrant[i];               // 3和1
            }
            //HtMissionVariables["IsCollidedBallAndFish0"] = "VariableDefaultValue";
        }
        #endregion

        #region public methods that implement IMission interface
        /// <summary>
        /// 实现IMission中的接口用于 设置当前使命类型中各对象的初始值
        /// </summary>
        public override void SetMission()
        {
            // 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值
            ResetTeamsAndFishes();

            // 重启或改变仿真使命类型时将该仿真使命相应的仿真环境各参数设置为默认值
            ResetEnvironment();

            // 重启或改变仿真使命类型时将当前选中使命的动态对象（仿真机器鱼和仿真水球）的部分运动学设置为默认值
            ResetSomeLocomotionPara();

            // 重启或改变仿真使命类型时将当前选中使命的决策数组各元素设置为默认值
            ResetDecision();

            // 重启或改变仿真使命类型时将该仿真使命特有的内部变量设置为默认值
            ResetInternalVariables();
        }

        /// <summary>
        /// 处理双鱼过孔竞速具体仿真使命的控制规则
        /// </summary>
        public override void ProcessControlRules()
        {
            // 双鱼过孔竞速控制规则
            HandleSpecialRulesWhileChasingOnField();

            // 处理比赛结束的判断和响应任务
            GameOverHandler();

            // 更新当前使命类特有的需要传递给策略的变量名和变量值
            UpdateHtMissionVariables();
        }

        #region 双鱼过孔竞速仿真使命控制规则所需私有变量
        /// <summary>
        /// 记录fishA和fishB前一仿真周期所处的弧度
        /// </summary>
        private float[] previousRadian = { (float)Math.PI, 0 };

        /// <summary>
        /// 记录fishA和fishB当前仿真周期所处的弧度
        /// </summary>
        private float[] currentRadian = { (float)Math.PI, 0 }; 
   
        /// <summary>
        /// 记录fishA和fishB游过的总弧度
        /// </summary>
        private float[] totalRadian = { 0, 0 };

        /// <summary>
        /// 记录fishA和fishB当前仿真周期所处的象限
        /// </summary>
        private int[] currentQuadrant = { 3, 1 };

        /// <summary>
        /// 记录fishA和fishB前一仿真周期所处的象限
        /// </summary>
        private int[] previousQuadrant = { 3, 1 };
        #endregion

        #region 双鱼过孔竞速仿真使命控制规则具体处理过程
        // added by LiYoubing 2010-07-06, modified by renjing 2010-12-29
        /// <summary>
        /// 双鱼过孔竞速控制规则
        /// 根据鱼相对场地中心点游过的角度（用弧度单位）的累计来判断是否并排及并排时是哪条鱼追上了另一条鱼
        /// 角度计算坐标系以场地中心为原点，正右为X轴正半轴，正下为Y轴正半轴
        /// </summary>
        private void HandleSpecialRulesWhileChasingOnField()
        {
            for (int i = 0; i < 2; i++)
            {
                // 计算仿真机器鱼位置角[0,2π)及所在象限
                currentRadian[i] = CalcRadianAndQuadrant(Teams[i].Fishes[0], ref currentQuadrant[i]);
                
                // 犯规判断
                bool bFouledFlag = JudgeFouling(Teams[i].Fishes[0], currentQuadrant[i], previousQuadrant[i]);
                if (bFouledFlag == true)
                {// 犯规了
                    CommonPara.IsPaused = true;         // 中断仿真循环停止计时
                    CommonPara.IsPauseNeeded = true;    // 指示界面模拟点击“暂停”按钮
                    
                    // 犯规提示
                    MessageBox.Show(string.Format("【{0}】 Fouled!", Teams[i].Para.Name),
                        "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // 犯规处理
                    FishDoubleFishRacingViaHoles fish = Teams[i].Fishes[0];
                    HandleFouling(ref fish, previousQuadrant[i]);
                }
                else
                {// 没有犯规则累加成绩处理计数
                    DataProcessing(ref previousQuadrant[i], currentQuadrant[i], 
                        ref previousRadian[i], currentRadian[i], ref totalRadian[i]);
                }
            }
        }

        // LiYoubing 2010-07-14, modified by renjing 2010-12-29
        /// <summary>
        /// 双鱼过孔竞速计算仿真机器鱼位置角及所在象限
        /// 场地中心为原点，正右为正X正下为正Z轴，位置角从正X开始沿顺时针取[0,2π)
        /// </summary>
        /// <param name="fish">仿真机器鱼</param>
        /// <param name="currentQuadrant">仿真机器鱼前方矩形中心当前所在象限，ref型，被赋予新值</param>
        /// <returns>场地中心点指向仿真机器鱼中心点向量的方向（取[0,2π)）</returns>
        private float CalcRadianAndQuadrant(FishDoubleFishRacingViaHoles fish, ref int currentQuadrant)
        {
            // 先计算仿真机器鱼位置和原点连线与X轴的夹角（取[0,PI/2)）
            double curRadian;
            if (fish.PositionMm.X != 0)
            {// 不在Z轴上可通过反正切求夹角
                curRadian = Math.Atan2(Math.Abs(fish.PositionMm.Z), Math.Abs(fish.PositionMm.X));
            }
            else
            {// 在Z轴上
                if (fish.PositionMm.Z < 0)
                {// 在Z轴负半轴上
                    curRadian = 3 * Math.PI / 2;
                }
                else if (fish.PositionMm.Z > 0)
                {// 在Z轴正半轴上
                    curRadian = Math.PI / 2;
                }
                else
                {// 在原点
                    curRadian = 0;
                }
            }
            if (fish.PositionMm.X > 0 && fish.PositionMm.Z >= 0)
            {// 正X轴算第一象限
                currentQuadrant = 1;
            }
            else if (fish.PositionMm.X <= 0 && fish.PositionMm.Z > 0)
            {// 正Z轴算第二象限
                curRadian = Math.PI - curRadian;
                currentQuadrant = 2;
            }
            else if (fish.PositionMm.X < 0 && fish.PositionMm.Z <= 0)
            {// 负X轴算第三象限
                curRadian = Math.PI + curRadian;
                currentQuadrant = 3;
            }
            else if (fish.PositionMm.X >= 0 && fish.PositionMm.Z < 0)
            {// 负Z轴算第四象限
                curRadian = 2 * Math.PI - curRadian;
                currentQuadrant = 4;
            }
            return (float)curRadian;
        }

        // LiYoubing 2010-07-14, modified by renjing 2010-12-29
        /// <summary>
        /// 双鱼过孔竞速犯规判断
        /// </summary>
        /// <param name="fish">目标仿真机器鱼</param>
        /// <param name="curQuadrant">当前仿真周期所在象限</param>
        /// <param name="preQuadrant">前一仿真周期所在象限</param>
        /// <returns>true犯规；false未犯规</returns>
        private bool JudgeFouling(FishDoubleFishRacingViaHoles fish, int curQuadrant, int preQuadrant)
        {
            if (fish.PositionMm.X == 0 || fish.PositionMm.Z == 0)
            {// 当前位置在坐标轴上
                return false;
            }
            else if ((Math.Abs(curQuadrant - preQuadrant) == 2) || 
                (curQuadrant - preQuadrant == -1) || (curQuadrant - preQuadrant == 3))
            {// 前后仿真周期分别处于一三或二四象限/鱼逆时针游过一个象限或顺时针游过三个象限
                return true;
            }
            if (fish.PositionMm.Z * fish.PrePositionMm.Z <= 0)
            {
                if ((fish.PositionMm.X >= Env.ObstaclesRect[0].PolygonVertices[1].X)
                    && (fish.PositionMm.X <= Env.ObstaclesRect[2].PolygonVertices[0].X))
                {// 位于最左边障碍物和最右边障碍物之间 方形障碍物左上/右上/右下/左下四个顶点序号依次为0/1/2/3
                    return true;
                }
                else if (Math.Abs(fish.PositionMm.X - fish.PrePositionMm.X) > Env.FieldInfo.FieldLengthXMm / 2)
                {
                    return true;
                }
            }
            return false;
        }

        // LiYoubing 2010-07-13, modified by renjing 2010-12-29
        /// <summary>
        /// 双鱼过孔竞速犯规处理
        /// </summary>
        /// <param name="fish">目标仿真机器鱼</param>
        /// <param name="preQuadrant">目标仿真机器鱼前一仿真周期所处象限</param>
        private void HandleFouling(ref FishDoubleFishRacingViaHoles fish, int preQuadrant)
        {
            if (preQuadrant == 1)
            {// 将鱼移到右侧障碍物右边且鱼体方向置为PI/2
                fish.PositionMm.X = Env.ObstaclesRect[2].PositionMm.X + Env.ObstaclesRect[2].LengthMm / 2 + 50;
                fish.PositionMm.Z = Env.ObstaclesRect[2].PositionMm.Z;
                fish.BodyDirectionRad = (float)Math.PI / 2;
                fish.VelocityDirectionRad = (float)Math.PI / 2;
            }
            else if (preQuadrant == 2)
            {// 将鱼移到底部障碍物下边且鱼体方向置为PI
                fish.PositionMm.X = Env.ObstaclesRect[1].PositionMm.X;
                fish.PositionMm.Z = Env.ObstaclesRect[1].PositionMm.Z + Env.ObstaclesRect[1].WidthMm / 2 + 50;
                fish.BodyDirectionRad = (float)Math.PI;
                fish.VelocityDirectionRad = (float)Math.PI;
            }
            else if (preQuadrant == 3)
            {// 将鱼移到左侧障碍物左边且鱼体方向置为3PI/2
                fish.PositionMm.X = Env.ObstaclesRect[0].PositionMm.X - Env.ObstaclesRect[0].LengthMm / 2 - 50;
                fish.PositionMm.Z = Env.ObstaclesRect[0].PositionMm.Z;
                fish.BodyDirectionRad = 3f * (float)Math.PI / 2;
                fish.VelocityDirectionRad = 3f * (float)Math.PI / 2;
            }
            else if (preQuadrant == 4)
            {// 将鱼移到顶部障碍物上边且鱼体方向置为0
                fish.PositionMm.X = Env.ObstaclesRect[1].PositionMm.X;
                fish.PositionMm.Z = Env.ObstaclesRect[1].PositionMm.Z - Env.ObstaclesRect[1].WidthMm / 2 - 50;
                fish.BodyDirectionRad = 0;
                fish.VelocityDirectionRad = 0;
            }
        }

        // LiYoubing 2010-07-14, modified by renjing 2010-12-29
        /// <summary>
        /// 双鱼过孔竞速每个仿真周期计算结束时的数据处理
        /// </summary>
        /// <param name="preQuadrant">前一仿真周期所在象限，ref型，可被赋新值</param>
        /// <param name="curQuadrant">当前仿真周期所在象限</param>
        /// <param name="preRandian">前一仿真周期所在弧度，ref型，可被赋新值</param>
        /// <param name="curRadian">当前仿真周期所在弧度</param>
        /// <param name="totalRadian">累计游过的弧度，ref型，可被赋新值</param>
        private void DataProcessing(ref int preQuadrant, int curQuadrant, 
            ref float preRandian, float curRadian,  ref float totalRadian)
        {
            if (preQuadrant == 4 && curQuadrant != 4)
            {// 前一仿真周期在第四象限当前不在第四象限
                // 当前弧度加2PI减前一仿真周期弧度方为差值累加到游过的总弧度
                totalRadian += curRadian + 2 * (float)Math.PI - preRandian;
            }
            else
            {// 当前弧度与前一仿真周期差值累加到游过的总弧度
                totalRadian += curRadian - preRandian;
            }
           
            // 保留数据
            preRandian = curRadian;     // 保留所处弧度
            preQuadrant = curQuadrant;  // 保留所处象限
        }

        /// <summary>
        /// 处理比赛结束的判断和响应任务 包括某条仿真机器鱼追上另一条仿真机器鱼和时间耗完两种情况
        /// </summary>
        void GameOverHandler()
        {
            if (Math.Abs(currentRadian[0] - currentRadian[1]) < 0.035 || CommonPara.RemainingCycles <= 0)
            {// fishA和fishB与场地中心连线与X轴正半轴夹角（取[0,2pi)）相差2°即弧度0.035以内则认为双鱼并排
                CommonPara.IsRunning = false;
                
                string strMsg = "Congratulations! Task Completed!\n";
                if (totalRadian[0] > totalRadian[1])
                {
                    strMsg += string.Format("Winner: 【{0}】.\n", Teams[0].Para.Name);
                }
                else if (totalRadian[0] < totalRadian[1])
                {
                    strMsg += string.Format("Winner: 【{0}】.\n", Teams[1].Para.Name);
                }
                else
                {
                    strMsg += "Competition Tied!\n";
                }
                strMsg += string.Format("Total Radian 【{0}】 traveled: {1:0.00}.\nTotal Radian 【{2}】 traveled: {3:0.00}.\n", 
                    Teams[0].Para.Name, totalRadian[0], Teams[1].Para.Name, totalRadian[1]);
                MessageBox.Show(strMsg, "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
           }
        }
        #endregion
        #endregion
    }
}