using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SummerMemories.Action3D.Squad
{
    public class SquadWorld3D : MonoBehaviour
    {
        public readonly Dictionary<string,GameObject> Markers = new Dictionary<string,GameObject>();
        private SquadBody _npcBody;
        private readonly List<Material> _materials = new List<Material>();
        private Material Mat(float r,float g,float b) {var m=SquadVisual.Material(new Color(r,g,b));_materials.Add(m);return m;}
        private GameObject Box(string id,Vector3 pos,Vector3 size,Material mat,bool solid=false) => SquadVisual.Shape(id,transform,PrimitiveType.Cube,pos,size,mat,solid);
        public void Build(SquadConfig config)
        {
            var sun=new GameObject("SummerSun").AddComponent<Light>();sun.transform.SetParent(transform,false);sun.type=LightType.Directional;sun.intensity=.85f;sun.color=new Color(1,.93f,.78f);sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.72f;sun.shadowBias=.025f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.28f,.38f,.46f);RenderSettings.ambientEquatorColor=new Color(.18f,.23f,.28f);RenderSettings.ambientGroundColor=new Color(.12f,.11f,.095f);QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=90;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.antiAliasing=4;
            var sand=Mat(.75f,.73f,.61f);var stone=Mat(.40f,.48f,.48f);var white=Mat(.87f,.85f,.73f);
            sand.SetFloat("_SurfaceDetail",1);stone.SetFloat("_SurfaceDetail",1);white.SetFloat("_SurfaceDetail",2);
            var skybox=new Material(Resources.Load<Shader>("Shaders/SquadSky"));_materials.Add(skybox);RenderSettings.skybox=skybox;
            var sea=new Material(Resources.Load<Shader>("Shaders/SquadWater"));_materials.Add(sea);var wood=Mat(.41f,.26f,.15f);var gold=Mat(.92f,.65f,.23f);var blue=Mat(.15f,.7f,.77f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.53f,.72f,.75f);RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.004f;
            var ocean=SquadVisual.Shape("Sea",transform,PrimitiveType.Plane,new Vector3(0,-.58f,45),new Vector3(60,1,60),sea);
            var sky=Mat(.86f,.94f,.96f);var green=Mat(.22f,.40f,.25f);var leaves=Mat(.32f,.53f,.27f);
            for(var i=0;i<18;i++)
            {
                var side=i%2==0?-1:1;var p=new Vector3(side*(52+i*9),-3,70+i*16);
                SquadVisual.Shape("Island",transform,PrimitiveType.Sphere,p,new Vector3(38+i*3,14+i%3*9,35),green);

            }
            for(var i=0;i<9;i++)
            {
                var p=new Vector3(-14.5f-(i%2)*3,0,3+i*7);
                SquadVisual.Shape("TreeTrunk",transform,PrimitiveType.Cylinder,p+Vector3.up*1.6f,new Vector3(.25f,1.6f,.25f),wood);
                for(var j=0;j<3;j++)SquadVisual.Shape("TreeCrown",transform,PrimitiveType.Sphere,p+new Vector3((j-1)*.8f,3.4f+j*.45f,0),new Vector3(2.7f,2.4f,2.6f),j%2==0?leaves:green);
            }
            Box("CoastalPath",new Vector3(0,-.25f,config.world.length*.5f),new Vector3(config.world.width,.5f,config.world.length+12),sand,true);
            foreach(var s in config.world.solids) Box(s.id,SquadConfig.Position(s.position),SquadConfig.Position(s.size),stone,true);
            // Small coastal details are decorative and do not determine gameplay collision.
            for(var z=0;z<config.world.length;z+=4)
            {
                Box("Paving",new Vector3(0,.012f,z),new Vector3(7,.025f,3.88f),white);
                foreach(var x in new[]{-11f,11f})
                {
                    Box("SeaWall",new Vector3(x,.42f,z),new Vector3(.35f,.85f,3.9f),stone);
                    Box("Post",new Vector3(x,1.05f,z),new Vector3(.25f,1.4f,.25f),white);
                }
                if(z%8==0)
                {
                    Box("LampPost",new Vector3(9.5f,1.65f,z),new Vector3(.08f,3.3f,.08f),wood);
                    Box("LampCap",new Vector3(9.5f,3.4f,z),new Vector3(.55f,.14f,.55f),stone);
                    Box("Lamp",new Vector3(9.5f,3.17f,z),new Vector3(.33f,.32f,.33f),white);
                    Box("Bench",new Vector3(-9,.52f,z),new Vector3(2,.13f,.6f),wood);
                    Box("BenchBack",new Vector3(-9,.95f,z-.28f),new Vector3(2,.48f,.08f),wood);
                    foreach(var leg in new[]{-0.8f,.8f})Box("BenchLeg",new Vector3(-9+leg,.25f,z),new Vector3(.12f,.5f,.4f),stone);
                }
            }
            var house=Resources.Load<GameObject>("Art/Models/st_house_v02");
            var boat=Resources.Load<GameObject>("Art/Models/st_boat_v02");
            for(var i=0;i<8;i++)
            {
                var h=Instantiate(house,transform,false);h.transform.position=new Vector3(-15.8f-i%2*.8f,0,12+i*7);h.transform.rotation=Quaternion.Euler(0,90,0);UseSurface(h);
            }
            for(var i=0;i<4;i++)
            {
                var b=Instantiate(boat,transform,false);b.transform.position=new Vector3(18+i%2*7,-.5f,9+i*13);b.transform.rotation=Quaternion.Euler(0,25+i*19,0);UseSurface(b);
            }
            // A working harbour edge supplies scale and flanking landmarks around the encounter.
            var rope=Mat(.56f,.48f,.32f);var iron=Mat(.16f,.24f,.27f);
            for(var i=0;i<8;i++)
            {
                var p=new Vector3(7.6f,0,5+i*7);
                SquadVisual.Shape("MooringBollard",transform,PrimitiveType.Cylinder,p+Vector3.up*.32f,new Vector3(.28f,.32f,.28f),iron);
                SquadVisual.Shape("BollardCap",transform,PrimitiveType.Cylinder,p+Vector3.up*.62f,new Vector3(.44f,.07f,.44f),iron);
                for(var j=0;j<3;j++)SquadVisual.Shape("CoiledRope",transform,PrimitiveType.Cylinder,p+new Vector3(.55f,.05f+j*.045f,0),new Vector3(.58f,.022f,.58f),rope);
                if(i%2==0)
                {
                    Box("FishingCrate",p+new Vector3(1.2f,.35f,1.3f),new Vector3(.85f,.7f,.8f),wood);
                    for(var j=0;j<4;j++)Box("CrateSlat",p+new Vector3(1.2f,.1f+j*.17f,1.72f),new Vector3(.86f,.055f,.04f),rope);
                }
            }
            for(var i=0;i<4;i++)
            {
                var p=new Vector3(-9.4f,0,30+i*6);
                SquadVisual.Shape("HarbourBarrel",transform,PrimitiveType.Cylinder,p+Vector3.up*.53f,new Vector3(.8f,.53f,.8f),wood);
                for(var y=.2f;y<1;y+=.6f)SquadVisual.Shape("BarrelBand",transform,PrimitiveType.Cylinder,p+Vector3.up*y,new Vector3(.83f,.035f,.83f),iron);
            }
            foreach(var f in config.scanTemplates)
            {
                if(!string.IsNullOrEmpty(f.attachToId))continue;
                var size=SquadConfig.Position(f.sizeMeters);var p=SquadConfig.Position(f.spawn)+Vector3.up*size.y*.5f;
                SquadVisual.Shape(f.id,transform,f.id.EndsWith("stone")?PrimitiveType.Sphere:PrimitiveType.Cube,p,size,wood,true);
                Beacon(f.id+"_scan",SquadConfig.Position(f.spawn),blue);
            }
            foreach(var clue in config.interactions)
            {
                Markers[clue.id]=Beacon(clue.id,SquadConfig.Position(clue.spawn),gold);
                if(clue.reward=="pipe")
                {
                    var pipe=SquadVisual.Shape("PickupPipe",Markers[clue.id].transform,PrimitiveType.Cylinder,new Vector3(0,.7f,0),new Vector3(.055f,.5f,.055f),stone);pipe.transform.localRotation=Quaternion.Euler(45,0,25);
                }
                if(clue.id.Contains("shell"))SquadVisual.Shape("Shell",Markers[clue.id].transform,PrimitiveType.Sphere,new Vector3(0,.2f,0),new Vector3(.45f,.20f,.38f),white);
            }
            Beacon("Encounter",SquadConfig.Position(config.world.encounterGate),Mat(.8f,.3f,.28f));
            Beacon("Exit",SquadConfig.Position(config.world.exit),blue);
            var npc=new GameObject("st_mio").transform;npc.SetParent(transform,false);npc.position=SquadConfig.Position(config.world.npc);npc.rotation=Quaternion.Euler(0,180,0);_npcBody=new SquadBody(npc,false);
            Physics.SyncTransforms();
        }
        private void UseSurface(GameObject go)
        {
            foreach(var r in go.GetComponentsInChildren<Renderer>())
            {
                var materials=r.sharedMaterials;
                for(var i=0;i<materials.Length;i++){var m=SquadVisual.Material(materials[i].color);_materials.Add(m);materials[i]=m;}
                r.sharedMaterials=materials;
            }
        }
        private GameObject Beacon(string id,Vector3 position,Material material)
        {
            var root=new GameObject(id);root.transform.SetParent(transform,false);root.transform.localPosition=position;
            SquadVisual.Shape("GroundRing",root.transform,PrimitiveType.Cylinder,new Vector3(0,.05f,0),new Vector3(.8f,.04f,.8f),material);
            var shard=SquadVisual.Shape("Signal",root.transform,PrimitiveType.Cube,new Vector3(0,2.3f,0),Vector3.one*.22f,material);
            shard.transform.localRotation=Quaternion.Euler(45,30,45);return root;
        }
        public bool Visible(Vector3 from,Vector3 to) => !Physics.Linecast(from+Vector3.up*.25f,to+Vector3.up*.25f,1,QueryTriggerInteraction.Ignore);
        public void RestoreMarkers(HashSet<string> collected) { foreach(var pair in Markers) pair.Value.SetActive(!collected.Contains(pair.Key)); }
        private void OnDestroy(){_npcBody?.Dispose();foreach(var m in _materials)Destroy(m);}
    }
}
