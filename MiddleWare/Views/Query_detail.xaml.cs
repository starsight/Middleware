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
using MahApps.Metro.Controls;
using MiddleWare.Views;
using LiveCharts;
using LiveCharts.Wpf;

namespace MiddleWare.Views
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    
    public partial class Query_detail : Window
    {
        DetailInfoPrintDS DsPrintDetail;
        DetailInfoPrintPL PlPrintDetail;
        private bool Device;//0代表DS,1代表PL

        public Query_detail(Query.single_record record)
        {
            InitializeComponent();

            DetailsShow(record);
            DetailPrint(record);
        }

        private void DetailsShow(Query.single_record record)
        {
            List<data_detailSource> result = new List<data_detailSource>();
            int len = record.result.Count;
            if (record.test_Device == "DS_800" || record.test_Device == "DS_400")
            {
                PL_data.Visibility = Visibility.Collapsed;
                PL_chart.Visibility = Visibility.Collapsed;
                DS_data.Visibility = Visibility.Visible;
                DS_data.show(record);
                //这个函数里面是直接赋值的,非绑定
            }
            if (record.test_Device == "PL_12" || record.test_Device == "PL_16")
            {
                DS_data.Visibility = Visibility.Collapsed;
                PL_data.Visibility = Visibility.Visible;
                PL_data.show(record);
                PL_chart.Visibility = Visibility.Visible;
                PL_chart.show(record);
            }
            foreach (var name in record.result)
            {
                if (name.item == "PAC1" || name.item == "PAC2")
                    continue;
                if (name.item == "PAC3" || name.item == "PAC4")
                    continue;
                if (name.item == "PAC5" || name.item == "PAC6")
                    continue;
                if (name.item == "PAC7" || name.item == "PAC8")
                    continue;
                if (name.item == "PACBit" || name.item == "PLTHist" || name.item == "RBCHist")
                    continue;
                data_detailSource res = new data_detailSource();
                res.item = name.item;
                res.full_item = name.fullname;
                res.result = name.result;
                res.unit = name.unit;
                res.normal_high = name.normal_high;
                res.normal_low = name.normal_low;
                res.indicate = name.indicate;
                result.Add(res);
            }
            this.Query_detail_datagrid.ItemsSource = result;//绑定进去
        }

        private void DetailPrint(Query.single_record record)
        {
            if (record.test_Device == "DS_800" || record.test_Device == "DS_400")
            {
                Device = false;
                DsPrintDetail = new DetailInfoPrintDS
                {
                    DSTYPE = record.type,
                    DSSAMPLE_ID = record.sample_ID,
                    DSDOCTOR = record.doctor,
                    DSPATIENT_ID = record.patiennt_ID,
                    DSTEST_TIME = record.test_Time,
                    DSDEPARTMENT = record.department,
                    DSSAMPLE_KIND = record.test_kind,
                    DSAREA = record.area,
                    DSSEX = record.patient_Sex,
                    DSDEVICE = record.test_Device,
                    DSBED = record.bed
                };
                foreach(Query.single_result single in record.result)
                {
                    DsPrintDetail.TableDetails.Add(new data_detailSource
                    {
                        item = single.item,
                        full_item = single.fullname,
                        result = single.result,
                        unit = single.unit,
                        normal_low = single.normal_low,
                        normal_high = single.normal_high,
                        indicate = single.indicate
                    });
                }
                
            }
            else if(record.test_Device == "PL_12" || record.test_Device == "PL_16")
            {
                Device = true;
                PlPrintDetail = new DetailInfoPrintPL
                {
                    PLTYPE = record.type,
                    PLSAMPLE_ID = record.sample_ID,
                    PLAAP = record.test_aap,
                    PLTEST_TIME = record.test_Time,
                    PLSAMPLE_KIND = record.test_kind,
                    PLDEVICE = record.test_Device,
                    PLBARCODE = record.barcode
                };

                foreach (Query.single_result single in record.result)
                {
                   if (single.item == "PAC1" || single.item == "PAC2")
                        continue;
                    if (single.item == "PAC3" || single.item == "PAC4")
                        continue;
                    if (single.item == "PAC5" || single.item == "PAC6")
                        continue;
                    if (single.item == "PAC7" || single.item == "PAC8")
                        continue;
                    if (single.item == "PACBit" || single.item == "PLTHist" || single.item == "RBCHist")
                        continue;
                    PlPrintDetail.TableDetails.Add(new data_detailSource
                    {
                        item = single.item,
                        full_item = single.fullname,
                        result = single.result,
                        unit = single.unit,
                        normal_low = single.normal_low,
                        normal_high = single.normal_high,
                        indicate = single.indicate
                    });
                }
            }
        }

        private void button_printpreview_Click(object sender, RoutedEventArgs e)
        {
            PrintPreview preveiew;
            if (!Device)
            {
                //DS
                preveiew = new PrintPreview("Views/DetailDocumentDS.xaml", DsPrintDetail, new DetailDocumentRendererDS());
            }
            else
            {
                //PL
                PlPrintDetail.PLPAC = PL_chart.getPacImage();
                preveiew = new PrintPreview("Views/DetailDocumentPL.xaml", PlPrintDetail, new DetailDocumentRendererPL());
            }
            preveiew.Owner = this;
            preveiew.ShowInTaskbar = false;
            preveiew.ShowDialog();
        }
    }

    public class DetailInfoPrintDS
    {
        public string DSTYPE { get; set; }
        public string DSSAMPLE_ID { get; set; }
        public string DSDOCTOR { get; set; }
        public string DSPATIENT_ID { get; set; }
        public string DSTEST_TIME { get; set; }
        public string DSDEPARTMENT { set; get; }
        public string DSFIRST_NAME { set; get; }
        public string DSSAMPLE_KIND { get; set; }
        public string DSAREA { get; set; }
        public string DSSEX { get; set; }
        public string DSDEVICE { get; set; }
        public string DSBED { get; set; }

        public List<data_detailSource> TableDetails = new List<data_detailSource>();
    }

    public class DetailInfoPrintPL
    {
        public string PLTYPE { get; set; }
        public string PLSAMPLE_ID { get; set; }
        public string PLAAP { get; set; }
        public string PLTEST_TIME { get; set; }
        public string PLSAMPLE_KIND { get; set; }
        public string PLDEVICE { get; set; }
        public string PLBARCODE { get; set; }
        public BitmapImage PLPAC { get; set; }
        public SeriesCollection SeriesCollect { get; set; }

        public List<data_detailSource> TableDetails = new List<data_detailSource>();
    }

    public class data_detailSource
    {
        public string item { set; get; }
        public string full_item { set; get; }
        public string result { set; get; }
        public string unit { set; get; }
        public string normal_low { set; get; }
        public string normal_high { set; get; }
        public string indicate { set; get; }
    }
}
