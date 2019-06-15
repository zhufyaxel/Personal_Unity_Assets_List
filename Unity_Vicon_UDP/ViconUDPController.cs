using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Net;
using System.Text;


public class ViconInfo
{
    public string Name;
    public double[] Translation;
    public double[] Rotation;

    public Transform targetTransform;

    public ViconInfo(Transform target)
    {
        Name = target.gameObject.name;
        targetTransform = target;
        //Translation = 
    }
}

public class ViconUDPController : MonoBehaviour
{
    public int port = 9003;
    public UdpClient client;

    public List<ViconInfo> viconInfos;
    public List<Transform> trackedObjects;

    private bool isDestroyed;

    private void Awake()
    {
        viconInfos = new List<ViconInfo>();
        foreach (Transform t in trackedObjects)
        {
            var newViconInfo = new ViconInfo(t);
            viconInfos.Add(newViconInfo);
        }
    }

    private void Start()
    {
        client = new UdpClient(port);
        try
        {
            client.BeginReceive(new AsyncCallback(udpReceive), null);
            Debug.Log("UDP begin");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start UDP receiver.");
        }
    }

    private void udpReceive(IAsyncResult res)
    {
        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, port);
        byte[] received = client.EndReceive(res, ref RemoteIpEndPoint);

        string str = Encoding.UTF8.GetString(received);
        ViconInfo commingVicon = JsonUtility.FromJson<ViconInfo>(str);
        //Vector3 newPos = new Vector3(-(float)commingVicon.Translation[0], (float)commingVicon.Translation[1], (float)commingVicon.Translation[2]);
        foreach (ViconInfo v in viconInfos)
        {
            if (v.Name == commingVicon.Name)
            {
                v.Translation = commingVicon.Translation;
                v.Rotation = commingVicon.Rotation;
            }
        }

        if (!isDestroyed)
        {
            client.BeginReceive(new AsyncCallback(udpReceive), null);
        }
    }

    private void Update()
    {
        foreach (ViconInfo v in viconInfos)
        {
            Vector3 newPos = GetPosFromViconInfo(v);
            if (newPos != new Vector3(0, 0, 0))// only get tracking when it is in.
            {
                v.targetTransform.position = GetPosFromViconInfo(v);
                v.targetTransform.rotation = GetRotFromViconInfo(v);
            }
           
        }
    }

    private void OnDestroy()
    {
        client.Close();
        isDestroyed = true;
    }

    public Vector3 GetPosFromViconInfo(ViconInfo v)
    {
        Vector3 newPos = new Vector3(0, 0, 0);
        if (v.Translation != null)
        {
            newPos = new Vector3(-(float)v.Translation[0], (float)v.Translation[2], -(float)v.Translation[1]) * 0.001f;
        }
        return newPos;
    }

    public Quaternion GetRotFromViconInfo(ViconInfo v)
    {
        var rotationMatrix = new Matrix4x4();
        
        int matrixIndex = 0;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                rotationMatrix[i, j] = (float)v.Rotation[matrixIndex];
                matrixIndex = matrixIndex + 1;
                rotationMatrix[3, j] = 0;
            }
            rotationMatrix[i, 3] = 0;
        }
        rotationMatrix[3, 3] = 1;

        Quaternion _q = rotationMatrix.rotation;
        Quaternion q = new Quaternion(_q.x, -_q.z, _q.y, _q.w);
        //Vector3 euler = rotationMatrix.rotation.eulerAngles;
        //Vector3 newEuler = new Vector3(euler.x, -euler.z, euler.y);
        //Quaternion q = Quaternion.Euler(newEuler);
        return q;
    }

}
