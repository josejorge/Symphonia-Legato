# TODO / Backlog — Symphonia Legato

`ROADMAP.md` tracks *completed* phases; **this file is the forward-looking backlog**
of features and fixes not yet done. Rough priority, top = sooner. Check items off
and move notable completions into `CHANGELOG.md`.

## Known limitations / tech debt
- [ ] **Mixer → playback not connected.** Mixer volume/pan/mute change live MIDI
      channels, but playback rebuilds the MIDI from the `Score` on each Play, so
      mixer tweaks made while stopped don't affect sound. Fix: have the mixer
      mutate the `Staff` (Volume/Pan/IsMuted) via commands, or send live CC.
- [ ] **Duration toolbar doesn't reflect keyboard duration changes.** Number-key
      shortcuts change `SelectedDuration` but the radio buttons are command-bound,
      so their highlight doesn't move. Bind `IsChecked` to `SelectedDuration`.
- [ ] **No tuplets / ties in playback or entry UI** (model supports ties; entry +
      MIDI handling are incomplete).
- [ ] **Overlapping/chord NoteOff timing** in `ScoreToMidiConverter` assumes
      sequential notes; revisit when polyphonic entry per staff lands.
- [ ] **`run_*.txt` / `msbuild.binlog`** stale debug artifacts in repo root — delete.

## Editing
- [ ] Chord entry (stack notes on one beat) — add a pitch to the selected note.
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
- [ ] Solo/mute honoured in the rebuilt MIDI (ties into the mixer fix above).
- [ ] Click-to-scrub the timeline; a visible transport/progress bar.

## Notation rendering
- [ ] Real music font (SMuFL/Bravura) instead of Unicode glyphs for clefs/noteheads.
- [ ] Proper beam slanting and cross-staff beaming.
- [ ] Multi-voice (independent voices on one staff).
- [ ] Grace notes, tremolos, ornaments rendering.
- [ ] Rehearsal marks, repeat endings (voltas render but aren't fully wired).

## Import / export
- [ ] MusicXML round-trip fidelity tests (slurs, dynamics, lyrics, tuplets).
- [ ] MIDI export menu item is stubbed (`MainWindow.OnExportAsync` "midi" case) —
      wire it to write a `.mid` via `ScoreToMidiConverter`.
- [ ] MXL (compressed MusicXML) import.

## UX / app
- [ ] Settings/Preferences dialog actually persists (theme, MIDI device, autosave).
      `USER_MANUAL.md` documents `Edit → Preferences`, which doesn't exist yet.
- [ ] Recent files menu (the list exists in the VM; surface it in the File menu).
- [ ] Autosave + crash recovery.
- [ ] On-screen keyboard shortcut cheat-sheet (Help menu).
- [ ] Light theme (only Dark + High Contrast today).

## Long-term
- [ ] Real-time MIDI keyboard input (record from a connected MIDI device).
- [ ] Collaborative editing / cloud project sync beyond folder-based `ScoreSyncService`.
- [ ] Mobile (Android) feature parity for editing (currently viewer-focused).
