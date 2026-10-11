using Rhino;
using Rhino.PlugIns;

namespace ClownId
{
    ///<summary>
    /// Clown ID: draws every object as one flat, unlit colour derived from its id
    /// whenever a viewport uses the "Clown ID" display mode.
    ///</summary>
    public class ClownIdPlugin : PlugIn
    {
        public ClownIdPlugin()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the ClownIdPlugin plug-in.</summary>
        public static ClownIdPlugin Instance { get; private set; }

        internal ClownIdConduit Conduit { get; private set; }

        // Load with Rhino so the display mode is never selected without the conduit behind it.
        public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            var mode = ClownDisplayMode.Ensure();
            if (mode == null)
            {
                errorMessage = "Could not create the \"" + ClownDisplayMode.Name + "\" display mode.";
                return LoadReturnCode.ErrorShowDialog;
            }
            Conduit = new ClownIdConduit(mode.Id) { Enabled = true };
            return LoadReturnCode.Success;
        }

        protected override void OnShutdown()
        {
            if (Conduit != null)
                Conduit.Enabled = false;
        }
    }
}
