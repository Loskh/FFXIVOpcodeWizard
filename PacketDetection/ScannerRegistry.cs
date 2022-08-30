using FFXIVOpcodeWizard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace FFXIVOpcodeWizard.PacketDetection
{
    public class ScannerRegistry
    {
        private readonly IList<Scanner> scanners;

        public IList<Scanner> AsList() => scanners.ToList();

        public ScannerRegistry()
        {
            this.scanners = new List<Scanner>();
            DeclareScanners();
        }

        private void DeclareScanners()
        {
            var inArray = (uint[] arr, uint item) => arr.Any(i => i == item);
            //=================
            RegisterScanner("ActorControlSelf", "Please enter sanctuary and wait for rested bonus gains.",
                PacketSource.Server,
                (packet, _) => packet.PacketSize == 64 &&
                               BitConverter.ToUInt16(packet.Data, Offsets.IpcData) == 24 &&
                               BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 4) <= 604800 &&
                               BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 8) == 0 &&
                               BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 12) == 0 &&
                               BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 16) == 0 &&
                               BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 20) == 0 &&
                               BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 24) == 0);
            //=================

            //=================
            RegisterScanner("HousingWardInfo", "Please view a housing ward from a city aetheryte/ferry.",
                PacketSource.Server,
                (packet, parameters) => packet.PacketSize == 2448 &&
                        IncludesBytes(new ArraySegment<byte>(packet.Data, Offsets.IpcData + 16, 32).ToArray(), Encoding.UTF8.GetBytes(parameters[0])),
                new[] { "Please enter the name of whoever owns the first house in the ward (if it's an FC, their shortname):" });
            //=================

            //=================
            RegisterScanner("ContainerInfo", "Please teleport to The Aftcastle (Adventurers' Guild in Limsa Lominsa Upper Decks) and wait.",
                PacketSource.Server,
                (packet, _) => packet.PacketSize == 48 &&
                               BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 8) == 2001);
            //=================

            //=================
            uint[] darkMatter = new uint[] { 5594, 5595, 5596, 5597, 5598, 10386, 17837, 33916 };
            var isDarkMatter = (uint itemId) => inArray(darkMatter, itemId);
            RegisterScanner("MarketBoardItemRequestStart", "Please open the market board listings for any Dark Matter.", //MarketBoardItemListingCount
                PacketSource.Server,
                (packet, _) => packet.PacketSize == 48 &&
                               isDarkMatter(BitConverter.ToUInt32(packet.Data, Offsets.IpcData)));
            RegisterScanner("MarketBoardHistory", string.Empty,//MarketBoardItemListingHistory
                PacketSource.Server,
                (packet, _) => packet.PacketSize == 1080 &&
                               isDarkMatter(BitConverter.ToUInt32(packet.Data, Offsets.IpcData)));
            RegisterScanner("MarketBoardOfferings", string.Empty,//MarketBoardItemListing
                PacketSource.Server,
                (packet, _) => packet.PacketSize > 1552 &&
                               isDarkMatter(BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 44)));
            RegisterScanner("MarketBoardPurchaseHandler", "Please purchase any Dark Matter",
                PacketSource.Client,
                (packet, _) => packet.PacketSize == 72 &&
                               isDarkMatter(BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 0x10)));
            RegisterScanner("MarketBoardPurchase", string.Empty,
                PacketSource.Server,
                (packet, _) => packet.PacketSize == 48 &&
                               isDarkMatter(BitConverter.ToUInt32(packet.Data, Offsets.IpcData)));
            //=================

            //=================
            uint inventoryModifyHandlerId = 0;
            const uint scannerItemId = 4850; // Honey
            RegisterScanner("InventoryModifyHandler", "Please drop the Honey.",
                PacketSource.Client,
                (packet, _, comment) =>
                {
                    var match = packet.PacketSize == 80 && BitConverter.ToUInt16(packet.Data, Offsets.IpcData + 0x18) == scannerItemId;
                    if (!match) return false;

                    inventoryModifyHandlerId = BitConverter.ToUInt32(packet.Data, Offsets.IpcData);

                    var baseOffset = BitConverter.ToUInt16(packet.Data, Offsets.IpcData + 4);
                    comment.Text = $"Base offset: {Util.NumberToString(baseOffset, NumberDisplayFormat.HexadecimalUppercase)}";
                    return true;
                });
            RegisterScanner("InventoryActionAck", "Please wait.",
                PacketSource.Server,
                (packet, _) => packet.PacketSize == 48 && BitConverter.ToUInt32(packet.Data, Offsets.IpcData) == inventoryModifyHandlerId);
            //=================
            RegisterScanner("MarketTaxRates", "Please visit a retainer counter and request information about market tax rates.",//ResultDialog
                PacketSource.Server,
                (packet, _) =>
                {
                    if (packet.PacketSize != 72)
                        return false;

                    var rate1 = BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 8);
                    var rate2 = BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 12);
                    var rate3 = BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 16);
                    var rate4 = BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 20);

                    return rate1 <= 7 && rate2 <= 7 && rate3 <= 7 && rate4 <= 7;
                });
            //=================
            byte[] retainerBytes = null;
            RegisterScanner("RetainerInformation", "Please use the Summoning Bell.",
                PacketSource.Server,
                (packet, parameters) =>
                {
                    retainerBytes ??= Encoding.UTF8.GetBytes(parameters[0]);
                    return packet.PacketSize == 112 && IncludesBytes(packet.Data.Skip(73).Take(32).ToArray(), retainerBytes);
                }, new[] { "Please enter one of your retainers' names:" });
            //=================

            //================
            RegisterScanner("ItemMarketBoardInfo", "Please put any item on sale for a unit price of 123456 and summon the retainer again",
                PacketSource.Server,
                (packet, parameters) => packet.PacketSize == 64 &&
                BitConverter.ToUInt32(packet.Data, Offsets.IpcData + 0x10) == 123456);
            //=================

            //=================
            RegisterScanner("CfNotifyPop", "Please enter the \"Sastasha\" as an undersized party.", // CFNotify
                PacketSource.Server,
                //(packet, _) => packet.PacketSize == 72 && BitConverter.ToUInt16(packet.Data, Offsets.IpcData + 28) == 4);
                (packet, _) => packet.PacketSize == 64 && BitConverter.ToUInt16(packet.Data, Offsets.IpcData + 20) == 4);
            //=================

            //=================
            string airshipName = null;
            string submarineName = null;

            RegisterScanner("AirshipTimers", "Open your Estate tab from the Timers window if you have any airships on exploration.",
                PacketSource.Server,
                (packet, parameters) =>
                {
                    airshipName = parameters[0];
                    return packet.PacketSize == 176 && IncludesBytes(packet.Data, Encoding.UTF8.GetBytes(airshipName));
                },
                new[] { "Please enter your airship name:" });
            RegisterScanner("SubmarineTimers", "Open your Estate tab from the Timers window if you have any submarines on exploration.",
                PacketSource.Server,
                (packet, parameters) =>
                {
                    submarineName = parameters[0];
                    return packet.PacketSize == 176 && IncludesBytes(packet.Data, Encoding.UTF8.GetBytes(submarineName));
                },
                new[] { "Please enter your submarine name:" });
            RegisterScanner("AirshipStatusList", "Open your airship management console if you have any airships",
                PacketSource.Server,
                (packet, parameters) => packet.PacketSize == 192 && IncludesBytes(packet.Data, Encoding.UTF8.GetBytes(airshipName)));
        }

        /// <summary>
        /// Adds a scanner to the scanner registry.
        /// </summary>
        /// <param name="packetName">The name (Sapphire-style) of the packet.</param>
        /// <param name="tutorial">How the packet's conditions are created.</param>
        /// <param name="source">Whether the packet originates on the client or the server.</param>
        /// <param name="del">A boolean function that returns true if a packet matches the contained heuristics.</param>
        /// <param name="paramPrompts">An array of requests for auxiliary data that will be passed into the detection delegate.</param>
        private void RegisterScanner(
            string packetName,
            string tutorial,
            PacketSource source,
            Func<IpcPacket, string[], Comment, bool> del,
            string[] paramPrompts = null)
        {
            this.scanners.Add(new Scanner
            {
                PacketName = packetName,
                Tutorial = tutorial,
                ScanDelegate = del,
                Comment = new Comment(),
                ParameterPrompts = paramPrompts ?? new string[] { },
                PacketSource = source,
            });
        }

        private void RegisterScanner(
            string packetName,
            string tutorial,
            PacketSource source,
            Func<IpcPacket, string[], bool> del,
            string[] paramPrompts = null)
        {
            bool Fn(IpcPacket a, string[] b, Comment c) => del(a, b);
            RegisterScanner(packetName, tutorial, source, Fn, paramPrompts);
        }

        private static bool IncludesBytes(byte[] source, byte[] search)
        {
            if (search == null) return false;

            for (var i = 0; i < source.Length - search.Length; ++i)
            {
                var result = true;
                for (var j = 0; j < search.Length; ++j)
                {
                    if (search[j] != source[i + j])
                    {
                        result = false;
                        break;
                    }
                }

                if (result)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
