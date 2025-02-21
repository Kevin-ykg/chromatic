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
    public partial class Form3 : Form
    {
        //用于界面自适应的创建
        private new AutoAdaptWindowsSize AutoSize;


        public Form3()
        {
            InitializeComponent();
        }


        private void Form3_Load(object sender, EventArgs e)
        {
            AutoSize = new AutoAdaptWindowsSize(this);
        }


        private void Form3_SizeChanged(object sender, EventArgs e)
        {
            //窗体大小改变事件
            if (AutoSize != null) // 一定加这个判断，电脑缩放布局不是100%的时候，会报错
            {
                AutoSize.FormSizeChanged();
            }
        }


        //显示色号信息
        private void timer1_Tick(object sender, EventArgs e)
        {
            textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;  //设置字体居中显示

            try
            {
                if (Num_Color.num != "" && Convert.ToInt32(Num_Color.num) > 1)
                {
                    textBox1.BackColor = Color.Red;    //对于有色差的砖，用于显示色号的文本框背景置红
                }
                else
                {
                    textBox1.BackColor = Color.Green;   //正常色号砖，用绿色背景来显示
                }
            }
            catch
            {
                textBox1.BackColor = Color.Red;   //有error的时候，背景是红色
            }
            
            textBox1.Text = Num_Color.num;
        }


    }


    //色号信息
    public struct Num_Color
    {
        public static string num = "";
    }
}
