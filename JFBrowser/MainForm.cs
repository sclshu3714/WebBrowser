// Copyright © 2010 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp;
using CefSharp.Callback;
using JFBrowser.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JFBrowser
{
    public partial class MainForm : Form
    {
        private FormWindowState winState;
        private FormBorderStyle brdStyle;
        private bool topMost;
        private Rectangle bounds;
        private bool IsFullScreen = false;

        private const string DefaultUrlForAddedTabs = "https://cn.bing.com/";

        // Default to a small increment:
        private const double ZoomIncrement = 0.10;

        private bool multiThreadedMessageLoopEnabled;

        public MainForm(bool multiThreadedMessageLoopEnabled)
        {
            InitializeComponent();
            JfCefSettings.Initialize();
            var bitness = Environment.Is64BitProcess ? "x64" : "x86";
            Text = "CefSharp.WinForms.Example - " + bitness;
            WindowState = FormWindowState.Maximized;

            Load += BrowserFormLoad;

            //Only perform layout when control has completly finished resizing
            ResizeBegin += (s, e) => SuspendLayout();
            ResizeEnd += (s, e) => ResumeLayout(true);

            this.multiThreadedMessageLoopEnabled = multiThreadedMessageLoopEnabled;
        }
        /// <summary>
        /// 全屏或者非全屏
        /// </summary>
        internal void OnFullScreen()
        {
            if (FormBorderStyle == FormBorderStyle.None)
            {
                this.Invoke(new Action(() => {
                    //this.FormBorderStyle = FormBorderStyle.Sizable;
                    //this.menuStrip1.Visible = true;
                    //Controls.JfBrowserControl control = this.browserTabControl.SelectedTab.Controls[0] as Controls.JfBrowserControl;
                    //control.SetVisible(true);
                    //this.browserTabControl.TabsVisible = true;
                    LeaveFullScreen();
                }));
            }
            else if (this.FormBorderStyle != FormBorderStyle.None)
            {//进入全屏
                this.Invoke(new Action(() => {
                    //this.FormBorderStyle = FormBorderStyle.None;
                    //this.menuStrip1.Visible = false;
                    //Controls.JfBrowserControl control = this.browserTabControl.SelectedTab.Controls[0] as Controls.JfBrowserControl;
                    //control.SetVisible(false);
                    //this.browserTabControl.TabsVisible = false;
                    EnterFullScreen();
                }));
            }
        }
        /// <summary>
        /// Save the current Window state.
        /// </summary>
        private void Save(Form targetForm)
        {
            winState = targetForm.WindowState;
            brdStyle = targetForm.FormBorderStyle;
            topMost = targetForm.TopMost;
            bounds = targetForm.Bounds;
        }
        public void EnterFullScreen()
        {
            if (!IsFullScreen)
            {
                Save(this); // Save the original form state.
                this.menuStrip1.Visible = false;
                Controls.JfBrowserControl control = this.browserTabControl.SelectedTab.Controls[0] as Controls.JfBrowserControl;
                control.SetVisible(false);
                this.browserTabControl.TabsVisible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Normal;
                this.TopMost = true;
                this.Location = new Point(0, 0);
                this.Size = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                //targetForm.Bounds = Screen.GetBounds(targetForm);
                IsFullScreen = true;
            }
        }
        /// <summary>
        /// Leave the full screen mode and restore the original window state.
        /// </summary>
        public void LeaveFullScreen()
        {
            if (IsFullScreen)
            {
                this.menuStrip1.Visible = true;
                Controls.JfBrowserControl control = this.browserTabControl.SelectedTab.Controls[0] as Controls.JfBrowserControl;
                control.SetVisible(true);
                this.browserTabControl.TabsVisible = true;
                // Restore the original Window state.
                this.WindowState = winState;
                this.FormBorderStyle = brdStyle;
                this.TopMost = topMost;
                this.Bounds = bounds;
                IsFullScreen = false;
            }
        }
        public IContainer Components
        {
            get
            {
                if (components == null)
                {
                    components = new Container();
                }

                return components;
            }
        }

        private void BrowserFormLoad(object sender, EventArgs e)
        {
            AddTab(JfCefSettings.DefaultUrl);
        }

        /// <summary>
        /// Used to add a Popup browser as a Tab
        /// </summary>
        /// <param name="browserHostControl"></param>
        public void AddTab(Control browserHostControl, string url)
        {
            browserTabControl.SuspendLayout();

            var tabPage = new TabPage(url)
            {
                Dock = DockStyle.Fill
            };

            tabPage.Controls.Add(browserHostControl);

            browserTabControl.TabPages.Add(tabPage);

            //Make newly created tab active
            browserTabControl.SelectedTab = tabPage;

            browserTabControl.ResumeLayout(true);
        }

        private void AddTab(string url, int? insertIndex = null)
        {
            browserTabControl.SuspendLayout();

            //var browser = new BrowserTabUserControl(AddTab, url, multiThreadedMessageLoopEnabled)
            //{
            //    Dock = DockStyle.Fill,
            //};
            var browser = JfCefSettings.CreatBrowserControl(AddTab,url);
            browser.Dock = DockStyle.Fill;
            var tabPage = new TabPage(url)
            {
                Dock = DockStyle.Fill
            };

            //This call isn't required for the sample to work. 
            //It's sole purpose is to demonstrate that #553 has been resolved.
            //browser.CreateControl();

            tabPage.Controls.Add(browser);

            if (insertIndex == null)
            {
                browserTabControl.TabPages.Add(tabPage);
            }
            else
            {
                browserTabControl.TabPages.Insert(insertIndex.Value, tabPage);
            }

            //Make newly created tab active
            browserTabControl.SelectedTab = tabPage;

            browserTabControl.ResumeLayout(true);
        }

        private void ExitMenuItemClick(object sender, EventArgs e)
        {
            ExitApplication();
        }

        private void ExitApplication()
        {
            Close();
        }

        private void AboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            //new AboutBox().ShowDialog();
        }

        public void RemoveTab(IntPtr windowHandle)
        {
            var parentControl = FromChildHandle(windowHandle);
            if (!parentControl.IsDisposed)
            {
                if (parentControl.Parent is TabPage tabPage)
                {
                    browserTabControl.TabPages.Remove(tabPage);
                }
                else if (parentControl.Parent is Panel panel)
                {
                    var browserTabUserControl = (BrowserTabUserControl)panel.Parent;

                    var tab = (TabPage)browserTabUserControl.Parent;
                    browserTabControl.TabPages.Remove(tab);
                }
            }
        }

        private void FindMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.ShowFind();
            }
        }

        private void CopySourceToClipBoardAsyncClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.CopySourceToClipBoardAsync();
            }
        }

        private BrowserTabUserControl GetCurrentTabControl()
        {
            if (browserTabControl.SelectedIndex == -1)
            {
                return null;
            }

            var tabPage = browserTabControl.Controls[browserTabControl.SelectedIndex];
            var control = tabPage.Controls[0] as BrowserTabUserControl;

            return control;
        }

        private void NewTabToolStripMenuItemClick(object sender, EventArgs e)
        {
            AddTab(DefaultUrlForAddedTabs);
        }

        private void CloseTabToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (browserTabControl.TabPages.Count == 0 || browserTabControl.SelectedIndex < 0)
            {
                return;
            }

            var currentIndex = browserTabControl.SelectedIndex;

            var tabPage = browserTabControl.TabPages[currentIndex];

            var control = GetCurrentTabControl();
            if (control != null && !control.IsDisposed)
            {
                control.Dispose();
            }

            browserTabControl.TabPages.Remove(tabPage);

            tabPage.Dispose();

            browserTabControl.SelectedIndex = currentIndex - 1;

            if (browserTabControl.TabPages.Count == 0)
            {
                ExitApplication();
            }
        }

        private void UndoMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.Browser.Undo();
            }
        }

        private void RedoMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.Browser.Redo();
            }
        }

        private void CutMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.Browser.Cut();
            }
        }

        private void CopyMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.Browser.Copy();
            }
        }

        private void PasteMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.Browser.Paste();
            }
        }

        private void DeleteMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.Browser.Delete();
            }
        }

        private void SelectAllMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.Browser.SelectAll();
            }
        }

        private void PrintToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.Browser.Print();
            }
        }

        private async void ShowDevToolsMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var isDevToolsOpen = await control.CheckIfDevToolsIsOpenAsync();
                if (!isDevToolsOpen)
                {
                    control.Browser.ShowDevTools();
                }
            }
        }

        private async void ShowDevToolsDockedMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var isDevToolsOpen = await control.CheckIfDevToolsIsOpenAsync();
                if (!isDevToolsOpen)
                {
                    if (control.Browser.LifeSpanHandler != null)
                    {
                        control.ShowDevToolsDocked();
                    }
                }
            }
        }

        private async void CloseDevToolsMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                //Check if DevTools is open before closing, this isn't strictly required
                //If DevTools isn't open and you call CloseDevTools it's a No-Op, so prefectly
                //safe to call without checking
                var isDevToolsOpen = await control.CheckIfDevToolsIsOpenAsync();
                if (isDevToolsOpen)
                {
                    control.Browser.CloseDevTools();
                }
            }
        }

        private void ZoomInToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var task = control.Browser.GetZoomLevelAsync();

                task.ContinueWith(previous =>
                {
                    if (previous.Status == TaskStatus.RanToCompletion)
                    {
                        var currentLevel = previous.Result;
                        control.Browser.SetZoomLevel(currentLevel + ZoomIncrement);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected failure of calling CEF->GetZoomLevelAsync", previous.Exception);
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        private void ZoomOutToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var task = control.Browser.GetZoomLevelAsync();
                task.ContinueWith(previous =>
                {
                    if (previous.Status == TaskStatus.RanToCompletion)
                    {
                        var currentLevel = previous.Result;
                        control.Browser.SetZoomLevel(currentLevel - ZoomIncrement);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected failure of calling CEF->GetZoomLevelAsync", previous.Exception);
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        private void CurrentZoomLevelToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var task = control.Browser.GetZoomLevelAsync();
                task.ContinueWith(previous =>
                {
                    if (previous.Status == TaskStatus.RanToCompletion)
                    {
                        var currentLevel = previous.Result;
                        MessageBox.Show("Current ZoomLevel: " + currentLevel.ToString());
                    }
                    else
                    {
                        MessageBox.Show("Unexpected failure of calling CEF->GetZoomLevelAsync: " + previous.Exception.ToString());
                    }
                }, TaskContinuationOptions.HideScheduler);
            }
        }

        private void DoesActiveElementAcceptTextInputToolStripMenuItemClick(object sender, EventArgs e)
        {
            //var control = GetCurrentTabControl();
            //if (control != null)
            //{
            //    var frame = control.Browser.GetFocusedFrame();

            //    //Execute extension method
            //    frame.ActiveElementAcceptsTextInput().ContinueWith(task =>
            //    {
            //        string message;
            //        var icon = MessageBoxIcon.Information;
            //        if (task.Exception == null)
            //        {
            //            var isText = task.Result;
            //            message = string.Format("The active element is{0}a text entry element.", isText ? " " : " not ");
            //        }
            //        else
            //        {
            //            message = string.Format("Script evaluation failed. {0}", task.Exception.Message);
            //            icon = MessageBoxIcon.Error;
            //        }

            //        MessageBox.Show(message, "Does active element accept text input", MessageBoxButtons.OK, icon);
            //    });
            //}
        }

        private void DoesElementWithIdExistToolStripMenuItemClick(object sender, EventArgs e)
        {
            //// This is the main thread, it's safe to create and manipulate form
            //// UI controls.
            //var dialog = new InputBox
            //{
            //    Instructions = "Enter an element ID to find.",
            //    Title = "Find an element with an ID"
            //};

            //dialog.OnEvaluate += (senderDlg, eDlg) =>
            //{
            //    // This is also the main thread.
            //    var control = GetCurrentTabControl();
            //    if (control != null)
            //    {
            //        var frame = control.Browser.GetFocusedFrame();

            //        //Execute extension method
            //        frame.ElementWithIdExists(dialog.Value).ContinueWith(task =>
            //        {
            //            // Now we're not on the main thread, perhaps the
            //            // Cef UI thread. It's not safe to work with
            //            // form UI controls or to block this thread.
            //            // Queue up a delegate to be executed on the
            //            // main thread.
            //            BeginInvoke(new Action(() =>
            //            {
            //                string message;
            //                if (task.Exception == null)
            //                {
            //                    message = task.Result.ToString();
            //                }
            //                else
            //                {
            //                    message = string.Format("Script evaluation failed. {0}", task.Exception.Message);
            //                }

            //                dialog.Result = message;
            //            }));
            //        });
            //    }
            //};

            //dialog.Show(this);
        }

        private void GoToDemoPageToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.Browser.Load("custom://cefsharp/ScriptedMethodsTest.html");
            }
        }

        private void InjectJavascriptCodeToolStripMenuItemClick(object sender, EventArgs e)
        {
            //var control = GetCurrentTabControl();
            //if (control != null)
            //{
            //    var frame = control.Browser.GetFocusedFrame();

            //    //Execute extension method
            //    frame.ListenForEvent("test-button", "click");
            //}
        }

        private async void PrintToPdfToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var dialog = new SaveFileDialog
                {
                    DefaultExt = ".pdf",
                    Filter = "Pdf documents (.pdf)|*.pdf"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var success = await control.Browser.PrintToPdfAsync(dialog.FileName, new PdfPrintSettings
                    {
                        MarginType = CefPdfPrintMarginType.Custom,
                        MarginBottom = 10,
                        MarginTop = 0,
                        MarginLeft = 20,
                        MarginRight = 10
                    });

                    if (success)
                    {
                        MessageBox.Show("Pdf was saved to " + dialog.FileName);
                    }
                    else
                    {
                        MessageBox.Show("Unable to save Pdf, check you have write permissions to " + dialog.FileName);
                    }

                }

            }
        }

        private void OpenDataUrlToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                const string html = "<html><head><title>Test</title></head><body><h1>Html Encoded in URL!</h1></body></html>";
                control.Browser.LoadHtml(html, false);
            }
        }

        private void OpenHttpBinOrgToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.Browser.Load("https://httpbin.org/");
            }
        }

        private void RunFileDialogToolStripMenuItemClick(object sender, EventArgs e)
        {
            //var control = GetCurrentTabControl();
            //if (control != null)
            //{
            //    control.Browser.GetBrowserHost().RunFileDialog(CefFileDialogMode.Open, "Open", null, new List<string> { "*.*" }, 0, new IRunFileDialogCallback());
            //}
        }

        private void LoadExtensionsToolStripMenuItemClick(object sender, EventArgs e)
        {
//            var control = GetCurrentTabControl();
//            if (control != null)
//            {
//                //The sample extension only works for http(s) schemes
//                if (control.Browser.Address.StartsWith("http"))
//                {
//                    var requestContext = control.Browser.GetBrowserHost().RequestContext;

//                    const string cefSharpExampleResourcesFolder =
//#if !NETCOREAPP
//                        @"..\..\..\..\CefSharp.Example\Extensions";
//#else
//                        @"..\..\..\..\..\CefSharp.Example\Resources";
//#endif

//                    var dir = Path.Combine(AppContext.BaseDirectory, cefSharpExampleResourcesFolder);
//                    dir = Path.GetFullPath(dir);
//                    if (!Directory.Exists(dir))
//                    {
//                        throw new DirectoryNotFoundException("Unable to locate example extensions folder - " + dir);
//                    }

//                    var extensionHandler = new ExtensionHandler
//                    {
//                        LoadExtensionPopup = (url) =>
//                        {
//                            BeginInvoke(new Action(() =>
//                            {
//                                var extensionForm = new Form();

//                                var extensionBrowser = new ChromiumWebBrowser(url);
//                                //extensionBrowser.IsBrowserInitializedChanged += (s, args) =>
//                                //{
//                                //    extensionBrowser.ShowDevTools();
//                                //};

//                                extensionForm.Controls.Add(extensionBrowser);

//                                extensionForm.Show(this);
//                            }));
//                        },
//                        GetActiveBrowser = (extension, isIncognito) =>
//                        {
//                            //Return the active browser for which the extension will act upon
//                            return control.Browser.GetBrowser();
//                        }
//                    };

//                    requestContext.LoadExtensionsFromDirectory(dir, extensionHandler);
//                }
//                else
//                {
//                    MessageBox.Show("The sample extension only works with http(s) schemes, please load a different website and try again", "Unable to load Extension");
//                }
//            }
        }

        private void JavascriptBindingStressTestToolStripMenuItemClick(object sender, EventArgs e)
        {
            //var control = GetCurrentTabControl();
            //if (control != null)
            //{
            //    control.Browser.Load(CefExample.BindingTestUrl);
            //    control.Browser.LoadingStateChanged += (o, args) =>
            //    {
            //        if (args.IsLoading == false)
            //        {
            //            Task.Delay(10000).ContinueWith(t =>
            //            {
            //                if (control.Browser != null)
            //                {
            //                    control.Browser.Reload();
            //                }
            //            });
            //        }
            //    };
            //}
        }
    }
}
