using System;
using System.Text.Json.Serialization;

namespace DL444.Ucqu.Models
{
    public struct UserInitializeStatus : ICosmosResource
    {
        public UserInitializeStatus(string id, bool completed, string? message)
        {
            TaskId = id;
            Completed = completed;
            Message = message;
            LastUpdateTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public string Id() => $"UserInitStatus-{TaskId}";
        public string PartitionKey() => "UserInitStatus";

        public string TaskId { get; set; }
        public bool Completed { get; set; }
        public string? Message { get; set; }
        public long LastUpdateTimestamp { get; set; }
    }
}
