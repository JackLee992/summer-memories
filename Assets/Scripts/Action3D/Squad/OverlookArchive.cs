using System;
using System.Collections.Generic;
using UnityEngine;

namespace SummerMemories.Action3D.Squad
{
    [Serializable] public class OverlookArchive
    {
        public int version=1;
        public List<TimelineBranch> branches=new List<TimelineBranch>();
        public TimelineBranch Current => branches.Count>0?branches[branches.Count-1]:null;
        public void Begin(string anchor,int parent=-1,float forkTime=0)
        {
            branches.Add(new TimelineBranch{id=branches.Count,parentId=parent,forkTime=forkTime,anchor=anchor});
        }
        public void Observe(float dt,Vector3 position)
        {
            var branch=Current;if(branch==null)return;branch.elapsed+=dt;
            var cell=new Vector3(Mathf.Round(position.x/2)*2,0,Mathf.Round(position.z/2)*2);
            if(branch.route.Count==0||branch.route[branch.route.Count-1]!=cell)branch.route.Add(cell);
        }
        public void Record(string kind,string detail,SquadActor3D actor,List<SquadActor3D> party)
        {
            var b=Current;if(b==null)return;
            var entry=new TimelineEvent{time=b.elapsed,kind=kind,detailKey=detail,actorId=actor.Config.id,position=actor.transform.position};
            foreach(var a in party)entry.party.Add(a.Snapshot());
            b.events.Add(entry);
        }
    }
    [Serializable] public class TimelineBranch
    {
        public int id,parentId=-1;
        public float elapsed,forkTime;
        public string anchor,outcome="st.timeline.active";
        public List<Vector3> route=new List<Vector3>();
        public List<TimelineEvent> events=new List<TimelineEvent>();
    }
    [Serializable] public class TimelineEvent
    {
        public float time;
        public string kind,detailKey,actorId;
        public Vector3 position;
        public List<ActorSnapshot> party=new List<ActorSnapshot>();
    }
}
