namespace TimeTracker.Tests


open System
open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open TimeTracker.Core

[<TestClass>]
type TestActivity() =

    [<TestMethod>]
    member this.TestMarkComplete() =
        let now = DateTime.Now
        let oneHourAgo = now.Add(new TimeSpan(-1, 0, 0))

        let a =
            { Start = oneHourAgo
              Description = "abc"
              End = None }

        let b = Activity.markComplete a now

        Assert.AreEqual(a.Description, b.Description)
        Assert.AreEqual(a.Start, b.Start)
        Assert.AreEqual(now, b.End.Value)

    [<TestMethod>]
    member this.TestActivityElapsed() =
        let now = DateTime.Now

        let duration =
            Activity.Duration
                { Start = now
                  End = Some (now.AddMinutes(1))
                  Description = "abc" }

        Assert.AreEqual(float (1), duration.TotalMinutes)
        Assert.AreEqual(1, duration.Minutes)

    [<TestMethod>]
    member this.TestActivityToString() =
        let start = DateTime.Parse("2022-01-01 12:13:55")
        let endT = start.AddMinutes(1)
        let a = { Start = start; Description = "abc"; End = Some endT}

        Assert.AreEqual("12:13-12:14 [00:01] : abc", Activity.ToStringWithDuration a)
        Assert.AreEqual("12:13-12:14:abc", Activity.ToStringWithFormat "{start}-{end}:{description}" a)
        Assert.AreEqual("12:13-12:14 : abc", Activity.ToString a)

    [<TestMethod>]
    member this.TestOngoingActivityToString() =
        let start = DateTime.Parse("2022-01-01 12:13:55")
        let a = { Start = start; Description = "abc"; End = None}
        Assert.AreEqual("12:13->>>>> : abc", Activity.ToString a)

        let b = { Start = DateTime.Now.Subtract(TimeSpan(0,1,0)); Description = "abc"; End = None}
        Assert.AreEqual("00:01", Activity.ToStringWithFormat "{duration}" b)

[<TestClass>]
type TestLog() =

    [<TestMethod>]
    member this.TestStartActivity() =
        let now = DateTime.Now

        let newLog =
            { Past = [] }
            |> Log.startActivity { Start = now; Description = "abc"; End = None }

        Assert.AreEqual(0, newLog.Past.Length)
        Assert.AreEqual(now, newLog.Current.Start)
        Assert.AreEqual("abc", newLog.Current.Description)

    [<TestMethod>]
    member this.TestEndActivity() =
        let now = DateTime.Now
        let oneHourAgo = now.Add(new System.TimeSpan(-1, 0, 0))

        let newLog =
            { Past = [] }
            |> Log.startActivity { Start = now; Description = "abc"; End = None }
            |> Log.endCurrentActivity oneHourAgo

        Assert.AreEqual(1, newLog.Past.Length)
        Assert.AreEqual(now, newLog.Past.Head.Start)
        Assert.AreEqual(oneHourAgo, newLog.Past.Head.End.Value)
        Assert.AreEqual("abc", newLog.Past.Head.Description)

    [<TestMethod>]
    member this.TestActivityOrder() =
        let now = DateTime.Now
        let oneHourAgo = now.Add(new System.TimeSpan(-1, 0, 0))

        let newLog =
            { Past = [] }
            |> Log.startActivity
                { Start = oneHourAgo; Description = "abc"; End = None }
            |> Log.endCurrentActivity now
            |> Log.startActivity { Start = now; Description = "def"; End = None }
            |> Log.endCurrentActivity (now.Add(new TimeSpan(10)))

        Assert.AreEqual(2, newLog.Past.Length)
        Assert.AreEqual("abc", newLog.Past.Head.Description)
        Assert.AreEqual("def", newLog.Past.Item(1).Description)

    [<TestMethod>]
    member this.TestTotalTime() =
        let now = DateTime.Now
        let oneHourAgo = now.Add(new TimeSpan(-1, 0, 0))

        let log1 = { Past = [] }

        let log2 =
            log1
            |> Log.startActivity
                { Start = oneHourAgo; Description = "abc"; End = None }

        let log3 = log2 |> Log.endCurrentActivity now

        Assert.AreEqual(TimeSpan.Zero, Log.TotalTime(Closed log1))
        Assert.AreEqual(TimeSpan.Zero, Log.TotalTime(Active log2))
        Assert.AreEqual(TimeSpan(1, 0, 0), Log.TotalTime(Closed log3))

[<TestClass>]
type TestReport() =

    // oneHourLog returns a Log with a single 1 hour activity for the day
    let oneHourLog (day: DateTime) =
        Closed(
            { Past = [] }
            |> Log.startActivity
                { Start = day.AddHours(1); Description = ""; End = None }
            |> Log.endCurrentActivity (day.AddHours(2))
        )


    [<TestMethod>]
    member this.TestWeekOf() =
        let monday = System.DateTime(2022, 5, 2).Date
        let wednesday = monday.AddDays(2)
        let sunday = monday.AddDays(6)
        let nextMonday = monday.AddDays(7)

        let startDate = monday
        let endDate = monday.AddDays(6)

        Assert.AreEqual((monday, sunday), Report.weekOf monday)
        Assert.AreEqual((monday, sunday), Report.weekOf wednesday)
        Assert.AreEqual((monday, sunday), Report.weekOf sunday)
        Assert.AreNotEqual((monday, sunday), Report.weekOf nextMonday)

    [<TestMethod>]
    member this.TestSimpleWeekReport() =
        let today = DateTime(2022, 4, 25)
        let tomorrow = today.AddDays(1)
        let lastWeek = today.AddDays(-7)

        let logs =
            [ oneHourLog today
              oneHourLog tomorrow
              oneHourLog lastWeek ]

        let weekReport = Report.weekReport today logs
        Assert.AreEqual(2, weekReport.TotalTime.Hours)

    [<TestMethod>]
    member this.TestCombineLogs() =
        let today = DateTime(2022, 4, 25)

        let combined =
            Report.combineLogs [ oneHourLog today
                                 oneHourLog (today.AddDays(1))
                                 oneHourLog (today.AddDays(2)) ]

        Assert.AreEqual(3, combined |> Log.Past |> List.length)
