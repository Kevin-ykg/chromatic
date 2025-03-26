using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;



namespace Template_Matching
{
    public class template_match
    {
        /// <summary>
        /// 哈希编码划分的区域，unit*unit
        /// </summary>
        public int unit { get; set; } = 300;



        /// <summary>
        /// 对图像进行哈希编码，使其拥有唯一的身份码，用来区分不同的版型纹理
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public int[] Fun_Hash_Code(Mat img)
        {
            //转换为灰度图，哈希编码只需要灰度图
            Cv2.CvtColor(img, img, ColorConversionCodes.BGR2GRAY);

            //sobel算子提取边缘
            //Mat mat_sobel = new Mat();
            Cv2.Sobel(img, img, MatType.CV_8UC1, 1, 0, 9, 0.005, 0, BorderTypes.Default);
            //Cv2.Threshold(mat_sobel, mat_sobel, 80, 255, ThresholdTypes.Binary);
            //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\DstImg.jpg", img);

            //灰度均衡化，想增加图像纹理丰富度，不太好用
            //Mat mat_add = new Mat();
            //Cv2.Add(finalImage, mat_sobel, mat_add);
            //Cv2.EqualizeHist(finalImage,finalImage);

            //拼接大图，丰富图像纹理，使其哈希编码更加准确
            Mat img_resize = new Mat();
            Cv2.Resize(img, img_resize, new Size(), 0.1, 0.1, InterpolationFlags.Area);    //降低分辨率，与要拼接大图的网格大小有关
            Mat finalImage = Composite_images(img_resize);
            //Cv2.ImWrite(@"C:\\Users\\Lenovo\\Desktop\\DstImg.jpg", finalImage);


            //1.自定义区域，通过哈希算法和汉明距离来衡量两幅图像的相似度
            int num = 0;
            double unit_mean = 0;
            int[] Hash = new int[unit * unit];
            Mat[] mats = new Mat[unit * unit];
            Rect[] rects = new Rect[unit * unit];
            double mean = (double)Cv2.Mean(finalImage);
            int unit_width_new = finalImage.Width / unit;
            int unit_height_new = finalImage.Height / unit;


            //主体思想是，把图像分区域，然后每个子区域的灰度均值与全图的灰度均值对比较，大于则该子区域赋值1，小于0
            for (int i = 0; i < unit; i++)
            {
                for (int j = 0; j < unit; j++)
                {

                    rects[num] = new Rect(unit_width_new * j, unit_height_new * i, unit_width_new, unit_height_new);
                    mats[num] = new Mat(finalImage, rects[num]);

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




        /// <summary>
        /// 进行图像的组合，将多张小图合成一张大图
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private Mat Composite_images(Mat img)
        {
            int gridSize = 10; // 4x4 网格
            int imgWidth = img.Width, imgHeight = img.Height;  // 单张图片大小
            int finalWidth = gridSize * imgWidth, finalHeight = gridSize * imgHeight;

            // 创建最终的大图像（白色背景）
            Mat finalImage = new Mat(new Size(finalWidth, finalHeight), MatType.CV_8UC1);

            for (int i = 0; i < gridSize * gridSize; i++)
            {
                // 计算拼接位置
                int x = (i % gridSize) * imgWidth;
                int y = (i / gridSize) * imgHeight;

                // 复制到大图像中
                img.CopyTo(new Mat(finalImage, new Rect(x, y, imgWidth, imgHeight)));

            }
            return finalImage;
        }



        /// <summary>
        /// 获得哈希编码后的两幅图像之间的汉明距离
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public int Get_Hamming_Distance(int[] a, int[] b)
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




        /// <summary>
        /// 通过仿射变换对图像进行任意角度的旋转
        /// </summary>
        /// <param name="input_img"></param>
        /// <param name="angle"></param>
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


        /// <summary>
        /// 获取Cv2.MeanStdDev计算得到砖面纹理标准差的复杂度系数，在第十块砖时计算
        /// </summary>
        /// <param name="mean_stddev"></param>
        /// <returns></returns>
        public double get_coefficient_first(double stddevs)
        {
            double mean_stddev = 0.0;
            double Coefficient = 0.0;
            mean_stddev = stddevs / 10;

            if (mean_stddev < 3)
            {
                Coefficient = 2;
            }
            else if (mean_stddev >= 3 && mean_stddev < 4)
            {
                Coefficient = 4;
            }
            else if (mean_stddev >= 4 && mean_stddev < 5)
            {
                Coefficient = 4;
            }
            else if (mean_stddev >= 5 && mean_stddev < 7)
            {
                Coefficient = 6;
            }
            else if (mean_stddev >= 7 && mean_stddev < 9)
            {
                Coefficient = 6;
            }
            else if (mean_stddev >= 9 && mean_stddev < 12)
            {
                Coefficient = 8;
            }
            else if (mean_stddev >= 12 && mean_stddev < 16)
            {
                Coefficient = 8;
            }
            else if (mean_stddev >= 16 && mean_stddev < 20)
            {
                Coefficient = 10;
            }
            else if (mean_stddev >= 20 && mean_stddev < 25)
            {
                Coefficient = 12;
            }
            else if (mean_stddev >= 25 && mean_stddev < 30)
            {
                Coefficient = 14;
            }
            else if (mean_stddev >= 30 && mean_stddev < 40)
            {
                Coefficient = 16;
            }
            else if (mean_stddev >= 40 && mean_stddev < 50)
            {
                Coefficient = 18;
            }
            else if (mean_stddev >= 50)
            {
                Coefficient = 20;
            }

            return Coefficient;
        }



        /// <summary>
        /// 根据汉明距离计算得到砖面纹理标准差的复杂度系数，在第16块砖时计算
        /// </summary>
        /// <param name="list_Hamming_Distance"></param>
        /// <returns></returns>
        public double get_coefficient_second(List<double> list_Hamming_Distance)
        {
            double k = 0.1;
            double sigma = GetSigma(list_Hamming_Distance);

            if (sigma < 200)
            {
                k = -0.5;

            }
            else if (sigma >= 200 && sigma < 400)
            {
                k = -0.45;

            }
            else if (sigma >= 400 && sigma < 600)
            {
                k = -0.3;

            }
            else if (sigma >= 600 && sigma < 800)
            {
                k = 0.03;

            }
            else if (sigma >= 800 && sigma < 1200)
            {
                k = 0.05;

            }
            else if (sigma >= 1200 && sigma < 1600)
            {
                k = 0.07;

            }
            else if (sigma >= 1600 && sigma < 3000)
            {
                k = 0.09;

            }
            else if (sigma >= 3000 && sigma < 5000)
            {
                k = 0.12;

            }
            else if (sigma >= 5000 && sigma < 8000)
            {
                k = 0.15;

            }
            else if (sigma >= 8000)
            {
                k = 0.2;

            }

            return k;
        }




        /// <summary>
        /// 求数组的均值和方差
        /// </summary>
        /// <param name="dataList"></param>
        /// <returns></returns>
        static double GetSigma(List<double> dataList)
        {
            var u = dataList.Average(); //平均值
            var sum = dataList.Sum(p => Math.Pow(p - u, 2));
            var sigma = Math.Sqrt(sum / (dataList.Count));
            return sigma;
        }
    }


}
