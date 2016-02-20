using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Infrastructure.Web.Startup))]
namespace Infrastructure.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
