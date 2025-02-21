using AwesomeCompany.Entities;
using Microsoft.EntityFrameworkCore;

namespace AwesomeCompany;

public class DatabaseContext: DbContext
{
    public DatabaseContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(builder =>
        {
            builder.ToTable("Companies")
                .HasMany(c => c.Employees)
                .WithOne()
                .HasForeignKey(e => e.CompanyId)
                .IsRequired();

            builder.HasData(new Company
            {
                Id = 1,
                Name = "Awesome Company"
            });
        });

        modelBuilder.Entity<Employee>(builder =>
        {
            builder.ToTable("Employees");

            List<Employee> employees = Enumerable
                .Range(1, 1000)
                .Select(id => new Employee
                {
                    Id = id,
                    Name = $"Employee #{id}",
                    Salary = 100.0m,
                    CompanyId = 1
                }).ToList();

            builder.HasData(employees);
        });
    }
}
