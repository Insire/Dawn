namespace Dawn.Core.Features.Util
{
    public interface IClipboardService
    {
        Task SetDataAsync(string text);

        Task SetTextAsync(string text);
    }
}
