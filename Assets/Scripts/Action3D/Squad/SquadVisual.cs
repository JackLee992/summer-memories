using UnityEngine;

namespace SummerMemories.Action3D.Squad
{
    // Original procedural stand-ins. Animation hooks stay separate from combat timing.
    public static class SquadVisual
    {
        public static Material Material(Color color)
        {
            var shader = Resources.Load<Shader>("Shaders/SquadSurface");
            if (shader == null) throw new System.InvalidOperationException("Missing SquadSurface shader");
            return new Material(shader) { color = color };
        }
        public static GameObject Shape(string name, Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material material, bool solid = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) { var c = go.GetComponent<Collider>(); c.enabled = false; Object.Destroy(c); }
            return go;
        }
        public static Transform Limb(string name, Transform root, Vector3 joint, Vector3 size, Material mat)
        {
            var pivot = new GameObject(name).transform; pivot.SetParent(root, false); pivot.localPosition = joint;
            Shape(name + "Mesh", pivot, PrimitiveType.Capsule, new Vector3(0, -size.y * .5f, 0), new Vector3(size.x,size.y*.5f,size.z), mat);
            return pivot;
        }
    }

}
