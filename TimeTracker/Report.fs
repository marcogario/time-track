namespace TimeTracker.Core

// A Report contains statistical/summary data for various activities within
// a log.
//
// To keep the implementation simple, we define a few functions to combine Logs.
// These make it possible to convert all reporting operations into operations
// that work on one Log at the time, making filtering operations simpler.

// For now we keep the Report simple and only show the total tracked time.
// We want to extend this to include summaries by keywords, grouping and extra filtering.
type Report =
    { StartDate: System.DateTime
      EndDate: System.DateTime
      TotalTime: System.TimeSpan }

module Report =
    let ensureClosedLogNow log =
        match log with
        | Active log -> log |> Log.endCurrentActivity System.DateTime.Now
        | Closed log -> log

    let combineTwoLogs logA logB =
        let clogA = logA |> ensureClosedLogNow
        let clogB = logB |> ensureClosedLogNow
        let allActivities = clogA.Past @ clogB.Past
        // NOTE: We sort the activities to make it easier to filter across periods of time.
        // It also is a nice property that the Log is sorted.
        // However, since we combine two arbitrary logs, we might end up with
        // overlapping activities. Right now this is not a problem, but it might
        // be in the future, and we could add a way to check that activities are
        // non-overlapping.
        let allSortedActivities =
            allActivities |> List.sortBy (fun a -> a.Start)

        Closed { Past = allSortedActivities }

    let combineLogs = List.fold combineTwoLogs Log.Empty

    let activitiesWithinDates (startTime, endTime) (aList: Activity list) =
        aList
        // TODO: Would be nice to avoid the .Value here that can throw a null pointer.
        //       At this point, we know all activities have an endtime, so it would be great to encode
        //       this assumption in the type of aList
        |> List.filter (fun a -> a.Start.Date >= startTime && a.End.Value <= endTime)

    // weekOf returns the start and end date for the week that contains the day in date.
    // In this implementation, weeks start on a Monday.
    // E.g.,
    //   weekOf 2022-05-01 = (2022-04-25, 2022-04-01)    // Sunday May 1st -> Monday April 25th
    let weekOf (date: System.DateTime) =
        let dow = date.DayOfWeek
        // Start the week from Monday, not Sunday
        // In DayOfWeek, Sunday = 0 and Monday = 1
        // instead we want Monday = 0 and Sunday = 6
        let dow =
            if dow = System.DayOfWeek.Sunday then
                6
            else
                (int) dow - 1

        let startDate = date.AddDays(-dow).Date
        let endDate = startDate.AddDays(6).Date
        (startDate, endDate)

    let createForPeriod (startDate, endDate) log =
        let duration =
            Log.Past log
            |> activitiesWithinDates (startDate, endDate)
            |> List.fold (fun tot a -> (Activity.Duration a) + tot) System.TimeSpan.Zero

        { StartDate = startDate
          EndDate = endDate
          TotalTime = duration }

    // weekReport returns the report for the week including today (Mon-Sun)
    // This function takes in input a list of Logs, so the caller should ensure that
    // all relevent logs for the week are given.
    let weekReport date =
        combineLogs
        >> createForPeriod (weekOf date)
