using System;
using System.Collections.Generic;
using System.Linq;
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
                r.BeginInvoke(sender, e, new AsyncCallback((IAsyncResult ar) =>
                {
                    r.EndInvoke(ar);
                    tcs.TrySetResult(true);
                }), null);
                await tcs.Task;
            }
        }
    }
}
