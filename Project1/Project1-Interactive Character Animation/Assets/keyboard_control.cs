// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// 姓名：乔思琦
// 学号：1900017872
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BVH;


public class keyboard_control : MonoBehaviour
{
    private GameObject root;
    // Start is called before the first frame update
    public GameObject myGameObject;
    public List<GameObject> gameObjects = new List<GameObject>();
    BVHLoader bvh1 = null;
    BVHLoader bvh2 = null;
    int time_steps = 0;
    int frame_num;
    int interval = 0;
    int level = 2;//通过调整level来决定是完成哪一部分的作业要求
    bool flag = false;
    int step = 0;
    Quaternion q = Quaternion.identity;
    Vector3 trans = Vector3.zero;
    Vector3 sum = Vector3.zero;
    //总帧数

    void Start()
    {
        bvh1 = gameObject.AddComponent<BVHLoader>();
        bvh2 = gameObject.AddComponent<BVHLoader>();
        string path= System.Environment.CurrentDirectory;
        bvh1.bvh_fname = path+"\\Assets\\BVH\\0005_SideSkip001.bvh";
        bvh2.bvh_fname = path+ "\\Assets\\BVH\\0018_Walking001.bvh";
        bvh1.BVHParserInit();
        bvh1.Start();
        bvh2.BVHParserInit();
        bvh2.Start();
        frame_num = 56;//截取walk的bvh片段
        GameObject tmp_obj = null;
        for (int i = 0; i < bvh2.joints.Count; i++)
        {
            if (bvh2.joints[i].Contains("Neck") ||bvh2.joints[i].Contains("HandThumb"))
                continue;
            tmp_obj = GameObject.Find(bvh2.joints[i]);
            gameObjects.Add(tmp_obj);
        }
        root = GameObject.Find("RootJoint");
        interval = 250;
    }
    public void Dfs_slerp(JointNode head1, JointNode head2,ref List<GameObject> objectdata, int n,float timecount)
    {
        if (head1.Level == 0)
        {
            objectdata[n].transform.SetPositionAndRotation(Vector3.Lerp(head1.Global_p, head2.Global_p, timecount)
                , Quaternion.Slerp(head1.Orientation, head2.Orientation, timecount));
        }
        else
            objectdata[n].transform.rotation = Quaternion.Slerp(head1.Orientation, head2.Orientation, timecount);
        if (head1.NChildren == 0)//End Site 
            return;
        for (int i = 0; i < head1.NChildren; i++)
        {
            Dfs_slerp(head1.Children[i],head2.Children[i], ref objectdata, head1.Children[i].Order, timecount);
        }
    }
    public void setmix(BVHLoader b1,BVHLoader b2,int time_step)
    {
        Vector3 origin1 = Vector3.zero;
        Quaternion q1 = Quaternion.identity;
        if (time_step < interval)
        {
            int tmp = time_step + b2.frame_num - interval;
            float timecount = time_step / (float)interval;
            Dfs_slerp(b2.Tree[tmp].Root, b1.Tree[time_step].Root, ref gameObjects, b2.Tree[b2.frame_num - 1].Root.Order, timecount);
        }
        else if (time_step < b1.frame_num - interval)
        {
            b1.Tree[time_step].Dfs_update(b1.Tree[time_step].Root, ref gameObjects, b1.Tree[time_step].Root.Order,q1,origin1);
        }
        else if (time_step < b1.frame_num)
        {
            int tmp = interval - b1.frame_num + time_step;
            float timecount = tmp / (float)interval;
            Dfs_slerp(b1.Tree[time_step].Root, b2.Tree[tmp].Root, ref gameObjects, b1.Tree[time_step].Root.Order, timecount);
        }
        else if (time_step < b1.frame_num + b2.frame_num - interval * 2)
        {
            int tmp = time_step - b1.frame_num + interval;
            b2.Tree[tmp].Dfs_update(b2.Tree[tmp].Root, ref gameObjects, b2.Tree[tmp].Root.Order,q1,origin1);
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (level == 1)
        {
            setmix(bvh1, bvh2, time_steps);
            time_steps = (time_steps + 1) % (bvh1.frame_num + bvh2.frame_num - interval * 2);
        }
        else if (level == 2)//完成人物的任意移动
        {
            Vector3 Axis = Vector3.up;
            // 用 GetKey 或 GetKeyDown 或 GetKeyUp 交互
            if (Input.GetKeyDown(KeyCode.W))
            {
                flag = true;
                trans= q*Vector3.forward;
                step++;
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                flag = true;
                trans = q*Vector3.left;
                q *= Quaternion.AngleAxis(-90, Axis);
                step++;
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                flag = true;
                trans =q* Vector3.back;
                q = Quaternion.AngleAxis(180, Axis)*q;
                step++;
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                flag = true;
                trans = q*Vector3.right;
                q *= Quaternion.AngleAxis(90, Axis);
                step++;
            }
            if (time_steps < frame_num && flag)
            {
                time_steps++;
                Vector3 origin = new Vector3(0, 0.900847f, 0);
                Vector3 offset= sum + time_steps / (float)frame_num * trans+ origin;
                int tmp = (step % 2 == 0) ? time_steps : time_steps + 56;
                bvh2.Tree[time_steps].Dfs_update(bvh2.Tree[tmp].Root, ref gameObjects, bvh2.Tree[tmp].Root.Order, Quaternion.AngleAxis(90, Vector3.up)*q,offset);
                if (time_steps == frame_num)
                    sum += trans;
            }
            else
            {
                time_steps = 0;
                flag = false;
            }
        }
    }
}
