# Apex Citadels - Game Design Document

> **Version:** 1.0  
> **Last Updated:** January 15, 2026  
> **Status:** Approved for Development

---

## Executive Summary

**Apex Citadels** is a location-based augmented reality game where players claim real-world territory, build persistent structures visible to all players, and engage in strategic combat to expand their domain.

**Core Fantasy:** *"I own a fortress at Central Park and I'm defending it from rivals right now."*

**Market Position:** The first game to combine persistent AR building + real-world territory ownership + meaningful combat + social alliance play.

---

## Table of Contents

1. [Core Philosophy](#core-philosophy)
2. [The Core Loop](#the-core-loop)
3. [Territory System](#territory-system)
4. [Building System](#building-system)
5. [Resource Economy](#resource-economy)
6. [Combat System](#combat-system)
7. [Siege & Loss Mechanics](#siege--loss-mechanics)
8. [Alliance System](#alliance-system)
9. [Progression Systems](#progression-systems)
10. [Victory Conditions](#victory-conditions)
11. [Monetization](#monetization)
12. [Safety & Moderation](#safety--moderation)
13. [Technical Parameters](#technical-parameters)

---

## Core Philosophy

### What Makes Apex Citadels Different

| Feature | Why It's Special |
|---------|------------------|
| **Real addresses matter** | "I own 123 Main Street" |
| **Physical presence rewarded** | Must BE there for full effectiveness |
| **Visible to everyone** | AR structures visible to all players |
| **Local rivalries** | School vs school, neighborhood vs neighborhood |
| **Persistent world** | Build today, still there next year |

### Design Pillars

1. **Real-World Stakes** - Your neighborhood, your territory, your pride
2. **Meaningful Conflict** - Battles have consequences, but aren't devastating
3. **Creative Expression** - Build YOUR citadel, YOUR way
4. **Social Connection** - Alliances matter, communities form
5. **Fair Play** - No pay-to-win, skill and dedication rewarded

### Theme: "Mythic Modern"

*"Magic has awakened. Ancient ley lines under our cities now power the construction of impossible structures. You're a Citadel Lord, claiming territory where magic is strongest."*

This provides:
- Fantasy aesthetics in real-world locations
- Narrative justification for AR structures
- Family-friendly but with real stakes
- Flexibility for medieval and modern content

---

## The Core Loop

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│   EXPLORE (walk around, find opportunities)                     │
│        ↓                                                        │
│   CLAIM (plant flag, own 25-50m radius)                        │
│        ↓                                                        │
│   GATHER (walking generates resources)                          │
│        ↓                                                        │
│   BUILD (walls, towers, defenses)                              │
│        ↓                                                        │
│   ┌─────────────────┐     ┌─────────────────┐                  │
│   │ DEFEND          │     │ ATTACK          │                  │
│   │ (protect yours) │ OR  │ (take theirs)   │                  │
│   └─────────────────┘     └─────────────────┘                  │
│        ↓                         ↓                              │
│   EARN REWARDS ←─────────────────┘                              │
│        ↓                                                        │
│   UPGRADE & EXPAND → (repeat)                                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Session Types

| Session | Duration | Activities |
|---------|----------|------------|
| **Quick Check** | 2-5 min | Claim daily rewards, check notifications |
| **Resource Run** | 10-20 min | Walk a route, collect resources |
| **Build Session** | 20-40 min | Design and upgrade citadel |
| **Battle** | 30-60 min | Scheduled territory attack/defense |
| **Alliance War** | 2-3 hours | Coordinated multi-territory campaign |

---

## Territory System

### Territory Size

| Environment | Claim Radius | Rationale |
|-------------|--------------|-----------|
| **Urban** (pop density > 5000/km²) | 25 meters | Higher density, more competition |
| **Suburban/Rural** | 50 meters | Lower density, larger claims |

Territory boundaries are calculated from the center point (flag location).

### Territory States

```
SECURE ──────→ CONTESTED ──────→ VULNERABLE ──────→ FALLEN
  ↑               (1 loss)         (2 losses)       (3 losses)
  │                                                     │
  └─────────────── RECLAIM (24hr cooldown) ─────────────┘
```

| State | Effects |
|-------|---------|
| **Secure** | 100% resource production, full defense bonus |
| **Contested** | 80% resource production, visible "contested" marker |
| **Vulnerable** | 50% resource production, structures show damage |
| **Fallen** | Lost ownership, 24-hour reclaim cooldown |

### Claiming Territory

**Requirements to claim:**
- Physical presence at location
- Sufficient resources (100 Stone, 50 Wood to plant flag)
- Location not already claimed
- Not within another territory's radius

**Claim Limits by Level:**

| Player Level | Max Territories |
|--------------|-----------------|
| 1-10 | 1 |
| 11-25 | 2 |
| 26-50 | 3 |
| 51-75 | 4 |
| 76-100 | 5 |

### Territory Value Factors

| Factor | Bonus | Example |
|--------|-------|---------|
| **Foot Traffic** | +10-50% resources | Busy park vs quiet alley |
| **Landmark** | +25% prestige points | Near monument, POI |
| **Water Proximity** | Fishing resources | Rivers, lakes, coast |
| **Park/Green Space** | Nature resources | Public parks |
| **Urban/Commercial** | Metal/Tech resources | Downtown areas |

---

## Building System

### Building Scale

| Structure Type | Scale | Size Range |
|----------------|-------|------------|
| **Personal Buildings** | Tabletop | 1-3 meters |
| **Territory Landmarks** | Medium | 3-10 meters |
| **World Event Structures** | Life-sized | 10-30+ meters |

Players build at tabletop scale by default. Life-sized structures appear during special events.

### Building Categories

| Category | Purpose | Examples |
|----------|---------|----------|
| **Core** | Required, central structure | Citadel Core, Command Tower |
| **Defense** | Protect territory | Walls, Gates, Guard Towers |
| **Production** | Generate resources | Mine, Lumber Mill, Arcane Well |
| **Military** | Create/upgrade troops | Barracks, Archery Range, Stable |
| **Utility** | Special functions | Storage, Teleport Pad, Beacon |
| **Decoration** | Cosmetic | Banners, Statues, Gardens |

### Building Limits

Each territory has building limits based on Citadel Core level:

| Core Level | Building Slots | Defense Structures | Production Buildings |
|------------|----------------|--------------------|--------------------|
| 1 | 5 | 2 | 1 |
| 2 | 8 | 3 | 2 |
| 3 | 12 | 5 | 3 |
| 4 | 16 | 7 | 4 |
| 5 (max) | 20 | 10 | 5 |

### Visibility Layers

| Layer | Who Sees | Content |
|-------|----------|---------|
| **Public** | All players | Territory flags, alliance banners, landmarks |
| **Alliance** | Guild members | Strategic structures, resource caches, trap layouts |
| **Personal** | Owner only | Decorations, experiments, hidden elements |

---

## Resource Economy

### Resource Types (5)

| Resource | Source | Primary Use |
|----------|--------|-------------|
| **Stone** | Urban areas, mountains | Walls, foundations, basic structures |
| **Wood** | Parks, forests | Buildings, siege equipment |
| **Iron** | Industrial areas | Weapons, armor, advanced defenses |
| **Crystal** | Special nodes, events | Upgrades, enchantments, rare items |
| **Arcane Essence** | Ley lines, world events | Magic structures, troop abilities, premium items |

### Resource Acquisition

| Method | Rate | Notes |
|--------|------|-------|
| **Walking** | 1 resource per 50m walked | Background collection |
| **Resource Nodes** | 10-50 per node | Spawn near territories |
| **Production Buildings** | 5-20 per hour | Passive income |
| **World Events** | 100-500 per event | Time-limited bonuses |
| **Battle Rewards** | Variable | Victory spoils |
| **Daily Rewards** | Increasing scale | Login streak bonuses |

### Resource Storage

| Structure | Capacity | Protection |
|-----------|----------|------------|
| **Basic Storage** | 1,000 each | 0% protected on loss |
| **Fortified Vault** | 2,500 each | 50% protected on loss |
| **Alliance Treasury** | 10,000 shared | 75% protected on loss |

When territory falls, unprotected resources can be looted (up to 50% of stored amount).

---

## Combat System

### Combat Model: Turn-Based Strategy

Combat uses a turn-based tactical system similar to Clash of Clans, but with more strategic depth.

### Battle Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. SCHEDULING (24 hours before)                                 │
│    Attacker selects target, schedules battle time               │
│    Defender receives notification                               │
├─────────────────────────────────────────────────────────────────┤
│ 2. PREPARATION (24 hour window)                                 │
│    Both sides: reinforce, gather allies, set formations         │
├─────────────────────────────────────────────────────────────────┤
│ 3. DEPLOYMENT (battle start)                                    │
│    Attacker deploys troops in waves                             │
│    Defender activates traps, positions reserves                 │
├─────────────────────────────────────────────────────────────────┤
│ 4. BATTLE (30 minute window, 10 rounds)                         │
│    Turn-based combat resolution                                 │
│    Both players issue commands each round                       │
├─────────────────────────────────────────────────────────────────┤
│ 5. RESOLUTION                                                   │
│    Victory/defeat determined                                    │
│    Rewards distributed, state changes applied                   │
│    4-hour shield activated                                      │
└─────────────────────────────────────────────────────────────────┘
```

### Troop Types (6 Base Units)

| Unit | Role | Strong Against | Weak Against |
|------|------|----------------|--------------|
| **Infantry** | Front line, balanced | Archers | Cavalry |
| **Archer** | Ranged damage | Cavalry | Infantry rush |
| **Cavalry** | Fast flanking | Archers, Siege | Infantry, Guardians |
| **Siege** | Structure damage | Buildings | All troops |
| **Mage** | Area damage, debuffs | Groups | Single targets |
| **Guardian** | Defense, protection | Cavalry | Siege, Mages |

### Participation Effectiveness

| Presence | Combat Power | Rationale |
|----------|--------------|-----------|
| **Physical (at location)** | 100% | Rewarded for showing up |
| **Remote (within 1km)** | 75% | Nearby but not confrontational |
| **Remote (anywhere)** | 50% | Can always participate |

This ensures:
- Physical presence is rewarded
- Nobody is REQUIRED to physically confront opponents
- Time zone / schedule differences are manageable

### Victory Conditions (Battle)

| Condition | Result |
|-----------|--------|
| **Destroy Citadel Core** | Attacker wins (decisive) |
| **50%+ structures destroyed** | Attacker wins (standard) |
| **Defend for 10 rounds** | Defender wins |
| **Destroy 70%+ attacking force** | Defender wins (decisive) |

---

## Siege & Loss Mechanics

### The Three-Strike System

Territories require **3 battle losses** to fall, providing:
- Multiple chances to defend
- Time to rally allies
- Stakes without devastation

### What Happens When Territory Falls

| What You LOSE | What You KEEP |
|---------------|---------------|
| Territory ownership | All cosmetic items |
| 50% of unprotected stored resources | Your player level/XP |
| Current structure arrangement | **Blueprint of your citadel** |
| That physical location (temporarily) | All troops (rebuilt over time) |

### The Reclaim Mechanic

```
TERRITORY FALLS
    ↓
24-hour cooldown begins (neither side can act)
    ↓
After 24 hours:
    ├── RECLAIM (if unoccupied): Pay 30% of original build cost
    ├── CHALLENGE (if enemy occupied): Attack to take it back
    └── RELOCATE: Claim new territory elsewhere, use saved blueprint
```

### Protection Systems

**Post-Battle Shield:**
- 4 hours after ANY battle (win or lose)
- Cannot be attacked
- Cannot attack others

**Activity-Based Protection:**

| Status | Protection |
|--------|------------|
| Active (played in last 24h) | Normal vulnerability |
| Away (2-3 days) | +50% defense bonus |
| Inactive (7+ days) | "Abandoned" status - can be claimed, no loot |
| New player (first 7 days) | "Newcomer Shield" - immune to attacks |

**Proportional Matchmaking:**
- If attacker is 2x+ stronger: -50% rewards
- If attacker is 5x+ stronger: Battle blocked

**Attack Cooldowns:**
- Same attacker → same territory: 48-hour cooldown
- Same alliance → same territory: 24-hour cooldown

---

## Alliance System

### Alliance Structure

| Role | Permissions |
|------|-------------|
| **Leader** | Full control, disband, promote officers |
| **Officer** | Invite/kick members, start wars, manage treasury |
| **Veteran** | Access alliance buildings, participate in wars |
| **Member** | Basic membership, chat, shared visibility |

### Alliance Size

**Initial cap: 10 members**

Can be increased via:
- Alliance level upgrades
- Special achievements
- Premium alliance features

### Alliance Features

| Feature | Description |
|---------|-------------|
| **Shared Visibility** | See alliance members' strategic structures |
| **Alliance Chat** | Private communication channel |
| **Treasury** | Shared resource pool for alliance projects |
| **Alliance Territory** | Combine adjacent territories for bonuses |
| **War Declaration** | Formal warfare system (see below) |
| **Alliance Leaderboard** | Compete for regional/global rankings |

### Alliance War System

```
WAR DECLARATION
    ↓
Cost: 500 of each resource from treasury
    ↓
24-HOUR WARNING
    ↓
Enemy alliance notified, both sides prepare
    ↓
WAR PERIOD (48 hours)
    ↓
All battles between alliances have:
  • 2x territory stakes
  • 2x rewards
  • War contribution tracking
    ↓
WAR ENDS
    ↓
72-HOUR PEACE TREATY (mandatory cooldown)
```

---

## Progression Systems

### Player Level (Cap: 100)

XP Sources:
- Territory claiming
- Building construction
- Battle victories
- Daily activities
- Achievements
- World events

Level Unlocks:
| Level | Unlock |
|-------|--------|
| 5 | PvP combat enabled |
| 10 | Second territory slot, Alliance creation |
| 15 | Advanced troop types |
| 20 | Alliance Officer eligibility |
| 25 | Third territory slot |
| 30+ | Prestige cosmetics, advanced features |

### Season Pass (100 Tiers, 10-Week Season)

| Track | Price | Rewards |
|-------|-------|---------|
| **Free** | $0 | Basic rewards every 10 tiers |
| **Premium** | $9.99 | Every tier, exclusive cosmetics |
| **Premium+** | $24.99 | Premium + 20 tier skip + exclusive title |

Weekly XP target: ~10 tiers (achievable through daily play)

### Daily Rewards

7-day cycle with increasing value:
| Day | Reward |
|-----|--------|
| 1 | 100 Stone |
| 2 | 100 Wood |
| 3 | 50 Iron |
| 4 | 25 Crystal |
| 5 | 10 Arcane Essence |
| 6 | Random troop |
| 7 | Premium chest |

Monthly bonus: Complete all 4 weeks = Legendary cosmetic

### Achievements

Categories:
- Territory (claim, defend, expand)
- Building (construct, upgrade, variety)
- Combat (victories, troops, strategies)
- Social (alliance, friends, referrals)
- Collection (cosmetics, blueprints, units)
- Exploration (distance walked, locations visited)

---

## Victory Conditions

### Seasonal Victory

Each 10-week season has global and regional leaderboards:

| Leaderboard | Metric | Reset |
|-------------|--------|-------|
| **Territory Points** | Total territory value held | End of season |
| **War Score** | Alliance battle performance | End of season |
| **Builder's Pride** | Building complexity/beauty | End of season |

Season rewards:
- Exclusive cosmetics
- Title badges
- Head start bonuses for next season

### Regional Control (Persistent)

| Scale | Achievement |
|-------|-------------|
| **Neighborhood Lord** | Control 5+ adjacent territories |
| **District Champion** | Control most territory in a district |
| **City Ruler** | Alliance controls most territory in a city |
| **Regional Legend** | Maintained City Ruler for full season |

Regional control provides:
- Permanent display on world map
- Prestige titles
- Alliance recruitment bonuses
- Featured in game news/social

---

## Monetization

### Revenue Streams

#### 1. Season Pass (Primary Revenue)

| Tier | Price | Content |
|------|-------|---------|
| Free | $0 | Basic rewards every 10 tiers |
| Premium | $9.99/season | Every tier, exclusive cosmetics |
| Premium+ | $24.99 | Premium + 20 tier skip + exclusive title |

#### 2. Cosmetics (Whale Revenue)

- Building skins (Gothic, Crystal, Floating, etc.)
- Avatar customization
- Banner/flag designs
- Victory effects
- Alliance themes
- Troop skins

**Pricing:** $1.99 - $19.99 per item, bundles available

#### 3. Convenience (Light Monetization)

| Item | Price | Effect |
|------|-------|--------|
| Resource Booster (2x, 1hr) | $0.99 | Double resource collection |
| Territory Shield (8hr) | $1.99 | Prevent attacks |
| Instant Construction | $0.99-4.99 | Skip build timers |
| Troop Refresh | $0.99 | Instant troop recovery |

#### 4. VIP Subscription (Recurring Revenue)

| Tier | Price | Benefits |
|------|-------|----------|
| Bronze | $4.99/mo | 100 daily gems, queue skip, +10% resources |
| Silver | $9.99/mo | Above + extra territory slot, exclusive shop |
| Gold | $19.99/mo | Above + priority events, custom banner |

### What We DON'T Monetize (Fair Play)

- ❌ Territory claiming (must walk to claim)
- ❌ Combat power advantage (no P2W weapons/troops)
- ❌ Essential features (all core gameplay free)
- ❌ Matchmaking advantage
- ❌ Information advantage (no paid scouting)

---

## Safety & Moderation

### Real-World Safety Design

#### No Real-Time Location Sharing
```
❌ NEVER show: "Player123 is at this location RIGHT NOW"
✅ DO show: "This territory was last attacked 2 hours ago"
```

#### Battle Scheduling (Not Real-Time Confrontation)
- All battles scheduled 24 hours in advance
- Both players participate remotely or in-person
- No requirement to physically confront opponents
- Time-shifted participation supported

#### Reporting System
- Harassment reports
- Real-world threat reports (escalated immediately)
- Inappropriate structure reports
- Cheating reports

**Zero tolerance:** Threatening real-world action = instant permanent ban

### Age & Content

| Requirement | Implementation |
|-------------|----------------|
| Minimum age | 13+ (age gate on signup) |
| PvP unlock | Level 5 (not instant) |
| Code of Conduct | Required acceptance before first PvP |
| Chat moderation | Profanity filter + human review |
| Structure moderation | AI + human review for inappropriate builds |

### GDPR Compliance

- Full data export on request
- Account deletion with data purge
- Consent management for each data type
- Parental controls for 13-17 age group

---

## Technical Parameters

### Core Parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| Territory radius (urban) | 25 meters | Population density > 5000/km² |
| Territory radius (rural) | 50 meters | Population density ≤ 5000/km² |
| Max territories per player | 5 (at level 100) | Scales with level |
| Alliance size (initial) | 10 members | Expandable |
| Battle duration | 30 minutes | 10 rounds |
| Post-battle shield | 4 hours | Both sides |
| Reclaim cooldown | 24 hours | After territory falls |
| Season duration | 10 weeks | ~70 days |
| Season pass tiers | 100 | |
| Player level cap | 100 | |
| Building types | 15 base + expansions | |
| Troop types | 6 base | Infantry, Archer, Cavalry, Siege, Mage, Guardian |
| Resource types | 5 | Stone, Wood, Iron, Crystal, Arcane Essence |

### Visibility Ranges

| Item | Visible From |
|------|--------------|
| Territory flag | 500 meters |
| Full structure details | 100 meters |
| AR rendering | 50 meters |
| Interaction range | 25 meters |

### Timing Parameters

| Action | Duration/Cooldown |
|--------|-------------------|
| Resource node respawn | 30 minutes |
| Troop training | 5-60 minutes per unit |
| Building construction | 1 minute - 24 hours |
| Battle scheduling notice | 24 hours |
| Same-target attack cooldown | 48 hours |
| War declaration warning | 24 hours |
| War duration | 48 hours |
| Post-war peace treaty | 72 hours |
| Daily reward reset | 00:00 UTC |
| World event frequency | 2-3 per week |

---

## Appendix A: Day-in-the-Life Scenarios

### Casual Player (Sarah, 25, plays 20 min/day)

**Morning commute (7:30 AM):**
- Opens app, claims daily reward
- Walks past 3 resource nodes on way to subway
- Collects 150 Stone, 80 Wood passively
- Checks territory - still secure, 4 hours of production waiting

**Lunch break (12:15 PM):**
- Push notification: "World Event starting nearby!"
- Walks 2 blocks to event zone
- Participates for 15 minutes, earns 200 Crystal
- Checks Season Pass - 2 more tiers this week

**Evening (8:00 PM):**
- Couch time: upgrades wall to level 3
- Reviews alliance chat - war declared for tomorrow
- Sets defense formation remotely
- Closes app, 18 minutes total play time

### Hardcore Player (Marcus, 19, plays 2+ hours/day)

**After class (3:00 PM):**
- Walks campus route collecting resources (45 min)
- Claims new territory near library
- Designs citadel layout, places 5 structures
- Coordinates with alliance for evening war

**Evening war (7:00 PM):**
- Physically present at contested territory
- Commands troops in battle (100% effectiveness)
- Alliance wins, captures 2 enemy territories
- Post-battle strategy meeting in voice chat

**Night session (10:00 PM):**
- Reviews analytics from battle
- Adjusts troop compositions
- Scouts enemy territories for tomorrow
- Completes daily challenges

### Alliance Leader (Jordan, 31, manages guild)

**Throughout day:**
- Monitors alliance chat, approves 2 join requests
- Reviews territory map, identifies expansion targets
- Coordinates war timing with officer team
- Manages alliance treasury, funds shared projects
- Represents alliance in cross-alliance diplomacy

---

## Appendix B: Competitive Integrity

### Anti-Cheat Measures

| Threat | Countermeasure |
|--------|----------------|
| GPS spoofing | Location validation via cell towers, WiFi, accelerometer |
| Multi-accounting | Device fingerprinting, behavioral analysis |
| Botting | CAPTCHA for suspicious activity, pattern detection |
| Exploits | Server-authoritative game state, client validation |

### Fair Play Guidelines

1. One account per person
2. No GPS manipulation
3. No third-party tools
4. No real-money trading of accounts
5. No harassment or threats
6. Report violations, don't retaliate

Violations result in:
- First offense: 7-day suspension
- Second offense: 30-day suspension
- Third offense: Permanent ban
- Real-world threats: Immediate permanent ban + law enforcement report

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | January 15, 2026 | Initial approved design |

---

*This document represents the approved game design for Apex Citadels. All implementation should reference this document for design decisions.*
