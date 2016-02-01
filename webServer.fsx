#r "paket-files/devcrafting/DDay.iCal/DDay.iCal/bin/Debug/Dday.iCal.dll"
#r "packages/Suave/lib/net40/suave.dll"
#r "packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#r "packages/FAKE/tools/FakeLib.dll"
#load "calendrier.fs"

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Writers
open Suave.Successful
open Calendars
open System

// TODO : allow several activities
open Newtonsoft.Json
open System.IO
let activities dataDir =
    let serializerSettings = new JsonSerializerSettings()
    serializerSettings.ContractResolver <- new Serialization.CamelCasePropertyNamesContractResolver()
    JsonConvert.DeserializeObject<Activity list>(File.ReadAllText(Path.Combine(dataDir,"devcrafting.json")))

let mergedCalendar logger activities =
    Log.verbose logger "" Logging.TraceHeader.empty "Merging calendars"
    activities |> exportMergedCalendars 
    Files.file "mergedCalendar.ics"

let days logger activities =
    Log.verbose logger "" Logging.TraceHeader.empty "Retrieving days..."
    activities
    |> Seq.map (fun a -> retrieveDaysForYear 2016 a)
    |> Seq.collect id

type SynthesysData = {
    ActivitiesByMonth: ActivityByMonth seq
    Months: string seq
    Totals: float seq 
}

let synthesisData logger days =
    Log.verbose logger "" Logging.TraceHeader.empty "SynthesisData"
    let minDay = days |> Seq.minBy (fun o -> o.Day)
    let maxDay = days |> Seq.maxBy (fun o -> o.Day)
    let activitiesByMonth =
        days
        |> Seq.groupBy (fun a -> a.Activity)
        |> getActivitiesByMonth minDay maxDay
        |> Seq.toList
    let months =
        activitiesByMonth
        |> Seq.take 1 
        |> Seq.collect (fun x -> x.ManDaysByMonth)
        |> Seq.map (fun x -> x.Month)
    let totals =
        activitiesByMonth
        |> Seq.collect (fun x -> x.ManDaysByMonth)
        |> Seq.groupBy (fun x -> x.Month) 
        |> Seq.map (fun x -> snd x |> Seq.sumBy (fun x -> x.ManDays))
    { ActivitiesByMonth = activitiesByMonth; Months = months; Totals = totals } 

open Newtonsoft.Json
let JSON v =
    let serializerSettings = new JsonSerializerSettings()
    serializerSettings.ContractResolver <- new Serialization.CamelCasePropertyNamesContractResolver()
    JsonConvert.SerializeObject(v, serializerSettings)
    |> OK
    >=> setMimeType "application/json; charset=utf-8"

// TODO : append HTML with binding on JSON data
let app : WebPart =
    let dataDir = Fake.EnvironmentHelper.getBuildParamOrDefault "data_dir" (__SOURCE_DIRECTORY__ + "\data")
    choose 
        [ path "/" >=> Files.file "public/default.html" 
          path "/mergedCalendar" >=> context (fun context -> (activities dataDir) |> (mergedCalendar context.runtime.logger))
          path "/synthesis" >=> context (fun context -> (activities dataDir) |> days context.runtime.logger |> synthesisData context.runtime.logger |> JSON)
          Files.browseHome ]

let mimeTypes =
    defaultMimeTypesMap
        @@ (function | ".ics" -> mkMimeType "text/calendar" false | _ -> None)

open Fake

let config = 
    let port = int (getBuildParamOrDefault "port" "8083")    
    { defaultConfig with
        homeFolder = Some __SOURCE_DIRECTORY__
        logger = Logging.Loggers.saneDefaultsFor Logging.LogLevel.Verbose
        bindings = [ HttpBinding.mkSimple HTTP "127.0.0.1" port ]
        mimeTypesMap = mimeTypes }

Target "run" (fun _ ->
    startWebServer config app
)

RunTargetOrDefault "run"