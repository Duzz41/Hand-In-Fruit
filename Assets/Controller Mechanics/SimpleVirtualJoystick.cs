using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SimpleVirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private float joystickRange = 50f;
    [SerializeField] private bool hideWhenNotPressed = true;
    [SerializeField] private float fadeSpeed = 5f; // Speed of fade in/out animation
    [SerializeField] private bool moveToTouchPosition = true; // Move joystick to touch position

    [Header("Visual Components")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;

    [Header("Player Controller")]
    [SerializeField] private PlayerController playerController;

    // Private variables
    private Canvas parentCanvas;
    private Camera uiCamera;
    private Vector2 inputVector;
    private bool isDragging = false;

    // Components for hiding behavior
    private CanvasGroup joystickCanvasGroup;
    private bool isVisible = true;

    // Touch area and positioning
    private RectTransform touchArea; // Full-screen touch detection area
    private Vector2 originalJoystickPosition;
    private bool shouldResetPosition = false; // Flag to reset position after fade out

    void Start()
    {
        InitializeComponents();
        SetupTouchArea();
        SetupHidingBehavior();

        // Auto-find player controller if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
    }

    void InitializeComponents()
    {
        // Get canvas component
        parentCanvas = GetComponentInParent<Canvas>();
        uiCamera = parentCanvas.worldCamera;

        // Auto-assign components if not set
        if (joystickBackground == null)
            joystickBackground = transform.GetChild(0).GetComponent<RectTransform>();

        if (joystickHandle == null)
            joystickHandle = joystickBackground.GetChild(0).GetComponent<RectTransform>();

        // Store original joystick position
        originalJoystickPosition = joystickBackground.anchoredPosition;
    }

    void SetupTouchArea()
    {
        // Use this GameObject's RectTransform as the touch area
        touchArea = GetComponent<RectTransform>();

        // Make the touch area cover the entire screen
        touchArea.anchorMin = Vector2.zero;
        touchArea.anchorMax = Vector2.one;
        touchArea.offsetMin = Vector2.zero;
        touchArea.offsetMax = Vector2.zero;
        touchArea.anchoredPosition = Vector2.zero;

        // Ensure this GameObject can receive input events
        if (GetComponent<Image>() == null)
        {
            Image touchImage = gameObject.AddComponent<Image>();
            touchImage.color = new Color(1, 1, 1, 0); // Fully transparent
            touchImage.raycastTarget = true;
        }
    }

    void SetupHidingBehavior()
    {
        // Add CanvasGroup component for proper hiding behavior
        joystickCanvasGroup = GetComponent<CanvasGroup>();
        if (joystickCanvasGroup == null)
        {
            joystickCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Set initial visibility
        if (hideWhenNotPressed)
        {
            // Keep it invisible but still able to receive input
            joystickCanvasGroup.alpha = 0f;
            joystickCanvasGroup.interactable = true;  // Always keep interactable for input
            joystickCanvasGroup.blocksRaycasts = true; // Always keep raycast blocking for input
        }
        else
        {
            SetJoystickVisibility(true, true);
        }
    }

    void Update()
    {
        // Handle smooth fading animation
        if (hideWhenNotPressed)
        {
            float targetAlpha = isDragging ? 1f : 0f;
            float previousAlpha = joystickCanvasGroup.alpha;
            joystickCanvasGroup.alpha = Mathf.MoveTowards(joystickCanvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

            // Reset position after fade out is complete
            if (shouldResetPosition && joystickCanvasGroup.alpha <= 0.01f && previousAlpha > 0.01f)
            {
                joystickBackground.anchoredPosition = originalJoystickPosition;
                shouldResetPosition = false;
            }

            // Always keep interactable and raycast blocking enabled for input detection
            joystickCanvasGroup.interactable = true;
            joystickCanvasGroup.blocksRaycasts = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Show joystick when pressed
        if (hideWhenNotPressed)
        {
            SetJoystickVisibility(true, false);
        }

        // Move joystick to touch position if enabled
        if (moveToTouchPosition)
        {
            Vector2 localTouchPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                touchArea,
                eventData.position,
                uiCamera,
                out localTouchPoint
            );

            joystickBackground.anchoredPosition = localTouchPoint;
        }

        // Reset handle to center when joystick appears
        joystickHandle.anchoredPosition = Vector2.zero;

        isDragging = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Convert screen point to local point in joystick background
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            uiCamera,
            out localPoint
        );

        // Calculate input vector
        Vector2 deltaFromCenter = localPoint;
        float distance = deltaFromCenter.magnitude;

        // Clamp to joystick range
        if (distance > joystickRange)
        {
            deltaFromCenter = deltaFromCenter.normalized * joystickRange;
        }

        // Update handle position
        joystickHandle.anchoredPosition = deltaFromCenter;

        // Calculate normalized input vector (-1 to 1)
        inputVector = deltaFromCenter / joystickRange;

        // Send input directly to player controller
        if (playerController != null)
        {
            playerController.SetJoystickInput(inputVector);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

        // Reset handle to center
        joystickHandle.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;

        // Mark that position should reset after fade out (only if moved to touch position)
        if (moveToTouchPosition)
        {
            shouldResetPosition = true;
        }

        // Hide joystick if specified (position will reset after fade out)
        if (hideWhenNotPressed)
        {
            SetJoystickVisibility(false, false);
        }

        // Send zero input to player controller
        if (playerController != null)
        {
            playerController.SetJoystickInput(Vector2.zero);
        }
    }

    void SetJoystickVisibility(bool visible, bool immediate = false)
    {
        isVisible = visible;

        if (immediate)
        {
            joystickCanvasGroup.alpha = visible ? 1f : 0f;
        }

        // Always keep these enabled for input detection when hiding is enabled
        if (hideWhenNotPressed)
        {
            joystickCanvasGroup.interactable = true;
            joystickCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            joystickCanvasGroup.interactable = visible;
            joystickCanvasGroup.blocksRaycasts = visible;
        }
    }

    // Public methods for external access
    public Vector2 GetInputVector()
    {
        return inputVector;
    }

    public bool IsPressed()
    {
        return isDragging;
    }

    public bool IsVisible()
    {
        return isVisible;
    }

    // Method to manually show/hide joystick (useful for other scripts)
    public void ShowJoystick()
    {
        if (hideWhenNotPressed && !isDragging)
        {
            SetJoystickVisibility(true, false);
        }
    }

    public void HideJoystick()
    {
        if (hideWhenNotPressed && !isDragging)
        {
            SetJoystickVisibility(false, false);
        }
    }

    // Method to manually set joystick position
    public void SetJoystickPosition(Vector2 position)
    {
        joystickBackground.anchoredPosition = position;
    }

    // Method to reset joystick to original position
    public void ResetJoystickPosition()
    {
        joystickBackground.anchoredPosition = originalJoystickPosition;
    }
}