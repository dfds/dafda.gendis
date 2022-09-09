namespace Dafda.Gendis.App.Configuration;

public static class Database
{
    public static void ConfigureDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<DbConnectionFactory>(provider =>
        {
            var connectionString = builder.Configuration["DB_CONNECTION_STRING"];
            return new DbConnectionFactory(connectionString);
        });
    }
}