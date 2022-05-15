namespace TimeTracker.Core

//
// Activity
//
// An Activity is a single continuous amount of time spent performing something.
//
// On-going Activities do not have an End time.
//
type Activity =
    { Start: System.DateTime
      Description: string
      End: System.DateTime option}

module Activity =
    // Duration returns the duration of the activity. If the activity is ongoing (does not have an end time),
    // we use the current time.
    let Duration a = match a.End with
                     | Some t -> t.Subtract a.Start
                     | None -> System.DateTime.Now.Subtract a.Start

    let ToStringWithFormat (fmt: string) (a: Activity) =
        let timeToStr (t: System.DateTime) = t.ToString("HH:mm")
        let durationToStr (d: System.TimeSpan) = d.ToString(@"hh\:mm")

        let fmtToIndex =
            fmt
                .Replace("{start}", "{0}")
                .Replace("{end}", "{1}")
                .Replace("{duration}", "{2}")
                .Replace("{description}", "{3}")

        let startTime = timeToStr a.Start
        let endTime =
            match a.End with
            | Some t -> timeToStr t
            | None -> ">>>>>"

        let desc = a.Description
        let duration = Duration a |> durationToStr

        System.String.Format(fmtToIndex, startTime, endTime, duration, desc)

    let ToString =
        ToStringWithFormat "{start}-{end} : {description}"

    let markComplete activity endTime =
        {activity with End = Some endTime}

    let ToStringWithDuration =
        ToStringWithFormat "{start}-{end} [{duration}] : {description}"
