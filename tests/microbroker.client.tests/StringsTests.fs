namespace microbroker.client.tests

open System
open microbroker.client
open FsCheck
open FsCheck.Xunit

module StringsTests =
    [<Property(Verbose = true)>]
    let ``toGzip returns byte array`` (value: NonEmptyString) =
        let r = Strings.toGzip value.Get

        r.Length > 0

    [<Property(Verbose = true)>]
    let ``toGzip/fromGzip is symmetric`` (value: NonEmptyString) =
        let toStream (bytes: byte[]) = new System.IO.MemoryStream(bytes)
        let f = Strings.toGzip >> toStream >> Strings.fromGzip

        f value.Get = value.Get
