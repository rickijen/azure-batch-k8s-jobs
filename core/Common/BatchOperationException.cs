using System;

namespace core.Common
{
  public class BatchOperationException : Exception
  {
     public BatchOperationException()
     {
     }

     public BatchOperationException(string message) : base(message)
     {
     }

     public BatchOperationException(string message, Exception inner) : base(message, inner)
     {
     }
  }
}
