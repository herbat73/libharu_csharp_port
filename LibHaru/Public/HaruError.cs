namespace LibHaru;

public sealed class HaruError
{
    public HaruError(HaruErrorHandler? errorHandler = null, object? userData = null)
    {
        ErrorHandler = errorHandler;
        UserData = userData;
    }

    public uint ErrorNo { get; private set; }

    public uint DetailNo { get; private set; }

    public HaruErrorHandler? ErrorHandler { get; private set; }

    public object? UserData { get; private set; }

    public void SetHandler(HaruErrorHandler? errorHandler, object? userData = null)
    {
        ErrorHandler = errorHandler;

        if (userData is not null)
            UserData = userData;
    }

    public void Reset()
    {
        ErrorNo = HaruStatus.NoError;
        DetailNo = HaruStatus.NoError;
    }

    public uint SetError(uint errorNo, uint detailNo = HaruStatus.NoError)
    {
        ErrorNo = errorNo;
        DetailNo = detailNo;
        return errorNo;
    }

    public uint CheckError()
    {
        if (ErrorNo != HaruStatus.OK)
            ErrorHandler?.Invoke(ErrorNo, DetailNo, UserData);

        return ErrorNo;
    }

    public uint RaiseError(uint errorNo, uint detailNo = HaruStatus.NoError)
    {
        SetError(errorNo, detailNo);
        return CheckError();
    }
}