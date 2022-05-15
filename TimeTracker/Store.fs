namespace TimeTracker

open System.IO
open FSharp.Data
open TimeTracker.Core

module CSVStorage =
    [<Literal>]
    let ResolutionFolder = __SOURCE_DIRECTORY__

    type LogFormat = CsvProvider<"../data/example-ongoing.csv", ResolutionFolder=ResolutionFolder>

    let exampleOngoing =
        ResolutionFolder + "/../data/example-ongoing.csv"

    let exampleComplete =
        ResolutionFolder + "/../data/example-complete.csv"

    type Row = LogFormat.Row
    type Rows = seq<Row>

module Storage =
    let parseRows (rows: CSVStorage.Rows) : Activity list =
        let rowToActivity (r: CSVStorage.Row) = { Start = r.Start; End = r.End; Description = r.Description }
        rows
        |> Seq.map rowToActivity
        |> Seq.toList

    let logFromRows rows =
        let activities = parseRows rows
        let current = List.tryFind (fun a -> a.End.IsNone) activities
        let past = List.filter (fun a -> a.End.IsSome) activities
        match current with
        | Some a -> Active {Past = past; Current = a }
        | None -> Closed {Past = past}

    let activityToRow (a: Activity) =
        CSVStorage.LogFormat.Row(a.Start, a.End, a.Description)

    let logToRows log =
        let current = match Log.Current log with
                      | None -> []
                      | Some a -> [a]
        Log.Past log @ current |> List.map activityToRow

    type LoadError =
        | DirectoryNotFound of System.Exception
        | FileNotFound of System.Exception

    let tryLoadCSV (fpath: string) =
        try
            Ok(CSVStorage.LogFormat.Load(fpath).Cache().Rows)
        with
        | :? DirectoryNotFoundException as e -> Error(DirectoryNotFound e)
        | :? FileNotFoundException as e -> Error(FileNotFound e)

    let loadFromCSV (fpath: string) =
        tryLoadCSV fpath |> Result.map logFromRows

    type SaveError =
        | DirectoryNotFound of System.Exception
        | PermissionError of System.Exception

    let trySaveCSV (fpath: string) rows =
        try
            (new CSVStorage.LogFormat(rows)).Save(fpath)
            Ok()
        with
        | :? System.IO.DirectoryNotFoundException as e -> Error(DirectoryNotFound e)
        | :? System.UnauthorizedAccessException as e -> Error(PermissionError e)

    let saveToCSV (fpath: string) log =
        log
        |> logToRows
        |> trySaveCSV fpath

    type EditLogError = string * System.Exception

    let forceCreateDirectory dpath =
        try
            Directory.CreateDirectory dpath |> ignore
            Ok()
        with
        | :? System.UnauthorizedAccessException as e -> Error(PermissionError e)
        | e -> Error(DirectoryNotFound e)

    /// editLog calls editFn after loading the log at fpath.
    /// If editFn returns a log, it will overwrite the one in fpath.
    /// If the file does not exist, a new empty log will be created.
    let editLog fpath editFn : Result<Log, EditLogError> =
        loadFromCSV fpath
        |> function
            | Ok log -> Ok log
            | Error (LoadError.DirectoryNotFound e) ->
                let dpath = Path.GetDirectoryName(fpath)

                match forceCreateDirectory dpath with
                | Ok _ -> Ok(Closed { Past = [] })
                | Error (DirectoryNotFound e) ->
                    Error("Failed to load log file. Check that the target directory {dpath} exists", e)
                | Error (PermissionError e) ->
                    Error("Failed to load log file. Permission error on target directory {dpath}", e)
            | Error (FileNotFound e) -> Ok(Closed { Past = [] })

        |> Result.bind (fun oldLog ->
            editFn oldLog
            |> function
                | Some newLog ->
                    saveToCSV fpath newLog
                    |> function
                        | Ok _ -> Ok newLog
                        | Error (DirectoryNotFound e)
                        | Error (PermissionError e) -> Error("Failed to save log file", e)
                | None -> Ok oldLog)


    let loadAllLogs dpath =
        // TODO: Rewrite to avoid mutable state.
        let fpaths = Directory.GetFiles(dpath) // NOTE: This can throw
        let mutable logs = []
        let mutable error = ""
        for fpath in fpaths do
            let log = loadFromCSV fpath
            match log with
            | Ok log -> logs <- log :: logs
            | Error _ -> error <- "failed loading"
        match error with
            | "" -> Ok logs
            | _ -> Error error