using UnityEngine;

/// <summary>
/// Control de cámara de tercera persona con retroceso (recoil) y cambio de FOV al sprintar.
/// Colocar en la cámara. Asignar playerBody al transform del jugador.
/// </summary>
public class MouseLook : MonoBehaviour
{
    [Header("Sensibilidad")]
    public Transform playerBody;
    public float sensitivity = 2f;
    public bool invertY = false;

    [Header("FOV")]
    public float normalFOV = 60f;
    public float sprintFOV = 72f;
    public float fovSpeed = 8f;

    [Header("Tercera persona")]
    public bool thirdPerson = true;
    public Vector3 thirdPersonOffset = new Vector3(3.1f, 8.0f, -12.0f);
    [Range(-45f, 45f)] public float thirdPersonPitch = 33f;
    [Header("Zoom con rueda")]
    public float zoomSpeed = 3.5f;
    public float minZoomDistance = 7f;
    public float maxZoomDistance = 22f;
    public float zoomSmoothness = 12f;

    private float xRotation = 0f;

    // Sistema de retroceso
    private float recoilAmount = 0f;
    private float recoilRecoverySpeed = 8f;

    private Camera cam;
    private PlayerMovement3D playerMovement;
    private Vector3 zoomDirection;
    private float targetZoomDistance;

    void Awake()
    {
        // Actualiza también cámaras que ya estaban serializadas en la escena.
        thirdPerson = true;
        thirdPersonOffset = new Vector3(3.1f, 8.0f, -12.0f);
        thirdPersonPitch = 33f;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (thirdPerson)
        {
            transform.localPosition = thirdPersonOffset;
            zoomDirection = thirdPersonOffset.normalized;
            targetZoomDistance = Mathf.Clamp(thirdPersonOffset.magnitude, minZoomDistance, maxZoomDistance);
            xRotation = thirdPersonPitch;
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            if (cam != null) cam.nearClipPlane = 0.12f;
        }
        if (playerBody == null && transform.parent != null)
            playerBody = transform.parent;
        if (playerBody != null)
            playerMovement = playerBody.GetComponent<PlayerMovement3D>();
    }

    void LateUpdate()
    {
        if (Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked)
            return;

        // Recuperar retroceso
        if (recoilAmount > 0f)
        {
            float recovery = recoilRecoverySpeed * Time.deltaTime;
            xRotation += recovery;
            recoilAmount -= recovery;
            if (recoilAmount < 0f)
            {
                xRotation += recoilAmount; // corregir exceso
                recoilAmount = 0f;
            }
        }

        // Input del mouse
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        xRotation += invertY ? mouseY : -mouseY;
        xRotation = Mathf.Clamp(xRotation, thirdPerson ? 20f : -85f, thirdPerson ? 70f : 85f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);

        // Rueda arriba acerca; rueda abajo aleja la cámara. El ángulo cenital
        // se conserva para que el personaje siga visible en tercera persona.
        if (thirdPerson)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                targetZoomDistance = Mathf.Clamp(targetZoomDistance - scroll * zoomSpeed * 6f, minZoomDistance, maxZoomDistance);
            // Orbitar alrededor del torso conserva al héroe en pantalla a cualquier zoom.
            Vector3 desiredOffset = Vector3.up * 1.1f +
                Quaternion.Euler(xRotation, 0f, 0f) * Vector3.back * targetZoomDistance;
            transform.localPosition = Vector3.Lerp(transform.localPosition, desiredOffset, zoomSmoothness * Time.deltaTime);
            thirdPersonOffset = desiredOffset;
        }

        // Cambio de FOV al sprintar
        if (cam != null && playerMovement != null)
        {
            float targetFOV = playerMovement.isSprinting ? sprintFOV : normalFOV;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Llamar desde PlayerShooting3D para aplicar retroceso visual.
    /// </summary>
    public void AddRecoil(float amount)
    {
        recoilAmount += amount;
        xRotation -= amount;
    }
}
