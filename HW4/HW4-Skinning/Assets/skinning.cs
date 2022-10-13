// !!!!!!!!!!!!!!!!!!!
// 姓名：乔思琦
// 学号：1900017872
// !!!!!!!!!!!!!!!!!!!
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class skinning : MonoBehaviour
{

    // 时间戳
    float time_stamp;
    // scene中的gameobject
    GameObject root, joint, end, capsule;
    // 胶囊体的网格模型
    Mesh mesh;
    // 胶囊体网格模型上每个顶点坐标
    Vector3[] vertices;
    // ! 其他你需要的成员变量
    float[] weights;
    Vector3[] dis;
    // 在以上空白定义需要的成员变量
    // Start is called before the first frame update
    void Start()
    {
        // 获取每个gameobject
        root = GameObject.Find("root");
        joint = GameObject.Find("joint");
        end = GameObject.Find("end");
        capsule = GameObject.Find("capsule");
        // 获取mesh和顶点
        mesh = capsule.GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;

        time_stamp = 0.0F;
        // ! 其他你认为需要的操作，如计算权重等
        int n = vertices.Length;
        weights = new float[2 * n];
        dis = new Vector3[2 * n];
        float p = 2.6f;
        for(int i = 0; i < n; i++)
        {
            float dis1 = Vector3.Distance(vertices[i], root.transform.position);
            float dis2 = Vector3.Distance(vertices[i], end.transform.position);
            float w1 = (float)(1.0f / (Mathf.Pow(dis1, p) + 0.1));
            float w2 = (float)(1.0f / (Mathf.Pow(dis2, p) + 0.1));
            float sum = w1 + w2;
            weights[i * 2] = w1 / sum;
            weights[i * 2+1] = w2 / sum;
            dis[i * 2] = vertices[i]- root.transform.position;
            dis[i * 2 + 1] = vertices[i]-end.transform.position;
        }
        // 在以上空白完成你认为需要的操作
    }

    // Update is called once per frame
    void Update()
    {
        // 控制关节旋转
        float angle0, angle1, angle2;
        time_stamp += 0.25F;
        if (time_stamp >= 540.0F)
            time_stamp -= 540.0F;
        if (time_stamp < 135.0F){
            angle1 = time_stamp;
            angle2 = 0.0F;
        }
        else if(time_stamp < 270.0F){
            angle1 = 135.0F;
            angle2 = time_stamp - 135.0F;
        }
        else if(time_stamp < 405.0F){
            angle1 = 135.0F;
            angle2 = 405.0F - time_stamp;
        }
        else{
            angle1 = 540.0F - time_stamp;
            angle2 = 0.0F;
        }
        if (time_stamp < 270.0F){
            angle0 = time_stamp;
        }
        else{
            angle0 = 540.0F - time_stamp;
        }
        root.transform.rotation = Quaternion.AngleAxis(angle0, Vector3.up);
        joint.transform.rotation = root.transform.rotation * Quaternion.AngleAxis(angle1, Vector3.up);
        joint.transform.rotation *= Quaternion.AngleAxis(angle2, Vector3.right);
        // 用一种蒙皮方式（如LBS, DQS）计算vertices中每个顶点的位置，让每个顶点位置跟着关节的转动进行变换，最终效果可以参考样例视频
        // ! 实现蒙皮
        int n = vertices.Length;
        for(int i = 0; i < n; i++)
        {
            vertices[i] = weights[i * 2] * (root.transform.rotation * dis[i * 2] + root.transform.position) +
                weights[i * 2 + 1] * (end.transform.rotation * dis[i * 2 + 1] + end.transform.position);
        }
        // 在以上空白完成你认为需要的操作
        // 更新顶点位置
        mesh.vertices = vertices;
    }
}

