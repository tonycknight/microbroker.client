namespace Microbroker.Client.Tests

open System
open System.Net.Http
open Microbroker.Client
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open NSubstitute
open Xunit
open FsUnit

module DependencyInjectionTests =

    [<Fact>]
    let ``addServices returns instance on demand`` () =
        let sc =
            (new ServiceCollection() :> IServiceCollection)
                .AddSingleton<HttpClient>(new HttpClient())
                .AddSingleton<ILoggerFactory>(Substitute.For<ILoggerFactory>())

        let config =
            { MicrobrokerConfiguration.brokerBaseUrl = ""
              MicrobrokerConfiguration.throttleMaxTime = TimeSpan.FromSeconds 5. }

        let sc = DependencyInjection.addServices config sc
        let sp = sc.BuildServiceProvider()

        let proxy = sp.GetService<IMicrobrokerProxy>()

        proxy |> should not' (equal null)
