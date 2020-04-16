namespace LogAnalytics

open Newtonsoft.Json
open System
open System.Net.Http
open System.Net.Http.Headers
open System.Security.Cryptography
open System.Text

module Collector =

    /// Builds a signature for a LogAnalytics message.
    let private buildSignature (workspaceKey:string) (dateTime:DateTimeOffset) (json:string) =
        let bytesLen = json |> Encoding.UTF8.GetBytes |> fun bytes -> bytes.Length
        let str = String.Format ("POST{0}{1}{0}application/json{0}x-ms-date:{2}{0}/api/logs", "\n", bytesLen, dateTime.ToString("r"))
        let secretBytes = Convert.FromBase64String workspaceKey
        let strBytes = str |> Encoding.ASCII.GetBytes
        use hmac = new HMACSHA256 (secretBytes)
        let signature = Convert.ToBase64String (hmac.ComputeHash (strBytes))
        signature

    /// Builds a log analytics HTTP request message.
    let logAnalyticsMessage (workspaceKey:string) (workspaceId:string) (logName:string) (json:string) =
        let now = DateTimeOffset.UtcNow
        let signature = buildSignature workspaceKey now json
        let msg = new HttpRequestMessage (HttpMethod.Post, Uri(String.Format("https://{0}.ods.opinsights.azure.com/api/logs?api-version=2016-04-01", workspaceId)))
        msg.Headers.Authorization <- AuthenticationHeaderValue("SharedKey", String.Format("{0}:{1}", workspaceId, signature))
        msg.Headers.Add("Accept", "application/json")
        msg.Headers.Add("Log-Type", logName)
        msg.Headers.Add("x-ms-date", now.ToString("r"))
        msg.Headers.Add("time-generated-field", now.ToString("o"))
        msg.Content <- new StringContent (json, Encoding.UTF8)
        msg.Content.Headers.ContentType <- MediaTypeHeaderValue("application/json")
        msg

    /// Logging context for sending logs to a custom log.
    type LoggingContext =
        {
            WorkspaceKey : string
            WorkspaceId : string
            LogName : string
        }

    /// A structured log entry.
    type StructuredLog<'t> =
        {
            Context : LoggingContext
            Data : 't
        }

    module StructuredLog =
        /// Builds a log analytics HTTP request message from a structured log.
        let private message (log:StructuredLog<_>) =
            // Currently only sends the single entry at a time until a queue and batch is added.
            let json = JsonConvert.SerializeObject([|log.Data|], Formatting.None, JsonSerializerSettings(TypeNameHandling=TypeNameHandling.None))
            logAnalyticsMessage log.Context.WorkspaceKey log.Context.WorkspaceId log.Context.LogName json

        /// Builds and sends a structured log message 
        let send (http:HttpClient) (log:StructuredLog<_>) =
            async {
                use msg = message log
                return! msg |> http.SendAsync |> Async.AwaitTask
            }

        /// Builds and sends structured log data for a specific context.
        let sendWithContext (http:HttpClient) (context:LoggingContext) data =
            send http { Context=context; Data=data }
