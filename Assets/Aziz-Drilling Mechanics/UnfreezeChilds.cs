using UnityEngine;
using UnityEngine.Events;

public class UnfreezeChilds : MonoBehaviour
{
    [Tooltip("Options for triggering the fracture")]
    public TriggerOptions triggerOptions;

    // True if this object has not been fractured yet
    private bool isFrozen = true;

    // Cache references to avoid repeated GetComponent calls
    private Rigidbody parentRigidbody;
    private MeshCollider parentCollider;
    private MeshRenderer meshRenderer;
    private Rigidbody[] childRigidbodies;

    private DestructibleObject destructibleObjectScript;

    void Start()
    {
        // Cache parent components
        parentRigidbody = GetComponent<Rigidbody>();
        parentCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        destructibleObjectScript = GetComponent<DestructibleObject>();

        // Cache all child rigidbodies
        childRigidbodies = GetComponentsInChildren<Rigidbody>();

        // Remove parent rigidbody from the array if it exists
        if (parentRigidbody != null)
        {
            System.Collections.Generic.List<Rigidbody> childList = new System.Collections.Generic.List<Rigidbody>(childRigidbodies);
            childList.Remove(parentRigidbody);
            childRigidbodies = childList.ToArray();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!this.isFrozen)
        {
            return;
        }

        if (collision.contactCount > 0)
        {
            // Collision force must exceed the minimum force (F = I / T = F)
            var contact = collision.contacts[0];
            var collisionForce = collision.impulse.magnitude / Time.fixedDeltaTime;

            // Colliding object tag must be in the set of allowed collision tags if filtering by tag is enabled
            bool colliderTagAllowed = triggerOptions.IsTagAllowed(contact.otherCollider.gameObject.tag);

            // Fragment is unfrozen if the colliding object has the correct tag (if tag filtering is enabled)
            // and the collision force exceeds the minimum collision force.
            if (collisionForce > triggerOptions.minimumCollisionForce &&
                (!triggerOptions.filterCollisionsByTag || colliderTagAllowed))
            {
                this.Fracture();
            }
        }
    }

    //void OnTriggerEnter(Collider collider)
    //{
    //    if (!this.isFrozen)
    //    {
    //        return;
    //    }

    //    bool tagAllowed = triggerOptions.IsTagAllowed(collider.gameObject.tag);
    //    if (!triggerOptions.filterCollisionsByTag || triggerOptions.IsTagAllowed(collider.gameObject.tag))
    //    {
    //        this.Fracture();
    //    }
    //}

    private void Fracture()
    {
        // Deactivate parent rigidbody and collider
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

        // Unfreeze and activate all child rigidbodies
        foreach (Rigidbody childRb in childRigidbodies)
        {
            if (childRb != null)
            {
                // Unfreeze constraints
                childRb.constraints = RigidbodyConstraints.None;

                // Make sure it's not kinematic so physics can affect it
                childRb.isKinematic = false;
            }
        }

        // Mark as fractured
        this.isFrozen = false;

        destructibleObjectScript.DoThisWhenBrokenIntoPieces();

        // Invoke completion callback
        //if (this.onFractureCompleted != null)
        //{
        //    this.onFractureCompleted.Invoke();
        //}
    }

    // Public method to manually trigger fracture (useful for testing or other scripts)
    public void TriggerFracture()
    {
        if (this.isFrozen)
        {
            this.Fracture();
        }
    }

    // Method to check if the object is still intact
    public bool IsIntact()
    {
        return this.isFrozen;
    }
}