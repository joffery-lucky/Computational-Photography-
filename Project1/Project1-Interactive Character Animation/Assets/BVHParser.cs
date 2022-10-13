// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// 姓名：乔思琦
// 学号：1900017872
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Xml.Serialization;

// ! 在以下部分定义需要的类或结构体
namespace BVH
{
    public class JointNode
    {
        private string name;
        private int nchildren = 0;
        private int level = -1;
        private int number = -1;
        private int order = -1;
        List<JointNode> children;
        int[] channels;//用来记录motion里的旋转顺序
        Vector3 local_pos = Vector3.zero;
        Vector3 local_rota = Vector3.zero;
        Vector3 global_pos = Vector3.zero;
        Quaternion orientation = Quaternion.identity;
        public static T DeepCopyByReflect<T>(T obj)
        {
            //如果是字符串或值类型则直接返回
            object retval;
            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer xml = new XmlSerializer(typeof(T));
                xml.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                retval = xml.Deserialize(ms);
                ms.Close();
            }
            return (T)retval;
        }
        public JointNode()
        {
            children = new List<JointNode>();
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public int NChildren
        {
            get { return nchildren; }
            set { nchildren = value; }
        }

        public int Level
        {
            get { return level; }
            set { level = value; }
        }

        public int Number
        {
            get { return number; }
            set { number = value; }
        }
        public int Order
        {
            get { return order; }
            set { order = value; }
        }
        public List<JointNode> Children
        {
            get { return children; }
            set { children = value; }
        }
        public int[] Channels
        {
            get { return channels; }
            set { channels = value; }
        }
        public Vector3 Local_p
        {
            get { return local_pos; }
            set { local_pos = value; }
        }

        public Vector3 Local_r //固定以x，y，z存储
        {
            get { return local_rota; }
            set { local_rota = value; }
        }

        public Vector3 Global_p
        {
            get { return global_pos; }
            set { global_pos = value; }
        }

        public Quaternion Orientation
        {
            get { return orientation; }
            set { orientation = value; }
        }

    }
    
    public class BVHLoader : MonoBehaviour
    {
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
            public void Dfs_write(JointNode head, string[] data)
            {
                if (head.NChildren == 0 && head.Name != "R_Wrist_End" && head.Name != "l_Wrist_End")//End Site 不用读入数,直接计算这一步的Q
                    return;
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
                head.Orientation = R;
                for (int i = 0; i < head.NChildren; i++)
                {
                    Dfs_write(head.Children[i], data);
                }
            }
            public void Dfs_calc(JointNode head, Vector3 parent_pos, Quaternion p_orientation)
            {
                head.Global_p = p_orientation * head.Local_p + parent_pos;
                if (head.NChildren == 0 && head.Name != "R_Wrist_End" && head.Name != "l_Wrist_End")
                {//End Site
                    head.Orientation = p_orientation;
                    return;
                }
                head.Orientation = p_orientation * head.Orientation;
                for (int i = 0; i < head.NChildren; i++)
                {
                    Dfs_calc(head.Children[i], head.Global_p, head.Orientation);
                }
            }
            public void Dfs_update(JointNode head, ref List<GameObject> objectdata, int n,Quaternion q,Vector3 origin)
            {
                if (head.Level == 0)
                {
                    Vector3 pos;
                    if (origin != Vector3.zero)
                    {
                        pos = origin;
                    }
                    else
                    {
                         pos =head.Global_p;
                    }
                    objectdata[n].transform.SetPositionAndRotation(pos, q * head.Orientation);
                }
                else
                    objectdata[n].transform.rotation = q * head.Orientation;
                if (head.NChildren == 0 )//End Site 
                    return;
                for (int i = 0; i < head.NChildren; i++)
                {
                    Dfs_update(head.Children[i], ref objectdata, head.Children[i].Order,q,origin);
                }
            }

        }
        public string bvh_fname = null; 
        public List<string> joints = new List<string>();
        // game object列表
        public Dictionary<string, string> relation = new Dictionary<string , string>();
        // bvh的帧数
        public int frame_num = 0;
        public List<JointTree> Tree = new List<JointTree>();
        public void BVHParserInit()//把SFU数据映射到对应的人物模型
        {
            relation.Add("Hips", "RootJoint");
            relation.Add("LeftUpLeg", "lHip");
            relation.Add("LeftLeg", "lKnee");
            relation.Add("LeftFoot", "lAnkle");
            relation.Add("LeftToeBase", "lToeJoint");
            relation.Add("RightUpLeg", "rHip");
            relation.Add("RightLeg", "rKnee");
            relation.Add("RightFoot", "rAnkle");
            relation.Add("RightToeBase", "rToeJoint");
            relation.Add("Spine", "pelvis_lowerback");
            relation.Add("Spine1", "lowerback_torso");//这里我们的模型相比SFU数据集缺少Neck数据
            relation.Add("Head", "torso_head");
            relation.Add("LeftShoulder", "lTorso_Clavicle");
            relation.Add("LeftArm", "lShoulder");
            relation.Add("LeftForeArm", "lElbow");
            relation.Add("LeftHand", "lWrist");
            relation.Add("L_Wrist_End", "lWrist_end");
            relation.Add("RightShoulder", "rTorso_Clavicle");
            relation.Add("RightArm", "rShoulder");
            relation.Add("RightForeArm", "rElbow");
            relation.Add("RightHand", "rWrist");
            relation.Add("R_Wrist_End", "rWrist_end");
        }
        // Start is called before the first frame update
        public void Start()
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
                    int length = split_line.Length;
                    name = split_line[length - 1];
                    if (relation.ContainsKey(name))
                    {
                        name = relation[name];
                    }
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
                            if (line[i] == '\t'||line[i]==' ')
                                len++;
                            else
                                break;
                        }
                        tmp = new JointNode
                        {
                            Level = (line[0] == ' ')?len/4:len,
                            Number = head.Number + 1,
                            Order = head.Order + 1,
                            Name = name
                        };
                        if (tmp.Name.Contains("HandThumb") || tmp.Name.Contains("Neck"))
                            tmp.Order -= 1;
                        while (t_stack.Peek().Level >= tmp.Level)
                        {
                            t_stack.Pop();
                        }
                        head = t_stack.Peek();
                        head.NChildren++;
                        head.Children.Add(tmp);
                        head = tmp;
                    }
                    joints.Add(name);
                    t_stack.Push(head);
                }
                else if (line.Contains("End Site"))
                {
                    End = new JointNode
                    {
                        Number = head.Number,//不增加 节点 因为读的时候没有数据
                        Order = head.Order + 1,
                        Level = head.Level + 1
                    };
                    head.NChildren++;
                    if (head.Name.Contains("HandThumb"))
                        End.Order -= 1;
                    head.Children.Add(End);
                    End.Name = head.Name + "_end";
                    joints.Add(End.Name);
                    head = End;
                    t_stack.Push(head);
                }
                else if (line.Contains("{"))
                {
                    continue;
                }
                else if (line.Contains("}"))
                {
                    continue;
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
                }
                else if (line.Contains("Frame Time"))
                {
                    break;
                }
                else if (line.Contains("Frames:"))
                {
                    frame_num = int.Parse(split_line[split_line.Length - 1]);
                }
            }
            while (!bvh_file.EndOfStream)
            {
                string line = bvh_file.ReadLine();
                string str = new System.Text.RegularExpressions.Regex("[\\s]+").Replace(line, " ");
                string[] split_line = str.Split(' ');
                Vector3 tp = Vector3.zero;
                for (int i = 1; i < 4; i++)//对head.global_pos进行一个赋值
                {
                    tp[i - 1] = float.Parse(split_line[i]);
                }
                if (bvh_fname.Contains("00"))
                {
                    BVHtree.Root.Global_p = tp / 35.0f;
                }
                else
                {
                    BVHtree.Root.Global_p = tp;
                }
                BVHtree.Dfs_write(BVHtree.Root, split_line);
                for (int i = 0; i < BVHtree.Root.NChildren; i++)
                {
                    BVHtree.Dfs_calc(BVHtree.Root.Children[i], BVHtree.Root.Global_p, BVHtree.Root.Orientation);
                }
                Tree.Add(new JointTree
                {
                    Root = JointNode.DeepCopyByReflect(BVHtree.Root)
                });
                // ! 在以上空白写下你的代码
            }
            bvh_file.Dispose();
        }
    }
}
