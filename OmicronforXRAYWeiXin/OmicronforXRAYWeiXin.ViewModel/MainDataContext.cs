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
        #endregion
        #region 变量
        private string MessageStr = "";
        #endregion
        #region 功能与方法
        public void ScanAction()
        {
            MsgText = AddMessage("Scan");
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
    }
    class VMManager
    {
        [Export(MEF.Contracts.Data)]
        [ExportMetadata(MEF.Key, "md")]
        MainDataContext md = MainDataContext.New<MainDataContext>();
    }
}
