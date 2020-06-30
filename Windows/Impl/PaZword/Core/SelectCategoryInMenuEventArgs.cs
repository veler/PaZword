using PaZword.Api.Models;
using System;

namespace PaZword.Core
{
    internal sealed class SelectCategoryInMenuEventArgs : EventArgs
    {
        internal Category Category { get; }

        public SelectCategoryInMenuEventArgs(Category category)
        {
            Category = category;
        }
    }
}
