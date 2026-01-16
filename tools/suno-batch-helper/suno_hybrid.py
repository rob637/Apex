#!/usr/bin/env python3
"""
Suno AI Hybrid Downloader

This script assists with downloading music from Suno.ai
Since Suno has no API, you must generate tracks manually first.

Two modes:
1. DOWNLOAD MODE: Downloads all tracks from your Suno library
2. ASSIST MODE: Auto-pastes prompts, you click Generate & pick results

Usage:
    python suno_hybrid.py --download    # Download all from library
    python suno_hybrid.py --assist      # Help with generation workflow

Requirements:
    pip install playwright pyperclip
    playwright install chromium
"""

import asyncio
import os
import re
import sys
import time
import json
from pathlib import Path
from datetime import datetime

try:
    from playwright.async_api import async_playwright, TimeoutError as PlaywrightTimeout
except ImportError:
    print("Installing playwright...")
    os.system("pip install playwright")
    os.system("playwright install chromium")
    from playwright.async_api import async_playwright, TimeoutError as PlaywrightTimeout

try:
    import pyperclip
except ImportError:
    print("Installing pyperclip...")
    os.system("pip install pyperclip")
    import pyperclip


# =============================================================================
# CONFIGURATION
# =============================================================================

OUTPUT_DIR = Path("./suno_downloads")
PROMPTS_FILE = Path("./output/ready_prompts.txt")
PROGRESS_FILE = Path("./suno_progress.json")

# All 60 tracks with their prompts
TRACKS = [
    # SECTION 1: MAIN THEMES (8 tracks)
    {
        "id": "MUS01",
        "name": "Main Theme Epic",
        "filename": "MUS01_Main_Theme_Epic.mp3",
        "style": "orchestral, epic, fantasy, cinematic, brass, strings, percussion",
        "prompt": "Epic orchestral main theme for medieval fantasy castle building game. Majestic brass fanfares, sweeping string melodies, powerful percussion. Starts quietly with solo French horn, builds to full orchestra crescendo. Themes of heroism, conquest, and building empires. Memorable main melody that can be hummed. Grand finale with timpani and full brass.",
        "duration": "3:00",
        "bpm": "90-100"
    },
    {
        "id": "MUS02",
        "name": "Main Theme Soft",
        "filename": "MUS02_Main_Theme_Soft.mp3",
        "style": "orchestral, gentle, fantasy, strings, harp, peaceful",
        "prompt": "Gentle orchestral version of main theme for medieval fantasy game menus. Soft strings carrying main melody, gentle harp arpeggios, light woodwinds. Peaceful and inviting, like viewing your kingdom from afar at sunset. Warm and welcoming atmosphere for new players.",
        "duration": "2:30",
        "bpm": "70-80"
    },
    {
        "id": "MUS03",
        "name": "Menu Ambient",
        "filename": "MUS03_Menu_Ambient.mp3",
        "style": "ambient, medieval, fantasy, atmospheric, calm, strings",
        "prompt": "Ambient background music for game menus. Soft evolving pads, gentle medieval instrumentation, occasional harp flourishes. Non-intrusive but engaging. Creates sense of entering a magical world. Subtle main theme motifs woven throughout.",
        "duration": "3:00",
        "bpm": "60-70"
    },
    {
        "id": "MUS04",
        "name": "Loading Screen",
        "filename": "MUS04_Loading_Screen.mp3",
        "style": "orchestral, building, anticipation, medieval, fantasy",
        "prompt": "Loading screen music for medieval fantasy game. Building anticipation, gentle orchestral swell, hint of adventure to come. Short loopable section. Ends on unresolved chord to transition smoothly to gameplay music.",
        "duration": "1:30",
        "bpm": "80-90"
    },
    {
        "id": "MUS05",
        "name": "Victory Fanfare",
        "filename": "MUS05_Victory_Fanfare.mp3",
        "style": "orchestral, triumphant, fanfare, brass, celebration",
        "prompt": "Triumphant victory fanfare for winning battles and completing objectives. Bright brass fanfare, celebratory percussion, ascending melody lines. Short but impactful. Feeling of achievement and glory. Can loop for extended victory screens.",
        "duration": "1:00",
        "bpm": "120-130"
    },
    {
        "id": "MUS06",
        "name": "Defeat Somber",
        "filename": "MUS06_Defeat_Somber.mp3",
        "style": "orchestral, sad, melancholy, strings, solo",
        "prompt": "Somber defeat music for lost battles. Solo cello or violin with quiet string accompaniment. Melancholic but not depressing. Sense of lesson learned, motivation to try again. Resolves to hopeful ending note.",
        "duration": "1:30",
        "bpm": "50-60"
    },
    {
        "id": "MUS07",
        "name": "Tutorial Friendly",
        "filename": "MUS07_Tutorial_Friendly.mp3",
        "style": "orchestral, light, friendly, woodwinds, playful",
        "prompt": "Friendly tutorial music for learning game mechanics. Light woodwinds, playful rhythm, encouraging tone. Not too complex or distracting. Helpful and supportive feeling. Like a wise mentor guiding the player.",
        "duration": "2:00",
        "bpm": "100-110"
    },
    {
        "id": "MUS08",
        "name": "Intro Cinematic",
        "filename": "MUS08_Intro_Cinematic.mp3",
        "style": "orchestral, epic, cinematic, dramatic, fantasy, choir",
        "prompt": "Epic cinematic intro music. Full orchestra with choir. Tells story of rising from small settlement to mighty empire. Dramatic build-ups, emotional peaks. Themes of destiny, ambition, and medieval glory. Hollywood-quality production feel.",
        "duration": "2:30",
        "bpm": "85-95"
    },
    
    # SECTION 2: GAMEPLAY AMBIENT (12 tracks)
    {
        "id": "MUS09",
        "name": "Building Mode Peaceful",
        "filename": "MUS09_Building_Mode_Peaceful.mp3",
        "style": "medieval, peaceful, acoustic, lute, flute, ambient",
        "prompt": "Peaceful building mode music for constructing castle. Gentle acoustic medieval instruments - lute, recorder, soft percussion. Non-intrusive background that doesn't distract from creative decisions. Pleasant loops for extended building sessions. Occasional bird sounds.",
        "duration": "3:30",
        "bpm": "70-80"
    },
    {
        "id": "MUS10",
        "name": "Building Mode Active",
        "filename": "MUS10_Building_Mode_Active.mp3",
        "style": "medieval, upbeat, construction, rhythmic, folk",
        "prompt": "Active building music with gentle energy. Medieval folk feel with steady rhythm suggesting construction activity. Hammers and building implied in percussion. Motivating without being intense. Good for productive building sessions.",
        "duration": "3:00",
        "bpm": "100-110"
    },
    {
        "id": "MUS11",
        "name": "Building Mode Night",
        "filename": "MUS11_Building_Mode_Night.mp3",
        "style": "medieval, calm, night, ambient, strings, peaceful",
        "prompt": "Nighttime building ambience. Soft strings, gentle night atmosphere, calm and focused mood. For players building late at night. Relaxing but not sleepy. Moonlit castle construction feeling.",
        "duration": "3:30",
        "bpm": "60-70"
    },
    {
        "id": "MUS12",
        "name": "Upgrade Celebration",
        "filename": "MUS12_Upgrade_Celebration.mp3",
        "style": "medieval, celebratory, fanfare, short, joyful",
        "prompt": "Short celebration music for building upgrades completing. Quick medieval fanfare, cheerful and satisfying. Positive reinforcement for player progress. Short enough to not interrupt gameplay flow.",
        "duration": "0:30",
        "bpm": "120"
    },
    {
        "id": "MUS13",
        "name": "World Map Overview",
        "filename": "MUS13_World_Map_Overview.mp3",
        "style": "orchestral, exploration, vast, wonder, adventure",
        "prompt": "World map overview music. Sense of vast world to explore. Orchestral with adventure themes. Looking down at territories, planning conquests. Inspiring wanderlust and strategic thinking. Hints of main theme.",
        "duration": "3:00",
        "bpm": "80-90"
    },
    {
        "id": "MUS14",
        "name": "Territory Neutral",
        "filename": "MUS14_Territory_Neutral.mp3",
        "style": "medieval, neutral, ambient, exploration, mystery",
        "prompt": "Music for exploring neutral territories. Mysterious but not threatening. Medieval ambient with sense of unknown lands. What secrets does this territory hold? Curiosity and careful exploration.",
        "duration": "3:30",
        "bpm": "75-85"
    },
    {
        "id": "MUS15",
        "name": "Territory Friendly",
        "filename": "MUS15_Territory_Friendly.mp3",
        "style": "medieval, friendly, warm, folk, welcoming",
        "prompt": "Music for allied/friendly territories. Warm and welcoming medieval folk music. Sense of visiting friends. Trade and diplomacy themes. Comfortable and safe feeling among allies.",
        "duration": "3:00",
        "bpm": "90-100"
    },
    {
        "id": "MUS16",
        "name": "Territory Enemy",
        "filename": "MUS16_Territory_Enemy.mp3",
        "style": "orchestral, tense, danger, suspense, enemy",
        "prompt": "Music for enemy territory proximity. Tense and suspenseful. Low strings, ominous undertones. Danger nearby but not immediate combat. Strategic awareness, need for caution.",
        "duration": "3:00",
        "bpm": "85-95"
    },
    {
        "id": "MUS17",
        "name": "Economy Prosperous",
        "filename": "MUS17_Economy_Prosperous.mp3",
        "style": "medieval, prosperous, market, lively, coins, trade",
        "prompt": "Prosperous economy music. Lively medieval market feel. Sound of commerce and busy trade. Happy citizens, flowing resources. Kingdom thriving economically. Cheerful without being silly.",
        "duration": "3:00",
        "bpm": "110-120"
    },
    {
        "id": "MUS18",
        "name": "Economy Struggling",
        "filename": "MUS18_Economy_Struggling.mp3",
        "style": "medieval, somber, concern, strings, worry",
        "prompt": "Music for low resources or economic trouble. Concerned tone, strings with worried quality. Not panic, but awareness of challenges. Motivation to improve situation. Hopeful undertones.",
        "duration": "2:30",
        "bpm": "70-80"
    },
    {
        "id": "MUS19",
        "name": "Shop & Market",
        "filename": "MUS19_Shop_Market.mp3",
        "style": "medieval, market, lively, folk, trading, cheerful",
        "prompt": "In-game shop music. Cheerful medieval marketplace. Merchants selling wares, coins changing hands. Encouraging purchases without being pushy. Pleasant shopping experience.",
        "duration": "3:00",
        "bpm": "100-110"
    },
    {
        "id": "MUS20",
        "name": "Treasure Found",
        "filename": "MUS20_Treasure_Found.mp3",
        "style": "orchestral, discovery, magical, sparkle, reward",
        "prompt": "Treasure discovery music. Magical sparkle sounds, rewarding orchestral swell. Short celebratory piece for finding rewards. Satisfying feedback for player achievements.",
        "duration": "0:45",
        "bpm": "100"
    },
    
    # SECTION 3: COMBAT MUSIC (12 tracks)
    {
        "id": "MUS21",
        "name": "Battle Preparation",
        "filename": "MUS21_Battle_Preparation.mp3",
        "style": "orchestral, preparation, tension, building, drums",
        "prompt": "Pre-battle preparation music. Building tension, drums beginning to roll. Army assembling, strategies being made. Anticipation of combat. Not full battle yet but imminent.",
        "duration": "2:30",
        "bpm": "90-100"
    },
    {
        "id": "MUS22",
        "name": "Enemy Approaching",
        "filename": "MUS22_Enemy_Approaching.mp3",
        "style": "orchestral, warning, urgent, approach, suspense",
        "prompt": "Enemy approaching warning music. Increasing urgency, enemy forces drawing near. Time to prepare defenses. Suspenseful build-up to combat. Growing intensity.",
        "duration": "2:00",
        "bpm": "100-110"
    },
    {
        "id": "MUS23",
        "name": "Siege Incoming",
        "filename": "MUS23_Siege_Incoming.mp3",
        "style": "orchestral, siege, massive, epic, impending, war drums",
        "prompt": "Siege incoming music. Massive war drums, full siege about to begin. Heaviest attack coming. Epic scale preparation. Final moments before major battle.",
        "duration": "2:30",
        "bpm": "85-95"
    },
    {
        "id": "MUS24",
        "name": "Combat Light Skirmish",
        "filename": "MUS24_Combat_Light_Skirmish.mp3",
        "style": "orchestral, action, combat, light, skirmish, quick",
        "prompt": "Light skirmish combat music. Quick action, small engagements. Fast-paced but not overwhelming. Minor combat encounters. Energetic percussion, exciting but manageable threat.",
        "duration": "2:30",
        "bpm": "130-140"
    },
    {
        "id": "MUS25",
        "name": "Combat Medium Battle",
        "filename": "MUS25_Combat_Medium_Battle.mp3",
        "style": "orchestral, battle, intense, combat, brass, percussion",
        "prompt": "Medium intensity battle music. Full combat engagement. Brass and percussion driving action. Strategic combat, measuring strength. Exciting but controlled chaos.",
        "duration": "3:00",
        "bpm": "120-130"
    },
    {
        "id": "MUS26",
        "name": "Combat Heavy War",
        "filename": "MUS26_Combat_Heavy_War.mp3",
        "style": "orchestral, war, epic, intense, full, choir",
        "prompt": "Full-scale war music. Maximum intensity combat. Epic orchestral assault with choir. Life and death stakes. Armies clashing, desperate fighting. Most intense battle music.",
        "duration": "3:30",
        "bpm": "140-150"
    },
    {
        "id": "MUS27",
        "name": "Combat Boss Battle",
        "filename": "MUS27_Combat_Boss_Battle.mp3",
        "style": "orchestral, boss, epic, dramatic, powerful, intense",
        "prompt": "Boss battle music. Facing powerful enemy commander. Dramatic and intense. Clear threat, high stakes. Memorable villain theme. Epic showdown feeling.",
        "duration": "3:30",
        "bpm": "130-140"
    },
    {
        "id": "MUS28",
        "name": "Siege Defense",
        "filename": "MUS28_Siege_Defense.mp3",
        "style": "orchestral, siege, defensive, desperate, heroic",
        "prompt": "Castle siege defense music. Defending against attackers. Heroic but desperate. Walls under assault. Fighting for survival. Bravery against overwhelming odds.",
        "duration": "3:00",
        "bpm": "120-130"
    },
    {
        "id": "MUS29",
        "name": "Siege Attack",
        "filename": "MUS29_Siege_Attack.mp3",
        "style": "orchestral, siege, attack, aggressive, conquering",
        "prompt": "Castle siege attack music. Storming enemy fortifications. Aggressive and conquering. Breaching walls, victory within reach. Triumphant aggression.",
        "duration": "3:00",
        "bpm": "125-135"
    },
    {
        "id": "MUS30",
        "name": "Combat Victory",
        "filename": "MUS30_Combat_Victory.mp3",
        "style": "orchestral, victory, triumphant, celebration, heroic",
        "prompt": "Battle won victory music. Triumphant celebration of combat victory. Heroes standing tall. Enemy defeated. Glory and honor earned. Celebratory brass fanfares.",
        "duration": "1:30",
        "bpm": "110-120"
    },
    {
        "id": "MUS31",
        "name": "Combat Retreat",
        "filename": "MUS31_Combat_Retreat.mp3",
        "style": "orchestral, retreat, urgent, escape, survival",
        "prompt": "Tactical retreat music. Escaping losing battle. Urgent but not panicked. Strategic withdrawal. Living to fight another day. Hopeful undercurrent.",
        "duration": "2:00",
        "bpm": "115-125"
    },
    {
        "id": "MUS32",
        "name": "Combat Defeat",
        "filename": "MUS32_Combat_Defeat.mp3",
        "style": "orchestral, defeat, somber, loss, aftermath",
        "prompt": "Battle lost defeat music. Somber aftermath of loss. Processing defeat. But not giving up. Sadness with determination to rebuild. Minor key resolution.",
        "duration": "1:30",
        "bpm": "60-70"
    },
    
    # SECTION 4: EVENT MUSIC (12 tracks)
    {
        "id": "MUS33",
        "name": "Alliance Formed",
        "filename": "MUS33_Alliance_Formed.mp3",
        "style": "orchestral, alliance, diplomatic, hopeful, unity",
        "prompt": "Alliance formation celebration music. Two kingdoms uniting. Hopeful and diplomatic. Strength through unity. New friendships forged. Optimistic future together.",
        "duration": "1:30",
        "bpm": "90-100"
    },
    {
        "id": "MUS34",
        "name": "Betrayal",
        "filename": "MUS34_Betrayal.mp3",
        "style": "orchestral, betrayal, shock, dramatic, dark",
        "prompt": "Betrayal revelation music. Ally turned enemy. Shock and drama. Trust broken. Dark turn of events. Dramatic orchestral sting with ominous continuation.",
        "duration": "1:00",
        "bpm": "70-80"
    },
    {
        "id": "MUS35",
        "name": "Territory Captured",
        "filename": "MUS35_Territory_Captured.mp3",
        "style": "orchestral, conquest, triumphant, expansion, glory",
        "prompt": "Territory conquest celebration. New lands claimed. Expansion of empire. Glory and accomplishment. Flag planting moment. Triumphant but brief.",
        "duration": "1:00",
        "bpm": "110-120"
    },
    {
        "id": "MUS36",
        "name": "Territory Lost",
        "filename": "MUS36_Territory_Lost.mp3",
        "style": "orchestral, loss, concern, setback, determination",
        "prompt": "Territory lost notification. Setback but not defeat. Concern and determination. Will reclaim what was lost. Motivating despite loss.",
        "duration": "1:00",
        "bpm": "70-80"
    },
    {
        "id": "MUS37",
        "name": "Hero Recruited",
        "filename": "MUS37_Hero_Recruited.mp3",
        "style": "orchestral, hero, epic, introduction, powerful",
        "prompt": "New hero recruitment fanfare. Powerful ally joins. Epic introduction. This changes everything. Heroic themes, character entrance music.",
        "duration": "0:45",
        "bpm": "100-110"
    },
    {
        "id": "MUS38",
        "name": "Hero Fallen",
        "filename": "MUS38_Hero_Fallen.mp3",
        "style": "orchestral, tragic, loss, memorial, sad",
        "prompt": "Hero death memorial music. Tragic loss of valued ally. Honoring their sacrifice. Sad but respectful. Legacy remembered. Solo instrument tribute.",
        "duration": "1:30",
        "bpm": "50-60"
    },
    {
        "id": "MUS39",
        "name": "Achievement Unlocked",
        "filename": "MUS39_Achievement_Unlocked.mp3",
        "style": "orchestral, achievement, celebration, reward, sparkle",
        "prompt": "Achievement unlock celebration. Player accomplishment recognized. Short rewarding fanfare. Positive reinforcement. Satisfying feedback.",
        "duration": "0:15",
        "bpm": "120"
    },
    {
        "id": "MUS40",
        "name": "Level Up",
        "filename": "MUS40_Level_Up.mp3",
        "style": "orchestral, level up, growth, power, ascending",
        "prompt": "Level up celebration music. Growing stronger. Ascending power. Achievement and growth. Short triumphant fanfare with magical overtones.",
        "duration": "0:20",
        "bpm": "110-120"
    },
    {
        "id": "MUS41",
        "name": "Season Change Spring",
        "filename": "MUS41_Season_Change_Spring.mp3",
        "style": "orchestral, spring, renewal, birds, fresh, hopeful",
        "prompt": "Spring season transition. Renewal and fresh beginnings. Bird songs, blooming themes. Hopeful and bright. New growth, new opportunities.",
        "duration": "0:30",
        "bpm": "90-100"
    },
    {
        "id": "MUS42",
        "name": "Season Change Summer",
        "filename": "MUS42_Season_Change_Summer.mp3",
        "style": "orchestral, summer, vibrant, warm, lively, abundance",
        "prompt": "Summer season transition. Warmth and abundance. Vibrant and lively. Peak prosperity. Full energy and activity. Bright brass and strings.",
        "duration": "0:30",
        "bpm": "100-110"
    },
    {
        "id": "MUS43",
        "name": "Season Change Autumn",
        "filename": "MUS43_Season_Change_Autumn.mp3",
        "style": "orchestral, autumn, harvest, golden, melancholy, change",
        "prompt": "Autumn season transition. Harvest and change. Golden hues in sound. Slight melancholy, preparing for winter. Gathering and storing.",
        "duration": "0:30",
        "bpm": "80-90"
    },
    {
        "id": "MUS44",
        "name": "Season Change Winter",
        "filename": "MUS44_Season_Change_Winter.mp3",
        "style": "orchestral, winter, cold, quiet, snow, peaceful",
        "prompt": "Winter season transition. Cold and quiet. Snow falling themes. Peaceful but challenging. Survival and endurance. Sparse, crystalline sounds.",
        "duration": "0:30",
        "bpm": "60-70"
    },
    
    # SECTION 5: ENVIRONMENTAL (8 tracks)
    {
        "id": "MUS45",
        "name": "Forest Ambient",
        "filename": "MUS45_Forest_Ambient.mp3",
        "style": "ambient, forest, nature, peaceful, birds, wind",
        "prompt": "Forest territory ambient. Natural woodland sounds. Birds, gentle wind, rustling leaves. Peaceful nature immersion. Non-musical ambient soundscape.",
        "duration": "4:00",
        "bpm": "N/A"
    },
    {
        "id": "MUS46",
        "name": "Mountain Ambient",
        "filename": "MUS46_Mountain_Ambient.mp3",
        "style": "ambient, mountain, wind, vast, echoes, majestic",
        "prompt": "Mountain territory ambient. High altitude atmosphere. Wind, vast echoes, majestic space. Cold and airy. Eagle cries in distance.",
        "duration": "4:00",
        "bpm": "N/A"
    },
    {
        "id": "MUS47",
        "name": "Desert Ambient",
        "filename": "MUS47_Desert_Ambient.mp3",
        "style": "ambient, desert, wind, sand, heat, desolate",
        "prompt": "Desert territory ambient. Hot and desolate. Wind over sand, heat shimmer. Sparse and dry. Occasional mysterious tones.",
        "duration": "4:00",
        "bpm": "N/A"
    },
    {
        "id": "MUS48",
        "name": "Swamp Ambient",
        "filename": "MUS48_Swamp_Ambient.mp3",
        "style": "ambient, swamp, murky, frogs, insects, mysterious",
        "prompt": "Swamp territory ambient. Murky and mysterious. Frogs, insects, bubbling water. Slightly ominous. Hidden dangers lurking.",
        "duration": "4:00",
        "bpm": "N/A"
    },
    {
        "id": "MUS49",
        "name": "Coastal Ambient",
        "filename": "MUS49_Coastal_Ambient.mp3",
        "style": "ambient, ocean, waves, seagulls, peaceful, coastal",
        "prompt": "Coastal territory ambient. Ocean waves, seagulls, sea breeze. Peaceful coastal atmosphere. Salt air and open water feeling.",
        "duration": "4:00",
        "bpm": "N/A"
    },
    {
        "id": "MUS50",
        "name": "Plains Ambient",
        "filename": "MUS50_Plains_Ambient.mp3",
        "style": "ambient, plains, grass, wind, open, peaceful",
        "prompt": "Plains territory ambient. Open grasslands, wind through grass. Vast and peaceful. Distant horizon feeling. Simple and calm.",
        "duration": "4:00",
        "bpm": "N/A"
    },
    {
        "id": "MUS51",
        "name": "Ruins Ambient",
        "filename": "MUS51_Ruins_Ambient.mp3",
        "style": "ambient, ruins, ancient, mysterious, echoes, dust",
        "prompt": "Ancient ruins ambient. Mysterious echoes, dust and age. What civilization was here? Haunting and curious atmosphere.",
        "duration": "4:00",
        "bpm": "N/A"
    },
    {
        "id": "MUS52",
        "name": "Cave Ambient",
        "filename": "MUS52_Cave_Ambient.mp3",
        "style": "ambient, cave, dripping, echoes, dark, underground",
        "prompt": "Underground cave ambient. Dripping water, echoing spaces. Dark and enclosed. What lurks in the depths? Tense exploration.",
        "duration": "4:00",
        "bpm": "N/A"
    },
    
    # SECTION 6: UI & MISC (8 tracks)
    {
        "id": "MUS53",
        "name": "UI Click",
        "filename": "MUS53_UI_Click.mp3",
        "style": "UI, click, interface, soft, satisfying",
        "prompt": "UI button click sound. Soft and satisfying. Not intrusive. Medieval-appropriate tone. Brief and responsive.",
        "duration": "0:02",
        "bpm": "N/A"
    },
    {
        "id": "MUS54",
        "name": "UI Hover",
        "filename": "MUS54_UI_Hover.mp3",
        "style": "UI, hover, soft, subtle, interface",
        "prompt": "UI hover sound effect. Subtle acknowledgment. Softer than click. Interface feedback. Brief whisper of interaction.",
        "duration": "0:01",
        "bpm": "N/A"
    },
    {
        "id": "MUS55",
        "name": "UI Open Menu",
        "filename": "MUS55_UI_Open_Menu.mp3",
        "style": "UI, menu, open, whoosh, interface",
        "prompt": "Menu opening sound. Gentle whoosh, medieval appropriate. Satisfying feedback. Brief and clean.",
        "duration": "0:03",
        "bpm": "N/A"
    },
    {
        "id": "MUS56",
        "name": "UI Close Menu",
        "filename": "MUS56_UI_Close_Menu.mp3",
        "style": "UI, menu, close, whoosh, interface",
        "prompt": "Menu closing sound. Reverse whoosh feel. Closing interaction. Brief and satisfying completion.",
        "duration": "0:03",
        "bpm": "N/A"
    },
    {
        "id": "MUS57",
        "name": "Notification Info",
        "filename": "MUS57_Notification_Info.mp3",
        "style": "notification, info, gentle, chime, alert",
        "prompt": "Information notification sound. Gentle chime, drawing attention. Not urgent, just informative. Medieval bell-like quality.",
        "duration": "0:04",
        "bpm": "N/A"
    },
    {
        "id": "MUS58",
        "name": "Notification Warning",
        "filename": "MUS58_Notification_Warning.mp3",
        "style": "notification, warning, alert, urgent, attention",
        "prompt": "Warning notification sound. More urgent than info. Attention needed. Not panic, but important. Clear alert tone.",
        "duration": "0:04",
        "bpm": "N/A"
    },
    {
        "id": "MUS59",
        "name": "Notification Error",
        "filename": "MUS59_Notification_Error.mp3",
        "style": "notification, error, negative, alert, problem",
        "prompt": "Error notification sound. Something wrong. Clear negative feedback. Brief but unmistakable. Problem requiring attention.",
        "duration": "0:03",
        "bpm": "N/A"
    },
    {
        "id": "MUS60",
        "name": "Notification Success",
        "filename": "MUS60_Notification_Success.mp3",
        "style": "notification, success, positive, celebration, done",
        "prompt": "Success notification sound. Positive completion. Task accomplished. Brief celebration tone. Satisfying confirmation.",
        "duration": "0:04",
        "bpm": "N/A"
    },
]


def load_progress():
    """Load download progress."""
    if PROGRESS_FILE.exists():
        with open(PROGRESS_FILE, "r") as f:
            return json.load(f)
    return {"completed": [], "downloaded": []}


def save_progress(progress):
    """Save download progress."""
    with open(PROGRESS_FILE, "w") as f:
        json.dump(progress, f, indent=2)


async def download_mode(page):
    """
    Download all tracks from Suno library.
    Assumes you have already generated tracks.
    """
    print("\n" + "="*60)
    print("SUNO DOWNLOAD MODE")
    print("="*60)
    print("\nThis will download all tracks from your Suno library.")
    print("Make sure you have already generated the tracks!")
    print("\n1. Log into suno.ai")
    print("2. Go to your Library page")
    print("3. Press ENTER when ready...")
    input()
    
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    progress = load_progress()
    downloaded_count = 0
    
    # Wait for library to load
    print("\nLooking for your generated tracks...")
    await asyncio.sleep(3)
    
    # Find all track cards/items in the library
    # Suno's structure may vary, so we'll look for download buttons
    try:
        # Look for song items - adjust selectors based on actual Suno UI
        song_items = await page.query_selector_all('[data-testid="song-item"], .song-card, [class*="SongCard"]')
        
        if not song_items:
            print("\nCouldn't find tracks with standard selectors.")
            print("Let's try a different approach...")
            
            # Alternative: look for any download buttons or audio elements
            await page.wait_for_selector('audio, [class*="download"], button[aria-label*="download"]', timeout=10000)
        
        print(f"\nFound potential tracks. Starting downloads...")
        
        # For each track, find download button and click
        for i, item in enumerate(song_items):
            try:
                # Hover to reveal download button
                await item.hover()
                await asyncio.sleep(0.5)
                
                # Look for download button within this item
                download_btn = await item.query_selector('button[aria-label*="download"], [class*="download"]')
                if download_btn:
                    await download_btn.click()
                    await asyncio.sleep(2)  # Wait for download
                    downloaded_count += 1
                    print(f"  Downloaded track {downloaded_count}")
            except Exception as e:
                print(f"  Error on track {i}: {e}")
                continue
        
        print(f"\n✅ Downloaded {downloaded_count} tracks!")
        
    except Exception as e:
        print(f"\nError finding tracks: {e}")
        print("\nTrying manual approach...")
        print("Please manually right-click each track and 'Save Audio As'")
        print("Save files to:", OUTPUT_DIR.absolute())


async def assist_mode(page):
    """
    Assist with generation by auto-pasting prompts.
    You click Generate, script pastes next prompt.
    """
    print("\n" + "="*60)
    print("SUNO GENERATION ASSIST MODE")
    print("="*60)
    print("\nThis mode helps speed up manual generation:")
    print("1. Script copies prompt to clipboard")
    print("2. YOU paste and click Generate")
    print("3. YOU pick the best result and download")
    print("4. Press ENTER for next track")
    print("\n" + "-"*60)
    
    progress = load_progress()
    completed = set(progress.get("completed", []))
    
    remaining = [t for t in TRACKS if t["id"] not in completed]
    print(f"\n{len(remaining)} tracks remaining out of {len(TRACKS)}")
    
    print("\n1. Make sure you're logged into suno.ai")
    print("2. Navigate to Create page")
    print("3. Press ENTER when ready...")
    input()
    
    for i, track in enumerate(remaining):
        print(f"\n{'='*60}")
        print(f"TRACK {i+1}/{len(remaining)}: [{track['id']}] {track['name']}")
        print(f"{'='*60}")
        print(f"Duration: {track['duration']} | BPM: {track['bpm']}")
        print(f"\nFilename: {track['filename']}")
        print(f"\n--- STYLE TAGS (copied to clipboard) ---")
        print(track['style'])
        
        # Copy style to clipboard
        pyperclip.copy(track['style'])
        print("\n✅ Style tags copied! Paste into Suno's style field.")
        input("Press ENTER when style is pasted...")
        
        # Now copy prompt
        print(f"\n--- PROMPT (copied to clipboard) ---")
        print(track['prompt'][:200] + "..." if len(track['prompt']) > 200 else track['prompt'])
        pyperclip.copy(track['prompt'])
        print("\n✅ Prompt copied! Paste into Suno's description field.")
        print("   Then click GENERATE!")
        
        input("\nPress ENTER when generation is complete and you've downloaded the result...")
        
        # Mark as complete
        completed.add(track['id'])
        progress["completed"] = list(completed)
        save_progress(progress)
        
        print(f"✅ Marked {track['id']} as complete ({len(completed)}/{len(TRACKS)})")
        
        if i < len(remaining) - 1:
            cont = input("\nContinue to next track? (y/n): ").strip().lower()
            if cont != 'y' and cont != '':
                print("\nProgress saved! Run again to continue.")
                break
    
    print(f"\n{'='*60}")
    print(f"SESSION COMPLETE!")
    print(f"Completed: {len(completed)}/{len(TRACKS)} tracks")
    print(f"{'='*60}")


async def main():
    """Main entry point."""
    import argparse
    parser = argparse.ArgumentParser(description="Suno AI Hybrid Helper")
    parser.add_argument("--download", action="store_true", help="Download from library")
    parser.add_argument("--assist", action="store_true", help="Assist with generation")
    parser.add_argument("--reset", action="store_true", help="Reset progress")
    args = parser.parse_args()
    
    if args.reset:
        if PROGRESS_FILE.exists():
            PROGRESS_FILE.unlink()
        print("Progress reset!")
        return
    
    print("="*60)
    print("SUNO AI HYBRID HELPER")
    print("="*60)
    
    if not args.download and not args.assist:
        print("\nChoose mode:")
        print("  1. DOWNLOAD - Download tracks from your Suno library")
        print("  2. ASSIST   - Help with generation (copies prompts)")
        choice = input("\nEnter 1 or 2: ").strip()
        args.download = choice == "1"
        args.assist = choice == "2"
    
    print("\nLaunching browser (visible mode)...")
    print("You will need to log in manually.\n")
    
    async with async_playwright() as p:
        browser = await p.chromium.launch(
            headless=False,  # VISIBLE BROWSER
            slow_mo=100,
        )
        
        context = await browser.new_context(
            accept_downloads=True,
            viewport={"width": 1400, "height": 900}
        )
        
        page = await context.new_page()
        
        # Set download path
        OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
        await page._impl_obj.set_default_timeout(60000)
        
        # Navigate to Suno
        print("Opening Suno.ai...")
        await page.goto("https://suno.ai/")
        
        print("\n" + "-"*60)
        print("Please LOG IN to your Suno account now.")
        print("After logging in, press ENTER to continue...")
        print("-"*60)
        input()
        
        if args.download:
            await download_mode(page)
        elif args.assist:
            await assist_mode(page)
        
        print("\nKeeping browser open for manual work...")
        print("Press ENTER to close browser and exit.")
        input()
        
        await browser.close()


if __name__ == "__main__":
    asyncio.run(main())
