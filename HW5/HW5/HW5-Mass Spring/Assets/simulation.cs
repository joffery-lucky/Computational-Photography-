// !!!!!!!!!!!!!!!!!!!
// 姓名：乔思琦
// 学号：1900017872
// !!!!!!!!!!!!!!!!!!!

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class simulation : MonoBehaviour {
    // mesh
    Mesh mesh;
    // 默认长宽相同
    const int width = 10;
    const int length = 10;
    // 弹簧原长
    float x0;
    // 顶点质量
    float m;
    // 重力加速度
    float g;
    // 仿真时间步长，可以修改以获得更好的效果
    float h;
    // 弹簧劲度系数，可以修改以获得更好的效果
    float k;
    // 布料一角悬挂高度
    float height;
    // mesh顶点速度
    Vector3[] vel = new Vector3[width * length];
    // ! 其他用到的成员变量
    Vector3[] force=new Vector3[width * length];//质点之间的相互作用力
    float k_d;
    // 在以上部分填写你的代码
    void Start () {
        // 获取mesh
        mesh = gameObject.GetComponent<MeshFilter>().mesh;
        // 初始化参数，可以修改以获得更好的效果
        x0 = 1.0F / width;
        m = 0.1F;
        g = -9.8F;
        h = 0.001F;
        k = 1000.0F;
        height = 0.0F;
        // ! 其他需要的操作
        k_d=1.0F;
        int n= width*length;//初始化
        for (int i = 0; i < n; i++)
        {
            vel[i] = Vector3.zero;
            force[i] = Vector3.zero;
        }
        // 在以上部分填写你的代码

        // 把布料的四角挂到高处，速度清零
        Vector3[] vertices = mesh.vertices;
        SetVertex(vertices, 0, 0, 0.0F, height, 0.0F);
        vel[0] = Vector3.zero;
        SetVertex(vertices, 9, 0, 0.9F, height, 0.0F);
        vel[9] = Vector3.zero;
        SetVertex(vertices, 0, 9, 0.0F, height, 0.9F);
        vel[90] = Vector3.zero;
        SetVertex(vertices, 9, 9, 0.9F, height, 0.9F);
        vel[99] = Vector3.zero;
        mesh.vertices = vertices;
    }
    
    void Update () {
        Vector3[] vertices = mesh.vertices;
        // 进行仿真，计算每个顶点的位置
        // ! 在以下部分实现mass spring仿真
        int[ ]dx={0,0,1,-1};
        int[ ]dz={1,-1,0,0}; 
        int n=vertices.Length;
        for(int i=0;i<n;i++){//计算Fij受到的合力方向，即aij
            int row=i/width;
            int col=i-row*width;
            force[i]=m*new Vector3(0,g,0);//重力
            for(int j=0;j<4;j++){
                int x_=row+dx[j];
                int y_=col+dz[j];
                if (x_ < 0 || y_ < 0|| y_ >= width||x_>=length)
                    continue;
                int neighbour=x_*width+y_;
                Vector3 tmp=vertices[i]-vertices[neighbour];
                force[i] += -k*(tmp.magnitude-x0)/tmp.magnitude * tmp;
            }
            Vector3 damping = -k_d * vel[i];
            force[i] += damping;
            //semi-implicit Euler
            vel[i] = vel[i] + force[i]/m * h;
            vertices[i] = vertices[i] + vel[i] * h;
        }
        // 在以上部分填写你的代码
        
        // 把布料的四角挂到高处，速度清零
        SetVertex(vertices, 0, 0, 0.0F, height, 0.0F);
        vel[0] = Vector3.zero;
        SetVertex(vertices, 9, 0, 0.9F, height, 0.0F);
        vel[9] = Vector3.zero;
        SetVertex(vertices, 0, 9, 0.0F, height, 0.9F);
        vel[90] = Vector3.zero;
        SetVertex(vertices, 9, 9, 0.9F, height, 0.9F);
        vel[99] = Vector3.zero;
        mesh.vertices = vertices;
    }

    private void SetVertex(Vector3[] verts, int idx_x, int idx_z, float x, float y, float z)
    {
        // 指定idx_x, idx_z位置的mesh顶点坐标为(x, y, z)
        verts[width * idx_z + idx_x].x = x;
        verts[width * idx_z + idx_x].y = y;
        verts[width * idx_z + idx_x].z = z;
    }
}
