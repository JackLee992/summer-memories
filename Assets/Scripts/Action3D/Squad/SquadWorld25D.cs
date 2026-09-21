using UnityEngine;
using UnityEngine.Rendering;

namespace SummerMemories.Action3D.Squad
{
    public partial class SquadWorld3D
    {
        private void Build25D(SquadConfig config)
        {
            var sun=new GameObject("AfternoonLight").AddComponent<Light>();sun.transform.SetParent(transform,false);sun.type=LightType.Directional;
            sun.color=new Color(1,.91f,.74f);sun.intensity=.85f;sun.transform.rotation=Quaternion.Euler(55,-28,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.55f;sun.shadowBias=.025f;
            RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.49f,.59f,.67f);RenderSettings.ambientEquatorColor=new Color(.34f,.40f,.43f);RenderSettings.ambientGroundColor=new Color(.20f,.24f,.24f);
            QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=65;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.antiAliasing=4;
            var pavement=Mat(.63f,.67f,.60f);pavement.SetFloat("_SurfaceDetail",2);
            var limestone=Mat(.56f,.60f,.55f);var edge=Mat(.36f,.45f,.44f);var soil=Mat(.34f,.42f,.28f);
            var timber=Mat(.39f,.28f,.18f);var pale=Mat(.9f,.86f,.69f);var roof=Mat(.17f,.32f,.38f);
            var sea=new Material(Resources.Load<Shader>("Shaders/SquadWater"));_materials.Add(sea);
            SquadVisual.Shape("TidalWater",transform,PrimitiveType.Plane,new Vector3(40,-.48f,32),new Vector3(18,1,18),sea);
            Box("IslandFoundation",new Vector3(-22,-1,30),new Vector3(70,1.4f,90),edge);
            Box("CoastalPath",new Vector3(0,-.20f,30),new Vector3(24,.4f,72),pavement,true);
            Box("VillageGround",new Vector3(-22,-.03f,30),new Vector3(20,.12f,85),soil);
            Box("Beach",new Vector3(13,-.35f,30),new Vector3(4,.1f,75),pale);
            foreach(var solid in config.world.solids)
            {
                var go=Box(solid.id,SquadConfig.Position(solid.position),SquadConfig.Position(solid.size),edge,true);
                if(solid.id.StartsWith("Boundary"))go.GetComponent<Renderer>().enabled=false;
                if(solid.id=="LowPassRoof")go.GetComponent<Renderer>().sharedMaterial=roof;
            }
            for(var z=-4;z<65;z+=2)
            {
                Box("PromenadeSlab",new Vector3(0,.009f,z),new Vector3(7,.018f,1.94f),limestone);
                foreach(var x in new[]{-11.6f,11.6f})Box("Kerb",new Vector3(x,.18f,z),new Vector3(.28f,.36f,1.96f),pale);
            }
            var house=Resources.Load<GameObject>("Art/Models/st_house_v02");
            for(var i=0;i<8;i++)
            {
                var h=Instantiate(house,transform,false);h.transform.position=new Vector3(-15.5f-i%2,0,1+i*8);h.transform.rotation=Quaternion.Euler(0,90,0);UseSurface(h);
                Box("DoorStep",new Vector3(-12.9f,.08f,1+i*8),new Vector3(1.0f,.16f,2),pale);
            }
            var foliage=Mat(.23f,.45f,.33f);var highlight=Mat(.39f,.57f,.36f);
            for(var i=0;i<11;i++)
            {
                var p=new Vector3(-10.3f,0,-2+i*6);
                Box("Planter",p+Vector3.up*.23f,new Vector3(1.5f,.46f,1.7f),edge);
                SquadVisual.Shape("TreeTrunk",transform,PrimitiveType.Cylinder,p+Vector3.up*1.2f,new Vector3(.13f,1,.13f),timber);
                for(var j=0;j<4;j++)
                {
                    var crown=SquadVisual.Shape("Foliage",transform,PrimitiveType.Sphere,p+new Vector3(Mathf.Sin(j*2.3f)*.47f,2.1f+j*.18f,Mathf.Cos(j*2.3f)*.45f),new Vector3(1.6f,1.35f,1.7f),j%2==0?foliage:highlight);
                    crown.transform.rotation=Quaternion.Euler(10,j*37,7);
                }
            }
            for(var i=0;i<9;i++)
            {
                var z=1+i*7f;
                Box("LanternPole",new Vector3(9.8f,1.2f,z),new Vector3(.08f,2.4f,.08f),timber);
                Box("Lantern",new Vector3(9.8f,2.35f,z),new Vector3(.30f,.4f,.30f),pale);
                Box("LanternRoof",new Vector3(9.8f,2.6f,z),new Vector3(.46f,.10f,.46f),roof);
                if(i%2==0)
                {
                    Box("BenchSeat",new Vector3(8.4f,.48f,z+1.5f),new Vector3(1.7f,.12f,.55f),timber);
                    Box("BenchBack",new Vector3(8.4f,.83f,z+1.25f),new Vector3(1.7f,.46f,.08f),timber);
                    foreach(var x in new[]{7.8f,9f})Box("BenchLeg",new Vector3(x,.22f,z+1.5f),new Vector3(.09f,.44f,.45f),edge);
                }
            }
            var boat=Resources.Load<GameObject>("Art/Models/st_boat_v02");
            for(var i=0;i<4;i++)
            {
                var b=Instantiate(boat,transform,false);b.transform.position=new Vector3(18+i%2*3,-.42f,6+i*14);b.transform.rotation=Quaternion.Euler(0,-22+i*14,0);UseSurface(b);
                Box("TimberJetty",new Vector3(14.8f,.03f,5+i*14),new Vector3(5.8f,.15f,1.8f),timber);
                for(var k=0;k<6;k++)Box("JettySeam",new Vector3(12.5f+k*.92f,.112f,5+i*14),new Vector3(.025f,.012f,1.8f),edge);
            }
            // Harbour landmarks frame the playable route without narrowing combat clearance.
            var rust=Mat(.57f,.28f,.20f);var blueCloth=Mat(.22f,.45f,.51f);var dark=Mat(.18f,.25f,.26f);
            for(var i=0;i<6;i++)
            {
                var z=3+i*10f;
                // Continuous sea rail, broken at the four timber jetties.
                Box("SeaRailPost",new Vector3(11.4f,.52f,z),new Vector3(.10f,1.04f,.10f),dark);
                Box("SeaRail",new Vector3(11.4f,.88f,z+2.9f),new Vector3(.07f,.08f,5.8f),dark);
                Box("SeaRailLower",new Vector3(11.4f,.44f,z+2.9f),new Vector3(.05f,.05f,5.8f),dark);
                // Small working areas sit at the edge of the promenade.
                for(var j=0;j<3;j++)
                {
                    var p=new Vector3(9.7f+j%2*.65f,0,z+2+j/2*.7f);
                    Box("FishingBox",p+Vector3.up*.26f,new Vector3(.58f,.52f,.61f),timber);
                    for(var k=0;k<3;k++)Box("BoxBand",p+new Vector3(0,.10f+k*.16f,-.312f),new Vector3(.60f,.035f,.018f),pale);
                }
            }
            foreach(var z in new[]{5f,29f,51f})
            {
                // An awning and counter define the village side of each small plaza.
                var awning=Box("ShopAwning",new Vector3(-11.4f,2.8f,z),new Vector3(2.0f,.10f,3.3f),blueCloth);awning.transform.rotation=Quaternion.Euler(0,0,-10);
                Box("ShopCounter",new Vector3(-11.3f,.58f,z),new Vector3(1.3f,1.16f,2.8f),timber);
                for(var j=0;j<4;j++)Box("AwningStripe",new Vector3(-11.4f,2.82f,z-1.2f+j*.8f),new Vector3(2,.12f,.16f),pale).transform.rotation=awning.transform.rotation;
                foreach(var dz in new[]{-2.1f,2.1f})
                {
                    SquadVisual.Shape("FlowerPot",transform,PrimitiveType.Cylinder,new Vector3(-10.7f,.24f,z+dz),new Vector3(.40f,.24f,.40f),rust);
                    SquadVisual.Shape("PotLeaf",transform,PrimitiveType.Sphere,new Vector3(-10.7f,.65f,z+dz),new Vector3(.72f,.7f,.7f),foliage);
                }
            }
            // Drains and crossing marks establish scale on the broad traversal floor.
            for(var z=0;z<62;z+=5)
                for(var j=0;j<6;j++)Box("DrainSlot",new Vector3(4.5f,.012f,z+j*.075f),new Vector3(.40f,.022f,.027f),dark);
            foreach(var z in new[]{2f,31f,52f})
                for(var j=-2;j<=2;j++)Box("CrossingMark",new Vector3(j*1.18f,.023f,z),new Vector3(.72f,.014f,1.6f),pale);
            var gold=Mat(1,.76f,.34f);
            foreach(var form in config.scanTemplates)
            {
                if(!string.IsNullOrEmpty(form.attachToId))continue;
                var size=SquadConfig.Position(form.sizeMeters);var p=SquadConfig.Position(form.spawn);
                SquadVisual.Shape(form.id,transform,form.id.EndsWith("stone")?PrimitiveType.Sphere:PrimitiveType.Cube,p+Vector3.up*size.y*.5f,size,form.id.EndsWith("stone")?edge:timber,true);
                Marker25D(form.id+"_scan",p,new Color(.23f,.86f,.81f));
            }
            foreach(var clue in config.interactions)
            {
                var p=SquadConfig.Position(clue.spawn);var marker=Marker25D(clue.id,p,new Color(1,.78f,.38f));Markers[clue.id]=marker;
                if(clue.reward=="pipe")
                {
                    var pipe=SquadVisual.Shape("PickupPipe",marker.transform,PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(.055f,.5f,.055f),edge);pipe.transform.localRotation=Quaternion.Euler(90,0,35);
                }
                else if(clue.id.Contains("shell"))SquadVisual.Shape("Shell",marker.transform,PrimitiveType.Sphere,Vector3.up*.13f,new Vector3(.50f,.20f,.40f),pale);
                else SquadVisual.Shape("Evidence",marker.transform,PrimitiveType.Cylinder,Vector3.up*.02f,new Vector3(.48f,.015f,.7f),clue.id.Contains("hair")?gold:edge);
            }
            Marker25D("Encounter",SquadConfig.Position(config.world.encounterGate),new Color(1,.43f,.35f));
            Marker25D("Exit",SquadConfig.Position(config.world.exit),new Color(.3f,.92f,.84f));
            var npc=new GameObject("st_mio").transform;npc.SetParent(transform,false);npc.position=SquadConfig.Position(config.world.npc);
            _npcBody=new SquadBody(npc,false,false,false,config.presentation,"st_mio");
            Physics.SyncTransforms();
        }
        private GameObject Marker25D(string id,Vector3 position,Color color)
        {
            var go=new GameObject(id);go.transform.SetParent(transform,false);go.transform.localPosition=position;
            var line=go.AddComponent<LineRenderer>();var material=new Material(Resources.Load<Shader>("Shaders/SquadTrail"));_materials.Add(material);
            line.sharedMaterial=material;line.useWorldSpace=false;line.positionCount=40;line.loop=true;line.startColor=line.endColor=color;line.startWidth=line.endWidth=.045f;
            for(var i=0;i<40;i++)line.SetPosition(i,Quaternion.Euler(0,i*9,0)*Vector3.forward*.55f+Vector3.up*.045f);
            return go;
        }
    }
}
