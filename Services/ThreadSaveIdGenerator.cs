using System;
using System.Threading;

namespace Coflnet
{
    /// <summary>
    /// Thread save 64bit identifier generator.
    /// Uses the current system time in ms to generate ids.
    /// </summary>
    public static class ThreadSaveIdGenerator
	{
        private const int GroupFactor = 1000;
        private static long _startEpoch = new DateTime(2022, 1, 1).Ticks;
		private static int _index;
        private static int offset;
        private static long _lastTime;
		private static object _threadLock = new object();


		/// <summary>
		/// Gets the next identifier.
		/// Will 'lock' increment and return the incremented index. 
		/// </summary>
		/// <value>The next identifier.</value>
		public static long NextId
		{
			get
			{
				lock (_threadLock)
				{
					// If we ever reach the limit wait one ms
					if (_index >= GroupFactor)
					{
						Thread.Sleep(1);
						// DateTime isn't as percise as one ms 
						// https://stackoverflow.com/a/3920090/10273138
						//_index = 0;
					}

					long currentMs = CurrentTime();

					if (currentMs > _lastTime)
					{
						_lastTime = currentMs;
						// start counting again for the next millisecond
						_index = 0;
					}
					return (currentMs * GroupFactor) + ((_index++ + offset) % GroupFactor);
				}
			}
		}

		private static long CurrentTime()
		{
			return (DateTime.UtcNow.Ticks - _startEpoch) / GroupFactor;
		}


		static ThreadSaveIdGenerator()
		{
			_lastTime = CurrentTime();
            offset = Random.Shared.Next(0, GroupFactor);
		}
	}
}