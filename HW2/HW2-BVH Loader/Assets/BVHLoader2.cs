// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// 姓名：
// 学号：
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// bonus部分，注意这个scene中character的rest pose和bvh文件中的rest pose不同，两只手从水平变成斜向下45度，需要对数据做retarget
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Xml.Serialization;
// ! 在以下部分定义需要的类或结构体


// ! 在以上空白写下你的代码

public class BVHLoader2 : MonoBehaviour
{
    // 本次作业提供两个bvh文件，cartwheel.bvh和walk.bvh，可以在这里修改路径
    private string bvh_fname = "C:\\Users\\123\\Desktop\\HW2\\HW2-BVH Loader\\Assets\\BVH\\walk.bvh";
    // 从bvh文件中读取所有的关节名称，需要用关节名称来找unity scene中的game object
    private List<string> joints = new List<string>();
    // game object列表
    private List<GameObject> gameObjects = new List<GameObject>();
    // 时间戳
    private int time_step = 0;
    // bvh的帧数
    private int frame_num = 0;

    // ! 在这里声明你需要的其他数据结构
    public class JointTree
    {
        JointNode root;
        public static T DeepCopyByReflect<T>(T obj)
        {
            //如果是字符串或值类型则直接返回
            if (obj is string || obj.GetType().IsValueType) return obj;
            object retval = Activator.CreateInstance(obj.GetType());
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                try { field.SetValue(retval, DeepCopyByReflect(field.GetValue(obj))); }
                catch { }
            }
            return (T)retval;
        }
        public JointNode Root
        {
            get { return root; }
            set { root = value; }
        }
        public JointTree() { }
        public static void Dfs_write(JointNode head, string[] data, Quaternion rotB,bool flag)
        {
            if (head.NChildren == 0)
            {//End Site 不用读入数,直接计算这一步的Q
                flag = false;
                return;
            }
            Vector3 t = Vector3.zero;
            for (int j = 0; j < 3; j++)
            {
                t[j] = float.Parse(data[4 + head.Number * 3 + j]);
            }
            head.Local_r = t;
            Quaternion R = Quaternion.identity;
            for (int k = 0; k < 3; k++)
            {
                Vector3 x = Vector3.zero;
                x[head.Channels[2 - k]] = t[2 - k];
                Quaternion rot = Quaternion.Euler(x);
                R = rot * R;
            }
            if (head.Name == "lShoulder")
            {
                rotB = Quaternion.Euler(-45.0f,0.0f, 0.0f);
                flag = true;
            }
            else if (head.Name == "rShoulder")
            {
                rotB = Quaternion.Euler(45.0f,0.0f, 0.0f);
                flag = true;
            }
            if (flag)
            {
                head.Orientation = rotB * R * Quaternion.Inverse(rotB);
            }
            else
                head.Orientation = R;
            for (int i = 0; i < head.NChildren; i++)
            {
                Dfs_write(head.Children[i],data,rotB,flag);
            }
        }
        public static void Dfs_calc(JointNode head, Vector3 parent_pos, Quaternion p_orientation)
        {
            if (head.NChildren == 0)
            {//End Site
                head.Orientation = p_orientation;
                return;
            }
            head.Orientation = p_orientation * head.Orientation;
            head.Global_p = p_orientation * head.Local_p + parent_pos;
            for (int i = 0; i < head.NChildren; i++)
            {
                Dfs_calc(head.Children[i], head.Global_p, head.Orientation);
            }
        }
        public static void Dfs_update(JointNode head, ref List<GameObject> objectdata, int n)
        {
            objectdata[n].transform.SetPositionAndRotation(head.Global_p, head.Orientation);
            if (head.NChildren == 0)//End Site 
                return;
            for (int i = 0; i < head.NChildren; i++)
            {
                Dfs_update(head.Children[i], ref objectdata, head.Children[i].Order);
            }
        }

    }
    private List<JointTree> Tree = new List<JointTree>();
    // ! 在以上空白写下你的代码

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        StreamReader bvh_file = new StreamReader(new FileStream(bvh_fname, FileMode.Open));

        JointNode tmp;
        JointNode End;
        JointNode head = null;
        JointTree BVHtree = new JointTree();
        Stack<JointNode> t_stack = new Stack<JointNode>();
        string name;

        while (!bvh_file.EndOfStream)
        {
            string line = bvh_file.ReadLine();
            string str = new System.Text.RegularExpressions.Regex("[\\s]+").Replace(line, " ");
            string[] split_line = str.Split(' ');
            // 处理bvh文件中的character hierarchy部分
            if (line.Contains("ROOT") || line.Contains("JOINT"))
            {
                // ! 处理这一行的信息
                int length = split_line.Length;
                name = split_line[length - 1];
                if (line.Contains("ROOT"))
                {
                    head = new JointNode
                    {
                        Level = 0,
                        Number = 0,
                        Order = 0,
                        Name = name
                    };
                    BVHtree.Root = head;
                }
                else
                {
                    int len = 0;
                    for (int i = 0; i < line.Length; i++)
                    {
                        if (line[i] == ' ')
                            len++;
                        else
                            break;
                    }
                    tmp = new JointNode
                    {
                        Level = len / 4,
                        Number = head.Number + 1,
                        Order = head.Order + 1,
                        Name = name
                    };
                    while (t_stack.Peek().Level >= tmp.Level)
                    {

                        //print("head level is " + t_stack.Peek().Level + "tmp level is " + tmp.Level);
                        t_stack.Pop();
                    }
                    head = t_stack.Peek();
                    head.NChildren++;
                    head.Children.Add(tmp);
                    head = tmp;
                }
                joints.Add(name);
                t_stack.Push(head);
                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("End Site"))
            {
                // ! 处理这一行的信息
                End = new JointNode
                {
                    Number = head.Number,//不增加 节点 因为读的时候没有数据
                    Order = head.Order + 1,
                    Level = head.Level + 1
                };
                head.NChildren++;
                head.Children.Add(End);
                End.Name = head.Name + "_end";
                joints.Add(End.Name);
                head = End;
                t_stack.Push(head);
                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("{"))
            {
                // ! 处理这一行的信息
                continue;
                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("}"))
            {
                // ! 处理这一行的信息
                continue;
                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("OFFSET"))
            {
                // ! 处理这一行的信息
                int len = split_line.Length;
                Vector3 t = Vector3.zero;
                for (int i = len - 3; i < len; i++)
                {
                    t[3 - (len - i)] = float.Parse(split_line[i]);
                }
                head.Local_p = t;
                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("CHANNELS"))
            {
                // ! 处理这一行的信息
                int len = split_line.Length;//记录的实际是顺序信息
                int[] t = new int[3];
                for (int j = 0; j < 3; j++)
                {
                    if (split_line[len - 3 + j].Contains("X"))
                    {
                        t[j] = 0;
                    }
                    else if (split_line[len - 3 + j].Contains("Y"))
                    {
                        t[j] = 1;
                    }
                    else
                    {
                        t[j] = 2;
                    }
                }
                head.Channels = t;
                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("Frame Time"))
            {
                // ! 处理这一行的信息
                // ! 在以上空白写下你的代码
                // Frame Time是数据部分前的最后一行，读到这一行后跳出循环
                break;
            }
            else if (line.Contains("Frames:"))
            {
                // ! 处理这一行的信息
                // ! 在以上空白写下你的代码
                // 获取帧数
                frame_num = int.Parse(split_line[split_line.Length - 1]);
            }
        }

        // 接下来处理bvh文件中的数据部分
        while (!bvh_file.EndOfStream)
        {
            string line = bvh_file.ReadLine();
            string str = new System.Text.RegularExpressions.Regex("[\\s]+").Replace(line, " ");
            string[] split_line = str.Split(' ');
            // ! 解析每一行数据，保存在合适的数据结构中，用于之后update
            // 注意数据的顺序是和之前的channel顺序对应的
            // 提示：欧拉角顺序可能有多种，但都可以用三个四元数相乘得到，注意相乘的顺序
            Vector3 tp = Vector3.zero;
            for (int i = 1; i < 4; i++)//对head.global_pos进行一个赋值
            {
                tp[i - 1] = float.Parse(split_line[i]);
            }
            BVHtree.Root.Global_p = tp;
            Quaternion rot = Quaternion.identity;
            bool flag = false;
            JointTree.Dfs_write(BVHtree.Root, split_line,rot,flag);
            for (int i = 0; i < BVHtree.Root.NChildren; i++)
            {
                JointTree.Dfs_calc(BVHtree.Root.Children[i], BVHtree.Root.Global_p, BVHtree.Root.Orientation);
            }
            Tree.Add(new JointTree
            {
                Root = JointNode.DeepCopyByReflect(BVHtree.Root)
            });
            // ! 在以上空白写下你的代码
        }

        // 按关节名称获取所有的game object
        GameObject tmp_obj = new GameObject();
        for (int i = 0; i < joints.Count; i++)
        {
            tmp_obj = GameObject.Find(joints[i]);
            gameObjects.Add(tmp_obj);
        }
        // ! 在这里写下你认为需要的其他操作
        /*print("Tree " + Tree.Count);
        print("joints " + joints.Count);
        int k = 0;
        foreach (var t in joints)
        {
            print("the number is " + k + " the joint is " + t);
            k++;
        }
        int index = 0;
        foreach (var t in gameObjects)
        {
            print("the number is " + index + " the objects is " + t);
            index++;
        }*/
        // ! 在以上空白写下你的代码
    }

    // Update is called once per frame
    void Update()
    {
        //Vector3 joint_position = new Vector3(0.0F, 0.0F, 0.0F);
        //Quaternion joint_orientation = Quaternion.identity;
        // ! 定义你需要的局部变量
        JointTree.Dfs_update(Tree[time_step].Root, ref gameObjects, Tree[time_step].Root.Order);
        // ! 在以上空白写下你的代码
        // 更新时间戳
        time_step = (time_step + 1) % frame_num;
    }
}