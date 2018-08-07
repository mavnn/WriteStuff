module Api

open Freya.Core
open Freya.Machines.Http
open Freya.Types.Http
open Freya.Routers.Uri.Template
open Freya.Optics.Http
open Freya.Machines.Http.Cors
open Freya.Types.Http.Cors
open System
open System.IO
open Hopac
open Json

let email =
    freya { 
        let! auth = Freya.Optic.get Request.Headers.authorization_
        match auth with
        | Some a when a.ToLowerInvariant().StartsWith "bearer " -> 
            return Some(a.Substring 7)
        | _ -> return None
    }
    |> Freya.memo

let user =
    freya { 
        let! email = email
        match email with
        | Some e -> return! Auth.userExists e |> Freya.fromJob
        | None -> return None
    }

let authMachine =
    freyaMachine { 
        authorized (email |> Freya.map (fun e -> e.IsSome))
        allowed (user |> Freya.map (fun u -> u.IsSome))
        handleUnauthorized (Represent.json "No authorization provided")
        handleForbidden (Represent.json "Forbidden")
    }

module WritingStuffApi =
    let stuffId = Freya.Optic.get <| Route.atom_ "stuffId"
    
    let getState store =
        freya { 
            let! id = stuffId
            match id with
            | Some i -> return! EventStore.getState store i |> Freya.fromJob
            | None -> 
                eprintfn "No ID for Writing Stuff?"
                return None
        }
    
    let eventsPosted getState store freyaCmds =
        freya { 
            let! cmds = freyaCmds
            let! id = stuffId
            match id with
            | Some i -> 
                let! state = getState
                             |> Freya.map (function 
                                    | Some s -> s
                                    | None -> WritingStuff.Empty)
                do! cmds
                    |> Seq.collect (WritingStuff.apply state)
                    |> EventStore.writeEvents store i
                    |> Freya.fromJob
            | None -> eprintfn "No ID for Writing Stuff?"
        }
        |> Freya.memo
    
    let view getState =
        freya { 
            let! id = stuffId
            match id with
            | Some i -> 
                let! state = getState
                match state with
                | Some s -> return Represent.json s
                | None -> 
                    eprintfn "We didn't manage to load the state"
                    return Represent.json (WritingStuff.Empty)
            | None -> 
                eprintfn "No ID for Writing Stuff?"
                return Represent.json WritingStuff.Empty
        }
    
    let getStuff getState =
        freyaMachine { 
            including authMachine
            methods [ GET; HEAD; OPTIONS ]
            handleOk (view getState)
        }
    
    let createStuff getState eventPosted =
        let freyaCmd =
            freya { 
                let! owner = user
                match owner with
                | Some o -> return [ WritingStuff.Create o ]
                | None -> 
                    eprintfn "No user found in authorized route"
                    return []
            }
        freyaMachine { 
            including authMachine
            methods [ PUT; HEAD; OPTIONS ]
            doPut (eventPosted freyaCmd)
            conflict (Freya.map Option.isSome getState)
            handleOk (view getState)
            handleConflict (Represent.json "This stuff already exists")
        }
    
    let updateTitle getState eventPosted =
        let freyaCmd = freya { let! newTitle = Interpret.json
                               return [ WritingStuff.UpdateTitle newTitle ] }
        freyaMachine { 
            including authMachine
            methods [ POST; HEAD; OPTIONS ]
            doPost (eventPosted freyaCmd)
            handleOk (view getState)
        }
    
    let updateDoc getState eventPosted =
        let freyaCmd = freya { let! newDoc = Interpret.json
                               return [ WritingStuff.UpdateDoc newDoc ] }
        freyaMachine { 
            including authMachine
            methods [ POST; HEAD; OPTIONS ]
            doPost (eventPosted freyaCmd)
            handleOk (view getState)
        }
    
    let submitStuff getState eventPosted =
        let freyaCmd = freya { return [ WritingStuff.Submit ] }
        freyaMachine { 
            including authMachine
            methods [ POST; HEAD; OPTIONS ]
            doPost (eventPosted freyaCmd)
            handleOk (view getState)
        }

let root store =
    let getState = WritingStuffApi.getState store
    let eventPosted = WritingStuffApi.eventsPosted getState store
    freyaRouter { 
        resource "/stuff/{stuffId}" (WritingStuffApi.getStuff getState)
        resource "/stuff/{stuffId}/create" 
            (WritingStuffApi.createStuff getState eventPosted)
        resource "/stuff/{stuffId}/updateTitle" 
            (WritingStuffApi.updateTitle getState eventPosted)
        resource "/stuff/{stuffId}/updateDoc" 
            (WritingStuffApi.updateDoc getState eventPosted)
        resource "/stuff/{stuffId}/submit" 
            (WritingStuffApi.submitStuff getState eventPosted)
    }
