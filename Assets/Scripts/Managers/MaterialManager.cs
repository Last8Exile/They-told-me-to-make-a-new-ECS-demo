using System.Collections.Generic;
using UnityEngine;

public class MaterialManager : LazyMonoSingleton<MaterialManager>
{
    private static readonly int _mainTexId = Shader.PropertyToID("_MainTex");

    private MaterialDictionary _materials = new MaterialDictionary();
    private CreatedMaterialsDictionary _createdMaterials = new CreatedMaterialsDictionary();

    public Material GetSharedInstanceMaterial(Material material, Texture texture)
    {
        if (material == null || texture == null)
        {
            Debug.LogError($"[{nameof(MaterialManager)}]:{nameof(GetSharedInstanceMaterial)} - Argument null");
            return null;
        }

        if (_createdMaterials.TryGetValue(material, out var associatedTexture) && associatedTexture == texture)
            return material;

        if (!_materials.TryGetValue(material, out var texureDictionary))
        {
            texureDictionary = new Dictionary<Texture, Material>();
            _materials.Add(material, texureDictionary);
        }

        if (!texureDictionary.TryGetValue(texture, out var sharedInstanceMaterial))
        {
            sharedInstanceMaterial = new Material(material);
            sharedInstanceMaterial.name += $"(Instance {texture.name})";
            texureDictionary.Add(texture, sharedInstanceMaterial);

            _createdMaterials.Add(sharedInstanceMaterial, texture);

            sharedInstanceMaterial.SetTexture(_mainTexId, texture);
        }

        return sharedInstanceMaterial;
    }

    public IEnumerable<Material> FindInstancedMaterials(Material material)
    {
        if (_materials.TryGetValue(material, out var textureDictionary))
        {
            foreach (var instancedMaterial in textureDictionary.Values)
                yield return instancedMaterial;
        }
    }

    protected override void OnDestroy()
    {
        foreach (var textureDictionary in _materials)
        {
            foreach (var sharedInstanceMaterial in textureDictionary.Value)
            {
                Destroy(sharedInstanceMaterial.Value);
            }
            textureDictionary.Value.Clear();
        }
        _materials.Clear();
        base.OnDestroy();
    }

    private class MaterialDictionary : Dictionary<Material, Dictionary<Texture, Material>> { }
    private class CreatedMaterialsDictionary : Dictionary<Material, Texture> { }
}
