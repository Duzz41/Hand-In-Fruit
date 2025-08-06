using UnityEngine;

public class SetFalseMountain : MonoBehaviour
{
    [SerializeField]
    private GameObject mountain;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (mountain.activeSelf == true)
                mountain.SetActive(false);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (mountain.activeSelf == false)
                mountain.SetActive(true);
        }
    }
}
