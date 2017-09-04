using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BingLibrary.hjb;
using BingLibrary.hjb.Intercepts;
using System.ComponentModel.Composition;

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
        #endregion
        #region 变量
        private string MessageStr = "";
        private XinjiePlc XinjiePLC;
        private string iniParameterPath = System.Environment.CurrentDirectory + "\\Parameter.ini";
        #endregion
        #region 构造函数
        public MainDataContext()
        {
            ScanCom = Inifile.INIGetStringValue(iniParameterPath, "Com", "ScanCom", "COM1");
            SerialPortCom = Inifile.INIGetStringValue(iniParameterPath, "Com", "SerialPortCom", "COM1");
            Scan.ini("ScanCom");
        }
        #endregion
        #region 功能与方法
        public void ScanAction()
        {
            Scan.GetBarCode(ScanActionCallback);
        }
        private void ScanActionCallback(string str)
        {
            if (str != "Error")
            {
                BarcodeString1 = str;
                MsgText = AddMessage("扫码成功：" + str);
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
        #endregion
        #region PLC
        [Initialize]
        public void PLCWork()
        {
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
    class VMManager
    {
        [Export(MEF.Contracts.Data)]
        [ExportMetadata(MEF.Key, "md")]
        MainDataContext md = MainDataContext.New<MainDataContext>();
    }
}
