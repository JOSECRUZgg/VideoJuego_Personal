using UnityEngine;

/// <summary>
/// Movimiento de tercera persona: WASD, sprint, agacharse, salto y seguimiento de cámara.
/// Requiere CharacterController en el mismo GameObject.
/// </summary>
public class PlayerMovement3D : MonoBehaviour
{
    [Header("Velocidades")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3f;

    [Header("Salto y gravedad")]
    public float gravity = -20f;
    public float jumpHeight = 1.5f;

    [Header("Agacharse")]
    public float standHeight = 2f;
    public float crouchHeight = 1.2f;
    public float crouchCenterY = 0.6f;
    public float standCenterY = 1f;

    [Header("Head Bob")]
    public float bobFrequency = 8f;
    public float bobAmplitude = 0.04f;

    // Estado público para HUD
    [HideInInspector] public float currentSpeed;
    public float ActualSpeed { get; private set; }
    [HideInInspector] public bool isSprinting;
    [HideInInspector] public bool isCrouching;
    [HideInInspector] public bool isGrounded;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 knockbackVelocity;
    private float bobTimer;
    private Transform camTransform;
    private float baseCamY;
    private Vector3 lastSafeGroundPosition;
    private bool hasSafeGroundPosition;

    /// <summary>Aplica un leve empujón al recibir un impacto.</summary>
    public void ApplyKnockback(Vector3 direction, float force = 7f)
    {
        direction.y = 0.2f; // Ligero levantamiento
        knockbackVelocity = direction.normalized * force;
    }

    void Awake()
    {
        if (GetComponent<PlayerCharacterVisual>() == null)
            gameObject.AddComponent<PlayerCharacterVisual>();
        HidePlayerBodyMesh();
    }

    void Start()
    {
        HidePlayerBodyMesh();
        controller = GetComponent<CharacterController>();
        // Buscar la cámara hija
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            camTransform = cam.transform;
            MouseLook look = cam.GetComponent<MouseLook>();
            if (look != null && look.thirdPerson)
                camTransform.localPosition = look.thirdPersonOffset;
            baseCamY = camTransform.localPosition.y;
            cam.nearClipPlane = 0.05f; // Evitar cualquier recorte cercano con armas
        }
    }

    void HidePlayerBodyMesh()
    {
        Transform body = transform.Find("Body");
        if (body != null)
        {
            Renderer rend = body.GetComponent<Renderer>();
            if (rend != null)
            {
                // El avatar detallado sustituye a la cápsula temporal.
                rend.enabled = false;
            }
        }
    }

    public void ResetMovement(Vector3 position)
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null) controller.enabled = false;
        transform.position = position;
        velocity = Vector3.zero;
        knockbackVelocity = Vector3.zero;
        currentSpeed = 0f;
        ActualSpeed = 0f;
        hasSafeGroundPosition = ArenaEnricher.Instance != null &&
            ArenaEnricher.Instance.TryGetSnowHeight(position, out _);
        if (hasSafeGroundPosition) lastSafeGroundPosition = position;
        isSprinting = false;
        isCrouching = false;
        bobTimer = 0f;
        blazeTimer = 0f;
        if (controller != null)
        {
            controller.height = standHeight;
            controller.center = new Vector3(0f, standCenterY, 0f);
            controller.enabled = wasEnabled;
        }
    }

    void Update()
    {
        if (Time.timeScale <= 0f || controller == null || !controller.enabled ||
            CoopRun.PersonalMenuOpen || GetComponent<PlayerHealth3D>()?.isDead == true) return;
        isGrounded = controller.isGrounded;
        // El relieve importado no es un plano: apoyar los pies sobre su malla,
        // incluso si el controlador atraviesa una unión entre triángulos.
        SupportOnSnow(false);
        Vector3 previousPosition = transform.position;
        bool hadSnowSupport = ArenaEnricher.Instance != null &&
            ArenaEnricher.Instance.TryGetSnowHeight(previousPosition, out _);
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        if (isGrounded && ArenaEnricher.Instance != null &&
            ArenaEnricher.Instance.TryGetSnowHeight(transform.position, out _))
        {
            lastSafeGroundPosition = transform.position;
            hasSafeGroundPosition = true;
        }

        // Input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Sprint y agacharse
        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && v > 0.1f;
        if (Input.GetKeyDown(KeyCode.LeftControl))
            isCrouching = !isCrouching;

        // Altura del CharacterController
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float targetCenterY = isCrouching ? crouchCenterY : standCenterY;
        controller.height = Mathf.Lerp(controller.height, targetHeight, 10f * Time.deltaTime);
        controller.center = Vector3.Lerp(controller.center, new Vector3(0f, targetCenterY, 0f), 10f * Time.deltaTime);

        // Velocidad actual (con multiplicador dinámico de habilidades y sinergias)
        float speedMult = XPSystem.Instance != null ? XPSystem.Instance.GetCurrentSpeedMultiplier() : 1f;
        speedMult*=CharacterPassives.Instance?.Speed??1;
        if (isCrouching)
            currentSpeed = crouchSpeed * speedMult;
        else if (isSprinting)
            currentSpeed = sprintSpeed * speedMult;
        else
            currentSpeed = walkSpeed * speedMult;

        // Blazing Speed: rastro de fuego mientras corres
        if (isSprinting && isGrounded && XPSystem.Instance != null && XPSystem.Instance.blazingSpeed)
        {
            blazeTimer -= Time.deltaTime;
            if (blazeTimer <= 0f)
            {
                blazeTimer = 0.35f;
                SpawnBlazeFire();
            }
        }

        // Tailwind: vórtices de torbellino a toda velocidad
        if (isSprinting && isGrounded && XPSystem.Instance != null && XPSystem.Instance.tailwindStacks > 0)
        {
            tailwindVfxTimer -= Time.deltaTime;
            if (tailwindVfxTimer <= 0f)
            {
                tailwindVfxTimer = 0.55f;
                MagicVFX.SpawnWhirlwind(transform.position - transform.forward * 0.4f, 0.65f, 1.5f, new Color(0.45f, 1f, 0.9f, 0.8f));
            }
        }

        // Movimiento relativo a la dirección del jugador
        Vector3 move = transform.right * h + transform.forward * v;
        if (move.magnitude > 1f)
            move.Normalize();
        ActualSpeed = move.magnitude * currentSpeed;

        // Aplicar movimiento del jugador + empujón por impacto
        Vector3 totalMove = move * currentSpeed + knockbackVelocity;
        controller.Move(totalMove * Time.deltaTime);

        // Desvanecer el empujón suavemente
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 8f * Time.deltaTime);

        // Salto
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Gravedad
        velocity.y += gravity * Time.deltaTime;
        var passive=CharacterPassives.Instance;
        if(passive!=null && Input.GetButton("Jump") && !isGrounded)
        {
            if(passive.CanFloat) velocity.y=Mathf.Max(velocity.y,passive.Hero=="Birdo"?-1.2f:-3f);
            if(passive.CanClimb && move.sqrMagnitude>.01f && Physics.Raycast(transform.position+Vector3.up,move.normalized,out var wall,.9f,~0,QueryTriggerInteraction.Ignore) && Mathf.Abs(wall.normal.y)<.3f)
                velocity.y=4f;
        }
        controller.Move(velocity * Time.deltaTime);

        // No dejar que el jugador salga por los bordes abiertos del modelo.
        if (hadSnowSupport && ArenaEnricher.Instance != null &&
            !ArenaEnricher.Instance.TryGetSnowHeight(transform.position, out _))
        {
            Vector3 edgePosition = transform.position;
            edgePosition.x = previousPosition.x;
            edgePosition.z = previousPosition.z;
            SetSupportedPosition(edgePosition);
        }
        SupportOnSnow(isGrounded && velocity.y <= 0f);

        // Protección anti-caídas al vacío
        if (transform.position.y < -4f)
        {
            controller.enabled = false;
            Vector3 recovery = hasSafeGroundPosition ? lastSafeGroundPosition + Vector3.up * 0.25f : new Vector3(0f, 1.2f, 0f);
            if (!hasSafeGroundPosition && ArenaEnricher.Instance != null)
                recovery = ArenaEnricher.Instance.PlaceOnSnow(recovery, 1.2f);
            transform.position = recovery;
            velocity = Vector3.zero;
            controller.enabled = true;
        }

        // Head Bob
        if (camTransform != null && (camTransform.GetComponent<MouseLook>() == null || !camTransform.GetComponent<MouseLook>().thirdPerson))
        {
            bool isMoving = (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f) && isGrounded;
            if (isMoving)
            {
                float freq = isSprinting ? bobFrequency * 1.4f : bobFrequency;
                bobTimer += Time.deltaTime * freq;
                float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
                Vector3 camPos = camTransform.localPosition;
                camPos.y = baseCamY + bobOffset;
                camTransform.localPosition = camPos;
            }
            else
            {
                bobTimer = 0f;
                Vector3 camPos = camTransform.localPosition;
                camPos.y = Mathf.Lerp(camPos.y, baseCamY, 5f * Time.deltaTime);
                camTransform.localPosition = camPos;
            }
        }
    }

    private float blazeTimer;
    private float tailwindVfxTimer;

    void SupportOnSnow(bool followSlope)
    {
        ArenaEnricher arena = ArenaEnricher.Instance;
        if (arena == null || !arena.TryGetSnowHeight(transform.position, out float groundY)) return;
        float feetY = transform.TransformPoint(controller.center).y -
            controller.height * Mathf.Abs(transform.lossyScale.y) * 0.5f;
        float clearance = 0.03f;
        float correction = groundY + clearance - feetY;
        bool nearSurface = correction >= -0.12f;
        bool followDownhill = followSlope && correction >= -(0.5f + currentSpeed * Time.deltaTime);
        if (correction > 0f || (velocity.y <= 0f && (nearSurface || followDownhill)))
        {
            SetSupportedPosition(transform.position + Vector3.up * correction);
            if (velocity.y <= 0f)
            {
                velocity.y = -2f;
                isGrounded = true;
            }
        }
    }

    void SetSupportedPosition(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
    }

    void SpawnBlazeFire()
    {
        GameObject fire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fire.name = "BlazeTrail";
        fire.transform.position = transform.position + Vector3.up * 0.05f;
        fire.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
        Destroy(fire.GetComponent<Collider>());

        Renderer r = fire.GetComponent<Renderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = RuntimeMaterialCache.Get(s, new Color(1f, 0.4f, 0.1f, 0.6f), new Color(1f, 0.35f, 0.05f) * 2f);
        r.sharedMaterial = m;

        fire.AddComponent<FirePuddleBehavior>();
        Destroy(fire, 2.0f);
    }
}
