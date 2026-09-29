using System.Data.Common;

namespace DA.AccesoDatos;

public static class DataReaderExtensions
{
    private static int GetOrdinalSafe(this DbDataReader reader, string name)
    {
        try
        {
            return reader.GetOrdinal(name);
        }
        catch (IndexOutOfRangeException)
        {
            return -1;
        }
    }

    public static string? GetStringSafe(this DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinalSafe(name);
        if (ordinal < 0 || reader.IsDBNull(ordinal))
            return null;
        return reader[ordinal].ToString();
    }

    public static decimal GetDecimalSafe(this DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinalSafe(name);
        if (ordinal < 0 || reader.IsDBNull(ordinal))
            return 0;
        return Convert.ToDecimal(reader[ordinal]);
    }

    public static int GetInt32Safe(this DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinalSafe(name);
        if (ordinal < 0 || reader.IsDBNull(ordinal))
            return 0;
        return Convert.ToInt32(reader[ordinal]);
    }
}
