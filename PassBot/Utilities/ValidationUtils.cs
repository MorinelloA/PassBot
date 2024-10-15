namespace PassBot.Utilities
{
    public static class ValidationUtils
    {
        public static bool IsValidEmail(string email)
        {
            try
            {
                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
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
    }
}
