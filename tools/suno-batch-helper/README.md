# Suno Music Batch Helper

Assists with manual generation of 60 music tracks from `SUNO_MUSIC_BATCH.md`.

## Why Manual?

**Suno has no public API** - all generation must be done through their web interface at [suno.ai](https://suno.ai).

## Usage

### Interactive Mode (Recommended)
```bash
cd /workspaces/Apex/tools/suno-batch-helper
python prompt_helper.py
```

Commands:
- `n` or `next` - Show next pending track
- `d MUS01` - Mark track as complete
- `s MUS05` - Show specific track details
- `l` or `list` - List all tracks with status
- `e` or `export` - Export remaining prompts to file
- `q` - Quit

### Export All Prompts
```bash
python prompt_helper.py --export
```
Creates `output/ready_prompts.txt` with all prompts formatted for easy copy-paste.

### Mark Tracks Complete
```bash
python prompt_helper.py --mark MUS01 MUS02 MUS03
```

### List All Tracks
```bash
python prompt_helper.py --list
```

## Workflow

1. **Run helper**: `python prompt_helper.py`
2. **Press `n`** to see next track
3. **Go to [suno.ai](https://suno.ai)** and select "Custom" mode
4. **Copy STYLE TAGS** into Suno's style field
5. **Copy PROMPT** into description/lyrics field
6. **Set duration** as shown and generate
7. **Download** the result
8. **Save as** the filename shown (e.g., `MUS01_Main_Theme_Epic.mp3`)
9. **Mark complete**: Press `d MUS01`
10. **Repeat** for next track

## Output

```
output/
├── progress.json       # Tracks completed status
└── ready_prompts.txt   # Exported prompts (after --export)
```

## Time Estimate

| Tracks | Time per track | Total |
|--------|----------------|-------|
| 60 | ~3 min | ~3 hours |

## Tips

- **Generate in batches** - Do 10-15 tracks per session
- **Use Suno's variations** - Generate 2-3 versions, pick best
- **Check loop points** - Ensure tracks loop smoothly for game use
- **Consistent style** - Keep similar instruments across related tracks

## Track Categories

| Section | Tracks | Type |
|---------|--------|------|
| Main Themes | MUS01-MUS08 | Title, menu, story |
| Gameplay Ambient | MUS09-MUS20 | Building, exploration |
| Combat Music | MUS21-MUS36 | Battle intensity levels |
| Event Music | MUS37-MUS48 | Seasons, special events |
| Environmental | MUS49-MUS56 | Weather, time of day |
| UI Sounds | MUS57-MUS60 | Short stingers |
