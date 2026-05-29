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
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Rigidbody body;
        [SerializeField] private BoxCollider targetCollider;

        private MaterialPropertyBlock propertyBlock;
        private CubeTypeDefinition cubeType;
        private ICubeTargetListener listener;
        private float playAreaEntryY;
        private float bottomMissY;
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
            float scale)
        {
            EnsureComponents();

            cubeType = type;
            listener = targetListener;
            playAreaEntryY = entryY;
            bottomMissY = missY;
            enteredPlayArea = false;
            resolved = false;

            transform.localScale = Vector3.one * scale;
            targetCollider.enabled = true;
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
    }
}
