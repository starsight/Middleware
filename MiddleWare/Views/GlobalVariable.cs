using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiddleWare.Views
{
    /// <summary>
    /// 全局变量  连接仪器的哈希值
    /// </summary>
    public class GlobalVariable
    {
        public delegate void MessageHandler(string message, string name);

        private static Dictionary<string, string> dic = new Dictionary<string, string>();
        private GlobalVariable()
        { }
        public static bool ClearAllList = false;//用于清除监控界面下拉菜单
        public static bool IsContainsKey(string akey)
        {
            return dic.ContainsKey(akey);
        }
        public static List<string> GetAllValue()
        {
            List<string> list = new List<string>();
            if (LEN > 0)
            {
                foreach (string akey in dic.Keys)
                {
                    list.Add(akey);
                }
            }
            return list;
        }
        public static string GetValue(string akey)
        {
            string avlaue = dic[akey];
            return avlaue;

        }
        public static void AddValue(string akey, string avalue)
        {
            dic.Add(akey, avalue);
        }
        public static void Remove(string akey)
        {
            if (LEN > 0)
            {
                dic.Remove(akey);
            }
        }
        public static int LEN
        {
            get
            {
                return dic.Count;
            }
        }

        public static bool IsOneWay { get; set; }//LIS是否为单向模式

        public static bool IsHL7Run;//HL7运行
        public static bool IsASTMRun;//ASTM运行
        public static bool IsASTMCom;//ASTM通过串口传输
        public static bool IsASTMNet;//ASTM通过网口传输

        public static bool DSNum = false;//确保只有一个DS连接
        public static bool PLNum = false;//确保只有一个PL连接
        public static bool IsDSRepeat = false;//判断是否之前连接过生化仪
        public static bool IsPLRepeat = false;//判断是否之前连接过血小板

        public static string DSDEVICEADDRESS;//DS仪器数据库地址
        public static string PLCOM;//PL串口号
        public static int PLBUAD;//PL串口波特率

        public static int DSDEVICE = 2;//1:DS400,0:DS800,其他值都不是
        public static int PLDEVICE = 2;//1:Pl16,0:PL12,其他值都不是  by wenjie 17-07-03

        public static bool SocketCode;//Socket通信的编码方式：true代表ASCII,false代表UTF8
        public static bool ComCode;//Com通信的编码方式：true代表ASCII,false代表UTF8

        /*语言环境 为int类型,便于以后添加多种语言 */
        public static int Language;//0代表简体中文 1代表英语
        public static System.IO.DirectoryInfo topDir = System.IO.Directory.GetParent(System.Environment.CurrentDirectory);

        /*MiddlewareWeb*/
        public static String BaseUrl = "http://localhost:8080/MiddlewareWeb";

        /*mini window*/
        public static bool isMiniMode = false;

    }

}
