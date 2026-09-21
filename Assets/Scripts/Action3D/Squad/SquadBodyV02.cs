using System;
using System.Collections.Generic;
using UnityEngine;

namespace SummerMemories.Action3D.Squad
{
    /// <summary>Meter-scale Blender meshes, named pivots, replaceable pose layer.</summary>
    public class SquadBody
    {
        public readonly Transform Root, LeftArm, RightArm, LeftLeg, RightLeg;
        public readonly Material Skin, Cloth, Hair;
        private readonly List<Material> _materials=new List<Material>();
        private readonly Transform[] _locks;
        private readonly Quaternion _la,_ra,_ll,_rl;
        private readonly Transform _leftForearm,_rightForearm,_leftShin,_rightShin,_torso,_hips,_head;
        private readonly Dictionary<Transform,Quaternion> _rest=new Dictionary<Transform,Quaternion>();
        private float _gait,_movement,_lastPoseTime;
        private readonly bool _ushio;
        private readonly bool _shadow,_hizuru;
        private readonly Transform _weaponRoot;
        private readonly SquadSpriteBody _sprite;
        public SquadBody(Transform parent,bool ushio,bool shadow=false,bool hizuru=false,SquadPresentation presentation=null,string spriteId=null)
        {
            _shadow=shadow;_hizuru=hizuru;_ushio=ushio;
            Root=new GameObject("Body").transform;Root.SetParent(parent,false);
            if(presentation!=null && presentation.isometric)
            {
                _sprite=Root.gameObject.AddComponent<SquadSpriteBody>();
                _sprite.Init(parent,presentation,spriteId??("st_"+(ushio?"ushio":hizuru?"hizuru":"shinpei")),shadow);
                Skin=Cloth=Hair=_sprite.Material;return;
            }
            var key="Art/Models/st_"+(ushio?"ushio":hizuru?"hizuru":"shinpei")+"_v02";
            var prefab=Resources.Load<GameObject>(key);
            if(prefab==null)throw new InvalidOperationException("Missing model "+key);
            var model=UnityEngine.Object.Instantiate(prefab,Root,false);
            var cache=new Dictionary<string,Material>();
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var source=renderer.sharedMaterials;var dest=new Material[source.Length];
                for(var i=0;i<source.Length;i++)
                {
                    var name=source[i].name;
                    if(!cache.TryGetValue(name,out var material))
                    {
                        var color=source[i].color;
                        if(shadow)color=name.Contains("Eye")||name.Contains("Catch")?new Color(1,.25f,.43f):name.Contains("Skin")?new Color(.11f,.12f,.21f):new Color(.055f,.075f,.14f);
                        material=SquadVisual.Material(color);material.name=name;cache.Add(name,material);_materials.Add(material);
                    }
                    dest[i]=material;
                }
                renderer.sharedMaterials=dest;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;
            }
            Material Get(string needle){foreach(var p in cache)if(p.Key.StartsWith(needle))return p.Value;return _materials[0];}
            Skin=Get("Skin");Cloth=Get(hizuru?"Jacket":"Cloth");Hair=Get("Hair");
            Transform Find(string name){foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name==name)return t;throw new InvalidOperationException("Model pivot missing: "+name);}
            LeftArm=Find("LeftArm");RightArm=Find("RightArm");LeftLeg=Find("LeftLeg");RightLeg=Find("RightLeg");
            _leftForearm=Find("LeftForearm");_rightForearm=Find("RightForearm");_leftShin=Find("LeftShin");_rightShin=Find("RightShin");_torso=Find("TorsoPivot");_hips=Find("Hips");_head=Find("Head");
            foreach(var t in new[]{LeftArm,RightArm,LeftLeg,RightLeg,_leftForearm,_rightForearm,_leftShin,_rightShin,_torso,_hips,_head})_rest[t]=t.localRotation;
            _la=LeftArm.localRotation;_ra=RightArm.localRotation;_ll=LeftLeg.localRotation;_rl=RightLeg.localRotation;
            var locks=new List<Transform>();foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name.StartsWith("HairStrand"))locks.Add(t);_locks=locks.ToArray();
            if(!ushio&&!hizuru&&!shadow)
            {
                _weaponRoot=new GameObject("EquippedPipe").transform;_weaponRoot.SetParent(_rightForearm,false);
                var metal=SquadVisual.Material(new Color(.28f,.42f,.47f));_materials.Add(metal);
                SquadVisual.Shape("SteelPipe",_weaponRoot,PrimitiveType.Cylinder,new Vector3(0,-.49f,.045f),new Vector3(.045f,.40f,.045f),metal);
                _weaponRoot.gameObject.SetActive(false);
            }
        }
        public void Equip(bool equipped){if(_sprite!=null){_sprite.Equip(equipped);return;}if(_weaponRoot!=null)_weaponRoot.gameObject.SetActive(equipped);}
        private void Rotate(Transform joint,float x=0,float y=0,float z=0)
        {joint.localRotation=_rest[joint]*Quaternion.Euler(x,y,z);}
        private static float Ease(float t)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
        public void Pose(float time,float movement,ActorAction action,float progress,int combo=0,bool heavy=false,bool airborne=false)
        {
            if(_sprite!=null){_sprite.Pose(time,movement,action,progress,airborne);return;}
            var dt=Mathf.Clamp(time-_lastPoseTime,0,.05f);_lastPoseTime=time;
            _movement=Mathf.Lerp(_movement,movement,1-Mathf.Exp(-16*dt));_gait+=dt*(8.6f+_movement*2);
            var left=Mathf.Sin(_gait);var right=Mathf.Sin(_gait+Mathf.PI);var breathe=Mathf.Sin(time*2.1f);
            Rotate(LeftLeg,left*31*_movement);Rotate(RightLeg,right*31*_movement);
            Rotate(_leftShin,Mathf.Max(0,-left)*53*_movement+3);Rotate(_rightShin,Mathf.Max(0,-right)*53*_movement+3);
            Rotate(LeftArm,-left*24*_movement+3,0,5);Rotate(RightArm,-right*24*_movement+3,0,-5);
            Rotate(_leftForearm,-12-18*_movement);Rotate(_rightForearm,-16-18*_movement);
            Rotate(_hips,0,left*4*_movement,Mathf.Cos(_gait)*1.4f*_movement);
            Rotate(_torso,3*_movement, -left*6*_movement, breathe*.45f);
            Rotate(_head,-2*_movement,-breathe*.6f,0);
            Root.localPosition=Vector3.up*(Mathf.Abs(Mathf.Sin(_gait))* .025f*_movement+breathe*.003f);
            Root.localRotation=Quaternion.identity;
            if(_shadow){Rotate(_torso,10,-left*4*_movement,0);Rotate(_leftForearm,-27);Rotate(_rightForearm,-35);}
            if(action==ActorAction.Attack)
            {
                // The hit at 42% occurs on the fast swing, between a readable windup and recovery.
                var wind=Ease(progress/.30f);var hit=Ease((progress-.30f)/.16f);var settle=Ease((progress-.54f)/.46f);
                var side=combo%2==0?1:-1;
                var twist=Mathf.Lerp(-side*35*wind,side*49,hit)*(1-settle);
                Rotate(_hips,0,twist*.35f);Rotate(_torso,heavy?Mathf.Lerp(-12*wind,22,hit)*(1-settle):6*hit*(1-settle),twist,0);
                Rotate(RightArm,Mathf.Lerp(heavy?-145*wind:-60*wind,heavy?-30:-74,hit)*(1-settle),side*Mathf.Lerp(-55*wind,75,hit)*(1-settle),-12-18*wind*(1-settle));
                Rotate(_rightForearm,Mathf.Lerp(-90*wind,-10,hit)*(1-settle)-12);
                Rotate(LeftArm,-28*wind*(1-settle),-twist*.35f,12);
                Rotate(_leftForearm,-35-55*wind*(1-settle));
                Rotate(LeftLeg,-10*wind*(1-settle));Rotate(RightLeg,14*wind*(1-settle));
                Rotate(_leftShin,12+18*hit*(1-settle));Rotate(_rightShin,10);
                Root.localPosition+=Vector3.down*(heavy?.08f:.035f)*Mathf.Sin(progress*Mathf.PI);
            }
            else if(action==ActorAction.Hair)
            {
                var reach=Ease(progress/.38f)*(1-Ease((progress-.65f)/.35f));
                Rotate(_torso,6*reach,-18*reach);Rotate(LeftArm,-75*reach,0,20*reach);Rotate(RightArm,-85*reach,0,-12*reach);
                Rotate(_leftForearm,-32*(1-reach)-5);Rotate(_rightForearm,-45*(1-reach)-8);
            }
            else if(action==ActorAction.Dodge)
            {
                var dip=Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI);
                Root.localPosition=Vector3.down*(.09f+.19f*dip);Rotate(_torso,27,0,-8*dip);
                Rotate(LeftLeg,-42);Rotate(RightLeg,-12);Rotate(_leftShin,90);Rotate(_rightShin,65);
                Rotate(LeftArm,-42,0,18);Rotate(RightArm,-32,0,-18);Rotate(_leftForearm,-85);Rotate(_rightForearm,-85);
            }
            else if(action==ActorAction.Hurt)
            {Rotate(_torso,-20,12,4);Rotate(_head,15);Rotate(_leftForearm,-70);Rotate(_rightForearm,-65);}
            if(airborne && action!=ActorAction.Dead)
            {Rotate(LeftLeg,-38);Rotate(RightLeg,-15);Rotate(_leftShin,70);Rotate(_rightShin,95);Rotate(_torso,8);}
            if(action==ActorAction.Dead)
            {Root.localRotation=Quaternion.Euler(0,0,88);Root.localPosition=new Vector3(.65f,.12f,0);Rotate(_leftShin,36);Rotate(_rightShin,13);Rotate(_torso,12);}
            for(var i=0;i<_locks.Length;i++)
            {
                var attackWave=action==ActorAction.Hair&&_ushio?Mathf.Sin(progress*Mathf.PI)*32:0;
                _locks[i].localRotation=Quaternion.Euler(_movement*9+Mathf.Sin(time*2.3f+i*.45f)*(1.2f+_movement*2)+attackWave,0,Mathf.Sin(time*1.7f+i)*1.3f);
            }
        }
        public void Dispose(){foreach(var m in _materials)UnityEngine.Object.Destroy(m);}
    }
}
