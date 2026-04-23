namespace SR3Generator.Database.Queries
{
    /// <summary>
    /// Parses the <c>"{bookcode}.{page}"</c> strings used in every item table
    /// (spells, adept powers, gear, foci, cyberware, bioware).
    /// Book codes can include digits (<c>cb1</c>, <c>sr3</c>), so we split on the last '.'.
    /// Trailing junk after the page number is tolerated — some legacy rows carry values
    /// like <c>"cb3.101-Page"</c> or <c>"cb2.49,SR2-???"</c>.
    /// </summary>
    internal static class BookPageParser
    {
        public static (string Book, int Page) Split(string? bookPage)
        {
            if (string.IsNullOrWhiteSpace(bookPage)) return (string.Empty, 0);

            var dot = bookPage.LastIndexOf('.');
            if (dot < 0) return (bookPage, 0);

            var book = bookPage[..dot];
            if (dot == bookPage.Length - 1) return (book, 0);

            var pageStr = bookPage[(dot + 1)..];
            var end = 0;
            while (end < pageStr.Length && char.IsDigit(pageStr[end])) end++;
            if (end == 0) return (book, 0);

            return int.TryParse(pageStr[..end], out var page) ? (book, page) : (book, 0);
        }
    }
}
