using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MahApps.Metro.Controls.Dialogs;

namespace MiddleWare.Views
{
    /// <summary>
    /// Number_Item.xaml 的交互逻辑
    /// </summary>
    public partial class Number_Item : UserControl
    {
        public ObservableCollection<Device> NDeviceList;
        public ObservableCollection<Item_Number_Show> item_show;
        List<Item_Number> item_total = new List<Item_Number>();//原始
        public Number_Item()
        {
            InitializeComponent();
            NDeviceList = new ObservableCollection<Device>();
            item_show = new ObservableCollection<Item_Number_Show>();

            NcomboBox.ItemsSource = NDeviceList;
            Number_dataGrid.ItemsSource = item_show;
        }
        private void combox()
        {
            NDeviceList.Clear();
            string pathto = GlobalVariable.topDir.Parent.FullName;
            string curFile = @pathto + "\\DSDB.mdb";
            if (File.Exists(curFile))//检测DSDB数据库是否存在
            {
                NDeviceList.Add(new Device { NAME = "DS400" });
                NDeviceList.Add(new Device { NAME = "DS800" });
            }
            curFile = @pathto + "\\PLDB.mdb";
            if (File.Exists(curFile))//检测PLDB数据库是否存在
            {
                //存在
                NDeviceList.Add(new Device { NAME = "PL" });
            }
        }

        /// <summary>
        /// 得到PL数据
        /// </summary>
        private async void GetPLDB()
        {
            item_show.Clear();
            List<Item_Number> item_number = new List<Item_Number>();//变量
            DataSet ds = new DataSet();
            OleDbConnection conn;
            string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
            string pathto = GlobalVariable.topDir.Parent.FullName;
            strConnection += "Data Source=" + @pathto + "\\PLDB.mdb";
            conn = new OleDbConnection(strConnection);
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            string strSelect;
            #region pl
            strSelect = "SELECT * FROM  PL_FullName";
            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
            {
                if (oa.Fill(ds, "Item") == 0)
                {
                    ds.Clear();
                    MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
                    await mainwin.ShowMessageAsync("警告", "请检查血小板数据库");
                    return;
                }
                else
                {
                    foreach (DataRow dr in ds.Tables["Item"].Rows)
                    {
                        Item_Number item = new Item_Number();
                        item.Item = dr["Item"] == DBNull.Value ? string.Empty : (string)dr["Item"];
                        item.FullName = dr["FULL_NAME"] == DBNull.Value ? string.Empty : (string)dr["FULL_NAME"];
                        item.Type = dr["Type"] == DBNull.Value ? string.Empty : (string)dr["Type"];
                        item.Index = dr["Index"] == DBNull.Value ? string.Empty : (string)dr["Index"];
                        item_number.Add(item);
                    }
                    for (int i = 0; i < item_number.Count; i++)
                    {
                        Item_Number_Show item = new Item_Number_Show();
                        item.Item = item_number[i].Item;
                        item.FullName = item_number[i].FullName;
                        item.Index = item_number[i].Index;
                        item.Type = item_number[i].Type;
                        item_show.Add(item);
                    }
                }
            }
            #endregion
            ds.Clear();
            conn.Close();
            item_total = item_number;
        }
        /// <summary>
        /// 得到DS400数据
        /// </summary>
        private async void GetDS400DB()
        {
            item_show.Clear();
            List<Item_Number> item_number = new List<Item_Number>();//显示的数据
            DataSet ds = new DataSet();
            OleDbConnection conn;
            string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
            string pathto = GlobalVariable.topDir.Parent.FullName;
            strConnection += "Data Source=" + @pathto + "\\DSDB.mdb";
            conn = new OleDbConnection(strConnection);
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            string strSelect;
            strSelect = "SELECT* FROM  item_info";
            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
            {
                if (oa.Fill(ds, "Item") == 0)
                {
                    ds.Clear();
                    MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
                    await mainwin.ShowMessageAsync("警告", "请更新生化数据库");
                }
                else
                {
                    foreach (DataRow dr in ds.Tables["Item"].Rows)
                    {
                        if ((string)dr["Device"] == "DS400")
                        {
                            Item_Number item = new Item_Number();
                            item.Item = dr["Item"] == DBNull.Value ? string.Empty : (string)dr["Item"];
                            item.FullName = dr["FullName"] == DBNull.Value ? string.Empty : (string)dr["FullName"];
                            item.Type = dr["Type"] == DBNull.Value ? string.Empty : (string)dr["Type"];
                            item.Index = dr["Index"] == DBNull.Value ? string.Empty : (string)dr["Index"];
                            item_number.Add(item);
                        }
                    }
                    for (int i = 0; i < item_number.Count; ++i)
                    {
                        Item_Number_Show item = new Item_Number_Show();
                        item.Item = item_number[i].Item;
                        item.FullName = item_number[i].FullName;
                        item.Index = item_number[i].Index;
                        item.Type = item_number[i].Type;
                        item_show.Add(item);
                    }
                }
            }
            ds.Clear();
            conn.Close();
            item_total = item_number;
        }
        /// <summary>
        /// 得到DS800数据
        /// </summary>
        private async void GetDS800DB()
        {
            item_show.Clear();
            List<Item_Number> item_number = new List<Item_Number>();//显示的数据
            DataSet ds = new DataSet();
            OleDbConnection conn;
            string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
            string pathto = GlobalVariable.topDir.Parent.FullName;
            strConnection += "Data Source=" + @pathto + "\\DSDB.mdb";
            conn = new OleDbConnection(strConnection);
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            string strSelect;
            strSelect = "SELECT* FROM  item_info";
            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
            {
                if (oa.Fill(ds, "Item") == 0)
                {
                    ds.Clear();
                    MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
                    await mainwin.ShowMessageAsync("警告", "请更新生化数据库");
                }
                else
                {
                    foreach (DataRow dr in ds.Tables["Item"].Rows)
                    {
                        if ((string)dr["Device"] == "DS800")
                        {
                            Item_Number item = new Item_Number();
                            item.Item = dr["Item"] == DBNull.Value ? string.Empty : (string)dr["Item"];
                            item.FullName = dr["FullName"] == DBNull.Value ? string.Empty : (string)dr["FullName"];
                            item.Type = dr["Type"] == DBNull.Value ? string.Empty : (string)dr["Type"];
                            item.Index = dr["Index"] == DBNull.Value ? string.Empty : (string)dr["Index"];
                            item_number.Add(item);
                        }
                    }
                    for (int i = 0; i < item_number.Count; ++i)
                    {
                        Item_Number_Show item = new Item_Number_Show();
                        item.Item = item_number[i].Item;
                        item.FullName = item_number[i].FullName;
                        item.Index = item_number[i].Index;
                        item.Type = item_number[i].Type;
                        item_show.Add(item);
                    }
                }
            }
            ds.Clear();
            conn.Close();
            item_total = item_number;
        }
        /// <summary>
        /// 确认选择设备类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Number_OK_Click(object sender, RoutedEventArgs e)
        {
            string device;
            device = (string)NcomboBox.SelectedValue;

            if (device == null)
            {
                MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
                await mainwin.ShowMessageAsync("警告", "请选择仪器");
                return;
            }
            switch (device)
            {
                case "PL":
                    {
                        GetPLDB();
                    }
                    break;
                case "DS400":
                    {
                        GetDS400DB();
                    }
                    break;
                case "DS800":
                    {
                        GetDS800DB();
                    }
                    break;
                default: break;
            }
        }
        /// <summary>
        /// 确认修改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Modefy_OK_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
            string device;
            device = (string)NcomboBox.SelectedValue;

            if (device == null)
            {
                await mainwin.ShowMessageAsync("警告", "请选择仪器");
                return;
            }
            if (device == "DS400" || device == "DS800") //DS
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                DataSet ds = new DataSet();
                OleDbConnection conn;
                string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
                string pathto = GlobalVariable.topDir.Parent.FullName;
                strConnection += "Data Source=" + @pathto + "\\DSDB.mdb";
                conn = new OleDbConnection(strConnection);
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                for (int i = 0; i < item_show.Count; i++)//用于判断是否重复
                {
                    if (item_show[i].Index != item_total[i].Index || item_show[i].Index != "" || item_show[i].Index != " ") //这样做是为了从有编号改为无编号,并且避免与已存在的重复
                    {
                        string tempIndex = item_show[i].Index;
                        if (tempIndex == "" || tempIndex == " ")
                        {
                            continue;
                        }
                        if (data.ContainsKey(tempIndex))
                        {
                            await mainwin.ShowMessageAsync("通知", "记录" + data[tempIndex].ToString() + "编号与" + item_show[i].Item.ToString() + "重复");
                            return;
                        }
                        else
                        {
                            data.Add(tempIndex, item_show[i].Item);
                        }
                    }
                }
                for (int i = 0; i < item_show.Count; i++)
                {
                    if (item_show[i].Index != item_total[i].Index)
                    {
                        string strUpdate;
                        strUpdate = "update item_info set [Index] = @Index where StrComp(Item,@Item,0)=0 AND [Device]='" + device + "'";//要加上仪器选择,以及对大小写严格区分
                        using (OleDbCommand oa = new OleDbCommand(strUpdate, conn))
                        {
                            oa.Parameters.Add("@Index", OleDbType.VarChar).Value = item_show[i].Index;
                            oa.Parameters.Add("@Item", OleDbType.VarChar).Value = item_show[i].Item;
                            try
                            {
                                oa.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                await mainwin.ShowMessageAsync("警告", ex.ToString());
                                return;
                            }
                        }
                    }
                }
                conn.Close();
                await mainwin.ShowMessageAsync("警告", "修改成功");
            }
            else if (device == "PL") //PL
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                DataSet ds1 = new DataSet();
                OleDbConnection conn;
                string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
                string pathto = GlobalVariable.topDir.Parent.FullName;
                strConnection += "Data Source=" + @pathto + "\\PLDB.mdb";
                conn = new OleDbConnection(strConnection);
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                for (int i = 0; i < item_show.Count; i++)//用于判断是否重复
                {
                    if (item_show[i].Index != item_total[i].Index || item_show[i].Index != "" || item_show[i].Index != " ") //这样做是为了从有编号改为无编号,并且避免与已存在的重复
                    {
                        string tempIndex = item_show[i].Index;
                        if (tempIndex == "" || tempIndex == " ")
                        {
                            continue;
                        }
                        if (data.ContainsKey(tempIndex))
                        {
                            await mainwin.ShowMessageAsync("通知", "记录" + data[tempIndex].ToString() + "编号与" + item_show[i].Item.ToString() + "重复");
                            return;
                        }
                        else
                        {
                            data.Add(tempIndex, item_show[i].Item);
                        }
                    }
                }
                for (int i = 0; i < item_show.Count; i++)
                {
                    if (item_show[i].Index != item_total[i].Index)
                    {
                        string strUpdate;
                        strUpdate = "update PL_FullName set [Index] = @Index where StrComp(Item,@Item,0)=0";//加上对大小写严格区分
                        using (OleDbCommand oa = new OleDbCommand(strUpdate, conn))
                        {
                            oa.Parameters.Add("@Index", OleDbType.VarChar).Value = item_show[i].Index;
                            oa.Parameters.Add("@Item", OleDbType.VarChar).Value = item_show[i].Item;
                            try
                            {
                                oa.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                await mainwin.ShowMessageAsync("警告", ex.ToString());
                                return;
                            }
                        }
                    }
                }
                conn.Close();
                await mainwin.ShowMessageAsync("警告", "修改成功");
            }
        }
        /// <summary>
        /// 取消修改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Modefy_ESC_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < item_total.Count; i++)
            {
                item_show[i].Index = item_total[i].Index;
            }
        }
        /// <summary>
        /// 更新生化仪数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Updata_DS_Click(object sender, RoutedEventArgs e)
        {
            int num = 0;
            string strConnection;
            string strSelect;
            string insert;
            DataSet ds = new DataSet();
            OleDbConnection conn;
            MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
            List<Item_Number> item_number = new List<Item_Number>();//缓存
            if (GlobalVariable.DSDEVICE != 0 && GlobalVariable.DSDEVICE != 1)
            {
                await mainwin.ShowMessageAsync("警告", "未连接生化仪");
                return;
            }
            else if (GlobalVariable.DSDEVICE == 0) //DS800运行
            {
                strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
                strConnection += "Data Source=";
                strConnection += GlobalVariable.DSDEVICEADDRESS;

                conn = new OleDbConnection(strConnection);
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                #region bio
                strSelect = "SELECT * FROM  BioItem";
                using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                {
                    if (oa.Fill(ds, "Item") != 0)
                    {
                        foreach (DataRow dr in ds.Tables["Item"].Rows)
                        {
                            Item_Number item = new Item_Number();
                            item.Item = dr["Item"] == DBNull.Value ? string.Empty : (string)dr["Item"];
                            item.FullName = dr["FullName"] == DBNull.Value ? string.Empty : (string)dr["FullName"];
                            item.Type = "bio";
                            item.Index = string.Empty;
                            item.Device = "DS800";
                            item_number.Add(item);
                        }
                    }
                }
                ds.Clear();
                #endregion
                #region ele
                strSelect = "SELECT * FROM  ElecItem";
                using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                {
                    if (oa.Fill(ds, "Item") != 0)
                    {
                        foreach (DataRow dr in ds.Tables["Item"].Rows)
                        {
                            Item_Number item = new Item_Number();
                            item.Item = dr["Item"] == DBNull.Value ? string.Empty : (string)dr["Item"];
                            item.FullName = dr["FullName"] == DBNull.Value ? string.Empty : (string)dr["FullName"];
                            item.Type = "ele";
                            item.Index = string.Empty;
                            item.Device = "DS800";
                            item_number.Add(item);
                        }
                    }
                }
                ds.Clear();
                #endregion
                #region cal
                strSelect = "SELECT * FROM  CalItem";
                using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                {
                    if (oa.Fill(ds, "Item") != 0)
                    {
                        foreach (DataRow dr in ds.Tables["Item"].Rows)
                        {
                            Item_Number item = new Item_Number();
                            item.Item = dr["Item"] == DBNull.Value ? string.Empty : (string)dr["Item"];
                            item.FullName = dr["FullName"] == DBNull.Value ? string.Empty : (string)dr["FullName"];
                            item.Type = "cal";
                            item.Index = string.Empty;
                            item.Device = "DS800";
                            item_number.Add(item);
                        }
                    }
                }
                ds.Clear();
                #endregion
                conn.Close();
            }
            else if (GlobalVariable.DSDEVICE == 1) //DS400运行
            {
                strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
                strConnection += "Data Source=";
                strConnection += GlobalVariable.DSDEVICEADDRESS;

                conn = new OleDbConnection(strConnection);
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                #region bio
                strSelect = "SELECT * FROM  ITEM_PARA_MAIN";
                using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                {
                    if (oa.Fill(ds, "Item") != 0)
                    {
                        foreach (DataRow dr in ds.Tables["Item"].Rows)
                        {
                            Item_Number item = new Item_Number();
                            item.Item = dr["Item"] == DBNull.Value ? string.Empty : (string)dr["Item"];
                            item.FullName = dr["FULL_NAME"] == DBNull.Value ? string.Empty : (string)dr["FULL_NAME"];
                            item.Type = "bio";
                            item.Index = string.Empty;
                            item.Device = "DS400";
                            item_number.Add(item);
                        }
                    }
                }
                ds.Clear();
                #endregion
                #region ele
                strSelect = "SELECT * FROM  ITEM_PARA_ELECTROLYTE";
                using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                {
                    if (oa.Fill(ds, "Item") != 0)
                    {
                        foreach (DataRow dr in ds.Tables["Item"].Rows)
                        {
                            Item_Number item = new Item_Number();
                            item.Item = dr["Item"] == DBNull.Value ? string.Empty : (string)dr["Item"];
                            item.FullName = dr["FULL_NAME"] == DBNull.Value ? string.Empty : (string)dr["FULL_NAME"];
                            item.Type = "ele";
                            item.Index = string.Empty;
                            item.Device = "DS400";
                            item_number.Add(item);
                        }
                    }
                }
                ds.Clear();
                #endregion 
                #region cal
                strSelect = "SELECT * FROM  ITEM_CAL_PARA";
                using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                {
                    if (oa.Fill(ds, "Item") != 0)
                    {
                        foreach (DataRow dr in ds.Tables["Item"].Rows)
                        {
                            Item_Number item = new Item_Number();
                            item.Item = dr["Item"] == DBNull.Value ? string.Empty : (string)dr["Item"];
                            item.FullName = dr["FULL_NAME"] == DBNull.Value ? string.Empty : (string)dr["FULL_NAME"];
                            item.Type = "cal";
                            item.Index = string.Empty;
                            item.Device = "DS400";
                            item_number.Add(item);
                        }
                    }
                }
                ds.Clear();
                #endregion
                conn.Close();
            }
            strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
            string pathto = GlobalVariable.topDir.Parent.FullName;
            strConnection += "Data Source=" + @pathto + "\\DSDB.mdb";
            conn = new OleDbConnection(strConnection);
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();
            }
            for (int i = 0; i < item_number.Count; ++i)
            {
                strSelect = "SELECT* FROM  item_info WHERE [Item]='" + item_number[i].Item + "' AND [Type]='" + item_number[i].Type + "' AND [Device]='" + item_number[i].Device + "'";
                using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                {
                    if (oa.Fill(ds, "Item") == 0)
                    {
                        //如果DS数据库没有这个ITEM,此时就添加进去
                        ++num;
                        ds.Clear();
                        insert = "insert into item_info ([Item],[FullName],[Index],[Type],[Device])" +
                                "values (@Item,@FullName,@Index,@Type,@Device)";
                        using (OleDbCommand cmd = new OleDbCommand(insert, conn))
                        {
                            cmd.Parameters.Add("@Item", OleDbType.VarChar).Value = item_number[i].Item;
                            cmd.Parameters.Add("@FullName", OleDbType.VarChar).Value = item_number[i].FullName;
                            cmd.Parameters.Add("@Index", OleDbType.VarChar).Value = item_number[i].Index;
                            cmd.Parameters.Add("@Type", OleDbType.VarChar).Value = item_number[i].Type;
                            cmd.Parameters.Add("@Device", OleDbType.VarChar).Value = item_number[i].Device;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            conn.Close();
            if (GlobalVariable.DSDEVICE == 0)
            {
                //DS800
                GetDS800DB();
            }
            else if (GlobalVariable.DSDEVICE == 1)
            {
                //DS400
                GetDS400DB();
            }
            await mainwin.ShowMessageAsync("通知", "更新生化仪数据成功\r\n\r\n共更新" + num.ToString() + "条数据");
        }
        /// <summary>
        /// 点击仪器选择下拉框时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NcomboBox_DropDownOpened(object sender, EventArgs e)
        {
            combox();
        }
    }
    public class Item_Number_Show : INotifyPropertyChanged
    {
        private string _Item;
        private string _FullName;
        private string _Index;
        private string _Type;

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
        public string FullName
        {
            get
            {
                return this._FullName;
            }
            set
            {
                if(this._FullName!=value)
                {
                    this._FullName = value;
                    OnPropertyChanged("FullName");
                }
            }
        }
        public string Index
        {
            get
            {
                return this._Index;
            }
            set
            {
                if(this._Index!=value)
                {
                    this._Index = value;
                    OnPropertyChanged("Index");
                }
            }
        }
        public string Type
        {
            get
            {
                return this._Type;
            }
            set
            {
                if(this._Type!=value)
                {
                    this._Type = value;
                    OnPropertyChanged("Type");
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
    public class Item_Number
    {
        public string Item;
        public string FullName;
        public string Index;
        public string Type;
        public string Device;
    }
}
