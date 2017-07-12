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
        public static FloatMiniWindow mini;

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
                //if (this.WindowState == WindowState.Minimized && !GlobalVariable.isMiniMode)
                if (this.WindowState == WindowState.Minimized)
                {
                    if(GlobalVariable.isMiniMode)
                    {
                        //如果在mini模式下,需要恢复还原
                        notificationIcon.MenuItems[0].Text = "mini模式";//菜单栏文字更新
                        GlobalVariable.isMiniMode = false;
                        if (mini != null)
                        {
                            mini.Close();
                        }
                    }
                    this.Visibility = Visibility.Visible;
                    this.ShowInTaskbar = true;//恢复状态栏显示
                    this.Show();
                    this.WindowState = WindowState.Normal;
                }
            }
        }

        /// <summary>
        /// 退出软件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MenuItem_Click_Close(object sender, EventArgs e)
        {
            shouldClose = true;
            if (Connect.ASTMseriaPort != null)
            {
                Connect.ASTMseriaPort.Close();
            }
            if (ProcessASTM.ProcessASTMCancel != null)
            {
                ProcessASTM.ProcessASTMCancel.Cancel();
            }
            Close();
        }

        /// <summary>
        /// 关于软件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_AboutUs(object sender, EventArgs e)
        {
            notificationIcon.MenuItems[0].Text = "mini模式";//菜单栏文字更新
            GlobalVariable.isMiniMode = false;
            if (mini != null)
            {
                mini.Close();
            }
            this.Visibility = Visibility.Visible;
            this.Show();
            this.WindowState = WindowState.Normal;
            this.ShowMessageAsync("关于软件", "江苏英诺华医疗技术有限公司");
        }

        /// <summary>
        /// 模式切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MenuItem_Click_Mini(object sender, EventArgs e)//完整与MINI切换
        {
            if (!GlobalVariable.isMiniMode)
            {
                //变小
                notificationIcon.MenuItems[0].Text = "完整模式";//菜单栏文字更新
                GlobalVariable.isMiniMode = true;

                // 隐藏自己(父窗体)
                this.Visibility = System.Windows.Visibility.Hidden;
                mini = new FloatMiniWindow();
                showMiniWindow();//显示mini窗口
            }
            else
            {
                notificationIcon.MenuItems[0].Text = "mini模式";//菜单栏文字更新
                GlobalVariable.isMiniMode = false;
                if (mini != null)
                {
                    mini.Close();
                }
                this.Visibility = Visibility.Visible;//恢复正常显示
                this.Show();
                this.WindowState = WindowState.Normal;
            }
        }

        /// <summary>
        /// 显示mini模式
        /// </summary>
        private void showMiniWindow()
        {
            if (mini == null)
            {
                return;
            }
            if (mini.ShowDialog() == false)
            {
                //如果mini切换失败
                notificationIcon.MenuItems[0].Text = "mini模式";//菜单栏文字更新
                GlobalVariable.isMiniMode = false;
                if (mini != null)
                {
                    mini.Close();
                } 
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
