using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SummerMemories.Action3D.Squad
{
    public enum TacticalAction { Move, Attack, Heavy, Skill }
    public sealed class SquadTactics : MonoBehaviour
    {
        public bool Planning {get;private set;}
        public int Selected {get;private set;}
        public TacticalAction Intent {get;private set;}=TacticalAction.Attack;
        private sealed class Command
        {
            public TacticalAction action;public ShadowEnemy3D target;public List<Vector3> path;public int next;
            public bool hold;public Vector3 destination;public float stuck,closest=float.MaxValue;
        }
        private SquadSession3D _session;
        private readonly Dictionary<int,Command> _orders=new Dictionary<int,Command>();
        private LineRenderer[] _lines;
        private Material _material;
        public void Init(SquadSession3D session)
        {
            _session=session;_material=new Material(Resources.Load<Shader>("Shaders/SquadTrail"));
            _lines=new LineRenderer[session.Party.Count];
            for(var i=0;i<_lines.Length;i++)
            {
                var l=new GameObject("PlanRoute"+i).AddComponent<LineRenderer>();l.transform.SetParent(transform,false);l.sharedMaterial=_material;
                l.startWidth=l.endWidth=.045f;l.positionCount=0;l.enabled=false;_lines[i]=l;
            }
        }
        public void Toggle()
        {
            if(_session.Phase!=SquadPhase.Explore&&_session.Phase!=SquadPhase.Fight)return;
            Planning=!Planning;if(Planning){Selected=_session.ActiveIndex;Intent=TacticalAction.Attack;}
        }
        public void Close(){Planning=false;}
        public void Clear(){Planning=false;_orders.Clear();foreach(var l in _lines)l.enabled=false;}
        public void Cancel(int index){_orders.Remove(index);}
        public void Select(int index)
        {
            if(index<0||index>=_session.Party.Count)return;
            var actor=_session.Party[index];if(!actor.Alive||actor.Carried){_session.Notify("st.plan.unavailable");return;}
            Selected=index;Intent=TacticalAction.Attack;
            if(!Planning)_session.Switch(index);
        }
        public void SetIntent(TacticalAction action){Intent=action;}
        public bool HasOrder(int index)=>_orders.ContainsKey(index);
        public string OrderKey(int index)
        {
            if(!_orders.TryGetValue(index,out var o))return "st.plan.none";
            return o.hold?"st.plan.hold":"st.plan."+o.action.ToString().ToLowerInvariant();
        }
        public bool Issue(int index,Vector3 point,ShadowEnemy3D target=null,TacticalAction action=TacticalAction.Move)
        {
            if(index<0||index>=_session.Party.Count||(_session.Phase!=SquadPhase.Explore&&_session.Phase!=SquadPhase.Fight))return false;
            var actor=_session.Party[index];if(!actor.Alive||actor.Carried)return false;
            if(target!=null&&!target.Alive)return false;
            if(action!=TacticalAction.Move&&target==null)return false;
            if(action==TacticalAction.Skill&&!(actor.IsUshio||(actor.IsHizuru&&actor.Ryunosuke)))
            {_session.Notify("st.feedback.abilityUnavailable");return false;}
            if(actor.Form!=null&&target!=null){_session.Notify("st.plan.unavailable");return false;}
            var destination=target!=null?target.transform.position:point;
            var path=SquadPath25D.Find(actor,destination,_session.Config.world);
            if(path==null){_session.Notify("st.plan.blocked");return false;}
            _orders[index]=new Command{action=action,target=target,path=path,destination=destination};
            _session.Notify("st.plan.queued");return true;
        }
        public void ReadInput()
        {
            if(!Planning)Selected=_session.ActiveIndex;
            if(Planning)
            {
                for(var i=0;i<_session.Party.Count;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))Select(i);
                if(Input.GetKeyDown(KeyCode.E))Intent=TacticalAction.Skill;
                if(Input.GetKeyDown(KeyCode.K))Intent=TacticalAction.Heavy;
                if(Input.GetKeyDown(KeyCode.J))Intent=TacticalAction.Attack;
                // Party-wide orders replace individual deployment without advancing the paused world.
                if(Input.GetKeyDown(KeyCode.Alpha4)){_session.SetOrder(AllyOrder.Follow);Planning=true;}
                if(Input.GetKeyDown(KeyCode.Alpha5)){_session.SetOrder(AllyOrder.Hold);Planning=true;}
                if(Input.GetKeyDown(KeyCode.Alpha6)){_session.SetOrder(AllyOrder.Focus);Planning=true;}
            }
            if(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject())return;
            if(Input.GetMouseButtonDown(0))
            {
                var best=48f;var index=-1;var camera=_session.CameraRig.Camera;
                for(var i=0;i<_session.Party.Count;i++)
                {
                    var p=camera.WorldToScreenPoint(_session.Party[i].transform.position+camera.transform.up*1.1f);
                    var d=Vector2.Distance(Input.mousePosition,p);if(p.z>0&&d<best){best=d;index=i;}
                }
                if(index>=0)Select(index);
            }
            if(Input.GetMouseButtonDown(1))
            {
                var camera=_session.CameraRig.Camera;ShadowEnemy3D target=null;var best=48f;
                foreach(var e in _session.Enemies)
                {
                    var p=camera.WorldToScreenPoint(e.transform.position+camera.transform.up*1.1f);var d=Vector2.Distance(Input.mousePosition,p);
                    if(e.Alive&&p.z>0&&d<best){target=e;best=d;}
                }
                if(target!=null)Issue(Selected,target.transform.position,target,Planning?Intent:TacticalAction.Attack);
                else
                {
                    var ray=camera.ScreenPointToRay(Input.mousePosition);
                    if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out var distance))Issue(Selected,ray.GetPoint(distance));
                }
            }
        }
        public bool TickActor(int index,float dt)
        {
            if(!_orders.TryGetValue(index,out var order))return false;
            var actor=_session.Party[index];if(!actor.Alive||actor.Carried){Cancel(index);return false;}
            if(order.hold||!actor.Free)return true;
            if(order.target!=null)
            {
                if(!order.target.Alive){order.hold=true;return true;}
                var d=order.target.transform.position-actor.transform.position;d.y=0;
                var range=order.action==TacticalAction.Skill&&actor.IsUshio?5.5f:2.05f;
                if(d.magnitude<range&&_session.World.Visible(actor.transform.position,order.target.transform.position))
                {
                    if(d.sqrMagnitude>.01f)actor.transform.rotation=Quaternion.LookRotation(d);
                    if(actor.Attack(order.action==TacticalAction.Skill,order.action==TacticalAction.Heavy)&&order.action!=TacticalAction.Attack)order.hold=true;
                    return true;
                }
                if(Vector3.Distance(order.target.transform.position,order.destination)>1.3f)
                {
                    order.destination=order.target.transform.position;order.path=SquadPath25D.Find(actor,order.destination,_session.Config.world);order.next=0;order.stuck=0;order.closest=float.MaxValue;
                    if(order.path==null){order.hold=true;_session.Notify("st.plan.blocked");return true;}
                }
            }
            if(order.next>=order.path.Count){order.hold=true;return true;}
            var toward=order.path[order.next]-actor.transform.position;toward.y=0;
            if(toward.magnitude<.25f){order.next++;order.closest=float.MaxValue;order.stuck=0;return true;}
            if(toward.magnitude<order.closest-.03f){order.closest=toward.magnitude;order.stuck=0;}else order.stuck+=dt;
            if(order.stuck>1.6f){order.hold=true;_session.Notify("st.plan.blocked");return true;}
            actor.Move(toward.normalized,false,dt);return true;
        }
        private void LateUpdate()
        {
            if(_session==null)return;
            for(var i=0;i<_lines.Length;i++)
            {
                var l=_lines[i];l.enabled=false;
                if(!_orders.TryGetValue(i,out var order)||order.hold||_session.Paused)continue;
                var points=new List<Vector3>{_session.Party[i].transform.position+Vector3.up*.09f};
                if(order.target!=null&&order.target.Alive)points.Add(order.target.transform.position+Vector3.up*.09f);
                else for(var n=order.next;n<order.path.Count;n++)points.Add(order.path[n]+Vector3.up*.09f);
                if(points.Count<2)continue;
                l.enabled=true;l.positionCount=points.Count;l.SetPositions(points.ToArray());
                var c=order.target!=null?new Color(1,.56f,.35f,.85f):i==1?new Color(.5f,1,.84f,.8f):new Color(1,.83f,.38f,.85f);
                l.startColor=l.endColor=c;
            }
        }
        private void OnDestroy(){if(_material!=null)Destroy(_material);}
    }

    internal static class SquadPath25D
    {
        // Bounded ground grid; capsule clearance follows the current copied form.
        public static List<Vector3> Find(SquadActor3D actor,Vector3 destination,WorldConfig world)
        {
            const float cell=.75f;var minX=-world.width*.5f+.65f;var minZ=-3f;
            var width=Mathf.FloorToInt((world.width-1.3f)/cell)+1;var height=Mathf.FloorToInt((world.length+4)/cell)+1;
            if(Mathf.Abs(destination.x)>world.width*.5f-.6f||destination.z<minZ||destination.z>world.length+1)return null;
            var radius=actor.Controller.radius+.035f;var bodyHeight=actor.Controller.height;
            System.Func<Vector3,bool> clear=p=>!Physics.CheckCapsule(p+Vector3.up*(radius+.06f),p+Vector3.up*Mathf.Max(radius+.06f,bodyHeight-radius),radius,1,QueryTriggerInteraction.Ignore);
            destination.y=0;if(!clear(destination))return null;
            System.Func<Vector3,int> id=p=>Mathf.Clamp(Mathf.RoundToInt((p.z-minZ)/cell),0,height-1)*width+Mathf.Clamp(Mathf.RoundToInt((p.x-minX)/cell),0,width-1);
            System.Func<int,Vector3> pos=n=>new Vector3(minX+n%width*cell,0,minZ+n/width*cell);
            var start=id(actor.transform.position);var end=id(destination);var count=width*height;
            var previous=new int[count];var visited=new bool[count];var queue=new Queue<int>();var walkable=new Dictionary<int,bool>();
            queue.Enqueue(start);visited[start]=true;previous[start]=-1;
            var offsets=new[]{new Vector2Int(1,0),new Vector2Int(-1,0),new Vector2Int(0,1),new Vector2Int(0,-1)};
            while(queue.Count>0)
            {
                var n=queue.Dequeue();if(n==end)break;var x=n%width;var z=n/width;
                foreach(var offset in offsets)
                {
                    var nx=x+offset.x;var nz=z+offset.y;if(nx<0||nx>=width||nz<0||nz>=height)continue;var next=nz*width+nx;if(visited[next])continue;
                    if(!walkable.TryGetValue(next,out var valid)){valid=clear(pos(next));walkable[next]=valid;}
                    if(!valid)continue;
                    var from=n==start?actor.transform.position:pos(n);from.y=0;var to=pos(next);
                    if(Physics.CapsuleCast(from+Vector3.up*(radius+.05f),from+Vector3.up*Mathf.Max(radius+.05f,bodyHeight-radius),radius,(to-from).normalized,Vector3.Distance(from,to),1,QueryTriggerInteraction.Ignore))continue;
                    visited[next]=true;previous[next]=n;queue.Enqueue(next);
                }
            }
            if(!visited[end])return null;var path=new List<Vector3>();
            for(var n=end;n!=start;n=previous[n])path.Add(pos(n));path.Reverse();path.Add(destination);return path;
        }
    }
}
