using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Services.Interfaces;

namespace Dashboard.Services;

public class CrudService<T> : ICrudService<T> where T : class
{
    private readonly ApplicationDbContext _context;
    private readonly DbSetSelector<T> _selector;

    public CrudService(ApplicationDbContext context, DbSetSelector<T> selector)
    {
        _context = context;
        _selector = selector;
    }

    private DbSet<T> DbSet => _selector(_context);

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.AsNoTracking().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await DbSet.FindAsync(id);
    }

    public async Task<T?> GetByIdAsync(long id)
    {
        return await DbSet.FindAsync(id);
    }

    public async Task<bool> CreateAsync(T entity)
    {
        try
        {
            DbSet.Add(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync(T entity)
    {
        try
        {
            DbSet.Update(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var entity = await DbSet.FindAsync(id);
            if (entity == null) return false;
            DbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(long id)
    {
        try
        {
            var entity = await DbSet.FindAsync(id);
            if (entity == null) return false;
            DbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string Message)> DeleteWithValidationAsync(int id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity == null)
            return (false, "Record not found.");

        try
        {
            DbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return (true, "Record deleted successfully.");
        }
        catch (DbUpdateException)
        {
            return (false, "Cannot delete this record because it is being referenced by other data.");
        }
    }

    public async Task<(bool Success, string Message)> DeleteWithValidationAsync(long id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity == null)
            return (false, "Record not found.");

        try
        {
            DbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return (true, "Record deleted successfully.");
        }
        catch (DbUpdateException)
        {
            return (false, "Cannot delete this record because it is being referenced by other data.");
        }
    }

    public DbSetSelector<T> GetDbSet()
    {
        return _selector;
    }
}
