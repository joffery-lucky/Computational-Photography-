using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateMesh : MonoBehaviour
{
    Mesh plane_mesh;
    const int width = 10;
    const int length = 10;
    // Start is called before the first frame update
    public void BuildObject()
    {
        plane_mesh = gameObject.AddComponent<MeshFilter>().mesh;
        gameObject.AddComponent<MeshRenderer>();
        gameObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Diffuse"));
        plane_mesh.vertices = GenerateVertices(width, length);
        plane_mesh.triangles = GenerateTriangles(width, length);
    }

    private int[] GenerateTriangles(int width, int length)
    {
        // 生成mesh的triangles index
        int[] triangles = new int[(width - 1) * (length - 1) * 6];
        for (int z = 0; z < length - 1; z++ )
        {
            for (int x = 0; x < width - 1; x++ )
            {
                triangles[(z * (width - 1) + x) * 6    ] = z * width + x;
                triangles[(z * (width - 1) + x) * 6 + 1] = z * width + x + 1 + width;
                triangles[(z * (width - 1) + x) * 6 + 2] = z * width + x + 1;
                triangles[(z * (width - 1) + x) * 6 + 3] = z * width + x;
                triangles[(z * (width - 1) + x) * 6 + 4] = z * width + x + width;
                triangles[(z * (width - 1) + x) * 6 + 5] = z * width + x + 1 + width;
            }
        }
        return triangles;
    }

    private Vector3[] GenerateVertices(int width, int length)
    {
        // 按照如下的顺序生成一个平面mesh
        Vector3[] vertices = new Vector3[width * length];
        for (int z = 0; z < length; z++ )
        {
            for (int x = 0; x < width; x++ )
            {
                vertices[z * width + x] = new Vector3(x / (float)width, 0.0F, z / (float) length);
            }
        }
        return vertices;
    }
}
