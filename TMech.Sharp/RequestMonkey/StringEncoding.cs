using System;
using System.Text;

namespace TMech.Sharp.RequestMonkey;

public enum StringEncoding
{
    ASCII,
    UTF8,
    UTF16,
    UTF16BE,
    UTF32,
    UTF32BE
}

public static class StringEncodingHelper
{
    public static Encoding GetEncoder(this StringEncoding self)
    {
        return self switch
        {
            StringEncoding.ASCII => new ASCIIEncoding(),
            StringEncoding.UTF8 => new UTF8Encoding(false, true),
            StringEncoding.UTF16 => new UnicodeEncoding(false, false),
            StringEncoding.UTF16BE => new UnicodeEncoding(true, false),
            StringEncoding.UTF32 => new UTF32Encoding(false, false),
            StringEncoding.UTF32BE => new UTF32Encoding(true, false),
            _ => throw new InvalidOperationException()
        };
    }

    public static StringEncoding FromString(string value)
    {
        return value switch
        {
            "ASCII" => StringEncoding.ASCII,
            "UTF8" => StringEncoding.UTF8,
            "UTF16" => StringEncoding.UTF16,
            "UTF16BE" => StringEncoding.UTF16BE,
            "UTF32" => StringEncoding.UTF32,
            "UTF32BE" => StringEncoding.UTF32BE,
            _ => throw new InvalidOperationException()
        };
    }
}
