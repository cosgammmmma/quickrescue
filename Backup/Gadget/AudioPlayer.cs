using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace URWPGSim2D.Gadget
{
    /// <summary>
    /// 使用API的音频播放器类
    /// 生成该类的对象后，给对象的FileName属性设置音频文件名加载音频文件
    /// 然后使用Play/Pause/Stop控制音频文件的播放/暂停/停止
    /// </summary>
    public class AudioPlayer
    {
        // 定义API函数使用的字符串变量 
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)]
        private string strName = "" ;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)]
        private string durLength = "" ;
        [MarshalAs(UnmanagedType.LPTStr, SizeConst=128)]
        private string strTmp ="";
        int ilong;

        /// <summary>
        /// 定义播放状态枚举类型
        /// </summary>
        public enum State
        {
            Playing = 1,
            Puased = 2,
            Stoped = 3
        };

        /// <summary>
        /// 定义音频文件各种参数构成的结构体类型
        /// </summary>
        public struct structMCI 
        {
            public bool bMut;
            public int iDur;
            public int iPos;
            public int iVol;
            public int iBal;
            public string strName;
            public State state;
        };

        /// <summary>
        /// 音频文件参数
        /// </summary>
        public structMCI mc = new structMCI() ;

        /// <summary>
        /// 关联到当前实例的音频文件名
        /// 通过给当前实例设置关联的音频文件名来加载音频文件
        /// </summary>
        public string FileName
        {
            get
            {
                return mc.strName;
            }
            set
            {
                try
                {
                    strTmp = "";
                    strTmp = strTmp.PadLeft(127,Convert.ToChar(" "));
                    strName = "";
                    strName = strName.PadLeft(260,Convert.ToChar(" "));
                    mc.strName = value;
                    ilong = AudioAPI.GetShortPathName(mc.strName, strName, strName.Length);
                    strName = GetCurrPath(strName);
                    strName = "open " + Convert.ToChar(34) + strName + Convert.ToChar(34) + " alias media";
                    ilong = AudioAPI.mciSendString("close all", strTmp, strTmp.Length , 0);
                    ilong = AudioAPI.mciSendString( strName, strTmp, strTmp.Length, 0);
                    ilong = AudioAPI.mciSendString("set media time format milliseconds", strTmp, strTmp.Length , 0);
                    mc.state = State.Stoped;
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("读取媒体文件出错!"); 
                }
            }
        }

        /// <summary>
        /// 开始播放音频文件
        /// </summary>
        public void Play()
        {
            strTmp = "";
            strTmp = strTmp.PadLeft(127,Convert.ToChar(" "));
            AudioAPI.mciSendString("play media repeat", strTmp, strTmp.Length, 0);
            mc.state = State.Playing;
        }

        /// <summary>
        /// 开始播放音频文件
        /// </summary>
        public void Play(int handle)
        {
            strTmp = "";
            strTmp = strTmp.PadLeft(127, Convert.ToChar(" "));
            //AudioAPI.mciSendString("seek to %0", null, 0, 0);
            AudioAPI.mciSendString("play media notify", strTmp, strTmp.Length, handle);
            mc.state = State.Playing;
        }

        /// <summary>
        /// 停止播放音频文件
        /// </summary>
        public void Stop()
        {
            strTmp = "";
            strTmp = strTmp.PadLeft(128,Convert.ToChar(" "));
            //ilong = AudioAPI.mciSendString("stop media", strTmp, 128, 0);
            ilong = AudioAPI.mciSendString("close media", strTmp, 128, 0);
            ilong = AudioAPI.mciSendString("close all", strTmp, 128, 0);
            mc.state = State.Stoped;
        }

        /// <summary>
        /// 暂停播放音频文件
        /// </summary>
        public void Pause()
        {
            strTmp = "";
            strTmp = strTmp.PadLeft(128, Convert.ToChar(" "));
            ilong = AudioAPI.mciSendString("pause media", strTmp, strTmp.Length, 0);
            mc.state = State.Puased;
        }

        /// <summary>
        /// 继续播放音频文件
        /// </summary>
        public void Resume()
        {
            if (mc.state == State.Puased)
            {
                strTmp = "";
                strTmp = strTmp.PadLeft(128, Convert.ToChar(" "));
                ilong = AudioAPI.mciSendString("resume media", strTmp, strTmp.Length, 0);
                mc.state = State.Playing;
            }
        }

        private string GetCurrPath(string strName)
        {
            if(strName.Length < 1) return ""; 
            strName = strName.Trim();
            strName = strName.Substring(0, strName.Length-1);
            return strName;
        }

        /// <summary>
        /// 音频文件总时长
        /// </summary>
        public int Duration
        {
            get
            {
                durLength = "";
                durLength = durLength.PadLeft(128, Convert.ToChar(" ")) ;
                AudioAPI.mciSendString("status media length", durLength, durLength.Length, 0);
                durLength = durLength.Trim();
                if(durLength == "") return 0;
                return (int)(Convert.ToDouble(durLength) / 1000f); 
            }
        }

        /// <summary>
        /// 音频文件当前已经播放的时长
        /// </summary>
        public int CurrentPosition
        {
            get
            {
                durLength = "";
                durLength = durLength.PadLeft(128,Convert.ToChar(" ")) ;
                AudioAPI.mciSendString("status media position", durLength, durLength.Length, 0);
                mc.iPos = (int)(Convert.ToDouble(durLength) / 1000f);
                return mc.iPos;
            }
        }
    }

    /// <summary>
    /// 音频文件播放API构成的静态类
    /// </summary>
    public static class AudioAPI
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName(string lpszLongPath, string shortFile, int cchBuffer);

        [DllImport("winmm.dll", EntryPoint="mciSendString", CharSet = CharSet.Auto)]
        public static extern int mciSendString(string lpstrCommand, 
            string lpstrReturnString, int uReturnLength, int hwndCallback);
    }
}
