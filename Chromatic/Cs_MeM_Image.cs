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
    internal class Cs_MeM_Image
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


        const int FILE_MAP_ALL_ACCESS = 0x0002 | 0x0004;
        const int PAGE_READONLY = 0x02;
        const int PAGE_READWRITE = 0x04;
        const int PAGE_WRITECOPY = 0x08;
        const int INVALID_HANDLE_VALUE = -1;


        static IntPtr p_MemoryFileW = IntPtr.Zero;//数据写入开始地址指针
        static IntPtr p_MemoryFileR = IntPtr.Zero;//数据读出开始地址指针
        static IntPtr Memory_Pointer1 = IntPtr.Zero;
        static IntPtr Memory_Pointer2 = IntPtr.Zero;
        static IntPtr Memory_Pointer3 = IntPtr.Zero;


        //strName：内存映射文件名称---与OpenMemMap必须一致
        public static bool CreateMemMap(string strName, long lngSize)
        {
            p_MemoryFileW = CreateFileMapping(//创建映射文件
                INVALID_HANDLE_VALUE,
                IntPtr.Zero,
                (uint)PAGE_READWRITE,
                0,
                (uint)lngSize, strName);

            if (IntPtr.Zero == p_MemoryFileW)
            {
                return false;
            }



            Memory_Pointer1 = MapViewOfFile(p_MemoryFileW, FILE_MAP_ALL_ACCESS, 0, 0, 10000 * 200000);//开辟一个10000 * 200000字节长的映射长度，这里在自己电脑允许的前提可以可以开设大一点，便于读写多个文件

            if (Memory_Pointer1 == IntPtr.Zero)
            {
                CloseHandle(p_MemoryFileW);
                return false;
            }

            return true;

        }


        //向内存以字节方式写入数据
        //wr_group_byte：需要写入的字节数组
        //add_bias_length:地址偏置，一般用于多文件写入
        public static bool WriteDataByte(byte[] wr_group_byte, int add_bias_length)
        {


            Marshal.Copy(wr_group_byte, 0, Memory_Pointer1 + sizeof(byte) * add_bias_length, wr_group_byte.Length);
            return true;
        }


        //OpenMemMap一般是内存读取者在内存写入者创建文件映射之后执行的，这里要注意必须要先CreateMemMap，才能OpenMemMap
        //strName：内存映射文件名称---与CreateMemMap必须一致
        public static bool OpenMemMap(string strName)
        {
            p_MemoryFileR = OpenFileMapping(
                FILE_MAP_ALL_ACCESS, false, strName);

            if (IntPtr.Zero == p_MemoryFileR)
            {
                return false;
            }

            Memory_Pointer1 = MapViewOfFile(p_MemoryFileR, FILE_MAP_ALL_ACCESS, 0, 0, 8192 * 20000 * 11);//开辟一个10000 * 200000字节长的映射长度，这里在自己电脑允许的前提可以可以开设大一点，便于读写多个文件

            if (Memory_Pointer1 == IntPtr.Zero)
            {
                CloseHandle(p_MemoryFileR);
                return false;
            }
            return true;

        }


        //向内存以字节方式读取数据
        //add_bias_length：地址偏置，一般用于多文件读取，即从内存的第几个字节开始读取
        //read_data_length:需要读取的数据长度
        //本函数可以通俗的理解为从哪里开始读取，并且需要读取多少数据
        public static byte[] ReadDataByte(int add_bias_length, int read_data_length)
        {
            byte[] bytData_false = new byte[1];
            byte[] bytData_OK = new byte[read_data_length];
            Marshal.Copy(Memory_Pointer1 + sizeof(byte) * add_bias_length, bytData_OK, 0, bytData_OK.Length);
            return bytData_OK;
        }


        //向内存以short类型方式写入数据---因为我项目里有些图像数据是16位的，这里需要用到short类型
        //wr_group_short：需要写入的字节数组
        //add_bias_length:地址偏置，一般用于多文件写入
        public static bool WriteDataUshort(short[] wr_group_short, int add_bias_length)
        {


            Marshal.Copy(wr_group_short, 0, Memory_Pointer1 + sizeof(short) * add_bias_length, wr_group_short.Length);
            return true;
        }


        //向内存以short类型方式读取数据---因为我项目里有些图像数据是16位的，这里需要用到short类型
        //add_bias_length：地址偏置，一般用于多文件读取，即从内存的第几个short开始读取
        //read_data_length:需要读取的数据长度
        //本函数可以通俗的理解为从哪里开始读取，并且需要读取多少数据
        public static short[] ReadDataShort(int add_bias_length, int read_data_length)
        {
            short[] bytData_false = new short[1];
            short[] bytData_OK = new short[read_data_length];
            Marshal.Copy(Memory_Pointer1 + sizeof(short) * add_bias_length, bytData_OK, 0, bytData_OK.Length);
            return bytData_OK;
        }
    }
}
