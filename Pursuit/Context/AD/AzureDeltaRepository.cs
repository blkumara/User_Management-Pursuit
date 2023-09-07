using Docker.DotNet.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Pursuit.Model;
using System.Linq.Expressions;

namespace Pursuit.Context.AD
{
    public class AzureDeltaRepository<TDeltaDoc> : IDeltaRepository<TDeltaDoc>
    where TDeltaDoc : IDeltaDoc
    {
        private readonly IMongoCollection<TDeltaDoc> _azDeltaCollection;

        public AzureDeltaRepository(IADDBSettings settings)
        {
            var database = new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
            _azDeltaCollection = database.GetCollection<TDeltaDoc>("Azure_Delta");
        }
        public IQueryable<TDeltaDoc> AsQueryable()
        {
            return _azDeltaCollection.AsQueryable();
        }
        //Insert Data
        public virtual void InsertOne(TDeltaDoc document)
        {
            _azDeltaCollection.InsertOne(document);
        }

        public virtual Task InsertOneAsync(TDeltaDoc document)
        {
            return Task.Run(() => _azDeltaCollection.InsertOneAsync(document));
        }

        public void InsertMany(ICollection<TDeltaDoc> documents)
        {
            _azDeltaCollection.InsertMany(documents);
        }


        public virtual async Task InsertManyAsync(ICollection<TDeltaDoc> documents)
        {
            await _azDeltaCollection.InsertManyAsync(documents);
        }
        public void ReplaceOne(string Id, DeltaModel delta)
        {
            var objectId = new ObjectId(Id);
            var filter = Builders<TDeltaDoc>.Filter.Eq(doc => doc.Id, objectId);

            var update = Builders<TDeltaDoc>.Update
                            .Set("NextLink", delta.NextLink).Set("PrevUpdateDate", delta.PrevUpdateDate).Set("IsNextLinkCalled", delta.IsNextLinkCalled)
                            .Set("Value", delta.Value).Set("DeltaLink", delta.DeltaLink);
             _azDeltaCollection.UpdateOneAsync(filter, update);

        }

        public async Task ReplaceOneAsync(string Id, DeltaModel delta)
        {
            var objectId = new ObjectId(Id);
            var filter = Builders<TDeltaDoc>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDeltaDoc>.Update
                            .Set("NextLink", delta.NextLink).Set("PrevUpdateDate", delta.PrevUpdateDate).Set("IsNextLinkCalled", delta.IsNextLinkCalled)
                            .Set("Value", delta.Value).Set("DeltaLink", delta.DeltaLink);
            await _azDeltaCollection.UpdateOneAsync(filter, update);
        }

        public IEnumerable<TDeltaDoc> FilterBy(Expression<Func<TDeltaDoc, bool>> filterExpression)
        {
            return _azDeltaCollection.Find(filterExpression).ToEnumerable();
        }

        public void UpdateFlag(string Id, Boolean flag)
        {
            var objectId = new ObjectId(Id);
            var filter = Builders<TDeltaDoc>.Filter.Eq(doc => doc.Id, objectId);

            var update = Builders<TDeltaDoc>.Update
            .Set("IsNextLinkCalled", flag);
            _azDeltaCollection.UpdateOne(filter, update);
        }

        public async Task UpdateFlagAsync(string Id, Boolean flag)
        {
            var objectId = new ObjectId(Id);
            var filter = Builders<TDeltaDoc>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDeltaDoc>.Update
            .Set("IsNextLinkCalled", flag);
            await _azDeltaCollection.UpdateOneAsync(filter, update);

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

        public Boolean UpsertManyAsync(ICollection<TDeltaDoc> documents)
        {
            throw new NotImplementedException();
        }

        Task IDeltaRepository<TDeltaDoc>.UpsertManyAsync(ICollection<TDeltaDoc> documents)
        {
            throw new NotImplementedException();
        }
    }
}
