//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Locomotion.cs
// Date: 20101128  Author: Renjing  Version: 1
// Description: 碰撞检测相关类定义文件
// Histroy:
// Date: 20110511  Author: LiYoubing
// Modification: 
// 1.为和仿真机器鱼有关的碰撞检测过程添加仿真机器鱼碰撞状态标志
// 2.受RoboFish的碰撞状态标志改成标志列表的影响 标志值的比较改用List.Contains方法进行
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using xna = Microsoft.Xna.Framework;

using URWPGSim2D.Core;

namespace URWPGSim2D.Common
{
    /// <summary>
    /// 碰撞检测计算静态类
    /// </summary>
    public static class CollisionDetection
    {

        #region 碰撞检测三个基本方法--多边形和多边形、多边形和圆、圆和圆
        /// <summary>
        /// 对两个多边形模型进行碰撞检测，这里设第1个多边形是动态对象的模型，返回碰撞检测结果参数结构体
        /// </summary>
        /// <param name="polygonA">待检测的第1个多边形模型对象</param>
        /// <param name="polygonB">待检测的第2个多边形模型对象</param>
        /// <param name="staticStatus">第2个多边形是否是静态对象的模型</param>
        /// <returns>碰撞检测结果参数结构体</returns>
        public static CollisionDetectionResult CollisionBetweenTwoPolygons(
            CollisionModelPolygon polygonA, CollisionModelPolygon polygonB, bool staticStatus)
        {
            CollisionDetectionResult result = new CollisionDetectionResult();
            result.Intersect = false;
            int edgeCountA = polygonA.Edges.Count;  // 记录A多边形的边数
            int edgeCountB = polygonB.Edges.Count;  // 记录B多边形的边数
            float minIntervalDistance = float.PositiveInfinity; // 定义最小重叠线段
            xna.Vector3 edge;                       // 定义多边形的边
            xna.Vector3 collisionPoint = new xna.Vector3();     // 两多边形重叠时的碰撞点
            xna.Vector3 point = new xna.Vector3(0, 0, 0);       // 中间变量
            float intervalDistance = 0;               // 重叠线段的长度

            for (int edgeIndex = 0; edgeIndex < edgeCountA + edgeCountB; edgeIndex++)
            {// 找出两物体所有的边
                // LiYoubing 20101211
                // 不加此条语句则只要第一次检测结果发现有重叠，Intersect置为true后
                // 以后的循环中即使再检测出无重叠，Intersect也不会被置回false，无法跳出循环，便错误地判断为发生了碰撞
                // 因为要两个多边形在所有边的垂线上投影都有重叠才能判断发生了碰撞，所以每次循环都把Intersect先置为false
                // 然后检测到重叠便置为true，如果有碰撞发生，所有循环都能将Intersect置为true，能正确反应结果
                // 若有某一次循环没有检测到重叠，则Intersect为false，判断没有碰撞发生，跳出循环
                result.Intersect = false;

                if (edgeIndex < edgeCountA)
                {
                    edge = polygonA.Edges[edgeIndex];
                }
                else
                {
                    edge = polygonB.Edges[edgeIndex - edgeCountA];
                }

                // ===== 1. 对所有边做垂线 =====
                xna.Vector3 axis = new xna.Vector3(-edge.Z, edge.Y, edge.X);
                if (axis.Length()!=0)
                {
                    axis.Normalize();
                }

                // ===== 2. 把两个物体投影到垂线上 =====
                float minA = 0; float maxA = 0; float minB = 0; float maxB = 0;
                xna.Vector3 minAPoint = polygonA.Points[0];
                xna.Vector3 maxAPoint = polygonA.Points[0];
                xna.Vector3 minBPoint = polygonB.Points[0];
                xna.Vector3 maxBPoint = polygonB.Points[0];
                ProjectPolygonToAxis(axis, polygonA, ref minA, ref maxA, ref minAPoint, ref maxAPoint);
                ProjectPolygonToAxis(axis, polygonB, ref minB, ref maxB, ref minBPoint, ref maxBPoint);

                // ===== 3. 判断二者的投影是否有重叠，并记录最小重叠线段的长度以及重叠时发生碰撞的点的坐标 =====
                if (minA < minB)
                {
                    if ((minB - maxA) <= 0)
                    {
                        result.Intersect = true;
                        intervalDistance = minB - maxA;
                        point = maxAPoint;      // 记录点的坐标 
                    }
                }
                else
                {
                    if ((minA - maxB) <= 0)
                    {
                        result.Intersect = true;
                        intervalDistance = minA - maxB;
                        point = minAPoint;
                    }
                }

                if (!result.Intersect) break;   // 存在一个投影不重叠，说明两个多边形没有发生碰撞

                intervalDistance = Math.Abs(intervalDistance);          // 取重叠线段的绝对值
                if (intervalDistance < minIntervalDistance)
                {// 找到最小重叠距离
                    minIntervalDistance = intervalDistance;
                    result.NormalAxis = axis;   // 储存作用面中的法线

                    xna.Vector3 d = polygonA.Center - polygonB.Center;  // 两物体中心连线的向量
                    if (xna.Vector3.Dot(d, result.NormalAxis) < 0)       // 如果法线方向和中心连线方向夹角大于90°
                    {
                        result.NormalAxis = -result.NormalAxis;         // 法线取相反方向
                    }
                    collisionPoint = point;     // 找到两多边形重叠时碰撞点的坐标
                }
            }

            if (result.Intersect)
            {
                // 找到最小重叠线段所在的向量
                result.MinimumTranslationVector = xna.Vector3.Multiply(result.NormalAxis, minIntervalDistance);
                if (staticStatus == true)
                {// 对象B是静态的
                    result.ActionPoint = xna.Vector3.Add(result.MinimumTranslationVector, collisionPoint); // 找到碰撞点
                }
                else
                {// 对象B是动态的
                    result.ActionPoint = xna.Vector3.Add(collisionPoint, 
                        xna.Vector3.Multiply(result.MinimumTranslationVector, 1 / 2));  // 找到碰撞点
                }
            }
            return result;
        }

        /// <summary>
        /// 对一个多边形和一个圆形模型进行碰撞检测，返回碰撞检测结果参数结构体
        /// </summary>
        /// <param name="polygon">待测多边形的模型对象</param>
        /// <param name="ball">待测球的模型对象</param>
        /// <param name="staticStatus">待测多边形的状态为静态或是动态</param>
        /// <returns>返回碰撞检测结果结构体</returns>
        public static CollisionDetectionResult CollisionBetweenPolygonAndCircle(
            CollisionModelPolygon polygon, Ball ball, bool staticStatus)
        {
            CollisionDetectionResult result = new CollisionDetectionResult();
            result.Intersect = false;
            int edgeCount = polygon.Edges.Count;  // 记录A多边形的边数
            float minIntervalDistance = float.PositiveInfinity; // 定义最小重叠线段
            xna.Vector3 edge;                       // 定义多边形的边
            int edgeIndexNext;                      //中间变量
            xna.Vector3[] voronoiTestLine = new xna.Vector3[edgeCount];//圆心到多边形个物体之间的连线向量
            xna.Vector3 collisionPoint = new xna.Vector3();     // 两多边形重叠时的碰撞点
            xna.Vector3 point = new xna.Vector3(0, 0, 0);       // 中间变量
            float intervalDistance = 0;               // 重叠线段的长度

            for (int edgeIndex = 0; edgeIndex < edgeCount; edgeIndex++)
            {// 找到每条边
                result.Intersect = false;   // LiYoubing 20101211

                edge = polygon.Edges[edgeIndex];
                if (edge.Length()!=0)
                {
                    edge.Normalize();
                }
                edgeIndexNext = (edgeIndex + 1) % edgeCount;    // LiYoubing 20101211

                // LiYoubing 20101211 begin
                //polygon.Edges[edgeIndexNext] = new xna.Vector3(-polygon.Edges[edgeIndexNext].X, 0, -polygon.Edges[edgeIndexNext].Z);
                xna.Vector3 tmpEdge = new xna.Vector3(-polygon.Edges[edgeIndexNext].X, 0, -polygon.Edges[edgeIndexNext].Z);
                if (tmpEdge.Length()!=0)
                {
                    tmpEdge.Normalize();
                }
                // LiYoubing 20101211 end

                voronoiTestLine[edgeIndex] = xna.Vector3.Subtract(ball.PositionMm, polygon.Points[edgeIndexNext]);
                if (voronoiTestLine[edgeIndex].Length()!=0)
                {
                    voronoiTestLine[edgeIndex].Normalize();
                }
                float tmpAngle1 = xna.Vector3.Dot(edge, voronoiTestLine[edgeIndex]);//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                if (tmpAngle1 > 1)
                {
                    tmpAngle1 = 1;
                }
                else if (tmpAngle1 < -1)
                {
                    tmpAngle1 = -1;
                }
                double angleBetweenSideAndLineA = Math.Acos(tmpAngle1);

                // LiYoubing 20101211 begin
                //double angleBetweenSideAndLineB = Math.Acos(xna.Vector3.Dot(polygon.Edges[edgeIndexNext], voronoiTestLine[edgeIndex]));
                //double angleBetweenSideAndSide = Math.Acos(xna.Vector3.Dot(edge, polygon.Edges[edgeIndexNext]));
                float tmpAngle2 = xna.Vector3.Dot(tmpEdge, voronoiTestLine[edgeIndex]);//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                if (tmpAngle2 > 1)
                {
                    tmpAngle2 = 1;
                }
                else if (tmpAngle2 < -1)
                {
                    tmpAngle2 = -1;
                }
                double angleBetweenSideAndLineB = Math.Acos(tmpAngle2);
                float tmpAngle3 = xna.Vector3.Dot(edge, tmpEdge);//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                if (tmpAngle3 > 1)
                {
                    tmpAngle3 = 1;
                }
                else if (tmpAngle3 < -1)
                {
                    tmpAngle3 = -1;
                }
                double angleBetweenSideAndSide = Math.Acos(tmpAngle3);
                // LiYoubing 20101211 end

                if (angleBetweenSideAndLineA <= angleBetweenSideAndSide && angleBetweenSideAndLineB <= angleBetweenSideAndSide)
                {//======判断球心是否在voronoi regions里=======
                    float distance = xna.Vector3.Distance(ball.PositionMm, polygon.Points[edgeIndexNext]);
                    if ((int)distance <= ball.RadiusMm)
                    {
                        result.Intersect = true;
                        result.NormalAxis = xna.Vector3.Subtract(ball.PositionMm, polygon.Points[edgeIndexNext]);
                        if (result.NormalAxis.Length()!=0)
                        {
                            result.NormalAxis.Normalize();  // LiYoubing 20101211
                        }
                        collisionPoint = polygon.Points[edgeIndexNext];
                        minIntervalDistance = ball.RadiusMm - distance;//最小相交距离，modified by renjing 20110329
                        break;
                    }
                }
                else
                {
                    // ===== 1. 对所有边做垂线 =====
                    xna.Vector3 axis = new xna.Vector3(-edge.Z, edge.Y, edge.X);
                    if (axis.Length()!=0)
                    {
                        axis.Normalize();
                    }

                    // ===== 2. 把两个物体投影到垂线上 =====
                    float minA = 0; float maxA = 0; float minB = 0; float maxB = 0;
                    xna.Vector3 minPoint = polygon.Points[0];
                    xna.Vector3 maxPoint = polygon.Points[0];
                    ProjectPolygonToAxis(axis, polygon, ref minA, ref maxA, ref minPoint, ref maxPoint);
                    ProjectCircleToAxis(axis, ball, ref minB, ref maxB);

                    // ===== 3. 判断二者的投影是否有重叠，并记录最小重叠线段的长度以及重叠时发生碰撞的点的坐标 =====
                    if (minA < minB)
                    {
                        if ((minB - maxA) <= 0)
                        {
                            result.Intersect = true;
                            intervalDistance = minB - maxA;
                            point = maxPoint;      // 记录点的坐标 
                        }
                    }
                    else
                    {
                        if ((minA - maxB) <= 0)
                        {
                            result.Intersect = true;
                            intervalDistance = minA - maxB;
                            point = minPoint;
                        }
                    }

                    if (!result.Intersect) break;   // 存在一个投影不重叠，说明两个多边形没有发生碰撞

                    intervalDistance = Math.Abs(intervalDistance);          // 取重叠线段的绝对值
                    if (intervalDistance < minIntervalDistance)
                    {// 找到最小重叠距离
                        minIntervalDistance = intervalDistance;
                        result.NormalAxis = axis;   // 储存作用面中的法线
                        collisionPoint = point;     // 找到两多边形重叠时碰撞点的坐标
                    }
                }
            }
            if (result.Intersect)
            {
                xna.Vector3 d = polygon.Center - ball.PositionMm;   // 两物体中心连线的向量
                if (xna.Vector3.Dot(d, result.NormalAxis) < 0)      // 如果法线方向和中心连线方向夹角大于90°
                {
                    result.NormalAxis = -result.NormalAxis;         // 法线取相反方向
                }
                // 找到最小重叠线段所在的向量
                result.MinimumTranslationVector = xna.Vector3.Multiply(result.NormalAxis, minIntervalDistance);
                if (staticStatus == true)
                {
                    result.ActionPoint = xna.Vector3.Subtract(ball.PositionMm, result.MinimumTranslationVector); // 找到碰撞点
                }
                else
                {
                    result.ActionPoint = xna.Vector3.Subtract(ball.PositionMm, result.MinimumTranslationVector / 2); // 找到碰撞点
                }
            }
            return result;
        }

        /// <summary>
        /// 检测两圆形对象的碰撞情况,设定第一个圆恒为动态的
        /// </summary>
        /// <param name="center1">待测第一个圆的圆心点</param>
        /// <param name="radius1">待测第一个圆的半径</param>
        /// <param name="center2">待测第二个圆的圆心点</param>
        /// <param name="radius2">待测第二个圆的半径</param>
        /// <returns>返回碰撞检测结果结构体</returns>
        public static CollisionDetectionResult CollisionBetweenTwoCircles(
            xna.Vector3 center1, int radius1, xna.Vector3 center2, int radius2)
        {
            CollisionDetectionResult result = new CollisionDetectionResult();
            result.Intersect = false;
            float distance = xna.Vector3.Distance(center1, center2);
            if (distance <= (radius1 + radius2))
            {
                result.Intersect = true;
                result.NormalAxis = xna.Vector3.Subtract(center1, center2);
                if (result.NormalAxis.Length() != 0)
                {
                    result.NormalAxis.Normalize();
                }
                result.ActionPoint = xna.Vector3.Add(center2, xna.Vector3.Multiply(result.NormalAxis, (float)radius2));
            }
            return result;
        }

        /// <summary>
        /// 对一个多边形和一个圆形障碍物进行碰撞检测，返回碰撞检测结果参数结构体
        /// </summary>
        /// <param name="polygon">待测多边形的模型对象</param>
        /// <param name="roundedObstacle">待测圆形障碍物的模型对象</param>
        /// <returns>返回碰撞检测结果结构体</returns>
        public static CollisionDetectionResult CollisionBetweenPolygonAndRoundedObstacle(
            CollisionModelPolygon polygon, RoundedObstacle roundedObstacle)
        {
            CollisionDetectionResult result = new CollisionDetectionResult();   //Chen Penghui
            result.Intersect = false;
            int edgeCount = polygon.Edges.Count;  // 记录A多边形的边数
            float minIntervalDistance = float.PositiveInfinity; // 定义最小重叠线段
            xna.Vector3 edge;                       // 定义多边形的边
            int edgeIndexNext;                      //中间变量
            xna.Vector3[] voronoiTestLine = new xna.Vector3[edgeCount];//圆心到多边形各顶点之间的连线向量
            //xna.Vector3 collisionPoint = new xna.Vector3();     // 两多边形重叠时的碰撞点
            //xna.Vector3 point = new xna.Vector3(0, 0, 0);       // 中间变量
            float intervalDistance = 0;               // 重叠线段的长度          

            for (int edgeIndex = 0; edgeIndex < edgeCount; edgeIndex++)
            {//找到每条边
                edge = polygon.Edges[edgeIndex];
                if (edge.Length() != 0)
                {
                    edge.Normalize();
                }
                if (edgeIndex == edgeCount)
                {
                    edgeIndexNext = 0;
                }
                else
                {
                    edgeIndexNext = edgeIndex + 1;
                }

                polygon.Edges[edgeIndexNext] = new xna.Vector3(-polygon.Edges[edgeIndexNext].X, 0, -polygon.Edges[edgeIndexNext].Z);
                voronoiTestLine[edgeIndex] = xna.Vector3.Subtract(roundedObstacle.PositionMm, polygon.Points[edgeIndexNext]);
                if (voronoiTestLine[edgeIndex].Length() != 0)
                {
                    voronoiTestLine[edgeIndex].Normalize();
                }
                xna.Vector3 tmp = new xna.Vector3();//中间变量，polygon.Edges[edgeIndexNext]标准化，否则下面的Math.Acos计算出错
                tmp = polygon.Edges[edgeIndexNext];
                if (tmp.Length() != 0)
                {
                    tmp.Normalize();
                }
                float tmpAngle1 = xna.Vector3.Dot(edge, voronoiTestLine[edgeIndex]);//先计算两向量夹角的余弦值，有可能超过-1~1的范围
                if (tmpAngle1 > 1)
                {
                    tmpAngle1 = 1;
                }
                else if (tmpAngle1 < -1)
                {
                    tmpAngle1 = -1;
                }
                double angleBetweenSideAndLineA = Math.Acos(tmpAngle1);
                float tmpAngle2 = xna.Vector3.Dot(tmp, voronoiTestLine[edgeIndex]);//先计算两向量夹角的余弦值，有可能超过-1~1的范围 by renjing 2011-02-23
                if (tmpAngle2 > 1)
                {
                    tmpAngle2 = 1;
                }
                else if (tmpAngle2 < -1)
                {
                    tmpAngle2 = -1;
                }
                double angleBetweenSideAndLineB = Math.Acos(tmpAngle2);
                float tmpAngle3 = xna.Vector3.Dot(edge, tmp);
                if (tmpAngle3 > 1)
                {
                    tmpAngle3 = 1;
                }
                else if (tmpAngle3 < -1)
                {
                    tmpAngle3 = -1;
                }
                double angleBetweenSideAndSide = Math.Acos(tmpAngle3);
                if (angleBetweenSideAndLineA <= angleBetweenSideAndSide && angleBetweenSideAndLineB <= angleBetweenSideAndSide)
                {//======判断球心是否在voronoi regions里=======
                    float distance = xna.Vector3.Distance(roundedObstacle.PositionMm, polygon.Points[edgeIndexNext]);
                    if ((int)distance <= roundedObstacle.RadiusMm)
                    {
                        result.Intersect = true;
                        result.NormalAxis = xna.Vector3.Subtract(roundedObstacle.PositionMm, polygon.Points[edgeIndexNext]);
                        //collisionPoint = polygon.Points[edgeIndexNext];
                        minIntervalDistance = roundedObstacle.RadiusMm - distance;//最小相交距离，modified by renjing 20110329
                        break;
                    }
                }
                else
                {
                    // ===== 1. 对所有边做垂线 =====
                    xna.Vector3 axis = new xna.Vector3(-edge.Z, edge.Y, edge.X);
                    if (axis.Length() != 0)
                    {
                        axis.Normalize();
                    }

                    // ===== 2. 把两个物体投影到垂线上 =====
                    float minA = 0; float maxA = 0; float minB = 0; float maxB = 0;
                    xna.Vector3 minPoint = polygon.Points[0];
                    xna.Vector3 maxPoint = polygon.Points[0];
                    ProjectPolygonToAxis(axis, polygon, ref minA, ref maxA, ref minPoint, ref maxPoint);
                    ProjectRoundedObstacleToAxis(axis, roundedObstacle, ref minB, ref maxB);

                    // ===== 3. 判断二者的投影是否有重叠，并记录最小重叠线段的长度以及重叠时发生碰撞的点的坐标 =====
                    if (minA < minB)
                    {
                        if ((minB - maxA) <= 0)
                        {
                            result.Intersect = true;
                            intervalDistance = minB - maxA;
                            //point = maxPoint;      // 记录点的坐标 
                        }
                    }
                    else
                    {
                        if ((minA - maxB) <= 0)
                        {
                            result.Intersect = true;
                            intervalDistance = minA - maxB;
                            //point = minPoint;
                        }
                    }

                    if (!result.Intersect) break;   // 存在一个投影不重叠，说明两个多边形没有发生碰撞

                    intervalDistance = Math.Abs(intervalDistance);          // 取重叠线段的绝对值
                    if (intervalDistance < minIntervalDistance)
                    {// 找到最小重叠距离
                        minIntervalDistance = intervalDistance;
                        result.NormalAxis = axis;   // 储存作用面中的法线
                        //collisionPoint = point;     // 找到两多边形重叠时碰撞点的坐标
                    }
                }
            }
            if (result.Intersect)
            {

                xna.Vector3 d = polygon.Center - roundedObstacle.PositionMm;  // 两物体中心连线的向量
                if (xna.Vector3.Dot(d, result.NormalAxis) < 0)       // 如果法线方向和中心连线方向夹角大于90°
                {
                    result.NormalAxis = -result.NormalAxis;         // 法线取相反方向
                }
                // 找到最小重叠线段所在的向量
                result.MinimumTranslationVector = xna.Vector3.Multiply(result.NormalAxis, minIntervalDistance);
                result.ActionPoint = xna.Vector3.Add(roundedObstacle.PositionMm, result.MinimumTranslationVector); // 找到碰撞点
            }
            return result;
        }

        /// <summary>
        /// 把多边形投影到直线上，并记录输出多边形各顶点向量和目标直线单位向量点积的最小和最大值
        /// </summary>
        /// <param name="axis">表示目标直线的单位向量</param>
        /// <param name="polygon">碰撞检测多边形模型对象</param>
        /// <param name="minDotProduct">多边形各顶点和目标直线单位向量点积的最小值 输出</param>
        /// <param name="maxDotProduct">多边形各顶点和目标直线单位向量点积的最大值 输出</param>
        /// <param name="minPoint">对应点积最小值的多边形顶点 输出</param>
        /// <param name="maxPoint">对应点积最大值的多边形顶点 输出</param>
        private static void ProjectPolygonToAxis(xna.Vector3 axis, CollisionModelPolygon polygon,
            ref float minDotProduct, ref float maxDotProduct, ref xna.Vector3 minPoint, ref xna.Vector3 maxPoint)
        {
            float d = xna.Vector3.Dot(axis, polygon.Points[0]); // 初始化
            minDotProduct = d;
            maxDotProduct = d;
            minPoint = polygon.Points[0];
            maxPoint = polygon.Points[0];

            for (int i = 1; i < polygon.Points.Count; i++)
            {
                d = xna.Vector3.Dot(axis, polygon.Points[i]);
                if (d < minDotProduct)
                {
                    minDotProduct = d;
                    minPoint = polygon.Points[i];
                }
                else
                {
                    if (d > maxDotProduct)
                    {
                        maxDotProduct = d;
                        maxPoint = polygon.Points[i];
                    }
                }
            }
        }

        /// <summary>
        /// 把圆投影到直线上，并记录输出多边形各顶点向量和目标直线单位向量点积的最小和最大值
        /// </summary>
        /// <param name="axis">表示目标直线的单位向量</param>
        /// <param name="ball">碰撞检测球的模型对象</param>
        /// <param name="minDotProduct">球和目标直线单位向量点积的最小值 输出</param>
        /// <param name="maxDotProduct">球和目标直线单位向量点积的最大值 输出</param>
        private static void ProjectCircleToAxis(xna.Vector3 axis, Ball ball,
             ref float minDotProduct, ref float maxDotProduct)
        {
            xna.Vector3 tmpVector = xna.Vector3.Multiply(axis, (float)ball.RadiusMm);
            xna.Vector3 minPoint = xna.Vector3.Subtract(ball.PositionMm, tmpVector);
            xna.Vector3 maxPoint = xna.Vector3.Add(ball.PositionMm, tmpVector);

            minDotProduct = xna.Vector3.Dot(axis, minPoint);
            maxDotProduct = xna.Vector3.Dot(axis, maxPoint);

            if (minDotProduct > maxDotProduct)
            {
                float dotProduct = maxDotProduct;
                maxDotProduct = minDotProduct;
                minDotProduct = dotProduct;
            }
        }

        /// <summary>
        /// 把圆形障碍物投影到直线上，并记录输出多边形各顶点向量和目标直线单位向量点积的最小和最大值
        /// </summary>
        /// <param name="axis">表示目标直线的单位向量</param>
        /// <param name="roundedObstacle">碰撞检测圆形障碍物的模型对象</param>
        /// <param name="minDotProduct">圆形障碍物和目标直线单位向量点积的最小值 输出</param>
        /// <param name="maxDotProduct">圆形障碍物和目标直线单位向量点积的最大值 输出</param>
        private static void ProjectRoundedObstacleToAxis(xna.Vector3 axis, RoundedObstacle roundedObstacle,
             ref float minDotProduct, ref float maxDotProduct)//Chen Penghui
        {
            xna.Vector3 minPoint = new xna.Vector3();
            xna.Vector3 maxPoint = new xna.Vector3();
            float dotProduct = 0;//中间变量
            minPoint = xna.Vector3.Subtract(roundedObstacle.PositionMm, xna.Vector3.Multiply(axis, (float)roundedObstacle.RadiusMm));
            maxPoint = xna.Vector3.Add(roundedObstacle.PositionMm, xna.Vector3.Multiply(axis, (float)roundedObstacle.RadiusMm));
            minDotProduct = xna.Vector3.Dot(axis, minPoint);
            maxDotProduct = xna.Vector3.Dot(axis, maxPoint);
            if (minDotProduct > maxDotProduct)
            {
                dotProduct = maxDotProduct;
                maxDotProduct = minDotProduct;
                minDotProduct = dotProduct;
            }
        }
        #endregion

        #region 仿真机器鱼和仿真场地元素包括边界和球门块的碰撞检测
        ///// <summary>
        ///// 检测仿真机器鱼和仿真场地边界及球门块碰撞情况
        ///// 调用上下边界检测方法，编写左右边界及四个球门块的检测方法
        ///// </summary>
        ///// <param name="fish">待检测的仿真机器鱼对象引用</param>
        ///// <param name="field">待检测仿真场地Field对象，用到四个球门块的顶点坐标列表和四个边界坐标值</param>
        ///// <returns>碰撞检测结果参数结构体包括是否碰撞/碰撞作用面中的法向/碰撞作用点坐标</returns>
        //public static CollisionDetectionResult DetectCollisionBetweenFishAndBorder(
        //    ref RoboFish fish, Field field)
        //{
        //    CollisionDetectionResult result = new CollisionDetectionResult();
        //    result.Intersect = false;
        //    List<int> listIndex = new List<int>();

        //    #region 使用外层模型检测仿真机器鱼是否没和任何边界及球门块发生碰撞，是则无需进一步处理
        //    if ((field.IsGoalBlockRegular == true) 
        //        && (fish.CollisionModelCenterPositionMm.X > (field.LeftMm + field.GoalDepthMm + fish.CollisionModelRadiusMm))
        //        && (fish.CollisionModelCenterPositionMm.X < (field.RightMm - field.GoalDepthMm - fish.CollisionModelRadiusMm))
        //        && (fish.CollisionModelCenterPositionMm.Z > (field.TopMm + fish.CollisionModelRadiusMm))
        //        && (fish.CollisionModelCenterPositionMm.Z < (field.BottomMm - fish.CollisionModelRadiusMm)))
        //    {// 仿真机器鱼外层模型检测，在左右球门块和上下边界所包围的区域内
        //        return result;
        //    }
        //    #endregion

        //    #region 使用内层模型检测仿真机器鱼是否没和任何边界及球门块发生碰撞，是则无需进一步处理
        //    // 记录仿真机器鱼内层碰撞检测四边形模型4个顶点X/Z坐标最大值和最小值的序号
        //    int maxXSeq = 0; int minXSeq = 0; int maxZSeq = 0; int minZSeq = 0;

        //    // 仿真机器鱼内层碰撞检测四边形模型4个顶点X坐标的最大值
        //    UrwpgSimHelper.Max(fish.PolygonVertices[0].X, fish.PolygonVertices[1].X,
        //        fish.PolygonVertices[2].X, fish.PolygonVertices[3].X, ref maxXSeq);

        //    // 仿真机器鱼内层碰撞检测四边形模型4个顶点X坐标的最小值
        //    UrwpgSimHelper.Min(fish.PolygonVertices[0].X, fish.PolygonVertices[1].X,
        //        fish.PolygonVertices[2].X, fish.PolygonVertices[3].X, ref minXSeq);

        //    // 仿真机器鱼内层碰撞检测四边形模型4个顶点Z坐标的最大值
        //    UrwpgSimHelper.Max(fish.PolygonVertices[0].Z, fish.PolygonVertices[1].Z,
        //        fish.PolygonVertices[2].Z, fish.PolygonVertices[3].Z, ref maxZSeq);

        //    // 仿真机器鱼内层碰撞检测四边形模型4个顶点Z坐标的最小值
        //    UrwpgSimHelper.Min(fish.PolygonVertices[0].Z, fish.PolygonVertices[1].Z,
        //        fish.PolygonVertices[2].Z, fish.PolygonVertices[3].Z, ref minZSeq);

        //    if ((field.IsGoalBlockRegular == true) 
        //        && (fish.PolygonVertices[maxXSeq].X <= field.RightMm - field.GoalDepthMm)
        //        && (fish.PolygonVertices[minXSeq].X >= field.LeftMm + field.GoalDepthMm)
        //        && (fish.PolygonVertices[maxZSeq].Z <= field.BottomMm)
        //        && (fish.PolygonVertices[minZSeq].Z >= field.TopMm))
        //    {// 仿真机器鱼鱼体上全部点的X/Z坐标区间位于左右球门块/上下边界之间
        //        return result;
        //    }
        //    #endregion

           

        //    CollisionModelPolygon polygonFish = new CollisionModelPolygon(fish.PolygonVertices);

        //    #region 仿真机器鱼和上右下左四个边界间的碰撞检测和响应
        //    if (fish.PolygonVertices[minZSeq].Z <= field.TopMm)
        //    {// 内层四边形模型4个顶点的Z坐标最小值小于场地上边界Z坐标则与上边界发生碰撞
        //        result.Intersect = true;
        //        result.NormalAxis = new xna.Vector3(0, 0, 1);   // 碰撞作用面中法向为正Z方向

        //        // 将仿真机器鱼当前绘图中心Z坐标下移正好使仿真机器鱼能在Z方向完全进入场地内的值
        //        fish.PositionMm.Z += (field.TopMm - fish.PolygonVertices[minZSeq].Z);

        //        // 碰撞作用点确定为四边形模型4个顶点中Z坐标值最小的那个沿Z轴下移到场地顶边时的值
        //        result.ActionPoint = new xna.Vector3(fish.PolygonVertices[minZSeq].X,
        //            fish.PolygonVertices[minZSeq].Y, field.TopMm);

        //        //CollisionResponseBetweenFishAndBorder(ref fish, result);
        //        listIndex.Add(0);       // 上边界编号为0
        //    }

        //    if (fish.PolygonVertices[maxXSeq].X >= field.RightMm)
        //    {// 内层四边形模型4个顶点的X坐标最大值大于场地右边界X坐标则与右边界发生碰撞
        //        result.Intersect = true;
        //        result.NormalAxis = new xna.Vector3(-1, 0, 0);// 碰撞作用面中法向为负X方向

        //        // 将仿真机器鱼当前绘图中心X坐标左移正好使仿真机器鱼能在X方向完全进入场地内的值
        //        fish.PositionMm.X -= (fish.PolygonVertices[maxXSeq].X - field.RightMm);

        //        // 碰撞作用点确定为四边形模型4个顶点中X坐标值最大的那个沿X轴左移到场地右边时的值
        //        result.ActionPoint = new xna.Vector3(field.RightMm, fish.PolygonVertices[maxXSeq].Y,
        //                                             fish.PolygonVertices[maxXSeq].Z);

        //        //CollisionResponseBetweenFishAndBorder(ref fish, result);
        //        listIndex.Add(1);       // 右边界编号为1
        //    }

        //    if (fish.PolygonVertices[maxZSeq].Z >= field.BottomMm)
        //    {// 内层四边形模型4个顶点的Z坐标最大值大于场地下边界Z坐标则与下边界发生碰撞
        //        result.Intersect = true;
        //        result.NormalAxis = new xna.Vector3(0, 0, -1);   // 碰撞作用面中法向为负Z方向

        //        // 将仿真机器鱼当前绘图中心Z坐标下移正好使仿真机器鱼能在Z方向完全进入场地内的值
        //        fish.PositionMm.Z += (field.BottomMm - fish.PolygonVertices[maxZSeq].Z);

        //        // 碰撞作用点确定为四边形模型4个顶点中Z坐标值最大的那个沿Z轴上移到场地底边时的值
        //        result.ActionPoint = new xna.Vector3(fish.PolygonVertices[maxZSeq].X,
        //            fish.PolygonVertices[maxZSeq].Y, field.BottomMm);

        //        //CollisionResponseBetweenFishAndBorder(ref fish, result);
        //        listIndex.Add(0);       // 下边界编号为2
        //    }

        //    if (fish.PolygonVertices[minXSeq].X <= field.LeftMm)
        //    {// 内层四边形模型4个顶点的X坐标最小值小于场地左边界X坐标则与左边界发生碰撞
        //        result.Intersect = true;
        //        result.NormalAxis = new xna.Vector3(1, 0, 0);// 碰撞作用面中法向为正X方向
        //        // 将仿真机器鱼当前绘图中心X坐标右移正好使仿真机器鱼能在X方向完全进入场地内的值
        //        fish.PositionMm.X += (field.LeftMm - fish.PolygonVertices[minXSeq].X);

        //        // 碰撞作用点确定为四边形模型4个顶点中X坐标值最小的那个沿X轴右移到场地左边时的值
        //        result.ActionPoint = new xna.Vector3(field.LeftMm, fish.PolygonVertices[minXSeq].Y,
        //                                            fish.PolygonVertices[minXSeq].Z);

        //        //CollisionResponseBetweenFishAndBorder(ref fish, result);
        //        listIndex.Add(3);       // 左边界编号为3
        //    }
        //    #endregion

        //    #region 仿真机器鱼已和某一边界发生碰撞后的进一步检测和响应
        //    if ((field.IsGoalBlockRegular == true) && (listIndex.Count == 1))
        //    {// 仿真机器鱼和四个边界中的某一个发生了碰撞且球门块是规则摆放的
        //        bool collided = false;
        //        switch (listIndex[0])
        //        {
        //            case 0:// 和上边界碰撞的同时只可能和左上或右上球门块碰撞
        //                collided = CollisionBetweenFishAndGoalBlock(ref fish, polygonFish,
        //                    field.BorderLeftTopVertices, field.BorderRightTopVertices);
        //                if (collided == true) return result;
        //                break;

        //            case 1:// 和右边界碰撞的同时只可能和右上或右下球门块碰撞
        //                collided = CollisionBetweenFishAndGoalBlock(ref fish, polygonFish,
        //                    field.BorderRightTopVertices, field.BorderRightBottomVertices);
        //                if (collided == true) return result;
        //                break;

        //            case 2:// 和下边界碰撞的同时只可能和右下或左下球门块碰撞
        //                collided = CollisionBetweenFishAndGoalBlock(ref fish, polygonFish,
        //                    field.BorderRightBottomVertices, field.BorderLeftBottomVertices);
        //                if (collided == true) return result;
        //                break;

        //            case 3:// 和左边界碰撞的同时只可能和左下或左上球门块碰撞
        //                collided = CollisionBetweenFishAndGoalBlock(ref fish, polygonFish,
        //                    field.BorderLeftBottomVertices, field.BorderLeftTopVertices);
        //                if (collided == true) return result;
        //                break;
        //        }
        //    }
        //    #endregion

        //    #region 仿真机器鱼未和任一边界发生碰撞则需进一步检测全部球门块
        //    // 对仿真机器鱼和左上球门块进行碰撞检测若碰撞则响应
        //    result = CollisionBetweenFishAndGoalBlock(ref fish, polygonFish, field.BorderLeftTopVertices);
        //    if (result.Intersect == true) return result;

        //    // 对仿真机器鱼和右上球门块进行碰撞检测若碰撞则响应
        //    result = CollisionBetweenFishAndGoalBlock(ref fish, polygonFish, field.BorderRightTopVertices);
        //    if (result.Intersect == true) return result;
            
        //    // 对仿真机器鱼和右下球门块进行碰撞检测若碰撞则响应
        //    result = CollisionBetweenFishAndGoalBlock(ref fish, polygonFish, field.BorderRightBottomVertices);
        //    if (result.Intersect == true) return result;

        //    // 对仿真机器鱼和左下球门块进行碰撞检测若碰撞则响应
        //    result = CollisionBetweenFishAndGoalBlock(ref fish, polygonFish, field.BorderLeftBottomVertices);
        //    #endregion
        //    return result;
        //}

        ///// <summary>
        ///// 在已检测出仿真机器鱼和仿真场地四个边界的某一个发生了碰撞的情况下进行
        ///// 检测仿真机器鱼和仿真场地规则摆放的四个球门块中与该边相邻的两个的碰撞情况若碰撞则响应
        ///// </summary>
        ///// <param name="fish">待检测的仿真机器鱼对象引用</param>
        ///// <param name="polygonFish">待检测的仿真机器鱼内层四边形模型对象</param>
        ///// <param name="vertices1">待检测第1个球门块的四个顶点列表</param>
        ///// <param name="vertices2">待检测第2个球门块的四个顶点列表</param>
        ///// <returns>true发生了碰撞false没发生碰撞</returns>
        //private static bool CollisionBetweenFishAndGoalBlock(ref RoboFish fish, 
        //    CollisionModelPolygon polygonFish, List<xna.Vector3> vertices1, List<xna.Vector3> vertices2)
        //{
        //    CollisionModelPolygon polygonGoalBlock = new CollisionModelPolygon(vertices1);
        //    CollisionDetectionResult result = CollisionBetweenTwoPolygons(polygonFish, polygonGoalBlock, true);
        //    if (result.Intersect == true)
        //    {// 和某条边界及其相邻的球门块中的第一个发生了碰撞，不可能再和其他对象发生碰撞
        //        //CollisionResponseBetweenFishAndBorder(ref fish, result);
        //        fish.PositionMm += result.MinimumTranslationVector;
        //        return true;
        //    }

        //    polygonGoalBlock = new CollisionModelPolygon(vertices2);
        //    result = CollisionBetweenTwoPolygons(polygonFish, polygonGoalBlock, true);
        //    if (result.Intersect == true)
        //    {// 和某条边界及其相邻的球门块中的第二个发生了碰撞，不可能再和其他对象发生碰撞
        //        //CollisionResponseBetweenFishAndBorder(ref fish, result);
        //        fish.PositionMm += result.MinimumTranslationVector;
        //        return true;
        //    }

        //    return false;
        //}

        ///// <summary>
        ///// 检测仿真机器鱼和仿真场地上某个球门块的碰撞情况若碰撞则响应
        ///// </summary>
        ///// <param name="fish">待检测的仿真机器鱼对象引用</param>
        ///// <param name="polygonFish">待检测的仿真机器鱼内层四边形模型对象</param>
        ///// <param name="vertices1">待检测球门块的四个顶点列表</param>
        //private static CollisionDetectionResult CollisionBetweenFishAndGoalBlock(ref RoboFish fish, 
        //    CollisionModelPolygon polygonFish, List<xna.Vector3> vertices)
        //{
        //    CollisionModelPolygon polygonGoalBlock = new CollisionModelPolygon(vertices);
        //    CollisionDetectionResult result = CollisionBetweenTwoPolygons(polygonFish, polygonGoalBlock, true);
        //    if (result.Intersect == true)
        //    {
        //        //CollisionResponseBetweenFishAndBorder(ref fish, result);
        //        fish.PositionMm += result.MinimumTranslationVector;
        //    }
        //    return result;
        //}

        ///// <summary>
        ///// 仿真机器鱼和仿真场地元素包括四个边界和四个球门块中任一对象的碰撞响应
        ///// </summary>
        ///// <param name="fish">待进行响应计算的仿真机器鱼对象</param>
        ///// <param name="result"></param>
        //private static void CollisionResponseBetweenFishAndBorder(ref RoboFish fish, CollisionDetectionResult result)
        //{
        //    if (result.Intersect == false) return;  // 未发生碰撞不做处理直接返回

        //    // 由速度值和速度方向生成速度矢量
        //    xna.Vector3 velocity = new xna.Vector3(fish.VelocityMmPs * (float)Math.Cos((double)fish.VelocityDirectionRad),
        //        0, fish.VelocityMmPs * (float)Math.Sin((double)fish.VelocityDirectionRad));

        //    // 根据碰撞检测结果进行碰撞响应计算
        //    VelocityAndAngularVelocityResponse response = CollisionResponse.CollisionResponseBetweenFishAndBorder(
        //        result.NormalAxis,ref velocity, fish.PolygonVertices[0],
        //        fish.PositionMm, result.ActionPoint, ref fish.BodyDirectionRad);
        //    fish.BodyDirectionRad += response.deltaAngularVelocityA * 0.1f;//仿真机器鱼和边界碰撞，鱼角速度改变量
        //    fish.PositionMm += response.velocityAfterCollisionA * 0.1f;
        //}
        #endregion

        #region 仿真鱼和仿真场地元素包括边界和球门块的碰撞检测——2011-01-12改 modified by renjing 20110426,modified by zhangjin20120209
        /// <summary>
        /// 检测仿真鱼和仿真场地边界及球门块碰撞情况
        /// </summary>
        /// <param name="fish">待检测的仿真机器鱼对象引用</param>
        /// <param name="field">待检测仿真场地Field对象，用到四个球门块的顶点坐标列表和四个边界坐标值</param>
        /// <returns>碰撞检测结果参数结构体包括是否碰撞/碰撞作用面中的法向/碰撞作用点坐标</returns>
        public static CollisionDetectionResult DetectCollisionBetweenFishAndBorder(
            ref RoboFish fish, Field field)
        {
            CollisionDetectionResult result = new CollisionDetectionResult();
            result.Intersect = false;
            float MaxDis=0;//记录最长相交向量的模长


            #region 使用外层模型检测仿真机器鱼是否没和任何边界及球门块发生碰撞，是则无需进一步处理
            if ((field.IsGoalBlockRegular == true)
                && (fish.CollisionModelCenterPositionMm.X > (field.LeftMm + field.GoalDepthMm + fish.CollisionModelRadiusMm))
                && (fish.CollisionModelCenterPositionMm.X < (field.RightMm - field.GoalDepthMm - fish.CollisionModelRadiusMm))
                && (fish.CollisionModelCenterPositionMm.Z > (field.TopMm + fish.CollisionModelRadiusMm))
                && (fish.CollisionModelCenterPositionMm.Z < (field.BottomMm - fish.CollisionModelRadiusMm)))
            {// 仿真机器鱼外层模型检测，在左右球门块和上下边界所包围的区域内
                return result;
            }
            #endregion

            CollisionModelPolygon PolygonVertices = new CollisionModelPolygon(fish.PolygonVertices);
            CollisionModelPolygon polygonBody = new CollisionModelPolygon(fish.BodyPolygonVertices);//BV树第三层，鱼1躯干部分模型
            CollisionModelPolygon polygonLeftPectoral = new CollisionModelPolygon(fish.LeftPectoralPolygonVertices);//BV树第三层，鱼1左胸鳍模型
            CollisionModelPolygon polygonRightPectoral = new CollisionModelPolygon(fish.RightPectoralPolygonVertices);//BV树第三层，鱼1右胸鳍模型
            CollisionModelPolygon polygonTail1 = new CollisionModelPolygon(fish.Tail1PolygonVertices);//BV树第三层，鱼1第一关节模型
            CollisionModelPolygon polygonTail2 = new CollisionModelPolygon(fish.Tail2PolygonVertices);//BV树第三层，鱼1第二关节模型
            CollisionModelPolygon polygonTail3 = new CollisionModelPolygon(fish.Tail3PolygonVertices);//BV树第三层，鱼1第三关节模型
            CollisionModelPolygon polygonLeftCaudal = new CollisionModelPolygon(fish.LeftCaudalFinVertices);//BV树第三层，鱼尾左关节模型
            CollisionModelPolygon polygonRightCaudal = new CollisionModelPolygon(fish.RightCaudalFinVertices);//BV树第三层，鱼尾右关节模型
            MyMission myMission = MyMission.Instance();

            // 记录仿真机器鱼BV树叶结点模型碰撞检测5个顶点X/Z坐标最大值和最小值的序号（其他的点不可能成为最大点或最小点，所以在这里省略）
            int maxXSeq = 0; int minXSeq = 0; int maxZSeq = 0; int minZSeq = 0;

            // 记录仿真机器鱼BV树叶结点模型碰撞检测5个顶点X坐标的最大值
            UrwpgSimHelper.Max(fish.PolygonVertices[0].X, fish.PolygonVertices[1].X,
                fish.PolygonVertices[2].X, fish.PolygonVertices[3].X, fish.PolygonVertices[4].X, fish.PolygonVertices[5].X, fish.PolygonVertices[6].X, ref maxXSeq);

            // 记录仿真机器鱼BV树叶结点模型碰撞检测5个顶点X坐标的最小值
            UrwpgSimHelper.Min(fish.PolygonVertices[0].X, fish.PolygonVertices[1].X,
                fish.PolygonVertices[2].X, fish.PolygonVertices[3].X, fish.PolygonVertices[4].X, fish.PolygonVertices[5].X, fish.PolygonVertices[6].X, ref minXSeq);

            // 记录仿真机器鱼BV树叶结点模型碰撞检测5个顶点Z坐标的最大值
            UrwpgSimHelper.Max(fish.PolygonVertices[0].Z, fish.PolygonVertices[1].Z,
                fish.PolygonVertices[2].Z, fish.PolygonVertices[3].Z, fish.PolygonVertices[4].Z, fish.PolygonVertices[5].Z, fish.PolygonVertices[6].Z, ref maxZSeq);

            // 记录仿真机器鱼BV树叶结点模型碰撞检测5个顶点Z坐标的最小值
            UrwpgSimHelper.Min(fish.PolygonVertices[0].Z, fish.PolygonVertices[1].Z,
                fish.PolygonVertices[2].Z, fish.PolygonVertices[3].Z, fish.PolygonVertices[4].Z, fish.PolygonVertices[5].Z, fish.PolygonVertices[6].Z, ref minZSeq);

            if (fish.PositionMm.X <= 0 && fish.PositionMm.Z <= 0)
            {//鱼在左上半场
                CollisionModelPolygon goalBlock1 = new CollisionModelPolygon(field.BorderLeftTopVertices);
                CollisionDetectionResult resultTail1 = CollisionBetweenTwoPolygons(polygonTail1, goalBlock1, true);//检测和鱼尾第一关节是否有碰撞
                if (resultTail1.Intersect == true)
                {
                    fish.PositionMm += resultTail1.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z)> MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z);
                        result = resultTail1;
                        result.LeafNodeA = 4;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultTail2 = CollisionBetweenTwoPolygons(polygonTail2, goalBlock1, true);//检测和鱼尾第二关节是否有碰撞
                if (resultTail2.Intersect == true)
                {
                    fish.PositionMm += resultTail2.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z);
                        result = resultTail2;
                        result.LeafNodeA = 5;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultTail3 = CollisionBetweenTwoPolygons(polygonTail3, goalBlock1, true);//检测和鱼尾第三关节是否有碰撞
                if (resultTail3.Intersect == true)
                {
                    fish.PositionMm += resultTail3.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z);
                        result = resultTail3;
                        result.LeafNodeA = 6;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultLeftCaudal = CollisionBetweenTwoPolygons(polygonLeftCaudal, goalBlock1, true);//检测和鱼尾左半尾鳍是否有碰撞
                if (resultLeftCaudal.Intersect == true)
                {
                    fish.PositionMm += resultLeftCaudal.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z);
                        result = resultLeftCaudal;
                        result.LeafNodeA = 7;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultRightCaudal = CollisionBetweenTwoPolygons(polygonRightCaudal, goalBlock1, true);//检测和鱼尾右半尾鳍是否有碰撞
                if (resultRightCaudal.Intersect == true)
                {
                    fish.PositionMm += resultRightCaudal.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z);
                        result = resultRightCaudal;
                        result.LeafNodeA = 8;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultBody = CollisionBetweenTwoPolygons(polygonBody, goalBlock1, true);//检测和身体躯干部分是否有碰撞
                if (resultBody.Intersect == true)
                {
                    fish.PositionMm += resultBody.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z);
                        result = resultBody;
                        result.LeafNodeA = 2;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultLeft = CollisionBetweenTwoPolygons(polygonLeftPectoral, goalBlock1, true);//检测和左胸鳍是否有碰撞
                if (resultLeft.Intersect == true)
                {
                    fish.PositionMm += resultLeft.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z);
                        result = resultLeft;
                        result.LeafNodeA = 1;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultRight = CollisionBetweenTwoPolygons(polygonRightPectoral, goalBlock1, true);//检测和右胸鳍是否有碰撞
                if (resultRight.Intersect == true)
                {
                    fish.PositionMm += resultRight.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z);
                        result = resultRight;
                        result.LeafNodeA = 3;
                        result.LeafNodeB = 0;
                    }
                }
                if ((fish.PolygonVertices[minZSeq].Z + result.MinimumTranslationVector.Z) <= field.TopMm + 5)//鱼中心点位置改变，因为此时没有重绘鱼，碰撞模型的各个点位置没有更新，所以以下要用到的点的位置也相应改变 by renjing 2011-2-22
                {// 内层四边形模型4个顶点的Z坐标最小值小于场地上边界Z坐标则与上边界发生碰撞
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(0, 0, 1);   // 碰撞作用面中法向为正Z方向

                    // 将仿真机器鱼当前绘图中心Z坐标下移正好使仿真机器鱼能在Z方向完全进入场地内的值
                    fish.PositionMm.Z += (field.TopMm + 5 - fish.PolygonVertices[minZSeq].Z - result.MinimumTranslationVector.Z);

                    // 碰撞作用点确定为四边形模型4个顶点中Z坐标值最小的那个沿Z轴下移到场地顶边时的值
                    result.ActionPoint = new xna.Vector3(fish.PolygonVertices[minZSeq].X,
                    fish.PolygonVertices[minZSeq].Y, field.TopMm);

                    //CollisionResponseBetweenFishAndBorder(ref fish, result);
                    //listIndex.Add(0);       // 上边界编号为0
                }
                if ((fish.PolygonVertices[minXSeq].X + result.MinimumTranslationVector.X) <= field.LeftMm + 5)//鱼中心点位置改变，因为此时没有重绘鱼，碰撞模型的各个点位置没有更新，所以以下要用到的点的位置也相应改变 by renjing 2011-2-22
                {// 内层四边形模型4个顶点的X坐标最小值小于场地左边界X坐标则与左边界发生碰撞
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(1, 0, 0);// 碰撞作用面中法向为正X方向
                    // 将仿真机器鱼当前绘图中心X坐标右移正好使仿真机器鱼能在X方向完全进入场地内的值
                    fish.PositionMm.X += (field.LeftMm + 5 - fish.PolygonVertices[minXSeq].X - result.MinimumTranslationVector.X);

                    // 碰撞作用点确定为四边形模型4个顶点中X坐标值最小的那个沿X轴右移到场地左边时的值
                    result.ActionPoint = new xna.Vector3(field.LeftMm, fish.PolygonVertices[minXSeq].Y,
                                                        fish.PolygonVertices[minXSeq].Z);

                    //CollisionResponseBetweenFishAndBorder(ref fish, result);
                    //listIndex.Add(3);       // 左边界编号为3
                }
            }
            else if (fish.PositionMm.X >= 0 && fish.PositionMm.Z <= 0)
            {//鱼在右上半场
                CollisionModelPolygon goalBlock2 = new CollisionModelPolygon(field.BorderRightTopVertices);
                CollisionDetectionResult resultTail1 = CollisionBetweenTwoPolygons(polygonTail1, goalBlock2, true);//检测和鱼尾第一关节是否有碰撞
                if (resultTail1.Intersect == true)
                {
                    fish.PositionMm += resultTail1.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z);
                        result = resultTail1;
                        result.LeafNodeA = 4;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultTail2 = CollisionBetweenTwoPolygons(polygonTail2, goalBlock2, true);//检测和鱼尾第二关节是否有碰撞
                if (resultTail2.Intersect == true)
                {
                    fish.PositionMm += resultTail2.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z);
                        result = resultTail2;
                        result.LeafNodeA = 5;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultTail3 = CollisionBetweenTwoPolygons(polygonTail3, goalBlock2, true);//检测和鱼尾第三关节是否有碰撞
                if (resultTail3.Intersect == true)
                {
                    fish.PositionMm += resultTail3.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z);
                        result = resultTail3;
                        result.LeafNodeA = 6;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultLeftCaudal = CollisionBetweenTwoPolygons(polygonLeftCaudal, goalBlock2, true);//检测和鱼尾左半尾鳍是否有碰撞
                if (resultLeftCaudal.Intersect == true)
                {
                    fish.PositionMm += resultLeftCaudal.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z);
                        result = resultLeftCaudal;
                        result.LeafNodeA = 7;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultRightCaudal = CollisionBetweenTwoPolygons(polygonRightCaudal, goalBlock2, true);//检测和鱼尾右半尾鳍是否有碰撞
                if (resultRightCaudal.Intersect == true)
                {
                    fish.PositionMm += resultRightCaudal.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z);
                        result = resultRightCaudal;
                        result.LeafNodeA = 8;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultBody = CollisionBetweenTwoPolygons(polygonBody, goalBlock2, true);//检测和身体躯干部分是否有碰撞
                if (resultBody.Intersect == true)
                {
                    fish.PositionMm += resultBody.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z);
                        result = resultBody;
                        result.LeafNodeA = 2;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultLeft = CollisionBetweenTwoPolygons(polygonLeftPectoral, goalBlock2, true);//检测和左胸鳍是否有碰撞
                if (resultLeft.Intersect == true)
                {
                    fish.PositionMm += resultLeft.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z);
                        result = resultLeft;
                        result.LeafNodeA = 1;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultRight = CollisionBetweenTwoPolygons(polygonRightPectoral, goalBlock2, true);//检测和右胸鳍是否有碰撞
                if (resultRight.Intersect == true)
                {
                    fish.PositionMm += resultRight.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z);
                        result = resultRight;
                        result.LeafNodeA = 3;
                        result.LeafNodeB = 0;
                    }
                }
                if ((fish.PolygonVertices[minZSeq].Z + result.MinimumTranslationVector.Z) <= field.TopMm + 5)//鱼中心点位置改变，因为此时没有重绘鱼，碰撞模型的各个点位置没有更新，所以以下要用到的点的位置也相应改变 by renjing 2011-2-22
                {// 内层四边形模型4个顶点的Z坐标最小值小于场地上边界Z坐标则与上边界发生碰撞
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(0, 0, 1);   // 碰撞作用面中法向为正Z方向

                    // 将仿真机器鱼当前绘图中心Z坐标下移正好使仿真机器鱼能在Z方向完全进入场地内的值
                    fish.PositionMm.Z += (field.TopMm + 5 - fish.PolygonVertices[minZSeq].Z - result.MinimumTranslationVector.Z);

                    // 碰撞作用点确定为四边形模型4个顶点中Z坐标值最小的那个沿Z轴下移到场地顶边时的值
                    result.ActionPoint = new xna.Vector3(fish.PolygonVertices[minZSeq].X,
                        fish.PolygonVertices[minZSeq].Y, field.TopMm);

                    //CollisionResponseBetweenFishAndBorder(ref fish, result);
                    // listIndex.Add(0);       // 上边界编号为0
                }
                if ((fish.PolygonVertices[maxXSeq].X + result.MinimumTranslationVector.X) >= field.RightMm - 5)//鱼中心点位置改变，因为此时没有重绘鱼，碰撞模型的各个点位置没有更新，所以以下要用到的点的位置也相应改变 by renjing 2011-2-22
                {// 内层四边形模型4个顶点的X坐标最大值大于场地右边界X坐标则与右边界发生碰撞
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(-1, 0, 0);// 碰撞作用面中法向为负X方向

                    // 将仿真机器鱼当前绘图中心X坐标左移正好使仿真机器鱼能在X方向完全进入场地内的值
                    fish.PositionMm.X -= (fish.PolygonVertices[maxXSeq].X + result.MinimumTranslationVector.X - field.RightMm + 5);

                    // 碰撞作用点确定为四边形模型4个顶点中X坐标值最大的那个沿X轴左移到场地右边时的值
                    result.ActionPoint = new xna.Vector3(field.RightMm, fish.PolygonVertices[maxXSeq].Y,
                                                         fish.PolygonVertices[maxXSeq].Z);

                    //CollisionResponseBetweenFishAndBorder(ref fish, result);
                    //listIndex.Add(1);       // 右边界编号为1
                }
            }
            else if (fish.PositionMm.X <= 0 && fish.PositionMm.Z >= 0)
            {//鱼在左下半场
                CollisionModelPolygon goalBlock3 = new CollisionModelPolygon(field.BorderLeftBottomVertices);
                CollisionDetectionResult resultTail1 = CollisionBetweenTwoPolygons(polygonTail1, goalBlock3, true);//检测和鱼尾第一关节是否有碰撞
                if (resultTail1.Intersect == true)
                {
                    fish.PositionMm += resultTail1.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z);
                        result = resultTail1;
                        result.LeafNodeA = 4;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultTail2 = CollisionBetweenTwoPolygons(polygonTail2, goalBlock3, true);//检测和鱼尾第二关节是否有碰撞
                if (resultTail2.Intersect == true)
                {
                    fish.PositionMm += resultTail2.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z);
                        result = resultTail2;
                        result.LeafNodeA = 5;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultTail3 = CollisionBetweenTwoPolygons(polygonTail3, goalBlock3, true);//检测和鱼尾第三关节是否有碰撞
                if (resultTail3.Intersect == true)
                {
                    fish.PositionMm += resultTail3.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z);
                        result = resultTail3;
                        result.LeafNodeA = 6;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultLeftCaudal = CollisionBetweenTwoPolygons(polygonLeftCaudal, goalBlock3, true);//检测和鱼尾左半尾鳍是否有碰撞
                if (resultLeftCaudal.Intersect == true)
                {
                    fish.PositionMm += resultLeftCaudal.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z);
                        result = resultLeftCaudal;
                        result.LeafNodeA = 7;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultRightCaudal = CollisionBetweenTwoPolygons(polygonRightCaudal, goalBlock3, true);//检测和鱼尾右半尾鳍是否有碰撞
                if (resultRightCaudal.Intersect == true)
                {
                    fish.PositionMm += resultRightCaudal.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z);
                        result = resultRightCaudal;
                        result.LeafNodeA = 8;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultBody = CollisionBetweenTwoPolygons(polygonBody, goalBlock3, true);//检测和身体躯干部分是否有碰撞
                if (resultBody.Intersect == true)
                {
                    fish.PositionMm += resultBody.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z);
                        result = resultBody;
                        result.LeafNodeA = 2;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultLeft = CollisionBetweenTwoPolygons(polygonLeftPectoral, goalBlock3, true);//检测和左胸鳍是否有碰撞
                if (resultLeft.Intersect == true)
                {
                    fish.PositionMm += resultLeft.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z);
                        result = resultLeft;
                        result.LeafNodeA = 1;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultRight = CollisionBetweenTwoPolygons(polygonRightPectoral, goalBlock3, true);//检测和右胸鳍是否有碰撞
                if (resultRight.Intersect == true)
                {
                    fish.PositionMm += resultRight.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z);
                        result = resultRight;
                        result.LeafNodeA = 3;
                        result.LeafNodeB = 0;
                    }
                }
                if ((fish.PolygonVertices[maxZSeq].Z + result.MinimumTranslationVector.Z) >= field.BottomMm - 5)//鱼中心点位置改变，因为此时没有重绘鱼，碰撞模型的各个点位置没有更新，所以以下要用到的点的位置也相应改变 by renjing 2011-2-22
                {// 内层四边形模型4个顶点的Z坐标最大值大于场地下边界Z坐标则与下边界发生碰撞
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(0, 0, -1);   // 碰撞作用面中法向为负Z方向

                    // 将仿真机器鱼当前绘图中心Z坐标下移正好使仿真机器鱼能在Z方向完全进入场地内的值
                    fish.PositionMm.Z += (field.BottomMm - 5 - fish.PolygonVertices[maxZSeq].Z - result.MinimumTranslationVector.Z);

                    // 碰撞作用点确定为四边形模型4个顶点中Z坐标值最大的那个沿Z轴上移到场地底边时的值
                    result.ActionPoint = new xna.Vector3(fish.PolygonVertices[maxZSeq].X,
                        fish.PolygonVertices[maxZSeq].Y, field.BottomMm);

                    //CollisionResponseBetweenFishAndBorder(ref fish, result);
                    //listIndex.Add(0);       // 下边界编号为2
                }

                if ((fish.PolygonVertices[minXSeq].X + result.MinimumTranslationVector.X) <= field.LeftMm + 5)//鱼中心点位置改变，因为此时没有重绘鱼，碰撞模型的各个点位置没有更新，所以以下要用到的点的位置也相应改变 by renjing 2011-2-22
                {// 内层四边形模型4个顶点的X坐标最小值小于场地左边界X坐标则与左边界发生碰撞
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(1, 0, 0);// 碰撞作用面中法向为正X方向
                    // 将仿真机器鱼当前绘图中心X坐标右移正好使仿真机器鱼能在X方向完全进入场地内的值
                    fish.PositionMm.X += (field.LeftMm + 5 - fish.PolygonVertices[minXSeq].X - result.MinimumTranslationVector.X);

                    // 碰撞作用点确定为四边形模型4个顶点中X坐标值最小的那个沿X轴右移到场地左边时的值
                    result.ActionPoint = new xna.Vector3(field.LeftMm, fish.PolygonVertices[minXSeq].Y,
                                                        fish.PolygonVertices[minXSeq].Z);

                    //CollisionResponseBetweenFishAndBorder(ref fish, result);
                    //listIndex.Add(3);       // 左边界编号为3
                }
            }
            else if (fish.PositionMm.X >= 0 && fish.PositionMm.Z >= 0)
            {//鱼在右下半场
                CollisionModelPolygon goalBlock4 = new CollisionModelPolygon(field.BorderRightBottomVertices);
                CollisionDetectionResult resultTail1 = CollisionBetweenTwoPolygons(polygonTail1, goalBlock4, true);//检测和鱼尾第一关节是否有碰撞
                if (resultTail1.Intersect == true)
                {
                    fish.PositionMm += resultTail1.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z);
                        result = resultTail1;
                        result.LeafNodeA = 4;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultTail2 = CollisionBetweenTwoPolygons(polygonTail2, goalBlock4, true);//检测和鱼尾第二关节是否有碰撞
                if (resultTail2.Intersect == true)
                {
                    fish.PositionMm += resultTail2.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z);
                        result = resultTail2;
                        result.LeafNodeA = 5;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultTail3 = CollisionBetweenTwoPolygons(polygonTail3, goalBlock4, true);//检测和鱼尾第三关节是否有碰撞
                if (resultTail3.Intersect == true)
                {
                    fish.PositionMm += resultTail3.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z);
                        result = resultTail3;
                        result.LeafNodeA = 6;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultLeftCaudal = CollisionBetweenTwoPolygons(polygonLeftCaudal, goalBlock4, true);//检测和鱼尾左半尾鳍是否有碰撞
                if (resultLeftCaudal.Intersect == true)
                {
                    fish.PositionMm += resultLeftCaudal.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z);
                        result = resultLeftCaudal;
                        result.LeafNodeA = 7;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultRightCaudal = CollisionBetweenTwoPolygons(polygonRightCaudal, goalBlock4, true);//检测和鱼尾右半尾鳍是否有碰撞
                if (resultRightCaudal.Intersect == true)
                {
                    fish.PositionMm += resultRightCaudal.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z);
                        result = resultRightCaudal;
                        result.LeafNodeA = 8;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultBody = CollisionBetweenTwoPolygons(polygonBody, goalBlock4, true);//检测和身体躯干部分是否有碰撞
                if (resultBody.Intersect == true)
                {
                    fish.PositionMm += resultBody.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z);
                        result = resultBody;
                        result.LeafNodeA = 2;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultLeft = CollisionBetweenTwoPolygons(polygonLeftPectoral, goalBlock4, true);//检测和左胸鳍是否有碰撞
                if (resultLeft.Intersect == true)
                {
                    fish.PositionMm += resultLeft.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z);
                        result = resultLeft;
                        result.LeafNodeA = 1;
                        result.LeafNodeB = 0;
                    }
                }
                CollisionDetectionResult resultRight = CollisionBetweenTwoPolygons(polygonRightPectoral, goalBlock4, true);//检测和右胸鳍是否有碰撞
                if (resultRight.Intersect == true)
                {
                    fish.PositionMm += resultRight.MinimumTranslationVector;
                    fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                    if (Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z) > MaxDis)
                    {
                        MaxDis = (float)Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z);
                        result = resultRight;
                        result.LeafNodeA = 3;
                        result.LeafNodeB = 0;
                    }
                }
                if ((fish.PolygonVertices[maxZSeq].Z + result.MinimumTranslationVector.Z) >= field.BottomMm - 5)//鱼中心点位置改变，因为此时没有重绘鱼，碰撞模型的各个点位置没有更新，所以以下要用到的点的位置也相应改变 by renjing 2011-2-22
                {// 内层四边形模型4个顶点的Z坐标最大值大于场地下边界Z坐标则与下边界发生碰撞
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(0, 0, -1);   // 碰撞作用面中法向为负Z方向

                    // 将仿真机器鱼当前绘图中心Z坐标下移正好使仿真机器鱼能在Z方向完全进入场地内的值
                    fish.PositionMm.Z += (field.BottomMm - 5 - fish.PolygonVertices[maxZSeq].Z - result.MinimumTranslationVector.Z);

                    // 碰撞作用点确定为四边形模型4个顶点中Z坐标值最大的那个沿Z轴上移到场地底边时的值
                    result.ActionPoint = new xna.Vector3(fish.PolygonVertices[maxZSeq].X,
                        fish.PolygonVertices[maxZSeq].Y, field.BottomMm);

                    //CollisionResponseBetweenFishAndBorder(ref fish, result);
                    //listIndex.Add(0);       // 下边界编号为2
                }

                if ((fish.PolygonVertices[maxXSeq].X + result.MinimumTranslationVector.X) >= field.RightMm - 5)//鱼中心点位置改变，因为此时没有重绘鱼，碰撞模型的各个点位置没有更新，所以以下要用到的点的位置也相应改变 by renjing 2011-2-22
                {// 内层四边形模型4个顶点的X坐标最大值大于场地右边界X坐标则与右边界发生碰撞
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(-1, 0, 0);// 碰撞作用面中法向为负X方向

                    // 将仿真机器鱼当前绘图中心X坐标左移正好使仿真机器鱼能在X方向完全进入场地内的值
                    fish.PositionMm.X -= (fish.PolygonVertices[maxXSeq].X + result.MinimumTranslationVector.X - field.RightMm + 5);

                    // 碰撞作用点确定为四边形模型4个顶点中X坐标值最大的那个沿X轴左移到场地右边时的值
                    result.ActionPoint = new xna.Vector3(field.RightMm, fish.PolygonVertices[maxXSeq].Y,
                                                         fish.PolygonVertices[maxXSeq].Z);

                    //CollisionResponseBetweenFishAndBorder(ref fish, result);
                    //listIndex.Add(1);       // 右边界编号为1
                }
            }
            if (result.Intersect == true)
            {// 为仿真机器鱼添加碰撞标志
                fish.Collision.Add(CollisionType.FISH_POOL);
            }

            //Console.WriteLine("X:");
            //Console.WriteLine(result.ActionPoint.X);
            //Console.WriteLine("Z:");
            //Console.WriteLine(result.ActionPoint.Z);
            return result;

        }
        #endregion

        #region 确定分离方向，用于两个对象碰撞检测后确定分离轴和分离方向
        /// <summary>
        /// 确定分离方向，根据物体的对象的运动方向，判断两者的分离方向，使得两者朝相离的方向运动
        /// </summary>
        /// <param name="NormalAxis">两物体碰撞的作用发现</param>
        /// <param name="VelocityDirectionRad">A对象的速度方向</param>
        /// <param name="MinimumTraslationVector">最小相交向量</param>
        /// <returns>返回计算后的最小相交向量</returns>
        public static xna.Vector3 MoveDirection(xna.Vector3 NormalAxis, float VelocityDirectionRad, ref xna.Vector3 MinimumTraslationVector)
        {
            //float tmpAngle = xna.Vector3.Dot(NormalAxis, new xna.Vector3(1, 0, 0));
            //if (tmpAngle > 1)
            //{
            //    tmpAngle = 1;
            //}
            //else if (tmpAngle < -1)
            //{
            //    tmpAngle = -1;
            //}
            //float normalAxisAngle = (float)Math.Acos(tmpAngle);//计算法线向量在世界坐标系中的角度
            //if (Math.Abs(normalAxisAngle - VelocityDirectionRad) <= Math.PI / 2)//如果法线向量方向和A对象的速度方向一致（运动趋于同一方向）
            //    MinimumTraslationVector = -MinimumTraslationVector;//最小相交向量为相反值
            return MinimumTraslationVector;
        }
        #endregion

        #region 判断是否发生完全穿越 重载+7
        /// <summary>
        /// 判断是否发生完全穿越现象，此环节发生在分离轴理论（SAT）判断后
        /// 如果发生完全穿越，则跳转至二分法；如果没有发生完全穿越，则跳出此次碰撞检测
        /// </summary>
        /// <param name="PolygonABefore">初始状态的A多边形</param>
        /// <param name="PolygonBBefore">初始状态B多边形</param>
        /// <param name="PolygonAAfter">结束状态的A多边形</param>
        /// <param name="PolygonBAfter">结束状态B多边形</param>
        /// <param name="PolygonAAngleBefore">初始状态的A多边形的倾角</param>
        /// <param name="PolygonBAngleBefore">初始状态的B多边形的倾角</param>
        /// <param name="PolygonAAngleAfter">结束状态的A多边形的倾角</param>
        /// <param name="PolygonBAngleAfter">结束状态的B多边形的倾角</param>
        /// <returns>返回布尔值，是否发生完全穿越现象</returns>
        public static bool IsTunneling(CollisionModelPolygon PolygonABefore, CollisionModelPolygon PolygonBBefore, CollisionModelPolygon PolygonAAfter, CollisionModelPolygon PolygonBAfter,
                                       float PolygonAAngleBefore, float PolygonBAngleBefore, float PolygonAAngleAfter, float PolygonBAngleAfter)
        {
            bool IsTunneling = false;
            float MaxZPolygonA = -5000, MaxXPolygonA = -5000, MaxProjectPointsZ = -5000, MaxProjectPointsX = -5000;//定义在本地坐标系中，A多边形和B多边形运动路径在新坐标系中纵坐标和横坐标的最大值
            float MinZPolygonA = 5000, MinXPolygonA = 5000, MinProjectPointsZ = 5000, MinProjectPointsX = 5000;//定义在本地坐标系中，A多边形和B多边形运动路径在新坐标系中纵坐标和横坐标的最小值
            List<float> ProjectPointsZ = new List<float>(PolygonBBefore.Points.Count);//在本地坐标系纵轴上的投影值
            List<float> ProjectPointsX = new List<float>(PolygonBBefore.Points.Count); //在本地坐标系横轴上的投影值
            xna.Vector3 tmp = new xna.Vector3(0, 0, 0);//用以解决list不能用作ref参数传递
            xna.Vector3 polygonBBeforeCenterLocal = new xna.Vector3(0, 0, 0);
            xna.Vector3 polygonBAfterCenterLocal = new xna.Vector3(0, 0, 0);

            //定义本地坐标系的坐标值
            List<xna.Vector3> PolygonABeforeLocal=new List<xna.Vector3>(PolygonABefore.Points.Count);
            List<xna.Vector3> PolygonAAfterLocal = new List<xna.Vector3>(PolygonAAfter.Points.Count);
            List<xna.Vector3> PolygonBBeforeLocal = new List<xna.Vector3>(PolygonBBefore.Points.Count);
            List<xna.Vector3> PolygonBAfterLocal = new List<xna.Vector3>(PolygonBAfter.Points.Count);
            for (int i = 0; i < PolygonABefore.Points.Count; i++)
            {
                PolygonABeforeLocal.Add(new xna.Vector3());
            }
            for (int i = 0; i < PolygonAAfter.Points.Count; i++)
            {
                PolygonAAfterLocal.Add(new xna.Vector3());
            }
            for (int i = 0; i < PolygonBBefore.Points.Count; i++)
            {
                PolygonBBeforeLocal.Add(new xna.Vector3());
                ProjectPointsX.Add(new float());
                ProjectPointsZ.Add(new float());
            }
            for (int i = 0; i < PolygonBAfter.Points.Count; i++)
            {
                PolygonBAfterLocal.Add(new xna.Vector3());
            }


            
            //将初始状态的A、B多边形都转换到以A物体初始状态的中心点为原点，A多边形倾角为坐标轴的坐标系中
            for (int i=0; i < PolygonABefore.Points.Count; i++)
            {
                tmp = PolygonABeforeLocal[i];
                UrwpgSimHelper.CoordinateTransformation(PolygonAAngleBefore, PolygonABefore.Center, ref tmp, PolygonABefore.Points[i]);
                PolygonABeforeLocal[i] = tmp;
                if (PolygonABeforeLocal[i].Z < MinZPolygonA)
                    MinZPolygonA = PolygonABeforeLocal[i].Z;//记录A多边形纵坐标的最小值
                if (PolygonABeforeLocal[i].Z > MaxZPolygonA)
                    MaxZPolygonA = PolygonABeforeLocal[i].Z;//记录A多边形纵坐标的最大值
                if (PolygonABeforeLocal[i].X < MinXPolygonA)
                    MinXPolygonA = PolygonABeforeLocal[i].X;//记录A多边形横坐标的最小值
                if (PolygonABeforeLocal[i].X > MaxXPolygonA)
                    MaxXPolygonA = PolygonABeforeLocal[i].X;//记录A多边形横坐标的最大值

            }
            for (int i = 0; i < PolygonBBefore.Points.Count; i++)
            {
                tmp = PolygonBBeforeLocal[i];
                UrwpgSimHelper.CoordinateTransformation(PolygonAAngleBefore, PolygonABefore.Center, ref tmp, PolygonBBefore.Points[i]);
                PolygonBBeforeLocal[i] = tmp;
            }
            UrwpgSimHelper.CoordinateTransformation(PolygonAAngleBefore, PolygonABefore.Center, ref polygonBBeforeCenterLocal, PolygonBBefore.Center);

            //将终止状态的A、B多边形都转换到以A物体终止状态的中心点为原点，A多边形倾角为坐标轴的坐标系中
            for (int i = 0; i < PolygonAAfter.Points.Count; i++)
            {
                tmp = PolygonAAfterLocal[i];
                UrwpgSimHelper.CoordinateTransformation(PolygonAAngleAfter, PolygonAAfter.Center, ref tmp, PolygonAAfter.Points[i]);
                PolygonAAfterLocal[i] = tmp;
            }

            for (int i = 0; i < PolygonBAfter.Points.Count; i++)
            {
                tmp = PolygonBAfterLocal[i];
                UrwpgSimHelper.CoordinateTransformation(PolygonAAngleAfter, PolygonAAfter.Center, ref tmp, PolygonBAfter.Points[i]);
                PolygonBAfterLocal[i] = tmp;
            }
            UrwpgSimHelper.CoordinateTransformation(PolygonAAngleBefore, PolygonABefore.Center, ref polygonBAfterCenterLocal, PolygonBAfter.Center);


            if (polygonBBeforeCenterLocal.X * polygonBAfterCenterLocal.X <= 0 || polygonBBeforeCenterLocal.Z * polygonBAfterCenterLocal.Z <= 0)
            {
            for (int j = 0; j < PolygonBAfter.Points.Count; j++)
            {
                 //轨迹直线不与坐标轴平行
                    if (Math.Abs(PolygonBBeforeLocal[j].X - PolygonBAfterLocal[j].X) > float.Epsilon && Math.Abs(PolygonBBeforeLocal[j].Z - PolygonBAfterLocal[j].Z) > float.Epsilon)
                    {
                        //记录B多边形运动路径在本地坐标系中，纵轴上的坐标值
                        ProjectPointsZ[j] = PolygonBBeforeLocal[j].Z - PolygonBBeforeLocal[j].X * (PolygonBBeforeLocal[j].Z - PolygonBAfterLocal[j].Z) / (PolygonBBeforeLocal[j].X - PolygonBAfterLocal[j].X);
                        if (ProjectPointsZ[j] < MinProjectPointsZ)
                            MinProjectPointsZ = ProjectPointsZ[j];//记录B多边形运动路径在纵坐标上得最小值
                        if (ProjectPointsZ[j] > MaxProjectPointsZ)
                            MaxProjectPointsZ = ProjectPointsZ[j];//记录B多边形运动路径在纵坐标上的最大值

                        //记录B多边形运动路径在本地坐标系中，横轴上的坐标值
                        ProjectPointsX[j] = (PolygonBBeforeLocal[j].Z * PolygonBAfterLocal[j].X - PolygonBAfterLocal[j].Z * PolygonBBeforeLocal[j].X) / (PolygonBBeforeLocal[j].Z - PolygonBAfterLocal[j].Z);
                        if (ProjectPointsX[j] < MinProjectPointsX)
                            MinProjectPointsX = ProjectPointsX[j];//记录B多边形运动路径在横坐标上得最小值
                        if (ProjectPointsX[j] > MaxProjectPointsX)
                            MaxProjectPointsX = ProjectPointsX[j];//记录B多边形运动路径在横坐标上的最大值


                        if ((ProjectPointsZ[j] <= MaxZPolygonA && ProjectPointsZ[j] >= MinZPolygonA) || (ProjectPointsX[j] <= MaxXPolygonA && ProjectPointsX[j] >= MinXPolygonA))
                        {//如果运动路径和A多边形相交，则认为发生完全穿越，终止循环，返回值
                            IsTunneling = true;
                            break;
                        }
                    }
                    //轨迹直线与纵轴平行，但不与横轴平行
                    else if (Math.Abs(PolygonBBeforeLocal[j].X - PolygonBAfterLocal[j].X) <= float.Epsilon && Math.Abs(PolygonBBeforeLocal[j].Z - PolygonBAfterLocal[j].Z) > float.Epsilon)
                    {
                        ProjectPointsX[j] = (PolygonBBeforeLocal[j].Z * PolygonBAfterLocal[j].X - PolygonBAfterLocal[j].Z * PolygonBBeforeLocal[j].X) / (PolygonBBeforeLocal[j].Z - PolygonBAfterLocal[j].Z);
                        if (ProjectPointsX[j] < MinProjectPointsX)
                            MinProjectPointsX = ProjectPointsX[j];//记录B多边形运动路径在横坐标上得最小值
                        if (ProjectPointsX[j] > MaxProjectPointsX)
                            MaxProjectPointsX = ProjectPointsX[j];//记录B多边形运动路径在横坐标上的最大值

                        if (ProjectPointsX[j] <= MaxXPolygonA && ProjectPointsX[j] >= MinXPolygonA)
                        {//如果运动路径和A多边形相交，则认为发生完全穿越，终止循环，返回值
                            IsTunneling = true;
                            break;
                        }
                    }
                    //轨迹直线与横轴平行，但不与纵轴平行
                    else if (Math.Abs(PolygonBBeforeLocal[j].X - PolygonBAfterLocal[j].X) > float.Epsilon && Math.Abs(PolygonBBeforeLocal[j].Z - PolygonBAfterLocal[j].Z) <= float.Epsilon)
                    {
                        ProjectPointsZ[j] = PolygonBBeforeLocal[j].Z - PolygonBBeforeLocal[j].X * (PolygonBBeforeLocal[j].Z - PolygonBAfterLocal[j].Z) / (PolygonBBeforeLocal[j].X - PolygonBAfterLocal[j].X);
                        if (ProjectPointsZ[j] < MinProjectPointsZ)
                            MinProjectPointsZ = ProjectPointsZ[j];//记录B多边形运动路径在纵坐标上得最小值
                        if (ProjectPointsZ[j] > MaxProjectPointsZ)
                            MaxProjectPointsZ = ProjectPointsZ[j];//记录B多边形运动路径在纵坐标上的最大值

                        if (ProjectPointsZ[j] <= MaxZPolygonA && ProjectPointsZ[j] >= MinZPolygonA)
                        {//如果运动路径和A多边形相交，则认为发生完全穿越，终止循环，返回值
                            IsTunneling = true;
                            break;
                        }
                    }
                    else
                        break;
                }
              }
           // if ((MinProjectPointsZ <= MinZPolygonA && MaxProjectPointsZ >= MaxZPolygonA) || (MinProjectPointsX <= MinXPolygonA && MaxProjectPointsX >= MaxXPolygonA))
                //IsTunneling = true;//这是当B多边形远远大于A多边形时，会出现B多边形路径投影在纵轴上时，完全包含了A多边形，此种情况也判断为发生完全穿越

                return IsTunneling;
        }

        /// <summary>
        /// 判断是否发生完全穿越现象，此环节发生在分离轴理论（SAT）判断后
        /// 如果发生完全穿越，则跳转至二分法；如果没有发生完全穿越，则跳出此次碰撞检测
        /// </summary>
        /// <param name="PolygonABefore">前一周期A多边形</param>
        /// <param name="BallBBeforeMm">前一周期B水球的坐标值</param>
        /// <param name="PolygonAAfter">后一周期A多边形</param>
        /// <param name="BallBAfter">后一周期B水球</param>
        /// <param name="PolygonAAngleBefore">前一周期A多边形的倾角</param>
        /// <param name="BallBAngleBefore">前一周期B水球的倾角</param>
        /// <param name="PolygonAAngleAfter">后一周期A多边形的倾角</param>
        /// <param name="BallBAngleAfter">后一周期B水球的倾角</param>
        /// <returns>返回布尔值，是否发生完全穿越现象</returns>
        public static bool IsTunneling(CollisionModelPolygon PolygonABefore, xna.Vector3 BallBBeforeMm, CollisionModelPolygon PolygonAAfter, Ball BallBAfter,
                                        float PolygonAAngleBefore, float BallBAngleBefore, float PolygonAAngleAfter, float BallBAngleAfter)
        {
            bool IsTunneling = false;
            xna.Vector3 BallBAfterMm = new xna.Vector3(BallBAfter.PositionMm.X, BallBAfter.PositionMm.Y, BallBAfter.PositionMm.Z);
            CollisionModelPolygon polygonABefore = PolygonABefore;
            CollisionModelPolygon polygonAAfter = PolygonAAfter;
            float MaxZPolygonA = 0, MaxBallCenterProjectPoint = 0;//定义在新坐标系中，A多边形和B水球在新坐标系中纵坐标的最大值
            float MinZPolygonA = 0, MinBallCenterProjectPoint = 0;//定义在新坐标系中，A多边形和B水球在新坐标系中纵坐标的最小值

            //将前一周期的A多边形、B水球都转换到以前一周期A的中心点为原点，A多边形倾角为坐标轴的坐标系中
            for (int i = 0; i < polygonABefore.Points.Count; i++)
            {
                polygonABefore.Points[i] = CoordinateTransformation(polygonABefore.Points[i], polygonABefore.Center, PolygonAAngleBefore);
                if (polygonABefore.Points[i].Z < MinZPolygonA)
                    MinZPolygonA = polygonABefore.Points[i].Z;//记录A多边形纵坐标的最小值
                if (polygonABefore.Points[i].Z > MaxZPolygonA)
                    MaxZPolygonA = polygonABefore.Points[i].Z;//记录A多边形纵坐标的最大值
            }
            BallBBeforeMm = CoordinateTransformation(BallBBeforeMm, polygonABefore.Center, PolygonAAngleBefore);

            //将后一周期的A多边形、B水球都转换到以后一周期A的中心点为原点，A多边形倾角为坐标轴的坐标系中
            for (int i = 0; i < polygonAAfter.Points.Count; i++)
            {
                polygonAAfter.Points[i] = CoordinateTransformation(polygonAAfter.Points[i], polygonAAfter.Center, PolygonAAngleAfter);
            }
            BallBAfterMm = CoordinateTransformation(BallBAfterMm, polygonAAfter.Center, PolygonAAngleAfter);

            if (Math.Abs(BallBBeforeMm.X - BallBAfterMm.X) > float.Epsilon)
            {
                //记录B水球运动路径在新坐标系中，纵轴上的坐标值
                float BallCenterProjectPoint = BallBBeforeMm.Z - BallBBeforeMm.X * (BallBBeforeMm.Z - BallBAfterMm.Z) / (BallBBeforeMm.X - BallBAfterMm.X);
                MaxBallCenterProjectPoint += BallBAfter.RadiusMm * (float)Math.Sqrt((BallBAfterMm.Z - BallBBeforeMm.Z) * (BallBAfterMm.Z - BallBBeforeMm.Z) +
                                            (BallBAfterMm.X - BallBBeforeMm.X) * (BallBAfterMm.X - BallBBeforeMm.X)) / (float)Math.Abs(BallBAfterMm.X - BallBBeforeMm.X);
                MinBallCenterProjectPoint -= BallBAfter.RadiusMm * (float)Math.Sqrt((BallBAfterMm.Z - BallBBeforeMm.Z) * (BallBAfterMm.Z - BallBBeforeMm.Z) +
                                            (BallBAfterMm.X - BallBBeforeMm.X) * (BallBAfterMm.X - BallBBeforeMm.X)) / (float)Math.Abs(BallBAfterMm.X - BallBBeforeMm.X);
                if (MaxBallCenterProjectPoint <= MaxZPolygonA && MinBallCenterProjectPoint >= MinZPolygonA)
                    IsTunneling = true;//如果运动路径和A多边形相交，则认为发生完全穿越，终止循环，返回值
            }

            if ((MaxBallCenterProjectPoint <= MinZPolygonA && MaxBallCenterProjectPoint >= MaxZPolygonA) || (MinBallCenterProjectPoint <= MinZPolygonA && MinBallCenterProjectPoint >= MaxZPolygonA))
                IsTunneling = true;//这是当B水球远远大于A多边形时，会出现B水球路径投影在纵轴上时，完全包含了A多边形，此种情况也判断为发生完全穿越

            return IsTunneling;
        }


        /// <summary>
        /// 判断是否发生完全穿越现象，此环节发生在分离轴理论（SAT）判断后
        /// 如果发生完全穿越，则跳转至二分法；如果没有发生完全穿越，则跳出此次碰撞检测
        /// </summary>
        /// <param name="RoundObstacleA">静态圆形障碍物A</param>
        /// <param name="PolygonBBefore">前一周期B多边形</param>
        /// <param name="PolygonBAfter">后一周期B多边形</param>
        /// <returns>返回布尔值，是否发生完全穿越现象</returns>
        public static bool IsTunneling(RoundedObstacle RoundObstacleA,CollisionModelPolygon PolygonBBefore, CollisionModelPolygon PolygonBAfter)
        {
            bool IsTunneling = false;
            return IsTunneling;
        }


        /// <summary>
        /// 判断是否发生完全穿越现象，此环节发生在分离轴理论（SAT）判断后
        /// 如果发生完全穿越，则跳转至二分法；如果没有发生完全穿越，则跳出此次碰撞检测
        /// </summary>
        /// <param name="RecObstacleA">静态方形障碍物A</param>
        /// <param name="PolygonBBefore">前一周期B多边形</param>
        /// <param name="PolygonBAfter">后一周期B多边形</param>
        /// <returns>返回布尔值，是否发生完全穿越现象</returns>
        public static bool IsTunneling(RectangularObstacle RecObstacleA , CollisionModelPolygon PolygonBBefore,CollisionModelPolygon PolygonBAfter)
        {
            bool IsTunneling = false;
            return IsTunneling;
        }


        /// <summary>
        /// 判断是否发生完全穿越现象，此环节发生在分离轴理论（SAT）判断后
        /// 如果发生完全穿越，则跳转至二分法；如果没有发生完全穿越，则跳出此次碰撞检测
        /// </summary>
        /// <param name="BallABeforeMm">前一周期A水球的球心位置坐标</param>
        /// <param name="BallBBeforeMm">前一周期B水球</param>
        /// <param name="BallAAfter">后一周期A水球</param>
        /// <param name="BallBAfter">后一周期B水球</param>
        /// <returns>返回布尔值，是否发生完全穿越现象</returns>
        public static bool IsTunneling(xna.Vector3 BallABeforeMm, xna.Vector3 BallBBeforeMm, Ball BallAAfter, Ball BallBAfter)
        {
            bool IsTunneling = false;
            float MaxBallCenterProjectPoint = 0;//定义在新坐标系中，B水球在新坐标系中纵坐标的最大值
            float MinBallCenterProjectPoint = 0;//定义在新坐标系中，B水球在新坐标系中纵坐标的最小值

            //将前一周期的B水球都转换到以前一周期A的中心点为原点，A多边形倾角为坐标轴的坐标系中
            BallBBeforeMm = CoordinateTransformation(BallBBeforeMm, BallABeforeMm, 0);

            //将后一周期的B水球都转换到以后一周期A的中心点为原点，A多边形倾角为坐标轴的坐标系中
            BallBAfter.PositionMm = CoordinateTransformation(BallBAfter.PositionMm, BallAAfter.PositionMm, 0);

            if (Math.Abs(BallBBeforeMm.X - BallBAfter.PositionMm.X) > float.Epsilon)
            {
                //记录B水球运动路径在新坐标系中，纵轴上的坐标值
                float BallCenterProjectPoint = BallBBeforeMm.Z - BallBBeforeMm.X * (BallBBeforeMm.Z - BallBAfter.PositionMm.Z) / (BallBBeforeMm.X - BallBAfter.PositionMm.X);
                MaxBallCenterProjectPoint += BallBAfter.RadiusMm * (float)Math.Sqrt((BallBAfter.PositionMm.Z - BallBBeforeMm.Z) * (BallBAfter.PositionMm.Z - BallBBeforeMm.Z) +
                                            (BallBAfter.PositionMm.X - BallBBeforeMm.X) * (BallBAfter.PositionMm.X - BallBBeforeMm.X)) / (float)Math.Abs(BallBAfter.PositionMm.X - BallBBeforeMm.X);
                MinBallCenterProjectPoint -= BallBAfter.RadiusMm * (float)Math.Sqrt((BallBAfter.PositionMm.Z - BallBBeforeMm.Z) * (BallBAfter.PositionMm.Z - BallBBeforeMm.Z) +
                                            (BallBAfter.PositionMm.X - BallBBeforeMm.X) * (BallBAfter.PositionMm.X - BallBBeforeMm.X)) / (float)Math.Abs(BallBAfter.PositionMm.X - BallBBeforeMm.X);
                if ((MaxBallCenterProjectPoint <= BallAAfter.RadiusMm && MaxBallCenterProjectPoint >= -BallAAfter.RadiusMm) || (MinBallCenterProjectPoint <= BallAAfter.RadiusMm && MinBallCenterProjectPoint >= -BallAAfter.RadiusMm))
                    IsTunneling = true;//如果运动路径和A水球相交，则认为发生完全穿越，终止循环，返回值
            }

            if (MinBallCenterProjectPoint <= -BallAAfter.RadiusMm && MaxBallCenterProjectPoint >= BallAAfter.RadiusMm)
                IsTunneling = true;//这是当B水球远远大于A多边形时，会出现B水球路径投影在纵轴上时，完全包含了A水球，此种情况也判断为发生完全穿越
            return IsTunneling;
        }


        /// <summary>
        /// 判断是否发生完全穿越现象，此环节发生在分离轴理论（SAT）判断后
        /// 如果发生完全穿越，则跳转至二分法；如果没有发生完全穿越，则跳出此次碰撞检测
        /// </summary>
        /// <param name="RoundObstacleA">静态圆形障碍物A</param>
        /// <param name="BallBBeforeMm">前一周期B水球的球心坐标</param>
        /// <param name="BallBAfter">后一周期B水球</param>
        /// <returns>返回布尔值，是否发生完全穿越现象</returns>
        public static bool IsTunneling(RoundedObstacle RoundObstacleA,xna.Vector3 BallBBeforeMm,  Ball BallBAfter)
        {
            bool IsTunneling = false;
            return IsTunneling;
        }


        /// <summary>
        /// 判断是否发生完全穿越现象，此环节发生在分离轴理论（SAT）判断后
        /// 如果发生完全穿越，则跳转至二分法；如果没有发生完全穿越，则跳出此次碰撞检测
        /// </summary>
        /// <param name="RecObstacleA">静态方形障碍物A</param>
        /// <param name="BallBBeforeMm">前一周期B水球的球心坐标</param>
        /// <param name="BallBAfter">后一周期B水球</param>
        /// <returns>返回布尔值，是否发生完全穿越现象</returns>
        public static bool IsTunneling( RectangularObstacle RecObstacleA,xna.Vector3 BallBBeforeMm, Ball BallBAfter)
        {
            bool IsTunneling = false;
            return IsTunneling;
        }
        #endregion

        #region 二分法
        /// <summary>
        /// 仿真球的二分法 added by zhangjin 20120311
        /// </summary>
        /// <param name="StartPosition">仿真模型的起始位置</param>
        /// <param name="EndPosition">仿真模型的终止位置</param>
        /// <param name="StartTime">仿真模型的起始位置时刻</param>
        /// <param name="EndTime">仿真模型的终止位置时刻</param> 
        /// <returns>返回二分法结果结构体</returns>
        public static DichotomyResult Dichotomyball(xna.Vector3 StartPosition, xna.Vector3 EndPosition, float StartTime, float EndTime)
        {
            DichotomyResult result = new DichotomyResult();
            //中间点坐标=（初始点坐标+结束点坐标）/2
            result.MidPositionMm.X = (StartPosition.X + EndPosition.X) / 2;
            result.MidPositionMm.Y = 0;
            result.MidPositionMm.Z = (StartPosition.Z + EndPosition.Z) / 2;
            result.MidTime = (StartTime + EndTime) / 2;
            return result;
        }

        /// <summary>
        /// 仿真鱼的二分法 added by zhangjin 20120311
        /// </summary>
        /// <param name="StartFish">仿真机器鱼的初始状态</param>
        /// <param name="EndFish">仿真机器鱼的终止状态</param>
        /// <param name="StartTime">仿真模型的起始位置时刻</param>
        /// <param name="EndTime">仿真模型的终止位置时刻</param>
        /// <returns>返回二分法结果结构体</returns>
        public static DichotomyResult Dichotomyfish(RoboFish StartFish,RoboFish EndFish, float StartTime, float EndTime)
        {
            while (StartFish.BodyDirectionRad >= (float)Math.PI)
                StartFish.BodyDirectionRad -=2 * (float)Math.PI;
            while (StartFish.BodyDirectionRad < -(float)Math.PI)
                StartFish.BodyDirectionRad +=2 * (float)Math.PI;

            while (EndFish.BodyDirectionRad >= (float)Math.PI)
                EndFish.BodyDirectionRad -= 2 * (float)Math.PI;
            while (EndFish.BodyDirectionRad < -(float)Math.PI)
                EndFish.BodyDirectionRad += 2 * (float)Math.PI;


            bool flag = false;//标记始末状态的机器鱼鱼体朝向是否分别处于2,3象限
            bool flag_1 = false;
            bool flag_2 = false;
            bool flag_3 = false;

            EndFish.CalculateFishPostion(true, false, EndFish.InitPhase, ref EndFish);

            RoboFish startFish = (RoboFish)StartFish.Clone();
            RoboFish endFish = (RoboFish)EndFish.Clone();

            DichotomyResult result = new DichotomyResult();
            result.MidRoboFish = (RoboFish)EndFish.Clone();

            EulerAngle startbodyeulerangle = new EulerAngle();//用于存储起始状态鱼体的欧拉角
            EulerAngle endbodyeulerangle = new EulerAngle();//用于存储终止状态鱼体的欧拉角
            EulerAngle midbodyeulerangle = new EulerAngle();//用于存储终止中间状态鱼体的欧拉角

            FourVariables startbodyFourVar = new FourVariables();//用于存储起始状态鱼体的四元数
            FourVariables endbodyFourVar = new FourVariables();//用于存储终止状态鱼体的四元数
            FourVariables midbodyFourVar = new FourVariables();//用于存储终止中间状态鱼体的四元数

            //插入状态的鱼体上半身中心位置。
            //中间点坐标=（初始点坐标+结束点坐标）/2-----平动插值
            result.MidRoboFish.PositionMm.X = (startFish.PositionMm.X + endFish.PositionMm.X) / 2;
            result.MidRoboFish.PositionMm.Y = 0;
            result.MidRoboFish.PositionMm.Z = (startFish.PositionMm.Z + endFish.PositionMm.Z) / 2;

            if ((Quadrant(startFish.BodyDirectionRad) == 2 && Quadrant(endFish.BodyDirectionRad) == 3) ||
                (Quadrant(startFish.BodyDirectionRad) == 3 && Quadrant(endFish.BodyDirectionRad) == 2))
            {
                if (Quadrant(startFish.BodyDirectionRad) == 2)
                    startFish.BodyDirectionRad -= (float)Math.PI;
                else if (Quadrant(startFish.BodyDirectionRad) == 3)
                    startFish.BodyDirectionRad += (float)Math.PI;
                if (Quadrant(endFish.BodyDirectionRad) == 2)
                    endFish.BodyDirectionRad -= (float)Math.PI;
                else if (Quadrant(endFish.BodyDirectionRad) == 3)
                    endFish.BodyDirectionRad += (float)Math.PI;
                flag = true;
            }

            //计算起始状态鱼体朝向的欧拉角
            startbodyeulerangle.alpha = 0;
            startbodyeulerangle.beta = 0;
            startbodyeulerangle.gama = startFish.BodyDirectionRad;

            //计算末状态鱼体朝向的欧拉角
            endbodyeulerangle.alpha = 0;
            endbodyeulerangle.beta = 0;
            endbodyeulerangle.gama = endFish.BodyDirectionRad;

            //计算起始状态鱼体朝向的四元数
            startbodyFourVar = EulerToFourVar(startbodyeulerangle);

            //计算末状态鱼体朝向的四元数
            endbodyFourVar = EulerToFourVar(endbodyeulerangle);

            //转动插值运用球面四元数插值法
            midbodyFourVar = Slerp(startbodyFourVar, endbodyFourVar, (StartTime + EndTime) / 2);

            //得到中间状态的欧拉角
            midbodyeulerangle = FourVarToEuler(midbodyFourVar);

            if (flag == true)
            {
                if (midbodyeulerangle.gama > 0)
                    midbodyeulerangle.gama -= (float)Math.PI;
                else
                    midbodyeulerangle.gama += (float)Math.PI;
            }

            //得到中间状态鱼体朝向.
            result.MidRoboFish.BodyDirectionRad = midbodyeulerangle.gama;
            if (result.MidRoboFish.BodyDirectionRad == float.NaN)
            {
                result.MidRoboFish.BodyDirectionRad = midbodyeulerangle.gama;

            }
            

            //第一尾关节

            EulerAngle startTail1eulerangle = new EulerAngle();//用于存储起始状态鱼第一尾关节的欧拉角
            EulerAngle endTail1eulerangle = new EulerAngle();//用于存储末状态鱼第一尾关节的欧拉角
            EulerAngle midTail1eulerangle = new EulerAngle();//用于存储中间状态鱼第一尾关节的欧拉角

            FourVariables startTail1FourVar = new FourVariables();//用于存储起始状态鱼第一尾关节的四元数
            FourVariables endTail1FourVar = new FourVariables();//用于存储末状态鱼第一尾关节的四元数
            FourVariables midTail1FourVar = new FourVariables();//用于存储中间状态鱼第一尾关节的四元数

            float Tail1Start = startFish.TailToBodyAngle1 + startFish.BodyDirectionRad;
            float Tail1End = endFish.TailToBodyAngle1 + endFish.BodyDirectionRad;

            while ((Tail1Start) >= (float)Math.PI)
                Tail1Start -= 2 * (float)Math.PI;
            while ((Tail1Start) < -(float)Math.PI)
                Tail1Start += 2 * (float)Math.PI;

            while ((Tail1End) >= (float)Math.PI)
                (Tail1End) -= 2 * (float)Math.PI;
            while ((Tail1End) < -(float)Math.PI)
                (Tail1End) += 2 * (float)Math.PI;


            if ((Quadrant(Tail1Start) == 2 && Quadrant(Tail1End) == 3) ||
                (Quadrant(Tail1Start) == 3 && Quadrant(Tail1End) == 2))
            {
                if (Quadrant(Tail1Start) == 2)
                    (Tail1Start) -= (float)Math.PI;
                else if (Quadrant(Tail1Start) == 3)
                    (Tail1Start) += (float)Math.PI;
                if (Quadrant(Tail1End) == 2)
                    (Tail1End) -= (float)Math.PI;
                else if (Quadrant(Tail1End) == 3)
                    (Tail1End) += (float)Math.PI;
                flag_1 = true;
            }

            //计算起始状态鱼第一尾关节的欧拉角
            startTail1eulerangle.alpha = 0;
            startTail1eulerangle.beta = 0;
            startTail1eulerangle.gama = StartFish.TailToBodyAngle1 + StartFish.BodyDirectionRad;

            //计算起始状态鱼第一尾关节的四元数
            startTail1FourVar = EulerToFourVar(startTail1eulerangle);

            //计算末状态鱼第一尾关节的欧拉角
            endTail1eulerangle.alpha = 0;
            endTail1eulerangle.beta = 0;
            endTail1eulerangle.gama = EndFish.TailToBodyAngle1 + EndFish.BodyDirectionRad;

            //计算末状态鱼第一尾关节的四元数
            endTail1FourVar = EulerToFourVar(endTail1eulerangle);

            //四元数插差值得到中间状态的四元数
            midTail1FourVar = Slerp(startTail1FourVar, endTail1FourVar, (StartTime + EndTime) / 2);
            //得到中间状态的欧拉角
            midTail1eulerangle = FourVarToEuler(midTail1FourVar);
            if (flag_1 == true)
            {
                if (midTail1eulerangle.gama > 0)
                    midTail1eulerangle.gama -= (float)Math.PI;
                else
                    midTail1eulerangle.gama += (float)Math.PI;
            }
            //得到鱼尾第一关节的角度
            result.MidRoboFish.TailToBodyAngle1 = midTail1eulerangle.gama - result.MidRoboFish.BodyDirectionRad;
        

            //第二尾关节
            EulerAngle startTail2eulerangle = new EulerAngle();//用于存储起始状态鱼第二尾关节的欧拉角
            EulerAngle endTail2eulerangle = new EulerAngle();//用于存储末状态鱼第二尾关节的欧拉角
            EulerAngle midTail2eulerangle = new EulerAngle();//用于存储中间状态鱼第二尾关节的欧拉角

            FourVariables startTail2FourVar = new FourVariables();//用于存储起始状态鱼第二尾关节的四元数
            FourVariables endTail2FourVar = new FourVariables();//用于存储末状态鱼第二尾关节的四元数
            FourVariables midTail2FourVar = new FourVariables();//用于存储中间状态鱼第二尾关节的四元数

            float Tail2Start = startFish.TailToBodyAngle2 + startFish.BodyDirectionRad;
            float Tail2End = endFish.TailToBodyAngle2 + endFish.BodyDirectionRad;

            while ((Tail2Start) >= (float)Math.PI)
                (Tail2Start) -= 2 * (float)Math.PI;
            while ((Tail2Start) < -(float)Math.PI)
                (Tail2Start) += 2 * (float)Math.PI;

            while ((Tail2End) >= (float)Math.PI)
                (Tail2End) -= 2 * (float)Math.PI;
            while ((Tail2End) < -(float)Math.PI)
                (Tail2End) += 2 * (float)Math.PI;


            if ((Quadrant(Tail2Start) == 2 && Quadrant(Tail2End) == 3) ||
                (Quadrant(Tail2Start) == 3 && Quadrant(Tail2End) == 2))
            {
                if (Quadrant(Tail2Start) == 2)
                    (Tail2Start) -= (float)Math.PI;
                else if (Quadrant(Tail2Start) == 3)
                    (Tail2Start) += (float)Math.PI;
                if (Quadrant(Tail2End) == 2)
                    (Tail2End) -= (float)Math.PI;
                else if (Quadrant(Tail2End) == 3)
                    (Tail2End) += (float)Math.PI;
                flag_2 = true;
            }


            //计算起始状态鱼第二尾关节的欧拉角
            startTail2eulerangle.alpha = 0;
            startTail2eulerangle.beta = 0;
            startTail2eulerangle.gama = StartFish.TailToBodyAngle2 + StartFish.BodyDirectionRad;

            //计算起始状态鱼第二尾关节的四元数
            startTail2FourVar = EulerToFourVar(startTail2eulerangle);

            //计算末状态鱼第二尾关节的欧拉角
            endTail2eulerangle.alpha = 0;
            endTail2eulerangle.beta = 0;
            endTail2eulerangle.gama = EndFish.TailToBodyAngle2 + EndFish.BodyDirectionRad;

            //计算末状态鱼第二尾关节的四元数
            endTail2FourVar = EulerToFourVar(endTail2eulerangle);

            //四元数插差值得到中间状态的四元数
            midTail2FourVar = Slerp(startTail2FourVar, endTail2FourVar, (StartTime + EndTime) / 2);
            //得到中间状态的欧拉角
            midTail2eulerangle = FourVarToEuler(midTail2FourVar);
            if (flag_2 == true)
            {
                if (midTail2eulerangle.gama > 0)
                    midTail2eulerangle.gama -= (float)Math.PI;
                else
                    midTail2eulerangle.gama += (float)Math.PI;
            }
            //得到鱼尾第二关节的朝向
            result.MidRoboFish.TailToBodyAngle2 = midTail2eulerangle.gama - result.MidRoboFish.BodyDirectionRad;
            

            //第三尾关节
            EulerAngle startTail3eulerangle = new EulerAngle();//用于存储起始状态鱼第三尾关节的欧拉角
            EulerAngle endTail3eulerangle = new EulerAngle();//用于存储末状态鱼第三尾关节的欧拉角
            EulerAngle midTail3eulerangle = new EulerAngle();//用于存储中间状态鱼第三尾关节的欧拉角

            FourVariables startTail3FourVar = new FourVariables();//用于存储起始状态鱼第三尾关节的四元数
            FourVariables endTail3FourVar = new FourVariables();//用于存储末状态鱼第三尾关节的四元数
            FourVariables midTail3FourVar = new FourVariables();//用于存储中间状态鱼第三尾关节的四元数

            float Tail3Start = startFish.TailToBodyAngle3 + startFish.BodyDirectionRad;
            float Tail3End = endFish.TailToBodyAngle3 + endFish.BodyDirectionRad;

            while ((Tail3Start) >= (float)Math.PI)
                (Tail3Start) -= 2 * (float)Math.PI;
            while ((Tail3Start) < -(float)Math.PI)
                (Tail3Start) += 2 * (float)Math.PI;

            while ((Tail3End) >= (float)Math.PI)
                (Tail3End) -= 2 * (float)Math.PI;
            while ((Tail3End) < -(float)Math.PI)
                (Tail3End) += 2 * (float)Math.PI;


            if ((Quadrant(Tail3Start) == 2 && Quadrant(Tail3End) == 3) ||
                (Quadrant(Tail3Start) == 3 && Quadrant(Tail3End) == 2))
            {
                if (Quadrant(Tail3Start) == 2)
                    (Tail3Start) -= (float)Math.PI;
                else if (Quadrant(Tail3Start) == 3)
                    (Tail3Start) += (float)Math.PI;
                if (Quadrant(Tail3End) == 2)
                    (Tail3End) -= (float)Math.PI;
                else if (Quadrant(Tail3End) == 3)
                    (Tail3End) += (float)Math.PI;
                flag_3 = true;
            }

            //计算起始状态鱼第三尾关节的欧拉角
            startTail3eulerangle.alpha = 0;
            startTail3eulerangle.beta = 0;
            startTail3eulerangle.gama = StartFish.TailToBodyAngle3 + StartFish.BodyDirectionRad;
            //计算起始状态鱼第三尾关节的四元数
            startTail3FourVar = EulerToFourVar(startTail3eulerangle);

            //计算末状态鱼第三尾关节的欧拉角
            endTail3eulerangle.alpha = 0;
            endTail3eulerangle.beta = 0;
            endTail3eulerangle.gama = EndFish.TailToBodyAngle3 + EndFish.BodyDirectionRad;
            //计算末状态鱼第三尾关节的四元数
            endTail3FourVar = EulerToFourVar(endTail3eulerangle);

            //四元数插差值得到中间状态的四元数
            midTail3FourVar = Slerp(startTail3FourVar, endTail3FourVar, (StartTime + EndTime) / 2);
            //得到中间状态的欧拉角
            midTail3eulerangle = FourVarToEuler(midTail3FourVar);
            if (flag_3 == true)
            {
                if (midTail3eulerangle.gama > 0)
                    midTail3eulerangle.gama -= (float)Math.PI;
                else
                    midTail3eulerangle.gama += (float)Math.PI;
            }
            //得到鱼尾第三关节朝向
            result.MidRoboFish.TailToBodyAngle3 = midTail3eulerangle.gama - result.MidRoboFish.BodyDirectionRad;
            
            result.MidTime = (StartTime + EndTime) / 2;

            //MidFish.CopyTo(result.MidRoboFish);
            result.MidRoboFish.CalculateFishPostion(true, false, EndFish.InitPhase, ref  result.MidRoboFish);
            if (Math.Abs(Math.Abs(result.MidRoboFish.BodyDirectionRad) - Math.Abs(EndFish.BodyDirectionRad)) > Math.PI / 3)
            {
 
            }
            return result;
        }

        ///// <summary>
        ///// 仿真鱼的二分法 added by zhangjin 20120311
        ///// </summary>
        ///// <param name="StartFish">仿真机器鱼的初始状态</param>
        ///// <param name="EndFish">仿真机器鱼的终止状态</param>
        ///// <param name="StartTime">仿真模型的起始位置时刻</param>
        ///// <param name="EndTime">仿真模型的终止位置时刻</param>
        ///// <returns>返回二分法结果结构体</returns>
        //public static DichotomyResult Dichotomyfish(xna.Vector3 StartFishPositionMm,float StartFishBodyDirection,xna.Vector3 EndFishPositionMm,float EndFishBodyDirection, float StartTime, float EndTime)
        //{
        //    while (StartFish.BodyDirectionRad >= (float)Math.PI)
        //        StartFish.BodyDirectionRad -= 2 * (float)Math.PI;
        //    while (StartFish.BodyDirectionRad < -(float)Math.PI)
        //        StartFish.BodyDirectionRad += 2 * (float)Math.PI;

        //    while (EndFish.BodyDirectionRad >= (float)Math.PI)
        //        EndFish.BodyDirectionRad -= 2 * (float)Math.PI;
        //    while (EndFish.BodyDirectionRad < -(float)Math.PI)
        //        EndFish.BodyDirectionRad += 2 * (float)Math.PI;


        //    bool flag = false;//标记始末状态的机器鱼鱼体朝向是否分别处于2,3象限
        //    bool flag_1 = false;
        //    bool flag_2 = false;
        //    bool flag_3 = false;

        //    EndFish.CalculateFishPostion(true, false, EndFish.InitPhase, ref EndFish);

        //    RoboFish startFish = (RoboFish)StartFish.Clone();
        //    RoboFish endFish = (RoboFish)EndFish.Clone();

        //    DichotomyResult result = new DichotomyResult();
        //    result.MidRoboFish = (RoboFish)EndFish.Clone();

        //    EulerAngle startbodyeulerangle = new EulerAngle();//用于存储起始状态鱼体的欧拉角
        //    EulerAngle endbodyeulerangle = new EulerAngle();//用于存储终止状态鱼体的欧拉角
        //    EulerAngle midbodyeulerangle = new EulerAngle();//用于存储终止中间状态鱼体的欧拉角

        //    FourVariables startbodyFourVar = new FourVariables();//用于存储起始状态鱼体的四元数
        //    FourVariables endbodyFourVar = new FourVariables();//用于存储终止状态鱼体的四元数
        //    FourVariables midbodyFourVar = new FourVariables();//用于存储终止中间状态鱼体的四元数

        //    //插入状态的鱼体上半身中心位置。
        //    //中间点坐标=（初始点坐标+结束点坐标）/2-----平动插值
        //    result.MidRoboFish.PositionMm.X = (startFish.PositionMm.X + endFish.PositionMm.X) / 2;
        //    result.MidRoboFish.PositionMm.Y = 0;
        //    result.MidRoboFish.PositionMm.Z = (startFish.PositionMm.Z + endFish.PositionMm.Z) / 2;

        //    if ((Quadrant(startFish.BodyDirectionRad) == 2 && Quadrant(endFish.BodyDirectionRad) == 3) ||
        //        (Quadrant(startFish.BodyDirectionRad) == 3 && Quadrant(endFish.BodyDirectionRad) == 2))
        //    {
        //        if (Quadrant(startFish.BodyDirectionRad) == 2)
        //            startFish.BodyDirectionRad -= (float)Math.PI;
        //        else if (Quadrant(startFish.BodyDirectionRad) == 3)
        //            startFish.BodyDirectionRad += (float)Math.PI;
        //        if (Quadrant(endFish.BodyDirectionRad) == 2)
        //            endFish.BodyDirectionRad -= (float)Math.PI;
        //        else if (Quadrant(endFish.BodyDirectionRad) == 3)
        //            endFish.BodyDirectionRad += (float)Math.PI;
        //        flag = true;
        //    }

        //    //计算起始状态鱼体朝向的欧拉角
        //    startbodyeulerangle.alpha = 0;
        //    startbodyeulerangle.beta = 0;
        //    startbodyeulerangle.gama = startFish.BodyDirectionRad;

        //    //计算末状态鱼体朝向的欧拉角
        //    endbodyeulerangle.alpha = 0;
        //    endbodyeulerangle.beta = 0;
        //    endbodyeulerangle.gama = endFish.BodyDirectionRad;

        //    //计算起始状态鱼体朝向的四元数
        //    startbodyFourVar = EulerToFourVar(startbodyeulerangle);

        //    //计算末状态鱼体朝向的四元数
        //    endbodyFourVar = EulerToFourVar(endbodyeulerangle);

        //    //转动插值运用球面四元数插值法
        //    midbodyFourVar = Slerp(startbodyFourVar, endbodyFourVar, (StartTime + EndTime) / 2);

        //    //得到中间状态的欧拉角
        //    midbodyeulerangle = FourVarToEuler(midbodyFourVar);

        //    if (flag == true)
        //    {
        //        if (midbodyeulerangle.gama > 0)
        //            midbodyeulerangle.gama -= (float)Math.PI;
        //        else
        //            midbodyeulerangle.gama += (float)Math.PI;
        //    }

        //    //得到中间状态鱼体朝向.
        //    result.MidRoboFish.BodyDirectionRad = midbodyeulerangle.gama;


        //    //第一尾关节

        //    EulerAngle startTail1eulerangle = new EulerAngle();//用于存储起始状态鱼第一尾关节的欧拉角
        //    EulerAngle endTail1eulerangle = new EulerAngle();//用于存储末状态鱼第一尾关节的欧拉角
        //    EulerAngle midTail1eulerangle = new EulerAngle();//用于存储中间状态鱼第一尾关节的欧拉角

        //    FourVariables startTail1FourVar = new FourVariables();//用于存储起始状态鱼第一尾关节的四元数
        //    FourVariables endTail1FourVar = new FourVariables();//用于存储末状态鱼第一尾关节的四元数
        //    FourVariables midTail1FourVar = new FourVariables();//用于存储中间状态鱼第一尾关节的四元数

        //    float Tail1Start = startFish.TailToBodyAngle1 + startFish.BodyDirectionRad;
        //    float Tail1End = endFish.TailToBodyAngle1 + endFish.BodyDirectionRad;

        //    while ((Tail1Start) >= (float)Math.PI)
        //        Tail1Start -= 2 * (float)Math.PI;
        //    while ((Tail1Start) < -(float)Math.PI)
        //        Tail1Start += 2 * (float)Math.PI;

        //    while ((Tail1End) >= (float)Math.PI)
        //        (Tail1End) -= 2 * (float)Math.PI;
        //    while ((Tail1End) < -(float)Math.PI)
        //        (Tail1End) += 2 * (float)Math.PI;


        //    if ((Quadrant(Tail1Start) == 2 && Quadrant(Tail1End) == 3) ||
        //        (Quadrant(Tail1Start) == 3 && Quadrant(Tail1End) == 2))
        //    {
        //        if (Quadrant(Tail1Start) == 2)
        //            (Tail1Start) -= (float)Math.PI;
        //        else if (Quadrant(Tail1Start) == 3)
        //            (Tail1Start) += (float)Math.PI;
        //        if (Quadrant(Tail1End) == 2)
        //            (Tail1End) -= (float)Math.PI;
        //        else if (Quadrant(Tail1End) == 3)
        //            (Tail1End) += (float)Math.PI;
        //        flag_1 = true;
        //    }

        //    //计算起始状态鱼第一尾关节的欧拉角
        //    startTail1eulerangle.alpha = 0;
        //    startTail1eulerangle.beta = 0;
        //    startTail1eulerangle.gama = StartFish.TailToBodyAngle1 + StartFish.BodyDirectionRad;

        //    //计算起始状态鱼第一尾关节的四元数
        //    startTail1FourVar = EulerToFourVar(startTail1eulerangle);

        //    //计算末状态鱼第一尾关节的欧拉角
        //    endTail1eulerangle.alpha = 0;
        //    endTail1eulerangle.beta = 0;
        //    endTail1eulerangle.gama = EndFish.TailToBodyAngle1 + EndFish.BodyDirectionRad;

        //    //计算末状态鱼第一尾关节的四元数
        //    endTail1FourVar = EulerToFourVar(endTail1eulerangle);

        //    //四元数插差值得到中间状态的四元数
        //    midTail1FourVar = Slerp(startTail1FourVar, endTail1FourVar, (StartTime + EndTime) / 2);
        //    //得到中间状态的欧拉角
        //    midTail1eulerangle = FourVarToEuler(midTail1FourVar);
        //    if (flag_1 == true)
        //    {
        //        if (midTail1eulerangle.gama > 0)
        //            midTail1eulerangle.gama -= (float)Math.PI;
        //        else
        //            midTail1eulerangle.gama += (float)Math.PI;
        //    }
        //    //得到鱼尾第一关节的角度
        //    result.MidRoboFish.TailToBodyAngle1 = midTail1eulerangle.gama - result.MidRoboFish.BodyDirectionRad;


        //    //第二尾关节
        //    EulerAngle startTail2eulerangle = new EulerAngle();//用于存储起始状态鱼第二尾关节的欧拉角
        //    EulerAngle endTail2eulerangle = new EulerAngle();//用于存储末状态鱼第二尾关节的欧拉角
        //    EulerAngle midTail2eulerangle = new EulerAngle();//用于存储中间状态鱼第二尾关节的欧拉角

        //    FourVariables startTail2FourVar = new FourVariables();//用于存储起始状态鱼第二尾关节的四元数
        //    FourVariables endTail2FourVar = new FourVariables();//用于存储末状态鱼第二尾关节的四元数
        //    FourVariables midTail2FourVar = new FourVariables();//用于存储中间状态鱼第二尾关节的四元数

        //    float Tail2Start = startFish.TailToBodyAngle2 + startFish.BodyDirectionRad;
        //    float Tail2End = endFish.TailToBodyAngle2 + endFish.BodyDirectionRad;

        //    while ((Tail2Start) >= (float)Math.PI)
        //        (Tail2Start) -= 2 * (float)Math.PI;
        //    while ((Tail2Start) < -(float)Math.PI)
        //        (Tail2Start) += 2 * (float)Math.PI;

        //    while ((Tail2End) >= (float)Math.PI)
        //        (Tail2End) -= 2 * (float)Math.PI;
        //    while ((Tail2End) < -(float)Math.PI)
        //        (Tail2End) += 2 * (float)Math.PI;


        //    if ((Quadrant(Tail2Start) == 2 && Quadrant(Tail2End) == 3) ||
        //        (Quadrant(Tail2Start) == 3 && Quadrant(Tail2End) == 2))
        //    {
        //        if (Quadrant(Tail2Start) == 2)
        //            (Tail2Start) -= (float)Math.PI;
        //        else if (Quadrant(Tail2Start) == 3)
        //            (Tail2Start) += (float)Math.PI;
        //        if (Quadrant(Tail2End) == 2)
        //            (Tail2End) -= (float)Math.PI;
        //        else if (Quadrant(Tail2End) == 3)
        //            (Tail2End) += (float)Math.PI;
        //        flag_2 = true;
        //    }


        //    //计算起始状态鱼第二尾关节的欧拉角
        //    startTail2eulerangle.alpha = 0;
        //    startTail2eulerangle.beta = 0;
        //    startTail2eulerangle.gama = StartFish.TailToBodyAngle2 + StartFish.BodyDirectionRad;

        //    //计算起始状态鱼第二尾关节的四元数
        //    startTail2FourVar = EulerToFourVar(startTail2eulerangle);

        //    //计算末状态鱼第二尾关节的欧拉角
        //    endTail2eulerangle.alpha = 0;
        //    endTail2eulerangle.beta = 0;
        //    endTail2eulerangle.gama = EndFish.TailToBodyAngle2 + EndFish.BodyDirectionRad;

        //    //计算末状态鱼第二尾关节的四元数
        //    endTail2FourVar = EulerToFourVar(endTail2eulerangle);

        //    //四元数插差值得到中间状态的四元数
        //    midTail2FourVar = Slerp(startTail2FourVar, endTail2FourVar, (StartTime + EndTime) / 2);
        //    //得到中间状态的欧拉角
        //    midTail2eulerangle = FourVarToEuler(midTail2FourVar);
        //    if (flag_2 == true)
        //    {
        //        if (midTail2eulerangle.gama > 0)
        //            midTail2eulerangle.gama -= (float)Math.PI;
        //        else
        //            midTail2eulerangle.gama += (float)Math.PI;
        //    }
        //    //得到鱼尾第二关节的朝向
        //    result.MidRoboFish.TailToBodyAngle2 = midTail2eulerangle.gama - result.MidRoboFish.BodyDirectionRad;


        //    //第三尾关节
        //    EulerAngle startTail3eulerangle = new EulerAngle();//用于存储起始状态鱼第三尾关节的欧拉角
        //    EulerAngle endTail3eulerangle = new EulerAngle();//用于存储末状态鱼第三尾关节的欧拉角
        //    EulerAngle midTail3eulerangle = new EulerAngle();//用于存储中间状态鱼第三尾关节的欧拉角

        //    FourVariables startTail3FourVar = new FourVariables();//用于存储起始状态鱼第三尾关节的四元数
        //    FourVariables endTail3FourVar = new FourVariables();//用于存储末状态鱼第三尾关节的四元数
        //    FourVariables midTail3FourVar = new FourVariables();//用于存储中间状态鱼第三尾关节的四元数

        //    float Tail3Start = startFish.TailToBodyAngle3 + startFish.BodyDirectionRad;
        //    float Tail3End = endFish.TailToBodyAngle3 + endFish.BodyDirectionRad;

        //    while ((Tail3Start) >= (float)Math.PI)
        //        (Tail3Start) -= 2 * (float)Math.PI;
        //    while ((Tail3Start) < -(float)Math.PI)
        //        (Tail3Start) += 2 * (float)Math.PI;

        //    while ((Tail3End) >= (float)Math.PI)
        //        (Tail3End) -= 2 * (float)Math.PI;
        //    while ((Tail3End) < -(float)Math.PI)
        //        (Tail3End) += 2 * (float)Math.PI;


        //    if ((Quadrant(Tail3Start) == 2 && Quadrant(Tail3End) == 3) ||
        //        (Quadrant(Tail3Start) == 3 && Quadrant(Tail3End) == 2))
        //    {
        //        if (Quadrant(Tail3Start) == 2)
        //            (Tail3Start) -= (float)Math.PI;
        //        else if (Quadrant(Tail3Start) == 3)
        //            (Tail3Start) += (float)Math.PI;
        //        if (Quadrant(Tail3End) == 2)
        //            (Tail3End) -= (float)Math.PI;
        //        else if (Quadrant(Tail3End) == 3)
        //            (Tail3End) += (float)Math.PI;
        //        flag_3 = true;
        //    }

        //    //计算起始状态鱼第三尾关节的欧拉角
        //    startTail3eulerangle.alpha = 0;
        //    startTail3eulerangle.beta = 0;
        //    startTail3eulerangle.gama = StartFish.TailToBodyAngle3 + StartFish.BodyDirectionRad;
        //    //计算起始状态鱼第三尾关节的四元数
        //    startTail3FourVar = EulerToFourVar(startTail3eulerangle);

        //    //计算末状态鱼第三尾关节的欧拉角
        //    endTail3eulerangle.alpha = 0;
        //    endTail3eulerangle.beta = 0;
        //    endTail3eulerangle.gama = EndFish.TailToBodyAngle3 + EndFish.BodyDirectionRad;
        //    //计算末状态鱼第三尾关节的四元数
        //    endTail3FourVar = EulerToFourVar(endTail3eulerangle);

        //    //四元数插差值得到中间状态的四元数
        //    midTail3FourVar = Slerp(startTail3FourVar, endTail3FourVar, (StartTime + EndTime) / 2);
        //    //得到中间状态的欧拉角
        //    midTail3eulerangle = FourVarToEuler(midTail3FourVar);
        //    if (flag_3 == true)
        //    {
        //        if (midTail3eulerangle.gama > 0)
        //            midTail3eulerangle.gama -= (float)Math.PI;
        //        else
        //            midTail3eulerangle.gama += (float)Math.PI;
        //    }
        //    //得到鱼尾第三关节朝向
        //    result.MidRoboFish.TailToBodyAngle3 = midTail3eulerangle.gama - result.MidRoboFish.BodyDirectionRad;

        //    result.MidTime = (StartTime + EndTime) / 2;

        //    //MidFish.CopyTo(result.MidRoboFish);
        //    result.MidRoboFish.CalculateFishPostion(true, false, EndFish.InitPhase, ref  result.MidRoboFish);
        //    if (Math.Abs(Math.Abs(result.MidRoboFish.BodyDirectionRad) - Math.Abs(EndFish.BodyDirectionRad)) > Math.PI / 3)
        //    {

        //    }
        //    return result;
        //}
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="StartFish"></param>
        /// <param name="EndFish"></param>
        /// <param name="StartTime"></param>
        /// <param name="EndTime"></param>
        /// <returns></returns>
        public static DichotomyResult Dichotomyfish2(RoboFish StartFish, RoboFish EndFish, float StartTime, float EndTime)
        {
            DichotomyResult result = new DichotomyResult();
            result.MidRoboFish = (RoboFish)EndFish.Clone();

            result.MidRoboFish.PositionMm.X = (StartFish.PositionMm.X + EndFish.PositionMm.X) / 2;
            result.MidRoboFish.PositionMm.Y = 0;
            result.MidRoboFish.PositionMm.Z = (StartFish.PositionMm.Z + EndFish.PositionMm.Z) / 2;

            return result;
 
        }

        /// <summary>
        /// 判断角度在几象限
        /// </summary>
        /// <param name="Angle">角度值</param>
        /// <returns>返回象限数，1,2,3,4</returns>
        public static int Quadrant(float Angle)
        {
            int quadrant = 0;
            while (Angle >= (float)Math.PI)
                Angle = Angle - 2 * (float)Math.PI;
            while (Angle < -(float)Math.PI)
                Angle = Angle + 2 * (float)Math.PI;

            if (Angle >= 0 && Angle < (float)Math.PI / 2)
                quadrant = 1;
            else if (Angle >= (float)Math.PI / 2 && Angle < (float)Math.PI)
                quadrant = 2;
            else if (Angle >= -(float)Math.PI && Angle < -(float)Math.PI / 2)
                quadrant = 3;
            else
                quadrant = 4;

            return quadrant;
        }

        #region 欧拉角和四元函数的相互转换
        /// <summary>
        /// 欧拉角转换为四元函数 added by zhangjin 20120311
        /// </summary>
        /// <param name="eulerangle">与X轴的夹角</param>
        /// <returns>返回四元函数结果结构体</returns>
        public static FourVariables EulerToFourVar(EulerAngle eulerangle)
        {
            FourVariables result = new FourVariables();

            result.s = (float)Math.Cos(eulerangle.alpha / 2) * (float)Math.Cos(eulerangle.beta / 2) * (float)Math.Cos(eulerangle.gama / 2)
                     + (float)Math.Sin(eulerangle.alpha / 2) * (float)Math.Sin(eulerangle.beta / 2) * (float)Math.Sin(eulerangle.gama / 2);

            result.x = (float)Math.Cos(eulerangle.alpha / 2) * (float)Math.Sin(eulerangle.beta / 2) * (float)Math.Cos(eulerangle.gama / 2)
                     + (float)Math.Sin(eulerangle.alpha / 2) * (float)Math.Cos(eulerangle.beta / 2) * (float)Math.Sin(eulerangle.gama / 2);

            result.y = (float)Math.Sin(eulerangle.alpha / 2) * (float)Math.Cos(eulerangle.beta / 2) * (float)Math.Cos(eulerangle.gama / 2)
                     - (float)Math.Cos(eulerangle.alpha / 2) * (float)Math.Sin(eulerangle.beta / 2) * (float)Math.Sin(eulerangle.gama / 2);

            result.z = (float)Math.Cos(eulerangle.alpha / 2) * (float)Math.Cos(eulerangle.beta / 2) * (float)Math.Sin(eulerangle.gama / 2)
                     - (float)Math.Sin(eulerangle.alpha / 2) * (float)Math.Sin(eulerangle.beta / 2) * (float)Math.Cos(eulerangle.gama / 2);
            return result;
        }

        /// <summary>
        /// 四元函数转换为欧拉角 added by zhangjin 20120311
        /// </summary>
        /// <param name="fvar"></param>
        /// <returns>返回四元函数结果结构体</returns>
        public static EulerAngle FourVarToEuler(FourVariables fvar)
        {
            EulerAngle result = new EulerAngle();
            if (Math.Abs(1 - 2 * (fvar.x * fvar.x + fvar.y * fvar.y)) >= float.Epsilon)
            {
                result.alpha = (float)Math.Atan2(2 * (fvar.s * fvar.x + fvar.y * fvar.z), (1 - 2 * (fvar.x * fvar.x + fvar.y * fvar.y)));
            }
            else
            {
                result.alpha =(float) Math.PI;

            }
            result.beta = (float)Math.Asin(2 * (fvar.s * fvar.y - fvar.z * fvar.x));
            if (Math.Abs(1 - 2 * (fvar.y * fvar.y + fvar.z * fvar.z)) >= float.Epsilon)
            {
                result.gama = (float)Math.Atan2(2 * (fvar.s * fvar.z + fvar.x * fvar.y), (1 - 2 * (fvar.y * fvar.y + fvar.z * fvar.z)));
            }
            else
            {
                result.gama = (float)Math.PI;
            }
            return result;
        }
        #endregion

        #region 四元数差值
        /// <summary>
        /// 四元数差值 added by zhangjin 20120311
        /// </summary>
        /// <param name="startFourVar">前一周期的四元数</param>
        /// <param name="endFourVar">当前周期的四元数</param>
        /// <param name="time">插入时刻</param>
        /// <returns>返回二分法结果结构体</returns>
        public static FourVariables Slerp(FourVariables startFourVar, FourVariables endFourVar, float time)
        {
            /* 
             * Q(t) = sin(θ*(1-t)) / sinθ * Q1 + sin(θ*t) / sinθ * Q2
             * 其中0<=t<=1，θ为Q1与Q2之间的夹角
             *θ= arccos(Q1 dot Q2)
             */
            FourVariables result = new FourVariables();
            float startdenominator = (float)Math.Sqrt(startFourVar.s * startFourVar.s + startFourVar.x * startFourVar.x + startFourVar.y * startFourVar.y + startFourVar.z * startFourVar.z);
            float enddenominator = (float)Math.Sqrt(endFourVar.s * endFourVar.s + endFourVar.x * endFourVar.x + endFourVar.y * endFourVar.y + endFourVar.z * endFourVar.z);
            float dotFourVar = (startFourVar.s * endFourVar.s + startFourVar.x * endFourVar.x + startFourVar.y * endFourVar.y + startFourVar.z * endFourVar.z) / (startdenominator * enddenominator);

            float angle = (float)Math.Acos(dotFourVar);//计算得到两向量的夹角
            if (Math.Abs(angle)>=float.Epsilon)
            {
                result.s = (((float)Math.Sin((1 - time) * angle)) / (float)Math.Sin(angle)) * startFourVar.s + ((float)Math.Sin(time * angle) / (float)Math.Sin(angle)) * endFourVar.s;
                result.x = (((float)Math.Sin((1 - time) * angle)) / (float)Math.Sin(angle)) * startFourVar.x + ((float)Math.Sin(time * angle) / (float)Math.Sin(angle)) * endFourVar.x;
                result.y = (((float)Math.Sin((1 - time) * angle)) / (float)Math.Sin(angle)) * startFourVar.y + ((float)Math.Sin(time * angle) / (float)Math.Sin(angle)) * endFourVar.y;
                result.z = (((float)Math.Sin((1 - time) * angle)) / (float)Math.Sin(angle)) * startFourVar.z + ((float)Math.Sin(time * angle) / (float)Math.Sin(angle)) * endFourVar.z;
            }
            else
            {
                result.s = endFourVar.s;
                result.x = endFourVar.x;
                result.y = endFourVar.y;
                result.z = endFourVar.z;
            }
            return result;
        }
        #endregion

        #region 仿真机器鱼和仿真水球的碰撞检测 modified by renjing 20110419
        /// <summary>
        /// 检测仿真机器鱼和仿真水球的碰撞情况
        /// </summary>
        /// <param name="fish">待检测的仿真机器鱼对象引用</param>
        /// <param name="ball">待检测的仿真水球对象引用</param>
        /// <returns>碰撞检测结果参数结构体包括是否碰撞/碰撞作用面中的法向/碰撞作用点坐标</returns>
        public static CollisionDetectionResult DetectCollisionBetweenFishAndBall(ref RoboFish fish, ref Ball ball)
        {
            CollisionDetectionResult result = new CollisionDetectionResult();
            result.Intersect = false;
            MyMission myMission = MyMission.Instance();
            float MaxDis = 0;//记录最长相交向量的模长
            
            float distance = xna.Vector3.Distance(fish.CollisionModelCenterPositionMm, ball.PositionMm);//球心到鱼碰撞模型BV树根结点圆模型中心点的距离
            //球和根结点的碰撞检测
            if (fish.CollisionModelRadiusMm + ball.RadiusMm >= distance)
            {   
                float distanceaBody = xna.Vector3.Distance(fish.CollisionModelBodyCenterPositionMm, ball.PositionMm);//球心到BV树第二层鱼刚体部分子结点圆模型中心点的距离
                float distanceaTail = xna.Vector3.Distance(fish.CollisionModelTailCenterPositionMm, ball.PositionMm);//球心到BV树第二层鱼尾部子结点圆模型中心点的距离
                //球和刚体部分子结点的碰撞检测
                if (fish.CollisionModelBodyRadiusMm + ball.RadiusMm >= distanceaBody)
                {
                    CollisionModelPolygon polygonFishBody = new CollisionModelPolygon(fish.BodyPolygonVertices);
                    CollisionModelPolygon polygonFishLeftPectoral = new CollisionModelPolygon(fish.LeftPectoralPolygonVertices);
                    CollisionModelPolygon polygonFishRightPectoral = new CollisionModelPolygon(fish.RightPectoralPolygonVertices);

                    CollisionDetectionResult resultBody = CollisionBetweenPolygonAndCircle(polygonFishBody, ball, false);//检测和身体躯干部分是否有碰撞
                    if (resultBody.Intersect == true)
                    {
                        fish.PositionMm += resultBody.MinimumTranslationVector / 2;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        ball.PositionMm -= resultBody.MinimumTranslationVector / 2;
                        if (Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z);
                            result = resultBody;
                            result.LeafNodeA = 2;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultLeft = CollisionBetweenPolygonAndCircle(polygonFishLeftPectoral, ball, false);//检测和左胸鳍是否有碰撞
                    if (resultLeft.Intersect == true)
                    {
                        fish.PositionMm += resultLeft.MinimumTranslationVector / 2;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        ball.PositionMm -= 1.5f * resultLeft.MinimumTranslationVector / 2;
                        if (Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z);
                            result = resultLeft;
                            result.LeafNodeA = 1;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultRight = CollisionBetweenPolygonAndCircle(polygonFishRightPectoral, ball, false);//检测和右胸鳍是否有碰撞
                    if (resultRight.Intersect == true)
                    {
                        fish.PositionMm += resultRight.MinimumTranslationVector / 2;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        ball.PositionMm -= 1.5f * resultRight.MinimumTranslationVector / 2;
                        if (Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z);
                            result = resultRight;
                            result.LeafNodeA = 3;
                            result.LeafNodeB = 0;
                        }
                    }
                    
                }
                //球和鱼尾部分子结点的碰撞检测
                if (fish.CollisionModelTailRadiusMm + ball.RadiusMm >= distanceaTail)
                {
                    CollisionModelPolygon polygonFishTail1 = new CollisionModelPolygon(fish.Tail1PolygonVertices);
                    CollisionModelPolygon polygonFishTail2 = new CollisionModelPolygon(fish.Tail2PolygonVertices);
                    CollisionModelPolygon polygonFishTail3 = new CollisionModelPolygon(fish.Tail3PolygonVertices);
                    CollisionModelPolygon polygonLeftCaudal = new CollisionModelPolygon(fish.LeftCaudalFinVertices);
                    CollisionModelPolygon polygonRightCaudal = new CollisionModelPolygon(fish.RightCaudalFinVertices);

                    CollisionDetectionResult resultTail3 = CollisionBetweenPolygonAndCircle(polygonFishTail3, ball, false);//检测和鱼尾第三关节是否有碰撞
                    if (resultTail3.Intersect == true)
                    {
                        fish.PositionMm += resultTail3.MinimumTranslationVector / 2;
                        ball.PositionMm -= resultTail3.MinimumTranslationVector / 2;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z);
                            result = resultTail3;
                            result.LeafNodeA = 6;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultTail2 = CollisionBetweenPolygonAndCircle(polygonFishTail2, ball, false);//检测和鱼尾第二关节是否有碰撞
                    if (resultTail2.Intersect == true)
                    {
                        fish.PositionMm += resultTail2.MinimumTranslationVector / 2;
                        ball.PositionMm -= resultTail2.MinimumTranslationVector / 2;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z);
                            result = resultTail2;
                            result.LeafNodeA = 5;
                            result.LeafNodeB = 0;
                        }
                    }

                    CollisionDetectionResult resultTail1 = CollisionBetweenPolygonAndCircle(polygonFishTail1, ball, false);//检测和鱼尾第一关节是否有碰撞
                    if (resultTail1.Intersect == true)
                    {
                        fish.PositionMm += resultTail1.MinimumTranslationVector / 2;
                        ball.PositionMm -= resultTail1.MinimumTranslationVector / 2;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z);
                            result = resultTail1;
                            result.LeafNodeA = 4;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultLeftCaudal = CollisionBetweenPolygonAndCircle(polygonLeftCaudal, ball, false);//检测和鱼尾左侧关节节是否有碰撞
                    if (resultLeftCaudal.Intersect == true)
                    {
                        fish.PositionMm += resultLeftCaudal.MinimumTranslationVector / 2;
                        ball.PositionMm -= resultLeftCaudal.MinimumTranslationVector / 2;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z);
                            result = resultLeftCaudal;
                            result.LeafNodeA = 7;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultRightCaudal = CollisionBetweenPolygonAndCircle(polygonRightCaudal, ball, false);//检测和鱼尾右侧关节是否有碰撞
                    if (resultRightCaudal.Intersect == true)
                    {
                        fish.PositionMm += resultRightCaudal.MinimumTranslationVector / 2;
                        ball.PositionMm -= resultRightCaudal.MinimumTranslationVector / 2;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z);
                            result = resultRightCaudal;
                            result.LeafNodeA = 8;
                            result.LeafNodeB = 0;
                        }
                    }
                }
            }

            if (result.Intersect == true)
            {// 为仿真机器鱼添加碰撞标志 LiYoubing 20110511
                fish.Collision.Add(CollisionType.FISH_BALL);
                //add by caiqiong 2011-3-11
                ball.Collision = CollisionType.FISH_BALL;
            }

            return result;
        }
        #endregion

        #region 两条仿真机器鱼之间的碰撞检测 modified by renjing 20110420
        /// <summary>
        /// 检测两条仿真机器鱼之间碰撞情况
        /// </summary>
        /// <param name="fish1">待检测的第1条仿真机器鱼对象引用</param>
        /// <param name="fish2">待检测的第2条仿真机器鱼对象引用</param>
        /// <returns>碰撞检测结果参数结构体包括是否碰撞/碰撞作用面中的法向/碰撞作用点坐标</returns>
        public static CollisionDetectionResult DetectCollisionBetweenTwoFishes(
            ref RoboFish fish1, ref RoboFish fish2)
        {
            float tempDistance = xna.Vector3.Distance(fish1.CollisionModelCenterPositionMm, fish2.CollisionModelCenterPositionMm);
            CollisionDetectionResult result = new CollisionDetectionResult();
            result.Intersect = false;
            MyMission myMission = MyMission.Instance();
            float MaxDis = 0;

            // 外层碰撞检测
            if (fish1.CollisionModelRadiusMm + fish2.CollisionModelRadiusMm >= tempDistance)
            {// 内层碰撞检测
                //CollisionModelPolygon polygonFish1 = new CollisionModelPolygon(fish1.PolygonVertices);
                //CollisionModelPolygon polygonFish2 = new CollisionModelPolygon(fish2.PolygonVertices);
                float distanceBodyToBody = xna.Vector3.Distance(fish1.CollisionModelBodyCenterPositionMm, fish2.CollisionModelBodyCenterPositionMm);//BV树第二层，鱼1和鱼2刚体部分圆心间的距离
                float distanceTailToTail = xna.Vector3.Distance(fish1.CollisionModelTailCenterPositionMm, fish2.CollisionModelTailCenterPositionMm);//BV树第二层，鱼1和鱼2尾部圆心间的距离
                float distanceBody1ToTail2 = xna.Vector3.Distance(fish1.CollisionModelBodyCenterPositionMm, fish2.CollisionModelTailCenterPositionMm);//BV树第二层，鱼1刚体部分和鱼2尾部圆心间的距离
                float distanceTail1ToBody2 = xna.Vector3.Distance(fish1.CollisionModelTailCenterPositionMm, fish2.CollisionModelBodyCenterPositionMm);//BV树第二层，鱼1尾部和鱼2刚体部分圆心间的距离
                CollisionModelPolygon polygonBody1 = new CollisionModelPolygon(fish1.BodyPolygonVertices);//BV树第三层，鱼1躯干部分模型
                CollisionModelPolygon polygonLeftPectoral1 = new CollisionModelPolygon(fish1.LeftPectoralPolygonVertices);//BV树第三层，鱼1左胸鳍模型
                CollisionModelPolygon polygonRightPectoral1 = new CollisionModelPolygon(fish1.RightPectoralPolygonVertices);//BV树第三层，鱼1右胸鳍模型
                CollisionModelPolygon polygonTail11 = new CollisionModelPolygon(fish1.Tail1PolygonVertices);//BV树第三层，鱼1第一关节模型
                CollisionModelPolygon polygonTail21 = new CollisionModelPolygon(fish1.Tail2PolygonVertices);//BV树第三层，鱼1第二关节模型
                CollisionModelPolygon polygonTail31 = new CollisionModelPolygon(fish1.Tail3PolygonVertices);//BV树第三层，鱼1第三关节模型
                CollisionModelPolygon polygonLeftCaudalFin1 = new CollisionModelPolygon(fish1.LeftCaudalFinVertices);//BV树第三层，鱼1左尾鳍
                CollisionModelPolygon polygonRightCaudalFin1 = new CollisionModelPolygon(fish1.RightCaudalFinVertices);//BV树第三层，鱼1右尾鳍

                CollisionModelPolygon polygonBody2 = new CollisionModelPolygon(fish2.BodyPolygonVertices);//BV树第三层，鱼2躯干部分模型
                CollisionModelPolygon polygonLeftPectoral2 = new CollisionModelPolygon(fish2.LeftPectoralPolygonVertices);//BV树第三层，鱼2左胸鳍模型
                CollisionModelPolygon polygonRightPectoral2 = new CollisionModelPolygon(fish2.RightPectoralPolygonVertices);//BV树第三层，鱼2右胸鳍模型
                CollisionModelPolygon polygonTail12 = new CollisionModelPolygon(fish2.Tail1PolygonVertices);//BV树第三层，鱼2第一关节模型
                CollisionModelPolygon polygonTail22 = new CollisionModelPolygon(fish2.Tail2PolygonVertices);//BV树第三层，鱼2第二关节模型
                CollisionModelPolygon polygonTail32 = new CollisionModelPolygon(fish2.Tail3PolygonVertices);//BV树第三层，鱼2第三关节模型
                CollisionModelPolygon polygonLeftCaudalFin2 = new CollisionModelPolygon(fish2.LeftCaudalFinVertices);//BV树第三层，鱼2左尾鳍
                CollisionModelPolygon polygonRightCaudalFin2 = new CollisionModelPolygon(fish2.RightCaudalFinVertices);//BV树第三层，鱼2右尾鳍

                xna.Vector3 minimumTranslationVector = new xna.Vector3();

                #region BV树第二层,鱼躯干部分圆模型之间的检测
                if (fish1.CollisionModelBodyRadiusMm + fish2.CollisionModelBodyRadiusMm >= distanceBodyToBody)
                {

                    CollisionDetectionResult resultLR = CollisionBetweenTwoPolygons(polygonLeftPectoral1, polygonRightPectoral2, false);//BV树第三层，鱼1左胸鳍部分和鱼2右胸鳍部分碰撞检测
                    if (resultLR.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLR.NormalAxis, fish1.VelocityDirectionRad, ref resultLR.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLR.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLR.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLR.MinimumTranslationVector.X * resultLR.MinimumTranslationVector.X + resultLR.MinimumTranslationVector.Z * resultLR.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLR.MinimumTranslationVector.X * resultLR.MinimumTranslationVector.X + resultLR.MinimumTranslationVector.Z * resultLR.MinimumTranslationVector.Z);
                            result = resultLR;
                            result.LeafNodeA = 1;
                            result.LeafNodeB = 3;
                        }
                    }

                    CollisionDetectionResult resultRL = CollisionBetweenTwoPolygons(polygonRightPectoral1, polygonLeftPectoral2, false);//BV树第三层，鱼1右胸鳍部分和鱼2左胸鳍部分碰撞检测
                    if (resultRL.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRL.NormalAxis, fish1.VelocityDirectionRad, ref resultRL.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRL.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRL.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRL.MinimumTranslationVector.X * resultRL.MinimumTranslationVector.X + resultRL.MinimumTranslationVector.Z * resultRL.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRL.MinimumTranslationVector.X * resultRL.MinimumTranslationVector.X + resultRL.MinimumTranslationVector.Z * resultRL.MinimumTranslationVector.Z);
                            result = resultRL;
                            result.LeafNodeA = 3;
                            result.LeafNodeB = 1;
                        }
                    }

                    CollisionDetectionResult resultLL = CollisionBetweenTwoPolygons(polygonLeftPectoral1, polygonLeftPectoral2, false);//BV树第三层，鱼1左胸鳍部分和鱼2左胸鳍部分碰撞检测
                    if (resultLL.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLL.NormalAxis, fish1.VelocityDirectionRad, ref resultLL.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLL.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLL.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLL.MinimumTranslationVector.X * resultLL.MinimumTranslationVector.X + resultLL.MinimumTranslationVector.Z * resultLL.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLL.MinimumTranslationVector.X * resultLL.MinimumTranslationVector.X + resultLL.MinimumTranslationVector.Z * resultLL.MinimumTranslationVector.Z);
                            result = resultLL;
                            result.LeafNodeA = 1;
                            result.LeafNodeB = 1;
                        }
                    }

                    CollisionDetectionResult resultRR = CollisionBetweenTwoPolygons(polygonRightPectoral1, polygonRightPectoral2, false);//BV树第三层，鱼1右胸鳍部分和鱼2右胸鳍部分碰撞检测
                    if (resultRR.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRR.NormalAxis, fish1.VelocityDirectionRad, ref resultRR.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRR.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRR.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRR.MinimumTranslationVector.X * resultRR.MinimumTranslationVector.X + resultRR.MinimumTranslationVector.Z * resultRR.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRR.MinimumTranslationVector.X * resultRR.MinimumTranslationVector.X + resultRR.MinimumTranslationVector.Z * resultRR.MinimumTranslationVector.Z);
                            result = resultRR;
                            result.LeafNodeA = 3;
                            result.LeafNodeB = 3;
                        }
                    }

                    CollisionDetectionResult resultMM = CollisionBetweenTwoPolygons(polygonBody1, polygonBody2, false);//BV树第三层，鱼1躯干部分和鱼2躯干部分碰撞检测
                    if (resultMM.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultMM.NormalAxis, fish1.VelocityDirectionRad, ref resultMM.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultMM.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultMM.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultMM.MinimumTranslationVector.X * resultMM.MinimumTranslationVector.X + resultMM.MinimumTranslationVector.Z * resultMM.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultMM.MinimumTranslationVector.X * resultMM.MinimumTranslationVector.X + resultMM.MinimumTranslationVector.Z * resultMM.MinimumTranslationVector.Z);
                            result = resultMM;
                            result.LeafNodeA = 2;
                            result.LeafNodeB = 2;
                        }
                    }

                    CollisionDetectionResult resultLM = CollisionBetweenTwoPolygons(polygonLeftPectoral1, polygonBody2, false);//BV树第三层，鱼1左胸鳍部分和鱼2躯干部分碰撞检测
                    if (resultLM.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLM.NormalAxis, fish1.VelocityDirectionRad, ref resultLM.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLM.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLM.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLM.MinimumTranslationVector.X * resultLM.MinimumTranslationVector.X + resultLM.MinimumTranslationVector.Z * resultLM.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLM.MinimumTranslationVector.X * resultLM.MinimumTranslationVector.X + resultLM.MinimumTranslationVector.Z * resultLM.MinimumTranslationVector.Z);
                            result = resultLM;
                            result.LeafNodeA = 1;
                            result.LeafNodeB = 2;
                        }
                    }

                    CollisionDetectionResult resultRM = CollisionBetweenTwoPolygons(polygonRightPectoral1, polygonBody2, false);//BV树第三层，鱼1右胸鳍部分和鱼2躯干部分碰撞检测
                    if (resultRM.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRM.NormalAxis, fish1.VelocityDirectionRad, ref resultRM.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRM.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRM.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRM.MinimumTranslationVector.X * resultRM.MinimumTranslationVector.X + resultRM.MinimumTranslationVector.Z * resultRM.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRM.MinimumTranslationVector.X * resultRM.MinimumTranslationVector.X + resultRM.MinimumTranslationVector.Z * resultRM.MinimumTranslationVector.Z);
                            result = resultRM;
                            result.LeafNodeA = 3;
                            result.LeafNodeB = 2;
                        }
                    }

                    CollisionDetectionResult resultML = CollisionBetweenTwoPolygons(polygonBody1, polygonLeftPectoral2, false);//BV树第三层，鱼1躯干部分和鱼2左胸鳍部分碰撞检测
                    if (resultML.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultML.NormalAxis, fish1.VelocityDirectionRad, ref resultML.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultML.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultML.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultML.MinimumTranslationVector.X * resultML.MinimumTranslationVector.X + resultML.MinimumTranslationVector.Z * resultML.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultML.MinimumTranslationVector.X * resultML.MinimumTranslationVector.X + resultML.MinimumTranslationVector.Z * resultML.MinimumTranslationVector.Z);
                            result = resultML;
                            result.LeafNodeA = 2;
                            result.LeafNodeB = 1;
                        }
                    }

                    CollisionDetectionResult resultMR = CollisionBetweenTwoPolygons(polygonBody1, polygonRightPectoral2, false);//BV树第三层，鱼1躯干部分和鱼2右胸鳍部分碰撞检测
                    if (resultMR.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultMR.NormalAxis, fish1.VelocityDirectionRad, ref resultMR.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultMR.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultMR.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultMR.MinimumTranslationVector.X * resultMR.MinimumTranslationVector.X + resultMR.MinimumTranslationVector.Z * resultMR.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultMR.MinimumTranslationVector.X * resultMR.MinimumTranslationVector.X + resultMR.MinimumTranslationVector.Z * resultMR.MinimumTranslationVector.Z);
                            result = resultMR;
                            result.LeafNodeA = 2;
                            result.LeafNodeB = 3;
                        }
                    }

                }
                #endregion

                #region BV树第二层,鱼躯干圆模型和鱼尾圆模型检测
                if (fish1.CollisionModelBodyRadiusMm + fish2.CollisionModelTailRadiusMm >= distanceBody1ToTail2)
                {
                    //鱼1左胸鳍一次只可能与鱼2三个关节中的一个或尾鳍有碰撞
                    CollisionDetectionResult resultL1 = CollisionBetweenTwoPolygons(polygonLeftPectoral1, polygonTail12, false);//BV树第三层，鱼1左胸鳍部分和鱼2第一个关节碰撞检测
                    if (resultL1.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultL1.NormalAxis, fish1.VelocityDirectionRad, ref resultL1.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultL1.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultL1.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultL1.MinimumTranslationVector.X * resultL1.MinimumTranslationVector.X + resultL1.MinimumTranslationVector.Z * resultL1.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultL1.MinimumTranslationVector.X * resultL1.MinimumTranslationVector.X + resultL1.MinimumTranslationVector.Z * resultL1.MinimumTranslationVector.Z);
                            result = resultL1;
                            result.LeafNodeA = 1;
                            result.LeafNodeB = 4;
                        }

                    }
                    CollisionDetectionResult resultL2 = CollisionBetweenTwoPolygons(polygonLeftPectoral1, polygonTail22, false);//BV树第三层，鱼1左胸鳍部分和鱼2第二个关节碰撞检测
                    if (resultL2.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultL2.NormalAxis, fish1.VelocityDirectionRad, ref resultL2.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultL2.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultL2.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultL2.MinimumTranslationVector.X * resultL2.MinimumTranslationVector.X + resultL2.MinimumTranslationVector.Z * resultL2.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultL2.MinimumTranslationVector.X * resultL2.MinimumTranslationVector.X + resultL2.MinimumTranslationVector.Z * resultL2.MinimumTranslationVector.Z);
                            result = resultL2;
                            result.LeafNodeA = 1;
                            result.LeafNodeB = 5;
                        }
                    }

                    CollisionDetectionResult resultL3 = CollisionBetweenTwoPolygons(polygonLeftPectoral1, polygonTail32, false);//BV树第三层，鱼1左胸鳍部分和鱼2第三个关节碰撞检测
                    if (resultL3.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultL3.NormalAxis, fish1.VelocityDirectionRad, ref resultL3.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultL3.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultL3.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultL3.MinimumTranslationVector.X * resultL3.MinimumTranslationVector.X + resultL3.MinimumTranslationVector.Z * resultL3.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultL3.MinimumTranslationVector.X * resultL3.MinimumTranslationVector.X + resultL3.MinimumTranslationVector.Z * resultL3.MinimumTranslationVector.Z);
                            result = resultL3;
                            result.LeafNodeA = 1;
                            result.LeafNodeB = 6;
                        }
                    }

                    CollisionDetectionResult resultLLC = CollisionBetweenTwoPolygons(polygonLeftPectoral1, polygonLeftCaudalFin2, false);//BV树第三层，鱼1左胸鳍部分和鱼2左尾鳍碰撞检测
                    if (resultLLC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLLC.NormalAxis, fish1.VelocityDirectionRad, ref resultLLC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLLC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLLC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLLC.MinimumTranslationVector.X * resultLLC.MinimumTranslationVector.X + resultLLC.MinimumTranslationVector.Z * resultLLC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLLC.MinimumTranslationVector.X * resultLLC.MinimumTranslationVector.X + resultLLC.MinimumTranslationVector.Z * resultLLC.MinimumTranslationVector.Z);
                            result = resultLLC;
                            result.LeafNodeA = 1;
                            result.LeafNodeB = 7;
                        }
                    }
                    CollisionDetectionResult resultLRC = CollisionBetweenTwoPolygons(polygonLeftPectoral1, polygonRightCaudalFin2, false);//BV树第三层，鱼1左胸鳍部分和鱼2右尾鳍碰撞检测
                    if (resultLRC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLRC.NormalAxis, fish1.VelocityDirectionRad, ref resultLRC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLRC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLRC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLRC.MinimumTranslationVector.X * resultLRC.MinimumTranslationVector.X + resultLRC.MinimumTranslationVector.Z * resultLRC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLRC.MinimumTranslationVector.X * resultLRC.MinimumTranslationVector.X + resultLRC.MinimumTranslationVector.Z * resultLRC.MinimumTranslationVector.Z);
                            result = resultLRC;
                            result.LeafNodeA = 1;
                            result.LeafNodeB = 8;
                        }
                    }

                    //鱼1右胸鳍一次只可能与鱼2三个关节中的一个或尾鳍有碰撞
                    CollisionDetectionResult resultR1 = CollisionBetweenTwoPolygons(polygonRightPectoral1, polygonTail12, false);//BV树第三层，鱼1右胸鳍部分和鱼2第一个关节碰撞检测
                    if (resultR1.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultR1.NormalAxis, fish1.VelocityDirectionRad, ref resultR1.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultR1.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultR1.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultR1.MinimumTranslationVector.X * resultR1.MinimumTranslationVector.X + resultR1.MinimumTranslationVector.Z * resultR1.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultR1.MinimumTranslationVector.X * resultR1.MinimumTranslationVector.X + resultR1.MinimumTranslationVector.Z * resultR1.MinimumTranslationVector.Z);
                            result = resultR1;
                            result.LeafNodeA = 3;
                            result.LeafNodeB = 4;
                        }

                    }
                    CollisionDetectionResult resultR2 = CollisionBetweenTwoPolygons(polygonRightPectoral1, polygonTail22, false);//BV树第三层，鱼1右胸鳍部分和鱼2第二个关节碰撞检测
                    if (resultR2.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultR2.NormalAxis, fish1.VelocityDirectionRad, ref resultR2.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultR2.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultR2.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultR2.MinimumTranslationVector.X * resultR2.MinimumTranslationVector.X + resultR2.MinimumTranslationVector.Z * resultR2.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultR2.MinimumTranslationVector.X * resultR2.MinimumTranslationVector.X + resultR2.MinimumTranslationVector.Z * resultR2.MinimumTranslationVector.Z);
                            result = resultR2;
                            result.LeafNodeA = 3;
                            result.LeafNodeB = 5;
                        }

                    }

                    CollisionDetectionResult resultR3 = CollisionBetweenTwoPolygons(polygonRightPectoral1, polygonTail32, false);//BV树第三层，鱼1右胸鳍部分和鱼2第三个关节碰撞检测
                    if (resultR3.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultR3.NormalAxis, fish1.VelocityDirectionRad, ref resultR3.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultR3.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultR3.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultR3.MinimumTranslationVector.X * resultR3.MinimumTranslationVector.X + resultR3.MinimumTranslationVector.Z * resultR3.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultR3.MinimumTranslationVector.X * resultR3.MinimumTranslationVector.X + resultR3.MinimumTranslationVector.Z * resultR3.MinimumTranslationVector.Z);
                            result = resultR3;
                            result.LeafNodeA = 3;
                            result.LeafNodeB = 6;
                        }

                    }
                    CollisionDetectionResult resultRLC = CollisionBetweenTwoPolygons(polygonRightPectoral1, polygonLeftCaudalFin2, false);//BV树第三层，鱼1右胸鳍部分和鱼2左尾鳍碰撞检测
                    if (resultRLC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRLC.NormalAxis, fish1.VelocityDirectionRad, ref resultRLC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRLC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRLC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRLC.MinimumTranslationVector.X * resultRLC.MinimumTranslationVector.X + resultRLC.MinimumTranslationVector.Z * resultRLC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRLC.MinimumTranslationVector.X * resultRLC.MinimumTranslationVector.X + resultRLC.MinimumTranslationVector.Z * resultRLC.MinimumTranslationVector.Z);
                            result = resultRLC;
                            result.LeafNodeA = 3;
                            result.LeafNodeB = 7;
                        }
                    }

                    CollisionDetectionResult resultRRC = CollisionBetweenTwoPolygons(polygonRightPectoral1, polygonRightCaudalFin2, false);//BV树第三层，鱼1右胸鳍部分和鱼2右尾鳍碰撞检测
                    if (resultRRC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRRC.NormalAxis, fish1.VelocityDirectionRad, ref resultRRC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRRC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRRC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRRC.MinimumTranslationVector.X * resultRRC.MinimumTranslationVector.X + resultRRC.MinimumTranslationVector.Z * resultRRC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRRC.MinimumTranslationVector.X * resultRRC.MinimumTranslationVector.X + resultRRC.MinimumTranslationVector.Z * resultRRC.MinimumTranslationVector.Z);
                            result = resultRRC;
                            result.LeafNodeA = 3;
                            result.LeafNodeB = 8;
                        }
                    }
                    //鱼1躯干部分可能和鱼2的三个关节和尾鳍都有碰撞
                    CollisionDetectionResult resultMLC = CollisionBetweenTwoPolygons(polygonBody1, polygonLeftCaudalFin2, false);//BV树第三层，鱼1躯干部分和鱼2左尾鳍碰撞检测
                    if (resultMLC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultMLC.NormalAxis, fish1.VelocityDirectionRad, ref resultMLC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultMLC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultMLC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultMLC.MinimumTranslationVector.X * resultMLC.MinimumTranslationVector.X + resultMLC.MinimumTranslationVector.Z * resultMLC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultMLC.MinimumTranslationVector.X * resultMLC.MinimumTranslationVector.X + resultMLC.MinimumTranslationVector.Z * resultMLC.MinimumTranslationVector.Z);
                            result = resultMLC;
                            result.LeafNodeA = 2;
                            result.LeafNodeB = 7;
                        }
                    }
                    CollisionDetectionResult resultMRC = CollisionBetweenTwoPolygons(polygonBody1, polygonRightCaudalFin2, false);//BV树第三层，鱼1躯干部分和鱼2右尾鳍碰撞检测
                    if (resultMRC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultMRC.NormalAxis, fish1.VelocityDirectionRad, ref resultMRC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultMRC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultMRC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultMRC.MinimumTranslationVector.X * resultMRC.MinimumTranslationVector.X + resultMRC.MinimumTranslationVector.Z * resultMRC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultMRC.MinimumTranslationVector.X * resultMRC.MinimumTranslationVector.X + resultMRC.MinimumTranslationVector.Z * resultMRC.MinimumTranslationVector.Z);
                            result = resultMRC;
                            result.LeafNodeA = 2;
                            result.LeafNodeB = 8;
                        }
                    }
                    CollisionDetectionResult resultM3 = CollisionBetweenTwoPolygons(polygonBody1, polygonTail32, false);//BV树第三层，鱼1躯干部分和鱼2第三个关节碰撞检测
                    if (resultM3.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultM3.NormalAxis, fish1.VelocityDirectionRad, ref resultM3.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultM3.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultM3.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultM3.MinimumTranslationVector.X * resultM3.MinimumTranslationVector.X + resultM3.MinimumTranslationVector.Z * resultM3.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultM3.MinimumTranslationVector.X * resultM3.MinimumTranslationVector.X + resultM3.MinimumTranslationVector.Z * resultM3.MinimumTranslationVector.Z);
                            result = resultM3;
                            result.LeafNodeA = 2;
                            result.LeafNodeB = 6;
                        }
                    }
                    CollisionDetectionResult resultM2 = CollisionBetweenTwoPolygons(polygonBody1, polygonTail22, false);//BV树第三层，鱼1躯干部分和鱼2第二个关节碰撞检测
                    if (resultM2.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultM2.NormalAxis, fish1.VelocityDirectionRad, ref resultM2.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultM2.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultM2.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultM2.MinimumTranslationVector.X * resultM2.MinimumTranslationVector.X + resultM2.MinimumTranslationVector.Z * resultM2.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultM2.MinimumTranslationVector.X * resultM2.MinimumTranslationVector.X + resultM2.MinimumTranslationVector.Z * resultM2.MinimumTranslationVector.Z);
                            result = resultM2;
                            result.LeafNodeA = 2;
                            result.LeafNodeB = 5;
                        }
                    }
                    CollisionDetectionResult resultM1 = CollisionBetweenTwoPolygons(polygonBody1, polygonTail12, false);//BV树第三层，鱼1躯干部分和鱼2第一个关节碰撞检测
                    if (resultM1.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultM1.NormalAxis, fish1.VelocityDirectionRad, ref resultM1.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultM1.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultM1.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultM1.MinimumTranslationVector.X * resultM1.MinimumTranslationVector.X + resultM1.MinimumTranslationVector.Z * resultM1.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultM1.MinimumTranslationVector.X * resultM1.MinimumTranslationVector.X + resultM1.MinimumTranslationVector.Z * resultM1.MinimumTranslationVector.Z);
                            result = resultM1;
                            result.LeafNodeA = 2;
                            result.LeafNodeB = 4;
                        }
                    }
               
                }
                #endregion

                #region BV树第二层,鱼躯干圆模型和鱼尾圆模型检测
                if (fish1.CollisionModelTailRadiusMm + fish2.CollisionModelBodyRadiusMm >= distanceTail1ToBody2)
                {
                    //鱼2左胸鳍一次只可能与鱼1三个关节中的一个或尾鳍有碰撞
                    CollisionDetectionResult result1L = CollisionBetweenTwoPolygons(polygonTail11, polygonLeftPectoral2, false);//BV树第三层，鱼2左胸鳍部分和鱼1第一个关节碰撞检测
                    if (result1L.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result1L.NormalAxis, fish1.VelocityDirectionRad, ref result1L.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result1L.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result1L.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result1L.MinimumTranslationVector.X * result1L.MinimumTranslationVector.X + result1L.MinimumTranslationVector.Z * result1L.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result1L.MinimumTranslationVector.X * result1L.MinimumTranslationVector.X + result1L.MinimumTranslationVector.Z * result1L.MinimumTranslationVector.Z);
                            result = result1L;
                            result.LeafNodeA = 4;
                            result.LeafNodeB = 1;
                        }

                    }
                    CollisionDetectionResult result2L = CollisionBetweenTwoPolygons(polygonTail21, polygonLeftPectoral2, false);//BV树第三层，鱼2左胸鳍部分和鱼1第二个关节碰撞检测
                    if (result2L.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result2L.NormalAxis, fish1.VelocityDirectionRad, ref result2L.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result2L.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result2L.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result2L.MinimumTranslationVector.X * result2L.MinimumTranslationVector.X + result2L.MinimumTranslationVector.Z * result2L.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result2L.MinimumTranslationVector.X * result2L.MinimumTranslationVector.X + result2L.MinimumTranslationVector.Z * result2L.MinimumTranslationVector.Z);
                            result = result2L;
                            result.LeafNodeA = 5;
                            result.LeafNodeB = 1;
                        }

                    }
                    CollisionDetectionResult result3L = CollisionBetweenTwoPolygons(polygonTail31, polygonLeftPectoral2, false);//BV树第三层，鱼2左胸鳍部分和鱼1第三个关节碰撞检测
                    if (result3L.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result3L.NormalAxis, fish1.VelocityDirectionRad, ref result3L.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result3L.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result3L.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result3L.MinimumTranslationVector.X * result3L.MinimumTranslationVector.X + result3L.MinimumTranslationVector.Z * result3L.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result3L.MinimumTranslationVector.X * result3L.MinimumTranslationVector.X + result3L.MinimumTranslationVector.Z * result3L.MinimumTranslationVector.Z);
                            result = result3L;
                            result.LeafNodeA = 6;
                            result.LeafNodeB = 1;
                        }

                    }
                    CollisionDetectionResult resultLCL = CollisionBetweenTwoPolygons(polygonLeftCaudalFin1, polygonLeftPectoral2, false);//BV树第三层，鱼2左胸鳍部分和鱼1左尾鳍碰撞检测
                    if (resultLCL.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLCL.NormalAxis, fish1.VelocityDirectionRad, ref resultLCL.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLCL.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLCL.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLCL.MinimumTranslationVector.X * resultLCL.MinimumTranslationVector.X + resultLCL.MinimumTranslationVector.Z * resultLCL.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLCL.MinimumTranslationVector.X * resultLCL.MinimumTranslationVector.X + resultLCL.MinimumTranslationVector.Z * resultLCL.MinimumTranslationVector.Z);
                            result = resultLCL;
                            result.LeafNodeA = 7;
                            result.LeafNodeB = 1;
                        }
                    }
                    CollisionDetectionResult resultRCL = CollisionBetweenTwoPolygons(polygonRightCaudalFin1, polygonLeftPectoral2, false);//BV树第三层，鱼2左胸鳍部分和鱼1右尾鳍碰撞检测
                    if (resultRCL.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRCL.NormalAxis, fish1.VelocityDirectionRad, ref resultRCL.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRCL.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRCL.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRCL.MinimumTranslationVector.X * resultRCL.MinimumTranslationVector.X + resultRCL.MinimumTranslationVector.Z * resultRCL.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRCL.MinimumTranslationVector.X * resultRCL.MinimumTranslationVector.X + resultRCL.MinimumTranslationVector.Z * resultRCL.MinimumTranslationVector.Z);
                            result = resultRCL;
                            result.LeafNodeA = 8;
                            result.LeafNodeB = 1;
                        }
                    }
                    //鱼2右胸鳍一次只可能与鱼1三个关节中的一个或尾鳍有碰撞
                    CollisionDetectionResult result1R = CollisionBetweenTwoPolygons(polygonTail11, polygonRightPectoral2, false);//BV树第三层，鱼2右胸鳍部分和鱼1第一个关节碰撞检测
                    if (result1R.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result1R.NormalAxis, fish1.VelocityDirectionRad, ref result1R.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result1R.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result1R.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result1R.MinimumTranslationVector.X * result1R.MinimumTranslationVector.X + result1R.MinimumTranslationVector.Z * result1R.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result1R.MinimumTranslationVector.X * result1R.MinimumTranslationVector.X + result1R.MinimumTranslationVector.Z * result1R.MinimumTranslationVector.Z);
                            result = result1R;
                            result.LeafNodeA = 4;
                            result.LeafNodeB = 3;
                        }

                    }

                    CollisionDetectionResult result2R = CollisionBetweenTwoPolygons(polygonTail21, polygonRightPectoral2, false);//BV树第三层，鱼2右胸鳍部分和鱼1第二个关节碰撞检测
                    if (result2R.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result2R.NormalAxis, fish1.VelocityDirectionRad, ref result2R.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result2R.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result2R.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result2R.MinimumTranslationVector.X * result2R.MinimumTranslationVector.X + result2R.MinimumTranslationVector.Z * result2R.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result2R.MinimumTranslationVector.X * result2R.MinimumTranslationVector.X + result2R.MinimumTranslationVector.Z * result2R.MinimumTranslationVector.Z);
                            result = result2R;
                            result.LeafNodeA = 5;
                            result.LeafNodeB = 3;
                        }

                    }
                    CollisionDetectionResult result3R = CollisionBetweenTwoPolygons(polygonTail31, polygonRightPectoral2, false);//BV树第三层，鱼2右胸鳍部分和鱼1第三个关节碰撞检测
                    if (result3R.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result3R.NormalAxis, fish1.VelocityDirectionRad, ref result3R.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result3R.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result3R.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result3R.MinimumTranslationVector.X * result3R.MinimumTranslationVector.X + result3R.MinimumTranslationVector.Z * result3R.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result3R.MinimumTranslationVector.X * result3R.MinimumTranslationVector.X + result3R.MinimumTranslationVector.Z * result3R.MinimumTranslationVector.Z);
                            result = result3R;
                            result.LeafNodeA = 6;
                            result.LeafNodeB = 3;
                        }

                    }
                    CollisionDetectionResult resultLCR = CollisionBetweenTwoPolygons(polygonLeftCaudalFin1, polygonRightPectoral2, false);//BV树第三层，鱼2右胸鳍部分和鱼1左尾鳍碰撞检测
                    if (resultLCR.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLCR.NormalAxis, fish1.VelocityDirectionRad, ref resultLCR.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLCR.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLCR.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLCR.MinimumTranslationVector.X * resultLCR.MinimumTranslationVector.X + resultLCR.MinimumTranslationVector.Z * resultLCR.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLCR.MinimumTranslationVector.X * resultLCR.MinimumTranslationVector.X + resultLCR.MinimumTranslationVector.Z * resultLCR.MinimumTranslationVector.Z);
                            result = resultLCR;
                            result.LeafNodeA = 7;
                            result.LeafNodeB = 3;
                        }
                    }
                    CollisionDetectionResult resultRCR = CollisionBetweenTwoPolygons(polygonRightCaudalFin1, polygonRightPectoral2, false);//BV树第三层，鱼2右胸鳍部分和鱼1右尾鳍碰撞检测
                    if (resultRCR.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRCR.NormalAxis, fish1.VelocityDirectionRad, ref resultRCR.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRCR.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRCR.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRCR.MinimumTranslationVector.X * resultRCR.MinimumTranslationVector.X + resultRCR.MinimumTranslationVector.Z * resultRCR.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRCR.MinimumTranslationVector.X * resultRCR.MinimumTranslationVector.X + resultRCR.MinimumTranslationVector.Z * resultRCR.MinimumTranslationVector.Z);
                            result = resultRCR;
                            result.LeafNodeA = 8;
                            result.LeafNodeB = 3;
                        }
                    }
                    //鱼2躯干部分可能和鱼1的三个关节和尾鳍都有碰撞
                    CollisionDetectionResult resultLCM = CollisionBetweenTwoPolygons(polygonLeftCaudalFin1, polygonBody2, false);//BV树第三层，鱼2躯干部分和鱼1左尾鳍碰撞检测
                    if (resultLCM.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLCM.NormalAxis, fish1.VelocityDirectionRad, ref resultLCM.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLCM.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLCM.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLCM.MinimumTranslationVector.X * resultLCM.MinimumTranslationVector.X + resultLCM.MinimumTranslationVector.Z * resultLCM.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLCM.MinimumTranslationVector.X * resultLCM.MinimumTranslationVector.X + resultLCM.MinimumTranslationVector.Z * resultLCM.MinimumTranslationVector.Z);
                            result = resultLCM;
                            result.LeafNodeA = 7;
                            result.LeafNodeB = 2;
                        }
                    }
                    CollisionDetectionResult resultRCM = CollisionBetweenTwoPolygons(polygonRightCaudalFin1, polygonBody2, false);//BV树第三层，鱼2躯干部分和鱼1右尾鳍碰撞检测
                    if (resultRCM.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRCM.NormalAxis, fish1.VelocityDirectionRad, ref resultRCM.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRCM.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRCM.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRCM.MinimumTranslationVector.X * resultRCM.MinimumTranslationVector.X + resultRCM.MinimumTranslationVector.Z * resultRCM.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRCM.MinimumTranslationVector.X * resultRCM.MinimumTranslationVector.X + resultRCM.MinimumTranslationVector.Z * resultRCM.MinimumTranslationVector.Z);
                            result = resultRCM;
                            result.LeafNodeA = 8;
                            result.LeafNodeB = 2;
                        }
                    }
                    CollisionDetectionResult result3M = CollisionBetweenTwoPolygons(polygonTail31, polygonBody2, false);//BV树第三层，鱼2躯干部分和鱼1第三个关节碰撞检测
                    if (result3M.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result3M.NormalAxis, fish1.VelocityDirectionRad, ref result3M.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result3M.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result3M.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result3M.MinimumTranslationVector.X * result3M.MinimumTranslationVector.X + result3M.MinimumTranslationVector.Z * result3M.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result3M.MinimumTranslationVector.X * result3M.MinimumTranslationVector.X + result3M.MinimumTranslationVector.Z * result3M.MinimumTranslationVector.Z);
                            result = result3M;
                            result.LeafNodeA = 6;
                            result.LeafNodeB = 2;
                        }
                    }
                    CollisionDetectionResult result2M = CollisionBetweenTwoPolygons(polygonTail21, polygonBody2, false);//BV树第三层，鱼2躯干部分和鱼1第二个关节碰撞检测
                    if (result2M.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result2M.NormalAxis, fish1.VelocityDirectionRad, ref result2M.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result2M.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result2M.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result2M.MinimumTranslationVector.X * result2M.MinimumTranslationVector.X + result2M.MinimumTranslationVector.Z * result2M.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result2M.MinimumTranslationVector.X * result2M.MinimumTranslationVector.X + result2M.MinimumTranslationVector.Z * result2M.MinimumTranslationVector.Z);
                            result = result2M;
                            result.LeafNodeA = 5;
                            result.LeafNodeB = 2;
                        }
                    }
                    CollisionDetectionResult result1M = CollisionBetweenTwoPolygons(polygonTail11, polygonBody2, false);//BV树第三层，鱼2躯干部分和鱼1第一个关节碰撞检测
                    if (result1M.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result1M.NormalAxis, fish1.VelocityDirectionRad, ref result1M.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result1M.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result1M.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result1M.MinimumTranslationVector.X * result1M.MinimumTranslationVector.X + result1M.MinimumTranslationVector.Z * result1M.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result1M.MinimumTranslationVector.X * result1M.MinimumTranslationVector.X + result1M.MinimumTranslationVector.Z * result1M.MinimumTranslationVector.Z);
                            result = result1M;
                            result.LeafNodeA = 4;
                            result.LeafNodeB = 2;
                        }
                    }
                }
                #endregion

                #region BV树第二层,尾部和尾部之间的检测
                if (fish1.CollisionModelTailRadiusMm + fish2.CollisionModelTailRadiusMm >= distanceTailToTail)
                {

                    //鱼1的第三个关节只可能与鱼2三个关节中的一个或尾鳍碰撞
                    CollisionDetectionResult result31 = CollisionBetweenTwoPolygons(polygonTail31, polygonTail12, false);//BV树第三层，鱼1第三个关节和鱼2第一个关节碰撞检测
                    if (result31.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result31.NormalAxis, fish1.VelocityDirectionRad, ref result31.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result31.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result31.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result31.MinimumTranslationVector.X * result31.MinimumTranslationVector.X + result31.MinimumTranslationVector.Z * result31.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result31.MinimumTranslationVector.X * result31.MinimumTranslationVector.X + result31.MinimumTranslationVector.Z * result31.MinimumTranslationVector.Z);
                            result = result31;
                            result.LeafNodeA = 6;
                            result.LeafNodeB = 4;
                        }
                    }
                    CollisionDetectionResult result32 = CollisionBetweenTwoPolygons(polygonTail31, polygonTail22, false);//BV树第三层，鱼1第三个关节和鱼2第二个关节碰撞检测
                    if (result32.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result32.NormalAxis, fish1.VelocityDirectionRad, ref result32.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result32.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result32.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result32.MinimumTranslationVector.X * result32.MinimumTranslationVector.X + result32.MinimumTranslationVector.Z * result32.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result32.MinimumTranslationVector.X * result32.MinimumTranslationVector.X + result32.MinimumTranslationVector.Z * result32.MinimumTranslationVector.Z);
                            result = result32;
                            result.LeafNodeA = 6;
                            result.LeafNodeB = 5;
                        }
                    } 
                    CollisionDetectionResult result33 = CollisionBetweenTwoPolygons(polygonTail31, polygonTail32, false);//BV树第三层，鱼1第三个关节和鱼2第三个关节碰撞检测
                    if (result33.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result33.NormalAxis, fish1.VelocityDirectionRad, ref result33.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result33.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result33.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result33.MinimumTranslationVector.X * result33.MinimumTranslationVector.X + result33.MinimumTranslationVector.Z * result33.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result33.MinimumTranslationVector.X * result33.MinimumTranslationVector.X + result33.MinimumTranslationVector.Z * result33.MinimumTranslationVector.Z);
                            result = result33;
                            result.LeafNodeA = 6;
                            result.LeafNodeB = 6;
                        }
                    }
                    CollisionDetectionResult result3LC = CollisionBetweenTwoPolygons(polygonTail31, polygonLeftCaudalFin2, false);//BV树第三层，鱼1第三个关节和鱼2左尾鳍碰撞检测
                    if (result3LC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result3LC.NormalAxis, fish1.VelocityDirectionRad, ref result3LC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result3LC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result3LC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result3LC.MinimumTranslationVector.X * result3LC.MinimumTranslationVector.X + result3LC.MinimumTranslationVector.Z * result3LC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result3LC.MinimumTranslationVector.X * result3LC.MinimumTranslationVector.X + result3LC.MinimumTranslationVector.Z * result3LC.MinimumTranslationVector.Z);
                            result = result3LC;
                            result.LeafNodeA = 6;
                            result.LeafNodeB = 7;
                        }
                    }
                    CollisionDetectionResult result3RC = CollisionBetweenTwoPolygons(polygonTail31, polygonRightCaudalFin2, false);//BV树第三层，鱼1第三个关节和鱼2右尾鳍碰撞检测
                    if (result3RC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result3RC.NormalAxis, fish1.VelocityDirectionRad, ref result3RC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result3RC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result3RC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result3RC.MinimumTranslationVector.X * result3RC.MinimumTranslationVector.X + result3RC.MinimumTranslationVector.Z * result3RC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result3RC.MinimumTranslationVector.X * result3RC.MinimumTranslationVector.X + result3RC.MinimumTranslationVector.Z * result3RC.MinimumTranslationVector.Z);
                            result = result3RC;
                            result.LeafNodeA = 6;
                            result.LeafNodeB = 8;
                        }
                    }
                    //鱼1的第二个关节只可能与鱼2三个关节中的一个或尾鳍碰撞
                    CollisionDetectionResult result21 = CollisionBetweenTwoPolygons(polygonTail21, polygonTail12, false);//BV树第三层，鱼1第二个关节和鱼2第一个关节碰撞检测
                    if (result21.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result21.NormalAxis, fish1.VelocityDirectionRad, ref result21.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result21.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result21.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result21.MinimumTranslationVector.X * result21.MinimumTranslationVector.X + result21.MinimumTranslationVector.Z * result21.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result21.MinimumTranslationVector.X * result21.MinimumTranslationVector.X + result21.MinimumTranslationVector.Z * result21.MinimumTranslationVector.Z);
                            result = result21;
                            result.LeafNodeA = 5;
                            result.LeafNodeB = 4;
                        }
                    }
                    CollisionDetectionResult result22 = CollisionBetweenTwoPolygons(polygonTail21, polygonTail22, false);//BV树第三层，鱼1第二个关节和鱼2第二个关节碰撞检测
                    if (result22.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result22.NormalAxis, fish1.VelocityDirectionRad, ref result22.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result22.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result22.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result22.MinimumTranslationVector.X * result22.MinimumTranslationVector.X + result22.MinimumTranslationVector.Z * result22.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result22.MinimumTranslationVector.X * result22.MinimumTranslationVector.X + result22.MinimumTranslationVector.Z * result22.MinimumTranslationVector.Z);
                            result = result22;
                            result.LeafNodeA = 5;
                            result.LeafNodeB = 5;
                        }
                    }
                    CollisionDetectionResult result23 = CollisionBetweenTwoPolygons(polygonTail21, polygonTail32, false);//BV树第三层，鱼1第二个关节和鱼2第三个关节碰撞检测
                    if (result23.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result23.NormalAxis, fish1.VelocityDirectionRad, ref result23.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result23.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result23.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result23.MinimumTranslationVector.X * result23.MinimumTranslationVector.X + result23.MinimumTranslationVector.Z * result23.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result23.MinimumTranslationVector.X * result23.MinimumTranslationVector.X + result23.MinimumTranslationVector.Z * result23.MinimumTranslationVector.Z);
                            result = result23;
                            result.LeafNodeA = 5;
                            result.LeafNodeB = 6;
                        }
                    }
                    CollisionDetectionResult result2LC = CollisionBetweenTwoPolygons(polygonTail21, polygonLeftCaudalFin2, false);//BV树第三层，鱼1第二个关节和鱼2左尾鳍碰撞检测
                    if (result2LC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result2LC.NormalAxis, fish1.VelocityDirectionRad, ref result2LC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result2LC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result2LC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result2LC.MinimumTranslationVector.X * result2LC.MinimumTranslationVector.X + result2LC.MinimumTranslationVector.Z * result2LC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result2LC.MinimumTranslationVector.X * result2LC.MinimumTranslationVector.X + result2LC.MinimumTranslationVector.Z * result2LC.MinimumTranslationVector.Z);
                            result = result2LC;
                            result.LeafNodeA = 5;
                            result.LeafNodeB = 7;
                        }
                    }
                    CollisionDetectionResult result2RC = CollisionBetweenTwoPolygons(polygonTail21, polygonRightCaudalFin2, false);//BV树第三层，鱼1第二个关节和鱼2右尾鳍碰撞检测
                    if (result2RC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result2RC.NormalAxis, fish1.VelocityDirectionRad, ref result2RC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result2RC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result2RC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result2RC.MinimumTranslationVector.X * result2RC.MinimumTranslationVector.X + result2RC.MinimumTranslationVector.Z * result2RC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result2RC.MinimumTranslationVector.X * result2RC.MinimumTranslationVector.X + result2RC.MinimumTranslationVector.Z * result2RC.MinimumTranslationVector.Z);
                            result = result2RC;
                            result.LeafNodeA = 5;
                            result.LeafNodeB = 8;
                        }
                    }
                    //鱼1的第一个关节只可能与鱼2三个关节中的一个碰撞
                    CollisionDetectionResult result11 = CollisionBetweenTwoPolygons(polygonTail11, polygonTail12, false);//BV树第三层，鱼1第一个关节和鱼2第一个关节碰撞检测
                    if (result11.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result11.NormalAxis, fish1.VelocityDirectionRad, ref result11.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result11.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result11.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result11.MinimumTranslationVector.X * result11.MinimumTranslationVector.X + result11.MinimumTranslationVector.Z * result11.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result11.MinimumTranslationVector.X * result11.MinimumTranslationVector.X + result11.MinimumTranslationVector.Z * result11.MinimumTranslationVector.Z);
                            result = result11;
                            result.LeafNodeA = 4;
                            result.LeafNodeB = 4;
                        }
                    }
                    CollisionDetectionResult result12 = CollisionBetweenTwoPolygons(polygonTail11, polygonTail22, false);//BV树第三层，鱼1第一个关节和鱼2第二个关节碰撞检测
                    if (result12.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result12.NormalAxis, fish1.VelocityDirectionRad, ref result12.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result12.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result12.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result12.MinimumTranslationVector.X * result12.MinimumTranslationVector.X + result12.MinimumTranslationVector.Z * result12.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result12.MinimumTranslationVector.X * result12.MinimumTranslationVector.X + result12.MinimumTranslationVector.Z * result12.MinimumTranslationVector.Z);
                            result = result12;
                            result.LeafNodeA = 4;
                            result.LeafNodeB = 5;
                        }
                    }
                    CollisionDetectionResult result13 = CollisionBetweenTwoPolygons(polygonTail11, polygonTail32, false);//BV树第三层，鱼1第一个关节和鱼2第三个关节碰撞检测
                    if (result13.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result13.NormalAxis, fish1.VelocityDirectionRad, ref result13.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result13.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result13.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result13.MinimumTranslationVector.X * result13.MinimumTranslationVector.X + result13.MinimumTranslationVector.Z * result13.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result13.MinimumTranslationVector.X * result13.MinimumTranslationVector.X + result13.MinimumTranslationVector.Z * result13.MinimumTranslationVector.Z);
                            result = result13;
                            result.LeafNodeA = 4;
                            result.LeafNodeB = 6;
                        }
                    }
                    CollisionDetectionResult result1LC = CollisionBetweenTwoPolygons(polygonTail11, polygonLeftCaudalFin2, false);//BV树第三层，鱼1第一个关节和鱼2左尾鳍碰撞检测
                    if (result1LC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result1LC.NormalAxis, fish1.VelocityDirectionRad, ref result1LC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result1LC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result1LC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result1LC.MinimumTranslationVector.X * result1LC.MinimumTranslationVector.X + result1LC.MinimumTranslationVector.Z * result1LC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result1LC.MinimumTranslationVector.X * result1LC.MinimumTranslationVector.X + result1LC.MinimumTranslationVector.Z * result1LC.MinimumTranslationVector.Z);
                            result = result1LC;
                            result.LeafNodeA = 4;
                            result.LeafNodeB = 7;
                        }
                    }
                    CollisionDetectionResult result1RC = CollisionBetweenTwoPolygons(polygonTail11, polygonRightCaudalFin2, false);//BV树第三层，鱼1第一个关节和鱼2右尾鳍碰撞检测
                    if (result1RC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(result1RC.NormalAxis, fish1.VelocityDirectionRad, ref result1RC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += result1RC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= result1RC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(result1RC.MinimumTranslationVector.X * result1RC.MinimumTranslationVector.X + result1RC.MinimumTranslationVector.Z * result1RC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(result1RC.MinimumTranslationVector.X * result1RC.MinimumTranslationVector.X + result1RC.MinimumTranslationVector.Z * result1RC.MinimumTranslationVector.Z);
                            result = result1RC;
                            result.LeafNodeA = 4;
                            result.LeafNodeB = 8;
                        }
                    }
                    //鱼1的左尾鳍只可能与鱼2三个关节中的一个或尾鳍碰撞
                    CollisionDetectionResult resultLC1 = CollisionBetweenTwoPolygons(polygonLeftCaudalFin1, polygonTail12, false);//BV树第三层，鱼1左尾鳍和鱼2第一个关节碰撞检测
                    if (resultLC1.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLC1.NormalAxis, fish1.VelocityDirectionRad, ref resultLC1.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLC1.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLC1.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLC1.MinimumTranslationVector.X * resultLC1.MinimumTranslationVector.X + resultLC1.MinimumTranslationVector.Z * resultLC1.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLC1.MinimumTranslationVector.X * resultLC1.MinimumTranslationVector.X + resultLC1.MinimumTranslationVector.Z * resultLC1.MinimumTranslationVector.Z);
                            result = resultLC1;
                            result.LeafNodeA = 7;
                            result.LeafNodeB = 4;
                        }
                    }
                    CollisionDetectionResult resultLC2 = CollisionBetweenTwoPolygons(polygonLeftCaudalFin1, polygonTail22, false);//BV树第三层，鱼1左尾鳍和鱼2第二个关节碰撞检测
                    if (resultLC2.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLC2.NormalAxis, fish1.VelocityDirectionRad, ref resultLC2.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLC2.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLC2.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLC2.MinimumTranslationVector.X * resultLC2.MinimumTranslationVector.X + resultLC2.MinimumTranslationVector.Z * resultLC2.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLC2.MinimumTranslationVector.X * resultLC2.MinimumTranslationVector.X + resultLC2.MinimumTranslationVector.Z * resultLC2.MinimumTranslationVector.Z);
                            result = resultLC2;
                            result.LeafNodeA = 7;
                            result.LeafNodeB = 5;
                        }
                    }
                    CollisionDetectionResult resultLC3 = CollisionBetweenTwoPolygons(polygonLeftCaudalFin1, polygonTail32, false);//BV树第三层，鱼1左尾鳍和鱼2第三个关节碰撞检测
                    if (resultLC3.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLC3.NormalAxis, fish1.VelocityDirectionRad, ref resultLC3.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLC3.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLC3.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLC3.MinimumTranslationVector.X * resultLC3.MinimumTranslationVector.X + resultLC3.MinimumTranslationVector.Z * resultLC3.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLC3.MinimumTranslationVector.X * resultLC3.MinimumTranslationVector.X + resultLC3.MinimumTranslationVector.Z * resultLC3.MinimumTranslationVector.Z);
                            result = resultLC3;
                            result.LeafNodeA = 7;
                            result.LeafNodeB = 6;
                        }
                    }
                    CollisionDetectionResult resultLCLC = CollisionBetweenTwoPolygons(polygonLeftCaudalFin1, polygonLeftCaudalFin2, false);//BV树第三层，鱼1左尾鳍和鱼2左尾鳍碰撞检测
                    if (resultLCLC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLCLC.NormalAxis, fish1.VelocityDirectionRad, ref resultLCLC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLCLC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLCLC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLCLC.MinimumTranslationVector.X * resultLCLC.MinimumTranslationVector.X + resultLCLC.MinimumTranslationVector.Z * resultLCLC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLCLC.MinimumTranslationVector.X * resultLCLC.MinimumTranslationVector.X + resultLCLC.MinimumTranslationVector.Z * resultLCLC.MinimumTranslationVector.Z);
                            result = resultLCLC;
                            result.LeafNodeA = 7;
                            result.LeafNodeB = 7;
                        }
                    }
                    CollisionDetectionResult resultLCRC = CollisionBetweenTwoPolygons(polygonLeftCaudalFin1, polygonRightCaudalFin2, false);//BV树第三层，鱼1左尾鳍和鱼2右尾鳍碰撞检测
                    if (resultLCRC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultLCRC.NormalAxis, fish1.VelocityDirectionRad, ref resultLCRC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultLCRC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultLCRC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLCRC.MinimumTranslationVector.X * resultLCRC.MinimumTranslationVector.X + resultLCRC.MinimumTranslationVector.Z * resultLCRC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLCRC.MinimumTranslationVector.X * resultLCRC.MinimumTranslationVector.X + resultLCRC.MinimumTranslationVector.Z * resultLCRC.MinimumTranslationVector.Z);
                            result = resultLCRC;
                            result.LeafNodeA = 7;
                            result.LeafNodeB = 8;
                        }
                    }
                    //鱼1的右尾鳍只可能与鱼2三个关节中的一个或尾鳍碰撞
                    CollisionDetectionResult resultRC1 = CollisionBetweenTwoPolygons(polygonRightCaudalFin1, polygonTail12, false);//BV树第三层，鱼1右尾鳍和鱼2第一个关节碰撞检测
                    if (resultRC1.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRC1.NormalAxis, fish1.VelocityDirectionRad, ref resultRC1.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRC1.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRC1.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRC1.MinimumTranslationVector.X * resultRC1.MinimumTranslationVector.X + resultRC1.MinimumTranslationVector.Z * resultRC1.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRC1.MinimumTranslationVector.X * resultRC1.MinimumTranslationVector.X + resultRC1.MinimumTranslationVector.Z * resultRC1.MinimumTranslationVector.Z);
                            result = resultRC1;
                            result.LeafNodeA = 8;
                            result.LeafNodeB = 4;
                        }
                    }
                    CollisionDetectionResult resultRC2 = CollisionBetweenTwoPolygons(polygonRightCaudalFin1, polygonTail22, false);//BV树第三层，鱼1右尾鳍和鱼2第二个关节碰撞检测
                    if (resultRC2.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRC2.NormalAxis, fish1.VelocityDirectionRad, ref resultRC2.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRC2.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRC2.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRC2.MinimumTranslationVector.X * resultRC2.MinimumTranslationVector.X + resultRC2.MinimumTranslationVector.Z * resultRC2.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRC2.MinimumTranslationVector.X * resultRC2.MinimumTranslationVector.X + resultRC2.MinimumTranslationVector.Z * resultRC2.MinimumTranslationVector.Z);
                            result = resultRC2;
                            result.LeafNodeA = 8;
                            result.LeafNodeB = 5;
                        }
                    }
                    CollisionDetectionResult resultRC3 = CollisionBetweenTwoPolygons(polygonRightCaudalFin1, polygonTail32, false);//BV树第三层，鱼1右尾鳍和鱼2第三个关节碰撞检测
                    if (resultRC3.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRC3.NormalAxis, fish1.VelocityDirectionRad, ref resultRC3.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRC3.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRC3.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRC3.MinimumTranslationVector.X * resultRC3.MinimumTranslationVector.X + resultRC3.MinimumTranslationVector.Z * resultRC3.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRC3.MinimumTranslationVector.X * resultRC3.MinimumTranslationVector.X + resultRC3.MinimumTranslationVector.Z * resultRC3.MinimumTranslationVector.Z);
                            result = resultRC3;
                            result.LeafNodeA = 8;
                            result.LeafNodeB = 6;
                        }
                    }
                    CollisionDetectionResult resultRCLC = CollisionBetweenTwoPolygons(polygonRightCaudalFin1, polygonLeftCaudalFin2, false);//BV树第三层，鱼1右尾鳍和鱼2左尾鳍碰撞检测
                    if (resultRCLC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRCLC.NormalAxis, fish1.VelocityDirectionRad, ref resultRCLC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRCLC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRCLC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRCLC.MinimumTranslationVector.X * resultRCLC.MinimumTranslationVector.X + resultRCLC.MinimumTranslationVector.Z * resultRCLC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRCLC.MinimumTranslationVector.X * resultRCLC.MinimumTranslationVector.X + resultRCLC.MinimumTranslationVector.Z * resultRCLC.MinimumTranslationVector.Z);
                            result = resultRCLC;
                            result.LeafNodeA = 8;
                            result.LeafNodeB = 7;
                        }
                    }
                    CollisionDetectionResult resultRCRC = CollisionBetweenTwoPolygons(polygonRightCaudalFin1, polygonRightCaudalFin2, false);//BV树第三层，鱼1右尾鳍和鱼2右尾鳍碰撞检测
                    if (resultRCRC.Intersect == true)
                    {
                        minimumTranslationVector = MoveDirection(resultRCRC.NormalAxis, fish1.VelocityDirectionRad, ref resultRCRC.MinimumTranslationVector);
                        fish1.PositionMm += minimumTranslationVector / 2;
                        //fish1.PositionMm += resultRCRC.MinimumTranslationVector / 2;
                        fish1.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish1.InitPhase, ref fish1);//更新机器鱼碰撞模型
                        fish2.PositionMm -= minimumTranslationVector / 2;
                        //fish2.PositionMm -= resultRCRC.MinimumTranslationVector / 2;
                        fish2.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish2.InitPhase, ref fish2);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRCRC.MinimumTranslationVector.X * resultRCRC.MinimumTranslationVector.X + resultRCRC.MinimumTranslationVector.Z * resultRCRC.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRCRC.MinimumTranslationVector.X * resultRCRC.MinimumTranslationVector.X + resultRCRC.MinimumTranslationVector.Z * resultRCRC.MinimumTranslationVector.Z);
                            result = resultRCRC;
                            result.LeafNodeA = 8;
                            result.LeafNodeB = 8;
                        }
                    }
                }
                #endregion


            }

            if (result.Intersect == true)
            {// 为仿真机器鱼添加碰撞标志 LiYoubing 20110511
                fish1.Collision.Add(CollisionType.FISH_FISH);
                fish2.Collision.Add(CollisionType.FISH_FISH);
            }
            return result;
        }
        #endregion

        #region 仿真水球和仿真场地元素包括边界和球门块的碰撞检测
        /// <summary>
        /// 检测仿真水球和仿真场地边界及球门块碰撞情况
        /// </summary>
        /// <param name="ball">待检测的仿真水球对象引用</param>
        /// <param name="field">待检测仿真场地Field对象，用到四个球门块的顶点坐标列表和四个边界坐标值</param>
        /// <returns>碰撞检测结果参数结构体包括是否碰撞/碰撞作用面中的法向/碰撞作用点坐标</returns>
        public static CollisionDetectionResult DetectCollisionBetweenBallAndBorder(
            ref Ball ball, Field field)
        {
            CollisionDetectionResult result = new CollisionDetectionResult();
            result.Intersect = false;
            if (ball.PositionMm.X <= 0 && ball.PositionMm.Z <= 0)
            {//球在左上半场
                CollisionModelPolygon goalBlock1 = new CollisionModelPolygon(field.BorderLeftTopVertices);
                result = CollisionBetweenPolygonAndCircle(goalBlock1, ball, true);
                if (result.Intersect == true)
                {
                    ball.PositionMm -= result.MinimumTranslationVector;
                }
                if (ball.PositionMm.Z <= field.TopMm + 10 + ball.RadiusMm)
                {//球和上边界碰撞检测
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(0, 0, 1);
                    ball.PositionMm.Z = field.TopMm + 10 + ball.RadiusMm;
                    result.ActionPoint = new xna.Vector3(ball.PositionMm.X, 0, field.TopMm);
                }
                if (ball.PositionMm.X <= field.LeftMm + 10 + ball.RadiusMm)
                {//球和左边界碰撞检测
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(1, 0, 0);
                    ball.PositionMm.X = field.LeftMm + 10 + ball.RadiusMm;
                    result.ActionPoint = new xna.Vector3(field.LeftMm, 0, ball.PositionMm.Z);
                }
            }
            else if (ball.PositionMm.X >= 0 && ball.PositionMm.Z <= 0)
            {//球在右上半场
                CollisionModelPolygon goalBlock2 = new CollisionModelPolygon(field.BorderRightTopVertices);
                result = CollisionBetweenPolygonAndCircle(goalBlock2, ball, true);
                if (result.Intersect == true)
                {
                    ball.PositionMm -= result.MinimumTranslationVector;
                }
                if (ball.PositionMm.Z <= field.TopMm + 10 + ball.RadiusMm)
                {
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(0, 0, 1);
                    ball.PositionMm.Z = field.TopMm + 10 + ball.RadiusMm;
                    result.ActionPoint = new xna.Vector3(ball.PositionMm.X, 0, field.TopMm);
                }
                if (ball.PositionMm.X >= field.RightMm - 10 - ball.RadiusMm)
                {
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(-1, 0, 0);
                    ball.PositionMm.X = field.RightMm - 10 - ball.RadiusMm;
                    result.ActionPoint = new xna.Vector3(field.RightMm, 0, ball.PositionMm.Z);
                }

            }
            else if (ball.PositionMm.X <= 0 && ball.PositionMm.Z >= 0)
            {//球在左下半场
                CollisionModelPolygon goalBlock3 = new CollisionModelPolygon(field.BorderLeftBottomVertices);
                result = CollisionBetweenPolygonAndCircle(goalBlock3, ball, true);
                if (result.Intersect == true)
                {
                    ball.PositionMm -= result.MinimumTranslationVector;
                }
                if (ball.PositionMm.Z >= field.BottomMm - 10 - ball.RadiusMm)
                {
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(0, 0, -1);
                    ball.PositionMm.Z = field.BottomMm - 10 - ball.RadiusMm;
                    result.ActionPoint = new xna.Vector3(ball.PositionMm.X, 0, field.BottomMm);
                }
                if (ball.PositionMm.X <= field.LeftMm + 10 + ball.RadiusMm)
                {
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(1, 0, 0);
                    ball.PositionMm.X = field.LeftMm + 10 + ball.RadiusMm;
                    result.ActionPoint = new xna.Vector3(field.LeftMm, 0, ball.PositionMm.Z);
                }
            }
            else if (ball.PositionMm.X >= 0 && ball.PositionMm.Z >= 0)
            {//球在右下半场
                CollisionModelPolygon goalBlock4 = new CollisionModelPolygon(field.BorderRightBottomVertices);
                result = CollisionBetweenPolygonAndCircle(goalBlock4, ball, true);
                if (result.Intersect == true)
                {
                    ball.PositionMm -= result.MinimumTranslationVector;
                }
                if (ball.PositionMm.Z >= field.BottomMm - 10 - ball.RadiusMm)
                {
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(0, 0, -1);
                    ball.PositionMm.Z = field.BottomMm - 10 - ball.RadiusMm;
                    result.ActionPoint = new xna.Vector3(ball.PositionMm.X, 0, field.BottomMm);
                }
                if (ball.PositionMm.X >= field.RightMm - 10 - ball.RadiusMm)
                {
                    result.Intersect = true;
                    result.NormalAxis = new xna.Vector3(-1, 0, 0);
                    ball.PositionMm.X = field.RightMm - 10 - ball.RadiusMm;
                    result.ActionPoint = new xna.Vector3(field.RightMm, 0, ball.PositionMm.Z);
                }
            }
            //if (ball.PositionMm.Z <= field.BorderLeftTopVertices[1].Z)
            //{// 球在水池上半区域（除去球门）
            //    if (ball.PositionMm.Z < (field.TopMm + ball.RadiusMm) && (ball.PositionMm.X > (field.BorderLeftTopVertices[0].X + ball.RadiusMm) || ball.PositionMm.X < (field.BorderRightTopVertices[0].X - ball.RadiusMm)))
            //    {
            //        result.NormalAxis = new xna.Vector3(0, 0, 1);
            //        ball.PositionMm.Z = field.TopMm + ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(ball.PositionMm.X, 0, field.TopMm);
            //    }
            //    else if (ball.PositionMm.X < field.BorderLeftTopVertices[0].X + ball.RadiusMm)
            //    {
            //        result.NormalAxis = new xna.Vector3(1, 0, 0);
            //        ball.PositionMm.X = field.BorderLeftTopVertices[0].X + ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(field.BorderLeftTopVertices[0].X, 0, ball.PositionMm.Z);
            //    }
            //    else if (ball.PositionMm.X > field.BorderRightTopVertices[0].X - ball.RadiusMm)
            //    {
            //        result.NormalAxis = new xna.Vector3(-1, 0, 0);
            //        ball.PositionMm.X = field.BorderRightTopVertices[0].X - ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(field.BorderRightTopVertices[0].X, 0, ball.PositionMm.Z);
            //    }
            //    else
            //    {
            //        result.Intersect = false;
            //    }
            //}
            //else if (ball.PositionMm.Z >= field.BorderLeftBottomVertices[1].Z && (ball.PositionMm.X > (field.BorderLeftBottomVertices[0].X + ball.RadiusMm) || ball.PositionMm.X < (field.BorderRightBottomVertices[0].X - ball.RadiusMm)))
            //{// 球在水池下半区域（除去球门）
            //    if (ball.PositionMm.Z >= (field.BottomMm - ball.RadiusMm))
            //    {
            //        result.NormalAxis = new xna.Vector3(0, 0, -1);
            //        ball.PositionMm.Z = field.BottomMm - ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(ball.PositionMm.X, 0, field.BottomMm);
            //    }
            //    else if (ball.PositionMm.X <= field.BorderLeftBottomVertices[0].X + ball.RadiusMm)
            //    {
            //        result.NormalAxis = new xna.Vector3(1, 0, 0);
            //        ball.PositionMm.X = field.BorderLeftBottomVertices[0].X + ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(field.BorderLeftBottomVertices[0].X, 0, ball.PositionMm.Z);
            //    }
            //    else if (ball.PositionMm.X >= field.BorderRightBottomVertices[0].X - ball.RadiusMm)
            //    {
            //        result.NormalAxis = new xna.Vector3(-1, 0, 0);
            //        ball.PositionMm.X = field.BorderRightBottomVertices[0].X - ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(field.BorderRightBottomVertices[0].X, 0, ball.PositionMm.Z);
            //    }
            //    else
            //    {
            //        result.Intersect = false;
            //    }
            //}
            //else if (ball.PositionMm.X <= field.BorderLeftBottomVertices[1].X && (ball.PositionMm.Z > (field.BorderLeftTopVertices[1].Z + ball.RadiusMm) || ball.PositionMm.Z < (field.BorderLeftBottomVertices[1].Z - ball.RadiusMm)))
            //{//球在左边球门区域
            //    if (ball.PositionMm.X <= field.LeftMm + ball.RadiusMm)
            //    {
            //        result.NormalAxis = new xna.Vector3(1, 0, 0);
            //        ball.PositionMm.X = field.LeftMm + ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(field.LeftMm, 0, ball.PositionMm.Z);
            //    }
            //    else if (ball.PositionMm.Z <= (field.BorderLeftTopVertices[1].Z + ball.RadiusMm))
            //    {
            //        result.NormalAxis = new xna.Vector3(0, 0, 1);
            //        ball.PositionMm.Z = field.BorderLeftTopVertices[1].Z + ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(ball.PositionMm.X, 0, field.BorderLeftTopVertices[1].Z);
            //    }
            //    else if (ball.PositionMm.Z >= field.BorderLeftBottomVertices[1].Z - ball.RadiusMm)
            //    {
            //        result.NormalAxis = new xna.Vector3(0, 0, -1);
            //        ball.PositionMm.Z = field.BorderLeftBottomVertices[1].Z - ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(ball.PositionMm.X, 0, field.BorderLeftBottomVertices[1].Z);
            //    }
            //    else
            //    {
            //        result.Intersect = false;
            //    }
            //}
            //else if (ball.PositionMm.X >= field.BorderRightBottomVertices[1].X && (ball.PositionMm.Z > (field.BorderLeftTopVertices[1].Z + ball.RadiusMm) || ball.PositionMm.Z < (field.BorderLeftBottomVertices[1].Z - ball.RadiusMm)))
            //{// 球在右边球门区域
            //    if (ball.PositionMm.X >= field.RightMm - ball.RadiusMm)
            //    {
            //        result.NormalAxis = new xna.Vector3(-1, 0, 0);
            //        ball.PositionMm.X = field.RightMm - ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(field.RightMm, 0, ball.PositionMm.Z);
            //    }

            //    else if (ball.PositionMm.Z <= (field.BorderRightTopVertices[1].Z + ball.RadiusMm))
            //    {
            //        result.NormalAxis = new xna.Vector3(0, 0, 1);
            //        ball.PositionMm.Z = field.BorderRightTopVertices[1].Z + ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(ball.PositionMm.X, 0, field.BorderRightTopVertices[1].Z);
            //    }
            //    else if (ball.PositionMm.Z >= field.BorderRightBottomVertices[1].Z - ball.RadiusMm)
            //    {
            //        result.NormalAxis = new xna.Vector3(0, 0, -1);
            //        ball.PositionMm.Z = field.BorderRightBottomVertices[1].Z - ball.RadiusMm;
            //        result.ActionPoint = new xna.Vector3(ball.PositionMm.X, 0, field.BorderRightBottomVertices[1].Z);
            //    }
            //    else
            //    {
            //        result.Intersect = false;
            //    }
            //}
            //else
            //{// 球在剩余的区域内
            //    xna.Vector3[] BallToBorderVertices = new xna.Vector3[4];
            //    BallToBorderVertices[0] = field.BorderLeftTopVertices[1];
            //    BallToBorderVertices[1] = field.BorderLeftBottomVertices[1];
            //    BallToBorderVertices[2] = field.BorderRightTopVertices[1];
            //    BallToBorderVertices[3] = field.BorderRightBottomVertices[1];
            //    int seqDMax = 0;

            //     检测球到四个球门柱的距离
            //    if (UrwpgSimHelper.Min(xna.Vector3.Distance(ball.PositionMm, BallToBorderVertices[0]),
            //        xna.Vector3.Distance(ball.PositionMm, BallToBorderVertices[1]),
            //        xna.Vector3.Distance(ball.PositionMm, BallToBorderVertices[2]),
            //        xna.Vector3.Distance(ball.PositionMm, BallToBorderVertices[3]), ref seqDMax) <= (float)ball.RadiusMm)
            //    {
            //        result.NormalAxis = xna.Vector3.Subtract(ball.PositionMm, BallToBorderVertices[seqDMax]);
            //        result.NormalAxis.Normalize();
            //        result.ActionPoint = BallToBorderVertices[seqDMax];
            //    }
            //    else
            //    {
            //        result.Intersect = false;
            //    }
            //}
            return result;
        }
        #endregion

        #region 仿真机器鱼和仿真障碍物的碰撞检测 modified by renjing 20110426
        /// <summary>
        /// 检测鱼和圆形障碍物之间的碰撞情况
        /// </summary>
        /// <param name="fish">待测鱼的模型对象</param>
        /// <param name="roundedObstacle">待测圆形障碍物的模型对象</param>
        /// <returns>返回碰撞检测结果结构体</returns>
        public static CollisionDetectionResult DetectCollisionBetweenFishAndObstacle(ref RoboFish fish, RoundedObstacle roundedObstacle)
        {
            CollisionDetectionResult result = new CollisionDetectionResult();
            result.Intersect = false;
            MyMission myMission = MyMission.Instance();
            float MaxDis = 0;

            // 外层碰撞检测
            float distance = xna.Vector3.Distance(fish.CollisionModelCenterPositionMm, roundedObstacle.PositionMm);//Chen Penghui
            //圆形障碍物和根结点的碰撞检测
            if (fish.CollisionModelRadiusMm + roundedObstacle.RadiusMm >= distance)
            {
                float distanceaBody = xna.Vector3.Distance(fish.CollisionModelBodyCenterPositionMm, roundedObstacle.PositionMm);//圆形障碍物圆心到BV树第二层鱼刚体部分子结点圆模型中心点的距离
                float distanceaTail = xna.Vector3.Distance(fish.CollisionModelTailCenterPositionMm, roundedObstacle.PositionMm);//圆形障碍物圆心到BV树第二层鱼尾部子结点圆模型中心点的距离
                //圆形障碍物和刚体部分子结点的碰撞检测
                if (fish.CollisionModelBodyRadiusMm + roundedObstacle.RadiusMm >= distanceaBody)
                {
                    CollisionModelPolygon polygonFishBody = new CollisionModelPolygon(fish.BodyPolygonVertices);
                    CollisionModelPolygon polygonFishLeftPectoral = new CollisionModelPolygon(fish.LeftPectoralPolygonVertices);
                    CollisionModelPolygon polygonFishRightPectoral = new CollisionModelPolygon(fish.RightPectoralPolygonVertices);

                    CollisionDetectionResult resultBody = CollisionBetweenPolygonAndRoundedObstacle(polygonFishBody, roundedObstacle);//检测和身体躯干部分是否有碰撞
                    if (resultBody.Intersect == true)
                    {
                        fish.PositionMm += resultBody.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z);
                            result = resultBody;
                            result.LeafNodeA = 2;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultLeft = CollisionBetweenPolygonAndRoundedObstacle(polygonFishLeftPectoral, roundedObstacle);//检测和左胸鳍是否有碰撞
                    if (resultLeft.Intersect == true)
                    {
                        fish.PositionMm += resultLeft.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z);
                            result = resultLeft;
                            result.LeafNodeA = 1;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultRight = CollisionBetweenPolygonAndRoundedObstacle(polygonFishRightPectoral, roundedObstacle);//检测和右胸鳍是否有碰撞
                    if (resultRight.Intersect == true)
                    {
                        fish.PositionMm += resultRight.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z);
                            result = resultRight;
                            result.LeafNodeA = 3;
                            result.LeafNodeB = 0;
                        }
                    }

                }
                //圆形障碍物和鱼尾部分子结点的碰撞检测
                if (fish.CollisionModelTailRadiusMm + roundedObstacle.RadiusMm >= distanceaTail)
                {
                    CollisionModelPolygon polygonFishTail1 = new CollisionModelPolygon(fish.Tail1PolygonVertices);
                    CollisionModelPolygon polygonFishTail2 = new CollisionModelPolygon(fish.Tail2PolygonVertices);
                    CollisionModelPolygon polygonFishTail3 = new CollisionModelPolygon(fish.Tail3PolygonVertices);
                    CollisionModelPolygon polygonLeftCaudal = new CollisionModelPolygon(fish.LeftCaudalFinVertices);
                    CollisionModelPolygon polygonRightCaudal = new CollisionModelPolygon(fish.RightCaudalFinVertices);

                    CollisionDetectionResult resultTail1 = CollisionBetweenPolygonAndRoundedObstacle(polygonFishTail1, roundedObstacle);//检测和鱼尾第一关节是否有碰撞
                    if (resultTail1.Intersect == true)
                    {
                        fish.PositionMm += result.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z);
                            result = resultTail1;
                            result.LeafNodeA = 4;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultTail2 = CollisionBetweenPolygonAndRoundedObstacle(polygonFishTail2, roundedObstacle);//检测和鱼尾第二关节是否有碰撞
                    if (resultTail2.Intersect == true)
                    {
                        fish.PositionMm += resultTail2.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z);
                            result = resultTail2;
                            result.LeafNodeA = 5;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultTail3 = CollisionBetweenPolygonAndRoundedObstacle(polygonFishTail3, roundedObstacle);//检测和鱼尾第三关节是否有碰撞
                    if (resultTail3.Intersect == true)
                    {
                        fish.PositionMm += resultTail3.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z);
                            result = resultTail3;
                            result.LeafNodeA = 6;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultLeftCaudal = CollisionBetweenPolygonAndRoundedObstacle(polygonLeftCaudal, roundedObstacle);//检测和鱼尾第三关节是否有碰撞
                    if (resultLeftCaudal.Intersect == true)
                    {
                        fish.PositionMm += resultLeftCaudal.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z);
                            result = resultLeftCaudal;
                            result.LeafNodeA = 7;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultRightCaudal = CollisionBetweenPolygonAndRoundedObstacle(polygonRightCaudal, roundedObstacle);//检测和鱼尾第三关节是否有碰撞
                    if (resultRightCaudal.Intersect == true)
                    {
                        fish.PositionMm += resultRightCaudal.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z);
                            result = resultRightCaudal;
                            result.LeafNodeA = 8;
                            result.LeafNodeB = 0;
                        }
                    }
                }
            }

            if (result.Intersect == true)
            {// 为仿真机器鱼添加碰撞标志 LiYoubing 20110511
                fish.Collision.Add(CollisionType.FISH_OBSTACLE);
            }

            return result;
        }
        /// <summary>
        /// 检测鱼和方形障碍物之间的碰撞情况
        /// </summary>
        /// <param name="fish">待测鱼的模型对象</param>
        /// <param name="rectangularObstacle">待测方形障碍物的模型对象</param>
        /// <returns>返回碰撞检测结果结构体</returns>
        public static CollisionDetectionResult DetectCollisionBetweenFishAndObstacle(
            ref RoboFish fish, RectangularObstacle rectangularObstacle)
        {
            float tempDistance = xna.Vector3.Distance(fish.CollisionModelCenterPositionMm, rectangularObstacle.PositionMm);//Chen Penghui
            CollisionDetectionResult result = new CollisionDetectionResult();
            CollisionModelPolygon polygonObstacle = new CollisionModelPolygon(rectangularObstacle.PolygonVertices);
            MyMission myMission = MyMission.Instance();
            result.Intersect = false;
            float MaxDis = 0;

            //方形障碍物和根结点的碰撞检测
            if (fish.CollisionModelRadiusMm + rectangularObstacle.CircumcircleRadiusMm >= tempDistance)
            {
                float distanceaBody = xna.Vector3.Distance(fish.CollisionModelBodyCenterPositionMm, rectangularObstacle.PositionMm);//方形障碍物外接圆心到BV树第二层鱼刚体部分子结点圆模型中心点的距离
                float distanceaTail = xna.Vector3.Distance(fish.CollisionModelTailCenterPositionMm, rectangularObstacle.PositionMm);//方形障碍物外接圆心到BV树第二层鱼尾部子结点圆模型中心点的距离
                if (fish.CollisionModelTailRadiusMm + rectangularObstacle.CircumcircleRadiusMm >= distanceaTail)
                {
                    CollisionModelPolygon polygonFishTail1 = new CollisionModelPolygon(fish.Tail1PolygonVertices);
                    CollisionModelPolygon polygonFishTail2 = new CollisionModelPolygon(fish.Tail2PolygonVertices);
                    CollisionModelPolygon polygonFishTail3 = new CollisionModelPolygon(fish.Tail3PolygonVertices);
                    CollisionModelPolygon polygonLeftCaudal = new CollisionModelPolygon(fish.LeftCaudalFinVertices);
                    CollisionModelPolygon polygonRightCaudal = new CollisionModelPolygon(fish.RightCaudalFinVertices);
                    CollisionDetectionResult resultTail1 = CollisionBetweenTwoPolygons(polygonFishTail1, polygonObstacle, true);//检测和鱼尾第一关节是否有碰撞
                    if (resultTail1.Intersect == true)
                    {
                        fish.PositionMm += resultTail1.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultTail1.MinimumTranslationVector.X * resultTail1.MinimumTranslationVector.X + resultTail1.MinimumTranslationVector.Z * resultTail1.MinimumTranslationVector.Z);
                            result = resultTail1;
                            result.LeafNodeA = 4;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultTail2 = CollisionBetweenTwoPolygons(polygonFishTail2, polygonObstacle, true);//检测和鱼尾第二关节是否有碰撞
                    if (resultTail2.Intersect == true)
                    {
                        fish.PositionMm += resultTail2.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultTail2.MinimumTranslationVector.X * resultTail2.MinimumTranslationVector.X + resultTail2.MinimumTranslationVector.Z * resultTail2.MinimumTranslationVector.Z);
                            result = resultTail2;
                            result.LeafNodeA = 5;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultTail3 = CollisionBetweenTwoPolygons(polygonFishTail3, polygonObstacle, true);//检测和鱼尾第三关节是否有碰撞
                    if (resultTail3.Intersect == true)
                    {
                        fish.PositionMm += resultTail3.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultTail3.MinimumTranslationVector.X * resultTail3.MinimumTranslationVector.X + resultTail3.MinimumTranslationVector.Z * resultTail3.MinimumTranslationVector.Z);
                            result = resultTail3;
                            result.LeafNodeA = 6;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultLeftCaudal = CollisionBetweenTwoPolygons(polygonLeftCaudal, polygonObstacle, true);//检测和鱼左半尾鳍是否有碰撞
                    if (resultLeftCaudal.Intersect == true)
                    {
                        fish.PositionMm += resultLeftCaudal.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLeftCaudal.MinimumTranslationVector.X * resultLeftCaudal.MinimumTranslationVector.X + resultLeftCaudal.MinimumTranslationVector.Z * resultLeftCaudal.MinimumTranslationVector.Z);
                            result = resultLeftCaudal;
                            result.LeafNodeA = 7;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultRightCaudal = CollisionBetweenTwoPolygons(polygonRightCaudal, polygonObstacle, true);//检测和鱼右半尾鳍是否有碰撞
                    if (resultRightCaudal.Intersect == true)
                    {
                        fish.PositionMm += resultRightCaudal.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRightCaudal.MinimumTranslationVector.X * resultRightCaudal.MinimumTranslationVector.X + resultRightCaudal.MinimumTranslationVector.Z * resultRightCaudal.MinimumTranslationVector.Z);
                            result = resultRightCaudal;
                            result.LeafNodeA = 8;
                            result.LeafNodeB = 0;
                        }
                    }

                }
                //方形障碍物和刚体部分子结点的碰撞检测
                if (fish.CollisionModelBodyRadiusMm + rectangularObstacle.CircumcircleRadiusMm >= distanceaBody)
                {
                    CollisionModelPolygon polygonFishBody = new CollisionModelPolygon(fish.BodyPolygonVertices);
                    CollisionModelPolygon polygonFishLeftPectoral = new CollisionModelPolygon(fish.LeftPectoralPolygonVertices);
                    CollisionModelPolygon polygonFishRightPectoral = new CollisionModelPolygon(fish.RightPectoralPolygonVertices);
                    CollisionModelPolygon polygonVertices = new CollisionModelPolygon(fish.PolygonVertices);

                    CollisionDetectionResult resultBody = CollisionBetweenTwoPolygons(polygonFishBody, polygonObstacle, true);//检测和身体躯干部分是否有碰撞
                    if (resultBody.Intersect == true)
                    {
                        fish.PositionMm += resultBody.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultBody.MinimumTranslationVector.X * resultBody.MinimumTranslationVector.X + resultBody.MinimumTranslationVector.Z * resultBody.MinimumTranslationVector.Z);
                            result = resultBody;
                            result.LeafNodeA = 2;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultLeft = CollisionBetweenTwoPolygons(polygonFishLeftPectoral, polygonObstacle, true);//检测和左胸鳍是否有碰撞
                    if (resultLeft.Intersect == true)
                    {
                        fish.PositionMm += resultLeft.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultLeft.MinimumTranslationVector.X * resultLeft.MinimumTranslationVector.X + resultLeft.MinimumTranslationVector.Z * resultLeft.MinimumTranslationVector.Z);
                            result = resultLeft;
                            result.LeafNodeA = 1;
                            result.LeafNodeB = 0;
                        }
                    }
                    CollisionDetectionResult resultRight = CollisionBetweenTwoPolygons(polygonFishRightPectoral, polygonObstacle, true);//检测和右胸鳍是否有碰撞
                    if (resultRight.Intersect == true)
                    {
                        fish.PositionMm += resultRight.MinimumTranslationVector;
                        fish.CalculateFishPostion(myMission.ParasRef.IsRunning, myMission.ParasRef.IsPaused, fish.InitPhase, ref fish);//更新机器鱼碰撞模型
                        if (Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z) > MaxDis)
                        {
                            MaxDis = (float)Math.Sqrt(resultRight.MinimumTranslationVector.X * resultRight.MinimumTranslationVector.X + resultRight.MinimumTranslationVector.Z * resultRight.MinimumTranslationVector.Z);
                            result = resultRight;
                            result.LeafNodeA = 3;
                            result.LeafNodeB = 0;
                        }
                    }
                }
            }

            if (result.Intersect == true)
            {// 为仿真机器鱼添加碰撞标志 LiYoubing 20110511
                fish.Collision.Add(CollisionType.FISH_OBSTACLE);
            }

            return result;
        }
        #endregion

        #region 仿真水球和仿真障碍物的碰撞检测
        /// <summary>
        /// 检测球和圆形障碍物之间的碰撞情况
        /// </summary>
        /// <param name="ball">待测球的模型对象</param>
        /// <param name="roundedObstacle">待测圆形障碍物的模型对象</param>
        /// <returns>返回碰撞检测结果结构体</returns>
        public static CollisionDetectionResult DetectCollisionBetweenBallAndObstacle(ref Ball ball, RoundedObstacle roundedObstacle)
        {
            CollisionDetectionResult result = new CollisionDetectionResult();
            result.Intersect = false;
            float distance = xna.Vector3.Distance(ball.PositionMm, roundedObstacle.PositionMm);
            if (distance <= (ball.RadiusMm + roundedObstacle.RadiusMm))
            {
                result.Intersect = true;
                result.NormalAxis = xna.Vector3.Subtract(ball.PositionMm, roundedObstacle.PositionMm);
                if (result.NormalAxis.Length() != 0)
                {
                    result.NormalAxis.Normalize();
                }
                ball.PositionMm += xna.Vector3.Multiply(result.NormalAxis, (float)(ball.RadiusMm + roundedObstacle.RadiusMm - distance));
                result.ActionPoint = xna.Vector3.Add(roundedObstacle.PositionMm, xna.Vector3.Multiply(result.NormalAxis, (float)(ball.RadiusMm + roundedObstacle.RadiusMm - distance)));
                //add by caiqiong 2011-3-11
                ball.Collision = CollisionType.BALL_OBSTACLE;
            }
            else//add by caiqiong 2011-3-11
            {
                ball.Collision = CollisionType.NONE;
            }
            return result;
        }

        /// <summary>
        /// 检测球和方形障碍物之间的碰撞情况
        /// </summary>
        /// <param name="ball">待测球的模型对象</param>
        /// <param name="rectangularObstacle">待测方形障碍物的模型对象</param>
        /// <returns>返回碰撞检测结果结构体</returns>
        public static CollisionDetectionResult DetectCollisionBetweenBallAndObstacle(
            ref Ball ball, RectangularObstacle rectangularObstacle)
        {
            CollisionDetectionResult result = new CollisionDetectionResult();
            result.Intersect = false;
            //bool staticStatus = rectangularObstacle.StaticStatus;//方形障碍物是静态的

            CollisionModelPolygon polygonObstacle = new CollisionModelPolygon(rectangularObstacle.PolygonVertices);//Chen Penghui
            result = CollisionBetweenPolygonAndCircle(polygonObstacle, ball, true);
            if (result.Intersect == true)
            {
                ball.PositionMm -= result.MinimumTranslationVector;
                //add by caiqiong 2011-3-11
                ball.Collision = CollisionType.BALL_OBSTACLE;
            }//add by caiqiong 2011-3-11
            else
            {
                ball.Collision = CollisionType.NONE;
            }

            return result;
        }
        #endregion

        #region 两个仿真水球之间的碰撞检测
        /// <summary>
        /// 检测两球之间的碰撞情况
        /// </summary>
        /// <param name="ballA">待测第一个球的模型对象</param>
        /// <param name="ballB">待测第二个球的模型对象</param>
        /// <returns>返回碰撞检测结果结构体</returns>
        public static CollisionDetectionResult DetectCollisionBetweenTwoBalls(ref Ball ballA, ref Ball ballB)
        {
            CollisionDetectionResult result = new CollisionDetectionResult();
            result.Intersect = false;
            float distance = xna.Vector3.Distance(ballA.PositionMm, ballB.PositionMm);
            if (distance <= (ballA.RadiusMm + ballB.RadiusMm))
            {
                result.Intersect = true;
                result.NormalAxis = xna.Vector3.Subtract(ballA.PositionMm, ballB.PositionMm);
                if (result.NormalAxis.Length() != 0)
                {
                    result.NormalAxis.Normalize();
                }
                ballA.PositionMm += xna.Vector3.Multiply(result.NormalAxis, (float)(ballA.RadiusMm + ballB.RadiusMm - distance) / 2);
                ballB.PositionMm -= xna.Vector3.Multiply(result.NormalAxis, (float)(ballA.RadiusMm + ballB.RadiusMm - distance) / 2);
                result.ActionPoint = xna.Vector3.Add(ballB.PositionMm, xna.Vector3.Multiply(result.NormalAxis, (float)(ballA.RadiusMm + ballB.RadiusMm - distance) / 2));
                //add by caiqiong 2011-3-11
               // ballA.Collision=C
            }
            return result;
        }
        #endregion

        #region 坐标系转换 added by renjing 20111213
        /// <summary>
        /// 两坐标系间转换
        /// </summary>
        /// <param name="PointA">A点在原坐标系中的坐标值</param>
        /// <param name="OriginalPoint">现坐标系原点在原坐标系中的坐标值</param>
        /// <param name="Angle">现坐标系和原坐标系之间的夹角，和平台角度值取值方法一致</param>
        /// <returns>返回在现坐标系中A点的坐标值</returns>
        public static xna.Vector3 CoordinateTransformation(xna.Vector3 PointA, xna.Vector3 OriginalPoint, float Angle)
        {
            xna.Vector3 PointADot = new xna.Vector3();

            PointADot.X = (PointA.X - OriginalPoint.X) * (float)Math.Cos(Angle) + (PointA.Z - OriginalPoint.Z) * (float)Math.Sin(Angle);
            PointADot.Z = (PointA.Z - OriginalPoint.Z) * (float)Math.Cos(Angle) - (PointA.X - OriginalPoint.X) * (float)Math.Sin(Angle);

            return PointADot;
        }
        #endregion

    }

    /// <summary>
    /// 碰撞检测结果参数结构体 modified by renjing 20120305
    /// </summary>
    public struct CollisionDetectionResult
    {
        /// <summary>
        /// 定义两多边形是否相交
        /// </summary>
        public bool Intersect;

        /// <summary>
        /// 两相撞物体，重叠的矢量
        /// </summary>
        public xna.Vector3 MinimumTranslationVector;

        /// <summary>
        /// 碰撞作用面中的法线方向
        /// </summary>
        public xna.Vector3 NormalAxis;

        /// <summary>
        /// 碰撞作用点
        /// </summary>
        public xna.Vector3 ActionPoint;

        /// <summary>
        /// 碰撞时刻
        /// </summary>
        public float CollisionTime;

        /// <summary>
        /// A对象的叶节点，如果A对象不是机器鱼，则该值为0；如果是机器鱼，取值范围是[1,8]
        /// 1~8依次为，左胸鳍、躯干、右胸鳍、第一尾关节、第二尾关节、第三尾关节、左半尾鳍、右半尾鳍
        /// </summary>
        public int LeafNodeA;

        /// <summary>
        /// B对象的叶节点，如果A对象不是机器鱼，则该值为0；如果是机器鱼，取值范围是[1,8]
        /// 1~8依次为，左胸鳍、躯干、右胸鳍、第一尾关节、第二尾关节、第三尾关节、左半尾鳍、右半尾鳍
        /// </summary>
        public int LeafNodeB;
    }

    /// <summary>
    /// 碰撞检测多边形模型类
    /// </summary>
    public class CollisionModelPolygon
    {
        /// <summary>
        /// 无参数的构造函数
        /// </summary>
        public CollisionModelPolygon()
        {
        }
        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="vertices">多边形模型顶点列表，顶点为xna.Vector3类型</param>
        public CollisionModelPolygon(List<xna.Vector3> vertices)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                _points.Add(vertices[i]);
            }

            BuildEdges();
        }

        private List<xna.Vector3> _points = new List<xna.Vector3>();//多边形顶点
        private List<xna.Vector3> _edges = new List<xna.Vector3>();//多边形边

        /// <summary>
        /// 根据多边形的顶点列表建立边向量列表
        /// </summary>
        public void BuildEdges()
        {//记录多边形的边
            xna.Vector3 p1;
            xna.Vector3 p2;
            _edges.Clear();
            for (int i = 0; i < _points.Count; i++)
            {
                p1 = _points[i];
                if (i + 1 >= _points.Count)
                {
                    p2 = _points[0];
                }
                else
                {
                    p2 = _points[i + 1];
                }
                _edges.Add(p2 - p1);
            }
        }

        /// <summary>
        /// 多边形的边向量列表
        /// </summary>
        public List<xna.Vector3> Edges
        {
            get { return _edges; }
        }

        /// <summary>
        /// 多边形的顶点列表
        /// </summary>
        public List<xna.Vector3> Points
        {
            get { return _points; }
        }

        /// <summary>
        /// 多边形的质心
        /// </summary>
        public xna.Vector3 Center
        {//计算质心
            get
            {
                float totalX = 0;
                float totalY = 0;
                float totalZ = 0;
                for (int i = 0; i < _points.Count; i++)
                {
                    totalX += _points[i].X;
                    totalY += _points[i].Y;
                    totalZ += _points[i].Z;
                }
                return new xna.Vector3(totalX / _points.Count, totalY / _points.Count, totalZ / _points.Count);
            }
        }

        /// <summary>
        /// 将当前对象代表的多边形模型平移一个xna.Vector3类型的向量三个分量距离（可正可负）
        /// </summary>
        /// <param name="v"></param>
        public void Offset(xna.Vector3 v)
        {
            Offset(v.X, v.Y, v.Z);
        }

        /// <summary>
        /// 将当前对象代表的多边形模型在X/Y/Z方向分别平移x/y/z距离（可正可负）
        /// </summary>
        /// <param name="x">X方向平移的距离（可正可负）</param>
        /// <param name="y">Y方向平移的距离（可正可负）</param>
        /// <param name="z">Z方向平移的距离（可正可负）</param>
        public void Offset(float x, float y, float z)
        {
            for (int i = 0; i < _points.Count; i++)
            {
                xna.Vector3 p = _points[i];
                _points[i] = new xna.Vector3(p.X + x, p.Y + y, p.Z + z);
            }
        }
    }

    /// <summary>
    /// 二分法结果参数结构体
    /// </summary>
    public struct DichotomyResult
    {
        /// <summary>
        /// 中间点坐标
        /// </summary>
        public xna.Vector3 MidPositionMm;
        /// <summary>
        /// 中间点鱼体朝向
        /// </summary>
        public float MidBodyDirectionRad;
        /// <summary>
        /// 中间点鱼尾第一关节相对鱼体的朝向
        /// </summary>
        public float MidTailToBodyAngle1;
        /// <summary>
        /// 中间点鱼尾第二关节相对鱼体的朝向
        /// </summary>
        public float MidTailToBodyAngle2;
        /// <summary>
        /// 中间点鱼尾第三关节相对鱼体的朝向
        /// </summary>
        public float MidTailToBodyAngle3;
        /// <summary>
        /// 中间点时刻
        /// </summary>
        public float MidTime;
        /// <summary>
        /// 机器鱼中间状态
        /// </summary>
        public RoboFish MidRoboFish;
    }
    /// <summary>
    /// 四元函数结构体
    /// </summary>
    public struct FourVariables
    {
        /// <summary>
        /// 四元数的旋转角度量
        /// </summary>
        public float s;
        /// <summary>
        /// 四元数旋转轴i的分量
        /// </summary>
        public float x;
        /// <summary>
        /// 四元数旋转轴j的分量
        /// </summary>
        public float y;
        /// <summary>
        /// 四元数旋转轴k的分量
        /// </summary>
        public float z;
    }
    /// <summary>
    /// 欧拉角结构体
    /// </summary>
    public struct EulerAngle
    {
        /// <summary>
        /// 物体朝向与X轴的夹角
        /// </summary>
        public float alpha;
        /// <summary>
        /// 物体朝向与Y轴的夹角
        /// </summary>
        public float beta;
        /// <summary>
        /// 物体朝向与Z轴的夹角
        /// </summary>
        public float gama;
    }
}