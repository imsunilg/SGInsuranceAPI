using Microsoft.EntityFrameworkCore;
using SGInsurance.Application.Interfaces;
using SGInsurance.Infrastructure.Persistence;

namespace SGInsurance.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly SGInsuranceDbContext Context;
    protected readonly DbSet<T> Set;

    public Repository(SGInsuranceDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(params object[] keyValues) => await Set.FindAsync(keyValues);

    public IQueryable<T> Query() => Set.AsQueryable();

    public async Task AddAsync(T entity) => await Set.AddAsync(entity);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);

    public Task<int> SaveChangesAsync() => Context.SaveChangesAsync();
}
