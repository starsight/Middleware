using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.IO.Ports;
using System.ComponentModel;

namespace MiddleWare.Views
{
    /// <summary>
    /// ASTMconnect.xaml 的交互逻辑
    /// </summary>
    public partial class ASTMconnect : UserControl
    {
        public static ASTMupdata astmupdata = new ASTMupdata();

        public static ObservableCollection<Com> ComList;
        public ObservableCollection<Buad> BuadList;
        public ObservableCollection<DataBit> DataBitList;
        public ObservableCollection<StopBit> StopBitList;
        public ObservableCollection<CheckBit> CheckBitList;
        public ASTMconnect()
        {
            InitializeComponent();

            this.grid_ASTMconnect.DataContext = astmupdata;

            ComList = new ObservableCollection<Com>();
            BuadList = new ObservableCollection<Buad>();
            DataBitList = new ObservableCollection<DataBit>();
            StopBitList = new ObservableCollection<StopBit>();
            CheckBitList = new ObservableCollection<CheckBit>();

            combobox_astmcom.ItemsSource = ComList;//把下拉列表绑定进去
            combobox_astmbuad.ItemsSource = BuadList;
            combobox_astmdatabit.ItemsSource = DataBitList;
            combobox_astmstopbit.ItemsSource = StopBitList;
            combobox_astmcheckbit.ItemsSource = CheckBitList;

            #region COM设置
            ComSearch();
            #endregion
            #region Buad设置
            BuadList.Add(new Buad { NUM = 9600, ID = 0 });
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

            //读取配置文件
            if (AppConfig.GetAppConfig("ASTMComBuad") != null)//波特率
            {
                int combaud = Convert.ToInt32(AppConfig.GetAppConfig("ASTMComBuad"));
                if (combaud == 9600)
                    combobox_astmbuad.SelectedIndex = 0;
                else if(combaud == 115200)
                    combobox_astmbuad.SelectedIndex = 1;
            }
            else
            {
                combobox_astmbuad.SelectedIndex = 1;//默认波特率115200
            }
            if (AppConfig.GetAppConfig("ASTMComDatabit") != null)//数据位
            {
                int databit = Convert.ToInt16(AppConfig.GetAppConfig("ASTMComDatabit"));
                if (databit == 6)
                    combobox_astmdatabit.SelectedIndex = 0;
                else if (databit == 7)
                    combobox_astmdatabit.SelectedIndex = 1;
                else if (databit == 8)
                    combobox_astmdatabit.SelectedIndex = 2;
            }
            else
            {
                combobox_astmdatabit.SelectedIndex = 2;//默认数据位8
            }
            if (AppConfig.GetAppConfig("ASTMComStopbit") != null)//停止位
            {
                string stopbit = AppConfig.GetAppConfig("ASTMComStopbit");
                if (stopbit == "1")
                    combobox_astmstopbit.SelectedIndex = 0;
                else if (stopbit == "1.5")
                    combobox_astmstopbit.SelectedIndex = 1;
                else if (stopbit == "2")
                    combobox_astmstopbit.SelectedIndex = 2;
            }
            else
            {
                combobox_astmstopbit.SelectedIndex = 0;//默认停止位1
            }
            if (AppConfig.GetAppConfig("ASTMComCheck") != null)//校验
            {
                string checkbit = AppConfig.GetAppConfig("ASTMComCheck");
                if (checkbit == "无")
                    combobox_astmcheckbit.SelectedIndex = 0;
                else if (checkbit == "奇校验")
                    combobox_astmcheckbit.SelectedIndex = 1;
                else if (checkbit == "偶校验")
                    combobox_astmcheckbit.SelectedIndex = 2;
            }
            else
            {
                combobox_astmcheckbit.SelectedIndex = 0;//默认无校验位
            }

            if (AppConfig.GetAppConfig("ASTMIP") != null)
            {
                this.textbox_astmip.Text = AppConfig.GetAppConfig("ASTMIP");
            }

            if (AppConfig.GetAppConfig("ASTMPORT") != null)
            {
                this.textbox_astmport.Text = AppConfig.GetAppConfig("ASTMPORT");
            }
           
            if (AppConfig.GetAppConfig("ASTMUpLoadWay") != null)//ASTM的上传方式 0-网口 1-串口
            {
                int i = Convert.ToInt16(AppConfig.GetAppConfig("ASTMUpLoadWay"));
                if (i == 0)
                {
                    astmupdata.IsASTMNet = true;
                   // this.checkbox_netupdata.IsChecked = true;
                } else if(i==1)
                {
                    astmupdata.IsASTMCom = true;
                    //this.checkbox_comupdata.IsChecked = true;
                }
            }
        }
        public static int ComSearch()
        {
            int i = 0;

            string[] ports = SerialPort.GetPortNames();//搜索可用COM列表
            ComList.Clear();
            if (ports.Length == 0)
            {
                //这里面不能写返回,否则下面不执行
            }
            else
            {
                foreach (var singal in ports)
                {
                    Com com = new Com();
                    com.NAME = singal;
                    com.ID = i;
                    ++i;
                    ComList.Add(com);
                }
            }
            return i;
        }
    }
    /// <summary>
    /// UI类,确定ASTM通讯方式
    /// </summary>
    public class ASTMupdata : INotifyPropertyChanged
    {
        private bool _IsASTMCom;
        private bool _IsASTMNet;
        
        public bool IsASTMCom
        {
            get
            {
                return this._IsASTMCom;
            }
            set
            {
                if(this._IsASTMCom!=value)
                {
                    this._IsASTMCom = value;
                    OnPropertyChanged("IsASTMCom");
                }
            }
        }
        public bool IsASTMNet
        {
            get
            {
                return this._IsASTMNet;
            }
            set
            {
                if(this._IsASTMNet!=value)
                {
                    this._IsASTMNet = value;
                    OnPropertyChanged("IsASTMNet");
                }
            }
        }

        public ASTMupdata()
        {
            this.PropertyChanged += IsASTMmodePropertyChanged;
        }
        void IsASTMmodePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsASTMCom")
            {
                GlobalVariable.IsASTMCom = IsASTMCom;
            }
            else if (e.PropertyName == "IsASTMNet")
            {
                GlobalVariable.IsASTMNet = IsASTMNet;
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
