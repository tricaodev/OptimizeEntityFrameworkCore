using AwesomeCompany;
using AwesomeCompany.Entities;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<DatabaseContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapPut("increase-salary", async (int companyId, DatabaseContext context) =>
{
    var company = await context.Set<Company>()
        .Include(c => c.Employees)
        .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id {companyId} was not found.");
    }

    foreach (var employee in company.Employees)
    {
        employee.Salary *= 1.1m;
    }

    company.LastSalaryUpdateUtc = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return Results.NoContent();
});

app.MapPut("increase-salary-sql", async (int companyId, DatabaseContext context) =>
{
    var company = await context.Set<Company>()
        .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id {companyId} was not found.");
    }

    await context.Database.BeginTransactionAsync();

    await context.Database.ExecuteSqlInterpolatedAsync(
        $"UPDATE public.\"Employees\" SET \"Salary\" = \"Salary\" * 1.1 WHERE \"CompanyId\" = {companyId}"
    );

    company.LastSalaryUpdateUtc = DateTime.UtcNow;

    await context.SaveChangesAsync();

    await context.Database.CommitTransactionAsync();

    return Results.NoContent();

});

app.MapPut("increase-salary-sql-dapper", async (int companyId, DatabaseContext context) =>
{
    var company = await context.Set<Company>()
        .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id {companyId} was not found.");
    }

    var transaction = await context.Database.BeginTransactionAsync();

    await context.Database.GetDbConnection().ExecuteAsync(
        "UPDATE public.\"Employees\" SET \"Salary\" = \"Salary\" * 1.1 WHERE \"CompanyId\" = @CompanyId", new
        {
            CompanyId = companyId
        },
        transaction.GetDbTransaction()
    );

    company.LastSalaryUpdateUtc = DateTime.UtcNow;

    await context.SaveChangesAsync();

    await context.Database.CommitTransactionAsync();

    return Results.NoContent();

});

app.Run();
