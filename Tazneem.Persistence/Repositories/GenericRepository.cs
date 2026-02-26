using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Persistence.Data.DbContexts;

namespace Tanzeem.Persistence.Repositories {
    public class GenericRepository<Entity>(TanzeemDbContext _context) : IGenericRepository<Entity> where Entity : class {

        public async Task<Entity?> GetByIdAsync(int id) {

            return await _context.Set<Entity>().FindAsync(id);
        }

        public async Task<IEnumerable<Entity>> GetAllAsync() {

            var entities = _context.Set<Entity>().ToListAsync();
            return await entities ?? throw new Exception("Entities are null");

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
