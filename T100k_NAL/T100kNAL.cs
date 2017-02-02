using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using Utils;
using log4net;

namespace T100k_NAL
{
    /// <summary>
    /// This class represents the output interfacing side for the controllers.
    /// It can control the led controllers T100-K using LPD6803 led controllers (3*5bit)
    /// The channel order is RGB, the transmission is implemented according to my reverse engineering efforts.
    /// </summary>
    public class T100kNAL
    {
        /// <summary>
        /// Internal helper class to abstract one controller.
        /// </summary>
        class Controller
        {
            #region Constants
            /// <summary>
            /// The port where the controller listens to data.
            /// </summary>
            const int TARGET_PORT = 5000;
            /// <summary>
            /// How many leds can be controlled per universe.
            /// </summary>
            const int LEDS_PER_UNIVERSE = 1024;
            /// <summary>
            /// How many leds are controlled per network packet.
            /// </summary>
            const int LEDS_PER_PACKET = 64;
            /// <summary>
            /// How many bytes in the network packet are needed to control one led.
            /// </summary>
            const int BYTES_PER_LED = 16;
            /// <summary>
            /// How many bits can be used per color.
            /// </summary>
            const int BITS_PER_COLOR = 5;
            /// <summary>
            /// The offset of the red color in the packet.
            /// </summary>
            const int RED_OFFSET = 1;
            /// <summary>
            /// The offset of the blue color in the packet.
            /// </summary>
            const int GREEN_OFFSET = RED_OFFSET + BITS_PER_COLOR;
            /// <summary>
            /// The offset of the blue color in the packet.
            /// </summary>
            const int BLUE_OFFSET = GREEN_OFFSET + BITS_PER_COLOR;
            /// <summary>
            /// A seperator byte between the single leds.
            /// This has to be set to 0xFF to force the controller to actually send the value
            /// of the pixels out using the specified port.
            /// </summary>
            const byte LED_SEPARATOR = 0xff;
            /// <summary>
            /// How many universes a controller has.
            /// </summary>
            const byte NUM_UNIVERSES = 8;

            #endregion

            #region Static Fields
            /// <summary>
            /// this packet must be sent when starting a transmission
            /// </summary> 
            public static byte[] StartPacket = { 0xc5, 0x77, 0x88, 0x00, 0x00 };
            /// <summary>
            /// this packet must be sent when continuing the transmission to the controller
            /// </summary>
            public static byte[] ContinuationPacket = { 0xaa, 0x00, 0x66, 0x00, 0x00 };
            #endregion

            #region Fields
            /// <summary>
            /// The IP Address of the controller. It has the form
            /// 192.168.60.{50+ControllerID} and can not be changed.
            /// </summary>
            readonly IPAddress controllerAddress;
            /// <summary>
            /// These byte arrays represent the current sending data.
            /// Each "universe" consists of 16 packets at 1040 byte length each.
            /// Those have to be sent successively.
            /// </summary>
            public byte[][] Packets = new byte[16][];
            #endregion

            /// <summary>
            /// The controller ID. This must match the dip switches on the controllers.
            /// </summary>
            /// <value>The controller identifier.</value>
            public byte ControllerId { get; }
            /// <summary>
            /// The remote endpoint of the controller, consists of {controllerAddress:TARGET_PORT}
            /// </summary>
            /// <value>The remote ip address of the controller</value>
            public IPEndPoint RemoteEndPoint { get; }

            /// <summary>
            /// Occurs when the data for this controller has changed and it needs a refresh.
            /// </summary>
            public event Action<int> RefreshController;

            /// <summary>
            /// Creates a new instance of the controller.
            /// </summary>
            /// <param name="id">The controller ID. Must be smaller than 128.</param>
            public Controller(byte id)
            {
                ControllerId = id;
                controllerAddress = new IPAddress(new byte[] { 192, 168, 60, (byte)(50 + id) });
                RemoteEndPoint = new IPEndPoint(controllerAddress, TARGET_PORT);

                // craft the packets which we want to send.
                for (byte i = 0; i < 16; i++)
                {
                    var packet = Packets[i] = new byte[1040]; // the payload for those chinese LED controllers has to be 1040 byte long.
                    packet[0] = 0x88;
                    packet[1] = i; // -> cycle between 0 and 16
                    packet[2] = 0xea;
                    packet[3] = 0x33;
                    packet[4] = 0xf1;
                    packet[5] = 0x88;
                    packet[6] = 0x00; //0xbc
                    packet[7] = 0xe0;
                    packet[8] = 0x32;
                    packet[9] = 0x22;
                    packet[10] = 0x14;
                    packet[11] = 0x7f;
                    packet[1039] = packet[1038] = packet[1037] = packet[1036] = 0; // postfix
                }

                var black = new RGBColor { R = 0, G = 0, B = 0 };
                for (byte u = 0; u < NUM_UNIVERSES; u++)
                    for (int i = 0; i < LEDS_PER_UNIVERSE; i++)
                    {
                        // each "universe" can control 1024 RGB leds, so we loop over all of those LEDs and set them to black initially
                        this[u, i] = black;
                    }
                Enabled = false;
            }

            /// <summary>
            /// Sets the color of the given pixel as RGB color.
            /// Note that this will discard the least significant three bits.
            /// </summary>
            /// <param name="index">The pixel index between 0 and LEDS_PER_UNIVERSE</param>
            public RGBColor this[byte universe, int index]
            {
                set
                {
                    if (index >= LEDS_PER_UNIVERSE || index < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    var packetIndex = index / LEDS_PER_PACKET;
                    var indexInPacket = (index % LEDS_PER_PACKET) * BYTES_PER_LED;
                    indexInPacket += 12; //skip the header (12 bytes)

                    var shift = 8 - BITS_PER_COLOR;
                    RGBColor v2 = new RGBColor
                    {
                        R = (byte)(value.R >> shift),
                        G = (byte)(value.G >> shift),
                        B = (byte)(value.B >> shift)
                    };

                    //The first byte of the led range has to be set to a value I call LED_SEPARATOR.
                    //This bit is not used by the LPD6803 controller in any way according to 
                    //its datasheet.
                    Packets[packetIndex][indexInPacket] = LED_SEPARATOR;

                    // Each byte payload represents one bit data for each universe
                    // So the least significant bit represents the bit at position x for universe 1 and
                    // the most significant bit represents the bit at position x for universe 8
                    // We use zero based indices here, so universe 0 to 7
                    var univ = (byte)(1 << universe);
                    var iuniv = (byte)(~univ);

                    for (byte bit = 0; bit < BITS_PER_COLOR; bit++)
                    {
                        // Update the associated bits.
                        if (v2.R.IsBitSet(bit))
                            Packets[packetIndex][indexInPacket + RED_OFFSET + bit] |= univ;
                        else
                            Packets[packetIndex][indexInPacket + RED_OFFSET + bit] &= iuniv;

                        if (v2.G.IsBitSet(bit))
                            Packets[packetIndex][indexInPacket + GREEN_OFFSET + bit] |= univ;
                        else
                            Packets[packetIndex][indexInPacket + GREEN_OFFSET + bit] &= iuniv;

                        if (v2.B.IsBitSet(bit))
                            Packets[packetIndex][indexInPacket + BLUE_OFFSET + bit] |= univ;
                        else
                            Packets[packetIndex][indexInPacket + BLUE_OFFSET + bit] &= iuniv;
                    }
                    // enable this controller, to ensure it will get updated correctly.
                    Enabled = true;
                    RefreshController?.Invoke(ControllerId);
                }
            }

            public bool Enabled { get; set; }
        }

        readonly ILog logger = LogManager.GetLogger("RGBController");
        UdpClient udpSocket = new UdpClient();

        List<Controller> controllers = new List<Controller>();

        ManualResetEvent wh = new ManualResetEvent(false);
        RateLimitedLoop sender;
        bool refresh;

        void updateControllers(Func<bool> run)
        {
            // using the first flag we ensure that all controllers perform an update when this function is called for
            // the first time. this prevents them from displaying garbage.
            bool first = true;

            while (run())
            {
                wh.WaitOne(sender.RateLimitTime);
                wh.Reset();

                foreach (var c in controllers)
                {
                    if (!c.Enabled && !first) continue;

                    for (int p = 0; p < c.Packets.Length; p++)
                    {
                        udpSocket.Send(c.Packets[p], 1040, c.RemoteEndPoint);
                        Thread.Sleep(1);
                    }

                    udpSocket.Send(Controller.ContinuationPacket, Controller.ContinuationPacket.Length, c.RemoteEndPoint);
                    try
                    {
                        IPEndPoint remote = new IPEndPoint(c.RemoteEndPoint.Address, c.RemoteEndPoint.Port);
                        udpSocket.Receive(ref remote);
                    }
                    catch (SocketException e)
                    {

                        // controller is offline.
                        LogManager.GetLogger("RGBLED:Controller")
                                  .Debug($"Controller {c.ControllerId} is offline or not responding: {e.Message}");
                    }
                    Thread.Sleep(3);
                    udpSocket.Send(Controller.StartPacket, Controller.StartPacket.Length, c.RemoteEndPoint);
                }
                first = false;
            }
        }

        /// <summary>
        /// Perform some cleanup tasks and unbind callbacks.
        /// </summary>
        public bool Destroy()
        {
            //dispose the controllers
            foreach (var c in controllers)
            {
                c.RefreshController -= controllerNeedsRefresh;
            }
            controllers.Clear();
            return true;
        }

        /// <summary>
        /// This function is executed when one controller needs to perform a refresh and wants to schedule this.
        /// </summary>
        /// <param name="obj">The controller id</param>
        void controllerNeedsRefresh(int obj)
        {
            refresh = true;
        }

        /// <summary>
        /// Initializes the controller chains using the given configuration.
        /// </summary>
        /// <param name="config">Configuration structure.</param>
        public bool Init(T100kConfig config)
        {
            logger.Info($"Initializing china RGB LED controller");

            sender = new RateLimitedLoop(updateControllers, (int)(1000.0 / config.Framerate), "RGBLED");

            bool foundNetworkInterface = !config.StrictIPChecking;
            //if "StrictIPChecking" is set, set foundNetworkInterface to false

            logger.Debug($"Searching for network interfaces");
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                logger.Debug($"Scanning network interface {iface.Name}.");
                if (iface.OperationalStatus != OperationalStatus.Up)
                {
                    //skip lo, and unconnected network interfaces
                    logger.Info($"Skipping network interface {iface.Name}, status: {iface.OperationalStatus}");
                    continue;
                }
                bool hasUseableIP = false;
                foreach (var ip in iface.GetIPProperties().UnicastAddresses)
                {
                    // According to the manual of those LED controllers, the host has to use
                    // IPv4 using the address 192.168.60.178
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                        ip.Address.Equals(new IPAddress(new byte[] { 192, 168, 60, 178 })))
                    {
                        hasUseableIP = true;
                        break;
                    }
                }
                if (hasUseableIP)
                {
                    logger.Info($"Using interface {iface.Name}");
                    foundNetworkInterface = true;
                    break;
                }
                logger.Debug($"Skipping interface {iface.Name}, no valid address.");
            }

            if (!foundNetworkInterface)
            {
                logger.Warn("Failed to find usable network interface with the IP address 192.168.60.178");
                return false;
            }

            logger.Info("Creating controllers.");
            for (byte i = 0; i < config.MaximumControllerID; i++)
            {
                var c = new Controller(i);
                c.RefreshController += controllerNeedsRefresh;
                controllers.Add(c);
            }
            udpSocket = new UdpClient();
            udpSocket.Client.ReceiveTimeout = 10;
            return true;
        }

        /// <summary>
        /// Starts the sending process to the controllers.
        /// This call is nonblocking and will return immediately.
        /// </summary>
        public void Start()
        {
            sender.Start();
        }

        /// <summary>
        /// Stops the sending process.
        /// This call is nonblocking and will return immediately.
        /// </summary>
        public void Stop()
        {
            sender.Stop();
        }

        /// <summary>
        /// Update the data sent to the controllers.
        /// This method is used to set the color of the specified LED.
        /// It is not meant to be used for updating the whole universe.
        /// This method is asynchronously and returns as soon as all output channels are updated.
        /// </summary>
        /// <param name="newData">Channels to update.</param>
        public void UpdateData(IEnumerable<OutputItem> newData)
        {
            foreach (var item in newData)
            {
                var cntrl = controllers[item.ControllerID];
                cntrl[item.UniverseID, item.Channel] = item.Color;
            }

            if (refresh)
                wh.Set();
        }
    }
}
