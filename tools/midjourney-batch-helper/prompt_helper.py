#!/usr/bin/env python3
"""
Midjourney Batch Prompt Helper
Parses MIDJOURNEY_UI_BATCH.md and generates ready-to-paste prompts

Usage:
  python prompt_helper.py                    # Interactive mode
  python prompt_helper.py --list             # List all prompts
  python prompt_helper.py --batch 10         # Show next 10 prompts
  python prompt_helper.py --section 1        # Show section 1 only
  python prompt_helper.py --mark RI01 RI02   # Mark prompts as done
  python prompt_helper.py --export           # Export all to clipboard-ready file
"""

import os
import re
import json
import argparse
from pathlib import Path
from dataclasses import dataclass, field
from typing import List, Optional, Dict

# ============================================================================
# Configuration
# ============================================================================

BATCH_FILE = Path(__file__).parent.parent.parent / "docs/assets/MIDJOURNEY_UI_BATCH.md"
PROGRESS_FILE = Path(__file__).parent / "progress.json"
EXPORT_FILE = Path(__file__).parent / "ready_prompts.txt"

# Global suffix for all prompts
GLOBAL_SUFFIX = "--v 6.1 --style raw --no text, words, letters, watermark"

# Aspect ratios by type (Midjourney web format)
ASPECT_RATIOS = {
    "icon": "1:1",
    "panel": "4:3",
    "banner": "3:1",
    "frame": "1:1",
    "texture": "1:1",
    "background": "16:9",
    "emote": "1:1",
}

# ============================================================================
# Data Classes
# ============================================================================

@dataclass
class MJPrompt:
    """Represents a single Midjourney prompt"""
    id: str              # e.g., "RI01", "AI15"
    name: str            # e.g., "Gold Coin"
    prompt: str          # The base prompt text
    section: int         # 1-12
    section_name: str    # e.g., "RESOURCE ICONS"
    category: str        # e.g., "Primary Resources"
    asset_type: str      # icon, panel, banner, frame, texture, background
    
    def get_full_prompt(self) -> str:
        """Generate the complete prompt with suffix and aspect ratio"""
        ar = ASPECT_RATIOS.get(self.asset_type, "1:1")
        # Web interface doesn't need /imagine prefix
        return f"{self.prompt} {GLOBAL_SUFFIX} --ar {ar}"
    
    def get_discord_prompt(self) -> str:
        """Generate prompt for Discord (with /imagine)"""
        ar = ASPECT_RATIOS.get(self.asset_type, "1:1")
        return f"/imagine {self.prompt} {GLOBAL_SUFFIX} --ar {ar}"
    
    @property
    def filename(self) -> str:
        """Generate expected filename"""
        safe_name = re.sub(r'[^\w\s-]', '', self.name).replace(' ', '_')
        return f"{self.id}_{safe_name}.png"

# ============================================================================
# Parser
# ============================================================================

def detect_asset_type(section_name: str, id_prefix: str) -> str:
    """Determine asset type from section name and ID"""
    section_lower = section_name.lower()
    
    if "icon" in section_lower:
        return "icon"
    elif "panel" in section_lower or "frame" in section_lower:
        return "panel"
    elif "banner" in section_lower:
        return "banner"
    elif "background" in section_lower:
        return "background"
    elif "texture" in section_lower:
        return "texture"
    elif "emote" in section_lower:
        return "emote"
    elif "frame" in section_lower:
        return "frame"
    else:
        return "icon"  # Default

def parse_batch_file(filepath: Path) -> List[MJPrompt]:
    """Parse MIDJOURNEY_UI_BATCH.md and extract all prompts"""
    
    content = filepath.read_text()
    prompts = []
    
    current_section = 0
    current_section_name = ""
    current_category = ""
    
    # Regex patterns
    section_pattern = r'^# SECTION (\d+): (.+)$'
    category_pattern = r'^## \d+\.\d+ (.+)$'
    id_pattern = r'^### ([A-Z]{1,3}\d{2}) - (.+)$'
    
    lines = content.split('\n')
    i = 0
    
    while i < len(lines):
        line = lines[i]
        
        # Check for section header
        section_match = re.match(section_pattern, line)
        if section_match:
            current_section = int(section_match.group(1))
            current_section_name = section_match.group(2).strip()
            i += 1
            continue
            
        # Check for category header
        category_match = re.match(category_pattern, line)
        if category_match:
            current_category = category_match.group(1).strip()
            i += 1
            continue
            
        # Check for prompt ID and name
        id_match = re.match(id_pattern, line)
        if id_match:
            prompt_id = id_match.group(1)
            prompt_name = id_match.group(2).strip()
            
            # Look for the prompt in following lines
            i += 1
            while i < len(lines) and not lines[i].startswith('```'):
                i += 1
            
            if i < len(lines) and lines[i].startswith('```'):
                i += 1  # Skip opening ```
                prompt_lines = []
                while i < len(lines) and not lines[i].startswith('```'):
                    prompt_lines.append(lines[i])
                    i += 1
                
                prompt_text = ' '.join(prompt_lines).strip()
                
                # Detect asset type
                asset_type = detect_asset_type(current_section_name, prompt_id)
                
                prompts.append(MJPrompt(
                    id=prompt_id,
                    name=prompt_name,
                    prompt=prompt_text,
                    section=current_section,
                    section_name=current_section_name,
                    category=current_category,
                    asset_type=asset_type
                ))
        
        i += 1
    
    return prompts

# ============================================================================
# Progress Tracking
# ============================================================================

def load_progress() -> Dict:
    """Load progress from file"""
    if PROGRESS_FILE.exists():
        return json.loads(PROGRESS_FILE.read_text())
    return {"completed": [], "in_progress": []}

def save_progress(progress: Dict) -> None:
    """Save progress to file"""
    PROGRESS_FILE.parent.mkdir(parents=True, exist_ok=True)
    PROGRESS_FILE.write_text(json.dumps(progress, indent=2))

def mark_complete(ids: List[str]) -> None:
    """Mark prompts as complete"""
    progress = load_progress()
    for id in ids:
        if id not in progress["completed"]:
            progress["completed"].append(id)
        if id in progress["in_progress"]:
            progress["in_progress"].remove(id)
    save_progress(progress)
    print(f"✅ Marked {len(ids)} prompts as complete")

def mark_in_progress(ids: List[str]) -> None:
    """Mark prompts as in progress"""
    progress = load_progress()
    for id in ids:
        if id not in progress["in_progress"]:
            progress["in_progress"].append(id)
    save_progress(progress)

# ============================================================================
# Display Functions
# ============================================================================

def display_batch(prompts: List[MJPrompt], batch_size: int = 10, start_from: Optional[str] = None) -> None:
    """Display a batch of ready-to-paste prompts"""
    progress = load_progress()
    completed = set(progress["completed"])
    
    # Filter out completed prompts
    remaining = [p for p in prompts if p.id not in completed]
    
    # Find start position
    start_idx = 0
    if start_from:
        for i, p in enumerate(remaining):
            if p.id == start_from:
                start_idx = i
                break
    
    batch = remaining[start_idx:start_idx + batch_size]
    
    if not batch:
        print("\n🎉 All prompts completed!")
        return
    
    print(f"\n{'='*70}")
    print(f"📋 BATCH: {batch[0].id} to {batch[-1].id} ({len(batch)} prompts)")
    print(f"   Remaining: {len(remaining)} | Completed: {len(completed)}")
    print(f"{'='*70}\n")
    
    for p in batch:
        print(f"--- {p.id}: {p.name} ---")
        print(p.get_full_prompt())
        print()
    
    # Mark as in progress
    mark_in_progress([p.id for p in batch])
    
    print(f"{'='*70}")
    print(f"💡 After generating, run: python prompt_helper.py --mark {' '.join(p.id for p in batch)}")
    print(f"{'='*70}")

def display_section(prompts: List[MJPrompt], section: int) -> None:
    """Display all prompts from a specific section"""
    section_prompts = [p for p in prompts if p.section == section]
    progress = load_progress()
    completed = set(progress["completed"])
    
    if not section_prompts:
        print(f"❌ Section {section} not found")
        return
    
    section_name = section_prompts[0].section_name
    done = sum(1 for p in section_prompts if p.id in completed)
    
    print(f"\n{'='*70}")
    print(f"📁 SECTION {section}: {section_name}")
    print(f"   Progress: {done}/{len(section_prompts)} complete")
    print(f"{'='*70}\n")
    
    for p in section_prompts:
        status = "✅" if p.id in completed else "⬜"
        print(f"{status} {p.id}: {p.name}")

def list_all(prompts: List[MJPrompt]) -> None:
    """List all prompts with status"""
    progress = load_progress()
    completed = set(progress["completed"])
    
    current_section = 0
    for p in prompts:
        if p.section != current_section:
            current_section = p.section
            section_prompts = [x for x in prompts if x.section == current_section]
            done = sum(1 for x in section_prompts if x.id in completed)
            print(f"\n--- Section {current_section}: {p.section_name} ({done}/{len(section_prompts)}) ---")
        
        status = "✅" if p.id in completed else "⬜"
        print(f"  {status} {p.id}: {p.name}")
    
    print(f"\n📊 Total: {len(completed)}/{len(prompts)} complete")

def export_all(prompts: List[MJPrompt]) -> None:
    """Export all prompts to a file for easy copying"""
    progress = load_progress()
    completed = set(progress["completed"])
    remaining = [p for p in prompts if p.id not in completed]
    
    EXPORT_FILE.parent.mkdir(parents=True, exist_ok=True)
    
    with open(EXPORT_FILE, 'w') as f:
        f.write("=" * 70 + "\n")
        f.write("MIDJOURNEY WEB - READY TO PASTE PROMPTS\n")
        f.write("=" * 70 + "\n")
        f.write(f"Total: {len(remaining)} prompts remaining\n")
        f.write("\nINSTRUCTIONS:\n")
        f.write("1. Copy ONE prompt at a time (the line after the ID)\n")
        f.write("2. Paste into Midjourney Web prompt box\n")
        f.write("3. Click Submit\n")
        f.write("4. Save the result with the ID as filename\n")
        f.write("=" * 70 + "\n\n")
        
        current_section = 0
        for p in remaining:
            if p.section != current_section:
                current_section = p.section
                f.write(f"\n{'='*70}\n")
                f.write(f"SECTION {current_section}: {p.section_name}\n")
                f.write(f"{'='*70}\n\n")
            
            f.write(f"[{p.id}] {p.name}\n")
            f.write(f"Save as: {p.filename}\n")
            f.write("-" * 50 + "\n")
            f.write(f"{p.get_full_prompt()}\n")
            f.write("-" * 50 + "\n\n")
    
    print(f"✅ Exported {len(remaining)} prompts to: {EXPORT_FILE}")

def interactive_mode(prompts: List[MJPrompt]) -> None:
    """Interactive prompt helper"""
    progress = load_progress()
    completed = set(progress["completed"])
    remaining = [p for p in prompts if p.id not in completed]
    
    print(f"""
╔══════════════════════════════════════════════════════════════════╗
║           MIDJOURNEY BATCH PROMPT HELPER                        ║
╠══════════════════════════════════════════════════════════════════╣
║  Total Prompts:     {len(prompts):>4}                                       ║
║  Completed:         {len(completed):>4}  ✅                                  ║
║  Remaining:         {len(remaining):>4}  ⬜                                  ║
╠══════════════════════════════════════════════════════════════════╣
║  Commands:                                                       ║
║    b [n]     - Show next batch (default: 10)                    ║
║    s [1-12]  - Show section                                     ║
║    m ID...   - Mark IDs as complete                             ║
║    e         - Export all remaining to file                     ║
║    l         - List all prompts                                 ║
║    q         - Quit                                             ║
╚══════════════════════════════════════════════════════════════════╝
    """)
    
    while True:
        try:
            cmd = input("\n> ").strip().lower().split()
            if not cmd:
                continue
            
            if cmd[0] == 'q':
                break
            elif cmd[0] == 'b':
                size = int(cmd[1]) if len(cmd) > 1 else 10
                display_batch(prompts, size)
            elif cmd[0] == 's':
                section = int(cmd[1]) if len(cmd) > 1 else 1
                display_section(prompts, section)
            elif cmd[0] == 'm':
                if len(cmd) > 1:
                    mark_complete([x.upper() for x in cmd[1:]])
                else:
                    print("Usage: m ID1 ID2 ID3...")
            elif cmd[0] == 'e':
                export_all(prompts)
            elif cmd[0] == 'l':
                list_all(prompts)
            else:
                print("Unknown command. Try: b, s, m, e, l, q")
                
        except KeyboardInterrupt:
            break
        except Exception as e:
            print(f"Error: {e}")

# ============================================================================
# Main
# ============================================================================

def main():
    parser = argparse.ArgumentParser(description="Midjourney Batch Prompt Helper")
    parser.add_argument("--list", action="store_true", help="List all prompts")
    parser.add_argument("--batch", type=int, metavar="N", help="Show next N prompts")
    parser.add_argument("--section", type=int, choices=range(1,13), help="Show specific section")
    parser.add_argument("--mark", nargs="+", metavar="ID", help="Mark IDs as complete")
    parser.add_argument("--export", action="store_true", help="Export all to file")
    parser.add_argument("--start-from", type=str, metavar="ID", help="Start batch from ID")
    
    args = parser.parse_args()
    
    # Parse the batch file
    print(f"📄 Parsing {BATCH_FILE}...")
    prompts = parse_batch_file(BATCH_FILE)
    print(f"   Found {len(prompts)} prompts\n")
    
    if args.list:
        list_all(prompts)
    elif args.batch:
        display_batch(prompts, args.batch, args.start_from)
    elif args.section:
        display_section(prompts, args.section)
    elif args.mark:
        mark_complete([x.upper() for x in args.mark])
    elif args.export:
        export_all(prompts)
    else:
        interactive_mode(prompts)

if __name__ == "__main__":
    main()
