namespace microbroker.client

open System

type MicrobrokerConfiguration =
    { brokerBaseUrl: string
      throttleMaxTime: TimeSpan }
