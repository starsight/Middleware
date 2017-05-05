using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
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
    /// PLconnect.xaml 的交互逻辑
    /// </summary>
    public partial class PLconnect : UserControl
    {
        public static ObservableCollection<Com> ComList;
        public ObservableCollection<Buad> BuadList;
        public ObservableCollection<DataBit> DataBitList;
        public ObservableCollection<StopBit> StopBitList;
        public ObservableCollection<CheckBit> CheckBitList;
        public PLconnect()
        {
            InitializeComponent();

            ComList = new ObservableCollection<Com>();
            BuadList = new ObservableCollection<Buad>();
            DataBitList = new ObservableCollection<DataBit>();
            StopBitList = new ObservableCollection<StopBit>();
            CheckBitList = new ObservableCollection<CheckBit>();

            combobox_plcom.ItemsSource = ComList;//把下拉列表绑定进去
            combobox_plbuad.ItemsSource = BuadList;
            combobox_pldatabit.ItemsSource = DataBitList;
            combobox_plstopbit.ItemsSource = StopBitList;
            combobox_plcheckbit.ItemsSource = CheckBitList;

            #region COM设置
            ComSearch();
            #endregion
            #region Buad设置
            BuadList.Add(new Buad { NUM = 9200, ID = 0 });
            BuadList.Add(new Buad { NUM = 115200, ID = 1 });
            #endregion
            #region 数据位设置
            DataBitList.Add(new DataBit { NUM = 6, ID = 0 });
            DataBitList.Add(new DataBit { NUM = 7, ID = 1 });
            DataBitList.Add(new DataBit { NUM = 8, ID = 2 });
            #endregion
            #region 停止位设置
            StopBitList.Add(new StopBit { NUM = "1", ID = 0 });
            StopBitList.Add(new StopBit { NUM = "1.5", ID = 1 });
            StopBitList.Add(new StopBit { NUM = "2", ID = 2 });
            #endregion
            #region 奇偶校验设置
            CheckBitList.Add(new CheckBit { NUM = "无", ID = 0 });
            CheckBitList.Add(new CheckBit { NUM = "奇校验", ID = 1 });
            CheckBitList.Add(new CheckBit { NUM = "偶校验", ID = 2 });
            #endregion

            if (AppConfig.GetAppConfig("PLComBuad") != null)//波特率
            {
                combobox_plbuad.SelectedValue = Convert.ToInt16(AppConfig.GetAppConfig("PLComBuad"));
            }
            else
            {
                combobox_plbuad.SelectedIndex = 1;//默认波特率115200
            }
            if (AppConfig.GetAppConfig("PLComDatabit") != null)//数据位
            {
                combobox_pldatabit.SelectedValue = Convert.ToInt16(AppConfig.GetAppConfig("PLComDatabit"));
            }
            else
            {
                combobox_pldatabit.SelectedIndex = 2;//默认数据位8
            }
            if (AppConfig.GetAppConfig("PLComStopbit") != null)//停止位
            {
                combobox_plstopbit.SelectedValue = Convert.ToInt16(AppConfig.GetAppConfig("PLComStopbit"));
            }
            else
            {
                combobox_plstopbit.SelectedIndex = 0;//默认停止位1
            }
            if (AppConfig.GetAppConfig("PLComCheck") != null)//校验
            {
                combobox_plcheckbit.SelectedValue = Convert.ToInt16(AppConfig.GetAppConfig("PLComCheck"));
            }
            else
            {
                combobox_plcheckbit.SelectedIndex = 0;//默认无校验位
            }
        }
        public static void ComSearch()
        {
            string[] ports = SerialPort.GetPortNames();//搜索可用COM列表
            ComList.Clear();
            if (ports.Length == 0)
            {
            }
            else
            {
                int i = 0;
                foreach (var singal in ports)
                {
                    Com com = new Com();
                    com.NAME = singal;
                    com.ID = i;
                    ++i;
                    ComList.Add(com);
                }
            }
        }
    }

    public class Com : INotifyPropertyChanged
    {
        private string _NAME;
        private int _ID;

        public string NAME
        {
            get
            {
                return this._NAME;
            }
            set
            {
                if(this._NAME!=value)
                {
                    this._NAME = value;
                    OnPropertyChanged("NAME");
                }
            }
        }
        public int ID
        {
            get
            {
                return this._ID;
            }
            set
            {
                if(this._ID!=value)
                {
                    this._ID = value;
                    OnPropertyChanged("ID");
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
    public class Buad : INotifyPropertyChanged
    {

        private int _NUM;
        private int _ID;

        public int NUM
        {
            get
            {
                return this._NUM;
            }
            set
            {
                if(this._NUM!=value)
                {
                    this._NUM = value;
                    OnPropertyChanged("NUM");
                }
            }
        }
        public int ID
        {
            get
            {
                return this._ID;
            }
            set
            {
                if (this._ID != value)
                {
                    this._ID = value;
                    OnPropertyChanged("ID");
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
    public class DataBit : INotifyPropertyChanged
    {

        private int _NUM;
        private int _ID;

        public int NUM
        {
            get
            {
                return this._NUM;
            }
            set
            {
                if (this._NUM != value)
                {
                    this._NUM = value;
                    OnPropertyChanged("NUM");
                }
            }
        }
        public int ID
        {
            get
            {
                return this._ID;
            }
            set
            {
                if (this._ID != value)
                {
                    this._ID = value;
                    OnPropertyChanged("ID");
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
    public class StopBit : INotifyPropertyChanged
    {

        private string _NUM;
        private int _ID;

        public string NUM
        {
            get
            {
                return this._NUM;
            }
            set
            {
                if (this._NUM != value)
                {
                    this._NUM = value;
                    OnPropertyChanged("NUM");
                }
            }
        }
        public int ID
        {
            get
            {
                return this._ID;
            }
            set
            {
                if (this._ID != value)
                {
                    this._ID = value;
                    OnPropertyChanged("ID");
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
    public class CheckBit : INotifyPropertyChanged
    {

        private string _NUM;
        private int _ID;

        public string NUM
        {
            get
            {
                return this._NUM;
            }
            set
            {
                if (this._NUM != value)
                {
                    this._NUM = value;
                    OnPropertyChanged("NUM");
                }
            }
        }
        public int ID
        {
            get
            {
                return this._ID;
            }
            set
            {
                if (this._ID != value)
                {
                    this._ID = value;
                    OnPropertyChanged("ID");
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


}
