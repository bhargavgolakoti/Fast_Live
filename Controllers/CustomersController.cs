using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Controllers;

public class CustomersController(CrmDbContext database) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var query = database.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(customer => customer.Name.Contains(term) || customer.Email.Contains(term) || (customer.Company != null && customer.Company.Contains(term)) || customer.Status.Contains(term));
        }

        ViewData["Search"] = search;
        return View(await query.OrderByDescending(customer => customer.CreatedAt).ToListAsync());
    }

    public IActionResult Create() => View(new Customer());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        if (!ModelState.IsValid) return View(customer);
        customer.CreatedAt = DateTime.UtcNow;
        database.Customers.Add(customer);
        await database.SaveChangesAsync();
        TempData["Success"] = "Customer created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var customer = await database.Customers.FindAsync(id);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.Id) return NotFound();
        if (!ModelState.IsValid) return View(customer);
        var existing = await database.Customers.FindAsync(id);
        if (existing is null) return NotFound();
        existing.Name = customer.Name;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.Company = customer.Company;
        existing.Status = customer.Status;
        await database.SaveChangesAsync();
        TempData["Success"] = "Customer updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await database.Customers.FindAsync(id);
        if (customer is not null)
        {
            database.Customers.Remove(customer);
            await database.SaveChangesAsync();
            TempData["Success"] = "Customer deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}