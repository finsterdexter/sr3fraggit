using SR3Generator.Database.Queries;

namespace SR3Generator.Database.Test
{
    public class BookPageParserTests
    {
        [Theory]
        // Standard cases
        [InlineData("sr3.300", "sr3", 300)]
        [InlineData("cc.8", "cc", 8)]
        [InlineData("mits.25", "mits", 25)]
        // Book codes that contain digits — this is the case the old letter-walking parser
        // got wrong: "cb1.2" used to return book="cb", page=1.
        [InlineData("cb1.2", "cb1", 2)]
        [InlineData("cb3.98", "cb3", 98)]
        [InlineData("sr2.100", "sr2", 100)]
        // Missing page after the dot
        [InlineData("cc.", "cc", 0)]
        // Zero page
        [InlineData("bjf.00", "bjf", 0)]
        // Trailing junk after the page number (real rows from the gear table)
        [InlineData("cb3.101-Page", "cb3", 101)]
        [InlineData("cb2.49,SR2-???", "cb2", 49)]
        // No dot at all: everything is the book, page is 0
        [InlineData("sr3", "sr3", 0)]
        // Empty / null / whitespace → empty book, zero page
        [InlineData("", "", 0)]
        [InlineData("   ", "", 0)]
        public void Split_ParsesVariousFormats(string input, string expectedBook, int expectedPage)
        {
            var (book, page) = BookPageParser.Split(input);
            Assert.Equal(expectedBook, book);
            Assert.Equal(expectedPage, page);
        }

        [Fact]
        public void Split_NullInput_ReturnsEmpty()
        {
            var (book, page) = BookPageParser.Split(null);
            Assert.Equal(string.Empty, book);
            Assert.Equal(0, page);
        }
    }
}
