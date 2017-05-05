using System;

using System.Windows.Controls;

using System.Windows.Media;
using System.Windows.Media.Imaging;

using LiveCharts;
using LiveCharts.Wpf;
using System.IO;
using System.ComponentModel;

namespace MiddleWare.Views
{
    /// <summary>
    /// PLlivechart.xaml 的交互逻辑
    /// </summary>
    public partial class PLlivechart : UserControl
    {

        public PLlivechart()
        {
            InitializeComponent();
        }


        public void show(Query.single_record record)
        {
            ChartValues<double> list_pac = new ChartValues<double>();
            for (int i = 0; i < 5; i++)
            {
                list_pac.Add(0.0);
            }
            ChartValues<double> list_rbc = new ChartValues<double>();
            ChartValues<double> list_plt = new ChartValues<double>();

            //label_PAC.Content = "test";
            foreach (var single in record.result)
            {
                if (single.item == "RBCHist" || single.item == "PLTHist" || single.item == "PACBit" || single.item == "PAC1"
                    || single.item == "PAC2" || single.item == "PAC3" || single.item == "PAC4" || single.item == "PAC5"
                    || single.item == "PAC6" || single.item == "PAC7" || single.item == "PAC8")
                {
                    if (single.item == "RBCHist")
                    {
                        for (int i = 0, j = 0; i < single.result.Length / 2; i++)
                        {
                            int Yaxis = Convert.ToInt16(single.result.Substring(j, 2), 16);
                            list_rbc.Add(Yaxis);

                            j = j + 2;
                        }
                    }
                    if (single.item == "PLTHist")
                    {
                        for (int i = 0, j = 0; i < single.result.Length / 2; i++)
                        {
                            int Yaxis = Convert.ToInt16(single.result.Substring(j, 2), 16);
                            list_plt.Add(Yaxis);
                            j = j + 2;
                        }
                    }
                    if (single.item == "PAC1")
                    {
                        list_pac[0] = Convert.ToInt16(single.result);

                    }
                    if (single.item == "PAC2")
                    {
                        list_pac[1] = Convert.ToInt16(single.result);
                    }
                    if (single.item == "PAC3")
                    {
                        list_pac[2] = Convert.ToInt16(single.result);
                    }
                    if (single.item == "PAC4")
                    {
                        list_pac[3] = Convert.ToInt16(single.result);
                    }
                    if (single.item == "PAC5")
                    {
                        list_pac[4] = Convert.ToInt16(single.result);
                    }
                    continue;
                }
            }

            LVC_PAC.Series = new SeriesCollection
             {
                 new LineSeries
                {
                    Values = list_pac,
                    Fill = Brushes.Transparent,
                    PointGeometrySize = 6
                }
            };
            LVC_RBC.Series = new SeriesCollection
             {
                 new LineSeries
                {
                    Values = list_rbc,
                    PointGeometry = null,
                    Fill = Brushes.OrangeRed,
                    Stroke = Brushes.OrangeRed
                }
            };
            LVC_PLT.Series = new SeriesCollection
             {
                 new LineSeries
                {
                    Values = list_plt,
                    PointGeometry = null,
                    Fill = Brushes.YellowGreen,
                    Stroke = Brushes.YellowGreen
                }
            };

        }

    }


}
