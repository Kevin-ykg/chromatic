using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NPOI.SS.Formula.Functions;
using OpenCvSharp;
using static System.Console;



namespace Chromatic
{
    internal class measure
    {
        public event delegate_result_return event_result_return;
        public event delegate_error_return event_error_return;

        Stopwatch stpwth = new Stopwatch();
        private object thislock = new object();

        public bool is_done = false;       //是否处理完成
        public bool has_error = false;     //处理中是否发生错误
        public Mat img_show = new Mat();   //用于界面显示的图像
        public List<Mat> small_img_chipping = new List<Mat>();
        public Double time_consuming;      //处理耗时
        public int count = 0;
        string path_use;                   //处理本地文件时图像的名称

        static int unit = 100;  //哈希编码划分的区域，unit*unit
        public int Coefficient = 6;   //用于判断版型复杂程度的系数
        public static double stddevs = 0; public static double means = 0; public static double means2 = 0; public static double means4 = 0; public static double stddevs8 = 0; //前几块图像灰度均方差的累计和
        public double Average_gray = 0;
        public static int count_pattern = 0;
        public static bool Pattern_Judgment = true;           //图像稳定，模板匹配功能是否可以开启
        public static bool Brightness_Judgment_Once = false;   //图像的亮度判断标志位
        public static bool Brightness_Judgment_Twice = false;
        public static bool Brightness_Judgment_Third = false;
        public double ExposureValue = 30.0;   //待设定的曝光时间

        int[] Hash_Code = new int[unit * unit];  //初始化图像的哈希编码
        int[] Hash_Code_rotate = new int[unit * unit];


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

                    List<Point[]> contours_select = new List<Point[]>();
                    time_consuming = 0.0;
                    is_done = false;
                    has_error = false;
                    count++;             //处理的次数，即检测次数，不会清零
                    count_pattern++;     //用来控制是否需要调整曝光，以及当前图像是否稳定可以启动模板匹配的计数，会在Form1.Initi函数中被清零

                    Mat DstImg = new Mat(); //原图矫正后的图像

                    if (Return_Info_calibration.not_original_picture == false)  //处理相机的原图
                    {
                        //适当降低图像的分辨率来减少耗时，同时可以便面图像的高清带来的噪音对色差检测的影响
                        //Cv2.Resize(image_RGB, img_show, new Size(), 1, 1, InterpolationFlags.Area);
                        Cv2.Resize(image_RGB, image_RGB, new Size(), 0.25, 0.25, InterpolationFlags.Area);
                        //Cv2.ImWrite(@"C:\\Users\\\Lenovo\\Desktop\\test.jpg", image_RGB);


                        //二值化处理，也可以根据色系通过MeanStdDev来求灰度均值后获取合适的分割阈值，目前所选的30偏低可以兼容当前图库所有色系
                        double width = image_RGB.Width;
                        double height = image_RGB.Height;
                        double Aspect_ratio = width / height;   //长宽比
                        int thread_set = 0;
                        Mat image_mono = new Mat(image_RGB.Size(), MatType.CV_8UC1);
                        Cv2.CvtColor(image_RGB, image_mono, ColorConversionCodes.BGR2GRAY);

                        Mat test_pidai = new Mat(image_mono, new Rect(100, 10, 30, 500));   //悬空铝合金的反光带
                        Mat test = new Mat(image_mono, new Rect((int)width / 2 - (int)((height / 4) * Aspect_ratio), (int)height / 2 - (int)((width / 3) / Aspect_ratio), (int)(2 * (height / 4) * Aspect_ratio), (int)(2 * (width / 3) / Aspect_ratio)));
                        //Cv2.ImWrite(@"C:\\Users\\\Lenovo\\Desktop\\test.jpg", image_mono);

                        //求图像灰度值的均值和方差，通过选取的砖中心区域的灰度均值来判断砖的色系，根据条件选取合适的二值化分割阈值，确保砖分割边缘的准确
                        //关键问题还是在于高角度效果图时皮带背景的干扰较大，当砖的色系较暗时，不好选取二值化分割阈值，选低了皮带干扰多，选高了砖表面边缘暗色花纹容易被认为是缺陷
                        Scalar mean, stddev;
                        Cv2.MeanStdDev(test, out mean, out stddev);
                        test.Dispose();


                        if (count_pattern <= 10)  //统计前几块砖面版型的复杂程度
                        {
                            if (count_pattern <= 2)
                            {
                                means2 += mean.Val0;
                            }
                            if (count_pattern <= 4)
                            {
                                means4 += mean.Val0;
                            }
                            if (count_pattern <= 8)
                            {
                                stddevs8 += stddev.Val0;
                            }

                            stddevs += stddev.Val0;
                            means += mean.Val0;


                            if (count_pattern == 2)   //曝光的第一次判定
                            {
                                // 获取当前相机的曝光值
                                ExposureValue = DALSA_Info.ExposureValue;

                                if ((means / 2) < 50)
                                {
                                    ExposureValue += 30;
                                }
                                else if ((means / 2) >= 50 && (means / 2) < 100)
                                {
                                    ExposureValue += 25;
                                }
                                else if ((means / 2) >= 100 && (means / 2) < 150)
                                {
                                    ExposureValue += 15;
                                }
                                else if ((means / 2) >= 150 && (means / 2) < 180)
                                {
                                    ExposureValue += 5;
                                }
                                else if ((means / 2) >= 180 && (means / 2) < 250)
                                {
                                    ExposureValue -= 15;
                                }
                                else if ((means / 2) >= 250)
                                {
                                    ExposureValue -= 25;
                                }

                                Brightness_Judgment_Once = true;
                            }

                            if (count_pattern == 4)   //曝光的第二次判定
                            {
                                // 获取当前相机的曝光值
                                ExposureValue = DALSA_Info.ExposureValue;

                                if (((means - means2) / 2) < 50)
                                {
                                    ExposureValue += 30;
                                }
                                else if (((means - means2) / 2) >= 50 && ((means - means2) / 2) < 100)
                                {
                                    ExposureValue += 20;
                                }
                                else if (((means - means2) / 2) >= 100 && ((means - means2) / 2) < 150)
                                {
                                    ExposureValue += 10;
                                }
                                else if (((means - means2) / 2) >= 150 && ((means - means2) / 2) < 180)
                                {
                                    ExposureValue += 0;
                                }
                                else if (((means - means2) / 2) >= 180 && ((means - means2) / 2) < 250)
                                {
                                    ExposureValue -= 10;
                                }
                                else if (((means - means2) / 2) >= 250)
                                {
                                    ExposureValue -= 20;
                                }

                                Brightness_Judgment_Twice = true;

                            }

                            if (count_pattern == 6)   //曝光的第三次判定
                            {
                                // 获取当前相机的曝光值
                                ExposureValue = DALSA_Info.ExposureValue;

                                if (((means - means4) / 2) < 50)
                                {
                                    ExposureValue += 20;
                                }
                                else if (((means - means4) / 2) >= 50 && ((means - means4) / 2) < 100)
                                {
                                    ExposureValue += 15;
                                }
                                else if (((means - means4) / 2) >= 100 && ((means - means4) / 2) < 150)
                                {
                                    ExposureValue += 10;
                                }
                                else if (((means - means4) / 2) >= 150 && ((means - means4) / 2) < 180)
                                {
                                    ExposureValue += 0;
                                }
                                else if (((means - means4) / 2) >= 180 && ((means - means4) / 2) < 250)
                                {
                                    ExposureValue -= 10;
                                }
                                else if (((means - means4) / 2) >= 250)
                                {
                                    ExposureValue -= 15;
                                }

                                Brightness_Judgment_Third = true;

                            }

                            if (count_pattern == 10)    //判断砖型的复杂程度
                            {
                                if (((stddevs - stddevs8) / 2) < 3)
                                {
                                    Coefficient = 4;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 3 && ((stddevs - stddevs8) / 2) < 4)
                                {
                                    Coefficient = 6;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 4 && ((stddevs - stddevs8) / 2) < 5)
                                {
                                    Coefficient = 8;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 5 && ((stddevs - stddevs8) / 2) < 7)
                                {
                                    Coefficient = 10;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 7 && ((stddevs - stddevs8) / 2) < 9)
                                {
                                    Coefficient = 12;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 9 && ((stddevs - stddevs8) / 2) < 12)
                                {
                                    Coefficient = 14;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 12 && ((stddevs - stddevs8) / 2) < 16)
                                {
                                    Coefficient = 16;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 16 && ((stddevs - stddevs8) / 2) < 20)
                                {
                                    Coefficient = 18;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 20 && ((stddevs - stddevs8) / 2) < 25)
                                {
                                    Coefficient = 20;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 25 && ((stddevs - stddevs8) / 2) < 30)
                                {
                                    Coefficient = 22;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 30 && ((stddevs - stddevs8) / 2) < 40)
                                {
                                    Coefficient = 24;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 40 && ((stddevs - stddevs8) / 2) < 50)
                                {
                                    Coefficient = 26;
                                }
                                else if (((stddevs - stddevs8) / 2) >= 50)
                                {
                                    Coefficient = 28;
                                }


                                Pattern_Judgment = true;
                            }

                        }



                        //方法二：二值化阈值定值
                        //double Average_pidai = (double)Cv2.Mean(test_pidai);
                        double Average_pidai = 5;
                        Average_gray = mean.Val0;
                        test_pidai.Dispose();
                        if (Average_pidai >= 15) { thread_set = (int)Average_pidai + 10; }
                        else if (Average_pidai < 15) { thread_set = (int)Average_pidai + 10; }



                        //方法一：二值化阈值动态调整
                        //thread_set = 20;
                        //if (mean.Val0 < 60)  //根据条件选取合适的分割阈值,以及曝光时间
                        //{
                        //    thread_set = 25;
                        //}
                        //else if (mean.Val0 >= 60 && mean.Val0 < 80)
                        //{
                        //    thread_set = 30;
                        //}
                        //else if (mean.Val0 >= 80 && mean.Val0 < 100)
                        //{
                        //    thread_set = 35;
                        //}
                        //else if (mean.Val0 >= 100 && mean.Val0 < 120)
                        //{
                        //    thread_set = 40;
                        //}
                        //else if (mean.Val0 >= 120 && mean.Val0 < 140)
                        //{
                        //    thread_set = 50;
                        //}
                        //else if (mean.Val0 >= 140 && mean.Val0 < 150)
                        //{
                        //    thread_set = 60;
                        //}
                        //else if (mean.Val0 >= 150 && mean.Val0 < 160)
                        //{
                        //    thread_set = 70;
                        //    if (stddev.Val0 > 15) { thread_set = 60; }
                        //    if (stddev.Val0 > 30) { thread_set = 50; }
                        //}
                        //else if (mean.Val0 >= 160 && mean.Val0 < 180)
                        //{
                        //    thread_set = 80;
                        //    if (stddev.Val0 > 15) { thread_set = 70; }
                        //    if (stddev.Val0 > 30) { thread_set = 60; }
                        //}
                        //else if (mean.Val0 >= 180 && mean.Val0 < 210)
                        //{
                        //    thread_set = 90;
                        //    if (stddev.Val0 > 15) { thread_set = 80; }
                        //    if (stddev.Val0 > 30) { thread_set = 70; }
                        //}
                        //else if (mean.Val0 >= 210)
                        //{
                        //    thread_set = 100;
                        //    if (stddev.Val0 > 15) { thread_set = 90; }
                        //    if (stddev.Val0 > 30) { thread_set = 80; }
                        //}


                        Mat img_threshold = new Mat(image_RGB.Size(), image_RGB.Type());
                        Cv2.Threshold(image_mono, img_threshold, thread_set, 255, ThresholdTypes.Binary);
                        image_mono.Dispose();


                        //形态学操作,用来去除砖以外的背景干扰,以及膨胀砖本体上的一些裂纹，高低角图像由于皮带干扰程度不同做了点区别对待
                        Mat img_erode = new Mat();
                        Mat kernel1 = new Mat(5, 1, MatType.CV_8U, 1);
                        Mat kernel2 = new Mat(1, 5, MatType.CV_8U, 1);
                        //if (stddev.Val0 <= 10)
                        //{
                        //    //砖型简单时，滤波算子可以大一些
                        //    kernel1 = new Mat(40, 1, MatType.CV_8U, 1);
                        //    kernel2 = new Mat(1, 40, MatType.CV_8U, 1);
                        //}
                        //else if (stddev.Val0 > 10 && stddev.Val0 <= 14)
                        //{
                        //    //砖型简单时，滤波算子可以大一些
                        //    kernel1 = new Mat(20, 1, MatType.CV_8U, 1);
                        //    kernel2 = new Mat(1, 20, MatType.CV_8U, 1);
                        //}
                        //else if (stddev.Val0 > 14 && stddev.Val0 <= 18)
                        //{
                        //    //砖型简单时，滤波算子可以大一些
                        //    kernel1 = new Mat(15, 1, MatType.CV_8U, 1);
                        //    kernel2 = new Mat(1, 15, MatType.CV_8U, 1);
                        //}
                        //else if (stddev.Val0 >= 18)
                        //{
                        //    //砖型复杂时，滤波算子要小一些，尤其是大花纹，在砖边缘有黑色底纹的那种
                        //    kernel1 = new Mat(7, 1, MatType.CV_8U, 1);
                        //    kernel2 = new Mat(1, 7, MatType.CV_8U, 1);
                        //    if (mean.Val0 < 110)
                        //    {
                        //        //如果砖型有复杂，整体亮度还低的话，滤波算子要更小
                        //        kernel1 = new Mat(5, 1, MatType.CV_8U, 1);
                        //        kernel2 = new Mat(1, 5, MatType.CV_8U, 1);
                        //    }
                        //}
                        Mat kernel3 = new Mat(3, 3, MatType.CV_8U, 1);
                        Cv2.MorphologyEx(img_threshold, img_erode, MorphTypes.Open, kernel1);
                        Cv2.MorphologyEx(img_erode, img_erode, MorphTypes.Open, kernel2);
                        Cv2.MorphologyEx(img_erode, img_erode, MorphTypes.Close, kernel3);
                        //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\img_erode.jpg", img_erode);
                        img_threshold.Dispose();


                        //轮廓提取，fingContours只接受二值化的图像
                        Point[][] contours;
                        List<double> length = new List<double>();
                        Mat img_copy = new Mat();
                        img_erode.CopyTo(img_copy);
                        img_erode.Dispose();
                        Cv2.FindContours(img_copy, out contours, out HierarchyIndex[] hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);
                        img_copy.Dispose();


                        foreach (Point[] c in contours)
                        {
                            //求轮廓的长度
                            length.Add(Cv2.ArcLength(c, true));
                        }
                        //WriteLine("阈值分割后总边缘轮廓数:" + contours.Length);


                        //把轮廓列表中最大值对应的序号输出来，列表list的属性
                        int max_id = length.IndexOf(length.Max());
                        contours_select.Add(contours[max_id]);


                        //Mat img_mask_RGB = new Mat(image_RGB.Size(), image_RGB.Type(), 0);
                        //Scalar scalar = new Scalar(1, 1, 1);
                        //Cv2.DrawContours(img_mask_RGB, contours_select, -1, scalar, Cv2.FILLED);
                        //Cv2.Blur(image_RGB, image_RGB, new Size(3, 3));
                        //Mat img_dst = new Mat(image_RGB.Size(), image_RGB.Type());
                        //Cv2.Multiply(image_RGB, img_mask_RGB, img_dst);
                        //img_mask_RGB.Dispose();
                        //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\img_dst.jpg", img_dst);



                        //方法二：通过拟合点形成直线来抓边
                        //通过条件把轮廓每条边上的点选取出来，拟合为直线后求出直线交点得到四个顶点坐标，important：选取点的坐标范围需要根据现场实际情况进行调整
                        List<Point> contours_horizontal_bottom = new List<Point>();
                        List<Point> contours_horizontal_top = new List<Point>();
                        List<Point> contours_vertical_left = new List<Point>();
                        List<Point> contours_vertical_right = new List<Point>();

                        Line2D lines__horizontal_bottom;
                        Line2D lines__horizontal_top;
                        Line2D lines__vertical_left;
                        Line2D lines__vertical_right;


                        for (int x = 0; x < contours[max_id].Length; x++)
                        {
                            if (contours[max_id][x].X > 500 && contours[max_id][x].X < 1500 && contours[max_id][x].Y > 2200 && contours[max_id][x].Y < 3500)
                            {
                                contours_horizontal_bottom.Add(contours[max_id][x]);
                            }

                            if (contours[max_id][x].X > 415 && contours[max_id][x].X < 465 && contours[max_id][x].Y > 1 && contours[max_id][x].Y < 300)
                            {
                                contours_horizontal_top.Add(contours[max_id][x]);
                            }

                            if (contours[max_id][x].X > 1572 && contours[max_id][x].X < 1592 && contours[max_id][x].Y > 1 && contours[max_id][x].Y < 300)
                            {
                                contours_horizontal_top.Add(contours[max_id][x]);
                            }

                            if (contours[max_id][x].X > 1 && contours[max_id][x].X < 520 && contours[max_id][x].Y > 500 && contours[max_id][x].Y < 2000)
                            {
                                contours_vertical_left.Add(contours[max_id][x]);
                            }

                            if (contours[max_id][x].X > 1572 && contours[max_id][x].X < 2048 && contours[max_id][x].Y > 500 && contours[max_id][x].Y < 2000)
                            {
                                contours_vertical_right.Add(contours[max_id][x]);
                            }
                        }



                        lines__horizontal_bottom = Cv2.FitLine(contours_horizontal_bottom, DistanceTypes.L2, 0, 0.01, 0.01);
                        lines__horizontal_top = Cv2.FitLine(contours_horizontal_top, DistanceTypes.L2, 0, 0.01, 0.01);
                        lines__vertical_left = Cv2.FitLine(contours_vertical_left, DistanceTypes.L2, 0, 0.01, 0.01);
                        lines__vertical_right = Cv2.FitLine(contours_vertical_right, DistanceTypes.L2, 0, 0.01, 0.01);


                        Point CrossPoint_T_L;
                        Point CrossPoint_T_R;
                        Point CrossPoint_B_L;
                        Point CrossPoint_B_R;
                        CrossPoint_T_L = getCrossPoint(lines__horizontal_top, lines__vertical_left);
                        CrossPoint_T_R = getCrossPoint(lines__horizontal_top, lines__vertical_right);
                        CrossPoint_B_L = getCrossPoint(lines__horizontal_bottom, lines__vertical_left);
                        CrossPoint_B_R = getCrossPoint(lines__horizontal_bottom, lines__vertical_right);

                        Point[] srcPts = new Point[] { };
                        //获取矩形四个角点
                        srcPts = new Point[4] { CrossPoint_T_L, CrossPoint_T_R, CrossPoint_B_L, CrossPoint_B_R };



                        //方法一：通过提取整个砖的形态来提取轮廓
                        //Point[][] contours_poly = new Point[contours.Length][];
                        //Point[] srcPts = new Point[] { };
                        //for (int i = 0; i < contours.Length; i++)
                        //{
                        //    double Area = Cv2.ContourArea(contours[i]);
                        //    /* 通过轮廓的面积来选择最大的外围轮廓 */
                        //    if (Area > 500000)
                        //    {
                        //        double epsilon = 0.05 * Cv2.ArcLength(contours[i], true);
                        //        /* 用近似多变行逼近来拟合轮廓的点集 */
                        //        contours_poly[i] = Cv2.ApproxPolyDP(contours[i], epsilon, true);

                        //        //获取矩形四个角点
                        //        srcPts = new Point[4] { new Point(contours_poly[i][0].X, contours_poly[i][0].Y),
                        //                            new Point(contours_poly[i][1].X, contours_poly[i][1].Y),
                        //                            new Point(contours_poly[i][2].X, contours_poly[i][2].Y),
                        //                            new Point(contours_poly[i][3].X, contours_poly[i][3].Y)};
                        //    }

                        //}



                        /*根据其角点所在图像位置特征确定左上、左下、右下、右上四个点*/
                        int width_helf = (int)width / 2;
                        int height_helf = (int)height / 2;
                        int T_L = new int(); int T_R = new int(); int B_R = new int(); int B_L = new int();

                        for (int i = 0; i < srcPts.Length; i++)
                        {
                            if (srcPts[i].X < width_helf && srcPts[i].Y < height_helf)
                            {
                                T_L = i;
                            }
                            if (srcPts[i].X > width_helf && srcPts[i].Y < height_helf)
                            {
                                T_R = i;
                            }
                            if (srcPts[i].X > width_helf && srcPts[i].Y > height_helf)
                            {
                                B_R = i;
                            }
                            if (srcPts[i].X < width_helf && srcPts[i].Y > height_helf)
                            {
                                B_L = i;
                            }

                        }


                        /*变换后，图像的长和宽应该变为*/
                        double LeftHeight = srcPts[T_L].DistanceTo(srcPts[B_L]);
                        double RightHeight = srcPts[T_R].DistanceTo(srcPts[B_R]);
                        double MaxHeight = Math.Round(Math.Max(LeftHeight, RightHeight) / 100) * 100;

                        double UpWidth = srcPts[T_L].DistanceTo(srcPts[T_R]);
                        double DownWidth = srcPts[B_L].DistanceTo(srcPts[B_R]);
                        double MaxWidth = Math.Round(Math.Max(UpWidth, DownWidth) / 100) * 100;


                        /*这里使用的顺序是左上、右上、右下、左下顺时针顺序。SrcAffinePts、DstAffinePts要一一对应*/
                        Point2f[] SrcAffinePts = new Point2f[4] { new Point2f(srcPts[T_L].X, srcPts[T_L].Y),
                                                      new Point2f(srcPts[T_R].X, srcPts[T_R].Y),
                                                      new Point2f(srcPts[B_R].X, srcPts[B_R].Y),
                                                      new Point2f(srcPts[B_L].X, srcPts[B_L].Y) };

                        Point2f[] DstAffinePts = new Point2f[4] { new Point2f(0, 0),
                                                      new Point2f( (float) MaxWidth, 0),
                                                      new Point2f( (float) MaxWidth, (float) MaxHeight),
                                                      new Point2f(0, (float) MaxHeight) };


                        /* 获取透视变换矩阵，得到矫正后的图像*/
                        Mat M = Cv2.GetPerspectiveTransform(SrcAffinePts, DstAffinePts);

                        Cv2.WarpPerspective(image_RGB, DstImg, M, new Size(MaxWidth, MaxHeight));
                        //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\DstImg.jpg", DstImg);
                    }
                    else  //处理有色差较大的效果图
                    {
                        DstImg = image_RGB;
                        Pattern_Judgment = true;
                    }



                    //获得图像的哈希编码
                    Mat img_hash = DstImg.Clone();
                    Mat img_hash_rotate = DstImg.Clone();
                    rotate_image(img_hash_rotate, 90);
                    rotate_image(img_hash_rotate, 90);
                    //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\img_hash.jpg", img_hash);
                    //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\img_hash_rotate.jpg", img_hash_rotate);


                    //同时计算原图和旋转180°后两幅图像的哈希编码，如现场有其他类似
                    Hash_Code = measure.Fun_Hash_Code(img_hash);
                    Hash_Code_rotate = measure.Fun_Hash_Code(img_hash_rotate);
                    img_hash.Dispose();
                    img_hash_rotate.Dispose();


                    //对图像做模糊化处理，降低噪点波动
                    Cv2.Resize(DstImg, img_show, new Size(), 1, 1, InterpolationFlags.Area);
                    //Cv2.Blur(DstImg, DstImg, new Size(33, 33));
                    //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\img_dst.jpg", DstImg);

                    //对L,A,B数据的区间的转换
                    DstImg.ConvertTo(DstImg, MatType.CV_32FC3, 1.0 / 255);




                    // 添加上需要旋转角度,逆时针为正,主要用于让界面显示的图像和工人的观感一致
                    double angle = 0;
                    rotate_image(img_show, angle);


                    //Mat img_mask_RGB = new Mat(image_RGB.Size(), image_RGB.Type(), 0);
                    //Scalar scalar = new Scalar(1, 1, 1);
                    //Cv2.DrawContours(img_mask_RGB, contours_select, -1, scalar, Cv2.FILLED);
                    //Cv2.Blur(image_RGB, image_RGB, new Size(3, 3));
                    //Mat img_dst = new Mat(image_RGB.Size(), image_RGB.Type());
                    //Cv2.Multiply(image_RGB, img_mask_RGB, img_dst);
                    //img_mask_RGB.Dispose();
                    //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\img_dst.jpg", img_dst);


                    //获得三通道的LAB和HSV信息
                    Mat[] LAB = new Mat[3]; 
                    double HSV_H, LAB_L, LAB_A, LAB_B;
                    Mat img_CIELAB = new Mat(image_RGB.Size(), MatType.CV_32FC3);
                    Cv2.CvtColor(DstImg, img_CIELAB, ColorConversionCodes.BGR2Lab);
                    Cv2.Split(img_CIELAB, out LAB);
                    img_CIELAB.Dispose();


                    Mat[] HSV = new Mat[3];
                    Mat img_HSV = new Mat(image_RGB.Size(), MatType.CV_32FC3);
                    Cv2.CvtColor(DstImg, img_HSV, ColorConversionCodes.BGR2HSV);
                    Cv2.Split(img_HSV, out HSV);
                    img_HSV.Dispose();


                    Mat[] BGR = new Mat[3];
                    double BGR_B,BGR_G,BGR_R;
                    Cv2.Split(DstImg,out BGR);
                    //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\RGB.jpg", BGR[2]);

                    //Mat img_mask_mono = new Mat(image_RGB.Size(), MatType.CV_8UC1, 0);
                    //Cv2.DrawContours(img_mask_mono, contours_select, -1, 1, Cv2.FILLED);
                    //double area = Cv2.Sum(img_mask_mono)[0];
                    //Cv2.Multiply(LAB[0], img_mask_mono, LAB[0]);
                    //Cv2.Multiply(LAB[1], img_mask_mono, LAB[1]);
                    //Cv2.Multiply(LAB[2], img_mask_mono, LAB[2]);
                    //Cv2.Multiply(HSV[0], img_mask_mono, HSV[0]);
                    //img_mask_mono.Dispose();

                    //HSV_H = Cv2.Sum(HSV[0])[0] / area;
                    //LAB_L = Cv2.Sum(LAB[0])[0] / area;
                    //LAB_A = Cv2.Sum(LAB[1])[0] / area;
                    //LAB_B = Cv2.Sum(LAB[2])[0] / area;


                    //把OpenCV计算的LAB结果还原到本来的区间
                    //LAB[0] = LAB[0] / 2.55;
                    //LAB[1] = LAB[1] - 128;
                    //LAB[2] = LAB[2] - 128;


                    //计算对应通道的均值
                    HSV_H = (double)Cv2.Mean(HSV[0]);
                    LAB_L = (double)Cv2.Mean(LAB[0]);
                    LAB_A = (double)Cv2.Mean(LAB[1]);
                    LAB_B = (double)Cv2.Mean(LAB[2]);

                    BGR_B = (double)Cv2.Mean(BGR[0]);
                    BGR_G = (double)Cv2.Mean(BGR[1]);
                    BGR_R = (double)Cv2.Mean(BGR[2]);



                    //计算直方图
                    //Create_Excel();
                    //double min, max;
                    //Cv2.MinMaxIdx(LAB[1], out min, out max);
                    //double unit = (max - min) / 1000;
                    // Mat hist = new Mat();
                    //Cv2.CalcHist(new Mat[] { LAB[1] }, new int[] { 0 }, null, hist, 1, new int[] { 1000 }, new Rangef[] { new Rangef((float)min, (float)max) });
                    //List<float> res = new List<float>();
                    //for (int i = 0; i < hist.Rows; i++)
                    //{
                    //    res.Add(hist.At<float>(i, 0));

                    //    Data2Excel((min + i * unit), hist.At<float>(i, 0));

                    //}

                    //double k = 1;
                    //for (int i = 0; i < LAB[1].Width; i++)
                    //{
                    //    for (int j = 0; j < LAB[1].Height; j++)
                    //    {
                    //        Data2Excel(k, LAB[1].Get<float>(i, j));
                    //        k++;
                    //    }
                    //}



                    //Cv2.Resize(LAB[1], LAB[1], new Size(), 0.03, 0.03, InterpolationFlags.Area);
                    //double min1, max1;
                    //Cv2.MinMaxIdx(LAB[1], out min1, out max1);
                    //float[] re = new float[3200 * 1600];
                    //int countt = 0;

                    //for (int i = 0; i < LAB[0].Width; i++)
                    //{ 
                    //    for (int j = 0; j < LAB[0].Height; j++)
                    //    {
                    //        float temp = LAB[0].Get<float>(i, j);
                    //        re[countt] = temp;
                    //        countt++;
                    //    }

                    //}
                    //Array.Sort(re);

                    //double kk = 1;
                    //float first = re[0];
                    //for (int i = 0; i < re.Length; i++)
                    //{
                    //    if (re[i] == first)
                    //    {
                    //        kk++;
                    //    }
                    //    else
                    //    {
                    //        Data2Excel(first, kk);
                    //        kk = 1;
                    //        first = re[i];
                    //    }

                    //}



                    //Cv2.ImWrite(path + "\\img_LAB_L.jpg", LAB[0]);
                    //Cv2.ImWrite(path + "\\img_LAB_A.jpg", LAB[1]);
                    //Cv2.ImWrite(path + "\\img_LAB_B.jpg", LAB[2]);
                    //Cv2.ImWrite(path + "\\img_HSV_H.jpg", HSV[0]);
                    //Cv2.ImWrite(path + "\\img_HSV_S.jpg", HSV[1]);
                    //Cv2.ImWrite(path + "\\img_HSV_V.jpg", HSV[2]);


                    //释放资源
                    LAB[0].Dispose();
                    LAB[1].Dispose();
                    LAB[2].Dispose();
                    HSV[0].Dispose();
                    HSV[1].Dispose();
                    HSV[2].Dispose();
                    BGR[0].Dispose();
                    BGR[1].Dispose();
                    BGR[2].Dispose();


                    //统计程序耗时
                    stpwth.Stop();
                    TimeSpan ts = stpwth.Elapsed;
                    //WriteLine("崩边图像处理耗时:" + ts.TotalMilliseconds + "ms");


                    //图像的保存和显示
                    //Cv2.NamedWindow("chipping");
                    //Cv2.ResizeWindow("chipping", width / 10, height / 10);
                    //Cv2.ImShow("chipping", img_color);
                    //Cv2.ImWrite(path + "\\chipping_" + use_img + ".jpg", img_color);
                    //Cv2.WaitKey();


                    //返回结果及耗时
                    //img_show = img_dst.Clone();
                    //img_dst.Dispose();
                    
                    time_consuming = ts.TotalMilliseconds;
                    is_done = true;

                    
                    //处理完有结果了进事件处理器
                    if (is_done)
                    {
                        if (event_result_return != null)
                            event_result_return(img_show, time_consuming, path_use, HSV_H, LAB_L, LAB_A, LAB_B, Hash_Code, Hash_Code_rotate,BGR_B,BGR_G,BGR_R,DstImg);
                    }

                    image_RGB.Dispose();
                    //DstImg.Dispose();
                    //GC.Collect();
                }
            }
            catch(Exception ex)
            {
                //throw new ArgumentException(ex.Message);

                has_error = true;
                if (event_error_return != null)
                    event_error_return(0,path_use);

                image_RGB.Dispose();
            }

            
        }



        //对图像进行哈希编码，使其拥有唯一的身份码，用来区分不同的版型纹理
        public static int[] Fun_Hash_Code(Mat img)
        {
            //降低图像的分辨率
            //Cv2.Resize(img, img, new Size(), 0.1, 0.1, Interpolation.Area);
            Cv2.CvtColor(img,img,ColorConversionCodes.BGR2GRAY);
            Cv2.Sobel(img, img, MatType.CV_8UC1, 1, 0, 9, 0.005, 0, BorderTypes.Default);
            //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\DstImg.jpg", img);

            int unit_width = img.Width / unit;
            int unit_height = img.Height / unit;
            int new_width = unit_width * unit;
            int new_height = unit_height * unit;

            //把图像的分辨率搞到一致
            Cv2.Resize(img, img, new Size(new_width, new_height));


            //1.自定义区域，通过哈希算法和汉明距离来衡量两幅图像的相似度
            int num = 0;
            double unit_mean = 0;
            int[] Hash = new int[unit * unit];
            Mat[] mats = new Mat[unit * unit];
            Rect[] rects = new Rect[unit * unit];
            double mean = (double)Cv2.Mean(img);
            int unit_width_new = img.Width / unit;
            int unit_height_new = img.Height / unit;


            //主体思想是，把图像分区域，然后每个子区域的灰度均值与全图的灰度均值对比较，大于则该子区域赋值1，小于0
            for (int i = 0; i < unit; i++)
            {
                for (int j = 0; j < unit; j++)
                {

                    rects[num] = new Rect(unit_width_new * j, unit_height_new * i, unit_width_new, unit_height_new);
                    mats[num] = new Mat(img, rects[num]);

                    unit_mean = (double)Cv2.Mean(mats[num]);

                    if (unit_mean >= mean)
                    {
                        Hash[num] = 1;
                    }
                    else
                    {
                        Hash[num] = 0;
                    }


                    num++;

                }
            }

            return Hash;
        }



        //通过仿射变换对图像进行任意角度的旋转
        public void rotate_image(Mat input_img, double angle)
        {
            
            double MaxWidth = input_img.Width;
            double MaxHeight = input_img.Height;

            // 计算2D的旋转变换矩阵，也可以通过给出变换前后对应的3对点可使用getAffineTransform方法得到仿射变换矩阵，两个方法都是针对2D图像的
            Point2f center = new Point2f((float)MaxWidth / 2, (float)MaxHeight / 2);
            Mat rot_matrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);

            // 计算旋转后输出图形的尺寸
            double sin_angle = Math.Sin(Math.Abs(angle) / 180 * Math.PI);
            double cos_angle = Math.Cos(Math.Abs(angle) / 180 * Math.PI);
            double new_height = (MaxWidth * sin_angle + MaxHeight * cos_angle);
            double new_width = (MaxWidth * cos_angle + MaxHeight * sin_angle);

            // 防止切边，对2行3列的旋转矩阵进行修改，
            double new_px = rot_matrix.Get<double>(0, 2) + (new_width - MaxWidth) / 2;
            double new_py = rot_matrix.Get<double>(1, 2) + (new_height - MaxHeight) / 2;
            rot_matrix.Set(0, 2, new_px);
            rot_matrix.Set(1, 2, new_py);

            // 应用仿射变换
            Cv2.WarpAffine(input_img, input_img, rot_matrix, new Size(new_width, new_height), InterpolationFlags.Linear, BorderTypes.Constant);
            rot_matrix.Dispose();
        }


        //求两直线相交的交点坐标
        public Point getCrossPoint(Line2D LineA, Line2D LineB)
        {
            double ka, kb;
            ka = (double)(LineA.Vy / LineA.Vx); //求出LineA斜率
            kb = (double)(LineB.Vy / LineB.Vx); //求出LineB斜率

            Point crossPoint;
            crossPoint.X = Convert.ToInt16((ka * LineA.X1 - LineA.Y1 - kb * LineB.X1 + LineB.Y1) / (ka - kb));
            crossPoint.Y = Convert.ToInt16((ka * kb * (LineA.X1 - LineB.X1) + ka * LineB.Y1 - kb * LineA.Y1) / (ka - kb));
            return crossPoint;
        }


        public static string pathExcel;
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
                row.CreateCell(0).SetCellValue("数值");
                row.CreateCell(1).SetCellValue("数量");
                

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
        public static void Data2Excel(float a, double b)
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
                row1.CreateCell(1).SetCellValue(b);
                

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
    }
}
