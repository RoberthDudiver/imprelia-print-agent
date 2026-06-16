using System.IO;
using System.Text;

namespace Imprelia.PrintAgent.Services;

/// <summary>Constantes del protocolo IPP (RFC 8011).</summary>
internal static class Ipp
{
    // Delimitadores de grupo
    public const byte TagOperation   = 0x01;
    public const byte TagJob         = 0x02;
    public const byte TagEnd         = 0x03;
    public const byte TagPrinter     = 0x04;
    public const byte TagUnsupported = 0x05;

    // Tags de valor
    public const byte ValInteger     = 0x21;
    public const byte ValBoolean     = 0x22;
    public const byte ValEnum        = 0x23;
    public const byte ValResolution  = 0x32;
    public const byte ValRange       = 0x33;
    public const byte ValText        = 0x41;
    public const byte ValName        = 0x42;
    public const byte ValKeyword     = 0x44;
    public const byte ValUri         = 0x45;
    public const byte ValCharset     = 0x47;
    public const byte ValLanguage    = 0x48;
    public const byte ValMimeType    = 0x49;

    // Operaciones
    public const short OpPrintJob            = 0x0002;
    public const short OpValidateJob         = 0x0004;
    public const short OpCreateJob           = 0x0005;
    public const short OpSendDocument        = 0x0006;
    public const short OpCancelJob           = 0x0008;
    public const short OpGetJobAttributes    = 0x0009;
    public const short OpGetJobs             = 0x000A;
    public const short OpGetPrinterAttributes= 0x000B;

    // Status codes
    public const short OkStatus              = 0x0000;
    public const short ClientErrorBadRequest = 0x0400;
    public const short ServerErrorNotSupported = 0x0501;
}

/// <summary>Constructor de mensajes IPP binarios (big-endian).</summary>
internal sealed class IppWriter
{
    private readonly MemoryStream _ms = new();

    public IppWriter Header(short statusOrOp, int requestId, byte verMajor = 2, byte verMinor = 0)
    {
        _ms.WriteByte(verMajor);
        _ms.WriteByte(verMinor);
        WriteInt16((short)statusOrOp);
        WriteInt32(requestId);
        return this;
    }

    public IppWriter Group(byte delimiterTag) { _ms.WriteByte(delimiterTag); return this; }
    public IppWriter End() { _ms.WriteByte(Ipp.TagEnd); return this; }
    public byte[] ToArray() => _ms.ToArray();

    public IppWriter Str(byte valueTag, string name, string value)
    {
        _ms.WriteByte(valueTag);
        WriteName(name);
        WriteValueBytes(Encoding.UTF8.GetBytes(value));
        return this;
    }

    /// <summary>Valor adicional de un atributo multivaluado (nombre vacío).</summary>
    public IppWriter Add(byte valueTag, string value)
    {
        _ms.WriteByte(valueTag);
        WriteInt16(0);
        WriteValueBytes(Encoding.UTF8.GetBytes(value));
        return this;
    }

    public IppWriter Int(byte valueTag, string name, int value)
    {
        _ms.WriteByte(valueTag);
        WriteName(name);
        WriteInt16(4);
        WriteInt32(value);
        return this;
    }

    public IppWriter AddInt(byte valueTag, int value)
    {
        _ms.WriteByte(valueTag);
        WriteInt16(0);
        WriteInt16(4);
        WriteInt32(value);
        return this;
    }

    public IppWriter Bool(string name, bool value)
    {
        _ms.WriteByte(Ipp.ValBoolean);
        WriteName(name);
        WriteInt16(1);
        _ms.WriteByte((byte)(value ? 1 : 0));
        return this;
    }

    public IppWriter Resolution(string name, int cross, int feed, byte units = 3)
    {
        _ms.WriteByte(Ipp.ValResolution);
        WriteName(name);
        WriteInt16(9);
        WriteInt32(cross);
        WriteInt32(feed);
        _ms.WriteByte(units);
        return this;
    }

    public IppWriter Range(string name, int lower, int upper)
    {
        _ms.WriteByte(Ipp.ValRange);
        WriteName(name);
        WriteInt16(8);
        WriteInt32(lower);
        WriteInt32(upper);
        return this;
    }

    private void WriteName(string name)
    {
        var b = Encoding.ASCII.GetBytes(name);
        WriteInt16((short)b.Length);
        _ms.Write(b);
    }

    private void WriteValueBytes(byte[] b)
    {
        WriteInt16((short)b.Length);
        _ms.Write(b);
    }

    private void WriteInt16(short v)
    {
        _ms.WriteByte((byte)(v >> 8));
        _ms.WriteByte((byte)v);
    }

    private void WriteInt32(int v)
    {
        _ms.WriteByte((byte)(v >> 24));
        _ms.WriteByte((byte)(v >> 16));
        _ms.WriteByte((byte)(v >> 8));
        _ms.WriteByte((byte)v);
    }
}

/// <summary>Parser tolerante de mensajes IPP. Extrae operación, request-id y el documento.</summary>
internal sealed class IppRequest
{
    public short OperationId { get; private set; }
    public int RequestId { get; private set; }
    public Dictionary<string, string> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public byte[] Document { get; private set; } = Array.Empty<byte>();

    public static IppRequest Parse(byte[] data)
    {
        var r = new IppRequest();
        if (data.Length < 8) return r;

        int i = 2; // saltar versión (major, minor)
        r.OperationId = (short)((data[i] << 8) | data[i + 1]); i += 2;
        r.RequestId = (data[i] << 24) | (data[i + 1] << 16) | (data[i + 2] << 8) | data[i + 3]; i += 4;

        string lastName = "";
        while (i < data.Length)
        {
            byte tag = data[i++];

            if (tag == Ipp.TagEnd)
            {
                // El resto son los datos del documento.
                if (i < data.Length)
                    r.Document = data[i..];
                return r;
            }

            if (tag <= 0x0F) continue; // delimitador de grupo

            if (i + 2 > data.Length) break;
            int nameLen = (data[i] << 8) | data[i + 1]; i += 2;
            if (i + nameLen > data.Length) break;
            string name = nameLen > 0 ? Encoding.ASCII.GetString(data, i, nameLen) : lastName;
            i += nameLen;

            if (i + 2 > data.Length) break;
            int valLen = (data[i] << 8) | data[i + 1]; i += 2;
            if (i + valLen > data.Length) break;
            string val = DecodeValue(tag, data, i, valLen);
            i += valLen;

            if (nameLen > 0) lastName = name;
            if (!string.IsNullOrEmpty(name)) r.Attributes[name] = val;
        }

        return r;
    }

    private static string DecodeValue(byte tag, byte[] data, int offset, int len)
    {
        try
        {
            switch (tag)
            {
                case Ipp.ValInteger:
                case Ipp.ValEnum:
                    if (len >= 4)
                        return ((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]).ToString();
                    return "0";
                case Ipp.ValBoolean:
                    return len >= 1 && data[offset] != 0 ? "true" : "false";
                default:
                    return Encoding.UTF8.GetString(data, offset, len);
            }
        }
        catch { return ""; }
    }
}
