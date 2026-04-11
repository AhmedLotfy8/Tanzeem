using System.Linq.Expressions;

namespace Tanzeem.Domain.Contracts {
    public interface IGenericRepository<Entity> where Entity : class {

        Task<IEnumerable<Entity>> GetAllAsync(params Expression<Func<Entity, object>>[] includes);

        Task<Entity?> GetByIdAsync(int id);

        Task AddAsync(Entity entity);
        
        void UpdateAsync(Entity entity);

        void DeleteAsync(Entity entity);



    }
}
