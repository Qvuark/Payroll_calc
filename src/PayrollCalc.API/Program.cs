using Microsoft.EntityFrameworkCore;
using Npgsql;
using PayrollCalc.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// EnableDynamicJson — обов'язково для List<string> у jsonb колонці (Position.ExcelAliases).
// Npgsql 8+ вимагає явний opt-in для dynamic JSON сериалізації.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dataSource = new NpgsqlDataSourceBuilder(connectionString)
    .EnableDynamicJson()
    .Build();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dataSource));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
