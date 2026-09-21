from pathlib import Path
import base64

out=Path(__file__).resolve().parent
def video(name):
    return 'data:video/mp4;base64,'+base64.b64encode((out/name).read_bytes()).decode()

html='''<!doctype html><html lang="zh-CN" translate="no"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>教程实验 · 从 BVH 到可编辑挥锤</title>
<style>*{box-sizing:border-box}body{margin:0;background:#eeeee8;color:#263b43;font:15px/1.65 system-ui}main{max-width:1240px;margin:auto;padding:30px}h1{font-size:29px;margin:6px 0}p{color:#61716f;margin:4px 0 18px}.over{font-size:12px;letter-spacing:2px;color:#5c7c81}.grid{display:grid;grid-template-columns:1fr 1fr;gap:20px}.card{background:#fafaf4;border:1px solid #cdd3cd;border-radius:10px;overflow:hidden}.label{padding:15px 18px 0;font-weight:650}.sub{padding:0 18px 12px;color:#62716f;font-size:13px}video{width:100%;display:block;aspect-ratio:1;object-fit:contain;background:#aaa}.controls{display:flex;flex-wrap:wrap;gap:15px;align-items:center;margin:18px 0}button,select{font:inherit;border:1px solid #a4b2ad;padding:8px 14px;border-radius:6px;color:#29424b;background:#fff}button{cursor:pointer}input[type=range]{accent-color:#52777d;flex:1;min-width:240px}.evidence{display:flex;gap:18px;flex-wrap:wrap;margin:20px 0}.metric{padding:14px 20px;background:#dce5df;border-radius:8px}.metric b{display:block;font-size:21px}.notes{max-width:1040px}a{color:#245d6c}details{margin:22px 0}details video{max-width:520px}li{margin:5px 0}@media(max-width:720px){.grid{grid-template-columns:1fr}main{padding:18px}}</style>
<main><div class="over">TUTORIAL LAB / 04</div><h1>从生成动作到可编辑挥锤</h1><p>已经实际调用 MoMask、导入 Blender，并完成一段重新设计的关键姿势练习。右侧是我们重做的动作，不是模型自动修复。</p>
<div class="controls"><button id="play">播放对照</button><button id="reset">回到开头</button><label>速度 <select id="speed"><option value="1">1×</option><option value=".5">0.5×</option></select></label><label for="seek">停帧</label><input id="seek" type="range" min="0" max="59" step="1" value="0"><span id="frame">1 / 60</span></div>
<div class="grid"><section class="card"><div class="label">A · MoMask 原始动作</div><div class="sub">3秒 / 20fps / 22骨骼 · 未改动作，只加中性人偶观察</div><video id="raw" muted playsinline preload="auto" src="__RAW__"></video></section>
<section class="card"><div class="label">B · 按教程重做关键姿势</div><div class="sub">同一骨架 · 13个关键时点 · 双手IK与刚性锤子</div><video id="authored" muted playsinline preload="auto" src="__AUTHORED__"></video></section></div>
<div id="status">加载预览…</div><div class="evidence"><div class="metric"><b>18.03 / 9.72 秒</b>两次在线请求的实测返回时间</div><div class="metric"><b>0.22 米</b>人为设定的双手握点间距</div><div class="metric"><b>可修改 .blend</b>已重新打开并验证控制器编辑</div></div>
<div class="notes"><strong>实验结论</strong><ul><li>两次模型结果均未完成清晰挥锤。没有继续抽结果，也没有把模型失败隐藏掉。</li><li>B保留骨架比例，重新编排预备、挥击和收回；AI动作曲线没有伪装成可用成品。人偶仅用于动作检查。</li><li>脚踝位置与脚部方向受约束；手指、完整脚底接触、武器碰撞、角色原画和商业品质均未完成。</li><li>A/B同时改变动作和道具，属于结果对照，不是证明IK单独提升表演质量的消融实验。</li></ul>
<p><a href="hammer_lesson.blend">Blender 可编辑工程</a> · <a href="README.md">操作方法与验证记录</a> · <a href="../tutorial-learning-route-2026-09-21.md">四组教程与练习</a></p></div>
<details><summary>查看第二次生成的原始预览（举斧劈下提示，4秒）</summary><video muted playsinline controls preload="metadata" src="__SECOND__"></video><p>此结果也未达到单次挥重物的要求，保留作失败对照。</p></details>
</main><script>const $=id=>document.getElementById(id),a=$('raw'),b=$('authored');const vids=[a,b];let playing=false;
function stop(){playing=false;vids.forEach(v=>v.pause());$('play').textContent='播放对照'}
function seek(f){stop();vids.forEach(v=>v.currentTime=f/20);$('seek').value=f;$('frame').textContent=(f+1)+' / 60'}
$('play').onclick=async()=>{if(playing){stop();return}if(a.currentTime>=2.95)seek(0);b.currentTime=a.currentTime;try{await Promise.all(vids.map(v=>v.play()));playing=true;$('play').textContent='暂停'}catch(e){$('status').textContent='播放失败 '+e.message}};
$('reset').onclick=()=>seek(0);$('seek').oninput=()=>seek(Number($('seek').value));$('speed').onchange=()=>vids.forEach(v=>v.playbackRate=Number($('speed').value));a.onended=stop;
function update(){if(playing){const f=Math.min(59,Math.floor(a.currentTime*20));$('seek').value=f;$('frame').textContent=(f+1)+' / 60'}requestAnimationFrame(update)}requestAnimationFrame(update);
let loaded=0;vids.forEach(v=>v.addEventListener('loadeddata',()=>{if(++loaded===2)$('status').textContent='本地预览已加载 · 可播放、半速与停帧'}));</script></html>'''
html=html.replace('__RAW__',video('blender_raw.mp4')).replace('__AUTHORED__',video('blender_authored.mp4')).replace('__SECOND__',video('momask_second.mp4'))
(out/'comparison.html').write_text(html)
