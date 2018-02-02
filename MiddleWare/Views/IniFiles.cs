using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MiddleWare.Views
{
    /// <summary>
    /// 读取ini文件
    /// </summary>
    class IniFiles
    {
        private string fileName = string.Empty;

        [DllImport("kernel32")]
        private static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, byte[] retVal, int size, string filePath);

        public IniFiles(string fileName)
        {
            //判断文件是否存在
            try
            {
                FileInfo fileInfo = new FileInfo(fileName);
                if (!fileInfo.Exists)
                {
                    //如果文件不存在，则不进行后续操作
                    this.fileName = string.Empty;
                    return;
                }
                else
                {
                    //如果文件存在
                    this.fileName = fileInfo.FullName;
                }
            }catch(ArgumentException e)
            {
                this.fileName = string.Empty;
            }
        }

        public string ReadString(string Section,string Ident,string Default)
        {
            if(!IsRead())
            {
                return string.Empty;
            }
            Byte[] Buffer = new Byte[65535];
            int bufLen = GetPrivateProfileString(Section, Ident, Default, Buffer, Buffer.GetUpperBound(0), this.fileName);
            //必须设定0（系统默认的代码页）的编码方式，否则无法支持中文  
            string s = Encoding.GetEncoding(0).GetString(Buffer);
            s = s.Substring(0, bufLen);
            return s.Trim();
        }

        public bool IsRead()
        {
            return !string.IsNullOrEmpty(this.fileName);
        }
    }
}
