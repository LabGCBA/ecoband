using System;
using System.Threading.Tasks;
using System.Threading;


namespace EcoBand {
    static class TaskExtensions {
        public static void NoAwait(this Task task) {
            
        }

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout, CancellationTokenSource tokenSource) {
            if (await Task.WhenAny(task, Task.Delay(timeout, tokenSource.Token)) != task) throw new TimeoutException("The operation has timed out.");
            else tokenSource.Cancel();
        }
    }
}
