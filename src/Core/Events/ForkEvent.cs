﻿using EvflLibrary.Common;
using EvflLibrary.Parsers;

namespace EvflLibrary.Core
{
    public class ForkEvent : Event, IEvflDataBlock
    {
        public ushort JoinEventIndex { get; set; }
        public List<ushort> ForkEventIndicies { get; set; }

        public ForkEvent(EvflReader reader, Event baseEvent) : base(baseEvent)
        {
            ushort forkCount = reader.ReadUInt16();
            JoinEventIndex = reader.ReadUInt16();
            reader.BaseStream.Position += 2; // unused ushort
            ForkEventIndicies = reader.ReadObjectsPtr(new ushort[forkCount], reader.ReadUInt16).ToList();
            reader.BaseStream.Position += 8 + 8; // unused pointers
        }

        public new void Write(EvflWriter writer)
        {
            base.Write(writer);
            writer.Write((ushort)ForkEventIndicies.Count);
            writer.Write(JoinEventIndex);
            writer.Write((ushort)0);
            Action insertForkEventIndiciesPtr = writer.ReservePtr();
            writer.Write(0L);
            writer.Write(0L);
            writer.ReserveBlockWriter("EventArrayDataBlock", () => {
                if (ForkEventIndicies.Count > 0) {
                    insertForkEventIndiciesPtr();
                    for (int i = 0; i < ForkEventIndicies.Count; i++) {
                        writer.Write(ForkEventIndicies[i]);
                    }
                    writer.Align(8);
                }
            });
        }
    }
}