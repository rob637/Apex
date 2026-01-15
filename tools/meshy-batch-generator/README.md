# Meshy.ai Batch 3D Model Generator

Automates the generation of 216+ 3D game assets using Meshy.ai's API.

## Prerequisites

1. **Meshy.ai Account** with API access
   - Get your API key from: https://www.meshy.ai/settings/api
   - Note: API access requires a paid plan (~$20/month for Pro)

2. **Python 3.8+** with requests library

## Quick Start

```bash
# Install dependencies
pip install requests

# Set your API key
export MESHY_API_KEY="your-api-key-here"

# Test with dry run (no API calls)
python generate_models.py --dry-run

# List all 216 models
python generate_models.py --list

# Generate ALL models (will take several hours)
python generate_models.py

# Generate just one section
python generate_models.py --only-section 1  # Foundations (24 pieces)
python generate_models.py --only-section 2  # Walls (36 pieces)
python generate_models.py --only-section 3  # Towers (24 pieces)
python generate_models.py --only-section 4  # Roofs (18 pieces)
python generate_models.py --only-section 5  # Buildings (30 pieces)
python generate_models.py --only-section 6  # Decorations (48+ pieces)

# Resume from a specific model (if interrupted)
python generate_models.py --start-from W15
```

## Output

Generated models are saved to:
```
tools/meshy-batch-generator/output/
├── F01_Stone_Foundation_1x1.glb
├── F02_Stone_Foundation_1x1_Variant_A_Cracked.glb
├── ...
└── progress.json  (tracks completed/failed models)
```

## Cost Estimate

| Quality Mode | Cost/Model | Total (216 models) |
|--------------|------------|-------------------|
| Preview      | ~$0.10     | ~$22              |
| Refine       | ~$0.40     | ~$86              |

Default uses "preview" mode. Edit `generate_models.py` line 148 to change to "refine" for higher quality.

## Time Estimate

- Each model takes 30-90 seconds to generate
- With rate limiting: ~5-6 hours for all 216 models
- The script saves progress and can resume if interrupted

## Progress Tracking

The script automatically:
- Skips already-downloaded models
- Saves progress after each model
- Can resume from where it left off
- Lists failed models at the end for retry

## Sections Overview

| Section | Type        | Count | Description                    |
|---------|-------------|-------|--------------------------------|
| 1       | Foundations | 24    | Stone, wood, magic bases       |
| 2       | Walls       | 36    | Stone, wood, barriers          |
| 3       | Towers      | 24    | Guard, archer, mage towers     |
| 4       | Roofs       | 18    | Peaked, flat, specialty        |
| 5       | Buildings   | 30    | Resource, military, special    |
| 6       | Decorations | 48+   | Props, furniture, effects      |

## Troubleshooting

**"MESHY_API_KEY not set"**
```bash
export MESHY_API_KEY="your-key-here"
```

**Rate limit errors**
- The script already includes rate limiting
- If you still hit limits, increase `REQUEST_DELAY` in the script

**Model generation fails**
- Check your Meshy account has credits
- Some complex prompts may need simplification
- Failed models are logged for retry

## Alternative: Manual Batch Workflow

If you prefer not to use the API:

1. Open Meshy.ai in multiple browser tabs
2. Use the `--list` command to get all prompts
3. Copy prompts in batches of 5-10
4. Use browser automation (Playwright/Selenium) for bulk submission

Would you like me to create a browser automation script as an alternative?
