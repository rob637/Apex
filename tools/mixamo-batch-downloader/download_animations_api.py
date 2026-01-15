#!/usr/bin/env python3
"""
Mixamo Animation Batch Downloader - API Version

Uses Mixamo's internal API (reverse-engineered from their web interface).
This requires authenticating through Adobe's identity service.

Usage:
    export MIXAMO_EMAIL="your@email.com"
    export MIXAMO_PASSWORD="your_password"
    python download_animations_api.py
"""

import os
import re
import json
import time
import requests
from pathlib import Path
from datetime import datetime
from dataclasses import dataclass
from urllib.parse import urlencode


@dataclass
class Animation:
    """Represents an animation to download."""
    id: str
    name: str
    search_term: str
    in_place: bool
    loop: bool
    category: str


class MixamoAPI:
    """Mixamo API client using internal endpoints."""
    
    # Adobe IMS (Identity Management Service) endpoints
    ADOBE_IMS_URL = "https://ims-na1.adobelogin.com"
    ADOBE_AUTH_URL = "https://adobeid-na1.services.adobe.com"
    
    # Mixamo API endpoints
    MIXAMO_API = "https://www.mixamo.com/api/v1"
    
    # Client ID for Mixamo (from their web app)
    CLIENT_ID = "mixamo1"
    
    def __init__(self, email: str, password: str):
        self.email = email
        self.password = password
        self.session = requests.Session()
        self.access_token = None
        self.character_id = None
        
        # Set up session headers
        self.session.headers.update({
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            "Accept": "application/json",
            "Accept-Language": "en-US,en;q=0.9",
            "Origin": "https://www.mixamo.com",
            "Referer": "https://www.mixamo.com/",
        })
    
    def login(self) -> bool:
        """Authenticate with Adobe and get access token for Mixamo."""
        print("🔐 Authenticating with Adobe...")
        
        try:
            # Step 1: Initialize OAuth flow
            auth_params = {
                "client_id": self.CLIENT_ID,
                "scope": "openid,creative_cloud,gnav,additional_info.roles",
                "response_type": "token",
                "locale": "en_US",
                "redirect_uri": "https://www.mixamo.com/",
            }
            
            # Get the authentication page/token
            init_url = f"{self.ADOBE_IMS_URL}/ims/authorize/v2?{urlencode(auth_params)}"
            
            # First, try direct API authentication
            auth_data = {
                "username": self.email,
                "password": self.password,
                "client_id": self.CLIENT_ID,
            }
            
            # Adobe's authentication endpoint
            auth_response = self.session.post(
                f"{self.ADOBE_IMS_URL}/ims/token/v3",
                data={
                    "grant_type": "password",
                    "username": self.email,
                    "password": self.password,
                    "client_id": self.CLIENT_ID,
                    "scope": "openid,creative_cloud",
                },
                headers={"Content-Type": "application/x-www-form-urlencoded"}
            )
            
            if auth_response.status_code == 200:
                token_data = auth_response.json()
                self.access_token = token_data.get("access_token")
                if self.access_token:
                    self.session.headers["Authorization"] = f"Bearer {self.access_token}"
                    print("   ✅ Authentication successful!")
                    return True
            
            # If direct auth fails, try the web flow simulation
            print(f"   ⚠️ Direct auth returned: {auth_response.status_code}")
            print(f"   Response: {auth_response.text[:200]}")
            
            # Alternative: Try SUSI (Sign Up Sign In) endpoint
            susi_response = self.session.post(
                "https://auth.services.adobe.com/signin/v2/login",
                json={
                    "username": self.email,
                    "password": self.password,
                },
                headers={
                    "X-IMS-CLIENTID": self.CLIENT_ID,
                    "Content-Type": "application/json",
                }
            )
            
            if susi_response.status_code == 200:
                token_data = susi_response.json()
                self.access_token = token_data.get("access_token") or token_data.get("token")
                if self.access_token:
                    self.session.headers["Authorization"] = f"Bearer {self.access_token}"
                    print("   ✅ Authentication successful (SUSI)!")
                    return True
            
            print(f"   ❌ SUSI auth returned: {susi_response.status_code}")
            return False
            
        except Exception as e:
            print(f"   ❌ Authentication error: {e}")
            return False
    
    def get_character(self) -> bool:
        """Get or set the default character for animations."""
        print("\n👤 Getting character...")
        
        try:
            # Get list of characters
            response = self.session.get(
                f"{self.MIXAMO_API}/characters",
                params={"page": 1, "limit": 24, "order": ""}
            )
            
            if response.status_code == 200:
                data = response.json()
                characters = data.get("results", [])
                if characters:
                    # Use Y Bot (standard animation character)
                    for char in characters:
                        if "y bot" in char.get("name", "").lower():
                            self.character_id = char.get("id")
                            print(f"   ✅ Using character: {char.get('name')} ({self.character_id})")
                            return True
                    
                    # Use first available
                    self.character_id = characters[0].get("id")
                    print(f"   ✅ Using character: {characters[0].get('name')}")
                    return True
            
            print(f"   ❌ Failed to get characters: {response.status_code}")
            return False
            
        except Exception as e:
            print(f"   ❌ Character error: {e}")
            return False
    
    def search_animations(self, query: str, limit: int = 24) -> list:
        """Search for animations by name."""
        try:
            response = self.session.get(
                f"{self.MIXAMO_API}/products",
                params={
                    "page": 1,
                    "limit": limit,
                    "order": "relevance",
                    "query": query,
                    "type": "Motion"
                }
            )
            
            if response.status_code == 200:
                data = response.json()
                return data.get("results", [])
            
            return []
            
        except Exception as e:
            print(f"   ❌ Search error: {e}")
            return []
    
    def download_animation(self, animation_id: str, character_id: str, 
                          in_place: bool = True, output_path: str = None) -> bool:
        """Download an animation as FBX."""
        try:
            # Request export
            export_data = {
                "character_id": character_id,
                "product_name": animation_id,
                "preferences": {
                    "format": "fbx7",
                    "skin": "false",
                    "fps": "30",
                    "reducekf": "0",
                    "inplace": "true" if in_place else "false"
                }
            }
            
            # Start export job
            response = self.session.post(
                f"{self.MIXAMO_API}/animations/export",
                json=export_data
            )
            
            if response.status_code in [200, 201, 202]:
                job_data = response.json()
                job_id = job_data.get("job_id") or job_data.get("uuid")
                
                if job_id:
                    # Poll for completion
                    return self._wait_and_download(job_id, output_path)
            
            print(f"   ❌ Export request failed: {response.status_code}")
            return False
            
        except Exception as e:
            print(f"   ❌ Download error: {e}")
            return False
    
    def _wait_and_download(self, job_id: str, output_path: str, timeout: int = 120) -> bool:
        """Wait for export job and download result."""
        start_time = time.time()
        
        while time.time() - start_time < timeout:
            try:
                response = self.session.get(f"{self.MIXAMO_API}/exports/{job_id}")
                
                if response.status_code == 200:
                    data = response.json()
                    status = data.get("status")
                    
                    if status == "completed":
                        download_url = data.get("result", {}).get("url")
                        if download_url:
                            return self._download_file(download_url, output_path)
                    
                    elif status == "failed":
                        print(f"   ❌ Export failed")
                        return False
                
                time.sleep(2)
                
            except Exception as e:
                print(f"   ⚠️ Poll error: {e}")
                time.sleep(2)
        
        print(f"   ❌ Export timeout")
        return False
    
    def _download_file(self, url: str, output_path: str) -> bool:
        """Download file from URL."""
        try:
            response = self.session.get(url, stream=True)
            if response.status_code == 200:
                with open(output_path, "wb") as f:
                    for chunk in response.iter_content(chunk_size=8192):
                        f.write(chunk)
                return True
        except Exception as e:
            print(f"   ❌ File download error: {e}")
        return False


class BatchDownloader:
    """Batch download manager."""
    
    def __init__(self, api: MixamoAPI, output_dir: str):
        self.api = api
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.progress_file = self.output_dir / "progress.json"
        self.completed = self._load_progress()
        self.failed = []
    
    def _load_progress(self) -> set:
        if self.progress_file.exists():
            with open(self.progress_file, "r") as f:
                data = json.load(f)
                return set(data.get("completed", []))
        return set()
    
    def _save_progress(self):
        with open(self.progress_file, "w") as f:
            json.dump({
                "completed": sorted(list(self.completed)),
                "failed": self.failed,
                "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }, f, indent=2)
    
    def process_animation(self, animation: Animation) -> bool:
        """Process a single animation."""
        if animation.id in self.completed:
            print(f"  ⏭️  {animation.id} already downloaded")
            return True
        
        print(f"\n{'='*60}")
        print(f"🎬 {animation.id}: {animation.name}")
        print(f"   Search: \"{animation.search_term}\"")
        print(f"{'='*60}")
        
        try:
            # Search for animation
            results = self.api.search_animations(animation.search_term)
            if not results:
                raise Exception("No results found")
            
            # Get first result
            anim_data = results[0]
            anim_product_id = anim_data.get("id") or anim_data.get("product_name")
            
            print(f"   Found: {anim_data.get('name', 'Unknown')}")
            
            # Download
            output_path = self.output_dir / f"{animation.id}_{animation.name.replace(' ', '_').lower()}.fbx"
            
            if self.api.download_animation(
                anim_product_id, 
                self.api.character_id,
                in_place=animation.in_place,
                output_path=str(output_path)
            ):
                self.completed.add(animation.id)
                self._save_progress()
                print(f"   ✅ Downloaded: {output_path.name}")
                return True
            else:
                raise Exception("Download failed")
            
        except Exception as e:
            print(f"   ❌ Failed: {e}")
            self.failed.append({"id": animation.id, "error": str(e)})
            return False
    
    def process_all(self, animations: list[Animation], delay: float = 2.0):
        """Process all animations."""
        remaining = [a for a in animations if a.id not in self.completed]
        
        print(f"\n{'='*60}")
        print(f"🎬 MIXAMO BATCH DOWNLOADER")
        print(f"{'='*60}")
        print(f"Total: {len(animations)}")
        print(f"Completed: {len(self.completed)}")
        print(f"Remaining: {len(remaining)}")
        print(f"{'='*60}")
        
        success = 0
        failed = 0
        
        for i, anim in enumerate(remaining, 1):
            print(f"\n[{i}/{len(remaining)}]")
            if self.process_animation(anim):
                success += 1
            else:
                failed += 1
            time.sleep(delay)
        
        self._save_progress()
        
        print(f"\n{'='*60}")
        print(f"🏁 COMPLETE")
        print(f"   ✅ Success: {success}")
        print(f"   ❌ Failed: {failed}")
        print(f"{'='*60}")


def parse_batch_file(filepath: str) -> list[Animation]:
    """Parse the MIXAMO_ANIMATION_BATCH.md file."""
    animations = []
    current_category = "Unknown"
    
    with open(filepath, "r") as f:
        content = f.read()
    
    for line in content.split('\n'):
        if line.startswith("# SECTION"):
            match = re.search(r"SECTION \d+: (.+)", line)
            if match:
                current_category = match.group(1)
        
        row_match = re.match(r'\| (ANI-\w+) \| (.+?) \| "(.+?)" \| (Yes|No) \| (Yes|No) \|', line)
        if row_match:
            animations.append(Animation(
                id=row_match.group(1),
                name=row_match.group(2).strip(),
                search_term=row_match.group(3),
                in_place=row_match.group(4) == "Yes",
                loop=row_match.group(5) == "Yes",
                category=current_category
            ))
    
    return animations


def main():
    email = os.environ.get("MIXAMO_EMAIL")
    password = os.environ.get("MIXAMO_PASSWORD")
    
    if not email or not password:
        print("❌ Error: Set MIXAMO_EMAIL and MIXAMO_PASSWORD environment variables")
        return
    
    script_dir = Path(__file__).parent
    batch_file = script_dir.parent.parent / "docs" / "assets" / "MIXAMO_ANIMATION_BATCH.md"
    output_dir = script_dir / "output"
    
    print(f"📄 Parsing: {batch_file.name}")
    animations = parse_batch_file(str(batch_file))
    print(f"   Found {len(animations)} animations")
    
    # Initialize API
    api = MixamoAPI(email, password)
    
    # Login
    if not api.login():
        print("\n❌ Failed to authenticate. Check credentials.")
        return
    
    # Get character
    if not api.get_character():
        print("\n❌ Failed to get character.")
        return
    
    # Process
    downloader = BatchDownloader(api, str(output_dir))
    downloader.process_all(animations)


if __name__ == "__main__":
    main()
