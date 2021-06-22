using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services
{
    public interface IProcessManager
    {
        ICollection<ProcessInfo> Processes { get; }

        void AddProcess(string connectionId, int processId);
        bool TryGetProcess(string connectionId, [NotNullWhen(true)] out ProcessInfo? processInfo);
        bool RemoveProcess(string connectionId);
    }

    public class ProcessManager : IProcessManager
    {
        private readonly ConcurrentDictionary<string, ProcessInfo> _connectedProcesses = new();
        private readonly ILogger<ProcessManager> _logger;

        public ProcessManager(ILogger<ProcessManager> logger)
        {
            _logger = logger;
        }

        public ICollection<ProcessInfo> Processes => _connectedProcesses.Values;

        public void AddProcess(string connectionId, int processId)
        {
            _connectedProcesses.GetOrAdd(connectionId, s => new ProcessInfo(s, processId));
            _logger.LogDebug($"Added process '{processId}' with connection id '{connectionId}'");
        }

        public bool TryGetProcess(string connectionId, [NotNullWhen(true)] out ProcessInfo? processInfo)
        {
            return _connectedProcesses.TryGetValue(connectionId, out processInfo);
        }

        public bool RemoveProcess(string connectionId)
        {
            if (_connectedProcesses.TryRemove(connectionId, out var processInfo))
            {
                _logger.LogDebug($"Removed process '{processInfo.ProcessId}' with connection id '{connectionId}'");
                return true;
            }

            return false;
        }
    }

    public class ProcessInfo
    {
        private Process? _nativeProcess;

        public ProcessInfo(string connectionId, int processId)
        {
            ConnectionId = connectionId;
            ProcessId = processId;
        }

        public string ConnectionId { get; }

        public int ProcessId { get; }

        public Process? NativeProcess => _nativeProcess ??= Process.GetProcessById(ProcessId);
    }
}