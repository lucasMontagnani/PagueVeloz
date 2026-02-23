using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Domain.Interfaces.Repositories.Generics
{
    public interface ICommandRepository<TEntity>
    {
        Task<TEntity> CreateTEntity(TEntity entity);
        void UpdateTEntity(TEntity entity);
        void UpdateOnly(TEntity entity, params Expression<Func<TEntity, object>>[] updatedProperties);
        void UpdateRangeOnly(IEnumerable<TEntity> entities, params Expression<Func<TEntity, object>>[] updatedProperties);
        void DeleteTEntity(TEntity entity);
        Task DeleteTEntityById(int id);
        Task SaveChangesAsync();
    }
}
