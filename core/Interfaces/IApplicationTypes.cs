using System;
using System.Collections.Generic;

namespace core.Interfaces
{
    // Constants defined in this interface either specify a 'blob' name or a 'folder' name 
    // containing blobs
    public interface IApplicationTypes
    {
       // Name of batch start task to execute as soon as vm/node joins batch pool
       public const string K8S_SINGLE_NODE = "k8s-single-node.sh";
    }
}
