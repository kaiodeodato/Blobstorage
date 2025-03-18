namespace BlobStorage.Helpers
{
    public class StringHelpers
    {
        public static string RemoveNonAsciiCharacters(string input)
        {
            return string.Concat(input.Where(c => c <= 127));
        }
    }
}
