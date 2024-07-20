namespace Microbroker.Client

open Microsoft.Extensions.DependencyInjection

module Startup =
    let addServices (sp: IServiceCollection) =
        sp
            .AddSingleton<Http.IHttpClient, Http.InternalHttpClient>()
            .AddSingleton<IMicrobrokerProxy, MicrobrokerProxy>()
