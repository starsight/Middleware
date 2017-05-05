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
using System.Windows.Shapes;

namespace MiddleWare.Views
{
    /// <summary>
    /// Query_PLdetail.xaml 的交互逻辑
    /// </summary>
    public partial class Query_PLdetail : UserControl
    {
        public Query_PLdetail()
        {
            InitializeComponent();
        }
        public void show(Query.single_record record)
        {
            PLtype.Text = record.type;
            PLsample_id.Text = record.sample_ID;
            PLaap.Text = record.test_aap;
            PLtest_time.Text = record.test_Time;
            PLsample_kind.Text = record.test_kind;
            PLdevice.Text = record.test_Device;
            PLbarcode.Text = record.barcode;
        }
    }
}
