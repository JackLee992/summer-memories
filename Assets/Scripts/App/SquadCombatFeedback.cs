using System.Collections.Generic;
using SummerMemories.Action3D.Squad;
using SummerMemories.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SummerMemories.App
{
    public class SquadCombatFeedback : MonoBehaviour
    {
        private class Popup{public Text text;public Vector3 position;public float age;}
        private readonly List<Popup> _popups=new List<Popup>();
        private SquadDemoDirector _app;private Transform _parent;private Image _flash;private float _flashTime;private int _damage;private SquadActor3D _lastActor;
        public void Init(SquadDemoDirector app,Transform parent)
        {
            _app=app;_parent=parent;
            var go=UIFactory.CreatePanel("DamageFlash",parent,new Color(.6f,.04f,.06f,0));_flash=go.GetComponent<Image>();_flash.raycastTarget=false;
            app.Session.Hit+=(position,amount,heavy)=>
            {
                var t=UIFactory.CreateText("HitNumber",_parent,amount.ToString("0"),heavy?54:39,heavy?new Color(1,.79f,.34f):Color.white,TextAnchor.MiddleCenter);
                var rt=(RectTransform)t.transform;rt.anchorMin=rt.anchorMax=new Vector2(0,0);rt.sizeDelta=new Vector2(130,70);
                _popups.Add(new Popup{text=t,position=position});
            };
        }
        private void LateUpdate()
        {
            if(_app==null)return;
            var actor=_app.Session.Active;
            if(actor!=_lastActor){_lastActor=actor;_damage=actor.DamageRevision;}
            if(actor.DamageRevision!=_damage){_damage=actor.DamageRevision;_flashTime=.2f;}
            _flashTime=Mathf.Max(0,_flashTime-Time.deltaTime);_flash.color=new Color(.7f,.02f,.035f,_flashTime*.7f);
            for(var i=_popups.Count-1;i>=0;i--)
            {
                var p=_popups[i];p.age+=Time.deltaTime;
                if(p.age>.7f){Destroy(p.text.gameObject);_popups.RemoveAt(i);continue;}
                var point=_app.Session.CameraRig.Camera.WorldToViewportPoint(p.position+Vector3.up*p.age*.7f);
                p.text.enabled=point.z>0;((RectTransform)p.text.transform).anchoredPosition=new Vector2(point.x*1920,point.y*1080);
                var color=p.text.color;color.a=1-p.age/.7f;p.text.color=color;
            }
        }
    }
}
