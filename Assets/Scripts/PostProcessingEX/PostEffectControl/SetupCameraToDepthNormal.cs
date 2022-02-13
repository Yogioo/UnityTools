/*
** Author      : Yogi
** CreateDate  : 2022-38-13 10:38:14
** Description : 
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupCameraToDepthNormal : MonoBehaviour
{
    #region Config

    #endregion

    #region Tmp

    #endregion

    #region UnityFunc

    void OnEnable()
    {
        Camera.main.depthTextureMode = DepthTextureMode.DepthNormals;
    }
    void OnDisable()
    {
        
    }

    void Awake()
    {

    }

    void Start()
    {
        
    }

    void Update()
    {

    }

    void OnDestroy()
    {

    }

    #endregion

    #region Public

    #endregion
    
    #region Private

    #endregion
    
}
