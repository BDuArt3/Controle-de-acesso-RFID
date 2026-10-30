using System.Net;
using ControleDeAcesso.Worker;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class AccessFeedbackSerialProtocolTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK, AccessFeedbackSerialProtocol.AccessGranted)]
    [InlineData(HttpStatusCode.Forbidden, AccessFeedbackSerialProtocol.AccessDenied)]
    [InlineData(HttpStatusCode.ServiceUnavailable, AccessFeedbackSerialProtocol.AccessDenied)]
    public void ForResponse_MapsHttpResultToFailClosedSerialCommand(
        HttpStatusCode statusCode,
        string expectedCommand)
    {
        using var response = new HttpResponseMessage(statusCode);

        var command = AccessFeedbackSerialProtocol.ForResponse(response);

        Assert.Equal(expectedCommand, command);
    }

    [Fact]
    public void ForResponse_RejectsNullResponse()
    {
        Assert.Throws<ArgumentNullException>(
            () => AccessFeedbackSerialProtocol.ForResponse(null!));
    }
}
