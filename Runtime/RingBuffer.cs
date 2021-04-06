using System;

public class GrowingRingBuffer<T>
{
    public static GrowingRingBuffer<T> Repeat(int count, T item)
    {
        var buffer = new GrowingRingBuffer<T>(count);
        for (int i = 0; i < count; ++i)
        {
            buffer._buffer[i] = item;
        }
        buffer._end = count;
        return buffer;
    }

    private static readonly T[] _emptyArray = new T[0];
    private const int _initialCapacity = 4;

    private T[] _buffer;
    private int _begin;
    private int _end;

    public int Capacity => _buffer.Length;

    public bool IsEmpty => _end == 0;

    public int Count
    {
        get
        {
            if (_end == 0) 
                return 0;
            if (_begin < _end)
                return _end - _begin;
            return _buffer.Length - _begin + _end;
        }
    }

    public GrowingRingBuffer(int capacity)
    {
        _buffer = capacity > 0 ? new T[capacity] : _emptyArray;
        _begin = _end = 0;
    }

    public T this[int index]
    {
        get
        {
            int i = (_begin + index) % _buffer.Length;
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Out of bounds of the RingBuffer");

            return _buffer[i];
        }
        set
        {
            int i = (_begin + index) % _buffer.Length;
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Out of bounds of the RingBuffer");

            _buffer[i] = value;
        }
    }

    public void Add(T item)
    {
        if (Count == Capacity)
        {
            _Grow();
        }

        _end %= _buffer.Length;
        _buffer[_end++] = item;
    }

    public T Pop()
    {
        if (IsEmpty)
            throw new ArgumentOutOfRangeException("The RingBuffer is empty.");

        var item = _buffer[_begin++];

        if (_begin == _end)
            _begin = _end = 0; // empty now
        else
            _begin %= _buffer.Length;

        return item;
    }

    public void Clear()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _begin = _end = 0;
    }

    // double capacity
    private void _Grow()
    {
        if (_end == 0)
        {
            _buffer = new T[_initialCapacity];
            return;
        }

        var len = _buffer.Length;
        var lastLen = len - _begin;
        var target = new T[len * 2];
        Array.Copy(_buffer, _begin, target, 0, lastLen);
        Array.Copy(_buffer, 0, target, lastLen, _begin);

        _buffer = target;
        _begin = 0;
        _end = len;
    }
}