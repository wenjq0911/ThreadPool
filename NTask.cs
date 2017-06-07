using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadPool
{
    public class NTask : IDisposable
    {
        public AutoResetEvent locks { get; set; } //线程锁
        public Thread thread{get;set;}  //线程对象
        public WaitCallback taskWorkItem { get; set; }//线程体委托
        public bool working { get; set; }  //线程是否工作
        public object contextdata{ get; set;}
        public event Action<NTask> WorkComplete;  //线程完成一次操作的事件
        public string Key { get; set; }        //用于字典的Key
        //初始化包装器
        public NTask()
        {
            //设置线程一进入就阻塞
            locks = new AutoResetEvent(false);
            Key = Guid.NewGuid().ToString();
            //初始化线程对象
            thread = new Thread(Work);
            thread.IsBackground = true;
            working = true;
            contextdata = new object();
            //开启线程
            thread.Start();
        }

        //唤醒线程
        public void Active()
        {
            working = true;
            locks.Set();
        }

        //设置执行委托和状态对象
        public void SetWorkItem(WaitCallback action, object context)
        {
            taskWorkItem = action;
            contextdata = context;
        }

        //线程体包装方法
        private void Work()
        {
            while (working)
            {
                //阻塞线程
                locks.WaitOne();
                taskWorkItem(contextdata);
                //完成一次执行，触发事件
                WorkComplete(this);
            }
        }

        //关闭线程
        public void Close()
        {
            working = false;
        }

        //回收资源
        public void Dispose()
        {
            try
            {
                thread.Abort();
            }
            catch { 
            }
        }
    }
}
