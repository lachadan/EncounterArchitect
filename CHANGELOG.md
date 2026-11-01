# Changes

## 1.3.0 — 2026-11-01

- Load D&D Beyond monster URLs into the existing review/import dialog.
- Select one sourcebook entry using its #anchor.
- Add an optional WebView2 browser for user-controlled sign-in and stat reading.
- Keep pasted-text import as a fallback for blocked or unsupported pages.
- Store the source URL with imported monsters; preserve existing save compatibility.
- Reject missing, ambiguous, or non-monster entries instead of guessing.
- Validate URL hosts and redirects; limit request size and time.

## 1.2.0 — 2026-11-01

- Display the build version in the main title, encounter heading, and About.
- Record session-only damage history with character attribution and HP changes.
- Show damage totals per character, including temporary HP and overkill details.
- Log healing, temporary HP changes, restored HP, and direct HP adjustments.
- Mark undone events as reversed and exclude them from damage totals.
- Keep session history out of saved files and clear it when the app exits.

## 1.1.0 — 2026-11-01

- Paste individual monster stat blocks copied from D&D Beyond.
- Review and edit detected name, type, AC, HP, initiative bonus, and CR.
- Flag missing fields; preserve complete pasted text locally.
- Confirm replacement of existing library entries.
- Test the parser against every bundled monster and several paste formats.
- Begin local Git history with a baseline and separate importer commit.

## 1.0.0 — 2026-11-01

- Offline library with 330 SRD 5.2.1 monsters.
- Multi-creature encounters with initiative, HP, temporary HP, notes, and turns.
- Damage/healing, undo, automatic saves, encounter files, and custom monsters.
