"""导入用户切图并提取参考图中的角色；不改变用户原始文件。"""
from pathlib import Path
from PIL import Image
from collections import deque
import shutil

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/C1CharacterUI'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE = ROOT.parent / '资源/已处理'
MAPPING = {
    'title': 'title-my-heroes.png', 'back': 'button-back-home.png',
    'paper': '切片_2_副本2-removebg-preview.png',
    'selected': '切片_1_副本2-removebg-preview.png',
    'star-on': '切片_1_副本3-removebg-preview.png',
    'star-off': '切片_2_副本3-removebg-preview.png',
    'add': '切片_3_副本2-removebg-preview.png',
    'preview-paper': '切片_4-removebg-preview.png',
    'run-card': '切片_5-removebg-preview.png',
    'jump-card': '切片_6-removebg-preview.png',
    'create': '切片_7-removebg-preview.png',
    'use': '切片_8-removebg-preview.png',
}
for name, file in MAPPING.items():
    shutil.copy2(SOURCE / file, OUT / (name + '.png'))
REFERENCE = ROOT / 'Docs/ui/character-source'
reference = Image.open(REFERENCE / 'character-screen.png').convert('RGBA')

def cutout(image, rect, name, largest=False):
    image = image.crop(rect)
    pix = image.load()
    width, height = image.size
    queue = deque([(x, 0) for x in range(width)] + [(x, height-1) for x in range(width)] + [(0,y) for y in range(height)] + [(width-1,y) for y in range(height)])
    seen = set()
    while queue:
        x,y = queue.popleft()
        if (x,y) in seen or not (0 <= x < width and 0 <= y < height):
            continue
        seen.add((x,y))
        r,g,b,a = pix[x,y]
        if min(r,g,b) < 155 or max(r,g,b)-min(r,g,b) > 65:
            continue
        pix[x,y] = (r,g,b,0)
        queue.extend([(x-1,y),(x+1,y),(x,y-1),(x,y+1)])
    if largest:
        visited, components = set(), []
        for y in range(height):
            for x in range(width):
                if (x,y) in visited or pix[x,y][3] == 0:
                    continue
                component, pending = [], [(x,y)]
                while pending:
                    cx,cy = pending.pop()
                    if not (0 <= cx < width and 0 <= cy < height) or (cx,cy) in visited or pix[cx,cy][3] == 0:
                        continue
                    visited.add((cx,cy)); component.append((cx,cy))
                    pending.extend([(cx-1,cy),(cx+1,cy),(cx,cy-1),(cx,cy+1)])
                components.append(component)
        keep = set(max(components, key=len))
        for y in range(height):
            for x in range(width):
                if (x,y) not in keep:
                    r,g,b,a = pix[x,y]; pix[x,y] = (r,g,b,0)
    image = image.crop(image.getbbox())
    image.save(OUT / (name + '.png'))

cutout(reference, (746,316,1014,630), 'green', True)
home = Image.open(REFERENCE / 'home-screen.png').convert('RGBA')
cutout(home, (143,315,378,562), 'chick', True)
chick = Image.open(OUT / 'chick.png').convert('RGBA')
pixels = chick.load()
for y in range(max(0,chick.height-10), chick.height):
    for x in range(chick.width):
        r,g,b,a = pixels[x,y]
        if max(r,g,b) < 180 and max(r,g,b)-min(r,g,b) < 40:
            pixels[x,y] = (r,g,b,0)
chick.save(OUT / 'chick.png')
for character in ['green', 'chick']:
    image = Image.open(OUT / (character + '.png'))
    image.crop((0,0,image.width,int(image.height * .63))).save(OUT / (character + '-thumbnail.png'))
for action, rect in [('run',(103,18,258,184)),('jump',(85,14,255,162))]:
    card = Image.open(OUT / (action + '-card.png')).convert('RGBA')
    cutout(card, rect, 'green-' + action, True)
    paper = Image.open(OUT / 'paper.png').convert('RGBA').crop((40,30,330,120))
    patch = paper.resize((rect[2]-rect[0], rect[3]-rect[1]), Image.Resampling.LANCZOS)
    card.paste(patch, rect[:2])
    card.save(OUT / (action + '-card.png'))
shutil.copy2(ROOT.parent / '资源/未处理/切片 3_副本.png', OUT / 'notebook.png')
for name, rect in [('cloud',(80,17,246,117)),('sun',(352,15,516,160)),('tree-left',(0,625,173,940)),('tree-right',(1533,630,1672,940))]:
    cutout(reference, rect, name)
cutout(reference, (0,891,1672,941), 'bottom-grass')
grass = Image.open(OUT / 'bottom-grass.png').convert('RGBA')
pixels = grass.load()
for y in range(min(16,grass.height)):
    for x in range(grass.width):
        r,g,b,a = pixels[x,y]
        pixels[x,y] = (r,g,b,int(a * y / 16))
grass.save(OUT / 'bottom-grass.png')
shutil.copy2(ROOT / 'Assets/Art/UI/PictureBook/home-paper-background.png', OUT / 'background.png')
print('Prepared', len(list(OUT.glob('*.png'))), 'UI assets:', OUT)
