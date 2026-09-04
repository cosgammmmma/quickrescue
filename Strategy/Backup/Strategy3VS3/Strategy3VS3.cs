using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using xna = Microsoft.Xna.Framework;
using URWPGSim2D.Common;
using URWPGSim2D.StrategyLoader;

namespace URWPGSim2D.Strategy
{
    public class Strategy : MarshalByRefObject, IStrategy
    {
        #region reserved code never be changed or removed
        /// <summary>
        /// override the InitializeLifetimeService to return null instead of a valid ILease implementation
        /// to ensure this type of remote object never dies
        /// </summary>
        /// <returns>null</returns>
        public override object InitializeLifetimeService()
        {
            //return base.InitializeLifetimeService();
            return null; // makes the object live indefinitely
        }
        #endregion

        /// <summary>
        /// 决策类当前对象对应的仿真使命参与队伍的决策数组引用 第一次调用GetDecision时分配空间
        /// </summary>
        private Decision[] decisions = null;

        /// <summary>
        /// 获取队伍名称 在此处设置参赛队伍的名称
        /// </summary>
        /// <returns>队伍名称字符串</returns>
        public string GetTeamName()
        {
            return "水球3VS3 Test Team";
        }

        /// <summary>
        /// 获取当前仿真使命（比赛项目）当前队伍所有仿真机器鱼的决策数据构成的数组
        /// </summary>
        /// <param name="mission">服务端当前运行着的仿真使命Mission对象</param>
        /// <param name="teamId">当前队伍在服务端运行着的仿真使命中所处的编号 
        /// 用于作为索引访问Mission对象的TeamsRef队伍列表中代表当前队伍的元素</param>
        /// <returns>当前队伍所有仿真机器鱼的决策数据构成的Decision数组对象</returns>
        public Decision[] GetDecision(Mission mission, int teamId)
        {
            // 决策类当前对象第一次调用GetDecision时Decision数组引用为null
            if (decisions == null)
            {// 根据决策类当前对象对应的仿真使命参与队伍仿真机器鱼的数量分配决策数组空间
                decisions = new Decision[mission.CommonPara.FishCntPerTeam];
            }

            #region 决策计算过程 需要各参赛队伍实现的部分
            #region 策略编写帮助信息
            //====================我是华丽的分割线====================//
            //======================策略编写指南======================//
            //1.策略编写工作直接目标是给当前队伍决策数组decisions各元素填充决策值
            //2.决策数据类型包括两个int成员，VCode为速度档位值，TCode为转弯档位值
            //3.VCode取值范围0-15共16个整数值，每个整数对应一个速度值，速度值整体但非严格递增
            //有个别档位值对应的速度值低于比它小的档位值对应的速度值，速度值数据来源于实验
            //4.TCode取值范围0-14共15个整数值，每个整数对应一个角速度值
            //整数7对应直游，角速度值为0，整数6-0，8-14分别对应左转和右转，偏离7越远，角度速度值越大
            //5.任意两个速度/转弯档位之间切换，都需要若干个仿真周期，才能达到稳态速度/角速度值
            //目前运动学计算过程决定稳态速度/角速度值接近但小于目标档位对应的速度/角速度值
            //6.决策类Strategy的实例在加载完毕后一直存在于内存中，可以自定义私有成员变量保存必要信息
            //====================我是华丽的分割线====================//
            //=======策略中可以使用的比赛环境信息和过程信息说明=======//
            //场地坐标系: 以毫米为单位，矩形场地中心为原点，向右为正X，向下为正Z
            //            正X轴到负X轴，顺时针为0到π，逆时针为0到-π
            //mission.CommonPara: 当前仿真使命公共参数
            //mission.CommonPara.FishCntPerTeam: 每支队伍仿真机器鱼数量
            //mission.CommonPara.MsPerCycle: 仿真周期毫秒数
            //mission.CommonPara.RemainingCycles: 当前剩余仿真周期数
            //mission.CommonPara.TeamCount: 当前仿真使命参与队伍数量
            //mission.CommonPara.TotalSeconds: 当前仿真使命运行时间秒数
            //mission.EnvRef.Balls: 
            //当前仿真使命涉及到的仿真水球列表，列表元素的成员意义参见URWPGSim2D.Common.Ball类定义中的注释
            //mission.EnvRef.Channels: 
            //当前仿真使命涉及到的仿真通道列表，列表元素的成员意义参见URWPGSim2D.Common.Channel类定义中的注释
            //mission.EnvRef.FieldInfo: 
            //当前仿真使命涉及到的仿真场地，各成员意义参见URWPGSim2D.Common.Field类定义中的注释
            //mission.EnvRef.ObstaclesRect: 
            //当前仿真使命涉及到的方形障碍物列表，列表元素的成员意义参见URWPGSim2D.Common.RectangularObstacle类定义中的注释
            //mission.EnvRef.ObstaclesRound:
            //当前仿真使命涉及到的圆形障碍物列表，列表元素的成员意义参见URWPGSim2D.Common.RoundedObstacle类定义中的注释
            //mission.TeamsRef[teamId]:
            //决策类当前对象对应的仿真使命参与队伍（当前队伍）
            //mission.TeamsRef[teamId].Para:
            //当前队伍公共参数，各成员意义参见URWPGSim2D.Common.TeamCommonPara类定义中的注释
            //mission.TeamsRef[teamId].Fishes:
            //当前队伍仿真机器鱼列表，列表元素的成员意义参见URWPGSim2D.Common.RoboFish类定义中的注释
            //mission.TeamsRef[teamId].Fishes[i].PositionMm和PolygonVertices[0],BodyDirectionRad,VelocityMmPs,
            //                                   AngularVelocityRadPs,Tactic,Collision:
            //当前队伍第i条仿真机器鱼鱼体矩形中心和鱼头顶点在场地坐标系中的位置（用到X坐标和Z坐标），鱼体方向，速度值，
            //                                   角速度值，决策值，碰撞状态值
            //====================我是华丽的分割线====================//
            //========================典型循环========================//
            //for (int i = 0; i < mission.CommonPara.FishCntPerTeam; i++)
            //{
            //    decisions[i].VCode = 0;
            //    decisions[i].TCode = 7;
            //}
            //====================我是华丽的分割线====================//
            #endregion
            //请从这里开始编写代码
            // 为简化代码书写而定义的引用变量
            Team<RoboFish> t = mission.TeamsRef[teamId];
            Ball b = mission.EnvRef.Balls[0];
            Field f = mission.EnvRef.FieldInfo;
            // 左右半场标志 左半场为1右半场为-1用于各种对称计算调节符号
            int flag = (t.Para.MyHalfCourt == HalfCourt.LEFT) ? 1 : -1;
            // 射门目标点（左右球门内边线中点往球门深度方向偏离一个仿真水球半径的点）
            xna.Vector3 goalPtMm;
            // 射门目标点指向球心的射线延长线与球边缘交点（球边缘点）
            xna.Vector3 ballEdgePtMm;
            // 左半场进攻右球门 右半场进攻左球门
            goalPtMm = new xna.Vector3(flag * (f.FieldLengthXMm / 2 - f.ForbiddenZoneLengthXMm + b.RadiusMm), 0, 0);

            // 球心指向射门目标点向量方向的弧度值（目标方向）
            float dirBallToGoalRad = xna.MathHelper.ToRadians(GetAngleDegree(goalPtMm - b.PositionMm));
            ballEdgePtMm = new xna.Vector3((float)(b.PositionMm.X - b.RadiusMm * Math.Cos(dirBallToGoalRad)),
                0, (float)(b.PositionMm.Z - b.RadiusMm * Math.Sin(dirBallToGoalRad)));

            for (int i = 0; i < mission.CommonPara.FishCntPerTeam; i++)
            {// 让所有仿真机器鱼静止
                decisions[i].TCode = 7;
                decisions[i].VCode = 0;

            }

            // 演示一下
            if (mission.TeamsRef[teamId].Para.MyHalfCourt == HalfCourt.LEFT)
            {// 在左半场以较大速度右转
                decisions[0].TCode = 8;
                decisions[0].VCode = 12;
            }
            else
            {// 在右半场时以较小速度左转
                decisions[0].TCode = 6;
                decisions[0].VCode = 3;
            }
            #endregion

            return decisions;
        }

        /// <summary>
        /// 返回Vector3类型的向量（Y置0，只有X和Z有意义）在场地坐标系中方向的角度值 LiYoubing 20110722
        /// 场地坐标系定义为：X向右，Z向下，Y置0，负X轴顺时针转回负X轴角度范围为(-PI,PI)的坐标系
        /// </summary>
        /// <param name="v">待计算角度值的xna.Vector3类型向量</param>
        /// <returns>向量v在场地坐标系中方向的角度值</returns>
        public static float GetAngleDegree(xna.Vector3 v)
        {
            float x = v.X;
            float y = v.Z;
            float angle = 0;

            if (Math.Abs(x) < float.Epsilon)
            {// x = 0 直角反正切不存在
                if (Math.Abs(y) < float.Epsilon) { angle = 0.0f; }
                else if (y > 0) { angle = 90.0f; }
                else if (y < 0) { angle = -90.0f; }
            }
            else if (x < 0)
            {// x < 0 (90,180]或(-180,-90)
                if (y >= 0) { angle = (float)(180 * Math.Atan(y / x) / Math.PI) + 180.0f; }
                else { angle = (float)(180 * Math.Atan(y / x) / Math.PI) - 180.0f; }
            }
            else
            {// x > 0 (-90,90)
                angle = (float)(180 * Math.Atan(y / x) / Math.PI);
            }

            return angle;
        }
    }
}