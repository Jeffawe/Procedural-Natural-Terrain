using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer planeRenderer;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawMap(Texture2D texture)
    {
        planeRenderer.sharedMaterial.mainTexture = texture;
        planeRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData){
        meshFilter.sharedMesh = meshData.CreateMesh();

        meshFilter.transform.localScale = Vector3.one * FindObjectOfType<MapGenerator>().terrainData.uniformScale;
    }
}
