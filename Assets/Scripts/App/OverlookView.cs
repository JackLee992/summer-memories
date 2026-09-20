using System;
using System.Collections.Generic;
using SummerMemories.Action3D.Squad;
using SummerMemories.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SummerMemories.App
{
    public class OverlookView : MonoBehaviour
    {
        private SquadDemoDirector _app;
        private Transform _branchList,_eventList,_map;
        private Text _summary,_detail;
        private int _selectedBranch;
        private readonly Color _dark=new Color(.025f,.075f,.10f,.98f),_pale=new Color(.86f,.94f,.91f),_gold=new Color(.97f,.73f,.32f),_teal=new Color(.2f,.64f,.69f);
        private string L(string key)=>_app.Text.Get(key);
        private static void Rect(Transform transform,float x,float y,float width,float height)
        {var r=(RectTransform)transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
        private GameObject Box(Transform parent,string name,float x,float y,float w,float h,Color c)
        {var b=UIFactory.CreatePanel(name,parent,c);Rect(b.transform,x,y,w,h);b.GetComponent<Image>().raycastTarget=false;return b;}
        private Text Label(Transform parent,string text,int size,float x,float y,float w,float h,Color? color=null)
        {var t=UIFactory.CreateText("Label",parent,text,size,color??_pale,TextAnchor.UpperLeft);Rect(t.transform,x,y,w,h);return t;}
        private void Button(Transform parent,string text,float x,float y,float w,Action action)
        {UIFactory.CreateButton("Select",parent,text,22,new Color(.09f,.23f,.28f),_pale,action,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y-58),new Vector2(x+w,-y));}
        private Transform Scroll(Transform parent,string name,float x,float y,float w,float h)
        {
            var viewport=Box(parent,name,x,y,w,h,new Color(.02f,.045f,.065f,.75f));viewport.GetComponent<Image>().raycastTarget=true;
            viewport.AddComponent<RectMask2D>();var scroll=viewport.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(viewport.transform,false);Rect(content.transform,0,0,w,h);
            scroll.viewport=(RectTransform)viewport.transform;scroll.content=(RectTransform)content.transform;return content.transform;
        }
        public void Init(SquadDemoDirector app)
        {
            _app=app;var root=Box(transform,"Archive",50,45,1820,990,_dark).transform;
            Label(root,L("st.overlook.title"),43,32,24,1300,65,_gold);
            Button(root,L("st.button.resume"),1430,26,350,_app.Resume);
            Label(root,L("st.overlook.subtitle"),22,32,93,1730,45);
            Label(root,L("st.overlook.branches"),24,32,156,320,40,_teal);
            Label(root,L("st.overlook.events"),24,410,156,720,40,_teal);
            Label(root,L("st.overlook.map"),24,1260,156,480,40,_teal);
            _branchList=Scroll(root,"Branches",32,207,345,700);
            _eventList=Scroll(root,"Events",410,275,790,406);
            _summary=Label(root,"",23,410,205,790,65,_gold);
            _detail=Label(root,"",23,410,712,790,232);
            _map=Box(root,"Map",1270,210,470,680,new Color(.08f,.14f,.17f)).transform;
            Label(root,L("st.overlook.legend"),20,1260,906,510,60);
            var archive=app.Session.Archive;((RectTransform)_branchList).sizeDelta=new Vector2(345,Mathf.Max(700,archive.branches.Count*76));
            foreach(var branch in archive.branches)
            {
                var index=branch.id;
                Button(_branchList,$"{L("st.overlook.branch")} {index+1}   {L(branch.outcome)}",8,index*76+8,327,()=>Select(index));
            }
            Select(Mathf.Max(0,archive.branches.Count-1));
        }
        private static string Stamp(float t)=>$"{(int)t/60:00}:{(int)t%60:00}";
        private void Select(int index)
        {
            _selectedBranch=index;var b=_app.Session.Archive.branches[index];Clear(_eventList);
            _summary.text=string.Format(L("st.overlook.summary"),index+1,L(b.anchor),Stamp(b.elapsed),b.parentId<0?"—":(b.parentId+1).ToString(),Stamp(b.forkTime));
            ((RectTransform)_eventList).sizeDelta=new Vector2(790,Mathf.Max(406,b.events.Count*72));
            for(var i=0;i<b.events.Count;i++)
            {
                var captured=i;var e=b.events[i];
                Button(_eventList,Stamp(e.time)+"    "+L("st.event."+e.kind)+" · "+L(e.detailKey),8,8+i*72,765,()=>ShowEvent(captured));
            }
            ShowEvent(b.events.Count-1);
        }
        private void ShowEvent(int index)
        {
            var b=_app.Session.Archive.branches[_selectedBranch];Clear(_map);
            var unique=new HashSet<Vector3>(b.route);
            foreach(var point in unique)MapDot(point,13,_teal);
            if(index<0){_detail.text=L("st.overlook.empty");return;}
            var e=b.events[index];var lines=new List<string>{Stamp(e.time)+"   "+L("st.event."+e.kind)+" · "+L(e.detailKey),string.Format(L("st.overlook.position"),e.position.x,e.position.z)};
            foreach(var a in e.party)
            {
                var config=_app.Session.Config.Member(a.id);
                lines.Add(L(a.ryunosuke?"st.character.ryunosuke":config.nameKey)+$"   {a.hp:0}/{config.hp:0}   "+string.Format(L("st.overlook.cooldown"),a.hairCooldown));
                MapDot(a.position,9,_pale);
            }
            MapDot(e.position,20,_gold);_detail.text=string.Join("\n",lines);
        }
        private void MapDot(Vector3 point,float size,Color c)
        {
            // Fixed geographic projection, independent of the live camera and current branch.
            var x=Mathf.InverseLerp(-13,13,point.x)*440+15;var y=(1-Mathf.InverseLerp(0,62,point.z))*650+15;
            Box(_map,"Observed",x-size/2,y-size/2,size,size,c);
        }
        private static void Clear(Transform parent){foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
    }
}
