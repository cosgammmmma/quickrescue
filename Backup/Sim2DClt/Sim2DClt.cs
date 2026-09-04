//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Sim2Clt.cs
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真平台客户端Sim2DClt Dss Service实现文件
// Histroy:
// Date: 20110511  Author: LiYoubing
// Modification: 
// 1.AnnounceDecisionToServer中增加null检查
// Date: 20110712  Author: LiYoubing
// Modification: 
// 1.AnnounceDecisionToServer中增加策略调用异常处理
// 2.Remote模式交换半场后还应该交换teamId在AnnounceDecisionToServer中处理
// ……
//-----------------------------------------------------------------------

using System;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using W3C.Soap;
using System.Windows.Forms;

using Microsoft.Ccr.Core;
using Microsoft.Ccr.Adapters.WinForms;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Dss.Hosting;

using URWPGSim2D.Common;
using server = URWPGSim2D.Sim2DSvr.Proxy;
using client = URWPGSim2D.Sim2DSvr.ClientBase.Proxy;

namespace URWPGSim2D.Sim2DClt
{
    [Contract(Contract.Identifier)]
    [DisplayName("Sim2DClt")]
    [Description("Sim2DClt service (水中机器人水球比赛仿真平台客户端服务)")]
    class Sim2DCltService : DsspServiceBase
    {
        /// <summary>
        /// Service state
        /// </summary>
        [ServiceState]
        Sim2DCltState _state = new Sim2DCltState();

        /// <summary>
        /// Main service port
        /// </summary>
        [ServicePort("/Sim2DClt", AllowMultipleInstances = true)]
        Sim2DCltOperations _mainPort = new Sim2DCltOperations();

        /// <summary>
        /// Server服务实例的Uri字符串，在Start方法中读取Manifest文件初始化
        /// </summary>
        string _strServerUri = "";

        /// <summary>
        /// 记录本队是A队还是B队，即左半场还是右半场
        /// </summary>
        //string _halfCourt = "";

        /// <summary>
        /// 服务端选中的当前使命类型的队伍总数，尚未连上服务端时默认取2
        /// </summary>
        int _teamCount = 2;

        /// <summary>
        /// 当前客户端实例连上服务端后，在服务端运行着的使命中所有队伍中从1开始的编号
        /// </summary>
        int _teamId = -1;

        /// <summary>
        /// 当客户端实例用户界面载入比赛策略时获得的参赛队伍名字 20101215
        /// </summary>
        string _teamName;

        /// <summary>
        /// 服务端实例建立的附加端口Uri  20110110
        /// </summary>
        string _alternateServerPortUri;

        /// <summary>
        /// 判断本队是否已经准备好了并将发送信息发到Server服务实例  20101215
        /// </summary>
        bool _isReady = false;   

        /// <summary>
        /// ClientControlBoard类型的私有成员让Client服务可以访问客户端界面
        /// </summary>
        ClientControlBoard _clientControlBoard;

        /// <summary>
        /// 此Port从用户界面接收事件消息
        /// </summary>
        FromClientUiEvents _fromClientUiPort = FromClientUiEvents.Instance(); //new FromClientUiEvents();

        /// <summary>
        /// Client服务向Server服务订阅“比赛项目变化”和“服务端界面按钮状态”消息的端口
        /// </summary>
        server.Sim2DSvrOperations _serverNotificationPort;

        /// <summary>
        /// Server服务向Client服务通知“比赛项目变化”和“服务端界面按钮状态”消息的端口
        /// </summary>
        server.Sim2DSvrOperations _serverNotificationNotify = new server.Sim2DSvrOperations();

        /// <summary>
        /// Server服务实例向Client服务实例发送消息“心跳消息”和“队伍编号变化”的端口 20101209
        /// </summary>
        [AlternateServicePort(AllowMultipleInstances = true,
          AlternateContract = client.Contract.Identifier)]
        client.ClientBaseOperations _clientBasePort = new client.ClientBaseOperations();

        /// <summary>
        /// Service constructor
        /// </summary>
        public Sim2DCltService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        /// <summary>
        /// Service start
        /// </summary>
        protected override void Start()
        {
            base.Start();

            //_serverNotificationPort = ServiceForwarder<server.Sim2DSvrOperations>(_strServerUri);
            
            // combine with our main coordination
            MainPortInterleave.CombineWith(
                Arbiter.Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive<FromClientUiMsg>(true, _fromClientUiPort, OnClientUiMessageHandler)
                    ),
                    new ConcurrentReceiverGroup(
                        Arbiter.Receive<server.CompetitionTypeChanged>(true, _serverNotificationNotify, NotifyCompetitionTypeHandler),
                        Arbiter.Receive<server.CompetitionControlButton>(true, _serverNotificationNotify, CompetitionControlButtonNotificationHandler),
                        Arbiter.Receive<server.MissionPara>(true, _serverNotificationNotify, MissionParaNotificationHandler),
                        Arbiter.Receive<client.HeartBeat>(true, _clientBasePort, HeartBeatAnnounceHandler),
                        Arbiter.Receive<client.TeamIdChanged>(true, _clientBasePort, TeamIdChangedAnnounceHandler)
                    )
                )
            );

            WinFormsServicePort.Post(new RunForm(CreateForm)); // 显示客户端界面

            // 从Manifest文件获取Server服务实例的Uri
            PartnerType partnerTypeServer = FindPartner("Server");
            if (partnerTypeServer != null)
            {
                _strServerUri = partnerTypeServer.Service;
                _serverNotificationPort = ServiceForwarder<server.Sim2DSvrOperations>(_strServerUri);
            }

            //server.Subscribe subscribe;
            //_serverNotificationPort.Subscribe(_serverNotificationNotify, out subscribe);
        }

        /// <summary>
        /// 初始化ClientControlBoard实例并返回供RunForm使用
        /// </summary>
        /// <returns></returns>
        System.Windows.Forms.Form CreateForm()
        {
            _clientControlBoard = new ClientControlBoard(_fromClientUiPort);
            return _clientControlBoard;
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

        void OnClientUiMessageHandler(FromClientUiMsg msg)
        {
            switch (msg.Msg)
            {
                case FromClientUiMsg.MsgEnum.UI_LOADED:
                    // 客户端界面加载完毕后将Client服务实例Uri发送给Server服务实例
                    SpawnIterator(AnnounceUriToServer);
                    break;

                case FromClientUiMsg.MsgEnum.STRATEGY_LOADED:
                    _teamName = msg.Parameters[0];  //20101215
                    // 策略加载完毕后将队伍名称发送给Server服务实例
                    SpawnIterator<string>(msg.Parameters[0], AnnounceTeamNameToServer);
                    break;

                case FromClientUiMsg.MsgEnum.READY:
                    // 参赛队伍准备完毕后发送Ready消息给Server服务实例
                    SpawnIterator(AnnounceReadyToServer);
                    break;

                case FromClientUiMsg.MsgEnum.CLOSED:
                    _mainPort.Post(new DsspDefaultDrop());
                    break;
            }
        }// end OnWinformMessageHandler...

        /// <summary>
        /// Client服务实例初始化完毕将ServiceUri通知给Server服务实例
        /// </summary>
        /// <returns></returns>
        IEnumerator<ITask> AnnounceUriToServer()
        {
            // 使用Uri建立到Server服务实例主端口的Forwarder引用
            server.Sim2DSvrOperations serverPort = ServiceForwarder<server.Sim2DSvrOperations>(_strServerUri);
            if (serverPort == null)
            {
                yield break;
            }

            // ClientAnnounce类必须先作为UnderwaterRobotOperations对应的PortSet的一个类型参数
            server.ClientAnnounceUri announce = new server.ClientAnnounceUri();
            //announce.Body = new server.ClientAnnounceUriRequest(ServiceInfo.Service);
            announce.Body = new server.ClientAnnounceUriRequest(AlternateContractServiceInfo[0].Service);//20101209
            serverPort.Post(announce);

            server.ClientAnnounceUriResponse announceRsp = null;
            yield return Arbiter.Choice(announce.ResponsePort,
                delegate(server.ClientAnnounceUriResponse rsp)
                {
                    announceRsp = rsp;

                    //与Server建立Subscribe关系
                    server.Subscribe subscribe;
                    _serverNotificationPort.Subscribe(_serverNotificationNotify, out subscribe);
                },
                delegate(Fault f)
                {
                    //LogError("Announce failed, continue announcing", f);
                    Console.WriteLine("Announce failed, continue announcing uri");
                    SpawnIterator(AnnounceUriToServer);
                }
            );

            if (announceRsp == null)
            {
                yield break;
            }

            _teamCount = announceRsp.TeamCount;
            _teamId = announceRsp.TeamId;
            _alternateServerPortUri = announceRsp.AlternateServerPortUri;

            if (_teamId < 0)
            {
                WinFormsServicePort.FormInvoke(delegate()
                {
                    _clientControlBoard.SetCompetitionType(announceRsp.MissionName);
                    _clientControlBoard.DisplayCompetitionState(string.Format("Connection rejected. Only {0} teams are allowed.", _teamCount));
                });
                
                //SpawnIterator(AnnounceUriToServer);
                yield break;
            }

            if (announceRsp.TeamCount == 2)
            {// 当前使命为对抗性比赛时才设置左右半场
                if (announceRsp.TeamId == 0)
                {
                    WinFormsServicePort.FormInvoke(delegate() { _clientControlBoard.SetRadioTeamState(true, false); });
                }
                else if (announceRsp.TeamId == 1)
                {
                    WinFormsServicePort.FormInvoke(delegate() { _clientControlBoard.SetRadioTeamState(false, true); });
                }
            }
            WinFormsServicePort.FormInvoke(delegate() {
                _clientControlBoard.SetTeamId(announceRsp.TeamId);
                //_clientControlBoard.SetCompetitionType(announceRsp.MissionName);
                //_clientControlBoard.SetBtnStrategyBrowseEnabledState(true);
                _clientControlBoard.OnCompetitionTypeChanged(announceRsp.MissionName);
            });
        }// end AnnounceToServer

        /// <summary>
        /// 客户端策略加载完毕将队伍名称通知给Server服务实例
        /// </summary>
        /// <returns></returns>
        IEnumerator<ITask> AnnounceTeamNameToServer(string strTeamName)
        {
            // 使用Uri建立到Server服务实例主端口的Forwarder引用
            server.Sim2DSvrOperations serverPort = ServiceForwarder<server.Sim2DSvrOperations>(_strServerUri);
            if (serverPort == null)
            {
                yield break;
            }

            // ClientAnnounceTeamName是Server服务主端口接受的一种消息类型
            server.ClientAnnounceTeamName announce = new server.ClientAnnounceTeamName();

            // 将队伍半场和队伍名字组成“队伍半场-队伍名字”的字符串，作为要发送到Server服务实例的信息 
            // 便于Server收到消息后判断该队伍名字是A队发来的还是B队发来的
            //announce.Body = new server.ClientAnnounceTeamNameRequest(ServiceInfo.Service, _halfCourt + "-" + strTeamName);
            announce.Body = new server.ClientAnnounceTeamNameRequest(AlternateContractServiceInfo[0].Service, _teamId, strTeamName);
            serverPort.Post(announce);

            // 不关心消息发出后的结果，但最好等待一下消息发送后响应端口的返回信息
            DefaultUpdateResponseType announceRsp = null;
            yield return Arbiter.Choice(announce.ResponsePort,
                delegate(DefaultUpdateResponseType rsp)
                {
                    announceRsp = rsp;
                },
                delegate(Fault f)
                {
                    //LogError("Announce failed, continue announcing", f);
                    Console.WriteLine("Announce failed, continue announcing team name");
                    //added  20110117
                    WinFormsServicePort.FormInvoke(delegate()
                    {
                        _clientControlBoard.DisplayCompetitionState(string.Format("Connection failed"));
                    });
                    SpawnIterator<string>(_teamName, AnnounceTeamNameToServer);
                }
            );

            if (announceRsp == null)
            {
                yield break;
            }
        }// end AnnounceToServer

        /// <summary>
        /// 当Client用户界面的Ready按钮按下之后，通知Server服务实例该队已经准备完毕。
        /// </summary>
        /// <returns></returns>
        IEnumerator<ITask> AnnounceReadyToServer()
        {
            // 每次重新Ready的时候清除策略调用异常标志 LiYoubing 20110712
            //_strategyExceptionFlag = false;

            // 使用Uri建立到Server服务实例主端口的Forwarder引用
            server.Sim2DSvrOperations serverPort = ServiceForwarder<server.Sim2DSvrOperations>(_strServerUri);
            if (serverPort == null)
            {
                yield break;
            }

            _isReady = true;

            // ClientAnnounceReady是Server服务主端口接受的一种消息类型
            server.ClientAnnounceReady announce = new server.ClientAnnounceReady();
            announce.Body = new server.ClientAnnounceReadyRequest(_teamId);
            serverPort.Post(announce);

            // 不关心消息发出后的结果，但最好等待一下消息发送后响应端口的返回信息
            DefaultUpdateResponseType announceRsp = null;
            yield return Arbiter.Choice(announce.ResponsePort,
                delegate(DefaultUpdateResponseType rsp)
                {
                    announceRsp = rsp;
                },
                delegate(Fault f)
                {
                    //LogError("Announce failed, continue announcing", f);
                    Console.WriteLine("Announce failed, continue announcing ready");
                    SpawnIterator(AnnounceReadyToServer);
                }
            );

            if (announceRsp == null)
            {
                yield break;
            }
        }// end AnnounceReadyToServer

        /// <summary>
        /// Client接收到Service的改变比赛项目的notification后，将比赛类型显示到用户界面
        /// </summary>
        /// <param name="announce"></param>
        void NotifyCompetitionTypeHandler(server.CompetitionTypeChanged announce)
        {
            WinFormsServicePort.FormInvoke(delegate() {
                _clientControlBoard.OnCompetitionTypeChanged(announce.Body.CompetitionType);
                _clientControlBoard.DisplayCompetitionState(string.Format("Competition type changed"));
            });

            _teamCount = announce.Body.TeamCount;

            //如果当前队伍的Id号大于当前比赛类型所允许连接的参赛队伍的最大数，或者当前队伍Id号为负时，则禁止此客户端与服务器端的连接。 added 20110113
            if (((_teamId + 1) > _teamCount) || (_teamId < 0))
            {
                WinFormsServicePort.FormInvoke(delegate()
                {
                    _clientControlBoard.SetBtnReadyEnabledState(false);
                    _clientControlBoard.SetBtnStrategyBrowseEnabledState(false);
                    _clientControlBoard.DisplayCompetitionState(string.Format("Connection rejected. Only {0} teams are allowed.", _teamCount));             
                });
            }

            _isReady = false;
        }

        /// <summary>
        /// Client接收到Server的CompetitionControlButton消息之后，判断消息的内容（包括Start，Restart，End），分别进行处理。
        /// </summary>
        /// <param name="announce"></param>
        void CompetitionControlButtonNotificationHandler(server.CompetitionControlButton announce)
        {
            switch (announce.Body.ControlButtonMessage)
            {
                case "START": // 从Server服务实例得到的是Start的通知
                    WinFormsServicePort.FormInvoke(delegate() { 
                        _clientControlBoard.SetBtnReadyEnabledState(false);
                        _clientControlBoard.SetBtnStrategyBrowseEnabledState(false);
                    });
                    Console.WriteLine("Competition Started");
                    break;

                case "STOP": // 从Server服务实例得到的是End的通知,重绘界面
                    WinFormsServicePort.FormInvoke(delegate() { 
                        _clientControlBoard.SetBtnReadyEnabledState(true);
                        _clientControlBoard.SetBtnStrategyBrowseEnabledState(true);
                    });
                    Console.WriteLine("Competition Stopped");
                    break;

                case "RESTART": // 从Server服务实例得到的是Restart的通知  added 20110117                   
                    WinFormsServicePort.FormInvoke(delegate() { 
                        _clientControlBoard.SetBtnStrategyBrowseEnabledState(true);
                        // 清除界面上的队伍名称和策略文件名 LiYoubing 20120520
                        _clientControlBoard.ClearTeamNameAndStrategyFileName();
                    });
                    Console.WriteLine("Competition Restarted");
                    break;

                default: // 默认作为Pause通知，该通知携带的消息可能是"Pause"或"Continue"字符串（服务端界面btnPause.Text）
                    if (announce.Body.ControlButtonMessage == "Pause")
                    {
                        WinFormsServicePort.FormInvoke(delegate() { _clientControlBoard.SetBtnStrategyBrowseEnabledState(true); }); //added 20110113
                        Console.WriteLine("Competition Paused");
                    }
                    else
                    {
                        WinFormsServicePort.FormInvoke(delegate() { _clientControlBoard.SetBtnStrategyBrowseEnabledState(false); }); //added 20110113
                        Console.WriteLine("Competition Continued");
                    }
                    break;
            }
            WinFormsServicePort.FormInvoke(delegate() { 
                _clientControlBoard.DisplayCompetitionState(announce.Body.ControlButtonMessage); });
        }

       
        //void MissionParaNotificationHandler(server.MissionPara announce)
        //{
        //    // ClientAnnounceDecision是Server服务主端口接受的一种消息类型
        //    client.ClientAnnounceDecision announcedec = new client.ClientAnnounceDecision();
        //    Decision[] decisions = _clientControlBoard.StragegyInterface.GetDecision(announce.Body.CurMission, _teamId);
        //    announcedec.Body = new client.ClientAnnounceDecisionRequest(decisions, _teamId);

        //    // 使用Uri建立到Server服务实例主端口的Forwarder引用
        //    client.ClientBaseOperations alternateServerPort = ServiceForwarder<client.ClientBaseOperations>(_alternateServerPortUri);
        //    if (alternateServerPort != null)
        //    {
        //        alternateServerPort.Post(announcedec);
        //    }
        //}

        /// <summary>
        /// Client服务实例接收到Server服务实例Notify的Mission对象后 
        /// 调用策略中的GetDecision方法计算决策结果 
        /// 然后将决策结果数组Announce给Server服务实例
        /// </summary>
        /// <param name="announce">Server服务实例Notify的包含Mission对象的消息</param>
        void MissionParaNotificationHandler(server.MissionPara announce)
        {
            //20101229
            SpawnIterator<server.MissionPara>(announce, AnnounceDecisionToServer);
        }

        // LiYoubing 20110712 加入策略调用异常处理
        /// <summary>
        /// 指示是否已经发生过策略调用异常true是false否
        /// 20110820废弃 策略代码中存在条件性除零/数组越界等异常时 不必要直接结束掉策略运行
        /// </summary>
        //bool _strategyExceptionFlag = false;

        //20101229
        /// <summary>
        /// 向Server服务实例发送决策计算结果
        /// 决策算法的调用过程在此
        /// </summary>
        /// <param name="announce">包含仿真使命Mission对象的消息</param>
        /// <returns></returns>
        IEnumerator<ITask> AnnounceDecisionToServer(server.MissionPara announce)
        {
            if (_clientControlBoard.StragegyInterface == null /*|| _strategyExceptionFlag == true*/)
            {// 加上null检查确保程序不会异常 LiYoubing 20110511
                yield break;
            }
            // ClientAnnounceDecision是Server服务主端口接受的一种消息类型
            client.ClientAnnounceDecision announcedec = new client.ClientAnnounceDecision();
            Decision[] decisions = null;

            try
            {
                // 交换半场后交换队伍id处理 LiYoubing 20110712
                // 交换半场后TeamsRef[0]/[1]代表交换前右/左半场的队伍因此teamId应该分别变为1/0
                //int strategyId = announce.Body.CurMission.CommonPara.IsExchangedHalfCourt ? (_teamId + 1) % 2 : _teamId;
                int strategyId = _teamId;
                if (announce.Body.CurMission.CommonPara.TeamCount == 2)
                {// 跟踪有且仅有两支队伍参与的仿真使命的半场状态 LiYoubing 20120520
                    strategyId = announce.Body.CurMission.CommonPara.IsExchangedHalfCourt ? (_teamId + 1) % 2 : _teamId;
                    bool rdoLeftChecked = (announce.Body.CurMission.TeamsRef[strategyId].Para.MyHalfCourt 
                        == HalfCourt.LEFT) ? true : false;
                    WinFormsServicePort.FormInvoke(delegate()
                    {
                        _clientControlBoard.SetRadioTeamState(rdoLeftChecked, !rdoLeftChecked, announce.Body.CurMission.TeamsRef[strategyId].Fishes[0].ColorFish);
                    });
                }
                decisions = _clientControlBoard.StragegyInterface.GetDecision(announce.Body.CurMission, strategyId);
                //decisions = _clientControlBoard.StragegyInterface.GetDecision(announce.Body.CurMission, _teamId);
            }
            catch
            {
                //_strategyExceptionFlag = true;
                MessageBox.Show("Remoting object timeout.\nThe instance of class Strategy has been released.\n" 
                    + "Your simulated robofish will not be controlled.",
                    "Confirming", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (decisions == null) yield break;
            announcedec.Body = new client.ClientAnnounceDecisionRequest(decisions, _teamId);

            // 使用Uri建立到Server服务实例附加端口的Forwarder引用
            client.ClientBaseOperations alternateServerPort = ServiceForwarder<client.ClientBaseOperations>(_alternateServerPortUri);
            if (alternateServerPort != null)
            {
                alternateServerPort.Post(announcedec);

                DefaultUpdateResponseType announceRsp = null;
                yield return Arbiter.Choice(announce.ResponsePort,
                delegate(DefaultUpdateResponseType rsp)
                {
                    announceRsp = rsp;
                },
                delegate(Fault f)
                {
                    WinFormsServicePort.FormInvoke(delegate()
                    {
                        _clientControlBoard.DisplayCompetitionState(string.Format("Connection failed"));
                    });
                });
            }

            yield break;
        }

        //20101209
        void HeartBeatAnnounceHandler(client.HeartBeat announce)
        {
            client.HeartBeatResponse response = new client.HeartBeatResponse();
            response.RspHeartBeat = announce.Body.HeartBeat + "OK";
            announce.ResponsePort.Post(response);
        }

        //20101214
        void TeamIdChangedAnnounceHandler(client.TeamIdChanged announce)
        {
            int tmp = _teamId;
            _teamId = announce.Body.TeamId;

            /*
            WinFormsServicePort.FormInvoke(delegate()
            {
                _clientControlBoard.DisplayCompetitionState(string.Format("ID Changed from {0} to {1}", tmp + 1, _teamId + 1));
            });  
             */

            //modified 20110218
            if (_teamId >= 0)
            {
                WinFormsServicePort.FormInvoke(delegate()
                {
                    _clientControlBoard.DisplayCompetitionState(string.Format("ID Changed from {0} to {1}", tmp + 1, _teamId + 1));
                });
            }
            else
            {
                WinFormsServicePort.FormInvoke(delegate()
                {
                    _clientControlBoard.DisplayCompetitionState(string.Format("Connection rejected. Only {0} teams are allowed.", _teamCount));
                });

                return;
            }

            if (_teamCount == 2)
            {// 当前使命为对抗性比赛时才设置左右半场
                if (_teamId == 0)
                {
                    WinFormsServicePort.FormInvoke(delegate() { _clientControlBoard.SetRadioTeamState(true, false); });
                }
                else if (_teamId == 1)
                {
                    WinFormsServicePort.FormInvoke(delegate() { _clientControlBoard.SetRadioTeamState(false, true); });
                }
            }
            WinFormsServicePort.FormInvoke(delegate()
            {
                _clientControlBoard.SetTeamId(_teamId);
            });

            SpawnIterator<string>(_teamName, AnnounceTeamNameToServer);

            //如果该队已经向Server服务实例发送过准备好的信息了，则需要带着新的编号再发送一遍供Server判断。
            //如果没有发送过，则不需要处理。
            if (_isReady == true)
            {
                SpawnIterator(AnnounceReadyToServer);
            }
        }
    }
}