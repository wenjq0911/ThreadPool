# ThreadPool
一个简单的线程池类库
# 调用方法
获取实例<br>
`ThreadPoolManager.getInstance();`<br>
或<br>
`ThreadPoolManager.getInstance(int min,int max,int increment);`<br>
<br>
调用一个线程执行任务<br>
`ThreadPoolManager.getInstence().AddTaskItem(new WaitCallback(HandleFunction), object param);`
