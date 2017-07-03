using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Data;
using System.Data.OleDb;
using MiddleWare.Views;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static MiddleWare.Communicate.PLManager;
using RestSharp;
//3个线程  2个队列 1个PL12Raw队列  1个PL12队列
namespace MiddleWare.Communicate
{
    public class Comm
    {
        public delegate void PortEventHandle(byte[] readBuffer, PLManager pm);
        public event PortEventHandle DataReceived;

        public static SerialPort serialPort;//此处转为静态,那就只能同时用一个COM口了.
        private const int dataSize = 607;

        private PLManager plManager;

        public static CancellationTokenSource CommCancel;

        public event GlobalVariable.MessageHandler CommMessage;
        public Comm(PLManager pm)
        {
            serialPort = new SerialPort();
            this.plManager = pm;

            CommCancel = new CancellationTokenSource();
        }

        public bool IsOpen
        {
            get
            {
                return serialPort.IsOpen;
            }
        }

        private void StartReading()
        {
            Task.Factory.StartNew(ReadPort, CommCancel.Token);
        }

        public static void StopReading()
        {
            CommCancel.Cancel();
        }

        byte[] readBuffer = new byte[dataSize - 4];//读取串口数据存放数组
        int[] header = new int[4];//数据开头标志位
        byte[] ender = new byte[4];//数据结尾标志位  
        byte checkBit;//校验位
        private void ReadPort()
        {
            while (!CommCancel.IsCancellationRequested) 
            {
                if (serialPort.IsOpen)
                {
                    int count;//串口缓冲区大小
                    try
                    {
                        count = serialPort.BytesToRead;
                    }
                    catch
                    {
                        StopReading();
                        return;//不发送任务取消请求,是为了下次还能重新打开串口
                    }
                    if (count >= dataSize)//一次是607个字节
                    {
                        header[0] = serialPort.ReadByte();
                        if (header[0] == 0x02)
                        {
                            header[1] = serialPort.ReadByte();
                            if (header[1] == 0x02)
                            {
                                header[2] = serialPort.ReadByte();
                                if (header[2] == 0x5F)
                                {
                                    header[3] = serialPort.ReadByte();
                                    if (header[3] == 0x38)
                                    {
                                        //Console.WriteLine("数据开头正确 准备读取数据");
                                        serialPort.Read(readBuffer, 0, dataSize - 4);

                                        for (int i = 0; i <= 3; i++)
                                        {
                                            ender[i] = readBuffer[i + 598];
                                        }
                                        if (ender[0] == 0 && ender[1] == 0 && ender[2] == 0 && ender[3] == 0x03)
                                        {
                                            //进入最后一位校验位
                                            checkBit = 0x02 ^ 0x5F ^ 0x38;
                                            for (int i = 0; i <= 601; i++)
                                            {
                                                checkBit ^= readBuffer[i];
                                            }
                                            if (checkBit == readBuffer[602])
                                            {
                                                Console.WriteLine("1");
                                                DataReceived.BeginInvoke(readBuffer, plManager, null, null);
                                                ++Statusbar.SBar.ReceiveNum;
                                                Thread.Sleep(500);
                                            }
                                            else
                                                continue;
                                        }
                                        else
                                            continue;
                                    }
                                    else
                                        continue;
                                }
                                else
                                    continue;
                            }
                            else
                                continue;
                        }
                        else
                            continue;
                    }
                }
                else
                {
                    CommMessage.Invoke("串口已断开\r\n", "DEVICE");
                    CommCancel.Cancel();
                }
            }
        }

        public void Open()
        {
            Close();
            serialPort.Open();
            if (serialPort.IsOpen)
            {
                //CommMessage.Invoke("串口打开成功\r\n", "DEVICE"); //自动连接时
                StartReading();
            }
            else
            {
               //CommMessage.Invoke("串口打开失败\r\n", "DEVICE");
            }
        }

        public static void Close()
        {
            serialPort.Close();
        }
    }

    public class PLManager
    {
        private object PL12Locker = new object();
        private object PL12RawLocker = new object();

        public struct PL12Raw
        {
            public string Device;
            public int Type;
            public int PLT_0;
            public double MPV_0;
            public double MAR;
            public int MAT;
            public double AAR;
            public double RBC_0;
            public int MCV_0;//MCV取整数
            public int PLT1;
            public int PLT2;
            public int PLT3;
            public int PLT4;
            public int PLT5;
            public double MPV1;
            public double MPV2;
            public double MPV3;
            public double MPV4;
            public double MPV5;
            public double RBC1;
            public double RBC2;
            public double RBC3;
            public double RBC4;
            public double RBC5;
            public int Index;//数据类型  MSH
            public DateTime TIME;//MSH
            public int PACBit;//PAC图形有效点数
            public int PAC1;//PAC图形第1点Y轴高度
            public int PAC2;
            public int PAC3;
            public int PAC4;
            public int PAC5;
            public int PAC6;
            public int PAC7;
            public int PAC8;
            public double PDW;
            public double RDW;
            public int MCV1;
            public int MCV2;
            public int MCV3;
            public int MCV4;
            public int MCV5;
            public double INH;
            public double A_INH;
            public double R_MAR;
            public double AR_1;//反应聚集率
            public double AR_2;
            public double AR_3;
            public double AR_4;
            public double AR_5;
            public double AR_6;
            public double INH_1;//反应抑制率  
            public double INH_2;
            public double INH_3;
            public double INH_4;
            public double INH_5;
            public double INH_6;
            public int ARBit;//聚集率有效位数 不传
            public int DataBit;//数据有效位数 不传
            public string AAP;//诱聚剂项目 放在第一个结果
            public int Version;//协议版本号 MSH
            public string RBCHist;//RBC直方图
            public string PLTHist;//PLT直方图
            public string BarCode;//条形码 MSH
            public string ID;//样本ID MSH
        }
        public struct PL12Result
        {
            public string ITEM;
            public string FULL_NAME;
            public string RESULT;
            public string UNIT;
            public double NORMAL_LOW;
            public double NORMAL_HIGH;
            public string INDICATE;
        }
        public struct PL12
        {
            public string SAMPLE_ID;//样品号
            public string BARCODE;//条形码
            public DateTime TEST_TIME;//测试时间
            public string DEVEICE;//仪器设备
            public string AAP;//诱聚剂项目
            public string TYPE;//血小板
            public string SAMPLE_KIND;//样本类型  等于1就代表检测结果
            public bool ISSEND;
            public List<PL12Result> Result;
        }
        public struct PL12Web
        {
            public string SAMPLE_ID;//样品号
            public string BarCode;//条形码
            public DateTime TEST_TIME;//测试时间
            public string DEVICE;//仪器设备
            public string AAP;//诱聚剂项目
            //public string TYPE;//血小板
            public string SAMPLE_KIND;//样本类型  等于1就代表检测结果
            public bool ISSEND;
            //public List<PL12Result> Result;
            public string ITEM;
            public string FULL_NAME;
            public string RESULT;
            public string UNIT;
            public double NORMAL_lOW;
            public double NORMAL_HIGH;
            public string INDICATE;
        }

        private readonly Queue<PL12> pl12Queue = new Queue<PL12>();
        private readonly Queue<PL12Raw> pl12RawQueue = new Queue<PL12Raw>();

        public ManualResetEvent PLRawSignal = new ManualResetEvent(false);
        public ManualResetEvent PLSignal = new ManualResetEvent(false);

        public void AddPL12(PL12 data)
        {
            lock (PL12Locker)
            {
                pl12Queue.Enqueue(data);
            }
        }
        public void AddPL12Raw(PL12Raw data)
        {
            lock (PL12RawLocker)
            {
                pl12RawQueue.Enqueue(data);
            }
        }
        public PL12 GetPL12()
        {
            lock (PL12Locker)
            {
                return pl12Queue.Dequeue();
            }
        }
        public PL12Raw GetPL12Raw()
        {
            lock (PL12RawLocker)
            {
                return pl12RawQueue.Dequeue();
            }
        }
        public bool IsPl12Availabel
        {
            get
            {
                return pl12Queue.Count > 0;
            }
        }
        public bool IsPl12RawAvailabel
        {
            get
            {
                return pl12RawQueue.Count > 0;
            }
        }

    }

    public class ProcessPLs
    {
        public delegate void PLEventHandle(object PLdata,string name);
        public event PLEventHandle PLtransmit;

        private PLManager plManager;
        public static CancellationTokenSource ProcessPLsCancel;

        public ProcessPLs(PLManager pm)
        {
            this.plManager = pm;

            ProcessPLsCancel = new CancellationTokenSource();
        }
        public void Start()
        {
            Task.Factory.StartNew(RunPL, ProcessPLsCancel.Token);
        }
        private void RunPL()
        {
            while (!ProcessPLsCancel.IsCancellationRequested)
            {
                plManager.PLSignal.WaitOne();
                if (plManager.IsPl12Availabel)
                {
                    PLManager.PL12 pl12 = plManager.GetPL12();
                    PLtransmit.Invoke(pl12, "PL");
                }
                else
                {
                    plManager.PLSignal.Reset();
                }
                Thread.Sleep(100);//这个时间可以商榷
            }
        }

        /// <summary>
        /// byte[]转16进制格式string
        /// 比如{0x30, 0x31}转成"3031"
        /// </summary>
        /// <param name="bytes">byte[]数组名称</param>
        /// <param name="beg">数组开始位置</param>
        /// <param name="num">转换个数</param>
        /// <returns></returns>
        public static string ToHexString(byte[] bytes, int beg, int num)
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                if (beg < bytes.Length)
                {
                    if (beg + num > bytes.Length)
                    {
                        num = bytes.Length - beg;
                    }
                    StringBuilder str = new StringBuilder();
                    for (int i = 0; i < num; i++)
                    {
                        str.Append(bytes[i + beg].ToString("X2"));
                    }
                    hexString = str.ToString();
                }
            }
            return hexString;
        }
        public static void DataReceived(byte[] readBuffer, PLManager pm)//PL12原始数据库队列是否为空判断在写数据库操作中
        {
            PLManager.PL12Raw data = new PLManager.PL12Raw();
            #region 串口数据解析为原始结构体
            data.PLT_0 = readBuffer[1] << 8 | readBuffer[2];
            data.MPV_0 = (readBuffer[3] << 8 | readBuffer[4]) / 100.0;
            data.MAR = (readBuffer[5] << 8 | readBuffer[6]) / 10;
            data.MAT = readBuffer[7] << 8 | readBuffer[8];
            data.AAR = (readBuffer[9] << 8 | readBuffer[10]) / 10.0;
            data.RBC_0 = (readBuffer[11] << 8 | readBuffer[12]) / 100.0;
            data.MCV_0 = (readBuffer[13] << 8 | readBuffer[14]) / 10;
            data.PLT1 = readBuffer[15] << 8 | readBuffer[16];
            data.PLT2 = readBuffer[17] << 8 | readBuffer[18];
            data.PLT3 = readBuffer[19] << 8 | readBuffer[20];
            data.PLT4 = readBuffer[21] << 8 | readBuffer[22];
            data.PLT5 = readBuffer[23] << 8 | readBuffer[24];
            data.MPV1 = (readBuffer[31] << 8 | readBuffer[32]) / 100.0;
            data.MPV2 = (readBuffer[33] << 8 | readBuffer[34]) / 100.0;
            data.MPV3 = (readBuffer[35] << 8 | readBuffer[36]) / 100.0;
            data.MPV4 = (readBuffer[37] << 8 | readBuffer[38]) / 100.0;
            data.MPV5 = (readBuffer[39] << 8 | readBuffer[40]) / 100.0;
            data.RBC1 = (readBuffer[47] << 8 | readBuffer[48]) / 100.0;
            data.RBC2 = (readBuffer[49] << 8 | readBuffer[50]) / 100.0;
            data.RBC3 = (readBuffer[51] << 8 | readBuffer[52]) / 100.0;
            data.RBC4 = (readBuffer[53] << 8 | readBuffer[54]) / 100.0;
            data.RBC5 = (readBuffer[55] << 8 | readBuffer[56]) / 100.0;
            data.Index = readBuffer[57];
            try
            {
                data.TIME = DateTime.ParseExact(ASCIIEncoding.ASCII.GetString(readBuffer, 65, 14), "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
            }
            catch
            {
                data.TIME = Convert.ToDateTime("1900-01-01 00:00:00");//如果传送时间为空,则改为当前时间
            }
            data.PACBit = readBuffer[79];
            data.PAC1 = readBuffer[80];
            data.PAC2 = readBuffer[81];
            data.PAC3 = readBuffer[82];
            data.PAC4 = readBuffer[83];
            data.PAC5 = readBuffer[84];
            data.PAC6 = readBuffer[85];
            data.PAC7 = readBuffer[86];
            data.PAC8 = readBuffer[87];
            data.PDW = (readBuffer[88] << 8 | readBuffer[89]) / 100.0;
            data.RDW = (readBuffer[90] << 8 | readBuffer[91]) / 100.0;
            data.MCV1 = (readBuffer[92] << 8 | readBuffer[93]) / 10;
            data.MCV2 = (readBuffer[94] << 8 | readBuffer[95]) / 10;
            data.MCV3 = (readBuffer[96] << 8 | readBuffer[97]) / 10;
            data.MCV4 = (readBuffer[98] << 8 | readBuffer[99]) / 10;
            data.MCV5 = (readBuffer[100] << 8 | readBuffer[101]) / 10;
            data.INH = (readBuffer[108] << 8 | readBuffer[109]) / 10.0;
            data.A_INH = (readBuffer[110] << 8 | readBuffer[101]) / 10.0;
            data.R_MAR = (readBuffer[112] << 8 | readBuffer[113]) / 10.0;
            data.AR_1 = (readBuffer[114] << 8 | readBuffer[115]) / 10.0;
            data.AR_2 = (readBuffer[116] << 8 | readBuffer[117]) / 10.0;
            data.AR_3 = (readBuffer[118] << 8 | readBuffer[119]) / 10.0;
            data.AR_4 = (readBuffer[120] << 8 | readBuffer[121]) / 10.0;
            data.AR_5 = (readBuffer[122] << 8 | readBuffer[123]) / 10.0;
            data.AR_6 = (readBuffer[124] << 8 | readBuffer[125]) / 10.0;
            data.INH_1 = (readBuffer[126] << 8 | readBuffer[127]) / 10.0;
            data.INH_2 = (readBuffer[128] << 8 | readBuffer[129]) / 10.0;
            data.INH_3 = (readBuffer[130] << 8 | readBuffer[131]) / 10.0;
            data.INH_4 = (readBuffer[132] << 8 | readBuffer[133]) / 10.0;
            data.INH_5 = (readBuffer[134] << 8 | readBuffer[135]) / 10.0;
            data.INH_6 = (readBuffer[136] << 8 | readBuffer[137]) / 10.0;
            data.ARBit = readBuffer[187];
            data.DataBit = readBuffer[188];
            switch(readBuffer[189])
            {
                case 1:data.AAP = "花生四烯酸";break;
                case 2:data.AAP = "二磷酸腺苷";break;
                case 3:data.AAP = "胶原";break;
                case 4:data.AAP = "肾上腺素";break;
                case 5:data.AAP = "凝血酶";break;
                case 6:data.AAP = "瑞斯托霉素";break;
                case 7:data.AAP = "INHI-IIb/IIIa IIIa";break;
                default:data.AAP = string.Empty;break;
            }
            data.Version = readBuffer[190];
            data.RBCHist = ToHexString(readBuffer, 191, 256);
            data.PLTHist = ToHexString(readBuffer, 447, 128);
            data.BarCode = ASCIIEncoding.ASCII.GetString(readBuffer, 576, 15);
            data.ID = string.Empty;
            for (int i = 0; i < 6; ++i)
            {
                data.ID += readBuffer[592 + i].ToString();
            }
            data.Device = "PL_12";
            data.Type = 1;
            #endregion
            pm.AddPL12Raw(data);
            pm.PLRawSignal.Set();
        }
    }

    public class ShareAccessPL
    {
        public static Mutex mutex = new Mutex();//互斥锁
    }

    public class WriteAccessPL
    {
        private string strConnection;
        private static OleDbConnection conn;
        private string strInsert;
        private string strJudge;
        private string strInsertLack;
        private string strSelectInfo;
        private string strSelectName;
        private double Normal_Low;
        private double Normal_High;
        private string Unit;
        private string Name;

        private OleDbCommand cmd;
        private OleDbDataAdapter oaInfo;
        private OleDbDataAdapter oaName;
        private DataTable dtInfo;
        private DataTable dtName;
        private DataSet ds;

        private static int ItemNum;
        private static string strIns;

        public delegate void NoticeRead(string selectName, string SAMPLE_ID);
        public event NoticeRead NoticeReadMessage;

        public event GlobalVariable.MessageHandler WriteAccessPLMessage;
        public static CancellationTokenSource WriteAccessPLCancel;

        private PLManager plManager;

        private List<PL12Web> ListPL12Result;
        public WriteAccessPL(PLManager pm)
        {
            strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
            string pathto = GlobalVariable.topDir.Parent.FullName;
            strConnection += "Data Source=" + @pathto + "\\PLDB.mdb";
            conn = new OleDbConnection(strConnection);
            ds = new DataSet();
            this.plManager = pm;

            WriteAccessPLCancel = new CancellationTokenSource();
        }
        public void Start()
        {
            Task.Factory.StartNew(Run, WriteAccessPLCancel.Token);
        }
        public void Run()
        {
            while (!WriteAccessPLCancel.IsCancellationRequested)
            {
                plManager.PLRawSignal.WaitOne();
                if (plManager.IsPl12RawAvailabel)
                {
                    WriteData(plManager.GetPL12Raw());
                }
                else
                {
                    plManager.PLRawSignal.Set();
                }
                Thread.Sleep(300);
            }
        }
        private void WriteDataAccessWebOperation(OleDbCommand cmd,PL12Raw data,string item,string name,string result,string unit,double low,double high,string indicate)
        {
            cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = item;//"MPV1";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = name;// Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = result;// data.MPV1;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = unit;// Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = low;// Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = high;// Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = indicate;//data.MPV1 > Normal_High ? "H" : (data.MPV1 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();

            ListPL12Result.Add(new PL12Web()
            {
                SAMPLE_ID = data.ID,
                BarCode = data.BarCode,
                TEST_TIME = data.TIME,
                DEVICE = data.Device,
                AAP = data.AAP,
                SAMPLE_KIND = data.Type.ToString(),
                ITEM = item,
                FULL_NAME = name,
                RESULT = result,
                UNIT = unit,
                NORMAL_lOW = low,
                NORMAL_HIGH = high,
                INDICATE = indicate,
                ISSEND = false
            });
        }
        public void WriteData(PLManager.PL12Raw data)
        {
            ShareAccessPL.mutex.WaitOne();
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();
            }

            strJudge = "select * from PL_lisoutput where [SAMPLE_ID]='" + data.ID + "' AND [DEVICE]='" + data.Device + "' AND [TEST_TIME] BETWEEN #" + data.TIME.ToString() + "# AND #" + data.TIME.ToString() + "#";
            using (OleDbDataAdapter oaJudge = new OleDbDataAdapter(strJudge, conn))//判断是否写入重复
            {
                try
                {
                    if (oaJudge.Fill(ds) != 0)
                    {
                        conn.Close();
                        ShareAccessPL.mutex.ReleaseMutex();
                        WriteAccessPLMessage.Invoke(data.ID + "数据库写入重复\r\n", "DEVICE");
                        NoticeReadMessage.Invoke("SAMPLE_ID", data.ID);
                        return;
                    }
                }
                finally
                {
                    ds.Clear();
                }
            }

            strInsert = "insert into PL_lisoutput(SAMPLE_ID,BarCode,TEST_TIME,DEVICE,AAP,SAMPLE_KIND,ITEM,FULL_NAME,RESULT,UNIT,NORMAL_lOW,NORMAL_HIGH,INDICATE,ISSEND) " +
                    "values (@SAMPLE_ID,@BarCode,@TEST_TIME,@DEVICE,@AAP,@SAMPLE_KIND,@ITEM,@FULL_NAME,@RESULT,@UNIT,@NORMAL_lOW,@NORMAL_HIGH,@INDICATE,@ISSEND)";
            strInsertLack = "insert into PL_lisoutput(SAMPLE_ID,BarCode,TEST_TIME,DEVICE,AAP,SAMPLE_KIND,ITEM,FULL_NAME,RESULT,UNIT,ISSEND) " +
                "values (@SAMPLE_ID,@BarCode,@TEST_TIME,@DEVICE,@AAP,@SAMPLE_KIND,@ITEM,@FULL_NAME,@RESULT,@UNIT,@ISSEND)";

            ListPL12Result = new List<PL12Web>();

            #region MPV封装
            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='MPV'";
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];

            strSelectName = "select * from PL_FullName where ITEM ='MPV_0'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MPV_0";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MPV_0;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MPV_0 > Normal_High ? "H" : (data.MPV_0 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;

            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd,data, "MPV_0",Name,data.MPV_0.ToString(),Unit,Normal_Low,Normal_High, data.MPV_0 > Normal_High ? "H" : (data.MPV_0 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='MPV1'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MPV1";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MPV1;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MPV1 > Normal_High ? "H" : (data.MPV1 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MPV1", Name, data.MPV1.ToString(), Unit, Normal_Low, Normal_High, data.MPV1 > Normal_High ? "H" : (data.MPV1 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='MPV2'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MPV2";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MPV2;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MPV2 > Normal_High ? "H" : (data.MPV2 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MPV2", Name, data.MPV2.ToString(), Unit, Normal_Low, Normal_High, data.MPV2 > Normal_High ? "H" : (data.MPV2 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='MPV3'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MPV3";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MPV3;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MPV3 > Normal_High ? "H" : (data.MPV3 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MPV3", Name, data.MPV3.ToString(), Unit, Normal_Low, Normal_High, data.MPV3 > Normal_High ? "H" : (data.MPV3 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='MPV4'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MPV4";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MPV4;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MPV4 > Normal_High ? "H" : (data.MPV4 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MPV4", Name, data.MPV4.ToString(), Unit, Normal_Low, Normal_High, data.MPV4 > Normal_High ? "H" : (data.MPV4 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='MPV5'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MPV5";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MPV5;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MPV5 > Normal_High ? "H" : (data.MPV5 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MPV5", Name, data.MPV5.ToString(), Unit, Normal_Low, Normal_High, data.MPV5 > Normal_High ? "H" : (data.MPV5 < Normal_Low ? "L" : "N"));

            #endregion
            #region MCV封装
            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='MCV'";
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];
            strSelectName = "select * from PL_FullName where ITEM ='MCV_0'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MCV_0";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MCV_0;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MCV_0 > Normal_High ? "H" : (data.MCV_0 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MCV_0", Name, data.MCV_0.ToString(), Unit, Normal_Low, Normal_High, data.MCV_0 > Normal_High ? "H" : (data.MCV_0 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='MCV1'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MCV1";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MCV1;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MCV1 > Normal_High ? "H" : (data.MCV1 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MCV1", Name, data.MCV1.ToString(), Unit, Normal_Low, Normal_High, data.MCV1 > Normal_High ? "H" : (data.MCV1 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='MCV2'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MCV2";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MCV2;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MCV2 > Normal_High ? "H" : (data.MCV2 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MCV2", Name, data.MCV2.ToString(), Unit, Normal_Low, Normal_High, data.MCV2 > Normal_High ? "H" : (data.MCV2 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='MCV3'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MCV3";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MCV3;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MCV3 > Normal_High ? "H" : (data.MCV3 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MCV3", Name, data.MCV3.ToString(), Unit, Normal_Low, Normal_High, data.MCV3 > Normal_High ? "H" : (data.MCV3 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='MCV4'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MCV4";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MCV4;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MCV4 > Normal_High ? "H" : (data.MCV4 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MCV4", Name, data.MCV4.ToString(), Unit, Normal_Low, Normal_High, data.MCV4 > Normal_High ? "H" : (data.MCV4 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='MCV5'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MCV5";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MCV5;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MCV5 > Normal_High ? "H" : (data.MCV5 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MCV5", Name, data.MCV5.ToString(), Unit, Normal_Low, Normal_High, data.MCV5 > Normal_High ? "H" : (data.MCV5 < Normal_Low ? "L" : "N"));

            #endregion
            #region 反应聚集率
            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='AAR'";//平均聚集率
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];
            strSelectName = "select * from PL_FullName where ITEM ='AAR'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "AAR";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.AAR;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.AAR > Normal_High ? "H" : (data.AAR < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "AAR", Name, data.AAR.ToString(), Unit, Normal_Low, Normal_High, data.AAR > Normal_High ? "H" : (data.AAR < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='MAR'";//最大聚集率
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];
            strSelectName = "select * from PL_FullName where ITEM ='MAR'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MAR";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MAR;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MAR > Normal_High ? "H" : (data.MAR < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MAR", Name, data.MAR.ToString(), Unit, Normal_Low, Normal_High, data.MAR > Normal_High ? "H" : (data.MAR < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='AR_1'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "AR_1";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.AR_1;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "AR_1", Name, data.AR_1.ToString(), Unit, 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='AR_2'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "AR_2";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.AR_2;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "AR_2", Name, data.AR_2.ToString(), Unit, 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='AR_3'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "AR_3";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.AR_3;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "AR_3", Name, data.AR_3.ToString(), Unit, 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='AR_4'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "AR_4";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.AR_4;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "AR_4", Name, data.AR_4.ToString(), Unit, 0, 0, "");     


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='AR_5'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "AR_5";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.AR_5;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "AR_5", Name, data.AR_5.ToString(), Unit, 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='AR_6'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "AR_6";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.AR_6;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "AR_6", Name, data.AR_6.ToString(), Unit, 0, 0, "");

            #endregion
            #region 反应抑制率
            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='A_INH'";
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];
            strSelectName = "select * from PL_FullName where ITEM ='A_INH'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "A_INH";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.A_INH;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.A_INH > Normal_High ? "H" : (data.A_INH < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "A_INH", Name, data.A_INH.ToString(), Unit, Normal_Low, Normal_High, data.A_INH > Normal_High ? "H" : (data.A_INH < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='INH'";
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];
            strSelectName = "select * from PL_FullName where ITEM ='INH'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "INH";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.INH;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.INH > Normal_High ? "H" : (data.INH < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "INH", Name, data.INH.ToString(), Unit, Normal_Low, Normal_High, data.INH > Normal_High ? "H" : (data.INH < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='INH_1'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "INH_1";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.INH_1;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "INH_1", Name, data.INH_1.ToString(), Unit, 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='INH_2'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "INH_2";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.INH_2;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "INH_2", Name, data.INH_2.ToString(), Unit, 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='INH_3'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "INH_3";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.INH_3;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "INH_3", Name, data.INH_3.ToString(), Unit, 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='INH_4'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "INH_4";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.INH_4;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "INH_4", Name, data.INH_4.ToString(), Unit, 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='INH_5'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "INH_5";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.INH_5;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "INH_5", Name, data.INH_5.ToString(), Unit, 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='INH_6'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "INH_6";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.INH_6;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "INH_6", Name, data.INH_6.ToString(), Unit, 0, 0, "");

            #endregion
            #region PAC高度
            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PACBit'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PACBit";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PACBit;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = "";
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PACBit", Name, data.PACBit.ToString(), "", 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PAC1'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PAC1";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PAC1;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = "";
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PAC1", Name, data.PAC1.ToString(), "", 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PAC2'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PAC2";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PAC2;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = "";
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PAC2", Name, data.PAC2.ToString(), "", 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PAC3'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PAC3";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PAC3;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = "";
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PAC3", Name, data.PAC3.ToString(), "", 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PAC4'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PAC4";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PAC4;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = "";
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PAC4", Name, data.PAC4.ToString(), "", 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PAC5'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PAC5";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PAC5;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = "";
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PAC5", Name, data.PAC5.ToString(), "", 0, 0, "");

            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PAC6'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PAC6";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PAC6;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = "";
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PAC6", Name, data.PAC6.ToString(), "", 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PAC7'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PAC7";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PAC7;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = "";
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PAC7", Name, data.PAC7.ToString(), "", 0, 0, "");

            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PAC8'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PAC8";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PAC8;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = "";
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PAC8", Name, data.PAC8.ToString(), "", 0, 0, "");

            #endregion
            #region 血小板数量
            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='PLT'";
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];
            strSelectName = "select * from PL_FullName where ITEM ='PLT_0'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PLT_0";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PLT_0;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.PLT_0 > Normal_High ? "H" : (data.PLT_0 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PLT_0", Name, data.PLT_0.ToString(), Unit, Normal_Low, Normal_High, data.PLT_0 > Normal_High ? "H" : (data.PLT_0 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PLT1'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PLT1";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PLT1;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.PLT1 > Normal_High ? "H" : (data.PLT1 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PLT1", Name, data.PLT1.ToString(), Unit, Normal_Low, Normal_High, data.PLT1 > Normal_High ? "H" : (data.PLT1 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PLT2'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PLT2";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PLT2;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.PLT2 > Normal_High ? "H" : (data.PLT2 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PLT2", Name, data.PLT2.ToString(), Unit, Normal_Low, Normal_High, data.PLT2 > Normal_High ? "H" : (data.PLT2 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PLT3'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PLT3";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PLT3;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.PLT3 > Normal_High ? "H" : (data.PLT3 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PLT3", Name, data.PLT3.ToString(), Unit, Normal_Low, Normal_High, data.PLT3 > Normal_High ? "H" : (data.PLT3 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PLT4'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PLT4";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PLT4;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.PLT4 > Normal_High ? "H" : (data.PLT4 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PLT4", Name, data.PLT4.ToString(), Unit, Normal_Low, Normal_High, data.PLT4 > Normal_High ? "H" : (data.PLT4 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PLT5'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PLT5";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PLT5;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.PLT5 > Normal_High ? "H" : (data.PLT5 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PLT5", Name, data.PLT5.ToString(), Unit, Normal_Low, Normal_High, data.PLT5 > Normal_High ? "H" : (data.PLT5 < Normal_Low ? "L" : "N"));

            #endregion
            #region 红细胞数量
            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='RBC'";
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];
            strSelectName = "select * from PL_FullName where ITEM ='RBC_0'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "RBC_0";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.RBC_0;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.RBC_0 > Normal_High ? "H" : (data.RBC_0 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "RBC_0", Name, data.RBC_0.ToString(), Unit, Normal_Low, Normal_High, data.RBC_0 > Normal_High ? "H" : (data.RBC_0 < Normal_Low ? "L" : "N"));


            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='RBC1'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "RBC1";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.RBC1;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.RBC1 > Normal_High ? "H" : (data.RBC1 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "RBC1", Name, data.RBC1.ToString(), Unit, Normal_Low, Normal_High, data.RBC1 > Normal_High ? "H" : (data.RBC1 < Normal_Low ? "L" : "N"));

            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='RBC2'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "RBC2";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.RBC2;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.RBC2 > Normal_High ? "H" : (data.RBC2 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "RBC2", Name, data.RBC2.ToString(), Unit, Normal_Low, Normal_High, data.RBC2 > Normal_High ? "H" : (data.RBC2 < Normal_Low ? "L" : "N"));

            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='RBC3'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "RBC3";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.RBC3;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.RBC3 > Normal_High ? "H" : (data.RBC3 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "RBC3", Name, data.RBC3.ToString(), Unit, Normal_Low, Normal_High, data.RBC3 > Normal_High ? "H" : (data.RBC3 < Normal_Low ? "L" : "N"));

            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='RBC4'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "RBC4";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.RBC4;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.RBC4 > Normal_High ? "H" : (data.RBC4 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "RBC4", Name, data.RBC4.ToString(), Unit, Normal_Low, Normal_High, data.RBC4 > Normal_High ? "H" : (data.RBC4 < Normal_Low ? "L" : "N"));

            cmd = new OleDbCommand(strInsert, conn);
            strSelectName = "select * from PL_FullName where ITEM ='RBC5'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "RBC5";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.RBC5;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.RBC5 > Normal_High ? "H" : (data.RBC5 < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "RBC5", Name, data.RBC5.ToString(), Unit, Normal_Low, Normal_High, data.RBC5 > Normal_High ? "H" : (data.RBC5 < Normal_Low ? "L" : "N"));

            #endregion
            #region 最大聚集时间
            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='MAT'";
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];

            strSelectName = "select * from PL_FullName where ITEM ='MAT'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "MAT";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.MAT;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.MAT > Normal_High ? "H" : (data.MAT < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "MAT", Name, data.MAT.ToString(), Unit, Normal_Low, Normal_High, data.MAT > Normal_High ? "H" : (data.MAT < Normal_Low ? "L" : "N"));

            #endregion
            #region 原始血小板分布宽度
            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='PDW'";
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];

            strSelectName = "select * from PL_FullName where ITEM ='PDW'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PDW";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PDW;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.PDW > Normal_High ? "H" : (data.PDW < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PDW", Name, data.PDW.ToString(), Unit, Normal_Low, Normal_High, data.PDW > Normal_High ? "H" : (data.PDW < Normal_Low ? "L" : "N"));

            #endregion
            #region 原始红细胞分布宽度
            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='RDW'";
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];

            strSelectName = "select * from PL_FullName where ITEM ='RDW'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PDW";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PDW;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.PDW > Normal_High ? "H" : (data.PDW < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "RDW", Name, data.RDW.ToString(), Unit, Normal_Low, Normal_High, data.RDW > Normal_High ? "H" : (data.RDW < Normal_Low ? "L" : "N"));

            #endregion
            #region 红细胞最大聚集率
            cmd = new OleDbCommand(strInsert, conn);
            strSelectInfo = "select * from PL_ExtraInfo where ITEM ='R_MAR'";
            oaInfo = new OleDbDataAdapter(strSelectInfo, conn);
            dtInfo = new DataTable();
            oaInfo.Fill(dtInfo);
            Normal_Low = (double)dtInfo.Rows[0]["NORMAL_LOW"];
            Normal_High = (double)dtInfo.Rows[0]["NORMAL_HIGH"];
            Unit = (string)dtInfo.Rows[0]["UNIT"];

            strSelectName = "select * from PL_FullName where ITEM ='R_MAR'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "R_MAR";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.R_MAR;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = Unit;
            cmd.Parameters.Add("@NORMAL_lOW", OleDbType.VarChar).Value = Normal_Low;
            cmd.Parameters.Add("@NORMAL_HIGH", OleDbType.VarChar).Value = Normal_High;
            cmd.Parameters.Add("@INDICATE", OleDbType.VarChar).Value = data.R_MAR > Normal_High ? "H" : (data.R_MAR < Normal_Low ? "L" : "N");
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "R_MAR", Name, data.R_MAR.ToString(), Unit, Normal_Low, Normal_High, data.R_MAR > Normal_High ? "H" : (data.R_MAR < Normal_Low ? "L" : "N"));

            #endregion
            #region 直方图 没有单位
            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='PLTHist'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "PLTHist";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.PLTHist;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = "";
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "PLTHist", Name, data.PLTHist.ToString(), "", 0, 0, "");


            cmd = new OleDbCommand(strInsertLack, conn);
            strSelectName = "select * from PL_FullName where ITEM ='RBCHist'";
            oaName = new OleDbDataAdapter(strSelectName, conn);
            dtName = new DataTable();
            oaName.Fill(dtName);
            Name = (string)dtName.Rows[0]["FULL_NAME"];
            /*cmd.Parameters.Add("@SAMPLE_ID", OleDbType.VarChar).Value = data.ID;
            cmd.Parameters.Add("@BarCode", OleDbType.VarChar).Value = data.BarCode;
            cmd.Parameters.Add("@TEST_TIME", OleDbType.VarChar).Value = data.TIME;
            cmd.Parameters.Add("@DEVICE", OleDbType.VarChar).Value = data.Device;
            cmd.Parameters.Add("@AAP", OleDbType.VarChar).Value = data.AAP;
            cmd.Parameters.Add("@SAMPLE_KIND", OleDbType.VarChar).Value = data.Type;
            cmd.Parameters.Add("@ITEM", OleDbType.VarChar).Value = "RBCHist";
            cmd.Parameters.Add("@FULL_NAME", OleDbType.VarChar).Value = Name;
            cmd.Parameters.Add("@RESULT", OleDbType.VarChar).Value = data.RBCHist;
            cmd.Parameters.Add("@UNIT", OleDbType.VarChar).Value = "";
            cmd.Parameters.Add("@ISSEND", OleDbType.Integer).Value = false;
            cmd.ExecuteNonQuery();*/
            WriteDataAccessWebOperation(cmd, data, "RBCHist", Name, data.RBCHist.ToString(), "", 0, 0, "");

            #endregion

            /*
            string json = JsonConvert.SerializeObject(ListPL12Result);
            var client = new RestClient();
            client.BaseUrl = new Uri(GlobalVariable.BaseUrl);//http://localhost:8080/MiddlewareWeb
            var request = new RestRequest("PL/PLResult", Method.POST);
            request.AddParameter("plJSON", json);
            IRestResponse response = client.Execute(request);
            */

            conn.Close();
            ShareAccessPL.mutex.ReleaseMutex();
            WriteAccessPLMessage.Invoke(data.ID + "写入数据库成功\r\n", "DEVICE");
            NoticeReadMessage.Invoke("SAMPLE_ID", data.ID);
        }
        public static void UpdateDB(string SAMPLE_ID, List<string> ITEM, string DEVICE)
        {
            if (DEVICE != "PL_12") 
            {
                return;
            }
            ShareAccessPL.mutex.WaitOne();
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();//打开连接
            }
            ItemNum = ITEM.Count;
            for (int i = 0; i < ItemNum; i++)
            {
                strIns = "update PL_lisoutput set ISSEND='" + "1" + "'" + " where " + "SAMPLE_ID='" + SAMPLE_ID + "'" + " and " + " ITEM='" + ITEM[i] + "'";

                using (OleDbCommand command = new OleDbCommand(strIns, conn))
                {
                    command.ExecuteNonQuery();

                    var tempEntity = new
                    {
                        SAMPLE_ID = SAMPLE_ID,
                        ITEM = ITEM[i],
                        DEVICE = DEVICE
                    };
                    /*
                    string json = JsonConvert.SerializeObject(tempEntity);
                    var client = new RestClient();
                    client.BaseUrl = new Uri(GlobalVariable.BaseUrl);//http://localhost:8080/MiddlewareWeb
                    var request = new RestRequest("PL/PLResult", Method.PUT);
                    request.AddParameter("plJSON", json);
                    IRestResponse response = client.Execute(request);
                    */
                }
            }
            conn.Close();//关闭连接
            ShareAccessPL.mutex.ReleaseMutex();
        }
    }

    public class ReadAccessPL
    {
        private static PLManager plManager;
        private static string table = "PL_lisoutput";
        private static OleDbConnection conn;

        private static DataSet ds;
        private static string blank = string.Empty;
        private static PLManager.PL12 pl12;
        private static PLManager.PL12Result result;
        private static bool IsAllSend;//判断一个ID号内是否所有Item都已经发送

        public static event GlobalVariable.MessageHandler ReadAccessPLMessage;

        public ReadAccessPL(PLManager pm)
        {
            plManager = pm;

            string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
            string pathto = GlobalVariable.topDir.Parent.FullName;
            strConnection += "Data Source=" + @pathto + "\\PLDB.mdb";
            conn = new OleDbConnection(strConnection);
        }

        public static void ReadData(string selectname, string selectvalue)
        {
            ShareAccessPL.mutex.WaitOne();
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();
            }
            IsAllSend = true;
            pl12 = new PLManager.PL12();

            string strSelect = "select * from " + table + " where " + selectname + "='" + selectvalue + "'";
            using (OleDbDataAdapter oa = new OleDbDataAdapter(strSelect, conn))
            {
                ds = new DataSet();
                pl12.Result = new List<PLManager.PL12Result>();
                if (oa.Fill(ds, table) == 0)
                {
                    //Console.WriteLine("这个ID没有数据");
                    ds.Clear();
                    conn.Close();
                    ShareAccessPL.mutex.ReleaseMutex();
                    return;
                }
                foreach (DataRow dr in ds.Tables[table].Rows)
                {
                    pl12.ISSEND = dr["ISSEND"] == DBNull.Value ? false : (bool)dr["ISSEND"];
                    IsAllSend &= pl12.ISSEND;
                    if (pl12.ISSEND)
                    {
                        continue;//对于PL数据来说,只要看到ID号内一项数据已经发送，就已经代表全部发送
                    }
                    else
                    {
                        #region 解析数据库数据
                        pl12.SAMPLE_ID = dr["SAMPLE_ID"] == DBNull.Value ? blank : (string)dr["SAMPLE_ID"];
                        pl12.BARCODE = dr["BarCode"] == DBNull.Value ? blank : (string)dr["BarCode"];
                        pl12.TEST_TIME = dr["TEST_TIME"] == DBNull.Value ? DateTime.Now : (DateTime)dr["TEST_TIME"];
                        pl12.DEVEICE = dr["DEVICE"] == DBNull.Value ? blank : (string)dr["DEVICE"];
                        pl12.AAP = dr["AAP"] == DBNull.Value ? blank : (string)dr["AAP"];
                        pl12.SAMPLE_KIND = (string)dr["SAMPLE_KIND"] == "1" ? "检测结果" : blank;
                        pl12.TYPE = "血小板";

                        result = new PLManager.PL12Result();
                        result.ITEM = (string)dr["ITEM"];
                        result.FULL_NAME = dr["FULL_NAME"] == DBNull.Value ? blank : (string)dr["FULL_NAME"];
                        result.RESULT = dr["RESULT"] == DBNull.Value ? "-1" : (string)dr["RESULT"];
                        result.UNIT = dr["UNIT"] == DBNull.Value ? blank : (string)dr["UNIT"];
                        result.NORMAL_LOW = dr["NORMAL_LOW"] == DBNull.Value ? -1 : (double)dr["NORMAL_LOW"];
                        result.NORMAL_HIGH = dr["NORMAL_HIGH"] == DBNull.Value ? -1 : (double)dr["NORMAL_HIGH"];
                        result.INDICATE = dr["INDICATE"] == DBNull.Value ? blank : (string)dr["INDICATE"];
                        #endregion
                        pl12.Result.Add(result);
                    }
                }
                if (!IsAllSend)
                {
                    plManager.AddPL12(pl12);
                    plManager.PLSignal.Set();
                }
                ds.Clear();
                conn.Close();
            }
            ReadAccessPLMessage.Invoke(pl12.SAMPLE_ID + "数据库读取成功\r\n", "DEVICE");
            ShareAccessPL.mutex.ReleaseMutex();
        }
    }

    public class PLCancel//取消所有与血小板有关线程,但没有关闭串口
    {
        public static event GlobalVariable.MessageHandler PLCancellMessage;
        private PLCancel() { }
        public static void Cancell()
        {
            GlobalVariable.PLNum = false;
            if (GlobalVariable.PLCOM != null)
            {
                if (GlobalVariable.IsContainsKey(GlobalVariable.PLCOM + "+" + GlobalVariable.PLBUAD.ToString()))
                {
                    GlobalVariable.Remove(GlobalVariable.PLCOM + "+" + GlobalVariable.PLBUAD.ToString());
                }
            }
            GlobalVariable.ClearAllList = true;//清屏处理
            Comm.CommCancel.Cancel();
            ProcessPLs.ProcessPLsCancel.Cancel();
            WriteAccessPL.WriteAccessPLCancel.Cancel();
            PLCancellMessage.Invoke("已取消与血小板连接\r\n", "DEVICE");
        }
    }
}
