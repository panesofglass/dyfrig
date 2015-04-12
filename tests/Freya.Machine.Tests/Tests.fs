﻿//----------------------------------------------------------------------------
//
// Copyright (c) 2014
//
//    Ryan Riley (@panesofglass) and Andrew Cherry (@kolektiv)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//----------------------------------------------------------------------------

module Freya.Machine.Tests

open System
open System.Net.Http
open Microsoft.Owin.Hosting
open Microsoft.Owin.Testing
open NUnit.Framework
open Swensen.Unquote
open Freya.Core

(* Katana

   Katana (Owin Self Hosting) expects us to expose a type with a specific
   method. Freya lets us do see easily, the OwinAppFunc module providing
   functions to turn any Freya<'a> function in to a suitable value for
   OWIN compatible hosts such as Katana. *)

type TodoBackend () =
    member __.Configuration () =
        OwinAppFunc.ofFreya Freya.TodoBackend.Api.api

(* Main

   A very simple program, simply a console app, with a blocking read from
   the console to keep our server from shutting down immediately. Though
   we are self hosting here as a console application, the same application
   should be easily transferrable to any OWIN compatible server, including
   IIS. *)


// Create test server and client
let baseAddress = Uri "http://localhost/"
let mutable server = Unchecked.defaultof<_>
let mutable client = Unchecked.defaultof<_>

[<TestFixtureSetUp>]
let startup() =
    server <- TestServer.Create<TodoBackend>()
    server.BaseAddress <- baseAddress
    client <- server.HttpClient
    client.BaseAddress <- baseAddress
    // Add common CORS headers
    client.DefaultRequestHeaders.Add("Origin", "http://example.org/")

[<TestFixtureTearDown>]
let dispose() =
    client.Dispose()
    server.Dispose()

[<Test>]
let ``todobackend returns empty array at first`` () = 
    async {
        use request = new HttpRequestMessage(HttpMethod.Get, Uri "http://localhost/")
        use! response = Async.AwaitTask <| client.SendAsync request
        let! result = Async.AwaitTask <| response.Content.ReadAsStringAsync()
        result =? "[]" }
    |> Async.RunSynchronously

[<Test>]
let ``todobackend should return 404 for non-existent resource`` () =
    async {
        use! response = Async.AwaitTask <| client.GetAsync("http://localhost/not/found")
        response.StatusCode =? System.Net.HttpStatusCode.NotFound }
    |> Async.RunSynchronously