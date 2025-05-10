using OpenCvSharp;
using OpenCvSharp.XFeatures2D;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using Template_Matching;
using OpenCvSharp.Extensions;



namespace template_test
{
    public delegate void delegate_result_return(Mat img_show, double time_consuming, string name, int[] Hash_Code, int[] Hash_Code_rotate, Mat DstImg);
    public delegate void delegate_error_return(double time_consuming, string name);


    public partial class Form1 : Form
    {
        template_match match = new template_match();

        public event delegate_result_return event_result_return;
        public event delegate_error_return event_error_return;

        //界面按钮状态
        bool Is_Have_Folder_Nest = false;
        bool has_folder = true;           //读取本地图像时，单张图像是否有文件夹嵌套
        string path_scr_batch;                   //批量处理本地图库时地址
        string[] dir = null;
        string[] names;
        List<string> names_use = new List<string>();
        List<string> strings = new List<string>();
        List<List<string>> ImagePaths = new List<List<string>>();
        public bool first_run = true;            //判断是否第一次进处理线程

        //高低角度图像是否处理完成标志
        public bool enable_process = false;

        Stopwatch stpwth1 = new Stopwatch();     //内存读取计时器,或者本地图像的读取耗时
        Stopwatch stpwth2 = new Stopwatch();     //处理图像的耗时
        Stopwatch stpwth = new Stopwatch();      //运行总时长

        Mat image_high = new Mat();       //离线工作时，本地读取的高角度图像
        int count_batch = 1;              //本地处理图像时计数用

        public int result_count = 0;
        bool first_Ceramics = true;       //处理结果汇总时，判断是否是第一块砖
        string pattern = "";       //砖的版型
        //用于砖型复杂系数二次判定的相关变量
        public int num_hash = 0;

        double k = 0.1;
        bool start_Pattern_Matching = true;       //是否启动模板匹配的色差计算
        public double Hamming_Distances = 0;
        List<double> list_Hamming_Distance = new List<double>();
        //陶瓷的版型，原图哈希编码，出现次数，总色差值ΔE，平均色差值，总的L通道值，平均L通道值，旋转180°图哈希编码

        string pattern_num = "0";
        string address_show = System.IO.Directory.GetCurrentDirectory().Split(new String[] { @"\bin" }, StringSplitOptions.None)[0] + @"\界面显示图";
        bool Online = false;              //判断当前是在线还是离线运行,区分主要是为了跑本地图库时显示文件夹的名称
        List<Tuple<string>> flag_list = new List<Tuple<string>>();    //用于界面信息显示（色号，耗时，色度E差值，H通道值，亮度L差值，A通道值，B通道值，版型）的列表
        System.Timers.Timer[] Timers = new System.Timers.Timer[100000000];      //延时显示与结果记录的Timer
        static int Time_Delay = 2000;     //设置延时时间，单位ms
        bool Is_Lock_Interface = false;


        public Form1()
        {
            InitializeComponent();

            event_result_return += result_show;

            //创建用于保存结果和小图合集的文件夹
            if (Directory.Exists(address_show) == false)
            {
                Directory.CreateDirectory(address_show);
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {

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

                                if (j == 2)
                                {
                                    break;
                                }
                            }
                        }
                        if (strings.Count == 2)
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
                    path_scr_batch = textBox3.Text.ToString();
                    //dir = Directory.GetFiles(path_scr_batch + "\\").OrderBy(file => File.GetLastWriteTime(file)).ToArray();
                    dir = Directory.GetFiles(path_scr_batch + "\\", "*.jpg").OrderBy(path => ExtractSequenceNumber(path)).ToArray();

                }).Invoke();
            }



            //开启工作线程
            Thread thd = new Thread(task_process);
            thd.IsBackground = true;
            thd.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            //处理本地图库时判断是否有文件夹的嵌套
            if (checkBox2.Checked == true)
            {
                Is_Have_Folder_Nest = true;
            }
            else
            {
                Is_Have_Folder_Nest = false;
            }
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
                        image_high = new Mat(dir[0], ImreadModes.AnyColor);
                    }
                    else if (has_folder == true)
                    {
                        image_high = new Mat(ImagePaths[0][1], ImreadModes.AnyColor);
                    }
                    //image_low = new Mat(ImagePaths[0][1], ImreadModes.Grayscale);
                    stpwth1.Stop();
                    TimeSpan ts2 = stpwth1.Elapsed;
                    textBox10.Invoke(new Action(() => textBox10.Text = ts2.TotalMilliseconds.ToString("f2")));

                    //Cv2.ImWrite(count_batch + ".jpg", image_high);

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
                else if ((count_batch < names_use.Count && enable_process == true) || has_error == true || (count_batch < dir.Length && enable_process == true))
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
                            image_high = new Mat(dir[count_batch], ImreadModes.AnyColor);
                        }
                        else if (has_folder == true)
                        {
                            image_high = new Mat(ImagePaths[count_batch][1], ImreadModes.AnyColor);
                        }
                        stpwth1.Stop();
                        TimeSpan ts3 = stpwth1.Elapsed;
                        textBox10.Invoke(new Action(() => textBox10.Text = ts3.TotalMilliseconds.ToString("f2")));

                        //Cv2.ImWrite(count_batch + ".jpg", image_high);

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



        //定义色差处理任务的Task线程
        async Task task_chromatic(Mat image, string path)    //将方法标记为async后，可以在方法中使用await关键字
        {
            await Task.Delay(2);
            await Task.Run(() => run(image, path));
        }


        private object thislock = new object();
        public bool is_done = false;       //是否处理完成
        public bool has_error = false;     //处理中是否发生错误
        public Mat img_show = new Mat();   //用于界面显示的图像
        public Double time_consuming;      //处理耗时
        public int count = 0;
        string path_use;                   //处理本地文件时图像的名称
        public static double stddevs = 0;
        int[] Hash_Code;  //初始化图像的哈希编码
        int[] Hash_Code_rotate;
        public void run(Mat image_RGB, string path)
        {
            try
            {
                lock (thislock)
                {
                    GC.Collect();
                    //程序计时开始
                    stpwth.Restart();

                    //获得当前文件夹的名称
                    string[] strings = path.Split(new char[] { '\\' });
                    path_use = strings[strings.Length - 1];

                    time_consuming = 0.0;
                    is_done = false;
                    has_error = false;
                    count++;             //处理的次数，即检测次数，不会清零

                    Mat DstImg = new Mat(); //原图矫正后的图像


                    //Cv2.Resize(image_RGB, image_RGB, new Size(), 0.25, 0.25, InterpolationFlags.Area);
                    DstImg = image_RGB;
                    Scalar mean, stddev;
                    Cv2.MeanStdDev(DstImg, out mean, out stddev);
                    stddevs += stddev.Val0;


                    //1.第一步：计算第一次砖面的复杂度系数
                    if (count == 4)
                    {
                        double mean_stddev = stddevs / count;
                        match.Coefficient = match.get_coefficient_first(mean_stddev);
                    }


                    //2.第二步：进行图像的哈希编码
                    Mat img_hash = DstImg.Clone();
                    Mat img_hash_rotate = DstImg.Clone();
                    match.rotate_image(img_hash_rotate, 90);
                    match.rotate_image(img_hash_rotate, 90);
                    //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\img_hash.jpg", img_hash);
                    //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\img_hash_rotate.jpg", img_hash_rotate);


                    //同时计算原图和旋转180°后两幅图像的哈希编码，如现场有其他类似
                    Hash_Code = match.Fun_Hash_Code(img_hash);
                    Hash_Code_rotate = match.Fun_Hash_Code(img_hash_rotate);
                    img_hash.Dispose();
                    img_hash_rotate.Dispose();


                    //对图像做模糊化处理，降低噪点波动
                    Cv2.Resize(DstImg, img_show, new Size(), 1, 1, InterpolationFlags.Area);
                    //Cv2.Blur(DstImg, DstImg, new Size(33, 33));
                    //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\img_dst.jpg", DstImg);

                    
                    //统计程序耗时
                    stpwth.Stop();
                    TimeSpan ts = stpwth.Elapsed;
                    //WriteLine("崩边图像处理耗时:" + ts.TotalMilliseconds + "ms");


                    //返回结果及耗时
                    //img_show = img_dst.Clone();
                    //img_dst.Dispose();

                    time_consuming = ts.TotalMilliseconds;
                    is_done = true;


                    //处理完有结果了进事件处理器
                    if (is_done)
                    {
                        if (event_result_return != null)
                            event_result_return(img_show, time_consuming, path_use, Hash_Code, Hash_Code_rotate,  DstImg);
                    }

                    image_RGB.Dispose();
                    //DstImg.Dispose();
                    //GC.Collect();
                }
            }
            catch (Exception ex)
            {
                //throw new ArgumentException(ex.Message);

                has_error = true;
                if (event_error_return != null)
                    event_error_return(0, path_use);

                image_RGB.Dispose();
            }


        }



        //砖处理结果汇总
        private void result_show(Mat img_show, double time_consuming, string name, int[] Hash_Code, int[] Hash_Code_rotate, Mat DstImg)
        {
            result_count++;

            //砖版型的在线判断并搜集,前提是前几块砖的版型复杂程度判断完成
            if (result_count <= 16)
            {
                //3.第三步：保存第一块砖的版型和哈希编码信息
                if (first_Ceramics == true)
                {
                    first_Ceramics = false;
                    pattern = "版型1";            //第一次处理时，模板匹配的版型设置为版型1

                    match.Ceramics_info_list.Add(new Tuple<string, int[], int[]>("版型1", Hash_Code, Hash_Code_rotate));

                }
                else
                {
                    foreach (var tuple_infos in match.Ceramics_info_list)
                    {
                        //4.第四步：计算第二次砖面的复杂度系数
                        int Hamming_Distance = match.Get_Hamming_Distance(tuple_infos.Item2, Hash_Code);
                        int Hamming_Distance_rotate = match.Get_Hamming_Distance(tuple_infos.Item3, Hash_Code);

                        //砖型复杂系数的二次判定
                        num_hash++;
                        Hamming_Distances += Hamming_Distance;
                        list_Hamming_Distance.Add(Hamming_Distance);
                        if (num_hash == 15)
                        {
                            match.set_Coefficient(Hamming_Distances, list_Hamming_Distance);
                            match.Pattern_ok = true;
                        }

                    }

                }
            }
            else
            {
                //5.第五步：正式计算新来砖的版型
                pattern = match.get_pattern(Hash_Code, Hash_Code_rotate);
            }




            //判断当前色号标准是否稳定
            if (result_count >=60)
            {
                match.is_stable = true;

            }


            //把版型信息打到图像上
            if (match.Pattern_ok == true)
            {
                pattern_num = pattern.Substring(2, pattern.Length - 2);
                Cv2.PutText(img_show, "pattern" + pattern_num, new OpenCvSharp.Point(img_show.Width / 4, img_show.Height / 3), HersheyFonts.Italic, 4, new Scalar(255, 0, 0));
            }
            Cv2.ImWrite(address_show + "\\" + "num_" + count.ToString() + ".jpg", img_show); //保存用于界面图像显示的图像


            var tuple = Tuple.Create(pattern_num);
            flag_list.Add(tuple);
            enable_process = true;   //是否处理完成状态标志改写



            //延时定时器加载
            Timers[count - 1] = new System.Timers.Timer();
            Timers[count - 1].Interval = Time_Delay - (int)time_consuming;
            Timers[count - 1].Elapsed += timer1_Tick;
            Timers[count - 1].AutoReset = false;
            Timers[count - 1].Enabled = true;
            Timers[count - 1].Start();


            //单次图像处理耗时统计
            stpwth2.Stop();
            TimeSpan ts4 = stpwth2.Elapsed;
            textBox5.Invoke(new Action(() => textBox5.Text = ts4.TotalMilliseconds.ToString("f2")));


        }


        //延时定时器加载
        //处理结果延迟显示的事件处理器
        static int count_tick = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            Action action = () =>
            {
                string time = DateTime.Now.ToString("yyyyMMddHHmmss.fff");


                //Thread.Sleep(1);
                //界面上图像的显示
                Mat show = Cv2.ImRead(address_show + "\\" + "num_" + (count_tick + 1).ToString() + ".jpg", ImreadModes.Color);
                if (Is_Lock_Interface == false)
                {
                    pictureBox1.Image = null;
                    pictureBox1.Image = BitmapConverter.ToBitmap(show);
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                }
                //File.Delete(address_show + "\\" + "num_" + (count_tick + 1).ToString() + ".jpg");  //是否删除用于界面显示的图像


                //界面上当前图像对应的色号以及色值结果显示
                if (Is_Lock_Interface == false)
                {
                    
                    label34.Text = flag_list[count_tick].Item1;
                }



                //enable_process = true;

                first_run = false;
                //Cv2.ImWrite(MesgDir + "\\" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_num" + (count_tick + 1) + ".jpg", flag);
                count_tick++;

            };
            this.Invoke(action);



        }
    }
}
