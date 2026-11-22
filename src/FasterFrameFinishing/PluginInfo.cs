namespace FasterFrameFinishing
{
    internal static class PluginInfo
    {
        /// <summary>
        /// Must equal &lt;ModID&gt; in About/About.xml. StationeersLaunchPad names the injected config
        /// file after it: BepInEx/config/stationeers.fasterframefinishing.cfg. Checked by tests.
        /// </summary>
        public const string ModId = "stationeers.fasterframefinishing";
        public const string Name = "Faster Frame Finishing";

        /// <summary>Must equal &lt;Version&gt; in About/About.xml. Checked by tests.</summary>
        public const string Version = "1.2.0";
    }
}
