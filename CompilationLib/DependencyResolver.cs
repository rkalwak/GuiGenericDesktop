using System;
using System.Collections.Generic;
using System.Linq;

namespace CompilationLib
{
    public static class DependencyResolver
    {
        /// <summary>
        /// Finds flags that THIS flag will auto-enable when enabled.
        /// Returns flags listed in THIS flag's DependenciesToEnable (depOpt).
        /// Example: If SUPLA_THERMOSTAT has depOpt:["SUPLA_RELAY"], this returns SUPLA_RELAY.
        /// </summary>
        private static List<BuildFlagItem> FindDependenciesToEnable(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags)
        {
            if (flag == null) return new List<BuildFlagItem>();
            if (allFlags == null) return new List<BuildFlagItem>();
            if (flag.DependenciesToEnable == null || !flag.DependenciesToEnable.Any())
                return new List<BuildFlagItem>();
                
            var result = new List<BuildFlagItem>();
            foreach (var depKey in flag.DependenciesToEnable.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                var dependency = allFlags.FirstOrDefault(f =>
                    string.Equals(f.Key?.Trim(), depKey?.Trim(), StringComparison.OrdinalIgnoreCase));
                
                if (dependency != null && !result.Contains(dependency))
                {
                    result.Add(dependency);
                }
            }

            return result;
        }
        
        /// <summary>
        /// Finds flags that will auto-enable THIS flag when any of them is enabled.
        /// Returns flags listed in THIS flag's EnabledByFlags (depOn).
        /// Example: If SUPLA_RELAY has depOn:["SUPLA_ROLLERSHUTTER"], this returns SUPLA_ROLLERSHUTTER.
        /// </summary>
        private static List<BuildFlagItem> FindFlagsThatAutoEnableThisFlag(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags)
        {
            if (flag == null) return new List<BuildFlagItem>();
            if (allFlags == null) return new List<BuildFlagItem>();
            if (flag.EnabledByFlags == null || !flag.EnabledByFlags.Any())
                return new List<BuildFlagItem>();
            
            var result = new List<BuildFlagItem>();
            foreach (var depKey in flag.EnabledByFlags.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                var dependency = allFlags.FirstOrDefault(f =>
                    string.Equals(f.Key?.Trim(), depKey?.Trim(), StringComparison.OrdinalIgnoreCase));
                
                if (dependency != null && !result.Contains(dependency))
                {
                    result.Add(dependency);
                }
            }

            return result;
        }

        /// <summary>
        /// Finds flags that THIS flag will disable when enabled (mutual exclusion).
        /// Returns flags listed in THIS flag's DependenciesToDisable (depRel).
        /// Example: If SUPLA_NTC_10K has depRel:["SUPLA_MPX_5XXX"], this returns SUPLA_MPX_5XXX.
        /// </summary>
        private static List<BuildFlagItem> FindDependenciesToDisable(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags)
        {
            if (flag == null) return new List<BuildFlagItem>();
            if (allFlags == null) return new List<BuildFlagItem>();
            if (flag.DependenciesToDisable == null || !flag.DependenciesToDisable.Any())
                return new List<BuildFlagItem>();
            
            var result = new List<BuildFlagItem>();
            foreach (var depKey in flag.DependenciesToDisable.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                var dependency = allFlags.FirstOrDefault(f =>
                    string.Equals(f.Key?.Trim(), depKey?.Trim(), StringComparison.OrdinalIgnoreCase));
                
                if (dependency != null && !result.Contains(dependency))
                {
                    result.Add(dependency);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if there are any blocking dependencies that prevent the flag from being enabled.
        /// A flag is blocked if any of its DisabledByDependencies (depOff) are currently disabled.
        /// </summary>
        /// <param name="flag">The flag to check for blocking dependencies</param>
        /// <param name="allFlags">All available flags to search through</param>
        /// <returns>True if there are blocking dependencies (flag cannot be enabled), false otherwise</returns>
        private static bool ExistBlockingDependency(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags)
        {
            if (flag == null) return false;
            if (allFlags == null) return false;
            if (flag.BlockedByDisabledFlags == null || !flag.BlockedByDisabledFlags.Any()) return false;

            // Check if any of the dependencies that should be enabled are actually disabled
            foreach (var depKey in flag.BlockedByDisabledFlags.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                var dependency = allFlags.FirstOrDefault(f => 
                    string.Equals(f.Key?.Trim(), depKey?.Trim(), StringComparison.OrdinalIgnoreCase));
                
                // If the dependency exists and is NOT enabled, then it's blocking
                if (dependency != null && !dependency.IsEnabled)
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Finds all flags that have the given flag in their DisabledByDependencies list.
        /// These are flags that will be blocked when the given flag is disabled.
        /// </summary>
        private static List<BuildFlagItem> FindFlagsBlockedByDisabling(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags)
        {
            if (flag == null) return new List<BuildFlagItem>();
            if (allFlags == null) return new List<BuildFlagItem>();

            var key = (flag.Key ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(key)) return new List<BuildFlagItem>();

            return allFlags
                .Where(f => f.BlockedByDisabledFlags != null && 
                           f.BlockedByDisabledFlags.Any(d => string.Equals(d?.Trim(), key, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        
        /// <summary>
        /// Finds flags that should be auto-enabled when THIS flag is enabled.
        /// Searches for flags that have THIS flag in their EnabledByFlags (depOn) list.
        /// Example: If SUPLA_BUTTON has depOn:["SUPLA_ACTION_TRIGGER"], enabling SUPLA_ACTION_TRIGGER will return SUPLA_BUTTON.
        /// </summary>
        private static List<BuildFlagItem> FindFlagsToAutoEnableByThisFlag(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags)
        {
            if (flag == null) return new List<BuildFlagItem>();
            if (allFlags == null) return new List<BuildFlagItem>();

            var key = (flag.Key ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(key)) return new List<BuildFlagItem>();

            return allFlags
                .Where(f => f.EnabledByFlags != null && 
                           f.EnabledByFlags.Any(d => string.Equals(d?.Trim(), key, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        
        /// <summary>
        /// Gets the list of blocking dependency names (user-friendly names or keys) for a flag.
        /// </summary>
        /// <param name="flag">The flag to check</param>
        /// <param name="allFlags">All available flags</param>
        /// <returns>List of user-friendly names of blocking dependencies</returns>
        private static List<string> GetBlockingDependencyNames(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags)
        {
            if (flag == null) return new List<string>();
            if (allFlags == null) return new List<string>();
            if (flag.BlockedByDisabledFlags == null || !flag.BlockedByDisabledFlags.Any())
                return new List<string>();

            return allFlags
                .Where(f => flag.BlockedByDisabledFlags.Any(d => 
                    string.Equals(d?.Trim(), f.Key?.Trim(), StringComparison.OrdinalIgnoreCase)) && 
                    !f.IsEnabled)
                .Select(f => f.FlagName ?? f.Key)
                .ToList();
        }
        
        /// <summary>
        /// Processes the enabling of a flag and all its dependencies.
        /// Returns null if successful, or an error message if the flag cannot be enabled.
        /// </summary>
        /// <param name="flag">The flag to enable</param>
        /// <param name="allFlags">All available flags</param>
        /// <returns>Error message if flag cannot be enabled, null if successful</returns>
        public static string ProcessFlagEnabled(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags)
        {
            if (flag == null) return "Flag is null";
            if (allFlags == null) return "All flags collection is null";

            // Check for blocking dependencies before enabling (depOff)
            if (ExistBlockingDependency(flag, allFlags))
            {
                var blockingDeps = GetBlockingDependencyNames(flag, allFlags);
                if (blockingDeps.Any())
                {
                    return $"Cannot enable '{flag.FlagName ?? flag.Key}'.\n\n" +
                           $"The following dependencies must be enabled first:\n" +
                           string.Join("\n", blockingDeps);
                }
            }
            
            // Disable mutually exclusive flags (depRel - mutual exclusion)
            // When this flag is enabled, disable any flags that are mutually exclusive with it
            var mutuallyExclusiveFlags = FindDependenciesToDisable(flag, allFlags);
            foreach (var exclusiveFlag in mutuallyExclusiveFlags.Where(f => f.IsEnabled))
            {
                exclusiveFlag.IsEnabled = false;
            }
            
            // Enable the flag itself
            flag.IsEnabled = true;
            
            // Enable flags that this flag auto-enables (depOpt)
            var depsToEnable = FindDependenciesToEnable(flag, allFlags);
            foreach (var d in depsToEnable)
            {
                d.IsEnabled = true;
            }
            
            // Enable flags that have this flag in their depOn list (auto-enable ignores depOff!)
            var autoEnabledByThis = FindFlagsToAutoEnableByThisFlag(flag, allFlags);
            foreach (var d in autoEnabledByThis)
            {
                // Auto-enable via depOn ALWAYS works (ignores depOff requirements)
                // depOff only blocks MANUAL enabling, not auto-enabling via depOn
                d.IsEnabled = true;
            }
            
            return null; // Success
        }
        
        /// <summary>
        /// Processes the disabling of a flag and all affected dependencies.
        /// </summary>
        /// <param name="flag">The flag to disable</param>
        /// <param name="allFlags">All available flags</param>
        public static void ProcessFlagDisabled(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags)
        {
            if (flag == null) return;
            if (allFlags == null) return;
            
            // Disable the flag itself
            flag.IsEnabled = false;
            
            // Disable flags that must be disabled when this flag is disabled (depRel)
            var depsToDisable = FindDependenciesToDisable(flag, allFlags);
            foreach (var d in depsToDisable)
            {
                d.IsEnabled = false;
            }
            
            // Auto-disable flags that are blocked by this flag being disabled (depOff)
            var blockedFlags = FindFlagsBlockedByDisabling(flag, allFlags);
            foreach (var blocked in blockedFlags.Where(f => f.IsEnabled))
            {
                blocked.IsEnabled = false;
            }
        }
     }
 }
