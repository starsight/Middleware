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
    /// PLchart.xaml 的交互逻辑
    /// </summary>
    public partial class PLchart : UserControl
    {
        public PLchart()
        {
            InitializeComponent();
        }
    }
    public class dot : INotifyPropertyChanged
    {
        private int _Xaxis;
        private int _Yaxis;

        public int Xaxis
        {
            get
            {
                return this._Xaxis;
            }
            set
            {
                if(this._Xaxis!=value)
                {
                    this._Xaxis = value;
                    OnPropertyChanged("Xaxis");
                }
            }
        }
        public int Yaxis
        {
            get
            {
                return this._Yaxis;
            }
            set
            {
                if(this._Yaxis!=value)
                {
                    this._Yaxis = value;
                    OnPropertyChanged("Yaxis");
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
