// Copyright (C) Microsoft Corporation. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace WebBrowser
{
    public partial class MainForm : Form
    {
        private FormWindowState winState;
        private FormBorderStyle brdStyle;
        private bool topMost;
        private Rectangle bounds;
        private bool IsFullScreen = false;
        public MainForm()
        {
            InitializeComponent();
            AttachControlEventHandlers(this.webView2Control);
            //HandleResize();
        }

        private void UpdateTitleWithEvent(string message)
        {
            string currentDocumentTitle = this.webView2Control?.CoreWebView2?.DocumentTitle ?? "Uninitialized";
            this.Text = currentDocumentTitle + " (" + message + ")";
        }

        #region Event Handlers
        private void WebView2Control_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            UpdateTitleWithEvent("NavigationStarting");
            //String uri = e.Uri;
            //if (!uri.StartsWith("https://"))
            //{
            //    e.Cancel = true;
            //}
        }

        private void WebView2Control_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            UpdateTitleWithEvent("NavigationCompleted");
        }

        private void WebView2Control_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            txtUrl.Text = webView2Control.Source.AbsoluteUri;
        }

        private void WebView2Control_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                MessageBox.Show($"WebView2 creation failed with exception = {e.InitializationException}");
                UpdateTitleWithEvent("CoreWebView2InitializationCompleted failed");
                return;
            }

            this.webView2Control.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            this.webView2Control.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
            this.webView2Control.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            this.webView2Control.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.Image);
            this.webView2Control.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            UpdateTitleWithEvent("CoreWebView2InitializationCompleted succeeded");
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.NewWindow = (CoreWebView2)sender;
        }

        void AttachControlEventHandlers(Microsoft.Web.WebView2.WinForms.WebView2 control) {
            control.CoreWebView2InitializationCompleted += WebView2Control_CoreWebView2InitializationCompleted;
            control.NavigationStarting += WebView2Control_NavigationStarting;
            control.NavigationCompleted += WebView2Control_NavigationCompleted;
            control.SourceChanged += WebView2Control_SourceChanged;
            control.KeyDown += WebView2Control_KeyDown;
            control.KeyUp += WebView2Control_KeyUp;
        }

        private void WebView2Control_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateTitleWithEvent($"AcceleratorKeyUp key={e.KeyCode}");
            if (e.KeyCode == Keys.F11) {
                if (FormBorderStyle == FormBorderStyle.None)
                {
                    this.Invoke(new Action(() => {
                        LeaveFullScreen();
                    }));
                }
                else if (this.FormBorderStyle != FormBorderStyle.None)
                {//进入全屏
                    this.Invoke(new Action(() => {
                        EnterFullScreen();
                    }));
                }
            }
            if (!this.acceleratorKeysEnabledToolStripMenuItem.Checked)
                e.Handled = true;
        }

        private void WebView2Control_KeyDown(object sender, KeyEventArgs e)
        {
            UpdateTitleWithEvent($"AcceleratorKeyDown key={e.KeyCode}");
            if (!this.acceleratorKeysEnabledToolStripMenuItem.Checked)
                e.Handled = true;
        }

        private void CoreWebView2_HistoryChanged(object sender, object e)
        {
            // No explicit check for webView2Control initialization because the events can only start
            // firing after the CoreWebView2 and its events exist for us to subscribe.
            btnBack.Enabled = webView2Control.CoreWebView2.CanGoBack;
            btnForward.Enabled = webView2Control.CoreWebView2.CanGoForward;
            UpdateTitleWithEvent("HistoryChanged");
        }

        private void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            this.txtUrl.Text = this.webView2Control.Source.AbsoluteUri;
            UpdateTitleWithEvent("SourceChanged");
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            this.Text = this.webView2Control.CoreWebView2.DocumentTitle;
            UpdateTitleWithEvent("DocumentTitleChanged");
        }
        #endregion

        #region UI event handlers
        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            webView2Control.Reload();
        }

        private void BtnGo_Click(object sender, EventArgs e)
        {
            var rawUrl = txtUrl.Text;
            Uri uri = null;

            if (Uri.IsWellFormedUriString(rawUrl, UriKind.Absolute))
            {
                uri = new Uri(rawUrl);
            }
            else if (!rawUrl.Contains(" ") && rawUrl.Contains("."))
            {
                // An invalid URI contains a dot and no spaces, try tacking http:// on the front.
                uri = new Uri("http://" + rawUrl);
            }
            else
            {
                // Otherwise treat it as a web search.
                uri = new Uri("https://bing.com/search?q=" + 
                    String.Join("+", Uri.EscapeDataString(rawUrl).Split(new string[] { "%20" }, StringSplitOptions.RemoveEmptyEntries)));
            }

            webView2Control.Source = uri;
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            webView2Control.GoBack();
        }

        private void btnEvents_Click(object sender, EventArgs e)
        {
            (new EventMonitor(this.webView2Control)).Show(this);
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            webView2Control.GoForward();
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            //HandleResize();
        }

        private void xToolStripMenuItem05_Click(object sender, EventArgs e)
        {
            this.webView2Control.ZoomFactor = 0.5;
        }

        private void xToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.webView2Control.ZoomFactor = 1.0;
        }

        private void xToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            this.webView2Control.ZoomFactor = 2.0;
        }

        private void xToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"Zoom factor: {this.webView2Control.ZoomFactor}", "WebView Zoom factor");
        }

        private void backgroundColorMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            Color backgroundColor = Color.FromName(menuItem.Text);
            this.webView2Control.DefaultBackgroundColor = backgroundColor;
        }
        #endregion

        private void HandleResize()
        {
            // Resize the webview
            webView2Control.Size = this.ClientSize - new System.Drawing.Size(webView2Control.Location);

            // Move the Events button
            btnEvents.Left = this.ClientSize.Width - btnEvents.Width;
            // Move the Go button
            btnGo.Left = this.btnEvents.Left - btnGo.Size.Width;

            // Resize the URL textbox
            txtUrl.Width = btnGo.Left - txtUrl.Left;
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
                this.tableLayoutPanel1.Visible = false;
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
                this.tableLayoutPanel1.Visible = true;
                // Restore the original Window state.
                this.WindowState = winState;
                this.FormBorderStyle = brdStyle;
                this.TopMost = topMost;
                this.Bounds = bounds;
                IsFullScreen = false;
            }
        }
    }
}
