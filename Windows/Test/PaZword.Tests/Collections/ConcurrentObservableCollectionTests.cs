using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api.Collections;
using PaZword.Core.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Tests.Collections
{
    [TestClass]
    public class ConcurrentObservableCollectionTests
    {
        private int _collectionChangedCount;

        [TestMethod]
        public async Task WriteInConcurrentObservableCollectionUIThread()
        {
            using (var collection = new ConcurrentObservableCollection<int>())
            {
                _collectionChangedCount = 0;

                collection.CollectionChanged += Collection_CollectionChanged;

                await TaskHelper.RunOnUIThreadAsync(() =>
                {
                    for (int i = 1; i < 10000; i++)
                    {
                        for (int j = 1; j < 10; j++)
                        {
                            collection.Add(i * j);
                        }

                        collection.RemoveAt(collection.Count - 1);
                        collection.Remove(i);
                        collection.Move(0, 6);
                        collection.Insert(0, i * i);
                    }
                }).ConfigureAwait(false);

                Assert.AreEqual(79992, collection.Count);
                Assert.AreEqual(129987, _collectionChangedCount);
            }
        }

        [TestMethod]
        public async Task WriteInConcurrentObservableCollectionMultiThread()
        {
            using (var collection = new ConcurrentObservableCollection<string>())
            using (var resetEvent = new ManualResetEvent(false))
            {
                _collectionChangedCount = 0;

                collection.CollectionChanged += Collection_CollectionChanged;

                var tasks = new List<Task>();

                for (int i = 0; i < 100; i++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        resetEvent.WaitOne();

                        for (int j = 0; j < 10000; j++)
                        {
                            collection.Add(i.ToString(CultureInfo.InvariantCulture) + " - " + j.ToString(CultureInfo.InvariantCulture));
                        }
                    }));
                }

                resetEvent.Set();

                await Task.WhenAll(tasks).ConfigureAwait(false);

                int count = collection.Count;
                Assert.AreEqual(1000000, count);
                Assert.AreEqual(1000000, _collectionChangedCount);
            }
        }

        private void Collection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _collectionChangedCount++;
        }
    }
}
