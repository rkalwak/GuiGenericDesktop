using System;
using System.Collections.Generic;
using System.Linq;

namespace CompilationLib
{
    public static class DependencyResolver
    {
        public static List<BuildFlagItem> FindOnDependencies(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags)
        {
            if (flag == null) return new List<BuildFlagItem>();
            if (allFlags == null) return new List<BuildFlagItem>();

            var key = (flag.Key ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(key)) return new List<BuildFlagItem>();

            return allFlags
                .Where(f => f.DependenciesToEnable != null && f.DependenciesToEnable.Any(d => string.Equals(d?.Trim(), key, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public static List<BuildFlagItem> FindOffDependencies(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags)
        {
            if (flag == null) return new List<BuildFlagItem>();
            if (allFlags == null) return new List<BuildFlagItem>();

            var key = (flag.Key ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(key)) return new List<BuildFlagItem>();

            return allFlags
                .Where(f => f.DependenciesToDisable != null && f.DependenciesToDisable.Any(d => string.Equals(d?.Trim(), key, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }
}
