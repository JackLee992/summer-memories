#!/usr/bin/env python3
"""Pack visually selected poses from irregular AI sheets, using local BiRefNet alpha.
Coordinates refer to the 1400x1050 review image, NOT an assumed uniform model grid.
Repeated entries are intentional pose holds; this does not create in-between frames.
"""
import json,sys,hashlib
from pathlib import Path
from PIL import Image,ImageDraw,ImageChops
from rembg import new_session,remove
root=Path(__file__).resolve().parents[2];source=root/'Builds/art-side2d';out=root/'Assets/Resources/Art/Side2D';out.mkdir(parents=True,exist_ok=True)
# bbox, feet anchor. Each entry was inspected before inclusion; malformed weapon poses excluded.
poses={
'shinpei':[
([80,365,230,676],[151,663]),([305,18,502,330],[420,315]),([285,374,505,676],[406,662]),([919,18,1135,330],[1041,315]),
([505,377,780,679],[635,662]),([305,18,502,330],[420,315]),([285,374,505,676],[406,662]),([787,378,1111,679],[918,660]),
([300,17,503,330],[420,315]),([787,378,1111,679],[918,660]),([1124,380,1391,680],[1240,660]),([1160,716,1390,1037],[1250,1018])],
'ushio':[
([20,6,292,345],[160,333]),([313,8,504,343],[420,332]),([355,383,651,695],[484,685]),([919,8,1079,343],[1002,332]),
([731,383,1018,695],[851,685]),([355,383,651,695],[484,685]),([313,8,504,343],[420,332]),([1090,378,1389,695],[1233,685]),
([1090,378,1389,695],[1233,685]),([1090,713,1398,1039],[1225,1021]),([1090,713,1398,1039],[1225,1021]),([20,6,292,345],[160,333])],
'hizuru':[
([721,712,1032,1034],[866,1020]),([505,17,696,342],[598,328]),([902,25,1052,339],[983,328]),([707,23,895,341],[794,328]),
([516,373,696,678],[601,669]),([516,373,696,678],[601,669]),([352,24,477,340],[421,328]),([724,383,1050,681],[882,671]),
([721,712,1032,1034],[866,1020]),([1080,368,1398,681],[1220,670]),([721,712,1032,1034],[866,1020]),([518,709,678,1034],[601,1020])]
}
session=new_session('birefnet-general-lite',providers=['CPUExecutionProvider']);records=[]
for name,entries in poses.items():
 path=next((source/name).glob('*.png'));original=Image.open(path).convert('RGB');factor=original.width/1400
 weapon=None
 if name=='hizuru':
  # Extract the illustrated long hammer from the approved shoulder pose, excluding hair/body.
  wp=original.crop(tuple(round(v*factor) for v in [732,724,1018,813])).convert('RGBA')
  mask=Image.new('L',wp.size);draw=ImageDraw.Draw(mask)
  poly=[(738,738),(754,727),(803,746),(795,774),(1014,789),(1013,800),(792,783),(787,810),(738,795)]
  draw.polygon([(round((x-732)*factor),round((y-724)*factor)) for x,y in poly],fill=255)
  wp.putalpha(mask);weapon=wp.resize((389,121),Image.Resampling.LANCZOS)
 atlas=Image.new('RGBA',(2048,1536));cache={};review=Image.new('RGB',(2048,1536),'#6b827d')
 folder=source/(name+'-cutouts');folder.mkdir(exist_ok=True)
 for i,(box,anchor) in enumerate(entries):
  key=tuple(box);crop=original.crop(tuple(round(n*factor) for n in box))
  fn=folder/(str(i)+'.png')
  if key in cache:cut=cache[key]
  elif fn.exists():cut=Image.open(fn).convert('RGBA');cache[key]=cut
  else:cut=remove(crop,session=session).convert('RGBA');cut.save(fn);cache[key]=cut
  # Consistent pixels per character across all poses; anchor feet instead of bbox center.
  scale=1.36/factor;cut=cut.resize((round(cut.width*scale),round(cut.height*scale)),Image.Resampling.LANCZOS)
  tile=Image.new('RGBA',(512,512));offset=(round(256-(anchor[0]-box[0])*1.36),round(482-(anchor[1]-box[1])*1.36));
  if weapon is not None and i in [1,2,3,4,5,6,7,11]:tile.alpha_composite(weapon,(50,153 if i!=7 else 200))
  tile.alpha_composite(cut,offset)
  atlas.alpha_composite(tile,(i%4*512,i//4*512));review.paste(tile,(i%4*512,i//4*512),tile)
  print(name,i,flush=True)
 dest=out/('st_'+name+'.png');atlas.save(dest);review.save(source/(name+'-packed-qa.jpg'),quality=94)
 records.append({'id':name,'sourceSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'sourceSize':list(original.size),'output':str(dest.relative_to(root)),'model':'birefnet-general-lite','poses':entries,'reviewCoordinates':[1400,1050],'pixelsPerReviewPixel':1.36,'frameSize':[512,512],'feetPixel':[256,482]})
(source/'packing.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n')
