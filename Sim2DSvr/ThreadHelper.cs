//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: ThreadHelper.cs
// Date: 20101231  Author: LiYoubing  Version: 1
// Description: 可取消线程任务的线程池定义文件
//              仿真循环处理方法使用线程池中的线程来执行，避免每个仿真周期创建和结束线程
// Histroy:
// Date: 20101231  Author: LiYoubing
// Modification: 修改内容简述
// ……
//-----------------------------------------------------------------------

using System;
using System.Threading;
using System.Collections.Generic;

namespace URWPGSim2D.Sim2DSvr.ThreadHelper
{
    /// <summary>
    /// a type for encapsulating a working task or method to run by a thread
    /// </summary>
    public sealed class WorkItem
    {
        private WaitCallback _callback;
        private object _state;
        private ExecutionContext _ctx;

        internal WorkItem(WaitCallback wc, object state, ExecutionContext ctx)
        {
            _callback = wc; _state = state; _ctx = ctx;
        }

        internal WaitCallback Callback { get { return _callback; } }
        internal object State { get { return _state; } }
        internal ExecutionContext Context { get { return _ctx; } }
    }

    public enum WorkItemStatus { Completed, Queued, Executing, Aborted }

    /// <summary>
    /// an encapsulated threadpool in which the thread can be aborted, using standard threadpool
    /// </summary>
    public static class AbortableThreadPool
    {
        private static LinkedList<WorkItem> _callbacks = new LinkedList<WorkItem>();
        private static Dictionary<WorkItem, Thread> _threads = new Dictionary<WorkItem, Thread>();

        public static Dictionary<WorkItem, Thread> Threads
        {
            get { return _threads; }
        }

        public static WorkItem QueueUserWorkItem(WaitCallback callback)
        {
            return QueueUserWorkItem(callback, null);
        }

        public static WorkItem QueueUserWorkItem(WaitCallback callback, object state)
        {
            if (callback == null) throw new ArgumentNullException("callback");

            WorkItem item = new WorkItem(callback, state, ExecutionContext.Capture());
            lock (_callbacks) _callbacks.AddLast(item);
            ThreadPool.QueueUserWorkItem(new WaitCallback(HandleItem));
            return item;
        }

        private static void HandleItem(object ignored)
        {
            WorkItem item = null;
            try
            {
                lock (_callbacks)
                {
                    if (_callbacks.Count > 0)
                    {
                        item = _callbacks.First.Value;
                        _callbacks.RemoveFirst();
                    }
                    if (item == null) return;
                    _threads.Add(item, Thread.CurrentThread);

                } ExecutionContext.Run(item.Context,
                    delegate { item.Callback(item.State); }, null);
            }
            finally
            {
                lock (_callbacks)
                {
                    if (item != null) _threads.Remove(item);
                }
            }
        }

        public static WorkItemStatus Cancel(WorkItem item, bool allowAbort)
        {
            if (item == null) throw new ArgumentNullException("item");
            lock (_callbacks)
            {
                LinkedListNode<WorkItem> node = _callbacks.Find(item);
                if (node != null)
                {
                    _callbacks.Remove(node);
                    return WorkItemStatus.Queued;
                }
                else if (_threads.ContainsKey(item))
                {
                    if (allowAbort)
                    {
                        _threads[item].Abort();
                        _threads.Remove(item);
                        return WorkItemStatus.Aborted;
                    }
                    else return WorkItemStatus.Executing;
                }
                else return WorkItemStatus.Completed;
            }
        }
    }
}
