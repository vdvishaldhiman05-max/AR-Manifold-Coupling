using UnityEngine;
using UnityEngine.VFX;
using TMPro;
using UnityEngine.UI;
using System;

public class SimpleHandleComponent : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float maxRotation = 90f;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    [SerializeField] private float rotationSpeed = 100f;

    private Camera arCamera;
    private bool canRotate = false;
    private bool isDragging = false;
    private float currentRotation = 0f;
    private Vector3 lastMousePos;
    private SimpleCouplingController controller;

    private string debugMessage = "Waiting...";

    public float CurrentRotation => currentRotation;

    public event Action onHandleRotationStart;
    public event Action onHandleRotationStop;
    public event Action<float> onHandleRotationProgress;
    public event Action onHandleUnlocked;

    public bool wasFullyRotated = false;

    void Start()
    {
        arCamera = Camera.main;
        controller = GetComponentInParent<SimpleCouplingController>();

        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(0.4f, 0.4f, 0.3f);
            Debug.Log("Collider added to handle.");
        }
    }

    void Update()
    {
        if (!canRotate && !wasFullyRotated) return;

        HandleInput();

        if (!wasFullyRotated && currentRotation >= maxRotation - 1f)
        {
            OnFullyRotated();
        }

        if (wasFullyRotated && currentRotation <= 0.01f)
        {
            OnFullyUnlocked();
        }
    }

    void HandleInput()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            TryStartRotation(Input.mousePosition);
        else if (Input.GetMouseButton(0) && isDragging)
            UpdateRotation(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0) && isDragging)
            EndRotation();
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TryStartRotation(touch.position);
                    break;
                case TouchPhase.Moved:
                    if (isDragging) UpdateRotation(touch.position);
                    break;
                case TouchPhase.Ended:
                    if (isDragging) EndRotation();
                    break;
            }
        }
#endif
    }

    void TryStartRotation(Vector3 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform == transform)
            {
                isDragging = true;
                lastMousePos = screenPos;
                debugMessage = "Started rotating handle";
                onHandleRotationStart?.Invoke();
            }
            else
            {
                debugMessage = $"Touched object: {hit.transform.name}";
            }
        }
        else
        {
            debugMessage = "Raycast missed everything.";
        }
    }

    void UpdateRotation(Vector3 screenPos)
    {
        float deltaX = screenPos.x - lastMousePos.x;
        float rotationDelta = deltaX * rotationSpeed * Time.deltaTime;

        if (rotationDelta > 0 || currentRotation > 0)
        {
            currentRotation = Mathf.Clamp(currentRotation + rotationDelta, 0, maxRotation);
            transform.localRotation = Quaternion.Euler(rotationAxis * currentRotation);
            debugMessage = $"Rotating... {currentRotation:F1}°";
            onHandleRotationProgress?.Invoke(currentRotation / maxRotation);
        }

        lastMousePos = screenPos;
    }

    void EndRotation()
    {
        isDragging = false;
        debugMessage = "Stopped rotating.";
        onHandleRotationStop?.Invoke();
    }

    void OnFullyRotated()
    {
        if (!wasFullyRotated)
        {
            wasFullyRotated = true;
            canRotate = true;
            currentRotation = maxRotation;
            transform.localRotation = Quaternion.Euler(rotationAxis * maxRotation);

            if (controller != null)
            {
                controller.OnHandleLocked();
            }

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.green;
            }

            debugMessage = "Rotation complete! Coupling locked.";
        }
    }

    void OnFullyUnlocked()
    {
        wasFullyRotated = false;
        debugMessage = "Handle fully returned. Unlocked.";
        onHandleUnlocked?.Invoke();
    }

    public void SetInteractable(bool interactable)
    {
        canRotate = interactable;

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = interactable ? Color.cyan : Color.gray;
        }

        debugMessage = interactable ? "Handle is now rotatable." : "Handle is locked.";
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.magenta;
        GUI.Label(new Rect(10, 60, 600, 40), $"[HANDLE DEBUG] {debugMessage}", style);
    }
}
