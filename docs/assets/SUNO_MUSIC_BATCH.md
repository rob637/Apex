# Suno AI Music Generation Batch

## 📊 CURRENT STATUS: 0/60 tracks (Not Started)

**Last Updated**: January 16, 2026

---

## ⚠️ No API Available - Manual Generation Required!

**Suno does not have a public API**, so use manual generation with these helper tools:
- **Hybrid Playwright script** - Opens browser, auto-copies prompts
- **Rename script** - Auto-renames short filenames to full names

---

## 🚀 QUICK START (Recommended Workflow)

### Option A: Simple Manual (No Script)
1. Go to [suno.ai](https://suno.ai) → Create → Custom mode
2. Copy style tags & prompts from sections below
3. Generate, download with **short name** (e.g., `MUS01.mp3`)
4. Upload to `/workspaces/Apex/tools/suno-batch-helper/output/`
5. Run rename script to get full names!

### Option B: Hybrid Script (Assisted)
See detailed setup below.

---

## 📁 FILE NAMING - SIMPLIFIED!

### When downloading from Suno, just use SHORT names:
```
MUS01.mp3
MUS02.mp3
MUS03.mp3
... etc
```

### Upload to:
```
/workspaces/Apex/tools/suno-batch-helper/output/
```

### Then run rename script:
```bash
cd /workspaces/Apex/tools/suno-batch-helper
python rename_tracks.py output/
```

### Script automatically renames to full names:
```
MUS01.mp3 → MUS01_Main_Theme_Epic.mp3
MUS02.mp3 → MUS02_Victory_Fanfare.mp3
MUS03.mp3 → MUS03_Defeat_Somber.mp3
... etc
```

---

## 🚀 HYBRID SCRIPT SETUP (Optional)

### Step 1: Copy Script to Local Machine

**Script location in repo**: `/workspaces/Apex/tools/suno-batch-helper/suno_local.py`

**Save to your Windows machine as**: `C:\Users\rob\suno_local.py`

### Step 2: Install Dependencies (PowerShell)
```powershell
cd C:\Users\rob
pip install playwright pyperclip
playwright install chromium
```

### Step 3: Run the Helper
```powershell
python suno_local.py
```

---

## 📋 WORKFLOW (With Hybrid Script)

1. **Run script**: `python suno_local.py`
2. **Browser opens** → Log into suno.ai
3. Go to **Create** page, select **Custom** mode
4. Press ENTER in terminal
5. **Style tags auto-copied** → Paste into Suno style field
6. Press ENTER → **Prompt auto-copied** → Paste into description
7. Click **Generate**, wait ~30 sec, pick best result
8. **Download** the MP3 (use short name like `MUS01.mp3`)
9. Press ENTER → **Auto-saves progress**, shows next track
10. Type `q` to quit anytime (progress saved!)

---

## ⏱️ Time Estimate

| Tracks | Time per track | Total |
|--------|----------------|-------|
| 60 | ~1-2 min | **~1-2 hours** |

---

## 💾 Output Locations

### Downloaded files:
```
/workspaces/Apex/tools/suno-batch-helper/output/
├── MUS01.mp3  (short names OK!)
├── MUS02.mp3
└── ... 
```

### After running rename_tracks.py:
```
/workspaces/Apex/tools/suno-batch-helper/output/
├── MUS01_Main_Theme_Epic.mp3
├── MUS02_Victory_Fanfare.mp3
└── ... (full names!)
```

### Final Unity location:
```
unity/ApexCitadels/Assets/Audio/Music/
├── Themes/          # MUS01-MUS08
├── Ambient/         # MUS09-MUS20
├── Combat/          # MUS21-MUS32
├── Events/          # MUS33-MUS44
├── Environmental/   # MUS45-MUS52
└── UI/              # MUS53-MUS60
```

---

## 🛠️ Alternative: Command-Line Helper (Codespaces)

Location: `/workspaces/Apex/tools/suno-batch-helper/`

```bash
cd /workspaces/Apex/tools/suno-batch-helper
python prompt_helper.py
```

| Command | Action |
|---------|--------|
| `n` / `next` | Show next pending track |
| `d MUS01` | Mark track as complete |
| `s MUS05` | Show specific track details |
| `l` / `list` | List all tracks with status |
| `e` / `export` | Export remaining to file |

---

## Global Settings
- **Duration**: 2-4 minutes per track (loop sections for game use)
- **Quality**: High (320kbps MP3 or WAV)
- **BPM Range**: Specified per track type
- **Key**: Major keys for positive, minor for dramatic

---

# SECTION 1: MAIN THEMES (8 tracks)

## 1.1 Title & Menu Music

### MUS01 - Main Theme Epic
**Style Tags**: `orchestral, epic, fantasy, cinematic, brass, strings, percussion`
**BPM**: 90-100
**Duration**: 3:00
```
Epic orchestral main theme for medieval fantasy castle building game. Majestic brass fanfares, sweeping string melodies, powerful percussion. Starts quietly with solo French horn, builds to full orchestra crescendo. Themes of heroism, conquest, and building empires. Memorable main melody that can be hummed. Grand finale with timpani and full brass.
```

### MUS02 - Main Theme Soft
**Style Tags**: `orchestral, gentle, fantasy, strings, harp, peaceful`
**BPM**: 70-80
**Duration**: 2:30
```
Gentle orchestral version of main theme for medieval fantasy game menus. Soft strings carrying main melody, gentle harp arpeggios, light woodwinds. Peaceful and inviting, like viewing your kingdom from afar at sunset. Warm and welcoming atmosphere for new players.
```

### MUS03 - Menu Ambient
**Style Tags**: `ambient, medieval, fantasy, atmospheric, calm, strings`
**BPM**: 60-70
**Duration**: 3:00
```
Ambient background music for game menus. Soft evolving pads, gentle medieval instrumentation, occasional harp flourishes. Non-intrusive but engaging. Creates sense of entering a magical world. Subtle main theme motifs woven throughout.
```

### MUS04 - Loading Screen
**Style Tags**: `orchestral, building, anticipation, medieval, fantasy`
**BPM**: 80-90
**Duration**: 1:30
```
Loading screen music for medieval fantasy game. Building anticipation, gentle orchestral swell, hint of adventure to come. Short loopable section. Ends on unresolved chord to transition smoothly to gameplay music.
```

## 1.2 Story & Cutscene Music

### MUS05 - Victory Fanfare
**Style Tags**: `orchestral, triumphant, fanfare, brass, celebration`
**BPM**: 120-130
**Duration**: 1:00
```
Triumphant victory fanfare for winning battles and completing objectives. Bright brass fanfare, celebratory percussion, ascending melody lines. Short but impactful. Feeling of achievement and glory. Can loop for extended victory screens.
```

### MUS06 - Defeat Somber
**Style Tags**: `orchestral, sad, melancholy, strings, solo`
**BPM**: 50-60
**Duration**: 1:30
```
Somber defeat music for lost battles. Solo cello or violin with quiet string accompaniment. Melancholic but not depressing. Sense of lesson learned, motivation to try again. Resolves to hopeful ending note.
```

### MUS07 - Tutorial Friendly
**Style Tags**: `orchestral, light, friendly, woodwinds, playful`
**BPM**: 100-110
**Duration**: 2:00
```
Friendly tutorial music for learning game mechanics. Light woodwinds, playful rhythm, encouraging tone. Not too complex or distracting. Helpful and supportive feeling. Like a wise mentor guiding the player.
```

### MUS08 - Intro Cinematic
**Style Tags**: `orchestral, epic, cinematic, dramatic, fantasy, choir`
**BPM**: 85-95
**Duration**: 2:30
```
Epic cinematic intro music. Full orchestra with choir. Tells story of rising from small settlement to mighty empire. Dramatic build-ups, emotional peaks. Themes of destiny, ambition, and medieval glory. Hollywood-quality production feel.
```

---

# SECTION 2: GAMEPLAY AMBIENT (12 tracks)

## 2.1 Base Building Music

### MUS09 - Building Mode Peaceful
**Style Tags**: `medieval, peaceful, acoustic, lute, flute, ambient`
**BPM**: 70-80
**Duration**: 3:30
```
Peaceful building mode music for constructing castle. Gentle acoustic medieval instruments - lute, recorder, soft percussion. Non-intrusive background that doesn't distract from creative decisions. Pleasant loops for extended building sessions. Occasional bird sounds.
```

### MUS10 - Building Mode Active
**Style Tags**: `medieval, upbeat, construction, rhythmic, folk`
**BPM**: 100-110
**Duration**: 3:00
```
Active building music with gentle energy. Medieval folk feel with steady rhythm suggesting construction activity. Hammers and building implied in percussion. Motivating without being intense. Good for productive building sessions.
```

### MUS11 - Building Mode Night
**Style Tags**: `medieval, calm, night, ambient, strings, peaceful`
**BPM**: 60-70
**Duration**: 3:30
```
Nighttime building ambience. Soft strings, gentle night atmosphere, calm and focused mood. For players building late at night. Relaxing but not sleepy. Moonlit castle construction feeling.
```

### MUS12 - Upgrade Celebration
**Style Tags**: `medieval, celebratory, fanfare, short, joyful`
**BPM**: 120
**Duration**: 0:30
```
Short celebration music for building upgrades completing. Quick medieval fanfare, cheerful and satisfying. Positive reinforcement for player progress. Short enough to not interrupt gameplay flow.
```

## 2.2 Map & Exploration Music

### MUS13 - World Map Overview
**Style Tags**: `orchestral, exploration, vast, wonder, adventure`
**BPM**: 80-90
**Duration**: 3:00
```
World map overview music. Sense of vast world to explore. Orchestral with adventure themes. Looking down at territories, planning conquests. Inspiring wanderlust and strategic thinking. Hints of main theme.
```

### MUS14 - Territory Neutral
**Style Tags**: `medieval, neutral, ambient, exploration, mystery`
**BPM**: 75-85
**Duration**: 3:30
```
Music for exploring neutral territories. Mysterious but not threatening. Medieval ambient with sense of unknown lands. What secrets does this territory hold? Curiosity and careful exploration.
```

### MUS15 - Territory Friendly
**Style Tags**: `medieval, friendly, warm, folk, welcoming`
**BPM**: 90-100
**Duration**: 3:00
```
Music for allied/friendly territories. Warm and welcoming medieval folk music. Sense of visiting friends. Trade and diplomacy themes. Comfortable and safe feeling among allies.
```

### MUS16 - Territory Enemy
**Style Tags**: `orchestral, tense, danger, suspense, enemy`
**BPM**: 85-95
**Duration**: 3:00
```
Music for enemy territory proximity. Tense and suspenseful. Low strings, ominous undertones. Danger nearby but not immediate combat. Strategic awareness, need for caution.
```

## 2.3 Resource & Economy Music

### MUS17 - Economy Prosperous
**Style Tags**: `medieval, prosperous, market, lively, coins, trade`
**BPM**: 110-120
**Duration**: 3:00
```
Prosperous economy music. Lively medieval market feel. Sound of commerce and busy trade. Happy citizens, flowing resources. Kingdom thriving economically. Cheerful without being silly.
```

### MUS18 - Economy Struggling
**Style Tags**: `medieval, somber, concern, strings, worry`
**BPM**: 70-80
**Duration**: 2:30
```
Music for low resources or economic trouble. Concerned tone, strings with worried quality. Not panic, but awareness of challenges. Motivation to improve situation. Hopeful undertones.
```

### MUS19 - Shop & Market
**Style Tags**: `medieval, market, lively, folk, trading, cheerful`
**BPM**: 100-110
**Duration**: 3:00
```
In-game shop music. Cheerful medieval marketplace. Merchants selling wares, coins changing hands. Encouraging purchases without being pushy. Pleasant shopping experience.
```

### MUS20 - Treasure Found
**Style Tags**: `orchestral, discovery, magical, sparkle, reward`
**BPM**: 100
**Duration**: 0:45
```
Treasure discovery music. Magical sparkle sounds, rewarding orchestral swell. Short celebratory piece for finding rewards. Satisfying feedback for player achievements.
```

---

# SECTION 3: COMBAT MUSIC (12 tracks)

## 3.1 Pre-Combat

### MUS21 - Battle Preparation
**Style Tags**: `orchestral, preparation, tension, building, drums`
**BPM**: 90-100
**Duration**: 2:30
```
Pre-battle preparation music. Building tension, drums beginning to roll. Army assembling, strategies being made. Anticipation of combat. Not full battle yet but imminent.
```

### MUS22 - Enemy Approaching
**Style Tags**: `orchestral, warning, urgent, approach, suspense`
**BPM**: 100-110
**Duration**: 2:00
```
Enemy approaching warning music. Increasing urgency, enemy forces drawing near. Time to prepare defenses. Suspenseful build-up to combat. Growing intensity.
```

### MUS23 - Siege Incoming
**Style Tags**: `orchestral, siege, massive, epic, impending, war drums`
**BPM**: 85-95
**Duration**: 2:30
```
Siege incoming music. Massive war drums, full siege about to begin. Heaviest attack coming. Epic scale preparation. Final moments before major battle.
```

## 3.2 Active Combat

### MUS24 - Combat Light Skirmish
**Style Tags**: `orchestral, action, combat, light, skirmish, quick`
**BPM**: 130-140
**Duration**: 2:30
```
Light skirmish combat music. Quick action, small engagements. Fast-paced but not overwhelming. Minor combat encounters. Energetic percussion, exciting but manageable threat.
```

### MUS25 - Combat Medium Battle
**Style Tags**: `orchestral, battle, intense, combat, brass, percussion`
**BPM**: 140-150
**Duration**: 3:00
```
Medium intensity battle music. Full combat engagement. Brass and strings clash, powerful percussion. Serious battle but not desperate. Strategic combat, skill required.
```

### MUS26 - Combat Heavy War
**Style Tags**: `orchestral, war, epic, intense, full orchestra, battle`
**BPM**: 150-160
**Duration**: 3:30
```
Full scale war music. Massive orchestral combat. Epic brass fanfares clashing, thunderous percussion. Life or death struggle. Everything on the line. Peak intensity combat.
```

### MUS27 - Combat Boss Fight
**Style Tags**: `orchestral, boss, epic, dramatic, challenging, powerful`
**BPM**: 140-150
**Duration**: 3:00
```
Boss encounter music. Unique challenging enemy. Dramatic orchestral with memorable motifs. Sense of major threat. Special encounter requiring full effort. Cinematic quality.
```

### MUS28 - Combat Siege Defense
**Style Tags**: `orchestral, siege, defense, desperate, walls, epic`
**BPM**: 145-155
**Duration**: 3:30
```
Siege defense music. Defending castle walls. Desperate but heroic. Waves of enemies attacking. Holding the line. Epic defense music with rising and falling intensity.
```

### MUS29 - Combat Siege Attack
**Style Tags**: `orchestral, siege, attack, conquest, invasion, powerful`
**BPM**: 140-150
**Duration**: 3:30
```
Siege attack music. Storming enemy fortress. Conquest and invasion themes. Battering rams and siege towers. Taking the fight to the enemy. Triumphant aggression.
```

## 3.3 Post-Combat

### MUS30 - Combat Victory
**Style Tags**: `orchestral, victory, triumph, celebration, glory`
**BPM**: 110-120
**Duration**: 1:30
```
Post-combat victory music. Battle won, enemies defeated. Triumphant but respectful. Glory earned through combat. Celebration of victory. Resolves to peaceful conclusion.
```

### MUS31 - Combat Defeat
**Style Tags**: `orchestral, defeat, loss, somber, reflection`
**BPM**: 60-70
**Duration**: 1:30
```
Post-combat defeat music. Battle lost, but not the war. Somber reflection, lessons learned. Motivation to rebuild and return stronger. Ends with hopeful notes.
```

### MUS32 - Combat Draw/Retreat
**Style Tags**: `orchestral, retreat, tactical, relief, neutral`
**BPM**: 80-90
**Duration**: 1:00
```
Tactical retreat or draw music. Combat ended without clear winner. Relief at survival, need to regroup. Neither triumphant nor defeated. Strategic reset.
```

---

# SECTION 4: SOCIAL & UI MUSIC (8 tracks)

## 4.1 Alliance & Social

### MUS33 - Alliance Hall
**Style Tags**: `medieval, guild, brotherhood, proud, fellowship`
**BPM**: 90-100
**Duration**: 3:00
```
Alliance guild hall music. Brotherhood and fellowship themes. Proud guild members united. Medieval tavern meets noble court. Camaraderie and loyalty. Warm social atmosphere.
```

### MUS34 - Alliance War
**Style Tags**: `orchestral, alliance, war, united, epic, coordination`
**BPM**: 130-140
**Duration**: 3:00
```
Alliance war coordination music. Multiple guilds united against common enemy. Epic cooperation themes. Coordinated attacks, allied forces combining. Greater than sum of parts.
```

### MUS35 - Friends Online
**Style Tags**: `medieval, social, friendly, notification, pleasant`
**BPM**: 100
**Duration**: 0:15
```
Friend online notification sound/music. Pleasant brief melody. Social connection acknowledged. Welcoming sound for when friends join. Short and sweet.
```

### MUS36 - Chat Ambient
**Style Tags**: `medieval, chat, social, background, unobtrusive`
**BPM**: 75-85
**Duration**: 2:30
```
Background music for chat interfaces. Unobtrusive medieval ambient. Focus on communication, not music. Gentle presence that doesn't distract from reading messages.
```

## 4.2 Rewards & Achievements

### MUS37 - Achievement Unlocked
**Style Tags**: `orchestral, achievement, fanfare, reward, triumphant`
**BPM**: 120
**Duration**: 0:30
```
Achievement unlock fanfare. Triumphant short piece. Player accomplished something notable. Positive reinforcement, feeling of progress. Memorable and satisfying.
```

### MUS38 - Level Up
**Style Tags**: `orchestral, level up, growth, ascending, magical`
**BPM**: 110
**Duration**: 0:45
```
Level up celebration music. Ascending melody, magical growth sounds. Player becoming more powerful. Satisfying progression feedback. Brief but impactful.
```

### MUS39 - Daily Reward
**Style Tags**: `medieval, reward, gift, pleasant, opening`
**BPM**: 100
**Duration**: 0:30
```
Daily reward collection music. Pleasant gift-opening feel. Anticipation and reward. Encouraging daily return to game. Cheerful and generous feeling.
```

### MUS40 - Chest Opening
**Style Tags**: `medieval, chest, treasure, mystery, reveal, magical`
**BPM**: 90-100
**Duration**: 0:45
```
Treasure chest opening music. Mystery building, then reveal. Magical sparkle as chest opens. Excitement of unknown rewards. Satisfying loot experience.
```

---

# SECTION 5: EVENT & SEASONAL (12 tracks)

## 5.1 Seasonal Events

### MUS41 - Spring Festival
**Style Tags**: `medieval, spring, renewal, flowers, cheerful, festival`
**BPM**: 110-120
**Duration**: 3:00
```
Spring festival event music. Renewal and rebirth themes. Flowers blooming, new beginnings. Cheerful medieval celebration. Light and joyful spring atmosphere.
```

### MUS42 - Summer Festival
**Style Tags**: `medieval, summer, celebration, warm, festive, lively`
**BPM**: 120-130
**Duration**: 3:00
```
Summer festival event music. Peak celebration, warm days. Lively medieval summer fair. Dancing and merriment. Energetic and joyful summer atmosphere.
```

### MUS43 - Autumn Harvest
**Style Tags**: `medieval, autumn, harvest, grateful, folk, warm`
**BPM**: 95-105
**Duration**: 3:00
```
Autumn harvest festival music. Gratitude for bounty. Medieval harvest celebration. Folk instruments, warm atmosphere. Preparing for winter, enjoying abundance.
```

### MUS44 - Winter Holiday
**Style Tags**: `medieval, winter, holiday, magical, snow, cozy`
**BPM**: 90-100
**Duration**: 3:00
```
Winter holiday event music. Magical snowy atmosphere. Medieval winter celebration. Warm despite cold weather. Family and fellowship themes. Cozy and magical.
```

### MUS45 - Halloween Event
**Style Tags**: `medieval, halloween, spooky, fun, mysterious, playful`
**BPM**: 100-110
**Duration**: 3:00
```
Halloween special event music. Spooky but fun, not scary. Medieval mystery and magic. Playful ghost themes. Treats and tricks atmosphere. Family-friendly spooky.
```

### MUS46 - Anniversary Event
**Style Tags**: `orchestral, celebration, anniversary, grateful, retrospective`
**BPM**: 100-110
**Duration**: 2:30
```
Game anniversary celebration music. Looking back on journey. Grateful orchestral themes. Special occasion feel. Memorable moments remembered. Hopeful future ahead.
```

## 5.2 Special Events

### MUS47 - Tournament
**Style Tags**: `medieval, tournament, competition, jousting, exciting`
**BPM**: 120-130
**Duration**: 3:00
```
Tournament event music. Medieval jousting and competition. Exciting contest atmosphere. Knights competing for glory. Crowd cheering, champions rising. Competitive but sporting.
```

### MUS48 - Invasion Event
**Style Tags**: `orchestral, invasion, urgent, massive, server-wide, epic`
**BPM**: 140-150
**Duration**: 3:30
```
Server-wide invasion event music. Massive threat requiring all players. Urgent epic orchestral. Everyone must unite against common enemy. Peak cooperative gameplay.
```

### MUS49 - Limited Time Event
**Style Tags**: `orchestral, urgent, limited, exciting, special, countdown`
**BPM**: 125-135
**Duration**: 2:30
```
Limited time event urgency music. Special opportunity won't last. Exciting but time-sensitive. Don't miss out feeling. Energetic and motivating.
```

### MUS50 - World Boss Event
**Style Tags**: `orchestral, world boss, massive, coordinated, epic, legendary`
**BPM**: 145-155
**Duration**: 3:30
```
World boss event music. Massive legendary creature. All players coordinating to defeat. Epic scale encounter. Legendary battle themes. Once in a lifetime challenge.
```

### MUS51 - Night Event
**Style Tags**: `medieval, night, special, mysterious, stars, magical`
**BPM**: 80-90
**Duration**: 3:00
```
Special nighttime event music. Stars aligned, magic stronger. Mysterious opportunities at night. Special nocturnal activities. Magical night atmosphere.
```

### MUS52 - Trading Event
**Style Tags**: `medieval, trading, market, merchant, exotic, special`
**BPM**: 105-115
**Duration**: 2:30
```
Special trading event music. Exotic merchants arrived. Special goods available. Limited time deals. Exciting marketplace atmosphere. Rare opportunity for trades.
```

---

# SECTION 6: AMBIENT & ENVIRONMENTAL (8 tracks)

## 6.1 Environmental Atmosphere

### MUS53 - Morning Ambience
**Style Tags**: `ambient, morning, peaceful, birds, sunrise, medieval`
**BPM**: 60-70
**Duration**: 4:00
```
Morning ambient atmosphere. Sunrise over castle. Birds beginning to sing. Peaceful start to day. Soft medieval textures. Fresh new day beginning. Extended ambient loop.
```

### MUS54 - Afternoon Ambience
**Style Tags**: `ambient, afternoon, busy, active, medieval, peaceful`
**BPM**: 75-85
**Duration**: 4:00
```
Afternoon ambient atmosphere. Kingdom active but calm. Busy peaceful activity. Citizens going about day. Productive afternoon feeling. Background ambience.
```

### MUS55 - Evening Ambience
**Style Tags**: `ambient, evening, sunset, reflection, peaceful, medieval`
**BPM**: 65-75
**Duration**: 4:00
```
Evening ambient atmosphere. Sunset over castle. Day's work ending. Reflective peaceful mood. Warm evening light. Day well spent feeling.
```

### MUS56 - Night Ambience
**Style Tags**: `ambient, night, calm, stars, sleep, medieval`
**BPM**: 50-60
**Duration**: 4:00
```
Night ambient atmosphere. Stars out, castle sleeping. Calm nocturnal sounds. Guards on watch. Peaceful protected feeling. Kingdom at rest.
```

## 6.2 Weather Atmosphere

### MUS57 - Rain Ambience
**Style Tags**: `ambient, rain, cozy, inside, medieval, peaceful`
**BPM**: 60-70
**Duration**: 4:00
```
Rainy day ambient atmosphere. Rain on castle roof. Cozy inside feeling. Medieval rainy day. Peaceful contemplation. Good day for planning.
```

### MUS58 - Storm Ambience
**Style Tags**: `ambient, storm, dramatic, thunder, medieval, powerful`
**BPM**: 70-80
**Duration**: 3:30
```
Storm ambient atmosphere. Thunder rolling, powerful winds. Dramatic but safe inside. Medieval castle in storm. Nature's power on display. Exciting weather.
```

### MUS59 - Snow Ambience
**Style Tags**: `ambient, snow, quiet, winter, peaceful, medieval`
**BPM**: 55-65
**Duration**: 4:00
```
Snowy winter ambient atmosphere. Quiet snowfall, muffled sounds. Peaceful winter kingdom. Medieval winter scene. Tranquil and beautiful.
```

### MUS60 - Wind Ambience
**Style Tags**: `ambient, wind, mysterious, medieval, lonely, atmospheric`
**BPM**: 60-70
**Duration**: 3:30
```
Windy ambient atmosphere. Strong winds around castle. Mysterious and atmospheric. Medieval wind sounds. Slightly lonely feeling. Dramatic wind textures.
```

---

# SUMMARY: TOTAL MUSIC TRACKS

| Category | Count |
|----------|-------|
| Main Themes | 8 |
| Gameplay Ambient | 12 |
| Combat Music | 12 |
| Social & UI Music | 8 |
| Event & Seasonal | 12 |
| Ambient & Environmental | 8 |
| **TOTAL** | **60** |

---

# PRODUCTION NOTES

## Suno Settings
- Use "Custom" mode for best control
- Set instrumental if no vocals needed
- Generate multiple versions, select best
- Use "Extend" feature for longer pieces

## Audio Technical Specs
- **Format**: WAV (master) → MP3 320kbps (Unity)
- **Sample Rate**: 44.1kHz or 48kHz
- **Bit Depth**: 16-bit or 24-bit
- **Loudness**: -14 LUFS (game standard)

## Unity Audio Setup
```csharp
// Audio Manager recommended settings
public class MusicManager : MonoBehaviour
{
    [SerializeField] AudioSource musicSource;
    [SerializeField] float fadeTime = 2.0f;
    
    // Crossfade between tracks
    public void CrossfadeTo(AudioClip newTrack)
    {
        StartCoroutine(CrossfadeRoutine(newTrack));
    }
}
```

## Unity Import Settings
```
Load Type: Streaming (for longer tracks)
Compression Format: Vorbis (quality 70%)
Sample Rate Setting: Preserve
Load In Background: Yes
Preload Audio Data: No (for music tracks)
```

## Looping Guidance
| Track Type | Loop Method |
|------------|-------------|
| Menu/Ambient | Full track loop, crossfade ends |
| Combat | Seamless loop section |
| Fanfares | One-shot, no loop |
| Event Music | Full track loop with crossfade |

## Music State Machine
```
MENU → GAMEPLAY_PEACEFUL → COMBAT_LIGHT → COMBAT_HEAVY
                ↓                                ↓
           EXPLORATION                    COMBAT_BOSS
                ↓                                ↓
          TERRITORY_VIEW                 VICTORY/DEFEAT
```

## Recommended Fade Times
| Transition Type | Fade Duration |
|-----------------|---------------|
| Menu → Gameplay | 2.0 seconds |
| Peaceful → Combat | 1.0 seconds |
| Combat intensity change | 0.5 seconds |
| Victory/Defeat | 1.5 seconds |
| Event music start | 1.0 seconds |
