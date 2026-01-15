#!/usr/bin/env python3
"""
Mixamo Animation Batch Helper

Creates organized lists of animations for efficient manual downloading from Mixamo.
Since Mixamo has no API, this helper organizes the work for faster manual processing.

Usage:
    python batch_helper.py
"""

import re
from pathlib import Path
from dataclasses import dataclass


@dataclass
class Animation:
    """Represents an animation to download."""
    id: str
    name: str
    search_term: str
    in_place: bool
    loop: bool
    category: str


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
                name=row_match.group(2).strip(),
                search_term=row_match.group(3),
                in_place=row_match.group(4) == "Yes",
                loop=row_match.group(5) == "Yes",
                category=current_category
            ))
    
    return animations


def export_search_terms(animations: list[Animation], output_file: str):
    """Export search terms grouped by category for quick copy-paste."""
    categories = {}
    for a in animations:
        if a.category not in categories:
            categories[a.category] = []
        categories[a.category].append(a)
    
    with open(output_file, "w") as f:
        f.write("=" * 70 + "\n")
        f.write("MIXAMO ANIMATION SEARCH TERMS - Copy & Paste Guide\n")
        f.write("=" * 70 + "\n\n")
        f.write("Instructions:\n")
        f.write("1. Go to mixamo.com and log in\n")
        f.write("2. Upload/select your character model\n")
        f.write("3. Go to Animations tab\n")
        f.write("4. For each animation below:\n")
        f.write("   a. Copy the search term\n")
        f.write("   b. Paste in Mixamo search\n")
        f.write("   c. Select best match\n")
        f.write("   d. Check 'In Place' if marked [IN PLACE]\n")
        f.write("   e. Download as FBX (Without Skin)\n")
        f.write("   f. Rename file to the ID shown\n")
        f.write("\n" + "=" * 70 + "\n\n")
        
        total = 0
        for category, anims in categories.items():
            f.write(f"\n{'='*70}\n")
            f.write(f"## {category} ({len(anims)} animations)\n")
            f.write(f"{'='*70}\n\n")
            
            for a in anims:
                in_place_tag = " [IN PLACE]" if a.in_place else ""
                loop_tag = " [LOOP]" if a.loop else ""
                f.write(f"{a.id}: {a.name}\n")
                f.write(f"   Search: {a.search_term}{in_place_tag}{loop_tag}\n\n")
                total += 1
        
        f.write(f"\n{'='*70}\n")
        f.write(f"TOTAL: {total} animations\n")
        f.write(f"{'='*70}\n")
    
    print(f"   ✅ Exported to: {output_file}")


def export_checklist(animations: list[Animation], output_file: str):
    """Export markdown checklist for tracking progress."""
    categories = {}
    for a in animations:
        if a.category not in categories:
            categories[a.category] = []
        categories[a.category].append(a)
    
    with open(output_file, "w") as f:
        f.write("# Mixamo Animation Download Checklist\n\n")
        f.write("Check off each animation as you download it.\n\n")
        f.write("---\n\n")
        
        for category, anims in categories.items():
            f.write(f"## {category}\n\n")
            
            for a in anims:
                tags = []
                if a.in_place:
                    tags.append("In Place")
                if a.loop:
                    tags.append("Loop")
                tag_str = f" *({', '.join(tags)})*" if tags else ""
                f.write(f"- [ ] **{a.id}**: {a.name} → `{a.search_term}`{tag_str}\n")
            
            f.write("\n")
    
    print(f"   ✅ Checklist: {output_file}")


def export_naming_guide(animations: list[Animation], output_file: str):
    """Export file naming guide."""
    with open(output_file, "w") as f:
        f.write("# Mixamo File Naming Guide\n\n")
        f.write("After downloading, rename files using these names:\n\n")
        f.write("| Mixamo Download | Rename To |\n")
        f.write("|----------------|----------|\n")
        
        for a in animations:
            clean_name = a.name.replace(" ", "_").lower()
            f.write(f"| {a.search_term}.fbx | {a.id}_{clean_name}.fbx |\n")
    
    print(f"   ✅ Naming guide: {output_file}")


def export_quick_list(animations: list[Animation], output_file: str):
    """Export minimal list - just search terms, one per line."""
    with open(output_file, "w") as f:
        for a in animations:
            f.write(f"{a.search_term}\n")
    
    print(f"   ✅ Quick list: {output_file}")


def main():
    script_dir = Path(__file__).parent
    batch_file = script_dir.parent.parent / "docs" / "assets" / "MIXAMO_ANIMATION_BATCH.md"
    output_dir = script_dir / "output"
    output_dir.mkdir(exist_ok=True)
    
    if not batch_file.exists():
        print(f"❌ Error: Batch file not found: {batch_file}")
        return
    
    print(f"📄 Parsing: {batch_file.name}")
    animations = parse_batch_file(str(batch_file))
    print(f"   Found {len(animations)} animations")
    
    # Show categories
    categories = {}
    for a in animations:
        categories[a.category] = categories.get(a.category, 0) + 1
    print("\n   Categories:")
    for cat, count in categories.items():
        print(f"   - {cat}: {count}")
    
    # Export files
    print("\n📝 Exporting helper files...")
    export_search_terms(animations, str(output_dir / "search_terms.txt"))
    export_checklist(animations, str(output_dir / "checklist.md"))
    export_naming_guide(animations, str(output_dir / "naming_guide.md"))
    export_quick_list(animations, str(output_dir / "quick_list.txt"))
    
    print(f"\n{'='*60}")
    print("🎬 MIXAMO BATCH HELPER - FILES READY")
    print(f"{'='*60}")
    print(f"\nOutput directory: {output_dir}")
    print("\nFiles created:")
    print("  📋 search_terms.txt  - Full guide with instructions")
    print("  ☑️  checklist.md     - Markdown checklist to track progress")
    print("  📛 naming_guide.md  - How to rename downloaded files")
    print("  📝 quick_list.txt   - Just search terms, one per line")
    print(f"\n{'='*60}")
    print("\n⏱️  ESTIMATED TIME: ~2-3 hours for 160 animations")
    print("   (About 1 minute per animation with practice)\n")
    print("💡 TIP: Open quick_list.txt and work through it systematically!")
    print(f"{'='*60}")


if __name__ == "__main__":
    main()
