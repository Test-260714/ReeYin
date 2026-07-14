using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Helper
{
    public static class SetTimeHelper
    {
        /// <summary>
        /// 函数调用计时
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static double SetTimer(this Action action)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// 带超时参数的方法(默认设置60s)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public static (T Result, double ElapsedMilliseconds, bool TimedOut) SetTimer<T>(
            this Func<T> func,
            int timeoutMilliseconds = 60 * 1000)
        {
            var stopwatch = new Stopwatch();
            var cts = new CancellationTokenSource();

            try
            {
                cts.CancelAfter(timeoutMilliseconds);
                stopwatch.Start();

                // 在后台线程执行函数
                var task = Task.Run(func, cts.Token);

                // 等待任务完成或超时
                bool completed = task.Wait(timeoutMilliseconds, cts.Token);
                stopwatch.Stop();

                if (completed)
                {
                    return (task.Result, stopwatch.ElapsedMilliseconds, false);
                }
                else
                {
                    return (default(T), stopwatch.ElapsedMilliseconds, true);
                }
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return (default(T), stopwatch.ElapsedMilliseconds, true);
            }
            catch (AggregateException ex)
            {
                stopwatch.Stop();
                // 解包内部异常并重新抛出
                if (ex.InnerException != null)
                    throw ex.InnerException;
                throw;
            }
            finally
            {
                cts.Dispose();
            }
        }


        // 拓展方法，支持带返回值的同步方法，并测量其执行时间
        public static (T Result, double ElapsedMilliseconds) SetTimer<T>(this Func<T> func)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            T result = func();

            stopwatch.Stop();
            double elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            return (result, elapsedMilliseconds);
        }

        /// <summary>
        /// 函数调用计时-异步
        /// </summary>
        /// <param name="func"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static async Task<double> SetTimerAsync(this Func<Task> func, Action<Exception> exception = null)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                await func();
            }
            catch (Exception ex)
            {
                exception?.Invoke(ex);
            }
            finally
            {
                stopwatch.Stop();
            }
            return stopwatch.ElapsedMilliseconds;
        }
    }
}
