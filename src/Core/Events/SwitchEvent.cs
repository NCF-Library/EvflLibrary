﻿using EvflLibrary.Common;
using EvflLibrary.Parsers;
using System.Text.Json.Serialization;

namespace EvflLibrary.Core
{
    public class SwitchEvent : Event, IEvflDataBlock
    {
        public record SwitchCase(uint Value, ushort EventIndex);

        public ushort ActorIndex { get; set; }
        public ushort ActorQueryIndex { get; set; }
        public Container? Parameters { get; set; }
        public List<SwitchCase> SwitchCases { get; set; }

        [JsonConstructor]
        public SwitchEvent(string name, EventType type) : base(name, type)
        {
            Parameters = new();
            SwitchCases = new();
        }

        public SwitchEvent(EvflReader reader) : base(reader)
        {
            ushort switchCaseCount = reader.ReadUInt16();
            ActorIndex = reader.ReadUInt16();
            ActorQueryIndex = reader.ReadUInt16();
            Parameters = reader.ReadObjectPtr<Container>(() => new(reader));
            SwitchCases = reader.ReadObjectsPtr(new SwitchCase[switchCaseCount], () => {
                SwitchCase switchCase = new(reader.ReadUInt32(), reader.ReadUInt16());
                reader.ReadUInt16();
                reader.Align(8);
                return switchCase;
            }).ToList();
            reader.BaseStream.Position += 8; // Unused pointer
        }

        public new void Write(EvflWriter writer)
        {
            base.Write(writer);
            writer.Write((ushort)SwitchCases.Count);
            writer.Write(ActorIndex);
            writer.Write(ActorQueryIndex);
            Action insertParamsPtr = writer.ReservePtrIf(Parameters?.CanWrite() ?? false);
            Action insertSwitchCasesPtr = writer.ReservePtrIf(SwitchCases.Count > 0, register: true);
            writer.Write(0L);
            writer.ReserveBlockWriter("EventArrayDataBlock", () => {
                if (SwitchCases.Count > 0) {
                    writer.Align(8);
                    insertSwitchCasesPtr();
                    for (int i = 0; i < SwitchCases.Count; i++) {
                        SwitchCase switchCase = SwitchCases[i];
                        writer.Write(switchCase.Value);
                        writer.Write(switchCase.EventIndex);
                        writer.Write((ushort)0); // Padding
                        writer.Align(8);
                    }
                }

                if (Parameters?.CanWrite() ?? false) {
                insertParamsPtr();
                Parameters?.Write(writer);
                }
            });
        }
    }
}
