//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: CustomControls.cs
// Date: 20110628  Author: LiYoubing
// Modification:
// 1.增加只允许输入数字的文本框和组合下拉框控件定义
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace URWPGSim2D.Screencast
{
    #region 只允许输入数字的文本框/组合下拉框控件
    // LiYoubing 20110627
    /// <summary>
    /// 只允许输入数字的文本框控件
    /// </summary>
    public class NumberTextBox : TextBox
    {
        private const int WM_CHAR = 0x0102;          // 输入字符消息（键盘输入的，输入法输入的好像不是这个消息）
        private const int WM_PASTE = 0x0302;         // 程序发送此消息给editcontrol或combobox从剪贴板中得到数据

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m) 
        { 
            switch(m.Msg)
            { 
                case WM_CHAR:
                    System.Console.WriteLine(m.WParam);
                    bool isSign = ((int)m.WParam == 45);
                    bool isNum = ((int)m.WParam >= 48) && ((int)m.WParam <= 57);
                    bool isBack = (int)m.WParam == (int)Keys.Back;
                    bool isDelete = (int)m.WParam == (int)Keys.Delete; //实际上这是一个"."键
                    bool isCtr = ((int)m.WParam == 24) || ((int)m.WParam == 22) || ((int)m.WParam == 26) ||((int)m.WParam == 3);

                    if( isNum || isBack || isCtr)
                    {
                        base.WndProc (ref m);
                    }
                    if (isSign)
                    {
                        if (this.SelectionStart!=0)
                        {
                            break;
                        }
                        base.WndProc (ref m);
                        break;
                    }
                    if (isDelete)
                    {
                        if (this.Text.IndexOf(".")<0)
                        {
                            base.WndProc (ref m);
                        }
                    }
                    if ((int)m.WParam == 1)
                    {
                        this.SelectAll();
                    }
                    break;

                case WM_PASTE:
                    IDataObject iData = Clipboard.GetDataObject();  // 取剪贴板对象

                    if(iData.GetDataPresent(DataFormats.Text))      // 判断是否是Text
                    {
                        string str = (string)iData.GetData(DataFormats.Text);   // 取数据
                        if (MatchNumber(str)) 
                        {
                            base.WndProc (ref m);
                            break;
                        }
                    }
                    m.Result = (IntPtr)0;   // 不可以粘贴
                    break;

                default:
                    base.WndProc (ref m);
                    break;
            }
        }

        private bool MatchNumber(string ClipboardText)
        {
            int index=0;
            string strNum = "-0.123456789";

            index = ClipboardText.IndexOf(strNum[0]);
            if (index>=0)
            {
                if (index>0)
                {
                    return false;
                }
                index = this.SelectionStart;
                if (index>0)
                {
                    return false;
                }
            }

            index = ClipboardText.IndexOf(strNum[2]);
            if (index!=-1)
            {
                index = this.Text.IndexOf(strNum[2]);
                if (index!=-1)
                {
                    return false;
                }
            }

            for(int i=0; i<ClipboardText.Length; i++)
            {
                index = strNum.IndexOf(ClipboardText[i]);
                if (index <0)
                {
                    return false;
                }
            }
            return true;
        }
    }

    // LiYoubing 20110627
    /// <summary>
    /// 只允许输入数字的组合框控件
    /// </summary>
    public class NumberComboBox : ComboBox
    {
        private IntPtr _editHandle = IntPtr.Zero;
        /// <summary>
        /// Editor窗口句柄
        /// </summary>
        public IntPtr EditHandle
        {
            get { return _editHandle; }
            set { _editHandle = value; }
        }

        private EditNativeWindow _editNativeWindow = null;
        /// <summary>
        /// Editor窗口的NativeWindow对象
        /// </summary>
        public EditNativeWindow EditNativeWindow
        {
            get { return _editNativeWindow; }
            set { _editNativeWindow = value; }
        }

        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr GetWindow(IntPtr hwnd, int wFlag);

        private const int GW_CHILD = 5;

        /// <summary>
        /// 重载句柄创建完成事件 保存窗体句柄和窗体对象的引用
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            _editHandle = GetWindow(Handle, GW_CHILD);
            if (_editHandle != IntPtr.Zero)
            {
                _editNativeWindow = new EditNativeWindow(this);
            }
        }

        /// <summary>
        /// 重载句柄销毁完成事件 销毁窗体对象释放资源
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            if (_editNativeWindow != null)
            {
                _editNativeWindow.Dispose();
                _editNativeWindow = null;
            }
        }
    }

    // LiYoubing 20110627
    /// <summary>
    /// Editor窗口的NativeWindow类
    /// </summary>
    public class EditNativeWindow : NativeWindow, IDisposable
    {
        private NumberComboBox _owner;

        /// <summary>
        /// 构造函数，给传入的NumberComboBox对象构造一个Native窗口
        /// </summary>
        /// <param name="owner"></param>
        public EditNativeWindow(NumberComboBox owner)
            : base()
        {
            _owner = owner;
            base.AssignHandle(owner.EditHandle);
        }

        private const int WM_PASTE = 0x0302;

        private const int WM_CHAR = 0x0102;

        /// <summary>
        /// 拦截NativeWindow的WM_PASTE和WM_CHAR消息
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_PASTE:
                    IDataObject iData = Clipboard.GetDataObject();
                    if (iData.GetDataPresent(DataFormats.Text))//粘贴的内容是否是文本
                    {
                        string str;
                        str = (String)iData.GetData(DataFormats.Text);
                        if (System.Text.RegularExpressions.Regex.IsMatch(str, @"^(\d{1,})$")) //文本内容是不是数字
                        {
                            break;
                        }
                    }
                    m.Result = IntPtr.Zero;
                    return;
                case WM_CHAR:
                    int keyChar = m.WParam.ToInt32();
                    bool charOk = (keyChar > 47 && keyChar < 58) ||   //数字
                         keyChar == 8 ||                              //退格
                         keyChar == 3 || keyChar == 22 || keyChar == 24;//拷贝,粘贴,剪切
                    if (!charOk)
                    {
                        m.WParam = IntPtr.Zero;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        #region IDisposable 成员
        /// <summary>
        /// 销毁对象
        /// </summary>
        public void Dispose()
        {
            base.ReleaseHandle();
            _owner = null;
        }
        #endregion
    }
    #endregion
}