using System;
using System.Text;
using FluentAssertions;
using Xunit;
using CompilationLib;

namespace CompilationLib.Tests
{
    public class SpiffsVersionParserTests
    {
        // Helper: wrap a version string in surrounding binary noise, as it appears in a real SPIFFS dump
        private static byte[] BuildSpiffsData(string versionString, int prefixPadding = 16, int suffixPadding = 16)
        {
            var prefix = new byte[prefixPadding];
            var content = Encoding.ASCII.GetBytes(versionString);
            var suffix = new byte[suffixPadding];
            return [.. prefix, .. content, .. suffix];
        }

        // ── Z2S-style patterns ─────────────────────────────────────────────────

        [Fact]
        public void FindVersion_Z2S_FullPattern_ReturnsVersionSegment()
        {
            // "SV-Z2S-1.5.1-07/04/26" → everything after "SV-" = "Z2S-1.5.1-07/04/26"
            var data = BuildSpiffsData("SV-Z2S-1.5.1-07/04/26");
            SpiffsVersionParser.FindVersion(data).Should().Be("Z2S-1.5.1-07/04/26");
        }

        [Fact]
        public void FindVersion_SimpleVersion_ReturnsThatVersion()
        {
            // "SV-1.5.1" → parts[1] = "1.5.1"
            var data = BuildSpiffsData("SV-1.5.1");
            SpiffsVersionParser.FindVersion(data).Should().Be("1.5.1");
        }

        [Fact]
        public void FindVersion_GuiGenericDate_ReturnsDateSegment()
        {
            // "SV-26.04.17" → parts[1] = "26.04.17"
            var data = BuildSpiffsData("SV-26.04.17");
            SpiffsVersionParser.FindVersion(data).Should().Be("26.04.17");
        }

        [Fact]
        public void FindVersion_VersionWithExtraSegments_ReturnsFullVersion()
        {
            // "SV-2.0.0-extra-data" → everything after "SV-" = "2.0.0-extra-data"
            var data = BuildSpiffsData("SV-2.0.0-extra-data");
            SpiffsVersionParser.FindVersion(data).Should().Be("2.0.0-extra-data");
        }

        // ── Marker not present ─────────────────────────────────────────────────

        [Fact]
        public void FindVersion_MarkerAbsent_ReturnsNull()
        {
            var data = BuildSpiffsData("SOMETHING-UNRELATED-1.2.3");
            SpiffsVersionParser.FindVersion(data).Should().BeNull();
        }

        [Fact]
        public void FindVersion_EmptyData_ReturnsNull()
        {
            SpiffsVersionParser.FindVersion([]).Should().BeNull();
        }

        [Fact]
        public void FindVersion_NullData_ReturnsNull()
        {
            SpiffsVersionParser.FindVersion(null).Should().BeNull();
        }

        // ── Custom marker ──────────────────────────────────────────────────────

        [Fact]
        public void FindVersion_CustomMarker_FindsVersion()
        {
            // "Z2S-1.5.1-07/04/26" with marker "Z2S-" → everything after "Z2S-" = "1.5.1-07/04/26"
            var data = BuildSpiffsData("Z2S-1.5.1-07/04/26");
            SpiffsVersionParser.FindVersion(data, "Z2S-").Should().Be("1.5.1-07/04/26");
        }

        [Fact]
        public void FindVersion_CustomMarkerAbsent_ReturnsNull()
        {
            var data = BuildSpiffsData("SV-1.5.1");
            SpiffsVersionParser.FindVersion(data, "Z2S-").Should().BeNull();
        }

        // ── Marker at various positions in the buffer ──────────────────────────

        [Fact]
        public void FindVersion_MarkerAtStart_ReturnsVersion()
        {
            var data = Encoding.ASCII.GetBytes("SV-1.0.0");
            SpiffsVersionParser.FindVersion(data).Should().Be("1.0.0");
        }

        [Fact]
        public void FindVersion_MarkerAtEnd_ReturnsVersion()
        {
            var data = BuildSpiffsData("SV-9.9.9", prefixPadding: 100, suffixPadding: 0);
            SpiffsVersionParser.FindVersion(data).Should().Be("9.9.9");
        }

        [Fact]
        public void FindVersion_MarkerInLargeBuffer_ReturnsVersion()
        {
            // Simulate a 64 KB SPIFFS chunk with version buried in the middle
            var buffer = new byte[65536];
            var versionBytes = Encoding.ASCII.GetBytes("SV-3.2.1");
            Array.Copy(versionBytes, 0, buffer, 32000, versionBytes.Length);
            SpiffsVersionParser.FindVersion(buffer).Should().Be("3.2.1");
        }

        // ── Edge cases ─────────────────────────────────────────────────────────

        [Fact]
        public void FindVersion_VersionSegmentIsWhitespaceOnly_ReturnsNull()
        {
            var data = Encoding.ASCII.GetBytes("SV-   ");
            SpiffsVersionParser.FindVersion(data).Should().BeNull();
        }

        [Fact]
        public void FindVersion_ControlCharTerminatesToken_ReturnsVersionBeforeControlChar()
        {
            // Version string followed by null bytes (typical in SPIFFS)
            var content = Encoding.ASCII.GetBytes("SV-1.2.3");
            var data = new byte[content.Length + 4];
            content.CopyTo(data, 0);
            SpiffsVersionParser.FindVersion(data).Should().Be("1.2.3");
        }

        [Fact]
        public void FindVersion_TwoMarkersInBuffer_ReturnsFirstMatch()
        {
            var first = Encoding.ASCII.GetBytes("SV-1.0.0");
            var gap = new byte[20];
            var second = Encoding.ASCII.GetBytes("SV-2.0.0");
            var data = new byte[first.Length + gap.Length + second.Length];
            first.CopyTo(data, 0);
            gap.CopyTo(data, first.Length);
            second.CopyTo(data, first.Length + gap.Length);

            SpiffsVersionParser.FindVersion(data).Should().Be("1.0.0");
        }

        [Fact]
        public void FindVersion_NullOrEmptyMarker_ReturnsNull()
        {
            var data = BuildSpiffsData("SV-1.0.0");
            SpiffsVersionParser.FindVersion(data, null).Should().BeNull();
            SpiffsVersionParser.FindVersion(data, "").Should().BeNull();
        }
    }
}
