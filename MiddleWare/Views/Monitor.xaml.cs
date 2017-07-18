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
using System.Threading;
using MiddleWare.Communicate;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using LiveCharts;
using LiveCharts.Wpf;

namespace MiddleWare.Views
{
    /// <summary>
    /// Monitor.xaml 的交互逻辑
    /// </summary>
    public partial class Monitor
    {

        private DSshow dsshow = new DSshow();//生化仪主键
        private PLshow plshow = new PLshow();//血小板主键
        private static MonitorState ms;
        private static ConvertQueue cq;
        private ObservableCollection<Result> ResultList;
        private ObservableCollection<Device> DeviceSelectMonitor;
        // private ObservableCollection<dot> DotListPAC;
        // private ObservableCollection<dot> DotListRBC;
        // private ObservableCollection<dot> DotListPLT;

        Thread UpdataStateThread;
        Thread ConvertResultDSThread;
        Thread ConvertResultPLThread;

        private ManualResetEvent DSsignal = new ManualResetEvent(false);
        private ManualResetEvent PLsignal = new ManualResetEvent(false);

        private List<string> ListName = new List<string>();//下拉菜单名称列表
        public Monitor()
        {
            InitializeComponent();
            ms = new MonitorState();//消息状态队列
            cq = new ConvertQueue();//转换队列
            UpdataStateThread = new Thread(new ThreadStart(UpdataState));//开始状态更新,包括消息传递和列表更新
            ConvertResultDSThread = new Thread(new ThreadStart(ConvertResultDS));//开始生化仪数据显示
            ConvertResultPLThread = new Thread(new ThreadStart(ConvertResultPL));//开始血小板数据显示
            UpdataStateThread.IsBackground = true;
            ConvertResultDSThread.IsBackground = true;
            ConvertResultPLThread.IsBackground = true;
            UpdataStateThread.Start();
            ConvertResultDSThread.Start();
            ConvertResultPLThread.Start();

            DSmonitor.grid_dsshow.DataContext = dsshow;//将生化仪主键绑定资源
            PLmonitor.grid_plshow.DataContext = plshow;//将血小板主键绑定资源

            //调试语句
            ResultList = new ObservableCollection<Result>();
            DeviceSelectMonitor = new ObservableCollection<Device>();
            //DotListPAC = new ObservableCollection<dot>();
            //DotListRBC = new ObservableCollection<dot>();
            //DotListPLT = new ObservableCollection<dot>();

            datagrid_monitor.ItemsSource = ResultList;//把表格绑定进去
            combobox_selectdevice.ItemsSource = DeviceSelectMonitor;//把下拉列表绑定进去
            //PLchart.chart_PAC.ItemsSource = DotListPAC;//把PAC绑定进去
            //PLchart.chart_RBC.ItemsSource = DotListRBC;//把RBC绑定进去
            //PLchart.chart_PLT.ItemsSource = DotListPLT;//把PLT绑定进去        
            this.DataContext = this;
            //datagird_monitor.DataContext = ResultList;
        }

        #region 状态更新
        public static void AddItemState(string message, string name)
        {
            switch (name)
            {
                case "DEVICE":
                    {
                        ms.AddDevice(message);
                    }
                    break;
                case "LIS":
                    {
                        ms.ADDLis(message);
                    } break;
                default: break;
            }

        }
        /// <summary>
        /// 状态更新线程,包括消息传递和下拉仪器列表更新
        /// </summary>
        private void UpdataState()
        {
            string message;
            while (true)
            {
                if (ms.IsDeviceAvailable)
                {
                    message = ms.GetDevice();
                    Dispatcher.Invoke(new Action(() =>
                    {
                        textbox_deveicestate.AppendText(message);
                        textbox_deveicestate.ScrollToEnd();
                    }));
                }
                if (ms.IsLisAvailable)
                {
                    message = ms.GetLis();
                    Dispatcher.Invoke(new Action(() =>
                    {
                        textbox_lisstate.AppendText(message);
                        textbox_lisstate.ScrollToEnd();
                    }));
                }
                if (GlobalVariable.LEN > ListName.Count) //如果列表大于零
                {
                    List<string> devicelist = new List<string>();
                    devicelist = GlobalVariable.GetAllValue();
                    if (devicelist.Count > 0)//双重判断
                    {
                        foreach (string akey in devicelist)
                        {
                            if (ListName.Contains(akey))//如果之前已经有了,就直接跳过
                                continue;
                            ListName.Add(akey);
                            Device device = new Device();
                            device.NAME = GlobalVariable.GetValue(akey);
                            Dispatcher.Invoke(new Action(() =>
                            {
                                DeviceSelectMonitor.Add(device);
                            }));
                        }
                    }
                }
                if (GlobalVariable.ClearAllList)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        DeviceSelectMonitor.Clear();
                    }));
                    ListName.Clear();
                    GlobalVariable.ClearAllList = false;
                }
                Thread.Sleep(300);
            }
        }
        #endregion
        /// <summary>
        /// 下拉栏变化时启发，切换仪器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void combobox_selectdevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string content = (string)combobox_selectdevice.SelectedValue;
            switch (content)
            {
                case "生化":
                    {
                        PLmonitor.Visibility = Visibility.Collapsed;
                        DSmonitor.Visibility = Visibility.Visible;
                        PLchart.Visibility = Visibility.Collapsed;
                        this.datagrid_monitor.Visibility = Visibility.Visible;
                        cq.IsNewDsSignal = true;
                        DSsignal.Set();
                        PLsignal.Reset();
                    }
                    break;
                case "血小板":
                    {
                        DSmonitor.Visibility = Visibility.Collapsed;
                        PLmonitor.Visibility = Visibility.Visible;
                        PLchart.Visibility = Visibility.Visible;
                        this.datagrid_monitor.Visibility = Visibility.Visible;
                        cq.IsNewPlSignal = true;
                        DSsignal.Reset();
                        PLsignal.Set();
                    } break;
                default: break;
            }
        }
        /// <summary>
        /// 后台一直接收数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="name"></param>
        public static void ReceiveResult(object data, string name)//这是后台一直运行的
        {
            if (name == "DS")
            {
                DI800Manager.DI800 di800 = (DI800Manager.DI800)data;
                if(GlobalVariable.NoDisplaySampleID.Contains(di800.SAMPLE_ID))
                {
                    GlobalVariable.NoDisplaySampleID.Remove(di800.SAMPLE_ID);
                    return;
                }
                cq.AddDS(di800);
                while (cq.IsConvertDsMorethanOne)//队列中就保持一个
                {
                    cq.RemoveDS();
                }
            }
            if (name == "PL")
            {
                PLManager.PL12 pl12 = (PLManager.PL12)data;
                cq.AddPL(pl12);
                while (cq.IsConvertPlMorethanOne)
                {
                    cq.RemovePL();
                }
            }
        }
        /// <summary>
        /// 生化仪的显示程序，如果不切换生化仪的话，线程会休眠
        /// </summary>
        private void ConvertResultDS()
        {
            while (true)
            {
                DSsignal.WaitOne();
                if (cq.IsNewDsSignal && cq.IsConvertDsAvailable)
                {
                    DI800Manager.DI800 di800 = cq.GetDS();
                    List<DI800Manager.DI800Result> di800result = di800.Result;
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ResultList.Clear();//清楚上条记录
                    }));
                    foreach (var single in di800result)
                    {
                        Result DSresult = new Result();
                        DSresult.ITEM = single.ITEM;
                        DSresult.FULL_NAME = single.FULL_NAME;
                        DSresult.RESULT = single.RESULT.ToString();
                        DSresult.UNIT = single.UNIT;
                        DSresult.NORMAL_LOW = single.NORMAL_LOW.ToString();
                        DSresult.NORMAL_HIGH = single.NORMAL_HIGH.ToString();
                        DSresult.INDICATE = single.INDICATE;
                        Dispatcher.Invoke(new Action(() =>
                        {
                            ResultList.Add(DSresult);
                        }));
                    }
                    Dispatcher.Invoke(new Action(() =>
                    {
                        dsshow.DSTYPE = di800.Type;
                        dsshow.DSPATIENT_ID = di800.PATIENT_ID;
                        dsshow.DSDOCTOR = di800.DOCTOR;
                        dsshow.DSSAMPLE_ID = di800.SAMPLE_ID;
                        dsshow.DSTEST_TIME = di800.TIME.ToString();
                        dsshow.DSFIRST_NAME = di800.FIRST_NAME;
                        dsshow.DSSAMPLE_KIND = di800.SAMPLE_KIND;
                        dsshow.DSAREA = di800.AREA;
                        dsshow.DSSEX = di800.SEX;
                        dsshow.DSBED = di800.BED;
                        dsshow.DSDEVICE = di800.Device;
                    }));
                }
                Thread.Sleep(300);
            }
        }
        /// <summary>
        /// 血小板的显示程序，如果不切换血小板的话，线程会休眠
        /// </summary>
        private void ConvertResultPL()
        {
            while (true)
            {
                PLsignal.WaitOne();
                if (cq.IsNewPlSignal && cq.IsConvertPlAvailable)
                {
                    PLManager.PL12 pl12 = cq.GetPL();
                    List<PLManager.PL12Result> pl12result = pl12.Result;

                    /**add by wenjie for livecharts**/
                    ChartValues<double> list_pac = new ChartValues<double>();
                    for (int i = 0; i < 5; i++)
                    {
                        list_pac.Add(0.0);
                    }
                    ChartValues<double> list_rbc = new ChartValues<double>();
                    ChartValues<double> list_plt = new ChartValues<double>();
                    /**add by wenjie for livecharts**/

                    Dispatcher.Invoke(new Action(() =>
                    {
                        ResultList.Clear();//清除上条记录
                        // DotListPAC.Clear();
                        // DotListPLT.Clear();
                        // DotListRBC.Clear();
                    }));
                    foreach (var single in pl12result)
                    {
                        if (single.ITEM == "RBCHist" || single.ITEM == "PLTHist" || single.ITEM == "PACBit" || single.ITEM == "PAC1"
                            || single.ITEM == "PAC2" || single.ITEM == "PAC3" || single.ITEM == "PAC4" || single.ITEM == "PAC5"
                            || single.ITEM == "PAC6" || single.ITEM == "PAC7" || single.ITEM == "PAC8")
                        {
                            if (single.ITEM == "RBCHist")
                            {
                                for (int i = 0, j = 0; i < single.RESULT.Length / 2; i++)
                                {
                                    int Yaxis = Convert.ToInt16(single.RESULT.Substring(j, 2), 16);
                                    list_rbc.Add(Yaxis);

                                    j = j + 2;
                                }
                            }
                            if (single.ITEM == "PLTHist")
                            {
                                for (int i = 0, j = 0; i < single.RESULT.Length / 2; i++)
                                {
                                    int Yaxis = Convert.ToInt16(single.RESULT.Substring(j, 2), 16);
                                    list_plt.Add(Yaxis);
                                    j = j + 2;
                                }
                            }
                            if (single.ITEM == "PAC1")
                            {
                                list_pac[0] = Convert.ToInt16(single.RESULT);
                            }
                            if (single.ITEM == "PAC2")
                            {
                                list_pac[1] = Convert.ToInt16(single.RESULT);
                            }
                            if (single.ITEM == "PAC3")
                            {
                                list_pac[2] = Convert.ToInt16(single.RESULT);
                            }
                            if (single.ITEM == "PAC4")
                            {
                                list_pac[3] = Convert.ToInt16(single.RESULT);
                            }
                            if (single.ITEM == "PAC5")
                            {
                                list_pac[4] = Convert.ToInt16(single.RESULT);
                            }
                            continue;
                        }
                        Result PLresult = new Result();
                        PLresult.ITEM = single.ITEM;
                        PLresult.FULL_NAME = single.FULL_NAME;
                        PLresult.RESULT = single.RESULT;
                        PLresult.UNIT = single.UNIT;
                        PLresult.NORMAL_LOW = single.NORMAL_LOW.ToString();
                        PLresult.NORMAL_HIGH = single.NORMAL_HIGH.ToString();
                        PLresult.INDICATE = single.INDICATE;
                        Dispatcher.Invoke(new Action(() =>
                        {
                            ResultList.Add(PLresult);
                            PLchart.LVC_PAC.Series = new SeriesCollection
                                 {
                                     new LineSeries
                                    {
                                        Values = list_pac,
                                    }
                                };
                            PLchart.LVC_RBC.Series = new SeriesCollection
                                 {
                                     new LineSeries
                                    {
                                        Values = list_rbc,
                                        PointGeometry = null,
                                        Fill = Brushes.OrangeRed,
                                        Stroke = Brushes.OrangeRed
                                    }
                                };
                            PLchart.LVC_PLT.Series = new SeriesCollection
                                 {
                                     new LineSeries
                                    {
                                        Values = list_plt,
                                        PointGeometry = null, 
                                    }
                                };
                        }));
                    }
                    Dispatcher.Invoke(new Action(() =>
                    {
                        plshow.PLTYPE = pl12.TYPE;
                        plshow.PLSAMPLE_ID = pl12.SAMPLE_ID;
                        plshow.PLTEST_TIME = pl12.TEST_TIME.ToString();
                        plshow.PLDEVICE = pl12.DEVEICE;
                        plshow.PLBARCODE = pl12.BARCODE;
                        plshow.PLSAMPLE_KIND = pl12.SAMPLE_KIND;
                        plshow.PLAAP = pl12.AAP;
                    }));
                }
                Thread.Sleep(200);
            }
        }
    }
    public class MonitorState
    {
        private static Queue<string> DeviceState = new Queue<string>();
        private static Queue<string> LisState = new Queue<string>();
        public void AddDevice(string data)
        {
            lock (this)
            {
                DeviceState.Enqueue(data);
            }
        }
        public void ADDLis(string data)
        {
            lock (this)
            {
                LisState.Enqueue(data);
            }
        }
        public string GetDevice()
        {
            lock (this)
            {
                return DeviceState.Dequeue();
            }
        }
        public string GetLis()
        {
            lock (this)
            {
                return LisState.Dequeue();
            }
        }
        public bool IsDeviceAvailable
        {
            get
            {
                return DeviceState.Count > 0;
            }
        }
        public bool IsLisAvailable
        {
            get
            {
                return LisState.Count > 0;
            }
        }
    }
    public class Result : INotifyPropertyChanged
    {
        private string _ITEM;
        private string _FULL_ITEM;
        private string _RESULT;
        private string _UNIT;
        private string _NORMAL_LOW;
        private string _NORMAL_HIGH;
        private string _INDICATE;

        public string ITEM
        {
            get
            {
                return this._ITEM;
            }
            set
            {
                if (this._ITEM != value)
                {
                    this._ITEM = value;
                    OnPropertyChanged("ITEM");
                }
            }
        }
        public string FULL_NAME
        {
            get
            {
                return this._FULL_ITEM;
            }
            set
            {
                if (this._FULL_ITEM != value)
                {
                    this._FULL_ITEM = value;
                    OnPropertyChanged("FULL_NAME");
                }
            }
        }
        public string RESULT
        {
            get
            {
                return this._RESULT;
            }
            set
            {
                if (this._RESULT != value)
                {
                    this._RESULT = value;
                    OnPropertyChanged("RESULT");
                }
            }
        }
        public string UNIT
        {
            get
            {
                return this._UNIT;
            }
            set
            {
                if (this._UNIT != value)
                {
                    this._UNIT = value;
                    OnPropertyChanged("UNIT");
                }
            }
        }
        public string NORMAL_LOW
        {
            get
            {
                return this._NORMAL_LOW;
            }
            set
            {
                if (this._NORMAL_LOW != value)
                {
                    this._NORMAL_LOW = value;
                    OnPropertyChanged("NORMAL_LOW");
                }
            }
        }
        public string NORMAL_HIGH
        {
            get
            {
                return this._NORMAL_HIGH;
            }
            set
            {
                if (this._NORMAL_HIGH != value)
                {
                    this._NORMAL_HIGH = value;
                    OnPropertyChanged("NORMAL_HIGH");
                }
            }
        }
        public string INDICATE
        {
            get
            {
                return this._INDICATE;
            }
            set
            {
                if (this._INDICATE != value)
                {
                    this._INDICATE = value;
                    OnPropertyChanged("INDICATE");
                }
            }
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string info)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }
    public class ConvertQueue
    {
        private object ConvertDSLocker = new object();
        private object ConvertPLLocker = new object();

        private static Queue<DI800Manager.DI800> ConvertDS = new Queue<DI800Manager.DI800>();
        private static Queue<PLManager.PL12> ConvertPL = new Queue<PLManager.PL12>();

        public void AddDS(DI800Manager.DI800 data)
        {
            lock (ConvertDSLocker)
            {
                ConvertDS.Enqueue(data);
                IsNewDsSignal = true;
            }
        }
        public void AddPL(PLManager.PL12 data)
        {
            lock (ConvertPLLocker)
            {
                ConvertPL.Enqueue(data);
                IsNewPlSignal = true;
            }
        }
        public DI800Manager.DI800 GetDS()
        {
            lock (ConvertDSLocker)
            {
                IsNewDsSignal = false;
                return ConvertDS.Peek();
            }
        }
        public PLManager.PL12 GetPL()
        {
            lock (ConvertPLLocker)
            {
                IsNewPlSignal = false;
                return ConvertPL.Peek();
            }
        }
        public bool IsConvertDsAvailable
        {
            get
            {
                return ConvertDS.Count > 0;
            }
        }
        public bool IsConvertPlAvailable
        {
            get
            {
                return ConvertPL.Count > 0;
            }
        }
        public bool IsConvertDsMorethanOne
        {
            get
            {
                return ConvertDS.Count > 1;
            }
        }
        public bool IsConvertPlMorethanOne
        {
            get
            {
                return ConvertPL.Count > 1;
            }
        }
        public void RemoveDS()//移除队列开始的对象
        {
            lock (this)
            {
                ConvertDS.Dequeue();
            }
        }
        public void RemovePL()
        {
            lock (this)
            {
                ConvertPL.Dequeue();
            }
        }

        private bool _IsNewDsSignal = false;
        private bool _IsNewPlSignal = false;
        public bool IsNewDsSignal
        {
            get
            {
                return this._IsNewDsSignal;
            }
            set
            {
                if (this._IsNewDsSignal != value)
                {
                    this._IsNewDsSignal = value;
                }
            }
        }
        public bool IsNewPlSignal
        {
            get
            {
                return this._IsNewPlSignal;
            }
            set
            {
                if (this._IsNewPlSignal != value)
                {
                    this._IsNewPlSignal = value;
                }
            }
        }
    }
}
