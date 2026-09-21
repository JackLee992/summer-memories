using UnityEngine;

namespace SummerMemories.Action3D.Squad
{
    // A single illustrated cutout with small pose accents; not a claimed animation sheet.
    public sealed class SquadSpriteBody : MonoBehaviour
    {
        public Material Material {get;private set;}
        private Transform _picture,_owner,_pipe;
        private LineRenderer _ring;
        private Material _ringMaterial;
        private SquadSession3D _session;
        private SquadActor3D _actor;
        private float _height,_width,_time,_motion,_progress;
        private ActorAction _action;
        private bool _airborne;
        private float _facing=1;
        public void Init(Transform owner,SquadPresentation presentation,string character,bool shadow)
        {
            _owner=owner;_session=owner.GetComponentInParent<SquadSession3D>();_actor=owner.GetComponent<SquadActor3D>();
            var texture=Resources.Load<Texture2D>(presentation.spriteRoot+character);
            if(texture==null)throw new System.InvalidOperationException("Missing sprite "+character);
            Material=new Material(Resources.Load<Shader>("Shaders/SquadSprite")){mainTexture=texture,color=shadow?new Color(.13f,.17f,.23f):Color.white};
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="IllustratedCharacter";quad.transform.SetParent(transform,false);
            var collider=quad.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            var renderer=quad.GetComponent<MeshRenderer>();renderer.sharedMaterial=Material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            _picture=quad.transform;_height=presentation.spriteHeight;_width=_height*texture.width/texture.height;
            _ringMaterial=new Material(Resources.Load<Shader>("Shaders/SquadTrail"));
            _ring=new GameObject("SelectionRing").AddComponent<LineRenderer>();_ring.transform.SetParent(transform,false);_ring.sharedMaterial=_ringMaterial;_ring.loop=true;_ring.positionCount=40;_ring.startWidth=_ring.endWidth=.035f;
            var pipeMaterial=new Material(Resources.Load<Shader>("Shaders/SquadSprite")){color=new Color(.22f,.31f,.36f)};
            if(character=="st_shinpei"&&!shadow)
            {
                var pipe=GameObject.CreatePrimitive(PrimitiveType.Quad);pipe.name="EquippedPipe2D";pipe.transform.SetParent(_picture,false);_pipe=pipe.transform;
                var c=pipe.GetComponent<Collider>();c.enabled=false;Destroy(c);pipe.GetComponent<Renderer>().sharedMaterial=pipeMaterial;
                _pipe.localPosition=new Vector3(.27f,-.19f,-.01f);_pipe.localScale=new Vector3(.025f,.44f,1);_pipe.localRotation=Quaternion.Euler(0,0,-20);pipe.SetActive(false);
            }
            else Destroy(pipeMaterial);
        }
        public void Equip(bool equipped){if(_pipe!=null)_pipe.gameObject.SetActive(equipped);}
        public void Pose(float time,float movement,ActorAction action,float progress,bool airborne)
        {_time=time;_motion=movement;_action=action;_progress=progress;_airborne=airborne;}
        private void LateUpdate()
        {
            if(_session==null||_session.CameraRig==null)return;
            var camera=_session.CameraRig.Camera;var side=Vector3.Dot(_owner.forward,camera.transform.right);
            if(Mathf.Abs(side)>.15f)_facing=side>=0?1:-1;
            var lean=0f;var bob=Mathf.Abs(Mathf.Sin(_time*10))*.055f*_motion;var offset=0f;var scale=1f;
            if(_action==ActorAction.Attack||_action==ActorAction.Hair)
            {var swing=Mathf.Sin(_progress*Mathf.PI);lean=-_facing*swing*12;offset=_facing*swing*.13f;}
            if(_action==ActorAction.Dodge){lean=_facing*20;scale=.83f;}
            if(_action==ActorAction.Hurt)lean=_facing*-13;
            if(_action==ActorAction.Dead){lean=82;scale=.72f;}
            _picture.rotation=camera.transform.rotation*Quaternion.Euler(0,0,lean);
            _picture.position=_owner.position+camera.transform.up*(_height*scale*.5f+bob)+camera.transform.right*offset;
            _picture.localScale=new Vector3(_width*_facing,_height*scale,1);
            var selected=_actor!=null&&(_session.Tactical!=null&&_session.Tactical.Planning?_session.Tactical.Selected==_session.Party.IndexOf(_actor):_session.Active==_actor);
            _ring.enabled=_actor!=null&&_actor.Alive;
            if(_ring.enabled)
            {
                var color=selected?new Color(1,.8f,.34f,.9f):new Color(.22f,.82f,.82f,.35f);
                _ring.startColor=_ring.endColor=color;
                var p=_owner.position;p.y=.035f;
                for(var i=0;i<40;i++)_ring.SetPosition(i,p+Quaternion.Euler(0,i*9,0)*Vector3.forward*(selected?.53f:.38f));
            }
        }
        private void OnDestroy()
        {
            if(_pipe!=null)Destroy(_pipe.GetComponent<Renderer>().sharedMaterial);
            if(Material!=null)Destroy(Material);if(_ringMaterial!=null)Destroy(_ringMaterial);
        }
    }
}
