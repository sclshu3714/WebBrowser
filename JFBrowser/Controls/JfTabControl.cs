using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JFBrowser.Controls
{
    public partial class JfTabControl : TabControl
    {
        public JfTabControl()
        {
            InitializeComponent();
        }
        private bool tabsVisible;

        [DefaultValue(false)]
        public bool TabsVisible
        {
            get { return tabsVisible; }
            set
            {
                if (tabsVisible == value) return;
                tabsVisible = value;
                RecreateHandle();
            }
        }

        protected override void WndProc(ref Message m)
        {
            // Hide tabs by trapping the TCM_ADJUSTRECT message
            if (m.Msg == 0x1328)
            {
                if (!tabsVisible && !DesignMode)
                {
                    m.Result = (IntPtr)1;
                    return;
                }
            }
            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Tab) || keyData == (Keys.Control | Keys.Shift | Keys.Tab) || keyData == (Keys.Left) || keyData == (Keys.Right))
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
