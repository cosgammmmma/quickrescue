using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WMEncoderLib;

namespace URWPGSim2D.Screencast
{
    public partial class Screencast : Form
    {
        public Screencast()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Windows Media Encoder编码器实例引用
        /// </summary>
        WMEncoder enc = null;
        
        /// <summary>
        /// 待屏幕录像的URWPGSim2D程序主窗口句柄
        /// </summary>
        int hWndURWPGSim2D = 0;

        private void btnStart_Click(object sender, EventArgs e)
        {
            // 实例化Windows Media Encoder编码器
            enc = new WMEncoderClass();
            IWMEncSourceGroupCollection SrcGrpColl;
            IWMEncSourceGroup2 SrcGrp;
            // 音频源
            //IWMEncAudioSource SrcAud;
            // 视频源
            IWMEncVideoSource2 SrcVid;
            //IWMEncProfile2 Pro = new WMEncProfile2Class();

            try
            {
                SrcGrpColl = enc.SourceGroupCollection;
                SrcGrp = (IWMEncSourceGroup2)SrcGrpColl.Add("SG_1");
                SrcVid = (IWMEncVideoSource2)SrcGrp.AddSource(WMENC_SOURCE_TYPE.WMENC_VIDEO);

                Screen currentScreen;
                if (hWndURWPGSim2D == 0)
                {// 获取当前窗口所在屏幕作为当前屏幕
                    currentScreen = Screen.FromHandle(this.Handle);
                }
                else
                {// 获取待屏幕录像的URWPGSim2D程序主窗口所在屏幕作为当前屏幕
                    currentScreen = Screen.FromHandle((IntPtr)hWndURWPGSim2D);
                    hWndURWPGSim2D = 0;
                }
                // 以当前屏幕设备作为视频来源设备
                SrcVid.SetInput(currentScreen.DeviceName, "ScreenCap", "");
                //SrcVid.SetInput("ScreenCapture1", "ScreenCap", "");
                
                // 设置音频来源
                //if (ckbSound.Checked)
                //{
                //    SrcAud = (IWMEncAudioSource)SrcGrp.AddSource(WMENC_SOURCE_TYPE.WMENC_AUDIO);
                //    SrcAud.SetInput("Default_Audio_Device", "DEVICE", "");
                //}

                foreach (IWMEncProfile wp in enc.ProfileCollection)
                {// 循环检查当前编码器实例中包含的全部配置文件
                    if (wp.Name.Equals(cmbEncoder.SelectedItem.ToString()))
                    {// 如果当前配置文件（Profile）名称与选中的移植
                        // 将当前Profile设为当前视频源的配置文件
                        SrcGrp.set_Profile(wp);
                        break;
                    }
                }

                // 为当前编码器实例设定输出文件名称
                enc.File.LocalFileName = txtFileName.Text;

                // 从界面输入取得待录屏区域左上角坐标/宽度/高度（单位为像素）
                int x = Convert.ToInt32(txtX.Text);
                int y = Convert.ToInt32(txtY.Text);
                int w = Convert.ToInt32(txtWidth.Text);
                int h = Convert.ToInt32(txtHeight.Text);

                // 适应多显示器环境 取得当前系统所有显示器构成的虚拟屏幕区域
                Rectangle screenRect = SystemInformation.VirtualScreen;

                #region
                //// 为当前视频源设定宽度为当前屏幕宽度
                //SrcVid.Width = currentScreen.Bounds.Width;
                //// 为当前视频源设定高度为当前屏幕高度
                //SrcVid.Height = currentScreen.Bounds.Height;

                //// 对当前视频源进行裁剪 裁剪后的视频宽度高度即为待录像区域宽度高度
                //// 左边裁掉待录像区域左上角坐标X值的大小
                //SrcVid.CroppingLeftMargin = x;
                //// 上边裁掉待录像区域左上角坐标Y值的大小
                //SrcVid.CroppingTopMargin = y;
                //// 右边裁掉总宽度和左边裁剪尺寸、待录像区域宽度之差
                //SrcVid.CroppingRightMargin = SrcVid.Width - SrcVid.CroppingLeftMargin - w;
                //// 下边裁掉总高度和上边裁剪尺寸、待录像区域高度之差
                //SrcVid.CroppingBottomMargin = SrcVid.Height - SrcVid.CroppingTopMargin - h;
                #endregion

                #region
                //int wm = SystemInformation.VirtualScreen.Width;
                //int hm = SystemInformation.VirtualScreen.Height;

                //SrcVid.Width = w;
                //SrcVid.Height = h;

                //float rx = (float)wm / (float)SrcVid.Width;
                //float ry = (float)hm / (float)SrcVid.Height;

                //SrcVid.CroppingLeftMargin = (int)(x / rx);
                //SrcVid.CroppingTopMargin = (int)(y / ry);
                //SrcVid.CroppingRightMargin = (int)((wm - x - w) / rx);
                //SrcVid.CroppingBottomMargin = (int)((hm - y - h) / ry);
                #endregion

                #region
                // 为当前视频源设定宽度为整个虚拟屏幕宽度
                SrcVid.Width = screenRect.Width;
                // 为当前视频源设定高度为整个虚拟屏幕高度
                SrcVid.Height = screenRect.Height;

                // 对当前视频源进行裁剪 裁剪后的视频宽度高度即为待录像区域宽度高度
                // 左边裁掉待录像区域左上角坐标X值与虚拟屏幕左上角坐标X值之差
                SrcVid.CroppingLeftMargin = x - screenRect.Left;
                // 上边裁掉待录像区域左上角坐标Y值与虚拟屏幕左上角坐标Y值之差
                SrcVid.CroppingTopMargin = y - screenRect.Top;
                // 右边裁掉总宽度和左边裁剪尺寸、待录像区域宽度之差
                SrcVid.CroppingRightMargin = SrcVid.Width - SrcVid.CroppingLeftMargin - w;
                // 下边裁掉总高度和上边裁剪尺寸、待录像区域高度之差
                SrcVid.CroppingBottomMargin = SrcVid.Height - SrcVid.CroppingTopMargin - h;
                #endregion

                #region
                //SrcVid.Width = w;
                //SrcVid.Height = h;

                //float rx = (float)screenRect.Width / (float)SrcVid.Width;
                //float ry = (float)screenRect.Height / (float)SrcVid.Height;

                //SrcVid.CroppingLeftMargin = (int)((x - screenRect.Left) / rx);
                //SrcVid.CroppingTopMargin = (int)((y - screenRect.Top) / ry);
                //SrcVid.CroppingRightMargin = (int)((screenRect.Width - (x - screenRect.Left) - w) / rx);
                //SrcVid.CroppingBottomMargin = (int)((screenRect.Height - (y - screenRect.Top) - h) / ry);
                #endregion

                // 最小化Screencast程序主窗口
                this.WindowState = FormWindowState.Minimized;

                // 开始编码
                enc.Start();
                btnStop.Enabled = true;
                btnStart.Enabled = false;
                this.TopMost = false;
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
            }
        }

        private void Screencast_Load(object sender, EventArgs e)
        {
            // 实例化Windows Media Encoder编码器
            enc = new WMEncoderClass();
            int index = 0;
            int i = 0;
            foreach (IWMEncProfile wp in enc.ProfileCollection)
            {// 将编码器Profile名称填充到cmbEncoder下拉框
                i++;
                cmbEncoder.Items.Add(wp.Name);
                // 存在"屏幕视频 - 高(CBR)"这一Profile则选中它
                if (index == 0 && wp.Name.Equals("屏幕视频 - 高(CBR)")) { index = i; }
            }
            cmbEncoder.SelectedIndex = index;
            enc = null;

            txtFileName.Text = Application.StartupPath + "\\test.wmv";
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (enc != null)
            {
                // 停止编码
                enc.Stop();
                enc = null;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }

        private void cmbEncoder_SelectedIndexChanged(object sender, EventArgs e)
        {
            toolTip.SetToolTip(cmbEncoder, cmbEncoder.SelectedItem.ToString());
        }

        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            const int WM_COPYDATA = 0x004A;
            switch (m.Msg)
            {
                case WM_COPYDATA:
                    COPYDATASTRUCT cds = new COPYDATASTRUCT();
                    Type cdsType = cds.GetType();
                    cds = (COPYDATASTRUCT)m.GetLParam(cdsType);
                    if (cds.dwData == IntPtr.Zero)
                    {// URWPGSim2DServer程序中按下的是End按钮则结束录像
                        btnStop_Click(null, null);
                        System.Diagnostics.Process.GetCurrentProcess().Kill();
                    }
                    else
                    {// URWPGSim2DServer程序中按下的是Video按钮则将发送的参数设置到Screencast界面
                        // 从收到的字符串分离录像区域左上角坐标/宽度高度/
                        // URWPGSim2D主窗口句柄/录像保存完整文件名
                        int x = Convert.ToInt32(cds.lpData.Substring(0, 6));
                        int y = Convert.ToInt32(cds.lpData.Substring(6, 6));
                        int w = Convert.ToInt32(cds.lpData.Substring(12, 6));
                        int h = Convert.ToInt32(cds.lpData.Substring(18, 6));
                        txtX.Text = x.ToString();
                        txtY.Text = y.ToString();
                        txtWidth.Text = w.ToString();
                        txtHeight.Text = h.ToString();
                        hWndURWPGSim2D = Convert.ToInt32(cds.lpData.Substring(24, 10));
                        txtFileName.Text = cds.lpData.Substring(34);
                        if (txtFileName.Text.LastIndexOf('.') != (txtFileName.Text.Length - 4))
                        {// 倒数第4个字符不是'.'则表明发送过来的文件名超长被截掉了一部分采用默认文件名
                            txtFileName.Text = Application.StartupPath + "\\test.wmv";
                        }
                        this.Location = new Point(x + (w - this.Width) / 2, y + (h - this.Height) / 2);
                    }
                    // 显示Screencast程序主窗口
                    this.WindowState = FormWindowState.Normal;
                    this.TopMost = true;
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }

        /// <summary>
        /// WM_COPYDATA类型的消息所传递的数据类型
        /// </summary>
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
            public string lpData;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            SaveFileDialog fileDlg = new SaveFileDialog();
            fileDlg.Filter = "video files (*.wmv)|*.wmv|video files (*.avi)|*.avi|All files (*.*)|*.*";  // 过滤文件类型
            if (fileDlg.ShowDialog() == DialogResult.OK)
            {
                txtFileName.Text = fileDlg.FileName;
            }
        }

        private void txtFileName_MouseHover(object sender, EventArgs e)
        {
            toolTip.SetToolTip(txtFileName, txtFileName.Text);
        }
    }
}