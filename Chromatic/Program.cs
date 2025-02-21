using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace Chromatic
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {

            //原理：当两个或多个线程需要同时访问共享资源时，系统需要一种同步机制来保证同一时刻只有一个线程使用该资源。互斥锁是一种同步原语，它仅向一个线程授予对共享资源的独占访问权限。如果一个线程获取互斥锁，则获取该互斥锁的第二个线程将被挂起，直到第一个线程释放该互斥锁。
            bool repeatFlag = false;
            Mutex mutex = new Mutex(true, Application.ProductName, out repeatFlag);
            if (repeatFlag)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            else
            {
                MessageBox.Show("该程序已经启动，请勿重复打开！", "程序提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

    }
}
