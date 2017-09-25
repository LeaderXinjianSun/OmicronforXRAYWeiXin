using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BingLibrary.hjb;
using BingLibrary.hjb.Intercepts;
using System.ComponentModel.Composition;
using MESBadMarkReflection;
using OfficeOpenXml;
using System.IO;
using System.Windows;

namespace OmicronforXRAYWeiXin.ViewModel
{
    [BingAutoNotify]
    public class MainDataContext : DataSource
    {
        #region 属性
        public virtual string MsgText { set; get; } = "";
        public virtual string BarcodeString1 { set; get; }
        public virtual string BarcodeString2 { set; get; }
        public virtual string HomePageVisibility { set; get; } = "Visible";
        public virtual string ParameterPageVisibility { set; get; } = "Collapsed";
        public virtual string ScanCom { set; get; }
        public virtual string SerialPortCom { set; get; }
        public virtual bool IsPLCConnect { set; get; } = false;
        public virtual bool IsScanConnect { set; get; } = false;
        public virtual string MACString { set; get; }
        #endregion
        #region 变量
        private string MessageStr = "";
        private XinjiePlc XinjiePLC;
        private string iniParameterPath = System.Environment.CurrentDirectory + "\\Parameter.ini";
        ReflectionData reflectionData = new ReflectionData();
        private string CoorExcelFile = System.Environment.CurrentDirectory + "\\坐标.xlsx";
        //private double[,] CoorExcel = new double[2, 20];
        private List<Coor> AxisCoorList= new List<Coor>();
        #endregion
        #region 构造函数
        public MainDataContext()
        {
            ScanCom = Inifile.INIGetStringValue(iniParameterPath, "Com", "ScanCom", "COM1");
            SerialPortCom = Inifile.INIGetStringValue(iniParameterPath, "Com", "SerialPortCom", "COM1");
            MACString = Inifile.INIGetStringValue(iniParameterPath, "MES", "MAC", "14-B3-1F-02-2D-83");
            Scan.ini(ScanCom);
            ImportDatefromExcel();
            
            //Console.WriteLine(Convert.ToString(16, 16));
        }
        #endregion
        #region 功能与方法
        public void FunctionTest()
        {
            CalcCoor();
            //XinjiePLC.WriteD(4200, "-123");
        }
        public void ScanAction()
        {
            Scan.GetBarCode(ScanActionCallback);
        }
        private void ScanActionCallback(string str)
        {
            if (str != "Error")
            {
                BarcodeString1 = str;
                Entity entity = new Entity();
                //entity.BarCode = "PJ6RN178V08SP";
                entity.BarCode = str;
                entity.MachineName = "";
                entity.MAC = MACString;
                entity.OperatorName = "";
                entity.Panel = "5";
                var aa = reflectionData.GetPanelInfo(entity);
                if (aa[0] == "0")
                {
                    MsgText = AddMessage("扫码成功：" + str);
                    XinjiePLC.SetM(307, true);
                }
                else
                {
                    MsgText = AddMessage("查询失败：" + aa[1]);
                }
                
            }
            else
            {
                MsgText = AddMessage("扫码失败：" + str);
            }           
        }
        public void ChooseHomePage()
        {
            HomePageVisibility = "Visible";
            ParameterPageVisibility = "Collapsed";
            Inifile.INIWriteValue(iniParameterPath, "MES", "MAC", MACString);
        }
        public void ChooseParameterPage()
        {
            HomePageVisibility = "Collapsed";
            ParameterPageVisibility = "Visible";
        }
        private string AddMessage(string str)
        {
            string[] s = MessageStr.Split('\n');
            if (s.Length > 1000)
            {
                MessageStr = "";
            }
            MessageStr += "\n" + System.DateTime.Now.ToString() + " " + str;
            return MessageStr;
        }
        private bool ImportDatefromExcel()
        {
            #region check file if exists
            FileStream stream = null;
            try
            {
                //stream = new FileStream(dialog.FileName, FileMode.Open);
                stream = File.OpenRead(CoorExcelFile);
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            #endregion
            using (stream)
            {
                ExcelPackage package = new ExcelPackage(stream);
                ExcelWorksheet sheet = package.Workbook.Worksheets[1];
                #region get last row index
                int lastRow = sheet.Dimension.End.Row;
                while (sheet.Cells[lastRow, 1].Value == null)
                {
                    lastRow--;
                }
                #endregion
                for (int i = 0; i < lastRow - 1; i++)
                {
                    try
                    {
                        Coor myCoor;
                        myCoor.x = double.Parse(sheet.Cells[i + 2, 2].Value.ToString());
                        myCoor.y = double.Parse(sheet.Cells[i + 2, 3].Value.ToString());
                        AxisCoorList.Add(myCoor);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return false;
                    }
                    
                }
                return true;
            }

        }
        
        public void CalcCoor()
        {
            //0.0075
            if (IsPLCConnect)
            {
                double x0 = XinjiePLC.ReadD(4100);
                double y0 = XinjiePLC.ReadD(4102);
                for (int i = 0; i < AxisCoorList.Count; i++)
                {
                    double new_x = AxisCoorList[i].x / 0.0075 + x0;
                    double new_y = AxisCoorList[i].y / 0.0075 + y0;
                    //XinjiePLC.WriteD(4200, "-123");
                    XinjiePLC.WriteD(5000 + i * 2, Convert.ToInt32(new_x).ToString());
                    XinjiePLC.WriteD(5200 + i * 2, Convert.ToInt32(new_y).ToString());
                }
            }
            else
            {
                MsgText = AddMessage("无法计算：PLC未连接");
            }
        }
        #endregion
        #region PLC
        [Initialize]
        public void PLCWork()
        {
            bool M305 = false, m305 = false;
            while (true)
            {
                System.Threading.Thread.Sleep(100);
                if (!IsPLCConnect)
                {
                    if (XinjiePLC != null)
                    {
                        XinjiePLC.DisConnectPLC();
                    }
                    try
                    {
                        XinjiePLC = new XinjiePlc(SerialPortCom, 19200, System.IO.Ports.Parity.Even, 8, System.IO.Ports.StopBits.One);
                        IsPLCConnect = XinjiePLC.ConnectPLC();
                    }
                    catch
                    {

                    }
                    if (IsPLCConnect)
                    {
                        IsPLCConnect = XinjiePLC.ReadM(24576);
                    }
                }
                else
                {
                    IsPLCConnect = XinjiePLC.ReadM(24576);
                    M305 = XinjiePLC.ReadM(305);
                    if (m305 != M305)
                    {
                        m305 = M305;
                        if (M305)
                        {
                            XinjiePLC.SetM(306,false);
                            XinjiePLC.SetM(307, false);
                            ScanAction();
                            XinjiePLC.SetM(306, true);
                        }
                        else
                        {
                            XinjiePLC.SetM(306, false);
                        }
                    }
                }
            }
        }
        [Initialize]
        public void UpdateUI()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(200);
                IsScanConnect = Scan.State;
            }
        }
        #endregion
    }
    //class MyBadMarkReflection : MESBadMarkReflection.BadMarkReflection
    //{
    //    string[] BadMarkReflection.BadMarkList(Entity entity)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
    class VMManager
    {
        [Export(MEF.Contracts.Data)]
        [ExportMetadata(MEF.Key, "md")]
        MainDataContext md = MainDataContext.New<MainDataContext>();
    }
    struct Coor
    {
        public double x;
        public double y;
    }
}
