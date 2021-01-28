using System;

namespace DL444.Ucqu.Models
{
    public struct UserInitializeStatus : ICosmosResource
    {
        public UserInitializeStatus(string id, bool completed)
        {
            TaskId = id;
            Completed = completed;
            LastUpdateTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public string Id() => $"UserInitStatus-{TaskId}";
        public string PartitionKey() => "UserInitStatus";

        public string TaskId { get; set; }
        public bool Completed { get; set; }
        public long LastUpdateTimestamp { get; set; }
    }
}
