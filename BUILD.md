# Encounter Architect

Native WinForms application for Windows x64. Requires .NET 10 SDK to build.
The published app includes the runtime and needs no separate installation.

```powershell
dotnet publish EncounterArchitect.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
publish\EncounterArchitect.exe --self-test
```

The self-test writes self-test.txt next to the executable; a failure exits with
an unhandled exception. Normal launches open the encounter UI.

Monster data is embedded from monsters.json. It was extracted using pypdf from
the official SRD 5.2.1 PDF at:
https://media.dndbeyond.com/compendium-images/srd/5.2/SRD_CC_v5.2.1.pdf

The optional extract.py script expects work/srd.pdf and writes
work/Forge/monsters.json relative to its working directory. It is not used by
the application at runtime. See README.txt for the full SRD attribution.

No network calls are made by the app. Data is stored in the user's LocalAppData
EncounterArchitect directory; exported encounters include their monster snapshots.


