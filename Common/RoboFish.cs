//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: RoboFish.cs
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真机器鱼基类定义文件
// Histroy:
// Date: 20110512  Author: LiYoubing
// Modification: 
// 1.为了双鱼协作过孔推球比赛项目中准备判断协作任务的完成状态，将仿真机器鱼的碰撞状态记录成员改成列表，
//   即RoboFish.Collision类型由CollisionType变成List<CollisionType>
// Date: 20110712  Author: LiYoubing
// Modification: 
// 1.修正RoboFish的CopyTo/Clone/Serialize/Deserialize向策略传递PolygonVertices[0]即仿真机器鱼鱼头位置
// Date: 20111225  Author: ChenXiao
// Modification: 
// 1.修正了左右鱼鳍和鱼体的染色裂隙问题，原因是场地z轴向下，算式符号取反
// 2.修改了鱼体的染色方法，由白色向指定的队伍颜色（ColorFish）渐变，因起始颜色固定为白色，所以建议ColorFish尽量避开亮色，如黄，绿；推荐颜色：黑，红，紫
// 3.加入尾鳍的绘制。
//尺寸：鱼头弧形半径为30mm；鱼体矩形长*宽为150mm*44mm；鱼尾

//三个关节的直线长度分别为70mm,54mm,54mm，三个关节的上边宽度为18.59mm,12.49mm,6.246mm；

//尾鳍上边宽度为4.858mm,下边宽度（尾部两极点的距离）为105mm，尾鳍的长度(尾鳍起始边的中点到尾

//鳍两顶点连线的中点的距离)为64.8mm；胸鳍的直线边的长度为75mm，弧形的最大宽度约为45mm，弧形

//的最大长度约为50mm.
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using xna = Microsoft.Xna.Framework; // in Microsoft.Xna.Framework.dll for Vector3

using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using System.Drawing.Drawing2D;
using URWPGSim2D.Core;

namespace URWPGSim2D.Common
{
    /// <summary>
    /// 仿真机器鱼基类，所有具体使命（比赛或实验项目）使用的仿真机器鱼类必须继承该类
    /// </summary>
    [DataContract]
    [Serializable]  // 跨AppDomain使用需要类型可序列化或继承MarshalByRefObject类
    public class RoboFish : IRoboFish, ICloneable, IDssSerializable
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public RoboFish()
        {
            SetFish();

            // 初始化碰撞检测内层四边形模型4个顶点构成的列表
            // 初始化碰撞检测BV树模型鱼尾部三关节叶结点4个顶点列表构成的列表
            for (int i = 0; i < 4; i++)
            {
                //PolygonVertices.Add(new xna.Vector3());
                Tail1PolygonVertices.Add(new xna.Vector3());
                Tail2PolygonVertices.Add(new xna.Vector3());
                Tail3PolygonVertices.Add(new xna.Vector3());
            }
            //初始化碰撞检测BV树模型鱼体躯干部分叶结点5个顶点构成的列表
            for (int i = 0; i < 5; i++)
            {
                BodyPolygonVertices.Add(new xna.Vector3());
            }
            //初始化碰撞检测模型7个顶点构成的列表
            for (int i = 0; i < 7; i++)
            {
                PolygonVertices.Add(new xna.Vector3());
                PrePolygonVertices.Add(new xna.Vector3());
            }
            //初始化碰撞检测BV树模型左右胸鳍叶结点4个顶点构成的列表
            for (int i = 0; i < 4; i++)
            {
                LeftPectoralPolygonVertices.Add(new xna.Vector3());
                RightPectoralPolygonVertices.Add(new xna.Vector3());
                TransactionVectorThreshold.Add(new float());
            }
            //初始化碰撞检测BV树模型尾鳍叶结点3个顶点构成的列表
            for (int i = 0; i < 3; i++)
            {
                LeftCaudalFinVertices.Add(new xna.Vector3());
                RightCaudalFinVertices.Add(new xna.Vector3());
            }
            //for (int i = 0; i < 20; i++)
            //{
            //    PreRoboFish.Add(new RoboFish());
            //}

            // 默认转弯档位值设置为7表示直游
            Tactic.TCode = 7;
            PreTactic.TCode = 7;
            TargetTactic.TCode = 7;
        }

        /// <summary>
        /// 从配置文件中读取可配置参数并计算出需要计算的参数
        /// </summary>
        public void SetFish()
        {
            BodyLength = 150;
            BodyWidth = 44;

            //鱼头长度
            HeadLength = 30;
            TailJointLength1 = 70;
            TailJointLength2 = 54;
            TailJointLength3 = 54;

            try
            {
                SysConfig myConfig = SysConfig.Instance();

                BodyLength = Convert.ToInt32(myConfig.MyXmlReader("BodyLength"));
                BodyWidth = Convert.ToInt32(myConfig.MyXmlReader("BodyWidth"));

                TailJointLength1 = Convert.ToInt32(myConfig.MyXmlReader("TailJointLength1"));
                TailJointLength2 = Convert.ToInt32(myConfig.MyXmlReader("TailJointLength2"));
                TailJointLength3 = Convert.ToInt32(myConfig.MyXmlReader("TailJointLength3"));
            }
            catch
            {
                Console.WriteLine("从配置文件读取参数出错...");
            }

            CollisionModelRadiusMm = (BodyLength + TailJointLength1 + TailJointLength2 + TailJointLength3) / 2;
            CollisionModelBodyRadiusMm = BodyLength / 2;
            CollisionModelTailRadiusMm = (TailJointLength1 + TailJointLength2 + TailJointLength3) / 2;
            FishBodyRadiusMm = (int)(Math.Sqrt(BodyLength * BodyLength + BodyWidth * BodyWidth)) / 2;
        }

        #region 具体仿真机器鱼需要向策略传递的参数表
        // 增加一个Hashtable用于保存具体仿真机器鱼需要给策略传递的参数值 LiYoubing 20110507
        /// <summary>
        /// 具体仿真机器鱼类即<see cref="URWPGSim2D.Common.RoboFish">RoboFish</see>类的子类需要给策略传递的变量名和值对
        /// 使用哈希表<see cref="System.Collections.Hashtable">Hashtable</see>存储需要传递的键值对
        /// 键（变量名）和值（变量值）均用string类型表示
        /// </summary>
        public Hashtable HtFishVariables = new Hashtable();
        #endregion

        #region 仿真机器鱼形状尺寸参数

        /// <summary>
        /// 鱼头长度（**配置**），单位毫米，added by chenxiao
        /// </summary>
        public int HeadLength;

        /// <summary>
        /// 鱼体前端刚体长度（**配置**），单位毫米
        /// </summary>
        public int BodyLength;

        /// <summary>
        /// 鱼体前端刚体宽度（**配置**），单位毫米
        /// </summary>
        public int BodyWidth;

        /// <summary>
        /// 尾部第一个关节长度（**配置**），单位毫米
        /// </summary>
        public int TailJointLength1;

        /// <summary>
        /// 尾部第二个关节长度（**配置**），单位毫米
        /// </summary>
        public int TailJointLength2;

        /// <summary>
        /// 尾部第三个关节长度（**配置**），单位毫米
        /// </summary>
        public int TailJointLength3;

        /// <summary>
        /// 尾部第一个关节相对鱼体的角度（**实时计算**），单位弧度
        /// </summary>
        public float TailToBodyAngle1;

        /// <summary>
        /// 尾部第二个关节相对鱼体的角度（**实时计算**），单位弧度
        /// </summary>
        public float TailToBodyAngle2;

        /// <summary>
        /// 尾部第三个关节相对鱼体的角度（**实时计算**），单位弧度
        /// </summary>
        public float TailToBodyAngle3;
        /// <summary>
        /// 尾部三个关节正弦摆动公式的初始调节相位值
        /// </summary>
        public float InitPhase;
        #endregion

        #region 仿真机器鱼运动学参数
        /// <summary>
        /// 当前绘图中心（鱼头刚体部分矩形中心）坐标，各维坐标单位毫米mm，2D版本中Y坐标固定为0
        /// </summary>
        public xna.Vector3 PositionMm;

        /// <summary>
        /// 当前绘图中心（鱼头刚体部分矩形中心）坐标备份，各维坐标单位毫米mm，2D版本中Y坐标固定为0
        /// </summary>        
        public xna.Vector3 PrePositionMm;

        /// <summary>
        /// 当前鱼体方向，单位弧度rad，值域[-PI, PI)
        /// </summary>
        public float BodyDirectionRad;

        /// <summary>
        /// 当前鱼体方向备份，单位弧度rad，值域[-PI, PI)
        /// </summary>
        public float PreBodyDirectionRad;

        /// <summary>
        /// 当前速度值，单位毫米每秒mm/s
        /// </summary>        
        public float VelocityMmPs;

        /// <summary>
        /// 当前速度值备份，单位毫米每秒mm/s
        /// </summary>        
        public float PreVelocityMmPs;

        /// <summary>
        /// 当前速度方向，单位弧度rad，值域(-PI, PI]
        /// </summary>        
        public float VelocityDirectionRad;

        /// <summary>
        /// 当前速度方向备份，单位弧度rad，值域(-PI, PI]
        /// </summary>        
        public float PreVelocityDirectionRad;

        /// <summary>
        /// 当前角速度值，单位弧度每秒rad/s
        /// </summary>
        public float AngularVelocityRadPs;

        /// <summary>
        /// 当前角速度值备份，单位弧度每秒rad/s
        /// </summary>
        public float PreAngularVelocityRadPs;
        #endregion

        #region 仿真机器鱼运动决策参数
        /// <summary>
        /// 当前决策数据值，每周期运动学计算完毕后根据条件决定是否更新
        /// </summary>
        public Decision Tactic;

        /// <summary>
        /// 当前决策数据值备份，每周期运动学计算完毕后使用目标决策数据值更新
        /// </summary>        
        public Decision PreTactic;

        /// <summary>
        /// 目标决策数据值，由高层策略计算后给出
        /// </summary>        
        public Decision TargetTactic;
        #endregion

        #region 仿真机器鱼碰撞检测参数 modified by renjing 20110414
        /// <summary>
        /// 鱼体碰撞检测BV树根结点，鱼圆形模型的圆心，各维坐标单位毫米
        /// </summary>
        public xna.Vector3 CollisionModelCenterPositionMm;

        /// <summary>
        /// 鱼体碰撞检测BV树根结点，鱼圆形模型半径（**待求**），单位毫米
        /// </summary>
        public int CollisionModelRadiusMm;

        /// <summary>
        /// 鱼体碰撞检测边界值点列表，主要用于判断鱼体最大值点和最小值点，7个元素依次为头部/左边头部点/左胸鳍/鱼尾第三关节左下点/鱼尾第三关节右下点/右胸鳍顶点/右边头部点，各顶点各维坐标单位毫米
        /// </summary>
        public List<xna.Vector3> PolygonVertices = new List<xna.Vector3>(7);

        /// <summary>
        /// 鱼体碰撞检测BV树第二层子结点，鱼刚体部分圆形模型的圆心，各维坐标单位毫米
        /// </summary>
        public xna.Vector3 CollisionModelBodyCenterPositionMm;

        /// <summary>
        /// 鱼体碰撞检测BV树第二层子结点，鱼刚体部分圆形模型的半径，单位毫米
        /// </summary>
        public int CollisionModelBodyRadiusMm;

        /// <summary>
        /// 鱼体碰撞检测BV树第二层子结点，鱼尾部圆形模型的圆心，各维坐标单位毫米
        /// </summary>
        public xna.Vector3 CollisionModelTailCenterPositionMm;

        /// <summary>
        /// 鱼体碰撞检测BV树第二层子结点，鱼尾部圆形模型半径，单位毫米
        /// </summary>
        public int CollisionModelTailRadiusMm;

        /// <summary>
        /// 鱼体碰撞检测BV树叶结点，左胸鳍四边形顶点列表，四个顶点顺序分别为胸鳍最左边点，下边点，上边点
        /// </summary>
        public List<xna.Vector3> LeftPectoralPolygonVertices = new List<xna.Vector3>(4);

        /// <summary>
        /// 鱼体碰撞检测BV树叶结点，右胸鳍四边形顶点列表，四个顶点顺序分别为胸鳍最右边点，下边点，上边点
        /// </summary>
        public List<xna.Vector3> RightPectoralPolygonVertices = new List<xna.Vector3>(4);

        /// <summary>
        /// 鱼体碰撞检测BV树叶结点，鱼体躯干部分五边形顶点列表，五个顶点顺序分别为头部左边点、头部顶点、头部右边点、右下边点、左下边点
        /// </summary>
        public List<xna.Vector3> BodyPolygonVertices = new List<xna.Vector3>(5);

        /// <summary>
        /// 鱼体碰撞检测BV树叶结点，第一关节四边形顶点列表，四个顶点顺序分别为左上边点、右上边点、右下边点、左下边点
        /// </summary>
        public List<xna.Vector3> Tail1PolygonVertices = new List<xna.Vector3>(4);

        /// <summary>
        /// 鱼体碰撞检测BV树叶结点，第二关节四边形顶点列表，四个顶点顺序分别为左上边点、右上边点、右下边点、左下边点
        /// </summary>
        public List<xna.Vector3> Tail2PolygonVertices = new List<xna.Vector3>(4);

        /// <summary>
        /// 鱼体碰撞检测BV树叶结点，第三关节四边形顶点列表，四个顶点顺序分别为左上边点、右上边点、右下边点、左下边点
        /// </summary>
        public List<xna.Vector3> Tail3PolygonVertices = new List<xna.Vector3>(4);

        /// <summary>
        /// 鱼体碰撞检测BV树叶节点，尾鳍左半部分三角形顶点列表，三个顶点分别为最左点、尾部凹形顶点、与尾部第三关节连接点
        /// </summary>
        public List<xna.Vector3> LeftCaudalFinVertices = new List<xna.Vector3>(3);

        /// <summary>
        /// 鱼体碰撞检测BV树叶节点，尾鳍右半部分三角形顶点列表，三个顶点分别为最右点、尾部凹形顶点、与尾部第三关节连接点
        /// </summary>
        public List<xna.Vector3> RightCaudalFinVertices = new List<xna.Vector3>(3);

        /// <summary>
        /// 鱼和其他对象碰撞时碰撞相交向量模长的阈值，依次为鱼和障碍物的阈值、鱼和鱼的阈值、鱼和球的阈值、鱼和边界的阈值
        /// </summary>
        public List<float> TransactionVectorThreshold = new List<float>(4);

        /// <summary>
        /// 机器鱼前一周期的碰撞模型，是由七点组成的外围模型
        /// </summary>
        public List<xna.Vector3> PrePolygonVertices = new List<xna.Vector3>(7);


        #endregion

        #region 仿真机器鱼其他状态参数
        //当前碰撞状态 改成 碰撞状态列表 LiYoubing 20110511
        /// <summary>
        /// 碰撞状态列表
        /// </summary>
        public List<CollisionType> Collision = new List<CollisionType>();
        //public CollisionType Collision;

        ///// <summary>
        ///// 当前碰撞状态备份 这个状态废除不用 LiYoubing 20110511
        ///// </summary>        
        //public CollisionType PreCollision;

        // modified by chenwei 20110603
        /// <summary>
        /// 前方色标（队伍标识）颜色->仿真机器鱼鱼体颜色
        /// 考虑实体机器鱼已经去掉色标，仿真机器鱼需要把鱼体绘制成统一颜色
        /// 新的着色方案不再使用仿真机器鱼鱼体前端矩形来绘制色标
        /// 仿真机器鱼通体使用统一颜色即此处的ColorFish
        /// 每支队伍的全部仿真机器鱼设置成统一颜色即所有仿真机器鱼的ColorFish相同
        /// 不同队伍的仿真机器鱼ColorFish应设置成不同值以区别不同队伍
        /// </summary>
        public Color ColorFish;

        // modified by LiYoubing 20110607
        /// <summary>
        /// 后方色标（队员标识）颜色->仿真机器鱼编号颜色
        /// 场上所有仿真机器鱼的编号应设置成相同值且不同于任何一支队伍的ColorFish值
        /// </summary>
        public Color ColorId;

        /// <summary>
        /// 仿真机器鱼绘制次数计数（Draw()方法中用）
        /// </summary>
        public int CountDrawing;

        /// <summary>
        ///  仿真机器鱼绘制轨迹用的点列表
        /// </summary>
        public List<Point> TrajectoryPoints = new List<Point>();

        /// <summary>
        /// 仿真机器鱼前端矩形部分外接圆半径，用于界面选中仿真机器鱼处理过程，单位毫米
        /// </summary>
        public int FishBodyRadiusMm;
        #endregion

        /// <summary>
        /// 重置仿真机器鱼的部分运动学参数，用于重新初始化使命清除上一次运行结束时保存的值
        /// </summary>
        public void ResetSomeLocomotionPara()
        {
            VelocityMmPs = PreVelocityMmPs = 0;                     // 重置速度值
            VelocityDirectionRad = PreVelocityDirectionRad = 0;     // 重置速度方向值
            AngularVelocityRadPs = PreAngularVelocityRadPs = 0;     // 重置角速度值
            Tactic.VCode = PreTactic.VCode = TargetTactic.VCode = 0;// 重置速度档位值
            Tactic.TCode = PreTactic.TCode = TargetTactic.TCode = 7;// 重置转弯档位值

            TailToBodyAngle1 = 0;   // 重置第一关节相对鱼体的角度
            TailToBodyAngle2 = 0;   // 重置第二关节相对鱼体的角度
            TailToBodyAngle3 = 0;   // 重置第三关节相对鱼体的角度
        }

        /// <summary>
        /// 根据仿真周期毫秒数更新仿真机器鱼位姿
        /// </summary>
        /// <param name="timeIntervalMs">仿真周期毫秒数</param>
        /// <param name="flag">所属队伍左右半场标志值1为左半场-1为右半场</param>
        /// <remarks>注意：按照运动学模型设计的顺序，每个周期各运动学计算相关方法调用顺序为
        /// UpdatePose, UpdateVelocityAndAngularVelocity, DetectCollision, ResponseCollision</remarks>
        public void UpdatePose(int timeIntervalMs, int flag)
        {
            Locomotion.UpdateFishPose(ref PositionMm, ref PrePositionMm, ref BodyDirectionRad, ref PreBodyDirectionRad,
                VelocityMmPs, VelocityDirectionRad, AngularVelocityRadPs, timeIntervalMs, flag);
        }

        /// <summary>
        /// 根据仿真周期毫秒数更新仿真机器鱼速度值速度方向和角速度值
        /// </summary>
        /// <param name="timeIntervalMs">仿真周期毫秒数</param>
        /// <param name="flag">所属队伍左右半场标志值1为左半场-1为右半场</param>
        /// <remarks>注意：按照运动学模型设计的顺序，每个周期各运动学计算相关方法调用顺序为
        /// UpdatePose, UpdateVelocityAndAngularVelocity, DetectCollision, ResponseCollision</remarks>
        public void UpdateVelocityAndAngularVelocity(int timeIntervalMs, int flag)
        {
            Locomotion.UpdateFishVelocityAndAngularVelocity(ref VelocityMmPs, ref PreVelocityMmPs,
                ref VelocityDirectionRad, ref PreVelocityDirectionRad, ref AngularVelocityRadPs, ref PreAngularVelocityRadPs, BodyDirectionRad,
                ref Tactic.VCode, ref PreTactic.VCode, TargetTactic.VCode,
                ref Tactic.TCode, ref PreTactic.TCode, TargetTactic.TCode, timeIntervalMs, flag);
        }

        //added by Chen Wei 20110307
        /// <summary>
        /// 仿真机器鱼各个点的更新计算方法 
        /// <param name="isRunning">仿真使命（比赛或实验项目）是否正在运行即机器鱼是否正在游动</param>
        /// <param name="isPaused">仿真使命（比赛或实验项目）是否暂停即机器鱼是否正在游动</param>
        /// <param name="initPhase">仿真机器鱼尾部关节的初始相位</param>
        /// <param name="fish">仿真机器鱼对象</param>
        /// </summary>
        public void CalculateFishPostion(bool isRunning, bool isPaused, float initPhase, ref RoboFish fish)
        {
            #region 计算仿真机器鱼8个位置点坐标，详见图fish_HeadLocation.JPG
            float WidthfishChange = 1.7f; // 胸稽不够宽 乘以一个倍数.

            float halfSinTheta = (float)Math.Sin(fish.BodyDirectionRad) / 2; // 一半SINθ=(SIN鱼体方向)/2
            float halfCosTheta = (float)Math.Cos(fish.BodyDirectionRad) / 2; // 一半COSθ=(COS鱼体方向)/2

            float xBodyHalf = halfCosTheta * BodyLength;
            float zBodyHalf = halfSinTheta * BodyLength;

            xna.Vector3 pointReal2 = new xna.Vector3((fish.PositionMm.X + BodyLength * halfCosTheta), 0, (fish.PositionMm.Z + BodyLength * halfSinTheta));
            xna.Vector3 pointReal1 = new xna.Vector3((pointReal2.X + BodyWidth * halfSinTheta), 0, (pointReal2.Z - BodyWidth * halfCosTheta));
            xna.Vector3 pointReal3 = new xna.Vector3((pointReal2.X - BodyWidth * halfSinTheta), 0, (pointReal2.Z + BodyWidth * halfCosTheta));
            xna.Vector3 pointReal4 = new xna.Vector3((fish.PositionMm.X - BodyWidth * halfSinTheta), 0, (fish.PositionMm.Z + BodyWidth * halfCosTheta));
            xna.Vector3 pointReal8 = new xna.Vector3((fish.PositionMm.X + BodyWidth * halfSinTheta), 0, (fish.PositionMm.Z - BodyWidth * halfCosTheta));
            xna.Vector3 pointReal6 = new xna.Vector3((fish.PositionMm.X - BodyLength * halfCosTheta), 0, (fish.PositionMm.Z - BodyLength * halfSinTheta));
            xna.Vector3 pointReal5 = new xna.Vector3((pointReal6.X - BodyWidth * halfSinTheta), 0, (pointReal6.Z + BodyWidth * halfCosTheta));
            xna.Vector3 pointReal7 = new xna.Vector3((pointReal6.X + BodyWidth * halfSinTheta), 0, (pointReal6.Z - BodyWidth * halfCosTheta));

            // 可以任意配置胸稽的起始点位置 通过比例来配置 chenwei 20100930
            xna.Vector3 pointReal11 = new xna.Vector3((pointReal1.X * 3 + pointReal8.X * 3) / 6, 0, (pointReal1.Z * 3 + pointReal8.Z * 3) / 6);
            xna.Vector3 pointReal12 = new xna.Vector3((pointReal3.X * 3 + pointReal4.X * 3) / 6, 0, (pointReal3.Z * 3 + pointReal4.Z * 3) / 6);

            // 可以任意配置胸稽的结束点位置 通过比例来配置
            xna.Vector3 pointReal13 = new xna.Vector3((pointReal4.X * 5 + pointReal5.X * 1) / 6, 0, (pointReal4.Z * 5 + pointReal5.Z * 1) / 6);
            xna.Vector3 pointReal14 = new xna.Vector3((pointReal8.X * 5 + pointReal7.X * 1) / 6, 0, (pointReal8.Z * 5 + pointReal7.Z * 1) / 6);

            xna.Vector3 pointReal15 = new xna.Vector3((pointReal13.X - BodyWidth * halfSinTheta * WidthfishChange), 0,
                (pointReal13.Z + BodyWidth * halfCosTheta * WidthfishChange));

            // 乘以一个倍数来改变胸稽宽度
            xna.Vector3 pointReal16 = new xna.Vector3((pointReal14.X + BodyWidth * halfSinTheta * WidthfishChange), 0,
                (pointReal14.Z - BodyWidth * halfCosTheta * WidthfishChange));
            #endregion

            #region 按照鱼体波数据绘制仿真机器鱼尾部三个关节
            //if ((isRunning == true) && (isPaused == false)) // 只有启动使命执行后仿真机器鱼游动起来的情况下才让鱼尾摆动
            //{
            //    MyMission myMission = MyMission.Instance();
            //    float[] DeflectionAngle2 = DeflectionAngleOfJoint.DeflectionAngleOfJoint1(TargetTactic.VCode, TargetTactic.TCode,
            //        myMission.ParasRef.TotalSeconds * 1000 / myMission.ParasRef.MsPerCycle - myMission.ParasRef.RemainingCycles,
            //        myMission.ParasRef.MsPerCycle, initPhase, false);
            //    TailToBodyAngle1 = DeflectionAngle2[0];
            //    TailToBodyAngle2 = DeflectionAngle2[1];
            //    TailToBodyAngle3 = DeflectionAngle2[2];
            //}

            float xTail = (float)(Math.Cos(fish.BodyDirectionRad + TailToBodyAngle1) * TailJointLength1);
            float zTail = (float)(Math.Sin(fish.BodyDirectionRad + TailToBodyAngle1) * TailJointLength1);

            float xTail2 = (float)(Math.Cos(fish.BodyDirectionRad + TailToBodyAngle2) * TailJointLength2);
            float zTail2 = (float)(Math.Sin(fish.BodyDirectionRad + TailToBodyAngle2) * TailJointLength2);

            float xTail3 = (float)(Math.Cos(fish.BodyDirectionRad + TailToBodyAngle3) * TailJointLength3);
            float zTail3 = (float)(Math.Sin(fish.BodyDirectionRad + TailToBodyAngle3) * TailJointLength3);

            // 尾部中心点
            xna.Vector3 pointReal21 = new xna.Vector3((fish.PositionMm.X - xBodyHalf),
                0, (fish.PositionMm.Z - zBodyHalf));
            xna.Vector3 pointReal22 = new xna.Vector3((fish.PositionMm.X - xBodyHalf - xTail),
                0, (fish.PositionMm.Z - zBodyHalf - zTail));
            xna.Vector3 pointReal23 = new xna.Vector3((fish.PositionMm.X - xBodyHalf - xTail - xTail2),
                0, (fish.PositionMm.Z - zBodyHalf - zTail - zTail2));
            xna.Vector3 pointReal24 = new xna.Vector3((fish.PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3),
                0, (fish.PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3));

            // 定义角度          
            float angleCenter2Uptail1 = (float)(TailToBodyAngle1 + fish.BodyDirectionRad - (float)Math.PI * 0.5);
            float angleCenter2Uptail2 = (float)(TailToBodyAngle2 + fish.BodyDirectionRad - (float)Math.PI * 0.5);
            float angleCenter2Uptail3 = (float)(TailToBodyAngle3 + fish.BodyDirectionRad - (float)Math.PI * 0.5);
            float CosThetaangleCenter2Uptail1 = (float)Math.Cos(angleCenter2Uptail1);
            float SinThetaangleCenter2Uptail1 = (float)Math.Sin(angleCenter2Uptail1);
            float CosThetaangleCenter2Uptail2 = (float)Math.Cos(angleCenter2Uptail2);
            float SinThetaangleCenter2Uptail2 = (float)Math.Sin(angleCenter2Uptail2);
            float CosThetaangleCenter2Uptail3 = (float)Math.Cos(angleCenter2Uptail3);
            float SinThetaangleCenter2Uptail3 = (float)Math.Sin(angleCenter2Uptail3);

            // 定义梯形上下底边半径
            float halfwidthFishTail1TX1 = TailJointLength1 * 0.29568f * 0.9f;
            float halfwidthFishTail1TX2 = TailJointLength1 * 0.274176f * 0.8f;
            float halfwidthFishTail2TX1 = TailJointLength2 * 0.25704f * 0.9f;
            float halfwidthFishTail2TX2 = TailJointLength2 * 0.2142f * 0.7f;
            float halfwidthFishTail3TX1 = TailJointLength3 * 0.12852f * 0.9f;
            float halfwidthFishTail3TX2 = TailJointLength3 * 0.12852f * 0.7f;

            // 定义梯形的12个点 412和411对称 中间原理还需要认真理解透.
            xna.Vector3 pointReal411 = new xna.Vector3((pointReal21.X + halfwidthFishTail1TX1 * CosThetaangleCenter2Uptail1),
                0, (pointReal21.Z + halfwidthFishTail1TX1 * SinThetaangleCenter2Uptail1));
            xna.Vector3 pointReal412 = new xna.Vector3((pointReal21.X - halfwidthFishTail1TX1 * CosThetaangleCenter2Uptail1),
                0, (pointReal21.Z - halfwidthFishTail1TX1 * SinThetaangleCenter2Uptail1));
            xna.Vector3 pointReal413 = new xna.Vector3((pointReal22.X + halfwidthFishTail1TX2 * CosThetaangleCenter2Uptail1),
                0, (pointReal22.Z + halfwidthFishTail1TX2 * SinThetaangleCenter2Uptail1));
            xna.Vector3 pointReal414 = new xna.Vector3((pointReal22.X - halfwidthFishTail1TX2 * CosThetaangleCenter2Uptail1),
                0, (pointReal22.Z - halfwidthFishTail1TX2 * SinThetaangleCenter2Uptail1));

            xna.Vector3 pointReal421 = new xna.Vector3((pointReal22.X + halfwidthFishTail2TX1 * CosThetaangleCenter2Uptail2),
                0, (pointReal22.Z + halfwidthFishTail2TX1 * SinThetaangleCenter2Uptail2));
            xna.Vector3 pointReal422 = new xna.Vector3((pointReal22.X - halfwidthFishTail2TX1 * CosThetaangleCenter2Uptail2),
                0, (pointReal22.Z - halfwidthFishTail2TX1 * SinThetaangleCenter2Uptail2));
            xna.Vector3 pointReal423 = new xna.Vector3((pointReal23.X + halfwidthFishTail2TX2 * CosThetaangleCenter2Uptail2),
                0, (pointReal23.Z + halfwidthFishTail2TX2 * SinThetaangleCenter2Uptail2));
            xna.Vector3 pointReal424 = new xna.Vector3((pointReal23.X - halfwidthFishTail2TX2 * CosThetaangleCenter2Uptail2),
                0, (pointReal23.Z - halfwidthFishTail2TX2 * SinThetaangleCenter2Uptail2));

            xna.Vector3 pointReal431 = new xna.Vector3((pointReal23.X + halfwidthFishTail3TX1 * CosThetaangleCenter2Uptail3),
                0, (pointReal23.Z + halfwidthFishTail3TX1 * SinThetaangleCenter2Uptail3));
            xna.Vector3 pointReal432 = new xna.Vector3((pointReal23.X - halfwidthFishTail3TX1 * CosThetaangleCenter2Uptail3),
                0, (pointReal23.Z - halfwidthFishTail3TX1 * SinThetaangleCenter2Uptail3));
            xna.Vector3 pointReal433 = new xna.Vector3((pointReal24.X + halfwidthFishTail3TX2 * CosThetaangleCenter2Uptail3),
                0, (pointReal24.Z + halfwidthFishTail3TX2 * SinThetaangleCenter2Uptail3));
            xna.Vector3 pointReal434 = new xna.Vector3((pointReal24.X - halfwidthFishTail3TX2 * CosThetaangleCenter2Uptail3),
                0, (pointReal24.Z - halfwidthFishTail3TX2 * SinThetaangleCenter2Uptail3));
            #endregion
            #region 鱼头和鱼鳍部分点的计算，详见draw方法 addedby chenxiao
            //计算绘制左侧鱼鳍的bezier曲线的两个控制点
            xna.Vector3 leftFinControl1 = new xna.Vector3((pointReal14.X + 100 * halfSinTheta), 0,
                (pointReal14.Z - 100 * halfCosTheta));
            xna.Vector3 leftFinControl2 = new xna.Vector3((pointReal7.X + 100 * halfSinTheta / 2), 0,
             (pointReal7.Z - 100 * halfCosTheta / 2));
            //计算绘制右侧鱼鳍的bezier曲线的两个控制点
            xna.Vector3 rightFinControl1 = new xna.Vector3((pointReal13.X - 100 * halfSinTheta), 0,
              (pointReal13.Z + 100 * halfCosTheta));
            xna.Vector3 rightFinControl2 = new xna.Vector3((pointReal5.X - 100 * halfSinTheta / 2), 0,
              (pointReal5.Z + 100 * halfCosTheta / 2));

            //鱼头最前点
            xna.Vector3 pointAhead = new xna.Vector3((pointReal2.X + HeadLength * 2 * halfCosTheta), 0, (pointReal2.Z + HeadLength * 2 * halfSinTheta));
            //xna.Vector3 pointAhead = new xna.Vector3((pointReal2.X + HeadLength * halfCosTheta), 0, (pointReal2.Z + HeadLength  * halfSinTheta));
            //尾鳍凹弧的上下两顶点连线的中点
            //xna.Vector3 tailEnd = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3*1.1f), 0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3*1.1f));
            xna.Vector3 tailEnd = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3 * 2.0f), 0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3 * 2.0f));

            xna.Vector3 tailAxis1 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3 * 1.3f), 0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3 * 1.3f));
            xna.Vector3 tailAxis2 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3 * 1.6f), 0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3 * 1.6f));
            xna.Vector3 tailAxis3 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3 * 1.8f), 0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3 * 1.8f));
            xna.Vector3 tailAxis4 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3 * 2.0f), 0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3 * 2.0f));

            //定义四个长度，表示与轴线的垂直距离，分别与四个轴线定位点对应
            float distanceToAxis1 = 55;
            float distanceToAxis2 = 85;
            float distanceToAxis3 = 70;
            float distanceToAxis4 = 100;
            //尾鳍轴心的角度
            float tailAngle = (BodyDirectionRad + TailToBodyAngle3);
            //当角度<-π,或者>π.进行处理使之范围处于-π~π
            if (tailAngle < -Math.PI)
            {
                tailAngle = tailAngle + (float)Math.PI * 2;
            }
            else if (tailAngle > Math.PI)
            {
                tailAngle = tailAngle - (float)Math.PI * 2;
            }
            //缩放参数
            float scalingParameter = (float)Math.Abs(Math.Cos(tailAngle));
            float halfSinTailAngle = (float)Math.Sin(tailAngle) / 2 * scalingParameter;//缩放后的sin
            float halfCosTailAngle = (float)Math.Cos(tailAngle) / 2 * scalingParameter;//缩放后的cos
            //镜像参数设定，如朝右的鱼左顶点偏离轴线大，朝左的鱼右顶点偏离轴线大,简单讲就是1,2对调，3,4对调
            if (Math.Abs(tailAngle) > Math.PI / 2)
            {
                distanceToAxis1 = 85;
                distanceToAxis2 = 55;
                distanceToAxis3 = 100;
                distanceToAxis4 = 70;
                xna.Vector3 temp = tailAxis2;
                tailAxis2 = tailAxis1;
                tailAxis1 = temp;
                temp = tailAxis3;
                tailAxis3 = tailAxis4;
                tailAxis4 = temp;
            }
            //尾鳍上顶点
            xna.Vector3 tailFinLeft = new xna.Vector3(tailAxis4.X + distanceToAxis4 * halfSinTailAngle, 0, tailAxis4.Z - distanceToAxis4 * halfCosTailAngle);
            //尾鳍下顶点
            xna.Vector3 tailFinRight = new xna.Vector3(tailAxis3.X - distanceToAxis3 * halfSinTailAngle, 0, tailAxis3.Z + distanceToAxis3 * halfCosTailAngle);
            //计算的临时点
            xna.Vector3 tempPoint = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3 * 1.8f), 0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3 * 1.8f));
            //尾鳍凹弧的中点
            xna.Vector3 tailFinMiddle = new xna.Vector3(tempPoint.X + 30 * halfSinTheta, 0, tempPoint.Z - 20 * halfCosTheta);

            #endregion
            //鱼体碰撞模型BV树左胸鳍叶结点
            //LeftPectoralPolygonVertices[0] = pointReal16;
            LeftPectoralPolygonVertices[0] = (leftFinControl1 + leftFinControl2) / 2;//改为两控制点的中点modified by chenxiao
            LeftPectoralPolygonVertices[1] = new xna.Vector3((pointReal14.X + leftFinControl2.X) / 2, 0, (pointReal14.Z + leftFinControl1.Z) / 2);
            LeftPectoralPolygonVertices[2] = pointReal14;
            LeftPectoralPolygonVertices[3] = pointReal11;

            //鱼体碰撞模型BV树右胸鳍叶结点
            RightPectoralPolygonVertices[0] = (rightFinControl1 + rightFinControl2) / 2; //改为两控制点的中点modified by chenxiao
            RightPectoralPolygonVertices[1] = new xna.Vector3((pointReal13.X + rightFinControl2.X) / 2, 0, (pointReal13.Z + rightFinControl1.Z) / 2);
            RightPectoralPolygonVertices[2] = pointReal13;
            RightPectoralPolygonVertices[3] = pointReal12;

            //鱼体碰撞模型BV树鱼体躯干部分叶结点
            BodyPolygonVertices[0] = pointReal1;
            //BodyPolygonVertices[1] = new xna.Vector3((float)(pointReal2.X + BodyWidth * halfCosTheta),
            //    0, (float)(pointReal2.Z + BodyWidth * halfSinTheta));
            BodyPolygonVertices[1] = pointAhead;//改为弧线的顶点 modified by chenxiao
            BodyPolygonVertices[2] = pointReal3;
            BodyPolygonVertices[3] = pointReal5;
            BodyPolygonVertices[4] = pointReal7;

            //鱼体碰撞模型BV树尾部第一关节叶结点
            Tail1PolygonVertices[0] = pointReal412;
            Tail1PolygonVertices[1] = pointReal411;
            Tail1PolygonVertices[2] = pointReal413;
            Tail1PolygonVertices[3] = pointReal414;

            //鱼体碰撞模型BV树尾部第二关节叶结点
            Tail2PolygonVertices[0] = pointReal422;
            Tail2PolygonVertices[1] = pointReal421;
            Tail2PolygonVertices[2] = pointReal423;
            Tail2PolygonVertices[3] = pointReal424;

            //鱼体碰撞模型BV树尾部第三关节叶结点
            Tail3PolygonVertices[0] = pointReal432;
            Tail3PolygonVertices[1] = pointReal431;
            Tail3PolygonVertices[2] = pointReal433;
            Tail3PolygonVertices[3] = pointReal434;
            //Tail3PolygonVertices[2] = tailFinLeft;//改为两控制点的中点 modified by chenxiao 
            //Tail3PolygonVertices[3] = tailFinRight;//改为两控制点的中点 modified by chenxiao

            //鱼体碰撞模型BV树尾鳍部分为两个三角形，左半三角形
            LeftCaudalFinVertices[0] = tailFinLeft;
            LeftCaudalFinVertices[1] = tailFinMiddle;
            LeftCaudalFinVertices[2] = pointReal434;

            //鱼体碰撞模型BV树尾鳍部分为两个三角形，右半三角形
            RightCaudalFinVertices[0] = tailFinRight;
            RightCaudalFinVertices[1] = tailFinMiddle;
            RightCaudalFinVertices[2] = pointReal433;


            // 实时更新仿真机器鱼碰撞模型BV树根结点
            CollisionModelCenterPositionMm = new xna.Vector3(
                (float)(BodyPolygonVertices[1].X - CollisionModelRadiusMm * 2 * halfCosTheta), 0,
                (float)(BodyPolygonVertices[1].Z - CollisionModelRadiusMm * 2 * halfSinTheta));

            // 实时更新仿真机器鱼碰撞模型BV树第二层鱼刚体部分子结点
            CollisionModelBodyCenterPositionMm = new xna.Vector3(
                (float)(BodyPolygonVertices[1].X - CollisionModelBodyRadiusMm * 2 * halfCosTheta), 0,
                (float)(BodyPolygonVertices[1].Z - CollisionModelBodyRadiusMm * 2 * halfSinTheta));

            // 实时更新仿真机器鱼碰撞模型BV树第二层鱼尾部子结点
            CollisionModelTailCenterPositionMm = (pointReal23 - pointReal22) / 3 + pointReal22;
            //CollisionModelTailCenterPositionMm = pointReal22;




            // 实时更新仿真机器鱼碰撞检测边界点列表
            PolygonVertices[0] = BodyPolygonVertices[1];   // 鱼体头部顶点
            PolygonVertices[1] = BodyPolygonVertices[0]; ;   //鱼体头部左边点
            PolygonVertices[2] = LeftPectoralPolygonVertices[0];  // 鱼体左胸鳍顶点
            PolygonVertices[3] = LeftCaudalFinVertices[0];//尾鳍左顶点
            PolygonVertices[4] = RightCaudalFinVertices[0];//尾鳍右顶点
            //PolygonVertices[3] = Tail3PolygonVertices[3];   // 鱼体尾部第三关节左下点
            //PolygonVertices[4] = Tail3PolygonVertices[2];//鱼体尾部第三关节右下点
            PolygonVertices[5] = RightPectoralPolygonVertices[0];// 鱼体右胸鳍顶点
            PolygonVertices[6] = BodyPolygonVertices[2]; //鱼体头部右边点

        }

        /// <summary>
        /// 将当前仿真机器鱼绘制到GDI+绘图对象上
        /// </summary>
        /// <param name="g">来自与绘图容器PictureBox等尺寸的Bitmap的GDI+绘图对象</param>
        /// <param name="isRunning">仿真使命（比赛或实验项目）是否正在运行即机器鱼是否正在游动 
        /// 用于控制是否绘制鱼尾游动效果</param>
        /// <param name="isPaused">仿真使命（比赛或实验项目）是否在运行过程中被设置为暂停状态
        /// 暂停状态可由界面按下暂停按钮设置，也可根据需要使用程序代码设置</param>
        /// <param name="initPhase">仿真机器鱼尾部关节的初始相位</param>
        /// <param name="fishId">鱼的ID</param>
        /// <param name="flag">所属队伍左右半场标志值1为左半场-1为右半场 
        /// 在右半场flag为-1，则在绘制之前让鱼体朝向取反，如此能绘出右半场队伍的仿真机器鱼从右向左动作的效果 
        /// 再配合仿真机器鱼位姿更新时各维坐标增量也取反，便实现了同一策略适用左右半场</param>
        public void Draw(ref Graphics g, bool isRunning, bool isPaused, float initPhase, int fishId, int flag)
        {
            #region
            Field f = Field.Instance();

            #region 计算仿真机器鱼8个位置点坐标，详见图fish_HeadLocation.JPG
            float WidthfishChange = 1.7f; // 胸稽不够宽 乘以一个倍数.

            float halfSinTheta = (float)Math.Sin(BodyDirectionRad) / 2; // 一半SINθ=(SIN鱼体方向)/2
            float halfCosTheta = (float)Math.Cos(BodyDirectionRad) / 2; // 一半COSθ=(COS鱼体方向)/2

            float xBodyHalf = halfCosTheta * BodyLength;
            float zBodyHalf = halfSinTheta * BodyLength;
            //鱼身矩形上边的中点
            xna.Vector3 pointReal2 = new xna.Vector3((PositionMm.X + BodyLength * halfCosTheta), 0, (PositionMm.Z + BodyLength * halfSinTheta));
            //鱼身矩形上边的左端点
            xna.Vector3 pointReal1 = new xna.Vector3((pointReal2.X + BodyWidth * halfSinTheta), 0, (pointReal2.Z - BodyWidth * halfCosTheta));
            //鱼身矩形上边的右端点
            xna.Vector3 pointReal3 = new xna.Vector3((pointReal2.X - BodyWidth * halfSinTheta), 0, (pointReal2.Z + BodyWidth * halfCosTheta));
            //鱼身矩形中线的右端点
            xna.Vector3 pointReal4 = new xna.Vector3((PositionMm.X - BodyWidth * halfSinTheta), 0, (PositionMm.Z + BodyWidth * halfCosTheta));
            //鱼身矩形中线的左端点
            xna.Vector3 pointReal8 = new xna.Vector3((PositionMm.X + BodyWidth * halfSinTheta), 0, (PositionMm.Z - BodyWidth * halfCosTheta));
            //鱼身矩形底边的中点
            xna.Vector3 pointReal6 = new xna.Vector3((PositionMm.X - BodyLength * halfCosTheta), 0, (PositionMm.Z - BodyLength * halfSinTheta));
            //鱼身矩形底边的右端点
            xna.Vector3 pointReal5 = new xna.Vector3((pointReal6.X - BodyWidth * halfSinTheta), 0, (pointReal6.Z + BodyWidth * halfCosTheta));
            //鱼身矩形底边的左端点
            xna.Vector3 pointReal7 = new xna.Vector3((pointReal6.X + BodyWidth * halfSinTheta), 0, (pointReal6.Z - BodyWidth * halfCosTheta));

            // 可以任意配置胸稽的起始点位置 通过比例来配置 chenwei 20100930
            //左侧鱼鳍的起点，现定位为鱼体矩形的上半方矩形的左侧中点
            xna.Vector3 pointReal11 = new xna.Vector3((pointReal1.X * 3 + pointReal8.X * 3) / 6, 0, (pointReal1.Z * 3 + pointReal8.Z * 3) / 6);
            //右侧鱼鳍的起点，现定位为鱼体矩形的上半方矩形的右侧中点
            xna.Vector3 pointReal12 = new xna.Vector3((pointReal3.X * 3 + pointReal4.X * 3) / 6, 0, (pointReal3.Z * 3 + pointReal4.Z * 3) / 6);

            // 可以任意配置胸稽的结束点位置 通过比例来配置
            //右侧鱼鳍的终点，现定位为鱼体矩形的上半方矩形的右侧中点
            xna.Vector3 pointReal13 = new xna.Vector3((pointReal4.X * 5 + pointReal5.X * 1) / 6, 0, (pointReal4.Z * 5 + pointReal5.Z * 1) / 6);
            //左侧鱼鳍的终点，现定位为鱼体矩形的上半方矩形的左侧中点
            xna.Vector3 pointReal14 = new xna.Vector3((pointReal8.X * 5 + pointReal7.X * 1) / 6, 0, (pointReal8.Z * 5 + pointReal7.Z * 1) / 6);
            //右鱼鳍的外部端点
            xna.Vector3 pointReal15 = new xna.Vector3((pointReal13.X - BodyWidth * halfSinTheta * WidthfishChange), 0,
                (pointReal13.Z + BodyWidth * halfCosTheta * WidthfishChange));

            // 乘以一个倍数来改变胸稽宽度
            //左鱼鳍的外部端点
            xna.Vector3 pointReal16 = new xna.Vector3((pointReal14.X + BodyWidth * halfSinTheta * WidthfishChange), 0,
                (pointReal14.Z - BodyWidth * halfCosTheta * WidthfishChange));
            #endregion

            // by chenwei 20100926
            #region 按照鱼体波数据绘制仿真机器鱼尾部三个关节
            if ((isRunning == true) && (isPaused == false)) // 只有启动使命执行后仿真机器鱼游动起来的情况下才让鱼尾摆动
            {
                MyMission myMission = MyMission.Instance();

                // 需要关节调直的条件设置，对抗性比赛进球或交换半场时IsPauseNeeded为true，需要关节调直
                // 有其他需要关节调直操作的情况时，可以进一步复合标志量 added by LiYoubing 20110310
                bool isClearNeeded = myMission.ParasRef.IsPauseNeeded;
                float[] DeflectionAngle2 = DeflectionAngleOfJoint.DeflectionAngleOfJoint1(TargetTactic.VCode, TargetTactic.TCode,
                    myMission.ParasRef.TotalSeconds * 1000 / myMission.ParasRef.MsPerCycle - myMission.ParasRef.RemainingCycles,
                    myMission.ParasRef.MsPerCycle, initPhase, isClearNeeded);
                TailToBodyAngle1 = DeflectionAngle2[0];
                TailToBodyAngle2 = DeflectionAngle2[1];
                TailToBodyAngle3 = DeflectionAngle2[2];
            }
            //三个尾部关节对应的x和z的偏移量
            float xTail = (float)(Math.Cos(BodyDirectionRad + TailToBodyAngle1) * TailJointLength1);
            float zTail = (float)(Math.Sin(BodyDirectionRad + TailToBodyAngle1) * TailJointLength1);

            float xTail2 = (float)(Math.Cos(BodyDirectionRad + TailToBodyAngle2) * TailJointLength2);
            float zTail2 = (float)(Math.Sin(BodyDirectionRad + TailToBodyAngle2) * TailJointLength2);

            float xTail3 = (float)(Math.Cos(BodyDirectionRad + TailToBodyAngle3) * TailJointLength3);
            float zTail3 = (float)(Math.Sin(BodyDirectionRad + TailToBodyAngle3) * TailJointLength3);
            // 尾部中心点
            xna.Vector3 pointReal21 = new xna.Vector3((PositionMm.X - xBodyHalf),
                0, (PositionMm.Z - zBodyHalf));
            xna.Vector3 pointReal22 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail),
                0, (PositionMm.Z - zBodyHalf - zTail));
            xna.Vector3 pointReal23 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2),
                0, (PositionMm.Z - zBodyHalf - zTail - zTail2));
            xna.Vector3 pointReal24 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3),
                0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3));

            // 定义角度          
            float angleCenter2Uptail1 = (float)(TailToBodyAngle1 + BodyDirectionRad - (float)Math.PI * 0.5);
            float angleCenter2Uptail2 = (float)(TailToBodyAngle2 + BodyDirectionRad - (float)Math.PI * 0.5);
            float angleCenter2Uptail3 = (float)(TailToBodyAngle3 + BodyDirectionRad - (float)Math.PI * 0.5);
            float CosThetaangleCenter2Uptail1 = (float)Math.Cos(angleCenter2Uptail1);
            float SinThetaangleCenter2Uptail1 = (float)Math.Sin(angleCenter2Uptail1);
            float CosThetaangleCenter2Uptail2 = (float)Math.Cos(angleCenter2Uptail2);
            float SinThetaangleCenter2Uptail2 = (float)Math.Sin(angleCenter2Uptail2);
            float CosThetaangleCenter2Uptail3 = (float)Math.Cos(angleCenter2Uptail3);
            float SinThetaangleCenter2Uptail3 = (float)Math.Sin(angleCenter2Uptail3);

            // 定义梯形上下底边半径
            float halfwidthFishTail1TX1 = TailJointLength1 * 0.29568f * 0.9f;
            float halfwidthFishTail1TX2 = TailJointLength1 * 0.274176f * 0.8f;
            float halfwidthFishTail2TX1 = TailJointLength2 * 0.25704f * 0.9f;
            float halfwidthFishTail2TX2 = TailJointLength2 * 0.2142f * 0.7f;
            float halfwidthFishTail3TX1 = TailJointLength3 * 0.12852f * 0.9f;
            float halfwidthFishTail3TX2 = TailJointLength3 * 0.12852f * 0.7f;

            // 定义梯形的12个点 412和411对称 中间原理还需要认真理解透.
            xna.Vector3 pointReal411 = new xna.Vector3((pointReal21.X + halfwidthFishTail1TX1 * CosThetaangleCenter2Uptail1),
                0, (pointReal21.Z + halfwidthFishTail1TX1 * SinThetaangleCenter2Uptail1));
            xna.Vector3 pointReal412 = new xna.Vector3((pointReal21.X - halfwidthFishTail1TX1 * CosThetaangleCenter2Uptail1),
                0, (pointReal21.Z - halfwidthFishTail1TX1 * SinThetaangleCenter2Uptail1));
            xna.Vector3 pointReal413 = new xna.Vector3((pointReal22.X + halfwidthFishTail1TX2 * CosThetaangleCenter2Uptail1),
                0, (pointReal22.Z + halfwidthFishTail1TX2 * SinThetaangleCenter2Uptail1));
            xna.Vector3 pointReal414 = new xna.Vector3((pointReal22.X - halfwidthFishTail1TX2 * CosThetaangleCenter2Uptail1),
                0, (pointReal22.Z - halfwidthFishTail1TX2 * SinThetaangleCenter2Uptail1));

            xna.Vector3 pointReal421 = new xna.Vector3((pointReal22.X + halfwidthFishTail2TX1 * CosThetaangleCenter2Uptail2),
                0, (pointReal22.Z + halfwidthFishTail2TX1 * SinThetaangleCenter2Uptail2));
            xna.Vector3 pointReal422 = new xna.Vector3((pointReal22.X - halfwidthFishTail2TX1 * CosThetaangleCenter2Uptail2),
                0, (pointReal22.Z - halfwidthFishTail2TX1 * SinThetaangleCenter2Uptail2));
            xna.Vector3 pointReal423 = new xna.Vector3((pointReal23.X + halfwidthFishTail2TX2 * CosThetaangleCenter2Uptail2),
                0, (pointReal23.Z + halfwidthFishTail2TX2 * SinThetaangleCenter2Uptail2));
            xna.Vector3 pointReal424 = new xna.Vector3((pointReal23.X - halfwidthFishTail2TX2 * CosThetaangleCenter2Uptail2),
                0, (pointReal23.Z - halfwidthFishTail2TX2 * SinThetaangleCenter2Uptail2));

            xna.Vector3 pointReal431 = new xna.Vector3((pointReal23.X + halfwidthFishTail3TX1 * CosThetaangleCenter2Uptail3),
                0, (pointReal23.Z + halfwidthFishTail3TX1 * SinThetaangleCenter2Uptail3));
            xna.Vector3 pointReal432 = new xna.Vector3((pointReal23.X - halfwidthFishTail3TX1 * CosThetaangleCenter2Uptail3),
                0, (pointReal23.Z - halfwidthFishTail3TX1 * SinThetaangleCenter2Uptail3));
            xna.Vector3 pointReal433 = new xna.Vector3((pointReal24.X + halfwidthFishTail3TX2 * CosThetaangleCenter2Uptail3),
                0, (pointReal24.Z + halfwidthFishTail3TX2 * SinThetaangleCenter2Uptail3));
            xna.Vector3 pointReal434 = new xna.Vector3((pointReal24.X - halfwidthFishTail3TX2 * CosThetaangleCenter2Uptail3),
                0, (pointReal24.Z - halfwidthFishTail3TX2 * SinThetaangleCenter2Uptail3));


            #endregion
            #region  利用计算得到的各点位，绘制机器鱼 created by chenxiao 20111208

            //鱼头最前点
            xna.Vector3 pointAhead = new xna.Vector3((pointReal2.X + HeadLength * 2 * halfCosTheta), 0, (pointReal2.Z + HeadLength * 2 * halfSinTheta));
            //	头部为一弧形曲线，由三点确定
            Point[] head = {
                              Vector3ToPoint(f.MmToPix(pointReal3)),
                              Vector3ToPoint(f.MmToPix(pointAhead)),
                              Vector3ToPoint(f.MmToPix(pointReal1))
                           };
            //鱼尾的左侧体线
            Point[] leftTail = {
                              Vector3ToPoint(f.MmToPix(pointReal7)),
                              Vector3ToPoint(f.MmToPix(pointReal411)),
                              Vector3ToPoint(f.MmToPix(pointReal413)),
                              Vector3ToPoint(f.MmToPix(pointReal421)),
                              Vector3ToPoint(f.MmToPix(pointReal423)),
                              Vector3ToPoint(f.MmToPix(pointReal431)),
                              Vector3ToPoint(f.MmToPix(pointReal433))
                           };
            //鱼尾的右侧体线
            Point[] rightTail = {
                              Vector3ToPoint(f.MmToPix(pointReal434)),
                              Vector3ToPoint(f.MmToPix(pointReal432)),
                              Vector3ToPoint(f.MmToPix(pointReal424)),
                              Vector3ToPoint(f.MmToPix(pointReal422)),
                              Vector3ToPoint(f.MmToPix(pointReal414)),
                              Vector3ToPoint(f.MmToPix(pointReal412)),
                              Vector3ToPoint(f.MmToPix(pointReal5))
                           };
            #region 生成尾鳍
            //尾鳍轴线上的四个定位点
            xna.Vector3 tailAxis1 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3 * 1.3f), 0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3 * 1.3f));
            xna.Vector3 tailAxis2 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3 * 1.6f), 0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3 * 1.6f));
            xna.Vector3 tailAxis3 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3 * 1.8f), 0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3 * 1.8f));
            xna.Vector3 tailAxis4 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - xTail3 * 2.0f), 0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - zTail3 * 2.0f));
            //定义尾鳍凹弧的中点
            xna.Vector3 tailFinMiddle = tailAxis2;
            //定义染色的最外变化点
            xna.Vector3 tailEnd = tailAxis4;
            //定义四个长度，表示与轴线的垂直距离，分别与四个轴线定位点对应
            float distanceToAxis1 = 55;
            float distanceToAxis2 = 85;
            float distanceToAxis3 = 70;
            float distanceToAxis4 = 100;
            //尾鳍轴心的角度
            float tailAngle = (BodyDirectionRad + TailToBodyAngle3);
            //当角度<-π,或者>π.进行处理使之范围处于-π~π
            if (tailAngle < -Math.PI)
            {
                tailAngle = tailAngle + (float)Math.PI * 2;
            }
            else if (tailAngle > Math.PI)
            {
                tailAngle = tailAngle - (float)Math.PI * 2;
            }
            //缩放参数
            float scalingParameter = (float)Math.Abs(Math.Cos(tailAngle));
            float halfSinTailAngle = (float)Math.Sin(tailAngle) / 2 * scalingParameter;//缩放后的sin
            float halfCosTailAngle = (float)Math.Cos(tailAngle) / 2 * scalingParameter;//缩放后的cos
            //镜像参数设定，如朝右的鱼左顶点偏离轴线大，朝左的鱼右顶点偏离轴线大,简单讲就是1,2对调，3,4对调
            if (Math.Abs(tailAngle) > Math.PI / 2)
            {
                distanceToAxis1 = 85;
                distanceToAxis2 = 55;
                distanceToAxis3 = 100;
                distanceToAxis4 = 70;
                xna.Vector3 temp = tailAxis2;
                tailAxis2 = tailAxis1;
                tailAxis1 = temp;
                temp = tailAxis3;
                tailAxis3 = tailAxis4;
                tailAxis4 = temp;
            }
            //尾鳍上顶点
            xna.Vector3 tailFinLeft = new xna.Vector3(tailAxis4.X + distanceToAxis4 * halfSinTailAngle, 0, tailAxis4.Z - distanceToAxis4 * halfCosTailAngle);
            //尾鳍下顶点
            xna.Vector3 tailFinRight = new xna.Vector3(tailAxis3.X - distanceToAxis3 * halfSinTailAngle, 0, tailAxis3.Z + distanceToAxis3 * halfCosTailAngle);
            //尾鳍上侧弧线的中点
            xna.Vector3 tailFinLeftMiddle = new xna.Vector3(tailAxis2.X + distanceToAxis2 * halfSinTailAngle, 0, tailAxis2.Z - distanceToAxis2 * halfCosTailAngle);
            //尾鳍下侧弧线的中点
            xna.Vector3 tailFinRightMiddle = new xna.Vector3(tailAxis1.X - distanceToAxis1 * halfSinTailAngle, 0, tailAxis1.Z + distanceToAxis1 * halfCosTailAngle);


            Point[] tailFinLeftArc = {
                                         Vector3ToPoint(f.MmToPix(pointReal433)),
                                         Vector3ToPoint(f.MmToPix(tailFinLeftMiddle)),
                                         Vector3ToPoint(f.MmToPix(tailFinLeft))
                                     };
            Point[] tailFinBackArc = {
                                         Vector3ToPoint(f.MmToPix(tailFinLeft)),
                                         Vector3ToPoint(f.MmToPix(tailFinMiddle)),
                                         Vector3ToPoint(f.MmToPix(tailFinRight))
                                     };

            Point[] tailFinRightArc = {
                                         Vector3ToPoint(f.MmToPix(tailFinRight)),
                                         Vector3ToPoint(f.MmToPix(tailFinRightMiddle)),
                                         Vector3ToPoint(f.MmToPix(pointReal434))
                                     };


            #endregion
            #region 将鱼体作为一个大的闭包的梭形path路径，逆时针依次画出鱼体体线，这里的path不含鱼鳍
            GraphicsPath pathBody = new GraphicsPath();

            //添加鱼头
            pathBody.AddCurve(head, 1f);

            //添加鱼体矩形的左侧
            pathBody.AddLine(Vector3ToPoint(f.MmToPix(pointReal1)), Vector3ToPoint(f.MmToPix(pointReal7)));

            // 添加鱼尾左侧体线
            pathBody.AddBeziers(leftTail);

            //添加鱼尾右侧体线
            pathBody.AddBeziers(rightTail);
            //添加鱼体矩形的右侧
            pathBody.AddLine(Vector3ToPoint(f.MmToPix(pointReal5)), Vector3ToPoint(f.MmToPix(pointReal3)));
            #endregion
            #region 尾鳍绘制
            GraphicsPath pathTailFin = new GraphicsPath();
            //添加尾鳍上侧弧线
            pathTailFin.AddCurve(tailFinLeftArc, 0.5f);
            //添加尾鳍后侧弧线
            pathTailFin.AddCurve(tailFinBackArc);
            //添加尾鳍下侧弧线
            pathTailFin.AddCurve(tailFinRightArc, 0.5f);
            #endregion

            #region 绘制左右胸鳍
            //为消除鱼鳍和鱼体的缝隙，使鱼鳍的绘制点向鱼体内缩4mm
            xna.Vector3 pointLeftFinOrigin = new xna.Vector3((pointReal11.X - 4 * halfSinTheta), 0, (pointReal11.Z + 4 * halfCosTheta));
            xna.Vector3 pointLeftFinEnd = new xna.Vector3((pointReal14.X - 4 * halfSinTheta), 0, (pointReal14.Z + 4 * halfCosTheta));
            xna.Vector3 pointRightFinOrigin = new xna.Vector3((pointReal12.X + 4 * halfSinTheta), 0, (pointReal12.Z - 4 * halfCosTheta));
            xna.Vector3 pointRightFinEnd = new xna.Vector3((pointReal13.X + 4 * halfSinTheta), 0, (pointReal13.Z - 4 * halfCosTheta));
            //生成新的路径，用来保存左侧鱼鳍
            GraphicsPath pathLeftFin = new GraphicsPath();
            //计算绘制左侧鱼鳍的bezier曲线的两个控制点
            xna.Vector3 leftFinControl1 = new xna.Vector3((pointReal14.X + 100 * halfSinTheta), 0,
                (pointReal14.Z - 100 * halfCosTheta));
            xna.Vector3 leftFinControl2 = new xna.Vector3((pointReal7.X + 100 * halfSinTheta / 2), 0,
    (pointReal7.Z - 100 * halfCosTheta / 2));

            //添加左侧鱼鳍
            pathLeftFin.AddBezier(Vector3ToPoint(f.MmToPix(pointLeftFinOrigin)),
                              Vector3ToPoint(f.MmToPix(leftFinControl1)),
                              Vector3ToPoint(f.MmToPix(leftFinControl2)),
                              Vector3ToPoint(f.MmToPix(pointLeftFinEnd)));

            //生成新的路径，用来保存右侧鱼鳍
            GraphicsPath pathRightFin = new GraphicsPath();
            //计算绘制右侧鱼鳍的bezier曲线的两个控制点
            xna.Vector3 rightFinControl1 = new xna.Vector3((pointReal13.X - 100 * halfSinTheta), 0,
              (pointReal13.Z + 100 * halfCosTheta));
            xna.Vector3 rightFinControl2 = new xna.Vector3((pointReal5.X - 100 * halfSinTheta / 2), 0,
              (pointReal5.Z + 100 * halfCosTheta / 2));
            //添加右侧鱼鳍
            pathRightFin.AddBezier(Vector3ToPoint(f.MmToPix(pointRightFinOrigin)),
                              Vector3ToPoint(f.MmToPix(rightFinControl1)),
                              Vector3ToPoint(f.MmToPix(rightFinControl2)),
                              Vector3ToPoint(f.MmToPix(pointRightFinEnd)));

            #endregion
            //生成颜色渐变的画刷
            //LinearGradientBrush brush = new LinearGradientBrush(Vector3ToPoint(f.MmToPix(pointReal8)), Vector3ToPoint(f.MmToPix(pointReal4)), ColorId, Color.White);
            //brush.SetBlendTriangularShape(0.5f, 0.4f);
            //g.FillPath(brush, path);


            PathGradientBrush brushBody = null
;

            //try
            //{
            brushBody = new PathGradientBrush(pathBody);
            brushBody.CenterPoint = Vector3ToPoint(f.MmToPix(PositionMm));
            brushBody.CenterColor = Color.Black;
            brushBody.SetBlendTriangularShape(1f, 0.46f);
            brushBody.SurroundColors = new Color[] { ColorFish };
            g.FillPath(brushBody, pathBody);
            //}
            //catch 
            //{
            //}

            LinearGradientBrush brushFin = new LinearGradientBrush(Vector3ToPoint(f.MmToPix(leftFinControl2)), Vector3ToPoint(f.MmToPix(pointReal11)), ColorFish, Color.White);
            brushFin.SetBlendTriangularShape(0.5f, 0.5f);
            g.FillPath(brushFin, pathLeftFin);
            g.FillPath(brushFin, pathRightFin);
            //为防止尾鳍和鱼身之间产生染色裂隙，将染色点内调到point24前端
            xna.Vector3 pointNear24 = new xna.Vector3((PositionMm.X - xBodyHalf - xTail - xTail2 - (float)0.9 * xTail3),
                                                        0, (PositionMm.Z - zBodyHalf - zTail - zTail2 - (float)0.9 * zTail3));
            brushFin = new LinearGradientBrush(Vector3ToPoint(f.MmToPix(tailEnd)), Vector3ToPoint(f.MmToPix(pointNear24)), Color.White, ColorFish);
            brushFin.SetBlendTriangularShape(1f, 1f);

            g.FillPath(brushFin, pathTailFin);


            #region 测试区域
            //测试用画笔
            Pen mypen = new Pen(Color.Red, 1f);
            // g.DrawCurve(mypen, tailFinLeftArc);
            //g.DrawPath(mypen, pathTailFin);
            //g.DrawPath(mypen, myPathLeft);
            //g.DrawPath(mypen, myPathRight);
            //g.DrawPath(mypen, path);

            //g.DrawLine(mypen, new Point((int)pointReal2.X, (int)pointReal2.Z),new Point(100,100));
            // g.DrawPath(mypen,path);
            // g.DrawPath(mypen, path);
            #endregion
            brushBody.Dispose();
            brushFin.Dispose();
            pathBody.Dispose();
            pathLeftFin.Dispose();
            pathRightFin.Dispose();
            mypen.Dispose();

            #endregion


            // created by chenwei 20110603
            // modified by LiYoubing 20110607
            #region 绘制鱼体编号
            //先计算绘制目标区域点的位置，这个需要通过鱼体的方向角度来计算
            Font drawFontID = new Font("黑体", 18 - (int)Field.Instance().ScaleMmToPix);
            SolidBrush drawBrushID = new SolidBrush(ColorId);
            //if (BodyDirectionRad > - xna.MathHelper.PiOver2 && BodyDirectionRad < xna.MathHelper.PiOver2)
            Point drawPointID = Vector3ToPoint(f.MmToPix(PositionMm));
            drawPointID = new Point(drawPointID.X - 10, drawPointID.Y - 10);
            g.DrawString(fishId.ToString(), drawFontID, drawBrushID, drawPointID);

            #endregion

            #region 碰撞检测参数更新 modified by renjing 20110414/20111214/20120207
            //// 实时更新仿真机器鱼碰撞检测内层四边形模型四个顶点坐标
            //PolygonVertices[0] = new xna.Vector3((float)(pointReal2.X + BodyWidth * halfCosTheta*2f),
            //    0, (float)(pointReal2.Z + BodyWidth * halfSinTheta*2f));   // 鱼体内层碰撞模型头部顶点,比绘制的鱼头范围大些
            //PolygonVertices[1] = pointReal15;   // 鱼体内层碰撞模型右胸鳍顶点
            //PolygonVertices[2] = pointReal433;  // 鱼体内层碰撞模型尾部顶点
            //PolygonVertices[3] = pointReal16;   // 鱼体内层碰撞模型左胸鳍顶点

            ////获得包围左胸鳍的矩形added by chenxiao
            //RectangleF recLeftFin = pathLeftFin.GetBounds();
            ////获得包围左胸鳍的矩形added by chenxiao
            //RectangleF recRightFin = pathRightFin.GetBounds();


            //鱼体碰撞模型BV树左胸鳍叶结点
            //LeftPectoralPolygonVertices[0] = pointReal16;
            LeftPectoralPolygonVertices[0] = (leftFinControl1 + leftFinControl2) / 2;//改为两控制点的中点modified by chenxiao
            LeftPectoralPolygonVertices[1] = new xna.Vector3((pointReal14.X + leftFinControl2.X) / 2, 0, (pointReal14.Z + leftFinControl1.Z) / 2);
            LeftPectoralPolygonVertices[2] = pointReal14;
            LeftPectoralPolygonVertices[3] = pointReal11;

            //鱼体碰撞模型BV树右胸鳍叶结点
            RightPectoralPolygonVertices[0] = (rightFinControl1 + rightFinControl2) / 2; //改为两控制点的中点modified by chenxiao
            RightPectoralPolygonVertices[1] = new xna.Vector3((pointReal13.X + rightFinControl2.X) / 2, 0, (pointReal13.Z + rightFinControl1.Z) / 2);
            RightPectoralPolygonVertices[2] = pointReal13;
            RightPectoralPolygonVertices[3] = pointReal12;

            //鱼体碰撞模型BV树鱼体躯干部分叶结点
            BodyPolygonVertices[0] = pointReal1;
            //BodyPolygonVertices[1] = new xna.Vector3((float)(pointReal2.X + BodyWidth * halfCosTheta),
            //    0, (float)(pointReal2.Z + BodyWidth * halfSinTheta));
            BodyPolygonVertices[1] = pointAhead;//改为弧线的顶点 modified by chenxiao
            BodyPolygonVertices[2] = pointReal3;
            BodyPolygonVertices[3] = pointReal5;
            BodyPolygonVertices[4] = pointReal7;

            //鱼体碰撞模型BV树尾部第一关节叶结点
            Tail1PolygonVertices[0] = pointReal412;
            Tail1PolygonVertices[1] = pointReal411;
            Tail1PolygonVertices[2] = pointReal413;
            Tail1PolygonVertices[3] = pointReal414;

            //鱼体碰撞模型BV树尾部第二关节叶结点
            Tail2PolygonVertices[0] = pointReal422;
            Tail2PolygonVertices[1] = pointReal421;
            Tail2PolygonVertices[2] = pointReal423;
            Tail2PolygonVertices[3] = pointReal424;

            //鱼体碰撞模型BV树尾部第三关节叶结点
            Tail3PolygonVertices[0] = pointReal432;
            Tail3PolygonVertices[1] = pointReal431;
            Tail3PolygonVertices[2] = pointReal433;
            Tail3PolygonVertices[3] = pointReal434;
            //Tail3PolygonVertices[2] = tailFinLeft;//改为两控制点的中点 modified by chenxiao 
            //Tail3PolygonVertices[3] = tailFinRight;//改为两控制点的中点 modified by chenxiao

            //鱼体碰撞模型BV树尾鳍部分为两个三角形，左半三角形
            LeftCaudalFinVertices[0] = tailFinLeft;
            LeftCaudalFinVertices[1] = tailFinMiddle;
            LeftCaudalFinVertices[2] = pointReal434;

            //鱼体碰撞模型BV树尾鳍部分为两个三角形，右半三角形
            RightCaudalFinVertices[0] = tailFinRight;
            RightCaudalFinVertices[1] = tailFinMiddle;
            RightCaudalFinVertices[2] = pointReal433;


            // 实时更新仿真机器鱼碰撞模型BV树根结点
            CollisionModelCenterPositionMm = new xna.Vector3(
                (float)(BodyPolygonVertices[1].X - CollisionModelRadiusMm * 2 * halfCosTheta), 0,
                (float)(BodyPolygonVertices[1].Z - CollisionModelRadiusMm * 2 * halfSinTheta));

            // 实时更新仿真机器鱼碰撞模型BV树第二层鱼刚体部分子结点
            CollisionModelBodyCenterPositionMm = new xna.Vector3(
                (float)(BodyPolygonVertices[1].X - CollisionModelBodyRadiusMm * 2 * halfCosTheta), 0,
                (float)(BodyPolygonVertices[1].Z - CollisionModelBodyRadiusMm * 2 * halfSinTheta));

            // 实时更新仿真机器鱼碰撞模型BV树第二层鱼尾部子结点
            CollisionModelTailCenterPositionMm = (pointReal23 - pointReal22) / 3 + pointReal22;
            //CollisionModelTailCenterPositionMm = pointReal22;




            // 实时更新仿真机器鱼碰撞检测边界点列表
            PolygonVertices[0] = BodyPolygonVertices[1];   // 鱼体头部顶点
            PolygonVertices[1] = BodyPolygonVertices[0]; ;   //鱼体头部左边点
            PolygonVertices[2] = LeftPectoralPolygonVertices[0];  // 鱼体左胸鳍顶点
            PolygonVertices[3] = LeftCaudalFinVertices[0];//尾鳍左顶点
            PolygonVertices[4] = RightCaudalFinVertices[0];//尾鳍右顶点
            //PolygonVertices[3] = Tail3PolygonVertices[3];   // 鱼体尾部第三关节左下点
            //PolygonVertices[4] = Tail3PolygonVertices[2];//鱼体尾部第三关节右下点
            PolygonVertices[5] = RightPectoralPolygonVertices[0];// 鱼体右胸鳍顶点
            PolygonVertices[6] = BodyPolygonVertices[2]; //鱼体头部右边点


            #endregion

            //if (flag == -1) RecoveryPose();
            //PositionMm = tmpVector3;
            //BodyDirectionRad = tmp; // 恢复计算用的鱼体方向值
            #endregion
        }

        xna.Vector3 _tmpPositionMm;
        float _tmpBodyDirectionRad;

        /// <summary>
        /// 对调所处半场，即将位置坐标和鱼体方向取反
        /// 用于仿真机器鱼所属队伍位于右半场时绘制/碰撞检测/向客户端或策略传递参数前的处理
        /// </summary>
        public void ReversePose()
        {
            _tmpPositionMm = PositionMm;
            PositionMm = -PositionMm;
            _tmpBodyDirectionRad = BodyDirectionRad;
            BodyDirectionRad += xna.MathHelper.Pi;
            BodyDirectionRad = xna.MathHelper.WrapAngle(BodyDirectionRad);
        }

        /// <summary>
        /// 恢复所处半场
        /// 用于仿真机器鱼所属队伍位于右半场时绘制/碰撞检测/向客户端或策略传递参数后的处理
        /// </summary>
        public void RecoveryPose()
        {
            PositionMm = _tmpPositionMm;
            BodyDirectionRad = _tmpBodyDirectionRad;
        }

        /// <summary>
        /// 将三维Vector3点转换成二维Point点，取X和Z维
        /// </summary>
        /// <param name="vector3">三维Vector3点</param>
        /// <returns>二维Point点</returns>
        private Point Vector3ToPoint(xna.Vector3 vector3)
        {
            return new Point((int)vector3.X, (int)vector3.Z);
        }

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// modified by renjing 20120330 增加了碰撞检测的几个参数拷贝
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public virtual void CopyTo(IDssSerializable target)
        {
            RoboFish typedTarget = target as RoboFish;

            typedTarget.PositionMm = this.PositionMm;
            typedTarget.BodyDirectionRad = this.BodyDirectionRad;
            typedTarget.VelocityMmPs = this.VelocityMmPs;
            typedTarget.VelocityDirectionRad = this.VelocityDirectionRad;
            typedTarget.AngularVelocityRadPs = this.AngularVelocityRadPs;

            typedTarget.CollisionModelRadiusMm = this.CollisionModelRadiusMm;
            typedTarget.CollisionModelBodyRadiusMm = this.CollisionModelBodyRadiusMm;
            typedTarget.CollisionModelTailRadiusMm = this.CollisionModelTailRadiusMm;
            typedTarget.BodyWidth = this.BodyWidth;
            typedTarget.BodyLength = this.BodyLength;
            typedTarget.CollisionModelCenterPositionMm = this.CollisionModelCenterPositionMm;
            typedTarget.CollisionModelBodyCenterPositionMm = this.CollisionModelBodyCenterPositionMm;
            typedTarget.CollisionModelTailCenterPositionMm = this.CollisionModelTailCenterPositionMm;
            typedTarget.TailToBodyAngle1 = this.TailToBodyAngle1;
            typedTarget.TailToBodyAngle2 = this.TailToBodyAngle2;
            typedTarget.TailToBodyAngle3 = this.TailToBodyAngle3;

            for (int i = 0; i < 7; i++)
            {
                typedTarget.PolygonVertices[i] = this.PolygonVertices[i];
            }
            for (int i = 0; i < 5; i++)
            {
                typedTarget.BodyPolygonVertices[i] = this.BodyPolygonVertices[i];
            }
            for (int i = 0; i < 4; i++)
            {
                typedTarget.Tail1PolygonVertices[i] = this.Tail1PolygonVertices[i];
                typedTarget.Tail2PolygonVertices[i] = this.Tail2PolygonVertices[i];
                typedTarget.Tail3PolygonVertices[i] = this.Tail3PolygonVertices[i];
                typedTarget.LeftPectoralPolygonVertices[i] = this.LeftPectoralPolygonVertices[i];
                typedTarget.RightPectoralPolygonVertices[i] = this.RightPectoralPolygonVertices[i];
            }
            for (int i = 0; i < 3; i++)
            {
                typedTarget.LeftCaudalFinVertices[i] = this.LeftCaudalFinVertices[i];
                typedTarget.RightCaudalFinVertices[i] = this.RightCaudalFinVertices[i];
            }

            foreach (DictionaryEntry de in this.HtFishVariables)
            {// 遍历哈希表为目标对象重构哈希表
                typedTarget.HtFishVariables.Add(de.Key, de.Value);
            }
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public virtual object Clone()
        {
            RoboFish target = new RoboFish();

            target.PositionMm = this.PositionMm;
            target.BodyDirectionRad = this.BodyDirectionRad;
            target.VelocityMmPs = this.VelocityMmPs;
            target.VelocityDirectionRad = this.VelocityDirectionRad;
            target.AngularVelocityRadPs = this.AngularVelocityRadPs;

            target.CollisionModelRadiusMm = this.CollisionModelRadiusMm;
            target.CollisionModelBodyRadiusMm = this.CollisionModelBodyRadiusMm;
            target.CollisionModelTailRadiusMm = this.CollisionModelTailRadiusMm;
            target.BodyWidth = this.BodyWidth;
            target.BodyLength = this.BodyLength;
            target.CollisionModelCenterPositionMm = this.CollisionModelCenterPositionMm;
            target.CollisionModelBodyCenterPositionMm = this.CollisionModelBodyCenterPositionMm;
            target.CollisionModelTailCenterPositionMm = this.CollisionModelTailCenterPositionMm;
            target.TailToBodyAngle1 = this.TailToBodyAngle1;
            target.TailToBodyAngle2 = this.TailToBodyAngle2;
            target.TailToBodyAngle3 = this.TailToBodyAngle3;


            //   target.PolygonVertices[0] = this.PolygonVertices[0];

            for (int i = 0; i < 7; i++)
            {
                target.PolygonVertices[i] = this.PolygonVertices[i];
            }
            for (int i = 0; i < 5; i++)
            {
                target.BodyPolygonVertices[i] = this.BodyPolygonVertices[i];
            }
            for (int i = 0; i < 4; i++)
            {
                target.Tail1PolygonVertices[i] = this.Tail1PolygonVertices[i];
                target.Tail2PolygonVertices[i] = this.Tail2PolygonVertices[i];
                target.Tail3PolygonVertices[i] = this.Tail3PolygonVertices[i];
                target.LeftPectoralPolygonVertices[i] = this.LeftPectoralPolygonVertices[i];
                target.RightPectoralPolygonVertices[i] = this.RightPectoralPolygonVertices[i];
            }
            for (int i = 0; i < 3; i++)
            {
                target.LeftCaudalFinVertices[i] = this.LeftCaudalFinVertices[i];
                target.RightCaudalFinVertices[i] = this.RightCaudalFinVertices[i];
            }

            foreach (DictionaryEntry de in this.HtFishVariables)
            {// 遍历哈希表为目标对象重构哈希表
                target.HtFishVariables.Add(de.Key, de.Value);
            }
            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public virtual void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(PositionMm.X);
            writer.Write(PositionMm.Y);
            writer.Write(PositionMm.Z);
            writer.Write(BodyDirectionRad);
            writer.Write(VelocityMmPs);
            writer.Write(VelocityDirectionRad);
            writer.Write(AngularVelocityRadPs);

            writer.Write(CollisionModelRadiusMm);
            writer.Write(CollisionModelBodyRadiusMm);
            writer.Write(CollisionModelTailRadiusMm);
            writer.Write(BodyWidth);
            writer.Write(BodyLength);

            writer.Write(PolygonVertices[0].X);
            writer.Write(PolygonVertices[0].Y);
            writer.Write(PolygonVertices[0].Z);

            // 先将Hashtable的元素个数序列化供反序列化时重构Hashtable用
            writer.Write(HtFishVariables.Count);
            foreach (DictionaryEntry de in this.HtFishVariables)
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
            PositionMm.X = reader.ReadSingle();
            PositionMm.Y = reader.ReadSingle();
            PositionMm.Z = reader.ReadSingle();
            BodyDirectionRad = reader.ReadSingle();
            VelocityMmPs = reader.ReadSingle();
            VelocityDirectionRad = reader.ReadSingle();
            AngularVelocityRadPs = reader.ReadSingle();

            CollisionModelRadiusMm = reader.ReadInt32();
            CollisionModelBodyRadiusMm = reader.ReadInt32();
            CollisionModelTailRadiusMm = reader.ReadInt32();
            BodyWidth = reader.ReadInt32();
            BodyLength = reader.ReadInt32();

            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            PolygonVertices[0] = new xna.Vector3(x, y, z);

            // 反序列化Hashtable元素个数
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {// 反序列化键值对重构Hashtable
                string key = reader.ReadString();
                string value = reader.ReadString();
                this.HtFishVariables.Add(key, value);
            }

            return this;
        }
        #endregion
    }

    /// <summary>
    /// 仿真机器鱼运动决策数据结构体
    /// </summary>
    [DataContract]
    [Serializable]
    public struct Decision
    {
        /// <summary>
        /// 决策结果的速度档位，取值范围{0,1,...,15}，0最慢，15最快
        /// </summary>
        [DataMember]
        public int VCode;

        /// <summary>
        /// 决策结果的转弯档位，取值范围{0,1,...,15}，0左转最急，7直游，15右转最急
        /// </summary>
        [DataMember]
        public int TCode;
    }

    /// <summary>
    /// 仿真进程中可能发生的对象间碰撞类型枚举
    /// </summary>
    [DataContract]
    public enum CollisionType
    {
        /// <summary>
        /// 无碰撞
        /// </summary>
        NONE = 0,

        /// <summary>
        /// 仿真机器鱼之间碰撞
        /// </summary>
        FISH_FISH,

        /// <summary>
        /// 仿真机器鱼和仿真水球碰撞
        /// </summary>
        FISH_BALL,

        /// <summary>
        /// 仿真机器鱼和仿真水池池壁碰撞
        /// </summary>
        FISH_POOL,

        /// <summary>
        /// 仿真机器鱼和仿真静态障碍物碰撞
        /// </summary>
        FISH_OBSTACLE,

        /// <summary>
        /// 仿真机器鱼和仿真通道碰撞
        /// </summary>
        FISH_CHANNEL,

        /// <summary>
        /// 仿真水球和仿真水池池壁碰撞
        /// </summary>
        BALL_POOL,

        /// <summary>
        /// 仿真水球和仿真静态障碍物碰撞
        /// </summary>
        BALL_OBSTACLE,

        /// <summary>
        /// 仿真水球和仿真通道碰撞
        /// </summary>
        BALL_CHANNEL
    }

    /// <summary>
    /// 仿真比赛队伍数据类
    /// </summary>
    [DataContract]
    [Serializable]
    public class Team<TFish>
    {
        /// <summary>
        ///  仿真比赛队伍公共参数
        /// </summary>
        [DataMember]
        public TeamCommonPara Para = new TeamCommonPara();

        /// <summary>
        ///  仿真比赛队伍中仿真机器鱼列表
        /// </summary>
        [DataMember]
        public List<TFish> Fishes = new List<TFish>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public Team()
        {
            Para.MyHalfCourt = HalfCourt.LEFT;  // 默认在左半场
        }
    }

    /// <summary>
    /// 仿真机器鱼队伍公共参数类
    /// </summary>
    [Serializable]
    public class TeamCommonPara : ICloneable, IDssSerializable
    {
        /// <summary>
        /// 仿真比赛队伍中仿真机器鱼数量
        /// </summary>
        [DataMember]
        public int FishCount;

        /// <summary>
        /// 仿真比赛队伍中所处仿真场地半场（左或右）
        /// </summary>
        [DataMember]
        public HalfCourt MyHalfCourt;

        /// <summary>
        /// 仿真比赛队伍中当前得分
        /// </summary>
        [DataMember]
        public int Score;

        /// <summary>
        /// 仿真比赛队伍名称
        /// </summary>
        [DataMember]
        public string Name;

        /// <summary>
        /// 仿真比赛队伍是否已经准备好，按下了Ready按钮
        /// </summary>
        public bool IsReady;

        ///// <summary>
        ///// 仿真比赛队伍所在客户端Client服务实例Uri
        ///// </summary>
        //public string Service;

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public virtual void CopyTo(IDssSerializable target)
        {
            TeamCommonPara typedTarget = target as TeamCommonPara;

            typedTarget.FishCount = this.FishCount;
            typedTarget.MyHalfCourt = this.MyHalfCourt;
            typedTarget.Score = this.Score;
            typedTarget.Name = this.Name;
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public virtual object Clone()
        {
            TeamCommonPara target = new TeamCommonPara();

            target.FishCount = this.FishCount;
            target.MyHalfCourt = this.MyHalfCourt;
            target.Score = this.Score;
            target.Name = this.Name;

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public virtual void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(FishCount);
            writer.Write((int)MyHalfCourt);
            writer.Write(Score);
            writer.Write(Name);
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public virtual object Deserialize(System.IO.BinaryReader reader)
        {
            FishCount = reader.ReadInt32();
            MyHalfCourt = (HalfCourt)(reader.ReadInt32());
            Score = reader.ReadInt32();
            Name = reader.ReadString();

            return this;
        }
        #endregion
    }

    /// <summary>
    /// 场地左右半场类型枚举
    /// </summary>
    [DataContract]
    [Serializable]
    public enum HalfCourt
    {
        /// <summary>
        /// 左半场
        /// </summary>
        LEFT = 0,

        /// <summary>
        /// 右半场
        /// </summary>
        RIGHT
    }
}