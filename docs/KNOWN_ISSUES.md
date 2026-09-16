# Known Issues

User-facing gaps and limitations, as of 2026-09-15. This is the
"what doesn't work yet" list for someone using the app; `docs/TODO.md` is the
matching developer-facing backlog (same items, framed as work to do rather
than limitations to expect). `docs/BUGS.md` is for actual defects (something
that misbehaves), not missing features.

## Editing

- Chord entry only works from the keyboard (Shift+A–G stacks a pitch onto the
  selected note) — there's no click-based way to add a chord pitch yet.
- No drag-to-edit (dragging a note to change pitch or move it in time).
- No cut/copy/paste or multi-note selection — as a result, Transpose
  (Score ▸ Transpose) only works on the *whole* score, not a selected range.
- No tuplet entry, and ties are only partially supported (model supports
  them; entry UI and MIDI playback handling are incomplete).

## Playback

- Mixer volume/pan/mute/solo changes don't update audio that's *currently
  playing* in real time — they save immediately and apply starting with the
  *next* Play (playback rebuilds the MIDI from the score each time). This
  used to be worse: before 2026-09-15, a mixer tweak made while stopped was
  silently lost and never affected any future playback either — see
  `docs/BUGS.md`.
- No SoundFont (.sf2) playback yet — the settings dialog has a picker, but it
  isn't wired to an actual synth; playback uses the system's default GM
  device (e.g. Microsoft GS Wavetable Synth on Windows).

## Notation rendering

- Notation uses Unicode glyphs, not a real music font (SMuFL/Bravura) — it
  reads as sheet music but isn't engraving-quality yet.
- No cross-staff beaming or multi-voice (independent voices on one staff).
- No grace notes, tremolos, or ornaments.

## Import/export

- No MXL (compressed MusicXML) import — only plain `.xml`/`.musicxml`.
- **Exporting a piano (grand staff) score to MusicXML only keeps the first
  staff — the entire bass clef silently disappears.** This is data loss, not
  a stylistic gap; see `docs/BUGS.md`.
- MusicXML export doesn't write slurs, hairpins, dynamics, lyrics,
  articulations, or ties — only pitch/rest, duration, dots, and chord notes
  survive a round trip today.

## App / UX

- No autosave or crash recovery.
- Android app is viewer-focused, not a full editor.

For the developer-facing version of this list (with file pointers and
priority tiering), see `docs/TODO.md`.
