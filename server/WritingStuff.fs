module WritingStuff

open System
open Auth

type InProgress =
    { title : Option<string>
      document : Option<string>
      owner : User }
    static member Create u =
        { title = None
          document = None
          owner = u }

type Complete =
    { title : string
      document : string
      owner : User }

type State =
    | Empty
    | Writing of InProgress
    | Finished of Complete

type Command =
    | Create of User
    | UpdateTitle of string
    | UpdateDoc of string
    | UpdateBoth of string * string
    | Submit

type Event =
    | Created of User
    | TitleUpdated of string
    | DocUpdated of string
    | InvalidCommandForState of State * Command
    | Submitted

let apply state command =
    match command with
    | Create user -> 
        match state with
        | Empty -> [ Created user ]
        | _ -> [ InvalidCommandForState(state, command) ]
    | UpdateTitle newTitle -> 
        match state with
        | Writing inProgress -> [ TitleUpdated newTitle ]
        | _ -> [ InvalidCommandForState(state, command) ]
    | UpdateDoc newDoc -> 
        match state with
        | Writing inProgress -> [ DocUpdated newDoc ]
        | _ -> [ InvalidCommandForState(state, command) ]
    | UpdateBoth(newTitle, newDoc) -> 
        match state with
        | Writing inProgress -> 
            [ TitleUpdated newTitle
              DocUpdated newDoc ]
        | _ -> [ InvalidCommandForState(state, command) ]
    | Submit -> 
        match state with
        | Writing { title = Some _; document = Some _ } -> [ Submitted ]
        | _ -> [ InvalidCommandForState(state, command) ]

let fold state evt =
    match evt with
    | Created u -> Writing(InProgress.Create u)
    | TitleUpdated t -> 
        match state with
        | Writing inProgress -> Writing { inProgress with title = Some t }
        | _ -> 
            eprintfn "Invalid event for state in store"
            state
    | DocUpdated d -> 
        match state with
        | Writing inProgress -> Writing { inProgress with document = Some d }
        | _ -> 
            eprintfn "Invalid event for state in store"
            state
    | InvalidCommandForState _ -> state
    | Submitted -> 
        match state with
        | Writing { title = Some t; document = Some d; owner = o } -> 
            Finished { title = t
                       document = d
                       owner = o }
        | _ -> 
            eprintfn "Invalid event for state in store"
            state
