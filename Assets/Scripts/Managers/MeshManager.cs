using System.Collections.Generic;

using UnityEngine;

public class MeshManager : LazyMonoSingleton<MeshManager>
{
    private Dictionary<Sprite, Mesh> _dictionary = new Dictionary<Sprite, Mesh>();

    private List<Vector3> _vertices = new List<Vector3>(128);

    public Mesh GetSharedInstanceMesh(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError($"[{nameof(MeshManager)}]:{nameof(GetSharedInstanceMesh)} - Argument null");
            return null;
        }

        if (!_dictionary.TryGetValue(sprite, out var sharedInstanceMesh))
        {
            sharedInstanceMesh = new Mesh();
            sharedInstanceMesh.name += $"(Instance {sprite.name})";
            _dictionary.Add(sprite, sharedInstanceMesh);

            var sharedVerts = _vertices;
            var verts = sprite.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                var vert = verts[i];
                if (sharedVerts.Count > i)
                    sharedVerts[i] = vert;
                else
                    sharedVerts.Add(vert);
            }

            sharedInstanceMesh.SetVertices(sharedVerts, 0, verts.Length);
            sharedInstanceMesh.SetUVs(0, sprite.uv);
            sharedInstanceMesh.SetTriangles(sprite.triangles, 0);
        }

        return sharedInstanceMesh;
    }

    protected override void OnDestroy()
    {
        foreach (var sharedInstanceMesh in _dictionary.Values)
            Destroy(sharedInstanceMesh);
        _dictionary.Clear();
        base.OnDestroy();
    }
}
