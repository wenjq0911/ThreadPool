using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;



namespace ThreadPool
{

    public class ThreadPoolManager : IDisposable
    {

        private int max = 5; //默认最大线程数

        private int min = 1;  //默认最小线程数

        private int increment = 1; //当活动线程不足的时候新增线程的默认增量

        private Dictionary<string, NTask> publicPool; //所有的线程

        public Dictionary<string, NTask> PublicPool
        {
            get { return publicPool; }
            set { publicPool = value; }
        }

        private Queue<NTask> freeQueue;  //空闲线程队列

        private Dictionary<string, NTask> working;   //正在工作的线程

        private List<string> workingKeys;

        private Queue<WaitItem> waitQueue;  //等待执行工作队列

        private static ThreadPoolManager threadPoolManager=null;

        //设置最大线程数

        public void Setmaxthread(int Value)
        {

            lock (this)
            {

                max = Value;

            }

        }

        //设置最小线程数

        public void Setminthread(int Value)
        {

            lock (this)
            {

                min = Value;

            }

        }

        //设置增量

        public void Setincrement(int Value)
        {

            lock (this)
            {

                increment = Value;

            }

        }

        private ThreadPoolManager(int min, int max, int increment)
        {
            this.min = min;
            this.max = max;
            this.increment = increment;
            initThreadTool();
        }

        private ThreadPoolManager()
        {
            initThreadTool();
        }

        /// <summary>
        /// 获取实例
        /// </summary>
        /// <param name="min">初始化最小线程数</param>
        /// <param name="max">初始化最大线程数</param>
        /// <param name="increment">线程增量数</param>
        /// <returns></returns>
        public static ThreadPoolManager getInstance(int min, int max, int increment)
        {
            if (threadPoolManager==null)
            {
                threadPoolManager = new ThreadPoolManager(min,max,increment);
            }

            return threadPoolManager;
        }
        public static ThreadPoolManager getInstance()
        {
            if (threadPoolManager == null)
            {
                threadPoolManager = new ThreadPoolManager(1, 5, 1);
            }
            return threadPoolManager;
        }


        /// <summary>
        /// 初始化线程池
        /// </summary>
        void initThreadTool() {

            publicPool = new Dictionary<string, NTask>();

            working = new Dictionary<string, NTask>();

            workingKeys = new List<string>();

            freeQueue = new Queue<NTask>();

            waitQueue = new Queue<WaitItem>();

            NTask t = null;

            //先创建最小线程数的线程

            for (int i = 0; i < min; i++)
            {
                t = new NTask();
                //注册线程完成时触发的事件
                t.WorkComplete += new Action<NTask>(t_WorkComplete);
                //加入到所有线程的字典中
                publicPool.Add(t.Key, t);
                //因为还没加入具体的工作委托就先放入空闲队列
                freeQueue.Enqueue(t);
            }

        }

        //线程执行完毕后的触发事件
        void t_WorkComplete(NTask obj)
        {
            lock (this)
            {

                //首先因为工作执行完了，所以从正在工作字典里删除
                working.Remove(obj.Key);
                //检查是否有等待执行的操作，如果有等待的优先执行等待的任务
                if (waitQueue.Count > 0)
                {
                    //先要注销当前的线程，将其从线程字典删
                    publicPool.Remove(obj.Key);
                    obj.Close();
                    //从等待任务队列提取一个任务
                    WaitItem item = waitQueue.Dequeue();
                    NTask nt = null;
                    //如果有空闲的线程，就是用空闲的线程来处理
                    if (freeQueue.Count > 0)
                    {
                        nt = freeQueue.Dequeue();
                    } else{
                        //如果没有空闲的线程就再新创建一个线程来执行
                        nt = new NTask();
                        publicPool.Add(nt.Key, nt);
                        nt.WorkComplete += new Action<NTask>(t_WorkComplete);
                    }

                    //设置线程的执行委托对象和上下文对象
                    nt.taskWorkItem = item.Works;
                    nt.contextdata = item.Context;
                    //添加到工作字典中
                    working.Add(nt.Key, nt);
                    workingKeys.Add(nt.Key);
                    //唤醒线程开始执行
                    nt.Active();
                }else{

                //如果没有等待执行的操作就回收多余的工作线程
                if (freeQueue.Count > min)
                {
                    //当空闲线程超过最小线程数就回收多余的这一个
                    publicPool.Remove(obj.Key);
                    obj.Dispose();
                }else{

                    //如果没超过就把线程从工作字典放入空闲队列
                    obj.contextdata = null;
                    freeQueue.Enqueue(obj);
                }

                }

            }

        }

        /// <summary>
        /// 添加工作委托
        /// </summary>
        /// <param name="TaskItem"></param>
        /// <param name="Context"></param>
        /// <returns>工作线程ID</returns>
        public int AddTaskItem(WaitCallback TaskItem, object Context)
        {

            lock (this)
            {

                NTask t = null;
                int len = publicPool.Values.Count;


                //如果空闲列表非空并且线程没有到达最大值
                if (freeQueue.Count == 0 && len < max)
                {
                    //如果没有空闲队列了，就根据增量创建线程
                    for (int i = 0; i < increment; i++)
                    {

                        //判断线程的总量不能超过max

                        if ((len + i+1) <= max)
                        {

                            t = new NTask();

                            //设置完成响应事件

                            t.WorkComplete += new Action<NTask>(t_WorkComplete);

                            //加入线程字典

                            publicPool.Add(t.Key, t);

                            //加入空闲队列

                            freeQueue.Enqueue(t);

                        }

                        else
                        {

                            break;

                        }

                    }
                }
                else if (freeQueue.Count == 0 && len == max)
                {
                    //如果线程达到max就把任务加入等待队列
                    waitQueue.Enqueue(new WaitItem() { Context = Context, Works = TaskItem });

                    return -1;
                }

                //从空闲队列pop一个线程

                t = freeQueue.Dequeue();

                //加入工作字典

                working.Add(t.Key, t);

                workingKeys.Add(t.Key);

                //设置执行委托

                t.SetWorkItem ( TaskItem,Context);

                //设置状态对象
                
                //t.contextdata = Context;
                Console.WriteLine(Context + TaskItem.Method.Name);

                //唤醒线程开始执行
                t.Active();
                return t.thread.ManagedThreadId;
            }
        }
        /// <summary>
        /// 通知在当前任务完成后关闭线程
        /// </summary>
        /// <param name="threadId"></param>
        public void CloseAfterCurrTask(int threadId)
        {
            //获取线程NTask对象
            NTask task = getNTask(threadId);
            if(task!=null){
                task.Close();
            }
           
        }

        /// <summary>
        /// 根据线程ID停止一个线程
        /// </summary>
        /// <param name="threadId"></param>
        public void CloseThread(int threadId) {

            //获取线程NTask对象
            NTask task = getNTask(threadId);
            //从工作列表删除，并添加至空闲
            working.Remove(task.Key);
            workingKeys.Remove(task.Key);
            freeQueue.Enqueue(task);
            //task.m_Thread.Suspend();
            task.locks.WaitOne();
        }

        /// <summary>
        /// 根据线程ID获取一个线程实例
        /// </summary>
        /// <param name="threadId"></param>
        /// <returns></returns>
        public NTask getNTask(int threadId)
        {

            //List<NTask> list = working.Select(x => x.Value).Where(x => x.thread.ManagedThreadId == threadId).ToList();

            //if(list.Count>0){
            //    return list[0];
            //}
            for (int i = 0; i < workingKeys.Count; i++)
            {
                string key = workingKeys[i];
                NTask n = working.ContainsKey(key)?working[key]:null;
                if(n!=null&&n.thread.ManagedThreadId==threadId){
                    return n;
                }

            }
            return null;

        }

        //回收资源

        public void Dispose()
        {

            //throw new NotImplementedException();

            foreach (NTask t in publicPool.Values)
            {

                //关闭所有的线程

                using (t) { t.Close(); }

            }

            publicPool.Clear();

            working.Clear();

            workingKeys.Clear();

            waitQueue.Clear();

            freeQueue.Clear();

        }
    }
}