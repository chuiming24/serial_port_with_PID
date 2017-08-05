using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Drawer
{
    public partial class Drawer : Form
    {
        private const int Unit_length = 32;//单位格大小
        private int DrawStep = 8;//默认绘制单位
        private const int Y_Max = 512;//Y轴最大数值
        private const int MaxStep = 33;//绘制单位最大值
        private const int MinStep = 1;//绘制单位最小值
        private const int StartPrint = 32;//点坐标偏移量
        private List<byte> DataList = new List<byte>();//数据结构----线性链表
        private Pen TablePen = new Pen(Color.FromArgb(0x00, 0x00, 0x00));//轴线颜色
        private Pen LinesPen = new Pen(Color.FromArgb(0xa0, 0x00, 0x00));//波形颜色
        public ShowWindow ShowMainWindow;//定义显示主窗口委托访问权限为public
        public HideWindow HideMainWindow;//定义隐藏主窗口委托
        public OpenPort OpenSerialPort;//定义打开串口委托
        public ClosePort CloseSerialPort;//定义关闭串口委托
        public GetMainPos GetMainPos;//定义获取主窗口信息委托(自动对齐)
        public GetMainWidth GetMainWidth;//定义获取主窗口宽度(自动对齐)
        private bool KeyShift, KeyShowMain, KeyHideMain, KeyExit, KeyOpen, KeyClose, KeyStepUp, KeyStepDown;
        public Drawer()
        {
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint |
                           ControlStyles.AllPaintingInWmPaint,
                           true);//开启双缓冲

            this.UpdateStyles();
            InitializeComponent();
        }

        public void AddData(byte[] Data)
        {
            for (int i = 0; i < Data.Length;i++ )
                DataList.Add(Data[i]);//链表尾部添加数据
            Invalidate();//刷新显示
        }

        private void Form1_Paint(object sender, PaintEventArgs e)//画
        {
            String Str = "";
            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
            e.Graphics.FillRectangle(Brushes.White, e.Graphics.ClipBounds);

//Draw Y 纵向轴绘制
            for (int i = 0; i <= this.ClientRectangle.Width / Unit_length; i++)
            {
                e.Graphics.DrawLine(TablePen, StartPrint + i * Unit_length, StartPrint, StartPrint + i * Unit_length, StartPrint + Y_Max);//画线
                
                gp.AddString((i * (Unit_length / DrawStep)).ToString(), this.Font.FontFamily, (int)FontStyle.Regular, 12, new RectangleF(StartPrint + i * Unit_length - 7,this.ClientRectangle.Height-StartPrint + 4, 400, 50), null);//添加文字
            }
//Draw X 横向轴绘制
            for (int i = 0; i <= this.ClientRectangle.Height / Unit_length; i++)
            {
                e.Graphics.DrawLine(TablePen, StartPrint, (i + 1) * Unit_length, this.ClientRectangle.Width, (i + 1) * Unit_length);//画线
                Str = ((16 - i) * 16).ToString("X");
                Str = "0x" + (Str.Length == 1 ? Str + "0" : Str);
                if (i == 0)
                    Str = "0xFF";
                if(i == 17)
                    break;
                gp.AddString(Str, this.Font.FontFamily, (int)FontStyle.Regular, 14, new RectangleF(0, StartPrint + i * Unit_length - 8, 400, 50), null);//添加文字
            }
            e.Graphics.DrawPath(Pens.Black, gp);//写文字
            if (DataList.Count - 1 >= (this.ClientRectangle.Width - StartPrint) / DrawStep)//如果数据量大于可容纳的数据量，即删除最左数据
            {
                DataList.RemoveRange(0, DataList.Count - (this.ClientRectangle.Width - StartPrint)/DrawStep - 1);
            }
            // = DataList.Count;
            for (int i = 0; i < DataList.Count - 1; i++)//绘制
            {
                e.Graphics.DrawLine(LinesPen, StartPrint + i * DrawStep, 17 * Unit_length - DataList[i] * 2, StartPrint + (i + 1) * DrawStep, 17 * Unit_length - DataList[i + 1] * 2);
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        public void SetWindow(int width, Point Pt)//允许主窗口设置自己
        {
            int height = this.ClientRectangle.Height;
            height = this.Height - height;
            int BorderWeigh = this.Width - this.ClientRectangle.Width;
            this.Size = new Size(width - (width - BorderWeigh) % Unit_length, height + Y_Max + StartPrint + Unit_length);
            this.Location = Pt;
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)//按键弹开，执行对应动作
        {
            if (KeyShift)
            {
                if (KeyShowMain)//显示主窗体快捷键
                {
                    ShowMainWindow();
                    Rectangle Rect = Screen.GetWorkingArea(this);
                    SetWindow(Rect.Width - GetMainWidth(), new Point(GetMainWidth(), GetMainPos().Y));//缩小自己
                    KeyShowMain = false;
                }
                else
                {
                    if (KeyOpen)
                    {
                        OpenSerialPort();//打开主窗口串口
                        KeyOpen = false;
                    }
                    else
                    {
                        if (KeyClose)
                        {
                            CloseSerialPort();//关闭主窗口串口
                            KeyClose = false;
                        }
                        else
                        {
                            if (KeyExit)
                            {
                                KeyExit = false;//退出自己
                                Close();
                            }
                            else
                            {
                                if (KeyStepUp)
                                {
                                    if(DrawStep < MaxStep)//绘制单位递增
                                        DrawStep++;
                                    Invalidate();
                                    KeyStepUp = false;
                                }
                                else
                                {
                                    if (KeyStepDown)
                                    {
                                        if (DrawStep > MinStep)//绘制单位递减
                                            DrawStep--;
                                        Invalidate();
                                        KeyStepDown = false;
                                    }
                                    else
                                    {
                                        if (KeyHideMain)
                                        {
                                            HideMainWindow();//隐藏主窗口并扩大自己
                                            Rectangle Rect = Screen.GetWorkingArea(this);
                                            SetWindow(Rect.Width, new Point(0, GetMainPos().Y));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else//如果是其他按键，清空所有标志位
            {
                KeyClose = false;
                KeyOpen = false;
                KeyShowMain = false;
                KeyExit = false;
                KeyStepUp = false;
                KeyStepDown = false;
            }
            KeyShift = false;//清空shift标志位
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)//快捷键检测
        {
            if(e.Shift)//shift功能键按下
                KeyShift = true;//标志位置位
            switch (e.KeyCode)//功能标志置位
            {
                case Keys.S://显示主窗口
                    KeyShowMain = true;
                    break;
                case Keys.E://退出波形显示
                    KeyExit = true;
                    break;
                case Keys.O://打开串口
                    KeyOpen = true;
                    break;
                case Keys.C://关闭串口
                    KeyClose = true;
                    break;
                case Keys.Up://放大波形
                    KeyStepUp = true;
                    break;
                case Keys.H://隐藏主窗口
                    KeyHideMain = true;
                    break;
                case Keys.Down://缩小波形
                    KeyStepDown = true;
                    break;
                default:
                    break;
            }
        }

        private void Drawer_FormClosing(object sender, FormClosingEventArgs e)
        {
            ShowMainWindow();//关闭自己，显示主窗口
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (DrawStep < MaxStep)//绘制单位递增
                DrawStep++;
            Invalidate();

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (DrawStep > MinStep)//绘制单位递减
                DrawStep--;
            Invalidate();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DataList.Clear();
            Invalidate();
        }
    }
}
