#!/usr/bin/env python3
"""
ElevenLabs Sound Effects Batch Generator

Automatically generates sound effects using ElevenLabs SFX API.

Usage:
    export ELEVENLABS_API_KEY="your_api_key_here"
    python generate_sounds.py

API Documentation: https://elevenlabs.io/docs/api-reference/sound-effects
"""

import os
import re
import json
import time
import requests
from dataclasses import dataclass
from pathlib import Path
from datetime import datetime


@dataclass
class SoundEffect:
    """Represents a sound effect to generate."""
    id: str
    name: str
    description: str
    duration: str
    category: str


class ElevenLabsClient:
    """Client for ElevenLabs Sound Effects API."""
    
    BASE_URL = "https://api.elevenlabs.io/v1"
    
    def __init__(self, api_key: str):
        self.api_key = api_key
        self.session = requests.Session()
        self.session.headers.update({
            "xi-api-key": api_key,
            "Content-Type": "application/json"
        })
    
    def generate_sound(self, text: str, duration_seconds: float = None) -> bytes:
        """
        Generate a sound effect from text description.
        
        Args:
            text: Description of the sound effect
            duration_seconds: Optional duration hint
        
        Returns:
            Audio data as bytes (MP3)
        """
        payload = {
            "text": text,
        }
        
        if duration_seconds:
            # ElevenLabs requires 0.5-22 seconds
            payload["duration_seconds"] = max(0.5, min(duration_seconds, 22.0))
        
        response = self.session.post(
            f"{self.BASE_URL}/sound-generation",
            json=payload
        )
        response.raise_for_status()
        return response.content
    
    def get_user_info(self) -> dict:
        """Get user account info to check credits."""
        response = self.session.get(f"{self.BASE_URL}/user")
        response.raise_for_status()
        return response.json()


class BatchGenerator:
    """Generates sounds in batch from ElevenLabs."""
    
    def __init__(self, client: ElevenLabsClient, output_dir: str):
        self.client = client
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.progress_file = self.output_dir / "progress.json"
        self.completed = self._load_progress()
        self.failed = []
    
    def _load_progress(self) -> set:
        """Load completed sound IDs."""
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
                "failed": self.failed,
                "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }, f, indent=2)
    
    def parse_batch_file(self, filepath: str) -> list[SoundEffect]:
        """Parse the SOUND_EFFECTS_BATCH.md file for ElevenLabs sounds."""
        sounds = []
        current_category = "Unknown"
        
        with open(filepath, "r") as f:
            content = f.read()
        
        # Find section headers
        section_pattern = r"^# SECTION \d+: (.+)$"
        
        # Parse table rows: | SFX-ID | name | description | duration | source |
        row_pattern = r"\| (SFX-\w+) \| (\w+) \| (.+?) \| ([\d.]+s) \| ElevenLabs SFX \|"
        
        lines = content.split('\n')
        for line in lines:
            section_match = re.match(section_pattern, line)
            if section_match:
                current_category = section_match.group(1)
            
            row_match = re.search(row_pattern, line)
            if row_match:
                sounds.append(SoundEffect(
                    id=row_match.group(1),
                    name=row_match.group(2),
                    description=row_match.group(3).strip(),
                    duration=row_match.group(4),
                    category=current_category
                ))
        
        return sounds
    
    def build_prompt(self, sound: SoundEffect) -> str:
        """Build optimal prompt for ElevenLabs."""
        # Use description directly, it's usually well-written
        prompt = sound.description
        
        # Add context for game sounds
        if "notification" in sound.name.lower():
            prompt += ", game notification sound"
        elif "magical" in sound.description.lower() or "magic" in sound.description.lower():
            prompt += ", fantasy game sound effect"
        elif "reward" in sound.name.lower() or "chest" in sound.name.lower():
            prompt += ", video game reward sound"
        
        return prompt
    
    def parse_duration(self, duration_str: str) -> float:
        """Parse duration string like '0.5s' to float."""
        return float(duration_str.replace('s', ''))
    
    def generate_sound(self, sound: SoundEffect) -> bool:
        """Generate a single sound effect."""
        if sound.id in self.completed:
            print(f"  ⏭️  {sound.id} already generated, skipping...")
            return True
        
        print(f"\n{'='*60}")
        print(f"🎵 {sound.id}: {sound.name}")
        print(f"   Description: {sound.description}")
        print(f"   Duration: {sound.duration}")
        print(f"{'='*60}")
        
        try:
            # Build prompt
            prompt = self.build_prompt(sound)
            duration = self.parse_duration(sound.duration)
            
            print(f"   🔊 Generating: '{prompt[:60]}...'")
            
            # Generate sound
            audio_data = self.client.generate_sound(prompt, duration_seconds=duration)
            
            # Save file
            output_path = self.output_dir / f"{sound.id}_{sound.name}.mp3"
            with open(output_path, "wb") as f:
                f.write(audio_data)
            
            # Record success
            self.completed.add(sound.id)
            self._save_progress()
            
            print(f"   ✅ Saved: {output_path.name} ({len(audio_data)} bytes)")
            return True
            
        except requests.exceptions.HTTPError as e:
            error_msg = str(e)
            try:
                error_detail = e.response.json()
                error_msg = error_detail.get("detail", {}).get("message", str(e))
            except:
                pass
            print(f"   ❌ API Error: {error_msg}")
            self.failed.append({"id": sound.id, "error": error_msg})
            return False
        except Exception as e:
            print(f"   ❌ Error: {e}")
            self.failed.append({"id": sound.id, "error": str(e)})
            return False
    
    def process_all(self, sounds: list[SoundEffect], delay: float = 1.0):
        """Generate all sounds."""
        total = len(sounds)
        remaining = [s for s in sounds if s.id not in self.completed]
        
        print(f"\n{'='*60}")
        print(f"🎵 ELEVENLABS SOUND EFFECTS GENERATOR")
        print(f"{'='*60}")
        print(f"Total sounds: {total}")
        print(f"Already generated: {len(self.completed)}")
        print(f"Remaining: {len(remaining)}")
        print(f"Output: {self.output_dir}")
        print(f"{'='*60}\n")
        
        success_count = 0
        fail_count = 0
        
        for i, sound in enumerate(remaining, 1):
            print(f"\n[{i}/{len(remaining)}] Processing {sound.id}...")
            
            if self.generate_sound(sound):
                success_count += 1
            else:
                fail_count += 1
            
            # Rate limiting
            if i < len(remaining):
                time.sleep(delay)
        
        # Save final progress
        self._save_progress()
        
        print(f"\n{'='*60}")
        print(f"🏁 BATCH COMPLETE")
        print(f"{'='*60}")
        print(f"✅ Generated: {success_count}")
        print(f"❌ Failed: {fail_count}")
        print(f"📁 Output: {self.output_dir}")
        print(f"{'='*60}")


def main():
    # Get API key
    api_key = os.environ.get("ELEVENLABS_API_KEY")
    if not api_key:
        print("❌ Error: ELEVENLABS_API_KEY environment variable not set")
        print("\nTo get an API key:")
        print("1. Go to https://elevenlabs.io/")
        print("2. Sign up and go to Profile Settings")
        print("3. Copy your API key")
        print("\nThen run:")
        print('  export ELEVENLABS_API_KEY="your_key_here"')
        print("  python generate_sounds.py")
        return
    
    # Paths
    script_dir = Path(__file__).parent
    batch_file = script_dir.parent.parent / "docs" / "assets" / "SOUND_EFFECTS_BATCH.md"
    output_dir = script_dir / "output"
    
    if not batch_file.exists():
        print(f"❌ Error: Batch file not found: {batch_file}")
        return
    
    # Initialize
    client = ElevenLabsClient(api_key)
    generator = BatchGenerator(client, str(output_dir))
    
    # Test API connection with a simple sound
    print("🔌 Testing ElevenLabs API connection...")
    try:
        test_audio = client.generate_sound("test beep", duration_seconds=0.5)
        if len(test_audio) > 1000:
            print(f"   ✅ Connected! Test sound: {len(test_audio)} bytes")
        else:
            print(f"   ⚠️  API returned small response, may have issues")
    except Exception as e:
        print(f"   ❌ API Error: {e}")
        return
    
    # Parse batch file
    print(f"\n📄 Parsing: {batch_file.name}")
    sounds = generator.parse_batch_file(str(batch_file))
    print(f"   Found {len(sounds)} ElevenLabs sounds")
    
    # Show categories
    categories = {}
    for s in sounds:
        categories[s.category] = categories.get(s.category, 0) + 1
    print("\n   Categories:")
    for cat, count in categories.items():
        print(f"   - {cat}: {count} sounds")
    
    # Process all
    generator.process_all(sounds, delay=1.0)


if __name__ == "__main__":
    main()
