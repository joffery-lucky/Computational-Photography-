using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Spatial.Euclidean;

public class RigidSimulation : MonoBehaviour
{
    public double m_simuStep = 0.001;


    Vector3D m_gravity = new Vector3D(0.0, -9.8, 0.0);

    RJoint[] m_joints;
    RBody[] m_bodies;

    // ! 在这里定义你需要的变量
    private GameObject ground;
    private GameObject articulated;
    // Start is called before the first frame update
    void Start()
    {
        m_joints = GetComponentsInChildren<RJoint>();
        m_bodies = GetComponentsInChildren<RBody>();

        // ! 在这里写下你需要的初始化
        ground = GameObject.Find("Ground");
        articulated = GameObject.Find("Articulated");
        for (int i = 0; i < m_joints.Length; i++)
        {
            if (i % 2 == 0)
                m_joints[i].m_body1 = m_bodies[0];
            else
                m_joints[i].m_body1 = m_bodies[i];
            m_joints[i].m_body2 = m_bodies[i + 1];
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        int simuSteps = (int)(Mathf.Round(Time.deltaTime / (float)m_simuStep));
        Debug.Log("delta time: " + Time.deltaTime + " steps: " + simuSteps);
        for (int i = 0; i < simuSteps; i++)
            UpdateFunc();
    }
    Matrix<double> GetIt(RBody x)
    {
        Matrix4x4 rot = new Matrix4x4();
        var q = x.transform.rotation;
        rot.SetTRS(new Vector3(0, 0, 0), q, new Vector3(1, 1, 1));
        var I = Matrix<double>.Build.Dense(3, 3, 0.0);
        I[0, 0] = x.m_inertia[0];
        I[1, 1] = x.m_inertia[1];
        I[2, 2] = x.m_inertia[2];
        var R = Matrix<double>.Build.Dense(3, 3, (i, j) => rot[i, j]);
        var It = R * I * R.Transpose();
        return It;
    }
    Vector<double> Vector3toVector(Vector3 a)
    {
        var v = Vector<double>.Build.Dense(3);
        v[0] = a.x;
        v[1] = a.y;
        v[2] = a.z;
        return v;
    }
    Vector3 VectortoVector3(Vector<double> b)
    {
        Vector3 v = new Vector3((float)b[0], (float)b[1], (float)b[2]);
        return v;
    }
    Matrix<double> GetCrossMatrix(Vector3 r)
    {
        var rx = Matrix<double>.Build.DenseOfArray(new double[,]
        {
          {0 , -r.z, r.y},
          {r.z, 0 , -r.x},
          {-r.y, r.x, 0 } });
        return rx;
    }
    void UpdateFunc()
    {
        // ! 在这里写下仿真过程
        var M = Matrix<double>.Build;
        var V = Vector<double>.Build;
        var J = M.Sparse(24, 54, 0.0);
        var I = M.SparseIdentity(3);
        //计算Jacobian
        for (int i = 0; i < m_joints.Length; i++)
        {
            //取body的index
            int body1, body2;
            body2 = i + 1;
            body1 = (i % 2 == 0) ? 0 : i;
            var r1 = m_joints[i].transform.position - m_joints[i].m_body1.transform.position;
            var r2 = m_joints[i].transform.position - m_joints[i].m_body1.transform.position;
            J.SetSubMatrix(i * 3, 3, body1 * 6, 3, I);
            J.SetSubMatrix(i * 3, 3, body2 * 6, 3, -I);
            J.SetSubMatrix(i * 3, 3, body1 * 6 + 3, 3, -GetCrossMatrix(r1));
            J.SetSubMatrix(i * 3, 3, body2 * 6 + 3, 3, GetCrossMatrix(r2));
        }
        //计算m、F
        var m= M.Sparse(54, 54, 0.0);
        var v_b = V.Dense(54, 0.0);
        var F = V.Dense(54, 0.0);
        for (int j = 0; j < m_bodies.Length; j++)
        {
            RBody x = m_bodies[j];
            var It = GetIt(x);
            var w = Vector3toVector(x.m_angularVelocity);
            var torque = V.Dense(3, 0.0);
            v_b.SetSubVector(j * 6, 3, Vector3toVector(x.m_linearVelocity));
            v_b.SetSubVector(j * 6 + 3, 3, w);
            F.SetSubVector(j * 6, 3, x.m_mass * m_gravity.ToVector());
            F.SetSubVector(j * 6 + 3, 3, torque - GetCrossMatrix(x.m_angularVelocity) * (It * w));
            m.SetSubMatrix(j * 6, 3, j * 6, 3, x.m_mass * I);
            m.SetSubMatrix(j * 6 + 3, 3, j * 6 + 3, 3, It);
        }
        //计算lambda
        var A = J * m.Inverse() * J.Transpose();
        var ct = -J * m.Inverse() * F - J * v_b / m_simuStep;
        var lambda = A.Inverse() * ct;
        var v = m.Inverse() * F; //+ J.Transpose() * lambda) * m_simuStep + v_b;
        //更新v与w
        for (int j = 0; j < m_bodies.Length; j++)
        {
            RBody x = m_bodies[j];
            x.m_linearVelocity = VectortoVector3(v.SubVector(j * 6, 3));
            x.m_angularVelocity = VectortoVector3(v.SubVector(j * 6 + 3, 3));
            UnityEngine.Quaternion q = new(x.m_angularVelocity.x, x.m_angularVelocity.y, x.m_angularVelocity.z, 0);
            x.transform.position += x.m_linearVelocity * (float)m_simuStep;
            x.transform.rotation *= UnityEngine.Quaternion.Slerp(UnityEngine.Quaternion.identity, q * x.transform.rotation, 0.5f * (float)m_simuStep);
            x.transform.rotation = x.transform.rotation.normalized;
        }
    }
}
