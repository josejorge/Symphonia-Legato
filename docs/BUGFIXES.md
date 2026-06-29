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
