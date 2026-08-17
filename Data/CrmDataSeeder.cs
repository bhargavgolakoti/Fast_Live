using AspnetCoreMvcFull.Models;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Data;

public static class CrmDataSeeder
{
    public static void Seed(CrmDbContext database)
    {
        database.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"VisitorCounters\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_VisitorCounters\" PRIMARY KEY, \"Count\" INTEGER NOT NULL);");
        if (!database.VisitorCounters.Any())
        {
            database.VisitorCounters.Add(new VisitorCounter { Id = 1, Count = 0 });
            database.SaveChanges();
        }

        if (database.Customers.Any())
        {
            return;
        }

        database.Customers.AddRange(
            new Customer { Name = "Ava Patel", Email = "ava@northstar.example", Company = "Northstar Labs", Phone = "+1 555 0142" },
            new Customer { Name = "Marcus Chen", Email = "marcus@orbit.example", Company = "Orbit Systems", Phone = "+1 555 0198" },
            new Customer { Name = "Sofia Williams", Email = "sofia@cedar.example", Company = "Cedar & Co.", Status = "Prospect" });
        database.Leads.AddRange(
            new Lead { Name = "Elena Torres", Email = "elena@novum.example", EstimatedValue = 12500, Status = "Qualified" },
            new Lead { Name = "Noah Brown", Email = "noah@river.example", EstimatedValue = 6800 });
        database.Tasks.AddRange(
            new CrmTask { Title = "Follow up with Elena Torres", DueDate = DateTime.Today },
            new CrmTask { Title = "Prepare Orbit Systems proposal", DueDate = DateTime.Today.AddDays(2) });
        database.SaveChanges();
    }
}