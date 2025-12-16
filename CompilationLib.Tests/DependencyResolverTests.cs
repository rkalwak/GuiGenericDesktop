using System.Collections.Generic;
using Xunit;

namespace CompilationLib.Tests
{
    public class DependencyResolverTests
    {
        #region Test Data Helpers

        private static BuildFlagItem CreateFlag(
            string key, 
            bool isEnabled = false,
            List<string> enabledByDependencies = null,
            List<string> dependenciesToEnable = null,
            List<string> dependenciesToDisable = null,
            List<string> disabledByDependencies = null)
        {
            return new BuildFlagItem
            {
                Key = key,
                FlagName = $"{key}_Name",
                IsEnabled = isEnabled,
                EnabledByFlags = enabledByDependencies,
                DependenciesToEnable = dependenciesToEnable,
                DependenciesToDisable = dependenciesToDisable,
                BlockedByDisabledFlags = disabledByDependencies
            };
        }

        #endregion

        #region ProcessFlagEnabled - Basic Tests

        [Fact]
        public void ProcessFlagEnabled_WithNullFlag_ReturnsError()
        {
            var allFlags = new List<BuildFlagItem>();
            var result = DependencyResolver.ProcessFlagEnabled(null, allFlags);
            Assert.NotNull(result);
            Assert.Contains("Flag is null", result);
        }

        [Fact]
        public void ProcessFlagEnabled_WithNullAllFlags_ReturnsError()
        {
            var flag = CreateFlag("FLAG_A");
            var result = DependencyResolver.ProcessFlagEnabled(flag, null);
            Assert.NotNull(result);
            Assert.Contains("All flags collection is null", result);
        }

        [Fact]
        public void ProcessFlagEnabled_NoBlockingDependencies_Succeeds()
        {
            var relay = CreateFlag("SUPLA_RELAY");
            var allFlags = new List<BuildFlagItem> { relay };
            
            var result = DependencyResolver.ProcessFlagEnabled(relay, allFlags);
            
            Assert.Null(result);
        }

        [Fact]
        public void ProcessFlagEnabled_BlockingDepOffDependencies_ReturnsError()
        {
            var relay = CreateFlag("SUPLA_RELAY", isEnabled: false);
            var button = CreateFlag("SUPLA_BUTTON", 
                disabledByDependencies: new List<string> { "SUPLA_RELAY" });
            
            var allFlags = new List<BuildFlagItem> { relay, button };
            
            var result = DependencyResolver.ProcessFlagEnabled(button, allFlags);
            
            Assert.NotNull(result);
            Assert.Contains("Cannot enable", result);
            Assert.Contains("SUPLA_BUTTON", result);
        }

        #endregion

        #region ProcessFlagEnabled - depOpt (DependenciesToEnable) Tests

        [Fact]
        public void ProcessFlagEnabled_DepOptEnablesMultipleFlags()
        {
            var thermostat = CreateFlag("SUPLA_THERMOSTAT", 
                dependenciesToEnable: new List<string> { "SUPLA_RELAY", "SUPLA_LED" });
            var relay = CreateFlag("SUPLA_RELAY");
            var led = CreateFlag("SUPLA_LED");
            
            var allFlags = new List<BuildFlagItem> { thermostat, relay, led };
            
            var result = DependencyResolver.ProcessFlagEnabled(thermostat, allFlags);
            
            Assert.Null(result);
            Assert.True(relay.IsEnabled);
            Assert.True(led.IsEnabled);
        }

        #endregion

        #region ProcessFlagEnabled - depOn Tests

        [Fact]
        public void ProcessFlagEnabled_DepOnEnablesMultipleFlags()
        {
            var rollershutter = CreateFlag("SUPLA_ROLLERSHUTTER");
            var relay = CreateFlag("SUPLA_RELAY", 
                enabledByDependencies: new List<string> { "SUPLA_ROLLERSHUTTER" });
            var button = CreateFlag("SUPLA_BUTTON", 
                enabledByDependencies: new List<string> { "SUPLA_ROLLERSHUTTER" });
            
            var allFlags = new List<BuildFlagItem> { rollershutter, relay, button };
            
            var result = DependencyResolver.ProcessFlagEnabled(rollershutter, allFlags);
            
            Assert.Null(result);
            Assert.True(relay.IsEnabled);
            Assert.True(button.IsEnabled);
        }

        [Fact]
        public void ProcessFlagEnabled_DepOnOverridesDepOff()
        {
            var rollershutter = CreateFlag("SUPLA_ROLLERSHUTTER");
            var relay = CreateFlag("SUPLA_RELAY", isEnabled: false);
            var rgbw = CreateFlag("SUPLA_RGBW", isEnabled: false);
            var actionTrigger = CreateFlag("SUPLA_ACTION_TRIGGER", isEnabled: false);
            var button = CreateFlag("SUPLA_BUTTON", 
                enabledByDependencies: new List<string> { "SUPLA_ROLLERSHUTTER" },
                disabledByDependencies: new List<string> { "SUPLA_RELAY", "SUPLA_ROLLERSHUTTER", "SUPLA_RGBW", "SUPLA_ACTION_TRIGGER" });
            
            var allFlags = new List<BuildFlagItem> { rollershutter, relay, rgbw, actionTrigger, button };
            
            var result = DependencyResolver.ProcessFlagEnabled(rollershutter, allFlags);
            
            Assert.Null(result);
            Assert.True(button.IsEnabled);
        }

        #endregion

        #region ProcessFlagEnabled - depRel (Mutual Exclusion) Tests

        [Fact]
        public void ProcessFlagEnabled_MutualExclusion_DisablesConflictingFlag()
        {
            var ntc = CreateFlag("SUPLA_NTC_10K", 
                dependenciesToDisable: new List<string> { "SUPLA_MPX_5XXX" });
            var mpx = CreateFlag("SUPLA_MPX_5XXX", isEnabled: true);
            
            var allFlags = new List<BuildFlagItem> { ntc, mpx };
            
            Assert.True(mpx.IsEnabled);
            
            var result = DependencyResolver.ProcessFlagEnabled(ntc, allFlags);
            
            Assert.Null(result);
            Assert.False(mpx.IsEnabled, "MPX should be automatically disabled");
        }

        [Fact]
        public void ProcessFlagEnabled_MutualExclusion_WorksBidirectionally()
        {
            var ntc = CreateFlag("SUPLA_NTC_10K", 
                dependenciesToDisable: new List<string> { "SUPLA_MPX_5XXX" });
            var mpx = CreateFlag("SUPLA_MPX_5XXX",
                dependenciesToDisable: new List<string> { "SUPLA_NTC_10K" });
            
            var allFlags = new List<BuildFlagItem> { ntc, mpx };
            
            // Enable NTC (should disable MPX)
            mpx.IsEnabled = true;
            var result = DependencyResolver.ProcessFlagEnabled(ntc, allFlags);
            ntc.IsEnabled = true;
            
            Assert.Null(result);
            Assert.True(ntc.IsEnabled);
            Assert.False(mpx.IsEnabled);
            
            // Enable MPX (should disable NTC)
            ntc.IsEnabled = true;
            mpx.IsEnabled = false;
            result = DependencyResolver.ProcessFlagEnabled(mpx, allFlags);
            mpx.IsEnabled = true;
            
            Assert.Null(result);
            Assert.False(ntc.IsEnabled);
            Assert.True(mpx.IsEnabled);
        }

        #endregion

        #region ProcessFlagDisabled Tests

        [Fact]
        public void ProcessFlagDisabled_DepRel_DisablesMutualExclusionFlags()
        {
            var ntc = CreateFlag("SUPLA_NTC_10K", 
                dependenciesToDisable: new List<string> { "SUPLA_MPX_5XXX" });
            var mpx = CreateFlag("SUPLA_MPX_5XXX", isEnabled: true);
            
            var allFlags = new List<BuildFlagItem> { ntc, mpx };
            
            DependencyResolver.ProcessFlagDisabled(ntc, allFlags);
            
            Assert.False(mpx.IsEnabled);
        }

        [Fact]
        public void ProcessFlagDisabled_DepOff_DisablesBlockedFlags()
        {
            var relay = CreateFlag("SUPLA_RELAY");
            var button = CreateFlag("SUPLA_BUTTON", isEnabled: true,
                disabledByDependencies: new List<string> { "SUPLA_RELAY" });
            var rollershutter = CreateFlag("SUPLA_ROLLERSHUTTER", isEnabled: true,
                disabledByDependencies: new List<string> { "SUPLA_RELAY" });
            
            var allFlags = new List<BuildFlagItem> { relay, button, rollershutter };
            
            DependencyResolver.ProcessFlagDisabled(relay, allFlags);
            
            Assert.False(button.IsEnabled);
            Assert.False(rollershutter.IsEnabled);
        }

        [Fact]
        public void ProcessFlagDisabled_HandlesNullParameters()
        {
            DependencyResolver.ProcessFlagDisabled(null, new List<BuildFlagItem>());
            DependencyResolver.ProcessFlagDisabled(CreateFlag("TEST"), null);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void ProcessFlagEnabled_SUPLA_ROLLERSHUTTER_EnablesRelayAndButton()
        {
            var rollershutter = CreateFlag("SUPLA_ROLLERSHUTTER");
            var relay = CreateFlag("SUPLA_RELAY", 
                enabledByDependencies: new List<string> { "SUPLA_ROLLERSHUTTER" });
            var button = CreateFlag("SUPLA_BUTTON", 
                enabledByDependencies: new List<string> { "SUPLA_ROLLERSHUTTER" },
                disabledByDependencies: new List<string> { "SUPLA_RELAY", "SUPLA_ROLLERSHUTTER", "SUPLA_RGBW", "SUPLA_ACTION_TRIGGER" });
            var rgbw = CreateFlag("SUPLA_RGBW", isEnabled: false);
            var actionTrigger = CreateFlag("SUPLA_ACTION_TRIGGER", isEnabled: false);

            var allFlags = new List<BuildFlagItem> { rollershutter, relay, button, rgbw, actionTrigger };
            
            var result = DependencyResolver.ProcessFlagEnabled(rollershutter, allFlags);
            
            Assert.Null(result);
            Assert.True(relay.IsEnabled, "RELAY should be enabled");
            Assert.True(button.IsEnabled, "BUTTON should be enabled despite missing RGBW and ACTION_TRIGGER");
            Assert.False(rgbw.IsEnabled, "RGBW should remain disabled");
            Assert.False(actionTrigger.IsEnabled, "ACTION_TRIGGER should remain disabled");
        }

        [Fact]
        public void ProcessFlagEnabled_ErrorMessage_IncludesUserFriendlyNames()
        {
            var relay = CreateFlag("SUPLA_RELAY", isEnabled: false);
            relay.FlagName = "Relays";
            var rollershutter = CreateFlag("SUPLA_ROLLERSHUTTER", isEnabled: false);
            rollershutter.FlagName = "Roller shutters";
            var button = CreateFlag("SUPLA_BUTTON", 
                disabledByDependencies: new List<string> { "SUPLA_RELAY", "SUPLA_ROLLERSHUTTER" });
            
            var allFlags = new List<BuildFlagItem> { relay, rollershutter, button };
            
            var result = DependencyResolver.ProcessFlagEnabled(button, allFlags);
            
            Assert.NotNull(result);
            Assert.Contains("Relays", result);
            Assert.Contains("Roller shutters", result);
        }

        #endregion
    }
}
