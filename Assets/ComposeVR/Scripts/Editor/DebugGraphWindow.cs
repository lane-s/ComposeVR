using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class DebugGraph
{
    private static DebugGraphWindow window;

    public static void SetWindow(DebugGraphWindow window)
    {
        DebugGraph.window = window;
    }

    public static void Log(string variableName, float value)
    {
        if(window == null)
        {
            DebugGraphWindow.ShowWindow();
        }

        window.Log(variableName, value);
    }
}

[InitializeOnLoadAttribute]
public class DebugGraphWindow : EditorWindow
{

    public Dictionary<string, Queue<Vector2>> graphData;
    public Dictionary<string, Color> graphColors;

    private Rect clearButtonRect;

    private Rect normalizedGraphViewport;
    private Rect graphSpaceViewport;

    private Rect xAxisRect;
    private bool draggingXAxis;

    private Rect yAxisRect;
    private bool draggingYAxis;

    private Vector2 lastDragPosition;

    private Vector2 graphMin;
    private Vector2 graphMax;

    private RenderTexture graphTexture;
    private Rect graphWindowRect;
    private Rect previousGraphWindowRect;

    private Color graphBgColor;
    private Color graphXAxisBgColor;
    private Color graphYAxisBgColor;

    private Vector2 previousWindowSize;
    private const int GRAPH_LEFT_MARGIN = 200;
    private const int CONTROL_WIDTH = 150;
    private const int AXIS_WIDTH = 20;

    private const float SMALL_MARGIN = 15.0f;

    private const int GRAPH_BOTTOM_MARGIN = 15;

    private const float HORIZONTAL_SCALE_SENSITIVITY = 1f;
    private const float VERTICAL_SCALE_SENSITIVITY = 3f;

    private float timeOffset;

    public void Awake()
    {
        graphData = new Dictionary<string, Queue<Vector2>>();
        graphColors = new Dictionary<string, Color>();

        clearButtonRect = new Rect(0, 5, 85, 25);

        graphMin = new Vector2(0, 0);
        graphMax = new Vector2(1, 1);

        normalizedGraphViewport = new Rect(0, 0, 1, 1);
        graphSpaceViewport = new Rect(0, 0, 1, 1);

        xAxisRect = new Rect(GRAPH_LEFT_MARGIN, 0, (int)position.width - GRAPH_LEFT_MARGIN, AXIS_WIDTH);
        yAxisRect = new Rect(GRAPH_LEFT_MARGIN - AXIS_WIDTH, 0, AXIS_WIDTH, (int)position.height);

        graphWindowRect = new Rect(GRAPH_LEFT_MARGIN, 0, (int)position.width - GRAPH_LEFT_MARGIN, (int)position.height);
        graphBgColor = new Color(0.2f, 0.2f, 0.2f);
        graphXAxisBgColor = Color.grey;
        graphYAxisBgColor = new Color(0.4f, 0.4f, 0.4f);

        UseRandomData();

    }

    [MenuItem("Window/DebugGraph")]
    public static void ShowWindow()
    {
        DebugGraphWindow window = GetWindow<DebugGraphWindow>("Debug Graph");
        DebugGraph.SetWindow(window);
        window.previousWindowSize = new Vector2(window.position.width, window.position.height);
    }

    public void Log(string variableName, float value)
    {
        Log(variableName, new Vector2(Time.time + timeOffset, value));
    }

    public void Log(string variableName, Vector2 point)
    {
        if (!graphData.ContainsKey(variableName))
        {
            graphData.Add(variableName, new Queue<Vector2>());
            graphColors.Add(variableName, Color.green);
        }

        graphData[variableName].Enqueue(point);

        graphMax.x = Mathf.Max(graphMax.x, point.x);
        graphMax.y = Mathf.Max(graphMax.y, point.y);

        graphMin.x = Mathf.Min(graphMin.x, point.x);
        graphMin.y = Mathf.Min(graphMin.y, point.y);

    }

    private void OnGUI()
    {
        DrawValueLabels();
        DrawClearButton();

        DrawAxis();
        HandleAxisInput();

        DrawGraph();
    }

    private void DrawValueLabels()
    {
        GUILayout.BeginArea(new Rect(SMALL_MARGIN, SMALL_MARGIN, CONTROL_WIDTH, position.height - SMALL_MARGIN * 2));
        int entryIndex = 0;

        foreach(KeyValuePair<string, Queue<Vector2>> entry in graphData)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(entry.Key + ": ");
            graphColors[entry.Key] = EditorGUILayout.ColorField(graphColors[entry.Key]);
            entryIndex += 1;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndArea();
    }

    private void DrawClearButton()
    {
        clearButtonRect.x = CONTROL_WIDTH/2 - clearButtonRect.width / 2;
        clearButtonRect.y = position.height - SMALL_MARGIN - clearButtonRect.height;
        if (GUI.Button(clearButtonRect, "Clear"))
        {
            graphData = new Dictionary<string, Queue<Vector2>>();
        }
    }

    private void DrawAxis()
    {
        yAxisRect.height = position.height;
        xAxisRect.width = position.width - GRAPH_LEFT_MARGIN;

        EditorGUIUtility.AddCursorRect(xAxisRect, MouseCursor.Zoom);
        EditorGUIUtility.AddCursorRect(yAxisRect, MouseCursor.Zoom);

        if(Event.current.type == EventType.Repaint)
        {
            //Viewport seems to use inverse y coordinates from the EditorGUIUtility
            Rect xAxisRectViewport = new Rect(xAxisRect.x, position.height - AXIS_WIDTH, xAxisRect.width, xAxisRect.height);
            GL.PushMatrix();
            GL.LoadPixelMatrix();
            GL.Viewport(xAxisRectViewport);
            GL.Clear(true, true, graphXAxisBgColor);
            GL.PopMatrix();

            GL.PushMatrix();
            GL.LoadPixelMatrix();
            GL.Viewport(yAxisRect);
            GL.Clear(true, true, graphYAxisBgColor);
            GL.PopMatrix();
        }
    }

    private void HandleAxisInput()
    {
        if(!draggingXAxis && Event.current.type == EventType.MouseDown && xAxisRect.Contains(Event.current.mousePosition))
        {
            draggingXAxis = true;
            lastDragPosition = Event.current.mousePosition;
        }else if(draggingXAxis && Event.current.rawType == EventType.MouseDrag)
        {
            Vector2 dragVector = Event.current.mousePosition - lastDragPosition;
            lastDragPosition = Event.current.mousePosition;

            //Handle scaling and panning    
            if(dragVector.magnitude > 0)
            {
                float widthChange = dragVector.y / Screen.height * HORIZONTAL_SCALE_SENSITIVITY * normalizedGraphViewport.width;

                normalizedGraphViewport.width += widthChange;
                normalizedGraphViewport.x -= (dragVector.x / Screen.width * normalizedGraphViewport.width + widthChange / 2);

                Repaint();
            }

        }else if(draggingXAxis && Event.current.rawType == EventType.MouseUp)
        {
            draggingXAxis = false;
        }


        if(!draggingYAxis && Event.current.type == EventType.MouseDown && yAxisRect.Contains(Event.current.mousePosition))
        {
            draggingYAxis = true;
            lastDragPosition = Event.current.mousePosition;
        }else if(draggingYAxis && Event.current.rawType == EventType.MouseDrag)
        {
            Vector2 dragVector = Event.current.mousePosition - lastDragPosition;
            lastDragPosition = Event.current.mousePosition;

            //Handle scaling and panning    
            if(dragVector.magnitude > 0)
            {
                float heightChange = dragVector.x / Screen.width * VERTICAL_SCALE_SENSITIVITY * normalizedGraphViewport.height;

                normalizedGraphViewport.height -= heightChange;
                normalizedGraphViewport.y -= (dragVector.y / Screen.height * normalizedGraphViewport.height - heightChange / 2);

                Repaint();
            }
        }else if(draggingYAxis && Event.current.rawType == EventType.MouseUp)
        {
            draggingYAxis = false;
        }
    }

    private void DrawGraph()
    {
        graphWindowRect.width = position.width - GRAPH_LEFT_MARGIN;
        graphWindowRect.height = position.height - AXIS_WIDTH;

        if (previousGraphWindowRect.width != graphWindowRect.width)
        {
            //Update normalizedGraphViewport width
        }else if(previousGraphWindowRect.height != graphWindowRect.height)
        {
            //Update normalizedGraphViewport height
        }

        //Resizing the graph window should also resize the viewport so that no scaling occurs from resizing the window

        if(Event.current.type == EventType.Repaint)
        {
            float graphWidth = graphMax.x - graphMin.x;
            float graphHeight = graphMax.y - graphMin.y;

            graphSpaceViewport.width = normalizedGraphViewport.width * graphWidth;
            graphSpaceViewport.height = normalizedGraphViewport.height * graphHeight;
            graphSpaceViewport.x = normalizedGraphViewport.x * graphWidth;
            graphSpaceViewport.y = normalizedGraphViewport.y * graphHeight;

            GL.PushMatrix();
            GL.LoadPixelMatrix();
            GL.Viewport(graphWindowRect);

            GL.Clear(true, true, graphBgColor);
            foreach(KeyValuePair<string, Queue<Vector2>> entry in graphData)
            {
                GL.Begin(GL.LINES);
                GL.Color(graphColors[entry.Key]);

                Vector3 lastPoint = TransformGraphVertex(entry.Value.Peek());
                foreach(Vector2 point in entry.Value)
                {
                    Vector3 vertex = TransformGraphVertex(point);
                    GL.Vertex(lastPoint);
                    GL.Vertex(vertex);
                    lastPoint = vertex;
                }
                GL.End();
            }

            GL.PopMatrix();
        }

        previousGraphWindowRect = graphWindowRect;
    }

    private Vector3 TransformGraphVertex(Vector2 v)
    {
        float normalizedX = (v.x - graphSpaceViewport.x) / graphSpaceViewport.width;
        float normalizedY = (v.y - graphSpaceViewport.y) / graphSpaceViewport.height;

        return new Vector3(normalizedX * Screen.width, normalizedY * Screen.height - GRAPH_BOTTOM_MARGIN, 0);
    }

    private void UseRandomData()
    {
        Queue<Vector2> randomPoints = new Queue<Vector2>();

        float timeStep = 10f;
        float time = 0;

        int steps = 250;
        float range = 250;

        for(int i = 0; i < steps; i++)
        {
            Log("test", new Vector2(time, Random.value * range));
            time += timeStep;
        }

    }
}