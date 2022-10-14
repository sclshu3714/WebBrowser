using JFBrowser.Common;
using CefSharp;
using CefSharp.Internals;
using CefSharp.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace JFBrowser.Controls
{
    public partial class JfBrowserControl : UserControl //: AicBaseControl
    {
        public ChromiumWebBrowser chromeBrowser { get; set; }
        public Func<string, Action<string>, bool> OnJavascriptCallAction { get; set; } = null;
        private IntPtr browserHandle;
        private ChromeWidgetMessageInterceptor messageInterceptor;
        private bool multiThreadedMessageLoopEnabled;
        //public IJavascriptCallback OnIJavascriptCallback { get; set; } = null;
        public bool IsLegacyBindingEnabled
        {
            get { return chromeBrowser == null ? false : chromeBrowser.JavascriptObjectRepository.Settings.LegacyBindingEnabled; }
            set { if (chromeBrowser != null) { chromeBrowser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = value; } }
        }

        public JfBrowserControl()
        {
            InitializeComponent();
            this.Load += AicBrowserControl_Load;
            //this.OnCallbackOperate += AicBrowserControl_OnCallbackOperate;
            //this.KeyDown += AicBrowserControl_KeyDown;
            
        }

        private void AicBrowserControl_Load(object sender, EventArgs e)
        {
            //System.IntPtr hWnd = this.Handle;
            //WmTouchDevice.MessageTouchDevice.RegisterTouchWindow(hWnd);
            //var timer = new System.Windows.Threading.DispatcherTimer(
            //   TimeSpan.FromMilliseconds(1000 / 60),
            //   System.Windows.Threading.DispatcherPriority.Render, 
            //   (s, e) => Cef.DoMessageLoopWork(), 
            //   Dispatcher);
            //timer.Start();
        }

        private void ChromeBrowser_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                ShowDevTools();
            }
        }

        internal void SetVisible(bool value)
        {
            toolStrip1.Visible = value;
            toolStrip2.Visible = value;
            statusLabel.Visible = value;
            outputLabel.Visible = value;
        }

        private void AicBrowserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1) {
                ShowDevTools();
            }
        }

        private string AicBrowserControl_OnCallbackOperate(string keyword, params object[] args)
        {
            JObject JResult = new JObject();
            switch (keyword)
            {
                case "SetResolveObject":
                    #region ResolveObject
                    string name = null;
                    object objectToBind = null;
                    bool isAsync = false;
                    BindingOptions options = null;
                    switch (args.Count())
                    {
                        case 2:
                            name = Convert.ToString(args[0]);
                            objectToBind = args[1];
                            SetResolveObject(name, objectToBind, isAsync, options);
                            JResult.Add("success", true);
                            break;
                        case 3:
                            name = Convert.ToString(args[0]);
                            objectToBind = args[1];
                            isAsync = Convert.ToBoolean(args[2]);
                            SetResolveObject(name, objectToBind, isAsync, options);
                            JResult.Add("success", true);
                            break;
                        case 4:
                            name = Convert.ToString(args[0]);
                            objectToBind = args[1];
                            isAsync = Convert.ToBoolean(args[2]);
                            options = args[3] as BindingOptions;
                            SetResolveObject(name, objectToBind, isAsync, options);
                            JResult.Add("success", true);
                            break;
                        default:
                            JResult.Add("success", false);
                            break;
                    }

                    #endregion
                    break;
                case "ExecuteScriptAsync":
                    if (args.Count() < 2)
                    {
                        JResult.Add("success", false);
                        break;
                    }
                    string methodName = Convert.ToString(args[0]);
                    object[] e_args = new object[] { args.ToList().Skip(1) };
                    ExecuteScriptAsync(methodName, e_args);
                    JResult.Add("success", true);
                    break;
                default:
                    JResult.Add("success", false);
                    break;
            }
            return JsonConvert.SerializeObject(JResult);
        }

        public JfBrowserControl(Action<string, int?> openNewTab,string url) : this()
        {
            InitializeChromium(openNewTab, url);
        }

        /// <summary>
        /// 初始化浏览器并启动
        /// </summary>
        /// <param name="url"></param>
        public void InitializeChromium(Action<string, int?> openNewTab, string url, IRequestContext requestContext = null)
        {
            //AicSystemHelper.FlushMemory();
            //var browser = new ChromiumWebBrowser(url, requestContext);
            //browser.RequestHandler = new DefaultRequestHandler();
            //Cef.UIThreadTaskFactory.StartNew(delegate
            //{
            //    var rc = browser.GetBrowser().GetHost().RequestContext;
            //    rc.GetAllPreferences(true);
            //    var dict = new Dictionary<string, object>();
            //    dict.Add("mode", "fixed_servers");
            //    dict.Add("server", "ipaddress:prot"); //此处替换成实际 ip地址：端口
            //    string error;
            //    bool success = rc.SetPreference("proxy", dict, out error);
            //    if (!success)
            //    {
            //        Console.WriteLine("something happen with the prerence set up" + error);
            //    }
            //});

            // Create a browser component
            chromeBrowser = new ChromiumWebBrowser(url, requestContext);
            chromeBrowser.Dock = DockStyle.Fill;
            chromeBrowser.KeyboardHandler = new DefautKeyboardHandler();
            //chromeBrowser.KeyDown += ChromeBrowser_KeyDown;
            //chromeBrowser.FrameLoadEnd += ChromeBrowser_FrameLoadEnd;
            chromeBrowser.IsBrowserInitializedChanged += OnIsBrowserInitializedChanged;
            chromeBrowser.LoadError += OnLoadError;
            chromeBrowser.LoadingStateChanged += OnBrowserLoadingStateChanged;
            chromeBrowser.ConsoleMessage += OnBrowserConsoleMessage;
            chromeBrowser.TitleChanged += OnBrowserTitleChanged;
            chromeBrowser.AddressChanged += OnBrowserAddressChanged;
            chromeBrowser.StatusMessage += OnBrowserStatusMessage;
            chromeBrowser.IsBrowserInitializedChanged += OnIsBrowserInitializedChanged;
            chromeBrowser.LoadError += OnLoadError;

            chromeBrowser.LifeSpanHandler = new JfLifeSpanHandler();
            chromeBrowser.MenuHandler = new JfMenuHandler();
            chromeBrowser.RequestHandler = new WinFormsRequestHandler(openNewTab);
            // Add it to the form and fill it to the form window.
            this.browserPanel.Controls.Add(chromeBrowser);

            BrowserSettings browserSettings = new BrowserSettings();
            browserSettings.FileAccessFromFileUrls = CefState.Enabled;
            browserSettings.UniversalAccessFromFileUrls = CefState.Enabled;
            //->2021.8.26
            browserSettings.ApplicationCache = CefState.Disabled;
            browserSettings.FileAccessFromFileUrls = CefState.Enabled;
            browserSettings.UniversalAccessFromFileUrls = CefState.Enabled;
            browserSettings.ImageLoading = CefState.Enabled;
            browserSettings.Javascript = CefState.Enabled;
            //<-2021.8.26
            chromeBrowser.BrowserSettings = browserSettings;
        }

        private void OnLoadError(object sender, LoadErrorEventArgs args)
        {
            //Don't display an error for external protocols that we allow the OS to
            //handle in OnProtocolExecution().
            if (args.ErrorCode == CefErrorCode.UnknownUrlScheme && args.Frame.Url.StartsWith("mailto"))
            {
                return;
            }
            //AicCefSettings.OnCallbackOperateClick?.Invoke(BarOperateActivation.Notice, $"页面加载失败 {args.ErrorCode };{args.ErrorText}", "加载出错", 3, 0);
        }

        private void OnIsBrowserInitializedChanged(object sender, EventArgs e)
        {
            //Get the underlying browser host wrapper
            var browserHost = chromeBrowser.GetBrowser().GetHost();
            var requestContext = browserHost.RequestContext;
            string errorMessage;
            // Browser must be initialized before getting/setting preferences
            var success = requestContext.SetPreference("enable_do_not_track", true, out errorMessage);
            if (!success)
            {
                this.InvokeOnUiThreadIfRequired(() => MessageBox.Show("无法设置首选项,跟踪错误消息： " + errorMessage));
            }

            //Example of disable spellchecking
            //success = requestContext.SetPreference("browser.enable_spellchecking", false, out errorMessage);

            var preferences = requestContext.GetAllPreferences(true);
            var doNotTrack = (bool)preferences["enable_do_not_track"];

            //Use this to check that settings preferences are working in your code
            //success = requestContext.SetPreference("webkit.webprefs.minimum_font_size", 24, out errorMessage);

            //If we're using CefSetting.MultiThreadedMessageLoop (the default) then to hook the message pump,
            // which running in a different thread we have to use a NativeWindow
            if (multiThreadedMessageLoopEnabled)
            {
                SetupMessageInterceptor();
            }
        }

        private void OnBrowserConsoleMessage(object sender, ConsoleMessageEventArgs args)
        {
            DisplayOutput(string.Format("Line: {0}, Source: {1}, Message: {2}", args.Line, args.Source, args.Message));
        }

        private void OnBrowserStatusMessage(object sender, StatusMessageEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => statusLabel.Text = args.Value);
        }

        private void OnBrowserLoadingStateChanged(object sender, LoadingStateChangedEventArgs args)
        {
            SetCanGoBack(args.CanGoBack);
            SetCanGoForward(args.CanGoForward);

            this.InvokeOnUiThreadIfRequired(() => SetIsLoading(args.IsLoading));
        }

        private void OnBrowserTitleChanged(object sender, TitleChangedEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => Parent.Text = args.Title);
        }

        private void OnBrowserAddressChanged(object sender, AddressChangedEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => urlTextBox.Text = args.Address);
        }
        public void LoadUrl(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                chromeBrowser.Load(url);
            }
            else
            {
                var searchUrl = "https://cn.bing.com/search?q=" + Uri.EscapeDataString(url);
                chromeBrowser.Load(searchUrl);
            }
        }
        public void Reload()
        {
            //AicSystemHelper.FlushMemory();
            if (chromeBrowser.IsBrowserInitialized)
                chromeBrowser?.Reload();
        }
        private void ChromeBrowser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            //设置滚动条宽度0
            e.Browser.MainFrame.ExecuteJavaScriptAsync("var style1 = document.createElement('style');style1.innerHTML = 'body::-webkit-scrollbar{width:1 !important;background:transparent}';document.head.appendChild(style1);");
        }

        public void SetLegacyBindingEnabled(bool IsEnabled = false) {
            chromeBrowser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = IsEnabled;
        }

        public void SetResolveObject(string name, object objectToBind, bool isAsync, BindingOptions options = null) {
            EventHandler<CefSharp.Event.JavascriptBindingEventArgs> TResolveObject = (object sender, CefSharp.Event.JavascriptBindingEventArgs e) => {
                if (e.ObjectName == JavascriptObjectRepository.LegacyObjects)
                {
                    if (!e.ObjectRepository.IsBound(name))
                        e.ObjectRepository.Register(name, objectToBind, isAsync: isAsync);
                }
            };
            chromeBrowser.JavascriptObjectRepository.ResolveObject -= TResolveObject;
            chromeBrowser.JavascriptObjectRepository.ResolveObject += TResolveObject;
        }

        /// <summary>
        /// 执行JS方法
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public void ExecuteScriptAsync(string methodName, params object[] args)
        {
            chromeBrowser?.ExecuteScriptAsync(methodName, args);
        }
        /// <summary>
        /// 执行JS方法
        /// </summary>
        /// <param name="script"></param>
        public void ExecuteScriptAsync(string script)
        {
            chromeBrowser?.ExecuteScriptAsync(script);
        }

        public Task<JavascriptResponse> EvaluateScriptAsync(string script, TimeSpan? timeout = null, bool useImmediatelyInvokedFuncExpression = false)
        {
            return chromeBrowser.EvaluateScriptAsync(script, timeout, useImmediatelyInvokedFuncExpression);
        }

        /// <summary>
        /// 启动调试工具
        /// </summary>
        public void ShowDevTools()
        {
            if (chromeBrowser.IsBrowserInitialized)
                chromeBrowser.ShowDevTools();
        }


        /// <summary>
        /// 窗体关闭时，记得停止浏览器
        /// </summary>
        public void Shutdown()
        {
            try
            {
                CefSharp.Cef.Shutdown();//释放资源
            }
            catch (Exception)
            {

                throw;
            }
        }


        public void Close() {
            chromeBrowser.CloseDevTools();//关闭调试
            chromeBrowser.JavascriptObjectRepository.UnRegisterAll();//解绑对象 高版本才有
            IBrowser browser = chromeBrowser?.GetBrowser();
            browser?.CloseBrowser(false);
            if (chromeBrowser != null && !chromeBrowser.IsDisposed)
                chromeBrowser.Dispose();
            //KillBrowserSubprocess();//杀死进程
            //AicSystemHelper.FlushMemory();
        }


        /// <summary>
        /// 清理CefSharp.BrowserSubprocess
        /// </summary>
        public static void KillBrowserSubprocess()
        {
            try
            {
                string SYSPath = System.AppDomain.CurrentDomain.BaseDirectory;
                Process[] processs = Process.GetProcessesByName("CefSharp.BrowserSubprocess");
                if (processs != null && processs.Length > 0)
                {
                    for (int i = 0; i < processs.Length; i++)
                    {
                        Process process = processs[i];

                        bool bKill = false;
                        if (process.MainModule != null)
                        {
                            string FileName = process.MainModule.FileName;
                            if (SYSPath.Contains(FileName) || FileName.Contains(SYSPath))
                            {
                                bKill = true;
                            }
                        }
                        if (bKill)
                        {
                            process.Kill();
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "清理CefSharp.BrowserSubprocess异常");
            }
        }


        private void SetCanGoBack(bool canGoBack)
        {
            this.InvokeOnUiThreadIfRequired(() => backButton.Enabled = canGoBack);
        }

        private void SetCanGoForward(bool canGoForward)
        {
            this.InvokeOnUiThreadIfRequired(() => forwardButton.Enabled = canGoForward);
        }

        private void SetIsLoading(bool isLoading)
        {
            goButton.Text = isLoading ?
                "Stop" :
                "Go";
            //goButton.Image = isLoading ?
            //    Properties.Resources.nav_plain_red :
            //    Properties.Resources.nav_plain_green;

            HandleToolStripLayout();
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);


        /// <summary>
        /// The ChromiumWebBrowserControl does not fire MouseEnter/Move/Leave events, because Chromium handles these.
        /// This method provides a demo of hooking the Chrome_RenderWidgetHostHWND handle to receive low level messages.
        /// You can likely hook other window messages using this technique, drag/drog etc
        /// </summary>
        private void SetupMessageInterceptor()
        {
            if (messageInterceptor != null)
            {
                messageInterceptor.ReleaseHandle();
                messageInterceptor = null;
            }

            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        IntPtr chromeWidgetHostHandle;
                        if (ChromeWidgetHandleFinder.TryFindHandle(browserHandle, out chromeWidgetHostHandle))
                        {
                            messageInterceptor = new ChromeWidgetMessageInterceptor((Control)chromeBrowser, chromeWidgetHostHandle, message =>
                            {
                                const int WM_MOUSEACTIVATE = 0x0021;
                                const int WM_NCLBUTTONDOWN = 0x00A1;
                                const int WM_DESTROY = 0x0002;

                                // Render process switch happened, need to find the new handle
                                if (message.Msg == WM_DESTROY)
                                {
                                    SetupMessageInterceptor();
                                    return;
                                }

                                if (message.Msg == WM_MOUSEACTIVATE)
                                {
                                    // The default processing of WM_MOUSEACTIVATE results in MA_NOACTIVATE,
                                    // and the subsequent mouse click is eaten by Chrome.
                                    // This means any .NET ToolStrip or ContextMenuStrip does not get closed.
                                    // By posting a WM_NCLBUTTONDOWN message to a harmless co-ordinate of the
                                    // top-level window, we rely on the ToolStripManager's message handling
                                    // to close any open dropdowns:
                                    // http://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/ToolStripManager.cs,1249
                                    var topLevelWindowHandle = message.WParam;
                                    PostMessage(topLevelWindowHandle, WM_NCLBUTTONDOWN, IntPtr.Zero, IntPtr.Zero);
                                }
                                //Forward mouse button down message to browser control
                                //else if(message.Msg == WM_LBUTTONDOWN)
                                //{
                                //    PostMessage(browserHandle, WM_LBUTTONDOWN, message.WParam, message.LParam);
                                //}

                                // The ChromiumWebBrowserControl does not fire MouseEnter/Move/Leave events, because Chromium handles these.
                                // However we can hook into Chromium's messaging window to receive the events.
                                //
                                //const int WM_MOUSEMOVE = 0x0200;
                                //const int WM_MOUSELEAVE = 0x02A3;
                                //
                                //switch (message.Msg) {
                                //    case WM_MOUSEMOVE:
                                //        Console.WriteLine("WM_MOUSEMOVE");
                                //        break;
                                //    case WM_MOUSELEAVE:
                                //        Console.WriteLine("WM_MOUSELEAVE");
                                //        break;
                                //}
                            });

                            break;
                        }
                        else
                        {
                            // Chrome hasn't yet set up its message-loop window.
                            await Task.Delay(10);
                        }
                    }
                }
                catch
                {
                    // Errors are likely to occur if browser is disposed, and no good way to check from another thread
                }
            });
        }

        private void DisplayOutput(string output)
        {
            outputLabel.InvokeOnUiThreadIfRequired(() => outputLabel.Text = output);
        }

        private void HandleToolStripLayout(object sender, LayoutEventArgs e)
        {
            HandleToolStripLayout();
        }

        private void HandleToolStripLayout()
        {
            var width = toolStrip1.Width;
            foreach (ToolStripItem item in toolStrip1.Items)
            {
                if (item != urlTextBox)
                {
                    width -= item.Width - item.Margin.Horizontal;
                }
            }
            urlTextBox.Width = Math.Max(0, width - urlTextBox.Margin.Horizontal - 18);
        }

        private void GoButtonClick(object sender, EventArgs e)
        {
            LoadUrl(urlTextBox.Text);
        }

        private void BackButtonClick(object sender, EventArgs e)
        {
            chromeBrowser.Back();
        }

        private void ForwardButtonClick(object sender, EventArgs e)
        {
            chromeBrowser.Forward();
        }

        private void UrlTextBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            LoadUrl(urlTextBox.Text);
        }

        public async void CopySourceToClipBoardAsync()
        {
            var htmlSource = await  chromeBrowser.GetSourceAsync();

            Clipboard.SetText(htmlSource);
            DisplayOutput("HTML Source copied to clipboard");
        }

        private void ToggleBottomToolStrip()
        {
            if (toolStrip2.Visible)
            {
                chromeBrowser.StopFinding(true);
                toolStrip2.Visible = false;
            }
            else
            {
                toolStrip2.Visible = true;
                findTextBox.Focus();
            }
        }

        private void FindNextButtonClick(object sender, EventArgs e)
        {
            Find(true);
        }

        private void FindPreviousButtonClick(object sender, EventArgs e)
        {
            Find(false);
        }

        private void Find(bool next)
        {
            if (!string.IsNullOrEmpty(findTextBox.Text))
            {
                chromeBrowser.Find(0, findTextBox.Text, next, false, false);
            }
        }

        private void FindTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            Find(true);
        }

        public void ShowFind()
        {
            ToggleBottomToolStrip();
        }

        private void FindCloseButtonClick(object sender, EventArgs e)
        {
            ToggleBottomToolStrip();
        }

        //Example of DevTools docked within the existing UserControl,
        //in this example it's hosted in a Panel with a SplitContainer
        public void ShowDevToolsDocked()
        {
            if (browserSplitContainer.Panel2Collapsed)
            {
                browserSplitContainer.Panel2Collapsed = false;
            }

            ////Find devToolsControl in Controls collection
            //DevToolsContainerControl devToolsControl = null;
            //devToolsControl = browserSplitContainer.Panel2.Controls.Find(nameof(devToolsControl), false).FirstOrDefault() as DevToolsContainerControl;

            //if (devToolsControl == null || devToolsControl.IsDisposed)
            //{
            //    devToolsControl = new DevToolsContainerControl()
            //    {
            //        Name = nameof(devToolsControl),
            //        Dock = DockStyle.Fill
            //    };

            //    EventHandler devToolsPanelDisposedHandler = null;
            //    devToolsPanelDisposedHandler = (s, e) =>
            //    {
            //        browserSplitContainer.Panel2.Controls.Remove(devToolsControl);
            //        browserSplitContainer.Panel2Collapsed = true;
            //        devToolsControl.Disposed -= devToolsPanelDisposedHandler;
            //    };

            //    //Subscribe for devToolsPanel dispose event
            //    devToolsControl.Disposed += devToolsPanelDisposedHandler;

            //    //Add new devToolsPanel instance to Controls collection
            //    browserSplitContainer.Panel2.Controls.Add(devToolsControl);
            //}

            //if (!devToolsControl.IsHandleCreated)
            //{
            //    //It's very important the handle for the control is created prior to calling
            //    //SetAsChild, if the handle hasn't been created then manually call CreateControl();
            //    //This code is not required for this example, it's left here for demo purposes.
            //    devToolsControl.CreateControl();
            //}

            ////Devtools will be a child of the DevToolsContainerControl
            ////DevToolsContainerControl is a simple custom Control that's only required
            ////when CefSettings.MultiThreadedMessageLoop = false so arrow/tab key presses
            ////are forwarded to DevTools correctly.
            //var rect = devToolsControl.ClientRectangle;
            //var windowInfo = new WindowInfo();
            //windowInfo.SetAsChild(devToolsControl.Handle, rect.Left, rect.Top, rect.Right, rect.Bottom);
            //Browser.GetBrowserHost().ShowDevTools(windowInfo);
        }

        public Task<bool> CheckIfDevToolsIsOpenAsync()
        {
            return Cef.UIThreadTaskFactory.StartNew(() =>
            {
                return chromeBrowser.GetBrowserHost().HasDevTools;
            });
        }
    }
}
