using System;

namespace LinqToXsd
{
    /// <summary>
    /// Represents an exception whereby two arguments were provided that are incompatible with each other.
    /// </summary>
    public class IncompatibleArgumentException: Exception
    {
        private readonly string otherArg;

        public string OtherArg => $"--{otherArg}";

        /// <param name="otherArgumentName">Use only the argument name without any auxiliary characters such as dashes or hyphens or slashes.</param>
        /// <param name="innerException">Optional inner <see cref="Exception"/></param>
        public IncompatibleArgumentException(string otherArgumentName, Exception innerException = null)
            : base(nameof(IncompatibleArgumentException) + " occurred. An argument provided isn't compatible with another that was given: '" + otherArgumentName + "'", innerException)
        {
            otherArg = otherArgumentName;
        }
    }
}