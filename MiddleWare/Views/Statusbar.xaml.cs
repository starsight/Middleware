using System;
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
        
        public Statusbar()
        {
            InitializeComponent();
            grid_StatusBar.DataContext = SBar;
            this.DataContext = this;
            SBar.SendNum = 0;
            SBar.ReceiveNum = 0;
            SBar.ReplyNum = 0; 
        }
       
    }

    public class StatusBar : INotifyPropertyChanged
    {
        private int _ReceiveNum;
        private int _SendNum;
        private int _ReplyNum;
        private bool _IsOneWay;

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
