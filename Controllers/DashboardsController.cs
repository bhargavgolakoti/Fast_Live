using AspnetCoreMvcFull.Data;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Controllers;

public class DashboardsController : Controller
{
  private readonly CrmDbContext _database;

  public DashboardsController(CrmDbContext database)
  {
    _database = database;
  }

  public async Task<IActionResult> Index()
  {
    var model = new DashboardViewModel
    {
      CustomerCount = await _database.Customers.CountAsync(),
      LeadCount = await _database.Leads.CountAsync(),
      PipelineValue = await _database.Leads.SumAsync(lead => (decimal?)lead.EstimatedValue) ?? 0,
      OpenTaskCount = await _database.Tasks.CountAsync(task => !task.IsCompleted),
      RecentLeads = await _database.Leads.OrderByDescending(lead => lead.CreatedAt).Take(5).ToListAsync(),
      UpcomingTasks = await _database.Tasks.Where(task => !task.IsCompleted).OrderBy(task => task.DueDate).Take(5).ToListAsync()
    };

    return View(model);
  }
}
