//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: ConductorSvr.cs
// Date: 20101116  Author: LiYoubing  Version: 1
// Description: 仿真平台服务端Sim2DSvr Dss Service及相应Dss Node启动引导WinForm程序实现文件
// Histroy:
// Date: 20101116  Author: LiYoubing
// Modification: 修改内容简述
// ……
//-----------------------------------------------------------------------

#region required by DssEnvironment Class
using Microsoft.Ccr.Core;   // in Microsoft.Ccr.Core assembly
using Microsoft.Dss.Hosting;// in Microsoft.Dss.Environment
using Microsoft.Dss.Runtime;// in Microsoft.Dss.Runtime
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

namespace ConductorSvr
{
    public partial class ConductorSvr : Form
    {
        public ConductorSvr()
        {
            InitializeComponent();
        }

        private void ConductorSvr_Load(object sender, EventArgs e)
        {
            if (IsPortInUse(ProtocolType.Tcp, 50000) || IsPortInUse(ProtocolType.Tcp, 50001))
            {
                System.Windows.Forms.MessageBox.Show("Tcp port 50000 or 50001 is in use, please check it.", "URWPGSim2D Server");
                System.Diagnostics.Process.GetCurrentProcess().Kill(); // 没有数据需要保存直接结束当前进程
            }

            // 调试时在同一节点上启动Server服务和Client服务
            string[] strArrManifestFileNames = new string[2] {
                Application.StartupPath + "\\Sim2DSvr.manifest.xml",
                Application.StartupPath + "\\Sim2DClt.manifest.xml",
            };
            //DssEnvironment.Initialize(50000, 50001, strArrManifestFileNames);

            // 在50000/50001这组端口上启动一个DSS Node并初始化Server服务实例
            string strServerManifestFileName = Application.StartupPath + "\\Sim2DSvr.manifest.xml";
            DssEnvironment.Initialize(50000, 50001, strServerManifestFileName);

            // 等待当前Dss Node上的某Dss Service发生Drop事件执行DssEnvironment.Shutdown命令
            DssEnvironment.WaitForShutdown();

            // 退出客户端启动引导程序界面不再显示
            Application.Exit();
        }

        /// <summary>
        /// 检查TCP或UDP端口是否已被占用
        /// </summary>
        /// <param name="protocol">ProtocolType.Tcp或ProtocolType.Udp</param>
        /// <param name="port">端口号</param>
        /// <returns>已占用为true否则为false</returns>
        private bool IsPortInUse(ProtocolType protocol, int port)
        {
            IPAddress ipAdd = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipe = new IPEndPoint(ipAdd, port);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, protocol);
            try
            {
                socket.Bind(ipe);
                socket.Close();
            }
            catch
            {
                socket.Close();
                return true;
            }
            return false;
        }
    }
}
