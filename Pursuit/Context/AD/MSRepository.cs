using MongoDB.Bson;
using MongoDB.Driver;

using Pursuit.Model;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Net;
using System.Reflection.Metadata;
using System;
using System.Xml.Linq;
using System.Text;
using System.Data;
using static Pursuit.Utilities.Enums;
using Pursuit.Context.AD;

/* =========================================================
    Item Name: Repository Class-ADRepository
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Context
{
    public class MSRepository<TAdDoc> : IADRepository<TAdDoc>
    where TAdDoc : IAdDoc
    {
        private readonly IMongoCollection<TAdDoc> _msAdCollection;
        private readonly IMongoCollection<TAdDoc> _azAdCollection;
        private readonly IMongoCollection<TAdDoc> _awsAdCollection;
        private readonly IMongoCollection<TAdDoc> _gwsAdCollection;

        public MSRepository(IADDBSettings settings)
        {
            var database = new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
            _msAdCollection = database.GetCollection<TAdDoc>("MS_AD");
        }

        public virtual IQueryable<TAdDoc> AsQueryable()
        {
            return _msAdCollection.AsQueryable();
        }

        public virtual IEnumerable<TAdDoc> FilterBy(
            Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _msAdCollection.Find(filterExpression).ToEnumerable();
        }
        public virtual Task RemoveManyAsync(
          Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _msAdCollection.DeleteManyAsync(filterExpression);
        }
        public virtual IEnumerable<TAdDoc> NameFilter(string filterExpression)
        {
            var builder = Builders<TAdDoc>.Filter;

            var filter = builder.AnyEq("ADDocument.Properties.givenname", filterExpression);

            return _msAdCollection.Find(filter).ToEnumerable();
        }
        public IEnumerable<TAdDoc> PhoneFilter(string filterExpression)
        {
            var builder = Builders<TAdDoc>.Filter;

            var filter = builder.AnyEq("ADDocument.Properties.telephonenumber", filterExpression);

            return _msAdCollection.Find(filter).ToEnumerable();
        }
        public virtual IEnumerable<TAdDoc> MultiFilter(
          ICollection<gFilterRequest> filters)
        {
            var builder = Builders<TAdDoc>.Filter;
            var filter = builder.Empty;
            var theFilter = builder.Empty;
            var flag = false;

            foreach (gFilterRequest gr in filters)
            {
                string expr = String.Empty;
                if (!string.IsNullOrWhiteSpace(gr.Key))
                {
                    switch (gr.Key.ToUpper())
                    {
                        case "FIRSTNAME":
                            expr = "ADDocument.Properties.givenname";
                            break;
                        case "LASTNAME":
                            expr = "UserDocument.Surname"; // Need to change this based on property name
                            break;
                        case "PHONE":
                            expr = "ADDocument.Properties.telephonenumber";
                            break;
                        case "EMAIL":
                            expr = "UserDocument.Mail"; // Need to change this based on property name
                            break;
                        default:
                            break;
                    }

                   /* switch (gr.ROperator.ToUpper())
                    {
                        case "EQ":
                            theFilter = builder.Eq(expr, gr.Value);

                            break;
                        case "NEQ":
                            theFilter = builder.Ne(expr, gr.Value);
                            break;
                        case "WC":
                            theFilter = new BsonDocument
                             { { expr, new BsonDocument { { "$regex", gr.Value }, { "$options", "i" } } } };

                            break;

                        default:
                            break;
                    }*/


                    if (flag)
                    {
                        if (gr.Operator == "AND")
                            filter &= theFilter;
                        else if (gr.Operator == "OR")
                            filter |= theFilter;
                        else
                            throw new Exception("Operator Needed");
                    }
                    else
                    {
                        filter = theFilter;
                        flag = true;
                    }
                }
            }

            try
            {
                return _gwsAdCollection.Find(filter).ToEnumerable();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public virtual IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TAdDoc, bool>> filterExpression,
            Expression<Func<TAdDoc, TProjected>> projectionExpression)
        {
            return _msAdCollection.Find(filterExpression).Project(projectionExpression).ToEnumerable();
        }

        public virtual TAdDoc FindOne(Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _msAdCollection.Find(filterExpression).FirstOrDefault();
        }

        public virtual Task<TAdDoc> FindOneAsync(Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return Task.Run(() => _msAdCollection.Find(filterExpression).FirstOrDefaultAsync());

        }
        //Find the data by id
        public virtual TAdDoc FindById(string id)
        {
            var objectId = new ObjectId(id);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            return _msAdCollection.Find(filter).SingleOrDefault();
        }

        public virtual Task<TAdDoc> FindByIdAsync(string id)
        {
            return Task.Run(() =>
            {
                var objectId = new ObjectId(id);
                var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
                return _msAdCollection.Find(filter).SingleOrDefaultAsync();
            });
        }

        //Insert Data
        public virtual void InsertOne(TAdDoc document)
        {
            _msAdCollection.InsertOne(document);
        }

        public virtual Task InsertOneAsync(TAdDoc document)
        {
            return Task.Run(() => _msAdCollection.InsertOneAsync(document));
        }

        public void InsertMany(ICollection<TAdDoc> documents)
        {
            _msAdCollection.InsertMany(documents);
        }


        public virtual async Task InsertManyAsync(ICollection<TAdDoc> documents)
        {
            await _msAdCollection.InsertManyAsync(documents);
        }
        //Update data by id
        public void ReplaceOne(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TAdDoc>.Update
                           .Set("Phone", user.Phone).Set("Email", user.Email).Set("Username", user.Username)
                           .Set("FirstName", user.FirstName).Set("LastName", user.LastName);
            _msAdCollection.UpdateOne(filter, update);
        }

        public virtual async Task ReplaceOneAsync(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TAdDoc>.Update
                          .Set("Phone", user.Phone).Set("Email", user.Email).Set("Username", user.Username)
                          .Set("FirstName", user.FirstName).Set("LastName", user.LastName);
            await _msAdCollection.UpdateOneAsync(filter, update);
        }

        public virtual async Task UpsertManyAsync(ICollection<TAdDoc> documents)
        {
            var bulkOps = new List<WriteModel<TAdDoc>>();
            foreach (var record in documents)
            {
                var upsertOne = new ReplaceOneModel<TAdDoc>(
                    Builders<TAdDoc>.Filter.Where(x => x.Email == record.Email),
                    record)
                { IsUpsert = true };
                bulkOps.Add(upsertOne);
            }
            await _msAdCollection.BulkWriteAsync(bulkOps);
        }

        public string DecryptAsync(string key)
        {
            throw new NotImplementedException();
        }
        public IList<string> DistinctList(string listKey, string deptName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> DateRangeEmpByDept(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> DeptEmpCount()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> pwdLastSet(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> lastLogOn(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> lastLogOff(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> badPwdCount(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> MGREmpCount()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> CompHeadCount()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadListCount()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadpwdLastSet(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadlastLogOn(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadlastLogOff(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadbadPwd(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> logFail(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadAllAttribute(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> msadgetallemp()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> gwsUserCreated(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> gwslastLogOn(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> gwsgetallemp()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> gwsListCount()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> azurepwdLastSet(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> azureUserCreated(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> azureListCount()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> azuregetallemp()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadpwdLastSetSingleData(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadlastLogOnSingleData(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadlastLogOffSingleData(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadbadPwdSingleData(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> createdTime(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadCreateSingleData(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public Boolean RemoveAsync(string EmailId)
        {
            var builder = Builders<TAdDoc>.Filter;
            var filter = builder.Eq("Email", EmailId);


            _msAdCollection.DeleteOneAsync(filter);

            return true;
        }

        public TAdDoc FindByADId(string id)
        {
            throw new NotImplementedException();
        }

        public Task<TAdDoc> FindByADIdAsync(string id)
        {
            throw new NotImplementedException();
        }
    }
}