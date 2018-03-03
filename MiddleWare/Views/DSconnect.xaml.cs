﻿using System;
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
using System.Windows.Forms;
using System.IO;
using log4net;
using System.Reflection;

namespace MiddleWare.Views
{
    /// <summary>
    /// DSconnect.xaml 的交互逻辑
    /// </summary>
    public partial class DSconnect
    {
        public DSconnect()
        {
            InitializeComponent();
            string abadFile = GlobalVariable.topDir.FullName + "\\ABAD.mdb";
            if (File.Exists(abadFile))//检测DSDB数据库是否存在
            {
                //存在
                this.textbox_dsdb.Text = abadFile;
            }
            else
            {
                //上一级目录没有数据情况下
                string str = AppConfig.GetAppConfig("DeviceConnectType");
                //&& str != "null" 
                if (str != null && AppConfig.GetAppConfig("DSAddress") != null) 
                {
                    string dsAddress = AppConfig.GetAppConfig("DSAddress");
                    if(File.Exists(dsAddress))
                    {
                        //如果是一个真实的文件地址
                        this.textbox_dsdb.Text = AppConfig.GetAppConfig("DSAddress");
                    }
                }
            }
        }

        private void button_openfile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "Access Files (*.mdb)|*.mdb"
            };
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                this.textbox_dsdb.Text = openFileDialog.FileName;
            }
        }
    }
}
