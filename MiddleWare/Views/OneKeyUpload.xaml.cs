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
    /// OneKeyUpload.xaml 的交互逻辑
    /// </summary>
    public partial class OneKeyUpload : UserControl
    {

        public ObservableCollection<Upload_Show> UploadList;

        public OneKeyUpload()
        {
            InitializeComponent();
            UploadList = new ObservableCollection<Upload_Show>();


            datagrid_upload.ItemsSource = UploadList;

        }

        private void button_onekeyupload_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button_allselect_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button_upload_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    
    public class Upload_Show: INotifyPropertyChanged
    {
        private int _number;
        private string _Sample_ID;
        private string _Patient_ID;
        private string _Item;
        private string _Kind;
        private string _Device;
        private string _Test_Time;
        private bool _IsSelected;

        public int number
        {
            get
            {
                return this._number;
            }
            set
            {
                if(this._number!=value)
                {
                    this._number = value;
                    OnPropertyChanged("number");
                }
            }
        }
        public string Sample_ID
        {
            get
            {
                return this._Sample_ID;
            }
            set
            {
                if(this._Sample_ID!=value)
                {
                    this._Sample_ID = value;
                    OnPropertyChanged("Sample_ID");
                }
            }
        }
        public string Patient_ID
        {
            get
            {
                return this._Patient_ID;
            }
            set
            {
                if(this._Patient_ID!=value)
                {
                    this._Patient_ID = value;
                    OnPropertyChanged("Patient_ID");
                }
            }
        }
        public string Item
        {
            get
            {
                return this._Item;
            }
            set
            {
                if(this._Item!=value)
                {
                    this._Item = value;
                    OnPropertyChanged("Item");
                }
            }
        }
        public string Kind
        {
            get
            {
                return this._Kind;
            }
            set
            {
                if(this._Kind!=value)
                {
                    this._Kind = value;
                    OnPropertyChanged("Kind");
                }
            }
        }
        public string Device
        {
            get
            {
                return this._Device;
            }
            set
            {
                if(this._Device!=value)
                {
                    this._Device = value;
                    OnPropertyChanged("Device");
                }
            }
        }
        public string Test_Time
        {
            get
            {
                return this._Test_Time;
            }
            set
            {
                if (this._Test_Time != value)
                {
                    this._Test_Time = value;
                    OnPropertyChanged("Test_Time");
                }
            }
        }
        public bool IsSelected
        {
            get
            {
                return this._IsSelected;
            }
            set
            {
                if(this._IsSelected!=value)
                {
                    this._IsSelected = value;
                    OnPropertyChanged("IsSelected");
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
