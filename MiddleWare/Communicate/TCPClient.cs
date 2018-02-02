using log4net;
using MiddleWare.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiddleWare.Communicate
{
    class TCPClient
    {
        //服务器IP
        private static String SERVER_IP = "127.0.0.1";

        //服务器端口号
        private static int SERVER_PORT = 2001;

        private ILog log = log4net.LogManager.GetLogger("TCPClient");

        private Socket clientSocket;

        private bool isSocketRun = false;

        private byte[] recyBytes = new byte[1024];

        private CancellationTokenSource socketCancel = new CancellationTokenSource();

        private IPAddress ip = IPAddress.Parse(SERVER_IP);

        private HL7Manager hl7Manager;

        public TCPClient(HL7Manager hl7Manager)
        {
            this.hl7Manager = hl7Manager;
        }

        public void start()
        {

            try
            {
                //创建连接服务器的Socket
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                clientSocket.Connect(new IPEndPoint(ip, SERVER_PORT));
                clientSocket.ReceiveTimeout = -1;//阻塞时的,如果接收不到一直在阻塞
                clientSocket.SendTimeout = 500;

                log.Info("TCP连接成功");
                isSocketRun = true;

                //发送client名称
                clientSocket.Send(Encoding.ASCII.GetBytes(GlobalVariable.DSDeviceID));

            }
            catch (Exception e)
            {
                isSocketRun = false;
                //连接出错，尝试重新连接
                for (int i = 0; i < 3; i++)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        clientSocket.Connect(new IPEndPoint(ip, SERVER_PORT));
                        clientSocket.ReceiveTimeout = -1;
                        clientSocket.SendTimeout = 500;
                    }
                    catch
                    {
                        continue;
                    }
                    if (clientSocket.Connected)
                    {
                        log.Info("TCP重新连接成功");
                        //发送client名称
                        clientSocket.Send(Encoding.ASCII.GetBytes(GlobalVariable.DSDeviceID));
                        isSocketRun = true;
                        break;
                    }
                }
                if (!isSocketRun)
                {
                    //重连失败
                    socketCancel.Cancel();
                }

            }
            if (isSocketRun)
            {
                Task.Factory.StartNew(run, socketCancel.Token);
            }
        }

        private void run()
        {
            int receiveNumber;
            String receiveStr;
            while (!socketCancel.IsCancellationRequested)
            {
                try
                {
                    receiveNumber = clientSocket.Receive(recyBytes);
                    receiveStr = Encoding.UTF8.GetString(recyBytes, 0, receiveNumber);
                    //接收到数据
                    if (receiveStr.Length > 10 && receiveStr.Substring(0, 3) == "MSH")
                    {
                        //传回来为标准信息
                        hl7Manager.AddHL7ApplySample(receiveStr);//扔给队列交给线程处理
                        hl7Manager.HL7ApplySampleSignal.Set();//唤醒线程
                    }
                }
                catch
                {
                    isSocketRun = false;
                    //连接出错，尝试重新连接
                    for (int i = 0; i < 3; i++)
                    {
                        Thread.Sleep(1000);
                        try
                        {
                            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                            clientSocket.Connect(new IPEndPoint(ip, SERVER_PORT));
                            clientSocket.ReceiveTimeout = -1;
                            clientSocket.SendTimeout = 500;
                        }
                        catch
                        {
                            continue;
                        }
                        if (clientSocket.Connected)
                        {
                            Console.WriteLine("重新连接成功");
                            //发送client名称
                            clientSocket.Send(Encoding.ASCII.GetBytes(GlobalVariable.DSDeviceID));
                            isSocketRun = true;
                            break;
                        }
                    }
                    if (!isSocketRun)
                    {
                        socketCancel.Cancel();
                        break;
                    }
                }
            }
        }
    }
}
