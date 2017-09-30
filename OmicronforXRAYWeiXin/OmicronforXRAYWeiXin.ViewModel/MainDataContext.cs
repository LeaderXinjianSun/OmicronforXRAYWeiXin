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
        public virtual string XYCoorStr { set; get; } = "0 , 0";
        public virtual int LiaoHaoSelectedIndex { set; get; } = 0;
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
            LiaoHaoSelectedIndex = int.Parse(Inifile.INIGetStringValue(iniParameterPath, "LiaoHao", "SelectIndex", "0"));
            Scan.ini(ScanCom);
            //ImportDatefromExcel();
            
            //Console.WriteLine(Convert.ToString(16, 16));
        }
        #endregion
        #region 功能与方法
        public void FunctionTest()
        {
            //CalcCoor();
            //XinjiePLC.WriteD(4200, "-123");

            //XinjiePLC.SetMultiM("M2000","01010010111");
            Entity entity = new Entity();
            entity.BarCode = "PJ6RN178V08SP";
            //entity.BarCode = str;
            entity.MachineName = "";
            entity.MAC = MACString;
            entity.OperatorName = "";
            entity.Panel = "0";
            var aa = reflectionData.GetPanelInfo(entity);
        }
        public void ScanAction()
        {
            Scan.GetBarCode(ScanActionCallback);
        }
        private void ScanActionCallback(string str)
        {
            try
            {
                if (str != "Error")
                {
                    BarcodeString1 = str;
                    Entity entity = new Entity();
                    //entity.BarCode = "PJ6RN178V08SP";
                    //entity.BarCode = str;
                    //entity.MachineName = "";
                    entity.MAC = MACString;
                    //entity.OperatorName = str;
                    entity.Panel = str;
                    string[] aa = reflectionData.GetPanelInfo(entity);
                    if (aa[0] == "0")
                    {

                        string ss = "";
                        for (int i = 1; i < aa.Length; i++)
                        {
                            if (aa[i] == "OK")
                            {
                                ss += "1";
                            }
                            else
                            {
                                ss += "0";
                            }
                        }
                        XinjiePLC.SetMultiM("M2000", ss);
                        MsgText = AddMessage("扫码成功：" + str + " " + ss);
                        XinjiePLC.SetM(307, true);
                        XinjiePLC.SetM(306, true);
                    }
                    else
                    {
                        MsgText = AddMessage("查询失败：" + aa[1]);
                        XinjiePLC.SetM(307, false);
                        XinjiePLC.SetM(306, true);
                    }

                }
                else
                {
                    MsgText = AddMessage("扫码失败：" + str);
                    XinjiePLC.SetM(307, false);
                    XinjiePLC.SetM(306, true);
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
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
                ExcelWorksheet sheet = package.Workbook.Worksheets[1 + LiaoHaoSelectedIndex];
                #region get last row index
                int lastRow = sheet.Dimension.End.Row;
                while (sheet.Cells[lastRow, 1].Value == null)
                {
                    lastRow--;
                }
                #endregion
                AxisCoorList.Clear();
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
                        MessageBox.Show(ex.Message + i.ToString());
                        return false;
                    }
                    
                }
                return true;
            }

        }
        
        public void CalcCoor()
        {
            //0.0075
            ImportDatefromExcel();
            if (IsPLCConnect)
            {
                double x0 = XinjiePLC.ReadD(4100 + LiaoHaoSelectedIndex * 4);
                double y0 = XinjiePLC.ReadD(4102 + LiaoHaoSelectedIndex * 4);
                XYCoorStr = (x0 * 0.0075).ToString() + " , " + (y0 * 0.0075).ToString();
                for (int i = 0; i < AxisCoorList.Count; i++)
                {
                    double new_x = AxisCoorList[i].x / 0.0075 + x0;
                    double new_y = AxisCoorList[i].y / 0.0075 + y0;
                    //XinjiePLC.WriteD(4200, "-123");
                    XinjiePLC.WriteD(5000 + i * 2, Convert.ToInt32(new_x).ToString());
                    XinjiePLC.WriteD(5200 + i * 2, Convert.ToInt32(new_y).ToString());
                }
                XinjiePLC.WriteW(4118, AxisCoorList.Count.ToString());
                MessageBox.Show("计算完成");
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
            bool M309 = false, m309 = false;
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
                        XinjiePLC.SetM(306, false);
                        if (M305)
                        {
                            
                            
                            ScanAction();
                            //System.Threading.Thread.Sleep(100);
                            
                        }
                    }
                    M309 = XinjiePLC.ReadM(309);
                    if (m309 != M309)
                    {
                        m309 = M309;
                        if (m309)
                        {
                            BarcodeString2 = BarcodeString1;
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
