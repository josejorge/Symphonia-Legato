# User Manual

## Getting Started

When you launch Symphonia Legato, a new empty piano score (Grand Staff, 4/4, C major) is created automatically.

---

## Note Entry

1. Select a **duration** from the toolbar at the bottom: whole, half, quarter, eighth, 16th, 32nd.
2. Optionally enable the **dot** button (·) for dotted notes.
3. Click on a **staff position** to enter the note at that pitch.
4. To enter a **rest**, click the rest toggle (𝄽) first, then click the staff.

### Keyboard Shortcuts

| Key | Action |
|---|---|
| `1`–`7` | Select whole–64th duration |
| `.` | Toggle dot |
| `R` | Toggle rest mode |
| `Delete` | Delete selected note |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Space` | Play / Pause |
| `Escape` | Stop |
| `Ctrl+S` | Save |
| `Ctrl+N` | New score |
| `Ctrl+O` | Open |
| `Ctrl++` | Zoom in |
| `Ctrl+-` | Zoom out |
| `Ctrl+0` | Zoom reset |

---

## Playback

Use the **playback toolbar** (or Space/Escape) to control playback.

- **Tempo multiplier**: drag the slider to speed up or slow down (25%–400%).
- **Loop**: click ↻ to enable looping between the set loop points.
- **Metronome**: toggle the metronome click from the Playback menu.

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

## Version Control

If your score is inside a git repository, Symphonia Legato tracks changes automatically. Go to **Score → Git History** to browse previous versions and restore any saved state.

---

## Settings

`Edit → Preferences` opens the settings dialog:
- **Theme**: Dark / Light / High Contrast
- **Font**: Score font (SMuFL-compatible)
- **MIDI Output**: Select your MIDI device
- **SoundFont**: Choose a .sf2 file for playback
- **Autosave**: Interval (default 3 min)
