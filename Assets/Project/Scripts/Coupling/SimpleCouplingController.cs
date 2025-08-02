using UnityEngine;
using UnityEngine.VFX;
using TMPro;
using UnityEngine.UI;
using System;

public class SimpleCouplingController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform maleHalf;
    [SerializeField] private Transform femaleHalf;
    [SerializeField] private Transform handle;

    [Header("Alignment Settings")]
    [SerializeField] private float positionTolerance = 0.05f;
    [SerializeField] private float rotationTolerance = 5f;
    [SerializeField] private float edgeSnapZOffset = 0.05f;

    [Header("Alignment Visuals (Cylinders)")]
    [SerializeField] private Transform maleAlignCylinder;
    [SerializeField] private Transform femaleAlignCylinder;
    [SerializeField] private Renderer maleAlignRenderer;
    [SerializeField] private Renderer femaleAlignRenderer;
    [SerializeField] private Material alignedMaterial;
    [SerializeField] private Material misalignedMaterial;

    [Header("Completion Effects")]
    [SerializeField] private VisualEffect completionEffect;

    [Header("Audio Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip alignClickSound;
    [SerializeField] private AudioClip clampLockSound;
    [SerializeField] private AudioClip flowSound;
    [SerializeField] private AudioClip errorSound;

    [Header("Floating UI Settings")]
    [SerializeField] private GameObject floatingHintUIPrefab;
    [SerializeField] private Transform alignmentUIAnchor;
    [SerializeField] private GameObject rotationArrowUI;

    [Header("Directional UI Arrows")]
    [SerializeField] private GameObject handleDirectionArrow;

    [Header("Material Change on Rotation")]
    [SerializeField] private GameObject[] objectsToChangeMaterial;
    [SerializeField] private Material rotatedStateMaterial;
    [SerializeField] private Material defaultMaterial;

    [Header("Hose Mechanism")]
    [SerializeField] private Transform pin;
    [SerializeField] private Transform spring;
    [SerializeField] private Transform valve;
    [SerializeField] private float pinMaxZMovement = 0.05f;
    [SerializeField] private float springMinZScale = 0.001492708f;
    [SerializeField] private float springMaxZScale = 0.003948962f;
    [SerializeField] private float valvePushDistance = 0.02f;

    private GameObject floatingUIInstance;
    private TextMeshProUGUI floatingText;

    private SimpleFemaleComponent femaleComponent;
    private SimpleHandleComponent handleComponent;

    private bool isConnected = false;
    private bool isCurrentlyAligning = false;
    private bool wasAlignedLastFrame = false;

    private Vector3 femaleInitialPos;
    private Quaternion femaleInitialRot;

    private string debugMessage = "Ready.";

    void Start()
    {
        Debug.Log("Initializing Coupling Controller...");

        femaleComponent = femaleHalf.GetComponent<SimpleFemaleComponent>();
        handleComponent = handle.GetComponent<SimpleHandleComponent>();

        if (femaleComponent != null)
        {
            femaleComponent.onDragUpdate = CheckLiveAlignment;
            femaleComponent.onDragEnd = TryConnectOnRelease;
        }

        if (handleComponent != null)
        {
            handleComponent.SetInteractable(false);
            handleComponent.onHandleRotationStart += HideRotationUI;
            handleComponent.onHandleRotationStop += ShowRotationUI;
            handleComponent.onHandleRotationProgress += MovePinAndInteractWithValve;
            handleComponent.onHandleUnlocked += OnHandleUnlocked;
        }

        femaleInitialPos = femaleHalf.position;
        femaleInitialRot = femaleHalf.rotation;

        CreateFloatingHintUI();
        HideAlignmentVisuals();
        HideRotationUI();
        if (handleDirectionArrow != null) handleDirectionArrow.SetActive(false);
        StartInteraction();
    }

    void Update()
    {
        if (floatingUIInstance != null && Camera.main != null)
        {
            floatingUIInstance.transform.rotation =
                Quaternion.LookRotation(floatingUIInstance.transform.position - Camera.main.transform.position);
        }

        if (rotationArrowUI != null && rotationArrowUI.activeSelf && !handleComponent.wasFullyRotated)
        {
            rotationArrowUI.transform.Rotate(-Vector3.up * 100f * Time.deltaTime);
        }

        if (rotationArrowUI != null && rotationArrowUI.activeSelf && handleComponent.wasFullyRotated)
        {
            rotationArrowUI.transform.localScale = new Vector3(-rotationArrowUI.transform.localScale.x, rotationArrowUI.transform.localScale.y, rotationArrowUI.transform.localScale.y);
            rotationArrowUI.transform.Rotate(Vector3.up * 100f * Time.deltaTime);
        }
    }

    public void StartInteraction()
    {
        debugMessage = "Drag the female connector toward the male.";
        if (femaleComponent != null) femaleComponent.EnableDragging(true);
    }

    void CheckLiveAlignment()
    {
        if (isConnected) return;

        ShowAlignmentVisuals();

        bool aligned = IsPerfectlyAligned(out float distance, out float angle);

        if (aligned && !wasAlignedLastFrame && audioSource && alignClickSound)
        {
            audioSource.PlayOneShot(alignClickSound);
        }

        wasAlignedLastFrame = aligned;

        if (floatingUIInstance != null)
        {
            floatingUIInstance.SetActive(true);
            floatingText.text = aligned
                ? "Perfect Alignment!"
                : $"Offset: {distance:F3}m\nAngle: {angle:F1}°";
            floatingText.color = aligned ? Color.green : Color.red;
        }

        if (maleAlignRenderer != null)
            maleAlignRenderer.material = aligned ? alignedMaterial : misalignedMaterial;

        if (femaleAlignRenderer != null)
            femaleAlignRenderer.material = aligned ? alignedMaterial : misalignedMaterial;

        isCurrentlyAligning = aligned;
    }

    void TryConnectOnRelease()
    {
        if (isConnected) return;

        HideAlignmentVisuals();

        if (isCurrentlyAligning)
        {
            ConnectParts();
        }
        else
        {
            debugMessage = "Release failed. Resetting.";
            Debug.Log(debugMessage);

            if (audioSource && errorSound)
                audioSource.PlayOneShot(errorSound);

            femaleHalf.position = femaleInitialPos;
            femaleHalf.rotation = femaleInitialRot;

            isCurrentlyAligning = false;

            if (floatingUIInstance != null)
                floatingUIInstance.SetActive(false);

            if (maleAlignRenderer != null)
                maleAlignRenderer.material = misalignedMaterial;

            if (femaleAlignRenderer != null)
                femaleAlignRenderer.material = misalignedMaterial;
        }
    }

    bool IsPerfectlyAligned(out float distance, out float angle)
    {
        Vector3 malePos = maleAlignCylinder.position;
        Vector3 femalePos = femaleAlignCylinder.position;
        distance = Vector3.Distance(femalePos, malePos);

        Vector3 maleForward = maleAlignCylinder.forward;
        Vector3 femaleForward = femaleAlignCylinder.forward;
        angle = Vector3.Angle(femaleForward, maleForward);

        Debug.Log($"[Cylinder Alignment] Distance: {distance:F4}, Angle: {angle:F2}");

        return distance < positionTolerance && angle < rotationTolerance;
    }

    void ConnectParts()
    {
        isConnected = true;
        debugMessage = "Connected! Now rotate the handle.";
        Debug.Log(debugMessage);

        Vector3 adjustedPosition = maleHalf.position - maleHalf.forward * edgeSnapZOffset;
        femaleHalf.position = adjustedPosition;
        femaleHalf.rotation = maleHalf.rotation;

        if (femaleComponent != null)
        {
            femaleComponent.EnableDragging(false);
            Collider col = femaleComponent.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        if (handleComponent != null)
        {
            handleComponent.SetInteractable(true);
            ApplyRotatedMaterial();
        }

        if (floatingUIInstance != null)
        {
            floatingText.text = "Rotate the Handle ➤";
            floatingText.color = Color.yellow;
            floatingUIInstance.SetActive(true);
        }

        if (handleDirectionArrow != null)
            handleDirectionArrow.SetActive(true);

        if (audioSource && clampLockSound)
            audioSource.PlayOneShot(clampLockSound);

        ShowRotationUI();
    }

    public void OnHandleLocked()
    {
        debugMessage = "Handle locked! Coupling complete!";
        Debug.Log(debugMessage);

        ApplySurfaceType(maleHalf);
        ApplySurfaceType(femaleHalf);
        ApplySurfaceType(handle);

        if (completionEffect != null)
        {
            completionEffect.gameObject.SetActive(true);
            completionEffect.Play();
        }

        if (audioSource && flowSound)
            audioSource.PlayOneShot(flowSound);

        if (floatingUIInstance != null)
        {
            floatingText.text = "Liquid Flowing! Coupling Locked ✔";
            floatingText.color = Color.cyan;
            floatingUIInstance.SetActive(true);
        }

        if (handleDirectionArrow != null)
            handleDirectionArrow.SetActive(false);

        HideRotationUI();
    }

    private void OnHandleUnlocked()
    {
        debugMessage = "Handle returned. Uncoupling...";
        Debug.Log(debugMessage);

        if (floatingUIInstance != null)
        {
            floatingText.text = "Handle Unlocked. Detach Possible.";
            floatingText.color = Color.white;
            floatingUIInstance.SetActive(true);
        }

        if (completionEffect != null)
        {
            completionEffect.Stop();
            completionEffect.gameObject.SetActive(false);
        }

        // Reset parts
        if (pin != null)
            pin.localPosition = new Vector3(pin.localPosition.x, pin.localPosition.y, 0);

        if (valve != null)
            valve.localPosition = new Vector3(valve.localPosition.x, valve.localPosition.y, 0);

        if (spring != null)
            spring.localScale = new Vector3(spring.localScale.x, spring.localScale.y, springMaxZScale);

        if (objectsToChangeMaterial != null && rotatedStateMaterial != null && defaultMaterial != null)
        {
            foreach (GameObject obj in objectsToChangeMaterial)
            {
                Renderer rend = obj.GetComponent<Renderer>();
                if (rend != null)
                    rend.material = defaultMaterial;
            }
        }

        isConnected = false;
        handleComponent.SetInteractable(false);

        if (femaleComponent != null)
        {
            Collider col = femaleComponent.GetComponent<Collider>();
            if (col != null) col.enabled = true;
            femaleComponent.EnableDragging(true);
        }
    }

    private void MovePinAndInteractWithValve(float normalizedRotation)
    {
        if (pin != null)
        {
            Vector3 pinLocal = pin.localPosition;
            pinLocal.z = -pinMaxZMovement * normalizedRotation;
            pin.localPosition = pinLocal;
            pin.localRotation = Quaternion.Euler(Vector3.forward * 360f * normalizedRotation);
        }

        if (spring != null)
        {
            Vector3 springScale = spring.localScale;
            springScale.z = Mathf.Lerp(springMaxZScale, springMinZScale, normalizedRotation);
            spring.localScale = springScale;
        }

        if (valve != null)
        {
            Vector3 valveLocal = valve.localPosition;
            valveLocal.z = -valvePushDistance * normalizedRotation;
            valve.localPosition = valveLocal;
        }
    }

    private void ApplySurfaceType(Transform target)
    {
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 1f);
                    mat.renderQueue = 3000;
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
                }
            }
        }

        Debug.Log($"Updated material surface for: {target.name}");
    }

    private void ApplyRotatedMaterial()
    {
        if (rotatedStateMaterial == null || objectsToChangeMaterial == null) return;

        foreach (GameObject obj in objectsToChangeMaterial)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = rotatedStateMaterial;
            }
        }

        Debug.Log("Rotated material applied to target objects.");
    }

    private void CreateFloatingHintUI()
    {
        if (floatingHintUIPrefab == null || alignmentUIAnchor == null)
        {
            Debug.LogWarning("FloatingHintUIPrefab or AlignmentUIAnchor not assigned.");
            return;
        }

        floatingUIInstance = Instantiate(floatingHintUIPrefab, alignmentUIAnchor.position, Quaternion.identity, alignmentUIAnchor);
        floatingText = floatingUIInstance.GetComponentInChildren<TextMeshProUGUI>();
        floatingText.text = "";
        floatingText.fontSize = 5f;
        floatingUIInstance.transform.localScale = Vector3.one * 0.3f;
        floatingUIInstance.SetActive(false);
    }

    private void ShowAlignmentVisuals()
    {
        if (maleAlignRenderer != null)
            maleAlignRenderer.enabled = true;
        if (femaleAlignRenderer != null)
            femaleAlignRenderer.enabled = true;
    }

    private void HideAlignmentVisuals()
    {
        if (maleAlignRenderer != null)
            maleAlignRenderer.enabled = false;
        if (femaleAlignRenderer != null)
            femaleAlignRenderer.enabled = false;
    }

    public void ShowRotationUI()
    {
        if (rotationArrowUI != null)
            rotationArrowUI.SetActive(true);
    }

    public void HideRotationUI()
    {
        if (rotationArrowUI != null)
            rotationArrowUI.SetActive(false);
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.green;
        GUI.Label(new Rect(10, 10, 1000, 30), $"[DEBUG] {debugMessage}", style);
    }
}