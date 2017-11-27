using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;

namespace ControlDevoluciones
{
    public partial class LoaderForm : DevComponents.DotNetBar.Metro.MetroForm
    {
        int iterate = 0;

        public LoaderForm()
        {
            InitializeComponent();
        }

        private void LoaderForm_Load(object sender, EventArgs e)
        {
            timer1.Interval = 100;
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (iterate == 10)
                this.Close();
            else
                iterate++;
        }
    }
}