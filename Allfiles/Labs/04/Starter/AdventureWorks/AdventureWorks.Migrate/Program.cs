using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdventureWorks.Context;
using AdventureWorks.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

public class Program
{
    private const string sqlDBConnectionString = "Server=tcp:polysqlsrvrsimon.database.windows.net,1433;Initial Catalog=AdventureWorks;Persist Security Info=False;User ID=testuser;Password=TestPa55w.rd;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
    private const string cosmosDBConnectionString = "AccountEndpoint=https://polycosmossimon.documents.azure.com:443/;AccountKey=NxYwH4H1Bt0RaiPyqtJ7nCJhbFtBqlu9cSYBDgVTMn0XOL5j80z7W09F0IA0drADN5in84OQfzWCvmVff4IRfA==;";

    public static async Task Main(string[] args)
    {
        await Console.Out.WriteLineAsync("Start Migration");

        using AdventureWorksSqlContext context = new AdventureWorksSqlContext(sqlDBConnectionString);
        List<Model> items = await context.Models
            .Include(m => m.Products)
            .ToListAsync<Model>();

        await Console.Out.WriteLineAsync($"Total Azure SQL DB Records: {items.Count}");

        using CosmosClient client = new CosmosClient(cosmosDBConnectionString);
        Database database = await client.CreateDatabaseIfNotExistsAsync("Retail");

        Container container = await database.CreateContainerIfNotExistsAsync(
            "Online",
            partitionKeyPath: $"/{nameof(Model.Category)}"
        );

        int count = 0;
        foreach (var item in items)
        {
            ItemResponse<Model> document = await container.UpsertItemAsync<Model>(item);
            await Console.Out.WriteLineAsync($"Upserted document #{++count:000} [Activity Id: {document.ActivityId}]");
        }

        await Console.Out.WriteLineAsync($"Total Azure Cosmos DB Documents: {count}");
    }
}