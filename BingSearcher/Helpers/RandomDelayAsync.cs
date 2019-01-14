using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace BingSearcher
{
    class RandomDelay
    {
        static int sleep_dur_s = 0;

        internal void Delay(string msg, int min, int max)
        {
            sleep_dur_s = new Random().Next(min, max);

            if (!string.IsNullOrEmpty(msg))
            {
                Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] {msg} resuming at: {DateTime.Now.AddSeconds(sleep_dur_s)}");
            }

            Thread.Sleep(sleep_dur_s * 1000);
        }
    }
}
