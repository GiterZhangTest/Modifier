using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Modifier
{
    public partial class WaitForm : Form
    {
        public WaitForm()
        {
            InitializeComponent();
        }
    }
    public static class WaitBox
    {
        private static WaitForm frm = new WaitForm();
        public static void Wait()
        {
            frm.ShowDialog();
        }
        public static void Close()
        {
            frm.Close();
        }
    }

}
