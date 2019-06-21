using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyFunctionApp.Services;
using System.IO;

[assembly: FunctionsStartup(typeof(MyFunctionApp.Startup))]
namespace MyFunctionApp
{
    // Use dependency injection in .NET Azure Functions
    // https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
    // View or download a sample of different service lifetimes on GitHub.
    // https://github.com/Azure/azure-functions-dotnet-extensions/tree/master/src/samples/DependencyInjection/Scopes
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            //builder.Services.AddSingleton((s) => {
            //    return new CosmosClient(Environment.GetEnvironmentVariable("COSMOSDB_CONNECTIONSTRING"));
            //});

            builder.Services.AddOptions();

            // https://blog.jongallant.com/2018/01/azure-function-config/

            var basePath = Directory.GetCurrentDirectory();

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Register MyServiceA as transient.
            // A new instance will be returned every
            // time a service request is made
            builder.Services.AddTransient<MyServiceA>();

            // Register MyServiceB as scoped.
            // The same instance will be returned
            // within the scope of a function invocation
            builder.Services.AddScoped<MyServiceB>();

            // Register ICommonIdProvider as scoped.
            // The same instance will be returned
            // within the scope of a function invocation
            builder.Services.AddScoped<ICommonIdProvider, CommonIdProvider>();


            // Register IGlobalIdProvider as singleton.
            // A single instance will be created and reused
            // with every service request
            builder.Services.AddSingleton<IGlobalIdProvider, CommonIdProvider>();
        }
    }
}
