using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace TwitchIRC.Utility_Primitives
{
    public static class EventHandlerExtensions
    {
        public static async Task InvokeAsync<T>(this EventHandler<T> Handler, object sender, T e) where T : EventArgs
        {
            foreach (EventHandler<T> r in Handler.GetInvocationList())
            {
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                r.BeginInvoke(sender, e, new AsyncCallback(_CompleteAsyncInvoke<T>), tcs);
                await tcs.Task;
            }
        }
        internal static void _CompleteAsyncInvoke<T>(IAsyncResult ar)
        {
            EventHandler<T> r = (ar as AsyncResult)?.AsyncDelegate as EventHandler<T>;
            TaskCompletionSource<bool> tcs = (ar as AsyncResult)?.AsyncState as TaskCompletionSource<bool>;
            try
            {
                r.EndInvoke(ar);
            }catch(Exception ex)
            {
                tcs?.TrySetException(ex);
            }
            tcs?.TrySetResult(true);
        }
    }
}
