/*
** Author      : Yogi
** CreateDate  : 2022-55-15 18:55:50
** Description : 
*/

using GraphProcessor;
using UnityEditor;
using UnityEngine;

public class BPWindow : BaseGraphWindow
{
    BaseGraph tmpGraph;

    [MenuItem("Window/Yogi/BPWindow")]
    public static BaseGraphWindow OpenWithTmpGraph()
    {
        var graphWindow = CreateWindow<BPWindow>();

        // When the graph is opened from the window, we don't save the graph to disk
        graphWindow.tmpGraph = ScriptableObject.CreateInstance<BaseGraph>();
        graphWindow.tmpGraph.hideFlags = HideFlags.HideAndDontSave;
        graphWindow.InitializeGraph(graphWindow.tmpGraph);

        graphWindow.Show();

        return graphWindow;
    }

    protected override void InitializeWindow(BaseGraph graph)
    {
        titleContent = new GUIContent("BP Graph");

        if (graphView == null)
        {
            graphView = new BaseGraphView(this);
            graphView.Add(new MiniMapView(graphView));
            // toolbarView = new CustomToolbarView(graphView);
            // graphView.Add(toolbarView);
        }
        
        rootView.Add(graphView);
    }
    
    protected override void OnDestroy()
    {
        graphView?.Dispose();
        DestroyImmediate(tmpGraph);
    }
}