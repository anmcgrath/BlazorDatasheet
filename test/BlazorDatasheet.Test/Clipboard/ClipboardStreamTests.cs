using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using BlazorDatasheet.Services;
using Microsoft.JSInterop;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Clipboard;

public class ClipboardStreamTests
{
    private class FakeStreamReference(string json) : IJSStreamReference
    {
        public long Length => Encoding.UTF8.GetByteCount(json);

        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Test]
    public async Task Reads_Large_Content_Sent_As_A_Stream()
    {
        var html = "<table>" + new string('x', 100_000) + "</table>";
        var data = await ClipboardStream.Read(
            new FakeStreamReference("{\"text\":\"a\\tb\",\"html\":\"" + html + "\"}"));
        data!.Text.Should().Be("a\tb");
        data.Html.Should().Be(html);
    }

    [Test]
    public async Task Null_Reference_Is_No_Content()
    {
        (await ClipboardStream.Read(null)).Should().BeNull();
    }
}
