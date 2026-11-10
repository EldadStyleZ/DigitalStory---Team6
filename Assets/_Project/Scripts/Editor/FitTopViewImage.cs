// Lives in Assets/_Project/Scripts/Editor (editor-only assembly DigitalStory.Editor).
// Opens from the menu: Tools > Fit Top View Image
//
// Lays the top-view PNG from the sidewalk generator over the imported sidewalk model,
// at exactly the model's size and position, so the two line up 1:1.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DigitalStory.Editor
{
public class FitTopViewImage : EditorWindow
{
    enum Look { LitAlbedo, UnlitBaked }

    GameObject model;
    Texture2D image;
    Look look = Look.LitAlbedo;
    bool rotate180 = true;
    float lift = 0.12f;

    [MenuItem("Tools/Fit Top View Image")]
    static void Open() => GetWindow<FitTopViewImage>("Fit Top View");

    void OnEnable()
    {
        // pre-fill: the newest top-view PNG in the project, and the selected object
        if (image == null)
        {
            string best = null; System.DateTime bestTime = default;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!System.IO.Path.GetFileName(p).Contains("_top_")) continue;
                var t = System.IO.File.GetLastWriteTime(p);
                if (best == null || t > bestTime) { best = p; bestTime = t; }
            }
            if (best != null) image = AssetDatabase.LoadAssetAtPath<Texture2D>(best);
        }
        if (model == null && Selection.activeGameObject != null) model = Selection.activeGameObject;
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "1. Drag the imported sidewalk model (from the ZIP) here.\n" +
            "2. Drag the top-view PNG here.\n" +
            "3. Press Create. A flat floor the exact size of the model is placed on it.\n" +
            "If the image looks rotated against the stones, tick or untick 'Rotate 180' and press Create again.",
            MessageType.Info);

        model = (GameObject)EditorGUILayout.ObjectField("Sidewalk model", model, typeof(GameObject), true);
        image = (Texture2D)EditorGUILayout.ObjectField("Top-view PNG", image, typeof(Texture2D), false);
        look = (Look)EditorGUILayout.EnumPopup(new GUIContent("Image type",
            "LitAlbedo = PNG saved as 'colour only' (lit by your scene).\nUnlitBaked = PNG saved 'with light and shadow' (shown exactly as saved)."), look);
        rotate180 = EditorGUILayout.Toggle(new GUIContent("Rotate 180", "Depends on which glTF importer you use. Toggle if the image is turned around."), rotate180);
        lift = EditorGUILayout.FloatField(new GUIContent("Height above ground (m)", "0.12 sits just above the stones. Use 0 to put it at ground level and hide the 3D model (a flat, light floor)."), lift);

        using (new EditorGUI.DisabledScope(model == null || image == null))
            if (GUILayout.Button("Create", GUILayout.Height(32))) Create();

        var sel = Selection.gameObjects;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Paint the model itself: the 3D sidewalk gets the image projected straight down, so every stone, dune and root " +
                                "has exactly the colour it has in the generator. Removes the flat top_view floors.", MessageType.None);
        using (new EditorGUI.DisabledScope(image == null || sel.Length == 0))
            if (GUILayout.Button($"Paint selected models with the image ({sel.Length})", GUILayout.Height(32)))
                PaintModels(sel);

        EditorGUILayout.Space();
        if (GUILayout.Button(new GUIContent("Match the generator's lighting in this scene",
            "Warm sun from the same direction as in the generator, and the same sky/ground ambient light. Undo-able."), GUILayout.Height(26)))
            MatchLighting();
        using (new EditorGUI.DisabledScope(image == null || sel.Length == 0))
            if (GUILayout.Button($"Create for all selected ({sel.Length})", GUILayout.Height(28)))
            {
                var keep = model;
                foreach (var go in sel) { model = go; Create(); }
                model = keep;
                Selection.objects = sel;
            }
    }

    void Create()
    {
        // footprint = the soil bed under the stones; the generator framed the PNG on exactly this
        Bounds b = default; bool found = false, hasBed = false;
        foreach (var r in model.GetComponentsInChildren<Renderer>())
            if (r.name.Contains("soil_bed")) { b = r.bounds; found = hasBed = true; break; }
        if (!found)
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                if (!found) { b = r.bounds; found = true; } else b.Encapsulate(r.bounds);
        if (!found) { EditorUtility.DisplayDialog("Fit Top View", "The model has no meshes.", "OK"); return; }

        FixImportSettings();

        foreach (Transform child in model.transform)
            if (child.name == "top_view") { Undo.DestroyObjectImmediate(child.gameObject); break; }

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Undo.RegisterCreatedObjectUndo(quad, "Fit Top View Image");
        quad.name = "top_view";
        DestroyImmediate(quad.GetComponent<Collider>());

        // a Unity Quad is 1 x 1 m, so its scale is the size in metres
        // Unity's OBJ importer merges everything into one mesh, so the soil bed can't be found by name.
        // Then: ground = bottom of the model + the generator's 0.3 m soil bed, and the size is taken from the
        // side that matches the image's aspect (stones tilted past the edge only make the box a little wider).
        float ground = hasBed ? b.max.y : b.min.y + 0.296f;
        float sx = b.size.x, sz = b.size.z, aspect = (float)image.width / image.height;
        if (!hasBed) { if (sx / sz > aspect) sx = sz * aspect; else sz = sx / aspect; }
        quad.transform.position = new Vector3(b.center.x, ground + lift, b.center.z);
        quad.transform.rotation = Quaternion.Euler(90f, model.transform.eulerAngles.y + (rotate180 ? 180f : 0f), 0f);
        bool sideways = Mathf.Abs(Mathf.Sin(model.transform.eulerAngles.y * Mathf.Deg2Rad)) > 0.7f;   // model turned 90°: the box's x/z swap
        quad.transform.localScale = sideways ? new Vector3(sz, sx, 1f) : new Vector3(sx, sz, 1f);
        Undo.SetTransformParent(quad.transform, model.transform, "Fit Top View Image");   // moves with its sidewalk

        quad.GetComponent<Renderer>().sharedMaterial = MakeMaterial();

        Debug.Log($"Top view placed on {model.name}: {sx:0.00} x {sz:0.00} m, image {image.width} x {image.height} px " +
                  $"({image.width / sx:0} px per metre).");
    }

    void PaintModels(GameObject[] models)
    {
        var shader = Shader.Find("DigitalStory/Top Projected");
        if (shader == null) { EditorUtility.DisplayDialog("Fit Top View", "Shader 'DigitalStory/Top Projected' is missing.", "OK"); return; }
        FixImportSettings();

        // the generator always centres the soil bed on the origin; the image covers exactly that bed.
        // Its depth (Z) is the model's full depth, its width follows from the image's aspect.
        Bounds mb = default; bool any = false;
        foreach (var mf in models[0].GetComponentsInChildren<MeshFilter>())
        {
            if (mf.name == "top_view" || mf.sharedMesh == null) continue;
            if (!any) { mb = mf.sharedMesh.bounds; any = true; } else mb.Encapsulate(mf.sharedMesh.bounds);
        }
        if (!any) return;
        float aspect = (float)image.width / image.height, sx = mb.size.x, sz = mb.size.z;
        if (sx / sz > aspect) sx = sz * aspect; else sz = sx / aspect;

        var mat = new Material(shader) { name = image.name + "_projected" };
        mat.SetTexture("_MainTex", image);
        mat.SetVector("_Rect", new Vector4(0f, mb.center.z, sx, sz));
        mat.SetVector("_Flip", new Vector4(rotate180 ? -1f : 1f, rotate180 ? -1f : 1f, 0f, 0f));
        string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(image));
        string path = $"{folder}/{mat.name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) { existing.shader = shader; existing.CopyPropertiesFromMaterial(mat); mat = existing; EditorUtility.SetDirty(mat); }
        else AssetDatabase.CreateAsset(mat, path);

        foreach (var go in models)
        {
            foreach (Transform child in go.transform)
                if (child.name == "top_view") { Undo.DestroyObjectImmediate(child.gameObject); break; }
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                Undo.RecordObject(r, "Paint sidewalk");
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Painted {models.Length} sidewalk(s) with {image.name}: {sx:0.00} x {sz:0.00} m.");
    }

    static void MatchLighting()
    {
        // the generator lights its scene with a sky/ground hemisphere light and one warm sun
        Light sun = RenderSettings.sun;
        if (sun == null)
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
        if (sun == null)
        {
            var go = new GameObject("Directional Light");
            Undo.RegisterCreatedObjectUndo(go, "Match generator lighting");
            sun = go.AddComponent<Light>(); sun.type = LightType.Directional;
        }
        Undo.RecordObject(sun, "Match generator lighting");
        Undo.RecordObject(sun.transform, "Match generator lighting");
        sun.color = new Color(1f, 0.945f, 0.863f);
        sun.intensity = 0.85f;
        sun.shadows = LightShadows.Soft;
        sun.useColorTemperature = false;
        // generator sun sits at (-5, 9, 4); Unity's OBJ import mirrors X, so it comes from (+5, 9, 4)
        sun.transform.rotation = Quaternion.LookRotation(-new Vector3(5f, 9f, 4f).normalized);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.875f, 0.902f, 0.933f) * 0.55f;
        RenderSettings.ambientEquatorColor = new Color(0.63f, 0.62f, 0.6f) * 0.55f;
        RenderSettings.ambientGroundColor = new Color(0.29f, 0.259f, 0.212f) * 0.55f;
        RenderSettings.ambientIntensity = 1f;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sun.gameObject.scene);
        Debug.Log("Scene lighting set to match the sidewalk generator.");
    }

    Material cached; Texture2D cachedFor; Look cachedLook;
    Material MakeMaterial()
    {
        if (cached != null && cachedFor == image && cachedLook == look) return cached;   // one material shared by all copies
        cachedFor = image; cachedLook = look;
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        string shaderName = urp
            ? (look == Look.LitAlbedo ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit")
            : (look == Look.LitAlbedo ? "Standard" : "Unlit/Texture");
        var mat = new Material(Shader.Find(shaderName)) { name = image.name + "_mat" };
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", image);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", image);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);

        string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(image));
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{mat.name}.mat");
        AssetDatabase.CreateAsset(mat, path);
        return cached = mat;
    }

    void FixImportSettings()
    {
        // keep the full resolution and stop the edges from tiling into each other
        var imp = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(image));
        if (imp == null) return;
        imp.GetSourceTextureWidthAndHeight(out int w, out int h);
        int longest = Mathf.Max(w, h), size = Mathf.Min(8192, Mathf.NextPowerOfTwo(longest));
        if (imp.wrapMode == TextureWrapMode.Clamp && imp.npotScale == TextureImporterNPOTScale.None && imp.maxTextureSize >= size) return;   // already set
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.sRGBTexture = true;
        imp.anisoLevel = 8;
        imp.maxTextureSize = Mathf.Max(imp.maxTextureSize, size);
        imp.npotScale = TextureImporterNPOTScale.None;   // keep the exact width x height, no stretching
        imp.SaveAndReimport();
    }
}
}
