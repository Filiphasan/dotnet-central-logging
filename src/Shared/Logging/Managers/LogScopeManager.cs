using System.Collections.Concurrent;
using Shared.Logging.Helpers;

namespace Shared.Logging.Managers;

public interface ILogScopeManager
{
    IDisposable PushState(object state);
    Dictionary<string, string?> GetScopeProperties(Dictionary<string, string?>? properties = null);
}

public sealed class LogScopeManager : ILogScopeManager
{
    private static readonly AsyncLocal<LogScope?> CurrentScope = new();

    public IDisposable PushState(object state)
    {
        var parent = CurrentScope.Value;
        var scope = LogScope.Rent(parent, state);
        return scope;
    }

    public Dictionary<string, string?> GetScopeProperties(Dictionary<string, string?>? properties = null)
    {
        properties ??= new Dictionary<string, string?>();
        var scope = CurrentScope.Value;

        while (scope is not null)
        {
            LoggerHelper.ExtractProperties(scope.State, properties);
            scope = scope.Parent;
        }

        return properties;
    }

    private sealed class LogScope : IDisposable
    {
        private static readonly ConcurrentStack<LogScope> Pool = new();
        private const int MaxPoolSize = 100;

        public LogScope? Parent { get; private set; }
        public object? State { get; private set; }
        private bool _isDisposed;

        private LogScope() { }

        public static LogScope Rent(LogScope? parent, object? state)
        {
            if (Pool.TryPop(out var scope))
            {
                scope.Initialize(parent, state);
                return scope;
            }

            return new LogScope { Parent = parent, State = state };
        }

        private void Initialize(LogScope? parent, object? state)
        {
            Parent = parent;
            State = state;
            _isDisposed = false;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (Pool.Count >= MaxPoolSize)
            {
                return;
            }

            Parent = null;
            State = null;
            Pool.Push(this);
        }
    }
}