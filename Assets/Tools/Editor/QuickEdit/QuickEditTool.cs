using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Reflection;

// Tagging a class with the EditorTool attribute and no target type registers a global tool. Global tools are valid for any selection, and are accessible through the top left toolbar in the editor.
[EditorTool("Quick Edit Tool")]
class QuickEditTool : EditorTool
{
    const float AXISDRAWSIZE = 1000;

    // Serialize this value to set a default value in the Inspector.
    [SerializeField]
    Texture2D m_ToolIcon;
    [SerializeField]
    Texture2D iconGrab;
    int iconGrabSize = 16;

    GUIContent m_IconContent;

    // Internal
    private bool needsRepaint;

    
    // Current mode
    enum EditAction { NONE, GRAB, ROTATE, SCALE };
    EditAction currentEditAction = EditAction.NONE;
    int startUndoGroup = -1;
    Transform[] transforms;
    CopiedTransform[] copiedTransforms;


    // Current axes
    enum ViewAxesMode { VIEW, GLOBAL, LOCAL };
    ViewAxesMode currentAxisMode = ViewAxesMode.VIEW;
    enum ViewAxes { X, Y, Z, XY, YZ, XZ };
    ViewAxes currentAxis = ViewAxes.XY;
    private Vector2 mousePosition;
    private Vector2 startMousePosition;
    private Vector3 startCenterHandlePosition;
    private Quaternion startCenterHandleRotation;

    private struct CopiedTransform {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    // TODO: take this into account everywhere to add support for multiple scene views! ??
    private static QuickEditTool lastActive;
    private EditorWindow lastActiveWindow;

    void OnEnable()
    {
        m_IconContent = new GUIContent()
        {
            image = m_ToolIcon,
            text = "Quick Edit Tool",
            tooltip = "Quick Edit Tool"
        };

        ExitWithoutSaving();
        RepaintIfNeeded();

        lastActive = this;
    }

    void OnDisable() {
        ExitWithoutSaving();
        RepaintIfNeeded();
        if(lastActive == this) {
            lastActive = null;
        }
    }

    [MenuItem("Edit/QuickEdit/Grab", false, 0)]
    public static void GrabCommand() {
        if(lastActive != null) {
            lastActive.StartAction(EditAction.GRAB);
        }
    }

    [MenuItem("Edit/QuickEdit/Rotate", false, 0)]
    public static void RotateCommand() {
        if(lastActive != null) {
            lastActive.StartAction(EditAction.ROTATE);
        }
    }

    [MenuItem("Edit/QuickEdit/Scale", false, 0)]
    public static void ScaleCommand() {
        if(lastActive != null) {
            lastActive.StartAction(EditAction.SCALE);
        }
    }

    void ExitWithoutSaving() {
        if(currentEditAction != EditAction.NONE && startUndoGroup!=-1) {
            Undo.RevertAllDownToGroup(startUndoGroup);
            startUndoGroup = -1;
            currentEditAction = EditAction.NONE;
            HandleUtility.Repaint();
        }
    }

    void ExitWithSaving() {
        if(currentEditAction != EditAction.NONE) {
            currentEditAction = EditAction.NONE;
            needsRepaint = true;
        }
    }

    void RepaintIfNeeded() {
        if(needsRepaint) {
            HandleUtility.Repaint();
            //SceneView.RepaintAll();
            needsRepaint = false;
        }
    }

    void StartAction(EditAction action) {
        if(currentEditAction != EditAction.NONE) {
            return;
        }
        if(this.lastActiveWindow != null) {
            this.lastActiveWindow.Focus();
        }
        Undo.IncrementCurrentGroup();
        startUndoGroup = Undo.GetCurrentGroup();
        currentEditAction = action;
        transforms = Selection.transforms;
        currentAxisMode = ViewAxesMode.VIEW;
        currentAxis = ViewAxes.XY;
        startMousePosition = mousePosition;
        startCenterHandlePosition = Tools.handlePosition;
        startCenterHandleRotation = Tools.handleRotation;

        // Snapshot transforms
        copiedTransforms = new CopiedTransform[transforms.Length];
        for(int i=0;i<transforms.Length;i++) {
            copiedTransforms[i].position = transforms[i].position;
            copiedTransforms[i].rotation = transforms[i].rotation;
            copiedTransforms[i].scale = transforms[i].localScale;
        }
    }

    void CalculateAxesToView(out Vector3 axisA, out Vector3 axisB) {
        Ray cameraCenter = Camera.current.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 viewNormal = cameraCenter.direction.normalized;

        Ray cameraUp = Camera.current.ViewportPointToRay(new Vector3(0.5f, 0));
        Vector3 upVector = ((cameraUp.origin + cameraUp.direction) - (cameraCenter.origin + cameraCenter.direction)).normalized;
        upVector = (upVector - Vector3.Dot(viewNormal, upVector) * viewNormal).normalized;
        Vector3 rightVector = Vector3.Cross(viewNormal, upVector);

        axisA = upVector;
        axisB = rightVector;
    }

    void CalculateAxesGlobal(out Vector3 axisA, out Vector3 axisB, out bool hasSecondAxis) {
        axisA = Vector3.zero;
        axisB = Vector3.zero;
        hasSecondAxis = false;
        if(currentAxis == ViewAxes.X) {
            axisA = Vector3.right;
            axisB = Vector3.zero;
        } else if (currentAxis == ViewAxes.Y) {
            axisA = Vector3.up;
            axisB = Vector3.zero;
        } else if (currentAxis == ViewAxes.Z) {
            axisA = Vector3.forward;
            axisB = Vector3.zero;
        } else if (currentAxis == ViewAxes.XY) {
            axisA = Vector3.right;
            axisB = Vector3.up;
            hasSecondAxis = true;
        } else if (currentAxis == ViewAxes.YZ) {
            axisA = Vector3.up;
            axisB = Vector3.forward;
            hasSecondAxis = true;
        } else if (currentAxis == ViewAxes.XZ) {
            axisA = Vector3.right;
            axisB = Vector3.forward;
            hasSecondAxis = true;
        }
    }

    void CalculateAxesColors(out Color axisAcolor, out Color axisBcolor) {
        axisAcolor = Color.white;
        // First axis
        if(currentAxis == ViewAxes.X) {
            axisAcolor = Color.red;
        } else if(currentAxis == ViewAxes.Y) {
            axisAcolor = Color.green;
        } else if(currentAxis == ViewAxes.Z) {
            axisAcolor = Color.blue;
        } else if(currentAxis == ViewAxes.XY) {
            axisAcolor = Color.red;
        } else if(currentAxis == ViewAxes.XZ) {
            axisAcolor = Color.red;
        } else if(currentAxis == ViewAxes.YZ) {
            axisAcolor = Color.green;
        }
        axisBcolor = Color.white;
        if(currentAxis == ViewAxes.XY) {
            axisBcolor = Color.green;
        } else if(currentAxis == ViewAxes.XZ) {
            axisBcolor = Color.blue;
        } else if(currentAxis == ViewAxes.YZ) {
            axisBcolor = Color.blue;
        }
    }

    void CalculateAxesLocal(Quaternion rotation, out Vector3 axisA, out Vector3 axisB, out bool hasSecondAxis) {
        Vector3 axisAglobal = Vector3.zero;
        Vector3 axisBglobal = Vector3.zero;
        CalculateAxesGlobal(out axisAglobal, out axisBglobal, out hasSecondAxis);
        axisA = rotation * axisAglobal;
        axisB = rotation * axisBglobal;
    }

    Vector2 FindPosition(Ray r, Vector3 point, Vector3 axisA, Vector3 axisB, bool hasSecondAxis) {
        if(hasSecondAxis) {
            // We have a plane...
            // Try plane intersection first
            float enter;
            Vector3 planeNormal = Vector3.Cross(axisA, axisB);
            if(new Plane(planeNormal, point).Raycast(r, out enter)) {
                // Ok, we got lucky, we hit the plane!
                Vector3 globalPoint = r.GetPoint(enter);
                float a = Vector3.Dot(axisA, globalPoint-point);
                float b = Vector3.Dot(axisB, globalPoint-point);
                return new Vector2(a, b);
            }

            // We didn't hit the plane
            // Project the normal of the plane on the screen
            // + take Cross product of the sceen + plane normal
            // Then try again
            Vector3 viewDir = Camera.current.ViewportPointToRay(new Vector3(0.5f, 0.5f)).direction.normalized;
            Vector3 planeNormalScreenPlane = (planeNormal - Vector3.Dot(viewDir, planeNormal)*planeNormal).normalized;
            Vector3 planeTangentScreenPlane = Vector3.Cross(viewDir, planeNormalScreenPlane);
            
            if(new Plane(viewDir, point).Raycast(r, out enter)) {
                // We hit the screen plane (obviously)
                Vector3 globalPoint = r.GetPoint(enter);
                // Project onto plane
                globalPoint = globalPoint - Vector3.Dot(globalPoint - point, planeNormalScreenPlane)*planeNormalScreenPlane;

                float a = Vector3.Dot(axisA, globalPoint-point);
                float b = Vector3.Dot(axisB, globalPoint-point);
                return new Vector2(a, b);
            }

            // Object is behind us?? How can we not have hit the screen plane?
            r = new Ray(r.origin, -r.direction);
            if(new Plane(viewDir, point).Raycast(r, out enter)) {
                // We hit the screen plane (obviously)
                Vector3 globalPoint = r.GetPoint(enter);
                // Project onto plane
                globalPoint = globalPoint - Vector3.Dot(globalPoint - point, planeNormalScreenPlane)*planeNormalScreenPlane;

                float a = Vector3.Dot(axisA, globalPoint-point);
                float b = Vector3.Dot(axisB, globalPoint-point);
                return new Vector2(a, b);
            }

            // Still not hitting anything?? How did this happen?
            return Vector2.zero;
        } else {
            // Just a line
            Vector3 viewDir = Camera.current.ViewportPointToRay(new Vector3(0.5f, 0.5f)).direction.normalized;
            Vector3 planeNormal = (viewDir - Vector3.Dot(axisA, viewDir) * axisA).normalized;
            Vector3 altAxis = Vector3.Cross(viewDir, planeNormal);

            float enter;
            if(new Plane(planeNormal, point).Raycast(r, out enter)) {
                // We hit the plane
                Vector3 globalPoint = r.GetPoint(enter);
                float a = Vector3.Dot(axisA, globalPoint-point);
                return new Vector2(a, 0);
            }

            // Yeah, we are not hitting anything
            return Vector2.zero;
        }
    }

    public override GUIContent toolbarIcon
    {
        get { return m_IconContent; }
    }

    // This is called for each window that your tool is active in. Put the functionality of your tool here.
    public override void OnToolGUI(EditorWindow window)
    {
        Event guiEvent = Event.current;

        if(this.lastActiveWindow == null) {
            // No, window, just pick one
            this.lastActiveWindow = window;
        }

        //EditorGUIUtility.AddCursorRect(new Rect(0, 0, 100000, 100000), MouseCursor.Pan);

        if(guiEvent.type == EventType.Repaint) {
            Draw();
        } else if (guiEvent.type == EventType.Layout) {
            if(currentEditAction != EditAction.NONE) {
                int controlId = GUIUtility.GetControlID(FocusType.Passive);
                HandleUtility.AddDefaultControl(controlId);
                if(GUIUtility.hotControl == controlId) {
                    Debug.Log("Active!");
                    this.lastActiveWindow = window;
                }
            }
        } else {
            HandleInput(guiEvent);
            RepaintIfNeeded();
        }
        
        // Don't allow other things to interfere while our component is dragging...
        if(currentEditAction != EditAction.NONE) {
            // Eat all events while in "dragging" mode
            if(guiEvent.type != EventType.Layout && guiEvent.type != EventType.Repaint) {
                guiEvent.Use();
            }
        }
    }

    private void ToggleAxis(ViewAxes axis) {
        if(currentAxisMode == ViewAxesMode.VIEW) {
            currentAxisMode = ViewAxesMode.GLOBAL;
            currentAxis = axis;
        } else if(currentAxisMode == ViewAxesMode.GLOBAL && currentAxis == axis) {
            currentAxisMode = ViewAxesMode.LOCAL;
        } else {
            currentAxisMode = ViewAxesMode.GLOBAL;
            currentAxis = axis;
        }
    }

    private void RestoreTransformsOngoing() {
        for(int i=0;i<transforms.Length;i++) {
            transforms[i].position = copiedTransforms[i].position;
            transforms[i].rotation = copiedTransforms[i].rotation;
            transforms[i].localScale = copiedTransforms[i].scale;
        }
    }

    private Vector3 ScaleVector(Vector3 vect, float scaleFactor, Vector3 axisA, Vector3 axisB, Vector3 axisC, bool hasSecondAxis) {
        float affectedA = Vector3.Dot(axisA, vect);
        if(hasSecondAxis) {
            float affectedB = Vector3.Dot(axisB, vect);
            float affectedC = Vector3.Dot(axisC, vect);
            return affectedA * axisA * scaleFactor + affectedB * axisB * scaleFactor + affectedC * axisC;
        } else {
            return vect - affectedA * axisA + affectedA * scaleFactor * axisA;
        }
    }

    private void HandleInput(Event guiEvent) {
        mousePosition = guiEvent.mousePosition;
        if(currentEditAction == EditAction.NONE) {
            // Not in an action
            /*if(guiEvent.type == EventType.KeyDown) {
                if (guiEvent.keyCode == KeyCode.G) {
                    guiEvent.Use();
                    StartAction(EditAction.DRAG);
                    needsRepaint = true;
                } else if (guiEvent.keyCode == KeyCode.R) {
                    guiEvent.Use();
                    StartAction(EditAction.ROTATE);
                    needsRepaint = true;
                } else if (guiEvent.keyCode == KeyCode.S) {
                    guiEvent.Use();
                    StartAction(EditAction.SCALE);
                    needsRepaint = true;
                }
            }*/

            // TODO: overrule focus command too!
            // + maybe zoom then also? (zoom to cursor?)

            // Overrule middle mouse button with orbit & move!
            if((guiEvent.type == EventType.MouseDown || guiEvent.type == EventType.MouseDrag || guiEvent.type == EventType.MouseUp) && (guiEvent.button == 1 || guiEvent.button == 2)) {
                // Remap Orbit logic
                // Orbit by default
                // Move with shift
                if(guiEvent.modifiers == EventModifiers.Shift) {
                    guiEvent.button = 2;
                    guiEvent.keyCode = KeyCode.Mouse2;
                    guiEvent.modifiers = EventModifiers.None;
                } else {
                    guiEvent.button = 0;
                    guiEvent.keyCode = KeyCode.Mouse0;
                    guiEvent.modifiers = EventModifiers.Alt;
                    Tools.viewTool = ViewTool.Orbit;
                }
                // Hack. Set the button in the Tools (to be able to activate Orbit!)
                FieldInfo field = typeof(Tools).GetField("s_ButtonDown", BindingFlags.NonPublic | BindingFlags.Static);
                if(field != null) {
                    field.SetValue(null, guiEvent.button);
                }
            }
        } else {
            if(guiEvent.type == EventType.KeyDown) {
                if(guiEvent.keyCode == KeyCode.X) {
                    guiEvent.Use();
                    if(guiEvent.modifiers == EventModifiers.Shift || currentEditAction == EditAction.ROTATE) {
                        ToggleAxis(ViewAxes.YZ);
                    } else {
                        ToggleAxis(ViewAxes.X);
                    }
                    needsRepaint = true;
                }
                if(guiEvent.keyCode == KeyCode.Z) {
                    guiEvent.Use();
                    if(guiEvent.modifiers == EventModifiers.Shift || currentEditAction == EditAction.ROTATE) {
                        ToggleAxis(ViewAxes.XZ);
                    } else {
                        ToggleAxis(ViewAxes.Y);
                    }
                    needsRepaint = true;
                }
                if(guiEvent.keyCode == KeyCode.Y) {
                    guiEvent.Use();
                    if(guiEvent.modifiers == EventModifiers.Shift || currentEditAction == EditAction.ROTATE) {
                        ToggleAxis(ViewAxes.XY);
                    } else {
                        ToggleAxis(ViewAxes.Z);
                    }
                    needsRepaint = true;
                }
            }

            // In an action, repaint every frame
            if(guiEvent.type == EventType.MouseMove || needsRepaint) {
                // Apply the changes!
                if(currentEditAction == EditAction.GRAB) {
                    // Move objects
                    Undo.RecordObjects(transforms, "Quick Move (Grab)");

                    // Restore transforms
                    RestoreTransformsOngoing();

                    // Prepare global axes
                    Vector3 axisA = Vector3.zero;
                    Vector3 axisB = Vector3.zero;
                    Vector3 pivotPoint = startCenterHandlePosition;
                    bool hasSecondAxis = true;
                    if(currentAxisMode == ViewAxesMode.VIEW) {
                        CalculateAxesToView(out axisA, out axisB);
                    } else if(currentAxisMode == ViewAxesMode.GLOBAL) {
                        CalculateAxesGlobal(out axisA, out axisB, out hasSecondAxis);
                    } else if(currentAxisMode == ViewAxesMode.LOCAL) {
                        CalculateAxesLocal(startCenterHandleRotation, out axisA, out axisB, out hasSecondAxis);
                    }

                    // Prepare rays
                    Ray startRay = HandleUtility.GUIPointToWorldRay(startMousePosition);
                    Ray currentRay = HandleUtility.GUIPointToWorldRay(mousePosition);

                    // Find shift
                    Vector2 startShift = FindPosition(startRay, pivotPoint, axisA, axisB, hasSecondAxis);
                    Vector2 currentShift = FindPosition(currentRay, pivotPoint, axisA, axisB, hasSecondAxis);

                    Vector2 actualShift = currentShift - startShift;

                    foreach(Transform t in transforms) {
                        // Prepare local axes
                        if(currentAxisMode == ViewAxesMode.LOCAL && Tools.pivotMode == PivotMode.Pivot) {
                            CalculateAxesLocal(t.rotation, out axisA, out axisB, out hasSecondAxis);
                        }

                        // Shift is along the axes
                        t.position = t.position + actualShift.x * axisA + actualShift.y * axisB;
                    }

                    Undo.CollapseUndoOperations(startUndoGroup);
                }
                if (currentEditAction == EditAction.ROTATE) {
                    // Move objects
                    Undo.RecordObjects(transforms, "Quick Move (Rotate)");

                    // Restore transforms
                    RestoreTransformsOngoing();

                    // Prepare global axes
                    Vector3 axisA = Vector3.zero;
                    Vector3 axisB = Vector3.zero;
                    Vector3 pivotPoint = startCenterHandlePosition;
                    bool hasSecondAxis = true;
                    if(currentAxisMode == ViewAxesMode.VIEW) {
                        CalculateAxesToView(out axisA, out axisB);
                    } else if(currentAxisMode == ViewAxesMode.GLOBAL) {
                        CalculateAxesGlobal(out axisA, out axisB, out hasSecondAxis);
                    } else if(currentAxisMode == ViewAxesMode.LOCAL) {
                        CalculateAxesLocal(startCenterHandleRotation, out axisA, out axisB, out hasSecondAxis);
                    }

                    // Prepare rays
                    Ray startRay = HandleUtility.GUIPointToWorldRay(startMousePosition);
                    Ray currentRay = HandleUtility.GUIPointToWorldRay(mousePosition);

                    // Find shift
                    Vector2 startShift = FindPosition(startRay, pivotPoint, axisA, axisB, hasSecondAxis).normalized;
                    Vector2 currentShift = FindPosition(currentRay, pivotPoint, axisA, axisB, hasSecondAxis).normalized;

                    // Convert to angle!
                    float angleBefore = Mathf.Atan2(startShift.y, startShift.x);
                    float angleAfter = Mathf.Atan2(currentShift.y, currentShift.x);
                    float angleShift = (angleAfter - angleBefore) * Mathf.Rad2Deg;

                    foreach(Transform t in transforms) {
                        // Prepare local axes
                        if(currentAxisMode == ViewAxesMode.LOCAL && Tools.pivotMode == PivotMode.Pivot) {
                            CalculateAxesLocal(t.rotation, out axisA, out axisB, out hasSecondAxis);
                        }

                        // Shift is along the axes
                        Vector3 normal = Vector3.Cross(axisA, axisB);
                        Vector3 pivot = t.position;
                        if(Tools.pivotMode == PivotMode.Center) {
                            pivot = startCenterHandlePosition;
                        }
                        t.RotateAround(pivot, normal, angleShift);
                    }

                    Undo.CollapseUndoOperations(startUndoGroup);
                }
                if (currentEditAction == EditAction.SCALE) {
                    // Move objects
                    Undo.RecordObjects(transforms, "Quick Move (Scale)");

                    // Restore transforms
                    RestoreTransformsOngoing();

                    // Prepare axes
                    Vector3 axisA = Vector3.zero;
                    Vector3 axisB = Vector3.zero;
                    CalculateAxesToView(out axisA, out axisB);
                    Vector3 pivotPoint = startCenterHandlePosition;

                    // Prepare rays
                    Ray startRay = HandleUtility.GUIPointToWorldRay(startMousePosition);
                    Ray currentRay = HandleUtility.GUIPointToWorldRay(mousePosition);

                    // Find shift
                    Vector2 startShift = FindPosition(startRay, pivotPoint, axisA, axisB, true);
                    Vector2 currentShift = FindPosition(currentRay, pivotPoint, axisA, axisB, true);

                    // Convert to distance!
                    float scaleFactor = (currentShift.x*axisA + currentShift.y*axisB).magnitude / (startShift.x*axisA + startShift.y*axisB).magnitude;

                    // Now, handle the axis
                    if(currentAxisMode == ViewAxesMode.VIEW) {
                        // 3D scaling
                        foreach(Transform t in transforms) {
                            if(Tools.pivotMode == PivotMode.Center) {
                                t.position = pivotPoint + (t.position - pivotPoint) * scaleFactor;
                            }
                            t.localScale *= scaleFactor;
                        }
                    } else {
                        // 2D or 1D scaling
                        bool hasSecondAxis = false;
                        if(currentAxisMode == ViewAxesMode.GLOBAL) {
                            CalculateAxesGlobal(out axisA, out axisB, out hasSecondAxis);
                        } else if(currentAxisMode == ViewAxesMode.LOCAL) {
                            CalculateAxesLocal(startCenterHandleRotation, out axisA, out axisB, out hasSecondAxis);
                        }

                        foreach(Transform t in transforms) {
                            // Prepare local axes
                            if(currentAxisMode == ViewAxesMode.LOCAL && Tools.pivotMode == PivotMode.Pivot) {
                                CalculateAxesLocal(t.rotation, out axisA, out axisB, out hasSecondAxis);
                            }
                            Vector3 crossAxis = Vector3.zero;
                            if(hasSecondAxis) {
                                crossAxis = Vector3.Cross(axisA, axisB);
                            }

                            // Scale along the axes
                            // 1. Position
                            if(Tools.pivotMode == PivotMode.Center) {
                                Vector3 diff = t.position - pivotPoint;
                                t.position = pivotPoint + ScaleVector(diff, scaleFactor, axisA, axisB, crossAxis, hasSecondAxis);
                            }

                            // Scale object itself (lossy scale - as not possible to do the "real thing")
                            Vector3 localRight = t.rotation * Vector3.right;
                            Vector3 localUp = t.rotation * Vector3.up;
                            Vector3 localForward = t.rotation * Vector3.forward;
                            float scaleFactorRight = Vector3.Dot(localRight, ScaleVector(localRight, scaleFactor, axisA, axisB, crossAxis, hasSecondAxis));
                            float scaleFactorUp = Vector3.Dot(localUp, ScaleVector(localUp, scaleFactor, axisA, axisB, crossAxis, hasSecondAxis));
                            float scaleFactorForward = Vector3.Dot(localForward, ScaleVector(localForward, scaleFactor, axisA, axisB, crossAxis, hasSecondAxis));
                            t.localScale = new Vector3(t.localScale.x * scaleFactorRight, t.localScale.y * scaleFactorUp, t.localScale.z * scaleFactorForward);
                        }
                    }

                    Undo.CollapseUndoOperations(startUndoGroup);
                }

                needsRepaint = true;
            }
            if(guiEvent.type == EventType.KeyDown || guiEvent.type == EventType.KeyUp) {
                if(guiEvent.keyCode == KeyCode.Escape) {
                    guiEvent.Use();
                    // Exit action without saving
                    ExitWithoutSaving();
                }
            }
            if(guiEvent.type == EventType.MouseUp) {
                if(guiEvent.modifiers == EventModifiers.None) {
                    if(guiEvent.button == 0) {
                        guiEvent.Use();
                        // Save
                        ExitWithSaving();
                    } else if(guiEvent.button == 1) {
                        guiEvent.Use();
                        // Exit action without saving
                        ExitWithoutSaving();
                    }
                }
            }
        }
    }

    private void Draw() {
        if(currentEditAction != EditAction.NONE) {
            // Draw axes
            Vector3 axisA = Vector3.zero;
            Vector3 axisB = Vector3.zero;
            bool hasSecondAxis = false;
            Color axisAColor = Color.white;
            Color axisBColor = Color.white;
            CalculateAxesColors(out axisAColor, out axisBColor);
            bool draw = false;
            if(currentAxisMode == ViewAxesMode.GLOBAL) {
                CalculateAxesGlobal(out axisA, out axisB, out hasSecondAxis);
                draw = true;
            } else if (currentAxisMode == ViewAxesMode.LOCAL) {
                draw = true;
            }

            if(draw) {
                Vector3 pivotPoint = startCenterHandlePosition;
                if(Tools.pivotMode == PivotMode.Center || (/* global drag = always center pivot */ currentAxisMode == ViewAxesMode.GLOBAL && currentEditAction == EditAction.GRAB)) {
                    if (currentAxisMode == ViewAxesMode.LOCAL) {
                        CalculateAxesLocal(startCenterHandleRotation, out axisA, out axisB, out hasSecondAxis);
                    }

                    // Draw axes
                    Handles.color = axisAColor;
                    Handles.DrawLine(pivotPoint + axisA * - AXISDRAWSIZE, pivotPoint + axisA * AXISDRAWSIZE);

                    if(hasSecondAxis) {
                        Handles.color = axisBColor;
                        Handles.DrawLine(pivotPoint + axisB * - AXISDRAWSIZE, pivotPoint + axisB * AXISDRAWSIZE);
                    }
                } else {
                    foreach(Transform t in transforms) {
                        // Prepare local axes
                        if(currentAxisMode == ViewAxesMode.LOCAL) {
                            //if (Tools.pivotMode == PivotMode.Pivot) {
                            CalculateAxesLocal(t.rotation, out axisA, out axisB, out hasSecondAxis);
                            pivotPoint = t.position;
                        }
                        pivotPoint = t.position;

                        // Draw axes
                        Handles.color = axisAColor;
                        Handles.DrawLine(pivotPoint + axisA * - AXISDRAWSIZE, pivotPoint + axisA * AXISDRAWSIZE);

                        if(hasSecondAxis) {
                            Handles.color = axisBColor;
                            Handles.DrawLine(pivotPoint + axisB * - AXISDRAWSIZE, pivotPoint + axisB * AXISDRAWSIZE);
                        }
                    }
                }
            }
        }

        // Draw grab handle
        if(currentEditAction == EditAction.GRAB) {
            DrawCursor();
        }
    }

    private void DrawCursor() {
        // Draw grab handle
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(mousePosition - Vector2.one*iconGrabSize/2);
        Vector3 mousePosition3D = mouseRay.GetPoint(1);
        
        //Debug.Log(mousePosition3D);
        var content = new GUIContent(iconGrab);
        GUIStyle style = new GUIStyle();
        style.fixedHeight = iconGrabSize;
        //HandleUtility.GetHandleSize(mousePosition3D)
        Handles.Label(mousePosition3D, content, style);
    }
}