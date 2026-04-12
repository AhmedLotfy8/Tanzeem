using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Domain.Contracts
{
    public interface IGenericRepository<Entity> where Entity : class
    {

        Task<IEnumerable<Entity>> GetAllAsync(params Expression<Func<Entity, object>>[] includes);

        Task<Entity?> GetByIdAsync(int id);

        Task AddAsync(Entity entity);

        void UpdateAsync(Entity entity);

        void DeleteAsync(Entity entity);

        #region new methods
        IQueryable<Entity> GetAllAsIQueryable();
        IQueryable<Entity> GetByIdAsQueryable(int id);
        #endregion

    }
}
