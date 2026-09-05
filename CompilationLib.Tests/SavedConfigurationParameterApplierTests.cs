using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace CompilationLib.Tests
{
    public class SavedConfigurationParameterApplierTests
    {
        [Fact]
        public void Apply_ShouldRestoreDynamicRelayParametersMissingFromBuilderMetadata()
        {
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_RELAY",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "Count", Name = "Count", Type = "number", Value = "1" }
                }
            };

            var savedParameters = new Dictionary<string, string>
            {
                ["GPIO1DirectLinksOn"] = "on",
                ["GPIO1DirectLinksOff"] = "off",
                ["GPIO2ThermostatType"] = "2"
            };

            SavedConfigurationParameterApplier.Apply(flag, savedParameters);

            flag.Parameters.Should().Contain(p =>
                string.Equals(p.Identifier, "GPIO1DirectLinksOn", StringComparison.OrdinalIgnoreCase) &&
                p.Value == "on");
            flag.Parameters.Should().Contain(p =>
                string.Equals(p.Identifier, "GPIO1DirectLinksOff", StringComparison.OrdinalIgnoreCase) &&
                p.Value == "off");
            flag.Parameters.Should().Contain(p =>
                string.Equals(p.Identifier, "GPIO2ThermostatType", StringComparison.OrdinalIgnoreCase) &&
                p.Value == "2");
        }
    }
}
