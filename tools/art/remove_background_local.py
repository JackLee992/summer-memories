#!/usr/bin/env python3
"""Batch local BiRefNet inference, original-size RGBA, and inspectable matte previews."""
import argparse,hashlib,json,time
from pathlib import Path
from PIL import Image,ImageOps,ImageDraw
from rembg import new_session,remove

def main():
 p=argparse.ArgumentParser(description=__doc__)
 p.add_argument('images',nargs='+',type=Path)
 p.add_argument('--output-dir',type=Path,required=True)
 p.add_argument('--model',default='birefnet-general',choices=['birefnet-general','birefnet-general-lite','birefnet-portrait'])
 p.add_argument('--overwrite',action='store_true')
 a=p.parse_args();a.output_dir.mkdir(parents=True,exist_ok=True)
 destinations=[a.output_dir/(x.stem+'.png') for x in a.images]
 if len(set(destinations))!=len(destinations):p.error('Input stems must be unique.')
 for src,dst in zip(a.images,destinations):
  if src.resolve()==dst.resolve():p.error('Output cannot overwrite an input.')
  if dst.exists() and not a.overwrite:p.error('Output already exists: '+str(dst))
 session=new_session(a.model,providers=['CPUExecutionProvider']);records=[]
 for src,dst in zip(a.images,destinations):
  start=time.monotonic();original=ImageOps.exif_transpose(Image.open(src)).convert('RGB')
  cutout=remove(original,session=session).convert('RGBA');cutout.save(dst)
  alpha=cutout.getchannel('A');bounds=alpha.point(lambda v:255 if v>32 else 0).getbbox()
  if not bounds or alpha.getextrema()==(255,255):raise ValueError('Invalid matte: '+str(src))
  thumb=cutout.copy();thumb.thumbnail((420,620),Image.Resampling.LANCZOS)
  sheet=Image.new('RGB',(1680,660),'#dddddd');draw=ImageDraw.Draw(sheet)
  for n,color in enumerate(['#f7f3e8','#1b2934','#688276']):
   tile=Image.new('RGBA',(420,620),color);tile.alpha_composite(thumb,((420-thumb.width)//2,(620-thumb.height)//2));sheet.paste(tile.convert('RGB'),(420*n,0));draw.text((420*n+10,635),color,fill='black')
  mask=alpha.copy();mask.thumbnail((420,620));sheet.paste(mask,(1260+(420-mask.width)//2,(620-mask.height)//2));draw.text((1270,635),'alpha',fill='black')
  preview=dst.with_name(dst.stem+'-qa.jpg');sheet.save(preview,quality=94)
  record={'input':str(src.resolve()),'inputSha256':hashlib.sha256(src.read_bytes()).hexdigest(),'output':str(dst.resolve()),'outputSha256':hashlib.sha256(dst.read_bytes()).hexdigest(),'size':list(cutout.size),'foregroundBounds':list(bounds),'seconds':round(time.monotonic()-start,2),'model':a.model,'provider':'CPUExecutionProvider','preview':str(preview.resolve())}
  records.append(record);print(json.dumps(record,ensure_ascii=False),flush=True)
 (a.output_dir/'manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':main()
