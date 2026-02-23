using Microsoft.EntityFrameworkCore;
using PagueVeloz.Domain.Interfaces.Repositories.Generics;
using PagueVeloz.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Infrastructure.Repositories.Generics
{
    public class CommandRepository<TEntity> : ICommandRepository<TEntity> where TEntity : class
    {
        private readonly PagueVelozDbContext _context;

        public CommandRepository(PagueVelozDbContext context)
        {
            _context = context;
        }

        public async Task<TEntity> CreateTEntity(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _context.Set<TEntity>().AddAsync(entity);
            return entity;
        }

        public void UpdateTEntity(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _context.Set<TEntity>().Update(entity);
        }

        public void UpdateOnly(TEntity entity, params Expression<Func<TEntity, object>>[] updatedProperties)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var entry = _context.Entry(entity);

            if (entry.State == EntityState.Detached)
                _context.Attach(entity);

            foreach (var property in updatedProperties)
            {
                var member = property.Body as MemberExpression;

                if (member == null && property.Body is UnaryExpression unary)
                    member = unary.Operand as MemberExpression;

                var propertyName = member?.Member.Name;

                if (propertyName == null)
                    continue;

                var navigation = entry.Metadata.FindNavigation(propertyName);

                if (navigation != null)
                {
                    // É navigation
                    entry.Reference(propertyName).IsModified = true;
                }
                else
                {
                    // É coluna normal
                    entry.Property(propertyName).IsModified = true;
                }
            }
        }

        public void UpdateRangeOnly(
            IEnumerable<TEntity> entities,
            params Expression<Func<TEntity, object>>[] updatedProperties)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            if (!entityList.Any())
                return;

            foreach (var entity in entityList)
            {
                _context.Attach(entity);

                foreach (var property in updatedProperties)
                {
                    _context.Entry(entity).Property(property).IsModified = true;
                }
            }
        }
        public void DeleteTEntity(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _context.Set<TEntity>().Remove(entity);
        }

        public async Task DeleteTEntityById(int id)
        {
            var dbSet = _context.Set<TEntity>();

            var entity = await dbSet.FindAsync(id);

            if (entity == null)
                throw new KeyNotFoundException($"Entity with key '{id}' not found.");

            // Remove the entity
            dbSet.Remove(entity);

            // Save changes
            await _context.SaveChangesAsync();

        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
