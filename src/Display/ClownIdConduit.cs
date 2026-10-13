using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ClownID
{
    /// <summary>
    /// In viewports using the Clown ID display mode: suppresses Rhino's own object drawing
    /// and redraws each object's render meshes in one flat, unlit colour.
    /// </summary>
    internal class ClownIdConduit : DisplayConduit
    {
        readonly Guid _modeId;
        readonly Dictionary<Guid, DisplayMaterial> _materials = new Dictionary<Guid, DisplayMaterial>();
        readonly List<RhinoObject> _pending = new List<RhinoObject>();

        // Meshes per object, kept between frames. Asking Rhino for them every frame hands
        // back new mesh objects, and the graphics card then gets every triangle uploaded
        // again on every frame: orbiting was ~9x slower than Shaded. Keyed by runtime serial
        // number, which changes whenever an object is replaced (moved, edited).
        readonly Dictionary<uint, (Mesh Mesh, Transform Xform)[]> _meshes = new Dictionary<uint, (Mesh, Transform)[]>();

        public ClownIdConduit(Guid modeId)
        {
            _modeId = modeId;
            RhinoDoc.DeleteRhinoObject += (s, e) => Forget(e.TheObject);
            RhinoDoc.ModifyObjectAttributes += (s, e) => Forget(e.RhinoObject); // per-object mesh settings
            RhinoDoc.InstanceDefinitionTableEvent += (s, e) => _meshes.Clear(); // block edits change instances
            RhinoDoc.DocumentPropertiesChanged += (s, e) => _meshes.Clear();    // render mesh quality
            RhinoDoc.CloseDocument += (s, e) => _meshes.Clear();
        }

        bool IsActive(RhinoViewport viewport) => viewport.DisplayMode?.Id == _modeId;

        void Forget(RhinoObject obj)
        {
            if (obj != null)
                _meshes.Remove(obj.RuntimeSerialNumber);
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            if (!IsActive(e.Viewport))
                return;
            _pending.Clear();
            // Paint over background, grid and axes. Rhino does not reliably apply the
            // mode's solid fill in every view, so do it here.
            var b = e.Viewport.Bounds;
            e.Display.Draw2dRectangle(new Rectangle(-2, -2, b.Width + 4, b.Height + 4), ClownColors.Background, 0, ClownColors.Background);
        }

        protected override void PreDrawObject(DrawObjectEventArgs e)
        {
            if (!IsActive(e.Viewport))
                return;
            e.DrawObject = false;
            // Rhino also calls this for the parts of each block instance; the instance draws them.
            var obj = e.RhinoObject;
            if (obj != null && !obj.IsInstanceDefinitionGeometry)
                _pending.Add(obj);
        }

        // Drawn here rather than in PreDrawObject: there the pipeline keeps the first
        // material of the frame for every object.
        protected override void PostDrawObjects(DrawEventArgs e)
        {
            if (!IsActive(e.Viewport))
                return;
            foreach (var obj in _pending)
            {
                var material = MaterialFor(obj.Id);
                foreach (var (mesh, xform) in MeshesFor(obj))
                {
                    if (xform.IsIdentity)
                    {
                        e.Display.DrawMeshShaded(mesh, material);
                        continue;
                    }
                    e.Display.PushModelTransform(xform);
                    e.Display.DrawMeshShaded(mesh, material);
                    e.Display.PopModelTransform();
                }
            }
            _pending.Clear();
        }

        (Mesh Mesh, Transform Xform)[] MeshesFor(RhinoObject obj)
        {
            if (_meshes.TryGetValue(obj.RuntimeSerialNumber, out var cached))
                return cached;
            var items = new List<(Mesh, Transform)>();
            bool complete = CollectRenderMeshes(obj, Transform.Identity, items);
            var result = items.ToArray();
            // Keep it only once Rhino has meshed everything; until then ask again next frame.
            if (complete)
                _meshes[obj.RuntimeSerialNumber] = result;
            return result;
        }

        DisplayMaterial MaterialFor(Guid id)
        {
            if (_materials.TryGetValue(id, out var material))
                return material;
            if (_materials.Count > 100000)
            {
                foreach (var m in _materials.Values)
                    m.Dispose();
                _materials.Clear();
            }
            // Emission only: the colour reaches the frame buffer unchanged by lighting.
            var c = ClownColors.For(id);
            material = new DisplayMaterial
            {
                Diffuse = Color.Black,
                Specular = Color.Black,
                Shine = 0,
                Emission = c,
                IsTwoSided = true,
                BackDiffuse = Color.Black,
                BackEmission = c,
            };
            _materials[id] = material;
            return material;
        }

        /// <summary>
        /// Render meshes of an object; block instances recurse with their transforms.
        /// Returns false when a part that should have meshes has none yet.
        /// </summary>
        static bool CollectRenderMeshes(RhinoObject obj, Transform xform, List<(Mesh, Transform)> items)
        {
            if (obj is InstanceObject instance)
            {
                var definition = instance.InstanceDefinition;
                if (definition == null)
                    return true;
                var t = xform * instance.InstanceXform;
                bool complete = true;
                foreach (var part in definition.GetObjects())
                    complete &= CollectRenderMeshes(part, t, items);
                return complete;
            }
            var meshes = obj.GetMeshes(MeshType.Render);
            if (meshes == null || meshes.Length == 0)
                return !obj.IsMeshable(MeshType.Render);
            foreach (var mesh in meshes)
                items.Add((mesh, xform));
            return true;
        }
    }
}
