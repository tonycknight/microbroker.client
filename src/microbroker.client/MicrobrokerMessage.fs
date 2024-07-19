﻿namespace microbroker.client

open System

[<CLIMutable>]
type MicrobrokerMessage =
    { messageType: string
      content: string
      created: DateTimeOffset
      active: DateTimeOffset }

module MicrobrokerMessages =
    let internal fromString (value: string) =
        try
            Newtonsoft.Json.JsonConvert.DeserializeObject<MicrobrokerMessage>(value) |> Some
        with ex ->
            None

    let internal toEntity<'a> (message: MicrobrokerMessage) =
        try
            Newtonsoft.Json.JsonConvert.DeserializeObject<'a>(message.content) |> Some
        with ex ->
            None

    let internal fromEntity (messageType: string) (message: 'a) =
        let j = Newtonsoft.Json.JsonConvert.SerializeObject(message)

        { MicrobrokerMessage.messageType = messageType
          content = j
          created = DateTimeOffset.UtcNow
          active = DateTimeOffset.MinValue }

    let internal toJson (message: MicrobrokerMessage) =
        Newtonsoft.Json.JsonConvert.SerializeObject(message)

    let internal toJsonArray (messages: MicrobrokerMessage[]) =
        Newtonsoft.Json.JsonConvert.SerializeObject(messages)

    let create () =
        { MicrobrokerMessage.created = Time.now ()
          content = ""
          messageType = ""
          active = DateTimeOffset.MinValue }

    let active (active: DateTimeOffset) (message: MicrobrokerMessage) = { message with active = active }

    let delayed (delay: TimeSpan) (message: MicrobrokerMessage) =
        { message with
            active = Time.now () |> Time.add delay }

    let messageType messageType (message: MicrobrokerMessage) =
        { message with
            messageType = messageType }

    let content content (message: MicrobrokerMessage) = { message with content = content }
