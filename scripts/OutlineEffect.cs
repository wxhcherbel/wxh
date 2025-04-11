using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
[DisallowMultipleComponent]
public class OutlineEffect : MonoBehaviour { // 描边特效
    private Renderer[] renderers; // 当前对象及其子对象的渲染器
    private Material outlineMaterial; // 描边材质
 
    private void Awake() {
        renderers = GetComponentsInChildren<Renderer>();
        outlineMaterial = new Material(Shader.Find("MyShader/OutlineEffect"));
        if (outlineMaterial == null)
        {
            Debug.LogError("❌ 没有找到 Shader: MyShader/OutlineEffect，请确认路径和命名是否正确！");
        }else{
            Debug.Log("找到 Shader: MyShader/OutlineEffect");
        }
        LoadSmoothNormals();
    }
 
    private void OnEnable() {
        outlineMaterial.SetFloat("_StartTime", Time.timeSinceLevelLoad * 2);
        foreach (var renderer in renderers) {
            List<Material> materials = renderer.sharedMaterials.ToList();
            materials.Add(outlineMaterial);
            renderer.materials = materials.ToArray();
        }
    }
 
    private void OnDisable() {
        foreach (var renderer in renderers) {
            // 这里只能用sharedMaterials, 使用materials会进行深拷贝, 使得删除材质会失败
            List<Material> materials = renderer.sharedMaterials.ToList();
            materials.Remove(outlineMaterial);
            renderer.materials = materials.ToArray();
        }
    }
 
    private void LoadSmoothNormals() { // 加载平滑的法线(对相同顶点的所有法线取平均值)
        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {
            List<Vector3> smoothNormals = SmoothNormals(meshFilter.sharedMesh);
            meshFilter.sharedMesh.SetUVs(3, smoothNormals); // 将平滑法线存储到UV3中
            var renderer = meshFilter.GetComponent<Renderer>();
            if (renderer != null) {
                CombineSubmeshes(meshFilter.sharedMesh, renderer.sharedMaterials.Length);
            }
        }
        foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>()) {
            // 清除SkinnedMeshRenderer的UV3
            skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];
            CombineSubmeshes(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials.Length);
        }
    }
 
    private List<Vector3> SmoothNormals(Mesh mesh) { // 计算平滑法线, 对相同顶点的所有法线取平均值
        // 按照顶点进行分组(如: 立方体有8个顶点, 但网格实际存储的是24个顶点, 因为相交的3个面的法线不同, 所以一个顶点存储了3次)
        var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);
        List<Vector3> smoothNormals = new List<Vector3>(mesh.normals);
        foreach (var group in groups) {
            if (group.Count() == 1) {
                continue;
            }
            Vector3 smoothNormal = Vector3.zero;
            foreach (var pair in group) { // 计算法线均值(如: 对立方体同一顶点的3个面的法线取平均值, 平滑法线沿对角线向外)
                smoothNormal += smoothNormals[pair.Value];
            }
            smoothNormal.Normalize();
            foreach (var pair in group) { // 平滑法线赋值(如: 立方体的同一顶点的3个面的平滑法线都是沿着对角线向外)
                smoothNormals[pair.Value] = smoothNormal;
            }
        }
        return smoothNormals;
    }
 
    private void CombineSubmeshes(Mesh mesh, int materialsLength) { // 绑定子网格
        if (mesh.subMeshCount == 1) {
            return;
        }
        if (mesh.subMeshCount > materialsLength) {
            return;
        }
        mesh.subMeshCount++;
        mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }
}