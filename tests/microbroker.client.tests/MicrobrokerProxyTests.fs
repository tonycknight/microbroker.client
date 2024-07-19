namespace Microbroker.Client.Tests

open System
open System.Net
open Microbroker.Client
open Microsoft.Extensions.Logging
open NSubstitute
open Xunit
open FsUnit

module MicrobrokerProxyTests =

    let toJson values =
        Newtonsoft.Json.JsonConvert.SerializeObject values

    let testConfig =
        { MicrobrokerConfiguration.brokerBaseUrl = "a"
          throttleMaxTime = TimeSpan.FromSeconds(1.) }

    let logger = Substitute.For<ILoggerFactory>()

    let ok json =
        HttpOkRequestResponse(HttpStatusCode.OK, json, None, [])

    let notfound json =
        HttpErrorRequestResponse(HttpStatusCode.NotFound, json, [])

    let badRequest json =
        HttpErrorRequestResponse(HttpStatusCode.BadRequest, json, [])

    let httpClient (response: HttpRequestResponse) =
        let http = Substitute.For<IHttpClient>()

        let param = Arg.Any<string>()

        http.GetAsync(param).Returns(Tasks.toTaskResult response) |> ignore

        http

    let httpClientPost (response: HttpRequestResponse) =
        let http = Substitute.For<IHttpClient>()

        (http.PostAsync (Arg.Any<string>()) (Arg.Any<string>()))
            .Returns(Tasks.toTaskResult response)
        |> ignore

        http

    let httpClientPostException () =
        let http = Substitute.For<IHttpClient>()

        (http.PostAsync (Arg.Any<string>()) (Arg.Any<string>()))
            .Returns(System.Threading.Tasks.Task.FromException<HttpRequestResponse>(new InvalidOperationException()))
        |> ignore

        http

    let proxy config logger client =
        new MicrobrokerProxy(config, client, logger) :> IMicrobrokerProxy

    let defaultProxy client = proxy testConfig logger client

    [<Fact>]
    let ``GetQueueCounts on empty array returns empty`` () =

        let resp = ok "[]"
        let http = httpClient resp
        let proxy = defaultProxy http

        let r = proxy.GetQueueCounts([| "" |]).Result

        r.Length |> should equal 0

    [<Fact>]
    let ``GetQueueCounts on matching name returns value`` () =

        let name = Guid.NewGuid().ToString()

        let counts =
            [| { MicrobrokerCount.name = name
                 count = 1
                 futureCount = 2 } |]

        let resp = ok (counts |> toJson)
        let http = httpClient resp
        let proxy = defaultProxy http

        let r = proxy.GetQueueCounts([| name |]).Result

        r.Length |> should equal 1
        r.[0] |> should equal counts.[0]

    [<Fact>]
    let ``GetQueueCounts on no matching name returns empty`` () =

        let name = Guid.NewGuid().ToString()

        let counts =
            [| { MicrobrokerCount.name = name
                 count = 1
                 futureCount = 2 } |]

        let resp = ok (counts |> toJson)
        let http = httpClient resp
        let proxy = defaultProxy http

        let r = proxy.GetQueueCounts([| Guid.NewGuid().ToString() |]).Result

        r.Length |> should equal 0

    [<Fact>]
    let ``GetNext on empty returns empty`` () =
        let resp = notfound ""
        let http = httpClient resp
        let proxy = defaultProxy http

        let r = proxy.GetNext("test").Result

        r |> should equal None

    [<Fact>]
    let ``GetNext on error returns empty`` () =
        let resp = badRequest ""
        let http = httpClient resp
        let proxy = defaultProxy http

        let r = proxy.GetNext("test").Result

        r |> should equal None

    [<Fact>]
    let ``GetNext returns message`` () =
        let msg =
            { MicrobrokerMessage.content = "test"
              messageType = "test msg"
              created = DateTimeOffset.UtcNow
              active = DateTimeOffset.UtcNow }

        let resp = msg |> toJson |> ok
        let http = httpClient resp
        let proxy = defaultProxy http

        let r = proxy.GetNext("test").Result

        r |> should equal (Some msg)

    [<Fact>]
    let ``PostMany with empty sequence posts nothing`` () =
        let resp = ok "[]"
        let http = httpClientPost resp
        let proxy = defaultProxy http

        let r = (proxy.PostMany "queue" []).Result

        http.DidNotReceiveWithAnyArgs().PostAsync (Arg.Any<string>()) (Arg.Any<string>())
        |> ignore

    [<Fact>]
    let ``PostMany on exception does not raise exceptions`` () =
        let http = httpClientPostException ()
        let proxy = defaultProxy http

        let r = (proxy.PostMany "queue" []).Result

        ignore r

    [<Fact>]
    let ``PostMany with sequence`` () =
        let resp = ok "[]"
        let http = httpClientPost resp
        let proxy = defaultProxy http

        let msgs =
            [ { MicrobrokerMessage.content = "test"
                messageType = "test msg"
                created = DateTimeOffset.UtcNow
                active = DateTimeOffset.UtcNow } ]

        let r = (proxy.PostMany "queue" msgs).Result

        http.ReceivedWithAnyArgs().PostAsync (Arg.Any<string>()) (toJson msgs) |> ignore
