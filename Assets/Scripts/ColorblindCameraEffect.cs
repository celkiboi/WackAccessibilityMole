using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ColorblindCameraEffect : MonoBehaviour
{
    [SerializeField]
    private Shader colorblindShader;

    private Material filterMaterial;

    private void Awake()
    {
        EnsureMaterial();
    }

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += UpdateShaderProperties;
        }
        UpdateShaderProperties();
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= UpdateShaderProperties;
        }
    }

    private void EnsureMaterial()
    {
        if (filterMaterial == null)
        {
            if (colorblindShader == null)
            {
                colorblindShader = Shader.Find("Custom/ColorblindFilter");
            }

            if (colorblindShader != null && colorblindShader.isSupported)
            {
                filterMaterial = new Material(colorblindShader);
                filterMaterial.hideFlags = HideFlags.DontSave;
            }
        }
    }

    public void UpdateShaderProperties()
    {
        EnsureMaterial();

        if (filterMaterial == null) return;

        if (SettingsManager.Instance != null)
        {
            filterMaterial.SetInt("_Mode", (int)SettingsManager.Instance.CurrentColorblindMode);
            filterMaterial.SetFloat("_Intensity", SettingsManager.Instance.ColorblindIntensity);
        }
        else
        {
            filterMaterial.SetInt("_Mode", 0);
            filterMaterial.SetFloat("_Intensity", 1.0f);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        EnsureMaterial();

        if (filterMaterial != null && SettingsManager.Instance != null && SettingsManager.Instance.CurrentColorblindMode != ColorblindMode.Off)
        {
            Graphics.Blit(source, destination, filterMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    private void OnDestroy()
    {
        if (filterMaterial != null)
        {
            DestroyImmediate(filterMaterial);
        }
    }
}
