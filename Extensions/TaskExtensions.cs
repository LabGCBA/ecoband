using System;
using System.Threading.Tasks;
using System.Threading;


namespace EcoBand {
    static class TaskExtensions {
        public static void NoAwait(this Task task) {
            
        }

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout) {
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task) throw new TimeoutException("The operation has timed out.");
        }
    }
}
