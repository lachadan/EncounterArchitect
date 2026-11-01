ENCOUNTER ARCHITECT 1.3
Offline monster lookup and encounter tracking for the D&D 2024 rules

START
Double-click EncounterArchitect.exe. No installer, account for the tracker, internet connection for offline use,
or separately installed .NET runtime is required. This build is for Windows
10/11 on x64 computers. The executable can be moved to a folder of your choice.

QUICK START
1. Search by monster name or type, or choose a challenge rating.
2. Select a result to read its complete SRD stat block in the middle pane.
3. Set the quantity and click Add to encounter. Each copy has its own HP.
4. Click Roll initiative to roll a separate d20 plus the published initiative
   bonus for each creature and sort the roster. Alternatively edit Init cells
   and click Sort. New creatures initially show their initiative BONUS until
   you roll or enter an initiative total.
5. Click Next turn to begin. Gold marks the active creature. The round counter
   advances when you cycle through the roster. All creatures, including those
   at 0 HP, remain in the turn order; remove them manually if desired.
6. Select a roster row, enter an amount, and click Damage or Heal.
   Damage consumes temporary HP first; HP stops at zero. Healing stops at Max.
   Set temp HP replaces the current temporary HP; it does not add to it.
7. Edit the roster cells for names, initiative, HP, Max, Temp, AC, or notes.
   Use the notes column for conditions, concentration, and ability usage.
   Conditions, resistances, and special abilities are adjudicated manually.
8. Use Undo to reverse encounter changes. Undo history lasts for this session.

SAVING
Changes are saved automatically to:
%LOCALAPPDATA%\EncounterArchitect\encounter.json
Custom monsters are saved beside that file as custom-monsters.json.
The most recent encounter is restored when you open the app again.
Use Save as to keep separate encounters and Open to switch between them.
Open/New replace the current roster; Undo can restore it during that session.
Keep only one copy of the app open at a time to avoid competing autosaves.

CUSTOM CREATURES / PLAYER CHARACTERS
Click Custom creature to enter a name, HP, AC, initiative bonus and stat text.
For players, enter their initiative bonus, then add them to the same roster.
Import JSON accepts an array in the format shown in custom-monsters.example.json.
Matching names replace library entries. Existing encounter entries retain their
original stat blocks. All imported content stays on your computer.

MONSTER COVERAGE AND SOURCE
Includes 330 monsters from SRD 5.2.1, the freely licensed reference for the
2024 rules. This is not the entire Monster Manual. You can enter other creatures
yourself. Stat blocks were extracted from the official PDF and reformatted;
consult the official source if an extraction or formatting issue arises.
https://www.dndbeyond.com/srd

This work includes material from the System Reference Document 5.2.1
("SRD 5.2.1") by Wizards of the Coast LLC, available at
https://www.dndbeyond.com/srd. The SRD 5.2.1 is licensed under the Creative Commons
Attribution 4.0 International License, available at
https://creativecommons.org/licenses/by/4.0/legalcode.
Changes: extraction, indexing, and display formatting. No rule changes intended.
Independent application; not affiliated with or endorsed by Wizards of the Coast.

VALIDATION
Build: self-contained single-file Windows x64 release, .NET 10.
Automated checks: temporary HP absorption, zero HP floor, healing cap, and
encounter JSON round trip. Desktop checks: library search, adding duplicate
creatures, independent damage, autosave, and roster layout.

SOURCE
The separate EncounterArchitect-Source.zip contains the source, embedded monster
data, extraction script, and build instructions. It is not required to run.



PASTE FROM D&D BEYOND
Click Paste from D&D Beyond, paste one complete stat block, preview the detected
fields, correct them, and save to your local library. Missing initiative bonuses
must be entered manually. Imported content is stored only on your computer.
See README.md in the source package for version history and build details.


SESSION HISTORY AND VERSION
The title and encounter heading show the installed version. Select or type a Character
name, apply damage, and click Session history to review entries and damage
totals. Undo marks affected entries as reversed. Healing, temporary HP changes,
and manual HP adjustments are also logged. History is kept only in memory,
spans encounter changes, and clears when the app exits. It is not saved.

URL IMPORT (VERSION 1.3.0)
Click Import from D&D Beyond, paste a monster URL, and click Load URL.
For a sourcebook chapter, include the entry anchor, such as #Alhoon.
If sign-in is required, use Sign in / open page. Sign in yourself, return to
the entry, and click Read this monster. Review the stats before saving.
The embedded browser requires Microsoft Edge WebView2 Runtime and keeps its
own local sign-in profile. Direct URL requests do not use your browser cookies.
If a page is blocked or unsupported, copy its stat block in your normal browser
and use Paste clipboard. URLs for playable race traits are not monster blocks.
URL import needs internet; existing tracking and saved monsters work offline.
Dependency notices are included in the licenses folder.
