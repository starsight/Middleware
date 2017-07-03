using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Input;
using MiddleWare.Views;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiddleWare.Communicate;

namespace MiddleWare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        bool shouldClose;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!shouldClose)
            {
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;//状态栏不显示
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
                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                    this.ShowInTaskbar = true;//恢复状态栏显示
                }
            }
        }
        private void MenuItem_Click_Close(object sender, EventArgs e)
        {
            this.shouldClose = true;
            if(Connect.ASTMseriaPort!=null)
                Connect.ASTMseriaPort.Close();
            if(ProcessASTM.ProcessASTMCancel != null)
            ProcessASTM.ProcessASTMCancel.Cancel();
            Close();
        }

        private void MenuItem_Click_AboutUs(object sender, EventArgs e)
        {
            MessageBox.Show("江苏英诺华医疗技术有限公司", "关于软件");
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
