//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: ConductorClt.cs
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真平台客户端Sim2DClt Dss Service及相应Dss Node启动引导WinForm程序实现文件
// Histroy:
// Date: 20110617  Author: LiYoubing
// Modification: 
// 1.修正IsPortInUse函数，解决URWPGSimDClient在Win7下运行一个以上的实例时提示端口被占用而不能成功的问题
// ……
//-----------------------------------------------------------------------

#region required by DssEnvironment Class
using Microsoft.Ccr.Core;   // in Microsoft.Ccr.Core assembly
using Microsoft.Dss.Hosting;// in Microsoft.Dss.Environment
using Microsoft.Dss.Runtime;// in Microsoft.Dss.Runtime
using Microsoft.Dss.Core;   // in Microsoft.Dss.Runtime
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace ConductorClt
{
    public partial class ConductorClt : Form
    {
        public ConductorClt()
        {
            InitializeComponent();
        }

        private void ConductorClt_Load(object sender, EventArgs e)
        {
            DssRuntimeConfiguration dssConfig = new DssRuntimeConfiguration();
            dssConfig.PublicHttpPort = 0;
            dssConfig.PublicTcpPort = 0;

            int port = 40000;
            do // 从TCP端口40000起查找尚未被占用的端口
            {
                while (IsPortInUse(ProtocolType.Tcp, port))
                {
                    port++;
                }
                if (dssConfig.PublicHttpPort == 0)
                {
                    dssConfig.PublicHttpPort = port;
                    port++;
                }
                else
                {
                    dssConfig.PublicTcpPort = port;
                }

            } while (dssConfig.PublicHttpPort == 0 || dssConfig.PublicTcpPort == 0);

            // 调试时在同一节点上启动Server服务和Client服务
            string[] strArrManifestFileNames = new string[2] {
                Application.StartupPath + "\\Sim2DSvr.manifest.xml",
                Application.StartupPath + "\\Sim2DClt.manifest.xml",
            };
            //DssEnvironment.Initialize(dssConfig, strArrManifestFileNames);
            
            // 在50000/50001这组端口上启动一个DSS Node并初始化Server服务实例
            string strServerManifestFileName = Application.StartupPath + "\\Sim2DSvr.manifest.xml";
            //DssEnvironment.Initialize(dssConfig, strServerManifestFileName);

            // 在40000/40001这组端口上启动一个DSS Node并初始化Client服务实例
            string strClientManifestFileName = Application.StartupPath + "\\Sim2DClt.manifest.xml";
            DssEnvironment.Initialize(dssConfig, strClientManifestFileName);

            // 等待当前Dss Node上的某Dss Service发生Drop事件执行DssEnvironment.Shutdown命令
            DssEnvironment.WaitForShutdown();

            // 退出客户端启动引导程序界面不再显示
            Application.Exit();
        }

        //[System.Runtime.InteropServices.DllImportAttribute("Ws2_32.dll")]
        //private static extern int WSAStartup();

        /// <summary>
        /// 检查TCP或UDP端口是否已被占用
        /// </summary>
        /// <param name="protocol">ProtocolType.Tcp或ProtocolType.Udp</param>
        /// <param name="port">端口号</param>
        /// <returns>已占用为true否则为false</returns>
        private bool IsPortInUse(ProtocolType protocol, int port)
        {
            //IPAddress ipAdd = IPAddress.Parse("127.0.0.1");
            //IPEndPoint ipe = new IPEndPoint(ipAdd, port);
            //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, protocol);
            //try
            //{
            //    socket.Bind(ipe);
            //    socket.Close();
            //}
            //catch
            //{
            //    socket.Close();
            //    return true;
            //}
            //return false;

            // LiYoubing 20110617
            // 第一个实例运行后40000作为PublicHttpPort，40001作为PubilcTcpPortTcp
            // 运行第二个实例时调用IsPortInUse，40000不能Bind成功，表示已经被占用，而40001仍能Bind成功
            // 需要catch SocketException才能判断是否真被占用
            IPAddress ipAdd = IPAddress.Any;
            IPEndPoint ipe = new IPEndPoint(ipAdd, port);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, protocol);
            try
            {
                socket.Bind(ipe);
                return false;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.AddressAlreadyInUse 
                    || e.SocketErrorCode == SocketError.AccessDenied)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                socket.Close();
                socket = null;
            }
        }
    }
}
