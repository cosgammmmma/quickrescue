//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: SysConfig.cs
// Date: 20101120  Author: LiYoubing  Version: 1
// Description: 杂项类定义文件
// Histroy:
// Date: 20110630  Author: LiYoubing
// Modification:
// 1.加入仿真机器鱼/水球/障碍物参数合法性检查方法
// Date: 20111110  Author: ZhangBo
// Modification:
// 1.由于某些项目无需绘制球门块，故修改仿真机器鱼/水球/障碍物参数合法性检查方法
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;
using xna = Microsoft.Xna.Framework;

namespace URWPGSim2D.Common
{
    /// <summary>
    /// URWPGSim项目需要用到的辅助功能静态类
    /// </summary>
    public static class UrwpgSimHelper
    {
        #region 求四个数值的最大值和最小值
        /// <summary>
        /// 求4个值的int序列的最大值并标出最大值的序号（0,1,2,3）
        /// </summary>
        /// <param name="value1">第0号int值</param>
        /// <param name="value2">第1号int值</param>
        /// <param name="value3">第2号int值</param>
        /// <param name="value4">第3号int值</param>
        /// <param name="seq">最大值的序号（0,1,2,3）</param>
        /// <returns>最大值</returns>
        public static int Max(int value1, int value2, int value3, int value4, ref int seq)
        {
            int result = value1;
            seq = 0;
            if (value2 > result)
            {
                result = value2;
                seq = 1;
            }
            if (value3 > result)
            {
                result = value3;
                seq = 2;
            }
            if (value4 > result)
            {
                result = value4;
                seq = 3;
            }
            return result;
        }

        /// <summary>
        /// 求4个值的int序列的最小值并标出最小值的序号（0,1,2,3）
        /// </summary>
        /// <param name="value1">第0号整数</param>
        /// <param name="value2">第1号整数</param>
        /// <param name="value3">第2号整数</param>
        /// <param name="value4">第3号整数</param>
        /// <param name="seq">最小值的序号（0,1,2,3）</param>
        /// <returns>最小值</returns>
        public static int Min(int value1, int value2, int value3, int value4, ref int seq)
        {
            int result = value1;
            seq = 0;
            if (value2 < result)
            {
                result = value2;
                seq = 1;
            }
            if (value3 < result)
            {
                result = value3;
                seq = 2;
            }
            if (value4 < result)
            {
                result = value4;
                seq = 3;
            }
            return result;
        }

        /// <summary>
        /// 求4个值的float序列的最大值并标出最大值的序号（0,1,2,3）
        /// </summary>
        /// <param name="value1">第0号整数</param>
        /// <param name="value2">第1号整数</param>
        /// <param name="value3">第2号整数</param>
        /// <param name="value4">第3号整数</param>
        /// <param name="seq">最大值的序号（0,1,2,3）</param>
        /// <returns>最大值</returns>
        public static float Max(float value1, float value2, float value3, float value4, ref int seq)
        {
            float result = value1;
            seq = 0;
            if (value2 > result)
            {
                result = value2;
                seq = 1;
            }
            if (value3 > result)
            {
                result = value3;
                seq = 2;
            }
            if (value4 > result)
            {
                result = value4;
                seq = 3;
            }
            return result;
        }

        /// <summary>
        /// 求4个值的float序列的最小值并标出最小值的序号（0,1,2,3）
        /// </summary>
        /// <param name="value1">第0号float值</param>
        /// <param name="value2">第1号float值</param>
        /// <param name="value3">第2号float值</param>
        /// <param name="value4">第3号float值</param>
        /// <param name="seq">最小值的序号（0,1,2,3）</param>
        /// <returns>最小值</returns>
        public static float Min(float value1, float value2, float value3, float value4, ref int seq)
        {
            float result = value1;
            seq = 0;
            if (value2 < result)
            {
                result = value2;
                seq = 1;
            }
            if (value3 < result)
            {
                result = value3;
                seq = 2;
            }
            if (value4 < result)
            {
                result = value4;
                seq = 3;
            }
            return result;
        }

        /// <summary>
        /// 求5个值的float序列的最大值并标出最大值的序号（0,1,2,3,4）
        /// </summary>
        /// <param name="value1">第0号整数</param>
        /// <param name="value2">第1号整数</param>
        /// <param name="value3">第2号整数</param>
        /// <param name="value4">第3号整数</param>
        /// <param name="value5">第4号整数</param>
        /// <param name="seq">最大值的序号（0,1,2,3,4）</param>
        /// <returns>最大值</returns>
        public static float Max(float value1, float value2, float value3, float value4,float value5, ref int seq)
        {
            float result = value1;
            seq = 0;
            if (value2 > result)
            {
                result = value2;
                seq = 1;
            }
            if (value3 > result)
            {
                result = value3;
                seq = 2;
            }
            if (value4 > result)
            {
                result = value4;
                seq = 3;
            }
            if (value5 > result)
            {
                result = value5;
                seq = 4;
            }
            return result;
        }

        /// <summary>
        /// 求5个值的float序列的最小值并标出最小值的序号（0,1,2,3,4）
        /// </summary>
        /// <param name="value1">第0号float值</param>
        /// <param name="value2">第1号float值</param>
        /// <param name="value3">第2号float值</param>
        /// <param name="value4">第3号float值</param>
        /// <param name="value5">第4号float值</param>
        /// <param name="seq">最小值的序号（0,1,2,3,4）</param>
        /// <returns>最小值</returns>
        public static float Min(float value1, float value2, float value3, float value4, float value5, ref int seq)
        {
            float result = value1;
            seq = 0;
            if (value2 < result)
            {
                result = value2;
                seq = 1;
            }
            if (value3 < result)
            {
                result = value3;
                seq = 2;
            }
            if (value4 < result)
            {
                result = value4;
                seq = 3;
            }
            if (value5 < result)
            {
                result = value5;
                seq = 4;
            }
            return result;
        }
        #endregion

        #region 求七个数值的最大值和最小值
        /// <summary>
        /// 求7个值的float序列的最大值并标出最大值的序号（0,1,2,3,4,5,6）
        /// </summary>
        /// <param name="value1">第0号整数</param>
        /// <param name="value2">第1号整数</param>
        /// <param name="value3">第2号整数</param>
        /// <param name="value4">第3号整数</param>
        /// <param name="value5">第4号整数</param>
        /// <param name="value6">第5号整数</param>
        /// <param name="value7">第6号整数</param>
        /// <param name="seq">最大值的序号（0,1,2,3,4,5,6）</param>
        /// <returns>最大值</returns>
        public static float Max(float value1, float value2, float value3, float value4, float value5, float value6, float value7, ref int seq)
        {
            float result = value1;
            seq = 0;
            if (value2 > result)
            {
                result = value2;
                seq = 1;
            }
            if (value3 > result)
            {
                result = value3;
                seq = 2;
            }
            if (value4 > result)
            {
                result = value4;
                seq = 3;
            }
            if (value5 > result)
            {
                result = value5;
                seq = 4;
            }
            if (value6 > result)
            {
                result = value6;
                seq = 5;
            }
            if (value7 > result)
            {
                result = value7;
                seq = 6;
            }
            return result;
        }

        /// <summary>
        /// 求7个值的float序列的最小值并标出最小值的序号（0,1,2,3,4,5,6）
        /// </summary>
        /// <param name="value1">第0号float值</param>
        /// <param name="value2">第1号float值</param>
        /// <param name="value3">第2号float值</param>
        /// <param name="value4">第3号float值</param>
        /// <param name="value5">第4号float值</param>
        /// <param name="value6">第5号float值</param>
        /// <param name="value7">第6号float值</param>
        /// <param name="seq">最小值的序号（0,1,2,3,4,5,6）</param>
        /// <returns>最小值</returns>
        public static float Min(float value1, float value2, float value3, float value4, float value5, float value6, float value7, ref int seq)
        {
            float result = value1;
            seq = 0;
            if (value2 < result)
            {
                result = value2;
                seq = 1;
            }
            if (value3 < result)
            {
                result = value3;
                seq = 2;
            }
            if (value4 < result)
            {
                result = value4;
                seq = 3;
            }
            if (value5 < result)
            {
                result = value5;
                seq = 4;
            }
            if (value6 < result)
            {
                result = value6;
                seq = 5;
            }
            if (value7 < result)
            {
                result = value7;
                seq = 6;
            }
            return result;
        }
        #endregion

        #region 平面坐标系转换 Added by LiYoubing 20110418
        /// <summary>
        /// 本地坐标系到世界坐标系（仿真场地中心为原点，向右为正X，向下为Z的坐标系）的变换
        /// </summary>
        /// <param name="theta">本地坐标系正X在世界坐标系中的角度值</param>
        /// <param name="xLocalOri">输入参数，本地坐标系原点在世界坐标系下的X坐标值</param>
        /// <param name="zLocalOri">输入参数，本地坐标系原点在世界坐标系下的Z坐标值</param>
        /// <param name="xLocal">输入参数，待变换坐标的点在本地坐标系下的X坐标值</param>
        /// <param name="zLocal">输入参数，待变换坐标的点在本地坐标系下的Z坐标值</param>
        /// <param name="xWorld">输出参数，待变换坐标的点在世界坐标系下的X坐标值</param>
        /// <param name="zWorld">输出参数，待变换坐标的点在世界坐标系下的Z坐标值</param>
        public static void CoordinateTransformation(float theta, float xLocalOri, float zLocalOri, float xLocal, float zLocal, ref float xWorld, ref float zWorld)
        {
            float cosTheta = (float)Math.Cos(theta);
            float sinTheta = (float)Math.Sin(theta);
            //xWorld = xLocal * cosTheta + zLocal * sinTheta + xLocalOri;
            //zWorld = xLocal * sinTheta - zLocal * cosTheta + zLocalOri;
            xWorld = xLocal * cosTheta - zLocal * sinTheta + xLocalOri;
            zWorld = xLocal * sinTheta + zLocal * cosTheta + zLocalOri;
        }

        /// <summary>
        /// 本地坐标系到世界坐标系（仿真场地中心为原点，向右为正X，向下为Z的坐标系）的变换
        /// </summary>
        /// <param name="theta">本地坐标系X轴在世界坐标系中的角度值</param>
        /// <param name="localOri">输入参数，本地坐标系原点在世界坐标系下的xna.Vector3坐标值</param>
        /// <param name="local">输入参数，待变换坐标的点在本地坐标系下的xna.Vector3坐标值</param>
        /// <param name="world">输出参数，待变换坐标的点在世界坐标系下的xna.Vector3坐标值</param>
        public static void CoordinateTransformation(float theta, xna.Vector3 localOri, xna.Vector3 local, ref xna.Vector3 world)
        {
            CoordinateTransformation(theta, localOri.X, localOri.Z, local.X, local.Z, ref world.X, ref world.Z);
        }

        /// <summary>
        /// 世界坐标系（仿真场地中心为原点，向右为正X，向下为Z的坐标系）到本地坐标系的变换
        /// </summary>
        /// <param name="theta">本地坐标系正X在世界坐标系中的角度值</param>
        /// <param name="xLocalOri">输入参数，本地坐标系原点在世界坐标系下的X坐标值</param>
        /// <param name="zLocalOri">输入参数，本地坐标系原点在世界坐标系下的Z坐标值</param>
        /// <param name="xLocal">输出参数，待变换坐标的点在本地坐标系下的X坐标值</param>
        /// <param name="zLocal">输出参数，待变换坐标的点在本地坐标系下的Z坐标值</param>
        /// <param name="xWorld">输入参数，待变换坐标的点在世界坐标系下的X坐标值</param>
        /// <param name="zWorld">输入参数，待变换坐标的点在世界坐标系下的Z坐标值</param>
        public static void CoordinateTransformation(float theta, float xLocalOri, float zLocalOri, ref float xLocal, ref float zLocal, float xWorld, float zWorld)
        {
            float cosTheta = (float)Math.Cos(theta);
            float sinTheta = (float)Math.Sin(theta);
            //xLocal = xWorld * cosTheta - zWorld * sinTheta - xLocalOri * cosTheta + zLocalOri * sinTheta;
            //zLocal = xWorld * sinTheta + zWorld * cosTheta - xLocalOri * sinTheta - zLocalOri * cosTheta;
            xLocal = xWorld * cosTheta + zWorld * sinTheta - xLocalOri * cosTheta - zLocalOri * sinTheta;
            zLocal = -xWorld * sinTheta + zWorld * cosTheta + xLocalOri * sinTheta - zLocalOri * cosTheta;
        }

        /// <summary>
        /// 世界坐标系（仿真场地中心为原点，向右为正X，向下为Z的坐标系）到本地坐标系的变换
        /// </summary>
        /// <param name="theta">本地坐标系X轴在世界坐标系中的角度值</param>
        /// <param name="localOri">输入参数，本地坐标系原点在世界坐标系下的xna.Vector3坐标值</param>
        /// <param name="local">输出参数，待变换坐标的点在本地坐标系下的xna.Vector3坐标值</param>
        /// <param name="world">输入参数，待变换坐标的点在世界坐标系下的xna.Vector3坐标值</param>
        public static void CoordinateTransformation(float theta, xna.Vector3 localOri, ref xna.Vector3 local, xna.Vector3 world)
        {
            CoordinateTransformation(theta, localOri.X, localOri.Z, ref local.X, ref local.Z, world.X, world.Z);
        }
        #endregion

        #region 仿真机器鱼/水球/障碍物参数合法性检查 Added by LiYoubing 20110630
        /// <summary>
        /// 检查仿真机器鱼的位置参数合法性 若超出场地则调整到场地内
        /// </summary>
        /// <param name="obj">待检查位置参数合法性的方形障碍物对象</param>
        public static void ParametersCheckingRoboFish(ref RoboFish obj)
        {
            Field f = Field.Instance();
            // 执行重绘动作更新当前仿真机器鱼的PolygonVertices值
            MyMission.Instance().IMissionRef.Draw();
            int tmp = 0;
            int minX = (int)UrwpgSimHelper.Min(obj.PolygonVertices[0].X, obj.PolygonVertices[1].X,
                obj.PolygonVertices[2].X, obj.PolygonVertices[3].X, ref tmp);
            if (minX < Field.Instance().LeftMm)
            {// 出了左边界
                obj.PositionMm.X += Field.Instance().LeftMm - minX;       // 右移超出的距离
            }
            else
            {// 没出左边界
                int maxX = (int)UrwpgSimHelper.Max(obj.PolygonVertices[0].X, obj.PolygonVertices[1].X,
                    obj.PolygonVertices[2].X, obj.PolygonVertices[3].X, ref tmp);
                if (maxX > Field.Instance().RightMm)
                {// 出了右边界
                    obj.PositionMm.X -= maxX - Field.Instance().RightMm;  // 左移超出的距离
                }
            }

            int minZ = (int)UrwpgSimHelper.Min(obj.PolygonVertices[0].Z, obj.PolygonVertices[1].Z,
                obj.PolygonVertices[2].Z, obj.PolygonVertices[3].Z, ref tmp);
            if (minZ < Field.Instance().TopMm)
            {// 出了上边界
                obj.PositionMm.Z += Field.Instance().TopMm - minZ;       // 下移超出的距离
            }
            else
            {// 没出上边界
                int maxZ = (int)UrwpgSimHelper.Max(obj.PolygonVertices[0].Z, obj.PolygonVertices[1].Z,
                    obj.PolygonVertices[2].Z, obj.PolygonVertices[3].Z, ref tmp);
                if (maxZ > Field.Instance().BottomMm)
                {// 出了下边界
                    obj.PositionMm.Z -= maxZ - Field.Instance().BottomMm;  // 上移超出的距离
                }
            }
        }

        /// <summary>
        /// 检查仿真水球位置参数合法性 若超出场地则调整到场地内
        /// </summary>
        /// <param name="obj"></param>
        public static void ParametersCheckingBall(ref Ball obj)
        {
            Field f = Field.Instance();
            // 确保障碍物半径不超过Z方向长度的一半
            obj.RadiusMm = (obj.RadiusMm > f.FieldLengthZMm / 2) ? f.FieldLengthZMm / 2 : obj.RadiusMm;
            if (obj.PositionMm.Z < f.TopMm + obj.RadiusMm)
            {// 出了上边界
                obj.PositionMm.Z = f.TopMm + obj.RadiusMm;           // 下移到刚好进入上边界
            }
            else
            {// 没出上边界
                if (obj.PositionMm.Z > f.BottomMm - obj.RadiusMm)
                {// 出了下边界
                    obj.PositionMm.Z = f.BottomMm - obj.RadiusMm;    // 上移到刚好进入下边界
                }
            }

            if (MyMission.Instance().ParasRef.IsGoalBlockNeeded)//需要绘制球门块项目的场地边界检测方法 added by zhangbo 20111110
            {
                if ((obj.PositionMm.Z > -f.GoalWidthMm) && (obj.PositionMm.Z < f.GoalWidthMm))
                {// 仿真水球目标位置Z坐标在球门上下坐标之间，即目标位置在左右球门上下边界连成的矩形区域内
                    if (obj.PositionMm.X < f.LeftMm + obj.RadiusMm)
                    {// 出了左边界
                        obj.PositionMm.X = f.LeftMm + obj.RadiusMm;       // 右移到刚好进入左边界
                    }
                    else
                    {// 没出左边界
                        if (obj.PositionMm.X > f.RightMm - obj.RadiusMm)
                        {// 出了右边界
                            obj.PositionMm.X = f.RightMm - obj.RadiusMm;    // 左移到刚好进入右边界
                        }
                    }
                }
                else
                {// 仿真水球目标位置Z坐标不在球门上下坐标之间，即目标位置在左右球门上下边界连成的矩形区域之外
                    if (obj.PositionMm.X < f.LeftMm + f.GoalDepthMm + obj.RadiusMm)
                    {// 出了左边球门块右边界
                        obj.PositionMm.X = f.LeftMm + f.GoalDepthMm + obj.RadiusMm;       // 右移到刚好进入左边界
                    }
                    else
                    {// 没出左边球门块右边界
                        if (obj.PositionMm.X > f.RightMm - f.GoalDepthMm - obj.RadiusMm)
                        {// 出了右边球门块左边界
                            obj.PositionMm.X = f.RightMm - f.GoalDepthMm - obj.RadiusMm;    // 左移到刚好进入右边界
                        }
                    }
                }
            }
            else//不需要球门块项目的场地边界检测方法 added by zhangbo 20111110
            {
                if (obj.PositionMm.X < f.LeftMm + obj.RadiusMm)
                {// 出了左边界
                    obj.PositionMm.X = f.LeftMm + obj.RadiusMm;       // 右移到刚好进入左边界
                }
                else
                {// 没出左边界
                    if (obj.PositionMm.X > f.RightMm - obj.RadiusMm)
                    {// 出了右边界
                        obj.PositionMm.X = f.RightMm - obj.RadiusMm;    // 左移到刚好进入右边界
                    }
                }
            }
        }

        /// <summary>
        /// 检查仿真方形障碍物的尺寸和位置参数合法性 若超出场地则调整到场地内
        /// </summary>
        /// <param name="obj">待检查尺寸和位置参数合法性的方形障碍物对象</param>
        public static void ParametersCheckingObstacle(ref RectangularObstacle obj)
        {
            Field f = Field.Instance();
            // 确保障碍物长度不超过场地X方向长度
            obj.LengthMm = (obj.LengthMm > f.FieldLengthXMm) ? f.FieldLengthXMm : obj.LengthMm;
            // 确保障碍物宽度不超过场地Z方向长度
            obj.WidthMm = (obj.WidthMm > f.FieldLengthZMm) ? f.FieldLengthZMm : obj.WidthMm;
            // 根据新的位置坐标和尺寸参数更新碰撞检测参数即4个顶点构成的列表
            obj.CalculateCollisionDetectionParas();
            int tmp = 0;
            int minX = (int)UrwpgSimHelper.Min(obj.PolygonVertices[0].X, obj.PolygonVertices[1].X,
                obj.PolygonVertices[2].X, obj.PolygonVertices[3].X, ref tmp);
            if (minX < Field.Instance().LeftMm)
            {// 出了左边界
                obj.PositionMm.X += Field.Instance().LeftMm - minX;       // 右移超出的距离
            }
            else
            {// 没出左边界
                int maxX = (int)UrwpgSimHelper.Max(obj.PolygonVertices[0].X, obj.PolygonVertices[1].X,
                    obj.PolygonVertices[2].X, obj.PolygonVertices[3].X, ref tmp);
                if (maxX > Field.Instance().RightMm)
                {// 出了右边界
                    obj.PositionMm.X -= maxX - Field.Instance().RightMm;  // 左移超出的距离
                }
            }

            int minZ = (int)UrwpgSimHelper.Min(obj.PolygonVertices[0].Z, obj.PolygonVertices[1].Z,
                obj.PolygonVertices[2].Z, obj.PolygonVertices[3].Z, ref tmp);
            if (minZ < Field.Instance().TopMm)
            {// 出了上边界
                obj.PositionMm.Z += Field.Instance().TopMm - minZ;       // 下移超出的距离
            }
            else
            {// 没出上边界
                int maxZ = (int)UrwpgSimHelper.Max(obj.PolygonVertices[0].Z, obj.PolygonVertices[1].Z,
                    obj.PolygonVertices[2].Z, obj.PolygonVertices[3].Z, ref tmp);
                if (maxZ > Field.Instance().BottomMm)
                {// 出了下边界
                    obj.PositionMm.Z -= maxZ - Field.Instance().BottomMm;  // 上移超出的距离
                }
            }
        }

        /// <summary>
        /// 检查仿真圆形障碍物的尺寸和位置参数合法性 若超出场地则调整到场地内
        /// </summary>
        /// <param name="obj"></param>
        public static void ParametersCheckingObstacle(ref RoundedObstacle obj)
        {
            Field f = Field.Instance();
            // 确保障碍物半径不超过Z方向长度的一半
            obj.RadiusMm = (obj.RadiusMm > f.FieldLengthZMm / 2) ? f.FieldLengthZMm / 2 : obj.RadiusMm;
            if (obj.PositionMm.Z < f.TopMm + obj.RadiusMm)
            {// 出了上边界
                obj.PositionMm.Z = f.TopMm + obj.RadiusMm;           // 下移到刚好进入上边界
            }
            else
            {// 没出上边界
                if (obj.PositionMm.Z > f.BottomMm - obj.RadiusMm)
                {// 出了下边界
                    obj.PositionMm.Z = f.BottomMm - obj.RadiusMm;    // 上移到刚好进入下边界
                }
            }

            if ((obj.PositionMm.Z > -f.GoalWidthMm) && (obj.PositionMm.Z < f.GoalWidthMm))
            {// 圆形障碍物目标位置Z坐标在球门上下坐标之间，即目标位置在左右球门上下边界连成的矩形区域内
                if (obj.PositionMm.X < f.LeftMm + obj.RadiusMm)
                {// 出了左边界
                    obj.PositionMm.X = f.LeftMm + obj.RadiusMm;       // 右移到刚好进入左边界
                }
                else
                {// 没出左边界
                    if (obj.PositionMm.X > f.RightMm - obj.RadiusMm)
                    {// 出了右边界
                        obj.PositionMm.X = f.RightMm - obj.RadiusMm;    // 左移到刚好进入右边界
                    }
                }
            }
            else
            {// 圆形障碍物目标位置Z坐标不在球门上下坐标之间，即目标位置在左右球门上下边界连成的矩形区域之外
                if (obj.PositionMm.X < f.LeftMm + f.GoalDepthMm + obj.RadiusMm)
                {// 出了左边球门块右边界
                    obj.PositionMm.X = f.LeftMm + f.GoalDepthMm + obj.RadiusMm;       // 右移到刚好进入左边界
                }
                else
                {// 没出左边球门块右边界
                    if (obj.PositionMm.X > f.RightMm - f.GoalDepthMm - obj.RadiusMm)
                    {// 出了右边球门块左边界
                        obj.PositionMm.X = f.RightMm - f.GoalDepthMm - obj.RadiusMm;    // 左移到刚好进入右边界
                    }
                }
            }
        }
        #endregion
    }
}