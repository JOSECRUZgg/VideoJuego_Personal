using UnityEngine;

public partial class ArenaEnricher
{
    Texture2D forestFloorTexture;
    // Los caminos conectan el claro central, el arco y los cuatro sectores de la expedición.
    static float ForestPathDistance(float x, float z)
    {
        float north = Mathf.Abs(x - Mathf.Sin(z * .035f) * 5f);
        float east = Mathf.Abs(z - Mathf.Sin(x * .04f) * 4f);
        float radius = new Vector2(x, z).magnitude;
        float ring = Mathf.Min(Mathf.Abs(radius - 112f), Mathf.Abs(radius - 240f));
        return Mathf.Min(north, Mathf.Min(east, ring));
    }

    static float ForestClearingDistance(float x, float z)
    {
        float d = new Vector2(x, z).magnitude - 29f;
        d = Mathf.Min(d, new Vector2(x, z - 110).magnitude - 24);
        d = Mathf.Min(d, new Vector2(x, z + 110).magnitude - 24);
        d = Mathf.Min(d, new Vector2(x - 115, z).magnitude - 24);
        return Mathf.Min(d, new Vector2(x + 115, z).magnitude - 24);
    }

    static float ForestHeight(float x, float z)
    {
        float radius = new Vector2(x, z).magnitude;
        float stageOffset = StageDirector.Instance != null ? (StageDirector.Instance.CurrentStage - 1) * 17f : 0;
        float hills = (Mathf.PerlinNoise(x * .009f + 47 + stageOffset, z * .009f + 83 + stageOffset) - .45f) * 16;
        hills += (Mathf.PerlinNoise(x * .027f + 73, z * .027f + 18) - .5f) * 3;
        float clear = Mathf.SmoothStep(0, 1, Mathf.Clamp01(ForestClearingDistance(x, z) / 22));
        float path = Mathf.Lerp(.35f, 1, Mathf.SmoothStep(0, 1, ForestPathDistance(x, z) / 10));
        float rim = Mathf.Pow(Mathf.Clamp01((radius - 300) / 48), 2) * 20;
        return hills * clear * path + rim;
    }

    void ConfigureForestFloor(Material material)
    {
        if (forestFloorTexture != null) Destroy(forestFloorTexture);
        const int resolution = 1024;
        forestFloorTexture = new Texture2D(resolution, resolution, TextureFormat.RGB24, true);
        forestFloorTexture.name = "Forest_Moss_Meadows_WindingTrails";
        forestFloorTexture.wrapMode = TextureWrapMode.Clamp;
        forestFloorTexture.anisoLevel = 8;
        var colors = new Color32[resolution * resolution];
        for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float wx = x / (float)(resolution - 1) * 700 - 350;
                float wz = z / (float)(resolution - 1) * 700 - 350;
                float meadow = Mathf.PerlinNoise(wx * .045f + 80, wz * .045f + 30);
                float grain = Mathf.PerlinNoise(wx * 1.2f + 300, wz * 1.2f + 300);
                Color grass = Color.Lerp(new Color(.10f,.20f,.055f), new Color(.34f,.43f,.13f), meadow);
                float trail = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((ForestPathDistance(wx,wz) - 2.5f + (meadow-.5f)*2) / 3f));
                Color dirt = Color.Lerp(new Color(.26f,.19f,.105f), new Color(.48f,.37f,.21f), grain);
                colors[z * resolution + x] = Color.Lerp(grass * (.85f + grain*.3f), dirt, trail * .92f);
            }
        forestFloorTexture.SetPixels32(colors);
        forestFloorTexture.Apply(true, true);
        material.color = Color.white;
        material.mainTexture = forestFloorTexture;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", forestFloorTexture);
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .08f);
    }

    void BuildDesignedForest()
    {
        var savedRandom = Random.state;
        Random.InitState(74193); // Distribución reproducible; no modifica la suerte de cofres ni enemigos.
        try
        {
            var root = new GameObject("EmeraldForest_ClearingsAndTrails");
            root.transform.SetParent(transform, false);
            var pine = FindPrefabByName("PT_Pine_Tree_03_green") ?? FindPrefabByName("UNS_Spruce_01");
            var fir = FindPrefabByName("UNS_Spruce_02") ?? pine;
            var broadleaf = FindPrefabByName("PT_Fruit_Tree_01_green") ?? pine;
            var apple = FindPrefabByName("PT_Fruit_Tree_01_apples") ?? broadleaf;
            var rocks = FindPrefabByName("UNS_Standard_Rock_02") ?? FindPrefabByName("UNS_Standard_Rock_01");
            var bush = FindPrefabByName("UNS_Bush");
            var log = FindPrefabByName("PT_Pine_Tree_03_logs");
            var mushroom = FindPrefabByName("PT_Caesars_Mushroom_01") ?? FindPrefabByName("UNS_Mushroom_Patch");
            var flower = FindPrefabByName("UNS_Flower");
            var grass = FindPrefabByName("PT_Grass_02") ?? FindPrefabByName("UNS_Grass");
            var occupied = new System.Collections.Generic.List<Vector2>();
            // Tres árboles guardianes dan identidad a los sectores sin bloquear sus objetivos.
            Vector2[] guardians = { new Vector2(68,64), new Vector2(-73,62), new Vector2(-68,-74) };
            foreach(var center in guardians)
            {
                ForestObject(broadleaf,center,24f,root.transform,true);
                occupied.Add(center);
                for(int stone=0;stone<9;stone++)
                {
                    float angle=stone*Mathf.PI*2/9;
                    Vector2 point=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*9;
                    ForestObject(rocks,point,Random.Range(.65f,1.1f),root.transform);
                    ForestObject(mushroom,point+Random.insideUnitCircle*1.5f,.45f,root.transform);
                }
            }
            for (int i = 0; i < 3500 && occupied.Count < 800; i++)
            {
                Vector2 point = Random.insideUnitCircle * 335;
                if (ForestClearingDistance(point.x,point.y) < 5 || ForestPathDistance(point.x,point.y) < 7) continue;
                bool crowded = false;
                foreach (var other in occupied) if ((point-other).sqrMagnitude < 42) { crowded=true; break; }
                if (crowded) continue;
                occupied.Add(point);
                float grove = Mathf.PerlinNoise(point.x*.02f+16,point.y*.02f+29);
                GameObject tree = grove > .52f ? (i%3==0 ? fir : pine) : (i%6==0 ? apple : broadleaf);
                ForestObject(tree, point, Random.Range(10f,17f), root.transform, true);
                if (i%3==0)
                    ForestObject(bush, point + Random.insideUnitCircle*3, Random.Range(.8f,1.6f), root.transform);
                if (i%5==0)
                    ForestObject(mushroom, point + Random.insideUnitCircle*3, Random.Range(.25f,.5f), root.transform);
            }
            // Pequeñas formaciones rocosas y troncos en las laderas, fuera de los caminos.
            for (int i=0;i<190;i++)
            {
                Vector2 p = Random.insideUnitCircle * 320;
                if(ForestClearingDistance(p.x,p.y)<4 || ForestPathDistance(p.x,p.y)<7) continue;
                ForestObject(i%6==0 ? log : rocks,p, i%6==0 ? .7f : Random.Range(.8f,2.4f),root.transform);
                if(i%2==0) ForestObject(bush,p+Random.insideUnitCircle*3,1.2f,root.transform);
            }
            var cover = root.AddComponent<ForestGroundCover>();
            cover.Configure(grass, flower);
            for(int i=0;i<55000;i++)
            {
                Vector2 p = Random.insideUnitCircle * 332;
                if(ForestPathDistance(p.x,p.y)<3.8f) continue;
                // Deja el centro de cada arena despejado; hierba corta en sus márgenes.
                float clear = ForestClearingDistance(p.x,p.y);
                if(clear < -8 && Random.value < .94f) continue;
                float meadow = Mathf.PerlinNoise(p.x*.055f+40,p.y*.055f+90);
                if(Random.value > .30f+meadow*.8f) continue;
                Vector3 position = PlaceOnGround(new Vector3(p.x,0,p.y));
                cover.Add(position, Random.Range(0,360), Random.Range(.65f,1.35f)*(clear<0?.55f:1f), i%19==0);
            }
            cover.Finish();
            CreateForestMotes(root.transform);
            foreach(Light sun in Object.FindObjectsByType<Light>())
                if(sun.type==LightType.Directional)
                {
                    sun.color=new Color(1,.91f,.70f); sun.intensity=1.35f;
                    sun.transform.rotation=Quaternion.Euler(39,-38,0);
                    sun.shadows=LightShadows.Soft; sun.shadowStrength=.72f;
                }
            RenderSettings.ambientSkyColor=new Color(.49f,.66f,.79f);
            RenderSettings.ambientEquatorColor=new Color(.35f,.44f,.28f);
            RenderSettings.ambientGroundColor=new Color(.15f,.21f,.12f);
        }
        finally { Random.state = savedRandom; }
    }

    void ForestObject(GameObject prefab, Vector2 point, float height, Transform parent, bool tree=false)
    {
        if(prefab==null) return;
        Vector3 ground = PlaceOnGround(new Vector3(point.x,0,point.y));
        // Los prefabs extraídos llevan su conversión Z-up -> Y-up en la rotación raíz.
        // La variación del bosque debe sumarse a ella, nunca sustituirla.
        var yaw=Quaternion.Euler(0,Random.Range(0,360),0);
        var obj=Instantiate(prefab,ground,yaw*prefab.transform.rotation,parent);
        Bounds bounds=CalculateRenderBounds(obj);
        if(bounds.size.y>.01f) obj.transform.localScale*=height/bounds.size.y;
        bounds=CalculateRenderBounds(obj);
        obj.transform.position += Vector3.up*(ground.y-bounds.min.y);
        foreach(var collider in obj.GetComponentsInChildren<Collider>()) collider.enabled=false;
        // Colisión solamente en el tronco: la copa no encierra al jugador.
        if(tree)
        {
            var trunk=obj.AddComponent<CapsuleCollider>();
            float scale=Mathf.Max(.001f,obj.transform.lossyScale.y);
            Vector3 upInModel=obj.transform.InverseTransformDirection(Vector3.up);
            Vector3 axis=new Vector3(Mathf.Abs(upInModel.x),Mathf.Abs(upInModel.y),Mathf.Abs(upInModel.z));
            trunk.direction=axis.x>axis.y && axis.x>axis.z ? 0 : axis.z>axis.y ? 2 : 1;
            trunk.height=height*.45f/scale;
            trunk.radius=Mathf.Clamp(height*.035f,.3f,.65f)/scale;
            trunk.center=obj.transform.InverseTransformPoint(ground+Vector3.up*height*.225f);
        }
    }

    void CreateForestMotes(Transform parent)
    {
        var go=new GameObject("Forest_PollenAndFireflies"); go.transform.SetParent(parent,false);
        go.transform.localPosition=Vector3.up*2;
        var ps=go.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main; main.maxParticles=280; main.startLifetime=12; main.startSpeed=.12f;
        main.startSize=new ParticleSystem.MinMaxCurve(.04f,.12f); main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.startColor=new ParticleSystem.MinMaxGradient(new Color(.95f,.85f,.4f,.4f),new Color(.5f,1,.65f,.6f));
        var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(340,4,340);
        var emission=ps.emission; emission.rateOverTime=20;
        var noise=ps.noise; noise.enabled=true; noise.strength=.25f; noise.frequency=.2f;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial=RuntimeMaterialCache.Get(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"),new Color(.7f,1,.4f,.45f),Color.black);
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
    }
}
