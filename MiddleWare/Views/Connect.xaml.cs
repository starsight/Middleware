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
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using MiddleWare.Communicate;
using System.ComponentModel;
using System.IO.Ports;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using ADOX;
using System.Data.OleDb;
using System.Data;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using log4net.Config;
using log4net;
using System.Reflection;

namespace MiddleWare.Views
{
    
    /// <summary>
    /// Connect.xaml 的交互逻辑
    /// </summary>
    public partial class Connect
    {
        public ObservableCollection<Device> DeviceList;
        public ObservableCollection<Lis> LisList;

        private bool LisNum = false;//确保每次只有一种方式与LIS连接

        private static Socket clientSocket;
        //private bool IsSocketRun;//判断Socket是否连接
        private static byte[] recyBytes = new byte[1024];//socket接收缓冲区域
        private string host;//socket的全局参数
        private int port;//socket的全局参数

        public static SerialPort ASTMseriaPort;
        private bool IsComRun;//判断Com是否连接
        private string comname;//COM的全局参数
        private int buad;//COM的全局参数
        private int databit;//COM的全局参数
        private string stopbit;//COM的全局参数
        private string check;//COM的全局参数

        private bool IsHL7show;//判断HL7connect是否显示
        private bool IsASTMshow;//判断ASTMconnect是否显示
        private bool IsDSshow;//判断DSconnect是否显示
        private bool IsPLshow;//判断PLconnect是否显示

        private static bool IsSocketRead = false;//Socket是否在读

        private Boolean isAutoConnectLisServer = false;
        private Boolean isAutoConnectDevice = false;

        private event GlobalVariable.MessageHandler LisMessage;

        private static ManualResetEvent TimeoutObject = new ManualResetEvent(false);
        public Connect()
        {
            InitializeComponent();
            /*
             * 日志管理初始化 log4net.config
             */
            var logCfg = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config");
            XmlConfigurator.ConfigureAndWatch(logCfg);

            //创建日志记录组件实例
            ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            //记录错误日志
            log.Debug("开始日志记录");

            DeviceList = new ObservableCollection<Device>();
            LisList = new ObservableCollection<Lis>();

            combobox_device.ItemsSource = DeviceList;
            combobox_lis.ItemsSource = LisList;

            DeviceList.Add(new Device { NAME = "DS400" });
            DeviceList.Add(new Device { NAME = "DS800" });
            DeviceList.Add(new Device { NAME = "PL12" });
            DeviceList.Add(new Device { NAME = "PL16" });
            LisList.Add(new Lis { NAME = "HL7", ID = 0 });
            LisList.Add(new Lis { NAME = "ASTM", ID = 1 });

            LisMessage += Monitor.AddItemState;//同时将LIS状态发送到监控界面

            DisableButton(button_closelis);
            DisableButton(button_closedevice);
            Statusbar.SBar.LISStatus = GlobalVariable.miniUnConn;// for mini mode
            Statusbar.SBar.DeviceStatus = GlobalVariable.miniUnConn;// for mini mode

            NamedPipe.Openpipe += new NamedPipe.MessTrans(OpenDs);//管道连接异常后，自动重新打开

            ReadConnectConfigForAutoRun(); //自动连接

        }

        /// <summary>
        /// 自动连接
        /// </summary>
        public void ReadConnectConfigForAutoRun()
        {
            //创建日志记录组件实例
            ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            //记录错误日志
            log.Debug("开始自动打开程序");
            string str = AppConfig.GetAppConfig("LisServerConnectWay");
            if (str != null && str != "null") //LIS Server 连接方式 HL7 ASTM
            {
                if (str == "HL7")
                {
                    isAutoConnectLisServer = true;
                    combobox_lis.SelectedIndex = 0;
                }
                else if (str == "ASTM")
                {
                    isAutoConnectLisServer = true;
                    combobox_lis.SelectedIndex = 1;
                }
            }

            str = AppConfig.GetAppConfig("DeviceConnectType");//仪器连接类型选择
            if (str != null && str != "null") 
            {
                MessageBox.Show(str);
                switch (str)
                {
                    case "DS400":
                        {
                            MessageBox.Show("0");
                            isAutoConnectDevice = true;
                            this.combobox_device.SelectedIndex = 0;
                            break;
                        }
                    case "DS800":
                        {
                            MessageBox.Show("1");
                            isAutoConnectDevice = true;
                            combobox_device.SelectedIndex = 1;
                            break;
                        }
                    case "PL12":
                        {
                            isAutoConnectDevice = true;
                            combobox_device.SelectedIndex = 2;
                            break;
                        }
                    case "PL16":
                        {
                            isAutoConnectDevice = true;
                            combobox_device.SelectedIndex = 3;
                            break;
                        }
                    default:
                        break;
                }
            }
        }

        private void AddItem(TextBox textbox, string text)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                textbox.Clear();//先清空之前内容
                textbox.AppendText(text);//增加文本
            }));
        }
        private void EnableButton(Button button)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                button.IsEnabled = true;
            }));
        }
        private void DisableButton(Button button)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                button.IsEnabled = false;
            }));
        }

        private void combobox_lis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string liscontent = (string)combobox_lis.SelectedValue;
            switch(liscontent)
            {
                case "HL7":
                    {
                        ASTMconnect.Visibility = Visibility.Collapsed;
                        HL7connect.Visibility = Visibility.Visible;
                        IsHL7show = true;
                        IsASTMshow = false;
                    }break;
                case "ASTM":
                    {
                        HL7connect.Visibility = Visibility.Collapsed;
                        ASTMconnect.Visibility = Visibility.Visible;
                        ASTMconnect.ComSearch();//自动搜索COM口
                        IsHL7show = false;
                        IsASTMshow = true;
                    }break;
                default:break;
            }

            if (isAutoConnectLisServer)
            {
                isAutoConnectLisServer = false;
                button_openlis_Click(null, null);
            }
        }
        private void button_openlis_Click(object sender, RoutedEventArgs e)
        {
            AppConfig.UpdateAppConfig("LisServerConnectWay","null");//如果此次连接失败，下次不会自动连接

            if (LisNum)
            {
                AddItem(textbox_lisshow, "已连接LIS\r\n");
                return;
            }
            if (IsHL7show && !IsASTMshow)
            {
                //HL7connect显示
                try
                {
                    host = HL7connect.textbox_hl7ip.Text;
                    port = Convert.ToInt16(HL7connect.textbox_hl7port.Text);
                }
                catch
                {
                    AddItem(textbox_lisshow, "请正确输入\r\n");
                    return;
                }
                Task.Factory.StartNew(StartSocket);
            }
            else if (!IsHL7show && IsASTMshow && (GlobalVariable.IsASTMCom ^ GlobalVariable.IsASTMNet)) 
            {
                //ASTMconnect显示
                if (GlobalVariable.IsASTMNet && !GlobalVariable.IsASTMCom)
                {
                    //这是网口通讯模式
                    try
                    {
                        host = ASTMconnect.textbox_astmip.Text;
                        port = Convert.ToInt16(ASTMconnect.textbox_astmport.Text);
                    }
                    catch
                    {
                        AddItem(textbox_lisshow, "请正确输入\r\n");
                        return;
                    }
                    Task.Factory.StartNew(StartSocket);
                }
                else if (!GlobalVariable.IsASTMNet && GlobalVariable.IsASTMCom)
                {
                    //这是串口通讯模式
                    try
                    {
                        if (ASTMconnect.combobox_astmcom.SelectedValue == null)
                        {
                            AddItem(textbox_lisshow, "请正确输入\r\n");
                            return;
                        }
                        comname = (string)ASTMconnect.combobox_astmcom.SelectedValue;
                        buad = (int)ASTMconnect.combobox_astmbuad.SelectedValue;
                        databit = (int)ASTMconnect.combobox_astmdatabit.SelectedValue;
                        stopbit = (string)ASTMconnect.combobox_astmstopbit.SelectedValue;//需要再判断转为枚举值
                        check = (string)ASTMconnect.combobox_astmcheckbit.SelectedValue;//需要再判断转为枚举值
                    }
                    catch
                    {
                        AddItem(textbox_lisshow, "请正确输入\r\n");
                        return;
                    }
                    /*ASTM串口模式后续处理及调用函数*/
                    Task.Factory.StartNew(StartCom);
                }
            }
            else
            {
                AddItem(textbox_lisshow, "请选择连接方式\r\n");
                return;
            }
            DisableButton(button_openlis);
            EnableButton(button_closelis);
        }
        private void StartSocket()
        {
            IPAddress ip = IPAddress.Parse(host);
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                clientSocket.Connect(new IPEndPoint(ip, port));
                clientSocket.ReceiveTimeout = 500;//设置读取超时时间500ms
                clientSocket.SendTimeout = 1000;//设置发送超时时间1s
                AddItem(textbox_lisshow, "连接LIS服务器成功\r\n");
            }
            catch (SocketException se)
            {
                AddItem(textbox_lisshow, "连接失败\r\n请打开LIS服务器后重新连接\r\n");
                Statusbar.SBar.LISStatus = GlobalVariable.miniUnConn;// for mini mode
                DisableButton(button_closelis);
                EnableButton(button_openlis);
                return;
            }
            Statusbar.SBar.LISStatus = GlobalVariable.miniConn;// for mini mode
            LisNum = true;
            GlobalVariable.IsSocketRun = true;

            HL7Manager hm = new HL7Manager();//新建一个hl7操作和队列
            ProcessHL7 ph = new ProcessHL7(hm);//新建一个HL7数据操作，包括socket给LIS服务器发送数据
            if (IsHL7show && !IsASTMshow)
            {
                #region HL7传输
                ph.UpdateDB += new ProcessHL7.UpdateAccessEventHandle(WriteAccessDS.UpdateDBOut);//根据HL7服务器反馈消息修改数据库数据
                ph.UpdateDB += new ProcessHL7.UpdateAccessEventHandle(WriteAccessPL.UpdateDB);
                ph.RequestSampleData += new ProcessHL7.RequestSampleDataEventHandle(WriteAccessDS.WriteRequestSampleDataHL7);//申请样本信息传输
                ph.ProcessHL7Message += Monitor.AddItemState;
                GlobalVariable.IsHL7Run = true;
                GlobalVariable.IsASTMRun = false;
                ph.Start();//开始发送数据

                //写入配置文件
                AppConfig.UpdateAppConfig("HL7IP", host);
                AppConfig.UpdateAppConfig("HL7PORT", port.ToString());
                AppConfig.UpdateAppConfig("LisServerConnectWay", "HL7");
                #endregion
            }
            else if (!IsHL7show && IsASTMshow && GlobalVariable.IsASTMNet && !GlobalVariable.IsASTMCom)
            {
                #region ASTM传输
                ASTMManager am = new ASTMManager();
                ProcessASTM pa = new ProcessASTM(am);
                pa.UpdateDB += new ProcessASTM.UpdateAccessEventHandle(WriteAccessDS.UpdateDBOut);//根据ASTM服务器反馈消息修改数据库数据
                pa.UpdateDB += new ProcessASTM.UpdateAccessEventHandle(WriteAccessPL.UpdateDB);
                pa.RequestSampleData += new ProcessASTM.RequestSampleDataEventHandle(WriteAccessDS.WriteRequestSampleDataASTM);//申请样本信息传输
                pa.ProcessASTMMessage += Monitor.AddItemState;
                GlobalVariable.IsASTMRun = true;
                GlobalVariable.IsHL7Run = false;
                pa.Start();

                //写入配置文件
                AppConfig.UpdateAppConfig("ASTMIP", host);
                AppConfig.UpdateAppConfig("ASTMPORT", port.ToString());
                AppConfig.UpdateAppConfig("LisServerConnectWay", "ASTM");
                AppConfig.UpdateAppConfig("ASTMUpLoadWay", 0 + "");//模式--网口
                #endregion
            }

            while (GlobalVariable.IsSocketRun && (clientSocket != null)) 
            {
                if (clientSocket.Poll(50, SelectMode.SelectRead) && (!IsSocketRead))  //判断socket是否在连接状态
                //if (false) //判断socket是否在连接状态
                {
                    bool IsDisConnect = false;
                    byte[] temp = new byte[1024];
                    try
                    {
                        int nRead = clientSocket.Receive(temp);
                        if (nRead == 0) 
                        {
                            IsDisConnect = true;
                        }
                    }
                    catch
                    {
                        IsDisConnect = true;
                    }
                    try
                    {
                        if (!IsDisConnect) 
                        {
                            int nSend = clientSocket.Send(new byte[1]);
                        }
                    }
                    catch
                    {
                        clientSocket.Dispose();
                        IsDisConnect = true;
                    }
                    if (!IsDisConnect) 
                    {
                        continue;
                    }
                    LisNum = false;
                    AddItem(textbox_lisshow, "LIS服务器断开连接\r\n正在重新连接\r\n");
                    LisMessage.Invoke("LIS服务器断开连接\r\n正在重新连接\r\n", "LIS");

                    GlobalVariable.IsSocketRun = false;
                    
                    /*自动连接，如果都还不行的话，直接放弃 by LZH*/
                    for (int i = 0; i < GlobalVariable.ReLisConnectNum; ++i) 
                    {
                        Thread.Sleep(1000);
                        try
                        {
                            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            clientSocket.Connect(new IPEndPoint(ip, port));
                            clientSocket.ReceiveTimeout = 500;//设置读取超时时间500ms
                            clientSocket.SendTimeout = 1000;//设置发送超时时间1s
                            if (clientSocket.Connected)
                            {
                                break;
                            }
                        }
                        catch(Exception ex)
                        {
                            continue;
                        }
                        
                    }
                    if(clientSocket.Connected)
                    {
                        //重连成功
                        AddItem(textbox_lisshow, "连接LIS服务器成功\r\n");
                        LisMessage.Invoke("连接LIS服务器成功\r\n", "LIS");
                        GlobalVariable.IsSocketRun = true;
                        LisNum = true;
                        ph.Start();
                        continue;
                    }
                    else
                    {
                        AddItem(textbox_lisshow, "LIS服务器断开连接\r\n请确定LIS后重新连接\r\n");
                        LisMessage.Invoke("LIS服务器断开连接\r\n请确定LIS后重新连接\r\n", "LIS");
                        if (GlobalVariable.IsHL7Run && !GlobalVariable.IsASTMRun)
                        {
                            ProcessHL7.ProcessHL7Cancel.Cancel();
                            GlobalVariable.IsHL7Run = false;
                        }
                        else if (!GlobalVariable.IsHL7Run && GlobalVariable.IsASTMRun && GlobalVariable.IsASTMNet && !GlobalVariable.IsASTMCom)
                        {
                            ProcessASTM.ProcessASTMCancel.Cancel();
                            GlobalVariable.IsASTMRun = false;
                        }
                        DisableButton(button_closelis);
                        EnableButton(button_openlis);
                        Statusbar.SBar.LISStatus = GlobalVariable.miniUnConn;// for mini mode

                        break;//退出去
                    }
                }
                Thread.Sleep(100);
            }
        }
        /// <summary>
        /// 通过socket发送单个字节
        /// </summary>
        /// <param name="bit"></param>
        /// <returns></returns>
        public static bool sendSocketByte(byte bit)
        {
            try
            {
                byte[] Bit = new byte[1] { bit };
                return clientSocket.Send(Bit) > 0;
            }
            catch
            {
                return false;//发不出去
            }
        }
        /// <summary>
        /// 通过socket接收单个字节
        /// </summary>
        /// <returns>若接收不成功,则返回0</returns>
        public static byte receiveSocketByet()
        {
            IsSocketRead = true;
            try
            {
                int receiveNumber = clientSocket.Receive(recyBytes);
                if (receiveNumber == 1)
                {
                    return recyBytes[0];
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;//接收超时数据
            }
            finally
            {
                IsSocketRead = false;
            }
        }
        /// <summary>
        /// 通过socket发送字符串
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool sendSocket(string message)
        {
            try
            {
                if (GlobalVariable.SocketCode) 
                {
                    return clientSocket.Send(Encoding.ASCII.GetBytes(message)) > 0;
                }
                else
                {
                    return clientSocket.Send(Encoding.UTF8.GetBytes(message)) > 0;
                }
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 通过socket接收字符串
        /// </summary>
        /// <returns></returns>
        public static string receiveSocket()
        {
            IsSocketRead = true;
            try
            {
                int receiveNumber = clientSocket.Receive(recyBytes);
                if (GlobalVariable.SocketCode)
                {
                    return Encoding.ASCII.GetString(recyBytes, 0, receiveNumber);
                }
                else
                {
                    return Encoding.UTF8.GetString(recyBytes, 0, receiveNumber);
                }
            }
            catch
            {
                return "ERROR";//接收超时
            }
            finally
            {
                IsSocketRead = false;
            }
        }

        private void StartCom()
        {
            ASTMseriaPort = new SerialPort();
            ASTMseriaPort.PortName = comname;
            ASTMseriaPort.BaudRate = buad;
            ASTMseriaPort.DataBits = databit;
            ASTMseriaPort.ReadTimeout = 1000;
            ASTMseriaPort.WriteTimeout = 1000;

            //写入配置文件
            AppConfig.UpdateAppConfig("ASTMComBuad", buad.ToString());
            AppConfig.UpdateAppConfig("ASTMComDatabit", databit.ToString());
            AppConfig.UpdateAppConfig("ASTMComStopbit", stopbit);
            AppConfig.UpdateAppConfig("ASTMComCheck", check);
            AppConfig.UpdateAppConfig("ASTMUpLoadWay", 1+"");//模式--串口
            AppConfig.UpdateAppConfig("LisServerConnectWay", "ASTM");
            AppConfig.UpdateAppConfig("ASTMCom", comname);

            switch (stopbit)
            {
                case "1":ASTMseriaPort.StopBits = StopBits.One;break;
                case "1.5":ASTMseriaPort.StopBits = StopBits.OnePointFive;break;
                case "2":ASTMseriaPort.StopBits = StopBits.Two;break;
                default:ASTMseriaPort.StopBits = StopBits.One;break;//默认下为1
            }
            switch(check)
            {
                case "无":ASTMseriaPort.Parity = Parity.None;break;
                case "奇校验":ASTMseriaPort.Parity = Parity.Odd;break;
                case "偶校验":ASTMseriaPort.Parity = Parity.Even;break;
                default:ASTMseriaPort.Parity = Parity.None;break;//默认无校验
            }
            ASTMseriaPort.ReadTimeout = 500;
            ASTMseriaPort.WriteTimeout = 500;
            ASTMseriaPort.Open();
            AddItem(textbox_lisshow, "已打开" + comname + "串口");
            Statusbar.SBar.LISStatus = GlobalVariable.miniConn;// for mini mode
            LisNum = true;
            IsComRun = true;

            //ASTM传输
            ASTMManager am = new ASTMManager();
            ProcessASTM pa = new ProcessASTM(am);
            pa.UpdateDB += new ProcessASTM.UpdateAccessEventHandle(WriteAccessDS.UpdateDBOut);//根据ASTM服务器反馈消息修改数据库数据
            pa.UpdateDB += new ProcessASTM.UpdateAccessEventHandle(WriteAccessPL.UpdateDB);
            pa.RequestSampleData += new ProcessASTM.RequestSampleDataEventHandle(WriteAccessDS.WriteRequestSampleDataASTM);//申请样本信息传输
            pa.ProcessASTMMessage += Monitor.AddItemState;
            GlobalVariable.IsASTMRun = true;
            GlobalVariable.IsHL7Run = false;
            pa.Start();

            while(IsComRun)
            {
                if (!ASTMseriaPort.IsOpen) //用于判断串口是否连接状态
                {
                    LisNum = false;
                    AddItem(textbox_lisshow, "LIS服务器断开连接\r\n请打开LIS服务器后重新连接\r\n");
                    LisMessage.Invoke("LIS服务器断开连接\r\n请打开LIS服务器后重新连接\r\n", "LIS");
                    if (!GlobalVariable.IsHL7Run && GlobalVariable.IsASTMRun && !GlobalVariable.IsASTMNet && GlobalVariable.IsASTMCom)
                    {
                        ProcessASTM.ProcessASTMCancel.Cancel();
                        GlobalVariable.IsASTMRun = false;
                    }
                    DisableButton(button_closelis);
                    EnableButton(button_openlis);
                    Statusbar.SBar.LISStatus = GlobalVariable.miniUnConn;// for mini mode

                    break;//退出去
                }
            }

        }
        /// <summary>
        /// 通过串口发送单个字节
        /// </summary>
        /// <param name="bit"></param>
        /// <returns></returns>
        public static bool sendComByte(byte bit)
        {
            try
            {
                byte[] Bit = new byte[1] { bit };
                ASTMseriaPort.Write(Bit, 0, 1);
                return true;
            }
            catch
            {
                return false;//发送失败
            }
        }
        /// <summary>
        /// 通过串口接收单个字节
        /// </summary>
        /// <returns></returns>
        public static byte recevieComByte()
        {
            try
            {
                int receiveNumber = ASTMseriaPort.Read(recyBytes, 0, 1);
                if(receiveNumber==1)
                {
                    return recyBytes[0];
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }
        /// <summary>
        /// 通过串口发送字符串
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool sendCom(string message)
        {
            byte[] sendByte;
            if (GlobalVariable.ComCode)
            {
                sendByte = Encoding.ASCII.GetBytes(message);
            }
            else
            {
                sendByte = Encoding.UTF8.GetBytes(message);
            }
            try
            {
                if (ASTMseriaPort.IsOpen)
                {
                    ASTMseriaPort.Write(sendByte, 0, sendByte.Length);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;//发送失败
            }
            
        }
        /// <summary>
        /// 通过串口接收字符串
        /// </summary>
        /// <returns></returns>
        public static string receiveCom()
        {
            try
            {
                int receiveNumber = ASTMseriaPort.BytesToRead;
                ASTMseriaPort.Read(recyBytes, 0, receiveNumber);
                if(GlobalVariable.SocketCode)
                {
                    return Encoding.ASCII.GetString(recyBytes, 0, receiveNumber);
                }
                else
                {
                    return Encoding.UTF8.GetString(recyBytes, 0, receiveNumber);
                }
            }
            catch
            {
                return "-1";//接收超时
            }
        }
        private void button_closelis_Click(object sender, RoutedEventArgs e)
        {
            DisableButton(button_closelis);
            EnableButton(button_openlis);
            Statusbar.SBar.LISStatus = GlobalVariable.miniUnConn;// for mini mode

            if (GlobalVariable.IsHL7Run || (GlobalVariable.IsASTMNet && !GlobalVariable.IsASTMCom))
            {
                //如果是网口传输模式
                LisNum = false;
                GlobalVariable.IsSocketRun = false;//停了创建线程
                clientSocket.Close();
                //HL7 task令牌
                if (GlobalVariable.IsHL7Run && !GlobalVariable.IsASTMRun)
                {
                    //HL7 网口连接方式
                    ProcessHL7.ProcessHL7Cancel.Cancel();
                    GlobalVariable.IsHL7Run = false;
                }
                else if (!GlobalVariable.IsHL7Run && GlobalVariable.IsASTMRun && GlobalVariable.IsASTMNet && !GlobalVariable.IsASTMCom)
                {
                    //ASTM 网口连接方式
                    ProcessASTM.ProcessASTMCancel.Cancel();
                    GlobalVariable.IsASTMRun = false;
                }
                AddItem(textbox_lisshow, "已关闭与LIS服务器连接\r\n");
            }
            else if (!GlobalVariable.IsASTMNet && GlobalVariable.IsASTMCom)
            {
                //ASTM 串口连接方式
                LisNum = false;
                IsComRun = false;
                if (Connect.ASTMseriaPort != null)
                {
                    Connect.ASTMseriaPort.Close();
                }
                if (ProcessASTM.ProcessASTMCancel != null)
                {
                    ProcessASTM.ProcessASTMCancel.Cancel();
                    GlobalVariable.IsASTMRun = false;
                }
                
                AddItem(textbox_lisshow, "已关闭与LIS服务器连接\r\n");
            }
        }

        private void combobox_device_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //创建日志记录组件实例
            ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            //记录错误日志
            log.Debug("仪器连接下拉列表变化");
            MessageBox.Show("仪器连接下拉列表变化0");
            string devicecontent = (string)combobox_device.SelectedValue;
            MessageBox.Show("仪器连接下拉列表变化1");
            EnableButton(button_opendevice);
            DisableButton(button_closedevice);
            Statusbar.SBar.DeviceStatus = GlobalVariable.miniUnConn;// for mini mode

            switch (devicecontent)
            {
                case "DS400":
                case "DS800":
                    {
                        PLconnect.Visibility = Visibility.Collapsed;
                        DSconnect.Visibility = Visibility.Visible;
                        IsDSshow = true;
                        IsPLshow = false;
                        if (GlobalVariable.DSDEVICEADDRESS != null && GlobalVariable.IsContainsKey(GlobalVariable.DSDEVICEADDRESS)) //如果之前打开过DS,则使能关闭按钮
                        {
                            EnableButton(button_closedevice);
                            DisableButton(button_opendevice);
                        }
                        MessageBox.Show("仪器连接下拉列表变化2");
                    } break;
                case "PL12":
                case "PL16":
                    {
                        DSconnect.Visibility = Visibility.Collapsed;
                        PLconnect.Visibility = Visibility.Visible;
                        string str = AppConfig.GetAppConfig("PLCom");//仪器连接类型选择

                        if (str != null)
                        {
                            PLconnect.ComSearch();

                            for (int i = 0; i < PLconnect.ComList.Count; i++)
                            {
                                if (PLconnect.ComList[i].NAME == str)
                                {
                                    PLconnect.combobox_plcom.SelectedIndex = i;
                                }
                            }
                        }

                        IsPLshow = true;
                        IsDSshow = false;
                        if (GlobalVariable.IsContainsKey(GlobalVariable.PLCOM + "+" + GlobalVariable.PLBUAD.ToString())) //如果之前没有建立过
                        {
                            EnableButton(button_closedevice);
                            DisableButton(button_opendevice);
                            Statusbar.SBar.DeviceStatus = GlobalVariable.miniConn;// for mini mode
                        }
                    }
                    break;
                default:break;
            }

           if (isAutoConnectDevice)
            {
                MessageBox.Show("仪器连接下拉列表变化，自动打开按钮");
                isAutoConnectDevice = false;
                button_opendevice_Click(null, null);
                log.Debug("仪器连接下拉列表变化，自动打开按钮");
            }
        }
        private void button_opendevice_Click(object sender, RoutedEventArgs e)
        {
            //创建日志记录组件实例
            ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            log.Debug("开始打开按钮");
            AppConfig.UpdateAppConfig("DeviceConnectType", "null");

            if (IsDSshow && !IsPLshow)
            {
                try//防止为空
                {
                    GlobalVariable.DSDEVICEADDRESS = DSconnect.textbox_dsdb.Text;
                    log.Debug("获取设备数据库地址"+GlobalVariable.DSDEVICEADDRESS);
                    if (GlobalVariable.DSDEVICEADDRESS == string.Empty)
                    {
                        AddItem(textbox_deviceshow, "请输入数据库地址\r\n");
                        return;
                    }
                }
                catch
                {
                    AddItem(textbox_deviceshow, "请输入数据库地址\r\n");
                    return;
                }

                //DSConnect显示
                string pathto = GlobalVariable.topDir.Parent.FullName;
                string curFile = @pathto + "\\DSDB.mdb";
                if (File.Exists(curFile))//检测DSDB数据库是否存在
                {
                    //存在
                    AddItem(textbox_deviceshow, "生化仪数据库存在\r\n");
                }
                else
                {
                    if (!CreatDSDB()) 
                    {
                        AddItem(textbox_deviceshow, "生化仪本地数据库创建失败\r\n");
                        return;
                    }
                }

                if(GlobalVariable.DSNum)
                {
                    return;
                }
                if(!GlobalVariable.IsContainsKey(GlobalVariable.DSDEVICEADDRESS))//如果之前没有建立过此项连接
                {
                    #region 用于判断是400还是800
                    try
                    {
                        switch ((string)combobox_device.SelectedValue)
                        {
                            case "DS400": GlobalVariable.DSDEVICE = 1; break;
                            case "DS800": GlobalVariable.DSDEVICE = 0; break;
                            default: break;
                        }
                    }
                    catch
                    {
                        AddItem(textbox_deviceshow, "仪器选择错误\r\n请重试\r\n");
                        return;
                    }
                    #endregion
                    GlobalVariable.DSNum = true;
                    GlobalVariable.AddValue(GlobalVariable.DSDEVICEADDRESS, "生化");
                    //记录错误日志
                    log.Debug("开始openDs" + GlobalVariable.DSDEVICEADDRESS);
                    MessageBox.Show("开始openDs" + GlobalVariable.DSDEVICEADDRESS);
                    OpenDs(GlobalVariable.DSDEVICEADDRESS);
                }
            }
            else if (IsPLshow && !IsDSshow) 
            {
                //PLConnect显示          
                try
                {
                    
                    if (PLconnect.combobox_plcom.SelectedValue == null)
                    {
                        AddItem(textbox_deviceshow, "请输入端口号与波特率\r\n");
                        return;
                    }
                    GlobalVariable.PLCOM = (string)PLconnect.combobox_plcom.SelectedValue;
                    GlobalVariable.PLBUAD = (int)PLconnect.combobox_plbuad.SelectedValue;

                    #region 用于判断是PL12还是PL16
                    try
                    {
                        switch ((string)combobox_device.SelectedValue)
                        {
                            case "PL12": GlobalVariable.PLDEVICE = 0; break;
                            case "PL16": GlobalVariable.PLDEVICE = 1; break;
                            default: break;
                        }
                    }
                    catch
                    {
                        AddItem(textbox_deviceshow, "仪器选择错误\r\n请重试\r\n");
                        return;
                    }
                    #endregion

                }
                catch
                {
                    AddItem(textbox_deviceshow, "请输入端口号与波特率\r\n");
                    return;
                }
                string pathto = GlobalVariable.topDir.Parent.FullName;
                string curFile = @pathto + "\\PLDB.mdb";
                if (File.Exists(curFile)) //检测PLDB数据库是否存在
                {
                    //存在
                    AddItem(textbox_deviceshow, "血小板本地数据库存在\r\n");
                }
                else
                {
                    if (!CreatPLDB())
                    {
                        AddItem(textbox_deviceshow, "血小板本地数据库创建失败\r\n");
                        return;
                    }
                }

                if(GlobalVariable.PLNum)
                {
                    AddItem(textbox_deviceshow, "已存在血小板连接\r\n");
                    DisableButton(button_opendevice);
                    EnableButton(button_closedevice);
                    return;
                }
                if (!GlobalVariable.IsContainsKey(GlobalVariable.PLCOM + "+" + GlobalVariable.PLBUAD.ToString()))//如果之前没有建立过
                {
                    GlobalVariable.PLNum = true;
                    GlobalVariable.AddValue(GlobalVariable.PLCOM + "+" + GlobalVariable.PLBUAD.ToString(), "血小板");
                    OpenPl(GlobalVariable.PLCOM, GlobalVariable.PLBUAD);
                }
            }
            else
            {
                AddItem(textbox_deviceshow, "请选择连接仪器");
                return;//如果没有显示的话,直接走掉
            }
            DisableButton(button_opendevice);
            EnableButton(button_closedevice);
        }
        private void OpenDs(string DSaddress)//
        {
            //创建日志记录组件实例
            ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            //记录错误日志
            log.Debug("进行openDs" + GlobalVariable.DSDEVICEADDRESS);
            DI800Manager dm = new DI800Manager();//新建一个DI800数据操作
            AccessManagerDS amd = new AccessManagerDS();//新建一个数据库操作
            WriteAccessDS wad = new WriteAccessDS(amd);//新建一个写数据库操作
            wad.NoticeReadMessage += new WriteAccessDS.NoticeRead(ReadAccessDS.ReadData);//委托，往数据库写完一个数据，就通知去读数据库
            wad.Start();//开始写数据库操作
            log.Debug("进行openDs" + 1);
            MessageBox.Show("进行openDs" + 1);
            WriteEquipAccess wea = new WriteEquipAccess();//新建一个往仪器数据库写数据操作

            NamedPipe np = new NamedPipe();//先建一个命名管道及各种操作
            np.NamedPipeMessage += Monitor.AddItemState;//命名管道建立成功
            ProcessPipes pPipes = new ProcessPipes(np, amd);//读命名管道
            pPipes.PipeMessage += new ProcessPipes.PipeTransmit(ReadEquipAccess.ReadData);//从命名管道读到相关ID，就从仪器数据库开始读此ID信息
            
            pPipes.start();//开始读命名管道
            log.Debug("进行openDs" + 2);
            MessageBox.Show("进行openDs" + 2);

            ReadEquipAccess rea = new ReadEquipAccess(DSaddress);//必须要实例化
            ReadAccessDS rad = new ReadAccessDS(dm);//新建一个读本地数据操作及将数据写入dm队列中

            ProcessDI800s pd = new ProcessDI800s(dm);//新建一个DS数据操作，包括将数据从队列取出送给HL7实例hm
            pd.Start();
            if (GlobalVariable.IsHL7Run && !GlobalVariable.IsASTMRun)//HL7连接形式下
            {
                pPipes.PipeApplyMessage += new ProcessPipes.PipeApplyTransmit(ProcessHL7.DSRequestSampleData);//从命名管道读到申请样本信息，直接转到HL7发送
                pd.DItransmit += new ProcessDI800s.DIEventHandle(ProcessHL7.DSdataReceived);//将DS数据送给hl7处理封装
            }
            else if (!GlobalVariable.IsHL7Run && GlobalVariable.IsASTMRun && (GlobalVariable.IsASTMNet || GlobalVariable.IsASTMCom))  //ASTM连接形式下
            {
                pPipes.PipeApplyMessage += new ProcessPipes.PipeApplyTransmit(ProcessASTM.DSRequestSampleData);//从命名管道读到申请样本信息，直接转到ASTM发送
                pd.DItransmit += new ProcessDI800s.DIEventHandle(ProcessASTM.DSdataReceived);//将DS数据送给ASTM处理封装
            }
            pd.DItransmit += new ProcessDI800s.DIEventHandle(Monitor.ReceiveResult);//将数据同时传给界面显示

            log.Debug("进行openDs" + 3);
            MessageBox.Show("进行openDs" + 3);
            if (!GlobalVariable.IsDSRepeat)//只需要写一次就够了
            {
                ReadEquipAccess.ReadEquipAccessMessage += Monitor.AddItemState;//
                WriteAccessDS.WriteAccessDSMessage += Monitor.AddItemState;
                ReadAccessDS.ReadAccessDSMessage += Monitor.AddItemState;
                DSCancel.DSCancellMessage += Monitor.AddItemState;
            }

            GlobalVariable.IsDSRepeat = true;//已经连接过生化仪
            AddItem(textbox_deviceshow, "等待生化仪器连接\r\n");
            log.Debug("进行openDs" + 4);
            MessageBox.Show("进行openDs" + 4);
            //写入配置文件
            /*
            AppConfig.UpdateAppConfig("DSAddress", DSaddress);
            if (GlobalVariable.DSDEVICE == 0)
            {
                AppConfig.UpdateAppConfig("DeviceConnectType", "DS800");
            }else if(GlobalVariable.DSDEVICE == 1)
            {
                AppConfig.UpdateAppConfig("DeviceConnectType", "DS400");
            }
            */
            log.Debug("进行openDs" + 5);
            MessageBox.Show("进行openDs" + 5);
            //自动更新数据库
            MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
            if(mainwin!=null)
            {
                //打开软件时，若是自动打开DS设备，由于窗口还未加载完，引用为null，需要在MainWindow初始化中再更新数据库
                if (mainwin.SetOption!=null)
                {
                    Number_Item number_item = mainwin.SetOption.Number_Item;
                    number_item.Updata_DS_Click(true, null);//这个ture来表明不是由按钮发出的
                }
            }
            log.Debug("进行openDs" + 6);
            MessageBox.Show("进行openDs" + 6);
        }
        private void OpenPl(string com, int baud)
        {
            PLManager pm = new PLManager();//实例一个PL12原始数据队列和PL12新格式数据队列

            #region 串口初始化
            Comm comm = new Comm(pm);
            comm.CommMessage += Monitor.AddItemState;//这个COM口的状态消息传递要放在前面
            Comm.serialPort.PortName = com;//COM口
            Comm.serialPort.BaudRate = baud;//波特率
            Comm.serialPort.DataBits = 8;//数据位
            Comm.serialPort.StopBits = System.IO.Ports.StopBits.One;//1个停止位
            Comm.serialPort.Parity = System.IO.Ports.Parity.None;//无奇偶校验位
            Comm.serialPort.ReadTimeout = SerialPort.InfiniteTimeout;//读不会超时
            Comm.serialPort.WriteTimeout = SerialPort.InfiniteTimeout;//写不会超时
            #endregion

            comm.Open();//打开串口

            comm.DataReceived += new Comm.PortEventHandle(ProcessPLs.DataReceived);//将串口接收回来数据委托出去处理成PL12RAW数据

            ProcessPLs pps = new ProcessPLs(pm);//PL处理操作，主要是将PL12数据提出队列
            pps.Start();
            if (GlobalVariable.IsHL7Run && !GlobalVariable.IsASTMRun)//HL7连接形式下
            {
                pps.PLtransmit += new ProcessPLs.PLEventHandle(ProcessHL7.PLdataReceived);//把数据库读取到的数据转向HL7封装
            }
            else if (!GlobalVariable.IsHL7Run && GlobalVariable.IsASTMRun && (GlobalVariable.IsASTMNet || GlobalVariable.IsASTMCom))  //ASTM连接形式下
            {
                pps.PLtransmit += new ProcessPLs.PLEventHandle(ProcessASTM.PLdataReceived);//把数据库读到的数据转向ASTM封装
            }
            pps.PLtransmit += new ProcessPLs.PLEventHandle(Monitor.ReceiveResult);//将数据同时传给界面显示

            WriteAccessPL wap = new WriteAccessPL(pm);//实例化一个把PL12RAW数据写入数据库的类，其中从pm取PL12RAW队列
            wap.Start();
            wap.NoticeReadMessage += new WriteAccessPL.NoticeRead(ReadAccessPL.ReadData);//写入数据库后就通知别人来读数据库

            ReadAccessPL arp = new ReadAccessPL(pm);//去数据库读数据，然后将数据压入PL队列

            wap.WriteAccessPLMessage += Monitor.AddItemState;
            if (!GlobalVariable.IsPLRepeat)
            {
                ReadAccessPL.ReadAccessPLMessage += Monitor.AddItemState;
                PLCancel.PLCancellMessage += Monitor.AddItemState;
            }

            GlobalVariable.IsPLRepeat = true;
            AddItem(textbox_deviceshow, "等待血小板数据传送\r\n");

            //写入配置文件
            AppConfig.UpdateAppConfig("PLComBuad", baud.ToString());
            AppConfig.UpdateAppConfig("PLCom", com);//保存COM口
            if (GlobalVariable.PLDEVICE == 0)
            {
                AppConfig.UpdateAppConfig("DeviceConnectType", "PL12");
            }
            else if (GlobalVariable.PLDEVICE == 1)
            {
                AppConfig.UpdateAppConfig("DeviceConnectType", "PL16");
            }

        }
        private void button_closedevice_Click(object sender, RoutedEventArgs e)
        {
            DisableButton(button_closedevice);
            EnableButton(button_opendevice);
            Statusbar.SBar.DeviceStatus = GlobalVariable.miniUnConn;// for mini mode

            if (IsDSshow && !IsPLshow && GlobalVariable.DSNum)
            {
                //关闭DS连接
                GlobalVariable.DSNum = false;

                NamedPipe.DisconnectPipe(0);//关闭命名通道,里面包括了关闭所有相关线程
                AddItem(textbox_deviceshow, "与生化仪器断开连接\r\n");
                return;
            }
            else if (!IsDSshow && IsPLshow && GlobalVariable.PLNum)
            {
                //关闭PL连接
                GlobalVariable.PLNum = false;
                
                Comm.Close();//单纯关闭串口
                PLCancel.Cancell();//只是关闭了血小板的线程,但没有关闭串口
                AddItem(textbox_deviceshow, "与当前串口断开连接\r\n");
            }
        }

        /// <summary>
        /// 创建生化仪本地数据库
        /// </summary>
        private bool CreatDSDB()
        {
            AddItem(textbox_deviceshow, "正在创建生化仪本地数据库\r\n");
            ADOX.CatalogClass cat = new CatalogClass();
            string pathto = GlobalVariable.topDir.Parent.FullName;
            string curFile = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + @pathto + "\\DSDB.mdb;" + "Jet OLEDB:Engine Type=5";
            try
            {
                cat.Create(curFile);
            }
            catch (Exception ex)
            {
                AddItem(textbox_deviceshow, "创建生化仪本地数据库\r\n" + ex.Message.ToString());
                return false;
            }
            #region lisoutput表
            ADOX.Table table_lisoutput = new ADOX.Table();
            table_lisoutput.ParentCatalog = cat;
            table_lisoutput.Name = "lisoutput";

            ADOX.Column[] column_lisoutput = new ADOX.Column[23];

            column_lisoutput[0] = new ADOX.Column();
            column_lisoutput[0].ParentCatalog = cat;
            column_lisoutput[0].Name = "SAMPLE_ID";

            column_lisoutput[1] = new ADOX.Column();
            column_lisoutput[1].ParentCatalog = cat;
            column_lisoutput[1].Name = "PATIENT_ID";
            column_lisoutput[1].Properties["Nullable"].Value = true;

            column_lisoutput[2] = new ADOX.Column();
            column_lisoutput[2].ParentCatalog = cat;
            column_lisoutput[2].Name = "ITEM";

            column_lisoutput[3] = new ADOX.Column();
            column_lisoutput[3].ParentCatalog = cat;
            column_lisoutput[3].Name = "Type";

            column_lisoutput[4] = new ADOX.Column();
            column_lisoutput[4].ParentCatalog = cat;
            column_lisoutput[4].Name = "SEND_TIME";
            column_lisoutput[4].Type = DataTypeEnum.adDate;
            column_lisoutput[4].Properties["Nullable"].Value = true;

            column_lisoutput[5] = new ADOX.Column();
            column_lisoutput[5].ParentCatalog = cat;
            column_lisoutput[5].Name = "Device";

            column_lisoutput[6] = new ADOX.Column();
            column_lisoutput[6].ParentCatalog = cat;
            column_lisoutput[6].Name = "FULL_NAME";
            column_lisoutput[6].Properties["Nullable"].Value = true;

            column_lisoutput[7] = new ADOX.Column();
            column_lisoutput[7].ParentCatalog = cat;
            column_lisoutput[7].Name = "RESULT";
            column_lisoutput[7].Type = DataTypeEnum.adDouble;
            column_lisoutput[7].Properties["Nullable"].Value = true;

            column_lisoutput[8] = new ADOX.Column();
            column_lisoutput[8].ParentCatalog = cat;
            column_lisoutput[8].Name = "UNIT";
            column_lisoutput[8].Properties["Nullable"].Value = true;

            column_lisoutput[9] = new ADOX.Column();
            column_lisoutput[9].ParentCatalog = cat;
            column_lisoutput[9].Name = "NORMAL_LOW";
            column_lisoutput[9].Type = DataTypeEnum.adDouble;
            column_lisoutput[9].Properties["Nullable"].Value = true;

            column_lisoutput[10] = new ADOX.Column();
            column_lisoutput[10].ParentCatalog = cat;
            column_lisoutput[10].Name = "NORMAL_HIGH";
            column_lisoutput[10].Type = DataTypeEnum.adDouble;
            column_lisoutput[10].Properties["Nullable"].Value = true;

            column_lisoutput[11] = new ADOX.Column();
            column_lisoutput[11].ParentCatalog = cat;
            column_lisoutput[11].Name = "TIME";
            column_lisoutput[11].Type = DataTypeEnum.adDate;
            column_lisoutput[11].Properties["Nullable"].Value = true;

            column_lisoutput[12] = new ADOX.Column();
            column_lisoutput[12].ParentCatalog = cat;
            column_lisoutput[12].Name = "INDICATE";
            column_lisoutput[12].Properties["Nullable"].Value = true;

            column_lisoutput[13] = new ADOX.Column();
            column_lisoutput[13].ParentCatalog = cat;
            column_lisoutput[13].Type = DataTypeEnum.adBoolean;
            column_lisoutput[13].Name = "IsGet";

            column_lisoutput[14] = new ADOX.Column();
            column_lisoutput[14].ParentCatalog = cat;
            column_lisoutput[14].Name = "FIRST_NAME";
            column_lisoutput[14].Properties["Nullable"].Value = true;

            column_lisoutput[15] = new ADOX.Column();
            column_lisoutput[15].ParentCatalog = cat;
            column_lisoutput[15].Name = "SEX";
            column_lisoutput[15].Properties["Nullable"].Value = true;

            column_lisoutput[16] = new ADOX.Column();
            column_lisoutput[16].ParentCatalog = cat;
            column_lisoutput[16].Name = "AGE";
            column_lisoutput[16].Properties["Nullable"].Value = true;

            column_lisoutput[17] = new ADOX.Column();
            column_lisoutput[17].ParentCatalog = cat;
            column_lisoutput[17].Name = "SAMPLE_KIND";
            column_lisoutput[17].Properties["Nullable"].Value = true;

            column_lisoutput[18] = new ADOX.Column();
            column_lisoutput[18].ParentCatalog = cat;
            column_lisoutput[18].Name = "DOCTOR";
            column_lisoutput[18].Properties["Nullable"].Value = true;

            column_lisoutput[19] = new ADOX.Column();
            column_lisoutput[19].ParentCatalog = cat;
            column_lisoutput[19].Name = "AREA";
            column_lisoutput[19].Properties["Nullable"].Value = true;

            column_lisoutput[20] = new ADOX.Column();
            column_lisoutput[20].ParentCatalog = cat;
            column_lisoutput[20].Name = "BED";
            column_lisoutput[20].Properties["Nullable"].Value = true;

            column_lisoutput[21] = new ADOX.Column();
            column_lisoutput[21].ParentCatalog = cat;
            column_lisoutput[21].Name = "DEPARTMENT";
            column_lisoutput[21].Properties["Nullable"].Value = true;

            column_lisoutput[22] = new ADOX.Column();
            column_lisoutput[22].ParentCatalog = cat;
            column_lisoutput[22].Type = DataTypeEnum.adBoolean;
            column_lisoutput[22].Name = "ISSEND";

            table_lisoutput.Columns.Append(column_lisoutput[0], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[1], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[2], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[3], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[4], DataTypeEnum.adDate, 0);
            table_lisoutput.Columns.Append(column_lisoutput[5], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[6], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[7], DataTypeEnum.adDouble, 0);
            table_lisoutput.Columns.Append(column_lisoutput[8], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[9], DataTypeEnum.adDouble, 0);
            table_lisoutput.Columns.Append(column_lisoutput[10], DataTypeEnum.adDouble, 0);
            table_lisoutput.Columns.Append(column_lisoutput[11], DataTypeEnum.adDate, 0);
            table_lisoutput.Columns.Append(column_lisoutput[12], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[13], DataTypeEnum.adBoolean, 0);
            table_lisoutput.Columns.Append(column_lisoutput[14], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[15], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[16], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[17], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[18], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[19], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[20], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[21], DataTypeEnum.adVarWChar, 50);
            table_lisoutput.Columns.Append(column_lisoutput[22], DataTypeEnum.adBoolean, 0);

            //设置主键

            ADOX.Key Key_lisoutput = new ADOX.Key();
            Key_lisoutput.Columns.Append("SAMPLE_ID");
            Key_lisoutput.Columns.Append("ITEM");
            Key_lisoutput.Columns.Append("Type");
            Key_lisoutput.Columns.Append("Device");
            Key_lisoutput.Name = "PrimaryKey";
            table_lisoutput.Keys.Append(Key_lisoutput, ADOX.KeyTypeEnum.adKeyPrimary);

            cat.Tables.Append(table_lisoutput);
            
            table_lisoutput = null;
            #endregion
            #region lisinput表
            ADOX.Table table_lisinput = new ADOX.Table();
            table_lisinput.ParentCatalog = cat;
            table_lisinput.Name = "lisinput";

            ADOX.Column[] column_lisinput = new ADOX.Column[10];
            column_lisinput[0] = new ADOX.Column();
            column_lisinput[0].ParentCatalog = cat;
            column_lisinput[0].Name = "SAMPLE_ID";

            column_lisinput[1] = new ADOX.Column();
            column_lisinput[1].ParentCatalog = cat;
            column_lisinput[1].Name = "PATIENT_ID";

            column_lisinput[2] = new ADOX.Column();
            column_lisinput[2].ParentCatalog = cat;
            column_lisinput[2].Name = "FIRST_NAME";
            column_lisinput[2].Properties["Nullable"].Value = true;

            column_lisinput[3] = new ADOX.Column();
            column_lisinput[3].ParentCatalog = cat;
            column_lisinput[3].Name = "SEX";
            column_lisinput[3].Properties["Nullable"].Value = true;

            column_lisinput[4] = new ADOX.Column();
            column_lisinput[4].ParentCatalog = cat;
            column_lisinput[4].Name = "AGE";
            column_lisinput[4].Properties["Nullable"].Value = true;

            column_lisinput[5] = new ADOX.Column();
            column_lisinput[5].ParentCatalog = cat;
            column_lisinput[5].Name = "SEND_TIME";
            column_lisinput[5].Properties["Nullable"].Value = true;
            column_lisinput[5].Type = DataTypeEnum.adDate;

            column_lisinput[6] = new ADOX.Column();
            column_lisinput[6].ParentCatalog = cat;
            column_lisinput[6].Name = "EMERGENCY";
            column_lisinput[6].Type = DataTypeEnum.adBoolean;

            column_lisinput[7] = new ADOX.Column();
            column_lisinput[7].ParentCatalog = cat;
            column_lisinput[7].Name = "SAMPLE_KIND";
            column_lisinput[7].Properties["Nullable"].Value = true;

            column_lisinput[8] = new ADOX.Column();
            column_lisinput[8].ParentCatalog = cat;
            column_lisinput[8].Name = "Device";

            column_lisinput[9] = new ADOX.Column();
            column_lisinput[9].ParentCatalog = cat;
            column_lisinput[9].Name = "IsSend";
            column_lisinput[9].Type = DataTypeEnum.adBoolean;

            table_lisinput.Columns.Append(column_lisinput[0], DataTypeEnum.adVarWChar, 50);
            table_lisinput.Columns.Append(column_lisinput[1], DataTypeEnum.adVarWChar, 50);
            table_lisinput.Columns.Append(column_lisinput[2], DataTypeEnum.adVarWChar, 50);
            table_lisinput.Columns.Append(column_lisinput[3], DataTypeEnum.adVarWChar, 50);
            table_lisinput.Columns.Append(column_lisinput[4], DataTypeEnum.adVarWChar, 50);
            table_lisinput.Columns.Append(column_lisinput[5], DataTypeEnum.adDate, 0);
            table_lisinput.Columns.Append(column_lisinput[6], DataTypeEnum.adBoolean);
            table_lisinput.Columns.Append(column_lisinput[7], DataTypeEnum.adVarWChar, 50);
            table_lisinput.Columns.Append(column_lisinput[8], DataTypeEnum.adVarWChar, 50);
            table_lisinput.Columns.Append(column_lisinput[9], DataTypeEnum.adBoolean);

            //设置主键
            ADOX.Key Key_lisinput = new ADOX.Key();
            Key_lisinput.Columns.Append("SAMPLE_ID");
            Key_lisinput.Columns.Append("PATIENT_ID");
            Key_lisinput.Columns.Append("Device");
            Key_lisinput.Name = "PrimaryKey";
            table_lisinput.Keys.Append(Key_lisinput, ADOX.KeyTypeEnum.adKeyPrimary);
            cat.Tables.Append(table_lisinput);
            table_lisinput = null;
            #endregion
            #region inf_elec_bio表
            ADOX.Table table_inf_elec_bio = new ADOX.Table();
            table_inf_elec_bio.ParentCatalog = cat;
            table_inf_elec_bio.Name = "inf_elec_bio";

            ADOX.Column[] column_inf_elec_bio = new ADOX.Column[5];
            column_inf_elec_bio[0] = new ADOX.Column();
            column_inf_elec_bio[0].ParentCatalog = cat;
            column_inf_elec_bio[0].Name = "ITEM";

            column_inf_elec_bio[1] = new ADOX.Column();
            column_inf_elec_bio[1].ParentCatalog = cat;
            column_inf_elec_bio[1].Name = "Enable";
            column_inf_elec_bio[1].Type = DataTypeEnum.adBoolean;

            column_inf_elec_bio[2] = new ADOX.Column();
            column_inf_elec_bio[2].ParentCatalog = cat;
            column_inf_elec_bio[2].Name = "NORMAL_LOW";
            column_inf_elec_bio[2].Properties["Nullable"].Value = true;
            column_inf_elec_bio[2].Type = DataTypeEnum.adDouble;

            column_inf_elec_bio[3] = new ADOX.Column();
            column_inf_elec_bio[3].ParentCatalog = cat;
            column_inf_elec_bio[3].Name = "NORMAL_HIGH";
            column_inf_elec_bio[3].Properties["Nullable"].Value = true;
            column_inf_elec_bio[3].Type = DataTypeEnum.adDouble;

            column_inf_elec_bio[4] = new ADOX.Column();
            column_inf_elec_bio[4].ParentCatalog = cat;
            column_inf_elec_bio[4].Name = "SORT";
            column_inf_elec_bio[4].Properties["Nullable"].Value = true;
            column_inf_elec_bio[4].Type = DataTypeEnum.adInteger;

            table_inf_elec_bio.Columns.Append(column_inf_elec_bio[0], DataTypeEnum.adVarWChar, 50);
            table_inf_elec_bio.Columns.Append(column_inf_elec_bio[1], DataTypeEnum.adBoolean, 0);
            table_inf_elec_bio.Columns.Append(column_inf_elec_bio[2], DataTypeEnum.adDouble, 0);
            table_inf_elec_bio.Columns.Append(column_inf_elec_bio[3], DataTypeEnum.adDouble, 0);
            table_inf_elec_bio.Columns.Append(column_inf_elec_bio[4], DataTypeEnum.adInteger, 0);

            ADOX.Key Key_inf_elec_bio = new ADOX.Key();
            Key_inf_elec_bio.Columns.Append("ITEM");
            Key_inf_elec_bio.Name = "PrimaryKey";
            table_inf_elec_bio.Keys.Append(Key_inf_elec_bio, ADOX.KeyTypeEnum.adKeyPrimary);
            cat.Tables.Append(table_inf_elec_bio);
            table_inf_elec_bio = null;
            #endregion
            #region item_bio表
            ADOX.Table table_item_bio = new ADOX.Table();
            table_item_bio.ParentCatalog = cat;
            table_item_bio.Name = "item_bio";

            ADOX.Column[] column_item_bio = new ADOX.Column[2];
            column_item_bio[0] = new ADOX.Column();
            column_item_bio[0].ParentCatalog = cat;
            column_item_bio[0].Name = "ITEM";

            column_item_bio[1] = new ADOX.Column();
            column_item_bio[1].ParentCatalog = cat;
            column_item_bio[1].Name = "Device";

            table_item_bio.Columns.Append(column_item_bio[0], DataTypeEnum.adVarWChar, 50);
            table_item_bio.Columns.Append(column_item_bio[1], DataTypeEnum.adVarWChar, 50);

            ADOX.Key Key_item_bio = new ADOX.Key();
            Key_item_bio.Columns.Append("ITEM");
            Key_item_bio.Columns.Append("Device");
            Key_item_bio.Name = "PrimaryKey";
            table_item_bio.Keys.Append(Key_item_bio, ADOX.KeyTypeEnum.adKeyPrimary);
            cat.Tables.Append(table_item_bio);
            table_item_bio = null;
            #endregion
            #region item_info表
            ADOX.Table table_item_info = new ADOX.Table();
            table_item_info.ParentCatalog = cat;
            table_item_info.Name = "item_info";

            ADOX.Column[] colum_item_info = new ADOX.Column[5];
            colum_item_info[0] = new ADOX.Column();
            colum_item_info[0].ParentCatalog = cat;
            colum_item_info[0].Name = "Item";

            colum_item_info[1] = new ADOX.Column();
            colum_item_info[1].ParentCatalog = cat;
            colum_item_info[1].Name = "FullName";
            colum_item_info[1].Properties["Nullable"].Value = true;

            colum_item_info[2] = new ADOX.Column();
            colum_item_info[2].ParentCatalog = cat;
            colum_item_info[2].Name = "Index";
            colum_item_info[2].Properties["Nullable"].Value = true;

            colum_item_info[3] = new ADOX.Column();
            colum_item_info[3].ParentCatalog = cat;
            colum_item_info[3].Name = "Type";

            colum_item_info[4] = new ADOX.Column();
            colum_item_info[4].ParentCatalog = cat;
            colum_item_info[4].Name = "Device";

            table_item_info.Columns.Append(colum_item_info[0], DataTypeEnum.adVarWChar, 50);
            table_item_info.Columns.Append(colum_item_info[1], DataTypeEnum.adVarWChar, 50);
            table_item_info.Columns.Append(colum_item_info[2], DataTypeEnum.adVarWChar, 50);
            table_item_info.Columns.Append(colum_item_info[3], DataTypeEnum.adVarWChar, 50);
            table_item_info.Columns.Append(colum_item_info[4], DataTypeEnum.adVarWChar, 50);

            ADOX.Key Key_item_info = new ADOX.Key();
            Key_item_info.Columns.Append("Item");
            Key_item_info.Columns.Append("Type");
            Key_item_info.Columns.Append("Device");
            Key_item_info.Name = "PrimaryKey";
            table_item_info.Keys.Append(Key_item_info, ADOX.KeyTypeEnum.adKeyPrimary);
            cat.Tables.Append(table_item_info);
            table_item_info = null;
            #endregion
            #region listask表
            ADOX.Table table_listask = new ADOX.Table();
            table_listask.ParentCatalog = cat;
            table_listask.Name = "listask";

            ADOX.Column[] column_listask = new ADOX.Column[5];
            column_listask[0] = new ADOX.Column();
            column_listask[0].ParentCatalog = cat;
            column_listask[0].Name = "SAMPLE_ID";

            column_listask[1] = new ADOX.Column();
            column_listask[1].ParentCatalog = cat;
            column_listask[1].Name = "ITEM";

            column_listask[2] = new ADOX.Column();
            column_listask[2].ParentCatalog = cat;
            column_listask[2].Name = "Type";

            column_listask[3] = new ADOX.Column();
            column_listask[3].ParentCatalog = cat;
            column_listask[3].Name = "Device";

            column_listask[4] = new ADOX.Column();
            column_listask[4].ParentCatalog = cat;
            column_listask[4].Name = "SEND_TIME";
            column_listask[4].Properties["Nullable"].Value = true;
            column_listask[4].Type = DataTypeEnum.adDate;

            table_listask.Columns.Append("SAMPLE_ID", DataTypeEnum.adVarWChar, 50);
            table_listask.Columns.Append("ITEM", DataTypeEnum.adVarWChar, 50);
            table_listask.Columns.Append("Type", DataTypeEnum.adVarWChar, 50);
            table_listask.Columns.Append("Device", DataTypeEnum.adVarWChar, 50);
            table_listask.Columns.Append("SEND_TIME", DataTypeEnum.adDate, 0);

            ADOX.Key Key_listask = new ADOX.Key();
            Key_listask.Columns.Append("SAMPLE_ID");
            Key_listask.Columns.Append("ITEM");
            Key_listask.Columns.Append("Type");
            Key_listask.Columns.Append("Device");
            Key_listask.Columns.Append("SEND_TIME");
            Key_listask.Name = "PrimaryKey";
            table_listask.Keys.Append(Key_listask, ADOX.KeyTypeEnum.adKeyPrimary);
            cat.Tables.Append(table_listask);
            table_listask = null;
            #endregion
            cat = null;
            AddItem(textbox_deviceshow, "创建生化仪本地数据库完成\r\n");
            return true;

        }
        /// <summary>
        /// 创建血小板本地数据库
        /// </summary>
        private bool CreatPLDB()
        {
            AddItem(textbox_deviceshow, "正在创建血小板本地数据库\r\n");
            string pathto = GlobalVariable.topDir.Parent.FullName;
            string curFile = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + @pathto + "\\PLDB.mdb;" + "Jet OLEDB:Engine Type=5";
            ADOX.CatalogClass cat = new CatalogClass();
            try
            {
                cat.Create(curFile);

            }
            catch (Exception ex)
            {
                AddItem(textbox_deviceshow, "创建血小板本地数据库\r\n" + ex.Message.ToString());
                return false;
            }
            #region PL_lisoutput表
            ADOX.Table table_PL_lisoutput = new ADOX.Table();
            table_PL_lisoutput.ParentCatalog = cat;
            table_PL_lisoutput.Name = "PL_lisoutput";

            ADOX.Column[] colunm_PL_lisoutput = new ADOX.Column[14];
            colunm_PL_lisoutput[0] = new ADOX.Column();
            colunm_PL_lisoutput[0].ParentCatalog = cat;
            colunm_PL_lisoutput[0].Name = "SAMPLE_ID";

            colunm_PL_lisoutput[1] = new ADOX.Column();
            colunm_PL_lisoutput[1].ParentCatalog = cat;
            colunm_PL_lisoutput[1].Name = "BarCode";

            colunm_PL_lisoutput[2] = new ADOX.Column();
            colunm_PL_lisoutput[2].ParentCatalog = cat;
            colunm_PL_lisoutput[2].Type = DataTypeEnum.adDate;
            colunm_PL_lisoutput[2].Name = "TEST_TIME";

            colunm_PL_lisoutput[3] = new ADOX.Column();
            colunm_PL_lisoutput[3].ParentCatalog = cat;
            colunm_PL_lisoutput[3].Name = "DEVICE";

            colunm_PL_lisoutput[4] = new ADOX.Column();
            colunm_PL_lisoutput[4].ParentCatalog = cat;
            colunm_PL_lisoutput[4].Name = "AAP";
            colunm_PL_lisoutput[4].Properties["Nullable"].Value = true;

            colunm_PL_lisoutput[5] = new ADOX.Column();
            colunm_PL_lisoutput[5].ParentCatalog = cat;
            colunm_PL_lisoutput[5].Name = "SAMPLE_KIND";
            colunm_PL_lisoutput[5].Properties["Nullable"].Value = true;

            colunm_PL_lisoutput[6] = new ADOX.Column();
            colunm_PL_lisoutput[6].ParentCatalog = cat;
            colunm_PL_lisoutput[6].Name = "ITEM";

            colunm_PL_lisoutput[7] = new ADOX.Column();
            colunm_PL_lisoutput[7].ParentCatalog = cat;
            colunm_PL_lisoutput[7].Name = "FULL_NAME";
            colunm_PL_lisoutput[7].Properties["Nullable"].Value = true;

            colunm_PL_lisoutput[8] = new ADOX.Column();
            colunm_PL_lisoutput[8].ParentCatalog = cat;
            colunm_PL_lisoutput[8].Name = "RESULT";
            colunm_PL_lisoutput[8].Type = DataTypeEnum.adLongVarWChar;
            colunm_PL_lisoutput[8].Properties["Nullable"].Value = true;

            colunm_PL_lisoutput[9] = new ADOX.Column();
            colunm_PL_lisoutput[9].ParentCatalog = cat;
            colunm_PL_lisoutput[9].Name = "UNIT";
            colunm_PL_lisoutput[9].Properties["Nullable"].Value = true;

            colunm_PL_lisoutput[10] = new ADOX.Column();
            colunm_PL_lisoutput[10].ParentCatalog = cat;
            colunm_PL_lisoutput[10].Name = "NORMAL_LOW";
            colunm_PL_lisoutput[10].Type = DataTypeEnum.adDouble;
            colunm_PL_lisoutput[10].Properties["Nullable"].Value = true;

            colunm_PL_lisoutput[11] = new ADOX.Column();
            colunm_PL_lisoutput[11].ParentCatalog = cat;
            colunm_PL_lisoutput[11].Name = "NORMAL_HIGH";
            colunm_PL_lisoutput[11].Type = DataTypeEnum.adDouble;
            colunm_PL_lisoutput[11].Properties["Nullable"].Value = true;

            colunm_PL_lisoutput[12] = new ADOX.Column();
            colunm_PL_lisoutput[12].ParentCatalog = cat;
            colunm_PL_lisoutput[12].Name = "INDICATE";
            colunm_PL_lisoutput[12].Properties["Nullable"].Value = true;

            colunm_PL_lisoutput[13] = new ADOX.Column();
            colunm_PL_lisoutput[13].ParentCatalog = cat;
            colunm_PL_lisoutput[13].Type = DataTypeEnum.adBoolean;
            colunm_PL_lisoutput[13].Name = "ISSEND";

            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[0], DataTypeEnum.adVarWChar, 50);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[1], DataTypeEnum.adVarWChar, 50);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[2], DataTypeEnum.adDate, 0);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[3], DataTypeEnum.adVarWChar, 0);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[4], DataTypeEnum.adVarWChar, 50);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[5], DataTypeEnum.adVarWChar, 50);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[6], DataTypeEnum.adVarWChar, 50);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[7], DataTypeEnum.adVarWChar, 50);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[8], DataTypeEnum.adLongVarWChar, 0);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[9], DataTypeEnum.adVarWChar, 50);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[10], DataTypeEnum.adDouble, 0);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[11], DataTypeEnum.adDouble, 0);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[12], DataTypeEnum.adVarWChar, 5);
            table_PL_lisoutput.Columns.Append(colunm_PL_lisoutput[13], DataTypeEnum.adBoolean);

            //设置主键
            ADOX.Key Key_PL_lisoutput = new ADOX.Key();
            Key_PL_lisoutput.Columns.Append("SAMPLE_ID");
            Key_PL_lisoutput.Columns.Append("BarCode");
            Key_PL_lisoutput.Columns.Append("TEST_TIME");
            Key_PL_lisoutput.Columns.Append("DEVICE");
            Key_PL_lisoutput.Columns.Append("ITEM");
            Key_PL_lisoutput.Name = "PrimaryKey";
            table_PL_lisoutput.Keys.Append(Key_PL_lisoutput, ADOX.KeyTypeEnum.adKeyPrimary);
            cat.Tables.Append(table_PL_lisoutput);
            table_PL_lisoutput = null;
            #endregion
            #region PL_ExtraInfo表
            ADOX.Table table_PL_ExtraInfo = new ADOX.Table();
            table_PL_ExtraInfo.ParentCatalog = cat;
            table_PL_ExtraInfo.Name = "PL_ExtraInfo";

            ADOX.Column[] column_PL_ExtraInfo = new ADOX.Column[4];
            column_PL_ExtraInfo[0] = new ADOX.Column();
            column_PL_ExtraInfo[0].ParentCatalog = cat;
            column_PL_ExtraInfo[0].Name = "ITEM";

            column_PL_ExtraInfo[1] = new ADOX.Column();
            column_PL_ExtraInfo[1].ParentCatalog = cat;
            column_PL_ExtraInfo[1].Name = "NORMAL_LOW";
            column_PL_ExtraInfo[1].Type = DataTypeEnum.adDouble;
            column_PL_ExtraInfo[1].Properties["Nullable"].Value = true;

            column_PL_ExtraInfo[2] = new ADOX.Column();
            column_PL_ExtraInfo[2].ParentCatalog = cat;
            column_PL_ExtraInfo[2].Name = "NORMAL_HIGH";
            column_PL_ExtraInfo[2].Type = DataTypeEnum.adDouble;
            column_PL_ExtraInfo[2].Properties["Nullable"].Value = true;

            column_PL_ExtraInfo[3] = new ADOX.Column();
            column_PL_ExtraInfo[3].ParentCatalog = cat;
            column_PL_ExtraInfo[3].Name = "UNIT";
            column_PL_ExtraInfo[3].Properties["Nullable"].Value = true;

            table_PL_ExtraInfo.Columns.Append(column_PL_ExtraInfo[0], DataTypeEnum.adVarWChar, 50);
            table_PL_ExtraInfo.Columns.Append(column_PL_ExtraInfo[1], DataTypeEnum.adDouble, 0);
            table_PL_ExtraInfo.Columns.Append(column_PL_ExtraInfo[2], DataTypeEnum.adDouble, 0);
            table_PL_ExtraInfo.Columns.Append(column_PL_ExtraInfo[3], DataTypeEnum.adVarWChar, 50);

            ADOX.Key Key_PL_ExtraInfo = new ADOX.Key();
            Key_PL_ExtraInfo.Columns.Append("ITEM");
            Key_PL_ExtraInfo.Name = "PrimaryKey";
            table_PL_ExtraInfo.Keys.Append(Key_PL_ExtraInfo, ADOX.KeyTypeEnum.adKeyPrimary);
            cat.Tables.Append(table_PL_ExtraInfo);
            table_PL_ExtraInfo = null;
            #endregion
            #region PL_FullName表
            ADOX.Table table_PL_FullName = new ADOX.Table();
            table_PL_FullName.ParentCatalog = cat;
            table_PL_FullName.Name = "PL_FullName";

            ADOX.Column[] column_PL_FullName = new ADOX.Column[4];
            column_PL_FullName[0] = new ADOX.Column();
            column_PL_FullName[0].ParentCatalog = cat;
            column_PL_FullName[0].Name = "Item";

            column_PL_FullName[1] = new ADOX.Column();
            column_PL_FullName[1].ParentCatalog = cat;
            column_PL_FullName[1].Name = "FULL_NAME";
            column_PL_FullName[1].Properties["Nullable"].Value = true;

            column_PL_FullName[2] = new ADOX.Column();
            column_PL_FullName[2].ParentCatalog = cat;
            column_PL_FullName[2].Name = "Type";
            column_PL_FullName[2].Properties["Nullable"].Value = true;

            column_PL_FullName[3] = new ADOX.Column();
            column_PL_FullName[3].ParentCatalog = cat;
            column_PL_FullName[3].Name = "Index";
            column_PL_FullName[3].Properties["Nullable"].Value = true;

            table_PL_FullName.Columns.Append(column_PL_FullName[0], DataTypeEnum.adVarWChar, 50);
            table_PL_FullName.Columns.Append(column_PL_FullName[1], DataTypeEnum.adVarWChar, 50);
            table_PL_FullName.Columns.Append(column_PL_FullName[2], DataTypeEnum.adVarWChar, 50);
            table_PL_FullName.Columns.Append(column_PL_FullName[3], DataTypeEnum.adVarWChar, 50);

            ADOX.Key Key_PL_FullName = new ADOX.Key();
            Key_PL_FullName.Columns.Append("Item");
            Key_PL_FullName.Name = "PrimaryKey";
            table_PL_FullName.Keys.Append(Key_PL_FullName, ADOX.KeyTypeEnum.adKeyPrimary);
            cat.Tables.Append(table_PL_FullName);
            table_PL_FullName = null;
            #endregion

            cat = null;

            #region 写入初始信息
            string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
            strConnection += "Data Source=" + @pathto + "\\PLDB.mdb";
            OleDbConnection conn = new OleDbConnection(strConnection);
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            string strInsert = "insert into PL_FullName([Item],[FULL_NAME],[Type]) values (@Item,@FULL_NAME,@Type)";
            string strJudge = "select * from PL_FullName where [Item]='AAP'";
            XElement rootNode = XElement.Load("..\\..\\Resources\\PLDBInitial.xml");
            IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Item") select target;
            foreach (XElement node in targetNodes)
            {
                using (OleDbCommand cmd = new OleDbCommand(strInsert, conn))
                {

                    cmd.Parameters.Add("@Item", OleDbType.VarChar).Value = node.Element("NAME").Value;
                    cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = node.Element("FULL_NAME").Value;
                    cmd.Parameters.Add("@Type", OleDbType.VarChar).Value = node.Element("TYPE").Value;

                    cmd.ExecuteNonQuery();

                    /*未知bug,如果不再做一遍处理,APP项会写不进去*/
                    if (node.Element("NAME").Value == "AAP")
                    {
                        using (OleDbDataAdapter oaJudge = new OleDbDataAdapter(strJudge, conn))
                        {
                            DataSet ds = new DataSet();
                            if (oaJudge.Fill(ds) == 0)
                            {
                                //不存在AAP情况下
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            strInsert = "insert into PL_ExtraInfo([ITEM],[NORMAL_lOW],[NORMAL_HIGH],[UNIT]) values (@ITEM,@NORMAL_lOW,@NORMAL_HIGH,@UNIT)";

            Hashtable ht = new Hashtable();//利用哈希表
            string tempName;
            foreach (XElement node in targetNodes)
            {
                using (OleDbCommand cmd = new OleDbCommand(strInsert, conn))
                {
                    if (node.Element("UNIT").Value != string.Empty && node.Element("UNIT").Value != null)
                    {
                        tempName = Regex.Replace(node.Element("NAME").Value, @"_\d", string.Empty);
                        tempName = Regex.Replace(tempName, @"\d", string.Empty);
                        if (ht.ContainsKey(tempName))
                        {
                            continue;
                        }
                        cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = tempName;
                        cmd.Parameters.Add("@NORMAL_lOW", OleDbType.Double).Value = Convert.ToDouble(node.Element("NORMAL_LOW").Value);
                        cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.Double).Value = Convert.ToDouble(node.Element("NORMAL_HIGH").Value);
                        cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = node.Element("UNIT").Value;
                        ht.Add(tempName, null);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            conn.Close();
            #endregion
            AddItem(textbox_deviceshow, "创建血小板本地数据库完成\r\n");
            return true;
        }
    }

    public class Device: INotifyPropertyChanged
    {
        private string _NAME;

        public string NAME
        {
            get
            {
                return this._NAME;
            }
            set
            {
                if(this._NAME!=value)
                {
                    this._NAME = value;
                    OnPropertyChanged("NAME");
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
    public class Lis: INotifyPropertyChanged
    {
        private string _NAME;
        private int _ID;
        public string NAME
        {
            get
            {
                return this._NAME;
            }
            set
            {
                if (this._NAME != value)
                {
                    this._NAME = value;
                    OnPropertyChanged("NAME");
                }
            }
        }
        public int ID
        {
            get
            {
                return this._ID;
            }
            set
            {
                if (this._ID != value)
                {
                    this._ID = value;
                    OnPropertyChanged("ID");
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
