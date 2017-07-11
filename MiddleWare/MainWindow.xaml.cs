using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Input;
using MiddleWare.Views;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiddleWare.Communicate;
using MahApps.Metro.Controls.Dialogs;

namespace MiddleWare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static bool shouldClose;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!shouldClose)//close按钮
            {
                e.Cancel = true;
                this.ShowInTaskbar = false;//任务栏不显示
                this.WindowState = WindowState.Minimized;

                GlobalVariable.isMiniMode = true;
                notificationIcon.MenuItems[0].Text = "完整模式";//菜单栏文字更新
                // 隐藏自己(父窗体)
                //this.Visibility = System.Windows.Visibility.Hidden;
                mini = new FloatMiniWindow();
                //mini.ShowDialog();
                showMiniWindow();
            }         
        }
        /// <summary>
        /// 双击还原
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotificationAreaIcon_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (this.WindowState == WindowState.Minimized && !GlobalVariable.isMiniMode)
                {
                    this.ShowInTaskbar = true;//恢复状态栏显示
                    this.Show();
                    this.WindowState = WindowState.Normal;
                }
            }
        }
        public void MenuItem_Click_Close(object sender, EventArgs e)
        {
            shouldClose = true;
            if (Connect.ASTMseriaPort != null)
                Connect.ASTMseriaPort.Close();
            if (ProcessASTM.ProcessASTMCancel != null)
                ProcessASTM.ProcessASTMCancel.Cancel();
            Close();
        }

        private void MenuItem_Click_AboutUs(object sender, EventArgs e)
        {
            //MessageBox.Show("", "");
            this.ShowMessageAsync("关于软件", "江苏英诺华医疗技术有限公司");
        }

        public static FloatMiniWindow mini;
        public void MenuItem_Click_Mini(object sender, EventArgs e)
        {
            if (!GlobalVariable.isMiniMode)
            {
                notificationIcon.MenuItems[0].Text = "完整模式";//菜单栏文字更新
                GlobalVariable.isMiniMode = true;

                // 隐藏自己(父窗体)
                this.Visibility = System.Windows.Visibility.Hidden;

                mini = new FloatMiniWindow();
                // mini.ShowDialog();
                showMiniWindow();

            }
            else
            {
                notificationIcon.MenuItems[0].Text = "mini模式";//菜单栏文字更新
                GlobalVariable.isMiniMode = false;
                if (mini != null)
                    mini.Close();

                this.Visibility = Visibility.Visible;
                this.Show();
                this.WindowState = WindowState.Normal;
            }
        }

        private void showMiniWindow()
        {
            if (mini == null)
                return;

            if (mini.ShowDialog() == false)
            {
                notificationIcon.MenuItems[0].Text = "mini模式";//菜单栏文字更新
                GlobalVariable.isMiniMode = false;
                if (mini != null)
                    mini.Close();

                if (!shouldClose)
                {
                    this.Visibility = Visibility.Visible;
                    this.Show();
                    this.WindowState = WindowState.Normal;
                }

            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)//最小化
            {
                this.Hide();
            }
        }

        /*
         * 关闭打开的弹窗
         */
        protected override void OnClosed(EventArgs e)
        {
            var collections = Application.Current.Windows;

            foreach (Window window in collections)
            {
                if (window != this)
                    window.Close();
            }

            base.OnClosed(e);
        }
    }
}
