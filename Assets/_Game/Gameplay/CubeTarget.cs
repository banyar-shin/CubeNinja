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
        private static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private const int PaintTextureSize = 64;

        private static Material sharedPaintMaterial;
        private static Texture2D sharedPaintTexture;

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Rigidbody body;
        [SerializeField] private BoxCollider targetCollider;

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
                var paintMaterial = GetSharedPaintMaterial();
                if (paintMaterial != null)
                {
                    meshRenderer.sharedMaterial = paintMaterial;
                }
            }

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

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private static Material GetSharedPaintMaterial()
        {
            if (sharedPaintMaterial != null)
            {
                return sharedPaintMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            sharedPaintMaterial = new Material(shader)
            {
                name = "CubeNinja Painterly Cube Material",
                hideFlags = HideFlags.DontSave
            };

            var texture = GetSharedPaintTexture();
            if (sharedPaintMaterial.HasProperty(MainTexId))
            {
                sharedPaintMaterial.SetTexture(MainTexId, texture);
            }

            if (sharedPaintMaterial.HasProperty(BaseMapId))
            {
                sharedPaintMaterial.SetTexture(BaseMapId, texture);
            }

            if (sharedPaintMaterial.HasProperty(SmoothnessId))
            {
                sharedPaintMaterial.SetFloat(SmoothnessId, 0.18f);
            }

            if (sharedPaintMaterial.HasProperty(GlossinessId))
            {
                sharedPaintMaterial.SetFloat(GlossinessId, 0.18f);
            }

            if (sharedPaintMaterial.HasProperty(MetallicId))
            {
                sharedPaintMaterial.SetFloat(MetallicId, 0f);
            }

            return sharedPaintMaterial;
        }

        private static Texture2D GetSharedPaintTexture()
        {
            if (sharedPaintTexture != null)
            {
                return sharedPaintTexture;
            }

            sharedPaintTexture = new Texture2D(PaintTextureSize, PaintTextureSize, TextureFormat.RGBA32, false)
            {
                name = "CubeNinja Painterly Cube Texture",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            for (var y = 0; y < PaintTextureSize; y++)
            {
                for (var x = 0; x < PaintTextureSize; x++)
                {
                    var broad = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    var fine = Mathf.PerlinNoise(20f + x * 0.42f, 40f + y * 0.42f);
                    var brush = Mathf.Clamp01((broad * 0.68f) + (fine * 0.32f));
                    var shade = Mathf.Lerp(0.72f, 1.12f, brush);
                    sharedPaintTexture.SetPixel(x, y, new Color(shade, shade, shade, 1f));
                }
            }

            sharedPaintTexture.Apply(false, true);
            return sharedPaintTexture;
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
