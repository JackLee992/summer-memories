"""Offline animation study; does not modify the Unity project.

Pillow only. Artwork is procedural, not Computer Use or model-generated art.
The previous Piskel drawings are reused unchanged in the timing-only control.
"""
from pathlib import Path
import base64
import io
import json
import math
import importlib.util
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
OLD = OUT.parent / 'line-test-2026-09-20/st_hizuru_hammer_rough_11f.gif'
SIZE, SCALE, FPS = 512, 3, 25
CAMERA_SCALE = .82
PAPER = '#f6f3eb'
INK = '#283644'
BLUE = '#829dab'
HAIR_LENGTHS = [30,30,28,23]
PERIOD = 2.6
HAIR = []


def hair_driver(t):
    px,py,lean,gx,gy,angle=sample(t%PERIOD)
    a=math.radians(lean)
    return (px+47*math.sin(a*.45)+46*math.sin(a)+44*math.sin(a*.35),lean)


def prepare_hair():
    """Independent angular springs, warmed into a periodic state.

    Deliberately not a cloth/contact simulator. Geometry is reconstructed with
    fixed segment lengths. No external package code or trained weights used.
    """
    dt=.005
    steps=round(PERIOD/dt)
    angles=[0.]*4
    velocities=[0.]*4
    for cycle in range(8):
        for n in range(steps):
            t=n*dt
            if cycle==7:HAIR.append(angles.copy())
            for j in range(4):
                delayed=t-j*.025
                x,lean=hair_driver(delayed)
                vx=(hair_driver(delayed+.01)[0]-hair_driver(delayed-.01)[0])/.02
                target=math.radians(max(-42,min(42,-lean*.35-vx*.14)))
                omega=13-j*1.2
                acceleration=omega*omega*(target-angles[j])-2*.72*omega*velocities[j]
                velocities[j]+=acceleration*dt
                angles[j]+=velocities[j]*dt
                angles[j]=max(math.radians(-48),min(math.radians(48),angles[j]))
    HAIR.append(angles.copy())


def hair_at(t):
    x=(t%PERIOD)/.005
    i=int(x)
    return mix(HAIR[i],HAIR[min(i+1,len(HAIR)-1)],x-i)


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


def render(t, secondary=True):
    px, py, lean, gx, gy, angle = sample(t)
    hip = (px, py)
    a = math.radians(lean)
    up = (math.sin(a), -math.cos(a))
    right = (math.cos(a), math.sin(a))
    lower_angle = a * .45
    lower_up = (math.sin(lower_angle), -math.cos(lower_angle))
    waist = add(hip, mul(lower_up, 47))
    chest = add(waist, mul(up, 46))
    head_angle = a * .35
    head_up = (math.sin(head_angle), -math.cos(head_angle))
    head_right = (math.cos(head_angle), math.sin(head_angle))
    head = add(chest, mul(head_up, 44))
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
    def headlocal(origin,x,y): return add(origin,add(mul(head_right,x),mul(head_up,-y)))

    line([(65,457),(451,457)], '#cecac0',1)
    for h,k,f in zip(hips,knees,feet):
        limb(h,k,f,13,10,6,'#dce1df')
        poly([(f[0]-8,440),(f[0]+6,440),(f[0]+24,452),(f[0]+24,457),(f[0]-10,457)],'#465663')

    # One rigid torso and one rigid head maintain the same shape across poses.
    poly([local(chest,-20,-4),local(chest,15,-4),local(waist,19,0),local(hip,21,-4),local(hip,12,12),local(hip,-23,9),local(waist,-22,0),local(chest,-27,14)],'#e5e8e2')
    line([local(chest,0,8),local(hip,1,-8)],'#8b9697',1)
    poly([local(chest,-11,0),local(chest,0,21),local(chest,12,0)],PAPER)
    # Hair uses fixed-length segments and a damped angular follower.
    root = headlocal(head,-19,-9)
    angles = hair_at(t) if secondary else [-head_angle]*4
    chain = [root]
    corrected_angles=[]
    separation_adjustments=0
    def behind_torso(point, margin):
        if not chest[1]-12 <= point[1] <= hip[1]+15:
            return True
        q=max(0,min(1,(point[1]-chest[1])/(hip[1]-chest[1])))
        center_x=chest[0]*(1-q)+hip[0]*q
        return point[0] <= center_x-24-margin
    for i,(length,theta) in enumerate(zip(HAIR_LENGTHS,angles)):
        # Conservative side-view separation plane, not a full collision solver.
        def endpoint(candidate):
            return add(chain[-1],(math.sin(candidate)*length,math.cos(candidate)*length))
        if secondary and not behind_torso(endpoint(theta),[12,10,7,2][i]):
            candidates=[math.radians(-70+k*.5) for k in range(281)]
            valid=[v for v in candidates if behind_torso(endpoint(v),[12,10,7,2][i])]
            if valid:
                theta=min(valid,key=lambda v:abs(v-theta))
                separation_adjustments+=1
        chain.append(endpoint(theta))
        corrected_angles.append(theta)
    widths=[12,12,10,7,2]
    left=[];right_edge=[]
    for i,p in enumerate(chain):
        tangent=sub(chain[min(i+1,4)],chain[max(i-1,0)])
        norm=math.hypot(*tangent);normal=(tangent[1]/norm,-tangent[0]/norm)
        left.append(add(p,mul(normal,-widths[i])))
        right_edge.append(add(p,mul(normal,widths[i])))
    poly(left+right_edge[::-1],'#aebfc2')
    line(chain,'#839ca4',1)
    poly([headlocal(head,-16,-22),headlocal(head,5,-25),headlocal(head,18,-15),headlocal(head,19,2),headlocal(head,27,9),headlocal(head,18,12),headlocal(head,15,25),headlocal(head,0,29),headlocal(head,-15,19)],PAPER)
    poly([headlocal(head,-18,-21),headlocal(head,4,-25),headlocal(head,10,-19),headlocal(head,-11,-14),headlocal(head,-13,11),headlocal(head,-20,15)],'#aebfc2')
    line([headlocal(head,5,5),headlocal(head,20,4),headlocal(head,21,12),headlocal(head,7,13),headlocal(head,5,5)],INK,1.7)
    line([headlocal(head,-7,7),headlocal(head,5,8)],INK,1.2)
    line([headlocal(head,16,19),headlocal(head,22,18)],INK,1)

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
        'phase':stage(t),'hair_lengths':[distance(a,b) for a,b in zip(chain,chain[1:])], 'hair_tip':chain[-1],'hair_angles':corrected_angles,'separation_adjustments':separation_adjustments,'head':head,'chest':chest,
    }
    return im.resize((SIZE,SIZE),Image.Resampling.LANCZOS),metrics


def save_gif(frames, path, durations):
    frames[0].save(path,save_all=True,append_images=frames[1:],duration=durations,loop=0,optimize=False,disposal=2)


def embedded_sheet(frames):
    sheet=Image.new('RGB',(512*len(frames),512),PAPER)
    for i,f in enumerate(frames):sheet.paste(f,(i*512,0))
    data=io.BytesIO();sheet.save(data,format='PNG')
    return 'data:image/png;base64,'+base64.b64encode(data.getvalue()).decode()


def main():
    prepare_hair()
    spec=importlib.util.spec_from_file_location('baseline',OUT.parent/'constraint-trial-2026-09-21/make_trial.py')
    baseline=importlib.util.module_from_spec(spec);spec.loader.exec_module(baseline)
    groups=[[],[],[]];records=[]
    for i in range(65):
        t=i/25
        a,_=baseline.render(t)
        b,_=render(t,False)
        c,m=render(t,True)
        for group,img in zip(groups,[a,b,c]):group.append(img)
        records.append(m)
    for name,frames in zip(['baseline','segmented_rigid_hair','overlap_hair'],groups):
        save_gif(frames,OUT/(name+'.gif'),40)
    sheet=Image.new('RGB',(256*4,280*3),PAPER)
    sd=ImageDraw.Draw(sheet)
    selected=[13,21,25,31]
    for row,frames in enumerate(groups):
        for col,i in enumerate(selected):
            x,y=col*256,row*280
            sheet.paste(frames[i].resize((256,256),Image.Resampling.LANCZOS),(x,y))
            sd.text((x+12,y+258),f'{"ABC"[row]} / {i/25:.2f}s',fill=INK)
    sheet.save(OUT/'contact_sheet.jpg',quality=94)
    check={
        'source':'Local procedural spring/IK study. Not an execution of the researched GitHub models.',
        'samples_per_variant':65,'fps':25,'duration_ms':2600,
        'hair_segment_lengths':HAIR_LENGTHS,
        'max_hair_length_error':max(abs(v-target) for m in records for v,target in zip(m['hair_lengths'],HAIR_LENGTHS)),
        'max_arm_length_error':max(abs(v-58) for m in records for p in m['arm_lengths'] for v in p),
        'max_leg_length_error':max(abs(v-target) for m in records for p in m['leg_lengths'] for v,target in zip(p,[87,85])),
        'max_hammer_length_error':max(abs(m['hammer_length']-230) for m in records),
        'max_grip_spacing_error':max(abs(m['grip_distance']-44) for m in records),
        'max_hair_angle_degrees':max(abs(math.degrees(v)) for m in records for v in m['hair_angles']),
        'frames_with_hair_separation_correction':sum(m['separation_adjustments']>0 for m in records),
        'spring_periodic_state_error_rad':max(abs(a-b) for a,b in zip(HAIR[0],HAIR[-1])),
        'sample_last_to_first_hair_tip_distance':distance(records[0]['hair_tip'],records[-1]['hair_tip']),
        'notes':['Conservative side-view endpoint separation only; not continuous mesh/weapon collision.','Hair-only control is B versus C. A versus B also changes head and hair drawing.','Baseline timing, weapon trajectory, camera and fixed foot targets retained.','No claim of a physically accurate center of mass or production animation.']
    }
    (OUT/'verification.json').write_text(json.dumps(check,indent=2)+'\n')
    data={'images':[embedded_sheet(fs) for fs in groups],'phases':[m['phase'] for m in records]}
    html='''<!doctype html><html lang="zh-CN" translate="no"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>动作实验 03 · 躯干与头发跟随</title><style>
*{box-sizing:border-box}body{background:#eae8e0;color:#283644;margin:0;font:15px/1.65 system-ui}main{max-width:1360px;padding:30px;margin:auto}h1{font-size:27px;margin:6px 0}p{color:#617071;margin:4px 0 18px}.eyebrow{font-size:12px;letter-spacing:2px}.controls{display:flex;gap:16px;align-items:center;margin:20px 0;flex-wrap:wrap}button,select{font:inherit;padding:7px 14px;border:1px solid #9ca9a8;border-radius:5px;background:#fff;color:#283644}input{flex:1;min-width:200px;accent-color:#497984}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}.card{background:#f6f3eb;border:1px solid #cfd4cc;border-radius:9px;overflow:hidden}.label{padding:14px 16px 0;font-weight:650}.sub{padding:0 16px;font-size:12px;color:#667571}canvas{width:100%;height:auto;display:block}.footer{font:12px monospace;padding:12px 16px;border-top:1px solid #deded4}.notes{margin-top:22px;max-width:1040px}.notes li{margin:5px 0}@media(max-width:800px){.grid{grid-template-columns:1fr}main{padding:16px}}
</style><main><div class="eyebrow">MOTION STUDY / 03</div><h1>躯干与头发跟随 · 同步对照</h1><p>同一段 2.6 秒动作、同一镜头和锤子轨迹。先看 B/C，判断头发跟随是否真正有帮助。</p>
<div id="status">加载画面…</div><div class="controls"><button id="play">暂停</button><button id="restart">从头播放</button><label>速度 <select id="speed"><option value=".5">0.5×</option><option value="1" selected>1×</option></select></label><label for="seek">停帧</label><input id="seek" type="range" min="0" max="64" step="1" value="0"><span id="time">0.00s</span></div>
<div class="grid"><section class="card"><div class="label">A · 上一版</div><div class="sub">刚性躯干、刚性头发</div><canvas id="a" width="512" height="512"></canvas><div class="footer">保留上一版画面作基线</div></section><section class="card"><div class="label">B · 分段躯干</div><div class="sub">腰胸分开旋转、头部减少跟倾</div><canvas id="b" width="512" height="512"></canvas><div class="footer">头发仍固定，观察身体调整</div></section><section class="card"><div class="label">C · 增加头发跟随</div><div class="sub">与 B 同姿势，增加四段阻尼跟随</div><canvas id="c" width="512" height="512"></canvas><div class="footer">观察甩动、回落和收招稳定</div></section></div>
<div class="notes"><strong>这次能验证什么</strong><ul><li>B/C 唯一变化是头发跟随；A/B 还改变了头部和发束画法，不是纯躯干消融。</li><li>三栏同步停帧；骨长、锤柄、双手握点与脚底位置仍受约束。</li><li>跟随是本地程序实验，没有运行调研中的模型。只有粗略躯干避让，没有完整发束碰撞、衣物模拟或目标受击，不能据此认定动作达到游戏质量。</li></ul><p>完整资料见本目录 README.md。页面与画面均可离线打开。</p></div></main>
<script>const data=__DATA__;const $=id=>document.getElementById(id);const imgs=data.images.map(src=>{let i=new Image();i.src=src;return i});let playing=true,elapsed=0,last=0,frame=0;
function draw(){if(playing)frame=Math.min(64,Math.floor((elapsed%2600)/40));['a','b','c'].forEach((id,n)=>$(id).getContext('2d').drawImage(imgs[n],frame*512,0,512,512,0,0,512,512));$('seek').value=frame;$('time').textContent=(frame*.04).toFixed(2)+'s · '+data.phases[frame]}
function tick(t){if(last&&playing)elapsed+=(t-last)*Number($('speed').value);last=t;draw();requestAnimationFrame(tick)}
$('play').onclick=()=>{playing=!playing;if(playing)elapsed=frame*40;$('play').textContent=playing?'暂停':'播放';draw()};$('restart').onclick=()=>{elapsed=0;frame=0;draw()};$('seek').oninput=()=>{playing=false;frame=Number($('seek').value);$('play').textContent='播放';draw()};Promise.all(imgs.map(i=>i.decode())).then(()=>{$('status').textContent='画面已加载 · 三栏同步';requestAnimationFrame(tick)}).catch(e=>$('status').textContent='加载失败 '+e.message);
</script></html>'''
    (OUT/'comparison.html').write_text(html.replace('__DATA__',json.dumps(data)))
    print(json.dumps(check,indent=2))


if __name__=='__main__':main()
