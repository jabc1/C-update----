using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;



namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        SerialPort myport = new SerialPort();//全局变量
        int packlen = 512,sendtime = 1500;
        string filename = null;
        byte[] sendbuf = null;
        byte[] backupbuff = null;
        //char[] updatebuf = {'U','p','d','a','t','e'};
            string updatebuf = "Updateinfo:";//发送升级包信息
        string updatebufinfo = "Updatedata:";//发送数据内容
        byte[] byteArray;
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "中远大升级工具";
        }
        System.Timers.Timer aTimer = new System.Timers.Timer(500);//实例化time类

        int len, cout, m;
        int i;
        float n;
        int length = 0;

        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            myport.DataReceived += Port_DataReceived;
            //System.Timers.Timer t = new System.Timers.Timer(1000);//实例化Timer类，设置间隔时间为10000毫秒； 

            string[] ports = SerialPort.GetPortNames();//获取当前计算机的串行端口名称数组  ports = {COM1,COM2.....}
            if (ports == null || ports.Length <= 0)
            {
                comboBox1.Items.Add("无端口");
            }
            else
            {
                comboBox1.Items.AddRange(ports);
                comboBox1.SelectedIndex = 1;
            }

            comboBox2.Items.AddRange(new object[]{
            "115200","2400","4800","9600","19200","38400","115200"
            });
            comboBox2.SelectedIndex = 0;
            button4.Enabled = false;
            button3.Enabled = false;
            this.SetTimerParam();//初始化定时器
            this.progressBar1.Value = 0;
            this.progressBar1.Maximum = 100;
            this.textBox2.Enabled = false;
            //myport.DataReceived += new SerialDataReceivedEventHandler(sp1_DataReceived); //订阅委托   
            //myport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }
        //public static string StrToHex(string mStr) //返回处理后的十六进制字符串
        //{
        //    //先专为10进制,在专为16进制
        //    return BitConverter.ToString(ASCIIEncoding.Default.GetBytes(mStr)).Replace("-", " "); //

        //}
        //private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        //{

        //    SerialPort sp = (SerialPort)sender;

        //    string indata = sp.ReadExisting();

        //    Console.WriteLine("Data Received:");

        //    Console.Write(StrToHex(indata));

        //}

        public void button1_Click(object sender, EventArgs e)//打开串口
        {
            if (button1.Text == "打开串口")
            {
                button1.Text = "关闭串口";
                openPort(myport, 1);
            }
            else
            {
                if (myport.IsOpen)
                {
                    myport.Close();
                    button4.Enabled = false;
                    button3.Enabled = false;
                    button1.Text = "打开串口";
                    textBox1.AppendText("串口已关闭\n");
                }
            }
        }
        public void openPort(SerialPort SP, int flag)
        {
            SP.PortName = comboBox1.Text;//端口号
            //if (flag == 1)
            {
                SP.BaudRate = Convert.ToInt32(comboBox2.Text);//波特率
                textBox1.AppendText("当前端口号：" + comboBox1.Text + "\n");
                textBox1.AppendText("当前波特率：" + comboBox2.Text + "\n");
            }

            SP.Parity = Parity.None;
            SP.StopBits = StopBits.One;
            SP.DataBits = 8;
            try
            {
                SP.Open();
                button1.Text = "关闭串口";
                //if (flag == 1)
                {
                    textBox1.AppendText("串口打开成功，可以开始升级！\n");
                    button3.Enabled = true;
                    button4.Enabled = true;
                }
                //else
                //{
                //    textBox1.AppendText("串口打开成功，请选择升级包！\n");
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string s = null;

            try
            {
                do  //确保数据接收完整----用myport.ReadExisting(s)会出现中文乱码现象
                {
                    int count = myport.BytesToRead;
                    if (count <= 0)
                        break;
                    byte[] readBuffer = new byte[count];

                    Application.DoEvents();
                    myport.Read(readBuffer, 0, count);
                    s += System.Text.Encoding.Default.GetString(readBuffer);

                } while (myport.BytesToRead > 0);
                textBox1.AppendText(s + "\n");
                State(s);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void State(string s)
        {
            switch(s)
            {
                case "OK":
                    MessageBox.Show("升级成功");
                    break;
            }
        }

        public static byte[] ConvertToBinary(string Path)
        {
            FileStream stream = new FileInfo(Path).OpenRead();
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, Convert.ToInt32(stream.Length));
            return buffer;
        }

        private void button4_Click(object sender, EventArgs e)//加载文件
        {
            OpenFileDialog file = new OpenFileDialog();
            file.Filter = @"updata|*.bin";
            if (file.ShowDialog() == DialogResult.OK)
            {
                if (file.FileNames.Length > 0)
                {
                    this.textBox2.Text = file.FileName;
                    filename = file.FileName;
                    textBox1.AppendText("已加载升级包！\n");
                    button3.Text = "升 级";
                }
                FileInfo fi = new FileInfo(file.FileName);
                textBox1.AppendText("升级长度：" + fi.Length.ToString()+ "byte" + "\n");
                sendbuf = ConvertToBinary(filename);//获取文件内容准备发送
                backupbuff = sendbuf;
                len = sendbuf.Length;//总长度
                len -= 16;
                cout = len / packlen;//总包书
                m = len - packlen * cout;//剩余包书
                cout += 1;
                textBox1.AppendText("单包长度：" + packlen + "byte" + "\n");
                textBox1.AppendText("总包数：" + cout +  "\n");
                textBox1.AppendText("最后一包：" + m + "byte" + "\n");
            }

        }

        private void send_function(int i)
        {

            //for (i = 0; i < cout + 1; i++)
            {
               
                //byte[] test = sendbuf.Concat(updatebuf.ToArray).ToArray();
                if (i == 1)//第一包准备系统复位
                {
                    byteArray = System.Text.Encoding.Default.GetBytes(updatebuf);
                    myport.Write(byteArray, 0, 11);
                    myport.Write(sendbuf, 0, 16);
                    progressBar1.Value = 1;
                }
                else if (i == 2)//第二包保存升级信息
                {
                   //byteArray = System.Text.Encoding.Default.GetBytes(updatebuf);
                    myport.Write(byteArray,0,11);
                    myport.Write(sendbuf, 0, 16);
                    progressBar1.Value = 2;
                }
                else if(i>=3)//开始发送升级文件
                {
                    if (i == 3)
                    {
                        byteArray = System.Text.Encoding.Default.GetBytes(updatebufinfo);
                    }
                    if (i == cout+2)
                    {
                        
                        myport.Write(byteArray, 0, 11);
                        myport.Write(sendbuf, length+16, m);
                        length += m;
                        progressBar1.Value = 100;
                        aTimer.Enabled = false;
                        MessageBox.Show("升级完成!");
                        button3.Text = "升 级";
                        button1.Enabled = true;
                        progressBar1.Value = 0;
                    }
                    else
                    {
                        myport.Write(updatebufinfo);
                        myport.Write(sendbuf, length+16, packlen);
                        length += packlen;
                        n = (float)((float)length / (float)len);
                        n *= 100;
                        progressBar1.Value = (int)n;
                        //progressBar1.PerformStep();
                    }
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)//开始升级
        {
            if (button3.Text == "升 级")
            {
                button1.Enabled = false;
                button3.Text = "取消升级";
                aTimer.Enabled = true;
                i = 0;//开始发送第一包数据
                length = 0;//起始位置
            }
            else if (button3.Text == "取消升级")
            {
                button3.Text = "开始升级";
                aTimer.Enabled = false;
                progressBar1.Value = 0;
            }
            else
            {
                MessageBox.Show("请加载升级文件");
            }
        }
        private void test(object source, System.Timers.ElapsedEventArgs e)//执行发送功能
        {
            //MessageBox.Show(DateTime.Now.ToString());
            i++;
            //textBox1.AppendText("now time ->" + DateTime.Now.ToString()+"\n");
            textBox1.AppendText("发送间隔:" + sendtime +"ms "+ "发送第->" + i + "包\n");
            this.send_function(i);
        } 
        private void SetTimerParam()
        {
            //throw new NotImplementedException();
            //到时间的时候执行事件  
            aTimer.Elapsed += new ElapsedEventHandler(test);
            aTimer.Interval = sendtime;
            aTimer.AutoReset = true;//执行一次 false，一直执行true  
            //是否执行System.Timers.Timer.Elapsed事件  
            aTimer.Enabled = false;
        }

        private void button5_Click(object sender, EventArgs e)//clear
        {
            textBox1.Clear();
            textBox3.Clear();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!myport.IsOpen)//没有打开串口
            {
                MessageBox.Show("请打开串口！");
                return;
            }
            string sendstr = textBox3.Text;
            if (false)//hex send
            {
                ;
            }
            else
            {
                myport.WriteLine(textBox3.Text);
            }

        }

       
    }
}
