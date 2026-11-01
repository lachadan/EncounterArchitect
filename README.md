# Encounter Architect

A self-contained Windows desktop app for looking up monsters and tracking
initiative, HP, temporary HP, damage, healing, and notes across an encounter.
Includes 330 monsters from SRD 5.2.1 for the 2024 rules.

## Run

Extract the Windows release ZIP and run **EncounterArchitect.exe**. No separate
.NET installation, account, or internet connection is needed. Windows x64 only.

## Paste from D&D Beyond

1. Open a monster you can access on D&D Beyond and copy its name and complete
   stat block, including the size/type line.
2. Click **Paste from D&D Beyond** in the bestiary panel.
3. Click **Paste clipboard**, or paste into the large text box and click
   **Preview detected stats**.
4. Review and correct the name, type, AC, HP, initiative bonus, and CR.
5. Click **Save to library**, then add the monster to your encounter.

The complete copied text stays with the monster. Missing values are flagged;
initiative must be the bonus, not the passive initiative score. Older stat
blocks may require entering this bonus manually. The app does not convert
older monsters to the 2024 rules. Paste one monster at a time.

Importing a matching name requires confirmation before replacing the library
entry. Existing encounter creatures keep their own saved stats. Imported
content stays on your computer; the app does not access your account or send
clipboard content over the network.

See [README.txt](README.txt) for the encounter controls and JSON import format.

## Build and check

Install the .NET 10 SDK, then run:

```powershell
dotnet build -c Release
dotnet bin\Release\net10.0-windows\EncounterArchitect.dll --self-test
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Checks cover damage, temporary HP, healing limits, encounter persistence,
initiative ties, round transitions, all 330 SRD stat blocks, missing fields,
legacy fields, split labels, Unicode minus signs, and invalid or multiple blocks.
The test result is written to `self-test.txt` beside the tested app.

For isolated manual testing, launch with `--data-dir C:\path\to\test-data`.
Normal saves are in `%LOCALAPPDATA%\EncounterArchitect`.

## Version history

Git uses `main` and release tags. `v1.0.0` preserves the initial tracker;
`v1.1.0` adds the paste importer. See [CHANGELOG.md](CHANGELOG.md).
Saved encounters, custom monster libraries, and build outputs are excluded
from version control. The SRD library and a clearly marked custom example
are the only monster data committed to the source repository.

## SRD attribution

This work includes material from the System Reference Document 5.2.1
(“SRD 5.2.1”) by Wizards of the Coast LLC, available at
https://www.dndbeyond.com/srd. The SRD 5.2.1 is licensed under the Creative Commons
Attribution 4.0 International License, available at
https://creativecommons.org/licenses/by/4.0/legalcode.

Changes: extraction, indexing, and display formatting. No rule changes intended.
The SRD collection is not the entire Monster Manual. Independent application;
not affiliated with or endorsed by Wizards of the Coast.
