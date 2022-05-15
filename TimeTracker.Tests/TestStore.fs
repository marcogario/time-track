namespace TimeTracker.Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open TimeTracker.Core
open TimeTracker.Storage

[<TestClass>]
type TestStorage() =
    let loadExample name =
        loadFromCSV (__SOURCE_DIRECTORY__ + "/../data/" + name + ".csv")

    let loadOngoingExample = loadExample "example-ongoing"
    let loadClosedExample = loadExample "example-complete" // TODO: Rename to closed

    let AssertNoError res =
        match res with
        | Error e -> Assert.Fail(e.ToString())
        | Ok _ -> ()

    [<TestMethod>]
    member this.TestLoadCSV() =
        loadOngoingExample
        |> Result.map (fun log ->
            match log with
            | Closed _ -> Assert.Fail "Expected Active log"
            | Active l ->
                Assert.AreEqual(6, l.Past.Length)
                Assert.AreEqual("lunch", l.Current.Description))
        |> AssertNoError

        loadClosedExample
        |> Result.map (fun log ->
            match log with
            | Active _ -> Assert.Fail "Expected closed log"
            | Closed l ->
                Assert.AreEqual(7, l.Past.Length)
                Assert.AreEqual("lunch", (Log.Last log).Description))
        |> AssertNoError

    [<TestMethod>]
    member this.TestRoundTripCSV() =
        let examples =
            [ loadClosedExample
              loadOngoingExample ]

        for example in examples do
            let tempPath = Path.GetTempFileName()

            example
            |> Result.bind (fun original ->
                saveToCSV tempPath original |> AssertNoError

                loadFromCSV tempPath
                |> Result.bind (fun reloaded -> Ok(Assert.AreEqual(original, reloaded))))
            |> AssertNoError

            File.Delete tempPath // TODO: Ensure clean-up

    [<TestMethod>]
    member this.TestLoadForEdit() =
        let tempPath = Path.GetTempFileName()

        loadOngoingExample
        |> Result.map (fun l -> saveToCSV tempPath l)
        |> AssertNoError

        editLog tempPath (fun log ->
            File.Delete tempPath
            Assert.IsFalse(File.Exists tempPath)
            None) // As we do not return a log, the file will not be recreated
        |> AssertNoError

        Assert.IsFalse(File.Exists tempPath)


        loadOngoingExample
        |> Result.map (fun l -> saveToCSV tempPath l)
        |> AssertNoError

        editLog tempPath (fun log ->
            File.Delete tempPath
            Assert.IsFalse(File.Exists tempPath)

            match log with
            | Active l -> Some(Closed(Log.endCurrentActivity System.DateTime.Now l))
            | Closed l ->
                Some(
                    Active(
                        Log.startActivity
                            { Start = System.DateTime.Now
                              Description = "abc"
                              End = None}
                            l
                    )
                ))
        |> AssertNoError

        // The file is recreated because the log changed.
        Assert.IsTrue(File.Exists tempPath)
