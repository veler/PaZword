using System;
using System.Composition.Hosting;

namespace PaZword.Core
{
    internal sealed class MefHost : IDisposable
    {
        internal CompositionHost ExportProvider { get; private set; }

        public void Dispose()
        {
            if (ExportProvider != null)
            {
                ExportProvider.Dispose();
            }
        }

        internal void InitializeMef()
        {
            if (ExportProvider != null)
            {
                return;
            }

            var configuration = new ContainerConfiguration()
                .WithAssembly(typeof(MefHost).Assembly) // this assembly
                .WithAssembly(typeof(Constants).Assembly); // PaZword.Core
            ExportProvider = configuration.CreateContainer();
        }

        internal void Reset()
        {
            // For unit tests.
            ExportProvider?.Dispose();
            ExportProvider = null;
            InitializeMef();
        }
    }
}
