using UnityEngine;
using UnityEngine.Events;

public class UnfreezeChilds : MonoBehaviour
{
    [Tooltip("Options for triggering the fracture")]
    public TriggerOptions triggerOptions;

    private bool isFrozen = true;

    // Cached components
    private Rigidbody parentRigidbody;
    private MeshCollider parentCollider;
    private MeshRenderer meshRenderer;
    private DestructibleObject destructibleObjectScript;
    private GameObject[] childObjects;

    void Start()
    {
        parentRigidbody = GetComponent<Rigidbody>();
        parentCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        destructibleObjectScript = GetComponent<DestructibleObject>();

        int childCount = transform.childCount;
        childObjects = new GameObject[childCount];

        for (int i = 0; i < childCount; i++)
        {
            childObjects[i] = transform.GetChild(i).gameObject;
            childObjects[i].SetActive(false);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isFrozen)
        {
            return;
        }

        if (collision.contactCount > 0)
        {
            var contact = collision.contacts[0];
            var collisionForce = collision.impulse.magnitude / Time.fixedDeltaTime;
            bool colliderTagAllowed = triggerOptions.IsTagAllowed(contact.otherCollider.gameObject.tag);

            if (collisionForce > MinimumCollisionForce &&
                (!triggerOptions.filterCollisionsByTag || colliderTagAllowed))
            {
                Fracture();
            }
        }
    }

    /// <summary>
    /// **FIXED:** This method now ensures it can only run once.
    /// </summary>
    public void Fracture()
    {
        // 1. Add a guard clause. If already fractured, exit immediately.
        if (!isFrozen)
        {
            return;
        }

        // 2. Set the state immediately to prevent any other calls from getting through.
        isFrozen = false;

        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        if (parentRigidbody != null)
        {
            parentRigidbody.isKinematic = true;
        }

        if (parentCollider != null)
        {
            parentCollider.enabled = false;
        }

        foreach (GameObject childObj in childObjects)
        {
            if (childObj != null)
            {
                childObj.SetActive(true);
            }
        }

        // 3. Call the logic from the other script *after* the state is secured.
        if (destructibleObjectScript != null)
        {
            destructibleObjectScript.DoThisWhenBrokenIntoPieces();
        }
    }

    public void TriggerFracture()
    {
        // This now safely calls the guarded Fracture method.
        Fracture();
    }

    public bool IsIntact()
    {
        return isFrozen;
    }

    public float MinimumCollisionForce
    {
        get { return triggerOptions.minimumCollisionForce; }
        set { triggerOptions.minimumCollisionForce = value; }
    }
}