#r "packages/Ical.Net/lib/net46/Ical.Net.Collections.dll"
#r "packages/Ical.Net/lib/net46/Ical.Net.dll"

open Ical.Net
open Ical.Net.DataTypes
open Ical.Net.Interfaces
open Ical.Net.Interfaces.Components
open Ical.Net.Serialization.iCalendar.Serializers

open System
open System.IO

type Activity = {
    Name: string
    DayRate: float
    CalendarUri: Uri
}
and Occupation = {
    Activity: Activity
    Day: DateTime
    Hours: float
}

let workingHoursByDay (event: IEvent) (occurrence:Occurrence) activity =
    if event.IsAllDay then
        [0..(int event.Duration.TotalDays - 1)]
            |> Seq.map (fun x -> { Day = occurrence.Period.StartTime.Date.AddDays(float x); Hours = 8. ; Activity = activity })
    else
        Seq.singleton { Day = occurrence.Period.StartTime.Date; Hours = event.Duration.TotalHours; Activity = activity }

let findDaysForEvent (startDate:DateTime) endDate subject (event: IEvent) =
    event.GetOccurrences(startDate, endDate)
        |> Seq.map (fun o -> workingHoursByDay event o subject)
        |> Seq.collect id

let retrieveDays startDate endDate activity dataDir =
    let iCalFile = Path.Combine(dataDir, activity.Name + ".ics")
    let calendars = Calendar.LoadFromFile(iCalFile)

    calendars 
    |> Seq.collect (fun c -> c.Events) 
    |> Seq.map (findDaysForEvent startDate endDate activity)
    |> Seq.collect id

let retrieveDaysForYear year activity dataDir =
    let startDate = new DateTime(year, 1, 1, 0, 0, 0)
    let endDate = new DateTime(year, 12, 31, 23, 59, 0)
    retrieveDays startDate endDate activity dataDir

type ActivityByMonth = {
    Activity: string
    ManDaysByMonth: DaysForMonth seq
    TotalManDays: float
    TotalRevenue: float
}
and DaysForMonth = {
    Month: string
    ManDays: float
}

let computeWorkingManDaysByMonth minDay maxDay days =
    let workingDaysByMonth =
        days
        |> Seq.filter (fun x -> x.Day.DayOfWeek <> DayOfWeek.Saturday && x.Day.DayOfWeek <> DayOfWeek.Sunday) 
        |> Seq.groupBy (fun o -> o.Day.Month)
        |> Seq.map (fun x -> { Month = fst x |> string; ManDays = snd x |> Seq.sumBy (fun x -> x.Hours / 8.0) })
    [minDay.Day.Month..maxDay.Day.Month]
    |> Seq.map (fun m -> 
        match workingDaysByMonth |> Seq.tryFind (fun x -> x.Month = string m) with
        | Some x -> x
        | None -> { Month = string m; ManDays = 0. })

let getActivitiesByMonth minDay maxDay (activities: (Activity * Occupation seq) seq) =
    activities 
    |> Seq.map (fun a -> 
        let workingManDaysByMonth = snd a |> computeWorkingManDaysByMonth minDay maxDay
        let workingManDays = workingManDaysByMonth |> Seq.sumBy (fun x -> x.ManDays)
        { 
            Activity = (fst a).Name
            ManDaysByMonth = workingManDaysByMonth
            TotalManDays = workingManDays
            TotalRevenue = workingManDays * (fst a).DayRate 
        })

let mergeCalendars (calendars: ICalendar seq) : Calendar =
    let calendar = new Calendar()
    calendars |> Seq.fold (fun (calendar:Calendar) c -> 
                                calendar.MergeWith(c)
                                calendar) calendar

let getOverloadedDays days =
    days
    |> Seq.groupBy (fun x -> x.Day)
    |> Seq.filter (fun x -> 
        (snd x) |> Seq.forall (fun x -> not (x.Activity.Name.Contains("#overloadChecked")))
        && (snd x) |> Seq.sumBy (fun y -> y.Hours) > 8.0)
    |> Seq.sortBy fst

(*let exportMergedCalendars activities = 
    let mergedCalendar = 
        activities
        |> Seq.map (fun a -> Calendar.LoadFromUri(a.CalendarUri))
        |> Seq.collect id
        |> Seq.fold (fun (calendar:Calendar) c -> 
                                    calendar.MergeWith(c)
                                    calendar) (new Calendar())
    let serializer = new CalendarSerializer();
    serializer.Serialize(mergedCalendar, "mergedCalendar.ics");*)
