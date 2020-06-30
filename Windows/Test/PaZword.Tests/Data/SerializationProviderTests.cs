using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api.Data;
using PaZword.Api.Models;
using System;

namespace PaZword.Tests.Data
{
    [TestClass]
    public class SerializationProviderTests : MefBaseTest
    {
        [TestMethod]
        public void CloneObjectTest()
        {
            var data = new UserDataBundle();
            data.Categories.Add(new Category(Guid.NewGuid(), "Category"));

            var dataCopy = ExportProvider.GetExport<ISerializationProvider>().CloneObject(data);

            Assert.AreEqual(data.GetHashCode(), data.GetHashCode());
            Assert.AreNotEqual(data.GetHashCode(), dataCopy.GetHashCode());
        }
    }
}
