﻿// (C) Made, programmed and designed by PlayIngoHD/PlayIngoHD Gaming/playingo/DEERUX and iProgramInCpp/iProgramMC only.
// Reselling this is illegal, because this is free-opensource-ware and credits are appreciated :)
using ENet.Managed;
using Kernys.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrowbrewProxy
{
    public partial class MainForm : Form
    {

        public class StateObject
        {
            // Client socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 1024;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new();
        }

        public static byte GrowbrewHNetVersion = 1;
        private static bool isHTTPRunning = false;
        public static PlayerForm pForm = new();

        public static bool multibottingEnabled = false;

        public static bool skipCache = false;
        public static bool logallpackettypes = false;

        public static TcpClient tClient = new();
        public static StateObject stateObj = new();


        public static string LogText = string.Empty;

        private delegate void SafeCallDelegate(string text);
        private delegate void SafeCallDelegatePort(ushort port);

        public void UpdatePortBoxSafe(ushort port)
        {
            if (textBox1.InvokeRequired)
            {
                SafeCallDelegatePort d = new(UpdatePortBoxSafe);
                _ = portBox.Invoke(d, new object[] { port });
            }
            else
            {
                portBox.Value = port;
            }
        }


        public static ENetHost client;
        private ENetHost m_Host;

        public static ENetPeer realPeer;
        public static ENetPeer proxyPeer;

        // unnecessary as botting isnt made for open src anyway

        public class UserData
        {
            public ulong connectIDReal = 0;
            public ulong connectID = 0;

            public bool didQuit = false;
            public bool mayContinue = false;
            public bool srvRunning = false;
            public bool clientRunning = false;
            public int Growtopia_Port = 17196;
            public string Growtopia_IP = "213.179.209.168";
            public string Growtopia_Master_IP = "213.179.209.168";
            public int Growtopia_Master_Port = 17196;

            public bool isSwitchingServer = false;
            public bool blockEnterGame = false;
            public bool serializeWorldsAdvanced = true;
            public bool bypass10PlayerMax = true;

            // internal variables =>
            public string tankIDName = "";
            public string tankIDPass = "";
            public string game_version = "5";
            public string country = "us";
            public string requestedName = "";
            public int token = 0;
            public bool resetStuffNextLogon = false;
            public int userID = -1;
            public int lmode = -1;
            public byte[] skinColor = new byte[4];
            public bool enableSilentReconnect = false;
            public bool hasLogonAlready = false;
            public bool hasUpdatedItemsAlready = false;
            public bool bypassAAP = false;
            public bool ghostSkin = false;
            // CHEAT VARS/DEFS
            public bool cheat_magplant = false;
            public bool cheat_rgbSkin = false;
            public bool cheat_autoworldban_mod = false;
            public bool cheat_speedy = false;
            public bool isAutofarming = false;
            public bool cheat_Autofarm_magplant_mode = false;
            public bool redDamageToBlock = false; // exploit discovered in servers at time of client being in version 3.36/3.37
                                                  // CHEAT VARS/DEFS
            public string macc = "02:15:01:20:30:05";
            public string doorid = "";
            public string rid = "", sid = "";


            public bool ignoreonsetpos = false;
            public bool unlimitedZoom = false;
            public bool isFacingSwapped = false;
            public bool blockCollecting = false;
            public short lastWrenchX = 0;
            public short lastWrenchY = 0;
            public bool awaitingReconnect = false;
            public bool enableAutoreconnect = false;
            public string autoEnterWorld = "";
            public bool dontSerializeInventory = false;
            public bool skipGazette = false;
        }

        public static UserData globalUserData = new();

        /*public struct ENetPacketQueueStruct
        {
            public byte[] ePacket;
            public ENetPeer ePeer;
        }

        public static List<ENetPacketQueueStruct> globalPacketQueue = new List<ENetPacketQueueStruct>();*/


        private readonly ItemDatabase itemDB = new();


        public static HandleMessages messageHandler = new();

        public MainForm()
        {
            InitializeComponent();
        }

        // adding rgb to version label :)
        private int r = 244, g = 65, b = 65;
        private int rgbTransitionState = 0;

        private int doTransitionRed()
        {
            if (b >= 250)
            {
                r -= 1; // red uses -1 / +1, doing it cuz red is a more dominant color imo

                if (r <= 65)
                {
                    rgbTransitionState = 1;
                }
            }

            if (b <= 65)
            {
                r += 1;

                if (r >= 250)
                {
                    rgbTransitionState = 1;
                }
            }
            return r;
        }

        private int doTransitionGreen()
        {
            if (r <= 65)
            {
                g += 2;

                if (g >= 250)
                {
                    rgbTransitionState = 2;
                }
            }

            if (r >= 250)
            {
                g -= 2;

                if (g <= 65)
                {
                    rgbTransitionState = 2;
                }
            }
            return g;
        }

        private int doTransitionBlue()
        {
            if (g <= 65)
            {
                b += 2;

                if (b >= 250)
                {
                    rgbTransitionState = 0;
                }
            }

            if (g >= 250)
            {
                b -= 2;

                if (b <= 65)
                {
                    rgbTransitionState = 0;
                }
            }
            return b;
        }


        public static string GenerateRID()
        {
            string str = "0";
            Random random = new();
            const string chars = "ABCDEF0123456789";
            str += new string(Enumerable.Repeat(chars, 31)
               .Select(s => s[random.Next(s.Length)]).ToArray());
            return str;
        }

        private static readonly Random random = new();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GenerateUniqueWinKey()
        {
            string str = "7";
            Random random = new();
            const string chars = "ABCDEF0123456789";
            str += new string(Enumerable.Repeat(chars, 31)
               .Select(s => s[random.Next(s.Length)]).ToArray());
            return str;
        }

        public static string GenerateMACAddress()
        {
            Random random = new();
            byte[] buffer = new byte[6];
            random.NextBytes(buffer);
            string result = string.Concat(buffer.Select(x => string.Format("{0}:", x.ToString("X2"))).ToArray());
            return result.TrimEnd(':');
        }

        public static string CreateLogonPacket(string customGrowID = "", string customPass = "", int customUserID = -1, int customToken = -1, string doorID = "")
        {

            // this is kinda messy
            string gversion = globalUserData.game_version;
            string p = string.Empty;
            Random rand = new();
            bool requireAdditionalData = globalUserData.token > -1;

            if (customGrowID == "")
            {
                if (globalUserData.tankIDName != "")
                {
                    p += "tankIDName|" + globalUserData.tankIDName + "\n";
                    p += "tankIDPass|" + globalUserData.tankIDPass + "\n";
                }
            }
            else
            {
                //Console.WriteLine("CUSTOM GROWID IS : " + customGrowID);
                p += "tankIDName|" + customGrowID + "\n";
                p += "tankIDPass|" + customPass + "\n";
            }

            p += "requestedName|" + "Growbrew" + rand.Next(0, 255).ToString() + "\n"; //"Growbrew" + rand.Next(0, 255).ToString() + "\n"
            p += "f|1\n";
            p += "protocol|120\n";
            p += "game_version|" + gversion + "\n";
            if (requireAdditionalData)
            {
                p += "lmode|" + globalUserData.lmode + "\n";
            }

            p += "cbits|128\n";
            p += "player_age|100\n";
            p += "GDPR|1\n";
            p += "hash2|" + rand.Next(-777777776, 777777776).ToString() + "\n";
            p += "meta|playingo.co.uk-456236.999666420.de\n"; // soon auto fetch meta etc.
            p += "fhash|-716928004\n";
            p += "platformID|0\n";
            p += "deviceVersion|0\n";
            p += "country|" + globalUserData.country + "\n";
            p += "hash|" + rand.Next(-777777776, 777777776).ToString() + "\n";
            p += "mac|" + globalUserData.macc + "\n";
            p += "rid|" + (globalUserData.rid == "" ? GenerateRID() : globalUserData.rid) + "\n";
            if (requireAdditionalData)
            {
                p += "user|" + globalUserData.userID.ToString() + "\n";
            }

            if (requireAdditionalData)
            {
                p += "token|" + globalUserData.token.ToString() + "\n";
            }

            if (customUserID > 0)
            {
                p += "user|" + customUserID.ToString() + "\n";
            }

            if (customToken > 0)
            {
                p += "token|" + customToken.ToString() + "\n";
            }

            if (globalUserData.doorid != "" && doorID == "")
            {
                p += "doorID|" + globalUserData.doorid + "\n";
            }
            else if (doorID != "")
            {
                p += "doorID|" + doorID + "\n";
            }

            p += "wk|" + (globalUserData.sid == "" ? GenerateUniqueWinKey() : globalUserData.sid) + "\n";
            p += "fz|1331849031";
            Console.WriteLine(p);
            p += "zf|-1331849031";
            return p;
        }

        public void AppendLog(string text)
        {
            if (text == string.Empty)
            {
                return;
            }

            if (logBox.InvokeRequired)
            {
                _ = logBox.Invoke(new SafeCallDelegate(AppendLog), new object[] { text });
            }
            else
            {
                logBox.Text += text + "\n";
            }
        }





        public static void ConnectToServer(ref ENetPeer peer, UserData userData = null, bool FirstInitialUseOfBot = false)
        {
            Console.WriteLine("Internal proxy client is attempting a connection to server...");

            string ip = globalUserData.Growtopia_IP;
            int port = globalUserData.Growtopia_Port;


            if (peer == null)
            {
                peer = client.Connect(new IPEndPoint(IPAddress.Parse(ip), port), 2, 0);
            }
            else
            {
                if (peer.IsNull)
                {
                    peer = client.Connect(new IPEndPoint(IPAddress.Parse(ip), port), 2, 0);
                }
                else if (peer.State != ENetPeerState.Connected)
                {
                    peer = client.Connect(new IPEndPoint(IPAddress.Parse(ip), port), 2, 0);
                }
                else
                {

                    // peer = client.Connect(new IPEndPoint(IPAddress.Parse(ip), port), 2, 0);
                    globalUserData.awaitingReconnect = true;
                    peer.Disconnect(0);

                    //In this case, we will want the realPeer to be disconnected first

                    // sub server switching, most likely.
                    peer = client.Connect(new IPEndPoint(IPAddress.Parse(ip), port), 2, 0);
                }
            }
        }

        private void Host_OnConnect(ENetPeer peer)
        {

            proxyPeer = peer;

            //peer.Timeout(1000, 5000, 8000);
            //e.Peer.Timeout(30000, 25000, 30000);
            //MessageBox.Show("a");
            // Thread.Sleep(1000);
            AppendLog("Connecting to gt servers at " + globalUserData.Growtopia_IP + ":" + globalUserData.Growtopia_Port.ToString() + "...");
            globalUserData.connectID++;
            ConnectToServer(ref MainForm.realPeer);

        }

        private void Peer_OnDisconnect(object sender, uint e)
        {
            ENetPeer peer = (ENetPeer)sender;
            if (globalUserData.isSwitchingServer)
            {
                globalUserData.isSwitchingServer = false;
                GamePacketProton variantPacket = new();
                variantPacket = new GamePacketProton
                {
                    delay = 0, //Avoid too quick connection and give headroom for enetcommand to prevent random/rare freezing (fix by Toxic Vampor)
                    NetID = -1
                };
                variantPacket.AppendString("OnSendToServer");
                variantPacket.AppendInt(2);
                variantPacket.AppendInt(globalUserData.token);
                variantPacket.AppendInt(globalUserData.userID);
                variantPacket.AppendString("127.0.0.1|" + globalUserData.doorid);
                variantPacket.AppendInt(globalUserData.lmode);

                messageHandler.packetSender.SendData(variantPacket.GetBytes(), MainForm.proxyPeer);
                return;
            }

            if (globalUserData.enableSilentReconnect)
            {
                unsafe
                {
                    if (((ENetPeer)sender).GetNativePointer()->ConnectID != realPeer.GetNativePointer()->ConnectID)
                    {
                        return;
                    }
                }

                try
                {
                    realPeer.Send(0, new byte[60], ENetPacketFlags.Reliable);
                }
                catch
                {

                    if (proxyPeer != null)
                    {
                        if (proxyPeer.State == ENetPeerState.Connected)
                        {
                            GamePacketProton variantPacket = new();
                            variantPacket.AppendString("OnConsoleMessage");
                            variantPacket.AppendString("`6(PROXY) `![GROWBREW SILENT RECONNECT]: `wGrowbrew detected an unexpected disconnection, silently reconnecting...``");
                            messageHandler.packetSender.SendData(variantPacket.GetBytes(), MainForm.proxyPeer);
                        }
                    }
                }

                // ConnectToServer(useRealPeer ? ref realPeer : ref peer);

                ConnectToServer(ref realPeer);
            }
            else if (globalUserData.enableAutoreconnect)
            {
                unsafe
                {
                    if (((ENetPeer)sender).GetNativePointer()->ConnectID != realPeer.GetNativePointer()->ConnectID)
                    {
                        return;
                    }
                }

                try
                {
                    realPeer.Send(0, new byte[60], ENetPacketFlags.Reliable);
                }
                catch
                {

                    if (proxyPeer != null)
                    {
                        if (proxyPeer.State == ENetPeerState.Connected)
                        {
                            GamePacketProton variantPacket2 = new();
                            variantPacket2.AppendString("OnReconnect");
                            messageHandler.packetSender.SendData(variantPacket2.GetBytes(), MainForm.proxyPeer);
                        }
                    }
                }
            }
            messageHandler.enteredGame = false;

            AppendLog("An internal disconnection was triggered in the proxy, you may want to reconnect your GT Client if you are not being disconnected by default (maybe because of sub-server switching?)");
        }

        private void Peer_OnReceive(object sender, ENetPacket e)
        {
            ENetPeer peer = (ENetPeer)sender;
            if (peer.IsNull)
            {
                AppendLog("Attention peer is null!! (Peer_OnReceive)");
            }

            string str = messageHandler.HandlePacketFromClient(ref peer, e);
            if (str is not "_none_" and not "")
            {
                AppendLog(str);
            }
        }

        private void Peer_OnReceive_Client(object sender, ENetPacket e)
        {

            ENetPeer peer = (ENetPeer)sender;
            if (peer.IsNull)
            {
                AppendLog("Attention peer is null!! (Peer_OnReceive_Client)");
            }

            string str = messageHandler.HandlePacketFromServer(ref peer, e);
            if (str is not "_none_" and not "")
            {
                AppendLog(str);
            }
        }

        private void loadLogs(bool requireReloadFromFile = false)
        {
#if DEBUG
            if (requireReloadFromFile)
            {
                LogText = File.ReadAllText("debuglog.txt");
                entireLog.Text = LogText;
                return;
            }
#endif
            entireLog.Text = LogText;
        }


        private void Client_OnConnect(ENetPeer peer)
        {
            AppendLog("The growtopia client just connected successfully.");
            peer.Timeout(1000, 4000, 6000);
            //peer.PingInterval(TimeSpan.FromMilliseconds(1000));

            realPeer = peer;
            globalUserData.connectIDReal++;
        }

        private void doServerService(int delay = 0)
        {
            doClientService(0);
            ENetEvent Event = m_Host.Service(TimeSpan.FromMilliseconds(delay));

            switch (Event.Type)
            {
                case ENetEventType.None:

                    break;
                case ENetEventType.Connect:
                    Host_OnConnect(Event.Peer);
                    break;
                case ENetEventType.Disconnect:

                    break;
                case ENetEventType.Receive:

                    Peer_OnReceive(Event.Peer, Event.Packet);

                    Event.Packet.Destroy();
                    break;
                default:
                    throw new NotImplementedException();
            }
            doClientService(0);
        }

        private void doClientService(int delay = 0)
        {
            if (client == null)
            {
                return;
            }

            if (client.Disposed)
            {
                return;
            }

            ENetEvent Event = client.Service(TimeSpan.FromMilliseconds(delay));

            unsafe
            {
                switch (Event.Type)
                {
                    case ENetEventType.None:

                        break;
                    case ENetEventType.Connect:
                        Client_OnConnect(Event.Peer);
                        break;
                    case ENetEventType.Disconnect:
                        Peer_OnDisconnect(Event.Peer, 0);
                        Event.Peer.UnsetUserData();
                        break;
                    case ENetEventType.Receive:

                        Peer_OnReceive_Client(Event.Peer, Event.Packet);
                        Event.Packet.Destroy();
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }


        private void doClientServerServiceLoop()
        {
            while (true)
            {
                doClientService(1);
                doServerService(3);
            }
        }

        private void doProxy()
        {
            if (client == null || m_Host == null)
            {
                _ = MessageBox.Show("Failed to start proxy, either client was null or m_Host was, please check if the port 2 is free. (Growbrew Proxy port)");
                return;
            }

            _ = Task.Run(() => doClientServerServiceLoop());
        }

        private void LaunchProxy()
        {
            if (!globalUserData.srvRunning)
            {
                globalUserData.srvRunning = true;
                globalUserData.clientRunning = true;

                // Setting up ENet-Server ->

                m_Host = new ENetHost(new IPEndPoint(IPAddress.Any, 2), 32, 10, 0, 0);
                m_Host.ChecksumWithCRC32();
                m_Host.CompressWithRangeCoder();
                m_Host.EnableNewProtocol(2);

                // Setting up ENet-Client ->
                client = new ENetHost(null, 64, 10); // for multibotting, coming soon.
                client.ChecksumWithCRC32();
                client.CompressWithRangeCoder();
                client.EnableNewProtocol(1);

                // realPeer = client.Connect(new IPEndPoint(IPAddress.Parse(globalUserData.Growtopia_Master_IP), globalUserData.Growtopia_Master_Port), 2, 0);
                //realPeer = client.Connect(new IPEndPoint(IPAddress.Parse(globalUserData.Growtopia_Master_IP), globalUserData.Growtopia_Master_Port), 2, 0);
                doProxy();

                // Setting up controls
                runproxy.Enabled = false; // too lazy to make it so u can disable it via button
                labelsrvrunning.Text = "Server is running!";
                labelsrvrunning.ForeColor = Color.Green;
                labelclientrunning.Text = "Client is running!";
                labelclientrunning.ForeColor = Color.Green;
            }
        }

        private void runproxy_Click(object sender, EventArgs e)
        {
            if (ipaddrBox.Text != "" && portBox.Text != "")
            {
                globalUserData.Growtopia_IP = ipaddrBox.Text;
                globalUserData.Growtopia_Port = Convert.ToInt32(portBox.Text);
            }
            LaunchProxy();
        }

        private void doRGBEverything()
        {
            while (true)
            {
                switch (rgbTransitionState)
                {
                    case 0:
                        Invoke(new Action(() =>
                        {
                            vLabel.ForeColor = Color.FromArgb(doTransitionRed(), g, b);
                        }));
                        break;
                    case 1:
                        Invoke(new Action(() =>
                        {
                            vLabel.ForeColor = Color.FromArgb(r, doTransitionGreen(), b);
                        }));
                        break;
                    case 2:
                        Invoke(new Action(() =>
                        {
                            vLabel.ForeColor = Color.FromArgb(r, g, doTransitionBlue());
                        }));
                        break;
                }

                Invoke(new Action(() =>
                {
                    label14.ForeColor = Color.FromArgb(b, r, g);
                }));

                if (globalUserData.cheat_rgbSkin)
                {
                    globalUserData.skinColor[0] = 200; // slight transparent alpha
                    globalUserData.skinColor[1] = (byte)r;
                    globalUserData.skinColor[2] = (byte)g;
                    globalUserData.skinColor[3] = (byte)b;

                    GamePacketProton variantPacket = new();
                    variantPacket.AppendString("OnChangeSkin");
                    variantPacket.AppendUInt(BitConverter.ToUInt32(globalUserData.skinColor, 0));
                    variantPacket.NetID = messageHandler.worldMap.netID;
                    messageHandler.packetSender.SendData(variantPacket.GetBytes(), proxyPeer);
                }
                Thread.Sleep(30);
            }
        }

        public static uint HashBytes(byte[] b) // Thanks to iProgramInCpp !
        {
            byte[] n = b;
            uint acc = 0x55555555;

            for (int i = 0; i < b.Length; i++)
            {
                acc = (acc >> 27) + (acc << 5) + n[i];
            }
            return acc;
        }


        private void MainForm_Load(object sender, EventArgs e)
        {

            //this.BackColor = Color.Snow; for those who want slight transparency, I have set the transparency key to snow, which can also be changed :)

            StartupScreen stsc = new();
            _ = stsc.ShowDialog();

            ENetStartupOptions startupOptions = new()
            {
                ModulePath = Directory.GetCurrentDirectory() + "\\enet.dll"
            };

            ManagedENet.Startup(startupOptions);
            ManagedENet.Shutdown(delete: false);

            globalUserData.macc = GenerateMACAddress();

            if (!Directory.Exists("stored"))
            {
                _ = Directory.CreateDirectory("stored");
            }

            DialogResult dr = MessageBox.Show("Proceeding will connect you to the Growbrew Network!\nGROWBREW MAY USE ANY OF YOUR HARDWARE IDENTIFIERS AND YOUR IP WHICH ARE USED TO SECURE THE PRODUCT E.G FOR BANS AND ANTI-CRACK SOLUTIONS! \nRead more in 'Growbrew Policies'\nContinue?", "Growbrew Proxy", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr == DialogResult.No)
            {
                Environment.Exit(-1);
            }

            PriceChecker.SetHTTPS();

            if (File.Exists("stored/config.gbrw"))
            {
                try
                {
                    /*BSONObject bsonObj = new BSONObject();
                    bsonObj["cfg_version"] = 1;
                    bsonObj["disable_advanced_world_loading"] = checkBox7.Checked;
                    bsonObj["unlimited_zoom"] = checkUnlimitedZoom.Checked;
                    bsonObj["block_enter_game"] = checkBox2.Checked;
                    bsonObj["append_netiduserid_to_names"] = checkAppendNetID.Checked;
                    bsonObj["ignore_position_setback"] = ignoresetback.Checked;
                    bsonObj["instant_world_menu_skip_cache"] = checkBox6.Checked;*/
                    BSONObject bsObj = SimpleBSON.Load(File.ReadAllBytes("stored/config.gbrw"));
                    int confVer = bsObj["cfg_version"];
                    checkBox7.Checked = bsObj["disable_advanced_world_loading"];
                    globalUserData.serializeWorldsAdvanced = checkBox7.Checked;
                    checkUnlimitedZoom.Checked = bsObj["unlimited_zoom"];
                    globalUserData.unlimitedZoom = checkUnlimitedZoom.Checked;
                    checkBox2.Checked = bsObj["block_enter_game"];
                    globalUserData.blockEnterGame = checkBox2.Checked;
                    ignoresetback.Checked = bsObj["ignore_position_setback"];
                    globalUserData.ignoreonsetpos = ignoresetback.Checked;
                    checkBox6.Checked = bsObj["instant_world_menu_skip_cache"];
                    skipCache = checkBox6.Checked;

                    if (confVer > 1)
                    {
                        checkBox9.Checked = bsObj["block_item_collect"];
                        globalUserData.blockCollecting = checkBox9.Checked;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"tried to load config from stored/config.gbrw, failed due to: {ex.Message}, please re-export.");
                }
            }
            //tClient.BeginConnect(IPAddress.Parse("89.47.163.53"), 6770, new AsyncCallback(ConnectCallback), tClient.Client);

            // hackernetwork is discontinued / servers shutdown, it was good to have it when the proxy was paid, now its abusive and just a big bug mess.

            patUpDown.Maximum = patTrackBar.Maximum;
            playerLogicUpdate.Start();
            itemDB.SetupItemDefs();


            _ = Task.Run(() => doRGBEverything());
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {


            globalUserData.srvRunning = false;
            globalUserData.clientRunning = false;

            Environment.Exit(0);

        }

        private void logBox_TextChanged(object sender, EventArgs e)
        {
            logBox.SelectionStart = logBox.Text.Length;
            // scroll it automatically
            logBox.ScrollToCaret();
        }

        private void formTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (ActiveForm == null)
                {
                    return;
                }

                if (proxyPages.SelectedTab == proxyPages.TabPages["proxyPage"])
                {
                    ActiveForm.Text = "Growbrew Proxy - Main Page";
                }
                else if (proxyPages.SelectedTab == proxyPages.TabPages["cheatPage"])
                {
                    ActiveForm.Text = "Growbrew Proxy - Cheats and more";
                }
                else if (proxyPages.SelectedTab == proxyPages.TabPages["extraPage"])
                {
                    loadLogs();
                    ActiveForm.Text = "Growbrew Proxy - Logs";
                }
                else if (proxyPages.SelectedTab == proxyPages.TabPages["accountCheckerPage"])
                {
                    ActiveForm.Text = "Growbrew Proxy - Account Checker";
                }
                else if (proxyPages.SelectedTab == proxyPages.TabPages["autofarmPage"])
                {
                    ActiveForm.Text = "Growbrew Proxy - Autofarming";
                }
                else if (proxyPages.SelectedTab == proxyPages.TabPages["multibottingPage"])
                {
                    ActiveForm.Text = "Growbrew Proxy - Multibot";
                }
            }
            catch
            {

            }
        }

        private void reloadLogs_Click(object sender, EventArgs e)
        {
            loadLogs();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (ipaddrBox.Text != "" && portBox.Value != 0)
            {
                globalUserData.token = 0;
                globalUserData.Growtopia_IP = ipaddrBox.Text;
                globalUserData.Growtopia_Port = (int)portBox.Value;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (realPeer != null)
            {
                if (realPeer.State == ENetPeerState.Connected)
                {
                    realPeer.DisconnectNow(0);
                }
            }
            if (proxyPeer != null)
            {
                if (proxyPeer.State == ENetPeerState.Connected)
                {
                    proxyPeer.DisconnectNow(0);
                }
            }
        }

        private void changeNameBox_TextChanged(object sender, EventArgs e)
        {
            GamePacketProton variantPacket = new();
            variantPacket.AppendString("OnNameChanged");
            variantPacket.AppendString("`w" + changeNameBox.Text + "``");
            variantPacket.NetID = messageHandler.worldMap.netID;
            messageHandler.packetSender.SendData(variantPacket.GetBytes(), proxyPeer);
            //variantPacket.NetID =


        }
        private void rgbSkinHack_CheckedChanged(object sender, EventArgs e)
        {
            globalUserData.cheat_rgbSkin = !globalUserData.cheat_rgbSkin; // keeping track
        }

        private void hack_magplant_CheckedChanged(object sender, EventArgs e)
        {
            globalUserData.cheat_magplant = !globalUserData.cheat_magplant;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            pForm.Text = "All players in " + messageHandler.worldMap.currentWorld;
            _ = pForm.ShowDialog();
        }

        private void aboutlabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            _ = Process.Start("http://github.com/iProgramMC");  // iprogramincpp
            _ = Process.Start("http://github.com/moien007/ENet.Managed"); // moien007
            _ = Process.Start("http://github.com/playingoDEERUX"); // me
        }

        private void hack_autoworldbanmod_CheckedChanged(object sender, EventArgs e)
        {
            globalUserData.cheat_autoworldban_mod = !globalUserData.cheat_autoworldban_mod;
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            try
            {
                TankPacket p2 = new()
                {
                    PacketType = (int)NetTypes.PacketTypes.ITEM_ACTIVATE_OBJ
                };
                if (messageHandler.worldMap == null)
                {
                    return;
                }

                custom_collect_x.Text = messageHandler.worldMap.player.X.ToString(); // crahp
                custom_collect_y.Text = messageHandler.worldMap.player.Y.ToString(); // crahp
                p2.X = int.Parse(custom_collect_x.Text);
                p2.Y = int.Parse(custom_collect_y.Text);
                p2.MainValue = int.Parse(custom_collect_uid.Text);

                messageHandler.packetSender.SendPacketRaw((int)NetTypes.NetMessages.GAME_PACKET, p2.PackForSendingRaw(), MainForm.realPeer);
            }
            catch // ignore exception
            {

            }
        }

        private void playerLogicUpdate_Tick(object sender, EventArgs e)
        {
            if (messageHandler.worldMap != null) // checking if we have it setup
            {
                Player playerObject = messageHandler.worldMap.player;
                posXYLabel.Text = "X: " + playerObject.X.ToString() + " Y: " + playerObject.Y.ToString();
                wrenchXYlabel.Text = $"Last Wrench X: {globalUserData.lastWrenchX} Y: {globalUserData.lastWrenchY}";
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            globalUserData.cheat_speedy = !globalUserData.cheat_speedy;

            TankPacket p = new()
            {
                PacketType = (int)NetTypes.PacketTypes.SET_CHARACTER_STATE,
                X = 1000,
                Y = 300,
                YSpeed = 1000,
                NetID = messageHandler.worldMap.netID,
                XSpeed = cheat_speed.Checked ? 100000 : 300
            };
            byte[] data = p.PackForSendingRaw();
            Buffer.BlockCopy(BitConverter.GetBytes(8487168), 0, data, 1, 3);
            messageHandler.packetSender.SendPacketRaw((int)NetTypes.NetMessages.GAME_PACKET, data, proxyPeer);
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            int netID = 0;
            World map = messageHandler.worldMap;
            foreach (Player p in map.players)
            {
                if (p.name.Contains(nameBoxOn.Text))
                {
                    netID = p.netID;
                    break;
                }
            }
            messageHandler.packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, "action|wrench\nnetid|" + netID.ToString(), realPeer);
            messageHandler.packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, "action|dialog_return\ndialog_name|popup\nnetID|" + netID.ToString() + "|\nbuttonClicked|" + actionButtonClicked.Text + "\n", realPeer);
        }

        private void sendAction_Click(object sender, EventArgs e)
        {
            //messageHandler.packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, "action|setSkin\ncolor|" + actionText.Text + "\n", realPeer);
        }

        private void macUpdate_Click(object sender, EventArgs e)
        {
            globalUserData.macc = setMac.Text;
        }

        private void button3_Click_2(object sender, EventArgs e)
        {
            //messageHandler.packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, "action|dialog_return\ndialog_name|storageboxxtreme\ntilex|" + tileX.ToString() + "|\ntiley|" + tileY.ToString() + "|\nitemid|" + itemid.ToString() + "|\nbuttonClicked|cancel\n\nitemcount|1\n", realPeer);
            messageHandler.packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, "action|dialog_return\ndialog_name|storageboxxtreme\ntilex|" + tileX.ToString() + "|\ntiley|" + tileY.ToString() + "|\nitemid|1|\nbuttonClicked|cancel\nitemcount|1\n", realPeer);
        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            globalUserData.bypassAAP = !globalUserData.bypassAAP;
        }

        private void ghostmodskin_CheckedChanged(object sender, EventArgs e)
        {
            if (ghostmodskin.Checked)
            {
                globalUserData.skinColor[0] = 110; // A - transparency
                globalUserData.skinColor[1] = 255;
                globalUserData.skinColor[2] = 255;
                globalUserData.skinColor[3] = 255;

                GamePacketProton variantPacket = new();
                variantPacket.AppendString("OnChangeSkin");
                variantPacket.AppendUInt(BitConverter.ToUInt32(globalUserData.skinColor, 0));
                variantPacket.NetID = messageHandler.worldMap.netID;
                //variantPacket.delay = 100;
                messageHandler.packetSender.SendData(variantPacket.GetBytes(), proxyPeer);
            }
            else
            {
                globalUserData.skinColor[0] = 255;
                GamePacketProton variantPacket = new();
                variantPacket.AppendString("OnChangeSkin");
                variantPacket.AppendUInt(BitConverter.ToUInt32(globalUserData.skinColor, 0));
                variantPacket.NetID = messageHandler.worldMap.netID;
                //variantPacket.delay = 100;
                messageHandler.packetSender.SendData(variantPacket.GetBytes(), proxyPeer);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (send2client.Checked)
            {
                messageHandler.packetSender.SendPacket(3, packetText.Text, proxyPeer);
            }
            else
            {
                messageHandler.packetSender.SendPacket(3, packetText.Text, realPeer);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (send2client.Checked)
            {
                messageHandler.packetSender.SendPacket(2, packetText.Text, proxyPeer);
            }
            else
            {
                messageHandler.packetSender.SendPacket(2, packetText.Text, realPeer);
            }
        }

        private void actionButtonClicked_TextChanged(object sender, EventArgs e)
        {

        }

        private void nameBoxOn_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            TankPacket p = new()
            {
                PacketType = 3
            };

            for (int i = 0; i < 100; i++)
            {
                p.PunchX = i;
                p.PunchY = i;
                p.ExtDataMask = 838338258;
                messageHandler.packetSender.SendPacketRaw(4, p.PackForSendingRaw(), realPeer);
                p.PacketType = 0;
                messageHandler.packetSender.SendPacketRaw(4, p.PackForSendingRaw(), realPeer);
            }
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            messageHandler.packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, "action|input|?", realPeer);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string str = "";
            for (int i = 0; i < 10000; i++)
            {
                str += "aaaaaaaaaaa";
            }

            messageHandler.packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, str, realPeer);
            _ = MessageBox.Show("Sent packet!");
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void doTheFastNukaz()
        {
            while (checkBox1.Checked)
            {
                Thread.Sleep(10);
                if (realPeer != null)
                {
                    if (realPeer.State != ENetPeerState.Connected)
                    {
                        return;
                    }

                    for (int c = 0; c < 3; c++)
                    {
                        Thread.Sleep(1000);
                        for (int i = 0; i < 40; i++)
                        {
                            int x, y;
                            x = messageHandler.worldMap.player.X / 32;
                            y = messageHandler.worldMap.player.Y / 32;




                            TankPacket tkPt = new()
                            {
                                PunchX = x,
                                PunchY = y + i,
                                MainValue = 18,
                                X = messageHandler.worldMap.player.X,
                                Y = messageHandler.worldMap.player.Y
                            };
                            tkPt.ExtDataMask &= ~0x04;
                            tkPt.ExtDataMask &= ~0x40;
                            tkPt.ExtDataMask &= ~0x10000;
                            tkPt.NetID = -1;
                            messageHandler.packetSender.SendPacketRaw(4, tkPt.PackForSendingRaw(), realPeer);
                            tkPt.NetID = -1;
                            tkPt.PacketType = 3;
                            tkPt.ExtDataMask = 0;
                            messageHandler.packetSender.SendPacketRaw(4, tkPt.PackForSendingRaw(), realPeer);
                        }
                    }
                }
            }
        }

        private void doTheNukaz()
        {
            while (checkBox3.Checked)
            {
                Thread.Sleep(10);
                if (realPeer != null)
                {
                    if (realPeer.State != ENetPeerState.Connected)
                    {
                        return;
                    }

                    int c = 3;
                    if (checkBox4.Checked)
                    {
                        c = 4;
                    }

                    for (int i = 0; i < c; i++)
                    {
                        int x, y;
                        x = messageHandler.worldMap.player.X / 32;
                        y = messageHandler.worldMap.player.Y / 32;

                        if (!checkBox5.Checked)
                        {

                            if (i == 0)
                            {
                                x++;
                            }
                            else if (i == 1)
                            {
                                x--;
                            }
                            else if (i == 2)
                            {
                                y--;
                            }

                            if (checkBox4.Checked)
                            {
                                if (i == 3)
                                {
                                    y++;
                                }
                            }
                        }
                        else
                        {
                            if (globalUserData.isFacingSwapped)
                            {
                                if (i == 1)
                                {
                                    x -= 1;
                                }

                                if (i == 2)
                                {
                                    x -= 2;
                                }
                            }
                            else
                            {
                                if (i == 1)
                                {
                                    x += 1;
                                }

                                if (i == 2)
                                {
                                    x += 2;
                                }
                            }
                        }

                        Thread.Sleep(166);
                        TankPacket tkPt = new()
                        {
                            PunchX = x,
                            PunchY = y,
                            MainValue = 18,
                            X = messageHandler.worldMap.player.X,
                            Y = messageHandler.worldMap.player.Y
                        };
                        tkPt.ExtDataMask &= ~0x04;
                        tkPt.ExtDataMask &= ~0x40;
                        tkPt.ExtDataMask &= ~0x10000;
                        tkPt.NetID = -1;
                        messageHandler.packetSender.SendPacketRaw(4, tkPt.PackForSendingRaw(), realPeer);
                        tkPt.NetID = -1;
                        tkPt.PacketType = 3;
                        tkPt.ExtDataMask = 0;
                        messageHandler.packetSender.SendPacketRaw(4, tkPt.PackForSendingRaw(), realPeer);
                    }
                }
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                Thread thread = new(new ThreadStart(doTheNukaz));
                thread.Start();
            }
        }

        private string filterOutAllBadChars(string str)
        {
            return Regex.Replace(str, @"[^a-zA-Z0-9\-]", "");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            World map = messageHandler.worldMap;
            foreach (Player p in map.players)
            {
                messageHandler.packetSender.SendPacket(2, "action|input\n|text|/pull " + p.name[2..^2], realPeer);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            World map = messageHandler.worldMap;
            foreach (Player p in map.players)
            {
                messageHandler.packetSender.SendPacket(2, "action|input\n|text|/ban " + p.name[2..^2], realPeer);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            World map = messageHandler.worldMap;
            foreach (Player p in map.players)
            {
                messageHandler.packetSender.SendPacket(2, "action|input\n|text|/kick " + p.name[2..^2], realPeer);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            World map = messageHandler.worldMap;
            foreach (Player p in map.players)
            {
                messageHandler.packetSender.SendPacket(2, "action|input\n|text|/trade " + p.name[2..^2], realPeer);
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            isHTTPRunning = !isHTTPRunning;
            if (isHTTPRunning)
            {
                string[] arr = new string[1];
                arr[0] = "http://*:80/";
                HTTPServer.StartHTTP(this, arr);
                button11.Text = "Stop HTTP Server";
            }
            else
            {
                HTTPServer.StopHTTP();
                button11.Text = "Start HTTP Server + Client";
            }
            label13.Visible = isHTTPRunning;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            skipCache = !skipCache;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            globalUserData.game_version = setVersion.Text;
        }

        private void proxyPage_Click(object sender, EventArgs e)
        {

        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {

        }



        private void button14_Click(object sender, EventArgs e)
        {
            TankPacket p = new()
            {
                PacketType = (int)NetTypes.PacketTypes.SET_CHARACTER_STATE,
                X = 1000,
                Y = 300,
                YSpeed = 1000,
                NetID = messageHandler.worldMap.netID,
                XSpeed = cheat_speed.Checked ? 100000 : 300,
                MainValue = 1
            };
            byte[] data = p.PackForSendingRaw();
            Buffer.BlockCopy(BitConverter.GetBytes(8487168), 0, data, 1, 3);
            messageHandler.packetSender.SendPacketRaw((int)NetTypes.NetMessages.GAME_PACKET, data, proxyPeer);
        }

        private void button15_Click(object sender, EventArgs e)
        {

        }

        private void changelog_Click(object sender, EventArgs e)
        {
            _ = MessageBox.Show("Growbrew Proxy Changelogs:\n" +
                "\n2.3\n" +
                "- Fix ping reply and random disconnects\n" +
                "- Add Item Price Checker, go check it out :D\n" +
                "- Added particle spawner (visual)\n" +
                "- Misc fixes\n" +
                "- Prepare internal, done research for android compilation\n" +
                "- Extreme edition is for sale now again, contact me on discord DEERUX#1551.\n" +
                "\n2.2.1\n" +
                "- Upgrade to .NET 5 and C# 9.0\n" +
                "- extreme version will contain an entire bundle of tools for GT (android stealer, growalts, cross/multibotting), including our own gt internal.\n" +
                "- Added BRB change & Slime spam exploit" +
                "- Fixed custom enet.dll with *server* (type2|1) sided and *client* sided fix :) thanks to mar4ello6\n" +
                "- Upgrade to ENet.Managed v5 by moien007 (async changes, support for using custom ENet native lib easily etc.\n" +
                "- Fix connect to servers due to modified ENet protocol from dev team\n" +
                "- Added new features, discover them your self!\n" +
                "- Updated a few common tile extras, didn't want to add support for every just so it gets skidded again...\n" +
                "- Growbrew Extreme is on sale now, and it's not just a 'proxy' anymore, it will feature an inbuilt internal with over 90+ features and support for android (have it working+privately tested, but not nearly 90 features yet), as well as multibotting with UP TO 90 BOTS of course...\n" +
                "- Enjoy!" +
                "\n2.1 (probably the last version)\n--------------------------\n" +
                "- (Biggest update) Crossbotting/Multibotting added (Not available in free open source version), better than multiboxing and it's all done in a single Growtopia window by design (saves tons of cpu and allows you to use many more accounts at the same time). You can basically add bots in the Multibot tab and make them do everything same as what you do without having more than one Growtopia window opened.\n" +
                "- Known bug: Peer.Reset doesn't trigger disconnect event after 5-8s of timeout, idk if it's one of my bugs but I thought it was because of ENet.Managed v4, so stay updated on that perhaps it'll get fixed?\n" +
                "- ENet.Managed v4 upgrade (github.com/moien007/ENet.Managed), proxy should run much more stable now, in a more optimized way (GC overhead reduced as you can now manually dispose ENetPackets). No random disconnects occur anymore when being in a world with many players.\n" +
                "- Overall MUCH faster performance, MUCH HIGHER stability and several bug fixes and more features added, again thanks to ENet.Managed v4 by moien007. Old Growbrew Proxy 2.0 used to disconnect randomly in popular worlds after a few min, this issue has been fixed because of that!\n" +
                "- GUI supports higher screen resolutions now.\n" +
                "- Autoban during autofarm has been fixed.\n" +
                "\n2.0\n--------------------------\n" +
                "- HUUUUGE UPDATE!\n" +
                "- Added NEW Account Checker (parses all your accounts in directory, and logs into them to see how much rares they have 1 by 1.)\n" +
                "- IT'S TIME TO GET RID OF ALL CRAPPY AUTOFARMERS THAT EXIST IN GROWTOPIA! :D Growbrew has now a super smart and superior autofarmer, you'll be amazed. It uses the entire world data, and autofarming via packets for the best possible experience. It is the fastest and most undetected autofarmer (and I am still improving it, this is the first ever release of it).\n" +
                "- Fixed HTTP Server Stopping (stopping it will now work properly, as well as relaunching/restarting it after)\n" +
                "- Updated RGB to use better RGB logics. (in RGB Skin Cheat, version label now has the same RGB too)\n" +
                "- Improved subserver switching, should avoid 1 more unnecessary sub server switch cause of request logon stuff (faster)\n" +
                "- Added silent automatic reconnection after proper detection of connection loss! (9-13 seconds)\n" +
                "- Added NEW Exploit: Red damage to blocks! Punch blocks and their damage will be red, OTHERS CAN SEE IT AND ITS NOT A VISUAL, its an exploit.\n" +
                "- Added Config page in Cheats/Mods/Misc, you can select if you want unlimited zoom in there and advanced world loading (by turning it off, you can enter worlds potentially faster)\n" +
                "- Added How to use button in Main page (Proxy page)\n" +
                "- Added Growbrew Spammer (right now in Multibot tab), a very undetected spammer using data packets and NOT the Keyboard as well.\n" +
                "\n1.5.4\n--------------------------\n" +
                "- Added simple load balancer for channel packet spread.\n" +
                "- Added better Unlimited Zoom related clothing loading fix\n" +
                "- Fixed doorID with subserver switching and entering through door/portal will not link to correct door in other world\n" +
                "- Many more fixes from previous builds, and optimizations.\n" +
                "- Removed many left over parts for Hacker Network, as the servers closed, I decided to remove some client code to keep the code a little more clean.\n" +
                "\nFull changelog in changelog.txt, too old versions wont be shown anymore. ~playingo/DEERUX");
        }

        private void button13_Click(object sender, EventArgs e)
        {
            TankPacket p = new()
            {
                PacketType = -1
            };


            messageHandler.packetSender.SendPacketRaw((int)NetTypes.NetMessages.GAME_PACKET, p.PackForSendingRaw(), realPeer);
        }

        private void button15_Click_1(object sender, EventArgs e)
        {

        }

        private void button15_Click_2(object sender, EventArgs e)
        {
            string pass = RandomString(8);
            messageHandler.packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, "action|input\n|text|/sb `2?_ [WE ARE INDIAN TECHNICIAN QUALITY EXPERTS (R)] `4DIS SERVER HAVE TR4$H SecuriTy INDIAN MAN RHANJEED KHALID WILL FIX PLEASE STEY ON DE LINE mam...\n\n\n\n\n\n\n\n`4DIS SERVER HAVE TR4$H SecuriTy INDIAN MAN RHANJEED KHALID WILL FIX PLEASE STEY ON DE LINE mam...\n\n\n\n\n\n\n\n`4DIS SERVER HAVE TR4$H SecuriTy INDIAN MAN RHANJEED KHALID WILL FIX PLEASE STEY ON DE LINE mam...\n\n\n\n\n\n\n\n`4DIS SERVER HAVE TR4$H SecuriTy INDIAN MAN RHANJEED KHALID WILL FIX PLEASE STEY ON DE LINE mam...\n\n\n\n\n\n\n\n  hacked by anonymous all ur data is hacked!`2_?", realPeer);
            for (int i = 0; i < 84; i++)
            {
                messageHandler.packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, "action|dialog_return\ndialog_name|register\nusername|" + RandomString(9) + "\npassword|" + pass + "\npasswordverify|" + pass + "\nemail|a@a.de\n", realPeer);

            }
        }

        private void doTakeAll()
        {
            _ = int.TryParse(setRID.Text, out _);

            for (int i = 0; i < 10000; i++)
            {
                Thread.Sleep(12);
                TankPacket p2 = new()
                {
                    PacketType = (int)NetTypes.PacketTypes.ITEM_ACTIVATE_OBJ,
                    MainValue = i
                };
                messageHandler.packetSender.SendPacketRaw((int)NetTypes.NetMessages.GAME_PACKET, p2.PackForSendingRaw(), MainForm.realPeer);
            }

        }

        private void button17_Click(object sender, EventArgs e)
        {

            /*Thread t = new Thread(doTakeAll);
            t.Start(); */
            // removed for unnecessarity, and confusion. can be reenabled through here but it most likely causes an autoban in real gt servers.
            // incase of reenabling this, set this button to visible, uncomment the code above and set textBox3 to visible as well.
        }

        private void checkBox1_CheckedChanged_2(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                Thread thread = new(new ThreadStart(doTheFastNukaz));
                thread.Start();
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            _ = MessageBox.Show("Growbrew Policies (last updated 16.04.2020):\n" +
                "- GROWBREW IS PERMITTED TO USE ANY HARDWARE IDENTIFIER AND YOUR IP ADDRESS.\n" +
                "- GROWBREW IS NOT RESPONSIBLE FOR BANNED GROWTOPIA ACCOUNTS.\n" +
                "- GROWBREW DOES AND WILL NOT PROVIDE ANY KIND OF PROGRAM RELIABILITY, THERE ARE UPDATES BUT BUGS, MISTAKES AND SUCH MAY OCCUR.\n" +
                "- GROWBREW MAY NOT BE SHARED, IT IS A PAID, PREMIUM PRODUCT.\n" +
                "- GROWBREW HAS THE RIGHTS TO CANCEL YOUR ACCOUNT AT ANY TIME, THIS CAN OCCUR IF THE FOLLOWING RULES WERE BROKEN:\n" +
                "- Reselling growbrew, sharing growbrew, fraud, decompilation/use of code for your own purposes and ban evading incase of a ban.");
        }


        private void checkUnlimitedZoom_CheckedChanged_1(object sender, EventArgs e)
        {
            globalUserData.unlimitedZoom = !globalUserData.unlimitedZoom;
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            globalUserData.serializeWorldsAdvanced = !globalUserData.serializeWorldsAdvanced;
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            globalUserData.redDamageToBlock = !globalUserData.redDamageToBlock;
        }

        private void button16_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog fbd = new();
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                accsDirTextBox.Text = fbd.SelectedPath;
            }
        }

        public struct AccountTable
        {
            public string GrowID;
            public string password;
        }

        private AccountTable ParseAccount(string[] lines)
        {
            AccountTable accTable = new()
            {
                GrowID = "",
                password = ""
            };

            foreach (string line in lines)
            {
                string accstr = line;
                accstr = accstr.Replace(" ", string.Empty); // remove all spaces, they are unnecessary because they arent allowed in passwords/usernames anyway.
                accstr = accstr.ToLower(); // we dont care about lower / upper either.

                if (accstr.StartsWith("tankidname|"))
                {
                    accTable.GrowID = accstr[11..];
                }
                else if (accstr.StartsWith("tankidpass|"))
                {
                    accTable.password = accstr[11..];
                }
                else if (accstr.StartsWith("username:"))
                {
                    accTable.GrowID = accstr[9..];
                }
                else if (accstr.StartsWith("password:"))
                {
                    accTable.password = accstr[9..];
                }
                else if (accstr.StartsWith("user:"))
                {
                    accTable.GrowID = accstr[5..];
                }
                else if (accstr.StartsWith("pass:"))
                {
                    accTable.password = accstr[5..];
                }

                if (accTable.GrowID != "" && accTable.password != "")
                {
                    break;
                }
            }
            return accTable;
        }

        private AccountTable[] ParseAllAccounts(string[] fileLocs)
        {
            // !DEKRAUf teg ssen
            // em pleh slp pleh ni mi
            // ness is an ongoing threat to the community, further actions will be taken.
            List<AccountTable> accTables = new();
            int c = fileLocs.Length;

            for (int i = 0; i < c; i++)
            {
                string fileLoc = fileLocs[i];
                string content = File.ReadAllText(fileLoc);
                string[] lines = content.Split('\n');
                accTables.Add(ParseAccount(lines));

            }


            return accTables.ToArray();
        }

        private void startaccCheck_Click(object sender, EventArgs e)
        {
            // ^^ released to slowdown ness's BOOMING business.
            _ = Directory.GetFiles(accsDirTextBox.Text);

        }

        private void button20_Click(object sender, EventArgs e)
        {
            _ = MessageBox.Show("How to use:\n" +
                "1. Click start http server (make sure you have no other app running on port 80 like another http server on your device)\n" +
                "2. Put this into your hosts file (in C:\\Windows\\System32\\drivers\\etc):\n" +
                "127.0.0.1 growtopia1.com\n" +
                "127.0.0.1 growtopia2.com\n" +
                "3. Click Start the proxy! If everything succeeded, the orange text in top left will both turn to green.\n" +
                "4. Normally connect to GT, and you have the proxy installed successfully!\n" +
                "TIP: To enable AAP Bypass, set your mac to 02:00:00:00:00:00 in Cheat Extra tab.\n" +
                "TIP: Compile in Release x64 (if you have a 64bit processor and OS) for most performance, if you don't have x64, compile in Release Any CPU!");
        }

        private void itemIDBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void itemIDBox_TextChanged(object sender, EventArgs e)
        {
            _ = int.TryParse(itemIDBox.Text, out int item);

            ItemDatabase.ItemDefinition itemDef = ItemDatabase.GetItemDef(item);
            detItemLabel.Text = "Detected Item: " + itemDef.itemName;
        }

        private void doDropAllInventory()
        {
            Inventory inventory = messageHandler.worldMap.player.inventory;

            if (inventory.items == null)
            {
                Console.WriteLine("inventory.items was null!");
                return;
            }

            int ctr = 0;
            bool swap = false;

            foreach (InventoryItem item in inventory.items)
            {
                ctr++;

                TankPacket tp = new()
                {
                    XSpeed = 32
                };

                if ((ctr % 24) == 0)
                {
                    swap = !swap;
                    Thread.Sleep(400);
                    if (swap)
                    {
                        if (globalUserData.isFacingSwapped)
                        {
                            messageHandler.worldMap.player.X -= 32;
                        }
                        else
                        {
                            messageHandler.worldMap.player.X += 32;
                        }
                    }
                    else
                    {
                        if (globalUserData.isFacingSwapped)
                        {
                            messageHandler.worldMap.player.X += 32;
                        }
                        else
                        {
                            messageHandler.worldMap.player.X -= 32;
                        }
                    }
                }

                tp.X = messageHandler.worldMap.player.X;
                tp.Y = messageHandler.worldMap.player.Y;


                messageHandler.packetSender.SendPacketRaw(4, tp.PackForSendingRaw(), realPeer);


                messageHandler.packetSender.SendPacket(2, "action|drop\nitemID|" + item.itemID.ToString() + "|\n", realPeer);
                // Console.WriteLine($"Dropping item with ID: {item.itemID} with amount: {item.amount}");
                string str = "action|dialog_return\n" +
                    "dialog_name|drop_item\n" +
                    "itemID|" + item.itemID.ToString() + "|\n" +
                    "count|" + item.amount.ToString() + "\n";

                messageHandler.packetSender.SendPacket(2, str, realPeer);
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            // drop all inventory items
            _ = Task.Run(() => doDropAllInventory());
        }

        private void checkBox2_CheckedChanged_1(object sender, EventArgs e)
        {
            globalUserData.blockEnterGame = !globalUserData.blockEnterGame;
        }

        private void ignoresetback_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void ignoresetback_CheckedChanged_1(object sender, EventArgs e)
        {
            globalUserData.ignoreonsetpos = !globalUserData.ignoreonsetpos;
        }

        private void button21_Click(object sender, EventArgs e)
        {

        }

        private void button21_Click_1(object sender, EventArgs e)
        {
            if (realPeer != null)
            {
                if (realPeer.State == ENetPeerState.Connected)
                {
                    realPeer.Reset();
                }
            }
        }

        private void entireLog_TextChanged(object sender, EventArgs e)
        {

        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {

        }

        private void aboutlabel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void spamIntervalBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void spammerTimer_Tick(object sender, EventArgs e)
        {

            if (randomizeIntervalCheckbox.Checked)
            {
                spammerTimer.Interval = int.Parse(spamIntervalBox.Text) + random.Next(0, 2500);
            }

            if (realPeer.State == ENetPeerState.Connected)
            {
                messageHandler.packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, "action|input\n|text|" + spamtextBox.Text, realPeer);
            }
        }

        private void spamStartStopBtn_Click(object sender, EventArgs e)
        {
            if (spammerTimer.Enabled)
            {
                spammerTimer.Stop();
                spamStartStopBtn.Text = "Start";
            }
            else
            {
                spammerTimer.Interval = int.Parse(spamIntervalBox.Text);
                spammerTimer.Start();
                spamStartStopBtn.Text = "Stop";
            }
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox9_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void button22_Click(object sender, EventArgs e)
        {
            BSONObject bsonObj = new()
            {
                ["cfg_version"] = 2,
                ["disable_advanced_world_loading"] = checkBox7.Checked,
                ["unlimited_zoom"] = checkUnlimitedZoom.Checked,
                ["block_enter_game"] = checkBox2.Checked,
                ["append_netiduserid_to_names"] = checkAppendNetID.Checked,
                ["ignore_position_setback"] = ignoresetback.Checked,
                ["instant_world_menu_skip_cache"] = checkBox6.Checked,
                ["block_item_collect"] = checkBox9.Checked
            };
            File.WriteAllBytes("stored/config.gbrw", SimpleBSON.Dump(bsonObj));
            _ = MessageBox.Show("Exported to stored/config.gbrw!");
        }

        private void checkBox9_CheckedChanged_2(object sender, EventArgs e)
        {
            globalUserData.blockCollecting = !globalUserData.blockCollecting;
        }

        private void logallpackets_CheckedChanged(object sender, EventArgs e)
        {
            logallpackettypes = !logallpackettypes;
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            globalUserData.cheat_Autofarm_magplant_mode = !globalUserData.cheat_Autofarm_magplant_mode;
        }

        private void startFromOwnTilePos_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void saveIfGemCount_CheckedChanged(object sender, EventArgs e)
        {
            //Multibot.SaveGemCount8K = !Multibot.SaveGemCount8K;
        }

        private void saveIfTokenCount_CheckedChanged(object sender, EventArgs e)
        {
            //Multibot.SaveGrowtokenCount9 = !Multibot.SaveGrowtokenCount9;
        }

        private void saveIfWlCount_CheckedChanged(object sender, EventArgs e)
        {
            //Multibot.SaveWLCountOver10 = !Multibot.SaveWLCountOver10;
        }

        private void enableMultibotcheck_CheckedChanged(object sender, EventArgs e)
        {
            multibottingEnabled = !multibottingEnabled;
        }

        private void addBotAcc_Click(object sender, EventArgs e)
        {


        }

        private void disableSilentReconnect_CheckedChanged(object sender, EventArgs e)
        {
            globalUserData.enableSilentReconnect = !globalUserData.enableSilentReconnect;
        }

        private void button23_Click(object sender, EventArgs e)
        {
            _ = MessageBox.Show("Super Multibotting: What is it?\n" +
                "Super Multibotting enables the MIRRORING of your proxy packets in a functioning way to all 'dumb bots', turning them into smart ones.\n" +
                "Shortly said: All bots will start to do what you do, move where you go, enter doors where you do, enter worlds where you go, break/place where you do and chat what you type. ");
        }

        private void cheattabs_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label33_Click(object sender, EventArgs e)
        {

        }

        private void broadcastIconStatus_CheckedChanged(object sender, EventArgs e)
        {
            if (broadcastIconStatus.Checked)
            {
                modifyIconStatusTimer.Start();
            }
            else
            {
                modifyIconStatusTimer.Stop();
            }
        }

        private int currentIconStatus = 0;
        private void modifyIconStatusTimer_Tick(object sender, EventArgs e)
        {
            Player[] players = messageHandler.worldMap.players.ToArray();

            TankPacket tStruct = new()
            {
                PacketType = (int)NetTypes.PacketTypes.ICON_STATE,
                PunchX = currentIconStatus++
            };


            if (currentIconStatus > 2)
            {
                currentIconStatus = 0;
            }

            foreach (Player p in players)
            {
                tStruct.NetID = p.netID;
                messageHandler.packetSender.SendPacketRaw((int)NetTypes.NetMessages.GAME_PACKET, tStruct.PackForSendingRaw(), MainForm.realPeer);
            }
        }

        private void reapplyLockBtn_Click(object sender, EventArgs e)
        {

            TankPacket tPacket = new()
            {
                PacketType = (int)NetTypes.PacketTypes.TILE_CHANGE_REQ,
                MainValue = 32,
                PunchX = globalUserData.lastWrenchX,
                PunchY = globalUserData.lastWrenchY,
                X = messageHandler.worldMap.player.X,
                Y = messageHandler.worldMap.player.Y,
                ExtDataMask = 16
            };

            string dlg = "action|dialog_return\n" +
                   "dialog_name|lock_edit\n" +
                   $"tilex|{globalUserData.lastWrenchX}|\n" +
                   $"tiley|{globalUserData.lastWrenchY}|\n" +
                   "buttonClicked|recalcLock\n" +
                   "checkbox_public|0\n" +
                   "checkbox_ignore|1\n";

            for (int i = 0; i < 5; i++)
            {
                messageHandler.packetSender.SendPacketRaw((int)NetTypes.NetMessages.GAME_PACKET, tPacket.PackForSendingRaw(), MainForm.realPeer);
                messageHandler.packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, dlg, MainForm.realPeer);
                Thread.Sleep(10);
            }
        }

        private readonly int rn = 0;

        private void doCrash()
        {

        }
        private void button25_Click(object sender, EventArgs e)
        {
            TankPacket tPacket = new()
            {
                PacketType = 46
            };


            Player[] players = messageHandler.worldMap.players.ToArray();
            for (int i = 0; i < 100000; i++)
            {
                foreach (Player p in players)
                {
                    int tileX = p.X / 32, tileY = p.Y / 32;

                    tPacket.X = tileX;
                    tPacket.Y = tileY;
                    tPacket.NetID = p.netID;
                    tPacket.SecondaryNetID = tileX;
                    tPacket.ExtDataMask = tileY;
                    tPacket.MainValue = 3728;
                    tPacket.PunchX = tileX;
                    tPacket.PunchY = tileY;
                    //rn++;
                    messageHandler.packetSender.SendPacketRaw(4, tPacket.PackForSendingRaw(), MainForm.realPeer, ENetPacketFlags.Reliable);

                    //Thread.Sleep(100);
                }
            }

        }

        private void annoyPlayers_Tick(object sender, EventArgs e)
        {
            TankPacket tPacket = new();
            int tileX = messageHandler.worldMap.player.X / 32, tileY = (messageHandler.worldMap.player.Y / 32) + 1;

            tPacket.PacketType = 46;
            tPacket.X = tileX;
            tPacket.Y = tileY;
            tPacket.NetID = 0;
            tPacket.SecondaryNetID = tileX;
            tPacket.ExtDataMask = tileY;
            tPacket.MainValue = 3728;
            tPacket.PunchX = tileX;
            tPacket.PunchY = tileY;

            Player[] players = messageHandler.worldMap.players.ToArray();



            messageHandler.packetSender.SendPacketRaw(4, tPacket.PackForSendingRaw(), MainForm.realPeer, ENetPacketFlags.Reliable);
            foreach (Player p in players)
            {

                tileX = p.X / 32;
                tileY = p.Y / 32;

                tPacket.X = tileX;
                tPacket.Y = tileY;
                tPacket.NetID = p.netID;
                tPacket.SecondaryNetID = tileX;
                tPacket.ExtDataMask = tileY;
                tPacket.MainValue = 3728;
                tPacket.PunchX = tileX;
                tPacket.PunchY = tileY;

                byte[] packet = tPacket.PackForSendingRaw();
                //rn++;

                messageHandler.packetSender.SendPacketRaw(4, packet, MainForm.realPeer, ENetPacketFlags.Reliable);

                //Thread.Sleep(100);

            }
            MainForm.realPeer.Ping();
        }

        private void annoyPlayerBox_CheckedChanged(object sender, EventArgs e)
        {
            if (annoyPlayerBox.Checked)
            {
                annoyPlayers.Start();
            }
            else
            {
                annoyPlayers.Stop();
            }
        }

        private void enableAutoReconnect_CheckedChanged(object sender, EventArgs e)
        {
            globalUserData.enableAutoreconnect = enableAutoReconnectBox.Checked;
        }

        private void autoEnterWorldBox_CheckedChanged(object sender, EventArgs e)
        {
            globalUserData.autoEnterWorld = autoEnterWorldBox.Checked ? autoWorldTextBox.Text : "";
        }

        private void autoWorldTextBox_TextChanged(object sender, EventArgs e)
        {
            if (autoEnterWorldBox.Checked)
            {
                globalUserData.autoEnterWorld = autoWorldTextBox.Text;
            }
        }

        private void ridUpdate_Click(object sender, EventArgs e)
        {
            globalUserData.rid = setRID.Text;
        }

        private void sidUpdate_Click(object sender, EventArgs e)
        {
            globalUserData.sid = setSID.Text;
        }

        private void spawnParticleBtn_Click(object sender, EventArgs e)
        {
            TankPacket datx = new()
            {
                PacketType = (int)NetTypes.PacketTypes.PARTICLE_EFF,
                X = messageHandler.worldMap.player.X,
                Y = messageHandler.worldMap.player.Y,
                YSpeed = (int)patUpDown.Value,
                XSpeed = (float)patSizeUpDown.Value,
                MainValue = (int)patDelayUpDown.Value
            };
            messageHandler.packetSender.SendPacketRaw(4, datx.PackForSendingRaw(), MainForm.proxyPeer);
        }

        private void patTrackBar_ValueChanged(object sender, EventArgs e)
        {
            patUpDown.Value = patTrackBar.Value;

            spawnParticleBtn.PerformClick();
        }


        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 1)]
        private struct CharacterExtraMods
        {
            [FieldOffset(0)] public byte cancel; // they act as bits
            [FieldOffset(0)] public byte dash;
            [FieldOffset(0)] public byte jump;
        }

        public static unsafe byte[] ConvertToBytes<T>(T value) where T : unmanaged
        {
            byte* pointer = (byte*)&value;

            byte[] bytes = new byte[sizeof(T)];
            for (int i = 0; i < sizeof(T); i++)
            {
                bytes[i] = pointer[i];
            }

            return bytes;
        }

        private void button12_Click_1(object sender, EventArgs e)
        {
        }

        private void UpdateItemPriceBox(bool showAll, bool doInv = false)
        {
            PriceChecker.ItemPriceList iPriceList = PriceChecker.GetItemPriceListFromUrl("http://168.119.93.204/"); // giving this away free for 1 month in opensrc / free version, but after that you will require to use paid. Constantly updated.

            string toDisplay = "";
            double invWealth = -1;

            PriceChecker.ItemPrice[] fetchedItems = null;

            if (!doInv)
            {
                fetchedItems = showAll ? iPriceList.itemPrices.ToArray() : iPriceList.FindByNameIgnoreCase(findPriceByNameBox.Text);
            }
            else
            {
                invWealth = 0;
                Inventory inven = messageHandler.worldMap.player.inventory;
                List<PriceChecker.ItemPrice> invFetchedItems = new();

                if (inven.items != null)
                {
                    foreach (InventoryItem invItem in inven.items)
                    {
                        //MessageBox.Show(invItem.itemID.ToString());
                        string itemName = ItemDatabase.GetItemDef(invItem.itemID).itemName;

                        invFetchedItems.AddRange(iPriceList.FindByNameIgnoreCase(itemName, true));
                    }
                }
                fetchedItems = invFetchedItems.ToArray();
            }

            if (fetchedItems != null)
            {
                foreach (PriceChecker.ItemPrice iPrice in fetchedItems)
                {
                    toDisplay += $"Item: '{iPrice.name}' with quantity: '{iPrice.quantity}' costs: '{iPrice.price} WLS'\n";

                    if (doInv)
                    {
                        invWealth += iPrice.price / iPrice.quantity;
                    }
                }
            }

            if (itemPricesBox.InvokeRequired || loadingPricesLabel.InvokeRequired || inventoryWealthLabel.InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    loadingPricesLabel.Visible = false;
                    itemPricesBox.Text = toDisplay;
                    if (invWealth > -1)
                    {
                        inventoryWealthLabel.Text = $"Your inventory is worth (rounded in WLS): {(int)Math.Round(invWealth)}";
                    }
                }));
            }
            else
            {
                loadingPricesLabel.Visible = false;
                itemPricesBox.Text = toDisplay;
                if (invWealth > -1)
                {
                    inventoryWealthLabel.Text = $"Your inventory is worth (rounded in WLS): {(int)Math.Round(invWealth)}";
                }
            }
        }

        private void searchBtn_Click(object sender, EventArgs e)
        {
            loadingPricesLabel.Visible = true;
            _ = Task.Run(() => UpdateItemPriceBox(showAllPricesBox.Checked, inventoryWealthCheckbox.Checked));
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {

        }

        // credits stackoverflow
        private void OpenUrl(string url)
        {
            try
            {
                _ = Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    _ = Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    _ = Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _ = Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        private void ytlinklabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenUrl("https://youtube.com/channel/UC0htMnKS9EGPlaeIkcVkxhw");
        }

        private void showAllPricesBox_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void syncPricesBtn_Click(object sender, EventArgs e)
        {
            if (!loadingPricesLabel.Visible)
            {
                string raw = PriceChecker.RefreshPrices("http://168.119.93.204/");
                PriceChecker.iPriceList = PriceChecker.ItemPriceList.Deserialize(raw);
                _ = MessageBox.Show("Updated item prices successfully!");
            }
            else
            {
                _ = MessageBox.Show("Items are currently loading internally, please try again when 'Loading...' is gone!");
            }
        }

        private void dontSerializeInvBox_CheckedChanged(object sender, EventArgs e)
        {
            globalUserData.dontSerializeInventory = dontSerializeInvBox.Checked;
        }

        private void inventoryWealthCheckbox_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void skipGazetteBox_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void doAutofarm(int itemID, bool remote_mode = false, bool oneblockmode = false, bool selfblockstart = false)
        {
            bool isBg = ItemDatabase.isBackground(itemID);
            while (globalUserData.isAutofarming)
            {
                Thread.Sleep(10);

                if (realPeer != null)
                {
                    if (realPeer.State != ENetPeerState.Connected)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    int c = 2 - (oneblockmode ? 1 : 0);


                    for (int i = 0; i < c; i++)
                    {
                        int x, y;
                        x = messageHandler.worldMap.player.X / 32;
                        y = messageHandler.worldMap.player.Y / 32;

                        if (globalUserData.isFacingSwapped)
                        {
                            if (i == 0)
                            {
                                x -= 1;
                            }

                            if (i == 1)
                            {
                                x -= 2;
                            }

                            if (selfblockstart)
                            {
                                x++;
                            }
                        }
                        else
                        {
                            if (i == 0)
                            {
                                x += 1;
                            }

                            if (i == 1)
                            {
                                x += 2;
                            }

                            if (selfblockstart)
                            {
                                x--;
                            }
                        }


                        Thread.Sleep(166);

                        if (messageHandler.worldMap == null)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        TankPacket tkPt = new()
                        {
                            PunchX = x,
                            PunchY = y
                        };

                        ushort fg = messageHandler.worldMap.tiles[x + (y * messageHandler.worldMap.width)].fg;
                        ushort bg = messageHandler.worldMap.tiles[x + (y * messageHandler.worldMap.width)].bg;

                        tkPt.MainValue = isBg ? bg != 0 ? 18 : itemID : fg == itemID ? 18 : itemID;

                        if (remote_mode && tkPt.MainValue != 18)
                        {
                            tkPt.MainValue = 5640;
                        }

                        tkPt.X = messageHandler.worldMap.player.X;
                        tkPt.Y = messageHandler.worldMap.player.Y;
                        tkPt.ExtDataMask &= ~0x04;
                        tkPt.ExtDataMask &= ~0x40;
                        tkPt.ExtDataMask &= ~0x10000;
                        tkPt.NetID = -1;

                        // TODO THREAD SAFETY
                        //messageHandler.packetSender.SendPacketRaw(4, tkPt.PackForSendingRaw(), realPeer); no need for this
                        tkPt.NetID = -1;
                        tkPt.PacketType = 3;
                        tkPt.ExtDataMask = 0;
                        messageHandler.packetSender.SendPacketRaw(4, tkPt.PackForSendingRaw(), realPeer);

                    }
                }
            }
        }



        private void startAutofarmBtn_Click(object sender, EventArgs e)
        {
            globalUserData.isAutofarming = !globalUserData.isAutofarming;
            if (globalUserData.isAutofarming)
            {
                _ = Task.Run(() => doAutofarm(int.Parse(itemIDBox.Text), globalUserData.cheat_Autofarm_magplant_mode, oneblockmode.Checked, startFromOwnTilePos.Checked));

                startAutofarmBtn.Text = "Stop autofarming";
            }
            else
            {
                startAutofarmBtn.Text = "Start autofarming";
            }
        }
    }
}
