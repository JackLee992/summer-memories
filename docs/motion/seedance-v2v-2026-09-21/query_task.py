"""Query existing paid task; never resubmit. Keep provider URLs in ignored Builds/."""
import subprocess,json,time,shutil
from pathlib import Path
from datetime import datetime,timezone
out=Path(__file__).resolve().parent
repo=out.parents[2];private=repo/'Builds/seedance-v2v-2026-09-21';private.mkdir(exist_ok=True,parents=True)
m=json.loads((out/'generation.json').read_text());sid=m['submission']['submit_id']
r=subprocess.run(['dreamina','query_result','--submit_id',sid,'--download_dir',str(private/'result')],capture_output=True,text=True)
(private/'query-latest.stdout.txt').write_text(r.stdout);(private/'query-latest.stderr.txt').write_text(r.stderr)
try:d=json.loads(r.stdout)
except ValueError:
 print('Unparsed response saved locally; do not resubmit. Exit',r.returncode);raise SystemExit(1)
s={k:d[k] for k in ['submit_id','gen_status','credit_count','fail_reason','message','code','progress'] if k in d}
if isinstance(d.get('queue_info'),dict):s['queue_info']={k:v for k,v in d['queue_info'].items() if k in ['queue_idx','queue_status','priority']}
s['queried_at_utc']=datetime.now(timezone.utc).isoformat()
m['status']=d.get('gen_status',m['status']);m.setdefault('queries',[]).append(s)
for q in m['queries']:
 if isinstance(q.get('queue_info'),dict):q['queue_info']={k:v for k,v in q['queue_info'].items() if k in ['queue_idx','queue_status','priority']}
(out/'generation.json').write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(s,ensure_ascii=False))
print('Response keys:',list(d.keys()))
if m['status']=='success':
 files=list((private/'result').rglob('*.mp4'))
 print('Downloaded MP4 count:',len(files))
 if len(files)==1:
  shutil.copy2(files[0],out/'seedance_original.mp4');print('Saved seedance_original.mp4')
