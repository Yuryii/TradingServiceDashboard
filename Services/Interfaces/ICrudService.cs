using Microsoft.EntityFrameworkCore;

namespace Dashboard.Services.Interfaces;

public interface ICrudService<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByIdAsync(long id);
    Task<bool> CreateAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteAsync(long id);
    Task<(bool Success, string Message)> DeleteWithValidationAsync(int id);
    Task<(bool Success, string Message)> DeleteWithValidationAsync(long id);
}

public delegate DbSet<T> DbSetSelector<T>(Dashboard.Data.ApplicationDbContext ctx) where T : class;
