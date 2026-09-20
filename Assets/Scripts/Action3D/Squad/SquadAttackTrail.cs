using UnityEngine;

namespace SummerMemories.Action3D.Squad
{
    public class SquadAttackTrail : MonoBehaviour
    {
        private LineRenderer _line;private Material _material;private float _life=.25f;
        public static void Spawn(SquadActor3D actor,float range,bool heavy)
        {
            var fx=new GameObject("AttackArc").AddComponent<SquadAttackTrail>();fx.transform.position=actor.transform.position;
            fx._line=fx.gameObject.AddComponent<LineRenderer>();fx._material=new Material(Resources.Load<Shader>("Shaders/SquadTrail"));
            fx._line.sharedMaterial=fx._material;fx._line.positionCount=24;fx._line.startWidth=heavy?.16f:.085f;fx._line.endWidth=.015f;
            fx._line.startColor=heavy?new Color(1,.76f,.34f,.8f):new Color(.52f,.95f,1,.8f);fx._line.endColor=new Color(1,1,1,0);
            for(var i=0;i<24;i++)
            {
                var t=i/23f;var a=Mathf.Lerp(-65,65,t);
                fx._line.SetPosition(i,actor.transform.position+Vector3.up*(1.0f+Mathf.Sin(t*Mathf.PI)*.35f)+Quaternion.AngleAxis(a,Vector3.up)*actor.transform.forward*(range*.8f));
            }
        }
        private void Update(){_life-=Time.deltaTime;var c=_material.color;c.a=Mathf.Clamp01(_life*4);_material.color=c;if(_life<=0)Destroy(gameObject);}
        private void OnDestroy(){if(_material!=null)Destroy(_material);}
    }
}
