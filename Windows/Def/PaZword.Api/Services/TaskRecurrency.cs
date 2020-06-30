namespace PaZword.Api.Services
{
    public enum TaskRecurrency
    {
        /// <summary>
        /// Defines that the recurrent task should be triggered only manually.
        /// </summary>
        Manual,

        /// <summary>
        /// The recurrent task will be run every 1 minute.
        /// </summary>
        OneMinute,

        /// <summary>
        /// The recurrent task will be run every 5 minute.
        /// </summary>
        FiveMinutes,

        /// <summary>
        /// The recurrent task will be run every 10 minute.
        /// </summary>
        TenMinutes,

        /// <summary>
        /// The recurrent task will be run every 1 hour.
        /// </summary>
        OneHour,

        /// <summary>
        /// The recurrent task will be run every 1 day.
        /// </summary>
        OneDay
    }
}
