# Reinstall coordination checks

Run these commands on Windows with the .NET 6.0.428 SDK:

```powershell
dotnet run --project tests/ReinstallCoordination --configuration Release
dotnet run --project tests/SettingsGate --configuration Release -p:DefineConstants=QA_BUILD
```

The first check uses unique names for its marker and temporary files. The second builds the application with the QA name, keeping its marker separate from the installed application. Run it with `TEMP` and `TMP` pointing to a temporary directory; it exercises the Settings window without displaying it or starting Roblox.
