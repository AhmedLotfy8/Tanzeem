using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Tanzeem.Domain.Contracts;
using Tanzeem.Persistence.Data.DbContexts;

namespace Tanzeem.Persistence.Repositories {
    public class GenericRepository<Entity>(TanzeemDbContext _context) : IGenericRepository<Entity> where Entity : class {

        public async Task<Entity?> GetByIdAsync(int id) {

            return await _context.Set<Entity>().FindAsync(id);
        }

        public async Task<IEnumerable<Entity>> GetAllAsync(params Expression<Func<Entity, object>>[] includes) {

            // return await _context.Set<Entity>().ToListAsync();
            IQueryable<Entity> query = _context.Set<Entity>();
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return await query.ToListAsync();
///TODO : better to make it return IQuerable due to performance then convert to .ToListAsync at service logic..
        }

        public async Task AddAsync(Entity entity) {
            await _context.AddAsync(entity);
        }

        public void UpdateAsync(Entity entity) {
            _context.Update(entity);
        }

        public void DeleteAsync(Entity entity) {
            _context.Remove(entity);
        }


    }
}
