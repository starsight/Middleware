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
using System.IO;
using log4net.Config;
using log4net;
using System.Reflection;

namespace MiddleWare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private bool shouldClose = false;

        public MainWindow()
        {
            InitializeComponent();

            /*
             * 日志管理初始化 log4net.config
             */
            var logCfg = new FileInfo(AppDomain.CurrentDomain.BaseDirectory+ "log4net.config");
            XmlConfigurator.ConfigureAndWatch(logCfg);
            //创建日志记录组件实例
            ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            //读取设备INI文件，获取设备型号
            IniFiles hardwareINI = new IniFiles(GlobalVariable.currentDir+ "//hardware.ini");
            if(hardwareINI.IsRead())
            {
                GlobalVariable.DSDeviceID = hardwareINI.ReadString("Device", "deviceID", GlobalVariable.DSDeviceID);
            }
            //记录日志
            log.Info(string.Format("Device is {0}",GlobalVariable.DSDeviceID));

            log.Info("Init mainwindow finish.");

            //开始自动连接
            this.Connect.ReadConnectConfigForAutoRun();

        }
        public static FloatMiniWindow mini;

        private async void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!shouldClose)//close按钮
            {
                e.Cancel = true;

                MessageDialogResult clickresult = await this.ShowMessageAsync("警告", "确定是否退出软件", MessageDialogStyle.AffirmativeAndNegative);
                if (clickresult == MessageDialogResult.Negative)//取消
                {
                    this.ShowInTaskbar = false;//任务栏不显示
                    this.WindowState = WindowState.Minimized;

                    GlobalVariable.isMiniMode = true;
                    notificationIcon.MenuItems[0].Text = "完整模式";//菜单栏文字更新
                    mini = new FloatMiniWindow();
                    showMiniWindow();
                }
                else//确认
                {
                    shouldClose = true;
                    Close();
                }
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
            this.ShowInTaskbar = true;//状态栏显示
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
                this.ShowInTaskbar = false;//状态栏不显示
                GlobalVariable.isMiniMode = true;

                // 隐藏自己(父窗体)
                this.Visibility = System.Windows.Visibility.Hidden;
                mini = new FloatMiniWindow();
                showMiniWindow();//显示mini窗口
            }
            else
            {
                notificationIcon.MenuItems[0].Text = "mini模式";//菜单栏文字更新
                this.ShowInTaskbar = true;//状态栏显示
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
            if (mini.ShowDialog() == false)//此处是阻塞函数，打开mini窗口后直接阻塞在这，等待close关闭后，就返回false
            {
                //如果mini切换失败
                notificationIcon.MenuItems[0].Text = "mini模式";//菜单栏文字更新
                this.ShowInTaskbar = true;//状态栏显示
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
                //this.Hide();  
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
