using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Xml.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using System.Text.RegularExpressions;



namespace Chromatic
{
    public delegate void delegate_result_return(Mat img_show, double time_consuming , string name, double HSV_H, double LAB_L, double LAB_A, double LAB_B, int[] Hash_Code, int[] Hash_Code_rotate,double BGR_B,double BGR_G,double BGR_R,Mat DstImg);
    public delegate void delegate_error_return(double time_consuming, string name);

    
    public partial class Form1 : Form
    {
        //色差处理类方法初始化
        measure measure = new measure();


        //窗体2,3,4,5的实例化
        Form2 form2;
        Form3 form3;
        Form4 form4;
        Form5 form5;


        //用于打开线扫采图软件的进程，集成后不再用
        private Process process = new Process();


        //用于和数码屏（USB转串口模块）之间的串口通信使用
        private System.IO.Ports.SerialPort serialPort = new SerialPort();


        //高低角度图像是否处理完成标志
        public bool enable_process = false;


        //定义相关过程控制变量
        Mat image_high = new Mat();       //离线工作时，本地读取的高角度图像
        Mat image_high_Mem = new Mat();   //在线工作时，内存共享拿到的高角度图像
        Mat[] BGR = new Mat[3];
        Mat mat_RGB_B = new Mat();        //内存共享拿到高角度的三通道图像
        Mat mat_RGB_G = new Mat();
        Mat mat_RGB_R = new Mat();
        bool Online = false;              //判断当前是在线还是离线运行,区分主要是为了跑本地图库时显示文件夹的名称
        bool has_folder = true;           //读取本地图像时，单张图像是否有文件夹嵌套
        static string pattern = "";       //砖的版型
        static bool start_Pattern_Matching = false;       //是否启动模板匹配的色差计算
        static bool has_chromatic_aberration = false;     //是否有颜色的偏离，用来判断是否要保存NG图像时用到
        bool Cyan = false;                //砖是否偏青色
        bool Whitening = false;           //砖是否偏白
        static bool is_stable = false;    //当前色号标准是否稳定
        bool Set_Red_Main = false;        //设置偏红为主色
        static int Time_Delay = 5500;     //设置延时时间，单位ms
        int count_batch = 1;              //本地处理图像时计数用
        int num_stable = 0;               //用于当色差稳定后连续保存图像的计数
        double num = 1; double num_Mem_sharing = 0;      //内存共享读取图像标志位的计数
        double num_digital_screen = 1;          //数码屏计数
        public string path_scr;           //处理本地图库时文件夹地址
        public double count = 0;          //处理图像方法的计数器
        public int next = 0;              //处理本地图库时，下一张图像的计数
        public static int result_count = 0; static int aberration_count = 0;       //送入图像后处理方法器的计数以及有色差砖的计数
        static List<double> list_LAB_A = new List<double>();                       //各种颜色通道的列表，用来判断颜色通道的波动
        static List<double> list_LAB_B = new List<double>();
        static List<double> list_LAB_L = new List<double>();
        static List<double> list_Hamming_Distance = new List<double>(); 
        static double sum_LAB_L = 0;             //模板匹配时记录下来的通道信息
        static double sum_LAB_A = 0;
        static double sum_LAB_B = 0;
        static double sum_HSV_H = 0;
        static double sum_BGR_B = 0;
        static double sum_BGR_G = 0;
        static double sum_BGR_R = 0;
        double sum_Delta_E_pure = 0;
        double sum_Delta_E_texture = 0;


        double Delta_E_pure = 0;                 //纯色砖色差ΔE
        double Delta_E_texture = 0;              //纹理砖色差ΔE
        double cosTheta = 0;            //两个向量的夹角余弦值
        double deltaE = 0;                       //CIE2000公式计算的δE

        double first_Delta_E = 0;
        double first_LAB_L = 0;
        int num_Delta_E = 0;

        static double mean_HSV_H = 0;
        static double mean_LAB_L = 0;            //不使用模板匹配或者匹配失败后采用的通道均值
        static double mean_LAB_A = 0;
        static double mean_LAB_B = 0;
        static double mean_BGR_B = 0;
        static double mean_BGR_G = 0;
        static double mean_BGR_R = 0;
        double mean_Delta_E_pure = 0;
        double mean_Delta_E_texture = 0;

        bool use_cat = true;                     //用来表示是否同时使用两种色差公式进行检测，谁的效果后计算中就采用谁
        bool Complex_patterns = false;            //用来标志当前砖型表面纹理是否复杂

        static double diff_HSV_H, diff_LAB_L, diff_LAB_A, diff_LAB_B, diff_Delta_E, diff_Delta_E_pure, diff_Delta_E_texture,diff_BGR_B,diff_BGR_G,diff_BGR_R,diff_B,diff_G,diff_R;            //各颜色通道的差值
        static double sigma_LAB_A = 0; static double sigma_LAB_B = 0; static double sigma_LAB_L = 0;       //均方差

        //用于砖型复杂系数二次判定的相关变量
        public int num_hash = 0;
        public double Hamming_Distances = 0;
        public double Coefficient = 6;
        public bool Pattern_ok = false;

        public bool is_first_color = true;
        bool is_first1 = true; bool is_first2 = true; bool is_first3 = true; bool is_first4 = true; bool is_first5 = true; bool is_first6 = true; bool is_first7 = true; bool is_first8 = true; bool is_first9 = true; bool is_first10 = true; bool is_first11 = true; bool is_first12 = true; bool is_first13 = true; bool is_first14 = true; bool is_first15 = true; bool is_first16 = true; bool is_first17 = true; bool is_first18 = true; bool is_first19 = true; bool is_first20 = true; bool is_first21 = true; bool is_first22 = true; bool is_first23 = true; bool is_first24 = true; bool is_first25 = true; bool is_first26 = true;
        int temp_color_num1, temp_color_num2, temp_color_num3, temp_color_num4, temp_color_num5, temp_color_num6, temp_color_num7, temp_color_num8, temp_color_num9, temp_color_num10, temp_color_num11, temp_color_num12, temp_color_num13, temp_color_num14, temp_color_num15, temp_color_num16, temp_color_num17, temp_color_num18, temp_color_num19, temp_color_num20, temp_color_num21, temp_color_num22, temp_color_num23, temp_color_num24, temp_color_num25, temp_color_num26;
        int count_color1 = 0; int count_color2 = 0; int count_color3 = 0; int count_color4 = 0; int count_color5 = 0; int count_color6 = 0; int count_color7 = 0; int count_color8 = 0; int count_color9 = 0; int count_color10 = 0; int count_color11 = 0; int count_color12 = 0; int count_color13 = 0; int count_color14 = 0; int count_color15 = 0; int count_color16 = 0; int count_color17 = 0; int count_color18 = 0; int count_color19 = 0; int count_color20 = 0; int count_color21 = 0; int count_color22 = 0; int count_color23 = 0; int count_color24 = 0; int count_color25 = 0; int count_color26 = 0;
        public int color_num = 1;    //色度数值
        public int color_num_max = 1;
        string pattern_num = "0";

        public double sensitivity_color = 1.0;               //颜色灵敏度系数
        public double sensitivity_luminance = 1.0;           //亮度灵敏度系数
        static int num_1, num_2, num_3, num_4, num_5, num_6, num_7, num_8, num_9, num_10, num_11, num_12;    //各颜色通道砖的计数
        //public bool two_to_what = false; public bool three_to_what = false; public bool four_to_what = false; public bool five_to_what = false; public bool six_to_what = false; public bool seven_to_what = false; public bool eight_to_what = false; public bool nine_to_what = false; public bool ten_to_what = false; public bool eleven_to_what = false; public bool twelve_to_what = false;                    //用来判断并色操作的状态按钮
        public bool first_run = true;            //判断是否第一次进处理线程
        static bool first_Ceramics = true;       //处理结果汇总时，判断是否是第一块砖
        string path_scr_batch;                   //批量处理本地图库时地址
        string[] dir = null;
        string[] names;
        List<string> names_use = new List<string>();
        List<string> strings = new List<string>();
        List<List<string>> ImagePaths = new List<List<string>>();
        static List<Tuple<string, double, double, double, double, double, double,Tuple<string>>> flag_list = new List<Tuple<string, double, double, double, double, double, double,Tuple<string>>>();    //用于界面信息显示（色号，耗时，色度E差值，H通道值，亮度L差值，A通道值，B通道值，版型）的列表
        List<Tuple<string,double,double,double,int>> list_lab = new List<Tuple<string ,double, double, double,int>>();


        //陶瓷的版型，原图哈希编码，出现次数，总色差值ΔE，平均色差值，总的L通道值，平均L通道值，旋转180°图哈希编码
        static List<Tuple<string, int[], int[], int, Tuple<double, double, double, double, double, double>, Tuple<double, double, double, double, double, double>,Tuple<double, double, double, double, double, double,Mat>>> Ceramics_info_list = new List<Tuple<string, int[], int[], int, Tuple<double, double, double, double, double, double>, Tuple<double, double, double, double, double, double>, Tuple<double, double, double, double, double, double,Mat>>>();

        //Mat image_low = new Mat();
        Stopwatch stpwth1 = new Stopwatch();     //内存读取计时器,或者本地图像的读取耗时
        Stopwatch stpwth2 = new Stopwatch();     //处理图像的耗时
        Stopwatch stpwth = new Stopwatch();      //运行总时长
        System.Timers.Timer[] Timers = new System.Timers.Timer[100000000];      //延时显示与结果记录的Timer


        //保存图像与数据的一些地址信息
        private static string MesgDir = System.IO.Directory.GetCurrentDirectory().Split(new String[] { @"\bin" }, StringSplitOptions.None)[0] + @"\Message";
        string address_original = System.IO.Directory.GetCurrentDirectory().Split(new String[] { @"\bin" }, StringSplitOptions.None)[0] + @"\原图";
        string address_test = System.IO.Directory.GetCurrentDirectory().Split(new String[] { @"\bin" }, StringSplitOptions.None)[0] + @"\效果图";
        string address_show = System.IO.Directory.GetCurrentDirectory().Split(new String[] { @"\bin" }, StringSplitOptions.None)[0] + @"\界面显示图";
        string address_NG = System.IO.Directory.GetCurrentDirectory().Split(new String[] { @"\bin" }, StringSplitOptions.None)[0] + @"\有色偏差图";
        string pathColorDifComu = @"D:\GH_CeramicDetection\色差检测\色差通讯";
        

        //界面按钮状态
        bool Is_Save_Original_Image = false;
        bool Is_Have_Folder_Nest = false;
        bool Is_Template_Matching = false;
        bool Is_Save_NG_Image = false;
        bool Is_Lock_Interface = false;
        bool Is_Use_Factory_Custom = true;

        DialogResult result;//定义对话框窗口返回值结果类型 变量 result ;

        /// <summary>
        /// 错误日志记录
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="errorDetails"></param>
        public static void CaptureMesg(string name, string color_num, double sigma_LAB_A, double sigma_LAB_B, double HSV_H, double LAB_L, double LAB_A, double LAB_B, double diff_LAB_A, double diff_LAB_B)
        {
            if (!System.IO.Directory.Exists(MesgDir))
            {
                System.IO.Directory.CreateDirectory(MesgDir);//构建存放错误日志的路径
            }
            string filePath = MesgDir + @"\MesgLog.txt";
            if (!System.IO.File.Exists(filePath))
            {
                System.IO.File.Create(filePath).Close();//创建完文件后必须关闭掉流
            }
            System.IO.File.SetAttributes(filePath, System.IO.FileAttributes.Normal);
            System.IO.StreamWriter sr = new System.IO.StreamWriter(filePath, true);
            sr.WriteLine("===============" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "=============");
            sr.Write("当前图像名称：");
            sr.WriteLine(name);
            sr.Write("当前图像色号：");
            sr.WriteLine(color_num);
            sr.Write("HSV中的H通道均值：");
            sr.WriteLine(HSV_H);
            sr.Write("前15块砖CIELAB中的A通道的均方差为：");
            sr.WriteLine(sigma_LAB_A);
            sr.Write("前15块砖CIELAB中的B通道的均方差为：");
            sr.WriteLine(sigma_LAB_B);
            sr.Write("CIELAB中的L通道均值：");
            sr.WriteLine(LAB_L);
            sr.Write("CIELAB中的A通道均值：");
            sr.WriteLine(LAB_A);
            sr.Write("CIELAB中的B通道均值：");
            sr.WriteLine(LAB_B);
            sr.Write("CIELAB中的A通道差值：");
            sr.WriteLine(diff_LAB_A);
            sr.Write("CIELAB中的B通道差值：");
            sr.WriteLine(diff_LAB_B);
            sr.Write("CIELAB中的A和B通道差值之和：");
            sr.WriteLine(diff_LAB_A + diff_LAB_B);
            sr.WriteLine();
            sr.Close();//关闭写入的流
        }



        public static string pathExcel;
        /// <summary>
        /// 创建指定名称的Excel表格
        /// </summary>
        public static void Create_Excel()
        {
            string MesgDir = System.IO.Directory.GetCurrentDirectory().Split(new String[] { @"\bin" }, StringSplitOptions.None)[0] + @"\Message";        //获取当前项目所在地址
            
            try
            {
                //创建表格
                //指定路径,判断有无文件夹没有就创建
                pathExcel = MesgDir + "\\" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".xls";
                if (!System.IO.File.Exists(pathExcel))
                {
                    System.IO.Directory.CreateDirectory(MesgDir);//构建存放错误日志的路径
                    System.IO.File.Create(pathExcel).Close();
                }
                //workbook.SaveAs(@"D:\MyExcel.xls", FormatNum);

                //创建EXCEL
                NPOI.HSSF.UserModel.HSSFWorkbook wk = new NPOI.HSSF.UserModel.HSSFWorkbook();
                //创建一个Sheet
                NPOI.SS.UserModel.ISheet sheet = wk.CreateSheet("sheet0");
                //创建一行设为表头，表头名称自定义
                NPOI.SS.UserModel.IRow row = sheet.CreateRow(0);
                row.CreateCell(0).SetCellValue("图像名称");
                row.CreateCell(1).SetCellValue("版型");
                row.CreateCell(2).SetCellValue("图像时间");
                row.CreateCell(3).SetCellValue("图像色号");
                row.CreateCell(4).SetCellValue("前16块砖CIELAB的A通道均方差");
                row.CreateCell(5).SetCellValue("前16块砖CIELAB的B通道均方差");
                row.CreateCell(6).SetCellValue("前16块砖CIELAB的L通道均方差");
                row.CreateCell(7).SetCellValue("HSV中H通道均值");
                row.CreateCell(8).SetCellValue("CIELAB中L通道均值");
                row.CreateCell(9).SetCellValue("CIELAB中A通道均值");
                row.CreateCell(10).SetCellValue("CIELAB中B通道均值");
                row.CreateCell(11).SetCellValue("CIELAB中L通道差值");
                row.CreateCell(12).SetCellValue("CIELAB中A通道差值");
                row.CreateCell(13).SetCellValue("CIELAB中B通道差值");
                row.CreateCell(14).SetCellValue("计算中使用的ΔE的差值");
                row.CreateCell(15).SetCellValue("ΔE_pure的差值");
                row.CreateCell(16).SetCellValue("ΔE_texture的差值");
                row.CreateCell(17).SetCellValue("前16块中ΔE_pure均值");
                row.CreateCell(18).SetCellValue("前16块中ΔE_texture均值");
                row.CreateCell(19).SetCellValue("当前砖的ΔE_pure值");
                row.CreateCell(20).SetCellValue("当前砖的ΔE_texture值");
                row.CreateCell(21).SetCellValue("前16块中L均值");
                row.CreateCell(22).SetCellValue("前16块中H均值");
                row.CreateCell(23).SetCellValue("HSV中H通道差值");
                row.CreateCell(24).SetCellValue("当前砖的灰度均值");
                row.CreateCell(25).SetCellValue("当前砖的灰度均方差值");
                row.CreateCell(26).SetCellValue("BGR中B通道均值");
                row.CreateCell(27).SetCellValue("BGR中G通道均值");
                row.CreateCell(28).SetCellValue("BGR中R通道均值");
                row.CreateCell(29).SetCellValue("BGR中B通道差值");
                row.CreateCell(30).SetCellValue("BGR中G通道差值");
                row.CreateCell(31).SetCellValue("BGR中R通道差值");
                row.CreateCell(32).SetCellValue("余弦夹角");
                row.CreateCell(33).SetCellValue("CIEDE2000差值");
                
                using (FileStream stream = new FileStream(pathExcel, FileMode.Open, FileAccess.Write))
                {
                    wk.Write(stream);
                    wk.Close();
                }
            }
            catch (Exception ex)
            {

            }

        }


        /// <summary>
        /// 把数据写入Excel表格中去
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="e"></param>
        public static void Data2Excel(string a, string pattern, string b, string c, double d, double e, double f, double g, double h, double k, double l, double m, double n, double o,double p, double q, double r,double s, double t, double u, double v, double w,double x,double y,double z,double zz,double aa,double bb,double cc, double dd, double ee, double ff, double gg, double hh)
        {
            try
            {
                // 第一步：读取文件流
                NPOI.HSSF.UserModel.HSSFWorkbook workbook;  //HSSFWorkbook:是操作Excel2003以前（包括2003）的版本，扩展名是.xls；
                                                            //XSSFWorkbook: 是操作Excel2007后的版本，扩展名是.xlsx；
                                                            //SXSSFWorkbook: 是操作Excel2007后的版本，扩展名是.xlsx；
                using (FileStream stream = new FileStream(pathExcel, FileMode.Open, FileAccess.Read))
                {
                    workbook = new NPOI.HSSF.UserModel.HSSFWorkbook(stream);
                }

                // 第二步：创建新数据行，并把数据写入行
                NPOI.SS.UserModel.ISheet sheet = workbook.GetSheetAt(0);
                NPOI.SS.UserModel.IRow row1 = sheet.CreateRow(sheet.LastRowNum + 1);

                row1.CreateCell(0).SetCellValue(a);
                row1.CreateCell(1).SetCellValue(pattern);
                row1.CreateCell(2).SetCellValue(b);
                row1.CreateCell(3).SetCellValue(c);
                row1.CreateCell(4).SetCellValue(d);
                row1.CreateCell(5).SetCellValue(e);
                row1.CreateCell(6).SetCellValue(f);
                row1.CreateCell(7).SetCellValue(g);
                row1.CreateCell(8).SetCellValue(h);
                row1.CreateCell(9).SetCellValue(k);
                row1.CreateCell(10).SetCellValue(l);
                row1.CreateCell(11).SetCellValue(m);
                row1.CreateCell(12).SetCellValue(n);
                row1.CreateCell(13).SetCellValue(o);
                row1.CreateCell(14).SetCellValue(p);
                row1.CreateCell(15).SetCellValue(q);
                row1.CreateCell(16).SetCellValue(r);
                row1.CreateCell(17).SetCellValue(s);
                row1.CreateCell(18).SetCellValue(t);
                row1.CreateCell(19).SetCellValue(u);
                row1.CreateCell(20).SetCellValue(v);
                row1.CreateCell(21).SetCellValue(w);
                row1.CreateCell(22).SetCellValue(x);
                row1.CreateCell(23).SetCellValue(y);
                row1.CreateCell(24).SetCellValue(z);
                row1.CreateCell(25).SetCellValue(zz);
                row1.CreateCell(26).SetCellValue(aa);
                row1.CreateCell(27).SetCellValue(bb);
                row1.CreateCell(28).SetCellValue(cc);
                row1.CreateCell(29).SetCellValue(dd);
                row1.CreateCell(30).SetCellValue(ee);
                row1.CreateCell(31).SetCellValue(ff);
                row1.CreateCell(32).SetCellValue(gg);
                row1.CreateCell(33).SetCellValue(hh);

                //第三步，写入数据
                using (FileStream stream = new FileStream(pathExcel, FileMode.Open, FileAccess.Write))
                {
                    workbook.Write(stream);
                    workbook.Close();
                }
            }
            catch (Exception ex)  //如果写入数据超出65536的行高限制，则重新生成excel表格
            {
                //throw new ArgumentException(ex.Message);

                Create_Excel();

            }

            
        }



        //定义色差处理任务的Task线程
        async Task task_chromatic(Mat image, string path)    //将方法标记为async后，可以在方法中使用await关键字
        {
            await Task.Delay(2);
            await Task.Run(()=> measure.run(image, path));
        }


        //初始化构造函数
        public Form1()
        {
            InitializeComponent();

            form2 = new Form2();
            form3= new Form3();
            //form4 = new Form4();
            form5 = new Form5();

            //绑定结果的显示与记录的事件方法
            measure.event_result_return += result_show;
            measure.event_error_return += error_record;


            //绑定数码屏通讯结果显示的事件方法
            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
            //open_SerialPort();


            //创建用于保存结果和小图合集的文件夹
            if (Directory.Exists(address_original) == false || Directory.Exists(address_test) == false || Directory.Exists(address_NG) == false || Directory.Exists(address_show) == false || Directory.Exists(pathColorDifComu) == false)
            {
                Directory.CreateDirectory(address_original);
                Directory.CreateDirectory(address_test);
                Directory.CreateDirectory(address_NG);
                Directory.CreateDirectory(address_show);
                Directory.CreateDirectory(pathColorDifComu);
                
            }

            Create_Excel();
            stpwth.Start();


            //取消界面上SDK按钮和内存共享计数的文本框的显示
            button27.Visible = false;


            //模板匹配以及保存NG图按钮状态
            checkBox3.Checked = true;
            checkBox4.Checked = false;

            //灵敏度默认值设定
            trackBar1.Value = 6;


            //删除色差通讯文件夹下的通讯标志位图像
            try
            {
                //获取所有文件夹的名称集合
                string path_sdk = "D:\\GH_CeramicDetection\\色差检测\\色差通讯\\";
                string jpgpattern = "*.jpg";

                string[] jpgfiles = Directory.GetFiles(path_sdk, jpgpattern);
                foreach (string jpgfile in jpgfiles)
                {
                    File.Delete(jpgfile);
                }

            }
            catch
            {
            }

        }


        //用于界面自适应的创建
        private new AutoAdaptWindowsSize AutoSize;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }


        Mat error_img = new Mat(150, 100, MatType.CV_8UC3, 255);   //处理有误时，界面显示的图像
        private void error_record(double time_consuming, string name)
        {
            //if (Online == true)  //只有在线检测的时候才会自动调整相机的曝光时间
            //{
            //    if (measure.Brightness_Judgment_Once == true)  //对图像的曝光进行第一次调整，改变亮度
            //    {
            //        if (measure.ExposureValue <= 180)
            //        {
            //            form4.m_AcqDevice.SetFeatureValue("ExposureTime", measure.ExposureValue);
            //            measure.Brightness_Judgment_Once = false;
            //        }
            //        else
            //        {
            //            MessageBox.Show("曝光值设置范围不得超过180");
            //        }
                    
            //    }

            //    if (measure.Brightness_Judgment_Twice == true)  //对图像的曝光进行第二次调整，改变亮度
            //    {
            //        if (measure.ExposureValue <= 180)
            //        {
            //            form4.m_AcqDevice.SetFeatureValue("ExposureTime", measure.ExposureValue);
            //            measure.Brightness_Judgment_Twice = false;
            //        }
            //        else
            //        {
            //            MessageBox.Show("曝光值设置范围不得超过180");
            //        }
                    
            //    }

            //    if (measure.Brightness_Judgment_Third == true)  //对图像的曝光进行第三次调整，改变亮度
            //    {
            //        if (measure.ExposureValue <= 180)
            //        {
            //            form4.m_AcqDevice.SetFeatureValue("ExposureTime", measure.ExposureValue);
            //            measure.Brightness_Judgment_Third = false;
            //        }
            //        else
            //        {
            //            MessageBox.Show("曝光值设置范围不得超过180");
            //        }

            //    }
            //}
            

            //保存用于界面显示的图像
            Cv2.ImWrite(address_show + "\\" + "num_" + measure.count.ToString() + ".jpg", error_img);
            if (Online == true)
            {
                //CaptureMesg(measure.count.ToString(), "has_error", 0, 0, 0, 0, 0, 0, 0, 0);
                Data2Excel(measure.count.ToString(), "无版型", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "has_error", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,0,0,0,0,0,0,0,0,0,0,0,0, 0, 0, 0, 0, 0, 0,0,0);
            }
            else if (Online == false)
            {
                //CaptureMesg(name, "has_error", 0, 0, 0, 0, 0, 0, 0, 0);
                Data2Excel(name, "无版型", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "has_error", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,0,0,0,0,0,0,0,0,0,0, 0, 0, 0, 0, 0, 0,0,0);
            }
            
            //界面信息显示的列表
            var tuple = Tuple.Create("err", time_consuming, 0.0, 0.0, 0.0, 0.0, 0.0,"0");
            flag_list.Add(tuple);
            

            Timers[measure.count - 1] = new System.Timers.Timer();
            Timers[measure.count - 1].Interval = Time_Delay - (int)time_consuming; //延时显示
            Timers[measure.count - 1].Elapsed += timer1_Tick;
            Timers[measure.count - 1].AutoReset = false; //计时器只运行一次
            Timers[measure.count - 1].Enabled = true;
            Timers[measure.count - 1].Start();
        }



        //砖处理结果汇总
        private void result_show(Mat img_show, double time_consuming, string name, double HSV_H, double LAB_L, double LAB_A, double LAB_B, int[] Hash_Code, int[] Hash_Code_rotate, double BGR_B,double BGR_G,double BGR_R,Mat DstImg)
        {
            //if (Online == true)  //只有在线检测的时候才会自动调整相机的曝光时间
            //{
            //    if (measure.Brightness_Judgment_Once == true)  //对图像的曝光进行第一次调整，改变亮度
            //    {
            //        if (measure.ExposureValue <= 180)
            //        {
            //            form4.m_AcqDevice.SetFeatureValue("ExposureTime", measure.ExposureValue);
            //            measure.Brightness_Judgment_Once = false;
            //        }
            //        else
            //        {
            //            MessageBox.Show("曝光值设置范围不得超过180");
            //        }

            //    }

            //    if (measure.Brightness_Judgment_Twice == true)  //对图像的曝光进行第二次调整，改变亮度
            //    {
            //        if (measure.ExposureValue <= 180)
            //        {
            //            form4.m_AcqDevice.SetFeatureValue("ExposureTime", measure.ExposureValue);
            //            measure.Brightness_Judgment_Twice = false;
            //        }
            //        else
            //        {
            //            MessageBox.Show("曝光值设置范围不得超过180");
            //        }

            //    }

            //    if (measure.Brightness_Judgment_Third == true)  //对图像的曝光进行第二次调整，改变亮度
            //    {
            //        if (measure.ExposureValue <= 180)
            //        {
            //            form4.m_AcqDevice.SetFeatureValue("ExposureTime", measure.ExposureValue);
            //            measure.Brightness_Judgment_Third = false;
            //        }
            //        else
            //        {
            //            MessageBox.Show("曝光值设置范围不得超过180");
            //        }

            //    }
            //}
            bool abnormal_pattern = false;       //版型匹配失常的标志位

            Return_Info_calibration.is_start_calibration = true;      //进入可以开始标定的状态
            has_chromatic_aberration = false;


            if (Info_calibration.is_calibration == true && Info_calibration.flag_calibration == true)  //如果点击标定按钮，则重新计数
            {
                result_count = 0;
                Info_calibration.flag_calibration = false;    //防止每次运行result_show方法时计数都被置零

            }

            result_count++;
            Return_Info_calibration.count = Info_calibration.Total - result_count;     //返回还剩余未标定砖的数量


            if (result_count == 1)   //第一块砖的信息让他进来，这样可以和first_Ceramics对得上
            {

                Delta_E_pure = Get_Delta_E_pure(LAB_L, LAB_A, LAB_B);               //纯色砖的色度计算方式
                Delta_E_texture = Get_Delta_E_texture(LAB_L, LAB_A, LAB_B);         //非纯色砖的色度计算方式
                
                num_Delta_E++;

                sum_HSV_H += HSV_H;
                sum_LAB_L += LAB_L;
                sum_LAB_A += LAB_A;
                sum_LAB_B += LAB_B;
                sum_BGR_B += BGR_B;
                sum_BGR_G += BGR_G;
                sum_BGR_R += BGR_R;
                sum_Delta_E_pure += Delta_E_pure;
                sum_Delta_E_texture += Delta_E_texture;
            }
            else if (result_count >= 2)
            {

                Delta_E_pure = Get_Delta_E_pure(LAB_L, LAB_A, LAB_B);               //纯色砖的色度计算方式
                Delta_E_texture = Get_Delta_E_texture(LAB_L, LAB_A, LAB_B);         //非纯色砖的色度计算方式


                //之所以色度和亮度的搜集条件这么宽松，是为了防止第一块砖如果是试抛或者异常砖，导致后面的砖都无法正常搜集
                if (Math.Abs(Delta_E_pure - first_Delta_E) <= 50 * sensitivity_color && Math.Abs(LAB_L - first_LAB_L) <= 200 * sensitivity_luminance)
                {
                    num_Delta_E++;

                    sum_HSV_H += HSV_H;
                    sum_LAB_L += LAB_L;
                    sum_LAB_A += LAB_A;
                    sum_LAB_B += LAB_B;
                    sum_BGR_B += BGR_B;
                    sum_BGR_G += BGR_G;
                    sum_BGR_R += BGR_R;
                    sum_Delta_E_pure += Delta_E_pure;
                    sum_Delta_E_texture += Delta_E_texture;

                }

            }



            //砖版型的在线判断并搜集,前提是前几块砖的版型复杂程度判断完成
            if (measure.Pattern_Judgment == true && result_count >= 1)
            {

                if (first_Ceramics == true)
                {
                    first_Ceramics = false;
                    pattern = "版型1";            //第一次处理时，模板匹配的版型设置为版型1
                    var tuple_info = Tuple.Create("版型1", Hash_Code, Hash_Code_rotate, 1, new Tuple<double, double, double, double, double, double>(Delta_E_pure, Delta_E_pure, Delta_E_texture, Delta_E_texture, HSV_H, HSV_H), new Tuple<double, double, double, double, double, double>(LAB_L, LAB_L, LAB_A, LAB_A, LAB_B, LAB_B), new Tuple<double, double, double, double, double, double,Mat>(BGR_B, BGR_B, BGR_G, BGR_G, BGR_R, BGR_R,DstImg));
                    first_Delta_E = Delta_E_pure;
                    first_LAB_L = LAB_L;
                    Ceramics_info_list.Add(tuple_info);

                }
                else
                {
                    int num = 0;
                    foreach (var tuple_infos in Ceramics_info_list)
                    {
                        int Hamming_Distance = Get_Hamming_Distance(tuple_infos.Item2, Hash_Code);
                        int Hamming_Distance_rotate = Get_Hamming_Distance(tuple_infos.Item3, Hash_Code);

                        //砖型复杂系数的二次判定
                        num_hash++;
                        Hamming_Distances += Hamming_Distance;
                        list_Hamming_Distance.Add(Hamming_Distance);
                        if (num_hash == 16)
                        {
                            double k = 0.1;
                            double sigma = GetSigma(list_Hamming_Distance);
                            if (sigma < 200 && sigma_LAB_L < 2)
                            {
                                k = -0.3;
                                use_cat = false;          //确定是纯色砖的色度计算公式只采用纯色的公式

                            }
                            else if (sigma >= 200 && sigma < 400 && sigma_LAB_L < 2)
                            {
                                k = -0.2;
                                use_cat = false;

                            }
                            else if (sigma >= 400 && sigma < 800)
                            {
                                k = 0.03;

                            }
                            else if (sigma >= 800 && sigma < 1200)
                            {
                                k = 0.06;

                            }
                            else if (sigma >= 1200 && sigma < 1600)
                            {
                                k = 0.1;

                            }
                            else if (sigma >= 1600)
                            {
                                k = 0.15;

                            }
                            if (sigma_LAB_L > 5 && measure.Coefficient >= 20)
                            {
                                Complex_patterns = true;      //表示当前砖面的花纹复杂
                            }

                            Coefficient = (Hash_Code.Length / (Hamming_Distances / num_hash));
                            if (Coefficient <= 30)
                            {
                                Coefficient = (Coefficient + k * measure.Coefficient);

                                if (Coefficient < 0) { Coefficient = 1.5; }
                            }
                            Pattern_ok = true;
                        }


                        //如果汉明距离小于哈希矩阵数量的1/6，则认为两块砖的版型接近
                        if ((Hamming_Distance < Hash_Code.Length / Coefficient || Hamming_Distance_rotate < Hash_Code.Length / Coefficient) && Pattern_ok == true)
                        { 
                            pattern = tuple_infos.Item1; //获取当前所匹配到的版型

                            if (start_Pattern_Matching == true) //由于标定时会使用标准色号的砖型，因此不通过版型区分
                            {
                                // 定义两个颜色的 Lab 值
                                var lab_current = new LabColor(LAB_L, LAB_A, LAB_B);     //当前砖的L,A,B通道数值
                                var lab_templeate = new LabColor(tuple_infos.Item6.Item2, tuple_infos.Item6.Item4, tuple_infos.Item6.Item6);                                  //对应模板砖的L,A,B通道数值


                                //计算空间中向量的夹角，来表示色差
                                cosTheta = Calculate_angle(lab_current,lab_templeate);


                                //启动模板匹配时，CIE76版本的色差计算方式
                                diff_Delta_E_pure = Delta_E_pure - tuple_infos.Item5.Item2;


                                //只有1个色号，且角度差距大于0.2时，计算CIE76色差时会考虑角度的影响
                                if (Math.Abs(diff_Delta_E_pure) <0.3 && cosTheta > 0.2 && list_lab.Count == 0)
                                {
                                    diff_Delta_E_pure = Delta_E_pure * Math.Pow(cosTheta, 0.2) - tuple_infos.Item5.Item2;
                                }


                                //当出现2号色后，对出现的所有色号都进行E的比较，并累加求和
                                if (list_lab.Count > 0)
                                {
                                    foreach (var lt_lab in list_lab)
                                    {
                                        if (lt_lab.Item1 == pattern)
                                        {
                                            var lab_select = new LabColor(lt_lab.Item2, lt_lab.Item3, lt_lab.Item4);

                                            if (Math.Abs(diff_Delta_E_pure) < 0.3 && cosTheta > 0.2)    
                                            {
                                                diff_Delta_E_pure = Delta_E_pure * Math.Pow(cosTheta, 0.2) - tuple_infos.Item5.Item2;
                                            }
                                        }

                                    }
                                }


                                diff_Delta_E_texture = Delta_E_texture - tuple_infos.Item5.Item4;
                                diff_HSV_H = HSV_H - tuple_infos.Item5.Item6;
                                diff_LAB_L = LAB_L - tuple_infos.Item6.Item2;
                                diff_LAB_A = LAB_A - tuple_infos.Item6.Item4;
                                diff_LAB_B = LAB_B - tuple_infos.Item6.Item6;
                                diff_BGR_B = BGR_B - tuple_infos.Item7.Item2;
                                diff_BGR_G = BGR_G - tuple_infos.Item7.Item4;
                                diff_BGR_R = BGR_R - tuple_infos.Item7.Item6;

                              

                                // 利用CIEDE2000公式计算色差
                                deltaE = CalculateCIEDE2000(lab_current, lab_templeate);


                                //基于CIEDE2000公式对出现的所有色号都进行E的比较，并累加求和
                                //if (list_lab.Count > 0)
                                //{
                                //    foreach (var lt_lab in list_lab)
                                //    {
                                //        if (lt_lab.Item1 == pattern)
                                //        {
                                //            var lab_select = new LabColor(lt_lab.Item2, lt_lab.Item3, lt_lab.Item4);

                                //            if (CalculateCIEDE2000(lab_current, lab_select) > 0)    //只有当差距大于某一阈值再进行累加，防止颜色的缓变，导致颜色越开越多，但也有矛盾，可能会导致一些E值表现不明显但现场分出色号的砖分不出来
                                //            {
                                //                deltaE += CalculateCIEDE2000(lab_current, lab_select);
                                //            }
                                //        }

                                //    }
                                //}


                                //通过匹配到模版，在RGB图像的基础上先差分，再求和算均值
                                Mat[] dst = new Mat[3];
                                Mat[] template = new Mat[3];
                                
                                Cv2.Split(DstImg, out dst);
                                Cv2.Split(tuple_infos.Item7.Item7, out template);

                                Mat dst_sub_temp_r = new Mat(DstImg.Size(),MatType.CV_8UC1);
                                Mat dst_sub_temp_g = new Mat(DstImg.Size(), MatType.CV_8UC1);
                                Mat dst_sub_temp_b = new Mat(DstImg.Size(), MatType.CV_8UC1);

                                Mat temp_sub_dst_r = new Mat(DstImg.Size(), MatType.CV_8UC1);
                                Mat temp_sub_dst_g = new Mat(DstImg.Size(), MatType.CV_8UC1);
                                Mat temp_sub_dst_b = new Mat(DstImg.Size(), MatType.CV_8UC1);

                                Cv2.Blur(dst[0], dst[0], new OpenCvSharp.Size(201, 201));
                                Cv2.Blur(dst[1], dst[1], new OpenCvSharp.Size(201, 201));
                                Cv2.Blur(dst[2], dst[2], new OpenCvSharp.Size(201, 201));
                                Cv2.Blur(template[0], template[0], new OpenCvSharp.Size(201, 201));
                                Cv2.Blur(template[1], template[1], new OpenCvSharp.Size(201, 201));
                                Cv2.Blur(template[2], template[2], new OpenCvSharp.Size(201, 201));

                                Cv2.Subtract(dst[0], template[0], dst_sub_temp_b);
                                Cv2.Subtract(dst[1], template[1], dst_sub_temp_g);
                                Cv2.Subtract(dst[2], template[2], dst_sub_temp_r);

                                Cv2.Subtract(template[0], dst[0], temp_sub_dst_b);
                                Cv2.Subtract(template[1], dst[1], temp_sub_dst_g);
                                Cv2.Subtract(template[2], dst[2], temp_sub_dst_r);


                                //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\dst_sub_temp_b.jpg", dst_sub_temp_b);
                                //double min, max, min1, max1;
                                //Cv2.MinMaxIdx(dst_sub_temp_b, out min, out max);
                                //Cv2.MinMaxIdx(temp_sub_dst_b, out min1, out max1);


                                //通过RGB通道的差值，来计算色差
                                diff_B = ((double)Cv2.Mean(dst_sub_temp_b) + (double)Cv2.Mean(dst_sub_temp_g) + (double)Cv2.Mean(dst_sub_temp_r));
                                diff_G = ((double)Cv2.Mean(temp_sub_dst_b) + (double)Cv2.Mean(temp_sub_dst_g) + (double)Cv2.Mean(temp_sub_dst_r));
                                diff_R = Math.Max((double)Cv2.Mean(dst_sub_temp_b), (double)Cv2.Mean(temp_sub_dst_b))
                                       + Math.Max((double)Cv2.Mean(dst_sub_temp_g), (double)Cv2.Mean(temp_sub_dst_g))
                                       + Math.Max((double)Cv2.Mean(dst_sub_temp_r), (double)Cv2.Mean(temp_sub_dst_r));
                               
                            }


                            //更新当前版型的信息
                            int number = tuple_infos.Item4 + 1;
                            double sum_Delta_E_pure = tuple_infos.Item5.Item1 + Delta_E_pure;
                            double sum_Delta_E_texture = tuple_infos.Item5.Item3 + Delta_E_texture;
                            double sum_HSV_H = tuple_infos.Item5.Item5 + HSV_H;
                            double sum_LAB_L = tuple_infos.Item6.Item1 + LAB_L;
                            double sum_LAB_A = tuple_infos.Item6.Item3 + LAB_A;
                            double sum_LAB_B = tuple_infos.Item6.Item5 + LAB_B;
                            double sum_BGR_B = tuple_infos.Item7.Item1 + BGR_B;
                            double sum_BGR_G = tuple_infos.Item7.Item3 + BGR_G;
                            double sum_BGR_R = tuple_infos.Item7.Item5 + BGR_R;

                            double mean_Delta_E_pure_temp = sum_Delta_E_pure / number;
                            double mean_Delta_E_texture_temp = sum_Delta_E_texture / number;
                            double mean_HSV_H_temp = sum_HSV_H / number;
                            double mean_LAB_L_temp = sum_LAB_L / number;
                            double mean_LAB_A_temp = sum_LAB_A / number;
                            double mean_LAB_B_temp = sum_LAB_B / number;
                            double mean_BGR_B_temp = sum_BGR_B / number;
                            double mean_BGR_G_temp = sum_BGR_G / number;
                            double mean_BGR_R_temp = sum_BGR_R / number;


                            if (Math.Abs(diff_Delta_E_pure) <= 10 * sensitivity_color && Math.Abs(diff_Delta_E_texture) <= 10 * sensitivity_color && Math.Abs(diff_LAB_L) <= 20 * sensitivity_luminance && is_stable == false) //对于有色差的砖不加入列表，同时如果砖的色号标准稳定后，则不再改动Ceramics_info_list列表
                            {
                                Ceramics_info_list.RemoveAt(num);
                                var tuple_info = Tuple.Create(tuple_infos.Item1, tuple_infos.Item2, tuple_infos.Item3, number, new Tuple<double, double, double, double,double,double>(sum_Delta_E_pure, mean_Delta_E_pure_temp, sum_Delta_E_texture, mean_Delta_E_texture_temp,sum_HSV_H,mean_HSV_H_temp), new Tuple<double, double, double, double, double, double>(sum_LAB_L, mean_LAB_L_temp, sum_LAB_A, mean_LAB_A_temp, sum_LAB_B, mean_LAB_B_temp), new Tuple<double, double, double, double, double, double,Mat>(sum_BGR_B, mean_BGR_B_temp, sum_BGR_G, mean_BGR_G_temp, sum_BGR_R, mean_BGR_R_temp,tuple_infos.Item7.Item7));
                                Ceramics_info_list.Add((tuple_info));
                            }


                            break;   //匹配上了就跳出循环
                        }


                        num++;

                        if (num == Ceramics_info_list.Count && is_stable == false && Pattern_ok == true)
                        {
                            //如果轮询当前版型数据集后没有相似的，则是新版型，增加进数据集中

                            string type = "版型" + (Ceramics_info_list.Count + 1).ToString();
                            pattern = type;

                            if (start_Pattern_Matching == true)  //由于标定时会使用标准色号的砖型，因此不通过版型区分
                            {
                                // 定义两个颜色的 Lab 值
                                var lab_current = new LabColor(LAB_L, LAB_A, LAB_B);     //当前砖的L,A,B通道数值
                                var lab_templeate = new LabColor(mean_LAB_L, mean_LAB_A, mean_LAB_B);                                                //对应模板砖的L,A,B通道数值


                                //计算空间中向量的夹角，来表示色差
                                cosTheta = Calculate_angle(lab_current, lab_templeate);


                                //启动模板匹配时，CIE76版本的色差计算方式
                                diff_Delta_E_pure = Delta_E_pure - mean_Delta_E_pure;


                                //只有1个色号，且角度差距大于0.2时，计算CIE76色差时会考虑角度的影响
                                if (Math.Abs(diff_Delta_E_pure) < 0.3 && cosTheta > 0.2 && list_lab.Count == 0)
                                {
                                    diff_Delta_E_pure = Delta_E_pure * Math.Pow(cosTheta, 0.2) - mean_Delta_E_pure;
                                }


                                //当出现2号色后，对出现的所有色号都进行E的比较，并累加求和
                                if (list_lab.Count > 0)
                                {
                                    foreach (var lt_lab in list_lab)
                                    {
                                        if (lt_lab.Item1 == pattern)
                                        {
                                            var lab_select = new LabColor(lt_lab.Item2, lt_lab.Item3, lt_lab.Item4);

                                            if (Math.Abs(diff_Delta_E_pure) < 0.3 && cosTheta > 0.2) 
                                            {
                                                diff_Delta_E_pure = Delta_E_pure * Math.Pow(cosTheta, 0.2) - mean_Delta_E_pure;
                                            }
                                        }

                                    }
                                }


                                diff_Delta_E_texture = Delta_E_texture - mean_Delta_E_texture;
                                diff_HSV_H = HSV_H - mean_HSV_H;
                                diff_LAB_L = LAB_L - mean_LAB_L;
                                diff_LAB_A = LAB_A - mean_LAB_A;
                                diff_LAB_B = LAB_B - mean_LAB_B;
                                diff_BGR_B = BGR_B - mean_BGR_B;
                                diff_BGR_G = BGR_G - mean_BGR_G;
                                diff_BGR_R = BGR_R - mean_BGR_R;


                                // 计算 CIEDE2000 色差
                                deltaE = CalculateCIEDE2000(lab_current, lab_templeate);


                                diff_B = 0;
                                diff_G = 0;
                                diff_R = 0;

                            }


                            if (Math.Abs(diff_Delta_E_pure) <= 10 * sensitivity_color && Math.Abs(diff_Delta_E_texture) <= 10 * sensitivity_color && Math.Abs(diff_LAB_L) <= 20 * sensitivity_luminance && is_stable == false)
                            {
                                var tuple_info_new = Tuple.Create(type, Hash_Code, Hash_Code_rotate, 1, new Tuple<double, double, double, double,double,double>(Delta_E_pure, Delta_E_pure, Delta_E_texture, Delta_E_texture,HSV_H,HSV_H), new Tuple<double, double, double, double, double, double>(LAB_L, LAB_L, LAB_A, LAB_A, LAB_B, LAB_B), new Tuple<double, double, double, double, double, double,Mat>(BGR_B, BGR_B, BGR_G, BGR_G, BGR_R, BGR_R,DstImg));
                                Ceramics_info_list.Add(tuple_info_new);
                            }
                            
                            break;
                        }
                        else if (num == Ceramics_info_list.Count && is_stable == true && Pattern_ok == true)
                        {
                            //版型稳定后，如果版型未匹配成功则不新增

                            pattern = "版型0";
                            abnormal_pattern = true;

                            if (start_Pattern_Matching == true)
                            {
                                // 定义两个颜色的 Lab 值
                                var lab_current = new LabColor(LAB_L, LAB_A, LAB_B);     //当前砖的L,A,B通道数值
                                var lab_templeate = new LabColor(mean_LAB_L, mean_LAB_A, mean_LAB_B);                                                //对应模板砖的L,A,B通道数值


                                //计算空间中向量的夹角，来表示色差
                                cosTheta = Calculate_angle(lab_current, lab_templeate);


                                //启动模板匹配时，CIE76版本的色差计算方式
                                diff_Delta_E_pure = Delta_E_pure - mean_Delta_E_pure;


                                //只有1个色号，且角度差距大于0.2时，计算CIE76色差时会考虑角度的影响
                                if (Math.Abs(diff_Delta_E_pure) < 0.3 && cosTheta > 0.2 && list_lab.Count == 0)
                                {
                                    diff_Delta_E_pure = Delta_E_pure * Math.Pow(cosTheta, 0.2) - mean_Delta_E_pure;
                                }


                                //当出现2号色后，对出现的所有色号都进行E的比较，并累加求和
                                if (list_lab.Count > 0)
                                {
                                    foreach (var lt_lab in list_lab)
                                    {
                                        if (lt_lab.Item1 == pattern)
                                        {
                                            var lab_select = new LabColor(lt_lab.Item2, lt_lab.Item3, lt_lab.Item4);

                                            if (Math.Abs(diff_Delta_E_pure) < 0.3 && cosTheta > 0.2)
                                            {
                                                diff_Delta_E_pure = Delta_E_pure * Math.Pow(cosTheta, 0.2) - mean_Delta_E_pure;
                                            }
                                        }

                                    }
                                }


                                diff_Delta_E_texture = Delta_E_texture - mean_Delta_E_texture;
                                diff_HSV_H = HSV_H - mean_HSV_H;
                                diff_LAB_L = LAB_L - mean_LAB_L;
                                diff_LAB_A = LAB_A - mean_LAB_A;
                                diff_LAB_B = LAB_B - mean_LAB_B;
                                diff_BGR_B = BGR_B - mean_BGR_B;
                                diff_BGR_G = BGR_G - mean_BGR_G;
                                diff_BGR_R = BGR_R - mean_BGR_R;


                                // 计算 CIEDE2000 色差
                                deltaE = CalculateCIEDE2000(lab_current, lab_templeate);


                                diff_B = 0;
                                diff_G = 0;
                                diff_R = 0;

                                

                            }


                            break;
                        }
                    }


                }
            }



            //计算前1-16块砖的均值和方差
            if (result_count >= 1 && result_count <= 16)
            {
                list_LAB_A.Add(LAB_A);
                list_LAB_B.Add(LAB_B);
                list_LAB_L.Add(LAB_L);
            }


            // 计算标定砖块的均值色度，目前标定未要求凑齐不同版型，所以用的diff_Delta_E = Delta_E - mean_Delta_E来计算色差
            if (result_count == Info_calibration.Total && Info_calibration.is_calibration == true)
            {
                sigma_LAB_A = 0; sigma_LAB_B = 0; sigma_LAB_L = 0;

                mean_LAB_L = sum_LAB_L / (result_count - 9);
                mean_LAB_A = sum_LAB_A / (result_count - 9);
                mean_LAB_B = sum_LAB_B / (result_count - 9);
                mean_Delta_E_pure = sum_Delta_E_pure / (result_count - 9);
                mean_Delta_E_texture = sum_Delta_E_texture / (result_count - 9);
                sigma_LAB_A = GetSigma(list_LAB_A);
                sigma_LAB_B = GetSigma(list_LAB_B);
                sigma_LAB_L = GetSigma(list_LAB_L);

                Return_Info_calibration.is_complete = true;
                Return_Info_calibration.is_start_calibration = false;

                start_Pattern_Matching = false;     //由于标定未要求凑齐不同版面，所以不启动模板匹配功能
            }


            // 计算前16块的均值色度
            if (result_count == 16 && Info_calibration.is_calibration == false)
            {
                sigma_LAB_A = 0; sigma_LAB_B = 0; sigma_LAB_L = 0;

                mean_HSV_H = sum_HSV_H / num_Delta_E;
                mean_LAB_L = sum_LAB_L / num_Delta_E;
                mean_LAB_A = sum_LAB_A / num_Delta_E;
                mean_LAB_B = sum_LAB_B / num_Delta_E;
                mean_BGR_B = sum_BGR_B / num_Delta_E;
                mean_BGR_G = sum_BGR_G / num_Delta_E;
                mean_BGR_R = sum_BGR_R / num_Delta_E;
                mean_Delta_E_pure = sum_Delta_E_pure / num_Delta_E;
                mean_Delta_E_texture = sum_Delta_E_texture / num_Delta_E;

                sigma_LAB_A = GetSigma(list_LAB_A);
                sigma_LAB_B = GetSigma(list_LAB_B);
                sigma_LAB_L = GetSigma(list_LAB_L);

            }



            // 计算前当前砖的色差与平均值的差异来对应色号
            if ((result_count >= 16 && Info_calibration.is_calibration == false) || (result_count > Info_calibration.Total && Info_calibration.is_calibration == true))
            {

                //是否启动版型匹配下的色差计算
                if (start_Pattern_Matching == false)
                {
                    diff_Delta_E_pure = Delta_E_pure - mean_Delta_E_pure;   //启动模板匹配时的色差计算公式
                    diff_Delta_E_texture = Delta_E_texture - mean_Delta_E_texture;
                    diff_HSV_H = HSV_H - mean_HSV_H;
                    diff_LAB_L = LAB_L - mean_LAB_L;
                    diff_LAB_A = LAB_A - mean_LAB_A;
                    diff_LAB_B = LAB_B - mean_LAB_B;
                    diff_BGR_B = BGR_B - mean_BGR_B;
                    diff_BGR_G = BGR_G - mean_BGR_G;
                    diff_BGR_R = BGR_R - mean_BGR_R;


                    // 定义两个颜色的 Lab 值
                    var lab_current = new LabColor(LAB_L, LAB_A, LAB_B);
                    var lab_templeate = new LabColor(mean_LAB_L, mean_LAB_A, mean_LAB_B);

                    // 计算 CIEDE2000 色差
                    deltaE = CalculateCIEDE2000(lab_current, lab_templeate);


                    diff_B = 0;
                    diff_G = 0;
                    diff_R = 0;

                    cosTheta = Math.Acos((LAB_L * mean_LAB_L + LAB_A * mean_LAB_A + LAB_B * mean_LAB_B) / (Math.Sqrt(LAB_L * LAB_L + LAB_A * LAB_A + LAB_B * LAB_B) * Math.Sqrt(mean_LAB_L * mean_LAB_L + mean_LAB_A * mean_LAB_A + mean_LAB_B * mean_LAB_B))) * 180 / Math.PI + 1;

                }

                if (Is_Template_Matching == true)  //根据界面上是否选中模板匹配的按钮转态来确定是否启动该功能
                {
                    start_Pattern_Matching = true;
                }
                else if (Is_Template_Matching == false)
                {
                    start_Pattern_Matching = false;
                }



                if (use_cat == false)    //纯色砖的情况
                {
                    diff_Delta_E = diff_Delta_E_pure;
                }
                else                //非纯色砖，两个公式都有，谁效果好用谁
                {
                    if (Math.Abs(diff_Delta_E_pure) >= Math.Abs(diff_Delta_E_texture)) { diff_Delta_E = diff_Delta_E_pure; }
                    if (Math.Abs(diff_Delta_E_pure) < Math.Abs(diff_Delta_E_texture)) { diff_Delta_E = diff_Delta_E_pure; if (sigma_LAB_L >= 0.6 || measure.Coefficient >= 14) { diff_Delta_E = diff_Delta_E_pure; } }     //对于有纹理的计算公式要慎重，所以加了些条件限制
                }

                if (Complex_patterns == true)       //纹理复杂的情况
                {
                    diff_Delta_E = diff_Delta_E_pure;
                }


                //if ((diff_LAB_A * diff_LAB_B) < 0 && Math.Abs(diff_LAB_A) > 0.05 && Math.Abs(diff_LAB_B) > 0.05)
                //{

                //    diff_Delta_E = diff_Delta_E_texture;
                //}
                //else
                //{

                //    diff_Delta_E = diff_Delta_E_pure;
                //}

                double pattern_count = Ceramics_info_list.Count;

                //根据砖面的色系和花纹复杂度，动态的调整开色阈值和区间范围
                //if (measure.Average_gray < 100 && sigma_LAB_L < 2) { diff_Delta_E *= 3; }
                //else if (measure.Average_gray < 100 && sigma_LAB_L < 0.5 && measure.Coefficient <= 16) { diff_Delta_E *= 3; }         //对于比较暗色系纹理不复杂的砖容易出现色差，且本身比较暗，色度值容易被拉低
                //else if (measure.Average_gray < 200 && sigma_LAB_L < 5 && measure.Coefficient <= 26 && measure.Coefficient >= 6 && pattern_count <= 4) { diff_Delta_E *= 3; }
                //else if (measure.Average_gray < 200 && sigma_LAB_L < 5 && measure.Coefficient <= 26 && measure.Coefficient >= 6 && pattern_count > 4) { diff_Delta_E *= 2.3; }
                //else if (measure.Average_gray < 180 && sigma_LAB_L < 0.3 && measure.Coefficient <= 6) { diff_Delta_E *= 3; }

                if (sigma_LAB_L >= 5 && measure.Coefficient > 26) { diff_Delta_E /= 3; }         //对于纹理复杂的砖型，要减弱影响

                if (pattern == "版型0") { diff_LAB_L *= 2; }    //对于变化不大的试抛砖，提升亮度的差值，让其色号显示为0

                //diff_Delta_E /= 10;
                //利用E2000的公式来进行色号的判断
                //diff_Delta_E = deltaE;

                bool is_judge_color = false;   //如果判断过亮度通道信息异常后则不再次判断颜色通道信息,可能会冲突或导致色号增加异常
                //亮度通道的判断
                if (diff_LAB_L >= -10 * sensitivity_luminance)
                {
                    Whitening = true;

                    if (diff_LAB_L <= 10 * sensitivity_luminance)
                    {

                        //亮度差异不大，不做任何操作

                    }
                    else if ((diff_LAB_L > 10 * sensitivity_luminance && diff_LAB_L <= 20 * sensitivity_luminance))
                    {

                        count_color13++;

                        if (count_color13 >= 1000)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first13 = false;
                                temp_color_num13 = 1;
                            }
                            else
                            {
                                if (is_first13 == true)
                                {
                                    is_first13 = false;
                                    color_num_max++;
                                    temp_color_num13 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num13;
                        }
                        else
                        {
                            color_num = 0;
                        }
                        if (Is_Use_Factory_Custom == true)
                        {
                            is_judge_color = true;
                        }

                    }
                    else if ((diff_LAB_L > 20 * sensitivity_luminance && diff_LAB_L <= 30 * sensitivity_luminance))
                    {
                        count_color14++;

                        if (count_color14 >= 1000)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first14 = false;
                                temp_color_num14 = 1;
                            }
                            else
                            {
                                if (is_first14 == true)
                                {
                                    is_first14 = false;
                                    color_num_max++;
                                    temp_color_num14 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num14;
                        }
                        else
                        {
                            color_num = 0;
                        }
                        if (Is_Use_Factory_Custom == true)
                        {
                            is_judge_color = true;
                        }

                    }
                    else if ((diff_LAB_L > 30 * sensitivity_luminance && diff_LAB_L <= 40 * sensitivity_luminance))
                    {

                        count_color15++;

                        if (count_color15 >= 1000)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first15 = false;
                                temp_color_num15 = 1;
                            }
                            else
                            {
                                if (is_first15 == true)
                                {
                                    is_first15 = false;
                                    color_num_max++;
                                    temp_color_num15 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num15;
                        }
                        else
                        {
                            color_num = 0;
                        }
                        if (Is_Use_Factory_Custom == true)
                        {
                            is_judge_color = true;
                        }

                    }
                    else if ((diff_LAB_L > 40 * sensitivity_luminance && diff_LAB_L <= 50 * sensitivity_luminance))
                    {

                        count_color16++;

                        if (count_color16 >= 1000)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first16 = false;
                                temp_color_num16 = 1;
                            }
                            else
                            {
                                if (is_first16 == true)
                                {
                                    is_first16 = false;
                                    color_num_max++;
                                    temp_color_num16 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num16;
                        }
                        else
                        {
                            color_num = 0;
                        }
                        if (Is_Use_Factory_Custom == true)
                        {
                            is_judge_color = true;
                        }

                    }
                    else
                    {

                        count_color17++;

                        if (count_color17 >= 1000)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first17 = false;
                                temp_color_num17 = 1;
                            }
                            else
                            {
                                if (is_first17 == true)
                                {
                                    is_first17 = false;
                                    color_num_max++;
                                    temp_color_num17 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num17;
                        }
                        else
                        {
                            color_num = 0;
                        }
                        if (Is_Use_Factory_Custom == true)
                        {
                            is_judge_color = true;
                        }
                    }
                }
                else if (diff_LAB_L < -10 * sensitivity_luminance)
                {
                    diff_LAB_L = Math.Abs(diff_LAB_L);
                    Whitening = false;

                    
                    if ((diff_LAB_L > 10 * sensitivity_luminance && diff_LAB_L <= 20 * sensitivity_luminance))
                    {

                        count_color18++;

                        if (count_color18 >= 1000)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first18 = false;
                                temp_color_num18 = 1;
                            }
                            else
                            {
                                if (is_first18 == true)
                                {
                                    is_first18 = false;
                                    color_num_max++;
                                    temp_color_num18 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num18;
                        }
                        else
                        {
                            color_num = 0;
                        }
                        if (Is_Use_Factory_Custom == true)
                        {
                            is_judge_color = true;
                        }

                    }
                    else if ((diff_LAB_L > 20 * sensitivity_luminance && diff_LAB_L <= 30 * sensitivity_luminance))
                    {

                        count_color19++;

                        if (count_color19 >= 1000)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first19 = false;
                                temp_color_num19 = 1;
                            }
                            else
                            {
                                if (is_first19 == true)
                                {
                                    is_first19 = false;
                                    color_num_max++;
                                    temp_color_num19 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num19;
                        }
                        else
                        {
                            color_num = 0;
                        }
                        if (Is_Use_Factory_Custom == true)
                        {
                            is_judge_color = true;
                        }

                    }
                    else if ((diff_LAB_L > 30 * sensitivity_luminance && diff_LAB_L <= 40 * sensitivity_luminance))
                    {
                        count_color20++;

                        if (count_color20 >= 1000)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first20 = false;
                                temp_color_num20 = 1;
                            }
                            else
                            {
                                if (is_first20 == true)
                                {
                                    is_first20 = false;
                                    color_num_max++;
                                    temp_color_num20 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num20;
                        }
                        else
                        {
                            color_num = 0;
                        }
                        if (Is_Use_Factory_Custom == true)
                        {
                            is_judge_color = true;
                        }

                    }
                    else if ((diff_LAB_L > 40 * sensitivity_luminance && diff_LAB_L <= 50 * sensitivity_luminance))
                    {

                        count_color21++;

                        if (count_color21 >= 1000)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first21 = false;
                                temp_color_num21 = 1;
                            }
                            else
                            {
                                if (is_first21 == true)
                                {
                                    is_first21 = false;
                                    color_num_max++;
                                    temp_color_num21 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num21;
                        }
                        else
                        {
                            color_num = 0;
                        }
                        if (Is_Use_Factory_Custom == true)
                        {
                            is_judge_color = true;
                        }

                    }
                    else if ((diff_LAB_L > 50 * sensitivity_luminance && diff_LAB_L <= 60 * sensitivity_luminance))
                    {

                        count_color22++;

                        if (count_color22 >= 1000)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first22 = false;
                                temp_color_num22 = 1;
                            }
                            else
                            {
                                if (is_first22 == true)
                                {
                                    is_first22 = false;
                                    color_num_max++;
                                    temp_color_num22 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num22;
                        }
                        else
                        {
                            color_num = 0;
                        }
                        if (Is_Use_Factory_Custom == true)
                        {
                            is_judge_color = true;
                        }

                    }
                    else
                    {
                        count_color23++;

                        if (count_color23 >= 1000)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first23 = false;
                                temp_color_num23 = 1;
                            }
                            else
                            {
                                if (is_first23 == true)
                                {
                                    is_first23 = false;
                                    color_num_max++;
                                    temp_color_num23 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num23;
                        }
                        else
                        {
                            color_num = 0;
                        }
                        if (Is_Use_Factory_Custom == true)
                        {
                            is_judge_color = true;
                        }

                    }
                }



                //颜色通道信息的判断
                if (diff_Delta_E >= -0.5 * sensitivity_color && is_judge_color == false)
                {
                    Cyan = false;

                    if (diff_Delta_E <= 0.5 * sensitivity_color)
                    {

                        count_color1++;

                        if (count_color1 >= 1)   //出现次数大于5次以上再开色号，防止试抛砖的影响
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first1 = false;
                                temp_color_num1 = 1;
                            }
                            else
                            {
                                if (is_first1 == true)
                                {
                                    is_first1 = false;
                                    color_num_max++;
                                    temp_color_num1 = color_num_max;
                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num1;
                        }
                        else
                        {
                            color_num = 0;
                        }

                    }
                    else if ((diff_Delta_E > 0.5 * sensitivity_color && diff_Delta_E <= 1.2 * sensitivity_color))
                    {

                        count_color2++;
                        if ((result_count % 20) == 0)
                        {
                            if (count_color2 > 16) { count_color2 = 20; }
                            else { count_color2 = 0; }
                        }

                        if (count_color2 >= 20)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first2 = false;
                                temp_color_num2 = 1;
                            }
                            else
                            {
                                if (is_first2 == true)
                                {
                                    is_first2 = false;
                                    color_num_max++;
                                    temp_color_num2 = color_num_max;

                                    list_lab.Add(new Tuple<string, double, double, double,int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num2));

                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num2;


                            int count_temp = 0;
                            foreach (var lt_lab in list_lab)
                            {
                                if (lt_lab.Item1 != pattern || lt_lab.Item5 != temp_color_num2)
                                {
                                    count_temp++;
                                }

                            }
                            if (count_temp == list_lab.Count)
                            {
                                list_lab.Add(new Tuple<string, double, double, double,int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num2));
                            }


                        }
                        else
                        {
                            color_num = 0;
                        }

                    }
                    else if ((diff_Delta_E > 1.2 * sensitivity_color && diff_Delta_E <= 2.0 * sensitivity_color))
                    {
                        count_color3++;
                        if ((result_count % 20) == 0)
                        {
                            if (count_color3 > 16) { count_color3 = 20; }
                            else { count_color3 = 0; }
                        }

                        if (count_color3 >= 20)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first3 = false;
                                temp_color_num3 = 1;
                            }
                            else
                            {
                                if (is_first3 == true)
                                {
                                    is_first3 = false;
                                    color_num_max++;
                                    temp_color_num3 = color_num_max;

                                    list_lab.Add(new Tuple<string, double, double, double,int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num3));

                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num3;

                            int count_temp = 0;
                            foreach (var lt_lab in list_lab)
                            {
                                if (lt_lab.Item1 != pattern || lt_lab.Item5 != temp_color_num3)
                                {
                                    count_temp++;
                                }

                            }
                            if (count_temp == list_lab.Count)
                            {
                                list_lab.Add(new Tuple<string, double, double, double,int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num3));
                            }
                        }
                        else
                        {
                            color_num = 0;
                        }

                    }
                    else if ((diff_Delta_E > 2.0 * sensitivity_color && diff_Delta_E <= 3.0 * sensitivity_color))
                    {

                        count_color4++;
                        if ((result_count % 20) == 0)
                        {
                            if (count_color4 > 16) { count_color4 = 20; }
                            else { count_color4 = 0; }
                        }

                        if (count_color4 >= 20)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first4 = false;
                                temp_color_num4 = 1;
                            }
                            else
                            {
                                if (is_first4 == true)
                                {
                                    is_first4 = false;
                                    color_num_max++;
                                    temp_color_num4 = color_num_max;

                                    list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num4));

                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num4;


                            int count_temp = 0;
                            foreach (var lt_lab in list_lab)
                            {
                                if (lt_lab.Item1 != pattern || lt_lab.Item5 != temp_color_num4)
                                {
                                    count_temp++;
                                }

                            }
                            if (count_temp == list_lab.Count)
                            {
                                list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num4));
                            }
                        }
                        else
                        {
                            color_num = 0;
                        }

                    }
                    else if ((diff_Delta_E > 3.0 * sensitivity_color && diff_Delta_E <= 4.0 * sensitivity_color))
                    {
                        count_color5++;
                        if ((result_count % 20) == 0)
                        {
                            if (count_color5 > 16) { count_color5 = 20; }
                            else { count_color5 = 0; }
                        }

                        if (count_color5 >= 20)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first5 = false;
                                temp_color_num5 = 1;
                            }
                            else
                            {
                                if (is_first5 == true)
                                {
                                    is_first5 = false;
                                    color_num_max++;
                                    temp_color_num5 = color_num_max;

                                    list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num5));

                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num5;

                            int count_temp = 0;
                            foreach (var lt_lab in list_lab)
                            {
                                if (lt_lab.Item1 != pattern || lt_lab.Item5 != temp_color_num5)
                                {
                                    count_temp++;
                                }

                            }
                            if (count_temp == list_lab.Count)
                            {
                                list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num5));
                            }
                        }
                        else
                        {
                            color_num = 0;
                        }

                    }
                    else
                    {
                        count_color6++;

                        if (count_color6 >= 20)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first6 = false;
                                temp_color_num6 = 1;
                            }
                            else
                            {
                                if (is_first6 == true)
                                {
                                    is_first6 = false;
                                    color_num_max++;
                                    temp_color_num6 = color_num_max;

                                    list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num6));

                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num6;

                            int count_temp = 0;
                            foreach (var lt_lab in list_lab)
                            {
                                if (lt_lab.Item1 != pattern || lt_lab.Item5 != temp_color_num6)
                                {
                                    count_temp++;
                                }

                            }
                            if (count_temp == list_lab.Count)
                            {
                                list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num5));
                            }
                        }
                        else
                        {
                            color_num = 0;
                        }

                    }
                }
                else if (diff_Delta_E < -0.5 * sensitivity_color && is_judge_color == false)
                {
                    diff_Delta_E = Math.Abs(diff_Delta_E);
                    Cyan = true;


                    if ((diff_Delta_E > 0.5 * sensitivity_color && diff_Delta_E <= 1.2 * sensitivity_color))
                    {

                        count_color7++;
                        if ((result_count % 20) == 0)
                        {
                            if (count_color7 > 16) { count_color7 = 20; }
                            else { count_color7 = 0; }
                        }

                        if (count_color7 >= 20)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first7 = false;
                                temp_color_num7 = 1;
                            }
                            else
                            {
                                if (is_first7 == true)
                                {
                                    is_first7 = false;
                                    color_num_max++;
                                    temp_color_num7 = color_num_max;

                                    list_lab.Add(new Tuple<string, double, double, double,int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num7));

                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num7;


                            int count_temp = 0;
                            foreach (var lt_lab in list_lab)
                            {
                                if (lt_lab.Item1 != pattern || lt_lab.Item5 != temp_color_num7)
                                {
                                    count_temp++;
                                }

                            }
                            if (count_temp == list_lab.Count)
                            {
                                list_lab.Add(new Tuple<string, double, double, double,int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num7));
                            }
                        }
                        else
                        {
                            color_num = 0;
                        }

                    }
                    else if ((diff_Delta_E > 1.2 * sensitivity_color && diff_Delta_E <= 2.0 * sensitivity_color))
                    {
                        count_color8++;
                        if ((result_count % 20) == 0)
                        {
                            if (count_color8 > 16) { count_color8 = 20; }
                            else { count_color8 = 0; }
                        }

                        if (count_color8 >= 20)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first8 = false;
                                temp_color_num8 = 1;
                            }
                            else
                            {
                                if (is_first8 == true)
                                {
                                    is_first8 = false;
                                    color_num_max++;
                                    temp_color_num8 = color_num_max;

                                    list_lab.Add(new Tuple<string, double, double, double,int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num8));

                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num8;

                            int count_temp = 0;
                            foreach (var lt_lab in list_lab)
                            {
                                if (lt_lab.Item1 != pattern || lt_lab.Item5 != temp_color_num8)
                                {
                                    count_temp++;
                                }

                            }
                            if (count_temp == list_lab.Count)
                            {
                                list_lab.Add(new Tuple<string, double, double, double,int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num8));
                            }
                        }
                        else
                        {
                            color_num = 0;
                        }

                    }
                    else if ((diff_Delta_E > 2.0 * sensitivity_color && diff_Delta_E <= 3.0 * sensitivity_color))
                    {

                        count_color9++;
                        if ((result_count % 20) == 0)
                        {
                            if (count_color9 > 16) { count_color9 = 20; }
                            else { count_color9 = 0; }
                        }

                        if (count_color9 >= 20)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first9 = false;
                                temp_color_num9 = 1;
                            }
                            else
                            {
                                if (is_first9 == true)
                                {
                                    is_first9 = false;
                                    color_num_max++;
                                    temp_color_num9 = color_num_max;

                                    list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num9));

                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num9;

                            int count_temp = 0;
                            foreach (var lt_lab in list_lab)
                            {
                                if (lt_lab.Item1 != pattern || lt_lab.Item5 != temp_color_num9)
                                {
                                    count_temp++;
                                }

                            }
                            if (count_temp == list_lab.Count)
                            {
                                list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num9));
                            }
                        }
                        else
                        {
                            color_num = 0;
                        }

                    }
                    else if ((diff_Delta_E > 3.0 * sensitivity_color && diff_Delta_E <= 4.0 * sensitivity_color))
                    {

                        count_color10++;
                        if ((result_count % 20) == 0)
                        {
                            if (count_color10 > 16) { count_color10 = 20; }
                            else { count_color10 = 0; }
                        }

                        if (count_color10 >= 20)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first10 = false;
                                temp_color_num10 = 1;
                            }
                            else
                            {
                                if (is_first10 == true)
                                {
                                    is_first10 = false;
                                    color_num_max++;
                                    temp_color_num10 = color_num_max;

                                    list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num10));

                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num10;

                            int count_temp = 0;
                            foreach (var lt_lab in list_lab)
                            {
                                if (lt_lab.Item1 != pattern || lt_lab.Item5 != temp_color_num10)
                                {
                                    count_temp++;
                                }

                            }
                            if (count_temp == list_lab.Count)
                            {
                                list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num10));
                            }
                        }
                        else
                        {
                            color_num = 0;
                        }
                    }
                    else if ((diff_Delta_E > 4.0 * sensitivity_color && diff_Delta_E <= 5.0 * sensitivity_color))
                    {

                        count_color11++;
                        if ((result_count % 20) == 0)
                        {
                            if (count_color11 > 16) { count_color11 = 20; }
                            else { count_color11 = 0; }
                        }

                        if (count_color11 >= 20)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first11 = false;
                                temp_color_num11 = 1;
                            }
                            else
                            {
                                if (is_first11 == true)
                                {
                                    is_first11 = false;
                                    color_num_max++;
                                    temp_color_num11 = color_num_max;

                                    list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num11));

                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num11;

                            int count_temp = 0;
                            foreach (var lt_lab in list_lab)
                            {
                                if (lt_lab.Item1 != pattern || lt_lab.Item5 != temp_color_num11)
                                {
                                    count_temp++;
                                }

                            }
                            if (count_temp == list_lab.Count)
                            {
                                list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num11));
                            }
                        }
                        else
                        {
                            color_num = 0;
                        }

                    }
                    else
                    {
                        count_color12++;

                        if (count_color12 >= 20)
                        {
                            if (is_first_color)
                            {
                                is_first_color = false;
                                is_first12 = false;
                                temp_color_num12 = 1;
                            }
                            else
                            {
                                if (is_first12 == true)
                                {
                                    is_first12 = false;
                                    color_num_max++;
                                    temp_color_num12 = color_num_max;

                                    list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num12));

                                }
                                else
                                {
                                    //如果某一色号重复出现时，不增加色号值
                                }

                            }
                            color_num = temp_color_num12;

                            int count_temp = 0;
                            foreach (var lt_lab in list_lab)
                            {
                                if (lt_lab.Item1 != pattern || lt_lab.Item5 != temp_color_num12)
                                {
                                    count_temp++;
                                }

                            }
                            if (count_temp == list_lab.Count)
                            {
                                list_lab.Add(new Tuple<string, double, double, double, int>(pattern, LAB_L, LAB_A, LAB_B, temp_color_num12));
                            }
                        }
                        else
                        {
                            color_num = 0;
                        }

                    }
                }
            }


            if (result_count >= 16 && Is_Use_Factory_Custom == true)
            {
                //switch (color_num)
                //{
                //    case 2:
                //        if (Operate_merge_color.two_to_what) { color_num = Info_merge_color.two_merge_what; }
                //        break;

                //    case 3:
                //        if (Operate_merge_color.three_to_what) { color_num = Info_merge_color.three_merge_what; }
                //        break;

                //    case 4:
                //        if (Operate_merge_color.four_to_what) { color_num = Info_merge_color.four_merge_what; }
                //        break;

                //    case 5:
                //        if (Operate_merge_color.five_to_what) { color_num = Info_merge_color.five_merge_what; }
                //        break;

                //    case 6:
                //        if (Operate_merge_color.six_to_what) { color_num = Info_merge_color.six_merge_what; }
                //        break;

                //    case 7:
                //        if (Operate_merge_color.seven_to_what) { color_num = Info_merge_color.seven_merge_what; }
                //        break;

                //    case 8:
                //        if (Operate_merge_color.eight_to_what) { color_num = Info_merge_color.eight_merge_what; }
                //        break;

                //    case 9:
                //        if (Operate_merge_color.nine_to_what) { color_num = Info_merge_color.nine_merge_what; }
                //        break;

                //    case 10:
                //        if (Operate_merge_color.ten_to_what) { color_num = Info_merge_color.ten_merge_what; }
                //        break;

                //    case 11:
                //        if (Operate_merge_color.eleven_to_what) { color_num = Info_merge_color.eleven_merge_what; }
                //        break;

                //    case 12:
                //        if (Operate_merge_color.twelve_to_what) { color_num = Info_merge_color.twelve_merge_what; }
                //        break;

                //}
                if (Operate_merge_color.two_to_what && color_num == 2) { color_num = Info_merge_color.two_merge_what; }
                if (Operate_merge_color.three_to_what && color_num == 3) { color_num = Info_merge_color.three_merge_what; }
                if (Operate_merge_color.four_to_what && color_num == 4) { color_num = Info_merge_color.four_merge_what; }
                if (Operate_merge_color.five_to_what && color_num == 5) { color_num = Info_merge_color.five_merge_what; }
                if (Operate_merge_color.six_to_what && color_num == 6) { color_num = Info_merge_color.six_merge_what; }
                if (Operate_merge_color.seven_to_what && color_num == 7) { color_num = Info_merge_color.seven_merge_what; }
                if (Operate_merge_color.eight_to_what && color_num == 8) { color_num = Info_merge_color.eight_merge_what; }
                if (Operate_merge_color.nine_to_what && color_num == 9) { color_num = Info_merge_color.nine_merge_what; }
                if (Operate_merge_color.ten_to_what && color_num == 10) { color_num = Info_merge_color.ten_merge_what; }
                if (Operate_merge_color.eleven_to_what && color_num == 11) { color_num = Info_merge_color.eleven_merge_what; }
                if (Operate_merge_color.twelve_to_what && color_num == 12) { color_num = Info_merge_color.twelve_merge_what; }


                if (Is_Use_Factory_Custom == true)
                {
                    switch (color_num)
                    {
                        case 0: aberration_count++; has_chromatic_aberration = true; break;
                        case 1: num_1++; textBox12.Invoke(new Action(() => textBox12.Text = num_1.ToString())); break;
                        case 2: num_2++; textBox13.Invoke(new Action(() => textBox13.Text = num_2.ToString())); aberration_count++; has_chromatic_aberration = true; break;
                        case 3: num_3++; textBox14.Invoke(new Action(() => textBox14.Text = num_3.ToString())); aberration_count++; has_chromatic_aberration = true; break;
                        case 4: num_4++; textBox15.Invoke(new Action(() => textBox15.Text = num_4.ToString())); aberration_count++; has_chromatic_aberration = true; break;
                        case 5: num_5++; textBox16.Invoke(new Action(() => textBox16.Text = num_5.ToString())); aberration_count++; has_chromatic_aberration = true; break;
                        case 6: num_6++; textBox17.Invoke(new Action(() => textBox17.Text = num_6.ToString())); aberration_count++; has_chromatic_aberration = true; break;
                        case 7: num_7++; textBox18.Invoke(new Action(() => textBox18.Text = num_7.ToString())); aberration_count++; has_chromatic_aberration = true; break;
                        case 8: num_8++; textBox19.Invoke(new Action(() => textBox19.Text = num_8.ToString())); aberration_count++; has_chromatic_aberration = true; break;
                        case 9: num_9++; textBox20.Invoke(new Action(() => textBox20.Text = num_9.ToString())); aberration_count++; has_chromatic_aberration = true; break;
                        case 10: num_10++; textBox21.Invoke(new Action(() => textBox21.Text = num_10.ToString())); aberration_count++; has_chromatic_aberration = true; break;
                        case 11: num_11++; textBox22.Invoke(new Action(() => textBox22.Text = num_11.ToString())); aberration_count++; has_chromatic_aberration = true; break;
                        case 12: num_12++; textBox23.Invoke(new Action(() => textBox23.Text = num_12.ToString())); aberration_count++; has_chromatic_aberration = true; break;

                    }
                }


            }


            //判断当前色号标准是否稳定
            if ((result_count % 60) == 0)
            {
                if ((result_count / 60) == 1)
                {
                    if (num_1 > 10)  //由于第一轮的前60块的最开始28块在求平均值过程，没有num_1的计数，总计数只有32个
                    {
                        is_stable = true;
                    }
                }
                else
                {
                    if (num_1 > 20 * (result_count / 60))
                    {
                        is_stable = true;
                    }
                }


            }



            //每100块砖计算一次色度超标的砖数，如果超标则初始化计数，重新计算当前批次砖的色差，同时把色度超标计数置零，达到自动换型的目的
            if ((result_count % 100) == 0 && is_stable == false)
            {
                if (aberration_count >= 90)
                {
                    Initi();
                    Create_Excel();   // 写入Excel数据的行高有限制，为65535
                    textBox4.Invoke(new Action(() => textBox4.Text = ""));
                    textBox12.Invoke(new Action(() => textBox12.Text = ""));
                    textBox13.Invoke(new Action(() => textBox13.Text = ""));
                    textBox14.Invoke(new Action(() => textBox14.Text = ""));
                    textBox15.Invoke(new Action(() => textBox15.Text = ""));
                    textBox16.Invoke(new Action(() => textBox16.Text = ""));
                    textBox17.Invoke(new Action(() => textBox17.Text = ""));
                    textBox18.Invoke(new Action(() => textBox18.Text = ""));
                    textBox19.Invoke(new Action(() => textBox19.Text = ""));
                    textBox20.Invoke(new Action(() => textBox20.Text = ""));
                    textBox21.Invoke(new Action(() => textBox21.Text = ""));
                    textBox22.Invoke(new Action(() => textBox22.Text = ""));
                    textBox23.Invoke(new Action(() => textBox23.Text = ""));
                }
                else
                {
                    aberration_count = 0;
                }

            }


            if (Cyan == true)  //由于前面计算色偏差diff_Delta_E的时候，如果砖是偏青取的是绝对值，这里用于信息记录的时候还原回来
            {
                diff_Delta_E = -diff_Delta_E;
            }
            if (Whitening == false)
            {
                diff_LAB_L = -diff_LAB_L;
            }


            //Bitmap bmp = BitmapConverter.ToBitmap(img_show);
            //Graphics g = Graphics.FromImage(bmp);
            //String str = "hello, string";
            //Font font = new Font("微软雅黑", 5);
            //SolidBrush sbrush = new SolidBrush(Color.Red);
            //g.DrawString(str, font, sbrush, new Rectangle(500, 50, 500, 50));


            //把版型信息打到图像上
            if (measure.Pattern_Judgment == true && Pattern_ok == true)
            {
                pattern_num = pattern.Substring(2, pattern.Length - 2);
                Cv2.PutText(img_show, "pattern" + pattern_num, new OpenCvSharp.Point(img_show.Width / 4, img_show.Height / 6), HersheyFonts.Italic, 4, new Scalar(255, 0, 0));
            }
            Cv2.ImWrite(address_show + "\\" + "num_" + measure.count.ToString() + ".jpg", img_show); //保存用于界面图像显示的图像



            if (Online == true && Is_Use_Factory_Custom == true)
            {
                if (has_chromatic_aberration == true && Is_Save_NG_Image == true)
                {
                    Cv2.ImWrite(address_NG + "\\" + "num__" + measure.count.ToString() + "__time__" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "__色号__" + color_num.ToString() + ".jpg", img_show); //保存NG，有色偏的图像
                }

                /*CaptureMesg(measure.count.ToString(), color_num.ToString(), sigma_LAB_A, sigma_LAB_B, HSV_H, LAB_L, LAB_A, LAB_B, diff_LAB_A, diff_LAB_B);*/   //结果记录方法
                Data2Excel(measure.count.ToString(), pattern, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), color_num.ToString(), sigma_LAB_A, sigma_LAB_B, sigma_LAB_L, HSV_H, LAB_L, LAB_A, LAB_B, diff_LAB_L, diff_LAB_A, diff_LAB_B, diff_Delta_E, diff_Delta_E_pure, diff_Delta_E_texture,mean_Delta_E_pure,mean_Delta_E_texture,Delta_E_pure,Delta_E_texture, mean_LAB_L,mean_HSV_H,diff_HSV_H, measure.Average_gray, measure.Coefficient, BGR_B, BGR_G, BGR_R, diff_B, diff_G, diff_R, cosTheta,deltaE);


            }
            else if (Online == false && Is_Use_Factory_Custom == true)
            {
                if (has_chromatic_aberration == true && Is_Save_NG_Image == true)
                {
                    Cv2.ImWrite(address_NG + "\\" + "name__" + name + "__time__" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "__色号__" + color_num.ToString() + ".jpg", img_show); //保存NG，有色偏的图像
                }

                /*CaptureMesg(measure.count.ToString(), color_num.ToString(), sigma_LAB_A, sigma_LAB_B, HSV_H, LAB_L, LAB_A, LAB_B, diff_LAB_A, diff_LAB_B);*/   //结果记录方法
                Data2Excel(name, pattern, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), color_num.ToString(), sigma_LAB_A, sigma_LAB_B, sigma_LAB_L, HSV_H, LAB_L, LAB_A, LAB_B, diff_LAB_L, diff_LAB_A, diff_LAB_B, diff_Delta_E, diff_Delta_E_pure, diff_Delta_E_texture, mean_Delta_E_pure, mean_Delta_E_texture, Delta_E_pure, Delta_E_texture, mean_LAB_L,mean_HSV_H,diff_HSV_H, measure.Average_gray, measure.Coefficient, BGR_B, BGR_G, BGR_R, diff_B, diff_G, diff_R, cosTheta, deltaE);

            }


            //色差稳定后，连续保存20张图像，用于与有色差砖的对比观察
            if (Online == true && is_stable == true)
            {
                
                if (num_stable <= 19)
                {
                    Cv2.ImWrite(address_NG + "\\" + "num__" + measure.count.ToString() + "__time__" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "__色号__" + color_num + ".jpg", img_show);
                    num_stable++;
                }

            }



            var tuple = Tuple.Create(color_num.ToString(), time_consuming, diff_Delta_E, HSV_H, diff_LAB_L, LAB_A, LAB_B, pattern_num);
            flag_list.Add(tuple);
            enable_process = true;   //是否处理完成状态标志改写



            //延时定时器加载
            Timers[measure.count - 1] = new System.Timers.Timer();
            Timers[measure.count - 1].Interval = Time_Delay - (int)time_consuming;
            Timers[measure.count - 1].Elapsed += timer1_Tick;
            Timers[measure.count - 1].AutoReset = false;
            Timers[measure.count - 1].Enabled = true;
            Timers[measure.count - 1].Start();


            //单次图像处理耗时统计
            stpwth2.Stop();
            TimeSpan ts4 = stpwth2.Elapsed;
            textBox5.Invoke(new Action(() => textBox5.Text = ts4.TotalMilliseconds.ToString("f2")));


        }


        //关闭软件
        private void button3_Click(object sender, EventArgs e)
        {
            MessageBoxButtons mess = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("确认退出吗？", "提示", mess);
            if (dr == DialogResult.OK)
            {
                Thread.Sleep(100);

                //关闭数码屏的串口
                try
                {
                    if (serialPort != null && serialPort.IsOpen)
                    {
                        serialPort.Close();
                        serialPort.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    //将异常信息传递给用户。  
                    MessageBox.Show(ex.Message);
                    return;
                }

                System.Environment.Exit(0);

                
            }
            

            //获得任务管理器中的所有进程,关闭SDK驱动
            //Process[] process = Process.GetProcesses();
            //foreach (Process p1 in process)
            //{
            //    try
            //    {
            //        string processName = p1.ProcessName.ToLower().Trim();
            //        //判断是否包含阻碍更新的进程
            //        if (processName == "camera_rgb")
            //        {
            //            p1.Kill();
            //        }
            //    }
            //    catch { }
            //}

            //form4.btn_freeze_Click(null,null);
            //Thread.Sleep(1000);
            //System.Environment.Exit(0);
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            MessageBoxButtons mess = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("确认退出吗？", "提示", mess);
            if (dr != DialogResult.OK)
            {
                e.Cancel = true;
            }
            else
            {
                Thread.Sleep(100);

                //关闭数码屏的串口
                try
                {
                    if (serialPort != null && serialPort.IsOpen)
                    {
                        serialPort.Close();
                        serialPort.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    //将异常信息传递给用户。  
                    MessageBox.Show(ex.Message);
                    return;
                }

                System.Environment.Exit(0);


                
            }



            //获得任务管理器中的所有进程,关闭SDK驱动
            ////Process[] process = Process.GetProcesses();
            ////foreach (Process p1 in process)
            ////{
            ////    try
            ////    {
            ////        string processName = p1.ProcessName.ToLower().Trim();
            ////        //判断是否包含阻碍更新的进程
            ////        if (processName == "camera_rgb")
            ////        {
            ////            p1.Kill();
            ////        }
            ////    }
            ////    catch { }
            ////}

            ////form4.btn_freeze_Click(null,null);

            //Thread.Sleep(1000);
            //System.Environment.Exit(0);


        }


        //批量文件的下一张处理
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //------------------文件夹中所有图像的处理---------------------

                //获取所有文件夹的名称集合
                path_scr = textBox3.Text.ToString() + "\\";
                string[] dir = Directory.GetDirectories(path_scr);

                string[] names = new string[dir.Length];

                

                if (next < dir.Length)
                {
                    try
                    {
                        //赋值文件命名
                        names[next] = Path.GetFileName(dir[next]);
                        List<string> ImagePaths = new List<string>();

                        //当前文件夹名称显示
                        textBox1.Text = "";
                        textBox1.Text = names[next];

                        //删除原有的处理过的效果图片
                        File.Delete(path_scr + names[next] + "\\img_LAB_L.jpg");
                        File.Delete(path_scr + names[next] + "\\img_LAB_A.jpg");
                        File.Delete(path_scr + names[next] + "\\img_LAB_B.jpg");
                        File.Delete(path_scr + names[next] + "\\img_HSV_H.jpg");
                        File.Delete(path_scr + names[next] + "\\img_HSV_S.jpg");
                        File.Delete(path_scr + names[next] + "\\img_HSV_V.jpg");


                        //获取当前文件夹中所有文件的路径
                        foreach (string Path in Directory.GetFiles(path_scr + names[next]))
                        {
                            //判断是否为图片格式
                            string PathExt = Path.Substring(Path.Length - 3, 3);
                            if (PathExt == "jpg" || PathExt == "bmp" || PathExt == "png") //筛选图片格式
                            {
                                ImagePaths.Add(Path);
                            }
                        }

                        //这里0表示高角度图像，1表示低角度图像
                        image_high = new Mat(ImagePaths[0], ImreadModes.Color);
                        //image_low = new Mat(ImagePaths[1], ImreadModes.Grayscale);
                    

                        if (first_run)
                        {
                            new Action(() =>
                            {
                                task_chromatic(image_high, path_scr + names[0]);
                                first_run = false;

                                next++;
                                //剩余张数的显示
                                textBox2.Invoke(new Action(() => textBox2.Text = (dir.Length - next).ToString()));
                                
                            }
                            ).Invoke();
                        }
                        else if ((next < dir.Length && enable_process == true) || measure.has_error == true)
                        {
                            enable_process = false;
                            task_chromatic(image_high, path_scr + names[next]);
                            next++;

                            //剩余张数的显示
                            textBox2.Invoke(new Action(() => textBox2.Text = (dir.Length - next).ToString()));

                            //处理完成标志
                            if (next == dir.Length)
                            {
                                label7.Invoke(new Action(new Action(() => label7.Visible = true)));
                                label7.Invoke(new Action(new Action(() => label7.BackColor = Color.Green)));
                            }
                        }

                    }
                    catch
                    { }
                }
            }
            catch
            {
                MessageBox.Show("请输入正确的文件地址! 例如：D:\\南京光衡\\图库\\建兴陶瓷\\");
            }

            

        }


        //删除存图
        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                //------------------文件夹中所有图像的处理---------------------

                //获取所有文件夹的名称集合
                path_scr = textBox3.Text.ToString() + "\\";
                string[] dir = Directory.GetDirectories(path_scr);

                string[] names = new string[dir.Length];
                for (int i = 0; i < dir.Length; i++)
                {
                    GC.Collect();

                    //赋值文件命名
                    names[i] = Path.GetFileName(dir[i]);
                    List<string> ImagePaths = new List<string>();

                    //删除原有的处理过的效果图片
                    File.Delete(path_scr + names[i] + "\\img_LAB_L.jpg");
                    File.Delete(path_scr + names[i] + "\\img_LAB_A.jpg");
                    File.Delete(path_scr + names[i] + "\\img_LAB_B.jpg");
                    File.Delete(path_scr + names[i] + "\\img_HSV_H.jpg");
                    File.Delete(path_scr + names[i] + "\\img_HSV_S.jpg");
                    File.Delete(path_scr + names[i] + "\\img_HSV_V.jpg");
                    File.Delete(path_scr + names[i] + "\\img_dst.bmp");
                }

            }
            catch
            {
                MessageBox.Show("请输入正确的文件地址! 例如：D:\\南京光衡\\图库\\建兴陶瓷\\");
            }
        }


        //清除地址
        private void button5_Click(object sender, EventArgs e)
        {
            textBox3.Text = "";
        }


        bool MemMap_Init = false;               //内存共享是否打开
        bool is_New_image = false;              //是否有新图像生成
        bool is_Image_stabilization = false;    //图像亮度是否稳定值
        Mat has_img = new Mat();
        Mat Replacement = new Mat();



        Mat Image_stabilization = new Mat();
        int image_height = 0;                   //内存共享得到的图像高度




        //打开内存映射并读取数据
        private void button7_Click(object sender, EventArgs e)
        {

            if (button7.Text == "开始色差检测")
            {
                button7.Text = "检测正在进行";
                //button7.ForeColor = Color.White;
                button7.BackColor = Color.Gray;
                button7.Enabled = false;


                ////打开dalsa采图SDK
                //ProcessStartInfo info = new ProcessStartInfo();
                //info.FileName = @"D:\kevin\色差检测\辉鹏色差检测V3.7\Camera_RGB_辉鹏\Camera_RGB_辉鹏\Camera_RGB_辉鹏\Camera_RGB\Camera_RGB\Camera_RGB\bin\x64\Debug\Camera_RGB.exe";
                //info.Arguments = "";
                ////指定程序运行状态，最大化、最小化等
                //info.WindowStyle = ProcessWindowStyle.Maximized;
                //Process pro = Process.Start(info);



                //开启工作线程
                try
                {
                    num_Mem_sharing = 1;
                }
                catch
                {
                    num_Mem_sharing = 1;
                }


                //MemMap_Init = Cs_MeM_Image.OpenMemMap("Mem_Camera_Image");
                Thread thd = new Thread(thd_process);
                thd.IsBackground = true;
                thd.Start();

                //timer1.Enabled = true;
                //timer1.Start();
            }

            //form4 = new Form4();   //初始化DALSA采集驱动

            Online = true;
        }



        
        private void thd_process()
        {

            //判断内存共享是否打开，没有打开会一直等待
            while (true)
            {
                MemMap_Init = Cs_MeM_Image.OpenMemMap("Mem_Camera_Image");
                if (MemMap_Init == true)
                {
                    break;
                }
                continue;
            }



            //删除内存共享文件夹下的通讯标志位图像，防止存在历史遗留的标志位图像
            try
            {
                //获取所有文件夹的名称集合
                string path_sdk = "D:\\GH_CeramicDetection\\配置\\色差\\";
                string jpgpattern = "*.jpg";

                string[] jpgfiles = Directory.GetFiles(path_sdk, jpgpattern);
                foreach (string jpgfile in jpgfiles)
                {
                    File.Delete(jpgfile);
                }

            }
            catch
            {
            }


            //删除与上位机通讯标志位图像，防止存在历史遗留的标志位图像
            try
            {
                //获取所有文件夹的名称集合
                string path_sdk = pathColorDifComu + "\\";
                string jpgpattern = "*.jpg";

                string[] jpgfiles = Directory.GetFiles(path_sdk, jpgpattern);
                foreach (string jpgfile in jpgfiles)
                {
                    File.Delete(jpgfile);
                }

            }
            catch
            {
            }



            //进入图像处理循环
            while (true) 
            {
                //判断是否换型
                try
                {
                    string path = "D:\\GH_CeramicDetection\\配置\\换型\\";
                    string jpgpattern = "*.jpg";
                    string[] jpgfiles = Directory.GetFiles(path, jpgpattern);
                    Replacement = Cv2.ImRead(jpgfiles[0], ImreadModes.AnyDepth);
                    File.Delete(jpgfiles[0]);

                    is_Image_stabilization = false;

                    Initi();
                    Create_Excel();

                    textBox4.Invoke(new Action(() => textBox4.Text = ""));
                    textBox12.Invoke(new Action(() => textBox12.Text = ""));
                    textBox13.Invoke(new Action(() => textBox13.Text = ""));
                    textBox14.Invoke(new Action(() => textBox14.Text = ""));
                    textBox15.Invoke(new Action(() => textBox15.Text = ""));
                    textBox16.Invoke(new Action(() => textBox16.Text = ""));
                    textBox17.Invoke(new Action(() => textBox17.Text = ""));
                    textBox18.Invoke(new Action(() => textBox18.Text = ""));
                    textBox19.Invoke(new Action(() => textBox19.Text = ""));
                    textBox20.Invoke(new Action(() => textBox20.Text = ""));
                    textBox21.Invoke(new Action(() => textBox21.Text = ""));
                    textBox22.Invoke(new Action(() => textBox22.Text = ""));
                    textBox23.Invoke(new Action(() => textBox23.Text = ""));



                    //删除稳定文件夹下的通讯标志位图像，防止存在历史遗留的标志位图像
                    try
                    {
                        //获取所有文件夹的名称集合
                        string path_sdk = "D:\\GH_CeramicDetection\\配置\\稳定\\";
                        string pattern = "*.jpg";

                        string[] files = Directory.GetFiles(path_sdk, pattern);
                        foreach (string jpgfile in files)
                        {
                            File.Delete(jpgfile);
                        }

                    }
                    catch
                    {
                    }

                }
                catch
                {
                }



                //判断图像亮度是否稳定
                try
                {
                    string path = "D:\\GH_CeramicDetection\\配置\\稳定\\";
                    string jpgpattern = "*.jpg";
                    string[] jpgfiles = Directory.GetFiles(path, jpgpattern);
                    Image_stabilization = Cv2.ImRead(jpgfiles[0], ImreadModes.AnyDepth);
                    File.Delete(jpgfiles[0]);

                    is_Image_stabilization = true;

                    //删除内存共享文件夹下的通讯标志位图像，因为换型到图像稳定需要跑一批砖
                    try
                    {
                        //获取所有文件夹的名称集合
                        string path_sc = "D:\\GH_CeramicDetection\\配置\\色差\\";
                        string Image_format = "*.jpg";

                        string[] Imagefiles = Directory.GetFiles(path_sc, Image_format);
                        foreach (string jpgfile in Imagefiles)
                        {
                            File.Delete(jpgfile);
                        }

                    }
                    catch
                    {
                    }
                }
                catch
                {
                }



                //判断是否有新图像生成
                if (enable_process == true || first_run == true || measure.has_error == true)
                {

                    try
                    {
                        string path = "D:\\GH_CeramicDetection\\配置\\色差\\";
                        string jpgpattern = "*.jpg";
                        string[] jpgfiles = Directory.GetFiles(path, jpgpattern);
                        has_img = Cv2.ImRead(jpgfiles[0], ImreadModes.AnyDepth);

                        string PathChoose = jpgfiles[0].Substring(29, jpgfiles[0].Length - 29);
                        PathChoose = PathChoose.Remove(PathChoose.Length - 4, 4);
                        string[] mesg = PathChoose.Split('&');
                        image_height = Convert.ToInt32(mesg[1]);

                        File.Delete(jpgfiles[0]);
                        is_New_image = true;
                    }
                    catch
                    {
                        is_New_image = false;
                    }



                    //try
                    //{
                    //    has_img = Cv2.ImRead(@"D:\\GH_CeramicDetection\\配置\\色差\\RGB3_" + num_Mem_sharing.ToString() + ".jpg", ImreadModes.AnyDepth);
                    //    //has_img = Cv2.ImRead(@"C:\\Users\\Lenovo\\Desktop\\test3.jpg", ImreadModes.AnyDepth);
                    //    is_New_image = true;
                    //    File.Delete("D:\\GH_CeramicDetection\\配置\\色差\\RGB3_" + num_Mem_sharing.ToString() + ".jpg");
                    //}
                    //catch(Exception ex)
                    //{//如果读取自加1的num_Mem_sharing失败，则会从计数1开始读取，上位机换型会初始化计数

                    //    //throw ex;
                    //    is_New_image = false;

                    //    try
                    //    {
                    //        has_img = Cv2.ImRead(@"D:\\GH_CeramicDetection\\配置\\色差\\RGB3_" + 1.ToString() + ".jpg", ImreadModes.AnyDepth);
                    //        num_Mem_sharing = 1; 
                    //        is_New_image = true;
                    //        File.Delete("D:\\GH_CeramicDetection\\配置\\色差\\RGB3_" + 1.ToString() + ".jpg");
                    //    }
                    //    catch(Exception ex2)
                    //    {
                    //        continue;
                    //    }

                    //}
                }



                //处理图像
                if ((MemMap_Init == true && is_New_image == true && is_Image_stabilization == true && enable_process == true) || (is_New_image == true && is_Image_stabilization == true && first_run == true) || (measure.has_error == true && is_New_image == true && is_Image_stabilization == true))
                {
                    try
                    {

                        //byte[] read_byte_image1 = Cs_MeM_Image.ReadDataByte(100 + 2000 * 2000 * 0, 1534 * 863);//图片数据
                        //Mat mat_save1 = new Mat(read_byte_image[3] * 256 + read_byte_image[2], read_byte_image[1] * 256 + read_byte_im[0],MatType.CV_8UC1, read_byte_image1);
                        //Cv2.ImWrite(@"C:\Users\Lenovo\Desktop\test1.jpg", mat_save1);

                        //byte[] read_byte_image2 = Cs_MeM_Image.ReadDataByte(100 + 2000 * 2000 * 1, 1534 * 863);//图片数据
                        //Mat mat_save2 = new Mat(read_byte_image[3] * 256 + read_byte_image[2], read_byte_image[1] * 256 + read_byte_im[0],MatType.CV_8UC1, read_byte_image2);
                        //Cv2.ImWrite(@"C:\Users\Lenovo\Desktop\test2.jpg", mat_save2);


                        stpwth1.Restart();
                        Thread.Sleep(1);
                        //MemMap_Init = false;
                        is_New_image = false;
                        enable_process = false;
                        first_run = false;

                        //测试现场数据读取
                        //byte[] read_byte_image = Cs_MeM_Image.ReadDataByte(2, 4);//从地址为2开始读取四个字节，为图片长宽
                        mat_RGB_B.Dispose();
                        mat_RGB_G.Dispose();
                        mat_RGB_R.Dispose();



                        byte[] byte_RGB_B = Cs_MeM_Image.ReadDataByte(8192 * 20000 * 8, 8192 * image_height);//图片B通道数据
                        mat_RGB_B = new Mat(image_height, 8192, MatType.CV_8UC1, byte_RGB_B);
                        //Cv2.ImWrite(address_test + "\\" + "RGB_B.jpg", mat_RGB_B);


                        byte[] byte_RGB_G = Cs_MeM_Image.ReadDataByte(8192 * 20000 * 9, 8192 * image_height);//图片G通道数据
                        mat_RGB_G = new Mat(image_height, 8192, MatType.CV_8UC1, byte_RGB_G);
                        //Cv2.ImWrite(address_test + "\\" + "RGB_G.jpg", mat_RGB_G);

                        byte[] byte_RGB_R = Cs_MeM_Image.ReadDataByte(8192 * 20000 * 10, 8192 * image_height);//图片R通道数据
                        mat_RGB_R = new Mat(image_height, 8192, MatType.CV_8UC1, byte_RGB_R);
                        //Cv2.ImWrite(address_test + "\\" + "RGB_R.jpg", mat_RGB_R);

                        BGR = new Mat[3] { mat_RGB_B, mat_RGB_G, mat_RGB_R };


                        image_high_Mem = new Mat(mat_RGB_B.Size(), MatType.CV_8UC3);
                        Cv2.Merge(BGR, image_high_Mem);
                        BGR[0].Dispose(); BGR[1].Dispose(); BGR[2].Dispose();


                        //保存dalsa相机内存共享原图按钮
                        if (Is_Save_Original_Image == true)
                        {
                            Cv2.ImWrite(address_original + "\\" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "num_" + num.ToString() + ".jpg", image_high_Mem);
                        }


                        textBox9.Invoke(new Action(() => textBox9.Text = num.ToString()));
                        string address_original_use = address_original /*+ "\\" + num.ToString()*/;
                        num++;
                        num_Mem_sharing++;

                        stpwth1.Stop();
                        TimeSpan ts1 = stpwth1.Elapsed;
                        textBox10.Invoke(new Action(() => textBox10.Text = ts1.TotalMilliseconds.ToString("f2")));


                        stpwth2.Restart();
                        task_chromatic(image_high_Mem, address_original_use);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                        num++;
                        num_Mem_sharing++;
                        enable_process = true;
                    }
                }

            }
            
        }


        //本地文件夹的批量图像处理
        private void button2_Click(object sender, EventArgs e)
        {
            
            Online = false;

            if (Is_Have_Folder_Nest == true)
            {
                new Action(() =>
                {
                    path_scr_batch = textBox3.Text.ToString() + "\\";
                    dir = Directory.GetDirectories(path_scr_batch).OrderBy(file => File.GetLastWriteTime(file)).ToArray();
                    //dir = Directory.GetFiles(path_scr_batch);
                    names = new string[dir.Length];
                    for (int i = 0; i < dir.Length; i++)
                    {
                        //赋值文件命名
                        names[i] = Path.GetFileName(dir[i]);

                        //删除原有的处理过的效果图片
                        File.Delete(path_scr_batch + names[i] + "\\img_LAB_L.jpg");
                        File.Delete(path_scr_batch + names[i] + "\\img_LAB_A.jpg");
                        File.Delete(path_scr_batch + names[i] + "\\img_LAB_B.jpg");
                        File.Delete(path_scr_batch + names[i] + "\\img_HSV_H.jpg");
                        File.Delete(path_scr_batch + names[i] + "\\img_HSV_S.jpg");
                        File.Delete(path_scr_batch + names[i] + "\\img_HSV_V.jpg");
                        File.Delete(path_scr_batch + names[i] + "\\img_dst.bmp");

                        int j = 0;
                        strings = new List<string>();
                        //获取当前文件夹中所有文件的路径
                        foreach (string Path in Directory.GetFiles(path_scr_batch + names[i]))
                        {
                            //判断是否为图片格式
                            string PathExt = Path.Substring(Path.Length - 3, 3);
                            if (PathExt == "jpg" || PathExt == "bmp" || PathExt == "png") //筛选图片格式
                            {
                                strings.Add(Path);
                                j++;

                                if (j == 1)
                                {
                                    break;
                                }
                            }
                        }
                        if (strings.Count == 1)
                        {
                            ImagePaths.Add(strings);
                            names_use.Add(names[i]);
                        }
                        else
                        {
                            continue;
                        }

                    }
                }).Invoke();
            }
            else
            {
                has_folder = false;

                new Action(() =>
                {
                    path_scr_batch = textBox3.Text.ToString() ;
                    //dir = Directory.GetFiles(path_scr_batch + "\\").OrderBy(file => File.GetLastWriteTime(file)).ToArray();
                    dir = Directory.GetFiles(path_scr_batch + "\\", "*.jpg").OrderBy(path => ExtractSequenceNumber(path)).ToArray();

                }).Invoke();
            }
            
            

            //开启工作线程
            Thread thd = new Thread(task_process);
            thd.IsBackground = true;
            thd.Start();

           
        }


        private static int ExtractSequenceNumber(string path)
        {
            //string sequencePart = path.Split('_')[2]; // 提取 _num_ 后的第一个部分
            //return int.Parse(sequencePart); // 转换为整数

            // 使用正则表达式匹配文件名中的数字
            string pattern = @"num_(\d+)_pattern";
            Match match = Regex.Match(path, pattern);
            int extractedNumber = 0;
            if (match.Success)
            {
                // 提取匹配的数字
                string num = match.Groups[1].Value;
                extractedNumber = int.Parse(num);
                //  Console.WriteLine($"Extracted number: {extractedNumber}");
            }
            return extractedNumber;
        }



        private async void task_process()
        {
            
            while (true)
            {
                if (first_run)
                {
                    if (has_folder == true)
                    {
                        textBox1.Invoke(new Action(() => textBox1.Text = names_use[0]));
                    }
                    
                    stpwth1.Restart();
                    //这里0表示高角度图像，1表示低角度图像
                    if (has_folder == false)
                    {
                        image_high = new Mat(dir[0], ImreadModes.Color);
                    }
                    else if (has_folder == true)
                    {
                        image_high = new Mat(ImagePaths[0][0], ImreadModes.Color);
                    }
                    //image_low = new Mat(ImagePaths[0][1], ImreadModes.Grayscale);
                    stpwth1.Stop();
                    TimeSpan ts2 = stpwth1.Elapsed;
                    textBox10.Invoke(new Action(() => textBox10.Text = ts2.TotalMilliseconds.ToString("f2")));


                    first_run = false;
                    //剩余张数的显示
                    if (has_folder == true)
                    {
                        textBox2.Invoke(new Action(() => textBox2.Text = (names_use.Count - count_batch).ToString()));
                    }
                    else if (has_folder == false)
                    {
                        textBox2.Invoke(new Action(() => textBox2.Text = (dir.Length - count_batch).ToString()));
                    }
                    

                    stpwth2.Restart();
                    if (has_folder == false)
                    {
                        await task_chromatic(image_high, dir[0]);
                    }
                    else if (has_folder == true)
                    {
                        await task_chromatic(image_high, path_scr_batch + names_use[0]);
                    }


                }
                else if ((count_batch < names_use.Count && enable_process == true) || measure.has_error == true || (count_batch < dir.Length && enable_process == true))
                {
                    try
                    {
                        Thread.Sleep(1);

                        //处理完成状态标志改写
                        enable_process = false;

                        //显示当前文件夹的名称
                        if (has_folder == true)
                        {
                            textBox1.Invoke(new Action(() => textBox1.Text = names_use[count_batch - 1]));
                        }
                        

                        stpwth1.Restart();
                        //这里0表示高角度图像，1表示低角度图像
                        //image_low = new Mat(ImagePaths[count_batch][1], ImreadModes.Grayscale);
                        if (has_folder == false)
                        {
                            image_high = new Mat(dir[count_batch], ImreadModes.Color);
                        }
                        else if (has_folder == true)
                        {
                            image_high = new Mat(ImagePaths[count_batch][0], ImreadModes.Color);
                        }
                        stpwth1.Stop();
                        TimeSpan ts3 = stpwth1.Elapsed;
                        textBox10.Invoke(new Action(() => textBox10.Text = ts3.TotalMilliseconds.ToString("f2")));


                        stpwth2.Restart();
                        if (has_folder == false)
                        {
                            await task_chromatic(image_high, dir[count_batch]);
                        }
                        else if (has_folder == true)
                        {
                            await task_chromatic(image_high, path_scr_batch + names_use[count_batch]);
                        }
                        


                        count_batch++;

                        //剩余张数的显示
                        if (has_folder == true)
                        {
                            textBox2.Invoke(new Action(() => textBox2.Text = (names_use.Count - count_batch).ToString()));
                        }
                        else if (has_folder == false)
                        {
                            textBox2.Invoke(new Action(() => textBox2.Text = (dir.Length - count_batch).ToString()));
                        }


                        // 完成标志
                        if (has_folder == true)
                        {
                            if (count_batch == names_use.Count)
                            {
                                label7.Invoke(new Action(new Action(() => label7.Visible = true)));
                                label7.Invoke(new Action(new Action(() => label7.BackColor = Color.Green)));
                                break;
                            }
                        }
                        

                        GC.Collect();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        count_batch++;
                    }

                }

            }

        }



        


        //手动换型
        private void button6_Click(object sender, EventArgs e)
        {
            MessageBox.Show("换型成功！");

            Initi();
            Create_Excel();
            textBox4.Text = "";
            textBox12.Text = "";
            textBox13.Text = "";
            textBox14.Text = "";
            textBox15.Text = "";
            textBox16.Text = "";
            textBox17.Text = "";
            textBox18.Text = "";
            textBox19.Text = "";
            textBox20.Text = "";
            textBox21.Text = "";
            textBox22.Text = "";
            textBox23.Text = "";
        }



        //初始化，只要是重新计算色度均值，用于手动换型
        public void Initi()
        {
            num = 1;
            num_digital_screen= 1;

            checkBox3.Checked= true;
            checkBox4.Checked= false;

            use_cat = true;
            Complex_patterns = false;
            is_stable = false;
            first_Ceramics = true;
            start_Pattern_Matching = false;
            Ceramics_info_list.Clear();
            color_num = 1;
            color_num_max = 1;
            pattern_num = "0";
            is_first_color = true;
            result_count = 0;
            aberration_count = 0;
            num_stable = 0;
            list_LAB_A.Clear();
            list_LAB_B.Clear();
            list_LAB_L.Clear();
            list_Hamming_Distance.Clear();
            mean_HSV_H = 0;
            mean_LAB_L = 0;
            mean_LAB_A = 0;
            mean_LAB_B = 0;
            mean_BGR_B = 0;
            mean_BGR_G = 0;
            mean_BGR_R = 0;
            mean_Delta_E_pure= 0;
            mean_Delta_E_texture= 0;
            diff_HSV_H = 0;
            diff_LAB_L = 0;
            diff_LAB_A = 0;
            diff_LAB_B = 0;
            diff_BGR_B = 0;
            diff_BGR_G = 0;
            diff_BGR_R = 0;
            diff_B = 0;
            diff_G = 0;
            diff_R = 0;
            diff_Delta_E = 0;
            diff_Delta_E_pure= 0;
            diff_Delta_E_texture = 0;
            sigma_LAB_A = 0;
            sigma_LAB_B = 0;
            sigma_LAB_L = 0;
            sum_HSV_H = 0;
            sum_LAB_L = 0;
            sum_LAB_A = 0;
            sum_LAB_B = 0;
            sum_BGR_B = 0;
            sum_BGR_G = 0;
            sum_BGR_R = 0;
            sum_Delta_E_pure= 0;
            sum_Delta_E_texture= 0;
            Delta_E_pure= 0;
            Delta_E_texture= 0;
            cosTheta = 0;
            deltaE = 0;
            first_Delta_E = 0;
            first_LAB_L = 0;
            num_Delta_E = 0;

            num_1 = 0;
            num_2 = 0;
            num_3 = 0;
            num_4 = 0;
            num_5 = 0;
            num_6 = 0;
            num_7 = 0;
            num_8 = 0;
            num_9 = 0;
            num_10 = 0;
            num_11 = 0;
            num_12 = 0;

            temp_color_num1 = 0;
            temp_color_num2 = 0;
            temp_color_num3 = 0;
            temp_color_num4 = 0;
            temp_color_num5 = 0;
            temp_color_num6 = 0;
            temp_color_num7 = 0;
            temp_color_num8 = 0;
            temp_color_num9 = 0;
            temp_color_num10 = 0;
            temp_color_num11 = 0;
            temp_color_num12 = 0;
            temp_color_num13 = 0;
            temp_color_num14 = 0;
            temp_color_num15 = 0;
            temp_color_num16 = 0;
            temp_color_num17 = 0;
            temp_color_num18 = 0;
            temp_color_num19 = 0;
            temp_color_num20 = 0;
            temp_color_num21 = 0;
            temp_color_num22 = 0;
            temp_color_num23 = 0;
            temp_color_num24 = 0;
            temp_color_num25 = 0;
            temp_color_num26 = 0;

            is_first1 = true;
            is_first2 = true;
            is_first3 = true;
            is_first4 = true;
            is_first5 = true;
            is_first6 = true;
            is_first7 = true;
            is_first8 = true;
            is_first9 = true;
            is_first10 = true;
            is_first11 = true;
            is_first12 = true;
            is_first13 = true;
            is_first14 = true;
            is_first15 = true;
            is_first16 = true;
            is_first17 = true;
            is_first18 = true;
            is_first19 = true;
            is_first20 = true;
            is_first21 = true;
            is_first22 = true;
            is_first23 = true;
            is_first24 = true;
            is_first25 = true;
            is_first26 = true;

            count_color1 = 0;
            count_color2 = 0;
            count_color3 = 0;
            count_color4 = 0;
            count_color5 = 0;
            count_color6 = 0;
            count_color7 = 0;
            count_color8 = 0;
            count_color9 = 0;
            count_color10 = 0;
            count_color11 = 0;
            count_color12 = 0;
            count_color13 = 0;
            count_color14 = 0;
            count_color15 = 0;
            count_color16 = 0;
            count_color17 = 0;
            count_color18 = 0;
            count_color19 = 0;
            count_color20 = 0;
            count_color21 = 0;
            count_color22 = 0;
            count_color23 = 0;
            count_color24 = 0;
            count_color25 = 0;
            count_color26 = 0;



            //版型复杂程度判断，在换型时也要将相关参数恢复默认
            Pattern_ok = false;
            num_hash = 0;
            Hamming_Distances = 0;
            measure.count_pattern = 0;
            measure.Pattern_Judgment = true;
            measure.means = 0;
            measure.means2 = 0;
            measure.means4 = 0;
            measure.stddevs = 0;
            measure.stddevs8 = 0;
            pattern = "";

            //灵敏度默认值设定
            trackBar1.Value = 6;

            Cancel_main_color(null, null);        //取消主色
            Cancel_color_blocking(null, null);    //取消拼色
        }


        //求数组的均值和方差
        static double GetSigma(List<double> dataList)
        {
            var u = dataList.Average(); //平均值
            var sum = dataList.Sum(p => Math.Pow(p - u, 2));
            var sigma = Math.Sqrt(sum / (dataList.Count));
            return sigma;
        }



        //求色差ΔE，L通道在砖表面有花纹时会比较乱，而对于纯色砖则相关性较大，偏红则L更大图像更亮，偏青则L更小图像更暗
        static double Get_Delta_E_texture(double LAB_L, double LAB_A, double LAB_B)
        {
            //return Math.Sqrt(LAB_A * LAB_A * 1.6 - LAB_B * LAB_B * 0.8);
            return Math.Sqrt(0.01 * LAB_L * LAB_L + 2 * LAB_A * LAB_A + 2 * LAB_B * LAB_B);
        }


        //利用76公式计算砖的色差
        static double Get_Delta_E_pure(double LAB_L, double LAB_A, double LAB_B)
        {
            //return Math.Sqrt(LAB_A * LAB_A * 1.6 - LAB_B * LAB_B * 0.8);
            return Math.Sqrt(0.01 * LAB_L * LAB_L + 2 * LAB_A * LAB_A + 2 * LAB_B * LAB_B);
        }


        // 定义 LabColor 结构
        public struct LabColor
        {
            public double L { get; set; }
            public double A { get; set; }
            public double B { get; set; }

            public LabColor(double l, double a, double b)
            {
                L = l;
                A = a;
                B = b;
            }
        }


        public double Calculate_angle(LabColor lab_current, LabColor lab_templeate)
        {
            double angle = Math.Acos((lab_current.L * lab_templeate.L + lab_current.A * lab_templeate.A + lab_current.B * lab_templeate.B) / (Math.Sqrt(lab_current.L * lab_current.L + lab_current.A * lab_current.A + lab_current.B * lab_current.B) * Math.Sqrt(lab_templeate.L * lab_templeate.L + lab_templeate.A * lab_templeate.A + lab_templeate.B * lab_templeate.B))) * 180 / Math.PI;

            return angle;
        }


        //CIEDE2000计算方式
        public static double CalculateCIEDE2000(LabColor lab_current, LabColor lab_templeate)
        {
            //根据具体业务需求调整kL、kC、kH参数（一般设置为1）。
            double kL = 2; double kC = 0.8; double kH = 0.8;

            double L1 = lab_current.L, a1 = lab_current.A, b1 = lab_current.B;
            double L2 = lab_templeate.L, a2 = lab_templeate.A, b2 = lab_templeate.B;

            // 计算色度
            double C1 = Math.Sqrt(a1 * a1 + b1 * b1);
            double C2 = Math.Sqrt(a2 * a2 + b2 * b2);
            double Cbar = (C1 + C2) / 2.0;

            // 色相计算
            double G = 0.5 * (1 - Math.Sqrt(Math.Pow(Cbar, 7) / (Math.Pow(Cbar, 7) + Math.Pow(25, 7))));
            double a1Prime = (1 + G) * a1;
            double a2Prime = (1 + G) * a2;

            double C1Prime = Math.Sqrt(a1Prime * a1Prime + b1 * b1);
            double C2Prime = Math.Sqrt(a2Prime * a2Prime + b2 * b2);

            double h1Prime = Math.Atan2(b1, a1Prime);
            if (h1Prime < 0) h1Prime += 2 * Math.PI;
            double h2Prime = Math.Atan2(b2, a2Prime);
            if (h2Prime < 0) h2Prime += 2 * Math.PI;

            // 计算 ΔL', ΔC', ΔH'
            double deltaLPrime = L2 - L1;
            double deltaCPrime = C2Prime - C1Prime;

            double deltahPrime = h2Prime - h1Prime;
            if (Math.Abs(deltahPrime) > Math.PI)
            {
                deltahPrime += deltahPrime > 0 ? -2 * Math.PI : 2 * Math.PI;
            }

            double deltaHPrime = 2 * Math.Sqrt(C1Prime * C2Prime) * Math.Sin(deltahPrime / 2.0);

            // 中间计算
            double Lbar = (L1 + L2) / 2.0;
            double CbarPrime = (C1Prime + C2Prime) / 2.0;
            double hbarPrime = (h1Prime + h2Prime) / 2.0;
            if (Math.Abs(h1Prime - h2Prime) > Math.PI)
            {
                hbarPrime += hbarPrime < Math.PI ? Math.PI : -Math.PI;
            }

            double T = 1 - 0.17 * Math.Cos(hbarPrime - Math.PI / 6) +
                       0.24 * Math.Cos(2 * hbarPrime) +
                       0.32 * Math.Cos(3 * hbarPrime + Math.PI / 30) -
                       0.20 * Math.Cos(4 * hbarPrime - Math.PI / 3);

            double SL = 1 + (0.015 * Math.Pow(Lbar - 50, 2)) / Math.Sqrt(20 + Math.Pow(Lbar - 50, 2));
            double SC = 1 + 0.045 * CbarPrime;
            double SH = 1 + 0.015 * CbarPrime * T;

            double deltaTheta = 30 * Math.Exp(-Math.Pow((hbarPrime - Math.PI / 6) / (Math.PI / 30), 2));
            double RC = 2 * Math.Sqrt(Math.Pow(CbarPrime, 7) / (Math.Pow(CbarPrime, 7) + Math.Pow(25, 7)));
            double RT = -RC * Math.Sin(2 * deltaTheta);

            // 最终 ΔE00 计算
            double deltaE00 = Math.Sqrt(
                Math.Pow(deltaLPrime / (SL*kL), 2) +
                Math.Pow(deltaCPrime / (SC*kC), 2) +
                Math.Pow(deltaHPrime / (SH*kH), 2) +
                RT * (deltaCPrime / (SC*kC)) * (deltaHPrime / (SH*kH))
            );

            return deltaE00;
        }


        //获得哈希编码后的两幅图像之间的汉明距离
        public static int Get_Hamming_Distance(int[] a, int[] b)
        {
            int num = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if ((a[i] ^ b[i]) == 1)
                {
                    num++;
                }
            }

            return num;
        }



        //窗体定时器，用来刷新显示和更新参数,轮训界面按钮状态
        private void timer1_Tick_1(object sender, EventArgs e)
        {
            //if (Online == true)
            //{
            //    // 获取当前相机的曝光值
            //    form4.m_AcqDevice.GetFeatureValue("ExposureTime", out DALSA_Info.ExposureValue);
            //}


            //判断当前的灵敏度
            switch (trackBar1.Value)
            {
                case 1:  sensitivity_color = 1.5; sensitivity_luminance = 1.8; break;
                case 2:  sensitivity_color = 1.4; sensitivity_luminance = 1.6; break;
                case 3:  sensitivity_color = 1.3; sensitivity_luminance = 1.4; break;
                case 4:  sensitivity_color = 1.2; sensitivity_luminance = 1.3; break;
                case 5:  sensitivity_color = 1.1; sensitivity_luminance = 1.2; break;
                case 6:  sensitivity_color = 1.0; sensitivity_luminance = 1.0; break;
                case 7:  sensitivity_color = 0.9; sensitivity_luminance = 0.92; break;
                case 8:  sensitivity_color = 0.8; sensitivity_luminance = 0.86; break;
                case 9:  sensitivity_color = 0.7; sensitivity_luminance = 0.80; break;
                case 10: sensitivity_color = 0.6; sensitivity_luminance = 0.70; break;
                case 11: sensitivity_color = 0.5; sensitivity_luminance = 0.60; break;

            }
            textBox25.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;  //设置字体居中显示
            textBox25.Text =  sensitivity_color.ToString();
            //Console.WriteLine(sensitivity);


            //显示系统运行总时长
            double days = stpwth.Elapsed.Days; // 总天数
            double hours = stpwth.Elapsed.Hours; // 总小时
            double minutes = stpwth.Elapsed.Minutes; // 总分钟
            double seconds = stpwth.Elapsed.Seconds; // 总秒数
            textBox26.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;  //设置字体居中显示
            textBox26.Text = $"{days}天{hours}时{minutes}分{seconds}秒";


            if (Info_calibration.is_calibration == true && Info_calibration.flag_calibration == true)
            {//如果点击标定按钮，则隐藏模板匹配按钮

                checkBox3.Hide();
            }
            else if (Info_calibration.is_calibration == false && Info_calibration.flag_calibration == false)
            {
                checkBox3.Show();
            }


            //是否保存原图判断
            if (checkBox1.Checked == true)
            {
                Is_Save_Original_Image = true;
            }
            else
            {
                Is_Save_Original_Image = false; 
            }


            //处理本地图库时判断是否有文件夹的嵌套
            if (checkBox2.Checked == true)
            {
                Is_Have_Folder_Nest = true;
            }
            else
            {
                Is_Have_Folder_Nest= false;
            }


            //判断是否需要模板匹配功能
            if (checkBox3.Checked == true)
            {
                Is_Template_Matching = true;
            }
            else
            {
                Is_Template_Matching = false;
            }


            //判断是否需要保存有色差的图像
            if (checkBox4.Checked == true)
            {
                Is_Save_NG_Image = true;
            }
            else
            {
                Is_Save_NG_Image = false;
            }


            //考虑到保存的有偏差的图像是处理后的，所以加这个状态用来区分
            if (checkBox5.Checked == true)
            {
                Return_Info_calibration.not_original_picture = true;
            }
            else
            {
                Return_Info_calibration.not_original_picture= false;
            }


        }



        //处理结果延迟显示的事件处理器
        Mat flag = new Mat(10, 10, MatType.CV_8UC1, 255);
        static int count_tick = 0;

        private void timer1_Tick(object sender, EventArgs e)
        {
            Action action = () =>
            {
                string time = DateTime.Now.ToString("yyyyMMddHHmmss.fff");
                Cv2.ImWrite(pathColorDifComu + "\\" + (count_tick + 1) + "#" + flag_list[count_tick].Item1 + "#" + time + ".jpg", flag); //保存用于缺陷界面显示的标志位图像

                //Thread.Sleep(1);
                //界面上图像的显示
                Mat show = Cv2.ImRead(address_show + "\\" + "num_" + (count_tick+1).ToString() + ".jpg", ImreadModes.Color);
                if (Is_Lock_Interface == false)
                {
                    pictureBox1.Image = null;
                    pictureBox1.Image = BitmapConverter.ToBitmap(show);
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                }
                File.Delete(address_show + "\\" + "num_" + (count_tick + 1).ToString() + ".jpg");  //是否删除用于界面显示的图像


                //界面上当前图像对应的色号以及色值结果显示
                if (Is_Lock_Interface == false)
                {
                    textBox4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;  //设置字体居中显示

                    try
                    {
                        if (flag_list[count_tick].Item1 != "" && (Convert.ToInt32(flag_list[count_tick].Item1) > 1 || Convert.ToInt32(flag_list[count_tick].Item1) ==0))
                        {
                            textBox4.BackColor = Color.Red;    //对于有色差的砖，用于显示色号的文本框背景置红
                        }
                        else
                        {
                            textBox4.BackColor = Color.Green;   //正常色号砖，用绿色背景来显示
                        }
                    }
                    catch
                    {
                        textBox4.BackColor = Color.Red;    //有error的时候，背景是红色
                    }
                    
                    textBox4.Text = flag_list[count_tick].Item1;
                    Num_Color.num = flag_list[count_tick].Item1;   // 用于现场工人使用界面色号的显示

                    textBox6.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;  //设置字体居中显示
                    textBox6.Text = flag_list[count_tick].Item4.ToString("f2");
                    textBox7.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;  //设置字体居中显示
                    textBox7.Text = flag_list[count_tick].Item6.ToString("f2");
                    textBox8.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;  //设置字体居中显示
                    textBox8.Text = flag_list[count_tick].Item7.ToString("f2");
                    textBox24.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;  //设置字体居中显示
                    textBox24.Text = flag_list[count_tick].Item3.ToString("f2");
                    textBox27.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;  //设置字体居中显示
                    textBox27.Text = flag_list[count_tick].Item5.ToString("f2");

                    label34.Text = flag_list[count_tick].Rest.Item1;
                }


                //string str_num = string.Format("{0:d5}", Convert.ToInt32(num_digital_screen));
                //string str_color_num = string.Format("{0:d2}", Convert.ToInt32(flag_list[count_tick].Item1));
                //string msgOrder = $"{'$' + "001," + str_num + str_color_num + '#'}";    //数码屏通信规则
                //serialPort.Write(msgOrder);


                //enable_process = true;

                first_run = false;
                //Cv2.ImWrite(MesgDir + "\\" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_num" + (count_tick + 1) + ".jpg", flag);
                count_tick++;
                num_digital_screen++;
            };
            this.Invoke(action);

            

        }



        //拼色操作
        private void button8_Click(object sender, EventArgs e)
        {
            Operate_merge_color.two_to_what = true;
            //button8.Enabled = false;
            button8.BackColor = Color.Gray;
            //MessageBox.Show("设置成功！");

            Form5 form5 = new Form5();//拼色窗体的初始化
            form5.Show();

        }


        private void button9_Click(object sender, EventArgs e)
        {
            Operate_merge_color.three_to_what = true;
            //button9.Enabled = false;
            button9.BackColor = Color.Gray;
            //MessageBox.Show("设置成功！");

            Form5 form5 = new Form5();//拼色窗体的初始化
            form5.Show();
        }


        private void button10_Click(object sender, EventArgs e)
        {
            Operate_merge_color.four_to_what = true;
            //button10.Enabled = false;
            button10.BackColor = Color.Gray;
            //MessageBox.Show("设置成功！");

            Form5 form5 = new Form5();//拼色窗体的初始化
            form5.Show();
        }


        private void button11_Click(object sender, EventArgs e)
        {
            Operate_merge_color.five_to_what = true;
            //button11.Enabled = false;
            button11.BackColor = Color.Gray;
            //MessageBox.Show("设置成功！");

            Form5 form5 = new Form5();//拼色窗体的初始化
            form5.Show();
        }



        private void button12_Click(object sender, EventArgs e)
        {
            Operate_merge_color.six_to_what = true;
            //button12.Enabled = false;
            button12.BackColor = Color.Gray;
            //MessageBox.Show("设置成功！");

            Form5 form5 = new Form5();//拼色窗体的初始化
            form5.Show();
        }


        private void button19_Click(object sender, EventArgs e)
        {
            Operate_merge_color.seven_to_what = true;
            //button19.Enabled = false;
            button19.BackColor = Color.Gray;
            //MessageBox.Show("设置成功！");

            Form5 form5 = new Form5();//拼色窗体的初始化
            form5.Show();
        }


        private void button13_Click(object sender, EventArgs e)
        {
            Operate_merge_color.eight_to_what = true;
            //button13.Enabled = false;
            button13.BackColor = Color.Gray;
            //MessageBox.Show("设置成功！");

            Form5 form5 = new Form5();//拼色窗体的初始化
            form5.Show();
        }



        private void button14_Click(object sender, EventArgs e)
        {
            Operate_merge_color.nine_to_what = true;
            //button14.Enabled = false;
            button14.BackColor = Color.Gray;
            //MessageBox.Show("设置成功！");

            Form5 form5 = new Form5();//拼色窗体的初始化
            form5.Show();
        }


        private void button15_Click(object sender, EventArgs e)
        {
            Operate_merge_color.ten_to_what = true;
            //button15.Enabled = false;
            button15.BackColor = Color.Gray;
            //MessageBox.Show("设置成功！");

            Form5 form5 = new Form5();//拼色窗体的初始化
            form5.Show();
        }


        private void button16_Click(object sender, EventArgs e)
        {
            Operate_merge_color.eleven_to_what = true;
            //button16.Enabled = false;
            button16.BackColor = Color.Gray;
            //MessageBox.Show("设置成功！");

            Form5 form5 = new Form5();//拼色窗体的初始化
            form5.Show();
        }



        private void button17_Click(object sender, EventArgs e)
        {
            Operate_merge_color.twelve_to_what = true;
            //button17.Enabled = false;
            button17.BackColor = Color.Gray;
            //MessageBox.Show("设置成功！");

            Form5 form5 = new Form5();//拼色窗体的初始化
            form5.Show();
        }



        //计数清零
        private void button28_Click(object sender, EventArgs e)
        {
            textBox9.Text = "";
            num = 1;
        }


        //标定窗体的初始化
        private void button26_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3();
            form3.Show();
        }


        //打开dalsa线扫SDK的界面
        private void button27_Click(object sender, EventArgs e)
        {
            form4 = new Form4();
            form4.Show();
        }


        //设置主色
        private void button18_Click(object sender, EventArgs e)
        {
            Set_Red_Main = true;
            button18.Enabled= false;
            button18.BackColor = Color.Gray;
            MessageBox.Show("设置成功！");
        }



        //取消主色
        private void button20_Click(object sender, EventArgs e)
        {
            Set_Red_Main = false;
            button18.BackColor = Color.LightSteelBlue;
            button18.Enabled= true;
            MessageBox.Show("设置成功！");
        }
        private void Cancel_main_color(object sender, EventArgs e)    //与上面功能一致，只是没有了消息框
        {
            Set_Red_Main = false;
            button18.BackColor = Color.LightSteelBlue;
            button18.Enabled = true;
        }
        



        //取消拼色
        private void button24_Click(object sender, EventArgs e)
        {
            Operate_merge_color.two_to_what = false; 
            Operate_merge_color.three_to_what = false; 
            Operate_merge_color.four_to_what = false; 
            Operate_merge_color.five_to_what = false; 
            Operate_merge_color.six_to_what = false; 
            Operate_merge_color.seven_to_what = false; 
            Operate_merge_color.eight_to_what = false; 
            Operate_merge_color.nine_to_what = false;
            Operate_merge_color.ten_to_what = false; 
            Operate_merge_color.eleven_to_what = false; 
            Operate_merge_color.twelve_to_what = false;

            Info_merge_color.two_merge_what = 0;
            Info_merge_color.three_merge_what = 0;
            Info_merge_color.four_merge_what = 0;
            Info_merge_color.five_merge_what = 0;
            Info_merge_color.six_merge_what = 0;
            Info_merge_color.seven_merge_what = 0;
            Info_merge_color.eight_merge_what = 0;
            Info_merge_color.nine_merge_what = 0;
            Info_merge_color.ten_merge_what = 0;
            Info_merge_color.eleven_merge_what = 0;
            Info_merge_color.twelve_merge_what = 0;

            button8.Enabled = true;
            button8.BackColor = Color.LightSteelBlue;
            button9.Enabled = true;
            button9.BackColor = Color.LightSteelBlue;
            button10.Enabled = true;
            button10.BackColor= Color.LightSteelBlue;
            button11.Enabled = true;
            button11.BackColor = Color.LightSteelBlue;
            button12.Enabled = true;
            button12.BackColor = Color.LightSteelBlue;
            button13.Enabled = true;
            button13.BackColor = Color.LightSteelBlue;
            button14.Enabled = true;
            button14.BackColor = Color.LightSteelBlue;
            button15.Enabled = true;
            button15.BackColor = Color.LightSteelBlue;
            button16.Enabled = true;
            button16.BackColor= Color.LightSteelBlue;
            button17.Enabled = true;
            button17.BackColor= Color.LightSteelBlue;
            button19.Enabled = true;
            button19.BackColor= Color.LightSteelBlue;

            MessageBox.Show("设置成功！");
        }
        private void Cancel_color_blocking(object sender, EventArgs e)    //与上面功能一致，只是没有了消息框
        {
            Operate_merge_color.two_to_what = false;
            Operate_merge_color.three_to_what = false;
            Operate_merge_color.four_to_what = false;
            Operate_merge_color.five_to_what = false;
            Operate_merge_color.six_to_what = false;
            Operate_merge_color.seven_to_what = false;
            Operate_merge_color.eight_to_what = false;
            Operate_merge_color.nine_to_what = false;
            Operate_merge_color.ten_to_what = false;
            Operate_merge_color.eleven_to_what = false;
            Operate_merge_color.twelve_to_what = false;

            Info_merge_color.two_merge_what = 0;
            Info_merge_color.three_merge_what = 0;
            Info_merge_color.four_merge_what = 0;
            Info_merge_color.five_merge_what = 0;
            Info_merge_color.six_merge_what = 0;
            Info_merge_color.seven_merge_what = 0;
            Info_merge_color.eight_merge_what = 0;
            Info_merge_color.nine_merge_what = 0;
            Info_merge_color.ten_merge_what = 0;
            Info_merge_color.eleven_merge_what = 0;
            Info_merge_color.twelve_merge_what = 0;


            button8.Enabled = true;
            button8.BackColor = Color.LightSteelBlue;
            button9.Enabled = true;
            button9.BackColor = Color.LightSteelBlue;
            button10.Enabled = true;
            button10.BackColor = Color.LightSteelBlue;
            button11.Enabled = true;
            button11.BackColor = Color.LightSteelBlue;
            button12.Enabled = true;
            button12.BackColor = Color.LightSteelBlue;
            button13.Enabled = true;
            button13.BackColor = Color.LightSteelBlue;
            button14.Enabled = true;
            button14.BackColor = Color.LightSteelBlue;
            button15.Enabled = true;
            button15.BackColor = Color.LightSteelBlue;
            button16.Enabled = true;
            button16.BackColor = Color.LightSteelBlue;
            button17.Enabled = true;
            button17.BackColor = Color.LightSteelBlue;
            button19.Enabled = true;
            button19.BackColor = Color.LightSteelBlue;
        }



        //延迟时间调整，单位ms
        private void button21_Click(object sender, EventArgs e)
        {
            Time_Delay += 500;
            MessageBox.Show("设置成功！");
        }


        private void button22_Click(object sender, EventArgs e)
        {
            Time_Delay -= 500;
            MessageBox.Show("设置成功！");
        }



        //标定程序
        private void button23_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();//标定窗体的初始化
            form2.Show();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            AutoSize = new AutoAdaptWindowsSize(this);
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //窗体大小改变事件
            if (AutoSize != null) // 一定加这个判断，电脑缩放布局不是100%的时候，会报错
            {
                AutoSize.FormSizeChanged();
            }
        }



        //冻结窗体，不刷新
        private void button25_Click(object sender, EventArgs e)
        {
            if (button25.Text == "锁定界面")
            {
                button25.Text = "取消锁定";
                Is_Lock_Interface = true;
            }
            else if (button25.Text == "取消锁定")
            {
                button25.Text = "锁定界面";
                Is_Lock_Interface = false;
            }
            
        }



        private void open_SerialPort()
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                MessageBox.Show("本机没有串口！");
            }


            Array.Sort(ports);
            serialPort.PortName = ports[1];
            serialPort.BaudRate = 9600;//波特率
            serialPort.DataBits = 8;//数据位
            serialPort.StopBits = System.IO.Ports.StopBits.One;//停止位
            serialPort.Encoding = System.Text.Encoding.GetEncoding("GB2312");//此行非常重要，解决接收中文乱码的问题


            // 打开串口
            try
            {
                serialPort.Open();
            }
            catch (Exception ex)
            {
                //捕获到异常信息，创建一个新的comm对象，之前的不能用了。  
                serialPort = new System.IO.Ports.SerialPort();
                //将异常信息传递给用户。  
                MessageBox.Show(ex.Message);
                return;
            }
        }


        //用于数码屏接收消息的事件处理器
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            this.Invoke(new EventHandler(UpdateUIText));
        }

        private void UpdateUIText(object sender, EventArgs e)
        {
            try
            {
                System.Threading.Thread.Sleep(500);
                string txt = serialPort.ReadExisting();
                textBox2.Text = txt;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }



    //返回的标定信息，以及是否测试非原图的状态
    public struct Return_Info_calibration
    {
        public static double count;                         //计数
        public static bool is_complete;                     //是否完成标定
        public static bool is_start_calibration = false;    //是否开始标定
        public static bool not_original_picture = false;    //所处理的图像是相机的原图还是处理后的只是砖部分的图像
    }



    //用于从线扫相机SDK获取参数的结构体，曝光，增益，行高等等
    public struct DALSA_Info
    {
        public static double ExposureValue;   //获取相机的曝光时间
    }



    //并色操作
    public struct Operate_merge_color
    {
        public static bool two_to_what = false; 
        public static bool three_to_what = false; 
        public static bool four_to_what = false; 
        public static bool five_to_what = false; 
        public static bool six_to_what = false; 
        public static bool seven_to_what = false; 
        public static bool eight_to_what = false; 
        public static bool nine_to_what = false; 
        public static bool ten_to_what = false; 
        public static bool eleven_to_what = false; 
        public static bool twelve_to_what = false;                    //用来判断并色操作的状态按钮

       
    }

}
