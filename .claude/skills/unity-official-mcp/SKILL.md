---
name: unity-official-mcp
description: Use Unity's official MCP server (com.unity.ai.assistant package, tools prefixed mcp__unity-mcp__) for Editor automation in this project — running arbitrary C# via Unity_RunCommand, reading console logs, capturing scenes/cameras, and generating assets. This is the only/primary path for Editor automation in this project as of 2026-06-27 — the third-party Coplay MCP bridge was removed.
---

# Unity Official MCP (com.unity.ai.assistant) — the Editor automation path for this project

**Decision as of 2026-06-27: this project uses only `com.unity.ai.assistant`'s MCP server for Editor automation.** The third-party Coplay bridge (`com.coplaydev.unity-mcp`) was removed from [Packages/manifest.json](../../../Packages/manifest.json) and its `mcp-local` entry removed from [.mcp.json](../../../.mcp.json). The user-level skill `unity-mcp-skill` (Coplay orchestrator) still exists globally for other projects but does not apply here — its tool names (`manage_gameobject`, `find_gameobjects`, etc.) will not work in this project anymore.

**Known reliability gap to watch for:** entering/exiting Play mode or triggering a domain reload temporarily disconnects the relay (`Unity_GetConsoleLogs` returns `"Unity not detected"` — see the gotcha section below). This was still unresolved as of the last test in this project, and there is no fallback MCP now — wait and retry per the recovery steps below rather than assuming the setup is broken.

## Tool inventory (discovered this session — schemas not all inspected yet)

| Tool | Purpose | Schema known? |
|---|---|---|
| `Unity_RunCommand` | Compile + execute arbitrary C# in the Editor | Yes (below) |
| `Unity_GetConsoleLogs` | Read Console messages/warnings/errors | Yes — `maxEntries` (int), `logTypes` (comma string e.g. `"Error,Warning"`), `includeStackTrace` (bool) |
| `Unity_AssetGeneration_GenerateAsset` | Generate an asset (likely AI image/texture/etc.) | No — inspect via `ToolSearch` before first use |
| `Unity_AssetGeneration_GetModels` | List available generation models | No |
| `Unity_Camera_Capture` | Capture a camera view | No |
| `Unity_SceneView_Capture2DScene` | Screenshot the 2D Scene View | No |
| `Unity_SceneView_CaptureMultiAngleSceneView` | Multi-angle Scene View capture | No |

Before calling any tool not yet inspected, run `ToolSearch` with `select:mcp__unity-mcp__<ToolName>` to load its real parameter schema — don't guess parameters.

## `Unity_RunCommand` — the golden template

This is the workhorse tool. It compiles and runs a C# script against the live Editor. The tool's own description enforces strict structure — deviating causes compile/execution failure:

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // 1. Your logic here
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // 2. Register changes for Undo/Redo and tracking
        result.RegisterObjectCreation(cube);

        // 3. Log the result
        result.Log("Created {0}", cube);
    }
}
```

Rules:
- Class **must** be named `CommandScript` (not `public`, must be `internal` — `public` causes an "Inconsistent Accessibility" compile error).
- Use `result.RegisterObjectCreation(obj)` after creating objects, `result.RegisterObjectModification(obj)` **before** mutating a property, `result.DestroyObject(obj)` instead of `Object.DestroyImmediate` — these wire up Undo/Redo.
- Log via `result.Log(...)` / `result.LogWarning(...)` / `result.LogError(...)`, not `Debug.Log`.
- No top-level statements — always the full class wrapper.
- The tool silently namespaces your class server-side (`Unity.AI.Assistant.Agent.Dynamic.Extension.Editor`) — visible in the `localFixedCode` field of the response; don't add your own namespace.

Response shape: `{ success, message, data: { isCompilationSuccessful, isExecutionSuccessful, executionId, compilationLogs, executionLogs, localFixedCode, result } }`. Check `isCompilationSuccessful` and `isExecutionSuccessful` separately — a script can compile fine and still fail at runtime, or vice versa is not possible but always check both.

## Known gotcha: domain reload breaks the relay connection

Calling `EditorApplication.EnterPlaymode()` (or anything else that triggers a domain reload — entering/exiting Play mode, recompiling scripts) drops the relay's live connection to the Editor process. Immediately afterward, `Unity_GetConsoleLogs` (and likely other tools) returns:

```
{"success": false, "error": "Unity not detected (no fresh discovery files found)"}
```

**This is expected, not a broken setup.** Wait a few seconds for the Editor to finish reloading and the relay to re-establish its discovery file, then retry. If it doesn't recover after ~10-15s, check that the Unity Editor window is still open and responsive (a domain reload can occasionally surface a compile error that halts things) — `Edit → Project Settings → AI → Unity MCP` should show **Unity Bridge: Running** and the client listed under **Connected Clients** again once it's back.

## Setup notes (for if the connection is ever lost / re-added)

1. Unity side: **Edit → Project Settings → AI → Unity MCP → Integrations**. Click **Configure** on the entry matching your actual client. Note: if you're using Claude Code through the **Claude Desktop app's "Code" tab**, the correct entry to configure is **Claude Desktop** (Unity sees the host process, not the in-app tab) — don't click the separate "Claude Code" entry in that case unless you're running the standalone Claude Code CLI outside Desktop.
2. This writes/confirms a relay command into the client's MCP config. In this project it landed in [.mcp.json](.mcp.json) as:
   ```json
   "unity-mcp": {
     "command": "C:\\Users\\Forte\\.unity/relay\\relay_win.exe",
     "args": ["--mcp"]
   }
   ```
3. **A new MCP server entry in `.mcp.json` is not picked up by restarting just the conversation/session.** The host app (Claude Desktop) must be fully quit and reopened for it to actually spawn the relay process and register tools. Confirm via `ToolSearch` for `unity-mcp` afterward — if nothing comes back, the process never launched.
4. Once launched, Unity's settings panel should show the client under **Connected Clients** (no separate manual approval step was needed in this session — "Pending Connections" stayed empty throughout, the Configure click itself was sufficient).
5. Sanity-check the connection with a trivial read-only `Unity_RunCommand` (e.g. log the active scene name and `Application.unityVersion`) before relying on it for real changes.
