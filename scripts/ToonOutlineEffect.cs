using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class ToonOutlineEffect : MonoBehaviour
{
    public Shader outlineShader;
    private Material outlineMaterial;

    void Start()
    {
        // 初始化材质
        if (outlineShader == null)
        {
            outlineShader = Shader.Find("MyShader/OutlineEffect");
        }
        outlineMaterial = new Material(outlineShader);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // 执行后处理渲染
        Graphics.Blit(src, dest, outlineMaterial);
    }
}
