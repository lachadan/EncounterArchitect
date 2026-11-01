# Encounter Architect

A self-contained Windows desktop app for looking up monsters and tracking
initiative, HP, temporary HP, damage, healing, and notes across an encounter.
Includes 330 monsters from SRD 5.2.1 for the 2024 rules.

## Run

[Download the Windows application ZIP](https://github.com/lachadan/EncounterArchitect/releases/latest/download/EncounterArchitect-Windows.zip)
from the [latest release](https://github.com/lachadan/EncounterArchitect/releases/latest).

Extract the Windows release ZIP and run **EncounterArchitect.exe**. No separate
.NET installation, account, or internet connection is needed. Windows x64 only.

## Paste from D&D Beyond

**Import from D&D Beyond** supports either a URL or pasted text.
Paste an individual monster URL or a sourcebook link ending in an entry anchor
(for example, `/sources/dnd/motm/bestiary#Alhoon`), then click **Load URL**.
The app extracts one monster and shows editable fields before you save.

For Google sign-in or owned sourcebooks, use **Open normal browser**, sign in
there, open the entry, and copy its complete stat block. Return to the importer
and click **Paste clipboard**. Google does not support OAuth sign-in in an
embedded browser; version 1.3.1 detects that route and displays these instructions
instead of attempting to load it.

**Load URL** does not inherit sign-in from your normal browser. A link such as
`/sources/dnd/motm/bestiary#GreenAbishai` is an owned-sourcebook entry, not a public
monster page. Public URL loading can also be blocked by the website.

For other sign-in methods, you can try the optional **Embedded browser**. Sign
in yourself, return to the requested entry, wait for it to load, and
click **Read this monster**. If automatic detection fails, select the complete
stat block's text inside that browser and click **Read this monster** again.
Your browser's existing sign-in is not shared with
this embedded browser. Its separate profile is stored locally in
`%LOCALAPPDATA%\EncounterArchitect\BeyondBrowser` and may remember your sign-in.
The app reads the selected stat block only when you press the button. It does
not extract cookies or passwords, or upload imported monster content.

The embedded browser requires the Microsoft Edge WebView2 Runtime. If it is
missing, [download it from Microsoft](https://developer.microsoft.com/microsoft-edge/webview2/).
The encounter tracker, direct URL loading, and pasted-text import still work
without this optional runtime. URL loading and browser import need internet;
the tracker and saved monster library work offline.

Sourcebook ownership does not guarantee that D&D Beyond will allow its sign-in
flow in an embedded browser. If sign-in, a verification screen, or page changes
prevent import, use your regular browser and paste the complete stat block.
Playable species/race traits cannot be imported as a monster with HP and AC.
Chapters containing multiple monsters need a specific entry anchor.

Pasted-text workflow:

1. Open a monster you can access on D&D Beyond and copy its name and complete
   stat block, including the size/type line.
2. Click **Import from D&D Beyond** in the bestiary panel.
3. Click **Paste clipboard**, or paste into the large text box and click
   **Preview detected stats**.
4. Review and correct the name, type, AC, HP, initiative bonus, and CR.
5. Click **Save to library**, then add the monster to your encounter.

The complete imported text and its source URL (when available) stay with the
monster. Missing values are flagged;
initiative must be the bonus, not the passive initiative score. Older stat
blocks may require entering this bonus manually. The app does not convert
older monsters to the 2024 rules. Paste one monster at a time.

Importing a matching name requires confirmation before replacing the library
entry. Existing encounter creatures keep their own saved stats. Imported
content stays on your computer; the paste importer does not send
clipboard content over the network. The optional embedded browser contacts
D&D Beyond and its sign-in providers as you browse and sign in.

See [README.txt](README.txt) for the encounter controls and JSON import format.

## Session damage history

The window title and encounter heading display the installed application version.
Enter or select a **Character** beside the damage controls, then use **Damage**
or **Heal** as usual. Names you type remain available for the current application
session; encounter roster names are also suggested. A blank name is recorded as
**Unspecified**.

Click **Session history** to see the time, round, character, target, amount
entered, HP and temporary HP before/after, and whether an entry was reversed.
The window updates live while you continue using the encounter tracker.
Damage totals distinguish damage entered from actual HP lost and temporary HP
absorbed, so overkill is visible. Healing, temporary HP changes, restoring HP,
and manual HP adjustments are also logged but do not count as damage totals.
Undo marks affected events as reversed and removes them from totals.

History spans encounter changes within the current application session, clears
when the app exits, and is never written to encounter autosaves or exported
files. Closing only the history window keeps the log until you exit the app.

## In the Works

Planned enhancements for future releases. Scope and release dates are not yet
assigned; these features are not available in the current version.

- [ ] **Condition tracking and turn-based reminders** — Track conditions such as
  Prone or Frightened and flag when they expire, including effects tied to
  another creature's turn.
- [ ] **Reusable encounter templates** — Prepare encounters ahead of time and
  start fresh copies without carrying over damage or spent resources.
- [ ] **Saved player roster** — Store character names and initiative bonuses
  for quick encounter setup and damage attribution.
- [ ] **Ability-use tracking** — Track legendary resistance, legendary actions,
  spell slots, and recharge abilities separately for each creature.
- [ ] **Optional session summary export** — Save damage history and character
  totals before exiting while retaining the default clear-on-exit behavior.
- [ ] **Player-facing display** — Show initiative order and creature names on
  another screen while keeping exact monster HP hidden.

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
`v1.1.0` adds the paste importer; `v1.2.0` adds session history, character
attribution, and a visible build version; `v1.3.0` adds URL import and an optional
sign-in browser. See [CHANGELOG.md](CHANGELOG.md).
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
