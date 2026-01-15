#!/usr/bin/env python3
"""
Suno AI Music Batch Helper

Helps organize and track the generation of 60 music tracks from SUNO_MUSIC_BATCH.md.
Since Suno has no public API, this tool assists with manual generation workflow.

Usage:
    python prompt_helper.py                    # Interactive mode
    python prompt_helper.py --export           # Export all prompts to ready file
    python prompt_helper.py --list             # List all tracks with status
    python prompt_helper.py --mark MUS01 MUS02 # Mark tracks as complete
    python prompt_helper.py --section 1        # Show specific section
"""

import os
import re
import json
import argparse
from dataclasses import dataclass
from pathlib import Path
from datetime import datetime


@dataclass
class MusicTrack:
    """Represents a single music track prompt."""
    id: str
    name: str
    style_tags: str
    bpm: str
    duration: str
    prompt: str
    section: str
    subsection: str


class SunoBatchHelper:
    """Helper for managing Suno music generation."""
    
    def __init__(self, batch_file: str, output_dir: str):
        self.batch_file = Path(batch_file)
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.progress_file = self.output_dir / "progress.json"
        self.completed = self._load_progress()
        self.tracks = self._parse_batch_file()
    
    def _load_progress(self) -> set:
        """Load completed track IDs."""
        if self.progress_file.exists():
            with open(self.progress_file, "r") as f:
                data = json.load(f)
                return set(data.get("completed", []))
        return set()
    
    def _save_progress(self):
        """Save progress to file."""
        with open(self.progress_file, "w") as f:
            json.dump({
                "completed": sorted(list(self.completed)),
                "total": len(self.tracks),
                "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }, f, indent=2)
    
    def _parse_batch_file(self) -> list[MusicTrack]:
        """Parse the SUNO_MUSIC_BATCH.md file."""
        tracks = []
        
        with open(self.batch_file, "r") as f:
            content = f.read()
        
        current_section = "Unknown"
        current_subsection = "Unknown"
        
        # Track section headers
        section_pattern = r"^# SECTION \d+: (.+)$"
        subsection_pattern = r"^## \d+\.\d+ (.+)$"
        
        # Track entry pattern
        # ### MUS## - Name
        # **Style Tags**: `tags`
        # **BPM**: ##-##
        # **Duration**: #:##
        # ```
        # prompt
        # ```
        track_pattern = r"### (MUS\d+) - (.+?)\n\*\*Style Tags\*\*: `(.+?)`\n\*\*BPM\*\*: (.+?)\n\*\*Duration\*\*: (.+?)\n```\n(.+?)\n```"
        
        lines = content.split('\n')
        for line in lines:
            section_match = re.match(section_pattern, line)
            if section_match:
                current_section = section_match.group(1)
            
            subsection_match = re.match(subsection_pattern, line)
            if subsection_match:
                current_subsection = subsection_match.group(1)
        
        # Find all track entries
        for match in re.finditer(track_pattern, content, re.DOTALL):
            track_id = match.group(1)
            name = match.group(2).strip()
            style_tags = match.group(3).strip()
            bpm = match.group(4).strip()
            duration = match.group(5).strip()
            prompt = match.group(6).strip()
            
            # Find section for this track
            pos = match.start()
            section = "Unknown"
            subsection = "Unknown"
            
            for sec_match in re.finditer(r"# SECTION \d+: (.+?)\n", content):
                if sec_match.start() < pos:
                    section = sec_match.group(1)
            
            for sub_match in re.finditer(r"## \d+\.\d+ (.+?)\n", content):
                if sub_match.start() < pos:
                    subsection = sub_match.group(1)
            
            tracks.append(MusicTrack(
                id=track_id,
                name=name,
                style_tags=style_tags,
                bpm=bpm,
                duration=duration,
                prompt=prompt,
                section=section,
                subsection=subsection
            ))
        
        return tracks
    
    def mark_complete(self, track_ids: list[str]):
        """Mark tracks as completed."""
        for tid in track_ids:
            tid = tid.upper()
            if any(t.id == tid for t in self.tracks):
                self.completed.add(tid)
                print(f"✅ Marked {tid} as complete")
            else:
                print(f"⚠️  Unknown track ID: {tid}")
        self._save_progress()
    
    def list_tracks(self, show_all: bool = False):
        """List all tracks with status."""
        print(f"\n{'='*70}")
        print(f"SUNO MUSIC BATCH - {len(self.tracks)} tracks")
        print(f"{'='*70}")
        print(f"Completed: {len(self.completed)}/{len(self.tracks)}")
        print(f"Remaining: {len(self.tracks) - len(self.completed)}")
        print(f"{'='*70}\n")
        
        current_section = None
        for track in self.tracks:
            if track.section != current_section:
                current_section = track.section
                print(f"\n📁 {current_section}")
                print("-" * 50)
            
            status = "✅" if track.id in self.completed else "○"
            if show_all or track.id not in self.completed:
                print(f"  {status} {track.id}: {track.name} ({track.duration})")
    
    def show_track(self, track_id: str):
        """Show full details for a specific track."""
        track_id = track_id.upper()
        track = next((t for t in self.tracks if t.id == track_id), None)
        
        if not track:
            print(f"❌ Track not found: {track_id}")
            return
        
        status = "✅ COMPLETED" if track.id in self.completed else "⏳ PENDING"
        
        print(f"\n{'='*70}")
        print(f"[{track.id}] {track.name}")
        print(f"{'='*70}")
        print(f"Status: {status}")
        print(f"Section: {track.section}")
        print(f"Duration: {track.duration}")
        print(f"BPM: {track.bpm}")
        print(f"\n📋 STYLE TAGS (copy this):")
        print(f"   {track.style_tags}")
        print(f"\n📝 PROMPT (copy this):")
        print("-" * 50)
        print(track.prompt)
        print("-" * 50)
        print(f"\n💾 SAVE AS: {track.id}_{track.name.replace(' ', '_')}.mp3")
        print(f"{'='*70}\n")
    
    def get_next_pending(self) -> MusicTrack | None:
        """Get the next pending track."""
        for track in self.tracks:
            if track.id not in self.completed:
                return track
        return None
    
    def export_all(self):
        """Export all prompts to a ready-to-use file."""
        export_file = self.output_dir / "ready_prompts.txt"
        
        remaining = [t for t in self.tracks if t.id not in self.completed]
        
        with open(export_file, "w") as f:
            f.write("=" * 70 + "\n")
            f.write("SUNO AI - READY TO PASTE PROMPTS\n")
            f.write("=" * 70 + "\n")
            f.write(f"Total: {len(remaining)} tracks remaining\n\n")
            f.write("WORKFLOW:\n")
            f.write("1. Go to suno.ai and select 'Custom' mode\n")
            f.write("2. Copy the STYLE TAGS into the style field\n")
            f.write("3. Copy the PROMPT into the lyrics/description field\n")
            f.write("4. Set duration and generate\n")
            f.write("5. Download and save with the filename shown\n")
            f.write("=" * 70 + "\n\n")
            
            current_section = None
            for track in remaining:
                if track.section != current_section:
                    current_section = track.section
                    f.write("\n" + "=" * 70 + "\n")
                    f.write(f"SECTION: {current_section}\n")
                    f.write("=" * 70 + "\n\n")
                
                f.write(f"[{track.id}] {track.name}\n")
                f.write(f"Save as: {track.id}_{track.name.replace(' ', '_')}.mp3\n")
                f.write(f"Duration: {track.duration} | BPM: {track.bpm}\n")
                f.write("-" * 50 + "\n")
                f.write(f"STYLE TAGS:\n{track.style_tags}\n\n")
                f.write(f"PROMPT:\n{track.prompt}\n")
                f.write("-" * 50 + "\n\n")
        
        print(f"✅ Exported {len(remaining)} prompts to: {export_file}")
        return str(export_file)
    
    def show_section(self, section_num: int):
        """Show all tracks in a specific section."""
        section_tracks = [t for t in self.tracks 
                        if f"SECTION {section_num}:" in f"SECTION {section_num}: {t.section}" 
                        or t.section.startswith(f"SECTION {section_num}")]
        
        # Better matching - find by looking at section names
        sections = {
            1: "MAIN THEMES",
            2: "GAMEPLAY AMBIENT", 
            3: "COMBAT MUSIC",
            4: "EVENT MUSIC",
            5: "ENVIRONMENTAL",
            6: "UI SOUNDS"
        }
        
        section_name = sections.get(section_num, "")
        section_tracks = [t for t in self.tracks if section_name.lower() in t.section.lower()]
        
        if not section_tracks:
            print(f"❌ No tracks found for section {section_num}")
            return
        
        print(f"\n{'='*70}")
        print(f"SECTION {section_num}: {section_name}")
        print(f"{'='*70}\n")
        
        for track in section_tracks:
            status = "✅" if track.id in self.completed else "○"
            print(f"{status} [{track.id}] {track.name}")
            print(f"   Duration: {track.duration} | BPM: {track.bpm}")
            print(f"   Style: {track.style_tags[:60]}...")
            print()
    
    def interactive_mode(self):
        """Run interactive mode for generating tracks one by one."""
        print(f"\n{'='*70}")
        print("🎵 SUNO MUSIC BATCH HELPER - Interactive Mode")
        print(f"{'='*70}")
        print(f"Total tracks: {len(self.tracks)}")
        print(f"Completed: {len(self.completed)}")
        print(f"Remaining: {len(self.tracks) - len(self.completed)}")
        print(f"{'='*70}\n")
        
        while True:
            print("\nCommands:")
            print("  n/next     - Show next pending track")
            print("  d [ID]     - Done/mark track complete")
            print("  s [ID]     - Show specific track")
            print("  l/list     - List all tracks")
            print("  e/export   - Export remaining to file")
            print("  q/quit     - Exit")
            
            try:
                cmd = input("\n> ").strip().lower()
            except (EOFError, KeyboardInterrupt):
                print("\nExiting...")
                break
            
            if not cmd:
                continue
            
            parts = cmd.split()
            action = parts[0]
            args = parts[1:] if len(parts) > 1 else []
            
            if action in ("n", "next"):
                track = self.get_next_pending()
                if track:
                    self.show_track(track.id)
                else:
                    print("🎉 All tracks completed!")
            
            elif action in ("d", "done", "mark"):
                if args:
                    self.mark_complete(args)
                else:
                    print("Usage: d MUS01 MUS02 ...")
            
            elif action in ("s", "show"):
                if args:
                    self.show_track(args[0])
                else:
                    print("Usage: s MUS01")
            
            elif action in ("l", "list"):
                self.list_tracks(show_all=True)
            
            elif action in ("e", "export"):
                self.export_all()
            
            elif action in ("q", "quit", "exit"):
                print("Goodbye!")
                break
            
            else:
                print(f"Unknown command: {action}")


def main():
    parser = argparse.ArgumentParser(description="Suno Music Batch Helper")
    parser.add_argument("--export", action="store_true", help="Export all prompts to file")
    parser.add_argument("--list", action="store_true", help="List all tracks")
    parser.add_argument("--mark", nargs="+", help="Mark track IDs as complete")
    parser.add_argument("--show", type=str, help="Show specific track details")
    parser.add_argument("--section", type=int, help="Show tracks in section")
    args = parser.parse_args()
    
    # Paths
    script_dir = Path(__file__).parent
    batch_file = script_dir.parent.parent / "docs" / "assets" / "SUNO_MUSIC_BATCH.md"
    output_dir = script_dir / "output"
    
    if not batch_file.exists():
        print(f"❌ Batch file not found: {batch_file}")
        return
    
    helper = SunoBatchHelper(str(batch_file), str(output_dir))
    
    if args.export:
        helper.export_all()
    elif args.list:
        helper.list_tracks(show_all=True)
    elif args.mark:
        helper.mark_complete(args.mark)
    elif args.show:
        helper.show_track(args.show)
    elif args.section:
        helper.show_section(args.section)
    else:
        helper.interactive_mode()


if __name__ == "__main__":
    main()
