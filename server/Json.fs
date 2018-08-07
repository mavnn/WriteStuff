module Json

open System
open System.IO
open Freya.Core
open Freya.Machines.Http
open Freya.Types.Http
open Freya.Optics.Http
open Hopac
open Newtonsoft.Json

let jsonConverter = Fable.JsonConverter() :> JsonConverter

module Represent =
    let json<'a> (value : 'a) =
        let data =
            JsonConvert.SerializeObject(value, [| jsonConverter |]) 
            |> Text.UTF8Encoding.UTF8.GetBytes
        
        let desc =
            { Encodings = None
              Charset = Some Charset.Utf8
              Languages = None
              MediaType = Some MediaType.Json }
        { Data = data
          Description = desc }

module Interpret =
    let private readStream (s : Stream) =
        job { 
            use reader = new StreamReader(s, Text.Encoding.UTF8)
            return! reader.ReadToEndAsync |> Job.fromTask
        }
    
    let json<'a> =
        freya { 
            let! bodyStream = Freya.Optic.get Request.body_
            let! bodyString = readStream bodyStream |> Freya.fromJob
            let bodyJson =
                JsonConvert.DeserializeObject<'a>
                    (bodyString, [| jsonConverter |])
            return bodyJson
        }
        |> Freya.memo
