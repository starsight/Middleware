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
using System.Collections.ObjectModel;
namespace MiddleWare.Views
{
    /// <summary>
    /// query_PLchart.xaml 的交互逻辑
    /// </summary>
    public partial class Query_PLchart : UserControl
    {
        public ObservableCollection<dot> DotListPAC;
        public ObservableCollection<dot> DotListRBC;
        public ObservableCollection<dot> DotListPLT;

        public Query_PLchart()
        {
            InitializeComponent();
        }
        public void show(Query.single_record record)
        {
            DotListPAC = new ObservableCollection<dot>();
            DotListRBC = new ObservableCollection<dot>();
            DotListPLT = new ObservableCollection<dot>();
            chart_PAC.ItemsSource = DotListPAC;
            chart_RBC.ItemsSource = DotListRBC;
            chart_PLT.ItemsSource = DotListPLT;
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
                            dot singledot = new dot();
                            singledot.Xaxis = i;
                            singledot.Yaxis = Convert.ToInt16(single.result.Substring(j, 2), 16);
                            Dispatcher.Invoke(new Action(() =>
                            {
                                DotListRBC.Add(singledot);
                            }));
                            j = j + 2;
                        }
                    }
                    if (single.item == "PLTHist")
                    {
                        for (int i = 0, j = 0; i < single.result.Length / 2; i++)
                        {
                            dot singledot = new dot();
                            singledot.Xaxis = i;
                            singledot.Yaxis = Convert.ToInt16(single.result.Substring(j, 2), 16);
                            Dispatcher.Invoke(new Action(() =>
                            {
                                DotListPLT.Add(singledot);
                            }));
                            j = j + 2;
                        }
                    }
                    if (single.item == "PAC1")
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            DotListPAC.Add(new dot { Xaxis = 1, Yaxis = Convert.ToInt16(single.result) });
                        }));
                    }
                    if (single.item == "PAC2")
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            DotListPAC.Add(new dot { Xaxis = 2, Yaxis = Convert.ToInt16(single.result) });
                        }));
                    }
                    if (single.item == "PAC3")
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            DotListPAC.Add(new dot { Xaxis = 3, Yaxis = Convert.ToInt16(single.result) });
                        }));
                    }
                    if (single.item == "PAC4")
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            DotListPAC.Add(new dot { Xaxis = 4, Yaxis = Convert.ToInt16(single.result) });
                        }));
                    }
                    if (single.item == "PAC5")
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            DotListPAC.Add(new dot { Xaxis = 5, Yaxis = Convert.ToInt16(single.result) });
                        }));
                    }
                    continue;
                }
            }
        }
    }
}
