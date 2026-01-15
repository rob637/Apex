#!/usr/bin/env python3
"""
Freesound Batch Downloader

Automatically downloads sound effects from Freesound.org based on SOUND_EFFECTS_BATCH.md.
Searches for best matching sounds and downloads them.

Usage:
    export FREESOUND_API_KEY="your_api_key_here"
    python download_sounds.py

API Documentation: https://freesound.org/docs/api/
"""

import os
import re
import json
import time
import requests
from dataclasses import dataclass
from typing import Optional
from pathlib import Path
from datetime import datetime


@dataclass
class SoundEffect:
    """Represents a sound effect to download."""
    id: str
    name: str
    description: str
    duration: str
    source: str
    category: str


class FreesoundClient:
    """Client for Freesound API."""
    
    BASE_URL = "https://freesound.org/apiv2"
    
    def __init__(self, api_key: str):
        self.api_key = api_key
        self.session = requests.Session()
    
    def search(self, query: str, max_duration: float = 5.0, min_duration: float = 0.0) -> list:
        """
        Search for sounds matching query.
        
        Args:
            query: Search terms
            max_duration: Maximum duration in seconds
            min_duration: Minimum duration in seconds
        
        Returns:
            List of matching sounds
        """
        params = {
            "token": self.api_key,
            "query": query,
            "filter": f"duration:[{min_duration} TO {max_duration}]",
            "fields": "id,name,duration,previews,license,download,avg_rating,num_ratings",
            "sort": "rating_desc",
            "page_size": 10
        }
        
        response = self.session.get(f"{self.BASE_URL}/search/text/", params=params)
        response.raise_for_status()
        return response.json().get("results", [])
    
    def get_sound_info(self, sound_id: int) -> dict:
        """Get detailed info about a sound."""
        params = {"token": self.api_key}
        response = self.session.get(f"{self.BASE_URL}/sounds/{sound_id}/", params=params)
        response.raise_for_status()
        return response.json()
    
    def download_preview(self, sound_data: dict, output_path: str) -> str:
        """
        Download the HQ preview of a sound (doesn't require OAuth).
        
        Args:
            sound_data: Sound data from search/get
            output_path: Where to save the file
        
        Returns:
            Path to downloaded file
        """
        # Use HQ MP3 preview (available without OAuth)
        previews = sound_data.get("previews", {})
        preview_url = previews.get("preview-hq-mp3") or previews.get("preview-lq-mp3")
        
        if not preview_url:
            raise Exception("No preview URL available")
        
        response = self.session.get(preview_url)
        response.raise_for_status()
        
        with open(output_path, "wb") as f:
            f.write(response.content)
        
        return output_path


class BatchDownloader:
    """Downloads sounds in batch from Freesound."""
    
    def __init__(self, client: FreesoundClient, output_dir: str):
        self.client = client
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.progress_file = self.output_dir / "progress.json"
        self.completed = self._load_progress()
        self.failed = set()
    
    def _load_progress(self) -> dict:
        """Load progress - maps sound ID to freesound ID used."""
        if self.progress_file.exists():
            with open(self.progress_file, "r") as f:
                data = json.load(f)
                return data.get("completed", {})
        return {}
    
    def _save_progress(self):
        """Save progress to file."""
        with open(self.progress_file, "w") as f:
            json.dump({
                "completed": self.completed,
                "failed": list(self.failed),
                "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }, f, indent=2)
    
    def parse_batch_file(self, filepath: str) -> list[SoundEffect]:
        """Parse the SOUND_EFFECTS_BATCH.md file for Freesound sounds."""
        sounds = []
        current_category = "Unknown"
        
        with open(filepath, "r") as f:
            content = f.read()
        
        # Find section headers
        section_pattern = r"^# SECTION \d+: (.+)$"
        
        # Parse table rows: | SFX-ID | name | description | duration | source |
        row_pattern = r"\| (SFX-\w+) \| (\w+) \| (.+?) \| ([\d.]+s) \| (Freesound\.org|ElevenLabs SFX|Stable Audio) \|"
        
        lines = content.split('\n')
        for line in lines:
            section_match = re.match(section_pattern, line)
            if section_match:
                current_category = section_match.group(1)
            
            row_match = re.search(row_pattern, line)
            if row_match and row_match.group(5) == "Freesound.org":
                sounds.append(SoundEffect(
                    id=row_match.group(1),
                    name=row_match.group(2),
                    description=row_match.group(3).strip(),
                    duration=row_match.group(4),
                    source=row_match.group(5),
                    category=current_category
                ))
        
        return sounds
    
    def build_search_query(self, sound: SoundEffect) -> str:
        """Build optimal search query from sound description."""
        # Extract key terms from description
        desc = sound.description.lower()
        name = sound.name.replace('_', ' ')
        
        # Combine name and key description words
        query = f"{name}"
        
        # Add category context
        if "ui" in sound.category.lower():
            query += " interface"
        elif "combat" in sound.category.lower():
            query += " game"
        elif "building" in sound.category.lower():
            query += " construction"
        
        return query
    
    def parse_duration(self, duration_str: str) -> float:
        """Parse duration string like '0.5s' to float."""
        return float(duration_str.replace('s', ''))
    
    def download_sound(self, sound: SoundEffect) -> bool:
        """Search and download best matching sound."""
        if sound.id in self.completed:
            print(f"  ⏭️  {sound.id} already downloaded, skipping...")
            return True
        
        print(f"\n{'='*60}")
        print(f"🔊 {sound.id}: {sound.name}")
        print(f"   Description: {sound.description}")
        print(f"   Duration: {sound.duration}")
        print(f"{'='*60}")
        
        try:
            # Build search query
            query = self.build_search_query(sound)
            target_duration = self.parse_duration(sound.duration)
            
            # Search with duration filter (allow 3x target duration)
            print(f"   🔍 Searching: '{query}'")
            max_dur = max(target_duration * 3, 2.0)  # At least 2 seconds max
            results = self.client.search(query, max_duration=max_dur)
            
            if not results:
                # Try broader search
                print(f"   🔍 Trying broader search...")
                broader_query = sound.name.replace('_', ' ')
                results = self.client.search(broader_query, max_duration=10.0)
            
            if not results:
                print(f"   ❌ No results found")
                self.failed.add(sound.id)
                return False
            
            # Pick best result (first one, already sorted by rating)
            best = results[0]
            print(f"   ✓ Found: '{best['name']}' ({best['duration']:.2f}s)")
            
            # Download preview
            output_path = self.output_dir / f"{sound.id}_{sound.name}.mp3"
            print(f"   📥 Downloading...")
            self.client.download_preview(best, str(output_path))
            
            # Record success
            self.completed[sound.id] = {
                "freesound_id": best["id"],
                "freesound_name": best["name"],
                "duration": best["duration"]
            }
            self._save_progress()
            
            print(f"   ✅ Saved: {output_path.name}")
            return True
            
        except Exception as e:
            print(f"   ❌ Error: {e}")
            self.failed.add(sound.id)
            return False
    
    def process_all(self, sounds: list[SoundEffect], delay: float = 0.5):
        """Download all sounds."""
        total = len(sounds)
        remaining = [s for s in sounds if s.id not in self.completed]
        
        print(f"\n{'='*60}")
        print(f"🔊 FREESOUND BATCH DOWNLOADER")
        print(f"{'='*60}")
        print(f"Total sounds: {total}")
        print(f"Already downloaded: {len(self.completed)}")
        print(f"Remaining: {len(remaining)}")
        print(f"Output: {self.output_dir}")
        print(f"{'='*60}\n")
        
        success_count = 0
        fail_count = 0
        
        for i, sound in enumerate(remaining, 1):
            print(f"\n[{i}/{len(remaining)}] Processing {sound.id}...")
            
            if self.download_sound(sound):
                success_count += 1
            else:
                fail_count += 1
            
            # Rate limiting - be nice to Freesound
            if i < len(remaining):
                time.sleep(delay)
        
        # Save final progress
        self._save_progress()
        
        print(f"\n{'='*60}")
        print(f"🏁 BATCH COMPLETE")
        print(f"{'='*60}")
        print(f"✅ Downloaded: {success_count}")
        print(f"❌ Failed: {fail_count}")
        print(f"📁 Output: {self.output_dir}")
        
        if self.failed:
            print(f"\n⚠️  Failed sounds (may need manual download):")
            for fid in sorted(self.failed):
                print(f"   - {fid}")
        
        print(f"{'='*60}")


def main():
    # Get API key
    api_key = os.environ.get("FREESOUND_API_KEY")
    if not api_key:
        print("❌ Error: FREESOUND_API_KEY environment variable not set")
        print("\nTo get an API key:")
        print("1. Go to https://freesound.org/apiv2/apply/")
        print("2. Create an application")
        print("3. Copy your API key")
        print("\nThen run:")
        print('  export FREESOUND_API_KEY="your_key_here"')
        print("  python download_sounds.py")
        return
    
    # Paths
    script_dir = Path(__file__).parent
    batch_file = script_dir.parent.parent / "docs" / "assets" / "SOUND_EFFECTS_BATCH.md"
    output_dir = script_dir / "output"
    
    if not batch_file.exists():
        print(f"❌ Error: Batch file not found: {batch_file}")
        return
    
    # Initialize
    client = FreesoundClient(api_key)
    downloader = BatchDownloader(client, str(output_dir))
    
    # Test API connection
    print("🔌 Testing Freesound API connection...")
    try:
        results = client.search("click", max_duration=1.0)
        print(f"   ✅ Connected! Found {len(results)} test results")
    except Exception as e:
        print(f"   ❌ API Error: {e}")
        return
    
    # Parse batch file
    print(f"\n📄 Parsing: {batch_file.name}")
    sounds = downloader.parse_batch_file(str(batch_file))
    print(f"   Found {len(sounds)} Freesound.org sounds")
    
    # Show categories
    categories = {}
    for s in sounds:
        categories[s.category] = categories.get(s.category, 0) + 1
    print("\n   Categories:")
    for cat, count in categories.items():
        print(f"   - {cat}: {count} sounds")
    
    # Process all
    downloader.process_all(sounds, delay=0.5)


if __name__ == "__main__":
    main()
