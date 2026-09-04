//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: IMission.cs
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真使命（比赛或实验项目）接口定义文件
// Histroy:
// Date: 20101116  Author: LiYoubing
// Modification: 修改内容简述
// Date: 20120414  Author: ChenXiao
// Modification: 添加ProcessDynamicObstacleLocomotion
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace URWPGSim2D.Common
{
    /// <summary>
    /// 仿真使命（比赛或实验项目）接口，仿真使命基类实现此接口
    /// </summary>
    public interface IMission
    {
        #region 获取具体仿真使命的公共对象引用的接口方法
        /// <summary>
        /// 获取当前使命参与队伍列表及每支队伍的通用仿真机器鱼对象，在使命基类中实现
        /// 每支队伍的公共参数为新建的副本，不是原始队伍列表中相应对象的引用
        /// </summary>
        /// <returns>仿真使命参与队伍列表，每支队伍里有相应的通用仿真机器鱼对象引用</returns>
        List<Team<RoboFish>> GetTeams();

        /// <summary>
        /// 获取当前使命的公共仿真环境，在使命基类中实现
        /// </summary>
        /// <returns>仿真环境基类对象引用</returns>
        SimEnvironment GetEnvironment();
        
        /// <summary>
        /// 获取当前使命的决策数组，在仿真基类中实现
        /// </summary>
        /// <returns>当前使命各支队伍各个队员的决策数组，d[i, j]为第i队第j条仿真机器鱼的决策数据</returns>
        Decision[,] GetDecision();
        #endregion

        #region 更改仿真使命类型时需要调用的接口方法
        /// <summary>
        /// 获取当前使命的公共参数，在使命基类中实现
        /// </summary>
        /// <returns>当前使命的公共参数值</returns>
        MissionCommonPara GetMissionCommonPara();

        /// <summary>
        /// 设置当前使命的公共参数，在使命基类中实现
        /// </summary>
        /// <param name="para">当前使命的公共参数值</param>
        void SetMissionCommonPara(MissionCommonPara para);

        /// <summary>
        /// 设置当前使命各相关对象的初始值，在每个具体使命类中实现
        /// </summary>
        void SetMission();
        #endregion

        #region 每个仿真周期均需调用的接口方法
        /// <summary>
        /// 根据当前决策数组更新全部仿真机器鱼的决策数据，在使命基类中实现
        /// </summary>
        void SetDecisionsToFishes();

        /// <summary>
        /// 更新当前使命全部仿真机器鱼的运动学参数，在使命基类中实现
        /// 注意左右半场的处理策略：若仿真机器鱼所在队伍位于右半场，
        /// 更新质心坐标值、鱼体方向值、速度方向值、角速度值时，增量值均按正常值取反，
        /// 如此便可实现运动行为反向的效果
        /// </summary>
        void ProcessFishLocomotion();

        /// <summary>
        /// 更新当前使命全部仿真水球的运动学参数，在使命基类中实现
        /// </summary>
        void ProcessBallLocomotion();

        //added by ChenXiao 20120414
        /// <summary>
        /// 更新当前使命全部动态障碍物的运动学参数，在使命基类中实现
        /// </summary>
        void ProcessDynamicObstacleLocomotion();

        /// <summary>
        /// 处理当前周期仿真环境中各种对象间的碰撞，包括检测和响应碰撞，在使命基类中实现
        /// </summary>
        void ProcessCollision();

        /// <summary>
        /// 处理当前仿真使命的控制规则，在具体使命类中实现
        /// </summary>
        void ProcessControlRules();

        /// <summary>
        /// 绘制当前使命各相关动态图形对象，在使命基类中实现
        /// </summary>
        /// <returns>绘制好各动态图形对象的Bitmap对象</returns>
        Bitmap Draw();
        #endregion

        #region 其他接口方法
        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值，在具体仿真使命类中实现
        /// </summary>
        void ResetTeamsAndFishes();

        //longhainan 20120801
        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值，在具体仿真使命类中实现
        /// </summary>
        void ResetGoalHandler();
        //longhainan 20120801
        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命参与队伍及其仿真机器鱼的各项参数设置为默认值，在具体仿真使命类中实现
        /// </summary>
        void ResetShootout();

        // LiYoubing 20110617
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真水球恢复默认位置，在具体仿真使命类中实现
        /// </summary>
        void ResetBalls();

        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命涉及的全部仿真障碍物恢复默认位置，在具体仿真使命类中实现
        /// </summary>
        void ResetObstacles();

        // LiYoubing 20110617
        /// <summary>
        /// 设置仿真机器鱼鱼体和编号默认颜色，在具体仿真使命类中实现
        /// </summary>
        void ResetColorFishAndId();

        // LiYoubing 20110617
        /// <summary>
        /// 设置仿真水球填充和边框默认颜色，在仿真使命基类中实现
        /// </summary>
        void ResetColorBall();

        // LiYoubing 20110701
        /// <summary>
        /// 重启或改变仿真使命类型和界面请求恢复默认时将当前仿真使命使用的仿真场地尺寸恢复默认值，在具体仿真使命类中实现
        /// </summary>
        void ResetField();

        /// <summary>
        /// 重启或改变仿真使命类型时将该仿真使命相应的仿真环境各参数设置为默认值，在具体仿真使命类中实现
        /// </summary>
        void ResetEnvironment();
        #endregion
    }
}