"""Normalize generated 4x5 character masters; never scale frames independently."""
from pathlib import Path
import argparse
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]

def pack(name, source):
    master = Image.open(source).convert('RGBA')
    source_dir = ROOT / 'Docs/art/default-characters/source'
    source_dir.mkdir(parents=True, exist_ok=True)
    master.save(source_dir / (name + '-master.png'))
    cells = []
    for i in range(20):
        x, y = i % 4, i // 4
        cell = master.crop((round(x*master.width/4), round(y*master.height/5),
                            round((x+1)*master.width/4), round((y+1)*master.height/5)))
        data = np.array(cell)
        # Retain the character component, excluding isolated generation speckles.
        labels, count = ndimage.label(data[:,:,3] > 32)
        sizes = np.bincount(labels.ravel()); sizes[0] = 0
        mask = labels == sizes.argmax()
        mask = ndimage.binary_fill_holes(mask)
        mask = ndimage.binary_dilation(mask, iterations=1)
        data[:,:,3][~mask] = 0
        cells.append(Image.fromarray(data))
    box = cells[0].getbbox()
    scale = 380 / (box[3]-box[1])
    frames = []
    for cell in cells:
        frame = Image.new('RGBA', (512,512))
        resized = cell.resize((round(cell.width*scale),round(cell.height*scale)), Image.Resampling.LANCZOS)
        # Same translation and scale for the entire animation, not per-frame fitting.
        ox = round(256-(box[0]+box[2])/2*scale)
        oy = round(460-box[3]*scale)
        frame.alpha_composite(resized,(ox,oy))
        frames.append(frame.resize((256,256), Image.Resampling.LANCZOS))
    dest = ROOT / 'Assets/Resources/C1Character/Defaults' / name
    dest.mkdir(parents=True, exist_ok=True)
    actions = {'idle':[0,1,1,2,2,3,3,0], 'run':list(range(4,12)), 'jump':list(range(12,20))}
    preview_dir = ROOT / 'Docs/art/default-characters'
    for action, indices in actions.items():
        sheet = Image.new('RGBA',(2048,256))
        for i,index in enumerate(indices):
            sheet.alpha_composite(frames[index],(256*i,0))
        sheet.save(dest / (action+'.png'))
        thumbnails = []
        for index in indices:
            bg = Image.new('RGBA',(256,256),(246,242,230,255))
            bg.alpha_composite(frames[index].resize((256,256),Image.Resampling.LANCZOS))
            thumbnails.append(bg.convert('RGB'))
        thumbnails[0].save(preview_dir / (name+'-'+action+'.gif'),save_all=True,
                           append_images=thumbnails[1:],duration=100 if action!='idle' else 167,loop=0)
    ui = ROOT / 'Assets/Resources/C1CharacterUI'
    frames[0].save(ui/(name+'.png'))
    frames[4].save(ui/(name+'-run.png'))
    frames[15].save(ui/(name+'-jump.png'))
    # Dedicated face portrait, same 512px canvas as all other standalone assets.
    portrait = frames[0].crop((32,32,224,175)).resize((236,176),Image.Resampling.LANCZOS)
    thumbnail = Image.new('RGBA',(256,256))
    thumbnail.alpha_composite(portrait,(10,40))
    thumbnail.save(ui/(name+'-thumbnail.png'))
    print(name, master.size, '-> three 2048x256 atlases and 256x256 UI images')

if __name__ == '__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('green'); parser.add_argument('chick')
    args=parser.parse_args(); pack('green',args.green); pack('chick',args.chick)
