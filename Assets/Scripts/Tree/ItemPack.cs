using UnityEngine;

namespace Sunset.SceneManagement
{
    /// <summary>
    /// 物体基类
    /// </summary>
    public abstract class ItemBase
    {
        public GameObject GameObject;
        public Bounds Bounds;
        public int ActiveCount
        {
            get => _ActiveCount;
            set
            {
                if (value != 0 && _ActiveCount == 0)
                {
                    SetActive(true);
                }
                else if (value == 0)
                {
                    SetActive(false);
                }
                _ActiveCount = value;
            }
        }

        private int _ActiveCount = 1;
        public abstract void SetActive(bool p_IsActive);

        protected ItemBase(GameObject p_GO, Bounds p_Bounds)
        {
            this.Bounds = p_Bounds;
            this.GameObject = p_GO;
        }

        public void Init()
        {
            this.ActiveCount = 0;
        }
    }
    /// <summary>
    /// 重写的物体类, 被看到和没被看到会触发GameObject的显示隐藏, 如果有其他需求可以重写ItemBase实现其他效果
    /// </summary>
    public class ItemPack : ItemBase
    {
        public ItemPack(GameObject p_GO, Bounds p_Bounds) : base(p_GO, p_Bounds)
        {
        }

        public override void SetActive(bool p_IsActive)
        {
            this.GameObject.SetActive(p_IsActive);
        }
    }
}
