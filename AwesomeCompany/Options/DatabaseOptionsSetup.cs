using Microsoft.Extensions.Options;

namespace AwesomeCompany.Options;

public class DatabaseOptionsSetup : IConfigureOptions<DatabaseOptions>
{
    private readonly IConfiguration _configuration;

    private readonly string _sectionName = "DatabaseOptions";

    public DatabaseOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public void Configure(DatabaseOptions options)
    {
        options.ConnectionString = _configuration.GetConnectionString("DefaultConnection");

        _configuration.GetSection(_sectionName).Bind(options);
    }
}
