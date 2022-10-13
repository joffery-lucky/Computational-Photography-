// 1900017872 乔思琦 hw1


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class interpolation : MonoBehaviour
{
    private GameObject character0, character1;
    private GameObject keyframe0, keyframe1, keyframe2, keyframe3;
    private int current_time = 0;
    private float[,,] data;
   
    // Start is called before the first frame update
    void Start()
    {
        // find all game objects
        character0 = GameObject.Find("Character0");
        character1 = GameObject.Find("Character1");
        keyframe0 = GameObject.Find("Keyframe0");
        keyframe1 = GameObject.Find("Keyframe1");
        keyframe2 = GameObject.Find("Keyframe2");
        keyframe3 = GameObject.Find("Keyframe3");

        // random initialize position of keyframe objects
        keyframe0.transform.position = new Vector3(Random.Range(5.0f, 10.0f), Random.Range(-5.0f, 5.0f), Random.Range(5.0f, 10.0f));
        keyframe1.transform.position = new Vector3(Random.Range(-10.0f, -5.0f), Random.Range(-5.0f, 5.0f), Random.Range(5.0f, 10.0f));
        keyframe2.transform.position = new Vector3(Random.Range(-10.0f, -5.0f), Random.Range(-5.0f, 5.0f), Random.Range(-10.0f, -5.0f));
        keyframe3.transform.position = new Vector3(Random.Range(5.0f, 10.0f), Random.Range(-5.0f, 5.0f), Random.Range(-10.0f, -5.0f));

        // random initialize rotation of keyframe objects
        keyframe0.transform.rotation = Random.rotation;
        keyframe1.transform.rotation = Random.rotation;
        keyframe2.transform.rotation = Random.rotation;
        keyframe3.transform.rotation = Random.rotation;

        // move characters to keyframe0
        character0.transform.position = keyframe0.transform.position;
        character0.transform.rotation = keyframe0.transform.rotation;
        character1.transform.position = keyframe0.transform.position;
        character1.transform.rotation = keyframe0.transform.rotation;

        // set current time
        current_time = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // 用插值的方式获得每个时刻两个character的位置和旋转
        // 以4个keyframe object为关键帧设计插值函数，完成positionUpdate0， rotationUpdate0, positionUpdate1, rotationUpdate1四个函数的代码填空
        // 使得current_time=0, 500, 1000, 1500时character1和character2的位置和旋转与keyframe0, keyframe1, keyframe2, keyframe3重合
        // 完成后的效果可以参考作业附件中的example.mp4

        // 根据插值结果更新character0的位置和旋转
        character0.transform.position = positionUpdate0(current_time);
        character0.transform.rotation = rotationUpdate0(current_time);

        // 根据插值结果更新character1的位置和旋转
        character1.transform.position = positionUpdate1(current_time);
        character1.transform.rotation = rotationUpdate1(current_time);

        // update time
        current_time += 1;
        if (current_time % 2000 == 0)
            current_time = 0;
            
    }

    Vector3 positionUpdate0(int t)
    {
        Vector3 current_pos = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3[] keyframePos = new Vector3[4];
        keyframePos[0] = keyframe0.transform.position;
        keyframePos[1] = keyframe1.transform.position;
        keyframePos[2] = keyframe2.transform.position;
        keyframePos[3] = keyframe3.transform.position;
        // ------------- 代码填空 -------------
        // 实现position的线性插值
        // 当t=0, 500, 1000, 1500时返回的current_pos与keyframePos[0, 1, 2, 3]重合，其余时刻的current_pos通过插值计算
        int ctime = t % 2000;
        int before = (ctime / 500) % 4;
        int next = (ctime % 500) == 0 ? before : (before + 1) % 4;
        for (int i = 0; i < 3; i++)
        {
            current_pos[i] = keyframePos[before][i] + (keyframePos[next][i] - keyframePos[before][i]) * ((ctime / 500.0f) - before);
        }
        // 返回current_pos为character在时刻t的位置
        return current_pos;
    }
   
    Vector3 positionUpdate1(int t)
    {
        Vector3 current_pos = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3[] keyframePos = new Vector3[4];
        keyframePos[0] = keyframe0.transform.position;
        keyframePos[1] = keyframe1.transform.position;
        keyframePos[2] = keyframe2.transform.position;
        keyframePos[3] = keyframe3.transform.position;

        // ------------- 代码填空 -------------
        // 自己选择一种非线性插值方式进行实现
        //polynomial,采取五个点完成插值
        int ctime = t % 2000;
        float[] x = new float[5] { 0f, 500f, 1000f, 1500f ,2000f};
        for (int k = 0; k < 3; k++) {
            for (int i = 0; i < 5; i++)
            {
                float f = 1.0f;
                for (int j = 0; j < 5; j++)
                {
                    if (j == i)
                        continue;
                    f *= (ctime - x[j])/(x[i] - x[j]);
                }
                current_pos[k] += f * keyframePos[i%4][k];
            } 
        }
        // 返回current_pos为character在时刻t的位置
        return current_pos;
    }

    Quaternion rotationUpdate0(int t)
    {
        Quaternion current_rot = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
        Quaternion[] keyframeRot = new Quaternion[4];
        keyframeRot[0] = keyframe0.transform.rotation;
        keyframeRot[1] = keyframe1.transform.rotation;
        keyframeRot[2] = keyframe2.transform.rotation;
        keyframeRot[3] = keyframe3.transform.rotation;
        // ------------- 代码填空 -------------
        // 实现四元数的slerp插值
        // 当t = 0, 500, 1000, 1500时返回的current_rot与keyframeRot[0, 1, 2, 3]重合，其余时刻的current_rot通过插值计算
        // 不要使用Unity API Quaternion.Slerp!
        int ctime = t % 2000;
        int before = (ctime / 500) % 4;
        int next = (ctime % 500) == 0 ? before : (before + 1) % 4;
        float cosa=Quaternion.Dot(Quaternion.Normalize(keyframeRot[before]), Quaternion.Normalize(keyframeRot[next]));
        cosa = cosa > 1 ? 1 : cosa;//精度原因可能超出范围
        float angle = Mathf.Acos(cosa);
        if (angle > Mathf.PI / 2)//当夹角超过九十度的时候会绕路插值
        {
            /*Quaternion quaternion = Quaternion.Negate(keyframeRot[next]);
            keyframeRot[next] = quaternion;*/ //版本使用不了
            for(int i = 0; i < 4; i++)
            {
                keyframeRot[next][i] = -keyframeRot[next][i];
            }
            angle -= Mathf.PI / 2;
        }
        if (angle < Mathf.PI / 36)//当夹角过于小的时候 进行线性插值
        {
            for (int i = 0; i < 4; i++)
            {
                current_rot[i] = (1 - (ctime / 500.0f - before)) * keyframeRot[before][i] +
                    (ctime / 500.0f - before) * keyframeRot[next][i];
            }
        }
        else//slerp插值
        {
            for (int i = 0; i < 4; i++)
            {
                current_rot[i] = (Mathf.Sin((1 - (ctime / 500.0f - before)) * angle) / Mathf.Sin(angle)) * keyframeRot[before][i] +
                    (Mathf.Sin((ctime / 500.0f - before) * angle) / Mathf.Sin(angle)) * keyframeRot[next][i];
            }
        }
        /*验证
        if (t % 500 == 0 && (!current_rot.Equals(keyframeRot[before])))
        {
            print("cosa is" + cosa);
            print("angle is" + angle);
            print("current is" + current_rot);
            print("before is " + keyframeRot[before]);
        }*/
        // 返回current_rot为character在时刻t的旋转（四元数表示）
        return current_rot;
    }

    Quaternion rotationUpdate1(int t)
    {
        Quaternion current_rot = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
        Quaternion[] keyframeRot = new Quaternion[4];
        keyframeRot[0] = keyframe0.transform.rotation;
        keyframeRot[1] = keyframe1.transform.rotation;
        keyframeRot[2] = keyframe2.transform.rotation;
        keyframeRot[3] = keyframe3.transform.rotation;
        // ------------- 代码填空 -------------
        // 用旋转的另一种表示方式进行线性或非线性插值
        // hint:
        // 对于Quaternion类的对象q，q.eulerAngles是这个旋转的欧拉角表示，q.ToAngleAxis将四元数转换为轴角表示，q.AngleAxis通过指定角度和转轴获得对应的四元数
        // 更详细的说明可以参考Unity Document关于Quaternion的说明 https://docs.unity3d.com/ScriptReference/Quaternion.html
        int ctime = t % 2000;
        int before = (ctime / 500) % 4;
        int next = (ctime % 500) == 0 ? before : (before + 1) % 4;
        Vector3 axis1 = Vector3.zero;
        Vector3 axis2 = Vector3.zero;
        //使用欧拉角插值
        Vector3 current_angle = Vector3.zero;
        axis1 = keyframeRot[before].eulerAngles;
        axis2 = keyframeRot[next].eulerAngles;
        for (int i = 0; i < 3; i++)
        {
            current_angle[i] = axis1[i]+ (axis2[i] - axis1[i]) * ((ctime / 500.0f) - before);
        }
         current_rot = Quaternion.Euler(current_angle);
        //使用轴角法插值
        /*float ans1 = 0.0f;
        float ans2 = 0.0f;
        keyframeRot[before].ToAngleAxis(out ans1, out axis1);
        keyframeRot[next].ToAngleAxis(out ans2, out axis2);
        print("ans1 is" + ans1 * (Mathf.PI / 180) + "axis1 is" + axis1);
        float angle = (ans1 + (ans2 - ans1) * ((ctime / 500.0f) - before))*(Mathf.PI / 180);
        for (int i = 0; i < 3; i++)
        {
            current_rot[i+1] = (axis1[i] + (axis2[i] - axis1[i]) * ((ctime / 500.0f) - before))*Mathf.Sin(angle/2);
        }
        current_rot[0] = Mathf.Cos(angle/ 2);*/
        // 返回current_rot为character0在时刻t的旋转（四元数表示）
        return current_rot;
    }
}
