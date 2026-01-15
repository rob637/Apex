#!/usr/bin/env python3
"""
Meshy.ai Batch 3D Model Generator
Automates generation of all 216+ game assets from MESHY_3D_BATCH.md

Usage:
  1. Get your Meshy API key from https://www.meshy.ai/settings/api
  2. Set environment variable: export MESHY_API_KEY="your-key-here"
  3. Run: python generate_models.py

Options:
  --dry-run       Parse prompts without calling API
  --start-from    Start from a specific model ID (e.g., W15)
  --only-section  Only process one section (1-6)
  --parallel      Number of concurrent requests (default: 3)
"""

import os
import re
import json
import time
import argparse
import requests
from pathlib import Path
from dataclasses import dataclass
from typing import List, Optional
from concurrent.futures import ThreadPoolExecutor, as_completed

# ============================================================================
# Configuration
# ============================================================================

MESHY_API_BASE = "https://api.meshy.ai/v2"
OUTPUT_DIR = Path(__file__).parent / "output"
BATCH_FILE = Path(__file__).parent.parent.parent / "docs/assets/MESHY_3D_BATCH.md"

# Global style modifier from the batch file
GLOBAL_STYLE = ", low-poly stylized medieval fantasy, game-ready asset, clean topology, soft shadows, PBR textures, white background"

# Rate limiting
REQUESTS_PER_MINUTE = 10
REQUEST_DELAY = 60 / REQUESTS_PER_MINUTE

# ============================================================================
# Data Classes
# ============================================================================

@dataclass
class ModelPrompt:
    """Represents a single 3D model to generate"""
    id: str           # e.g., "F01", "W15", "T09"
    name: str         # e.g., "Stone Foundation 1x1"
    prompt: str       # The full prompt text
    section: int      # 1-6
    category: str     # e.g., "Stone Foundations", "Guard Towers"
    
    @property
    def filename(self) -> str:
        """Generate filename like F01_Stone_Foundation_1x1.glb"""
        safe_name = re.sub(r'[^\w\s-]', '', self.name).replace(' ', '_')
        return f"{self.id}_{safe_name}"

@dataclass  
class GenerationTask:
    """Tracks a generation request"""
    model: ModelPrompt
    task_id: Optional[str] = None
    status: str = "pending"  # pending, processing, completed, failed
    result_url: Optional[str] = None
    error: Optional[str] = None

# ============================================================================
# Prompt Parser
# ============================================================================

def parse_batch_file(filepath: Path) -> List[ModelPrompt]:
    """Parse MESHY_3D_BATCH.md and extract all prompts"""
    
    content = filepath.read_text()
    prompts = []
    
    current_section = 0
    current_category = ""
    
    # Regex patterns
    section_pattern = r'^# SECTION (\d+): (.+)$'
    category_pattern = r'^## \d+\.\d+ (.+)$'
    id_pattern = r'^### ([A-Z]\d{2}) - (.+)$'
    prompt_pattern = r'^```\n(.+?)\n```'
    
    lines = content.split('\n')
    i = 0
    
    while i < len(lines):
        line = lines[i]
        
        # Check for section header
        section_match = re.match(section_pattern, line)
        if section_match:
            current_section = int(section_match.group(1))
            i += 1
            continue
            
        # Check for category header
        category_match = re.match(category_pattern, line)
        if category_match:
            current_category = category_match.group(1)
            i += 1
            continue
            
        # Check for model ID and name
        id_match = re.match(id_pattern, line)
        if id_match:
            model_id = id_match.group(1)
            model_name = id_match.group(2)
            
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
                
                prompt_text = '\n'.join(prompt_lines).strip()
                
                # Add global style if not already present
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
# Meshy API Client
# ============================================================================

class MeshyClient:
    """Client for Meshy.ai API"""
    
    def __init__(self, api_key: str):
        self.api_key = api_key
        self.headers = {
            "Authorization": f"Bearer {api_key}",
            "Content-Type": "application/json"
        }
    
    def create_text_to_3d(self, prompt: str, art_style: str = "realistic") -> str:
        """Submit a text-to-3d generation request, returns task_id"""
        
        url = f"{MESHY_API_BASE}/text-to-3d"
        
        payload = {
            "mode": "preview",  # Use "refine" for higher quality (costs more)
            "prompt": prompt,
            "art_style": art_style,  # Options: realistic, cartoon, low-poly, sculpture
            "negative_prompt": "blurry, low quality, distorted, ugly"
        }
        
        response = requests.post(url, headers=self.headers, json=payload)
        
        if response.status_code != 202 and response.status_code != 200:
            print(f"      API Error: {response.status_code} - {response.text}")
            response.raise_for_status()
        
        data = response.json()
        return data["result"]  # Returns task_id
    
    def get_task_status(self, task_id: str) -> dict:
        """Check status of a generation task"""
        
        url = f"{MESHY_API_BASE}/text-to-3d/{task_id}"
        response = requests.get(url, headers=self.headers)
        response.raise_for_status()
        
        return response.json()
    
    def wait_for_completion(self, task_id: str, timeout: int = 900) -> dict:
        """Poll until task completes or times out"""
        
        start_time = time.time()
        last_progress = -1
        
        while time.time() - start_time < timeout:
            status = self.get_task_status(task_id)
            progress = status.get("progress", 0)
            
            # Only print if progress changed
            if progress != last_progress:
                elapsed = int(time.time() - start_time)
                print(f"      Progress: {progress}% ({elapsed}s elapsed)")
                last_progress = progress
            
            if status["status"] == "SUCCEEDED":
                return status
            elif status["status"] == "FAILED":
                raise Exception(f"Task failed: {status.get('task_error', {}).get('message', 'Unknown error')}")
            elif status["status"] in ["PENDING", "IN_PROGRESS"]:
                time.sleep(5)  # Poll every 5 seconds for faster feedback
            else:
                raise Exception(f"Unknown status: {status['status']}")
        
        raise TimeoutError(f"Task {task_id} timed out after {timeout} seconds")
    
    def download_model(self, model_url: str, output_path: Path) -> None:
        """Download the generated GLB file"""
        
        response = requests.get(model_url, stream=True)
        response.raise_for_status()
        
        output_path.parent.mkdir(parents=True, exist_ok=True)
        
        with open(output_path, 'wb') as f:
            for chunk in response.iter_content(chunk_size=8192):
                f.write(chunk)

# ============================================================================
# Batch Processor
# ============================================================================

class BatchProcessor:
    """Processes all models in batch"""
    
    def __init__(self, client: MeshyClient, output_dir: Path, parallel: int = 3):
        self.client = client
        self.output_dir = output_dir
        self.parallel = parallel
        self.tasks: List[GenerationTask] = []
        self.progress_file = output_dir / "progress.json"
    
    def load_progress(self) -> dict:
        """Load progress from previous run"""
        if self.progress_file.exists():
            return json.loads(self.progress_file.read_text())
        return {"completed": [], "failed": []}
    
    def save_progress(self, completed: List[str], failed: List[str]) -> None:
        """Save progress for resume capability"""
        self.progress_file.parent.mkdir(parents=True, exist_ok=True)
        self.progress_file.write_text(json.dumps({
            "completed": completed,
            "failed": failed,
            "timestamp": time.strftime("%Y-%m-%d %H:%M:%S")
        }, indent=2))
    
    def process_model(self, model: ModelPrompt) -> GenerationTask:
        """Process a single model generation"""
        
        task = GenerationTask(model=model)
        output_path = self.output_dir / f"{model.filename}.glb"
        
        # Skip if already exists
        if output_path.exists():
            task.status = "completed"
            task.result_url = str(output_path)
            print(f"  ⏭️  {model.id} - Already exists, skipping")
            return task
        
        try:
            print(f"  🎨 {model.id} - Submitting: {model.name}")
            
            # Submit generation request
            task.task_id = self.client.create_text_to_3d(model.prompt)
            task.status = "processing"
            
            print(f"  ⏳ {model.id} - Task ID: {task.task_id}, waiting...")
            
            # Wait for completion
            result = self.client.wait_for_completion(task.task_id)
            
            # Download the model
            model_url = result.get("model_urls", {}).get("glb")
            if model_url:
                print(f"  📥 {model.id} - Downloading...")
                self.client.download_model(model_url, output_path)
                task.status = "completed"
                task.result_url = str(output_path)
                print(f"  ✅ {model.id} - Saved to {output_path.name}")
            else:
                task.status = "failed"
                task.error = "No GLB URL in response"
                
        except Exception as e:
            task.status = "failed"
            task.error = str(e)
            print(f"  ❌ {model.id} - Failed: {e}")
        
        # Rate limiting
        time.sleep(REQUEST_DELAY)
        
        return task
    
    def process_all(self, models: List[ModelPrompt], start_from: Optional[str] = None) -> None:
        """Process all models with progress tracking"""
        
        progress = self.load_progress()
        completed = set(progress["completed"])
        failed = list(progress["failed"])
        
        # Filter models
        models_to_process = []
        started = start_from is None
        
        for model in models:
            if not started:
                if model.id == start_from:
                    started = True
                else:
                    continue
            
            if model.id not in completed:
                models_to_process.append(model)
        
        total = len(models_to_process)
        print(f"\n🚀 Processing {total} models (skipping {len(completed)} already completed)\n")
        
        for i, model in enumerate(models_to_process, 1):
            print(f"\n[{i}/{total}] Processing {model.id}...")
            
            task = self.process_model(model)
            
            if task.status == "completed":
                completed.add(model.id)
            else:
                failed.append(model.id)
            
            # Save progress after each model
            self.save_progress(list(completed), failed)
        
        # Summary
        print(f"\n" + "="*60)
        print(f"✅ Completed: {len(completed)}")
        print(f"❌ Failed: {len(failed)}")
        if failed:
            print(f"   Failed IDs: {', '.join(failed)}")
        print(f"="*60)

# ============================================================================
# Main
# ============================================================================

def main():
    parser = argparse.ArgumentParser(description="Batch generate 3D models from Meshy.ai")
    parser.add_argument("--dry-run", action="store_true", help="Parse prompts without calling API")
    parser.add_argument("--start-from", type=str, help="Start from a specific model ID (e.g., W15)")
    parser.add_argument("--only-section", type=int, choices=[1,2,3,4,5,6], help="Only process one section")
    parser.add_argument("--parallel", type=int, default=1, help="Number of concurrent requests")
    parser.add_argument("--list", action="store_true", help="List all model IDs and exit")
    
    args = parser.parse_args()
    
    # Parse the batch file
    print(f"📄 Parsing {BATCH_FILE}...")
    models = parse_batch_file(BATCH_FILE)
    print(f"   Found {len(models)} model prompts\n")
    
    # Filter by section if requested
    if args.only_section:
        models = [m for m in models if m.section == args.only_section]
        print(f"   Filtered to section {args.only_section}: {len(models)} models\n")
    
    # List mode
    if args.list:
        print("\n📋 Model List:")
        current_section = 0
        for m in models:
            if m.section != current_section:
                current_section = m.section
                print(f"\n--- Section {current_section} ---")
            print(f"  {m.id}: {m.name}")
        return
    
    # Dry run mode
    if args.dry_run:
        print("\n🔍 Dry Run - Parsed Prompts:")
        for m in models:
            print(f"\n{m.id} - {m.name}")
            print(f"   Section: {m.section} | Category: {m.category}")
            print(f"   Prompt: {m.prompt[:100]}...")
        return
    
    # Check for API key
    api_key = os.environ.get("MESHY_API_KEY")
    if not api_key:
        print("❌ Error: MESHY_API_KEY environment variable not set")
        print("\n   Get your API key from: https://www.meshy.ai/settings/api")
        print("   Then run: export MESHY_API_KEY='your-key-here'")
        return
    
    # Initialize client and processor
    client = MeshyClient(api_key)
    processor = BatchProcessor(client, OUTPUT_DIR, parallel=args.parallel)
    
    # Process all models
    processor.process_all(models, start_from=args.start_from)

if __name__ == "__main__":
    main()
