namespace Prm.Application.Common;

public static class ApiKeyMasker
{
    public static string Mask(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        if (key.Length <= MaskingConstants.VisibleSuffixLength)
        {
            return MaskingConstants.ShortKeyMask;
        }

        var maskLength = Math.Max(
            MaskingConstants.MinimumMaskLength,
            key.Length - MaskingConstants.VisibleSuffixLength);

        return $"{new string('*', maskLength)}{key[^MaskingConstants.VisibleSuffixLength..]}";
    }
}
