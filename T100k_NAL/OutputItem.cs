using System;
using Utils;

namespace T100k_NAL
{
    /// <summary>
    /// This struct represents one changed channel.
    /// </summary>
    public struct OutputItem
    {
        /// <summary>
        /// The id of the controller.
        /// </summary>
        public byte ControllerID;

        /// <summary>
        /// The universe where the LEDs start (output port).
        /// </summary>
        public byte UniverseID;

        /// <summary>
        /// The channel within for the LED in the universe.
        /// </summary>
        public ushort Channel;

        /// <summary>
        /// The color to set this channel to.
        /// </summary>
        public RGBColor Color;
    }
}

