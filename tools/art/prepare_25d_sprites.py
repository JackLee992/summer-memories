#!/usr/bin/env python3
"""Pack approved RGBA cutouts; never infer transparency from clothing/skin colors.
Usage: python prepare_25d_sprites.py CUTOUT_DIR REPO_ROOT
The input directory must contain shinpei.png, ushio.png, hizuru.png and mio.png.
"""
import sys,json
from pathlib import Path
from PIL import Image,ImageDraw
source=Path(sys.argv[1]);root=Path(sys.argv[2]);out=root/'Assets/Resources/Art/Portraits/25D';out.mkdir(parents=True,exist_ok=True)
contact=Image.new('RGB',(1400,900),'#dfe5de');draw=ImageDraw.Draw(contact);records=[]
for i,name in enumerate(['shinpei','ushio','hizuru','mio']):
 image=Image.open(source/(name+'.png'))
 if image.mode!='RGBA' or image.getchannel('A').getextrema()==(255,255):raise ValueError(name+' needs an approved alpha matte')
 bounds=image.getchannel('A').point(lambda a:255 if a>32 else 0).getbbox()
 if not bounds:raise ValueError(name+' produced an empty sprite')
 clean=image.crop(bounds);w=round(clean.width*760/clean.height);clean=clean.resize((w,760),Image.Resampling.LANCZOS)
 result=Image.new('RGBA',(w+24,784));result.alpha_composite(clean,(12,12));dest=out/('st_'+name+'.png');result.save(dest)
 thumb=result.copy();thumb.thumbnail((335,800));contact.paste(thumb,(i*350+(350-thumb.width)//2,50),thumb);draw.text((i*350+15,865),name,fill='#23404b')
 records.append({'id':'st_'+name,'path':str(dest.relative_to(root)),'size':list(result.size),'sourceSize':list(image.size),'bounds':list(bounds)})
contact.save(source/'sprites-contact.jpg');(source/'sprite-processing.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(records,ensure_ascii=False))
