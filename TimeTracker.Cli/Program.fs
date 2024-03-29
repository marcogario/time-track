open System
open System.IO
open TimeTracker.Storage
open TimeTracker.Core

module Cli =
    open System.Diagnostics

    let DEFAULT_STORE_DIR =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".tt")

    let STORE_DIR =
        match Environment.GetEnvironmentVariable("TIMETRACKER_DIR") with
        | null -> DEFAULT_STORE_DIR
        | v -> v

    let logPath (date: DateTime) =
        let dateStr = date.ToString "yyyyMMdd"
        Path.Combine(STORE_DIR, dateStr + ".csv")

    let todayLog = logPath DateTime.Today

    // Returns the current time truncated to the minute
    let timeNow =
        let truncate = fun (d: DateTime) -> d.AddTicks(-(d.Ticks % TimeSpan.TicksPerMinute))
        truncate DateTime.Now

    let endCurrentActivityIfAny log : ClosedLog =
        log
        |> Log.fold
            (fun l ->
                let newLog = l |> Log.endCurrentActivity timeNow
                let a = Log.Last(Closed newLog)
                printf "End: %s" a.Description
                printf " (elapsed %s)\n" ((Activity.Duration a).ToString("hh\\:mm"))
                newLog)
            id

    let start desc log =
        Active(
            log
            |> endCurrentActivityIfAny
            |> Log.startActivity
                { Start = timeNow
                  Description = desc
                  End = None }
            |> fun log ->
                printf "Start: %s\n" desc
                log
        )

    let editLogErrorToString (msg, e) = msg + "\n" + e.ToString()

    let withTodaysLogDo f =
        f
        |> editLog todayLog
        |> Result.mapError editLogErrorToString

    let startCmd desc =
        withTodaysLogDo (fun log -> Some(start desc log))

    let endCmd () =
        withTodaysLogDo (fun log -> Some(Closed(endCurrentActivityIfAny log)))

    let showCmd () =
        withTodaysLogDo (fun log ->
            printf "%s\n" (Log.ToString log)
            printf "Total: %s\n" ((Log.TotalTime log).ToString("hh\\:mm"))
            None)

    // warnActiveLog prints a warning if the log is Active
    let warnActiveLog (log:Log) =
        match log with
        // TODO: This is reconstructing the file path, but maybe we could fetch those with the logs?
        | Active l -> printf "Warn: Active log: %s\n" (l.Current.Start.Date |> logPath)
        | Closed _ -> ()

    let reportCmd () =
        let printTotal logs =
            let report = Report.weekReport timeNow logs
            let total = report.TotalTime
            printf "Weekly Total: %.0f:%d\n" total.TotalHours total.Minutes

        match loadAllLogs STORE_DIR with
        | Ok logs ->
            Ok(
                logs |> List.map warnActiveLog  |> ignore
                printTotal logs
                Log.Empty
            )
        | Error e -> Error e

    let amendCmd() =
        let mutable p = new Process()
        p.StartInfo.FileName <- todayLog
        p.StartInfo.UseShellExecute <- true
        p.Start() |> ignore
        Ok (Log.Empty)

let errorCheck =
    function
    | Ok _ -> 0
    | Error e ->
        printfn "Error: %s" (e)
        -1

[<EntryPoint>]
let main args =
    if args.Length > 0 then
        match args.[0].ToLower() with
        // TODO: The return type of the Cmd should be Result<unit, string>
        | "start" -> Cli.startCmd (args |> Seq.skip 1 |> String.concat " ")
        | "end" -> Cli.endCmd ()
        | "show" -> Cli.showCmd ()
        | "report" -> Cli.reportCmd ()
        | "amend" -> Cli.amendCmd ()
        | _ -> Error "Must provide one of start|end|show|report|amend"
    else
        Cli.showCmd ()
    |> errorCheck
