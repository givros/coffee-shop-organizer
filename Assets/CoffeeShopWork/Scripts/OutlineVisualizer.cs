using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoffeeShop
{
    public sealed class OutlineVisualizer : MonoBehaviour
    {
        private const string ShellPrefix = "__OutlineShell_";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int CullId = Shader.PropertyToID("_Cull");
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        private static readonly int ZTestId = Shader.PropertyToID("_ZTest");
        private static readonly int ReceiveShadowsId = Shader.PropertyToID("_ReceiveShadows");
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

        [SerializeField, Min(1.001f)] private float shellScale = 1.1f;

        private readonly List<Renderer> shellRenderers = new List<Renderer>();
        private readonly List<Material> shellMaterials = new List<Material>();
        private MaterialPropertyBlock propertyBlock;
        private Color currentColor = Color.white;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            BuildShells();
            SetVisible(false);
        }

        public static bool IsGeneratedShell(Renderer renderer)
        {
            return renderer != null && IsGeneratedShell(renderer.transform);
        }

        public static void MakeRendererTransparent(Renderer renderer, float alpha)
        {
            if (renderer == null || IsGeneratedShell(renderer))
            {
                return;
            }

            Material[] sourceMaterials = renderer.sharedMaterials;
            if (sourceMaterials == null || sourceMaterials.Length == 0)
            {
                return;
            }

            Material[] transparentMaterials = new Material[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material sourceMaterial = sourceMaterials[i];
                Material transparentMaterial = sourceMaterial != null
                    ? new Material(sourceMaterial)
                    : CreateFallbackMaterial();

                if (transparentMaterial == null)
                {
                    continue;
                }

                ConfigureTransparentMaterial(transparentMaterial, alpha);
                transparentMaterials[i] = transparentMaterial;
            }

            renderer.sharedMaterials = transparentMaterials;
        }

        public void SetVisible(bool visible)
        {
            for (int i = 0; i < shellRenderers.Count; i++)
            {
                if (shellRenderers[i] != null)
                {
                    shellRenderers[i].enabled = visible;
                }
            }
        }

        public void SetColor(Color color)
        {
            currentColor = color;

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            for (int i = 0; i < shellRenderers.Count; i++)
            {
                Renderer shellRenderer = shellRenderers[i];
                if (shellRenderer == null)
                {
                    continue;
                }

                shellRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                propertyBlock.SetColor(EmissionColorId, color * 3f);
                shellRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        public void SetState(Color color, bool visible)
        {
            SetColor(color);
            SetVisible(visible);
        }

        private void BuildShells()
        {
            RemoveExistingShells();
            shellRenderers.Clear();
            shellMaterials.Clear();

            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter sourceFilter = meshFilters[i];
                if (sourceFilter == null || sourceFilter.sharedMesh == null || IsGeneratedShell(sourceFilter.transform))
                {
                    continue;
                }

                Renderer sourceRenderer = sourceFilter.GetComponent<Renderer>();
                if (sourceRenderer == null || !sourceRenderer.enabled)
                {
                    continue;
                }

                GameObject shellObject = new GameObject($"{ShellPrefix}{i}");
                shellObject.transform.SetParent(sourceFilter.transform, false);
                shellObject.transform.localPosition = new Vector3(0f, 0f, 0f);
                shellObject.transform.localRotation = Quaternion.identity;
                shellObject.transform.localScale = Vector3.one * Mathf.Max(shellScale, 1.1f);
                shellObject.layer = sourceFilter.gameObject.layer;

                MeshFilter shellFilter = shellObject.AddComponent<MeshFilter>();
                shellFilter.sharedMesh = sourceFilter.sharedMesh;

                MeshRenderer shellRenderer = shellObject.AddComponent<MeshRenderer>();
                Material sourceMaterial = sourceRenderer.sharedMaterial;
                Material shellMaterial = CreateOutlineMaterial(sourceMaterial);

                if (shellMaterial == null)
                {
                    Destroy(shellObject);
                    continue;
                }

                ConfigureOutlineMaterial(shellMaterial);

                int materialCount = Mathf.Max(1, sourceRenderer.sharedMaterials.Length);
                Material[] shellMaterialsForRenderer = new Material[materialCount];
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    Material material = materialIndex == 0
                        ? shellMaterial
                        : new Material(shellMaterial);

                    shellMaterialsForRenderer[materialIndex] = material;
                    shellMaterials.Add(material);
                }

                shellRenderer.sharedMaterials = shellMaterialsForRenderer;
                shellRenderer.shadowCastingMode = ShadowCastingMode.Off;
                shellRenderer.receiveShadows = false;
                shellRenderer.allowOcclusionWhenDynamic = false;
                shellRenderers.Add(shellRenderer);
            }

            SetColor(currentColor);
        }

        private void RemoveExistingShells()
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == transform || !IsGeneratedShell(child))
                {
                    continue;
                }

                child.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static bool IsGeneratedShell(Transform transformToCheck)
        {
            return transformToCheck != null && transformToCheck.name.StartsWith(ShellPrefix, StringComparison.Ordinal);
        }

        private static Material CreateFallbackMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");

            return shader != null ? new Material(shader) : null;
        }

        private static Material CreateOutlineMaterial(Material sourceMaterial)
        {
            Shader outlineShader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color");

            if (outlineShader != null)
            {
                return new Material(outlineShader);
            }

            return sourceMaterial != null
                ? new Material(sourceMaterial)
                : CreateFallbackMaterial();
        }

        private static void ConfigureOutlineMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(BaseMapId))
            {
                material.SetTexture(BaseMapId, null);
            }

            if (material.HasProperty(MainTexId))
            {
                material.SetTexture(MainTexId, null);
            }

            if (material.HasProperty(CullId))
            {
                material.SetFloat(CullId, (float)CullMode.Front);
            }

            if (material.HasProperty(SurfaceId))
            {
                material.SetFloat(SurfaceId, 0f);
            }

            if (material.HasProperty(ZWriteId))
            {
                material.SetFloat(ZWriteId, 0f);
            }

            if (material.HasProperty(ZTestId))
            {
                material.SetFloat(ZTestId, (float)CompareFunction.LessEqual);
            }

            if (material.HasProperty(ReceiveShadowsId))
            {
                material.SetFloat(ReceiveShadowsId, 0f);
            }

            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void ConfigureTransparentMaterial(Material material, float alpha)
        {
            if (material == null)
            {
                return;
            }

            alpha = Mathf.Clamp01(alpha);

            if (material.HasProperty(SurfaceId))
            {
                material.SetFloat(SurfaceId, 1f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (material.HasProperty(BlendId))
            {
                material.SetFloat(BlendId, 0f);
            }

            if (material.HasProperty(SrcBlendId))
            {
                material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty(DstBlendId))
            {
                material.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty(ZWriteId))
            {
                material.SetFloat(ZWriteId, 0f);
            }

            if (material.HasProperty(CullId))
            {
                material.SetFloat(CullId, (float)CullMode.Back);
            }

            Color baseColor = material.HasProperty(BaseColorId)
                ? material.GetColor(BaseColorId)
                : Color.white;
            baseColor.a = alpha;
            SetMaterialColor(material, baseColor);
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, color);
            }

            if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, color);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < shellMaterials.Count; i++)
            {
                if (shellMaterials[i] != null)
                {
                    Destroy(shellMaterials[i]);
                }
            }
        }
    }
}
