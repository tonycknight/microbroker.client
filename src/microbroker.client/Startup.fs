﻿namespace Microbroker.Client

open Microsoft.Extensions.DependencyInjection

module Startup =
    let addServices (sp: IServiceCollection) =
        sp
            .AddSingleton<IHttpClient, InternalHttpClient>()
            .AddSingleton<IMicrobrokerProxy, MicrobrokerProxy>()
