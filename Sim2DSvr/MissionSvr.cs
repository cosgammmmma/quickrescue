//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: MissionSvr.cs
// Date: 20141210 
// Description:增加新的比赛项目
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真平台服务端Sim2DSvr Dss Service实现文件之与使命（比赛或实验项目）相关文件
//              增加新的比赛类型所涉及到的修改点主要集中于此文件
// Histroy:
// Date: 20110511  Author: LiYoubing
// Modification: 
// 1.GetLocalDecision中增加null检查
// 2.根据2011中国水中机器人大赛（成都）组委会要求修改仿真使命名称
// Date: 20110518  Author: LiYoubing
// Modification: 
// 1.修正半场交换相关实现将本文件与半场交换相关代码移除
// Date: 20110617  Author: LiYoubing
// Modification: 
// 1.GetLocalDecision中增加图像处理干扰相关代码
// Date: 20110701  Autohor: LiYoubing
// Modification:
// 1.InitMission中增加场地重绘功能使得仿真使命可以自定义场地尺寸
// Date: 20110711  Author: LiYoubing
// Modification: 
// 1.GetLocalDecision中修正半场交换问题：增加交换半场后交换策略的代码
// Date: 20110712  Author: LiYoubing
// Modification: 
// 1.GetLocalDecision中增加策略调用异常处理
// Date: 20111101  Author: ZhangBo
// Modification: 
// 1.MissionCommonPara中增加两个布尔参数，用于判断比赛是否需要绘制球门块、场地线等
// 2.更改部分比赛项目
// Date: 20120414  Author: ChenXiao
// Modification: 
// 1.NextStepProcessDetail中添加ProcessDynamicObstacleLocomotion的调用
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using W3C.Soap;
using System.Threading;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

using Microsoft.Ccr.Core;
using Microsoft.Ccr.Adapters.WinForms;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using submgr = Microsoft.Dss.Services.SubscriptionManager;

using URWPGSim2D.Common;
using URWPGSim2D.Match;
using URWPGSim2D.Core;
using URWPGSim2D.Sim2DSvr.ThreadHelper;

namespace URWPGSim2D.Sim2DSvr
{
    partial class Sim2DSvrService : DsspServiceBase
    {
        #region 仿真使命周期性循环执行通用处理过程
        ///// <summary>
        ///// 仿真使命周期性循环执行用的线程
        ///// </summary>
        //private Thread threadNextStep = null;
        //delegate void NextStepCallback();

        ///// <summary>
        ///// 比赛平台独占程序冻结控制量：比赛进行时，冻结独占程序（比赛运行部分代码）
        ///// 冻结期间外部时钟不会调用该段代码，比赛时间不变，直到独占运行完毕
        ///// </summary>
        //private bool bFreezeGameProcess = false;

        ///// <summary>
        ///// 仿真使命周期性循环执行逻辑，以当前时间为参数调用此方法进入新的仿真周期
        ///// </summary>
        ///// <param name="time"></param>
        //private void SimulationLoop(DateTime time)
        //{
        //    //MyMission.Instance().ParasRef.RemainingCycles--; // 剩余周期数递减

        //    //if ((MyMission.Instance().ParasRef.IsRunning == true) && (MyMission.Instance().ParasRef.RemainingCycles >= 0))
        //    //{
        //    //    threadNextStep = new Thread(new ThreadStart(ThreadNextStepProcSafe));
        //    //    threadNextStep.Start();
        //    //    if (MyMission.Instance().ParasRef.RemainingCycles == 0)
        //    //    {
        //    //        Activate(Arbiter.Receive(false, TimeoutPort(MyMission.Instance().ParasRef.MsPerCycle), SimulationLoop));
        //    //    }
        //    //    //Activate(Arbiter.Receive(true, TimeoutPort(MyMission.Instance().ParasRef.MsPerCycle), SimulationLoop));
        //    //}

        //    MyMission myMission = MyMission.Instance();
        //    myMission.ParasRef.RemainingCycles--; // 剩余周期数递减

        //    //if ((MyMission.Instance().ParasRef.IsRunning == true) && (MyMission.Instance().ParasRef.RemainingCycles >= 0))
        //    if ((myMission.ParasRef.IsRunning == true) 
        //        && (myMission.ParasRef.IsPaused == false)
        //        && (myMission.ParasRef.RemainingCycles >= 0))
        //    {// 剩余周期数尚未减到0且没有暂停则继续在TimeoutPort上启动Receiver
        //        Activate(Arbiter.Receive(false, TimeoutPort(MyMission.Instance().ParasRef.MsPerCycle), SimulationLoop));
        //    }

        //    threadNextStep = new Thread(new ThreadStart(ThreadNextStepProcSafe));
        //    threadNextStep.Start();
        //}

        ///// <summary>
        ///// 仿真使命循环执行线程入口处理函数
        ///// </summary>
        //private void ThreadNextStepProcSafe()
        //{
        //    NextStepProcess();
        //}

        ///// <summary>
        ///// 仿真使命循环执行线程回调处理函数
        ///// </summary>
        //private void NextStepProcess()
        //{
        //    if (_serverControlBoard.InvokeRequired)
        //    {
        //        try
        //        {
        //            NextStepCallback callback = new NextStepCallback(NextStepProcess);
        //            _serverControlBoard.Invoke(callback, new object[] { });
        //        }
        //        catch
        //        {
        //        }
        //    }
        //    else
        //    {
        //        if (!bFreezeGameProcess)
        //        {
        //            #region Pre Process NextStepProcessDetail()
        //            bFreezeGameProcess = true
        //            #endregion

        //            try
        //            {
        //                NextStepProcessDetail();
        //            }
        //            catch
        //            {
        //            }

        //            #region Post Process NextStepProcessDetail()
        //            bFreezeGameProcess = false;
        //            #endregion
        //        }
        //    }
        //}

        ///// <summary>
        ///// 仿真使命循环执行具体处理过程，仿真比赛进程控制
        ///// </summary>
        //private void NextStepProcessDetail()
        //{
        //    // 请在这里加入仿真比赛运行控制程序
        //    DateTime now = DateTime.Now;
        //    Console.WriteLine("{0:00}:{1:00}:{2:00}:{3:000}", now.Hour, now.Minute, now.Second, now.Millisecond);

        //    MyMission myMission = MyMission.Instance();

        //    if (myMission.IsRomteMode == false)
        //    {// 运行于Local模式，则需要调用本地策略获取各队伍决策数据填充到决策数组
        //        if (_serverControlBoard.IsTestMode == true)
        //        {// 当前使用测速功能
        //            myMission.DecisionRef[0, 0] = _serverControlBoard.GetTestDecision();
        //        }
        //        else
        //        {// 当前运行实际使命
        //            for (int i = 0; i < myMission.TeamsRef.Count; i++)
        //            {
        //                if (_serverControlBoard.TeamStrategyControls[i].StrategyInterface != null)
        //                {
        //                    Decision[] decisions = _serverControlBoard.TeamStrategyControls[i].StrategyInterface.GetDecision(
        //                        myMission.MissionRef, i);
        //                    for (int j = 0; j < myMission.TeamsRef[i].Fishes.Count; j++)
        //                    {
        //                        myMission.DecisionRef[i, j] = decisions[j];
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    // 根据当前决策数组更新当前使命类型中全部仿真机器鱼的决策数据
        //    myMission.IMissionRef.SetDecisionsToFishes();

        //    // 更新当前使命类型中全部仿真机器鱼的运动学参数
        //    myMission.IMissionRef.ProcessFishLocomotion();

        //    // 更新当前使命类型中全部仿真水球的运动学参数
        //    myMission.IMissionRef.ProcessBallLocomotion();

        //    // 处理当前周期仿真环境中各种对象间的碰撞，包括检测和响应碰撞
        //    myMission.IMissionRef.ProcessCollision();

        //    // 处理当前仿真使命的控制规则
        //    myMission.IMissionRef.ProcessControlRules();

        //    // 处理服务端界面动态数据更新任务
        //    ProcessUiUpdating();

        //    // 向客户端发送仿真使命参数更新通知
        //    SpawnIterator(MissionParaNotification);
        //}
        #endregion

        #region 仿真使命周期性循环执行通用处理过程新设计20101229
        /// <summary>
        /// 仿真使命周期性循环执行用的线程
        /// </summary>
        private Thread threadNextStep = null;
        delegate void NextStepCallback();

        /// <summary>
        /// 比赛平台独占程序冻结控制量：比赛进行时，冻结独占程序（比赛运行部分代码）
        /// 冻结期间外部时钟不会调用该段代码，比赛时间不变，直到独占运行完毕
        /// </summary>
        private bool bFreezeGameProcess = false;

        /// <summary>
        /// 进入可中止线程池的工作任务对象
        /// </summary>
        //WorkItem workItem = null;

        /// <summary>
        /// 仿真循环执行具体处理过程委托（回调）类型
        /// </summary>
        delegate void DelegateNextStepProcessDetail();

        //Receiver<DateTime> _receiver = null;

        //Port<DateTime> _portSimLoop = new Port<DateTime>();

        /// <summary>
        /// 仿真使命周期性循环执行逻辑，以当前时间为参数调用此方法进入新的仿真周期
        /// </summary>
        /// <param name="time"></param>
        private void SimulationLoop(DateTime time)
        {
            //if (workItem != null)
            //{
            //    Console.WriteLine(AbortableThreadPool.Cancel(workItem, true).ToString());
            //}

            MyMission myMission = MyMission.Instance();
            //myMission.ParasRef.RemainingCycles--; // 剩余周期数递减

            //if (myMission.ParasRef.RemainingCycles < -1)
            if (myMission.ParasRef.RemainingCycles < 0)
            {
                myMission.ParasRef.IsRunning = false;       // 运行标志复位
                //myMission.ParasRef.IsShowDlgNeeded = true;  // 需要弹出提示对话框的标志置位
                //myMission.ParasRef.Message = "TimeOut";
                _serverControlBoard.UnloadStrategy(); //20110307
                //InitMission(myMission.ParasRef.Name, myMission.ParasRef.TotalSeconds / 60);
            }

            if ((myMission.ParasRef.IsRunning == true)
                && (myMission.ParasRef.IsPaused == false)
                && (myMission.ParasRef.RemainingCycles >= 0))
            {// 剩余周期数尚未减到0且没有暂停则继续在TimeoutPort上启动Receiver
                Activate(Arbiter.Receive(false, TimeoutPort(MyMission.Instance().ParasRef.MsPerCycle), SimulationLoop));
            }

            //if ((myMission.ParasRef.IsRunning == false)
            //    || (myMission.ParasRef.IsPaused == true)
            //    || (myMission.ParasRef.RemainingCycles < 0))
            //{// 仿真使命停止或暂停运行或剩余周期数小于0则清空_portSimLoop上的receiver
            //    _receiver.Cleanup();
            //    _receiver = null;
            //    //_portSimLoop = new Port<DateTime>();
            //}

            //workItem = AbortableThreadPool.QueueUserWorkItem(
            //delegate(object state)
            //{
            //    //NextStepProcess();
            //    DelegateNextStepProcessDetail callback = new DelegateNextStepProcessDetail(NextStepProcessDetail);
            //    _serverControlBoard.Invoke(callback, new object[] { });
            //});

            threadNextStep = new Thread(new ThreadStart(ThreadNextStepProcSafe));
            threadNextStep.Start();
        }

        /// <summary>
        /// 仿真使命循环执行线程入口处理函数
        /// </summary>
        private void ThreadNextStepProcSafe()
        {
            NextStepProcess();
        }

        /// <summary>
        /// 仿真使命循环执行线程回调处理函数
        /// </summary>
        private void NextStepProcess()
        {
            if (_serverControlBoard.InvokeRequired)
            {
                try
                {
                    NextStepCallback callback = new NextStepCallback(NextStepProcess);
                    _serverControlBoard.Invoke(callback, new object[] { });
                }
                catch
                {
                }
            }
            else
            {
                if (!bFreezeGameProcess)
                {
                    #region Pre Process NextStepProcessDetail()
                    bFreezeGameProcess = true;
                    #endregion

                    try
                    {
                        NextStepProcessDetail();
                    }
                    catch
                    {
                    }

                    #region Post Process NextStepProcessDetail()
                    bFreezeGameProcess = false;
                    #endregion
                }
            }
        }

        Port<int> _portLocalDecision = new Port<int>();

        Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// 仿真使命循环执行具体处理过程，仿真比赛进程控制
        /// </summary>
        private void NextStepProcessDetail()
        {
            // 请在这里加入仿真比赛运行控制程序
            //DateTime now = DateTime.Now;
            //Console.WriteLine("{0:00}:{1:00}:{2:00}:{3:000}", now.Hour, now.Minute, now.Second, now.Millisecond);            
            //stopwatch.Start();
            Console.WriteLine("{0:000}", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
            stopwatch.Start();

            MyMission myMission = MyMission.Instance();

            myMission.ParasRef.RemainingCycles--; // 剩余周期数递减

            // 根据当前决策数组更新当前使命类型中全部仿真机器鱼的决策数据
            myMission.IMissionRef.SetDecisionsToFishes();


            // 更新当前使命类型中全部动态障碍物的运动学参数
            myMission.IMissionRef.ProcessDynamicObstacleLocomotion();

            // 更新当前使命类型中全部仿真机器鱼的运动学参数
            myMission.IMissionRef.ProcessFishLocomotion();

            // 更新当前使命类型中全部仿真水球的运动学参数
            myMission.IMissionRef.ProcessBallLocomotion();

            // 处理当前周期仿真环境中各种对象间的碰撞，包括检测和响应碰撞
            myMission.IMissionRef.ProcessCollision();

            // 处理当前仿真使命的控制规则
            myMission.IMissionRef.ProcessControlRules();

            if (myMission.IsRomteMode == false)
            {// 运行于Local模式，则需要调用本地策略获取各队伍决策数据填充到决策数组
                if (_serverControlBoard.IsTestMode == true)
                {// 当前使用测速功能
                    myMission.DecisionRef[0, 0] = _serverControlBoard.GetTestDecision();
                }
                else
                {// 当前运行实际使命
                    for (int i = 0; i < myMission.TeamsRef.Count; i++)
                    {
                        if (_serverControlBoard.TeamStrategyControls[i].StrategyInterface != null)
                        {
                            Activate(Arbiter.Receive(false, _portLocalDecision, GetLocalDecision));
                            _portLocalDecision.Post(i);
                            //Decision[] decisions = _serverControlBoard.TeamStrategyControls[i].StrategyInterface.GetDecision(
                            //    myMission.MissionRef, i);
                            //for (int j = 0; j < myMission.TeamsRef[i].Fishes.Count; j++)
                            //{
                            //    myMission.DecisionRef[i, j] = decisions[j];
                            //}
                        }
                    }
                }
            }
            else
            {// 运行于Remote模式
                // 向客户端发送仿真使命参数更新通知
                SpawnIterator(MissionParaNotification);
            }

            // 处理服务端界面动态数据更新任务
            ProcessUiUpdating();

            //stopwatch.Stop();
            //Console.WriteLine("{0:000}", stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// 指示是否已经发生过策略调用异常true是false否
        /// 20110820废弃 策略代码中存在条件性除零/数组越界等异常时 不必要直接结束掉策略运行
        /// </summary>
        //bool _strategyExceptionFlag = false;

        /// <summary>
        /// 进行策略调用 获取指定id对应队伍的决策数据
        /// </summary>
        /// <param name="teamId">待获取决策数据的队伍id(从0开始)</param>
        void GetLocalDecision(int teamId)
        {
            MyMission myMission = MyMission.Instance();
            Decision[] decisions = null;
            if (_serverControlBoard.TeamStrategyControls[teamId].StrategyInterface != null 
                /*&& _strategyExceptionFlag == false*/)
            {// 进行null检查确保不出异常 LiYoubing 20110511
                // 将当前仿真使命的通用Mission对象拷贝一份用于策略调用的参数传递
                Mission mission = (Mission)MyMission.Instance().MissionRef.Clone();
                for (int i = 0; i < mission.CommonPara.TeamCount; i++)
                {// 循环对每条仿真机器鱼施加图像处理干扰 LiYoubing 20110617
                    for (int j = 0; j < mission.CommonPara.FishCntPerTeam; j++)
                    {
                        RoboFish f = mission.TeamsRef[i].Fishes[j];
                        Interference.ImageProcessInterference(ref f.PositionMm, ref f.BodyDirectionRad,
                            f.VelocityMmPs, f.AngularVelocityRadPs);
                    }
                }
                for (int i = 0; i < mission.EnvRef.Balls.Count; i++)
                {// 循环对所有仿真水球施加图像处理干扰 LiYoubing 20110617
                    Ball b = mission.EnvRef.Balls[i];
                    float tmp = 0;
                    Interference.ImageProcessInterference(ref b.PositionMm, ref tmp, b.VelocityMmPs, b.AngularVelocityRadPs);
                }
                // 交换半场后交换策略处理 LiYoubing 20110711                
                // 交换半场后TeamsRef[0]/[1]代表交换前右/左半场的队伍因此应该分别调用第1/0号控件所加载的策略
                int strategyId = myMission.ParasRef.IsExchangedHalfCourt ? (teamId + 1) % 2 : teamId;
                try
                {
                    decisions = _serverControlBoard.TeamStrategyControls[strategyId].StrategyInterface.GetDecision(
                       mission, teamId);
                }
                catch
                {
                    //_strategyExceptionFlag = true;
                    MessageBox.Show("Remoting object timeout.\nThe instance of class Strategy has been released." 
                        + "\nYour simulated robofish will not be controlled.", 
                        "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                //decision = _serverControlBoard.TeamStrategyControls[teamId].StrategyInterface.GetDecision(
                //   mission, teamId);
                //decision = _serverControlBoard.TeamStrategyControls[teamId].StrategyInterface.GetDecision(
                //   myMission.MissionRef, teamId);
            }
            if (decisions == null) return;
            for (int j = 0; j < myMission.TeamsRef[teamId].Fishes.Count; j++)
            {
                myMission.DecisionRef[teamId, j] = decisions[j];
            }
        }
        #endregion

        /// <summary>
        /// 仿真使命初始化方法，根据具体的仿真使命名称进行相应初始化设置
        /// </summary>
        /// <param name="strMissionName">仿真使命（比赛或实验项目）名称</param>
        private void InitMission(string strMissionName, int missionMinutes)
        {
            MissionCommonPara para = new MissionCommonPara("1VS1", 2, 1, 60 * 10, 100, false, false);

            switch (strMissionName)
            {
                case "水球5VS5":
                    // 实例化Match3V3或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)Match5V5.Instance();
                    MyMission.Instance().IMissionRef = (IMission)Match5V5.Instance();
                    //para = new MissionCommonPara("3 Versus 3", 2, 3, 60 * missionMinutes, 100, 0, false, 0);
                    para = new MissionCommonPara("水球5VS5", 2, 5, 60 * missionMinutes, 100, true, true);
                    break;

                case "水球3VS3":
                    // 实例化Match3V3或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)Match3V3.Instance();
                    MyMission.Instance().IMissionRef = (IMission)Match3V3.Instance();
                    //para = new MissionCommonPara("3 Versus 3", 2, 3, 60 * missionMinutes, 100, 0, false, 0);
                    para = new MissionCommonPara("水球3VS3", 2, 3, 60 * missionMinutes, 100, true, true);

                    break;

                //case "抢球大作战":
                //    // 实例化Match2V2BallRace或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                //    MyMission.Instance().MissionRef = (Mission)Match2V2BallRace.Instance();
                //    MyMission.Instance().IMissionRef = (IMission)Match2V2BallRace.Instance();
                //    para = new MissionCommonPara("抢球大作战", 2, 2, 60 * missionMinutes, 100, false, false);
                //    break;

                case "抢球大作战":
                    // 实例化NewMatch2V2或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)NewMatch2V2BallRace.Instance();
                    MyMission.Instance().IMissionRef = (IMission)NewMatch2V2BallRace.Instance();
                    para = new MissionCommonPara("抢球大作战", 2, 2, 60 * missionMinutes, 100, false, 0);
                    break;

                case "抢球博弈":
                    // 实例化NewMatch2V2或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)TheNewMatch2V2BallRace.Instance();
                    MyMission.Instance().IMissionRef = (IMission)TheNewMatch2V2BallRace.Instance();
                    para = new MissionCommonPara("抢球博弈", 2, 2, 60 * missionMinutes, 100, false, 0);
                    break;

                case "生存挑战":
                    // 实例化NewMatch2V2或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)MatchTransfer.Instance();
                    MyMission.Instance().IMissionRef = (IMission)MatchTransfer.Instance();
                    para = new MissionCommonPara("生存挑战", 2, 4, 60 * missionMinutes, 100, false, 0);
                    break;

                case "障碍越野":
                    // 实例化NewMatch2V2或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)MatchObstacleRacing.Instance();
                    MyMission.Instance().IMissionRef = (IMission)MatchObstacleRacing.Instance();
                    para = new MissionCommonPara("障碍越野", 1, 1, 60 * missionMinutes, 100, false, 4);
                    break;

                case "急速救援":
                    MyMission.Instance().MissionRef = (Mission)MatchRescue.Instance();
                    MyMission.Instance().IMissionRef = (IMission)MatchRescue.Instance();
                    para = new MissionCommonPara("急速救援", 2, 2, 60 * missionMinutes, 100, false, 3);
                    break;

                case "水中搬运":
                    // 实例化NewMatch2V2或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)Match1V1Moving.Instance();
                    MyMission.Instance().IMissionRef = (IMission)Match1V1Moving.Instance();
                    para = new MissionCommonPara("水中搬运", 1, 2, 60 * missionMinutes, 100, false, 2);
                    break;
                case "水球斯诺克":
                    // 实例化MatchSnooker或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)MatchSnooker.Instance();
                    MyMission.Instance().IMissionRef = (IMission)MatchSnooker.Instance();
                    para = new MissionCommonPara("水球斯诺克", 1, 1, 60 * missionMinutes, 100, false, false);
                    break;
                
                case "协作过孔":
                    // 实例化Match3V3或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)TwoFishPushBallViaChannel.Instance();
                    MyMission.Instance().IMissionRef = (IMission)TwoFishPushBallViaChannel.Instance();
                    para = new MissionCommonPara("协作过孔", 1, 2, 60 * missionMinutes, 100, true, false);
                    break;

                case "花样游泳":
                    // 实例化Match3V3或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)MatchSynchronisedSwimming.Instance();
                    MyMission.Instance().IMissionRef = (IMission)MatchSynchronisedSwimming.Instance();
                    para = new MissionCommonPara("花样游泳", 1, 10, 60 * missionMinutes, 100, false, false);
                    break;

                case "带球接力":
                    // 实例化Match3V3或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)SpeedRelayWithBallIn12Meter.Instance();
                    MyMission.Instance().IMissionRef = (IMission)SpeedRelayWithBallIn12Meter.Instance();
                    para = new MissionCommonPara("带球接力", 1, 2, 60 * missionMinutes, 100, false, false);
                    break;

                //case "双鱼竞速":
                //    // 实例化双鱼过孔竞速或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                //    MyMission.Instance().MissionRef = (Mission)DoubleFishRaceViaHoles.Instance();
                //    MyMission.Instance().IMissionRef = (IMission)DoubleFishRaceViaHoles.Instance();
                //    para = new MissionCommonPara("双鱼竞速", 2, 1, 60 * missionMinutes, 100, false, false);
                //    break;

                case "4支队伍测试":
                    // 实例化Match3V3或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)Match4Teams.Instance();
                    MyMission.Instance().IMissionRef = (IMission)Match4Teams.Instance();
                    para = new MissionCommonPara("4支队伍测试", 4, 3, 60 * missionMinutes, 100, false, false);
                    break;

                case "带球避障":
                    // 实例化单鱼推球避障或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)SingleFishPushBallAvoidObstacles.Instance();
                    MyMission.Instance().IMissionRef = (IMission)SingleFishPushBallAvoidObstacles.Instance();
                    para = new MissionCommonPara("带球避障", 1, 1, 60 * missionMinutes, 100, false, false);
                    break;

                default:
                    // 实例化Match1V1或获取它的唯一实例并将其接口引用保存到能全局访问的MyMission.IMissionRef
                    MyMission.Instance().MissionRef = (Mission)Match1V1.Instance();
                    MyMission.Instance().IMissionRef = (IMission)Match1V1.Instance();
                    //para = new MissionCommonPara("1 Versus 1", 2, 1, 60 * missionMinutes, 100, 0, false, 0);
                    para = new MissionCommonPara("水球1VS1", 2, 1, 60 * missionMinutes, 100, true, true);
                    break;
            }
            InitMission(para);
            // 绘制当前选中的仿真使命对应的场地
            Bitmap bmp = URWPGSim2D.Gadget.WaveEffect.Instance().GetBitmap();
            Bitmap field = Field.Instance().Draw(bmp, MyMission.Instance().ParasRef.IsGoalBlockNeeded, MyMission.Instance().ParasRef.IsFieldInnerLinesNeeded);
            // 绘制当前选中的仿真使命对应的场景（仿真机器鱼/水球/障碍物等）
            Bitmap match = MyMission.Instance().IMissionRef.Draw();
            WinFormsServicePort.FormInvoke(delegate() { _serverControlBoard.SetFieldBmp(field);
                _serverControlBoard.DrawMatch(match); });
        }

        /// <summary>
        /// 仿真使命初始化方法，该方法在MyMission.Instance().IMissionRef初始化之后调用
        /// </summary>
        /// <param name="para">当前仿真使命公共参数值</param>
        private void InitMission(MissionCommonPara para)
        {
            MyMission myMission = MyMission.Instance();
            myMission.IsRomteMode = false;

            // 设置当前仿真使命公共参数值
            myMission.IMissionRef.SetMissionCommonPara(para);

            // 获取当前仿真使命相关各对象的引用
            myMission.ParasRef = myMission.IMissionRef.GetMissionCommonPara();
            myMission.TeamsRef = myMission.IMissionRef.GetTeams();
            myMission.EnvRef = myMission.IMissionRef.GetEnvironment();
            myMission.DecisionRef = myMission.IMissionRef.GetDecision();

            // 设置当前仿真使命特有参数
            myMission.IMissionRef.SetMission();

            InitUi();
            myMission.IsRomteMode = true;

            //_strategyExceptionFlag = false;
        }

        /// <summary>
        /// 改变比赛类型时重新初始化与队伍数量和每队仿真机器鱼数量相关的动态界面元素
        /// </summary>
        public void InitUi()
        {
            WinFormsServicePort.FormInvoke(delegate() {
                // 初始化主界面Referee面板Strategy区动态控件
                _serverControlBoard.InitTeamStrategyControls();

                // 初始化主界面Fish面板Fish Setting区动态控件
                _serverControlBoard.InitFishAndBallSettingControls();

                // 重置ServerControlBoard中随使命运行动态变化的私有变量值和控件状态
                _serverControlBoard.ResetPrivateVarsAndControls();
            });
        }

        /// <summary>
        /// 注册给TimeoutPort Receiver的响应方法
        /// 跨线程调用服务端主界面下的Replay方法，实现使命运行过程中的局部画面回放
        /// </summary>
        /// <param name="now">没有用的参数，但TimeoutPort Receiver需要它的存在</param>
        private void Replay(DateTime now)
        {
            WinFormsServicePort.FormInvoke(delegate() { _serverControlBoard.Replay(); });
        }

        /// <summary>
        /// 处理服务端界面动态数据更新任务
        /// </summary>
        private void ProcessUiUpdating()
        {
            // 处理当前仿真使命的场地中各对象绘制任务
            Bitmap bmp = MyMission.Instance().IMissionRef.Draw();
            _serverControlBoard.DrawMatch(bmp);

            // 处理当前运行着的使命中动态对象（仿真机器鱼和仿真水球）参数集中显示任务
            _serverControlBoard.ProcessFishInfoDisplaying();

            // 处理Fish面板FishSetting区当前选中的仿真机器鱼和仿真水球信息更新任务
            _serverControlBoard.ProcessFishAndBallInfoUpdating();

            //// 处理对话框提示任务
            //if (MyMission.Instance().ParasRef.IsShowDlgNeeded)
            //{
            //    System.Windows.Forms.MessageBox.Show(MyMission.Instance().ParasRef.Message, "URWPGSim2D Server");
            //}

            // 处理模拟点击“暂停”按钮任务
            // 对抗性比赛进球或交换半场时可能需要执行该任务
            if (MyMission.Instance().ParasRef.IsPauseNeeded)
            {   // 模拟点击“暂停”按钮并将标志值复位
                _serverControlBoard.ClickPauseBtn();
                MyMission.Instance().ParasRef.IsPauseNeeded = false;
            }

            ////处理交换半场时执行该任务
            //if (MyMission.Instance().ParasRef.IsExchangedHalfCourt)
            //{
            //    bmp = MyMission.Instance().IMissionRef.Draw();
            //     _serverControlBoard.DrawMatch(bmp); //重绘界面

            //    MyMission.Instance().ParasRef.IsExchangedHalfCourt = false;
            //}

            // 处理当前运行着的仿真使命中动态对象（仿真机器鱼和仿真水球）轨迹绘制任务
            _serverControlBoard.ProcessTrajectoryDrawing();
        }
    }
}