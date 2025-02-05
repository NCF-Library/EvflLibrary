using BfevLibrary.Common;
using BfevLibrary.Parsers;
using System.Buffers.Binary;

namespace BfevLibrary.Core;
public class VariableDef : IBfevDataBlock
{
    public enum VariableDefType : ushort
    {
        Int = 2,
        Bool = 3,
        Float = 4
    }

    public VariableDefType GetVariableType()
    {
        if (Int != null)
        {
            return VariableDefType.Int;
        }
        else if (Bool != null)
        {
            return VariableDefType.Bool;
        }
        else if (Float != null) 
        { 
            return VariableDefType.Float; 
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Creates a new Variable Definition
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="isImplicit"><inheritdoc cref="IsImplicit" path="/summary"/></param>
    public VariableDef(BfevReader reader, bool isImplicit)
    {
        Read(reader);
        IsImplicit = isImplicit;
    }

    public VariableDefType Type { get; set; }
    public int? Int { get; set; }
    public bool? Bool { get; set; }
    public float? Float { get; set; }

    /// <summary>
    /// Whether or not the variable is being implicitly used in some <see cref="ActionEvent"/>. <b>False</b> means it has been loaded by an <seealso cref="EntryPoint"/>.
    /// </summary>
    public bool IsImplicit { get; set; } = false;

    public IBfevDataBlock Read(BfevReader reader)
    {
        byte[] initialValueRaw = reader.ReadBytes(8);
        var parametersCount = reader.ReadUInt16();

        // No arrays is expected, may change in the future
        if (parametersCount == 1)
        {
            Type = (VariableDefType) reader.ReadUInt16();

            switch (Type)
            {
                case VariableDefType.Int:
                    Int = BinaryPrimitives.ReadInt32LittleEndian(initialValueRaw.AsSpan(0, 4));
                    break;

                case VariableDefType.Bool:
                    Bool = initialValueRaw[0] != 0;
                    break;

                case VariableDefType.Float:
                    Float = BinaryPrimitives.ReadSingleLittleEndian(initialValueRaw.AsSpan(0, 4));
                    break;

                default:
                    throw new InvalidDataException($"Unsupported VarDef type: {Type}");
            }

            reader.BaseStream.Position += 4; // padding
        }
        return this;
    }


    public void Write(BfevWriter writer)
    {
        byte[] initialValueRaw = new byte[8];

        if (Int.HasValue)
            BinaryPrimitives.WriteInt32LittleEndian(initialValueRaw.AsSpan(0, 4), Int.Value);
        else if (Bool.HasValue)
            initialValueRaw[0] = (byte)(Bool.Value ? 1 : 0);

        else if (Float.HasValue)
            BinaryPrimitives.WriteSingleLittleEndian(initialValueRaw.AsSpan(0, 4), Float.Value);


        writer.Write(initialValueRaw);
        writer.Write((ushort) 1);
        writer.Write((ushort) GetVariableType());
        writer.Write(0); // padding
    }
}