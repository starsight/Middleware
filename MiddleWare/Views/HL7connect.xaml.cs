using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                this.textbox_hl7ip.Text = AppConfig.GetAppConfig("HL7IP");
            }

            if (AppConfig.GetAppConfig("HL7PORT") != null)
            {
                this.textbox_hl7port.Text = AppConfig.GetAppConfig("HL7PORT");
            }
        }
    }
}
