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

    [Header("Phone Vibration Settings")]
    [Tooltip("Interval between vibration pulses for continuous vibration (in seconds).")]
    [SerializeField] private float vibrationInterval = 0.1f;
    [Tooltip("Enable continuous phone vibration while drilling.")]
    [SerializeField] private bool enableContinuousVibration = true;

    [Header("Debug Settings")]
    [SerializeField] private bool debugVibration = true;

    private Vector3 initialPosition;
    private bool isShaking = false;
    private bool isVibratingPhone = false;
    private Coroutine shakeCoroutine;
    private Coroutine vibrationCoroutine;

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

        // Start continuous phone vibration if enabled
        if (enableContinuousVibration)
        {
            StartContinuousVibration();
        }

        if (debugVibration)
            Debug.Log("[VibratePlayer] Started shaking and continuous vibration.");
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

        // Stop continuous phone vibration
        StopContinuousVibration();

        // Immediately reset position
        if (playerModel != null)
        {
            playerModel.localPosition = initialPosition;
        }

        if (debugVibration)
            Debug.Log("[VibratePlayer] Stopped shaking and vibration.");
    }

    private void StartContinuousVibration()
    {
        if (isVibratingPhone || !SystemInfo.supportsVibration)
            return;

        isVibratingPhone = true;
        vibrationCoroutine = StartCoroutine(ContinuousVibrationCoroutine());

        if (debugVibration)
            Debug.Log("[VibratePlayer] Started continuous phone vibration.");
    }

    private void StopContinuousVibration()
    {
        if (!isVibratingPhone)
            return;

        isVibratingPhone = false;

        if (vibrationCoroutine != null)
        {
            StopCoroutine(vibrationCoroutine);
            vibrationCoroutine = null;
        }

        if (debugVibration)
            Debug.Log("[VibratePlayer] Stopped continuous phone vibration.");
    }

    private IEnumerator ContinuousVibrationCoroutine()
    {
        while (isVibratingPhone && SystemInfo.supportsVibration)
        {
            Handheld.Vibrate();
            yield return new WaitForSeconds(vibrationInterval);
        }
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

    // Method to trigger single phone vibration (for completion events)
    public void VibratePhone()
    {
        // Check if the device supports vibration
        if (SystemInfo.supportsVibration)
        {
            Handheld.Vibrate();
            if (debugVibration)
                Debug.Log("[VibratePlayer] Single phone vibration triggered.");
        }
        else
        {
            if (debugVibration)
                Debug.Log("[VibratePlayer] Device does not support vibration.");
        }
    }

    // Public getters
    public bool IsShaking()
    {
        return isShaking;
    }

    public bool IsVibratingPhone()
    {
        return isVibratingPhone;
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

    // Method to adjust vibration settings at runtime
    public void SetVibrationInterval(float interval)
    {
        vibrationInterval = Mathf.Max(0.05f, interval); // Minimum 50ms interval
        if (debugVibration)
            Debug.Log($"[VibratePlayer] Vibration interval set to {vibrationInterval:F2}s");
    }

    public void SetContinuousVibrationEnabled(bool enabled)
    {
        enableContinuousVibration = enabled;

        if (!enabled && isVibratingPhone)
        {
            StopContinuousVibration();
        }
        else if (enabled && isShaking && !isVibratingPhone)
        {
            StartContinuousVibration();
        }

        if (debugVibration)
            Debug.Log($"[VibratePlayer] Continuous vibration {(enabled ? "enabled" : "disabled")}");
    }

    void OnDisable()
    {
        // Clean up when the component is disabled
        StopShaking();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // Stop vibration when app is paused/backgrounded
        if (pauseStatus)
        {
            StopContinuousVibration();
        }
        else if (isShaking && enableContinuousVibration)
        {
            // Resume vibration when app becomes active again
            StartContinuousVibration();
        }
    }
}