// !!!!!!!!!!!!!!!!!!!!!!!!!!
// 姓名：乔思琦
// 学号：1900017872
// !!!!!!!!!!!!!!!!!!!!!!!!!!
using System;
using System.Collections.Generic;
using UnityEngine;

public class IK : MonoBehaviour
{
    // 第一个机械臂上的关节名称
    private List<string> jointList_1 = new List<string>{"Joint1_1", "Joint1_2", "Joint1_3", "Joint1_4", "Joint1_5"};
    // 第二个机械臂上的关节名称
    private List<string> jointList_2 = new List<string>{"Joint2_1", "Joint2_2", "Joint2_3", "Joint2_4", "Joint2_5"};
    // 第一个机械臂上的根节点位置
    private Vector3 root_position_1 = new Vector3(-0.2F, 0.0F, 0.0F);
    // 第二个机械臂上的根节点位置
    private Vector3 root_position_2 = new Vector3(0.2F, 0.0F, 0.0F);
    // 每段机械臂长度
    private List<Vector3> offsets = new List<Vector3>();
    // 第一个机械臂上需要控制的GameObject
    private List<GameObject> objectList_1 = new List<GameObject>();
    // 第二个机械臂上需要控制的GameObject
    private List<GameObject> objectList_2 = new List<GameObject>();
    // 第一个机械臂上需要控制的关节的局部旋转
    private List<Quaternion> rotation_1 = new List<Quaternion>();
    // 第二个机械臂上需要控制的关节的局部旋转
    private List<Quaternion> rotation_2 = new List<Quaternion>();
    // 两个机械臂末端节点的目标object，每一帧需要末端节点与target位置尽可能重合
    private GameObject target;
    // 第一个机械臂的末端节点，每次迭代通过FK计算其位置，使之与target尽可能重合
    private GameObject end_joint_1;
    // 第二个机械臂的末端节点，每次迭代通过FK计算其位置，使之与target尽可能重合
    private GameObject end_joint_2;
    // 时间戳
    private int time_stamp;
    // ! 定义你需要的其他数据结构
    class Bone
    {
        public Vector3 position;
        public Vector3 off;
        public Quaternion R = Quaternion.identity;
        public Quaternion Local_R= Quaternion.identity;
        public Bone parent;
        public string name;
        public Bone(Bone a, Vector3 b,string str)
        {
            if (a != null)
            {
                parent = a;
                position = a.position + b;
                off = b;
            }
            else//root
            {
                position = b;
                off = Vector3.zero;
            }
            name = str;
        }
    }
    List<Bone> bones1 = new List<Bone>();
    List<Bone> bones2 = new List<Bone>();
    // ! 在以上空白处填写你的代码 
     void CalcJacobian(ref float[,] matrix,float[]theta,Vector3 target)
     {
        Quaternion R_p = Quaternion.identity;
        for (int i = 0; i <= 4; i++)
        {
            float thetax, thetay, thetaz;
            //得到这次计算Jacobian的初始值
            thetax = theta[i * 3];
            thetay = theta[i * 3 + 1];
            thetaz = theta[i * 3 + 2];
            //得到三个欧拉角的旋转轴
            Vector3 axisx, axisy, axisz;
            axisx = R_p * Vector3.right;
            axisy = R_p * Quaternion.AngleAxis(thetax, axisx) * Vector3.up;
            axisz = R_p * Quaternion.AngleAxis(thetax, axisx) * Quaternion.AngleAxis(thetay, Vector3.up) * Vector3.forward;
            //欧拉顺序为xyz
            R_p = Quaternion.AngleAxis(thetax, axisx) * Quaternion.AngleAxis(thetay, axisy) * Quaternion.AngleAxis(thetaz, axisz) * R_p;
            Vector3[] cross = new Vector3[3];
            Vector3 r = target - bones2[i].position;
            cross[0] = Vector3.Cross(axisx, r);
            cross[1] = Vector3.Cross(axisy, r);
            cross[2] = Vector3.Cross(axisz, r);
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    matrix[j, i * 3 + k] = cross[k][j];
                }
            }
            //更新骨骼存储的全局位置以及旋转
            if (i >= 1)
            {
                bones2[i].position = bones2[i - 1].position + bones2[i - 1].R * bones2[i].off;
            }
            bones2[i].R = R_p;
        }
        bones2[5].position= bones2[4].position + bones2[4].R * bones2[5].off;
    }
     void CalcCCD(Vector3 target_position)
     {
        for (int i = 4; i >= 0; i--)
        {
            Vector3 dir_to = target_position - bones1[i].position;
            Vector3 dir_from = bones1[5].position - bones1[i].position;
            Vector3 Axis = Vector3.Cross(dir_from,dir_to);
            float theta = Vector3.SignedAngle(dir_from, dir_to, Axis);
            Quaternion rot = Quaternion.AngleAxis(theta, Axis);
            //此时i作为根节点 更新后面的坐标
            bones1[i].Local_R = rot * bones1[i].Local_R;
            bones1[i].R = rot * bones1[i].R;
            for (int j = i + 1; j <= 5; j++)//更新这个关节之后的关节包括end_joint的全局位置和全局旋转
            {
                bones1[j].R = bones1[j - 1].R * bones1[j].Local_R;
                bones1[j].position = bones1[j - 1].position + bones1[j - 1].R * bones1[j].off;
            }
        }
     }

    // Start is/. called before the first frame update
    void Start()
    {
        // 找到所有需要的Game Object
        target = GameObject.Find("Target");
        end_joint_1 = GameObject.Find("EndJoint1");
        end_joint_2 = GameObject.Find("EndJoint2");
        foreach (string joint_name in jointList_1){
            objectList_1.Add(GameObject.Find(joint_name));
        }
        foreach (string joint_name in jointList_2){
            objectList_2.Add(GameObject.Find(joint_name));
        }
        // 初始化offset列表，两个机械臂的每一段长度都是相同的
        offsets.Add(new Vector3(0.0F, 1.0F, 0.0F));
        offsets.Add(new Vector3(0.0F, 0.8F, 0.0F));
        offsets.Add(new Vector3(0.0F, 0.6F, 0.0F));
        offsets.Add(new Vector3(0.0F, 0.4F, 0.0F));
        offsets.Add(new Vector3(0.0F, 0.2F, 0.0F));
        // 初始化关节旋转
        for (int joint_idx = 0; joint_idx < 5; joint_idx ++){
            rotation_1.Add(new Quaternion());
            rotation_2.Add(new Quaternion());
        }
        // 初始化时间戳
        time_stamp = 0;
        // ! 在这里进行需要的其他操作
        //存入每个Joint的初始转轴，初始全局位置
        Bone root2 = new Bone(null, root_position_2,jointList_2[0]);
        Bone root1 = new Bone(null, root_position_1,jointList_1[0]);
        int i = 0;
        bones2.Add(root2);
        bones1.Add(root1);
        Bone tmp = root2;
        for(i=0;i<4;i++)
        {
            Bone now = new Bone(tmp, offsets[i],jointList_2[i+1]);
            bones2.Add(now);
            tmp = now;
        }
        bones2.Add(new Bone(tmp, offsets[4], "End_Joint"));
        Bone tmp1 = root1;
        for (i = 0; i < 4; i++)
        {
            Bone now = new Bone(tmp1, offsets[i], jointList_1[i + 1]);
            bones1.Add(now);
            tmp1 = now;
        }
        bones1.Add(new Bone(tmp1, offsets[4], "End_Joint"));
     
        // ! 在以上空白填写你的代码
    }

    // Update is called once per frame
    void Update()
    {
        // 更新target位置，每一帧都要控制两个机械臂的末端节点与target重合
        double phase = (time_stamp % 360) / 180.0 * Math.PI;
        Vector3 target_position = new Vector3(2F * (float)Math.Cos(phase), (float)Math.Sin(phase), 1.5F * (float)Math.Sin(phase));
        target.transform.position = target_position;
        // 用一种启发式IK算法控制objectList_1中关节的局部旋转，使机械臂末端与target位置重合
        // ! 计算第一个机械臂上每个关节的全局旋转，保存在rotation_1中
        //while (Vector3.Distance(target_position,bones1[5].position)>0.01f)
        int times = 0;
        while(times < 101)
        {//末端端点与target重合终止
            CalcCCD(target_position);
            times++;
        }
        for (int k = 0; k < 5; k++)
        {
            rotation_1[k] = bones1[k].R;
        }
        // ! 在以上空白写下你的计算过程
        for (int i = 0; i < 5; i++){
            objectList_1[i].transform.rotation = rotation_1[i]; 
        }
        
        // 用一种基于Jacobian的方法控制objectList_2关节的局部旋转，使机械臂末端与target位置重合
        // ! 计算第二个机械臂上每个关节的全局旋转，保存在rotation_2中
        // 注意欧拉角的旋转顺序
        // 获取GameObject在其父节点坐标系下的局部旋转，可以参考Unity文档中的Transform.localEulerAngles
        float[] theta = new float[15];
        for (int i = 0; i < 15; i++)
            theta[i] = 0.0f;
        float alpha = 0.5f;
        int time = 0;
        while (time<500)
        {
            time++;
            Vector3 dp = target_position - bones2[5].position;
            float[,] Jacobian = new float[3, 15];
            CalcJacobian(ref Jacobian,theta,target_position);
            for(int i = 0; i < 15; i++)
            {
                float dtheta = 0.0f;
                for(int j = 0; j < 3; j++)
                {
                    dtheta += dp[j] * Jacobian[j, i];
                }
                theta[i] += alpha * dtheta;
            }
        }
        for (int k = 0; k < 5; k++)
        {
            rotation_2[k] = bones2[k].R;
        }
        // ! 在以上空白写下你的计算过程
        for (int i = 0; i < 5; i++){
            objectList_2[i].transform.rotation = rotation_2[i];
        }
        // 更新时间戳
        time_stamp += 1;
    }
}
