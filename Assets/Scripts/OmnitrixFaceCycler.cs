using UnityEngine;

/// <summary>
/// Drives the watch face.
///
/// The face is two stacked discs: a plate ('screen out') with a transparent
/// diamond window, and the alien slate ('screen in') underneath. Scrolling the
/// slate's UV window changes which silhouette shows through the diamond.
///
/// The silhouettes in alien slate.jpg are NOT evenly spaced - measured centres
/// range from 0.0935 to 0.1357 apart - so each one is addressed by its own
/// measured U centre rather than by index * step.
/// </summary>
public class OmnitrixFaceCycler : MonoBehaviour
{
    [Header("Slate (alien strip under the glass)")]
    public Renderer slateRenderer;
    public string textureProperty = "_BaseMap";

    [Header("Measured silhouette centres (U)")]
    [Tooltip("Scanned from alien slate.jpg - one entry per silhouette.")]
    public float[] alienCentresU =
    {
        0.0481f, 0.1453f, 0.2388f, 0.3347f, 0.4504f,
        0.5771f, 0.7129f, 0.8376f, 0.9438f
    };
    [Tooltip("Vertical centre of the silhouette band.")]
    public float alienCentreV = 0.1729f;

    [Header("Framing")]
    [Tooltip("UV centre of the 'screen in' quad's island.")]
    public Vector2 islandCentre = new Vector2(0.0474f, 0.1776f);
    [Tooltip("How much texture the quad samples. Bigger = alien appears smaller.")]
    public Vector2 windowTiling = new Vector2(1.537f, 1.430f);

    [Header("Selection")]
    public int alienIndex = 4;

    [Header("Auto scroll")]
    public bool autoScroll;
    [Tooltip("Silhouettes per second while scrolling.")]
    public float scrollSpeed = 6f;

    [Header("Glow pulse")]
    public bool pulse = true;
    [ColorUsage(false, true)]
    public Color glowColor = new Color(0.15f, 1f, 0.20f);
    public float glowMin = 1.5f;
    public float glowMax = 3.5f;
    public float pulseSpeed = 2f;

    private Material slateMaterial;
    private float scrollTimer;

    public int AlienCount => alienCentresU != null ? alienCentresU.Length : 0;

    void Awake()
    {
        if (slateRenderer != null)
            slateMaterial = slateRenderer.material;   // runtime instance, leaves the asset alone

        ApplyFraming();
    }

    void Update()
    {
        if (slateMaterial == null)
            return;

        if (autoScroll && scrollSpeed > 0f && AlienCount > 0)
        {
            scrollTimer += Time.deltaTime * scrollSpeed;
            while (scrollTimer >= 1f)
            {
                scrollTimer -= 1f;
                alienIndex = (alienIndex + 1) % AlienCount;
                ApplyFraming();
            }
        }

        if (pulse && slateMaterial.HasProperty("_EmissionColor"))
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            slateMaterial.SetColor("_EmissionColor", glowColor * Mathf.Lerp(glowMin, glowMax, t));
        }
    }

    public void Next()
    {
        if (AlienCount == 0) return;
        alienIndex = (alienIndex + 1) % AlienCount;
        ApplyFraming();
    }

    public void SetAlien(int index)
    {
        if (AlienCount == 0) return;
        alienIndex = Mathf.Clamp(index, 0, AlienCount - 1);
        ApplyFraming();
    }

    /// <summary>Centres the sampling window on the selected silhouette.</summary>
    public void ApplyFraming()
    {
        if (slateMaterial == null || AlienCount == 0)
            return;

        int i = Mathf.Clamp(alienIndex, 0, AlienCount - 1);

        // sampled = uv * tiling + offset, so offset places the island centre on the alien centre
        var offset = new Vector2(
            alienCentresU[i] - (islandCentre.x * windowTiling.x),
            alienCentreV      - (islandCentre.y * windowTiling.y));

        Set(textureProperty, offset);
        Set("_EmissionMap", offset);
    }

    private void Set(string prop, Vector2 offset)
    {
        if (!slateMaterial.HasProperty(prop))
            return;

        slateMaterial.SetTextureScale(prop, windowTiling);
        slateMaterial.SetTextureOffset(prop, offset);
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            ApplyFraming();
    }
}
