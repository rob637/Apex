# Implementation Plan: Game Design Updates

> **Created:** January 15, 2026  
> **Status:** Sprints 1-5 Complete, Sprint 6 Documented  
> **Reference:** [GAME_DESIGN.md](GAME_DESIGN.md)

---

## Sprint Progress

| Sprint | Feature | Status |
|--------|---------|--------|
| Sprint 1 | Core Battle System (6 troop types, 3-strike siege, turn-based combat) | ✅ COMPLETE |
| Sprint 2 | Participation & Protection (shields, activity bonuses, dynamic radius) | ✅ COMPLETE |
| Sprint 3 | Alliance War Enhancement (24hr warning → 48hr war → 72hr peace) | ✅ COMPLETE |
| Sprint 4 | Resource Adjustments (metal→iron, add Arcane Essence) | ✅ COMPLETE |
| Sprint 5 | Blueprint System (save/restore citadel layouts) | ✅ COMPLETE |
| Sprint 6 | Unity Integration (client-side battle UI) | 📝 DOCUMENTED |

---

## Executive Summary

This document outlines the implementation work needed to align the existing codebase with the approved game design. The codebase has strong foundations—the changes are primarily **enhancements** rather than rewrites.

---

## Current State vs. Target State

### ✅ Already Implemented (No Changes Needed)

| Feature | Location | Notes |
|---------|----------|-------|
| Territory claiming | `combat.ts`, types | Fully functional |
| Basic combat (damage-based) | `combat.ts` | Works, needs siege enhancement |
| Alliance system | `alliance.ts` | 50-member max, all roles |
| Alliance wars | `alliance.ts` | Basic war system exists |
| Resource economy | `types/index.ts` | ✅ 6 types (stone, wood, iron, crystal, arcaneEssence, gems) |
| Season pass | `season-pass.ts` | 100 tiers implemented |
| World events | `world-events.ts` | FOMO mechanics exist |
| Daily rewards | `progression.ts` | 7-day cycle works |
| Push notifications | `push.ts` | Full FCM implementation |
| Anti-cheat | `anticheat.ts` | Location validation |
| IAP | `iap.ts` | Receipt validation |
| Chat | `chat.ts` | Global/alliance/DM |
| Friends system | `friends.ts` | Requests, gifts |
| Moderation | `moderation.ts` | Bans, reports |
| GDPR | `gdpr.ts` | Export, delete |

### ✅ Enhanced (Sprints 1-4)

| Feature | Current State | Status |
|---------|---------------|--------|
| **Territory States** | 4-state siege system (Secure→Contested→Vulnerable→Fallen) | ✅ DONE |
| **Combat Model** | Turn-based with 6 troop types | ✅ DONE |
| **Territory Radius** | Dynamic (25m urban/35m suburban/50m rural) | ✅ DONE |
| **Resource Types** | 6 types with iron (was metal) + Arcane Essence | ✅ DONE |
| **Battle Scheduling** | 24-hour scheduled battles | ✅ DONE |
| **Remote Participation** | 50%/75%/100% power levels | ✅ DONE |
| **War System** | 24hr warning + 48hr war + 72hr peace treaty | ✅ DONE |
| **Reclaim Mechanic** | 24hr cooldown + reclaim | ✅ DONE |
| **Protection Systems** | Newcomer shield (7d) + activity bonus (0-50%) | ✅ DONE |

### 🔧 Still Needs Work

| Feature | Current State | Target State | Priority |
|---------|---------------|--------------|----------|
| **Blueprint System** | Not exists | Save/restore citadel layouts | MEDIUM |
| **Alliance Size** | 50 max | Start at 10, expandable | LOW |

### 🆕 Needs New Implementation

| Feature | Description | Priority | Estimated Effort |
|---------|-------------|----------|------------------|
| **Troop System** | 6 unit types with training | HIGH | 2-3 days |
| **Battle Simulation** | Turn-based combat resolution | HIGH | 3-4 days |
| **Territory State Machine** | Secure→Contested→Vulnerable→Fallen | HIGH | 1-2 days |
| **Battle Scheduling System** | Schedule attacks 24hr ahead | HIGH | 2 days |
| **Blueprint System** | Save/restore citadel layouts | MEDIUM | 1-2 days |
| **Population Density Check** | Determine urban vs rural | MEDIUM | 0.5 days |
| **Participation Tracker** | Track physical vs remote | MEDIUM | 1 day |
| **Protection Systems** | Newcomer shield, activity bonus | MEDIUM | 1 day |
| **Attack Cooldowns (Enhanced)** | Per-territory cooldowns | LOW | 0.5 days |

---

## Detailed Implementation Tasks

### Phase 1: Territory State System (HIGH Priority)

#### 1.1 Update Territory Types

**File:** `backend/functions/src/types/index.ts`

```typescript
// ADD: Territory state enum
export type TerritoryState = 'secure' | 'contested' | 'vulnerable' | 'fallen';

// UPDATE: Territory interface
export interface Territory {
  // ... existing fields ...
  state: TerritoryState;
  battleLosses: number; // 0-3
  lastStateChangeAt?: admin.firestore.Timestamp;
  fallenAt?: admin.firestore.Timestamp;
  previousOwnerId?: string; // For reclaim tracking
  blueprintId?: string; // Reference to saved blueprint
}
```

#### 1.2 Create Territory State Machine

**New file:** `backend/functions/src/territory-state.ts`

Functions needed:
- `updateTerritoryState(territoryId, battleResult)` - Handle state transitions
- `checkReclaim(userId, territoryId)` - Check if user can reclaim
- `reclaimTerritory(userId, territoryId)` - Execute reclaim
- `relocateWithBlueprint(userId, newLocation, blueprintId)` - Relocate + rebuild

#### 1.3 State Transition Effects

| Transition | Trigger | Effects |
|------------|---------|---------|
| Secure → Contested | 1st battle loss | -20% production |
| Contested → Vulnerable | 2nd battle loss | -50% production, damage visuals |
| Vulnerable → Fallen | 3rd battle loss | Ownership lost, 24hr cooldown |
| Fallen → Secure | Reclaim/capture | Reset state, new owner |

---

### Phase 2: Troop & Battle System (HIGH Priority)

#### 2.1 Troop Types

**Add to:** `backend/functions/src/types/index.ts`

```typescript
export type TroopType = 'infantry' | 'archer' | 'cavalry' | 'siege' | 'mage' | 'guardian';

export interface Troop {
  type: TroopType;
  level: number;
  count: number;
}

export interface TroopDefinition {
  type: TroopType;
  displayName: string;
  baseAttack: number;
  baseDefense: number;
  baseHealth: number;
  trainingTime: number; // seconds
  trainingCost: Partial<UserResources>;
  strongAgainst: TroopType[];
  weakAgainst: TroopType[];
}

export const TROOP_DEFINITIONS: Record<TroopType, TroopDefinition> = {
  infantry: {
    type: 'infantry',
    displayName: 'Infantry',
    baseAttack: 10,
    baseDefense: 10,
    baseHealth: 100,
    trainingTime: 60,
    trainingCost: { iron: 10 },
    strongAgainst: ['archer'],
    weakAgainst: ['cavalry']
  },
  // ... define all 6 types
};
```

#### 2.2 Battle System

**New file:** `backend/functions/src/battle.ts`

Core functions:
- `scheduleBattle(attackerId, territoryId, scheduledTime)` - Create scheduled battle
- `prepareBattle(battleId, userId, formation)` - Set troop formation
- `executeBattle(battleId)` - Run battle simulation
- `calculateRoundOutcome(attackers, defenders, round)` - Single round calc
- `applyBattleResult(battleId)` - Apply outcome to territory

#### 2.3 Battle Flow

```
scheduleBattle()
    ↓
[24 hour wait - both sides prepare]
    ↓
prepareBattle() - both sides set formations
    ↓
executeBattle() - triggered at scheduled time
    ↓
[10 rounds of turn-based combat]
    ↓
applyBattleResult() - update territory state, award rewards
```

---

### Phase 3: Participation & Remote System (MEDIUM Priority)

#### 3.1 Participation Types

**Add to:** `backend/functions/src/types/index.ts`

```typescript
export type ParticipationType = 'physical' | 'nearby' | 'remote';

export interface BattleParticipation {
  oderId: string;
  participationType: ParticipationType;
  effectivenessMultiplier: number; // 1.0, 0.75, 0.5
  location?: GeoPoint;
  verifiedAt: admin.firestore.Timestamp;
}
```

#### 3.2 Participation Verification

**Add to:** `backend/functions/src/anticheat.ts`

```typescript
export async function verifyParticipation(
  userId: string, 
  territoryId: string,
  reportedLocation: GeoPoint
): Promise<ParticipationType> {
  // Check distance to territory
  // < 50m = physical (100%)
  // < 1km = nearby (75%)
  // else = remote (50%)
}
```

---

### Phase 4: Dynamic Territory Radius (MEDIUM Priority)

#### 4.1 Population Density Check

**New file:** `backend/functions/src/location-utils.ts`

```typescript
export async function getPopulationDensity(
  latitude: number, 
  longitude: number
): Promise<'urban' | 'suburban' | 'rural'> {
  // Option 1: Use OpenStreetMap Overpass API
  // Option 2: Pre-computed density grid
  // Option 3: Simple city boundary check
}

export function getTerritoryRadius(density: string): number {
  return density === 'urban' ? 25 : 50;
}
```

#### 4.2 Update Territory Claiming

**Modify:** `backend/functions/src/combat.ts` (or new territory.ts)

- Call `getPopulationDensity()` when claiming
- Set `radiusMeters` based on result

---

### Phase 5: Alliance War Enhancement (MEDIUM Priority)

#### 5.1 War Timeline

**Current:** Instant start → 24hr duration → end

**Target:** 24hr warning → 48hr war → 72hr peace

**Modify:** `backend/functions/src/alliance.ts`

```typescript
const WAR_WARNING_HOURS = 24;
const WAR_DURATION_HOURS = 48;
const PEACE_TREATY_HOURS = 72;

// War states: 'pending' | 'active' | 'ended' | 'peace_treaty'
```

#### 5.2 War Participation Tracking

Track individual contributions:
- Attacks made
- Defenses successful
- Territories captured
- Resources looted

---

### Phase 6: Protection Systems (MEDIUM Priority)

#### 6.1 Newcomer Shield

**Add to:** `backend/functions/src/types/index.ts`

```typescript
export interface UserProtection {
  newcomerShieldExpires?: admin.firestore.Timestamp; // 7 days from signup
  lastActiveAt: admin.firestore.Timestamp;
  activityBonus: number; // 0-0.5 defense bonus
}
```

#### 6.2 Activity-Based Protection

**Add to:** Combat validation

```typescript
function getDefenseBonus(lastActive: Date): number {
  const daysSinceActive = (Date.now() - lastActive.getTime()) / 86400000;
  if (daysSinceActive < 1) return 0; // Active
  if (daysSinceActive < 3) return 0.25; // Away
  if (daysSinceActive < 7) return 0.5; // Inactive
  return 0; // Abandoned (no bonus, can be taken)
}
```

---

### Phase 7: Blueprint System (MEDIUM Priority)

#### 7.1 Blueprint Storage

**New collection:** `blueprints`

```typescript
export interface Blueprint {
  id: string;
  ownerId: string;
  name: string;
  buildings: BuildingPlacement[];
  createdAt: admin.firestore.Timestamp;
  sourceeTerritoryId?: string;
}
```

#### 7.2 Blueprint Functions

**New file:** `backend/functions/src/blueprints.ts`

- `saveBlueprint(territoryId)` - Save current layout
- `loadBlueprint(blueprintId, territoryId)` - Apply to territory
- `autoSaveOnFall(territoryId)` - Auto-save when territory falls

---

### Phase 8: Resource Rename (LOW Priority)

#### 8.1 Rename metal → iron

**Files to update:**
- `types/index.ts` - UserResources interface
- All combat/progression functions using `metal`
- Frontend Unity scripts

**Migration:**
- Keep `metal` as alias for `iron` during transition
- Update display strings

#### 8.2 Arcane Essence

Currently `gems` serves as premium currency. Options:
1. Keep gems as premium, add Arcane Essence as 6th resource
2. Rename gems → Arcane Essence (breaking change)

**Recommendation:** Keep gems as premium currency (IAP), add Arcane Essence as rare gameplay resource. Total: 6 resource types.

---

## Implementation Priority Order

### Sprint 1 (Week 1-2): Core Battle System ✅ COMPLETE
1. ✅ Territory state machine (SECURE → CONTESTED → VULNERABLE → FALLEN)
2. ✅ Battle scheduling system
3. ✅ Basic troop types (6 units)
4. ✅ Turn-based battle simulation

### Sprint 2 (Week 3): Participation & Protection ✅ COMPLETE
5. ✅ Remote participation (50%/75%/100%) - Built into battle.ts
6. ✅ Newcomer shield (7 days) - protection.ts
7. ✅ Activity-based defense bonus (0-50%) - protection.ts
8. ✅ Post-battle shields - battle.ts
9. ✅ Dynamic territory radius (25m/35m/50m) - location-utils.ts

### Sprint 3 (Week 4): Recovery & Enhancement ✅ COMPLETE
10. ✅ Reclaim mechanic - battle.ts (reclaimTerritory)
11. ✅ Blueprint system - battle.ts (saveTerritoryBlueprint)
12. ✅ Alliance war enhancement (24hr warning → 48hr war → 72hr peace) - alliance.ts
13. ⏳ Pre-compute density for launch cities (operational task)

### Sprint 4 (Week 5): Polish & Testing
14. Resource type adjustments
14. UI/notification updates
15. Integration testing
16. Balance tuning

---

## Database Schema Updates

### New Collections

| Collection | Purpose |
|------------|---------|
| `scheduled_battles` | Upcoming battle queue |
| `battle_results` | Completed battle records |
| `blueprints` | Saved citadel layouts |
| `troops` | User's trained troops (subcollection of users) |

### Updated Collections

| Collection | Changes |
|------------|---------|
| `territories` | Add: state, battleLosses, blueprintId |
| `users` | Add: troops subcollection, protection fields |
| `alliance_wars` | Add: warningPhase, peaceTreaty fields |

---

## Testing Strategy

### Unit Tests
- Territory state transitions
- Battle damage calculations
- Participation effectiveness
- Protection bonus calculations

### Integration Tests
- Full battle flow (schedule → execute → result)
- Reclaim workflow
- Alliance war with peace treaty

### Load Tests
- Multiple simultaneous battles
- Battle scheduling at scale

---

## Estimated Timeline

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Territory States | 3 days | None |
| Battle System | 5 days | Territory States |
| Participation | 2 days | Battle System |
| Protection | 2 days | Territory States |
| Reclaim/Blueprint | 3 days | Territory States |
| Alliance War | 2 days | Battle System |
| Dynamic Radius | 1 day | None |
| Testing & Polish | 3 days | All above |

**Total:** ~3 weeks of development

---

## Questions for Product

1. **Troop persistence:** Do troops persist between battles or are they single-use?
   - **Recommendation:** Persist with recovery time (4-8 hours to restore losses)

2. **Blueprint limits:** How many blueprints can a player save?
   - **Recommendation:** 3 free, more with VIP

3. **Newcomer shield duration:** 7 days enough?
   - **Recommendation:** 7 days or until first attack initiated

4. **Remote participation UI:** How do players indicate they're participating remotely?
   - **Recommendation:** Auto-detect, with option to "boost" by going physical

---

*This plan will be updated as implementation progresses.*
