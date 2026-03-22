using System.Collections.Generic;

namespace OpenScrape.App.Services;

/// <summary>
/// Cache LRU (Least-Recently-Used) genérico, thread-safe mediante lock.
/// Cuando se supera la capacidad máxima, descarta la entrada menos usada recientemente.
/// </summary>
internal sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _map;
    private readonly LinkedList<(TKey Key, TValue Value)> _lruList;
    private readonly object _lock = new();

    public LruCache(int capacity)
    {
        _capacity = capacity;
        _map = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(capacity + 1);
        _lruList = new LinkedList<(TKey, TValue)>();
    }

    public int Count
    {
        get { lock (_lock) return _map.Count; }
    }

    /// <summary>Retorna una snapshot de los valores actuales (para disposición de recursos).</summary>
    public IReadOnlyList<TValue> Values
    {
        get
        {
            lock (_lock)
            {
                var values = new TValue[_map.Count];
                int i = 0;
                foreach (var node in _lruList)
                    values[i++] = node.Value;
                return values;
            }
        }
    }

    /// <summary>Intenta obtener un valor del cache.</summary>
    public bool TryGet(TKey key, out TValue value)
    {
        lock (_lock)
        {
            if (_map.TryGetValue(key, out var node))
            {
                // Mover al frente (más recientemente usado)
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                value = node.Value.Value;
                return true;
            }
        }
        value = default!;
        return false;
    }

    /// <summary>Agrega o actualiza un valor. Si supera la capacidad, descarta el LRU.</summary>
    public void Set(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_map.TryGetValue(key, out var existing))
            {
                _lruList.Remove(existing);
                _map.Remove(key);
            }

            var node = _lruList.AddFirst((key, value));
            _map[key] = node;

            if (_map.Count > _capacity)
                EvictLru();
        }
    }

    /// <summary>
    /// Obtiene el valor si existe; si no, lo calcula con <paramref name="factory"/> y lo almacena.
    /// </summary>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        if (TryGet(key, out var value))
            return value;

        var newValue = factory(key);
        Set(key, newValue);
        return newValue;
    }

    /// <summary>Vacía el cache por completo.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _map.Clear();
            _lruList.Clear();
        }
    }

    private void EvictLru()
    {
        var oldest = _lruList.Last;
        if (oldest is null) return;
        _lruList.RemoveLast();
        _map.Remove(oldest.Value.Key);
    }
}
