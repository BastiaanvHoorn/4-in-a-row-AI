using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace connect4
{
    public static class button_awaiter_extensions
    {
        public static button_awaiter GetAwaiter(this Button button)
        {
            return new button_awaiter()
            {
                Button = button
            };
        }
    }

    public class button_awaiter : INotifyCompletion
    {
        public bool IsCompleted
        {
            get { return false; }
        }

        public void GetResult()
        {

        }

        public Button Button { get; set; }

        public void OnCompleted(Action continuation)
        {
            EventHandler h = null;
            h = (o, e) =>
            {
                Button.Click -= h;
                continuation();
            };
            Button.Click += h;
        }
    }
}
