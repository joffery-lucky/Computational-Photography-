using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using System.Threading;
using System.Net;
using System.Net.Sockets;
public class globalValue
{
    public static Socket SocketValue;
    public static string animPath;
    public static SynchronizationContext synValue;
    public static List<String> fileNames;
}

public class StatTemplateStateDto
{
    /// <summary>
    /// 映射标识
    /// </summary>
    public virtual String[] files { get; set; }

}

public class animcraftMenu : Editor
{
    [MenuItem("Animcraft/Connect Animcraft", false, 40)]
    public static void AccReady()
    {
        SynchronizationContext mainThreadSynContext;
        mainThreadSynContext = SynchronizationContext.Current;
        globalValue.synValue = mainThreadSynContext;
        string host = "127.0.0.1";
        int port = 10086;
        byte[] messTmp;
        messTmp = new byte[1024];
        Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        globalValue.SocketValue = client;
        try
        {
            //client.IOControl(IOControlCode.KeepAliveValues, BitConverter.GetBytes(120), null);
            client.Connect(new IPEndPoint(IPAddress.Parse(host), port));
            var count = client.Receive(messTmp);
            
            string str = System.Text.Encoding.Default.GetString(messTmp);
            Debug.Log(str);
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(Application.dataPath);
            client.Send(byteArray);
            EditorUtility.DisplayDialog("Successful", "Connect Animcraft successful", "Okay");
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            return;
        }

        Thread threadAccept = new Thread(MyAccept);
        threadAccept.IsBackground = true;
        threadAccept.Start();
    }
    public static void MyAccept(object obj)
    {
        byte[] messTmp = new byte[1024];
        Socket client = globalValue.SocketValue;

        while (true)
        {
            var count = client.Receive(messTmp);
            if (count>0)
            {
                string str = System.Text.Encoding.UTF8.GetString(messTmp,0,count);
                string resulatStr = str.Replace("'", "");
                globalValue.animPath = resulatStr;
                SynchronizationContext mainThreadSynContext;
                mainThreadSynContext = globalValue.synValue;
                if (resulatStr == "importFbx")
                {
                    Debug.Log("start import fbx...");
                    mainThreadSynContext.Post(new SendOrPostCallback(AccExportFbx), null);
                }
                else if (resulatStr == "close")
                {
                    mainThreadSynContext.Post(new SendOrPostCallback(AccCloseEvent), null);
                }
                else if (resulatStr == "update")
                {
                    mainThreadSynContext.Post(new SendOrPostCallback(AccUpdate), null);
                }
                else if (resulatStr.Contains("update_instantiate"))
                {
                    resulatStr = resulatStr.Replace("update_instantiate", "");
                    List<string> files = new List<string>(resulatStr.Split(new string[] { "_ac_" }, StringSplitOptions.None));
                    globalValue.fileNames = files;
                    mainThreadSynContext.Post(new SendOrPostCallback(AccUpdateAndInstantiate), null);
                }
                else
                {
                    mainThreadSynContext.Post(new SendOrPostCallback(AccImportFbx), null);
                }
            }
        }
    }

    private static void AccUpdate(object state)
    {
        AssetDatabase.Refresh();
        Socket client = globalValue.SocketValue;
        byte[] byteArray = System.Text.Encoding.Default.GetBytes("Success");
        client.Send(byteArray);
    }

    private static void AccUpdateAndInstantiate(object state)
    {
        AssetDatabase.Refresh();
        Socket client = globalValue.SocketValue;
        foreach (String file in globalValue.fileNames)
        {
           
            GameObject GO = AssetDatabase.LoadAssetAtPath("Assets/" + file, typeof(GameObject)) as GameObject;
            Instantiate(GO);
        }
        byte[] byteArray = System.Text.Encoding.Default.GetBytes("Success");
        client.Send(byteArray);
        
    }
    private static void AccCloseEvent(object state)
    {
        EditorUtility.DisplayDialog("Connect Close", "Animcraft closed", "Okay");
    }

    private static void AccExportFbx(object state)
    {
        Socket client = globalValue.SocketValue;
        string fbx = AccGetFbxPath();
        if (fbx == null)
        {
            byte[] byteArray = System.Text.Encoding.Default.GetBytes("None");
            client.Send(byteArray);
        }
        else
        {
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(fbx);
            client.Send(byteArray);
        }
    }

    [MenuItem("Animcraft/Export Fbx", false, 41)]
    public static string AccGetFbxPath()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            EditorUtility.DisplayDialog("Selection Error", "Please select any Character", "Okay");
            return null;
        }
        SkinnedMeshRenderer[] mfs = Selection.activeGameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (mfs.Length != 0)
        {
            string[] fbxPathList = new string[] { };
            List<string> strList = new List<string>(fbxPathList);
            for (int i = 0, count = mfs.Length; i < count; i++)
            {
                Mesh mesh = mfs[i].sharedMesh;
                string fbxPath = AssetDatabase.GetAssetPath(mesh);
                bool exists = strList.Contains(fbxPath);
                if (exists == false)
                {
                    strList.Add(fbxPath);
                }
            }
            string[] strArray = strList.ToArray();
            if (strArray.Length == 0)
            {
                EditorUtility.DisplayDialog("Can't find fbx", "Can't find the source fbx file", "Okay");
                return null;
            }
            else if (strArray.Length > 1)
            {
                EditorUtility.DisplayDialog("Selection Error", "More than one FBX in your selected game object, try to select its child object.", "Okay");
                return null;
            }
            else
            {
                string[] sArray = strArray[0].Split(new char[1] { '/'});
                string resultPath = "";
                for (int i = 1, count = sArray.Length; i < count; i++)
                {
                    if (i != sArray.Length - 1)
                        resultPath += sArray[i] + "/";
                    else
                        resultPath += sArray[i];
                }
                Debug.Log(Application.dataPath + "/" + resultPath);
                return (Application.dataPath+"/"+resultPath);
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Can't find fbx", "This gameobject have no skinned mesh", "Okay");
            return null;
        }
        return null;
    }

    //[MenuItem("Animcraft/Import Anim", false, 42)]
    public static void AccImportFbx(object state)
    {
        Socket client = globalValue.SocketValue;
        string importFbx = globalValue.animPath;
        string fbxFile = AccGetFbxPath();
        Avatar fbxAvatar = AccGetAvatar();
        if (fbxFile != null)
        {

            string[] sArray = fbxFile.Split(new char[1] { '/' });
            string[] sArray3 = Application.dataPath.Split(new char[1] { '/' });
            string fbxPath = "";
            string gameObjectPath = "";

            for (int i = 0, count = sArray.Length - 1; i < count; i++)
            {
                fbxPath += sArray[i] + "/";
                if (i > sArray3.Length - 2)
                    gameObjectPath += sArray[i] + "/";
            }

            string[] sArray2 = importFbx.Split(new char[1] { '/' });
            string fbxName = sArray2[sArray2.Length - 1];
            Debug.Log(fbxPath);
            Debug.Log(fbxName);
            Debug.Log(gameObjectPath);
            
            string targetFbx = fbxPath + fbxName;
            if (System.IO.File.Exists(targetFbx))
            {
                System.IO.File.Delete(targetFbx);
            }
            System.IO.File.Copy(importFbx, targetFbx);



            AssetDatabase.ImportAsset(targetFbx);
            AssetDatabase.Refresh();
            ModelImporter modelimporter = ModelImporter.GetAtPath(gameObjectPath + fbxName) as ModelImporter;

            //modelimporter.importVisibility = false;
            //modelimporter.importCameras = false;
            //modelimporter.importLights = false;

            modelimporter.animationType = ModelImporterAnimationType.Human;
            modelimporter.sourceAvatar = fbxAvatar;
            AssetDatabase.ImportAsset(targetFbx, ImportAssetOptions.ImportRecursive);
            AssetDatabase.ImportAsset(targetFbx, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            byte[] byteArray = System.Text.Encoding.Default.GetBytes("True");
            client.Send(byteArray);
        }
        else
        {
            byte[] byteArray = System.Text.Encoding.Default.GetBytes("False");
            client.Send(byteArray);
        }
    }
    public static Avatar AccGetAvatar()
    {
        GameObject obj = Selection.activeGameObject;
        Animator[] animators = Selection.activeGameObject.GetComponentsInChildren<Animator>();
        if (animators.Length == 0)
            return null;
        else
            return animators[0].avatar;
    }

}
