# Time Tracking (`tt`)

The main operation of our Time Tracker is to keep track of *Activities*. An Activity represents a continuous stretch of time that we spent doing something.
When we add an Activity to the Time Tracker we say that we *log* (verb) the Activity. We then refer to the *Log* (noun) as the set of Activities we have logged.

We want to mine the Log to extract insights on how we spend our time. To do so, we want to generate *Reports*. A Report is a summary of the time we spent on various activities during a *Period* (e.g., day, week, work-week). To make our Reports more useful, we use *Keywords* to annotate activities and leverage Keywords to filter and aggregate Activities.[^1]

[^1]: Reporting is limited to total tracked time in the current week for release 1.

### CLI Example

To input activities in the Time Tracking, you can specify the start and end of an activity.
The activity is made up of keywords that will be used for reporting and filtering.

```
$ tt start yoga
Starting yoga at 9:00

$ tt end
Ending yoga at 9:20 (elapsed 00:20)

$ tt start work emails
Starting work emails at 9:30

$ tt end
Ending work emails at 10:00 (elapsed 00:30)
```

You can also omit the start of the activity for contiguous activities:

```
$ tt start coffee-break
Ending work emails at 10:05 (elapsed 00:05)
Starting coffee-break
```

To display all entries for the day we use `show`:

```
$ tt show
 09:00 - 09:20 : Yoga
 09:30 - 10:00 : Work Emails
 10:00 - 10:05 : Coffee Break
 10:05 - 11:55 : Pair with John Issue-37
 11:55 - 12:10 : Pairing follow-up
 12:15 - 12:35 : Work Emails
 12:35 - >>>>> : Lunch
 Total: 03:20
```

The last entry is on-going, so the end-time is not included.

At the end of the week (day or month), we can extract a report. In the current version the report only shows the total for the current week (Mon-Sun):

```
$ tt report
Weekly Total: 00:35
```

Logs are stored under `~/.tt/` (configurable via `TIMETRACKER_DIR` env) one file per day using the following CSV structure:
```
start, end, description
```

where:
* `start` and `end` are timestamps in ISO-8601 format
* `end` might be empty
* `description` is a quoted string

Our examples would look like (omitting milliseconds and tzinfo):

```csv
start               , end                 , description
2022-01-22T09:00:00,2022-01-22T09:20:00,"yoga"
2022-01-22T09:30:00,2022-01-22T10:00:00,"work emails"
2022-01-22T10:00:00,2022-01-22T10:05:00,"coffee break"
2022-01-22T10:05:00,2022-01-22T11:55:00,"pair with John Issue-37"
2022-01-22T11:55:00,2022-01-22T12:10:00,"pairing follow-up"
2022-01-22T12:15:00,2022-01-22T12:35:00,"work emails"
2022-01-22T12:35:00,,"lunch"
```

NOTE: There are a few ambiguities around CSV and delimiters that tend to be implementation dependent (how to represent line breaks, quotes etc.) As our descriptions are meant to be rather simple, we are not going to worry about them for now.

## Installation

* SDK 6.0 (`sudo apt install dotnet-sdk-6.0`)
* Build the release: `dotnet build --configuration Release`