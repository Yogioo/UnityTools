using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace EditorAnimatorControl.Editor
{
    /// <summary>
    /// 动画混合的预览窗口 包含Timeline 包含动画控制 包含预览
    /// </summary>
    public class AnimBlendPreviewWindow : EditorWindow
    {
        [MenuItem("AnimSystem/AnimBlendPreviewWindow")]
        public static AnimBlendPreviewWindow Popup()
        {
            return EditorWindow.GetWindow<AnimBlendPreviewWindow>();
        }

        // 1. 首先实现预览生物和摄像机控制功能

        #region Config

        private const string AnimPreviewUXMLPath = @"Assets\YogiEditor\EditorAnimatorControl\Editor\AnimPreview.uxml";

        #endregion

        #region TMP

        /// <summary>
        /// 预览窗口类
        /// </summary>
        private PreviewRenderUtility m_PreviewRenderUtility;

        /// <summary>
        /// 摄像机注视中心
        /// </summary>
        private Transform m_LookAtCenter;

        /// <summary>
        /// 预览对象
        /// </summary>
        private GameObject m_PreviewGo;

        /// <summary>
        /// 预览图片UI
        /// </summary>
        private VisualElement m_PreviewImgUI;

        /// <summary>
        /// 预览图片
        /// </summary>
        private Texture2D m_PreviewImg;


        private Camera m_Cam;
        private Transform m_CamTrans;

        /// <summary>
        /// 鼠标滚轮
        /// </summary>
        private float m_MouseWheelDelta;

        private bool m_IsDragging = false, m_IsMoveDragging = false;
        private Vector2 m_MouseDragDelta;
        private bool m_IsReset;

        #endregion

        #region UnityFunc

        private void OnEnable()
        {
            // 初始化Preview
            InitPreview();

            InitUIElements();

            // 生成物体
            var goTMP = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GeneratePreviewGO(goTMP);
        }

        private void OnDisable()
        {
            DestructPreview();
        }

        private void Update()
        {
            UpdateScene();
            m_PreviewImg = RenderPreview();
            UpdateUIElements();
        }

        private void OnGUI()
        {
            UpdateMousePos();
        }

        #endregion

        #region 预览生物

        private void InitPreview()
        {
            m_PreviewRenderUtility = new PreviewRenderUtility(true, true);

            m_Cam = m_PreviewRenderUtility.camera;
            m_CamTrans = m_Cam.transform;
            m_Cam.farClipPlane = 500;
            m_Cam.clearFlags = CameraClearFlags.SolidColor;
            m_IsReset = true;

            m_LookAtCenter = new GameObject().transform;
            ResetByKeyF();

            m_PreviewRenderUtility.AddSingleGO(m_LookAtCenter.gameObject);
        }

        private void DestructPreview()
        {
            m_PreviewRenderUtility?.Cleanup();
            m_PreviewRenderUtility = null;
        }

        private void GeneratePreviewGO(GameObject p_GO)
        {
            m_PreviewGo = p_GO;
            m_PreviewRenderUtility.AddSingleGO(m_PreviewGo);
        }

        private void UpdateScene()
        {
            ScaleByMouseWheel();
            RotateByMouseDrag();
            MoveByMouseMidDrag();
            ResetByKeyF();
        }

        private void ScaleByMouseWheel()
        {
            if (Vector3.Distance(m_CamTrans.position, m_LookAtCenter.position) < 10 && -m_MouseWheelDelta > 0)
            {
                m_MouseWheelDelta = 0;
            }

            m_CamTrans.position += -m_MouseWheelDelta * m_CamTrans.forward;
            m_MouseWheelDelta = 0;
        }

        private void RotateByMouseDrag()
        {
            if (m_IsDragging)
            {
                CameraRotate(m_MouseDragDelta);
                m_MouseDragDelta = Vector2.zero;
            }
        }

        private void CameraRotate(Vector2 p_MouseDragDelta)
        {
            var lookAtTargetPos = m_LookAtCenter.position;
            m_CamTrans.RotateAround(lookAtTargetPos, m_CamTrans.right, p_MouseDragDelta.y);
            m_CamTrans.RotateAround(lookAtTargetPos, m_CamTrans.up, p_MouseDragDelta.x);
            m_CamTrans.LookAt(m_LookAtCenter);
        }
        
        private void MoveByMouseMidDrag()
        {
            if (m_IsMoveDragging)
            {
                Vector3 offset = -m_MouseDragDelta.x * m_CamTrans.right
                                 + m_MouseDragDelta.y * m_CamTrans.up;
                offset *= 0.01f;
                m_LookAtCenter.position += offset;
                m_CamTrans.position += offset;
                m_MouseDragDelta = Vector2.zero;
            }
        }

        private void UpdateMousePos()
        {
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0) //左键
                    {
                        m_IsDragging = true;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    else if (e.button == 2) //中键
                    {
                        m_IsMoveDragging = true;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                case EventType.MouseUp:
                    if (e.button == 0 && m_IsDragging)
                    {
                        m_IsDragging = false;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    else if (e.button == 2 && m_IsMoveDragging)
                    {
                        m_IsMoveDragging = false;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }

                    break;
                case EventType.MouseMove:
                    break;
                case EventType.MouseDrag:
                    if (m_IsDragging || m_IsMoveDragging)
                    {
                        m_MouseDragDelta = e.delta * (!e.shift?1:3);
                    }
                    break;
                case EventType.ScrollWheel:
                    m_MouseWheelDelta += -HandleUtility.niceMouseDeltaZoom;
                    break;
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.F)
                    {
                        m_IsReset = true;
                        ResetByKeyF();
                    }
                    break;
            }
        }

        private void ResetByKeyF()
        {
            if (m_IsReset)
            {
                m_CamTrans.position = new Vector3(0, 0, -10);
                m_CamTrans.forward = Vector3.forward;
                CameraRotate(new Vector2(50,45));

                m_IsReset = false;
                m_LookAtCenter.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        private Texture2D RenderPreview()
        {
            m_PreviewRenderUtility.BeginPreview(new Rect(0, 0, 1024, 1024), GUIStyle.none);
            m_PreviewRenderUtility.Render();
            return m_PreviewRenderUtility.EndStaticPreview();
        }

        #endregion

        #region UI Elements

        private void InitUIElements()
        {
            var animPreviewTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AnimPreviewUXMLPath);
            animPreviewTree.CloneTree(rootVisualElement);
            m_PreviewImgUI = rootVisualElement.Q<VisualElement>("PreviewImg");
        }

        private void UpdateUIElements()
        {
            var minSize = Mathf.Min(this.position.width, this.position.height);
            m_PreviewImgUI.style.width = minSize;
            m_PreviewImgUI.style.height = minSize;
            m_PreviewImgUI.style.backgroundImage = m_PreviewImg;
        }

        #endregion
    }
}