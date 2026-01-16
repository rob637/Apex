"""
Mixamo Hybrid Downloader
========================
1. Opens browser to Mixamo
2. YOU log in and select a character manually
3. Press Enter when ready
4. Automation downloads all 160 animations

Run: python mixamo_hybrid.py
"""

from playwright.sync_api import sync_playwright
import time
import os

# All 160 animations from MIXAMO_ANIMATION_BATCH.md
ANIMATIONS = [
    # LOCOMOTION (28)
    "Walking", "Walking Backwards", "Left Strafe Walk", "Right Strafe Walk",
    "Injured Walk", "Sneaking", "Confident Walk", "Running", "Running Backward",
    "Left Strafe", "Right Strafe", "Fast Run", "Injured Run", "Combat Run",
    "Left Turn 90", "Right Turn 90", "Turn", "Jump", "Falling Idle", "Landing",
    "Running Jump", "Falling", "Hard Landing", "Dodge Left", "Dodge Right",
    "Dodge Back", "Combat Roll",
    
    # IDLE (16)
    "Idle", "Alert Idle", "Tired Idle", "Happy Idle", "Sad Idle", "Breathing Idle",
    "Sword Idle", "Two Hand Sword Idle", "Bow Idle", "Shield Idle", "Spear Idle",
    "Looking Around", "Scratching Head", "Stretching", "Yawning", "Check Watch",
    
    # COMBAT (48)
    "Sword Slash", "Sword And Shield Slash", "Great Sword Slash", "Sword Block",
    "Sword Parry", "Draw Sword", "Sheathe Sword", "Two Hand Sword Attack",
    "Great Sword Attack", "Great Sword Overhead", "Two Hand Block", "Axe Slash",
    "Axe Attack", "Spear Thrust", "Spear Swing", "Spear Block", "Throw",
    "Shield Block", "Shield Block High", "Shield Bash", "Sword And Shield Attack",
    "Draw Arrow", "Aim Idle", "Bow Fire", "Running Bow Fire", "Crossbow Reload",
    "Crossbow Fire", "Casting Spell", "Magic Attack", "Channeling", "Blessing",
    "Magic AOE", "Summoning", "Hit Reaction", "Hit From Back", "Hit Reaction Left",
    "Hit Reaction Right", "Heavy Hit", "Knockback", "Knocked Down", "Getting Up",
    "Death Forward", "Death Backward", "Death Left", "Death Right", "Dramatic Death",
    "Rising From Ground",
    
    # INTERACTION (24)
    "Picking Up", "Reaching Up", "Put Down", "Pushing", "Pulling", "Opening Chest",
    "Open Door", "Lever Pull", "Waving", "Bowing", "Salute", "Clapping",
    "Victory Cheer", "Head Shake", "Head Nod", "Pointing", "Hammering", "Shoveling",
    "Chopping Wood", "Mining", "Carrying Heavy", "Walking With Object", "Sweeping",
    "Reading",
    
    # EMOTES (20)
    "Happy", "Excited", "Laughing", "Dancing", "Flexing", "Thumbs Up", "Victory",
    "Thinking", "Shrug", "Check Time", "Cross Arms", "Angry", "Sad", "Face Palm",
    "Crying", "Yelling", "Sitting Down", "Sitting Idle", "Standing Up",
    
    # NPC (24)
    "Standing Guard", "Alert", "Walk With Weapon", "Stop Gesture", "Standing Idle",
    "Welcoming", "Presenting", "Counting Money", "Tired Standing", "Construction",
    "Carrying Boxes", "Wiping Sweat", "Casual Walk", "Talking", "Cheering",
    "Menacing Idle", "Taunting", "Charge", "Roaring", "Standing Proud", "Ordering",
    "Point Forward", "Rallying Troops"
]

def main():
    # Create downloads folder
    os.makedirs("downloads", exist_ok=True)
    
    print("=" * 60)
    print("MIXAMO HYBRID DOWNLOADER")
    print("=" * 60)
    
    with sync_playwright() as p:
        # Launch visible browser
        browser = p.chromium.launch(
            headless=False,
            slow_mo=300  # Slow down for visibility
        )
        
        context = browser.new_context(
            accept_downloads=True,
            viewport={"width": 1920, "height": 1080}
        )
        
        page = context.new_page()
        
        # Step 1: Open Mixamo
        print("\n[1] Opening Mixamo...")
        page.goto("https://www.mixamo.com/")
        page.wait_for_load_state("networkidle")
        
        # Step 2: Wait for user
        print("\n" + "=" * 60)
        print("YOUR TURN!")
        print("=" * 60)
        print("""
1. Log in with your Adobe account
2. Go to the Characters tab
3. Select a character (like Y Bot)
4. Make sure you're on the Animations tab
5. Press ENTER here when ready...
""")
        input(">>> Press ENTER when you're on the Animations page: ")
        
        # Verify we're on the right page
        print("\n[2] Checking page state...")
        page.wait_for_timeout(2000)
        
        # Step 3: Start automation
        print("\n" + "=" * 60)
        print("AUTOMATION STARTING!")
        print(f"Will download {len(ANIMATIONS)} animations")
        print("=" * 60)
        
        downloaded = 0
        failed = []
        
        for i, animation_name in enumerate(ANIMATIONS):
            print(f"\n[{i+1}/{len(ANIMATIONS)}] Searching: {animation_name}")
            
            try:
                # Find and clear search box
                search_box = page.locator('input[type="search"], input[placeholder*="Search"]').first
                if not search_box.is_visible(timeout=5000):
                    print("  ⚠ Search box not visible, trying to find it...")
                    page.keyboard.press("Escape")  # Close any dialogs
                    page.wait_for_timeout(1000)
                    search_box = page.locator('input[type="search"], input[placeholder*="Search"]').first
                
                # Clear and type search
                search_box.click()
                search_box.fill("")
                page.wait_for_timeout(300)
                search_box.fill(animation_name)
                page.keyboard.press("Enter")
                page.wait_for_timeout(2000)
                
                # Click first result
                print(f"  → Selecting first result...")
                thumbnail = page.locator('.animation-thumbnail, [class*="thumbnail"], [class*="card"]').first
                if thumbnail.is_visible(timeout=5000):
                    thumbnail.click()
                    page.wait_for_timeout(2000)  # Wait for animation to load on character
                else:
                    # Try clicking any result image
                    page.locator('img[src*="thumbnail"]').first.click()
                    page.wait_for_timeout(2000)
                
                # Click Download button
                print(f"  → Clicking Download...")
                download_btn = page.locator('button:has-text("Download"), [class*="download"]').first
                download_btn.click()
                page.wait_for_timeout(1500)
                
                # Handle download dialog
                print(f"  → Handling download dialog...")
                
                # Look for the dialog download button
                dialog_download = page.locator('.modal button:has-text("Download"), .dialog button:has-text("Download"), button.primary:has-text("Download")').first
                
                if dialog_download.is_visible(timeout=3000):
                    # Start download
                    with page.expect_download(timeout=60000) as download_info:
                        dialog_download.click()
                    
                    download = download_info.value
                    filename = f"{i+1:03d}_{animation_name.replace(' ', '_')}.fbx"
                    download.save_as(f"downloads/{filename}")
                    print(f"  ✓ Downloaded: {filename}")
                    downloaded += 1
                else:
                    # Maybe dialog didn't open - try pressing Enter
                    print("  ⚠ Dialog not found, trying Enter key...")
                    page.keyboard.press("Enter")
                    page.wait_for_timeout(3000)
                    
                    # Check if a download started
                    if page.locator('.modal, .dialog').count() == 0:
                        print(f"  ✗ Could not download {animation_name}")
                        failed.append(animation_name)
                
                # Close any open dialogs
                page.keyboard.press("Escape")
                page.wait_for_timeout(500)
                
            except Exception as e:
                print(f"  ✗ Error: {str(e)[:50]}")
                failed.append(animation_name)
                page.keyboard.press("Escape")
                page.wait_for_timeout(1000)
        
        # Summary
        print("\n" + "=" * 60)
        print("DOWNLOAD COMPLETE!")
        print("=" * 60)
        print(f"Downloaded: {downloaded}/{len(ANIMATIONS)}")
        print(f"Failed: {len(failed)}")
        
        if failed:
            print("\nFailed animations:")
            for name in failed:
                print(f"  - {name}")
            
            # Save failed list
            with open("downloads/failed.txt", "w") as f:
                f.write("\n".join(failed))
            print("\nFailed list saved to downloads/failed.txt")
        
        print("\nPress Enter to close browser...")
        input()
        browser.close()

if __name__ == "__main__":
    main()
