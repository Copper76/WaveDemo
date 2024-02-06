using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(Rigidbody))]
public class Float : MonoBehaviour
{
    public float airDrag;
    public float waterDrag;
    public Transform[] floatPoints;
    public bool affectDirection;
    public bool attachToSurface;

    private Rigidbody rb;
    private WavesController wavesController;

    private float waterLine;
    private Vector3[] waterLinePoints;

    private Vector3 centreOffset;
    private Vector3 smoothVectorRotation;
    private Vector3 targetUp;
    public Vector3 centre { get { return transform.position + centreOffset; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        wavesController = FindObjectOfType<WavesController>();

        waterLinePoints = new Vector3[floatPoints.Length];
        for (int i=0;i<floatPoints.Length;i++)
        {
            waterLinePoints[i] = floatPoints[i].position;
        }
        centreOffset = GetCentre(waterLinePoints) - transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float newWaterLine = 0.0f;
        bool pointUnderWater = false;

        for (int i=0;i<floatPoints.Length;i++)
        {
            waterLinePoints[i] = floatPoints[i].position;
            waterLinePoints[i].y = wavesController.GetHeight(floatPoints[i].position);
            newWaterLine += waterLinePoints[i].y;
            if (waterLinePoints[i].y > floatPoints[i].position.y)
            {
                pointUnderWater = true;
            }
        }
        newWaterLine /= floatPoints.Length;
        float waterLineDelta = newWaterLine - waterLine;
        waterLine = newWaterLine;

        targetUp = GetNormal(waterLinePoints);

        Vector3 gravity = Physics.gravity;
        rb.drag = airDrag;
        if (waterLine > centre.y)
        {
            rb.drag = waterDrag;
            if (attachToSurface)
            {
                rb.position = new Vector3(rb.position.x, waterLine - centreOffset.y, rb.position.z);
            }
            else
            {
                gravity = affectDirection ? targetUp * -Physics.gravity.y : -Physics.gravity;
                //gravity = -Physics.gravity;
                transform.Translate(Vector3.up * waterLineDelta * 0.9f);
            }
        }
        rb.AddForce(gravity * Mathf.Clamp(Mathf.Abs(waterLine - centre.y), 0.0f, 1.0f));

        if (pointUnderWater)
        {
            //attach to water surface
            targetUp = Vector3.SmoothDamp(transform.up, targetUp, ref smoothVectorRotation, 0.2f);
            rb.rotation = Quaternion.FromToRotation(transform.up, targetUp) * rb.rotation;
        }

        if (targetUp != Vector3.up)
        {
            Vector3 pushDir = targetUp;
            pushDir.y = 0.0f;
            rb.AddForce(pushDir * 10.0f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (floatPoints == null)
            return;

        for (int i = 0; i < floatPoints.Length; i++)
        {
            if (floatPoints[i] == null)
                continue;

            if (wavesController != null)
            {

                //draw cube
                Gizmos.color = Color.red;
                Gizmos.DrawCube(waterLinePoints[i], Vector3.one * 0.3f);
            }

            //draw sphere
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(floatPoints[i].position, 0.1f);

        }

        //draw center
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(new Vector3(centre.x, waterLine, centre.z), Vector3.one * 1f);
        }
    }

    public static Vector3 GetCentre(Vector3[] points)
    {
        var center = Vector3.zero;
        for (int i = 0; i < points.Length; i++)
            center += points[i];
        return center / points.Length;
    }

    public static Vector3 GetNormal(Vector3[] points)
    {
        //https://www.ilikebigbits.com/2015_03_04_plane_from_points.html
        if (points.Length < 3)
            return Vector3.up;

        var center = GetCentre(points);

        float xx = 0f, xy = 0f, xz = 0f, yy = 0f, yz = 0f, zz = 0f;

        for (int i = 0; i < points.Length; i++)
        {
            var r = points[i] - center;
            xx += r.x * r.x;
            xy += r.x * r.y;
            xz += r.x * r.z;
            yy += r.y * r.y;
            yz += r.y * r.z;
            zz += r.z * r.z;
        }

        var det_x = yy * zz - yz * yz;
        var det_y = xx * zz - xz * xz;
        var det_z = xx * yy - xy * xy;

        if (det_x > det_y && det_x > det_z)
            return new Vector3(det_x, xz * yz - xy * zz, xy * yz - xz * yy).normalized;
        if (det_y > det_z)
            return new Vector3(xz * yz - xy * zz, det_y, xy * xz - yz * xx).normalized;
        else
            return new Vector3(xy * yz - xz * yy, xy * xz - yz * xx, det_z).normalized;

    }
}
