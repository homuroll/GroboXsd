namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public enum DateTimeTypeCode
    {
        None = 0,
        DateTime = 0x01,
        Time = 0x02,
        Date = 0x04,
        GYearMonth = 0x08,
        GYear = 0x10,
        GMonthDay = 0x20,
        GDay = 0x40,
        GMonth = 0x80,
        XdrDateTimeNoTz = 0x100,
        XdrDateTime = 0x200,
        XdrTimeNoTz = 0x400, //XDRTime with tz is the same as xsd:time  
        AllXsd = 0xFF //All still does not include the XDR formats
    }
}