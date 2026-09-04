//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: StrategyHelper.cs
// Date: 20111122  Author: ZhangBo  Version: 1
// Description: 位姿到位姿算法和带球算法文件
// Histroy:
// Date:   Author: 
// Modification: 
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using xna = Microsoft.Xna.Framework;
using URWPGSim2D.Common;
using URWPGSim2D.Core;

namespace URWPGSim2D.StrategyHelper
{
    public static class Helpers
    {
        #region dribble算法
        /// <summary>
        /// dirbble带球算法 Modified By Zhangbo 2011.10.22
        /// </summary>
        /// <param name="decision">每周期机器鱼执行策略，包含速度档位，转弯档位。</param>
        /// <param name="fish">目标仿真机器鱼参数，包含当前位置、速度信息。</param>
        /// <param name="destPtMm">目标点。</param>
        /// <param name="destDirRad">目标方向。</param>
        /// <param name="angleTheta1">鱼体方向与目标方向角度差的阈值一。
        ///   角度差在此阈值范围内，则赋给机器鱼一个合理的速度档位（见参数disThreshold说明）。</param>
        /// <param name="angleTheta2">鱼体方向与目标方向角度差的阈值二。角度差小于此阈值，则机器鱼直线游动；
        /// 角度差大于此阈值，则机器鱼调整游动方向。</param>
        /// <param name="disThreshold">距离阈值。距离大于此阈值，机器鱼以速度档位VCode1游动；
        /// 距离小于此阈值，机器鱼以速度档位VCode2游动。</param>
        /// /// <param name="VCode1">直游档位1（默认6档）。</param>
        /// /// <param name="VCode2">直游档位2（默认4档）。</param>
        /// <param name="cycles">速度和转弯档位之间切换所需周期数经验值。建议取值范围在5-20之间。此参数作用是防止机器鱼“转过”。</param>
        /// <param name="msPerCycle">每个仿真周期的毫秒数，传递固定参数，不能修改。</param>
        /// <param name="flag">机器鱼坐标选择标准，true为PositionMm，即鱼体绘图中心；false为PolygonVertices[0]，即鱼头点。</param>
        public static void Dribble(ref Decision decision, RoboFish fish, xna.Vector3 destPtMm, float destDirRad,
            float angleTheta1, float angleTheta2, float disThreshold, int VCode1, int VCode2, int cycles, int msPerCycle, bool flag)
        {
            // 调节所用周期数及每周期毫秒数转换得到秒数
            double seconds1 = 15 * msPerCycle / 1000.0;
            double seconds2 = cycles * msPerCycle / 1000.0;
            // 标志量为true则起始点为PositionMm即鱼体绘图中心false则起始点为PolygonVertices[0]即鱼头点（起始点）
            xna.Vector3 srcPtMm = (flag == true) ? fish.PositionMm : fish.PolygonVertices[0];
            // 起始点到目标点的距离（目标距离）
            double disSrcPtMmToDestPtMm = Math.Sqrt(Math.Pow(destPtMm.X - srcPtMm.X, 2.0)
                + Math.Pow(destPtMm.Z - srcPtMm.Z, 2.0));

            // 鱼体绘图中心指向目标点向量方向的弧度值（中间方向）
            double dirFishToDestPtRad = xna.MathHelper.ToRadians((float)GetAngleDegree(destPtMm - fish.PositionMm));
            if (disSrcPtMmToDestPtMm < 30)
            {// 起始点到目标点距离小于阈值（默认58毫米）将中间方向调为目标方向
                dirFishToDestPtRad = destDirRad;
            }

            // 中间方向与鱼体方向的差值（目标角度）
            double deltaTheta = dirFishToDestPtRad - fish.BodyDirectionRad;
            // 将目标角度规范化到(-PI,PI]
            // 规范化之后目标角度为正表示目标方向在鱼体方向右边
            // 规范化之后目标角度为负表示目标方向在鱼体方向左边
            if (deltaTheta > Math.PI)
            {// 中间方向为正鱼体方向为负才可能目标角度大于PI
                deltaTheta -= 2 * Math.PI;  // 规范化到(-PI,0)
            }
            else if (deltaTheta < -Math.PI)
            {// 中间方向为负鱼体方向为正才可能目标角度小于-PI
                deltaTheta += 2 * Math.PI;  // 规范化到(0,PI)
            }

            // 最大角速度取左转和右转最大角速度绝对值的均值
            float maxAngularV = (Math.Abs(DataBasedOnExperiment.TCodeAndAngularVelocityTable[0])
                + Math.Abs(DataBasedOnExperiment.TCodeAndAngularVelocityTable[14])) / 2;
            // 以最大角速度转过目标角度所需的预计时间（角度预计时间）
            double estimatedTimeByAngle = Math.Abs((double)(deltaTheta / maxAngularV));
            // 以角度预计时间游过目标距离所需平均速度值（目标速度）
            double targetVelocity = disSrcPtMmToDestPtMm / estimatedTimeByAngle;

            int code = 1;   // 目标（速度）档位初值置1
            while ((code < 10) && (DataBasedOnExperiment.VCodeAndVelocityTable[code] < targetVelocity))
            {// 目标（速度）档位对应的速度值尚未达到目标速度则调高目标（速度）档位
                code++;
            }
            decision.VCode = code;
            if (Math.Abs(deltaTheta) > angleTheta2 * Math.PI / 180.0)
            {// 目标角度绝对值超过某一阈值，速度档位置次低进行小半径转弯
                decision.VCode = 1;
            }
            else if (Math.Abs(deltaTheta) < angleTheta1 * Math.PI / 180.0)
            {// 目标角度绝对值小于某一阈值，若此时距离较远速度档置较高高全速前进，否则置适中档位前进。
                if (disSrcPtMmToDestPtMm > disThreshold)
                {
                    decision.VCode = VCode1;
                }
                else
                {
                    decision.VCode = VCode2;
                }
            }

            // 以最大速度游过目标距离所需的预计时间（距离预计时间）
            double estimatedTimeByDistance = disSrcPtMmToDestPtMm / DataBasedOnExperiment.VCodeAndVelocityTable[14];
            if (estimatedTimeByDistance > seconds1)
            {// 距离预计时间超过一次档位切换所需平均时间则取为该时间（默认为1秒）
                estimatedTimeByDistance = seconds1;
            }
            // 以距离预计时间游过目标角度所需平均角速度（目标角速度）
            double targetAngularV = deltaTheta / estimatedTimeByDistance;

            code = 7;
            if (deltaTheta <= 0)
            {// 目标角度为负目标方向在鱼体方向左边需要给左转档位（注意左转档位对应的角速度值为负值）
                while ((code > 0) && (DataBasedOnExperiment.TCodeAndAngularVelocityTable[code] > targetAngularV))
                {// 目标（转弯）档位对应的角速度值尚未达到目标角速度则调低目标（转弯）档位
                    code--;
                }
                if ((fish.AngularVelocityRadPs * seconds2) < deltaTheta)
                {// 当前角速度值绝对值过大 一次档位切换所需平均时间内能游过的角度超过目标角度
                    // 则给相反方向次大转弯档位
                    code = 12;
                }
            }
            else
            {// 目标角度为正目标方向在鱼体方向右边需要给右转档位
                while ((code < 14) && (DataBasedOnExperiment.TCodeAndAngularVelocityTable[code] < targetAngularV))
                {// 目标（转弯）档位对应的角速度值尚未达到目标角速度则调高目标（转弯）档位
                    code++;
                }
                if ((fish.AngularVelocityRadPs * seconds2) > deltaTheta)
                {// 当前角速度值绝对值过大 一次档位切换所需平均时间内能游过的角度超过目标角度
                    // 则给相反方向次大转弯档位
                    code = 2;
                }
            }
            decision.TCode = code;
        }
        #endregion

        #region PoseToPose镇定算法
        /// <summary>
        /// 仿真机器鱼位姿到位姿镇定算法（位置坐标和方向弧度值） Modified by Zhangbo20111020
        /// 场地坐标系定义为：X向右，Y向下，负X轴顺时针转回负X轴角度范围为(-PI,PI)的坐标系
        /// </summary>
        /// <param name="decision">决策变量 输出参数 会被修改</param>
        /// <param name="fish">目标仿真机器鱼（其PositionMm/PolygonVertices[0]和BodyDirectionRad参数为起始位姿）</param>
        /// <param name="destPtMm">目标位置坐标（目标点）</param>
        /// <param name="destDirRad">目标方向弧度值（目标方向）</param>
        /// <param name="angThreshold">关键调节参数（中间方向与鱼体方向度数差值绝对值）上限，默认30度</param>
        /// <param name="disThreshold">关键调节参数（临时目标点与最终目标点距离）阈值</param>
        /// <param name="msPerCycle">每个仿真周期的毫秒数，默认取100</param>
        public static void PoseToPose(ref Decision decision, RoboFish fish, xna.Vector3 destPtMm, float destDirRad,
            float angThreshold, float disThreshold, int msPerCycle, ref int times)
        {
            // 标志量为true则起始点为PositionMm即鱼体绘图中心false则起始点为PolygonVertices[0]即鱼头点（起始点）
            xna.Vector3 srcPtMm = fish.PositionMm;
            // 起始点到目标点的距离（目标距离）
            double disSrcPtMmToDestPtMm = Math.Sqrt(Math.Pow(destPtMm.X - srcPtMm.X, 2.0)
                + Math.Pow(destPtMm.Z - srcPtMm.Z, 2.0));

            // 沿目标方向的反方向偏离目标点阈值距离的临时目标点（临时目标点）
            xna.Vector3 tmpPtMm = new xna.Vector3((float)(destPtMm.X - disThreshold * Math.Cos(destDirRad)),
                0, (float)(destPtMm.Z - disThreshold * Math.Sin(destDirRad)));

            double disSrcPtMmToTmpPtMm = Math.Sqrt(Math.Pow(tmpPtMm.X - srcPtMm.X, 2.0)
                + Math.Pow(tmpPtMm.Z - srcPtMm.Z, 2.0));

            // 镇定阶段标志 1为远距离第一阶段2为近距离第二阶段
            int phrase = 2;
            if (disSrcPtMmToTmpPtMm > 0.2 * disThreshold && times == 0)
            {// 起始点到临时目标点的距离大于阈值的20%，则认为尚未镇定到临时目标点，需要把目标点修正成临时目标点
                destPtMm = tmpPtMm;
                phrase = 1;
            }

            // 鱼体绘图中心指向目标点向量方向的弧度值（中间方向）
            double dirFishToDestPtRad = xna.MathHelper.ToRadians((float)GetAngleDegree(destPtMm - fish.PositionMm));

            // 中间方向与鱼体方向的差值（目标角度）
            double deltaTheta = dirFishToDestPtRad - fish.BodyDirectionRad;
            // 将目标角度规范化到(-PI,PI]
            // 规范化之后目标角度为正，表示目标方向在鱼体方向右边
            // 规范化之后目标角度为负，表示目标方向在鱼体方向左边
            if (deltaTheta > Math.PI)
            {// 中间方向为正鱼体方向为负才可能目标角度大于PI
                deltaTheta -= 2 * Math.PI;  // 规范化到(-PI,0)
            }
            else if (deltaTheta < -Math.PI)
            {// 中间方向为负鱼体方向为正才可能目标角度小于-PI
                deltaTheta += 2 * Math.PI;  // 规范化到(0,PI)
            }

            if (Math.Abs(deltaTheta) > angThreshold * Math.PI / 180.0)
            {// 目标角度绝对值超过某一阈值（默认30度）速度档位置次低进行小半径转弯。防止控制率过大。
                decision.VCode = 1;
                decision.TCode = (deltaTheta <= 0) ? 1 : 13;
            }
            else
            {
                if (phrase == 1)
                {// 第一阶段（在阈值区域之外）镇定算法
                    times = 0;

                    //decision.VCode = (disSrcPtMmToTmpPtMm < 0.5 * disThreshold) ? 4 : 10;
                    decision.VCode = 8;
                    decision.TCode = 7;
                    float lamdadot = ((destPtMm.X - srcPtMm.X) * (fish.VelocityMmPs * (float)Math.Sin(fish.BodyDirectionRad))
                        - (-destPtMm.Z + srcPtMm.Z) * (fish.VelocityMmPs * (float)Math.Cos(fish.BodyDirectionRad)))
                        / ((float)Math.Pow(destPtMm.X - srcPtMm.X, 2.0) + (float)Math.Pow(destPtMm.Z - srcPtMm.Z, 2.0));
                    double targetAngularV = 50 * lamdadot;
                    if (deltaTheta <= 0)
                    {// 目标角度为负目标方向在鱼体方向左边需要给左转档位（注意左转档位对应的角速度值为负值）
                        while ((decision.TCode > 0) && (DataBasedOnExperiment.TCodeAndAngularVelocityTable[decision.TCode] > targetAngularV))
                        {// 目标（转弯）档位对应的角速度值尚未达到目标角速度则调低目标（转弯）档位
                            decision.TCode--;
                        }
                    }
                    else
                    {// 目标角度为正目标方向在鱼体方向右边需要给右转档位
                        while ((decision.TCode < 14) && (DataBasedOnExperiment.TCodeAndAngularVelocityTable[decision.TCode] < targetAngularV))
                        {// 目标（转弯）档位对应的角速度值尚未达到目标角速度则调高目标（转弯）档位
                            decision.TCode++;
                        }
                    }
                }
                else
                {// 第二阶段（进入阈值区域）镇定算法
                    times++;
                    float thetae = destDirRad - fish.BodyDirectionRad;
                    const float K1 = 0.6f;
                    const float K2 = 12.0f;
                    const float K3 = 18.0f;
                    xna.Vector3 srcPtMmLocal = new xna.Vector3(0, 0, 0);
                    UrwpgSimHelper.CoordinateTransformation(destDirRad, destPtMm, ref srcPtMmLocal, srcPtMm);
                    float u1 = -K1 * srcPtMmLocal.X * (float)Math.Pow(Math.Sin(times * msPerCycle / 1000.0f), 2.0);
                    float u2 = u1 * K2 * srcPtMmLocal.Z + u1 * K3 * (float)Math.Tan(thetae);
                    double targetVelocity = u1 / Math.Cos(thetae);
                    double targetAngularV = u2 * Math.Pow(Math.Cos(thetae), 2.0);

                    if (disSrcPtMmToDestPtMm < 150.0f && Math.Abs(deltaTheta) < 10.0f * Math.PI / 180.0f)
                    {
                        decision.VCode = 0;
                        decision.TCode = 7;
                    }
                    else
                    {
                        decision.VCode = 2;
                        while ((decision.VCode < 14) && (DataBasedOnExperiment.VCodeAndVelocityTable[decision.VCode] < targetVelocity))
                        {// 目标（速度）档位对应的速度值尚未达到目标速度则调高目标（速度）档位
                            decision.VCode++;
                        }
                        decision.TCode = 7;
                        if (deltaTheta <= 0)
                        {// 目标角度为负目标方向在鱼体方向左边需要给左转档位（注意左转档位对应的角速度值为负值）
                            while ((decision.TCode > 0) && (DataBasedOnExperiment.TCodeAndAngularVelocityTable[decision.TCode] > targetAngularV))
                            {// 目标（转弯）档位对应的角速度值尚未达到目标角速度则调低目标（转弯）档位
                                decision.TCode--;
                            }
                        }
                        else
                        {// 目标角度为正目标方向在鱼体方向右边需要给右转档位
                            while ((decision.TCode < 14) && (DataBasedOnExperiment.TCodeAndAngularVelocityTable[decision.TCode] < targetAngularV))
                            {// 目标（转弯）档位对应的角速度值尚未达到目标角速度则调高目标（转弯）档位
                                decision.TCode++;
                            }
                        }
                    }
                }
            }
        }
        #endregion

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