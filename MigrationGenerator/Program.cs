using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using gadget_repair.Data;

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

optionsBuilder.UseMySql(
    DatabaseConfig.ConnectionString,
    ServerVersion.AutoDetect(DatabaseConfig.ConnectionString)
);

using var db = new ApplicationDbContext(optionsBuilder.Options);

Console.WriteLine("DbContext loaded successfully.");
Console.WriteLine($"Database: {db.Database.GetDbConnection().Database}");