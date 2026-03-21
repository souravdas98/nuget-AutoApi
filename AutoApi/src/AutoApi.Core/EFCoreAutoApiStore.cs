using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoApi.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AutoApi.EFCore;

/// <summary>
/// EF Core-backed IAutoApiStore&lt;T&gt;.
/// T must have a public int Id property.
///
/// Usage in Program.cs:
///   builder.Services.AddAutoApi(o =>
///       o.UseStore&lt;Product, EFCoreAutoApiStore&lt;Product, AppDbContext&gt;&gt;());
/// </summary>
public class EFCoreAutoApiStore<T, TContext> : IAutoApiStore<T>
    where T : class
    where TContext : DbContext
{
    private readonly TContext _db;
    private readonly DbSet<T> _set;

    private static PropertyInfo IdProp =>
        typeof(T).GetProperty("Id", typeof(int))
        ?? throw new InvalidOperationException(
            $"EFCoreAutoApiStore requires '{typeof(T).Name}' to have a public int Id property."
        );

    public EFCoreAutoApiStore(TContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync() => await _set.ToListAsync();

    public async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);

    public async Task<T> CreateAsync(T item)
    {
        _set.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<T> UpdateAsync(int id, T item)
    {
        var existing =
            await _set.FindAsync(id)
            ?? throw new KeyNotFoundException($"{typeof(T).Name} with id {id} not found.");

        _db.Entry(existing).CurrentValues.SetValues(item);
        IdProp.SetValue(existing, id); // ensure id is not overwritten
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _set.FindAsync(id);
        if (existing is not null)
        {
            _set.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }
}
