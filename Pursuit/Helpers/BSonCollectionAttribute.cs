[AttributeUsage(AttributeTargets.Class, Inherited = false)]
/* =========================================================
    Item Name: Collection Class-BsonCollectionAttribute
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
public class BsonCollectionAttribute : Attribute
{
    public string CollectionName { get; }

    public BsonCollectionAttribute(string collectionName)
    {
        CollectionName = collectionName;
    }
}