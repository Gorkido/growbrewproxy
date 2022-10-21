﻿using ENet.Managed;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GrowbrewProxy
{
    public class HandleMessages
    {
        private delegate void SafeCallDelegate(string text);
        public PacketSending packetSender = new();
        private readonly VariantList variant = new();

        public World worldMap = new();
        private bool isSwitchingServers = false;
        public bool enteredGame = false;
        public bool serverRelogReq = false;

        private int checkPeerUsability(ENetPeer peer)
        {
            return peer.IsNull ? -1 : peer.State != ENetPeerState.Connected ? -3 : 0;
        }

        private void LogDebugFile(string text)
        {
#if DEBUG
            File.AppendAllText("debuglogs.txt", text);
#endif

        }

        private NetTypes.NetMessages GetMessageType(byte[] data)
        {
            uint messageType = uint.MaxValue - 1;
            if (data.Length > 4)
            {
                messageType = BitConverter.ToUInt32(data, 0);
            }

            return (NetTypes.NetMessages)messageType;
        }

        private NetTypes.PacketTypes GetPacketType(byte[] packetData)
        {
            return (NetTypes.PacketTypes)packetData[0]; // additional data will be located at 1, 2, not required for packet type tho.
        }


        /*
         **ONSENDTOSERVER INDEXES/VALUE LOCATIONS**
            port = 1
            token = 2
            userId = 3
            IPWithExtraData = 4
            lmode = 5 (Used for determining how client should behave when leaving, and could also influence the connection after.
            */
        private int OperateVariant(VariantList.VarList vList, object botPeer)
        {
            switch (vList.FunctionName)
            {
                case "OnConsoleMessage":
                    {
                        string m = (string)vList.functionArgs[1];
                        if ((m.Contains("lagged out,") || m.Contains("experiencing high load")) && !m.Contains("<") && !m.Contains("["))
                        {
                            GamePacketProton variantPacket2 = new();
                            variantPacket2.AppendString("OnReconnect");
                            packetSender.SendData(variantPacket2.GetBytes(), MainForm.proxyPeer);
                        }


                        break;
                    }
                case "OnRequestWorldSelectMenu":
                    {
                        if (MainForm.globalUserData.autoEnterWorld != "")
                        {
                            packetSender.SendPacket(3, "action|join_request\nname|" + MainForm.globalUserData.autoEnterWorld, MainForm.realPeer);
                        }
                        break;
                    }
                case "OnSuperMainStartAcceptLogonHrdxs47254722215a":
                    {

                        if (MainForm.skipCache && botPeer == null)
                        {
                            MainForm.LogText += "[" + DateTime.UtcNow + "] (CLIENT): Skipping potential caching (will make world list disappear)...";
                            GamePacketProton gp = new(); // variant list
                            gp.AppendString("OnRequestWorldSelectMenu");
                            packetSender.SendData(gp.GetBytes(), MainForm.proxyPeer);
                        }
                        if (botPeer != null)
                        {
                            Console.WriteLine("BOT PEER IS ENTERING THE GAME...");
                            packetSender.SendPacket(3, "action|enter_game\n", (ENetPeer)botPeer);
                        }
                        return -1;
                    }
                case "OnZoomCamera":
                    {
                        MainForm.LogText += "[" + DateTime.UtcNow + "] (SERVER): Camera zoom parameters (" + vList.functionArgs.Length + "): v1: " + ((float)vList.functionArgs[1] / 1000).ToString() + " v2: " + vList.functionArgs[2].ToString();
                        return -1;
                    }
                case "onShowCaptcha":
                    _ = ((string)vList.functionArgs[1]).Replace("PROCESS_LOGON_PACKET_TEXT_42", "");// make captcha completable
                    try
                    {
                        string[] lines = ((string)vList.functionArgs[1]).Split('\n');
                        foreach (string line in lines)
                        {
                            if (line.Contains("+"))
                            {
                                string line2 = line.Replace(" ", "");
                                int a1, a2;
                                string[] splitByPipe = line2.Split('|');
                                string[] splitByPlus = splitByPipe[1].Split('+');
                                a1 = int.Parse(splitByPlus[0]);
                                a2 = int.Parse(splitByPlus[1]);
                                int result = a1 + a2;
                                string resultingPacket = "action|dialog_return\ndialog_name|captcha_submit\ncaptcha_answer|" + result.ToString() + "\n";
                                packetSender.SendPacket(2, resultingPacket, MainForm.realPeer);
                            }
                        }
                        return -1;
                    }
                    catch
                    {
                        return -1; // Give this to user.
                    }
                case "OnDialogRequest":
                    MainForm.LogText += "[" + DateTime.UtcNow + "] (SERVER): OnDialogRequest called, logging its params here:\n" +
                           (string)vList.functionArgs[1] + "\n";
                    if (!((string)vList.functionArgs[1]).ToLower().Contains("captcha"))
                    {
                        return -1; // Send Client Dialog
                    }
                    _ = ((string)vList.functionArgs[1]).Replace("PROCESS_LOGON_PACKET_TEXT_42", "");// make captcha completable
                    try
                    {
                        string[] lines = ((string)vList.functionArgs[1]).Split('\n');
                        foreach (string line in lines)
                        {
                            if (line.Contains("+"))
                            {
                                string line2 = line.Replace(" ", "");
                                int a1, a2;
                                string[] splitByPipe = line2.Split('|');
                                string[] splitByPlus = splitByPipe[1].Split('+');
                                a1 = int.Parse(splitByPlus[0]);
                                a2 = int.Parse(splitByPlus[1]);
                                int result = a1 + a2;
                                string resultingPacket = "action|dialog_return\ndialog_name|captcha_submit\ncaptcha_answer|" + result.ToString() + "\n";
                                packetSender.SendPacket(2, resultingPacket, MainForm.realPeer);
                            }
                        }
                        return -1;
                    }
                    catch
                    {
                        return -1; // Give this to user.
                    }

                case "OnSendToServer":
                    {
                        // TODO FIX THIS AND MIRROR ALL PACKETS AND SOME BUG FIXES.

                        string ip = (string)vList.functionArgs[4];
                        string doorid = "";

                        if (ip.Contains("|"))
                        {
                            doorid = ip[(ip.IndexOf("|") + 1)..];
                            ip = ip[..ip.IndexOf("|")];
                        }

                        int port = (int)vList.functionArgs[1];
                        int userID = (int)vList.functionArgs[3];
                        int token = (int)vList.functionArgs[2];

                        MainForm.LogText += "[" + DateTime.UtcNow + "] (SERVER): OnSendToServer (func call used for server switching/sub-servers) " +
                                "IP: " +
                                ip + " PORT: " + port
                                + " UserId: " + userID
                                + " Session-Token: " + token + "\n";
                        GamePacketProton variantPacket = new();
                        variantPacket.AppendString("OnConsoleMessage");
                        variantPacket.AppendString("`6(PROXY)`o Switching subserver...``");
                        packetSender.SendData(variantPacket.GetBytes(), MainForm.proxyPeer);


                        MainForm.globalUserData.Growtopia_IP = token < 0 ? MainForm.globalUserData.Growtopia_Master_IP : ip;
                        MainForm.globalUserData.Growtopia_Port = token < 0 ? MainForm.globalUserData.Growtopia_Master_Port : port;
                        MainForm.globalUserData.isSwitchingServer = true;
                        MainForm.globalUserData.token = token;
                        MainForm.globalUserData.lmode = 1;
                        MainForm.globalUserData.userID = userID;
                        MainForm.globalUserData.doorid = doorid;

                        packetSender.SendPacket(3, "action|quit", MainForm.realPeer);
                        MainForm.realPeer.Disconnect(0);

                        return -1;
                    }
                case "OnSpawn":
                    {
                        worldMap.playerCount++;
                        string onspawnStr = (string)vList.functionArgs[1];
                        //MessageBox.Show(onspawnStr);
                        _ = onspawnStr.Split('|');
                        Player p = new();
                        string[] lines = onspawnStr.Split('\n');

                        bool localplayer = false;

                        foreach (string line in lines)
                        {
                            string[] lineToken = line.Split('|');
                            if (lineToken.Length != 2)
                            {
                                continue;
                            }

                            switch (lineToken[0])
                            {
                                case "netID":
                                    p.netID = Convert.ToInt32(lineToken[1]);
                                    break;
                                case "userID":
                                    p.userID = Convert.ToInt32(lineToken[1]);
                                    break;
                                case "name":
                                    p.name = lineToken[1];
                                    break;
                                case "country":
                                    p.country = lineToken[1];
                                    break;
                                case "invis":
                                    p.invis = Convert.ToInt32(lineToken[1]);
                                    break;
                                case "mstate":
                                    p.mstate = Convert.ToInt32(lineToken[1]);
                                    break;
                                case "smstate":
                                    p.mstate = Convert.ToInt32(lineToken[1]);
                                    break;
                                case "posXY":
                                    if (lineToken.Length == 3) // exactly 3 not more not less
                                    {
                                        p.X = Convert.ToInt32(lineToken[1]);
                                        p.Y = Convert.ToInt32(lineToken[2]);
                                    }
                                    break;
                                case "type":
                                    if (lineToken[1] == "local")
                                    {
                                        localplayer = true;
                                    }

                                    break;

                            }
                        }
                        //MainForm.LogText += ("[" + DateTime.UtcNow + "] (PROXY): " + onspawnStr);
                        worldMap.players.Add(p);
                        if (p.name.Length > 2)
                        {
                            worldMap.AddPlayerControlToBox(p);
                        }


                        /*if (p.name.Contains(MainForm.tankIDName))
                        {
                           
                        }*/ //crappy code

                        if (p.mstate > 0 || p.smstate > 0 || p.invis > 0)
                        {
                            if (MainForm.globalUserData.cheat_autoworldban_mod)
                            {
                                banEveryoneInWorld();
                            }

                            MainForm.LogText += "[" + DateTime.UtcNow + "] (PROXY): A moderator or developer seems to have joined your world!\n";
                        }

                        if (localplayer)
                        {
                            string lestring = (string)vList.functionArgs[1];

                            string[] avatardata = lestring.Split('\n');
                            string modified_avatardata = string.Empty;

                            foreach (string av in avatardata)
                            {
                                if (av.Length <= 0)
                                {
                                    continue;
                                }

                                string key = av[..av.IndexOf('|')];
                                string value = av[(av.IndexOf('|') + 1)..];

                                switch (key)
                                {
                                    case "mstate": // unlimited punch/place range edit smstate
                                        value = "1";
                                        break;
                                }

                                modified_avatardata += key + "|" + value + "\n";
                            }

                            //lestring = lestring.Replace("mstate|0", "mstate|1");

                            if (MainForm.globalUserData.unlimitedZoom)
                            {
                                GamePacketProton gp = new();
                                gp.AppendString("OnSpawn");
                                gp.AppendString(modified_avatardata);
                                gp.delay = (int)vList.delay;
                                gp.NetID = vList.netID;

                                packetSender.SendData(gp.GetBytes(), MainForm.proxyPeer);
                            }

                            MainForm.LogText += "[" + DateTime.UtcNow + "] (PROXY): World player objects loaded! Your NetID:  " + p.netID + " -- Your UserID: " + p.userID + "\n";
                            worldMap.netID = p.netID;
                            worldMap.userID = p.userID;
                            return -2;
                        }
                        else
                        {
                            return p.netID;
                        }
                    }
                case "OnRemove":
                    {
                        string onremovestr = (string)vList.functionArgs[1];
                        string[] lineToken = onremovestr.Split('|');
                        if (lineToken[0] != "netID")
                        {
                            break;
                        }

                        _ = int.TryParse(lineToken[1], out int netID);
                        for (int i = 0; i < worldMap.players.Count; i++)
                        {
                            if (worldMap.players[i].netID == netID)
                            {
                                worldMap.players.RemoveAt(i);
                                break;
                            }
                        }
                        worldMap.RemovePlayerControl(netID);

                        return netID;
                    }
                default:
                    return -1;
            }
            return 0;
        }

        private string GetProperGenericText(byte[] data)
        {
            string growtopia_text = string.Empty;
            if (data.Length > 5)
            {
                int len = data.Length - 5;
                byte[] croppedData = new byte[len];
                Array.Copy(data, 4, croppedData, 0, len);
                growtopia_text = Encoding.ASCII.GetString(croppedData);
            }
            return growtopia_text;
        }

        private void SwitchServers(ref ENetPeer peer, string ip, int port, int lmode = 0, int userid = 0, int token = 0)
        {
            MainForm.globalUserData.Growtopia_IP = token < 0 ? MainForm.globalUserData.Growtopia_Master_IP : ip;
            MainForm.globalUserData.Growtopia_Port = token < 0 ? MainForm.globalUserData.Growtopia_Master_Port : port;
            isSwitchingServers = true;

            MainForm.ConnectToServer(ref peer, MainForm.globalUserData);
        }

        private void banEveryoneInWorld()
        {
            foreach (Player p in worldMap.players)
            {
                string pName = p.name[2..];
                pName = pName[..^2];
                packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, "action|input\n|text|/ban " + pName, MainForm.realPeer);
            }
        }

        private bool IsBitSet(int b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }



        public string HandlePacketFromClient(ref ENetPeer peer, ENetPacket packet) // Why string? Oh yeah, it's the best thing to also return a string response for anything you want!
        {

            if (peer.IsNull)
            {
                return "";
            }

            if (peer.State != ENetPeerState.Connected)
            {
                return "";
            }

            if (MainForm.realPeer.IsNull)
            {
                return "";
            }

            if (MainForm.realPeer.State != ENetPeerState.Connected)
            {
                return "";
            }

            byte[] data = packet.Data.ToArray();

            string log = string.Empty;



            switch ((NetTypes.NetMessages)data[0])
            {
                case NetTypes.NetMessages.GENERIC_TEXT:
                    string str = GetProperGenericText(data);

                    MainForm.LogText += "[" + DateTime.UtcNow + "] (CLIENT): String package fetched (GENERIC_TEXT):\n" + str + "\n";
                    if (str.StartsWith("action|"))
                    {
                        string actionExecuted = str[7..];
                        string inputPH = "input\n|text|";
                        if (actionExecuted.StartsWith("enter_game"))
                        {
                            if (MainForm.globalUserData.blockEnterGame)
                            {
                                return "Blocked enter_game packet!";
                            }

                            enteredGame = true;
                        }
                        else if (actionExecuted.StartsWith(inputPH))
                        {

                            string text = actionExecuted[inputPH.Length..];

                            if (text.Length > 0)
                            {
                                if (text.StartsWith("/")) // bAd hAcK - but also lazy, so i'll be doing this.
                                {

                                    switch (text)
                                    {
                                        case "/banworld":
                                            {
                                                banEveryoneInWorld();
                                                return "called /banworld, attempting to ban everyone who is in world (requires admin/owner)";
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // for (int i = 0; i < 1000; i++) packetSender.SendPacket(2, "action|refresh_item_data\n", MainForm.realPeer);
                        string[] lines = str.Split('\n');

                        string tankIDName = "";
                        foreach (string line in lines)
                        {
                            string[] lineToken = line.Split('|');
                            if (lineToken.Length != 2)
                            {
                                continue;
                            }

                            switch (lineToken[0])
                            {
                                case "tankIDName":
                                    tankIDName = lineToken[1];
                                    break;
                                case "tankIDPass":
                                    MainForm.globalUserData.tankIDPass = lineToken[1];
                                    break;
                                case "requestedName":
                                    MainForm.globalUserData.requestedName = lineToken[1];
                                    break;
                                case "token":
                                    MainForm.globalUserData.token = int.Parse(lineToken[1]);
                                    break;
                                case "user":
                                    MainForm.globalUserData.userID = int.Parse(lineToken[1]);
                                    break;
                                case "lmode":
                                    MainForm.globalUserData.lmode = int.Parse(lineToken[1]);
                                    break;

                            }
                        }
                        MainForm.globalUserData.tankIDName = tankIDName;
                        packetSender.SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, MainForm.CreateLogonPacket(), MainForm.realPeer);
                        return "Sent logon packet!"; // handling logon over proxy
                    }
                    break;
                case NetTypes.NetMessages.GAME_MESSAGE:
                    string str2 = GetProperGenericText(data);
                    MainForm.LogText += "[" + DateTime.UtcNow + "] (CLIENT): String package fetched (GAME_MESSAGE):\n" + str2 + "\n";
                    if (str2.StartsWith("action|"))
                    {
                        string actionExecuted = str2[7..];
                        if (actionExecuted.StartsWith("quit") && !actionExecuted.StartsWith("quit_to_exit"))
                        {

                            // super multibotting will not mirror all packets in here (the "quit" action), cuz i found it unnecessary, although, you can enable that by pasting the code that does it.
                            MainForm.globalUserData.token = -1;
                            MainForm.globalUserData.Growtopia_IP = MainForm.globalUserData.Growtopia_Master_IP;
                            MainForm.globalUserData.Growtopia_Port = MainForm.globalUserData.Growtopia_Master_Port;

                            if (MainForm.realPeer != null)
                            {
                                if (!MainForm.realPeer.IsNull)
                                {
                                    if (MainForm.realPeer.State != ENetPeerState.Disconnected)
                                    {
                                        MainForm.realPeer.Disconnect(0);
                                    }
                                }
                            }
                            if (MainForm.proxyPeer != null)
                            {
                                if (!MainForm.proxyPeer.IsNull)
                                {
                                    if (MainForm.proxyPeer.State == ENetPeerState.Connected)
                                    {
                                        MainForm.proxyPeer.Disconnect(0);
                                    }
                                }
                            }
                        }
                        else if (actionExecuted.StartsWith("join_request\nname|")) // ghetto fetching of worldname
                        {
                            string[] rest = actionExecuted[18..].Split('\n');
                            string joinWorldName = rest[0];
                            Console.WriteLine($"Joining world {joinWorldName}...");
                        }
                    }
                    break;
                case NetTypes.NetMessages.GAME_PACKET:
                    {
                        TankPacket p = TankPacket.UnpackFromPacket(data);

                        switch ((NetTypes.PacketTypes)(byte)p.PacketType)
                        {
                            case NetTypes.PacketTypes.APP_INTEGRITY_FAIL:  /*rn definitely just blocking autoban packets, 
                                usually a failure of an app integrity is never good 
                                and usually used for security stuff*/
                                return "Possible autoban packet with id (25) from your GT Client has been blocked."; // remember, returning anything will interrupt sending this packet. To Edit packets, load/parse them and you may just resend them like normally after fetching their bytes.
                            case NetTypes.PacketTypes.PLAYER_LOGIC_UPDATE:
                                if (p.PunchX > 0 || p.PunchY > 0)
                                {
                                    MainForm.LogText += "[" + DateTime.UtcNow + "] (PROXY): PunchX/PunchY detected, pX: " + p.PunchX.ToString() + " pY: " + p.PunchY.ToString() + "\n";
                                }
                                MainForm.globalUserData.isFacingSwapped = IsBitSet(p.CharacterState, 4);

                                worldMap.player.X = (int)p.X;
                                worldMap.player.Y = (int)p.Y;
                                break;
                            case NetTypes.PacketTypes.PING_REPLY:
                                {
                                    //SpoofedPingReply(p);
                                    return "Blocked ping reply!";
                                }
                            case NetTypes.PacketTypes.TILE_CHANGE_REQ:
                                if (p.MainValue == 32)
                                {
                                    /*MessageBox.Show("Log of potentially wanted received GAME_PACKET Data:" +
                    "\npackettype: " + data[4].ToString() +
                    "\npadding byte 1|2|3: " + data[5].ToString() + "|" + data[6].ToString() + "|" + data[7].ToString() +
                    "\nnetID: " + p.NetID +
                    "\nsecondnetid: " + p.SecondaryNetID +
                    "\ncharacterstate (prob 8): " + p.CharacterState +
                    "\nwaterspeed / offs 16: " + p.Padding +
                    "\nmainval: " + p.MainValue +
                    "\nX|Y: " + p.X + "|" + p.Y +
                    "\nXSpeed: " + p.XSpeed +
                    "\nYSpeed: " + p.YSpeed +
                    "\nSecondaryPadding: " + p.SecondaryPadding +
                    "\nPunchX|PunchY: " + p.PunchX + "|" + p.PunchY);*/

                                    MainForm.globalUserData.lastWrenchX = (short)p.PunchX;
                                    MainForm.globalUserData.lastWrenchY = (short)p.PunchY;
                                }
                                else if (p.MainValue == 18 && MainForm.globalUserData.redDamageToBlock)
                                {
                                    // playingo
                                    p.SecondaryPadding = -1;
                                    p.ExtDataMask |= 1 << 27; // 28
                                    p.Padding = 1;
                                    packetSender.SendPacketRaw(4, p.PackForSendingRaw(), MainForm.realPeer);
                                    return "";
                                }
                                break;
                            case NetTypes.PacketTypes.ITEM_ACTIVATE_OBJ: // just incase, to keep better track of items incase something goes wrong
                                worldMap.dropped_ITEMUID = p.MainValue;
                                if (MainForm.globalUserData.blockCollecting)
                                {
                                    return "";
                                }

                                break;
                            default:
                                //MainForm.LogText += ("[" + DateTime.UtcNow + "] (CLIENT): Got Packet Type: " + p.PacketType.ToString() + "\n");
                                break;
                        }

                        if (data[4] > 23)
                        {
                            log = $"(CLIENT) Log of potentially wanted received GAME_PACKET Data:" +
                        "\npackettype: " + data[4].ToString() +
                        "\npadding byte 1|2|3: " + data[5].ToString() + "|" + data[6].ToString() + "|" + data[7].ToString() +
                        "\nnetID: " + p.NetID +
                        "\nsecondnetid: " + p.SecondaryNetID +
                        "\ncharacterstate (prob 8): " + p.CharacterState +
                        "\nwaterspeed / offs 16: " + p.Padding +
                        "\nmainval: " + p.MainValue +
                        "\nX|Y: " + p.X + "|" + p.Y +
                        "\nXSpeed: " + p.XSpeed +
                        "\nYSpeed: " + p.YSpeed +
                        "\nSecondaryPadding: " + p.SecondaryPadding +
                        "\nPunchX|PunchY: " + p.PunchX + "|" + p.PunchY;

                        }
                    }

                    break;
                case NetTypes.NetMessages.TRACK:
                    return "Packet with messagetype used for tracking was blocked!";
                case NetTypes.NetMessages.LOG_REQ:
                    return "Log request packet from client was blocked!";
                default:
                    break;
            }

            packetSender.SendData(data, MainForm.realPeer);

            return log;


        }

        private void SpoofedPingReply(TankPacket tPacket)
        {
            if (worldMap == null)
            {
                return;
            }

            TankPacket p = new()
            {
                PacketType = (int)NetTypes.PacketTypes.PING_REPLY,
                PunchX = (int)1000.0f,
                PunchY = (int)250.0f,
                X = 64.0f,
                Y = 64.0f,
                MainValue = tPacket.MainValue, // GetTickCount()
                SecondaryNetID = (int)MainForm.HashBytes(BitConverter.GetBytes(tPacket.MainValue)) // HashString of it
            };

            // rest is 0 by default to not get detected by ac.
            packetSender.SendPacketRaw((int)NetTypes.NetMessages.GAME_PACKET, p.PackForSendingRaw(), MainForm.realPeer);
        }

        public string HandlePacketFromServer(ref ENetPeer peer, ENetPacket packet)
        {

            if (MainForm.proxyPeer.IsNull)
            {
                return "HandlePacketFromServer() -> Proxy peer is null!";
            }

            if (MainForm.proxyPeer.State != ENetPeerState.Connected)
            {
                return $"HandlePacketFromServer() -> proxyPeer is not connected: state = {MainForm.proxyPeer.State}";
            }

            if (peer.IsNull)
            {
                return "HandlePacketFromServer() -> peer.IsNull is true!";
            }

            if (peer.State != ENetPeerState.Connected)
            {
                return "HandlePacketFromServer() -> peer.State was not ENetPeerState.Connected!";
            }

            byte[] data = packet.Data.ToArray();


            NetTypes.NetMessages msgType = (NetTypes.NetMessages)data[0]; // more performance.
            switch (msgType)
            {
                case NetTypes.NetMessages.SERVER_HELLO:
                    {
                        MainForm.LogText += "[" + DateTime.UtcNow + "] (SERVER): Initial logon accepted." + "\n";

                        if (peer.TryGetUserData(out MainForm.UserData ud))
                        {
                            packetSender.SendPacket(2, MainForm.CreateLogonPacket(ud.tankIDName, ud.tankIDPass, ud.userID, ud.token, ud.doorid), peer);
                        }

                        break;
                    }
                case NetTypes.NetMessages.GAME_MESSAGE:

                    string str = GetProperGenericText(data);
                    MainForm.LogText += "[" + DateTime.UtcNow + "] (SERVER): A game_msg packet was sent: " + str + "\n";

                    if (str.Contains("Server requesting that you re-logon"))
                    {
                        MainForm.globalUserData.token = -1;
                        MainForm.globalUserData.Growtopia_IP = MainForm.globalUserData.Growtopia_Master_IP;
                        MainForm.globalUserData.Growtopia_Port = MainForm.globalUserData.Growtopia_Master_Port;
                        MainForm.globalUserData.isSwitchingServer = true;

                        MainForm.realPeer.Disconnect(0);
                    }

                    break;
                case NetTypes.NetMessages.GAME_PACKET:

                    byte[] tankPacket = VariantList.get_struct_data(data);
                    if (tankPacket == null)
                    {
                        break;
                    }

                    byte tankPacketType = tankPacket[0];
                    NetTypes.PacketTypes packetType = (NetTypes.PacketTypes)tankPacketType;
                    if (MainForm.logallpackettypes)
                    {
                        GamePacketProton gp = new();
                        gp.AppendString("OnConsoleMessage");
                        gp.AppendString("`6(PROXY) `wPacket TYPE: " + tankPacketType.ToString());
                        packetSender.SendData(gp.GetBytes(), MainForm.proxyPeer);

                        if (tankPacketType > 18)
                        {
                            File.WriteAllBytes("newpacket.dat", tankPacket);
                        }
                    }

                    switch (packetType)
                    {

                        case NetTypes.PacketTypes.PLAYER_LOGIC_UPDATE:
                            {
                                TankPacket p = TankPacket.UnpackFromPacket(data);
                                foreach (Player pl in worldMap.players)
                                {
                                    if (pl.netID == p.NetID)
                                    {
                                        pl.X = (int)p.X;
                                        pl.Y = (int)p.Y;
                                        break;
                                    }
                                }
                                break;
                            }
                        case NetTypes.PacketTypes.INVENTORY_STATE:
                            {
                                if (!MainForm.globalUserData.dontSerializeInventory)
                                {
                                    worldMap.player.SerializePlayerInventory(VariantList.get_extended_data(tankPacket));
                                }

                                break;
                            }
                        case NetTypes.PacketTypes.TILE_CHANGE_REQ:
                            {
                                TankPacket p = TankPacket.UnpackFromPacket(data);

                                if (worldMap == null)
                                {
                                    MainForm.LogText += "[" + DateTime.UtcNow + "] (PROXY): (ERROR) World map was null." + "\n";
                                    break;
                                }
                                byte tileX = (byte)p.PunchX;
                                byte tileY = (byte)p.PunchY;
                                ushort item = (ushort)p.MainValue;


                                if (tileX >= worldMap.width)
                                {
                                    break;
                                }
                                else if (tileY >= worldMap.height)
                                {
                                    break;
                                }

                                ItemDatabase.ItemDefinition itemDef = ItemDatabase.GetItemDef(item);



                                if (ItemDatabase.isBackground(item))
                                {
                                    worldMap.tiles[tileX + (tileY * worldMap.width)].bg = item;
                                }
                                else
                                {
                                    worldMap.tiles[tileX + (tileY * worldMap.width)].fg = item;
                                }

                                break;
                            }
                        case NetTypes.PacketTypes.CALL_FUNCTION:
                            VariantList.VarList VarListFetched = VariantList.GetCall(VariantList.get_extended_data(tankPacket));
                            VarListFetched.netID = BitConverter.ToInt32(tankPacket, 4); // add netid
                            VarListFetched.delay = BitConverter.ToUInt32(tankPacket, 20); // add keep track of delay modifier

                            bool isABot = false;

                            int netID = OperateVariant(VarListFetched, isABot ? peer : null); // box enetpeer obj to generic obj
                            string argText = string.Empty;

                            for (int i = 0; i < VarListFetched.functionArgs.Count(); i++)
                            {
                                argText += " [" + i.ToString() + "]: " + VarListFetched.functionArgs[i].ToString();
                            }

                            MainForm.LogText += "[" + DateTime.UtcNow + "] (SERVER): A function call was requested, see log infos below:\nFunction Name: " + VarListFetched.FunctionName + " parameters: " + argText + " \n";

                            if (VarListFetched.FunctionName == "OnSendToServer")
                            {
                                return "Server switching forced, not continuing as Proxy Client has to deal with this.";
                            }

                            if (VarListFetched.FunctionName == "onShowCaptcha")
                            {
                                return "Received captcha solving request, instantly bypassed it so it doesnt show up on client side.";
                            }

                            if (VarListFetched.FunctionName == "OnDialogRequest" && ((string)VarListFetched.functionArgs[1]).ToLower().Contains("captcha"))
                            {
                                return "Received captcha solving request, instantly bypassed it so it doesnt show up on client side.";
                            }

                            if (VarListFetched.FunctionName == "OnDialogRequest" && ((string)VarListFetched.functionArgs[1]).ToLower().Contains("gazette"))
                            {
                                return "Received gazette, skipping it...";
                            }

                            if (VarListFetched.FunctionName == "OnSetPos" && MainForm.globalUserData.ignoreonsetpos && netID == worldMap.netID)
                            {
                                return "Ignored position set by server, may corrupt doors but is used so it wont set back. (CAN BE BUGGY WITH SLOW CONNECTIONS)";
                            }

                            if (VarListFetched.FunctionName == "OnSpawn" && netID == -2)
                            {
                                if (MainForm.globalUserData.unlimitedZoom)
                                {
                                    return "Modified OnSpawn for unlimited zoom (mstate|1)"; // only doing unlimited zoom and not unlimited punch/place to be sure that no bans occur due to this. If you wish to use unlimited punching/placing as well, change the smstate in OperateVariant function instead.
                                }
                            }


                            break;
                        case NetTypes.PacketTypes.SET_CHARACTER_STATE:
                            {

                                /*TankPacket p = TankPacket.UnpackFromPacket(data);

                                return "Log of potentially wanted received GAME_PACKET Data:" +
                         "\nnetID: " + p.NetID +
                         "\nsecondnetid: " + p.SecondaryNetID +
                         "\ncharacterstate (prob 8): " + p.CharacterState +
                         "\nwaterspeed / offs 16: " + p.Padding +
                         "\nmainval: " + p.MainValue +
                         "\nX|Y: " + p.X + "|" + p.Y +
                         "\nXSpeed: " + p.XSpeed +
                         "\nYSpeed: " + p.YSpeed +
                         "\nSecondaryPadding: " + p.SecondaryPadding +
                         "\nPunchX|PunchY: " + p.PunchX + "|" + p.PunchY;*/
                                break;
                            }
                        case NetTypes.PacketTypes.PING_REQ:
                            SpoofedPingReply(TankPacket.UnpackFromPacket(data));
                            break;
                        case NetTypes.PacketTypes.LOAD_MAP:
                            if (MainForm.LogText.Length >= 32678)
                            {
                                MainForm.LogText = string.Empty;
                            }

                            worldMap = worldMap.LoadMap(tankPacket);
                            worldMap.player.didCharacterStateLoad = false;
                            worldMap.player.didClothingLoad = false;
                            if (MainForm.pForm.IsHandleCreated)
                            {
                                void action()
                                {
                                    MainForm.pForm.Text = "All players in " + worldMap.currentWorld;

                                    foreach (Button btn in MainForm.pForm.playerBox.Controls)
                                    {
                                        btn.Dispose();
                                    }

                                    MainForm.pForm.playerBox.Controls.Clear();
                                }

                                MainForm.pForm.Invoke(action);
                            }


                            MainForm.realPeer.Timeout(1000, 2800, 3400);

                            break;
                        case NetTypes.PacketTypes.MODIFY_ITEM_OBJ:
                            {
                                TankPacket p = TankPacket.UnpackFromPacket(data);
                                if (p.NetID == -1)
                                {
                                    if (worldMap == null)
                                    {
                                        MainForm.LogText += "[" + DateTime.UtcNow + "] (PROXY): (ERROR) World map was null." + "\n";
                                        break;
                                    }

                                    worldMap.dropped_ITEMUID++;

                                    DroppedObject dItem = new()
                                    {
                                        id = p.MainValue,
                                        itemCount = data[16],
                                        x = p.X,
                                        y = p.Y,
                                        uid = worldMap.dropped_ITEMUID
                                    };
                                    worldMap.droppedItems.Add(dItem);

                                    if (MainForm.globalUserData.cheat_magplant)
                                    {


                                        TankPacket p2 = new()
                                        {
                                            PacketType = (int)NetTypes.PacketTypes.ITEM_ACTIVATE_OBJ,
                                            NetID = p.NetID,
                                            X = (int)p.X,
                                            Y = (int)p.Y,
                                            MainValue = dItem.uid
                                        };

                                        packetSender.SendPacketRaw((int)NetTypes.NetMessages.GAME_PACKET, p2.PackForSendingRaw(), MainForm.realPeer);
                                        //return "Blocked dropped packet due to magplant hack (auto collect/pickup range) tried to collect it instead, infos of dropped item => uid was " + worldMap.dropped_ITEMUID.ToString() + " id: " + p.MainValue.ToString();
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case NetTypes.NetMessages.TRACK:
                    {
                        return "Track message:\n" + GetProperGenericText(data);
                        break;
                    }
                case NetTypes.NetMessages.LOG_REQ:
                case NetTypes.NetMessages.ERROR:
                    return "Blocked LOG_REQUEST/ERROR message from server";
                default:
                    //return "(SERVER): An unknown event occured. Message Type: " + msgType.ToString() + "\n";
                    break;

            }

            packetSender.SendData(data, MainForm.proxyPeer);
            if (msgType == NetTypes.NetMessages.GAME_PACKET && data[4] > 39) // customizable on which packets you wanna log, for speed im just gonna do this!
            {
                TankPacket p = TankPacket.UnpackFromPacket(data);
                uint extDataSize = BitConverter.ToUInt32(data, 56);
                byte[] actualData = data.Skip(4).Take(56).ToArray();
                byte[] extData = data.Skip(60).ToArray();

                string extDataStr = "";
                string extDataString = Encoding.UTF8.GetString(extData);
                for (int i = 0; i < extDataSize; i++)
                {
                    //ushort pos = BitConverter.ToUInt16(extData, i);
                    extDataStr += extData[i].ToString() + "|";
                }


                return "Log of potentially wanted received GAME_PACKET Data:" +
                    "\npackettype: " + actualData[0].ToString() +
                    "\npadding byte 1|2|3: " + actualData[1].ToString() + "|" + actualData[2].ToString() + "|" + actualData[3].ToString() +
                    "\nnetID: " + p.NetID +
                    "\nsecondnetid: " + p.SecondaryNetID +
                    "\ncharacterstate (prob 8): " + p.CharacterState +
                    "\nwaterspeed / offs 16: " + p.Padding +
                    "\nmainval: " + p.MainValue +
                    "\nX|Y: " + p.X + "|" + p.Y +
                    "\nXSpeed: " + p.XSpeed +
                    "\nYSpeed: " + p.YSpeed +
                    "\nSecondaryPadding: " + p.SecondaryPadding +
                    "\nPunchX|PunchY: " + p.PunchX + "|" + p.PunchY +
                    "\nExtended Packet Data Length: " + extDataSize.ToString() +
                    "\nExtended Packet Data:\n" + extDataStr + "\n";
                return string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
