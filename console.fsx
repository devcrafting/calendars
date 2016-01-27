#r "Dday.iCal.dll"
#load "calendrier.fs"

open Calendars

let prettyPrint printedActivities =
    let width = 7
    let widthTotal = 10
    let prettyPrintHeader printedActivities =
        printf "|%-*s|" (width+8) "Activité"
        printedActivities 
        |> Seq.take 1 
        |> Seq.collect (fun x -> x.ManDaysByMonth) 
        |> Seq.iter (fun x -> printf "%*s|" width x.Month)
        printfn "|%*s|%*s|" widthTotal "Total jh" widthTotal "Total CA"

    let dashes = [1..width] |> Seq.fold (fun x y -> x + "-") "" 
    let prettyPrintSeparator printedActivities =
        printf "|%-*s|" (width+8) dashes
        let nbColumns =
            printedActivities 
            |> Seq.take 1 
            |> Seq.collect (fun x -> x.ManDaysByMonth) 
            |> Seq.length
        [1..nbColumns] |> Seq.iter (fun x -> printf "%*s|" width dashes)
        printfn "|%*s|%*s|" widthTotal dashes widthTotal dashes

    let prettyFormatFloat float =
        if float = 0. then " -  "
        else sprintf "%.3f" float
    let prettyPrintLine printedActivity =
        printf "|%-*s|" (width+8) printedActivity.Activity
        printedActivity.ManDaysByMonth |> Seq.iter (fun x -> printf "%*s|" width (prettyFormatFloat x.ManDays))
        printfn "|%*s|%*s|" widthTotal (prettyFormatFloat printedActivity.TotalManDays) widthTotal (prettyFormatFloat printedActivity.TotalRevenue)

    let prettyPrintTotal printedActivities =
        printf "|%-*s|" (width+8) "TOTAL"
        printedActivities 
        |> Seq.collect (fun x -> x.ManDaysByMonth) 
        |> Seq.groupBy (fun x -> x.Month) 
        |> Seq.iter (fun x -> printf "%*.3f|" width (snd x |> Seq.sumBy (fun x -> x.ManDays)))
        printfn "|%*.3f|%*.3f|" widthTotal (printedActivities |> Seq.sumBy (fun x -> x.TotalManDays)) widthTotal (printedActivities |> Seq.sumBy (fun x -> x.TotalRevenue))

    prettyPrintHeader printedActivities
    prettyPrintSeparator printedActivities
    printedActivities |> Seq.iter prettyPrintLine
    prettyPrintSeparator printedActivities
    prettyPrintTotal printedActivities

let displaySynthesis days =
    printfn "=========== SYNTHESIS =========="
    let minDay = days |> Seq.minBy (fun o -> o.Day)
    let maxDay = days |> Seq.maxBy (fun o -> o.Day)
    days
    |> Seq.groupBy (fun a -> a.Activity)
    |> getActivitiesByMonth minDay maxDay
    |> prettyPrint

let displayOverloadedDays days =
    printfn "=========== OVERLOADED DAYS TO CHECK =========="
    if days |> Seq.length = 0 then printfn "NONE ! GREAT !" 
    else
        days
        |> Seq.groupBy (fun x -> x.Day)
        |> Seq.filter (fun x -> 
                        (snd x) |> Seq.forall (fun x -> not (x.Activity.Name.Contains("#overloadChecked")))
                        && (snd x) |> Seq.sumBy (fun y -> y.Hours) > 8.0)
        |> Seq.iter (fun x -> 
                        printfn "%s" ((fst x).ToString())
                        snd x |> Seq.iter (fun y -> printfn "\t- %s (%.3f)" y.Activity.Name y.Hours))
    days
