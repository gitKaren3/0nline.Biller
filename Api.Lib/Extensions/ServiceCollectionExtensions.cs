using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Extensions;
using _0nline.Shared.Db.Contract.Models;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace _0nline.Biller.Api.Lib.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBillerControllers(this IServiceCollection services, IConfiguration config)
        {
            // Register controllers from this assembly
            var assembly = typeof(ServiceCollectionExtensions).Assembly;
           

            services.AddBillerServices(config); // Register services from the Biller.DL assembly

            services.AddControllers()
                .PartManager.ApplicationParts.Add(new AssemblyPart(assembly));
        }
    }
}

