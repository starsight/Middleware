using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MiddleWare.Views
{
    /// <summary>
    /// HL7connect.xaml 的交互逻辑
    /// </summary>
    public partial class HL7connect : UserControl
    {
        public HL7connect()
        {
            InitializeComponent();

            if (AppConfig.GetAppConfig("HL7IP") != null) 
            {
                //加入是否为准确文件判断
                string hl7IP = AppConfig.GetAppConfig("HL7IP");
                Regex regex = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
                if (regex.IsMatch(hl7IP)) 
                {
                    //匹配IP成功
                    this.textbox_hl7ip.Text = hl7IP;
                }
            }

            if (AppConfig.GetAppConfig("HL7PORT") != null)
            {
                string hl7PORT = AppConfig.GetAppConfig("HL7PORT");
                Regex regex = new Regex(@"^([0-9]|[1-9]\d|[1-9]\d{2}|[1-9]\d{3}|[1-5]\d{4}|6[0-5]{2}[0-3][0-5])$");
                if (regex.IsMatch(hl7PORT)) 
                {
                    //匹配PORT成功
                    this.textbox_hl7port.Text = hl7PORT;
                }
            }
        }
    }
}
