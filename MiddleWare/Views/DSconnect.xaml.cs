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
using System.Windows.Forms;

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
            
            if (AppConfig.GetAppConfig("DSAddress") != null)
            {
                this.textbox_dsdb.Text = AppConfig.GetAppConfig("DSAddress");
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
