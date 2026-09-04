namespace URWPGSim2D.Strategy
{
    using System;
    using System.IO;
    using URWPGSim2D.Common;
    public class BaseAction
    {
        //******************************************************************
        // 新增：α-β 滤波器（卡尔曼固定增益特例）球状态估计字段，用于抗测量噪声的轨迹预测
        private double[] filterBallPx = new double[9];
        private double[] filterBallPz = new double[9];
        private double[] filterBallVx = new double[9];
        private double[] filterBallVz = new double[9];
        private bool[] filterBallInit = new bool[9];
        private int[] stalemateCount = new int[2];   // 相持连续帧计数（防瞬态误判）
        private int[] ramCount = new int[2];          // 冲撞持续帧计数（防追着对方鱼跑）
        private int[] lockBallId = new int[2];        // 横扫/带球锁球状态（防止扫球途中换球）
        private int[] sweepOrbitCount = new int[2];   // 横扫绕位帧计数（超时回退直接带球，防围球转圈）

        /// <summary>
        /// 计算到指定位置的安全速度（不会冲过的最佳速度）。
        /// 物理原理：制动距离公式 d_brake = v²/(2a)，要求 v² ≤ 2·a·d，即 v ≤ sqrt(2·a·d)。
        /// 仿真鱼实际减速度约 500mm/s²（原 200 估低导致速度被压、抢球比对手慢），最高速 314mm/s。
        /// 还要考虑当前速度：若已超 v_safe，给最低速让其尽快减速。
        /// </summary>
        public double SafeApproachSpeed(int fishId, double targetX, double targetZ)
        {
            double fx = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
            double fz = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;
            double v0 = GlobalVar.MyTeam.Fishes[fishId].VelocityMmPs;
            double d = Compute_Distance(fx, fz, targetX, targetZ);
            const double a = 500.0;   // 减速度 mm/s²（实测调高，原 200 估低）
            double vSafe = Math.Sqrt(2.0 * a * d);
            if (vSafe > 314.0) vSafe = 314.0;
            if (vSafe < 80.0) vSafe = 80.0;   // 下限 80（原 30 太低，贴近球时还在微动反而碰不到球）
            // 当前速度已超过安全速度 → 给最低速强制减速（避免冲过）
            if (v0 > vSafe + 50.0)
            {
                return 80.0;
            }
            return vSafe;
        }

        /// <summary>
        /// Bang-Bang 时间最优控制：到达指定位置的最快方案。
        /// 理论：加速段(VCode=15) → 巡航段(VCode=15) → 减速段(v≤sqrt(2·a·d))。
        /// 与 Position 的区别：Position 用 0.9·v 衰减模型（减速度仅 0.1·v≈31mm/s²），
        /// 严重高估制动距离导致过早降档、起步被压速。本方法用实测减速度 a=500mm/s²。
        /// 边转边走：方向偏差大时降低 VCode 但非零（保留运动维持转向力）。
        /// </summary>
        public void FastMoveTo(int fishId, double tx, double tz)
        {
            double fx = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
            double fz = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;
            double fishAng = GlobalVar.MyTeam.Fishes[fishId].BodyDirectionRad;
            double v0 = GlobalVar.MyTeam.Fishes[fishId].VelocityMmPs;

            double d = Compute_Distance(fx, fz, tx, tz);
            double angToTarget = Math.Atan2(tz - fz, tx - fx);
            double angErr = this.FormatAngleRad(angToTarget - fishAng);
            double absAng = Math.Abs(angErr);

            const double aMax = 500.0;   // 实测减速度 mm/s²
            double dBrake = (v0 * v0) / (2.0 * aMax);   // 当前速度制动距离

            // === 速度决策（Bang-Bang）===
            int vCode;
            if (d > dBrake + 50.0)   // 距离足够，可以保持/加速
            {
                if (absAng < (GlobalVar.PI / 4.0))       // 方向基本对 → 全速冲（最大加速度起步）
                    vCode = 15;
                else if (absAng < (GlobalVar.PI / 2.0))  // 中等偏差 → 中高速（边转边走）
                    vCode = 12;
                else                                       // 大偏差 → 低速但保持运动
                    vCode = 8;
            }
            else   // 距离不够，必须减速
            {
                double vSafe = Math.Sqrt(2.0 * aMax * d);
                if (vSafe > 314.0) vSafe = 314.0;
                if (vSafe < 80.0) vSafe = 80.0;
                // 查 vTable 找最接近档
                vCode = 7;
                double bestE = 99999.0;
                double[] vTable = GlobalVar.vTable;
                for (int i = 0; i < vTable.Length; i++)
                {
                    double e = Math.Abs(vTable[i] - vSafe);
                    if (e < bestE) { bestE = e; vCode = i; }
                }
            }

            // === 转向决策（比例控制 + 档位量化）===
            int tCode;
            if (absAng > (GlobalVar.PI / 2.0))        // >90°：最大转向
                tCode = (angErr > 0) ? 0 : 14;          // tTable[0]=-0.3552(左), [14]=0.3438(右)
            else if (absAng > (GlobalVar.PI / 6.0))    // >30°：中等转向
                tCode = (angErr > 0) ? 3 : 11;
            else if (absAng > 0.1)                      // >5°：小幅转向
                tCode = (angErr > 0) ? 5 : 9;
            else                                       // 对齐：直行
                tCode = 7;

            GlobalVar.decisions[fishId].VCode = vCode;
            GlobalVar.decisions[fishId].TCode = tCode;
        }

        //******************************************************************
        public bool equal(double a1, double a2)
        {
            return (Math.Abs((double)(a1 - a2)) < 1E-06);
        }

        public double FormatAngle(double ang)
        {
            if (ang > 180.0)
            {
                return (ang - 360.0);
            }
            if (ang <= -180.0)
            {
                return (ang + 360.0);
            }
            return ang;
        }
        public double FormatAngleRad(double ang)
        {
            if (ang > GlobalVar.PI)
            {
                return (ang - (2.0 * GlobalVar.PI));
            }
            if (ang <= -GlobalVar.PI)
            {
                return (ang + (2.0 * GlobalVar.PI));
            }
            return ang;
        }
        public double GetAngle(double point1_x, double point1_z, double point2_x, double point2_z)
        {
            return ((Math.Atan2(point2_z - point1_z, point2_x - point1_x) / GlobalVar.PI) * 180.0);
        }
        public double GetAngleRad(double point1_x, double point1_z, double point2_x, double point2_z)
        {
            return Math.Atan2(point2_z - point1_z, point2_x - point1_x);
        }
        public Platform GetLineAcrossPoint(double x1, double y1, double ang1, double x2, double y2, double ang2)
        {
            Platform pointd = new Platform();
            double pI = GlobalVar.PI;
            if (((((x1 > 2200.0) || (x1 < -2200.0)) || ((y1 > 1600.0) || (y1 < -1600.0))) || (((x2 > 2200.0) || (x2 < -2200.0)) || (y2 > 1600.0))) || (y2 < -1600.0))
            {
                pointd.Posx = 10000.0;
                pointd.Posz = 10000.0;
                return pointd;
            }
            if (this.equal(x1, x2) && this.equal(y1, y2))
            {
                pointd.Posx = 0.0;
                pointd.Posz = 0.0;
                return pointd;
            }
            while (ang1 < -180.0)
            {
                ang1 += 360.0;
            }
            while (ang1 > 180.0)
            {
                ang1 -= 360.0;
            }
            while (ang2 < -180.0)
            {
                ang2 += 360.0;
            }
            while (ang2 > 180.0)
            {
                ang2 -= 360.0;
            }
            if (ang1 < 0.0)
            {
                ang1 += 180.0;
            }
            if (ang2 < 0.0)
            {
                ang2 += 180.0;
            }
            if (this.equal(ang2, ang1))
            {
                if (this.equal(x1, x2))
                {
                    if (this.equal(ang1, 90.0))
                    {
                        pointd.Posx = 0.0;
                        pointd.Posz = 0.0;
                    }
                    else
                    {
                        pointd.Posx = 10000.0;
                        pointd.Posz = 10000.0;
                    }
                    return pointd;
                }
                if (this.equal(Math.Tan((ang1 * pI) / 180.0), (y2 - y1) / (x2 - x1)))
                {
                    pointd.Posx = 0.0;
                    pointd.Posz = 0.0;
                }
                else
                {
                    pointd.Posx = 10000.0;
                    pointd.Posz = 10000.0;
                }
                return pointd;
            }
            if (this.equal(90.0, ang1))
            {
                pointd.Posx = x1;
                double num2 = Math.Tan((ang2 * pI) / 180.0);
                pointd.Posz = (num2 * x1) + (y2 - (num2 * x2));
            }
            else if (this.equal(90.0, ang2))
            {
                pointd.Posx = x2;
                double num3 = Math.Tan((ang1 * pI) / 180.0);
                pointd.Posz = (num3 * x1) + (y1 - (num3 * x2));
            }
            else
            {
                double num4 = Math.Tan((ang1 * pI) / 180.0);
                double num5 = Math.Tan((ang2 * pI) / 180.0);
                pointd.Posx = ((y1 - (num4 * x1)) - (y2 - (num5 * x2))) / (num5 - num4);
                pointd.Posz = (num4 * pointd.Posx) + (y1 - (num4 * x1));
            }
            return pointd;
        }
        public bool IsAngleDuring(double ang11, double ang22, double ang33)
        {
            double num = this.FormatAngle(ang11);
            double num2 = this.FormatAngle(ang22);
            double num3 = this.FormatAngle(ang33);
            if ((((Math.Abs(num) + Math.Abs(num2)) < 180.0) || ((num > 0.0) && (num2 > 0.0))) || ((num < 0.0) && (num2 < 0.0)))
            {
                return (((num > num3) && (num2 < num3)) || ((num < num3) && (num2 > num3)));
            }
            return (((num3 > num) && (num3 > num2)) || ((num3 < num) && (num3 < num2)));
        }

        //******************************************************************
        private int boundary_dribbleact_constdir = 0;
        private int current_w = 7;
        private int previous_w = 7;

        public double x;
        public double z;

        const double pi = 3.14;
        public void StayForDecision(int whichfish)
        {
            GlobalVar.decisions[whichfish].TCode = 7;
            GlobalVar.decisions[whichfish].VCode = 0;
        }

        public void Set_Decisions(int fishIndex,int a,int b)
        {
            GlobalVar.decisions[fishIndex].VCode = a;
            GlobalVar.decisions[fishIndex].TCode = b;
        }

        public double Compute_Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2.0) + Math.Pow(y2 - y1, 2.0));
        }

        public int CheckDuqiumenStatus()
        {
            bool fish1InZone = (Math.Abs(GlobalVar.OppTeam.Fishes[1].PositionMm.Z) < 800f) &&
                               (Math.Abs(GlobalVar.OppTeam.Fishes[1].PositionMm.Z) > 150f) &&
                               (GlobalVar.OppTeam.Fishes[1].PositionMm.X < -1000f) &&
                               (GlobalVar.OppTeam.Fishes[1].PositionMm.X > -1500f);

            bool fish0InZone = (Math.Abs(GlobalVar.OppTeam.Fishes[0].PositionMm.Z) < 800f) &&
                               (Math.Abs(GlobalVar.OppTeam.Fishes[0].PositionMm.Z) > 150f) &&
                               (GlobalVar.OppTeam.Fishes[0].PositionMm.X < -1000f) &&
                               (GlobalVar.OppTeam.Fishes[0].PositionMm.X > -1500f);

            return (fish1InZone && fish0InZone) ? 1 : 0;
        }

        public bool IsInGoal(double x, double z, int num)
        {
            int upperFishIndex = this.GetUpperFish();
            double upperFishZ = GlobalVar.OppTeam.Fishes[upperFishIndex].PositionMm.Z;
            double lowerFishZ = GlobalVar.OppTeam.Fishes[1 - upperFishIndex].PositionMm.Z;

            return (z > upperFishZ) && (z < lowerFishZ) && (x < -930.0) && (x > -1500.0);
        }

        public bool IsInMyUnArea(double locationX, double locationZ)
        {
            bool isInRestrictedZone = (Math.Abs(locationZ) < 320.0) && (locationX < -952.0);
            return isInRestrictedZone || this.IsInHome(locationX, locationZ);
        }

        public bool IsInMyUnArea_01(double locationX)
        {
            return locationX < -952.0;
        }

        public bool IsInMyUnArea_02(double locationX, double locationZ)
        {
            return (Math.Abs(locationZ) < 508.0) && (locationX < -952.0);
        }

        public bool IsInHome(double locationX, double locationZ)
        {
            return (Math.Abs(locationZ) < 508.0) && (locationX < -984.0) && (locationX > -1090.0);
        }

        public bool IsInOppUnArea_01(double locationX, double locationZ)
        {
            return locationX > 952.0;
        }

        public bool IsInOppHome_Area(double locationX, double locationZ)
        {
            return (Math.Abs(locationZ) < 507.0) && (locationX > 984.0) && (locationX < 1090.0);
        }

        public int GetCornerSelection(double posX, double posZ)
        {
            if (posX > 949.0 && posZ > 670.0)
            {
                return 2;
            }
            if (posX > 949.0 && posZ < -670.0)
            {
                return 1;
            }
            return 0;
        }

        public int CheckCornerInterferenceUp()
        {
            int score = 0;

            for (int i = 0; i < 9; i++)
            {
                if (GlobalVar.balls[i].PositionMm.X > 949f && GlobalVar.balls[i].PositionMm.Z < -670f)
                {
                    if (i <= 2)
                    {
                        score += 3;
                    }
                    else if (i <= 6)
                    {
                        score += 1;
                    }
                    else
                    {
                        score += 2;
                    }
                }
            }

            return score;
        }

        public int CheckCornerInterferenceDown()
        {
            int score = 0;
            for (int i = 0; i <= 2; i++)
            {
                if ((GlobalVar.balls[i].PositionMm.X > 949f) && (GlobalVar.balls[i].PositionMm.Z > 670f))
                {
                    score += 3;
                }
            }
            for (int j = 3; j <= 6; j++)
            {
                if ((GlobalVar.balls[j].PositionMm.X > 949f) && (GlobalVar.balls[j].PositionMm.Z > 670f))
                {
                    score++;
                }
            }
            for (int k = 7; k <= 8; k++)
            {
                if ((GlobalVar.balls[k].PositionMm.X > 949f) && (GlobalVar.balls[k].PositionMm.Z > 670f))
                {
                    score += 2;
                }
            }
            return score;
        }

        public int GetUpperFish()
        {
            return GlobalVar.OppTeam.Fishes[0].PositionMm.Z < GlobalVar.OppTeam.Fishes[1].PositionMm.Z ? 0 : 1;
        }
        //*********************************(谨慎修改)
        public void Angle(int which, double drad)
        {
            double x = GlobalVar.MyTeam.Fishes[which].PositionMm.X;
            double z = GlobalVar.MyTeam.Fishes[which].PositionMm.Z;
            double velocityMmPs = GlobalVar.MyTeam.Fishes[which].VelocityMmPs;
            double angularVelocityRadPs = GlobalVar.MyTeam.Fishes[which].AngularVelocityRadPs;
            double velocityDirectionRad = GlobalVar.MyTeam.Fishes[which].VelocityDirectionRad;
            double[] tTable = GlobalVar.tTable;
            double num7 = drad;
            double num8 = x + ((velocityMmPs * Math.Cos(velocityDirectionRad)) * 0.1);
            double num9 = z + ((velocityMmPs * Math.Sin(velocityDirectionRad)) * 0.1);
            double num10 = velocityDirectionRad + (angularVelocityRadPs * 0.1);
            double num11 = 0.0;
            double num12 = 0.0;
            double num13 = 0.0;
            int index = 0;
            num11 = num7 - num10;
            if (num11 > pi)
            {
                num11 -= 2.0 * pi;
            }
            if (num11 < -pi)
            {
                num11 += 2.0 * pi;
            }
            if (num11 > 0.0)
            {
                for (index = 15; index > 0; index--)
                {
                    num12 = (angularVelocityRadPs * 0.9) + (tTable[index] * 0.1);
                    if ((angularVelocityRadPs < 0.0) && (num12 < 0.0))
                    {
                        break;
                    }
                    num13 = 0.0;
                    if (num12 <= 0.0)
                    {
                        break;
                    }
                    while (num12 > 0.0)
                    {
                        num13 += num12;
                        num12 = (0.9 * num12) + (tTable[0] * 0.1);
                    }
                    if ((num13 * 0.1) <= num11)
                    {
                        break;
                    }
                }
            }
            else
            {
                index = 0;
                while (index < 15)
                {
                    num12 = (angularVelocityRadPs * 0.9) + (tTable[index] * 0.1);
                    if ((angularVelocityRadPs > 0.0) && (num12 >= 0.0))
                    {
                        break;
                    }
                    num13 = 0.0;
                    if (num12 > 0.0)
                    {
                        break;
                    }
                    while (num12 < 0.0)
                    {
                        num13 += num12;
                        num12 = (0.9 * num12) + (tTable[15] * 0.1);
                    }
                    if ((num13 * 0.1) >= num11)
                    {
                        break;
                    }
                    index++;
                }
            }
            GlobalVar.decisions[which].TCode = index;
        }
        //******************************************************************
        public void HandleBallAction(int whichFish)
        {
            bool isInterferingDown = GlobalVar.BaAction.CheckCornerInterferenceDown() > 0 || GlobalVar.interfereBallStage_Down[whichFish] == 1;
            bool isInterferingUp = GlobalVar.BaAction.CheckCornerInterferenceUp() > 0 || GlobalVar.interfereBallStage_Up[whichFish] == 1;
            bool isDribbleAction = GlobalVar.BaAction.DribbleActJudge() == 1;
            int oppositeFish = 1 - whichFish;

            if (whichFish == 0)
            {
                if (isInterferingDown)
                {
                    GlobalVar.BaAction.HandleCornerInterfereBall(0, 0);
                    GlobalVar.boundary_dribbleact[0] = 0;
                }
                else if (isDribbleAction && GlobalVar.boundary_dribbleact[1] != 1)
                {
                    GlobalVar.BaAction.boundary_dribbleact(0);
                    GlobalVar.boundary_dribbleact[0] = 1;
                }
                else if (isInterferingUp && GlobalVar.Bringing_BallID[1] != -1)
                {
                    GlobalVar.BaAction.HandleCornerInterfereBall(0, 1);
                    GlobalVar.boundary_dribbleact[0] = 0;
                }
                else
                {
                    GlobalVar.boundary_dribbleact[0] = 0;
                    GlobalVar.BaAction.QuickInterfereBall(0);
                }
            }
            else
            {
                if (isInterferingUp)
                {
                    GlobalVar.BaAction.HandleCornerInterfereBall(1, 1);
                    GlobalVar.boundary_dribbleact[1] = 0;
                }
                else if (isDribbleAction && GlobalVar.boundary_dribbleact[0] == 0)
                {
                    GlobalVar.BaAction.boundary_dribbleact(1);
                    GlobalVar.boundary_dribbleact[1] = 1;
                }
                else if (isInterferingDown && GlobalVar.Bringing_BallID[0] != -1)
                {
                    GlobalVar.BaAction.HandleCornerInterfereBall(1, 0);
                    GlobalVar.boundary_dribbleact[1] = 0;
                }
                else
                {
                    GlobalVar.boundary_dribbleact[1] = 0;
                    GlobalVar.BaAction.QuickInterfereBall(1);
                }
            }
        }

        public bool IsInOppForbiddenArea(double locationX, double locationZ)
        {
            return locationX > 952.0;
        }

        public bool IsInCornerChoseBallArea(double locationX, double locationZ)
        {
            return (locationX > 949.0) && (Math.Abs(locationZ) <= 560.0);
        }

        //*********************************(谨慎修改)
        public int ChoseBall_for_duqiumen(int fishID)
        {
            int index = GlobalVar.Bringing_BallID[fishID];
            for (int i = 0; i < 9; i++)
            {
                if ((((Math.Abs(GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z) < 508f) && (GlobalVar.mission.EnvRef.Balls[i].PositionMm.X < -984f)) && ((GlobalVar.mission.EnvRef.Balls[i].PositionMm.X > -1500f) && (GlobalVar.leftScore[i] != 1))) && (GlobalVar.Bringing_BallID[1 - fishID] != i))
                {
                    GlobalVar.Bringing_BallID[fishID] = i;
                    return i;
                }
            }
            if ((((((-1 == index) || this.IsInCornerChoseBallArea((double)GlobalVar.balls[index].PositionMm.X, (double)GlobalVar.balls[index].PositionMm.Z)) || (this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z) || (GlobalVar.Bringing_BallID[1 - fishID] == index))) || (GlobalVar.willbring_ballid[1 - fishID] == index)) || (((GlobalVar.Paishu < 0xbb8) && (GlobalVar.final == 0)) && (((fishID == 0) && (GlobalVar.mission.EnvRef.Balls[index].PositionMm.X < -952f)) && (GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z < -508f)))) || ((((GlobalVar.Paishu < 0xbb8) && (GlobalVar.final == 0)) && ((fishID == 1) && (GlobalVar.mission.EnvRef.Balls[index].PositionMm.X < -952f))) && (GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z > 508f)))
            {
                double num2;
                double num5 = 4000.0;
                double num6 = 4000.0;
                int num7 = -1;
                int num8 = -1;
                for (int j = 0; j < 3; j++)
                {
                    if (((((fishID != 0) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z >= -508f)) && (((fishID != 1) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z <= 508f))) && (((!this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[j].PositionMm.X, (double)(Math.Abs(GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z) - 30f)) && (GlobalVar.Bringing_BallID[1 - fishID] != j)) && !this.IsInOppForbiddenArea((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z)) && (GlobalVar.interfereBallStage[fishID] == 0)))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != j)
                        {
                            if ((num2 < num6) && (num2 > num5))
                            {
                                num6 = num2;
                                num8 = j;
                            }
                            if (num2 < num5)
                            {
                                num6 = num5;
                                num5 = num2;
                                num8 = num7;
                                num7 = j;
                            }
                        }
                    }
                }
                if ((num8 == -1) && (num7 != -1))
                {
                    index = num7;
                }
                else if ((num7 == -1) && (num8 != -1))
                {
                    index = num8;
                }
                else if ((num7 == -1) && (num8 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num7;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
                num5 = 4000.0;
                num6 = 4000.0;
                num7 = -1;
                num8 = -1;
                for (int k = 7; k < 9; k++)
                {
                    if (((((fishID != 0) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.Z >= -508f)) && (((fishID != 1) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.Z <= 508f))) && (((!this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[k].PositionMm.X, (double)(Math.Abs(GlobalVar.mission.EnvRef.Balls[k].PositionMm.Z) - 30f)) && (GlobalVar.Bringing_BallID[1 - fishID] != k)) && !this.IsInOppForbiddenArea((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z)) && (GlobalVar.interfereBallStage[fishID] == 0)))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != k)
                        {
                            if ((num2 < num6) && (num2 > num5))
                            {
                                num6 = num2;
                                num8 = k;
                            }
                            if (num2 < num5)
                            {
                                num6 = num5;
                                num5 = num2;
                                num8 = num7;
                                num7 = k;
                            }
                        }
                    }
                }
                if ((num8 == -1) && (num7 != -1))
                {
                    index = num7;
                }
                else if ((num7 == -1) && (num8 != -1))
                {
                    index = num8;
                }
                else if ((num7 == -1) && (num8 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num7;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
                num5 = 4000.0;
                num6 = 4000.0;
                num7 = -1;
                num8 = -1;
                for (int m = 3; m < 7; m++)
                {
                    if (GlobalVar.teamId == 0)
                    {
                        if (((fishID == 0) && (m == 3)) || ((fishID == 1) && (m == 6)))
                        {
                            continue;
                        }
                    }
                    else if ((GlobalVar.teamId == 1) && (((fishID == 0) && (m == 5)) || ((fishID == 1) && (m == 4))))
                    {
                        continue;
                    }
                    if (((((fishID != 0) || (GlobalVar.mission.EnvRef.Balls[m].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[m].PositionMm.Z >= -508f)) && (((fishID != 1) || (GlobalVar.mission.EnvRef.Balls[m].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[m].PositionMm.Z <= 508f))) && ((!this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[m].PositionMm.X, (double)(Math.Abs(GlobalVar.mission.EnvRef.Balls[m].PositionMm.Z) - 30f)) && (GlobalVar.Bringing_BallID[1 - fishID] != m)) && (GlobalVar.interfereBallStage[fishID] == 0)))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[m].PositionMm.X, (double)GlobalVar.balls[m].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[m].PositionMm.X, (double)GlobalVar.balls[m].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != m)
                        {
                            if ((num2 < num6) && (num2 > num5))
                            {
                                num6 = num2;
                                num8 = m;
                            }
                            if (num2 < num5)
                            {
                                num6 = num5;
                                num5 = num2;
                                num8 = num7;
                                num7 = m;
                            }
                        }
                    }
                }
                if ((num8 == -1) && (num7 != -1))
                {
                    index = num7;
                }
                else if ((num7 == -1) && (num8 != -1))
                {
                    index = num8;
                }
                else if ((num7 == -1) && (num8 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num7;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
            }
            if (GlobalVar.Bringing_BallID[fishID] != index)
            {
                GlobalVar.whether_change_to_test3[fishID] = 0;
            }
            if (index == -1)
            {
                GlobalVar.Bringing_BallID[fishID] = -1;
                return index;
            }
            if ((((GlobalVar.willbring_ballid[fishID] != -1) && (GlobalVar.MyTeam.Fishes[fishID].PositionMm.X < 950f)) && (Math.Abs(GlobalVar.MyTeam.Fishes[fishID].PolygonVertices[0].Z) > 850f)) && (GlobalVar.leftScore[GlobalVar.willbring_ballid[fishID]] != 1))
            {
                index = GlobalVar.willbring_ballid[fishID];
            }
            if (index == GlobalVar.willbring_ballid[fishID])
            {
                GlobalVar.willbring_ballid[fishID] = -1;
            }
            GlobalVar.Bringing_BallID[fishID] = index;
            return index;
        }

        public int ChoseBall2(int fishID)
        {
            int index = GlobalVar.Bringing_BallID[fishID];
            if ((((((-1 == index) || this.IsInOppUnArea_01((double)GlobalVar.balls[index].PositionMm.X, (double)GlobalVar.balls[index].PositionMm.Z)) || (this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z) || (GlobalVar.leftScore[index] == 1))) || ((GlobalVar.Bringing_BallID[1 - fishID] == index) || (GlobalVar.willbring_ballid[1 - fishID] == index))) || ((((GlobalVar.Paishu < 0xbb8) && (GlobalVar.final == 0)) && ((fishID == 0) && (GlobalVar.mission.EnvRef.Balls[index].PositionMm.X < -952f))) && ((GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z < -508f) && ((GlobalVar.Paishu >= 0x5dc) || (GlobalVar.MyTeam.Fishes[1 - fishID].PositionMm.Z <= 200f))))) || (((((GlobalVar.Paishu < 0xbb8) && (GlobalVar.final == 0)) && ((fishID == 1) && (GlobalVar.mission.EnvRef.Balls[index].PositionMm.X < -952f))) && (GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z > 508f)) && ((GlobalVar.Paishu >= 0x5dc) || (GlobalVar.MyTeam.Fishes[1 - fishID].PositionMm.Z >= -200f))))
            {
                double num2;
                double num3 = 4000.0;
                double num4 = 4000.0;
                int num5 = -1;
                int num6 = -1;
                for (int i = 0; i < 3; i++)
                {
                    if ((((((GlobalVar.Paishu >= 0xbb8) || (GlobalVar.final != 0)) || ((fishID != 0) || (GlobalVar.mission.EnvRef.Balls[i].PositionMm.X >= -952f))) || (GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z >= -508f)) && ((((GlobalVar.Paishu >= 0xbb8) || (GlobalVar.final != 0)) || ((fishID != 1) || (GlobalVar.mission.EnvRef.Balls[i].PositionMm.X >= -952f))) || (GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z <= 508f))) && (((!this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[i].PositionMm.X, (double)(Math.Abs(GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z) - 30f)) && (GlobalVar.Bringing_BallID[1 - fishID] != i)) && (!this.IsInOppUnArea_01((double)GlobalVar.balls[i].PositionMm.X, (double)GlobalVar.balls[i].PositionMm.Z) && (GlobalVar.leftScore[i] != 1))) && (GlobalVar.interfereBallStage[fishID] == 0)))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[i].PositionMm.X, (double)GlobalVar.balls[i].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[i].PositionMm.X, (double)GlobalVar.balls[i].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != i)
                        {
                            if ((num2 < num4) && (num2 > num3))
                            {
                                num4 = num2;
                                num6 = i;
                            }
                            if (num2 < num3)
                            {
                                num4 = num3;
                                num3 = num2;
                                num6 = num5;
                                num5 = i;
                            }
                        }
                    }
                }
                if ((num6 == -1) && (num5 != -1))
                {
                    index = num5;
                }
                else if ((num5 == -1) && (num6 != -1))
                {
                    index = num6;
                }
                else if ((num5 == -1) && (num6 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num5;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
                num3 = 4000.0;
                num4 = 4000.0;
                num5 = -1;
                num6 = -1;
                for (int j = 7; j < 9; j++)
                {
                    if ((((((GlobalVar.Paishu >= 0xbb8) || (GlobalVar.final != 0)) || ((fishID != 0) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.X >= -952f))) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z >= -508f)) && ((((GlobalVar.Paishu >= 0xbb8) || (GlobalVar.final != 0)) || ((fishID != 1) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.X >= -952f))) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z <= 508f))) && (((!this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[j].PositionMm.X, (double)(Math.Abs(GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z) - 30f)) && (GlobalVar.Bringing_BallID[1 - fishID] != j)) && (!this.IsInOppUnArea_01((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z) && (GlobalVar.leftScore[j] != 1))) && (GlobalVar.interfereBallStage[fishID] == 0)))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != j)
                        {
                            if ((num2 < num4) && (num2 > num3))
                            {
                                num4 = num2;
                                num6 = j;
                            }
                            if (num2 < num3)
                            {
                                num4 = num3;
                                num3 = num2;
                                num6 = num5;
                                num5 = j;
                            }
                        }
                    }
                }
                if ((num6 == -1) && (num5 != -1))
                {
                    index = num5;
                }
                else if ((num5 == -1) && (num6 != -1))
                {
                    index = num6;
                }
                else if ((num5 == -1) && (num6 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num5;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
                num3 = 4000.0;
                num4 = 4000.0;
                num5 = -1;
                num6 = -1;
                for (int k = 3; k < 7; k++)
                {
                    if ((GlobalVar.teamId == 0) && (GlobalVar.final == 0))
                    {
                        if (((fishID == 0) && (k == 3)) || ((fishID == 1) && (k == 6)))
                        {
                            continue;
                        }
                    }
                    else if (((GlobalVar.teamId == 1) && (GlobalVar.final == 0)) && (((fishID == 0) && (k == 5)) || ((fishID == 1) && (k == 4))))
                    {
                        continue;
                    }
                    if ((((((GlobalVar.Paishu >= 0xbb8) || (GlobalVar.final != 0)) || ((fishID != 0) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.X >= -952f))) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.Z >= -508f)) && ((((GlobalVar.Paishu >= 0xbb8) || (GlobalVar.final != 0)) || ((fishID != 1) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.X >= -952f))) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.Z <= 508f))) && (((!this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[k].PositionMm.X, (double)(Math.Abs(GlobalVar.mission.EnvRef.Balls[k].PositionMm.Z) - 30f)) && (GlobalVar.Bringing_BallID[1 - fishID] != k)) && (!this.IsInOppUnArea_01((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z) && (GlobalVar.leftScore[k] != 1))) && (GlobalVar.interfereBallStage[fishID] == 0)))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != k)
                        {
                            if ((num2 < num4) && (num2 > num3))
                            {
                                num4 = num2;
                                num6 = k;
                            }
                            if (num2 < num3)
                            {
                                num4 = num3;
                                num3 = num2;
                                num6 = num5;
                                num5 = k;
                            }
                        }
                    }
                }
                if ((num6 == -1) && (num5 != -1))
                {
                    index = num5;
                }
                else if ((num5 == -1) && (num6 != -1))
                {
                    index = num6;
                }
                else if ((num5 == -1) && (num6 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num5;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
            }
            if (GlobalVar.Bringing_BallID[fishID] != index)
            {
                GlobalVar.whether_change_to_test3[fishID] = 0;
            }
            if (index == -1)
            {
                GlobalVar.Bringing_BallID[fishID] = -1;
                return index;
            }
            if ((((GlobalVar.willbring_ballid[fishID] != -1) && (GlobalVar.MyTeam.Fishes[fishID].PositionMm.X < 950f)) && (Math.Abs(GlobalVar.MyTeam.Fishes[fishID].PolygonVertices[0].Z) > 850f)) && (GlobalVar.leftScore[GlobalVar.willbring_ballid[fishID]] != 1))
            {
                index = GlobalVar.willbring_ballid[fishID];
            }
            if (index == GlobalVar.willbring_ballid[fishID])
            {
                GlobalVar.willbring_ballid[fishID] = -1;
            }
            GlobalVar.Bringing_BallID[fishID] = index;
            return index;
        }

        public int ChoseBall20(int fishID)
        {
            int index = GlobalVar.Bringing_BallID[fishID];
            if ((((((-1 == index) || this.IsInCornerChoseBallArea((double)GlobalVar.balls[index].PositionMm.X, (double)GlobalVar.balls[index].PositionMm.Z)) || (this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z) || (GlobalVar.Bringing_BallID[1 - fishID] == index))) || (GlobalVar.willbring_ballid[1 - fishID] == index)) || (((GlobalVar.Paishu < 0xbb8) && (GlobalVar.final == 0)) && (((fishID == 0) && (GlobalVar.mission.EnvRef.Balls[index].PositionMm.X < -952f)) && (GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z < -508f)))) || ((((GlobalVar.Paishu < 0xbb8) && (GlobalVar.final == 0)) && ((fishID == 1) && (GlobalVar.mission.EnvRef.Balls[index].PositionMm.X < -952f))) && (GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z > 508f)))
            {
                double num2;
                double num3 = 4000.0;
                double num4 = 4000.0;
                int num5 = -1;
                int num6 = -1;
                for (int i = 0; i < 3; i++)
                {
                    if (((((fishID != 0) || (GlobalVar.mission.EnvRef.Balls[i].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z >= -508f)) && (((fishID != 1) || (GlobalVar.mission.EnvRef.Balls[i].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z <= 508f))) && (((!this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[i].PositionMm.X, (double)(Math.Abs(GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z) - 30f)) && (GlobalVar.Bringing_BallID[1 - fishID] != i)) && !this.IsInOppUnArea_01((double)GlobalVar.balls[i].PositionMm.X, (double)GlobalVar.balls[i].PositionMm.Z)) && (GlobalVar.interfereBallStage[fishID] == 0)))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[i].PositionMm.X, (double)GlobalVar.balls[i].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[i].PositionMm.X, (double)GlobalVar.balls[i].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != i)
                        {
                            if ((num2 < num4) && (num2 > num3))
                            {
                                num4 = num2;
                                num6 = i;
                            }
                            if (num2 < num3)
                            {
                                num4 = num3;
                                num3 = num2;
                                num6 = num5;
                                num5 = i;
                            }
                        }
                    }
                }
                if ((num6 == -1) && (num5 != -1))
                {
                    index = num5;
                }
                else if ((num5 == -1) && (num6 != -1))
                {
                    index = num6;
                }
                else if ((num5 == -1) && (num6 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num5;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
                num3 = 4000.0;
                num4 = 4000.0;
                num5 = -1;
                num6 = -1;
                for (int j = 7; j < 9; j++)
                {
                    if (((((fishID != 0) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z >= -508f)) && (((fishID != 1) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z <= 508f))) && (((!this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[j].PositionMm.X, (double)(Math.Abs(GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z) - 30f)) && (GlobalVar.Bringing_BallID[1 - fishID] != j)) && !this.IsInOppUnArea_01((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z)) && (GlobalVar.interfereBallStage[fishID] == 0)))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != j)
                        {
                            if ((num2 < num4) && (num2 > num3))
                            {
                                num4 = num2;
                                num6 = j;
                            }
                            if (num2 < num3)
                            {
                                num4 = num3;
                                num3 = num2;
                                num6 = num5;
                                num5 = j;
                            }
                        }
                    }
                }
                if ((num6 == -1) && (num5 != -1))
                {
                    index = num5;
                }
                else if ((num5 == -1) && (num6 != -1))
                {
                    index = num6;
                }
                else if ((num5 == -1) && (num6 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num5;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
                num3 = 4000.0;
                num4 = 4000.0;
                num5 = -1;
                num6 = -1;
                for (int k = 3; k < 7; k++)
                {
                    if (GlobalVar.teamId == 0)
                    {
                        if (((fishID == 0) && (k == 3)) || ((fishID == 1) && (k == 6)))
                        {
                            continue;
                        }
                    }
                    else if ((GlobalVar.teamId == 1) && (((fishID == 0) && (k == 5)) || ((fishID == 1) && (k == 4))))
                    {
                        continue;
                    }
                    if (((((fishID != 0) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.Z >= -508f)) && (((fishID != 1) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.X >= -952f)) || (GlobalVar.mission.EnvRef.Balls[k].PositionMm.Z <= 508f))) && (((!this.IsInMyUnArea((double)GlobalVar.mission.EnvRef.Balls[k].PositionMm.X, (double)(Math.Abs(GlobalVar.mission.EnvRef.Balls[k].PositionMm.Z) - 30f)) && (GlobalVar.Bringing_BallID[1 - fishID] != k)) && !this.IsInOppUnArea_01((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z)) && (GlobalVar.interfereBallStage[fishID] == 0)))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != k)
                        {
                            if ((num2 < num4) && (num2 > num3))
                            {
                                num4 = num2;
                                num6 = k;
                            }
                            if (num2 < num3)
                            {
                                num4 = num3;
                                num3 = num2;
                                num6 = num5;
                                num5 = k;
                            }
                        }
                    }
                }
                if ((num6 == -1) && (num5 != -1))
                {
                    index = num5;
                }
                else if ((num5 == -1) && (num6 != -1))
                {
                    index = num6;
                }
                else if ((num5 == -1) && (num6 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num5;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
            }
            if (GlobalVar.Bringing_BallID[fishID] != index)
            {
                GlobalVar.whether_change_to_test3[fishID] = 0;
            }
            if (index == -1)
            {
                GlobalVar.Bringing_BallID[fishID] = -1;
                return index;
            }
            if ((((GlobalVar.willbring_ballid[fishID] != -1) && (GlobalVar.MyTeam.Fishes[fishID].PositionMm.X < 950f)) && (Math.Abs(GlobalVar.MyTeam.Fishes[fishID].PolygonVertices[0].Z) > 850f)) && (GlobalVar.leftScore[GlobalVar.willbring_ballid[fishID]] != 1))
            {
                index = GlobalVar.willbring_ballid[fishID];
            }
            if (index == GlobalVar.willbring_ballid[fishID])
            {
                GlobalVar.willbring_ballid[fishID] = -1;
            }
            GlobalVar.Bringing_BallID[fishID] = index;
            return index;
        }

        public int choseball3_special(int whichfish)
        {
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            int index = GlobalVar.Bringing_BallID[whichfish];
            if ((((-1 == index) || (GlobalVar.leftScore[index] == 1)) || (GlobalVar.Bringing_BallID[1 - whichfish] == index)) || ((index != -1) && !GlobalVar.BaAction.IsInMyUnArea_01((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X)))
            {
                index = -1;
                for (index = 0; index < 3; index++)
                {
                    num2 = 1;
                    if (((GlobalVar.Bringing_BallID[1 - whichfish] != index) && GlobalVar.BaAction.IsInMyUnArea_02((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z)) && (GlobalVar.leftScore[index] != 1))
                    {
                        GlobalVar.Bringing_BallID[whichfish] = index;
                        return index;
                    }
                }
                for (index = 7; index < 9; index++)
                {
                    num3 = 1;
                    if (((GlobalVar.Bringing_BallID[1 - whichfish] != index) && GlobalVar.BaAction.IsInMyUnArea_02((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z)) && (GlobalVar.leftScore[index] != 1))
                    {
                        GlobalVar.Bringing_BallID[whichfish] = index;
                        return index;
                    }
                }
                index = 3;
                while (index < 7)
                {
                    num4 = 1;
                    if (((GlobalVar.Bringing_BallID[1 - whichfish] != index) && GlobalVar.BaAction.IsInMyUnArea_02((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z)) && (GlobalVar.leftScore[index] != 1))
                    {
                        GlobalVar.Bringing_BallID[whichfish] = index;
                        return index;
                    }
                    index++;
                }
                if (((num2 == 1) && (num3 == 1)) && (num4 == 1))
                {
                    index = -1;
                    return index;
                }
            }
            GlobalVar.Bringing_BallID[whichfish] = index;
            return index;
        }

        public int choseball3_special2(int whichfish)
        {
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            int index = GlobalVar.Bringing_BallID[whichfish];
            if ((((-1 == index) || (GlobalVar.leftScore[index] == 1)) || (GlobalVar.Bringing_BallID[1 - whichfish] == index)) || ((index != -1) && !GlobalVar.BaAction.IsInMyUnArea_02((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z)))
            {
                index = -1;
                for (index = 0; index < 3; index++)
                {
                    num2 = 1;
                    if (((GlobalVar.Bringing_BallID[1 - whichfish] != index) && GlobalVar.BaAction.IsInMyUnArea_02((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z)) && (GlobalVar.leftScore[index] != 1))
                    {
                        GlobalVar.Bringing_BallID[whichfish] = index;
                        return index;
                    }
                }
                for (index = 7; index < 9; index++)
                {
                    num3 = 1;
                    if (((GlobalVar.Bringing_BallID[1 - whichfish] != index) && GlobalVar.BaAction.IsInMyUnArea_02((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z)) && (GlobalVar.leftScore[index] != 1))
                    {
                        GlobalVar.Bringing_BallID[whichfish] = index;
                        return index;
                    }
                }
                index = 3;
                while (index < 7)
                {
                    num4 = 1;
                    if (((GlobalVar.Bringing_BallID[1 - whichfish] != index) && GlobalVar.BaAction.IsInMyUnArea_02((double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[index].PositionMm.Z)) && (GlobalVar.leftScore[index] != 1))
                    {
                        GlobalVar.Bringing_BallID[whichfish] = index;
                        return index;
                    }
                    index++;
                }
                if (((num2 == 1) && (num3 == 1)) && (num4 == 1))
                {
                    index = -1;
                    return index;
                }
            }
            GlobalVar.Bringing_BallID[whichfish] = index;
            return index;
        }

        public int ChoseBallplus(int fishID)
        {
            int index = GlobalVar.Bringing_BallID[fishID];
            if ((((-1 == index) || this.IsInOppUnArea_01((double)GlobalVar.balls[index].PositionMm.X, (double)GlobalVar.balls[index].PositionMm.Z)) || ((GlobalVar.leftScore[index] == 1) || (GlobalVar.Bringing_BallID[1 - fishID] == index))) || (GlobalVar.willbring_ballid[1 - fishID] == index))
            {
                double num2;
                double num3 = 4000.0;
                double num4 = 4000.0;
                int num5 = -1;
                int num6 = -1;
                for (int i = 0; i < 3; i++)
                {
                    if ((((GlobalVar.leftScore[i] != 1) && (GlobalVar.Bringing_BallID[1 - fishID] != i)) && !this.IsInOppUnArea_01((double)GlobalVar.balls[i].PositionMm.X, (double)GlobalVar.balls[i].PositionMm.Z)) && (GlobalVar.interfereBallStage[fishID] == 0))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[i].PositionMm.X, (double)GlobalVar.balls[i].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[i].PositionMm.X, (double)GlobalVar.balls[i].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != i)
                        {
                            if ((num2 < num4) && (num2 > num3))
                            {
                                num4 = num2;
                                num6 = i;
                            }
                            if (num2 < num3)
                            {
                                num4 = num3;
                                num3 = num2;
                                num6 = num5;
                                num5 = i;
                            }
                        }
                    }
                }
                if ((num6 == -1) && (num5 != -1))
                {
                    index = num5;
                }
                else if ((num5 == -1) && (num6 != -1))
                {
                    index = num6;
                }
                else if ((num5 == -1) && (num6 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num5;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
                num3 = 4000.0;
                num4 = 4000.0;
                num5 = -1;
                num6 = -1;
                for (int j = 7; j < 9; j++)
                {
                    if ((((GlobalVar.leftScore[j] != 1) && (GlobalVar.Bringing_BallID[1 - fishID] != j)) && !this.IsInOppUnArea_01((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z)) && (GlobalVar.interfereBallStage[fishID] == 0))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[j].PositionMm.X, (double)GlobalVar.balls[j].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != j)
                        {
                            if ((num2 < num4) && (num2 > num3))
                            {
                                num4 = num2;
                                num6 = j;
                            }
                            if (num2 < num3)
                            {
                                num4 = num3;
                                num3 = num2;
                                num6 = num5;
                                num5 = j;
                            }
                        }
                    }
                }
                if ((num6 == -1) && (num5 != -1))
                {
                    index = num5;
                }
                else if ((num5 == -1) && (num6 != -1))
                {
                    index = num6;
                }
                else if ((num5 == -1) && (num6 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num5;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
                num3 = 4000.0;
                num4 = 4000.0;
                num5 = -1;
                num6 = -1;
                for (int k = 3; k < 7; k++)
                {
                    if ((((GlobalVar.leftScore[k] != 1) && (GlobalVar.Bringing_BallID[1 - fishID] != k)) && !this.IsInOppUnArea_01((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z)) && (GlobalVar.interfereBallStage[fishID] == 0))
                    {
                        if (fishID == 0)
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z, -1500.0, 300.0);
                        }
                        else
                        {
                            num2 = this.Compute_Distance((double)GlobalVar.balls[k].PositionMm.X, (double)GlobalVar.balls[k].PositionMm.Z, -1500.0, -300.0);
                        }
                        if (GlobalVar.Bringing_BallID[1 - fishID] != k)
                        {
                            if ((num2 < num4) && (num2 > num3))
                            {
                                num4 = num2;
                                num6 = k;
                            }
                            if (num2 < num3)
                            {
                                num4 = num3;
                                num3 = num2;
                                num6 = num5;
                                num5 = k;
                            }
                        }
                    }
                }
                if ((num6 == -1) && (num5 != -1))
                {
                    index = num5;
                }
                else if ((num5 == -1) && (num6 != -1))
                {
                    index = num6;
                }
                else if ((num5 == -1) && (num6 == -1))
                {
                    index = -1;
                }
                else
                {
                    index = num5;
                }
                if (index != -1)
                {
                    if (GlobalVar.Bringing_BallID[fishID] != index)
                    {
                        GlobalVar.whether_change_to_test3[fishID] = 0;
                    }
                    GlobalVar.Bringing_BallID[fishID] = index;
                    return index;
                }
            }
            if (GlobalVar.Bringing_BallID[fishID] != index)
            {
                GlobalVar.whether_change_to_test3[fishID] = 0;
            }
            if (index == -1)
            {
                GlobalVar.Bringing_BallID[fishID] = -1;
                return index;
            }
            if ((((GlobalVar.willbring_ballid[fishID] != -1) && (GlobalVar.MyTeam.Fishes[fishID].PositionMm.X < 950f)) && (Math.Abs(GlobalVar.MyTeam.Fishes[fishID].PolygonVertices[0].Z) > 850f)) && (GlobalVar.leftScore[GlobalVar.willbring_ballid[fishID]] != 1))
            {
                index = GlobalVar.willbring_ballid[fishID];
            }
            if (index == GlobalVar.willbring_ballid[fishID])
            {
                GlobalVar.willbring_ballid[fishID] = -1;
            }
            GlobalVar.Bringing_BallID[fishID] = index;
            return index;
        }

        public void Circle_last2min(int whichfish)
        {
            int num;
            if (whichfish == 0)
            {
                num = 1;
            }
            else
            {
                num = -1;
            }
            double pI = GlobalVar.PI;
            double x = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].X;
            double z = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].Z;
            double num9 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].X;
            double num10 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].Z;
            double num4 = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.X;
            double num5 = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.Z;
            double num7 = GlobalVar.MyTeam.Fishes[1].PositionMm.Z;
            double num8 = GlobalVar.MyTeam.Fishes[0].PositionMm.Z;
            double velocityDirectionRad = GlobalVar.MyTeam.Fishes[whichfish].VelocityDirectionRad;
            if ((Math.Abs(velocityDirectionRad) > ((5.0 * pI) / 6.0)) && (GlobalVar.Circle_last2min_tag[whichfish] == 0))
            {
                GlobalVar.Circle_last2min_tag[whichfish] = 1;
            }
            if ((x > -1152.0) && (GlobalVar.Circle_last2min_tag[whichfish] == 1))
            {
                GlobalVar.Circle_last2min_tag[whichfish] = 2;
            }
            if ((GlobalVar.Circle_last2min_tag[whichfish] == 2) && (x < -1250.0))
            {
                GlobalVar.Circle_last2min_tag[whichfish] = 3;
            }
            if ((GlobalVar.Circle_last2min_tag[whichfish] == 3) && (Math.Abs(num5) > 700.0))
            {
                GlobalVar.Circle_last2min_tag[whichfish] = 0;
            }
            if ((GlobalVar.teamId == 1) && (GlobalVar.Paishu > 0xbb8))
            {
                num = -num;
            }
            if (GlobalVar.Circle_last2min_tag[whichfish] == 0)
            {
                GlobalVar.decisions[whichfish].TCode = 7 - (num * 3);
                GlobalVar.decisions[whichfish].VCode = 5;
            }
            if (GlobalVar.Circle_last2min_tag[whichfish] == 1)
            {
                if (GlobalVar.Circle_last2min_tag[1 - whichfish] == 0)
                {
                    this.StayForDecision(whichfish);
                }
                else if ((x > (num9 + 6.0)) && (GlobalVar.Circle_last2min_tag[whichfish] == GlobalVar.Circle_last2min_tag[1 - whichfish]))
                {
                    this.StayForDecision(whichfish);
                }
                else
                {
                    GlobalVar.decisions[whichfish].TCode = 7 + (num * 4);
                    this.KeepV(whichfish, 18.0);
                }
            }
            if (GlobalVar.Circle_last2min_tag[whichfish] == 2)
            {
                if (GlobalVar.Circle_last2min_tag[1 - whichfish] == 1)
                {
                    this.StayForDecision(whichfish);
                }
                else if ((x < (num9 - 6.0)) && (GlobalVar.Circle_last2min_tag[whichfish] == GlobalVar.Circle_last2min_tag[1 - whichfish]))
                {
                    this.StayForDecision(whichfish);
                }
                else
                {
                    GlobalVar.decisions[whichfish].TCode = 7 - (num * 4);
                    this.KeepV(whichfish, 18.0);
                }
            }
            if (GlobalVar.Circle_last2min_tag[whichfish] == 3)
            {
                GlobalVar.lanqiuFlag[whichfish] = 1;
            }
        }
        //******************************************************************

        public void ChooseCornerInterfereBall(int fishIndex, int upDown)
        {
            double x = GlobalVar.MyTeam.Fishes[fishIndex].PositionMm.X;
            double z = GlobalVar.MyTeam.Fishes[fishIndex].PositionMm.Z;
            double vertexX = GlobalVar.MyTeam.Fishes[fishIndex].PolygonVertices[0].X;
            double vertexZ = GlobalVar.MyTeam.Fishes[fishIndex].PolygonVertices[0].Z;
            double velocityDirectionRad = GlobalVar.MyTeam.Fishes[fishIndex].VelocityDirectionRad;

            int directionMultiplier = (upDown == 0) ? 1 : -1;
            GlobalVar.corner[fishIndex] = 0;
            GlobalVar.corner_help[fishIndex] = 0;

            double fishPosX = GlobalVar.MyTeam.Fishes[fishIndex].PositionMm.X;
            double fishPosZ = GlobalVar.MyTeam.Fishes[fishIndex].PositionMm.Z;

            // Check if in interference position
            if (fishPosX > 1250.0 && this.Compute_Distance(fishPosX, fishPosZ, 1500.0, directionMultiplier * 620.0) <= 200.0)
            {
                GlobalVar.chooseinterfereBallStage[fishIndex] = 1;
                if (upDown == 0)
                {
                    GlobalVar.chooseinterfereBallStage_Down[fishIndex] = 1;
                }
                else
                {
                    GlobalVar.chooseinterfereBallStage_Up[fishIndex] = 1;
                }
            }

            // Reset interference stage if back in certain zone
            if (GlobalVar.chooseinterfereBallStage[fishIndex] == 1 && fishPosX < 650.0)
            {
                GlobalVar.chooseinterfereBallStage[fishIndex] = 0;
                if (upDown == 0)
                {
                    GlobalVar.chooseinterfereBallStage_Down[fishIndex] = 0;
                }
                else
                {
                    GlobalVar.chooseinterfereBallStage_Up[fishIndex] = 0;
                }
            }

            // Positioning logic based on fish coordinates
            if (fishPosZ < 500.0 && fishPosZ > -500.0 && fishPosX < -980.0)
            {
                this.Position(fishIndex, -1300.0, 750.0, 300.0);
            }
            else if (fishPosX < 1350.0)
            {
                if (fishPosX < 1100.0)
                {
                    this.Position(fishIndex, 1200.0, directionMultiplier * 620.0, 300.0);
                }
                else if (fishPosX > 1100.0)
                {
                    this.Position(fishIndex, 1500.0, directionMultiplier * 620.0, 300.0);
                }
            }

            
            if (GlobalVar.chooseinterfereBallStage[fishIndex] == 1)
            {
                int tCode = this.Angle_V((directionMultiplier * 2.0) - velocityDirectionRad);
                int adjustedTCode = Math.Abs(7 - tCode);

                if (Math.Abs(velocityDirectionRad) > 1.85 && Math.Abs(velocityDirectionRad) < 2.15)
                {
                    GlobalVar.decisions[fishIndex].TCode = tCode;
                    GlobalVar.decisions[fishIndex].VCode = 11 - adjustedTCode;
                }
                else if (Math.Abs(velocityDirectionRad) > 2.0)
                {
                    GlobalVar.decisions[fishIndex].TCode = tCode;
                    adjustedTCode = Math.Max(1, 5 - adjustedTCode);
                    GlobalVar.decisions[fishIndex].VCode = adjustedTCode;
                }
                else
                {
                    GlobalVar.decisions[fishIndex].TCode = tCode;
                    adjustedTCode = Math.Min(2, adjustedTCode);
                    GlobalVar.decisions[fishIndex].VCode = 10 - adjustedTCode;
                }
            }
        }

        public void HandleCornerInterfereBall(int fishIndex, int upDown)
        {
            int directionMultiplier;
            double posX = GlobalVar.MyTeam.Fishes[fishIndex].PositionMm.X;
            double posZ = GlobalVar.MyTeam.Fishes[fishIndex].PositionMm.Z;
            double vertexX = GlobalVar.MyTeam.Fishes[fishIndex].PolygonVertices[0].X;
            double vertexZ = GlobalVar.MyTeam.Fishes[fishIndex].PolygonVertices[0].Z;
            double fishVelocity = GlobalVar.MyTeam.Fishes[fishIndex].VelocityMmPs;
            double fishDirection = GlobalVar.MyTeam.Fishes[fishIndex].VelocityDirectionRad;
            double piValue = GlobalVar.PI;

            directionMultiplier = (upDown == 0) ? 1 : -1;

            double checkPosX = GlobalVar.MyTeam.Fishes[fishIndex].PositionMm.X;
            double checkPosZ = GlobalVar.MyTeam.Fishes[fishIndex].PositionMm.Z;
            double checkDirection = GlobalVar.MyTeam.Fishes[fishIndex].VelocityDirectionRad;

            if (checkPosX > 1250.0 && this.Compute_Distance(checkPosX, checkPosZ, 1500.0, directionMultiplier * 620.0) <= 200.0)
            {
                GlobalVar.interfereBallStage[fishIndex] = 1;
                if (upDown == 0)
                {
                    GlobalVar.interfereBallStage_Down[fishIndex] = 1;
                }
                else
                {
                    GlobalVar.interfereBallStage_Up[fishIndex] = 1;
                }
            }

            if (GlobalVar.interfereBallStage[fishIndex] == 1 && checkPosX < 650.0)
            {
                GlobalVar.interfereBallStage[fishIndex] = 0;
                GlobalVar.corner[fishIndex] = 0;
                GlobalVar.corner_help[fishIndex] = 0;
                if (upDown == 0)
                {
                    GlobalVar.interfereBallStage_Down[fishIndex] = 0;
                }
                else
                {
                    GlobalVar.interfereBallStage_Up[fishIndex] = 0;
                }
            }

            if (checkPosZ < 500.0 && checkPosZ > -500.0 && checkPosX < -980.0)
            {
                this.Position(fishIndex, -1300.0, directionMultiplier * 750.0, 300.0);
            }
            else
            {
                if (checkPosX < 1350.0)
                {
                    if (checkPosX < 1100.0)
                    {
                        this.Position(fishIndex, 1200.0, directionMultiplier * 620.0, 300.0);
                    }
                    else if (checkPosX > 1100.0)
                    {
                        this.Position(fishIndex, 1500.0, directionMultiplier * 620.0, 300.0);
                    }
                }

                if (GlobalVar.interfereBallStage[fishIndex] == 1)
                {
                    int tCode = this.Angle_V((directionMultiplier * 2.0) - fishDirection);
                    int adjustedTCode = Math.Abs(7 - tCode);

                    if (Math.Abs(fishDirection) > 1.85 && Math.Abs(fishDirection) < 2.15)
                    {
                        GlobalVar.decisions[fishIndex].TCode = tCode;
                        GlobalVar.decisions[fishIndex].VCode = 11 - adjustedTCode;
                    }
                    else if (Math.Abs(fishDirection) > 2.0)
                    {
                        GlobalVar.decisions[fishIndex].TCode = tCode;
                        adjustedTCode = Math.Max(1, 5 - adjustedTCode);
                        GlobalVar.decisions[fishIndex].VCode = adjustedTCode;
                    }
                    else
                    {
                        GlobalVar.decisions[fishIndex].TCode = tCode;
                        adjustedTCode = Math.Min(2, adjustedTCode);
                        GlobalVar.decisions[fishIndex].VCode = 10 - adjustedTCode;
                    }
                }

                double polygonVertexZ = GlobalVar.MyTeam.Fishes[fishIndex].PolygonVertices[0].Z;
                if ((GlobalVar.BaAction.JudgeAtLast(fishIndex) == 0 && posX < 1000.0) &&
                    (Math.Abs(polygonVertexZ) > 850.0 && GlobalVar.interfereBallStage[fishIndex] == 1) || posX < 500.0)
                {
                    GlobalVar.interfereBallStage[fishIndex] = 0;
                    if (upDown == 0)
                    {
                        GlobalVar.interfereBallStage_Down[fishIndex] = 0;
                    }
                    else
                    {
                        GlobalVar.interfereBallStage_Up[fishIndex] = 0;
                    }
                }

                if (vertexX > 950.0 && upDown == 0 && vertexZ <= 480.0)
                {
                    this.Position(fishIndex, 2000.0, 600.0, 300.0);
                }

                if (vertexX > 950.0 && upDown == 1 && vertexZ >= -480.0)
                {
                    this.Position(fishIndex, 2000.0, -600.0, 300.0);
                }
            }
        }

        public int JudgeAtLast(int fishIndex)
        {
            int dribbleResult = this.dribblejudge(fishIndex);

            if (dribbleResult == -1)
            {
                GlobalVar.meaningless[fishIndex]++;
            }
            else
            {
                GlobalVar.meaningless[fishIndex] = 0;
                GlobalVar.willbring_ballid[fishIndex] = dribbleResult;
            }

            if (GlobalVar.meaningless[fishIndex] > 20)
            {
                GlobalVar.willbring_ballid[fishIndex] = -1;
                return 0;
            }
            return 1;
        }

        public void KeepVelocity(int fishIndex, double targetVelocity)
        {
            double currentVelocity = GlobalVar.MyTeam.Fishes[fishIndex].VelocityMmPs;
            double minDifference = 400.0;
            int bestVCode = 0;

            for (int i = 15; i >= 0; i--)
            {
                double adjustedVelocity = (0.9 * currentVelocity) + (0.1 * GlobalVar.vTable[i]);
                if (Math.Abs(adjustedVelocity - targetVelocity) < minDifference)
                {
                    minDifference = Math.Abs(adjustedVelocity - targetVelocity);
                    bestVCode = i;
                }
            }
            GlobalVar.decisions[fishIndex].VCode = bestVCode;
        }

        public double CalculateBallZ()
        {
            double maxAbsZ = 0.0;
            int maxZBallIndex = -1;

            for (int i = 0; i < 9; i++)
            {
                double ballPosX = GlobalVar.balls[i].PositionMm.X;
                double ballPosZ = GlobalVar.balls[i].PositionMm.Z;

                if (this.IsInUnBall(ballPosX, ballPosZ) && Math.Abs(ballPosZ) > maxAbsZ)
                {
                    maxAbsZ = Math.Abs(ballPosZ);
                    maxZBallIndex = i;
                }
            }

            if (maxZBallIndex != -1)
            {
                return Math.Abs(GlobalVar.balls[maxZBallIndex].PositionMm.Z) + 100.0;
            }

            return 720.0;
        }
        //******************************************************************
        //控球函数（难改）(谨慎修改)
        //******************************************************************
        public void Dribble(int which, int whichball, double dx, double dz)
        {
            double num = 1.0;
            double x = GlobalVar.balls[whichball].PositionMm.X;
            double z = GlobalVar.balls[whichball].PositionMm.Z;
            double num4 = GlobalVar.MyTeam.Fishes[which].PositionMm.X;
            double num5 = GlobalVar.MyTeam.Fishes[which].PositionMm.Z;
            double num6 = this.GetAngleRad(x, z, dx, dz);
            double num7 = this.GetAngleRad(num4, num5, x, z);
            Platform pointd = new Platform();
            Platform pointd2 = new Platform();
            Platform pointd3 = new Platform();
            double num8 = this.Compute_Distance(x, z, dx, dz);
            if (num8 > 2000.0)
            {
                num8 = 2000.0;
            }
            double num9 = (num8 / 4.0) * num;
            pointd.Posx = dx + (num9 * Math.Cos(num6 + (GlobalVar.PI / 2.0)));
            pointd.Posz = dz + (num9 * Math.Sin(num6 + (GlobalVar.PI / 2.0)));
            pointd2.Posx = dx - (num9 * Math.Cos(num6 + (GlobalVar.PI / 2.0)));
            pointd2.Posz = dz - (num9 * Math.Sin(num6 + (GlobalVar.PI / 2.0)));
            Platform pointd4 = this.GetLineAcrossPoint(num4, num5, (num7 * 180.0) / GlobalVar.PI, dx, dz, ((num6 * 180.0) / GlobalVar.PI) + 90.0);
            double num10 = this.GetAngleRad(num4, num5, pointd.Posx, pointd.Posz);
            double num11 = this.GetAngleRad(num4, num5, pointd2.Posx, pointd2.Posz);
            double num12 = Math.Abs(this.FormatAngleRad(num7 - num10));
            double num13 = Math.Abs(this.FormatAngleRad(num7 - num11));
            if (this.IsAngleDuring((num10 * 180.0) / GlobalVar.PI, (num11 * 180.0) / GlobalVar.PI, (num7 * 180.0) / GlobalVar.PI))
            {
                pointd3.Posx = pointd4.Posx;
                pointd3.Posz = pointd4.Posz;
            }
            else if (num12 < num13)
            {
                pointd3.Posx = pointd.Posx;
                pointd3.Posz = pointd.Posz;
            }
            else
            {
                pointd3.Posx = pointd2.Posx;
                pointd3.Posz = pointd2.Posz;
            }
            this.dribble4(which, whichball, pointd3.Posx, pointd3.Posz);
        }

        public void boundary_dribble(int whichfish, int whichball, int num, bool dir)
        {
            double pI = GlobalVar.PI;
            RoboFish fish = GlobalVar.MyTeam.Fishes[whichfish];
            double velocityDirectionRad = fish.VelocityDirectionRad;
            double x = fish.PositionMm.X;
            double z = fish.PositionMm.Z;
            double velocityMmPs = fish.VelocityMmPs;
            double angularVelocityRadPs = fish.AngularVelocityRadPs;
            double d = GlobalVar.mission.EnvRef.Balls[whichball].VelocityDirectionRad;
            double num9 = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.X;
            double num10 = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.Z;
            double num11 = GlobalVar.mission.EnvRef.Balls[whichball].VelocityMmPs;
            x += (velocityMmPs * Math.Cos(velocityDirectionRad)) * 0.1;
            z += (velocityMmPs * Math.Sin(velocityDirectionRad)) * 0.1;
            velocityDirectionRad += angularVelocityRadPs * 0.1;
            num9 += (GlobalVar.mission.EnvRef.Balls[whichball].VelocityMmPs * Math.Cos(d)) * 0.1;
            num10 += (GlobalVar.mission.EnvRef.Balls[whichball].VelocityMmPs * Math.Sin(d)) * 0.1;
            double num12 = x + (GlobalVar.Fish_centertohead * Math.Cos(velocityDirectionRad));
            double num13 = z + (GlobalVar.Fish_centertohead * Math.Sin(velocityDirectionRad));
            int num14 = 0;
            double dv = 250.0;
            double dx = 0.0;
            double dz = 0.0;
            double num18 = 0.0;
            num14 = (-180 + (num * 90)) + (dir ? 0x19 : -25);
            num14 = (int)this.FormatAngle((double)num14);
            if (num == 0)
            {
                if (dir)
                {
                    dx = -(GlobalVar.Yard_halflength + 300);
                    dz = num10 - 200.0;
                }
                else
                {
                    dx = -(GlobalVar.Yard_halflength + 300);
                    dz = num10 + 200.0;
                }
                num18 = -(GlobalVar.Yard_halflength - 200);
            }
            if (num == 1)
            {
                if (dir)
                {
                    dx = num9 + 200.0;
                    dz = -(GlobalVar.Yard_halfwidth + 300);
                }
                else
                {
                    dx = num9 - 200.0;
                    dz = -(GlobalVar.Yard_halfwidth + 300);
                }
                num18 = -(GlobalVar.Yard_halfwidth - 200);
            }
            if (num == 2)
            {
                if (dir)
                {
                    dx = GlobalVar.Yard_halflength + 300;
                    dz = num10 + 200.0;
                }
                else
                {
                    dx = GlobalVar.Yard_halflength + 300;
                    dz = num10 - 200.0;
                }
                num18 = GlobalVar.Yard_halflength - 200;
            }
            if (num == 3)
            {
                if (dir)
                {
                    dx = num9 - 200.0;
                    dz = GlobalVar.Yard_halfwidth + 300;
                }
                else
                {
                    dx = num9 + 200.0;
                    dz = GlobalVar.Yard_halfwidth + 300;
                }
                num18 = GlobalVar.Yard_halfwidth - 200;
            }
            double ang = Math.Atan2(num10 - num13, num9 - num12) - velocityDirectionRad;
            ang = this.FormatAngleRad(ang);
            if (((((num == 0) || (num == 2)) && (Math.Abs(num9) > (Math.Abs(num18) / 2.0))) || (((num == 1) || (num == 3)) && (Math.Abs(num10) > (Math.Abs(num18) / 2.0)))) && ((!dir && (((ang / pI) * 180.0) < -5.0)) || (dir && (((ang / pI) * 180.0) > 5.0))))
            {
                if (Math.Abs((double)(((velocityDirectionRad / pI) * 180.0) - num14)) < 10.0)
                {
                    this.Angle(whichfish, (((double)num14) / 180.0) * pI);
                    this.KeepV(whichfish, dv);
                }
                else
                {
                    this.Angle(whichfish, (((double)num14) / 180.0) * pI);
                    this.KeepV(whichfish, 0.0);
                }
            }
            else
            {
                this.Dribble(whichfish, whichball, dx, dz);
            }
        }

        public void boundary_dribbleact(int whichfish)
        {
            int num9;
            GlobalVar.corner[whichfish] = 0;
            GlobalVar.corner_help[whichfish] = 0;
            double pI = GlobalVar.PI;
            RoboFish fish = GlobalVar.MyTeam.Fishes[whichfish];
            double velocityDirectionRad = fish.VelocityDirectionRad;
            double x = fish.PositionMm.X;
            double z = fish.PositionMm.Z;
            double velocityMmPs = fish.VelocityMmPs;
            double angularVelocityRadPs = fish.AngularVelocityRadPs;
            double num7 = 1250.0;
            if (whichfish == 0)
            {
                num9 = 1;
            }
            else
            {
                num9 = -1;
            }
            x += (velocityMmPs * Math.Cos(velocityDirectionRad)) * 0.1;
            z += (velocityMmPs * Math.Sin(velocityDirectionRad)) * 0.1;
            velocityDirectionRad += angularVelocityRadPs * 0.1;
            double num10 = x + (GlobalVar.Fish_centertohead * Math.Cos(velocityDirectionRad));
            double num11 = z + (GlobalVar.Fish_centertohead * Math.Sin(velocityDirectionRad));
            int num12 = this.boundary_dribbleact_constdir;
            double dv = 220.0;
            if ((num10 > 1400.0) && (Math.Abs(num11) < 980.0))
            {
                if (Math.Abs((double)(((velocityDirectionRad / pI) * 180.0) - num12)) < 10.0)
                {
                    this.Angle(whichfish, (((double)num12) / 180.0) * pI);
                    this.KeepV(whichfish, dv);
                }
                else
                {
                    this.Angle(whichfish, (((double)num12) / 180.0) * pI);
                    this.KeepV(whichfish, 0.0);
                }
            }
            else if ((num11 < -900.0) && (num10 > 400.0))
            {
                if (num10 > num7)
                {
                    GlobalVar.decisions[whichfish].VCode = 6;
                    this.Angle(whichfish, ((-90 - Math.Abs(num12)) * pI) / 180.0);
                    if ((velocityDirectionRad < ((-80.0 * pI) / 180.0)) && (velocityDirectionRad > ((-125.0 * pI) / 180.0)))
                    {
                        GlobalVar.decisions[whichfish].TCode = 6;
                    }
                }
                else if (Math.Abs((double)(((velocityDirectionRad / pI) * 180.0) + 115.0)) < 10.0)
                {
                    this.Angle(whichfish, (((double)(-90 - Math.Abs(num12))) / 180.0) * pI);
                    this.KeepV(whichfish, dv);
                }
                else
                {
                    this.Angle(whichfish, (((double)(-90 - Math.Abs(num12))) / 180.0) * pI);
                    this.KeepV(whichfish, 0.0);
                }
                if (num10 < 650.0)
                {
                    GlobalVar.BaAction.Position(whichfish, 0.0, 0.0, 999.0);
                }
                if ((x < 900.0) && (this.dribbleactsup(whichfish) == 0))
                {
                    GlobalVar.BaAction.Position(whichfish, 0.0, 0.0, 999.0);
                    GlobalVar.boundary_dribbleact[whichfish] = 0;
                }
            }
            else if ((num11 > 900.0) && (num10 > 400.0))
            {
                if (num10 > num7)
                {
                    GlobalVar.decisions[whichfish].VCode = 6;
                    this.Angle(whichfish, ((90 + Math.Abs(num12)) * pI) / 180.0);
                    if ((velocityDirectionRad > ((80.0 * pI) / 180.0)) && (velocityDirectionRad < ((125.0 * pI) / 180.0)))
                    {
                        GlobalVar.decisions[whichfish].TCode = 8;
                    }
                }
                else if (Math.Abs((double)(((velocityDirectionRad / pI) * 180.0) - 115.0)) < 10.0)
                {
                    this.Angle(whichfish, (((double)(90 + Math.Abs(num12))) / 180.0) * pI);
                    this.KeepV(whichfish, dv);
                }
                else
                {
                    this.Angle(whichfish, (((double)(90 + Math.Abs(num12))) / 180.0) * pI);
                    this.KeepV(whichfish, 0.0);
                }
                if (num10 < 650.0)
                {
                    GlobalVar.BaAction.Position(whichfish, 0.0, 0.0, 999.0);
                }
                if ((x < 900.0) && (this.dribbleactsup(whichfish) == 0))
                {
                    GlobalVar.BaAction.Position(whichfish, 0.0, 0.0, 999.0);
                    GlobalVar.boundary_dribbleact[whichfish] = 0;
                }
            }
            else
            {
                if ((z < -200.0) || ((z < 200.0) && (velocityDirectionRad < 0.0)))
                {
                    this.boundary_dribbleact_constdir = 0x19;
                    this.Position(whichfish, 1500.0, -700.0, 300.0);
                }
                else
                {
                    this.boundary_dribbleact_constdir = -25;
                    this.Position(whichfish, 1500.0, 700.0, 300.0);
                }
                if ((x < -900.0) && (Math.Abs(z) < 700.0))
                {
                    this.Position(whichfish, -1300.0, (double)(num9 * 800), 300.0);
                }
            }
            if (x < 650.0)
            {
                GlobalVar.boundary_dribbleact[whichfish] = 0;
            }
        }

        public void Dribble_blue(int which, double dx, double dz, int num)
        {
            double num2 = 1.0;
            if ((num >= 0) && (num <= 2))
            {
                num2 = 1.0;
            }
            else if ((num >= 3) && (num <= 6))
            {
                num2 = (num2 * 78.0) / 38.0;
            }
            else
            {
                num2 = (num2 * 58.0) / 38.0;
            }
            int i = (which == 0) ? 13 : 14;
            double x = GlobalVar.balls[num].PositionMm.X;
            double z = GlobalVar.balls[num].PositionMm.Z;
            double num6 = GlobalVar.MyTeam.Fishes[which].PolygonVertices[0].X;
            double num7 = GlobalVar.MyTeam.Fishes[which].PolygonVertices[0].Z;
            double num8 = GlobalVar.MyTeam.Fishes[which].PositionMm.X;
            double num9 = GlobalVar.MyTeam.Fishes[which].PositionMm.Z;
            double bodyDirectionRad = GlobalVar.MyTeam.Fishes[which].BodyDirectionRad;
            double velocityMmPs = GlobalVar.MyTeam.Fishes[which].VelocityMmPs;
            double num12 = this.Compute_Distance(num6, num7, x, z);
            double num13 = this.Compute_Distance(x, z, num8, num9);
            double num14 = this.Compute_Distance(num8, num9, dx, dz);
            double num15 = this.Compute_Distance(x, z, dx, dz);
            double num16 = this.GetAngleRad(x, z, num8, num9);
            double num17 = this.GetAngleRad(x, z, num6, num7);
            double num18 = this.GetAngleRad(num6, num7, x, z);
            double drad = this.GetAngleRad(num8, num9, x, z);
            double num20 = this.GetAngleRad(x, z, dx, dz);
            double num21 = this.GetAngleRad(dx, dz, x, z);
            double num22 = this.GetAngleRad(num8, num9, dx, dz);
            double num23 = this.FormatAngleRad(num21 + GlobalVar.PI);
            double pI = GlobalVar.PI;
            double d = this.FormatAngleRad(num17 - num21);
            double num27 = num12 * Math.Cos(d);
            double num28 = num12 * Math.Sin(d);
            double num29 = this.FormatAngleRad(num16 - num21);
            double num30 = num13 * Math.Cos(num29);
            double num31 = num13 * Math.Sin(num29);
            double num32 = 0.0;
            double num33 = 0.0;
            double num34 = this.FormatAngleRad(num23 - bodyDirectionRad);
            double num35 = bodyDirectionRad;
            double num36 = ((double)GlobalVar.balls[num].RadiusMm) / 38.0;
            if ((((z < -880.0) || (x < -1400.0)) || (z > 880.0)) || (x > 1400.0))
            {
                if ((x < -1400.0) && (z < -900.0))
                {
                    this.position3_1(which, x, -1000.0, 50.0, 0);
                    this.KeepVelocity(which, 170.0);
                }
                else if ((x < -1400.0) && (z > 900.0))
                {
                    this.position3_1(which, x, 1000.0, 50.0, 0);
                    this.KeepVelocity(which, 170.0);
                }
                else if ((x > 1400.0) && (z < -900.0))
                {
                    if ((num6 > 1420.0) && (num7 > -900.0))
                    {
                        this.position3_1(which, 1500.0, z, 50.0, 0);
                        this.KeepVelocity(which, 170.0);
                    }
                    else
                    {
                        this.position3_1(which, 1484.0, -800.0, 50.0, 0);
                    }
                }
                else if ((x > 1400.0) && (z > 900.0))
                {
                    if ((num6 > 1420.0) && (num7 < 900.0))
                    {
                        this.position3_1(which, 1500.0, z, 50.0, 0);
                        this.KeepVelocity(which, 170.0);
                    }
                    else
                    {
                        this.position3_1(which, 1484.0, 800.0, 50.0, 0);
                    }
                }
                else if ((((num >= 0) && (num <= 2)) && (x < -1400.0)) || (((num >= 3) && (num <= 8)) && (x <= -1160.0)))
                {
                    if (z < 160.0)
                    {
                        if ((num17 > (-pI / 6.0)) && (num6 > -1420.0))
                        {
                            this.position3_1(which, x + ((num36 * 200.0) * Math.Cos(pI / 4.0)), z - ((num36 * 200.0) * Math.Sin(pI / 4.0)), 50.0, 0);
                        }
                        else if ((num17 > ((-pI * 8.0) / 18.0)) && (num6 > -1420.0))
                        {
                            this.position3_1(which, x - (num36 * 50.0), z - (num36 * 100.0), 50.0, 0);
                        }
                        else if (num9 > z)
                        {
                            this.position3_1(which, x + (num36 * 100.0), z, 50.0, 0);
                        }
                        else
                        {
                            if (((num == 0) || (num == 1)) || (num == 2))
                            {
                                this.position3_1(which, -1500.0 - (Math.Cos(pI / 12.0) * 800.0), (z - (38.0 * num36)) + (Math.Sin(pI / 12.0) * 800.0), 50.0, 0);
                            }
                            else if ((num == 7) || (num == 8))
                            {
                                this.position3_1(which, -1500.0 - (Math.Cos(pI / 12.0) * 800.0), (z - (38.0 * num36)) + (Math.Sin(pI / 12.0) * 800.0), 50.0, 0);
                                GlobalVar.decisions[which].VCode = 14;
                                if ((x > -1350.0) && (((2.0 * GlobalVar.PI) - Math.Abs((double)(bodyDirectionRad - GlobalVar.OppTeam.Fishes[this.GetUpperFish()].BodyDirectionRad))) >= (GlobalVar.PI / 4.0)))
                                {
                                    this.position3_1(which, (double)GlobalVar.OppTeam.Fishes[this.GetUpperFish()].PositionMm.X, (double)GlobalVar.OppTeam.Fishes[this.GetUpperFish()].PositionMm.Z, 100.0, 0);
                                }
                            }
                            else
                            {
                                GlobalVar.BaAction.position3_1(which, -1800.0, z, 50.0, 0);
                                GlobalVar.decisions[which].VCode = 14;
                                if ((x > -1350.0) && (((2.0 * GlobalVar.PI) - Math.Abs((double)(bodyDirectionRad - GlobalVar.OppTeam.Fishes[this.GetUpperFish()].BodyDirectionRad))) >= (GlobalVar.PI / 4.0)))
                                {
                                    this.position3_1(which, (double)GlobalVar.OppTeam.Fishes[this.GetUpperFish()].PositionMm.X, (double)GlobalVar.OppTeam.Fishes[this.GetUpperFish()].PositionMm.Z, 100.0, 0);
                                }
                            }
                            if (((z < -660.0) && (num7 > -960.0)) && (this.Compute_Distance(num8, num9, x, z) < 130.0))
                            {
                                if ((this.current_w - this.previous_w) >= 5)
                                {
                                    this.current_w = this.previous_w + 1;
                                }
                                else if ((this.current_w - this.previous_w) <= -5)
                                {
                                    this.current_w = this.previous_w - 1;
                                }
                                else
                                {
                                }
                                if (this.current_w > 9)
                                {
                                    this.current_w = 9;
                                }
                                else if (this.current_w < 5)
                                {
                                    this.current_w = 5;
                                }
                                GlobalVar.decisions[which].TCode = this.current_w;
                            }
                        }
                    }
                    else if ((num17 < (pI / 6.0)) && (num6 > -1420.0))
                    {
                        this.position3_1(which, x + ((num36 * 50.0) * Math.Cos(pI / 4.0)), z + ((num36 * 50.0) * Math.Sin(pI / 4.0)), 50.0, 0);
                    }
                    else if ((num17 < ((pI * 8.0) / 18.0)) && (num6 > -1420.0))
                    {
                        this.position3_1(which, x - (num36 * 50.0), z + (num36 * 100.0), 50.0, 0);
                    }
                    else if (num9 < z)
                    {
                        this.position3_1(which, x + (num36 * 100.0), z, 50.0, 0);
                    }
                    else
                    {
                        if (((num == 0) || (num == 1)) || (num == 2))
                        {
                            this.position3_1(which, -1500.0 - (Math.Cos(pI / 12.0) * 800.0), (z + (38.0 * num36)) - (Math.Sin(pI / 12.0) * 800.0), 50.0, 0);
                        }
                        else if ((num == 7) || (num == 8))
                        {
                            this.position3_1(which, -1500.0 - (Math.Cos(pI / 12.0) * 800.0), (z + (38.0 * num36)) - (Math.Sin(pI / 12.0) * 800.0), 50.0, 0);
                            GlobalVar.decisions[which].VCode = 14;
                            if ((x > -1350.0) && (((2.0 * GlobalVar.PI) - Math.Abs((double)(bodyDirectionRad - GlobalVar.OppTeam.Fishes[1 - this.GetUpperFish()].BodyDirectionRad))) >= (GlobalVar.PI / 4.0)))
                            {
                                this.position3_1(which, (double)GlobalVar.OppTeam.Fishes[1 - this.GetUpperFish()].PositionMm.X, (double)GlobalVar.OppTeam.Fishes[1 - this.GetUpperFish()].PositionMm.Z, 100.0, 0);
                            }
                        }
                        else
                        {
                            GlobalVar.BaAction.position3_1(which, -1800.0, z, 50.0, 0);
                            GlobalVar.decisions[which].VCode = 14;
                            if ((x > -1350.0) && (((2.0 * GlobalVar.PI) - Math.Abs((double)(bodyDirectionRad - GlobalVar.OppTeam.Fishes[1 - this.GetUpperFish()].BodyDirectionRad))) >= (GlobalVar.PI / 4.0)))
                            {
                                this.position3_1(which, (double)GlobalVar.OppTeam.Fishes[1 - this.GetUpperFish()].PositionMm.X, (double)GlobalVar.OppTeam.Fishes[1 - this.GetUpperFish()].PositionMm.Z, 100.0, 0);
                            }
                        }
                        if (((z > 720.0) && (num7 < 960.0)) && (this.Compute_Distance(num8, num9, x, z) < 130.0))
                        {
                            if ((this.current_w - this.previous_w) >= 5)
                            {
                                this.current_w = this.previous_w + 1;
                            }
                            else if ((this.current_w - this.previous_w) <= -5)
                            {
                                this.current_w = this.previous_w - 1;
                            }
                            else
                            {
                            }
                            if (this.current_w > 8)
                            {
                                this.current_w = 8;
                            }
                            else if (this.current_w < 6)
                            {
                                this.current_w = 6;
                            }
                            GlobalVar.decisions[which].TCode = this.current_w;
                            if (velocityMmPs > 120.0)
                            {
                                this.KeepVelocity(which, 80.0);
                            }
                        }
                    }
                }
                else if (z > 880.0)
                {
                    if ((num17 < (-pI / 3.0)) && (num7 < 920.0))
                    {
                        this.position3_1(which, x + ((num36 * Math.Cos(-pI / 4.0)) * 200.0), z + ((num36 * Math.Sin(-pI / 4.0)) * 200.0), 50.0, 0);
                    }
                    else if ((num17 < (-pI / 18.0)) && (num7 < 920.0))
                    {
                        this.position3_1(which, x + (num36 * 100.0), z + (num36 * 50.0), 50.0, 0);
                    }
                    else if (num8 < x)
                    {
                        this.position3_1(which, x + ((num36 * Math.Cos((pI * 70.0) / 180.0)) * 100.0), z - ((num36 * Math.Sin((pI * 70.0) / 180.0)) * 100.0), 50.0, 1);
                    }
                    else
                    {
                        this.position3_1(which, (x + (num36 * 38.0)) - (800.0 * Math.Sin(pI / 12.0)), 1000.0 + (800.0 * Math.Cos(pI / 12.0)), 50.0, 0);
                        if ((bodyDirectionRad < ((pI * 8.0) / 18.0)) && (num7 > 950.0))
                        {
                            this.Angle_Stay(which, pI / 2.0, 0);
                            if (velocityMmPs > 70.0)
                            {
                                this.KeepVelocity(which, 1.0);
                            }
                        }
                        if ((bodyDirectionRad > 2.0) && (num7 > 950.0))
                        {
                            this.Angle_Stay(which, pI / 2.0, 0);
                            if (velocityMmPs > 200.0)
                            {
                                this.KeepVelocity(which, 1.0);
                            }
                        }
                        if (this.Compute_Distance(num8, num9, x, z) > 100.0)
                        {
                            this.position3_1(which, x, z + (num36 * 120.0), 50.0, 1);
                        }
                    }
                }
                else if ((num17 > (pI / 3.0)) && (num7 > -920.0))
                {
                    this.position3_1(which, x + ((num36 * Math.Cos(pI / 4.0)) * 200.0), z + ((num36 * Math.Sin(pI / 4.0)) * 200.0), 50.0, 0);
                }
                else if ((num17 > (pI / 18.0)) && (num7 > -920.0))
                {
                    this.position3_1(which, x + (num36 * 100.0), z - (num36 * 50.0), 50.0, 0);
                }
                else if (num8 < x)
                {
                    this.position3_1(which, x + ((num36 * Math.Cos((pI * 7.0) / 18.0)) * 300.0), z + ((num36 * Math.Sin((pI * 7.0) / 18.0)) * 300.0), 50.0, 1);
                }
                else
                {
                    this.position3_1(which, (x + (num36 * 38.0)) - (800.0 * Math.Sin(pI / 12.0)), -1000.0 - (800.0 * Math.Cos(pI / 12.0)), 50.0, 0);
                    if ((bodyDirectionRad > ((-pI * 8.0) / 18.0)) && (num7 < -950.0))
                    {
                        this.Angle_Stay(which, -pI / 2.0, 0);
                        if (velocityMmPs > 70.0)
                        {
                            this.KeepVelocity(which, 1.0);
                        }
                    }
                    if ((bodyDirectionRad < -2.0) && (num7 < -950.0))
                    {
                        this.Angle_Stay(which, -pI / 2.0, 0);
                        if (velocityMmPs > 200.0)
                        {
                            this.KeepVelocity(which, 1.0);
                        }
                    }
                    if (this.Compute_Distance(num8, num9, x, z) > 100.0)
                    {
                        this.position3_1(which, x, z - (num36 * 120.0), 50.0, 1);
                    }
                }
            }
            else if ((((x < -820.0) && (x > -940.0)) && (z > -580.0)) && (z < 580.0))
            {
                if ((z > -580.0) && (z < -450.0))
                {
                    this.position3_1(which, x + ((num36 * 38.0) * Math.Cos((pI * 2.0) / 3.0)), z + ((num36 * 38.0) * Math.Sin((pI * 2.0) / 3.0)), 100.0, 0);
                }
                else if (z < 160.0)
                {
                    if ((num17 < (pI / 6.0)) && (num6 > -860.0))
                    {
                        this.position3_1(which, x + ((num36 * 50.0) * Math.Cos(pI / 4.0)), z + ((num36 * 50.0) * Math.Sin(pI / 4.0)), 50.0, 0);
                    }
                    else if ((num17 < ((pI * 8.0) / 18.0)) && (num6 > -860.0))
                    {
                        this.position3_1(which, x - (num36 * 50.0), z + (num36 * 100.0), 50.0, 0);
                    }
                    else if (num9 < z)
                    {
                        this.position3_1(which, x + (num36 * 100.0), z, 50.0, 0);
                    }
                    else
                    {
                        this.position3_1(which, -940.0 - (Math.Cos(pI / 12.0) * 800.0), (z + (num36 * 38.0)) - (Math.Sin(pI / 12.0) * 800.0), 50.0, 0);
                    }
                }
                else if ((z < 580.0) && (z > 560.0))
                {
                    if (Math.Abs((double)(bodyDirectionRad - (pI / 2.0))) > (pI / 9.0))
                    {
                        this.Angle_Stay(which, pI / 2.0, 1);
                    }
                    else
                    {
                        this.position3_1(which, x, z, 100.0, 0);
                    }
                }
                else if ((num17 > (-pI / 6.0)) && (num6 > -880.0))
                {
                    this.position3_1(which, x + ((num36 * 200.0) * Math.Cos(pI / 4.0)), z - ((num36 * 200.0) * Math.Sin(pI / 4.0)), 50.0, 0);
                }
                else if ((num17 > ((-pI * 8.0) / 18.0)) && (num6 > -880.0))
                {
                    this.position3_1(which, x - (num36 * 50.0), z - (num36 * 100.0), 50.0, 0);
                }
                else if (num9 > z)
                {
                    this.position3_1(which, x + (num36 * 100.0), z, 50.0, 0);
                }
                else
                {
                    this.position3_1(which, -940.0 - (Math.Cos(pI / 12.0) * 800.0), (z - (num36 * 38.0)) + (Math.Sin(pI / 12.0) * 800.0), 50.0, 0);
                }
            }
            else
            {
                if (num30 < 0.0)
                {
                    if (num31 < 0.0)
                    {
                        num32 = x + ((num36 * 100.0) * Math.Cos(this.FormatAngleRad(num21 - (pI / 3.0))));
                        num33 = z + ((num36 * 100.0) * Math.Sin(this.FormatAngleRad(num21 - (pI / 3.0))));
                        this.position3_1(which, num32, num33, 100.0, 1);
                    }
                    else
                    {
                        num32 = x + ((num36 * 100.0) * Math.Cos(this.FormatAngleRad(num21 + (pI / 3.0))));
                        num33 = z + ((num36 * 100.0) * Math.Sin(this.FormatAngleRad(num21 + (pI / 3.0))));
                        this.position3_1(which, num32, num33, 100.0, 1);
                    }
                }
                else if ((num29 > (pI / 4.0)) && (num29 < (pI / 2.0)))
                {
                    if (Math.Abs(this.FormatAngleRad(num18 - bodyDirectionRad)) <= (pI / 18.0))
                    {
                        this.KeepVelocity(which, 170.0);
                    }
                    else if ((Math.Abs((double)(bodyDirectionRad - num21)) < (pI / 2.0)) && (Math.Abs(d) < Math.Abs(num29)))
                    {
                        num32 = x + ((num36 * Math.Cos(num21)) * 100.0);
                        num33 = z + ((num36 * Math.Sin(num21)) * 100.0);
                        this.position3_1(which, num32, num33, 80.0, 1);
                    }
                    else
                    {
                        num32 = x + ((num36 * 150.0) * Math.Cos(num21));
                        num33 = z + ((num36 * 150.0) * Math.Sin(num21));
                        this.position3_1(which, num32, num33, 120.0, 0);
                    }
                }
                else if ((num29 < (-pI / 4.0)) && (num29 > (-pI / 2.0)))
                {
                    if (Math.Abs(this.FormatAngleRad(num18 - bodyDirectionRad)) <= (pI / 18.0))
                    {
                        this.KeepVelocity(which, 170.0);
                    }
                    else if ((Math.Abs((double)(bodyDirectionRad - num21)) < (pI / 2.0)) && (Math.Abs(d) < Math.Abs(num29)))
                    {
                        num32 = x + ((num36 * Math.Cos(num21)) * 100.0);
                        num33 = z + ((num36 * Math.Sin(num21)) * 100.0);
                        this.position3_1(which, num32, num33, 80.0, 1);
                    }
                    else
                    {
                        num32 = x + ((num36 * 150.0) * Math.Cos(num21));
                        num33 = z + ((num36 * 150.0) * Math.Sin(num21));
                        this.position3_1(which, num32, num33, 120.0, 0);
                    }
                }
                else if ((num29 > (-pI / 4.0)) && (num29 < (pI / 4.0)))
                {
                    if (num12 > 70.0)
                    {
                        this.position3_1(which, x + ((num36 * 50.0) * Math.Cos(num21)), z + ((num36 * 50.0) * Math.Sin(num21)), 70.0, 0);
                    }
                    else if (Math.Abs(this.FormatAngleRad(num18 - bodyDirectionRad)) <= (pI / 18.0))
                    {
                        this.KeepVelocity(which, 170.0);
                    }
                    else
                    {
                        this.Angle_Stay(which, drad, 1);
                    }
                }
            }
        }

        public void Dribble_boundaryf(int whichfish, int whichball)
        {
            double pI = GlobalVar.PI;
            RoboFish fish = GlobalVar.MyTeam.Fishes[whichfish];
            double velocityDirectionRad = fish.VelocityDirectionRad;
            double x = fish.PositionMm.X;
            double z = fish.PositionMm.Z;
            double velocityMmPs = fish.VelocityMmPs;
            double angularVelocityRadPs = fish.AngularVelocityRadPs;
            double num7 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].X;
            double num8 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].Z;
            double num9 = GlobalVar.mission.EnvRef.Balls[whichball].VelocityDirectionRad;
            double num10 = GlobalVar.mission.EnvRef.Balls[whichball].VelocityMmPs;
            double num11 = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.X;
            double num12 = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.Z;
            int num13 = 0x4ec;
            int num14 = 850;
            if ((num11 > num13) && (Math.Abs(num12) < (num14 + 50)))
            {
                if (GlobalVar.Dribble_boundaryf_zt1[whichfish] == 100)
                {
                    if (num12 < 0.0)
                    {
                        GlobalVar.Dribble_boundaryf_zt1[whichfish] = 1;
                    }
                    else
                    {
                        GlobalVar.Dribble_boundaryf_zt1[whichfish] = 0;
                    }
                }
                if (Math.Abs(num12) > 100.0)
                {
                    GlobalVar.Dribble_boundaryf_zt1[whichfish] = 100;
                }
                if ((num12 > 100.0) || (GlobalVar.Dribble_boundaryf_zt1[whichfish] == 1))
                {
                    this.boundary_dribble(whichfish, whichball, 2, true);
                }
                else if ((num12 < -100.0) || (GlobalVar.Dribble_boundaryf_zt1[whichfish] == 0))
                {
                    this.boundary_dribble(whichfish, whichball, 2, false);
                }
            }
            else if ((num11 < -num13) && (Math.Abs(num12) < 700.0))
            {
                if (GlobalVar.Dribble_boundaryf_zt2[whichfish] == 100)
                {
                    if (num12 < 0.0)
                    {
                        GlobalVar.Dribble_boundaryf_zt2[whichfish] = 1;
                    }
                    else
                    {
                        GlobalVar.Dribble_boundaryf_zt2[whichfish] = 0;
                    }
                }
                if (Math.Abs(num12) > 560.0)
                {
                    GlobalVar.Dribble_boundaryf_zt2[whichfish] = 100;
                }
                if ((num12 < -560.0) || (GlobalVar.Dribble_boundaryf_zt2[whichfish] == 1))
                {
                    this.boundary_dribble(whichfish, whichball, 0, false);
                }
                if ((num12 > 560.0) || (GlobalVar.Dribble_boundaryf_zt2[whichfish] == 0))
                {
                    this.boundary_dribble(whichfish, whichball, 0, true);
                }
            }
            else
            {
                this.Dribbleboundary(whichfish, whichball);
            }
        }

        public void Rescue_boundary(int whichfish, int whichball, double dx, double dz)
        {
            double pI = GlobalVar.PI;
            RoboFish fish = GlobalVar.MyTeam.Fishes[whichfish];
            double velocityDirectionRad = fish.VelocityDirectionRad;
            double x = fish.PositionMm.X;
            double z = fish.PositionMm.Z;
            double velocityMmPs = fish.VelocityMmPs;
            double angularVelocityRadPs = fish.AngularVelocityRadPs;
            double num7 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].X;
            double num8 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].Z;
            double num9 = (Math.Cos(velocityDirectionRad) * 0.9114) * velocityMmPs;
            double num10 = (Math.Sin(velocityDirectionRad) * 0.9114) * velocityMmPs;
            int num13 = 750;
            int num14 = -50;
            int num15 = 20;
            int num16 = -50;
            int num17 = 0x47e;
            double num18 = GlobalVar.mission.EnvRef.Balls[whichball].VelocityDirectionRad;
            double num19 = GlobalVar.mission.EnvRef.Balls[whichball].VelocityMmPs;
            double num20 = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.X;
            double num21 = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.Z;
            if (velocityDirectionRad < -pI)
            {
                velocityDirectionRad += 2.0 * pI;
            }
            if (velocityDirectionRad > pI)
            {
                velocityDirectionRad -= 2.0 * pI;
            }
            double num24 = dx;
            double num25 = Math.Abs(dz);
            double num26 = num20;
            double num27 = Math.Abs(num21);
            double num29 = Math.Sqrt(((num20 - x) * (num20 - x)) + ((num21 - z) * (num21 - z)));
            double num22 = (num25 - num27) / (num24 - num26);
            double num23 = num25 - (num22 * num24);
            double num28 = (1000.0 - num23) / num22;
            if ((Math.Abs(num21) > 650.0) && (Math.Abs(num20) < 1150.0))
            {
                double num11;
                double num12;
                if ((num20 > 600.0) && (num29 > 500.0))
                {
                    GlobalVar.Rescue_boundary_zt2[whichfish] = 1;
                }
                if (num21 > 0.0)
                {
                    num11 = ((num21 + num15) - num8) / (num20 - num7);
                    num12 = num8 - (num11 * num7);
                    if (((GlobalVar.Rescue_boundary_Zhuangtai[whichfish] == 0) && ((num20 - 150.0) < x)) || ((GlobalVar.Rescue_boundary_Zhuangtai[whichfish] != 0) && ((num20 + 150.0) < x)))
                    {
                        if (Math.Abs(num21) > 800.0)
                        {
                            GlobalVar.Rescue_boundary_Zhuangtai[whichfish] = 0;
                            if ((num20 - num7) > 90.0)
                            {
                                GlobalVar.Rescue_boundary_Zhuangtai[whichfish] = 1;
                            }
                            this.Position(whichfish, num20 - 150.0, num21 - 100.0, 300.0);
                        }
                        else
                        {
                            if (((Math.Abs(num8) - Math.Abs(num21)) < 50.0) || ((Math.Abs(num7) - Math.Abs(num20)) > 80.0))
                            {
                                this.Position(whichfish, num20 + 80.0, num21 + 30.0, 300.0);
                            }
                            else
                            {
                                if (((GlobalVar.Rescue_boundary_zt1[whichfish] == 0) && (num21 < (num13 + 15))) || ((GlobalVar.Rescue_boundary_zt1[whichfish] != 0) && (num21 < (num13 - 15))))
                                {
                                    this.Position(whichfish, num20 + 12.0, num21 + 35.0, 300.0);
                                    GlobalVar.Rescue_boundary_zt1[whichfish] = 0;
                                }
                                else
                                {
                                    GlobalVar.Rescue_boundary_zt1[whichfish] = 1;
                                    if (num21 > num13)
                                    {
                                        if ((velocityDirectionRad < ((110.0 * pI) / 180.0)) || (velocityDirectionRad > ((165.0 * pI) / 180.0)))
                                        {
                                            this.Angle(whichfish, (135.0 * pI) / 180.0);
                                            this.KeepV(whichfish, 5.0);
                                        }
                                        else
                                        {
                                            this.Angle(whichfish, (135.0 * pI) / 180.0);
                                            this.KeepV(whichfish, 300.0);
                                        }
                                    }
                                    if (num21 < (num13 + 100))
                                    {
                                        this.Position(whichfish, num20 - 15.0, num21 + 58.0, 250.0);
                                    }
                                }
                                if (((num20 - (num7 + num9)) > -num16) && (num21 > num13))
                                {
                                    GlobalVar.decisions[whichfish].TCode = 12;
                                    GlobalVar.decisions[whichfish].VCode = 1;
                                }
                            }
                            GlobalVar.Rescue_boundary_Zhuangtai[whichfish] = 0;
                        }
                    }
                    else
                    {
                        if (((Math.Abs(num8) - Math.Abs(num21)) < 50.0) || ((Math.Abs(num7) - Math.Abs(num20)) < -80.0))
                        {
                            this.Position(whichfish, num20 - 80.0, num21 + 30.0, 300.0);
                        }
                        else
                        {
                            if (((GlobalVar.Rescue_boundary_zt1[whichfish] == 0) && (num21 < (num13 + 15))) || ((GlobalVar.Rescue_boundary_zt1[whichfish] != 0) && (num21 < (num13 - 15))))
                            {
                                this.Position(whichfish, num20 - 15.0, num21 + 40.0, 300.0);
                                GlobalVar.Rescue_boundary_zt1[whichfish] = 0;
                            }
                            else
                            {
                                GlobalVar.Rescue_boundary_zt1[whichfish] = 1;
                                if (num21 > num13)
                                {
                                    if ((velocityDirectionRad < ((20.0 * pI) / 180.0)) || (velocityDirectionRad > ((75.0 * pI) / 180.0)))
                                    {
                                        this.Angle(whichfish, (45.0 * pI) / 180.0);
                                        this.KeepV(whichfish, 5.0);
                                    }
                                    else
                                    {
                                        this.Angle(whichfish, (45.0 * pI) / 180.0);
                                        this.KeepV(whichfish, 300.0);
                                    }
                                }
                                if (num21 < (num13 + 80))
                                {
                                    this.Position(whichfish, num20 + 15.0, num21 + 65.0, 250.0);
                                }
                            }
                            if (((num20 - (num7 + num9)) < num14) && (num21 > num13))
                            {
                                GlobalVar.decisions[whichfish].TCode = 2;
                                GlobalVar.decisions[whichfish].VCode = 1;
                            }
                        }
                        GlobalVar.Rescue_boundary_Zhuangtai[whichfish] = 1;
                    }
                }
                else
                {
                    num11 = ((num21 - num15) - num8) / (num20 - num7);
                    num12 = num8 - (num11 * num7);
                    if (((GlobalVar.Rescue_boundary_Zhuangtai[whichfish] == 0) && ((num20 - 150.0) < x)) || ((GlobalVar.Rescue_boundary_Zhuangtai[whichfish] != 0) && ((num20 + 150.0) < x)))
                    {
                        if (Math.Abs(num21) > 800.0)
                        {
                            GlobalVar.Rescue_boundary_Zhuangtai[whichfish] = 0;
                            if ((num20 - num7) > 90.0)
                            {
                                GlobalVar.Rescue_boundary_Zhuangtai[whichfish] = 1;
                            }
                            this.Position(whichfish, num20 - 150.0, num21 + 100.0, 300.0);
                        }
                        else
                        {
                            if (((Math.Abs(num8) - Math.Abs(num21)) < 50.0) || ((Math.Abs(num7) - Math.Abs(num20)) > 80.0))
                            {
                                this.Position(whichfish, num20 + 80.0, num21 - 30.0, 300.0);
                            }
                            else
                            {
                                if (((GlobalVar.Rescue_boundary_zt1[whichfish] == 0) && (Math.Abs(num21) < (num13 + 15))) || ((GlobalVar.Rescue_boundary_zt1[whichfish] != 0) && (Math.Abs(num21) < (num13 - 15))))
                                {
                                    this.Position(whichfish, num20 + 12.0, num21 - 35.0, 300.0);
                                    GlobalVar.Rescue_boundary_zt1[whichfish] = 0;
                                }
                                else
                                {
                                    GlobalVar.Rescue_boundary_zt1[whichfish] = 1;
                                    if (Math.Abs(num21) > num13)
                                    {
                                        if ((velocityDirectionRad > ((-110.0 * pI) / 180.0)) || (velocityDirectionRad < ((-165.0 * pI) / 180.0)))
                                        {
                                            this.Angle(whichfish, (-135.0 * pI) / 180.0);
                                            this.KeepV(whichfish, 5.0);
                                        }
                                        else
                                        {
                                            this.Angle(whichfish, (-135.0 * pI) / 180.0);
                                            this.KeepV(whichfish, 300.0);
                                        }
                                    }
                                    if (Math.Abs(num21) < (num13 + 100))
                                    {
                                        this.Position(whichfish, num20 - 12.0, num21 - 58.0, 250.0);
                                    }
                                }
                                if (((num20 - (num7 + num9)) > -num16) && (Math.Abs(num21) > num13))
                                {
                                    GlobalVar.decisions[whichfish].TCode = 2;
                                    GlobalVar.decisions[whichfish].VCode = 1;
                                }
                            }
                            GlobalVar.Rescue_boundary_Zhuangtai[whichfish] = 0;
                        }
                    }
                    else
                    {
                        GlobalVar.deadjudge[whichfish] = 0;
                        if (((Math.Abs(num8) - Math.Abs(num21)) < 50.0) || ((Math.Abs(num7) - Math.Abs(num20)) < -80.0))
                        {
                            this.Position(whichfish, num20 - 80.0, num21 - 30.0, 300.0);
                            GlobalVar.deadjudge[whichfish] = 1;
                        }
                        else
                        {
                            if (((GlobalVar.Rescue_boundary_zt1[whichfish] == 0) && (Math.Abs(num21) < (num13 + 15))) || ((GlobalVar.Rescue_boundary_zt1[whichfish] != 0) && (Math.Abs(num21) < (num13 - 15))))
                            {
                                this.Position(whichfish, num20 - 15.0, num21 - 40.0, 300.0);
                                GlobalVar.Rescue_boundary_zt1[whichfish] = 0;
                            }
                            else
                            {
                                GlobalVar.Rescue_boundary_zt1[whichfish] = 1;
                                if (Math.Abs(num21) > num13)
                                {
                                    if ((velocityDirectionRad > ((-20.0 * pI) / 180.0)) || (velocityDirectionRad < ((-75.0 * pI) / 180.0)))
                                    {
                                        this.Angle(whichfish, (-45.0 * pI) / 180.0);
                                        this.KeepV(whichfish, 5.0);
                                    }
                                    else
                                    {
                                        this.Angle(whichfish, (-45.0 * pI) / 180.0);
                                        this.KeepV(whichfish, 300.0);
                                    }
                                }
                                if (Math.Abs(num21) < (num13 + 80))
                                {
                                    this.Position(whichfish, num20 + 15.0, num21 - 65.0, 250.0);
                                }
                            }
                            if (((num20 - (num7 + num9)) < num14) && (Math.Abs(num21) > num13))
                            {
                                GlobalVar.decisions[whichfish].TCode = 12;
                                GlobalVar.decisions[whichfish].VCode = 1;
                            }
                        }
                        GlobalVar.Rescue_boundary_Zhuangtai[whichfish] = 1;
                    }
                }
                if ((GlobalVar.Rescue_boundary_zt2[whichfish] == 1) || (num20 > 900.0))
                {
                    GlobalVar.Rescue_boundary_zt2[whichfish] = 1;
                    GlobalVar.Rescue_boundary_Zhuangtai[whichfish] = 0;
                    this.Dribble_boundaryf(whichfish, whichball);
                    if (num20 < -300.0)
                    {
                        GlobalVar.Rescue_boundary_zt2[whichfish] = 0;
                    }
                }
            }
            if (Math.Abs(num20) > 1000.0)
            {
                if (num20 < 0.0)
                {
                    if ((((GlobalVar.Rescue_boundary_Zhuangtai1[whichfish] == 0) && ((num21 - 150.0) < z)) || ((GlobalVar.Rescue_boundary_Zhuangtai1[whichfish] != 0) && ((num21 + 150.0) < z))) && (velocityDirectionRad < (-GlobalVar.PI / 15.0)))
                    {
                        if (((Math.Abs(num7) - Math.Abs(num20)) < 30.0) || ((num8 - num21) > 100.0))
                        {
                            this.Position(whichfish, num20 - 50.0, num21 + 80.0, 300.0);
                        }
                        else
                        {
                            if (((GlobalVar.Rescue_boundary_zt1[whichfish] == 0) && (num20 > (-num17 - 15))) || ((GlobalVar.Rescue_boundary_zt1[whichfish] != 0) && (num20 > (-num17 + 15))))
                            {
                                this.Position(whichfish, num20 - 40.0, num21 + 15.0, 300.0);
                                GlobalVar.Rescue_boundary_zt1[whichfish] = 0;
                            }
                            else
                            {
                                GlobalVar.Rescue_boundary_zt1[whichfish] = 1;
                                if (num20 < -num17)
                                {
                                    if ((velocityDirectionRad > ((-110.0 * pI) / 180.0)) || (velocityDirectionRad < ((-150.0 * pI) / 180.0)))
                                    {
                                        this.Angle(whichfish, (-130.0 * pI) / 180.0);
                                        this.KeepV(whichfish, 5.0);
                                        if ((velocityDirectionRad > ((-100.0 * pI) / 180.0)) && (velocityDirectionRad < 0.0))
                                        {
                                            if (velocityDirectionRad > ((-90.0 * pI) / 180.0))
                                            {
                                                GlobalVar.decisions[whichfish].TCode = 5;
                                            }
                                            GlobalVar.decisions[whichfish].TCode = 3;
                                        }
                                    }
                                    else
                                    {
                                        this.Angle(whichfish, (-130.0 * pI) / 180.0);
                                        this.KeepV(whichfish, 350.0);
                                    }
                                }
                                if (num20 > -num17)
                                {
                                    this.Dribble(whichfish, whichball, num20 + 200.0, num21 - 350.0);
                                }
                            }
                            if (((num21 - (num8 + num10)) > (-num14 + 15)) && (num20 < -num17))
                            {
                                GlobalVar.decisions[whichfish].TCode = 12;
                                GlobalVar.decisions[whichfish].VCode = 1;
                            }
                        }
                        GlobalVar.Rescue_boundary_Zhuangtai1[whichfish] = 0;
                    }
                    else
                    {
                        if (((Math.Abs(num7) - Math.Abs(num20)) < 30.0) || ((num8 - num21) < -100.0))
                        {
                            this.Position(whichfish, num20 - 50.0, num21 - 80.0, 300.0);
                        }
                        else
                        {
                            if (((GlobalVar.Rescue_boundary_zt1[whichfish] == 0) && (num20 > (-num17 - 15))) || ((GlobalVar.Rescue_boundary_zt1[whichfish] != 0) && (num20 > (-num17 + 15))))
                            {
                                this.Position(whichfish, num20 - 40.0, num21 - 15.0, 300.0);
                                GlobalVar.Rescue_boundary_zt1[whichfish] = 0;
                            }
                            else
                            {
                                GlobalVar.Rescue_boundary_zt1[whichfish] = 1;
                                if (num20 < -num17)
                                {
                                    if (((velocityDirectionRad < ((110.0 * pI) / 180.0)) && (velocityDirectionRad > 0.0)) || (velocityDirectionRad > ((150.0 * pI) / 180.0)))
                                    {
                                        this.Angle(whichfish, (130.0 * pI) / 180.0);
                                        this.KeepV(whichfish, 5.0);
                                        if ((velocityDirectionRad < ((100.0 * pI) / 180.0)) && (velocityDirectionRad > 0.0))
                                        {
                                            if (velocityDirectionRad < ((90.0 * pI) / 180.0))
                                            {
                                                GlobalVar.decisions[whichfish].TCode = 9;
                                            }
                                            GlobalVar.decisions[whichfish].TCode = 11;
                                        }
                                    }
                                    else
                                    {
                                        this.Angle(whichfish, (130.0 * pI) / 180.0);
                                        this.KeepV(whichfish, 350.0);
                                    }
                                }
                                if (num20 > (-num17 - 80))
                                {
                                    this.Position(whichfish, num20 - 65.0, num21 + 15.0, 250.0);
                                }
                            }
                            if (((num21 - (num8 + num10)) < (num14 - 15)) && (num20 < -num17))
                            {
                                GlobalVar.decisions[whichfish].TCode = 2;
                                GlobalVar.decisions[whichfish].VCode = 1;
                            }
                        }
                        GlobalVar.Rescue_boundary_Zhuangtai1[whichfish] = 1;
                    }
                }
                if (((velocityDirectionRad > ((-10.0 * pI) / 180.0)) && (velocityDirectionRad < ((10.0 * pI) / 180.0))) && (this.Compute_Distance(num20, num21, x, z) < 200.0))
                {
                    if (num21 > z)
                    {
                        GlobalVar.Rescue_boundary_tail[whichfish] = 2;
                    }
                    else
                    {
                        GlobalVar.Rescue_boundary_tail[whichfish] = 1;
                    }
                }
                if (GlobalVar.Rescue_boundary_tail[whichfish] == 1)
                {
                    GlobalVar.decisions[whichfish].VCode = 1;
                    this.KeepV(whichfish, 5.0);
                    this.Angle(whichfish, (145.0 * pI) / 180.0);
                    this.KeepV(whichfish, 0.0);
                    GlobalVar.decisions[whichfish].VCode = 1;
                    if (velocityDirectionRad < ((135.0 * pI) / 180.0))
                    {
                        GlobalVar.decisions[whichfish].TCode = 14;
                    }
                    if (((velocityDirectionRad > ((130.0 * pI) / 180.0)) || (num20 > -1045.0)) || (this.Compute_Distance(num20, num21, x, z) > 300.0))
                    {
                        GlobalVar.Rescue_boundary_tail[whichfish] = 0;
                        GlobalVar.Jinqiu_Rescue[whichfish] = 0;
                    }
                }
                if (GlobalVar.Rescue_boundary_tail[whichfish] == 2)
                {
                    GlobalVar.decisions[whichfish].VCode = 1;
                    this.KeepV(whichfish, 0.0);
                    this.Angle(whichfish, (-145.0 * pI) / 180.0);
                    if (((velocityDirectionRad < ((-130.0 * pI) / 180.0)) || (num20 > -1045.0)) || (this.Compute_Distance(num20, num21, x, z) > 300.0))
                    {
                        GlobalVar.Rescue_boundary_tail[whichfish] = 0;
                        GlobalVar.Jinqiu_Rescue[whichfish] = 0;
                    }
                }
            }
        }

        public void Dribble_boundaryf2(int whichfish, int whichball)
        {
            double pI = GlobalVar.PI;
            RoboFish fish = GlobalVar.MyTeam.Fishes[whichfish];
            double velocityDirectionRad = fish.VelocityDirectionRad;
            double x = fish.PositionMm.X;
            double z = fish.PositionMm.Z;
            double velocityMmPs = fish.VelocityMmPs;
            double angularVelocityRadPs = fish.AngularVelocityRadPs;
            double num7 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].X;
            double num8 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].Z;
            double num9 = GlobalVar.mission.EnvRef.Balls[whichball].VelocityDirectionRad;
            double num10 = GlobalVar.mission.EnvRef.Balls[whichball].VelocityMmPs;
            double dx = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.X;
            double num12 = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.Z;
            int num13 = 0x4ec;
            int num14 = 850;
            if ((dx > num13) && (Math.Abs(num12) < (num14 + 50)))
            {
                if (GlobalVar.Dribble_boundaryf_zt1[whichfish] == 100)
                {
                    if (num12 < 0.0)
                    {
                        GlobalVar.Dribble_boundaryf_zt1[whichfish] = 1;
                    }
                    else
                    {
                        GlobalVar.Dribble_boundaryf_zt1[whichfish] = 0;
                    }
                }
                if (Math.Abs(num12) > 100.0)
                {
                    GlobalVar.Dribble_boundaryf_zt1[whichfish] = 100;
                }
                if ((num12 > 100.0) || (GlobalVar.Dribble_boundaryf_zt1[whichfish] == 1))
                {
                    this.boundary_dribble(whichfish, whichball, 2, true);
                }
                else if ((num12 < -100.0) || (GlobalVar.Dribble_boundaryf_zt1[whichfish] == 0))
                {
                    this.boundary_dribble(whichfish, whichball, 2, false);
                }
            }
            else if ((dx < -num13) && (Math.Abs(num12) < 700.0))
            {
                if (GlobalVar.Dribble_boundaryf_zt2[whichfish] == 100)
                {
                    if (num12 < 0.0)
                    {
                        GlobalVar.Dribble_boundaryf_zt2[whichfish] = 1;
                    }
                    else
                    {
                        GlobalVar.Dribble_boundaryf_zt2[whichfish] = 0;
                    }
                }
                if (Math.Abs(num12) > 560.0)
                {
                    GlobalVar.Dribble_boundaryf_zt2[whichfish] = 100;
                }
                if ((num12 < -560.0) || (GlobalVar.Dribble_boundaryf_zt2[whichfish] == 1))
                {
                    this.boundary_dribble(whichfish, whichball, 0, false);
                }
                if ((num12 > 560.0) || (GlobalVar.Dribble_boundaryf_zt2[whichfish] == 0))
                {
                    this.boundary_dribble(whichfish, whichball, 0, true);
                }
            }
            else
            {
                int num15 = 1;
                if (whichfish == 0)
                {
                    num15 = 1;
                }
                else
                {
                    num15 = -1;
                }
                if ((GlobalVar.Paishu > 0xbb8) || (GlobalVar.final == 1))
                {
                    if (num12 > 300.0)
                    {
                        num15 = 1;
                    }
                    else if (num12 < -300.0)
                    {
                        num15 = -1;
                    }
                }
                else if ((GlobalVar.Paishu > 0x5dc) || (GlobalVar.final == 1))
                {
                    if (num12 > 700.0)
                    {
                        num15 = 1;
                    }
                    else if (num12 < -700.0)
                    {
                        num15 = -1;
                    }
                }
                if ((num12 * num15) < 500.0)
                {
                    if ((num12 * num15) < -750.0)
                    {
                        this.Rescue_boundary(whichfish, whichball, -1000.0, 0.0);
                    }
                    else
                    {
                        this.Dribble(whichfish, whichball, dx, (double)(num15 * 0x3e8));
                    }
                }
                else
                {
                    this.Dribbleboundary(whichfish, whichball);
                }
            }
        }

        public void dribble4(int which, int whichball, double dx, double dz)
        {
            double num = 1.0;
            if ((whichball >= 0) && (whichball <= 2))
            {
                num = (num * 62.0) / 58.0;
            }
            else if ((whichball >= 3) && (whichball <= 6))
            {
                num = (num * 78.0) / 58.0;
            }
            double pI = GlobalVar.PI;
            RoboFish fish = GlobalVar.MyTeam.Fishes[which];
            double velocityDirectionRad = fish.VelocityDirectionRad;
            double x = fish.PositionMm.X;
            double z = fish.PositionMm.Z;
            double velocityMmPs = fish.VelocityMmPs;
            double angularVelocityRadPs = fish.AngularVelocityRadPs;
            double d = GlobalVar.mission.EnvRef.Balls[whichball].VelocityDirectionRad;
            double num9 = GlobalVar.mission.EnvRef.Balls[whichball].VelocityMmPs;
            double num10 = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.X;
            double num11 = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.Z;
            x += (velocityMmPs * Math.Cos(velocityDirectionRad)) * 0.1;
            z += (velocityMmPs * Math.Sin(velocityDirectionRad)) * 0.1;
            velocityDirectionRad += angularVelocityRadPs * 0.1;
            num10 += (GlobalVar.mission.EnvRef.Balls[0].VelocityMmPs * Math.Cos(d)) * 0.1;
            num11 += (GlobalVar.mission.EnvRef.Balls[0].VelocityMmPs * Math.Sin(d)) * 0.1;
            double num12 = x + (GlobalVar.Fish_centertohead * Math.Cos(velocityDirectionRad));
            double num13 = z + (GlobalVar.Fish_centertohead * Math.Sin(velocityDirectionRad));
            if ((((dx > 1900.0) && (this.Compute_Distance(num12, num13, dx, dz) > 300.0)) && ((Math.Abs(num13) >= 200.0) && (num12 > 1500.0))) && (Math.Abs(num13) < 900.0))
            {
                Platform pointd7 = new Platform();
                double num32 = this.GetAngleRad(num12, num13, dx, dz);
                dx += 550.0 * Math.Cos(num32);
                dz += 550.0 * Math.Sin(num32);
            }
            else if ((((dx > 1900.0) && (this.Compute_Distance(num12, num13, dx, dz) > 300.0)) && (num12 > 1500.0)) && (Math.Abs(num13) < 900.0))
            {
                dx += 1000.0;
            }
            double num14 = this.GetAngleRad(num10, num11, dx, dz);
            double num15 = Math.Atan2(num11 - z, num10 - x);
            double num16 = num14 - num15;
            if (num16 < -pI)
            {
                num16 += 2.0 * pI;
            }
            if (num16 > pI)
            {
                num16 -= 2.0 * pI;
            }
            if (num16 > (pI / 2.0))
            {
                num14 = num15 + (pI / 2.0);
                num16 = pI / 2.0;
            }
            if (num16 < (-pI / 2.0))
            {
                num14 = num15 - (pI / 2.0);
                num16 = -pI / 2.0;
            }
            double num17 = 30.0 + ((((num16 / 1.5) + 0.2) * ((num16 / 1.5) + 0.2)) * 35.0);
            num17 *= num;
            double num18 = this.GetAngleRad(x, z, num10, num11);
            Platform pointd = new Platform(0.0, 0.0)
            {
                Posx = num10 - (num17 * Math.Cos(num14)),
                Posz = num11 - (num17 * Math.Sin(num14))
            };
            num17 = 30.0;
            num17 *= num;
            double drad = this.GetAngleRad(x, z, pointd.Posx, pointd.Posz);
            double num21 = this.FormatAngleRad(this.GetAngleRad(num12, num13, pointd.Posx, pointd.Posz) - velocityDirectionRad);
            double num22 = Math.Sqrt(((num12 - pointd.Posx) * (num12 - pointd.Posx)) + ((num13 - pointd.Posz) * (num13 - pointd.Posz)));
            double num23 = num10 - ((50.0 * num) * Math.Cos(num14));
            double num24 = num11 - ((50.0 * num) * Math.Sin(num14));
            Platform pointd2 = this.GetLineAcrossPoint(x, z, (velocityDirectionRad / pI) * 180.0, num23, num24, 90.0 + ((num14 / pI) * 180.0));
            double num25 = Math.Sqrt(((pointd2.Posx - num10) * (pointd2.Posx - num10)) + ((pointd2.Posz - num11) * (pointd2.Posz - num11)));
            Platform pointd3 = new Platform();
            Platform pointd4 = new Platform();
            pointd3.Posx = num23 - ((30.0 * num) * Math.Cos((pI / 2.0) + num14));
            pointd3.Posz = num24 - ((30.0 * num) * Math.Sin((pI / 2.0) + num14));
            pointd4.Posx = num23 - ((30.0 * num) * Math.Cos((-pI / 2.0) + num14));
            pointd4.Posz = num24 - ((30.0 * num) * Math.Sin((-pI / 2.0) + num14));
            double num26 = this.GetAngle(num10, num11, pointd3.Posx, pointd3.Posz);
            double num27 = this.GetAngle(num10, num11, pointd4.Posx, pointd4.Posz);
            double num28 = this.FormatAngle(((velocityDirectionRad * 180.0) / pI) + 180.0);
            Platform pointd5 = new Platform();
            Platform pointd6 = new Platform();
            double num29 = 42.0 * num;
            pointd5.Posx = (num23 + ((10.0 * num) * Math.Cos(num14))) - (num29 * Math.Cos((pI / 2.0) + num14));
            pointd5.Posz = (num24 + ((10.0 * num) * Math.Sin(num14))) - (num29 * Math.Sin((pI / 2.0) + num14));
            pointd6.Posx = (num23 + ((10.0 * num) * Math.Cos(num14))) - (num29 * Math.Cos((-pI / 2.0) + num14));
            pointd6.Posz = (num24 + ((10.0 * num) * Math.Sin(num14))) - (num29 * Math.Sin((-pI / 2.0) + num14));
            double num30 = this.GetAngle(num10, num11, pointd5.Posx, pointd5.Posz);
            double num31 = this.GetAngle(num10, num11, pointd6.Posx, pointd6.Posz);
            if ((((num22 < (150.0 * num)) && (Math.Abs((double)((num21 / pI) * 180.0)) > 15.0)) && ((num25 > 54.0) || !this.IsAngleDuring(num30, num31, num28))) || (Math.Abs((double)((num21 / pI) * 180.0)) > 90.0))
            {
                GlobalVar.decisions[which].VCode = 1;
                this.Angle(which, drad);
                this.KeepV(which, 1.0);
            }
            else
            {
                double num33 = this.FormatAngleRad(((2.0 * pI) + num14) - num18);
                double num34 = this.FormatAngleRad(((2.0 * pI) + num18) - velocityDirectionRad);
                if ((((num22 < (300.0 * num)) && (Math.Abs((double)((num33 / pI) * 180.0)) > 20.0)) && ((Math.Abs((double)((num33 / pI) * 180.0)) < 110.0) && ((num33 * num34) > 0.0))) && (Math.Abs((double)((num34 / pI) * 180.0)) < 30.0))
                {
                    if (Math.Abs((double)((num33 / pI) * 180.0)) < 90.0)
                    {
                        this.Angle(which, drad);
                        this.KeepV(which, 150.0);
                    }
                    else
                    {
                        this.Angle(which, drad);
                        this.KeepV(which, 180.0);
                    }
                }
                else if (num22 < 150.0)
                {
                    this.Angle(which, drad);
                    this.KeepV(which, 200.0);
                }
                else
                {
                    this.position3(which, pointd.Posx, pointd.Posz, 270.0);
                }
            }
        }

        public int DribbleActJudge()
        {
            for (int i = 0; i < 9; i++)
            {
                double ballPosX = GlobalVar.balls[i].PositionMm.X;
                double ballPosZ = GlobalVar.balls[i].PositionMm.Z;

                if (ballPosX > 1150.0 && Math.Abs(ballPosZ) < 695.0)
                {
                    return 1;
                }
            }
            return 0;
        }

        public int dribbleactsup(int whichfish)
        {
            int num = 0;
            for (int i = 0; i < 9; i++)
            {
                double x = GlobalVar.balls[i].PositionMm.X;
                double z = GlobalVar.balls[i].PositionMm.Z;
                double num5 = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.X;
                double num6 = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.Z;
                if (Math.Sqrt(((num5 - x) * (num5 - x)) + ((num6 - z) * (num6 - z))) <= 200.0)
                {
                    num = 1;
                }
            }
            if (num == 0)
            {
                return 0;
            }
            return 1;
        }

        public void Dribbleboundary(int whichfish, int whichball)
        {
            int i = (whichfish == 0) ? 6 : 10;
            double pI = GlobalVar.PI;
            RoboFish fish = GlobalVar.MyTeam.Fishes[whichfish];
            double velocityDirectionRad = fish.VelocityDirectionRad;
            double x = fish.PositionMm.X;
            double z = fish.PositionMm.Z;
            double velocityMmPs = fish.VelocityMmPs;
            double angularVelocityRadPs = fish.AngularVelocityRadPs;
            double num8 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].X;
            double num9 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].Z;
            double num10 = GlobalVar.mission.EnvRef.Balls[whichball].VelocityDirectionRad;
            double num11 = GlobalVar.mission.EnvRef.Balls[whichball].VelocityMmPs;
            double dx = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.X;
            double num13 = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.Z;
            if ((GlobalVar.Dribbleboundary_zt1[whichfish] == 0) && (Math.Abs(num13) > 900.0))
            {
                GlobalVar.Dribbleboundary_zt1[whichfish] = 1;
            }
            else if ((GlobalVar.Dribbleboundary_zt1[whichfish] == 1) && (((dx - x) > 180.0) || (Math.Abs(num13) < 650.0)))
            {
                GlobalVar.Dribbleboundary_zt1[whichfish] = 0;
            }
            if ((dx - num8) > 150.0)
            {
                GlobalVar.Dribbleboundary_zt2[whichfish] = 0;
            }
            if (GlobalVar.Dribbleboundary_zt1[whichfish] == 0)
            {
                if (num13 > 0.0)
                {
                    this.Dribble(whichfish, whichball, dx, 2000.0);
                }
                else
                {
                    this.Dribble(whichfish, whichball, dx, -2000.0);
                }
            }
            if (GlobalVar.Dribbleboundary_zt1[whichfish] == 1)
            {
                if (num13 > 0.0)
                {
                    if ((((((num8 - dx) >= 200.0) || ((num8 - dx) <= 0.0)) || ((num9 - num13) <= -50.0)) && (GlobalVar.Dribbleboundary_zt2[whichfish] == 0)) || (((((num8 - dx) >= 200.0) || ((num8 - dx) <= -200.0)) || ((num9 - num13) <= -50.0)) && (GlobalVar.Dribbleboundary_zt2[whichfish] == 1)))
                    {
                        if (Math.Abs(num13) < 900.0)
                        {
                            GlobalVar.Dribbleboundary_zt1[whichfish] = 0;
                        }
                        GlobalVar.Dribbleboundary_zt2[whichfish] = 0;
                        if (num8 < (dx + 30.0))
                        {
                            this.Position(whichfish, dx + 60.0, num13 - 80.0, 300.0);
                        }
                        else
                        {
                            this.Position(whichfish, dx + 80.0, 1000.0, 300.0);
                        }
                    }
                    else
                    {
                        GlobalVar.Dribbleboundary_zt2[whichfish] = 1;
                        GlobalVar.BaAction.Angle(whichfish, (115.0 * pI) / 180.0);
                        if ((velocityDirectionRad < ((130.0 * pI) / 180.0)) && (velocityDirectionRad > ((90.0 * pI) / 180.0)))
                        {
                            GlobalVar.decisions[whichfish].VCode = 9;
                            if (dx > 1200.0)
                            {
                                GlobalVar.decisions[whichfish].TCode = 8;
                            }
                        }
                        else
                        {
                            GlobalVar.decisions[whichfish].VCode = 1;
                        }
                        if ((num13 > 700.0) && (dx < -1400.0))
                        {
                            GlobalVar.decisions[whichfish].VCode = 8;
                            this.Angle(whichfish, (-155.0 * pI) / 180.0);
                            if ((velocityDirectionRad < ((-145.0 * pI) / 180.0)) || (velocityDirectionRad > ((170.0 * pI) / 180.0)))
                            {
                                GlobalVar.decisions[whichfish].TCode = 8;
                            }
                        }
                    }
                }
                if (num13 < 0.0)
                {
                    if ((((((x - dx) >= 200.0) || ((x - dx) <= 0.0)) || ((num9 - num13) >= 50.0)) && (GlobalVar.Dribbleboundary_zt2[whichfish] == 0)) || (((((x - dx) >= 200.0) || ((x - dx) <= -200.0)) || ((num9 - num13) >= 50.0)) && (GlobalVar.Dribbleboundary_zt2[whichfish] == 1)))
                    {
                        if (Math.Abs(num13) < 900.0)
                        {
                            GlobalVar.Dribbleboundary_zt1[whichfish] = 0;
                        }
                        GlobalVar.Dribbleboundary_zt2[whichfish] = 0;
                        if (num8 < (dx + 30.0))
                        {
                            this.Position(whichfish, dx + 60.0, num13 + 80.0, 300.0);
                        }
                        else
                        {
                            this.Position(whichfish, dx + 80.0, -1000.0, 300.0);
                        }
                    }
                    else
                    {
                        GlobalVar.Dribbleboundary_zt2[whichfish] = 1;
                        GlobalVar.BaAction.Angle(whichfish, (-105.0 * pI) / 180.0);
                        if ((velocityDirectionRad > ((-125.0 * pI) / 180.0)) && (velocityDirectionRad < ((-90.0 * pI) / 180.0)))
                        {
                            GlobalVar.decisions[whichfish].VCode = 10;
                            if (dx > 1200.0)
                            {
                                GlobalVar.decisions[whichfish].TCode = 8;
                            }
                        }
                        else
                        {
                            GlobalVar.decisions[whichfish].VCode = 1;
                        }
                        if ((num13 < -700.0) && (dx < -1400.0))
                        {
                            GlobalVar.decisions[whichfish].VCode = 8;
                            this.Angle(whichfish, (155.0 * pI) / 180.0);
                            if ((velocityDirectionRad < ((-170.0 * pI) / 180.0)) || (velocityDirectionRad > ((145.0 * pI) / 180.0)))
                            {
                                GlobalVar.decisions[whichfish].TCode = 6;
                            }
                        }
                    }
                }
            }
        }

        public void Dribblehometest1(int whichfish, int whichball)
        {
            if ((whichball >= 0) && (whichball <= 8))
            {
                int num5;
                double x = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.X;
                double z = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.Z;
                double radiusMm = GlobalVar.mission.EnvRef.Balls[whichball].RadiusMm;
                int num6 = this.GetUpperFish();
                double num7 = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.X;
                double num8 = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.Z;
                double num9 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[whichfish].X;
                double num10 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[whichfish].Z;
                double bodyDirectionRad = GlobalVar.MyTeam.Fishes[whichfish].BodyDirectionRad;
                if (whichfish == 0)
                {
                    num5 = 1;
                }
                else
                {
                    num5 = -1;
                }
                if ((GlobalVar.Paishu > 0xbb8) || (GlobalVar.final == 1))
                {
                    if (z > 500.0)
                    {
                        num5 = 1;
                    }
                    else if (z < -500.0)
                    {
                        num5 = -1;
                    }
                }
                else if ((GlobalVar.Paishu > 0x5dc) || (GlobalVar.final == 1))
                {
                    if (z > 700.0)
                    {
                        num5 = 1;
                    }
                    else if (z < -700.0)
                    {
                        num5 = -1;
                    }
                }
                GlobalVar.corner[whichfish] = 0;
                GlobalVar.corner_help[whichfish] = 0;
                if ((GlobalVar.flag_duqiumen == 1) && this.IsInGoal(num7, num8, -1))
                {
                    GlobalVar.enter_gate_stage[whichfish] = 0;
                }
                if (((GlobalVar.flag_duqiumen == 1) && this.IsInGoal(num7, num8, -1)) && this.IsInGoal(x, z, whichball))
                {
                    this.the_warrior2(whichfish, whichball);
                }
                else if (((GlobalVar.flag_duqiumen == 1) && this.IsInGoal(num7, num8, -1)) && !this.IsInGoal(x, z, whichball))
                {
                    if (this.Compute_Distance(num7, num8, -1192.0, (double)(num5 * 0x214)) > 100.0)
                    {
                        this.position3(whichfish, -1192.0, (double)(num5 * 0x214), 50.0);
                    }
                    else
                    {
                        this.position3(whichfish, -1160.0, (double)(num5 * 0x394), 170.0);
                    }
                }
                else if (((GlobalVar.flag_duqiumen == 1) && !this.IsInGoal(num7, num8, -1)) && this.IsInGoal(x, z, whichball))
                {
                    if (GlobalVar.whether_change_to_test3[whichfish] == 1)
                    {
                        GlobalVar.whether_change_to_test3[whichfish] = 0;
                    }
                    if (num7 > -970.0)
                    {
                        this.position3(whichfish, -932.0, (double)(num5 * 740), 80.0);
                    }
                    else if (this.Compute_Distance(num7, num8, -1180.0, (double)(num5 * 0x204)) < 100.0)
                    {
                        this.position3(whichfish, -1216.0, 0.0, 200.0);
                    }
                    else
                    {
                        this.position3(whichfish, -1180.0, (double)(num5 * 0x204), 80.0);
                    }
                }
                else if ((this.IsInMyUnArea_02(num7, num8) && (x > -949.0)) && (x < 949.0))
                {
                    this.Position(whichfish, -1500.0, (double)(num5 * 0x3e8), 200.0);
                }
                else if ((this.IsInMyUnArea_02(num7, num8) && (x > -1150.0)) && (Math.Abs(z) > 550.0))
                {
                    this.Position(whichfish, -1500.0, (double)(num5 * 0x3e8), 200.0);
                }
                else if ((((num7 < -949.0) && (Math.Abs(num8) < 900.0)) && (x > -949.0)) && (x < 949.0))
                {
                    this.Position(whichfish, -800.0, (double)(num5 * 700), 200.0);
                }
                else if (this.IsInOppUnArea_01(num7, num8) && !this.IsInOppUnArea_01(x, z))
                {

                    this.Position(whichfish, 1350.0, (double)(num5 * 700), 200.0);
                    if (Math.Abs(num8) > 600.0)
                    {
                        this.Position(whichfish, 800.0, (double)(num5 * 700), 200.0);
                    }
                }
                else if (this.IsInMyUnArea_02(num7, num8) && this.IsInOppUnArea_01(x, z))
                {
                    this.Position(whichfish, -1500.0, (double)(num5 * 0x3e8), 200.0);
                }
                else if (((num7 < -949.0) && (Math.Abs(num8) < 900.0)) && this.IsInOppUnArea_01(x, z))
                {
                    this.Position(whichfish, -800.0, (double)(num5 * 700), 200.0);
                }
                else if ((((Math.Abs(bodyDirectionRad) > ((5.0 * GlobalVar.PI) / 6.0)) && (num9 < -1400.0)) && (x > -1100.0)) && (Math.Abs(z) > 560.0))
                {
                    this.Position(whichfish, -1100.0, (double)(num5 * 700), 999.0);
                }
                else
                {
                    int num12;
                    if (z > 0.0)
                    {
                        num12 = 1;
                    }
                    else
                    {
                        num12 = -1;
                    }
                    if ((num7 > -920.0) && this.IsInMyUnArea_02((double)GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.Z))
                    {
                        GlobalVar.BaAction.Position(whichfish, -1100.0, (double)(num12 * 700), 200.0);
                    }
                    else if (((((whichball != 0) && (whichball != 1)) && ((whichball != 2) && (x >= -1400.0))) && ((x + radiusMm) < -1160.0)) && (Math.Abs(z) < 700.0))
                    {
                        GlobalVar.BaAction.position3_1(whichfish, -1800.0, z, 50.0, 0);
                        GlobalVar.decisions[whichfish].VCode = 14;
                    }
                    else
                    {
                        if (x > -1300.0)
                        {
                            if (whichfish == 1)
                            {
                                if (x > -952.0)
                                {
                                    if (GlobalVar.flag_duqiumen != 1)
                                    {
                                        this.Dribble(whichfish, whichball, -980.0, num5 * 645.0);
                                    }
                                    else
                                    {
                                        this.Dribble(whichfish, whichball, -980.0, num5 * 800.0);
                                    }
                                }
                                else if (x > -1150.0)
                                {
                                    this.Dribble(whichfish, whichball, -1230.0, num5 * 500.0);
                                }
                                else if (GlobalVar.flag_duqiumen != 1)
                                {
                                    if (Math.Abs(z) < 300.0)
                                    {
                                        this.Dribble(whichfish, whichball, -1180.0, num5 * 0.0);
                                    }
                                    else
                                    {
                                        this.Dribble(whichfish, whichball, -1180.0, -num5 * 300.0);
                                    }
                                }
                                else
                                {
                                    this.Dribble(whichfish, whichball, -1500.0, num5 * 580.0);
                                }
                            }
                            else if (GlobalVar.teamId == 1)
                            {
                                if (x > -900.0)
                                {
                                    this.Dribble(whichfish, whichball, -980.0, num5 * 700.0);
                                }
                                else if (x > -1100.0)
                                {
                                    this.KeepV(whichfish, 100.0);
                                    this.Dribble(whichfish, whichball, -1500.0, num5 * 600.0);
                                }
                                else if (GlobalVar.flag_duqiumen != 1)
                                {
                                    this.KeepV(whichfish, 100.0);
                                    this.Dribble(whichfish, whichball, -1500.0, num5 * 520.0);
                                }
                                else
                                {
                                    this.Dribble(whichfish, whichball, -1500.0, num5 * 580.0);
                                }
                            }
                            else
                            {
                                if (x > -952.0)
                                {
                                    this.Dribble(whichfish, whichball, -980.0, num5 * 665.0);
                                }
                                else if (x > -1150.0)
                                {
                                    if (GlobalVar.flag_duqiumen != 1)
                                    {
                                        this.KeepV(whichfish, 100.0);
                                        this.Dribble(whichfish, whichball, -1500.0, num5 * 600.0);
                                    }
                                    else
                                    {
                                        this.Dribble(whichfish, whichball, -1500.0, num5 * 580.0);
                                    }
                                }
                                else
                                {
                                    this.KeepV(whichfish, 100.0);
                                    this.Dribble(whichfish, whichball, -1500.0, num5 * 520.0);
                                }
                            }
                        }
                        else if (whichfish == 0)
                        {
                            if (x > -1380.0)
                            {
                                this.Dribble(whichfish, whichball, -1500.0, z - 35.0);
                            }
                            else if (this.Compute_Distance(num7, num8, x, z) > 150.0)
                            {
                                this.Position(whichfish, x, z + (num5 * 60), 100.0);
                            }
                            else if (GlobalVar.flag_duqiumen == 0)
                            {
                                this.Dribblehometest3(whichfish, whichball);
                            }
                            else
                            {
                                this.Dribble_blue(whichfish, -1500.0, z, whichball);
                            }
                        }
                        else if (whichfish == 1)
                        {
                            if (this.Compute_Distance(num7, num8, x, z) > 150.0)
                            {
                                this.Position(whichfish, x, z + (num5 * 60), 100.0);
                            }
                            else if (GlobalVar.flag_duqiumen == 0)
                            {
                                this.Dribblehometest3(whichfish, whichball);
                            }
                            else
                            {
                                this.Dribble_blue(whichfish, -1500.0, z, whichball);
                            }
                        }
                        if ((Math.Abs(z) > 750.0) || (GlobalVar.whether_change_to_test3[whichfish] == 1))
                        {
                            GlobalVar.whether_change_to_test3[whichfish] = 1;
                            if (((((Math.Abs((double)(num7 + 1200.0)) < 100.0) && (Math.Abs((double)(num8 - 550.0)) < 100.0)) && ((x < -900.0) && (x > -1000.0))) && (Math.Abs(z) > 500.0)) && (Math.Abs(z) < 650.0))
                            {
                                GlobalVar.whether_change_to_test3[whichfish] = 0;
                            }
                            if (((x < -1350.0) || ((x < -976.0) && (Math.Abs(z) < 600.0))) || ((x < -1280.0) && (Math.Abs(z) < 950.0)))
                            {
                                if (GlobalVar.flag_duqiumen == 0)
                                {
                                    this.Dribblehometest3(whichfish, whichball);
                                    if ((x < -1400.0) && ((z > 580.0) || (z < -580.0)))
                                    {
                                        this.Dribble_blue(whichfish, -1500.0, z, whichball);
                                    }
                                }
                                else
                                {
                                    this.Dribble_blue(whichfish, -1500.0, z, whichball);
                                }
                            }
                            else if (Math.Abs(z) > 750.0)
                            {
                                this.Dribble_boundaryf2(whichfish, whichball);
                            }
                        }
                        if (GlobalVar.flag_duqiumen == 0)
                        {
                            int index = GlobalVar.Bringing_BallID[0];
                            int num14 = GlobalVar.Bringing_BallID[1];
                            if ((index != -1) && (num14 != -1))
                            {
                                if ((GlobalVar.teamId == 0) && (whichfish == 1))
                                {
                                    if ((((whichfish == 0) && (GlobalVar.Bringing_BallID[0] != 6)) || ((whichfish == 1) && (GlobalVar.Bringing_BallID[1] != 3))) && ((((Math.Abs(num8) > 550.0) && (Math.Abs(num7) > 1000.0)) && (Math.Abs(num8) > (Math.Abs(GlobalVar.balls[3].PositionMm.Z) - 66f))) && (Math.Abs(num8) > (Math.Abs(GlobalVar.balls[num14].PositionMm.Z) - 66f))))
                                    {
                                        double num15 = GlobalVar.MyTeam.Fishes[whichfish].BodyDirectionRad;
                                        double pI = GlobalVar.PI;
                                        if (GlobalVar.jiaoluobaiwei[whichfish] == 1)
                                        {
                                            GlobalVar.decisions[whichfish].TCode = 14;
                                            GlobalVar.decisions[whichfish].VCode = 1;
                                            if ((Math.Abs((double)(num15 + (0.5 * pI))) < (pI / 8.0)) || (Math.Abs(num8) < Math.Abs(GlobalVar.balls[3].PositionMm.Z)))
                                            {
                                                GlobalVar.jiaoluobaiwei[whichfish] = 0;
                                                GlobalVar.liangqiutongchan[1] = 1;
                                            }
                                            if (Math.Abs(num8) < (Math.Abs(z) - 50.0))
                                            {
                                                GlobalVar.liangqiutongchan[1] = 1;
                                            }
                                        }
                                        if (((Math.Abs(z) > 300.0) && ((x < -1250.0) || ((GlobalVar.jiaoluobaiwei[whichfish] == 1) && (x < -1050.0)))) && (((whichfish == 1) && (GlobalVar.balls[3].PositionMm.X < -1200f)) && (GlobalVar.balls[3].PositionMm.Z < -300f)))
                                        {
                                            double num19;
                                            GlobalVar.jiaoluobaiwei[whichfish] = 1;
                                            double num17 = Math.Abs((double)(num10 - 1000.0));
                                            double num18 = GlobalVar.Fish_length;
                                            double num20 = 2.0 - ((3.0 * pI) / 2.0);
                                            if (num17 < num18)
                                            {
                                                num19 = Math.Acos(num17 / num18);
                                            }
                                            else
                                            {
                                                num19 = pI - 2.0;
                                            }
                                            if ((num5 * num20) > (num5 * ((-pI / 2.0) - num19)))
                                            {
                                                num20 = (-pI / 2.0) - num19;
                                            }
                                            num20 += 0.3;
                                            int num21 = this.Angle_V((num5 * num20) - num15);
                                            int num22 = Math.Abs((int)(9 - num21));
                                            if (((num5 * num15) > 0.0) || ((num5 * num15) < -(pI - 0.1)))
                                            {
                                                if ((num5 * num15) > 0.0)
                                                {
                                                    GlobalVar.decisions[whichfish].VCode = 5;
                                                    GlobalVar.decisions[whichfish].TCode = 7 + (num5 * 4);
                                                }
                                                else
                                                {
                                                    GlobalVar.decisions[whichfish].VCode = 5;
                                                    GlobalVar.decisions[whichfish].TCode = 7 + (num5 * 4);
                                                }
                                            }
                                            else if (Math.Abs(num10) > 750.0)
                                            {
                                                num20 = (-pI / 2.0) - num19;
                                                num20 -= 0.1;
                                                if (Math.Abs(num15) > Math.Abs(num20))
                                                {
                                                    GlobalVar.decisions[whichfish].TCode = 7 + (num5 * 2);
                                                    GlobalVar.decisions[whichfish].VCode = 2;
                                                }
                                                else
                                                {
                                                    GlobalVar.decisions[whichfish].TCode = 7;
                                                    GlobalVar.decisions[whichfish].VCode = 5;
                                                }
                                            }
                                            else if ((Math.Abs(num15) < (Math.Abs(num20) + 0.15)) && (Math.Abs(num15) > (Math.Abs(num20) - 0.15)))
                                            {
                                                GlobalVar.decisions[whichfish].TCode = 8;
                                                GlobalVar.decisions[whichfish].VCode = 12 - num22;
                                            }
                                            else if (Math.Abs(num15) < Math.Abs(num20))
                                            {
                                                if (Math.Abs((int)(GlobalVar.prefish[whichfish].preTcode - num21)) > 1)
                                                {
                                                    num21 = Math.Sign((int)(GlobalVar.prefish[whichfish].preTcode - num21)) + num21;
                                                }
                                                GlobalVar.decisions[whichfish].TCode = num21;
                                                num22 = 3 - num22;
                                                if (num22 < 1)
                                                {
                                                    num22 = 1;
                                                }
                                                if (Math.Abs((int)(GlobalVar.prefish[whichfish].preVcode - num22)) > 2)
                                                {
                                                    num22 = (num22 + GlobalVar.prefish[whichfish].preVcode) / 2;
                                                }
                                                GlobalVar.decisions[whichfish].VCode = num22;
                                            }
                                            else
                                            {
                                                GlobalVar.decisions[whichfish].TCode = num21;
                                                num22 = 9 - num22;
                                                if (num22 <= 6)
                                                {
                                                    num22 = 6;
                                                }
                                                GlobalVar.decisions[whichfish].VCode = num22;
                                            }
                                        }
                                    }
                                }
                                else if (((whichfish == 0) && (GlobalVar.Bringing_BallID[0] != 4)) || ((whichfish == 1) && (GlobalVar.Bringing_BallID[1] != 5)))
                                {
                                    if ((((whichfish == 0) && (Math.Abs(num8) > 550.0)) && ((Math.Abs(num7) > 1000.0) && (Math.Abs(num8) > (Math.Abs(GlobalVar.balls[4].PositionMm.Z) - 66f)))) && (Math.Abs(num8) > (Math.Abs(GlobalVar.balls[index].PositionMm.Z) - 66f)))
                                    {
                                        double num23 = GlobalVar.MyTeam.Fishes[whichfish].BodyDirectionRad;
                                        double num24 = GlobalVar.PI;
                                        if (GlobalVar.jiaoluobaiwei[whichfish] == 1)
                                        {
                                            GlobalVar.decisions[whichfish].TCode = 1;
                                            GlobalVar.decisions[whichfish].VCode = 1;
                                            if (((Math.Abs((double)(num23 - (0.5 * num24))) < (num24 / 8.0)) || (Math.Abs(num8) < Math.Abs(GlobalVar.balls[4].PositionMm.Z))) || (Math.Abs(num8) < 600.0))
                                            {
                                                GlobalVar.jiaoluobaiwei[whichfish] = 0;
                                            }
                                            if (Math.Abs(num8) < (Math.Abs(z) - 50.0))
                                            {
                                                GlobalVar.liangqiutongchan[0] = 1;
                                            }
                                        }
                                        if (((Math.Abs(z) > 300.0) && ((x < -1250.0) || ((GlobalVar.jiaoluobaiwei[whichfish] == 1) && (x < -1100.0)))) && (((whichfish == 0) && (GlobalVar.balls[4].PositionMm.X < -1200f)) && (GlobalVar.balls[4].PositionMm.Z > 300f)))
                                        {
                                            double num27;
                                            GlobalVar.jiaoluobaiwei[whichfish] = 1;
                                            double num25 = Math.Abs((double)(num10 - 1000.0));
                                            double num26 = GlobalVar.Fish_length;
                                            double num28 = 2.0 - ((3.0 * num24) / 2.0);
                                            if (num25 < num26)
                                            {
                                                num27 = Math.Acos(num25 / num26);
                                            }
                                            else
                                            {
                                                num27 = num24 - 2.0;
                                            }
                                            if ((num5 * num28) > (num5 * ((-num24 / 2.0) - num27)))
                                            {
                                                num28 = (-num24 / 2.0) - num27;
                                            }
                                            num28 += 0.3;
                                            int num29 = this.Angle_V((num5 * num28) - num23);
                                            int num30 = Math.Abs((int)(9 - num29));
                                            if (((num5 * num23) > 0.0) || ((num5 * num23) < -(num24 - 0.1)))
                                            {
                                                if ((num5 * num23) > 0.0)
                                                {
                                                    GlobalVar.decisions[whichfish].VCode = 6;
                                                    GlobalVar.decisions[whichfish].TCode = 7 + (num5 * 3);
                                                }
                                                else
                                                {
                                                    GlobalVar.decisions[whichfish].VCode = 6;
                                                    GlobalVar.decisions[whichfish].TCode = 7 + (num5 * 3);
                                                }
                                            }
                                            else if (Math.Abs(num10) > 750.0)
                                            {
                                                num28 = (-num24 / 2.0) - num27;
                                                num28 -= 0.1;
                                                if (Math.Abs(num23) > Math.Abs(num28))
                                                {
                                                    GlobalVar.decisions[whichfish].TCode = 7 + (num5 * 2);
                                                    GlobalVar.decisions[whichfish].VCode = 2;
                                                }
                                                else
                                                {
                                                    GlobalVar.decisions[whichfish].TCode = 7;
                                                    GlobalVar.decisions[whichfish].VCode = 5;
                                                }
                                            }
                                            else if ((Math.Abs(num23) < (Math.Abs(num28) + 0.15)) && (Math.Abs(num23) > (Math.Abs(num28) - 0.15)))
                                            {
                                                GlobalVar.decisions[whichfish].TCode = 7;
                                                GlobalVar.decisions[whichfish].VCode = 13 - num30;

                                            }
                                            else if (Math.Abs(num23) < Math.Abs(num28))
                                            {
                                                if (Math.Abs((int)(GlobalVar.prefish[whichfish].preTcode - num29)) > 1)
                                                {
                                                    num29 = Math.Sign((int)(GlobalVar.prefish[whichfish].preTcode - num29)) + num29;
                                                }
                                                GlobalVar.decisions[whichfish].TCode = num29;
                                                num30 = 3 - num30;
                                                if (num30 < 1)
                                                {
                                                    num30 = 1;
                                                }
                                                if (Math.Abs((int)(GlobalVar.prefish[whichfish].preVcode - num30)) > 2)
                                                {
                                                    num30 = (num30 + GlobalVar.prefish[whichfish].preVcode) / 2;
                                                }
                                                GlobalVar.decisions[whichfish].VCode = num30;
                                            }
                                            else
                                            {
                                                GlobalVar.decisions[whichfish].TCode = num29;
                                                num30 = 9 - num30;
                                                if (num30 <= 6)
                                                {
                                                    num30 = 6;
                                                }
                                                GlobalVar.decisions[whichfish].VCode = num30;
                                            }
                                        }
                                    }
                                    else if ((((Math.Abs(num8) > 550.0) && (Math.Abs(num7) > 1000.0)) && (Math.Abs(num8) > (Math.Abs(GlobalVar.balls[5].PositionMm.Z) - 66f))) && (Math.Abs(num8) > (Math.Abs(GlobalVar.balls[index].PositionMm.Z) - 66f)))
                                    {
                                        double num31 = GlobalVar.MyTeam.Fishes[whichfish].BodyDirectionRad;
                                        double num32 = GlobalVar.PI;
                                        if (GlobalVar.jiaoluobaiwei[whichfish] == 1)
                                        {
                                            GlobalVar.decisions[whichfish].TCode = 14;
                                            GlobalVar.decisions[whichfish].VCode = 1;
                                            if ((Math.Abs((double)(num31 + (0.5 * num32))) < (num32 / 8.0)) || (Math.Abs(num8) < Math.Abs(GlobalVar.balls[5].PositionMm.Z)))
                                            {
                                                GlobalVar.jiaoluobaiwei[whichfish] = 0;
                                                GlobalVar.liangqiutongchan[1] = 1;
                                            }
                                            if (Math.Abs(num8) < (Math.Abs(z) - 50.0))
                                            {
                                                GlobalVar.liangqiutongchan[1] = 1;
                                            }
                                        }
                                        if (((Math.Abs(z) > 300.0) && ((x < -1250.0) || ((GlobalVar.jiaoluobaiwei[whichfish] == 1) && (x < -1100.0)))) && (((whichfish == 1) && (GlobalVar.balls[5].PositionMm.X < -1200f)) && (GlobalVar.balls[5].PositionMm.Z < -300f)))
                                        {
                                            double num35;
                                            GlobalVar.jiaoluobaiwei[whichfish] = 1;
                                            double num33 = Math.Abs((double)(num10 - 1000.0));
                                            double num34 = GlobalVar.Fish_length;
                                            double num36 = 2.0 - ((3.0 * num32) / 2.0);
                                            if (num33 < num34)
                                            {
                                                num35 = Math.Acos(num33 / num34);
                                            }
                                            else
                                            {
                                                num35 = num32 - 2.0;
                                            }
                                            if ((num5 * num36) > (num5 * ((-num32 / 2.0) - num35)))
                                            {
                                                num36 = (-num32 / 2.0) - num35;
                                            }
                                            num36 += 0.3;
                                            int num37 = this.Angle_V((num5 * num36) - num31);
                                            int num38 = Math.Abs((int)(9 - num37));
                                            if (((num5 * num31) > 0.0) || ((num5 * num31) < -(num32 - 0.1)))
                                            {
                                                if ((num5 * num31) > 0.0)
                                                {
                                                    GlobalVar.decisions[whichfish].VCode = 5;
                                                    GlobalVar.decisions[whichfish].TCode = 7 + (num5 * 4);
                                                }
                                                else
                                                {
                                                    GlobalVar.decisions[whichfish].VCode = 5;
                                                    GlobalVar.decisions[whichfish].TCode = 7 + (num5 * 4);
                                                }
                                            }
                                            else if (Math.Abs(num10) > 750.0)
                                            {
                                                num36 = (-num32 / 2.0) - num35;
                                                num36 -= 0.1;
                                                if (Math.Abs(num31) > Math.Abs(num36))
                                                {
                                                    GlobalVar.decisions[whichfish].TCode = 7 + (num5 * 2);
                                                    GlobalVar.decisions[whichfish].VCode = 2;
                                                }
                                                else
                                                {
                                                    GlobalVar.decisions[whichfish].TCode = 7;
                                                    GlobalVar.decisions[whichfish].VCode = 5;
                                                }
                                            }
                                            else if ((Math.Abs(num31) < (Math.Abs(num36) + 0.15)) && (Math.Abs(num31) > (Math.Abs(num36) - 0.15)))
                                            {
                                                GlobalVar.decisions[whichfish].TCode = 8;
                                                GlobalVar.decisions[whichfish].VCode = 12 - num38;
                                            }
                                            else if (Math.Abs(num31) < Math.Abs(num36))
                                            {
                                                if (Math.Abs((int)(GlobalVar.prefish[whichfish].preTcode - num37)) > 1)
                                                {
                                                    num37 = Math.Sign((int)(GlobalVar.prefish[whichfish].preTcode - num37)) + num37;
                                                }
                                                GlobalVar.decisions[whichfish].TCode = num37;
                                                num38 = 3 - num38;
                                                if (num38 < 1)
                                                {
                                                    num38 = 1;
                                                }
                                                if (Math.Abs((int)(GlobalVar.prefish[whichfish].preVcode - num38)) > 2)
                                                {
                                                    num38 = (num38 + GlobalVar.prefish[whichfish].preVcode) / 2;
                                                }
                                                GlobalVar.decisions[whichfish].VCode = num38;
                                            }
                                            else
                                            {
                                                GlobalVar.decisions[whichfish].TCode = num37;
                                                num38 = 9 - num38;
                                                if (num38 <= 6)
                                                {
                                                    num38 = 6;
                                                }
                                                GlobalVar.decisions[whichfish].VCode = num38;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        GlobalVar.boundary_dribbleact[0] = 0;
                        GlobalVar.boundary_dribbleact[1] = 0;
                    }
                }
            }
        }

        public void Dribblehometest3(int whichfish, int whichball)
        {
            int num;
            double x = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.X;
            double z = GlobalVar.mission.EnvRef.Balls[whichball].PositionMm.Z;
            double num4 = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.X;
            double num5 = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.Z;
            double num6 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].X;
            double num7 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].Z;
            double velocityMmPs = GlobalVar.MyTeam.Fishes[whichfish].VelocityMmPs;
            double velocityDirectionRad = GlobalVar.MyTeam.Fishes[whichfish].VelocityDirectionRad;
            double pI = GlobalVar.PI;
            if (whichfish == 0)
            {
                num = 1;
            }
            else
            {
                num = -1;
            }
            if ((GlobalVar.Paishu > 0xbb8) || (GlobalVar.final == 1))
            {
                if (z > 300.0)
                {
                    num = 1;
                }
                else if (z < -300.0)
                {
                    num = -1;
                }
            }
            else if ((GlobalVar.Paishu > 0x5dc) || (GlobalVar.final == 1))
            {
                if (z > 700.0)
                {
                    num = 1;
                }
                else if (z < -700.0)
                {
                    num = -1;
                }
            }
            switch (num)
            {
                case 1:
                    if (((x > -1420.0) && (((x <= -1420.0) || (x >= -978.0)) || (Math.Abs(z) >= 507.0))) && (((x <= -1420.0) || (x >= -1200.0)) || (Math.Abs(z) >= 740.0)))
                    {
                        if (Math.Abs(z) < 800.0)
                        {
                            if (x < -978.0)
                            {
                                if ((1500.0 - Math.Abs(x)) < (1000.0 - Math.Abs(z)))
                                {
                                    this.Dribble(whichfish, whichball, -1500.0, z - (num * 50));
                                }
                                else
                                {
                                    this.Dribble(whichfish, whichball, x - 100.0, (double)(num * 0x3e8));
                                }
                            }
                            else
                            {
                                this.Dribble(whichfish, whichball, x - 100.0, (double)(num * 0x3e8));
                            }
                        }
                        else
                        {
                            if (((num6 < x) && ((((velocityDirectionRad * num) < (pI / 2.0)) || (Math.Abs(num7) < Math.Abs(z))) || ((num6 - x) < -10.0))) || (Math.Abs(num7) < 950.0))
                            {
                                double num11 = this.Compute_Distance(num6, num7, x, z);
                                if ((num6 < x) && (num11 < 100.0))
                                {
                                    GlobalVar.decisions[whichfish].VCode = 2;
                                    GlobalVar.decisions[whichfish].TCode = 7 - (num * 4);
                                }
                                else if (Math.Abs(num7) < 800.0)
                                {
                                    this.Position(whichfish, x + 80.0, (double)(num * 910), 150.0);
                                }
                                else
                                {
                                    this.Position(whichfish, x + 80.0, (double)(num * 0x3e8), 150.0);
                                }
                            }
                            else
                            {
                                int num12 = this.Angle_V((num * 2.0) - velocityDirectionRad);
                                int num13 = Math.Abs((int)(7 - num12));
                                if (Math.Abs(z) > 800.0)
                                {
                                    if ((Math.Abs(velocityDirectionRad) > 1.85) && (Math.Abs(velocityDirectionRad) < 2.15))
                                    {
                                        GlobalVar.decisions[whichfish].TCode = num12;
                                        GlobalVar.decisions[whichfish].VCode = 11 - num13;
                                    }
                                    else if (Math.Abs(velocityDirectionRad) > 2.0)
                                    {
                                        GlobalVar.decisions[whichfish].TCode = num12;
                                        num13 = 5 - num13;
                                        if (num13 < 1)
                                        {
                                            num13 = 1;
                                        }
                                        GlobalVar.decisions[whichfish].VCode = num13;
                                    }
                                    else
                                    {
                                        GlobalVar.decisions[whichfish].TCode = num12;
                                        if (num13 >= 2)
                                        {
                                            num13 = 2;
                                        }
                                        GlobalVar.decisions[whichfish].VCode = 10 - num13;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Math.Abs(z) > 200.0)
                        {
                            if (Math.Abs(z) > 780.0)
                            {
                                if ((num * velocityDirectionRad) >= 0.0)
                                {
                                    int num14 = 0x5dc - ((int)Math.Abs(num6));
                                    if (z > -800.0)
                                    {
                                        num14 += 10;
                                    }
                                    if (num14 > 20)
                                    {
                                        this.Position(whichfish, -1500.0, (double)(num * 0x3e8), 300.0);
                                    }
                                    else
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 2;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 4);
                                    }
                                }
                                else
                                {
                                    double num15 = 1000.0 - Math.Abs(num7);
                                    double num16 = GlobalVar.Fish_length;
                                    double num17 = Math.Acos(num15 / num16);
                                    if (num17 < 1.15)
                                    {
                                        num17 = 1.15;
                                    }
                                    if (Math.Abs(velocityDirectionRad) >= ((pI / 2.0) + num17))
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 1;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 5);
                                    }
                                    else
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 3;
                                        GlobalVar.decisions[whichfish].TCode = 7;
                                    }
                                }
                                if ((num6 - x) >= 60.0)
                                {
                                    if ((num * velocityDirectionRad) < -1.047)
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 1;
                                        GlobalVar.decisions[whichfish].TCode = 7 - (num * 7);
                                    }
                                    else if (Math.Abs(velocityDirectionRad) < 1.047)
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 1;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 7);
                                    }
                                    else if (num6 > -1000.0)
                                    {
                                        this.Position(whichfish, -1200.0, (double)(num * 0x3e8), 100.0);
                                    }
                                    else if (num6 < -1200.0)
                                    {
                                        this.Position(whichfish, -1500.0, (double)(num * 0x3e8), 100.0);
                                    }
                                }
                                else if (((Math.Abs(z) - Math.Abs(num7)) > 30.0) && ((num6 - x) < 60.0))
                                {
                                    GlobalVar.decisions[whichfish].VCode = 1;
                                    GlobalVar.decisions[whichfish].TCode = 7 - (num * 7);
                                }
                            }
                            else
                            {
                                double num20;
                                double num18 = Math.Abs((double)(num7 - 1000.0));
                                double num19 = GlobalVar.Fish_length;
                                double num21 = 2.0 - ((3.0 * pI) / 2.0);
                                if (num18 < num19)
                                {
                                    num20 = Math.Acos(num18 / num19);
                                }
                                else
                                {
                                    num20 = pI - 2.0;
                                }
                                if ((num * num21) > (num * ((-pI / 2.0) - num20)))
                                {
                                    num21 = (-pI / 2.0) - num20;
                                }
                                int num22 = this.Angle_V((num * num21) - velocityDirectionRad);
                                int num23 = Math.Abs((int)(7 - num22));
                                if (((num * velocityDirectionRad) > 0.0) || ((num * velocityDirectionRad) < -(pI - 0.3)))
                                {
                                    if ((num * velocityDirectionRad) > 0.0)
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 4;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 4);
                                        double num24 = num4 + ((velocityMmPs * 0.1) * Math.Cos(velocityDirectionRad));
                                        double num25 = num5 + ((velocityMmPs * 0.1) * Math.Sin(velocityDirectionRad));
                                        double num26 = this.Compute_Distance(num4, num5, -1500.0, -1000.0);
                                        double num27 = this.Compute_Distance(num24, num25, -1500.0, -1000.0);
                                        if ((num26 < 50.0) && (num27 < 50.0))
                                        {
                                            GlobalVar.decisions[whichfish].VCode = 1;
                                            if (num == 1)
                                            {
                                                GlobalVar.decisions[whichfish].TCode = 14;
                                            }
                                            else
                                            {
                                                GlobalVar.decisions[whichfish].VCode = 0;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 4;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 4);
                                    }
                                }
                                else if (Math.Abs(num7) > 750.0)
                                {
                                    num21 = (-pI / 2.0) - num20;
                                    num21 -= 0.1;
                                    if (Math.Abs(velocityDirectionRad) > Math.Abs(num21))
                                    {
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 2);
                                        GlobalVar.decisions[whichfish].VCode = 5;
                                    }
                                    else
                                    {
                                        GlobalVar.decisions[whichfish].TCode = 7;
                                        GlobalVar.decisions[whichfish].VCode = 7;
                                    }
                                }
                                else if ((Math.Abs(velocityDirectionRad) < (Math.Abs(num21) + 0.05)) && (Math.Abs(velocityDirectionRad) > (Math.Abs(num21) - 0.05)))
                                {
                                    GlobalVar.decisions[whichfish].TCode = 7;
                                    GlobalVar.decisions[whichfish].VCode = 12 - num23;
                                }
                                else if (Math.Abs(velocityDirectionRad) < Math.Abs(num21))
                                {
                                    if (Math.Abs((int)(GlobalVar.prefish[whichfish].preTcode - num22)) > 1)
                                    {
                                        num22 = Math.Sign((int)(GlobalVar.prefish[whichfish].preTcode - num22)) + num22;
                                    }
                                    GlobalVar.decisions[whichfish].TCode = num22;
                                    num23 = 9 - num23;
                                    if (num23 < 1)
                                    {
                                        num23 = 1;
                                    }
                                    if (Math.Abs((int)(GlobalVar.prefish[whichfish].preVcode - num23)) > 2)
                                    {
                                        num23 = (num23 + GlobalVar.prefish[whichfish].preVcode) / 2;
                                    }
                                    GlobalVar.decisions[whichfish].VCode = num23;
                                }
                                else
                                {
                                    GlobalVar.decisions[whichfish].TCode = num22;
                                    num23 = 3 - num23;
                                    if (num23 <= 6)
                                    {
                                        num23 = 6;
                                    }
                                    GlobalVar.decisions[whichfish].VCode = num23;
                                }
                                if ((num6 - x) >= 60.0)
                                {
                                    if ((num * velocityDirectionRad) < -1.047)
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 1;
                                        GlobalVar.decisions[whichfish].TCode = 7 - (num * 7);
                                    }
                                    else if (Math.Abs(velocityDirectionRad) < 1.047)
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 1;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 7);
                                    }
                                    if (num6 > -1000.0)
                                    {
                                        this.Position(whichfish, -1200.0, z + (num * 50), 200.0);
                                    }
                                    else if (num6 < -1200.0)
                                    {
                                        this.Position(whichfish, -1500.0, z + (num * 0x41), 100.0);
                                    }
                                }
                                else if (((Math.Abs(z) - Math.Abs(num7)) > 30.0) && ((num6 - x) < 60.0))
                                {
                                    GlobalVar.decisions[whichfish].VCode = 1;
                                    GlobalVar.decisions[whichfish].TCode = 7 - (num * 7);
                                }
                            }
                        }
                        else
                        {
                            GlobalVar.whether_change_to_test3[whichfish] = 0;
                        }
                    }
                    break;

                case -1:
                    if (((x > -1420.0) && (((x <= -1420.0) || (x >= -978.0)) || (Math.Abs(z) >= 507.0))) && (((x <= -1420.0) || (x >= -1200.0)) || (Math.Abs(z) >= 750.0)))
                    {
                        if (Math.Abs(z) < 840.0)
                        {
                            if (x < -978.0)
                            {
                                if ((1500.0 - Math.Abs(x)) < (1000.0 - Math.Abs(z)))
                                {
                                    this.Dribble(whichfish, whichball, -1500.0, z - (num * 50));
                                }
                                else
                                {
                                    this.Dribble(whichfish, whichball, x - 100.0, (double)(num * 0x3e8));
                                }
                            }
                            else
                            {
                                this.Dribble(whichfish, whichball, x - 100.0, (double)(num * 0x3e8));
                            }
                        }
                        else
                        {
                            if (((num6 < x) && ((((velocityDirectionRad * num) < (pI / 2.0)) || (Math.Abs(num7) < Math.Abs(z))) || ((num6 - x) < -10.0))) || (Math.Abs(num7) < 950.0))
                            {
                                double num28 = this.Compute_Distance(num6, num7, x, z);
                                if ((num6 < x) && (num28 < 100.0))
                                {
                                    GlobalVar.decisions[whichfish].VCode = 2;
                                    GlobalVar.decisions[whichfish].TCode = 7 - (num * 4);
                                }
                                else if (Math.Abs(num7) < 840.0)
                                {
                                    this.Position(whichfish, x + 80.0, (double)(num * 910), 150.0);
                                }
                                else
                                {
                                    this.Position(whichfish, x + 80.0, (double)(num * 0x3e8), 150.0);
                                }
                            }
                            else
                            {
                                int num29 = this.Angle_V((num * 2.0) - velocityDirectionRad);
                                int num30 = Math.Abs((int)(7 - num29));
                                if (Math.Abs(z) > 840.0)
                                {
                                    if ((Math.Abs(velocityDirectionRad) > 1.85) && (Math.Abs(velocityDirectionRad) < 2.15))
                                    {
                                        GlobalVar.decisions[whichfish].TCode = num29;
                                        GlobalVar.decisions[whichfish].VCode = 11 - num30;
                                    }
                                    else if (Math.Abs(velocityDirectionRad) > 2.0)
                                    {
                                        GlobalVar.decisions[whichfish].TCode = num29;
                                        num30 = 5 - num30;
                                        if (num30 < 1)
                                        {
                                            num30 = 1;
                                        }
                                        GlobalVar.decisions[whichfish].VCode = num30;
                                    }
                                    else
                                    {
                                        GlobalVar.decisions[whichfish].TCode = num29;
                                        if (num30 >= 2)
                                        {
                                            num30 = 2;
                                        }
                                        GlobalVar.decisions[whichfish].VCode = 10 - num30;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Math.Abs(z) > 200.0)
                        {
                            if (Math.Abs(z) > 820.0)
                            {
                                if ((num * velocityDirectionRad) >= 0.0)
                                {
                                    int num31 = 0x5dc - ((int)Math.Abs(num6));
                                    if (z > -800.0)
                                    {
                                        num31 += 10;
                                    }
                                    if (num31 > 20)
                                    {
                                        this.Position(whichfish, -1500.0, (double)(num * 0x3e8), 300.0);
                                    }
                                    else
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 2;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 4);
                                    }
                                }
                                else
                                {
                                    double num32 = 1000.0 - Math.Abs(num7);
                                    double num33 = GlobalVar.Fish_length;
                                    double num34 = Math.Acos(num32 / num33);
                                    if (num34 < 1.15)
                                    {
                                        num34 = 1.15;
                                    }
                                    if (Math.Abs(velocityDirectionRad) >= ((pI / 2.0) + num34))
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 1;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 5);
                                    }
                                    else
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 3;
                                        GlobalVar.decisions[whichfish].TCode = 7;
                                    }
                                }
                                if ((num6 - x) >= 60.0)
                                {
                                    if ((num * velocityDirectionRad) < -1.047)
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 1;
                                        GlobalVar.decisions[whichfish].TCode = 7 - (num * 7);
                                    }
                                    else if (Math.Abs(velocityDirectionRad) < 1.047)
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 1;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 7);
                                    }
                                    else if (num6 > -1000.0)
                                    {
                                        this.Position(whichfish, -1200.0, (double)(num * 0x3e8), 100.0);
                                    }
                                    else if (num6 < -1200.0)
                                    {
                                        this.Position(whichfish, -1500.0, (double)(num * 0x3e8), 100.0);
                                    }
                                }
                                else if (((Math.Abs(z) - Math.Abs(num7)) > 30.0) && ((num6 - x) < 60.0))
                                {
                                    GlobalVar.decisions[whichfish].VCode = 1;
                                    GlobalVar.decisions[whichfish].TCode = 7 - (num * 7);
                                }
                            }
                            else
                            {
                                double num37;
                                double num35 = Math.Abs((double)(num7 - 1000.0));
                                double num36 = GlobalVar.Fish_length;
                                double num38 = 2.0 - ((3.0 * pI) / 2.0);
                                if (num35 < num36)
                                {
                                    num37 = Math.Acos(num35 / num36);
                                }
                                else
                                {
                                    num37 = pI - 2.0;
                                }
                                if ((num * num38) > (num * ((-pI / 2.0) - num37)))
                                {
                                    num38 = (-pI / 2.0) - num37;
                                }
                                int num39 = this.Angle_V((num * num38) - velocityDirectionRad);
                                int num40 = Math.Abs((int)(7 - num39));
                                if (((num * velocityDirectionRad) > 0.0) || ((num * velocityDirectionRad) < -(pI - 0.3)))
                                {
                                    if ((num * velocityDirectionRad) > 0.0)
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 4;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 4);
                                        double num41 = num4 + ((velocityMmPs * 0.1) * Math.Cos(velocityDirectionRad));
                                        double num42 = num5 + ((velocityMmPs * 0.1) * Math.Sin(velocityDirectionRad));
                                        double num43 = this.Compute_Distance(num4, num5, -1500.0, -1000.0);
                                        double num44 = this.Compute_Distance(num41, num42, -1500.0, -1000.0);
                                        if ((num43 < 50.0) && (num44 < 50.0))
                                        {
                                            GlobalVar.decisions[whichfish].VCode = 1;
                                            if (num == 1)
                                            {
                                                GlobalVar.decisions[whichfish].TCode = 14;
                                            }
                                            else
                                            {
                                                GlobalVar.decisions[whichfish].VCode = 0;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 4;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 4);
                                    }
                                }
                                else if (Math.Abs(num7) > 750.0)
                                {
                                    num38 = (-pI / 2.0) - num37;
                                    num38 -= 0.1;
                                    if (Math.Abs(velocityDirectionRad) > Math.Abs(num38))
                                    {
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 2);
                                        GlobalVar.decisions[whichfish].VCode = 5;
                                    }
                                    else
                                    {
                                        GlobalVar.decisions[whichfish].TCode = 7;
                                        GlobalVar.decisions[whichfish].VCode = 7;
                                    }
                                }
                                else
                                {
                                    if ((Math.Abs(velocityDirectionRad) < (Math.Abs(num38) + 0.05)) && (Math.Abs(velocityDirectionRad) > (Math.Abs(num38) - 0.05)))
                                    {
                                        GlobalVar.decisions[whichfish].TCode = 7;
                                        GlobalVar.decisions[whichfish].VCode = 12 - num40;
                                    }
                                    else if (Math.Abs(velocityDirectionRad) < Math.Abs(num38))
                                    {
                                        if (Math.Abs((int)(GlobalVar.prefish[whichfish].preTcode - num39)) > 1)
                                        {
                                            num39 = Math.Sign((int)(GlobalVar.prefish[whichfish].preTcode - num39)) + num39;
                                        }
                                        GlobalVar.decisions[whichfish].TCode = num39;
                                        num40 = 9 - num40;
                                        if (num40 < 1)
                                        {
                                            num40 = 1;
                                        }
                                        if (Math.Abs((int)(GlobalVar.prefish[whichfish].preVcode - num40)) > 2)
                                        {
                                            num40 = (num40 + GlobalVar.prefish[whichfish].preVcode) / 2;
                                        }
                                        GlobalVar.decisions[whichfish].VCode = num40;
                                    }
                                    else
                                    {
                                        GlobalVar.decisions[whichfish].TCode = num39;
                                        num40 = 3 - num40;
                                        if (num40 <= 6)
                                        {
                                            num40 = 6;
                                        }
                                        GlobalVar.decisions[whichfish].VCode = num40;
                                    }
                                }
                                if ((num6 - x) >= 60.0)
                                {
                                    if ((num * velocityDirectionRad) < -1.047)
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 1;
                                        GlobalVar.decisions[whichfish].TCode = 7 - (num * 7);
                                    }
                                    else if (Math.Abs(velocityDirectionRad) < 1.047)
                                    {
                                        GlobalVar.decisions[whichfish].VCode = 1;
                                        GlobalVar.decisions[whichfish].TCode = 7 + (num * 7);
                                    }
                                    if (num6 > -1000.0)
                                    {
                                        this.Position(whichfish, -1200.0, z + (num * 50), 200.0);
                                    }
                                    else if (num6 < -1200.0)
                                    {
                                        this.Position(whichfish, -1500.0, z + (num * 0x41), 100.0);
                                    }
                                }
                                else if (((Math.Abs(z) - Math.Abs(num7)) > 30.0) && ((num6 - x) < 60.0))
                                {
                                    GlobalVar.decisions[whichfish].VCode = 1;
                                    GlobalVar.decisions[whichfish].TCode = 7 - (num * 7);
                                }
                            }
                        }
                        else
                        {
                            GlobalVar.whether_change_to_test3[whichfish] = 0;
                        }
                    }
                    break;
            }
        }

        public void shutdownlanqiu()
        {
            double x = GlobalVar.MyTeam.Fishes[0].PositionMm.X;
            double z = GlobalVar.MyTeam.Fishes[0].PositionMm.Z;
            double num3 = GlobalVar.MyTeam.Fishes[1].PositionMm.X;
            double num4 = GlobalVar.MyTeam.Fishes[1].PositionMm.Z;
            double num5 = GlobalVar.OppTeam.Fishes[0].PositionMm.X;
            double num6 = GlobalVar.OppTeam.Fishes[0].PositionMm.Z;
            double num7 = GlobalVar.OppTeam.Fishes[1].PositionMm.X;
            double num8 = GlobalVar.OppTeam.Fishes[1].PositionMm.Z;
            if (this.IsInMyUnArea_02(num5, num6))
            {
                if ((z > num6) && (num6 > num4))
                {
                    GlobalVar.lanqiuFlag[1] = 1;
                    GlobalVar.lanqiuFlag[0] = 1;
                    GlobalVar.finallanqiu = 0;
                }
                else if ((z < num6) && (num6 < num4))
                {
                    GlobalVar.lanqiuFlag[1] = 1;
                    GlobalVar.lanqiuFlag[0] = 1;
                    GlobalVar.finallanqiu = 0;
                }
            }
            if (this.IsInMyUnArea_02(num7, num8))
            {
                if ((z > num8) && (num8 > num4))
                {
                    GlobalVar.lanqiuFlag[1] = 1;
                    GlobalVar.lanqiuFlag[0] = 1;
                    GlobalVar.finallanqiu = 0;
                }
                else if ((z < num8) && (num8 < num4))
                {
                    GlobalVar.lanqiuFlag[1] = 1;
                    GlobalVar.lanqiuFlag[0] = 1;
                    GlobalVar.finallanqiu = 0;
                }
            }
        }

        public void Dribblehometestlast2min(int whichfish)
        {
            double num6;
            int num9;
            GlobalVar.corner[whichfish] = 0;
            GlobalVar.corner_help[whichfish] = 0;
            double velocityDirectionRad = GlobalVar.MyTeam.Fishes[whichfish].VelocityDirectionRad;
            double pI = GlobalVar.PI;
            double x = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.X;
            double z = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.Z;
            double num3 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].X;
            double num4 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].Z;
            if (whichfish == 0)
            {
                num9 = 1;
            }
            else
            {
                num9 = -1;
            }
            if ((GlobalVar.teamId == 1) && (GlobalVar.Paishu > 0xbb8))
            {
                num9 = -num9;
            }
            double dx = -1500.0;
            if (this.CalculateBallZ() >= 1000.0)
            {
                num6 = (this.CalculateBallZ() + 300.0) * num9;
            }
            else
            {
                num6 = this.CalculateBallZ() * num9;
            }
            if ((x > -1152.0) && ((x >= -978.0) || (Math.Abs(z) >= 600.0)))
            {
                GlobalVar.last2min_is_in_200m[whichfish] = 0;
                if (this.IsInOppUnArea_01(x, z))
                {
                    if (Math.Abs(num4) < 520.0)
                    {
                        this.Position(whichfish, 1500.0, (double)(num9 * 0x3e8), 200.0);
                    }
                    else
                    {
                        this.Position(whichfish, 800.0, (double)(num9 * 700), 200.0);
                    }
                }
                else
                {
                    this.Position(whichfish, dx, num6, 200.0);
                }
            }
            else if ((((Math.Abs(z) <= 120.0) && (x < -1300.0)) && (num3 < -1450.0)) || (GlobalVar.last2min_is_in_200m[whichfish] == 1))
            {
                GlobalVar.last2min_is_in_200m[whichfish] = 1;
                double num10 = GlobalVar.MyTeam.Fishes[1 - whichfish].PositionMm.X;
                double num11 = GlobalVar.MyTeam.Fishes[1 - whichfish].PositionMm.Z;
                double num12 = GlobalVar.MyTeam.Fishes[1].PositionMm.Z;
                double num13 = GlobalVar.MyTeam.Fishes[0].PositionMm.Z;
                if ((Math.Abs(num11) >= 120.0) && (GlobalVar.last2min_is_in_200m[1 - whichfish] == 0))
                {
                    this.StayForDecision(whichfish);
                    if (((GlobalVar.teamId == 0) && (GlobalVar.Paishu < 0xbb8)) && (num12 > (num13 + 200.0)))
                    {
                        GlobalVar.lanqiuFlag[1] = 1;
                        GlobalVar.lanqiuFlag[0] = 1;
                    }
                    if (((GlobalVar.teamId == 1) && (GlobalVar.Paishu < 0xbb8)) && (num12 > (num13 + 200.0)))
                    {
                        GlobalVar.lanqiuFlag[1] = 1;
                        GlobalVar.lanqiuFlag[0] = 1;
                    }
                    if (((GlobalVar.teamId == 0) && (GlobalVar.Paishu > 0xbb8)) && (num12 > (num13 + 200.0)))
                    {
                        GlobalVar.lanqiuFlag[1] = 1;
                        GlobalVar.lanqiuFlag[0] = 1;
                    }
                    if (((GlobalVar.teamId == 1) && (GlobalVar.Paishu > 0xbb8)) && (num12 < (num13 - 200.0)))
                    {
                        GlobalVar.lanqiuFlag[1] = 1;
                        GlobalVar.lanqiuFlag[0] = 1;
                    }
                }
                else
                {
                    this.Circle_last2min(whichfish);
                }
                GlobalVar.BaAction.shutdownlanqiu();
            }
            else
            {
                double num16;
                double num14 = Math.Abs((double)(num4 - 1000.0));
                double num15 = GlobalVar.Fish_length;
                double num17 = 2.0 - ((3.0 * pI) / 2.0);
                if (num14 < num15)
                {
                    num16 = Math.Acos(num14 / num15);
                }
                else
                {
                    num16 = pI - 2.0;
                }
                if ((num9 * num17) > (num9 * ((-pI / 2.0) - num16)))
                {
                    num17 = (-pI / 2.0) - num16;
                }
                num17 += 0.3;
                int num18 = this.Angle_V((num9 * num17) - velocityDirectionRad);
                int num19 = Math.Abs((int)(7 - num18));
                if (((num9 * velocityDirectionRad) > 0.0) || ((num9 * velocityDirectionRad) < -(pI - 0.3)))
                {
                    if ((num9 * velocityDirectionRad) > 0.0)
                    {
                        GlobalVar.decisions[whichfish].VCode = 4;
                        GlobalVar.decisions[whichfish].TCode = 7 + (num9 * 4);
                    }
                    else
                    {
                        GlobalVar.decisions[whichfish].VCode = 4;
                        GlobalVar.decisions[whichfish].TCode = 7 + (num9 * 4);
                    }
                }
                else if (Math.Abs(num4) > 750.0)
                {
                    num17 = (-pI / 2.0) - num16;
                    num17 -= 0.1;
                    if (Math.Abs(velocityDirectionRad) > Math.Abs(num17))
                    {
                        GlobalVar.decisions[whichfish].TCode = 7 + (num9 * 2);
                        GlobalVar.decisions[whichfish].VCode = 2;
                    }
                    else
                    {
                        GlobalVar.decisions[whichfish].TCode = 7;
                        GlobalVar.decisions[whichfish].VCode = 5;
                    }
                }
                else if ((Math.Abs(velocityDirectionRad) < (Math.Abs(num17) + 0.15)) && (Math.Abs(velocityDirectionRad) > (Math.Abs(num17) - 0.15)))
                {
                    GlobalVar.decisions[whichfish].TCode = 7;
                    GlobalVar.decisions[whichfish].VCode = 12 - num19;
                }
                else if (Math.Abs(velocityDirectionRad) < Math.Abs(num17))
                {
                    if (Math.Abs((int)(GlobalVar.prefish[whichfish].preTcode - num18)) > 1)
                    {
                        num18 = Math.Sign((int)(GlobalVar.prefish[whichfish].preTcode - num18)) + num18;
                    }
                    GlobalVar.decisions[whichfish].TCode = num18;
                    num19 = 3 - num19;
                    if (num19 < 1)
                    {
                        num19 = 1;
                    }
                    if (Math.Abs((int)(GlobalVar.prefish[whichfish].preVcode - num19)) > 2)
                    {
                        num19 = (num19 + GlobalVar.prefish[whichfish].preVcode) / 2;
                    }
                    GlobalVar.decisions[whichfish].VCode = num19;
                }
                else
                {
                    GlobalVar.decisions[whichfish].TCode = num18;
                    num19 = 9 - num19;
                    if (num19 <= 6)
                    {
                        num19 = 6;
                    }
                    GlobalVar.decisions[whichfish].VCode = num19;
                }
            }
        }

        public int dribblejudge(int whichfish)
        {
            double x = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.X;
            double z = GlobalVar.MyTeam.Fishes[whichfish].PositionMm.Z;
            double num4 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].X;
            double num5 = GlobalVar.MyTeam.Fishes[whichfish].PolygonVertices[0].Z;
            double bodyDirectionRad = GlobalVar.MyTeam.Fishes[whichfish].BodyDirectionRad;
            double num7 = 0.0;
            double num8 = 0.0;
            if ((GlobalVar.Bringing_BallID[whichfish] == -1) && (GlobalVar.finallanqiu == 0))
            {
                for (int i = 0; i < 9; i++)
                {
                    double num9 = GlobalVar.balls[i].PositionMm.X;
                    double num10 = GlobalVar.balls[i].PositionMm.Z;
                    num7 = Math.Sqrt(((x - num9) * (x - num9)) + ((z - num10) * (z - num10)));
                    num8 = Math.Sqrt(((num4 - num9) * (num4 - num9)) + ((num5 - num10) * (num5 - num10)));
                    if ((((Math.Abs(num5) > 850.0) || (Math.Abs(num4) > 1350.0)) && ((num7 < 330.0) || (num8 < 300.0))) && ((((num9 < (x + 30.0)) && (Math.Abs(num5) > 850.0)) || (((Math.Abs(num4) > 1350.0) && (bodyDirectionRad > 0.0)) && (num10 < num5))) || (((Math.Abs(num4) > 1350.0) && (bodyDirectionRad < 0.0)) && (num10 > num5))))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
        //******************************************************************

        public void QuickInterfereBall(int fishID)
        {
            GlobalVar.corner[fishID] = 0;
            GlobalVar.corner_help[fishID] = 0;

            double fishPosX = GlobalVar.MyTeam.Fishes[fishID].PositionMm.X;
            double fishPosZ = GlobalVar.MyTeam.Fishes[fishID].PositionMm.Z;
            double fishVelocityDirection = GlobalVar.MyTeam.Fishes[fishID].VelocityDirectionRad;

            if (GlobalVar.Stage == 0)
            {
                this.QuickInterfereBall_Pre(fishID);
            }
            else if (GlobalVar.Stage == 1)
            {
                this.QuickInterfereBallFromPositive(fishID);
            }
            else if (GlobalVar.Stage == 2)
            {
                this.QuickInterfereBallFromNegative(fishID);
            }

            if (fishPosX < 980.0 || fishPosZ < -550.0 || fishPosZ > 550.0)
            {
                GlobalVar.Stage = 0;
            }
        }

        public void QuickInterfereBallFromNegative(int fishID)
        {
            double fishPosX = GlobalVar.MyTeam.Fishes[fishID].PositionMm.X;
            double fishPosZ = GlobalVar.MyTeam.Fishes[fishID].PositionMm.Z;
            double fishVelocityDirection = GlobalVar.MyTeam.Fishes[fishID].VelocityDirectionRad;

            double projectedPosZ = fishPosZ + (105.0 * Math.Sin(fishVelocityDirection));

            if (fishPosX > 1152.0)
            {
                this.Position(fishID, 1100.0, 400.0, 100.0);
            }
            else
            {
                if (fishVelocityDirection < 0.0)
                {
                    Set_Decisions(fishID, 1, 0);
                }
                else if (fishVelocityDirection > 2.7 && fishVelocityDirection < 3.14)
                {
                    Set_Decisions(fishID, 14, 0);
                }
                else if (fishVelocityDirection > 2.5 && fishVelocityDirection < 2.7)
                {
                    Set_Decisions(fishID, 14, 1);
                }
                else if (fishVelocityDirection > 2.2 && fishVelocityDirection < 2.5)
                {
                    Set_Decisions(fishID, 12, 5);
                }
                else if (fishVelocityDirection > 2.0 && fishVelocityDirection < 2.2)
                {
                    Set_Decisions(fishID, 11, 5);
                }
                else
                {
                    Set_Decisions(fishID, 9, 8);
                }

                if (projectedPosZ > 460.0)
                {
                    GlobalVar.Stage = 1;
                }
            }
        }

        public void QuickInterfereBallFromPositive(int fishID)
        {
            double fishPosX = GlobalVar.MyTeam.Fishes[fishID].PositionMm.X;
            double fishPosZ = GlobalVar.MyTeam.Fishes[fishID].PositionMm.Z;
            double fishVelocityDirection = GlobalVar.MyTeam.Fishes[fishID].VelocityDirectionRad;

            double projectedPosZ = fishPosZ + (105.0 * Math.Sin(fishVelocityDirection));

            if (fishPosX > 1152.0)
            {
                this.Position(fishID, 1100.0, -400.0, 100.0);
            }
            else
            {
                if (fishVelocityDirection > 0.0)
                {
                    Set_Decisions(fishID, 2, 14);
                }
                else if (fishVelocityDirection < -2.7 && fishVelocityDirection > -3.14)
                {
                    Set_Decisions(fishID, 12, 10);
                }
                else if (fishVelocityDirection < -2.5 && fishVelocityDirection > -2.7)
                {
                    Set_Decisions(fishID, 12, 9);
                }
                else if (fishVelocityDirection < -2.3 && fishVelocityDirection > -2.5)
                {
                    Set_Decisions(fishID, 10, 6);
                }
                else
                {
                    Set_Decisions(fishID, 9, 6);
                }

                if (projectedPosZ < -497.0)
                {
                    GlobalVar.Stage = 2;
                }
            }
        }

        public void QuickInterfereBall_Pre(int fishID)
        {
            int directionMultiplier = (fishID == 0) ? 1 : -1;
            int state = 0;

            double fishPosX = GlobalVar.MyTeam.Fishes[fishID].PositionMm.X;
            double fishPosZ = GlobalVar.MyTeam.Fishes[fishID].PositionMm.Z;
            double velocityDirection = GlobalVar.MyTeam.Fishes[fishID].VelocityDirectionRad;

            // Determine state based on fish position
            if (fishPosZ < 600.0 && fishPosZ > -600.0 && fishPosX < -980.0)
            {
                state = -1;
            }
            else if (fishPosZ >= 500.0 || fishPosZ <= -500.0 || fishPosX <= 980.0)
            {
                state = 0;
            }
            else if (fishPosZ < 500.0 && fishPosZ > -500.0 && fishPosX > 1150.0)
            {
                state = 1;
            }
            else
            {
                state = 2;
            }

            // Execute actions based on determined state
            switch (state)
            {
                case -1:
                    this.Position(fishID, -1300.0, directionMultiplier * 750.0, 150.0);
                    break;

                case 0:
                    if (fishPosX > 940.0 && fishPosZ < -500.0)
                    {
                        this.Position(fishID, 0.0, 0.0, 150.0);
                    }
                    else if (fishPosX < 1150.0)
                    {
                        this.Position(fishID, 1200.0, 620.0, 150.0);
                    }
                    else if (fishPosX > 1150.0 && fishPosZ > 500.0)
                    {
                        this.Position(fishID, 1250.0, 460.0, 150.0);
                    }
                    break;

                case 1:
                    if (velocityDirection >= -3.0 || velocityDirection <= -3.14)
                    {
                        GlobalVar.decisions[fishID].VCode = 2;
                        GlobalVar.decisions[fishID].TCode = 2;
                    }
                    break;

                case 2:
                    GlobalVar.Stage = 1;
                    break;
            }
        }

        public void Angle_Stay(int fishIndex, double targetAngleRad, int velocityCode)
        {
            double initialAngularVelocity;
            double adjustedVelocityDirection = 0;
            double adjustedAngularVelocity = 0;
            double finalVelocityDirection;
            double finalAngularVelocity;
            int tCodeIndex = 0;

            double currentVelocityDirection = GlobalVar.MyTeam.Fishes[fishIndex].VelocityDirectionRad;
            double currentAngularVelocity = GlobalVar.MyTeam.Fishes[fishIndex].AngularVelocityRadPs;
            double angleDifference = this.FormatAngleRad(targetAngleRad - currentVelocityDirection);

            double tempAngularVelocity = currentAngularVelocity;
            double tempVelocityDirection = currentVelocityDirection;

            if (angleDifference >= 0.0)
            {
                tCodeIndex = 7;
                while (tCodeIndex < 15)
                {
                    adjustedAngularVelocity = (tempAngularVelocity * 0.9) + GlobalVar.tTable[tCodeIndex];
                    adjustedVelocityDirection = tempVelocityDirection + (adjustedAngularVelocity * 0.1);
                    if (this.FormatAngleRad(targetAngleRad - adjustedVelocityDirection) < 0.0)
                    {
                        break;
                    }
                    tCodeIndex++;
                }

                finalAngularVelocity = (tempAngularVelocity * 0.9) + GlobalVar.tTable[tCodeIndex];
                finalVelocityDirection = tempVelocityDirection + (adjustedAngularVelocity * 0.1);
                initialAngularVelocity = this.FormatAngleRad(targetAngleRad - adjustedVelocityDirection);
                finalAngularVelocity = (tempAngularVelocity * 0.9) + GlobalVar.tTable[tCodeIndex];
                finalVelocityDirection = tempVelocityDirection + (adjustedAngularVelocity * 0.1);

                if (Math.Abs(this.FormatAngleRad(targetAngleRad - adjustedVelocityDirection)) < Math.Abs(initialAngularVelocity))
                {
                    tCodeIndex--;
                }
            }

            if (angleDifference < 0.0)
            {
                tCodeIndex = 7;
                while (tCodeIndex > 0)
                {
                    adjustedAngularVelocity = (tempAngularVelocity * 0.9) + GlobalVar.tTable[tCodeIndex];
                    adjustedVelocityDirection = tempVelocityDirection + (adjustedAngularVelocity * 0.1);
                    if (this.FormatAngleRad(targetAngleRad - adjustedVelocityDirection) > 0.0)
                    {
                        break;
                    }
                    tCodeIndex--;
                }

                finalAngularVelocity = (tempAngularVelocity * 0.9) + GlobalVar.tTable[tCodeIndex];
                finalVelocityDirection = tempVelocityDirection + (adjustedAngularVelocity * 0.1);
                initialAngularVelocity = this.FormatAngleRad(targetAngleRad - adjustedVelocityDirection);
                finalAngularVelocity = (tempAngularVelocity * 0.9) + GlobalVar.tTable[tCodeIndex];
                finalVelocityDirection = tempVelocityDirection + (adjustedAngularVelocity * 0.1);

                if (Math.Abs(this.FormatAngleRad(targetAngleRad - adjustedVelocityDirection)) < Math.Abs(initialAngularVelocity))
                {
                    tCodeIndex++;
                }
            }

            GlobalVar.decisions[fishIndex].TCode = tCodeIndex;
            if (velocityCode > 0)
            {
                GlobalVar.decisions[fishIndex].VCode = velocityCode;
            }
        }

        public void KeepV(int fishIndex, double desiredVelocity)
        {
            double targetVelocity = desiredVelocity;
            double fishPosX = GlobalVar.MyTeam.Fishes[fishIndex].PositionMm.X;
            double fishPosZ = GlobalVar.MyTeam.Fishes[fishIndex].PositionMm.Z;
            double currentVelocity = GlobalVar.MyTeam.Fishes[fishIndex].VelocityMmPs;
            double currentAngularVelocity = GlobalVar.MyTeam.Fishes[fishIndex].AngularVelocityRadPs;
            double bodyDirection = GlobalVar.MyTeam.Fishes[fishIndex].BodyDirectionRad;

            double[] velocityTable = GlobalVar.vTable;
            double[] angularTable = GlobalVar.tTable;

            double projectedPosX = fishPosX + (currentVelocity * Math.Cos(bodyDirection) * 0.1);
            double projectedPosZ = fishPosZ + (currentVelocity * Math.Sin(bodyDirection) * 0.1);
            double adjustedVelocity = 0.0;
            double projectedDirection = bodyDirection + (currentAngularVelocity * 0.1);

            // Clamping target velocity within the allowed range
            if (targetVelocity < 0.0)
            {
                targetVelocity = 0.0;
            }
            if (targetVelocity > 317.0)
            {
                targetVelocity = 317.0;
            }

            int bestVCode = 15;
            double minDifference = 318.0;

            for (int i = 15; i > 0; i--)
            {
                adjustedVelocity = (currentVelocity * 0.9) + (velocityTable[i] * 0.1);
                if (minDifference > Math.Abs(adjustedVelocity - targetVelocity))
                {
                    minDifference = Math.Abs(adjustedVelocity - targetVelocity);
                    bestVCode = i;
                }
            }

            GlobalVar.decisions[fishIndex].VCode = bestVCode;
        }

        public bool IsInUnBall(double Posx, double Posz)
        {
            return ((Math.Abs(Posz) >= 600.0) && (Posx <= -1120.0));
        }

        //******************************************************************

        public int CountBallsInMyBigArea(bool includeForbiddenArea, bool calculateScore)
        {
            double[,] ballPositions = new double[9, 2];
            for (int i = 0; i < 9; i++)
            {
                ballPositions[i, 0] = GlobalVar.mission.EnvRef.Balls[i].PositionMm.X;
                ballPositions[i, 1] = GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z;
            }

            int ballCount = 0;
            int scoreCount = 0;

            for (int j = 0; j < 9; j++)
            {
                if (this.IsInMyUnArea_01(ballPositions[j, 0]))
                {
                    if (GlobalVar.leftScore[j] == 0)
                    {
                        ballCount++;
                    }
                    if (this.IsInHome(ballPositions[j, 0], ballPositions[j, 1]))
                    {
                        scoreCount++;
                    }
                }
            }

            if (calculateScore)
            {
                for (int k = 0; k < 9; k++)
                {
                    if (this.IsInMyUnArea_01(ballPositions[k, 0]))
                    {
                        if (GlobalVar.leftScore[k] == 0)
                        {
                            if (k == 0 || k == 1 || k == 2)
                            {
                                ballCount += 2;
                            }
                            else if (k == 7 || k == 8)
                            {
                                ballCount++;
                            }
                        }

                        if (this.IsInHome(ballPositions[k, 0], ballPositions[k, 1]) || GlobalVar.leftScore[k] == 1)
                        {
                            if (this.IsInHome(ballPositions[k, 0], ballPositions[k, 1]))
                            {
                                if (k == 0 || k == 1 || k == 2)
                                {
                                    scoreCount += 2;
                                }
                                else if (k == 7 || k == 8)
                                {
                                    scoreCount++;
                                }
                            }
                            else if (GlobalVar.leftScore[k] == 1 && !this.IsInHome(ballPositions[k, 0], ballPositions[k, 1]))
                            {
                                if (k == 0 || k == 1 || k == 2)
                                {
                                    scoreCount += 3;
                                }
                                else if (k == 7 || k == 8)
                                {
                                    scoreCount += 2;
                                }
                                else
                                {
                                    scoreCount++;
                                }
                            }
                        }
                    }
                }
            }

            if (includeForbiddenArea)
            {
                return ballCount;
            }

            return scoreCount;
        }

        public int CountBallsInMySmallArea(bool includeForbiddenArea, bool calculateScore)
        {
            double[,] ballPositions = new double[9, 2];
            for (int i = 0; i < 9; i++)
            {
                ballPositions[i, 0] = GlobalVar.mission.EnvRef.Balls[i].PositionMm.X;
                ballPositions[i, 1] = GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z;
            }

            int ballCount = 0;
            int scoreCount = 0;

            for (int j = 0; j < 9; j++)
            {
                if (this.IsInMyUnArea_02(ballPositions[j, 0], ballPositions[j, 1]))
                {
                    if (GlobalVar.leftScore[j] == 0)
                    {
                        ballCount++;
                    }
                    if (this.IsInHome(ballPositions[j, 0], ballPositions[j, 1]))
                    {
                        scoreCount++;
                    }
                }
            }

            if (calculateScore)
            {
                for (int k = 0; k < 9; k++)
                {
                    if (this.IsInMyUnArea_02(ballPositions[k, 0], ballPositions[k, 1]))
                    {
                        if (GlobalVar.leftScore[k] == 0)
                        {
                            if (k == 0 || k == 1 || k == 2)
                            {
                                ballCount += 2;
                            }
                            else if (k == 7 || k == 8)
                            {
                                ballCount++;
                            }
                        }

                        if (this.IsInHome(ballPositions[k, 0], ballPositions[k, 1]) || GlobalVar.leftScore[k] == 1)
                        {
                            if (this.IsInHome(ballPositions[k, 0], ballPositions[k, 1]))
                            {
                                if (k == 0 || k == 1 || k == 2)
                                {
                                    scoreCount += 2;
                                }
                                else if (k == 7 || k == 8)
                                {
                                    scoreCount++;
                                }
                            }
                            else if (GlobalVar.leftScore[k] == 1 && !this.IsInHome(ballPositions[k, 0], ballPositions[k, 1]))
                            {
                                if (k == 0 || k == 1 || k == 2)
                                {
                                    scoreCount += 3;
                                }
                                else if (k == 7 || k == 8)
                                {
                                    scoreCount += 2;
                                }
                                else
                                {
                                    scoreCount++;
                                }
                            }
                        }
                    }
                }
            }

            if (includeForbiddenArea)
            {
                return ballCount;
            }

            return scoreCount;
        }

        public int CountBallsInOppBigArea(bool includeForbiddenArea, bool calculateScore)
        {
            double[,] ballPositions = new double[9, 2];
            for (int i = 0; i < 9; i++)
            {
                ballPositions[i, 0] = GlobalVar.mission.EnvRef.Balls[i].PositionMm.X;
                ballPositions[i, 1] = GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z;
            }

            int ballCount = 0;
            int scoreCount = 0;

            for (int j = 0; j < 9; j++)
            {
                if (this.IsInOppUnArea_01(ballPositions[j, 0], ballPositions[j, 1]) && !this.IsInOppHome_Area(ballPositions[j, 0], ballPositions[j, 1]))
                {
                    ballCount++;
                }
                if (this.IsInOppHome_Area(ballPositions[j, 0], ballPositions[j, 1]))
                {
                    scoreCount++;
                }
            }

            if (calculateScore)
            {
                for (int k = 0; k < 9; k++)
                {
                    if (this.IsInOppUnArea_01(ballPositions[k, 0], ballPositions[k, 1]))
                    {
                        if (GlobalVar.rightScore[k] == 0)
                        {
                            if (k == 0 || k == 1 || k == 2)
                            {
                                ballCount += 2;
                            }
                            else if (k == 7 || k == 8)
                            {
                                ballCount++;
                            }
                        }

                        if (this.IsInOppHome_Area(ballPositions[k, 0], ballPositions[k, 1]) || GlobalVar.rightScore[k] == 1)
                        {
                            if (this.IsInOppHome_Area(ballPositions[k, 0], ballPositions[k, 1]))
                            {
                                if (k == 0 || k == 1 || k == 2)
                                {
                                    scoreCount += 2;
                                }
                                else if (k == 7 || k == 8)
                                {
                                    scoreCount++;
                                }
                            }
                            else if (GlobalVar.rightScore[k] == 1 && !this.IsInOppHome_Area(ballPositions[k, 0], ballPositions[k, 1]))
                            {
                                if (k == 0 || k == 1 || k == 2)
                                {
                                    scoreCount += 3;
                                }
                                else if (k == 7 || k == 8)
                                {
                                    scoreCount += 2;
                                }
                                else
                                {
                                    scoreCount++;
                                }
                            }
                        }
                    }
                }
            }

            return includeForbiddenArea ? ballCount : scoreCount;
        }

        public bool IsInOppForbiddenAreaReduceMiddle(double posX, double posZ)
        {
            return (posX > 952.0) && (Math.Abs(posZ) < 700.0);
        }

        public int CountBallsInOppMiddleArea(bool includeForbiddenArea, bool calculateScore)
        {
            double[,] ballPositions = new double[9, 2];
            for (int i = 0; i < 9; i++)
            {
                ballPositions[i, 0] = GlobalVar.mission.EnvRef.Balls[i].PositionMm.X;
                ballPositions[i, 1] = GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z;
            }

            int ballCount = 0;
            int scoreCount = 0;

            for (int j = 0; j < 9; j++)
            {
                if (this.IsInOppForbiddenAreaReduceMiddle(ballPositions[j, 0], ballPositions[j, 1]) &&
                    !this.IsInOppHome_Area(ballPositions[j, 0], ballPositions[j, 1]))
                {
                    ballCount++;
                }
                if (this.IsInOppHome_Area(ballPositions[j, 0], ballPositions[j, 1]))
                {
                    scoreCount++;
                }
            }

            if (calculateScore)
            {
                for (int k = 0; k < 9; k++)
                {
                    if (this.IsInOppForbiddenAreaReduceMiddle(ballPositions[k, 0], ballPositions[k, 1]))
                    {
                        if (GlobalVar.rightScore[k] == 0)
                        {
                            if (k == 0 || k == 1 || k == 2)
                            {
                                ballCount += 2;
                            }
                            else if (k == 7 || k == 8)
                            {
                                ballCount++;
                            }
                        }

                        if (this.IsInOppHome_Area(ballPositions[k, 0], ballPositions[k, 1]) || GlobalVar.rightScore[k] == 1)
                        {
                            if (this.IsInOppHome_Area(ballPositions[k, 0], ballPositions[k, 1]))
                            {
                                if (k == 0 || k == 1 || k == 2)
                                {
                                    scoreCount += 2;
                                }
                                else if (k == 7 || k == 8)
                                {
                                    scoreCount++;
                                }
                            }
                            else if (GlobalVar.rightScore[k] == 1 && !this.IsInOppHome_Area(ballPositions[k, 0], ballPositions[k, 1]))
                            {
                                if (k == 0 || k == 1 || k == 2)
                                {
                                    scoreCount += 3;
                                }
                                else if (k == 7 || k == 8)
                                {
                                    scoreCount += 2;
                                }
                                else
                                {
                                    scoreCount++;
                                }
                            }
                        }
                    }
                }
            }

            return includeForbiddenArea ? ballCount : scoreCount;
        }
        //******************************************************************


        //*******************************(谨慎修改)
        public void Position(int which, double dx, double dz, double dv)
        {
            double velocityDirectionRad = GlobalVar.MyTeam.Fishes[which].VelocityDirectionRad;
            double num2 = GlobalVar.MyTeam.Fishes[which].PositionMm.X + (GlobalVar.Fish_centertohead * Math.Cos(velocityDirectionRad));
            double num3 = GlobalVar.MyTeam.Fishes[which].PositionMm.Z + (GlobalVar.Fish_centertohead * Math.Sin(velocityDirectionRad));
            double velocityMmPs = GlobalVar.MyTeam.Fishes[which].VelocityMmPs;
            double angularVelocityRadPs = GlobalVar.MyTeam.Fishes[which].AngularVelocityRadPs;
            double[] vTable = GlobalVar.vTable;
            double[] tTable = GlobalVar.tTable;
            double pI = GlobalVar.PI;
            double num7 = dx;
            double num8 = dz;
            double num9 = dv;
            double num10 = num2 + ((velocityMmPs * Math.Cos(velocityDirectionRad)) * 0.1);
            double num11 = num3 + ((velocityMmPs * Math.Sin(velocityDirectionRad)) * 0.1);
            double num12 = velocityDirectionRad + (angularVelocityRadPs * 0.1);
            double num13 = 0.0;
            double num14 = 0.0;
            double num15 = 0.0;
            double num16 = 0.0;
            if (dv < 0.0)
            {
                dv = 0.0;
            }
            if (dv > 314.0)
            {
                dv = 314.0;
            }
            num16 = Math.Sqrt(((num7 - num2) * (num7 - num2)) + ((num8 - num3) * (num8 - num3)));
            num13 = Math.Sqrt(((num7 - num10) * (num7 - num10)) + ((num8 - num11) * (num8 - num11)));
            int index = 15;
            while (index > 1)
            {
                num15 = (velocityMmPs * 0.9) + (vTable[index] * 0.1);
                num14 = 0.0;
                int num22 = 0;
                while ((num15 > dv) && (num22 < 0x17))
                {
                    num22++;
                    num14 += num15;
                    num15 = 0.9 * num15;
                }
                if ((num14 * 0.1) <= num13)
                {
                    break;
                }
                index--;
            }
            GlobalVar.decisions[which].VCode = index;
            double num18 = 0.0;
            double num20 = 0.0;
            double num21 = 0.0;
            num18 = Math.Atan2(num8 - num11, num7 - num10) - num12;
            if (num18 > pI)
            {
                num18 -= 2.0 * pI;
            }
            if (num18 < -pI)
            {
                num18 += 2.0 * pI;
            }
            if (num18 > 0.0)
            {
                for (index = 15; index > 0; index--)
                {
                    num20 = (angularVelocityRadPs * 0.9) + (tTable[index] * 0.1);
                    if ((angularVelocityRadPs < 0.0) && (num20 < 0.0))
                    {
                        break;
                    }
                    num21 = 0.0;
                    if (num20 <= 0.0)
                    {
                        break;
                    }
                    while (num20 > 0.0)
                    {
                        num21 += num20;
                        num20 = (0.9 * num20) + (tTable[0] * 0.1);
                    }
                    if ((num21 * 0.1) <= (1.1 * num18))
                    {
                        break;
                    }
                }
            }
            else
            {
                index = 0;
                while (index < 15)
                {
                    num20 = (angularVelocityRadPs * 0.9) + (tTable[index] * 0.1);
                    if ((angularVelocityRadPs > 0.0) && (num20 >= 0.0))
                    {
                        break;
                    }
                    num21 = 0.0;
                    if (num20 > 0.0)
                    {
                        break;
                    }
                    while (num20 < 0.0)
                    {
                        num21 += num20;
                        num20 = (0.9 * num20) + (tTable[15] * 0.1);
                    }
                    if ((num21 * 0.1) >= (0.9 * num18))
                    {
                        break;
                    }
                    index++;
                }
            }
            GlobalVar.decisions[which].TCode = index;
            if (GlobalVar.decisions[which].VCode > 15)
            {
                GlobalVar.decisions[which].VCode = 15;
            }
            if (GlobalVar.decisions[which].VCode < 0)
            {
                GlobalVar.decisions[which].VCode = 0;
            }
            if (GlobalVar.decisions[which].TCode > 15)
            {
                GlobalVar.decisions[which].TCode = 15;
            }
            if (GlobalVar.decisions[which].TCode < 0)
            {
                GlobalVar.decisions[which].TCode = 0;
            }
            num18 = Math.Abs(num18) + 0.651;
            if (num18 > 1.0)
            {
                GlobalVar.decisions[which].VCode = (int)(((double)GlobalVar.decisions[which].VCode) / (((num18 * num18) * num18) + 1.0));
            }
            if ((GlobalVar.decisions[which].TCode != 7) && (GlobalVar.decisions[which].VCode == 0))
            {
                GlobalVar.decisions[which].VCode = 1;
            }
        }
        
        public void position3(int which, double dx, double dz, double dv)
        {
            double velocityDirectionRad = GlobalVar.MyTeam.Fishes[which].VelocityDirectionRad;
            double num2 = GlobalVar.MyTeam.Fishes[which].PositionMm.X + (GlobalVar.Fish_centertohead * Math.Cos(velocityDirectionRad));
            double num3 = GlobalVar.MyTeam.Fishes[which].PositionMm.Z + (GlobalVar.Fish_centertohead * Math.Sin(velocityDirectionRad));
            double velocityMmPs = GlobalVar.MyTeam.Fishes[which].VelocityMmPs;
            double angularVelocityRadPs = GlobalVar.MyTeam.Fishes[which].AngularVelocityRadPs;
            double[] vTable = GlobalVar.vTable;
            double[] tTable = GlobalVar.tTable;
            double pI = GlobalVar.PI;
            double num7 = dx;
            double num8 = dz;
            double num9 = dv;
            double num10 = num2 + ((velocityMmPs * Math.Cos(velocityDirectionRad)) * 0.1);
            double num11 = num3 + ((velocityMmPs * Math.Sin(velocityDirectionRad)) * 0.1);
            double num12 = velocityDirectionRad + (angularVelocityRadPs * 0.1);
            double num13 = 0.0;
            double num14 = 0.0;
            double num15 = 0.0;
            double num16 = 0.0;
            if (dv < 0.0)
            {
                dv = 0.0;
            }
            if (dv > 314.0)
            {
                dv = 314.0;
            }
            num16 = Math.Sqrt(((num7 - num2) * (num7 - num2)) + ((num8 - num3) * (num8 - num3)));
            num13 = Math.Sqrt(((num7 - num10) * (num7 - num10)) + ((num8 - num11) * (num8 - num11)));
            int index = 15;
            while (index > 1)
            {
                num15 = (velocityMmPs * 0.9) + (vTable[index] * 0.1);
                num14 = 0.0;
                int num22 = 0;
                while ((num15 > dv) && (num22 < 0x17))
                {
                    num22++;
                    num14 += num15;
                    num15 = 0.9 * num15;
                }
                if ((num14 * 0.1) <= num13)
                {
                    break;
                }
                index--;
            }
            GlobalVar.decisions[which].VCode = index;
            double num18 = 0.0;
            double num20 = 0.0;
            double num21 = 0.0;
            num18 = Math.Atan2(num8 - num11, num7 - num10) - num12;
            if (num18 > pI)
            {
                num18 -= 2.0 * pI;
            }
            if (num18 < -pI)
            {
                num18 += 2.0 * pI;
            }
            if (num18 > 0.0)
            {
                for (index = 14; index > 0; index--)
                {
                    num20 = (angularVelocityRadPs * 0.9) + (tTable[index] * 0.1);
                    if ((angularVelocityRadPs < 0.0) && (num20 < 0.0))
                    {
                        break;
                    }
                    num21 = 0.0;
                    if (num20 <= 0.0)
                    {
                        break;
                    }
                    while (num20 > 0.0)
                    {
                        num21 += num20;
                        num20 = (0.9 * num20) + (tTable[0] * 0.1);
                    }
                    if ((num21 * 0.1) <= num18)
                    {
                        break;
                    }
                }
            }
            else
            {
                index = 0;
                while (index < 14)
                {
                    num20 = (angularVelocityRadPs * 0.9) + (tTable[index] * 0.1);
                    if ((angularVelocityRadPs > 0.0) && (num20 >= 0.0))
                    {
                        break;
                    }
                    num21 = 0.0;
                    if (num20 > 0.0)
                    {
                        break;
                    }
                    while (num20 < 0.0)
                    {
                        num21 += num20;
                        num20 = (0.9 * num20) + (tTable[14] * 0.1);
                    }
                    if ((num21 * 0.1) >= num18)
                    {
                        break;
                    }
                    index++;
                }
            }
            GlobalVar.decisions[which].TCode = index;
            if (GlobalVar.decisions[which].VCode > 15)
            {
                GlobalVar.decisions[which].VCode = 15;
            }
            if (GlobalVar.decisions[which].VCode < 0)
            {
                GlobalVar.decisions[which].VCode = 0;
            }
            if (GlobalVar.decisions[which].TCode > 15)
            {
                GlobalVar.decisions[which].TCode = 15;
            }
            if (GlobalVar.decisions[which].TCode < 0)
            {
                GlobalVar.decisions[which].TCode = 0;
            }
            num18 = Math.Abs(num18) + 0.651;
            if (num18 > 1.0)
            {
                GlobalVar.decisions[which].VCode = (int)(((double)GlobalVar.decisions[which].VCode) / ((num18 * num18) + 1.0));
            }
            if ((GlobalVar.decisions[which].TCode != 7) && (GlobalVar.decisions[which].VCode == 0))
            {
                GlobalVar.decisions[which].VCode = 1;
            }
        }

        public void position3_1(int which, double target_x, double target_z, double target_v, int tag)
        {
            int num18;
            double velocityDirectionRad = GlobalVar.MyTeam.Fishes[which].VelocityDirectionRad;
            double x = GlobalVar.MyTeam.Fishes[which].PositionMm.X;
            double z = GlobalVar.MyTeam.Fishes[which].PositionMm.Z;
            if (tag == 0)
            {
                x = GlobalVar.MyTeam.Fishes[which].PolygonVertices[0].X;
                z = GlobalVar.MyTeam.Fishes[which].PolygonVertices[0].Z;
            }
            double velocityMmPs = GlobalVar.MyTeam.Fishes[which].VelocityMmPs;
            double angularVelocityRadPs = GlobalVar.MyTeam.Fishes[which].AngularVelocityRadPs;
            double pI = GlobalVar.PI;
            double num7 = x + ((velocityMmPs * Math.Cos(velocityDirectionRad)) * 0.1);
            double num8 = z + ((velocityMmPs * Math.Sin(velocityDirectionRad)) * 0.1);
            double num9 = this.Compute_Distance(x, z, target_x, target_z);
            double num10 = this.Compute_Distance(num7, num8, target_x, target_z);
            double num11 = velocityDirectionRad + (angularVelocityRadPs * 0.1);
            double num12 = 0.0;
            double num13 = 0.0;
            double ang = 0.0;
            double num16 = 0.0;
            ang = Math.Atan2(target_z - num8, target_x - num7) - num11;
            ang = this.FormatAngleRad(ang);
            int index = 15;
            while (index > 1)
            {
                num13 = (velocityMmPs * 0.9) + (GlobalVar.vTable[index] * 0.1);
                num12 = 0.0;
                int num19 = 0;
                while ((num13 > target_v) && (num19 < 0x12))
                {
                    num19++;
                    num12 += num13 * 0.1;
                    num13 = (0.9 * num13) + (GlobalVar.vTable[index] * 0.1);
                }
                if (num12 <= num10)
                {
                    break;
                }
                index--;
            }
            if (index > 15)
            {
                index = 15;
            }
            if (index < 0)
            {
                index = 0;
            }
            GlobalVar.decisions[which].VCode = index;
            if (ang > 0.0)
            {
                for (num18 = 14; num18 > 0; num18--)
                {
                    num16 = (angularVelocityRadPs * 0.9) + (GlobalVar.tTable[num18] * 0.1);
                    if ((angularVelocityRadPs < 0.0) && (num16 < 0.0))
                    {
                        break;
                    }
                    double num20 = 0.0;
                    if (num16 <= 0.0)
                    {
                        break;
                    }
                    while (num16 > 0.0)
                    {
                        num20 += num16 * 0.1;
                        num16 = (0.9 * num16) + (GlobalVar.tTable[0] * 0.1);
                    }
                    if (num20 <= ang)
                    {
                        break;
                    }
                }
            }
            else
            {
                num18 = 0;
                while (num18 < 14)
                {
                    num16 = (angularVelocityRadPs * 0.9) + (GlobalVar.tTable[num18] * 0.1);
                    if ((angularVelocityRadPs > 0.0) && (num16 >= 0.0))
                    {
                        break;
                    }
                    double num21 = 0.0;
                    if (num16 > 0.0)
                    {
                        break;
                    }
                    while (num16 < 0.0)
                    {
                        num21 += num16 * 0.1;
                        num16 = (0.9 * num16) + (GlobalVar.tTable[14] * 0.1);
                    }
                    if (num21 >= ang)
                    {
                        break;
                    }
                    num18++;
                }
            }
            if (num18 > 15)
            {
                num18 = 15;
            }
            if (num18 < 0)
            {
                num18 = 0;
            }
            GlobalVar.decisions[which].TCode = num18;
            this.previous_w = this.current_w;
            this.current_w = num18;
            ang = Math.Abs(ang) + 0.651;
            if (ang > 1.0)
            {
                GlobalVar.decisions[which].VCode = (int)(((double)GlobalVar.decisions[which].VCode) / ((ang * ang) + 1.0));
            }
            if ((GlobalVar.decisions[which].TCode != 7) && (GlobalVar.decisions[which].VCode == 0))
            {
                GlobalVar.decisions[which].VCode = 1;
            }
        }
        //******************************************************************

        public void GoBack(int fishIndex, double targetX, double targetZ, double desiredVelocity)
        {
            GlobalVar.decisions[fishIndex].VCode = 15;

            double fishPosX = GlobalVar.MyTeam.Fishes[fishIndex].PolygonVertices[0].X;
            double fishPosZ = GlobalVar.MyTeam.Fishes[fishIndex].PolygonVertices[0].Z;
            double bodyDirection = GlobalVar.MyTeam.Fishes[fishIndex].BodyDirectionRad;
            double currentVelocity = GlobalVar.MyTeam.Fishes[fishIndex].VelocityMmPs;
            double currentAngularVelocity = GlobalVar.MyTeam.Fishes[fishIndex].AngularVelocityRadPs;

            double adjustedVelocity = 1.0;
            int velocityCodeIndex = 0;
            int[] velocityTable = new int[] { 0, 10, 35, 67, 98, 112, 135, 154, 175, 227, 273, 291, 298, 294, 307, 317 };

            double distanceToTarget = Math.Sqrt(Math.Pow(fishPosZ - targetZ, 2) + Math.Pow(fishPosX - targetX, 2));
            double targetDirection = Math.Atan2(targetZ - fishPosZ, targetX - fishPosX);

            // Adjust target direction based on quadrant
            if (fishPosX > targetX && fishPosZ > targetZ)
            {
                targetDirection -= GlobalVar.PI;
            }
            else if (fishPosX > targetX && fishPosZ < targetZ)
            {
                targetDirection += GlobalVar.PI;
            }

            // Clamp desired velocity within allowable range
            desiredVelocity = Math.Max(0.0, Math.Min(317.0, desiredVelocity));

            velocityCodeIndex = 15;
            while (velocityCodeIndex > 0)
            {
                double projectedPosX = fishPosX + (currentVelocity * 0.1 * Math.Cos(targetDirection));
                double projectedPosZ = fishPosZ + (currentVelocity * 0.1 * Math.Sin(targetDirection));
                adjustedVelocity = currentVelocity;
                GlobalVar.n = 0;

                while (adjustedVelocity > (desiredVelocity + 5.0) && GlobalVar.n < 25)
                {
                    adjustedVelocity = (adjustedVelocity * 0.9) + (velocityTable[velocityCodeIndex] * 0.1);
                    projectedPosX += adjustedVelocity * 0.1 * Math.Cos(targetDirection);
                    projectedPosZ += adjustedVelocity * 0.1 * Math.Sin(targetDirection);
                    GlobalVar.n++;
                }

                if (Math.Sqrt(Math.Pow(fishPosZ - projectedPosZ, 2) + Math.Pow(fishPosX - projectedPosX, 2)) < distanceToTarget)
                {
                    break;
                }

                velocityCodeIndex--;
            }

            if (Math.Abs(targetDirection - bodyDirection) > 0.4)
            {
                velocityCodeIndex = 1;
            }

            GlobalVar.decisions[fishIndex].VCode = velocityCodeIndex;
            GlobalVar.BaAction.Angle(fishIndex, targetDirection);
        }

        //*******************************(谨慎修改)
        public void the_warrior2(int i, int j)
        {
            int num11;
            GlobalVar.corner[i] = 0;
            GlobalVar.corner_help[i] = 0;
            Platform pointd = new Platform();
            Platform pointd2 = new Platform();
            Platform pointd3 = new Platform();
            Platform pointd4 = new Platform();
            double num = 1.0;
            if ((j >= 0) && (j <= 2))
            {
                num = (num * 38.0) / 58.0;
            }
            else if ((j >= 3) && (j <= 6))
            {
                num = (num * 78.0) / 58.0;
            }
            pointd.Posx = GlobalVar.mission.EnvRef.Balls[j].PositionMm.X;
            pointd.Posz = GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z;
            pointd2.Posx = GlobalVar.MyTeam.Fishes[i].PositionMm.X;
            pointd2.Posz = GlobalVar.MyTeam.Fishes[i].PositionMm.Z;
            pointd4.Posx = (GlobalVar.MyTeam.Fishes[i].PolygonVertices[3].X + GlobalVar.MyTeam.Fishes[i].PolygonVertices[4].X) / 2f;
            pointd4.Posz = (GlobalVar.MyTeam.Fishes[i].PolygonVertices[3].Z + GlobalVar.MyTeam.Fishes[i].PolygonVertices[4].Z) / 2f;
            double pI = GlobalVar.PI;
            double velocityDirectionRad = GlobalVar.MyTeam.Fishes[i].VelocityDirectionRad;
            double[] numArray = new double[] { 360.0, 360.0 };
            pointd3.Posx = GlobalVar.MyTeam.Fishes[i].PolygonVertices[0].X;
            pointd3.Posz = GlobalVar.MyTeam.Fishes[i].PolygonVertices[0].Z;
            double num4 = this.Compute_Distance(pointd3.Posx, pointd3.Posz, pointd.Posx, pointd.Posz);
            double num5 = this.Compute_Distance(pointd2.Posx, pointd2.Posz, pointd.Posx, pointd.Posz);
            GlobalVar.test_to_twist_x[i, GlobalVar.cnt] = pointd3.Posx;
            GlobalVar.test_to_twist_ballx[i, GlobalVar.cnt] = pointd.Posx;
            GlobalVar.differencial_x[i, GlobalVar.cnt] = Math.Abs((double)(pointd3.Posx - pointd.Posx));
            GlobalVar.test_to_twist_z[i, GlobalVar.cnt] = pointd3.Posz;
            GlobalVar.test_to_twist_ballz[i, GlobalVar.cnt] = pointd.Posz;
            GlobalVar.differencial_z[i, GlobalVar.cnt] = Math.Abs((double)(pointd3.Posz - pointd.Posz));
            GlobalVar.recordfishz[i, GlobalVar.GFY] = Math.Abs(pointd2.Posz);
            GlobalVar.GFY++;
            GlobalVar.cnt++;
            if (GlobalVar.cnt >= 5)
            {
                GlobalVar.cnt = 0;
            }
            if (GlobalVar.GFY >= 20)
            {
                GlobalVar.GFY = 0;
            }
            double velocityMmPs = GlobalVar.MyTeam.Fishes[i].VelocityMmPs;
            double angularVelocityRadPs = GlobalVar.MyTeam.Fishes[i].AngularVelocityRadPs;
            double num9 = pointd3.Posx - (444.0 * Math.Cos(velocityDirectionRad));
            double num10 = pointd3.Posz - (444.0 * Math.Sin(velocityDirectionRad));
            if (pointd.Posz > 0.0)
            {
                num11 = 1;
            }
            else
            {
                num11 = -1;
            }
            if (GlobalVar.teamId == 0)
            {
                if (GlobalVar.danyuchujie[i] == 0)
                {
                    if (num4 > 420.0)
                    {
                        this.GoBack(i, pointd.Posx - 80.0, pointd.Posz, 200.0);
                        GlobalVar.warrior[i] = 0;
                    }
                    else if (pointd.Posx < -1250.0)
                    {
                        if (velocityDirectionRad < 0.0)
                        {
                            this.Position(i, pointd.Posx - 80.0, pointd.Posz - 50.0, 50.0);
                            this.KeepV(i, 50.0);
                        }
                        else
                        {
                            this.Position(i, pointd.Posx - 80.0, pointd.Posz + 50.0, 50.0);
                            this.KeepV(i, 50.0);
                        }
                        if (GlobalVar.decisions[i].TCode > 10)
                        {
                            GlobalVar.decisions[i].TCode = 10;
                        }
                        if (GlobalVar.decisions[i].TCode < 4)
                        {
                            GlobalVar.decisions[i].TCode = 4;
                        }
                        if ((((velocityDirectionRad < 0.0) && (pointd2.Posx < pointd.Posx)) && (pointd3.Posz < (pointd.Posz - 10.0))) && (num10 > (pointd.Posz + (58.0 * num))))
                        {
                            GlobalVar.decisions[i].TCode = 10;
                            if (num5 > 95.0)
                            {
                                this.KeepV(i, 25.0);
                            }
                            else
                            {
                                this.KeepV(i, 50.0);
                            }
                        }
                        else if ((((velocityDirectionRad > 0.0) && (pointd2.Posx < pointd.Posx)) && (pointd3.Posz > pointd.Posz)) && (num10 < (pointd.Posz - (58.0 * num))))
                        {
                            GlobalVar.decisions[i].TCode = 3;
                            this.KeepV(i, 1.0);
                        }
                    }
                    else
                    {
                        if ((((velocityDirectionRad < 0.0) && (num4 < 400.0)) && (((pointd3.Posx > (pointd.Posx - 100.0)) && (pointd3.Posz < (pointd.Posz - 80.0))) || ((pointd3.Posx > (pointd.Posx - 40.0)) && (pointd3.Posz < (pointd.Posz - 40.0))))) && (num9 < pointd.Posx))
                        {
                            this.KeepV(i, 10.0);
                            GlobalVar.decisions[i].TCode = 10;
                            if (velocityDirectionRad > ((-1.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = -1;
                            }
                        }
                        else if ((((velocityDirectionRad > 0.0) && (num4 < 400.0)) && (((pointd3.Posx > (pointd.Posx - 100.0)) && (pointd3.Posz > (pointd.Posz + 80.0))) || ((pointd3.Posx > (pointd.Posx - 40.0)) && (pointd3.Posz > (pointd.Posz + 40.0))))) && (num9 < pointd.Posx))
                        {
                            this.KeepV(i, 0.0);
                            GlobalVar.decisions[i].TCode = 4;
                            if (velocityDirectionRad < (pI / 4.0))
                            {
                                GlobalVar.warrior[i] = 1;
                            }
                        }
                        else if (velocityDirectionRad < 0.0)
                        {
                            this.Position(i, pointd.Posx - 60.0, pointd.Posz - 50.0, 100.0);
                        }
                        else
                        {
                            this.Position(i, pointd.Posx - 60.0, pointd.Posz + 50.0, 100.0);
                        }
                        if (GlobalVar.warrior[i] == -1)
                        {
                            GlobalVar.decisions[i].TCode = 1;
                            this.KeepV(i, 1.0);
                            if (velocityDirectionRad <= ((-3.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = 0;
                            }
                        }
                        if (GlobalVar.warrior[i] == 1)
                        {
                            GlobalVar.decisions[i].TCode = 15;
                            this.KeepV(i, 1.0);
                            if (velocityDirectionRad >= ((3.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = 0;
                            }
                        }
                    }
                }
            }
            else if (GlobalVar.teamId == 1)
            {
                if ((GlobalVar.danyuchujie[i] == 0) || (GlobalVar.flag_duqiumen == 1))
                {
                    if (num4 > 420.0)
                    {
                        this.GoBack(i, pointd.Posx - 80.0, pointd.Posz, 200.0);
                        GlobalVar.warrior[i] = 0;
                    }
                    else if (pointd.Posx < -1250.0)
                    {
                        if (velocityDirectionRad < 0.0)
                        {
                            this.Position(i, pointd.Posx - 80.0, pointd.Posz - 50.0, 50.0);
                        }
                        else
                        {
                            this.Position(i, pointd.Posx - 80.0, pointd.Posz + 50.0, 50.0);
                        }
                        if ((((velocityDirectionRad > 0.0) && (pointd2.Posx < pointd.Posx)) && (pointd3.Posz > (pointd.Posz + 10.0))) && (num10 < (pointd.Posz - (58.0 * num))))
                        {
                            GlobalVar.decisions[i].TCode = 4;
                            if (num5 > 95.0)
                            {
                                this.KeepV(i, 25.0);
                            }
                            else
                            {
                                this.KeepV(i, 50.0);
                            }
                        }
                    }
                    else
                    {
                        if ((((velocityDirectionRad < 0.0) && (num4 < 400.0)) && (((pointd3.Posx > (pointd.Posx - 100.0)) && (pointd3.Posz < (pointd.Posz - 80.0))) || ((pointd3.Posx > (pointd.Posx - 40.0)) && (pointd3.Posz < (pointd.Posz - 40.0))))) && (num9 < pointd.Posx))
                        {
                            this.KeepV(i, 10.0);
                            GlobalVar.decisions[i].TCode = 11;
                            if (velocityDirectionRad > ((-1.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = -1;
                            }
                        }
                        else if ((((velocityDirectionRad > 0.0) && (num4 < 400.0)) && ((pointd3.Posx > (pointd.Posx - 100.0)) && (pointd3.Posz > pointd.Posz))) && (num9 < pointd.Posx))
                        {
                            this.KeepV(i, 25.0);
                            GlobalVar.decisions[i].TCode = 3;
                            if (velocityDirectionRad < (pI / 4.0))
                            {
                                GlobalVar.warrior[i] = 1;
                            }
                        }
                        else if (velocityDirectionRad < 0.0)
                        {
                            this.Position(i, pointd.Posx - 60.0, pointd.Posz - 50.0, 100.0);
                        }
                        else
                        {
                            this.Position(i, pointd.Posx - 60.0, pointd.Posz + 50.0, 100.0);
                        }
                        if (GlobalVar.warrior[i] == -1)
                        {
                            GlobalVar.decisions[i].TCode = 0;
                            this.KeepV(i, 0.0);
                            if (velocityDirectionRad <= ((-3.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = 0;
                            }
                        }
                        if (GlobalVar.warrior[i] == 1)
                        {
                            GlobalVar.decisions[i].TCode = 15;
                            this.KeepV(i, 0.0);
                            if (velocityDirectionRad >= ((3.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = 0;
                            }
                        }
                    }
                }
            }
            if (GlobalVar.flag_duqiumen != 1)
            {
                if ((((pointd.Posx < -1350.0) && (pointd.Posz < -450.0)) && (pointd.Posz > -720.0)) || (((pointd.Posx < -1152.0) && (pointd.Posz < -507.0)) && (pointd.Posz > -720.0)))
                {
                    GlobalVar.danyuchujie[i] = 1;
                    if ((pointd2.Posz > pointd.Posz) && (pointd4.Posz > pointd.Posz))
                    {
                        if (GlobalVar.teamId == 0)
                        {
                            this.Position(i, pointd.Posx + (70.0 * num), pointd.Posz - 70.0, 90.0);
                        }
                        else if (GlobalVar.teamId == 1)
                        {
                            this.Position(i, pointd.Posx + 70.0, pointd.Posz - 100.0, 90.0);
                        }
                    }
                    else
                    {
                        this.Dribblehometest1(i, j);
                    }
                }
                else if (((pointd.Posx < -1152.0) && (pointd.Posz > 507.0)) && (pointd.Posz < 720.0))
                {
                    GlobalVar.danyuchujie[i] = 1;
                    if ((pointd2.Posz < pointd.Posz) && (pointd4.Posz < pointd.Posz))
                    {
                        this.Position(i, pointd.Posx + 70.0, pointd.Posz + 70.0, 50.0);
                    }
                    else
                    {
                        this.Dribblehometest1(i, j);
                    }
                }
                else if (GlobalVar.danyuchujie[i] == 1)
                {
                    this.Dribblehometest1(i, j);
                }
                else if ((pointd.Posx >= -950.0) || (Math.Abs(pointd.Posz) >= 507.0))
                {
                    this.Dribblehometest1(i, j);
                }
                if ((pointd2.Posx > -920.0) && this.IsInMyUnArea_02((double)GlobalVar.mission.EnvRef.Balls[j].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z))
                {
                    GlobalVar.BaAction.Position(i, -1100.0, (double)(num11 * 700), 200.0);
                }
                if (this.IsInOppUnArea_01(pointd2.Posx, pointd2.Posz) && !this.IsInOppUnArea_01(pointd.Posx, pointd.Posz))
                {
                    int num12;
                    if (i == 0)
                    {
                        num12 = 1;
                    }
                    else
                    {
                        num12 = -1;
                    }
                    this.Position(i, 1350.0, (double)(num12 * 700), 200.0);
                    if (Math.Abs(pointd2.Posz) > 600.0)
                    {
                        this.Position(i, 800.0, (double)(num12 * 700), 200.0);
                    }
                }
                else
                {
                    int num6 = 0;
                    while (num6 <= 0x13)
                    {
                        if (GlobalVar.recordfishz[i, num6] < 360.0)
                        {
                            break;
                        }
                        num6++;
                    }
                    if (num6 == 20)
                    {
                        numArray[i] = 360.0;
                    }
                    else
                    {
                        numArray[i] = 450.0;
                    }
                    if (Math.Abs(pointd.Posz) < numArray[i])
                    {
                        GlobalVar.danyuchujie[i] = 0;
                    }
                    num6 = 0;
                    while (num6 <= 4)
                    {
                        if ((GlobalVar.differencial_x[i, num6] > 40.0) || (GlobalVar.differencial_z[i, num6] > 65.0))
                        {
                            break;
                        }
                        num6++;
                    }
                    if (num6 == 5)
                    {
                        this.KeepV(i, 10.0);
                        if (velocityDirectionRad <= 0.0)
                        {
                            GlobalVar.decisions[i].TCode = 0;
                        }
                        else
                        {
                            GlobalVar.decisions[i].TCode = 7;
                        }
                    }
                }
            }
        }

        public void the_warrior2_special(int i, int j)
        {
            int num7;
            GlobalVar.corner[i] = 0;
            GlobalVar.corner_help[i] = 0;
            Platform pointd = new Platform();
            Platform pointd2 = new Platform();
            Platform pointd3 = new Platform();
            pointd.Posx = GlobalVar.mission.EnvRef.Balls[j].PositionMm.X;
            pointd.Posz = GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z;
            pointd2.Posx = GlobalVar.MyTeam.Fishes[i].PositionMm.X;
            pointd2.Posz = GlobalVar.MyTeam.Fishes[i].PositionMm.Z;
            double pI = GlobalVar.PI;
            double velocityDirectionRad = GlobalVar.MyTeam.Fishes[i].VelocityDirectionRad;
            pointd3.Posx = GlobalVar.MyTeam.Fishes[i].PolygonVertices[0].X;
            pointd3.Posz = GlobalVar.MyTeam.Fishes[i].PolygonVertices[0].Z;
            double num3 = this.Compute_Distance(pointd3.Posx, pointd3.Posz, pointd.Posx, pointd.Posz);
            double velocityMmPs = GlobalVar.MyTeam.Fishes[i].VelocityMmPs;
            double angularVelocityRadPs = GlobalVar.MyTeam.Fishes[i].AngularVelocityRadPs;
            double num6 = pointd3.Posx - (444.0 * Math.Cos(velocityDirectionRad));
            if (i == 0)
            {
                num7 = 1;
            }
            else
            {
                num7 = -1;
            }
            if (GlobalVar.teamId == 0)
            {
                if (GlobalVar.danyuchujie[i] == 0)
                {
                    if (num3 > 420.0)
                    {
                        this.GoBack(i, pointd.Posx - 80.0, pointd.Posz, 200.0);
                        GlobalVar.warrior[i] = 0;
                    }
                    else if (pointd.Posx < -1250.0)
                    {
                        if (velocityDirectionRad < 0.0)
                        {
                            this.Position(i, pointd.Posx - 80.0, pointd.Posz - 50.0, 50.0);
                        }
                        else
                        {
                            this.Position(i, pointd.Posx - 80.0, pointd.Posz + 50.0, 50.0);
                        }
                        if (((velocityDirectionRad < 0.0) && (pointd2.Posx < pointd.Posx)) && (pointd3.Posz < (pointd.Posz - 10.0)))
                        {
                            GlobalVar.decisions[i].TCode = 10;
                            this.KeepV(i, 30.0);
                        }
                        else if (((velocityDirectionRad > 0.0) && (pointd2.Posx < pointd.Posx)) && (pointd3.Posz > (pointd.Posz - 30.0)))
                        {
                            GlobalVar.decisions[i].TCode = 3;
                            this.KeepV(i, 0.0);
                        }
                    }
                    else
                    {
                        if ((((velocityDirectionRad < 0.0) && (num3 < 400.0)) && (((pointd3.Posx > (pointd.Posx - 100.0)) && (pointd3.Posz < (pointd.Posz - 80.0))) || ((pointd3.Posx > (pointd.Posx - 40.0)) && (pointd3.Posz < (pointd.Posz - 40.0))))) && (num6 < pointd.Posx))
                        {
                            this.KeepV(i, 10.0);
                            GlobalVar.decisions[i].TCode = 13;
                            if (velocityDirectionRad > ((-1.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = -1;
                            }
                        }
                        else if ((((velocityDirectionRad > 0.0) && (num3 < 400.0)) && (((pointd3.Posx > (pointd.Posx - 100.0)) && (pointd3.Posz > (pointd.Posz + 80.0))) || ((pointd3.Posx > (pointd.Posx - 40.0)) && (pointd3.Posz > (pointd.Posz + 40.0))))) && (num6 < pointd.Posx))
                        {
                            this.KeepV(i, 0.0);
                            GlobalVar.decisions[i].TCode = 1;
                            if (velocityDirectionRad < (pI / 4.0))
                            {
                                GlobalVar.warrior[i] = 1;
                            }
                        }
                        else if (velocityDirectionRad < 0.0)
                        {
                            this.Position(i, pointd.Posx - 60.0, pointd.Posz - 50.0, 100.0);
                        }
                        else
                        {
                            this.Position(i, pointd.Posx - 60.0, pointd.Posz + 50.0, 100.0);
                        }
                        if (GlobalVar.warrior[i] == -1)
                        {
                            GlobalVar.decisions[i].TCode = 1;
                            this.KeepV(i, 0.0);
                            if (velocityDirectionRad <= ((-3.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = 0;
                            }
                        }
                        if (GlobalVar.warrior[i] == 1)
                        {
                            GlobalVar.decisions[i].TCode = 15;
                            this.KeepV(i, 0.0);
                            if (velocityDirectionRad >= ((3.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = 0;
                            }
                        }
                    }
                }
            }
            else if (GlobalVar.teamId == 1)
            {
                if (GlobalVar.danyuchujie[i] == 0)
                {
                    if (num3 > 420.0)
                    {
                        this.GoBack(i, pointd.Posx - 80.0, pointd.Posz, 200.0);
                        GlobalVar.warrior[i] = 0;
                    }
                    else if (pointd.Posx < -1250.0)
                    {
                        if (velocityDirectionRad < 0.0)
                        {
                            this.Position(i, pointd.Posx - 80.0, pointd.Posz - 50.0, 50.0);
                        }
                        else
                        {
                            this.Position(i, pointd.Posx - 80.0, pointd.Posz + 50.0, 50.0);
                        }
                        if (((velocityDirectionRad > 0.0) && (pointd2.Posx < pointd.Posx)) && (pointd3.Posz > (pointd.Posz + 10.0)))
                        {
                            GlobalVar.decisions[i].TCode = 4;
                            this.KeepV(i, 30.0);
                        }
                    }
                    else
                    {
                        if ((((velocityDirectionRad < 0.0) && (num3 < 400.0)) && (((pointd3.Posx > (pointd.Posx - 100.0)) && (pointd3.Posz < (pointd.Posz - 80.0))) || ((pointd3.Posx > (pointd.Posx - 40.0)) && (pointd3.Posz < (pointd.Posz - 40.0))))) && (num6 < pointd.Posx))
                        {
                            this.KeepV(i, 10.0);
                            GlobalVar.decisions[i].TCode = 11;
                            if (velocityDirectionRad > ((-1.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = -1;
                            }
                        }
                        else if ((((velocityDirectionRad > 0.0) && (num3 < 400.0)) && (((pointd3.Posx > (pointd.Posx - 100.0)) && (pointd3.Posz > (pointd.Posz + 80.0))) || ((pointd3.Posx > (pointd.Posx - 40.0)) && (pointd3.Posz > (pointd.Posz + 40.0))))) && (num6 < pointd.Posx))
                        {
                            this.KeepV(i, 10.0);
                            GlobalVar.decisions[i].TCode = 2;
                            if (velocityDirectionRad < (pI / 4.0))
                            {
                                GlobalVar.warrior[i] = 1;
                            }
                        }
                        else if (velocityDirectionRad < 0.0)
                        {
                            this.Position(i, pointd.Posx - 60.0, pointd.Posz - 50.0, 100.0);
                        }
                        else
                        {
                            this.Position(i, pointd.Posx - 60.0, pointd.Posz + 50.0, 100.0);
                        }
                        if (GlobalVar.warrior[i] == -1)
                        {
                            GlobalVar.decisions[i].TCode = 0;
                            this.KeepV(i, 0.0);
                            if (velocityDirectionRad <= ((-3.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = 0;
                            }
                        }
                        if (GlobalVar.warrior[i] == 1)
                        {
                            GlobalVar.decisions[i].TCode = 13;
                            this.KeepV(i, 0.0);
                            if (velocityDirectionRad >= ((3.0 * pI) / 4.0))
                            {
                                GlobalVar.warrior[i] = 0;
                            }
                        }
                    }
                }
            }
            if ((((pointd.Posx < -1350.0) && (pointd.Posz < -450.0)) && (pointd.Posz > -720.0)) || (((pointd.Posx < -1152.0) && (pointd.Posz < -507.0)) && (pointd.Posz > -720.0)))
            {
                GlobalVar.danyuchujie[i] = 1;
                if (pointd3.Posz > pointd.Posz)
                {
                    if (GlobalVar.teamId == 0)
                    {
                        this.Position(i, pointd.Posx + 70.0, pointd.Posz - 70.0, 90.0);
                    }
                    else if (GlobalVar.teamId == 1)
                    {
                        this.Position(i, pointd.Posx + 70.0, pointd.Posz - 100.0, 90.0);
                    }
                }
                else if (pointd3.Posz < pointd.Posz)
                {
                    this.Dribblehometest1(i, j);
                }
            }
            else if ((((pointd.Posx < -1350.0) && (pointd.Posz > 450.0)) && (pointd.Posz < 720.0)) || (((pointd.Posx < -1152.0) && (pointd.Posz > 507.0)) && (pointd.Posz < 720.0)))
            {
                GlobalVar.danyuchujie[i] = 1;
                if (pointd3.Posz < pointd.Posz)
                {
                    this.Position(i, pointd.Posx + 70.0, pointd.Posz + 70.0, 50.0);
                }
                else if (pointd3.Posz > pointd.Posz)
                {
                    this.Dribblehometest1(i, j);
                }
            }
            else if (GlobalVar.danyuchujie[i] == 1)
            {
                this.Dribblehometest1(i, j);
            }
            else if ((pointd.Posx >= -950.0) || (Math.Abs(pointd.Posz) >= 507.0))
            {
                this.Dribblehometest1(i, j);
            }
            if ((pointd2.Posx > -920.0) && this.IsInMyUnArea_02((double)GlobalVar.mission.EnvRef.Balls[j].PositionMm.X, (double)GlobalVar.mission.EnvRef.Balls[j].PositionMm.Z))
            {
                GlobalVar.BaAction.Position(i, -1100.0, (double)(num7 * 700), 200.0);
            }
            if (this.IsInOppUnArea_01(pointd2.Posx, pointd2.Posz) && !this.IsInOppUnArea_01(pointd.Posx, pointd.Posz))
            {
                int num8;
                if (i == 0)
                {
                    num8 = 1;
                }
                else
                {
                    num8 = -1;
                }
                this.Position(i, 1350.0, (double)(num8 * 700), 200.0);
                if (Math.Abs(pointd2.Posz) > 600.0)
                {
                    this.Position(i, 800.0, (double)(num8 * 700), 200.0);
                }
            }
            else if (Math.Abs(pointd.Posz) < 360.0)
            {
                GlobalVar.danyuchujie[i] = 0;
            }
        }
        //******************************************************************
           
        public int Angle_V(double tempjiaodu)
        {
            if (0.0 == tempjiaodu)
            {
                return 7;
            }
            if (tempjiaodu < 0.0)
            {
                if ((-0.0269 <= tempjiaodu) && (0.0 > tempjiaodu))
                {
                    if ((0.0 - tempjiaodu) >= (tempjiaodu + 0.0269))
                    {
                        return 6;
                    }
                    return 7;
                }
                if ((-0.04508 <= tempjiaodu) && (-0.0269 > tempjiaodu))
                {
                    if ((-0.0269 - tempjiaodu) >= (tempjiaodu + 0.04508))
                    {
                        return 5;
                    }
                    return 6;
                }
                if ((-0.071013 <= tempjiaodu) && (-0.04508 > tempjiaodu))
                {
                    if ((-0.04508 - tempjiaodu) >= (tempjiaodu + 0.071013))
                    {
                        return 4;
                    }
                    return 5;
                }
                if ((-0.09953 <= tempjiaodu) && (-0.071013 > tempjiaodu))
                {
                    if ((-0.071013 - tempjiaodu) >= (tempjiaodu + 0.09953))
                    {
                        return 3;
                    }
                    return 4;
                }
                if ((-0.1265 <= tempjiaodu) && (-0.09953 > tempjiaodu))
                {
                    if ((-0.09953 - tempjiaodu) >= (tempjiaodu + 0.1265))
                    {
                        return 2;
                    }
                    return 3;
                }
                if ((-0.168 <= tempjiaodu) && (-0.1265 > tempjiaodu))
                {
                    if ((-0.1265 - tempjiaodu) >= (tempjiaodu + 0.168))
                    {
                        return 1;
                    }
                    return 2;
                }
                if ((-0.20424 <= tempjiaodu) && (-0.168 > tempjiaodu))
                {
                    if ((-0.168 - tempjiaodu) >= (tempjiaodu + 0.20424))
                    {
                        return 0;
                    }
                    return 1;
                }
                return 0;
            }
            if ((0.0269 >= tempjiaodu) && (0.0 < tempjiaodu))
            {
                if ((tempjiaodu - 0.0) > (0.0269 - tempjiaodu))
                {
                    return 8;
                }
                return 7;
            }
            if ((0.04508 >= tempjiaodu) && (0.0269 < tempjiaodu))
            {
                if ((tempjiaodu - 0.0269) > (0.04508 - tempjiaodu))
                {
                    return 9;
                }
                return 8;
            }
            if ((0.071013 >= tempjiaodu) && (0.04508 < tempjiaodu))
            {
                if ((tempjiaodu - 0.04508) > (0.071013 - tempjiaodu))
                {
                    return 10;
                }
                return 9;
            }
            if ((0.09953 >= tempjiaodu) && (0.071013 < tempjiaodu))
            {
                if ((tempjiaodu - 0.071013) > (0.09953 - tempjiaodu))
                {
                    return 11;
                }
                return 10;
            }
            if ((0.1265 >= tempjiaodu) && (0.09953 < tempjiaodu))
            {
                if ((tempjiaodu - 0.09953) > (0.1265 - tempjiaodu))
                {
                    return 12;
                }
                return 11;
            }
            if ((0.168 >= tempjiaodu) && (0.1265 < tempjiaodu))
            {
                if ((tempjiaodu - 0.1265) > (0.168 - tempjiaodu))
                {
                    return 13;
                }
                return 12;
            }
            if ((0.1977 >= tempjiaodu) && (0.168 < tempjiaodu))
            {
                if ((tempjiaodu - 0.168) > (0.1977 - tempjiaodu))
                {
                    return 14;
                }
                return 13;
            }
            return 14;
        }
        
        //******************************************************************
        // 球轨迹预测拦截策略（新增）
        //******************************************************************

        /// <summary>
        /// 预测球在 secondsAhead 秒后的位置（线性预测）
        /// </summary>
        public Platform PredictBallPosition(int ballId, double secondsAhead)
        {
            Platform predicted = new Platform();
            Ball ball = GlobalVar.mission.EnvRef.Balls[ballId];
            double velocity = ball.VelocityMmPs;
            double direction = ball.VelocityDirectionRad;
            predicted.Posx = ball.PositionMm.X + velocity * Math.Cos(direction) * secondsAhead;
            predicted.Posz = ball.PositionMm.Z + velocity * Math.Sin(direction) * secondsAhead;
            return predicted;
        }

        /// <summary>
        /// 判断是否应该使用预测拦截策略：
        /// 球速度较快（>30mm/s）且距离鱼头较远（>350mm）时使用预测
        /// </summary>
        public bool ShouldInterceptBall(int fishId, int ballId)
        {
            if (ballId < 0 || ballId > 8)
                return false;

            Ball ball = GlobalVar.mission.EnvRef.Balls[ballId];
            double ballSpeed = ball.VelocityMmPs;
            double fishHeadX = GlobalVar.MyTeam.Fishes[fishId].PolygonVertices[0].X;
            double fishHeadZ = GlobalVar.MyTeam.Fishes[fishId].PolygonVertices[0].Z;
            double distance = Compute_Distance(fishHeadX, fishHeadZ,
                ball.PositionMm.X, ball.PositionMm.Z);

            // 球速>30mm/s 且 距离>350mm 时启用拦截预测
            return (ballSpeed > 30.0 && distance > 350.0);
        }

        /// <summary>
        /// 接近球的预测拦截位置。
        /// 计算鱼到达球当前位置所需时间，预测球在该时间后的位置，然后导航到预测点。
        /// </summary>
        public void InterceptBallApproach(int fishId, int ballId)
        {
            Ball ball = GlobalVar.mission.EnvRef.Balls[ballId];
            double ballX = ball.PositionMm.X;
            double ballZ = ball.PositionMm.Z;
            double ballSpeed = ball.VelocityMmPs;

            double fishHeadX = GlobalVar.MyTeam.Fishes[fishId].PolygonVertices[0].X;
            double fishHeadZ = GlobalVar.MyTeam.Fishes[fishId].PolygonVertices[0].Z;
            double fishSpeed = GlobalVar.MyTeam.Fishes[fishId].VelocityMmPs;

            // 使用预估速度（取期望速度和当前速度的较大值，避免低估）
            double approachSpeed = System.Math.Max(fishSpeed, 100.0);
            if (approachSpeed < 1.0) approachSpeed = 100.0;

            double currentDistance = Compute_Distance(fishHeadX, fishHeadZ, ballX, ballZ);

            // 预估到达时间 = 距离 / 接近速度
            double timeToReach = currentDistance / approachSpeed;

            // 限制预测时间窗口在 0.05~0.5 秒之间，避免极端预测
            if (timeToReach > 0.5)
                timeToReach = 0.5;
            if (timeToReach < 0.05)
                timeToReach = 0.05;

            Platform predicted = PredictBallPosition(ballId, timeToReach);

            // 确定 Z 轴偏置方向（Fish0 偏上，Fish1 偏下）
            int zSign;
            if (fishId == 0)
                zSign = 1;
            else
                zSign = -1;

            // 从预测位置的侧面接近球（偏置距离取决于球速）
            double lateralOffset = 60.0 + ballSpeed * 0.5;
            if (lateralOffset > 180.0)
                lateralOffset = 180.0;

            double targetX = predicted.Posx - 80.0;           // 在球前方（X 负方向）接近
            double targetZ = predicted.Posz + zSign * lateralOffset; // 侧面偏置

            // 修复：目标点 clamp 到场地安全区，避免鱼被派到界外撞墙
            if (targetX > 1950.0) targetX = 1950.0;
            if (targetX < -1950.0) targetX = -1950.0;
            if (targetZ > 1400.0) targetZ = 1400.0;
            if (targetZ < -1400.0) targetZ = -1400.0;

            double targetSpeed = 200.0 + ballSpeed;
            if (targetSpeed > 350.0)
                targetSpeed = 350.0;

            Position(fishId, targetX, targetZ, targetSpeed);
        }

        //******************************************************************
        // 对抗赛优化：A-已得分球判断 / B-抢断对方带球 / C-球门威胁防守（新增）
        //******************************************************************

        /// <summary>
        /// 判断球是否已经进入我方球门（镜像后 leftScore==1 表示已得分）
        /// 规则：同一球重复进同一球门不重复计分，故已得分球不应再被选来推
        /// </summary>
        public bool IsBallScoredInMyGoal(int ballId)
        {
            if (ballId < 0 || ballId > 8)
                return false;
            return GlobalVar.leftScore[ballId] == 1;
        }

        /// <summary>
        /// 寻找对方正在带球且向我方球门推进的球（B-抢断检测）
        /// 条件：球未进双方球门、球在我方半场(X<0)、球在向我方门移动(Vx<0)、对方鱼头离球较近
        /// </summary>
        public int FindOppDribblingBall()
        {
            for (int i = 0; i < 9; i++)
            {
                if (GlobalVar.leftScore[i] == 1) continue;   // 已进我方门
                if (GlobalVar.rightScore[i] == 1) continue;  // 已进对方门
                double bx = GlobalVar.mission.EnvRef.Balls[i].PositionMm.X;
                double bz = GlobalVar.mission.EnvRef.Balls[i].PositionMm.Z;
                if (bx > -400.0) continue;                   // 球必须已进入我方半场
                double speed = GlobalVar.mission.EnvRef.Balls[i].VelocityMmPs;
                double vx = 0.0;
                if (speed > 0.0)
                {
                    vx = speed * Math.Cos(GlobalVar.mission.EnvRef.Balls[i].VelocityDirectionRad);
                }
                if (vx > 20.0) continue;                     // 球必须朝向我方球门推进
                for (int j = 0; j < 2; j++)
                {
                    double fx = GlobalVar.OppTeam.Fishes[j].PolygonVertices[0].X;
                    double fz = GlobalVar.OppTeam.Fishes[j].PolygonVertices[0].Z;
                    if (Compute_Distance(fx, fz, bx, bz) < 300.0)
                    {
                        return i;                            // 对方鱼头距球很近，判定其在带球
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 寻找威胁我方球门的球（C-防守检测）
        /// 条件：球未进我方门、球已到我方禁区深部(X<-1100)、球静止或朝门方向移动
        /// </summary>
        public int FindGoalThreatBall()
        {
            for (int i = 0; i < 9; i++)
            {
                if (GlobalVar.leftScore[i] == 1) continue;   // 已进我方门，无需防守
                double bx = GlobalVar.mission.EnvRef.Balls[i].PositionMm.X;
                if (bx > -1100.0) continue;                  // 球必须靠近我方球门
                double speed = GlobalVar.mission.EnvRef.Balls[i].VelocityMmPs;
                double vx = 0.0;
                if (speed > 0.0)
                {
                    vx = speed * Math.Cos(GlobalVar.mission.EnvRef.Balls[i].VelocityDirectionRad);
                }
                if (speed > 20.0 && vx > -10.0) continue;    // 快速移动但方向背离球门
                return i;
            }
            return -1;
        }

        /// <summary>
        /// 球门威胁防守站位（C-防守执行）
        /// 去"球与我方球门之间"的位置堵住推进路径，Fish0 偏上、Fish1 偏下
        /// </summary>
        public void DefendGoalApproach(int fishId, int ballId)
        {
            double bx = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.X;
            double bz = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.Z;
            double zSign = (fishId == 0) ? 100.0 : -100.0;
            double targetX = bx - 250.0;    // 球门一侧 250mm
            double targetZ = bz + zSign;
            // 修复：目标点 clamp 到场地安全区，避免球贴近球门线时目标点出界导致撞墙
            if (targetX > 1950.0) targetX = 1950.0;
            if (targetX < -1950.0) targetX = -1950.0;
            if (targetZ > 1400.0) targetZ = 1400.0;
            if (targetZ < -1400.0) targetZ = -1400.0;
            Position(fishId, targetX, targetZ, 250.0);
        }

        //******************************************************************
        // 对抗赛优化：D-撞墙脱困 / E-相持破解 / F-智能劫球（新增）
        //******************************************************************

        /// <summary>
        /// 撞墙脱困（问题1修复）：
        /// 鱼贴墙（距边界<250mm）且速度极低（被卡死）时，强制转向背离墙的方向并加速脱离。
        /// 需在 Strategy.cs 的防呆滞 VCode=12 强制逻辑之后调用以覆盖其顶墙行为。
        /// </summary>
        public void UnstickFish(int fishId)
        {
            double v = GlobalVar.MyTeam.Fishes[fishId].VelocityMmPs;
            double w = GlobalVar.MyTeam.Fishes[fishId].AngularVelocityRadPs;
            if (v > 30.0 || w > 0.1)
            {
                return;    // 鱼还在正常运动，不干预
            }
            double x = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
            double z = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;
            double ang = GlobalVar.MyTeam.Fishes[fishId].BodyDirectionRad;
            double toCenterAng = 0.0;
            bool nearWall = false;
            if (x < -1900.0) { nearWall = true; toCenterAng = 0.0; }          // 左墙 → 朝 +X
            else if (x > 1900.0) { nearWall = true; toCenterAng = GlobalVar.PI; } // 右墙 → 朝 -X
            else if (z < -1300.0) { nearWall = true; toCenterAng = GlobalVar.PI / 2.0; }  // 上墙 → 朝 +Z
            else if (z > 1300.0) { nearWall = true; toCenterAng = -GlobalVar.PI / 2.0; }  // 下墙 → 朝 -Z
            if (!nearWall)
            {
                return;
            }
            double diff = this.FormatAngleRad(toCenterAng - ang);
            if (Math.Abs(diff) > 0.5)
            {
                GlobalVar.decisions[fishId].VCode = 8;
                GlobalVar.decisions[fishId].TCode = diff > 0 ? 11 : 3;   // 转向背离墙
            }
            else
            {
                GlobalVar.decisions[fishId].VCode = 12;                   // 已正对脱困方向，全速离开
                GlobalVar.decisions[fishId].TCode = 7;
            }
        }

        /// <summary>
        /// 相持检测（问题2修复）：
        /// 我方鱼速度<50、球速度<50、且存在一条对方鱼同时距我方鱼<450 且距球<450 → 判定相持。
        /// </summary>
        public bool IsInStalemate(int fishId, int ballId)
        {
            if (ballId < 0 || ballId > 8)
            {
                this.stalemateCount[fishId] = 0;
                return false;
            }
            double mx = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
            double mz = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;
            double bx = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.X;
            double bz = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.Z;
            double myAng = GlobalVar.MyTeam.Fishes[fishId].BodyDirectionRad;

            // 条件1：球必须在我方鱼头部前方 ±60° 且距离<300 —— 鱼正处于推球姿态
            double distToBall = Compute_Distance(mx, mz, bx, bz);
            // 条件2：存在一条对方鱼贴着球(<400)，且与我在球的两侧（球→我方 与 球→对方 向量点积<0，真正的对顶）
            bool nearOpp = false;
            bool twoSides = false;
            for (int j = 0; j < 2; j++)
            {
                double ox = GlobalVar.OppTeam.Fishes[j].PositionMm.X;
                double oz = GlobalVar.OppTeam.Fishes[j].PositionMm.Z;
                if (Compute_Distance(bx, bz, ox, oz) < 400.0)
                {
                    nearOpp = true;
                    double vmx = mx - bx;
                    double vmz = mz - bz;
                    double vox = ox - bx;
                    double voz = oz - bz;
                    double dot = (vmx * vox) + (vmz * voz);
                    if (dot < 0.0)
                    {
                        twoSides = true;
                    }
                    break;
                }
            }
            // 条件3：双方都推不动（我方鱼与球都低速）
            bool slowMe = GlobalVar.MyTeam.Fishes[fishId].VelocityMmPs <= 80.0;
            bool slowBall = GlobalVar.mission.EnvRef.Balls[ballId].VelocityMmPs <= 80.0;
            // 条件4：球在我方鱼头前 ±60°
            double angToBall = Math.Atan2(bz - mz, bx - mx);
            bool ballInFront = Math.Abs(this.FormatAngleRad(angToBall - myAng)) <= (GlobalVar.PI / 3.0);

            // 滑动窗口：条件满足→计数+1，不满足→计数-1（不清零，容忍偶发抖动）
            bool allMet = (distToBall <= 300.0) && ballInFront && nearOpp && twoSides && slowMe && slowBall;
            if (allMet)
            {
                this.stalemateCount[fishId]++;
            }
            else
            {
                if (this.stalemateCount[fishId] > 0) this.stalemateCount[fishId]--;
            }
            // 连续累计达 15 帧（容忍偶发不满足）才算真相持
            return this.stalemateCount[fishId] >= 15;
        }

        /// <summary>
        /// 相持破解（问题2修复）：
        /// 高速冲向"离球最近的对方鱼"（挡路的鱼），用物理碰撞撞开僵持局面。
        /// 若没有对方鱼贴着球，则撞离我方最近的对方鱼兜底。
        /// </summary>
        public void RamOpponent(int fishId, int ballId)
        {
            double bx = 0.0;
            double bz = 0.0;
            if (ballId >= 0 && ballId <= 8)
            {
                bx = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.X;
                bz = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.Z;
            }
            int target = -1;
            double best = 99999.0;
            double mx = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
            double mz = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;
            for (int j = 0; j < 2; j++)
            {
                double ox = GlobalVar.OppTeam.Fishes[j].PositionMm.X;
                double oz = GlobalVar.OppTeam.Fishes[j].PositionMm.Z;
                double d;
                if (ballId >= 0 && ballId <= 8)
                {
                    // 优先按"对方鱼到球的距离"选择目标（撞挡在球前的鱼）
                    d = Compute_Distance(bx, bz, ox, oz);
                    if (d < best)
                    {
                        best = d;
                        target = j;
                    }
                }
                else
                {
                    // 兜底：按到我方鱼的距离选择
                    d = Compute_Distance(mx, mz, ox, oz);
                    if (d < best)
                    {
                        best = d;
                        target = j;
                    }
                }
            }
            if (target == -1)
            {
                return;
            }
            double tx = GlobalVar.OppTeam.Fishes[target].PositionMm.X;
            double tz = GlobalVar.OppTeam.Fishes[target].PositionMm.Z;
            if (tx > 1950.0) tx = 1950.0;
            if (tx < -1950.0) tx = -1950.0;
            if (tz > 1400.0) tz = 1400.0;
            if (tz < -1400.0) tz = -1400.0;
            Position(fishId, tx, tz, 300.0);   // 高速冲撞
        }

        /// <summary>
        /// 带球回家入口（问题2修复包装）：
        /// 检测到与对方鱼相持（头顶头推不动球）时，先高速撞开对方鱼，否则执行原有 Dribblehometest1。
        /// 撞开后对方鱼远离球，IsInStalemate 自动失效，下一帧自然恢复正常带球。
        /// </summary>
        public void Dribblehometest1Smart(int whichfish, int whichball)
        {
            if (whichball < 0 || whichball > 8)
            {
                Dribblehometest1(whichfish, whichball);
                return;
            }
            // 问题1修复：球已在门口 → 精确冲门（把球推进球门框窗口）
            if (IsBallAtGoalMouth(whichball))
            {
                ShootBallIntoGoal(whichfish, whichball);
                return;
            }
            // 相持 → 撞开
            if (IsInStalemate(whichfish, whichball))
            {
                this.ramCount[whichfish]++;
                if (this.ramCount[whichfish] <= 20)   // 撞开动作最多持续2秒
                {
                    RamOpponent(whichfish, whichball);
                    return;
                }
                // 撞不开则回退正常带球，避免一直追对方鱼
                this.ramCount[whichfish] = 0;
                this.stalemateCount[whichfish] = 0;
            }
            else
            {
                this.ramCount[whichfish] = 0;
            }
            // 问题2/3修复：对方贴身争球或球在墙边 → 侧身横扫（身体做墙，把球扫向门，顺便扫走墙边其他球）
            if (ShouldShieldCarry(whichfish, whichball))
            {
                SweepShieldToGoal(whichfish, whichball);
                return;
            }
            Dribblehometest1(whichfish, whichball);
        }

        /// <summary>
        /// 问题1修复：计算目标门（左门）进球窗口中心 X。
        /// 进球判定：球必须完整落在球门框 X 区间内，窗口 = [LeftMm + 6r, LeftMm + 9r]，中心 = LeftMm + 7.5r。
        /// </summary>
        public double GetGoalCenterX(int ballId)
        {
            double leftMm = GlobalVar.mission.EnvRef.FieldInfo.LeftMm;
            double r = GlobalVar.mission.EnvRef.Balls[ballId].RadiusMm;
            return leftMm + (7.5 * r);
        }

        /// <summary>
        /// 问题1修复：判断球是否已到门口区域（可触发精确冲门）。
        /// 原策略把球推到 -1500/-1230/-1180 全部越过进球窗口 [-1152,-978]，导致球永远进不了门。
        /// </summary>
        public bool IsBallAtGoalMouth(int ballId)
        {
            double bx = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.X;
            double bz = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.Z;
            double goalCenterX = GetGoalCenterX(ballId);
            // 球位于窗口上沿向门方向 500mm 内（无论球是刚靠近还是已推过头）
            return (bx < goalCenterX + 500.0) && (bx > goalCenterX - 800.0) && (Math.Abs(bz) < 700.0);
        }

        /// <summary>
        /// 问题1修复：精确冲门。
        /// 用 Dribble 把球精确推向进球窗口中心（LeftMm + 7.5r）。
        /// 若球已推过头（越过窗口），Dribble 会把球带回窗口，途经窗口时即触发得分判定。
        /// </summary>
        public void ShootBallIntoGoal(int fishId, int ballId)
        {
            double goalCenterX = GetGoalCenterX(ballId);
            double goalCenterZ = 0.0;
            this.Dribble(fishId, ballId, goalCenterX, goalCenterZ);
        }

        /// <summary>
        /// 问题2/3修复：判断是否需要护球推进（身体隔离）。
        /// 条件：存在对方鱼距球 <650mm（贴身争球），或球贴近墙边（|x|>1400 或 |z|>1350）。
        /// </summary>
        public bool ShouldShieldCarry(int fishId, int ballId)
        {
            double bx = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.X;
            double bz = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.Z;
            double fx = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
            double fz = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;
            // 我方鱼已控球（距球<200mm）→ 不横扫，直接带球（横扫会绕位丢球）
            if (Compute_Distance(fx, fz, bx, bz) < 200.0)
            {
                return false;
            }
            // 我方鱼未控球 + 对方鱼贴身争球（<650mm）→ 横扫隔离抢球
            for (int j = 0; j < 2; j++)
            {
                double ox = GlobalVar.OppTeam.Fishes[j].PositionMm.X;
                double oz = GlobalVar.OppTeam.Fishes[j].PositionMm.Z;
                if (Compute_Distance(bx, bz, ox, oz) < 650.0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 问题2/3修复：侧身横扫护球（用户要求的"身体做墙横扫"）。
        /// 与"头顶着球走"不同，本动作让鱼游到球的门反侧，鱼头朝门方向（-X），
        /// 鱼身长边（422mm）像一堵墙贴着球侧面，向前推进时把球持续扫向门；
        /// 鱼身同时充当墙隔离对方鱼（对方抢球只能撞在鱼身上）。
        /// 墙边场景沿墙推进，可顺手把靠墙的其他球一起扫向门。
        /// 每帧重算站位，球被扫动后鱼自动跟进，形成持续推进。
        /// 几何关键：球贴鱼身侧面，球中心 Z 偏移 = 鱼半宽(22) + 球半径(58) - 接触重叠(8) = 72mm
        /// </summary>
        public void SweepShieldToGoal(int fishId, int ballId)
        {
            double bx = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.X;
            double bz = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.Z;
            double fx = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
            double fz = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;
            double r = GlobalVar.mission.EnvRef.Balls[ballId].RadiusMm;
            // 找最近的对方鱼（用于扫向方向选择）
            int opp = -1;
            double best = 99999.0;
            for (int j = 0; j < 2; j++)
            {
                double oox = GlobalVar.OppTeam.Fishes[j].PositionMm.X;
                double ooz = GlobalVar.OppTeam.Fishes[j].PositionMm.Z;
                double d = Compute_Distance(fx, fz, oox, ooz);
                if (d < best)
                {
                    best = d;
                    opp = j;
                }
            }
            double oz = (opp != -1) ? GlobalVar.OppTeam.Fishes[opp].PositionMm.Z : 0.0;
            bool oppNear = (opp != -1) && (best < 700.0);

            // 选择球贴鱼身哪一侧（+Z 或 -Z）：
            double zSign;
            if (Math.Abs(bz) > 1200.0)
            {
                zSign = (bz > 0) ? -1.0 : 1.0;      // 上下墙边：朝场地中央（顺手扫走墙边其他球）
            }
            else if (oppNear)
            {
                zSign = (oz > bz) ? -1.0 : 1.0;     // 对方在球上方则球贴下侧（鱼身隔开对方）
            }
            else
            {
                zSign = (bz >= 0) ? -1.0 : 1.0;     // 默认朝场地中央
            }

            // 站位：鱼中心在球门反侧，鱼头朝 -X，鱼身长边贴球侧面
            double halfW = 22.0;                      // 鱼身半宽（Fishbody_width = 44）
            // sideX 就近选择：鱼在球的 +X 侧则站 +X，在 -X 侧则站 -X，避免绕远路
            double sideX = (fx >= bx) ? (bx + 100.0) : (bx - 100.0);
            double sideZ = bz + (zSign * (halfW + r - 8.0));   // 球正好贴鱼身侧面
            if (sideX > 1900.0) sideX = 1900.0;
            if (sideX < -1900.0) sideX = -1900.0;
            if (sideZ > 1400.0) sideZ = 1400.0;
            if (sideZ < -1400.0) sideZ = -1400.0;

            double distToSide = Compute_Distance(fx, fz, sideX, sideZ);
            if (distToSide > 100.0)
            {
                // 绕位超时检测：连续绕位超过 30 帧（3秒）还没就位 → 回退直接带球，防止围球转圈
                this.sweepOrbitCount[fishId]++;
                if (this.sweepOrbitCount[fishId] > 30)
                {
                    this.sweepOrbitCount[fishId] = 0;
                    this.Dribblehometest1(fishId, ballId);
                    return;
                }
                // 先绕到球侧面（贴球站位），用 Position + SafeApproachSpeed（不冲过）
                this.Position(fishId, sideX, sideZ, SafeApproachSpeed(fishId, sideX, sideZ));
            }
            else
            {
                this.sweepOrbitCount[fishId] = 0;   // 就位成功，重置计数
                // 已就位：推进方向取决于站位（鱼在球+X侧则推-X，在球-X侧则推+X），略偏向球侧保持贴球
                double fishAng = GlobalVar.MyTeam.Fishes[fishId].BodyDirectionRad;
                double pushDir = (fx >= bx) ? GlobalVar.PI : 0.0;   // +X 侧推 -X，-X 侧推 +X
                double targetAng = pushDir + (zSign * 0.15);   // 主方向 + 略偏向球侧（持续贴球）
                double diff = this.FormatAngleRad(targetAng - fishAng);
                if (Math.Abs(diff) > 0.4)
                {
                    GlobalVar.decisions[fishId].VCode = 8;
                    GlobalVar.decisions[fishId].TCode = diff > 0 ? 11 : 3;
                }
                else
                {
                    GlobalVar.decisions[fishId].VCode = 14;
                    GlobalVar.decisions[fishId].TCode = 7;
                }
            }
        }

        /// <summary>
        /// 优化E：锁定正在横扫/带球的球，防止扫球途中被 ChoseBall20 每帧重选切换去抢别的球。
        /// - 若已锁定球且球仍在己方控制范围（距鱼 <900mm）→ 继续返回锁定球
        /// - 若锁定球已进我方球门（得分重置）或丢失（球被带走/离太远）→ 解锁并接受新选球
        /// 注意：必须在 ChoseBall20 选球之后、带球动作之前调用。
        /// </summary>
        public int LockCarryingBall(int fishId, int newBallId)
        {
            int otherFish = 1 - fishId;
            int lockedBall = this.lockBallId[fishId];

            // 1) 当前锁定球是否继续有效
            if ((lockedBall >= 0) && (lockedBall <= 8))
            {
                double bx = GlobalVar.mission.EnvRef.Balls[lockedBall].PositionMm.X;
                double bz = GlobalVar.mission.EnvRef.Balls[lockedBall].PositionMm.Z;
                double fx = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
                double fz = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;
                // 球已进我方球门（得分重置）→ 解锁，接受新球
                if (IsBallScoredInMyGoal(lockedBall))
                {
                    this.lockBallId[fishId] = -1;
                }
                else if (Compute_Distance(fx, fz, bx, bz) < 900.0)
                {
                    // 互斥检查：另一条鱼也锁了同一个球 → 放弃锁定，避免双鱼抢球
                    if (this.lockBallId[otherFish] == lockedBall)
                    {
                        this.lockBallId[fishId] = -1;
                        // 让路后接受新选球（若新球不与另一条鱼冲突）
                        if ((newBallId >= 0) && (this.lockBallId[otherFish] != newBallId))
                        {
                            this.lockBallId[fishId] = newBallId;
                            return newBallId;
                        }
                        return -1;   // 让路，空闲（走防守/干扰分支）
                    }
                    // 正常：继续锁定原球，避免扫球途中换球
                    return lockedBall;
                }
                else
                {
                    // 球离太远（丢失控制）→ 解锁
                    this.lockBallId[fishId] = -1;
                }
            }

            // 2) 无有效锁：锁定新球（与另一条鱼互斥）
            double nbX = 0.0;
            double nbZ = 0.0;
            double nfX = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
            double nfZ = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;
            if (newBallId >= 0)
            {
                nbX = GlobalVar.mission.EnvRef.Balls[newBallId].PositionMm.X;
                nbZ = GlobalVar.mission.EnvRef.Balls[newBallId].PositionMm.Z;
            }
            if ((newBallId >= 0) && (this.lockBallId[otherFish] == newBallId))
            {
                // 两条鱼同时选中同一球：距球更远者让路（空闲），避免双鱼抢球
                double ofX = GlobalVar.MyTeam.Fishes[otherFish].PositionMm.X;
                double ofZ = GlobalVar.MyTeam.Fishes[otherFish].PositionMm.Z;
                double dMe = Compute_Distance(nfX, nfZ, nbX, nbZ);
                double dOther = Compute_Distance(ofX, ofZ, nbX, nbZ);
                if (dMe > dOther)
                {
                    this.lockBallId[fishId] = -1;
                    return -1;
                }
            }
            // 无锁或已解锁：锁定新选球
            this.lockBallId[fishId] = newBallId;
            return newBallId;
        }

        /// <summary>
        /// α-β 滤波器更新球的平滑状态（问题3修复，抗测量噪声）
        /// 恒定速度模型：预测 p' = p + v*dt；更新：p = p' + α*残差，v = v + (β/dt)*残差
        /// </summary>
        public void UpdateBallFilter(int ballId)
        {
            double mx = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.X;
            double mz = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.Z;
            const double dt = 0.1;
            if (!this.filterBallInit[ballId])
            {
                this.filterBallPx[ballId] = mx;
                this.filterBallPz[ballId] = mz;
                this.filterBallVx[ballId] = 0.0;
                this.filterBallVz[ballId] = 0.0;
                this.filterBallInit[ballId] = true;
                return;
            }
            double predPx = this.filterBallPx[ballId] + (this.filterBallVx[ballId] * dt);
            double predPz = this.filterBallPz[ballId] + (this.filterBallVz[ballId] * dt);
            const double alpha = 0.6;
            const double beta = 0.2;
            double residX = mx - predPx;
            double residZ = mz - predPz;
            this.filterBallPx[ballId] = predPx + (alpha * residX);
            this.filterBallPz[ballId] = predPz + (alpha * residZ);
            this.filterBallVx[ballId] += (beta / dt) * residX;
            this.filterBallVz[ballId] += (beta / dt) * residZ;
        }

        /// <summary>
        /// 高级控球策略（用户要求）：
        /// - 我方控球+对方来争：加速把球控在头部到身体中部
        /// - 对方在远离门侧（+X侧）：把球与对方隔开（我方鱼在球与对方之间，向-X推球）
        /// - 对方在靠近门侧（-X侧）：身子中部顶对方头，产生角度横向移动运球（球往门方向走）
        /// 门在 -X 方向。
        /// </summary>
        public void ShieldCarryAdvanced(int fishId, int ballId)
        {
            RoboFish me = GlobalVar.MyTeam.Fishes[fishId];
            Ball ball = GlobalVar.mission.EnvRef.Balls[ballId];
            double fx = me.PositionMm.X;
            double fz = me.PositionMm.Z;
            double fishAng = me.BodyDirectionRad;
            double bx = ball.PositionMm.X;
            double bz = ball.PositionMm.Z;

            // 找争球的对方鱼（距球最近）
            int opp = 0;
            double bestDist = 99999.0;
            for (int j = 0; j < 2; j++)
            {
                double ox = GlobalVar.OppTeam.Fishes[j].PositionMm.X;
                double oz = GlobalVar.OppTeam.Fishes[j].PositionMm.Z;
                double d = Compute_Distance(bx, bz, ox, oz);
                if (d < bestDist) { bestDist = d; opp = j; }
            }
            double oppX = GlobalVar.OppTeam.Fishes[opp].PositionMm.X;
            double oppZ = GlobalVar.OppTeam.Fishes[opp].PositionMm.Z;

            double distToBall = Compute_Distance(fx, fz, bx, bz);

            // 门在 -X 方向
            // 对方在球的 +X 侧 = 远离门侧；对方在球的 -X 侧 = 靠近门侧
            bool oppFarFromGoal = (oppX > bx + 30.0);
            bool oppNearGoal = (oppX < bx - 30.0);

            if (oppFarFromGoal)
            {
                // 场景2：对方在远离门侧 → 把球与对方隔开
                // 我方鱼站位在球 +X 侧（对方与球之间），头朝 -X 推球，身体挡住对方
                double standX = bx + 80.0;
                double standZ = bz;
                if (standX > 1500.0) standX = 1500.0;   // 不冲向对方墙（原1900太接近对方球门墙2100）
                if (distToBall > 150.0)
                {
                    // 还没控到球：用 FastMoveTo 快速到位（Bang-Bang 最快）
                    FastMoveTo(fishId, standX, standZ);
                }
                else
                {
                    // 已控球：头朝 -X 全速推球，身体隔开对方
                    double targetAng = GlobalVar.PI;   // -X 方向
                    double angDiff = this.FormatAngleRad(targetAng - fishAng);
                    if (Math.Abs(angDiff) > 0.3)
                    {
                        GlobalVar.decisions[fishId].VCode = 10;
                        GlobalVar.decisions[fishId].TCode = (angDiff > 0) ? 11 : 3;
                    }
                    else
                    {
                        GlobalVar.decisions[fishId].VCode = 14;   // 高速推球（加速运球）
                        GlobalVar.decisions[fishId].TCode = 7;
                    }
                }
            }
            else if (oppNearGoal)
            {
                // 场景3：对方在靠近门侧 → 身子中部顶对方头，产生角度横向移动运球
                // 我方鱼站位在球 +X 侧，Z 对齐对方鱼头（身子中部顶对方头）
                double standX = bx + 80.0;
                double standZ = oppZ;   // Z 对齐对方（身子中部顶对方头）
                if (standX > 1500.0) standX = 1500.0;   // 不冲向对方墙（原1900太接近对方球门墙2100）
                if (standZ > 1400.0) standZ = 1400.0;
                if (standZ < -1400.0) standZ = -1400.0;

                if (distToBall > 150.0)
                {
                    FastMoveTo(fishId, standX, standZ);
                }
                else
                {
                    // 已控球：产生角度，横向移动运球（球往门方向走）
                    // 鱼头朝 -X + 斜向（远离对方方向），横向移动把球扫向门
                    double zSign = (oppZ >= fz) ? -1.0 : 1.0;   // 远离对方方向
                    double targetAng = GlobalVar.PI + (zSign * 0.4);   // -X + 23° 斜向
                    double angDiff = this.FormatAngleRad(targetAng - fishAng);
                    if (Math.Abs(angDiff) > 0.3)
                    {
                        GlobalVar.decisions[fishId].VCode = 10;
                        GlobalVar.decisions[fishId].TCode = (angDiff > 0) ? 11 : 3;
                    }
                    else
                    {
                        GlobalVar.decisions[fishId].VCode = 14;   // 高速（加速运球）
                        GlobalVar.decisions[fishId].TCode = 7;
                    }
                }
            }
            else
            {
                // 对方正对球（同 X）→ 默认向 -X 全速推球
                double targetAng = GlobalVar.PI;
                double angDiff = this.FormatAngleRad(targetAng - fishAng);
                if (Math.Abs(angDiff) > 0.3)
                {
                    GlobalVar.decisions[fishId].VCode = 10;
                    GlobalVar.decisions[fishId].TCode = (angDiff > 0) ? 11 : 3;
                }
                else
                {
                    GlobalVar.decisions[fishId].VCode = 15;   // 全速
                    GlobalVar.decisions[fishId].TCode = 7;
                }
            }
        }

        /// <summary>
        /// 智能劫球（问题3修复）：
        /// 1) 用滤波后的球状态预测拦截点（含对方鱼姿态与球走向）
        /// 2) 离拦截点远时用 Position 奔向拦截点
        /// 3) 接近拦截点时切换到垂直斜切姿态（鱼头朝向垂直于球运动方向），横切截球
        /// </summary>
        public void InterceptBallSmart(int fishId, int ballId)
        {
            this.UpdateBallFilter(ballId);
            double px = this.filterBallPx[ballId];
            double pz = this.filterBallPz[ballId];
            double vx = this.filterBallVx[ballId];
            double vz = this.filterBallVz[ballId];
            double speed = Math.Sqrt((vx * vx) + (vz * vz));

            double fx = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
            double fz = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;

            // 鱼的最大可达速度（vTable 最高档约 314mm/s）
            double U = 320.0;
            // 用解析式求相遇时间：需要以本鱼坐标为参考，临时重算
            double dx0 = px - fx;
            double dz0 = pz - fz;
            double a = (vx * vx) + (vz * vz) - (U * U);
            double b = 2.0 * ((vx * dx0) + (vz * dz0));
            double c = (dx0 * dx0) + (dz0 * dz0);
            double t = -1.0;
            if (Math.Abs(a) > 1E-06)
            {
                double disc = (b * b) - (4.0 * a * c);
                if (disc >= 0.0)
                {
                    double t1 = (-b - Math.Sqrt(disc)) / (2.0 * a);
                    double t2 = (-b + Math.Sqrt(disc)) / (2.0 * a);
                    if (t1 > 0.0) t = t1;
                    else if (t2 > 0.0) t = t2;
                }
            }
            else if (Math.Abs(b) > 1E-06)
            {
                t = -c / b;
            }
            if (t < 0.0)
            {
                t = Math.Sqrt(c) / U;
            }
            if (t > 1.0) t = 1.0;
            if (t < 0.1) t = 0.1;

            // 拦截点 = 球在 t 时刻的预测位置
            double ipx = px + (vx * t);
            double ipz = pz + (vz * t);
            // 拦截点 clamp 到场地安全区
            if (ipx > 1950.0) ipx = 1950.0;
            if (ipx < -1950.0) ipx = -1950.0;
            if (ipz > 1400.0) ipz = 1400.0;
            if (ipz < -1400.0) ipz = -1400.0;

            double distToIp = Compute_Distance(fx, fz, ipx, ipz);

            if (distToIp > 200.0)
            {
                // 冲向拦截点：用 Position（内部边运动边转向）+ SafeApproachSpeed（不冲过）
                this.Position(fishId, ipx, ipz, SafeApproachSpeed(fishId, ipx, ipz));
            }
            else
            {
                // 接近拦截点（问题2修复）：绕到球的门反侧（球正X侧=推球站位），把球顶向门方向，身体隔开对方鱼
                double sideX = ipx + 200.0;   // 球的预测位置正X侧（门反侧，我方推球站位）
                double sideZ = ipz;
                if (sideX > 1900.0) sideX = 1900.0;
                if (sideZ > 1400.0) sideZ = 1400.0;
                if (sideZ < -1400.0) sideZ = -1400.0;
                double distToSide = Compute_Distance(fx, fz, sideX, sideZ);
                if (distToSide > 120.0)
                {
                    // 还没到推球站位：用 Position + SafeApproachSpeed（不冲过）
                    this.Position(fishId, sideX, sideZ, SafeApproachSpeed(fishId, sideX, sideZ));
                }
                else
                {
                    // 已就位（劫球成功）：头朝门方向（-X）推进——鱼身在球门反侧像一堵墙贴着球，
                    // 把球持续扫向门，同时身体隔开对方鱼
                    // 已就位（劫球成功）：斜向引球往门中央方向（夹角度，非纯-X直推）
                    // 角度根据球当前 Z 选择：球在上半场则朝左下斜推，球在下半场则朝左上斜推 → 收敛到门中央 Z=0
                    double fishAng = GlobalVar.MyTeam.Fishes[fishId].BodyDirectionRad;
                    double bzNow = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.Z;
                    double zSign = (bzNow >= 0) ? -1.0 : 1.0;   // 引球往门中央（Z→0）
                    double leadAng = GlobalVar.PI + (zSign * 0.35);   // -X 主方向 + 斜向 0.35rad(20°) 引球
                    double diff = this.FormatAngleRad(leadAng - fishAng);
                    if (Math.Abs(diff) > 0.4)
                    {
                        GlobalVar.decisions[fishId].VCode = 8;
                        GlobalVar.decisions[fishId].TCode = diff > 0 ? 11 : 3;
                    }
                    else
                    {
                        GlobalVar.decisions[fishId].VCode = 14;
                        GlobalVar.decisions[fishId].TCode = 7;
                    }
                }
            }
        }

        // ==================================================================
        // V2 升级方法：理论最优位置+角度到达 / 改进控球 / 改进劫球
        // 不修改现有 FastMoveTo / ShieldCarryAdvanced / InterceptBallSmart，
        // 仅作为升级版供 Strategy.cs 调用切换
        // ==================================================================

        /// <summary>
        /// 理论最优 Bang-Bang 控制：到达指定位置 AND 指定角度的最快方案（FastMoveTo 升级版）。
        /// 核心改进 vs FastMoveTo：
        ///   1. 永远 VCode=15（除非要制动），不管角度偏差——因为角速度与线速度物理独立
        ///   2. 永远用最大转向 TCode=0/14（当角度误差>0.05rad），不浪费角速度档位
        ///   3. 三阶段：边走边转(d>dBrake) → 制动(d≤dBrake, vCode=vSafe) → 角度精调(d<30)
        ///   4. 大角度反向保护：absAng>90° 时鱼会朝错误方向冲，用 VCode=8 低速保持运动
        /// 物理原理：
        ///   - 制动距离 d_brake = v²/(2·a)，a=500mm/s²
        ///   - 角速度独立于线速度，可边走边转
        ///   - 总时间 T = max(T_position, T_angle)，位置和角度并行控制
        /// </summary>
        /// <param name="fishId">鱼编号</param>
        /// <param name="tx">目标位置 X</param>
        /// <param name="tz">目标位置 Z</param>
        /// <param name="targetAng">到达后要朝向的目标角度（弧度）</param>
        public void FastMoveToPose(int fishId, double tx, double tz, double targetAng)
        {
            double fx = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
            double fz = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;
            double fishAng = GlobalVar.MyTeam.Fishes[fishId].BodyDirectionRad;

            double d = Compute_Distance(fx, fz, tx, tz);
            double angToTarget = Math.Atan2(tz - fz, tx - fx);
            double angErrMove = this.FormatAngleRad(angToTarget - fishAng);   // 运动方向误差
            double angErrFinal = this.FormatAngleRad(targetAng - fishAng);    // 最终角度误差
            double absMove = Math.Abs(angErrMove);

            // === 转向决策（所有阶段通用，边走边转）===
            double angErrForTurn;
            if (d < 80.0)
                angErrForTurn = angErrFinal;   // 接近目标：对齐最终角度
            else
                angErrForTurn = angErrMove;    // 远距离：对齐运动方向
            int tCode;
            if (Math.Abs(angErrForTurn) > 0.05)
                tCode = (angErrForTurn > 0) ? 0 : 14;   // 最大转向
            else
                tCode = 7;

            // === 速度决策 ===
            // 致命bug修复：仿真器速度模型是 0.9 衰减（v_next=0.9*v0+0.1*v_target），
            // 实际制动距离 ≈ v0*0.95（v0=314时需要298mm），不是 v0²/(2·500)=98.6mm
            // → 近距离改用 Position 方法（它用正确的 0.9 衰减模型预测制动距离，保证不冲过）
            int vCode;
            if (absMove > 2.0)
            {
                // >115°：几乎完全反向 → 停止纯转向（避免朝错误方向冲）
                vCode = 0;
            }
            else if (d > 350.0)
            {
                // 远距离：全速冲刺（起步快）
                vCode = 15;
            }
            else
            {
                // 近距离：用 Position 的减速逻辑（保证不冲过）
                // Position 内部用 0.9 衰减模型预测制动距离，选合适 VCode
                // dv=80 表示到达时保留 80mm/s 速度（避免完全停止后无法推球）
                this.Position(fishId, tx, tz, 80.0);
                // Position 设置了 VCode 和 TCode，覆盖 TCode 为我们的边走边转
                GlobalVar.decisions[fishId].TCode = tCode;
                return;
            }

            GlobalVar.decisions[fishId].VCode = vCode;
            GlobalVar.decisions[fishId].TCode = tCode;
        }

        /// <summary>
        /// 改进控球策略（ShieldCarryAdvanced 升级版）：
        ///   1. 自己控球+对方争球 → 加速（VCode=15），球控在头部到身体中部
        ///   2. 对方在远离我方球门侧（+X）→ 把球与对方隔开（站位球+X侧，身体挡对方）
        ///   3. 对方在靠近我方球门侧（-X）→ 身子中部顶对方头，产生角度横向移动运球
        /// 我方球门在 -X 方向（Goal_line=-2100）。鱼头朝 -X 推球。
        /// 关键改进：用 FastMoveToPose 到位（带目标角度），推球时 VCode=15 加速控球
        /// </summary>
        public void ShieldCarryV2(int fishId, int ballId)
        {
            RoboFish me = GlobalVar.MyTeam.Fishes[fishId];
            Ball ball = GlobalVar.mission.EnvRef.Balls[ballId];
            double fx = me.PositionMm.X;
            double fz = me.PositionMm.Z;
            double fishAng = me.BodyDirectionRad;
            double bx = ball.PositionMm.X;
            double bz = ball.PositionMm.Z;

            // 找争球的对方鱼（距球最近）
            int opp = 0;
            double bestDist = 99999.0;
            for (int j = 0; j < 2; j++)
            {
                double ox = GlobalVar.OppTeam.Fishes[j].PositionMm.X;
                double oz = GlobalVar.OppTeam.Fishes[j].PositionMm.Z;
                double d = Compute_Distance(bx, bz, ox, oz);
                if (d < bestDist) { bestDist = d; opp = j; }
            }
            double oppX = GlobalVar.OppTeam.Fishes[opp].PositionMm.X;
            double oppZ = GlobalVar.OppTeam.Fishes[opp].PositionMm.Z;

            double distToBall = Compute_Distance(fx, fz, bx, bz);

            // 我方球门在 -X。对方在球的 +X 侧 = 远离我方门侧；-X 侧 = 靠近我方门侧
            bool oppFarFromGoal = (oppX > bx + 30.0);
            bool oppNearGoal = (oppX < bx - 30.0);

            // 球控位置：鱼头到身体中部
            //   鱼头距中心 ~135mm（Fish_centertohead + Fishhead_Radius）
            //   理想：球在鱼头前方 30~80mm → 站位 X = bx + 100（鱼头朝-X，球在鱼头-X侧前方）
            double standX, standZ;

            if (oppFarFromGoal)
            {
                // 场景2：对方在远离门侧（+X）→ 把球与对方隔开
                // 我方鱼站位：球 +X 侧（对方与球之间），头朝 -X 推球，身体挡住对方
                standX = bx + 100.0;
                standZ = bz;
                if (standX > 1500.0) standX = 1500.0;   // 不冲向对方墙（原1900太接近对方球门墙2100）

                if (distToBall > 150.0)
                {
                    // 还没控到球：FastMoveToPose 到位，头朝 -X（推球方向）
                    FastMoveToPose(fishId, standX, standZ, GlobalVar.PI);
                }
                else
                {
                    // 已控球：加速推球，身体隔开对方
                    double targetAng = GlobalVar.PI;   // 头朝 -X
                    double angDiff = this.FormatAngleRad(targetAng - fishAng);
                    if (Math.Abs(angDiff) > 0.2)
                    {
                        // 边转边走：最大转向 + 中速
                        GlobalVar.decisions[fishId].VCode = 12;
                        GlobalVar.decisions[fishId].TCode = (angDiff > 0) ? 0 : 14;
                    }
                    else
                    {
                        // 对齐：全速推球（加速控球！）
                        GlobalVar.decisions[fishId].VCode = 15;
                        GlobalVar.decisions[fishId].TCode = 7;
                    }
                }
            }
            else if (oppNearGoal)
            {
                // 场景3：对方在靠近门侧（-X）→ 身子中部顶对方头，产生角度横向移动运球
                // 站位：球 +X 侧，Z 对齐对方鱼头（身子中部顶对方头）
                standX = bx + 100.0;
                standZ = oppZ;   // Z 对齐对方（身子中部顶对方头）
                if (standX > 1500.0) standX = 1500.0;   // 不冲向对方墙（原1900太接近对方球门墙2100）
                if (standZ > 1400.0) standZ = 1400.0;
                if (standZ < -1400.0) standZ = -1400.0;

                if (distToBall > 150.0)
                {
                    FastMoveToPose(fishId, standX, standZ, GlobalVar.PI);
                }
                else
                {
                    // 已控球：产生角度，横向移动运球（球往门方向走）
                    // 鱼头朝 -X + 斜向（远离对方方向），横向移动把球扫向门
                    double zSign = (oppZ >= fz) ? -1.0 : 1.0;   // 远离对方方向
                    double targetAng = GlobalVar.PI + (zSign * 0.5);   // -X + 28° 斜向
                    double angDiff = this.FormatAngleRad(targetAng - fishAng);
                    if (Math.Abs(angDiff) > 0.2)
                    {
                        GlobalVar.decisions[fishId].VCode = 12;
                        GlobalVar.decisions[fishId].TCode = (angDiff > 0) ? 0 : 14;
                    }
                    else
                    {
                        // 对齐：全速运球（加速！）
                        GlobalVar.decisions[fishId].VCode = 15;
                        GlobalVar.decisions[fishId].TCode = 7;
                    }
                }
            }
            else
            {
                // 对方正对球（同 X）→ 全速推球
                double targetAng = GlobalVar.PI;
                double angDiff = this.FormatAngleRad(targetAng - fishAng);
                if (Math.Abs(angDiff) > 0.2)
                {
                    GlobalVar.decisions[fishId].VCode = 12;
                    GlobalVar.decisions[fishId].TCode = (angDiff > 0) ? 0 : 14;
                }
                else
                {
                    GlobalVar.decisions[fishId].VCode = 15;
                    GlobalVar.decisions[fishId].TCode = 7;
                }
            }
        }

        /// <summary>
        /// 改进劫球策略（InterceptBallSmart 升级版）：
        ///   1. 在对方球门侧（球的 +X 侧）站位
        ///   2. 身体前半部分横向顶住对方鱼头
        ///   3. 产生角度，让球向我方球门（-X）运动
        /// 关键改进：用 FastMoveToPose 替代 Position+SafeApproachSpeed，劫球速度更快
        /// </summary>
        public void InterceptBallV2(int fishId, int ballId)
        {
            this.UpdateBallFilter(ballId);
            double px = this.filterBallPx[ballId];
            double pz = this.filterBallPz[ballId];
            double vx = this.filterBallVx[ballId];
            double vz = this.filterBallVz[ballId];

            double fx = GlobalVar.MyTeam.Fishes[fishId].PositionMm.X;
            double fz = GlobalVar.MyTeam.Fishes[fishId].PositionMm.Z;

            // 用解析式求相遇时间（鱼最大可达速度 U）
            double U = 320.0;
            double dx0 = px - fx;
            double dz0 = pz - fz;
            double a = (vx * vx) + (vz * vz) - (U * U);
            double b = 2.0 * ((vx * dx0) + (vz * dz0));
            double c = (dx0 * dx0) + (dz0 * dz0);
            double t = -1.0;
            if (Math.Abs(a) > 1E-06)
            {
                double disc = (b * b) - (4.0 * a * c);
                if (disc >= 0.0)
                {
                    double t1 = (-b - Math.Sqrt(disc)) / (2.0 * a);
                    double t2 = (-b + Math.Sqrt(disc)) / (2.0 * a);
                    if (t1 > 0.0) t = t1;
                    else if (t2 > 0.0) t = t2;
                }
            }
            else if (Math.Abs(b) > 1E-06)
            {
                t = -c / b;
            }
            if (t < 0.0) t = Math.Sqrt(c) / U;
            if (t > 1.0) t = 1.0;
            if (t < 0.1) t = 0.1;

            // 拦截点 = 球在 t 时刻的预测位置
            double ipx = px + (vx * t);
            double ipz = pz + (vz * t);
            if (ipx > 1950.0) ipx = 1950.0;
            if (ipx < -1950.0) ipx = -1950.0;
            if (ipz > 1400.0) ipz = 1400.0;
            if (ipz < -1400.0) ipz = -1400.0;

            // 找距球最近的对方鱼，同时判断对方是否真的在带球（距球<300mm 才算带球）
            int opp = 0;
            double bestDist = 99999.0;
            for (int j = 0; j < 2; j++)
            {
                double ox = GlobalVar.OppTeam.Fishes[j].PositionMm.X;
                double oz = GlobalVar.OppTeam.Fishes[j].PositionMm.Z;
                double d = Compute_Distance(ipx, ipz, ox, oz);
                if (d < bestDist) { bestDist = d; opp = j; }
            }
            bool oppDribbling = (bestDist < 300.0);   // 对方鱼距球<300mm 才算在带球

            if (!oppDribbling)
            {
                // 场景A：对方没带球 → 直接冲向球抢球
                // 致命bug修复：目标角度用 PI（-X，朝我方门），不是 angToBall
                // 原来用 angToBall 会导致鱼到达后头朝球方向（可能朝对方门），把球往对方门推→冲墙
                // 用 PI：到达后鱼头朝-X，准备把球往我方门推
                FastMoveToPose(fishId, ipx, ipz, GlobalVar.PI);
                return;
            }

            // 场景B：对方在带球 → 劫球：去对方球门侧（球的+X侧）站位，横向顶对方鱼头
            double oppZ = GlobalVar.OppTeam.Fishes[opp].PositionMm.Z;

            // 劫球站位：球的 +X 侧（对方球门侧），Z 对齐对方鱼头（身体前半部分横向顶对方鱼头）
            double standX = ipx + 150.0;
            double standZ = oppZ;
            // 关键修复：限制站位不冲向对方墙（对方球门在2100，墙也在那，standX 不超过1500）
            if (standX > 1500.0) standX = 1500.0;
            if (standZ > 1400.0) standZ = 1400.0;
            if (standZ < -1400.0) standZ = -1400.0;

            double distToStand = Compute_Distance(fx, fz, standX, standZ);

            if (distToStand > 150.0)
            {
                // 冲向站位：FastMoveToPose（带目标角度 PI，头朝 -X 推球方向）
                FastMoveToPose(fishId, standX, standZ, GlobalVar.PI);
            }
            else
            {
                // 已就位：横向顶住对方鱼头，产生角度引球往我方门
                // 鱼头朝 -X + 斜向（引球往门中央 Z=0）
                double fishAng = GlobalVar.MyTeam.Fishes[fishId].BodyDirectionRad;
                double bzNow = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.Z;
                double zSign = (bzNow >= 0) ? -1.0 : 1.0;   // 引球往门中央
                double leadAng = GlobalVar.PI + (zSign * 0.4);   // -X + 23° 斜向
                double diff = this.FormatAngleRad(leadAng - fishAng);
                if (Math.Abs(diff) > 0.2)
                {
                    GlobalVar.decisions[fishId].VCode = 12;
                    GlobalVar.decisions[fishId].TCode = (diff > 0) ? 0 : 14;
                }
                else
                {
                    // 对齐：全速引球
                    GlobalVar.decisions[fishId].VCode = 15;
                    GlobalVar.decisions[fishId].TCode = 7;
                }
            }
        }

        /// <summary>
        /// 判断是否应启用 ShieldCarryV2 护球控球（ShouldShieldCarry 的升级版判断）。
        /// 与 ShouldShieldCarry 的区别：
        ///   - ShouldShieldCarry：已控球时返回 false（为横扫设计，避免绕位丢球）
        ///   - ShouldShieldCarryV2：只要对方在争球（距球<650mm）就返回 true，
        ///     无论我方是否已控球——因为 ShieldCarryV2 在已控球时是加速推球+护球，不会绕位丢球
        /// 触发条件：对方任一鱼距球<650mm（贴身争球威胁）
        /// </summary>
        public bool ShouldShieldCarryV2(int fishId, int ballId)
        {
            double bx = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.X;
            double bz = GlobalVar.mission.EnvRef.Balls[ballId].PositionMm.Z;
            // 对方任一鱼距球<650mm → 对方在争球
            for (int j = 0; j < 2; j++)
            {
                double ox = GlobalVar.OppTeam.Fishes[j].PositionMm.X;
                double oz = GlobalVar.OppTeam.Fishes[j].PositionMm.Z;
                if (Compute_Distance(bx, bz, ox, oz) < 650.0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 包装分发：对方争球时用 ShieldCarryV2 护球控球，否则用 Dribblehometest1Smart 带球回家。
        /// 供 Strategy.cs 替换 Dribblehometest1Smart 调用，实现"对方争球时加速护球"策略。
        /// 不修改 Dribblehometest1Smart 方法本身，仅做调用分发。
        /// </summary>
        public void DribbleOrShieldV2(int fishId, int ballId)
        {
            if (ShouldShieldCarryV2(fishId, ballId))
            {
                ShieldCarryV2(fishId, ballId);
            }
            else
            {
                Dribblehometest1Smart(fishId, ballId);
            }
        }

    }

    public class Platform
    {
        public double Posx;
        public double Posz;

        public Platform()
        {
            this.Posx = 0.0;
            this.Posz = 0.0;
            this.Posx = 0.0;
            this.Posz = 0.0;
        }

        public Platform(double Index_x, double Index_z)
        {
            this.Posx = 0.0;
            this.Posz = 0.0;
            this.Posx = Index_x;
            this.Posz = Index_z;
        }
    }

    public class Fish
    {
        public int fishId = 0;
        public double Last_v = 0.0;
        public double Last_w = 0.0;
        public int LastChangeTCode = 7;
        public int LastChangeVCode = 0;
        public int LastStadyTCode = 7;
        public int LastStadyVCode = 0;
        public int LastTCode = 7;
        public int LastVCode = 0;
        public int next_rad = 0;
        public int next_top_x = 0;
        public int next_top_z = 0;
        public int next_x = 0;
        public int next_z = 0;
        public double p_Last_v = 0.0;
        public double p_Last_w = 0.0;
        public int p_LastStadyTCode = 7;
        public int p_LastStadyVCode = 0;
        public int p_LastTCode = 7;
        public int p_LastVCode = 0;
        public double p_now_v = 0.0;
        public double p_now_w = 0.0;
        public RoboFish rf = null;
    }

}

