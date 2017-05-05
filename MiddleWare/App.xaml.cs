using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace MiddleWare
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }

    public class AppConfig
    {
        /// <summary>
        /// 在config文件中appSettings配置节增加一对键、值对
        /// </summary>
        /// <param name="newKey"></param>
        /// <param name="newValue"></param>
        public static void UpdateAppConfig(string newKey,string newValue)
        {
            bool isModified = false;
            foreach (string key in ConfigurationManager.AppSettings) 
            {
                if (key == newKey) 
                {
                    isModified = true;//存在key
                }
            }
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (isModified) 
            {
                //如果存在KEY 需要删除掉KEY
                config.AppSettings.Settings.Remove(newKey);//移除旧的key
            }
            config.AppSettings.Settings.Add(newKey, newValue);//增加新的kye
            config.Save(ConfigurationSaveMode.Modified);//保存
            ConfigurationManager.RefreshSection("appSettings");//刷新
        }
        /// <summary>
        /// 返回config文件中appSettings配置节的value项 
        /// </summary>
        /// <param name="strKey"></param>
        /// <returns></returns>
        public static string GetAppConfig(string strKey)
        {
            foreach (string key in ConfigurationManager.AppSettings) 
            {
                if (key == strKey) 
                {
                    return ConfigurationManager.AppSettings[strKey];
                }
            }
            return null;
        }
    }
}
