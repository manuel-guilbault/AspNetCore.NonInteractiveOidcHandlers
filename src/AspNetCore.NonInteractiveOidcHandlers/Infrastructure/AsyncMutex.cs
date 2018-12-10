using System;
using System.Threading.Tasks;

namespace AspNetCore.NonInteractiveOidcHandlers.Infrastructure
{
	internal sealed class AsyncMutex<T>
	{
		private Task<T> _task;
		private readonly object _taskGuard = new object();

		public Task<T> AcquireAsync(Func<Task<T>> factory)
		{
			lock (_taskGuard)
			{
				return _task ?? (_task = factory());
			}
		}

		public void Release()
		{
			lock (_taskGuard)
			{
				_task = null;
			}
		}
	}
}
