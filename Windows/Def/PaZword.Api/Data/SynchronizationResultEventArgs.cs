using System;

namespace PaZword.Api.Data
{
    public sealed class SynchronizationResultEventArgs : EventArgs
    {
        public bool Succeeded { get; }

        public bool RequiresReloadLocalData { get; }

        public SynchronizationResultEventArgs(bool succeeded, bool requiresReloadLocalData)
        {
            Succeeded = succeeded;
            RequiresReloadLocalData = requiresReloadLocalData;
        }
    }
}
