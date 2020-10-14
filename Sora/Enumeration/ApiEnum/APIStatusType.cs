using System.ComponentModel;

namespace Sora.Enumeration.ApiEnum
{
    /// <summary>
    /// API返回值
    /// </summary>
    [DefaultValue(OK)]
    public enum APIStatusType
    {

        OK       = 0,

        Failed    = 100,

        NotFound = 404,

        Error    = 502,

        Failed_   = 102,

        NoResult = -1
    }
}
