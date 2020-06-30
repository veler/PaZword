using System.Collections.Generic;
using System.Linq;

namespace PaZword.Core.Security.PasswordStrengthEvaluator
{
    /// <summary>
    /// A single grouping from the GroupAdjacent function, includes start and end indexes for the grouping in addition to standard IGrouping bits
    /// </summary>
    /// <typeparam name="TElement">Type of grouped elements</typeparam>
    /// <typeparam name="TKey">Type of key used for grouping</typeparam>
    internal class AdjacentGrouping<TKey, TElement> : IGrouping<TKey, TElement>, IEnumerable<TElement>
    {
        /// <summary>
        /// The key value for this grouping
        /// </summary>
        public TKey Key
        {
            get;
            private set;
        }

        /// <summary>
        /// The start index in the source enumerable for this group (i.e. index of first element)
        /// </summary>
        internal int StartIndex
        {
            get;
            private set;
        }

        /// <summary>
        /// The end index in the enumerable for this group (i.e. the index of the last element)
        /// </summary>
        internal int EndIndex
        {
            get;
            private set;
        }

        private readonly IEnumerable<TElement> m_groupItems;

        internal AdjacentGrouping(TKey key, IEnumerable<TElement> groupItems, int startIndex, int endIndex)
        {
            this.Key = key;
            this.StartIndex = startIndex;
            this.EndIndex = endIndex;
            m_groupItems = groupItems;
        }

        private AdjacentGrouping() { }

        IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator()
        {
            return m_groupItems.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_groupItems.GetEnumerator();
        }
    }
}
