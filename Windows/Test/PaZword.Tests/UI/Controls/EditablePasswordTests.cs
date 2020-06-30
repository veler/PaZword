using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI.Controls;
using System.Threading.Tasks;

namespace PaZword.Tests.UI.Controls
{
    [TestClass]
    public class EditablePasswordTests
    {
        [TestMethod]
        public async Task EditablePasswordPasswordStrengthDetectionTest()
        {
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                var control = new EditablePassword();
                control.IsEditing = true;

                control.TextEditing = "".ToSecureString();
                Assert.AreEqual(0, control.Strength);

                control.TextEditing = "aaaaaaaaaaaaaaa".ToSecureString();
                Assert.AreEqual(10, control.Strength);

                control.TextEditing = "aA1".ToSecureString();
                Assert.AreEqual(10, control.Strength);

                control.TextEditing = "password".ToSecureString();
                Assert.AreEqual(10, control.Strength);

                control.TextEditing = "abcABC".ToSecureString();
                Assert.AreEqual(10, control.Strength);

                control.TextEditing = "ABCabc123!@#".ToSecureString();
                Assert.AreEqual(25, control.Strength);

                control.TextEditing = "YHq/k".ToSecureString();
                Assert.AreEqual(25, control.Strength);

                control.TextEditing = "YHq/kk".ToSecureString();
                Assert.AreEqual(50, control.Strength);

                control.TextEditing = "YHq/kkl".ToSecureString();
                Assert.AreEqual(75, control.Strength);

                control.TextEditing = "YHq/kkL".ToSecureString();
                Assert.AreEqual(75, control.Strength);

                control.TextEditing = "YHq/kkL=".ToSecureString();
                Assert.AreEqual(100, control.Strength);

                control.TextEditing = "YHq/kkL0".ToSecureString();
                Assert.AreEqual(100, control.Strength);

                control.TextEditing = "YHq/kkL0#".ToSecureString();
                Assert.AreEqual(100, control.Strength);

                control.TextEditing = "YHq/kkL0#d".ToSecureString();
                Assert.AreEqual(100, control.Strength);
            }).ConfigureAwait(false);
        }
    }
}
