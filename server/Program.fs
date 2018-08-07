module Program

open KestrelInterop
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging

let configureLogging (b : IWebHostBuilder) =
    b.ConfigureLogging(fun l -> l.AddConsole() |> ignore)

[<EntryPoint>]
let main argv =
    let store = EventStore.initializeStore "localhost" "noredink" "noredink"
    let configureApp = ApplicationBuilder.useFreya (Api.root store)
    WebHost.create()
    |> WebHost.bindTo [| "http://localhost:5000" |]
    |> configureLogging
    |> WebHost.configure configureApp
    |> WebHost.buildAndRun
    0
