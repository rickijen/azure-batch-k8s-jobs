using System;
using System.Collections.Generic;

namespace core.Interfaces
{
    public interface IParseInputs
    {
       public Dictionary<string, string> GetEnvVars();
    }
}
