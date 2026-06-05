using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDarknessDeathEffect : MonoBehaviour
{
    [Header("Renderer Setup")]
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Material darknessMaterial;

    [Header("Spread Settings")]
    [SerializeField] private AnimationCurve spreadCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("The shader value where the enemy visually looks fully covered by darkness.")]
    [SerializeField] private float fullDarknessValue = 0.6f;

    [Tooltip("Starting shader value. Use 0 if the darkness starts immediately at the feet.")]
    [SerializeField] private float startDarknessValue = 0f;

    [Header("Optional Texture/Color Copying")]
    [SerializeField] private bool copyOriginalBaseMap = true;
    [SerializeField] private bool copyOriginalBaseColor = true;

    private static readonly int DarknessAmountID = Shader.PropertyToID("_DarknessAmount");
    private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private readonly List<Material> runtimeDarknessMaterials = new();
    private readonly List<RendererMaterialSnapshot> originalMaterialSnapshots = new();
    private bool darknessMaterialsApplied;

    private class RendererMaterialSnapshot
    {
        public Renderer renderer;
        public Material[] materials;

        public RendererMaterialSnapshot(Renderer renderer, Material[] materials)
        {
            this.renderer = renderer;
            this.materials = materials;
        }
    }

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(false);
    }


    // Used for LoadGame()
    public IEnumerator SpreadDarkness(float duration)
    {
        if (darknessMaterial == null)
        {
            Debug.LogWarning($"{name}: No darkness material assigned.", this);
            yield break;
        }

        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning($"{name}: No renderers found for darkness death effect.", this);
            yield break;
        }

        ApplyUniqueDarknessMaterials();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            float curvedT = spreadCurve.Evaluate(t);
            float darknessAmount = Mathf.Lerp(startDarknessValue, fullDarknessValue, curvedT);
            SetDarknessAmount(darknessAmount);

            yield return null;
        }

        SetDarknessAmount(fullDarknessValue);
    }

    private void ApplyUniqueDarknessMaterials()
    {
        if (darknessMaterialsApplied)
            return;

        runtimeDarknessMaterials.Clear();
        originalMaterialSnapshots.Clear();

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            Material[] currentMaterials = rend.materials;
            originalMaterialSnapshots.Add(new RendererMaterialSnapshot(rend, (Material[])currentMaterials.Clone()));

            for (int i = 0; i < currentMaterials.Length; i++)
            {
                Material originalMaterial = currentMaterials[i];
                Material runtimeDarknessMaterial = new Material(darknessMaterial);

                CopyOriginalMaterialValues(
                    originalMaterial,
                    runtimeDarknessMaterial
                );

                runtimeDarknessMaterial.SetFloat(DarknessAmountID, startDarknessValue);

                currentMaterials[i] = runtimeDarknessMaterial;
                runtimeDarknessMaterials.Add(runtimeDarknessMaterial);
            }

            rend.materials = currentMaterials;
        }

        darknessMaterialsApplied = true;
    }

    public void RestoreOriginalMaterials()
    {
        if (!darknessMaterialsApplied)
            return;

        foreach (RendererMaterialSnapshot snapshot in originalMaterialSnapshots)
        {
            if (snapshot == null || snapshot.renderer == null) continue;

            snapshot.renderer.materials = snapshot.materials;
        }

        foreach (Material runtimeMaterial in runtimeDarknessMaterials)
        {
            if (runtimeMaterial == null) continue;

            if (Application.isPlaying) Destroy(runtimeMaterial);
            else DestroyImmediate(runtimeMaterial);
        }

        runtimeDarknessMaterials.Clear();
        originalMaterialSnapshots.Clear();
        darknessMaterialsApplied = false;
    }

    private void CopyOriginalMaterialValues(Material originalMaterial, Material runtimeDarknessMaterial)
    {
        if (originalMaterial == null || runtimeDarknessMaterial == null)
            return;

        if (copyOriginalBaseMap &&
            originalMaterial.HasProperty(BaseMapID) &&
            runtimeDarknessMaterial.HasProperty(BaseMapID))
        {
            Texture originalTexture = originalMaterial.GetTexture(BaseMapID);
            runtimeDarknessMaterial.SetTexture(BaseMapID, originalTexture);
        }

        if (copyOriginalBaseColor &&
            originalMaterial.HasProperty(BaseColorID) &&
            runtimeDarknessMaterial.HasProperty(BaseColorID))
        {
            Color originalColor = originalMaterial.GetColor(BaseColorID);
            runtimeDarknessMaterial.SetColor(BaseColorID, originalColor);
        }
    }

    private void SetDarknessAmount(float amount)
    {
        for (int i = 0; i < runtimeDarknessMaterials.Count; i++)
        {
            Material mat = runtimeDarknessMaterials[i];

            if (mat == null) continue;

            mat.SetFloat(DarknessAmountID, amount);
        }
    }
}
