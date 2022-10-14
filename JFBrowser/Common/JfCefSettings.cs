using JFBrowser.Controls;
using CefSharp;
using CefSharp.Internals;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JFBrowser.Common
{
    //public delegate void JavascriptObjectRegister(string name, object objectToBind, bool isAsync, BindingOptions options = null);
    //public delegate bool JavascriptObjectUnRegister(string name);
    //public delegate bool JavascriptObjectUnRegisterAll();
    public class JfCefSettings
    {
        internal static string DefaultUrl => "https://cn.bing.com/";

        public static bool IsInitialize { get; set; } = false;
        private static JfBrowserControl AicBrowser { get; set; } = null;

        private static ScriptCallbackMethod CallbackMethod { get; set; } = null;
        /// <summary>
        /// 初始化
        /// </summary>
        public static bool Initialize()
        {
            string archSpecificPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "3dparty", "CefSharp", Environment.Is64BitProcess ? "x64" : "x86", "CefSharp.BrowserSubprocess.exe");
            CefSettings settings = new CefSettings
            {
                Locale = "zh-CN",
                AcceptLanguageList = "zh-CN",
                MultiThreadedMessageLoop = true,
                BrowserSubprocessPath = archSpecificPath
            };
            // 设置是否使用GPU
            //settings.CefCommandLineArgs.Add("disable-gpu", "1");
            //settings.CefCommandLineArgs.Add("headless", "1");
            // 设置是否使用代理服务
            //settings.CefCommandLineArgs.Add("js-flags", "max_old_space_size=6096");
            //settings.CefCommandLineArgs.Add("no-proxy-server", "1");
            //settings.CefCommandLineArgs.Add("autoplay-policy", "0");
            //settings.CefCommandLineArgs.Add("enable-media-stream", "1");
            //settings.CefCommandLineArgs.Add("allow-running-insecure-content", "1");
            //settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream", "1");
            //settings.CefCommandLineArgs.Add("enable-speech-input", "1");
            //settings.CefCommandLineArgs.Add("enable-usermedia-screen-capture", "1");
            //settings.CefCommandLineArgs.Add("debug-plugin-loading", "1");
            //settings.CefCommandLineArgs.Add("allow-outdated-plugins", "1");
            //settings.CefCommandLineArgs.Add("always-authorize-plugins", "1");
            //settings.CefCommandLineArgs.Add("disable-web-security", "1");
            //settings.CefCommandLineArgs.Add("enable-npapi", "1");

            settings.CefCommandLineArgs.Add("disable-gpu", "1");
            //settings.CefCommandLineArgs.Add("headless", "1");
            // 设置是否使用代理服务
            settings.CefCommandLineArgs.Add("no-proxy-server", "1");
            settings.CefCommandLineArgs.Add("autoplay-policy", "1");
            settings.CefCommandLineArgs.Add("enable-media-stream", "1");
            settings.CefCommandLineArgs.Add("allow-running-insecure-content", "1");
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream", "1");
            settings.CefCommandLineArgs.Add("enable-speech-input", "1");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capture", "1");
            settings.CefCommandLineArgs.Add("debug-plugin-loading", "1");
            settings.CefCommandLineArgs.Add("allow-outdated-plugins", "1");
            settings.CefCommandLineArgs.Add("always-authorize-plugins", "1");
            settings.CefCommandLineArgs.Add("enable-npapi", "1");
            settings.CefCommandLineArgs.Add("touch-events", "1");

            settings.CefCommandLineArgs.Add("--ignore-urlfetcher-cert-requests", "1");
            settings.CefCommandLineArgs.Add("--ignore-certificate-errors", "1");
            //禁止启用同源策略安全限制，禁止后不会出现跨域问题
            settings.CefCommandLineArgs.Add("--disable-web-security", "1"); 
            string theRootCachePath = $@"{ AppDomain.CurrentDomain.BaseDirectory }{"cache"}";
            string theCachePath = $@"{ AppDomain.CurrentDomain.BaseDirectory  }{"cache"}\{"google"}";
            if (!Directory.Exists(theCachePath)) {
                Directory.CreateDirectory(theCachePath);
            }
            settings.JavascriptFlags = "--max_old_space_size=6096";
            settings.RootCachePath = theRootCachePath;
            settings.CachePath = theCachePath;
            settings.IgnoreCertificateErrors = true;
            settings.LogSeverity = LogSeverity.Error;
            //settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";
            settings.PersistSessionCookies = false;
            try
            {
                IsInitialize = Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
            }
            catch (Exception err)
            {
                //AicSystemHelper.WriteSystemLog(AicSystemHelper.GetExceptionMsg(err,err.ToString()), "ERROR");
                //AicGlobalSetting.OnCallbackOperateClick(jf.Aic.Core.Enums.BarOperateActivation.Notice, "初始化浏览器失败", "温馨提示", 3, 0);
            }
            //安装证书
            //AicRequestAddress.InstallStore();
            return IsInitialize;
        }

        /// <summary>
        /// 创建浏览器嵌入控件
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static JfBrowserControl CreatBrowserControl(Action<string, int?> openNewTab, string url)
        {
            if (!IsInitialize && !(IsInitialize = Initialize()))
                return null;
            AicBrowser = new JfBrowserControl(openNewTab,url);
            AicBrowser.Dock = DockStyle.Fill;
            //if (IsRegisterJsObject) {
            //    if (objectToBind is ScriptCallbackMethod method)
            //    {
            //        method.AicBrowser = AicBrowser;
            //        CallbackMethod = method;
            //    }
            //    AicBrowser.OnJavascriptCallAction = OnJavascriptCallAction;
            //    RegisterJsObject(name, objectToBind, isAsync,CefSharp.BindingOptions.DefaultBinder);
            //}
            return AicBrowser;
        }
        /// <summary>
        /// 加载URL
        /// </summary>
        /// <param name="url"></param>
        public static void LoadUrl(string url)
        {
            //AicBrowser?.Close();
            AicBrowser?.LoadUrl(url);
        }
        public static void RegisterJsObject(string name, object objectToBind, bool isAsync)
        {
            if (AicBrowser == null)
                return;
            AicBrowser.SetLegacyBindingEnabled(true);
            AicBrowser.SetResolveObject(name, objectToBind, isAsync, CefSharp.BindingOptions.DefaultBinder);
        }
        /// <summary>
        /// 启动JS回调
        /// </summary>
        /// <param name="name">Brower</param>
        /// <param name="objectToBind">new ScriptCallbackMethod()</param>
        /// <param name="isAsync"></param>
        /// <param name="options"></param>
        /// <example>
        /// e.ObjectRepository.Register("Brower", new ScriptCallbackMethod(), isAsync: true);
        /// 
        /// 在HTML页面上必须加上这句，其中 bound 是在C#代码里注册对应的name参数。
        /// CefSharp.BindObjectAsync('Brower').
        /// then(function (result) {
        /// Brower.SetAutoResetEvent('ID', 'text');
        /// });
        /// </example>
        public static void RegisterJsObject(string name, object objectToBind, bool isAsync, BindingOptions options)
        {
            if (AicBrowser == null)
                return;
            AicBrowser.SetLegacyBindingEnabled(true);
            AicBrowser.SetResolveObject(name, objectToBind, isAsync, options);
        }

        /// <summary>
        /// 执行JS方法
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <example>
        /// 带参数的传递方式
        /// AicBrowser.ExecuteScriptAsync("DoSomething('left','" + btnShowInfo.Text + "')", new object[] { });
        /// 
        /// </example>
        public static void ExecuteScriptAsync(string methodName, params object[] args)
        {
            AicBrowser.ExecuteScriptAsync(methodName, args);
        }

        /// <summary>
        /// 启动调试工具
        /// </summary>
        public static void ShowDevTools()
        {
            AicBrowser.ShowDevTools();
        }

        public static void Close() {
            AicBrowser.Close();
        }

        /// <summary>
        /// 窗体关闭时，记得停止浏览器
        /// </summary>
        public static void Shutdown()
        {
            AicBrowser.Shutdown();
        }
    }
}
