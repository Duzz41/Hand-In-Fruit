using UnityEngine;
using System.Collections;

public class VibratePlayer : MonoBehaviour
{
    [Header("Player Model Shake Settings")]
    [Tooltip("The GameObject that represents the player's visual model to be shaken.")]
    [SerializeField] private Transform playerModel;
    [Tooltip("The amount of shake applied to the model.")]
    [SerializeField] private float shakeMagnitude = 0.05f;
    [Tooltip("The speed of the shake animation.")]
    [SerializeField] private float shakeSpeed = 50f;

    [Header("Debug Settings")]
    [SerializeField] private bool debugVibration = true;

    private Vector3 initialPosition;
    private bool isShaking = false;
    private Coroutine shakeCoroutine;

    void Start()
    {
        if (playerModel != null)
        {
            initialPosition = playerModel.localPosition;
        }
        else
        {
            Debug.LogError("[VibratePlayer] playerModel Transform is not assigned!");
        }
    }

    public void StartShaking()
    {
        if (isShaking)
        {
            if (debugVibration)
                Debug.Log("[VibratePlayer] Already shaking, ignoring StartShaking call.");
            return;
        }

        if (playerModel == null)
        {
            Debug.LogError("[VibratePlayer] playerModel Transform is not assigned. Cannot shake.");
            return;
        }

        isShaking = true;
        shakeCoroutine = StartCoroutine(ShakeCoroutine());

        if (debugVibration)
            Debug.Log("[VibratePlayer] Started shaking.");
    }

    public void StopShaking()
    {
        if (!isShaking)
        {
            if (debugVibration)
                Debug.Log("[VibratePlayer] Not shaking, ignoring StopShaking call.");
            return;
        }

        isShaking = false;

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        // Immediately reset position
        if (playerModel != null)
        {
            playerModel.localPosition = initialPosition;
        }

        if (debugVibration)
            Debug.Log("[VibratePlayer] Stopped shaking.");
    }

    private IEnumerator ShakeCoroutine()
    {
        while (isShaking && playerModel != null)
        {
            // Calculate a random offset for the shake using Perlin noise
            Vector3 randomOffset = new Vector3(
                Mathf.PerlinNoise(Time.time * shakeSpeed, 0) * 2 - 1,
                Mathf.PerlinNoise(0, Time.time * shakeSpeed) * 2 - 1,
                Mathf.PerlinNoise(Time.time * shakeSpeed, Time.time * shakeSpeed) * 2 - 1
            ) * shakeMagnitude;

            playerModel.localPosition = initialPosition + randomOffset;
            yield return null;
        }

        // Reset the model's position back to its original state
        if (playerModel != null)
        {
            playerModel.localPosition = initialPosition;
        }
    }

    // Method to trigger phone vibration
    public void VibratePhone()
    {
        // Check if the device supports vibration
        if (SystemInfo.supportsVibration)
        {
            Handheld.Vibrate();
            if (debugVibration)
                Debug.Log("[VibratePlayer] Phone vibration triggered.");
        }
        else
        {
            if (debugVibration)
                Debug.Log("[VibratePlayer] Device does not support vibration.");
        }
    }

    // Public getter to check if currently shaking
    public bool IsShaking()
    {
        return isShaking;
    }

    // Method to force reset if something goes wrong
    public void ForceReset()
    {
        StopShaking();
        if (playerModel != null)
        {
            playerModel.localPosition = initialPosition;
        }
        if (debugVibration)
            Debug.Log("[VibratePlayer] Force reset performed.");
    }

    void OnDisable()
    {
        // Clean up when the component is disabled
        StopShaking();
    }
}