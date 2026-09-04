namespace URWPGSim2D.Strategy
{
    using System;
    using Microsoft.Xna.Framework;
    using URWPGSim2D.Common;
    using URWPGSim2D.StrategyLoader;

    /// <summary>
    /// 花样游泳（排位赛）策略
    /// 比赛规则要点：
    ///   1. 1.5倍标准场地 4500x3000mm，1 支队伍 10 条仿真机器鱼，无球无障碍物
    ///   2. 1号鱼随机游动不受策略控制；2~10号鱼由策略编排（本策略控制 Fishes[0..9] 全部 10 条）
    ///   3. 标准动作阶段（前3分钟）：罗马数字造型 -> 静止5s -> 汉字造型 -> 静止5s -> 封闭图形造型 -> 静止5s
    ///   4. 自由动作阶段（后2分钟）：自行设计动作
    ///   5. 主题：不负韶华
    /// 时间轴（1 周期 = 100ms，总 3000 周期 = 300s = 5 分钟）：
    ///   [0,100)      集合：3x3 方阵
    ///   [100,260)    标准动作1：罗马数字 V
    ///   [260,320)    静止 6s（规则要求造型间 5s 绝对静止）
    ///   [320,480)    标准动作2：汉字 "中"
    ///   [480,540)    静止 6s
    ///   [540,700)    标准动作3：封闭图形（圆）
    ///   [700,760)    静止 6s
    ///   [760,1030)   自由动作1：圆形顺时针旋转
    ///   [1030,1330)  自由动作2：半径呼吸（缩放）
    ///   [1330,1630)  自由动作3：圆形逆时针旋转 + 反向呼吸
    ///   [1630,1930)  自由动作4：V 形与圆形交替（波浪）
    ///   [1930,3000)  收尾：圆形慢速旋转
    /// </summary>
    public class Strategy : MarshalByRefObject, IStrategy
    {
        private Decision[] decisions = null;
        private int cycleCount = 0;          // 已运行周期数（1 周期 = 100ms）
        private double rotateAngle = 0.0;    // 自由动作旋转角
        private Random rnd = new Random();

        // 造型目标点定义（下标 0~8 对应 Fishes[1]~Fishes[9]）
        // 集合：3x3 方阵，间距 600mm（相邻点距 600mm，大于鱼长 450mm，避免碰撞）
        private static readonly double[,] FORM_GRID = new double[,]
        {
            { -600, -600 }, { -600, 0 }, { -600, 600 },
            {    0, -600 }, {    0, 0 }, {    0, 600 },
            {  600, -600 }, {  600, 0 }, {  600, 600 }
        };

        // 罗马数字 "V"（臂上相邻点距约 481mm）
        private static readonly double[,] FORM_V = new double[,]
        {
            { -900,  900 }, { -675,  475 }, { -450,   50 }, { -225, -375 },
            {    0, -800 },
            {  900,  900 }, {  675,  475 }, {  450,   50 }, {  225, -375 }
        };

        // 汉字 "中"（"口"四角 + 竖直贯穿 + 右横端，最小点距约 520mm）
        private static readonly double[,] FORM_ZHONG = new double[,]
        {
            { -600, -600 }, { -600,  600 }, {  600, -600 }, {  600,  600 },
            { -120, -1200 }, { -120, -400 }, { -120,  400 }, { -120, 1200 },
            {  600,    0 }
        };

        /// <summary>
        /// 使远程对象永久存活（模板要求，不可删改）
        /// </summary>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        /// <summary>
        /// 获取队伍名称
        /// </summary>
        public string GetTeamName()
        {
            return "不负韶华-NEU";
        }

        /// <summary>
        /// 获取当前队伍所有仿真机器鱼的决策数据
        /// </summary>
        public Decision[] GetDecision(Mission mission, int teamId)
        {
            if (this.decisions == null)
            {
                this.decisions = new Decision[mission.CommonPara.FishCntPerTeam];
            }
            this.cycleCount++;

            // 1号鱼：随机游动（规则规定其不受策略控制，此处给予在场地内漫游的行为）
            SetFish0Wander(mission);

            // 2~10号鱼：执行编排
            ExecuteChoreography(mission);

            return this.decisions;
        }

        /// <summary>
        /// 1号鱼随机游动：始终朝场地中心方向游动并加入随机扰动，接近边界时自然转向场内
        /// </summary>
        private void SetFish0Wander(Mission mission)
        {
            RoboFish fish = mission.TeamsRef[0].Fishes[0];
            double x = fish.PositionMm.X;
            double z = fish.PositionMm.Z;
            double ang = fish.BodyDirectionRad;
            double toCenter = Math.Atan2(-z, -x);
            double diff = NormalizeAngle(toCenter - ang);
            diff += (this.rnd.NextDouble() - 0.5) * 0.9;   // 随机扰动，避免直线往返
            if (Math.Abs(diff) > 0.5)
            {
                this.decisions[0].VCode = 5;
                this.decisions[0].TCode = diff > 0 ? 11 : 3;
            }
            else
            {
                this.decisions[0].VCode = 7;
                this.decisions[0].TCode = 7;
            }
        }

        /// <summary>
        /// 2~10号鱼队形编排：按时间轴执行标准动作与自由动作
        /// </summary>
        private void ExecuteChoreography(Mission mission)
        {
            int cycle = this.cycleCount;
            double[,] targets = new double[9, 2];
            bool holdStill = false;

            if (cycle < 100)
            {
                CopyTargets(FORM_GRID, targets);
            }
            else if (cycle < 260)
            {
                CopyTargets(FORM_V, targets);            // 标准动作1：罗马数字 V
            }
            else if (cycle < 320)
            {
                holdStill = true;                        // 静止 6s（规则要求 5s 绝对静止）
            }
            else if (cycle < 480)
            {
                CopyTargets(FORM_ZHONG, targets);        // 标准动作2：汉字 "中"
            }
            else if (cycle < 540)
            {
                holdStill = true;                        // 静止 6s
            }
            else if (cycle < 700)
            {
                GetCirclePoints(900.0, 0.0, targets);    // 标准动作3：封闭图形（圆）
            }
            else if (cycle < 760)
            {
                holdStill = true;                        // 静止 6s
            }
            else if (cycle < 1030)
            {
                this.rotateAngle += 0.030;               // 自由动作1：圆形顺时针旋转
                GetCirclePoints(900.0, this.rotateAngle, targets);
            }
            else if (cycle < 1330)
            {
                this.rotateAngle += 0.015;               // 自由动作2：半径呼吸（缩放）
                double t = (cycle - 1030) / 300.0;
                double r = 600.0 + 300.0 * Math.Sin(2 * Math.PI * t);
                GetCirclePoints(r, this.rotateAngle, targets);
            }
            else if (cycle < 1630)
            {
                this.rotateAngle -= 0.030;               // 自由动作3：圆形逆时针旋转 + 反向呼吸
                double t = (cycle - 1330) / 300.0;
                double r = 600.0 + 300.0 * Math.Sin(2 * Math.PI * t);
                GetCirclePoints(r, this.rotateAngle, targets);
            }
            else if (cycle < 1930)
            {
                int seg = (cycle - 1630) / 100;          // 自由动作4：V 形与圆形交替（每 10s 切换）
                if (seg % 2 == 0)
                {
                    CopyTargets(FORM_V, targets);
                }
                else
                {
                    this.rotateAngle += 0.010;
                    GetCirclePoints(900.0, this.rotateAngle, targets);
                }
            }
            else
            {
                this.rotateAngle += 0.008;               // 收尾：圆形慢速旋转
                GetCirclePoints(900.0, this.rotateAngle, targets);
            }

            for (int i = 1; i < 10; i++)
            {
                if (holdStill)
                {
                    this.decisions[i].VCode = 0;         // 绝对静止
                    this.decisions[i].TCode = 7;
                }
                else
                {
                    MoveTo(mission, i, targets[i - 1, 0], targets[i - 1, 1]);
                }
            }
        }

        /// <summary>
        /// 复制队形目标点
        /// </summary>
        private static void CopyTargets(double[,] src, double[,] dst)
        {
            for (int i = 0; i < 9; i++)
            {
                dst[i, 0] = src[i, 0];
                dst[i, 1] = src[i, 1];
            }
        }

        /// <summary>
        /// 生成圆上 9 个均匀分布的点
        /// </summary>
        private static void GetCirclePoints(double radius, double startAngle, double[,] pts)
        {
            for (int i = 0; i < 9; i++)
            {
                double a = startAngle + i * (2 * Math.PI / 9);
                pts[i, 0] = radius * Math.Cos(a);
                pts[i, 1] = radius * Math.Sin(a);
            }
        }

        /// <summary>
        /// 简易"游到目标点"控制：
        /// 角度差较大时以转向为主低速前进，否则直游并按距离选择速度档位
        /// </summary>
        private void MoveTo(Mission mission, int fishIndex, double tx, double tz)
        {
            RoboFish fish = mission.TeamsRef[0].Fishes[fishIndex];
            double hx = fish.PolygonVertices[0].X;
            double hz = fish.PolygonVertices[0].Z;
            double angToTarget = Math.Atan2(tz - hz, tx - hx);
            double diff = NormalizeAngle(angToTarget - fish.BodyDirectionRad);
            double dist = Math.Sqrt((tx - hx) * (tx - hx) + (tz - hz) * (tz - hz));

            if (Math.Abs(diff) > 0.55)
            {
                this.decisions[fishIndex].VCode = 3;     // 先转向，低速前进
                this.decisions[fishIndex].TCode = diff > 0 ? 11 : 3;
            }
            else
            {
                this.decisions[fishIndex].TCode = 7;     // 直游，按距离选择速度
                if (dist > 350.0)
                {
                    this.decisions[fishIndex].VCode = 9;
                }
                else if (dist > 150.0)
                {
                    this.decisions[fishIndex].VCode = 6;
                }
                else if (dist > 60.0)
                {
                    this.decisions[fishIndex].VCode = 3;
                }
                else
                {
                    this.decisions[fishIndex].VCode = 1;
                }
            }
        }

        /// <summary>
        /// 角度归一化到 (-PI, PI]
        /// </summary>
        private static double NormalizeAngle(double a)
        {
            while (a > Math.PI)
            {
                a -= 2 * Math.PI;
            }
            while (a <= -Math.PI)
            {
                a += 2 * Math.PI;
            }
            return a;
        }
    }
}
