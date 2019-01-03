using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace StatNotifier
{
    public partial class Watcher : Form
    {

        public Watcher()
        {
            InitializeComponent();
        }
        Point previous ;
        private void Watcher_Load(object sender, EventArgs e)
        {
            this.ClientSize = Properties.Settings.Default.watcherSize;
            this.BackColor=Color.FromArgb(240,241,242);//三色同じだとサイズ変更時に異常動作
            this.TransparencyKey = this.BackColor;
            Point loc = this.PointToClient(this.Bounds.Location);
            int xx = this.Bounds.Location.X - loc.X;
            int yy = this.Bounds.Location.Y - loc.Y;
        }

        private void Watcher_Move(object sender, EventArgs e)
        {
            previous = this.Location;
            timer1.Interval = 500;
            timer1.Start();
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Enabled = false;
            if (this.Location.Equals(previous))
            {
                Properties.Settings.Default.watcherPos = this.Location;
                Properties.Settings.Default.Save();
            }
        }

        private void Watcher_ResizeEnd(object sender, EventArgs e)
        {
            Properties.Settings.Default.watcherSize = this.ClientSize;
            Properties.Settings.Default.Save();
        }
    }
}
