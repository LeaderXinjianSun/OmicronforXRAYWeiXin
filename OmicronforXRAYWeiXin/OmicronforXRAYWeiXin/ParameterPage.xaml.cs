using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using BingLibrary.hjb;

namespace OmicronforXRAYWeiXin
{
    /// <summary>
    /// ParameterPage.xaml 的交互逻辑
    /// </summary>
    public partial class ParameterPage : UserControl
    {
        private string iniParameterPath = System.Environment.CurrentDirectory + "\\Parameter.ini";
        public ParameterPage()
        {
            InitializeComponent();
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var validComNames = SerialPort.GetPortNames();
            foreach (var comName in validComNames)
            {
                if (!Com.Items.Contains(comName))
                    Com.Items.Add(comName);
            }
            List<string> toRemove = new List<string>();
            foreach (string addedName in Com.Items)
            {
                if (!validComNames.Contains(addedName))
                    toRemove.Add(addedName);
            }
            foreach (string remove in toRemove)
            {
                Com.Items.Remove(remove);
            }
        }

        private void Com1_DropDownOpened(object sender, EventArgs e)
        {
            var validComNames = SerialPort.GetPortNames();
            foreach (var comName in validComNames)
            {
                if (!Com1.Items.Contains(comName))
                    Com1.Items.Add(comName);
            }
            List<string> toRemove = new List<string>();
            foreach (string addedName in Com1.Items)
            {
                if (!validComNames.Contains(addedName))
                    toRemove.Add(addedName);
            }
            foreach (string remove in toRemove)
            {
                Com1.Items.Remove(remove);
            }
        }

        private void Com1_DropDownClosed(object sender, EventArgs e)
        {
            Inifile.INIWriteValue(iniParameterPath, "Com", "ScanCom", Com1.Text);
        }

        private void Com_DropDownClosed(object sender, EventArgs e)
        {
            Inifile.INIWriteValue(iniParameterPath, "Com", "SerialPortCom", Com.Text);
        }
    }
}
