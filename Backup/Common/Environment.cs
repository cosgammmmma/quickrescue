//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Environment.cs
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真环境基类定义文件
// Histroy:
// Date: 20110511  Author: LiYoubing
// Modification: 
// 1.RectangularStatic.Draw中增加更新碰撞检测多边形模型顶点的过程
// Date: 20110630  Author: LiYoubing
// 1.用仿真场地绘制控件PictureBox的BackgroundImage属性承载仿真场地的背景图片
// 2.DrawHelper中调换圆形和方形对象绘制代码中将填充调换到绘制边框之前实现边框全部可见
// Date: 20110710  Author: LiYoubing
// 1.DrawRectangle中将矩形各顶点往左上偏移1像素消除绘制导致的视觉误差
// 2.修正Field的CopyTo/Clone/Serialize/Deserialize向策略传递几个原始尺寸参数
// Date: 20110810  Author: LiYoubing
// 1.FieldCalculation根据当前URWPGSim2DServer主窗口所在显示器进行计算
// Date: 20111101  Author: ZhangBo
// 1.增加两个布尔变量IsGoalBlockNeeded、IsFieldInnerLinesNeeded，根据具体项目判断是需要否绘制球门块，禁区线等。
// Date: 20120408  Author: ChenXiao
// 1.添加RectDynamic类
// 2.SimEnvironment中添加动态仿真障碍物列表DynamicRect
// 3.在SimEnvironment的CopyTo，clone，Serialize，deSerialize函数中添加动态障碍物的处理
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.CompilerServices; // for MethodImpl
using System.Windows.Forms;
using System.Drawing.Drawing2D;
//using pfm = System.Windows.Media;
//using pfs = System.Windows.Shapes;
using xna = Microsoft.Xna.Framework; // in Microsoft.Xna.Framework.dll for Vector3

using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;

using URWPGSim2D.Core;

namespace URWPGSim2D.Common
{
    /// <summary>
    /// 仿真环境基类，所有具体使命（比赛或实验项目）使用的环境类必须继承该类
    /// </summary>
    [DataContract]
    [Serializable]
    public class SimEnvironment : ISimEnvironment, ICloneable, IDssSerializable
    {
        #region 仿真环境中各种通用对象列表
        /// <summary>
        /// 仿真环境中仿真场地
        /// </summary>
        [DataMember]
        public Field FieldInfo = Field.Instance();

        /// <summary>
        /// 仿真环境中仿真水球列表
        /// </summary>
        [DataMember]
        public List<Ball> Balls = new List<Ball>();

        /// <summary>
        /// 仿真环境中仿真圆形障碍物列表
        /// </summary>
        [DataMember]
        public List<RoundedObstacle> ObstaclesRound = new List<RoundedObstacle>();

        /// <summary>
        /// 仿真环境中仿真方形障碍物列表
        /// </summary>
        [DataMember]
        public List<RectangularObstacle> ObstaclesRect = new List<RectangularObstacle>();

        /// <summary>
        /// 仿真环境中动态仿真方形障碍物列表
        /// </summary>
        [DataMember]
        public List<RectangularDynamic> DynamicRect = new List<RectangularDynamic>();

        /// <summary>
        /// 仿真环境中仿真通道列表
        /// </summary>
        [DataMember]
        public List<Channel> Channels = new List<Channel>();
        #endregion

        #region 具体仿真环境需要向策略传递的参数表
        // 增加一个Hashtable用于保存具体仿真环境需要给策略传递的参数值 LiYoubing 20110507
        /// <summary>
        /// 具体仿真环境类即<see cref="URWPGSim2D.Common.SimEnvironment">SimEnvironment</see>类的子类需要给策略传递的变量名和值对
        /// 使用哈希表<see cref="System.Collections.Hashtable">Hashtable</see>存储需要传递的键值对
        /// 键（变量名）和值（变量值）均用string类型表示
        /// </summary>
        public Hashtable HtEnvVariables = new Hashtable();
        #endregion

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递    modified 20101207
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public virtual void CopyTo(IDssSerializable target)
        {
            SimEnvironment typedTarget = target as SimEnvironment;

            this.FieldInfo.CopyTo(typedTarget.FieldInfo);
            //typedTarget.FieldInfo = this.FieldInfo;

            for (int i = 0; i < this.Balls.Count; i++)
            {
                typedTarget.Balls.Add(new Ball());

                // 将当前对象第i个仿真水球的公共参数CopyTo目标对象
                this.Balls[i].CopyTo(typedTarget.Balls[i]);
            }

            for (int i = 0; i < this.ObstaclesRound.Count; i++)
            {
                typedTarget.ObstaclesRound.Add(new RoundedObstacle());

                // 将当前对象第i个仿真圆形障碍物的公共参数CopyTo目标对象
                this.ObstaclesRound[i].CopyTo(typedTarget.ObstaclesRound[i]);
            }

            for (int i = 0; i < this.ObstaclesRect.Count; i++)
            {
                typedTarget.ObstaclesRect.Add(new RectangularObstacle());

                // 将当前对象第i个仿真方形障碍物的公共参数CopyTo目标对象
                this.ObstaclesRect[i].CopyTo(typedTarget.ObstaclesRect[i]);
            }

            for (int i = 0; i < this.DynamicRect.Count; i++)
            {
                typedTarget.DynamicRect.Add(new RectangularDynamic());

                // 将当前对象第i个动态仿真方形障碍物的公共参数CopyTo目标对象
                this.DynamicRect[i].CopyTo(typedTarget.DynamicRect[i]);
            }

            for (int i = 0; i < this.Channels.Count; i++)
            {
                typedTarget.Channels.Add(new Channel());

                // 将当前对象第i个仿真方形障碍物的公共参数CopyTo目标对象
                this.Channels[i].CopyTo(typedTarget.Channels[i]);
            }

            foreach (DictionaryEntry de in this.HtEnvVariables)
            {// 遍历哈希表为目标对象重构哈希表
                typedTarget.HtEnvVariables.Add(de.Key, de.Value);
            }
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public virtual object Clone()
        {
            SimEnvironment target = new SimEnvironment();

            this.FieldInfo.CopyTo(target.FieldInfo);
            //target.FieldInfo = this.FieldInfo;

            for (int i = 0; i < this.Balls.Count; i++)
            {
                target.Balls.Add(new Ball());

                // 将当前对象第i个仿真水球的公共参数CopyTo目标对象
                this.Balls[i].CopyTo(target.Balls[i]);
            }

            for (int i = 0; i < this.ObstaclesRound.Count; i++)
            {
                target.ObstaclesRound.Add(new RoundedObstacle());

                // 将当前对象第i个仿真圆形障碍物的公共参数CopyTo目标对象
                this.ObstaclesRound[i].CopyTo(target.ObstaclesRound[i]);
            }

            for (int i = 0; i < this.ObstaclesRect.Count; i++)
            {
                target.ObstaclesRect.Add(new RectangularObstacle());

                // 将当前对象第i个仿真方形障碍物的公共参数CopyTo目标对象
                this.ObstaclesRect[i].CopyTo(target.ObstaclesRect[i]);
            }

            for (int i = 0; i < this.DynamicRect.Count; i++)
            {
                target.DynamicRect.Add(new RectangularDynamic());

                // 将当前对象第i个动态仿真方形障碍物的公共参数CopyTo目标对象
                this.DynamicRect[i].CopyTo(target.DynamicRect[i]);
            }

            for (int i = 0; i < this.Channels.Count; i++)
            {
                target.Channels.Add(new Channel());

                // 将当前对象第i个仿真方形障碍物的公共参数CopyTo目标对象
                this.Channels[i].CopyTo(target.Channels[i]);
            }

            foreach (DictionaryEntry de in this.HtEnvVariables)
            {// 遍历哈希表为目标对象重构哈希表
                target.HtEnvVariables.Add(de.Key, de.Value);
            }

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public virtual void Serialize(System.IO.BinaryWriter writer)
        {
            // 将仿真场地信息写入writer
            this.FieldInfo.Serialize(writer);

            // 先往writer写入仿真水球列表元素个数以便Deserialize时能先从reader取得该值
            writer.Write(this.Balls.Count);
            for (int i = 0; i < this.Balls.Count; i++)
            {
                // 将当前对象第i个仿真水球的详细信息写入writer
                this.Balls[i].Serialize(writer);
            }

            writer.Write(this.ObstaclesRound.Count);
            for (int i = 0; i < this.ObstaclesRound.Count; i++)
            {
                // 将当前对象第i个仿真圆形障碍物的详细信息写入writer
                this.ObstaclesRound[i].Serialize(writer);
            }

            writer.Write(this.ObstaclesRect.Count);
            for (int i = 0; i < this.ObstaclesRect.Count; i++)
            {
                // 将当前对象第i个仿真方形障碍物的详细信息写入writer
                this.ObstaclesRect[i].Serialize(writer);
            }


            writer.Write(this.DynamicRect.Count);
            for (int i = 0; i < this.DynamicRect.Count; i++)
            {
                // 将当前对象第i个动态仿真方形障碍物的详细信息写入writer
                this.DynamicRect[i].Serialize(writer);
            }

            writer.Write(this.Channels.Count);
            for (int i = 0; i < this.Channels.Count; i++)
            {
                // 将当前对象第i个仿真方形障碍物的详细信息写入writer
                this.Channels[i].Serialize(writer);
            }

            // 先将Hashtable的元素个数序列化供反序列化时重构Hashtable用
            writer.Write(HtEnvVariables.Count);
            foreach (DictionaryEntry de in this.HtEnvVariables)
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
            // 从reader读取仿真场地信息
            this.FieldInfo.Deserialize(reader);

            int count = reader.ReadInt32(); // 读取仿真水球列表元素个数
            for (int i = 0; i < count; i++)
            {
                this.Balls.Add(new Ball());

                // 从reader中读取值填充当前对象第i个仿真水球的详细信息
                this.Balls[i].Deserialize(reader);
            }

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                this.ObstaclesRound.Add(new RoundedObstacle());

                // 将当前对象第i个仿真圆形障碍物的详细信息写入writer
                this.ObstaclesRound[i].Deserialize(reader);
            }

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                this.ObstaclesRect.Add(new RectangularObstacle());

                // 将当前对象第i个仿真圆形障碍物的详细信息写入writer
                this.ObstaclesRect[i].Deserialize(reader);
            }

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                this.DynamicRect.Add(new RectangularDynamic());

                // 将当前对象第i个仿真圆形障碍物的详细信息写入writer
                this.DynamicRect[i].Deserialize(reader);
            }

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                this.Channels.Add(new Channel());

                // 将当前对象第i个仿真圆形障碍物的详细信息写入writer
                this.Channels[i].Deserialize(reader);
            }

            // 反序列化Hashtable元素个数
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {// 反序列化键值对重构Hashtable
                string key = reader.ReadString();
                string value = reader.ReadString();
                this.HtEnvVariables.Add(key, value);
            }
            return this;
        }
        #endregion
    }

    #region 仿真场地定义
    /// <summary>
    /// 仿真比赛场地数据类
    /// </summary>
    /// <remarks>无get和set以外实现逻辑的数据成员可使用自动实现的属性，
    /// 需要在Dss Service之间传递的属性均置以Attribute[DataMember]</remarks>
    [DataContract]
    [Serializable]
    public class Field : ICloneable, IDssSerializable
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static Field instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Field Instance()
        {
            if (instance == null)
            {
                instance = new Field();
            }
            return instance;
        }

        /// <summary>
        /// 默认构造函数从配置文件中读取可配置参数并计算出需要计算的参数
        /// </summary>
        private Field() 
        {
            SetField();

            // 给4个球门块碰撞检测用的顶点列表分配空间
            for (int i = 0; i < 4; i++)
            {
                BorderLeftTopVertices.Add(new xna.Vector3());
                BorderLeftBottomVertices.Add(new xna.Vector3());
                BorderRightTopVertices.Add(new xna.Vector3());
                BorderRightBottomVertices.Add(new xna.Vector3());
            }
        }
        #endregion

        #region 仿真比赛物理场地毫米尺寸向屏幕场地像素尺寸转换的系数
        /// <summary>
        /// 毫米向像素转化的比例因子（**待求**），单位毫米每像素
        /// </summary>
        public double ScaleMmToPix { get; set; }

        /// <summary>
        /// X方向毫米向像素转化的比例因子（**待求**），单位毫米每像素
        /// </summary>
        public double ScaleMmToPixX { get; set; }

        /// <summary>
        /// Z方向毫米向像素转化的比例因子（**待求**），单位毫米每像素
        /// </summary>
        public double ScaleMmToPixZ { get; set; }
        #endregion

        #region 仿真比赛屏幕场地以像素为单位的尺寸参数
        /// <summary>
        /// 比赛场地中心点在绘图容器PictureBox中的X方向屏幕坐标（**待求**），单位像素
        /// </summary>
        public int FieldCenterXPix { get; set; }

        /// <summary>
        /// 比赛场地中心点在绘图容器PictureBox中的Z方向屏幕坐标（**待求**），单位像素
        /// </summary>
        public int FieldCenterZPix { get; set; }

        /// <summary>
        /// 选项卡控件X方向上的屏幕长度（**配置**），单位像素
        /// </summary>
        public int TabControlLengthXPix { get; set; }

        /// <summary>
        /// 选项卡控件Z方向上的屏幕长度（**配置**），单位像素
        /// </summary>
        public int TabControlLengthZPix { get; set; }

        /// <summary>
        /// 比赛场地内边界的屏幕宽度（**配置**），单位像素
        /// </summary>
        public int FieldInnerBorderPix { get; set; }

        /// <summary>
        /// 比赛场地外边界的屏幕宽度（**配置**），单位像素
        /// </summary>
        public int FieldOuterBorderPix { get; set; }

        /// <summary>
        /// 比赛场地和选项卡控件之间空隙的屏幕宽度（**配置**），单位像素
        /// </summary>
        public int SpanBetweenFieldAndTabControlPix { get; set; }

        /// <summary>
        /// 窗体内边距的屏幕宽度（**配置**），单位像素
        /// </summary>
        public int FormPaddingPix { get; set; }

        /// <summary>
        /// 图片箱X方向上的屏幕宽度（**待求**），单位像素
        /// </summary>
        public int PictureBoxXPix { get; set; }

        /// <summary>
        /// 图片箱Z方向上的屏幕宽度（**待求**），单位像素
        /// </summary>
        public int PictureBoxZPix { get; set; }

        /// <summary>
        /// 窗体X方向上的屏幕宽度（**待求**），单位像素
        /// </summary>
        public int FormLengthXPix { get; set; }

        /// <summary>
        /// 窗体Z方向上的屏幕宽度（**待求**），单位像素
        /// </summary>
        public int FormLengthZPix { get; set; }

        /// <summary>
        /// 比赛场地X方向上的屏幕长度（**待求**），单位像素
        /// </summary>
        public int FieldLengthXPix { get; set; }

        /// <summary>
        /// 比赛场地Z方向上的屏幕长度（**待求**），单位像素
        /// </summary>
        public int FieldLengthZPix { get; set; }

        /// <summary>
        /// 球门长边（沿Z方向）的屏幕长度（**待求**），单位像素
        /// </summary>
        [DataMember]
        public int GoalWidthPix { get; set; }

        /// <summary>
        /// 球门短边（沿X方向）的屏幕长度（**待求**），单位像素
        /// </summary>
        [DataMember]
        public int GoalDepthPix { get; set; }

        /// <summary>
        /// 球门块长边（沿Z方向）的屏幕长度（**待求**），单位像素
        /// </summary>
        public int GoalBlockWidthPix { get; set; }

        /// <summary>
        /// 球门块短边（沿X方向）的屏幕长度（**待求**），单位像素
        /// </summary>
        public int GoalBlockDepthPix { get; set; }

        /// <summary>
        /// 禁区Z方向的屏幕长度（**待求**），单位像素
        /// </summary>
        public int ForbiddenZoneLengthZPix { get; set; }

        /// <summary>
        /// 禁区X方向的屏幕长度（**待求**），单位像素
        /// </summary>
        public int ForbiddenZoneLengthXPix { get; set; }

        /// <summary>
        /// 中心圆的屏幕半径（**待求**），单位像素
        /// </summary>
        public int CenterCircleRadiusPix { get; set; }
        #endregion

        #region 仿真比赛物理场地以毫米为单位的尺寸参数
        /// <summary>
        /// 比赛场地X方向上的实际长度（**配置或界面修改**），单位毫米
        /// </summary>
        [DataMember]
        public int FieldLengthXMm { get; set; }

        /// <summary>
        /// 比赛场地Z方向上的实际长度（**配置或界面修改**），单位毫米
        /// </summary>
        [DataMember]
        public int FieldLengthZMm { get; set; }

        #region 保存X和Z方向实际长度最初配置值 LiYoubing 20110627
        // 保存X和Z方向实际长度最初配置值，是为了根据界面上输入的X长度值按照配置值的比例计算Z长度值
        // 不直接保存X和Z方向最初配置值之比，是为了不引入浮点乘除法而引起精度问题
        // LiYoubing 20110627
        /// <summary>
        /// 比赛场地X方向上的实际长度最初配置值（**配置文件中的值**），单位毫米
        /// </summary>
        [DataMember]
        public int FieldLengthXOriMm { get; set; }

        // LiYoubing 20110627
        /// <summary>
        /// 比赛场地Z方向上的实际长度最初配置值（**配置文件中的值**），单位毫米
        /// </summary>
        [DataMember]
        public int FieldLengthZOriMm { get; set; }
        #endregion

        /// <summary>
        /// 球门长边（正置沿Z方向）的实际长度（**配置**），单位毫米
        /// </summary>
        [DataMember]
        public int GoalWidthMm { get; set; }

        /// <summary>
        /// 球门短边（正置沿X方向）的实际长度（**配置**），单位毫米
        /// </summary>
        [DataMember]
        public int GoalDepthMm { get; set; }

        /// <summary>
        /// 禁区Z方向的实际长度（**配置**），单位毫米
        /// </summary>
        [DataMember]
        public int ForbiddenZoneLengthZMm { get; set; }

        /// <summary>
        /// 禁区X方向的实际长度（**配置**），单位毫米
        /// </summary>
        [DataMember]
        public int ForbiddenZoneLengthXMm { get; set; }

        /// <summary>
        /// 中心圆的实际半径（**配置**），单位毫米
        /// </summary>
        [DataMember]
        public int CenterCircleRadiusMm { get; set; }

        #endregion

        #region 仿真比赛物理场地以毫米为单位的坐标参数
        /// <summary>
        /// 矩形场地中心为原点，右为正X，下为正Z的坐标系中场地左边界X坐标值，单位毫米
        /// </summary>
        [DataMember]
        public int LeftMm { get; set; }

        /// <summary>
        /// 矩形场地中心为原点，右为正X，下为正Z的坐标系中场地上边界Y坐标值，单位毫米
        /// </summary>
        [DataMember]
        public int TopMm { get; set; }

        /// <summary>
        /// 矩形场地中心为原点，右为正X，下为正Z的坐标系中场地右边界X坐标值，单位毫米
        /// </summary>
        [DataMember]
        public int RightMm { get; set; }

        /// <summary>
        /// 矩形场地中心为原点，右为正X，下为正Z的坐标系中场地下边界Y坐标值，单位毫米
        /// </summary>
        [DataMember]
        public int BottomMm { get; set; }

        #endregion

        #region 仿真场地碰撞检测参数 Renjing 20101207
        /// <summary>
        /// 边界左上角球门块的碰撞检测多边形顶点列表
        /// </summary>
        public List<xna.Vector3> BorderLeftTopVertices = new List<xna.Vector3>(4);

        /// <summary>
        /// 边界左下角球门块的碰撞检测多边形顶点列表
        /// </summary>
        public List<xna.Vector3> BorderLeftBottomVertices = new List<xna.Vector3>(4);

        /// <summary>
        /// 边界右上角球门块的碰撞检测多边形顶点列表
        /// </summary>
        public List<xna.Vector3> BorderRightTopVertices = new List<xna.Vector3>(4);

        /// <summary>
        /// 边界右下角球门块的碰撞检测多边形顶点列表
        /// </summary>
        public List<xna.Vector3> BorderRightBottomVertices = new List<xna.Vector3>(4);

        /// <summary>
        /// 球门块是否规则地摆放在仿真场地四个角上
        /// </summary>
        public bool IsGoalBlockRegular = true;
        #endregion

        /// <summary>
        /// 从配置文件中读取可配置参数并计算出需要计算的参数
        /// </summary>
        public void SetField()
        {
            TabControlLengthXPix = 312;
            TabControlLengthZPix = 550;

            FieldInnerBorderPix = 5;
            FieldOuterBorderPix = 20;
            SpanBetweenFieldAndTabControlPix = 10;
            FormPaddingPix = 10;

            FieldLengthXMm = 3000;
            FieldLengthZMm = 2000;
            GoalDepthMm = 150;
            GoalWidthMm = 400;

            ForbiddenZoneLengthXMm = 400;
            ForbiddenZoneLengthZMm = 1000;
            CenterCircleRadiusMm = 200;

            try
            {
                SysConfig myConfig = SysConfig.Instance();

                FieldLengthXMm = Convert.ToInt32(myConfig.MyXmlReader("FieldLengthXMm"));
                FieldLengthZMm = Convert.ToInt32(myConfig.MyXmlReader("FieldLengthZMm"));
                GoalDepthMm = Convert.ToInt32(myConfig.MyXmlReader("GoalDepthMm"));
                GoalWidthMm = Convert.ToInt32(myConfig.MyXmlReader("GoalWidthMm"));

                FieldInnerBorderPix = Convert.ToInt32(myConfig.MyXmlReader("FieldInnerBorderPix"));
                FieldOuterBorderPix = Convert.ToInt32(myConfig.MyXmlReader("FieldOuterBorderPix"));
                SpanBetweenFieldAndTabControlPix = Convert.ToInt32(myConfig.MyXmlReader("SpanBetweenFieldAndTabControlPix"));
                FormPaddingPix = Convert.ToInt32(myConfig.MyXmlReader("FormPaddingPix"));
                ForbiddenZoneLengthXMm = Convert.ToInt32(myConfig.MyXmlReader("ForbiddenZoneLengthXMm"));
                ForbiddenZoneLengthZMm = Convert.ToInt32(myConfig.MyXmlReader("ForbiddenZoneLengthZMm"));
                CenterCircleRadiusMm = Convert.ToInt32(myConfig.MyXmlReader("CenterCircleRadiusMm"));
            }
            catch
            {
                Console.WriteLine("从配置文件读取参数出错...");
            }

            // 保存X和Z方向实际长度最初配置值 LiYoubing 20110627
            FieldLengthXOriMm = FieldLengthXMm;
            FieldLengthZOriMm = FieldLengthZMm;

            // 根据设定的部分场地参数计算需要计算的部分场地参数
            FieldCalculation();
        }

        /// <summary>
        /// API: Retrieves a handle to the top-level window whose class name and window name 
        /// match the specified strings
        /// </summary>
        /// <param name="lpClassName">The class name or a class atom</param>
        /// <param name="lpWindowName">The window name (the window's title)</param>
        /// <returns>A handle to the window that has the specified class name and window name or NULL</returns>
        [System.Runtime.InteropServices.DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);

        // LiYoubing 20110627
        /// <summary>
        /// 根据设定的部分场地参数计算需要计算的部分场地参数
        /// </summary>
        public void FieldCalculation()
        {
            // 查找URWPGSim2DServer程序主窗口获取窗口句柄
            int hWnd = FindWindow(null, "Server of Underwater Robot Water Polo Game Sim 2D");
            Screen screen = (hWnd == 0) ? Screen.PrimaryScreen : Screen.FromHandle((IntPtr)hWnd);
            int ScreenWidth = screen.Bounds.Width;    //显示器宽度
            int ScreenHeight = screen.Bounds.Height;  //显示器高度 
            double dRatioPictureBoxWidthAndHeight = (double)(ScreenWidth - TabControlLengthXPix - 2 * FormPaddingPix
                - SpanBetweenFieldAndTabControlPix - 10) / (double)(TabControlLengthZPix);
            double dRatioFieldXMmAndZMm = (double)(FieldLengthXMm) / (double)(FieldLengthZMm);
            if (dRatioFieldXMmAndZMm <= dRatioPictureBoxWidthAndHeight)
            {
                FieldLengthZPix = TabControlLengthZPix - 2 * FieldInnerBorderPix - 2 * FieldOuterBorderPix;
                ScaleMmToPix = (double)FieldLengthZMm / (double)FieldLengthZPix;
                FieldLengthXPix = (int)(FieldLengthXMm / ScaleMmToPix + 0.5);
                GoalDepthPix = (int)(GoalDepthMm / ScaleMmToPix + 0.5);
                GoalWidthPix = (int)(GoalWidthMm / ScaleMmToPix + 0.5);
                GoalBlockWidthPix = (int)((FieldLengthZPix - GoalWidthPix) / 2 + 0.5);
                GoalBlockDepthPix = GoalDepthPix;

                PictureBoxXPix = FieldLengthXPix + 2 * FieldInnerBorderPix + 2 * FieldOuterBorderPix;
                PictureBoxZPix = FieldLengthZPix + 2 * FieldInnerBorderPix + 2 * FieldOuterBorderPix;
                FormLengthXPix = TabControlLengthXPix + FieldLengthXPix + SpanBetweenFieldAndTabControlPix
                    + 2 * FieldInnerBorderPix + 2 * FieldOuterBorderPix + 2 * FormPaddingPix;
                FormLengthZPix = TabControlLengthZPix + 2 * FormPaddingPix;
                ForbiddenZoneLengthXPix = (int)(ForbiddenZoneLengthXMm / ScaleMmToPix + 0.5);
                ForbiddenZoneLengthZPix = (int)(ForbiddenZoneLengthZMm / ScaleMmToPix + 0.5);
                CenterCircleRadiusPix = (int)(CenterCircleRadiusMm / ScaleMmToPix + 0.5);
            }
            else
            {
                PictureBoxXPix = ScreenWidth - TabControlLengthXPix - 2 * FormPaddingPix - SpanBetweenFieldAndTabControlPix - 10;
                FieldLengthXPix = PictureBoxXPix - 2 * FieldOuterBorderPix - 2 * FieldInnerBorderPix;
                ScaleMmToPix = (double)(FieldLengthXMm) / (double)(FieldLengthXPix);
                FieldLengthZPix = (int)(FieldLengthZMm / ScaleMmToPix + 0.5);
                GoalDepthPix = (int)(GoalDepthMm / ScaleMmToPix + 0.5);
                GoalWidthPix = (int)(GoalWidthMm / ScaleMmToPix + 0.5);
                GoalBlockWidthPix = (int)((FieldLengthZPix - GoalWidthPix) / 2 + 0.5);
                GoalBlockDepthPix = GoalDepthPix;
                PictureBoxZPix = FieldLengthZPix + 2 * FieldInnerBorderPix + 2 * FieldOuterBorderPix;

                FormLengthXPix = TabControlLengthXPix + FieldLengthXPix + SpanBetweenFieldAndTabControlPix
                    + 2 * FieldInnerBorderPix + 2 * FieldOuterBorderPix + 2 * FormPaddingPix;
                FormLengthZPix = TabControlLengthZPix + 2 * FormPaddingPix;
                ForbiddenZoneLengthXPix = (int)(ForbiddenZoneLengthXMm / ScaleMmToPix + 0.5);
                ForbiddenZoneLengthZPix = (int)(ForbiddenZoneLengthZMm / ScaleMmToPix + 0.5);
                CenterCircleRadiusPix = (int)(CenterCircleRadiusMm / ScaleMmToPix + 0.5);
            }

            // X和Z方向即屏幕水平和垂直方向毫米到像素的转换系数置为相同
            ScaleMmToPixX = ScaleMmToPix;
            ScaleMmToPixZ = ScaleMmToPix;

            // 以毫米为单位的实际场地坐标原点选在矩形场地中心点
            LeftMm = -FieldLengthXMm / 2;
            RightMm = FieldLengthXMm / 2;
            TopMm = -FieldLengthZMm / 2;
            BottomMm = FieldLengthZMm / 2;

            // 场地中心点在绘图容器PictureBox中的像素坐标
            FieldCenterXPix = FieldOuterBorderPix + FieldInnerBorderPix + FieldLengthXPix / 2;
            FieldCenterZPix = FieldOuterBorderPix + FieldInnerBorderPix + FieldLengthZPix / 2;
        }

        /// <summary>
        /// 球门块方向类型，TYPE11/TYPE12分别表示直角弯钩里侧沿逆时针/顺时针方向
        /// </summary>
        public enum GoalBlockType
        {
            /// <summary>
            /// 球门块直角弯钩里侧沿逆时针方向
            /// </summary>
            TYPE11 = 0, 
            
            /// <summary>
            /// 球门块直角弯钩里侧沿顺时针方向
            /// </summary>
            TYPE12
        }

        /// <summary>
        /// 将仿真比赛场地绘制到Bitmap对象上并返回该对象
        /// </summary>
        /// <param name="bmp"></param>
        ///  <param name="IsGoalBlockNeeded"></param>
        ///  <param name="IsFieldInnerLinesNeeded"></param>
        /// <returns>绘制好比赛场地的Bitmap对象的引用</returns>
        public Bitmap Draw(Bitmap bmp, bool IsGoalBlockNeeded, bool IsFieldInnerLinesNeeded)
        {
            bool flag = false;  // 使用背景图片标识
            //Bitmap bmp = null;  // 要绘制的图片对象引用

            // 启用水波作为绘制背景
            flag = true;
            //bmp = URWPGSim2D.Gadget.WaveEffect.Instance().GetBitmap();
           
            Graphics myGraphic = Graphics.FromImage(bmp);

            #region 绘制比赛场地边界 Weiqingdan
            //// 通过三个尺寸逐渐缩小的不同颜色矩形依次填充得到两层边界和最里面的矩形比赛场地
            //// 最外层矩形和Bitmap对象尺寸一致填充灰色
            //myGraphic.FillRectangle(new SolidBrush(Color.Gray), new Rectangle(new Point(0, 0),
            //    new Size(FieldLengthXPix + 2 * FieldInnerBorderPix + 2 * FieldOuterBorderPix,
            //        FieldLengthZPix + 2 * FieldInnerBorderPix + 2 * FieldOuterBorderPix)));
            //// 中间层矩形长宽尺寸均缩小2*FieldOuterBorderPix个像素填充白色覆盖最外层矩形得到灰色外边框
            //myGraphic.FillRectangle(new SolidBrush(Color.White),
            //    new Rectangle(new Point(FieldOuterBorderPix, FieldOuterBorderPix),
            //        new Size(FieldLengthXPix + 2 * FieldInnerBorderPix, FieldLengthZPix + 2 * FieldInnerBorderPix)));
            //// 最里层矩形长宽再缩小2*FieldInnerBorderPix个像素填充蓝色覆盖中间层矩形得到白色内边框和蓝色场地
            //myGraphic.FillRectangle(new SolidBrush(Color.RoyalBlue),
            //    new Rectangle(new Point(FieldOuterBorderPix + FieldInnerBorderPix,
            //        FieldOuterBorderPix + FieldInnerBorderPix), new Size(FieldLengthXPix, FieldLengthZPix)));
            #endregion

            #region 绘制比赛场地边界新方法 LiYoubing 20110626
            // 绘制上外框
            myGraphic.FillRectangle(new SolidBrush(Color.Gray), 0, 0, 
                FieldLengthXPix + 2 * FieldInnerBorderPix + 2 * FieldOuterBorderPix, FieldOuterBorderPix);
            // 绘制下外框
            myGraphic.FillRectangle(new SolidBrush(Color.Gray), 0, FieldLengthZPix + 2 * FieldInnerBorderPix + FieldOuterBorderPix, 
                FieldLengthXPix + 2 * FieldInnerBorderPix + 2 * FieldOuterBorderPix, FieldOuterBorderPix);
            // 绘制左外框
            myGraphic.FillRectangle(new SolidBrush(Color.Gray), 0, 0, 
                FieldOuterBorderPix, FieldLengthZPix + 2 * FieldInnerBorderPix + 2 * FieldOuterBorderPix);
            // 绘制右外框
            myGraphic.FillRectangle(new SolidBrush(Color.Gray), FieldLengthXPix + 2 * FieldInnerBorderPix + FieldOuterBorderPix, 0, 
                FieldOuterBorderPix, FieldLengthZPix + 2 * FieldInnerBorderPix + 2 * FieldOuterBorderPix);
            // 绘制上内框
            myGraphic.FillRectangle(new SolidBrush(Color.White), FieldOuterBorderPix, FieldOuterBorderPix, 
                FieldLengthXPix + 2 * FieldInnerBorderPix, FieldInnerBorderPix);
            // 绘制下内框
            myGraphic.FillRectangle(new SolidBrush(Color.White), FieldOuterBorderPix, FieldLengthZPix + FieldInnerBorderPix + FieldOuterBorderPix, 
                FieldLengthXPix + 2 * FieldInnerBorderPix, FieldInnerBorderPix);
            // 绘制左内框
            myGraphic.FillRectangle(new SolidBrush(Color.White), FieldOuterBorderPix, FieldOuterBorderPix,
                FieldInnerBorderPix, FieldLengthZPix + 2 * FieldInnerBorderPix);
            // 绘制右内框
            myGraphic.FillRectangle(new SolidBrush(Color.White), FieldLengthXPix + FieldInnerBorderPix + FieldOuterBorderPix, FieldOuterBorderPix,
                FieldInnerBorderPix, FieldLengthZPix + 2 * FieldInnerBorderPix);

            if (flag == false)
            {// 不使用背景图片则填充蓝色背景
                myGraphic.FillRectangle(new SolidBrush(Color.RoyalBlue),
                    new Rectangle(new Point(FieldOuterBorderPix + FieldInnerBorderPix,
                        FieldOuterBorderPix + FieldInnerBorderPix), new Size(FieldLengthXPix, FieldLengthZPix)));
            }
            #endregion

            #region 绘制球门块部分代码
            // 画四个球门块
            if (IsGoalBlockNeeded)//比赛需要绘制球门块，则绘制在场地内
            {
                DrawGoal(myGraphic, new Point(FieldOuterBorderPix + FieldInnerBorderPix + GoalDepthPix,
                    FieldOuterBorderPix + FieldInnerBorderPix), GoalBlockType.TYPE12, 90);
                DrawGoal(myGraphic, new Point(FieldOuterBorderPix + FieldInnerBorderPix + GoalDepthPix,
                    FieldOuterBorderPix + FieldInnerBorderPix + FieldLengthZPix), GoalBlockType.TYPE11, 270);
                DrawGoal(myGraphic, new Point(FieldOuterBorderPix + FieldInnerBorderPix + FieldLengthXPix - GoalDepthPix,
                    FieldOuterBorderPix + FieldInnerBorderPix), GoalBlockType.TYPE11, 90);
                DrawGoal(myGraphic, new Point(FieldOuterBorderPix + FieldInnerBorderPix + FieldLengthXPix - GoalDepthPix,
                    FieldOuterBorderPix + FieldInnerBorderPix + FieldLengthZPix), GoalBlockType.TYPE12, 270);
            }
            else//比赛不需要绘制球门块，则绘制在场地外。避免碰撞处理部分重写代码。
            {
                DrawGoal(myGraphic, new Point(5000,
                    5000), GoalBlockType.TYPE12, 90);
                DrawGoal(myGraphic, new Point(5000,
                    5000), GoalBlockType.TYPE11, 270);
                DrawGoal(myGraphic, new Point(5000,
                   5000), GoalBlockType.TYPE11, 90);
                DrawGoal(myGraphic, new Point(5000,
                    5000), GoalBlockType.TYPE12, 270);
            }
            #endregion

            #region 绘制禁区线、中线、中圈部分代码
            // revised by LiYoubing 20110314
            // DrawLine方法画宽度为1个像素以上的直线时，其宽度所占据的点是从起始点按照逆时针方向分布的
            // 造成左右禁区的右边线和左边线不对称，由于使用的2个像素的画笔，故可在左边线的起始坐标X分量加1像素调节

            // 画左边禁区
            // 画上横线
            if (IsFieldInnerLinesNeeded)
            {
                myGraphic.DrawLine(new Pen(Brushes.White, 2),
                    new Point(GoalDepthPix + FieldInnerBorderPix + FieldOuterBorderPix,
                        (FieldLengthZPix - ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix),
                    new Point(GoalDepthPix + FieldInnerBorderPix + FieldOuterBorderPix + ForbiddenZoneLengthXPix,
                        (FieldLengthZPix - ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix));
                // 画右竖线
                //myGraphic.DrawLine(new Pen(Brushes.White, 2),
                //    new Point(GoalDepthPix + FieldInnerBorderPix + FieldOuterBorderPix + ForbiddenZoneLengthXPix,
                //        (FieldLengthZPix + ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix),
                //    new Point(GoalDepthPix + FieldInnerBorderPix + FieldOuterBorderPix + ForbiddenZoneLengthXPix,
                //        (FieldLengthZPix - ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix));
                myGraphic.DrawLine(new Pen(Brushes.White, 2),
                    new Point(GoalDepthPix + FieldInnerBorderPix + FieldOuterBorderPix + ForbiddenZoneLengthXPix + 1,
                        (FieldLengthZPix - ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix),
                    new Point(GoalDepthPix + FieldInnerBorderPix + FieldOuterBorderPix + ForbiddenZoneLengthXPix + 1,
                        (FieldLengthZPix + ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix));
                // 画下横线
                myGraphic.DrawLine(new Pen(Brushes.White, 2),
                    new Point(GoalDepthPix + FieldInnerBorderPix + FieldOuterBorderPix + ForbiddenZoneLengthXPix,
                        (FieldLengthZPix + ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix),
                    new Point(GoalDepthPix + FieldInnerBorderPix + FieldOuterBorderPix,
                        (FieldLengthZPix + ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix));

                // 画右边禁区
                // 画上横线
                myGraphic.DrawLine(new Pen(Brushes.White, 2),
                    new Point(FieldLengthXPix + FieldInnerBorderPix + FieldOuterBorderPix - GoalDepthPix,
                        (FieldLengthZPix - ForbiddenZoneLengthZPix) / 2 + FieldInnerBorderPix + FieldOuterBorderPix),
                    new Point(FieldLengthXPix + FieldOuterBorderPix + FieldInnerBorderPix - ForbiddenZoneLengthXPix - GoalDepthPix,
                        (FieldLengthZPix - ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix));
                // 画左竖线
                myGraphic.DrawLine(new Pen(Brushes.White, 2),
                    new Point(FieldLengthXPix + FieldOuterBorderPix + FieldInnerBorderPix - ForbiddenZoneLengthXPix - GoalDepthPix,
                        (FieldLengthZPix - ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix),
                    new Point(FieldLengthXPix + FieldOuterBorderPix + FieldInnerBorderPix - ForbiddenZoneLengthXPix - GoalDepthPix,
                        (FieldLengthZPix + ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix));
                // 画下横线
                myGraphic.DrawLine(new Pen(Brushes.White, 2),
                    new Point(FieldLengthXPix + FieldOuterBorderPix + FieldInnerBorderPix - ForbiddenZoneLengthXPix - GoalDepthPix,
                        (FieldLengthZPix + ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix),
                    new Point(FieldLengthXPix + FieldInnerBorderPix + FieldOuterBorderPix - GoalDepthPix,
                        (FieldLengthZPix + ForbiddenZoneLengthZPix) / 2 + FieldOuterBorderPix + FieldInnerBorderPix));

                //画中场线和中心圆
                myGraphic.DrawLine(new Pen(Brushes.White, 2),
                    new Point(FieldLengthXPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix,
                        FieldOuterBorderPix + FieldInnerBorderPix),
                    new Point(FieldLengthXPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix,
                        FieldLengthZPix + FieldOuterBorderPix + FieldInnerBorderPix));
                myGraphic.DrawEllipse(new Pen(Brushes.White, 2),
                    FieldLengthXPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix - CenterCircleRadiusPix - 1,
                    FieldLengthZPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix - CenterCircleRadiusPix - 1,
                    2 * CenterCircleRadiusPix, 2 * CenterCircleRadiusPix);

                //画5V5比赛项目的犯规虚线 added by zhangbo 2011.12.27
                Pen dotLinePen = new Pen(Brushes.White, (float)1.5);
                dotLinePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                myGraphic.DrawLine(dotLinePen, new Point(FieldLengthXPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix - CenterCircleRadiusPix,
                    FieldOuterBorderPix + FieldInnerBorderPix),
                    new Point(FieldLengthXPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix - CenterCircleRadiusPix,
                        FieldOuterBorderPix + FieldInnerBorderPix + FieldLengthZPix));
                myGraphic.DrawLine(dotLinePen, new Point(FieldLengthXPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix + CenterCircleRadiusPix,
                     FieldOuterBorderPix + FieldInnerBorderPix),
                    new Point(FieldLengthXPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix + CenterCircleRadiusPix,
                        FieldOuterBorderPix + FieldInnerBorderPix + FieldLengthZPix));


                //画三个小圆点（中圈里的小圆点、禁区内的点球点）
                myGraphic.DrawEllipse(new Pen(Brushes.White, 3),
                    FieldLengthXPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix - 3,
                    FieldLengthZPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix - 3, 5, 5);
                myGraphic.DrawEllipse(new Pen(Brushes.White, 2),
                    ForbiddenZoneLengthXPix + GoalDepthPix + FieldOuterBorderPix + FieldInnerBorderPix - 10 - 1,
                    FieldLengthZPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix - 1, 2, 2);
                myGraphic.DrawEllipse(new Pen(Brushes.White, 2),
                    FieldLengthXPix + FieldInnerBorderPix + FieldOuterBorderPix - GoalDepthPix - ForbiddenZoneLengthXPix + 10 - 1,
                    FieldLengthZPix / 2 + FieldInnerBorderPix + FieldOuterBorderPix - 1, 2, 2);
            }
            #endregion
            //截取场地图像，保存为Png格式
            //string ThePath = Application.StartupPath + "\\Image\\"; // Image目录 
            //string FileName = DateTime.Now.Date.ToString("yyyyMMdd") + DateTime.Now.ToString("HHmmss") + ".PNG";
            //if (!System.IO.Directory.Exists(ThePath))
            //{
            //    // 保存截图目录不存在则新建
            //    System.IO.Directory.CreateDirectory(ThePath);
            //}
            //bmp.Save(ThePath + "\\" + FileName, System.Drawing.Imaging.ImageFormat.Png);
            return bmp;
        }

        /// <summary>
        /// 绘制球门块  weiqingdan 20101116
        /// </summary>
        /// <param name="myGraphic">GDI+绘图对象</param>
        /// <param name="point">球门块长边的起始点</param>
        /// <param name="goalBlockType">球门块的方向，用枚举类型表示</param>
        /// <param name="iThetaAngle">球门块长边的倾斜角度</param>
        public void DrawGoal(Graphics myGraphic, Point point, GoalBlockType goalBlockType, int iThetaAngle)
        {
            Point point2 = new Point();
            Point point3 = new Point();
            Point point4 = new Point();
            Pen pen = new Pen(Brushes.White, 2);
            if (iThetaAngle >= 0 && iThetaAngle <= 90)
            {
                double dThetaRad = (double)iThetaAngle * Math.PI / 180;
                point2.X = (int)(point.X + GoalBlockWidthPix * Math.Cos(dThetaRad) + 0.5);
                point2.Y = (int)(point.Y + GoalBlockWidthPix * Math.Sin(dThetaRad) + 0.5);
                if (goalBlockType == (int)GoalBlockType.TYPE11)
                {
                    point3.X = (int)(point2.X + GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);
                    point3.Y = (int)(point2.Y - GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);

                    point4.X = (int)(point.X + GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);
                    point4.Y = (int)(point.Y - GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);

                    myGraphic.DrawLine(pen, point, point2);
                    myGraphic.DrawLine(pen, point2, point3);

                    //球门块右上部分的4个顶点，储存在一个序列里，在鱼和边界的碰撞检测中用到
                    BorderRightTopVertices[0] = PixToMm(new xna.Vector3(point.X, 0, point.Y));
                    BorderRightTopVertices[1] = PixToMm(new xna.Vector3(point2.X, 0, point2.Y));
                    BorderRightTopVertices[2] = PixToMm(new xna.Vector3(point3.X, 0, point3.Y));
                    BorderRightTopVertices[3] = PixToMm(new xna.Vector3(point4.X, 0, point4.Y));
                }
                if (goalBlockType == GoalBlockType.TYPE12)
                {
                    point3.X = (int)(point2.X - GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);
                    point3.Y = (int)(point2.Y + GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);

                    point4.X = (int)(point.X - GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);
                    point4.Y = (int)(point.Y + GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);

                    myGraphic.DrawLine(pen, point, point2);
                    myGraphic.DrawLine(pen, point2, point3);

                    //球门块左上部分的4个顶点，储存在一个序列里，在鱼和边界的碰撞检测中用到
                    BorderLeftTopVertices[0] = PixToMm(new xna.Vector3((float)point.X, 0, (float)point.Y));
                    BorderLeftTopVertices[1] = PixToMm(new xna.Vector3(point2.X, 0, point2.Y));
                    BorderLeftTopVertices[2] = PixToMm(new xna.Vector3(point3.X, 0, point3.Y));
                    BorderLeftTopVertices[3] = PixToMm(new xna.Vector3(point4.X, 0, point4.Y));
                }
            }
            if (iThetaAngle > 90 && iThetaAngle <= 180)
            {
                double dThetaRad = (double)(iThetaAngle - 90) * Math.PI / 180;
                point2.X = (int)(point.X - GoalBlockWidthPix * Math.Sin(dThetaRad) + 0.5);
                point2.Y = (int)(point.Y + GoalBlockWidthPix * Math.Cos(dThetaRad) + 0.5);
                if (goalBlockType == (int)GoalBlockType.TYPE11)
                {
                    point3.X = (int)(point2.X + GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);
                    point3.Y = (int)(point2.Y + GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);

                    point4.X = (int)(point.X + GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);
                    point4.Y = (int)(point.Y + GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);

                    myGraphic.DrawLine(pen, point, point2);
                    myGraphic.DrawLine(pen, point2, point3);
                }
                if (goalBlockType == GoalBlockType.TYPE12)
                {
                    point3.X = (int)(point2.X - GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);
                    point3.Y = (int)(point2.Y - GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);

                    point4.X = (int)(point.X - GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);
                    point4.Y = (int)(point.Y - GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);

                    myGraphic.DrawLine(pen, point, point2);
                    myGraphic.DrawLine(pen, point2, point3);
                }
            }
            if (iThetaAngle > 180 && iThetaAngle <= 270)
            {
                double dThetaRad = (double)(iThetaAngle - 180) * Math.PI / 180;
                point2.X = (int)(point.X - GoalBlockWidthPix * Math.Cos(dThetaRad) + 0.5);
                point2.Y = (int)(point.Y - GoalBlockWidthPix * Math.Sin(dThetaRad) + 0.5);
                if (goalBlockType == (int)GoalBlockType.TYPE11)
                {
                    point3.X = (int)(point2.X - GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);
                    point3.Y = (int)(point2.Y + GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);

                    point4.X = (int)(point.X - GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);
                    point4.Y = (int)(point.Y + GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);

                    myGraphic.DrawLine(pen, point, point2);
                    myGraphic.DrawLine(pen, point2, point3);

                    //球门块左下部分的4个顶点，储存在一个序列里，在鱼和边界的碰撞检测中用到
                    BorderLeftBottomVertices[0] = PixToMm(new xna.Vector3(point.X, 0, point.Y));
                    BorderLeftBottomVertices[1] = PixToMm(new xna.Vector3(point2.X, 0, point2.Y));
                    BorderLeftBottomVertices[2] = PixToMm(new xna.Vector3(point3.X, 0, point3.Y));
                    BorderLeftBottomVertices[3] = PixToMm(new xna.Vector3(point4.X, 0, point4.Y));
                }
                if (goalBlockType == GoalBlockType.TYPE12)
                {
                    point3.X = (int)(point2.X + GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);
                    point3.Y = (int)(point2.Y - GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);

                    point4.X = (int)(point.X + GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);
                    point4.Y = (int)(point.Y - GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);

                    myGraphic.DrawLine(pen, point, point2);
                    myGraphic.DrawLine(pen, point2, point3);

                    //球门块右下部分的4个顶点，储存在一个序列里，在鱼和边界的碰撞检测中用到
                    BorderRightBottomVertices[0] = PixToMm(new xna.Vector3(point.X, 0, point.Y));
                    BorderRightBottomVertices[1] = PixToMm(new xna.Vector3(point2.X, 0, point2.Y));
                    BorderRightBottomVertices[2] = PixToMm(new xna.Vector3(point3.X, 0, point3.Y));
                    BorderRightBottomVertices[3] = PixToMm(new xna.Vector3(point4.X, 0, point4.Y));
                }
            }
            if (iThetaAngle > 270 && iThetaAngle < 360)
            {
                double dThetaRad = (double)(iThetaAngle - 270) * Math.PI / 180;
                point2.X = (int)(point.X + GoalBlockWidthPix * Math.Sin(dThetaRad) + 0.5);
                point2.Y = (int)(point.Y - GoalBlockWidthPix * Math.Cos(dThetaRad) + 0.5);
                if (goalBlockType == (int)GoalBlockType.TYPE11)
                {
                    point3.X = (int)(point2.X - GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);
                    point3.Y = (int)(point2.Y - GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);

                    point4.X = (int)(point.X - GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);
                    point4.Y = (int)(point.Y - GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);

                    myGraphic.DrawLine(pen, point, point2);
                    myGraphic.DrawLine(pen, point2, point3);
                }
                if (goalBlockType == GoalBlockType.TYPE12)
                {
                    point3.X = (int)(point2.X + GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);
                    point3.Y = (int)(point2.Y + GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);

                    point4.X = (int)(point.X + GoalBlockDepthPix * Math.Cos(dThetaRad) + 0.5);
                    point4.Y = (int)(point.Y + GoalBlockDepthPix * Math.Sin(dThetaRad) + 0.5);

                    myGraphic.DrawLine(pen, point, point2);
                    myGraphic.DrawLine(pen, point2, point3);
                }
            }
        }

        #region 以毫米为单位的尺寸或坐标向绘图容器PictureBox中像素坐标或其分量转换的方法
        /// <summary>
        /// 将X方向毫米数转换成绘图容器PictureBox中像素数
        /// </summary>
        /// <param name="mm">X方向毫米数</param>
        /// <returns>X方向在绘图容器PictureBox中像素数</returns>
        public int MmToPixX(int mm)
        {
            return (int)(mm / ScaleMmToPixX + 0.5) + FieldCenterXPix;
        }

        /// <summary>
        /// 将Z方向毫米数转换成绘图容器PictureBox中像素数
        /// </summary>
        /// <param name="mm">Z方向毫米数</param>
        /// <returns>Z方向在绘图容器PictureBox中像素数</returns>
        public int MmToPixZ(int mm)
        {
            return (int)(mm / ScaleMmToPixZ + 0.5) + FieldCenterZPix;
        }

        /// <summary>
        /// 将以毫米为单位的二维点转换成绘图容器PictureBox中以像素为单位的二维点
        /// </summary>
        /// <param name="pointMm">以毫米为单位的二维点</param>
        /// <returns>绘图容器PictureBox中以像素为单位的二维点</returns>
        public Point MmToPix(Point pointMm)
        {
            return new Point(MmToPixX(pointMm.X), MmToPixZ(pointMm.Y));
        }

        /// <summary>
        /// 将以毫米为单位的三维矢量转换成绘图容器PictureBox中以像素为单位的三维矢量，Y值置为0
        /// </summary>
        /// <param name="vector3">以毫米为单位的三维矢量</param>
        /// <returns>绘图容器PictureBox中以像素为单位的三维矢量</returns>
        public xna.Vector3 MmToPix(xna.Vector3 vector3)
        {
            return new xna.Vector3(MmToPixX((int)(vector3.X)), 0, MmToPixZ((int)(vector3.Z)));
        }

        /// <summary>
        /// 使用以毫米为单位的左上角坐标和宽度高度值生成绘图容器PictureBox中以像素为单位的矩形对象
        /// </summary>
        /// <param name="xMm">矩形左上角X坐标以毫米为单位的值</param>
        /// <param name="zMm">矩形左上角Z坐标以毫米为单位的值</param>
        /// <param name="widthMm">矩形宽度以毫米为单位的值</param>
        /// <param name="heightMm">矩形高度以毫米为单位的值</param>
        /// <returns>绘图容器PictureBox中以像素为单位的矩形对象</returns>
        public Rectangle MmToPix(int xMm, int zMm, int widthMm, int heightMm)
        {
            return new Rectangle(MmToPixX(xMm), MmToPixZ(zMm), 
                MmToPixX(widthMm) - FieldCenterXPix, MmToPixZ(heightMm) - FieldCenterZPix);
        }

        /// <summary>
        /// 将以毫米为坐标单位的矩形对象转换成绘图容器PictureBox中以像素为坐标单位的矩形对象
        /// </summary>
        /// <param name="rectMm">以毫米为坐标单位的矩形对象</param>
        /// <returns>绘图容器PictureBox中以像素为坐标单位的矩形对象</returns>
        public Rectangle MmToPix(Rectangle rectMm)
        {
            return new Rectangle(MmToPixX(rectMm.Left), MmToPixZ(rectMm.Top), 
                MmToPixX(rectMm.Width) - FieldCenterXPix, MmToPixZ(rectMm.Height) - FieldCenterZPix);
        }
        #endregion

        #region 绘图容器PictureBox中像素坐标向以毫米为单位的坐标转换的方法
        /// <summary>
        /// 点在场地绘图对象PictureBox坐标系中以像素为单位的坐标转换成实际场地坐标系中以毫米为单位的坐标
        /// </summary>
        /// <param name="pointPix">点在场地绘图对象PictureBox坐标系中以像素为单位的坐标值</param>
        /// <returns>点在实际场地坐标系中以毫米为单位的坐标值</returns>
        public Point PixToMm(Point pointPix)
        {
            return new Point((int)((pointPix.X - FieldCenterXPix) * ScaleMmToPixX), 
                (int)((pointPix.Y - FieldCenterZPix) * ScaleMmToPixZ));
        }

        /// <summary>
        /// 点在场地绘图对象PictureBox坐标系中以像素为单位的坐标转换成实际场地坐标系中以毫米为单位的坐标
        /// </summary>
        /// <param name="vector3Pix">点在场地绘图对象PictureBox坐标系中以像素为单位的三维坐标值</param>
        /// <returns>点在实际场地坐标系中以毫米为单位的三维坐标值</returns>
        public xna.Vector3 PixToMm(xna.Vector3 vector3Pix)
        {
            return new xna.Vector3((float)((vector3Pix.X - FieldCenterXPix) * ScaleMmToPixX), 0,
                (float)((vector3Pix.Z - FieldCenterZPix) * ScaleMmToPixZ));
        }
        #endregion

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public virtual void CopyTo(IDssSerializable target)
        {
            Field typedTarget = target as Field;

            typedTarget = this;
            //typedTarget.FieldLengthXMm = this.FieldLengthXMm;
            //typedTarget.FieldLengthZMm = this.FieldLengthZMm;
            //typedTarget.GoalDepthMm = this.GoalDepthMm;
            //typedTarget.GoalWidthMm = this.GoalWidthMm;
            //typedTarget.ForbiddenZoneLengthXMm = this.ForbiddenZoneLengthXMm;
            //typedTarget.ForbiddenZoneLengthZMm = this.ForbiddenZoneLengthZMm;
            //typedTarget.LeftMm = this.LeftMm;
            //typedTarget.RightMm = this.RightMm;
            //typedTarget.TopMm = this.TopMm;
            //typedTarget.BottomMm = this.BottomMm;
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public virtual object Clone()
        {
            //Field target = new Field();

            //target.FieldLengthXMm = this.FieldLengthXMm;
            //target.FieldLengthZMm = this.FieldLengthZMm;
            //target.GoalDepthMm = this.GoalDepthMm;
            //target.GoalWidthMm = this.GoalWidthMm;
            //target.ForbiddenZoneLengthXMm = this.ForbiddenZoneLengthXMm;
            //target.ForbiddenZoneLengthZMm = this.ForbiddenZoneLengthZMm;
            //target.LeftMm = this.LeftMm;
            //target.RightMm = this.RightMm;
            //target.TopMm = this.TopMm;
            //target.BottomMm = this.BottomMm;

            //return target;
            return this;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public virtual void Serialize(System.IO.BinaryWriter writer)
        {// 只Serialize需要在Dss Node间传递的Field
            writer.Write(FieldLengthXMm);
            writer.Write(FieldLengthZMm);
            writer.Write(GoalDepthMm);
            writer.Write(GoalWidthMm);
            writer.Write(ForbiddenZoneLengthXMm);
            writer.Write(ForbiddenZoneLengthZMm);
            writer.Write(LeftMm);
            writer.Write(RightMm);
            writer.Write(TopMm);
            writer.Write(BottomMm);
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public virtual object Deserialize(System.IO.BinaryReader reader)
        {// 只Deserialize需要在Dss Node间传递的Field
            FieldLengthXMm = reader.ReadInt32();
            FieldLengthZMm = reader.ReadInt32();
            GoalDepthMm = reader.ReadInt32();
            GoalWidthMm = reader.ReadInt32();
            ForbiddenZoneLengthXMm = reader.ReadInt32();
            ForbiddenZoneLengthZMm = reader.ReadInt32();
            LeftMm = reader.ReadInt32();
            RightMm = reader.ReadInt32();
            TopMm = reader.ReadInt32();
            BottomMm = reader.ReadInt32();

            return this;
        }
        #endregion
    }
    #endregion

    #region 仿真场地上水球/障碍物/通道等对象的定义
    /// <summary>
    /// 仿真场地中圆形和方形对象绘制静态类
    /// </summary>
    public static class DrawHelper
    {
        /// <summary>
        /// 绘制仿真场地中圆形对象到GDI+绘图对象上
        /// </summary>
        /// <param name="g">来自与绘图容器PictureBox等尺寸的Bitmap的GDI+绘图对象</param>
        /// <param name="positionMm">待绘制对象中心点在场地坐标系中的坐标，各维单位毫米</param>
        /// <param name="radiusMm">待绘制对象半径，单位毫米</param>
        /// <param name="borderWithPix">待绘制对象边框宽度，单位像素</param>
        /// <param name="colorBorder">待绘制对象边框颜色</param>
        /// <param name="colorFilled">待绘制对象填充颜色</param>
        public static void DrawEllipse(ref Graphics g, xna.Vector3 positionMm,
            int radiusMm, int borderWithPix, Color colorBorder, Color colorFilled)
        {
            Field f = Field.Instance();

            int leftPix = f.MmToPixX((int)positionMm.X - radiusMm);
            int topPix = f.MmToPixZ((int)positionMm.Z - radiusMm);
            int lengthPix = f.MmToPixX(2 * radiusMm) - f.FieldCenterXPix;
            int widthPix = f.MmToPixZ(2 * radiusMm) - f.FieldCenterZPix;
            /*
                        SolidBrush brush = new SolidBrush(colorFilled);
                        g.FillEllipse(brush, leftPix + borderWithPix, topPix + borderWithPix,
                            lengthPix - 2 * borderWithPix, widthPix - 2 * borderWithPix);   // 填充对象
            */
            //尝试绘制渐变色彩的球形
            GraphicsPath pathBall = new GraphicsPath();
            pathBall.AddEllipse(new Rectangle(leftPix, topPix, lengthPix, widthPix));
            PathGradientBrush brush = new PathGradientBrush(pathBall);
            brush.CenterColor = colorBorder;
            brush.SurroundColors = new Color[]
            {
               colorFilled
            };
            brush.SetBlendTriangularShape(1f, 0.98f);
           // g.FillPath(brush, pathBall);
           
            //LinearGradientBrush gbrush = new LinearGradientBrush(new Rectangle(leftPix, topPix, lengthPix, widthPix), Color.Yellow, Color.Black, LinearGradientMode.Horizontal);
            g.FillEllipse(brush, leftPix, topPix, lengthPix, widthPix);  //进行渐变色彩填充


            Pen pen = new Pen(new SolidBrush(colorBorder), borderWithPix);
           // g.DrawEllipse(pen, leftPix, topPix, lengthPix, widthPix);          // 绘制边框

        }
        /// <summary>
        /// 绘制仿真场地中方形对象到GDI+绘图对象上
        /// </summary>
        /// <param name="g">来自与绘图容器PictureBox等尺寸的Bitmap的GDI+绘图对象</param>
        /// <param name="positionMm">待绘制对象中心点在场地坐标系中的坐标，各维单位毫米</param>
        /// <param name="directionRad">待绘制对象长度方向，单位弧度</param>
        /// <param name="lengthMm">待绘制对象长度，单位毫米</param>
        /// <param name="widthMm">待绘制对象宽度，单位毫米</param>
        /// <param name="borderWithPix">待绘制对象边框宽度，单位像素</param>
        /// <param name="colorBorder">待绘制对象边框颜色</param>
        /// <param name="colorFilled">待绘制对象填充颜色</param>
        public static void DrawRectangle(ref Graphics g, xna.Vector3 positionMm, float directionRad, 
            int lengthMm, int widthMm, int borderWithPix, Color colorBorder, Color colorFilled)
        {
            Field f = Field.Instance();

            // 根据中心点位置、长度、宽度和方向计算矩形四个顶点坐标
            float sine = (float)Math.Sin(directionRad);
            float cosine = (float)Math.Cos(directionRad);

            Point[] PolygonVertices = new Point[4];
            PolygonVertices[0] = f.MmToPix(new Point((int)(positionMm.X - lengthMm * cosine / 2 + widthMm * sine / 2),
                (int)(positionMm.Z - lengthMm * sine / 2 - widthMm * cosine / 2)));
            PolygonVertices[1] = f.MmToPix(new Point((int)(positionMm.X + lengthMm * cosine / 2 + widthMm * sine / 2),
                (int)(positionMm.Z + lengthMm * sine / 2 - widthMm * cosine / 2)));
            PolygonVertices[2] = f.MmToPix(new Point((int)(positionMm.X + lengthMm * cosine / 2 - widthMm * sine / 2),
                (int)(positionMm.Z + lengthMm * sine / 2 + widthMm * cosine / 2)));
            PolygonVertices[3] = f.MmToPix(new Point((int)(positionMm.X - lengthMm * cosine / 2 - widthMm * sine / 2),
                (int)(positionMm.Z - lengthMm * sine / 2 + widthMm * cosine / 2)));

            for (int i = 0; i < PolygonVertices.Length; i++)
            {// 所有顶点往左上偏移1像素 以消除绘制导致的视觉误差 LiYoubing 20110710
                PolygonVertices[i].X -= 1;
                PolygonVertices[i].Y -= 1;
            }
            //modified by chenxiao 绘制3d效果的障碍物
            GraphicsPath path = new GraphicsPath();
            path.AddPolygon(PolygonVertices);
            PathGradientBrush brush = new PathGradientBrush(path);
            //brush.CenterPoint = PolygonVertices[0];
            brush.CenterPoint = new Point((PolygonVertices[0].X + PolygonVertices[1].X) / 2, (PolygonVertices[0].Y + PolygonVertices[3].Y) / 2);
            brush.CenterColor =Color .White;
            brush.SetBlendTriangularShape(1f, 0.56f);
          //  brush.SurroundColors = new Color[] { Color.SlateGray };
            brush.SurroundColors = new Color[] { Color.Green};
            //g.FillPath(brush, path);

            //// 填充对象
            //SolidBrush brush = new SolidBrush(colorFilled);
            g.FillPolygon(brush, PolygonVertices);

            //// 绘制边框
            Pen pen = new Pen(new SolidBrush(colorBorder), borderWithPix);
            g.DrawPolygon(pen, PolygonVertices);
        }
    }

    /// <summary>
    /// 仿真场地中圆形动态对象基类
    /// </summary>
    [DataContract]
    [Serializable]
    public class RoundedDynamic : ICloneable, IDssSerializable
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public RoundedDynamic()
        {
            SetRoundedDynamic();
        }

        #region 外观参数
        /// <summary>
        /// 半径（**设置**），单位毫米
        /// </summary>
        public int RadiusMm;

        /// <summary>
        /// 填充颜色
        /// </summary>
        public Color ColorFilled;

        /// <summary>
        /// 边框颜色
        /// </summary>
        public Color ColorBorder;

        /// <summary>
        /// 边框宽度，单位像素
        /// </summary>
        public int BorderWidthPix;
        #endregion

        #region 运动学参数
        /// <summary>
        /// 当前中心点坐标，各维坐标单位毫米mm，2D版本中Y坐标固定为0
        /// </summary>
        public xna.Vector3 PositionMm;

        /// <summary>
        /// 当前中心点坐标备份，各维坐标单位毫米mm，2D版本中Y坐标固定为0
        /// </summary>        
        public xna.Vector3 PrePositionMm;

        /// <summary>
        /// 当前速度值，单位毫米每秒mm/s
        /// </summary>        
        public float VelocityMmPs;

        /// <summary>
        /// 当前速度值备份，单位毫米每秒mm/s
        /// </summary>        
        public float PreVelocityMmPs;

        /// <summary>
        /// 当前速度方向，单位弧度rad，值域[-PI, PI)
        /// </summary>        
        public float VelocityDirectionRad;

        /// <summary>
        /// 当前速度方向备份，单位弧度rad，值域[-PI, PI)
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

        #region 其他状态参数
        /// <summary>
        /// 当前碰撞状态
        /// </summary>
        public CollisionType Collision;

        /// <summary>
        /// 当前碰撞状态备份
        /// </summary>        
        public CollisionType PreCollision;
        #endregion

        /// <summary>
        /// 设置默认参数
        /// </summary>
        public void SetRoundedDynamic()
        {
            RadiusMm = 58;          // 默认半径58毫米
            BorderWidthPix = 0;     // 默认边框2像素
        }

        /// <summary>
        /// 重置仿真水球的部分运动学参数，用于重新初始化使命清除上一次运行结束时保存的值
        /// </summary>
        public void ResetSomeLocomotionPara()
        {
            VelocityMmPs = PreVelocityMmPs = 0;                     // 重置速度值
            VelocityDirectionRad = PreVelocityDirectionRad = 0;     // 重置速度方向值
            AngularVelocityRadPs = PreAngularVelocityRadPs = 0;     // 重置角速度值
        }

        /// <summary>
        /// 将当前对象绘制到GDI+绘图对象上
        /// </summary>
        /// <param name="g">来自与绘图容器PictureBox等尺寸的Bitmap的GDI+绘图对象</param>
        public void Draw(ref Graphics g)
        {
            DrawHelper.DrawEllipse(ref g, PositionMm, RadiusMm, BorderWidthPix, ColorBorder, ColorFilled);
        }

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public virtual void CopyTo(IDssSerializable target)
        {
            RoundedDynamic typedTarget = target as RoundedDynamic;

            typedTarget.RadiusMm = this.RadiusMm;
            typedTarget.BorderWidthPix = this.BorderWidthPix;

            typedTarget.PositionMm = this.PositionMm;
            typedTarget.VelocityMmPs = this.VelocityMmPs;
            typedTarget.VelocityDirectionRad = this.VelocityDirectionRad;
            typedTarget.AngularVelocityRadPs = this.AngularVelocityRadPs;
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public virtual object Clone()
        {
            RoundedDynamic target = new RoundedDynamic();

            target.RadiusMm = this.RadiusMm;
            target.BorderWidthPix = this.BorderWidthPix;

            target.PositionMm = this.PositionMm;
            target.VelocityMmPs = this.VelocityMmPs;
            target.VelocityDirectionRad = this.VelocityDirectionRad;
            target.AngularVelocityRadPs = this.AngularVelocityRadPs;

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public virtual void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(RadiusMm);
            writer.Write(BorderWidthPix);

            writer.Write(PositionMm.X);
            writer.Write(PositionMm.Y);
            writer.Write(PositionMm.Z);
            writer.Write(VelocityMmPs);
            writer.Write(VelocityDirectionRad);
            writer.Write(AngularVelocityRadPs);
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public virtual object Deserialize(System.IO.BinaryReader reader)
        {
            RadiusMm = reader.ReadInt32();
            BorderWidthPix = reader.ReadInt32();

            PositionMm.X = reader.ReadSingle();
            PositionMm.Y = reader.ReadSingle();
            PositionMm.Z = reader.ReadSingle();
            VelocityMmPs = reader.ReadSingle();
            VelocityDirectionRad = reader.ReadSingle();
            AngularVelocityRadPs = reader.ReadSingle();

            return this;
        }
        #endregion
    }

    /// <summary>
    /// 仿真场地中圆形静态对象基类
    /// </summary>
    [DataContract]
    [Serializable]
    public class RoundedStatic : ICloneable, IDssSerializable
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public RoundedStatic()
        {
            SetRoundedStatic();
        }

        #region 外观参数
        /// <summary>
        /// 半径（**设置**），单位毫米
        /// </summary>
        public int RadiusMm;

        /// <summary>
        /// 填充颜色
        /// </summary>
        public Color ColorFilled;

        /// <summary>
        /// 边框颜色
        /// </summary>
        public Color ColorBorder;

        /// <summary>
        /// 边框宽度，单位像素
        /// </summary>
        public int BorderWidthPix;
        #endregion

        #region 运动学参数
        /// <summary>
        /// 中心点坐标，各维坐标单位毫米mm，2D版本中Y坐标固定为0
        /// </summary>
        public xna.Vector3 PositionMm;
        #endregion

        /// <summary>
        /// 设置默认参数
        /// </summary>
        public void SetRoundedStatic()
        {
            RadiusMm = 58;          // 默认半径58毫米
            BorderWidthPix = 0;     // 默认边框2像素
        }

        /// <summary>
        /// 将当前对象绘制到GDI+绘图对象上
        /// </summary>
        /// <param name="g">来自与绘图容器PictureBox等尺寸的Bitmap的GDI+绘图对象</param>
        public void Draw(ref Graphics g)
        {
            DrawHelper.DrawEllipse(ref g, PositionMm, RadiusMm, BorderWidthPix, ColorBorder, ColorFilled);
        }

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public virtual void CopyTo(IDssSerializable target)
        {
            RoundedStatic typedTarget = target as RoundedStatic;

            typedTarget.RadiusMm = this.RadiusMm;
            typedTarget.BorderWidthPix = this.BorderWidthPix;
            typedTarget.PositionMm = this.PositionMm;
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public virtual object Clone()
        {
            RoundedStatic target = new RoundedStatic();

            target.RadiusMm = this.RadiusMm;
            target.BorderWidthPix = this.BorderWidthPix;
            target.PositionMm = this.PositionMm;

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public virtual void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(RadiusMm);
            writer.Write(BorderWidthPix);
            writer.Write(PositionMm.X);
            writer.Write(PositionMm.Y);
            writer.Write(PositionMm.Z);
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public virtual object Deserialize(System.IO.BinaryReader reader)
        {
            RadiusMm = reader.ReadInt32();
            BorderWidthPix = reader.ReadInt32();

            PositionMm.X = reader.ReadSingle();
            PositionMm.Y = reader.ReadSingle();
            PositionMm.Z = reader.ReadSingle();

            return this;
        }
        #endregion
    }

    /// <summary>
    /// 仿真场地中方形动态对象基类
    /// </summary>
    [DataContract]
    [Serializable]
    public class RectangularDynamic : ICloneable, IDssSerializable
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name;

        // LiYoubing 20110627
        /// <summary>
        /// 对象是否允许删除的标识量
        /// 仿真使命初始化时添加的对象不允许删除该值为false
        /// </summary>
        public bool IsDeletionAllowed;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RectangularDynamic()
        {
            SetRectangularDynamic();
            for (int i = 0; i < 4; i++)
            {// 初始化矩形四个顶点列表
                PolygonVertices.Add(new xna.Vector3(0, 0, 0));
            }
        }

                /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="strName">仿真方形障碍物名称</param>
        /// <param name="positionMm">仿真方形障碍物位置（在场地坐标系中，以毫米为单位）</param>
        /// <param name="colorBorder">仿真方形障碍物轮廓颜色</param>
        /// <param name="colorFilled">仿真方形障碍物填充颜色</param>
        /// <param name="borderWithPix">仿真方形障碍物轮廓像素宽度</param>
        /// <param name="lengthMm">仿真方形障碍物长度（以毫米为单位）</param>
        /// <param name="widthMm">仿真方形障碍物宽度（以毫米为单位）</param>
        /// <param name="directionDeg">仿真方形障碍物方向（在场地坐标系中，以弧度为单位）</param>
        /// <param name="velocityMmPs">当前速度值，单位毫米每秒mm/s</param>
        /// <param name="velocityDirectionRad">当前速度方向备份，单位弧度rad，值域[-PI, PI)</param>
        /// <param name="angularVelocityRadPs">当前角速度值，单位弧度每秒rad/s</param>
        /// <param name="circleTimes">一个循环的周期数</param>
        public RectangularDynamic(string strName, xna.Vector3 positionMm, Color colorBorder,
            Color colorFilled, int borderWithPix, int lengthMm, int widthMm, float directionDeg, float velocityMmPs, float velocityDirectionRad, float angularVelocityRadPs,int circleTimes)
        {
            Name = strName;
            PositionMm = positionMm;
            ColorBorder = colorBorder;
            ColorFilled = colorFilled;
            BorderWidthPix = borderWithPix;
            LengthMm = lengthMm;
            WidthMm = widthMm;
            DirectionRad = xna.MathHelper.ToRadians(directionDeg);
            IsDeletionAllowed = true;   // 默认允许删除
            VelocityMmPs = velocityMmPs;
            VelocityDirectionRad = velocityDirectionRad;
            AngularVelocityRadPs = angularVelocityRadPs;
            CircleTimes = circleTimes;
            TimesCouter = CircleTimes;//计数器赋初值

        }


        #region 外观参数
        /// <summary>
        /// 长度（**设置**），单位毫米
        /// </summary>
        public int LengthMm;

        /// <summary>
        /// 宽度（**设置**），单位毫米
        /// </summary>
        public int WidthMm;

        /// <summary>
        /// 方向（**设置**），单位弧度
        /// </summary>
        public float DirectionRad;

        /// <summary>
        /// 填充颜色
        /// </summary>
        public Color ColorFilled;

        /// <summary>
        /// 边框颜色
        /// </summary>
        public Color ColorBorder;

        /// <summary>
        /// 边框宽度，单位像素
        /// </summary>
        public int BorderWidthPix;
        #endregion

        #region 运动学参数
        /// <summary>
        /// 当前中心点坐标，各维坐标单位毫米mm，2D版本中Y坐标固定为0
        /// </summary>
        public xna.Vector3 PositionMm;

        /// <summary>
        /// 当前中心点坐标备份，各维坐标单位毫米mm，2D版本中Y坐标固定为0
        /// </summary>        
        public xna.Vector3 PrePositionMm;

        /// <summary>
        /// 当前长度（Length）方向，单位弧度rad
        /// </summary>
        public float LengthDirectionRad;

        /// <summary>
        /// 当前长度（Length）方向备份，单位弧度rad
        /// </summary>        
        public float PreLengthDirectionRad;

        /// <summary>
        /// 当前速度值，单位毫米每秒mm/s
        /// </summary>        
        public float VelocityMmPs;

        /// <summary>
        /// 当前速度值备份，单位毫米每秒mm/s
        /// </summary>        
        public float PreVelocityMmPs;

        /// <summary>
        /// 当前速度方向，单位弧度rad，值域[-PI, PI)
        /// </summary>        
        public float VelocityDirectionRad;

        /// <summary>
        /// 当前速度方向备份，单位弧度rad，值域[-PI, PI)
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

        /// <summary>
        /// 一个循环包含的周期数
        /// </summary>        
        public int CircleTimes;

        /// <summary>
        /// 周期的计数器
        /// </summary>        
        public int TimesCouter;


        #endregion

        #region 其他状态参数
        /// <summary>
        /// 当前碰撞状态
        /// </summary>
        public CollisionType Collision;

        /// <summary>
        /// 当前碰撞状态备份
        /// </summary>        
        public CollisionType PreCollision;


        #region 碰撞检测参数 
        /// <summary>
        /// 外接圆半径（**根据长度宽度计算**），单位毫米
        /// </summary>
        public int CircumcircleRadiusMm;

        /// <summary>
        /// 矩形4个顶点列表，4个元素依次为左上/右上/右下/左下，各顶点各维坐标单位毫米
        /// </summary>
        public List<xna.Vector3> PolygonVertices = new List<xna.Vector3>(4);

        #endregion

        #endregion

        /// <summary>
        /// 设置默认参数
        /// </summary>
        public void SetRectangularDynamic()
        {
            LengthMm = 100;         // 默认长度100毫米
            WidthMm = 100;          // 默认宽度100毫米
            DirectionRad = 0;       // 默认方向水平
            BorderWidthPix = 0;     // 默认边框0像素
        }

        /// <summary>
        /// 计算碰撞检测参数，由各继承类构造函数调用
        /// </summary>
        public void CalculateCollisionDetectionParas()
        {
            CircumcircleRadiusMm = (int)Math.Sqrt(LengthMm * LengthMm + WidthMm * WidthMm) / 2;
            float sine = (float)Math.Sin(DirectionRad);
            float cosine = (float)Math.Cos(DirectionRad);
            PolygonVertices[0] = new xna.Vector3(PositionMm.X - LengthMm * cosine / 2 + WidthMm * sine / 2, 0, PositionMm.Z - LengthMm * sine / 2 - WidthMm * cosine / 2);
            PolygonVertices[1] = new xna.Vector3(PositionMm.X + LengthMm * cosine / 2 + WidthMm * sine / 2, 0, PositionMm.Z + LengthMm * sine / 2 - WidthMm * cosine / 2);
            PolygonVertices[2] = new xna.Vector3(PositionMm.X + LengthMm * cosine / 2 - WidthMm * sine / 2, 0, PositionMm.Z + LengthMm * sine / 2 + WidthMm * cosine / 2);
            PolygonVertices[3] = new xna.Vector3(PositionMm.X - LengthMm * cosine / 2 - WidthMm * sine / 2, 0, PositionMm.Z - LengthMm * sine / 2 + WidthMm * cosine / 2);
        }


        /// <summary>
        /// 将当前对象绘制到GDI+绘图对象上
        /// </summary>
        /// <param name="g">来自与绘图容器PictureBox等尺寸的Bitmap的GDI+绘图对象</param>
        public void Draw(ref Graphics g)
        {
            DrawHelper.DrawRectangle(ref g, PositionMm, DirectionRad, LengthMm, WidthMm, BorderWidthPix, ColorBorder, ColorFilled);
        }


        /// <summary>
        /// 根据仿真周期毫秒数更新动态障碍物运动学参数包括中心点位置坐标/速度值/速度方向/角速度值
        /// </summary>
        /// <param name="timeIntervalMs">仿真周期毫秒数</param>
        public void UpdateRectangularDynamicLocomotionPara(int timeIntervalMs)
        {
            Locomotion.UpdateRectangularDynamicLocomotionPara(ref PositionMm, ref PrePositionMm, ref VelocityMmPs,
                ref VelocityDirectionRad, ref AngularVelocityRadPs, ref PreVelocityMmPs, ref PreVelocityDirectionRad,
                ref PreAngularVelocityRadPs, -30.8f, -0.05f, timeIntervalMs, ref TimesCouter, ref CircleTimes);//原来Acceleration=-9.8f
        }

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public virtual void CopyTo(IDssSerializable target)
        {
            RectangularDynamic typedTarget = target as RectangularDynamic;

            typedTarget.BorderWidthPix = this.BorderWidthPix;
            typedTarget.WidthMm = this.WidthMm;
            typedTarget.LengthMm = this.LengthMm;
            typedTarget.LengthDirectionRad = this.LengthDirectionRad;
            typedTarget.DirectionRad = this.DirectionRad;
            typedTarget.PositionMm = this.PositionMm;
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public virtual object Clone()
        {
            RectangularDynamic target = new RectangularDynamic();

            target.BorderWidthPix = this.BorderWidthPix;
            target.WidthMm = this.WidthMm;
            target.LengthMm = this.LengthMm;
            target.LengthDirectionRad = this.LengthDirectionRad;
            target.DirectionRad = this.DirectionRad;
            target.PositionMm = this.PositionMm;

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public virtual void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(BorderWidthPix);
            writer.Write(WidthMm);
            writer.Write(LengthMm);
            writer.Write(LengthDirectionRad);
            writer.Write(DirectionRad);
            writer.Write(PositionMm.X);
            writer.Write(PositionMm.Y);
            writer.Write(PositionMm.Z);
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public virtual object Deserialize(System.IO.BinaryReader reader)
        {
            BorderWidthPix = reader.ReadInt32();
            WidthMm = reader.ReadInt32();
            LengthMm = reader.ReadInt32();
            LengthDirectionRad = reader.ReadSingle();
            DirectionRad = reader.ReadSingle();
            PositionMm.X = reader.ReadSingle();
            PositionMm.Y = reader.ReadSingle();
            PositionMm.Z = reader.ReadSingle();

            return this;
        }
        #endregion
    }

    /// <summary>
    /// 仿真场地中方形静态对象基类
    /// </summary>
    [DataContract]
    [Serializable]
    public class RectangularStatic : ICloneable, IDssSerializable
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public RectangularStatic()
        {
            SetRectangularStatic();
        }

        #region 外观参数
        /// <summary>
        /// 长度（**设置**），单位毫米
        /// </summary>
        public int LengthMm;

        /// <summary>
        /// 宽度（**设置**），单位毫米
        /// </summary>
        public int WidthMm;

        /// <summary>
        /// 方向（**设置**），单位弧度
        /// </summary>
        public float DirectionRad;

        /// <summary>
        /// 填充颜色
        /// </summary>
        public Color ColorFilled;

        /// <summary>
        /// 边框颜色
        /// </summary>
        public Color ColorBorder;

        /// <summary>
        /// 边框宽度，单位像素
        /// </summary>
        public int BorderWidthPix;
        #endregion

        #region 运动学参数
        /// <summary>
        /// 当前中心点坐标，各维坐标单位毫米mm，2D版本中Y坐标固定为0
        /// </summary>
        public xna.Vector3 PositionMm;

        /// <summary>
        /// 当前长度（Length）方向，单位弧度rad
        /// </summary>
        public float LengthDirectionRad;
        #endregion

        #region 碰撞检测参数 ChenPenghui
        /// <summary>
        /// 外接圆半径（**根据长度宽度计算**），单位毫米
        /// </summary>
        public int CircumcircleRadiusMm;

        /// <summary>
        /// 矩形4个顶点列表，4个元素依次为左上/右上/右下/左下，各顶点各维坐标单位毫米
        /// </summary>
        public  List<xna.Vector3> PolygonVertices = new List<xna.Vector3>(4);

        #endregion

        /// <summary>
        /// 设置默认参数
        /// </summary>
        public void SetRectangularStatic()
        {
            LengthMm = 100;         // 默认长度100毫米
            WidthMm = 100;          // 默认宽度100毫米
            DirectionRad = 0;       // 默认方向水平
            BorderWidthPix = 0;     // 默认边框0像素
            for (int i = 0; i < 4; i++)
            {// 初始化矩形四个顶点列表
                PolygonVertices.Add(new xna.Vector3(0, 0, 0));
            }
        }

        /// <summary>
        /// 计算碰撞检测参数，由各继承类构造函数调用
        /// </summary>
        public void CalculateCollisionDetectionParas()
        {
            CircumcircleRadiusMm = (int)Math.Sqrt(LengthMm * LengthMm + WidthMm * WidthMm) / 2;
            float sine = (float)Math.Sin(DirectionRad);
            float cosine = (float)Math.Cos(DirectionRad); 
            PolygonVertices[0] = new xna.Vector3(PositionMm.X - LengthMm * cosine / 2 + WidthMm * sine / 2, 0, PositionMm.Z - LengthMm * sine / 2 - WidthMm * cosine / 2);
            PolygonVertices[1] = new xna.Vector3(PositionMm.X + LengthMm * cosine / 2 + WidthMm * sine / 2, 0, PositionMm.Z + LengthMm * sine / 2 - WidthMm * cosine / 2);
            PolygonVertices[2] = new xna.Vector3(PositionMm.X + LengthMm * cosine / 2 - WidthMm * sine / 2, 0, PositionMm.Z + LengthMm * sine / 2 + WidthMm * cosine / 2);
            PolygonVertices[3] = new xna.Vector3(PositionMm.X - LengthMm * cosine / 2 - WidthMm * sine / 2, 0, PositionMm.Z - LengthMm * sine / 2 + WidthMm * cosine / 2);
        }

        /// <summary>
        /// 将当前对象绘制到GDI+绘图对象上
        /// </summary>
        /// <param name="g">来自与绘图容器PictureBox等尺寸的Bitmap的GDI+绘图对象</param>
        public void Draw(ref Graphics g)
        {
            DrawHelper.DrawRectangle(ref g, PositionMm, DirectionRad, LengthMm, WidthMm, BorderWidthPix, ColorBorder, ColorFilled);

            // 更新碰撞检测参数 LiYoubing 20110511
            CalculateCollisionDetectionParas();
        }

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public virtual void CopyTo(IDssSerializable target)
        {
            RectangularStatic typedTarget = target as RectangularStatic;

            typedTarget.BorderWidthPix = this.BorderWidthPix;
            typedTarget.WidthMm = this.WidthMm;
            typedTarget.LengthMm = this.LengthMm;
            typedTarget.LengthDirectionRad = this.LengthDirectionRad;
            typedTarget.DirectionRad = this.DirectionRad;
            typedTarget.PositionMm = this.PositionMm;
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public virtual object Clone()
        {
            RectangularStatic target = new RectangularStatic();

            target.BorderWidthPix = this.BorderWidthPix;
            target.WidthMm = this.WidthMm;
            target.LengthMm = this.LengthMm;
            target.LengthDirectionRad = this.LengthDirectionRad;
            target.DirectionRad = this.DirectionRad;
            target.PositionMm = this.PositionMm;

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public virtual void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(BorderWidthPix);
            writer.Write(WidthMm);
            writer.Write(LengthMm);
            writer.Write(LengthDirectionRad);
            writer.Write(DirectionRad);
            writer.Write(PositionMm.X);
            writer.Write(PositionMm.Y);
            writer.Write(PositionMm.Z);
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public virtual object Deserialize(System.IO.BinaryReader reader)
        {
            BorderWidthPix = reader.ReadInt32();
            WidthMm = reader.ReadInt32();
            LengthMm = reader.ReadInt32();
            LengthDirectionRad = reader.ReadSingle();
            DirectionRad = reader.ReadSingle();
            PositionMm.X = reader.ReadSingle();
            PositionMm.Y = reader.ReadSingle();
            PositionMm.Z = reader.ReadSingle();

            return this;
        }
        #endregion
    }

    /// <summary>
    /// 仿真水球类
    /// </summary>
    [DataContract]
    [Serializable]
    public class Ball : RoundedDynamic, ICloneable, IDssSerializable
    {
        /// <summary>
        ///  仿真机器鱼绘制轨迹用的点列表
        /// </summary>
        public List<Point> TrajectoryPoints = new List<Point>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public Ball()
        {
            SetBall();
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="radiusMm">仿真水球半径</param>
        /// <param name="color">仿真水球颜色</param>
        public Ball(int radiusMm, Color color)
        {
            SetBall();
            RadiusMm = radiusMm;
            ColorBorder = color;
            ColorFilled = color;
        }

        /// <summary>
        /// 从配置文件中读取可配置参数
        /// </summary>
        public void SetBall()
        {
            RadiusMm = 58;              // 默认半径58毫米
            ColorBorder = Color.Pink;   // 默认粉色边框
            ColorFilled = Color.Black;   // 默认粉色填充
            PositionMm = new xna.Vector3(0, 0, 0); // 默认置于场地中心点

            try
            {
                SysConfig myConfig = SysConfig.Instance();

                RadiusMm = Convert.ToInt32(myConfig.MyXmlReader("BallRadius"));
            }
            catch
            {
                Console.WriteLine("从配置文件读取参数出错...");
            }
        }

        /// <summary>
        /// 根据仿真周期毫秒数更新仿真水球运动学参数包括中心点位置坐标/速度值/速度方向/角速度值
        /// </summary>
        /// <param name="timeIntervalMs">仿真周期毫秒数</param>
        public void UpdateBallLocomotionPara(int timeIntervalMs)
        {
            Locomotion.UpdateBallLocomotionPara(ref PositionMm, ref PrePositionMm, ref VelocityMmPs,
                ref VelocityDirectionRad, ref AngularVelocityRadPs, ref PreVelocityMmPs, ref PreVelocityDirectionRad,
                ref PreAngularVelocityRadPs, -30.8f, -0.05f, timeIntervalMs);//原来Acceleration=-9.8f
        }

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public override void CopyTo(IDssSerializable target)
        {
            Ball typedTarget = target as Ball;

            base.CopyTo(typedTarget);
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public override object Clone()
        {
            Ball target = new Ball();

            base.CopyTo(target);

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public override void Serialize(System.IO.BinaryWriter writer)
        {
            base.Serialize(writer);
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public override object Deserialize(System.IO.BinaryReader reader)
        {
            base.Deserialize(reader);

            return this;
        }
        #endregion      
    }

    /// <summary>
    /// 仿真圆形静态障碍物类
    /// </summary>
    [DataContract]
    [Serializable]
    public class RoundedObstacle : RoundedStatic, ICloneable, IDssSerializable
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name;

        // LiYoubing 20110627
        /// <summary>
        /// 对象是否允许删除的标识量
        /// 仿真使命初始化时添加的对象不允许删除该值为false
        /// </summary>
        public bool IsDeletionAllowed;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public RoundedObstacle() { }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="strName">仿真圆形障碍物名称</param>
        /// <param name="positionMm">仿真圆形障碍物位置（在世界坐标系中，以毫米为单位）</param>
        /// <param name="color">仿真圆形障碍物颜色</param>
        /// <param name="radiusMm">仿真圆形障碍物半径（以毫米为单位）</param>
        public RoundedObstacle(string strName, xna.Vector3 positionMm, Color color, int radiusMm)
        {
            Name = strName;
            PositionMm = positionMm;
            ColorBorder = color;
            ColorFilled = color;
            RadiusMm = radiusMm;
            IsDeletionAllowed = true;   // 默认允许删除
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="strName">仿真圆形障碍物名称</param>
        /// <param name="positionMm">仿真圆形障碍物位置（在世界坐标系中，以毫米为单位）</param>
        /// <param name="colorBorder">仿真圆形障碍物轮廓颜色</param>
        /// <param name="colorFilled">仿真圆形障碍物填充颜色</param>
        /// <param name="radiusMm">仿真圆形障碍物半径（以毫米为单位）</param>
        public RoundedObstacle(string strName, xna.Vector3 positionMm, Color colorBorder, 
            Color colorFilled, int radiusMm)
        {
            Name = strName;
            PositionMm = positionMm;
            ColorBorder = colorBorder;
            ColorFilled = colorFilled;
            RadiusMm = radiusMm;
            IsDeletionAllowed = true;   // 默认允许删除
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="strName">仿真圆形障碍物名称</param>
        /// <param name="positionMm">仿真圆形障碍物位置（在场地坐标系中，以毫米为单位）</param>
        /// <param name="colorBorder">仿真圆形障碍物轮廓颜色</param>
        /// <param name="colorFilled">仿真圆形障碍物填充颜色</param>
        /// <param name="borderWithPix">仿真圆形障碍物轮廓像素宽度</param>
        /// <param name="radiusMm">仿真圆形障碍物半径（以毫米为单位）</param>
        public RoundedObstacle(string strName, xna.Vector3 positionMm, Color colorBorder,
            Color colorFilled, int borderWithPix, int radiusMm)
        {
            Name = strName;
            PositionMm = positionMm;
            ColorBorder = colorBorder;
            ColorFilled = colorFilled;
            BorderWidthPix = borderWithPix;
            RadiusMm = radiusMm;
            IsDeletionAllowed = true;   // 默认允许删除
        }

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public override void CopyTo(IDssSerializable target)
        {
            RoundedObstacle typedTarget = target as RoundedObstacle;

            typedTarget.Name = this.Name;
            base.CopyTo(typedTarget);
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public override object Clone()
        {
            RoundedObstacle target = new RoundedObstacle();

            target.Name = this.Name;
            base.CopyTo(target);

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public override void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(Name);
            base.Serialize(writer);
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public override object Deserialize(System.IO.BinaryReader reader)
        {
            this.Name = reader.ReadString();
            base.Deserialize(reader);

            return this;
        }
        #endregion      
    }

    /// <summary>
    /// 仿真方形静态障碍物类
    /// </summary>
    [DataContract]
    [Serializable]
    public class RectangularObstacle : RectangularStatic, ICloneable, IDssSerializable
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name;

        // LiYoubing 20110627
        /// <summary>
        /// 对象是否允许删除的标识量
        /// 仿真使命初始化时添加的对象不允许删除该值为false
        /// </summary>
        public bool IsDeletionAllowed;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public RectangularObstacle(){ }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="strName">仿真方形障碍物名称</param>
        /// <param name="positionMm">仿真方形障碍物位置（在场地坐标系中，以毫米为单位）</param>
        /// <param name="color">仿真方形障碍物颜色</param>
        /// <param name="lengthMm">仿真方形障碍物长度（以毫米为单位）</param>
        /// <param name="widthMm">仿真方形障碍物宽度（以毫米为单位）</param>
        /// <param name="directionDeg">仿真方形障碍物方向（在场地坐标系中，以弧度为单位）</param>
        public RectangularObstacle(string strName, xna.Vector3 positionMm, Color color, 
            int lengthMm, int widthMm, float directionDeg)
        {
            Name = strName;
            PositionMm = positionMm;
            ColorBorder = color;
            ColorFilled = color;
            LengthMm = lengthMm;
            WidthMm = widthMm;
            DirectionRad = xna.MathHelper.ToRadians(directionDeg);
            IsDeletionAllowed = true;   // 默认允许删除

            CalculateCollisionDetectionParas();
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="strName">仿真方形障碍物名称</param>
        /// <param name="positionMm">仿真方形障碍物位置（在场地坐标系中，以毫米为单位）</param>
        /// <param name="colorBorder">仿真方形障碍物轮廓颜色</param>
        /// <param name="colorFilled">仿真方形障碍物填充颜色</param>
        /// <param name="lengthMm">仿真方形障碍物长度（以毫米为单位）</param>
        /// <param name="widthMm">仿真方形障碍物宽度（以毫米为单位）</param>
        /// <param name="directionDeg">仿真方形障碍物方向（在场地坐标系中，以弧度为单位）</param>
        public RectangularObstacle(string strName, xna.Vector3 positionMm, Color colorBorder, 
            Color colorFilled, int lengthMm, int widthMm, float directionDeg)
        {
            Name = strName;
            PositionMm = positionMm;
            ColorBorder = colorBorder;
            ColorFilled = colorFilled;
            LengthMm = lengthMm;
            WidthMm = widthMm;
            DirectionRad = xna.MathHelper.ToRadians(directionDeg);
            IsDeletionAllowed = true;   // 默认允许删除

            CalculateCollisionDetectionParas();
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="strName">仿真方形障碍物名称</param>
        /// <param name="positionMm">仿真方形障碍物位置（在场地坐标系中，以毫米为单位）</param>
        /// <param name="colorBorder">仿真方形障碍物轮廓颜色</param>
        /// <param name="colorFilled">仿真方形障碍物填充颜色</param>
        /// <param name="borderWithPix">仿真方形障碍物轮廓像素宽度</param>
        /// <param name="lengthMm">仿真方形障碍物长度（以毫米为单位）</param>
        /// <param name="widthMm">仿真方形障碍物宽度（以毫米为单位）</param>
        /// <param name="directionDeg">仿真方形障碍物方向（在场地坐标系中，以弧度为单位）</param>
        public RectangularObstacle(string strName, xna.Vector3 positionMm, Color colorBorder,
            Color colorFilled, int borderWithPix, int lengthMm, int widthMm, float directionDeg)
        {
            Name = strName;
            PositionMm = positionMm;
            ColorBorder = colorBorder;
            ColorFilled = colorFilled;
            BorderWidthPix = borderWithPix;
            LengthMm = lengthMm;
            WidthMm = widthMm;
            DirectionRad = xna.MathHelper.ToRadians(directionDeg);
            IsDeletionAllowed = true;   // 默认允许删除

            CalculateCollisionDetectionParas();
        }

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public override void CopyTo(IDssSerializable target)
        {
            RectangularObstacle typedTarget = target as RectangularObstacle;

            typedTarget.Name = this.Name;
            base.CopyTo(typedTarget);
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public override object Clone()
        {
            RectangularObstacle target = new RectangularObstacle();

            target.Name = this.Name;
            base.CopyTo(target);

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public override void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(Name);
            base.Serialize(writer);
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public override object Deserialize(System.IO.BinaryReader reader)
        {
            this.Name = reader.ReadString();
            base.Deserialize(reader);

            return this;
        }
        #endregion      
    }

    /// <summary>
    /// 仿真通道类
    /// </summary>
    [DataContract]
    [Serializable]
    public class Channel : RectangularStatic, ICloneable, IDssSerializable
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Channel() { }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="strName">仿真通道名称</param>
        /// <param name="positionMm">仿真通道中心点位置（在场地坐标系中，以毫米为单位）</param>
        /// <param name="color">仿真通道颜色</param>
        /// <param name="lengthMm">仿真通道长度（以毫米为单位）</param>
        /// <param name="widthMm">仿真通道宽度（以毫米为单位）</param>
        /// <param name="directionDeg">仿真通道方向（在场地坐标系中，以弧度为单位）</param>
        public Channel(string strName, xna.Vector3 positionMm, Color color, 
            int lengthMm, int widthMm, float directionDeg)
        {
            Name = strName;
            PositionMm = positionMm;
            ColorBorder = color;
            ColorFilled = color;
            LengthMm = lengthMm;
            WidthMm = widthMm;
            DirectionRad = xna.MathHelper.ToRadians(directionDeg);
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="strName">仿真通道名称</param>
        /// <param name="positionMm">仿真通道中心点位置（在场地坐标系中，以毫米为单位）</param>
        /// <param name="colorBorder">仿真通道轮廓颜色</param>
        /// <param name="colorFilled">仿真通道填充颜色</param>
        /// <param name="lengthMm">仿真通道长度（以毫米为单位）</param>
        /// <param name="widthMm">仿真通道宽度（以毫米为单位）</param>
        /// <param name="directionDeg">仿真通道方向（在场地坐标系中，以弧度为单位）</param>
        public Channel(string strName, xna.Vector3 positionMm, Color colorBorder, 
            Color colorFilled, int lengthMm, int widthMm, float directionDeg)
        {
            Name = strName;
            PositionMm = positionMm;
            ColorBorder = colorBorder;
            ColorFilled = colorFilled;
            LengthMm = lengthMm;
            WidthMm = widthMm;
            DirectionRad = xna.MathHelper.ToRadians(directionDeg);
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="strName">仿真通道名称</param>
        /// <param name="positionMm">仿真通道中心点位置（在场地坐标系中，以毫米为单位）</param>
        /// <param name="colorBorder">仿真通道轮廓颜色</param>
        /// <param name="colorFilled">仿真通道填充颜色</param>
        /// <param name="borderWithPix">仿真通道轮廓像素宽度</param>
        /// <param name="lengthMm">仿真通道长度（以毫米为单位）</param>
        /// <param name="widthMm">仿真通道宽度（以毫米为单位）</param>
        /// <param name="directionDeg">仿真通道方向（在场地坐标系中，以弧度为单位）</param>
        public Channel(string strName, xna.Vector3 positionMm, Color colorBorder,
            Color colorFilled, int borderWithPix, int lengthMm, int widthMm, float directionDeg)
        {
            Name = strName;
            PositionMm = positionMm;
            ColorBorder = colorBorder;
            ColorFilled = colorFilled;
            BorderWidthPix = borderWithPix;
            LengthMm = lengthMm;
            WidthMm = widthMm;
            DirectionRad = xna.MathHelper.ToRadians(directionDeg);
        }

        #region 实现ICloneable, IDssSerializable接口，用于Dss消息传递
        /// <summary>
        /// 实现CopyTo接口的方法,将当前对象拷贝到target指定的<see cref="Microsoft.Dss.Core.IDssSerializable">IDssSerializable</see>类型对象
        /// </summary>
        /// <param name="target">当前对象拷贝到的目标对象</param>
        public override void CopyTo(IDssSerializable target)
        {
            Channel typedTarget = target as Channel;

            typedTarget.Name = this.Name;
            base.CopyTo(typedTarget);
        }

        /// <summary>
        /// 实现Clone接口的方法，拷贝一份当前对象的副本
        /// </summary>
        /// <returns>当前对象的副本</returns>
        public override object Clone()
        {
            Channel target = new Channel();

            target.Name = this.Name;
            base.CopyTo(target);

            return target;
        }

        /// <summary>
        /// 实现Serialize接口的方法，将当前对象序列化到writer指定的<see cref="System.IO.BinaryWriter">BinaryWriter</see>类型对象
        /// </summary>
        /// <param name="writer">当前对象序列化到的目标对象</param>
        public override void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(Name);
            base.Serialize(writer);
        }

        /// <summary>
        /// 实现Deserialize接口的方法，从reader指定的<see cref="System.IO.BinaryReader">BinaryReader</see>类型对象中反序列化一个当前类型的对象
        /// </summary>
        /// <param name="reader">反序列化操作的来源对象</param>
        /// <returns>反序列化得到的当前类型对象</returns>
        public override object Deserialize(System.IO.BinaryReader reader)
        {
            this.Name = reader.ReadString();
            base.Deserialize(reader);

            return this;
        }
        #endregion
    }
    #endregion
}