using UnityEngine;
using System;

public class SimpleFemaleComponent : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private bool constrainToHorizontal = true;
    [SerializeField] private float dragSpeed = 10f;
    [SerializeField] private float maxDragDistance = 1f;

    public Action onDragUpdate;
    public Action onDragEnd; // ✅ New delegate for drag end

    private Camera arCamera;
    private bool canDrag = false;
    private bool isDragging = false;
    private Vector3 startPosition;
    private float fixedY;
    private string debugMessage = "Waiting...";

    void Start()
    {
        arCamera = Camera.main;
        if (arCamera == null)
        {
            Debug.LogError("AR Camera not found! Make sure it's tagged as 'MainCamera'.");
            debugMessage = "Error: No MainCamera!";
        }

        startPosition = transform.position;
        fixedY = transform.position.y;

        // Add collider if missing
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.size = Vector3.one * 0.2f;
            Debug.Log("BoxCollider added for interaction.");
        }
    }

    void Update()
    {
        if (!canDrag) return;
        HandleInput();
    }

    void HandleInput()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            TryStartDrag(Input.mousePosition);
        else if (Input.GetMouseButton(0) && isDragging)
            UpdateDrag(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0) && isDragging)
            EndDrag();
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TryStartDrag(touch.position);
                    break;
                case TouchPhase.Moved:
                    if (isDragging) UpdateDrag(touch.position);
                    break;
                case TouchPhase.Ended:
                    if (isDragging) EndDrag();
                    break;
            }
        }
#endif
    }

    void TryStartDrag(Vector3 screenPos)
    {
        if (arCamera == null) return;

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                isDragging = true;
                debugMessage = "Dragging started!";
                Debug.Log("Started dragging female component");
            }
            else
            {
                debugMessage = $"Hit something else: {hit.transform.name}";
            }
        }
        else
        {
            debugMessage = "Raycast missed everything.";
        }
    }

    void UpdateDrag(Vector3 screenPos)
    {
        if (arCamera == null) return;

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, fixedY, 0));
        if (dragPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 targetPos = hitPoint;

            if (constrainToHorizontal)
                targetPos.y = fixedY;

            Vector3 offset = targetPos - startPosition;
            if (offset.magnitude > maxDragDistance)
                targetPos = startPosition + offset.normalized * maxDragDistance;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * dragSpeed);

            onDragUpdate?.Invoke(); // ✅ Continuous alignment feedback
            debugMessage = "Dragging in progress...";
        }
    }

    void EndDrag()
    {
        isDragging = false;
        debugMessage = "Drag ended.";
        Debug.Log("Ended dragging");

        onDragEnd?.Invoke(); // ✅ Trigger final alignment check
    }

    public void EnableDragging(bool enable)
    {
        canDrag = enable;

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = enable ? Color.yellow : Color.white;
        }

        debugMessage = enable ? "Dragging enabled." : "Dragging disabled.";
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(10, 10, 600, 40), $"[DEBUG] {debugMessage}", style);
    }
}


//using UnityEngine;
//using System;

//public class SimpleFemaleComponent : MonoBehaviour
//{
//    [Header("Drag Settings")]
//    [SerializeField] private bool constrainToHorizontal = true;
//    [SerializeField] private float dragSpeed = 10f;
//    [SerializeField] private float maxDragDistance = 1f;

//    public Action onDragUpdate;

//    private Camera arCamera;
//    private bool canDrag = false;
//    private bool isDragging = false;
//    private Vector3 startPosition;
//    private float fixedX;
//    private string debugMessage = "Waiting...";

//    void Start()
//    {
//        arCamera = Camera.main;
//        if (arCamera == null)
//        {
//            Debug.LogError("AR Camera not found! Make sure it's tagged as 'MainCamera'.");
//            debugMessage = "Error: No MainCamera!";
//        }

//        startPosition = transform.position;
//        fixedX = transform.position.x;

//        // Add collider if missing
//        if (GetComponent<Collider>() == null)
//        {
//            BoxCollider col = gameObject.AddComponent<BoxCollider>();
//            col.size = Vector3.one * 0.2f;
//            Debug.Log("BoxCollider added for interaction.");
//        }
//    }

//    void Update()
//    {
//        if (!canDrag) return;
//        HandleInput();
//    }

//    void HandleInput()
//    {
//#if UNITY_EDITOR
//        // Mouse input for testing
//        if (Input.GetMouseButtonDown(0))
//            TryStartDrag(Input.mousePosition);
//        else if (Input.GetMouseButton(0) && isDragging)
//            UpdateDrag(Input.mousePosition);
//        else if (Input.GetMouseButtonUp(0) && isDragging)
//            EndDrag();
//#else
//        // Touch input
//        if (Input.touchCount > 0)
//        {
//            Touch touch = Input.GetTouch(0);
//            switch (touch.phase)
//            {
//                case TouchPhase.Began:
//                    TryStartDrag(touch.position);
//                    break;
//                case TouchPhase.Moved:
//                    if (isDragging) UpdateDrag(touch.position);
//                    break;
//                case TouchPhase.Ended:
//                    if (isDragging) EndDrag();
//                    break;
//            }
//        }
//#endif
//    }

//    void TryStartDrag(Vector3 screenPos)
//    {
//        if (arCamera == null) return;

//        Ray ray = arCamera.ScreenPointToRay(screenPos);
//        RaycastHit hit;

//        if (Physics.Raycast(ray, out hit))
//        {
//            if (hit.transform == transform || hit.transform.IsChildOf(transform))
//            {
//                isDragging = true;
//                debugMessage = "Dragging started!";
//                Debug.Log("Started dragging female component");
//            }
//            else
//            {
//                debugMessage = $"Hit something else: {hit.transform.name}";
//            }
//        }
//        else
//        {
//            debugMessage = "Raycast missed everything.";
//        }
//    }

//    void UpdateDrag(Vector3 screenPos)
//    {
//        if (arCamera == null) return;

//        Ray ray = arCamera.ScreenPointToRay(screenPos);

//        // Create a vertical plane facing right, fixed on X-axis
//        Plane dragPlane = new Plane(Vector3.right, new Vector3(fixedX, 0, 0));
//        float distance;

//        if (dragPlane.Raycast(ray, out distance))
//        {
//            Vector3 hitPoint = ray.GetPoint(distance);
//            Vector3 targetPos = hitPoint;

//            if (constrainToHorizontal)
//                targetPos.x = fixedX;

//            Vector3 offset = targetPos - startPosition;
//            if (offset.magnitude > maxDragDistance)
//                targetPos = startPosition + offset.normalized * maxDragDistance;

//            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * dragSpeed);

//            onDragUpdate?.Invoke();
//            debugMessage = "Dragging in progress...";
//        }
//    }

//    void EndDrag()
//    {
//        isDragging = false;
//        debugMessage = "Drag ended.";
//        Debug.Log("Ended dragging");
//    }

//    public void EnableDragging(bool enable)
//    {
//        canDrag = enable;

//        var renderer = GetComponent<Renderer>();
//        if (renderer != null)
//        {
//            renderer.material.color = enable ? Color.yellow : Color.white;
//        }

//        debugMessage = enable ? "Dragging enabled." : "Dragging disabled.";
//    }

//    void OnGUI()
//    {
//        GUIStyle style = new GUIStyle(GUI.skin.label);
//        style.fontSize = 20;
//        style.normal.textColor = Color.cyan;
//        GUI.Label(new Rect(10, 10, 600, 40), $"[DEBUG] {debugMessage}", style);
//    }
//}