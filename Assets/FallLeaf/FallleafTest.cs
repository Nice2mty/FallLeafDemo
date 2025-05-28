using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallleafTest : MonoBehaviour
{
    [Serializable]
    struct Area
    {
        public float minX, maxX;
        public float minY, maxY;
        public float Width => Mathf.Abs(maxX - minX);
        public float Height => Mathf.Abs(maxY - minY);

        public bool Contains(Vector3 pos)
        {
            if (pos.x < minX) return false;
            if (pos.x > maxX) return false;
            if (pos.y < minY) return false;
            if (pos.y > maxY) return false;
            return true;
        }
    }
    
    struct TouchPoint
    {
        public Vector2 pos, vec;
    }

    [SerializeField]
    private Transform cameraPivot;
    
    [SerializeField]
    private Transform maskSphere;

    [SerializeField]
    private RenderTexture bakeTarget;

    [SerializeField]
    private ComputeShader _shader;

    private ComputeBuffer _buffer;
    
    [SerializeField]
    private Area area;
    
    [SerializeField]
    private Transform[] points;
    
    private Vector3[] records;

    private List<TouchPoint> tps = new();
    
    int gs = (int)(512f / 8f);
    private int kid = 0;
    
    private void Start()
    {
        records = new Vector3[points.Length];
        for (int i = 0; i < records.Length ; i++)
        {
            records[i] =  points[i].position;
        }
        
        _buffer = new ComputeBuffer(10, 16);
        _shader.SetTexture(kid, "Result", bakeTarget);
        _shader.SetBuffer(kid, "Points", _buffer);
    }

    void Update()
    {
        cameraPivot.Rotate(0, Time.deltaTime * 10f, 0);
        maskSphere.position = Vector3.back * 1000;
        if (Input.GetMouseButton(0))
        {
            var mp = Input.mousePosition;
            mp.z = 1f;
            var ray = Camera.main.ScreenPointToRay(mp);
            RaycastHit info;
            if (Physics.Raycast(ray, out info, 100))
            {
                maskSphere.position = info.point;
            }
        }
        
        tps.Clear();
        for (int i = 0; i < points.Length ; i++)
        {
            if (points[i].gameObject.activeSelf && area.Contains(points[i].position) )
            {
                tps.Add( new TouchPoint()
                {
                    pos = CalculateUV(points[i].position),
                    vec = CalculateUV(points[i].position) - CalculateUV(records[i])
                });
            }
            records[i] = points[i].position;
        }
        _buffer.SetData(tps.ToArray());
        _shader.SetInt("PointCounts", tps.Count);
        _shader.Dispatch(kid, gs, gs, 1);
    }

    Vector2 CalculateUV( Vector3 pos )
    {
        var v2 = Vector2.zero;
        v2.x = Mathf.Clamp01((pos.x - area.minX)/area.Width);
        v2.y = Mathf.Clamp01((pos.z - area.minY)/area.Height);
        return v2;
    }

    private void OnDestroy() => _buffer.Release();
}
