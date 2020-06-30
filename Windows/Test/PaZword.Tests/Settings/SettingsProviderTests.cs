using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api.Settings;

namespace PaZword.Tests.Settings
{
    [TestClass]
    public class SettingsProviderTests : MefBaseTest
    {
        [TestMethod]
        public void SettingsTest()
        {
            ISettingsProvider settingsProvider = ExportProvider.GetExport<ISettingsProvider>();

            var setting = new SettingDefinition<bool>("dummySetting", isRoaming: false, defaultValue: true);

            // Remove previous setting if exists.
            settingsProvider.ResetSetting(setting);

            Assert.IsTrue(settingsProvider.GetSetting(setting));

            settingsProvider.SetSetting(setting, false);

            Assert.IsFalse(settingsProvider.GetSetting(setting));
        }
    }
}
