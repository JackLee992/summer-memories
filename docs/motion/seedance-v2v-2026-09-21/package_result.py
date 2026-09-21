"""Package a downloaded Seedance result without changing the provider's video."""
from pathlib import Path
import subprocess,json,base64,hashlib,shutil
from PIL import Image,ImageDraw
OUT=Path(__file__).resolve().parent
FF='/opt/homebrew/bin/ffmpeg';PROBE='/opt/homebrew/bin/ffprobe'
def run(args):return subprocess.run(args,check=True,capture_output=True,text=True)
def meta(p):return json.loads(run([PROBE,'-v','error','-show_entries','stream=codec_type,codec_name,width,height,nb_frames,r_frame_rate:format=duration','-of','json',str(p)]).stdout)
original=OUT/'seedance_original.mp4'
if not original.exists():raise SystemExit('Provider result has not been downloaded/copied yet')
source=OUT/'input_reference_4s.mp4'
metadata={'input':meta(source),'output':meta(original)}
(OUT/'media_check.json').write_text(json.dumps(metadata,indent=2)+'\n')
duration=float(metadata['output']['format']['duration']);source_duration=float(metadata['input']['format']['duration'])
frames=OUT/'qa_frames';frames.mkdir(exist_ok=True)
times=[0,.5,1,1.35,1.65,1.9,2.2,2.5,2.9,3.2,3.5,3.9]
board=Image.new('RGB',(1440,4*262),'#142126');d=ImageDraw.Draw(board)
for row,(label,p) in enumerate([('BLENDER INPUT',source),('SEEDANCE OUTPUT',original)]):
    for i,t in enumerate(times):
        dest=frames/(('input' if row==0 else 'output')+f'_{i:02d}.png')
        run([FF,'-y','-loglevel','error','-ss',str(min(t,(source_duration if row==0 else duration)-.07)),'-i',str(p),'-frames:v','1',str(dest)])
        im=Image.open(dest).convert('RGB');im.thumbnail((240,240))
        x=(i%6)*240;y=(row*2+i//6)*262
        board.paste(im,(x+(240-im.width)//2,y+(240-im.height)//2));d.text((x+8,y+243),f'{label} / {t:.2f}s',fill='#e3ece7')
board.save(OUT/'frame_comparison.jpg',quality=93)
run([FF,'-y','-loglevel','error','-i',str(original),'-filter_complex','fps=16,scale=540:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a','-loop','0',str(OUT/'seedance_preview.gif')])
def data(path,mime):return 'data:'+mime+';base64,'+base64.b64encode(path.read_bytes()).decode()
html='''<!doctype html><html lang="zh-CN" translate="no"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Blender → Seedance · 3D动画精制测试</title><style>*{box-sizing:border-box}body{margin:0;background:#111b20;color:#dfeae5;font:15px/1.7 -apple-system,BlinkMacSystemFont,"PingFang SC",sans-serif}main{max-width:1140px;margin:auto;padding:28px 24px 55px}h1{font-size:28px;margin:5px 0 8px}.muted{color:#9ab0b4;font-size:13px}.eyebrow{font-size:11px;letter-spacing:2px;color:#90baaf}.grid{display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-top:25px}.card{background:#1b2a30;border:1px solid #34484e;border-radius:12px;overflow:hidden}.label{padding:12px 16px;font-weight:600}video{width:100%;display:block;background:#293d43;aspect-ratio:1}.toolbar{display:flex;flex-wrap:wrap;gap:12px;align-items:center;margin:20px 0}button,select{background:#294047;color:#e4f1e9;border:1px solid #527069;border-radius:6px;padding:9px 15px;font:inherit;cursor:pointer}input{flex:1;accent-color:#bed9cd;min-width:180px}a{color:#9ed5c1}.sheet{width:100%;border-radius:9px}h2{font-size:19px;margin-top:28px}.notes{padding:16px 20px;background:#1a2b30;border-left:3px solid #9abda7}.notes p{margin:5px 0}footer{font-size:12px;color:#87a3a5;margin-top:24px}@media(max-width:700px){.grid{grid-template-columns:1fr}main{padding:18px 12px}}</style>
<main><div class="eyebrow">SUMMER MEMORIES / SEEDANCE REFERENCE TEST</div><h1>用现有动作，测试更精致的三维表现</h1><p class="muted">Seedance 2.0 · 全能参考 · 1080p 请求 · 单次生成 / 264 积分</p>
<div class="grid"><section class="card"><div class="label">A · 实际上传的 Blender 动画</div><video id="a" muted playsinline preload="metadata" controls src="__INPUT__"></video></section><section class="card"><div class="label">B · Seedance 原始返回</div><video id="b" playsinline preload="metadata" controls src="__OUTPUT__"></video></section></div>
<div class="toolbar"><button id="play">播放对照</button><button id="reset">回到开头</button><select id="speed" aria-label="播放速度"><option value="1">1× 原速</option><option value=".5">0.5× 慢放</option></select><input id="seek" aria-label="时间" type="range" min="0" max="3.95" step=".05" value="0"><span id="time">0.00 s</span></div>
<p class="muted">原片3.6秒，末尾补0.4秒静止供输入；动作没有变速。生成模式使用视频作参考，并不保证逐帧复制。两栏按相同经过时间定位。</p>
<div class="notes"><p><b>这次验证什么</b>：人物、头发、衣料与金属质感是否改善；单手扛锤、接柄、下砸、回肩是否保持。</p><p>生成结果是视频像素，不会自动返回升级后的3D模型、材质、绑定或动作曲线。</p><p><a href="README.md">实际观察与限制</a> · <a href="prompt.txt">完整提示词</a> · <a href="generation.json">生成记录</a> · <a href="seedance_original.mp4">原始MP4</a></p></div>
<h2>按相同时间取帧对照</h2><img class="sheet" src="__SHEET__" alt="输入与Seedance生成的12个时间点对照"><footer>《夏日重现》日鹤同人原型实验，非官方作品。保留原输入、原输出与任务记录。</footer></main>
<script>const $=id=>document.getElementById(id),a=$('a'),b=$('b');let playing=false;function stop(){playing=false;a.pause();b.pause();$('play').textContent='播放对照'}function seek(t){stop();a.currentTime=t;b.currentTime=t;$('seek').value=t;$('time').textContent=t.toFixed(2)+' s'}$('play').onclick=async()=>{if(playing){stop();return}if(b.currentTime>=3.95)seek(0);a.currentTime=b.currentTime;try{await Promise.all([a.play(),b.play()]);playing=true;$('play').textContent='暂停'}catch(e){stop()}};$('reset').onclick=()=>seek(0);$('seek').oninput=()=>seek(Number($('seek').value));$('speed').onchange=()=>{a.playbackRate=b.playbackRate=Number($('speed').value)};b.onended=stop;function tick(){if(playing){const t=Math.min(3.95,b.currentTime);$('seek').value=t;$('time').textContent=t.toFixed(2)+' s'}requestAnimationFrame(tick)}tick();</script></html>'''
html=html.replace('__INPUT__',data(source,'video/mp4')).replace('__OUTPUT__',data(original,'video/mp4')).replace('__SHEET__',data(OUT/'frame_comparison.jpg','image/jpeg'))
(OUT/'comparison.html').write_text(html)
m=json.loads((OUT/'generation.json').read_text());m['local_output']={'file':original.name,'bytes':original.stat().st_size,'sha256':hashlib.sha256(original.read_bytes()).hexdigest(),'metadata':metadata['output']};(OUT/'generation.json').write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(metadata,indent=2))
