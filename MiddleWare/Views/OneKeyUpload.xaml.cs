using log4net;
using MahApps.Metro.Controls.Dialogs;
using MiddleWare.Communicate;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
        private static OleDbConnection conn;
        private string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
        private string pathto = GlobalVariable.topDir.Parent.FullName;
        private UpOrDownload_Show singleSample;
        private int num;
        private DataSet ds;
        private string blank = string.Empty;
        private MainWindow mainwin = (MainWindow)Application.Current.MainWindow;

        private List<UpOrDownload_Show> chooseList;
        public ObservableCollection<UpOrDownload_Show> UploadList;

        private static ILog log;
        

        public OneKeyUpload()
        {
            InitializeComponent();

            UploadList = new ObservableCollection<UpOrDownload_Show>();
            chooseList = new List<UpOrDownload_Show>();

            datagrid_upload.ItemsSource = UploadList;

            strConnection += "Data Source=" + @pathto + "\\DSDB.mdb";
            conn = new OleDbConnection(strConnection);

            grid_upload.DataContext = Statusbar.SBar;

            //创建日志记录组件实例
            log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        /// <summary>
        /// 获取没有上传的样本数据
        /// </summary>
        private void GetNoSendData()
        {
            AccessManagerDS.mutex.WaitOne();
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            num = 0;
            ds = new DataSet();
            UploadList.Clear();
            string strSelect = "select * from lisoutput where [ISSEND]= 0";
            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
            {
                if (oa.Fill(ds, "Up") == 0)
                {
                    //已经全部上传了
                    ds.Clear();
                    conn.Close();
                    AccessManagerDS.mutex.ReleaseMutex();
                    return;
                }
                else
                {
                    //有一些样本没有上传
                    //先往哈希表里写入样本号
                    Hashtable htID = new Hashtable();//选用哈希表来消除重复
                    string tempItem;
                    string tempID;
                    string tempAllItem;
                    foreach (DataRow dr in ds.Tables["Up"].Rows)
                    {
                        tempID = dr["SAMPLE_ID"].ToString();
                        tempItem = dr["ITEM"].ToString();
                        if (!htID.ContainsKey(tempID))
                        {
                            //第一次进入这个样本号
                            htID.Add(tempID, tempItem);//首先给项目赋值
                        }
                        else
                        {
                            tempAllItem = htID[tempID].ToString();
                            tempAllItem += ("," + tempItem);
                            htID[tempID] = tempAllItem;
                        }
                    }
                    DataSet tempDs;
                    foreach (var tempSampleID in htID.Keys)
                    {
                        singleSample = new UpOrDownload_Show();
                        singleSample.number = ++num;
                        singleSample.IsSelected = false;
                        singleSample.Sample_ID = (string)tempSampleID;
                        singleSample.Item = (string)htID[tempSampleID];
                        strSelect = "select * from lisoutput where [ISSEND]= 0 and [SAMPLE_ID]='" + (string)tempSampleID + "'";
                        using (OleDbDataAdapter tempOa = new OleDbDataAdapter(strSelect, conn))
                        {
                            tempDs = new DataSet();
                            if (tempOa.Fill(tempDs, "temp") != 0)
                            {
                                foreach (DataRow dr in tempDs.Tables["temp"].Rows)
                                {
                                    singleSample.Test_Time = dr["SEND_TIME"] == DBNull.Value ? DateTime.Now.ToString() : dr["SEND_TIME"].ToString();
                                    singleSample.Patient_ID = dr["PATIENT_ID"] == DBNull.Value ? blank : (string)dr["PATIENT_ID"];
                                    singleSample.Device = dr["Device"] == DBNull.Value ? blank : (string)dr["Device"];
                                    singleSample.Kind = dr["Type"] == DBNull.Value ? blank : (string)dr["Type"];
                                    break;
                                }
                            }
                            tempDs.Clear();
                        }
                        UploadList.Add(singleSample);
                    }
                }
                ds.Clear();
            }
            AccessManagerDS.mutex.ReleaseMutex();
            conn.Close();
            Statusbar.SBar.NoSendNum = UploadList.Count();
        }
        /// <summary>
        /// 一键上传所有未发送样本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_onekeyupload_Click(object sender, RoutedEventArgs e)
        {
            if (!GlobalVariable.DSNum)
            {
                //如果没连接生化仪，会不进行此操作
                await mainwin.ShowMessageAsync("警告", "未连接生化仪");
                return;
            }

            AccessManagerDS.mutex.WaitOne();
            HashSet<string> hID = new HashSet<string>();
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            num = 0;
            ds = new DataSet();
            string strSelect = "select * from lisoutput where [ISSEND]= false";
            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
            {
                if (oa.Fill(ds, "Up") == 0)
                {
                    //已经全部上传了
                    UploadList.Clear();
                    ds.Clear();
                    conn.Close();
                    AccessManagerDS.mutex.ReleaseMutex();
                    return;
                }
                else
                {
                    //有一些样本没有上传
                    //先往哈希表里写入样本号
                    foreach (DataRow dr in ds.Tables["Up"].Rows)
                    {
                        //只能上传当前连接仪器的信息
                        if (dr["Device"].ToString() == GlobalVariable.DSDeviceID)
                        {
                            hID.Add(dr["SAMPLE_ID"].ToString());
                        }
                    }
                }
            }
            AccessManagerDS.mutex.ReleaseMutex();
            conn.Close();
            Thread.Sleep(500);
            if (hID.Count == 0) 
            {
                await mainwin.ShowMessageAsync("提醒", "无样本数据可处理");
                log.Info("无一键上传样本");
                return;
            }
            ProgressDialogController controller = await mainwin.ShowProgressAsync("Please wait...", "Progress message");
            foreach (string singleID in hID)
            {
                log.Info("一键上传样本" + singleID);
                ReadAccessDS.ReadData("SAMPLE_ID", singleID);
                GlobalVariable.NoDisplaySampleID.Add(singleID);
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(1000);
            }

            Thread.Sleep(1000);
            GetNoSendData();//重新获取数据
            ReadAccessDS.CheckUnDoneSampleNum(false);//重新获取未发送样本
            await controller.CloseAsync();
            
        }
        /// <summary>
        /// 全选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_allselect_Click(object sender, RoutedEventArgs e)
        {
            foreach (var single in UploadList)
            {
                single.IsSelected = true;
            }
        }
        /// <summary>
        /// 确定上传选择样本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_upload_Click(object sender, RoutedEventArgs e)
        {
            
            if (!GlobalVariable.DSNum)
            {
                //如果没连接生化仪，会不进行此操作
                await mainwin.ShowMessageAsync("警告", "未连接生化仪");
                return;
            }
            foreach (var single in UploadList) 
            {
                if (single.IsSelected) 
                {
                    chooseList.Add(single);
                }
            }
            if (chooseList.Count == 0) 
            {
                await mainwin.ShowMessageAsync("提醒", "请选择样本");
                log.Info("无选择上传样本");
                return;
            }
            foreach(var single in chooseList)
            {
                if (single.Device == GlobalVariable.DSDeviceID)
                {
                    //只有当前连接生化仪的项目才能上传
                    log.Info("选择上传样本" + single.Sample_ID);
                    ReadAccessDS.ReadData("SAMPLE_ID", single.Sample_ID);
                    GlobalVariable.NoDisplaySampleID.Add(single.Sample_ID);
                }
            }
            ProgressDialogController controller = await mainwin.ShowProgressAsync("Please wait...", "Progress message");

            for (int i = 0; i < (chooseList.Count*5); i++)
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(100);
            }

            Thread.Sleep(500);
            GetNoSendData();//重新获取数据
            ReadAccessDS.CheckUnDoneSampleNum(false);//重新获取未发送样本
            await controller.CloseAsync();

        }
        /// <summary>
        /// 全选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var single in UploadList)
            {
                single.IsSelected = true;
            }
        }
        /// <summary>
        /// 全不选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var single in UploadList)
            {
                single.IsSelected = false;
            }
        }
        /// <summary>
        /// 未上传样本显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_viewsamole_Click(object sender, RoutedEventArgs e)
        {
            GetNoSendData();
        }
    }

    public class UpOrDownload_Show : INotifyPropertyChanged
    {
        private int _number;
        private string _Sample_ID;
        private string _Patient_ID;
        private string _Item;
        private string _Kind;
        private string _Device;
        private string _Test_Time;//字符串类型，主要用于绑定显示
        private bool _IsSelected;
        //后续数据未在表格中显示用到,也没有数据绑定
        public DateTime Send_Time;//时间类型,主要用于传递写入数据库
        public string Patient_Name;
        public bool Emergency;
        public int Patient_Age;
        public string Patient_Sex;

        public int number
        {
            get
            {
                return this._number;
            }
            set
            {
                if (this._number != value)
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
                if (this._Sample_ID != value)
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
                if (this._Patient_ID != value)
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
                if (this._Item != value)
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
                if (this._Kind != value)
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
                if (this._Device != value)
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
                if (this._IsSelected != value)
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
