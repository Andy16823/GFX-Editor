using LibGFX;
using LibGFX.Assets;
using LibGFX.Audio;
using LibGFX.Compute;
using LibGFX.Core;
using LibGFX.Core.GameElements;
using LibGFX.Graphics;
using LibGFX.Graphics.Enviroment;
using LibGFX.Graphics.Lights;
using LibGFX.Graphics.Materials;
using LibGFX.Graphics.Primitives;
using LibGFX.Graphics.Shader;
using LibGFX.Math;
using LibGFX.Physics;
using LibGFX.UI;
using NewGFXEditor.Editor;
using NewGFXEditor.Exporter;
using NewGFXEditor.Nodes;
using NewGFXEditor.Viewmodel;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.CodeDom;
using System.Diagnostics;
using System.Security.Authentication.ExtendedProtection;
using System.Windows.Forms;
using static OpenTK.Graphics.OpenGL.GL;
using static System.Formats.Asn1.AsnWriter;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace NewGFXEditor
{
    /// <summary>
    /// Main form for the 3D graphics editor application.
    /// </summary>
    public partial class Form1 : Form
    {
        public Camera SelectedCamera { get; set; }
        public PerspectiveCamera PerspectiveCamera { get; set; }
        public Scene3D Scene { get; set; }
        public EditorPanel3D Editor { get => _editorPanel3D; }
        public bool ShowGrid { get; set; } = true;
        public bool PickLights { get; set; } = false;
        public AssetManager AssetManager { get; set; }
        public Gizmo TransformGizmo { get; set; }
        public string WorkingDirectory { get; set; }

        private EditorPanel3D _editorPanel3D;
        private bool _dragCamera = false;
        private Vector2 _mousePos;
        private GameElement _selectedElement = null;
        private bool _sceneEnabled = true;
        private ComputeRaycast _computeRaycast;
        private TreeBuilder _treeBuilder;
        private Transform _debugSphereTransform = new Transform();
        private bool _showDebugSphere = false;

        /// <summary>
        /// Initializes a new instance of the Form1 class and sets up the 3D editor panel and related event handlers.
        /// </summary>
        /// <remarks>This constructor configures the main form by initializing UI components, creating the
        /// 3D editor panel, and subscribing to relevant editor events. It also loads startup assets and updates the GUI
        /// to ensure the form is ready for user interaction after construction.</remarks>
        public Form1()
        {
            InitializeComponent();

            // Create the asset manager and load default assets
            this.AssetManager = new AssetManager();

            // Setup editor panel and callbacks
            _editorPanel3D = new EditorPanel3D(this.editorHostPanel);
            _editorPanel3D.EditorLoaded += EditorPanel3D_EditorLoaded;
            _editorPanel3D.OnKeyDown += EditorPanel3D_OnKeyDown;
            _editorPanel3D.BeforeRender += EditorPanel3D_BeforeRender;
            _editorPanel3D.OnRender += EditorPanel3D_OnRender;
            _editorPanel3D.AfterRender += EditorPanel3D_AfterRender;
            _editorPanel3D.OnMouseDown += EditorPanel3D_OnMouseDown;
            _editorPanel3D.OnMouseMove += EditorPanel3D_OnMouseMove;
            _editorPanel3D.OnMouseUp += EditorPanel3D_OnMouseUp;
            _editorPanel3D.OnResized += _editorPanel3D_OnResized;

            // Setup cameras
            this.PerspectiveCamera = new PerspectiveCamera(new Vector3(0, 5, -10), new Vector2(800, 600));
            PerspectiveCamera.LookAt(new Vector3(0, 0, 0));
            SelectedCamera = PerspectiveCamera;

            // Setup debug sphere
            _debugSphereTransform.Position = new Vector3(0, 0, 0);
            _debugSphereTransform.Scale = new Vector3(0.2f);

            // Load Gizmos
            TransformGizmo = new Gizmo("Assets/Gizmos/Transform/TransformGizmo.obj");
            TransformGizmo.GizmoUpdated += TransformGizmo_GizmoUpdated;
            TransformGizmo.GizmoMoved += TransformGizmo_GizmoMoved;
            TransformGizmo.GizmoScaled += TransformGizmo_GizmoScaled;
            TransformGizmo.GizmoRotated += TransformGizmo_GizmoRotated;

            // Setup tree builder
            _treeBuilder = new TreeBuilder();
            _treeBuilder.AddBuilder(new SceneNodeBuilder());
            _treeBuilder.AddBuilder(new MaterialNodeBuilder());
            _treeBuilder.AddBuilder(new MeshNodeBuilder());
            _treeBuilder.AddBuilder(new ModelNodeBuilder());
            _treeBuilder.AddBuilder(new LightNodeBuilder());
        }

        /// <summary>
        /// Resets the current scene to a new, empty state and clears all selections in the editor.
        /// </summary>
        /// <remarks>Call this method to discard the existing scene and start with a blank workspace. All
        /// current selections and transformations are cleared, and the editor interface is updated to reflect the new
        /// scene state.</remarks>
        public void NewScene()
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            MessageBox.Show("Select a working directory for the new scene.");
            if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
            {
                this.WorkingDirectory = folderBrowserDialog.SelectedPath;
            }

            // Create or clear the scene
            if (this.Scene == null)
            {
                // Create a new scene if none exists usually on startup
                var scene3d = new Scene3D();
                scene3d.DirectionalLight = new DirectionalLight3D(new Vector3(-0.2f, 1.0f, -0.3f), new Vector4(0.4f, 0.4f, 0.4f, 1.0f), 1.0f);
                scene3d.OnRenderPassEnd += Scene3d_OnRenderPassEnd;
                Scene = scene3d;
            }
            else
            {
                _selectedElement = null;
                TransformGizmo.Enabled = false;
                this.AssetManager.DisposeAssets(_editorPanel3D.Renderer);
                this.AssetManager.ClearAssets();
                this.Scene.FreeScene(_editorPanel3D.Renderer);
                this.Scene.DirectionalLight = new DirectionalLight3D(new Vector3(-0.2f, 1.0f, -0.3f), new Vector4(0.4f, 0.4f, 0.4f, 1.0f), 1.0f);
                this.Scene.InitLights(_editorPanel3D.Renderer);
                this.UpdateGUI();
                _editorPanel3D.Redraw();
            }

            // Create default materials
            var blankMaterial = new SGMaterial("e_BlankMaterial", Vector4.One);
            AssetManager.Add<SGMaterial>(blankMaterial);
            var debugMaterial = new SGMaterial("e_DebugMaterial", new Vector4(1, 0, 0, 1));
            AssetManager.Add<SGMaterial>(debugMaterial);
            AssetManager.InitializeAssets(_editorPanel3D.Renderer);
        }

        public void DeleteElement(GameElement element)
        {
            if (element != null)
            {
                element.Dispose(this.Scene, _editorPanel3D.Renderer);
                this.Scene.RemoveElement(element);

                if (element == _selectedElement)
                {
                    TransformGizmo.Enabled = false;
                    _selectedElement = null;
                }
            }
        }

        /// <summary>
        /// Displays a dialog that allows the user to save the current scene to a GFX level file.
        /// </summary>
        /// <remarks>If the user selects a file and confirms the dialog, the current scene is exported to
        /// the specified file in the GFX level format. If the user cancels the dialog, no file is saved.</remarks>
        public void SaveSceneAs()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "GFX Level Files|*.gfxlevel";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                GFXExporter exporter = new GFXExporter();
                SerializationContext context = new SerializationContext();
                exporter.ExportScene(sfd.FileName, this.Scene as Scene3D, AssetManager, context);
            }
        }

        /// <summary>
        /// Loads a scene from a GFX level file selected by the user through an open file dialog. 
        /// The current scene is cleared and replaced with the loaded scene.
        /// </summary>
        public void OpenScene()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "GFX Scene File |*.gfxlevel";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Clear the current scene and assets before loading the new scene
                _sceneEnabled = false;
                AssetManager.DisposeAssets(_editorPanel3D.Renderer);
                AssetManager.ClearAssets();
                Scene.FreeScene(_editorPanel3D.Renderer);

                // Load the new scene from the selected file
                var exporter = new GFXExporter();
                var context = new SerializationContext();
                var shaders = _editorPanel3D.Renderer.GetAllRenderShaders();
                foreach (var kvp in shaders)
                {
                    context.SetValue(kvp.Key, kvp.Value);
                }
                exporter.ImportScene(openFileDialog.FileName, this.Scene as Scene3D, AssetManager, context);

                // Re-initialize Scene and Assets after loading
                AssetManager.InitializeAssets(_editorPanel3D.Renderer);
                Scene.InitializeElements(_editorPanel3D.Viewport, _editorPanel3D.Renderer);
                Scene.InitLights(_editorPanel3D.Renderer);
                _sceneEnabled = true;
                _editorPanel3D.Redraw();
                this.UpdateGUI();
            }
        }

        /// <summary>
        /// Handles the resize event for the 3D editor panel and updates the camera resolution to match the new viewport
        /// size.
        /// </summary>
        /// <param name="sender">The source of the event, typically the 3D editor panel that was resized.</param>
        /// <param name="viewport">The viewport representing the new dimensions of the 3D editor panel after resizing.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void _editorPanel3D_OnResized(object sender, Viewport viewport, EventArgs e)
        {
            if (this.SelectedCamera is PerspectiveCamera pc)
            {
                pc.Resolution = new Vector2(viewport.Width, viewport.Height);
            }
        }

        /// <summary>
        /// Creates a cube primitive with the specified position, scale, rotation, and material.
        /// </summary>
        /// <param name="position">The position of the cube in world coordinates.</param>
        /// <param name="scale">The scale of the cube along each axis.</param>
        /// <param name="rotation">The rotation of the cube, specified in degrees for each axis.</param>
        /// <param name="material">The material to apply to the cube. Cannot be null.</param>
        /// <returns>A <see cref="Primitive"/> instance representing the created cube with the specified transformation and
        /// material.</returns>
        public Primitive CreateCube(Vector3 position, Vector3 scale, Vector3 rotation, SGMaterial material)
        {
            var cubeMesh = AssetManager.Get<Mesh>("Primitive_Cube");
            if (cubeMesh == null)
            {
                cubeMesh = Cube.GetMesh();
                cubeMesh.Init(_editorPanel3D.Renderer);
                AssetManager.Add<Mesh>("Primitive_Cube", cubeMesh);
            }
            var cube = new Primitive("Cube", cubeMesh, material);

            // Set the cube's transform properties
            cube.Transform.Position = position;
            cube.Transform.Scale = scale;
            cube.Transform.Rotate(rotation);
            return cube;
        }

        /// <summary>
        /// Creates a new sphere primitive with the specified position, scale, rotation, and material.
        /// </summary>
        /// <param name="position">The position of the sphere in world coordinates.</param>
        /// <param name="scale">The scale to apply to the sphere along each axis.</param>
        /// <param name="rotation">The rotation to apply to the sphere, specified as Euler angles in degrees.</param>
        /// <param name="material">The material to assign to the sphere. Cannot be null.</param>
        /// <returns>A new <see cref="Primitive"/> instance representing the created sphere.</returns>
        public Primitive CreateSphere(Vector3 position, Vector3 scale, Vector3 rotation, SGMaterial material)
        {
            var sphereMesh = AssetManager.Get<Mesh>("Primitive_Sphere");
            if (sphereMesh == null)
            {
                sphereMesh = LibGFX.Graphics.Generator.SphereGenerator.CreateSphere();
                sphereMesh.Init(_editorPanel3D.Renderer);
                AssetManager.Add<Mesh>("Primitive_Sphere", sphereMesh);
            }
            var sphere = new Primitive("Sphere", sphereMesh, material);

            // Set the sphere's transform properties
            sphere.Transform.Position = position;
            sphere.Transform.Scale = scale;
            sphere.Transform.Rotate(rotation);
            return sphere;
        }

        /// <summary>
        /// Creates a quad primitive with the specified position, scale, rotation, and material.
        /// </summary>
        /// <param name="position">The position of the quad in world space coordinates.</param>
        /// <param name="scale">The scale to apply to the quad along each axis.</param>
        /// <param name="rotation">The rotation to apply to the quad, specified in degrees for each axis.</param>
        /// <param name="material">The material to apply to the quad. Cannot be null.</param>
        /// <returns>A new <see cref="Primitive"/> instance representing the configured quad.</returns>
        public Primitive CreateQuad(Vector3 position, Vector3 scale, Vector3 rotation, SGMaterial material)
        {
            var quadMesh = AssetManager.Get<Mesh>("Primitive_Quad");
            if (quadMesh == null)
            {
                quadMesh = Quad.GetMesh();
                quadMesh.Init(_editorPanel3D.Renderer);
                AssetManager.Add<Mesh>("Primitive_Quad", quadMesh);
            }
            var quad = new Primitive("Quad", quadMesh, material);

            // Set the quad's transform properties
            quad.Transform.Position = position;
            quad.Transform.Scale = scale;
            quad.Transform.Rotate(rotation);
            return quad;
        }

        /// <summary>
        /// Handles the MouseUp event for the 3D editor panel, ending camera drag operations and releasing the transform
        /// gizmo.
        /// </summary>
        /// <param name="sender">The source of the event, typically the 3D editor panel control.</param>
        /// <param name="e">A MouseEventArgs that contains the event data for the mouse button release.</param>
        private void EditorPanel3D_OnMouseUp(object sender, MouseEventArgs e)
        {
            _dragCamera = false;
            this.TransformGizmo.ReleaseGizmo();
        }

        /// <summary>
        /// Handles the MouseMove event for the 3D editor panel, updating gizmo highlighting, object manipulation, or
        /// camera rotation based on the current interaction state.
        /// </summary>
        /// <remarks>This method enables interactive manipulation of objects or the camera within the 3D
        /// editor view. Depending on the current tool and user action, it may highlight gizmo axes, move or scale
        /// objects, or rotate the camera in response to mouse movement.</remarks>
        /// <param name="sender">The source of the event, typically the 3D editor panel control.</param>
        /// <param name="e">A MouseEventArgs that contains the event data, including the current mouse position.</param>
        private void EditorPanel3D_OnMouseMove(object sender, MouseEventArgs e)
        {
            // Highlight the gizmo based on mouse position
            TransformGizmo.HighlightGizmo((PerspectiveCamera)SelectedCamera, _editorPanel3D.Viewport, e.X, e.Y);

            // Move the gizmo or rotate the camera
            bool setNewMousePos = false;
            if (TransformGizmo.ActiveAxis != GizmoActiveAxis.None)
            {
                if (TransformGizmo.Type == GizmoType.Translation)
                {
                    TransformGizmo.MoveAlongAxis2D((PerspectiveCamera)SelectedCamera, _editorPanel3D.Viewport, (int)_mousePos.X, (int)_mousePos.Y, e.X, e.Y);
                }
                else if (TransformGizmo.Type == GizmoType.Scale)
                {
                    TransformGizmo.ScaleAlongAxis((PerspectiveCamera)SelectedCamera, _editorPanel3D.Viewport, (int)_mousePos.X, (int)_mousePos.Y, e.X, e.Y);
                }
                else if (TransformGizmo.Type == GizmoType.Rotation)
                {
                    TransformGizmo.RotateAlongAxis((PerspectiveCamera)SelectedCamera, _editorPanel3D.Viewport, (int)_mousePos.X, (int)_mousePos.Y, e.X, e.Y);
                }
                setNewMousePos = true;
            }

            // Rotate the camera
            if (_dragCamera)
            {
                var delataX = e.X - _mousePos.X;
                var delataY = e.Y - _mousePos.Y;
                SelectedCamera.Transform.Rotate(new Vector3(-delataY * 0.1f, -delataX * 0.1f, 0.0f));
                setNewMousePos = true;
            }

            // Update the mouse position
            if (setNewMousePos)
            {
                _mousePos = new Vector2(e.X, e.Y);
            }
        }

        /// <summary>
        /// Handles mouse down events on the 3D editor panel to initiate camera dragging or select scene elements and
        /// gizmos.
        /// </summary>
        /// <remarks>Right mouse button presses begin camera drag operations. Left mouse button presses
        /// attempt to pick and select a gizmo or scene element at the mouse position. If a scene element is selected,
        /// its properties and transform gizmo are updated accordingly.</remarks>
        /// <param name="sender">The source of the event, typically the 3D editor panel.</param>
        /// <param name="e">A MouseEventArgs that contains the event data, including mouse button and position information.</param>
        private void EditorPanel3D_OnMouseDown(object sender, MouseEventArgs e)
        {
            // Start dragging the camera
            if (e.Button == MouseButtons.Right)
            {
                if (!_dragCamera)
                {
                    _dragCamera = true;
                    _mousePos = new Vector2(e.X, e.Y);
                }
            }

            // Pick gizmo or scene element
            if (e.Button == MouseButtons.Left)
            {

                if (this.TransformGizmo.Enabled)
                {
                    var gizmoPicked = this.TransformGizmo.PickGizmo((PerspectiveCamera)SelectedCamera, _editorPanel3D.Viewport, e.X, e.Y);
                    if (gizmoPicked)
                    {
                        _mousePos = new Vector2(e.X, e.Y);
                        TransformGizmo.PrepareForAction();
                    }
                    else
                    {
                        var pickedElement = PickElement((PerspectiveCamera)SelectedCamera, Scene.GetAllElements(), e.X, e.Y, _editorPanel3D.Viewport);
                        if (pickedElement != null)
                        {
                            _selectedElement = pickedElement;

                            // Update the element view model and property grid
                            var elementViewModel = new ElementViewModel(pickedElement);
                            this.propertyGrid1.SelectedObject = elementViewModel;

                            //this.propertyGrid1.SelectedObject = _elementViewModel;
                            this.TransformGizmo.Transform.Position = _selectedElement.Transform.Position;
                            this.TransformGizmo.Enabled = true;
                        }
                        else
                        {
                            _selectedElement = null;
                            this.propertyGrid1.SelectedObject = null;
                            this.TransformGizmo.Enabled = false;
                        }
                    }
                }
                else
                {
                    var pickedElement = PickElement((PerspectiveCamera)SelectedCamera, Scene.GetAllElements(), e.X, e.Y, _editorPanel3D.Viewport);
                    if (pickedElement != null)
                    {
                        _selectedElement = pickedElement;

                        var elementViewModel = new ElementViewModel(pickedElement);
                        this.propertyGrid1.SelectedObject = elementViewModel;

                        this.TransformGizmo.Transform.Position = _selectedElement.Transform.Position;
                        this.TransformGizmo.Enabled = true;
                    }
                    else
                    {
                        _selectedElement = null;
                        this.propertyGrid1.SelectedObject = null;
                        this.TransformGizmo.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Pick an element from the scene based on the mouse position and camera view.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="elements"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="viewport"></param>
        /// <returns></returns>
        private GameElement? PickElement(PerspectiveCamera camera, IEnumerable<GameElement> elements, int x, int y, Viewport viewport)
        {
            // Set min distance to max value to detect closest hit
            var minDist = float.MaxValue;
            GameElement hitElement = null;

            // Create a ray from the screen point
            var ray = MeshRaycast.ScreenPointToWorldRay(camera, viewport, x, y);

            // Iterate through all elements and check for intersection
            foreach (var element in elements)
            {
                // Check against light handles if enabled
                if (this.PickLights && element.GetType() == typeof(PointLight3DHandle))
                {
                    // Check against AABB since lights have no meshes
                    if (MeshRaycast.IntersectsAABB(ray, element.WorldAABB, out float min, out float max))
                    {
                        // Check if this is the closest hit
                        if (min < minDist)
                        {
                            // Update closest hit
                            minDist = min;
                            hitElement = element;
                        }
                    }
                }
                else
                {
                    // Check against meshes
                    var meshes = element.GetMeshes();
                    if (meshes == null)
                    {
                        continue;
                    }

                    // Check each mesh for intersection
                    foreach (var mesh in meshes)
                    {
                        var hit = _computeRaycast.PerformRaycast(ray, element.Transform, mesh);
                        if (hit.hit == true && hit.hitDistance < minDist)
                        {
                            minDist = hit.hitDistance;
                            _debugSphereTransform.Position = hit.hitLocation;
                            hitElement = element;
                        }
                    }
                }
            }
            return hitElement;
        }

        /// <summary>
        /// Handles the event that occurs before the 3D editor panel is rendered, updating the renderer's viewport to
        /// match the current control size.
        /// </summary>
        /// <param name="sender">The source of the event, typically the 3D editor panel control.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void EditorPanel3D_BeforeRender(object sender, EventArgs e)
        {
            _editorPanel3D.Renderer.SetViewport(new Viewport(_editorPanel3D.GLControl.Width, _editorPanel3D.GLControl.Height));
            _editorPanel3D.Renderer.SetViewMatrix(this.SelectedCamera.GetViewMatrix());
            _editorPanel3D.Renderer.SetProjectionMatrix(this.SelectedCamera.GetProjectionMatrix(_editorPanel3D.Viewport));
        }

        /// <summary>
        /// Handles the render event for the 3D editor panel, updating the scene and drawing visual elements such as the
        /// scene contents, transform gizmo, and optional bounding boxes.
        /// </summary>
        /// <remarks>This method is typically invoked as part of the rendering loop for the 3D editor
        /// panel. If the option to display axis-aligned bounding boxes (AABBs) is enabled, the method will render
        /// bounding boxes for all scene elements to assist with visualization and debugging.</remarks>
        /// <param name="sender">The source of the event, typically the 3D editor panel control.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void EditorPanel3D_OnRender(object sender, EventArgs e)
        {
            if (this.Scene == null)
            {
                return;
            }

            // Render the scene
            if (_sceneEnabled)
            {
                this.Scene.BuildShadowMaps(_editorPanel3D.Viewport, _editorPanel3D.Renderer, this.PerspectiveCamera);
                this.Scene.Render(_editorPanel3D.Viewport, _editorPanel3D.Renderer, SelectedCamera);
                _editorPanel3D.Renderer.DrawRenderTarget(Scene.RenderTarget as LibGFX.Graphics.RenderTarget.MSAARenderTarget2D, 0);
            }

            // Render the transform gizmo
            this.TransformGizmo.RenderGizmo(_editorPanel3D.Renderer, SelectedCamera, _editorPanel3D.Viewport);

            // Render selected element AABB
            if (_selectedElement != null)
            {
                var aabb = _selectedElement.WorldAABB;
                _editorPanel3D.Renderer.DrawAABB(aabb, ColorPresets.LightCyan);
            }

            // Render all AABBs
            if (showAABBsToolStripMenuItem.Checked)
            {
                this.Scene.ForEachElement(element =>
                {
                    if (element == _selectedElement)
                    {
                        return;
                    }
                    var aabb = element.WorldAABB;
                    _editorPanel3D.Renderer.DrawAABB(aabb, ColorPresets.LimeGreen);
                });
            }

            if (_showDebugSphere)
            {
                _editorPanel3D.Renderer.DrawPrimitive(_debugSphereTransform, LibGFX.Graphics.Primitives.PrimitiveType.Sphere, ColorPresets.Magenta);
            }
        }

        /// <summary>
        /// Handles the event that occurs after the 3D editor panel has completed rendering.
        /// </summary>
        /// <param name="sender">The source of the event, typically the 3D editor panel.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void EditorPanel3D_AfterRender(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles key down events for the 3D editor panel, enabling camera movement and element deletion based on user
        /// input.
        /// </summary>
        /// <remarks>Pressing the W, A, S, or D keys moves the camera in the corresponding direction.
        /// Pressing the Delete key removes the currently selected element from the scene if one is selected.</remarks>
        /// <param name="sender">The source of the event, typically the 3D editor panel control.</param>
        /// <param name="e">A KeyEventArgs that contains the event data, including information about the pressed key.</param>
        private void EditorPanel3D_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W)
            {
                var front = SelectedCamera.Transform.Forward * 0.1f;
                SelectedCamera.Transform.Position += front;
            }
            else if (e.KeyCode == Keys.S)
            {
                var back = SelectedCamera.Transform.Forward * 0.1f;
                SelectedCamera.Transform.Position -= back;
            }

            if (e.KeyCode == Keys.A)
            {
                var right = SelectedCamera.Transform.GetRightFlat() * 0.1f;
                SelectedCamera.Transform.Position -= right;
            }
            else if (e.KeyCode == Keys.D)
            {
                var right = SelectedCamera.Transform.GetRightFlat() * 0.1f;
                SelectedCamera.Transform.Position += right;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                if (_selectedElement != null)
                {
                    this.DeleteElement(_selectedElement);
                    //this.propertyGrid1.SelectedObject = null;
                    this.UpdateGUI();
                    _editorPanel3D.Redraw();
                }
            }
        }

        private void TransformGizmo_GizmoUpdated(Gizmo obj)
        {
            if (this.propertyGrid1.SelectedObject is ElementViewModel elementViewModel)
            {
                elementViewModel.Refresh();
                this.propertyGrid1.Refresh();
            }
        }

        /// <summary>
        /// Scene render pass end event handler that draws a grid in the scene if the ShowGrid property is enabled.
        /// </summary>
        /// <param name="scene">The scene being rendered.</param>
        /// <param name="viewport">The viewport through which the scene is viewed.</param>
        /// <param name="renderer">The rendering device used to draw the scene.</param>
        /// <param name="camera">The camera used to view the scene.</param>
        private void Scene3d_OnRenderPassEnd(BaseScene scene, Viewport viewport, IRenderDevice renderer, Camera camera)
        {
            if (this.ShowGrid)
            {
                renderer.DrawGrid(camera, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
            }
        }

        /// <summary>
        /// Transform Gizmo rotation event handler that applies rotation to the selected element based on the active axis and the provided rotation factor.
        /// </summary>
        /// <param name="rotationFactor"></param>
        private void TransformGizmo_GizmoRotated(float rotationFactor)
        {
            if (_selectedElement != null)
            {
                float rotMultiplier = 5.0f;
                switch (this.TransformGizmo.ActiveAxis)
                {
                    case GizmoActiveAxis.None:
                        break;
                    case GizmoActiveAxis.X:
                        _selectedElement.Transform.Rotate(new Vector3(rotationFactor * rotMultiplier, 0, 0));
                        break;
                    case GizmoActiveAxis.Y:
                        _selectedElement.Transform.Rotate(new Vector3(0, rotationFactor * rotMultiplier, 0));
                        break;
                    case GizmoActiveAxis.Z:
                        _selectedElement.Transform.Rotate(new Vector3(0, 0, rotationFactor * rotMultiplier));
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Handles scaling of the selected gizmo element along the currently active axis by the specified scale factor.
        /// </summary>
        /// <remarks>This method applies scaling only if an element is currently selected. The scaling is
        /// performed along the axis that is active in the transform gizmo. After scaling, the 3D editor panel is
        /// redrawn to reflect the changes.</remarks>
        /// <param name="scaleFactor">The amount by which to scale the selected element along the active axis. Positive values increase the size;
        /// negative values decrease it.</param>
        private void TransformGizmo_GizmoScaled(float scaleFactor)
        {
            Debug.WriteLine($"Gizmo scaled with factor: {scaleFactor}");
            if (_selectedElement != null)
            {
                if (TransformGizmo.ActiveAxis == GizmoActiveAxis.X)
                {
                    _selectedElement.Transform.Scale += new Vector3(scaleFactor, 0, 0);
                }
                else if (TransformGizmo.ActiveAxis == GizmoActiveAxis.Y)
                {
                    _selectedElement.Transform.Scale += new Vector3(0, scaleFactor, 0);
                }
                else if (TransformGizmo.ActiveAxis == GizmoActiveAxis.Z)
                {
                    _selectedElement.Transform.Scale += new Vector3(0, 0, scaleFactor);
                }
                _editorPanel3D.Redraw();
            }
        }

        /// <summary>
        /// Event handler for when the transform gizmo is moved.
        /// </summary>
        /// <param name="newPosition"></param>
        private void TransformGizmo_GizmoMoved(Vector3 newPosition)
        {
            if (_selectedElement != null)
            {
                _selectedElement.Transform.Position = newPosition;
            }
        }

        /// <summary>
        /// Updates the GUI elements such as the scene tree, material list view, and layers combobox.
        /// </summary>
        private void UpdateGUI()
        {
            this.treeView1.Nodes.Clear();
            this.treeView1.Nodes.Add(_treeBuilder.Build(this.Scene, this.AssetManager));
            this.treeView1.ExpandAll();
        }

        /// <summary>
        /// Event handler for when the editor panel is loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorPanel3D_EditorLoaded(object sender, EventArgs e)
        {
            // Create a new scene on startup
            this.NewScene();

            //var computeShader = AssetManager.Load<ComputeShader>("Resources/Shader/Raycaster.glsl");
            //_computeRaycast = new ComputeRaycast(computeShader);
            _computeRaycast = new ComputeRaycast();

            // Initialize the materials within the asset manager
            AssetManager.InitializeAssets(_editorPanel3D.Renderer);

            // Initilize the scene
            this.Scene.Init(_editorPanel3D.Viewport, _editorPanel3D.Renderer);

            // Initialize the transform gizmo
            TransformGizmo.Init(_editorPanel3D.Renderer, _editorPanel3D.Viewport);
            _computeRaycast.Init(_editorPanel3D.Renderer);

            this.UpdateGUI();
        }

        /// <summary>
        /// Handles the event that occurs when the form is loaded.
        /// </summary>
        /// <param name="sender">The source of the event, typically the form being loaded.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the KeyDown event for the form and processes key press information.
        /// </summary>
        /// <param name="sender">The source of the event, typically the form that received the key press.</param>
        /// <param name="e">A KeyEventArgs that contains the event data for the key press.</param>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine(e.KeyCode.ToString());
        }

        /// <summary>
        /// Handles the AfterSelect event of the TreeView control to display the selected node's associated object in
        /// the property grid.
        /// </summary>
        /// <param name="sender">The source of the event, typically the TreeView control.</param>
        /// <param name="e">A TreeViewEventArgs that contains the event data for the selected node.</param>
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (this.treeView1.SelectedNode != null)
            {
                var selectedElement = this.treeView1.SelectedNode.Tag;
                if (selectedElement != null)
                {
                    if (selectedElement is GameElement entity)
                    {
                        _selectedElement = entity;

                        var elementViewModel = new ElementViewModel(entity);
                        this.propertyGrid1.SelectedObject = elementViewModel;

                        this.TransformGizmo.Transform.Position = _selectedElement.Transform.Position;
                        this.TransformGizmo.Enabled = true;
                        this._editorPanel3D.Redraw();

                    }
                    else
                    {
                        _selectedElement = null;
                        this.propertyGrid1.SelectedObject = null;
                        this.TransformGizmo.Enabled = false;
                        this._editorPanel3D.Redraw();
                    }
                }
            }
        }

        /// <summary>
        /// Imports a material from the specified file path and optionally displays the material editor for further
        /// editing.
        /// </summary>
        /// <remarks>If the imported material is missing any of its standard textures (diffuse, normal, or
        /// specular), default textures are assigned automatically. When showEditor is false, the method updates the
        /// material thumbnail and list view instead of opening the editor.</remarks>
        /// <param name="path">The file path to the material asset to import. Must refer to a valid material file.</param>
        /// <param name="showEditor">true to display the material editor after importing; otherwise, false to import the material without opening
        /// the editor. The default is true.</param>
        public void ImportMaterial(String path, bool showEditor = true)
        {
            // TODO: Implement material importing
            //var material = AssetManager.Load<SGMaterial>(path);
            //if (material != null)
            //{
            //    if (material.DiffuseTexture == null)
            //    {
            //        material.DiffuseTexture = new Texture(1, 1, new Vector4i(255, 255, 255, 255));
            //    }

            //    if (material.NormalTexture == null)
            //    {
            //        material.NormalTexture = new Texture(1, 1, new Vector4i(128, 128, 255, 255));
            //    }

            //    if (material.SpecularTexture == null)
            //    {
            //        material.SpecularTexture = new Texture(1, 1, new Vector4i(0, 0, 0, 255));
            //    }


            //    material.Init(_editorPanel3D.Renderer);
            //    if (showEditor)
            //    {
            //        var materialEditor = new MaterialEditor(material);
            //        materialEditor.Show();
            //    }
            //}
        }

        /// <summary>
        /// Handles the Click event of the ImportScene Material menu item and initiates the process of importing material
        /// from a selected file.
        /// </summary>
        /// <param name="sender">The source of the event, typically the ImportScene Material menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void importMaterialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                this.ImportMaterial(openFileDialog.FileName);
            }
        }

        /// <summary>
        /// Creatres a new material and opens the material editor for it.
        /// </summary>
        public void CreateMaterial()
        {
            var materialCount = AssetManager.GetAssetCount<SGMaterial>();
            var blankMaterial = new SGMaterial($"e_NewMaterial_{materialCount + 1}", Vector4.One);

            var materialEditor = new MaterialEditor(blankMaterial);
            if (materialEditor.ShowDialog() == DialogResult.OK)
            {
                blankMaterial.Init(_editorPanel3D.Renderer);
                AssetManager.Add<SGMaterial>(blankMaterial.Name, blankMaterial);
            }
        }

        /// <summary>
        /// Handles the Click event of the Material Editor menu item and initiates the material editor workflow.
        /// </summary>
        /// <param name="sender">The source of the event, typically the Material Editor menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void materialEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.CreateMaterial();
        }

        /// <summary>
        /// Handles the DragDrop event for the material list view, importing files that are dropped onto the control.
        /// </summary>
        /// <param name="sender">The source of the event, typically the material list view control.</param>
        /// <param name="e">A DragEventArgs that contains the event data, including information about the dropped files.</param>
        private void materialListView_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    ImportMaterial(file, false);
                }
            }
        }

        /// <summary>
        /// Handles the DragEnter event for the material list view to determine whether the dragged data contains files
        /// and sets the appropriate drag-and-drop effect.
        /// </summary>
        /// <param name="sender">The source of the event, typically the material list view control.</param>
        /// <param name="e">A DragEventArgs that contains the event data, including information about the data being dragged.</param>
        private void materialListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        /// <summary>
        /// Handles the FormClosing event to release resources and perform cleanup before the form is closed.
        /// </summary>
        /// <remarks>This method ensures that assets, scene resources, and rendering components are
        /// properly disposed of when the form is closing. It should be attached to the FormClosing event of the form to
        /// prevent resource leaks.</remarks>
        /// <param name="sender">The source of the event, typically the form being closed.</param>
        /// <param name="e">A FormClosingEventArgs that contains the event data, including the reason for closing and the ability to
        /// cancel the event.</param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _computeRaycast.Dispose(_editorPanel3D.Renderer);
            AssetManager.DisposeAssets(_editorPanel3D.Renderer);
            Scene.DisposeScene(_editorPanel3D.Renderer);
            TransformGizmo.Dispose(_editorPanel3D.Renderer);
            _editorPanel3D.Renderer.Dispose();
        }

        /// <summary>
        /// Handles the Click event of the Cube menu item to add a new cube primitive to the current scene.
        /// </summary>
        /// <param name="sender">The source of the event, typically the Cube menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void cubeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var primitive = this.CreateCube(new Vector3(0, 0, 0), new Vector3(1, 1, 1), Vector3.Zero, AssetManager.Get<SGMaterial>("e_BlankMaterial"));
            primitive.Init(Scene, _editorPanel3D.Viewport, _editorPanel3D.Renderer);
            Scene.AddGameElement(primitive);
            this.UpdateGUI();
            this.Editor.Redraw();
        }

        /// <summary>
        /// Handles the Click event of the Sphere menu item to add a new sphere primitive to the current scene.
        /// </summary>
        /// <param name="sender">The source of the event, typically the Sphere menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void sphereToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var primitive = this.CreateSphere(new Vector3(0, 0, 0), new Vector3(1, 1, 1), Vector3.Zero, AssetManager.Get<SGMaterial>("e_BlankMaterial"));
            primitive.Init(Scene, _editorPanel3D.Viewport, _editorPanel3D.Renderer);
            Scene.AddGameElement(primitive);
            this.UpdateGUI();
        }

        /// <summary>
        /// Handles the Click event of the Quad menu item to add a new quad primitive to the current scene.
        /// </summary>
        /// <param name="sender">The source of the event, typically the Quad menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void quadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var primitive = this.CreateQuad(new Vector3(0, 0, 0), new Vector3(1, 1, 1), Vector3.Zero, AssetManager.Get<SGMaterial>("e_BlankMaterial"));
            primitive.Init(Scene, _editorPanel3D.Viewport, _editorPanel3D.Renderer);
            Scene.AddGameElement(primitive);
            this.UpdateGUI();
        }

        /// <summary>
        /// Handles the Click event of the Edit Position menu item, allowing the user to modify the position of the
        /// currently selected element.
        /// </summary>
        /// <remarks>If no element is currently selected, this handler does nothing. After a successful
        /// edit, the 3D editor panel is refreshed to reflect the updated position.</remarks>
        /// <param name="sender">The source of the event, typically the Edit Position menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void editPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedElement != null)
            {
                var vec3Editor = new Vec3Editor(_selectedElement.Transform.Position);
                if (vec3Editor.ShowDialog() == DialogResult.OK)
                {
                    _selectedElement.Transform.Position = vec3Editor.Value;
                    _editorPanel3D.Redraw();
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Edit Rotation menu item, allowing the user to modify the rotation of the
        /// currently selected element.
        /// </summary>
        /// <remarks>This method displays a rotation editor dialog for the selected element. If the user
        /// confirms the changes, the element's rotation is updated and the 3D editor panel is redrawn to reflect the
        /// modification. If no element is selected, the method does nothing.</remarks>
        /// <param name="sender">The source of the event, typically the Edit Rotation menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void editRotationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedElement != null)
            {
                var vec3Editor = new Vec3Editor(_selectedElement.Transform.GetEulerAngles());
                if (vec3Editor.ShowDialog() == DialogResult.OK)
                {
                    _selectedElement.Transform.Rotate(vec3Editor.Value);
                    _editorPanel3D.Redraw();
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Edit Scale menu item, allowing the user to modify the scale of the currently
        /// selected 3D element.
        /// </summary>
        /// <remarks>If no element is selected, this handler does nothing. After a successful scale edit,
        /// the 3D editor panel is refreshed to reflect the changes.</remarks>
        /// <param name="sender">The source of the event, typically the Edit Scale menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void editScaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedElement != null)
            {
                var vec3Editor = new Vec3Editor(_selectedElement.Transform.Scale);
                if (vec3Editor.ShowDialog() == DialogResult.OK)
                {
                    _selectedElement.Transform.Scale = vec3Editor.Value;
                    _editorPanel3D.Redraw();
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Translate gizmo mode button and sets the transform gizmo to translation mode.
        /// </summary>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void gizmoModeTranslateBtn_Click(object sender, EventArgs e)
        {
            TransformGizmo.Type = GizmoType.Translation;
            gizmoModeTranslateBtn.Checked = true;
            gizmoModeScaleBtn.Checked = false;
            gizmoModeRotateBtn.Checked = false;
        }

        /// <summary>
        /// Handles the Click event of the Scale gizmo mode button, setting the transform gizmo to Scale mode.
        /// </summary>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void gizmoModeScaleBtn_Click(object sender, EventArgs e)
        {
            TransformGizmo.Type = GizmoType.Scale;
            gizmoModeTranslateBtn.Checked = false;
            gizmoModeScaleBtn.Checked = true;
            gizmoModeRotateBtn.Checked = false;
        }

        /// <summary>
        /// Handles the Click event of the third tool strip button and sets the transform gizmo to rotation mode.
        /// </summary>
        /// <param name="sender">The source of the event, typically the tool strip button that was clicked.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void gizmoModeRotateBtn_Click(object sender, EventArgs e)
        {
            this.TransformGizmo.Type = GizmoType.Rotation;
            gizmoModeTranslateBtn.Checked = false;
            gizmoModeScaleBtn.Checked = false;
            gizmoModeRotateBtn.Checked = true;
        }

        /// <summary>
        /// Handles the Click event for the Model menu item, allowing the user to import a 3D model file into the
        /// current scene.
        /// </summary>
        /// <remarks>Supported model file formats include OBJ, FBX, GLTF, and GLB. The imported model is
        /// added to the currently selected scene layer and displayed in the 3D editor panel.</remarks>
        /// <param name="sender">The source of the event, typically the Model menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void modelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Model Files|*.obj;*.fbx;*.gltf;*.glb";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var staticMeshModel = AssetManager.Load<StaticMeshModel>(ofd.FileName);
                if (!staticMeshModel.IsInitialized)
                {
                    staticMeshModel.Init(_editorPanel3D.Renderer);
                    Debug.WriteLine($"Initializing new StaticMeshModel from {ofd.FileName}");
                }
                else
                {
                    Debug.WriteLine($"Using cached StaticMeshModel from {ofd.FileName}");
                }

                var model = new StaticModel("Model", staticMeshModel);
                Scene.AddGameElement(model);
                model.Init(Scene, _editorPanel3D.Viewport, _editorPanel3D.Renderer);
                _editorPanel3D.Redraw();
                this.UpdateGUI();
            }
        }

        /// <summary>
        /// Handles the Click event of the Create menu item to initiate the creation process.
        /// </summary>
        /// <param name="sender">The source of the event, typically the Create menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void createToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the Click event of the Delete menu item to remove the currently selected element from the scene.
        /// </summary>
        /// <remarks>If no element is selected when the menu item is clicked, no action is taken. After
        /// deletion, the user interface is updated to reflect the change.</remarks>
        /// <param name="sender">The source of the event, typically the Delete menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedElement != null)
            {
                this.DeleteElement(_selectedElement);
            }
            this.UpdateGUI();
        }

        /// <summary>
        /// Handles the Click event of the Show AABBs menu item to update the 3D editor panel display.
        /// </summary>
        /// <param name="sender">The source of the event, typically the Show AABBs menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void showAABBsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _editorPanel3D.Redraw();
        }

        /// <summary>
        /// Handles the Click event of the Add String menu item, allowing the user to add a new string property to the
        /// currently selected element.
        /// </summary>
        /// <remarks>If no element is selected when the menu item is clicked, an error message is
        /// displayed and no property is added.</remarks>
        /// <param name="sender">The source of the event, typically the Add String menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void addStringToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedElement != null)
            {
                var stringPropertyDialog = new Dialogs.StringProperty("Key", "Value");
                if (stringPropertyDialog.ShowDialog() == DialogResult.OK)
                {
                    var data = stringPropertyDialog.Data;
                    _selectedElement.Properties.Add(data.Name, data.Value);
                }
            }
            else
            {
                MessageBox.Show("No element selected to add string property.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event of the Add Collision Type menu item, allowing the user to add a collision type
        /// property to the currently selected element.
        /// </summary>
        /// <remarks>If no element is selected when the menu item is clicked, an error message is
        /// displayed and no action is taken.</remarks>
        /// <param name="sender">The source of the event, typically the Add Collision Type menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void addCollisionTypeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedElement != null)
            {
                var collisionTypePropertyDialog = new Dialogs.CollisionTypeProperty(String.Empty);
                if (collisionTypePropertyDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedElement.Properties.Add("CollisionType", collisionTypePropertyDialog.CollisionType);
                }
            }
            else
            {
                MessageBox.Show("No element selected to add collision type property.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event for the GFX Level File menu item, allowing the user to export the current 3D scene
        /// to a GFX Level file.
        /// </summary>
        /// <remarks>Displays a Save File dialog for the user to specify the destination file. If a file
        /// is selected, exports the current scene to the specified GFX Level file format.</remarks>
        /// <param name="sender">The source of the event, typically the menu item that was clicked.</param>
        /// <param name="e">An EventArgs instance containing event data.</param>
        private void gFXLevelFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSceneAs();
        }

        /// <summary>
        /// Handles the Click event for the GFX Level File menu item and initiates the process of opening a scene.
        /// </summary>
        /// <param name="sender">The source of the event, typically the menu item that was clicked.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void gFXLevelFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenScene();
        }

        /// <summary>
        /// Handles the Click event of the neuToolStripButton control and releases resources associated with the current
        /// 3D scene.
        /// </summary>
        /// <param name="sender">The source of the event, typically the neuToolStripButton control.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void neuToolStripButton_Click(object sender, EventArgs e)
        {
            NewScene();
        }

        /// <summary>
        /// Handles the Click event of the Öffnen menu item to initiate the process of opening a scene.
        /// </summary>
        /// <param name="sender">The source of the event, typically the Öffnen menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void öffnenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenScene();
        }

        /// <summary>
        /// Handles the Click event of the 'Save As' menu item to prompt the user to save the current scene under a new
        /// file name.
        /// </summary>
        /// <param name="sender">The source of the event, typically the 'Save As' menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void speichernunterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSceneAs();
        }

        /// <summary>
        /// Handles the Click event of the Show Grid menu item, toggling the visibility of the grid in the 3D editor
        /// panel.
        /// </summary>
        /// <remarks>This method updates both the grid visibility in the editor and the checked state of
        /// the menu item to reflect the current setting.</remarks>
        /// <param name="sender">The source of the event, typically the Show Grid menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void showGridToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ShowGrid = !this.ShowGrid;
            _editorPanel3D.Redraw();
            showGridToolStripMenuItem.Checked = this.ShowGrid;
        }

        /// <summary>
        /// Handles the Click event of the 'New' menu item to create a new scene.
        /// </summary>
        /// <param name="sender">The source of the event, typically the 'New' menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void neuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewScene();
        }

        /// <summary>
        /// Handles the Click event of the Point Light menu item by creating and adding a new point light to the current
        /// scene.
        /// </summary>
        /// <remarks>This method initializes a new point light with predefined position and color
        /// settings, adds it to the selected scene layer, and updates its bounding box. Use this handler to insert a
        /// point light into the 3D editor environment via the menu.</remarks>
        /// <param name="sender">The source of the event, typically the menu item that was clicked.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void pointLightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pointLight = new PointLight3DHandle("PointLight", new Vector3(-2f, 1f, 0f), new Vector4(0.0f, 0.8f, 0.0f, 1.0f), 4f, 30f);
            pointLight.Init(Scene, _editorPanel3D.Viewport, _editorPanel3D.Renderer);
            Scene.AddGameElement(pointLight);
            pointLight.ComputeAABB();
            this.UpdateGUI();
        }

        /// <summary>
        /// Handles the Click event of the Light Picking menu item, toggling the light picking mode in the application.
        /// </summary>
        /// <param name="sender">The source of the event, typically the Light Picking menu item.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void lightPickingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.PickLights = !this.PickLights;
            lightPickingToolStripMenuItem.Checked = this.PickLights;
        }

        /// <summary>
        /// Enables or disables snapping for the transform gizmo when the corresponding toolbar button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            this.TransformGizmo.EnableSnapping = !this.TransformGizmo.EnableSnapping;
            toolStripButton1.Checked = this.TransformGizmo.EnableSnapping;
        }

        private void orthogalToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the Click event for the light settings menu item, allowing the user to edit the settings of a
        /// selected point light element.
        /// </summary>
        /// <remarks>If no element is selected or the selected element is not a point light, an error
        /// message is displayed to the user. When a point light is selected, a dialog is shown to edit its settings. If
        /// the dialog is confirmed, the 3D editor panel is redrawn to reflect any changes.</remarks>
        /// <param name="sender">The source of the event, typically the menu item that was clicked.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void lightSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedElement != null)
            {
                if (_selectedElement is PointLight3DHandle light)
                {
                    var pointLightDialog = new Dialogs.PointLightDialog(light.LightSource);
                    if (pointLightDialog.ShowDialog() == DialogResult.OK)
                    {
                        _editorPanel3D.Redraw();
                    }
                }
                else
                {
                    MessageBox.Show("Selected element is not a light.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No element selected to edit light settings.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void reloadUToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UpdateGUI();
        }

        private void gFXAssetsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void showRaycastPointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _showDebugSphere = !_showDebugSphere;
            showRaycastPointToolStripMenuItem.Checked = _showDebugSphere;
            _editorPanel3D.Redraw();
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (_selectedElement != null)
            {
                TransformGizmo.Transform.Position = _selectedElement.Transform.Position;
            }
            _editorPanel3D.Redraw();
        }

        private void propertyGrid1_SelectedObjectsChanged(object sender, EventArgs e)
        {
            this.propertyGrid1.ExpandAllGridItems();
        }

        private void cubeMeshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new CreateDialog("Create Cube Mesh", "CubeMesh");
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var name = dialog.Value;
                var mesh = Cube.GetMesh();
                mesh.Init(_editorPanel3D.Renderer);
                AssetManager.Add<Mesh>(name, mesh);
                MessageBox.Show($"Cube Mesh '{name}' created and added to assets.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.UpdateGUI();
            }
        }

        private void quadMeshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new CreateDialog("Create Quad Mesh", "QuadMesh");
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var name = dialog.Value;
                var mesh = Quad.GetMesh();
                mesh.Init(_editorPanel3D.Renderer);
                AssetManager.Add<Mesh>(name, mesh);
                MessageBox.Show($"Quad Mesh '{name}' created and added to assets.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.UpdateGUI();
            }
        }

        private void sphereMeshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new CreateDialog("Create Sphere Mesh", "SphereMesh");
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var name = dialog.Value;
                var mesh = LibGFX.Graphics.Generator.SphereGenerator.CreateSphere();
                mesh.Init(_editorPanel3D.Renderer);
                AssetManager.Add<Mesh>(name, mesh);
                MessageBox.Show($"Sphere Mesh '{name}' created and added to assets.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.UpdateGUI();
            }
        }

        private void kopierenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Implement copy functionality for the selected element
            //if (this._selectedElement != null)
            //{
            //    var clone = _selectedElement.clone();
            //    if (clone != null)
            //    {
            //        clone.Name = $"{_selectedElement.Name}_Copy";
            //        clone.Init(Scene, _editorPanel3D.Viewport, _editorPanel3D.Renderer);
            //        Scene.AddGameElement(clone);
            //        this.UpdateGUI();
            //        this.Editor.Redraw();
            //    }
            //}
        }

        private void öffnenToolStripButton_Click(object sender, EventArgs e)
        {

        }
    }
}
