using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ClownId
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

        public ClownIdConduit(Guid modeId)
        {
            _modeId = modeId;
        }

        bool IsActive(RhinoViewport viewport) => viewport.DisplayMode?.Id == _modeId;

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
                foreach (var (mesh, xform) in RenderMeshes(obj, Transform.Identity))
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

        /// <summary>Render meshes of an object; block instances recurse with their transforms.</summary>
        static IEnumerable<(Mesh, Transform)> RenderMeshes(RhinoObject obj, Transform xform)
        {
            if (obj is InstanceObject instance)
            {
                var definition = instance.InstanceDefinition;
                if (definition == null)
                    yield break;
                var t = xform * instance.InstanceXform;
                foreach (var part in definition.GetObjects())
                    foreach (var item in RenderMeshes(part, t))
                        yield return item;
                yield break;
            }
            var meshes = obj.GetMeshes(MeshType.Render);
            if (meshes == null)
                yield break;
            foreach (var mesh in meshes)
                yield return (mesh, xform);
        }
    }
}
