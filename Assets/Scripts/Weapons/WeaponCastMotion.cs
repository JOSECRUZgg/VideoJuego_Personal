using UnityEngine;

/// <summary>Short attack motion, separate from damage. Never substitutes an inventory icon.</summary>
public class WeaponCastMotion : MonoBehaviour
{
    Transform owner;
    Vector3 direction;
    float elapsed;
    bool melee;
    const float Duration=.32f;
    public static void Play(Transform owner,string id,Vector3 direction)
    {
        bool melee=id=="mb_sword" || id=="mb_katana" || id=="mb_scythe" ||
            id=="mb_dexecutioner" || id=="mb_corrupted";
        if(!melee && id!="mb_fire")return;
        direction.y=0;if(direction.sqrMagnitude<.001f)direction=owner.forward;
        var go=new GameObject("AttackMotion_"+id);
        var motion=go.AddComponent<WeaponCastMotion>();motion.owner=owner;motion.direction=direction.normalized;motion.melee=melee;
        // Procedural readable fallback: original spell prefabs are emitted separately.
        // No icon extrusion, colliders or damage are attached to this visual.
        motion.Part(new Vector3(0,0,0),new Vector3(.08f,melee?.28f:.8f,.08f),new Color(.32f,.16f,.07f));
        if(melee)
        {
            motion.Part(Vector3.up*.5f,new Vector3(.14f,.75f,.05f),new Color(.85f,.93f,1));
            motion.Part(Vector3.up*.12f,new Vector3(.38f,.06f,.1f),new Color(.9f,.65f,.14f));
        }
        else
        {
            motion.Part(Vector3.up*.5f,Vector3.one*.22f,new Color(1,.3f,.025f));
            SpellVFX.Burst(owner.position+Vector3.up*1.2f+direction.normalized*.5f,"flame_01",new Color(1,.45f,.05f),.8f);
        }
        motion.Pose(0);
    }
    void Part(Vector3 position,Vector3 scale,Color color)
    {
        var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.transform.SetParent(transform,false);part.transform.localPosition=position;part.transform.localScale=scale;
        part.GetComponent<Collider>().enabled=false;Destroy(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().sharedMaterial=RuntimeMaterialCache.Get(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default"),color,color);
    }
    void Pose(float t)
    {
        Quaternion facing=Quaternion.LookRotation(direction);
        float sweep=melee?Mathf.Lerp(-75,75,t):0;
        transform.position=owner.position+Vector3.up+facing*(Quaternion.Euler(0,sweep,0)*Vector3.forward*.75f);
        transform.rotation=facing*Quaternion.Euler(melee?65:Mathf.Sin(t*Mathf.PI)*-25,sweep,melee?-25:15);
    }
    void Update()
    {
        var gm=GameManager.Instance;
        if(owner==null || gm==null || gm.gameState==GameManager.GameState.MainMenu || gm.gameState==GameManager.GameState.GameOver || gm.gameState==GameManager.GameState.Victory){Destroy(gameObject);return;}
        if(Time.timeScale<=0)return;
        elapsed+=Time.deltaTime;if(elapsed>=Duration){Destroy(gameObject);return;}Pose(elapsed/Duration);
    }
}
