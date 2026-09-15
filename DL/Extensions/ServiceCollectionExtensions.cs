using _0nline.Biller.DL.Contract.Models;
using _0nline.Shared.Db.Service.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;


namespace _0nline.Biller.DL.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBillerServices(this IServiceCollection services, IConfiguration dbconfig)
        {
            services.AddDatabase<BillerDbConfig>(dbconfig);

            // Use the assembly where this extension method is defined
            var assembly = Assembly.GetExecutingAssembly();

            // register this assembly's required services      
            services.AddDbServices(assembly);

        }
    }
}
