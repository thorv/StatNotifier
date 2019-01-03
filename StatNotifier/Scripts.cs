using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StatNotifier
{
    public class Scripts
    {
        public const string HELPTEXT = 
            "wt sec : wait time in sec(float)\r\n" +
            "ts \"str\" : text send str\r\n" +
            "mm x,y : mouse move to x,y\r\n" +
            "md x,y : mouse moves x,y from current position\r\n" +
            "mw val : mouse wheel rotate\r\n" +
            "ml     : mouse left button click\r\n" +
            "mc     : mouse center button click\r\n" +
            "mr     : mouse right button click\r\n" +
            "al color(Hex),x,y,width,height,title : show color alert window\r\n" +
            "bp n   : Beep system sound. n=1..5\n" +

            ";      : command delimiter\r\n" +
            "//     : line comment\r\n" +
            "\r\n" +
            "This software call 'Tesseract-ocr 3.3.0' \r\n" +
            "Library under the Apache License V2.0";
        private static readonly object syncObject = new object();
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void SetCursorPos(int X, int Y);

        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        private const int MOUSEEVENTF_MOVE = 0x1;
        private const int MOUSEEVENTF_LEFTDOWN = 0x2;
        private const int MOUSEEVENTF_LEFTUP = 0x4;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x8;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        private const int MOUSEEVENTF_MIDDLEUP = 0x40;
        private const int MOUSEEVENTF_WHEEL = 0x800;

        public String myScript { get; set; }
        SerialPort myPort;
        Form_Main parentForm;
        Boolean isBreak;
        class ActionItem
        {
            public String cmd { get; set; }
            public delegate void actionDelegate(String data);
            public actionDelegate func { get; set; }
            public ActionItem(String cmd, actionDelegate func)
            {
                this.cmd = cmd;
                this.func = func;
            }
        }

        ActionItem[] ActionItems;

        public Scripts(SerialPort port,Form_Main form, String script)
        {
            myPort = port;
            parentForm = form;
            myScript = script;
            ActionItems = new ActionItem[] {
            new ActionItem("ts", doTextSend),
            new ActionItem("wt", doWait),
            new ActionItem("ml", doMouseClickL),
            new ActionItem("mc", doMouseClickCT),
            new ActionItem("mr", doMouseClickR),
            new ActionItem("mm", doMouseMoveAbsolute),
            new ActionItem("md", doMouseMoveDifferential),
            new ActionItem("mw", doMouseWheel),
            new ActionItem("al", doAlert),
            new ActionItem("bp", doBeep)
            };
        }
        delegate void showCommandDelegate(String str); 
        public void doScripts()
        {
            showCommandDelegate showCmd = parentForm.commandMonitor;
            System.IO.StringReader rs = new System.IO.StringReader(myScript);
            isBreak = false;
            while (rs.Peek() > -1)
            {
                String[] cmds = rs.ReadLine().Split(';');
                for (int i = 0; i < cmds.Length; i++)
                {
                    if (isBreak)
                    {
                        break;
                    }
                    cmds[i]=cmds[i].Trim();
                    String data = "";
                    if (cmds[i].Length > 2)
                    {
                        data = cmds[i].Substring(2).Trim();
                    }
                    String cmd = "";
                    if (cmds[i].Length >= 2)
                    {
                        cmd=cmds[i].Substring(0, 2).ToLower();
                    }
                    parentForm.Invoke(showCmd, cmd+data);
                    if (String.Equals("//", cmd))
                    {
                        break; //行末まで読み飛ばし
                    }
                    if (String.IsNullOrEmpty(cmd))
                    {
                        continue; //空のコマンドは次の;まで読み飛ばし
                    }
                    bool found = false;
                    foreach (ActionItem item in ActionItems)
                    {
                        if (String.Equals(item.cmd, cmd))
                        {
                            item.func(data);
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        MessageBox.Show("Command Error ("+cmd+")", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                parentForm.Invoke(showCmd, "");
            }
            parentForm.Invoke(showCmd, "done");
            rs.Close();
        }
        public void doTextSend(String data)
        {
            try
            {
                data=data.Trim('"');
//                MessageBox.Show("Send '" + data + "'", "DEBUG", MessageBoxButtons.OK, MessageBoxIcon.Information);
                myPort.Write(data);
            }catch
            {
                MessageBox.Show("Error (text send:"+data+")", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        public void doWait(String data)
        {
            try
            {
                int t=0;
                while (t < Double.Parse(data) * 1000)
                {
                    if (isBreak)
                    {
                        break;
                    }
                    t += 100;
                    System.Threading.Thread.Sleep(100);
                }
            }catch
            {
                MessageBox.Show("Parameter Error(wait:"+data+")", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }

        public void doMouseClickL(string data)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);              // マウスの左ボタンダウンイベントを発生させる
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);                // マウスの左ボタンアップイベントを発生させる

        }
        public void doMouseClickCT(string data)
        {
            mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);              // マウスの左ボタンダウンイベントを発生させる
            mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);                // マウスの左ボタンアップイベントを発生させる

        }
        public void doMouseClickR(string data)
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);              // マウスの左ボタンダウンイベントを発生させる
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);                // マウスの左ボタンアップイベントを発生させる

        }

        public void doMouseMoveAbsolute(string data)
        {
            try
            {
                String[] pos = data.Split(',');
                int mouseX = int.Parse(pos[0]);
                int mouseY = int.Parse(pos[1]);
                SetCursorPos(mouseX, mouseY);
            }catch
            {
                MessageBox.Show("Parameter Error(move mouse :"+data+")","ERROR", MessageBoxButtons.OK,MessageBoxIcon.Error);
            }                      
        }
        private void doMouseMoveDifferential(string data)
        {
            try
            {
                String[] pos = data.Split(',');
                int mouseX = int.Parse(pos[0]);
                int mouseY = int.Parse(pos[1]);
                mouse_event(MOUSEEVENTF_MOVE, mouseX, mouseY, 0, 0);
            }
            catch
            {
                MessageBox.Show("Parameter Error(move mouse differetial :" + data + ")", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void doMouseWheel(string data)
        {
            try
            {
                int dw = int.Parse(data);
                mouse_event(MOUSEEVENTF_WHEEL, 0, 0, dw, 0);
            }
            catch
            {
                MessageBox.Show("Parameter Error(move mouse differetial :" + data + ")", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void doBeep(string data)
        {
            System.Media.SystemSound[] beep = {
                System.Media.SystemSounds.Asterisk,
                System.Media.SystemSounds.Beep,
                System.Media.SystemSounds.Exclamation,
                System.Media.SystemSounds.Hand,
                System.Media.SystemSounds.Question
            };
            /*
             * [コントロールパネル]-[サウンド]の設定との対応
             Asterisk:メッセージ（情報）
             Beep:一般の警告音
             Exclamation:メッセージ（警告）
             Hand:システムエラー
             Question:メッセージ（問い合わせ）
            */

            int n=0;
            try
            {
                n = int.Parse(data)-1;
            }
            catch (Exception) { n = 0; }
            if(n<0 || n >= beep.Length) { n = 0; }
            beep[n].Play();
        }

        public void doAlert(string data)
        {
            string msg = "Alert";
            int col = 0xff0000;
            int posX = 0;
            int posY = 0;
            int sizeX = 320;
            int sizeY = 160;
            try
            {
                if (!string.Equals(data, string.Empty))
                {
                    String[] param = data.Split(',');
                    col = Convert.ToInt32(param[0], 16);
                    posX = int.Parse(param[1]);
                    posY = int.Parse(param[2]);
                    sizeX = int.Parse(param[3]);
                    sizeY = int.Parse(param[4]);
                    if (param.Length > 5)
                    {
                        msg = param[5];
                    }
                }
                parentForm.Invoke((MethodInvoker)delegate() { parentForm.alertWindow(msg, posX, posY, sizeX, sizeY, col); });
            }
            catch {
                MessageBox.Show("Parameter Error(Alert window:" + data + ")", "ERROR", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void setBreak()
        {
            isBreak = true;
        }
    }
}
