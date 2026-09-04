//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Mission.cs
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真使命（比赛或实验项目）基类定义文件
// Histroy:
// Date: 20110512  Author: LiYoubing
// Modification: 
// 1.ProcessCollision中先清除所有队伍所有仿真机器鱼的碰撞状态标志列表
// Date: 20110712  Author: LiYoubing
// Modification: 
// 1.IsExchangedHalfCourt需要传递给客户端用于处理半场交换时teamId的交换
// Date: 20111101  Author: ZhangBo
// 1.MissionCommonPara中增加两个布尔参数IsGoalBlockNeeded、IsFieldInnerLinesNeeded，根据具体项目判断是需要否绘制球门块，禁区线等。
// Date: 20120408  Author: ChenXiao
// 1.Draw（）中添加动态障碍物的绘制
// 2.添加ResetDyns（）方法
// ……
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices; // for MethodImpl
using System.Drawing;
using System.Reflection;
using xna = Microsoft.Xna.Framework;

using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;

using URWPGSim2D.Core;

namespace URWPGSim2D.Common
{
    /// <summary>
    /// 仿真使命（比赛或实验项目）基类，所有具体使命（比赛或实验项目）使用的使命类必须继承该类
    /// </summary>
    [DataContract]
    [Serializable]
    public class Mission : IMission, ICloneable, IDssSerializable
    {
        static int count = 400;//计数器
        /// <summary>
        /// 构造函数
        /// </summary>
        public Mission()
        {
            SetDefaultMissionCommonPara();
        }
        RoboFish[] PreRoboFish = new RoboFish[20];//新建一个鱼体状态，用于碰撞检测二分法

        #region public fields 仿真使命公共对象
        /// <summary>
        /// 仿真使命（比赛或实验项目）公共参数
        /// </summary>
        [DataMember]
        public MissionCommonPara CommonPara = new MissionCommonPara();

        /// <summary>
        /// 仿真使命（比赛或实验项目）通用参与队伍列表及其通用仿真机器鱼
        /// </summary>
        [DataMember]
        public List<Team<RoboFish>> TeamsRef = new List<Team<RoboFish>>();

        /// <summary>
        /// 仿真使命（比赛或实验项目）所使用的通用仿真环境
        /// </summary>
        [DataMember]
        public SimEnvironment EnvRef = new SimEnvironment();

        /// <summary>
        /// 仿真使命（比赛或实验项目）各支队伍各条仿真机器鱼的决策数组 
        /// DecisionRef[i, j]为第i队第j条鱼的决策数据
        /// </summary>
        public Decision[,] DecisionRef;
        #endregion

        #region public fields 具体仿真使命需要向策略传递的参数表
        // 增加一个Hashtable用于保存具体仿真使命需要给策略传递的参数值 LiYoubing 20110507
        /// <summary>
        /// 具体仿真使命（比赛或实验项目）类即<see cref="URWPGSim2D.Common.Mission">Mission</see>类的子类需要给策略传递的变量名和值对
        /// 使用哈希表<see cref="System.Collections.Hashtable">Hashtable</see>存储需要传递的键值对
        /// 键（变量名）和值（变量值）均用string类型表示
        /// </summary>
        public Hashtable HtMissionVariables = new Hashtable();
        #endregion

        #region private and protected methods
        /// <summary>
        /// 设置默认使命参数，置为1V1比赛所需值
        /// </summary>
        private void SetDefaultMissionCommonPara()
        {
            CommonPara.Name = "1 Versus 1";
            CommonPara.TeamCount = 2;
            CommonPara.FishCntPerTeam = 1;
            CommonPara.TotalSeconds = 10 * 60;
            CommonPara.MsPerCycle = 100;
            CommonPara.RemainingCycles = CommonPara.TotalSeconds * 1000 / CommonPara.MsPerCycle;
            CommonPara.IsRunning = false;
            CommonPara.IsPaused = false;
            CommonPara.DisplayingCycles = 0;
            //CommonPara.ReadyTeamCount = 0;
        }

        /// <summary>
        /// 初始化决策数组空间，由各具体使命类构造函数调用
        /// 该方法要在调用SetMissionCommonPara设置好比赛类型公共参数（如每队队员数量）之后调用
        /// </summary>
        /// <returns></returns>
        protected void InitDecision()
        {
            DecisionRef = new Decision[CommonPara.TeamCount, CommonPara.FishCntPerTeam];
        }

        /// <summary>
        /// 将决策数组各元素重置为默认值，由各具体使命类的SetMission方法调用
        /// 供使命类型改变时重新初始化当前选中的使命用
        /// </summary>
        protected void ResetDecision()
        {
            if (DecisionRef == null) return;
            for (int i = 0; i < CommonPara.TeamCount; i++)
            {
                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    DecisionRef[i, j].VCode = 0; // 速度档位值默认置为0表示静止
                    DecisionRef[i, j].TCode = 7; // 转弯档位值默认置为7表示直游
                }
            }
        }

        /// <summary>
        /// 对调所处半场，即将位置坐标和鱼体方向取反
        /// 用于仿真机器鱼所属队伍位于右半场时绘制/碰撞检测/向客户端或策略传递参数前的处理
        /// </summary>
        public void ReversePose()
        {
            for (int i = 0; i < TeamsRef.Count; i++)
            {
                if (TeamsRef[i].Para.MyHalfCourt == HalfCourt.RIGHT)
                {
                    for (int j = 0; j < TeamsRef[i].Fishes.Count; j++)
                    {
                        TeamsRef[i].Fishes[j].ReversePose();
                    }
                }
            }
        }

        /// <summary>
        /// 恢复所处半场
        /// 用于仿真机器鱼所属队伍位于右半场时绘制/碰撞检测/向客户端或策略传递参数后的处理
        /// </summary>
        public void RecoveryPose()
        {
            for (int i = 0; i < TeamsRef.Count; i++)
            {
                if (TeamsRef[i].Para.MyHalfCourt == HalfCourt.RIGHT)
                {
                    for (int j = 0; j < TeamsRef[i].Fishes.Count; j++)
                    {
                        TeamsRef[i].Fishes[j].RecoveryPose();
                    }
                }
            }
        }

        /// <summary>
        /// 重启或改变仿真使命类型时重置当前选中使命的动态对象（仿真机器鱼和仿真水球）的部分运动学参数
        /// 主要是将速度值/速度方向值/角速度值/直游档位值/转弯档位值等重置为默认值
        /// 该方法由各具体使命类的SetMission接口实现方法调用
        /// </summary>
        protected void ResetSomeLocomotionPara()
        {
            for (int i = 0; i < TeamsRef.Count; i++)
            {
                for (int j = 0; j < TeamsRef[i].Fishes.Count; j++)
                {
                    // 重置仿真机器鱼的部分运动学参数
                    TeamsRef[i].Fishes[j].ResetSomeLocomotionPara();
                }
            }
            for (int i = 0; i < EnvRef.Balls.Count; i++)
            {
                // 重置仿真水球的部分运动学参数
                EnvRef.Balls[i].ResetSomeLocomotionPara();
            }
        }
        #endregion

        #region public methods that implement IMission interface
        #region 更改仿真使命类型时需要调用的接口方法
        /// <summary>
        /// 实现IMission中的接口用于 获取当前使命的公共参数
        /// </summary>
        /// <returns>当前使命类型的公共参数值</returns>
        public MissionCommonPara GetMissionCommonPara()
        {
            return CommonPara;
        }

        /// <summary>
        /// 实现IMission中的接口用于 设置当前使命的公共参数
        /// </summary>
        /// <param name="para">当前使命类型的公共参数值</param>
        public void SetMissionCommonPara(MissionCommonPara para)
        {            
            //CommonPara = para;
            // 对象拷贝而不能直接赋值那样赋的只是对象的引用
            para.CopyTo(CommonPara);
        }

        /// <summary>
        /// 设置当前使命各相关对象的初始值，在每个具体使命类中实现
        /// </summary>
        public virtual void SetMission() { }
        #endregion

        #region 获取具体仿真使命的公共对象引用的接口方法
        /// <summary>
        /// 实现IMission中的接口用于 获取当前使命参与队伍列表及每支队伍的通用仿真机器鱼对象
        /// 每支队伍的公共参数为新建的副本，不是原始队伍列表中相应对象的引用
        /// </summary>
        /// <returns>仿真使命参与队伍列表，每支队伍里有相应的通用仿真机器鱼对象引用</returns>
        public List<Team<RoboFish>> GetTeams()
        {
            return TeamsRef;
        }

        /// <summary>
        /// 实现IMission中的接口用于 获取当前使命的公共仿真环境
        /// </summary>
        /// <returns>仿真环境基类对象引用</returns>
        public SimEnvironment GetEnvironment()
        {
            return EnvRef;
        }

        /// <summary>
        /// 实现IMission中的接口用于 获取当前使命类型的决策数组
        /// </summary>
        /// <returns>当前使命类型各支队伍各个队员的决策数组，d[i, j]为第i队第j条仿真机器鱼的决策数据</returns>
        public Decision[,] GetDecision()
        {
            return DecisionRef;
        }
        #endregion

        #region 每个仿真周期均需调用的接口方法
        /// <summary>
        /// 实现IMission中的接口用于 根据当前决策数组更新全部仿真机器鱼的决策数据
        /// </summary>
        public void SetDecisionsToFishes()
        {
            //修改花样游泳项目，随机获取1号鱼速度和角速度；AnYongyue 20120727
            Random random = new Random();
            count--;
            for (int i = 0; i < CommonPara.TeamCount; i++)
            {
                float edgex = TeamsRef[i].Fishes[0].PositionMm.X;
                float edgez = TeamsRef[i].Fishes[0].PositionMm.Z;
                Field f = Field.Instance();
                if (CommonPara.Name == "花样游泳")
                {
                    if (edgex >= f.LeftMm + 400 && edgex <= f.RightMm - 400 && edgez >= f.TopMm + 400 && edgez <= f.BottomMm - 400)
                    {
                        if (count < 200)
                        {
                            TeamsRef[i].Fishes[0].TargetTactic.VCode = random.Next(0, 10); //随机生成速度档位 
                            TeamsRef[i].Fishes[0].TargetTactic.TCode = random.Next(0, 7);
                        }
                        else
                        {
                            TeamsRef[i].Fishes[0].TargetTactic.VCode = random.Next(0, 10);
                            TeamsRef[i].Fishes[0].TargetTactic.TCode = random.Next(8, 15);
                        }
                    }
                    else
                    {
                        TeamsRef[i].Fishes[0].TargetTactic.VCode = 4;
                        TeamsRef[i].Fishes[0].TargetTactic.TCode = 1;
                    }

                    for (int j = 1; j < CommonPara.FishCntPerTeam; j++)
                    {
                        TeamsRef[i].Fishes[j].TargetTactic = DecisionRef[i, j];

                        // 通过速度档位值和角速度档位值合法性检查把其值限制在0-14范围内 LiYoubing 20110511
                        // 为了让所有参赛队伍都能利用2011年3月16日发布时URWPGSim2D.Core.dll中的非对称角速度值
                        // 暂时将档位值范围扩大到0-15 LiYoubing 20110516
                        if (TeamsRef[i].Fishes[j].TargetTactic.VCode > 15)
                        {// 检查速度档位值上限
                            TeamsRef[i].Fishes[j].TargetTactic.VCode = 15;
                        }
                        if (TeamsRef[i].Fishes[j].TargetTactic.VCode < 0)
                        {// 检查速度档位值下限
                            TeamsRef[i].Fishes[j].TargetTactic.VCode = 0;
                        }
                        if (TeamsRef[i].Fishes[j].TargetTactic.TCode > 15)
                        {// 检查角速度档位值上限
                            TeamsRef[i].Fishes[j].TargetTactic.TCode = 15;
                        }
                        if (TeamsRef[i].Fishes[j].TargetTactic.TCode < 0)
                        {// 检查角速度档位值下限
                            TeamsRef[i].Fishes[j].TargetTactic.TCode = 0;
                        }
                    }
                }
                else if (CommonPara.Name == "生存挑战")
                {
                    TeamsRef[i].Fishes[0].TargetTactic = DecisionRef[i, 0];
                    if (TeamsRef[i].Fishes[0].TargetTactic.VCode > 15)
                    {// 检查速度档位值上限
                        TeamsRef[i].Fishes[0].TargetTactic.VCode = 15;
                    }
                    if (TeamsRef[i].Fishes[0].TargetTactic.VCode < 0)
                    {// 检查速度档位值下限
                        TeamsRef[i].Fishes[0].TargetTactic.VCode = 0;
                    }
                    if (TeamsRef[i].Fishes[0].TargetTactic.TCode > 15)
                    {// 检查角速度档位值上限
                        TeamsRef[i].Fishes[0].TargetTactic.TCode = 15;
                    }
                    if (TeamsRef[i].Fishes[0].TargetTactic.TCode < 0)
                    {// 检查角速度档位值下限
                        TeamsRef[i].Fishes[0].TargetTactic.TCode = 0;
                    }
                    for (int j = 1; j < CommonPara.FishCntPerTeam; j++)
                    {
                        TeamsRef[i].Fishes[j].TargetTactic = DecisionRef[i, j];

                        // 通过速度档位值和角速度档位值合法性检查把其值限制在0-14范围内 LiYoubing 20110511
                        // 为了让所有参赛队伍都能利用2011年3月16日发布时URWPGSim2D.Core.dll中的非对称角速度值
                        // 暂时将档位值范围扩大到0-15 LiYoubing 20110516
                        if (TeamsRef[i].Fishes[j].TargetTactic.VCode > 8)
                        {// 检查速度档位值上限
                            TeamsRef[i].Fishes[j].TargetTactic.VCode = 8;
                        }
                        if (TeamsRef[i].Fishes[j].TargetTactic.VCode < 0)
                        {// 检查速度档位值下限
                            TeamsRef[i].Fishes[j].TargetTactic.VCode = 0;
                        }
                        if (TeamsRef[i].Fishes[j].TargetTactic.TCode > 15)
                        {// 检查角速度档位值上限
                            TeamsRef[i].Fishes[j].TargetTactic.TCode = 15;
                        }
                        if (TeamsRef[i].Fishes[j].TargetTactic.TCode < 0)
                        {// 检查角速度档位值下限
                            TeamsRef[i].Fishes[j].TargetTactic.TCode = 0;
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                    {
                        TeamsRef[i].Fishes[j].TargetTactic = DecisionRef[i, j];

                        // 通过速度档位值和角速度档位值合法性检查把其值限制在0-14范围内 LiYoubing 20110511
                        // 为了让所有参赛队伍都能利用2011年3月16日发布时URWPGSim2D.Core.dll中的非对称角速度值
                        // 暂时将档位值范围扩大到0-15 LiYoubing 20110516
                        if (TeamsRef[i].Fishes[j].TargetTactic.VCode > 15)
                        {// 检查速度档位值上限
                            TeamsRef[i].Fishes[j].TargetTactic.VCode = 15;
                        }
                        if (TeamsRef[i].Fishes[j].TargetTactic.VCode < 0)
                        {// 检查速度档位值下限
                            TeamsRef[i].Fishes[j].TargetTactic.VCode = 0;
                        }
                        if (TeamsRef[i].Fishes[j].TargetTactic.TCode > 15)
                        {// 检查角速度档位值上限
                            TeamsRef[i].Fishes[j].TargetTactic.TCode = 15;
                        }
                        if (TeamsRef[i].Fishes[j].TargetTactic.TCode < 0)
                        {// 检查角速度档位值下限
                            TeamsRef[i].Fishes[j].TargetTactic.TCode = 0;
                        }
                    }

                }
            }
            if (count < 0)
            {
                count = 400;
            }
        }

        /// <summary>
        /// 实现IMission中的接口用于 更新当前使命中全部仿真机器鱼的运动学参数
        /// 注意左右半场的处理方法：若仿真机器鱼所在队伍位于右半场，
        /// 更新质心坐标值、鱼体方向值、速度方向值、角速度值时，增量值均按正常值取反，
        /// 如此便可实现运动行为反向的效果
        /// </summary>
        public void ProcessFishLocomotion()
        {
            RoboFish roboFish = new RoboFish();
            MyMission myMission = MyMission.Instance();
            for (int i = 0; i < CommonPara.TeamCount; i++)
            {
                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    int flag = 1; // 设置标志值左半场取1右半场取-1
                    if (TeamsRef[i].Para.MyHalfCourt == HalfCourt.RIGHT)
                    {
                        flag = -1;
                    }

                    // 更新当前仿真机器鱼位姿
                    TeamsRef[i].Fishes[j].UpdatePose(CommonPara.MsPerCycle, flag);

                    // 更新当前仿真机器鱼速度值速度方向和角速度值
                    TeamsRef[i].Fishes[j].UpdateVelocityAndAngularVelocity(CommonPara.MsPerCycle, flag);
                    RoboFish fish = (RoboFish)TeamsRef[i].Fishes[j].Clone();//更新该条机器鱼的碰撞模型
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, TeamsRef[i].Fishes[j].InitPhase, ref fish);
                    fish.CopyTo(TeamsRef[i].Fishes[j]);

                }
            }
        }

        /// <summary>
        /// 实现IMission中的接口用于 更新当前使命全部仿真水球的运动学参数
        /// </summary>
        public void ProcessBallLocomotion()
        {
            for (int i = 0; i < EnvRef.Balls.Count; i++)
            {
                EnvRef.Balls[i].UpdateBallLocomotionPara(CommonPara.MsPerCycle);
            }
        }

        //added by ChenXiao 20120414
        /// <summary>
        /// 实现IMission中的接口用于 更新当前使命全部动态障碍物的运动学参数
        /// </summary>
        public void ProcessDynamicObstacleLocomotion()
        {

            for (int i = 0; i < EnvRef.DynamicRect.Count; i++)
            {
                EnvRef.DynamicRect[i].UpdateRectangularDynamicLocomotionPara(CommonPara.MsPerCycle);
            }

        }

        /// <summary>
        /// 实现IMission中的接口用于 处理当前周期仿真环境中各种对象间的碰撞，包括检测和响应碰撞
        /// </summary>
        public void ProcessCollision()
        {
            //for (int s = 0; s < 3; s++)
            //{
                for (int i = 0; i < TeamsRef.Count; i++)
                {// 先将所有仿真机器鱼的碰撞状态列表清除 LiYoubing 20110511
                    for (int j = 0; j < TeamsRef[i].Fishes.Count; j++)
                    {
                        //TeamsRef[i].Fishes[j].Collision = CollisionType.NONE;
                        TeamsRef[i].Fishes[j].Collision.Clear();
                    }
                }
                #region 球和障碍物的碰撞
                for (int k = 0; k < EnvRef.Balls.Count; k++)
                {
                    CollisionDetectionResult result = new CollisionDetectionResult();
                    VelocityAndAngularVelocityResponse response = new VelocityAndAngularVelocityResponse();
                    Ball ball = EnvRef.Balls[k];
                    //定义仿真水球的速度矢量 by renjing
                    xna.Vector3 ballVelocity = new xna.Vector3(ball.VelocityMmPs * (float)Math.Cos((double)ball.VelocityDirectionRad), 0, ball.VelocityMmPs * (float)Math.Sin((double)ball.VelocityDirectionRad));
                    //begin 检测仿真水球和障碍物碰撞情况
                    //仿真水球和方形障碍物的碰撞
                    for (int n = 0; n < EnvRef.ObstaclesRect.Count; n++)
                    {
                        RectangularObstacle rectangularObstacle = EnvRef.ObstaclesRect[n];
                        result = CollisionDetection.DetectCollisionBetweenBallAndObstacle(ref ball, rectangularObstacle);
                        if (result.Intersect == true)
                        {   //碰撞响应 by renjing
                            response = CollisionResponse.CollisionResponseBetweenBallAndObstacle(result.NormalAxis, ref ballVelocity);
                            //仿真水球和障碍物的碰撞，球的速度改变量
                            ball.VelocityMmPs = response.velocityAfterCollisionA.Length();
                            if (response.velocityAfterCollisionA.Length() != 0)
                            {
                                response.velocityAfterCollisionA.Normalize();
                            }
                            float tmpAngle = xna.Vector3.Dot(response.velocityAfterCollisionA, new xna.Vector3(1, 0, 0));//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                            if (tmpAngle > 1)
                            {
                                tmpAngle = 1;
                            }
                            else if (tmpAngle < -1)
                            {
                                tmpAngle = -1;
                            }
                            //球的速度方向计算，注意方向！renjing 20110310
                            if (response.velocityAfterCollisionB.Z < 0)
                            {
                                ball.VelocityDirectionRad = -(float)Math.Acos(tmpAngle);
                            }
                            else
                            {
                                ball.VelocityDirectionRad = (float)Math.Acos(tmpAngle);
                            }
                            CollisionDetection.DetectCollisionBetweenBallAndObstacle(ref ball, rectangularObstacle);//碰撞响应处理后，再进行碰撞检测，避免错误
                        }
                    }
                    //仿真水球和圆形障碍物的碰撞
                    for (int n = 0; n < EnvRef.ObstaclesRound.Count; n++)
                    {
                        RoundedObstacle roundedObstacle = EnvRef.ObstaclesRound[n];
                        result = CollisionDetection.DetectCollisionBetweenBallAndObstacle(ref ball, roundedObstacle);
                        if (result.Intersect == true)
                        {   //碰撞响应 by renjing
                            response = CollisionResponse.CollisionResponseBetweenBallAndObstacle(result.NormalAxis, ref ballVelocity);
                            //仿真水球和障碍物的碰撞，球的速度改变量
                            ball.VelocityMmPs = response.velocityAfterCollisionA.Length();
                            if (response.velocityAfterCollisionA.Length() != 0)
                            {
                                response.velocityAfterCollisionA.Normalize();
                            }
                            float tmpAngle = xna.Vector3.Dot(response.velocityAfterCollisionA, new xna.Vector3(1, 0, 0));//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                            if (tmpAngle > 1)
                            {
                                tmpAngle = 1;
                            }
                            else if (tmpAngle < -1)
                            {
                                tmpAngle = -1;
                            }
                            //球的速度方向计算，注意方向！renjing 20110310
                            if (response.velocityAfterCollisionB.Z < 0)
                            {
                                ball.VelocityDirectionRad = -(float)Math.Acos(tmpAngle);
                            }
                            else
                            {
                                ball.VelocityDirectionRad = (float)Math.Acos(tmpAngle);
                            }
                            CollisionDetection.DetectCollisionBetweenBallAndObstacle(ref ball, roundedObstacle);//碰撞响应处理后，再进行碰撞检测，避免错误
                        }
                    }
                }

                #endregion

                #region 球和其它球的碰撞
                for (int k = 0; k < EnvRef.Balls.Count; k++)
                {
                    CollisionDetectionResult result = new CollisionDetectionResult();
                    VelocityAndAngularVelocityResponse response = new VelocityAndAngularVelocityResponse();
                    Ball ball = EnvRef.Balls[k];
                    //定义仿真水球的速度矢量 by renjing
                    xna.Vector3 ballVelocity = new xna.Vector3(ball.VelocityMmPs * (float)Math.Cos((double)ball.VelocityDirectionRad), 0, ball.VelocityMmPs * (float)Math.Sin((double)ball.VelocityDirectionRad));
                    //begin 检测仿真水球和其他仿真水球的碰撞情况
                    for (int m = k; m < EnvRef.Balls.Count; m++)
                    {
                        Ball ballNext = EnvRef.Balls[m];
                        if (m != k)
                        {
                            xna.Vector3 ballVelocityNext = new xna.Vector3(ballNext.VelocityMmPs * (float)Math.Cos((double)ballNext.VelocityDirectionRad), 0, ballNext.VelocityMmPs * (float)Math.Sin((double)ballNext.VelocityDirectionRad));
                            result = CollisionDetection.DetectCollisionBetweenTwoBalls(ref ball, ref ballNext);
                            if (result.Intersect == true)
                            {
                                response = CollisionResponse.CollisionResponseBetweenBallAndBall(result.NormalAxis, ballVelocity, ballVelocityNext);
                                ball.VelocityMmPs = response.velocityAfterCollisionA.Length();
                                if (response.velocityAfterCollisionA.Length() != 0)
                                {
                                    response.velocityAfterCollisionA.Normalize();
                                }
                                float tmpAngle1 = xna.Vector3.Dot(response.velocityAfterCollisionA, new xna.Vector3(1, 0, 0));//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                                if (tmpAngle1 > 1)
                                {
                                    tmpAngle1 = 1;
                                }
                                else if (tmpAngle1 < -1)
                                {
                                    tmpAngle1 = -1;
                                }
                                //球的速度方向计算，注意方向！renjing 20110310
                                if (response.velocityAfterCollisionA.Z < 0)
                                {
                                    ball.VelocityDirectionRad = -(float)Math.Acos(tmpAngle1);
                                }
                                else
                                {
                                    ball.VelocityDirectionRad = (float)Math.Acos(tmpAngle1);
                                }

                                ballNext.VelocityMmPs = response.velocityAfterCollisionB.Length();
                                if (response.velocityAfterCollisionB.Length() != 0)
                                {
                                    response.velocityAfterCollisionB.Normalize();
                                }
                                float tmpAngle2 = xna.Vector3.Dot(response.velocityAfterCollisionB, new xna.Vector3(1, 0, 0));//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                                if (tmpAngle2 > 1)
                                {
                                    tmpAngle2 = 1;
                                }
                                else if (tmpAngle2 < -1)
                                {
                                    tmpAngle2 = -1;
                                }
                                //球的速度方向计算，注意方向！renjing 20110310
                                if (response.velocityAfterCollisionB.Z < 0)
                                {
                                    ball.VelocityDirectionRad = -(float)Math.Acos(tmpAngle2);
                                }
                                else
                                {
                                    ball.VelocityDirectionRad = (float)Math.Acos(tmpAngle2);
                                }
                                CollisionDetection.DetectCollisionBetweenTwoBalls(ref ball, ref ballNext);
                            }
                        }
                    }

                }
                #endregion

                //ReversePose();
                int teamsFishId1 = 0, fishesId1 = 0;
                for (int i = 0; i < TeamsRef.Count; i++)
                {
                    for (int j = 0; j < TeamsRef[i].Fishes.Count; j++)
                    {
                        fishesId1 = teamsFishId1 + j;
                        RoboFish f1 = TeamsRef[i].Fishes[j];
                        for (int c = 0; c < 4; c++)
                        {//定义阈值，可更改
                            f1.TransactionVectorThreshold[c] = 0.5f;
                            f1.TransactionVectorThreshold[1] = 1;
                            f1.TransactionVectorThreshold[0] = 10;
                        }

                        //定义仿真机器鱼的速度矢量 by renjing 

                        xna.Vector3 f1Velocity = new xna.Vector3(f1.VelocityMmPs * (float)Math.Cos((double)f1.VelocityDirectionRad), 0, f1.VelocityMmPs * (float)Math.Sin((double)f1.VelocityDirectionRad));
                        CollisionDetectionResult result = new CollisionDetectionResult();
                        VelocityAndAngularVelocityResponse response = new VelocityAndAngularVelocityResponse();
                        RoboFish roboFish = new RoboFish();
                        MyMission myMission = MyMission.Instance();

                        #region 鱼和障碍物的碰撞
                        //仿真机器鱼和方形障碍物的碰撞
                        for (int n = 0; n < EnvRef.ObstaclesRect.Count; n++)
                        {
                            RectangularObstacle rectangularObstacle = EnvRef.ObstaclesRect[n];
                            result = CollisionDetection.DetectCollisionBetweenFishAndObstacle(ref f1, rectangularObstacle);
                            if (result.Intersect == true)
                            {
                                response = CollisionResponse.CollisionResponseBetweenFishAndObstacle(result.NormalAxis,
                                    ref f1Velocity, f1.PolygonVertices[0], f1.PositionMm, result.ActionPoint, ref f1.BodyDirectionRad);
                                f1.BodyDirectionRad += response.deltaAngularVelocityA * 0.01f;//仿真机器鱼和障碍物碰撞，鱼角速度改变量表现为一周期内鱼体朝向的改变
                                f1.PositionMm += response.velocityAfterCollisionA * 0.01f;//仿真机器鱼和障碍物碰撞，鱼速度改变量表现为一周期内鱼位置的改变
                                f1.VelocityDirectionRad += response.deltaAngularVelocityA * 0.01f;//速度方向和鱼体朝向保持一致
                                roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f1.InitPhase, ref f1);//更新机器鱼碰撞模型


                                //CollisionDetection.DetectCollisionBetweenFishAndObstacle(ref f1, rectangularObstacle);//碰撞响应处理后，再进行碰撞检测，避免错误
                            }

                        }
                        //仿真机器鱼和圆形障碍物的碰撞
                        for (int n = 0; n < EnvRef.ObstaclesRound.Count; n++)
                        {
                            RoundedObstacle roundedObstacle = EnvRef.ObstaclesRound[n];
                            result = CollisionDetection.DetectCollisionBetweenFishAndObstacle(ref f1, roundedObstacle);
                            if (result.Intersect == true)
                            {
                                response = CollisionResponse.CollisionResponseBetweenFishAndObstacle(result.NormalAxis,
                                    ref f1Velocity, f1.PolygonVertices[0], f1.PositionMm, result.ActionPoint, ref f1.BodyDirectionRad);
                                f1.BodyDirectionRad += response.deltaAngularVelocityA * 0.01f;//仿真机器鱼和障碍物碰撞，鱼角速度改变量表现为一周期内鱼体朝向的改变
                                f1.PositionMm += response.velocityAfterCollisionA * 0.01f;//仿真机器鱼和障碍物碰撞，鱼速度改变量表现为一周期内鱼位置的改变
                                f1.VelocityDirectionRad += response.deltaAngularVelocityA * 0.01f;//速度方向和鱼体朝向保持一致
                                roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f1.InitPhase, ref f1);//更新机器鱼碰撞模型

                                //CollisionDetection.DetectCollisionBetweenFishAndObstacle(ref f1, roundedObstacle);//碰撞响应处理后，再进行碰撞检测，避免错误
                            }

                        }
                        #endregion

                        #region 鱼和另外鱼的碰撞
                        int teamsFishId2 = 0, fishesId2 = 0;
                        for (int m = 0; m < TeamsRef.Count; m++)
                        {
                            for (int n = 0; n < TeamsRef[m].Fishes.Count; n++)
                            {
                                fishesId2 = teamsFishId2 + n;
                                if (fishesId2 > fishesId1)
                                {
                                    RoboFish f2 = TeamsRef[m].Fishes[n];
                                    //定义f2仿真机器鱼的速度矢量 by renjing
                                    xna.Vector3 f2Velocity = new xna.Vector3(f2.VelocityMmPs * (float)Math.Cos((double)f2.VelocityDirectionRad), 0, f2.VelocityMmPs * (float)Math.Sin((double)f2.VelocityDirectionRad));
                                    // 检测两条仿真机器鱼之间的块碰撞情况
                                    result = CollisionDetection.DetectCollisionBetweenTwoFishes(ref f1, ref f2);

                                    if (result.Intersect == true)
                                    {   //回溯半个周期，检测半周期前的碰撞情况，这是连续碰撞的一种简化处理，目前运用这种方法，运行速度有所降低 added by renjing 20110310
                                        xna.Vector3 f1PositionMmNow = f1.PositionMm;
                                        xna.Vector3 f2PositionMmNow = f2.PositionMm;
                                        f1.PositionMm -= (float)0.05 * f1Velocity;
                                        f2.PositionMm -= f2Velocity * (float)0.05;
                                        roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f1.InitPhase, ref f1);//更新机器鱼碰撞模型
                                        roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f2.InitPhase, ref f2);//更新机器鱼碰撞模型
                                        // 检测两条仿真机器鱼之间的块碰撞情况
                                        result = CollisionDetection.DetectCollisionBetweenTwoFishes(ref f1, ref f2);
                                        if (result.Intersect == false)
                                        {   //回溯半个周期后，未发生碰撞，继续按照本周期的碰撞进行处理 added by renjing 20110310
                                            f1.PositionMm = f1PositionMmNow;
                                            f2.PositionMm = f2PositionMmNow;
                                            roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f1.InitPhase, ref f1);//更新机器鱼碰撞模型
                                            roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f2.InitPhase, ref f2);//更新机器鱼碰撞模型
                                        }
                                        //碰撞响应 by renjing
                                        response = CollisionResponse.CollisionResponseBetweenFishAndFish(result.NormalAxis, f1Velocity, f2Velocity,
                                                   f1.PolygonVertices[0], f2.PolygonVertices[0], f1.PositionMm, f2.PositionMm,
                                                   result.ActionPoint, ref f1.BodyDirectionRad, ref f2.BodyDirectionRad);

                                        f1.BodyDirectionRad += response.deltaAngularVelocityA * 0.01f;//仿真机器鱼之间碰撞，f1鱼的角速度改变量表现为一周期内鱼体朝向的改变
                                        f1.PositionMm += response.velocityAfterCollisionA * 0.01f;//仿真机器鱼和边界碰撞，f1鱼速度改变量表现为一周期内鱼位置的改变
                                        f1.VelocityDirectionRad += response.deltaAngularVelocityA * 0.01f;//鱼速度方向和鱼体朝向保持一致
                                        roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f1.InitPhase, ref f1);//更新机器鱼碰撞模型

                                        f2.BodyDirectionRad += response.deltaAngularVelocityB * 0.01f;//仿真机器鱼之间碰撞，f2鱼的角速度改变量表现为一周期内鱼体朝向的改变
                                        f2.PositionMm += response.velocityAfterCollisionB * 0.01f;//仿真机器鱼和边界碰撞，f2鱼速度改变量表现为一周期内鱼位置的改变
                                        f2.VelocityDirectionRad += response.deltaAngularVelocityB * 0.01f;//鱼速度方向和鱼体朝向保持一致
                                        roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f2.InitPhase, ref f2);//更新机器鱼碰撞模型

                                        //CollisionDetection.DetectCollisionBetweenTwoFishes(ref f1, ref f2);//碰撞响应处理后，再进行碰撞检测，避免错误
                                    }
                                }
                            }
                            teamsFishId2 += TeamsRef[m].Fishes.Count;
                        }
                        #endregion

                        #region 鱼和球的碰撞
                            for (int k = 0; k < EnvRef.Balls.Count; k++)
                            {
                                Ball ball = EnvRef.Balls[k];
                                //定义仿真水球的速度矢量 by renjing
                                xna.Vector3 ballVelocity = new xna.Vector3(ball.VelocityMmPs * (float)Math.Cos((double)ball.VelocityDirectionRad), 0, ball.VelocityMmPs * (float)Math.Sin((double)ball.VelocityDirectionRad));
                                //float CollisionTime = 1;//记录碰撞时刻，取值范围为[0,1]，随着二分法而改变，初始值为1，表示为当前时刻
                                result = CollisionDetection.DetectCollisionBetweenFishAndBall(ref f1, ref ball);
                                if (result.Intersect == true)
                                {
                                    xna.Vector3 fishPositionMmNow = f1.PositionMm;
                                    xna.Vector3 ballPositionMmNow = ball.PositionMm;
                                    f1.PositionMm -= (float)0.05 * f1Velocity;
                                    ball.PositionMm -= ballVelocity * (float)0.05;
                                    roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f1.InitPhase, ref f1);//更新机器鱼碰撞模型
                                    // 检测两条仿真机器鱼之间的块碰撞情况
                                    result = CollisionDetection.DetectCollisionBetweenFishAndBall(ref f1, ref ball);
                                    if (result.Intersect == false)
                                    {
                                        f1.PositionMm = fishPositionMmNow;
                                        ball.PositionMm = ballPositionMmNow;
                                        roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f1.InitPhase, ref f1);//更新机器鱼碰撞模型
                                    }
                                    //碰撞响应 by renjing
                                    response = CollisionResponse.CollisionResponseBetweenFishAndBall(result.NormalAxis, ref f1Velocity, ref ballVelocity, f1.PolygonVertices[0],
                                               f1.PositionMm, result.ActionPoint, ref f1.BodyDirectionRad);
                                    f1.BodyDirectionRad += response.deltaAngularVelocityA * 0.01f;//仿真机器鱼和仿真水球碰撞，鱼的角速度改变量表现为一周期内鱼体朝向的改变
                                    f1.PositionMm += response.velocityAfterCollisionA * 0.01f;//仿真机器鱼和边界碰撞，鱼速度改变量表现为一周期内鱼位置的改变
                                    f1.VelocityDirectionRad += response.deltaAngularVelocityA * 0.01f;//鱼速度方向和鱼体朝向保持一致
                                    roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f1.InitPhase, ref f1);//更新机器鱼碰撞模型

                                    ball.VelocityMmPs = response.velocityAfterCollisionB.Length();
                                    if (response.velocityAfterCollisionB.Length() != 0)
                                    {
                                        response.velocityAfterCollisionB.Normalize();
                                    }
                                    float tmpAngle = xna.Vector3.Dot(response.velocityAfterCollisionB, new xna.Vector3(1, 0, 0));//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                                    if (tmpAngle > 1)
                                    {
                                        tmpAngle = 1;
                                    }
                                    else if (tmpAngle < -1)
                                    {
                                        tmpAngle = -1;
                                    }
                                    //球的速度方向计算，注意方向！renjing 20110310
                                    if (response.velocityAfterCollisionB.Z < 0)
                                    {
                                        ball.VelocityDirectionRad = -(float)Math.Acos(tmpAngle);
                                    }
                                    else
                                    {
                                        ball.VelocityDirectionRad = (float)Math.Acos(tmpAngle);
                                    }

                                    //CollisionDetection.DetectCollisionBetweenFishAndBall(ref f1, ref ball);//碰撞响应处理后，再进行碰撞检测，避免错误
                                }
                            }
                            #endregion

                        #region 鱼和边界的碰撞
                        result = CollisionDetection.DetectCollisionBetweenFishAndBorder(ref f1, Field.Instance());
                        roboFish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, f1.InitPhase, ref f1);//更新机器鱼碰撞模型

                        #endregion
                    }
                    teamsFishId1 += TeamsRef[i].Fishes.Count;
                }
                #region 球与边界的碰撞检测
                for (int k = 0; k < EnvRef.Balls.Count; k++)
                {
                    CollisionDetectionResult result = new CollisionDetectionResult();
                    VelocityAndAngularVelocityResponse response = new VelocityAndAngularVelocityResponse();
                    Ball ball = EnvRef.Balls[k];
                    //定义仿真水球的速度矢量 by renjing
                    xna.Vector3 ballVelocity = new xna.Vector3(ball.VelocityMmPs * (float)Math.Cos((double)ball.VelocityDirectionRad), 0, ball.VelocityMmPs * (float)Math.Sin((double)ball.VelocityDirectionRad));
                    //begin 检测仿真水球和边界碰撞情况
                    result = CollisionDetection.DetectCollisionBetweenBallAndBorder(ref ball, Field.Instance());
                    if (result.Intersect == true)
                    {   //碰撞响应 by renjing
                        response = CollisionResponse.CollisionResponseBetweenBallAndBorder(result.NormalAxis, ref ballVelocity);
                        //仿真水球和边界的碰撞，球的速度改变量
                        ball.VelocityMmPs = response.velocityAfterCollisionA.Length();
                        if (response.velocityAfterCollisionA.Length() != 0)
                        {
                            response.velocityAfterCollisionA.Normalize();
                        }
                        float tmpAngle = xna.Vector3.Dot(response.velocityAfterCollisionA, new xna.Vector3(1, 0, 0));//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                        if (tmpAngle > 1)
                        {
                            tmpAngle = 1;
                        }
                        else if (tmpAngle < -1)
                        {
                            tmpAngle = -1;
                        }
                        //球的速度方向计算，注意方向！renjing 20110310
                        if (response.velocityAfterCollisionB.Z < 0)
                        {
                            ball.VelocityDirectionRad = -(float)Math.Acos(tmpAngle);
                        }
                        else
                        {
                            ball.VelocityDirectionRad = (float)Math.Acos(tmpAngle);
                        }
                        CollisionDetection.DetectCollisionBetweenBallAndBorder(ref ball, Field.Instance());
                    }
                }
                #endregion
                #region 球和障碍物的碰撞
                for (int k = 0; k < EnvRef.Balls.Count; k++)
                {
                    CollisionDetectionResult result = new CollisionDetectionResult();
                    VelocityAndAngularVelocityResponse response = new VelocityAndAngularVelocityResponse();
                    Ball ball = EnvRef.Balls[k];
                    //定义仿真水球的速度矢量 by renjing
                    xna.Vector3 ballVelocity = new xna.Vector3(ball.VelocityMmPs * (float)Math.Cos((double)ball.VelocityDirectionRad), 0, ball.VelocityMmPs * (float)Math.Sin((double)ball.VelocityDirectionRad));
                    //begin 检测仿真水球和障碍物碰撞情况
                    //仿真水球和方形障碍物的碰撞
                    for (int n = 0; n < EnvRef.ObstaclesRect.Count; n++)
                    {
                        RectangularObstacle rectangularObstacle = EnvRef.ObstaclesRect[n];
                        result = CollisionDetection.DetectCollisionBetweenBallAndObstacle(ref ball, rectangularObstacle);
                        if (result.Intersect == true)
                        {   //碰撞响应 by renjing
                            response = CollisionResponse.CollisionResponseBetweenBallAndObstacle(result.NormalAxis, ref ballVelocity);
                            //仿真水球和障碍物的碰撞，球的速度改变量
                            ball.VelocityMmPs = response.velocityAfterCollisionA.Length();
                            if (response.velocityAfterCollisionA.Length() != 0)
                            {
                                response.velocityAfterCollisionA.Normalize();
                            }
                            float tmpAngle = xna.Vector3.Dot(response.velocityAfterCollisionA, new xna.Vector3(1, 0, 0));//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                            if (tmpAngle > 1)
                            {
                                tmpAngle = 1;
                            }
                            else if (tmpAngle < -1)
                            {
                                tmpAngle = -1;
                            }
                            //球的速度方向计算，注意方向！renjing 20110310
                            if (response.velocityAfterCollisionB.Z < 0)
                            {
                                ball.VelocityDirectionRad = -(float)Math.Acos(tmpAngle);
                            }
                            else
                            {
                                ball.VelocityDirectionRad = (float)Math.Acos(tmpAngle);
                            }
                            CollisionDetection.DetectCollisionBetweenBallAndObstacle(ref ball, rectangularObstacle);//碰撞响应处理后，再进行碰撞检测，避免错误
                        }
                    }
                    //仿真水球和圆形障碍物的碰撞
                    for (int n = 0; n < EnvRef.ObstaclesRound.Count; n++)
                    {
                        RoundedObstacle roundedObstacle = EnvRef.ObstaclesRound[n];
                        result = CollisionDetection.DetectCollisionBetweenBallAndObstacle(ref ball, roundedObstacle);
                        if (result.Intersect == true)
                        {   //碰撞响应 by renjing
                            response = CollisionResponse.CollisionResponseBetweenBallAndObstacle(result.NormalAxis, ref ballVelocity);
                            //仿真水球和障碍物的碰撞，球的速度改变量
                            ball.VelocityMmPs = response.velocityAfterCollisionA.Length();
                            if (response.velocityAfterCollisionA.Length() != 0)
                            {
                                response.velocityAfterCollisionA.Normalize();
                            }
                            float tmpAngle = xna.Vector3.Dot(response.velocityAfterCollisionA, new xna.Vector3(1, 0, 0));//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                            if (tmpAngle > 1)
                            {
                                tmpAngle = 1;
                            }
                            else if (tmpAngle < -1)
                            {
                                tmpAngle = -1;
                            }
                            //球的速度方向计算，注意方向！renjing 20110310
                            if (response.velocityAfterCollisionB.Z < 0)
                            {
                                ball.VelocityDirectionRad = -(float)Math.Acos(tmpAngle);
                            }
                            else
                            {
                                ball.VelocityDirectionRad = (float)Math.Acos(tmpAngle);
                            }
                            CollisionDetection.DetectCollisionBetweenBallAndObstacle(ref ball, roundedObstacle);//碰撞响应处理后，再进行碰撞检测，避免错误
                        }
                    }
                }

                #endregion

                //RecoveryPose();x
            //}
          }

        /// <summary>
        /// 处理当前仿真使命的控制规则，在具体使命类中实现
        /// </summary>
        public virtual void ProcessControlRules() { }

        /// <summary>
        /// 实现IMission中的接口用于 绘制当前使命中的动态图形对象
        /// </summary>
        /// <returns>绘制好各动态图形对象的Bitmap对象</returns>
        public Bitmap Draw()
        {
            Bitmap bmp = new Bitmap(Field.Instance().PictureBoxXPix, Field.Instance().PictureBoxZPix);
            Graphics myGraphic = Graphics.FromImage(bmp);
            
            #region 绘制当前仿真使命中全部仿真机器鱼
            for (int i = 0; i < CommonPara.TeamCount; i++)
            {
                int flag = (TeamsRef[i].Para.MyHalfCourt == HalfCourt.RIGHT) ? -1 : 1;
                //if (TeamsRef[i].Para.MyHalfCourt == HalfCourt.RIGHT)
                //{
                //    flag = -1;
                //}
                for (int j = 0; j < CommonPara.FishCntPerTeam; j++)
                {
                    try
                    {
                        TeamsRef[i].Fishes[j].Draw(ref myGraphic, CommonPara.IsRunning, CommonPara.IsPaused, TeamsRef[i].Fishes[j].InitPhase, j + 1, flag);
                    }
                    catch 
                    {
                    }
                    PreRoboFish[i * 10 + j] = (RoboFish)TeamsRef[i].Fishes[j].Clone();//记录上一周期的机器鱼状态
                    PreRoboFish[i * 10 + j].CalculateFishPostion(CommonPara.IsRunning, CommonPara.IsPaused, TeamsRef[i].Fishes[j].InitPhase, ref PreRoboFish[i * 10 + j]);//更新碰撞模型
                }
            }
            #endregion

            #region 绘制当前仿真使命中全部仿真水球
            for (int i = 0; i < EnvRef.Balls.Count; i++)
            {
                EnvRef.Balls[i].Draw(ref myGraphic);
            }
            #endregion

            #region 绘制当前仿真使命中全部仿真障碍物
            for (int i = 0; i < EnvRef.ObstaclesRound.Count; i++)
            {
                EnvRef.ObstaclesRound[i].Draw(ref myGraphic);
            }
            for (int i = 0; i < EnvRef.ObstaclesRect.Count; i++)
            {
                EnvRef.ObstaclesRect[i].Draw(ref myGraphic);
            }
            //added by ChenXiao 20120401
            for (int i = 0; i < EnvRef.DynamicRect.Count; i++)
            {
                EnvRef.DynamicRect[i].Draw(ref myGraphic);
            }
            #endregion

            #region 绘制当前仿真使命中全部仿真通道
            for (int i = 0; i < EnvRef.Channels.Count; i++)
            {
                EnvRef.Channels[i].Draw(ref myGraphic);
            }
            #endregion

            #region 绘制当前仿真使命类型名称/计时/队名/比分等
            //Field f = Field.Instance();
            //int seconds = CommonPara.RemainingCycles * CommonPara.MsPerCycle / 1000; // 使命时间剩余秒数
            
            //// 仿真使命类型名称和仿真使命持续时间绘制在同一行，起始点距左边球门块和底边分别为10像素和100像素距离
            //PointF ptfTmp = new PointF(f.MmToPixX(f.LeftMm + f.GoalDepthMm) + 10, f.MmToPixZ(f.BottomMm) - 100);
            //string strTmp = CommonPara.Name + " / " + string.Format("{0:00} : {1:00}", seconds / 60, seconds % 60);
            //myGraphic.DrawString(strTmp, new Font("宋体", 12), Brushes.White, ptfTmp);

            //ptfTmp = new PointF(ptfTmp.X, ptfTmp.Y + 50);   // 仿真使命队伍及比分绘制在第二行，距离上一行50像素
            //strTmp = TeamsRef[0].Para.Name; // 若只有一支队伍则第二行只绘制该队伍名字
            //if (CommonPara.TeamCount == 2)  // 若有两支队伍则按照“队伍1  比分1 : 比分2  队伍2”格式绘制
            //{
            //    strTmp += "  " + string.Format("{0:00}", TeamsRef[0].Para.Score) + " : "
            //        + string.Format("{0:00}", TeamsRef[1].Para.Score) + "  " + TeamsRef[1].Para.Name;
            //}
            //myGraphic.DrawString(strTmp, new Font("宋体", 12), Brushes.White, ptfTmp);
            #endregion

            return bmp;
        }
        #endregion

        #region 其他接口方法
        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值，在具体仿真使命类中实现
        /// </summary>
        public virtual void ResetTeamsAndFishes() { }

        // longhainan 20120801
        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值，在具体仿真使命类中实现
        /// </summary>
        public virtual void ResetGoalHandler() { }
        //longhainan 20120801
        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值，在具体仿真使命类中实现
        /// </summary>
        public virtual void ResetShootout() { }

        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真水球恢复默认位置，在具体仿真使命类中实现
        /// </summary>
        public virtual void ResetBalls() { }

        // LiYoubing 20110622
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真障碍物恢复默认位置，在具体仿真使命类中实现
        /// </summary>
        public virtual void ResetObstacles() { }

        // ChenXiao 20120415
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部动态仿真障碍物恢复默认位置，在具体仿真使命类中实现
        /// </summary>
        public virtual void ResetDynamicObstacles() { }

        // LiYoubing 20110617
        /// <summary>
        /// 设置仿真机器鱼鱼体和编号默认颜色，在具体仿真使命类中实现
        /// </summary>
        public virtual void ResetColorFishAndId() { }

        // LiYoubing 20110617
        /// <summary>
        /// 实现IMission中的接口用于 设置仿真水球填充和边框默认颜色，仿真使命基类中提供标准实现，可在具体仿真使命类中重载
        /// </summary>
        public virtual void ResetColorBall()
        {
            for (int i = 0; i < EnvRef.Balls.Count; i++)
            {
                // 所有仿真水球默认填充颜色和边框颜色均为粉色
                EnvRef.Balls[i].ColorFilled = Color.Pink;
                EnvRef.Balls[i].ColorBorder = Color.Pink;
            }
        }

        // LiYoubing 20110701
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命使用的仿真场地尺寸恢复默认值，在具体仿真使命类中实现
        /// </summary>
        public virtual void ResetField()
        {// 默认实现
            Field f = Field.Instance();
            // 仿真场地长宽恢复配置文件中配置的默认值
            f.FieldLengthXMm = f.FieldLengthXOriMm;
            f.FieldLengthZMm = f.FieldLengthZOriMm;
            // 根据设定的场地长度和宽度重新计算其他需要计算的场地参数
            f.FieldCalculation();
        }

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命相应的仿真环境各参数设置为默认值，在具体仿真使命类中实现
        /// </summary>
        public virtual void ResetEnvironment() { }

        /// <summary>
        /// 设置仿真机器鱼的颜色
        /// </summary>
        /// <param name="teamId">待设置颜色的仿真机器鱼所在队伍编号（从0开始）</param>
        /// <param name="fishId">待设置颜色的仿真机器鱼在其队伍中的编号（从0开始）</param>
        /// <param name="ColorFish">仿真机器鱼鱼体颜色 modified by chenwei 20110603</param>
        public void SetColorFish(int teamId, int fishId, Color ColorFish)
        {
            if ((teamId >= 0) && (teamId < CommonPara.TeamCount)
                && (fishId >= 0) && (fishId < CommonPara.FishCntPerTeam))
            {
                TeamsRef[teamId].Fishes[fishId].ColorFish = ColorFish;
            }
        }

        /// <summary>
        /// 设置仿真机器鱼位姿
        /// </summary>
        /// <param name="teamId">待设置颜色的仿真机器鱼所在队伍编号（从0开始）</param>
        /// <param name="fishId">待设置颜色的仿真机器鱼在其队伍中的编号（从0开始）</param>
        /// <param name="position">仿真机器鱼位置</param>
        /// <param name="direction">仿真机器鱼方向</param>
        public void SetFishPose(int teamId, int fishId, xna.Vector3 position, float direction)
        {
            if ((teamId >= 0) && (teamId < CommonPara.TeamCount)
               && (fishId >= 0) && (fishId < CommonPara.FishCntPerTeam))
            {
                TeamsRef[teamId].Fishes[fishId].PositionMm = position;
                TeamsRef[teamId].Fishes[fishId].BodyDirectionRad = direction;
            }
        }
        #endregion
        #endregion

        # region 实现IDssSerializable和ICloneable接口
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public virtual void CopyTo(IDssSerializable target)
        {
            Mission typedTarget = target as Mission;

            if (typedTarget == null)
            {
                throw new ArgumentException("CopyTo({0}) requires type {0}", this.GetType().FullName);
            }

            this.CommonPara.CopyTo(typedTarget.CommonPara);
            for (int i = 0; i < this.TeamsRef.Count; i++)
            {
                typedTarget.TeamsRef.Add(new Team<RoboFish>());

                // 将当前对象第i支队伍的公共参数CopyTo目标对象
                this.TeamsRef[i].Para.CopyTo(typedTarget.TeamsRef[i].Para);
                for (int j = 0; j < this.TeamsRef[i].Fishes.Count; j++)
                {
                    typedTarget.TeamsRef[i].Fishes.Add(new RoboFish());

                    // 将当前对象第i支队伍第j条仿真机器鱼的信息CopyTo目标对象
                    this.TeamsRef[i].Fishes[j].CopyTo(typedTarget.TeamsRef[i].Fishes[j]);
                }
            }
            this.EnvRef.CopyTo(typedTarget.EnvRef);
            foreach (DictionaryEntry de in this.HtMissionVariables)
            {// 遍历哈希表为目标对象重构哈希表
                typedTarget.HtMissionVariables.Add(de.Key, de.Value);
            }
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public virtual object Clone()
        {
            Mission target = new Mission();

            target.CommonPara = (MissionCommonPara)this.CommonPara.Clone();
            for (int i = 0; i < this.TeamsRef.Count; i++)
            {
                target.TeamsRef.Add(new Team<RoboFish>());

                // 将当前对象第i支队伍的公共参数Clone到目标对象
                this.TeamsRef[i].Para.CopyTo(target.TeamsRef[i].Para);
                for (int j = 0; j < this.TeamsRef[i].Fishes.Count; j++)
                {
                    target.TeamsRef[i].Fishes.Add(new RoboFish());

                    // 将当前对象第i支队伍第j条仿真机器鱼的信息Clone到目标对象
                    target.TeamsRef[i].Fishes[j] = (RoboFish)this.TeamsRef[i].Fishes[j].Clone();
                }
            }
            target.EnvRef = (SimEnvironment)this.EnvRef.Clone();
            foreach (DictionaryEntry de in this.HtMissionVariables)
            {// 遍历哈希表为目标对象重构哈希表
                target.HtMissionVariables.Add(de.Key, de.Value);
            }

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public virtual void Serialize(System.IO.BinaryWriter writer)
        {
            this.CommonPara.Serialize(writer);
            for (int i = 0; i < this.TeamsRef.Count; i++)
            {
                // 将当前对象第i支队伍的公共参数写入writer
                this.TeamsRef[i].Para.Serialize(writer);
                for (int j = 0; j < this.TeamsRef[i].Fishes.Count; j++)
                {
                    // 将当前对象第i支队伍第j条仿真机器鱼的信息写入writer
                    this.TeamsRef[i].Fishes[j].Serialize(writer);
                }
            }
            this.EnvRef.Serialize(writer);
            // 先将Hashtable的元素个数序列化供反序列化时重构Hashtable用
            writer.Write(HtMissionVariables.Count);
            foreach (DictionaryEntry de in this.HtMissionVariables)
            {// 遍历Hashtable序列化键值对
                writer.Write((string)de.Key);
                writer.Write((string)de.Value);
            }
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public virtual object Deserialize(System.IO.BinaryReader reader)
        {
            this.CommonPara.Deserialize(reader);
            for (int i = 0; i < this.CommonPara.TeamCount; i++)
            {// 特别注意TeamsRef列表必须先添加Team<RoboFish>类型的元素后才能从reader中取值填充
                this.TeamsRef.Add(new Team<RoboFish>());

                // 从reader中读取值填充当前对象第i支队伍的公共参数
                this.TeamsRef[i].Para.Deserialize(reader);
                for (int j = 0; j < this.CommonPara.FishCntPerTeam; j++)
                {// TeamsRef[i].Fishes列表必须先添加RoboFish类型元素后才能从readerz中取值填充
                    this.TeamsRef[i].Fishes.Add(new RoboFish());

                    // 从reader中读取值填充当前对象第i支队伍第j条仿真机器鱼的信息
                    this.TeamsRef[i].Fishes[j].Deserialize(reader);
                }
            }
            this.EnvRef.Deserialize(reader);
            // 反序列化Hashtable元素个数
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {// 反序列化键值对重构Hashtable
                string key = reader.ReadString();
                string value = reader.ReadString();
                this.HtMissionVariables.Add(key, value);
            }

            return this;
        }
        # endregion
    }

    /// <summary>
    ///  仿真使命（比赛或实验项目）公共参数类型结构体
    /// </summary>    
    [DataContract]
    [Serializable]
    public class MissionCommonPara : ICloneable, IDssSerializable
    {
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public MissionCommonPara() { }

        /// <summary>
        /// 带参数构造函数，默认构造出1V1比赛类型的使命公共参数结构体变量
        /// </summary>
        public MissionCommonPara(string strName, int iTeamCount, int iFishCntPerTeam,
            int iTotalSeconds, int iMsPerCycle,bool isGoalBlockNeeded, bool isFieldInnerLinesNeeded)
        {
            Name = strName;
            TeamCount = iTeamCount;
            FishCntPerTeam = iFishCntPerTeam;
            TotalSeconds = iTotalSeconds;
            MsPerCycle = iMsPerCycle;
            IsGoalBlockNeeded = isGoalBlockNeeded;
            IsFieldInnerLinesNeeded =(isFieldInnerLinesNeeded==true)?1:0;

            SetDefaultValue();
        }

        /// <summary>
        /// 带参数构造函数，默认构造出搬运比赛类型的使命公共参数结构体变量
        /// </summary>
        public MissionCommonPara(string strName, int iTeamCount, int iFishCntPerTeam,
    int iTotalSeconds, int iMsPerCycle, bool isGoalBlockNeeded, int isFieldInnerLinesNeeded)
        {
            Name = strName;
            TeamCount = iTeamCount;
            FishCntPerTeam = iFishCntPerTeam;
            TotalSeconds = iTotalSeconds;
            MsPerCycle = iMsPerCycle;
            IsGoalBlockNeeded = isGoalBlockNeeded;
            IsFieldInnerLinesNeeded = isFieldInnerLinesNeeded;

            SetDefaultValue();
        }
        private void SetDefaultValue()
        {
            RemainingCycles = TotalSeconds * 1000 / MsPerCycle;
            IsRunning = false;
            IsPaused = false;
            //ReadyTeamCount = 0;
        }

        /// <summary>
        ///  仿真使命（比赛或实验项目）名称字符串
        /// </summary>
        [DataMember]
        public string Name;

        /// <summary>
        ///  仿真使命（比赛或实验项目）参与队伍数量
        /// </summary>
        [DataMember]
        public int TeamCount;

        /// <summary>
        ///  仿真使命（比赛或实验项目）参与队伍每队仿真机器鱼数量
        /// </summary>
        [DataMember]
        public int FishCntPerTeam;

        /// <summary>
        ///  仿真使命（比赛或实验项目）完成所需时间总秒数
        /// </summary>
        [DataMember]
        public int TotalSeconds;

        /// <summary>
        ///  仿真使命（比赛或实验项目）的仿真周期毫秒数
        /// </summary>
        [DataMember]
        public int MsPerCycle;

        /// <summary>
        ///  仿真使命（比赛或实验项目）过程当前所剩仿真周期数
        /// </summary>
        [DataMember]
        public int RemainingCycles;

        /// <summary>
        ///  仿真使命（比赛或实验项目）是否正在运行，用于实现界面状态约束
        /// </summary>
        public bool IsRunning;

        /// <summary>
        ///  仿真使命（比赛或实验项目）是否正在运行，且处于暂停状态
        /// </summary>
        public bool IsPaused;

        /// <summary>
        /// 仿真使命（比赛或实验项目）运行过程中，当前周期是否需要弹出提示对话框
        /// </summary>
        public bool IsShowDlgNeeded;

        /// <summary>
        /// 仿真使命（比赛或实验项目）运行过程中，需要使用弹出对话框显示的消息字符串
        /// </summary>
        public string Message;

        /// <summary>
        /// 动态对象（仿真机器鱼和仿真水球）参数集中显示窗口DlgFishInfo打开后经过的周期数
        /// </summary>
        public int DisplayingCycles;

        /// <summary>
        /// 仿真使命（比赛或实验项目）运行过程中，需要模拟点击界面“暂停”按钮，true需要，false不需要
        /// 用于对抗性比赛进球或交换半场后，在ProcessControlRules方法中设置以通知Sim2DSvr使用代码模拟按下“暂停”按钮
        /// </summary>
        /// <remarks>added by LiYoubing 20110309</remarks>
        public bool IsPauseNeeded;
  
        // added by liushu 20110314
        /// <summary>
        /// 仿真使命运行过程中交换了半场，true为已经交换，false为没有交换
        /// </summary>
        public bool IsExchangedHalfCourt;

        ///// <summary>
        ///// 已经准备好按下“Ready”按钮的队伍数量
        ///// </summary>
        //public int ReadyTeamCount;

        // added by zhangbo 20111101
        /// <summary>
        /// 是否需要绘制球门块
        /// </summary>
        public bool IsGoalBlockNeeded;

        // added by zhangbo 20111101
        /// <summary>
        /// 是否需要绘制禁区线、中圈线
        /// </summary>
        public int IsFieldInnerLinesNeeded;

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public virtual void CopyTo(IDssSerializable target)
        {
            MissionCommonPara typedTarget = target as MissionCommonPara;

            typedTarget.FishCntPerTeam = this.FishCntPerTeam;
            typedTarget.IsRunning = this.IsRunning;
            typedTarget.MsPerCycle = this.MsPerCycle;
            typedTarget.Name = this.Name;
            typedTarget.RemainingCycles = this.RemainingCycles;
            typedTarget.TeamCount = this.TeamCount;
            typedTarget.TotalSeconds = this.TotalSeconds;
            typedTarget.IsExchangedHalfCourt = this.IsExchangedHalfCourt;
            
            // CopyTo对MissionCommonPara的每一个Field都进行拷贝，供对象间值传递用
            typedTarget.DisplayingCycles = this.DisplayingCycles;
            typedTarget.IsPaused = this.IsPaused;
            //typedTarget.ReadyTeamCount = this.ReadyTeamCount;
            typedTarget.IsGoalBlockNeeded = this.IsGoalBlockNeeded;
            typedTarget.IsFieldInnerLinesNeeded = this.IsFieldInnerLinesNeeded;
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public virtual object Clone()
        {
            MissionCommonPara target = new MissionCommonPara();

            target.FishCntPerTeam = this.FishCntPerTeam;
            target.IsRunning = this.IsRunning;
            target.MsPerCycle = this.MsPerCycle;
            target.Name = this.Name;
            target.RemainingCycles = this.RemainingCycles;
            target.TeamCount = this.TeamCount;
            target.TotalSeconds = this.TotalSeconds;
            target.IsExchangedHalfCourt = this.IsExchangedHalfCourt;

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public virtual void Serialize(System.IO.BinaryWriter writer)
        {// 只Serialize需要在Dss Node间传递的Field
            writer.Write(FishCntPerTeam);
            writer.Write(IsRunning);
            writer.Write(MsPerCycle);
            writer.Write(Name);
            writer.Write(RemainingCycles);
            writer.Write(TeamCount);
            writer.Write(TotalSeconds);
            writer.Write(IsExchangedHalfCourt);
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public virtual object Deserialize(System.IO.BinaryReader reader)
        {// 只Deserialize需要在Dss Node间传递的Field
            FishCntPerTeam = reader.ReadInt32();
            IsRunning = reader.ReadBoolean();
            MsPerCycle = reader.ReadInt32();
            Name = reader.ReadString();
            RemainingCycles = reader.ReadInt32();
            TeamCount = reader.ReadInt32();
            TotalSeconds = reader.ReadInt32();
            IsExchangedHalfCourt = reader.ReadBoolean();

            return this;
        }
        #endregion
    }

    /// <summary>
    /// 与使命相关各对象的引用
    /// </summary>
    public class MyMission
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private MyMission() { }
        private static MyMission instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static MyMission Instance()
        {
            if (instance == null)
            {
                instance = new MyMission();
            }
            return instance;
        }
        #endregion

        #region 指向各种具体使命类型的以Mission基类成员存在的引用变量
        /// <summary>
        /// 通用使命对象引用变量，指向当前选中的具体使命类型对象
        /// </summary>
        public Mission MissionRef;
        
        /// <summary>
        /// 使命接口引用变量，用于调用各具体比赛类型的使命类实例中的方法
        /// </summary>
        public IMission IMissionRef;

        /// <summary>
        /// 仿真使命公共参数引用变量
        /// 其值通过使命接口引用变量IMissionRef调用GetMissionCommonPara()方法获得
        /// </summary>
        public MissionCommonPara ParasRef;

        /// <summary>
        /// 仿真使命参与队伍列表引用变量，其中的RoboFish成员指向具体的仿真机器鱼对象
        /// 其值通过使命接口引用变量IMissionRef调用GetTeams()方法获得
        /// </summary>
        public List<Team<RoboFish>> TeamsRef;

        /// <summary>
        /// 仿真环境引用变量，其值通过使命接口引用变量IMissionRef调用GetEnvironment()方法获得
        /// </summary>
        public SimEnvironment EnvRef;

        /// <summary>
        /// 当前仿真周期决策结果数组，_decisions[i, j]表示第i支队伍第j条仿真机器鱼的决策结果
        /// 其值通过使命接口引用变量IMissionRef调用GetDecision()方法获得
        /// </summary>
        public Decision[,] DecisionRef;
        #endregion

        #region 需要在Sim2DSvr和ServerControlBoard类共享的变量
        /// <summary>
        /// 指示服务端运行模式，true表示运行于Remote模式，false表示运行于Local模式
        /// 该值为true时
        /// </summary>
        public bool IsRomteMode = true;

        ///// <summary>
        ///// 当前仿真周期比赛场景包括环境及各种静态和动态对象绘制的目标位图对象引用
        ///// 每次Mission.Draw都创建新的Bitmap对象，CLR回收内存有滞后
        ///// 导致内存消耗量呈波浪线上升下降，最大可达1.3G
        ///// 使用程序全局可见的MyMission.Bmp来引用Mission.Draw方法中创建的Bitmap对象
        ///// 则可在每周期创建新对象前先销毁前一周期创建的对象
        ///// </summary>
        //public Bitmap Bmp;
        #endregion
    }
}