using LibHaru;

namespace LibHaru.Tests;

public sealed class HaruErrorTests
{
    [Fact]
    public void CheckError_WhenNoError_DoesNotInvokeHandler()
    {
        var calls = 0;
        var error = new HaruError((_, _, _) => calls++);

        var status = error.CheckError();

        Assert.Equal(HaruStatus.NoError, status);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void SetError_StoresStatusUntilReset()
    {
        var error = new HaruError();

        var status = error.SetError(HaruStatus.InvalidPage, HaruStatus.InvalidPageIndex);

        Assert.Equal(HaruStatus.InvalidPage, status);
        Assert.Equal(HaruStatus.InvalidPage, error.ErrorNo);
        Assert.Equal(HaruStatus.InvalidPageIndex, error.DetailNo);

        error.Reset();

        Assert.Equal(HaruStatus.NoError, error.ErrorNo);
        Assert.Equal(HaruStatus.NoError, error.DetailNo);
    }

    [Fact]
    public void CheckError_WhenErrorIsSet_InvokesHandlerWithUserData()
    {
        var userData = new object();
        var calls = new List<(uint ErrorNo, uint DetailNo, object? UserData)>();
        var error = new HaruError(
            (errorNo, detailNo, data) => calls.Add((errorNo, detailNo, data)),
            userData);

        error.SetError(HaruStatus.InvalidFontName, HaruStatus.InvalidEncodingName);

        var status = error.CheckError();

        Assert.Equal(HaruStatus.InvalidFontName, status);
        var call = Assert.Single(calls);
        Assert.Equal(HaruStatus.InvalidFontName, call.ErrorNo);
        Assert.Equal(HaruStatus.InvalidEncodingName, call.DetailNo);
        Assert.Same(userData, call.UserData);
    }

    [Fact]
    public void SetHandler_ReplacesHandlerAndUserData()
    {
        var firstUserData = new object();
        var secondUserData = new object();
        var firstCalls = 0;
        var secondCalls = 0;
        var error = new HaruError((_, _, _) => firstCalls++, firstUserData);

        error.SetHandler((_, _, data) =>
        {
            Assert.Same(secondUserData, data);
            secondCalls++;
        }, secondUserData);

        error.RaiseError(HaruStatus.InvalidParameter);

        Assert.Equal(0, firstCalls);
        Assert.Equal(1, secondCalls);
        Assert.Same(secondUserData, error.UserData);
    }
}
