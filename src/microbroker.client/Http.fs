﻿namespace microbroker.client

open System
open System.Diagnostics.CodeAnalysis
open System.Threading.Tasks
open System.Net
open System.Net.Http

type HttpResponseHeaders = (string * string) list

type HttpRequestResponse =
    | HttpOkRequestResponse of
        status: HttpStatusCode *
        body: string *
        contentType: string option *
        headers: HttpResponseHeaders
    | HttpTooManyRequestsResponse of headers: HttpResponseHeaders
    | HttpBadGatewayResponse of headers: HttpResponseHeaders
    | HttpErrorRequestResponse of status: HttpStatusCode * body: string * headers: HttpResponseHeaders
    | HttpExceptionRequestResponse of ex: Exception

    static member status(response: HttpRequestResponse) =
        match response with
        | HttpOkRequestResponse(status, _, _, _) -> status
        | HttpTooManyRequestsResponse(_) -> System.Net.HttpStatusCode.TooManyRequests
        | HttpErrorRequestResponse(status, _, _) -> status
        | HttpExceptionRequestResponse _ -> HttpStatusCode.InternalServerError
        | HttpBadGatewayResponse _ -> HttpStatusCode.BadGateway

    static member loggable(response: HttpRequestResponse) =
        let status = HttpRequestResponse.status response
        $"{response.GetType().Name} {status}"

    static member headers(response: HttpRequestResponse) =
        match response with
        | HttpOkRequestResponse(_, _, _, headers) -> headers
        | HttpTooManyRequestsResponse(headers) -> headers
        | HttpErrorRequestResponse(_, _, headers) -> headers
        | _ -> []

    static member headerValues name (response: HttpRequestResponse) =
        response
        |> HttpRequestResponse.headers
        |> Seq.filter (fun t -> StringComparer.InvariantCultureIgnoreCase.Equals(fst t, name))
        |> Seq.map snd

    static member okBody(response: HttpRequestResponse) =
        match response with
        | HttpOkRequestResponse(_, json, _, _) -> Some json
        | _ -> None

[<ExcludeFromCodeCoverage>]
module internal Http =

    let body (resp: HttpResponseMessage) =
        task {
            let! body =
                match resp.Content.Headers.ContentEncoding |> Seq.tryHead with
                | Some x when x = "gzip" ->
                    task {
                        use s = resp.Content.ReadAsStream(System.Threading.CancellationToken.None)
                        return Strings.fromGzip s
                    }
                | _ -> resp.Content.ReadAsStringAsync()

            return body
        }

    let contentHeaders (resp: HttpResponseMessage) =
        resp.Content.Headers
        |> Seq.collect (fun x -> x.Value |> Seq.map (fun v -> Strings.toLower x.Key, v))

    let respHeaders (resp: HttpResponseMessage) =
        resp.Headers
        |> Seq.collect (fun x -> x.Value |> Seq.map (fun v -> (Strings.toLower x.Key, v)))

    let headers (resp: HttpResponseMessage) =
        respHeaders resp
        |> Seq.append (contentHeaders resp)
        |> Seq.sortBy fst
        |> List.ofSeq

    let parse (resp: HttpResponseMessage) =
        let respHeaders = headers resp

        match resp.IsSuccessStatusCode, resp.StatusCode with
        | true, _ ->
            task {
                let! body = body resp

                let mediaType =
                    resp.Content.Headers.ContentType
                    |> Option.ofNull<Headers.MediaTypeHeaderValue>
                    |> Option.map _.MediaType

                return HttpOkRequestResponse(resp.StatusCode, body, mediaType, respHeaders)
            }
        | false, HttpStatusCode.TooManyRequests -> HttpTooManyRequestsResponse(respHeaders) |> Threading.toTaskResult
        | false, HttpStatusCode.BadGateway -> HttpBadGatewayResponse(respHeaders) |> Threading.toTaskResult
        | false, _ ->
            task {
                let! body = body resp
                return HttpErrorRequestResponse(resp.StatusCode, body, respHeaders)
            }

    let send (client: HttpClient) (msg: HttpRequestMessage) =
        task {
            try
                use! resp = client.SendAsync msg
                return! parse resp
            with ex ->
                return HttpExceptionRequestResponse(ex)
        }

    let encodeUrl (value: string) = System.Web.HttpUtility.UrlEncode value

type IHttpClient =
    abstract member GetAsync: url: string -> Task<HttpRequestResponse>
    abstract member PutAsync: url: string -> content: string -> Task<HttpRequestResponse>
    abstract member PostAsync: url: string -> content: string -> Task<HttpRequestResponse>

[<ExcludeFromCodeCoverage>]
type internal InternalHttpClient(httpClient: HttpClient) =
    let httpSend = Http.send httpClient

    let getReq (url: string) =
        new HttpRequestMessage(HttpMethod.Get, url)

    let sendJsonReq (method: HttpMethod) (url: string) (content: string) =
        let result = new HttpRequestMessage(method, url)

        result.Content <-
            new System.Net.Http.StringContent(
                content,
                Text.Encoding.UTF8,
                System.Net.Mime.MediaTypeNames.Application.Json
            )

        result

    let putJsonReq = sendJsonReq HttpMethod.Put

    let postJsonReq = sendJsonReq HttpMethod.Post

    interface IHttpClient with
        member this.GetAsync url = url |> getReq |> httpSend
        member this.PutAsync url content = content |> putJsonReq url |> httpSend
        member this.PostAsync url content = content |> postJsonReq url |> httpSend
