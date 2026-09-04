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

namespace URWPGSim2D.Sim2DSvr.ClientBase
{
    #region DssNewService自动生成的代码
    /// <summary>
    /// ClientBase contract class
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// DSS contract identifer for ClientBase
        /// </summary>
        [DataMember]
        public const string Identifier = "http://www.ursim.org/2010/11/sim2dclientbase.html";
    }

    /// <summary>
    /// ClientBase state
    /// </summary>
    [DataContract]
    public class ClientBaseState
    {
    }

    /// <summary>
    /// ClientBase main operations port
    /// </summary>
    [ServicePort]
    public class ClientBaseOperations : PortSet<HeartBeat, TeamIdChanged, ClientAnnounceDecision,
        DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

    /// <summary>
    /// ClientBase get operation
    /// </summary>
    public class Get : Get<GetRequestType, PortSet<ClientBaseState, Fault>>
    {
    }

    #endregion

    #region HeartBeat消息
    // 20101209
    /// <summary>
    /// Server向Client发送的心跳请求信息类
    /// </summary>
    /// <remarks>核心内容是的Server发送简单消息判断Client服务实例是否掉线。</remarks>
    [DataContract]
    public class HeartBeatRequest
    {
        public HeartBeatRequest()
        { }

        public HeartBeatRequest(string strHeartBeat)
        {
            HeartBeat = strHeartBeat;
        }

        [DataMember]
        [DataMemberConstructor]
        public string HeartBeat;
    }

    /// <summary>
    /// Server向Client发送的心跳响应信息类
    /// </summary>
    /// <remarks>核心内容是Server服务实例等待Client服务实例返回信息来判断此Client服务实例是否断掉</remarks>
    [DataContract]
    public class HeartBeatResponse
    {
        public HeartBeatResponse()
        { }

        public HeartBeatResponse(string rspHeartBeat)
        {
            RspHeartBeat = rspHeartBeat;
        }

        [DataMember]
        [DataMemberConstructor]
        public string RspHeartBeat;
    }

    public class HeartBeat : Update<HeartBeatRequest, DsspResponsePort<HeartBeatResponse>>
    {
    }
    #endregion

    #region TeamIdChanged消息
    // LiuShu 20101214
    /// <summary>
    /// Server向Client发送的通知比赛类型变化情况的请求消息类
    /// </summary>
    /// <remarks>核心内容是将当前服务端界面上变化后的比赛类型通知给Client</remarks>
    [DataContract]
    public class TeamIdChangedRequest
    {
        public TeamIdChangedRequest()
        { }

        public TeamIdChangedRequest(int teamId)
        {
            TeamId = teamId;
        }

        /// <summary>
        /// 服务端界面上刚变化过（有客户端断线后）的TeamId
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public int TeamId;
    }

    public class TeamIdChanged : Update<TeamIdChangedRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
    #endregion

    #region ClientAnnounceDecision消息
    // LiuShu 20101116
    /// <summary>
    /// Client向Server发送的Ready消息类
    /// </summary>
    [DataContract]
    public class ClientAnnounceDecisionRequest
    {
        public ClientAnnounceDecisionRequest()
        { }

        public ClientAnnounceDecisionRequest(Decision[] decisions, int teamId)
        {
            Decisions = decisions;
            TeamId = teamId;
        }

        /// <summary>
        /// 当前Client服务实例为其所代表的队伍所有仿真机器鱼计算出的决策数组
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public Decision[] Decisions;

        /// <summary>
        /// 当前Client服务实例连上Server服务实例时获得的序号即从0开始的队伍编号
        /// </summary>
        [DataMember]
        [DataMemberConstructor]
        public int TeamId;
    }

    public class ClientAnnounceDecision : Update<ClientAnnounceDecisionRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
    #endregion
}