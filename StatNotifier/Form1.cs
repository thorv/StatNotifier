using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO.Ports;
using System.Runtime.InteropServices;

namespace StatNotifier
{
    public partial class Form_Main : Form
    {
        Boolean busy { set; get; }
        const int MATCHPATTERNS = 3;//中止時パターンを追加
        Watcher watcher;
        OcrCore ocr;
        regMatch[] matches;
        Scripts[] scripts;
        String portName;
        public bool removeSpace { set; get; }

        private void configs()
        {
            ocr.threshold = Properties.Settings.Default.threshold;
            serialPort1.BaudRate = Properties.Settings.Default.BaudRate;
            portName = Properties.Settings.Default.PortName;
            matches = new regMatch[MATCHPATTERNS];
            scripts = new Scripts[MATCHPATTERNS+1];
            matches[0] = new regMatch(
                Properties.Settings.Default.exp1,
                Properties.Settings.Default.accumlate1
                );
            scripts[0]= new Scripts(serialPort1,this, Properties.Settings.Default.script1);

            matches[1]=new regMatch(
                Properties.Settings.Default.exp2,
                Properties.Settings.Default.accumlate2
                );
            scripts[1] = new Scripts(serialPort1, this, Properties.Settings.Default.script2);
            matches[2] = new regMatch(
                Properties.Settings.Default.exp3,
                Properties.Settings.Default.accumlate3
                );
            scripts[2] = new Scripts(serialPort1, this, Properties.Settings.Default.script3);
            removeSpace = Properties.Settings.Default.removeSpace;

            scripts[3] = new Scripts(serialPort1, this, Properties.Settings.Default.script4);

            timer1.Interval = Properties.Settings.Default.samplePeriod;

        }

        public Form_Main()
        {
            InitializeComponent();
            this.Text = "Stat notifier "+Program.VERSION;
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            watcher = new Watcher();
            watcher.StartPosition = FormStartPosition.Manual;
            watcher.Location = Properties.Settings.Default.watcherPos;
            watcher.Show();

            float sc =GetScalingFactor();
            ocr = new StatNotifier.OcrCore(watcher,sc);

            configs();

            serialPort1.DataBits = 8;
            serialPort1.Parity = System.IO.Ports.Parity.None;
            serialPort1.StopBits = StopBits.One;
            serialPort1.Handshake = System.IO.Ports.Handshake.None;

            String[] ports = SerialPort.GetPortNames();
            foreach ( var s in ports)
            {
                if (String.Equals(portName, s)) {
                    serialPort1.PortName = portName;
                    try
                    {
                        serialPort1.Open();
                    }catch (Exception ex)
                    {
                        MessageBox.Show(this,"Port Open Error\r\n"+ex.Message,"COM Port",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    }
                }
            }
            if (!serialPort1.IsOpen)
            {
                MessageBox.Show(this, "Port Open Error\r\nNo port", "COM Port", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            timer1.Stop();
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!busy)
            {
                if (diag == null || !diag.Visible)
                {
                    busy = true;
                    ocr.setOcr();
                    Task t = new Task(doOcr);
                    t.Start();
                }
            }
            else
            {
                toolStripProgressBar1.ForeColor = Color.Thistle;
            }
        }
        regMatch matched;
        Scripts selScript;
        private void doOcr()
        {
            int pos=0, len=0;
            Invoke((MethodInvoker)delegate () {
                toolStripProgressBar1.ForeColor = Color.PaleGreen;
                toolStripProgressBar1.Step = 100;
                toolStripProgressBar1.PerformStep(); });
            ocr.doOcr();
            Invoke((MethodInvoker)delegate () {
                toolStripProgressBar1.Value = 0;
                });
            OcrCore.OcrResults ocrRes = ocr.getResults();
            String str1 = ocrRes.text.Replace("\n", System.Environment.NewLine);
            if (removeSpace)
            {
                str1 = str1.Replace(" ", String.Empty);
            }
            string str = str1;//表示用バックアップ
            int ii=0;
            String str2 = "";
            matched = null;
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i].checkMatch(str1))
                {
                    pos = matches[i].pos;
                    len = matches[i].len;
                }
                if (matches[i].result == regMatch.RESULTS.FOUND)
                {
                    str2 = "Found pattern" + (i + 1).ToString();
                    str1 = null;//以降確実にUNMATCHにする
                } else if (matches[i].result == regMatch.RESULTS.MATCH) {
                    matched = matches[i];
                    str2 = "Matched pattern" + (i + 1).ToString();
                    str1 = null;//以降確実にUNMATCHにする
                    ii = i;
                }
            }
            this.Invoke((MethodInvoker)delegate() { ShowResult(str, str2, ocrRes.bmp, pos, len); }) ;
            if (matched!=null) {
                selScript = scripts[ii];
                actScript();
            }else
            {
                busy = false;
            }
        }


        public void testScript(int n)
        {
            selScript = scripts[n];
            Task t = new Task(actScript);
            t.Start();
        }

        void actScript()
        {
            selScript.doScripts();
        }


        private void ShowResult(String str1, String str2, Bitmap bmp, int pos, int len)
        {
            int ww, hh;

            textBox1.Text = str1;
            if (len != 0)
            {
                textBox1.Select(pos, len);
                textBox1.ScrollToCaret();
            }
            if (str2 != null)
            {
                toolStripStatusLabel2.Text = str2;
            }

            Bitmap tmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Graphics g = Graphics.FromImage(tmp);
            if (1.0*bmp.Width / bmp.Height > 1.0*pictureBox1.Width / pictureBox1.Height)//元絵の方が横長
            {
                ww = pictureBox1.Width;//横幅優先表示
                hh = bmp.Height * pictureBox1.Width / bmp.Width;
            }
            else
            {
                hh = pictureBox1.Height;//縦幅優先表示
                ww = bmp.Width * pictureBox1.Height / bmp.Height;
            }
            g.DrawImage(bmp, 0, 0, ww, hh);
            g.Dispose();
            pictureBox1.Image = tmp;
        }

        Form_Settings diag;
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            breakScript();
            diag = new Form_Settings(this, ocr, serialPort1, matches,scripts, timer1);
            diag.ShowDialog(this);
        }

        public void commandMonitor(String cmd)
        {
            if (String.Equals(cmd, "done"))
            {
                if (diag != null)
                {
                    diag.doneAbort();
                }
                    busy = false;
            }
            else
            {
                toolStripStatusLabel4.Text = cmd;
                this.Update();
            }
        }
        public void breakScript()
        {
            if (selScript != null)
            {
                selScript.setBreak();
            }
        }
        AlertForm f;
        public void alertWindow(string msg, int posX, int posY, int sizeX, int sizeY, int col)
        {
            if (f == null || f.IsDisposed) f = new AlertForm();
            if (sizeX == 0 || sizeY == 0)
            {
                f.Hide();
            }
            else
            {
                f.Show();
                f.Text = msg;
                f.Location = new System.Drawing.Point(posX, posY);
                f.Size = new System.Drawing.Size(sizeX, sizeY);
                f.BackColor = System.Drawing.Color.FromArgb((int)(col | 0xff000000));
                f.TopMost = true;
                f.TopMost = false;
            }

        }

        private void Stop_Click(object sender, EventArgs e)
        {
            if (String.Equals(Stop.Text, "STOP")){
                Stop.Text = "RESUME";
                this.BackColor = SystemColors.ControlDark;
                breakScript();
                timer1.Stop();
                busy = true;
                testScript(MATCHPATTERNS);
            }
            else
            {
                Stop.Text = "STOP";
                this.BackColor = SystemColors.Control;
                timer1.Start();
                busy = false;

            }
        }
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,
        }
        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateDC(string lpszDriver, string lpszDevice,
            string lpszOutput, IntPtr lpInitData);
        [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
        static extern bool DeleteDC(IntPtr hdc);

        private float GetScalingFactor()
        {
            var sc = Screen.FromHandle(this.Handle);
            var desktop = CreateDC(sc.DeviceName, null, null, IntPtr.Zero);
            int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);
            DeleteDC(desktop);
            float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;
            return ScreenScalingFactor; // 1.00 = 100%
        }

        private void positionResetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            watcher.Location = new Point(50, 50);
            watcher.ClientSize = new System.Drawing.Size(210, 105);
            Properties.Settings.Default.watcherPos = watcher.Location;
            Properties.Settings.Default.watcherSize = watcher.ClientSize;
            Properties.Settings.Default.Save();
        }
    }
}
