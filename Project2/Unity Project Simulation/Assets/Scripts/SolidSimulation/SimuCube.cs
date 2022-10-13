
// !!!!!!!!!!!!!!!!!!!
// 姓名：乔思琦
// 学号：1900017872
// !!!!!!!!!!!!!!!!!!!

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

public static class Matrix4x4Extension
{
    public static Matrix4x4 Add(this Matrix4x4 lhs, Matrix4x4 rhs)
    {
        Matrix4x4 ret = Matrix4x4.zero;
        for (int i = 0; i < 4; ++i)
            ret.SetColumn(i, lhs.GetColumn(i) + rhs.GetColumn(i));

        return ret;
    }

    public static Matrix4x4 Substract(this Matrix4x4 lhs, Matrix4x4 rhs)
    {
        Matrix4x4 ret = Matrix4x4.zero;
        for (int i = 0; i < 4; ++i)
            ret.SetColumn(i, lhs.GetColumn(i) - rhs.GetColumn(i));

        return ret;
    }

    public static Matrix4x4 Negative(this Matrix4x4 lhs)
    {
        Matrix4x4 ret = Matrix4x4.zero;
        for (int i = 0; i < 4; ++i)
            ret.SetColumn(i, -lhs.GetColumn(i));

        return ret;
    }

    public static Matrix4x4 Multiply(this Matrix4x4 lhs, float s)
    {
        Matrix4x4 ret = Matrix4x4.zero;
        for (int i = 0; i < 4; ++i)
            ret.SetColumn(i, lhs.GetColumn(i) * s);

        return ret;
    }
    public static Matrix4x4 Multiply(this float s, Matrix4x4 lhs)
    {
        Matrix4x4 ret = Matrix4x4.zero;
        for (int i = 0; i < 4; ++i)
            ret.SetColumn(i, s * lhs.GetColumn(i));

        return ret;
    }

    public static Matrix4x4 Multiply(this Matrix4x4 lhs, Matrix4x4 rhs)
    {
        return lhs * rhs;
    }

    public static float Trace(this Matrix4x4 lhs)
    {
        return lhs.m00 + lhs.m11 + lhs.m22;
    }


    public static Vector3 Add3(this Vector3 lhs, Vector4 rhs)
    {
        return new Vector3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
    }
    public static Vector3 Add3(this Vector4 lhs, Vector3 rhs)
    {
        return new Vector3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
    }
    public static Vector3 Add3(this Vector4 lhs, Vector4 rhs)
    {
        return new Vector3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
    }
    public static Vector3 Add3(this Vector3 lhs, Vector3 rhs)
    {
        return lhs + rhs;
    }

    public static Vector3 AddAssign3(ref this Vector3 lhs, Vector4 rhs)
    {
        lhs.x += rhs.x;
        lhs.y += rhs.y;
        lhs.z += rhs.z;
        return lhs;
    }
    public static Vector3 AddAssign3(ref this Vector4 lhs, Vector3 rhs)
    {
        lhs.x += rhs.x;
        lhs.y += rhs.y;
        lhs.z += rhs.z;
        return lhs;
    }
    public static Vector3 AddAssign3(ref this Vector4 lhs, Vector4 rhs)
    {
        lhs.x += rhs.x;
        lhs.y += rhs.y;
        lhs.z += rhs.z;
        return lhs;
    }
    public static Vector3 AddAssign3(ref this Vector3 lhs, Vector3 rhs)
    {
        return lhs += rhs;
    }
}


public class SimuCube : MonoBehaviour
{
    // 杨氏模量
    public float youngModulus = 1e6f;

    // 泊松比
    public float possionRatio = 0.47f;

    // 密度
    public float density = 1000f;

    // 重力加速度
    public Vector3 gravity = new Vector3(0.0f, -9.8f, 0.0f);

    // 时间步
    public float simuTimeStep = 0.001f;

    // 最大仿真次数, 防止过于卡顿
    public int maxSimuSteps = -1;


    // size
    readonly float cubeSize = 1.0f;
    readonly int subdivide = 2;

    float []masses;
    Vector3[]positions;
    Vector3[]velocity;
    Vector3[]forces;

    // ground
    GameObject groundObject;

    // mesh
    MeshFilter mesh;
    int[] meshVertexMap;

    Matrix4x4 identityMatrix = Matrix4x4.identity;

    private class Tetrahedron
    {
        public int[] vi = new int[4];
        public float volume = 1.0f / 6.0f;
        public Matrix4x4 Dm = Matrix4x4.identity;
        public Matrix4x4 Bm = Matrix4x4.identity;
    }
    Tetrahedron[] tets;

    // 计算拉梅参数
    float mu = 0;
    float lmbda = 0;

    int length;
    int[,] number = new int[6, 4]{ {0,1,16,4},{5,1,4,16},{1,17,16,5},{16,5,17,20},
       {21,5,17,20},{20,16,5,4}};//第0个为顶点
    float damp = 0.998f;

    Matrix4x4 CalcH(Vector3 col0, Vector3 col1, Vector3 col2, Matrix4x4 M, int n)
    {
        Matrix4x4 Ds = new Matrix4x4();
        Ds.SetColumn(0, col0);
        Ds.SetColumn(1, col1);
        Ds.SetColumn(2, col2);
        Ds.SetColumn(3, new Vector4(0, 0, 0, 1));
        Matrix4x4 F = Ds * tets[n].Bm;
        if (F.determinant < 0)
        {
            var A = Matrix<double>.Build.Dense(3,3);
            for(int i=0;i<3;i++)
                for(int j = 0; j < 3; j++)
                {
                    A[i, j] = F[i, j];
                }
            Evd<double> evd = (A.Transpose()*A).Evd();
            var V =evd.EigenVectors;
            var temp = evd.EigenValues;
            var F_d= Matrix<double>.Build.Dense(3, 3);
            F_d[0, 0] = Math.Sqrt(temp[0].Real);
            F_d[1, 1] = Math.Sqrt(temp[1].Real);
            F_d[2, 2] = Math.Sqrt(temp[2].Real);
            var U = A * V * F_d.Inverse();
            if (V.Determinant() < 0.0)
                V.SetColumn(0, 0, 3, -V.Column(0));
            A = U * F_d * V.Transpose();
            //print("V " + V.Transpose().Determinant()+ "U " + U.Determinant());
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    F[i, j] = (float)A[i, j];
                }
            //print(A.Determinant());
        }
        //StVk
        Matrix4x4 strain = Matrix4x4Extension.Multiply(0.5f, Matrix4x4Extension.Substract(Matrix4x4Extension.Multiply(F, F.transpose), Matrix4x4.identity));
        Matrix4x4 P = Matrix4x4Extension.Multiply(Matrix4x4Extension.Add(Matrix4x4Extension.Multiply(2 * mu, strain),
        Matrix4x4Extension.Multiply(lmbda * Matrix4x4Extension.Trace(strain), Matrix4x4.identity)),F);
        P[3, 3] = 1;
        Matrix4x4 H = Matrix4x4Extension.Multiply(P * (tets[n].Bm.transpose), -tets[n].volume);
        return H;
    }
  
    // 在以上部分填写你的代码
    void Start()
    {
        int numVerticesPerDim = subdivide + 2;
        int numVertices = numVerticesPerDim * numVerticesPerDim * numVerticesPerDim;

        masses = new float[numVertices];
        positions = new Vector3[numVertices];
        velocity = new Vector3[numVertices];
        forces = new Vector3[numVertices];
        
        // 初始化仿真格点
        int posIdx = 0;
        for (int i = 0; i < numVerticesPerDim; ++i)
            for (int j = 0; j < numVerticesPerDim; ++j)
                for(int k = 0; k < numVerticesPerDim; ++k)
                {
                    var offset = cubeSize * new Vector3((float)i / (subdivide + 1), (float)j / (subdivide + 1), (float)k / (subdivide + 1));
                    positions[posIdx] = transform.TransformPoint(offset);
                    ++posIdx;
                }

        groundObject = GameObject.Find("Ground");

        // 获取mesh
        mesh = GetComponentInChildren<MeshFilter>();
        var vertices = mesh.mesh.vertices;
        meshVertexMap = new int[vertices.Length];
        
        Vector3 vMin = vertices[0];
        Vector3 vMax = vertices[0];
        foreach (var v in vertices)
        {
            vMin = Vector3.Min(v, vMin);
            vMax = Vector3.Max(v, vMax);
        }

        var meshOffset = vMin;
        var meshScale = (vMax - vMin) / cubeSize;
        var invScale = new Vector3(1.0f / meshScale.x, 1.0f / meshScale.y, 1.0f / meshScale.z);

        for (int i = 0; i < vertices.Length; i++)
        {
            var pos = (vertices[i] - meshOffset);
            pos.Scale(invScale);//相对位置
            var idx = pos * (subdivide + 1);
            int xi = Mathf.Clamp(Mathf.RoundToInt(idx.x), 0, numVerticesPerDim - 1);
            int yi = Mathf.Clamp(Mathf.RoundToInt(idx.y), 0, numVerticesPerDim - 1);
            int zi = Mathf.Clamp(Mathf.RoundToInt(idx.z), 0, numVerticesPerDim - 1);
            meshVertexMap[i] = (xi * numVerticesPerDim + yi) * numVerticesPerDim + zi;
        }
        // 计算拉梅参数
        mu = youngModulus / (2.0f * (1.0f + possionRatio));
        lmbda = youngModulus * possionRatio / ((1.0f + possionRatio) * (1.0f - 2.0f * possionRatio));
        
        
        // 计算四面体参数
        int num = (subdivide + 1) * (subdivide + 1) * (subdivide + 1);
        tets = new Tetrahedron[num * 6];
    
        int cnts = 0;
        for (int i = 0; i < numVertices ; i++)
        {
            int ind = i;// meshVertexMap[i];
            bool flag = false;
            if (ind % 4 == 3 || ind > 42)
                flag = true;
            for(int l = 0; l < 3; l++)
            {
                if ((ind == l * 16+ 12) || (ind == l * 16 + 13) || (ind == l * 16 + 14))
                    flag = true;
            }
            if (flag)
                continue;
            for (int j = 0; j < 6; j++)
            {
                Vector3 col0 = positions[ind + number[j, 1]] - positions[ind + number[j, 0]];
                Vector3 col1 = positions[ind + number[j, 2]] - positions[ind + number[j, 0]];
                Vector3 col2 = positions[ind + number[j, 3]] - positions[ind + number[j, 0]];

                tets[cnts] = new Tetrahedron();
                tets[cnts].Dm.SetColumn(0, col0);
                tets[cnts].Dm.SetColumn(1, col1);
                tets[cnts].Dm.SetColumn(2, col2);
                tets[cnts].Dm.SetColumn(3, new Vector4(0, 0, 0, 1));
                tets[cnts].Bm = tets[cnts].Dm.inverse;
                tets[cnts].volume = 1.0f / 6.0f;// * tets[cnts].Bm.determinant);
                

                for (int k = 0; k < 4; k++)
                {
                   tets[cnts].vi[k] = ind + number[j, k];
                   masses[ind + number[j, k]] += density / 4.0f * tets[cnts].volume;
                }
                cnts++;
            }
        }
        length = numVertices;
        // 其他初始化代码
        // 在以上部分填写你的代码
    }


    void FixedUpdate()
    {
        int simuSteps = (int)(Mathf.Round(Time.deltaTime / simuTimeStep));
        if (maxSimuSteps > 0)
            simuSteps = Mathf.Min(simuSteps, maxSimuSteps);

        for (int simuCnt = 0; simuCnt < simuSteps; ++simuCnt)
        {
            UpdateFunc();
        }


        // 更新顶点位置
        var vertices = mesh.mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            var pos = positions[meshVertexMap[i]];
            pos = mesh.transform.InverseTransformPoint(pos);
            vertices[i] = pos;
        }
        mesh.mesh.vertices = vertices;
        mesh.mesh.RecalculateNormals();
    }

    void UpdateFunc()
    {
        float groundHeight = 0;
        if (groundObject != null)
            groundHeight = groundObject.transform.position.y + groundObject.transform.localScale.y / 2;

        // 进行仿真，计算每个顶点的位置
        // ! 在以下部分实现FEM仿真
        for (int v = 0; v < length; v++)
            forces[v] = Vector3.zero;
        int cnts = 0;

        Vector3[] deform = new Vector3[4];
        Vector3 dV = Vector3.zero;
        float scale = 0.05f;
        float f = 0.01f;
        float a = 1.0f;
        int dir = 0;
        if (Input.GetKeyDown(KeyCode.W))
        {
            dV += scale * Vector3.forward;
            a = 0.95f;
            dir = 4;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            dV += scale * Vector3.back;
            a = 0.95f;
            dir = 4;

        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            dV += scale * Vector3.left;
            a = 0.95f;
            dir = 4;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            dV += scale * Vector3.right;
            a = 0.95f;
            dir = 4;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            a = 0.9f;
            dir = 4;
        }

        for (int i = 0; i < length; i++)
        {
            int ind = i;// meshVertexMap[i];
            bool flag = false;
            if (ind % 4 == 3 || ind > 42)
                flag = true;
            for (int l = 0; l < 3; l++)
            {
                if ((ind == l * 16 + 12) || (ind == l * 16 + 13) || (ind == l * 16 + 14))
                    flag = true;
            }
            if (flag)
                continue;
            for (int j = 0; j < 6; j++)
            { 
                Vector3 col0 = positions[ind + number[j, 1]] - positions[ind + number[j, 0]];
                Vector3 col1 = positions[ind + number[j, 2]] - positions[ind + number[j, 0]];
                Vector3 col2 = positions[ind + number[j, 3]] - positions[ind + number[j, 0]];
                Matrix4x4 M = tets[cnts].Dm;
                
                for (int n = 1; n < 4; n++) {
                    if (Math.Abs(tets[cnts].vi[0] - tets[cnts].vi[n]) == dir)
                        M.SetColumn(n-1, a * tets[cnts].Dm.GetColumn(n-1));
                }

                tets[cnts].Bm = M.inverse;
                Matrix4x4 H = CalcH(col0, col1, col2, M , cnts);
                cnts++;
                Vector3 sum = Vector3.zero;
                for (int k = 0; k < 3; k++)
                {
                    forces[ind + number[j, k + 1]] += (Vector3)H.GetColumn(k);
                    sum += (Vector3)H.GetColumn(k);
                }
                forces[ind + number[j, 0]] -= sum;
            }
        }

        for (int i = 0; i < velocity.Length; i++)
        {
            //Semi-implicit Euler
            Vector3 dv = Vector3.zero;
         
            velocity[i] += (forces[i] / masses[i] - damp * velocity[i] + gravity) * simuTimeStep + dV;
            positions[i] += velocity[i] * simuTimeStep;

            float dis = positions[i].y - groundHeight;
            if (dis < 0)
            {
               Vector3 normal = Vector3.up;
               float Ut = 0.5f;
               float restitution = 0.4f;
               float viN = velocity[i].y;
               if (viN >= 0) return;
               Vector3 v_Ni = viN * normal;                          
               Vector3 v_Ti = velocity[i] - v_Ni;
               float l = Mathf.Max(1.0f - Ut * (1 + restitution) * v_Ni.magnitude / v_Ti.magnitude , 0.0f);
               positions[i] -= dis * normal;
               Vector3 v_Ni_new = -restitution * v_Ni; ;
               Vector3 v_Ti_new = l * v_Ti;
               velocity[i] = v_Ni_new + v_Ti_new;
            }
        }
        //// 在以上部分填写你的代码

    }
}
