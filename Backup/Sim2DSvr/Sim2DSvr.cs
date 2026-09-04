//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Sim2DSvr.cs
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真平台服务端Sim2DSvr Dss Service实现文件之主文件
// Histroy:
// Date: 20110617  Author: LiYoubing
// Modification: 
// 1.MissionParaNotification中增加图像处理干扰相关代码
// ……
//-----------------------------------------------------------------------

using System;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using W3C.Soap;

using Microsoft.Ccr.Core;
using Microsoft.Ccr.Adapters.WinForms;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Dss.Hosting;
using submgr = Microsoft.Dss.Services.SubscriptionManager;

using URWPGSim2D.Core;
using URWPGSim2D.Common;
using client = URWPGSim2D.Sim2DSvr.ClientBase;

namespace URWPGSim2D.Sim2DSvr
{
    [Contract(Contract.Identifier)]
    [DisplayName("Sim2DSvr")]
    [Description("Sim2DSvr service (水中机器人水球比赛仿真平台服务端服务)")]
    partial class Sim2DSvrService : DsspServiceBase
    {
        /// <summary>
        /// Service state
        /// </summary>
        [ServiceState]
        Sim2DSvrState _state = new Sim2DSvrState();
        
        /// <summary>
        /// Main service port
        /// </summary>
        [ServicePort("/Sim2DSvr", AllowMultipleInstances = false)]
        Sim2DSvrOperations _mainPort = new Sim2DSvrOperations();

        [SubscriptionManagerPartner]
        submgr.SubscriptionManagerPort _submgrPort = new submgr.SubscriptionManagerPort();

        #region 实现逻辑所需的自定义成员
        /// <summary>
        /// ServerControlBoard类型的私有成员让Server服务可以访问客户端界面
        /// </summary>
        ServerControlBoard _serverControlBoard;

        /// <summary>
        /// Server服务实例建立除mainPort以外的的其他端口 added 20110110
        /// </summary>
        [AlternateServicePort(AllowMultipleInstances = true,
          AlternateContract = ClientBase.Contract.Identifier)]
        ClientBase.ClientBaseOperations _clientBasePort = new ClientBase.ClientBaseOperations();

        /// <summary>
        /// 此Port从用户界面接收事件消息
        /// </summary>
        FromServerUiEvents _fromServerUiPort = FromServerUiEvents.Instance(); //= new FromServerUiEvents();

        /// <summary>
        /// 已连接上的客户端Client服务实例Uri
        /// </summary>
        List<string> _listClientUris = new List<string>();

        /// <summary>
        /// 已经准备好的客户端的队伍Id  Liushu 20110104
        /// </summary>
        List<int> _listClientReady = new List<int>();

        ///// <summary>
        ///// 心跳线程 20101209
        ///// </summary>
        //Thread threadHeartBeat = null;

        //EnvironmentSetting env = new EnvironmentSetting();

        //ConfigXmlOperator config = new ConfigXmlOperator();

        #endregion

        /// <summary>
        /// Service constructor
        /// </summary>
        public Sim2DSvrService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        /// <summary>
        /// Service start
        /// </summary>
        protected override void Start()
        {
            base.Start();

            // combine with our main coordination
            MainPortInterleave.CombineWith(
                Arbiter.Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive<FromServerUiMsg>(true, _fromServerUiPort, OnServerUiMessageHandler),
                        Arbiter.Receive<ClientBase.ClientAnnounceDecision>(true, _clientBasePort, ClientAnnounceDecisionHandler) //added 20110110
                        //Arbiter.Receive<FromServerUiControlButtonMsg>(true, _fromServerUiPort, OnServerUiControlButtonMsgHandler)
                    ),
                    new ConcurrentReceiverGroup(
                    )
                )
            );

            WinFormsServicePort.Post(new RunForm(CreateForm)); // 显示服务端界面
        }

        /// <summary>
        /// 初始化ServerControlBoard实例并返回供RunForm使用
        /// </summary>
        /// <returns></returns>
        System.Windows.Forms.Form CreateForm()
        {
            //_serverControlBoard = new ServerControlBoard(env, config, _fromServerUiPort);
            //_serverControlBoard = new ServerControlBoard(env, config);
            _serverControlBoard = new ServerControlBoard();
            return _serverControlBoard;
        }

        /// <summary>
        /// Handle Get messages
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public IEnumerator<ITask> GetHandler(Get get)
        {
            get.ResponsePort.Post(_state);
            yield break;
        }

        /// <summary>
        /// Handler HttpGet messages
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public IEnumerator<ITask> HttpGetHandler(HttpGet get)
        {
            get.ResponsePort.Post(new HttpResponseType(HttpStatusCode.OK, _state));
            yield break;
        }

        /// <summary>
        /// Handle Drop messages
        /// </summary>
        /// <param name="drop"></param>
        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        public void DropHandler(DsspDefaultDrop drop)
        {
            DssEnvironment.ControlPanelPort.Shutdown();
            DssEnvironment.Shutdown();
        }

        /// <summary>
        /// Handles Subscribe messages
        /// </summary>
        /// <param name="subscribe">the subscribe request</param>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public IEnumerator<ITask> SubscribeHandler(Subscribe subscribe)
        {
            SubscribeRequestType request = subscribe.Body;
            //LogInfo("Subscribe request from: " + request.Subscriber);
            Console.WriteLine("Subscribe request from: " + request.Subscriber);

            // Use the Subscription Manager to handle the subscribers
            yield return Arbiter.Choice(
                SubscribeHelper(_submgrPort, request, subscribe.ResponsePort),
                delegate(SuccessResult success)
                {
                    // Send a notification on successful subscription so that the
                    // subscriber can initialize its own state
                    //base.SendNotificationToTarget<Replace>(request.Subscriber, _submgrPort);
                },
                delegate(Exception e)
                {
                    LogError(null, "Subscribe failed", e);
                }
            );

            yield break;
        }

        /// <summary>
        /// 处理Client发送给Server的ClientAnnounceUri消息
        /// </summary>
        /// <param name="announce">Client服务实例发送过来的ClientAnnounceUri消息</param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public IEnumerator<ITask> ClientAnnounceUriHandler(ClientAnnounceUri announce)
        {
            // 服务端晚于客户端启动时，默认使命类型尚未初始化完毕则暂时不响应AnnounceUri消息
            // 或服务端运行于Local模式时，策略从本地加载不响应AnnounceUri消息
            if ((MyMission.Instance().TeamsRef == null) || (MyMission.Instance().IsRomteMode == false))
            {
                announce.ResponsePort.Post(new Fault());
                yield break;
            }

            /*
            if (_listClientUris.Count < MyMission.Instance().ParasRef.TeamCount)
            {// 当前已经连上的队伍数量未达到当前使命允许的队伍数量上限
                if (_listClientUris.Contains(announce.Body.Service) == false)
                {// 保存的已连上客户端Uri中不包含当前连上的客户端Uri则认为是新的客户端
                    // 保存Client服务实例的Uri
                    _listClientUris.Add(announce.Body.Service);

                    // 设置客户端个数
                    WinFormsServicePort.FormInvoke(delegate()
                    {
                        _serverControlBoard.ConnetedClientsCount = _listClientUris.Count;
                    });

                    // 将当前使命类型队伍总数和当前客户端从0开始的编号及当前使命类型名称还有附加端口Uri响应回去
                    announce.ResponsePort.Post(new ClientAnnounceUriResponse(
                        MyMission.Instance().TeamsRef.Count, _listClientUris.Count - 1, MyMission.Instance().ParasRef.Name, AlternateContractServiceInfo[0].Service));

                    //20101209
                    if (_listClientUris.Count == 1)
                    {// 如果是第一支连上的队伍则启动心跳线程循环检测当前已连上的队伍是否掉线
                        //启动心跳线程           
                        SpawnIterator(SendHeartBeatToClient);
                    }
                }
                else
                {
                    announce.ResponsePort.Post(new Fault());
                }
            }
            else
            {// 当前已经连上的队伍数量已经达到当前使命允许的队伍数量上限
                // 给当前正在连接的队伍返回-1作为teamId表示当前使命队伍已满
                announce.ResponsePort.Post(new ClientAnnounceUriResponse(
                        MyMission.Instance().TeamsRef.Count, -1, MyMission.Instance().ParasRef.Name, AlternateContractServiceInfo[0].Service));
            }
            */

            //modified by liushu 20110218
            if (_listClientUris.Contains(announce.Body.Service) == false)
            {// 保存的已连上客户端Uri中不包含当前连上的客户端Uri则认为是新的客户端
                // 保存Client服务实例的Uri
                _listClientUris.Add(announce.Body.Service);

                if (_listClientUris.Count <= MyMission.Instance().ParasRef.TeamCount)
                {// 当前已经连上的队伍数量未达到当前使命允许的队伍数量上限
                    // 设置客户端个数
                    WinFormsServicePort.FormInvoke(delegate()
                    {
                        _serverControlBoard.ConnetedClientsCount = _listClientUris.Count;
                    });

                    // 将当前使命类型队伍总数和当前客户端从0开始的编号及当前使命类型名称还有附加端口Uri响应回去
                    announce.ResponsePort.Post(new ClientAnnounceUriResponse(
                        MyMission.Instance().TeamsRef.Count, _listClientUris.Count - 1, MyMission.Instance().ParasRef.Name, AlternateContractServiceInfo[0].Service));

                    //20101209
                    if (_listClientUris.Count == 1)
                    {// 如果是第一支连上的队伍则启动心跳线程循环检测当前已连上的队伍是否掉线
                        //启动心跳线程           
                        SpawnIterator(SendHeartBeatToClient);
                    }
                }
                else
                {// 当前已经连上的队伍数量已经达到当前使命允许的队伍数量上限
                    // 给当前正在连接的队伍返回-1作为teamId表示当前使命队伍已满
                    announce.ResponsePort.Post(new ClientAnnounceUriResponse(
                            MyMission.Instance().TeamsRef.Count, -1, MyMission.Instance().ParasRef.Name, AlternateContractServiceInfo[0].Service));
                }
            }
            else
            {
                announce.ResponsePort.Post(new Fault());
            }

            yield break;
        }

        //added by LiuShu 20101108
        /// <summary>
        /// Server接收Client发的参赛队伍名字的信息，并处理
        /// </summary>
        ///<remarks>将参赛队伍名字显示到用户界面上</remarks>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public IEnumerator<ITask> ClientAnnounceTeamNameHandler(ClientAnnounceTeamName announce)
        {
            if (MyMission.Instance().IsRomteMode == false)
            {// 服务端运行于Local模式时，策略从本地加载，不响应AnnounceTeamName消息
                announce.ResponsePort.Post(new Fault());
                yield break;
            }

            // 执行过ClientAnnounceUri保存了Client服务实例的Uri后才允许AnnounceTeamName
            if (_listClientUris.Contains(announce.Body.Service) == true)
            {// 半场交换队伍名称显示错误修正 LiYoubing 20120520
                int teamId = announce.Body.TeamId;
                if (MyMission.Instance().ParasRef.IsExchangedHalfCourt == true
                    && MyMission.Instance().ParasRef.TeamCount == 2)
                {
                    teamId = (announce.Body.TeamId + 1) % 2;
                }
                MyMission.Instance().TeamsRef[teamId].Para.Name = announce.Body.TeamName;
            }

            // 重绘服务端界面
            Bitmap bmp = MyMission.Instance().IMissionRef.Draw();
            WinFormsServicePort.FormInvoke(delegate() { _serverControlBoard.DrawMatch(bmp); });

            yield break;
        }

        //added by LiuShu 20101117
        /// <summary>
        /// Server接收Client发的Ready信息，并处理
        /// </summary>
        ///<remarks></remarks>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public IEnumerator<ITask> ClientAnnounceReadyHandler(ClientAnnounceReady announce)
        {
            if (MyMission.Instance().IsRomteMode == false)
            {// 服务端运行于Local模式时，策略从本地加载，不响应AnnounceReady消息
                announce.ResponsePort.Post(new Fault());
                yield break;
            }

            _listClientReady.Add(announce.Body.TeamId);    //保存已经准备好的队伍的Id  Added by Liushu 20110104


            WinFormsServicePort.FormInvoke(delegate() { 
                _serverControlBoard.SetTeamReadyState(announce.Body.TeamId); });

            yield break;
        }

        //added by LiuShu 20101117
        //modified 20110110
        /// <summary>
        /// Server接收Client发的Decision信息，并处理
        /// </summary>
        void ClientAnnounceDecisionHandler(ClientBase.ClientAnnounceDecision announce)
        {
            // 交换半场后交换策略处理 LiYoubing 20110711                
            // 交换半场后TeamsRef[0]/[1]代表交换前右/左半场的队伍因此应该分别接收1/0号客户端发来的策略
            int strategyId = MyMission.Instance().ParasRef.IsExchangedHalfCourt 
                ? (announce.Body.TeamId + 1) % 2 : announce.Body.TeamId;
            for (int i = 0; i < MyMission.Instance().ParasRef.FishCntPerTeam; i++)
            {
                MyMission.Instance().DecisionRef[strategyId, i] = announce.Body.Decisions[i];
                //MyMission.Instance().DecisionRef[announce.Body.TeamId, i] = announce.Body.Decisions[i];
            }
        }

        /// <summary>
        /// Server服务实例向Client服务实例Notify当前周期的Mission对象
        /// </summary>
        /// <returns></returns>
        public IEnumerator<ITask> MissionParaNotification()
        {
            // Notify all subscribers.
            MissionPara announce = new MissionPara();

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
            announce.Body = new MissionParaRequest(mission);
            //announce.Body = new MissionParaRequest(MyMission.Instance().MissionRef);//modified 20101203
            base.SendNotification(_submgrPort, announce);

            announce.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }

        void OnServerUiMessageHandler(FromServerUiMsg msg)
        {
            switch (msg.Msg)
            {
                // 比赛类型改变事件
                case FromServerUiMsg.MsgEnum.COMPETITION_ITEM_CHANGED:
                    // 以比赛类型名称为参数初始化具体使命
                    InitMission(msg.Parameters[0], Convert.ToInt32(msg.Parameters[1]));

                    // 新建比赛类型改变事件消息将比赛类型名称作为消息体
                    CompetitionTypeChanged announce = new CompetitionTypeChanged();
                    announce.Body = new CompetitionTypeChangedRequest(msg.Parameters[0], MyMission.Instance().TeamsRef.Count);

                    // 给Server主端口发送比赛类型改变消息以启动CompetitionTypeChangedHandler向所有订阅者发送通知
                    _mainPort.Post(announce);

                    //// 以比赛类型名称为参数初始化具体使命
                    //InitMission(msg.Parameters[0], Convert.ToInt32(msg.Parameters[1]));
                    break;

                // 比赛开始事件
                case FromServerUiMsg.MsgEnum.START:
                    MyMission.Instance().ParasRef.IsRunning = true;
                    //TaskQueue.EnqueueTimer(TimeSpan.FromMilliseconds(MyMission.Instance().ParasRef.MsPerCycle), _portSimLoop);
                    //_receiver = Arbiter.Receive(true, TimeoutPort(MyMission.Instance().ParasRef.MsPerCycle), SimulationLoop);
                    //_receiver = Arbiter.Receive(true, _portSimLoop, SimulationLoop);
                    //Activate(_receiver);
                    SimulationLoop(DateTime.Now);
                    break;

                // 比赛暂停事件
                case FromServerUiMsg.MsgEnum.PAUSE:
                    //MyMission.Instance().ParasRef.IsRunning = !MyMission.Instance().ParasRef.IsRunning;
                    MyMission.Instance().ParasRef.IsPaused = !MyMission.Instance().ParasRef.IsPaused;
                    //if (MyMission.Instance().ParasRef.IsRunning == true)
                    if (MyMission.Instance().ParasRef.IsPaused == false)
                    {
                        //TaskQueue.EnqueueTimer(TimeSpan.FromMilliseconds(MyMission.Instance().ParasRef.MsPerCycle), _portSimLoop);
                        //_receiver = Arbiter.Receive(true, TimeoutPort(MyMission.Instance().ParasRef.MsPerCycle), SimulationLoop);
                        //_receiver = Arbiter.Receive(true, _portSimLoop, SimulationLoop);
                        //Activate(_receiver);
                        SimulationLoop(DateTime.Now);   // 按下“Continue”继续启动仿真循环
                    }
                    else
                    {
                        //_receiver.Cleanup();
                        //_receiver = null;
                        //Activate(Arbiter.Receive(false, TimeoutPort(MyMission.Instance().ParasRef.MsPerCycle), SimulationLoop));
                    }
                    break;

                // 比赛回放事件
                case FromServerUiMsg.MsgEnum.REPLAY:
                    Activate(Arbiter.Receive(false, TimeoutPort(MyMission.Instance().ParasRef.MsPerCycle), Replay));
                    break;

                case FromServerUiMsg.MsgEnum.CLOSED:
                    _mainPort.Post(new DsspDefaultDrop());
                    break;

                case FromServerUiMsg.MsgEnum.RESTART:
                    InitMission(MyMission.Instance().ParasRef.Name, MyMission.Instance().ParasRef.TotalSeconds / 60);

                    break;
            }

            // 开始/暂停/重启事件通知给客户端
            if (msg.Msg == FromServerUiMsg.MsgEnum.START 
                || msg.Msg == FromServerUiMsg.MsgEnum.RESTART
                || msg.Msg == FromServerUiMsg.MsgEnum.PAUSE)
            {
                CompetitionControlButton announceCompetitionControlButton = new CompetitionControlButton();
                announceCompetitionControlButton.Body = new CompetitionControlButtonRequest(msg.Parameters[0]);
                _mainPort.Post(announceCompetitionControlButton);
            }
        }// end OnServerUiMessageHandler...

        //added by LiuShu 20101115
        /// <summary>
        /// Server服务实例收到服务端界面改变比赛类型消息后的处理过程
        /// </summary>
        ///<remarks>将刚改变过的比赛类型通知给所有连接上的客户端</remarks>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public IEnumerator<ITask> CompetitionTypeChangedHandler(CompetitionTypeChanged announce)
        {
            // Notify all subscribers.
            base.SendNotification(_submgrPort, announce);

            announce.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// Server服务实例对传入_mianPort中的ControlButton的处理过程
        /// </summary>
        ///<remarks>当Server服务实例_mainPort收到ControlButton的信息时，将其发送到所有连接上的客户端</remarks>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public IEnumerator<ITask> CompetitionControlButtonHandler(CompetitionControlButton announce)
        {
            // Notify all subscribers.
            base.SendNotification(_submgrPort, announce);

            announce.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }

        ////20101209
        //void ThreadHeartBeatProc()
        //{
        //    SpawnIterator(SendHeartBeatToClient);
        //}

        //20101209
        public IEnumerator<ITask> SendHeartBeatToClient()
        {
            client.HeartBeat heartBeat = new client.HeartBeat();
            heartBeat.Body.HeartBeat = "heart broken...";

            //对连接上的客户端进行心跳检测，但是心跳检测的最大队伍数即比赛类型所允许的参赛队伍数  modified 20110218
            for (int i = 0; i < Math.Min(_listClientUris.Count, MyMission.Instance().ParasRef.TeamCount); i++)      
            {
                ServiceForwarder<client.ClientBaseOperations>(_listClientUris[i]).Post(heartBeat);

                yield return Arbiter.Choice(
                heartBeat.ResponsePort,
                delegate(client.HeartBeatResponse rsp)
                {
                    Console.WriteLine("Successful");
                },
                delegate(Fault f)
                {
                    Console.WriteLine("Team{0} failed", i + 1);

                    WinFormsServicePort.FormInvoke(delegate()
                    {
                        _serverControlBoard.ShowDialogInfo(string.Format("Team{0} Connection Failed", i + 1));
                    });

                    MyMission myMission = MyMission.Instance();
                    InitMission(myMission.ParasRef.Name, myMission.ParasRef.TotalSeconds / 60);

                    //// 发送比赛类型改变事件消息，以当前选中比赛类型名称和比赛时间分钟数作为参数
                    //FromServerUiEvents.Instance().Post(new FromServerUiMsg(FromServerUiMsg.MsgEnum.COMPETITION_ITEM_CHANGED,
                    //    new string[] { myMission.ParasRef.Name, (myMission.ParasRef.TotalSeconds / 60).ToString() }));

                    //CompetitionControlButton announceCompetitionControlButton = new CompetitionControlButton();
                    //announceCompetitionControlButton.Body = new CompetitionControlButtonRequest("ID Changed");
                    //_mainPort.Post(announceCompetitionControlButton);   //将第i只队伍断线的信息发送给其他客户端

                    RemoveUnresponsiveClient(ref i);
                });
            }

            if (_listClientUris.Count != 0)
            {
                Activate(Arbiter.Receive(false, TimeoutPort(20 * (MyMission.Instance().ParasRef.MsPerCycle)), SendHeartBeat));
            }

            yield break;
        }

        /// <summary>
        /// 将断线的客户端移除，并令其后面客户端的编号依次提前。
        /// </summary>
        public void RemoveUnresponsiveClient(ref int i)
        {
            int j;

            ////Added by Liushu 20110104 
            //if (i != 0)
            //{
            //    for (j = 0; j < i; j++)
            //    {
            //        if (_listClientReady.Contains(j))
            //        {
            //            WinFormsServicePort.FormInvoke(delegate()
            //            {
            //                _serverControlBoard.SetTeamReadyState(j);
            //            });
            //        }
            //    }
            //}

            if (i != 0)
            {
                int m = i;
                WinFormsServicePort.FormInvoke(delegate()
                {
                    for (j = 0; j < m; j++)
                    {
                        if (_listClientReady.Contains(j))
                        {
                            _serverControlBoard.SetTeamReadyState(j);
                        }
                    }   
                });
            }

            for (j = i; j < _listClientUris.Count - 1; j++)
            {
                _listClientUris[j] = _listClientUris[j + 1];
                client.TeamIdChanged teamId = new client.TeamIdChanged();
                teamId.Body.TeamId = j;

                //modified 20110218
                if (j <= MyMission.Instance().ParasRef.TeamCount - 1)
                {
                    ServiceForwarder<client.ClientBaseOperations>(_listClientUris[j]).Post(teamId);
                }
                else
                {
                    teamId.Body.TeamId = -1;
                    ServiceForwarder<client.ClientBaseOperations>(_listClientUris[j]).Post(teamId);
                }
                //ServiceForwarder<client.ClientBaseOperations>(_listClientUris[j]).Post(teamId);
            }
            _listClientUris.RemoveAt(j);
            i = i - 1;

            // 设置客户端个数
            WinFormsServicePort.FormInvoke(delegate()
            {
                _serverControlBoard.ConnetedClientsCount = _listClientUris.Count;
            });
        }

        public void SendHeartBeat(DateTime dateTime)
        {
            SpawnIterator(SendHeartBeatToClient);
        }  
    }
}