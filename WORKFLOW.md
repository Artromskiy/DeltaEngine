# DeltaEngine workflow

Build the narrow producer first, then the solution:

```bash
dotnet restore Source/Delta.Engine.slnx
dotnet build Source/Delta.Engine.Windowed/Delta.Engine.Windowed.csproj \
  -c Release --no-restore --disable-build-servers -m:1 \
  /p:UseSharedCompilation=false
dotnet build Source/Delta.Engine.slnx -c Release --no-restore \
  --disable-build-servers -m:1 /p:UseSharedCompilation=false -v:minimal
dotnet test Source/Delta.Engine.slnx -c Release --no-build --no-restore \
  --disable-build-servers -m:1
```

Use headless contract tests before native composition. Surface existing legacy
warnings separately; do not mix dependency upgrades into an integration fix.
Run the real editor window from [../DeltaEditor/WORKFLOW.md](../DeltaEditor/WORKFLOW.md).
