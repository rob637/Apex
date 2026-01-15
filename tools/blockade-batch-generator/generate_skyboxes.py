#!/usr/bin/env python3
"""
Blockade Labs Skybox Batch Generator

Automates the generation of 84 skybox assets from BLOCKADE_SKYBOX_BATCH.md
using the Blockade Labs API.

Usage:
    export BLOCKADE_API_KEY="your_api_key_here"
    python generate_skyboxes.py

API Documentation: https://api-documentation.blockadelabs.com/api/skybox.html
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
class SkyboxPrompt:
    """Represents a single skybox prompt from the batch file."""
    id: str
    name: str
    prompt: str
    category: str


class BlockadeClient:
    """Client for interacting with Blockade Labs API."""
    
    BASE_URL = "https://backend.blockadelabs.com/api/v1"
    
    def __init__(self, api_key: str):
        self.api_key = api_key
        self.session = requests.Session()
        self.session.headers.update({
            "x-api-key": api_key,
            "Content-Type": "application/json"
        })
    
    def get_styles(self) -> list:
        """Get available skybox styles."""
        response = self.session.get(
            f"{self.BASE_URL}/skybox/styles",
            params={"model_version": "3"}
        )
        response.raise_for_status()
        return response.json()
    
    def create_skybox(self, prompt: str, style_id: int = 67, enhance: bool = True) -> dict:
        """
        Create a new skybox generation request.
        
        Args:
            prompt: Description of the skybox
            style_id: Style ID (67 = Fantasy Landscape, see get_styles())
            enhance: Whether to use AI prompt enhancement
        
        Returns:
            API response with generation ID
        """
        payload = {
            "prompt": prompt,
            "skybox_style_id": style_id,
            "enhance_prompt": enhance
        }
        
        response = self.session.post(
            f"{self.BASE_URL}/skybox",
            json=payload
        )
        response.raise_for_status()
        return response.json()
    
    def get_skybox_status(self, skybox_id: int) -> dict:
        """Get the status of a skybox generation."""
        response = self.session.get(
            f"{self.BASE_URL}/imagine/requests/{skybox_id}"
        )
        response.raise_for_status()
        data = response.json()
        # API returns nested response under "request" key
        return data.get("request", data)
    
    def wait_for_completion(self, skybox_id: int, timeout: int = 300, poll_interval: int = 5) -> dict:
        """
        Wait for skybox generation to complete.
        
        Args:
            skybox_id: The ID of the skybox generation
            timeout: Maximum time to wait in seconds
            poll_interval: Time between status checks
        
        Returns:
            Completed skybox data
        """
        start_time = time.time()
        
        while time.time() - start_time < timeout:
            status = self.get_skybox_status(skybox_id)
            
            if status.get("status") == "complete":
                return status
            elif status.get("status") == "error":
                error_msg = status.get("error_message", "Unknown error")
                raise Exception(f"Skybox generation failed: {error_msg}")
            elif status.get("status") == "abort":
                raise Exception("Skybox generation was aborted")
            
            # Show progress
            progress = status.get("progress", 0)
            queue_position = status.get("queue_position", 0)
            if queue_position > 0:
                print(f"  Queue position: {queue_position}", end="\r")
            else:
                print(f"  Progress: {progress}%", end="\r")
            
            time.sleep(poll_interval)
        
        raise TimeoutError(f"Skybox generation timed out after {timeout}s")


class BatchProcessor:
    """Processes the batch file and manages generation."""
    
    # Skybox style IDs (from Blockade Labs)
    STYLES = {
        "fantasy": 67,           # Fantasy Landscape
        "digital_painting": 64,  # Digital Painting  
        "anime": 69,             # Anime Art Style
        "realistic": 70,         # Realistic
        "scifi": 72,             # Sci-Fi
    }
    
    def __init__(self, client: BlockadeClient, output_dir: str):
        self.client = client
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.progress_file = self.output_dir / "progress.json"
        self.completed = self._load_progress()
    
    def _load_progress(self) -> set:
        """Load previously completed skybox IDs."""
        if self.progress_file.exists():
            with open(self.progress_file, "r") as f:
                data = json.load(f)
                return set(data.get("completed", []))
        return set()
    
    def _save_progress(self):
        """Save progress to file."""
        with open(self.progress_file, "w") as f:
            json.dump({
                "completed": list(self.completed),
                "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }, f, indent=2)
    
    def parse_batch_file(self, filepath: str) -> list[SkyboxPrompt]:
        """Parse the BLOCKADE_SKYBOX_BATCH.md file."""
        prompts = []
        current_category = "Unknown"
        
        with open(filepath, "r") as f:
            content = f.read()
        
        # Find all sections
        section_pattern = r"# SECTION \d+: (.+?)(?=\n)"
        sections = re.finditer(section_pattern, content)
        section_starts = [(m.start(), m.group(1)) for m in sections]
        
        # Find all skybox entries
        # Pattern: ### SKY## - Name\n```\nprompt\n```
        skybox_pattern = r"### (SKY\d+) - (.+?)\n```\n(.+?)\n```"
        
        for match in re.finditer(skybox_pattern, content, re.DOTALL):
            skybox_id = match.group(1)
            name = match.group(2).strip()
            prompt = match.group(3).strip()
            
            # Find which section this belongs to
            pos = match.start()
            category = "Unknown"
            for start, cat_name in section_starts:
                if start < pos:
                    category = cat_name
            
            prompts.append(SkyboxPrompt(
                id=skybox_id,
                name=name,
                prompt=prompt,
                category=category
            ))
        
        return prompts
    
    def parse_essential_json(self, filepath: str) -> list[SkyboxPrompt]:
        """Parse the essential_skyboxes.json file (curated AR-optimized list)."""
        with open(filepath, "r") as f:
            data = json.load(f)
        
        prompts = []
        for item in data.get("skyboxes", []):
            prompts.append(SkyboxPrompt(
                id=item["id"],
                name=item["name"],
                prompt=item["prompt"],
                category=item.get("use", "General")
            ))
        
        return prompts
    
    def download_skybox(self, skybox_data: dict, skybox_id: str) -> str:
        """Download the generated skybox image."""
        file_url = skybox_data.get("file_url")
        if not file_url:
            raise Exception("No file URL in skybox data")
        
        # Download the file
        response = requests.get(file_url)
        response.raise_for_status()
        
        # Save as PNG (equirectangular)
        filename = f"{skybox_id}.png"
        filepath = self.output_dir / filename
        
        with open(filepath, "wb") as f:
            f.write(response.content)
        
        # Also try to get HDR if available
        hdr_url = skybox_data.get("hdri_url")
        if hdr_url:
            try:
                hdr_response = requests.get(hdr_url)
                hdr_response.raise_for_status()
                hdr_filepath = self.output_dir / f"{skybox_id}.hdr"
                with open(hdr_filepath, "wb") as f:
                    f.write(hdr_response.content)
                print(f"  Also saved HDR: {hdr_filepath.name}")
            except Exception as e:
                print(f"  HDR download failed (PNG saved): {e}")
        
        return str(filepath)
    
    def process_single(self, skybox_prompt: SkyboxPrompt, style: str = "fantasy") -> bool:
        """Process a single skybox prompt."""
        if skybox_prompt.id in self.completed:
            print(f"⏭️  {skybox_prompt.id} already completed, skipping...")
            return True
        
        print(f"\n{'='*60}")
        print(f"🌌 Generating: {skybox_prompt.id} - {skybox_prompt.name}")
        print(f"   Category: {skybox_prompt.category}")
        print(f"   Style: {style}")
        print(f"{'='*60}")
        
        try:
            # Get style ID
            style_id = self.STYLES.get(style, self.STYLES["fantasy"])
            
            # Create skybox
            print(f"📤 Sending to Blockade Labs API...")
            result = self.client.create_skybox(
                prompt=skybox_prompt.prompt,
                style_id=style_id,
                enhance=True
            )
            
            skybox_gen_id = result.get("id")
            if not skybox_gen_id:
                raise Exception("No ID returned from API")
            
            print(f"   Generation ID: {skybox_gen_id}")
            
            # Wait for completion
            print(f"⏳ Waiting for generation...")
            completed = self.client.wait_for_completion(skybox_gen_id)
            
            # Download
            print(f"\n📥 Downloading skybox...")
            filepath = self.download_skybox(completed, skybox_prompt.id)
            
            # Mark complete
            self.completed.add(skybox_prompt.id)
            self._save_progress()
            
            print(f"✅ Saved: {filepath}")
            return True
            
        except Exception as e:
            print(f"❌ Error processing {skybox_prompt.id}: {e}")
            return False
    
    def process_all(self, prompts: list[SkyboxPrompt], style: str = "fantasy", 
                    delay: int = 2, start_from: Optional[str] = None):
        """Process all skybox prompts."""
        total = len(prompts)
        remaining = [p for p in prompts if p.id not in self.completed]
        
        print(f"\n{'='*60}")
        print(f"🌌 BLOCKADE LABS SKYBOX BATCH GENERATOR")
        print(f"{'='*60}")
        print(f"Total skyboxes: {total}")
        print(f"Already completed: {len(self.completed)}")
        print(f"Remaining: {len(remaining)}")
        print(f"Style: {style}")
        print(f"Output: {self.output_dir}")
        print(f"{'='*60}\n")
        
        if start_from:
            # Find starting point
            start_idx = next((i for i, p in enumerate(remaining) if p.id == start_from), 0)
            remaining = remaining[start_idx:]
            print(f"Starting from: {start_from}")
        
        success_count = 0
        fail_count = 0
        
        for i, prompt in enumerate(remaining, 1):
            print(f"\n[{i}/{len(remaining)}] Processing {prompt.id}...")
            
            if self.process_single(prompt, style):
                success_count += 1
            else:
                fail_count += 1
            
            # Small delay between requests
            if i < len(remaining):
                print(f"⏱️  Waiting {delay}s before next request...")
                time.sleep(delay)
        
        print(f"\n{'='*60}")
        print(f"🏁 BATCH COMPLETE")
        print(f"{'='*60}")
        print(f"✅ Successful: {success_count}")
        print(f"❌ Failed: {fail_count}")
        print(f"📁 Output: {self.output_dir}")
        print(f"{'='*60}")


def main():
    import argparse
    
    parser = argparse.ArgumentParser(description="Generate Blockade Labs skyboxes")
    parser.add_argument("--all", action="store_true", help="Generate all 84 skyboxes (default: essential 15)")
    parser.add_argument("--style", default="fantasy", help="Style: fantasy, digital_painting, anime, realistic, scifi")
    args = parser.parse_args()
    
    # Get API key from environment
    api_key = os.environ.get("BLOCKADE_API_KEY")
    if not api_key:
        print("❌ Error: BLOCKADE_API_KEY environment variable not set")
        print("\nTo get an API key:")
        print("1. Go to https://skybox.blockadelabs.com/")
        print("2. Sign up/login")
        print("3. Go to API settings and generate a key")
        print("\nThen run:")
        print('  export BLOCKADE_API_KEY="your_key_here"')
        print("  python generate_skyboxes.py")
        return
    
    # Paths
    script_dir = Path(__file__).parent
    output_dir = script_dir / "output"
    
    # Initialize
    client = BlockadeClient(api_key)
    processor = BatchProcessor(client, str(output_dir))
    
    # Optional: List available styles
    print("📋 Fetching available styles...")
    try:
        styles = client.get_styles()
        print(f"   Found {len(styles)} styles available")
    except Exception as e:
        print(f"   Warning: Could not fetch styles: {e}")
    
    # Choose batch file based on --all flag
    if args.all:
        batch_file = script_dir.parent.parent / "docs" / "assets" / "BLOCKADE_SKYBOX_BATCH.md"
        print(f"\n📄 Mode: FULL (all 84 skyboxes)")
        print(f"   Parsing: {batch_file.name}")
        
        if not batch_file.exists():
            print(f"❌ Error: Batch file not found: {batch_file}")
            return
        
        prompts = processor.parse_batch_file(str(batch_file))
    else:
        essential_file = script_dir / "essential_skyboxes.json"
        print(f"\n📄 Mode: ESSENTIAL (15 AR-optimized skyboxes)")
        print(f"   Parsing: {essential_file.name}")
        
        if not essential_file.exists():
            print(f"❌ Error: Essential file not found: {essential_file}")
            return
        
        prompts = processor.parse_essential_json(str(essential_file))
    
    print(f"   Found {len(prompts)} skybox prompts")
    
    # Show what will be generated
    print("\n   Skyboxes to generate:")
    for p in prompts:
        status = "✓" if p.id in processor.completed else "○"
        print(f"   {status} {p.id}: {p.name}")
    
    # Process all
    processor.process_all(
        prompts,
        style=args.style,
        delay=2
    )


if __name__ == "__main__":
    main()
