using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

namespace Sunset.SceneManagement
{
    public class SceneTree : MonoBehaviour
    {
        public static SceneTree Instance => _Instance;
        private static SceneTree _Instance;

        [Header("是否随机生成Cube(测试)")]
        public bool IsDebugMode = true;
        [Header("随机种子(测试)")]
        public string Seed;
        [Header("随机位置(测试)")]
        public float randomPos = 100;
        [Header("随机缩放(测试)")]
        public float randomScale = 5;
        [Header("随机的生成数量(测试)")]
        public uint SpawnCount = 50;

        [Header("颗粒度,代表了一个Cell内物体数量")]
        public uint MaxCellCount = 10;
        [Header("颗粒度,代表了一个Cell最小大小")]
        public float MinCell = 20;
        [Header("强制显示距离")]
        public float MinDisplayDistance = 25;
        [Header("是否绘制Gizmos")]
        public bool IsDrawGizmos = false;

        // 主摄像机(用于剔除的)
        private Camera m_Cam;
        // 初始化四叉树是否已完成
        private bool m_IsInitOver = false;
        // 场景中所有需要剔除的物体
        private List<ItemBase> AllItems;
        // 四叉树的Root节点
        private SceneDetectorBase Root;
        // 已经激活显示的节点
        private List<SceneDetectorBase> ActiveTrans;


        private void Awake()
        {
            m_Cam = Camera.main;
            _Instance = this;
            AllItems = new List<ItemBase>();
            ActiveTrans = new List<SceneDetectorBase>();
            m_IsInitOver = false;

            // 开启测试模式后 会生成随机数量的Cube 作为剔除测试
            if (IsDebugMode)
            {
                Random.InitState(Seed.GetHashCode());

                for (int i = 0; i < SpawnCount; i++)
                {
                    var cubeTrans = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    var randomPosition = Random.insideUnitSphere * randomPos + new Vector3(1, 0, 1) * randomPos;
                    randomPosition.y = 0;
                    cubeTrans.transform.SetPositionAndRotation(randomPosition, Quaternion.Euler(Random.insideUnitSphere * 360));
                    cubeTrans.transform.localScale = Random.value * randomScale * Vector3.one;


                    var bounds = cubeTrans.GetComponent<Renderer>().bounds;
                    bounds.center = cubeTrans.transform.position;
                    this.AddItem(cubeTrans.gameObject, bounds);
                }
                Init();
            }

        }

        void Update()
        {
            if (m_IsInitOver)
            {
                // 隐藏已经激活的
                var camPos = m_Cam.transform.position;
                Profiler.BeginSample("SceneTree.HideTick");
                HideTick(ref camPos);
                Profiler.EndSample();

                // 四叉树激活显示
                Profiler.BeginSample("SceneTree.OnShowTick");
                OnShow(Root);
                Profiler.EndSample();

                // 基于距离显示
                Profiler.BeginSample("SceneTree.ShowByDistance");
                ShowByDistance(Root, ref camPos);
                Profiler.EndSample();
            }
        }

        /// <summary>
        /// 添加需要进行视椎剔除的物体
        /// </summary>
        public void AddItem(GameObject p_Go, Bounds p_Bounds)
        {
            var pack = new ItemPack(p_Go, p_Bounds);
            this.AllItems.Add(pack);
        }

        /// <summary>
        /// 初始化四叉树
        /// </summary>
        public void Init()
        {
            Vector3 pos = m_Cam.transform.position;

            this.AllItems.ForEach(x =>
            {
                x.Init();
            });

            Split();
            m_Cam.transform.position = pos;

            m_IsInitOver = true;
        }


        private void Split()
        {
            Split(AllItems, null);
        }

        private void Split(List<ItemBase> p_Cubes, SceneDetectorBase p_Parent)
        {
            //1.求中心点位置
            //2.求四个子节点的中心点位置
            //3.求各个节点的bounds大小
            SceneDetectorBase Root, LF, RF, LB, RB;

            // 二分之一
            float halfX, halfZ, halfY;
            // 四分之一
            float quarterX, quarterZ, quarterY;

            Vector3 center;

            if (p_Parent == null)
            {
                float minX = float.MaxValue, minZ = float.MaxValue, minY = float.MaxValue,
                    maxX = float.MinValue, maxZ = float.MinValue, maxY = float.MinValue;

                p_Cubes.ForEach(x =>
                {
                    var cubeBounds = x.Bounds;
                    Vector3 boundsMin, boundsMax;
                    boundsMin = cubeBounds.min;
                    boundsMax = cubeBounds.max;

                    minX = Mathf.Min(boundsMin.x, minX);
                    minY = Mathf.Min(boundsMin.y, minY);
                    minZ = Mathf.Min(boundsMin.z, minZ);

                    maxX = Mathf.Max(boundsMax.x, maxX);
                    maxY = Mathf.Max(boundsMax.y, maxY);
                    maxZ = Mathf.Max(boundsMax.z, maxZ);
                });

                halfX = (maxX - minX) / 2;
                halfY = (maxY - minY) / 2;
                halfZ = (maxZ - minZ) / 2;
                quarterX = halfX / 2;
                quarterY = halfY / 2;
                quarterZ = halfZ / 2;
                center = new Vector3(minX + halfX, minY + halfY, minZ + halfZ);

                this.Root = Root = new SceneDetectorBase();
                Root.Bounds = new Bounds(center, new Vector3(halfX, halfY, halfZ) * 2);
            }
            else
            {
                var halfParentSize = p_Parent.Parent.Bounds.size / 4;

                halfX = halfParentSize.x;
                halfY = halfParentSize.y * 2;
                halfZ = halfParentSize.z;
                quarterX = halfX / 2;
                quarterZ = halfZ / 2;

                Root = p_Parent;
                center = p_Parent.Bounds.center;
            }

            var lfPos = center + new Vector3(-quarterX, 0, +quarterZ);
            var rfPos = center + new Vector3(+quarterX, 0, +quarterZ);
            var lbPos = center + new Vector3(-quarterX, 0, -quarterZ);
            var rbPos = center + new Vector3(+quarterX, 0, -quarterZ);


            LF = new SceneDetectorBase();
            LF.Bounds = new Bounds(lfPos, new Vector3(halfX, halfY * 2, halfZ));

            RF = new SceneDetectorBase();
            RF.Bounds = new Bounds(rfPos, new Vector3(halfX, halfY * 2, halfZ));

            LB = new SceneDetectorBase();
            LB.Bounds = new Bounds(lbPos, new Vector3(halfX, halfY * 2, halfZ));

            RB = new SceneDetectorBase();
            RB.Bounds = new Bounds(rbPos, new Vector3(halfX, halfY * 2, halfZ));


            Root.AddChild(LF);
            Root.AddChild(RF);
            Root.AddChild(LB);
            Root.AddChild(RB);

            p_Cubes.ForEach(x =>
            {
                //var pos = x.position;
                var cubeBounds = x.Bounds;
                // Transform itemNode = null;
                // SceneDetectorBase itemParent = null;
                foreach (var child in Root.Child)
                {
                    if (child.Bounds.Intersects(cubeBounds))
                    {
                        child.AddItem(x);
                    }
                }
            });

            // 递归所有子节点(如果子节点包含多个Child) 
            for (var i = 0; i < Root.Child.Count; i++)
            {
                var x = Root.Child[i] as SceneDetectorBase;
                if (x.ItemData.Count == 0)
                {
                    Root.Child.RemoveAt(i);
                    i--;
                }

                if (x.ItemData.Count > MaxCellCount && x.Bounds.size.x > MinCell) // 格子内颗粒度太小的不再细分 过小的不再细分
                {
                    var needSplitData = new List<ItemBase>();
                    x.ItemData.ForEach(tmp =>
                    {
                        needSplitData.Add(tmp);
                    });
                    x.ClearObj();
                    Split(needSplitData, x);
                }
            }
        }

        private void ShowByDistance(SceneDetectorBase p_Cell, ref Vector3 camerePos)
        {
            if (Vector3.Magnitude(p_Cell.Bounds.center - camerePos) < MinDisplayDistance)
            {
                ShowSingleCell(p_Cell);
            }

            foreach (var child in p_Cell.Child)
            {
                ShowByDistance(child, ref camerePos);
            }
        }

        private void ShowSingleCell(SceneDetectorBase p_Cell)
        {
            if (!ActiveTrans.Contains(p_Cell))
            {
                ActiveTrans.Add(p_Cell);
                p_Cell.ItemData.ForEach(x =>
                {
                    x.ActiveCount++;
                });
            }
        }

        private void OnDrawGizmos()
        {
            if (!IsDrawGizmos)
            {
                return;
            }
            if (Application.isPlaying && m_IsInitOver)
            {
                OnDrawGizmos(Root, Color.white);
            }

            var mat = Gizmos.matrix;
            mat.SetTRS(this.transform.position, this.transform.rotation, Vector3.one);
            Gizmos.matrix = mat;
            Gizmos.DrawFrustum(Vector3.zero, m_Cam.fieldOfView, m_Cam.farClipPlane, m_Cam.nearClipPlane, m_Cam.aspect);
        }

        private void OnDrawGizmos(SceneDetectorBase p_Cell, Color color)
        {
            var origin = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireCube(p_Cell.Bounds.center, p_Cell.Bounds.size);

            Color childColor = Color.black;
            p_Cell.Child.ForEach(x =>
            {
                if (p_Cell.DebugColor == Color.black)
                {
                    if (childColor == Color.black)
                    {
                        childColor = Random.ColorHSV(0, 1, 0, 1, .4f, 1, 1, 1);
                    }
                    else
                    {
                        p_Cell.DebugColor = childColor;
                    }
                }
                else
                {
                    childColor = p_Cell.DebugColor;
                }
                OnDrawGizmos(x as SceneDetectorBase, childColor);
            });

            Gizmos.color = origin;

        }

        private void OnShow(SceneDetectorBase p_Cell)
        {
            ShowSingleCell(p_Cell);

            for (var i = 0; i < p_Cell.Child.Count; i++)
            {
                var child = p_Cell.Child[i];
                if (child.IsDetected(m_Cam, out var isAllInside))
                {
                    if (isAllInside)
                    {
                        //TODO: 递归显示所有子类
                        ShowAllChildren(child);
                    }
                    else
                    {
                        OnShow(child);
                    }
                }
            }
        }

        private void ShowAllChildren(SceneDetectorBase p_Cell)
        {
            ShowSingleCell(p_Cell);

            for (var i = 0; i < p_Cell.Child.Count; i++)
            {
                var child = p_Cell.Child[i];
                ShowAllChildren(child);
            }
        }

        private void HideTick(ref Vector3 camerePos)
        {
            for (var i = 0; i < ActiveTrans.Count; i++)
            {
                var cell = ActiveTrans[i];
                if (!cell.IsDetected(m_Cam, out _) && Vector3.Magnitude(cell.Bounds.center - camerePos) > MinDisplayDistance)
                {
                    cell.ItemData.ForEach(itemPack =>
                    {
                        itemPack.ActiveCount--;
                        if (itemPack.ActiveCount <= 0)
                        {
                            itemPack.ActiveCount = 0;
                        }
                    });
                    ActiveTrans.RemoveAt(i);
                    i--;
                }
            }
        }
    }


    public interface IDetector
    {
        bool IsDetected(Camera mainCam, out bool p_IsAllInside);
        Bounds Bounds { get; }
    }

    public class SceneDetectorBase : IDetector
    {
        public SceneDetectorBase()
        {
        }

        public Color DebugColor = Color.black;
        public SceneDetectorBase Parent = null;
        public List<SceneDetectorBase> Child = new List<SceneDetectorBase>();
        public List<ItemBase> ItemData = new List<ItemBase>();

        public Bounds Bounds
        {
            get => _Bounds;
            set
            {
                _Bounds = value;
                BoundsVerts = new Vector3[8];
                var min = BoundsVerts[0] = value.min;
                var max = BoundsVerts[1] = value.max;
                BoundsVerts[2] = new Vector3(max.x, max.y, min.z);
                BoundsVerts[3] = new Vector3(max.x, min.y, max.z);
                BoundsVerts[4] = new Vector3(max.x, min.y, min.z);
                BoundsVerts[5] = new Vector3(min.x, max.y, max.z);
                BoundsVerts[6] = new Vector3(min.x, max.y, min.z);
                BoundsVerts[7] = new Vector3(min.x, min.y, max.z);
            }
        }

        private Bounds _Bounds;
        private Vector3[] BoundsVerts;
        public bool IsDetected(Camera mainCam, out bool p_IsAllInside)
        {
            Profiler.BeginSample("SceneTree.CheckBoundIsInCamera");
            var result = CheckExtent.CheckBoundIsInCamera(mainCam, ref BoundsVerts, out p_IsAllInside);
            Profiler.EndSample();
            return result;
        }

        /// <summary>
        /// 是否在此区间内
        /// </summary>
        /// <param name="p_Bounds"></param>
        /// <returns></returns>
        public bool Intersects(Bounds p_Bounds)
        {
            return Bounds.Intersects(p_Bounds);
        }

        public void AddChild(SceneDetectorBase child)
        {
            this.Child.Add(child);
            child.Parent = this;
        }

        public void AddItem(ItemBase go)
        {
            ItemData.Add(go);
        }

        public void ClearObj()
        {
            ItemData = new List<ItemBase>();
        }
    }

    public static class CheckExtent
    {
        private static int lastFrame = 0;
        private static Vector4[] cameraPanels;

        //检测物体是否在摄像机范围内
        public static bool CheckBoundIsInCamera(Camera p_Camera, ref Vector3[] p_BoundVerts, out bool p_IsAllInSide)
        {
            p_IsAllInSide = false;
            if (lastFrame != Time.frameCount)
            {
                lastFrame = Time.frameCount;
                cameraPanels = GetFrustumPlane(p_Camera);
                //worldToProjectionMatrix = p_Camera.projectionMatrix * p_Camera.worldToCameraMatrix;
            }
            int insideCount = 0;
            for (int j = 0; j < 8; j++)
            {
                int inCameraCount = 0;
                for (int i = 0; i < 6; i++)
                {
                    if (!IsOutsideThePlane(cameraPanels[i], p_BoundVerts[j]))
                    {
                        inCameraCount++;
                    }
                }

                if (inCameraCount == 6)
                {
                    insideCount++;
                }
            }

            if (insideCount == 8)
            {
                p_IsAllInSide = true;
            }
            return insideCount > 0;
        }

        /// <summary>
        /// 一个点和一个法向量确定一个平面
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Vector4 GetPlane(Vector3 normal, Vector3 point)
        {
            return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point));
        }

        /// <summary>
        /// 三点确定一个平面
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Vector4 GetPlane(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            return GetPlane(normal, a);
        }

        /// <summary>
        /// 获取视锥体远平面的四个点
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Vector3[] GetCameraFarClipPlanePoint(Camera camera)
        {
            Vector3[] points = new Vector3[4];
            Transform transform = camera.transform;
            float distance = camera.farClipPlane;
            float halfFovRad = Mathf.Deg2Rad * camera.fieldOfView * 0.5f;
            float upLen = distance * Mathf.Tan(halfFovRad);
            float rightLen = upLen * camera.aspect;
            Vector3 farCenterPoint = transform.position + distance * transform.forward;
            Vector3 up = upLen * transform.up;
            Vector3 right = rightLen * transform.right;
            points[0] = farCenterPoint - up - right;//left-bottom
            points[1] = farCenterPoint - up + right;//right-bottom
            points[2] = farCenterPoint + up - right;//left-up
            points[3] = farCenterPoint + up + right;//right-up
            return points;
        }
        /// <summary>
        /// 获取视锥体的六个平面
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Vector4[] GetFrustumPlane(Camera camera)
        {
            Vector4[] planes = new Vector4[6];
            Transform transform = camera.transform;
            Vector3 cameraPosition = transform.position;
            Vector3[] points = GetCameraFarClipPlanePoint(camera);
            //顺时针
            planes[0] = GetPlane(cameraPosition, points[0], points[2]);//left
            planes[1] = GetPlane(cameraPosition, points[3], points[1]);//right
            planes[2] = GetPlane(cameraPosition, points[1], points[0]);//bottom
            planes[3] = GetPlane(cameraPosition, points[2], points[3]);//up
            planes[4] = GetPlane(-transform.forward, transform.position + transform.forward * camera.nearClipPlane);//near
            planes[5] = GetPlane(transform.forward, transform.position + transform.forward * camera.farClipPlane);//far
            return planes;
        }
        /// <summary>
        /// 判断点是否在平面正面或背面
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="pointPosition"></param>
        /// <returns></returns>
        public static bool IsOutsideThePlane(Vector4 plane, Vector3 pointPosition)
        {
            if (Vector3.Dot(plane, pointPosition) + plane.w > 0)
                return true;
            return false;
        }
    }
}