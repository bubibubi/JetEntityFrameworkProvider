using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// Illegal Db Expression Exception
    /// </summary>
    [Serializable]
    public class IllegalDbExpressionException:InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IllegalDbExpressionException"/> class.
        /// </summary>
        public IllegalDbExpressionException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="IllegalDbExpressionException" /> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public IllegalDbExpressionException(string message) : base(message) { }


        /// <summary>
        /// Initializes a new instance of the <see cref="IllegalDbExpressionException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the <paramref name="innerException" /> parameter is not a null reference (Nothing in Visual Basic), the current exception is raised in a catch block that handles the inner exception.</param>
        public IllegalDbExpressionException(string message, Exception innerException) : base(message, innerException) { }

        protected IllegalDbExpressionException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="IllegalDbExpressionException"/> class.
        /// </summary>
        /// <param name="type">The type of the expression.</param>
        public IllegalDbExpressionException(Type type) : base(string.Format("DbExpression is illegal in output query command tree ({0})", type == null ? "Unknown" : type.Name))
        {

        }
    }
}
