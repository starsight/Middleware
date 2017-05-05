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
    /// Query_DSdetail.xaml 的交互逻辑
    /// </summary>
    public class dsdata_detailSource
    {
        public string test_type { set; get; }
        public string sample_id { set; get; }
        public string test_doctor { set; get; }
        public string patient_id { set; get; }
        public string test_time { set; get; }
        public string department { set; get; }
        public string patient_name { set; get; }
        public string test_kind { set; get; }
        public string area { set; get; }
        public string patient_sex { set; get; }
        public string test_device { set; get; }
        public string bed { set; get; }
    }
    public partial class Query_DSdetail : UserControl
    {
        public Query_DSdetail()
        {
            InitializeComponent();

        }
        public void show(Query.single_record record)
        {
            DStype.Text = record.type;
            DSsample_id.Text = record.sample_ID;
            DSdoctor.Text = record.doctor;
            DSpatient_id.Text = record.patiennt_ID;
            DStest_time.Text = record.test_Time;
            DSdepartment.Text = record.department;
            DSfirst_name.Text = record.patient_Name;
            DSsample_kind.Text = record.test_kind;
            DSarea.Text = record.area;
            DSsex.Text = record.patient_Sex;
            DSdevice.Text = record.test_Device;
            DSbed.Text = record.bed;
        }
    }
}
