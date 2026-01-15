#!/usr/bin/env python3
"""
Mixamo Animation Batch Downloader - Cookie Auth Version

This version uses a one-time browser login to capture cookies,
then uses those cookies for API-based batch downloading.

STEP 1: Run with --login to open browser and login manually
STEP 2: Run without --login to batch download using saved cookies

Usage:
    python download_mixamo.py --login     # One-time: opens browser for login
    python download_mixamo.py             # Batch download using saved auth
"""

import os
import re
import sys
import json
import time
import pickle
import requests
from pathlib import Path
from datetime import datetime
from dataclasses import dataclass


@dataclass
class Animation:
    id: str
    name: str
    search_term: str
    in_place: bool
    loop: bool
    category: str


class MixamoDownloader:
    """Download animations using Mixamo's internal API."""
    
    MIXAMO_URL = "https://www.mixamo.com"
    API_URL = "https://www.mixamo.com/api/v1"
    
    def __init__(self, output_dir: str):
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.cookies_file = self.output_dir / "cookies.pkl"
        self.progress_file = self.output_dir / "progress.json"
        self.session = requests.Session()
        self.session.headers.update({
            "User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36",
            "Accept": "application/json, text/plain, */*",
            "Accept-Language": "en-US,en;q=0.9",
            "X-Requested-With": "XMLHttpRequest",
            "Origin": self.MIXAMO_URL,
            "Referer": f"{self.MIXAMO_URL}/",
        })
        self.character_id = None
        self.completed = self._load_progress()
        self.failed = []
    
    def _load_progress(self) -> set:
        if self.progress_file.exists():
            with open(self.progress_file, "r") as f:
                return set(json.load(f).get("completed", []))
        return set()
    
    def _save_progress(self):
        with open(self.progress_file, "w") as f:
            json.dump({
                "completed": sorted(list(self.completed)),
                "failed": self.failed,
                "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }, f, indent=2)
    
    def save_cookies(self, cookies: dict):
        """Save cookies from manual browser login."""
        with open(self.cookies_file, "wb") as f:
            pickle.dump(cookies, f)
        print(f"   ✅ Cookies saved to {self.cookies_file}")
    
    def load_cookies(self) -> bool:
        """Load saved cookies."""
        if not self.cookies_file.exists():
            return False
        
        try:
            with open(self.cookies_file, "rb") as f:
                cookies = pickle.load(f)
            self.session.cookies.update(cookies)
            return True
        except Exception as e:
            print(f"   ❌ Failed to load cookies: {e}")
            return False
    
    def verify_auth(self) -> bool:
        """Verify authentication is working."""
        try:
            response = self.session.get(f"{self.API_URL}/characters", params={"limit": 1})
            return response.status_code == 200
        except:
            return False
    
    def get_default_character(self) -> str:
        """Get the default Y Bot character ID."""
        try:
            response = self.session.get(
                f"{self.API_URL}/characters",
                params={"page": 1, "limit": 96, "order": ""}
            )
            if response.status_code == 200:
                data = response.json()
                for char in data.get("results", []):
                    name = char.get("name", "").lower()
                    if "y bot" in name or "ybot" in name:
                        self.character_id = char["id"]
                        print(f"   ✅ Using character: {char['name']}")
                        return self.character_id
                
                # Fallback to first character
                if data.get("results"):
                    self.character_id = data["results"][0]["id"]
                    print(f"   ✅ Using character: {data['results'][0]['name']}")
                    return self.character_id
        except Exception as e:
            print(f"   ❌ Character fetch error: {e}")
        return None
    
    def search_animation(self, query: str) -> dict:
        """Search for an animation."""
        try:
            response = self.session.get(
                f"{self.API_URL}/products",
                params={
                    "page": 1,
                    "limit": 24,
                    "order": "relevance",
                    "query": query,
                    "type": "Motion"
                }
            )
            if response.status_code == 200:
                data = response.json()
                results = data.get("results", [])
                if results:
                    return results[0]
        except Exception as e:
            print(f"   ❌ Search error: {e}")
        return None
    
    def export_animation(self, product_id: str, in_place: bool = True) -> str:
        """Request animation export and get download URL."""
        try:
            # Request export
            export_body = {
                "character_id": self.character_id,
                "product_name": product_id,
                "preferences": {
                    "format": "fbx7",
                    "skin": "false",
                    "fps": "30",
                    "reducekf": "0",
                    "inplace": "true" if in_place else "false"
                }
            }
            
            response = self.session.post(
                f"{self.API_URL}/animations/export",
                json=export_body
            )
            
            if response.status_code in [200, 201, 202]:
                # Export request returns immediately with status
                # Check if direct download URL or job ID
                data = response.json()
                
                # Some responses include direct URL
                if "result" in data and "url" in data.get("result", {}):
                    return data["result"]["url"]
                
                # Others need polling
                job_id = data.get("uuid") or data.get("job_id")
                if job_id:
                    return self._poll_export(job_id)
            
            print(f"   ❌ Export failed: {response.status_code} - {response.text[:100]}")
            
        except Exception as e:
            print(f"   ❌ Export error: {e}")
        return None
    
    def _poll_export(self, job_id: str, timeout: int = 60) -> str:
        """Poll for export completion."""
        start = time.time()
        while time.time() - start < timeout:
            try:
                response = self.session.get(
                    f"{self.API_URL}/characters/{self.character_id}/monitor",
                    params={"status": job_id}
                )
                
                if response.status_code == 200:
                    data = response.json()
                    status = data.get("status")
                    
                    if status == "completed":
                        return data.get("result", {}).get("url")
                    elif status == "failed":
                        return None
                
                time.sleep(2)
            except:
                time.sleep(2)
        
        return None
    
    def download_file(self, url: str, output_path: Path) -> bool:
        """Download file from URL."""
        try:
            response = self.session.get(url, stream=True)
            if response.status_code == 200:
                with open(output_path, "wb") as f:
                    for chunk in response.iter_content(chunk_size=8192):
                        f.write(chunk)
                return True
        except Exception as e:
            print(f"   ❌ Download error: {e}")
        return False
    
    def process_animation(self, anim: Animation) -> bool:
        """Process a single animation."""
        if anim.id in self.completed:
            print(f"  ⏭️  {anim.id} already done")
            return True
        
        print(f"\n{'='*60}")
        print(f"🎬 {anim.id}: {anim.name}")
        print(f"   Search: \"{anim.search_term}\"")
        
        # Search
        result = self.search_animation(anim.search_term)
        if not result:
            self.failed.append({"id": anim.id, "error": "Not found"})
            print(f"   ❌ Not found")
            return False
        
        product_id = result.get("id") or result.get("product_name")
        print(f"   ✓ Found: {result.get('name', product_id)}")
        
        # Export
        print(f"   ⏳ Exporting...")
        download_url = self.export_animation(product_id, anim.in_place)
        if not download_url:
            self.failed.append({"id": anim.id, "error": "Export failed"})
            return False
        
        # Download
        output_path = self.output_dir / f"{anim.id}_{anim.name.replace(' ', '_').lower()}.fbx"
        if self.download_file(download_url, output_path):
            self.completed.add(anim.id)
            self._save_progress()
            size_kb = output_path.stat().st_size // 1024
            print(f"   ✅ Saved: {output_path.name} ({size_kb}KB)")
            return True
        
        self.failed.append({"id": anim.id, "error": "Download failed"})
        return False
    
    def process_all(self, animations: list[Animation]):
        """Process all animations."""
        remaining = [a for a in animations if a.id not in self.completed]
        
        print(f"\n{'='*60}")
        print(f"📥 MIXAMO BATCH DOWNLOAD")
        print(f"   Total: {len(animations)}")
        print(f"   Done: {len(self.completed)}")
        print(f"   Remaining: {len(remaining)}")
        print(f"{'='*60}")
        
        for i, anim in enumerate(remaining, 1):
            print(f"\n[{i}/{len(remaining)}]", end="")
            self.process_animation(anim)
            time.sleep(1)  # Rate limiting
        
        self._save_progress()
        
        print(f"\n{'='*60}")
        print(f"✅ Complete: {len(self.completed)}/{len(animations)}")
        print(f"❌ Failed: {len(self.failed)}")
        print(f"{'='*60}")


def parse_batch_file(filepath: str) -> list[Animation]:
    """Parse the MIXAMO_ANIMATION_BATCH.md file."""
    animations = []
    category = "Unknown"
    
    with open(filepath, "r") as f:
        for line in f:
            if line.startswith("# SECTION"):
                match = re.search(r"SECTION \d+: (.+)", line)
                if match:
                    category = match.group(1)
            
            match = re.match(r'\| (ANI-\w+) \| (.+?) \| "(.+?)" \| (Yes|No) \| (Yes|No) \|', line)
            if match:
                animations.append(Animation(
                    id=match.group(1),
                    name=match.group(2).strip(),
                    search_term=match.group(3),
                    in_place=match.group(4) == "Yes",
                    loop=match.group(5) == "Yes",
                    category=category
                ))
    
    return animations


def setup_interactive_login():
    """
    Guide user through getting cookies manually.
    Since we can't run a browser in Codespaces, provide instructions.
    """
    print("""
╔══════════════════════════════════════════════════════════════════╗
║           MIXAMO COOKIE AUTHENTICATION SETUP                      ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                   ║
║  Since we can't run a browser here, you need to get cookies       ║
║  from your local browser:                                         ║
║                                                                   ║
║  1. Open https://www.mixamo.com in Chrome                         ║
║  2. Login with your Adobe account                                 ║
║  3. Open DevTools (F12) → Network tab                             ║
║  4. Click any animation                                           ║
║  5. Find a request to mixamo.com/api/v1/...                       ║
║  6. Right-click → Copy → Copy as cURL                             ║
║  7. Paste it below when prompted                                  ║
║                                                                   ║
╚══════════════════════════════════════════════════════════════════╝
""")


def main():
    script_dir = Path(__file__).parent
    batch_file = script_dir.parent.parent / "docs" / "assets" / "MIXAMO_ANIMATION_BATCH.md"
    output_dir = script_dir / "output"
    
    downloader = MixamoDownloader(str(output_dir))
    
    # Check for --login flag
    if "--login" in sys.argv:
        setup_interactive_login()
        return
    
    # Check for cookie file
    if not downloader.cookies_file.exists():
        print("❌ No authentication found.")
        print("   Run: python download_mixamo.py --login")
        print("   Or provide cookies.pkl file")
        return
    
    # Load cookies
    print("🔐 Loading saved authentication...")
    if not downloader.load_cookies():
        print("❌ Failed to load cookies")
        return
    
    # Verify auth
    print("   Verifying...")
    if not downloader.verify_auth():
        print("❌ Authentication expired. Run --login again")
        return
    print("   ✅ Authenticated")
    
    # Get character
    print("\n👤 Getting character...")
    if not downloader.get_default_character():
        print("❌ Failed to get character")
        return
    
    # Parse and process
    print(f"\n📄 Parsing: {batch_file.name}")
    animations = parse_batch_file(str(batch_file))
    print(f"   Found {len(animations)} animations")
    
    downloader.process_all(animations)


if __name__ == "__main__":
    main()
