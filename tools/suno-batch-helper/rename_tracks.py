#!/usr/bin/env python3
"""
Rename Suno music files from short names (MUS01.mp3) to full names (MUS01_Main_Theme_Epic.mp3)
"""

import os
from pathlib import Path

# Track ID to full filename mapping
TRACK_NAMES = {
    "MUS01": "MUS01_Main_Theme_Epic.mp3",
    "MUS02": "MUS02_Victory_Fanfare.mp3",
    "MUS03": "MUS03_Defeat_Somber.mp3",
    "MUS04": "MUS04_Menu_Theme.mp3",
    "MUS05": "MUS05_Loading_Loop.mp3",
    "MUS06": "MUS06_Tutorial_Friendly.mp3",
    "MUS07": "MUS07_Intro_Cinematic.mp3",
    "MUS08": "MUS08_Credits_Roll.mp3",
    "MUS09": "MUS09_Citadel_Home_Base.mp3",
    "MUS10": "MUS10_Citadel_Under_Construction.mp3",
    "MUS11": "MUS11_Citadel_Thriving.mp3",
    "MUS12": "MUS12_Citadel_Under_Siege.mp3",
    "MUS13": "MUS13_Territory_Peaceful.mp3",
    "MUS14": "MUS14_Territory_Contested.mp3",
    "MUS15": "MUS15_Territory_Enemy.mp3",
    "MUS16": "MUS16_Exploration_Wonder.mp3",
    "MUS17": "MUS17_Exploration_Danger.mp3",
    "MUS18": "MUS18_Exploration_Discovery.mp3",
    "MUS19": "MUS19_Night_Time.mp3",
    "MUS20": "MUS20_Dawn_Rising.mp3",
    "MUS21": "MUS21_Combat_Tension_Building.mp3",
    "MUS22": "MUS22_Combat_Imminent.mp3",
    "MUS23": "MUS23_Siege_Incoming.mp3",
    "MUS24": "MUS24_Combat_Light_Skirmish.mp3",
    "MUS25": "MUS25_Combat_Medium_Battle.mp3",
    "MUS26": "MUS26_Combat_Heavy_War.mp3",
    "MUS27": "MUS27_Combat_Boss_Battle.mp3",
    "MUS28": "MUS28_Siege_Defense.mp3",
    "MUS29": "MUS29_Siege_Attack.mp3",
    "MUS30": "MUS30_Combat_Victory.mp3",
    "MUS31": "MUS31_Combat_Retreat.mp3",
    "MUS32": "MUS32_Combat_Defeat.mp3",
    "MUS33": "MUS33_Alliance_Formed.mp3",
    "MUS34": "MUS34_Betrayal.mp3",
    "MUS35": "MUS35_Territory_Captured.mp3",
    "MUS36": "MUS36_Territory_Lost.mp3",
    "MUS37": "MUS37_Hero_Recruited.mp3",
    "MUS38": "MUS38_Hero_Fallen.mp3",
    "MUS39": "MUS39_Achievement_Unlocked.mp3",
    "MUS40": "MUS40_Level_Up.mp3",
    "MUS41": "MUS41_Season_Change_Spring.mp3",
    "MUS42": "MUS42_Season_Change_Summer.mp3",
    "MUS43": "MUS43_Season_Change_Autumn.mp3",
    "MUS44": "MUS44_Season_Change_Winter.mp3",
    "MUS45": "MUS45_Forest_Ambient.mp3",
    "MUS46": "MUS46_Mountain_Ambient.mp3",
    "MUS47": "MUS47_Desert_Ambient.mp3",
    "MUS48": "MUS48_Swamp_Ambient.mp3",
    "MUS49": "MUS49_Coastal_Ambient.mp3",
    "MUS50": "MUS50_Plains_Ambient.mp3",
    "MUS51": "MUS51_Ruins_Ambient.mp3",
    "MUS52": "MUS52_Cave_Ambient.mp3",
    "MUS53": "MUS53_UI_Click.mp3",
    "MUS54": "MUS54_UI_Hover.mp3",
    "MUS55": "MUS55_UI_Open_Menu.mp3",
    "MUS56": "MUS56_UI_Close_Menu.mp3",
    "MUS57": "MUS57_Notification_Info.mp3",
    "MUS58": "MUS58_Notification_Warning.mp3",
    "MUS59": "MUS59_Notification_Error.mp3",
    "MUS60": "MUS60_Notification_Success.mp3",
}


def rename_tracks(input_dir: str):
    """Rename all MUSxx.mp3 files to their full names."""
    input_path = Path(input_dir)
    
    if not input_path.exists():
        print(f"Directory not found: {input_dir}")
        return
    
    renamed = 0
    skipped = 0
    not_found = 0
    
    for track_id, full_name in TRACK_NAMES.items():
        # Look for short name (MUS01.mp3, MUS01.MP3, etc.)
        short_name = f"{track_id}.mp3"
        short_path = input_path / short_name
        
        # Also check uppercase
        if not short_path.exists():
            short_path = input_path / f"{track_id}.MP3"
        
        if short_path.exists():
            full_path = input_path / full_name
            
            if full_path.exists():
                print(f"  ⚠️  {full_name} already exists, skipping")
                skipped += 1
            else:
                short_path.rename(full_path)
                print(f"  ✅ {short_name} → {full_name}")
                renamed += 1
        else:
            not_found += 1
    
    print(f"\n{'=' * 50}")
    print(f"  Renamed: {renamed}")
    print(f"  Skipped (already exists): {skipped}")
    print(f"  Not found: {not_found}")
    print(f"{'=' * 50}")


if __name__ == "__main__":
    import sys
    
    if len(sys.argv) > 1:
        directory = sys.argv[1]
    else:
        # Default to output folder
        directory = Path(__file__).parent / "output"
    
    print(f"\n🎵 Renaming Suno tracks in: {directory}\n")
    rename_tracks(str(directory))
