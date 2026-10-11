using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Display;

namespace ClownId
{
    /// <summary>Toggles the active viewport between Clown ID and the mode it had before.</summary>
    public class ClownIdCommand : Command
    {
        readonly Dictionary<Guid, Guid> _previousModes = new Dictionary<Guid, Guid>();

        public ClownIdCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static ClownIdCommand Instance { get; private set; }

        public override string EnglishName => "ClownId";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var view = doc.Views.ActiveView;
            if (view == null)
                return Result.Failure;
            var clown = ClownDisplayMode.Ensure();
            if (clown == null)
                return Result.Failure;

            var viewport = view.ActiveViewport;
            if (viewport.DisplayMode?.Id == clown.Id)
            {
                var previous = _previousModes.TryGetValue(viewport.Id, out var id) ? DisplayModeDescription.GetDisplayMode(id) : null;
                viewport.DisplayMode = previous ?? DisplayModeDescription.GetDisplayMode(DisplayModeDescription.ShadedId);
                RhinoApp.WriteLine("Clown ID off in {0}.", viewport.Name);
            }
            else
            {
                if (viewport.DisplayMode != null)
                    _previousModes[viewport.Id] = viewport.DisplayMode.Id;
                viewport.DisplayMode = clown;
                RhinoApp.WriteLine("Clown ID on in {0}. Run ClownId again to switch back.", viewport.Name);
            }
            view.Redraw();
            return Result.Success;
        }
    }
}
