#!/usr/bin/env python3
"""
Mixamo Animation Downloader - LOCAL VERSION
Run this on your local machine (not in Codespaces) with a visible browser.

Requirements:
    pip install playwright
    playwright install chromium

Usage:
    python mixamo_local.py
"""

import asyncio
import time
from pathlib import Path
from playwright.async_api import async_playwright

# Configuration
EMAIL = "rob@sagecg.com"
PASSWORD = "ModestT1!"
OUTPUT_DIR = Path("./downloads")

# Animation list - first 10 for testing
ANIMATIONS = [
    ("ANI-LOC01", "Walking"),
    ("ANI-LOC02", "Walking Backwards"),
    ("ANI-LOC03", "Left Strafe Walk"),
    ("ANI-LOC04", "Right Strafe Walk"),
    ("ANI-LOC05", "Injured Walk"),
    ("ANI-LOC06", "Sneaking"),
    ("ANI-LOC07", "Confident Walk"),
    ("ANI-LOC08", "Running"),
    ("ANI-LOC09", "Running Backward"),
    ("ANI-LOC10", "Left Strafe"),
]

async def main():
    OUTPUT_DIR.mkdir(exist_ok=True)
    
    async with async_playwright() as p:
        # Launch VISIBLE browser (not headless)
        browser = await p.chromium.launch(
            headless=False,  # You can SEE the browser!
            slow_mo=500,     # Slow down so you can watch
        )
        
        page = await browser.new_page()
        await page.set_viewport_size({"width": 1920, "height": 1080})
        
        # Step 1: Go to Mixamo
        print("Opening Mixamo...")
        await page.goto("https://www.mixamo.com/")
        await asyncio.sleep(2)
        
        # Step 2: Click Sign In
        print("Clicking Sign In...")
        await page.click("text=Sign In")
        await asyncio.sleep(3)
        
        # Step 3: Handle Adobe login (may redirect to signup page)
        print("Logging in...")
        
        # Check if on signup page
        if "signup" in page.url or "Create" in await page.title():
            print("  On signup page, clicking 'Sign in' link...")
            await page.click("text=Sign in")
            await asyncio.sleep(2)
        
        # Enter email
        await page.fill("input[name='username']", EMAIL)
        await page.click("button:has-text('Continue')")
        await asyncio.sleep(2)
        
        # Enter password
        await page.fill("input[name='password']", PASSWORD)
        await page.click("button:has-text('Continue')")
        
        # Wait for redirect to Mixamo
        print("Waiting for Mixamo to load...")
        await asyncio.sleep(10)
        
        # Navigate to Mixamo if stuck on auth page
        if "mixamo.com" not in page.url:
            await page.goto("https://www.mixamo.com/")
            await asyncio.sleep(5)
        
        print("✅ Logged in!")
        
        # Step 4: Select character (click first character in grid)
        print("\nSelecting character...")
        await page.click("text=Characters")
        await asyncio.sleep(3)
        
        # Click first character thumbnail
        chars = await page.query_selector_all("img")
        for char in chars:
            box = await char.bounding_box()
            if box and box['y'] > 150 and box['width'] > 80:
                await char.click()
                print("  ✅ Character selected")
                break
        
        await asyncio.sleep(3)
        
        # Step 5: Process each animation
        print(f"\n{'='*50}")
        print(f"Starting downloads ({len(ANIMATIONS)} animations)")
        print(f"{'='*50}\n")
        
        for i, (anim_id, search_term) in enumerate(ANIMATIONS, 1):
            print(f"[{i}/{len(ANIMATIONS)}] {anim_id}: {search_term}")
            
            # Click Animations tab
            await page.click("text=Animations")
            await asyncio.sleep(2)
            
            # Search
            search_box = await page.query_selector("input[type='search'], input[placeholder*='Search']")
            if search_box:
                await search_box.fill("")
                await search_box.fill(search_term)
                await page.keyboard.press("Enter")
                await asyncio.sleep(3)
            
            # Click first animation result
            imgs = await page.query_selector_all("img")
            clicked = False
            for img in imgs:
                box = await img.bounding_box()
                if box and box['x'] < 600 and box['y'] > 150 and box['width'] > 80:
                    await img.click()
                    clicked = True
                    print(f"  ✓ Animation selected")
                    break
            
            if not clicked:
                print(f"  ❌ Could not find animation")
                continue
            
            # Wait for animation to load on character
            await asyncio.sleep(4)
            
            # Click Download button
            print(f"  Clicking Download button...")
            download_btn = await page.query_selector("button:has-text('Download')")
            if download_btn:
                await download_btn.click()
                await asyncio.sleep(2)
                
                # NOW YOU SHOULD SEE THE DIALOG
                # Click the DOWNLOAD button in the dialog
                print(f"  Looking for dialog DOWNLOAD button...")
                
                # Try to find and click dialog download
                dialog_download = await page.query_selector("button:has-text('DOWNLOAD')")
                if dialog_download:
                    box = await dialog_download.bounding_box()
                    if box and box['y'] > 300:  # Dialog button is lower
                        async with page.expect_download() as download_info:
                            await dialog_download.click()
                        
                        download = await download_info.value
                        filename = f"{anim_id}_{search_term.replace(' ', '_').lower()}.fbx"
                        await download.save_as(OUTPUT_DIR / filename)
                        print(f"  ✅ Downloaded: {filename}")
                    else:
                        print(f"  ⚠️ Found button but wrong position")
                else:
                    print(f"  ❌ Dialog download button not found")
                    input("  >>> MANUAL INTERVENTION: Click the DOWNLOAD button, then press Enter...")
            else:
                print(f"  ❌ Download button not found")
            
            await asyncio.sleep(2)
        
        print(f"\n{'='*50}")
        print("Done! Check ./downloads/ folder")
        print(f"{'='*50}")
        
        input("\nPress Enter to close browser...")
        await browser.close()

if __name__ == "__main__":
    asyncio.run(main())
