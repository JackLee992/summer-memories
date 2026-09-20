using UnityEngine;

namespace SummerMemories.Action3D
{
    /// <summary>夜海堤白盒场景：长堤、海面、石桩围栏、礁石、远端海蚀洞、月光与雾、隐形边界。M2 替换为 URP 美术场景。</summary>
    public static class WorldBuilder3D
    {
        public class World
        {
            public Transform Root;
            public Vector3 PlayerSpawn;
            public Vector3[] EnemySpawns;
            public Camera Camera;
        }

        // 堤道尺寸
        private const float PathWidth = 10f;
        private const float PathLength = 84f;

        public static World Build()
        {
            var root = new GameObject("World").transform;

            BuildEnvironment();
            BuildSeaAndPath(root);
            BuildRailings(root);
            BuildRocks(root);
            BuildCaveGate(root);
            BuildLamps(root);
            BuildBounds(root);

            var world = new World
            {
                Root = root,
                PlayerSpawn = new Vector3(0f, 0f, 26f),
                EnemySpawns = new[]
                {
                    new Vector3(0f, 0f, 6f),
                    new Vector3(-2.2f, 0f, -4f),
                    new Vector3(2.2f, 0f, -14f),
                }
            };
            return world;
        }

        private static void BuildEnvironment()
        {
            // 夜间环境光与线性雾
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.03f, 0.06f, 0.11f);
            RenderSettings.fogStartDistance = 16f;
            RenderSettings.fogEndDistance = 95f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.10f, 0.14f, 0.22f);
            RenderSettings.ambientEquatorColor = new Color(0.05f, 0.08f, 0.13f);
            RenderSettings.ambientGroundColor = new Color(0.02f, 0.03f, 0.05f);

            var lightGo = new GameObject("MoonLight");
            var dl = lightGo.AddComponent<Light>();
            dl.type = LightType.Directional;
            dl.color = new Color(0.62f, 0.72f, 0.95f);
            dl.intensity = 1.05f;
            dl.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(42f, -38f, 0f);
        }

        private static void BuildSeaAndPath(Transform root)
        {
            // 海面（不透明深蓝绿，M2 换半透明波纹）
            var sea = Vis.Prim("Sea", PrimitiveType.Cube, root,
                new Color(0.02f, 0.10f, 0.16f), new Vector3(0f, -0.62f, 0f),
                new Vector3(600f, 0.2f, 600f), collider: false,
                mat: Mat.Make(new Color(0.02f, 0.12f, 0.18f), 0.75f, 0f));

            // 堤道石板
            var pathMat = Mat.Make(new Color(0.28f, 0.29f, 0.30f), 0.2f);
            var path = Vis.Prim("StonePath", PrimitiveType.Cube, root, Color.gray,
                new Vector3(0f, -0.5f, 0f), new Vector3(PathWidth, 1f, PathLength),
                collider: true, mat: pathMat);

            // 石板缝（略深的横向装饰条）
            var seamMat = Mat.Make(new Color(0.20f, 0.21f, 0.22f), 0.1f);
            for (var z = -PathLength / 2f + 3f; z < PathLength / 2f; z += 3f)
            {
                Vis.Prim("Seam", PrimitiveType.Cube, path.transform, Color.black,
                    new Vector3(0f, 0.51f, z - (path.transform.position.z)),
                    new Vector3(PathWidth, 0.02f, 0.12f), collider: false, mat: seamMat);
            }
        }

        private static void BuildRailings(Transform root)
        {
            var postMat = Mat.Make(new Color(0.22f, 0.17f, 0.12f), 0.3f);
            var railMat = Mat.Make(new Color(0.30f, 0.22f, 0.14f), 0.3f);
            for (var z = -PathLength / 2f + 2f; z <= PathLength / 2f - 2f; z += 6f)
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    var x = side * (PathWidth / 2f - 0.4f);
                    Vis.Prim("Post", PrimitiveType.Cube, root, Color.gray,
                        new Vector3(x, 0.55f, z), new Vector3(0.45f, 1.1f, 0.45f),
                        collider: true, mat: postMat);
                }
            }
            // 两道横栏（每段）
            for (var z = -PathLength / 2f + 5f; z < PathLength / 2f - 2f; z += 6f)
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    var x = side * (PathWidth / 2f - 0.4f);
                    Vis.Prim("Rail", PrimitiveType.Cube, root, Color.gray,
                        new Vector3(x, 0.95f, z), new Vector3(0.18f, 0.16f, 6f),
                        collider: false, mat: railMat);
                    Vis.Prim("RailLow", PrimitiveType.Cube, root, Color.gray,
                        new Vector3(x, 0.55f, z), new Vector3(0.14f, 0.14f, 6f),
                        collider: false, mat: railMat);
                }
            }
        }

        private static void BuildRocks(Transform root)
        {
            var rockMat = Mat.Make(new Color(0.13f, 0.14f, 0.15f), 0.4f);
            var rng = new System.Random(20260720);
            for (var i = 0; i < 26; i++)
            {
                var side = rng.Next(2) == 0 ? -1f : 1f;
                var x = side * (7f + (float)rng.NextDouble() * 26f);
                var z = (float)(rng.NextDouble() * 70f - 35f);
                var s = 0.8f + (float)rng.NextDouble() * 2.4f;
                var rock = Vis.Prim("Rock", PrimitiveType.Cube, root, Color.gray,
                    new Vector3(x, -0.4f + (float)rng.NextDouble() * 0.3f, z),
                    new Vector3(s, s * (0.7f + (float)rng.NextDouble() * 0.6f), s * (0.8f + (float)rng.NextDouble())),
                    collider: false, mat: rockMat);
                rock.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 20f,
                    (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 20f);
            }
        }

        private static void BuildCaveGate(Transform root)
        {
            var caveMat = Mat.Make(new Color(0.08f, 0.09f, 0.11f), 0.25f);
            // 远端海蚀洞双柱 + 横梁
            Vis.Prim("CavePillarL", PrimitiveType.Cube, root, Color.gray,
                new Vector3(-4.2f, 3f, -PathLength / 2f + 2f), new Vector3(2.4f, 7f, 2.4f),
                collider: true, mat: caveMat);
            Vis.Prim("CavePillarR", PrimitiveType.Cube, root, Color.gray,
                new Vector3(4.2f, 3f, -PathLength / 2f + 2f), new Vector3(2.4f, 7f, 2.4f),
                collider: true, mat: caveMat);
            Vis.Prim("CaveTop", PrimitiveType.Cube, root, Color.gray,
                new Vector3(0f, 6.6f, -PathLength / 2f + 2f), new Vector3(10.8f, 1.6f, 2.4f),
                collider: false, mat: caveMat);
            // 洞口黑面
            Vis.Prim("CaveMouth", PrimitiveType.Cube, root, Color.black,
                new Vector3(0f, 2.6f, -PathLength / 2f + 1.9f), new Vector3(6f, 5.4f, 0.4f),
                collider: false, mat: Mat.MakeUnlit(new Color(0.005f, 0.006f, 0.012f)));
        }

        private static void BuildLamps(Transform root)
        {
            var glowMat = Mat.MakeEmissive(new Color(1f, 0.72f, 0.32f), 2.2f);
            var poleMat = Mat.Make(new Color(0.18f, 0.14f, 0.10f), 0.3f);
            foreach (var z in new[] { 18f, -2f, -22f })
            {
                var side = z < 10 ? -1f : 1f;
                var x = side * (PathWidth / 2f - 1.1f);
                Vis.Prim("LampPole", PrimitiveType.Cube, root, Color.gray,
                    new Vector3(x, 1.1f, z), new Vector3(0.18f, 2.2f, 0.18f),
                    collider: false, mat: poleMat);
                var bulb = Vis.Prim("LampGlow", PrimitiveType.Sphere, root, Color.yellow,
                    new Vector3(x, 2.3f, z), Vector3.one * 0.32f, collider: false, mat: glowMat);
                var plGo = new GameObject("LampPoint");
                plGo.transform.SetParent(root, false);
                plGo.transform.position = bulb.transform.position;
                var pl = plGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = new Color(1f, 0.7f, 0.36f);
                pl.range = 9f;
                pl.intensity = 1.4f;
                pl.shadows = LightShadows.None;
            }
        }

        private static void BuildBounds(Transform root)
        {
            // 两侧 + 两端隐形墙，防止角色走出堤面
            CreateInvisibleWall(root, "WallSideL", new Vector3(-(PathWidth / 2f + 0.4f), 1.5f, 0f),
                new Vector3(0.6f, 3f, PathLength));
            CreateInvisibleWall(root, "WallSideR", new Vector3(PathWidth / 2f + 0.4f, 1.5f, 0f),
                new Vector3(0.6f, 3f, PathLength));
            CreateInvisibleWall(root, "WallEndN", new Vector3(0f, 1.5f, PathLength / 2f + 0.4f),
                new Vector3(PathWidth + 2f, 3f, 0.6f));
            CreateInvisibleWall(root, "WallEndS", new Vector3(0f, 1.5f, -PathLength / 2f + 0.6f),
                new Vector3(PathWidth + 2f, 3f, 0.6f));
        }

        private static void CreateInvisibleWall(Transform root, string name, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
        }
    }
}
