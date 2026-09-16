# User Manual — Symphonia Legato

*Created by Jose Jorge Hernandez*


## Getting Started

When you launch Symphonia Legato, a new empty piano score (Grand Staff, 4/4, C major) is created automatically.

To see the app in action immediately, choose **File → Load Demo Score** — it loads
"Ode to Joy" across several bars. Press **Play ▶** to hear it and watch the
playback cursor sweep the sheet.

---

## Note Entry

1. Select a **duration** from the toolbar at the bottom: whole, half, quarter, eighth, 16th, 32nd.
2. Optionally enable the **dot** button (·) for dotted notes.
3. Click on a **staff position** to enter the note at that pitch — you'll **hear the
   note** as you enter it.
4. To enter a **rest**, click the rest toggle (𝄽) first, then click the staff.

Notes **flow automatically across bar lines**: when a measure fills up, the next
note moves to the following measure, and new measures (and new lines/systems) are
created as needed. Notes never pile up or overlap inside one bar.

### Keyboard note entry

Type a **letter A–G** to place a note of that pitch. The octave is chosen to be
closest to the previous note (so stepwise melodies stay on the staff), the note
honours the current key signature, and notes flow across bar lines automatically.
Pick a duration first with the number keys.

### Keyboard Shortcuts

The full, current list lives in the app itself — **Help ▸ Keyboard Shortcuts**
(added 2026-09-15) — rather than being duplicated here where it can drift out
of sync. Every entry there is cross-checked against the actual code, not just
copied from menu labels; see `docs/BUGS.md` for a case where a menu label
(`Ctrl+L` for Loop) had turned out to be decorative with nothing behind it.

A few of the most-used ones: `A`–`G` enters a note, Shift+`A`–`G` adds a pitch
to the selected note (chord entry), `1`–`6` picks a duration, `Space` plays,
`Escape` stops.

---

## Playback

Use the **playback toolbar** (or Space/Escape) to control playback.

- **Play / Pause / Stop / Rewind**: standard transport controls.
- **Tempo multiplier**: drag the slider to speed up or slow down (25%–400%).
- **Loop** (↻): enable looping.
- **Metronome** (🥁): when on, an audible click sounds on every beat during playback.
- **Count-in** (⏱): when on, one bar of clicks plays before playback starts.

### Playback indicator

While playing, the sheet shows where you are:
- a **vertical cursor line** at the current beat,
- a **shaded band** over the measure being played, and
- the note(s) **currently sounding highlighted in blue**.

The view **scrolls automatically** to keep the cursor in view.

### Play from a specific note

**Click any note** to place the cursor there and arm playback — pressing **Play**
then starts from that note instead of the beginning. **Stop** or **Rewind**
returns to the top.

---

## Mixer

Click **View → Show Mixer** (or the mixer icon on the toolbar) to open the Mixer panel.

Each staff has:
- **Vol** slider (0–127)
- **Pan** slider (0=left, 64=center, 127=right)
- **M** button — mute
- **S** button — solo

---

## Piano Keyboard

The virtual keyboard at the bottom of the screen:
- **Click** a key to preview the note through MIDI.
- Notes played during score playback are **highlighted** in green.
- Use the **Octave** and **Range** controls to show more of the keyboard.

---

## File Formats

### Saving

`File → Save` saves in **.enscore** format (ZIP with MusicXML inside).

### Exporting

`File → Export` supports:
- **MusicXML** — for use in other notation software
- **MIDI** — for use in DAWs
- **PDF** — for printing
- **PNG** / **SVG** — for embedding in documents

### Importing

`File → Open` accepts:
- `.enscore` (native format)
- `.musicxml` / `.xml` (MusicXML)
- `.mid` / `.midi` (MIDI)

---

## Grand Staff (Piano)

The default piano score shows a **treble + bass clef** grand staff. Notes entered on one staff are independent of the other. Use the **Hand coloring** toggle to colour right-hand notes blue and left-hand notes red.

---

## Transpose

`Score → Transpose` shifts every note in the score up or down a semitone or
an octave (chromatic transposition — added 2026-09-15). It applies to the
**whole score** — there's no way to select just a range of notes yet, so
transposing a single passage isn't possible today (see `docs/KNOWN_ISSUES.md`).
Undo restores the exact original spelling of every note, including flats.

---

## Version Control

If your score is inside a git repository, Symphonia Legato tracks changes automatically. Go to **Score → Git History** to browse previous versions and restore any saved state.

---

## Settings

There is no single unified Preferences dialog yet — settings are split across
a few menus, each of which now **persists across restarts** (as of
2026-09-15; previously everything reset every launch):

- **Theme**: `View → Theme` picks Dark / Light / High Contrast (all three are
  real as of 2026-09-15). The toolbar's theme button cycles through them.
- **MIDI Output**: `Playback → MIDI / Audio Settings...` — pick your device,
  click Apply. The selection now actually takes effect (previously the app
  always used the first device no matter what was selected).
- **SoundFont**: same dialog has a `.sf2` picker, but it isn't wired to an
  actual synth yet — selecting one has no audible effect (see
  `docs/KNOWN_ISSUES.md`). Not persisted, since persisting a setting that
  does nothing would be misleading.
- **Cloud sync folder**: `Sync → Cloud Sync Settings...`.
- **Recent Files**: `File → Recent Files` — the last 10 files you opened or
  saved, newest first; stale entries (deleted/moved files) are dropped
  automatically.
- **Score font / Autosave interval**: not implemented — see
  `docs/KNOWN_ISSUES.md` and `docs/TODO.md`.
