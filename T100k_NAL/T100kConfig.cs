namespace T100k_NAL
{
    /// <summary>
    /// Configuration struct for the T100k Controller Network Abstraction Layer
    /// </summary>
    public struct T100kConfig
    {
        /// <summary>
        /// A flag indicating whether the output interface should check the computers IP Address for the allowed
        /// master address (192.168.60.178). 
        /// By default this should be set to true to fight networking problems, but in some scenarios
        /// like proxies etc. it can be useful to be set to false.
        /// </summary>
        public bool StrictIPChecking;

        /// <summary>
        /// How many controllers you plan to use within the network.
        /// This is used for performance optimizations and a more responsive startup.
        /// By default, this is set to 128
        /// </summary>
        public byte MaximumControllerID;

        /// <summary>
        /// How many frames should be at least sent per second.
        /// However, if data is updated, even more frames are sent, to ensure immediate updates.
        /// 0 to 60, default: 4
        /// </summary>
        public byte Framerate;

        /// <summary>
        /// Gets the default configuration for the NAL.
        /// </summary>
        /// <value>The default configuration.</value>
        public static T100kConfig Default
        {
            get
            {
                return new T100kConfig { MaximumControllerID = 128, StrictIPChecking = true, Framerate = 4 };
            }
        }
    }
}

