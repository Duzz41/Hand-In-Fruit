using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SimpleVirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private float joystickRange = 50f;
    [SerializeField] private bool snapToFinger = true;
    [SerializeField] private bool hideWhenNotPressed = false;

    [Header("Visual Components")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;

    [Header("Player Controller")]
    [SerializeField] private MobilePlayerController playerController;

    // Private variables
    private Canvas parentCanvas;
    private Camera uiCamera;
    private Vector2 joystickCenter;
    private Vector2 inputVector;
    private bool isDragging = false;

    // Original positions for reset
    private Vector2 originalBackgroundPosition;

    void Start()
    {
        InitializeComponents();
        SetupInitialPositions();

        // Auto-find player controller if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<MobilePlayerController>();
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
    }

    void SetupInitialPositions()
    {
        originalBackgroundPosition = joystickBackground.anchoredPosition;
        joystickCenter = joystickBackground.anchoredPosition;

        if (hideWhenNotPressed)
        {
            SetJoystickVisibility(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (hideWhenNotPressed)
        {
            SetJoystickVisibility(true);
        }

        // Convert screen point to local point
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground.parent as RectTransform,
            eventData.position,
            uiCamera,
            out localPoint
        );

        // Move joystick to finger position if snap to finger is enabled
        if (snapToFinger)
        {
            joystickBackground.anchoredPosition = localPoint;
            joystickCenter = localPoint;
        }

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

        // Reset joystick position if snap to finger was used
        if (snapToFinger)
        {
            joystickBackground.anchoredPosition = originalBackgroundPosition;
            joystickCenter = originalBackgroundPosition;
        }

        // Hide joystick if specified
        if (hideWhenNotPressed)
        {
            SetJoystickVisibility(false);
        }

        // Send zero input to player controller
        if (playerController != null)
        {
            playerController.SetJoystickInput(Vector2.zero);
        }
    }

    void SetJoystickVisibility(bool visible)
    {
        joystickBackground.gameObject.SetActive(visible);
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
}