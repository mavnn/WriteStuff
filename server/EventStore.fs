module EventStore

open System
open Marten
open Marten.Services
open Hopac

type EventWrapper(evt) =
    member val Evt : WritingStuff.Event = evt with get, set

type StateWrapper() =
    member val Id : string = "" with get, set
    member val State : WritingStuff.State = WritingStuff.Empty with get, set

type AggregateWritingStuffState() =
    member val Id : string = "" with get, set
    member val Data : StateWrapper = StateWrapper() with get, set
    member this.Apply(evt : EventWrapper) =
        printfn "Apply has been called!"
        let after = WritingStuff.fold this.Data.State evt.Evt
        this.Data <- StateWrapper(State = after)

let initializeStore host username password =
    let connString =
        let builder = Npgsql.NpgsqlConnectionStringBuilder()
        builder.Host <- host
        builder.Username <- username
        builder.Password <- password
        builder.ConnectionString

    let setOptions (storeOptions : StoreOptions) =
        storeOptions.Connection(connString)
        storeOptions.Events.StreamIdentity <- Events.StreamIdentity.AsString
        storeOptions.Events.AddEventType(typeof<EventWrapper>)
        storeOptions.Events.InlineProjections.AggregateStreamsWith<AggregateWritingStuffState>()
        |> ignore


    DocumentStore.For(setOptions)

let writeEvents (store : IDocumentStore) (id : string) (evts : WritingStuff.Event seq) =
    job {
        use session =
            store.OpenSession(DocumentTracking.None,
                              Data.IsolationLevel.Serializable)
        printfn "Writing %d events to stream %s" (Seq.length evts) id
        for evt in evts do
            session.Events.Append(id, EventWrapper evt) |> ignore
        do! session.SaveChangesAsync
            |> Job.fromUnitTask
            |> Job.map (fun () -> printfn "Events written to stream %s" id)
    }

let getState (store : IDocumentStore) (id : string) =
    job {
        use session =
            store.OpenSession(DocumentTracking.None,
                              Data.IsolationLevel.Serializable)
        let! state =
            fun () ->
                session.LoadAsync<AggregateWritingStuffState>(id)
            |> Job.fromTask
        if isNull (box state) then
            return None
        else
            return Some (state.Data.State)
    }
