using System;
using System.Globalization;
using System.Linq;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;
using Guid = WowPacketParser.Misc.Guid;

namespace WowPacketParserModule.V5_4_8_18291.Parsers
{
    public static class PetHandler
    {
        [Parser(Opcode.CMSG_PET_NAME_QUERY)]
        public static void HandlePetNameQuery(Packet packet)
        {
            var guid = new byte[8];
            var number = new byte[8];

            number[0] = packet.ReadBit();
            number[5] = packet.ReadBit();
            guid[1] = packet.ReadBit();
            guid[7] = packet.ReadBit();
            number[7] = packet.ReadBit();
            guid[6] = packet.ReadBit();
            guid[4] = packet.ReadBit();
            guid[5] = packet.ReadBit();
            guid[0] = packet.ReadBit();
            number[3] = packet.ReadBit();
            number[6] = packet.ReadBit();
            number[2] = packet.ReadBit();
            guid[3] = packet.ReadBit();
            guid[2] = packet.ReadBit();
            number[1] = packet.ReadBit();
            number[4] = packet.ReadBit();

            packet.ReadXORByte(number, 2);
            packet.ReadXORByte(number, 1);
            packet.ReadXORByte(number, 0);
            packet.ReadXORByte(number, 7);
            packet.ReadXORByte(guid, 5);
            packet.ReadXORByte(guid, 0);
            packet.ReadXORByte(number, 6);
            packet.ReadXORByte(guid, 4);
            packet.ReadXORByte(number, 5);
            packet.ReadXORByte(guid, 2);
            packet.ReadXORByte(guid, 6);
            packet.ReadXORByte(number, 3);
            packet.ReadXORByte(guid, 3);
            packet.ReadXORByte(number, 4);
            packet.ReadXORByte(guid, 1);
            packet.ReadXORByte(guid, 7);

            var GUID = new Guid(BitConverter.ToUInt64(number, 0));
            var Number = BitConverter.ToUInt64(number, 0);
            packet.WriteGuid("Guid", guid);
            packet.WriteLine("Pet Number: {0}", Number);

            // Store temporary name (will be replaced in SMSG_PET_NAME_QUERY_RESPONSE)
            StoreGetters.AddName(GUID, Number.ToString(CultureInfo.InvariantCulture));
        }

        [Parser(Opcode.SMSG_PET_NAME_QUERY_RESPONSE)]
        public static void HandlePetNameQueryResponse(Packet packet)
        {
            var hasData = packet.ReadBit();
            if (!hasData)
            {
                packet.ReadUInt64("Pet number");
                return;
            }

            packet.ReadBit("Declined");

            const int maxDeclinedNameCases = 5;
            var declinedNameLen = new int[maxDeclinedNameCases];
            for (var i = 0; i < maxDeclinedNameCases; ++i)
                declinedNameLen[i] = (int)packet.ReadBits(7);

            var len = packet.ReadBits(8);

            for (var i = 0; i < maxDeclinedNameCases; ++i)
                if (declinedNameLen[i] != 0)
                    packet.ReadWoWString("Declined name", declinedNameLen[i], i);

            var petName = packet.ReadWoWString("Pet name", len);
            packet.ReadTime("Time");
            var number = packet.ReadUInt64("Pet number");

            var guidArray = (from pair in StoreGetters.NameDict where Equals(pair.Value, number) select pair.Key).ToList();
            foreach (var guid in guidArray)
                StoreGetters.NameDict[guid] = petName;
        }        
    }
}
