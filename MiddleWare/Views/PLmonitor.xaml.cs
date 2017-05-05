using System;
using System.Collections.Generic;
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
    /// PLmonitor.xaml 的交互逻辑
    /// </summary>
    public partial class PLmonitor : UserControl
    {
        public PLmonitor()
        {
            InitializeComponent();
        }
    }
    public class PLshow : INotifyPropertyChanged
    {
        private string _PLTYPE;
        private string _PLSAMPLE_ID;
        private string _PLAAP;
        private string _PLTEST_TIME;
        private string _PLSAMPLE_KIND;
        private string _PLDEVICE;
        private string _PLBARCODE;

        public string PLTYPE
        {
            get
            {
                return _PLTYPE;
            }
            set
            {
                if(this._PLTYPE!=value)
                {
                    this._PLTYPE = value;
                    OnPropertyChanged("PLTYPE");
                }
            }
        }
        public string PLSAMPLE_ID
        {
            get
            {
                return _PLSAMPLE_ID;
            }
            set
            {
                if(this._PLSAMPLE_ID!=value)
                {
                    this._PLSAMPLE_ID = value;
                    OnPropertyChanged("PLSAMPLE_ID");
                }
            }
        }
        public string PLAAP
        {
            get
            {
                return this._PLAAP;
            }
            set
            {
                if(this._PLAAP!=value)
                {
                    this._PLAAP = value;
                    OnPropertyChanged("PLAAP");
                }
            }
        }
        public string PLTEST_TIME
        {
            get
            {
                return this._PLTEST_TIME;
            }
            set
            {
                if(this._PLTEST_TIME!=value)
                {
                    this._PLTEST_TIME = value;
                    OnPropertyChanged("PLTEST_TIME");
                }
            }
        }
        public string PLSAMPLE_KIND
        {
            get
            {
                return this._PLSAMPLE_KIND;
            }
            set
            {
                if(this._PLSAMPLE_KIND!=value)
                {
                    this._PLSAMPLE_KIND = value;
                    OnPropertyChanged("PLSAMPLE_KIND");
                }
            }
        }
        public string PLDEVICE
        {
            get
            {
                return this._PLDEVICE;
            }
            set
            {
                if(this._PLDEVICE!=value)
                {
                    this._PLDEVICE = value;
                    OnPropertyChanged("PLDEVICE");
                }
            }
        }
        public string PLBARCODE
        {
            get
            {
                return this._PLBARCODE;
            }
            set
            {
                if(this._PLBARCODE!=value)
                {
                    this._PLBARCODE = value;
                    OnPropertyChanged("PLBARCODE");
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
