using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JFBrowser.Common
{
    public class DefautKeyboardHandler : IKeyboardHandler
    {
        //https://bbs.csdn.net/topics/392375553
        public bool OnKeyEvent(IWebBrowser browserControl, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey)
        {
            if (type == KeyType.KeyUp && Enum.IsDefined(typeof(Keys), windowsKeyCode))
            {
                var key = (Keys)windowsKeyCode;
                switch (key)
                {
                    case Keys.F12:
                        browser.ShowDevTools();
                        break;
                    case Keys.F11://全屏
                        CefSharp.WinForms.ChromiumWebBrowser chromeBrowser = browserControl as CefSharp.WinForms.ChromiumWebBrowser;
                        if (chromeBrowser != null && chromeBrowser.FindForm() is MainForm form) {
                            //退出全屏
                            form.OnFullScreen();
                        }
                        break;
                    case Keys.F5:
                        if (modifiers == CefEventFlags.ControlDown)
                        {
                            //MessageBox.Show("ctrl+f5");
                            browser.Reload(true); //强制忽略缓存
                        }
                        else
                        {
                            //MessageBox.Show("f5");
                            browser.Reload();
                        }
                        break;
                }
            }
            return false;
        }

        public bool OnPreKeyEvent(IWebBrowser browserControl, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            return false;
        }
    }
}
