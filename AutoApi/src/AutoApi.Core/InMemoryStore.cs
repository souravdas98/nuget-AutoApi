using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoApi.Core.Abstractions;

namespace AutoApi.Core.Internal;

/// <summary>
/// Zero-config in-memory store. Used by default when no custom IAutoApiStore is registered.
/// Looks for a property named "Id" (int) on T for keying.
/// Not suitable for production — swap in an EF Core or Dapper store for persistence.
/// </summary>
internal class InMemoryStore<T> : IAutoApiStore<T>
    where T : class
{
    private readonly ConcurrentDictionary<int, T> _store = new();
    private int _nextId = 1;

    private static PropertyInfo IdProperty =>
        typeof(T).GetProperty("Id", typeof(int))
        ?? throw new InvalidOperationException(
            $"AutoApi InMemoryStore requires '{typeof(T).Name}' to have a public int Id property."
        );

    private int GetId(T item) => (int)IdProperty.GetValue(item)!;

    private void SetId(T item, int id) => IdProperty.SetValue(item, id);

    public Task<IEnumerable<T>> GetAllAsync() =>
        Task.FromResult<IEnumerable<T>>(_store.Values.ToList());

    public Task<T?> GetByIdAsync(int id) =>
        Task.FromResult(_store.TryGetValue(id, out var item) ? item : null);

    public Task<T> CreateAsync(T item)
    {
        var id = _nextId++;
        SetId(item, id);
        _store[id] = item;
        return Task.FromResult(item);
    }

    public Task<T> UpdateAsync(int id, T item)
    {
        if (!_store.ContainsKey(id))
            throw new KeyNotFoundException($"{typeof(T).Name} with id {id} not found.");
        SetId(item, id);
        _store[id] = item;
        return Task.FromResult(item);
    }

    public Task DeleteAsync(int id)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
