using PaZword.Api.Services;
using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Tests.Mocks
{
    class MockRecurrentTask : IRecurrentTask
    {
        public Task<bool> CanExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>("Hello there");
        }
    }
}
