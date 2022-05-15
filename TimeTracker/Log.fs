namespace TimeTracker.Core

//
// Log
//
// A Log collects a sequence of activities.
//
// An Active Log is one in which there is an OnGoing activity, while a ClosedLog
// only has completed activities.
//
type ActiveLog =
    { Current: Activity
      Past: Activity list }

type ClosedLog = { Past: Activity list }

type Log =
    | Active of ActiveLog
    | Closed of ClosedLog

module Log =

    let endCurrentActivity time (log: ActiveLog) =
        { Past =
            log.Past
            @ [ Activity.markComplete log.Current time ] }

    let startActivity activity log = { Current = activity; Past = log.Past }

    let fold onActive onClosed log =
        match log with
        | Active l -> onActive l
        | Closed l -> onClosed l

    let Past =
        function
        | Active l -> l.Past
        | Closed l -> l.Past

    let Last log = log |> Past |> List.last

    let Current =
        function
        | Active l -> Some l.Current
        | Closed _ -> None

    let ToStringWith activityToStr log : string =
        let current =
            match Current log with
            | Some a -> [ a ]
            | None -> []

        (Past log) @ current
        |> List.map activityToStr
        |> String.concat "\n"

    let ToString =
        ToStringWith(Activity.ToStringWithFormat "{start}-{end} [{duration}] : {description}")

    let TotalTime log =
        Past log
        |> List.fold (fun tot a -> (Activity.Duration a) + tot) System.TimeSpan.Zero

    let Empty = Closed { Past = [] }
