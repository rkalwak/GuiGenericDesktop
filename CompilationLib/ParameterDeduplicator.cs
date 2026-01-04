using System;
using System.Collections.Generic;
using System.Linq;

namespace CompilationLib
{
    /// <summary>
    /// Utility class for deduplicating parameters between BuildFlags and global settings
    /// </summary>
    public static class ParameterDeduplicator
    {
        /// <summary>
        /// Removes parameters from a BuildFlagItem that are already defined globally.
        /// This prevents duplicate parameter definitions when global parameters (like SDA/SCL) are used.
        /// </summary>
        /// <param name="buildFlag">The build flag item to process</param>
        /// <param name="globalParameters">List of globally defined parameters</param>
        /// <returns>A new BuildFlagItem with deduplicated parameters</returns>
        public static BuildFlagItem RemoveDuplicatedGlobalParameters(
            BuildFlagItem buildFlag, 
            List<Parameter> globalParameters)
        {
            if (buildFlag == null)
                throw new ArgumentNullException(nameof(buildFlag));

            if (globalParameters == null || !globalParameters.Any())
            {
                // No global parameters, return the original flag
                return buildFlag;
            }

            // Create a set of global parameter identifiers for quick lookup
            var globalParamIdentifiers = new HashSet<string>(
                globalParameters
                    .Where(p => !string.IsNullOrEmpty(p.Identifier))
                    .Select(p => p.Identifier),
                StringComparer.OrdinalIgnoreCase);

            // If the flag has no parameters, return it as-is
            if (buildFlag.Parameters == null || !buildFlag.Parameters.Any())
            {
                return buildFlag;
            }

            // Filter out parameters that exist in global parameters
            var deduplicatedParameters = buildFlag.Parameters
                .Where(p => !globalParamIdentifiers.Contains(p.Identifier))
                .ToList();

            // Create a new BuildFlagItem with deduplicated parameters
            // We don't modify the original to maintain immutability
            var deduplicatedFlag = new BuildFlagItem
            {
                Key = buildFlag.Key,
                FlagName = buildFlag.FlagName,
                Description = buildFlag.Description,
                Section = buildFlag.Section,
                IsEnabled = buildFlag.IsEnabled,
                EnabledByFlags = buildFlag.EnabledByFlags,
                DependenciesToDisable = buildFlag.DependenciesToDisable,
                DependenciesToEnable = buildFlag.DependenciesToEnable,
                BlockedByDisabledFlags = buildFlag.BlockedByDisabledFlags,
                SectionOrder = buildFlag.SectionOrder,
                Parameters = deduplicatedParameters,
                DisabledOnPlatforms = buildFlag.DisabledOnPlatforms
            };

            return deduplicatedFlag;
        }

        /// <summary>
        /// Removes duplicated global parameters from multiple BuildFlagItems at once
        /// </summary>
        /// <param name="buildFlags">Collection of build flags to process</param>
        /// <param name="globalParameters">List of globally defined parameters</param>
        /// <returns>A new list of BuildFlagItems with deduplicated parameters</returns>
        public static List<BuildFlagItem> RemoveDuplicatedGlobalParameters(
            IEnumerable<BuildFlagItem> buildFlags,
            List<Parameter> globalParameters)
        {
            if (buildFlags == null)
                throw new ArgumentNullException(nameof(buildFlags));

            if (globalParameters == null || !globalParameters.Any())
            {
                // No global parameters, return the original flags
                return buildFlags.ToList();
            }

            return buildFlags
                .Select(flag => RemoveDuplicatedGlobalParameters(flag, globalParameters))
                .ToList();
        }

        /// <summary>
        /// Gets the list of parameter identifiers that were removed from a BuildFlagItem
        /// </summary>
        /// <param name="buildFlag">The build flag to check</param>
        /// <param name="globalParameters">List of globally defined parameters</param>
        /// <returns>List of parameter identifiers that would be removed</returns>
        public static List<string> GetDuplicatedParameterIdentifiers(
            BuildFlagItem buildFlag,
            List<Parameter> globalParameters)
        {
            if (buildFlag == null || buildFlag.Parameters == null || !buildFlag.Parameters.Any())
                return new List<string>();

            if (globalParameters == null || !globalParameters.Any())
                return new List<string>();

            var globalParamIdentifiers = new HashSet<string>(
                globalParameters
                    .Where(p => !string.IsNullOrEmpty(p.Identifier))
                    .Select(p => p.Identifier),
                StringComparer.OrdinalIgnoreCase);

            return buildFlag.Parameters
                .Where(p => globalParamIdentifiers.Contains(p.Identifier))
                .Select(p => p.Identifier)
                .ToList();
        }
    }
}
