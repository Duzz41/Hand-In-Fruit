using UnityEngine;

public class SetFalseMountain : MonoBehaviour
{
    [SerializeField]
    private GameObject mountain;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            mountain.SetActive(false);
    }
}
