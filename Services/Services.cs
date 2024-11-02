using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.DependencyInjection;
using PlayerSkin.Models;

namespace PlayerSkin.Services
{
    public class PlayerSkinInjection : IPluginServiceCollection<Plugin>
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Database>();
        }
    }
}
