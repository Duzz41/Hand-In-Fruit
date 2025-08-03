using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class UpgradeTrigger : MonoBehaviour
{
    [Tooltip("Bu tetikleyici alaný aktif edecek GameObject'in Tag'i (genellikle 'Player').")]
    public string playerTag = "Player";

    public UnityEvent onPlayerEnter;

    public UnityEvent onPlayerExit;

    void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("UpgradeTrigger script'ini kullanan objenin Collider'ý 'Is Trigger' olarak ayarlanmalý.", this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            onPlayerEnter.Invoke();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            onPlayerExit.Invoke();
        }
    }
}
