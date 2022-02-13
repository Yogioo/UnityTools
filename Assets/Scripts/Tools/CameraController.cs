/*
** Author      : Yogi
** CreateDate  : 6/20/2019 9:03:18 AM
** Description : Camera Look At Target Ang Rotate By Input
*/

using System;
using UnityEngine;

//移动端和PC端的绕点摄像机控制器
public class CameraController : MonoBehaviour
{
    // 是否允许旋转
    [Header("是否允许旋转轴")] public bool canRotation_X = true;


    public bool canRotation_Y = true;


    [Header("是否开启缩放")] public bool canScale = true;


    #region 字段和属性


    /// <summary>
    /// 旋转中心
    /// </summary>
    [Header("旋转中心物体Transform")] public Transform target;


    /// <summary>
    /// 关于鼠标的设置 Settings of mouse button, pointer and scrollwheel.
    /// </summary>
    [Header("鼠标设置,鼠标id,灵敏度,滚轮强度")] public MouseSettings mouseSettings = new MouseSettings(0, 10, 10);


    /// <summary>
    /// 旋转角度限制
    /// </summary>
    [Header("旋转角限制")] public Range angleRange = new Range(-90, 90);


    /// <summary>
    /// 距离的限制
    /// </summary>
    [Header("缩放距离限制")] public Range distanceRange = new Range(1, 10);


    /// <summary>
    /// Damper for move and rotate.
    /// </summary>
    [Header("移动和旋转的阻力")] [Range(0, 100)] public float damper = 5;


    /// <summary>
    /// 角度,摄像机
    /// </summary>
    public Vector2 CurrentAngles { protected set; get; }


    /// <summary>
    /// 当前距离,从摄像机到物体
    /// </summary>
    public float CurrentDistance { protected set; get; }


    /// <summary>
    /// 摄像机当前角度
    /// </summary>
    protected Vector2 targetAngles;


    /// <summary>
    /// 目标距离,从摄像机到目标,Target distance from camera to target.
    /// </summary>
    protected float targetDistance;


    #endregion


    #region Protected Method


    protected virtual void Start()
    {
        //初始化当前角度目标角度和当前距离目标距离
        CurrentAngles = targetAngles = transform.eulerAngles;
        CurrentDistance = targetDistance = Vector3.Distance(transform.position, target.position);


        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                mouseSettings.wheelSensitivity = 0.1f;
                break;
            case RuntimePlatform.WindowsPlayer:
                mouseSettings.mouseButtonID = 1;
                mouseSettings.wheelSensitivity = 5f;
                mouseSettings.pointerSensitivity = 10f;
                damper = 5f;
                break;
            case RuntimePlatform.WindowsEditor:
                mouseSettings.mouseButtonID = 1;
                mouseSettings.wheelSensitivity = 5f;
                mouseSettings.pointerSensitivity = 10f;
                damper = 5;
                break;
        }
    }


    /// <summary>
    /// 初始化距离
    /// </summary>
    /// <param name="diatance"></param>
    public void InitalScale(float diatance)
    {
        targetDistance = diatance;
    }


    protected virtual void Update()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            AroundByMobileInput();
        }
        else if (Application.platform == RuntimePlatform.WindowsEditor ||
                 Application.platform == RuntimePlatform.WindowsPlayer)
        {
            AroundByMouseInput();
        }
    }


    //记录上一次手机触摸位置判断用户是在左放大还是缩小手势  
    private Vector2 oldPosition1;


    private Vector2 oldPosition2;


    //是否是单指
    private bool m_IsSingleFinger;


    /// <summary>
    /// 安卓控制
    /// </summary>
    protected void AroundByMobileInput()
    {
        //如果是单指
        if (Input.touchCount == 1)
        {
            //如果是按下并且拖拽
            if (Input.touches[0].phase == TouchPhase.Moved)
            {
                //改变角度
                targetAngles.y += Input.GetAxis("Mouse X") * mouseSettings.pointerSensitivity;
                targetAngles.x -= Input.GetAxis("Mouse Y") * mouseSettings.pointerSensitivity;


                //限定x角度 Range.
                targetAngles.x = Mathf.Clamp(targetAngles.x, angleRange.min, angleRange.max);
            }


            //是单指 Mouse pointer.
            m_IsSingleFinger = true;
        }


        //缩放 Mouse scrollwheel.
        if (canScale)
        {
            //是否两指
            if (Input.touchCount > 1)
            {
                //计算出当前两点触摸点的位置  
                if (m_IsSingleFinger)
                {
                    oldPosition1 = Input.GetTouch(0).position;
                    oldPosition2 = Input.GetTouch(1).position;
                }


                //如果双指都在移动
                if (Input.touches[0].phase == TouchPhase.Moved && Input.touches[1].phase == TouchPhase.Moved)
                {
                    //暂存移动后的手指位置
                    var tempPosition1 = Input.GetTouch(0).position;
                    var tempPosition2 = Input.GetTouch(1).position;


                    //当前双指距离
                    float currentTouchDistance = Vector3.Distance(tempPosition1, tempPosition2);
                    //上次双指距离
                    float lastTouchDistance = Vector3.Distance(oldPosition1, oldPosition2);


                    //计算上次和这次双指触摸之间的距离差距  
                    //然后去更改摄像机的距离  
                    targetDistance -= (currentTouchDistance - lastTouchDistance) * Time.deltaTime *
                                      mouseSettings.wheelSensitivity;
                    Debug.Log(targetDistance);


                    //备份上一次触摸点的位置，用于对比  
                    oldPosition1 = tempPosition1;
                    oldPosition2 = tempPosition2;
                    //不是单指
                    m_IsSingleFinger = false;
                }
            }
        }


        //把距离限制住在min和max之间  
        targetDistance = Mathf.Clamp(targetDistance, distanceRange.min, distanceRange.max);


        //差值运算 Lerp.
        CurrentAngles = Vector2.Lerp(CurrentAngles, targetAngles, damper * Time.deltaTime);
        CurrentDistance = Mathf.Lerp(CurrentDistance, targetDistance, damper * Time.deltaTime);


        //如果限定了摄像机的话,角度不变
        if (!canRotation_X) targetAngles.y = 0;
        if (!canRotation_Y) targetAngles.x = 0;


        //更新旋转
        transform.rotation = Quaternion.Euler(CurrentAngles);
        //更新位置
        transform.position = target.position - transform.forward * CurrentDistance;
        // transform.position = target.position - Vector3.forward * CurrentDistance;
    }


    /// <summary>
    /// Camera around target by mouse input.
    /// </summary>
    protected void AroundByMouseInput()
    {
        if (Input.GetMouseButton(mouseSettings.mouseButtonID))
        {
            //Mouse pointer.
            targetAngles.y += Input.GetAxis("Mouse X") * mouseSettings.pointerSensitivity;
            targetAngles.x -= Input.GetAxis("Mouse Y") * mouseSettings.pointerSensitivity;


            //Range.
            targetAngles.x = Mathf.Clamp(targetAngles.x, angleRange.min, angleRange.max);
        }


        //Mouse scrollwheel.
        if (canScale)
        {
            targetDistance -= Input.GetAxis("Mouse ScrollWheel") * mouseSettings.wheelSensitivity;
        }
        // m_debugTip.text = Input.GetAxis("Mouse ScrollWheel").ToString() + " + " + targetDistance.ToString();


        targetDistance = Mathf.Clamp(targetDistance, distanceRange.min, distanceRange.max);


        //Lerp.
        CurrentAngles = Vector2.Lerp(CurrentAngles, targetAngles, damper * Time.deltaTime);
        CurrentDistance = Mathf.Lerp(CurrentDistance, targetDistance, damper * Time.deltaTime);


        if (!canRotation_X) targetAngles.y = 0;
        if (!canRotation_Y) targetAngles.x = 0;


        //Update transform position and rotation.
        transform.rotation = Quaternion.Euler(CurrentAngles);


        transform.position = target.position - transform.forward * CurrentDistance;
        // transform.position = target.position - Vector3.forward * CurrentDistance;
    }


    #endregion


    #region MoveToMethod
    private float range = 1.5f;


    /// <summary>
    /// 摄像机看向物体，并且移动靠近， 如果物体不存在mesh 那么就看向最大的物体
    /// </summary>
    /// <param name="target"></param>
    public void Move(Transform target)
    {
        this.target = target;
        transform.LookAt(target);
        transform.localPosition = target.localPosition + new Vector3(0, 0, -10f);
        MeshFilter mf = target.gameObject.GetComponent<MeshFilter>();


        float maxLength = 0;


        if (mf == null)
        {


            foreach (Transform trans in target.GetComponentsInChildren<Transform>())
            {
                if (trans.GetComponent<MeshFilter>())
                {
                    float length = GetMaxLength(trans.GetComponent<MeshFilter>().mesh);
                    if (length > maxLength)
                    {
                        maxLength = length;
                    }
                }
            }
        }
        else
        {
            maxLength = GetMaxLength(mf.mesh);
        }


        GetComponent<CameraController>().InitalScale(maxLength * range);
    }


    /// <summary>
    /// 获得一个模型最大的边的长度
    /// </summary>
    /// <returns></returns>
    private float GetMaxLength(Mesh mesh)
    {
        float maxSize;
        Vector3 meshSize = mesh.bounds.size;


        if (meshSize.x >= meshSize.y)
            maxSize = meshSize.x;
        else
            maxSize = meshSize.y;


        if (maxSize < meshSize.z)
            maxSize = meshSize.z;
        return maxSize;
    }
    #endregion
}


[Serializable]
public struct MouseSettings
{
    /// <summary>
    /// ID of mouse button.
    /// </summary>
    public int mouseButtonID;


    /// <summary>
    /// Sensitivity of mouse pointer.
    /// </summary>
    public float pointerSensitivity;


    /// <summary>
    /// Sensitivity of mouse ScrollWheel.
    /// </summary>
    public float wheelSensitivity;


    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="mouseButtonID">ID of mouse button.</param>
    /// <param name="pointerSensitivity">Sensitivity of mouse pointer.</param>
    /// <param name="wheelSensitivity">Sensitivity of mouse ScrollWheel.</param>
    public MouseSettings(int mouseButtonID, float pointerSensitivity, float wheelSensitivity)
    {
        this.mouseButtonID = mouseButtonID;
        this.pointerSensitivity = pointerSensitivity;
        this.wheelSensitivity = wheelSensitivity;
    }
}


/// <summary>
/// Range form min to max.
/// </summary>
[Serializable]
public struct Range
{
    /// <summary>
    /// Min value of range.
    /// </summary>
    public float min;


    /// <summary>
    /// Max value of range.
    /// </summary>
    public float max;


    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="min">Min value of range.</param>
    /// <param name="max">Max value of range.</param>
    public Range(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
}


/// <summary>
/// Rectangle area on plane.
/// </summary>
[Serializable]
public struct PlaneArea
{
    /// <summary>
    /// Center of area.
    /// </summary>
    public Transform center;


    /// <summary>
    /// Width of area.
    /// </summary>
    public float width;


    /// <summary>
    /// Length of area.
    /// </summary>
    public float length;


    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="center">Center of area.</param>
    /// <param name="width">Width of area.</param>
    /// <param name="length">Length of area.</param>
    public PlaneArea(Transform center, float width, float length)
    {
        this.center = center;
        this.width = width;
        this.length = length;
    }
}


/// <summary>
/// Target of camera align.
/// </summary>
[Serializable]
public struct AlignTarget
{
    /// <summary>
    /// Center of align target.
    /// </summary>
    public Transform center;


    /// <summary>
    /// Angles of align.
    /// </summary>
    public Vector2 angles;


    /// <summary>
    /// Distance from camera to target center.
    /// </summary>
    public float distance;


    /// <summary>
    /// Range limit of angle.
    /// </summary>
    public Range angleRange;


    /// <summary>
    /// Range limit of distance.
    /// </summary>
    public Range distanceRange;


    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="center">Center of align target.</param>
    /// <param name="angles">Angles of align.</param>
    /// <param name="distance">Distance from camera to target center.</param>
    /// <param name="angleRange">Range limit of angle.</param>
    /// <param name="distanceRange">Range limit of distance.</param>
    public AlignTarget(Transform center, Vector2 angles, float distance, Range angleRange, Range distanceRange)
    {
        this.center = center;
        this.angles = angles;
        this.distance = distance;
        this.angleRange = angleRange;
        this.distanceRange = distanceRange;
    }
}