using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//using NLSCAN.MacCtrl;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using ArmAssistBll;
using SYNCC;
using OpenNETCF.Net.NetworkInformation;
using System.Threading;

namespace MatClient_nlscan
{
    public partial class Form1 : Form
    {
        private string _stockNo;
        private string _workerNo;
        private string _IpAddress;
        private TextBox _nowControlModule;    //��ǰ��ȡ����Ŀؼ�
        private TextBox _nextControlModule;   //��һ������ؼ�
        private int _nowRunningState;        //��ǰ����״̬
        private string _codeStr;
        private int _pFlag;
        private int _FlagStatus;
        private string _outStr;
        //private NLSScanner scanCode = new NLSScanner();
        TcpClient m_socketClient;
        private int _ConnectTimeOut;
        private string _stockName;
        private string _workerName;
        private string _applicationName;
        private string _serverIP;
        private int _serverPort;
        private bool _cFlag;
        private int _reloadInterval;
        private int _reloadTime;
        private SYSTEM_POWER_STATUS_EX status;
        private int _oldTime;
        private string _tmpMsg;
        private string _cardNo;
        private string _cargoNo;
        private bool _connFlag;

        // 2M �Ľ��ջ�������Ŀ����һ�ν�������������ص���Ϣ
        byte[] m_receiveBuffer = new byte[2048 * 1024];

        /*
        [DllImport("user32.dll", EntryPoint = "keybd_event")]

        public static extern void keybd_event(

        byte bVk, //�����ֵ
         * 

        byte bScan,// һ��Ϊ0

        int dwFlags, //�������������� 0 Ϊ���£�2Ϊ�ͷ�

        int dwExtraInfo //�������������� һ����������Ϊ 0

        ); 
        */

        [DllImport("coredll.Dll")]
        public static extern int SetWindowPos(IntPtr hwnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);
        [DllImport("Coredll.dll", EntryPoint = "GetTickCount")]
        private static extern int GetTickCount();

        private class SYSTEM_POWER_STATUS_EX
        {
            public byte ACLineStatus = 0;
            public byte BatteryFlag = 0;
            public byte BatteryLifePercent = 0;
            public byte Reserved1 = 0;
            public uint BatteryLifeTime = 0;
            public uint BatteryFullLifeTime = 0;
            public byte Reserved2 = 0;
            public byte BackupBatteryFlag = 0;
            public byte BackupBatteryLifePercent = 0;
            public byte Reserved3 = 0;
            public uint BackupBatteryLifeTime = 0;
            public uint BackupBatteryFullLifeTime = 0;
        }

        [DllImport("coredll")]
        private static extern int GetSystemPowerStatusEx(SYSTEM_POWER_STATUS_EX lpSystemPowerStatus, bool fUpdate);


        public Form1()
        {
            InitializeComponent();
            //scanCode.OnDecodeEvent += new DecodeEventHandler(scanCode_OnDecodeEvent);            
            Init();
            
        }
        /// <summary>
        /// ��ȡ���еĺ�����
        /// </summary>
        /// <returns></returns>
        private int GetTick()
        {
            return GetTickCount();
        }

        /// <summary>
        /// ��ȡ����
        /// </summary>
        /// <returns></returns>
        private int GetPower()
        {
            if (GetSystemPowerStatusEx(status, false) == 1)
            {
                if (status.BatteryLifePercent > 100)
                    status.BatteryLifePercent = 100;
                return status.BatteryLifePercent;
            }
            else
            {
                return -1;
            }
        }
        private void ShowPower()
        {
            //statusBar1.Text="����:"+ NLSSysInfo.GetPowerPercent().ToString()+"%";
            //statusBar1.Text = GetTick().ToString();
            statusBar2.Text = _serverIP.Substring(_serverIP.Length-3,3)+"�û�:" + _workerName + "   | ����:" + GetPower();
        }
        /*
        /// <summary>
        /// ɨ���¼�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void scanCode_OnDecodeEvent(object sender, ScannerEventArgs e)
        {
            //string scanstr = NLSScanner.MultiByteToUnicodeString(e.ByteData, e.DataLen);
            try
            {
                //(_nowControlModule as TextBox).Text = e.Data;
                (_nowControlModule as TextBox).Text = NLSScanner.MultiByteToUnicodeString(e.ByteData, e.DataLen);
                //keybd_event((byte)Keys.D1, 0, 0, 0); //����ENTER
                //Thread.Sleep(200);
                //keybd_event((byte)Keys.D1, 0, 2, 0); //�ͷ�ENTER
                
                
            }
            catch { }
        }

        /// <summary>
        /// ����ɨ�賬ʱʱ��
        /// </summary>
        /// <param name="time"></param>
        private void SetScanTimeOut(int time)
        {
            scanCode.StartScanTimeOut = time;
        }

        
        /// <summary>
        /// �Ƿ���ɨ�蹦��
        /// </summary>
        /// <param name="flag">���ر�־</param>
        private void IsScan(bool flag)
        {
            scanCode.G_SetScannerEnabled(flag);
        }
        */

        /// <summary>
        /// ��ʾ����
        /// </summary>
        /// <param name="msg"></param>
        private void ShowMessage(string msg, string title)
        {
            /*
            try
            {
                //_nextControlModule = _nowControlModule;
               // _nowControlModule = null;
                MessageBox.Show(msg, title);
                //_nowControlModule = _nextControlModule;
            }
            catch (Exception e)
            {
                MessageBox.Show("�������:"+e.Message,"����");
            }
             * */
        }


        /// <summary>
        /// �µ�ͨ�ŷ�ʽ
        /// </summary>
        private void NewTransmit()
        {
            string msg;
            if (!WifiCtrl.GetInstance().isConnectWifi(_IpAddress, out msg))
            {
                //MessageBox.Show(msg + ",�뻻���ط����¿���!");
                _outStr = msg;
                return;
            }
            CompactFormatter.TransDTO transDTO = new CompactFormatter.TransDTO();
            transDTO.AppName = _applicationName;
            transDTO.CodeStr = _codeStr;
            transDTO.IP = _IpAddress;
            transDTO.pFlag = _pFlag;
            transDTO.StockNo = _stockNo;
            transDTO.Remark = msg;
            NetWorkScript.Instance.write(1, 1, 1, transDTO);
            NetWorkScript.Instance.AsyncReceive();
            if (NetWorkScript.Instance.messageList.Count > 0)
            {
                SocketModel socketModel = NetWorkScript.Instance.messageList[0];
                NetWorkScript.Instance.messageList.RemoveAt(0);
                _outStr = socketModel.message.ToString();
                _connFlag = true;
            }
            else
            {
                NetWorkScript.Instance.release();
                if (_connFlag)
                {
                    _connFlag = false;
                    Thread.Sleep(2000);
                    NewTransmit();
                }
                else
                {
                    _outStr = "û�з�����Ϣ!";
                }
            }
        }

        /// <summary>
        /// �����ʼ��
        /// </summary>
        private void Init()
        {
            //SetWindowPos(this.Handle, -1, 0, 0, 0, 0, 1 | 2);
            status = new SYSTEM_POWER_STATUS_EX();
            _connFlag = true;
            _oldTime = 0;
            _reloadTime = 0;
            _workerNo = "";
            _stockNo = "";
            _outStr = "";
            _codeStr = "";
            _stockName = "";
            _workerName = "";
            _tmpMsg = "û���̵�����";
            _cardNo = "";
            _cargoNo = "";
            _applicationName = "MatClient";
            _cFlag = true;
            _pFlag = 1;
            _nowRunningState = 4;
            _nowControlModule = textBox5;
            tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage5);
            textBox5.Focus();
            //tabControl1.Focus();
            string ver = "20160202";
            lbl_version.Text = "�汾��" + ver;
            XmlDocument xml = new XmlDocument();
            xml.Load("\\Program Files\\CONFIG.XML");
            try
            {
                ProcessInfo[] list = ProcessCE.GetProcesses();
                foreach (ProcessInfo item in list)
                {
                    if (item.FullPath.IndexOf("AutoUpdate") > 0)
                    {
                        item.Kill();
                    }
                }
                _serverIP = xml.SelectSingleNode("/Root/System/server_ip").InnerText;
                _serverPort = int.Parse(xml.SelectSingleNode("/Root/System/server_port").InnerText);
                _stockName = xml.SelectSingleNode("/Root/System/stock_name").InnerText;
                _stockNo = xml.SelectSingleNode("/Root/System/stock_no").InnerText;
                _reloadInterval = int.Parse(xml.SelectSingleNode("/Root/Applications/MatClient/reload_interval").InnerText);
                _ConnectTimeOut = int.Parse(xml.SelectSingleNode("/Root/System/maxSessionTimeout").InnerText) * 1000;
                
            }
            catch(Exception ex) 
            {
                MessageBox.Show(ex.Message, "����");
            }
            try
            {
                _IpAddress = WifiCtrl.GetInstance().GetWifiStatus().CurrentIpAddress.ToString();
                if (_IpAddress == "0.0.0.0")
                {
                    _IpAddress = IPHelper.GetIpAddress();
                }
            }
            catch
            {
                _IpAddress = IPHelper.GetIpAddress();
            }
            ShowPower();

        }

        /// <summary>
        /// У������
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private bool CheckBarCode(string code)
        {
            if (code.Length != 13)
            {
                return false;
            }
            int mOdd=0;
            int mEven=0;
            int mNumber=0;
            for (int i=1;i<code.Length;i++)
            {
                mNumber = int.Parse(code[i-1].ToString());
                if (i % 2 == 0)
                {
                    mEven += mNumber;
                }
                else
                {
                    mOdd += mNumber;
                }
            }
            mEven *= 3;
            mNumber = mOdd + mEven;
            mNumber = (10 - (mNumber % 10)) % 10;
            if (mNumber.ToString() == code[code.Length - 1].ToString())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// ���ܷ�������
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string DecryInt(string code)
        {
            string mapping = "1389602457138960245713896024571389602457138960245713896024571389602457138960245713896024571389602457";
            string decry_code = "";
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        if (mapping[j] == code[i])
                        {
                            decry_code += j.ToString();
                        }
                    }
                }
                int offset = int.Parse(decry_code);
                //ShowMessage(decry_code);
                for (int i = 3; i < code.Length - 1; i++)
                {
                    for (int j = offset; j < 10 + offset; j++)
                    {
                        if (mapping[j] == code[i])
                        {
                            decry_code += (j - offset).ToString();
                        }
                    }
                }
                //ShowMessage(decry_code);
                string decry = "";
                for (int i = 1; i < 4; i++)
                {
                    //decry += Convert.ToString(int.Parse(decry_code.Substring(3 * i, 3)),16);
                    //decry += String.Format("{0:X}", int.Parse(decry_code.Substring(3 * i, 3)));
                    decry += int.Parse(decry_code.Substring(3 * i, 3)).ToString("X2");
                }
                //ShowMessage(decry);
                return decry;
            }
            catch
            {
                return "";
            }

        }

        /// <summary>
        /// ���ӷ�����
        /// </summary>
        private void Connect()
        {
            /*
            //lock (this)
            //{
                try
                {
                    m_socketClient = new TcpClient(_serverIP, _serverPort);
                    //m_socketClient.ReceiveTimeout = 20 * 1000;

                    if (m_socketClient.Client.Connected)
                    {
                        //this.AddInfo("���ӳɹ�.");
                    }
                    else
                    {
                        //this.AddInfo("����ʧ��.");
                         ShowMessage("����ʧ��!","����");
                    }

                }catch
                {
                }
           // }
            //_oldTime = GetTick();
        */
        }

        /// <summary>
        /// ��������Ͽ�����
        /// </summary>
        private void Disconnect()
        {
            /*
            //lock (this)
            //{
                if (m_socketClient == null)
                {
                    return;
                }

                try
                {
                    m_socketClient.Close();
                    //this.AddInfo("�Ͽ����ӳɹ���");
                }
                catch 
                {
                    //this.AddInfo("�Ͽ�����ʱ����: " + err.Message);
                   // ShowMessage("�Ͽ�����ʱ����: " + err.Message,"����");
                }
                finally
                {
                    m_socketClient = null;
                    GC.Collect();
                    _oldTime = 0;
                }
           // }
             * */
        }

        /// <summary>
        /// ��ʾ��Ϣ
        /// </summary>
        /// <param name="message"></param>
        private void AddInfo(string message)
        {
            ShowPower();
            if (message.Length == 0)
            {
                //ShowMessage("�޷�����Ϣ��","����");

                //message = "�޷�����Ϣ��";
                return;
            }
            //ShowMessage(msg, "��ʾ", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            textBox8.Text = "";
            string[] msg = message.Split('^');

            foreach (string str in msg)
            {
                textBox8.Text = textBox8.Text+str+"\r\n";
            }
            if (_nowRunningState == 23)
            {
                textBox8.Text = textBox8.Text + "��1������\r\n";
                textBox8.Text = textBox8.Text + "��2�����\r\n";
            }
            panel2.Visible = true;
            _nextControlModule = _nowControlModule;
            _nowControlModule = textBox7;
            textBox7.Focus();
            if (message.IndexOf("�ɹ�") == -1)
            {
                textBox8.BackColor = Color.Red;
                //ShowMessage(textBox8.BackColor.ToString(),"color");
                buz_on();
            }
            else
            {
                textBox8.BackColor = Color.Green;
            }
            //_outStr="";
        }

        /// <summary>
        /// ������Ϣ
        /// </summary>
        private void SendOneDatagram()
        {
            if (GetTick()>(_oldTime+_ConnectTimeOut))
            {
                if (m_socketClient != null)
                {
                    this.Disconnect();
                }
                this.Connect();
            }
            
            string datagramText2 ="1#" + _pFlag + "#" + _codeStr+"#"+_applicationName+"#"+_stockNo;

            byte[] b = Encoding.UTF8.GetBytes(datagramText2);//����ָ�����뽫string����ֽ�����
            string datagramText = string.Empty;
            for (int i = 0; i < b.Length; i++)//���ֽڱ�Ϊ16�����ַ�����%����
            {
                datagramText += "%" + Convert.ToString(b[i], 16);
            }

            //byte[] encbuff = System.Text.Encoding.UTF8.GetBytes(datagramText);
            //datagramText = Convert.ToBase64String(encbuff);
            //if (ShowMessage(datagramText, "��ʾ", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.OK)
            //{
                //Application.Exit();
            //}
            //datagramText = textBox1.Text + "#" + textBox2.Text + "#" + textBox3.Text + "|" + textBox4.Text + "|" + textBox5.Text + "|";
            //datagramText += textBox6.Text + "|" + textBox8.Text + "|" + textBox7.Text + "|#";

            byte[] Cmd = Encoding.ASCII.GetBytes(datagramText);
            byte check = (byte)(Cmd[0] ^ Cmd[1]);
            for (int i = 2; i < Cmd.Length; i++)
            {
                check = (byte)(check ^ Cmd[i]);
            }
            datagramText = "<" + datagramText +(char)check + ">";
            byte[] datagram = Encoding.ASCII.GetBytes(datagramText);

            try
            {
                m_socketClient.Client.Send(datagram);
                //this.AddInfo("send text = " + datagramText);

                //if (ck_AsyncReceive.Checked)  // �첽���ջش�
                // {
                //m_socketClient.Client.BeginReceive(m_receiveBuffer, 0, m_receiveBuffer.Length, SocketFlags.None, this.EndReceiveDatagram, this);
                //}
                // else
                // {
                this.Receive();
                //}
            }
            catch (Exception err)
            {
                if (_cFlag && _stockNo=="01")
                {
                    _cFlag = false;
                    if (m_socketClient != null)
                    {
                        this.Disconnect();
                    }
                    this.Connect();
                    try
                    {
                        m_socketClient.Client.Send(datagram);
                        this.Receive();
                    }
                    catch { }
                    
                }
                else
                {
                    //this.AddInfo("���ʹ���: " + err.Message);
                    ShowMessage("���ӷ�����ʧ��: " + err.Message, "����");
                    //this.AddInfo("���ӷ�����ʧ��:!\r\n" + err.Message);
                    _outStr = "";
                    this.CloseClientSocket();
                    _oldTime = 0;
                }

            }
        }

        private void Receive()
        {
            try
            {
                int len = m_socketClient.Client.Receive(m_receiveBuffer, 0, m_receiveBuffer.Length, SocketFlags.None);
                if (len > 0)
                {
                    CheckReplyDatagram(len);
                }
                _cFlag = true;
                _oldTime = GetTick();
            }
            catch (Exception err)
            {
                //this.AddInfo("���մ���: " + err.Message);
                ShowMessage("���մ���: " + err.Message, "����");
                this.CloseClientSocket();
                _oldTime = 0;
            }
        }

        private void CheckReplyDatagram(int len)
        {
            string datagramText = Encoding.ASCII.GetString(m_receiveBuffer, 0, len);
            //byte[] decbuff = Convert.FromBase64String(replyMesage);
            if (datagramText[0] != '%')
            {
                _outStr = "���ص���Ϣ����";
                return;
            }
            string[] chars = datagramText.Substring(1,datagramText.Length-1).Split('%');
            byte[] b = new byte[chars.Length];
            //����ַ���Ϊ16�����ֽ�����
            for (int i = 0; i < chars.Length; i++)
            {
                b[i] = Convert.ToByte(chars[i], 16);
            }
            //����ָ�����뽫�ֽ������Ϊ�ַ���
            //string content = Encoding.UTF8.GetString(b);
            _outStr = Encoding.UTF8.GetString(b,0,b.Length);
            //this.AddInfo(replyMesage);
        }

        /// <summary>
        /// �رտͻ�������
        /// </summary>
        private void CloseClientSocket()
        {
            try
            {
                //m_socketClient.Client.Shutdown(SocketShutdown.Both);
                m_socketClient.Client.Close();
                m_socketClient.Close();
            }
            catch
            {
            }
            finally
            {
                m_socketClient = null;
                GC.Collect();
            }
        }

        /// <summary>
        /// ����������
        /// </summary>
        private void buz_on()
        {
            /*
            int m_iFreq = 2730;
            int m_iVolume = 60;
            int m_iMdelay = 300;
            int m_iBuzCtrlRe = -1;
            m_iBuzCtrlRe = NLSSysCtrl.buz_ctrl(m_iFreq, m_iVolume, m_iMdelay);
            NLSSysCtrl.vibrator_ctrl(m_iMdelay);
             * */

        }

        /// <summary>
        /// �˳�����
        /// </summary>
        public void Quit()
        {
            if (MessageBox.Show("�Ƿ��˳�?", "��ʾ", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.OK)
            {
                _nowControlModule = null;
                _nextControlModule = null;
                this.Disconnect();
                ProcessContext pi = new ProcessContext();
                ProcessCE.CreateProcess("\\Program Files\\AutoUpdate\\AutoUpdate.exe",
                                  "", IntPtr.Zero,
                                  IntPtr.Zero, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, pi);
                Thread.Sleep(2500);
                Application.Exit();
            }
        }

        /// <summary>
        /// �������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IsLostFocus(object sender, EventArgs e)
        {
            if (_nowControlModule != null)
            {
                _nowControlModule.Focus();
            }
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            string remark = "";
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    if (_nowRunningState == 1)
                    {
                        //buz_on();
                        panel1.Visible = false;
                        _codeStr = "";
                        _nowControlModule = textBox9;
                        _nowRunningState = 0;
                        tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage1);
                        textBox9.Focus();
                    }
                    else
                    {
                        _nowRunningState = 1;
                        label5.Text = "ɨ�賵��:";
                        label5.ForeColor = Color.Black;
                        _codeStr = "";
                        textBox1.Text = "";
                        textBox1.Focus();
                    }
                    break;
                case Keys.Enter:
                    if (_nowRunningState == 1)
                    {
                        if (textBox1.Text.Length >= 12)
                        {
                            if (textBox1.Text.Length > 13)
                                textBox1.Text = textBox1.Text.Substring(textBox1.Text.Length - 13, 13);
                            _codeStr += textBox1.Text.Substring(0, 12) + "|";
                            _nowRunningState = 11;
                            label5.Text = "�������:";
                            label5.ForeColor = Color.Green;
                            textBox1.Text = "";
                            textBox1.Focus();
                            /*
                            if (CheckBarCode(textBox1.Text))
                            {
                                _codeStr += textBox1.Text.Substring(0, 12) + "|";
                                _nowRunningState = 11;
                                label5.Text = "�������:";
                                label5.ForeColor = Color.Green;
                                textBox1.Text = "";
                                textBox1.Focus();
                            }
                            else
                            {
                                textBox1.Text = "";
                            }
                             * */
                        }
                        else
                        {
                            textBox1.Text = "";
                        }
                    }
                    else if (_nowRunningState == 11)
                    {
                        
                        if (textBox1.Text.Length == 13)
                        {
                            if (CheckBarCode(textBox1.Text))
                            {
                                string seal_no = DecryInt(textBox1.Text);
                                if (seal_no != "")
                                {
                                    //trip_no|seal_no|(01,02)|remark|worker_no|stock_no|stock_name|
                                    
                                    _codeStr += seal_no+"|";
                                    panel1.Visible = true;
                                    _nowRunningState = 111;
                                }
                            }
                        }
                        else if (textBox1.Text.Length == 6)
                        {
                            remark = "(����)";
                            _codeStr += textBox1.Text + "|";
                            panel1.Visible = true;
                            _nowRunningState = 111;
                        }
                        textBox1.Text = "";
                        textBox1.Focus();
                    }
                    else
                    {
                        textBox1.Text = "";
                    }
                    break;
                case Keys.D1:
                    if (_nowRunningState == 111)
                    {
                        _nowRunningState = 1;
                        _pFlag = 1;
                        _codeStr +="01|�Ϸ���" +remark+"|"+ _workerNo + "|"+_stockNo+"|"+_stockName+"|";
                        //SendOneDatagram();
                        NewTransmit();
                        label5.Text = "ɨ�賵��:";
                        label5.ForeColor = Color.Black;
                        //_codeStr = "";
                        _nowControlModule = textBox1;
                        panel1.Visible = false;
                        textBox1.Text = "";
                        textBox1.Focus();
                        this.AddInfo(_outStr);
                    }
                    break;
                case Keys.D2:
                    if (_nowRunningState == 111)
                    {
                        _nowRunningState = 1;
                        _pFlag = 1;
                        _codeStr += "02|�����" + remark + "|" + _workerNo + "|" + _stockNo + "|" + _stockName + "|";
                        //SendOneDatagram();
                        NewTransmit();
                        label5.Text = "ɨ�賵��:";
                        label5.ForeColor = Color.Black;
                        //_codeStr = "";
                        _nowControlModule = textBox1;
                        panel1.Visible = false;
                        textBox1.Text = "";
                        textBox1.Focus();
                        this.AddInfo(_outStr);
                    }
                    break;
            }
        }

        /// <summary>
        /// �����û���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (textBox5.Text.Length > 0)
                    {
                        _nowControlModule = textBox6;
                        textBox6.Focus();
                    }
                    break;
                case Keys.Escape:
                    if (textBox5.Text.Length > 0)
                    {
                        textBox5.Text = "";
                    }
                    else
                    {
                        Quit();
                    }
                    break;
            }
        }

        /// <summary>
        /// �������룬ȷ�ϼ���¼
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox6_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (textBox6.Text.Length > 0)
                    {
                        _pFlag = 2;
                        _codeStr = textBox5.Text + "|" + textBox6.Text + "|" + _stockNo + "|";
                        //SendOneDatagram();
                        NewTransmit();
                        string[] data = _outStr.Split('#');
                        if (data[0] == "SUCCESS")
                        {
                            _workerNo = textBox5.Text;
                            _workerName = data[1];
                            _nowControlModule = textBox9;
                            statusBar2.Text = "�û�:" + _workerName + "   | ����:" + GetPower();
                            //buz_on();
                            tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage1);
                            _nowRunningState = 0;
                            //this.AddInfo("��¼�ɹ�");
                            _codeStr = "";
                            _outStr = "";
                            textBox9.Focus();
                            textBox5.Text = "";
                        }
                        else
                        {
                            _nowControlModule = textBox5;
                            this.AddInfo(_outStr);
                            textBox6.Text = "";
                        }
                        textBox5.Text = "";
                        textBox6.Text = "";

                        
                    }
                    break;
                case Keys.Escape:
                    if (textBox6.Text.Length > 0)
                    {
                        textBox6.Text = "";
                    }
                    else
                    {
                        _nowControlModule = textBox5;
                        textBox5.Focus();
                    }
                    break;
            }
        }

        /// <summary>
        /// �������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.D1:
                    if (_nowRunningState == 2)
                    {
                        textBox3.Text = "װж���";
                        textBox3.BackColor = Color.Green;
                        _FlagStatus = 3;
                        _nowRunningState = 21;
                        textBox2.Text = "";
                        panel3.Visible = false;
                    }
                    break;
                case Keys.D2:
                    if ((_stockNo == "01") && (_nowRunningState==2))
                    {
                        textBox3.Text = "ȡ��װ��";
                        textBox3.BackColor = Color.Red;
                        _FlagStatus = 5;
                        _nowRunningState = 22;
                        textBox2.Text = "";
                        panel3.Visible = false;
                    }
                    break;
                case Keys.D3:
                    if (_nowRunningState == 2)
                    {
                        textBox3.Text = "���ʻ���";
                        textBox3.BackColor = Color.Yellow;
                        _FlagStatus = 6;
                        _nowRunningState = 23;
                        textBox2.Text = "";
                        panel3.Visible = false;
                    }
                    break;
                case Keys.D4:
                    if ((_stockNo == "01") && (_nowRunningState == 2))
                    {
                        textBox3.Text = "��������";
                        textBox3.BackColor = Color.Red;
                        _FlagStatus = 11;
                        _nowRunningState = 24;
                        textBox2.Text = "";
                        panel3.Visible = false;
                    }
                    break;
                case Keys.D5:
                    if (_nowRunningState == 2)
                    {
                        textBox3.Text = "�����̵�";
                        textBox3.BackColor = Color.Blue;
                        _FlagStatus = 12;
                        _nowRunningState = 25;
                        textBox2.Text = "";
                        panel3.Visible = false;
                    }
                    break;
                case Keys.Enter:
                    if (textBox2.Text.Length == 0 && _nowRunningState == 23)
                    {
                        AddInfo("��һ������^^");
                        return;
                    }
                    if (textBox2.Text.Length == 0 && _nowRunningState == 25)
                    {
                        textBox4.Text = _tmpMsg;
                        _nowControlModule = textBox4;
                        _nowControlModule.Focus();
                        panel5.Visible = true;
                    }
                    else if (textBox2.Text.Length >= 12)
                    {
                        if (_FlagStatus == 3 && _stockNo == "01")
                        {
                            if ((_reloadTime + _reloadInterval * 1000) > GetTick())
                            {
                                this.AddInfo("װ�����С��" + _reloadInterval + "�룡");
                                return;
                            }
                        }
                        string isBar = "0";
                        if (CheckBarCode(textBox2.Text))
                        {
                            isBar = "1";
                        }
                        if (textBox2.Text.Length > 13)
                            textBox2.Text = textBox2.Text.Substring(textBox2.Text.Length - 13, 13);
                        _codeStr += textBox2.Text.Substring(0, 12) + "|" + _stockNo + "|" + _workerNo + "|" + _IpAddress + "|" + isBar + "|";
                        _pFlag = _FlagStatus;
                        //SendOneDatagram();
                        NewTransmit();
                        if (_outStr.IndexOf("��X") > -1)
                        {
                            lbl_carNo.Text = _outStr.Substring(_outStr.IndexOf("��X"), 7);
                            _reloadTime = GetTick();
                        }
                        if (_outStr.IndexOf("�ɹ�") > -1 && _nowRunningState == 25)
                        {
                            _tmpMsg = _outStr.Substring(_outStr.IndexOf("^")+1);
                        }
                        this.AddInfo(_outStr);
                    }
                    textBox2.Text = "";
                    textBox2.Focus();
                    break;
                case Keys.Escape:
                    //buz_on();
                    lbl_carNo.Text = "";
                    _nowControlModule = textBox9;
                    _nowRunningState = 0;
                    _codeStr = "";
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage1);
                    textBox9.Focus();
                    break;

            }
        }

        private void Form1_Closing(object sender, CancelEventArgs e)
        {
        }

        /// <summary>
        /// �������ȷ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox7_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D1)
            {
           
                try
                {
                    if (_codeStr.Length > 0)
                    {
                        _pFlag = 4;
                        string codestr = _codeStr.Replace("|", "_");
                        _codeStr = _IpAddress + "|" + _applicationName + "|" + _stockNo + "|" + codestr + "|" + textBox8.Text + "|" + _workerNo + "|";
                        //SendOneDatagram();
                        NewTransmit();
                    }
                }
                catch 
                {
                    ShowMessage(_outStr,"����");
                }
                finally
                {
                    if (_nowRunningState == 522)
                    {
                        _nowRunningState = 52;
                    }
                    textBox7.Text = "";
                    textBox8.Text = "";
                    _nowControlModule = _nextControlModule;
                    panel2.Visible = false;
                    _nowControlModule.Text = "";
                    _nowControlModule.Focus();
                    _codeStr = "";
                    _outStr = "";
                    textBox8.BackColor = Color.Yellow;
                }
            }
            else if ((e.KeyCode == Keys.D2))
            {
                if (_nowRunningState == 522)
                {
                    label22.Text = "��ʵ����";
                    textBox7.Text = "";
                    textBox8.Text = "";
                    _nowControlModule = _nextControlModule;
                    panel2.Visible = false;
                    _nowControlModule.Text = "";
                    _nowControlModule.Focus();
                    _codeStr = "";
                    _outStr = "";
                    textBox8.BackColor = Color.Yellow;
                }
                else if (_nowRunningState == 23)
                {
                    _nowRunningState = 231;
                    panel6.Visible = true;
                    textBox7.Text = "";
                    textBox8.Text = "";
                    _nowControlModule = tb_recycle_trip;
                    panel2.Visible = false;
                    _nowControlModule.Text = "";
                    _nowControlModule.Focus();
                    _codeStr = "";
                    _outStr = "";
                    textBox8.BackColor = Color.Yellow;
                }

            }
        }

        /// <summary>
        /// ��������ת
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox9_KeyDown(object sender, KeyEventArgs e)
        {
            //buz_on();
            switch (e.KeyCode)
            {
                case Keys.D1:
                    _nowControlModule = textBox1;
                    _nowRunningState = 1;
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage2);
                    textBox1.Focus();
                    break;
                case Keys.D2:
                    _nowControlModule = textBox2;
                    _nowRunningState = 2;
                    panel3.Visible = true;
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage3);
                    textBox2.Focus();
                    break;
                case Keys.D3:
                    _nowControlModule = textBox5;
                    _nowRunningState = 4;
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage5);
                    textBox5.Focus();
                    _workerNo = "";
                    statusBar2.Text = "�û���";
                    break;
                case Keys.D4:
                    _nowControlModule = tb_deal01;
                    _nowRunningState = 5;
                    panel4.Visible = true;
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(deal);
                    tb_deal01.Focus();
                    break;

            }
        }

        private void Form1_LostFocus(object sender, EventArgs e)
        {
            if (_nowControlModule != null)
            {
                _nowControlModule.Focus();
            }
            else
            {
                _nowControlModule = textBox9;
                _nowRunningState = 0;
                tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage1);
                textBox9.Focus();
            }


            
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tb_deal01_KeyUp(object sender, KeyEventArgs e)
        {
            /*
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    _nowControlModule = textBox9;
                    _nowRunningState = 0;
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage1);
                    textBox9.Focus();
                    break;
                case Keys.Enter:
                    if (tb_deal01.Text.Length ==13)
                    {
                        _codeStr = tb_deal01.Text + "|" + _stockNo + "|" + _workerNo + "|" + _serialNo + "|";
                        _pFlag = 13;
                        SendOneDatagram();
                        tb_deal01.Text = "";
                        AddInfo(_outStr);
                    }
                    break;
            }
             * */
            switch (e.KeyCode)
            {
                case Keys.D1:
                    if (_nowRunningState == 5)
                    {
                        label21.Text = "��ѯ�����̳�";
                        label22.Text = "ɨ������";
                        _FlagStatus = 13;
                        _nowRunningState = 51;
                        tb_deal01.Text = "";
                        panel4.Visible = false;
                    }
                    break;
                case Keys.D2:
                    if ((_stockNo == "01") && (_nowRunningState == 5))
                    {
                        label21.Text = "������";
                        label22.Text = "ɨ������";
                        _FlagStatus = 14;
                        _nowRunningState = 52;
                        tb_deal01.Text = "";
                        panel4.Visible = false;
                        _codeStr = "";
                    }
                    break;
                case Keys.D3:
                    if (_nowRunningState == 5)
                    {
                        label21.Text = "��ӡ˾����ִ";
                        label22.Text = "ɨ�賵�̿�";
                        _FlagStatus = 29;
                        _nowRunningState = 53;
                        tb_deal01.Text = "";
                        panel4.Visible = false;
                        _codeStr = "";
                    }
                    break;
                case Keys.Enter:
                    if (_nowRunningState == 522)
                    {
                        try
                        {
                            int.Parse(tb_deal01.Text);
                        }
                        catch 
                        {
                            tb_deal01.Text = "";
                            return;
                        }
                        _pFlag = 20;
                        _codeStr = _cardNo + "|" + _cargoNo + "|" + tb_deal01.Text + "|" + _workerNo + "|" + _stockNo + "|";
                        //SendOneDatagram();
                        NewTransmit();
                        //if (_outStr.IndexOf("�ɹ�") > 0)
                        //{
                            _nowRunningState = 52;
                            label22.Text = "ɨ������";
                        //}
                        this.AddInfo(_outStr);
                        tb_deal01.Text = "";
                    }
                    else if ((tb_deal01.Text.Length == 0) && (_nowRunningState == 52))
                    {
                        _cargoNo = "000000000000";
                        _nowRunningState = 523;
                        label22.Text = "ɨ�����ʿ�";
                        tb_deal01.Text = "";
                    }
                    else if ((tb_deal01.Text.Length >= 12) && (_nowRunningState!=522))
                    {
                        if (_nowRunningState == 52)
                        {
                            label22.Text = "ɨ�����ʿ�";
                            _cargoNo = tb_deal01.Text.Substring(0, 12);
                            _codeStr = _cargoNo + "|";
                            tb_deal01.Text = "";
                            _nowRunningState = 521;
                            return;
                        }
                        else if (_nowRunningState == 521)
                        {
                            _cardNo = tb_deal01.Text.Substring(0, 12);
                            _codeStr += _cardNo + "|" + _workerNo + "|";
                        }
                        else if (_nowRunningState == 523)
                        {
                            _cardNo = tb_deal01.Text.Substring(0, 12);
                            label22.Text = "��ʵ����";
                            tb_deal01.Text = "";
                            _nowRunningState = 522;
                            return;
                        }
                        else
                        {
                            _codeStr = tb_deal01.Text + "|" + _stockNo + "|" + _workerNo + "|" + _stockName + "|";
                        }
                        _pFlag = _FlagStatus;
                        //SendOneDatagram();
                        NewTransmit();
                        if ((_nowRunningState == 521) && (_outStr.IndexOf("�ɹ�") > 0))
                        {
                            _nowRunningState = 522;
                            label22.Text = "ɨ������";
                        }
                        else if (_nowRunningState == 51)
                        {
                            label22.Text = "ɨ������";
                        }
                        else if (_nowRunningState == 53)
                        {
                            label22.Text = "ɨ�賵�̿�";
                        }
                        else
                        {
                            _nowRunningState = 52;
                            label22.Text = "ɨ������";
                        }
                        this.AddInfo(_outStr);
                    }

                    /*
                    switch (_nowRunningState)
                    {
                        case 522:
                            try
                            {
                                int.Parse(tb_deal01.Text);
                            }
                            catch
                            {
                                tb_deal01.Text = "";
                                return;
                            }
                            _pFlag = 20;
                            _codeStr = _cardNo + "|" + _cargoNo + "|" + tb_deal01.Text + "|" + _workerNo + "|" + _stockNo + "|";
                            //SendOneDatagram();
                            NewTransmit();
                            if (_outStr.IndexOf("�ɹ�") > 0)
                            {
                                _nowRunningState = 52;
                                label22.Text = "ɨ������";
                            }
                            this.AddInfo(_outStr);
                            tb_deal01.Text = "";
                            break;
                        case 52:
                            if (tb_deal01.Text.Length >= 12)
                            {
                                label22.Text = "ɨ�����ʿ�";
                                _cargoNo = tb_deal01.Text.Substring(0, 12);
                                _codeStr = _cargoNo + "|";
                                tb_deal01.Text = "";
                                _nowRunningState = 521;
                                return;
                            }
                            else if (tb_deal01.Text.Length == 0)
                            {
 
                            }
                            break;
                        case 521:
                            _cardNo = tb_deal01.Text.Substring(0, 12);
                            _codeStr += _cardNo + "|" + _workerNo + "|";
                            break;
                        case 51:
                            if (tb_deal01.Text.Length >= 12)
                            {
                                _codeStr = tb_deal01.Text + "|" + _stockNo + "|" + _workerNo + "|" + _stockName + "|";
                            }
                            break;
                    }
                    _pFlag = _FlagStatus;
                    SendOneDatagram();
                    if ((_nowRunningState == 521) && (_outStr.IndexOf("�ɹ�") > 0))
                    {
                        _nowRunningState = 522;
                    }
                    else
                    {
                        _nowRunningState = 52;
                    }
                    this.AddInfo(_outStr);

                    label22.Text = "ɨ������";
                    tb_deal01.Text = "";
                    tb_deal01.Focus();
                    */
                    break;
                case Keys.Escape:
                    //buz_on();
                    _nowControlModule = textBox9;
                    _nowRunningState = 0;
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage1);
                    textBox9.Focus();
                    break;

            }
        }

        private void tb_deal01_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// ����̵����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox4_KeyUp(object sender, KeyEventArgs e)
        {
            panel5.Visible = false;
            _nowControlModule = textBox2;

            if (e.KeyCode==Keys.D1)
            {
                _codeStr = _stockNo + "|" + _workerNo + "|";
                _pFlag = 18;
                //SendOneDatagram();
                NewTransmit();
                this.AddInfo(_outStr);
                if (_outStr.IndexOf("�ɹ�") > -1)
                {
                    _tmpMsg = "û���̵�����";
                }
            }
            else if (e.KeyCode==Keys.D2)
            {
                _codeStr = _stockNo + "|" + _workerNo + "|";
                _pFlag = 19;
                //SendOneDatagram();
                NewTransmit();
                this.AddInfo(_outStr);
                if (_outStr.IndexOf("�ɹ�") > -1)
                {
                    _tmpMsg = "û���̵�����";
                }
            }
            textBox2.Focus();
        }

        private void tb_recycle_trip_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (tb_recycle_trip.Text.Length == 13)
                    {
                        _codeStr = tb_recycle_trip.Text.Substring(0,12) + "|" + _stockNo + "|" + _workerNo + "|";
                        _pFlag = 24;
                        //SendOneDatagram();
                        NewTransmit();
                        if (_outStr.IndexOf("�ɹ�") > -1)
                        {
                            panel6.Visible = false;
                            lbl_carNo.Text = "";
                            _nowControlModule = textBox9;
                            _nowRunningState = 0;
                            _codeStr = "";
                            tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage1);
                            
                        }
                        else
                        {
                            tb_recycle_trip.Text = "";
                        }
                        this.AddInfo(_outStr);
                    }
                    break;
                case Keys.Escape:
                    lbl_carNo.Text = "";
                    _nowControlModule = textBox9;
                    _nowRunningState = 0;
                    _codeStr = "";
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPage1);
                    textBox9.Focus();
                    panel6.Visible = false;
                    break;
            }
        }
    }

}