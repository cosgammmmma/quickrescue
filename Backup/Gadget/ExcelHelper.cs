using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using URWPGSim2D.Common;

namespace URWPGSim2D.Gadget
{
    public class ExcelHelper
    {
        public ExcelHelper()
        {
            CreatFishInfoExcel();
        }

        MSExcel.Application _excelApp;   // Excel应用程序变量
        MSExcel.Workbook _excelDoc;      // Excel文档变量

        // 20101129
        /// <summary>
        /// 创建Excel文件用于保存动态对象（仿真机器鱼和仿真水球）运动学参数
        /// 文件保存于当前程序目录下的Report子目录，以当前系统时间命名
        /// </summary>
        public void CreatFishInfoExcel()
        {
            if (MyMission.Instance().TeamsRef == null) return;

            string strTargetExcelPath = Application.StartupPath + "\\Report\\"; // Report目录
            string strTargetExcelFullName = strTargetExcelPath + "Report" 
                + string.Format(" {0:0000}{1:00}{2:00} {3:00}{4:00}{5:00}.xls", 
                DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 
                DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            try
            {
                if (!System.IO.Directory.Exists(strTargetExcelPath))
                {
                    //Excel信息保存目录不存在则新建
                    System.IO.Directory.CreateDirectory(strTargetExcelPath);
                }
            }
            catch
            { 
            }

            _excelApp = new MSExcel.ApplicationClass();         // 启动MSExcel应用服务器
            _excelDoc = _excelApp.Workbooks.Add(Missing.Value); // 新建Excel工作薄

            int defaultSheets = _excelDoc.Sheets.Count;         // 默认工作表数量

            MyMission myMission = MyMission.Instance();
            int fishes = 0;
            for (int i = 0; i < myMission.TeamsRef.Count; i++)
            {
                for (int j = 0; j < myMission.TeamsRef[i].Fishes.Count; j++)
                {
                    // 添加新的工作表
                    _excelApp.Worksheets.Add(_excelDoc.Sheets[fishes + j + 1], Missing.Value, Missing.Value, Missing.Value);

                    // 使用第n个工作表作为插入第n条仿真机器鱼信息的工作表
                    MSExcel.Worksheet tmpSheet = (MSExcel.Worksheet)_excelDoc.Sheets[fishes + j + 1];
                    tmpSheet.Name = string.Format("Team{0}Fish{1}", i + 1, j + 1);  // 重命名工作表
                    //tmpSheet.Columns.AutoFit();   // 列宽自适应
                    tmpSheet.Rows.AutoFit();        // 行高自适应
                    tmpSheet.StandardWidth = 10.00; // 工作表中每个单元格的宽度
                    tmpSheet.Rows.HorizontalAlignment = MSExcel.XlHAlign.xlHAlignCenter;    // 设置全部行即整张表所有单元格水平居中
                    tmpSheet.Rows.VerticalAlignment = MSExcel.XlVAlign.xlVAlignCenter;      // 设置全部行即整张表所有单元格垂直居中
                    tmpSheet.Rows.WrapText = true;                                 // 设置全部行即整张表所有单元格自动换行

                    MSExcel.Range headRange = tmpSheet.get_Range("A1", "L1");   // 选中第1行
                    headRange.Merge(Missing.Value);     // 合并单元格
                    headRange.Font.Bold = true;         // 加粗
                    headRange.Value2 = tmpSheet.Name;   // 第1行为表名
                    

                    // 设置从A2到L2的单元格范围，分别在每个单元格中添加仿真机器鱼运动学参数名称
                    MSExcel.Range infoTypeRange = tmpSheet.get_Range("A2", "L2");
                    object[] objInfoType = { "N.O.", "Time", "PositionX", "PositionZ", "Direction", 
                                               "Velocity", "Velocity Direction", "Angular Velocity", 
                                               "Velocity Tactic", "Turn Around Tactic", 
                                               "Target Velocity Tactic", "Target Turn Around Tactic" };
                    infoTypeRange.Value2 = objInfoType;
                }
                fishes += myMission.TeamsRef[i].Fishes.Count;
            }

            for (int i = defaultSheets; i > 0; i--)
            {
                ((MSExcel.Worksheet)_excelDoc.Sheets[i + fishes]).Delete(); // 删除未使用的默认工作表
            }

            ((Microsoft.Office.Interop.Excel._Worksheet)_excelDoc.Sheets[1]).Activate();    // 激活第1张工作表

            // 将_excelDoc文档对象的内容保存为xls文档
            _excelApp.DisplayAlerts = false;    // 不弹出是否保存的提示
            _excelDoc.SaveAs(strTargetExcelFullName, MSExcel.XlFileFormat.xlWorkbookNormal,
                Missing.Value, Missing.Value, Missing.Value, Missing.Value,
                MSExcel.XlSaveAsAccessMode.xlNoChange, MSExcel.XlSaveConflictResolution.xlLocalSessionChanges,
                Missing.Value, Missing.Value, Missing.Value, Missing.Value);
        }

        /// <summary>
        /// 将仿真机器鱼的运动学参数写入Excel文档
        /// </summary>
        /// <param name="num"></param>
        public void FishInfoWriteToExcel()
        {
            MyMission myMission = MyMission.Instance();
            int fishes = 0;
            for (int i = 0; i < myMission.TeamsRef.Count; i++)
            {
                for (int j = 0; j < myMission.TeamsRef[i].Fishes.Count; j++)
                {
                    RoboFish f = myMission.TeamsRef[i].Fishes[j];
                    object[] objData = new Object[12];   // 仿真机器鱼运动参数记录数组
                    MSExcel.Worksheet sheet = (MSExcel.Worksheet)_excelDoc.Sheets[fishes + j + 1];
                    int iTmp = sheet.UsedRange.Cells.Rows.Count + 1;    // 获取当前行数 + 1 
                    objData[0] = iTmp - 2;
                    objData[1] = string.Format("{0:00}:{1:00}:{2:00}", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

                    // 第3/4列第fishes + j个单元格的值分别为仿真机器鱼的X/Z坐标
                    objData[2] = (int)(myMission.TeamsRef[i].Fishes[j].PositionMm.X);
                    objData[3] = (int)(myMission.TeamsRef[i].Fishes[j].PositionMm.Z);

                    //第5列第fishes + j个单元格的值为机器鱼的鱼体方向
                    objData[4] = string.Format("{0:0.0000}", myMission.TeamsRef[i].Fishes[j].BodyDirectionRad);

                    //第6列第fishes + j个单元格的值为机器鱼的速度
                    objData[5] = string.Format("{0:0.0000}", myMission.TeamsRef[i].Fishes[j].VelocityMmPs);

                    //第7列第fishes + j个单元格的值为机器鱼的速度方向
                    objData[6] = string.Format("{0:0.0000}", myMission.TeamsRef[i].Fishes[j].VelocityDirectionRad);

                    //第8列第fishes + j个单元格的值为机器鱼的角速度
                    objData[7] = string.Format("{0:0.0000}", myMission.TeamsRef[i].Fishes[j].AngularVelocityRadPs);

                    //第9/10列第fishes + j个单元格的值为机器鱼的速度档位/方向档位
                    objData[8] = myMission.TeamsRef[i].Fishes[j].Tactic.VCode;
                    objData[9] = myMission.TeamsRef[i].Fishes[j].Tactic.TCode;

                    //第11/12列第fishes + j个单元格的值为机器鱼的目标决策速度档位/方向档位
                    objData[10] = myMission.TeamsRef[i].Fishes[j].TargetTactic.VCode;
                    objData[11] = myMission.TeamsRef[i].Fishes[j].TargetTactic.TCode;

                    // get target range (cells of one row) for writing the info record above
                    MSExcel.Range range = sheet.get_Range(string.Format("A{0}", iTmp), string.Format("L{0}", iTmp));

                    // write the info record above to the target range
                    range.Value2 = objData;
                }
                fishes += myMission.TeamsRef[i].Fishes.Count;
            }
        }

        /// <summary>
        /// 关闭此Excel文档并退出Excel应用程序
        /// </summary>
        public void CloseExcel()
        {
            if (_excelDoc != null)
            {
                // 关闭_excelDoc文档对象
                _excelDoc.Close(true, Missing.Value, Missing.Value);
                _excelDoc = null;
            }

            if (_excelApp != null)
            {
                // 关闭_excelApp组件对象
                _excelApp.Quit();
                _excelApp = null;
            }

            // 关闭当前对象打开的Excel进程
            KillSpecialExcel();
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);  
        /// <summary>
        /// 关闭当前对象打开的Excel进程
        /// </summary>
        private void KillSpecialExcel()
        {
            try
            {
                if (_excelApp != null)
                {
                    int lpdwProcessId;
                    GetWindowThreadProcessId(new IntPtr(_excelApp.Hwnd), out lpdwProcessId);
                    System.Diagnostics.Process.GetProcessById(lpdwProcessId).Kill();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Delete Excel Process Error:" + ex.Message);
            }
        } 
    }
}