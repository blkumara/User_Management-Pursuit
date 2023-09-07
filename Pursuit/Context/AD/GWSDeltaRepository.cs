using MongoDB.Driver;
using Pursuit.Model;
using System.Linq.Expressions;

namespace Pursuit.Context.AD
{
    public class GWSDeltaRepository<TDeltaDoc> : IDeltaRepository<TDeltaDoc>
    where TDeltaDoc : IDeltaDoc
    {
        private readonly IMongoCollection<TDeltaDoc> _gwsDeltaCollection;

        public GWSDeltaRepository(IADDBSettings settings)
        {
            var database = new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
            _gwsDeltaCollection = database.GetCollection<TDeltaDoc>("GWS_Delta");
        }
        public IQueryable<TDeltaDoc> AsQueryable()
        {
            return _gwsDeltaCollection.AsQueryable();
        }
        //Insert Data
        public virtual void InsertOne(TDeltaDoc document)
        {
            _gwsDeltaCollection.InsertOne(document);
        }

        public virtual Task InsertOneAsync(TDeltaDoc document)
        {
            return Task.Run(() => _gwsDeltaCollection.InsertOneAsync(document));
        }

        public void InsertMany(ICollection<TDeltaDoc> documents)
        {
            _gwsDeltaCollection.InsertMany(documents);
        }


        public virtual async Task InsertManyAsync(ICollection<TDeltaDoc> documents)
        {
            await _gwsDeltaCollection.InsertManyAsync(documents);
        }
        public IEnumerable<TDeltaDoc> FilterBy(Expression<Func<TDeltaDoc, bool>> filterExpression)
        {
            return _gwsDeltaCollection.Find(filterExpression).ToEnumerable();
        }

        public IEnumerable<TProjected> FilterBy<TProjected>(Expression<Func<TDeltaDoc, bool>> filterExpression, Expression<Func<TDeltaDoc, TProjected>> projectionExpression)
        {
            throw new NotImplementedException();
        }

        public TDeltaDoc FindById(string id)
        {
            throw new NotImplementedException();
        }

        public Task<TDeltaDoc> FindByIdAsync(string id)
        {
            throw new NotImplementedException();
        }

        public TDeltaDoc FindOne(Expression<Func<TDeltaDoc, bool>> filterExpression)
        {
            throw new NotImplementedException();
        }

        public Task<TDeltaDoc> FindOneAsync(Expression<Func<TDeltaDoc, bool>> filterExpression)
        {
            throw new NotImplementedException();
        }

       
        public IEnumerable<TDeltaDoc> MultiFilter(ICollection<gFilterRequest> filters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TDeltaDoc> NameFilter(string filterExpression)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TDeltaDoc> PhoneFilter(string filterExpression)
        {
            throw new NotImplementedException();
        }

        public Task RemoveManyAsync(Expression<Func<TDeltaDoc, bool>> filterExpression)
        {
            throw new NotImplementedException();
        }

        public Task UpsertManyAsync(ICollection<TDeltaDoc> documents)
        {
            throw new NotImplementedException();
        }

        public void ReplaceOne(string Id, DeltaModel delta)
        {
            throw new NotImplementedException();
        }

        public Task ReplaceOneAsync(string Id, DeltaModel delta)
        {
            throw new NotImplementedException();
        }

        public void UpdateFlag(string Id, Boolean flag)
        {
            throw new NotImplementedException();
        }

        public Task UpdateFlagAsync(string Id, Boolean flag)
        {
            throw new NotImplementedException();
        }
    }
}
