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
    /// DSmonitor.xaml 的交互逻辑
    /// </summary>
    public partial class DSmonitor : UserControl
    {
        
        public DSmonitor()
        {
            InitializeComponent();
        }

    }
    public class DSshow : INotifyPropertyChanged
    {
        private string _DSTYPE;
        private string _DSSAMPLE_ID;
        private string _DSDOCTOR;
        private string _DSPATIENT_ID;
        private string _DSTEST_TIME;
        private string _DSDEPARTMENT;
        private string _DSFIRST_NAME;
        private string _DSSAMPLE_KIND;
        private string _DSAREA;
        private string _DSSEX;
        private string _DSDEVICE;
        private string _DSBED;

        public string DSTYPE
        {
            get
            {
                return _DSTYPE;
            }
            set
            {
                if(this._DSTYPE!=value)
                {
                    this._DSTYPE = value;
                    OnPropertyChanged("DSTYPE");
                }
            }
        }
        public string DSSAMPLE_ID
        {
            get
            {
                return _DSSAMPLE_ID;
            }
            set
            {
                if (this._DSSAMPLE_ID != value)
                {
                    this._DSSAMPLE_ID = value;
                    OnPropertyChanged("DSSAMPLE_ID");
                }
            }
        }
        public string DSDOCTOR
        {
            get
            {
                return _DSDOCTOR;
            }
            set
            {
                if (this._DSDOCTOR != value)
                {
                    this._DSDOCTOR = value;
                    OnPropertyChanged("DSDOCTOR");
                }
            }
        }
        public string DSPATIENT_ID
        {
            get
            {
                return _DSPATIENT_ID;
            }
            set
            {
                if (this._DSPATIENT_ID != value)
                {
                    this._DSPATIENT_ID = value;
                    OnPropertyChanged("DSPATIENT_ID");
                }
            }
        }
        public string DSTEST_TIME
        {
            get
            {
                return _DSTEST_TIME;
            }
            set
            {
                if (this._DSTEST_TIME != value)
                {
                    this._DSTEST_TIME = value;
                    OnPropertyChanged("DSTEST_TIME");
                }
            }
        }
        public string DSDEPARTMENT
        {
            get
            {
                return _DSDEPARTMENT;
            }
            set
            {
                if (this._DSDEPARTMENT != value)
                {
                    this._DSDEPARTMENT = value;
                    OnPropertyChanged("DSDEPARTMENT");
                }
            }
        }
        public string DSFIRST_NAME
        {
            get
            {
                return _DSFIRST_NAME;
            }
            set
            {
                if (this._DSFIRST_NAME != value)
                {
                    this._DSFIRST_NAME = value;
                    OnPropertyChanged("DSFIRST_NAME");
                }
            }
        }
        public string DSSAMPLE_KIND
        {
            get
            {
                return _DSSAMPLE_KIND;
            }
            set
            {
                if (this._DSSAMPLE_KIND != value)
                {
                    this._DSSAMPLE_KIND = value;
                    OnPropertyChanged("DSSAMPLE_KIND");
                }
            }
        }
        public string DSAREA
        {
            get
            {
                return _DSAREA;
            }
            set
            {
                if (this._DSAREA != value)
                {
                    this._DSAREA = value;
                    OnPropertyChanged("DSAREA");
                }
            }
        }
        public string DSSEX
        {
            get
            {
                return _DSSEX;
            }
            set
            {
                if (this._DSSEX != value)
                {
                    this._DSSEX = value;
                    OnPropertyChanged("DSSEX");
                }
            }
        }
        public string DSDEVICE
        {
            get
            {
                return _DSDEVICE;
            }
            set
            {
                if (this._DSDEVICE != value)
                {
                    this._DSDEVICE = value;
                    OnPropertyChanged("DSDEVICE");
                }
            }
        }
        public string DSBED
        {
            get
            {
                return _DSBED;
            }
            set
            {
                if (this._DSBED != value)
                {
                    this._DSBED = value;
                    OnPropertyChanged("DSBED");
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
