"""Offline animation study; does not modify the Unity project.

Pillow only. Artwork is procedural, not Computer Use or model-generated art.
The previous Piskel drawings are reused unchanged in the timing-only control.
"""
from pathlib import Path
import base64
import io
import json
import math
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
OLD = OUT.parent / 'line-test-2026-09-20/st_hizuru_hammer_rough_11f.gif'
SIZE, SCALE, FPS = 512, 3, 25
CAMERA_SCALE = .82
PAPER = '#f6f3eb'
INK = '#283644'
BLUE = '#829dab'


def mix(a, b, t):
    return tuple(x + (y-x)*t for x, y in zip(a, b))


def add(a, b):
    return (a[0]+b[0], a[1]+b[1])


def sub(a, b):
    return (a[0]-b[0], a[1]-b[1])


def mul(a, s):
    return (a[0]*s, a[1]*s)


def distance(a, b):
    return math.dist(a, b)


def ik(a, c, l1, l2, side):
    """Two bones, exact wrist/ankle target. Reject unreachable input."""
    d = distance(a, c)
    if not abs(l1-l2) < d < l1+l2:
        raise ValueError(f'Unreachable target {a} -> {c}: {d}')
    u = mul(sub(c, a), 1/d)
    x = (l1*l1-l2*l2+d*d)/(2*d)
    y = math.sqrt(max(0, l1*l1-x*x))*side
    return add(a, (u[0]*x-u[1]*y, u[1]*x+u[0]*y))


# time, pelvis x/y, torso lean degrees, grip x/y, hammer angle degrees.
# Angle stays on one branch; recovery retraces the arc, not a full propeller turn.
KEYS = [
    (0.00, 260, 287, 0, 302, 211, 185),
    (0.28, 260, 287, 0, 302, 211, 185),
    (0.52, 252, 303, -8, 276, 192, 224),
    (0.72, 248, 307, -10, 264, 171, 252),
    (0.84, 248, 307, -10, 264, 171, 252),
    (0.92, 257, 298, 9, 306, 179, 295),
    (1.00, 271, 297, 19, 340, 243, 355),
    (1.08, 271, 297, 19, 340, 243, 355),
    (1.24, 274, 308, 23, 325, 271, 385),
    (1.44, 270, 305, 16, 310, 257, 380),
    (1.68, 264, 298, 7, 310, 209, 297),
    (1.96, 260, 291, 0, 293, 188, 228),
    (2.24, 260, 287, 0, 302, 211, 185),
    (2.56, 260, 287, 0, 302, 211, 185),
]


def sample(t):
    for a, b in zip(KEYS, KEYS[1:]):
        if t <= b[0]:
            u = max(0, (t-a[0])/(b[0]-a[0]))
            # Strike is linear and fast; preparation/recovery ease in/out.
            if not 0.84 <= a[0] < 1.0:
                u = u*u*(3-2*u)
            return mix(a[1:], b[1:], u)
    return KEYS[-1][1:]


def stage(t):
    if t < .28: return 'SHOULDER CARRY'
    if t < .84: return 'ANTICIPATION'
    if t < 1.: return 'STRIKE'
    if t < 1.08: return 'CONTACT HOLD'
    if t < 1.44: return 'FOLLOW THROUGH'
    if t < 2.24: return 'RECOVERY'
    return 'SHOULDER CARRY'


def render(t):
    px, py, lean, gx, gy, angle = sample(t)
    hip = (px, py)
    a = math.radians(lean)
    up = (math.sin(a), -math.cos(a))
    right = (math.cos(a), math.sin(a))
    chest = add(hip, mul(up, 93))
    head = add(chest, mul(up, 44))
    wa = math.radians(angle)
    u, v = (math.cos(wa), math.sin(wa)), (-math.sin(wa), math.cos(wa))
    grip = (gx, gy)
    hands = [add(grip, mul(u, 22)), add(grip, mul(u, -22))]
    shoulders = [add(chest, mul(right, -8)), add(chest, mul(right, 8))]
    elbows = [ik(s, w, 58, 58, 1) for s, w in zip(shoulders, hands)]
    feet = [(207, 445), (322, 445)]
    hips = [add(hip, mul(right, -8)), add(hip, mul(right, 8))]
    knees = [ik(h, f, 87, 85, -1) for h, f in zip(hips, feet)]
    im = Image.new('RGB', (SIZE*SCALE, SIZE*SCALE), PAPER)
    d = ImageDraw.Draw(im)

    def camera(p): return ((p[0]-256)*CAMERA_SCALE+256,(p[1]-256)*CAMERA_SCALE+256)
    def pts(seq): return [(round(x*SCALE), round(y*SCALE)) for x,y in map(camera,seq)]
    def line(seq, color=INK, width=2):
        d.line(pts(seq), fill=color, width=round(width*SCALE), joint='curve')
    def poly(seq, fill=PAPER, outline=INK, width=2):
        p = pts(seq)
        d.polygon(p, fill=fill)
        if outline: d.line(p+[p[0]], fill=outline, width=round(width*SCALE), joint='curve')
    def circle(p, r, fill=PAPER, outline=INK, width=2):
        x,y = camera(p)
        r *= CAMERA_SCALE
        d.ellipse((int((x-r)*SCALE),int((y-r)*SCALE),int((x+r)*SCALE),int((y+r)*SCALE)), fill=fill, outline=outline, width=round(width*SCALE))
    def segment(a,b,r1,r2,fill=PAPER):
        dv = sub(b,a); l=math.hypot(*dv); n=(-dv[1]/l,dv[0]/l)
        poly([add(a,mul(n,r1)),add(b,mul(n,r2)),add(b,mul(n,-r2)),add(a,mul(n,-r1))],fill)
    def limb(a,b,c,r1,r2,r3,fill=PAPER):
        segment(a,b,r1,r2,fill)
        circle(b,r2,fill,outline=None)
        segment(b,c,r2,r3,fill)
        # Joint circle avoids crossed straight polygon edges at the elbow.
        circle(b,r2*.8,fill,outline=None)
    def local(origin,x,y): return add(origin,add(mul(right,x),mul(up,-y)))

    line([(65,457),(451,457)], '#cecac0',1)
    for h,k,f in zip(hips,knees,feet):
        limb(h,k,f,13,10,6,'#dce1df')
        poly([(f[0]-8,440),(f[0]+6,440),(f[0]+24,452),(f[0]+24,457),(f[0]-10,457)],'#465663')

    # One rigid torso and one rigid head maintain the same shape across poses.
    poly([local(chest,-20,-4),local(chest,15,-4),local(hip,21,-4),local(hip,12,12),local(hip,-23,9),local(chest,-27,14)],'#e5e8e2')
    line([local(chest,0,8),local(hip,1,-8)],'#8b9697',1)
    poly([local(chest,-11,0),local(chest,0,21),local(chest,12,0)],PAPER)
    # A single head/hair shape. This prototype uses rigid hair, not hair simulation.
    poly([local(head,-17,-22),local(head,-26,-9),local(chest,-31,14),local(chest,-37,64),local(chest,-17,68),local(head,1,-20)],'#aebfc2')
    poly([local(head,-16,-22),local(head,5,-25),local(head,18,-15),local(head,19,2),local(head,27,9),local(head,18,12),local(head,15,25),local(head,0,29),local(head,-15,19)],PAPER)
    line([local(head,5,5),local(head,20,4),local(head,21,12),local(head,7,13),local(head,5,5)],INK,1.7)
    line([local(head,-7,7),local(head,5,8)],INK,1.2)
    line([local(head,16,19),local(head,22,18)],INK,1)

    # Rear and front arms share a prop with exact, constant grip offsets.
    for s,e,w in zip(shoulders,elbows,hands):
        limb(s,e,w,8,6,4,'#eef0e9')
    butt=add(grip,mul(u,-54))
    tip=add(grip,mul(u,176))
    poly([add(butt,mul(v,-3)),add(tip,mul(v,-3)),add(tip,mul(v,3)),add(butt,mul(v,3))],'#a6a496',INK,1.5)
    center=add(tip,mul(u,8))
    poly([add(center,add(mul(u,x),mul(v,y))) for x,y in [(-12,-17),(12,-17),(12,17),(-12,17)]],'#aebbc2',INK,2)
    for w in hands:
        circle(w,4.5,PAPER,INK,1.5)

    # Small wrist marks expose the grip anchors in the constraint study.
    # No camera shake or smear: first evaluate the body and prop motion itself.
    metrics={
        'arm_lengths': [[distance(s,e),distance(e,w)] for s,e,w in zip(shoulders,elbows,hands)],
        'leg_lengths': [[distance(h,k),distance(k,f)] for h,k,f in zip(hips,knees,feet)],
        'hammer_length':distance(butt,tip),
        'grip_distance':distance(*hands),
        'feet':feet,
        'phase':stage(t),
    }
    return im.resize((SIZE,SIZE),Image.Resampling.LANCZOS),metrics


def save_gif(frames, path, durations):
    frames[0].save(path,save_all=True,append_images=frames[1:],duration=durations,loop=0,optimize=False,disposal=2)


def embedded_sheet(frames):
    sheet=Image.new('RGB',(512*len(frames),512),PAPER)
    for i,f in enumerate(frames):sheet.paste(f,(i*512,0))
    data=io.BytesIO();sheet.save(data,format='PNG')
    return 'data:image/png;base64,'+base64.b64encode(data.getvalue()).decode()


def comparison_page(originals,frames,durations,metrics):
    data=json.dumps({'old':embedded_sheet(originals),'rig':embedded_sheet(frames),
                     'timing':durations,'phases':[m['phase'] for m in metrics]})
    html='''<!doctype html><html lang="zh-CN"><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>挥锤动作 · 连续性对照</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#eae8e0;color:#283644;font:15px/1.65 system-ui,sans-serif}
main{max-width:1400px;margin:auto;padding:30px}header{display:flex;justify-content:space-between;align-items:center;gap:24px}
h1{font-size:28px;margin:0 0 4px}p{margin:4px 0 18px;color:#52616a}.tag{font-size:12px;letter-spacing:2px;color:#69858d}
.controls{display:flex;flex-wrap:wrap;gap:15px;align-items:center;margin:20px 0}
button,select{font:inherit;background:#fff;border:1px solid #9eaaa9;border-radius:6px;padding:7px 16px;color:#283644;cursor:pointer}
button.primary{background:#283644;color:#fff}input[type=range]{flex:1;min-width:200px;accent-color:#517786}
.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:18px}.card{background:#f6f3eb;border:1px solid #d0d4cc;border-radius:10px;overflow:hidden}
.label{padding:15px 18px 0;font-weight:650}.sub{padding:0 18px;font-size:13px;color:#67716d}
canvas{display:block;width:100%;height:auto}.foot{padding:12px 18px;border-top:1px solid #deded4;font:12px ui-monospace,monospace;min-height:58px}
.notes{margin-top:22px;max-width:1000px}.notes li{margin:5px 0}.status{font-size:13px;color:#567468}
@media(max-width:800px){.grid{grid-template-columns:1fr}header{display:block}main{padding:18px}}
</style><main>
<header><div><div class="tag">MOTION STUDY / 02</div><h1>挥锤动作 · 连续性对照</h1>
<p>先看节奏，再看结构。右侧是程序骨架实验，不是手绘清稿或模型生成结果。</p></div><div id="status" class="status">正在加载本地画面…</div></header>
<div class="controls"><button id="play" class="primary">暂停</button><button id="restart">从头播放</button>
<label>速度 <select id="speed"><option value="0.5">0.5×</option><option value="1" selected>1×</option></select></label>
<label for="seek">停帧检查</label><input id="seek" type="range" min="0" max="1000" value="0" aria-label="停帧检查"><span id="percent">0%</span></div>
<div class="grid">
<section class="card"><div class="label">A · 原线稿</div><div class="sub"><label><input id="matched" type="checkbox"> 与 B 同总长（980 ms）</label></div><canvas id="a" width="512" height="512"></canvas><div class="foot" id="af"></div></section>
<section class="card"><div class="label">B · 只调整停留时间</div><div class="sub">原来的 11 张图不变 · 980 ms</div><canvas id="b" width="512" height="512"></canvas><div class="foot" id="bf"></div></section>
<section class="card"><div class="label">C · 固定骨长与双手握点</div><div class="sub">重新制作二维骨架 · 2600 ms</div><canvas id="c" width="512" height="512"></canvas><div class="foot" id="cf"></div></section>
</div>
<div class="notes"><strong>怎么比较</strong><ul>
<li>勾选 A 的“同总长”，比较 A/B，才能把整体变慢与节奏变化分开。</li>
<li>C 使用不同姿势、画法、镜头和时长，只验证约束能否避免漂移，不是单变量美术优劣对照。</li>
<li>暂停拖动时，三栏显示各自动作周期的相同比例位置；正常播放则保留各自实际时长。</li>
<li>C 的骨长、锤柄和握点固定，首尾像素一致。仍没有目标碰撞、衣发动态和人体关节角限制；停顿只是接触假设。</li>
</ul><p>全部画面内嵌本页；不联网，不需要运行游戏。动画研究 · 2026-09-21</p></div></main>
<script>const data=__DATA__;
const old=new Image(),rig=new Image();old.src=data.old;rig.src=data.rig;
const $=id=>document.getElementById(id);let playing=true,elapsed=0,last=0,fraction=0;
const total=a=>a.reduce((x,y)=>x+y,0);
function index(ds,t){let s=0;for(let i=0;i<ds.length;i++){s+=ds[i];if(t<s)return i}return ds.length-1}
function paint(id,img,i){$(id).getContext('2d').drawImage(img,i*512,0,512,512,0,0,512,512)}
function draw(){let ad=$('matched').checked?[...Array(10).fill(90),80]:Array(11).fill(80),bd=data.timing;
let at=playing?elapsed%total(ad):fraction*(total(ad)-.01),bt=playing?elapsed%total(bd):fraction*(total(bd)-.01),ct=playing?elapsed%2600:fraction*2599.99;
let ai=index(ad,at),bi=index(bd,bt),ci=Math.min(64,Math.floor(ct/40));paint('a',old,ai);paint('b',old,bi);paint('c',rig,ci);
$('af').textContent=`Frame ${ai+1}/11 · ${ad[ai]} ms · cycle ${total(ad)} ms`;
$('bf').textContent=`Frame ${bi+1}/11 · ${bd[bi]} ms · cycle 980 ms`;
$('cf').textContent=`Frame ${ci+1}/65 · ${data.phases[ci]}`;
if(playing){fraction=ct/2600}$('seek').value=Math.round(fraction*1000);$('percent').textContent=Math.round(fraction*100)+'%'}
function tick(t){if(last&&playing)elapsed+=(t-last)*Number($('speed').value);last=t;draw();requestAnimationFrame(tick)}
$('play').onclick=()=>{playing=!playing;if(playing)elapsed=fraction*2600;$('play').textContent=playing?'暂停':'播放';draw()};
$('restart').onclick=()=>{elapsed=0;fraction=0;draw()};
$('seek').oninput=()=>{playing=false;fraction=Number($('seek').value)/1000;$('play').textContent='播放';draw()};
$('matched').onchange=draw;
Promise.all([old.decode(),rig.decode()]).then(()=>{$('status').textContent='画面已加载 · 离线可用';requestAnimationFrame(tick)}).catch(e=>{$('status').textContent='画面加载失败：'+e.message});
</script></html>'''
    (OUT/'comparison.html').write_text(html.replace('__DATA__',data))


def main():
    old = Image.open(OLD)
    originals=[]
    for n in range(old.n_frames):
        old.seek(n);originals.append(old.convert('RGB'))
    # Timing-only control: existing pixels unchanged, no generated inbetweens.
    durations=[100,100,120,160,40,30,30,100,70,100,130]
    save_gif(originals,OUT/'timing_only.gif',durations)
    save_gif(originals,OUT/'uniform_same_duration.gif',[90]*10+[80])
    frames,metrics=[],[]
    for i in range(round(KEYS[-1][0]*FPS)+1):
        f,m=render(i/FPS);frames.append(f);metrics.append(m)
    save_gif(frames,OUT/'constrained_rig.gif',40)
    # Contact sheet from actual rendered images.
    chosen=[0,13,18,23,25,27,31,36,42,49,56,64]
    sheet=Image.new('RGB',(4*256,3*280),PAPER)
    sd=ImageDraw.Draw(sheet)
    for j,i in enumerate(chosen):
        x,y=(j%4)*256,(j//4)*280
        sheet.paste(frames[i].resize((256,256),Image.Resampling.LANCZOS),(x,y))
        sd.text((x+10,y+257),f'{i/FPS:.2f}s  {metrics[i]["phase"]}',fill=INK)
    sheet.save(OUT/'contact_sheet.jpg',quality=92)
    checks={
        'source':'Procedural Pillow 2D IK study, no AI model or Computer Use drawing',
        'generated_samples':len(frames),'sample_fps':FPS,
        'max_arm_length_error_px':max(abs(x-58) for m in metrics for pair in m['arm_lengths'] for x in pair),
        'max_leg_length_error_px':max(abs(x-target) for m in metrics for pair in m['leg_lengths'] for x,target in zip(pair,[87,85])),
        'max_hammer_length_error_px':max(abs(m['hammer_length']-230) for m in metrics),
        'max_grip_spacing_error_px':max(abs(m['grip_distance']-44) for m in metrics),
        'fixed_foot_targets':all(m['feet']==metrics[0]['feet'] for m in metrics),
        'first_last_pixels_equal':frames[0].tobytes()==frames[-1].tobytes(),
        'timing_only_durations_ms':durations,
        'limitations':['Rigid hair, no cloth simulation','No collision or anatomical joint-angle solver','Stylized structural study, not final character art','Numerical constraints do not prove convincing weight or gameplay quality'],
    }
    for name in ['timing_only.gif','uniform_same_duration.gif','constrained_rig.gif']:
        gif=Image.open(OUT/name);total=0
        for i in range(gif.n_frames):gif.seek(i);total+=gif.info['duration']
        checks[name]={'encoded_frames':gif.n_frames,'duration_ms':total,'size':list(gif.size)}
    (OUT/'verification.json').write_text(json.dumps(checks,indent=2)+'\n')
    comparison_page(originals,frames,durations,metrics)
    print(json.dumps(checks,indent=2))


if __name__=='__main__':main()
