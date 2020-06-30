using System.Composition.Hosting;

namespace PaZword.Api
{
    public interface IApp
    {
        CompositionHost ExportProvider { get; }

        void ResetMef();

        void UpdateColorTheme();
    }
}
