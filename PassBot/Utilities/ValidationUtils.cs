using System.Text.RegularExpressions;

namespace PassBot.Utilities
{
    public static class ValidationUtils
    {
        public static bool IsValidEmail(string email)
        {
            const string pattern = @"^(?!.*[<>])(?:(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*)|(?:""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*""))@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$";

            var emailRegex = new System.Text.RegularExpressions.Regex(
                pattern,
                RegexOptions.IgnoreCase
            );

            return emailRegex.IsMatch(email);
        }

        public static bool IsValidEthereumAddress(string walletAddress)
        {
            try
            {
                var ethAddressRegex = new System.Text.RegularExpressions.Regex(@"^0x[a-fA-F0-9]{40}$");
                return ethAddressRegex.IsMatch(walletAddress);
            }
            catch
            {
                return false;
            }
        }

        public static string NormalizeText(string input)
        {
            // Convert to lowercase
            string normalized = input.ToLowerInvariant();

            // Remove all non-alphanumeric characters
            normalized = Regex.Replace(normalized, @"[^\w\s]", "");

            return normalized;
        }
    }
}
