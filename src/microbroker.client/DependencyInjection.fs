namespace Microbroker.Client

open Microsoft.Extensions.DependencyInjection

module DependencyInjection =
    let addServices (config: MicrobrokerConfiguration) (sc: IServiceCollection) =
        sc
            .AddSingleton<MicrobrokerConfiguration>(config)
            .AddSingleton<IHttpClient, InternalHttpClient>()
            .AddSingleton<IMicrobrokerProxy, MicrobrokerProxy>()
