# Bug Fixes — Rendering & Playback Pass (2026-06-28)

This document records the bugs found and fixed during the "score canvas not
showing / no playback sound" investigation, with root cause and the fix for
each. All fixes build with **0 warnings / 0 errors** and the full test suite
(104 tests) still passes.

---

## 1. The music sheet (page) was invisible — dark ink on a dark page

**Symptom:** The score canvas appeared empty / "broken" — no visible sheet of
paper, the staff barely (if at all) discernible.

**Root cause:** `ScoreCanvas` drew the page with
`PageBrush = RGB(38,38,38)` (`#262626`) on top of the editor background
`EditorBackground = #252526`. Those two colours are virtually identical, so the
"paper" was invisible. Notation was drawn in light grey (≈ `RGB(220,220,220)`),
designed for a dark page that never showed.

**Fix:** Repainted the canvas as a classic engraving surface — **white paper
with black ink**, independent of the dark application chrome (this is what
Encore/Finale/Sibelius/MuseScore all do, even in dark mode):

- `PageBrush` → off-white `RGB(252,251,248)`, plus a drop shadow and a 1px
  border so the page reads as a physical sheet on the workspace.
- All notation brushes/pens (notes, stems, clefs, staff lines, barlines,
  accidentals, dynamics, lyrics, slurs, hairpins, beams) → near-black ink.
- Hand-colouring (right = blue, left = red) and the selection highlight were
  re-tuned for legibility on white.

**Files:** `src/Core/SymphoniaLegato.Rendering/ScoreCanvas.cs`
(brush/pen palette + `DrawPage`).

> Bonus: PNG/PDF/SVG exports now also produce clean white-paper output.

---

## 2. Pressing Play produced no sound

**Symptom (user-reported):** Add notes, press Play → silence.

**Root cause:** `PlaybackViewModel.LoadScoreAsync` / `IPlaybackEngine.LoadScoreAsync`
was **never called from anywhere**. The engine's `_midiFile` therefore stayed
`null`, and `MidiPlaybackEngine.PlayAsync` begins with `if (_midiFile is null) return;`
— so Play was a silent no-op. Even if it had been loaded once at startup, the
score is edited in place afterwards, so the engine's MIDI snapshot would be
stale and would not contain newly entered notes.

**Fix:**
- Added `PlaybackViewModel.ScoreProvider` (a `Func<Score?>`), wired in
  `MainWindowViewModel` to `() => ScoreEditor?.Editor?.Score`.
- `PlayPauseAsync` now rebuilds the MIDI from the **current** score the moment
  playback starts (but not when resuming from pause), so whatever you have
  entered is always what you hear.

**Files:** `src/Apps/SymphoniaLegato.Desktop/ViewModels/PlaybackViewModel.cs`,
`src/Apps/SymphoniaLegato.Desktop/ViewModels/MainWindowViewModel.cs`.

**Verified:** a 4-note score converts to a MIDI file containing 4 `NoteOn`
(velocity > 0) + 4 `NoteOff` events across 2 tracks; the machine exposes a MIDI
output device (the Microsoft GS Wavetable Synth at index 0), so audio is
produced.

---

## 3. The virtual piano keyboard was silent until you had pressed Play once

**Root cause:** `MidiPlaybackEngine.PreviewNoteAsync` (used by the on-screen
piano) returned early when `_outputDevice` was `null`, but the output device was
only ever opened inside `PlayAsync`. So clicking piano keys before starting
playback produced nothing.

**Fix:** Extracted a shared, lazy `EnsureOutputDevice()` and call it from both
`PlayAsync` and `PreviewNoteAsync`.

**Files:** `src/Core/SymphoniaLegato.PlaybackEngine/MidiPlaybackEngine.cs`.

---

## 4. The Mixer panel was always empty

**Root cause:** `MixerViewModel.LoadScore(score)` was never called, so the
mixer never built any per-staff channels.

**Fix:** Call `MixerVm.LoadScore(score)` from `MainWindowViewModel.LoadScore`
(alongside the AI assistant load that was already there).

**Files:** `src/Apps/SymphoniaLegato.Desktop/ViewModels/MainWindowViewModel.cs`.

---

## 5. Zoom buttons / menu did not zoom the score

**Root cause:** Two disconnected `Zoom` properties. The toolbar buttons and
View ▸ Zoom menu bound to `MainWindowViewModel.Zoom`, but the `ScoreCanvas`
binds to `ScoreEditorViewModel.Zoom` (which is what actually triggers a layout
recompute). Changing the toolbar zoom only updated the percentage label; the
canvas never zoomed.

**Fix:** `MainWindowViewModel.ZoomIn/ZoomOut/ZoomReset` now delegate to
`ScoreEditor.ZoomInCommand/…` and mirror the resulting value back into the local
`Zoom` for the label.

**Files:** `src/Apps/SymphoniaLegato.Desktop/ViewModels/MainWindowViewModel.cs`.

---

## 6. The canvas/scroll area didn't resize when zooming or adding measures

**Root cause:** `ScoreCanvas` registered `InvalidateVisual()` on `LayoutResult`
and `Zoom` changes but not `InvalidateMeasure()`. Since `MeasureOverride`
reports the control's size to the `ScrollViewer`, the scroll extent never
updated after a zoom or after measures were added — scrollbars were wrong and
content could be clipped.

**Fix:** Those property-change handlers now call **both** `InvalidateMeasure()`
and `InvalidateVisual()`.

**Files:** `src/Core/SymphoniaLegato.Rendering/ScoreCanvas.cs`.

---

## 7. Playback ignored the score's tempo

**Root cause:** `ScoreToMidiConverter.BuildTrack` received `bpm` but never
emitted a `SetTempoEvent`, so DryWetMidi played every score at its default
120 BPM regardless of `Score.InitialTempo`.

**Fix:** Emit a `SetTempoEvent` (from `InitialTempo`) once, on the first track.

**Files:** `src/Core/SymphoniaLegato.PlaybackEngine/ScoreToMidiConverter.cs`.

---

## 8. Dead / confusing code in piano-key construction (cleanup)

`PianoKeyboardViewModel.BuildKeys` computed an unused `isBlack` local via a
`? false : false` ternary and a redundant `&& !(E || B)` guard. Removed the dead
code; the generated keyboard is unchanged.

**Files:** `src/Apps/SymphoniaLegato.Desktop/ViewModels/PianoKeyboardViewModel.cs`.

---

---

# Note flow & playback cursor pass (2026-06-29)

## 9. Notes overflowed / overlapped past the bar line

**Symptom (user-reported):** "when the amount of notes overlaps the size of the
line, it doesn't create a new line — it overlaps the notes in one line."

**Root cause:** `Measure.AddNote` always appends at `TickOffset = UsedTicks`, and
note entry put every click into the *clicked* measure with no capacity check. Once
a measure was full, extra notes kept piling in with tick offsets beyond the
measure's capacity; the layout placed them at `headerX + tickOffset * tickWidth`,
i.e. past the closing bar line, overlapping the next measure. (System/line
wrapping itself already worked — the measures just never filled correctly.)

**Fix (two layers):**
1. **Auto-flow on entry** — `ScoreEditorViewModel.ResolveTargetMeasure` advances
   from the clicked measure to the first measure with room for the note, and
   appends new measures (on every staff) when the music runs off the end. Notes
   now fill bar 1, then bar 2, …, and the existing width-based system breaking
   wraps full bars onto new lines.
2. **Defensive layout** — `LayoutEngine` scales note positions by
   `max(timeSignatureCapacity, actualContentSpan)`, so even a hand-crafted
   over-full measure compresses to fit instead of spilling past the bar line.

**Verified (headless):** clicking one measure 60×  → notes flow into 15 measures
(exactly 4 quarters each, none over-full), every notehead stays within its
measure's note area, and the layout wraps to 3 systems/lines. The demo score
(32 notes) wraps to 2 lines.

**Files:** `ScoreEditorViewModel.cs`, `LayoutEngine.cs`, `ILayoutEngine.cs`.

## Features added alongside

- **Playback indicator** (`ScoreCanvas.PlaybackTick` + `MidiPlaybackEngine`
  position timer + `MainWindowViewModel` wiring): a vertical cursor at the current
  beat, a translucent band over the active measure, and a blue highlight on the
  note(s) currently sounding. `ScoreEditorView` auto-scrolls to follow it.
- **Live transport** — the engine reports `PositionChanged` on a 50 ms timer, so
  the time and measure/beat displays update during playback.
- **Audible note preview** — entering a pitched note plays it via
  `PreviewNoteAsync`.
- **File ▸ Load Demo Score** — loads "Ode to Joy" across 8 bars (wraps to 2
  lines) to exercise playback, the cursor, and wrapping at a glance.
- **Play from a note** — clicking a note arms `PlaybackViewModel.StartTick`; Play
  seeks there first (`SeekAsync` stores a pending seek applied when the `Playback`
  is built). `Stop`/`Rewind` reset to the top.
- **Audible metronome + count-in** — `MidiPlaybackEngine.SendClick` plays GM
  wood-block notes on the percussion channel (10). A per-beat click fires from the
  position timer when the metronome is on; a one-bar count-in plays before
  playback when count-in is on. Toggles live in the transport toolbar.

---

# Pitch round-trip & keyboard entry (2026-06-29)

## 10. Click-entered notes were stored/played at the wrong pitch

**Root cause:** `StaffPositionCalculator.FromStaffPosition` was **not** the inverse
of `Calculate`. It computed `targetDiatonic = refDiatonic + staffPosition` (missing
the `-1` that `Calculate` adds) and `octave = targetDiatonic / 7` (missing the
diatonic-group → real-octave shift). Result: a note clicked on the treble bottom
line (E4) was stored as **F5** — a step and an octave too high. Notes *drew* at the
clicked position (that uses the staff position directly) but *played* the wrong
pitch. There was no test on `FromStaffPosition`, so it went unnoticed.

**Fix:** Made it the exact inverse of `Calculate`
(`targetDiatonic = refDiatonic + staffPosition - 1`, `octave = (targetDiatonic - step)/7 - 1`),
and added a `Calculate`↔`FromStaffPosition` round-trip test.

**Files:** `StaffPositionCalculator.cs`, `tests/.../StaffPositionTests.cs`.

This was surfaced while building **keyboard note entry**, which round-trips through
the same method.

## Features added alongside (this pass)

- **Keyboard note entry** — type `A`–`G` to place a note (octave chosen nearest the
  previous note, honouring the key signature, flowing across bars), `1`–`6` for
  duration, `R` rest, `.` dot, `Delete` to remove, `Space`/`Esc` transport.
  (`ScoreEditorViewModel.EnterNoteByName`, `MainWindow.OnKeyDown`.)
- **Sample-accurate metronome** — the per-beat click is now **baked into the MIDI**
  (`ScoreToMidiConverter` adds a percussion click track when enabled) instead of
  being fired from the UI position timer, so it no longer jitters. Count-in remains
  a short pre-roll click loop.

## Notes / non-bugs

- `run_err.txt` in the repo root is a **stale** crash log (the old `InputGesture="Plus"`
  startup crash) from before commit `3d64da4`. The current `MainWindow.axaml`
  already uses `OemPlus`/`OemMinus`/`D0`, so the app starts cleanly. The leftover
  `run_*.txt` files are not used by the app.

## Known limitation (not fixed here)

- Mixer volume/pan/mute changes are applied to live MIDI channels but are not
  written back into the `Score`. Because playback now rebuilds the MIDI from the
  `Score` on each Play, mixer tweaks made while stopped are reflected only if the
  corresponding `Staff` properties change. Wiring the mixer to mutate the `Score`
  (or to send live CC during playback) is a follow-up.
