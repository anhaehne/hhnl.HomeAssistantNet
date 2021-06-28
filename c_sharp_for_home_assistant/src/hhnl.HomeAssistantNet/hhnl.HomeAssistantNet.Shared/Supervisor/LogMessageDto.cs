using System;

namespace hhnl.HomeAssistantNet.Shared.Supervisor
{
    public class LogMessageDto
    {
        public LogMessageDto(Guid runId, string message, int logLevel, int notificationId)
        {
            RunId = runId;
            Message = message;
            LogLevel = logLevel;
            NotificationId = notificationId;
        }

        public Guid RunId { get; }

        public string Message { get; }

        public int LogLevel { get; }

        public int NotificationId { get; }
    }
}
