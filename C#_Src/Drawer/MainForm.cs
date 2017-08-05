using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Drawer
{
    public delegate void ShowWindow();
    public delegate void HideWindow();
    public delegate void OpenPort();
    public delegate void ClosePort();
    public delegate Point GetMainPos();
    public delegate int GetMainWidth();
    public partial class MainForm : Form
    {
        Drawer Displayer;
        public MainForm()
        {
            InitializeComponent();
            serialPort1.Encoding = Encoding.GetEncoding("GB2312");                                  //串口接收编码
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;                   //
        }

        public void ClosePort()//关闭串口，供委托调用
        {
            try
            {
                serialPort1.Close();
            }
            catch (System.Exception)
            {
            	
            }
        }

        private Point GetMyPos()//供委托调用
        {
            return this.Location;
        }

        public void OpenPort()//打开串口，供委托调用
        {
            try
            {
                serialPort1.Open();
            }
            catch (System.Exception)
            {
                MessageBox.Show("串口打开失败，请检查", "错误");
            }
        }
        public void ShowMe()//供委托调用
        {
            this.Show();
        }
        public void HideMe()//供委托调用
        {
            this.Hide();
        }
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (!radioButton3.Checked)
            {
                textBox1.AppendText(serialPort1.ReadExisting());                                //串口类会自动处理汉字，所以不需要特别转换
            }
            else
            {
                try
                {
                    byte[] data = new byte[serialPort1.BytesToRead];                                //定义缓冲区，因为串口事件触发时有可能收到不止一个字节
                    serialPort1.Read(data, 0, data.Length);
                    if (Displayer != null)
                        Displayer.AddData(data);
                    foreach (byte Member in data)                                                   //遍历用法
                    {
                        string str = Convert.ToString(Member, 16).ToUpper();
                        textBox1.AppendText("0x" + (str.Length == 1 ? "0" + str : str) + " ");
                    }
                }
                catch { }
            }
        }

        private void CreateNewDrawer()//创建ADC绘制窗口
        {
            Displayer = new Drawer();//创建新对象
            Displayer.ShowMainWindow = new ShowWindow(ShowMe);//初始化类成员委托
            Displayer.HideMainWindow = new HideWindow(HideMe);
            Displayer.GetMainPos = new GetMainPos(GetMyPos);
            Displayer.CloseSerialPort = new ClosePort(ClosePort);
            Displayer.OpenSerialPort = new OpenPort(OpenPort);
            Displayer.GetMainWidth = new GetMainWidth(GetMyWidth);
            Displayer.Show();//显示窗口
        }

        int GetMyWidth()//供委托调用
        {
            return this.Width;
        }

        private void CreateDisplayer()
        {
            this.Left = 0;
            CreateNewDrawer();
            Rectangle Rect = Screen.GetWorkingArea(this);
            Displayer.SetWindow(Rect.Width - this.Width, new Point(this.Width, this.Top));//设置绘制窗口宽度，以及坐标
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (Displayer == null)//第一次创建Displayer = null
            {
                CreateDisplayer();
            }
            else
            {
                if (Displayer.IsDisposed)//多次创建通过判断IsDisposed确定串口是否已关闭，避免多次创建
                {
                    CreateDisplayer();
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            comboBox2.Text = "9600";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SerchAndAddSerialToComboBox(serialPort1, comboBox1);
        }
        private void SerchAndAddSerialToComboBox(SerialPort MyPort, ComboBox MyBox)
        {
            string Buffer = "";
            MyBox.Items.Clear();
            for (int i = 1; i < 20; i++)
            {
                try
                {
                    Buffer = "COM" + i.ToString();
                    MyPort.PortName = Buffer;
                    MyPort.Open();
                    MyBox.Items.Add(Buffer);
                    MyPort.Close();
                }
                catch
                {
                    //
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            byte[] Data = new byte[1];                                                         //单字节发数据     
            if (serialPort1.IsOpen)
            {
                if (textBox2.Text != "")
                {
                    if (!radioButton1.Checked)
                    {
                        try
                        {
                            serialPort1.Write(textBox2.Text);
                            //serialPort1.WriteLine();                             //字符串写入
                        }
                        catch
                        {
                            MessageBox.Show("串口数据写入错误", "错误");
                        }
                    }
                    else                                                                    //数据模式
                    {
                        try                                                                 //如果此时用户输入字符串中含有非法字符（字母，汉字，符号等等，try，catch块可以捕捉并提示）
                        {
                            for (int i = 0; i < (textBox2.Text.Length - textBox2.Text.Length % 2) / 2; i++)//转换偶数个
                            {
                                Data[0] = Convert.ToByte(textBox2.Text.Substring(i * 2, 2), 16);           //转换
                                serialPort1.Write(Data, 0, 1);
                            }
                            if (textBox2.Text.Length % 2 != 0)
                            {
                                Data[0] = Convert.ToByte(textBox2.Text.Substring(textBox2.Text.Length - 1, 1), 16);//单独处理最后一个字符
                                serialPort1.Write(Data, 0, 1);                              //写入
                            }
                            //Data = Convert.ToByte(textBox2.Text.Substring(textBox2.Text.Length - 1, 1), 16);
                            //  }
                        }
                        catch
                        {
                            MessageBox.Show("数据转换错误，请输入数字。", "错误");
                        }
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Close();
                }
                catch { }
                button2.Text = "打开串口";
            }
            else
            {
                try
                {
                    serialPort1.PortName = comboBox1.Text;
                    serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text, 10);//切换波特率
                    serialPort1.Open();
                    button2.Text = "关闭串口";
                }
                catch
                {
                    MessageBox.Show("打开串口失败", "错误");
                }
            }
        }

        private void btn_PIDOne_Click(object sender, EventArgs e)
        {
            byte[] UpData = new byte[4] { 0xff, 0xf0, 0xff, 0xf0};
            byte[] DownData = new byte[2] {0x0f, 0xf0};
            string Data;
            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Write(UpData, 0, 4);
                }
                catch { }
                try
                {
                    Data = "PIDOne" + "P:" + box_PIDOne_P.Text + "I:" + box_PIDOne_I.Text + "D:" + box_PIDOne_D.Text;
                    serialPort1.Write(Data);
                }
                catch { }
                try
                {
                    serialPort1.Write(DownData, 0, 2);
                }
                catch { }
            }
            else 
            {
                MessageBox.Show("请先打开串口", "错误");
            }
        }

        private void btn_PIDTwo_Click(object sender, EventArgs e)
        {
            byte[] UpData = new byte[4] { 0xff, 0xf0, 0xff, 0xf0 };
            byte[] DownData = new byte[2] { 0x0f, 0xf0 };
            string Data;
            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Write(UpData, 0, 4);
                }
                catch { }
                try
                {
                    Data = "PIDTwo" + "P:" + box_PIDTwo_P.Text + "I:" + box_PIDTwo_I.Text + "D:" + box_PIDTwo_D.Text;
                    serialPort1.Write(Data);
                }
                catch { }
                try
                {
                    serialPort1.Write(DownData, 0, 2);
                }
                catch { }
            }
            else
            {
                MessageBox.Show("请先打开串口", "错误");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.linkLabel1.Links[0].LinkData = "https://github.com/chuiming24";
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());    
        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            MessageBox.Show("傻逼！！让你点！", "错误");
            MessageBox.Show("傻逼！！都说啦不要点！！", "错误");
            MessageBox.Show("傻逼！！你就是要点！", "错误");
            MessageBox.Show("傻逼！！傻了吧！！", "错误");
            MessageBox.Show("啊好吧我知道你想知道PID发送的方式。", "错误");
            MessageBox.Show("宽宏大量的告诉你吧~", "错误");
            MessageBox.Show("PID发送格式为：0xff, 0xf0, 0xff, 0xf0 + \"PIDOne\"or\"PIDtwo\"+\"P:%fI:%fD:%D\"+0x0f, 0xf0", "提示");
        }
    }
}
