using System;
using System.Collections.Generic;
using System.Data.OleDb;
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
using System.Data;
using System.Collections.ObjectModel;
using MiddleWare.Communicate;
using System.Collections;
using MahApps.Metro.Controls.Dialogs;
using System.Threading;

namespace MiddleWare.Views
{
    /// <summary>
    /// OneKeyDownload.xaml 的交互逻辑
    /// </summary>
    public partial class OneKeyDownload : UserControl
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
        public ObservableCollection<UpOrDownload_Show> DownloadList;
        private List<UpOrDownload_Show> BackStageList;

        public OneKeyDownload()
        {
            InitializeComponent();
            DownloadList = new ObservableCollection<UpOrDownload_Show>();
            chooseList = new List<UpOrDownload_Show>();
            BackStageList = new List<UpOrDownload_Show>();

            datagrid_download.ItemsSource = DownloadList;

            strConnection += "Data Source=" + @pathto + "\\DSDB.mdb";
            conn = new OleDbConnection(strConnection);

            grid_download.DataContext = Statusbar.SBar;

        }
        /// <summary>
        /// 确定未下发样本,flag为true代表需要显示出来，为false代表不需要显示
        /// </summary>
        private void GetNoIssueData(bool flag)
        {
            AccessManagerDS.mutex.WaitOne();
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            num = 0;
            ds = new DataSet();
            if(flag)
            {
                DownloadList.Clear();
            }
            else
            {
                BackStageList.Clear();
            }
            string strSelect = "select * from lisinput where [IsSend]= false";
            HashSet<string> hsID = new HashSet<string>();//哈希表用来保存各个样本号(非重复)
            Hashtable htID = new Hashtable();
            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
            {
                if(oa.Fill(ds,"Down")==0)
                {
                    //已经全部下发
                    ds.Clear();
                    conn.Close();
                    AccessManagerDS.mutex.ReleaseMutex();
                    return;
                }
                else
                {
                    //还没有完全下发
                    //用哈希表来消除重复
                    foreach (DataRow dr in ds.Tables["Down"].Rows)
                    {
                        hsID.Add(dr["SAMPLE_ID"].ToString());
                    }
                }
                ds.Clear();
            }
            DataSet singleds;
            foreach(string singleID in hsID)
            {
                strSelect = "select * from listask where [SAMPLE_ID]= '" + singleID + "'";
                singleds = new DataSet();
                using (OleDbDataAdapter singleoa = new OleDbDataAdapter(strSelect, conn))
                {
                    if (singleoa.Fill(singleds, "single") != 0) 
                    {
                        string tempItem;
                        string tempAllItem;
                        foreach (DataRow dr in singleds.Tables["single"].Rows)
                        {
                            tempItem = dr["ITEM"].ToString();
                            if (!htID.Contains(singleID))
                            {
                                //如果之前不存在
                                htID.Add(singleID, tempItem);
                            }
                            else
                            {
                                tempAllItem = htID[singleID].ToString();
                                tempAllItem += ("," + tempItem);
                                htID[singleID] = tempAllItem;
                            }
                        }
                        singleds.Clear();
                    }
                }
            }
            DataSet tempds;
            foreach(string tempID in htID.Keys)
            {
                singleSample = new UpOrDownload_Show();
                singleSample.Sample_ID = tempID;
                singleSample.Item = htID[tempID].ToString();
                singleSample.number = ++num;
                singleSample.IsSelected = false;
                strSelect = "select * from lisinput where [IsSend]= false and [SAMPLE_ID] ='" + tempID + "'";
                using (OleDbDataAdapter tempoa = new OleDbDataAdapter(strSelect, conn))
                {
                    tempds = new DataSet();
                    if (tempoa.Fill(tempds, "temp") != 0) 
                    {
                        foreach(DataRow dr in tempds.Tables["temp"].Rows)
                        {
                            singleSample.Test_Time = dr["SEND_TIME"] == DBNull.Value ? DateTime.Now.ToString() : dr["SEND_TIME"].ToString();
                            singleSample.Patient_ID = dr["PATIENT_ID"] == DBNull.Value ? blank : (string)dr["PATIENT_ID"];
                            singleSample.Device = dr["Device"] == DBNull.Value ? blank : (string)dr["Device"];
                            singleSample.Kind = dr["SAMPLE_KIND"] == DBNull.Value ? blank : (string)dr["SAMPLE_KIND"];
                            singleSample.Patient_Age = dr["AGE"] == DBNull.Value ? 0 : Convert.ToInt32((string)dr["AGE"]);
                            singleSample.Patient_Name = dr["FIRST_NAME"] == DBNull.Value ? blank : (string)dr["FIRST_NAME"];
                            singleSample.Patient_Sex = dr["SEX"] == DBNull.Value ? blank : (string)dr["SEX"];
                            if((bool)dr["EMERGENCY"]==true)
                            {
                                singleSample.Emergency = "Y";
                            }
                            else
                            {
                                singleSample.Emergency = "N";
                            }
                            break;
                        }
                    }
                    tempds.Clear();
                }
                if (flag)
                {
                    DownloadList.Add(singleSample);
                    Statusbar.SBar.NoIssueNum = DownloadList.Count();//检查更新一下
                }
                else
                {
                    BackStageList.Add(singleSample);
                    Statusbar.SBar.NoIssueNum = DownloadList.Count();//检查更新一下
                }
            }
            conn.Close();
            AccessManagerDS.mutex.ReleaseMutex();
        }
        /// <summary>
        /// 一键下发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_onekeydownload_Click(object sender, RoutedEventArgs e)
        {
            if (!GlobalVariable.DSNum)
            {
                //如果没连接生化仪，会不进行此操作
                await mainwin.ShowMessageAsync("警告", "未连接生化仪");
                return;
            }

            GetNoIssueData(false);//查看未下发样本，但不显示出来
            if (BackStageList.Count == 0) 
            {
                await mainwin.ShowMessageAsync("提醒", "无样本数据可处理");
                return;
            }
            
            ProgressDialogController controller = await mainwin.ShowProgressAsync("Please wait...", "Progress message");
            foreach (var single in BackStageList)
            {
                WriteEquipAccess.WriteApplySampleDS(single.Sample_ID, single.Patient_ID, single.Patient_Name, single.Patient_Age,
                    single.Patient_Sex, single.Item, Convert.ToDateTime(single.Test_Time), single.Emergency);//去写入到设备数据库
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(500);
            }

            Thread.Sleep(500);
            DownloadList.Clear();
            ReadAccessDS.CheckUnDoneSampleNum(true);//重新获取未发送样本

            await controller.CloseAsync();
        }
        /// <summary>
        /// 未下发样本显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_viewsamole_Click(object sender, RoutedEventArgs e)
        {
            GetNoIssueData(true);
        }
        /// <summary>
        /// 全选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_allselect_Click(object sender, RoutedEventArgs e)
        {
            foreach (var single in DownloadList)
            {
                single.IsSelected = true;
            }
        }
        /// <summary>
        /// 确定选择样本下发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_download_Click(object sender, RoutedEventArgs e)
        {
            if (!GlobalVariable.DSNum)
            {
                //如果没连接生化仪，会不进行此操作
                await mainwin.ShowMessageAsync("警告", "未连接生化仪");
                return;
            }
            foreach (var single in DownloadList)
            {
                if (single.IsSelected)
                {
                    chooseList.Add(single);
                }
            }
            if (chooseList.Count == 0)
            {
                await mainwin.ShowMessageAsync("提醒", "请选择样本");
                return;
            }
            ProgressDialogController controller = await mainwin.ShowProgressAsync("Please wait...", "Progress message");
            foreach (var single in chooseList)
            {
                WriteEquipAccess.WriteApplySampleDS(single.Sample_ID, single.Patient_ID, single.Patient_Name, single.Patient_Age, 
                    single.Patient_Sex, single.Item, Convert.ToDateTime(single.Test_Time), single.Emergency);//去写入到设备数据库
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(500);
            }

            Thread.Sleep(500);
            GetNoIssueData(true);//重新获取数据
            ReadAccessDS.CheckUnDoneSampleNum(true);//重新获取未发送样本

            await controller.CloseAsync();
        }
        /// <summary>
        /// 全选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach(var single in DownloadList)
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
            foreach(var single in DownloadList)
            {
                single.IsSelected = false;
            }
        }
    }
}
