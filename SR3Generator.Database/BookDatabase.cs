using Microsoft.Extensions.Options;
using SR3Generator.Data;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    /// <summary>
    /// Canonical list of published sourcebooks. Loaded once at startup from the <c>books</c> table.
    /// Each item's <c>Book</c> field on domain models refers to one of these
    /// <see cref="Book.Abbreviation"/> values.
    /// </summary>
    public class BookDatabase
    {
        /// <summary>Abbreviation of the core rulebook. Always enabled regardless of user settings. </summary>
        public const string CoreBookAbbreviation = "sr3";

        public IReadOnlyList<Book> Books { get; }

        public BookDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options), new ReadBooksQueryHandler())
        {
        }

        internal BookDatabase(DbConnectionFactory factory, ReadBooksQueryHandler handler)
        {
            using var conn = factory.CreateConnection();
            Books = handler.HandleAsync(new ReadBooksQuery(), conn, null!).Result.ToList();
        }
    }
}
