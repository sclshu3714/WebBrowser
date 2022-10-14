using JFBrowser.Controls;
using CefSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JFBrowser.Common
{
    public class ScriptCallbackMethod
    {
        public JfBrowserControl AicBrowser { get; set; } = null;
        public ScriptCallbackMethod() { }
        /// <summary>
        /// JS回调获取数据
        /// </summary>
        /// <param name="javascriptCallback"></param>
        public void scriptCallbackInfo(string callname, string jsonparam, IJavascriptCallback javascriptCallback = null)
        {
            string response = string.Empty;
            Task.Factory.StartNew(async () =>
            {
                switch (callname)
                {
                    default:
                        MessageBox.Show(callname);    
                        break;
                }
            });
        }
    }
}
