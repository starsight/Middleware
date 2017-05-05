using System;
using System.Collections.Generic;
using ASTM;
using System.Threading;
using System.Threading.Tasks;
using MiddleWare.Views;
using System.Collections;

namespace MiddleWare.Communicate
{
    public class ASTMManager
    {
        private object ASTMLocker = new object();//ASTM队列锁
        private object ASTMRequestLocker = new object();//ASTMRequest队列锁

        public struct ASTMStruct
        {
            public string ASTMMessage;
            public string Sample_ID;
            public List<string> Item;
            public string Device;
        }
        private readonly Queue<ASTMStruct> ASTMQueue = new Queue<ASTMStruct>();
        //public ManualResetEvent ASTMSignal = new ManualResetEvent(false);
        public void AddASTM(ASTMStruct data)
        {
            lock(ASTMLocker)
            {
                ASTMQueue.Enqueue(data);
            }
        }
        public string GetASTMMessage()
        {
            lock(ASTMLocker)
            {
                return ASTMQueue.Peek().ASTMMessage;
            }
        }
        public string GetASTMSample_ID()
        {
            lock(ASTMLocker)
            {
                return ASTMQueue.Peek().Sample_ID;
            }
        }
        public List<string> GetASTMItem()
        {
            lock(ASTMLocker)
            {
                return ASTMQueue.Peek().Item;
            }
        }
        public string GetASTMDevice()
        {
            lock(ASTMLocker)
            {
                return ASTMQueue.Peek().Device;
            }
        }
        public bool IsASTMAvailable
        {
            get
            {
                return ASTMQueue.Count > 0;
            }
        }
        public void RemoveASTM()
        {
            lock(ASTMLocker)
            {
                ASTMQueue.Dequeue();
            }
        }

        //ASTM申请样本队列
        public struct ASTMRequestStruct
        {
            public string ASTMRequestMessage;
            public string RequestSample_ID;
            public string RequestDevice;
        }
        private readonly Queue<ASTMRequestStruct> ASTMRequestSampleDataQueue = new Queue<ASTMRequestStruct>();
        public void AddASTMRequestSampleData(ASTMRequestStruct data)
        {
            lock (ASTMRequestLocker)
            {
                ASTMRequestSampleDataQueue.Enqueue(data);
            }
        }
        public string GetASTMRequestSampleDataMessage()
        {
            lock (ASTMRequestLocker)
            {
                return ASTMRequestSampleDataQueue.Peek().ASTMRequestMessage;
            }
        }
        public string GetASTMRequestSampleDataSample_ID()
        {
            lock(ASTMRequestLocker)
            {
                return ASTMRequestSampleDataQueue.Peek().RequestSample_ID;
            }
        }
        public string GetASTMRequestSampleDataDevice()
        {
            lock(ASTMRequestLocker)
            {
                return ASTMRequestSampleDataQueue.Peek().RequestDevice;
            }
        }
        public bool IsASTMRequestSampleDataAvailable
        {
            get
            {
                return ASTMRequestSampleDataQueue.Count > 0;
            }
        }
        public void RemoveASTMRequestSampleData()
        {
            lock (ASTMRequestLocker)
            {
                ASTMRequestSampleDataQueue.Dequeue();
            }
        }

        //用于接收LIS发回来的病人样本信息
        public struct ASTMExtraInfo
        {
            public string ItemID;//项目编码
            public string ItemName;//项目名称 简称
            public string ItemDilutionRate;//稀释倍数
            public string ItemRepeatNum;//重复测试次数
        }
        public struct ASTMSampleInfo
        {
            public string SampleID;//样本ID
            public string BarCode;//样本条码
            public DateTime RequestedTime;//申请时间
            public DateTime TestTime;//测试时间
            public string CollectedName;//样本采集者
            public string SampleType;//样本类型
            public string OrderingDoctor;//送检医生
            public string Department;//送检科室
            public int OfflineDilutionfactor;//稀释因子
            public string ReportType;//报告类型
            public List<ASTMExtraInfo> ExtraInfo;//额外项目
        }
        public struct ASTMPatientInfo
        {
            public string PatientID;//病人ID 病人住院号
            public string PatientName;//病人姓名
            public int Age;//病人年龄
            public string Sex;//病人性别
            public string ReservedField;//病人血型
            public string SpecialField;//样本病状
            public string PatientDiet;//血袋编号
            public string Location;//病区
            public string Bed;//病床
            public string Device;//仪器
            public string Doctor;//医生
            public List<ASTMSampleInfo> SampleInfo;//额外样本
        }
    }
    public class ProcessASTM
    {
        private static ASTMManager astmManager;

        public delegate void UpdateAccessEventHandle(string SAMPLE_ID, List<string> ITEM, string DEVICE);
        public event UpdateAccessEventHandle UpdateDB;

        public delegate void RequestSampleDataEventHandle(ASTMManager.ASTMPatientInfo PatientInfo);
        public event RequestSampleDataEventHandle RequestSampleData;

        public event GlobalVariable.MessageHandler ProcessASTMMessage;
        public static CancellationTokenSource ProcessASTMCancel;

        public ProcessASTM(ASTMManager am)
        {
            astmManager = am;

            ProcessASTMCancel = new CancellationTokenSource();
        }
        public void Start()
        {
            if (GlobalVariable.IsASTMRun && GlobalVariable.IsASTMNet && !GlobalVariable.IsASTMCom)//通过网口传输
            {
                Task.Factory.StartNew(sendSocket, ProcessASTMCancel.Token);
            }
            if (GlobalVariable.IsASTMRun && !GlobalVariable.IsASTMNet && GlobalVariable.IsASTMCom)//通过串口传输
            {
                Task.Factory.StartNew(sendCom, ProcessASTMCancel.Token);
            }
        }
        private void sendCom()//通过COM发送
        {
            string ASTMMessage = string.Empty;
            byte receiveByte;
            string receiveString;
            string EndJudge = string.Empty;//字段检查
            while (!ProcessASTMCancel.IsCancellationRequested)
            {
                if (astmManager.IsASTMAvailable)
                {
                    #region 向ASTM发送样本测试结果
                    ASTMMessage = astmManager.GetASTMMessage();

                    if (!GlobalVariable.IsOneWay)
                    {
                        #region 双向通信
                        Connect.sendComByte(ASTM_Commands.ENQ);//先发送ENQ
                        receiveByte = Connect.recevieComByte();
                        if (receiveByte == ASTM_Commands.ENQ)
                        {
                            //此时双方都在发送ENQ命令争夺主模式,遵循仪器优先原则
                            Connect.sendComByte(ASTM_Commands.NAK);//发送NAK表示不接受LIS的主模式
                            receiveByte = Connect.recevieComByte();//等待LIS发送ACK
                        }
                        if (receiveByte == ASTM_Commands.ACK)
                        {
                            //LIS应答ACK
                            //此时仪器为主模式,仪器为从模式
                            if (Connect.sendCom(ASTMMessage))
                            {
                                ++Statusbar.SBar.SendNum;
                                receiveByte = Connect.recevieComByte();//等待LIS发送应带ACK
                                if (receiveByte == ASTM_Commands.ACK)
                                {
                                    //如果应答ACK,则表示接受成功
                                    //发送结束标识符EOT
                                    Connect.sendComByte(ASTM_Commands.EOT);
                                    ProcessASTMMessage.Invoke(astmManager.GetASTMSample_ID() + "Lis服务器接收成功\r\n", "LIS");//发送成功
                                    ++Statusbar.SBar.ReplyNum;
                                    UpdateDB.Invoke(astmManager.GetASTMSample_ID(), astmManager.GetASTMItem(), astmManager.GetASTMDevice());//回调
                                }
                            }
                            else
                            {
                                ProcessASTMMessage.Invoke(astmManager.GetASTMSample_ID() + "Lis服务器发送失败\r\n", "LIS");
                            }
                        }
                        else
                        {
                            //LIS没有应答ACK,LIS连接错误
                            ProcessASTMMessage.Invoke(astmManager.GetASTMSample_ID() + "Lis服务器连接错误\r\n", "LIS");
                        }
                        #endregion
                    }
                    else
                    {
                        #region 单向通信
                        if (Connect.sendCom(ASTMMessage))
                        {
                            ProcessASTMMessage.Invoke(astmManager.GetASTMSample_ID() + "Lis服务器发送成功\r\n", "LIS");
                            ++Statusbar.SBar.SendNum;
                            UpdateDB.Invoke(astmManager.GetASTMSample_ID(), astmManager.GetASTMItem(), astmManager.GetASTMDevice());//回调
                        }
                        else
                        {
                            ProcessASTMMessage.Invoke(astmManager.GetASTMSample_ID() + "Lis服务器发送失败\r\n", "LIS");
                        }
                        #endregion
                    }
                    astmManager.RemoveASTM();
                    #endregion
                }
                else if (astmManager.IsASTMRequestSampleDataAvailable) 
                {
                    #region 向ASTM请求样本信息
                    Connect.sendComByte(ASTM_Commands.ENQ);//先发送ENQ
                    receiveByte = Connect.recevieComByte();
                    if (receiveByte == ASTM_Commands.ENQ)
                    {
                        //此时双方都在发送ENQ命令争夺主模式,遵循仪器优先原则
                        Connect.sendComByte(ASTM_Commands.NAK);//发送NAK表示不接受LIS的主模式
                        receiveByte = Connect.recevieComByte();//等待LIS发送ACK
                    }
                    if (receiveByte == ASTM_Commands.ACK)
                    {
                        //LIS应答ACK
                        //此时仪器为主模式,仪器是从模式
                        ASTMMessage = astmManager.GetASTMRequestSampleDataMessage();
                        Connect.sendCom(ASTMMessage);
                        ++Statusbar.SBar.SendNum;
                        receiveByte = Connect.recevieComByte();//等待LIS发送应答ACK
                        if (receiveByte == ASTM_Commands.ACK)
                        {
                            //如果应答ACK,则表示接受成功
                            //发送结束标识符EOT
                            Connect.sendComByte(ASTM_Commands.EOT);
                            //此时进入中立模式
                            receiveByte = Connect.recevieComByte();//测试等待接收LIS发回来样本信息
                            if (receiveByte == ASTM_Commands.ENQ)
                            {
                                //应答ACK
                                Connect.sendComByte(ASTM_Commands.ACK);
                                //LIS开始为主模式,仪器为从模式
                                receiveString = Connect.receiveCom();//接收样本申请信息
                                //接收到样本消息后,应答ACK
                                Connect.sendComByte(ASTM_Commands.ACK);
                                //然后等待LIS发送EOT结束命令
                                receiveByte = Connect.recevieComByte();
                                if (receiveByte == ASTM_Commands.EOT)
                                {
                                    //样本查询和下载结束
                                    //此时来验证是否有相应的样本信息
                                    ASTM_Parser parser = new ASTM_Parser();
                                    List<ASTM_Message> message = new List<ASTM_Message>();
                                    try
                                    {
                                        parser.parse(receiveString, ref message);
                                        EndJudge = message[0].terminatorRecords[0].f_terminatorcode;//检查L字段
                                    }
                                    catch
                                    {
                                        //解析出现异常
                                        ProcessASTMMessage.Invoke(astmManager.GetASTMRequestSampleDataSample_ID() + "Lis服务器申请样本异常\r\n", "LIS");
                                    }

                                    if (EndJudge == "I")
                                    {
                                        //LIS无相应样本信息
                                        ProcessASTMMessage.Invoke(astmManager.GetASTMRequestSampleDataSample_ID() + "Lis服务器无相关样本信息\r\n", "LIS");
                                    }
                                    else if (EndJudge == "N")
                                    {
                                        //LIS内有相应样本信息
                                        ASTMManager.ASTMPatientInfo pi = ASTM_ParesrPatientInfo(receiveString);
                                        RequestSampleData.BeginInvoke(pi, null, null);
                                        //委托出去
                                    }
                                    else
                                    {
                                        //LIS 异常
                                        ProcessASTMMessage.Invoke(astmManager.GetASTMRequestSampleDataSample_ID() + "Lis服务器申请样本异常\r\n", "LIS");
                                    }
                                }

                            }
                        }
                    }
                    astmManager.RemoveASTMRequestSampleData();
                    #endregion
                }
                else
                {
                    #region LIS主动发送样本信息
                    //astmManager.ASTMSignal.Reset();
                    //持续监听LIS服务器
                    //LIS为主模式 仪器为从模式
                    receiveString = Connect.receiveCom();
                    //接收到样本消息后,应答ACK
                    Connect.sendComByte(ASTM_Commands.ACK);
                    if (receiveString.Length > 10 && receiveString.IndexOf("H") != -1)
                    {
                        //传回来标准消息
                        //然后等待LIS发送EOT结束命令
                        receiveByte = Connect.recevieComByte();
                        if (receiveByte == ASTM_Commands.EOT)
                        {
                            //样本查询和下载结束
                            //此时来验证是否有相应的样本信息
                            ASTM_Parser parser = new ASTM_Parser();
                            List<ASTM_Message> message = new List<ASTM_Message>();
                            try
                            {
                                parser.parse(receiveString, ref message);
                                EndJudge = message[0].patientRecords[0].orderRecords[0].f_report_type;//检查O字段的第26字段
                            }
                            catch
                            {
                                //解析出现异常
                                ProcessASTMMessage.Invoke(astmManager.GetASTMRequestSampleDataSample_ID() + "Lis服务器主动发送样本信息异常\r\n", "LIS");
                            }
                            if (EndJudge == "O")
                            {
                                //LIS内有相应样本信息
                                ASTMManager.ASTMPatientInfo pi = ASTM_ParesrPatientInfo(receiveString);
                                RequestSampleData.BeginInvoke(pi, null, null);//委托出去
                            }
                            else
                            {
                                //LIS 异常
                                ProcessASTMMessage.Invoke(astmManager.GetASTMRequestSampleDataSample_ID() + "Lis服务器主动发送样本信息异常\r\n", "LIS");
                            }
                        }
                    }
                    #endregion 
                }
            }
        }
        private void sendSocket()//通过socket发送
        {
            string ASTMMessage = string.Empty;
            string receiveString;
            byte receiveByte;
            string EndJudge = string.Empty;//字段检查

            while (!ProcessASTMCancel.IsCancellationRequested) 
            {
                if(astmManager.IsASTMAvailable)
                {
                    #region 向ASTM发送样本测试结果
                    ASTMMessage = astmManager.GetASTMMessage();//先把要发送的数据取出来
                    if (!GlobalVariable.IsOneWay)
                    {
                        #region 双向通信
                        Connect.sendSocketByte(ASTM_Commands.ENQ);//先发送ENQ
                        receiveByte = Connect.receiveSocketByet();
                        if (receiveByte == ASTM_Commands.ENQ)
                        {
                            //此时双方都在发送ENQ命令争夺主模式,遵循仪器优先原则
                            Connect.sendSocketByte(ASTM_Commands.NAK);//发送NAK表示不接受LIS的主模式
                            receiveByte = Connect.receiveSocketByet();//等待LIS发送ACK
                        }
                        if (receiveByte == ASTM_Commands.ACK)
                        {
                            //LIS应答ACK
                            //此时仪器为主模式,仪器是从模式
                            if (Connect.sendSocket(ASTMMessage)) 
                            {
                                ++Statusbar.SBar.SendNum;
                                receiveByte = Connect.receiveSocketByet();//等待LIS发送应答ACK
                                if (receiveByte == ASTM_Commands.ACK)
                                {
                                    //如果应答ACK,则表示接受成功
                                    //发送结束标识符EOT
                                    Connect.sendSocketByte(ASTM_Commands.EOT);
                                    ProcessASTMMessage.Invoke(astmManager.GetASTMSample_ID() + "Lis服务器接收成功\r\n", "LIS");//发送成功
                                    ++Statusbar.SBar.ReplyNum;
                                    UpdateDB.Invoke(astmManager.GetASTMSample_ID(), astmManager.GetASTMItem(), astmManager.GetASTMDevice());//回调
                                }
                                else
                                {
                                    ProcessASTMMessage.Invoke(astmManager.GetASTMSample_ID() + "Lis服务器发送失败\r\n", "LIS");
                                }
                            }
                            else
                            {
                                ProcessASTMMessage.Invoke(astmManager.GetASTMSample_ID() + "Lis服务器发送失败\r\n", "LIS");
                            }
                        }
                        else
                        {
                            //LIS没有应答ACK,LIS连接错误
                            ProcessASTMMessage.Invoke(astmManager.GetASTMSample_ID() + "Lis服务器连接错误\r\n", "LIS");
                        }
                        #endregion
                    }
                    else
                    {
                        #region 单向通信
                        if (Connect.sendSocket(ASTMMessage))
                        {
                            ++Statusbar.SBar.SendNum;
                            ProcessASTMMessage.Invoke(astmManager.GetASTMSample_ID() + "Lis服务器发送成功\r\n", "LIS");//发送成功
                            UpdateDB.Invoke(astmManager.GetASTMSample_ID(), astmManager.GetASTMItem(), astmManager.GetASTMDevice());//回调
                        }
                        else
                        {
                            ProcessASTMMessage.Invoke(astmManager.GetASTMSample_ID() + "Lis服务器发送失败\r\n", "LIS");//发送成功
                        }
                        #endregion
                    }
                    astmManager.RemoveASTM();//无论如何,最后都在队列删除掉这条数据
                    #endregion
                }
                else if(astmManager.IsASTMRequestSampleDataAvailable)
                {
                    #region 向ASTM请求样本信息
                    Connect.sendSocketByte(ASTM_Commands.ENQ);//先发送ENQ
                    receiveByte = Connect.receiveSocketByet();
                    if (receiveByte == ASTM_Commands.ENQ) 
                    {
                        //此时双方都在发送ENQ命令争夺主模式,遵循仪器优先原则
                        Connect.sendSocketByte(ASTM_Commands.NAK);//发送NAK表示不接受LIS的主模式
                        receiveByte = Connect.receiveSocketByet();//等待LIS发送ACK
                    }
                    if (receiveByte == ASTM_Commands.ACK)
                    {
                        //LIS应答ACK
                        //此时仪器为主模式,仪器是从模式
                        ASTMMessage = astmManager.GetASTMRequestSampleDataMessage();
                        Connect.sendSocket(ASTMMessage);
                        ++Statusbar.SBar.SendNum;
                        receiveByte = Connect.receiveSocketByet();//等待LIS发送应答ACK
                        if (receiveByte == ASTM_Commands.ACK)
                        {
                            //如果应答ACK,则表示接受成功
                            //发送结束标识符EOT
                            Connect.sendSocketByte(ASTM_Commands.EOT);
                            //此时进入中立模式
                            receiveByte = Connect.receiveSocketByet();//测试等待接收LIS发回来样本信息
                            if (receiveByte == ASTM_Commands.ENQ)
                            {
                                //应答ACK
                                Connect.sendSocketByte(ASTM_Commands.ACK);
                                //LIS开始为主模式,仪器为从模式
                                receiveString = Connect.receiveSocket();//接收样本申请信息
                                //接收到样本消息后,应答ACK
                                Connect.sendSocketByte(ASTM_Commands.ACK);
                                //然后等待LIS发送EOT结束命令
                                receiveByte = Connect.receiveSocketByet();
                                if (receiveByte == ASTM_Commands.EOT)
                                {
                                    //样本查询和下载结束
                                    //此时来验证是否有相应的样本信息
                                    ASTM_Parser parser = new ASTM_Parser();
                                    List<ASTM_Message> message = new List<ASTM_Message>();
                                    try
                                    {
                                        parser.parse(receiveString, ref message);
                                        EndJudge = message[0].terminatorRecords[0].f_terminatorcode;//检查L字段
                                    }
                                    catch
                                    {
                                        //解析出现异常
                                        ProcessASTMMessage.Invoke(astmManager.GetASTMRequestSampleDataSample_ID() + "Lis服务器申请样本异常\r\n", "LIS");
                                    }
                                    
                                    if (EndJudge == "I")
                                    {
                                        //LIS无相应样本信息
                                        ProcessASTMMessage.Invoke(astmManager.GetASTMRequestSampleDataSample_ID() + "Lis服务器无相关样本信息\r\n", "LIS");
                                    }
                                    else if (EndJudge == "N")
                                    {
                                        //LIS内有相应样本信息
                                        ASTMManager.ASTMPatientInfo pi = ASTM_ParesrPatientInfo(receiveString);
                                        RequestSampleData.BeginInvoke(pi, null, null);
                                        //委托出去
                                    }
                                    else
                                    {
                                        //LIS 异常
                                        ProcessASTMMessage.Invoke(astmManager.GetASTMRequestSampleDataSample_ID() + "Lis服务器申请样本异常\r\n", "LIS");
                                    }
                                }

                            }

                        }
                    }
                    astmManager.RemoveASTMRequestSampleData();
                    #endregion
                }
                else
                {
                    #region LIS主动发送样本信息
                    //astmManager.ASTMSignal.Reset();
                    //持续监听LIS服务器
                    //LIS为主模式 仪器为从模式
                    receiveString = Connect.receiveSocket();
                    //接收到样本消息后,应答ACK
                    Connect.sendSocketByte(ASTM_Commands.ACK);
                    if (receiveString.Length > 10 && receiveString.IndexOf("H") != -1) 
                    {
                        //传回来标准消息
                        //然后等待LIS发送EOT结束命令
                        receiveByte = Connect.receiveSocketByet();
                        if (receiveByte == ASTM_Commands.EOT)
                        {
                            //样本查询和下载结束
                            //此时来验证是否有相应的样本信息
                            ASTM_Parser parser = new ASTM_Parser();
                            List<ASTM_Message> message = new List<ASTM_Message>();
                            try
                            {
                                parser.parse(receiveString, ref message);
                                EndJudge = message[0].patientRecords[0].orderRecords[0].f_report_type;//检查O字段的第26字段
                            }
                            catch
                            {
                                //解析出现异常
                                ProcessASTMMessage.Invoke(astmManager.GetASTMRequestSampleDataSample_ID() + "Lis服务器主动发送样本信息异常\r\n", "LIS");
                            }
                            if (EndJudge == "O")
                            {
                                //LIS内有相应样本信息
                                ASTMManager.ASTMPatientInfo pi = ASTM_ParesrPatientInfo(receiveString);
                                RequestSampleData.BeginInvoke(pi, null, null);//委托出去
                            }
                            else
                            {
                                //LIS 异常
                                ProcessASTMMessage.Invoke(astmManager.GetASTMRequestSampleDataSample_ID() + "Lis服务器主动发送样本信息异常\r\n", "LIS");
                            }
                        }
                    }
                    #endregion 
                }
            }
        }
        public static void DSdataReceived(object receivedata, string name)//DS数据ASTM化
        {
            DI800Manager.DI800 data = (DI800Manager.DI800)receivedata;
            ASTMManager.ASTMStruct astm = new ASTMManager.ASTMStruct();
            astm.Item = new List<string>();

            ASTM_HeaderRecordPack h = new ASTM_HeaderRecordPack();
            h.DelimiterDefinition = "\\^&";
            h.SenderNameorID = data.Device;
            h.ProcessingID = "PR";
            h.VersionNumber = "1394-97";
            h.DateandTime = DateTime.Now.ToString("yyyyMMddhhmmss");

            ASTM_PatientRecordPack p = new ASTM_PatientRecordPack();
            p.SequenceNumber = "1";
            p.PatientID = data.PATIENT_ID;
            p.PatientLastName = data.FIRST_NAME;
            p.Age = data.AGE;
            p.PatientSex = data.SEX;
            p.DocterLastName = data.DOCTOR;
            p.Location = data.AREA;
            p.NatureofaltDiagCodeandClass = data.BED;

            ASTM_TestOrderRecordPack o = new ASTM_TestOrderRecordPack();
            o.SequenceNumber = "1";
            o.SampleID = data.SAMPLE_ID;
            o.Priority = (data.Type.IndexOf("急诊") > 0) ? "S" : "R";
            o.SpecimenCollectionDateandTime = data.TIME.ToString("yyyyMMddhhmmss");//检测时间
            o.DateTimeSpecimenReceivedintheLab = data.SEND_TIME.ToString("yyyyMMddhhmmss");//送检时间
            o.ReportType = "F";//最终的结果

            ASTM_TestOrderRecordPack.Assay item = new ASTM_TestOrderRecordPack.Assay();
            int num = 0;
            List<ASTM_ResultRecordPack> listR = new List<ASTM_ResultRecordPack>();
            foreach (DI800Manager.DI800Result temp in data.Result)
            {
                ++num;
                item.AssayNo = num.ToString();
                item.AssayName = temp.ITEM;
                item.RepeatNum = "1";
                o.AssayList.Add(item);

                ASTM_ResultRecordPack r = new ASTM_ResultRecordPack();
                r.SequenceNumber = num.ToString();
                r.AssayNo = num.ToString();
                r.AssayName = temp.FULL_NAME;
                r.Replicatenumber = "1";
                r.ResultType = "F";//定量结果
                r.MeasurementValue = temp.RESULT.ToString();
                r.Units = temp.UNIT;
                r.MeasurementRangeLowerLimit = temp.NORMAL_LOW.ToString();
                r.MeasurementRangeUpperLimit = temp.NORMAL_HIGH.ToString();
                r.ResultAbnormalflag = temp.INDICATE;
                r.ResultStatus = "F";//最终结果
                r.InstrumentIdentification = data.Device;
                listR.Add(r);
                astm.Item.Add(temp.ITEM);
            }
            ASTM_Encode message = new ASTM_Encode(h, p, o, listR);
            astm.ASTMMessage = message.Encode();
            astm.Device = data.Device;
            astm.Sample_ID = data.SAMPLE_ID;
            astmManager.AddASTM(astm);
        }
        public static void PLdataReceived(object receivedata, string name)//PL数据ASTM化
        {
            PLManager.PL12 data = (PLManager.PL12)receivedata;
            ASTMManager.ASTMStruct astm = new ASTMManager.ASTMStruct();
            astm.Item = new List<string>();

            ASTM_HeaderRecordPack h = new ASTM_HeaderRecordPack();
            h.DelimiterDefinition = "\\^&";
            h.SenderNameorID = data.DEVEICE;
            h.ProcessingID = "PR";
            h.VersionNumber = "1394-97";
            h.DateandTime = DateTime.Now.ToString("yyyyMMddhhmmss");

            ASTM_PatientRecordPack p = new ASTM_PatientRecordPack();//这个必须有
            p.SequenceNumber = "1";
            p.PatientID = data.SAMPLE_ID;

            ASTM_TestOrderRecordPack o = new ASTM_TestOrderRecordPack();
            o.SequenceNumber = "1";
            o.SampleID = data.SAMPLE_ID;
            o.SpecimenCollectionDateandTime = data.TEST_TIME.ToString("yyyyMMddhhmmss");//检测时间
            o.ReportType = "F";//最终的结果

            ASTM_TestOrderRecordPack.Assay item = new ASTM_TestOrderRecordPack.Assay();
            int num = 1;
            List<ASTM_ResultRecordPack> listR = new List<ASTM_ResultRecordPack>();
            ASTM_ResultRecordPack rAPP = new ASTM_ResultRecordPack();
            item.AssayNo = num.ToString();
            item.AssayName = "AAP";
            item.RepeatNum = "1";
            o.AssayList.Add(item);
            rAPP.SequenceNumber = num.ToString();
            rAPP.AssayNo = num.ToString();
            rAPP.AssayName = "诱聚剂项目";
            rAPP.Replicatenumber = "1";
            rAPP.ResultType = "F";//定量结果
            rAPP.MeasurementValue = data.AAP;
            rAPP.ResultStatus = "F";//最终结果
            rAPP.InstrumentIdentification = data.DEVEICE;
            listR.Add(rAPP);
            foreach (PLManager.PL12Result temp in data.Result)
            {
                ++num;
                item.AssayNo = num.ToString();
                item.AssayName = temp.ITEM;
                item.RepeatNum = "1";
                o.AssayList.Add(item);

                ASTM_ResultRecordPack r = new ASTM_ResultRecordPack();
                r.SequenceNumber = num.ToString();
                r.AssayNo = num.ToString();
                r.AssayName = temp.FULL_NAME;
                r.Replicatenumber = "1";
                r.ResultType = "F";//定量结果
                r.MeasurementValue = temp.RESULT;
                r.Units = temp.UNIT;
                r.MeasurementRangeLowerLimit = temp.NORMAL_LOW.ToString();
                r.MeasurementRangeUpperLimit = temp.NORMAL_HIGH.ToString();
                r.ResultAbnormalflag = temp.INDICATE;
                r.ResultStatus = "F";//最终结果
                r.InstrumentIdentification = data.DEVEICE;
                listR.Add(r);
                astm.Item.Add(temp.ITEM);
            }
            ASTM_Encode message = new ASTM_Encode(h, p, o, listR);
            astm.ASTMMessage = message.Encode();
            astm.Device = data.DEVEICE;
            astm.Sample_ID = data.SAMPLE_ID;
            astmManager.AddASTM(astm);
        }
        public static void DSRequestSampleData(string sample_id,int device)//处理生化仪申请消息
        {
            ASTMManager.ASTMRequestStruct astmrequest = new ASTMManager.ASTMRequestStruct();

            ASTM_HeaderRecordPack h = new ASTM_HeaderRecordPack();
            h.DelimiterDefinition = "\\^&";
            h.SenderNameorID = device == 0 ? "DS800" : (device == 1 ? "DS400" : string.Empty);
            h.ProcessingID = "PR";
            h.VersionNumber = "1394-97";
            h.DateandTime = DateTime.Now.ToString("yyyyMMddhhmmss");

            ASTM_RequestRecordPack q = new ASTM_RequestRecordPack();
            q.SequenceNumber = "1";
            q.SpecimenID = sample_id;//样本ID
            q.RequestInformationstatusCodes = "O";//请求样本查询

            ASTM_Encode message = new ASTM_Encode(h, q);
            astmrequest.ASTMRequestMessage = message.Encode();
            astmrequest.RequestSample_ID = sample_id;
            astmrequest.RequestDevice = device == 0 ? "DS800" : (device == 1 ? "DS400" : string.Empty);
            astmManager.AddASTMRequestSampleData(astmrequest);//压入队列
        }
        private ASTMManager.ASTMPatientInfo ASTM_ParesrPatientInfo(string astmdata)
        {
            ASTMManager.ASTMPatientInfo pi = new ASTMManager.ASTMPatientInfo();
            pi.SampleInfo = new List<ASTMManager.ASTMSampleInfo>();

            #region 解析ASTM申请样本信息
            ASTM_Parser parser = new ASTM_Parser();
            List<ASTM_Message> message = new List<ASTM_Message>();
            parser.parse(astmdata, ref message);
            foreach(ASTM_Message am in message)
            {
                string[] tempdevice = am.f_sender.Split(new char[] { '^' });
                pi.Device = tempdevice[0];//仪器
                foreach(ASTM_Patient ap in am.patientRecords)
                {
                    pi.PatientID = ap.f_laboratory_id;//病人ID
                    string[] tempname = ap.f_name.Split(new char[] { '^' });
                    pi.PatientName = tempname.Length > 1 ? tempname[1] + tempname[0] : tempname[0];
                    string[] tempage = ap.f_birthdate.Split(new char[] { '^' });
                    pi.Age = tempage.Length > 1 ? Convert.ToInt16(tempage[1]) : -1;//年龄
                    pi.Sex = ap.f_sex;//性别
                    pi.ReservedField = ap.f_reserved;//血型
                    pi.Doctor = ap.f_physician_id;//医生
                    pi.SpecialField = ap.f_special_1;//症状
                    pi.Bed = ap.f_dialogCode;//病床
                    pi.PatientDiet = ap.f_diet;//血袋编号
                    pi.Location = ap.f_location;//病区
                    foreach(ASTM_Order ao in ap.orderRecords)
                    {
                        ASTMManager.ASTMSampleInfo si = new ASTMManager.ASTMSampleInfo();
                        si.ExtraInfo = new List<ASTMManager.ASTMExtraInfo>();

                        string[] tempid = ao.f_sample_id.Split(new char[] { '^' });
                        si.SampleID = tempid[0];//样本ID
                        si.BarCode = ao.f_sampled_at;//样本条形码
                        try
                        {
                            si.RequestedTime = DateTime.ParseExact(ao.f_created_at, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);//样本请求时间
                        }
                        catch
                        {
                            si.RequestedTime = DateTime.Now;
                        }
                        try
                        {
                            si.TestTime = DateTime.ParseExact(ao.f_sampled_at, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);//样本测试时间
                        }
                        catch
                        {
                            si.TestTime = DateTime.Now;
                        }
                        si.CollectedName = ao.f_collector;//样本采集者
                        si.SampleType = ao.f_biomaterial;//样本类型
                        string[] tempordername = ao.f_physician.Split(new char[] { '^' });
                        si.OrderingDoctor = tempordername.Length > 1 ? tempordername[1] + tempordername[0] : tempordername[0];//送检医生
                        si.Department = ao.f_physician_phone;//送检科室
                        si.OfflineDilutionfactor = ao.f_user_field_1 == string.Empty ? 0 : Convert.ToInt16(ao.f_user_field_1);
                        si.ReportType = ao.f_report_type;//报告类型
                        string[] temptest = ao.f_test.Split(new char[] { '\\' });
                        foreach(string temp in temptest)
                        {
                            ASTMManager.ASTMExtraInfo ex = new ASTMManager.ASTMExtraInfo();
                            string[] assay = temp.Split(new char[] { '^' });
                            switch(assay.Length)
                            {
                                case 1:
                                    {
                                        ex.ItemID = assay[0];
                                    }
                                    break;
                                case 2:
                                    {
                                        ex.ItemID = assay[0];
                                        ex.ItemName = assay[1];
                                    }
                                    break;
                                case 3:
                                    {
                                        ex.ItemID = assay[0];
                                        ex.ItemName = assay[1];
                                        ex.ItemDilutionRate = assay[2];
                                    }
                                    break;
                                case 4:
                                    {
                                        ex.ItemID = assay[0];
                                        ex.ItemName = assay[1];
                                        ex.ItemDilutionRate = assay[2];
                                        ex.ItemRepeatNum = assay[3];
                                    }
                                    break;
                                default:break;
                            }
                            si.ExtraInfo.Add(ex);
                        }
                        pi.SampleInfo.Add(si);
                    }
                }
            }
            #endregion

            return pi;
        }
    }
}
