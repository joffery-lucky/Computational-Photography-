using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateObject : MonoBehaviour
{
    Mesh plane_mesh;
    const int length = 10;
    // Start is called before the first frame update
    public void BuildObject()
    {
        plane_mesh = gameObject.AddComponent<MeshFilter>().mesh;
        gameObject.AddComponent<MeshRenderer>();
        // gameObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Diffuse"));
        plane_mesh.vertices = GenerateVertices(length);
        plane_mesh.triangles = GenerateTriangles(length);
        plane_mesh.normals = GenerateNormals(length);
        plane_mesh.uv = GenerateUV(length);
    }

    private int[] GenerateTriangles(int length)
    {
        // 生成mesh的triangles index
        int[] triangles = new int[(length - 1) * 4 * 2 * 3 + 2 * 2 * 3];
        for (int x = 0; x < length - 1; x++ )
        {
            int v_idx = x * 4;
            int t_idx = x * 4 * 2 * 3;
            for (int i = 0; i < 4; i++){
                triangles[t_idx + i * 6 + 0] = v_idx + i;
                triangles[t_idx + i * 6 + 1] = v_idx + i + 1;
                triangles[t_idx + i * 6 + 2] = v_idx + i + 4;

                triangles[t_idx + i * 6 + 3] = v_idx + i + 1;
                triangles[t_idx + i * 6 + 4] = v_idx + i + 5;
                triangles[t_idx + i * 6 + 5] = v_idx + i + 4;
                if (i == 3){
                    triangles[t_idx + i * 6 + 1] = v_idx + 0;
                    triangles[t_idx + i * 6 + 3] = v_idx + 0;
                    triangles[t_idx + i * 6 + 4] = v_idx + 4;
                }
            }
        }
        triangles[(length - 1) * 4 * 2 * 3 + 0] = 0;
        triangles[(length - 1) * 4 * 2 * 3 + 1] = 2;
        triangles[(length - 1) * 4 * 2 * 3 + 2] = 1;
        triangles[(length - 1) * 4 * 2 * 3 + 3] = 0;
        triangles[(length - 1) * 4 * 2 * 3 + 4] = 3;
        triangles[(length - 1) * 4 * 2 * 3 + 5] = 2;
        triangles[((length - 1) * 4 + 1) * 2 * 3 + 0] = 36;
        triangles[((length - 1) * 4 + 1) * 2 * 3 + 1] = 37;
        triangles[((length - 1) * 4 + 1) * 2 * 3 + 2] = 38;
        triangles[((length - 1) * 4 + 1) * 2 * 3 + 3] = 36;
        triangles[((length - 1) * 4 + 1) * 2 * 3 + 4] = 38;
        triangles[((length - 1) * 4 + 1) * 2 * 3 + 5] = 39;
        return triangles;
    }

    private Vector3[] GenerateVertices(int length)
    {
        // 按照如下的顺序生成一个平面mesh
        Vector3[] vertices = new Vector3[length * 4];
        for (int x = 0; x < length; x++ )
        {
            vertices[x * 4 + 0] = new Vector3((float)x - length / 2, -0.5F, -0.5F);
            vertices[x * 4 + 1] = new Vector3((float)x - length / 2,  0.5F, -0.5F);
            vertices[x * 4 + 2] = new Vector3((float)x - length / 2,  0.5F,  0.5F);
            vertices[x * 4 + 3] = new Vector3((float)x - length / 2, -0.5F,  0.5F);
        }
        return vertices;
    }

    private Vector3[] GenerateNormals(int length)
    {
        // 按照如下的顺序生成一个平面mesh
        Vector3[] normals = new Vector3[length * 4];
        for (int x = 0; x < length; x++)
        {
            normals[x * 4 + 0] = new Vector3(0, -0.5F, -0.5F).normalized;
            normals[x * 4 + 1] = new Vector3(0,  0.5F, -0.5F).normalized;
            normals[x * 4 + 2] = new Vector3(0,  0.5F,  0.5F).normalized;
            normals[x * 4 + 3] = new Vector3(0, -0.5F,  0.5F).normalized;
        }
        return normals;
    }

    private Vector2[] GenerateUV(int length)
    {
        // 按照如下的顺序生成一个平面mesh
        Vector2[] uvs = new Vector2[length * 4];
        for (int x = 0; x < length; x++)
        {
            int skip = x % 2;
            uvs[x * 4 + 0] = new Vector3(skip - 0f, skip - 0F).normalized;
            uvs[x * 4 + 1] = new Vector3(skip - 1f, skip - 0F).normalized;
            uvs[x * 4 + 2] = new Vector3(skip - 1f, skip - 1F).normalized;
            uvs[x * 4 + 3] = new Vector3(skip - 0f, skip - 1F).normalized;
        }
        return uvs;
    }
}
