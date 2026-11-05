using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Reports;
using _0nline.Shared.Db.Service.Extensions;
using _0nline.Shared.Services;
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

            // PDF layouts are app-specific: the shared renderer resolves this one
            // for an InvoicePdfModel. Add one line per document type here.
            services.AddScoped<IPdfDocument<InvoicePdfModel>, InvoicePdfDocument>();

        }
    }
}
