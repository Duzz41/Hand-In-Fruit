using UnityEngine;
using UnityEngine.Events;

public class UnfreezeChilds : MonoBehaviour
{
    [Tooltip("Options for triggering the fracture")]
    public TriggerOptions triggerOptions;

    [Header("Drilling System Integration")]
    [Tooltip("Only allow fracturing through the drilling system, not direct collision")]
    [SerializeField] private bool onlyAllowDrillingFracture = true;

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

        // FIXED: Only process collision if drilling fracture is disabled
        if (onlyAllowDrillingFracture)
        {
            // Let the DrillForceBuildup system handle all fracturing
            return;
        }

        // Original collision logic (only runs if onlyAllowDrillingFracture is false)
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
    /// This method ensures it can only run once and is the only way to fracture objects.
    /// </summary>
    public void Fracture()
    {
        // 1. Guard clause - if already fractured, exit immediately
        if (!isFrozen)
        {
            return;
        }

        // 2. Set the state immediately to prevent any other calls
        isFrozen = false;

        // 3. Log for debugging
        Debug.Log($"[UnfreezeChilds] Fracturing {gameObject.name}");

        // 4. Disable visual and physics components
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

        // 5. Activate child pieces
        foreach (GameObject childObj in childObjects)
        {
            if (childObj != null)
            {
                childObj.SetActive(true);
            }
        }

        // 6. Notify the drilling system that this object is now fractured
        var drillScript = FindFirstObjectByType<DrillForceBuildup>();
        if (drillScript != null && drillScript.GetCurrentTarget() == gameObject)
        {
            drillScript.ForceStopDrilling();
        }

        // 7. Call the destructible object logic
        if (destructibleObjectScript != null)
        {
            destructibleObjectScript.DoThisWhenBrokenIntoPieces();
        }
    }

    public void TriggerFracture()
    {
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

    // FIXED: New method to enable/disable drilling-only mode
    public void SetDrillingOnlyMode(bool drillingOnly)
    {
        onlyAllowDrillingFracture = drillingOnly;
    }

    // FIXED: Check if this object can be fractured by collision (not just drilling)
    public bool CanFractureByCollision()
    {
        return !onlyAllowDrillingFracture;
    }
}