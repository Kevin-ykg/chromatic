using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using OpenCvSharp;
using OpenCvSharp.Extensions;



namespace Chromatic
{
    internal class Cs_MeM_Image2D
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFileMapping(int hFile, IntPtr lpAttributes, uint flProtect, uint dwMaxSizeHi, uint dwMaxSizeLow, string lpName);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr OpenFileMapping(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, string lpName);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMapping, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool UnmapViewOfFile(IntPtr pvBaseAddress);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32", EntryPoint = "GetLastError")]
        public static extern int GetLastError();

        const int ERROR_ALREADY_EXISTS = 183;

        const int FILE_MAP_COPY = 0x0001;
        const int FILE_MAP_WRITE = 0x0002;
        const int FILE_MAP_READ = 0x0004;
        const int FILE_MAP_ALL_ACCESS = 0x0002 | 0x0004;

        const int PAGE_READONLY = 0x02;
        const int PAGE_READWRITE = 0x04;
        const int PAGE_WRITECOPY = 0x08;
        const int PAGE_EXECUTE = 0x10;
        const int PAGE_EXECUTE_READ = 0x20;
        const int PAGE_EXECUTE_READWRITE = 0x40;

        const int SEC_COMMIT = 0x8000000;
        const int SEC_IMAGE = 0x1000000;
        const int SEC_NOCACHE = 0x10000000;
        const int SEC_RESERVE = 0x4000000;

        const int INVALID_HANDLE_VALUE = -1;


        static IntPtr m_hSharedMemoryFileW = IntPtr.Zero;
        static IntPtr m_hSharedMemoryFileR = IntPtr.Zero;
        static IntPtr 内存指针1 = IntPtr.Zero;
        static IntPtr 内存指针2 = IntPtr.Zero;
        static IntPtr 内存指针3 = IntPtr.Zero;

        static Semaphore semWrite;

        public static bool OpenMemMap(string strName, uint lngSize)
        {
            m_hSharedMemoryFileR = OpenFileMapping(
                FILE_MAP_ALL_ACCESS, false, strName);

            if (IntPtr.Zero == m_hSharedMemoryFileR)
            {
                return false;
            }

            内存指针1 = MapViewOfFile(m_hSharedMemoryFileR, FILE_MAP_ALL_ACCESS, 0, 0, lngSize);

            if (内存指针1 == IntPtr.Zero)
            {
                CloseHandle(m_hSharedMemoryFileR);
                return false;
            }
            return true;

        }

        public static bool CreateMemMap(string strName, uint lngSize)
        {
            m_hSharedMemoryFileW = CreateFileMapping(
                INVALID_HANDLE_VALUE,
                IntPtr.Zero,
                (uint)PAGE_READWRITE,
                0,
                (uint)lngSize, strName);

            if (IntPtr.Zero == m_hSharedMemoryFileW)
            {
                return false;
            }



            内存指针1 = MapViewOfFile(m_hSharedMemoryFileW, FILE_MAP_ALL_ACCESS, 0, 0, lngSize);

            if (内存指针1 == IntPtr.Zero)
            {
                CloseHandle(m_hSharedMemoryFileW);
                return false;
            }

            return true;

        }

        public static bool WriteDataByte(byte[] wr_group_byte, int add_bias_length)
        {


            Marshal.Copy(wr_group_byte, 0, 内存指针1 + sizeof(byte) * add_bias_length, wr_group_byte.Length);

            //CloseHandle(m_hSharedMemoryFile1);
            return true;
        }

        public static bool WriteData(int width, int height)
        {
            /*
            内存指针1 = MapViewOfFile(m_hSharedMemoryFileW, FILE_MAP_ALL_ACCESS, 0, 0, 2000 * 5000*16);

            if (内存指针1 == IntPtr.Zero)
            {
                CloseHandle(m_hSharedMemoryFileW);
                return false;
            }
            */

            Byte[] bytData_width = BitConverter.GetBytes(width);
            Marshal.Copy(bytData_width, 0, 内存指针1, bytData_width.Length);
            Byte[] bytData_height = BitConverter.GetBytes(height);
            Marshal.Copy(bytData_height, 0, 内存指针1 + sizeof(byte) * bytData_height.Length, bytData_height.Length);

            Mat src = Cv2.ImRead(@"D:\Company_Project\瓷砖DLL实时存图\V1_DLL\设备_总亮度裁剪1通道.bmp", ImreadModes.AnyDepth);
            byte[] img_data_array_src = new byte[src.Rows * src.Cols];
            src.GetArray(0, 0, img_data_array_src);
            Marshal.Copy(img_data_array_src, 0, 内存指针1 + sizeof(byte) * bytData_width.Length + sizeof(byte) * bytData_height.Length, img_data_array_src.Length);

            //UnmapViewOfFile(内存指针1);
            //CloseHandle(m_hSharedMemoryFile1);
            return true;
        }

        public static byte[] ReadDataByte(int add_bias_length, int read_data_length)
        {
            byte[] bytData_false = new byte[1];
            /*
            内存指针1 = MapViewOfFile(m_hSharedMemoryFileR, FILE_MAP_ALL_ACCESS, 0, 0, 2000 * 5000*16);

            if (内存指针1 == IntPtr.Zero)
            {
                CloseHandle(m_hSharedMemoryFileR);
                return bytData_false;
            }
            */
            /*
            Byte[] bytData_width = BitConverter.GetBytes(width);
            Marshal.Copy(bytData_width, 0, 内存指针1, bytData_width.Length);
            Byte[] bytData_height = BitConverter.GetBytes(height);
            Marshal.Copy(bytData_height, 0, 内存指针1 + sizeof(byte) * bytData_height.Length, bytData_height.Length);
            */


            /*
            Byte[] bytData1 = new Byte[4];
            Marshal.Copy(内存指针1, bytData1, 0, bytData1.Length);
            Byte[] bytData2 = new Byte[4];
            Marshal.Copy(内存指针1 + sizeof(byte) * bytData2.Length, bytData2, 0, bytData2.Length);
            //BitConverter.ToInt32(bytData, 0);

            Mat src = Cv2.ImRead(@"D:\Company_Project\瓷砖DLL实时存图\V1_DLL\设备_总亮度裁剪1通道.bmp", ImreadModes.AnyDepth);
            byte[] img_data_array_dst = new byte[src.Rows * src.Cols];

            Marshal.Copy(内存指针1 + sizeof(byte) * bytData2.Length + sizeof(byte) * bytData2.Length, img_data_array_dst, 0, img_data_array_dst.Length);

            UnmapViewOfFile(内存指针1);
            */
            //CloseHandle(m_hSharedMemoryFile1);
            byte[] bytData_OK = new byte[read_data_length];
            Marshal.Copy(内存指针1 + sizeof(byte) * add_bias_length, bytData_OK, 0, bytData_OK.Length);
            return bytData_OK;
        }

        public static short[] ReadDataShort(int add_bias_length, int read_data_length)
        {
            short[] bytData_false = new short[1];
            /*
            内存指针1 = MapViewOfFile(m_hSharedMemoryFileR, FILE_MAP_ALL_ACCESS, 0, 0, 2000 * 5000*16);

            if (内存指针1 == IntPtr.Zero)
            {
                CloseHandle(m_hSharedMemoryFileR);
                return bytData_false;
            }
            */
            short[] bytData_OK = new short[read_data_length];
            Marshal.Copy(内存指针1 + sizeof(short) * add_bias_length, bytData_OK, 0, bytData_OK.Length);
            return bytData_OK;
        }

        public static bool UnmapViewOfFileW_Opera()
        {

            UnmapViewOfFile(内存指针1);
            CloseHandle(m_hSharedMemoryFileW);
            return true;
        }

        public static bool UnmapViewOfFileR_Opera()
        {

            UnmapViewOfFile(内存指针1);
            CloseHandle(m_hSharedMemoryFileR);
            return true;
        }
    }
}
