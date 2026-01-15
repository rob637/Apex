# Apex Citadels - Game Data Configuration

This folder contains all the game balance and configuration data in JSON format.

## Files

| File | Purpose |
|------|---------|
| `blocks.json` | Building block definitions, costs, and properties |
| `resources.json` | Resource types, nodes, biomes, and harvesting rules |
| `progression.json` | XP curve, level bonuses, milestones |
| `combat.json` | Attack types, defenses, damage calculations |
| `achievements.json` | All achievements with requirements and rewards |
| `daily-rewards.json` | Daily login rewards, streaks, seasonal events |
| `territories.json` | Territory levels, zones, control bonuses |

## Usage

### In Cloud Functions
```typescript
import blocks from '../gamedata/blocks.json';
import combat from '../gamedata/combat.json';

// Use in functions
const blockDef = blocks.blocks[blockType];
const attackDef = combat.attacks[attackType];
```

### In Unity
These files can be loaded via `Resources.Load<TextAsset>()` or fetched from Firebase Remote Config.

## Versioning

Each file has a `version` field. When updating:
1. Increment the version
2. Update `lastUpdated` date
3. Document changes in changelog

## Balance Guidelines

- Early game (Level 1-10): Focus on exploration and basic building
- Mid game (Level 11-30): Combat and alliance features unlock
- Late game (Level 31+): Advanced strategies and territory control

Resource costs should follow these ratios:
- Stone/Wood: Common (base cost)
- Metal: 3-4x rarer than stone
- Crystals: 10x rarer than stone
- Gems: Premium currency, earned sparingly

## Modifying Values

When adjusting balance:
1. Test changes in Firebase emulator first
2. Use A/B testing via Remote Config for significant changes
3. Announce major balance changes to players in advance
