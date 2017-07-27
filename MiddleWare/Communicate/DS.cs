﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.IO.Pipes;
using MiddleWare.Views;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Forms;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

//3个线程 2个队列 1个用于从仪器数据库读取写入到自己数据的的DI800队列  1个是从自己数据库读取然后发送的DI800队列
namespace MiddleWare.Communicate
{
    public class NamedPipe
    {
        public event GlobalVariable.MessageHandler NamedPipeMessage;
        public static NamedPipeServerStream pipeServer;
        public static NamedPipeServerStream pipeServer_write;

        public static string Pipename = "sinnowa";
        public static string Pipename_write = "sinnowa_write";

        public static int pipe_read = 0;//读管道开关情况
        public static int pipe_write = 0;//写管道开关情况
        public static bool run_status = false;//命名管道整体开关情况

        public delegate void MessTrans(string str);
        public static event MessTrans Openpipe;//用于自动打开软件连接
        /// <summary>
        /// 命名通道传送数据结构体
        /// </summary>
        public struct PipeMessage
        {
            public int CheckBit;//检验位 0：生化仪上传，1：MiddleWare下发样本信息，2：生化仪要求关闭MiddleWare，3：生化仪发来生化仪器标识ID
            public int GetNewData;//是否得到新数据  0:有新的数据结果  1:申请样本信息
            public int GetTestEnd;//是否测试完成  若发送新的数据结果，此位必须置1
            public int GetTestType;//数据类型  0:生化  1:电解质  2:质控   3:定标
            public int UploadEnd;//是否上传LIS完成 生化端发送信息后再收到的信息中此位为1才代表传输正确
            public int Device;//仪器 0:DS800  1:DS400
            public int NotUse1;
            public int NotUse2;
            public int NotUse3;
            public int IDNum;//ID号位数
            public string ID;//ID号
            public int BarCodeNum;//条形码位数
            public string BarCode;//条形码
        }
        /// <summary>
        /// 修改发送结构体成员变量
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tempID"></param>
        public void CreatSinnowa(ref PipeMessage data, int _CheckBit, int _GetNewData, int _GetTestEnd, int _GetTestType, int _UploadEnd, int _Device, string _ID, string _BarCode)
        {
            data.CheckBit = _CheckBit;
            data.GetNewData = _GetNewData;
            data.GetTestEnd = _GetTestEnd;
            data.GetTestType = _GetTestType;
            data.UploadEnd = _UploadEnd;
            data.Device = _Device;
            data.NotUse1 = 0;
            data.NotUse2 = 0;
            data.NotUse3 = 0;
            data.ID = _ID;
            data.IDNum = data.ID.Length;
            data.BarCode = _BarCode;
            data.BarCodeNum = data.BarCode.Length;
        }
        /// <summary>
        /// 从命名管道读结构体
        /// </summary>
        /// <param name="pipeServer"></param>
        /// <param name="receiveData"></param>
        public void ReadNamedPipe(NamedPipeServerStream pipeServer, ref PipeMessage receiveData)
        {
            if (!run_status)
            {
                return;
            }
            StreamReader sr = new StreamReader(pipeServer);//准备读通道
            char[] buf = new char[100];//读取缓存区
            try
            {
                sr.Read(buf, 0, 100);//最多读取位数
            }
            catch
            {
                NamedPipeMessage.BeginInvoke("命名管道已关闭\r\n", "DEVICE", null, null);
                DisconnectPipe(1);//非正常关闭，对面取消了连接，被动关闭
                return;
            }
            bool flag = false;//接受是否为空，true代表不为空，false代表为空
            for (int i = 0; i < 100; ++i)
            {
                if(buf[i]!='\0')
                {
                    flag = true;
                    break;
                }
            }
            if (!flag && run_status) 
            {
                NamedPipeMessage.BeginInvoke("命名管道已关闭\r\n", "DEVICE", null, null);
                DisconnectPipe(1);//非正常关闭，对面取消了连接，被动关闭
                return;
            }

            if (run_status)
            {
                //再一次判断
                string recDataStr = new string(buf);
                receiveData.CheckBit = Convert.ToInt32(recDataStr.Substring(0, 1));
                receiveData.GetNewData = Convert.ToInt32(recDataStr.Substring(1, 1));
                receiveData.GetTestEnd = Convert.ToInt32(recDataStr.Substring(2, 1));
                receiveData.GetTestType = Convert.ToInt32(recDataStr.Substring(3, 1));
                receiveData.UploadEnd = Convert.ToInt32(recDataStr.Substring(4, 1));
                receiveData.Device = Convert.ToInt32(recDataStr.Substring(5, 1));
                receiveData.NotUse1 = Convert.ToInt32(recDataStr.Substring(6, 1));
                receiveData.NotUse2 = Convert.ToInt32(recDataStr.Substring(7, 1));
                receiveData.NotUse3 = Convert.ToInt32(recDataStr.Substring(8, 1));
                receiveData.IDNum = Convert.ToInt32(recDataStr.Substring(9, 2));
                receiveData.ID = recDataStr.Substring(11, receiveData.IDNum);
                receiveData.BarCodeNum = Convert.ToInt32(recDataStr.Substring(11 + receiveData.IDNum, 2));
                receiveData.BarCode = recDataStr.Substring(13 + receiveData.IDNum, receiveData.BarCodeNum);
            }
        }
        /// <summary>
        /// 往命名通道写结构体
        /// </summary>
        /// <param name="pipeServer"></param>
        /// <param name="sendData"></param>
        public void WriteNamedPipe(NamedPipeServerStream pipeServer, ref PipeMessage sendData)
        {
            StreamWriter sw = new StreamWriter(pipeServer);//准备写通道
            sw.AutoFlush = true;

            string sendDataStr = string.Empty;
            sendDataStr += sendData.CheckBit.ToString();
            sendDataStr += sendData.GetNewData.ToString();
            sendDataStr += sendData.GetTestEnd.ToString();
            sendDataStr += sendData.GetTestType.ToString();
            sendDataStr += sendData.UploadEnd.ToString();
            sendDataStr += sendData.Device.ToString();
            sendDataStr += sendData.NotUse1.ToString();
            sendDataStr += sendData.NotUse2.ToString();
            sendDataStr += sendData.NotUse3.ToString();
            if (sendData.IDNum < 10)
            {
                sendDataStr += "0";
            }
            sendDataStr += sendData.IDNum.ToString();
            sendDataStr += sendData.ID;
            if (sendData.BarCodeNum < 10)
            {
                sendDataStr += "0";
            }
            sendDataStr += sendData.BarCodeNum.ToString();
            sendDataStr += sendData.BarCode;

            sw.WriteLine(sendDataStr);
        }
        /// <summary>
        /// 创建一个命名通道
        /// </summary>
        public void NamedPipeCreat(object name,object name_write)
        {
            pipe_read = 0;//0代表没有建立管道，1代表建立管道并连接，2代表建立管道未连接
            pipe_write = 0;//0代表没有建立管道，1代表建立管道并连接，2代表建立管道未连接
            run_status = false;
            try
            {
                Thread.Sleep(500);//延迟500ms，等待一下。要不跑的太快了。
                pipeServer = new NamedPipeServerStream(name.ToString(), PipeDirection.InOut, 1);
                pipe_read = 2;//等于2代表有读命名管道，但不代表已经连接
                pipeServer_write = new NamedPipeServerStream(name_write.ToString(), PipeDirection.InOut, 1);
                NamedPipeMessage.Invoke("命名管道创建中\r\n", "DEVICE");
                
                pipeServer.WaitForConnection();//等待连接
                
                //创建一个写的管道
                if (pipeServer.IsConnected && pipe_read == 2)
                {
                    pipe_write = 2;
                    pipeServer_write.WaitForConnection();//等待连接
                    
                }
                if (pipeServer.IsConnected && pipeServer_write.IsConnected && pipe_read == 2 && pipe_write == 2)
                {
                    NamedPipeMessage.Invoke("命名管道建立成功\r\n", "DEVICE");

                    Statusbar.SBar.DeviceStatus = GlobalVariable.miniConn;// for mini mode

                    pipe_read = 1;
                    pipe_write = 1;
                    run_status = true;

                    /*此处添加数据库查询处理  未上传样本和未下发样本*/
                    ReadAccessDS.CheckUnDoneSampleNum(false);
                    ReadAccessDS.CheckUnDoneSampleNum(true);
                    
                }
            }
            catch(Exception e)
            {
                string str = e.Message.ToString();
                NamedPipeMessage.Invoke("命名管道建立异常\r\n正在重新打开\r\n", "DEVICE");
                DisconnectPipe(2);
            }
        }
        /// <summary>
        /// 取消命名管道连接以及与DS相关的线程
        /// </summary>
        public static void DisconnectPipe(int CloseStatus)
        {
            //CloseStatus:0  主动正常关闭；1：被动关闭；2：主动异常关闭
            //1和2状态下需要重新建立连接
            run_status = false;
            DSCancel.Cancell();
            Statusbar.SBar.DeviceStatus = GlobalVariable.miniUnConn;// for mini mode
            if (!pipeServer.IsConnected && pipe_read == 2)
            {
                connectSelf(false);//重要
            }

            pipeServer.Close();
            pipeServer.Dispose();
            if (!pipeServer_write.IsConnected && pipe_write == 2) //pipeServer_write在后面创建的
            {
                connectSelf(true);//重要
            }

            pipeServer_write.Close();
            pipeServer_write.Dispose();
            if (CloseStatus != 0)
            {
                GlobalVariable.DSNum = true;
                Openpipe.BeginInvoke(GlobalVariable.DSDEVICEADDRESS, null, null);
                Thread.Sleep(200);
            }
        }
        /// <summary>
        /// 自己建立个命名管道客户端去解脱阻塞的服务器
        /// </summary>
        private static void connectSelf(bool flag)
        {
            //flag:0：代表读管道；1代表写管道
            if (!flag) 
            {
                using (NamedPipeClientStream npcs = new NamedPipeClientStream(Pipename))
                {
                    npcs.Connect();
                    pipe_read = 0;//这块应该是0
                }
            }
            else
            {
                using (NamedPipeClientStream npcs = new NamedPipeClientStream(Pipename_write))
                {
                    npcs.Connect();
                    pipe_write = 0;//都取消连接了
                }
            }
            
        }
    }

    public class ProcessPipes
    {
        public delegate void PipeTransmit(string ID,int Device, int TestType, AccessManagerDS am,ref int Exist);
        public event PipeTransmit PipeMessage;

        public delegate void PipeApplyTransmit(string sample_id, int Device);
        public event PipeApplyTransmit PipeApplyMessage;
        public static CancellationTokenSource ProcessPipesCancel;

        private static NamedPipe namedpipe;
        private AccessManagerDS accessmanager;

        public ProcessPipes(NamedPipe np, AccessManagerDS am)
        {
            namedpipe = np;
            this.accessmanager = am;
            ProcessPipesCancel = new CancellationTokenSource();
        }
        public void start()
        {
            Task.Factory.StartNew(Run, ProcessPipesCancel.Token);
        }
        public async void Run()
        {
            namedpipe.NamedPipeCreat(NamedPipe.Pipename, NamedPipe.Pipename_write);//读 写
            while ((!ProcessPipesCancel.IsCancellationRequested) && NamedPipe.run_status)
            {
                NamedPipe.PipeMessage receiveData = new NamedPipe.PipeMessage();
                namedpipe.ReadNamedPipe(NamedPipe.pipeServer, ref receiveData);

                if(receiveData.Device!=GlobalVariable.DSDEVICE)
                {
                    //生化仪打开和命名管道发送生化仪方式不统一
                    MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
                    await mainwin.ShowMessageAsync("警告", "生化仪选择错误");
                    return;
                }
                else if (receiveData.CheckBit == 2)// 关闭管道，同时关闭客户端
                {
                    namedpipe.WriteNamedPipe(NamedPipe.pipeServer_write, ref receiveData);//回写函数

                    bool canClose = false;//当前连接是否可以安全关闭
                    if (ProcessHL7.hl7Manager != null)
                    {
                        canClose = canClose || (ProcessHL7.hl7Manager.IsHL7Available);
                    }

                    if (ProcessASTM.astmManager != null)
                    {
                        canClose = canClose || (ProcessASTM.astmManager.IsASTMAvailable);
                    }

                    if (Statusbar.SBar.SoftStatus == GlobalVariable.miniBusy)
                    {
                        canClose = true;
                    }

                    if (canClose)
                    {
                        //不可以安全关闭
                        MainWindow mainwin = (MainWindow)System.Windows.Application.Current.MainWindow;
                        MessageDialogResult clickresult = await mainwin.ShowMessageAsync("警告", "确定是否退出软件", MessageDialogStyle.AffirmativeAndNegative);
                        if (clickresult == MessageDialogResult.Negative)//取消
                        {
                            continue;
                        }
                        else
                        {
                            //确认
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        //可以直接关闭
                        Environment.Exit(0);
                    }
                }
                else if (receiveData.GetTestEnd == 1 && receiveData.CheckBit == 0) //如果测试完成   CheckBit 1表示主动下发返回 客户端不需要处理
                {
                    if (receiveData.GetNewData == 0)
                    {
                        Statusbar.SBar.SoftStatus = GlobalVariable.miniBusy;//mini mode
                        Statusbar.SBar.SampleId = receiveData.ID;
                        //新数据结果
                        PipeMessage.Invoke(receiveData.ID, receiveData.Device, receiveData.GetTestType, accessmanager, ref receiveData.UploadEnd);//把这三个数据委托出去，三个数据分别为样本ID ,样本测试仪器,和样本类型
                        namedpipe.WriteNamedPipe(NamedPipe.pipeServer_write, ref receiveData);//回写函数
                        Statusbar.SBar.SoftStatus = GlobalVariable.miniWaiting;
                    }
                    else if (receiveData.GetNewData == 1)
                    {
                        Statusbar.SBar.SoftStatus = GlobalVariable.miniBusy;//mini mode
                        Statusbar.SBar.SampleId = receiveData.ID;
                        //申请样本信息
                        ++Statusbar.SBar.ReceiveNum;
                        PipeApplyMessage.BeginInvoke(receiveData.ID, receiveData.Device, null, null);//样本仪器和样本类型
                        receiveData.UploadEnd = 1;
                        namedpipe.WriteNamedPipe(NamedPipe.pipeServer_write, ref receiveData);//回写函数
                        Statusbar.SBar.SoftStatus = GlobalVariable.miniWaiting;
                    }
                }
                else if (receiveData.CheckBit == 3 && GlobalVariable.DSDeviceID != string.Empty)
                {
                    //生化仪发过来仪器标识ID
                    GlobalVariable.DSDeviceID = receiveData.BarCode;
                    namedpipe.WriteNamedPipe(NamedPipe.pipeServer_write, ref receiveData);
                }
                Thread.Sleep(200);
            }
        }
        /// <summary>
        /// 主动下发样本信息时管道传递信息
        /// </summary>
        public static void ActiveSend(string _ID)
        {
            //只发送ID号
            NamedPipe.PipeMessage message = new NamedPipe.PipeMessage();
            message.ID = _ID;
            message.IDNum = _ID.Length;
            message.BarCode = string.Empty;
            message.BarCodeNum = 0;
            message.CheckBit = 1;

            namedpipe.WriteNamedPipe(NamedPipe.pipeServer_write, ref message);
            if (GlobalVariable.DSDEVICE == 0)
            {
                WriteAccessDS.UpdateDBIn(_ID, "DS800");
            }
            else if (GlobalVariable.DSDEVICE == 1) 
            {
                WriteAccessDS.UpdateDBIn(_ID, "DS400");
            }
            ++Statusbar.SBar.IssueNum;
        }
    }

    public class AccessManagerDS
    {
        private object AccessDSLocker = new object();//ACCESS队列锁
        public static Mutex mutex = new Mutex();//本地数据库的互斥锁
        public static Mutex EquipMutex = new Mutex();//设备数据库的互斥锁
        private readonly Queue<DI800Manager.DI800> DI800QueueAccess = new Queue<DI800Manager.DI800>();
        public ManualResetEvent DI800SignalAccess = new ManualResetEvent(false);

        public void AddDI800Access(DI800Manager.DI800 data)
        {
            lock (AccessDSLocker)
            {
                DI800QueueAccess.Enqueue(data);
            }
        }
        public DI800Manager.DI800 GetDI800Access()
        {
            lock (AccessDSLocker)
            {
                return DI800QueueAccess.Dequeue();
            }
        }
        public bool IsDI800AvailabelAccess
        {
            get
            {
                return DI800QueueAccess.Count > 0;
            }
        }
    }

    public class ReadEquipAccess
    {
        private static DataSet ds;
        private static string strSelect;
        private static OleDbConnection conn;
        private static string blank = string.Empty;
        private static DI800Manager.DI800 di800;
        private static DI800Manager.DI800Result result;
        private static int Count;

        public static event GlobalVariable.MessageHandler ReadEquipAccessMessage;

        public ReadEquipAccess(string DBaddress)
        {
            string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
            
            strConnection += "Data Source=";
            strConnection += DBaddress;
            conn = new OleDbConnection(strConnection);
        }
        public static void ReadData(string testID, int Device, int type, AccessManagerDS am,ref int ExistSample)//读取生化仪数据库
        {
            ExistSample = 0;
            AccessManagerDS.EquipMutex.WaitOne();//上锁
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                try
                {
                    conn.Open();//打开连接
                }
                catch
                {
                    ReadEquipAccessMessage.Invoke("数据库地址错误\r\n", "DEVICE");
                    AccessManagerDS.EquipMutex.ReleaseMutex();//斜锁
                    return;
                }
            }
            if (Device == 0)//DS800数据库
            {
                switch (type)
                {
                    case 0://生化
                        {
                            strSelect = "SELECT a.ITEM,a.RESULT,a.AddTime as TEST_TIME," +
                                 "b.PATIENTID,b.FamilyName,b.FIRSTNAME,b.SEX,b.AGE," +
                                 "c.FullName,c.NORMALLOW,c.NORMALHIGH,c.UNIT, d.DEPARTMENT,d.AERA,d.BedNum,d.DOCTOR," +
                                 "e.StartTime,e.Kind " +
                                 "FROM ((((BioResult a INNER JOIN Patient b ON b.BioID = a.BioID) " +
                                 "INNER JOIN BioItem c ON a.ITEM = c.ITEM) " +
                                 "INNER JOIN Register d ON a.BioID = d.BioID) " +
                                 "INNER JOIN BioMain e ON a.BioID = e.BioID) WHERE a.BioID " + "='" + testID +
                                 "'and a.IsSended = false and a.IsValid = true";
                        }
                        break;
                    case 1://电解质 未测试
                        {
                            strSelect = "SELECT a.ITEM,a.RESULT,a.AddTime as TEST_TIME," +
                                 "b.PATIENTID,b.FamilyName,b.FIRSTNAME,b.SEX,b.AGE," +
                                 "c.FullName,c.NormalLow as NORMALLOW,c.NormalHigh as NORMALHIGH,c.Unit as UNIT, d.DEPARTMENT,d.AERA,d.BedNum,d.DOCTOR," +
                                 "e.StartTime,e.Kind " +
                                 "FROM ((((ElecResult a INNER JOIN Patient b ON b.BioID = a.BioID) " +
                                 "INNER JOIN ElecItem c ON a.ITEM = c.ITEM) " +
                                 "INNER JOIN Register d ON a.BioID = d.BioID) " +
                                 "INNER JOIN BioMain e ON a.BioID = e.BioID) WHERE a.BioID " + "='" + testID +
                                 "'and a.IsSended = false and a.IsValid = true";
                        }
                        break;
                    case 2://质控 未测试
                        {
                            strSelect = "SELECT a.ITEM,a.RESULT,a.AddTime as TEST_TIME," +
                                 "b.PATIENTID,b.FamilyName,b.FIRSTNAME,b.SEX,b.AGE," +
                                 "c.FullName,c.NORMALLOW,c.NORMALHIGH,c.UNIT, d.DEPARTMENT,d.AERA,d.BedNum,d.DOCTOR," +
                                 "e.StartTime,e.Kind " +
                                 "FROM ((((BioResult a INNER JOIN Patient b ON b.BioID = a.BioID) " +
                                 "INNER JOIN BioItem c ON a.ITEM = c.ITEM) " +
                                 "INNER JOIN Register d ON a.BioID = d.BioID) " +
                                 "INNER JOIN BioMain e ON a.BioID = e.BioID) WHERE a.BioID " + "='" + testID +
                                 "'and a.IsSended = false and a.IsValid = true";
                        }
                        break;
                    case 3://定标 未测试
                        {
                            strSelect = "SELECT a.ITEM,a.RESULT,a.AddTime as TEST_TIME," +
                                 "b.PATIENTID,b.FamilyName,b.FIRSTNAME,b.SEX,b.AGE," +
                                 "c.FullName,c.NormalLow as NORMALLOW,c.NormalHigh as NORMALHIGH,c.Unit as UNIT, d.DEPARTMENT,d.AERA,d.BedNum,d.DOCTOR," +
                                 "e.StartTime,e.Kind " +
                                 "FROM ((((CalResult a INNER JOIN Patient b ON b.BioID = a.BioID) " +
                                 "INNER JOIN CalItem c ON a.ITEM = c.ITEM) " +
                                 "INNER JOIN Register d ON a.BioID = d.BioID) " +
                                 "INNER JOIN BioMain e ON a.BioID = e.BioID) WHERE a.BioID " + "='" + testID +
                                 "'and a.IsSended = false and a.IsValid = true";
                        }
                        break;
                    default: break;
                }

            }
            else if (Device == 1) //DS400数据库
            {
                switch (type)
                {
                    case 0://生化
                        {
                            strSelect = "SELECT a.ITEM,a.RESULT,a.UNIT,a.NORMAL_LOW as NORMALLOW,a.NORMAL_HIGH as NORMALHIGH,a.TIME as TEST_TIME," +
                            "b.PATIENT_ID  as PATIENTID,b.SEND_TIME,b.SAMPLE_KIND,b.EMERGENCY," +
                            "c.FULL_NAME as FullName, d.FIRST_NAME as FIRSTNAME ,d.SEX,d.AGE,e.DEPATMENT as DEPARTMENT,e.TREAT_AERA as AERA,e.SILKBED_NO as BedNum,e.DOCTOR " +
                            "FROM ((((SAMPLE_ITEM_TEST_RESULT a INNER JOIN SAMPLE_MAIN b ON b.SAMPLE_ID=a.SAMPLE_ID)" +
                            "INNER JOIN ITEM_PARA_MAIN c ON a.ITEM=c.ITEM)" +
                            "INNER JOIN SAMPLE_PATIENT_INFO d ON d.SAMPLE_ID=a.SAMPLE_ID) LEFT JOIN SAMPLE_REGISTER_INFO e ON e.SAMPLE_ID=a.SAMPLE_ID)" +
                            " WHERE a.SAMPLE_ID ='" + testID +
                            "'and a.IsValid = true";
                        }
                        break;
                    case 1://电解质  未测试
                        {
                            strSelect = "SELECT a.ITEM,a.RESULT,a.UNIT,a.NORMAL_LOW as NORMALLOW,a.NORMAL_HIGH as NORMALHIGH,a.TIME as TEST_TIME," +
                            "b.PATIENT_ID  as PATIENTID,b.SEND_TIME,b.SAMPLE_KIND,b.EMERGENCY," +
                            "a.FULL_NAME as FullName, d.FIRST_NAME as FIRSTNAME ,d.SEX,d.AGE,e.DEPATMENT as DEPARTMENT,e.TREAT_AERA as AERA,e.SILKBED_NO as BedNum,e.DOCTOR " +
                            "FROM (((SAMPLE_ELEC_RESULT a INNER JOIN SAMPLE_MAIN b ON b.SAMPLE_ID=a.SAMPLE_ID)" +
                            "INNER JOIN SAMPLE_PATIENT_INFO d ON d.SAMPLE_ID=a.SAMPLE_ID) LEFT JOIN SAMPLE_REGISTER_INFO e ON e.SAMPLE_ID=a.SAMPLE_ID)" +
                            " WHERE a.SAMPLE_ID ='" + testID +
                            "' and a.IsValid = true";
                        }
                        break;
                    case 2://质控  未测试
                        {
                            strSelect = "SELECT a.ITEM,a.RESULT,a.UNIT,a.NORMAL_LOW as NORMALLOW,a.NORMAL_HIGH as NORMALHIGH,a.TIME as TEST_TIME," +
                            "b.PATIENT_ID  as PATIENTID,b.SEND_TIME,b.SAMPLE_KIND,b.EMERGENCY," +
                            "c.FULL_NAME as FullName, d.FIRST_NAME as FIRSTNAME ,d.SEX,d.AGE,e.DEPATMENT as DEPARTMENT,e.TREAT_AERA as AERA,e.SILKBED_NO as BedNum,e.DOCTOR " +
                            "FROM ((((SAMPLE_ITEM_TEST_RESULT a INNER JOIN SAMPLE_MAIN b ON b.SAMPLE_ID=a.SAMPLE_ID)" +
                            "INNER JOIN ITEM_PARA_MAIN c ON a.ITEM=c.ITEM)" +
                            "INNER JOIN SAMPLE_PATIENT_INFO d ON d.SAMPLE_ID=a.SAMPLE_ID) LEFT JOIN SAMPLE_REGISTER_INFO e ON e.SAMPLE_ID=a.SAMPLE_ID)" +
                            " WHERE a.SAMPLE_ID ='" + testID +
                            "' and a.IsValid = true";
                        }
                        break;
                    case 3://定标 未测试
                        {
                            strSelect = "SELECT a.ITEM,a.RESULT,a.UNIT,a.NORMAL_LOW as NORMALLOW,a.NORMAL_HIGH as NORMALHIGH,b.TEST_TIME," +
                            "b.PATIENT_ID  as PATIENTID,b.SEND_TIME,b.SAMPLE_KIND,b.EMERGENCY," +
                            "c.FULL_NAME as FullName, d.FIRST_NAME as FIRSTNAME ,d.SEX,d.AGE,e.DEPATMENT as DEPARTMENT,e.TREAT_AERA as AERA,e.SILKBED_NO as BedNum,e.DOCTOR " +
                            "FROM ((((SAMPLE_ITEM_CAL_RESULT a INNER JOIN SAMPLE_MAIN b ON b.SAMPLE_ID=a.SAMPLE_ID)" +
                            "INNER JOIN ITEM_CAL_PARA c ON a.ITEM=c.ITEM)" +
                            "INNER JOIN SAMPLE_PATIENT_INFO d ON d.SAMPLE_ID=a.SAMPLE_ID) LEFT JOIN SAMPLE_REGISTER_INFO e ON e.SAMPLE_ID=a.SAMPLE_ID)" +
                            " WHERE a.SAMPLE_ID ='" + testID +
                            "' and a.IsVaile = true";
                        }
                        break;
                    default: break;
                }
            }
            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
            {
                ds = new DataSet();
                di800 = new DI800Manager.DI800();
                di800.Result = new List<DI800Manager.DI800Result>();
                Count = 0;
                try
                {
                    if (oa.Fill(ds, "BioResult") == 0)
                    {
                        ReadEquipAccessMessage.Invoke("设备数据库没有" + testID + "信息\r\n", "DEVICE");
                        ds.Clear();
                        conn.Close();
                        AccessManagerDS.EquipMutex.ReleaseMutex();
                        return;
                    }
                }
                catch (Exception e)
                {
                    ReadEquipAccessMessage.Invoke("设备数据库选择错误\r\n请检查后重新建立连接\r\n", "DEVICE");
                    ds.Clear();
                    conn.Close();
                    AccessManagerDS.EquipMutex.ReleaseMutex();
                    return;
                }

                foreach (DataRow dr in ds.Tables["BioResult"].Rows)
                {
                    #region 解析数据库数据
                    di800.PATIENT_ID = dr["PATIENTID"] == DBNull.Value ? blank : (string)dr["PATIENTID"];
                    di800.TIME = dr["TEST_TIME"] == DBNull.Value ? GlobalVariable.DefalutTime : ((DateTime)dr["TEST_TIME"]);
                    if (Device == 0)
                    {
                        di800.SEND_TIME = dr["StartTime"] == DBNull.Value ? GlobalVariable.DefalutTime : ((DateTime)dr["StartTime"]);//检验开始时间
                    }
                    else if (Device == 1)
                    {
                        di800.SEND_TIME = dr["SEND_TIME"] == DBNull.Value ? GlobalVariable.DefalutTime : ((DateTime)dr["SEND_TIME"]);
                    }
                    di800.SAMPLE_ID = dealID(testID);//需要对ID进行一次再处理

                    di800.Device = GlobalVariable.DSDeviceID;
                    di800.FIRST_NAME = dr["FIRSTNAME"] == DBNull.Value ? blank : (string)dr["FIRSTNAME"];
                    di800.SEX = dr["SEX"] == DBNull.Value ? blank : (string)dr["SEX"];
                    di800.AGE = dr["AGE"] == DBNull.Value ? blank : (string)dr["AGE"];
                    if (Device == 0)//800
                    {
                        di800.SAMPLE_KIND = dr["Kind"] == DBNull.Value ? blank : (string)dr["Kind"];
                    }
                    else if (Device == 1)//400
                    {
                        di800.SAMPLE_KIND = dr["SAMPLE_KIND"] == DBNull.Value ? blank : (string)dr["SAMPLE_KIND"];
                    }
                    di800.DOCTOR = dr["DOCTOR"] == DBNull.Value ? blank : (string)dr["DOCTOR"];
                    di800.AREA = dr["AERA"] == DBNull.Value ? blank : (string)dr["AERA"];
                    di800.BED = dr["BedNum"] == DBNull.Value ? blank : (string)dr["BedNum"];
                    di800.DEPARTMENT = dr["DEPARTMENT"] == DBNull.Value ? blank : (string)dr["DEPARTMENT"];
                    switch (type)
                    {
                        case 0:
                            {
                                di800.Type = "生化";
                            }
                            break;
                        case 1:
                            {
                                di800.Type = "电解质";
                            }
                            break;
                        case 2:
                            {
                                di800.Type = "质控";
                            }
                            break;
                        case 3:
                            {
                                di800.Type = "定标";
                            }
                            break;
                        default: break;
                    }
                    if ((Device == 1) && ((bool)dr["EMERGENCY"] == true))
                    {
                        di800.Type += " 急诊";
                    }
                    result = new DI800Manager.DI800Result();
                    result.ITEM = (string)dr["ITEM"];
                    result.FULL_NAME = dr["FULLNAME"] == DBNull.Value ? blank : (string)dr["FULLNAME"];
                    result.RESULT = dr["RESULT"] == DBNull.Value ? -1 : (double)dr["RESULT"];
                    result.UNIT = dr["UNIT"] == DBNull.Value ? blank : (string)dr["UNIT"];
                    result.NORMAL_LOW = dr["NORMALLOW"] == DBNull.Value ? -1 : (double)dr["NORMALLOW"];
                    result.NORMAL_HIGH = dr["NORMALHIGH"] == DBNull.Value ? -1 : (double)dr["NORMALHIGH"];
                    if (result.RESULT == -1 || result.NORMAL_LOW == -1 || result.NORMAL_HIGH == -1 || result.NORMAL_HIGH == 0) //如果最高值为0，则肯定不正确
                    {
                        result.INDICATE = string.Empty;
                    }
                    else
                    {
                        result.INDICATE = result.RESULT > result.NORMAL_HIGH ? "H" : (result.RESULT < result.NORMAL_LOW ? "L" : "N");
                    }
                    #endregion
                    di800.Result.Add(result);
                    Count++;
                }
            }


            #region DS400 特别读取信息
            if (Device == 1)//DS400额外添加内容
            {
                switch (type)
                {
                    case 0:
                        {
                            #region SAMPLE_ITEM_CAL_RESULT
                            strSelect = "select a.ITEM,a.RESULT,a.UNIT,a.NORMAL_LOW as NORMALLOW,a.NORMAL_HIGH as NORMALHIGH FROM SAMPLE_ITEM_CAL_RESULT as a WHERE a.SAMPLE_ID = '" + testID + "'  and a.IsVaile = true";
                            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                            {
                                ds = new DataSet();
                                try
                                {
                                    if (oa.Fill(ds, "CalResult") != 0)
                                    {
                                        foreach (DataRow dr in ds.Tables["CalResult"].Rows)
                                        {
                                            {
                                                result = new DI800Manager.DI800Result();
                                                result.ITEM = (string)dr["ITEM"];
                                                result.RESULT = dr["RESULT"] == DBNull.Value ? -1 : (double)dr["RESULT"];
                                                result.FULL_NAME = String.Empty;
                                                result.UNIT = dr["UNIT"] == DBNull.Value ? blank : (string)dr["UNIT"];
                                                result.NORMAL_LOW = dr["NORMALLOW"] == DBNull.Value ? -1 : (double)dr["NORMALLOW"];
                                                result.NORMAL_HIGH = dr["NORMALHIGH"] == DBNull.Value ? -1 : (double)dr["NORMALHIGH"];
                                                if (result.RESULT == -1 || result.NORMAL_LOW == -1 || result.NORMAL_HIGH == -1 || result.NORMAL_HIGH == 0) //如果最高值为0，则肯定不正确
                                                {
                                                    result.INDICATE = string.Empty;
                                                }
                                                else
                                                {
                                                    result.INDICATE = result.RESULT > result.NORMAL_HIGH ? "H" : (result.RESULT < result.NORMAL_LOW ? "L" : "N");
                                                }
                                                di800.Result.Add(result);
                                                Count++;
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    ReadEquipAccessMessage.Invoke("设备数据库选择错误\r\n请检查后重新建立连接\r\n", "DEVICE");
                                    ds.Clear();
                                    conn.Close();
                                    AccessManagerDS.EquipMutex.ReleaseMutex();
                                    return;
                                }

                            }
                            #endregion

                            #region SAMPLE_ITEM_INPUT_RESULT
                            strSelect = "select a.ITEM,a.RESULT,a.UNIT,a.NORMAL_LOW as NORMALLOW,a.NORMAL_HIGH as NORMALHIGH FROM SAMPLE_ITEM_INPUT_RESULT as a WHERE a.SAMPLE_ID = '" + testID + "' and a.IsVaile = true";
                            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                            {
                                ds = new DataSet();
                                try
                                {
                                    if (oa.Fill(ds, "InpResult") != 0)
                                    {
                                        foreach (DataRow dr in ds.Tables["InpResult"].Rows)
                                        {
                                            {
                                                result = new DI800Manager.DI800Result();
                                                result.ITEM = (string)dr["ITEM"];
                                                result.RESULT = dr["RESULT"] == DBNull.Value ? -1 : (double)dr["RESULT"];
                                                result.FULL_NAME = String.Empty;
                                                result.UNIT = dr["UNIT"] == DBNull.Value ? blank : (string)dr["UNIT"];
                                                result.NORMAL_LOW = dr["NORMALLOW"] == DBNull.Value ? -1 : (double)dr["NORMALLOW"];
                                                result.NORMAL_HIGH = dr["NORMALHIGH"] == DBNull.Value ? -1 : (double)dr["NORMALHIGH"];
                                                if (result.RESULT == -1 || result.NORMAL_LOW == -1 || result.NORMAL_HIGH == -1 || result.NORMAL_HIGH == 0) //如果最高值为0，则肯定不正确
                                                {
                                                    result.INDICATE = string.Empty;
                                                }
                                                else
                                                {
                                                    result.INDICATE = result.RESULT > result.NORMAL_HIGH ? "H" : (result.RESULT < result.NORMAL_LOW ? "L" : "N");
                                                }
                                                di800.Result.Add(result);
                                                Count++;
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    ReadEquipAccessMessage.Invoke("设备数据库选择错误\r\n请检查后重新建立连接\r\n", "DEVICE");
                                    ds.Clear();
                                    conn.Close();
                                    AccessManagerDS.EquipMutex.ReleaseMutex();
                                    return;
                                }

                            }
                            #endregion

                            #region SAMPLE_ITEM_PRINT_RESULT
                            //RESULT_D -> RESULT    RESULT_S -> INDICATE
                            strSelect = "select a.ITEM,a.RESULT_D as RESULT,a.RESULT_S as INDICATE ,a.UNIT,a.NORMAL_LOW as NORMALLOW,a.NORMAL_HIGH as NORMALHIGH FROM SAMPLE_ITEM_PRINT_RESULT as a WHERE a.SAMPLE_ID = '" + testID + "' and a.IsVaile = true";
                            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                            {
                                ds = new DataSet();
                                try
                                {
                                    if (oa.Fill(ds, "PriResult") != 0)
                                    {
                                        foreach (DataRow dr in ds.Tables["PriResult"].Rows)
                                        {
                                            {
                                                result = new DI800Manager.DI800Result();
                                                result.ITEM = (string)dr["ITEM"];
                                                result.RESULT = dr["RESULT"] == DBNull.Value ? -1 : (double)dr["RESULT"];
                                                result.FULL_NAME = String.Empty;
                                                result.UNIT = dr["UNIT"] == DBNull.Value ? blank : (string)dr["UNIT"];
                                                result.NORMAL_LOW = dr["NORMALLOW"] == DBNull.Value ? -1 : (double)dr["NORMALLOW"];
                                                result.NORMAL_HIGH = dr["NORMALHIGH"] == DBNull.Value ? -1 : (double)dr["NORMALHIGH"];
                                                result.INDICATE = dr["INDICATE"] == DBNull.Value ? blank : (string)dr["INDICATE"];

                                                di800.Result.Add(result);
                                                Count++;
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    ReadEquipAccessMessage.Invoke("设备数据库选择错误\r\n请检查后重新建立连接\r\n", "DEVICE");
                                    ds.Clear();
                                    conn.Close();
                                    AccessManagerDS.EquipMutex.ReleaseMutex();
                                    return;
                                }

                            }
                            #endregion
                        }
                        break;
                    case 1://电解质
                        {
                            #region SAMPLE_ELEC_RESULT
                            /*strSelect = "select a.ITEM, a.FULL_NAME as FULLNAME, a.RESULT,a.UNIT,a.NORMAL_LOW as NORMALLOW,a.NORMAL_HIGH as NORMALHIGH FROM SAMPLE_ELEC_RESULT as a WHERE a.SAMPLE_ID = '" + testID + "'";
                            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                            {
                                ds = new DataSet();
                                try
                                {
                                    if (oa.Fill(ds, "EleResult") != 0)
                                    {
                                        foreach (DataRow dr in ds.Tables["EleResult"].Rows)
                                        {
                                            {
                                                result = new DI800Manager.DI800Result();
                                                result.ITEM = (string)dr["ITEM"];
                                                result.RESULT = dr["RESULT"] == DBNull.Value ? -1 : (double)dr["RESULT"];
                                                result.FULL_NAME = dr["FULLNAME"] == DBNull.Value ? blank : (string)dr["FULLNAME"];
                                                result.UNIT = dr["UNIT"] == DBNull.Value ? blank : (string)dr["UNIT"];
                                                result.NORMAL_LOW = dr["NORMALLOW"] == DBNull.Value ? -1 : (double)dr["NORMALLOW"];
                                                result.NORMAL_HIGH = dr["NORMALHIGH"] == DBNull.Value ? -1 : (double)dr["NORMALHIGH"];
                                                if (result.RESULT == -1 || result.NORMAL_LOW == -1 || result.NORMAL_HIGH == -1 || result.NORMAL_HIGH == 0) //如果最高值为0，则肯定不正确
                                                {
                                                    result.INDICATE = string.Empty;
                                                }
                                                else
                                                {
                                                    result.INDICATE = result.RESULT > result.NORMAL_HIGH ? "H" : (result.RESULT < result.NORMAL_LOW ? "L" : "N");
                                                }
                                                di800.Result.Add(result);
                                                Count++;
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    ReadEquipAccessMessage.Invoke("设备数据库选择错误\r\n请检查后重新建立连接\r\n", "DEVICE");
                                    ds.Clear();
                                    conn.Close();
                                    AccessManagerDS.EquipMutex.ReleaseMutex();
                                    return;
                                }

                            }*/
                            #endregion
                        }
                        break;
                    case 2://不做
                        {

                        }
                        break;
                    case 3://不做
                        {

                        }
                        break;
                    default: break;
                }
            }
            #endregion

            am.AddDI800Access(di800);
            am.DI800SignalAccess.Set();
            ReadEquipAccessMessage.Invoke(di800.SAMPLE_ID + "读取设备数据库成功\r\n", "DEVICE");
            ExistSample = 1;
            ds.Clear();//清除DataSet所有数据
            conn.Close();//关闭
            AccessManagerDS.EquipMutex.ReleaseMutex();
        }
        //清除部分SAMPLE_ID前面的L字符
        private static string dealID(string ID)
        {
            if (ID.IndexOf('L') == 0)
            {
                //表明有第一个字母是L，则把L去掉
                return ID.TrimStart('L');//移除字符串中头部的'L'字符
            }
            else
            {
                //如果没有字母L或者L不在首字母，则不管
                return ID;
            }
        }
    }

    public class WriteEquipAccess
    {
        private static OleDbConnection conn;
        private static string strConnection;
        //private static string strDSInsert;
        private static string strJudge;

        public WriteEquipAccess()
        {
            strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
            strConnection += "Data Source=";
            strConnection += GlobalVariable.DSDEVICEADDRESS;

            conn = new OleDbConnection(strConnection);
        }
        public static void WriteApplySampleDS(DI800Manager.DsInput SampleInfo, List<DI800Manager.DsTask> task)
        {
            
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();//打开连接
            }
            AccessManagerDS.EquipMutex.WaitOne();//上锁

            
            //时间也作为主键，也要查询时间
            //首先判断重复，如果重复，直接返回，不提示
            strJudge = "select * from lisinput where [SAMPLE_ID]='" + SampleInfo.SAMPLE_ID + "' AND [Device] = '" + SampleInfo.Device + "' AND [SEND_TIME] = #" + SampleInfo.SEND_TIME.ToString() + "#";
            using (OleDbDataAdapter oaJudge = new OleDbDataAdapter(strJudge, conn))//判断是否写入重复
            {
                DataSet ds = new DataSet();
                try
                {
                    if (oaJudge.Fill(ds) != 0)
                    {
                        //申请样本重复
                        conn.Close();
                        AccessManagerDS.mutex.ReleaseMutex();
                        return;
                    }
                }
                finally
                {
                    ds.Clear();
                }
            }
            //然后插入到Lisinput表中
            string strInsert = "insert into Lisinput([SAMPLE_ID],[PATIENT_ID],[FIRST_NAME],[SEX],[AGE],[SEND_TIME],[EMERGENCY],[SAMPLE_KIND],[Device],[IsSend]) " +
                     "values (@SAMPLE_ID,@PATIENT_ID,@FIRST_NAME,@SEX,@AGE,@SEND_TIME,@EMERGENCY,@SAMPLE_KIND,@Device,@IsSend)";
            using (OleDbCommand cmd = new OleDbCommand(strInsert, conn))
            {
                cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = SampleInfo.SAMPLE_ID;
                cmd.Parameters.Add("@PATIENT_ID", OleDbType.VarChar).Value = SampleInfo.PATIENT_ID;
                cmd.Parameters.Add("@FIRST_NAME", OleDbType.VarChar).Value = SampleInfo.FIRST_NAME;
                cmd.Parameters.Add("@SEX", OleDbType.VarChar).Value = SampleInfo.SEX;
                cmd.Parameters.Add("@AGE", OleDbType.VarChar).Value = SampleInfo.AGE;
                cmd.Parameters.Add("@SEND_TIME", OleDbType.Date).Value = SampleInfo.SEND_TIME;
                cmd.Parameters.Add("@EMERGENCY", OleDbType.Boolean).Value = SampleInfo.EMERGENCY;
                cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = SampleInfo.SAMPLE_KIND;//血清血浆尿液
                cmd.Parameters.Add("@Device", OleDbType.VarChar).Value = SampleInfo.Device;
                cmd.Parameters.Add("@IsSend", OleDbType.Boolean).Value = false;
                cmd.ExecuteNonQuery();
            }
            //具体任务插入到Listask表中
            foreach (DI800Manager.DsTask dstask in task)
            {
                strInsert = "insert into Listask([SAMPLE_ID],[Item],[Type],[Device],[SEND_TIME]) values (@SAMPLE_ID,@Item,@Type,@Device,@SEND_TIME)";
                using (OleDbCommand cmd = new OleDbCommand(strInsert, conn))
                {
                    cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = dstask.SAMPLE_ID;
                    cmd.Parameters.Add("@Item", OleDbType.VarChar).Value = dstask.ITEM;
                    cmd.Parameters.Add("@Type", OleDbType.VarChar).Value = dstask.Type;
                    cmd.Parameters.Add("@Device", OleDbType.VarChar).Value = dstask.Device;
                    cmd.Parameters.Add("@SEND_TIME", OleDbType.Date).Value = dstask.SEND_TIME;
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            AccessManagerDS.EquipMutex.ReleaseMutex();//卸锁
            ProcessPipes.ActiveSend(SampleInfo.SAMPLE_ID); //将样本ID通过管道发送到生化仪
            conn.Close();
        }
    }

    public class WriteAccessDS
    {
        private static OleDbConnection conn;
        private static int ItemNum;
        private static string strInsert;
        private static string strJudge;
        private static string strIns;

        private AccessManagerDS accessmanager;
        public delegate void NoticeRead(string selectName, string SAMPLE_ID);
        public event NoticeRead NoticeReadMessage;
        public static event GlobalVariable.MessageHandler WriteAccessDSMessage;

        public static CancellationTokenSource WriteAccessDSCancel;

        public WriteAccessDS(AccessManagerDS am)
        {
            string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
            string pathto = GlobalVariable.topDir.Parent.FullName;
            strConnection += "Data Source=" + @pathto + "\\DSDB.mdb";
            conn = new OleDbConnection(strConnection);
            this.accessmanager = am;

            WriteAccessDSCancel = new CancellationTokenSource();
        }
        public void Start()
        {
            Task.Factory.StartNew(Run, WriteAccessDSCancel.Token);
        }
        public void Run()
        {
            while (!WriteAccessDSCancel.IsCancellationRequested) 
            {
                accessmanager.DI800SignalAccess.WaitOne();
                if (accessmanager.IsDI800AvailabelAccess)
                {
                    WriteData(accessmanager.GetDI800Access());
                }
                else
                {
                    accessmanager.DI800SignalAccess.Reset();
                }
            }
        }
        public void WriteData(DI800Manager.DI800 di800)
        {
            AccessManagerDS.mutex.WaitOne();
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();//打开数据库
            }

            strInsert = "insert into lisoutput([SAMPLE_ID],[PATIENT_ID],[ITEM],[TYPE],[SEND_TIME],[DEVICE],[FULL_NAME],[RESULT],[UNIT],[NORMAL_lOW],[NORMAL_HIGH],[TIME],[INDICATE],[IsGet],[FIRST_NAME],[SEX],[AGE],[SAMPLE_KIND],[DOCTOR],[AREA],[BED],[DEPARTMENT],[ISSEND]) " +
                    "values (@SAMPLE_ID,@PATIENT_ID,@ITEM,@TYPE,@SEND_TIME,@DEVICE,@FULL_NAME,@RESULT,@UNIT,@NORMAL_lOW,@NORMAL_HIGH,@TIME,@INDICATE,@IsGet,@FIRST_NAME,@SEX,@AGE,@SAMPLE_KIND,@DOCTOR,@AREA,@BED,@DEPARTMENT,@ISSEND)";
            int num = 0;//防止为空的时候写入
            for (int i = 0; i < di800.Result.Count; i++)
            {
                //判断重复
                strJudge = "select * from lisoutput where [SAMPLE_ID]='" + di800.SAMPLE_ID + "' AND [ITEM]='" + di800.Result[i].ITEM + "' AND [DEVICE]='" + di800.Device + "'";
                using (OleDbDataAdapter oaJudge = new OleDbDataAdapter(strJudge, conn))//判断是否写入重复
                {
                    DataSet ds = new DataSet();
                    try
                    {
                        if (oaJudge.Fill(ds) != 0)
                        {
                            WriteAccessDSMessage.Invoke(di800.SAMPLE_ID + " " + di800.Result[i].ITEM + "写入数据库重复\r\n", "DEVICE");
                            continue;
                        }
                    }
                    finally
                    {
                        ds.Clear();
                    }
                }
                #region 封装
                using (OleDbCommand cmd = new OleDbCommand(strInsert, conn))
                {
                    ++num;
                    cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = di800.SAMPLE_ID;
                    cmd.Parameters.Add("@PATIENT_ID", OleDbType.VarChar).Value = di800.PATIENT_ID;
                    cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = di800.Result[i].ITEM;
                    cmd.Parameters.Add("@TYPE", OleDbType.VarChar).Value = di800.Type;
                    cmd.Parameters.Add("@SEND_TIME", OleDbType.Date).Value = di800.SEND_TIME;
                    cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = di800.Device;
                    cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = di800.Result[i].FULL_NAME;
                    cmd.Parameters.Add("@RESULT", OleDbType.Double).Value = di800.Result[i].RESULT;
                    cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = di800.Result[i].UNIT;
                    cmd.Parameters.Add("@NORMAL_lOW", OleDbType.Double).Value = di800.Result[i].NORMAL_LOW;
                    cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.Double).Value = di800.Result[i].NORMAL_HIGH;
                    cmd.Parameters.Add("@TIME", OleDbType.Date).Value = di800.TIME;
                    cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = di800.Result[i].INDICATE;
                    cmd.Parameters.Add("@IsGet", OleDbType.VarChar).Value = "0";
                    cmd.Parameters.Add("@FIRST_NAME", OleDbType.VarChar).Value = di800.FIRST_NAME;
                    cmd.Parameters.Add("@SEX", OleDbType.VarChar).Value = di800.SEX;
                    cmd.Parameters.Add("@AGE", OleDbType.VarChar).Value = di800.AGE;
                    cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = di800.SAMPLE_KIND;
                    cmd.Parameters.Add("@DOCTOR", OleDbType.VarChar).Value = di800.DOCTOR;
                    cmd.Parameters.Add("@AREA", OleDbType.VarChar).Value = di800.AREA;
                    cmd.Parameters.Add("@BED", OleDbType.VarChar).Value = di800.BED;
                    cmd.Parameters.Add("@DEPARTMENT", OleDbType.VarChar).Value = di800.DEPARTMENT;
                    cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;

                    cmd.ExecuteNonQuery();

                    /* author: wenjie 
                     * function:send result to lis middleware server
                     * problems: should send record data at one time
                     */
                    var tempEntity = new
                    {
                        SAMPLE_ID = di800.SAMPLE_ID,
                        PATIENT_ID = di800.PATIENT_ID,
                        ITEM = di800.Result[i].ITEM,
                        TYPE = di800.Type,
                        SEND_TIME = 1000 * di800.SEND_TIME.Subtract(new DateTime(1970, 1, 1, 8, 0, 0)).TotalSeconds,// transfer millisecond based on beijing time zone
                        DEVICE = di800.Device,
                        FULL_NAME = di800.Result[i].FULL_NAME,
                        RESULT = di800.Result[i].RESULT,
                        UNIT = di800.Result[i].UNIT,
                        NORMAL_lOW = di800.Result[i].NORMAL_LOW,
                        NORMAL_HIGH = di800.Result[i].NORMAL_HIGH,
                        TIME = 1000 * di800.TIME.Subtract(new DateTime(1970, 1, 1, 8, 0, 0)).TotalSeconds,// transfer millisecond based on beijing time zone
                        INDICATE = di800.Result[i].INDICATE,
                        IsGet = "0",
                        FIRST_NAME = di800.FIRST_NAME,
                        SEX = di800.SEX,
                        AGE = di800.AGE,
                        SAMPLE_KIND = di800.SAMPLE_KIND,
                        DOCTOR = di800.DOCTOR,
                        AREA = di800.AREA,
                        BED = di800.BED,
                        DEPARTMENT = di800.DEPARTMENT,
                        ISSEND = false
                    };
                    /*
                    string json = JsonConvert.SerializeObject(tempEntity);
                    var client = new RestClient();
                    client.BaseUrl = new Uri(GlobalVariable.BaseUrl);//http://localhost:8080/MiddlewareWeb
                    var request = new RestRequest("DS/DSResult", Method.POST);
                    request.AddParameter("dsJSON", json);
                    IRestResponse response = client.Execute(request);
                    */
                }
                #endregion
            }
            conn.Close();//关闭
            AccessManagerDS.mutex.ReleaseMutex();
            if (num>0)
            {
                WriteAccessDSMessage.Invoke(di800.SAMPLE_ID + "写入数据库成功\r\n", "DEVICE");
            }
            NoticeReadMessage.BeginInvoke("SAMPLE_ID", di800.SAMPLE_ID, null, null);//避免之前数据库有重复值但没有发送出去,这样可以重新发送命令发送
            ++Statusbar.SBar.ReceiveNum;
        }
        public static void WriteRequestSampleDataHL7(HL7Manager.HL7SampleInfo SampleInfo)
        {
            string age;//病人年龄
            if (SampleInfo.SampleID == string.Empty)//不可能发生的事
            {
                return;//无样本ID号,但这种不可能发生
            }
            AccessManagerDS.mutex.WaitOne();
            age= SampleInfo.DateOfBrith == null ? string.Empty : ((DateTime.Now.Month < SampleInfo.DateOfBrith.Value.Month || (DateTime.Now.Month == SampleInfo.DateOfBrith.Value.Month &&
                    DateTime.Now.Day < SampleInfo.DateOfBrith.Value.Day)) ? (DateTime.Now.Year - SampleInfo.DateOfBrith.Value.Year).ToString() : (DateTime.Now.Year - SampleInfo.DateOfBrith.Value.Year - 1).ToString());

            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();//打开连接
            }
            List<DI800Manager.DsTask> tasklist = new List<DI800Manager.DsTask>();
            DI800Manager.DsInput taskInput = new DI800Manager.DsInput();
            DateTime SendTime = SampleInfo.SampleTime == null ? DateTime.Now : SampleInfo.SampleTime;
            //时间也作为主键，也要查询时间
            strJudge = "select * from lisinput where [SAMPLE_ID]='" + SampleInfo.SampleID + "' AND [Device] = '" + SampleInfo.Device + "' AND [SEND_TIME] = #" + SendTime.ToString() + "#";
            using (OleDbDataAdapter oaJudge = new OleDbDataAdapter(strJudge, conn))//判断是否写入重复
            {
                DataSet ds = new DataSet();
                try
                {
                    if (oaJudge.Fill(ds) != 0)
                    {
                        
                        WriteAccessDSMessage.Invoke(SampleInfo.SampleID + "申请样本重复\r\n", "DEVICE");
                        conn.Close();
                        AccessManagerDS.mutex.ReleaseMutex();
                        return;
                    }
                }
                finally
                {
                    ds.Clear();
                }
            }
            strInsert = "insert into lisinput([SAMPLE_ID],[PATIENT_ID],[FIRST_NAME],[SEX],[AGE],[SEND_TIME],[EMERGENCY],[SAMPLE_KIND],[Device],[IsSend]) " +
                     "values (@SAMPLE_ID,@PATIENT_ID,@FIRST_NAME,@SEX,@AGE,@SEND_TIME,@EMERGENCY,@SAMPLE_KIND,@Device,@IsSend)";
            using (OleDbCommand cmd = new OleDbCommand(strInsert, conn))
            {
                taskInput.SAMPLE_ID = SampleInfo.SampleID;
                taskInput.PATIENT_ID = SampleInfo.AdmissionNumber == null ? string.Empty : SampleInfo.AdmissionNumber;
                taskInput.FIRST_NAME = SampleInfo.PatientName == null ? string.Empty : SampleInfo.PatientName;
                taskInput.SEX = SampleInfo.Sex == null ? string.Empty : SampleInfo.Sex;
                taskInput.AGE = age;
                taskInput.SEND_TIME = SendTime;
                taskInput.EMERGENCY = SampleInfo.IsEmergency == "Y" ? true : false;
                taskInput.SAMPLE_KIND = SampleInfo.SampleType == null ? string.Empty : SampleInfo.SampleType;//血清血浆尿液
                taskInput.Device = SampleInfo.Device == null ? string.Empty : SampleInfo.Device;
                taskInput.IsSend = false;
                cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = taskInput.SAMPLE_ID;
                cmd.Parameters.Add("@PATIENT_ID", OleDbType.VarChar).Value = taskInput.PATIENT_ID;
                cmd.Parameters.Add("@FIRST_NAME", OleDbType.VarChar).Value = taskInput.FIRST_NAME;
                cmd.Parameters.Add("@SEX", OleDbType.VarChar).Value = taskInput.SEX;
                cmd.Parameters.Add("@AGE", OleDbType.VarChar).Value = age;
                cmd.Parameters.Add("@SEND_TIME", OleDbType.Date).Value = SendTime;
                cmd.Parameters.Add("@EMERGENCY", OleDbType.Boolean).Value = taskInput.EMERGENCY;
                cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = taskInput.SAMPLE_KIND;//血清血浆尿液
                cmd.Parameters.Add("@Device", OleDbType.VarChar).Value = taskInput.Device;
                cmd.Parameters.Add("@IsSend", OleDbType.Boolean).Value = false;
                cmd.ExecuteNonQuery();
            }
            strJudge = "select * from listask where [SAMPLE_ID]='" + SampleInfo.SampleID + "'AND [Device]='" + SampleInfo.Device + "'AND [SEND_TIME] = #" + SendTime.ToString() + "#"; 
            using (OleDbDataAdapter oaJudge = new OleDbDataAdapter(strJudge, conn))//判断是否写入重复
            {
                DataSet ds = new DataSet();
                try
                {
                    if (oaJudge.Fill(ds) != 0)
                    {
                        WriteAccessDSMessage.Invoke(SampleInfo.SampleID + "申请样本任务重复\r\n", "DEVICE");
                        conn.Close();
                        AccessManagerDS.mutex.ReleaseMutex();
                        return;
                    }
                }
                finally
                {
                    ds.Clear();
                }
            }
            
            for (int i = 0; i < SampleInfo.ExtraInfo.Count; ++i)
            {
                if (SampleInfo.ExtraInfo[i].TextID == string.Empty)
                {
                    //如果没有项目编号
                    if (SampleInfo.ExtraInfo[i].TextName == string.Empty) 
                    {
                        //如果也没有项目名称
                        //发过来的任务为空,那就不处理
                        continue;
                    }
                    else
                    {
                        //有项目成名,那就去搜本地数据库是否有相应的名称
                        strJudge = "select * from item_info WHERE SrtComp(Item,'" + SampleInfo.ExtraInfo[i].TextName + "',0)=0 AND [Device]='" + SampleInfo.Device + "'";
                        using (OleDbDataAdapter oa = new OleDbDataAdapter(strJudge, conn))
                        {
                            DataSet ds = new DataSet();
                            if (oa.Fill(ds, "Item") == 0)
                            {
                                //此时是本地无相应的名称,返回
                                ds.Clear();
                                continue;
                            }
                            else
                            {
                                //有本地名称,取一下项目类型
                                foreach (DataRow dr in ds.Tables["Item"].Rows)
                                {
                                    if (dr["Type"] == DBNull.Value)
                                    {
                                        //万一出现没有Type的值,就返回,但这种情况是几乎不可能的
                                        WriteAccessDSMessage.Invoke(SampleInfo.ExtraInfo[i].TextName + "项目无编号\r\n", "DEVICE");
                                        break;
                                    }
                                    //往本地数据库写任务
                                    strInsert = "insert into listask([SAMPLE_ID],[Item],[Type],[Device],[SEND_TIME])" +
                                                "values (@SAMPLE_ID,@Item,@Type,@Device,@SEND_TIME)";
                                    using (OleDbCommand cmd = new OleDbCommand(strInsert, conn))
                                    {
                                        DI800Manager.DsTask dstask = new DI800Manager.DsTask();
                                        dstask.SAMPLE_ID= SampleInfo.SampleID == null ? string.Empty : SampleInfo.SampleID;
                                        dstask.ITEM = SampleInfo.ExtraInfo[i].TextName;
                                        dstask.Type = (string)dr["Type"];
                                        dstask.Device = SampleInfo.Device;
                                        dstask.SEND_TIME = SendTime;
                                        tasklist.Add(dstask);
                                        cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = dstask.SAMPLE_ID;
                                        cmd.Parameters.Add("@Item", OleDbType.VarChar).Value = dstask.ITEM;
                                        cmd.Parameters.Add("@Type", OleDbType.VarChar).Value = dstask.Type;
                                        cmd.Parameters.Add("@Device", OleDbType.VarChar).Value = dstask.Device;
                                        cmd.Parameters.Add("@SEND_TIME", OleDbType.Date).Value = dstask.SEND_TIME;
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            ds.Clear();
                        }
                    }
                }
                else
                {
                    //如果有项目编号,就去找项目编号
                    strJudge = "select * from item_info where [Index]='" + SampleInfo.ExtraInfo[i].TextID + "'AND [Device]='" + SampleInfo.Device + "'";
                    using (OleDbDataAdapter oa = new OleDbDataAdapter(strJudge, conn))
                    {
                        DataSet ds = new DataSet();
                        if (oa.Fill(ds, "Item") == 0)
                        {
                            //数据库没有对应的数据编号
                            ds.Clear();
                            WriteAccessDSMessage.Invoke("本地数据库无" + SampleInfo.ExtraInfo[i].TextID + "项目编号\r\n", "DEVICE");
                            continue;
                        }
                        else
                        {
                            foreach (DataRow dr in ds.Tables["Item"].Rows)
                            {
                                if (dr["Type"] == DBNull.Value || dr["Item"] == DBNull.Value) 
                                {
                                    //万一出现没有Type或者Item的值,就返回,但这种情况是几乎不可能的
                                    break;
                                }
                                
                                //WriteEquipAccess.WriteApplySampleDS(SampleInfo.SampleID, (string)dr["Type"], (string)dr["Item"]);
                                //往本地数据库写任务
                                strInsert = "insert into listask([SAMPLE_ID],[Item],[Type],[Device],[SEND_TIME])" +
                                                "values (@SAMPLE_ID,@Item,@Type,@Device,@SEND_TIME)";
                                using (OleDbCommand cmd = new OleDbCommand(strInsert, conn))
                                {
                                    DI800Manager.DsTask dstask = new DI800Manager.DsTask();
                                    dstask.SAMPLE_ID = SampleInfo.SampleID == null ? string.Empty : SampleInfo.SampleID;
                                    dstask.ITEM = (string)dr["Item"];
                                    dstask.Type = (string)dr["Type"];
                                    dstask.Device = SampleInfo.Device;
                                    dstask.SEND_TIME = SendTime;
                                    tasklist.Add(dstask);
                                    cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = SampleInfo.SampleID == null ? string.Empty : SampleInfo.SampleID;
                                    cmd.Parameters.Add("@Item", OleDbType.VarChar).Value = (string)dr["Item"];
                                    cmd.Parameters.Add("@Type", OleDbType.VarChar).Value = (string)dr["Type"];
                                    cmd.Parameters.Add("@Device", OleDbType.VarChar).Value = SampleInfo.Device;
                                    cmd.Parameters.Add("@SEND_TIME", OleDbType.Date).Value = SendTime;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                        ds.Clear();
                    }
                }
            }
            conn.Close();
            AccessManagerDS.mutex.ReleaseMutex();
            WriteEquipAccess.WriteApplySampleDS(taskInput, tasklist);
            WriteAccessDSMessage.Invoke(SampleInfo.SampleID + "申请样本任务成功\r\n", "DEVICE");
        }
        public static void WriteRequestSampleDataASTM(ASTMManager.ASTMPatientInfo PatientInfo)
        {
            
            AccessManagerDS.mutex.WaitOne();
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            
            foreach (ASTMManager.ASTMSampleInfo SampleInfo in PatientInfo.SampleInfo)
            {
                string AllItem = string.Empty;
                if (SampleInfo.SampleID == string.Empty) //不可能发生的事
                {
                    conn.Close();
                    AccessManagerDS.mutex.ReleaseMutex();
                    return;//无样本ID号,但这种不可能发生
                }
                List<DI800Manager.DsTask> tasklist = new List<DI800Manager.DsTask>();
                DI800Manager.DsInput taskInput = new DI800Manager.DsInput();
                //先写入本地数据库DSDB lisinput
                //先检查表内是否重复
                strJudge = "select * from lisinput where [SAMPLE_ID]='" + SampleInfo.SampleID + "'AND [Device]='" + PatientInfo.Device + "'";
                using (OleDbDataAdapter oaJudge = new OleDbDataAdapter(strJudge, conn))//判断是否写入重复
                {
                    DataSet ds = new DataSet();
                    try
                    {
                        if (oaJudge.Fill(ds) != 0)
                        {
                            WriteAccessDSMessage.Invoke(SampleInfo.SampleID + "申请样本重复\r\n", "DEVICE");
                            conn.Close();
                            AccessManagerDS.mutex.ReleaseMutex();
                            return;
                        }
                    }
                    finally
                    {
                        ds.Clear();
                    }
                }
                strInsert = "insert into lisinput([SAMPLE_ID],[PATIENT_ID],[FIRST_NAME],[SEX],[AGE],[SEND_TIME],[EMERGENCY],[SAMPLE_KIND],[Device],[IsSend]) " +
                     "values (@SAMPLE_ID,@PATIENT_ID,@FIRST_NAME,@SEX,@AGE,@SEND_TIME,@EMERGENCY,@SAMPLE_KIND,@Device,@IsSend)";
                using (OleDbCommand cmd = new OleDbCommand(strInsert, conn))
                {
                    taskInput.SAMPLE_ID= SampleInfo.SampleID;
                    taskInput.PATIENT_ID = PatientInfo.PatientID == null ? string.Empty : PatientInfo.PatientID;
                    taskInput.FIRST_NAME = PatientInfo.PatientName == null ? string.Empty : PatientInfo.PatientName;
                    taskInput.SEX = PatientInfo.Sex == null ? string.Empty : PatientInfo.Sex;
                    taskInput.AGE = PatientInfo.Age.ToString();
                    taskInput.SEND_TIME = SampleInfo.RequestedTime;
                    taskInput.EMERGENCY = false;
                    taskInput.SAMPLE_KIND = SampleInfo.SampleType == null ? string.Empty : SampleInfo.SampleType;//样本类型
                    taskInput.Device = PatientInfo.Device;
                    taskInput.IsSend = false;

                    cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = taskInput.SAMPLE_ID;
                    cmd.Parameters.Add("@PATIENT_ID", OleDbType.VarChar).Value = taskInput.PATIENT_ID;
                    cmd.Parameters.Add("@FIRST_NAME", OleDbType.VarChar).Value = taskInput.FIRST_NAME;
                    cmd.Parameters.Add("@SEX", OleDbType.VarChar).Value = taskInput.SEX;
                    cmd.Parameters.Add("@AGE", OleDbType.VarChar).Value = taskInput.AGE;
                    cmd.Parameters.Add("@SEND_TIME", OleDbType.Date).Value = taskInput.SEND_TIME;
                    cmd.Parameters.Add("@EMERGENCY", OleDbType.Boolean).Value = false;
                    cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = taskInput.SAMPLE_KIND;//样本类型
                    cmd.Parameters.Add("@Device", OleDbType.VarChar).Value = taskInput.Device;
                    cmd.Parameters.Add("@IsSend", OleDbType.Boolean).Value = false;
                    cmd.ExecuteNonQuery();
                }
                //再写入本地数据库任务表中 listask
                //先检查重复
                strJudge = "select * from listask where [SAMPLE_ID]='" + SampleInfo.SampleID + "'AND [Device]='" + PatientInfo.Device + "'";
                using (OleDbDataAdapter oaJudge = new OleDbDataAdapter(strJudge, conn))//判断是否写入重复
                {
                    DataSet ds = new DataSet();
                    try
                    {
                        if (oaJudge.Fill(ds) != 0)
                        {
                            WriteAccessDSMessage.Invoke(SampleInfo.SampleID + "申请样本任务重复\r\n", "DEVICE");
                            conn.Close();
                            AccessManagerDS.mutex.ReleaseMutex();
                            return;
                        }
                    }
                    finally
                    {
                        ds.Clear();
                    }
                }
                //此时不重复了
                //开始写任务
                for (int i = 0; i < SampleInfo.ExtraInfo.Count; ++i) 
                {
                    //一条一条任务地写
                    if (SampleInfo.ExtraInfo[i].ItemID == string.Empty) 
                    {
                        //如果没有项目编号
                        if (SampleInfo.ExtraInfo[i].ItemName == string.Empty)
                        {
                            //如果也没有项目名称
                            //发过来的任务为空,那就不处理
                            continue;
                        }
                        else
                        {
                            //有项目名字,那就去搜本地数据库是否有相应的名称
                            strJudge = "select * from item_info WHERE SrtComp(Item,'" + SampleInfo.ExtraInfo[i].ItemName + "',0)=0 AND [Device]='" + PatientInfo.Device + "'";
                            using (OleDbDataAdapter oa = new OleDbDataAdapter(strJudge, conn))
                            {
                                DataSet ds = new DataSet();
                                if (oa.Fill(ds, "Item") == 0)
                                {
                                    //此时是本地无相应的名称,返回
                                    ds.Clear();
                                    continue;
                                }
                                else
                                {
                                    //有本地名称,取项目类型
                                    foreach (DataRow dr in ds.Tables["Item"].Rows)
                                    {
                                        if (dr["Type"] == DBNull.Value)
                                        {
                                            //万一出现没有Type的值,就返回,但这种情况是几乎不可能的
                                            WriteAccessDSMessage.Invoke(SampleInfo.ExtraInfo[i].ItemName + "项目无编号\r\n", "DEVICE");
                                            break;
                                        }
                                        if (AllItem == string.Empty)
                                        {
                                            AllItem += SampleInfo.ExtraInfo[i].ItemName;
                                        }
                                        else
                                        {
                                            AllItem += ("," + SampleInfo.ExtraInfo[i].ItemName);
                                        }
                                        //往本地数据库写任务
                                        strInsert = "insert into listask([SAMPLE_ID],[Item],[Type],[Device],[SEND_TIME])" +
                                                    "values (@SAMPLE_ID,@Item,@Type,@Device,@SEND_TIME)";
                                        using (OleDbCommand cmd = new OleDbCommand(strInsert, conn))
                                        {
                                            DI800Manager.DsTask dstask = new DI800Manager.DsTask();
                                            dstask.SAMPLE_ID = SampleInfo.SampleID == null ? string.Empty : SampleInfo.SampleID;
                                            dstask.ITEM = SampleInfo.ExtraInfo[i].ItemName;
                                            dstask.Type = (string)dr["Type"];
                                            dstask.Device = PatientInfo.Device;
                                            dstask.SEND_TIME = taskInput.SEND_TIME;
                                            tasklist.Add(dstask);
                                            cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = dstask.SAMPLE_ID;
                                            cmd.Parameters.Add("@Item", OleDbType.VarChar).Value = dstask.ITEM;
                                            cmd.Parameters.Add("@Type", OleDbType.VarChar).Value = dstask.Type;
                                            cmd.Parameters.Add("@Device", OleDbType.VarChar).Value = dstask.Device;
                                            cmd.Parameters.Add("@SEND_TIME", OleDbType.Date).Value = dstask.SEND_TIME;
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                                ds.Clear();
                            }
                        }
                    }
                    else
                    {
                        //如果有项目编号,就去找项目编号
                        strJudge = "select * from item_info where [Index]='" + SampleInfo.ExtraInfo[i].ItemID + "'AND [Device]='" + PatientInfo.Device + "'";
                        using (OleDbDataAdapter oa = new OleDbDataAdapter(strJudge, conn))
                        {
                            DataSet ds = new DataSet();
                            if (oa.Fill(ds, "Item") == 0)
                            {
                                //数据库没有对应的数据编号
                                ds.Clear();
                                WriteAccessDSMessage.Invoke("本地数据库无" + SampleInfo.ExtraInfo[i].ItemID + "项目编号\r\n", "DEVICE");
                                continue;
                            }
                            else
                            {
                                foreach (DataRow dr in ds.Tables["Item"].Rows)
                                {
                                    if (dr["Type"] == DBNull.Value || dr["Item"] == DBNull.Value)
                                    {
                                        //万一出现没有Type或者Item的值,就返回,但这种情况是几乎不可能的
                                        break;
                                    }
                                    if (AllItem == string.Empty)
                                    {
                                        AllItem += SampleInfo.ExtraInfo[i].ItemName;
                                    }
                                    else
                                    {
                                        AllItem += ("," + SampleInfo.ExtraInfo[i].ItemName);
                                    }
                                    //往本地数据库写任务
                                    strInsert = "insert into listask([SAMPLE_ID],[Item],[Type],[Device],[SEND_TIME])" +
                                                    "values (@SAMPLE_ID,@Item,@Type,@Device,@SEND_TIME)";
                                    using (OleDbCommand cmd = new OleDbCommand(strInsert, conn))
                                    {
                                        DI800Manager.DsTask dstask = new DI800Manager.DsTask();
                                        dstask.SAMPLE_ID = SampleInfo.SampleID == null ? string.Empty : SampleInfo.SampleID;
                                        dstask.ITEM = (string)dr["Item"];
                                        dstask.Type = (string)dr["Type"];
                                        dstask.Device = PatientInfo.Device;
                                        dstask.SEND_TIME = taskInput.SEND_TIME;
                                        tasklist.Add(dstask);
                                        cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = dstask.SAMPLE_ID;
                                        cmd.Parameters.Add("@Item", OleDbType.VarChar).Value = dstask.ITEM;
                                        cmd.Parameters.Add("@Type", OleDbType.VarChar).Value = dstask.Type;
                                        cmd.Parameters.Add("@Device", OleDbType.VarChar).Value = dstask.Device;
                                        cmd.Parameters.Add("@SEND_TIME", OleDbType.Date).Value = dstask.SEND_TIME;

                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            ds.Clear();
                        }
                    }
                }
                //ASTM模式下写入仪器数据库
                WriteEquipAccess.WriteApplySampleDS(taskInput, tasklist);
                WriteAccessDSMessage.Invoke(SampleInfo.SampleID + "申请样本任务成功\r\n", "DEVICE");
            }
            conn.Close();
            AccessManagerDS.mutex.ReleaseMutex();
        }
        public static void UpdateDBOut(string SAMPLE_ID, List<string> ITEM, string DEVICE)
        {
            if (DEVICE != GlobalVariable.DSDeviceID) 
            {
                return;
            }
            AccessManagerDS.mutex.WaitOne();
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();//打开连接
            }
            ItemNum = ITEM.Count;
            for (int i = 0; i < ItemNum; i++)
            {
                //todo : issend=-1 in access file  2017-05-10 wenjie
                strIns = "update lisoutput set ISSEND='" + "1" + "'" + " where " + "SAMPLE_ID='" + SAMPLE_ID + "'" + " and " + " ITEM='" + ITEM[i] + "'";
                using (OleDbCommand cmd = new OleDbCommand(strIns, conn))
                {
                    cmd.ExecuteNonQuery();

                    var tempEntity = new
                    {
                        SAMPLE_ID = SAMPLE_ID,
                        ITEM = ITEM[i],
                        DEVICE = DEVICE
                    };

                }
            }
            conn.Close();//关闭连接

            

            AccessManagerDS.mutex.ReleaseMutex();
        }
        public static void UpdateDBIn(string SAMPLE_ID, string DEVICE)
        {
            if (DEVICE != "DS800" && DEVICE != "DS400")
            {
                return;
            }
            AccessManagerDS.mutex.WaitOne();//上锁
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();//打开连接
            }
            strIns = "update lisinput set IsSend='" + "1" + "'" + " where " + "SAMPLE_ID='" + SAMPLE_ID + "' AND DEVICE ='" + DEVICE + "'";
            using (OleDbCommand cmd = new OleDbCommand(strIns, conn))
            {
                cmd.ExecuteNonQuery();
            }
            conn.Close();
            AccessManagerDS.mutex.ReleaseMutex();//卸锁
        }
    }

    public class ReadAccessDS
    {
        private static string table = "lisoutput";
        private static DataSet ds;
        private static OleDbConnection conn;
        private static string blank = string.Empty;
        private static DI800Manager.DI800 di800;
        private static DI800Manager.DI800Result result;
        private static bool IsAllSend;//判断一个ID号内是否所有Item都已经发送

        public static event GlobalVariable.MessageHandler ReadAccessDSMessage;

        private static DI800Manager di800Manager;
        public ReadAccessDS(DI800Manager dm)
        {
            di800Manager = dm;
            string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
            string pathto = GlobalVariable.topDir.Parent.FullName;
            strConnection += "Data Source=" + @pathto + "\\DSDB.mdb";
            conn = new OleDbConnection(strConnection);
        }
        public static void ReadData(string selectname, string selectvalue)
        {
            AccessManagerDS.mutex.WaitOne();
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();//打开连接
            }
            IsAllSend = true;
            di800 = new DI800Manager.DI800();
            ds = new DataSet();

            string strSelect = "select * from " + table + " where " + selectname + "='" + selectvalue + "'";
            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
            {
                di800.Result = new List<DI800Manager.DI800Result>();
                if (oa.Fill(ds, table) == 0)
                {
                    //ReadAccessDSMessage?.Invoke("这个ID没有数据");
                    ds.Clear();
                    conn.Close();
                    AccessManagerDS.mutex.ReleaseMutex();
                    return;
                }
                foreach (DataRow dr in ds.Tables[table].Rows)
                {
                    di800.ISSEND = dr["ISSEND"] == DBNull.Value ? false : (bool)dr["ISSEND"];
                    IsAllSend &= di800.ISSEND;
                    if (di800.ISSEND)
                    {
                        //这个项目已经上传
                        continue;
                    }
                    else
                    {
                        #region 解析数据库数据
                        di800.PATIENT_ID = dr["Patient_ID"] == DBNull.Value ? blank : (string)dr["Patient_ID"];
                        di800.TIME = dr["TIME"] == DBNull.Value ? DateTime.Now : ((DateTime)dr["TIME"]);
                        di800.SEND_TIME = dr["SEND_TIME"] == DBNull.Value ? DateTime.Now : ((DateTime)dr["SEND_TIME"]);
                        di800.SAMPLE_ID = (string)dr["SAMPLE_ID"];
                        di800.Device = (string)dr["Device"];
                        di800.FIRST_NAME = dr["FIRST_NAME"] == DBNull.Value ? blank : (string)dr["FIRST_NAME"];
                        di800.SEX = dr["SEX"] == DBNull.Value ? blank : (string)dr["SEX"];
                        di800.AGE = dr["AGE"] == DBNull.Value ? blank : (string)dr["AGE"];
                        di800.SAMPLE_KIND = dr["SAMPLE_KIND"] == DBNull.Value ? blank : (string)dr["SAMPLE_KIND"];
                        di800.DOCTOR = dr["DOCTOR"] == DBNull.Value ? blank : (string)dr["DOCTOR"];
                        di800.AREA = dr["AREA"] == DBNull.Value ? blank : (string)dr["AREA"];
                        di800.BED = dr["BED"] == DBNull.Value ? blank : (string)dr["BED"];
                        di800.DEPARTMENT = dr["DEPARTMENT"] == DBNull.Value ? blank : (string)dr["DEPARTMENT"];
                        di800.Type = dr["Type"] == DBNull.Value ? blank : (string)dr["Type"];

                        result = new DI800Manager.DI800Result();
                        result.ITEM = (string)dr["ITEM"];
                        result.FULL_NAME = dr["FULL_NAME"] == DBNull.Value ? blank : (string)dr["FULL_NAME"];
                        result.RESULT = dr["RESULT"] == DBNull.Value ? -1 : (double)dr["RESULT"];
                        result.UNIT = dr["UNIT"] == DBNull.Value ? blank : (string)dr["UNIT"];
                        result.NORMAL_LOW = dr["NORMAL_LOW"] == DBNull.Value ? -1 : (double)dr["NORMAL_LOW"];
                        result.NORMAL_HIGH = dr["NORMAL_HIGH"] == DBNull.Value ? -1 : (double)dr["NORMAL_HIGH"];
                        result.INDICATE = dr["INDICATE"] == DBNull.Value ? blank : (string)dr["INDICATE"];
                        #endregion
                        di800.Result.Add(result);
                    }
                }
            }
            if (!IsAllSend)
            {
                di800Manager.AddDI800(di800);
                di800Manager.DI800Signal.Set();
                ReadAccessDSMessage.Invoke(di800.SAMPLE_ID + "数据库读取成功\r\n", "DEVICE");
            }
            ds.Clear();//清楚DataSet所有数据
            conn.Close();//关闭
            AccessManagerDS.mutex.ReleaseMutex();
        }
        /// <summary>
        /// 检查数据库内未上传和未下发样本数目
        /// </summary>
        /// <param name="UpDown">false:未上传;true:未下发</param>
        public static void CheckUnDoneSampleNum(bool UpDown)
        {
            AccessManagerDS.mutex.WaitOne();//上锁
            if (conn.State == ConnectionState.Closed) 
            {
                conn.Open();
            }
            string strSelect;
            if (!UpDown)
            {
                //未上传
                strSelect = "select * from lisoutput where [ISSEND] = 0";
                ds = new DataSet();
                using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                {
                    if (oa.Fill(ds, "tempID") != 0)
                    {
                        HashSet<string> hID = new HashSet<string>();//选用哈希表来消除重复
                        foreach (DataRow dr in ds.Tables["tempID"].Rows)
                        {
                            hID.Add(dr["SAMPLE_ID"].ToString());
                        }
                        Statusbar.SBar.NoSendNum = hID.Count;
                    }
                    else
                    {
                        Statusbar.SBar.NoSendNum = 0;
                    }
                }
            }
            else
            {
                //未下发
                strSelect = "select * from lisinput where [IsSend] = false";
                ds = new DataSet();
                using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
                {
                    if (oa.Fill(ds, "tempID") != 0)
                    {
                        HashSet<string> hID = new HashSet<string>();//选用哈希表来消除重复
                        foreach (DataRow dr in ds.Tables["tempID"].Rows)
                        {
                            hID.Add(dr["SAMPLE_ID"].ToString());
                        }
                        Statusbar.SBar.NoIssueNum = hID.Count;
                    }
                    else
                    {
                        Statusbar.SBar.NoIssueNum = 0;
                    }
                }

            }
            conn.Close();
            AccessManagerDS.mutex.ReleaseMutex();//卸锁
        }
    }

    public class DI800Manager
    {
        private object DI800Locker = new object();

        public struct DI800Result
        {
            public string ITEM;//项目
            public string FULL_NAME;//项目全程
            public double RESULT;//结果
            public string UNIT;//结果单位
            public double NORMAL_LOW;//正常低
            public double NORMAL_HIGH;//正常高
            public string INDICATE;//提示
        }
        public struct DI800
        {
            public string Type;//项目类型
            public DateTime TIME;//检测时间
            public DateTime SEND_TIME;//送检时间
            public string SAMPLE_ID;//样品号
            public string PATIENT_ID;//病人号
            public string Device;//仪器型号
            public string FIRST_NAME;//病人姓名
            public string SEX;//病人性别
            public string AGE;//病人年龄  实际传输过程没有年龄
            public string SAMPLE_KIND;//样本类型
            public string DOCTOR;//申请医生
            public string AREA;//病区
            public string BED;//病床
            public string DEPARTMENT;//科室
            public bool ISSEND;//是否上传LIS
            public List<DI800Result> Result;//检测结果链表
        }
        private readonly Queue<DI800> DI800Queue = new Queue<DI800>();
        public ManualResetEvent DI800Signal = new ManualResetEvent(false);

        public void AddDI800(DI800 data)
        {
            lock (DI800Locker)
            {
                DI800Queue.Enqueue(data);
            }
        }
        public DI800 GetDI800()
        {
            lock (DI800Locker)
            {
                return DI800Queue.Dequeue();
            }
        }
        public bool IsDI800Availabel
        {
            get
            {
                return DI800Queue.Count > 0;
            }
        }

        //用于下发样本详细任务
        public struct DsTask
        {
            public string SAMPLE_ID;
            public string ITEM;
            public string Type;
            public string Device;
            public DateTime SEND_TIME;
        }
        //用于下发样本信息
        public struct DsInput
        {
            public string SAMPLE_ID;
            public string PATIENT_ID;
            public string FIRST_NAME;
            public string SEX;
            public string AGE;
            public DateTime SEND_TIME;
            public bool EMERGENCY;
            public string SAMPLE_KIND;
            public string Device;
            public bool IsSend;
        }

    }

    public class ProcessDI800s//数据从队列取出送给HL7实例hm
    {
        public delegate void DIEventHandle(object DIdata, string name);
        public event DIEventHandle DItransmit;

        private DI800Manager di800Manager;
        public static CancellationTokenSource ProcessDI800sCancel;

        public ProcessDI800s(DI800Manager dm)
        {
            this.di800Manager = dm;

            ProcessDI800sCancel = new CancellationTokenSource();
        }

        public void Start()
        {
            Task.Factory.StartNew(Run, ProcessDI800sCancel.Token);
        }

        private void Run()
        {
            while (!ProcessDI800sCancel.IsCancellationRequested)
            {
                di800Manager.DI800Signal.WaitOne();
                if (di800Manager.IsDI800Availabel)
                {
                    DI800Manager.DI800 di800 = di800Manager.GetDI800();
                    DItransmit.Invoke(di800, "DS");
                }
                else
                {
                    di800Manager.DI800Signal.Reset();
                }
                Thread.Sleep(1000);//这个时间可以商榷
            }
        }
    }

    public class DSCancel//取消所有与生化仪相关的线程,没有关闭命名管道
    {
        public static event GlobalVariable.MessageHandler DSCancellMessage;
        private DSCancel() { }
        public static void Cancell()
        {
            GlobalVariable.DSNum = false;
            if (GlobalVariable.DSDEVICEADDRESS != null)
            {
                if (GlobalVariable.IsContainsKey(GlobalVariable.DSDEVICEADDRESS))
                {
                    GlobalVariable.Remove(GlobalVariable.DSDEVICEADDRESS);
                }
            }
            GlobalVariable.ClearAllList = true;//清屏处理
            ProcessPipes.ProcessPipesCancel.Cancel();
            WriteAccessDS.WriteAccessDSCancel.Cancel();
            ProcessDI800s.ProcessDI800sCancel.Cancel();
            DSCancellMessage.Invoke("已经取消与生化仪连接\r\n", "DEVICE");
        }
    }

}
