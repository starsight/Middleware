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
using System.Collections;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using MiddleWare.Views;
using System.IO;
using System.ComponentModel;
using Newtonsoft.Json;
using RestSharp;
using MahApps.Metro.Controls.Dialogs;

namespace MiddleWare.Views
{

    public partial class Query
    {
        private ObservableCollection<Device> QDeviceList;
        private ObservableCollection<dataSource> DataSourceList;

        public struct single_result//单个检测项目集合
        {
            public string item;//项目编号
            public string fullname;//项目全称
            public string result;//结果
            public string unit;//单位
            public string normal_low;//正常值低
            public string normal_high;//正常高
            public string indicate;//评价
            public string issend;//是否发送
        }
        public struct single_record//单个记录集合
        {
            //整合DS和PL数据
            public string sample_ID;//样本号
            public string patiennt_ID;//病人ID
            public string patient_Name;//病人姓名
            public string patient_Sex;//病人性别
            public string test_Device;//检测设备
            public string test_Time;//检测时间
            public string test_kind;//项目类型  
            public string test_aap;//诱聚剂
            public string barcode;//条码号
            public string age;//病人年龄
            public string sample_kind;//样本类型
            public string doctor;//送检医生
            public string area;//病区
            public string bed;//病床
            public string department;//科室
            public string type;//项目类型 比如生化血小板
            public List<single_result> result;
        }
        List<single_record> Result = new List<single_record>();

        public Query()
        {
            InitializeComponent();
            QDeviceList = new ObservableCollection<Device>();
            DataSourceList = new ObservableCollection<dataSource>();

            comboBox_device.ItemsSource = QDeviceList;
            QDataGrid.ItemsSource = DataSourceList;
        }
        
        /// <summary>
        /// 更新下拉仪器列表
        /// </summary>
        private void combox()
        {
            QDeviceList.Clear();
            bool IsDSDB = false;
            bool IsPLDB = false;
            string pathto = GlobalVariable.currentDir.FullName;
            string curFile = @pathto + "\\DSDB.mdb";
            if (File.Exists(curFile))//检测DSDB数据库是否存在
            {
                QDeviceList.Add(new Device { NAME = "生化分析仪" });
                IsDSDB = true;
            }
            curFile = @pathto + "\\PLDB.mdb";
            if (File.Exists(curFile))//检测PLDB数据库是否存在
            {
                //存在
                QDeviceList.Add(new Device { NAME = "血小板分析仪" });
                IsPLDB = true;
            }
            if (IsPLDB && IsDSDB) 
            {
                QDeviceList.Add(new Device { NAME = "所有仪器" });
            }
        }
        /// <summary>
        /// 从数据库取数据
        /// </summary>
        /// <param name="device">仪器类型</param>
        /// <param name="date">时间</param>
        /// <param name="ID">样本号</param>
        private async void getdata_database(string device, DateTime date, string ID)
        {
            Result.Clear();
            //result = new List<signal_record>();
            string strSelect;
            string numID = string.Empty;
            string Endtime = date.ToString("d") + " 23:59:59";
            string blank = string.Empty;
            #region DS
            if (device == "生化分析仪" || device == "所有仪器") //生化数据库获取数据
            {
                DataSet ds = new DataSet();
                OleDbConnection conn;
                string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
                string pathto = GlobalVariable.currentDir.FullName;
                strConnection += "Data Source=" + @pathto + "\\DSDB.mdb";
                conn = new OleDbConnection(strConnection);
                if (conn.State == System.Data.ConnectionState.Closed)
                {
                    conn.Open();
                }
                if (ID == string.Empty)
                {
                    strSelect = "SELECT * FROM LISOUTPUT WHERE [SEND_TIME] BETWEEN #" + date.ToString() + "# AND #" + Endtime + "# ORDER BY [SAMPLE_ID]";
                }
                else
                {
                    strSelect = "SELECT * FROM LISOUTPUT WHERE [SEND_TIME] BETWEEN #" + date.ToString() + "# AND #" + Endtime + "# AND [SAMPLE_ID]='" + ID + "' ORDER BY [SAMPLE_ID]";
                }
                using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                {
                    Hashtable recordHash = new Hashtable();
                    if (oa.Fill(ds, "LISOUTPUT") == 0)
                    {
                        if (device != "所有仪器")
                        {
                            MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
                            await mainwin.ShowMessageAsync("通知", "这个ID没有数据");
                        }
                        ds.Clear();
                    }
                    else
                    {
                        single_record record = new single_record();//记录一条ID的数据
                        record.result = new List<single_result>();
                        foreach (DataRow dr in ds.Tables["LISOUTPUT"].Rows)
                        {
                            #region

                            string tempID = dr["SAMPLE_ID"].ToString() + dr["SEND_TIME"].ToString();

                            if (!tempID.Equals(numID))//因为一条SAMPLEID很多数据,避免重复
                            {
                                if (numID != string.Empty)//不是第一次进来
                                {
                                    //Result.Add(record);//把上一次记录添加进去
                                    if(recordHash.Contains(numID))
                                    {
                                        recordHash.Remove(numID);
                                    }
                                    recordHash.Add(numID, record);
                                    if(recordHash.Contains(tempID))
                                    {
                                        //如果之前有这个样本数据的话
                                        record = (single_record)recordHash[tempID];
                                    }
                                    else
                                    {
                                        record = new single_record();
                                        record.result = new List<single_result>();
                                    }
                                }
                                numID = tempID;
                                record.type = dr["Type"] == DBNull.Value ? blank : (string)dr["Type"];
                                record.sample_ID = dr["SAMPLE_ID"] == DBNull.Value ? blank : (string)dr["SAMPLE_ID"];
                                record.patiennt_ID = dr["PATIENT_ID"] == DBNull.Value ? blank : (string)dr["PATIENT_ID"];
                                record.patient_Name = dr["FIRST_NAME"] == DBNull.Value ? blank : (string)dr["FIRST_NAME"];
                                record.patient_Sex = dr["SEX"] == DBNull.Value ? blank : (string)dr["SEX"];
                                record.test_Device = dr["Device"] == DBNull.Value ? blank : (string)dr["Device"];
                                record.test_Time = dr["SEND_TIME"] == DBNull.Value ? DateTime.Now.ToString() : dr["SEND_TIME"].ToString();
                                record.test_kind = dr["SAMPLE_KIND"] == DBNull.Value ? blank : (string)dr["SAMPLE_KIND"];
                                record.doctor = dr["DOCTOR"] == DBNull.Value ? blank : (string)dr["DOCTOR"];
                                record.area = dr["AREA"] == DBNull.Value ? blank : (string)dr["AREA"];
                                record.bed = dr["BED"] == DBNull.Value ? blank : (string)dr["BED"];
                                record.department = dr["DEPARTMENT"] == DBNull.Value ? blank : (string)dr["DEPARTMENT"];
                            }
                            single_result singleResult = new single_result();
                            singleResult.item = dr["ITEM"] == DBNull.Value ? blank : (string)dr["ITEM"];
                            singleResult.fullname = dr["FULL_NAME"] == DBNull.Value ? blank : (string)dr["FULL_NAME"];
                            singleResult.result = dr["RESULT"] == DBNull.Value ? blank : dr["RESULT"].ToString();
                            singleResult.unit = dr["UNIT"] == DBNull.Value ? blank : (string)dr["UNIT"];
                            singleResult.normal_high = dr["NORMAL_HIGH"] == DBNull.Value ? blank : dr["NORMAL_HIGH"].ToString();
                            singleResult.normal_low = dr["NORMAL_lOW"] == DBNull.Value ? blank : dr["NORMAL_lOW"].ToString();
                            singleResult.indicate = dr["INDICATE"] == DBNull.Value ? blank : (string)dr["INDICATE"];
                            singleResult.issend = dr["ISSEND"] == DBNull.Value ? blank : dr["ISSEND"].ToString();
                            record.result.Add(singleResult);
                            #endregion
                        }
                        if (record.sample_ID != string.Empty)
                        {
                            List<single_record> recordList = new List<single_record>();//用于排序
                            //把最后一次的记录加进来
                            string tempID = record.sample_ID + record.test_Time;
                            if(recordHash.Contains(tempID))
                            {
                                recordHash.Remove(tempID);
                            }
                            recordHash.Add(tempID, record);
                            //把哈希队列内的记录添加到表格
                            foreach(single_record temp in recordHash.Values)
                            {
                                recordList.Add(temp);
                            }
                            /*排序操作,降序*/
                            recordList.Sort(delegate (single_record x, single_record y)
                            {
                                return y.test_Time.CompareTo(x.test_Time);
                            });
                            foreach(single_record temp in recordList)
                            {
                                Result.Add(temp);
                            }
                        }
                    }
                }
                ds.Clear();
                conn.Close();
            }
            #endregion
            #region PL
            if (device == "血小板分析仪" || device == "所有仪器") //血小板数据库获取数据
            {
                string pathto = GlobalVariable.currentDir.FullName;
                numID = string.Empty;
                DataSet ds = new DataSet();
                OleDbConnection conn;
                string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";

                strConnection += "Data Source=" + @pathto + "\\PLDB.mdb";
                conn = new OleDbConnection(strConnection);
                if (conn.State == System.Data.ConnectionState.Closed)
                {
                    conn.Open();
                }
                if (ID == string.Empty)
                {
                    strSelect = "SELECT * FROM [PL_lisoutput] WHERE [TEST_TIME] BETWEEN #" + date.ToString() + "# AND #" + Endtime + "# ORDER BY [SAMPLE_ID]";
                }
                else
                {
                    strSelect = "SELECT * FROM [PL_lisoutput] WHERE [TEST_TIME] BETWEEN #" + date.ToString() + "# AND #" + Endtime + "# AND [SAMPLE_ID]='" + ID + "' ORDER BY [SAMPLE_ID]";
                }
                using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                {
                    if (oa.Fill(ds, "LISOUTPUT") == 0)
                    {
                        if (device != "所有仪器")
                        {
                            MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
                            await mainwin.ShowMessageAsync("通知", "这个ID没有数据");
                        }
                        ds.Clear();
                    }
                    else
                    {
                        single_record record = new single_record();
                        record.result = new List<single_result>();

                        foreach (DataRow dr in ds.Tables["LISOUTPUT"].Rows)
                        {
                            #region
                            string tempID = dr["SAMPLE_ID"] == DBNull.Value ? blank : (string)dr["SAMPLE_ID"];
                            if (!tempID.Equals(numID))
                            {
                                if (numID != string.Empty)
                                {
                                    Result.Add(record);
                                    record = new single_record();
                                    record.result = new List<single_result>();
                                    //record.result.Clear();
                                }
                                numID = tempID;
                                record.type = "血小板";
                                record.sample_ID = dr["SAMPLE_ID"] == DBNull.Value ? blank : (string)dr["SAMPLE_ID"];
                                record.barcode = dr["BarCode"] == DBNull.Value ? blank : (string)dr["BarCode"];
                                record.test_Device = dr["DEVICE"] == DBNull.Value ? blank : (string)dr["DEVICE"];
                                record.test_Time = dr["TEST_TIME"] == DBNull.Value ? DateTime.Now.ToString() : dr["TEST_TIME"].ToString();
                                record.test_aap = dr["AAP"] == DBNull.Value ? blank : (string)dr["AAP"];
                                record.test_kind = (string)dr["SAMPLE_KIND"] == "1" ? "检测结果" : blank;
                            }
                            single_result singleResult = new single_result();
                            singleResult.item = dr["ITEM"] == DBNull.Value ? blank : (string)dr["ITEM"];
                            singleResult.fullname = dr["FULL_NAME"] == DBNull.Value ? blank : (string)dr["FULL_NAME"];
                            singleResult.result = dr["RESULT"] == DBNull.Value ? blank : (string)dr["RESULT"];
                            singleResult.unit = dr["UNIT"] == DBNull.Value ? blank : (string)dr["UNIT"];
                            singleResult.normal_high = dr["NORMAL_HIGH"] == DBNull.Value ? blank : dr["NORMAL_HIGH"].ToString();
                            singleResult.normal_low = dr["NORMAL_lOW"] == DBNull.Value ? blank : dr["NORMAL_lOW"].ToString();
                            singleResult.indicate = dr["INDICATE"] == DBNull.Value ? blank : (string)dr["INDICATE"];
                            singleResult.issend = dr["ISSEND"] == DBNull.Value ? blank : dr["ISSEND"].ToString();
                            //Result[len1 - 1].result[len1 - 1] = singleResult;
                            record.result.Add(singleResult);
                            #endregion
                        }
                        if (record.sample_ID != string.Empty)
                        {
                            Result.Add(record);//把最后一条记录加进来
                        }
                    }
                }
                ds.Clear();
                conn.Close();
            }
            #endregion
            DataSourceList.Clear();
            int i = 0;
            foreach (var name in Result)//更新数据表格
            {
                dataSource data = new dataSource();
                data.number_ID = (++i).ToString();
                data.patient_ID = name.patiennt_ID;
                data.patient_Name = name.patient_Name;
                data.patient_Sex = name.patient_Sex;
                data.sample_ID = name.sample_ID;
                data.test_Device = name.test_Device;
                data.test_Kind = name.test_kind;
                data.test_Time = name.test_Time;
                DataSourceList.Add(data);
            }
        }
        /// <summary>
        /// 双击某行展开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_GotFocus(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                var item = (DataGridRow)sender;
                int len = Result.Count;
                if (len == 0)
                    return;
                FrameworkElement objElement = QDataGrid.Columns[0].GetCellContent(item);
                if (objElement != null)
                {
                    TextBlock objChk = (TextBlock)objElement;
                    string index = objChk.Text;
                    if (!index.Equals(string.Empty)) 
                    {
                        int num = int.Parse(index) - 1;
                        Query_detail detail = new Query_detail(Result[num]);
                        detail.ShowDialog();
                    }
                }
            }
        }
        /// <summary>
        /// 点击查询按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_query_Click(object sender, RoutedEventArgs e)
        {
            string device;
            DateTime date;
            string ID;
            /*string json = "{\"SAMPLE_ID\":\"201702160017\",\"ITEM\":\"TP\",\"DEVICE\":\"DS_800\"}";
            var client = new RestClient();
            client.BaseUrl = new Uri(GlobalVariable.BaseUrl);//http://localhost:8080/MiddlewareWeb
            var request = new RestRequest("DS/DSResult", Method.PUT);
            request.AddParameter("dsJSON", json);
            //request.AddJsonBody(json);
            //client.ExecuteAsyncGet();
            IRestResponse response = client.Execute(request);*/

            /*var tempEntity = new
            {
                SAMPLE_ID = "000001",
                ITEM = "AAR",
                DEVICE = "PL_12",
            };*/
            //string json = JsonConvert.SerializeObject(tempEntity);
            //string json = "{\"SAMPLE_ID\":\"2017-05-24 21:14:29\"}";
            /*var client = new RestClient();
            client.BaseUrl = new Uri(GlobalVariable.BaseUrl);//http://localhost:8080/MiddlewareWeb
            var request = new RestRequest("PL/PLResultByTime", Method.GET);
            request.AddParameter("plJSON", "2017-05-24 21:14:29");
            IRestResponse response = client.Execute(request);*/

            try
            {
                device = ((string)(comboBox_device.SelectedValue));
                if (device == null) 
                {
                    MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
                    await mainwin.ShowMessageAsync("通知", "请选择需要查询的仪器类型！！");
                }
            }
            catch
            {
                MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
                await mainwin.ShowMessageAsync("通知", "请选择需要查询的仪器类型！！");
            }
            try
            {
                date = Query_datetime.SelectedDate.Value;
            }
            catch
            {
                MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
                await mainwin.ShowMessageAsync("通知", "请选择需要查询的日期！！");
            }
            ID = Query_ID.Text;
            device = ((string)(comboBox_device.SelectedValue));
            date = Query_datetime.SelectedDate.Value;
            getdata_database(device, date, ID);
        }
        /// <summary>
        /// 点击仪器选择下拉框时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox_device_DropDownOpened(object sender, EventArgs e)
        {
            //启动刷新
            combox();
        }
    }

    public class dataSource : INotifyPropertyChanged
    {
        private string _number_ID;
        private string _sample_ID;
        private string _patient_ID;
        private string _patient_Name;
        private string _patient_Sex;
        private string _test_Device;
        private string _test_Kind;
        private string _test_Time;

        public string number_ID
        {
            get
            {
                return this._number_ID;
            }
            set
            {
                if(this._number_ID!=value)
                {
                    this._number_ID = value;
                    OnPropertyChanged("number_ID");
                }
            }
        }
        public string sample_ID
        {
            get
            {
                return this._sample_ID;
            }
            set
            {
                if(this._sample_ID!=value)
                {
                    this._sample_ID = value;
                    OnPropertyChanged("sample_ID");
                }
            }
        }
        public string patient_ID
        {
            get
            {
                return this._patient_ID;
            }
            set
            {
                if(this._patient_ID!=value)
                {
                    this._patient_ID = value;
                    OnPropertyChanged("patient_ID");
                }
            }
        }
        public string patient_Name
        {
            get
            {
                return this._patient_Name;
            }
            set
            {
                if(this._patient_Name!=value)
                {
                    this._patient_Name = value;
                    OnPropertyChanged("patient_Name");
                }
            }
        }
        public string patient_Sex
        {
            get
            {
                return this._patient_Sex;
            }
            set
            {
                if(this._patient_Sex!=value)
                {
                    this._patient_Sex = value;
                    OnPropertyChanged("patient_Sex");
                }
            }
        }
        public string test_Device
        {
            get
            {
                return this._test_Device;
            }
            set
            {
                if(this._test_Device!=value)
                {
                    this._test_Device = value;
                    OnPropertyChanged("test_Device");
                }
            }
        }
        public string test_Kind
        {
            get
            {
                return this._test_Kind;
            }
            set
            {
                if(this._test_Kind!=value)
                {
                    this._test_Kind = value;
                    OnPropertyChanged("test_Kind");
                }
            }
        }
        public string test_Time
        {
            get
            {
                return this._test_Time;
            }
            set
            {
                if(this._test_Time!=value)
                {
                    this._test_Time = value;
                    OnPropertyChanged("test_Time");
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
