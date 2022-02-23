/*
** Author      : Yogi
** CreateDate  : 2022-15-23 21:15:17
** Description : 
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudBox : MonoBehaviour
{
    #region Config

    #endregion

    #region Tmp

    #endregion

    #region UnityFunc

    void OnEnable()
    {

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

    private void OnDrawGizmosSelected()
    {
    }

    private void OnDrawGizmos()
    {
        var o = Gizmos.color;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(this.transform.position, this.transform.localScale);
        
        Gizmos.color = o;
    }

    #endregion
    
}
