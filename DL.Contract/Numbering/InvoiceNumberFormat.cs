using System.Globalization;
using System.Text;

namespace _0nline.Biller.DL.Contract.Numbering
{
    /// <summary>
    /// A tenant's invoice number format: literal text with exactly one <c>{n}</c>
    /// token and an optional year token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The token renders the counter; a run of zeros after a colon sets the minimum
    /// width. Everything outside a token is printed as typed, so a literal zero
    /// (a year such as 2026, for instance) is not a placeholder and needs no escaping.
    /// </para>
    /// <code>
    /// {n}            -> 84
    /// {n:00000}      -> 00084
    /// #{n:00000}     -> #00084
    /// INV-{n:0000}   -> INV-0084
    /// 2026/{n:0000}  -> 2026/0084
    /// {yyyy}/{n:0000}-> 2026/0084
    /// </code>
    /// <para>
    /// The width is a minimum: a number that outgrows it widens instead of being
    /// truncated, so #99999 is followed by #100000. The year token does not reset the
    /// sequence - it only renders the invoice's year.
    /// </para>
    /// <para>
    /// This is a generator, not a validator. A number typed by the tenant is kept as
    /// typed and does not have to match the format; uniqueness is the database's job.
    /// </para>
    /// </remarks>
    public sealed class InvoiceNumberFormat
    {
        /// <summary>The format a new tenant starts with.</summary>
        public const string DefaultFormat = "#{n:00000}";

        /// <summary>Longest format text and longest rendered number, both set by <see cref="Text"/> / Invoice.InvoiceNumber.</summary>
        public const int MaxLength = 30;

        /// <summary>Longest zero run, kept to a single int's worth of digits.</summary>
        public const int MaxDigits = 9;

        // int.MaxValue is ten digits; a width below that can still render ten.
        private const int MaxNumberWidth = 10;

        private enum SegmentKind
        {
            Literal,
            Number,
            Year
        }

        private readonly record struct Segment(SegmentKind Kind, string Literal, int Width);

        private readonly Segment[] _segments;

        private InvoiceNumberFormat(string text, Segment[] segments, int digits, bool hasYear, int maxRenderedLength)
        {
            Text = text;
            _segments = segments;
            Digits = digits;
            HasYear = hasYear;
            MaxRenderedLength = maxRenderedLength;
        }

        /// <summary>The normalised format text, as stored in the tenant's settings.</summary>
        public string Text { get; }

        /// <summary>Zero-padding width of the number, or 0 when the number is unpadded.</summary>
        public int Digits { get; }

        /// <summary>Whether the format prints the invoice's year.</summary>
        public bool HasYear { get; }

        /// <summary>The longest number this format can produce for any counter value.</summary>
        public int MaxRenderedLength { get; }

        /// <summary>Renders <paramref name="number"/> for an invoice dated <paramref name="invoiceDate"/>.</summary>
        public string Render(int number, DateTime invoiceDate)
        {
            if (number < 0)
                throw new ArgumentOutOfRangeException(nameof(number), "An invoice number may not be negative.");

            var text = new StringBuilder(MaxRenderedLength);

            foreach (var segment in _segments)
            {
                switch (segment.Kind)
                {
                    case SegmentKind.Literal:
                        text.Append(segment.Literal);
                        break;

                    case SegmentKind.Number:
                        text.Append(PadNumber(number, segment.Width));
                        break;

                    case SegmentKind.Year:
                        text.Append(PadYear(invoiceDate.Year, segment.Width));
                        break;
                }
            }

            return text.ToString();
        }

        /// <summary>Parses a format. On failure <paramref name="error"/> explains why.</summary>
        public static bool TryParse(string? text, out InvoiceNumberFormat? format, out string? error)
        {
            format = null;

            if (string.IsNullOrWhiteSpace(text))
            {
                error = $"Enter an invoice number format, for example {DefaultFormat}.";
                return false;
            }

            var input = text.Trim();

            if (input.Length > MaxLength)
            {
                error = $"The format may not exceed {MaxLength} characters.";
                return false;
            }

            var segments = new List<Segment>();
            var literal = new StringBuilder();
            var digits = 0;
            var hasNumber = false;
            var hasYear = false;
            var yearWidth = 0;

            for (var index = 0; index < input.Length; index++)
            {
                var character = input[index];

                if (character == '}')
                {
                    error = "Braces are only used for {n} and {yyyy}.";
                    return false;
                }

                if (character != '{')
                {
                    literal.Append(character);
                    continue;
                }

                var close = input.IndexOf('}', index + 1);
                if (close < 0)
                {
                    error = "A brace is not closed. Use {n} for the number.";
                    return false;
                }

                var token = input.Substring(index + 1, close - index - 1).Trim();

                if (literal.Length > 0)
                {
                    segments.Add(new Segment(SegmentKind.Literal, literal.ToString(), 0));
                    literal.Clear();
                }

                if (token == "n" || token.StartsWith("n:", StringComparison.Ordinal))
                {
                    if (hasNumber)
                    {
                        error = "The format may contain only one {n}.";
                        return false;
                    }

                    var width = 0;
                    if (token.Length > 1)
                    {
                        var spec = token[2..];
                        if (spec.Length is 0 or > MaxDigits || spec.Any(digit => digit != '0'))
                        {
                            error = $"Write the number width as a run of zeros, for example {{n:00000}}, up to {MaxDigits} digits.";
                            return false;
                        }

                        width = spec.Length;
                    }

                    digits = width;
                    hasNumber = true;
                    segments.Add(new Segment(SegmentKind.Number, string.Empty, width));
                }
                else if (token is "y" or "yyyy" or "y:2" or "y:4")
                {
                    if (hasYear)
                    {
                        error = "The format may contain only one year token.";
                        return false;
                    }

                    yearWidth = token == "y:2" ? 2 : 4;
                    hasYear = true;
                    segments.Add(new Segment(SegmentKind.Year, string.Empty, yearWidth));
                }
                else
                {
                    error = $"'{token}' is not a token. Use {{n}} for the number and {{yyyy}} for the year.";
                    return false;
                }

                index = close;
            }

            if (literal.Length > 0)
                segments.Add(new Segment(SegmentKind.Literal, literal.ToString(), 0));

            if (!hasNumber)
            {
                error = "The format needs an {n} somewhere for the number.";
                return false;
            }

            var literalLength = segments
                .Where(segment => segment.Kind == SegmentKind.Literal)
                .Sum(segment => segment.Literal.Length);

            var maxRenderedLength = literalLength + yearWidth + Math.Max(digits, MaxNumberWidth);

            if (maxRenderedLength > MaxLength)
            {
                error = $"This format can produce {maxRenderedLength} characters; an invoice number may not exceed {MaxLength}.";
                return false;
            }

            format = new InvoiceNumberFormat(input, segments.ToArray(), digits, hasYear, maxRenderedLength);
            error = null;
            return true;
        }

        /// <summary>Returns the validation message for a format, or null when it is usable.</summary>
        public static string? Validate(string? text)
            => TryParse(text, out _, out var error) ? null : error;

        /// <summary>
        /// Reads the numeric portion back out of a number: its trailing digit run. The
        /// counter follows a number the tenant types, which is how 2026/0084 reads as 84.
        /// A number with no digits, or one too large for the counter, reads as nothing.
        /// </summary>
        public static bool TryReadNumber(string? invoiceNumber, out int number)
        {
            number = 0;
            if (string.IsNullOrWhiteSpace(invoiceNumber))
                return false;

            var end = invoiceNumber.Length;
            var start = end;
            while (start > 0 && char.IsAsciiDigit(invoiceNumber[start - 1]))
                start--;

            return start < end
                && int.TryParse(
                    invoiceNumber.AsSpan(start, end - start),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out number);
        }

        private static string PadNumber(int number, int width)
        {
            var text = number.ToString(CultureInfo.InvariantCulture);
            return width <= text.Length ? text : text.PadLeft(width, '0');
        }

        private static string PadYear(int year, int width)
            => width == 2
                ? (year % 100).ToString("D2", CultureInfo.InvariantCulture)
                : year.ToString("D4", CultureInfo.InvariantCulture);
    }
}
