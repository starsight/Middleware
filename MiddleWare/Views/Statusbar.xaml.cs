﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// Statusbar.xaml 的交互逻辑
    /// </summary>
    public partial class Statusbar
    {
        public static StatusBar SBar = new StatusBar();
        private MainWindow mainwin = (MainWindow)Application.Current.MainWindow;
        public Statusbar()
        {
            InitializeComponent();
            grid_StatusBar.DataContext = SBar;
            this.DataContext = this;
            SBar.SendNum = 0;
            SBar.ReceiveNum = 0;
            SBar.ReplyNum = 0;
            SBar.IssueNum = 0;
            SBar.NoSendNum = 0;
            SBar.NoIssueNum = 0;
        }

        private void ChangePage(int index)
        {
            mainwin.MainTabControl.SelectedIndex = 3;
            if (index == 1)
            {
                //从0开始检索，这是第2行，代表一键上传
                mainwin.SetOption.onekeyupload_MouseLeftButtonUp(null, null);
                mainwin.SetOption.onekeyupload.IsSelected = true;
            }
            else if (index == 2)
            {
                //从0开始检索，这是第三行，代表一键下发
                mainwin.SetOption.onekeydownload_MouseLeftButtonUp(null, null);
                mainwin.SetOption.onekeydownload.IsSelected = true;
            }
        }

        private void button_nosendnum_Click(object sender, RoutedEventArgs e)
        {
            ChangePage(1);//转到一键上传界面
            OneKeyUpload onekeyupload = mainwin.SetOption.OneKeyUpload;
            onekeyupload.button_viewsamole_Click(null, null);
        }

        private void button_noissuenum_Click(object sender, RoutedEventArgs e)
        {
            ChangePage(2);
            OneKeyDownload onkeydownload = mainwin.SetOption.OneKeyDownload;
            onkeydownload.button_viewsamole_Click(null, null);
        }
    }

    public class StatusBar : INotifyPropertyChanged
    {
        private int _ReceiveNum;
        private int _SendNum;
        private int _ReplyNum;
        private int _IssueNum;
        private int _NoSendNum;
        private int _NoIssueNum;
        private bool _IsOneWay;

        private string _LISStatus="×";//mini mode show
        private string _DeviceStatus = "√";
        private string _SoftStatus = "Wait";
        private string _SampleId = string.Empty;

        public int ReceiveNum
        {
            get
            {
                return this._ReceiveNum;
            }
            set
            {
                if (this._ReceiveNum != value)
                {
                    this._ReceiveNum = value;
                    OnPropertyChanged("ReceiveNum");
                }
            }
        }
        public int SendNum
        {
            get
            {
                return this._SendNum;
            }
            set
            {
                if (this._SendNum != value)
                {
                    this._SendNum = value;
                    OnPropertyChanged("SendNum");
                }
            }
        }
        public int ReplyNum
        {
            get
            {
                return this._ReplyNum;
            }
            set
            {
                if (this._ReplyNum != value)
                {
                    this._ReplyNum = value;
                    OnPropertyChanged("ReplyNum");
                }
            }
        }
        public int IssueNum
        {
            get
            {
                return this._IssueNum;
            }
            set
            {
                if (this._IssueNum != value) 
                {
                    this._IssueNum = value;
                    OnPropertyChanged("IssueNum");
                }
            }
        }
        public int NoSendNum
        {
            get
            {
                return this._NoSendNum;
            }
            set
            {
                if (this._NoSendNum != value)
                {
                    this._NoSendNum = value;
                    OnPropertyChanged("NoSendNum");
                }
            }
        }
        public int NoIssueNum
        {
            get
            {
                return this._NoIssueNum;
            }
            set
            {
                if (this._NoIssueNum != value) 
                {
                    this._NoIssueNum = value;
                    OnPropertyChanged("NoIssueNum");
                }
            }
        }
        public bool IsOneWay
        {
            get
            {
                return this._IsOneWay;
            }
            set
            {
                if (this._IsOneWay != value)
                {
                    this._IsOneWay = value;
                    OnPropertyChanged("IsOneWay");
                }
            }
        }
        public string LISStatus
        {
            get
            {
                return _LISStatus;
            }

            set
            {           
                if (this._LISStatus != value)
                {
                    _LISStatus = value;
                    OnPropertyChanged("LISStatus");
                }
            }
        }
        public string DeviceStatus
        {
            get
            {
                return _DeviceStatus;
            }

            set
            {
                if (this._DeviceStatus != value)
                {
                    _DeviceStatus = value;
                    OnPropertyChanged("DeviceStatus");
                }
            }
        }
        public string SoftStatus
        {
            get
            {
                return _SoftStatus;
            }

            set
            {
                if (this._SoftStatus != value)
                {
                    _SoftStatus = value;
                    OnPropertyChanged("SoftStatus");
                }
            }
        }
        public string SampleId
        {
            get
            {
                return _SampleId;
            }

            set
            {
                if (this._SampleId != value)
                {
                    _SampleId = value;
                    OnPropertyChanged("SampleId");
                }
            }
        }

        public StatusBar()
        {
            this.PropertyChanged += IsOneWayPropertyChanged;
        }

        void IsOneWayPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsOneWay")
            {
                GlobalVariable.IsOneWay = IsOneWay;
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
