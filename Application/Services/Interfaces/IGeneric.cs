using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Application.Services.Interfaces
{
    public interface IGeneric<TEntity> where TEntity : class
    {
        IQueryable<TEntity> Query();
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity> GetByIdAsync(Guid id);
        Task<TEntity> AddAsync(TEntity entity);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task<int> DeleteAsync(Guid id);
    }
}
