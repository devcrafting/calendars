module Calendars

open DDay.iCal
open System

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

let retrieveDays startDate endDate activity =
    let iCalendar = iCalendar.LoadFromUri(activity.CalendarUri)
    iCalendar.[0].Events |> Seq.map (findDaysForEvent startDate endDate activity) |> Seq.collect id

let retrieveDaysForYear year activity =
    let startDate = new DateTime(year, 1, 1, 0, 0, 0)
    let endDate = new DateTime(year, 12, 31, 23, 59, 0)
    retrieveDays startDate endDate activity

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

let mergeCalendars (calendars: IICalendar seq) : iCalendar =
    let calendar = new iCalendar()
    calendars |> Seq.fold (fun (calendar:iCalendar) c -> 
                                calendar.MergeWith(c)
                                calendar) calendar

let getOverloadedDays days =
    days
    |> Seq.groupBy (fun x -> x.Day)
    |> Seq.filter (fun x -> 
        (snd x) |> Seq.forall (fun x -> not (x.Activity.Name.Contains("#overloadChecked")))
        && (snd x) |> Seq.sumBy (fun y -> y.Hours) > 8.0)
    |> Seq.sortBy fst

open DDay.iCal.Serialization.iCalendar

let exportMergedCalendars activities = 
    let mergedCalendar = 
        activities
        |> Seq.map (fun a -> iCalendar.LoadFromUri(a.CalendarUri))
        |> Seq.collect id
        |> Seq.fold (fun (calendar:iCalendar) c -> 
                                    calendar.MergeWith(c)
                                    calendar) (new iCalendar())
    let serializer = new iCalendarSerializer();
    serializer.Serialize(mergedCalendar, "mergedCalendar.ics");
