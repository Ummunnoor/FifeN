using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace Persistence.Repositories
{
    public class GenericRepository<TEntity> : IGeneric<TEntity> where TEntity : class
    {
        private readonly FifeNDbContext _context;
        private readonly DbSet<TEntity> _dbSet;
        public GenericRepository(FifeNDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }
        public async Task<TEntity> AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<int> DeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
                throw new ItemNotFoundException($"Item with {id} not found");
            _dbSet.Remove(entity);
            return await _context.SaveChangesAsync();
        }


        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<TEntity> GetByIdAsync(Guid id)
        {

            var entity = await _dbSet.FindAsync(id);
            if (entity is not null)
            {
                return entity;
            }

            return null!;
        }

        public IQueryable<TEntity> Query()
        {
            return _dbSet;
        }

        public async Task<TEntity> UpdateAsync(TEntity entity)
        {
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}

