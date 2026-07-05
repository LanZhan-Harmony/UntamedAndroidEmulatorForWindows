namespace UntamedAndroidSubsystem.Core.HyperV;

public sealed class HcsException : Exception
{
    public HcsException(string operation, int hresult, string? resultDocument = null)
        : base(CreateMessage(operation, hresult, resultDocument))
    {
        Operation = operation;
        HResultCode = hresult;
        ResultDocument = resultDocument;
    }

    public string Operation { get; }

    public int HResultCode { get; }

    public string? ResultDocument { get; }

    private static string CreateMessage(string operation, int hresult, string? resultDocument)
    {
        string hex = unchecked((uint)hresult).ToString("X8");
        return string.IsNullOrWhiteSpace(resultDocument)
            ? $"{operation} failed with HRESULT 0x{hex}."
            : $"{operation} failed with HRESULT 0x{hex}: {resultDocument}";
    }
}
