using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Core;
using PaZword.Core.UI.Converters;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.Tests.UI.Converters
{
    [TestClass]
    public class ConvertersTests
    {
        [TestMethod]
        public void TextWrappingToBooleanConverterTest()
        {
            var converter = new TextWrappingToBooleanConverter();
            Assert.IsFalse((bool)converter.Convert(TextWrapping.NoWrap, typeof(bool), null, null));
            Assert.IsTrue((bool)converter.Convert(TextWrapping.WrapWholeWords, typeof(bool), null, null));
        }

        [TestMethod]
        public void PageWidthToOpenPaneLengthConverterTest()
        {
            var converter = new PageWidthToOpenPaneLengthConverter();
            converter.PaneWidth = 10;

            Assert.AreEqual(19.9, converter.Convert(19.9, typeof(double), null, null));
            Assert.AreEqual(10.0, converter.Convert(20.0, typeof(double), null, null));
        }

        [TestMethod]
        public void NullToBooleanConverterTest()
        {
            var converter = new NullToBooleanConverter();
            converter.IsInverted = false;

            Assert.IsTrue((bool)converter.Convert(null, typeof(bool), null, null));
            Assert.IsFalse((bool)converter.Convert(1, typeof(bool), null, null));

            converter.IsInverted = true;

            Assert.IsFalse((bool)converter.Convert(null, typeof(bool), null, null));
            Assert.IsTrue((bool)converter.Convert(1, typeof(bool), null, null));

            converter.IsInverted = false;

            Assert.IsTrue((bool)converter.Convert(string.Empty, typeof(bool), null, null));
            Assert.IsFalse((bool)converter.Convert("foo", typeof(bool), null, null));

            converter.IsInverted = true;

            Assert.IsFalse((bool)converter.Convert(string.Empty, typeof(bool), null, null));
            Assert.IsTrue((bool)converter.Convert("foo", typeof(bool), null, null));
        }

        [TestMethod]
        public void BooleanToVisibilityConverterTest()
        {
            var converter = new BooleanToVisibilityConverter();
            converter.IsInverted = false;

            Assert.AreEqual(Visibility.Visible, converter.Convert(true, typeof(Visibility), null, null));
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(false, typeof(Visibility), null, null));

            converter.IsInverted = true;

            Assert.AreEqual(Visibility.Collapsed, converter.Convert(true, typeof(Visibility), null, null));
            Assert.AreEqual(Visibility.Visible, converter.Convert(false, typeof(Visibility), null, null));
        }

        [TestMethod]
        public void IntToVisibilityConverterTest()
        {
            var converter = new IntToVisibilityConverter();

            Assert.AreEqual(Visibility.Visible, converter.Convert(0, typeof(Visibility), "0", null));
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(0, typeof(Visibility), "-1", null));
        }

        [TestMethod]
        public void EnumToBooleanConverterTest()
        {
            var converter = new EnumToBooleanConverter();

            Assert.IsFalse((bool)converter.Convert(null, null, "foo", null));
            Assert.IsFalse((bool)converter.Convert(AppBarClosedDisplayMode.Compact, null, null, null));
            Assert.IsFalse((bool)converter.Convert("foo", null, "foo", null));

            Assert.IsTrue((bool)converter.Convert(AppBarClosedDisplayMode.Compact, null, "Compact", null));
            Assert.IsFalse((bool)converter.Convert(AppBarClosedDisplayMode.Compact, null, "Test", null));

            Assert.AreEqual(AppBarClosedDisplayMode.Compact, converter.ConvertBack(true, typeof(AppBarClosedDisplayMode), "Compact", null));
        }

        [TestMethod]
        public void EnumToVisibilityConverterTest()
        {
            var converter = new EnumToVisibilityConverter();

            Assert.AreEqual(Visibility.Collapsed, converter.Convert(null, null, "foo", null));
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(AppBarClosedDisplayMode.Compact, null, null, null));
            Assert.AreEqual(Visibility.Collapsed, converter.Convert("foo", null, "foo", null));

            Assert.AreEqual(Visibility.Visible, converter.Convert(AppBarClosedDisplayMode.Compact, null, "Compact", null));
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(AppBarClosedDisplayMode.Compact, null, "Test", null));
        }
    }
}
