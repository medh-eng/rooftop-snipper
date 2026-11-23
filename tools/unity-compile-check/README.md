# Unity compile check (no Unity needed)

`dotnet build tools/unity-compile-check` compiles every Harbor Watch script,
runtime and Editor, against small stand-ins for the Unity APIs the project
uses (`UnityStubs.cs`, `EditorStubs.cs`).

It catches syntax and structural C# errors early. On 23 Nov 2026 it found four
errors that would have stopped the Unity Editor from entering Play mode:
a duplicate local `shoulder` in `HarborCrowdTraffic.cs` (CS0136) and a missing
`#endif` in three Editor build tools (CS1027).

The stand-ins accept any arguments, so a passing build here does **not**
prove the code behaves correctly in Unity. When a new script uses a Unity API
the stand-ins lack, add the member to the stub file (signature only).

The case-engine tests run separately: `dotnet run --project tools/core-tests`.
