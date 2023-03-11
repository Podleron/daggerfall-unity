using System;
using System.Threading;
using System.Threading.Tasks;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public class Debouncer
    {
        private CancellationTokenSource _cancelTokenSource = null;

        public async Task Debounce(Func<Task> method, int milliseconds = 300)
        {
            _cancelTokenSource?.Cancel();
            _cancelTokenSource?.Dispose();

            _cancelTokenSource = new CancellationTokenSource();

            await Task.Delay(milliseconds, _cancelTokenSource.Token);

            await method();
        }
    }
}