# Midjourney UI & 2D Asset Generation Batch

## Instructions

### Using Midjourney Web (Recommended)
1. Go to [midjourney.com/imagine](https://midjourney.com/imagine)
2. Copy ONE prompt at a time (including the global suffix)
3. Paste into the prompt box and click Submit
4. Select your favorite from the 4 generated images
5. Right-click → Save Image As
6. Save with the ID prefix filename (e.g., `RI01_Gold_Coin.png`)

### Using Discord (Alternative)
1. Open Discord and navigate to Midjourney
2. Copy each prompt below and paste into `/imagine`
3. Use variations (V1-V4) to get best results
4. Upscale (U1-U4) final selections
5. Save with ID prefix naming

---

## Global Suffix (Add to ALL prompts)
```
--v 6.1 --style raw --no text, words, letters, watermark
```

## Aspect Ratios

| Asset Type | Ratio | Add to prompt |
|------------|-------|---------------|
| Icons | Square | `--ar 1:1` |
| Panels | Wide | `--ar 4:3` |
| Banners | Ultra-wide | `--ar 3:1` |

---

## ⚠️ CRITICAL: Style Consistency

For a professional game, related assets **MUST look consistent**. Use Midjourney's style reference feature to ensure matching designs.

### Method 1: Style Reference (`--sref`)
After generating your first asset in a group, use its URL as a style reference:
```
[your prompt] --sref [IMAGE_URL] --sw 100
```
- `--sref` = style reference image URL
- `--sw 100` = style weight (0-100, higher = more similar)

### Method 2: Image Prompt
Upload/paste the reference image directly before your prompt:
```
[IMAGE_URL] [your prompt]
```

### Asset Groups That MUST Match

| Group | IDs | Description |
|-------|-----|-------------|
| **Gold Coins** | RI01, RI02, RI03 | Same coin embossing, color, shine |
| **Gems** | RI04, RI05, RI06, RI07, RI08 | Same faceting style, cut design |
| **Wood** | RI09, RI10 | Same grain texture, color |
| **Stone** | RI11, RI12 | Same stone texture, coloring |
| **Metal** | RI13, RI14, RI15 | Same metallic sheen, style |
| **Crystals** | RI16, RI17 | Same magical glow effect |
| **Food** | RI18, RI19, RI20, RI21 | Same art style, lighting |
| **XP/Progress** | RI22, RI23, RI24, RI25 | Same glow, energy style |
| **Potions/Status** | RI26, RI27, RI28, RI29 | Same icon style |
| **Attack Actions** | AI09, AI10, AI11, AI12 | Same weapon style |
| **Defense Actions** | AI13, AI14 | Same shield/fortress style |
| **Navigation** | NI01-NI24 | Same overall UI style |
| **Buttons** | BI01-BI48 | Same frame style, colors |
| **Panels** | PI01-PI24 | Same panel frame design |
| **Backgrounds** | BG01-BG12 | Same atmosphere, style |

### Recommended Workflow

1. **Generate "hero" asset first** - Pick the best one from each group
2. **Copy its Midjourney image URL** - Click image → Copy Link
3. **Use `--sref` for remaining group items** - This ensures visual consistency
4. **Adjust `--sw` if needed** - Lower (50) for variety, higher (100) for exact match

### Example Workflow for Gold Coins:

```bash
# Step 1: Generate RI01 first
Game UI icon, single shiny gold coin... --ar 1:1

# Step 2: Pick the best, copy its URL, then use for RI02
Game UI icon, stack of gold coins... --sref https://cdn.midjourney.com/xxx --sw 100 --ar 1:1

# Step 3: Same for RI03
Game UI icon, pile of gold coins... --sref https://cdn.midjourney.com/xxx --sw 100 --ar 1:1
```

---

## Output Locations

Save generated assets to the Unity project:

| Category | Save To |
|----------|---------|
| Resource Icons (RI) | `unity/ApexCitadels/Assets/Art/UI/Icons/Resources/` |
| Action Icons (AI) | `unity/ApexCitadels/Assets/Art/UI/Icons/Actions/` |
| Navigation Icons (NI) | `unity/ApexCitadels/Assets/Art/UI/Icons/Navigation/` |
| Button Icons (BI) | `unity/ApexCitadels/Assets/Art/UI/Buttons/` |
| Panel Frames (PI) | `unity/ApexCitadels/Assets/Art/UI/Panels/` |
| Backgrounds (BG) | `unity/ApexCitadels/Assets/Art/UI/Backgrounds/` |
| Portraits (PO) | `unity/ApexCitadels/Assets/Art/UI/Portraits/` |
| Misc | `unity/ApexCitadels/Assets/Art/UI/Misc/` |

---

# SECTION 1: RESOURCE ICONS (32 icons)

## 1.1 Primary Resources - 128x128px

### RI01 - Gold Coin
```
Game UI icon, single shiny gold coin, medieval fantasy currency, detailed embossed design, metallic reflection, clean simple design, dark background, 2D digital art, professional game asset
```

### RI02 - Gold Coin Stack
```
Game UI icon, stack of gold coins, medieval fantasy treasure, multiple coins arranged, gleaming metal, clean design, dark background, 2D digital art, professional game asset
```

### RI03 - Gold Pile
```
Game UI icon, pile of gold coins overflowing, medieval treasure hoard, wealthy abundance, sparkling gold, dark background, 2D digital art, professional game asset
```

### RI04 - Gem Blue Crystal
```
Game UI icon, blue crystal gem, faceted sapphire, premium currency, magical glow, brilliant shine, dark background, 2D digital art, professional game asset
```

### RI05 - Gem Red Ruby
```
Game UI icon, red ruby gem, faceted precious stone, rare currency, deep crimson glow, dark background, 2D digital art, professional game asset
```

### RI06 - Gem Green Emerald
```
Game UI icon, green emerald gem, faceted precious stone, nature currency, verdant glow, dark background, 2D digital art, professional game asset
```

### RI07 - Gem Purple Amethyst
```
Game UI icon, purple amethyst gem, faceted crystal, arcane currency, mystical glow, dark background, 2D digital art, professional game asset
```

### RI08 - Gem Diamond
```
Game UI icon, white diamond gem, brilliant cut, ultimate premium currency, rainbow sparkle, dark background, 2D digital art, professional game asset
```

### RI09 - Wood Bundle
```
Game UI icon, bundle of wooden logs, tied with rope, medieval lumber resource, natural brown tones, clean design, dark background, 2D digital art, professional game asset
```

### RI10 - Wood Planks
```
Game UI icon, stack of wooden planks, processed lumber, construction material, crafted wood resource, dark background, 2D digital art, professional game asset
```

### RI11 - Stone Pile
```
Game UI icon, pile of gray stone blocks, quarried rock, medieval building material, rough texture, dark background, 2D digital art, professional game asset
```

### RI12 - Stone Brick
```
Game UI icon, single stone brick, processed building block, masonry material, clean cut stone, dark background, 2D digital art, professional game asset
```

### RI13 - Metal Ore
```
Game UI icon, raw metal ore chunk, iron ore rock, unprocessed mineral, metallic glints in stone, dark background, 2D digital art, professional game asset
```

### RI14 - Metal Ingot
```
Game UI icon, silver steel ingot bar, refined metal, processed ore, industrial shine, dark background, 2D digital art, professional game asset
```

### RI15 - Metal Gold Bar
```
Game UI icon, gold ingot bar, precious refined metal, stamped bullion, wealthy resource, dark background, 2D digital art, professional game asset
```

### RI16 - Crystal Shard
```
Game UI icon, purple magic crystal shard, glowing arcane energy, mystical resource, magical sparkle, dark background, 2D digital art, professional game asset
```

### RI17 - Crystal Cluster
```
Game UI icon, cluster of magic crystals, multiple glowing shards, concentrated arcane power, dark background, 2D digital art, professional game asset
```

### RI18 - Food Bread
```
Game UI icon, loaf of bread, medieval bakery product, golden brown crust, food resource, dark background, 2D digital art, professional game asset
```

### RI19 - Food Meat
```
Game UI icon, cooked meat leg, medieval feast food, protein resource, savory appearance, dark background, 2D digital art, professional game asset
```

### RI20 - Food Basket
```
Game UI icon, woven basket with bread and fruit, medieval food bundle, provisions, meal resource, dark background, 2D digital art, professional game asset
```

### RI21 - Food Apple
```
Game UI icon, red apple fruit, fresh produce, simple food resource, healthy shine, dark background, 2D digital art, professional game asset
```

## 1.2 Experience & Points - 128x128px

### RI22 - XP Star
```
Game UI icon, golden star with sparkles, experience points symbol, level up indicator, radiant glow, dark background, 2D digital art, professional game asset
```

### RI23 - XP Orb
```
Game UI icon, glowing blue experience orb, floating energy sphere, XP pickup, magical aura, dark background, 2D digital art, professional game asset
```

### RI24 - Level Up Arrow
```
Game UI icon, upward golden arrow with sparkles, level up symbol, progression indicator, triumphant glow, dark background, 2D digital art, professional game asset
```

### RI25 - Skill Point
```
Game UI icon, glowing skill point crystal, ability unlock token, purple magical energy, talent resource, dark background, 2D digital art, professional game asset
```

## 1.3 Special Resources - 128x128px

### RI26 - Energy Lightning
```
Game UI icon, lightning bolt energy symbol, action points, stamina resource, electric yellow glow, dark background, 2D digital art, professional game asset
```

### RI27 - Mana Potion
```
Game UI icon, blue mana potion bottle, magical energy drink, mystical liquid, glowing contents, dark background, 2D digital art, professional game asset
```

### RI28 - Health Heart
```
Game UI icon, red heart health symbol, life points, vitality resource, pulsing glow, dark background, 2D digital art, professional game asset
```

### RI29 - Shield Armor
```
Game UI icon, golden shield symbol, defense points, protection resource, metallic shine, dark background, 2D digital art, professional game asset
```

### RI30 - Key Golden
```
Game UI icon, ornate golden key, unlock item, access resource, medieval design, dark background, 2D digital art, professional game asset
```

### RI31 - Ticket Token
```
Game UI icon, event ticket token, special entry pass, limited resource, golden trim, dark background, 2D digital art, professional game asset
```

### RI32 - Crown Royal
```
Game UI icon, golden royal crown, premium status, VIP resource, jeweled decoration, dark background, 2D digital art, professional game asset
```

---

# SECTION 2: ACTION ICONS (48 icons)

## 2.1 Building Actions - 96x96px

### AI01 - Build Hammer
```
Game UI icon, medieval hammer tool, construction symbol, building action, golden handle, silver head, clean design, dark background, 2D digital art
```

### AI02 - Build Plus
```
Game UI icon, plus sign in stone frame, add building symbol, construction action, medieval style, dark background, 2D digital art
```

### AI03 - Upgrade Arrow
```
Game UI icon, upward arrow with sparkles, upgrade symbol, improvement action, green and gold colors, dark background, 2D digital art
```

### AI04 - Demolish X
```
Game UI icon, red X with debris, demolish symbol, destruction action, warning style, dark background, 2D digital art
```

### AI05 - Move Arrows
```
Game UI icon, four directional arrows, move symbol, relocation action, white arrows, dark background, 2D digital art
```

### AI06 - Rotate Circular
```
Game UI icon, circular rotation arrows, rotate symbol, orientation action, curved arrows, dark background, 2D digital art
```

### AI07 - Place Checkmark
```
Game UI icon, green checkmark on foundation, place confirm, building placement action, approval symbol, dark background, 2D digital art
```

### AI08 - Cancel X
```
Game UI icon, red X mark bold, cancel symbol, abort action, stop indicator, dark background, 2D digital art
```

## 2.2 Combat Actions - 96x96px

### AI09 - Attack Sword
```
Game UI icon, crossed medieval swords, attack symbol, combat action, silver blades, dark background, 2D digital art
```

### AI10 - Attack Quick
```
Game UI icon, single sword with speed lines, quick attack, fast strike action, silver blade, dark background, 2D digital art
```

### AI11 - Attack Heavy
```
Game UI icon, large battle axe, heavy attack, powerful strike action, massive weapon, dark background, 2D digital art
```

### AI12 - Attack Siege
```
Game UI icon, catapult projectile, siege attack, artillery action, explosive impact, dark background, 2D digital art
```

### AI13 - Defend Shield
```
Game UI icon, medieval shield raised, defend symbol, protection action, silver and blue, dark background, 2D digital art
```

### AI14 - Defend Fortress
```
Game UI icon, castle walls fortified, fortress defense, stronghold action, impenetrable walls, dark background, 2D digital art
```

### AI15 - Heal Heart Plus
```
Game UI icon, red heart with green plus, heal symbol, recovery action, medical cross, dark background, 2D digital art
```

### AI16 - Buff Arrow Up
```
Game UI icon, upward arrow with wings, buff symbol, enhancement action, power increase, dark background, 2D digital art
```

### AI17 - Debuff Arrow Down
```
Game UI icon, downward arrow with chains, debuff symbol, weakness action, power decrease, dark background, 2D digital art
```

### AI18 - Special Star Burst
```
Game UI icon, star burst explosion, special ability, ultimate action, golden radiance, dark background, 2D digital art
```

## 2.3 Resource Actions - 96x96px

### AI19 - Collect Hand
```
Game UI icon, hand grabbing coins, collect symbol, gather resources action, golden glow, dark background, 2D digital art
```

### AI20 - Harvest Sickle
```
Game UI icon, medieval sickle tool, harvest symbol, resource gathering action, farming tool, dark background, 2D digital art
```

### AI21 - Mine Pickaxe
```
Game UI icon, mining pickaxe, mine symbol, extraction action, stone and metal, dark background, 2D digital art
```

### AI22 - Chop Axe
```
Game UI icon, woodcutting axe, chop symbol, lumber action, sharp blade, dark background, 2D digital art
```

### AI23 - Trade Scales
```
Game UI icon, balanced scales, trade symbol, exchange action, merchant tool, dark background, 2D digital art
```

### AI24 - Buy Cart
```
Game UI icon, shopping cart with coins, buy symbol, purchase action, market transaction, dark background, 2D digital art
```

### AI25 - Sell Coin Hand
```
Game UI icon, hand offering coin, sell symbol, sale action, merchant transaction, dark background, 2D digital art
```

### AI26 - Craft Anvil
```
Game UI icon, blacksmith anvil with hammer, craft symbol, creation action, forging tool, dark background, 2D digital art
```

## 2.4 Social Actions - 96x96px

### AI27 - Chat Bubble
```
Game UI icon, speech bubble, chat symbol, message action, communication icon, dark background, 2D digital art
```

### AI28 - Chat Bubbles Double
```
Game UI icon, two overlapping speech bubbles, conversation symbol, dialogue action, dark background, 2D digital art
```

### AI29 - Friend Add
```
Game UI icon, person silhouette with plus, add friend symbol, social connection action, dark background, 2D digital art
```

### AI30 - Alliance Handshake
```
Game UI icon, two hands shaking, alliance symbol, guild action, partnership, dark background, 2D digital art
```

### AI31 - Invite Letter
```
Game UI icon, sealed envelope with ribbon, invite symbol, invitation action, message delivery, dark background, 2D digital art
```

### AI32 - Gift Box
```
Game UI icon, wrapped gift box with bow, gift symbol, present action, giving, dark background, 2D digital art
```

### AI33 - Report Flag
```
Game UI icon, warning flag, report symbol, moderation action, alert indicator, dark background, 2D digital art
```

### AI34 - Block Circle
```
Game UI icon, circle with slash, block symbol, prevent action, restriction indicator, dark background, 2D digital art
```

## 2.5 System Actions - 96x96px

### AI35 - Settings Gear
```
Game UI icon, mechanical gear cog, settings symbol, configuration action, metallic gray, dark background, 2D digital art
```

### AI36 - Menu Bars
```
Game UI icon, three horizontal bars, menu symbol, navigation action, hamburger menu, dark background, 2D digital art
```

### AI37 - Close X
```
Game UI icon, X close button, exit symbol, close action, window control, dark background, 2D digital art
```

### AI38 - Back Arrow
```
Game UI icon, left pointing arrow, back symbol, return action, navigation arrow, dark background, 2D digital art
```

### AI39 - Forward Arrow
```
Game UI icon, right pointing arrow, forward symbol, proceed action, navigation arrow, dark background, 2D digital art
```

### AI40 - Refresh Circular
```
Game UI icon, circular arrows refresh, reload symbol, update action, sync indicator, dark background, 2D digital art
```

### AI41 - Search Magnify
```
Game UI icon, magnifying glass, search symbol, find action, exploration tool, dark background, 2D digital art
```

### AI42 - Filter Funnel
```
Game UI icon, filter funnel, sort symbol, organize action, refinement tool, dark background, 2D digital art
```

### AI43 - Info Circle i
```
Game UI icon, letter i in circle, info symbol, information action, help indicator, dark background, 2D digital art
```

### AI44 - Help Question
```
Game UI icon, question mark in circle, help symbol, support action, guidance indicator, dark background, 2D digital art
```

### AI45 - Warning Triangle
```
Game UI icon, exclamation in triangle, warning symbol, alert action, caution indicator, dark background, 2D digital art
```

### AI46 - Lock Closed
```
Game UI icon, closed padlock, locked symbol, secure action, restriction indicator, dark background, 2D digital art
```

### AI47 - Lock Open
```
Game UI icon, open padlock, unlocked symbol, access granted action, freedom indicator, dark background, 2D digital art
```

### AI48 - Sound Speaker
```
Game UI icon, speaker with sound waves, audio symbol, sound action, volume indicator, dark background, 2D digital art
```

---

# SECTION 3: NAVIGATION ICONS (24 icons)

## 3.1 Main Navigation - 80x80px

### NI01 - Home Castle
```
Game UI icon, medieval castle silhouette, home symbol, main base navigation, fortress icon, dark background, 2D digital art
```

### NI02 - Map Scroll
```
Game UI icon, treasure map scroll, world map navigation, exploration symbol, parchment design, dark background, 2D digital art
```

### NI03 - Inventory Backpack
```
Game UI icon, medieval backpack open, inventory navigation, storage symbol, adventurer bag, dark background, 2D digital art
```

### NI04 - Inventory Chest
```
Game UI icon, treasure chest open, inventory navigation, storage symbol, item container, dark background, 2D digital art
```

### NI05 - Alliance Banner
```
Game UI icon, guild banner flag, alliance navigation, clan symbol, team flag, dark background, 2D digital art
```

### NI06 - Alliance Shield
```
Game UI icon, heraldic shield emblem, alliance navigation, guild crest, team symbol, dark background, 2D digital art
```

### NI07 - Leaderboard Trophy
```
Game UI icon, golden trophy cup, leaderboard navigation, ranking symbol, champion prize, dark background, 2D digital art
```

### NI08 - Leaderboard Crown
```
Game UI icon, ranking crown with numbers, leaderboard navigation, competition symbol, dark background, 2D digital art
```

### NI09 - Shop Storefront
```
Game UI icon, medieval shop building, store navigation, market symbol, merchant stall, dark background, 2D digital art
```

### NI10 - Shop Coins
```
Game UI icon, bag of coins, shop navigation, purchase symbol, merchant currency, dark background, 2D digital art
```

### NI11 - Profile Knight
```
Game UI icon, knight helmet profile, player navigation, character symbol, avatar icon, dark background, 2D digital art
```

### NI12 - Profile Shield
```
Game UI icon, personal shield with silhouette, profile navigation, identity symbol, player badge, dark background, 2D digital art
```

## 3.2 Secondary Navigation - 80x80px

### NI13 - Notifications Bell
```
Game UI icon, golden bell with dot, notifications navigation, alert symbol, message indicator, dark background, 2D digital art
```

### NI14 - Chat Messages
```
Game UI icon, multiple message bubbles, chat navigation, communication symbol, conversation icon, dark background, 2D digital art
```

### NI15 - Friends People
```
Game UI icon, two person silhouettes, friends navigation, social symbol, connections icon, dark background, 2D digital art
```

### NI16 - Quests Scroll
```
Game UI icon, quest scroll with checkmarks, missions navigation, task list symbol, objectives icon, dark background, 2D digital art
```

### NI17 - Achievements Medal
```
Game UI icon, golden medal with star, achievements navigation, accomplishment symbol, reward icon, dark background, 2D digital art
```

### NI18 - Events Calendar
```
Game UI icon, calendar with star event, events navigation, schedule symbol, special date icon, dark background, 2D digital art
```

### NI19 - Battle Swords
```
Game UI icon, crossed swords, battle navigation, combat symbol, warfare icon, dark background, 2D digital art
```

### NI20 - Territory Flag
```
Game UI icon, territory flag on map, region navigation, land claim symbol, conquest icon, dark background, 2D digital art
```

### NI21 - Resources Pile
```
Game UI icon, mixed resource pile, resources navigation, materials symbol, inventory icon, dark background, 2D digital art
```

### NI22 - Army Troops
```
Game UI icon, soldier group formation, army navigation, military symbol, troops icon, dark background, 2D digital art
```

### NI23 - Tech Book
```
Game UI icon, ancient tome with gear, technology navigation, research symbol, progress icon, dark background, 2D digital art
```

### NI24 - Daily Gift
```
Game UI icon, wrapped daily gift box, rewards navigation, bonus symbol, daily login icon, dark background, 2D digital art
```

---

# SECTION 4: STATUS ICONS (24 icons)

## 4.1 Player Status - 64x64px

### SI01 - Online Green
```
Game UI icon, green glowing dot, online status, active player indicator, presence symbol, dark background, 2D digital art
```

### SI02 - Offline Gray
```
Game UI icon, gray circle dot, offline status, inactive player indicator, absence symbol, dark background, 2D digital art
```

### SI03 - Away Yellow
```
Game UI icon, yellow half-moon, away status, idle player indicator, pause symbol, dark background, 2D digital art
```

### SI04 - Busy Red
```
Game UI icon, red minus circle, busy status, do not disturb indicator, unavailable symbol, dark background, 2D digital art
```

### SI05 - VIP Crown
```
Game UI icon, small golden crown, VIP status, premium player indicator, elite symbol, dark background, 2D digital art
```

### SI06 - Admin Star
```
Game UI icon, blue admin star badge, administrator status, moderator indicator, authority symbol, dark background, 2D digital art
```

## 4.2 Building Status - 64x64px

### SI07 - Under Construction
```
Game UI icon, construction crane symbol, building in progress status, work ongoing indicator, dark background, 2D digital art
```

### SI08 - Complete Check
```
Game UI icon, green checkmark badge, complete status, finished building indicator, success symbol, dark background, 2D digital art
```

### SI09 - Damaged Warning
```
Game UI icon, orange warning crack, damaged status, repair needed indicator, alert symbol, dark background, 2D digital art
```

### SI10 - Destroyed Rubble
```
Game UI icon, gray rubble debris, destroyed status, ruin indicator, loss symbol, dark background, 2D digital art
```

### SI11 - Upgrading Arrow
```
Game UI icon, blue upward arrow badge, upgrading status, improvement indicator, progress symbol, dark background, 2D digital art
```

### SI12 - Protected Shield
```
Game UI icon, blue shield bubble, protected status, invulnerable indicator, safety symbol, dark background, 2D digital art
```

## 4.3 Effect Status - 64x64px

### SI13 - Buff Attack Sword
```
Game UI icon, green sword up arrow, attack buff status, damage increase indicator, power boost symbol, dark background, 2D digital art
```

### SI14 - Buff Defense Shield
```
Game UI icon, green shield up arrow, defense buff status, protection increase indicator, armor boost symbol, dark background, 2D digital art
```

### SI15 - Buff Speed Wing
```
Game UI icon, green wing up arrow, speed buff status, movement increase indicator, haste boost symbol, dark background, 2D digital art
```

### SI16 - Debuff Attack Sword
```
Game UI icon, red sword down arrow, attack debuff status, damage decrease indicator, weakness symbol, dark background, 2D digital art
```

### SI17 - Debuff Defense Shield
```
Game UI icon, red shield down arrow, defense debuff status, protection decrease indicator, vulnerability symbol, dark background, 2D digital art
```

### SI18 - Debuff Slow Chain
```
Game UI icon, red chain down arrow, slow debuff status, movement decrease indicator, hindrance symbol, dark background, 2D digital art
```

### SI19 - Poison Skull
```
Game UI icon, green skull with drops, poison status, damage over time indicator, toxin symbol, dark background, 2D digital art
```

### SI20 - Burn Flame
```
Game UI icon, orange flame burn, burning status, fire damage indicator, combustion symbol, dark background, 2D digital art
```

### SI21 - Freeze Snowflake
```
Game UI icon, blue snowflake ice, frozen status, cold effect indicator, frost symbol, dark background, 2D digital art
```

### SI22 - Stun Stars
```
Game UI icon, yellow spinning stars, stunned status, incapacitated indicator, daze symbol, dark background, 2D digital art
```

### SI23 - Heal Sparkle
```
Game UI icon, green sparkle plus, healing status, recovery indicator, regeneration symbol, dark background, 2D digital art
```

### SI24 - Invisible Ghost
```
Game UI icon, faded ghost outline, invisible status, hidden indicator, stealth symbol, dark background, 2D digital art
```

---

# SECTION 5: BUILDING TYPE ICONS (20 icons)

## 5.1 Building Categories - 96x96px

### BI01 - Resource Gold Mine
```
Game UI icon, gold mine entrance with pickaxe, resource building category, mining facility symbol, dark background, 2D digital art
```

### BI02 - Resource Lumber Mill
```
Game UI icon, sawmill with logs, lumber building category, wood processing symbol, dark background, 2D digital art
```

### BI03 - Resource Quarry
```
Game UI icon, stone quarry with blocks, quarry building category, stone mining symbol, dark background, 2D digital art
```

### BI04 - Resource Farm
```
Game UI icon, farm with crops, agriculture building category, food production symbol, dark background, 2D digital art
```

### BI05 - Military Barracks
```
Game UI icon, barracks with sword, military building category, troop training symbol, dark background, 2D digital art
```

### BI06 - Military Forge
```
Game UI icon, blacksmith forge with hammer, forge building category, weapon crafting symbol, dark background, 2D digital art
```

### BI07 - Military Tower
```
Game UI icon, guard tower silhouette, tower building category, defense symbol, dark background, 2D digital art
```

### BI08 - Military Wall
```
Game UI icon, castle wall segment, wall building category, fortification symbol, dark background, 2D digital art
```

### BI09 - Economy Market
```
Game UI icon, market stall with coins, market building category, trade symbol, dark background, 2D digital art
```

### BI10 - Economy Treasury
```
Game UI icon, vault with gold bars, treasury building category, wealth storage symbol, dark background, 2D digital art
```

### BI11 - Economy Warehouse
```
Game UI icon, warehouse with crates, storage building category, inventory symbol, dark background, 2D digital art
```

### BI12 - Magic Academy
```
Game UI icon, wizard tower with book, academy building category, magic research symbol, dark background, 2D digital art
```

### BI13 - Magic Portal
```
Game UI icon, portal archway with energy, portal building category, teleportation symbol, dark background, 2D digital art
```

### BI14 - Magic Temple
```
Game UI icon, temple with divine light, temple building category, worship symbol, dark background, 2D digital art
```

### BI15 - Special Throne
```
Game UI icon, throne room silhouette, throne building category, headquarters symbol, dark background, 2D digital art
```

### BI16 - Special Monument
```
Game UI icon, statue monument, monument building category, landmark symbol, dark background, 2D digital art
```

### BI17 - Special Hospital
```
Game UI icon, hospital with cross, hospital building category, healing symbol, dark background, 2D digital art
```

### BI18 - Special Tavern
```
Game UI icon, tavern with mug sign, tavern building category, social hub symbol, dark background, 2D digital art
```

### BI19 - Special Prison
```
Game UI icon, prison bars, prison building category, detention symbol, dark background, 2D digital art
```

### BI20 - Special Stable
```
Game UI icon, stable with horse, stable building category, mount facility symbol, dark background, 2D digital art
```

---

# SECTION 6: RARITY & QUALITY FRAMES (15 frames)

## 6.1 Item Rarity Frames - 120x120px

### RF01 - Common Frame Gray
```
Game UI item frame, simple gray stone border, common rarity tier, basic design, empty center, square frame, dark background, 2D digital art --ar 1:1
```

### RF02 - Uncommon Frame Green
```
Game UI item frame, green glowing border, uncommon rarity tier, subtle magic effect, empty center, square frame, dark background, 2D digital art --ar 1:1
```

### RF03 - Rare Frame Blue
```
Game UI item frame, blue glowing border, rare rarity tier, magical sparkles, empty center, square frame, dark background, 2D digital art --ar 1:1
```

### RF04 - Epic Frame Purple
```
Game UI item frame, purple glowing border, epic rarity tier, energy wisps, empty center, square frame, dark background, 2D digital art --ar 1:1
```

### RF05 - Legendary Frame Gold
```
Game UI item frame, golden glowing border, legendary rarity tier, divine light rays, empty center, square frame, dark background, 2D digital art --ar 1:1
```

### RF06 - Mythic Frame Rainbow
```
Game UI item frame, rainbow prismatic border, mythic rarity tier, ultimate power glow, empty center, square frame, dark background, 2D digital art --ar 1:1
```

## 6.2 Card Frames - 180x240px (for character/unit cards)

### RF07 - Card Frame Common
```
Game UI card frame, simple stone border, common tier card, medieval style, empty center, portrait orientation, dark background, 2D digital art --ar 3:4
```

### RF08 - Card Frame Rare
```
Game UI card frame, ornate blue border, rare tier card, magical accents, empty center, portrait orientation, dark background, 2D digital art --ar 3:4
```

### RF09 - Card Frame Epic
```
Game UI card frame, elaborate purple border, epic tier card, energy effects, empty center, portrait orientation, dark background, 2D digital art --ar 3:4
```

### RF10 - Card Frame Legendary
```
Game UI card frame, magnificent golden border, legendary tier card, divine radiance, empty center, portrait orientation, dark background, 2D digital art --ar 3:4
```

## 6.3 Achievement Frames - 100x100px

### RF11 - Achievement Bronze
```
Game UI achievement frame, bronze medal border, third tier achievement, circular frame, laurel details, dark background, 2D digital art --ar 1:1
```

### RF12 - Achievement Silver
```
Game UI achievement frame, silver medal border, second tier achievement, circular frame, laurel details, dark background, 2D digital art --ar 1:1
```

### RF13 - Achievement Gold
```
Game UI achievement frame, gold medal border, first tier achievement, circular frame, laurel crown details, dark background, 2D digital art --ar 1:1
```

### RF14 - Achievement Platinum
```
Game UI achievement frame, platinum diamond border, ultimate tier achievement, circular frame, sparkling details, dark background, 2D digital art --ar 1:1
```

### RF15 - Achievement Special
```
Game UI achievement frame, unique prismatic border, special limited achievement, circular frame, animated effect look, dark background, 2D digital art --ar 1:1
```

---

# SECTION 7: UI PANELS & FRAMES (24 panels)

## 7.1 Main Panels - Various Sizes

### UP01 - Main Panel Large
```
Game UI panel, medieval fantasy style frame, ornate golden corners, dark gradient center with subtle pattern, elegant stone border, wide horizontal panel, empty interior, 2D digital art --ar 16:9
```

### UP02 - Main Panel Medium
```
Game UI panel, medieval fantasy style frame, decorative corners, dark center with texture, medium sized panel, empty interior, 2D digital art --ar 4:3
```

### UP03 - Main Panel Small
```
Game UI panel, medieval fantasy frame, compact design, golden accents, small panel, empty interior, 2D digital art --ar 1:1
```

### UP04 - Main Panel Vertical
```
Game UI panel, medieval fantasy frame, tall vertical design, ornate top and bottom, side panel style, empty interior, 2D digital art --ar 9:16
```

## 7.2 Dialog Panels

### UP05 - Dialog Box Standard
```
Game UI dialog box, parchment style background, decorative scroll corners, speech panel, horizontal format, empty interior, 2D digital art --ar 3:1
```

### UP06 - Dialog Box Fancy
```
Game UI dialog box, ornate golden frame, royal announcement style, important message panel, empty interior, 2D digital art --ar 3:1
```

### UP07 - Dialog Box Alert
```
Game UI dialog box, red warning border, alert notification style, urgent message panel, empty interior, 2D digital art --ar 3:1
```

### UP08 - Dialog Box Success
```
Game UI dialog box, green success border, celebration style, achievement message panel, empty interior, 2D digital art --ar 3:1
```

## 7.3 Tooltip & Info Panels

### UP09 - Tooltip Standard
```
Game UI tooltip, small info box, dark background, thin border, pointed bottom indicator, compact design, empty interior, 2D digital art --ar 2:1
```

### UP10 - Tooltip Item
```
Game UI item tooltip, rarity-ready frame, stat display area, item information panel, empty interior, 2D digital art --ar 3:4
```

### UP11 - Tooltip Building
```
Game UI building tooltip, construction info panel, resource requirement area, building details frame, empty interior, 2D digital art --ar 1:1
```

### UP12 - Info Card
```
Game UI info card, character or unit display, portrait area top, stats area bottom, detailed information panel, empty interior, 2D digital art --ar 3:4
```

## 7.4 Button Frames

### UP13 - Button Normal Large
```
Game UI button, medieval stone style, golden border, rounded rectangle, normal unpressed state, large size, empty center, 2D digital art --ar 3:1
```

### UP14 - Button Normal Medium
```
Game UI button, medieval stone style, golden border, rounded rectangle, normal state, medium size, empty center, 2D digital art --ar 3:1
```

### UP15 - Button Normal Small
```
Game UI button, medieval stone style, thin border, rounded rectangle, normal state, small compact size, empty center, 2D digital art --ar 2:1
```

### UP16 - Button Pressed Large
```
Game UI button pressed state, darker stone texture, inset shadow effect, golden border, pressed down appearance, large size, empty center, 2D digital art --ar 3:1
```

### UP17 - Button Disabled
```
Game UI button disabled state, grayed out stone texture, muted border, unavailable appearance, empty center, 2D digital art --ar 3:1
```

### UP18 - Button Special Gold
```
Game UI premium button, golden metallic style, ornate design, special action button, glowing effect, empty center, 2D digital art --ar 3:1
```

## 7.5 Progress Elements

### UP19 - Progress Bar Frame
```
Game UI progress bar frame, medieval style, empty bar with ornate end caps, horizontal track, filling area empty, 2D digital art --ar 6:1
```

### UP20 - Progress Bar Fill Green
```
Game UI progress bar fill, green gradient, health or progress indicator, left-to-right fill, glowing edge, 2D digital art --ar 6:1
```

### UP21 - Progress Bar Fill Blue
```
Game UI progress bar fill, blue gradient, mana or experience indicator, left-to-right fill, glowing edge, 2D digital art --ar 6:1
```

### UP22 - Progress Bar Fill Gold
```
Game UI progress bar fill, golden gradient, premium or special progress, left-to-right fill, sparkling effect, 2D digital art --ar 6:1
```

### UP23 - Circular Progress Frame
```
Game UI circular progress frame, round border, cooldown or timer display, empty center, radial design, 2D digital art --ar 1:1
```

### UP24 - Slider Track
```
Game UI slider track, horizontal bar with notches, settings slider, volume or value control, empty design, 2D digital art --ar 8:1
```

---

# SECTION 8: ACHIEVEMENT ICONS (30 icons)

## 8.1 Combat Achievements - 96x96px

### AC01 - First Blood
```
Game UI achievement icon, single sword with blood drop, first victory, combat milestone, dramatic lighting, dark background, 2D digital art
```

### AC02 - Warrior 10 Wins
```
Game UI achievement icon, sword and shield with number 10, battle veteran, combat milestone, bronze style, dark background, 2D digital art
```

### AC03 - Champion 100 Wins
```
Game UI achievement icon, crossed swords with laurel wreath, hundred victories, combat master, silver style, dark background, 2D digital art
```

### AC04 - Conqueror 1000 Wins
```
Game UI achievement icon, golden crown over crossed swords, thousand victories, legendary warrior, golden style, dark background, 2D digital art
```

### AC05 - Undefeated Streak
```
Game UI achievement icon, flaming sword with streak counter, winning streak, unbeaten record, intense glow, dark background, 2D digital art
```

### AC06 - Giant Slayer
```
Game UI achievement icon, small sword defeating large enemy, underdog victory, epic triumph, dramatic scene, dark background, 2D digital art
```

## 8.2 Building Achievements - 96x96px

### AC07 - First Foundation
```
Game UI achievement icon, single foundation block with sparkle, first building placed, builder milestone, dark background, 2D digital art
```

### AC08 - Architect 10 Buildings
```
Game UI achievement icon, small village layout, ten buildings achievement, settlement milestone, dark background, 2D digital art
```

### AC09 - City Planner 50 Buildings
```
Game UI achievement icon, city skyline, fifty buildings achievement, urban development milestone, dark background, 2D digital art
```

### AC10 - Master Builder 100 Buildings
```
Game UI achievement icon, grand castle complex, hundred buildings achievement, architectural mastery, dark background, 2D digital art
```

### AC11 - Max Level Building
```
Game UI achievement icon, building with max level stars, fully upgraded structure, perfection milestone, dark background, 2D digital art
```

### AC12 - Diverse Builder
```
Game UI achievement icon, various building types arranged, all building types placed, variety milestone, dark background, 2D digital art
```

## 8.3 Resource Achievements - 96x96px

### AC13 - First Fortune
```
Game UI achievement icon, single gold coin with sparkle, first gold earned, wealth milestone, dark background, 2D digital art
```

### AC14 - Wealthy 10000 Gold
```
Game UI achievement icon, gold coin pile, ten thousand gold accumulated, wealth milestone, dark background, 2D digital art
```

### AC15 - Tycoon 100000 Gold
```
Game UI achievement icon, treasure vault overflowing, hundred thousand gold, extreme wealth milestone, dark background, 2D digital art
```

### AC16 - Gem Collector
```
Game UI achievement icon, various gems arranged, gem collection achievement, precious stones milestone, dark background, 2D digital art
```

### AC17 - Resource Baron
```
Game UI achievement icon, all resource types piled, resource mastery achievement, material wealth milestone, dark background, 2D digital art
```

## 8.4 Social Achievements - 96x96px

### AC18 - First Friend
```
Game UI achievement icon, two hands connecting, first friend added, social milestone, warm glow, dark background, 2D digital art
```

### AC19 - Popular 10 Friends
```
Game UI achievement icon, group of people, ten friends achievement, social network milestone, dark background, 2D digital art
```

### AC20 - Social Butterfly 50 Friends
```
Game UI achievement icon, large connected network, fifty friends achievement, popular player milestone, dark background, 2D digital art
```

### AC21 - Alliance Founder
```
Game UI achievement icon, guild banner planted, alliance created achievement, leadership milestone, dark background, 2D digital art
```

### AC22 - Alliance Champion
```
Game UI achievement icon, guild trophy raised, alliance victories achievement, teamwork milestone, dark background, 2D digital art
```

### AC23 - Generous Gifter
```
Game UI achievement icon, gift boxes sent, gifting achievement, generosity milestone, warm glow, dark background, 2D digital art
```

## 8.5 Progression Achievements - 96x96px

### AC24 - Level 10
```
Game UI achievement icon, number 10 with level badge, level milestone, progression achievement, bronze style, dark background, 2D digital art
```

### AC25 - Level 25
```
Game UI achievement icon, number 25 with level badge, level milestone, progression achievement, silver style, dark background, 2D digital art
```

### AC26 - Level 50
```
Game UI achievement icon, number 50 with level badge, level milestone, progression achievement, gold style, dark background, 2D digital art
```

### AC27 - Level 100 Max
```
Game UI achievement icon, number 100 with crown, max level achievement, ultimate progression milestone, legendary style, dark background, 2D digital art
```

### AC28 - Daily Streak 7
```
Game UI achievement icon, calendar with 7 day streak, weekly login achievement, dedication milestone, dark background, 2D digital art
```

### AC29 - Daily Streak 30
```
Game UI achievement icon, calendar with 30 day streak, monthly login achievement, commitment milestone, dark background, 2D digital art
```

### AC30 - Daily Streak 365
```
Game UI achievement icon, calendar with yearly badge, annual login achievement, legendary dedication milestone, dark background, 2D digital art
```

---

# SECTION 9: EMOTES & EXPRESSIONS (16 emotes)

## 9.1 Positive Emotes - 128x128px

### EM01 - Happy Smile
```
Game UI emote, happy smiling face, positive emotion, cheerful expression, stylized cartoon, yellow glow, dark background, 2D digital art
```

### EM02 - Thumbs Up
```
Game UI emote, thumbs up hand gesture, approval expression, positive feedback, stylized cartoon, golden glow, dark background, 2D digital art
```

### EM03 - Heart Love
```
Game UI emote, red heart with sparkles, love expression, affection symbol, stylized cartoon, pink glow, dark background, 2D digital art
```

### EM04 - Celebration
```
Game UI emote, party popper with confetti, celebration expression, victory symbol, stylized cartoon, colorful, dark background, 2D digital art
```

### EM05 - Star Impressed
```
Game UI emote, starry eyes face, impressed expression, amazement symbol, stylized cartoon, sparkling, dark background, 2D digital art
```

### EM06 - Clap Applause
```
Game UI emote, clapping hands, applause expression, appreciation symbol, stylized cartoon, golden sparkles, dark background, 2D digital art
```

## 9.2 Neutral Emotes - 128x128px

### EM07 - Thinking
```
Game UI emote, thinking face with thought bubble, contemplation expression, pondering symbol, stylized cartoon, dark background, 2D digital art
```

### EM08 - Wave Hello
```
Game UI emote, waving hand gesture, greeting expression, hello symbol, stylized cartoon, friendly glow, dark background, 2D digital art
```

### EM09 - Shrug
```
Game UI emote, shrugging figure, uncertainty expression, dunno symbol, stylized cartoon, neutral tone, dark background, 2D digital art
```

### EM10 - Surprised
```
Game UI emote, surprised face, shock expression, amazement symbol, stylized cartoon, blue accents, dark background, 2D digital art
```

## 9.3 Negative Emotes - 128x128px

### EM11 - Sad Tear
```
Game UI emote, sad face with tear, sadness expression, disappointment symbol, stylized cartoon, blue tones, dark background, 2D digital art
```

### EM12 - Angry
```
Game UI emote, angry face, frustration expression, upset symbol, stylized cartoon, red accents, dark background, 2D digital art
```

### EM13 - Thumbs Down
```
Game UI emote, thumbs down hand gesture, disapproval expression, negative feedback, stylized cartoon, gray tone, dark background, 2D digital art
```

### EM14 - Facepalm
```
Game UI emote, facepalm gesture, exasperation expression, disbelief symbol, stylized cartoon, muted colors, dark background, 2D digital art
```

## 9.4 Action Emotes - 128x128px

### EM15 - Battle Cry
```
Game UI emote, shouting face with energy, battle cry expression, war symbol, stylized cartoon, intense red, dark background, 2D digital art
```

### EM16 - GG Good Game
```
Game UI emote, handshake with GG letters, good game expression, sportsmanship symbol, stylized cartoon, golden glow, dark background, 2D digital art
```

---

# SECTION 10: BACKGROUNDS & TEXTURES (20 textures)

## 10.1 Panel Backgrounds - Seamless Tiles

### TX01 - Stone Wall Texture
```
Seamless stone wall texture, medieval castle interior, gray bricks, subtle shadows, tileable pattern, game UI background, 2D digital art --tile
```

### TX02 - Wood Plank Texture
```
Seamless wooden plank texture, medieval floor boards, warm brown tones, wood grain detail, tileable pattern, game UI background, 2D digital art --tile
```

### TX03 - Parchment Paper Texture
```
Seamless old parchment texture, aged paper, subtle stains, scroll background, tileable pattern, game UI background, 2D digital art --tile
```

### TX04 - Metal Plate Texture
```
Seamless metal plate texture, iron or steel surface, rivets and panels, industrial medieval, tileable pattern, game UI background, 2D digital art --tile
```

### TX05 - Leather Texture
```
Seamless leather texture, worn leather surface, stitching details, equipment background, tileable pattern, game UI background, 2D digital art --tile
```

### TX06 - Dark Gradient Texture
```
Seamless dark gradient texture, subtle noise pattern, UI panel background, vignette edges, tileable pattern, game interface background, 2D digital art --tile
```

## 10.2 Material Textures - For 3D Models (512x512)

### TX07 - Stone Material
```
Seamless stone material texture, medieval castle stone, normal map ready, PBR style, gray tones, tileable pattern, game asset texture --tile
```

### TX08 - Wood Material
```
Seamless wood material texture, medieval construction lumber, normal map ready, PBR style, brown tones, tileable pattern, game asset texture --tile
```

### TX09 - Metal Material
```
Seamless metal material texture, medieval iron armor, normal map ready, PBR style, silver metallic, tileable pattern, game asset texture --tile
```

### TX10 - Gold Material
```
Seamless gold material texture, ornate treasure surface, normal map ready, PBR style, rich golden, tileable pattern, game asset texture --tile
```

### TX11 - Crystal Material
```
Seamless crystal material texture, magical gem surface, translucent quality, PBR style, blue purple tones, tileable pattern, game asset texture --tile
```

### TX12 - Grass Material
```
Seamless grass material texture, medieval ground cover, normal map ready, PBR style, green natural, tileable pattern, game environment texture --tile
```

### TX13 - Dirt Path Material
```
Seamless dirt path texture, worn earth road, normal map ready, PBR style, brown earth tones, tileable pattern, game environment texture --tile
```

### TX14 - Cobblestone Material
```
Seamless cobblestone street texture, medieval town road, normal map ready, PBR style, gray stones, tileable pattern, game environment texture --tile
```

## 10.3 Special Effects Textures

### TX15 - Magic Glow Texture
```
Seamless magical glow texture, energy effect, blue ethereal light, particle effect background, tileable pattern, game VFX texture --tile
```

### TX16 - Fire Gradient Texture
```
Seamless fire gradient texture, flame effect, orange to yellow gradient, heat distortion background, tileable pattern, game VFX texture --tile
```

### TX17 - Water Surface Texture
```
Seamless water surface texture, ripple patterns, blue reflective, caustics effect, tileable pattern, game environment texture --tile
```

### TX18 - Smoke Texture
```
Seamless smoke texture, wispy cloud pattern, gray translucent, atmosphere effect, tileable pattern, game VFX texture --tile
```

### TX19 - Lightning Texture
```
Seamless lightning pattern texture, electric energy, branching bolts, blue white energy, tileable pattern, game VFX texture --tile
```

### TX20 - Sparkle Noise Texture
```
Seamless sparkle noise texture, random bright points, magical particle effect, star field style, tileable pattern, game VFX texture --tile
```

---

# SECTION 11: FACTION SPECIFIC ASSETS (40 items)

## 11.1 Faction A - Kingdom (Blue/Gold) - 10 items

### FA01 - Kingdom Banner
```
Game UI icon, blue kingdom banner with golden lion crest, royal heraldry, faction A symbol, noble design, dark background, 2D digital art
```

### FA02 - Kingdom Shield
```
Game UI icon, blue shield with golden crown emblem, kingdom faction crest, royal defense symbol, dark background, 2D digital art
```

### FA03 - Kingdom Emblem
```
Game UI emblem, golden lion on blue background, kingdom faction logo, royal insignia, circular design, dark background, 2D digital art
```

### FA04 - Kingdom Frame
```
Game UI panel frame, blue velvet with golden ornate border, kingdom faction style, royal elegant design, empty center, 2D digital art --ar 1:1
```

### FA05 - Kingdom Button
```
Game UI button, blue background with golden trim, kingdom faction style, royal button design, empty center, 2D digital art --ar 3:1
```

### FA06 - Kingdom Portrait Frame
```
Game UI portrait frame, blue and gold ornate border, kingdom faction style, character frame, empty center, 2D digital art --ar 3:4
```

### FA07 - Kingdom Victory Banner
```
Game UI victory screen banner, kingdom blue and gold, celebration design, win announcement, faction A style, 2D digital art --ar 3:1
```

### FA08 - Kingdom Territory Marker
```
Game UI map marker, blue flag with golden trim, kingdom territory indicator, map ownership symbol, dark background, 2D digital art
```

### FA09 - Kingdom Chat Badge
```
Game UI chat badge, small kingdom emblem, faction identifier, message accent, compact design, dark background, 2D digital art
```

### FA10 - Kingdom Loading Frame
```
Game UI loading screen frame, kingdom blue and gold theme, decorative border, loading content area, faction A style, 2D digital art --ar 16:9
```

## 11.2 Faction B - Empire (Red/Black) - 10 items

### FB01 - Empire Banner
```
Game UI icon, red empire banner with black eagle crest, military heraldry, faction B symbol, aggressive design, dark background, 2D digital art
```

### FB02 - Empire Shield
```
Game UI icon, red shield with black sword emblem, empire faction crest, military defense symbol, dark background, 2D digital art
```

### FB03 - Empire Emblem
```
Game UI emblem, black eagle on red background, empire faction logo, military insignia, circular design, dark background, 2D digital art
```

### FB04 - Empire Frame
```
Game UI panel frame, red background with black iron border, empire faction style, military design, empty center, 2D digital art --ar 1:1
```

### FB05 - Empire Button
```
Game UI button, red background with black trim, empire faction style, military button design, empty center, 2D digital art --ar 3:1
```

### FB06 - Empire Portrait Frame
```
Game UI portrait frame, red and black ornate border, empire faction style, character frame, empty center, 2D digital art --ar 3:4
```

### FB07 - Empire Victory Banner
```
Game UI victory screen banner, empire red and black, conquest design, win announcement, faction B style, 2D digital art --ar 3:1
```

### FB08 - Empire Territory Marker
```
Game UI map marker, red flag with black trim, empire territory indicator, map ownership symbol, dark background, 2D digital art
```

### FB09 - Empire Chat Badge
```
Game UI chat badge, small empire emblem, faction identifier, message accent, compact design, dark background, 2D digital art
```

### FB10 - Empire Loading Frame
```
Game UI loading screen frame, empire red and black theme, decorative border, loading content area, faction B style, 2D digital art --ar 16:9
```

## 11.3 Faction C - Forest Realm (Green/Brown) - 10 items

### FC01 - Forest Banner
```
Game UI icon, green forest banner with tree crest, nature heraldry, faction C symbol, organic design, dark background, 2D digital art
```

### FC02 - Forest Shield
```
Game UI icon, green shield with leaf emblem, forest faction crest, nature defense symbol, dark background, 2D digital art
```

### FC03 - Forest Emblem
```
Game UI emblem, great tree on green background, forest faction logo, nature insignia, circular design, dark background, 2D digital art
```

### FC04 - Forest Frame
```
Game UI panel frame, green background with wooden vine border, forest faction style, organic design, empty center, 2D digital art --ar 1:1
```

### FC05 - Forest Button
```
Game UI button, green background with brown trim, forest faction style, nature button design, empty center, 2D digital art --ar 3:1
```

### FC06 - Forest Portrait Frame
```
Game UI portrait frame, green and brown ornate border, forest faction style, character frame, empty center, 2D digital art --ar 3:4
```

### FC07 - Forest Victory Banner
```
Game UI victory screen banner, forest green and brown, nature celebration, win announcement, faction C style, 2D digital art --ar 3:1
```

### FC08 - Forest Territory Marker
```
Game UI map marker, green flag with brown trim, forest territory indicator, map ownership symbol, dark background, 2D digital art
```

### FC09 - Forest Chat Badge
```
Game UI chat badge, small forest emblem, faction identifier, message accent, compact design, dark background, 2D digital art
```

### FC10 - Forest Loading Frame
```
Game UI loading screen frame, forest green and brown theme, decorative border, loading content area, faction C style, 2D digital art --ar 16:9
```

## 11.4 Faction D - Arcane Order (Purple/Silver) - 10 items

### FD01 - Arcane Banner
```
Game UI icon, purple arcane banner with silver moon crest, magical heraldry, faction D symbol, mystical design, dark background, 2D digital art
```

### FD02 - Arcane Shield
```
Game UI icon, purple shield with crystal emblem, arcane faction crest, magical defense symbol, dark background, 2D digital art
```

### FD03 - Arcane Emblem
```
Game UI emblem, silver crescent moon on purple background, arcane faction logo, magical insignia, circular design, dark background, 2D digital art
```

### FD04 - Arcane Frame
```
Game UI panel frame, purple background with silver runic border, arcane faction style, mystical design, empty center, 2D digital art --ar 1:1
```

### FD05 - Arcane Button
```
Game UI button, purple background with silver trim, arcane faction style, magical button design, empty center, 2D digital art --ar 3:1
```

### FD06 - Arcane Portrait Frame
```
Game UI portrait frame, purple and silver ornate border, arcane faction style, character frame, empty center, 2D digital art --ar 3:4
```

### FD07 - Arcane Victory Banner
```
Game UI victory screen banner, arcane purple and silver, magical celebration, win announcement, faction D style, 2D digital art --ar 3:1
```

### FD08 - Arcane Territory Marker
```
Game UI map marker, purple flag with silver trim, arcane territory indicator, map ownership symbol, dark background, 2D digital art
```

### FD09 - Arcane Chat Badge
```
Game UI chat badge, small arcane emblem, faction identifier, message accent, compact design, dark background, 2D digital art
```

### FD10 - Arcane Loading Frame
```
Game UI loading screen frame, arcane purple and silver theme, decorative border, loading content area, faction D style, 2D digital art --ar 16:9
```

---

# SECTION 12: SEASONAL & EVENT ASSETS (24 items)

## 12.1 Spring/Easter Theme - 6 items

### SE01 - Spring Banner
```
Game UI banner, spring festival theme, flowers and butterflies, pastel colors, seasonal celebration, game event style, 2D digital art --ar 3:1
```

### SE02 - Spring Frame
```
Game UI panel frame, spring theme, flower border, pastel pink and green, seasonal design, empty center, 2D digital art --ar 4:3
```

### SE03 - Spring Icon
```
Game UI icon, blooming flower, spring event symbol, nature awakening, pastel colors, dark background, 2D digital art
```

### SE04 - Easter Egg Icon
```
Game UI icon, decorated easter egg, holiday event symbol, colorful patterns, festive design, dark background, 2D digital art
```

### SE05 - Spring Currency
```
Game UI icon, spring petal currency, seasonal event money, flower token, limited time, dark background, 2D digital art
```

### SE06 - Spring Chest
```
Game UI icon, spring themed reward chest, flower decorations, seasonal loot box, event reward, dark background, 2D digital art
```

## 12.2 Summer Theme - 6 items

### SE07 - Summer Banner
```
Game UI banner, summer festival theme, sun and waves, bright warm colors, seasonal celebration, game event style, 2D digital art --ar 3:1
```

### SE08 - Summer Frame
```
Game UI panel frame, summer theme, beach border, sunny yellow and blue, seasonal design, empty center, 2D digital art --ar 4:3
```

### SE09 - Summer Icon
```
Game UI icon, bright sun, summer event symbol, warm season, golden rays, dark background, 2D digital art
```

### SE10 - Beach Ball Icon
```
Game UI icon, colorful beach ball, summer holiday symbol, fun festive design, seasonal item, dark background, 2D digital art
```

### SE11 - Summer Currency
```
Game UI icon, seashell currency, seasonal event money, ocean token, limited time, dark background, 2D digital art
```

### SE12 - Summer Chest
```
Game UI icon, summer themed reward chest, beach decorations, seasonal loot box, event reward, dark background, 2D digital art
```

## 12.3 Autumn/Halloween Theme - 6 items

### SE13 - Autumn Banner
```
Game UI banner, autumn festival theme, falling leaves and pumpkins, orange warm colors, seasonal celebration, game event style, 2D digital art --ar 3:1
```

### SE14 - Autumn Frame
```
Game UI panel frame, autumn theme, leaf border, orange and brown, seasonal design, empty center, 2D digital art --ar 4:3
```

### SE15 - Halloween Icon
```
Game UI icon, jack-o-lantern pumpkin, halloween event symbol, spooky festive, orange glow, dark background, 2D digital art
```

### SE16 - Ghost Icon
```
Game UI icon, cute ghost, halloween symbol, spooky but friendly, festive design, dark background, 2D digital art
```

### SE17 - Autumn Currency
```
Game UI icon, maple leaf currency, seasonal event money, autumn token, limited time, dark background, 2D digital art
```

### SE18 - Halloween Chest
```
Game UI icon, halloween themed reward chest, spooky decorations, seasonal loot box, event reward, dark background, 2D digital art
```

## 12.4 Winter/Christmas Theme - 6 items

### SE19 - Winter Banner
```
Game UI banner, winter festival theme, snow and presents, blue and white colors, seasonal celebration, game event style, 2D digital art --ar 3:1
```

### SE20 - Winter Frame
```
Game UI panel frame, winter theme, snowflake border, icy blue and white, seasonal design, empty center, 2D digital art --ar 4:3
```

### SE21 - Christmas Icon
```
Game UI icon, christmas tree with star, holiday event symbol, festive green and gold, dark background, 2D digital art
```

### SE22 - Snowflake Icon
```
Game UI icon, crystal snowflake, winter symbol, icy design, sparkling detail, dark background, 2D digital art
```

### SE23 - Winter Currency
```
Game UI icon, snowflake currency, seasonal event money, winter token, limited time, dark background, 2D digital art
```

### SE24 - Christmas Chest
```
Game UI icon, christmas themed reward chest, wrapped present style, seasonal loot box, event reward, dark background, 2D digital art
```

---

# SUMMARY: TOTAL 2D/UI ASSETS

| Category | Count |
|----------|-------|
| Resource Icons | 32 |
| Action Icons | 48 |
| Navigation Icons | 24 |
| Status Icons | 24 |
| Building Type Icons | 20 |
| Rarity Frames | 15 |
| UI Panels | 24 |
| Achievement Icons | 30 |
| Emotes | 16 |
| Textures | 20 |
| Faction Assets | 40 |
| Seasonal Assets | 24 |
| **TOTAL** | **317** |

---

# PRODUCTION NOTES

## Midjourney Settings
- Always use `--v 6.1` for best quality
- Use `--style raw` for clean UI assets
- Add `--no text, words, letters, watermark`
- Use appropriate aspect ratios per asset type

## Batch Processing Workflow
1. Group by category for visual consistency
2. Generate 4 variations per prompt
3. Select best, upscale to max
4. Download and rename with ID prefix
5. Process in image editor if needed

## Post-Processing in Photoshop/GIMP
1. Remove any remaining backgrounds
2. Ensure transparency is clean
3. Resize to exact dimensions
4. Export as PNG-24 with transparency
5. Create 1x and 2x versions

## Unity Import Settings
```
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Filter Mode: Bilinear
Compression: High Quality
Max Size: 512 or 1024
```
