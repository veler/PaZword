using PaZword.Api.Models;
using PaZword.Core;

namespace PaZword.Models
{
    internal sealed class CategoryPageNavigationParameter
    {
        internal Category Category { get; }

        internal bool NavigatedProgrammatically { get; }

        internal bool ShouldClearSelection { get; }

        public CategoryPageNavigationParameter(Category category, bool navigatedProgrammatically, bool shouldClearSelection)
        {
            Category = Arguments.NotNull(category, nameof(category));
            NavigatedProgrammatically = navigatedProgrammatically;
            ShouldClearSelection = shouldClearSelection;
        }
    }
}
