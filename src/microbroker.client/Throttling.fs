namespace microbroker.client

open System
open System.Threading.Tasks

type internal WindowThrottlingCounts = Map<DateTime, int>

module internal Throttling =

    let windowThrottling (window: int, maxCount: int) (counts: WindowThrottlingCounts) (current: DateTime) =
        if window < 1 || window > 60 then
            invalidArg "window" "window out of range."

        if 60 % window > 0 then
            invalidArg "window" "window must be a divisor of 60."

        if maxCount < 1 then
            invalidArg "maxCount" "maxCount out of range."

        // put current time into the window
        let secs = int current.TimeOfDay.Seconds
        let bucket = (int (secs / window)) * window

        let key =
            new DateTime(current.Year, current.Month, current.Day, current.Hour, current.Minute, bucket)

        match counts |> Map.tryFind key with
        | None ->
            let map = Map.empty |> Map.add key 1
            (map, TimeSpan.Zero)
        | Some x when (x + 1 <= maxCount) ->
            let map = counts |> Map.add key (x + 1)
            (map, TimeSpan.Zero)
        | Some x ->
            let wait = (bucket + window) - secs |> TimeSpan.FromSeconds
            (counts, wait)

    let exponentialWait duration func =
        let finish = DateTime.UtcNow + duration
        let mutable wait = TimeSpan.FromMilliseconds 100.

        let rec loop iteration =
            task {
                match! func () with
                | Some r -> return Some r
                | _ ->
                    let now = DateTime.UtcNow

                    if now >= finish then
                        return None
                    else
                        wait <- wait + wait
                        do! Task.Delay wait
                        return! loop (iteration + 1.)
            }

        loop 0.
