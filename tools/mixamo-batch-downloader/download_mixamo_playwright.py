#!/usr/bin/env python3
"""
Mixamo Animation Batch Downloader - Playwright Version

Uses Playwright for browser automation which works better in containerized environments.

Usage:
    export MIXAMO_EMAIL="your@email.com"
    export MIXAMO_PASSWORD="your_password"
    python download_mixamo_playwright.py
"""

import os
import sys
import re
import json
import time
import asyncio
from pathlib import Path
from datetime import datetime
from dataclasses import dataclass
from playwright.async_api import async_playwright, Page, Browser, TimeoutError as PlaywrightTimeout

# Force unbuffered output for real-time logging
sys.stdout.reconfigure(line_buffering=True)


@dataclass
class Animation:
    """Represents an animation to download."""
    id: str
    name: str
    search_term: str
    in_place: bool
    loop: bool
    category: str


class MixamoPlaywrightDownloader:
    """Automated Mixamo animation downloader using Playwright."""
    
    MIXAMO_URL = "https://www.mixamo.com/"
    
    def __init__(self, email: str, password: str, output_dir: str):
        self.email = email
        self.password = password
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.progress_file = self.output_dir / "progress.json"
        self.completed = self._load_progress()
        self.failed = []
        self.browser: Browser = None
        self.page: Page = None
        self.character_selected = False
    
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
    
    async def setup_browser(self):
        """Initialize Playwright browser."""
        print("🌐 Setting up browser...")
        self._playwright = await async_playwright().start()
        
        # Launch with more realistic browser args to avoid bot detection
        self.browser = await self._playwright.chromium.launch(
            headless=True,
            args=[
                '--no-sandbox',
                '--disable-setuid-sandbox',
                '--disable-dev-shm-usage',
                '--disable-blink-features=AutomationControlled',
                '--disable-infobars',
                '--window-size=1920,1080',
            ]
        )
        
        # Create context with realistic fingerprint
        context = await self.browser.new_context(
            accept_downloads=True,
            viewport={'width': 1920, 'height': 1080},
            user_agent='Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
            locale='en-US',
            timezone_id='America/New_York',
        )
        
        # Remove webdriver property to avoid detection
        await context.add_init_script("""
            Object.defineProperty(navigator, 'webdriver', {
                get: () => undefined
            });
        """)
        
        self.page = await context.new_page()
        print("   ✅ Browser initialized")
    
    async def login(self) -> bool:
        """Login to Mixamo via Adobe account."""
        print("\n🔐 Logging into Mixamo...")
        
        try:
            # Start at Mixamo homepage
            await self.page.goto("https://www.mixamo.com/")
            await self.page.wait_for_load_state("networkidle", timeout=15000)
            await asyncio.sleep(3)
            
            await self.page.screenshot(path=str(self.output_dir / "01_mixamo_home.png"))
            print(f"   Current URL: {self.page.url[:80]}...")
            
            # Check if already logged in (look for user avatar or account menu)
            page_content = await self.page.content()
            if "Sign Out" in page_content or "signout" in page_content.lower():
                print("   ✅ Already logged in!")
                return True
            
            # Click Sign In/Log In button on Mixamo
            sign_in_clicked = False
            for selector in [
                "button:has-text('Sign')",
                "button:has-text('Log')",
                "a:has-text('Sign In')",
                "a:has-text('Log In')",
            ]:
                try:
                    btn = await self.page.wait_for_selector(selector, timeout=3000, state="visible")
                    if btn:
                        await btn.click()
                        print(f"   Clicked sign-in button")
                        sign_in_clicked = True
                        break
                except:
                    continue
            
            # Wait for Adobe auth page
            await asyncio.sleep(5)
            await self.page.screenshot(path=str(self.output_dir / "02_after_signin_click.png"))
            
            current_url = self.page.url
            print(f"   After sign-in click URL: {current_url[:80]}...")
            title = await self.page.title()
            print(f"   Page title: {title}")
            
            # Check if we're on signup page - if so, need to find "Sign in" link
            # Based on Adobe's UI, there should be a "Sign in" link for existing accounts
            if "signup" in current_url.lower() or "create" in title.lower():
                print("   Detected signup page, looking for existing account sign-in...")
                
                # Look for "Already have an Adobe Account? Sign in" or similar
                signin_link_selectors = [
                    "a:has-text('Sign in')",
                    "text=Sign in",  
                    "[href*='login']",
                    "//a[contains(text(), 'Sign in')]",
                    "//span[contains(text(), 'Sign in')]",
                ]
                
                found_signin = False
                for selector in signin_link_selectors:
                    try:
                        link = await self.page.wait_for_selector(selector, timeout=3000)
                        if link:
                            # Make sure it's not the current page's header
                            text = await link.inner_text()
                            if text and "sign in" in text.lower():
                                await link.click()
                                print(f"   Clicked 'Sign in' link")
                                found_signin = True
                                await asyncio.sleep(4)
                                break
                    except:
                        continue
                
                await self.page.screenshot(path=str(self.output_dir / "02b_after_signin_link.png"))
            
            # Now we should be on the email entry page ("Sign in" page from your screenshot)
            print("   Looking for email field...")
            
            # Wait for email field
            email_field = None
            for attempt in range(5):
                for selector in [
                    "input[name='username']",
                    "#EmailPage-EmailField", 
                    "input[type='email']",
                    "input[placeholder*='Email']",
                ]:
                    try:
                        email_field = await self.page.wait_for_selector(selector, timeout=3000, state="visible")
                        if email_field:
                            print(f"   Found email field with: {selector}")
                            break
                    except:
                        continue
                if email_field:
                    break
                print(f"   Waiting for email field... attempt {attempt + 1}")
                await self.page.screenshot(path=str(self.output_dir / f"email_wait_{attempt}.png"))
                await asyncio.sleep(2)
            
            if not email_field:
                await self.page.screenshot(path=str(self.output_dir / "email_not_found.png"))
                print("   ❌ Could not find email field")
                return False
            
            # Enter email
            await email_field.click()
            await email_field.fill("")
            await asyncio.sleep(0.5)
            # Type slowly to appear more human-like
            await email_field.type(self.email, delay=50)
            print(f"   Entered email: {self.email}")
            await asyncio.sleep(1)
            
            await self.page.screenshot(path=str(self.output_dir / "03_email_entered.png"))
            
            # Click Continue button - use JavaScript click for better reliability
            print("   Clicking Continue...")
            
            continue_clicked = False
            for selector in [
                "button:has-text('Continue')",
                "button[data-id='EmailPage-ContinueButton']",
                "button[type='submit']",
            ]:
                try:
                    continue_btn = await self.page.wait_for_selector(selector, timeout=3000, state="visible")
                    if continue_btn and await continue_btn.is_enabled():
                        print(f"   Found Continue button: {selector}")
                        # Use JavaScript click to bypass any click handlers
                        await continue_btn.evaluate("btn => btn.click()")
                        continue_clicked = True
                        break
                except:
                    continue
            
            if not continue_clicked:
                print("   Fallback: pressing Enter")
                await email_field.press("Enter")
            
            # Wait for password page ("Enter your password" from your screenshot)
            print("   Waiting for password page...")
            await asyncio.sleep(4)
            
            await self.page.screenshot(path=str(self.output_dir / "04_after_email_continue.png"))
            
            # Enter password
            print("   Looking for password field...")
            
            password_field = None
            for attempt in range(5):
                for selector in [
                    "input[type='password']",
                    "input[name='password']",
                    "#PasswordPage-PasswordField",
                ]:
                    try:
                        password_field = await self.page.wait_for_selector(selector, timeout=3000, state="visible")
                        if password_field:
                            print(f"   Found password field with: {selector}")
                            break
                    except:
                        continue
                if password_field:
                    break
                print(f"   Waiting for password field... attempt {attempt + 1}")
                # Check if we're on wrong page
                page_content = await self.page.content()
                if "First name" in page_content or "Create your" in page_content:
                    print("   ⚠️ On signup page - looking for sign in link again")
                    try:
                        signin = await self.page.query_selector("a:has-text('Sign in')")
                        if signin:
                            await signin.click()
                            await asyncio.sleep(3)
                    except:
                        pass
                await self.page.screenshot(path=str(self.output_dir / f"password_wait_{attempt}.png"))
                await asyncio.sleep(2)
            
            if not password_field:
                await self.page.screenshot(path=str(self.output_dir / "password_not_found.png"))
                print("   ❌ Could not find password field")
                return False
            
            # Enter password
            await password_field.click()
            # Type password slowly to appear human-like
            await password_field.type(self.password, delay=50)
            print("   Entered password")
            await asyncio.sleep(1)
            
            await self.page.screenshot(path=str(self.output_dir / "05_password_entered.png"))
            
            # Click Continue to login - use JavaScript click
            print("   Clicking Continue to login...")
            
            continue_clicked = False
            for selector in [
                "button:has-text('Continue')",
                "button[type='submit']",
            ]:
                try:
                    continue_btn = await self.page.wait_for_selector(selector, timeout=3000, state="visible")
                    if continue_btn:
                        # Use JavaScript click for reliability
                        await continue_btn.evaluate("btn => btn.click()")
                        continue_clicked = True
                        print("   Clicked Continue")
                        break
                except:
                    continue
            
            if not continue_clicked:
                await password_field.press("Enter")
                print("   Pressed Enter")
            
            # Wait for redirect to Mixamo - give it more time and check for errors
            print("   Waiting for Mixamo redirect...")
            
            # Wait up to 30 seconds for redirect
            for i in range(15):
                await asyncio.sleep(2)
                current_url = self.page.url
                title = await self.page.title()
                
                # Check for error messages
                page_content = await self.page.content()
                if "Something went wrong" in page_content or "error" in title.lower():
                    print(f"   ⚠️ Error detected on page. URL: {current_url[:60]}")
                    await self.page.screenshot(path=str(self.output_dir / f"error_{i}.png"))
                    continue
                
                # Check if redirected to Mixamo
                if "mixamo.com" in current_url and "auth" not in current_url:
                    print(f"   ✅ Redirected to Mixamo! ({i*2}s)")
                    break
                    
                print(f"   ...waiting ({i*2}s), URL: {current_url[:50]}")
            
            await self.page.screenshot(path=str(self.output_dir / "06_after_login.png"))
            
            # Check if we're on Mixamo
            current_url = self.page.url
            print(f"   Final URL: {current_url}")
            
            if "mixamo.com" in current_url and "auth" not in current_url:
                print("   ✅ Logged in successfully!")
                return True
            else:
                print(f"   ⚠️ Not on Mixamo yet. URL: {current_url}")
                # Navigate to Mixamo manually
                await self.page.goto("https://www.mixamo.com/")
                await asyncio.sleep(5)
                await self.page.screenshot(path=str(self.output_dir / "06b_manual_navigate.png"))
                if "mixamo.com" in self.page.url:
                    # Check if we're actually logged in now
                    content = await self.page.content()
                    if "Sign In" not in content or "Log In" not in content:
                        print("   ✅ Navigated to Mixamo and logged in")
                        return True
                    print("   ⚠️ On Mixamo but may not be logged in")
                return True  # Continue anyway and see what happens
            
        except Exception as e:
            await self.page.screenshot(path=str(self.output_dir / "error_debug.png"))
            print(f"   ❌ Login failed: {e}")
            import traceback
            traceback.print_exc()
            return False
    
    async def select_character(self):
        """Select Y Bot character for animations - using Mixamo's default character."""
        if self.character_selected:
            return True
            
        print("\n👤 Selecting character (Y Bot)...")
        try:
            # Wait for Mixamo to fully load
            await asyncio.sleep(3)
            await self.page.screenshot(path=str(self.output_dir / "07_before_character.png"))
            
            # First, check if we need to dismiss any modals or accept terms
            try:
                accept_btn = await self.page.query_selector("button:has-text('Accept'), button:has-text('OK'), button:has-text('Got it')")
                if accept_btn:
                    await accept_btn.click()
                    print("   Dismissed modal")
                    await asyncio.sleep(2)
            except:
                pass
            
            # Check for "USE THIS CHARACTER" dialog that appears when switching characters
            try:
                use_char_dialog = await self.page.query_selector("button:has-text('USE THIS CHARACTER')")
                if use_char_dialog:
                    await use_char_dialog.click()
                    print("   Clicked 'USE THIS CHARACTER' in dialog")
                    await asyncio.sleep(2)
                    self.character_selected = True
                    return True
            except:
                pass
            
            # Debug: Print what's on the page
            page_content = await self.page.content()
            print(f"   Page content length: {len(page_content)} chars")
            
            # Look for key UI elements to understand page state
            print("   Looking for key elements...")
            
            # Check if there's a "Find Character" or "Use Character" button
            find_char = await self.page.query_selector("text=Find Character")
            if find_char:
                print("   Found 'Find Character' button - clicking...")
                await find_char.click()
                await asyncio.sleep(3)
                await self.page.screenshot(path=str(self.output_dir / "07b_find_character.png"))
            
            # Look for "Use this character" button which appears when Y Bot is ready
            use_char_btn = await self.page.query_selector("button:has-text('Use this character')")
            if use_char_btn:
                await use_char_btn.click()
                print("   ✅ Clicked 'Use this character'")
                await asyncio.sleep(2)
                self.character_selected = True
                return True
            
            # Check for canvas element - indicates 3D viewport with character
            canvas = await self.page.query_selector("canvas")
            if canvas:
                box = await canvas.bounding_box()
                if box and box['width'] > 100 and box['height'] > 100:
                    print(f"   ✅ Found large canvas ({box['width']}x{box['height']}) - character likely loaded")
                    self.character_selected = True
                    return True
            
            # If no character, we need to click on "Characters" to select one
            # But first, check if there's a character selection popup/panel visible
            char_selectors = [
                ".product-thumbnail",
                ".character-thumbnail", 
                "[class*='thumbnail']",
                "[class*='product-item']",
                "img[src*='character']",
                "img[src*='avatar']",
                "img[src*='ybot']",
            ]
            
            for selector in char_selectors:
                try:
                    chars = await self.page.query_selector_all(selector)
                    if chars and len(chars) > 0:
                        await chars[0].click()
                        print(f"   ✅ Selected character using: {selector}")
                        self.character_selected = True
                        await asyncio.sleep(3)
                        await self.page.screenshot(path=str(self.output_dir / "08_character_selected.png"))
                        return True
                except:
                    continue
            
            # As last resort, click Characters tab then select first character
            print("   Trying Characters tab...")
            char_tab = await self.page.query_selector("text=Characters")
            if char_tab:
                await char_tab.click()
                await asyncio.sleep(5)  # Give more time to load
                await self.page.screenshot(path=str(self.output_dir / "08_characters_tab.png"))
                
                # Debug: dump some HTML structure
                try:
                    # Find all clickable items in the main content area
                    all_imgs = await self.page.query_selector_all("img")
                    print(f"   Found {len(all_imgs)} images on page")
                    
                    # Try clicking on first image that looks like a character
                    for img in all_imgs[:10]:  # Check first 10 images
                        src = await img.get_attribute("src") or ""
                        alt = await img.get_attribute("alt") or ""
                        if any(x in src.lower() for x in ["character", "thumbnail", "avatar", "bot"]) or \
                           any(x in alt.lower() for x in ["character", "ybot", "x bot", "y bot"]):
                            print(f"   Found character image: {src[:50]}...")
                            # Use JavaScript click to bypass overlay
                            await img.evaluate("el => el.click()")
                            await asyncio.sleep(3)
                            
                            # Check if character loaded
                            canvas = await self.page.query_selector("canvas")
                            if canvas:
                                self.character_selected = True
                                await self.page.screenshot(path=str(self.output_dir / "09_character_selected.png"))
                                print("   ✅ Character clicked and loaded!")
                                return True
                    
                    # Alternative: Click on the parent card/container of the image
                    print("   Trying to find clickable card containers...")
                    cards = await self.page.query_selector_all("[class*='product'], [class*='character'], [class*='item']")
                    for card in cards[:5]:
                        try:
                            await card.evaluate("el => el.click()")
                            await asyncio.sleep(3)
                            canvas = await self.page.query_selector("canvas")
                            if canvas:
                                box = await canvas.bounding_box()
                                if box and box['width'] > 100:
                                    self.character_selected = True
                                    await self.page.screenshot(path=str(self.output_dir / "09_character_selected.png"))
                                    print("   ✅ Character selected via card!")
                                    return True
                        except:
                            continue
                    
                except Exception as e:
                    print(f"   Debug selector error: {e}")
            
            # If we can't click a character, check if canvas is showing a loaded character
            canvas = await self.page.query_selector("canvas")
            if canvas:
                box = await canvas.bounding_box()
                if box and box['width'] > 200 and box['height'] > 200:
                    print("   ✅ Large canvas found - character may already be loaded")
                    self.character_selected = True
                    return True
            
            await self.page.screenshot(path=str(self.output_dir / "character_not_found.png"))
            print("   ⚠️ Could not select a character. Downloads may fail.")
            return False
            
        except Exception as e:
            await self.page.screenshot(path=str(self.output_dir / "character_error.png"))
            print(f"   ❌ Character selection failed: {e}")
            return False
    
    async def search_and_download(self, anim: Animation) -> bool:
        """Search for and download an animation."""
        if anim.id in self.completed:
            print(f"  ⏭️  {anim.id} already done")
            return True
        
        print(f"\n{'='*60}")
        print(f"🎬 {anim.id}: {anim.name}")
        print(f"   Search: \"{anim.search_term}\"")
        
        try:
            # Click Animations tab and wait for it to load
            print("   Clicking Animations tab...")
            anim_tab = await self.page.query_selector("text=Animations")
            if anim_tab:
                await anim_tab.click()
                await asyncio.sleep(3)
                
                # Verify we're on animations tab by checking URL
                current_url = self.page.url
                if "Motion" not in current_url:
                    # Try clicking again or navigate directly
                    await self.page.goto("https://www.mixamo.com/#/?page=1&type=Motion%2CMotionPack")
                    await asyncio.sleep(3)
                print(f"   URL after tab click: {self.page.url[:60]}...")
            
            # Take screenshot to see current state
            await self.page.screenshot(path=str(self.output_dir / f"10_search_{anim.id}.png"))
            
            # Find search box - it's an input with placeholder "Search"
            search_box = await self.page.query_selector("input[placeholder='Search']")
            if not search_box:
                search_box = await self.page.query_selector("input[type='text']")
            
            if not search_box:
                print("   ❌ Could not find search box")
                self.failed.append({"id": anim.id, "error": "Search box not found"})
                return False
            
            print(f"   Found search box")
            
            # Use JavaScript to interact with search box - more reliable
            try:
                # Focus and clear the search box using JavaScript
                await search_box.evaluate("el => { el.focus(); el.value = ''; }")
                await asyncio.sleep(0.3)
                
                # Type the search term using Playwright's keyboard
                await self.page.keyboard.type(anim.search_term, delay=50)
                print(f"   Typed: {anim.search_term}")
                await asyncio.sleep(0.3)
                
                # Press Enter using keyboard
                await self.page.keyboard.press("Enter")
                print(f"   Pressed Enter")
            except Exception as e:
                print(f"   Search input error: {e}")
                # Fallback: Try direct fill and press
                await search_box.fill(anim.search_term)
                await search_box.press("Enter")
            
            # Wait for URL to contain query parameter (search results loaded)
            for i in range(10):
                await asyncio.sleep(1)
                url = self.page.url
                if "query=" in url:
                    print(f"   URL updated with query: {url[:80]}...")
                    break
            
            await asyncio.sleep(2)  # Extra time for animations to load
            
            # Take screenshot of results
            await self.page.screenshot(path=str(self.output_dir / f"11_results_{anim.id}.png"))
            
            # Find animation thumbnails - they're in the left panel grid
            result = None
            
            try:
                # Get all images that could be animation thumbnails
                all_imgs = await self.page.query_selector_all("img")
                print(f"   Found {len(all_imgs)} images")
                
                for img in all_imgs:
                    try:
                        box = await img.bounding_box()
                        src = await img.get_attribute("src") or ""
                        # Animation thumbnails are in the left content area (x < 650) and below header (y > 150)
                        if box and box['x'] < 650 and box['y'] > 170 and box['width'] > 80 and box['height'] > 80:
                            if "logo" not in src.lower() and "adobe" not in src.lower():
                                result = img
                                print(f"   Found animation at ({int(box['x'])},{int(box['y'])}), size={int(box['width'])}x{int(box['height'])}")
                                break
                    except:
                        continue
            except Exception as e:
                print(f"   Error finding results: {e}")
            
            if not result:
                print("   ❌ No results found")
                self.failed.append({"id": anim.id, "error": "No search results"})
                return False
            
            # Get the bounding box to click in the center of the thumbnail
            box = await result.bounding_box()
            if box:
                # Click in the center of the thumbnail - this triggers the animation selection
                center_x = box['x'] + box['width']/2
                center_y = box['y'] + box['height']/2
                
                # First click to select
                await self.page.mouse.click(center_x, center_y)
                await asyncio.sleep(1)
                
                # Second click may be needed for some animations
                await self.page.mouse.click(center_x, center_y)
                print("   ✓ Clicked animation thumbnail")
            else:
                await result.click()
                print("   ✓ Selected animation (fallback)")
            
            # Wait for animation to load on character - this is crucial!
            # The animation needs to download and apply to the 3D character
            print("   Waiting for animation to load on character...")
            await asyncio.sleep(5)
            
            # Check if there's a loading spinner - wait for it to disappear
            for i in range(20):
                # Look for any loading indicators
                spinner = await self.page.query_selector(".loading, .spinner, [class*='loading'], [class*='spinner']")
                progress = await self.page.query_selector("[class*='progress']")
                if not spinner and not progress:
                    break
                if i == 0:
                    print("   ...waiting for loading to complete")
                await asyncio.sleep(0.5)
            
            # Additional wait for WebGL/3D rendering and animation playback
            await asyncio.sleep(2)
            
            await self.page.screenshot(path=str(self.output_dir / f"12_after_anim_click_{anim.id}.png"))
            
            # Set in-place option if needed (checkbox appears in right panel)
            if anim.in_place:
                try:
                    inplace_checkbox = await self.page.query_selector("input[type='checkbox']")
                    if inplace_checkbox:
                        is_checked = await inplace_checkbox.is_checked()
                        if not is_checked:
                            await inplace_checkbox.click()
                            print("   ✓ Set In Place")
                except Exception as e:
                    print(f"   In-place checkbox note: {e}")
            
            # Debug: List all buttons on the page with their text and position
            print("   Scanning all buttons on page...")
            all_buttons = await self.page.query_selector_all("button")
            for btn in all_buttons:
                try:
                    text = (await btn.text_content() or "").strip()[:30]
                    box = await btn.bounding_box()
                    if box and text:
                        is_disabled = await btn.get_attribute("disabled")
                        btn_class = (await btn.get_attribute("class") or "")[:40]
                        print(f"      Button: '{text}' at ({int(box['x'])},{int(box['y'])}) class={btn_class} disabled={is_disabled}")
                except:
                    pass
            
            # Look for the DOWNLOAD button - it should be in the right panel header area
            print("   Looking for Download button...")
            download_btn = None
            
            # Try multiple selectors for the download button
            download_selectors = [
                "button:has-text('DOWNLOAD')",
                "button:has-text('Download')", 
                ".download-button",
                "[class*='download']",
                "a:has-text('Download')",
            ]
            
            for selector in download_selectors:
                try:
                    btn = await self.page.wait_for_selector(selector, timeout=5000)
                    if btn:
                        box = await btn.bounding_box()
                        # Download button should be on the right side (x > 600) and near top (y < 200)
                        if box and box['x'] > 500:
                            # Check if button is disabled
                            is_disabled = await btn.get_attribute("disabled")
                            btn_class = await btn.get_attribute("class") or ""
                            if is_disabled or "disabled" in btn_class.lower():
                                print(f"   ⚠️ Download button appears disabled (class: {btn_class[:50]})")
                            else:
                                download_btn = btn
                                print(f"   Found download at ({int(box['x'])},{int(box['y'])}), enabled")
                                break
                except:
                    continue
            
            if not download_btn:
                # Try getting all buttons and find one with Download text
                all_buttons = await self.page.query_selector_all("button")
                for btn in all_buttons:
                    try:
                        text = await btn.text_content() or ""
                        if "download" in text.lower():
                            box = await btn.bounding_box()
                            if box and box['x'] > 500:
                                download_btn = btn
                                print(f"   Found download button via text: '{text.strip()}'")
                                break
                    except:
                        continue
            
            if not download_btn:
                await self.page.screenshot(path=str(self.output_dir / f"13_no_download_btn_{anim.id}.png"))
                print("   ❌ Download button not found")
                self.failed.append({"id": anim.id, "error": "Download button not found"})
                return False
            
            # Click download button - try multiple methods to ensure it works
            box = await download_btn.bounding_box()
            if box:
                print(f"   Download button at ({int(box['x'])},{int(box['y'])})")
                
                # Method 1: Direct mouse click with proper event sequence
                center_x = box['x'] + box['width']/2
                center_y = box['y'] + box['height']/2
                
                # Move mouse to position first (like a real user)
                await self.page.mouse.move(center_x, center_y)
                await asyncio.sleep(0.2)
                
                # Click with full event sequence
                await self.page.mouse.down()
                await asyncio.sleep(0.1)
                await self.page.mouse.up()
                
            else:
                # Fallback: try force click on element
                await download_btn.click(force=True)
            
            print("   ✓ Clicked Download")
            
            # Wait briefly for dialog to appear
            await asyncio.sleep(1)
            
            # Also try JavaScript click as backup - React apps sometimes need this
            try:
                await download_btn.evaluate("el => { el.click(); }")
            except:
                pass
            
            # Wait for download dialog to appear - it's a modal overlay
            await asyncio.sleep(2)
            
            # Check if dialog opened - look for the dialog container or CANCEL button
            dialog_opened = False
            for attempt in range(3):
                # Look for dialog indicators - Mixamo uses a modal with specific elements
                cancel_btn = await self.page.query_selector("button:has-text('CANCEL')")
                modal = await self.page.query_selector(".modal, .dialog, [role='dialog'], .overlay, [class*='modal'], [class*='dialog']")
                format_dropdown = await self.page.query_selector("text=FBX Binary")
                skin_option = await self.page.query_selector("text=With Skin")
                
                # Also check for any new overlay/popup
                page_content = await self.page.content()
                has_download_settings = "DOWNLOAD SETTINGS" in page_content or "Download Settings" in page_content
                
                if cancel_btn or format_dropdown or skin_option or has_download_settings:
                    dialog_opened = True
                    print(f"   ✓ Dialog opened (detected via: {'cancel' if cancel_btn else 'format' if format_dropdown else 'skin' if skin_option else 'content'})")
                    break
                
                print(f"   ⚠️ Dialog not detected (attempt {attempt+1}/3)")
                await self.page.screenshot(path=str(self.output_dir / f"dialog_check_{anim.id}_{attempt}.png"))
                
                # Try clicking again - maybe the button wasn't ready
                if box:
                    await self.page.mouse.click(box['x'] + box['width']/2, box['y'] + box['height']/2)
                await asyncio.sleep(2)
            
            if not dialog_opened:
                print("   ❌ Could not open download dialog after 3 attempts")
                await self.page.screenshot(path=str(self.output_dir / f"no_dialog_{anim.id}.png"))
                self.failed.append({"id": anim.id, "error": "Dialog did not open"})
                return False
            
            # Take screenshot of dialog
            await self.page.screenshot(path=str(self.output_dir / f"dialog_{anim.id}.png"))
            
            # Dialog should now be open with DOWNLOAD button at bottom right
            download_path = self.output_dir / f"{anim.id}_{anim.name.replace(' ', '_').lower()}.fbx"
            
            try:
                # Debug: List all visible buttons and their positions
                buttons = await self.page.query_selector_all("button")
                print(f"   Debug: Found {len(buttons)} buttons")
                for btn in buttons:
                    try:
                        text = (await btn.text_content() or "").strip()
                        visible = await btn.is_visible()
                        if visible and text:
                            box = await btn.bounding_box()
                            if box:
                                print(f"   Button '{text}' at ({int(box['x'])},{int(box['y'])})")
                    except:
                        pass
                
                # Find the DOWNLOAD button in the dialog (should be at y > 300)
                dialog_btn = None
                for btn in buttons:
                    try:
                        text = (await btn.text_content() or "").strip().upper()
                        if text == "DOWNLOAD":
                            box = await btn.bounding_box()
                            if box and box['y'] > 300:
                                dialog_btn = btn
                                print(f"   Found dialog DOWNLOAD at ({int(box['x'])},{int(box['y'])})")
                                break
                    except:
                        continue
                
                async with self.page.expect_download(timeout=45000) as download_info:
                    if dialog_btn:
                        await dialog_btn.click()
                        print("   ✓ Clicked dialog DOWNLOAD")
                    else:
                        # Try clicking at the expected position of the dialog DOWNLOAD button
                        # Based on user's screenshot, it should be in lower right of dialog
                        # Dialog is centered, DOWNLOAD button is at bottom-right
                        print("   ⚠️ No dialog button found at y>300, trying coordinate click")
                        # Try clicking where dialog DOWNLOAD button should be
                        # The viewport is 1920x1080, dialog is centered, button is bottom-right
                        await self.page.mouse.click(858, 365)
                        print("   Clicked at (858,365)")
                
                download = await download_info.value
                await download.save_as(str(download_path))
                size_kb = download_path.stat().st_size // 1024
                print(f"   ✅ Saved: {download_path.name} ({size_kb}KB)")
                self.completed.add(anim.id)
                self._save_progress()
                return True
                    
            except Exception as e:
                print(f"   ❌ Download error: {e}")
                self.failed.append({"id": anim.id, "error": str(e)})
                return False
            
        except PlaywrightTimeout:
            print(f"   ❌ Timeout during download")
            self.failed.append({"id": anim.id, "error": "Timeout"})
            return False
        except Exception as e:
            print(f"   ❌ Error: {e}")
            self.failed.append({"id": anim.id, "error": str(e)})
            return False
    
    async def process_all(self, animations: list):
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
            await self.search_and_download(anim)
            await asyncio.sleep(2)  # Rate limiting
        
        self._save_progress()
        
        print(f"\n{'='*60}")
        print(f"✅ Complete: {len(self.completed)}/{len(animations)}")
        print(f"❌ Failed: {len(self.failed)}")
        print(f"{'='*60}")
    
    async def close(self):
        """Close browser."""
        if self.browser:
            await self.browser.close()


def parse_batch_file(filepath: str) -> list:
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


async def main():
    # Get credentials
    email = os.environ.get("MIXAMO_EMAIL")
    password = os.environ.get("MIXAMO_PASSWORD")
    
    if not email or not password:
        print("❌ Error: MIXAMO_EMAIL and MIXAMO_PASSWORD environment variables required")
        print("\nUsage:")
        print('  export MIXAMO_EMAIL="your@email.com"')
        print('  export MIXAMO_PASSWORD="your_password"')
        print("  python download_mixamo_playwright.py")
        return
    
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
    downloader = MixamoPlaywrightDownloader(email, password, str(output_dir))
    
    try:
        await downloader.setup_browser()
        
        if not await downloader.login():
            print("\n❌ Login failed. Check screenshots in output folder for debugging.")
            return
        
        # Need to select a character first - Mixamo requires a character to be loaded
        # before you can search for animations. We'll use Y Bot (Mixamo's default)
        if not await downloader.select_character():
            print("\n❌ Character selection failed.")
            return
        
        await downloader.process_all(animations)
        
    finally:
        await downloader.close()


if __name__ == "__main__":
    asyncio.run(main())
