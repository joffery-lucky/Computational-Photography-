// !!!!!!!!!!!!!!!!!!!
// 姓名：乔思琦
// 学号：1900017872
// !!!!!!!!!!!!!!!!!!!

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class simulation : MonoBehaviour
{
    // mesh
    Mesh mesh;
    // 方格数量
    const int length = 10;

    // 杨氏模量
    public float young = 1e5f;

    // 泊松比
    public float poisson = 0.37f;
    
    // 拉梅参数
    float mu = 0;
    float lmbda = 0;

    // 密度
    public float rho = 1000f;

    // 重力加速度
    public Vector3 g = new Vector3(0.0f, -9.8f, 0.0f);

    // 时间步
    public float time_step = 0.001f;

    // 顶点质量
    float []masses = new float[4 * length];
    // 顶点速度
    Vector3[] velo = new Vector3[4 * length];
    // 地面
    GameObject ground;
    // ! 定义其他用到的类成员变量
    Matrix4x4[] Bm = new Matrix4x4[6 * (length-1)];
    Vector3[] f = new Vector3[4 * length];
    int[,] number = new int[6, 4]{ {0,1,3,4},{1,3,4,5},{1,2,3,5},{7,2,3,5},
       {2,5,6,7},{7,3,5,4}};//第0个为顶点
    float W = 1.0f / 6.0f;
    float kd = 0;
    void setBm(Vector3 col0,Vector3 col1,Vector3 col2,int n)
    {
        Bm[n].SetColumn(0, col0);
        Bm[n].SetColumn(1, col1);
        Bm[n].SetColumn(2, col2);
        Bm[n].SetColumn(3, new Vector4(0, 0, 0, 1));
        Bm[n] = Bm[n].inverse;
    }
    // 在以上部分填写你的代码
    void Start()
    {
        // 获取mesh
        mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        // 速度清零
        for (int v = 0; v < 4 * length; v++)
            velo[v] = Vector3.zero;

        // 计算拉梅参数
        mu = young / (2.0f * (1.0f + poisson));
        lmbda = young * poisson / ((1.0f + poisson) * (1.0f - 2.0f * poisson));

        //获取地面
        ground = GameObject.Find("Ground");
        // ! 其他需要的操作，如记录每个四面体的顶点序号，计算四面体顶点质量
        kd = 150.0f;
        for (int i = 0; i < length - 1; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                Vector3 col0 = vertices[4 * i + number[j, 1]] - vertices[4 * i + number[j, 0]];
                Vector3 col1 = vertices[4 * i + number[j, 2]] - vertices[4 * i + number[j, 0]];
                Vector3 col2 = vertices[4 * i + number[j, 3]] - vertices[4 * i + number[j, 0]];
                setBm(col0, col1, col2, 6 * i + j);
                for (int k = 0; k < 4; k++)
                    masses[4 * i + number[j, k]] += rho / 4.0f * W;
            }
        }
        // 在以上部分填写你的代码
    }

    // 注意Matrix4x4对象不支持用"+" "-"等符号进行加减，可以考虑用以下函数
    // 矩阵加法
    static Matrix4x4 Plus(Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 m = Matrix4x4.zero;
        for (int i = 0; i < 4; ++i)
            m.SetColumn(i, a.GetColumn(i) + b.GetColumn(i));
        return m;
    }
    // 矩阵减法
    static Matrix4x4 Minus(Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 m = Matrix4x4.zero;
        for (int i = 0; i < 4; ++i)
            m.SetColumn(i, a.GetColumn(i) - b.GetColumn(i));
        return m;
    }
    // 矩阵数乘
    static Matrix4x4 ScalarMultiply(Matrix4x4 a, float b)
    {
        Matrix4x4 m = Matrix4x4.zero;
        for (int i = 0; i < 4; ++i)
            m.SetColumn(i, a.GetColumn(i) * b);
        return m;
    }
    static Matrix4x4 ScalarMultiply(float b, Matrix4x4 a)
    {
        Matrix4x4 m = Matrix4x4.zero;
        for (int i = 0; i < 4; ++i)
            m.SetColumn(i, a.GetColumn(i) * b);
        return m;
    }

    Matrix4x4 CalcH(Vector3 col0, Vector3 col1, Vector3 col2,int n)
    {
        Matrix4x4 Ds=new Matrix4x4();
        Ds.SetColumn(0, col0);
        Ds.SetColumn(1, col1);
        Ds.SetColumn(2, col2);
        Ds.SetColumn(3, new Vector4(0, 0, 0, 1));
        Matrix4x4 F = Ds * Bm[n];
        Matrix4x4 strain = Minus(ScalarMultiply(0.5f, Plus(F, F.transpose)),Matrix4x4.identity);
        Matrix4x4 P = Plus(ScalarMultiply(2* mu,strain),
        ScalarMultiply(lmbda*(strain[0,0]+ strain[1, 1]+ strain[2, 2]), Matrix4x4.identity));
        P[3, 3] = 1;
        Matrix4x4 H = ScalarMultiply(P*(Bm[n].transpose),-W);
        return H;
    }
    void Update()
    {
        Vector3[] vertices = mesh.vertices;
        // 这里是实时仿真的代码，如果你的仿真结果过于卡顿，可以手动改成以下设置
        // int simuSteps = 10;
        int simuSteps = (int)(Mathf.Ceil(Time.deltaTime / time_step));
        for (int simuCnt = 0; simuCnt < simuSteps; ++simuCnt)
        {
            // 进行一步仿真，计算每个顶点受到的力，然后根据受力计算顶点速度，更新顶点位置
            // bonus: 可以进一步考虑物体碰撞到地面后反弹，即判断顶点位置和地面高度的关系检测碰撞，再根据碰撞对相应顶点施加外力
            // 地面上表面高度
            float ground_height = ground.transform.position.y + 0.5f;
            // ! 在以下部分实现FEM仿真
            for (int v = 0; v < 4 * length; v++)
                f[v] = Vector3.zero;
            for (int i = 0; i < length - 1; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    Vector3 col0 = vertices[4 * i + number[j, 1]] - vertices[4 * i + number[j, 0]];
                    Vector3 col1 = vertices[4 * i + number[j, 2]] - vertices[4 * i + number[j, 0]];
                    Vector3 col2 = vertices[4 * i + number[j, 3]] - vertices[4 * i + number[j, 0]];
                    Matrix4x4 H = CalcH(col0, col1, col2, 6 * i+j);
                    Vector3 sum = Vector3.zero;
                    for (int k = 0; k < 3; k++) {
                        f[4 * i + number[j, k + 1]] += (Vector3)H.GetColumn(k);
                        sum += (Vector3)H.GetColumn(k);
                    }
                    f[4 * i + number[j, 0]] -= sum;
                }
            }
            for (int i = 0; i < velo.Length; i++) {
                //Semi-implicit Euler
                velo[i] = velo[i] + ((f[i]-kd*velo[i])/ masses[i]+ g)* time_step;
                vertices[i] = vertices[i] + velo[i]* time_step;
                if (vertices[i].y < ground_height)
                {
                    vertices[i].y = ground_height;
                    velo[i].y = -0.5f*velo[i].y;//碰撞能量损失
                }
            }
            // 在以上部分填写你的代码

            // 固定长方体一端
            velo[0] = Vector3.zero;
            velo[1] = Vector3.zero;
            velo[2] = Vector3.zero;
            velo[3] = Vector3.zero;
            vertices[0] = new Vector3(-5.0f, -0.5F, -0.5F);
            vertices[1] = new Vector3(-5.0f,  0.5F, -0.5F);
            vertices[2] = new Vector3(-5.0f,  0.5F,  0.5F);
            vertices[3] = new Vector3(-5.0f, -0.5F, 0.5F);

        }
        // 更新顶点位置
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}
