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

namespace MiddleWare.Views
{
    /// <summary>
    /// SetOption.xaml 的交互逻辑
    /// </summary>

    public partial class SetOption : UserControl
    {
        public SetOption()
        {
            InitializeComponent();
        }

        private void number_item_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Personal_Set.Visibility = Visibility.Collapsed;
            About.Visibility = Visibility.Collapsed;
            Number_Item.Visibility = Visibility.Visible;
        }

        private void personal_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Number_Item.Visibility = Visibility.Collapsed;
            About.Visibility = Visibility.Collapsed;
            Personal_Set.Visibility = Visibility.Visible;
        }

        private void about_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Personal_Set.Visibility = Visibility.Collapsed;
            Number_Item.Visibility = Visibility.Collapsed;
            About.Visibility = Visibility.Visible;
        }
    }
}
