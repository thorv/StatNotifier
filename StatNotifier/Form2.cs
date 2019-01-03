using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO.Ports;
namespace StatNotifier
{
    public partial class Form_Settings : Form
    {
        OcrCore ocr;
        SerialPort port;
        Form_Main parent;
        regMatch[] match;
        Scripts[] scripts;

        TextBox[] matchText;
        TextBox[] accumlate;
        TextBox[] script;
        Button[] test;
        Timer samplePeriod;

        public Form_Settings(Form_Main parent, OcrCore ocr, SerialPort port, regMatch[] match, Scripts[] scripts, Timer tmr)
        {
            InitializeComponent();
            this.ocr = ocr;
            this.port = port;
            this.match = match;
            this.scripts = scripts;
            numericUpDown1.Value = ocr.threshold;
            String[] ports = SerialPort.GetPortNames();
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(ports);
            int li = comboBox1.FindString(port.PortName);
            if (li >= 0)
            {
                comboBox1.SelectedIndex = li;
            }
            comboBox2.Items.Clear();
            comboBox2.Items.Add("9600");
            comboBox2.Items.Add("38400");
            comboBox2.Items.Add("115200");
            li = comboBox2.FindString(port.BaudRate.ToString());
            if (li < 0) li = 0;
            comboBox2.SelectedIndex = li;

            this.parent = parent;
            checkBox1.Checked = parent.removeSpace;

            matchText=new TextBox[tabControl1.TabCount];
            for (int i= 0; i < tabControl1.TabCount-1;i++) { matchText[i] = new TextBox(); }
            accumlate= new TextBox[tabControl1.TabCount];
            for (int i = 0; i < tabControl1.TabCount-1; i++) { accumlate[i] = new TextBox(); }
            script = new TextBox[tabControl1.TabCount];
            for (int i = 0; i < tabControl1.TabCount; i++) { script[i] = new TextBox(); }
            test = new Button[tabControl1.TabCount];
            for( int i=0; i< tabControl1.TabCount; i++) { test[i] = new Button(); }
            for (int i = 0; i < tabControl1.TabCount - 1; i++)
            {
                tabControl1.SelectedIndex = i;

                Label l1 = new Label();
                l1.Location = new Point(6, 16);
                l1.AutoSize = true;
                l1.Text = "Match text";
                tabControl1.TabPages[i].Controls.Add(l1);
                Label l2 = new Label();
                l2.Location = new Point(118, 72);
                l2.AutoSize = true;
                l2.Text = "Accumlate";
                tabControl1.TabPages[i].Controls.Add(l2);

                Label l3 = new Label();
                l3.Location = new Point(6, 92);
                l3.AutoSize = true;
                l3.Text = "Script";
                tabControl1.TabPages[i].Controls.Add(l3);

                tabControl1.TabPages[i].Controls.Add(matchText[i]);

                matchText[i].Location = new Point(6, 34);
                matchText[i].Size = new Size(263, 22);
                tabControl1.TabPages[i].Controls.Add(matchText[i]);
                matchText[i].TextChanged += new EventHandler(matchText_TextChanged);

                accumlate[i].Location = new Point(213, 69);
                accumlate[i].Size = new Size(52, 22);
                tabControl1.TabPages[i].Controls.Add(accumlate[i]);
                accumlate[i].TextChanged += new EventHandler(accumlate_TextChanged);
            }

            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                script[i].Location = new Point(6,110);
                script[i].Size = new Size(278,140);
                script[i].Multiline = true;
                script[i].ScrollBars = ScrollBars.Vertical;
                tabControl1.TabPages[i].Controls.Add(script[i]);
                script[i].Text = scripts[i].myScript;
                script[i].TextChanged += new EventHandler(script_TextChanged);

                test[i].Location = new Point(209, 263);
                test[i].Size = new Size(75, 23);
                test[i].Text = "Test";
                tabControl1.TabPages[i].Controls.Add(test[i]);
                test[i].Click += new EventHandler(test_Click);


            }
            Label lb = new Label();
            lb.AutoSize = true;
            lb.Text = "Script for [STOP] button pressed.";
            lb.Location = new Point(6, 90);
            tabControl1.TabPages[tabControl1.TabCount - 1].Controls.Add(lb);
            for (int i = 0; i < tabControl1.TabCount-1; i++)
            {
                regMatch tmpMatch = match[i];
                matchText[i].Text = String.Copy(tmpMatch.exps);
                accumlate[i].Text = tmpMatch.accumlate.ToString();
            }
            MaximumSize = new Size(370, 585);
            MinimumSize = new Size(370, 585);

            samplePeriod = tmr;
            nm_SamplePeriod.Value = tmr.Interval;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            ocr.threshold = (int)numericUpDown1.Value;
            Properties.Settings.Default.threshold=ocr.threshold;

        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            String s = comboBox1.SelectedItem.ToString();
            if (!String.Equals(s, String.Empty))
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
                port.PortName = s;
                try{
                    port.Open();
                    Properties.Settings.Default.PortName=s;
                }catch (Exception ex)
                {
                    MessageBox.Show(this, "Port Open Error\r\n"+ex.Message, "COM Port", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            Properties.Settings.Default.Save();
            timer1.Stop();
        }

        private void comboBox2_TextChanged(object sender, EventArgs e)
        {
            String s = comboBox2.SelectedItem.ToString();
            if (!String.Equals(s, String.Empty))
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
                try
                {
                    port.BaudRate = Int32.Parse(s);
                    Properties.Settings.Default.BaudRate = port.BaudRate;
                    port.Open();
                }
                catch (Exception )
                {
                    port.BaudRate = 9600;
                }
            }
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            parent.removeSpace = checkBox1.Checked;
            Properties.Settings.Default.removeSpace=checkBox1.Checked;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Mouse: " + Control.MousePosition.ToString();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void matchText_TextChanged(object sender, EventArgs e)
        {
            if (sender == matchText[0])
            {
                match[0].exps = String.Copy(matchText[0].Text);
                Properties.Settings.Default.exp1 = match[0].exps;
            }
            if (sender == matchText[1])
            {
                match[1].exps = String.Copy(matchText[1].Text);
                Properties.Settings.Default.exp2 = match[1].exps;
            }
            if (sender == matchText[2])
            {

                match[2].exps = String.Copy(matchText[2].Text);
                Properties.Settings.Default.exp3 = match[2].exps;
            }

        }
        private void accumlate_TextChanged(object sender, EventArgs e)
        {
            for(int i = 0; i < tabControl1.TabCount; i++)
            {
                if (sender == accumlate[i])
                {
                    try
                    {
                        match[i].accumlate = int.Parse(accumlate[i].Text);
                        if (match[i].accumlate < 1)
                        {
                            match[i].accumlate = 1;
                            accumlate[i].Text = "1";
                        }
                    }
                    catch (Exception)
                    {
                        match[i].accumlate = 1;
                        accumlate[i].Text = "1";
                    }
                }
            }
            Properties.Settings.Default.accumlate1 = match[0].accumlate;
            Properties.Settings.Default.accumlate2 = match[1].accumlate;
            Properties.Settings.Default.accumlate3 = match[2].accumlate;

        }
        private void script_TextChanged(object sender, EventArgs e)
        {
            if (sender == script[0])
            {
                scripts[0].myScript = script[0].Text;
            }
            if (sender == script[1])
            {
                scripts[1].myScript = script[1].Text;
            }
            if (sender == script[2])
            {
                scripts[2].myScript = script[2].Text;
            }
            if(sender == script[3])
            {
                scripts[3].myScript = script[3].Text;
            }
            Properties.Settings.Default.script1 = scripts[0].myScript;
            Properties.Settings.Default.script2 = scripts[1].myScript;
            Properties.Settings.Default.script3 = scripts[2].myScript;
            Properties.Settings.Default.script4 = scripts[3].myScript;
        }

        private void test_Click(object sender, EventArgs e)
        {
            for(int i = 0; i < tabControl1.TabCount; i++)
            {
                if(sender == test[i])
                {
                    if (string.Equals(test[i].Text, "Test"))
                    {
                        parent.testScript(i);
                        test[i].Text = "Abort";
                    }else
                    {
                        parent.breakScript();
                    }
                }
            }
        }

        public void doneAbort()
        {
            test[tabControl1.SelectedIndex].Text = "Test";
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                Scripts.HELPTEXT, "Script help", MessageBoxButtons.OK);
        }

        private void nm_SamplePeriod_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.samplePeriod = (int)nm_SamplePeriod.Value;
            samplePeriod.Interval= (int)nm_SamplePeriod.Value;
        }

        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            String[] ports = SerialPort.GetPortNames();
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(ports);
        }
    }
}
