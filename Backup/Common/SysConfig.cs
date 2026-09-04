//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: SysConfig.cs
// Date: 20101120  Author: LiYoubing  Version: 1
// Description: 系统参数配置处理类定义文件
// Histroy:
// Date: 20101120  Author: LiYoubing
// Modification: 修改内容简述
// Date: 20120414  Author: ChenXiao
// Modification: xml中添加missions节点
// Date: 20120420  Author: ChenXiao
// Modification:
// 1、将strConfigPath改为成员变量，供新添加的AddXmElements（），addNode（）方法调用
// 2、添加了删除该类实例的方法，以便热加载xml
// 3、添加了GetValue（）；GetXmlNode（）方法
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Runtime.CompilerServices; // for MethodImpl

namespace URWPGSim2D.Common
{
    /// <summary>
    /// 系统参数配置xml文件处理类 by chenwei 20101106
    /// </summary>
    public class SysConfig
    {
        #region Singleton设计模式实现让该类最多只有一个实例且能全局访问
        //private SysConfig() { }
        private static SysConfig instance = null;

        /// <summary>
        /// 创建或获取该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SysConfig Instance()
        {
            if (instance == null)
            {
                instance = new SysConfig();
            }
            return instance;
        }

        /// <summary>
        /// 销毁该类的唯一实例
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void release()
        {
            if (instance != null)
            {
                instance = null;
            }
        }

        #endregion

        /// <summary>
        /// 缓存配置文件的XmlDocument对象
        /// </summary>
        private XmlDocument _configDoc = null
            ;
        string strConfigPath = @"config.xml";

        /// <summary>
        ///  构造函数中尝试加载配置文件，不成功则创建默认配置文件
        /// </summary>
        private SysConfig()
        {
            _configDoc = new XmlDocument();         // 创建一个Xml文件对象
            try
            {
                _configDoc.Load("config.xml");      // 试图加载配置文件
            }
            catch                                   // 配置文件加载不成功
            {
                try
                {
                    DefautConfigFileWriter();       // 创建默认配置文件
                    _configDoc.Load("config.xml");  // 读取默认配置文件
                }
                catch
                {
                    throw (new Exception("application configuration file initial error."));
                }
                if (_configDoc == null)             // 如果load不成功就没有返回值
                {
                    throw (new Exception("application configuration file initial error."));
                }
            }
        }

        /// <summary>
        /// 系统参数默认配置Xml文件写操作方法
        /// </summary>
        /// <returns></returns>
        private void DefautConfigFileWriter()
        {
          

            try//写入元素
            {
                XmlWriterSettings mySettings = new XmlWriterSettings();
                mySettings.Indent = true;               // 是否进行缩进
                mySettings.IndentChars = ("    ");      // 缩进4个空格

                XmlWriter myWriter = XmlWriter.Create(strConfigPath, mySettings);

                #region XML文档
                myWriter.WriteStartDocument();              // 文档开始写入文档声明

                #region config
                myWriter.WriteStartElement("config");       // 根节点config begin

                //第1个子节点 //写入嵌套元素
                #region fish
                myWriter.WriteStartElement("fish");         // fish begin

                #region fishbody
                myWriter.WriteStartElement("fishbody");     // fishbody begin
                myWriter.WriteElementString("BodyLength", "160");
                myWriter.WriteElementString("BodyWidth", "45");
                myWriter.WriteEndElement();                 // fishbody end
                #endregion

                #region fishtail
                myWriter.WriteStartElement("fishtail");     // fishtail begin
                myWriter.WriteElementString("TailJointLength1", "88");
                myWriter.WriteElementString("TailJointLength2", "66");
                myWriter.WriteElementString("TailJointLength3", "55");
                myWriter.WriteEndElement();                 // fishtail end
                #endregion fishtail

                myWriter.WriteEndElement();                 // fish end
                #endregion fish

                //第2个子节点
                #region ball
                myWriter.WriteStartElement("ball");         // ball begin
                myWriter.WriteElementString("BallRadius", "58");
                myWriter.WriteEndElement();                 // ball end
                #endregion

                //第3个子节点
                #region obstacle
                myWriter.WriteStartElement("obstacle");     // obstacle begin
                myWriter.WriteEndElement();                 // obstacle end
                #endregion

                //第4个子节点
                #region form
                myWriter.WriteStartElement("form");         // form begin

                #region mainform
                myWriter.WriteStartElement("mainform");     // mainform begin
                myWriter.WriteElementString("SpanBetweenFieldAndTabControlPix", "10");
                myWriter.WriteElementString("FormPaddingPix", "10");
                myWriter.WriteEndElement();                 // mainform end
                #endregion

                #region showGameform
                myWriter.WriteStartElement("showGameform"); // showGameform begin
                myWriter.WriteElementString("FormLocationX", "11");
                myWriter.WriteElementString("FormLocationY", "102");
                myWriter.WriteElementString("FormWidth", "662");
                myWriter.WriteElementString("FormHighth", "450");
                myWriter.WriteEndElement();                 // showGameform end
                #endregion

                #region tabControl
                myWriter.WriteStartElement("tabControl");   // tabControl begin
                myWriter.WriteEndElement();                 // tabControl end
                #endregion

                myWriter.WriteEndElement();                 // form end
                #endregion

                //第5个子节点
                #region field
                myWriter.WriteStartElement("field");        // field begin
                myWriter.WriteElementString("FieldLengthXMm", "3000");
                myWriter.WriteElementString("FieldLengthZMm", "2000");
                myWriter.WriteElementString("GoalDepthMm", "150");
                myWriter.WriteElementString("GoalWidthMm", "400");

                myWriter.WriteElementString("FieldInnerBorderPix", "5");
                myWriter.WriteElementString("FieldOuterBorderPix", "20");
                myWriter.WriteElementString("ForbiddenZoneLengthXMm", "400");
                myWriter.WriteElementString("ForbiddenZoneLengthZMm", "1000");
                myWriter.WriteElementString("CenterCircleRadiusMm", "500");
                myWriter.WriteEndElement();                 // field end
                #endregion

                //第6个子节点
                #region paras
                myWriter.WriteStartElement("paras");        // paras begin
                myWriter.WriteElementString("MaxCachedCycles", "50");
                myWriter.WriteEndElement();                 // paras end
                #endregion
                //第7个子节点
                #region missions
                myWriter.WriteStartElement("missions");        // missions begin
                myWriter.WriteEndElement();                // missions结束
                #endregion
                myWriter.WriteEndElement();                 //根节点config结束
                #endregion
                myWriter.WriteEndDocument();                 //文档结束
                #endregion
                myWriter.Flush();   // 将XmlWriter中的内容写入磁盘文件
                myWriter.Close();
            }
            catch (Exception e)
            {
                throw (e);
            }
        }

        /// <summary>
        /// 根据给定的配置参数名读取配置参数值
        /// </summary>
        /// <param name="strKeyName">string类型的配置参数名称</param>
        /// <returns>string类型的配置参数值</returns>
        public string MyXmlReader(string strKeyName)
        {
            XmlReaderSettings mySettings = new XmlReaderSettings();

            // 创建一个读取节点类的对象.表示提供对XmlNode中的XML数据进行快速非缓存的只进访问的读取器
            // 是在内存中读取,所以路径是内存中的对象.
            XmlNodeReader nodeReader = new XmlNodeReader(_configDoc);
            XmlReader myReader = XmlReader.Create(nodeReader, mySettings);  // 将读取节点类的对象作为Xml文件读取类的输入参数
            myReader.ReadToFollowing(strKeyName);                           // 定位到配置参数名称所在的目标节点
            return myReader.ReadElementContentAsString();                   // 将配置参数值作为string返回
        }

        //added by chenxiao 20120420
        /// <summary>
        /// 根据给定的配置参数名读取配置参数值
        /// </summary>
        /// <param name="strNodeName">所要找的节点的xpath</param>
        /// <returns>返回指定节点，找不到返回null</returns>
        public XmlNode GetXmlNode(string xpath)
        {
            return _configDoc.SelectSingleNode(xpath);
        }
        /// <summary>
        /// 根据给定的xpath获取属性值
        /// </summary>
        /// <param name="xpath">所要找的属性的xpath</param>
        /// <returns>返回指定属性值</returns>
        public string GetValue(string xpath)
        {
            return _configDoc.SelectSingleNode(xpath).InnerText;
        }

        /// <summary>
        /// 为父节点插入新节点
        /// </summary>
        /// <param name="parentNode">父节点，即所要插入的位置</param>
        /// <param name="nodeName">所要生成的新节点名称</param>
        public void AddXmlNode(XmlNode parentNode,string nodeName)
        {
            parentNode.AppendChild(_configDoc.CreateElement(nodeName));
            _configDoc.Save(strConfigPath);
        }

        /// <summary>
        /// 为父节点插入多个子元素
        /// </summary>
        /// <param name="parentNode">父节点，即所要插入的位置</param>
        /// <param name="dic">key是属性名，value是属性值</param>
        public void AddXmElements(XmlNode parentNode, Dictionary<string, string> dic)
        {
            XmlNode node;
            foreach (KeyValuePair<string,string> pair in dic)
            {
                node = _configDoc.CreateElement(pair.Key);
                node.InnerText = pair.Value;
                parentNode.AppendChild(node);
            }
            _configDoc.Save(strConfigPath);
        }
    }
}