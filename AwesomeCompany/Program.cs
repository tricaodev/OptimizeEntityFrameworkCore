using AwesomeCompany;
using AwesomeCompany.Entities;
using AwesomeCompany.Options;
using AwesomeCompany.Responses;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.ConfigureOptions<DatabaseOptionsSetup>();

builder.Services.AddDbContext<DatabaseContext>((serviceProvider, optionsBuilder) =>
    {
        var configuration = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        optionsBuilder.UseNpgsql(configuration.ConnectionString, sqlAction =>
        {
            //sqlAction.EnableRetryOnFailure(configuration.EnableRetryOnFailure);

            sqlAction.CommandTimeout(configuration.CommandTimeout);
        });

        optionsBuilder.EnableDetailedErrors(configuration.EnableDetailedErrors);

        optionsBuilder.EnableSensitiveDataLogging(configuration.EnableSensitiveDataLogging);
    }
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

app.MapGet("company/{id}", async (int id, DatabaseContext context) =>
{
    var company = await context
        .Set<Company>()
        .FirstOrDefaultAsync(c => c.Id == id);

    if (company is null)
    {
        return Results.NotFound($"The company with Id {id} was not found.");
    }

    var response = new CompanyResponse()
    {
        Id = id,
        Name = company.Name
    };

    return Results.Ok(response);
});

app.Run();
