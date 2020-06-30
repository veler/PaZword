using System;

namespace PaZword.Api.Services
{
    public sealed class RecurrentTaskEventArgs : EventArgs
    {
        public string TaskName { get; }

        public object Result { get; }

        public RecurrentTaskEventArgs(string taskName, object result)
        {
            TaskName = taskName;
            Result = result;
        }
    }
}
