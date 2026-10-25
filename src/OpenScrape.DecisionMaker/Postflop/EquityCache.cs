using System.Collections.Concurrent;

namespace OpenScrape.DecisionMaker.Postflop;

/// <summary>
/// Caché de equity acotada por capacidad. Al llenarse desaloja las entradas más
/// viejas en orden de inserción (FIFO), en vez de vaciar todo: un Clear() total
/// tira el trabajo de todas las manos anteriores y obliga a repetir Monte Carlo
/// en cada mano nueva, que es justo lo que la caché evita.
/// </summary>
internal sealed class EquityCache
{
    private readonly ConcurrentDictionary<string, double> _values = new();
    private readonly ConcurrentQueue<string> _insertionOrder = new();
    private readonly int _capacity;

    public EquityCache(int capacity = 2048)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "La capacidad tiene que ser positiva.");

        _capacity = capacity;
    }

    public int Capacity => _capacity;

    public int Count => _values.Count;

    public bool TryGet(string key, out double value) => _values.TryGetValue(key, out value);

    public void Set(string key, double value)
    {
        if (!_values.ContainsKey(key))
        {
            _values[key] = value;
            _insertionOrder.Enqueue(key);

            // El conteo puede pasarse un poco entre hilos; el lazo lo vuelve a acotar.
            while (_values.Count > _capacity)
            {
                if (!_insertionOrder.TryDequeue(out var oldest))
                    break;

                _values.TryRemove(oldest, out _);
            }

            return;
        }

        _values[key] = value;
    }
}
