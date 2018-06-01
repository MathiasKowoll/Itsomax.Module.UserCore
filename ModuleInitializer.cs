using Itsomax.Data.Infrastructure;
using Itsomax.Module.UserCore.Interfaces;
using Itsomax.Module.UserCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Itsomax.Module.UserCore
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IManageUser, ManageUser>();
        }
    }
}