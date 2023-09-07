using Pursuit.Model;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Pursuit.Context;

namespace Pursuit.Service;

public class OrganizationsService
{
    private readonly IMongoCollection<Organization> _orgCollection;

    public OrganizationsService(
        IOptions<PursuitDBSettings> orgDBSettings)
    {
        var mongoClient = new MongoClient(orgDBSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(orgDBSettings.Value.DatabaseName);

        _orgCollection = mongoDatabase.GetCollection<Organization>(
            orgDBSettings.Value.EDBCollectionName);
    }

    public async Task<List<Organization>> GetAsync() =>
        await _orgCollection.Find(_ => true).ToListAsync();

    public async Task<Organization?> GetAsync(string id) =>
        await _orgCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Organization newBook) =>
        await _orgCollection.InsertOneAsync(newBook);

    public async Task UpdateAsync(string id, Organization updatedBook) =>
        await _orgCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

    public async Task RemoveAsync(string id) =>
        await _orgCollection.DeleteOneAsync(x => x.Id == id);
}