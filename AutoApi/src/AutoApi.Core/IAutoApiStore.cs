using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoApi.Core.Abstractions;

/// <summary>
/// Storage contract that AutoApi uses to fulfill CRUD operations.
/// Implement this interface with EF Core, Dapper, or any data source.
/// Register via: builder.Services.AddAutoApi(o => o.UseStore&lt;T, TStore&gt;())
/// </summary>
public interface IAutoApiStore<T>
    where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<T> CreateAsync(T item);
    Task<T> UpdateAsync(int id, T item);
    Task DeleteAsync(int id);
}
