using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Add this component to destructible objects
public class DestructibleObject : MonoBehaviour
{
    [Header("Object Properties")]
    public string resourceType = "Rock"; // Stone, Wood, Metal, etc.
    public int resourceValue = 1; // How much to add to inventory

    [Header("Destruction Settings")]
    public float debrisLifetime = 5f; // How long debris stays before cleanup
    private List<Transform> childPieces = new List<Transform>();

    [Header("Collection Settings")]
    public float collectionRadius = 3f;
    public LayerMask debrisLayer; // What layers count as debris
    public float collectionDelay = 0.3f; // debris collection

    [Header("Player Reference")]
    public Transform playerTransform; // Reference to player for collection position
    public float minRandomForceAmount = 3.0f;
    public float maxRandomForceAmount = 6.0f;

    void Start()
    {
        // Cache all child pieces for later cleanup
        foreach (Transform child in transform)
        {
            childPieces.Add(child);
        }

        // Try to find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }

    public void DoThisWhenBrokenIntoPieces()
    {
        InventoryManager.Instance.CollectResource(resourceType, resourceValue);
        InventoryManager.Instance.ShowInventory();

        CollectNearbyDebris();
        AddRandomForceToChildren(minRandomForceAmount, maxRandomForceAmount);
        StartCoroutine(CleanupDebris());
    }

    public void CollectNearbyDebris()
    {
        Vector3 collectionPosition = transform.position;

        // Use player position if available, otherwise use object position
        if (playerTransform != null)
        {
            collectionPosition = playerTransform.position;
        }

        Collider[] nearbyDebris = Physics.OverlapSphere(collectionPosition, collectionRadius, debrisLayer);

        foreach (Collider debris in nearbyDebris)
        {
            Rigidbody rb = debris.GetComponent<Rigidbody>();
            if (rb != null && debris.transform.IsChildOf(transform))
            {
                StartCoroutine(DestroyDebrisWithDelay(debris.gameObject, collectionDelay));
            }
        }
    }

    private IEnumerator DestroyDebrisWithDelay(GameObject debris, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (debris != null)
        {
            Destroy(debris);
        }
    }

    public void AddRandomForceToChildren(float minForce, float maxForce)
    {
        foreach (Transform child in transform)
        {
            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Random direction (unit sphere gives a normalized vector in all directions)
                Vector3 randomDirection = Random.onUnitSphere;
                // Random force amount between min and max
                float randomForceAmount = Random.Range(minForce, maxForce);
                // Apply force
                rb.AddForce(randomDirection * randomForceAmount, ForceMode.Impulse);
            }
        }
    }

    private IEnumerator CleanupDebris()
    {
        yield return new WaitForSeconds(debrisLifetime);

        // Destroy all child pieces
        foreach (Transform child in childPieces)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        // Destroy the parent object
        Destroy(gameObject);
    }

    // Visualize collection radius in scene view
    void OnDrawGizmosSelected()
    {
        Vector3 gizmoCenter = transform.position;

        // Show collection sphere at player position if available
        if (playerTransform != null)
        {
            gizmoCenter = playerTransform.position;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gizmoCenter, collectionRadius);
    }
}