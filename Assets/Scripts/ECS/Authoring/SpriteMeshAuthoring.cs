using Unity.Entities;
using Unity.Rendering;

using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SpriteMeshAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public Sprite Sprite;
    public Material Material;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var renderMesh = new RenderMesh();
        var mesh = renderMesh.mesh = MeshManager.Instance.GetSharedInstanceMesh(Sprite);
        renderMesh.material = MaterialManager.Instance.GetSharedInstanceMaterial(Material, Sprite.texture);
        dstManager.AddSharedComponentData(entity, renderMesh);

        var bounds = mesh.bounds;
        dstManager.AddComponentData(entity, new RenderBounds { Value = new Unity.Mathematics.AABB { Center = bounds.center, Extents = bounds.extents } });
    }

#if UNITY_EDITOR

    [NonSerialized] private SpriteRenderer _spriteRenderer;
    private void OnEnable()
    {
        if (Application.IsPlaying(gameObject))
            return;

        if (_spriteRenderer == null && !TryGetComponent(out _spriteRenderer))
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
            _spriteRenderer.sprite = Sprite;
        }

        _spriteRenderer.enabled = true;
    }

    private void OnDisable()
    {
        if (Application.IsPlaying(gameObject))
            return;

        _spriteRenderer.enabled = false;
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpriteMeshAuthoring))]
    public class SpriteMeshAuthoringEditor : Editor
    {
        private SpriteMeshAuthoring _target => target as SpriteMeshAuthoring;

        public override void OnInspectorGUI()
        {
            if (DrawDefaultInspector())
            {
                var sr = _target._spriteRenderer;
                if (sr != null)
                    sr.sprite = _target.Sprite;
            }
                
        }
    }
#endif
}