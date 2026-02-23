using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Domain.Interfaces.Repositories.Generics
{
    public interface IQueryRepository<TEntity> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(int id);

        Task<IEnumerable<TEntity>> GetAllAsync();

        Task<IEnumerable<TEntity>> GetAllWithFilterAsync(
            Expression<Func<TEntity, bool>> predicate,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null);

        Task<TEntity?> GetFirstOrDefaultWithFilterAsync(
            Expression<Func<TEntity, bool>> predicate);

        Task<IEnumerable<TEntity>> GetAllIncludingAsync(
            params Expression<Func<TEntity, object>>[] includes);
    }
}
