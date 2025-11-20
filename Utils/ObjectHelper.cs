using System;
using System.Reflection;

namespace Pidar.Helpers
{
    public static class ObjectHelper
    {
        /// <summary>
        /// Safely gets a nested property using "A.B.C" format.
        /// Returns null if object or any property is missing.
        /// </summary>
        public static string? GetValue(object? root, string path)
        {
            if (root == null || string.IsNullOrWhiteSpace(path))
                return null;

            object? current = root;

            foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (current == null)
                    return null;

                PropertyInfo? prop = current.GetType().GetProperty(part);
                if (prop == null)
                    return null;

                current = prop.GetValue(current);
            }

            return current?.ToString();
        }
    }
}
