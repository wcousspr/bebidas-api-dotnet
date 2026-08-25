using crudEbancoDist8.Interfaces;
using crudEbancoDist8.Services;
using crudEbancoDist8.Mappings;

namespace crudEbancoDist8.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IBebidasService, BebidasService>();

        services.AddAutoMapper(
       cfg => { },
       typeof(BebidasProfile)
   );

        return services;
    }
}