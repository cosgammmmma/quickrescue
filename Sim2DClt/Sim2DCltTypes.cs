//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: Sim2DCltTypes.cs
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真平台客户端Sim2DClt Dss Service相关类型定义文件
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

namespace URWPGSim2D.Sim2DClt
{
    #region DssNewService自动生成的代码
    /// <summary>
    /// Sim2DClt contract class
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// DSS contract identifer for Sim2DClt
        /// </summary>
        [DataMember]
        public const string Identifier = "http://www.ursim.org/2010/11/sim2dclt.html";
    }

    /// <summary>
    /// Sim2DClt state
    /// </summary>
    [DataContract]
    public class Sim2DCltState
    {
    }

    /// <summary>
    /// Sim2DClt main operations port
    /// </summary>
    [ServicePort]
    public class Sim2DCltOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

    /// <summary>
    /// Sim2DClt get operation
    /// </summary>
    public class Get : Get<GetRequestType, PortSet<Sim2DCltState, Fault>>
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
        public Get(GetRequestType body, PortSet<Sim2DCltState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }
    #endregion

    #region 客户端界面与Client服务实例交互消息相关定义
    /// <summary>
    /// 客户端界面与Client服务实例交互消息端口类
    /// </summary>
    public class FromClientUiEvents : Port<FromClientUiMsg>
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        private static FromClientUiEvents instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static FromClientUiEvents Instance()
        {
            if (instance == null)
            {
                instance = new FromClientUiEvents();
            }
            return instance;
        }

        private FromClientUiEvents() { }
        #endregion
    }

    /// <summary>
    /// 客户端界面与Client服务实例交互的消息类
    /// </summary>
    public class FromClientUiMsg
    {
        /// <summary>
        /// 客户端界面与Client服务实例交互的消息类型枚举定义
        /// </summary>
        public enum MsgEnum
        {
            UI_LOADED,
            STRATEGY_LOADED,
            READY,
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
        public FromClientUiMsg(MsgEnum msg, string[] parameters)
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
        public FromClientUiMsg(MsgEnum msg, string[] parameters, object objectParam)
        {
            _msg = msg;
            _parameters = parameters;
            _object = objectParam;
        }
    }
    #endregion
}


