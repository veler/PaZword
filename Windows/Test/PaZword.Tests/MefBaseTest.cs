using PaZword.Core;
using PaZword.Localization;
using System;
using System.Composition.Hosting;
using System.Globalization;

namespace PaZword.Tests
{
    public abstract class MefBaseTest : IDisposable
    {
        private readonly MefHost _mefHost = new MefHost();

        private bool _isDisposed;

        protected CompositionHost ExportProvider => _mefHost.ExportProvider;

        public MefBaseTest()
        {
            // Do all the tests in English.
            LanguageManager.Instance.SetCurrentCulture(new CultureInfo("en"));

            _mefHost.InitializeMef();
        }

        ~MefBaseTest()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _mefHost.Dispose();
            }

            _isDisposed = true;
        }
    }
}
