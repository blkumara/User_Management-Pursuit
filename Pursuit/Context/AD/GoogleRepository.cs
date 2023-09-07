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
using System.Security.Cryptography;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System.Globalization;

namespace Pursuit.Context
{
    public class GoogleRepository<TAdDoc> : IADRepository<TAdDoc>
    where TAdDoc : IAdDoc
    {
        private readonly IMongoCollection<TAdDoc> _gwsAdCollection;



        public GoogleRepository(IADDBSettings settings)
        {
            var database = new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
            _gwsAdCollection = database.GetCollection<TAdDoc>("GWS_AD");


        }

        public virtual IQueryable<TAdDoc> AsQueryable()
        {
            return _gwsAdCollection.AsQueryable();
        }

        public virtual IEnumerable<TAdDoc> FilterBy(
            Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _gwsAdCollection.Find(filterExpression).ToEnumerable();
        }
        public virtual Task RemoveManyAsync(
          Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _gwsAdCollection.DeleteManyAsync(filterExpression);
        }
        public virtual IEnumerable<TAdDoc> NameFilter(string filterExpression)
        {
            var builder = Builders<TAdDoc>.Filter;

            var filter = builder.AnyEq("UserDocument.name.givenName", filterExpression);
            return _gwsAdCollection.Find(filter).ToEnumerable();
        }

        public virtual IEnumerable<TAdDoc> PhoneFilter(string filterExpression)
        {
            var builder = Builders<TAdDoc>.Filter;

            var filter = builder.AnyEq("UserDocument.phones", filterExpression);
            return _gwsAdCollection.Find(filter).ToEnumerable();
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
                            expr = "UserDocument.name.givenName";
                            break;
                        case "LASTNAME":
                            expr = "UserDocument.name.familyName";
                            break;
                        case "PHONE":
                            expr = "UserDocument.phones";
                            break;
                        case "EMAIL":
                            expr = "UserDocument.primaryEmail";
                            break;
                        default:
                            break;
                    }

                     switch (gr.ROperator.ToUpper())
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
                     }
 

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
            return _gwsAdCollection.Find(filterExpression).Project(projectionExpression).ToEnumerable();
        }

        public virtual TAdDoc FindOne(Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _gwsAdCollection.Find(filterExpression).FirstOrDefault();
        }

        public virtual Task<TAdDoc> FindOneAsync(Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return Task.Run(() => _gwsAdCollection.Find(filterExpression).FirstOrDefaultAsync());

        }
        //Find the data by id
        public virtual TAdDoc FindById(string id)
        {
            var objectId = new ObjectId(id);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            return _gwsAdCollection.Find(filter).SingleOrDefault();
        }

        public virtual Task<TAdDoc> FindByIdAsync(string id)
        {
            return Task.Run(() =>
            {
                var objectId = new ObjectId(id);
                var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
                return _gwsAdCollection.Find(filter).SingleOrDefaultAsync();
            });
        }

        //Find the data by google id
        public virtual TAdDoc FindByADId(string id)
        {
            var builder = Builders<TAdDoc>.Filter;
            var filter = builder.Eq("UserDocument.id", id);

            return _gwsAdCollection.Find(filter).SingleOrDefault();
        }

        public virtual Task<TAdDoc> FindByADIdAsync(string id)
        {
            return Task.Run(() =>
            {
                var builder = Builders<TAdDoc>.Filter;
                var filter = builder.Eq("UserDocument.id", id);
                return _gwsAdCollection.Find(filter).SingleOrDefaultAsync();
            });
        }

        //Insert Data
        public virtual void InsertOne(TAdDoc document)
        {
            _gwsAdCollection.InsertOne(document);
        }

        public virtual Task InsertOneAsync(TAdDoc document)
        {
            return Task.Run(() => _gwsAdCollection.InsertOneAsync(document));
        }

        public void InsertMany(ICollection<TAdDoc> documents)
        {
            _gwsAdCollection.InsertMany(documents);
        }


        public virtual async Task InsertManyAsync(ICollection<TAdDoc> documents)
        {
            await _gwsAdCollection.InsertManyAsync(documents);
        }
        //Update data by id
        public void ReplaceOne(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TAdDoc>.Update
                           .Set("Phone", user.Phone).Set("Email", user.Email).Set("Username", user.Username)
                           .Set("FirstName", user.FirstName).Set("LastName", user.LastName);
            _gwsAdCollection.UpdateOne(filter, update);
        }

        public virtual async Task ReplaceOneAsync(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TAdDoc>.Update
                          .Set("Phone", user.Phone).Set("Email", user.Email).Set("Username", user.Username)
                          .Set("FirstName", user.FirstName).Set("LastName", user.LastName);
            await _gwsAdCollection.UpdateOneAsync(filter, update);
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
            await _gwsAdCollection.BulkWriteAsync(bulkOps);
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
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

            var pipeline = new BsonDocument[]
              {
                                 new BsonDocument("$match", new BsonDocument
                                  {
                                        { "UserDocument.creationTime", new BsonDocument
                                             {
                                                { "$gte", new BsonDateTime(startdate) },
                                                { "$lte", new BsonDateTime(enddate) }
                                              }
                                        }
                                  }),
                                 new BsonDocument("$group", new BsonDocument
                                  {
                                       { "_id", new BsonDocument
                                             {
                                                  { "User", "Users" }
                                              }
                                       },
                                       { "count", new BsonDocument
                                           {
                                               { "$sum", 1 }
                                            }
                                        }
                                  }),
                                 new BsonDocument("$group", new BsonDocument
                                 {
                                       { "_id", "$_id.User" },
                                       { "count", new BsonDocument
                                         {
                                             { "$sum", "$count" }
                                         }
                                       }
                                 })
              };
            return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
        }

        public List<BsonDocument> gwslastLogOn(DateRangeRequest req)
        {
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

            var pipeline = new BsonDocument[]
              {
                                 new BsonDocument("$match", new BsonDocument
                                  {
                                        { "UserDocument.lastLoginTime", new BsonDocument
                                             {
                                                { "$gte", new BsonDateTime(startdate) },
                                                { "$lte", new BsonDateTime(enddate) }
                                              }
                                        }
                                  }),
                                 new BsonDocument("$group", new BsonDocument
                                  {
                                       { "_id", new BsonDocument
                                             {
                                                  { "User", "Users" }
                                              }
                                       },
                                       { "count", new BsonDocument
                                           {
                                               { "$sum", 1 }
                                            }
                                        }
                                  }),
                                 new BsonDocument("$group", new BsonDocument
                                 {
                                       { "_id", "$_id.User" },
                                       { "count", new BsonDocument
                                         {
                                             { "$sum", "$count" }
                                         }
                                       }
                                 })
              };
            return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
        }
        public IEnumerable<TAdDoc> gwsgetallemp()
        {
            FilterDefinition<TAdDoc> filter = null;
            var builder = Builders<TAdDoc>.Filter;
            string value = null;
            filter = builder.Ne("Email", value);
            return _gwsAdCollection.Find(filter).ToEnumerable();
        }

        public List<BsonDocument> gwsListCount()
        {
            var pipeline = new BsonDocument[]
                            {
                                 new BsonDocument("$group", new BsonDocument
                                 {
                                     { "_id", new BsonDocument
                                       {
                                        { "User", "Users" }
                                       }
                                     },
                                     { "count", new BsonDocument
                                         {
                                             { "$sum", 1 }
                                         }
                                     }
                                }),
                                 new BsonDocument("$group", new BsonDocument
                                 {
                                       { "_id", "$_id.User" },
                                       { "count", new BsonDocument
                                         {
                                           { "$sum", "$count" }
                                         }
                                       }
                                 })
                        };
            return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
            FilterDefinition<TAdDoc> filter = null;
            var builder = Builders<TAdDoc>.Filter;
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();


            filter = builder.And(
                   
                 builder.Gte("UserDocument.creationTime", new BsonDateTime(startdate)),
                 builder.Lte("UserDocument.creationTime", new BsonDateTime(enddate))
                );
            return _gwsAdCollection.Find(filter).ToEnumerable();


        }
        public IEnumerable<TAdDoc> lastLogOn(DateRangeRequest req)
        {
            FilterDefinition<TAdDoc> filter = null;
            var builder = Builders<TAdDoc>.Filter;
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();


            filter = builder.And(
                    
                 builder.Gte("UserDocument.lastLoginTime", new BsonDateTime(startdate)),
                 builder.Lte("UserDocument.lastLoginTime", new BsonDateTime(enddate))
                );
            return _gwsAdCollection.Find(filter).ToEnumerable();
        }

        public List<BsonDocument> msadCreateSingleData(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public  Boolean RemoveAsync(string Id)
        {
            var builder = Builders<TAdDoc>.Filter;
            var filter = builder.Eq("UserDocument.id", Id);

           
             _gwsAdCollection.DeleteOneAsync(filter);

            return true;
        }
    }
    }