/*
** Author      : Yogi
** CreateDate  : 2022-01-15 13:01:38
** Description : 
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using YogiEditor;

public class CommonWindow : EditorWindow
{
    #region Config

    #endregion

    #region Tmp

    #endregion

    #region UnityFunc

    void OnEnable()
    {
        InitData();
        InitTitle();
        InitUIElements();
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

    [MenuItem("Window/Yogi/CommonWindow")]
    public static void OpenWindow()
    {
        var window = GetWindow<CommonWindow>();
    }

    #endregion

    #region Private

    private void InitData()
    {
        InputDataManager.TestSetAll();
    }

    private void InitTitle()
    {
        titleContent.text = "Yogi's Common Window";
    }

    private void InitUIElements()
    {
        rootVisualElement.Add(new Button(() =>
        {
            rootVisualElement.Clear();
            InitUIElements();
        }) {text = "Init"});

        // rootVisualElement.Add(new Label("UI Element Demo"));
        var container = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        rootVisualElement.Add(container);

        var head = new Box()
        {
            style =
            {
                flexDirection = FlexDirection.Row,
            }
        };
        container.Add(head);

        var body = new Box();
        container.Add(body);

        var displayData = InputDataManager.GetInputByType(typeof(DemoConfig));
        var data = displayData.Cast<DemoConfig>().ToList();

        
        head.Add(new Label("SID"));
        head.Add(new Label("HP"));
        head.Add(new Label("MP"));
        head.Add(new Label("Name"));
        head.Add(new Label("Des"));

        for (var i = 0; i < data.Count; i++)
        {
            var singleBox = new Box()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                }
            };
            body.Add(singleBox);

            var singleLineData = data[i];
            var so = ScriptableObject.CreateInstance<ScriptAllData>();
            so.Config = singleLineData;
            var bindObj = new SerializedObject(so);

            var fileds = bindObj.targetObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

            var firstName= nameof(so.Config);
            var seconedNames = so.Config.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in seconedNames)
            {
                var seconedName = field.Name;
                singleBox.Add(new PropertyField(bindObj.FindProperty($"{firstName}.{seconedName}")));
            }
            
            // var sid = new IntegerField() {bindingPath = "Config.SID"};
            // singleBox.Add(sid);
            // singleBox.Add(new FloatField() {bindingPath = "Config.HP"});
            // singleBox.Add(new FloatField() {bindingPath = "Config.MP"});
            // singleBox.Add(new TextField()
            //     {bindingPath = "Config.BaseInfo.Name"});
            // singleBox.Add(new TextField()
            //     {bindingPath = "Config.BaseInfo.Description"});
            //
            // sid.Add(new Button(() =>
            // {
            //     //TODO: 打开与生成蓝图窗口
            // }) {text = "Open"});
            singleBox.Bind(bindObj);
        }
    }



    #endregion
}