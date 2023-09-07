using Pursuit.Model;
using System.Linq.Expressions;

namespace Pursuit.Context.AD
{
    public interface IDeltaRepository<TDeltaDoc>
        where TDeltaDoc : IDeltaDoc
    {
        IQueryable<TDeltaDoc> AsQueryable();

        IEnumerable<TDeltaDoc> FilterBy(
            Expression<Func<TDeltaDoc, bool>> filterExpression);

        IEnumerable<TDeltaDoc> NameFilter(string filterExpression);

        IEnumerable<TDeltaDoc> PhoneFilter(string filterExpression);

        IEnumerable<TDeltaDoc> MultiFilter(ICollection<gFilterRequest> filters);

        IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TDeltaDoc, bool>> filterExpression,
            Expression<Func<TDeltaDoc, TProjected>> projectionExpression);

        TDeltaDoc FindOne(Expression<Func<TDeltaDoc, bool>> filterExpression);

        Task<TDeltaDoc> FindOneAsync(Expression<Func<TDeltaDoc, bool>> filterExpression);
        Task RemoveManyAsync(Expression<Func<TDeltaDoc, bool>> filterExpression);

        //Find the user data by using ID

        TDeltaDoc FindById(string id);

        Task<TDeltaDoc> FindByIdAsync(string id);

        //Insert user whole details

        void InsertMany(ICollection<TDeltaDoc> documents);

        Task InsertManyAsync(ICollection<TDeltaDoc> documents);

        Task UpsertManyAsync(ICollection<TDeltaDoc> documents);
        void InsertOne(TDeltaDoc document);

        Task InsertOneAsync(TDeltaDoc document);
        void ReplaceOne(string Id, DeltaModel delta);

        Task ReplaceOneAsync(string Id, DeltaModel delta);
        void UpdateFlag(string Id, Boolean flag);

        Task UpdateFlagAsync(string Id, Boolean flag);

    }
}
