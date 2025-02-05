using BfevLibrary.Common;
using BfevLibrary.Parsers;

namespace BfevLibrary.Core;

public class EntryPoint : IBfevDataBlock
{
    public RadixTree<VariableDef>? Parameters { get; set; } = new();
    public List<short> SubFlowEventIndices { get; set; }
    public short EventIndex { get; set; }

    public EntryPoint() { }
    public EntryPoint(BfevReader reader)
    {
        Read(reader);
    }

    public IBfevDataBlock Read(BfevReader reader)
    {
        long subFlowEventIndicesPtr = reader.ReadInt64();
        long entrypointVariableDefNamesPtr = reader.ReadInt64();
        long entrypointVariableDefPtr = reader.ReadInt64();
        ushort subFlowEventIndicesCount = reader.ReadUInt16();
        ushort varDefCount= reader.ReadUInt16();
        EventIndex = reader.ReadInt16();

        reader.BaseStream.Position += 2; // padding

        if (varDefCount > 0)
        {
            var variableDefinitions = new VariableDef[varDefCount];
            reader.ReadObjectsPtr(variableDefinitions, () => new VariableDef(reader, false), entrypointVariableDefPtr);
            reader.TemporarySeek(entrypointVariableDefNamesPtr, SeekOrigin.Begin, () => Parameters = new RadixTree<VariableDef>(reader, variableDefinitions));
        }

        SubFlowEventIndices = reader.ReadObjectsPtr(new short[subFlowEventIndicesCount], reader.ReadInt16, subFlowEventIndicesPtr).ToList();
        return this;
    }

    public void Write(BfevWriter writer)
    {
        Action insertSubFlowEventIndicesPtr = writer.ReservePtrIf(SubFlowEventIndices.Count > 0, register: true);
        writer.Write(0L); // Unused (in botw) VariableDef pointer (ulong)
        writer.WriteNullPtr(register: true); // Unused (in botw) VariableDef dict pointer (ulong)
        writer.Write((ushort)SubFlowEventIndices.Count);
        writer.Write((ushort)0); // Unused (in botw) VariableDefs count
        writer.Write(EventIndex);
        writer.Write((ushort)0); // Padding
        writer.ReserveBlockWriter("EntryPointArrayDataBlock", () => {
            if (SubFlowEventIndices.Count > 0) {
                insertSubFlowEventIndicesPtr();
                for (int i = 0; i < SubFlowEventIndices.Count; i++) {
                    writer.Write(SubFlowEventIndices[i]);
                }
                writer.Align(8);
            }

            // Not really sure what this is for, based
            // off evfl by leoetlino (evfl/entry_point.py)
            writer.Seek(24, SeekOrigin.Current);
        });
    }
}
