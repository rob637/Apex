# Mixamo Character Animation List

## Overview
Mixamo (mixamo.com) provides free character animations that can be applied to humanoid characters. This document lists all required animations for Apex Citadels with recommended Mixamo animation names.

## Instructions
1. Go to https://mixamo.com/ (free Adobe account required)
2. Upload your character model OR use a Mixamo character
3. Search for animation names below
4. Download in FBX format (with skin for first animation, without skin for rest)
5. Configure "In Place" for movement animations to work with root motion

---

# SECTION 1: LOCOMOTION ANIMATIONS (28 animations)

## 1.1 Walking Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-LOC01 | Walk Forward | "Walking" | Yes | Yes | Standard walk cycle |
| ANI-LOC02 | Walk Backward | "Walking Backwards" | Yes | Yes | Retreat movement |
| ANI-LOC03 | Walk Left | "Left Strafe Walk" | Yes | Yes | Lateral movement |
| ANI-LOC04 | Walk Right | "Right Strafe Walk" | Yes | Yes | Lateral movement |
| ANI-LOC05 | Walk Injured | "Injured Walk" | Yes | Yes | Low health walk |
| ANI-LOC06 | Walk Sneaking | "Sneaking" | Yes | Yes | Stealth movement |
| ANI-LOC07 | Walk Confident | "Confident Walk" | Yes | Yes | Victory/proud walk |

## 1.2 Running Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-LOC08 | Run Forward | "Running" | Yes | Yes | Standard run |
| ANI-LOC09 | Run Backward | "Running Backward" | Yes | Yes | Retreat run |
| ANI-LOC10 | Run Left | "Left Strafe" | Yes | Yes | Combat strafe |
| ANI-LOC11 | Run Right | "Right Strafe" | Yes | Yes | Combat strafe |
| ANI-LOC12 | Sprint | "Fast Run" or "Sprint" | Yes | Yes | Maximum speed |
| ANI-LOC13 | Run Injured | "Injured Run" | Yes | Yes | Low health run |
| ANI-LOC14 | Run Combat | "Combat Run" | Yes | Yes | Weapon ready run |

## 1.3 Turning Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-LOC15 | Turn Left 90 | "Left Turn 90" | Yes | No | Quick turn |
| ANI-LOC16 | Turn Right 90 | "Right Turn 90" | Yes | No | Quick turn |
| ANI-LOC17 | Turn Left 180 | "Turn" | Yes | No | About face |
| ANI-LOC18 | Turn Right 180 | "Turn" | Yes | No | About face |

## 1.4 Jump & Fall Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-LOC19 | Jump Start | "Jump" | Yes | No | Jump takeoff |
| ANI-LOC20 | Jump Air | "Falling Idle" | Yes | Yes | Mid-air loop |
| ANI-LOC21 | Jump Land | "Landing" | Yes | No | Landing impact |
| ANI-LOC22 | Jump Running | "Running Jump" | Yes | No | Jump while running |
| ANI-LOC23 | Fall | "Falling" | Yes | Yes | Free fall loop |
| ANI-LOC24 | Fall Land Hard | "Hard Landing" | Yes | No | High fall landing |

## 1.5 Special Movement

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-LOC25 | Dodge Left | "Dodge Left" | Yes | No | Combat dodge |
| ANI-LOC26 | Dodge Right | "Dodge Right" | Yes | No | Combat dodge |
| ANI-LOC27 | Dodge Back | "Dodge Back" | Yes | No | Combat dodge |
| ANI-LOC28 | Roll | "Combat Roll" | Yes | No | Roll dodge |

---

# SECTION 2: IDLE ANIMATIONS (16 animations)

## 2.1 Basic Idle

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-IDL01 | Idle Relaxed | "Idle" | Yes | Yes | Standard idle |
| ANI-IDL02 | Idle Alert | "Alert Idle" | Yes | Yes | Combat ready |
| ANI-IDL03 | Idle Tired | "Tired Idle" | Yes | Yes | Exhausted |
| ANI-IDL04 | Idle Happy | "Happy Idle" | Yes | Yes | Positive mood |
| ANI-IDL05 | Idle Sad | "Sad Idle" | Yes | Yes | Defeated mood |
| ANI-IDL06 | Idle Breathing | "Breathing Idle" | Yes | Yes | Subtle breathing |

## 2.2 Weapon Idle

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-IDL07 | Idle Sword | "Sword Idle" | Yes | Yes | Holding sword |
| ANI-IDL08 | Idle Two Handed | "Two Hand Sword Idle" | Yes | Yes | Large weapon |
| ANI-IDL09 | Idle Bow | "Bow Idle" | Yes | Yes | Holding bow |
| ANI-IDL10 | Idle Shield | "Shield Idle" | Yes | Yes | Shield stance |
| ANI-IDL11 | Idle Spear | "Spear Idle" | Yes | Yes | Holding spear |

## 2.3 Activity Idle

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-IDL12 | Idle Looking | "Looking Around" | Yes | No | Awareness |
| ANI-IDL13 | Idle Scratch | "Scratching Head" | Yes | No | Idle fidget |
| ANI-IDL14 | Idle Stretch | "Stretching" | Yes | No | Idle fidget |
| ANI-IDL15 | Idle Yawn | "Yawning" | Yes | No | Tired fidget |
| ANI-IDL16 | Idle Check Gear | "Check Watch" | Yes | No | Idle fidget |

---

# SECTION 3: COMBAT ANIMATIONS (48 animations)

## 3.1 Sword Combat

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-CMB01 | Sword Attack 1 | "Sword Slash" | Yes | No | Basic attack |
| ANI-CMB02 | Sword Attack 2 | "Sword Slash 2" | Yes | No | Combo attack |
| ANI-CMB03 | Sword Attack 3 | "Sword Slash 3" | Yes | No | Combo finisher |
| ANI-CMB04 | Sword Heavy | "Great Sword Slash" | Yes | No | Heavy attack |
| ANI-CMB05 | Sword Block | "Sword Block" | Yes | No | Block stance |
| ANI-CMB06 | Sword Parry | "Sword Parry" | Yes | No | Parry reaction |
| ANI-CMB07 | Sword Draw | "Draw Sword" | Yes | No | Weapon draw |
| ANI-CMB08 | Sword Sheathe | "Sheathe Sword" | Yes | No | Put away |

## 3.2 Two-Handed Combat

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-CMB09 | Two Hand Attack 1 | "Two Hand Sword Attack" | Yes | No | Basic swing |
| ANI-CMB10 | Two Hand Attack 2 | "Great Sword Attack" | Yes | No | Second attack |
| ANI-CMB11 | Two Hand Heavy | "Great Sword Overhead" | Yes | No | Heavy overhead |
| ANI-CMB12 | Two Hand Block | "Two Hand Block" | Yes | No | Block stance |
| ANI-CMB13 | Axe Attack 1 | "Axe Slash" | Yes | No | Axe swing |
| ANI-CMB14 | Axe Attack 2 | "Axe Attack" | Yes | No | Axe combo |

## 3.3 Spear Combat

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-CMB15 | Spear Thrust | "Spear Thrust" | Yes | No | Basic thrust |
| ANI-CMB16 | Spear Swing | "Spear Swing" | Yes | No | Sweep attack |
| ANI-CMB17 | Spear Block | "Spear Block" | Yes | No | Block stance |
| ANI-CMB18 | Spear Throw | "Throw" | Yes | No | Javelin throw |

## 3.4 Shield Combat

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-CMB19 | Shield Block Front | "Shield Block" | Yes | No | Frontal block |
| ANI-CMB20 | Shield Block High | "Shield Block High" | Yes | No | Overhead block |
| ANI-CMB21 | Shield Bash | "Shield Bash" | Yes | No | Shield attack |
| ANI-CMB22 | Shield + Sword | "Sword And Shield Attack" | Yes | No | Combo attack |

## 3.5 Ranged Combat

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-CMB23 | Bow Draw | "Draw Arrow" | Yes | No | Nock arrow |
| ANI-CMB24 | Bow Aim | "Aim Idle" | Yes | Yes | Aiming pose |
| ANI-CMB25 | Bow Fire | "Bow Fire" | Yes | No | Release arrow |
| ANI-CMB26 | Bow Fire Moving | "Running Bow Fire" | Yes | No | Mobile shot |
| ANI-CMB27 | Crossbow Load | "Crossbow Reload" | Yes | No | Loading |
| ANI-CMB28 | Crossbow Fire | "Crossbow Fire" | Yes | No | Shoot bolt |

## 3.6 Magic Combat

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-CMB29 | Cast Spell 1 | "Casting Spell" | Yes | No | Basic cast |
| ANI-CMB30 | Cast Spell 2 | "Magic Attack" | Yes | No | Attack spell |
| ANI-CMB31 | Cast Channel | "Channeling" | Yes | Yes | Channeled spell |
| ANI-CMB32 | Cast Buff | "Blessing" | Yes | No | Support spell |
| ANI-CMB33 | Cast Area | "Magic AOE" | Yes | No | Area spell |
| ANI-CMB34 | Cast Summon | "Summoning" | Yes | No | Summon spell |

## 3.7 Combat Reactions

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-CMB35 | Hit Front | "Hit Reaction" | Yes | No | Frontal hit |
| ANI-CMB36 | Hit Back | "Hit From Back" | Yes | No | Rear hit |
| ANI-CMB37 | Hit Left | "Hit Reaction Left" | Yes | No | Left hit |
| ANI-CMB38 | Hit Right | "Hit Reaction Right" | Yes | No | Right hit |
| ANI-CMB39 | Hit Heavy | "Heavy Hit" | Yes | No | Strong hit |
| ANI-CMB40 | Knockback | "Knockback" | Yes | No | Pushed back |
| ANI-CMB41 | Knockdown | "Knocked Down" | Yes | No | Fall down |
| ANI-CMB42 | Get Up | "Getting Up" | Yes | No | Recovery |

## 3.8 Death Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-CMB43 | Death Front | "Death Forward" | Yes | No | Fall forward |
| ANI-CMB44 | Death Back | "Death Backward" | Yes | No | Fall backward |
| ANI-CMB45 | Death Left | "Death Left" | Yes | No | Fall left |
| ANI-CMB46 | Death Right | "Death Right" | Yes | No | Fall right |
| ANI-CMB47 | Death Dramatic | "Dramatic Death" | Yes | No | Boss death |
| ANI-CMB48 | Resurrect | "Rising From Ground" | Yes | No | Revive |

---

# SECTION 4: INTERACTION ANIMATIONS (24 animations)

## 4.1 Object Interaction

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-INT01 | Pick Up Ground | "Picking Up" | Yes | No | Ground pickup |
| ANI-INT02 | Pick Up High | "Reaching Up" | Yes | No | Shelf pickup |
| ANI-INT03 | Put Down | "Put Down" | Yes | No | Place object |
| ANI-INT04 | Push Object | "Pushing" | Yes | No | Push heavy |
| ANI-INT05 | Pull Object | "Pulling" | Yes | No | Pull heavy |
| ANI-INT06 | Open Chest | "Opening Chest" | Yes | No | Treasure |
| ANI-INT07 | Open Door | "Open Door" | Yes | No | Door use |
| ANI-INT08 | Lever Pull | "Lever Pull" | Yes | No | Mechanism |

## 4.2 Social Interaction

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-INT09 | Wave | "Waving" | Yes | No | Greeting |
| ANI-INT10 | Bow | "Bowing" | Yes | No | Respect |
| ANI-INT11 | Salute | "Salute" | Yes | No | Military |
| ANI-INT12 | Clap | "Clapping" | Yes | No | Applause |
| ANI-INT13 | Cheer | "Victory Cheer" | Yes | No | Celebration |
| ANI-INT14 | Shake Head | "Head Shake" | Yes | No | Negative |
| ANI-INT15 | Nod | "Head Nod" | Yes | No | Agreement |
| ANI-INT16 | Point | "Pointing" | Yes | No | Indicate |

## 4.3 Work Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-INT17 | Hammer Work | "Hammering" | Yes | Yes | Construction |
| ANI-INT18 | Dig | "Shoveling" | Yes | Yes | Digging |
| ANI-INT19 | Chop Wood | "Chopping Wood" | Yes | Yes | Lumber |
| ANI-INT20 | Mine | "Mining" | Yes | Yes | Quarry |
| ANI-INT21 | Carry Heavy | "Carrying Heavy" | Yes | Yes | Transport |
| ANI-INT22 | Carry Light | "Walking With Object" | Yes | Yes | Light load |
| ANI-INT23 | Sweep | "Sweeping" | Yes | Yes | Cleaning |
| ANI-INT24 | Read Book | "Reading" | Yes | Yes | Study |

---

# SECTION 5: EMOTE ANIMATIONS (20 animations)

## 5.1 Positive Emotes

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-EMT01 | Happy | "Happy" | Yes | No | Joy |
| ANI-EMT02 | Excited | "Excited" | Yes | No | Enthusiasm |
| ANI-EMT03 | Laugh | "Laughing" | Yes | No | Humor |
| ANI-EMT04 | Dance | "Dancing" | Yes | Yes | Celebration |
| ANI-EMT05 | Flex | "Flexing" | Yes | No | Show off |
| ANI-EMT06 | Thumbs Up | "Thumbs Up" | Yes | No | Approval |
| ANI-EMT07 | Victory | "Victory" | Yes | No | Triumph |

## 5.2 Neutral Emotes

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-EMT08 | Thinking | "Thinking" | Yes | No | Contemplation |
| ANI-EMT09 | Shrug | "Shrug" | Yes | No | Indifference |
| ANI-EMT10 | Look Around | "Looking Around" | Yes | No | Searching |
| ANI-EMT11 | Check Watch | "Check Time" | Yes | No | Waiting |
| ANI-EMT12 | Cross Arms | "Cross Arms" | Yes | No | Impatient |

## 5.3 Negative Emotes

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-EMT13 | Angry | "Angry" | Yes | No | Frustration |
| ANI-EMT14 | Sad | "Sad" | Yes | No | Disappointment |
| ANI-EMT15 | Facepalm | "Face Palm" | Yes | No | Disbelief |
| ANI-EMT16 | Cry | "Crying" | Yes | No | Sorrow |
| ANI-EMT17 | Yell | "Yelling" | Yes | No | Rage |

## 5.4 Action Emotes

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-EMT18 | Sit | "Sitting Down" | Yes | No | Rest |
| ANI-EMT19 | Sit Idle | "Sitting Idle" | Yes | Yes | Seated loop |
| ANI-EMT20 | Stand Up | "Standing Up" | Yes | No | Rise |

---

# SECTION 6: NPC SPECIFIC ANIMATIONS (24 animations)

## 6.1 Guard Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-NPC01 | Guard Idle | "Standing Guard" | Yes | Yes | Sentry |
| ANI-NPC02 | Guard Alert | "Alert" | Yes | No | Noticed threat |
| ANI-NPC03 | Guard Patrol | "Walk With Weapon" | Yes | Yes | Patrolling |
| ANI-NPC04 | Guard Challenge | "Stop Gesture" | Yes | No | Halt |

## 6.2 Merchant Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-NPC05 | Merchant Idle | "Standing Idle" | Yes | Yes | Shop keeping |
| ANI-NPC06 | Merchant Welcome | "Welcoming" | Yes | No | Greeting |
| ANI-NPC07 | Merchant Show | "Presenting" | Yes | No | Show wares |
| ANI-NPC08 | Merchant Counting | "Counting Money" | Yes | Yes | Transaction |

## 6.3 Worker Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-NPC09 | Worker Idle | "Tired Standing" | Yes | Yes | Rest |
| ANI-NPC10 | Worker Build | "Construction" | Yes | Yes | Building |
| ANI-NPC11 | Worker Carry | "Carrying Boxes" | Yes | Yes | Transport |
| ANI-NPC12 | Worker Wipe | "Wiping Sweat" | Yes | No | Break |

## 6.4 Villager Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-NPC13 | Villager Walk | "Casual Walk" | Yes | Yes | Wandering |
| ANI-NPC14 | Villager Talk | "Talking" | Yes | Yes | Conversation |
| ANI-NPC15 | Villager Gather | "Picking Up" | Yes | Yes | Collecting |
| ANI-NPC16 | Villager Cheer | "Cheering" | Yes | No | Celebration |

## 6.5 Enemy Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-NPC17 | Enemy Idle | "Menacing Idle" | Yes | Yes | Threatening |
| ANI-NPC18 | Enemy Taunt | "Taunting" | Yes | No | Provoke |
| ANI-NPC19 | Enemy Charge | "Charge" | Yes | No | Rush attack |
| ANI-NPC20 | Enemy Roar | "Roaring" | Yes | No | Intimidate |

## 6.6 Commander Animations

| ID | Animation Name | Mixamo Search Term | In Place | Loop | Notes |
|----|---------------|-------------------|----------|------|-------|
| ANI-NPC21 | Command Idle | "Standing Proud" | Yes | Yes | Authority |
| ANI-NPC22 | Command Order | "Ordering" | Yes | No | Give orders |
| ANI-NPC23 | Command Point | "Point Forward" | Yes | No | Direct troops |
| ANI-NPC24 | Command Rally | "Rallying Troops" | Yes | No | Inspire |

---

# SUMMARY: TOTAL ANIMATIONS

| Category | Count |
|----------|-------|
| Locomotion | 28 |
| Idle | 16 |
| Combat | 48 |
| Interaction | 24 |
| Emotes | 20 |
| NPC Specific | 24 |
| **TOTAL** | **160** |

---

# UNITY SETUP GUIDE

## Download Settings

### First Animation (with character)
```
Format: FBX Binary (.fbx)
Skin: With Skin
Frames per Second: 30
Keyframe Reduction: None
```

### All Other Animations
```
Format: FBX Binary (.fbx)
Skin: Without Skin
Frames per Second: 30
Keyframe Reduction: None
```

## Unity Import Settings

### Model Tab
```
Scale Factor: 1
Mesh Compression: Off
Read/Write Enabled: Yes
Optimize Mesh: Everything
Import Blend Shapes: No
```

### Rig Tab
```
Animation Type: Humanoid
Avatar Definition: Create From This Model (first time)
Avatar Definition: Copy From Other Avatar (subsequent)
```

### Animation Tab
```
Loop Time: Yes (for looping animations)
Loop Pose: Yes
Cycle Offset: 0
Root Transform Rotation: Bake Into Pose
Root Transform Position (Y): Bake Into Pose (for in-place)
Root Transform Position (XZ): Bake Into Pose (for in-place)
```

## Animator Controller Setup

```
States (Layer 0 - Base):
├── Locomotion (Blend Tree)
│   ├── Idle
│   ├── Walk (Speed 0-0.5)
│   ├── Run (Speed 0.5-1.0)
│   └── Sprint (Speed > 1.0)
├── Jump (3 states: Start → Air → Land)
├── Combat (Sub-State Machine)
│   ├── Attack Combo
│   ├── Block
│   └── Hit Reactions
├── Death
└── Emotes (Any State transition)
```

## Blend Tree Example
```
Type: 2D Freeform Directional
Parameters: MoveX, MoveY

Motions:
- Idle (0, 0)
- Walk Forward (0, 0.5)
- Walk Backward (0, -0.5)
- Walk Left (-0.5, 0)
- Walk Right (0.5, 0)
- Run Forward (0, 1)
- Run Backward (0, -1)
- Run Left (-1, 0)
- Run Right (1, 0)
```

## Performance Tips
- Use Animation Compression: Optimal
- Enable Optimize Game Objects for characters
- Use Avatar Masks for upper body animations
- Pool animation events rather than callbacks
- Use Animator.StringToHash() for parameter names
