using System;
using System.Linq;
using System.Windows.Forms;
using DALSA.SaperaLT.SapClassBasic;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Windows;
using System.Windows.Input;
using System.IO.Ports;
using System.Globalization;
using System.Net.WebSockets;
using System.Configuration;
using System.Threading.Tasks;



namespace Chromatic
{
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();

            Mem_Create();    //创建内存共享
            InitParam();    //读取采集卡的配置文件
            Init();
        }

        public string str;
        public bool flag = false;
        public Mat mat_image_src_high = new Mat();
        public string[,] str_error_send = new string[100, 10];
        public string str_OKNG = "";

        /// <summary>
        /// 延时时间
        /// </summary>
        const int m_delayedTime = 1;

        private static int picCountNum = 0;
        private static int RGBCountNum = 0;
        private static int CountNum = 0;
        private static int DarkNum = 0;
        private static int WhiteNum = 0;

        Stopwatch sw_mem = new Stopwatch();
        public long CZ_Comm_Count = 0;
        public long CZ_Comm_Restore_Count = 0;
        Stopwatch sw_main1 = new Stopwatch();
        public long sw_main1_time = 0;
        Stopwatch sw_main2 = new Stopwatch();
        public long sw_main2_time = 0;
        Stopwatch sw_main3 = new Stopwatch();
        public long sw_main3_time = 0;
        public int En_Camera_Work = 0;

        /// <summary>
        /// 采集标识。1:单步采集/2:连续采集/0:停止采集
        /// </summary>
        int m_grabFlg = 0;
        bool m_threadStartFlg = false;

        public Thread CDL_thread_Camera_Opera;

        int cz_count_temp = 0;

        private Thread test_thread1;
        private Thread test_thread2;
        private bool[] testFlag = new bool[2];
        private bool m_online;
        bool isInited = false;

        private SapBuffer m_Buffer1, m_Buffer2;
        private SapLocation m_ServerLocation;  // 设备的连接地址
        private SapLocation m_ServerLocation1;  // 设备的连接地址
        private SapAcquisition[] m_Acquisition = null;   // 采集设备
        private SapBufferRoi[] m_Buffers = null;           // 缓存对象
        private SapAcqToBuf[] m_Xfer = null;      // 传输对象
        public SapAcqDevice m_AcqDevice = null;

        SapTransfer m_pTransfer1 = new SapTransfer(), m_pTransfer2 = new SapTransfer();

        private string BoardConfigPath;
        private string CameraConfigPath;

        bool Judge_Image_Mem_OK = false;
        bool Sys_Start = false;
        int Timer_1s_Count = 0;
        int init_img = 0;
        int m_exposure_restore_value = 0;


        public struct CopyData_Struct//定义WM_COPYDATA消息结构体
        {
            public IntPtr dwData;//dwData为32位的自定义数据
            public int cbData;//cbData为lpData指针指向数据的大小（字节数）
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;//lpData为指向数据的指针，
        }

        public const int WM_COPYDATA = 0x004A;

        //通过窗口标题来查找窗口句柄   
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);

        //发送消息函数  
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage
         (
          int hWnd,                         // 目标窗口的句柄    
          int Msg,                          // 在这里是WM_COPYDATA  
          int wParam,                       // 第一个消息参数  
          ref CopyData_Struct lParam        // 第二个消息参数  
         );





        public bool Mem_Create()    //创建内存共享
        {
            Judge_Image_Mem_OK = Cs_MeM_Image2D.CreateMemMap("Mem_Camera_Image", 8192 * 20000 * 11);
            return Judge_Image_Mem_OK;
        }

        private void InitParam()
        {
            ReadConfigParam("BoardConfigPath", out BoardConfigPath);
            ReadConfigParam("CameraConfigPath", out CameraConfigPath);

        }

        private void ReadConfigParam(string key, out string path, bool isPathData = true)
        {
            string pathTmp = "";
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (isPathData)
            {
                pathTmp = Path.Combine("@", cfa.AppSettings.Settings[key].Value);
            }
            else
            {
                pathTmp = cfa.AppSettings.Settings[key].Value;
            }
            path = pathTmp;
        }

        private void Init()
        {
            m_Acquisition = new SapAcquisition[2];    // 采集设备初始化，有几个就初始化几个
            m_Buffers = new SapBufferRoi[2];          // 缓存对象
            m_Xfer = new SapAcqToBuf[2];              // 传输对象

            ArrayList Names;
            //int Index;
            //bool RTemp = GetCameraInfo(out Name, out Index);  
            //    GetCameraInfo(out Names, out string[] dNames, out int index);


            //m_ServerLocation = new SapLocation("Xtium-CL_MX4_2".ToString(), 0);
            //m_Acquisition[0] = new SapAcquisition(m_ServerLocation, @"D:\dalsapath\T_LA_CM_08K08A_00_R_External_Default.ccf");


            m_ServerLocation1 = new SapLocation("Xtium-CL_MX4_1".ToString(), 1);
            m_Acquisition[1] = new SapAcquisition(m_ServerLocation1, BoardConfigPath);

            //1235
            var m_acqDeviceLocation = new SapLocation("CameraLink_1", 0);
            m_AcqDevice = new SapAcqDevice(m_acqDeviceLocation, false);//CameraConfigPath

            //if (m_Acquisition[0] != null && !m_Acquisition[0].Initialized)
            //{
            //    if (m_Acquisition[0].Create() == false)
            //    {
            //        DestroyObjects();
            //    }
            //}

            bool issuccess = false;
            if (m_Acquisition[1] != null && !m_Acquisition[1].Initialized)
            {
                if (m_Acquisition[1].Create() == false)
                {
                    DestroyObjects();

                }
                else
                {
                    issuccess = true;
                }
            }

            //1235
            if (!issuccess)
            {
                return;
            }
            if (m_AcqDevice != null && !m_AcqDevice.Initialized)
            {
                if (m_AcqDevice.Create() == false)
                {
                    DestroyObjects();
                }
                else
                {
                    issuccess = true;
                }
            }
            if (!issuccess)
            {
                return;
            }

            GetBufferImageSize(out image_width, out image_length);

            Task.Run(() => { ShowImageInfo(image_width, image_length); GetCurrGainValue(); GetCurrExposureValue(); });

            m_online = true;

            //check to see if both acquision devices support scatter gather.
            //bool acq0SupportSG = SapBuffer.IsBufferTypeSupported(m_Acquisition[0].Location, SapBuffer.MemoryType.ScatterGather);
            bool acq1SupportSG = SapBuffer.IsBufferTypeSupported(m_Acquisition[1].Location, SapBuffer.MemoryType.ScatterGather);


            //if (!acq0SupportSG || !acq1SupportSG)
            if (!acq1SupportSG)
            {
                // check if they support scatter gather physical
                //bool acq0SupportSGP = SapBuffer.IsBufferTypeSupported(m_Acquisition[0].Location, SapBuffer.MemoryType.ScatterGatherPhysical);
                bool acq1SupportSGP = SapBuffer.IsBufferTypeSupported(m_Acquisition[1].Location, SapBuffer.MemoryType.ScatterGatherPhysical);

                //if (!(!acq0SupportSG && !acq1SupportSG && acq0SupportSGP && acq1SupportSGP))
                if (!(!acq1SupportSG && acq1SupportSGP))
                {
                    String message;
                    message = String.Format("The chosen acquisition devices\n\n-{0}\n-{1}\n\ndo not support similar buffer types.", m_Acquisition[1].Location.ServerName, m_Acquisition[1].Location.ServerName);
                    MessageBox.Show(message, "Buffer Type Error");
                    m_online = false;
                }
            }


            //if (SapBuffer.IsBufferTypeSupported(m_ServerLocation, SapBuffer.MemoryType.ScatterGather))
            //{
            //    m_Buffer1 = new SapBufferWithTrash();
            //    m_Buffers[0] = new SapBufferRoi(m_Buffer1);
            //}
            //else
            //{
            //    m_Buffer1 = new SapBufferWithTrash();
            //    m_Buffers[0] = new SapBufferRoi(m_Buffer1);
            //}

            if (SapBuffer.IsBufferTypeSupported(m_ServerLocation1, SapBuffer.MemoryType.ScatterGather))
            {
                m_Buffer2 = new SapBufferWithTrash();
                m_Buffers[1] = new SapBufferRoi(m_Buffer2);
            }
            else
            {
                m_Buffer2 = new SapBufferWithTrash();
                m_Buffers[1] = new SapBufferRoi(m_Buffer2);
            }


            //m_Xfer[0] = new SapAcqToBuf(m_Acquisition[0], m_Buffers[0]);
            m_Xfer[1] = new SapAcqToBuf(m_Acquisition[1], m_Buffers[1]);

            //m_pTransfer1.AddPair(new SapXferPair(m_Acquisition[0], m_Buffers[0]));
            m_pTransfer2.AddPair(new SapXferPair(m_Acquisition[1], m_Buffers[1]));
            //m_View = new SapView(m_Buffer);


            //event for view
            //m_Xfer[0].Pairs[0].EventType = SapXferPair.XferEventType.EndOfFrame;
            //m_Xfer[0].XferNotify += new SapXferNotifyHandler(m_Xfer_XferNotify0);
            //m_Xfer[0].XferNotifyContext = this;


            m_Xfer[1].Pairs[0].EventType = SapXferPair.XferEventType.EndOfFrame;
            m_Xfer[1].XferNotify += new SapXferNotifyHandler(m_Xfer_XferNotify1);
            m_Xfer[1].XferNotifyContext = this;

            if (!CreateObjects())
            {
                DisposeObjects();
                //return false;
            }

            UpdateControls();


            //if (CreateNewObjects())
            //    this.Close();

            //cameraStart = new ThreadStart(Grab_Thread);

            CDL_thread_Camera_Opera = new Thread(Grab_Thread);
            //1235
            CDL_thread_Camera_Opera.IsBackground = true;
            CDL_thread_Camera_Opera.Start();


            //AcqConfigDlg acConfigDlg = new AcqConfigDlg(null, "", AcqConfigDlg.ServerCategory.ServerAcq);
            //if (acConfigDlg.ShowDialog() == DialogResult.OK)
            //    m_online = true;
            //else
            //    m_online = false;

            //if (!CreateNewObjects(acConfigDlg))
            //    this.Close();
            //test_thread1 = new Thread(test_dark_thread);
            //test_thread2 = new Thread(test_light_thread);
            //test_thread1.Start();
            //test_thread2.Start();

            //for (int i = 0; i < testFlag.Length; i++)
            //{
            //    testFlag[i] = false;
            //}

            Sys_Start = true;
            //1235
            btn_grab_Click(null, null);
        }

        public int mem_index = 0;
        public int mem_index_wr = 0;
        public int mem_index1 = 0;
        public int mem_index2 = 0;
        public int mem_index3 = 0;
        private int MeM_W_Count = 0;
        private int MeM_W_Count2 = 0;
        void Grab_Thread()
        {
            while (true)
            {
                Thread.Sleep(30);
                try
                {
                    //dataGridView1.Rows[6].Cells[0].Value = JW_BM.iCalLbackNum[0].ToString();
                    if (m_grabFlg == 2)
                    {

                        m_threadStartFlg = true;

                        //if (h1_Image == null)
                        //{
                        //    //isWaitFor = true;
                        //    //MessageBox.Show("The ho_Image is null!");
                        //    //isWaitFor = false;

                        //    continue;
                        //}

                        //HObject ho_Image_1 = new HObject();
                        //HOperatorSet.GenEmptyObj(out ho_Image_1);
                        //ho_Image_1.Dispose();

                        //ho_Image_1 = h1_Image.CopyObj(1, 1);


                        //HOperatorSet.CopyImage(h1_Image, out ho_Image_1);
                        if (cz_count == cz_restore_count)        //cz_count在相机触发获取到图像后会cz_count++，这里来判断有没有新的图像生成
                        {
                            continue;
                        }
                        else
                        {
                            cz_restore_count = cz_count;
                            //HObject ho_Image_1 = new HObject();
                            //ho_Image_1 = h1_Image.CopyObj(1, 1);
                            if (m_grabFlg == 2) //触发模式
                            {
                                //////////////////////////////////////////////////////////////////////////
                                //-----------------------halcon方法调用


                                try
                                {


                                    //if (sendSocket.Connected == true)
                                    //{
                                    //    string str = "相机进程开始";// 
                                    //    byte[] buf = Encoding.UTF8.GetBytes(str);
                                    //    //发送消息  
                                    //    //sendSocket.Send(buf);
                                    //    AsyncSend(sendSocket, str);
                                    //}
                                }
                                catch
                                {

                                }
                                sw_main1.Reset();
                                sw_main1.Start();



                                //新版三通道
                                if (Judge_Image_Mem_OK == true)
                                {
                                    sw_mem.Reset();
                                    sw_mem.Start();
                                    //Cv2.ImWrite(@"D:\GH_CeramicDetection\异常\高角度\常规\blur_src" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bmp", mat_image_src);
                                    Mat mat_image_src_high_S = new Mat();
                                    Cv2.CvtColor(mat_image_src_high, mat_image_src_high_S, ColorConversionCodes.BGR2GRAY);



                                    //byte[] img_data_array_src1 = new byte[8192 * 16384];
                                    //byte[] img_data_array_src2 = new byte[8192 * 16384];
                                    //byte[] img_data_array_src3 = new byte[8192 * 16384];
                                    ////Mat mat_image_src_temp = mat_image_src.CvtColor(ColorConversionCodes.BGR2GRAY);
                                    //spilt_mat_hsv[0].GetArray(0, 0, img_data_array_src1);
                                    //bool bw1 = Cs_MeM_Image2D.WriteDataByte(img_data_array_src1, 100 + 8192 * 16384 * 0);
                                    //spilt_mat_hsv[1].GetArray(0, 0, img_data_array_src2);
                                    //bool bw2 = Cs_MeM_Image2D.WriteDataByte(img_data_array_src2, 100 + 8192 * 16384 * 1);
                                    //spilt_mat_hsv[2].GetArray(0, 0, img_data_array_src3);
                                    //bool bw3 = Cs_MeM_Image2D.WriteDataByte(img_data_array_src3, 100 + 8192 * 16384 * 2);
                                    mem_index++;
                                    //Cv2.ImWrite(@"D:\GH_CeramicDetection\异常\相机\" + mem_index.ToString() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_High.jpg", mat_image_src_high);
                                    //Cv2.ImWrite(@"D:\GH_CeramicDetection\异常\相机\" + mem_index.ToString() + "_Low.jpg", mat_image_src_low);
                                    byte[] img_data_array_high = new byte[image_length * image_width];
                                    //byte[] img_data_array_low = new byte[13400 * 8192];
                                    ////Mat mat_image_src_temp = mat_image_src.CvtColor(ColorConversionCodes.BGR2GRAY);
                                    ////Mat mat_image_src_temp = mat_image_src.Clone();
                                    mat_image_src_high_S.GetArray(0, 0, img_data_array_high);

                                    bool bw1 = false;
                                    if (mem_index_wr >= 0)
                                    {
                                        mem_index_wr = 1;

                                        mem_index1++;
                                        Mat mat_d_set_fun = 255 * Mat.Ones(5, 5, MatType.CV_8UC1);
                                        if (mem_index1 % 2 == 1)
                                        {
                                            //bw1 = Cs_MeM_Image2D.WriteDataByte(img_data_array_high, 8192 * 20000 * 3);
                                            Cv2.ImWrite("D:\\GH_CeramicDetection\\配置\\瓷砖\\单数瓷砖\\" + "RGB1" + ".jpg", mat_d_set_fun);
                                        }
                                        else
                                        {
                                            //bw1 = Cs_MeM_Image2D.WriteDataByte(img_data_array_high, 8192 * 20000 * 5);
                                            Cv2.ImWrite("D:\\GH_CeramicDetection\\配置\\瓷砖\\双数瓷砖\\" + "RGB2" + ".jpg", mat_d_set_fun);
                                        }
                                        //txt_mem1.Text = mem_index1.ToString();
                                        //txt_mem1.BeginInvoke(new Action(() => { txt_mem1.Text = mem_index1.ToString(); }));
                                    }

                                    sw_mem.Stop();

                                    if (bw1)
                                    {
                                        //SendMessage_Operation("相机写入成功");
                                        //if (Cs_SerialOpera_001_opera.Command_comm.IsOpen)
                                        //{
                                        //    try
                                        //    {
                                        //        //00 00 00 00 00 00 00 00 00 00

                                        //        string str_send11 = "55 AA 01 02 03 04 05 06 " + (mem_index / 255).ToString("X2") + " " + (mem_index % 255).ToString("X2") + " " + ((int)(Math.Ceiling(float.Parse(lbl_currExposureValue.Text)))).ToString("X2") + " 02 03 04 05 06 07 08 33 CC";

                                        //        Cs_SerialOpera_001_opera.CDL_Command_Send(str_send11);
                                        //        //Cs_SerialOpera_001_opera.CDL_Command_Send("55 AA 01 02 03 04 05 00 00 00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 33 CC");
                                        //    }
                                        //    catch
                                        //    {

                                        //        // MessageBox.Show("请检查输入数据是否正确，修改后重新电机写入");
                                        //    }
                                        //}

                                        MeM_W_Count++;
                                        //txt_mem.BeginInvoke(new Action(() => { txt_mem.Text = MeM_W_Count.ToString(); }));
                                    }
                                    else
                                    {
                                        MeM_W_Count2++;
                                        //textBox2.BeginInvoke(new Action(() => { textBox2.Text = MeM_W_Count2.ToString(); }));
                                    }



                                    //Mat src_hsv = new Mat();
                                    Mat src_rgb = new Mat();
                                    //Cv2.CvtColor(mat_image_src_high, src_hsv, ColorConversionCodes.BGR2HSV_FULL);
                                    Mat[] spilt_mat_rgb = mat_image_src_high.Split();
                                    byte[] img_data_array_high_r = new byte[image_length * image_width];
                                    byte[] img_data_array_high_g = new byte[image_length * image_width];
                                    byte[] img_data_array_high_b = new byte[image_length * image_width];
                                    spilt_mat_rgb[0].GetArray(0, 0, img_data_array_high_b);
                                    spilt_mat_rgb[1].GetArray(0, 0, img_data_array_high_g);
                                    spilt_mat_rgb[2].GetArray(0, 0, img_data_array_high_r);

                                    bool bw_b = Cs_MeM_Image2D.WriteDataByte(img_data_array_high_b, 8192 * 8192 * 0);
                                    bool bw_g = Cs_MeM_Image2D.WriteDataByte(img_data_array_high_g, 8192 * 8192 * 1);
                                    bool bw_r = Cs_MeM_Image2D.WriteDataByte(img_data_array_high_r, 8192 * 8192 * 2);

                                    Mat mat_d_set_fun3 = 255 * Mat.Ones(5, 5, MatType.CV_8UC1);
                                    Cv2.ImWrite("D:\\GH_CeramicDetection\\配置\\色差\\" + "RGB3_" + RGBCountNum.ToString() + ".jpg", mat_d_set_fun3);
                                    //if (checkBox_RGB.Checked == true)
                                    //{


                                    //    Cv2.ImWrite("D:\\GH_CeramicDetection\\异常\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bmp", mat_image_src_high);
                                    //}

                                    string koiu = "";
                                }

                                //ProcessingImage(mat_image_src);

                                sw_main1.Stop();
                                sw_main1_time = sw_main1.ElapsedMilliseconds;
                                //label2.BeginInvoke(new Action(() => { label2.Text = sw_main1_time.ToString(); }));
                                //lbl_Count.BeginInvoke(new Action(() => { lbl_Count.Text = picCountNum.ToString(); }));

                                //Thread.Sleep(TimeSpan.FromMilliseconds(m_delayedTime));//暂停2ms,让cpu优化一下缓存
                            }

                            //ho_Image_1.Dispose();
                        }

                        //h1_Image.Dispose();
                        // 如果表已经处理完成,线程暂停
                    }

                }
                catch (Exception ex)
                {
                    //HObject ho_Image_1 = new HObject();
                    //ho_Image_1 = h1_Image.CopyObj(1, 1);
                    //HOperatorSet.WriteImage(ho_Image_1, "jpeg", 0, "D:\\Picture1\\" + picCountNum);
                    //Cv2.ImWrite("D:\\Picture1\\" + picCountNum + ".jpg", mat_image_src);

                    // ng_type = 1;
                    //throw;
                    //MessageBox.Show(ex.Message, "err_Cam01");
                    errr_count++;
                    //textBox1.Text = errr_count.ToString() + "----" + ex.Message;
                    if (errr_count >= 10000)
                    {
                        errr_count = 0;
                    }
                }
                // string kk = "";
            }
        }

        private void DestroyObjects()
        {
            //stop grabbing
            //if (m_Xfer[0] != null && m_Xfer[0].Grabbing)
            //    m_Xfer[0].Abort();
            if (m_Xfer[1] != null && m_Xfer[1].Grabbing)
                m_Xfer[1].Abort();

            //if (m_Xfer[0] != null && m_Xfer[0].Initialized)
            //    m_Xfer[0].Destroy();
            if (m_Xfer[1] != null && m_Xfer[1].Initialized)
                m_Xfer[1].Destroy();
            //if (m_Buffers[0] != null && m_Buffers[0].Initialized)
            //    m_Buffers[0].Destroy();
            if (m_Buffers[1] != null && m_Buffers[1].Initialized)
                m_Buffers[1].Destroy();
            //if (m_Buffer1 != null && m_Buffer1.Initialized)
            //    m_Buffer1.Destroy();
            if (m_Buffer2 != null && m_Buffer2.Initialized)
                m_Buffer2.Destroy();
            //if (m_Acquisition[0] != null && m_Acquisition[0].Initialized)
            //    m_Acquisition[0].Destroy();
            if (m_Acquisition[1] != null && m_Acquisition[1].Initialized)
                m_Acquisition[1].Destroy();
            //1235
            if (m_AcqDevice != null && m_AcqDevice.Initialized)
                m_AcqDevice.Destroy();
        }

        public int errr_count = 0;
        public long cz_count = 0;
        public long cz_restore_count = 0;
        public void m_Xfer_XferNotify1(object sender, SapXferNotifyEventArgs argsNotify)
        {
            //首先需判断此帧是否是废弃帧，若是则立即返回，等待下一帧
            // if (argsNotify.Trash) return;
            //Form1 GrabDlg = argsNotify.Context as Form1;
            // If grabbing in trash buffer, do not display the image, update the
            // appropriate number of frames on the status bar instead
            if (argsNotify.Trash)
            {

            }
            //GrabDlg.Invoke(new DisplayFrameAcquired(GrabDlg.ShowFrameNumber), argsNotify.EventCount, true);
            else
            {
                //GrabDlg.Invoke(new DisplayFrameAcquired(GrabDlg.ShowFrameNumber), argsNotify.EventCount, false);
                if (argsNotify.Trash) return;
                //获取m_Buffers的地址（指针），只要知道了图片内存的地址，其实就能有各种办法搞出图片了（例如转成Bitmap）
                IntPtr addr2;
                //m_Buffer1.GetAddress(out addr1);  //高角度图像地址
                m_Buffer2.GetAddress(out addr2);
                //观察buffer中的图片的一些属性值，语句后注释里面的值是可能的值
                //int count = m_Buffer1.Count;  //2
                //SapFormat format = m_Buffer1.Format;
                //mat_image_src_low = new Mat(13400, 8192, MatType.CV_8UC1, addr1);

                //mat_image_src_high = new Mat(13400, 8192, MatType.CV_8UC3, addr2);
                mat_image_src_high = new Mat(image_length, image_width, MatType.CV_8UC3, addr2);
                //Cv2.CvtColor(mat_image_src_high, mat_image_src_high, ColorConversionCodes.BGR2GRAY);
                //HOperatorSet.GenEmptyObj(out h1_Image);
                //h1_Image.Dispose();
                //HOperatorSet.GenImageInterleaved(out h1_Image, addr, (HTuple)"bgr", (HTuple)m_Buffer.Width, (HTuple)m_Buffer.Height, -1, "byte", 0, 0, 0, 0, -1, 0);


                //Mat img1 = new Mat(13400, 8192, MatType.CV_8UC1, addr1);
                //Mat img1_bgr = new Mat();
                //Cv2.CvtColor(img1, img1_bgr, ColorConversionCodes.GRAY2BGR);
                //Cv2.CvtColor(img1_bgr, img1_bgr, ColorConversionCodes.BGR2GRAY);
                //Mat img2 = new Mat(13400, 8192, MatType.CV_8UC3, addr2);

                //mat_image_src = new Mat(13400, 16384, MatType.CV_8UC1, new Scalar(0, 0, 0));
                //Mat imgROI = new Mat(mat_image_src, new Rect(0, 0, img1_bgr.Width, img1_bgr.Height));
                //img1_bgr.CopyTo(imgROI);

                ////imgROI = new Mat(mat_image_src, new Rect(img1_bgr.Width, 0, img2.Width, img2.Height));
                ////img2.CopyTo(imgROI);

                //img1.Release();
                //img1_bgr.Release();
                //img2.Release();
                //imgROI.Release();
                //GC.Collect();
                //Mat mattt = img1_bgr.Clone();
                //mat_image_src = new Mat(8192, 8192, MatType.CV_8UC1, addr1);  //高角度图像
                //Mat mat_image_src11 = new Mat(13400, 8192, MatType.CV_8UC1, new Scalar(0, 0, 0));
                //Mat imgROI11 = new Mat(mat_image_src, new Rect(0, 0, img1_bgr.Width, img1_bgr.Height));
                //img1_bgr.CopyTo(imgROI11);
                //Cv2.ImWrite(@"D:\GH_CeramicDetection\异常\相机\blur_src" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg", img1);

                // 启动线程
                if (m_threadStartFlg == false)
                {
                    //new Thread(cameraStart).Start();
                }

                //if (Cs_SerialOpera_001_opera.En_Camera_Work == 0)
                //{
                picCountNum++;
                RGBCountNum++;
                cz_count++;
                //label5.BeginInvoke(new Action(() => { label5.Text = cz_count.ToString(); }));
                //}
                cz_count_temp++;
                //label15.BeginInvoke(new Action(() => { label15.Text = cz_count_temp.ToString(); }));
                //label1.Text = cz_count.ToString();

            }
        }

        private bool CreateObjects()
        {

            if (m_online)
            {
                //m_Buffer1.Count = 1;
                //m_Buffer1.Width = m_Acquisition[0].XferParams.Width;
                //m_Buffer1.Height = m_Acquisition[0].XferParams.Height;
                //m_Buffer1.Format = m_Acquisition[0].XferParams.Format;
                //m_Buffer1.PixelDepth = m_Acquisition[0].XferParams.PixelDepth;

                m_Buffer2.Count = 1;
                m_Buffer2.Width = m_Acquisition[1].XferParams.Width;
                m_Buffer2.Height = m_Acquisition[1].XferParams.Height;
                m_Buffer2.Format = m_Acquisition[1].XferParams.Format;
                m_Buffer2.PixelDepth = m_Acquisition[1].XferParams.PixelDepth;

            }
            // Create buffer object
            //if (m_Buffer1 != null && !m_Buffer1.Initialized)
            //{
            //    if (m_Buffer1.Create() == false)
            //    {
            //        DestroyObjects();
            //        return false;
            //    }
            //    m_Buffer1.Clear();
            //}

            if (m_Buffer2 != null && !m_Buffer2.Initialized)
            {
                if (m_Buffer2.Create() == false)
                {
                    DestroyObjects();
                    return false;
                }
                m_Buffer2.Clear();
            }

            if (m_online)
            {
                //m_Buffers[0].SetRoi(0, 0, m_Acquisition[0].XferParams.Width, m_Acquisition[0].XferParams.Height);
                //if (m_Buffers[0] != null && !m_Buffers[0].Initialized)
                //{
                //    if (m_Buffers[0].Create() == false)
                //    {
                //        DestroyObjects();
                //        return false;
                //    }
                //}

                m_Buffers[1].SetRoi(0, 0, m_Acquisition[1].XferParams.Width, m_Acquisition[1].XferParams.Height);
                if (m_Buffers[1] != null && !m_Buffers[1].Initialized)
                {
                    if (m_Buffers[1].Create() == false)
                    {
                        DestroyObjects();
                        return false;
                    }
                }
            }


            // Create Xfer object
            //if (m_Xfer[0] != null && !m_Xfer[0].Initialized)
            //{
            //    if (m_Xfer[0].Create() == false)
            //    {
            //        DestroyObjects();
            //        return false;
            //    }
            //}
            if (m_Xfer[1] != null && !m_Xfer[1].Initialized)
            {
                if (m_Xfer[1].Create() == false)
                {
                    DestroyObjects();
                    return false;
                }
            }
            return true;
        }

        private void DisposeObjects()
        {

            if (m_Xfer[1] != null)
            { m_Xfer[1].Dispose(); }
            //if (m_Xfer[0] != null)
            //{ m_Xfer[0].Dispose(); m_Xfer = null; }


            if (m_Buffers[1] != null)
            { m_Buffers[1].Dispose(); }
            //if (m_Buffers[0] != null)
            //{ m_Buffers[0].Dispose(); m_Buffers = null; }

            //if (m_Buffer1 != null)
            //{ m_Buffer1.Dispose(); m_Buffer1 = null; }
            if (m_Buffer2 != null)
            { m_Buffer2.Dispose(); m_Buffer2 = null; }

            if (m_Acquisition[1] != null)
            { m_Acquisition[1].Dispose(); }
            //if (m_Acquisition[0] != null)
            //{ m_Acquisition[0].Dispose(); m_Acquisition = null; }
            //1235
            if (m_AcqDevice != null)
            { m_AcqDevice.Dispose(); m_AcqDevice = null; }
        }




        public int image_width;
        public int image_length;
        public string str_chima_msg = "";



        public void SendMessage_Daemon(string str)
        {


            CopyData_Struct cds;
            cds.dwData = (IntPtr)1; //这里可以传入一些自定义的数据，但只能是4字节整数        
            cds.lpData = str;    //消息字符串  
            cds.cbData = System.Text.Encoding.Default.GetBytes(str).Length + 1;  //注意，这里的长度是按字节来算的  
            SendMessage(FindWindow(null, "Daemon_Process"), WM_COPYDATA, 0, ref cds);       // 发送给窗口标题是"Form1"的进程
        }


        private void Main_Timer_Tick(object sender, EventArgs e)
        {
            if (Judge_Image_Mem_OK == false)
            {
                Judge_Image_Mem_OK = Cs_MeM_Image2D.OpenMemMap("Mem_Camera_Image", 8192 * 10000 * 3);
                //Judge_Image_Mem_OK = Cs_MeM_Image2D.OpenMemMap("Mem_Camera_Image", (uint)image_width * (uint)image_length * 10);
            }
            else
            {
                if (Sys_Start == true)
                {
                    //1235
                    //  btn_grab_Click(null, null);
                    Sys_Start = false;
                }


                //串口测试
                //if (Cs_SerialOpera_001_opera.Command_comm.IsOpen)
                //{
                //    try
                //    {
                //        //00 00 00 00 00 00 00 00 00 00

                //        string str_send11 = "55 AA 01 02 03 04 05 06 " + (12 / 255).ToString("X2") + " " + (12 % 255).ToString("X2") + " " + ((int)(Math.Ceiling(float.Parse(lbl_currExposureValue.Text)))).ToString("X2") + " 02 03 04 05 06 07 08 33 CC";

                //        Cs_SerialOpera_001_opera.CDL_Command_Send(str_send11);
                //        //Cs_SerialOpera_001_opera.CDL_Command_Send("55 AA 01 02 03 04 05 00 00 00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 33 CC");
                //    }
                //    catch
                //    {

                //        //MessageBox.Show("请检查输入数据是否正确，修改后重新电机写入11");
                //    }
                //}
            }
            SendMessage_Daemon("Camera_RGB");

            //if ((Cs_SerialOpera_001_opera.En_Camera_Work != 1) && (cz_count_temp >= 1))
            //{
            //    //SendMessage_Operation("Camera_RGB");
            //    Mat mat_Camera_RGB = 255 * Mat.Ones(5, 5, MatType.CV_8UC1);
            //    Cv2.ImWrite(@"D:\GH_CeramicDetection\配置\相机\Camera_RGB.jpg", mat_Camera_RGB);
            //}

            //txt_serial.Text = Cs_SerialOpera_001_opera.str_data_rev;
            GC.Collect();

            Timer_1s_Count++;
            if (Timer_1s_Count >= 2)
            {
                Timer_1s_Count = 0;

                if (lbl_GainValue.Text == "1")
                {
                    txt_GainValue.Text = "2";
                    btn_setGainValue_Click(null, null);
                }

                //if (Cs_SerialOpera_001_opera.m_exposure_value > 0)
                //{
                //    m_exposure_restore_value = (int)(Math.Ceiling(float.Parse(lbl_currExposureValue.Text)));
                //    if ((m_exposure_restore_value != Cs_SerialOpera_001_opera.m_exposure_value) && (Cs_SerialOpera_001_opera.m_exposure_value <= 180))
                //    {
                //        txt_ExposureValue.Text = Cs_SerialOpera_001_opera.m_exposure_value.ToString();
                //        btn_setExposureValue_Click(null, null);
                //    }
                //    else
                //    {

                //    }

                //}
                //try
                //{
                //    var files = Directory.GetFiles(@"D:\GH_CeramicDetection\配置\尺寸\high_bg", "*.jpg");

                //    foreach (var file in files)
                //    {
                //        string str_name = System.IO.Path.GetFileNameWithoutExtension(file);//file.ToString();
                //        if ((str_name != txt_ExposureValue.Text) && (int.Parse(str_name) > 10) && (int.Parse(str_name) < 200) && (str_name != "high_bg_ok"))
                //        {
                //            try
                //            {

                //                Directory.Delete(@"D:\GH_CeramicDetection\配置\尺寸\high_bg", true);
                //                Directory.CreateDirectory(@"D:\GH_CeramicDetection\配置\尺寸\high_bg");



                //                Mat mat_d1_set = 255 * Mat.Ones(100, 100, MatType.CV_8UC1);
                //                Cv2.ImWrite(@"D:\GH_CeramicDetection\配置\尺寸\high_bg\" + "high_bg_ok.jpg", mat_d1_set);

                //                //bz_cut_seban_test_v = int.Parse(txt_small_seban_cut.Text);
                //                //bz_cut_aotu_test_v = int.Parse(txt_small_aotu_cut.Text);

                //                txt_ExposureValue.Text = System.IO.Path.GetFileNameWithoutExtension(file);//file.ToString();
                //                btn_setExposureValue_Click(null, null);

                //            }
                //            catch
                //            {

                //            }





                //        }
                //    }
                //    //private void btn_setExposureValue_Click(object sender, EventArgs e)
                //    //{
                //    //if (!string.IsNullOrEmpty(txt_ExposureValue.Text.Trim()))

                //    //Mat Mat_ExposureValue = new Mat("D:\\GH_CeramicDetection\\配置\\尺寸\\chicun\\900x1800.jpg", ImreadModes.AnyDepth);
                //    //image_width = 8192;
                //    //image_length = 15840;
                //    //str_chima_msg = "900x1800";
                //}
                //catch
                //{

                //}

                if ((image_length != int.Parse(lbl_imagelegth.Text)) && (image_length > 0) && (int.Parse(lbl_imagelegth.Text) > 0))
                {
                    btn_freeze_Click(null, null);
                    Thread.Sleep(300);
                    txt_imageHeighth.Text = image_length.ToString();

                    btn_saveImageHighth_Click(null, null);
                    Thread.Sleep(300);
                    Application.Exit();
                }
            }


            if (lbl_currExposureValue.Text.Contains("---"))
            {

            }
            else if (init_img == 0)
            {


                try
                {

                    //Directory.Delete(@"D:\GH_CeramicDetection\配置\尺寸\high_bg", true);
                    //Directory.CreateDirectory(@"D:\GH_CeramicDetection\配置\尺寸\high_bg");



                    //Mat mat_d1_set = 255 * Mat.Ones(100, 100, MatType.CV_8UC1);
                    //Cv2.ImWrite(@"D:\GH_CeramicDetection\配置\尺寸\high_bg\" + lbl_currExposureValue.Text + ".jpg", mat_d1_set);

                    //bz_cut_seban_test_v = int.Parse(txt_small_seban_cut.Text);
                    //bz_cut_aotu_test_v = int.Parse(txt_small_aotu_cut.Text);
                    string lpoit = "";
                    init_img = 1;
                }
                catch
                {

                }
            }
        }

        public double exposureTime;
        private bool GetCurrExposureValue()
        {
            try
            {
                //double exposureTime;
                if (m_AcqDevice.GetFeatureValue("ExposureTime", out exposureTime))
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => lbl_currExposureValue.Text = exposureTime.ToString()));
                    }
                    else
                    {
                        lbl_currExposureValue.Text = exposureTime.ToString();
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }


        public void btn_setExposureValue_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txt_ExposureValue.Text.Trim()))
            {
                if (double.Parse(txt_ExposureValue.Text.Trim()) <= 180 && double.Parse(txt_ExposureValue.Text.Trim()) >= 15)
                {
                    double valueTmp = double.Parse(txt_ExposureValue.Text.Trim());
                    if (m_AcqDevice.SetFeatureValue("ExposureTime", valueTmp))
                    {
                        //   m_AcqDevice.SaveFeatures(CameraConfigPath);
                        GetCurrExposureValue();
                        //MessageBox.Show("曝光设置成功!");
                    }
                    else
                    {
                        //MessageBox.Show("曝光值设置失败!");
                    }
                }
                else
                {
                    MessageBox.Show("曝光值设置范围不得超过[7-300]");
                }
            }
            else
            {
                MessageBox.Show("曝光设置值不能为空!");
            }
        }

        private bool GetCurrGainValue()
        {
            try
            {
                double gain;
                if (m_AcqDevice.GetFeatureValue("Gain", out gain))
                {

                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => lbl_GainValue.Text = gain.ToString()));
                    }
                    else
                    {
                        lbl_GainValue.Text = gain.ToString();
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }


        private void btn_setGainValue_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txt_GainValue.Text.Trim()))
            {
                if (double.Parse(txt_GainValue.Text.Trim()) <= 9.9 || double.Parse(txt_GainValue.Text.Trim()) >= 1)
                {
                    double valueTmp = double.Parse(txt_GainValue.Text.Trim());
                    if (m_AcqDevice.SetFeatureValue("Gain", valueTmp))
                    {
                        //   m_AcqDevice.SaveFeatures(CameraConfigPath);
                        GetCurrGainValue();
                        MessageBox.Show("增益设置成功!");
                    }
                    else
                    {
                        MessageBox.Show("增益值设置失败!");
                    }
                }
                else
                {
                    MessageBox.Show("增益值设置范围不得超过[1-9.9]");
                }
            }
            else
            {
                MessageBox.Show("增益设置值不能为空!");
            }
        }


        public void btn_freeze_Click(object sender, EventArgs e)
        {
            //   AbortDlg abort0 = new AbortDlg(m_Xfer[0]);
            AbortDlg abort1 = new AbortDlg(m_Xfer[1]);
            //1235
            // if (m_Xfer[0].Freeze() && m_Xfer[1].Freeze())
            if (m_Xfer[1].Freeze())
            {
                //abort0.ShowDialog() != DialogResult.OK &&  1235
                if (abort1.ShowDialog() != DialogResult.OK)
                {
                    //   m_Xfer[0].Abort();
                    m_Xfer[1].Abort();
                }
                UpdateControls();

                m_grabFlg = 0;
                //1235
                isInited = false;
                Main_Timer.Stop();
            }
        }


        private bool SetBufferImageSize(int heigth, bool isUpdateNow)
        {
            try
            {
                bool ok2 = m_Acquisition[1].SetParameter(SapAcquisition.Prm.CROP_HEIGHT, heigth, isUpdateNow);
                bool ok3 = m_Acquisition[1].SaveParameters(BoardConfigPath);
                if (ok2 && ok3)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

        }


        private bool GetBufferImageSize(out int wth, out int hth)
        {
            try
            {
                int width, highth;
                bool ok1 = m_Acquisition[1].GetParameter(SapAcquisition.Prm.CROP_HEIGHT, out highth);
                bool ok2 = m_Acquisition[1].GetParameter(SapAcquisition.Prm.CROP_WIDTH, out width);
                if (ok1 && ok2)
                {
                    wth = width;
                    hth = highth;
                    return true;
                }
                else
                {
                    wth = -1;
                    hth = -1;
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

        }


        private void ShowImageInfo(int wth, int lth)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => { lbl_imagelegth.Text = lth.ToString(); }));
            }
            else
            {
                lbl_imagelegth.Text = lth.ToString();
                //lbl_imageWidth.Text = wth.ToString();
            }
        }


        private void btn_saveImageHighth_Click(object sender, EventArgs e)
        {
            //WriteConfigParam("ImageHighConfig", txt_imageHeighth.Text.Trim());
            //WriteConfigParam("IsImageSizeChang", "true");
            if (!isInited)
            {
                if (!string.IsNullOrEmpty(txt_imageHeighth.Text.Trim()))
                {
                    bool isok1 = SetBufferImageSize(Convert.ToInt32(txt_imageHeighth.Text.Trim()), false);
                    bool isok2 = GetBufferImageSize(out image_width, out image_length);
                    if (isok1 && isok2)
                    {
                        ShowImageInfo(image_width, Convert.ToInt32(txt_imageHeighth.Text.Trim()));
                        //MessageBox.Show("图片大小设置成功!");
                    }
                    else
                    {
                        //MessageBox.Show("图片大小设置失败!");
                    }


                }
                else
                {
                    MessageBox.Show("图像高度设置值不能为空!");
                }
            }
            else
            {
                MessageBox.Show("请先点击 [Freeze] 按钮停止采集!");
            }
        }



        void UpdateControls()
        {

            bool bAcqNoGrab = (m_Xfer[1] != null) && (m_Xfer[1].Grabbing == false);
            bool bAcqGrab = (m_Xfer[1] != null) && (m_Xfer[1].Grabbing == true);
            bool bNoGrab = (m_Xfer[1] == null) || (m_Xfer[1].Grabbing == false);

            // Acquisition Control
            btn_grab.Enabled = bAcqNoGrab && m_online;
            //btn_snap.Enabled = bAcqNoGrab && m_online;
            btn_freeze.Enabled = bAcqGrab && m_online;
        }


        private void btn_grab_Click(object sender, EventArgs e)
        {
            //if (m_Xfer[0].Grab() && m_Xfer[1].Grab())
            if (m_Xfer[1].Grab())
            {
                UpdateControls();

                m_grabFlg = 2;

                //1235
                isInited = true;
                Main_Timer.Enabled = true;
                Main_Timer.Start();
            }
        }
    }
}
