using System;
using System.Collections.Concurrent;

public class MainThreadRunner : SingletonObject<MainThreadRunner>
{
    private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

    public new static MainThreadRunner Instance { get; private set; }

    void Update()
    {
        lock (_executionQueue)
        {
            if (!_executionQueue.IsEmpty)
            {
                while (_executionQueue.TryDequeue(out Action action))
                {
                    action?.Invoke();
                }
            }
        }
    }
    void OnDestroy()
    {
        lock (_executionQueue)
        {
            _executionQueue.Clear();
        }
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}
