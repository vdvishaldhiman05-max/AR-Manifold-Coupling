using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class SimpleARManager : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Placement")]
    [SerializeField] private GameObject placementIndicatorPrefab;
    [SerializeField] private GameObject couplingPrefab; // Your 3D model

    private GameObject placementIndicator;
    private GameObject spawnedObject;
    private Camera arCamera;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private string debugMessage = "Initializing AR...";

    void Start()
    {
        arCamera = Camera.main;

        if (arCamera == null)
        {
            Debug.LogError("AR Camera not found! Make sure your AR camera has the 'MainCamera' tag.");
            debugMessage = "Error: No MainCamera!";
        }

        // Create placement indicator
        if (placementIndicatorPrefab != null)
        {
            placementIndicator = Instantiate(placementIndicatorPrefab);
            placementIndicator.SetActive(false);
            debugMessage = "Placement indicator ready.";
        }
        else
        {
            debugMessage = "Error: No placement indicator prefab assigned!";
        }
    }

    void Update()
    {
        if (spawnedObject == null)
        {
            UpdatePlacementIndicator();

            // Touch to place
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                PlaceObject();
            }

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0) && placementIndicator.activeSelf)
            {
                PlaceObject();
            }
#endif
        }
    }

    void UpdatePlacementIndicator()
    {
        var screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.position = hits[0].pose.position;
            placementIndicator.transform.rotation = hits[0].pose.rotation;
            debugMessage = "Plane detected. Tap to place.";
        }
        else
        {
            placementIndicator.SetActive(false);
            debugMessage = "Searching for a plane...";
        }
    }

    void PlaceObject()
    {
        if (placementIndicator.activeSelf && couplingPrefab != null)
        {
            Quaternion placementRotation = Quaternion.Euler(0, arCamera.transform.eulerAngles.y + 90f, 0);
            spawnedObject = Instantiate(couplingPrefab,
                placementIndicator.transform.position,
                placementRotation);

            placementIndicator.SetActive(false);
            debugMessage = "Placed object. Interaction started.";

            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }

            var controller = spawnedObject.GetComponent<SimpleCouplingController>();
            if (controller != null)
            {
                controller.StartInteraction();
            }
            else
            {
                debugMessage = "Placed but no SimpleCouplingController found!";
                Debug.LogWarning("Coupling prefab is missing the SimpleCouplingController script.");
            }
        }
        else
        {
            debugMessage = "Cannot place: Indicator inactive or prefab missing.";
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.green;
        GUI.Label(new Rect(10, 10, 1000, 40), $"[AR DEBUG] {debugMessage}", style);
    }
}
