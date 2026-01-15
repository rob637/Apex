#!/usr/bin/env python3
"""
Meshy.ai Browser Automation Script
Uses Playwright to automate the web interface when API isn't available

This is an ALTERNATIVE to the API-based approach if:
- You don't have API access
- You want to use free tier credits via the web interface

Usage:
  pip install playwright
  playwright install chromium
  python browser_automation.py
"""

import asyncio
import json
import re
import time
from pathlib import Path
from dataclasses import dataclass
from typing import List, Optional

try:
    from playwright.async_api import async_playwright, Page, Browser
except ImportError:
    print("❌ Playwright not installed. Run: pip install playwright && playwright install chromium")
    exit(1)

# ============================================================================
# Configuration
# ============================================================================

BATCH_FILE = Path(__file__).parent.parent.parent / "docs/assets/MESHY_3D_BATCH.md"
OUTPUT_DIR = Path(__file__).parent / "output"
PROGRESS_FILE = OUTPUT_DIR / "browser_progress.json"

# How long to wait for model generation (in seconds)
GENERATION_TIMEOUT = 300  # 5 minutes max

# Delay between submissions to avoid rate limiting
SUBMISSION_DELAY = 5  # seconds

# ============================================================================
# Reuse the prompt parser from the main script
# ============================================================================

GLOBAL_STYLE = ", low-poly stylized medieval fantasy, game-ready asset, clean topology, soft shadows, PBR textures, white background"

@dataclass
class ModelPrompt:
    id: str
    name: str
    prompt: str
    section: int
    category: str
    
    @property
    def filename(self) -> str:
        safe_name = re.sub(r'[^\w\s-]', '', self.name).replace(' ', '_')
        return f"{self.id}_{safe_name}"

def parse_batch_file(filepath: Path) -> List[ModelPrompt]:
    """Parse MESHY_3D_BATCH.md and extract all prompts"""
    content = filepath.read_text()
    prompts = []
    current_section = 0
    current_category = ""
    
    section_pattern = r'^# SECTION (\d+): (.+)$'
    category_pattern = r'^## \d+\.\d+ (.+)$'
    id_pattern = r'^### ([A-Z]\d{2}) - (.+)$'
    
    lines = content.split('\n')
    i = 0
    
    while i < len(lines):
        line = lines[i]
        
        section_match = re.match(section_pattern, line)
        if section_match:
            current_section = int(section_match.group(1))
            i += 1
            continue
            
        category_match = re.match(category_pattern, line)
        if category_match:
            current_category = category_match.group(1)
            i += 1
            continue
            
        id_match = re.match(id_pattern, line)
        if id_match:
            model_id = id_match.group(1)
            model_name = id_match.group(2)
            
            i += 1
            while i < len(lines) and not lines[i].startswith('```'):
                i += 1
            
            if i < len(lines) and lines[i].startswith('```'):
                i += 1
                prompt_lines = []
                while i < len(lines) and not lines[i].startswith('```'):
                    prompt_lines.append(lines[i])
                    i += 1
                
                prompt_text = '\n'.join(prompt_lines).strip()
                if "low-poly stylized" not in prompt_text.lower():
                    prompt_text += GLOBAL_STYLE
                
                prompts.append(ModelPrompt(
                    id=model_id,
                    name=model_name,
                    prompt=prompt_text,
                    section=current_section,
                    category=current_category
                ))
        i += 1
    
    return prompts

# ============================================================================
# Browser Automation
# ============================================================================

class MeshyBrowserAutomation:
    """Automates Meshy.ai web interface using Playwright"""
    
    def __init__(self):
        self.browser: Optional[Browser] = None
        self.page: Optional[Page] = None
        self.completed: List[str] = []
        self.failed: List[str] = []
        
    async def start(self):
        """Launch browser and navigate to Meshy"""
        playwright = await async_playwright().start()
        
        # Use headed mode so you can see what's happening and handle login
        self.browser = await playwright.chromium.launch(
            headless=False,  # Show browser window
            slow_mo=100  # Slow down actions for visibility
        )
        
        self.page = await self.browser.new_page()
        
        # Set download path
        OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
        
    async def login_prompt(self):
        """Navigate to Meshy and wait for manual login"""
        await self.page.goto("https://www.meshy.ai/")
        
        print("\n" + "="*60)
        print("🔐 Please log in to Meshy.ai in the browser window")
        print("   After logging in, press Enter here to continue...")
        print("="*60)
        
        input()  # Wait for user to confirm login
        
        # Navigate to text-to-3d
        await self.page.goto("https://www.meshy.ai/create/text-to-3d")
        await self.page.wait_for_load_state("networkidle")
        
    async def submit_prompt(self, model: ModelPrompt) -> bool:
        """Submit a single prompt via the web interface"""
        try:
            print(f"  🎨 {model.id} - Submitting: {model.name}")
            
            # Find and clear the prompt textarea
            textarea = await self.page.wait_for_selector('textarea[placeholder*="Describe"]', timeout=10000)
            await textarea.fill("")
            await textarea.fill(model.prompt)
            
            # Click Generate button
            generate_btn = await self.page.wait_for_selector('button:has-text("Generate")', timeout=5000)
            await generate_btn.click()
            
            print(f"  ⏳ {model.id} - Waiting for generation...")
            
            # Wait for generation to complete (look for download button or completion indicator)
            # This selector may need adjustment based on Meshy's actual UI
            await self.page.wait_for_selector('[data-testid="download-button"], .download-btn, button:has-text("Download")', 
                                              timeout=GENERATION_TIMEOUT * 1000)
            
            print(f"  📥 {model.id} - Downloading...")
            
            # Set up download handler
            async with self.page.expect_download() as download_info:
                # Click download button for GLB format
                download_btn = await self.page.query_selector('button:has-text("GLB"), [data-format="glb"]')
                if download_btn:
                    await download_btn.click()
                else:
                    # Try generic download button
                    await self.page.click('button:has-text("Download")')
            
            download = await download_info.value
            
            # Save to our output directory
            output_path = OUTPUT_DIR / f"{model.filename}.glb"
            await download.save_as(output_path)
            
            print(f"  ✅ {model.id} - Saved to {output_path.name}")
            return True
            
        except Exception as e:
            print(f"  ❌ {model.id} - Failed: {e}")
            return False
    
    async def process_all(self, models: List[ModelPrompt], start_from: Optional[str] = None):
        """Process all models"""
        
        # Load previous progress
        if PROGRESS_FILE.exists():
            progress = json.loads(PROGRESS_FILE.read_text())
            self.completed = progress.get("completed", [])
            self.failed = progress.get("failed", [])
        
        # Filter models
        models_to_process = []
        started = start_from is None
        
        for model in models:
            if not started:
                if model.id == start_from:
                    started = True
                else:
                    continue
            if model.id not in self.completed:
                models_to_process.append(model)
        
        total = len(models_to_process)
        print(f"\n🚀 Processing {total} models\n")
        
        for i, model in enumerate(models_to_process, 1):
            print(f"\n[{i}/{total}] Processing {model.id}...")
            
            success = await self.submit_prompt(model)
            
            if success:
                self.completed.append(model.id)
            else:
                self.failed.append(model.id)
            
            # Save progress
            self.save_progress()
            
            # Delay between submissions
            await asyncio.sleep(SUBMISSION_DELAY)
            
            # Navigate back to create page for next model
            await self.page.goto("https://www.meshy.ai/create/text-to-3d")
            await self.page.wait_for_load_state("networkidle")
        
        # Summary
        print(f"\n" + "="*60)
        print(f"✅ Completed: {len(self.completed)}")
        print(f"❌ Failed: {len(self.failed)}")
        if self.failed:
            print(f"   Failed IDs: {', '.join(self.failed[-20:])}")  # Show last 20
        print(f"="*60)
    
    def save_progress(self):
        """Save progress to file"""
        PROGRESS_FILE.parent.mkdir(parents=True, exist_ok=True)
        PROGRESS_FILE.write_text(json.dumps({
            "completed": self.completed,
            "failed": self.failed,
            "timestamp": time.strftime("%Y-%m-%d %H:%M:%S")
        }, indent=2))
    
    async def close(self):
        """Close browser"""
        if self.browser:
            await self.browser.close()

# ============================================================================
# Main
# ============================================================================

async def main():
    import argparse
    
    parser = argparse.ArgumentParser(description="Browser automation for Meshy.ai")
    parser.add_argument("--start-from", type=str, help="Start from a specific model ID")
    parser.add_argument("--only-section", type=int, choices=[1,2,3,4,5,6], help="Only process one section")
    args = parser.parse_args()
    
    # Parse prompts
    print(f"📄 Parsing {BATCH_FILE}...")
    models = parse_batch_file(BATCH_FILE)
    print(f"   Found {len(models)} model prompts")
    
    if args.only_section:
        models = [m for m in models if m.section == args.only_section]
        print(f"   Filtered to section {args.only_section}: {len(models)} models")
    
    # Start automation
    automation = MeshyBrowserAutomation()
    
    try:
        await automation.start()
        await automation.login_prompt()
        await automation.process_all(models, start_from=args.start_from)
    finally:
        await automation.close()

if __name__ == "__main__":
    asyncio.run(main())
