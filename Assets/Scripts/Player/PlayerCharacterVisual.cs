using UnityEngine;

/// <summary>
/// Avatar legible de tercera persona. Se construye con geometría ligera para
/// estar disponible en cualquier escena y cambia de paleta con el héroe elegido.
/// </summary>
public class PlayerCharacterVisual : MonoBehaviour
{
    private Transform avatarRoot;
    private Vector3 previousPosition;
    private float stridePhase, strideWeight;
    private Animator importedAnimator;
    private Quaternion modelFacingOffset = Quaternion.identity;
    public float FacingYaw => avatarRoot != null ? avatarRoot.eulerAngles.y : transform.eulerAngles.y;

    void LateUpdate()
    {
        Vector3 displacement = transform.position - previousPosition;
        previousPosition = transform.position;
        displacement.y = 0f;
        if (avatarRoot == null || Time.deltaTime <= 0f) return;
        float speed = Mathf.Min(displacement.magnitude / Time.deltaTime, 20f);
        if (importedAnimator != null)
        {
            if (speed > .2f)
                avatarRoot.rotation = Quaternion.Slerp(avatarRoot.rotation,
                    Quaternion.LookRotation(displacement) * modelFacingOffset, 1 - Mathf.Exp(-12 * Time.deltaTime));
            if (importedAnimator.runtimeAnimatorController == null) return;
            var movement = GetComponent<PlayerMovement3D>();
            foreach (var parameter in importedAnimator.parameters)
            {
                if (parameter.type != AnimatorControllerParameterType.Bool) continue;
                if (parameter.name == "moving") importedAnimator.SetBool(parameter.nameHash, speed > .2f);
                if (parameter.name == "grounded") importedAnimator.SetBool(parameter.nameHash, movement == null || movement.isGrounded);
                if (parameter.name == "jumping") importedAnimator.SetBool(parameter.nameHash, movement != null && !movement.isGrounded);
            }
            return;
        }
        strideWeight = Mathf.MoveTowards(strideWeight, Mathf.Clamp01(speed / 3f), Time.deltaTime * 8f);
        stridePhase += speed * Time.deltaTime * 1.5f;
        float swing = Mathf.Sin(stridePhase) * 28f * strideWeight;
        avatarRoot.Find("LeftLeg").localRotation = Quaternion.Euler(swing, 0, 0);
        avatarRoot.Find("RightLeg").localRotation = Quaternion.Euler(-swing, 0, 0);
        avatarRoot.Find("LeftArm").localRotation = Quaternion.Euler(-swing * .7f, 0, -12);
        avatarRoot.Find("RightArm").localRotation = Quaternion.Euler(swing * .7f, 0, 12);
        avatarRoot.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(stridePhase)) * .035f * strideWeight;
        if (speed > .15f && displacement.sqrMagnitude < 16f)
        {
            Vector3 facing = transform.InverseTransformDirection(displacement);
            avatarRoot.localRotation = Quaternion.Slerp(avatarRoot.localRotation,
                Quaternion.LookRotation(facing), 1f - Mathf.Exp(-12f * Time.deltaTime));
        }
    }

    void Awake()
    {
        Build();
    }

    public void Refresh()
    {
        if (avatarRoot != null) Destroy(avatarRoot.gameObject);
        Build();
    }

    void Build()
    {
        GameManager.CharacterType hero = GameManager.Instance != null
            ? GameManager.Instance.selectedCharacter
            : GameManager.CharacterType.CL4NK;
        importedAnimator = null;
        modelFacingOffset = Quaternion.identity;
        var selected = OriginalContentCatalog.Selected;
        if (selected != null)
        {
            var original = OriginalModel.Create(selected.prefab, transform, 1.8f);
            if (original != null)
            {
                avatarRoot = original.transform;
                modelFacingOffset = avatarRoot.localRotation;
                importedAnimator = original.GetComponentInChildren<Animator>();
                if (importedAnimator == null) importedAnimator = original.AddComponent<Animator>();
                previousPosition = transform.position;
                return;
            }
        }
        string model = hero == GameManager.CharacterType.Calcium ? "Calcium" :
            hero == GameManager.CharacterType.SirOofie ? "Knight" :
            hero == GameManager.CharacterType.Megachad ? "B_Megachad" : "RobotCowboy";
        var imported = ExtractedVisuals.Attach(model, transform, 1.8f);
        if (imported != null)
        {
            avatarRoot = imported.transform;
            modelFacingOffset = avatarRoot.localRotation;
            importedAnimator = imported.GetComponentInChildren<Animator>();
            previousPosition = transform.position;
            // Imported static models keep their authored pose.
            if (importedAnimator == null) imported.AddComponent<Animator>();
            importedAnimator = imported.GetComponentInChildren<Animator>();
            return;
        }

        Color coat;
        Color trim;
        Color skin;
        switch (hero)
        {
            case GameManager.CharacterType.Megachad:
                coat = new Color(0.38f, 0.46f, 0.56f);
                trim = new Color(0.72f, 0.82f, 0.92f);
                skin = new Color(0.55f, 0.66f, 0.76f);
                break;
            case GameManager.CharacterType.SirOofie:
                coat = new Color(0.72f, 0.12f, 0.13f);
                trim = new Color(1f, 0.52f, 0.16f);
                skin = new Color(0.93f, 0.67f, 0.47f);
                break;
            case GameManager.CharacterType.Calcium:
                coat = new Color(0.10f, 0.33f, 0.64f);
                trim = new Color(0.18f, 0.95f, 1f);
                skin = new Color(0.68f, 0.84f, 0.96f);
                break;
            default:
                coat = new Color(0.12f, 0.30f, 0.72f);
                trim = new Color(0.95f, 0.68f, 0.15f);
                skin = new Color(0.90f, 0.62f, 0.42f);
                break;
        }

        GameObject root = new GameObject("HeroAvatar");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localScale = Vector3.one * 0.78f;
        avatarRoot = root.transform;
        previousPosition = transform.position;

        // Torso, piernas, brazos y cabeza: silueta clara a distancia.
        CreatePart("Torso", PrimitiveType.Capsule, new Vector3(0f, 1.05f, 0f), new Vector3(0.66f, 0.78f, 0.48f), coat);
        CreatePart("Head", PrimitiveType.Sphere, new Vector3(0f, 1.78f, 0.02f), new Vector3(0.46f, 0.46f, 0.46f), skin);
        CreatePart("LeftLeg", PrimitiveType.Capsule, new Vector3(-0.19f, 0.40f, 0f), new Vector3(0.23f, 0.48f, 0.23f), new Color(0.08f, 0.10f, 0.16f));
        CreatePart("RightLeg", PrimitiveType.Capsule, new Vector3(0.19f, 0.40f, 0f), new Vector3(0.23f, 0.48f, 0.23f), new Color(0.08f, 0.10f, 0.16f));
        CreatePart("LeftArm", PrimitiveType.Capsule, new Vector3(-0.46f, 1.16f, 0f), new Vector3(0.19f, 0.48f, 0.19f), coat, new Vector3(0f, 0f, -12f));
        CreatePart("RightArm", PrimitiveType.Capsule, new Vector3(0.46f, 1.16f, 0f), new Vector3(0.19f, 0.48f, 0.19f), coat, new Vector3(0f, 0f, 12f));

        // Sombrero/armadura característico y emblema luminoso de clase.
        CreatePart("HatBrim", PrimitiveType.Cylinder, new Vector3(0f, 2.02f, 0f), new Vector3(0.62f, 0.05f, 0.62f), trim);
        CreatePart("Hat", PrimitiveType.Cylinder, new Vector3(0f, 2.19f, 0f), new Vector3(0.32f, 0.22f, 0.32f), coat);
        CreatePart("Emblem", PrimitiveType.Sphere, new Vector3(0f, 1.15f, 0.29f), new Vector3(0.16f, 0.16f, 0.08f), trim, Vector3.zero, true);
        CreatePart("Rifle", PrimitiveType.Cube, new Vector3(0.52f, 1.02f, 0.34f), new Vector3(0.10f, 0.10f, 0.72f), new Color(0.12f, 0.14f, 0.18f), new Vector3(8f, 0f, 0f));
    }

    void CreatePart(string partName, PrimitiveType primitive, Vector3 position, Vector3 scale, Color color,
        Vector3 rotation = default(Vector3), bool emissive = false)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = partName;
        part.transform.SetParent(avatarRoot, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(rotation);
        part.transform.localScale = scale;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Renderer renderer = part.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        renderer.sharedMaterial = RuntimeMaterialCache.Get(shader, color, emissive ? color * 2.2f : color);
    }
}
