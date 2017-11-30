using System.Collections.Generic;
using Newtonsoft.Json;

namespace DaaSDemo.Models.DatabaseProxy
{
    /// <summary>
    ///     Response body when executing a T-SQL command (i.e. a non-query).
    /// </summary>
    public class CommandResult
        : SqlResult
    {
    }
}
