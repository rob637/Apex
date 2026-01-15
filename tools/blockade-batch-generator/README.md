# Blockade Labs Skybox Batch Generator

Automatically generates all 84 skybox assets from `BLOCKADE_SKYBOX_BATCH.md` using the Blockade Labs API.

## Prerequisites

1. **Blockade Labs Account** - Sign up at [skybox.blockadelabs.com](https://skybox.blockadelabs.com/)
2. **API Key** - Get from your account API settings
3. **Python 3.8+** with `requests` library

## Pricing

| Plan | Price | Skyboxes/month | Cost for 84 |
|------|-------|----------------|-------------|
| Free | $0 | 15 | N/A (need paid) |
| Indie | $12/mo | 100 | ~$12 |
| Pro | $30/mo | 500 | ~$30 |
| Studio | $100/mo | 2000 | ~$100 |

**Recommendation**: **Indie plan ($12/mo)** is enough for 84 skyboxes with some spare.

## Setup

```bash
# Navigate to the tool directory
cd /workspaces/Apex/tools/blockade-batch-generator

# Install dependencies (if needed)
pip install requests

# Set your API key
export BLOCKADE_API_KEY="your_api_key_here"
```

## Usage

### Generate All Skyboxes

```bash
python generate_skyboxes.py
```

### Run in Background (Recommended)

```bash
# Run detached so it survives terminal disconnect
nohup bash -c 'export BLOCKADE_API_KEY="your_key" && python -u generate_skyboxes.py >> generation.log 2>&1' &

# Check progress
tail -f generation.log
cat output/progress.json
```

## Output

Generated skyboxes are saved to:

```
output/
├── SKY01.png          # Equirectangular PNG (8K default)
├── SKY01.hdr          # HDR version (if available)
├── SKY02.png
├── SKY02.hdr
├── ...
└── progress.json      # Tracks completed skyboxes
```

## Resume After Interruption

The script automatically tracks progress. If interrupted, just run again:

```bash
python generate_skyboxes.py
```

It will skip already-completed skyboxes and continue from where it left off.

## Available Styles

| Style ID | Name | Best For |
|----------|------|----------|
| 67 | Fantasy Landscape | ✅ Recommended for game |
| 64 | Digital Painting | Artistic look |
| 69 | Anime Art Style | Stylized games |
| 70 | Realistic | Photo-realistic |
| 72 | Sci-Fi | Futuristic |

To change style, edit `generate_skyboxes.py` line ~280:

```python
processor.process_all(
    prompts,
    style="fantasy",  # Change to: digital_painting, anime, realistic, scifi
    delay=2
)
```

## Generation Time

- **Per skybox**: ~30-90 seconds
- **Total 84 skyboxes**: ~2-4 hours
- **Queue times may vary** based on server load

## Unity Import

After generation, copy to Unity:

```bash
# Copy all PNG files
cp output/*.png /workspaces/Apex/unity/ApexCitadels/Assets/Art/Skyboxes/

# Or copy HDR for better quality
cp output/*.hdr /workspaces/Apex/unity/ApexCitadels/Assets/Art/Skyboxes/
```

### Unity Skybox Material Setup

```csharp
// Create Skybox Material
Material skyboxMat = new Material(Shader.Find("Skybox/Panoramic"));
skyboxMat.SetTexture("_MainTex", yourSkyboxTexture);
skyboxMat.SetFloat("_Exposure", 1.0f);
RenderSettings.skybox = skyboxMat;
```

## Troubleshooting

### "API Key not set"
```bash
export BLOCKADE_API_KEY="your_key_here"
```

### Rate Limited
The script has built-in delays. If still rate limited, increase delay:
```python
processor.process_all(prompts, delay=5)  # 5 seconds
```

### Generation Failed
Check `generation.log` for details. Failed skyboxes can be retried by removing their ID from `output/progress.json`.

## Files

| File | Purpose |
|------|---------|
| `generate_skyboxes.py` | Main script |
| `output/progress.json` | Tracks completed skyboxes |
| `output/*.png` | Generated skybox images |
| `output/*.hdr` | HDR versions (when available) |
| `generation.log` | Execution log (when using nohup) |
