using PaZword.Api.Security;
using System.Threading.Tasks;

namespace PaZword.Tests.Mocks
{
    class MockIWindowsHelloAuthProvider : IWindowsHelloAuthProvider
    {
        internal bool Authenticated { get; set; }

        internal bool IsWindowsHelloEnabled { get; set; }

        public Task<bool> AuthenticateAsync()
        {
            return Task.FromResult(Authenticated);
        }

        public Task<bool> IsWindowsHelloEnabledAsync()
        {
            return Task.FromResult(IsWindowsHelloEnabled);
        }
    }
}
