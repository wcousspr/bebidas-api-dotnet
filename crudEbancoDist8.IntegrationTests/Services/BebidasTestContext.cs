using AutoMapper;
using crudEbancoDist.Data;
using crudEbancoDist8.Mappings;
using crudEbancoDist8.Models;
using crudEbancoDist8.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace crudEbancoDist8.IntegrationTests.Services
{
    public sealed class BebidasTestContext : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        public AppDbContext Context { get; }

        public BebidasService Service { get; }

        private BebidasTestContext(
            SqliteConnection connection,
            AppDbContext context,
            ServiceProvider provider,
            BebidasService service)
        {
            _connection = connection;
            Context = context;
            _provider = provider;
            Service = service;
        }

        public static async Task<BebidasTestContext> CreateAsync()
        {
            var connection =
                new SqliteConnection("Data Source=:memory:");

            AppDbContext? context = null;
            ServiceProvider? provider = null;

            try
            {
                await connection.OpenAsync();

                var options =
                    new DbContextOptionsBuilder<AppDbContext>()
                        .UseSqlite(connection)
                        .Options;

                context = new AppDbContext(options);

                await context.Database.EnsureCreatedAsync();

                var categoria = new Categoria
                {
                    Name = "Refrigerante"
                };

                context.Categorias.Add(categoria);

                context.Bebidas.AddRange(
                    new Bebidas
                    {
                        Name = "Bebida A",
                        Price = 5m,
                        Quantity = 5,
                        Category = categoria
                    },
                    new Bebidas
                    {
                        Name = "Bebida B",
                        Price = 6m,
                        Quantity = 10,
                        Category = categoria
                    },
                    new Bebidas
                    {
                        Name = "Bebida C",
                        Price = 7m,
                        Quantity = 20,
                        Category = categoria
                    },
                    new Bebidas
                    {
                        Name = "Bebida D",
                        Price = 8m,
                        Quantity = 30,
                        Category = categoria
                    });

                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();

                var services = new ServiceCollection();

                services.AddLogging();

                services.AddAutoMapper(config =>
                {
                    config.AddProfile<BebidasProfile>();
                });

                provider = services.BuildServiceProvider();

                var mapper = provider.GetRequiredService<IMapper>();

                var service = new BebidasService(
                    context,
                    mapper,
                    NullLogger<BebidasService>.Instance);

                return new BebidasTestContext(
                    connection,
                    context,
                    provider,
                    service);
            }
            catch
            {
                // Libera recursos se a preparação falhar.
                try
                {
                    if (context is not null)
                        await context.DisposeAsync();
                }
                finally
                {
                    try
                    {
                        if (provider is not null)
                            await provider.DisposeAsync();
                    }
                    finally
                    {
                        await connection.DisposeAsync();
                    }
                }

                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Context.DisposeAsync();
            }
            finally
            {
                try
                {
                    await _provider.DisposeAsync();
                }
                finally
                {
                    await _connection.DisposeAsync();
                }
            }
        }
    }
}