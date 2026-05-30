using CubeNinja.Data;
using UnityEngine;

namespace CubeNinja.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CubeTarget : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int CullId = Shader.PropertyToID("_Cull");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int SpecColorId = Shader.PropertyToID("_SpecColor");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        private const float EdgeThickness = 0.0175f;
        private const float CubeAlpha = 0.68f;
        private const float EdgeInset = 0.5f - (EdgeThickness * 0.5f);
        private const float EdgeLength = 1f;

        private static Material sharedEdgeMaterial;
        private static Material sharedCubeMaterial;

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Rigidbody body;
        [SerializeField] private BoxCollider targetCollider;

        private Transform edgeRoot;
        private MaterialPropertyBlock propertyBlock;
        private CubeTypeDefinition cubeType;
        private ICubeTargetListener listener;
        private float playAreaEntryY;
        private float bottomMissY;
        private float leftBoundX;
        private float rightBoundX;
        private bool enteredPlayArea;
        private bool resolved;

        public CubeTypeDefinition CubeType => cubeType;

        private void Awake()
        {
            EnsureComponents();
        }

        private void Update()
        {
            if (resolved)
            {
                return;
            }

            var y = transform.position.y;
            ReflectInsideHorizontalBounds();

            if (!enteredPlayArea && y >= playAreaEntryY)
            {
                enteredPlayArea = true;
            }

            if (enteredPlayArea && y < bottomMissY)
            {
                resolved = true;
                listener?.OnCubeMissed(this);
            }
        }

        private void OnMouseDown()
        {
            if (resolved)
            {
                return;
            }

            resolved = true;
            listener?.OnCubeClicked(this);
        }

        public void Initialize(
            CubeTypeDefinition type,
            ICubeTargetListener targetListener,
            float entryY,
            float missY,
            float leftX,
            float rightX,
            float scale)
        {
            EnsureComponents();

            cubeType = type;
            listener = targetListener;
            playAreaEntryY = entryY;
            bottomMissY = missY;
            leftBoundX = Mathf.Min(leftX, rightX);
            rightBoundX = Mathf.Max(leftX, rightX);
            enteredPlayArea = false;
            resolved = false;

            transform.localScale = Vector3.one * scale;
            targetCollider.enabled = true;
            targetCollider.isTrigger = true;
            body.useGravity = true;
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezePositionZ;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            ApplyColor(type != null ? type.Color : Color.white);
        }

        public void Launch(Vector3 position, Vector3 velocity, Vector3 angularVelocity)
        {
            EnsureComponents();

            transform.position = position;
            transform.rotation = Random.rotation;
            body.linearVelocity = velocity;
            body.angularVelocity = angularVelocity;
            body.WakeUp();
        }

        public void PrepareForPool()
        {
            EnsureComponents();

            resolved = true;
            listener = null;
            cubeType = null;

            if (targetCollider != null)
            {
                targetCollider.enabled = false;
            }

            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    body.Sleep();
                }

                body.isKinematic = true;
            }
        }

        private void EnsureComponents()
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            if (meshRenderer != null)
            {
                var cubeMaterial = GetSharedCubeMaterial();
                if (cubeMaterial != null)
                {
                    meshRenderer.sharedMaterial = cubeMaterial;
                }
            }

            EnsureEdgeRenderers();

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            if (targetCollider == null)
            {
                targetCollider = GetComponent<BoxCollider>();
            }

            if (targetCollider == null)
            {
                targetCollider = gameObject.AddComponent<BoxCollider>();
            }

            targetCollider.isTrigger = true;
        }

        private void ApplyColor(Color color)
        {
            if (meshRenderer == null)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            var translucentColor = new Color(color.r, color.g, color.b, CubeAlpha);
            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, translucentColor);
            propertyBlock.SetColor(ColorId, translucentColor);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private static Material GetSharedCubeMaterial()
        {
            if (sharedCubeMaterial != null)
            {
                return sharedCubeMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            sharedCubeMaterial = new Material(shader)
            {
                name = "CubeNinja Smooth Cube Material",
                hideFlags = HideFlags.DontSave
            };

            if (sharedCubeMaterial.HasProperty(MainTexId))
            {
                sharedCubeMaterial.SetTexture(MainTexId, Texture2D.whiteTexture);
            }

            if (sharedCubeMaterial.HasProperty(BaseMapId))
            {
                sharedCubeMaterial.SetTexture(BaseMapId, Texture2D.whiteTexture);
            }

            if (sharedCubeMaterial.HasProperty(SmoothnessId))
            {
                sharedCubeMaterial.SetFloat(SmoothnessId, 0f);
            }

            if (sharedCubeMaterial.HasProperty(GlossinessId))
            {
                sharedCubeMaterial.SetFloat(GlossinessId, 0f);
            }

            if (sharedCubeMaterial.HasProperty(MetallicId))
            {
                sharedCubeMaterial.SetFloat(MetallicId, 0f);
            }

            if (sharedCubeMaterial.HasProperty(SpecColorId))
            {
                sharedCubeMaterial.SetColor(SpecColorId, Color.black);
            }

            ConfigureTransparentMaterial(sharedCubeMaterial);
            return sharedCubeMaterial;
        }

        private static Material GetSharedEdgeMaterial()
        {
            if (sharedEdgeMaterial != null)
            {
                return sharedEdgeMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            sharedEdgeMaterial = new Material(shader)
            {
                name = "CubeNinja Cube Edge Material",
                hideFlags = HideFlags.DontSave
            };
            sharedEdgeMaterial.color = Color.black;
            if (sharedEdgeMaterial.HasProperty(BaseColorId))
            {
                sharedEdgeMaterial.SetColor(BaseColorId, Color.black);
            }

            if (sharedEdgeMaterial.HasProperty(ColorId))
            {
                sharedEdgeMaterial.SetColor(ColorId, Color.black);
            }

            sharedEdgeMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.GeometryLast;
            return sharedEdgeMaterial;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            if (material.HasProperty(SurfaceId))
            {
                material.SetFloat(SurfaceId, 1f);
            }

            if (material.HasProperty(SrcBlendId))
            {
                material.SetFloat(SrcBlendId, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty(DstBlendId))
            {
                material.SetFloat(DstBlendId, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty(ZWriteId))
            {
                material.SetFloat(ZWriteId, 0f);
            }

            if (material.HasProperty(CullId))
            {
                material.SetFloat(CullId, (float)UnityEngine.Rendering.CullMode.Back);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private void EnsureEdgeRenderers()
        {
            if (edgeRoot != null)
            {
                return;
            }

            var edgeMaterial = GetSharedEdgeMaterial();
            if (edgeMaterial == null)
            {
                return;
            }

            var rootObject = new GameObject("Cube Edges");
            edgeRoot = rootObject.transform;
            edgeRoot.SetParent(transform, false);
            edgeRoot.localPosition = Vector3.zero;
            edgeRoot.localRotation = Quaternion.identity;
            edgeRoot.localScale = Vector3.one;

            var index = 0;
            for (var y = -1; y <= 1; y += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                {
                    CreateEdge($"Edge X {index++}", new Vector3(0f, y * EdgeInset, z * EdgeInset), new Vector3(EdgeLength, EdgeThickness, EdgeThickness), edgeMaterial);
                }
            }

            for (var x = -1; x <= 1; x += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                {
                    CreateEdge($"Edge Y {index++}", new Vector3(x * EdgeInset, 0f, z * EdgeInset), new Vector3(EdgeThickness, EdgeLength, EdgeThickness), edgeMaterial);
                }
            }

            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    CreateEdge($"Edge Z {index++}", new Vector3(x * EdgeInset, y * EdgeInset, 0f), new Vector3(EdgeThickness, EdgeThickness, EdgeLength), edgeMaterial);
                }
            }
        }

        private void CreateEdge(string edgeName, Vector3 localPosition, Vector3 localScale, Material edgeMaterial)
        {
            var edgeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edgeObject.name = edgeName;
            edgeObject.transform.SetParent(edgeRoot, false);
            edgeObject.transform.localPosition = localPosition;
            edgeObject.transform.localRotation = Quaternion.identity;
            edgeObject.transform.localScale = localScale;

            var edgeCollider = edgeObject.GetComponent<Collider>();
            if (edgeCollider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(edgeCollider);
                }
                else
                {
                    DestroyImmediate(edgeCollider);
                }
            }

            var edgeRenderer = edgeObject.GetComponent<MeshRenderer>();
            if (edgeRenderer != null)
            {
                edgeRenderer.sharedMaterial = edgeMaterial;
                edgeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                edgeRenderer.receiveShadows = false;
            }
        }

        private void ReflectInsideHorizontalBounds()
        {
            if (body == null || body.isKinematic)
            {
                return;
            }

            var position = body.position;
            var velocity = body.linearVelocity;
            var changed = false;

            if (position.x <= leftBoundX)
            {
                position.x = leftBoundX;
                if (velocity.x < 0f)
                {
                    velocity.x = -velocity.x;
                }

                changed = true;
            }
            else if (position.x >= rightBoundX)
            {
                position.x = rightBoundX;
                if (velocity.x > 0f)
                {
                    velocity.x = -velocity.x;
                }

                changed = true;
            }

            if (!changed)
            {
                return;
            }

            body.position = position;
            body.linearVelocity = velocity;
        }
    }
}
