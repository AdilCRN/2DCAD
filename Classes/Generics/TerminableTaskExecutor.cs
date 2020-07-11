using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkGeometriesLib.Classes.Generics
{
    public class TerminableTaskExecutor
    {
        private Thread _thread;
        private CancellationTokenSource _cts;

        public bool IsRunning { get; set; } = false;

        public TerminableTaskExecutor()
        {
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Cancels previous running action and starts this.
        /// </summary>
        /// <param name="actionIn">A terminable action</param>
        public Task<Exception> CancelCurrentAndRun(Func<CancellationToken, Task> actionIn)
        {
            Cancel();

            // wait for running task
            while (IsRunning) { }

            IsRunning = true;
            _thread = Thread.CurrentThread;
            _cts = new CancellationTokenSource();

            return Task.Run(async () =>
            {
                try
                {
                    await actionIn.Invoke(_cts.Token);
                }
                catch (Exception exp)
                {
                    return exp;
                }
                finally
                {
                    IsRunning = false;
                }

                return null;
            }, _cts.Token);
        }

        /// <summary>
        /// Cancels previous running action and starts this.
        /// </summary>
        /// <param name="actionIn">A terminable action</param>
        public Task<Exception> CancelCurrentAndRun(Action<CancellationToken> actionIn)
        {
            return Task.Run(() =>
            {
                try
                {
                    Cancel();

                    // wait for running task
                    while (IsRunning) { }

                    IsRunning = true;
                    _thread = Thread.CurrentThread;
                    _cts = new CancellationTokenSource();

                    actionIn.Invoke(_cts.Token);
                }
                catch (Exception exp)
                {
                    return exp;
                }
                finally
                {
                    IsRunning = false;
                }

                return null;
            }, _cts.Token);
        }

        /// <summary>
        /// Aborts previous running action and starts this.
        /// </summary>
        /// <param name="actionIn">A terminable action</param>
        public Task<Exception> AbortCurrentAndRun(Action<CancellationToken> actionIn)
        {
            return Task.Run(() =>
            {
                try
                {
                    Abort();

                    // wait for running task
                    while (IsRunning) { }

                    IsRunning = true;
                    _thread = Thread.CurrentThread;
                    _cts = new CancellationTokenSource();

                    actionIn.Invoke(_cts.Token);
                }
                catch (Exception exp)
                {
                    return exp;
                }
                finally
                {
                    IsRunning = false;
                }

                return null;
            }, _cts.Token);
        }

        public async Task<Exception> RunIfNotRunning(Action<CancellationToken> actionIn)
        {
            if (IsRunning)
                return null;

            return await CancelCurrentAndRun(actionIn);
        }

        public bool Cancel()
        {
            if (IsRunning)
                _cts.Cancel();

            return IsRunning;
        }

        public bool Abort()
        {
            if (!Cancel())
            {
                _thread?.Abort();
                IsRunning = false;
            }

            return IsRunning;
        }
    }
}
