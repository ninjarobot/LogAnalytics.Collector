LogAnalytics.Collector
======================

Log Analytics Collector can sign and build messages to send structured logging data to an Azure Log Analytics workspace.

```fsharp
open LogAnalytics.Collector

// The structured data to log.
type LogEntry = {
     LogLevel : int
     Field1 : string
     Field2 : string
     Field3 : int
 }

// The workspace ID
let workspaceId = "YOUR-WORKSPACE-ID"

// The workspace key, a base64 string
let workspaceKey = "YOUR-WORKSPACE-KEY"

// A logging context that can be used for sending multiple log entries.
let loggingContext =
    {
        WorkspaceKey = workspaceKey
        WorkspaceId = workspaceId
        LogName = "Testing"
    }

// Sends the log data. 
use http = new System.Net.Http.HttpClient ()
use! res = StructuredLog.sendWithContext http loggingContext { LogLevel = 3; Field1 = "foo"; Field2 = "bar"; Field3 = 42 }
```

You can partially apply the functions to reuse the HttpClient and LoggingContext.
```fsharp
let sendLog = StructuredLog.sendWithContext http loggingContext
use! res = sendLog { LogLevel = 3; Field1 = "foo"; Field2 = "bar"; Field3 = 42 }
```
