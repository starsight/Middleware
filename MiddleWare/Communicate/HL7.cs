using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V231.Datatype;
using NHapi.Model.V231.Group;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Segment;
using System.Threading;
using MiddleWare.Views;
using System.Threading.Tasks;
using System.Net.Sockets;
//1个线程  1个HL7队列
namespace MiddleWare.Communicate
{
    public class HL7Manager
    {
        private object HL7Locker = new object();//HL7队列锁
        private object HL7RequestLocker = new object();//HL7Request队列锁

        public struct HL7Struct
        {
            public string HL7Message;
            public string Sample_ID;
            public List<string> Item;
            public string Device;
        }
        private readonly Queue<HL7Struct> HL7Queue = new Queue<HL7Struct>();
        public void AddHL7(HL7Struct data)
        {
            lock (HL7Locker)
            {
                HL7Queue.Enqueue(data);
            }
        }
        public string GetHL7Message()
        {
            lock (HL7Locker)
            {
                return HL7Queue.Peek().HL7Message;
            }
        }
        public string GetHL7Sample_ID()
        {
            lock (HL7Locker)
            {
                return HL7Queue.Peek().Sample_ID;
            }
        }
        public List<string> GetHL7Item()
        {
            lock (HL7Locker)
            {
                return HL7Queue.Peek().Item;
            }
        }
        public string GetHL7Device()
        {
            lock (HL7Locker)
            {
                return HL7Queue.Peek().Device;
            }
        }
        public bool IsHL7Available
        {
            get
            {
                return HL7Queue.Count > 0;
            }
        }
        /// <summary>
        /// //移除队列中开始处的HL7
        /// </summary>
        public void RemoveHL7()
        {
            lock (HL7Locker)
            {
                HL7Queue.Dequeue();
            }
        }

        //HL7申请样本队列
        public struct HL7RequestStruct
        {
            public string HL7RequestMessage;
            public string RequestSample_ID;
            public string RequestDevice;
        }
        private readonly Queue<HL7RequestStruct> HL7RequestSampleDataQueue = new Queue<HL7RequestStruct>();
        public void AddHL7RequestSampleData(HL7RequestStruct data)
        {
            lock (HL7RequestLocker)
            {
                HL7RequestSampleDataQueue.Enqueue(data);
            }
        }
        public string GetHL7RequestSampleDataMessage()
        {
            lock (HL7RequestLocker)
            {
                return HL7RequestSampleDataQueue.Peek().HL7RequestMessage;
            }
        }
        public string GetHL7RequestSampleDataSample_ID()
        {
            lock (HL7RequestLocker)
            {
                return HL7RequestSampleDataQueue.Peek().RequestSample_ID;
            }
        }
        public string GetHL7RequestSampleDataDevice()
        {
            lock (HL7RequestLocker)
            {
                return HL7RequestSampleDataQueue.Peek().RequestDevice;
            }
        }
        public bool IsHL7RequestSampleDataAvailable
        {
            get
            {
                return HL7RequestSampleDataQueue.Count > 0;
            }
        }
        public void RemoveHL7RequestSampleData()
        {
            lock (HL7RequestLocker)
            {
                HL7RequestSampleDataQueue.Dequeue();
            }
        }

        //用于接收LIS发回来的病人样本信息
        public struct HL7ExtraInfo
        {
            public string TextID;//项目ID
            public string TextName;//项目名称
            public string Unit;//单位
            public string Normal;//参考范围
        }
        public struct HL7SampleInfo
        {
            public string AdmissionNumber;//住院号
            public string BedNumber;//床号
            public string PatientName;//病人姓名
            public DateTime? DateOfBrith;//出生日期
            public string Sex;//性别
            public string PatientAlias;//别名，曾用名，用作血型
            public string Race;//种族 未用
            public string PatientAddress;//地址
            public string CountryCode;//郡县邮编代码
            public string HomePhoneNumber;//家庭电话
            public string BusinessPhoneNumber;//单位电话
            public string PrimaryLanguage;//主要语言
            public string MaritalStatus;//婚姻状况
            public string Religion;//宗教
            public string PatientAccoutNumber;//账号，用作病人类别
            public string SocialSecurityNumber;//医保卡号
            public string DriverLicenseNumber;//驾驶证执照号，身份证号，收费类型
            public string EthnicGroup;//民族
            public string BirthPlace;//出生地
            public string Nationality;//国家
            public string BarCode;//样本编码
            public string SampleID;//样本编号
            public DateTime SampleTime;//样本接收日期时间即送检时间
            public string IsEmergency;//是否急诊
            public string CollcetionVolume;//采集量
            public string SampleType;//样本类型 血清/serum 血浆/plasma  尿液/urine
            public string FetchDoctor;//送检医生
            public string FetchDepartment;//送检科
            public string Device;//检验仪器
            public List<HL7ExtraInfo> ExtraInfo;//额外项目
        }
    }
    public class ProcessHL7
    {
        public static HL7Manager hl7Manager;

        public delegate void UpdateAccessEventHandle(string SAMPLE_ID, List<string> ITEM, string DEVICE);
        public event UpdateAccessEventHandle UpdateDB;

        public delegate void RequestSampleDataEventHandle(HL7Manager.HL7SampleInfo SampleInfo);//往外发送申请到的样本信息
        public event RequestSampleDataEventHandle RequestSampleData;

        public event GlobalVariable.MessageHandler ProcessHL7Message;
        public static CancellationTokenSource ProcessHL7Cancel;

        public ProcessHL7(HL7Manager hm)
        {
            hl7Manager = hm;

            ProcessHL7Cancel = new CancellationTokenSource();
        }
        public void Start()
        {
            Task.Factory.StartNew(sendSocket, ProcessHL7Cancel.Token);
        }
        private void sendSocket()
        {
            string HL7Message = string.Empty;
            string receiveString;
            PipeParser parser;
            IMessage m;
            int SendNum = 0;
            while (!ProcessHL7Cancel.IsCancellationRequested && GlobalVariable.IsSocketRun) 
            {
                if (hl7Manager.IsHL7Available)
                {
                    #region 向HL7发送样本测试结果
                    if (SendNum == GlobalVariable.ReSendNum) 
                    {
                        //连续发送失败超过规定次数，不再发送
                        hl7Manager.RemoveHL7();//移除队列中开始处的HL7
                        SendNum = 0;
                        continue;
                    }
                    Statusbar.SBar.SoftStatus = GlobalVariable.miniBusy;// mini mode
                    try
                    {
                        HL7Message = hl7Manager.GetHL7Message();
                        Connect.sendSocket(HL7Message);
                        if (SendNum == 0) 
                        {
                            ++Statusbar.SBar.SendNum;
                        }
                        ++SendNum;
                    }
                    catch
                    {
                        Statusbar.SBar.SoftStatus = GlobalVariable.miniError;// mini mode
                        ProcessHL7Message.Invoke(hl7Manager.GetHL7Sample_ID() + "Lis服务器发送失败\r\n请重新打开软件\r\n", "LIS");
                        break;
                    }
                    if (!GlobalVariable.IsOneWay)//双向
                    {
                        receiveString = Connect.receiveSocket();
                        if (receiveString.Substring(0, 3) == "MSH")//相当于一个判断
                        {
                            parser = new PipeParser();
                            m = parser.Parse(receiveString);
                            ACK ack = m as ACK;
                            if (ack.MSA.AcknowledgementCode.Value == "AA")
                            {
                                ProcessHL7Message.Invoke(hl7Manager.GetHL7Sample_ID() + "Lis服务器接收成功\r\n", "LIS");
                                Statusbar.SBar.SoftStatus = GlobalVariable.miniWaiting;// mini mode 
                                Statusbar.SBar.SampleId = hl7Manager.GetHL7Sample_ID();//mini mode
                                ++Statusbar.SBar.ReplyNum;
                                SendNum = 0;
                                UpdateDB.Invoke(hl7Manager.GetHL7Sample_ID(), hl7Manager.GetHL7Item(), hl7Manager.GetHL7Device());
                                hl7Manager.RemoveHL7();//移除队列中开始处的HL7
                            }
                            else 
                            {
                                Statusbar.SBar.SoftStatus = GlobalVariable.miniError;// mini mode
                                ProcessHL7Message.Invoke(hl7Manager.GetHL7Sample_ID() + "Lis服务器第" + SendNum.ToString() + "次接收失败\r\n", "LIS");
                            }
                        }
                        else
                        {
                            //接收异常
                            Statusbar.SBar.SoftStatus = GlobalVariable.miniError;// mini mode
                            ProcessHL7Message.Invoke(hl7Manager.GetHL7Sample_ID() + "Lis服务器第" + SendNum.ToString() + "次接收失败\r\n", "LIS");
                        }
                    }
                    else//单向
                    {
                        Statusbar.SBar.SoftStatus = GlobalVariable.miniWaiting;// mini mode
                        Statusbar.SBar.SampleId = hl7Manager.GetHL7Sample_ID();//mini mode

                        ProcessHL7Message.Invoke(hl7Manager.GetHL7Sample_ID() + "Lis服务器发送成功\r\n", "LIS");
                        UpdateDB.Invoke(hl7Manager.GetHL7Sample_ID(), hl7Manager.GetHL7Item(), hl7Manager.GetHL7Device());//直接回调
                        hl7Manager.RemoveHL7();//移除队列中开始处的HL7
                    }
                    #endregion
                }
                else if (hl7Manager.IsHL7RequestSampleDataAvailable)
                {
                    #region 向LIS请求样本信息
                    Statusbar.SBar.SoftStatus = GlobalVariable.miniBusy;// mini mode
                    string HL7Apply;
                    //申请信息发送
                    try
                    {
                        HL7Apply = hl7Manager.GetHL7RequestSampleDataMessage();
                        Connect.sendSocket(HL7Apply);
                        ++Statusbar.SBar.SendNum;
                    }
                    catch
                    {
                        Statusbar.SBar.SoftStatus = GlobalVariable.miniError;// mini mode
                        ProcessHL7Message.Invoke(hl7Manager.GetHL7Sample_ID() + "Lis服务器发送失败\r\n请重新打开软件\r\n", "LIS");
                        break;
                    }
                    receiveString = Connect.receiveSocket();
                    if (receiveString.Substring(0, 3) == "MSH")
                    {
                        //传回为标准消息
                        parser = new PipeParser();
                        m = parser.Parse(receiveString);
                        QCK_Q02 qck = m as QCK_Q02;
                        if (qck.QAK.QueryResponseStatus.Value == "OK")//代表有样本数据
                        {
                            string receiveStringDSR;
                            int DSRIndex = receiveString.IndexOf("DSR");
                            if (DSRIndex == -1)//如果刚才接收过来的没有DSR数据
                            {
                                receiveStringDSR = Connect.receiveSocket();//重新来接受DSR数据
                            }
                            else
                            {
                                receiveStringDSR = receiveString.Substring(DSRIndex);//截取后面一部分得为DSR数据
                            }
                            //解析DSR
                            //解析完DSR数据就要回复ACKQ03消息，此时设备信息传出去
                            PipeParser parserDSR = new PipeParser();
                            IMessage mDSR = parserDSR.Parse(receiveStringDSR);
                            DSR_Q03 dsr = mDSR as DSR_Q03;
                            if (dsr.QAK.QueryResponseStatus.Value == "OK") //相当于再做了一层判断
                            {
                                HL7Manager.HL7SampleInfo hl7info = HL7_ParserSampleInfo(receiveStringDSR);
                                //接收成功后就要发送应答信号
                                RequestSampleData.BeginInvoke(hl7info, null, null);
                                Connect.sendSocket(CreatACKQ03(dsr.MSH.ReceivingFacility.NamespaceID.Value));
                                Statusbar.SBar.SoftStatus = GlobalVariable.miniWaiting;// mini mode
                                Statusbar.SBar.SampleId = hl7Manager.GetHL7RequestSampleDataSample_ID();//mini mode
                                ProcessHL7Message.Invoke(hl7Manager.GetHL7RequestSampleDataSample_ID() + "LIS服务器申请样本成功\r\n", "LIS");
                            }
                            else
                            {
                                Statusbar.SBar.SoftStatus = GlobalVariable.miniError;// mini mode
                                ProcessHL7Message.Invoke(hl7Manager.GetHL7RequestSampleDataSample_ID() + "LIS服务器申请样本异常\r\n", "LIS");
                            }
                        }
                        else if (qck.QAK.QueryResponseStatus.Value == "NF")//代表没有样本数据
                        {
                            //此时不再回复消息
                            //结束
                            Statusbar.SBar.SoftStatus = GlobalVariable.miniError;// mini mode
                            ProcessHL7Message.Invoke(hl7Manager.GetHL7RequestSampleDataSample_ID() + "LIS服务器无相关样本信息\r\n", "LIS");
                        }
                    }
                    #endregion
                    hl7Manager.RemoveHL7RequestSampleData();//移除掉队列中的申请信息
                }
                else
                {
                    #region LIS主动发送样本信息                    
                    //hl7Manager.HL7Signal.Reset();
                    //持续监听LIS服务器
                    receiveString = Connect.receiveSocket();
                    if (receiveString.Length > 10 && receiveString.Substring(0, 3) == "MSH")
                    {
                        //传回来为标准信息
                        if (receiveString.IndexOf("DSR") != -1)
                        {
                            //判断传回来数据为DSR数据
                            //解析DSR
                            //解析完DSR数据就要回复ACKQ03消息，此时设备信息传出去
                            PipeParser parserActiveDSR = new PipeParser();
                            IMessage mActiveDSR = parserActiveDSR.Parse(receiveString);
                            DSR_Q03 dsr = mActiveDSR as DSR_Q03;
                            if (dsr.MSH.AcceptAcknowledgmentType.Value == "P" && dsr.QAK.QueryResponseStatus.Value == "OK")
                            {
                                Statusbar.SBar.SoftStatus = GlobalVariable.miniBusy;// mini mode

                                //双重判断,既判断是否为LIS主动发送样本信息,也要判断是否OK
                                HL7Manager.HL7SampleInfo hl7info = HL7_ParserSampleInfo(receiveString);
                                RequestSampleData.Invoke(hl7info);//这个地方应该要判断一下是否为DS
                                //接收成功后就要发送应答信号
                                Connect.sendSocket(CreatACKQ03(dsr.MSH.ReceivingFacility.NamespaceID.Value));
                                //ProcessHL7Message.Invoke(hl7Manager.GetHL7RequestSampleDataSample_ID() + "LIS服务器主动发送样本申请信息\r\n", "LIS");
                                ProcessHL7Message.Invoke(hl7info.SampleID + "LIS服务器主动发送样本申请信息\r\n", "LIS");

                                Statusbar.SBar.SoftStatus = GlobalVariable.miniWaiting;// mini mode
                                Statusbar.SBar.SampleId = hl7info.SampleID;//mini mode
                            }
                        }
                    }
                    #endregion
                }
            }
        }
        public static void DSdataReceived(object receivedata, string name)//处理生化仪样本测试结果 ORU_R01
        {
            DI800Manager.DI800 data = (DI800Manager.DI800)receivedata;
            HL7Manager.HL7Struct hl7 = new HL7Manager.HL7Struct();
            hl7.Item = new List<string>();

            PipeParser Parser = new PipeParser();
            ORU_R01 oruR01 = new ORU_R01();
            #region 消息段封装
            //MSH段,位于消息最前面
            oruR01.MSH.FieldSeparator.Value = "|";
            oruR01.MSH.EncodingCharacters.Value = @"^~\&";
            oruR01.MSH.SendingApplication.NamespaceID.Value = "Mindray";//仪器供应商
            oruR01.MSH.SendingFacility.NamespaceID.Value = data.Device;
            oruR01.MSH.DateTimeOfMessage.TimeOfAnEvent.SetLongDate(DateTime.Now);//当前时间
            oruR01.MSH.MessageType.MessageType.Value = "ORU";
            oruR01.MSH.MessageType.TriggerEvent.Value = "R01";
            oruR01.MSH.MessageControlID.Value = "1";
            oruR01.MSH.ProcessingID.ProcessingID.Value = "P";
            oruR01.MSH.VersionID.VersionID.Value = "2.3.1";
            oruR01.MSH.ApplicationAcknowledgmentType.Value = "0";//样本测试结果
            oruR01.MSH.GetCharacterSet(0).Value = GlobalVariable.SocketCode ? "ASCII" : "UTF8";
            //PID段,主要用来构建病人的个人信息
            oruR01.GetPATIENT_RESULT().PATIENT.PID.SetIDPID.Value = "1";//这个值还要商榷
            oruR01.GetPATIENT_RESULT().PATIENT.PID.GetPatientIdentifierList(0).ID.Value = data.PATIENT_ID;
            oruR01.GetPATIENT_RESULT().PATIENT.PID.GetAlternatePatientIDPID(0).ID.Value = data.BED;
            oruR01.GetPATIENT_RESULT().PATIENT.PID.GetPatientName(0).FamilyLastName.FamilyName.Value = data.FIRST_NAME;
            oruR01.GetPATIENT_RESULT().PATIENT.PID.GetMotherSMaidenName(0).FamilyLastName.FamilyName.Value = data.AREA;
            oruR01.GetPATIENT_RESULT().PATIENT.PID.Sex.Value = data.SEX;

            //OBR段,用于传输关于检验报告相关的医嘱信息
            oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR.SetIDOBR.Value = "1";
            oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR.PlacerOrderNumber.EntityIdentifier.Value = data.SAMPLE_ID;
            oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR.ObservationDateTime.TimeOfAnEvent.SetLongDate(data.TIME);
            oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR.SpecimenReceivedDateTime.TimeOfAnEvent.SetLongDate(data.SEND_TIME);
            oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR.FillerField1.Value = data.DOCTOR;
            oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR.FillerField2.Value = data.DEPARTMENT;
            //oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR.SpecimenReceivedDateTime.TimeOfAnEvent.Value = data.TIME;
            //0BX段,用于在报告消息中传递观察的信息
            ORU_R01_ORDER_OBSERVATION orderObservation = oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION();
            int num = data.Result.Count();
            if (num != 0)
            {
                OBX[] obx = new OBX[num];
                CE[] ce = new CE[num];
                Varies value;
                for (int i = 0; i < num; i++)
                {
                    obx[i] = orderObservation.GetOBSERVATION(i).OBX;
                    obx[i].SetIDOBX.Value = (i + 1).ToString();
                    obx[i].ValueType.Value = "NM";
                    obx[i].ObservationSubID.Value = data.Result[i].ITEM;
                    obx[i].ObservationIdentifier.Identifier.Value = data.Result[i].FULL_NAME;

                    ce[i] = new CE(oruR01);
                    value = obx[i].GetObservationValue(0);
                    value.Data = ce[i];
                    ce[i].Identifier.Value = data.Result[i].RESULT.ToString();

                    obx[i].Units.Identifier.Value = data.Result[i].UNIT;
                    obx[i].ReferencesRange.Value = data.Result[i].NORMAL_LOW.ToString() + "---" + data.Result[i].NORMAL_HIGH.ToString();
                    obx[i].GetAbnormalFlags(0).Value = data.Result[i].RESULT > data.Result[i].NORMAL_HIGH ? "H" : (data.Result[i].RESULT < data.Result[i].NORMAL_LOW ? "L" : "N");
                    obx[i].NatureOfAbnormalTest.Value = data.Result[i].INDICATE;
                    hl7.Item.Add(data.Result[i].ITEM);
                }
            }
            #endregion
            hl7.HL7Message = Parser.Encode(oruR01);
            hl7.Sample_ID = data.SAMPLE_ID;
            hl7.Device = data.Device;
            hl7Manager.AddHL7(hl7);
        }
        public static void PLdataReceived(object receivedata, string name)//处理血小板样本测试结果  ORU_RO1
        {
            PLManager.PL12 data = (PLManager.PL12)receivedata;
            HL7Manager.HL7Struct hl7 = new HL7Manager.HL7Struct();
            hl7.Item = new List<string>();

            PipeParser Parser = new PipeParser();
            ORU_R01 oruR01 = new ORU_R01();

            #region 消息段封装
            //MSH段,位于消息最前面
            oruR01.MSH.FieldSeparator.Value = "|";
            oruR01.MSH.EncodingCharacters.Value = @"^~\&";
            oruR01.MSH.SendingApplication.NamespaceID.Value = "Mindray";//仪器供应商
            oruR01.MSH.SendingFacility.NamespaceID.Value = data.DEVEICE;
            oruR01.MSH.DateTimeOfMessage.TimeOfAnEvent.SetLongDate(DateTime.Now);//当前时间
            oruR01.MSH.MessageType.MessageType.Value = "ORU";
            oruR01.MSH.MessageType.TriggerEvent.Value = "R01";
            oruR01.MSH.MessageControlID.Value = "1";
            oruR01.MSH.ProcessingID.ProcessingID.Value = "P";
            oruR01.MSH.VersionID.VersionID.Value = "2.3.1";
            oruR01.MSH.ApplicationAcknowledgmentType.Value = "0";//样本测试结果
            oruR01.MSH.GetCharacterSet(0).Value = GlobalVariable.SocketCode ? "ASCII" : "UTF8";
            //没有病人信息,PID段直接省略
            //OBR段,用于传输关于检验报告相关的医嘱信息
            oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR.SetIDOBR.Value = "1";
            oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR.PlacerOrderNumber.EntityIdentifier.Value = data.SAMPLE_ID;
            oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR.ObservationDateTime.TimeOfAnEvent.SetLongDate(data.TEST_TIME);
            //0BX段,用于在报告消息中传递观察的信息
            ORU_R01_ORDER_OBSERVATION orderObservation = oruR01.GetPATIENT_RESULT().GetORDER_OBSERVATION();

            int num = data.Result.Count();
            if (num != 0)
            {
                OBX[] obx = new OBX[num + 1];
                CE[] ce = new CE[num + 1];
                Varies value;

                obx[0] = orderObservation.GetOBSERVATION(0).OBX;
                obx[0].SetIDOBX.Value = "1";
                obx[0].ValueType.Value = "NM";
                obx[0].ObservationSubID.Value = "AAP";
                obx[0].ObservationIdentifier.Identifier.Value = "诱聚剂项目";
                ce[0] = new CE(oruR01);
                value = obx[0].GetObservationValue(0);
                value.Data = ce[0];
                ce[0].Identifier.Value = data.AAP;
                for (int i = 1; i <= num; i++)
                {
                    obx[i] = orderObservation.GetOBSERVATION(i).OBX;
                    obx[i].SetIDOBX.Value = (i + 1).ToString();
                    obx[i].ValueType.Value = "NM";
                    obx[i].ObservationSubID.Value = data.Result[i - 1].ITEM;
                    obx[i].ObservationIdentifier.Identifier.Value = data.Result[i - 1].FULL_NAME;

                    ce[i] = new CE(oruR01);
                    value = obx[i].GetObservationValue(0);
                    value.Data = ce[i];
                    ce[i].Identifier.Value = data.Result[i - 1].RESULT.ToString();
                    if (data.Result[i - 1].UNIT != string.Empty)
                    {
                        obx[i].Units.Identifier.Value = data.Result[i - 1].UNIT;
                    }
                    if (data.Result[i - 1].NORMAL_HIGH != 0)
                    {
                        obx[i].ReferencesRange.Value = data.Result[i - 1].NORMAL_LOW.ToString() + "---" + data.Result[i - 1].NORMAL_HIGH.ToString();
                        obx[i].GetAbnormalFlags(0).Value = data.Result[i - 1].INDICATE;
                    }
                    hl7.Item.Add(data.Result[i - 1].ITEM);
                }
            }
            #endregion

            hl7.HL7Message = Parser.Encode(oruR01);
            hl7.Sample_ID = data.SAMPLE_ID;
            hl7.Device = data.DEVEICE;
            hl7Manager.AddHL7(hl7);
        }
        public static void DSRequestSampleData(string sample_id, int device)//处理生化仪申请消息  QRY_Q02
        {
            HL7Manager.HL7RequestStruct hl7request = new HL7Manager.HL7RequestStruct();

            PipeParser Parser = new PipeParser();
            QRY_Q02 qryQ02 = new QRY_Q02();

            #region QRY_Q02样本申请封装
            qryQ02.MSH.FieldSeparator.Value = "|";
            qryQ02.MSH.EncodingCharacters.Value = @"^~\&";
            qryQ02.MSH.SendingApplication.NamespaceID.Value = "Mindray";
            qryQ02.MSH.SendingFacility.NamespaceID.Value = device == 0 ? "DS800" : (device == 1 ? "DS400" : string.Empty);
            qryQ02.MSH.DateTimeOfMessage.TimeOfAnEvent.SetLongDate(DateTime.Now);
            qryQ02.MSH.MessageType.MessageType.Value = "QRY";
            qryQ02.MSH.MessageType.TriggerEvent.Value = "Q02";
            qryQ02.MSH.MessageControlID.Value = "1";
            qryQ02.MSH.ProcessingID.ProcessingID.Value = "P";
            qryQ02.MSH.VersionID.VersionID.Value = "2.3.1";
            qryQ02.MSH.GetCharacterSet(0).Value = GlobalVariable.SocketCode ? "ASCII" : "UTF8";

            qryQ02.QRD.QueryDateTime.TimeOfAnEvent.SetLongDate(DateTime.Now);
            qryQ02.QRD.QueryFormatCode.Value = "R";
            qryQ02.QRD.QueryPriority.Value = "D";
            qryQ02.QRD.QueryID.Value = "1";
            qryQ02.QRD.QueryResultsLevel.Value = "RD";
            qryQ02.QRD.GetWhoSubjectFilter(0).IDNumber.Value = sample_id;//就为了这句话
            qryQ02.QRD.GetWhatSubjectFilter(0).Identifier.Value = "OTH";
            qryQ02.QRD.QueryResultsLevel.Value = "T";

            qryQ02.QRF.GetWhereSubjectFilter(0).Value = device == 0 ? "DS800" : (device == 1 ? "DS400" : null);
            qryQ02.QRF.WhenDataEndDateTime.TimeOfAnEvent.SetLongDate(DateTime.Now);
            qryQ02.QRF.WhenDataStartDateTime.TimeOfAnEvent.SetLongDate(DateTime.Now);
            qryQ02.QRF.GetWhichDateTimeQualifier(0).Value = "RCT";
            qryQ02.QRF.GetWhichDateTimeStatusQualifier(0).Value = "COR";
            qryQ02.QRF.GetDateTimeSelectionQualifier(0).Value = "ALL";
            #endregion

            hl7request.HL7RequestMessage = Parser.Encode(qryQ02);
            hl7request.RequestDevice = device == 0 ? "DS800" : (device == 1 ? "DS400" : string.Empty);
            hl7request.RequestSample_ID = sample_id;
            hl7Manager.AddHL7RequestSampleData(hl7request);
        }
        private string CreatACKQ03(string device)
        {
            PipeParser Parser = new PipeParser();
            ACK ack = new ACK();

            ack.MSH.FieldSeparator.Value = "|";
            ack.MSH.EncodingCharacters.Value = @"^~\&";
            ack.MSH.SendingApplication.NamespaceID.Value = "Mindray";//仪器供应商
            ack.MSH.SendingFacility.NamespaceID.Value = device;
            ack.MSH.DateTimeOfMessage.TimeOfAnEvent.SetLongDate(DateTime.Now);//当前时间
            ack.MSH.MessageType.MessageType.Value = "ACK";
            ack.MSH.MessageType.TriggerEvent.Value = "Q03";
            ack.MSH.MessageControlID.Value = "1";
            ack.MSH.ProcessingID.ProcessingID.Value = "P";
            ack.MSH.VersionID.VersionID.Value = "2.3.1";
            ack.MSH.GetCharacterSet(0).Value = GlobalVariable.SocketCode ? "ASCII" : "UTF8";

            ack.MSA.AcknowledgementCode.Value = "AA";
            ack.MSA.MessageControlID.Value = "1";
            ack.MSA.TextMessage.Value = "Message accepted";
            ack.MSA.ErrorCondition.Identifier.Value = "0";

            ack.ERR.GetErrorCodeAndLocation(0).SegmentID.Value = "0";

            return Parser.Encode(ack);
        }
        private HL7Manager.HL7SampleInfo HL7_ParserSampleInfo(string hl7data)
        {
            HL7Manager.HL7SampleInfo hl7info = new HL7Manager.HL7SampleInfo();
            hl7info.ExtraInfo = new List<HL7Manager.HL7ExtraInfo>();

            PipeParser parser = new PipeParser();
            IMessage m = parser.Parse(hl7data);
            DSR_Q03 dsr = m as DSR_Q03;

            #region 解析HL7申请样本信息
            hl7info.AdmissionNumber = dsr.GetDSP(0).DataLine.Value;
            hl7info.BedNumber = dsr.GetDSP(1).DataLine.Value;
            hl7info.PatientName = dsr.GetDSP(2).DataLine.Value;
            try
            {
                hl7info.DateOfBrith = DateTime.ParseExact(dsr.GetDSP(3).DataLine.Value, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
            }
            catch
            {
                hl7info.DateOfBrith = GlobalVariable.DefalutTime;
            }
            hl7info.Sex = dsr.GetDSP(4).DataLine.Value;
            hl7info.PatientAlias = dsr.GetDSP(5).DataLine.Value;
            hl7info.Race = dsr.GetDSP(6).DataLine.Value;
            hl7info.PatientAddress = dsr.GetDSP(7).DataLine.Value;
            hl7info.CountryCode = dsr.GetDSP(8).DataLine.Value;
            hl7info.HomePhoneNumber = dsr.GetDSP(9).DataLine.Value;
            hl7info.BusinessPhoneNumber = dsr.GetDSP(10).DataLine.Value;
            hl7info.PrimaryLanguage = dsr.GetDSP(11).DataLine.Value;
            hl7info.MaritalStatus = dsr.GetDSP(12).DataLine.Value;
            hl7info.Religion = dsr.GetDSP(13).DataLine.Value;
            hl7info.PatientAccoutNumber = dsr.GetDSP(14).DataLine.Value;
            hl7info.SocialSecurityNumber = dsr.GetDSP(15).DataLine.Value;
            hl7info.DriverLicenseNumber = dsr.GetDSP(16).DataLine.Value;
            hl7info.EthnicGroup = dsr.GetDSP(17).DataLine.Value;
            hl7info.BirthPlace = dsr.GetDSP(18).DataLine.Value;
            hl7info.Nationality = dsr.GetDSP(19).DataLine.Value;
            hl7info.BarCode = dsr.GetDSP(20).DataLine.Value;
            hl7info.SampleID = dsr.GetDSP(21).DataLine.Value;
            //hl7info.SampleID = "915066351347";
            try
            {
                hl7info.SampleTime = DateTime.ParseExact(dsr.GetDSP(22).DataLine.Value, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                //hl7info.SampleTime = GlobalVariable.DefalutTime;
            }
            catch
            {
                hl7info.SampleTime = GlobalVariable.DefalutTime;
            }
            hl7info.IsEmergency = dsr.GetDSP(23).DataLine.Value == null ? "N" : dsr.GetDSP(23).DataLine.Value;
            hl7info.CollcetionVolume = dsr.GetDSP(24).DataLine.Value;
            hl7info.SampleType = dsr.GetDSP(25).DataLine.Value;
            hl7info.SampleType = null;
            hl7info.FetchDoctor = dsr.GetDSP(26).DataLine.Value;
            hl7info.FetchDepartment = dsr.GetDSP(27).DataLine.Value;
            hl7info.Device = dsr.MSH.ReceivingFacility.NamespaceID.Value;
            hl7info.ExtraInfo = new List<HL7Manager.HL7ExtraInfo>();
            for (int num = 28; (dsr.DSPRepetitionsUsed > 28) && (num < dsr.DSPRepetitionsUsed); ++num)
            {
                HL7Manager.HL7ExtraInfo temp = new HL7Manager.HL7ExtraInfo();
                string tempString = dsr.GetDSP(num).DataLine.Value;
                string[] split = tempString.Split(new char[] { '^' });
                temp.TextID = split[0];
                temp.TextName = split[1];
                temp.Unit = split[2];
                temp.Normal = split[3];
                hl7info.ExtraInfo.Add(temp);
            }
            #endregion

            return hl7info;
        }
    }
}
