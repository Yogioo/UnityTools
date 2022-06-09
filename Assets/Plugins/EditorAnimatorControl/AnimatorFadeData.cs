using System.Collections.Generic;
using UnityEngine;

namespace EditorAnimatorControl
{
    public class AnimatorFadeData : ScriptableObject
    {
        /// <summary>
        /// 第一个动画节点名
        /// </summary>
        public string FirstStateName = "idle";
        /// <summary>
        /// 是否自动播放
        /// </summary>
        public bool IsAutoPlay = true;
        /// <summary>
        /// 是否循环
        /// </summary>
        public bool IsLoop = true;

        /// <summary>
        /// 播放速度
        /// </summary>
        public float PlaySpeed = 1.0f;
        
        /// <summary>
        /// 是否混合其他动画
        /// </summary>
        public bool IsCrossFade = true;

        /// <summary>
        /// 第二个动画节点名
        /// </summary>
        public string SecondStateName = "winpose";

        /// <summary>
        /// 开始混合时间
        /// </summary>
        public float StartCrossFadeTime;

        /// <summary>
        /// 是否是固定时间混合
        /// </summary>
        public bool IsFixedFade = true; //IsFixedTime

        /// <summary>
        /// 正常时间混合
        /// </summary>
        public float NormalizeFadeTime = 0.1f;

        /// <summary>
        /// 固定时间混合
        /// </summary>
        public float FixedFadeTime = .1f;

        /// <summary>
        /// 混合偏移时间
        /// </summary>
        public float TimeOffset = 0.0f;

        /// <summary>
        /// 动画层 暂时不加入UI里面
        /// </summary>
        public int AnimLayer = 0;
        /// <summary>
        /// 动画的事件
        /// </summary>
        public List<EventData> Evnets=new List<EventData>()
        {
            // new EventData(0.1f, 0.2f, "Event0","起飞~"),
            // new EventData(0.3f, 0.1f, "Event2","下落~")
        };
        
    }

    [System.Serializable]
    public class EventData
    {
        public float StartTime;
        public float Duration;
        public string Name;
        public string Description;
        public Color DisplayColor;

        public EventData(float p_Start,float p_Duration, string p_Name,string p_Description,Color p_DisplayColor)
        {
            StartTime = p_Start;
            Duration = p_Duration;
            Name = p_Name;
            Description = p_Description;
            DisplayColor = p_DisplayColor;
        }

        public override string ToString()
        {
            return $"{Name}\n{Description}\nS:{StartTime}s\nD:{Duration}s";
        }
    }
}