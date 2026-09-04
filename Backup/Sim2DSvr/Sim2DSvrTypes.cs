//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Sim2DSvrTypes.cs
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真平台服务端Sim2DSvr Dss Service相关类型定义文件
// Histroy:
// Date: 20101116  Author: LiYoubing
// Modification: 修改内容简述
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices; // for MethodImpl
using W3C.Soap;

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;

using URWPGSim2D.Common;

namespace URWPGSim2D.Sim2DSvr
{
    #region DssNewService自动生成的代码
    /// <summary>
    /// Sim2DSvr contract class
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// DSS contract identifer for Sim2DSvr
        /// </summary>
        [DataMember]
        public const string Identifier = "http://www.ursim.org/2010/11/sim2dsvr.html";
    }

    /// <summary>
    /// Sim2DSvr state
    /// </summary>
    [DataContract]
    public class Sim2DSvrState
    {
    }

    /// <summary>
    /// Sim2DSvr main operations port
    /// </summary>
    [ServicePort]
    public class Sim2DSvrOperations : PortSet<
        ClientAnnounceUri, ClientAnnounceTeamName, ClientAnnounceReady,
        CompetitionTypeChanged, CompetitionControlButton, MissionPara, 
        DsspDefaultLookup, DsspDefaultDrop, Get, Subscribe>
    {
    }

    /// <summary>
    /// Sim2DSvr get operation
    /// </summary>
    public class Get : Get<GetRequestType, PortSet<Sim2DSvrState, Fault>>
    {
        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        public Get()
        {
        }

        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        /// <param name="body">the request message body</param>
        public Get(GetRequestType body)
            : base(body)
        {
        }

        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        /// <param name="body">the request message body</param>
        /// <param name="responsePort">the response port for the request</param>
        public Get(GetRequestType body, PortSet<Sim2DSvrState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// Sim2DSvr subscribe operation
    /// </summary>
    public class Subscribe : Subscribe<SubscribeRequestType, PortSet<SubscribeResponseType, Fault>>
    {
        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        public Subscribe()
        {
        }

        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        /// <param name="body">the request message body</param>
        public Subscribe(SubscribeRequestType body)
            : base(body)
        {
        }

        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        /// <param name="body">the request message body</param>
        /// <param name="responsePort">the response port for the request</param>
        public Subscribe(SubscribeRequestType body, PortSet<SubscribeResponseType, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }
    #endregion

    #region 服务端界面与Server服务实例交互消息相关定义
    /// <summary>
    /// 服务端界面与Server服务实例交互消息端口类
    /// </summary>
    //public class FromServerUiEvents : PortSet<FromServerUiMsg, FromServerUiControlButtonMsg>
    public class FromServerUiEvents : Port<FromServerUiMsg>
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static FromServerUiEvents instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static FromServerUiEvents Instance()
        {
            if (instance == null)
            {
                instance = new FromServerUiEvents();
            }
            return instance;
        }

        private FromServerUiEvents() { }
        #endregion
    }

    /// <summary>
    /// 服务端界面与Server服务实例交互的消息类
    /// </summary>
    public class FromServerUiMsg
    {
        /// <summary>
        /// 服务端界面与Server服务实例交互的消息类型枚举定义
        /// </summary>
        public enum MsgEnum
        {
            COMPETITION_ITEM_CHANGED,
            START,
            PAUSE,
            REPLAY,
            RESTART,
            CLOSED
        }

        /// <summary>
        /// 随消息传递的string型参数数组
        /// </summary>
        private string[] _parameters;
        public string[] Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        /// <summary>
        /// 消息类型
        /// </summary>
        private MsgEnum _msg;
        public MsgEnum Msg
        {
            get { return _msg; }
            set { _msg = value; }
        }

        /// <summary>
        /// 随消息传递的Object型参数
        /// </summary>
        private object _object;
        public object Object
        {
            get { return _object; }
            set { _object = value; }
        }

        /// <summary>
        /// 使用消息类型msg和string型参数数组创建消息对象
        /// </summary>
        /// <param name="msg">消息类型</param>
        /// <param name="parameters">string型参数数组，不需要参数则使用null初始化</param>
        public FromServerUiMsg(MsgEnum msg, string[] parameters)
        {
            _msg = msg;
            _parameters = parameters;
        }

        /// <summary>
        /// 使用消息类型msg/string型参数数组/Object型参数创建消息对象
        /// </summary>
        /// <param name="msg">消息类型</param>
        /// <param name="parameters">string型参数数组，不需要参数则使用null初始化</param>
        /// <param name="objectParam">Object型参数，不需要则使用null初始化</param>
        public FromServerUiMsg(MsgEnum msg, string[] parameters, object objectParam)
        {
            _msg = msg;
            _parameters = parameters;
            _object = objectParam;
        }
    }
    #endregion

    #region MissionPara消息
    // LiuShu 20101108
    /// <summary>
    /// Server向Client发送的通知当前使命对象的请求消息类
    /// </summary>
    /// <remarks>将当前周期更新过的Mission对象通知给Client</remarks>
    [DataContract]
    public class MissionParaRequest
    {
        // 如果有带参数的构造函数则一定要定义无参构造函数
        public MissionParaRequest()
        { }

        public MissionParaRequest(Mission mission)
        {
            CurMission = mission;
        }

        /// <summary>
        /// 服务端界面上刚变化（重新选择）过的比赛类型
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public Mission CurMission;
    }

    public class MissionPara : Update<MissionParaRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
    #endregion
    
    #region CompetitionTypeChanged消息
    // LiuShu 20101108
    /// <summary>
    /// Server向Client发送的通知比赛类型变化情况的请求消息类
    /// </summary>
    /// <remarks>核心内容是将当前服务端界面上变化后的比赛类型通知给Client</remarks>
    [DataContract]
    public class CompetitionTypeChangedRequest
    {
        // 如果有带参数的构造函数则一定要定义无参构造函数
        // CompetitionTypeChangedRequest类要作为Update的TBody参数必须有一个无参构造函数
        public CompetitionTypeChangedRequest()
        { }

        public CompetitionTypeChangedRequest(string strCompetitionType, int teamCount)
        {
            CompetitionType = strCompetitionType;
            TeamCount = teamCount;
        }

        /// <summary>
        /// 服务端界面上刚变化（重新选择）过的比赛类型
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public string CompetitionType;

        /// <summary>
        /// 当前使命类型允许的队伍总数
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public int TeamCount;

    }

    public class CompetitionTypeChanged : Update<CompetitionTypeChangedRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
    #endregion

    #region CompetitionControlButton消息
    // LiuShu 20101108
    /// <summary>
    /// Server向Client发送的比赛控制请求消息类
    /// </summary>
    [DataContract]
    public class CompetitionControlButtonRequest
    {
        public CompetitionControlButtonRequest()
        { }

        public CompetitionControlButtonRequest(string controlButtonMessage)
        {
            ControlButtonMessage = controlButtonMessage;
        }

        /// <summary>
        /// 服务端界面上点击了控制按钮信息
        [DataMember]
        [DataMemberConstructor]
        public string ControlButtonMessage;
    }

    public class CompetitionControlButton : Update<CompetitionControlButtonRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
    #endregion

    #region ClientAnnounceUri消息
    // LiYoubing 20101104
    /// <summary>
    /// Client向Server发送的通知请求消息类
    /// </summary>
    /// <remarks>核心内容是将当前Client服务实例的URI通知给Server</remarks>
    [DataContract]
    public class ClientAnnounceUriRequest
    {
        // 如果有带参数的构造函数则一定要定义无参构造函数
        // ClientAnnounceRequest类要作为Insert的TBody参数必须有一个无参构造函数
        public ClientAnnounceUriRequest()
        { }

        public ClientAnnounceUriRequest(string strServiceUri)
        {
            Service = strServiceUri;
        }

        /// <summary>
        /// 当前Client服务实例URI
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public string Service;
    }

    /// <summary>
    /// Server对Client的通知请求的响应消息类
    /// </summary>
    /// <remarks>核心内容是将当前队伍是A队还是B队的信息返回给Client，后修改成返回队伍从0开始的编号,以及当前比赛项目，允许添加的队伍数，Server附加端口Uri</remarks>
    [DataContract]
    public class ClientAnnounceUriResponse
    {
        // 如果有带参数的构造函数则一定要定义无参构造函数
        public ClientAnnounceUriResponse()
        { }

        public ClientAnnounceUriResponse(int teamCount, int teamId, string missionName, string alternateServerPortUri)
        {
            TeamCount = teamCount;
            TeamId = teamId;
            MissionName = missionName;
            AlternateServerPortUri = alternateServerPortUri;
        }

        /// <summary>
        /// 当前使命类型允许的队伍总数
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public int TeamCount;

        /// <summary>
        /// 当前Client服务实例连上Server服务实例时获得的序号即从0开始的队伍编号
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public int TeamId;

        /// <summary>
        /// 当前选中的使命类型名称
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public string MissionName;

        /// <summary>
        /// Server服务实例建立的供传递信息的附加端口
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public string AlternateServerPortUri;

    }

    public class ClientAnnounceUri : Insert<ClientAnnounceUriRequest, DsspResponsePort<ClientAnnounceUriResponse>>
    {
    }
    #endregion

    #region ClientAnnounceTeamName消息
    // LiuShu 20101108
    /// <summary>
    /// Client向Server发送的通知队伍名称请求消息类
    /// </summary>
    /// <remarks>核心内容是将当前Client服务实例加载的策略中给定的队伍名称通知给Server</remarks>
    [DataContract]
    public class ClientAnnounceTeamNameRequest
    {
        // 如果有带参数的构造函数则一定要定义无参构造函数
        // ClientTeamNameRequest类要作为Insert的TBody参数必须有一个无参构造函数
        public ClientAnnounceTeamNameRequest()
        { }

        public ClientAnnounceTeamNameRequest(string uri, int teamId, string strTeamName)
        {
            Service = uri;
            TeamId = teamId;
            TeamName = strTeamName;
        }

        [DataMember]
        [DataMemberConstructor]
        public string Service;

        /// <summary>
        /// 当前Client服务实例连上Server服务实例时获得的序号即从0开始的队伍编号
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public int TeamId;

        /// <summary>
        /// 当前Client服务实例加载的策略中给定的队伍名称
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public string TeamName;
    }

    public class ClientAnnounceTeamName : Update<ClientAnnounceTeamNameRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
    #endregion

    #region ClientAnnounceReady消息
    // LiuShu 20101116
    /// <summary>
    /// Client向Server发送的Ready消息类
    /// </summary>
    [DataContract]
    public class ClientAnnounceReadyRequest
    {
        public ClientAnnounceReadyRequest()
        { }

        public ClientAnnounceReadyRequest(int teamId)
        {
            TeamId = teamId;
        }

        /// <summary>
        /// 当前Client服务实例在Server服务实例中的编号
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public int TeamId;
    }

    public class ClientAnnounceReady : Update<ClientAnnounceReadyRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
    #endregion

    //#region ClientAnnounceDecision消息
    //// LiuShu 20101116
    ///// <summary>
    ///// Client向Server发送的Ready消息类
    ///// </summary>
    //[DataContract]
    //public class ClientAnnounceDecisionRequest
    //{
    //    public ClientAnnounceDecisionRequest()
    //    { }

    //    public ClientAnnounceDecisionRequest(Decision[] decisions, int teamId)
    //    {
    //        Decisions = decisions;
    //        TeamId = teamId;
    //    }

    //    /// <summary>
    //    /// 当前Client服务实例为其所代表的队伍所有仿真机器鱼计算出的决策数组
    //    /// </summary>
    //    [DataMember]
    //    [DataMemberConstructor]
    //    public Decision[] Decisions;

    //    /// <summary>
    //    /// 当前Client服务实例连上Server服务实例时获得的序号即从0开始的队伍编号
    //    /// </summary>
    //    [DataMember]
    //    [DataMemberConstructor]
    //    public int TeamId;
    //}

    //public class ClientAnnounceDecision : Update<ClientAnnounceDecisionRequest, PortSet<DefaultUpdateResponseType, Fault>>
    //{
    //}
    //#endregion
}