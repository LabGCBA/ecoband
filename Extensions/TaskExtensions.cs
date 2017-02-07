using System;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reactive.Concurrency;


namespace EcoBand {
    static class TaskExtensions {
        public static void NoAwait(this Task task) {
            
        }

        public static Task TimeoutAfter(this Task task, TimeSpan timeout, IScheduler scheduler) {
            return task.ToObservable().Timeout(timeout, scheduler).ToTask();
        }
    }
}
