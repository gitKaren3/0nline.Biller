using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Numbering;
using _0nline.Biller.DL.Queries;
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
            services.AddScoped<IInvoicePdfService, InvoicePdfService>();

            // Invoice-specific reads and the number counter are DL implementation
            // details, not API surfaces, so they are registered explicitly rather than
            // discovered as a DbStore or a query provider.
            services.AddScoped<IInvoiceQueries, InvoiceQueries>();
            services.AddScoped<IInvoiceNumberAllocator, InvoiceNumberAllocator>();

            // The remaining app queries: statements the entity store cannot express.
            services.AddScoped<IBankAccountQueries, BankAccountQueries>();
            services.AddScoped<ICompanyQueries, CompanyQueries>();
            services.AddScoped<ITenantSettingQueries, TenantSettingQueries>();
            services.AddScoped<IInvoiceSnapshotQueries, InvoiceSnapshotQueries>();

        }
    }
}
