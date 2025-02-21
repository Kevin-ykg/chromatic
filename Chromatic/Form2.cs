using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;




namespace Chromatic
{
    public partial class Form2 : Form
    {
        bool is_start_calibration = false;  //是否按下标定按钮

        //用于界面自适应的创建
        private new AutoAdaptWindowsSize AutoSize;

        public Form2()
        {
            InitializeComponent();
        }


        //开始标定
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Info_calibration.Total = Convert.ToDouble(textBox1.Text);
                Info_calibration.flag_calibration = true;
                Info_calibration.is_calibration = true;
                is_start_calibration = true;
            }
            catch
            {
                //如果没有输入待标定砖的数量，则不进行标定功能，防止误操作
            }
            
            
        }


        //取消标定
        private void button2_Click(object sender, EventArgs e)
        {
            Info_calibration.flag_calibration = false;
            Info_calibration.is_calibration = false;
            Info_calibration.Total = 0;


        }


        //定时器
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (is_start_calibration == true && Return_Info_calibration.is_start_calibration == true)
            {
                textBox2.Text = Return_Info_calibration.count.ToString();
            }
            

            if (Return_Info_calibration.is_complete == true)
            {
                label7.Visible = true;
                timer1.Enabled = false;
            }

        }


        private void Form2_Load(object sender, EventArgs e)
        {
            AutoSize = new AutoAdaptWindowsSize(this);
        }


        private void Form2_SizeChanged(object sender, EventArgs e)
        {
            //窗体大小改变事件
            if (AutoSize != null) // 一定加这个判断，电脑缩放布局不是100%的时候，会报错
            {
                AutoSize.FormSizeChanged();
            }
        }

    }


    //标定信息
    public struct Info_calibration
    {
        public static double Total;                 //需要标定砖的总数
        public static bool is_calibration;          //是否点击标定按钮的两个标志位
        public static bool flag_calibration;
    }
}
