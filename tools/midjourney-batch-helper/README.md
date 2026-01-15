# Midjourney Batch Prompt Helper

Streamlines the process of generating 317 UI assets for Apex Citadels using Midjourney.

## Quick Start

```bash
cd /workspaces/Apex/tools/midjourney-batch-helper

# Interactive mode (recommended)
python prompt_helper.py

# Show next 10 prompts ready to paste
python prompt_helper.py --batch 10

# Export all prompts to a file
python prompt_helper.py --export
```

## Workflow

### Step 1: Get Your Batch
```bash
python prompt_helper.py --batch 10
```

This outputs 10 ready-to-paste prompts like:
```
/imagine Game UI icon, single shiny gold coin... --v 6.1 --style raw --no text, words, letters, watermark --ar 1:1
```

### Step 2: Paste into Discord
1. Open Discord → Midjourney server
2. Copy each `/imagine` line
3. Paste and send
4. Wait for generation (~60 seconds each)

### Step 3: Select & Upscale
1. Review the 4 variations
2. Click U1-U4 to upscale your favorite
3. Right-click → Save Image As → Use the ID as filename (e.g., `RI01_Gold_Coin.png`)

### Step 4: Mark as Done
```bash
python prompt_helper.py --mark RI01 RI02 RI03 RI04 RI05
```

### Step 5: Repeat!

## Commands

| Command | Description |
|---------|-------------|
| `--batch N` | Show next N prompts (default: 10) |
| `--section N` | Show all prompts in section N (1-12) |
| `--mark ID...` | Mark prompts as complete |
| `--list` | List all prompts with status |
| `--export` | Export all remaining to `ready_prompts.txt` |
| `--start-from ID` | Start batch from specific ID |

## Interactive Mode

Just run `python prompt_helper.py` for an interactive menu:

```
> b 5        # Show batch of 5
> s 3        # Show section 3
> m RI01     # Mark RI01 complete
> e          # Export all
> l          # List all
> q          # Quit
```

## Sections Overview

| # | Section | Count | Type |
|---|---------|-------|------|
| 1 | Resource Icons | 32 | 1:1 icons |
| 2 | Action Icons | 48 | 1:1 icons |
| 3 | Navigation Icons | 24 | 1:1 icons |
| 4 | Status Icons | 24 | 1:1 icons |
| 5 | Building Type Icons | 20 | 1:1 icons |
| 6 | Rarity & Quality Frames | 15 | 1:1 frames |
| 7 | UI Panels & Frames | 24 | 4:3 panels |
| 8 | Achievement Icons | 30 | 1:1 icons |
| 9 | Emotes & Expressions | 16 | 1:1 emotes |
| 10 | Backgrounds & Textures | 20 | 16:9 / 1:1 |
| 11 | Faction Specific Assets | 40 | mixed |
| 12 | Seasonal & Event Assets | 24 | mixed |
| **Total** | | **317** | |

## Time Estimate

| Batch Size | Time per Batch | Total Time |
|------------|----------------|------------|
| 10 prompts | ~15-20 min | ~8-10 hours |
| 20 prompts | ~30-40 min | ~8-10 hours |

**Tips for Speed:**
- Work in batches of 10-20
- Queue multiple prompts at once (fast mode helps)
- Use "relax" mode if you have limited GPU minutes
- Save immediately after upscaling

## Output Location

Save your completed images to:
```
/workspaces/Apex/tools/midjourney-batch-helper/output/
```

Or directly to Unity assets:
```
/workspaces/Apex/unity/ApexCitadels/Assets/Art/UI/
```

## Progress Tracking

Progress is saved to `progress.json`:
```json
{
  "completed": ["RI01", "RI02", "RI03"],
  "in_progress": ["RI04", "RI05"]
}
```

Resume anytime - the tool remembers where you left off!
