#!/usr/bin/env python3
"""
Mixamo Animation Batch Downloader

Automates downloading animations from Mixamo using Selenium browser automation.
Requires Chrome/Chromium and chromedriver.

Usage:
    export MIXAMO_EMAIL="your@email.com"
    export MIXAMO_PASSWORD="your_password"
    python download_animations.py

Requirements:
    pip install selenium webdriver-manager requests
"""

import os
import re
import json
import time
import requests
from pathlib import Path
from datetime import datetime
from dataclasses import dataclass
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from selenium.common.exceptions import TimeoutException, NoSuchElementException
from webdriver_manager.chrome import ChromeDriverManager


@dataclass
class Animation:
    """Represents an animation to download."""
    id: str
    name: str
    search_term: str
    in_place: bool
    loop: bool
    category: str


class MixamoDownloader:
    """Automated Mixamo animation downloader."""
    
    MIXAMO_URL = "https://www.mixamo.com/"
    
    def __init__(self, email: str, password: str, output_dir: str, headless: bool = False):
        self.email = email
        self.password = password
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.progress_file = self.output_dir / "progress.json"
        self.completed = self._load_progress()
        self.failed = []
        self.headless = headless
        self.driver = None
        self.bearer_token = None
        self.character_id = None
    
    def _load_progress(self) -> set:
        """Load completed animation IDs."""
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
    
    def setup_browser(self):
        """Initialize Chrome browser."""
        options = Options()
        if self.headless:
            options.add_argument("--headless")
        options.add_argument("--no-sandbox")
        options.add_argument("--disable-dev-shm-usage")
        options.add_argument("--window-size=1920,1080")
        options.add_argument("--disable-gpu")
        
        # Set download directory
        prefs = {
            "download.default_directory": str(self.output_dir.absolute()),
            "download.prompt_for_download": False,
            "download.directory_upgrade": True,
        }
        options.add_experimental_option("prefs", prefs)
        
        service = Service(ChromeDriverManager().install())
        self.driver = webdriver.Chrome(service=service, options=options)
        self.driver.implicitly_wait(10)
        print(f"   ✅ Browser initialized")
    
    def login(self) -> bool:
        """Login to Mixamo via Adobe account."""
        print("\n🔐 Logging into Mixamo...")
        
        try:
            self.driver.get(self.MIXAMO_URL)
            time.sleep(3)
            
            # Try multiple selectors for Sign In button
            sign_in_selectors = [
                "//button[contains(text(), 'Sign')]",
                "//button[contains(text(), 'Log')]", 
                "//a[contains(text(), 'Sign')]",
                "//a[contains(@href, 'login')]",
                "//*[contains(@class, 'sign-in')]",
                "//*[contains(@class, 'login')]",
            ]
            
            sign_in_btn = None
            for selector in sign_in_selectors:
                try:
                    sign_in_btn = WebDriverWait(self.driver, 3).until(
                        EC.element_to_be_clickable((By.XPATH, selector))
                    )
                    print(f"   Found sign-in button with: {selector}")
                    break
                except:
                    continue
            
            if sign_in_btn:
                sign_in_btn.click()
                time.sleep(3)
            
            # Wait for Adobe login page - try multiple possible selectors
            email_selectors = [
                (By.ID, "EmailPage-EmailField"),
                (By.NAME, "username"),
                (By.NAME, "email"),
                (By.CSS_SELECTOR, "input[type='email']"),
                (By.CSS_SELECTOR, "input[name='username']"),
                (By.XPATH, "//input[@placeholder='Email address']"),
            ]
            
            email_field = None
            for by, selector in email_selectors:
                try:
                    email_field = WebDriverWait(self.driver, 5).until(
                        EC.presence_of_element_located((by, selector))
                    )
                    print(f"   Found email field")
                    break
                except:
                    continue
            
            if not email_field:
                # Take screenshot for debugging
                self.driver.save_screenshot("login_debug.png")
                print(f"   ⚠️ Could not find email field. Screenshot saved to login_debug.png")
                print(f"   Current URL: {self.driver.current_url}")
                print(f"   Page title: {self.driver.title}")
                
                # Check if already logged in
                if "mixamo.com" in self.driver.current_url and "login" not in self.driver.current_url.lower():
                    print("   ✅ Appears to be already logged in!")
                    return True
                return False
            
            # Enter email
            email_field.clear()
            email_field.send_keys(self.email)
            time.sleep(1)
            
            # Click Continue - try multiple selectors
            continue_selectors = [
                (By.XPATH, "//button[@data-id='EmailPage-ContinueButton']"),
                (By.XPATH, "//button[contains(text(), 'Continue')]"),
                (By.XPATH, "//button[contains(text(), 'Next')]"),
                (By.XPATH, "//button[@type='submit']"),
                (By.CSS_SELECTOR, "button[type='submit']"),
            ]
            
            for by, selector in continue_selectors:
                try:
                    continue_btn = self.driver.find_element(by, selector)
                    continue_btn.click()
                    print(f"   Clicked continue button")
                    break
                except:
                    continue
            
            time.sleep(3)
            
            # Enter password - try multiple selectors
            password_selectors = [
                (By.ID, "PasswordPage-PasswordField"),
                (By.NAME, "password"),
                (By.CSS_SELECTOR, "input[type='password']"),
                (By.XPATH, "//input[@type='password']"),
            ]
            
            password_field = None
            for by, selector in password_selectors:
                try:
                    password_field = WebDriverWait(self.driver, 10).until(
                        EC.presence_of_element_located((by, selector))
                    )
                    print(f"   Found password field")
                    break
                except:
                    continue
            
            if not password_field:
                self.driver.save_screenshot("password_debug.png")
                print(f"   ⚠️ Could not find password field. Screenshot saved.")
                return False
            
            password_field.clear()
            password_field.send_keys(self.password)
            time.sleep(1)
            
            # Click login/continue
            for by, selector in continue_selectors:
                try:
                    continue_btn = self.driver.find_element(by, selector)
                    continue_btn.click()
                    print(f"   Clicked login button")
                    break
                except:
                    continue
            
            # Wait for Mixamo to load after login
            print("   Waiting for Mixamo to load...")
            time.sleep(8)
            
            # Check if we're on Mixamo
            if "mixamo.com" in self.driver.current_url:
                print("   ✅ Logged in successfully")
                return True
            else:
                self.driver.save_screenshot("post_login_debug.png")
                print(f"   ⚠️ Unexpected URL after login: {self.driver.current_url}")
                return False
            
        except TimeoutException as e:
            self.driver.save_screenshot("timeout_debug.png")
            print(f"   ❌ Login timeout. Screenshot saved.")
            print(f"   Current URL: {self.driver.current_url}")
            return False
        except Exception as e:
            self.driver.save_screenshot("error_debug.png")
            print(f"   ❌ Login failed: {e}")
            return False
    
    def extract_bearer_token(self):
        """Extract bearer token from browser for API calls."""
        try:
            # Execute JavaScript to get token from localStorage or session
            token = self.driver.execute_script("""
                return localStorage.getItem('access_token') || 
                       sessionStorage.getItem('access_token') ||
                       null;
            """)
            if token:
                self.bearer_token = token
                print(f"   ✅ Bearer token extracted")
                return True
            
            # Alternative: get from cookies
            cookies = self.driver.get_cookies()
            for cookie in cookies:
                if 'token' in cookie['name'].lower():
                    self.bearer_token = cookie['value']
                    print(f"   ✅ Token from cookie: {cookie['name']}")
                    return True
                    
        except Exception as e:
            print(f"   ⚠️ Could not extract token: {e}")
        return False
    
    def select_character(self):
        """Select a character to apply animations to."""
        print("\n👤 Selecting character...")
        try:
            # Click on Characters tab
            char_tab = WebDriverWait(self.driver, 10).until(
                EC.element_to_be_clickable((By.XPATH, "//div[contains(@class, 'product-panel')]//span[contains(text(), 'Characters')]"))
            )
            char_tab.click()
            time.sleep(2)
            
            # Select first available character (Y Bot is good for all animations)
            char_item = WebDriverWait(self.driver, 10).until(
                EC.element_to_be_clickable((By.XPATH, "//div[contains(@class, 'product-thumbnail')]"))
            )
            char_item.click()
            time.sleep(3)
            
            print("   ✅ Character selected")
            return True
            
        except Exception as e:
            print(f"   ❌ Character selection failed: {e}")
            return False
    
    def search_animation(self, search_term: str) -> bool:
        """Search for an animation by name."""
        try:
            # Click Animations tab if not already there
            anim_tab = self.driver.find_element(By.XPATH, "//div[contains(@class, 'product-panel')]//span[contains(text(), 'Animations')]")
            anim_tab.click()
            time.sleep(1)
            
            # Find and clear search box
            search_box = WebDriverWait(self.driver, 10).until(
                EC.presence_of_element_located((By.XPATH, "//input[@placeholder='Search animations...']"))
            )
            search_box.clear()
            search_box.send_keys(search_term)
            search_box.send_keys(Keys.RETURN)
            time.sleep(2)
            
            return True
            
        except Exception as e:
            print(f"   ❌ Search failed: {e}")
            return False
    
    def select_first_animation(self) -> bool:
        """Select the first animation from search results."""
        try:
            # Wait for results to load
            anim_item = WebDriverWait(self.driver, 10).until(
                EC.element_to_be_clickable((By.XPATH, "//div[contains(@class, 'product-thumbnail') and contains(@class, 'animation')]"))
            )
            anim_item.click()
            time.sleep(2)
            
            return True
            
        except TimeoutException:
            print(f"   ⚠️ No animations found")
            return False
        except Exception as e:
            print(f"   ❌ Selection failed: {e}")
            return False
    
    def configure_animation(self, in_place: bool):
        """Configure animation settings (In Place checkbox)."""
        try:
            if in_place:
                # Find In Place checkbox
                in_place_checkbox = WebDriverWait(self.driver, 5).until(
                    EC.presence_of_element_located((By.XPATH, "//label[contains(text(), 'In Place')]//input"))
                )
                if not in_place_checkbox.is_selected():
                    in_place_checkbox.click()
                    time.sleep(1)
            
            return True
        except Exception as e:
            print(f"   ⚠️ Could not configure In Place: {e}")
            return True  # Continue anyway
    
    def download_animation(self, animation: Animation) -> bool:
        """Download the currently selected animation."""
        try:
            # Click Download button
            download_btn = WebDriverWait(self.driver, 10).until(
                EC.element_to_be_clickable((By.XPATH, "//button[contains(text(), 'Download')]"))
            )
            download_btn.click()
            time.sleep(1)
            
            # Configure download options in modal
            # Select FBX format
            fbx_option = WebDriverWait(self.driver, 10).until(
                EC.element_to_be_clickable((By.XPATH, "//label[contains(text(), 'FBX')]"))
            )
            fbx_option.click()
            
            # Select "Without Skin" for additional animations
            without_skin = self.driver.find_element(By.XPATH, "//label[contains(text(), 'Without Skin')]")
            without_skin.click()
            
            # Click final Download button
            final_download_btn = self.driver.find_element(By.XPATH, "//div[contains(@class, 'modal')]//button[contains(text(), 'Download')]")
            final_download_btn.click()
            
            # Wait for download to start
            time.sleep(3)
            
            # Rename downloaded file
            self._rename_latest_download(animation)
            
            return True
            
        except Exception as e:
            print(f"   ❌ Download failed: {e}")
            return False
    
    def _rename_latest_download(self, animation: Animation):
        """Rename the most recently downloaded file."""
        try:
            # Find most recent .fbx file in download directory
            fbx_files = list(self.output_dir.glob("*.fbx"))
            if not fbx_files:
                return
            
            latest_file = max(fbx_files, key=lambda p: p.stat().st_mtime)
            new_name = self.output_dir / f"{animation.id}_{animation.name}.fbx"
            
            if latest_file != new_name:
                latest_file.rename(new_name)
                print(f"   📁 Renamed to: {new_name.name}")
                
        except Exception as e:
            print(f"   ⚠️ Rename failed: {e}")
    
    def process_animation(self, animation: Animation) -> bool:
        """Process a single animation: search, select, configure, download."""
        if animation.id in self.completed:
            print(f"  ⏭️  {animation.id} already downloaded, skipping...")
            return True
        
        print(f"\n{'='*60}")
        print(f"🎬 {animation.id}: {animation.name}")
        print(f"   Search: \"{animation.search_term}\"")
        print(f"   In Place: {animation.in_place}, Loop: {animation.loop}")
        print(f"{'='*60}")
        
        try:
            # Search for animation
            if not self.search_animation(animation.search_term):
                raise Exception("Search failed")
            
            # Select first result
            if not self.select_first_animation():
                raise Exception("No animation found")
            
            # Configure settings
            self.configure_animation(animation.in_place)
            
            # Download
            if not self.download_animation(animation):
                raise Exception("Download failed")
            
            # Record success
            self.completed.add(animation.id)
            self._save_progress()
            
            print(f"   ✅ Downloaded successfully")
            return True
            
        except Exception as e:
            print(f"   ❌ Failed: {e}")
            self.failed.append({"id": animation.id, "error": str(e)})
            return False
    
    def close(self):
        """Close browser."""
        if self.driver:
            self.driver.quit()
            print("\n🔒 Browser closed")


def parse_batch_file(filepath: str) -> list[Animation]:
    """Parse the MIXAMO_ANIMATION_BATCH.md file."""
    animations = []
    current_category = "Unknown"
    
    with open(filepath, "r") as f:
        content = f.read()
    
    lines = content.split('\n')
    
    for line in lines:
        # Track section headers
        if line.startswith("# SECTION"):
            match = re.search(r"SECTION \d+: (.+)", line)
            if match:
                current_category = match.group(1)
        
        # Parse table rows: | ANI-ID | Name | "Search Term" | Yes/No | Yes/No | Notes |
        row_match = re.match(r'\| (ANI-\w+) \| (.+?) \| "(.+?)" \| (Yes|No) \| (Yes|No) \|', line)
        if row_match:
            animations.append(Animation(
                id=row_match.group(1),
                name=row_match.group(2).strip().replace(" ", "_").lower(),
                search_term=row_match.group(3),
                in_place=row_match.group(4) == "Yes",
                loop=row_match.group(5) == "Yes",
                category=current_category
            ))
    
    return animations


def main():
    # Get credentials
    email = os.environ.get("MIXAMO_EMAIL")
    password = os.environ.get("MIXAMO_PASSWORD")
    
    if not email or not password:
        print("❌ Error: Mixamo credentials not set")
        print("\nTo use this tool:")
        print("1. Create a free Adobe account at mixamo.com")
        print("2. Set environment variables:")
        print('   export MIXAMO_EMAIL="your@email.com"')
        print('   export MIXAMO_PASSWORD="your_password"')
        print("3. Run: python download_animations.py")
        return
    
    # Paths
    script_dir = Path(__file__).parent
    batch_file = script_dir.parent.parent / "docs" / "assets" / "MIXAMO_ANIMATION_BATCH.md"
    output_dir = script_dir / "output"
    
    if not batch_file.exists():
        print(f"❌ Error: Batch file not found: {batch_file}")
        return
    
    # Parse animations
    print(f"📄 Parsing: {batch_file.name}")
    animations = parse_batch_file(str(batch_file))
    print(f"   Found {len(animations)} animations")
    
    # Show categories
    categories = {}
    for a in animations:
        categories[a.category] = categories.get(a.category, 0) + 1
    print("\n   Categories:")
    for cat, count in categories.items():
        print(f"   - {cat}: {count} animations")
    
    # Initialize downloader
    downloader = MixamoDownloader(email, password, str(output_dir), headless=False)
    
    try:
        # Setup browser
        print("\n🌐 Setting up browser...")
        downloader.setup_browser()
        
        # Login
        if not downloader.login():
            print("❌ Login failed, exiting")
            return
        
        # Select character
        if not downloader.select_character():
            print("❌ Character selection failed, exiting")
            return
        
        # Process animations
        total = len(animations)
        remaining = [a for a in animations if a.id not in downloader.completed]
        
        print(f"\n{'='*60}")
        print(f"🎬 MIXAMO ANIMATION DOWNLOADER")
        print(f"{'='*60}")
        print(f"Total animations: {total}")
        print(f"Already downloaded: {len(downloader.completed)}")
        print(f"Remaining: {len(remaining)}")
        print(f"Output: {output_dir}")
        print(f"{'='*60}")
        
        success_count = 0
        fail_count = 0
        
        for i, animation in enumerate(remaining, 1):
            print(f"\n[{i}/{len(remaining)}] Processing...")
            
            if downloader.process_animation(animation):
                success_count += 1
            else:
                fail_count += 1
            
            # Brief pause between animations
            time.sleep(2)
        
        # Final report
        print(f"\n{'='*60}")
        print(f"🏁 BATCH COMPLETE")
        print(f"{'='*60}")
        print(f"✅ Downloaded: {success_count}")
        print(f"❌ Failed: {fail_count}")
        print(f"📁 Output: {output_dir}")
        print(f"{'='*60}")
        
    finally:
        downloader.close()


if __name__ == "__main__":
    main()
