# TODO / Backlog — Symphonia Legato

`ROADMAP.md` tracks *completed* phases; **this file is the forward-looking backlog**
of features and fixes not yet done. Rough priority, top = sooner. Check items off
and move notable completions into `CHANGELOG.md`.

## Known limitations / tech debt
- [x] **Mixer → playback not connected.** Fixed 2026-09-15: `MixerChannelViewModel`
      now writes Volume/Pan/IsMuted/IsSolo straight back into the `Staff` (bypassing
      undo/redo — a continuous mixing control, not a discrete notation edit, same
      treatment `Zoom` already got), and `ScoreEditor.MarkDirty()` marks the score
      dirty for Save without pushing an undo entry. `ScoreToMidiConverter` now also
      honours `IsSolo` (previously never read at all — see "Solo/mute honoured"
      below, resolved by the same change).
- [x] **Duration toolbar doesn't reflect keyboard duration changes.** Fixed
      2026-09-15: added `EnumEqualsConverter`, bound each `RadioButton.IsChecked`
      to `SelectedDuration.Value`.
- [ ] **No tuplets / ties in playback or entry UI** (model supports ties; entry +
      MIDI handling are incomplete).
- [ ] **Overlapping/chord NoteOff timing** in `ScoreToMidiConverter` assumes
      sequential (monophonic) notes; a longer note whose Off would land after the
      next note's On would emit a negative DeltaTime. Not yet reachable — entry
      is monophonic per staff today — so left alone rather than half-fixed;
      revisit together with true polyphonic/multi-voice entry per staff.
- [x] **`run_*.txt` / `msbuild.binlog`** stale debug artifacts in repo root —
      deleted 2026-09-15, added to `.gitignore`.
- [x] **`tests/SymphoniaLegato.PlaybackEngine.Tests` was an empty stub project**
      (found 2026-09-15). Added real coverage for `ScoreToMidiConverter` (tick
      conversion, tempo, percussion channel, chords, metronome). `MidiPlaybackEngine`
      itself still has no tests — it opens a real MIDI output device, so it needs
      a fake/abstraction over the device before it's unit-testable.
- [x] **`File ▸ Export ▸ MIDI...` was stubbed** — fixed 2026-09-15, now writes a
      real `.mid` via `ScoreToMidiConverter` (see "Import / export" below, which
      previously listed this as open).

## Editing
- [x] Chord entry (stack notes on one beat) — fixed 2026-09-15: Shift+letter
      (A–G) stacks a pitch onto the currently selected note instead of
      creating a new one (`AddChordPitchCommand`, `ScoreEditorViewModel.
      AddPitchToSelectedNote`). Octave chosen nearest the note's own root
      pitch. No-op on a rest or an exact-duplicate pitch. Click-based chord
      entry (Shift+click on the canvas) is not implemented — keyboard only
      for now; would need `ScoreCanvas`'s click routing to carry the Shift
      modifier through to the ViewModel.
- [ ] Drag a note vertically to change pitch; drag horizontally to move in time.
- [ ] Cut / copy / paste of note ranges and measures.
- [ ] Selection of multiple notes (marquee / shift-click) + bulk operations.
- [ ] Tie / slur entry by keyboard; tuplet entry (3, 5, …).
- [ ] Insert vs. overwrite entry modes; caret-based step entry indicator.
- [ ] Transpose selection / whole score by interval or diatonic step.

## Playback & audio
- [ ] **SoundFont (.sf2) playback** — the MIDI/Audio Settings dialog has a picker;
      wire an actual SF2 synth (e.g. via a soft-synth) instead of the GS Wavetable.
- [ ] Per-staff instrument change reflected live in playback.
- [ ] Loop region UI (set loop start/end on the sheet; engine already supports it).
- [ ] Count-in: make it sample-accurate by baking a pre-roll into the MIDI (today
      it's a `Task.Delay` click loop — fine, but not sample-locked).
- [x] Solo/mute honoured in the rebuilt MIDI — fixed 2026-09-15 alongside the
      mixer→playback wiring (see "Known limitations / tech debt" above).
- [ ] Click-to-scrub the timeline; a visible transport/progress bar.

## Notation rendering
- [ ] Real music font (SMuFL/Bravura) instead of Unicode glyphs for clefs/noteheads.
- [ ] Proper beam slanting and cross-staff beaming.
- [ ] Multi-voice (independent voices on one staff).
- [ ] Grace notes, tremolos, ornaments rendering.
- [ ] Rehearsal marks, repeat endings (voltas render but aren't fully wired).

## Import / export
- [ ] MusicXML round-trip fidelity tests (slurs, dynamics, lyrics, tuplets).
- [x] MIDI export menu item is stubbed (`MainWindow.OnExportAsync` "midi" case) —
      wired 2026-09-15 to write a `.mid` via `ScoreToMidiConverter`.
- [ ] MXL (compressed MusicXML) import.

## UX / app
- [x] Settings actually persist (theme, MIDI device, sync folder, recent files) —
      fixed 2026-09-15 via a new `AppSettingsService` (`%AppData%\SymphoniaLegato\
      settings.json`). Deliberately does NOT persist the Claude API key (kept
      session-only, a security tradeoff — see `docs/AUTHENTICATION.md`) or the
      SoundFont path (not wired to a real synth yet, so persisting it would be
      misleading). Along the way, fixed a real bug: choosing a MIDI device in
      the Settings dialog never actually changed which device played — it was
      hardcoded to `OutputDevice.GetByIndex(0)`. Added `IPlaybackEngine.
      SetOutputDevice(string?)`. No unified `Edit → Preferences` dialog was
      built — settings remain split across the View menu / MIDI Settings /
      Sync Settings, `USER_MANUAL.md` corrected to describe that accurately.
      Consolidating into one dialog is still open if wanted.
- [x] Recent files menu — fixed 2026-09-15: `File → Recent Files` submenu,
      capped at 10, persisted, stale (deleted/moved) entries pruned on load.
- [ ] Autosave + crash recovery.
- [ ] On-screen keyboard shortcut cheat-sheet (Help menu).
- [ ] Light theme (only Dark + High Contrast today).

## Long-term
- [ ] Real-time MIDI keyboard input (record from a connected MIDI device).
- [ ] Collaborative editing / cloud project sync beyond folder-based `ScoreSyncService`.
- [ ] Mobile (Android) feature parity for editing (currently viewer-focused).
